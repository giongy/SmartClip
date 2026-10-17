using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Text;

// A folder of saved entries. Folders hold folders too: a tree, each knowing the one it is in.
class Folder
{
    public long Id, ParentId;   // ParentId 0: outside them all
    public string Name;
    public bool Open;           // what it holds is shown
    public int Level;           // how many folders it is inside of
}

// What the list offers, kept in one SQLite file: the texts and pictures copied, and those the user saved.
//
// One table holds both. A copied entry is temporary: it goes when its days are over, also if it is kept at the
// top of its column (pinned), and it is stored as it is, unencrypted. A saved entry never goes by itself, can be
// in a folder and can have a name (the list then shows the name and hides the content); it is encrypted for the
// Windows user (DPAPI), content and preview. A number from 1 to MainForm.MaxNumber chooses one entry of either
// kind from anywhere. The database itself refuses what breaks these rules (the CHECKs of Schema).
//
// Times are kept in UTC, in milliseconds since 1970; days are counted in local time. Used from one thread.
sealed class Clips : IDisposable
{
    public const int SchemaVersion = 1;   // in PRAGMA user_version: a newer file is refused, an older one brought up to date
    const int TextKind = 1, PictureKind = 2;
    static readonly string Schema =
        "CREATE TABLE folders ("
        + " id INTEGER PRIMARY KEY,"
        + " parent_id INTEGER NOT NULL DEFAULT 0,"   // 0: outside them all, which is why it has no foreign key
        + " name TEXT NOT NULL,"
        + " name_key TEXT NOT NULL,"                 // the name as compared: see Key
        + " open INTEGER NOT NULL DEFAULT 1,"
        + " UNIQUE (parent_id, name_key));"
        + "CREATE TABLE entries ("
        + " id INTEGER PRIMARY KEY,"
        + " kind INTEGER NOT NULL CHECK (kind IN (" + TextKind + ", " + PictureKind + ")),"
        + " content BLOB NOT NULL,"                  // the text in UTF-8, or the picture as PNG; encrypted if saved
        + " preview BLOB,"                           // of a picture, as PNG; encrypted if saved
        + " width INTEGER, height INTEGER,"          // of a picture
        + " folded TEXT,"                            // of a copied entry, as the search compares it (Texts.Fold)
        + " hash BLOB,"                              // of a copied entry, to know it when it is copied again
        + " copied_at INTEGER NOT NULL,"
        + " saved INTEGER NOT NULL DEFAULT 0 CHECK (saved IN (0, 1)),"
        + " saved_at INTEGER,"                       // null if nobody knows (saved before the days were kept)
        + " name TEXT, name_key TEXT UNIQUE,"        // more entries without a name are fine: UNIQUE leaves NULLs alone
        + " folder_id INTEGER REFERENCES folders (id),"
        + " pinned INTEGER NOT NULL DEFAULT 0 CHECK (pinned IN (0, 1)),"
        + " number INTEGER UNIQUE CHECK (number BETWEEN 1 AND " + MainForm.MaxNumber + "),"
        + " CHECK (saved = 1 OR (name IS NULL AND folder_id IS NULL AND saved_at IS NULL)),"
        // Nothing of a saved entry stays readable: its search text and its hash would give its content away.
        + " CHECK (saved = 0 OR (pinned = 0 AND folded IS NULL AND hash IS NULL)),"
        + " CHECK ((name IS NULL) = (name_key IS NULL)),"
        + " CHECK ((kind = " + PictureKind + ") = (preview IS NOT NULL)));"
        + "CREATE INDEX entries_recent ON entries (saved, pinned DESC, copied_at DESC);"
        + "CREATE INDEX entries_hash ON entries (hash) WHERE hash IS NOT NULL;";
    static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    // The columns an entry is read from, in the order Read takes them. The content of a copied picture is left
    // out: it is read when it is chosen (Png).
    static readonly string EntryColumns ="id, kind, CASE WHEN kind = " + TextKind + " THEN content END, preview, width, height,"
        + " copied_at, saved, saved_at, name, folder_id, pinned, number";

    readonly Database data;
    readonly string path;
    DateTime pruned;   // the day the expired entries were last removed

    // Opens the file at 'path', creating it if it is not there.
    public Clips(string path)
    {
        this.path = path;
        data = new Database(path);
        try
        {
            // A removed entry is overwritten, not only forgotten; nothing is lost to a crash or a power cut.
            data.Execute("PRAGMA foreign_keys = ON; PRAGMA secure_delete = ON; PRAGMA synchronous = FULL; PRAGMA journal_mode = DELETE");
            long version = (long)data.Value("PRAGMA user_version");
            if (version > SchemaVersion)
                throw new InvalidDataException(string.Format(Strings.NewerDatabase, path, Program.AppName));
            if (version == 0) Create();
        }
        catch (Exception)
        {
            data.Dispose();
            throw;
        }
        KeepDays = Texts.DefaultKeepDays;
    }

    void Create()
    {
        data.Execute("PRAGMA auto_vacuum = INCREMENTAL");   // only before the first table: see Compact
        using (Transaction transaction = data.Begin())
        {
            data.Execute(Schema);
            data.Execute("PRAGMA user_version = " + SchemaVersion);
            transaction.Commit();
        }
    }

    public string FileName
    {
        get { return path; }
    }

    // For the migration from the registry and the history files, which writes in bulk.
    internal Database Data
    {
        get { return data; }
    }

    public void Dispose()
    {
        data.Dispose();
    }

    // --- Days ---

    // For how many days a copied entry stays, today being the first: zero keeps today's only.
    public int KeepDays { get; private set; }

    public void Keep(int days)
    {
        KeepDays = days;
        Prune();
    }

    // Removes the copied entries whose days are over, pinned or not.
    public void Prune()
    {
        pruned = DateTime.Today;
        Drop(DateTime.Today.AddDays(-KeepDays));
    }

    // Asked by the user: removes at once what was copied before the last 'days' days, today being the first;
    // with zero, all of it. Saved entries stay.
    public void Clean(int days)
    {
        Drop(DateTime.Today.AddDays(1 - days));
    }

    // Forgets every copied entry: the history was turned off.
    public void ClearCopied()
    {
        data.Execute("DELETE FROM entries WHERE saved = 0");
        Compact();
    }

    void Drop(DateTime oldest)
    {
        data.Execute("DELETE FROM entries WHERE saved = 0 AND copied_at < ?1", Stamp(oldest));
        Compact();
    }

    // Gives back to the disk the room of what was removed, a little at a time instead of rebuilding the file.
    void Compact()
    {
        data.Execute("PRAGMA incremental_vacuum");
    }

    // --- Copied entries ---

    // Remembers a copied text. Copied again, it is the same entry, which only takes the new time: at the top,
    // with its star and its number. False if there is nothing worth keeping.
    public bool AddText(string text)
    {
        if (!Worth(text)) return false;
        byte[] content = Encoding.UTF8.GetBytes(text);
        Insert(TextKind, content, null, 0, 0, Texts.Fold(text), Hash(content), DateTime.Now, false);
        return true;
    }

    static bool Worth(string text)
    {
        return text != null && text.Trim().Length > 0 && text.Length <= Texts.MaxLength;
    }

    // Remembers a copied picture, as PNG with a small preview. The same picture again, also because Windows
    // announces one copy several times, is the same entry.
    public void AddPicture(Image picture)
    {
        Entry entry = new Entry(picture);
        using (entry.Preview)
            Insert(PictureKind, entry.Png, Entry.ToPng(entry.Preview), entry.Width, entry.Height, Texts.Fold(entry.Label),
                Hash(entry.Png), DateTime.Now, false);
    }

    // Puts back a copied text read from an exported file, at the time it was copied. False if it was left out:
    // nothing worth keeping, or there already, whatever its line ends (the file has its own).
    public bool Import(string text, DateTime time, bool pinned)
    {
        if (!Worth(text)) return false;
        string bare = text.Replace("\r\n", "\n");
        foreach (string each in new string[] { text, bare, bare.Replace("\n", "\r\n") })
            if (data.Value("SELECT 1 FROM entries WHERE hash = ?1", Hash(Encoding.UTF8.GetBytes(each))) != null) return false;
        byte[] content = Encoding.UTF8.GetBytes(text);
        Insert(TextKind, content, null, 0, 0, Texts.Fold(text), Hash(content), time, pinned);
        return true;
    }

    void Insert(int kind, byte[] content, byte[] preview, int width, int height, string folded, byte[] hash, DateTime time, bool pinned)
    {
        if (pruned != DateTime.Today) Prune();
        data.Execute("UPDATE entries SET copied_at = max(copied_at, ?2) WHERE hash = ?1", hash, Stamp(time));
        if (data.Changes > 0) return;
        data.Execute("INSERT INTO entries (kind, content, preview, width, height, folded, hash, copied_at, pinned)"
            + " VALUES (?1, ?2, ?3, ?4, ?5, ?6, ?7, ?8, ?9)",
            kind, content, preview, kind == PictureKind ? (object)width : null, kind == PictureKind ? (object)height : null,
            folded, hash, Stamp(time), pinned);
    }

    // The copied entries the list shows, those at the top first and then the newest, at most 'most'. 'words' are
    // those of a search, as Texts.Words gives them: every one must be there.
    public List<Entry> Copied(int most, string[] words)
    {
        StringBuilder sql = new StringBuilder("SELECT " + EntryColumns + " FROM entries WHERE saved = 0");
        List<object> values = new List<object>();
        foreach (string word in words)
        {
            values.Add(word);
            sql.Append(" AND instr(folded, ?" + values.Count + ") > 0");
        }
        values.Add(most);
        sql.Append(" ORDER BY pinned DESC, copied_at DESC, id DESC LIMIT ?" + values.Count);
        List<Entry> entries = new List<Entry>();
        using (Statement rows = data.Prepare(sql.ToString(), values.ToArray()))
            while (rows.Step()) entries.Add(Read(rows));
        return entries;
    }

    // How many copied entries there are, also beyond those the list shows.
    public int CopiedCount()
    {
        return (int)(long)data.Value("SELECT count(*) FROM entries WHERE saved = 0");
    }

    // Forgets a copied text, typed as a password and cleared from the clipboard: unless the user kept it at the
    // top or gave it a number.
    public void Forget(string text)
    {
        data.Execute("DELETE FROM entries WHERE hash = ?1 AND pinned = 0 AND number IS NULL", Hash(Encoding.UTF8.GetBytes(text)));
    }

    // Keeps a copied entry at the top of its column, or lets it go back among the others. It goes all the same
    // when its days are over.
    public void Pin(long id, bool pinned)
    {
        data.Execute("UPDATE entries SET pinned = ?2 WHERE id = ?1 AND saved = 0", id, pinned);
    }

    // --- Any entry ---

    public void Remove(long id)
    {
        data.Execute("DELETE FROM entries WHERE id = ?1", id);
    }

    // Gives an entry a number, taking it from whatever had it; zero takes its own away.
    public void Number(long id, int number)
    {
        using (Transaction transaction = data.Begin())
        {
            if (number > 0) data.Execute("UPDATE entries SET number = NULL WHERE number = ?1", number);
            data.Execute("UPDATE entries SET number = ?2 WHERE id = ?1", id, number > 0 ? (object)number : null);
            transaction.Commit();
        }
    }

    // The entry a number chooses, or null.
    public Entry Numbered(int number)
    {
        using (Statement row = data.Prepare("SELECT " + EntryColumns + " FROM entries WHERE number = ?1", number))
            return row.Step() ? Read(row) : null;
    }

    // The whole picture of an entry, as PNG.
    public byte[] Png(long id)
    {
        using (Statement row = data.Prepare("SELECT content, saved FROM entries WHERE id = ?1 AND kind = ?2", id, PictureKind))
        {
            if (!row.Step()) throw new InvalidOperationException("No picture " + id);
            return row.Bool(1) ? Unprotect(row.Blob(0)) : row.Blob(0);
        }
    }

    // --- Saved entries ---

    // Every saved entry, its content readable again, in the order of what the list shows of each. One saved
    // by another Windows user, or on another PC, cannot be read and is left out.
    public List<Entry> Saved()
    {
        List<Entry> entries = new List<Entry>();
        using (Statement rows = data.Prepare("SELECT " + EntryColumns + " FROM entries WHERE saved = 1"))
            while (rows.Step())
            {
                try
                {
                    entries.Add(Read(rows));
                }
                catch (CryptographicException)
                {
                }
            }
        entries.Sort(delegate(Entry a, Entry b) { return StringComparer.CurrentCultureIgnoreCase.Compare(a.Label, b.Label); });
        return entries;
    }

    // The id of the saved entry with that name, whatever its letter case, or 0.
    public long Named(string name)
    {
        object id = data.Value("SELECT id FROM entries WHERE name_key = ?1", Key(name));
        return id == null ? 0 : (long)id;
    }

    // Saves a copied entry, under a name or without one, in a folder (0: none). From now on it never goes by
    // itself, and only this Windows user can read it.
    public void Save(long id, string name, long folder)
    {
        using (Transaction transaction = data.Begin())
        {
            byte[] content, preview;
            using (Statement row = data.Prepare("SELECT content, preview FROM entries WHERE id = ?1 AND saved = 0", id))
            {
                if (!row.Step()) throw new InvalidOperationException("No copied entry " + id);
                content = row.Blob(0);
                preview = row.Blob(1);
            }
            data.Execute("UPDATE entries SET saved = 1, saved_at = ?2, name = ?3, name_key = ?4, folder_id = ?5, pinned = 0,"
                + " folded = NULL, hash = NULL, content = ?6, preview = ?7 WHERE id = ?1",
                id, Stamp(DateTime.Now), Name(name), Key(name), Place(folder), Protect(content), preview == null ? null : Protect(preview));
            transaction.Commit();
        }
    }

    // A new saved text, written by the user or read from an exported file ('time' DateTime.MinValue: nobody
    // knows when it was saved). Returns its id.
    public long AddSaved(string name, string text, long folder, DateTime time)
    {
        bool dated = time != DateTime.MinValue;
        data.Execute("INSERT INTO entries (kind, content, copied_at, saved, saved_at, name, name_key, folder_id)"
            + " VALUES (?1, ?2, ?3, 1, ?4, ?5, ?6, ?7)",
            TextKind, Protect(Encoding.UTF8.GetBytes(text)), Stamp(dated ? time : DateTime.Now), dated ? (object)Stamp(time) : null,
            Name(name), Key(name), Place(folder));
        return data.LastId;
    }

    // Changes the name, the folder and, unless 'text' is null (a picture), the content of a saved entry.
    public void Change(long id, string name, string text, long folder)
    {
        if (text == null)
            data.Execute("UPDATE entries SET name = ?2, name_key = ?3, folder_id = ?4 WHERE id = ?1 AND saved = 1",
                id, Name(name), Key(name), Place(folder));
        else
            data.Execute("UPDATE entries SET name = ?2, name_key = ?3, folder_id = ?4, content = ?5 WHERE id = ?1 AND saved = 1 AND kind = ?6",
                id, Name(name), Key(name), Place(folder), Protect(Encoding.UTF8.GetBytes(text)), TextKind);
        if (data.Changes == 0) throw new InvalidOperationException("No saved entry " + id);
    }

    // Puts a saved entry into a folder, or with 0 out of them all.
    public void Move(long id, long folder)
    {
        data.Execute("UPDATE entries SET folder_id = ?2 WHERE id = ?1 AND saved = 1", id, Place(folder));
    }

    // --- Folders ---

    // Every folder, each right before those inside it, those side by side by name.
    public List<Folder> Folders()
    {
        Dictionary<long, List<Folder>> inside = new Dictionary<long, List<Folder>>();
        using (Statement rows = data.Prepare("SELECT id, parent_id, name, open FROM folders"))
            while (rows.Step())
            {
                Folder folder = new Folder();
                folder.Id = rows.Long(0);
                folder.ParentId = rows.Long(1);
                folder.Name = rows.Text(2);
                folder.Open = rows.Bool(3);
                List<Folder> siblings;
                if (!inside.TryGetValue(folder.ParentId, out siblings)) inside[folder.ParentId] = siblings = new List<Folder>();
                siblings.Add(folder);
            }
        List<Folder> folders = new List<Folder>();
        AddTree(inside, 0, 0, folders);
        return folders;
    }

    static void AddTree(Dictionary<long, List<Folder>> inside, long parent, int level, List<Folder> folders)
    {
        List<Folder> siblings;
        if (!inside.TryGetValue(parent, out siblings)) return;
        siblings.Sort(delegate(Folder a, Folder b) { return StringComparer.CurrentCultureIgnoreCase.Compare(a.Name, b.Name); });
        foreach (Folder folder in siblings)
        {
            folder.Level = level;
            folders.Add(folder);
            AddTree(inside, folder.Id, level + 1, folders);
        }
    }

    // A new folder inside 'parent' (0: outside them all). Returns its id; refused if the name is taken there.
    public long AddFolder(long parent, string name)
    {
        data.Execute("INSERT INTO folders (parent_id, name, name_key) VALUES (?1, ?2, ?3)", parent, name, Key(name));
        return data.LastId;
    }

    // Refused if the name is taken where the folder is.
    public void RenameFolder(long id, string name)
    {
        data.Execute("UPDATE folders SET name = ?2, name_key = ?3 WHERE id = ?1", id, name, Key(name));
    }

    // Moves a folder, with all it holds, into another (0: out of them all). Never into itself or one it holds;
    // refused if the name is taken there.
    public void MoveFolder(long id, long parent)
    {
        for (long at = parent; at != 0; at = ParentOf(at))
            if (at == id) throw new ArgumentException("A folder cannot go inside itself");
        data.Execute("UPDATE folders SET parent_id = ?2 WHERE id = ?1", id, parent);
    }

    public void OpenFolder(long id, bool open)
    {
        data.Execute("UPDATE folders SET open = ?2 WHERE id = ?1", id, open);
    }

    // Opens a folder and those it is inside of, so that what it holds is seen; 0 is none.
    public void Reveal(long id)
    {
        for (; id != 0; id = ParentOf(id)) OpenFolder(id, true);
    }

    // Deletes a folder, not what it holds: its entries are too easily passwords to go along. They and its folders
    // move up one level; a folder there with the same name takes in what the one coming up holds.
    public void DeleteFolder(long id)
    {
        using (Transaction transaction = data.Begin())
        {
            long parent = ParentOf(id);
            data.Execute("UPDATE entries SET folder_id = ?2 WHERE folder_id = ?1", id, Place(parent));
            List<long> children = ChildrenOf(id);
            data.Execute("DELETE FROM folders WHERE id = ?1", id);
            foreach (long child in children) Join(child, parent);
            transaction.Commit();
        }
    }

    // Puts a folder inside 'parent'. If one there has its name already, that one takes in what it holds, the
    // folders inside it the same way, and it goes.
    void Join(long folder, long parent)
    {
        object same = data.Value("SELECT id FROM folders WHERE parent_id = ?1 AND id <> ?2"
            + " AND name_key = (SELECT name_key FROM folders WHERE id = ?2)", parent, folder);
        if (same == null)
        {
            data.Execute("UPDATE folders SET parent_id = ?2 WHERE id = ?1", folder, parent);
            return;
        }
        long into = (long)same;
        data.Execute("UPDATE entries SET folder_id = ?2 WHERE folder_id = ?1", folder, into);
        List<long> children = ChildrenOf(folder);
        data.Execute("DELETE FROM folders WHERE id = ?1", folder);
        foreach (long child in children) Join(child, into);
    }

    long ParentOf(long id)
    {
        object parent = data.Value("SELECT parent_id FROM folders WHERE id = ?1", id);
        if (parent == null) throw new InvalidOperationException("No folder " + id);
        return (long)parent;
    }

    List<long> ChildrenOf(long id)
    {
        List<long> children = new List<long>();
        using (Statement rows = data.Prepare("SELECT id FROM folders WHERE parent_id = ?1", id))
            while (rows.Step()) children.Add(rows.Long(0));
        return children;
    }

    // --- Rows and values ---

    // An entry from a row of EntryColumns.
    static Entry Read(Statement row)
    {
        bool saved = row.Bool(7);
        string name = row.Text(9);
        DateTime time = saved ? (row.IsNull(8) ? DateTime.MinValue : Moment(row.Long(8))) : Moment(row.Long(6));
        Entry entry;
        if (row.Int(1) == PictureKind)
        {
            byte[] preview = saved ? Unprotect(row.Blob(3)) : row.Blob(3);
            // Copied into a bitmap of its own, which does not need the stream to stay open.
            using (MemoryStream packed = new MemoryStream(preview))
            using (Image image = Image.FromStream(packed))
                entry = new Entry(new Bitmap(image), row.Int(4), row.Int(5), time, name);
        }
        else
        {
            byte[] content = row.Blob(2);
            entry = new Entry(name, Encoding.UTF8.GetString(saved ? Unprotect(content) : content), time);
        }
        entry.Id = row.Long(0);
        entry.Saved = saved;
        entry.FolderId = row.IsNull(10) ? 0 : row.Long(10);
        entry.Pinned = row.Bool(11);
        entry.Number = row.IsNull(12) ? 0 : row.Int(12);
        return entry;
    }

    static long Stamp(DateTime time)
    {
        return (long)(time.ToUniversalTime() - Epoch).TotalMilliseconds;
    }

    static DateTime Moment(long stamp)
    {
        return Epoch.AddMilliseconds(stamp).ToLocalTime();
    }

    // A name as it is stored: none if empty.
    static string Name(string name)
    {
        return name == null || name.Trim().Length == 0 ? null : name.Trim();
    }

    // A name as names are compared, so that two cannot differ only in letter case: "Città" and "CITTÀ" are one.
    // SQLite's own NOCASE knows only the English letters.
    static string Key(string name)
    {
        string kept = Name(name);
        return kept == null ? null : kept.ToUpperInvariant();
    }

    // A folder as a column holds it: 0, none, is NULL.
    static object Place(long folder)
    {
        return folder == 0 ? null : (object)folder;
    }

    static byte[] Hash(byte[] content)
    {
        using (SHA256 sha = SHA256.Create()) return sha.ComputeHash(content);
    }

    static byte[] Protect(byte[] plain)
    {
        return ProtectedData.Protect(plain, null, DataProtectionScope.CurrentUser);
    }

    static byte[] Unprotect(byte[] secret)
    {
        return ProtectedData.Unprotect(secret, null, DataProtectionScope.CurrentUser);
    }
}
