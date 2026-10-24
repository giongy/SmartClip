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
// top of the list (pinned), and it is stored as it is, unencrypted. A saved entry never goes by itself, can be
// in a folder and can have a name (the list then shows the name and hides the content); it is encrypted for the
// Windows user (DPAPI), content and preview. A number from 1 to MainForm.MaxNumber chooses one entry of either
// kind from anywhere. The database itself refuses what breaks these rules (the CHECKs of EntriesSchema).
//
// Times are kept in UTC, in milliseconds since 1970; days are counted in local time. Used from one thread.
sealed class Clips : IDisposable
{
    // In PRAGMA user_version: a newer file is refused, an older one brought up to date (Upgrade). 2: the large
    // columns of the entries at the end of their rows. 3: the formatting of saved texts. 4: the program each copied
    // entry was copied in. 5: what it is (Category). 6: what the user says it is, of a saved entry too. 7: the indexes
    // of the sidebar, the tabs and the room of the pictures (LookupIndexes).
    public const int SchemaVersion = 7;
    const int TextKind = 1, PictureKind = 2;
    // The most the copied pictures take, previews included: beyond it the oldest go (TrimPictures). The checks
    // make it smaller.
    public static long PictureSpace = 200L * 1024 * 1024;
    const string FoldersSchema =
        "CREATE TABLE folders ("
        + " id INTEGER PRIMARY KEY,"
        + " parent_id INTEGER NOT NULL DEFAULT 0,"   // 0: outside them all, which is why it has no foreign key
        + " name TEXT NOT NULL,"
        + " name_key TEXT NOT NULL,"                 // the name as compared: see Key
        + " open INTEGER NOT NULL DEFAULT 1,"
        + " UNIQUE (parent_id, name_key));";
    // The programs things were copied in (Origin), each once: the name and the icon the rows of the copied entries
    // show. Those no copied entry comes from any more go with the expired entries (DropApps).
    const string AppsSchema =
        "CREATE TABLE apps ("
        + " id INTEGER PRIMARY KEY,"
        + " path TEXT NOT NULL UNIQUE,"              // of its executable, as Windows gives it (of a remote one: Origin.Within)
        + " name TEXT NOT NULL,"
        + " icon BLOB);";                            // PNG; null if none could be read
    // The small columns first, the large ones last: SQLite keeps a large value in a chain of pages of its own, and
    // reads through all of it to get to any column after it. So the list and the search read times, names and
    // search texts without touching the pictures.
    static readonly string EntriesSchema =
        "CREATE TABLE entries ("
        + " id INTEGER PRIMARY KEY,"
        + " kind INTEGER NOT NULL CHECK (kind IN (" + TextKind + ", " + PictureKind + ")),"
        + " width INTEGER, height INTEGER,"          // of a picture
        + " copied_at INTEGER NOT NULL,"
        + " saved INTEGER NOT NULL DEFAULT 0 CHECK (saved IN (0, 1)),"
        + " saved_at INTEGER,"                       // null if nobody knows (saved before the days were kept)
        + " name TEXT, name_key TEXT UNIQUE,"        // more entries without a name are fine: UNIQUE leaves NULLs alone
        + " folder_id INTEGER REFERENCES folders (id),"
        + " pinned INTEGER NOT NULL DEFAULT 0 CHECK (pinned IN (0, 1)),"
        + " number INTEGER UNIQUE CHECK (number BETWEEN 1 AND " + MainForm.MaxNumber + "),"
        + " app_id INTEGER REFERENCES apps (id),"    // the program a copied entry was copied in, if known
        // What an entry is (Category), for the tabs of the list: of a copied one as guessed (Texts.CategoryOf), or as
        // the user chose ('chosen'); of a saved one only as chosen, otherwise it is guessed again from its content.
        + " category INTEGER CHECK (category BETWEEN 1 AND 4),"
        + " chosen INTEGER NOT NULL DEFAULT 0 CHECK (chosen IN (0, 1)),"
        + " rtf BLOB, html BLOB,"                    // of a formatted saved text (Rich), encrypted
        + " hash BLOB,"                              // of a copied entry, to know it when it is copied again
        + " folded TEXT,"                            // of a copied entry, as the search compares it (Texts.Fold)
        + " preview BLOB,"                           // of a picture, as PNG; encrypted if saved
        + " content BLOB NOT NULL,"                  // the text in UTF-8, or the picture as PNG; encrypted if saved
        + " CHECK (saved = 1 OR (name IS NULL AND folder_id IS NULL AND saved_at IS NULL)),"
        // The content of a saved entry stays unreadable: its search text and its hash would give it away, and the
        // program it was copied in a little of it. What it is says no more than its name, which is readable too.
        + " CHECK (saved = 0 OR (pinned = 0 AND folded IS NULL AND hash IS NULL AND app_id IS NULL AND (category IS NULL OR chosen = 1))),"
        + " CHECK (chosen = 0 OR category IS NOT NULL),"
        + " CHECK (category IS NULL OR (category = " + (int)Category.Picture + ") = (kind = " + PictureKind + ")),"
        + " CHECK ((name IS NULL) = (name_key IS NULL)),"
        + " CHECK ((kind = " + PictureKind + ") = (preview IS NOT NULL)),"
        // Only saved texts are kept formatted, always with HTML, which every program that takes formatting reads.
        + " CHECK (html IS NULL OR (saved = 1 AND kind = " + TextKind + ")),"
        + " CHECK (rtf IS NULL OR html IS NOT NULL));"
        + "CREATE INDEX entries_recent ON entries (saved, pinned DESC, copied_at DESC);"
        + "CREATE INDEX entries_hash ON entries (hash) WHERE hash IS NOT NULL;"
        + LookupIndexes;
    // What the list asks at every copy besides the newest entries: the programs of the sidebar with how many come from
    // each, the entries of a tab or of a program and how many, what the copied pictures take. Without these each
    // read the whole table: with 20,000 entries, 50 ms each (measured with "tests.exe many").
    const string LookupIndexes =
        "CREATE INDEX entries_category ON entries (saved, category, pinned DESC, copied_at DESC);"
        + "CREATE INDEX entries_app ON entries (saved, app_id, pinned DESC, copied_at DESC);"
        + "CREATE INDEX entries_kind ON entries (saved, kind, copied_at);";
    // The columns of the entries in versions 1 and 2, which an upgrade copies into the table of today; version 3
    // has the formatting too.
    const string OldFields = "id, kind, width, height, copied_at, saved, saved_at, name, name_key, folder_id, pinned, number,"
        + " hash, folded, preview, content";
    const string FormattingFields = ", rtf, html", ProgramFields = ", app_id", CategoryFields = ", category";   // 4 and 5
    static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    // The columns an entry is read from, in the order Read takes them. The content of a copied picture is left
    // out: it is read when it is chosen (Png).
    static readonly string EntryColumns ="id, kind, CASE WHEN kind = " + TextKind + " THEN content END, preview, width, height,"
        + " copied_at, saved, saved_at, name, folder_id, pinned, number, html, rtf, app_id, category, chosen";

    public const string Memory = ":memory:";   // a database that is not a file: when the file cannot be opened
    // Where the program keeps it; the checks use one of their own.
    public static string DefaultFile = System.IO.Path.Combine(Program.DataFolder, Program.AppName + ".db");

    readonly Database data;
    readonly string path;
    DateTime pruned;   // the day the expired entries were last removed
    // The previews of the copied pictures listed last, by row. The list is read again at every copy, and a picture
    // never changes: decoded anew each time, they would cost time and fill the memory with copies of themselves.
    Dictionary<long, Image> previews = new Dictionary<long, Image>();
    // The programs things were copied in: by the path of their executable, those described in this session; by their
    // row, as the list shows them, read once.
    readonly Dictionary<string, long> programs = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<long, Origin> origins = new Dictionary<long, Origin>();

    // Opens the file at 'path', creating it if it is not there.
    public Clips(string path) : this(path, null)
    {
    }

    // The same; a new database is filled by 'fill' (the migration) in the transaction that creates it. If that
    // fails, the database is not created at all: the next time it is new again, and 'fill' is tried again.
    public Clips(string path, Action<Clips> fill)
    {
        this.path = path;
        if (path != Memory) Directory.CreateDirectory(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(path)));
        data = new Database(path);
        try
        {
            // A removed entry is overwritten, not only forgotten; nothing is lost to a crash or a power cut. 8 MB of
            // pages kept in memory instead of 2: with thousands of entries the list, the sidebar and the room of the
            // pictures each read the whole table, at every copy, from the disk again (12 ms each; 2 ms so).
            data.Execute("PRAGMA foreign_keys = ON; PRAGMA secure_delete = ON; PRAGMA synchronous = FULL; PRAGMA journal_mode = DELETE;"
                + " PRAGMA cache_size = -8192");
            long version = (long)data.Value("PRAGMA user_version");
            if (version > SchemaVersion)
                throw new InvalidDataException(string.Format(Strings.NewerDatabase, path, Program.AppName));
            if (version == 0) Create(fill);
            else if (version < SchemaVersion) Upgrade(version);
        }
        catch (Exception)
        {
            data.Dispose();
            throw;
        }
        KeepDays = Texts.DefaultKeepDays;
    }

    void Create(Action<Clips> fill)
    {
        data.Execute("PRAGMA auto_vacuum = INCREMENTAL");   // only before the first table: see Compact
        using (Transaction transaction = data.Begin())
        {
            data.Execute(FoldersSchema + AppsSchema + EntriesSchema);
            data.Execute("PRAGMA user_version = " + SchemaVersion);
            if (fill != null) fill(this);
            transaction.Commit();
        }
        Created = true;
    }

    // A database written by an older version, brought up to date at once, all of it or nothing.
    void Upgrade(long version)
    {
        using (Transaction transaction = data.Begin())
        {
            // 1 → 2: the same entries, the large columns at the end. 2 → 3: the columns of the formatting, 3 → 4 the
            // program of a copied entry, 4 → 5 its category and 5 → 6 whether the user chose it (and the checks that let
            // a saved entry have one), before the large ones, which ADD COLUMN would put
            // after them. Either way the table is made anew under its name and filled from the old one, with the
            // indexes of today. 6 → 7 only adds indexes.
            if (version < 6)
            {
                string fields = version < 3 ? OldFields : version < 4 ? OldFields + FormattingFields
                    : version < 5 ? OldFields + FormattingFields + ProgramFields : OldFields + FormattingFields + ProgramFields + CategoryFields;
                if (version < 4) data.Execute(AppsSchema);
                data.Execute("DROP INDEX entries_recent; DROP INDEX entries_hash; ALTER TABLE entries RENAME TO entries_old;");
                data.Execute(EntriesSchema);
                data.Execute("INSERT INTO entries (" + fields + ") SELECT " + fields + " FROM entries_old; DROP TABLE entries_old;");
                if (version < 5) Categorize();
            }
            else if (version < 7) data.Execute(LookupIndexes);
            data.Execute("PRAGMA user_version = " + SchemaVersion);
            transaction.Commit();
        }
        Compact();   // the room of the old table back to the disk
    }

    // Says what each copied entry is, those of an older version that did not say it.
    void Categorize()
    {
        data.Execute("UPDATE entries SET category = ?1 WHERE saved = 0 AND kind = ?2", (int)Category.Picture, PictureKind);
        Dictionary<long, Category> found = new Dictionary<long, Category>();
        using (Statement rows = data.Prepare("SELECT id, content FROM entries WHERE saved = 0 AND kind = ?1", TextKind))
            while (rows.Step()) found[rows.Long(0)] = Texts.CategoryOf(Encoding.UTF8.GetString(rows.Blob(1)));
        using (Statement update = data.Prepare("UPDATE entries SET category = ?2 WHERE id = ?1"))
            foreach (KeyValuePair<long, Category> each in found) update.Run(each.Key, (int)each.Value);
    }

    // The database did not exist and was made now.
    public bool Created { get; private set; }

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

    // Whether SQLite finds the file sound, page by page.
    public bool Healthy()
    {
        return Equals(data.Value("PRAGMA quick_check"), "ok");
    }

    // Moves the database to 'target', where there is none yet: 'from' is closed, its file copied there, the copy
    // opened and checked, and only then is the old file deleted. If anything fails the copy goes, the old file is
    // opened again as it was, and the error is thrown on. Returns the database at its new place.
    public static Clips Move(Clips from, string target)
    {
        string source = from.FileName;
        int days = from.KeepDays;
        if (File.Exists(target)) throw new IOException("A database is there already: " + target);
        from.Dispose();
        Clips moved = null;
        try
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(target));
            File.Copy(source, target);
            moved = new Clips(target);
            if (!moved.Healthy()) throw new InvalidDataException("The copy is damaged: " + target);
        }
        catch (Exception ex)
        {
            if (moved != null) moved.Dispose();
            try
            {
                if (File.Exists(target)) File.Delete(target);
            }
            catch (Exception)
            {
                // A copy that cannot even be removed is left there: the old file is what counts.
            }
            Clips back = new Clips(source);
            back.KeepDays = days;
            throw new ClipsMoveException(back, ex);
        }
        moved.KeepDays = days;
        // What was copied there must not stay behind, unencrypted, in the old place. If the old file cannot go (a
        // sync tool holding it), the move is done all the same, and the log says so.
        try
        {
            File.Delete(source);
        }
        catch (IOException ex)
        {
            Log.Error(ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            Log.Error(ex);
        }
        return moved;
    }

    // Several changes that must happen together, or not at all: see Database.Begin.
    public Transaction Begin()
    {
        return data.Begin();
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
        ForgetHosts();
        Drop(DateTime.Today.AddDays(-KeepDays));
        TrimPictures();   // the room may have been made smaller, or the pictures come from an older version
    }

    // --- Room ---

    // How many bytes the copied pictures take, previews included. Their lengths are written at the start of
    // each row: the pictures themselves are not read.
    public long PictureBytes()
    {
        return (long)data.Value("SELECT coalesce(sum(length(content) + length(preview)), 0) FROM entries"
            + " WHERE saved = 0 AND kind = ?1", PictureKind);
    }

    // The copied pictures fill their room: one more as large as the largest of them would not fit, so the oldest
    // go as new ones come.
    public bool PicturesFull
    {
        get
        {
            using (Statement total = data.Prepare("SELECT coalesce(sum(size), 0), coalesce(max(size), 0) FROM (SELECT"
                + " length(content) + length(preview) AS size FROM entries WHERE saved = 0 AND kind = ?1)", PictureKind))
            {
                total.Step();
                return total.Long(1) > 0 && total.Long(0) + total.Long(1) > PictureSpace;
            }
        }
    }

    // Removes the oldest copied pictures until those left fit in PictureSpace, pinned or numbered as they may
    // be, as their days would. The saved ones are never counted nor touched. The file is not made smaller here: the
    // next picture takes the room back, and compacting would only move the last one into it, a second at times.
    void TrimPictures()
    {
        long over = PictureBytes() - PictureSpace;
        if (over <= 0) return;
        List<long> oldest = new List<long>();
        using (Statement rows = data.Prepare("SELECT id, length(content) + length(preview) FROM entries"
            + " WHERE saved = 0 AND kind = ?1 ORDER BY copied_at, id", PictureKind))
        {
            while (over > 0 && rows.Step())
            {
                oldest.Add(rows.Long(0));
                over -= rows.Long(1);
            }
        }
        using (Transaction transaction = data.Begin())
        using (Statement delete = data.Prepare("DELETE FROM entries WHERE id = ?1"))
        {
            foreach (long id in oldest) delete.Run(id);
            transaction.Commit();
        }
    }

    // How large the file of the database is, as SQLite counts it.
    public long Size
    {
        get { return (long)data.Value("PRAGMA page_count") * (long)data.Value("PRAGMA page_size"); }
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
        DropApps();
        Compact();
    }

    void Drop(DateTime oldest)
    {
        data.Execute("DELETE FROM entries WHERE saved = 0 AND copied_at < ?1", Stamp(oldest));
        DropApps();
        Compact();
    }

    // The clipboard service of Windows is never the program something was copied in (Origin.IsHost), but versions
    // before 4.3.3 could say so: those entries forget it, and DropApps the program, which the sidebar listed.
    void ForgetHosts()
    {
        List<long> hosts = new List<long>();
        using (Statement apps = data.Prepare("SELECT id, path FROM apps"))
            while (apps.Step())
                if (Origin.IsHost(apps.Text(1))) hosts.Add(apps.Long(0));
        foreach (long id in hosts) data.Execute("UPDATE entries SET app_id = NULL WHERE app_id = ?1", id);
    }

    // Forgets the programs no copied entry comes from any more. Their rows can then go to other programs: what this
    // session knows of them is forgotten too.
    void DropApps()
    {
        data.Execute("DELETE FROM apps WHERE id NOT IN (SELECT app_id FROM entries WHERE app_id IS NOT NULL)");
        if (data.Changes == 0) return;
        programs.Clear();
        origins.Clear();
    }

    // Gives back to the disk the room of what was removed, a little at a time instead of rebuilding the file.
    void Compact()
    {
        data.Execute("PRAGMA incremental_vacuum");
    }

    // --- Copied entries ---

    // Remembers a copied text. Copied again, it is the same entry, which only takes the new time: at the top,
    // with its arrow and its number. False if there is nothing worth keeping.
    public bool AddText(string text)
    {
        return AddText(text, null);
    }

    // The same, copied in the program whose executable is 'program' (null: nobody knows). Copied again elsewhere,
    // the entry comes from there.
    public bool AddText(string text, string program)
    {
        if (!Worth(text)) return false;
        if (pruned != DateTime.Today) Prune();
        byte[] content = Encoding.UTF8.GetBytes(text);
        Insert(TextKind, content, null, 0, 0, Texts.Fold(text), Hash(content), DateTime.Now, false, App(program));
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
        AddPicture(picture, null);
    }

    // The same, copied in the program whose executable is 'program' (null: nobody knows).
    public void AddPicture(Image picture, string program)
    {
        if (pruned != DateTime.Today) Prune();
        Entry entry = new Entry(picture);
        using (entry.Preview)
            Insert(PictureKind, entry.Png, Entry.ToPng(entry.Preview), entry.Width, entry.Height, PictureWords(entry.Width, entry.Height),
                Hash(entry.Png), DateTime.Now, false, App(program));
        TrimPictures();
    }

    // The row of the program whose executable is 'path' among the apps, made the first time it is met. Once per
    // session it is described again, so that a program updated since shows its name and icon of today. Null for none.
    object App(string path)
    {
        if (path == null) return null;
        long id;
        if (programs.TryGetValue(path, out id)) return id;
        string name = Origin.NameOf(path);
        byte[] icon = Origin.IconOf(path);
        object known = data.Value("SELECT id FROM apps WHERE path = ?1", path);
        if (known == null)
        {
            data.Execute("INSERT INTO apps (path, name, icon) VALUES (?1, ?2, ?3)", path, name, icon);
            id = data.LastId;
        }
        else
        {
            id = (long)known;
            data.Execute("UPDATE apps SET name = ?2, icon = ?3 WHERE id = ?1", id, name, icon);
            origins.Remove(id);
        }
        programs[path] = id;
        return id;
    }

    // Says in which program a copied entry was copied: for the entries the checks make up, which come from no
    // clipboard.
    internal void Attribute(long id, string program)
    {
        data.Execute("UPDATE entries SET app_id = ?2 WHERE id = ?1 AND saved = 0", id, App(program));
    }

    // The program of a row of the apps, as the list shows it: read once, its icon decoded once.
    Origin OriginOf(long id)
    {
        Origin origin;
        if (origins.TryGetValue(id, out origin)) return origin;
        using (Statement row = data.Prepare("SELECT name, icon FROM apps WHERE id = ?1", id))
        {
            if (!row.Step()) return null;
            Image icon = null;
            if (!row.IsNull(1))
            {
                try
                {
                    using (MemoryStream packed = new MemoryStream(row.Blob(1)))
                    using (Image image = Image.FromStream(packed))
                        icon = new Bitmap(image);
                }
                catch (ArgumentException)
                {
                    // Not a picture after all: the name alone.
                }
            }
            origin = new Origin(row.Text(0), icon);
        }
        origins[id] = origin;
        return origin;
    }

    // What the search finds a picture by: its size, as the list writes it.
    static string PictureWords(int width, int height)
    {
        return Texts.Fold(string.Format(Strings.PictureLabel, width, height));
    }

    // Puts back a copied text read from an exported file, at the time it was copied. False if it was left out:
    // nothing worth keeping, or there already, whatever its line ends (the file has its own).
    public bool Import(string text, DateTime time, bool pinned)
    {
        if (!Worth(text)) return false;
        string bare = text.Replace("\r\n", "\n");
        foreach (string each in new string[] { text, bare, bare.Replace("\n", "\r\n") })
            if (data.Value("SELECT 1 FROM entries WHERE hash = ?1", Hash(Encoding.UTF8.GetBytes(each))) != null) return false;
        if (pruned != DateTime.Today) Prune();
        byte[] content = Encoding.UTF8.GetBytes(text);
        Insert(TextKind, content, null, 0, 0, Texts.Fold(text), Hash(content), time, pinned, null);
        return true;
    }

    // A new copied entry, or the one with the same content taking the later time, and the program it was copied in
    // last if known ('app', a row of the apps, or null). Returns its id.
    long Insert(int kind, byte[] content, byte[] preview, int width, int height, string folded, byte[] hash, DateTime time, bool pinned,
        object app)
    {
        object same = data.Value("SELECT id FROM entries WHERE hash = ?1", hash);
        if (same != null)
        {
            data.Execute("UPDATE entries SET copied_at = max(copied_at, ?2), pinned = max(pinned, ?3), app_id = coalesce(?4, app_id)"
                + " WHERE id = ?1", same, Stamp(time), pinned, app);
            return (long)same;
        }
        Category category = kind == PictureKind ? Category.Picture : Texts.CategoryOf(Encoding.UTF8.GetString(content));
        data.Execute("INSERT INTO entries (kind, content, preview, width, height, folded, hash, copied_at, pinned, app_id, category)"
            + " VALUES (?1, ?2, ?3, ?4, ?5, ?6, ?7, ?8, ?9, ?10, ?11)",
            kind, content, preview, kind == PictureKind ? (object)width : null, kind == PictureKind ? (object)height : null,
            folded, hash, Stamp(time), pinned, app, (int)category);
        // SQLite may give the new row the id of the last one removed: a preview kept for that one is not this one's.
        previews.Remove(data.LastId);
        return data.LastId;
    }

    // The copied entries the list shows, those at the top first and then the newest, at most 'most'. 'words' are
    // those of a search, as Texts.Words gives them: every one must be there.
    public List<Entry> Copied(int most, string[] words)
    {
        Sieve sieve = new Sieve();
        sieve.Words = words;
        return Copied(most, sieve);
    }

    // The same, only those the sieve lets through.
    public List<Entry> Copied(int most, Sieve sieve)
    {
        List<object> values = new List<object>();
        StringBuilder sql = new StringBuilder("SELECT " + EntryColumns + " FROM entries WHERE " + Where(sieve, values));
        values.Add(most);
        sql.Append(" ORDER BY pinned DESC, copied_at DESC, id DESC LIMIT ?" + values.Count);
        List<Entry> entries = new List<Entry>();
        Dictionary<long, Image> listed = new Dictionary<long, Image>();
        using (Statement rows = data.Prepare(sql.ToString(), values.ToArray()))
            while (rows.Step())
            {
                Entry entry = Read(rows);
                entries.Add(entry);
                if (entry.Picture) listed[entry.Id] = entry.Preview;
            }
        previews = listed;   // those no longer listed are left to whoever still shows them
        return entries;
    }

    // How many copied entries there are, also beyond those the list shows.
    public int CopiedCount()
    {
        return CopiedCount(new Sieve());
    }

    // The same, those the sieve lets through.
    public int CopiedCount(Sieve sieve)
    {
        List<object> values = new List<object>();
        string where = Where(sieve, values);
        return (int)(long)data.Value("SELECT count(*) FROM entries WHERE " + where, values.ToArray());
    }

    // The condition of the copied entries a sieve lets through, its values added to 'values'.
    static string Where(Sieve sieve, List<object> values)
    {
        // With a number, through the index of the numbers, which only a few have: through that of the copied entries
        // SQLite would read them all.
        StringBuilder sql = new StringBuilder(sieve.Numbered ? "+saved = 0 AND number > 0" : "saved = 0");
        foreach (string word in sieve.Words)
        {
            values.Add(word);
            sql.Append(" AND instr(folded, ?" + values.Count + ") > 0");
        }
        if (sieve.Category.HasValue)
        {
            values.Add((int)sieve.Category.Value);
            sql.Append(" AND category = ?" + values.Count);
        }
        if (sieve.App != 0)
        {
            values.Add(sieve.App);
            sql.Append(" AND app_id = ?" + values.Count);
        }
        if (sieve.Pinned) sql.Append(" AND pinned = 1");
        return sql.ToString();
    }

    // The programs the copied entries come from, each with how many come from it: that of the latest copy first, and
    // so on (wanted by the user on 19/10/2026; they were by name).
    public List<Source> Sources()
    {
        List<Source> sources = new List<Source>();
        using (Statement rows = data.Prepare("SELECT app_id, count(*) FROM entries WHERE saved = 0 AND app_id IS NOT NULL"
            + " GROUP BY app_id ORDER BY max(copied_at) DESC, app_id DESC"))
            while (rows.Step())
            {
                Source source = new Source();
                source.App = rows.Long(0);
                source.Count = rows.Int(1);
                source.Origin = OriginOf(source.App);
                if (source.Origin != null) sources.Add(source);
            }
        return sources;
    }

    // Forgets a copied text, typed as a password and cleared from the clipboard: unless the user kept it at the
    // top or gave it a number.
    public void Forget(string text)
    {
        data.Execute("DELETE FROM entries WHERE hash = ?1 AND pinned = 0 AND number IS NULL", Hash(Encoding.UTF8.GetBytes(text)));
    }

    // Keeps a copied entry at the top of the list, or lets it go back among the others. It goes all the same
    // when its days are over.
    public void KeepAtTop(long id, bool pinned)
    {
        data.Execute("UPDATE entries SET pinned = ?2 WHERE id = ?1 AND saved = 0", id, pinned);
    }

    // --- Any entry ---

    // Says what a text is, whatever was guessed: copied again, or saved, it stays so.
    public void Choose(long id, Category category)
    {
        if (category == Category.Picture) throw new ArgumentException("A text is not a picture");
        data.Execute("UPDATE entries SET category = ?2, chosen = 1 WHERE id = ?1 AND kind = ?3", id, (int)category, TextKind);
        if (data.Changes == 0) throw new InvalidOperationException("No text " + id);
    }

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

    // The numbers some entry has, whatever its kind and wherever it is listed.
    public HashSet<int> Numbers()
    {
        HashSet<int> numbers = new HashSet<int>();
        using (Statement rows = data.Prepare("SELECT number FROM entries WHERE number IS NOT NULL"))
            while (rows.Step()) numbers.Add(rows.Int(0));
        return numbers;
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
                + " app_id = NULL, category = CASE WHEN chosen = 1 THEN category END, folded = NULL, hash = NULL, content = ?6, preview = ?7 WHERE id = ?1",
                id, Stamp(DateTime.Now), Name(name), Key(name), Place(folder), Protect(content), preview == null ? null : Protect(preview));
            transaction.Commit();
        }
    }

    // A new saved text, written by the user or read from an exported file ('time' DateTime.MinValue: nobody
    // knows when it was saved). Returns its id.
    public long AddSaved(string name, string text, long folder, DateTime time)
    {
        return AddSaved(name, text, null, null, folder, time);
    }

    // The same, formatted if 'html' is not null; 'rtf' only beside it.
    public long AddSaved(string name, string text, string html, string rtf, long folder, DateTime time)
    {
        bool dated = time != DateTime.MinValue;
        data.Execute("INSERT INTO entries (kind, content, copied_at, saved, saved_at, name, name_key, folder_id, html, rtf)"
            + " VALUES (?1, ?2, ?3, 1, ?4, ?5, ?6, ?7, ?8, ?9)",
            TextKind, Protect(Encoding.UTF8.GetBytes(text)), Stamp(dated ? time : DateTime.Now), dated ? (object)Stamp(time) : null,
            Name(name), Key(name), Place(folder), Secret(html), Secret(rtf));
        return data.LastId;
    }

    // Changes the name, the folder and, unless 'text' is null (a picture), the content of a saved entry: plain.
    public void Change(long id, string name, string text, long folder)
    {
        Change(id, name, text, null, null, folder);
    }

    // The same, the content formatted if 'html' is not null.
    public void Change(long id, string name, string text, string html, string rtf, long folder)
    {
        if (text == null)
            data.Execute("UPDATE entries SET name = ?2, name_key = ?3, folder_id = ?4 WHERE id = ?1 AND saved = 1",
                id, Name(name), Key(name), Place(folder));
        else
            data.Execute("UPDATE entries SET name = ?2, name_key = ?3, folder_id = ?4, content = ?5, html = ?7, rtf = ?8"
                + " WHERE id = ?1 AND saved = 1 AND kind = ?6",
                id, Name(name), Key(name), Place(folder), Protect(Encoding.UTF8.GetBytes(text)), TextKind, Secret(html), Secret(rtf));
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
        data.Execute("INSERT INTO folders (parent_id, name, name_key) VALUES (?1, ?2, ?3)", parent, Name(name), Key(name));
        return data.LastId;
    }

    // Moves a folder, with all it holds, into another (0: out of them all). Never into itself or one it holds;
    // refused if the name is taken there.
    public void MoveFolder(long id, long parent)
    {
        Refuse(id, parent);
        data.Execute("UPDATE folders SET parent_id = ?2 WHERE id = ?1", id, parent);
    }

    // Gives a folder another name and another place at once, checked as a whole: a name that would clash only
    // halfway, between the two changes, does not count.
    public void PlaceFolder(long id, long parent, string name)
    {
        Refuse(id, parent);
        data.Execute("UPDATE folders SET parent_id = ?2, name = ?3, name_key = ?4 WHERE id = ?1", id, parent, Name(name), Key(name));
    }

    void Refuse(long id, long parent)
    {
        for (long at = parent; at != 0; at = ParentOf(at))
            if (at == id) throw new ArgumentException("A folder cannot go inside itself");
    }

    // The folder at a path, the names from the outermost joined by Texts.Separator, or 0 if there is none.
    public long FindFolder(string path)
    {
        return Walk(path, false);
    }

    // The same, making the folders of the path that are missing.
    public long MakeFolder(string path)
    {
        return Walk(path, true);
    }

    long Walk(string path, bool make)
    {
        if (path == null) return 0;
        long at = 0;
        foreach (string name in path.Split(Texts.Separator))
        {
            object found = data.Value("SELECT id FROM folders WHERE parent_id = ?1 AND name_key = ?2", at, Key(name));
            if (found != null) at = (long)found;
            else if (make) at = AddFolder(at, name);
            else return 0;
        }
        return at;
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
            Dissolve(id, ParentOf(id));
            transaction.Commit();
        }
    }

    // Puts a folder inside 'parent'. If one there has its name already, that one takes in what it holds, the
    // folders inside it the same way, and it goes.
    void Join(long folder, long parent)
    {
        object same = data.Value("SELECT id FROM folders WHERE parent_id = ?1 AND id <> ?2"
            + " AND name_key = (SELECT name_key FROM folders WHERE id = ?2)", parent, folder);
        if (same == null) data.Execute("UPDATE folders SET parent_id = ?2 WHERE id = ?1", folder, parent);
        else Dissolve(folder, (long)same);
    }

    // Deletes a folder, what it holds going into 'into' (0: out of them all), its folders joining those there.
    void Dissolve(long folder, long into)
    {
        data.Execute("UPDATE entries SET folder_id = ?2 WHERE folder_id = ?1", folder, Place(into));
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

    // --- The migration: entries as the versions before the database kept them (see Migration) ---

    // A copied text with its time, its star and its number. Nothing is pruned meanwhile: the days of the settings
    // apply once the program runs.
    internal void AdoptText(string text, DateTime time, bool pinned, int number)
    {
        if (!Worth(text)) return;
        byte[] content = Encoding.UTF8.GetBytes(text);
        Adopted(Insert(TextKind, content, null, 0, 0, Texts.Fold(text), Hash(content), time, pinned, null), number);
    }

    internal void AdoptPicture(byte[] png, byte[] preview, int width, int height, DateTime time, bool pinned, int number)
    {
        Adopted(Insert(PictureKind, png, preview, width, height, PictureWords(width, height), Hash(png), time, pinned, null), number);
    }

    // A saved text, its content still encrypted as the registry held it: it is not even read, so one that cannot
    // be read here (another Windows user's) comes along too. A name taken already, whatever its letter case,
    // gets a number after it. Returns its id.
    internal long AdoptSaved(string name, byte[] secret, long folder, DateTime time, int number)
    {
        string sql = "INSERT INTO entries (kind, content, copied_at, saved, saved_at, name, name_key, folder_id)"
            + " VALUES (?1, ?2, ?3, 1, ?4, ?5, ?6, ?7)";
        bool dated = time != DateTime.MinValue;
        for (int copy = 1; ; copy++)
        {
            string given = copy == 1 ? name : name + " (" + copy + ")";
            if (data.Value("SELECT 1 FROM entries WHERE name_key = ?1", Key(given)) != null) continue;
            data.Execute(sql, TextKind, secret, Stamp(dated ? time : DateTime.Now), dated ? (object)Stamp(time) : null,
                Name(given), Key(given), Place(folder));
            long id = data.LastId;
            Adopted(id, number);
            return id;
        }
    }

    // A number goes to the first entry that had it: the old places could disagree.
    void Adopted(long id, int number)
    {
        if (number < 1 || number > MainForm.MaxNumber || data.Value("SELECT 1 FROM entries WHERE number = ?1", number) != null) return;
        data.Execute("UPDATE entries SET number = ?2 WHERE id = ?1", id, number);
    }

    // --- Rows and values ---

    // An entry from a row of EntryColumns. The whole of a picture is read only when it is asked for.
    Entry Read(Statement row)
    {
        long id = row.Long(0);
        bool saved = row.Bool(7);
        string name = row.Text(9);
        DateTime time = saved ? (row.IsNull(8) ? DateTime.MinValue : Moment(row.Long(8))) : Moment(row.Long(6));
        Entry entry;
        if (row.Int(1) == PictureKind)
        {
            Image preview;
            if (saved || !previews.TryGetValue(id, out preview))
            {
                // Copied into a bitmap of its own, which does not need the stream to stay open.
                using (MemoryStream packed = new MemoryStream(saved ? Unprotect(row.Blob(3)) : row.Blob(3)))
                using (Image image = Image.FromStream(packed))
                    preview = new Bitmap(image);
            }
            entry = new Entry(preview, row.Int(4), row.Int(5), time, name);
            entry.PngSource = delegate { return Png(id); };
        }
        else
        {
            byte[] content = row.Blob(2);
            entry = new Entry(name, Encoding.UTF8.GetString(saved ? Unprotect(content) : content), time);
            entry.Html = Revealed(row.Blob(13));
            entry.Rtf = Revealed(row.Blob(14));
        }
        entry.Id = id;
        entry.Saved = saved;
        entry.FolderId = row.IsNull(10) ? 0 : row.Long(10);
        entry.Pinned = row.Bool(11);
        entry.Number = row.IsNull(12) ? 0 : row.Int(12);
        entry.Origin = row.IsNull(15) ? null : OriginOf(row.Long(15));
        if (!row.IsNull(16)) entry.Category = (Category)row.Int(16);   // of a saved one, only if chosen: else guessed
        entry.Chosen = row.Bool(17);
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

    // A text that may be missing, as a saved entry keeps it: encrypted, or NULL.
    static object Secret(string text)
    {
        return text == null ? null : Protect(Encoding.UTF8.GetBytes(text));
    }

    static string Revealed(byte[] secret)
    {
        return secret == null ? null : Encoding.UTF8.GetString(Unprotect(secret));
    }
}

// Which copied entries the list asks for: the words of a search (as Texts.Words gives them, every one must be there)
// and what the sidebar and the tabs above the list chose.
class Sieve
{
    public string[] Words = new string[0];
    public Category? Category;   // null: of any category
    public long App;             // the row of the program they come from among the apps; 0: any
    public bool Pinned;          // only those kept at the top
    public bool Numbered;        // only those with a number
}

// A program copied entries come from, as the sidebar lists it.
class Source
{
    public long App;
    public Origin Origin;
    public int Count;
}

// A move of the database that did not happen (Clips.Move): the reason, and the database back at its old place.
class ClipsMoveException : Exception
{
    public readonly Clips Back;

    public ClipsMoveException(Clips back, Exception reason) : base(reason.Message, reason)
    {
        Back = back;
    }
}
