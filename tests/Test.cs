using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

class ShotForm : MainForm
{
    protected override bool ShowWithoutActivation { get { return true; } }
}

class ShotEdit : EditForm
{
    public ShotEdit(Entry source, string group, List<string> groups) : base(source, group, groups) { }
    protected override bool ShowWithoutActivation { get { return true; } }
}

// The made-up entries of the pictures, in the language of the pictures: a list in Italian with English entries
// would not look like anybody's.
class Sample
{
    public string Signature, SignatureText, Work, Address, Servers, TestServer, Home, Wifi, Long, TwoLines, Remote;
    public string Probe;   // a word found only in the name of the test server, for a search

    public static Sample Current
    {
        get
        {
            Sample s = new Sample();
            switch (Strings.Code)
            {
                case "it":
                    s.Signature = "Firma email"; s.SignatureText = "Cordiali saluti,\nMario Rossi\n\nUfficio tecnico\nTel. 02 1234567";
                    s.Work = "Lavoro"; s.Address = "Indirizzo ufficio"; s.Servers = "Server";
                    s.TestServer = "Accesso al server di prova"; s.Home = "Casa"; s.Wifi = "Wi-Fi di casa";
                    s.Long = "Una frase copiata più lunga della finestra, che finisce tagliata sul bordo destro con i puntini";
                    s.TwoLines = "prima riga\r\nseconda riga"; s.Remote = "srv-database-01 - Connessione Desktop remoto"; s.Probe = "prova";
                    break;
                case "es":
                    s.Signature = "Firma de correo"; s.SignatureText = "Saludos cordiales,\nMaría García\n\nOficina técnica\nTel. 91 123 45 67";
                    s.Work = "Trabajo"; s.Address = "Dirección de la oficina"; s.Servers = "Servidores";
                    s.TestServer = "Acceso al servidor de pruebas"; s.Home = "Casa"; s.Wifi = "Wi-Fi de casa";
                    s.Long = "Una frase copiada más larga que la ventana, que termina cortada en el borde derecho con puntos suspensivos";
                    s.TwoLines = "primera línea\r\nsegunda línea"; s.Remote = "srv-database-01 - Conexión a Escritorio remoto"; s.Probe = "pruebas";
                    break;
                case "fr":
                    s.Signature = "Signature e-mail"; s.SignatureText = "Cordialement,\nMarie Dupont\n\nBureau technique\nTél. 01 23 45 67 89";
                    s.Work = "Travail"; s.Address = "Adresse du bureau"; s.Servers = "Serveurs";
                    s.TestServer = "Accès au serveur de test"; s.Home = "Maison"; s.Wifi = "Wi-Fi de la maison";
                    s.Long = "Une phrase copiée plus longue que la fenêtre, coupée au bord droit par des points de suspension";
                    s.TwoLines = "première ligne\r\ndeuxième ligne"; s.Remote = "srv-database-01 - Connexion Bureau à distance"; s.Probe = "test";
                    break;
                default:
                    s.Signature = "Email signature"; s.SignatureText = "Kind regards,\nJohn Smith\n\nTechnical office\nTel. 020 7946 0958";
                    s.Work = "Work"; s.Address = "Office address"; s.Servers = "Servers";
                    s.TestServer = "Test server access"; s.Home = "Home"; s.Wifi = "Home Wi-Fi";
                    s.Long = "A copied sentence longer than the window, which ends cut short at the right edge with an ellipsis";
                    s.TwoLines = "first line\r\nsecond line"; s.Remote = "srv-database-01 - Remote Desktop Connection"; s.Probe = "test";
                    break;
            }
            return s;
        }
    }
}

// The plain background the Store pictures are taken on: the colours of the icon, without text or logos.
class Backdrop : Form
{
    public Backdrop(Rectangle area)
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        Bounds = area;
        TopMost = true;   // above the taskbar too, where the screen is small
    }

    protected override bool ShowWithoutActivation { get { return true; } }

    // The teal of the icon: lighter behind light windows, deeper behind dark ones. Read as it paints, so it follows
    // a change of theme.
    protected override void OnPaintBackground(PaintEventArgs e)
    {
        using (System.Drawing.Drawing2D.LinearGradientBrush brush = Look.Dark
            ? new System.Drawing.Drawing2D.LinearGradientBrush(ClientRectangle, Color.FromArgb(14, 52, 58), Color.FromArgb(34, 78, 74), 35f)
            : new System.Drawing.Drawing2D.LinearGradientBrush(ClientRectangle, Color.FromArgb(150, 196, 200), Color.FromArgb(208, 224, 214), 35f))
        {
            e.Graphics.FillRectangle(brush, ClientRectangle);
        }
    }
}

class ShotHelp : HelpForm
{
    protected override bool ShowWithoutActivation { get { return true; } }
}

static class TestProgram
{
    [DllImport("user32.dll")]
    static extern int GetKeyboardLayoutList(int count, [Out] IntPtr[] list);

    [STAThread]
    static int Main(string[] args)
    {
        Native.SetProcessDPIAware();
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        // Never what belongs to the user: settings under a key of their own; the database, the old history and the
        // log in a folder of their own. Both are emptied before and removed after.
        sandbox = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "SmartClip test " + Guid.NewGuid());
        MainForm.SettingsKey = TestKey;
        Clips.DefaultFile = System.IO.Path.Combine(sandbox, Program.AppName + ".db");
        Migration.OldHistory = System.IO.Path.Combine(sandbox, "History");
        Log.FileName = Log.FileIn(sandbox);
        Microsoft.Win32.Registry.CurrentUser.DeleteSubKeyTree(TestKey, false);
        try
        {
            return Run(args);
        }
        finally
        {
            Microsoft.Win32.Registry.CurrentUser.DeleteSubKeyTree(TestKey, false);
            if (System.IO.Directory.Exists(sandbox)) System.IO.Directory.Delete(sandbox, true);
        }
    }

    const string TestKey = @"Software\" + Program.AppName + " test";
    static string sandbox;   // the folder of the checks, removed at the end

    static int Run(string[] args)
    {
        string mode = args.Length > 0 ? args[0] : "";
        // The pictures can be taken in any language: "shot <png> it". Everything else runs in English.
        if (args.Length > 2 && (mode == "shot" || mode == "real" || mode == "store")) Strings.Use(args[2]);
        // And in either theme, whatever Windows has: "shot <png> it dark".
        if (Array.IndexOf(args, "dark") > 0) Look.Use(true);
        else if (Array.IndexOf(args, "light") > 0) Look.Use(false);
        if (mode == "map") return MapTest();
        if (mode == "shot") return Shot(args[1]);
        if (mode == "type") return TypeTest();
        if (mode == "texts") return TextsTest();
        if (mode == "data") return DataTest();
        if (mode == "real") return Real(args[1]);
        if (mode == "store") return Store(args[1]);
        Console.WriteLine("usage: map | texts | data | shot <png> [lang] | real <png> [lang] | store <folder> [lang] | type");
        return 64;
    }

    static int failed;

    static void Check(bool ok, string what)
    {
        Console.WriteLine("[{0}] {1}", ok ? "PASS" : "FAIL", what);
        if (!ok) failed++;
    }

    static DataObject Copied(string format, int value)
    {
        DataObject data = new DataObject();
        data.SetData(DataFormats.UnicodeText, "secret");
        if (format != null) data.SetData(format, new System.IO.MemoryStream(BitConverter.GetBytes(value)));
        return data;
    }

    // The database, from SQLite up, in files of the sandbox.
    static int DataTest()
    {
        SqliteTest();
        ClipsTest();
        RoomTest();
        UpgradeTest();
        FormattedTest();
        MigrationTest();
        BackupTest();
        MoveTest();
        return failed == 0 ? 0 : 1;
    }

    // A formatted saved text: its HTML and RTF kept encrypted beside its plain text, changed or taken away with it,
    // and imported from an exported file; never on a copied entry.
    static void FormattedTest()
    {
        string path = System.IO.Path.Combine(sandbox, "formatted test.db"), file = System.IO.Path.Combine(sandbox, "formatted test.txt");
        using (Clips clips = new Clips(path))
        {
            long id = clips.AddSaved("Firma", "Sai Chavan", "<b>Sai Chavan</b>", @"{\rtf1 \b Sai Chavan}", 0, DateTime.Now);
            Entry saved = clips.Saved()[0];
            Check(saved.Html == "<b>Sai Chavan</b>" && saved.Rtf == @"{\rtf1 \b Sai Chavan}" && saved.Text == "Sai Chavan",
                "a formatted text comes back with its HTML and RTF");
            byte[] html = (byte[])clips.Data.Value("SELECT html FROM entries WHERE id = ?1", id);
            byte[] rtf = (byte[])clips.Data.Value("SELECT rtf FROM entries WHERE id = ?1", id);
            Check(Encoding.UTF8.GetString(html).IndexOf("Chavan") < 0 && Encoding.UTF8.GetString(rtf).IndexOf("Chavan") < 0, "both encrypted");
            clips.Change(id, "Firma", "Sai", "<i>Sai</i>", null, 0);
            saved = clips.Saved()[0];
            Check(saved.Html == "<i>Sai</i>" && saved.Rtf == null && saved.Text == "Sai", "changed, it takes the new HTML, and no RTF");
            clips.Change(id, "Firma", "Sai", 0);
            Check(clips.Saved()[0].Html == null, "saved plain, it loses its formatting");
            clips.AddText("copied");
            Check(Refuses(clips, "UPDATE entries SET html = x'00' WHERE saved = 0"), "a copied entry is never formatted");
            Check(Refuses(clips, "UPDATE entries SET rtf = x'00' WHERE saved = 1"), "RTF only beside HTML");
            Check(clips.Copied(10, new string[0])[0].Html == null, "copied entries are read without formatting");

            Entry formatted = new Entry("Formattata", "testo", DateTime.Now);
            formatted.Saved = true;
            formatted.Html = "<u>testo</u>";
            System.IO.File.WriteAllLines(file, Backup.Lines(new List<Entry>(new Entry[] { formatted })).ToArray(), new UTF8Encoding(true));
            Backup.Import(clips, file, false, delegate { return false; });
            Entry imported = clips.Saved().Find(delegate(Entry e) { return e.Name == "Formattata"; });
            Check(imported != null && imported.Html == "<u>testo</u>" && imported.Rtf == null && imported.Text == "testo",
                "an exported formatted text comes back formatted");
        }
        System.IO.File.Delete(path);
    }

    // A picture of noise, which PNG cannot shrink: pictures of the same size take about the same room.
    static Bitmap Noise(int width, int seed)
    {
        Random random = new Random(seed);
        Bitmap picture = new Bitmap(width, 40);
        for (int x = 0; x < picture.Width; x++)
            for (int y = 0; y < picture.Height; y++)
                picture.SetPixel(x, y, Color.FromArgb(random.Next(256), random.Next(256), random.Next(256)));
        return picture;
    }

    // The room of the copied pictures: beyond it the oldest go, the saved ones are neither counted nor touched, and
    // the list is told when it is full.
    static void RoomTest()
    {
        string path = System.IO.Path.Combine(sandbox, "room test.db");
        long space = Clips.PictureSpace;
        try
        {
            using (Clips clips = new Clips(path))
            {
                using (Bitmap picture = Noise(60, 60)) clips.AddPicture(picture);
                long kept = clips.Copied(10, new string[0])[0].Id;
                long one = clips.PictureBytes();
                clips.Save(kept, "kept", 0);
                Check(clips.PictureBytes() == 0 && !clips.PicturesFull, "a saved picture takes none of the room of the copied ones");
                Clips.PictureSpace = one * 7 / 2;   // room for three
                List<long> added = new List<long>();
                for (int i = 0; i < 5; i++)
                {
                    using (Bitmap picture = Noise(60, i)) clips.AddPicture(picture);
                    added.Add(clips.Copied(1, new string[0])[0].Id);
                    System.Threading.Thread.Sleep(5);   // one after the other, also by the clock
                }
                List<Entry> left = clips.Copied(10, new string[0]);
                Check(left.Count == 3 && left[0].Id == added[4] && left[2].Id == added[2] && clips.PictureBytes() <= Clips.PictureSpace,
                    "beyond their room the oldest copied pictures go, the newest stay");
                Check(clips.Saved().Exists(delegate(Entry e) { return e.Id == kept && e.Picture; }), "the saved picture stays");
                Check(clips.PicturesFull, "and the list is told the room is full");
                clips.Clean(0);
                Check(clips.PictureBytes() == 0 && !clips.PicturesFull && clips.Size > 0, "emptied, it is not full any more");
            }
        }
        finally
        {
            Clips.PictureSpace = space;
            System.IO.File.Delete(path);
        }
    }

    // A database of version 1, with the large columns in the middle of the rows, is brought up to date when it is
    // opened: the same entries, the large columns at the end.
    static void UpgradeTest()
    {
        string path = System.IO.Path.Combine(sandbox, "upgrade test.db");
        List<string> before = new List<string>();
        byte[] png;
        long pictureId;
        using (Clips clips = new Clips(path))
        {
            clips.AddText("copied text");
            using (Bitmap picture = Noise(30, 7)) clips.AddPicture(picture);
            pictureId = clips.Copied(10, new string[0])[0].Id;
            clips.KeepAtTop(pictureId, true);
            clips.Number(pictureId, 3);
            png = clips.Png(pictureId);
            clips.AddSaved("Bank", "secret", clips.MakeFolder("Work"), DateTime.Now);
            clips.AddSaved(null, "without a name", 0, DateTime.Now);
            foreach (Entry e in clips.Copied(10, new string[0])) before.Add(Describe(e));
            foreach (Entry e in clips.Saved()) before.Add(Describe(e));
            // Back to the layout of version 1.
            clips.Data.Execute("DROP INDEX entries_recent; DROP INDEX entries_hash; ALTER TABLE entries RENAME TO entries_new;"
                + "CREATE TABLE entries (id INTEGER PRIMARY KEY, kind INTEGER NOT NULL CHECK (kind IN (1, 2)),"
                + " content BLOB NOT NULL, preview BLOB, width INTEGER, height INTEGER, folded TEXT, hash BLOB,"
                + " copied_at INTEGER NOT NULL, saved INTEGER NOT NULL DEFAULT 0 CHECK (saved IN (0, 1)), saved_at INTEGER,"
                + " name TEXT, name_key TEXT UNIQUE, folder_id INTEGER REFERENCES folders (id),"
                + " pinned INTEGER NOT NULL DEFAULT 0 CHECK (pinned IN (0, 1)), number INTEGER UNIQUE CHECK (number BETWEEN 1 AND 9),"
                + " CHECK (saved = 1 OR (name IS NULL AND folder_id IS NULL AND saved_at IS NULL)),"
                + " CHECK (saved = 0 OR (pinned = 0 AND folded IS NULL AND hash IS NULL)),"
                + " CHECK ((name IS NULL) = (name_key IS NULL)), CHECK ((kind = 2) = (preview IS NOT NULL)));"
                + "INSERT INTO entries SELECT id, kind, content, preview, width, height, folded, hash, copied_at, saved, saved_at,"
                + " name, name_key, folder_id, pinned, number FROM entries_new; DROP TABLE entries_new; DROP TABLE apps;"
                + "CREATE INDEX entries_recent ON entries (saved, pinned DESC, copied_at DESC);"
                + "CREATE INDEX entries_hash ON entries (hash) WHERE hash IS NOT NULL; PRAGMA user_version = 1;");
        }
        using (Clips clips = new Clips(path))
        {
            List<string> after = new List<string>();
            foreach (Entry e in clips.Copied(10, new string[0])) after.Add(Describe(e));
            foreach (Entry e in clips.Saved()) after.Add(Describe(e));
            List<string> columns = new List<string>();
            using (Statement rows = clips.Data.Prepare("PRAGMA table_info(entries)"))
                while (rows.Step()) columns.Add(rows.Text(1));
            Check(Equals(clips.Data.Value("PRAGMA user_version"), (long)Clips.SchemaVersion) && columns[columns.Count - 1] == "content"
                && columns[columns.Count - 2] == "preview" && columns.IndexOf("html") >= 0 && columns.IndexOf("html") < columns.IndexOf("hash")
                && columns.IndexOf("app_id") >= 0 && columns.IndexOf("app_id") < columns.IndexOf("hash")
                && Equals(clips.Data.Value("SELECT count(*) FROM apps"), 0L)
                && Equals(clips.Data.Value("SELECT count(*) FROM entries WHERE saved = 0 AND category IS NULL"), 0L)
                && Equals(clips.Data.Value("SELECT category FROM entries WHERE id = ?1", pictureId), (long)Category.Picture),
                "a database of version 1 is brought up to date, the large columns last, formatting and program before them,"
                + " and what each copied entry is");
            Check(string.Join("|", after.ToArray()) == string.Join("|", before.ToArray()) && after.Count == 4
                && System.Linq.Enumerable.SequenceEqual(clips.Png(pictureId), png), "with all its entries, as they were");
            Check(Equals(clips.Data.Value("SELECT count(*) FROM sqlite_master WHERE name IN ('entries_recent', 'entries_hash')"), 2L)
                && clips.Healthy(), "its indexes, and sound");
        }
        System.IO.File.Delete(path);

        // Version 3, the one before the programs: its formatted saved texts come along.
        using (Clips clips = new Clips(path))
        {
            clips.AddText("copied text");
            clips.AddSaved("Firma", "Sai", "<b>Sai</b>", @"{\rtf1 \b Sai}", 0, DateTime.Now);
            before.Clear();
            foreach (Entry e in clips.Copied(10, new string[0])) before.Add(Describe(e));
            foreach (Entry e in clips.Saved()) before.Add(Describe(e) + e.Html + e.Rtf);
            clips.Data.Execute("DROP INDEX entries_recent; DROP INDEX entries_hash; ALTER TABLE entries RENAME TO entries_new;"
                + "CREATE TABLE entries (id INTEGER PRIMARY KEY, kind INTEGER NOT NULL CHECK (kind IN (1, 2)), width INTEGER, height INTEGER,"
                + " copied_at INTEGER NOT NULL, saved INTEGER NOT NULL DEFAULT 0 CHECK (saved IN (0, 1)), saved_at INTEGER,"
                + " name TEXT, name_key TEXT UNIQUE, folder_id INTEGER REFERENCES folders (id),"
                + " pinned INTEGER NOT NULL DEFAULT 0 CHECK (pinned IN (0, 1)), number INTEGER UNIQUE CHECK (number BETWEEN 1 AND 9),"
                + " rtf BLOB, html BLOB, hash BLOB, folded TEXT, preview BLOB, content BLOB NOT NULL);"
                + "INSERT INTO entries SELECT id, kind, width, height, copied_at, saved, saved_at, name, name_key, folder_id, pinned, number,"
                + " rtf, html, hash, folded, preview, content FROM entries_new; DROP TABLE entries_new; DROP TABLE apps;"
                + "CREATE INDEX entries_recent ON entries (saved, pinned DESC, copied_at DESC);"
                + "CREATE INDEX entries_hash ON entries (hash) WHERE hash IS NOT NULL; PRAGMA user_version = 3;");
        }
        using (Clips clips = new Clips(path))
        {
            List<string> after = new List<string>();
            foreach (Entry e in clips.Copied(10, new string[0])) after.Add(Describe(e));
            foreach (Entry e in clips.Saved()) after.Add(Describe(e) + e.Html + e.Rtf);
            Check(Equals(clips.Data.Value("PRAGMA user_version"), (long)Clips.SchemaVersion)
                && string.Join("|", after.ToArray()) == string.Join("|", before.ToArray()) && after.Count == 2 && after[1].EndsWith(@"<b>Sai</b>{\rtf1 \b Sai}")
                && clips.Healthy(), "a database of version 3 is brought up to date, the formatting kept");
            Check(clips.AddText("now from somewhere", System.IO.Path.Combine(Environment.SystemDirectory, "notepad.exe"))
                && clips.Copied(1, new string[0])[0].Origin != null, "and knows the programs from then on");
        }
        System.IO.File.Delete(path);

        // Version 4, with the programs but not what each entry is: the programs stay, the categories come.
        using (Clips clips = new Clips(path))
        {
            string notepad = System.IO.Path.Combine(Environment.SystemDirectory, "notepad.exe");
            clips.AddText("int x = 1;\nreturn x;", notepad);
            clips.AddSaved("Firma", "Sai", "<b>Sai</b>", null, 0, DateTime.Now);
            clips.Data.Execute("DROP INDEX entries_recent; DROP INDEX entries_hash; ALTER TABLE entries RENAME TO entries_new;"
                + "CREATE TABLE entries (id INTEGER PRIMARY KEY, kind INTEGER NOT NULL CHECK (kind IN (1, 2)), width INTEGER, height INTEGER,"
                + " copied_at INTEGER NOT NULL, saved INTEGER NOT NULL DEFAULT 0 CHECK (saved IN (0, 1)), saved_at INTEGER,"
                + " name TEXT, name_key TEXT UNIQUE, folder_id INTEGER REFERENCES folders (id),"
                + " pinned INTEGER NOT NULL DEFAULT 0 CHECK (pinned IN (0, 1)), number INTEGER UNIQUE CHECK (number BETWEEN 1 AND 9),"
                + " app_id INTEGER REFERENCES apps (id), rtf BLOB, html BLOB, hash BLOB, folded TEXT, preview BLOB, content BLOB NOT NULL);"
                + "INSERT INTO entries SELECT id, kind, width, height, copied_at, saved, saved_at, name, name_key, folder_id, pinned, number,"
                + " app_id, rtf, html, hash, folded, preview, content FROM entries_new; DROP TABLE entries_new;"
                + "CREATE INDEX entries_recent ON entries (saved, pinned DESC, copied_at DESC);"
                + "CREATE INDEX entries_hash ON entries (hash) WHERE hash IS NOT NULL; PRAGMA user_version = 4;");
        }
        using (Clips clips = new Clips(path))
        {
            Sieve code = new Sieve();
            code.Category = Category.Code;
            List<Entry> found = clips.Copied(10, code);
            Check(Equals(clips.Data.Value("PRAGMA user_version"), (long)Clips.SchemaVersion) && found.Count == 1 && found[0].Origin != null
                && clips.Saved()[0].Html == "<b>Sai</b>" && clips.Healthy(),
                "a database of version 4 is brought up to date: its programs kept, what each entry is found");
        }
        System.IO.File.Delete(path);

        // Version 5, where only copied entries could say what they are: they keep it, and a saved text can be told.
        using (Clips clips = new Clips(path))
        {
            clips.AddText("int x = 1;\nreturn x;", null);
            clips.AddSaved("Firma", "Sai", 0, DateTime.Now);
            clips.Data.Execute("DROP INDEX entries_recent; DROP INDEX entries_hash; ALTER TABLE entries RENAME TO entries_new;"
                + "CREATE TABLE entries (id INTEGER PRIMARY KEY, kind INTEGER NOT NULL CHECK (kind IN (1, 2)), width INTEGER, height INTEGER,"
                + " copied_at INTEGER NOT NULL, saved INTEGER NOT NULL DEFAULT 0 CHECK (saved IN (0, 1)), saved_at INTEGER,"
                + " name TEXT, name_key TEXT UNIQUE, folder_id INTEGER REFERENCES folders (id),"
                + " pinned INTEGER NOT NULL DEFAULT 0 CHECK (pinned IN (0, 1)), number INTEGER UNIQUE CHECK (number BETWEEN 1 AND 9),"
                + " app_id INTEGER REFERENCES apps (id), category INTEGER CHECK (category BETWEEN 1 AND 4),"
                + " rtf BLOB, html BLOB, hash BLOB, folded TEXT, preview BLOB, content BLOB NOT NULL,"
                + " CHECK (saved = 0 OR category IS NULL));"
                + "INSERT INTO entries SELECT id, kind, width, height, copied_at, saved, saved_at, name, name_key, folder_id, pinned, number,"
                + " app_id, category, rtf, html, hash, folded, preview, content FROM entries_new; DROP TABLE entries_new;"
                + "CREATE INDEX entries_recent ON entries (saved, pinned DESC, copied_at DESC);"
                + "CREATE INDEX entries_hash ON entries (hash) WHERE hash IS NOT NULL; PRAGMA user_version = 5;");
        }
        using (Clips clips = new Clips(path))
        {
            Sieve code = new Sieve();
            code.Category = Category.Code;
            long saved = clips.Saved()[0].Id;
            clips.Choose(saved, Category.Link);
            Check(Equals(clips.Data.Value("PRAGMA user_version"), (long)Clips.SchemaVersion) && clips.Copied(10, code).Count == 1
                && clips.Saved()[0].Category == Category.Link && clips.Healthy(),
                "a database of version 5 is brought up to date: what the copied entries are kept, a saved text can be told");
        }
        System.IO.File.Delete(path);
    }

    static string Describe(Entry e)
    {
        return e.Id + "," + e.Picture + "," + e.Text + "," + e.Name + "," + e.Saved + "," + e.FolderId + "," + e.Pinned + ","
            + e.Number + "," + e.Width + "," + e.Time.Ticks;
    }

    // The database moved to another folder: all of it, the old file gone; never over another database; and when the
    // move fails halfway, the old one back as it was.
    static void MoveTest()
    {
        string first = System.IO.Path.Combine(sandbox, @"move\one\SmartClip.db"), second = System.IO.Path.Combine(sandbox, @"move\two\SmartClip.db");
        Clips clips = new Clips(first);
        try
        {
            clips.Keep(9);
            clips.AddText("copiato");
            long kept = clips.AddSaved("Banca", "segreta", clips.AddFolder(0, "Cartella"), DateTime.Now);
            Check(clips.Healthy(), "a sound database says so");
            clips = Clips.Move(clips, second);
            Check(clips.FileName == second && !System.IO.File.Exists(first) && clips.KeepDays == 9 && clips.CopiedCount() == 1
                && clips.Saved().Find(delegate(Entry e) { return e.Id == kept; }).Text == "segreta" && clips.Folders().Count == 1,
                "a database moves with all it holds, its days too, and leaves no copy behind");
            using (Clips other = new Clips(first)) other.AddText("un altro");
            Clips current = clips;
            Check(Throws<System.IO.IOException>(delegate { Clips.Move(current, first); }) && clips.CopiedCount() == 1,
                "it never goes over another database, and stays open where it is");
            System.IO.File.Delete(first);
            ClipsMoveException failure = null;
            try
            {
                Clips.Move(clips, sandbox + @"\move\no|such\SmartClip.db");   // a character no folder can have
            }
            catch (ClipsMoveException ex)
            {
                failure = ex;
            }
            if (failure != null) clips = failure.Back;
            Check(failure != null && clips.FileName == second && clips.CopiedCount() == 1 && clips.Healthy(),
                "a move that fails leaves the database where it was, as it was, and open");
        }
        finally
        {
            clips.Dispose();
        }
    }

    // An exported file taken back into a database: folders made as the file names them, the names already taken asked
    // about once, the copied texts at their time unless the list has them, those whose days are over gone at once.
    static void BackupTest()
    {
        string path = System.IO.Path.Combine(sandbox, "backup test.db"), file = System.IO.Path.Combine(sandbox, "backup test.txt");
        using (Clips clips = new Clips(path))
        {
            clips.Keep(30);
            long kept = clips.AddSaved("Firma", "vecchia", 0, DateTime.Now);
            clips.AddText("già qui");
            List<Entry> sent = new List<Entry>();
            Entry folder = new Entry(@"Lavoro\Server", "", DateTime.MinValue);
            folder.IsGroup = folder.Saved = true;
            Entry signature = new Entry("Firma", "nuova", DateTime.Now);
            signature.Saved = true;
            signature.Group = @"Lavoro\Server";
            Entry other = new Entry("Altro testo", "in una cartella che il file non elenca", DateTime.Now);
            other.Saved = true;
            other.Group = "Senza riga";
            Entry here = new Entry(null, "già qui", DateTime.Now.AddDays(-1)), fromFile = new Entry(null, "dal file", DateTime.Now.AddDays(-1));
            fromFile.Pinned = true;
            sent.AddRange(new Entry[] { folder, signature, other, here, fromFile, new Entry(null, "vecchissimo", DateTime.Now.AddDays(-100)) });
            System.IO.File.WriteAllLines(file, Backup.Lines(sent).ToArray(), new UTF8Encoding(true));

            int asked = 0;
            BackupResult result = Backup.Import(clips, file, true, delegate(int clashes) { asked = clashes; return false; });
            List<Entry> saved = clips.Saved();
            Check(asked == 1 && result.Kept == 1 && result.Saved == 1 && saved.Find(delegate(Entry e) { return e.Id == kept; }).Text == "vecchia",
                "a name already taken is asked about once, and left as it was if the answer is no");
            Check(clips.FindFolder(@"Lavoro\Server") != 0 && saved.Find(delegate(Entry e) { return e.Name == "Altro testo"; }).FolderId == clips.FindFolder("Senza riga"),
                "folders are made as the file names them");
            List<Entry> copied = clips.Copied(10, new string[0]);
            Check(result.Copied == 2 && copied.Count == 2 && copied[0].Text == "dal file" && copied[0].Pinned
                && !copied.Exists(delegate(Entry e) { return e.Text == "vecchissimo"; }),
                "copied texts come back with their arrow, not twice, and not beyond the days kept");
            result = Backup.Import(clips, file, false, delegate(int clashes) { asked = clashes; return true; });
            Entry replaced = clips.Saved().Find(delegate(Entry e) { return e.Name == "Firma"; });
            Check(asked == 2 && result.Saved == 2 && replaced.Id == kept && replaced.Text == "nuova" && replaced.FolderId == clips.FindFolder(@"Lavoro\Server")
                && clips.CopiedCount() == 2, "if the answer is yes, they take what the file has; the copied ones only when asked");
            Entry nameless = new Entry(null, "mail@second.example", DateTime.Now);
            nameless.Saved = true;
            System.IO.File.WriteAllLines(file, Backup.Lines(new List<Entry>(new Entry[] { nameless })).ToArray(), new UTF8Encoding(true));
            Backup.Import(clips, file, false, delegate { return false; });
            Backup.Import(clips, file, false, delegate { return false; });
            Check(clips.Saved().FindAll(delegate(Entry e) { return e.Name == null; }).Count == 1,
                "a saved text without a name comes in once, however many times the file is imported");
        }
    }

    // What a folder holds, byte by byte, and the values of a registry key with those of its subkeys: to see that
    // the migration leaves them as they were.
    static string Fingerprint(string folder, string key)
    {
        StringBuilder all = new StringBuilder();
        foreach (string file in System.IO.Directory.GetFiles(folder))
            all.Append(file).Append(Convert.ToBase64String(System.IO.File.ReadAllBytes(file)));
        Fingerprint(Microsoft.Win32.Registry.CurrentUser.OpenSubKey(key), all);
        return all.ToString();
    }

    static void Fingerprint(Microsoft.Win32.RegistryKey key, StringBuilder all)
    {
        if (key == null) return;
        using (key)
        {
            foreach (string name in key.GetValueNames())
            {
                object value = key.GetValue(name);
                all.Append(name).Append(value is byte[] ? Convert.ToBase64String((byte[])value) : Convert.ToString(value));
            }
            foreach (string sub in key.GetSubKeyNames()) Fingerprint(key.OpenSubKey(sub), all);
        }
    }

    // A saved text as the versions before the database kept it in the registry: encrypted under its name, and beside
    // it, under the same name, its day, its number, its folder. Each left out if null or 0.
    static void OldSaved(string savedKey, string name, byte[] secret, DateTime? day, int number, string place)
    {
        using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(savedKey)) key.SetValue(name, secret);
        if (day.HasValue)
            using (Microsoft.Win32.RegistryKey dates = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(savedKey + @"\Dates"))
                dates.SetValue(name, day.Value.Ticks, Microsoft.Win32.RegistryValueKind.QWord);
        if (number > 0)
            using (Microsoft.Win32.RegistryKey numbers = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(savedKey + @"\Numbers"))
                numbers.SetValue(name, number, Microsoft.Win32.RegistryValueKind.DWord);
        if (place != null)
            using (Microsoft.Win32.RegistryKey places = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(savedKey + @"\Places"))
                places.SetValue(name, place);
    }

    static byte[] Protected(string text)
    {
        return System.Security.Cryptography.ProtectedData.Protect(Encoding.UTF8.GetBytes(text), null,
            System.Security.Cryptography.DataProtectionScope.CurrentUser);
    }

    // A record of a history file of the versions before the database, in the file of its day: the kind (1 a text,
    // 2 a picture, 0 deleted; the star in the high bit and the number in the bits under it), the time in ticks, the
    // length, then the content.
    static void OldRecord(string folder, DateTime time, int kind, bool pinned, int number, byte[] content)
    {
        System.IO.Directory.CreateDirectory(folder);
        string file = System.IO.Path.Combine(folder, time.ToString("yyyy-MM-dd") + ".clips");
        using (System.IO.FileStream stream = new System.IO.FileStream(file, System.IO.FileMode.Append))
        using (System.IO.BinaryWriter writer = new System.IO.BinaryWriter(stream))
        {
            writer.Write((byte)(kind | (pinned ? 0x80 | number << 3 : 0)));
            writer.Write(time.Ticks);
            writer.Write(content.Length);
            writer.Write(content);
        }
    }

    // Of a picture: width, height, the length of the preview, then preview and picture as PNG.
    static byte[] OldPicture(Bitmap picture)
    {
        byte[] png = Entry.ToPng(picture), preview;
        using (Bitmap small = new Bitmap(picture, picture.Width / 2, picture.Height / 2)) preview = Entry.ToPng(small);
        using (System.IO.MemoryStream content = new System.IO.MemoryStream())
        using (System.IO.BinaryWriter writer = new System.IO.BinaryWriter(content))
        {
            writer.Write(picture.Width);
            writer.Write(picture.Height);
            writer.Write(preview.Length);
            writer.Write(preview);
            writer.Write(png);
            writer.Flush();
            return content.ToArray();
        }
    }

    // The data of the versions before the database, written as they wrote it, brought into a new one.
    static void MigrationTest()
    {
        string history = System.IO.Path.Combine(sandbox, "old history"), path = System.IO.Path.Combine(sandbox, "migrated.db");
        string savedKey = MainForm.SettingsKey + @"\Saved";
        {
            // Saved texts and folders: one folder only implied by the one inside it, one closed; a folder named
            // that is not there, a text without a day, one encrypted by another Windows user, a number two claim.
            using (Microsoft.Win32.RegistryKey folders = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(savedKey + @"\Folders"))
            {
                folders.SetValue(@"Lavoro\Server", 0, Microsoft.Win32.RegistryValueKind.DWord);
                folders.SetValue("Casa", 1, Microsoft.Win32.RegistryValueKind.DWord);
            }
            DateTime day = new DateTime(2025, 5, 6, 7, 8, 9);
            OldSaved(savedKey, "Banca", Protected("pässwörd €"), day, 2, null);
            OldSaved(savedKey, "Server prod", Protected("root:secret"), DateTime.Now, 0, @"Lavoro\Server");
            OldSaved(savedKey, "Wi-Fi", Protected("casa123"), DateTime.Now, 2, "Casa");
            OldSaved(savedKey, "Orfano", Protected("nessuna cartella"), DateTime.Now, 0, "Sparita");
            OldSaved(savedKey, "Senza data", Protected("vecchio"), null, 0, null);
            OldSaved(savedKey, "Illeggibile", new byte[] { 1, 2, 3 }, DateTime.Now, 0, null);

            // The history: today, last week and long ago (beyond the days kept by default), a star with a number,
            // a picture, an entry deleted, a record cut short, a file that cannot be read.
            DateTime now = DateTime.Today.AddHours(DateTime.Now.Hour).AddMinutes(DateTime.Now.Minute);
            OldRecord(history, now.AddSeconds(-30), 1, false, 0, Encoding.UTF8.GetBytes("copiato oggi"));
            OldRecord(history, now.AddSeconds(-20), 1, true, 7, Encoding.UTF8.GetBytes("in alto"));
            OldRecord(history, now.AddSeconds(-10), 0, false, 0, new byte["cancellato".Length]);   // deleted: zeros
            using (Bitmap picture = new Bitmap(30, 20))
            {
                picture.SetPixel(3, 3, Color.Green);
                OldRecord(history, now.AddSeconds(-40), 2, false, 0, OldPicture(picture));
            }
            OldRecord(history, DateTime.Now.AddDays(-6), 1, false, 0, Encoding.UTF8.GetBytes("settimana scorsa"));
            OldRecord(history, DateTime.Now.AddDays(-100), 1, false, 0, Encoding.UTF8.GetBytes("cento giorni fa"));
            string today = System.IO.Path.Combine(history, now.ToString("yyyy-MM-dd") + ".clips");
            using (System.IO.FileStream cut = new System.IO.FileStream(today, System.IO.FileMode.Append)) cut.Write(new byte[] { 1, 9, 9, 9 }, 0, 4);
            string locked = System.IO.Path.Combine(history, "2020-01-01.clips");
            string untouched = Fingerprint(history, savedKey);

            MigrationResult result = null;
            using (new System.IO.FileStream(locked, System.IO.FileMode.Create, System.IO.FileAccess.ReadWrite, System.IO.FileShare.None))
            using (Clips clips = new Clips(path, delegate(Clips fresh) { result = Migration.Run(fresh, MainForm.SettingsKey, history); }))
            {
                Check(clips.Created && result.Saved == 6 && result.Folders == 3 && result.Copied == 4 && result.Pictures == 1
                    && result.Unreadable == 1, "everything comes along, the file that cannot be read left out");
                List<Entry> saved = clips.Saved();
                Func<string, Entry> find = delegate(string name) { return saved.Find(delegate(Entry e) { return e.Name == name; }); };
                Check(find("Banca").Text == "pässwörd €" && find("Banca").Time == day, "a saved text, readable, with its day");
                Check(find("Senza data").Text == "vecchio" && find("Senza data").Time == DateTime.MinValue, "one without a day stays without");
                Check(saved.Count == 5 && Convert.ToInt64(clips.Data.Value("SELECT count(*) FROM entries WHERE saved = 1")) == 6,
                    "one that cannot be read here comes along all the same, still encrypted");
                List<Folder> folders = clips.Folders();
                Folder casa = folders.Find(delegate(Folder f) { return f.Name == "Casa"; });
                Folder lavoro = folders.Find(delegate(Folder f) { return f.Name == "Lavoro"; });
                Folder server = folders.Find(delegate(Folder f) { return f.Name == "Server"; });
                Check(folders.Count == 3 && casa.Open && lavoro.Open && !server.Open && server.ParentId == lavoro.Id && server.Level == 1,
                    "folders become a tree, with the one only implied, each open or closed as it was");
                Check(find("Server prod").FolderId == server.Id && find("Wi-Fi").FolderId == casa.Id && find("Orfano").FolderId == 0,
                    "saved texts go into their folders, into none if theirs is gone");
                Check(Convert.ToInt64(clips.Data.Value("SELECT count(*) FROM entries WHERE number = 2")) == 1, "a number claimed twice goes to one");

                List<Entry> copied = clips.Copied(MainForm.MaxShown, new string[0]);
                Entry top = copied.Find(delegate(Entry e) { return e.Text == "in alto"; });
                Check(top != null && top.Pinned && top.Number == 7 && copied[0] == top, "an entry with the old star is at the top, with its number");
                Check(copied.Exists(delegate(Entry e) { return e.Text == "cento giorni fa"; })
                    && copied.Exists(delegate(Entry e) { return e.Text == "settimana scorsa" && e.Time.Date == DateTime.Today.AddDays(-6); }),
                    "copied entries keep their days, also beyond those kept by default");
                Check(!copied.Exists(delegate(Entry e) { return e.Text == "cancellato"; }), "a deleted entry stays deleted");
                Entry picture = copied.Find(delegate(Entry e) { return e.Picture; });
                using (Image back = Image.FromStream(new System.IO.MemoryStream(clips.Png(picture.Id))))
                using (Bitmap pixels = new Bitmap(back))
                    Check(picture.Width == 30 && picture.Preview != null && pixels.GetPixel(3, 3).ToArgb() == Color.Green.ToArgb(),
                        "a picture comes along whole, with its preview");
            }
            System.IO.File.Delete(locked);   // made for the test, after the fingerprint
            Check(Fingerprint(history, savedKey) == untouched, "the registry and the history files are left exactly as they were");
            using (Clips clips = new Clips(path, delegate(Clips fresh) { Migration.Run(fresh, MainForm.SettingsKey, history); }))
                Check(!clips.Created && clips.Saved().Count == 5, "opened again, it is not migrated twice");

            // A migration that fails halfway leaves no database: the next start tries again.
            string again = System.IO.Path.Combine(sandbox, "again.db");
            Check(Throws<InvalidOperationException>(delegate
            {
                new Clips(again, delegate(Clips fresh)
                {
                    Migration.Run(fresh, MainForm.SettingsKey, history);
                    throw new InvalidOperationException("halfway");
                }).Dispose();
            }), "a failing migration says so");
            using (Clips clips = new Clips(again, delegate(Clips fresh) { result = Migration.Run(fresh, MainForm.SettingsKey, history); }))
                Check(clips.Created && result.Saved == 6 && clips.Saved().Count == 5, "and the next start migrates everything");
        }
    }

    // Whether the database refuses a statement written by hand: a rule of the schema at work.
    static bool Refuses(Clips clips, string sql)
    {
        try
        {
            clips.Data.Execute(sql);
            return false;
        }
        catch (SqliteException ex)
        {
            return ex.IsConstraint;
        }
    }

    // What the list keeps, in a database of the sandbox: the rules agreed for copied and saved entries.
    static void ClipsTest()
    {
        string path = System.IO.Path.Combine(sandbox, "clips test.db");
        using (Clips clips = new Clips(path))
        {
            Check(Equals(clips.Data.Value("PRAGMA user_version"), (long)Clips.SchemaVersion), "a new database gets the schema");

            // Copied texts.
            Check(clips.AddText("one") && clips.AddText("two"), "copied texts are remembered");
            Check(!clips.AddText(null) && !clips.AddText(" \r\n ") && !clips.AddText(new string('x', Texts.MaxLength + 1)),
                "nothing, blanks and huge texts are not");
            System.Threading.Thread.Sleep(5);
            clips.AddText("one");
            List<Entry> copied = clips.Copied(MainForm.MaxShown, new string[0]);
            Check(copied.Count == 2 && copied[0].Text == "one" && copied[1].Text == "two" && clips.CopiedCount() == 2,
                "copied again, it is the same entry, back at the top");
            Check(copied[0].Id > 0 && !copied[0].Saved && copied[0].Name == null && copied[0].Time.Date == DateTime.Today,
                "with its row, not saved, copied today");
            clips.AddText("Città di Torino\nseconda riga");
            Check(clips.Copied(10, Texts.Words("TORINO citta")).Count == 1 && clips.Copied(10, Texts.Words("torino milano")).Count == 0,
                "the search: words in any order, without case or accents, every one");
            Check(clips.Copied(1, new string[0]).Count == 1, "at most as many as asked");

            // At the top: first, and still temporary.
            long two = clips.Copied(10, Texts.Words("two"))[0].Id;
            clips.KeepAtTop(two, true);
            copied = clips.Copied(10, new string[0]);
            Check(copied[0].Id == two && copied[0].Pinned, "an entry kept at the top comes first");
            clips.AddText("two");
            Check(clips.CopiedCount() == 3 && clips.Copied(10, new string[0])[0].Pinned, "copied again, it keeps its star");

            // The program each entry was copied in, kept once for all its entries.
            string notepad = System.IO.Path.Combine(Environment.SystemDirectory, "notepad.exe"), explorer = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe");
            clips.AddText("written in notepad", notepad);
            clips.AddText("notepad again", notepad);
            Entry noted = clips.Copied(10, Texts.Words("written"))[0];
            Check(noted.Origin != null && noted.Origin.Name == Origin.NameOf(notepad) && noted.Origin.Icon != null
                && Equals(clips.Data.Value("SELECT count(*) FROM apps"), 1L), "a copied entry knows its program, with name and icon, kept once");
            Check(Origin.NameOf(notepad) != "notepad" && Origin.NameOf(@"C:\nowhere\made up.exe") == "made up",
                "a program goes by the description in its file, or else by the file's name");
            clips.AddText("written in notepad", explorer);
            clips.AddText("written in notepad", null);
            Check(clips.Copied(10, Texts.Words("written"))[0].Origin.Name == Origin.NameOf(explorer),
                "copied again elsewhere it comes from there; from nowhere known, it keeps the last");
            Check(clips.Copied(10, Texts.Words("two"))[0].Origin == null, "copied where nobody knows, it has none");
            Check(Refuses(clips, "UPDATE entries SET app_id = 999 WHERE id = " + noted.Id), "a program must be among the apps");
            clips.Save(noted.Id, null, 0);
            Check(clips.Data.Value("SELECT app_id FROM entries WHERE id = ?1", noted.Id) == null
                && Refuses(clips, "UPDATE entries SET app_id = (SELECT min(id) FROM apps) WHERE id = " + noted.Id),
                "a saved entry has none: it would say something of it");
            clips.Remove(noted.Id);
            clips.Remove(clips.Copied(10, Texts.Words("notepad again"))[0].Id);

            // What each entry is, for the tabs over the cards; and what the sidebar and the tabs let through.
            Check(Texts.CategoryOf("https://example.com/a?b=1") == Category.Link && Texts.CategoryOf("www.example.com and more") == Category.Text
                && Texts.CategoryOf("int x = 1;\nreturn x;") == Category.Code && Texts.CategoryOf("console.log(x);") == Category.Code
                && Texts.CategoryOf("SELECT * FROM entries") == Category.Code && Texts.CategoryOf("Ciao; ci vediamo domani") == Category.Text
                && Texts.CategoryOf("ssh admin@server") == Category.Text && Texts.CategoryOf("Dear Sai,\nthank you.\nBest") == Category.Text,
                "a text is a web address, a piece of code, or any other text");
            clips.AddText("for (int i = 0; i < 3; i++) {\n    go(i);\n}", notepad);
            clips.AddText("https://example.com/sieve", explorer);
            long notepadApp = (long)clips.Data.Value("SELECT id FROM apps WHERE path = ?1", notepad);
            Sieve code = new Sieve(), link = new Sieve(), fromNotepad = new Sieve(), atTop = new Sieve();
            code.Category = Category.Code;
            link.Category = Category.Link;
            fromNotepad.App = notepadApp;
            atTop.Pinned = true;
            Check(clips.Copied(10, code).Count == 1 && clips.Copied(10, code)[0].Category == Category.Code
                && clips.CopiedCount(link) == 1 && clips.Copied(10, fromNotepad).Count == 1 && clips.CopiedCount(atTop) == 1
                && clips.Copied(10, atTop)[0].Text == "two", "the copied entries of a category, of a program, those at the top");
            List<Source> sources = clips.Sources();
            Check(sources.Count == 2 && sources.TrueForAll(delegate(Source s) { return s.Count == 1 && s.Origin != null; }),
                "the programs the copied entries come from, each with how many");
            Check(sources[0].Origin.Name == Origin.NameOf(explorer), "the program of the latest copy first");
            clips.Remove(clips.Copied(1, code)[0].Id);
            clips.Remove(clips.Copied(1, link)[0].Id);
            // The programs a remote one shows (Citrix): its name, and the icon of their window, one for each icon.
            using (Form first = new Form(), second = new Form())
            {
                first.Icon = SystemIcons.Application;
                second.Icon = SystemIcons.Warning;
                string shown = Origin.Within(notepad, first.Handle), other = Origin.Within(notepad, second.Handle);
                clips.AddText("copied on the server", shown);
                clips.AddText("copied on the server too", other);
                clips.AddText("and once more", Origin.Within(notepad, first.Handle));
                List<Source> remote = clips.Sources().FindAll(delegate(Source s) { return s.Origin.Name == Origin.NameOf(notepad); });
                Check(shown != notepad && other != shown && remote.Count == 2 && remote[0].Count == 2 && remote[1].Count == 1
                    && remote[0].Origin.Icon != null && !Origin.IsHost(shown) && Origin.NameOf(shown) == Origin.NameOf(notepad)
                    && Origin.IconOf(shown) != null && !Equals(Convert.ToBase64String(Origin.IconOf(shown)), Convert.ToBase64String(Origin.IconOf(other))),
                    "a program shown from a server goes by the name of the one here, one for each icon of its windows");
                foreach (Entry made in clips.Copied(10, Texts.Words("server"))) clips.Remove(made.Id);
                clips.Remove(clips.Copied(1, Texts.Words("once more"))[0].Id);
            }
            // What versions before 4.3.3 said came from the clipboard service of Windows comes from no program.
            string host = System.IO.Path.Combine(Environment.SystemDirectory, "svchost.exe");
            clips.AddText("through the service", host);
            clips.Prune();
            Check(clips.Copied(10, Texts.Words("through the service"))[0].Origin == null
                && Convert.ToInt64(clips.Data.Value("SELECT count(*) FROM apps WHERE path = ?1", host)) == 0,
                "an entry said to come from the clipboard service of Windows forgets it, and the service goes");
            clips.Remove(clips.Copied(10, Texts.Words("through the service"))[0].Id);

            // What the user says a text is: it stays, copied again or saved; a saved text not told is guessed.
            clips.AddText("just words", null);
            long words = clips.Copied(1, Texts.Words("just words"))[0].Id;
            clips.Choose(words, Category.Code);
            clips.AddText("just words", null);
            Entry told = clips.Copied(10, code).Find(delegate(Entry e) { return e.Id == words; });
            Check(told != null && told.Chosen && told.Category == Category.Code, "a text told to be code is code, also copied again");
            clips.Save(words, null, 0);
            told = clips.Saved().Find(delegate(Entry e) { return e.Id == words; });
            Check(told.Category == Category.Code && told.Chosen, "and saved it stays so");
            long plain = clips.AddSaved("Plain", "int x = 1;\nreturn x;", 0, DateTime.Now);
            Entry guessed = clips.Saved().Find(delegate(Entry e) { return e.Id == plain; });
            Check(clips.Data.Value("SELECT category FROM entries WHERE id = ?1", plain) == null && guessed.Category == Category.Code
                && !guessed.Chosen, "a saved text nobody told about is guessed, and nothing of it is kept");
            clips.Choose(plain, Category.Text);
            Check(clips.Saved().Find(delegate(Entry e) { return e.Id == plain; }).Category == Category.Text
                && Throws<ArgumentException>(delegate { clips.Choose(plain, Category.Picture); })
                && Refuses(clips, "UPDATE entries SET chosen = 0 WHERE id = " + plain), "told, it is what it was told; never a picture");
            clips.Remove(words);
            clips.Remove(plain);

            // Pictures.
            using (Bitmap picture = new Bitmap(40, 30))
            {
                picture.SetPixel(1, 1, Color.Red);
                clips.AddPicture(picture);
                clips.AddPicture(picture);
            }
            Entry shot = clips.Copied(10, Texts.Words("picture 40")).Find(delegate(Entry e) { return e.Picture; });
            Check(shot != null && clips.CopiedCount() == 4 && shot.Preview != null && shot.Width == 40 && shot.Height == 30,
                "a picture announced twice is one entry, with its preview, found by its size");
            using (Image back = Image.FromStream(new System.IO.MemoryStream(clips.Png(shot.Id))))
            using (Bitmap pixels = new Bitmap(back))
                Check(back.Width == 40 && pixels.GetPixel(1, 1).ToArgb() == Color.Red.ToArgb(), "and comes back whole when chosen");

            // Numbers: one entry each, of either kind; a temporary one stays temporary.
            long one = clips.Copied(10, Texts.Words("one"))[0].Id;
            clips.Number(one, 3);
            Check(clips.Numbered(3) != null && clips.Numbered(3).Id == one && !clips.Numbered(3).Pinned,
                "a number chooses a copied entry, which does not get the star for it");
            clips.Number(two, 3);
            Check(clips.Numbered(3).Id == two && clips.Copied(10, Texts.Words("one"))[0].Number == 0, "given to another, it moves");
            clips.Number(two, 0);
            Check(clips.Numbered(3) == null, "zero takes it away");
            clips.Number(one, 4);
            clips.Forget("one");
            Check(clips.Copied(10, Texts.Words("one")).Count == 1, "a typed password is not forgotten if it has a number");
            clips.AddText("password");
            clips.Forget("password");
            Check(clips.Copied(10, Texts.Words("password")).Count == 0, "otherwise it is");

            // The rules of the schema, written around Clips: the database itself says no.
            Check(Refuses(clips, "INSERT INTO entries (kind, content, copied_at, name, name_key) VALUES (1, x'00', 0, 'n', 'N')"),
                "a copied entry cannot have a name");
            Check(Refuses(clips, "INSERT INTO entries (kind, content, copied_at, saved, pinned) VALUES (1, x'00', 0, 1, 1)"),
                "a saved one cannot be at the top");
            Check(Refuses(clips, "INSERT INTO entries (kind, content, copied_at, number) VALUES (1, x'00', 0, 10)"),
                "a number goes from 1 to " + MainForm.MaxNumber);
            Check(Refuses(clips, "INSERT INTO entries (kind, content, copied_at, number) VALUES (1, x'00', 0, 4)"), "and chooses one entry only");
            Check(Refuses(clips, "INSERT INTO entries (kind, content, copied_at, saved, folder_id) VALUES (1, x'00', 0, 1, 999)"),
                "an entry cannot be in a folder that is not there");
            Check(Refuses(clips, "INSERT INTO entries (kind, content, copied_at) VALUES (2, x'00', 0)"), "a picture has a preview");

            // Saved entries: never going by themselves, encrypted, with a name or without.
            const string secret = "pässwörd € ~` segreta";
            long bank = clips.AddSaved("Banca", secret, 0, DateTime.MinValue);
            clips.AddText("mail@second.example");
            long mail = clips.Copied(10, Texts.Words("mail@second"))[0].Id;
            clips.Save(mail, null, 0);
            List<Entry> saved = clips.Saved();
            Entry banked = saved.Find(delegate(Entry e) { return e.Id == bank; }), mailed = saved.Find(delegate(Entry e) { return e.Id == mail; });
            Check(banked != null && banked.Saved && banked.Name == "Banca" && banked.Text == secret && banked.Time == DateTime.MinValue,
                "a saved text comes back with its name, and without a day if nobody knows it");
            Check(mailed != null && mailed.Saved && mailed.Name == null && mailed.Label == "mail@second.example" && mailed.Time.Date == DateTime.Today,
                "a copied entry saved without a name shows its content");
            Check(clips.Copied(10, Texts.Words("mail@second")).Count == 0, "and is no longer among the copied ones");
            clips.AddText("x@second.example");
            clips.Save(clips.Copied(10, Texts.Words("x@second"))[0].Id, "", 0);
            Check(clips.Saved().FindAll(delegate(Entry e) { return e.Name == null; }).Count == 2, "more saved entries can be without a name");
            byte[] raw = (byte[])clips.Data.Value("SELECT content FROM entries WHERE id = ?1", bank);
            Check(Encoding.UTF8.GetString(raw).IndexOf("segreta") < 0 && Encoding.Unicode.GetString(raw).IndexOf("segreta") < 0
                && clips.Data.Value("SELECT folded FROM entries WHERE id = ?1", mail) == null
                && clips.Data.Value("SELECT hash FROM entries WHERE id = ?1", mail) == null,
                "nothing of a saved entry is readable in the file: content, search text, hash");
            Check(clips.Named("BANCA") == bank && clips.Named("other") == 0, "a name is found whatever its letter case");
            SqliteException taken = null;
            try
            {
                clips.AddSaved("banca", "x", 0, DateTime.Now);
            }
            catch (SqliteException ex)
            {
                taken = ex;
            }
            Check(taken != null && taken.IsConstraint, "a name taken, also in another letter case, is refused");
            clips.Number(bank, 5);
            clips.Change(bank, "Banca 2", "nuova", 0);
            Entry changed = clips.Saved().Find(delegate(Entry e) { return e.Id == bank; });
            Check(changed.Name == "Banca 2" && changed.Text == "nuova" && changed.Number == 5, "a saved text changes name and content, its number stays");
            clips.Change(bank, null, "nuova", 0);
            Check(clips.Saved().Find(delegate(Entry e) { return e.Id == bank; }).Name == null, "and can lose its name");

            using (Bitmap picture = new Bitmap(20, 10))
            {
                picture.SetPixel(2, 2, Color.Blue);
                clips.AddPicture(picture);
            }
            long kept = clips.Copied(10, new string[0]).Find(delegate(Entry e) { return e.Picture && e.Width == 20; }).Id;
            clips.Save(kept, "Schema", 0);
            Entry framed = clips.Saved().Find(delegate(Entry e) { return e.Id == kept; });
            using (Image back = Image.FromStream(new System.IO.MemoryStream(clips.Png(kept))))
            using (Bitmap pixels = new Bitmap(back))
                Check(framed.Picture && framed.Name == "Schema" && framed.Preview != null && pixels.GetPixel(2, 2).ToArgb() == Color.Blue.ToArgb(),
                    "a picture can be saved too, and comes back whole");

            // Days: the copied entries go when their days are over, also those at the top; the saved ones never.
            clips.Import("eight days ago", DateTime.Now.AddDays(-8), true);
            clips.Import("yesterday", DateTime.Today.AddHours(-1), false);
            long old = clips.AddSaved("old", "saved long ago", 0, DateTime.Now.AddYears(-3));
            clips.AddText("first\nsecond");
            Check(!clips.Import("yesterday", DateTime.Now.AddDays(-5), false) && !clips.Import("first\r\nsecond", DateTime.Now, false),
                "an imported text already there is left out, whatever its line ends");
            clips.Keep(7);
            Check(clips.Copied(10, Texts.Words("eight")).Count == 0 && clips.Copied(10, Texts.Words("yesterday")).Count == 1,
                "seven days: an older one goes, even at the top");
            clips.Number(clips.Copied(10, Texts.Words("yesterday"))[0].Id, 6);
            clips.Keep(0);
            Check(clips.Copied(10, Texts.Words("yesterday")).Count == 0 && clips.Numbered(6) == null && clips.Copied(10, Texts.Words("one")).Count == 1,
                "zero days: only today's stay, and a number goes with its entry");
            Check(clips.Saved().Exists(delegate(Entry e) { return e.Id == old; }), "a saved entry outlives every day");
            clips.Clean(0);
            Check(clips.CopiedCount() == 0 && clips.Saved().Count == 5, "cleaning everything leaves the saved ones");
            Check(Equals(clips.Data.Value("SELECT count(*) FROM apps"), 0L), "and forgets the programs nothing comes from any more");

            // Folders: a tree, each before those inside it.
            long work = clips.AddFolder(0, "Lavoro"), servers = clips.AddFolder(work, "Server"), home = clips.AddFolder(0, "Casa");
            long deep = clips.AddFolder(servers, "Dentro");
            List<Folder> folders = clips.Folders();
            Check(folders.Count == 4 && folders[0].Id == home && folders[1].Id == work && folders[2].Id == servers && folders[2].Level == 1
                && folders[3].Id == deep && folders[3].Level == 2 && folders[0].Open, "folders come in order, each before those inside it");
            Check(Refuses(clips, "INSERT INTO folders (parent_id, name, name_key) VALUES (" + work + ", 'server', 'SERVER')"),
                "two folders side by side cannot have one name");
            clips.AddFolder(home, "Server");
            Check(true, "the same name in another folder is fine");
            clips.Move(bank, deep);
            Check(clips.Saved().Find(delegate(Entry e) { return e.Id == bank; }).FolderId == deep, "a saved entry goes into a folder");
            Check(Throws<ArgumentException>(delegate { clips.MoveFolder(work, deep); }), "a folder never goes inside one it holds");
            clips.OpenFolder(work, false);
            clips.Reveal(deep);
            Check(clips.Folders().Find(delegate(Folder f) { return f.Id == work; }).Open, "revealing a folder opens those around it");
            clips.PlaceFolder(servers, home, "Server remoti");
            folders = clips.Folders();
            Check(folders.Find(delegate(Folder f) { return f.Id == servers; }).ParentId == home
                && folders.Find(delegate(Folder f) { return f.Id == deep; }).Level == 2, "a folder moves with what it holds");
            // Deleted, a folder leaves what it holds one level up; one of the same name there takes it in.
            long inner = clips.AddFolder(servers, "Server");   // inside Casa › Server remoti, next to Dentro
            clips.Move(old, inner);
            clips.DeleteFolder(servers);
            folders = clips.Folders();
            Check(folders.Find(delegate(Folder f) { return f.Id == servers; }) == null
                && folders.Find(delegate(Folder f) { return f.Id == deep; }).ParentId == home
                && folders.FindAll(delegate(Folder f) { return f.ParentId == home && f.Name == "Server"; }).Count == 1,
                "a deleted folder's folders move up, and join one of the same name");
            Check(clips.Saved().Find(delegate(Entry e) { return e.Id == old; }).FolderId != inner
                && clips.Saved().Find(delegate(Entry e) { return e.Id == bank; }).FolderId == deep,
                "and what they hold goes along");
            clips.DeleteFolder(home);
            Check(clips.Saved().Count == 5 && clips.Folders().TrueForAll(delegate(Folder f) { return f.ParentId != home; }),
                "deleting folders never deletes a saved entry");
        }
        using (Clips clips = new Clips(path))
            Check(clips.Saved().Count == 5, "what was saved is there when the file is opened again");
        System.IO.File.Delete(path);
        Check(!System.IO.File.Exists(path), "a closed file is free");
        using (Database db = new Database(path)) db.Execute("PRAGMA user_version = " + (Clips.SchemaVersion + 1));
        Check(Throws<System.IO.InvalidDataException>(delegate { new Clips(path).Dispose(); }), "a file of a newer version is refused");
        System.IO.File.Delete(path);
    }

    // Whether a call throws the exception it should.
    static bool Throws<T>(Action call) where T : Exception
    {
        try
        {
            call();
            return false;
        }
        catch (T)
        {
            return true;
        }
    }

    // The layer over the SQLite of Windows: what goes in comes back as it was, what is wrong is refused.
    static void SqliteTest()
    {
        Console.WriteLine("SQLite " + Database.Version);
        Check(Database.VersionNumber >= Database.MinimumVersion, "Windows has a SQLite recent enough");
        string path = System.IO.Path.Combine(sandbox, "sqlite test.db");
        System.IO.Directory.CreateDirectory(sandbox);
        using (Database db = new Database(path))
        {
            db.Execute("CREATE TABLE t (id INTEGER PRIMARY KEY, a, b, UNIQUE (a));"
                + " -- a comment, then a second statement\n CREATE TABLE u (n INTEGER CHECK (n > 0));");
            Check(Convert.ToInt64(db.Value("SELECT count(*) FROM sqlite_master WHERE type = 'table'")) == 2, "several statements run at once");

            string text = "Città già 😀 ~`\r\nriga\0dopo lo zero";
            byte[] big = new byte[1 << 20];
            new Random(1).NextBytes(big);
            db.Execute("INSERT INTO t (a, b) VALUES (?1, ?2)", text, big);
            long id = db.LastId;
            Check(id == 1 && db.Changes == 1, "the id of the row inserted, and the rows changed");
            using (Statement row = db.Prepare("SELECT a, b FROM t WHERE id = ?1", id))
            {
                Check(row.Step(), "a row comes back");
                Check(row.Text(0) == text, "a text comes back whole: accents, emoji, line ends, the character zero");
                byte[] back = row.Blob(1);
                Check(back.Length == big.Length && back[12345] == big[12345] && back[big.Length - 1] == big[big.Length - 1],
                    "a megabyte of bytes comes back");
                Check(!row.Step(), "and only that row");
            }
            db.Execute("INSERT INTO t (a, b) VALUES (?1, ?2)", "empty", new byte[0]);
            db.Execute("INSERT INTO t (a, b) VALUES (?1, ?2)", "nothing", null);
            using (Statement row = db.Prepare("SELECT b FROM t WHERE a = ?1", "empty"))
                Check(row.Step() && !row.IsNull(0) && row.Blob(0).Length == 0, "empty bytes stay empty, not nothing");
            using (Statement row = db.Prepare("SELECT b FROM t WHERE a = ?1", "nothing"))
                Check(row.Step() && row.IsNull(0) && row.Blob(0) == null && row.Text(0) == null && row.Get(0) == null, "nothing stays nothing");
            Check(Equals(db.Value("SELECT ?1", long.MaxValue), long.MaxValue) && Equals(db.Value("SELECT ?1", -7), -7L)
                && Equals(db.Value("SELECT ?1", true), 1L) && Equals(db.Value("SELECT ?1", 0.5), 0.5) && db.Value("SELECT 1 WHERE 0") == null,
                "numbers, a bool, a missing row");

            using (Statement insert = db.Prepare("INSERT INTO u (n) VALUES (?1)"))
                for (int i = 1; i <= 100; i++) insert.Run(i);
            Check(Convert.ToInt64(db.Value("SELECT sum(n) FROM u")) == 5050, "one statement prepared once, run many times");

            SqliteException refused = null;
            try
            {
                db.Execute("INSERT INTO t (a) VALUES (?1)", text);
            }
            catch (SqliteException ex)
            {
                refused = ex;
            }
            Check(refused != null && refused.IsConstraint && refused.Message.IndexOf("UNIQUE") >= 0 && refused.Message.IndexOf("Città") < 0,
                "a broken rule is refused, said with the statement and never with the values");
            Check(Throws<SqliteException>(delegate { db.Execute("INSERT INTO u (n) VALUES (0)"); }), "and so is a CHECK");
            Check(Throws<SqliteException>(delegate { db.Execute("SELEKT 1"); }), "a statement that makes no sense is refused");
            Check(Throws<ArgumentException>(delegate { db.Prepare("SELECT 1; SELECT 2").Dispose(); }), "Prepare takes one statement only");
            Check(Throws<ArgumentException>(delegate { db.Value("SELECT ?1, ?2", 1); }), "and as many values as it asks for");
            Check(Throws<ArgumentException>(delegate { db.Value("SELECT ?1", DateTime.Now); }), "and only values SQLite can take");

            // Transactions: all or nothing, also one inside another.
            try
            {
                using (Transaction transaction = db.Begin())
                {
                    db.Execute("INSERT INTO u (n) VALUES (1000)");
                    throw new InvalidOperationException();
                }
            }
            catch (InvalidOperationException)
            {
            }
            Check(db.Value("SELECT n FROM u WHERE n = 1000") == null, "a transaction that fails halfway is undone");
            using (Transaction outer = db.Begin())
            {
                db.Execute("INSERT INTO u (n) VALUES (2000)");
                using (Transaction inner = db.Begin()) db.Execute("INSERT INTO u (n) VALUES (3000)");
                using (Transaction inner = db.Begin())
                {
                    db.Execute("INSERT INTO u (n) VALUES (4000)");
                    inner.Commit();
                }
                outer.Commit();
            }
            Check(db.Value("SELECT n FROM u WHERE n = 2000") != null && db.Value("SELECT n FROM u WHERE n = 3000") == null
                && db.Value("SELECT n FROM u WHERE n = 4000") != null, "one inside another undoes only its own part");

            db.Prepare("SELECT * FROM t").Step();   // left open on purpose: closing the database must finalize it
        }
        using (Database db = new Database(path))
            Check(Convert.ToInt64(db.Value("SELECT count(*) FROM t")) == 3, "what was written is there when the file is opened again");
        System.IO.File.Delete(path);
        Check(!System.IO.File.Exists(path), "a closed database leaves its file free, statements left open included");
        Check(Throws<SqliteException>(delegate { new Database(System.IO.Path.Combine(sandbox, @"no such folder\x.db")).Dispose(); }),
            "a file that cannot be opened is refused");
    }

    // The logic around the list, without opening it and without touching the clipboard: private clipboard formats,
    // the search, the paths of the folders, the exported file. What the list keeps is checked by 'data'.
    static int TextsTest()
    {
        Check(MainForm.SettingsKey == TestKey && Clips.DefaultFile.StartsWith(System.IO.Path.GetTempPath())
            && Migration.OldHistory.StartsWith(System.IO.Path.GetTempPath()) && Log.FileName.StartsWith(System.IO.Path.GetTempPath()),
            "the checks keep away from the settings, the database, the old history and the log of the user");
        Log.Error(new InvalidOperationException("a test of the log"));
        Check(System.IO.File.ReadAllText(Log.FileName).IndexOf("a test of the log") > 0, "an error goes to the log");

        Check(!Texts.IsPrivate(Copied(null, 0)), "plain text is not private");
        Check(Texts.IsPrivate(Copied("ExcludeClipboardContentFromMonitorProcessing", 1)), "a marker format makes it private");
        Check(Texts.IsPrivate(Copied("Clipboard Viewer Ignore", 1)), "the older marker format too");
        Check(Texts.IsPrivate(Copied("CanIncludeInClipboardHistory", 0)), "history allowed = 0 makes it private");
        Check(!Texts.IsPrivate(Copied("CanIncludeInClipboardHistory", 1)), "history allowed = 1 does not");
        Check(Texts.IsPrivate(Copied("CanUploadToCloudClipboard", 0)), "cloud upload = 0 makes it private");

        Entry city = new Entry("Città di Torino", "riga uno\nSeconda RIGA");
        city.Saved = true;
        Check(Texts.Matches(city, ""), "an empty search matches everything");
        Check(Texts.Matches(city, "citta"), "accents do not count");
        Check(Texts.Matches(city, "TORINO città"), "words in any order, any case");
        Check(Texts.Matches(city, "riga seconda torino"), "name and text are both searched");
        Check(!Texts.Matches(city, "torino milano"), "every word must be there");
        Check(Texts.Matches(new Entry(null, "perché"), "perche"), "a copied text has no name and still matches");
        // Where the words found are, to be marked on the cards: in the text itself, its accents too.
        List<CharacterRange> marks = Texts.Find("Città di Torino, città", Texts.Words("CITTA torino"));
        Check(marks.Count == 3 && marks[0].First == 0 && marks[0].Length == 5 && marks[1].First == 9 && marks[1].Length == 6
            && marks[2].First == 17, "the words found, where they are in the text");
        marks = Texts.Find("perché no", Texts.Words("perché"));
        Check(marks.Count == 1 && marks[0].Length == 7, "an accent written apart goes with its letter");
        marks = Texts.Find("abcdef", Texts.Words("abc cde x"));
        Check(marks.Count == 1 && marks[0].First == 0 && marks[0].Length == 5, "overlapping words are marked as one");
        Check(Texts.Find("abc", new string[0]).Count == 0 && Texts.Find("", Texts.Words("a")).Count == 0, "nothing searched, nothing marked");
        Check(city.ToString().IndexOf("riga") < 0, "the list shows only the name of a saved text");
        Check(new Entry(null, " a\r\nb\tc ").ToString() == "a  b c", "a copied text is shown on one line");
        Check(Texts.Within(@"a\b\c", "a") && Texts.Within("a", "A") && !Texts.Within("a 2", "a") && Texts.Parent("a") == null
            && Texts.Parent(@"a\b\c") == @"a\b" && Texts.Leaf(@"a\b") == "b" && Texts.Depth(@"a\b") == 2 && Texts.Join(null, "a") == "a"
            && Texts.Display(@"a\b") == "a › b" && Texts.CleanPath(@" a \\ b\ ") == @"a\b" && Texts.CleanPath(@" \ ") == null,
            "paths of folders");

        // The exported file: what goes out comes back, also a text that looks like the file itself.
        List<Entry> sent = new List<Entry>();
        Entry sentFolder = new Entry("Lavoro", "", DateTime.MinValue);
        sentFolder.IsGroup = sentFolder.Saved = true;
        Entry undated = new Entry("Senza data", "uno", DateTime.MinValue);
        undated.Saved = true;
        Entry sentSaved = new Entry("Firma", "[Salvato]\r\nNome: finto\r\n\r\n\tcon tabulazione\r\nultima riga\r\n\r\n", new DateTime(2026, 3, 4, 5, 6, 7));
        sentSaved.Saved = true;
        sentSaved.Group = "Lavoro";
        Entry sentCopied = new Entry(null, "copiato è", new DateTime(2026, 1, 2, 3, 4, 5));
        sentCopied.Pinned = true;
        sent.AddRange(new Entry[] { undated, sentFolder, sentSaved, sentCopied });
        using (Bitmap picture = new Bitmap(4, 4)) sent.Add(new Entry(picture));
        List<Entry> read = Backup.Parse(Backup.Lines(sent));
        Check(read.Count == 4 && read[0].Saved && read[0].Name == "Senza data" && read[0].Text == "uno" && read[0].Time == DateTime.MinValue
            && read[0].Group == null, "an exported text comes back, a picture is left out");
        Check(read[1].IsGroup && read[1].Name == "Lavoro", "a folder comes back");
        Check(read[2].Saved && read[2].Name == "Firma" && read[2].Text == sentSaved.Text && read[2].Group == "Lavoro" && read[2].Time == sentSaved.Time,
            "a text that looks like the file comes back identical, with folder and day");
        Check(!read[3].Saved && read[3].Name == null && read[3].Text == "copiato è" && read[3].Pinned && read[3].Time == sentCopied.Time,
            "a copied text comes back with its time and its arrow");
        List<string> edited = Backup.Lines(sent);
        edited.Insert(edited.IndexOf("\tuno") + 1, "");
        edited.Add("righe senza senso");
        Check(Backup.Parse(edited).Count == 4 && Backup.Parse(edited)[0].Text == "uno", "a file changed by hand is still read");
        Entry nameless = new Entry(null, "mail@second.example", DateTime.MinValue);
        nameless.Saved = true;
        List<Entry> back = Backup.Parse(Backup.Lines(new List<Entry>(new Entry[] { nameless })));
        Check(back.Count == 1 && back[0].Saved && back[0].Name == null && back[0].Text == nameless.Text && back[0].Label == nameless.Text,
            "a saved text without a name comes back as one, and shows its content");
        Check(MainForm.NumberName(1) == "Ctrl+" + Strings.ShiftName + "+1", "the keys of a number: those held, and the digit");

        // A font that is not there, or a size that is not offered, as a settings value written by hand: the defaults.
        string firstFont = Look.FontNames[0];
        float window = Look.TextFont.SizeInPoints;
        Look.UseEntryFont("No Such Font", 999);
        Check(Look.EntryFamily == firstFont && Look.EntrySize == Look.DefaultEntrySize && Look.EntryFont.Name == firstFont,
            "an unknown font or size of the entries falls back to the defaults");
        string lastFont = Look.FontNames[Look.FontNames.Length - 1];
        int largest = Look.EntrySizes[Look.EntrySizes.Length - 1];
        Look.UseEntryFont(lastFont, largest);
        Check(Look.EntryFont.Name == lastFont && Math.Abs(Look.EntryFont.SizeInPoints - largest * 0.75f) < 0.01f
            && Math.Abs(Look.StrongEntryFont.SizeInPoints - Look.EntryFont.SizeInPoints) < 0.01f
            && Look.TextFont.SizeInPoints == window, "the entries take their font and size in pixels, the windows keep theirs");
        Look.UseEntryFont(null, Look.DefaultEntrySize);
        RichTest();
        LanguagesTest();
        OcrTest();
        using (Form own = new Form())
            Check(string.Equals(Origin.ProgramOf(own.Handle), System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName,
                StringComparison.OrdinalIgnoreCase) && Origin.ProgramOf(IntPtr.Zero) == null, "the program of a window is that of its process");
        Check(Origin.IsHost(@"C:\Windows\System32\SVCHOST.EXE") && !Origin.IsHost(@"C:\Windows\System32\notepad.exe"),
            "the host of the clipboard service is never the program copied in");
        return failed == 0 ? 0 : 1;
    }

    // A picture of a text as the screen shows it, as PNG: on white, or on nothing (transparent). Of 16 pixels:
    // smaller, Windows reads the text of a screen with mistakes, which are not what is checked here.
    static byte[] TextPicture(string text, bool transparent)
    {
        using (Bitmap picture = new Bitmap(500, 80, transparent ? PixelFormat.Format32bppArgb : PixelFormat.Format24bppRgb))
        using (Font font = new Font("Segoe UI", 12f))
        using (System.IO.MemoryStream png = new System.IO.MemoryStream())
        {
            using (Graphics graphics = Graphics.FromImage(picture))
            {
                // GDI would leave the letters transparent on a transparent picture: there GDI+ writes them.
                if (transparent)
                {
                    graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                    graphics.DrawString(text, font, Brushes.Black, 4, 4);
                }
                else
                {
                    graphics.Clear(Color.White);
                    TextRenderer.DrawText(graphics, text, font, new Rectangle(4, 4, 490, 70), Color.Black, TextFormatFlags.WordBreak);
                }
            }
            picture.Save(png, ImageFormat.Png);
            return png.ToArray();
        }
    }

    // The text of a picture, read by Windows. Where it reads none of the languages (a server may not), only said.
    static void OcrTest()
    {
        string read;
        try
        {
            read = Ocr.Read(TextPicture("Tuesday 2026 meeting\nsecond line here", false));
        }
        catch (Exception ex)
        {
            Console.WriteLine("[SKIP] reading pictures: " + ex.Message);
            return;
        }
        if (read == null)
        {
            Console.WriteLine("[SKIP] reading pictures: Windows reads none of the languages here");
            return;
        }
        Check(read == "Tuesday 2026 meeting\r\nsecond line here", "the text of a picture is read, a line per line: " + read.Replace("\r\n", " | "));
        Check(Ocr.Read(TextPicture("Tuesday 2026 meeting", true)) == "Tuesday 2026 meeting", "a transparent picture is read too");
        Check(Ocr.Read(TextPicture("", false)) == "", "a picture without text has none");
        // Read as the language of the program, when Windows has it: that of English would lose the accents.
        bool italian = false;
        foreach (Windows.Globalization.Language language in Windows.Media.Ocr.OcrEngine.AvailableRecognizerLanguages)
            italian |= language.LanguageTag.StartsWith("it");
        if (!italian) return;
        string code = Strings.Code;
        Strings.Code = "it";
        try
        {
            Check(Ocr.Read(TextPicture("Città più bella", false)) == "Città più bella", "in Italian the accents are read");
        }
        finally
        {
            Strings.Code = code;
        }
    }

    static Run NewRun(string text, string font, float size, FontStyle style, Color colour)
    {
        Run run = new Run();
        run.Text = text;
        run.Font = font;
        run.Size = size;
        run.Style = style;
        run.Color = colour;
        return run;
    }

    // An edit box as the window of a saved text has it, in a font and a colour no run of the checks has.
    static RichBox NewBox()
    {
        RichBox box = new RichBox();
        GC.KeepAlive(box.Handle);
        box.Font = new Font("Tahoma", 11);
        box.Ink = Color.FromArgb(40, 40, 40);
        box.ForeColor = box.Ink;
        return box;
    }

    static string Describe(List<Run> runs)
    {
        StringBuilder all = new StringBuilder();
        foreach (Run run in runs)
            all.AppendFormat("[{0}|{1}|{2}|{3}|{4:X}]", run.Text.Replace("\n", "\\n"), run.Font, run.Size, run.Style,
                run.Color.IsEmpty ? 0 : run.Color.ToArgb());
        return all.ToString();
    }

    // Formatted text: HTML read as a browser shows it, written back, shown in an edit box and read from it, its
    // formatting changed by the buttons; the HTML of the clipboard with its header.
    static void RichTest()
    {
        string fragment = "<b>Città €</b> “x”";
        string document = Rich.Document(fragment);
        byte[] bytes = Encoding.UTF8.GetBytes(document);
        int start = document.IndexOf("StartFragment:") + "StartFragment:".Length, end = document.IndexOf("EndFragment:") + "EndFragment:".Length;
        Check(Rich.Fragment(document) == fragment && Encoding.UTF8.GetString(bytes, int.Parse(document.Substring(start, 10)),
            int.Parse(document.Substring(end, 10)) - int.Parse(document.Substring(start, 10))) == fragment,
            "the HTML of the clipboard says in bytes where its fragment is, and the fragment comes back");
        Check(Rich.Fragment("<html><body><!--StartFragment--><i>a</i><!--EndFragment--></body></html>") == "<i>a</i>",
            "without a header the fragment is found by its markers");
        // As the clipboard holds it: written by this program as bytes of UTF-8, which Windows Forms would write in the
        // code page of Windows, and read back as such. Read from another program, through Windows, it is the same
        // path (Rich.Bytes): tried by hand on the real clipboard, as the checks do not touch it.
        DataObject put = new DataObject();
        Rich.Put(put, fragment, null);
        System.IO.MemoryStream written = put.GetData(DataFormats.Html) as System.IO.MemoryStream;
        Check(written != null && Encoding.UTF8.GetString(written.ToArray()).TrimEnd('\0') == document && Rich.Html(put) == fragment,
            "the HTML of the clipboard is written and read in UTF-8, accents included");
        // Copied from a page in a dark theme (the chat of VS Code): its dark backgrounds and the light grey of its text
        // go, colours that are colours and darker greys stay, as does a light background.
        string dark = "<p style=\"color: rgb(191, 191, 191); font-family: &quot;Segoe UI&quot;, sans-serif; background-color: rgb(25, 26, 27);\">"
            + "<b>A</b> <a style=\"color: rgb(72, 160, 199);\">B</a> <code style=\"color: #8c8c8c; background: none 0% 0% / auto rgb(38, 38, 38); "
            + "padding: 2px\">C</code> <span style='color:#666;background-color:#fff'>D</span></p>";
        string light = Rich.Lighten(dark);
        List<Run> lit = Rich.FromHtml(light);
        Check(!light.Contains("191") && !light.Contains("25, 26, 27") && !light.Contains("38, 38, 38") && light.Contains("72, 160, 199")
            && light.Contains("#8c8c8c") && light.Contains("padding: 2px") && light.Contains("&quot;Segoe UI&quot;")
            && light.Contains("color:#666;background-color:#fff") && Rich.FromHtml("<span style=\"color:#bfbfbf\">x</span>")[0].Color.IsEmpty
            && Rich.TextOf(lit) == "A B C D", "what comes from a dark page loses its dark backgrounds and its light grey text");

        // A signature as a web mail copies it: inline styles, fonts, entities, a style sheet and a hidden part.
        string signature = "<div style=\"font-family:'No Such Face',serif\"><b><span style=\"font-size:20pt\">Sai&nbsp;Chavan</span></b></div>\r\n"
            + "<div style='font-family: \"Georgia\"; font-size:12px'>Owner,\r\n   Munson&#8217;s   Pickles</div>"
            + "<style>div { color: red }</style><span style=\"display:none\">hidden</span><!-- a comment -->"
            + "<div><font face=\"Arial\" color=\"#808080\" size=\"2\">(872) 555-0179<br>sai@munsons.example</font></div>"
            + "<p><i>a</i> &amp; <u>b</u> &lt;3 <a href=\"https://munsons.example\">link</a> <span style=\"color:#111\">black</span>"
            + " <span style=\"color: rgb(0, 128, 0); font-weight: 700; text-decoration: line-through\">green</span></p>";
        List<Run> runs = Rich.FromHtml(signature);
        Check(Rich.TextOf(runs) == "Sai Chavan\nOwner, Munson’s Pickles\n(872) 555-0179\nsai@munsons.example\na & b <3 link black green",
            "the text of HTML is what a browser shows: blocks on lines of their own, spaces as one, nothing hidden");
        // The run that starts with a text.
        Func<string, Run> at = delegate(string text) { return runs.Find(delegate(Run run) { return run.Text.TrimStart().StartsWith(text); }); };
        Check(at("Sai").Font == "No Such Face" && at("Sai").Size == 20 && at("Sai").Style == FontStyle.Bold
            && at("Owner").Font == "Georgia" && at("Owner").Size == 9 && at("Owner").Style == FontStyle.Regular,
            "fonts, sizes in points or pixels, bold");
        Check(at("(872)").Font == "Arial" && at("(872)").Size == 10 && at("(872)").Color.ToArgb() == Color.FromArgb(128, 128, 128).ToArgb()
            && at("(872)").Text.Contains("sai@"), "the old <font> with its face, size and colour");
        Check(at("a").Style == FontStyle.Italic && at("b").Style == FontStyle.Underline && at("link").Style == FontStyle.Underline
            && !at("link").Color.IsEmpty && at("black").Color.IsEmpty && at("green").Color.ToArgb() == Color.FromArgb(0, 128, 0).ToArgb()
            && at("green").Style == (FontStyle.Bold | FontStyle.Strikeout), "italic, underlined, links, colours; black is the default");

        Check(Rich.Matches(runs, "Sai Chavan\r\nOwner, Munson’s Pickles\r\n\t(872) 555-0179 sai@munsons.example\r\na & b <3 link black green")
            && !Rich.Matches(Rich.FromHtml("<a href=\"https://meteo.example/it/tempo\">Meteo Montorio - meteo</a>"), "https://meteo.example/it/tempo"),
            "formatting goes with the text it formats, not with a link that names a page by its title");

        // The window opened on a copied text, plain or formatted, can save it at once.
        foreach (Entry each in new Entry[] { new Entry(null, "https://www.ilpost.it/"), SavedSample() })
            using (EditForm edit = new EditForm(each, null, new List<string>()))
            {
                Button save = (Button)typeof(EditForm).GetField("saveButton", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(edit);
                Check(save.Enabled && edit.Content.Replace("\r\n", "\n") == each.Text.Replace("\r\n", "\n"),
                    "the window of a " + (each.Html == null ? "plain" : "formatted") + " text can save it as soon as it opens");
            }

        // Written as HTML and read again: the same.
        List<Run> made = new List<Run>
        {
            NewRun("Title & <more>", "Georgia", 18, FontStyle.Bold, Color.Empty),
            NewRun("\n  two spaces,\ta tab ", null, 0, FontStyle.Regular, Color.Empty),
            NewRun("red", null, 9.5f, FontStyle.Italic | FontStyle.Underline, Color.FromArgb(200, 30, 30)),
            NewRun(" end", "Courier New", 0, FontStyle.Strikeout, Color.Empty)
        };
        string html = Rich.ToHtml(made);
        Check(html.Contains("Title &amp; &lt;more&gt;") && html.Contains("font-family:'Georgia'") && html.Contains("<br>")
            && html.Contains("&nbsp;") && html.Contains("color:#c81e1e"), "HTML written with its entities, spans and kept spaces");
        List<Run> again = Rich.FromHtml(html);
        Check(Describe(again) == Describe(Rich.FromHtml(Rich.ToHtml(again))) && Rich.TextOf(again) == "Title & <more>\n  two spaces,    a tab red end"
            && again[0].Font == "Georgia" && again[0].Size == 18 && again[again.Count - 1].Font == "Courier New",
            "and read back the same, spaces kept, a tab as four");

        // Shown in an edit box and read from it: what has the font and colour of the box has the default ones.
        using (RichBox box = NewBox())
        {
            List<Run> shown = Rich.FromHtml(signature);
            box.Rtf = Rich.ToRtf(shown, box.Font, box.Ink);
            Check(Describe(Rich.FromBox(box, box.Font, box.Ink)) == Describe(shown) && box.Text == Rich.TextOf(shown),
                "runs shown in an edit box are read back from it as they were");
        }
        // A long text is read a piece at a time, not a character at a time: the same runs, at once.
        using (RichBox box = NewBox())
        {
            List<Run> pieces = new List<Run>();
            for (int i = 0; i < 40; i++)
            {
                pieces.Add(NewRun(new string('a', 500) + "\n" + new string('b', 300), null, 0, FontStyle.Regular, Color.Empty));
                pieces.Add(NewRun("word" + i, i % 2 == 0 ? "Georgia" : null, i % 3 == 0 ? 16 : 0, FontStyle.Bold, Color.Empty));
            }
            box.Rtf = Rich.ToRtf(pieces, box.Font, box.Ink);
            System.Diagnostics.Stopwatch clock = System.Diagnostics.Stopwatch.StartNew();
            List<Run> read = Rich.FromBox(box, box.Font, box.Ink);
            Check(Describe(read) == Describe(pieces) && clock.ElapsedMilliseconds < 2000,
                "a long formatted text is read back as it was, in " + clock.ElapsedMilliseconds + " ms");
        }
        using (RichBox box = NewBox())
        {
            box.Text = "hello world";
            Check(Rich.Plain(Rich.FromBox(box, box.Font, box.Ink)), "plain text in the box is plain");
            box.Select(0, 5);
            box.Toggle(FontStyle.Bold);
            box.Select(6, 5);
            box.Face("Georgia");
            box.Points(16);
            List<Run> read = Rich.FromBox(box, box.Font, box.Ink);
            Check(read.Count == 3 && read[0].Text == "hello" && read[0].Style == FontStyle.Bold && read[0].Font == null
                && read[2].Text == "world" && read[2].Font == "Georgia" && read[2].Size == 16 && read[2].Style == FontStyle.Regular,
                "the buttons make the selection bold, give it a font and a size");
            box.Select(0, 3);
            box.Toggle(FontStyle.Bold);
            Check(Rich.FromBox(box, box.Font, box.Ink)[0].Text == "hel" && Rich.FromBox(box, box.Font, box.Ink)[0].Style == FontStyle.Regular,
                "bold again takes it away");
            box.Select(0, 0);
            box.Plain();
            Check(Rich.Plain(Rich.FromBox(box, box.Font, box.Ink)) && box.SelectionStart == 0, "the rubber with nothing selected clears all");
        }
        string rtf = Rich.ToRtf(made, new Font("Arial", 10), Color.Black);
        Check(Rich.TextOf(Rich.FromRtf(rtf)) == Rich.TextOf(made) && Rich.FromRtf(rtf)[0].Font == "Georgia" && Rich.FromRtf("not rtf") == null,
            "RTF is read as runs; what is not RTF is not");
    }

    // Every language: no text asks for more values than the program gives it (string.Format would throw), the
    // guide has the same subjects, an exported file is read back whatever its language, and what is left in
    // English is listed. Each language starts again from English, as the program does at its start.
    static void LanguagesTest()
    {
        Dictionary<FieldInfo, object> english = new Dictionary<FieldInfo, object>();
        foreach (FieldInfo field in typeof(Strings).GetFields(BindingFlags.Public | BindingFlags.Static))
            if (!field.IsInitOnly) english[field] = field.GetValue(null);
        Func<string[], int> subjects = delegate(string[] lines)
        {
            return Array.FindAll(lines, delegate(string line) { return line.StartsWith("# "); }).Length;
        };
        int englishSubjects = subjects(Strings.Guide());
        Entry folder = new Entry("A", "", DateTime.MinValue);
        folder.IsGroup = folder.Saved = true;
        Entry text = new Entry("B", "riga\r\n\tuna tabulazione", new DateTime(2026, 5, 6, 7, 8, 9));
        text.Saved = true;
        text.Group = "A";
        text.Html = "<b>riga</b><br>\r\n\r\n<i>\tuna tabulazione</i>";
        Entry copied = new Entry(null, "copied", new DateTime(2026, 5, 6, 7, 8, 9));
        copied.Pinned = true;
        List<Entry> sent = new List<Entry>(new Entry[] { folder, text, copied });
        foreach (string code in Strings.Codes)
        {
            foreach (KeyValuePair<FieldInfo, object> pair in english) pair.Key.SetValue(null, pair.Value);
            Strings.Use(code);
            List<string> tooMany = new List<string>(), same = new List<string>();
            foreach (KeyValuePair<FieldInfo, object> pair in english)
            {
                string original = pair.Value as string, now = pair.Key.GetValue(null) as string;
                if (original == null) continue;
                if (Highest(now) > Highest(original)) tooMany.Add(pair.Key.Name);
                if (code != Strings.Codes[0] && now == original) same.Add(pair.Key.Name);
            }
            Check(tooMany.Count == 0, code + ": every text fits the values it is given" + (tooMany.Count == 0 ? "" : ": " + string.Join(", ", tooMany.ToArray())));
            Check(subjects(Strings.Guide()) == englishSubjects, code + ": the guide has the " + englishSubjects + " subjects");
            // Nothing the program says, nor the made-up entries of the pictures of the Store, has a word kept out of it.
            List<string> said = new List<string>(Strings.Guide());
            foreach (FieldInfo field in english.Keys) if (field.GetValue(null) is string) said.Add((string)field.GetValue(null));
            foreach (FieldInfo field in typeof(Sample).GetFields()) said.Add(field.GetValue(Sample.Current) as string ?? "");
            List<string> kept = said.FindAll(delegate(string each) { return Forbidden(each) != null; });
            Check(kept.Count == 0, code + ": no word kept out of the Store" + (kept.Count == 0 ? "" : ": " + Forbidden(kept[0]) + " in “" + kept[0] + "”"));
            List<Entry> read = Backup.Parse(Backup.Lines(sent));
            Check(read.Count == 3 && read[0].IsGroup && read[1].Text == text.Text && read[1].Group == "A" && read[1].Html == text.Html
                && read[2].Pinned && read[2].Html == null, code + ": an exported file comes back, a formatted text with its HTML");
            if (same.Count > 0) Console.WriteLine("       same as English: " + string.Join(", ", same.ToArray()));
        }
        foreach (KeyValuePair<FieldInfo, object> pair in english) pair.Key.SetValue(null, pair.Value);
    }

    // The words that must never reach the Store, from the list make-listing.ps1 reads too.
    static List<System.Text.RegularExpressions.Regex> forbidden;

    // The first of those words in a text, or null.
    static string Forbidden(string text)
    {
        if (forbidden == null)
        {
            forbidden = new List<System.Text.RegularExpressions.Regex>();
            string list = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\store\forbidden-words.txt");
            foreach (string line in System.IO.File.ReadAllLines(list, Encoding.UTF8))
                if (line.Trim().Length > 0 && !line.StartsWith("#"))
                    forbidden.Add(new System.Text.RegularExpressions.Regex(line.Trim(), System.Text.RegularExpressions.RegexOptions.IgnoreCase));
        }
        foreach (System.Text.RegularExpressions.Regex word in forbidden)
        {
            System.Text.RegularExpressions.Match found = word.Match(text);
            if (found.Success) return found.Value;
        }
        return null;
    }

    // The highest {n} a text asks string.Format for, or -1.
    static int Highest(string text)
    {
        int highest = -1;
        foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(text ?? "", @"\{(\d+)\}"))
            highest = Math.Max(highest, int.Parse(match.Groups[1].Value));
        return highest;
    }

    static string Show(char c)
    {
        return c > ' ' && c < 127 ? c.ToString() : string.Format("<{0:X4}>", (int)c);
    }

    static string Show(string s)
    {
        StringBuilder b = new StringBuilder();
        foreach (char c in s) b.Append(c >= ' ' && c < 127 ? c.ToString() : string.Format("<{0:X4}>", (int)c));
        return b.ToString();
    }

    static string Ascii()
    {
        StringBuilder b = new StringBuilder();
        for (char c = ' '; c <= '~'; c++) b.Append(c);
        return b.ToString();
    }

    const string Extra = "àèéìòùç°§£€È";

    static int MapTest()
    {
        IntPtr[] list = new IntPtr[32];
        int n = GetKeyboardLayoutList(list.Length, list);
        IntPtr us = IntPtr.Zero;
        for (int i = 0; i < n; i++)
        {
            long id = list[i].ToInt64() & 0xFFFFFFFF;
            Console.WriteLine("installed layout: 0x{0:X8}", id);
            if (id == 0x04090409) us = list[i];
        }
        IntPtr layout = Native.GetKeyboardLayout(0);
        Console.WriteLine("active layout:    0x{0:X8}", layout.ToInt64() & 0xFFFFFFFF);

        System.Collections.Generic.Dictionary<char, Stroke> local = Typer.MapLayout(layout);
        Console.WriteLine("characters with a key on the active layout: {0}", local.Count);
        StringBuilder missing = new StringBuilder();
        StringBuilder line = new StringBuilder();
        foreach (char c in Ascii() + Extra)
        {
            Stroke s;
            if (local.TryGetValue(c, out s))
                line.AppendFormat("{0}={1:X2}{2}{3}{4}  ", Show(c), s.Scan, s.Shift ? "s" : "", s.AltGr ? "g" : "", s.Dead ? "d" : "");
            else missing.Append(Show(c)).Append(' ');
            if (line.Length > 100) { Console.WriteLine(line); line.Length = 0; }
        }
        Console.WriteLine(line);
        Console.WriteLine("no key on active layout: " + missing);

        int problems = 0, compared = 0;
        System.Collections.Generic.Dictionary<char, Stroke> reference = us != IntPtr.Zero ? Typer.MapLayout(us) : local;
        for (char c = ' '; c <= '~'; c++)
        {
            Stroke u, l;
            if (!Typer.UsKeys.TryGetValue(c, out u))
            {
                Console.WriteLine("US table: missing " + Show(c));
                problems++;
                continue;
            }
            bool comparable = us != IntPtr.Zero || char.IsLetterOrDigit(c) || c == ' ';
            if (!comparable || !reference.TryGetValue(c, out l)) continue;
            compared++;
            if (l.Scan != u.Scan || l.Shift != u.Shift || l.AltGr || l.Dead)
            {
                Console.WriteLine("US table: {0} is scan {1:X2} shift={2}, reference says {3:X2} shift={4} altgr={5}",
                    Show(c), u.Scan, u.Shift, l.Scan, l.Shift, l.AltGr);
                problems++;
            }
        }
        Console.WriteLine("US table: {0} problems, {1} keys cross-checked against {2}", problems, compared,
            us != IntPtr.Zero ? "the installed US layout" : "the active layout (letters and digits only)");
        return problems == 0 ? 0 : 1;
    }

    static int Shot(string path)
    {
        using (ShotForm form = new ShotForm())
        {
            form.TopMost = false;
            form.ShowInTaskbar = false;
            form.StartPosition = FormStartPosition.Manual;
            form.Location = new Point(-30000, -30000);
            BindingFlags hidden = BindingFlags.NonPublic | BindingFlags.Instance;
            FillSample(form);
            form.Show();
            Application.DoEvents();
            Save(form, path + "-idle.png");

            // Under the pointer a card shows its buttons in the place of the program and the time, and is as tall as
            // without them; the saved texts with a name, one line each, are all as tall.
            RowList hovered = (RowList)typeof(MainForm).GetField("cardList", hidden).GetValue(form);
            int hoverAt = RowOf(hovered, delegate(Entry e) { return e.Category == Category.Link; });
            int restHeight = hovered.GetItemHeight(hoverAt);
            hovered.Glow.Set(hoverAt);
            typeof(MainForm).GetField("hoverRow", hidden).SetValue(form, hoverAt);
            typeof(MainForm).GetField("hoverPart", hidden).SetValue(form, Part.Copy);
            for (int i = 0; i < 100 && hovered.Glow.Of(hoverAt) < 1; i++)
            {
                System.Threading.Thread.Sleep(10);
                Application.DoEvents();
            }
            hovered.Invalidate();
            Application.DoEvents();
            Save(form, path + "-hover.png");
            bool steady = hovered.GetItemHeight(hoverAt) == restHeight;
            hovered.Glow.Set(-1);
            typeof(MainForm).GetField("hoverRow", hidden).SetValue(form, -1);
            // Every card as tall as the others, whatever it holds: texts of one line or more, pictures, names; with two
            // lines all of them taller.
            MethodInfo useLines = typeof(MainForm).GetMethod("UseCardLines", hidden);
            List<int> one = CardHeights(form, hovered);
            useLines.Invoke(form, new object[] { 2 });
            Save(form, path + "-two.png");
            List<int> two = CardHeights(form, hovered);
            useLines.Invoke(form, new object[] { 1 });
            Console.WriteLine("[{0}] a card is as tall under the pointer, and all cards are as tall, with one line or two: {1} / {2}",
                steady && one.Count == 1 && two.Count == 1 && two[0] > one[0] ? "PASS" : "FAIL",
                string.Join(", ", one.ConvertAll(delegate(int h) { return h.ToString(); }).ToArray()),
                string.Join(", ", two.ConvertAll(delegate(int h) { return h.ToString(); }).ToArray()));

            // The history: the copied entry at the top under its own name, said by the bar over the cards, then the others under
            // the name of their day; the saved ones are not there.
            ListBox cards = (ListBox)typeof(MainForm).GetField("cardList", hidden).GetValue(form);
            Control dayBar = (Control)typeof(MainForm).GetField("dayBar", hidden).GetValue(form);
            List<Entry> shown = Cards(cards);
            Console.WriteLine("[{0}] the history, the one at the top first, then each day under its name, nothing saved: {1} rows",
                shown.Count == 5 && shown[0].Pinned && cards.Items[0] is Entry && cards.Items[1] is Heading && cards.Items.Count == 6
                && dayBar.Visible && dayBar.Text == Strings.PinnedPlace && cards.Top == dayBar.Bottom
                && !shown.Exists(delegate(Entry e) { return e.Saved; }) ? "PASS" : "FAIL", cards.Items.Count);
            // Refilling the list (it happens at every clipboard change) must not move the selection.
            cards.SelectedIndex = 5;
            Entry chosen = (Entry)cards.SelectedItem;
            typeof(MainForm).GetMethod("Filter", hidden).Invoke(form, null);
            Console.WriteLine("[{0}] the selection survives a refill", cards.SelectedItem is Entry && ((Entry)cards.SelectedItem).Id == chosen.Id
                ? "PASS" : "FAIL");
            cards.SelectedIndex = 1;
            Console.WriteLine("[{0}] the name of a day is never selected", cards.SelectedIndex == 5 ? "PASS" : "FAIL");

            // Scrolling, the bar over the cards says the day of the top card: that of the name before it, or with a name at
            // the top, right under the bar, the day before; back at the top, the name the cards begin under. With the list
            // made short, so that it scrolls.
            MethodInfo showDay = typeof(MainForm).GetMethod("ShowDay", hidden);
            int listHeight = cards.Height;
            cards.Height = 2 * cards.GetItemHeight(0);
            cards.TopIndex = 2;
            showDay.Invoke(form, null);
            string scrolled = dayBar.Text;
            cards.TopIndex = 1;
            showDay.Invoke(form, null);
            string onName = dayBar.Text;
            cards.TopIndex = 0;
            showDay.Invoke(form, null);
            string atTop = dayBar.Text;
            cards.Height = listHeight;
            Console.WriteLine("[{0}] scrolling, the bar over the cards says the day of the top one: {1}, {2}, {3}",
                scrolled == ((Heading)cards.Items[1]).Text && onName == Strings.PinnedPlace && atTop == Strings.PinnedPlace ? "PASS" : "FAIL",
                scrolled, onName, atTop);

            // Ctrl and Shift with a click pick several cards, over the names of the days; a bar in the place of the tabs says
            // how many, a refill keeps them, the arrow of all of them goes on and then off together; one left is the selected
            // card again, and letting them go leaves the tabs.
            MethodInfo pick = typeof(MainForm).GetMethod("Pick", hidden);
            HashSet<long> picked = (HashSet<long>)typeof(MainForm).GetField("picked", hidden).GetValue(form);
            Control pickBar = (Control)typeof(MainForm).GetField("pickBar", hidden).GetValue(form);
            Control kindTabs = (Control)typeof(MainForm).GetField("tabs", hidden).GetValue(form);
            int lastRow = cards.Items.Count - 1;
            cards.SelectedIndex = 0;
            pick.Invoke(form, new object[] { lastRow, Keys.Shift });
            bool ranged = picked.Count == 5 && cards.SelectedIndex == lastRow && pickBar.Visible && !kindTabs.Visible;
            pick.Invoke(form, new object[] { 3, Keys.Control });
            bool lessOne = picked.Count == 4 && !picked.Contains(((Entry)cards.Items[3]).Id);
            Application.DoEvents();
            Save(form, path + "-picked.png");
            typeof(MainForm).GetMethod("Filter", hidden).Invoke(form, null);
            bool kept = picked.Count == 4;
            long wasPinned = shown[0].Id;
            MethodInfo pinPicked = typeof(MainForm).GetMethod("PinSelected", hidden);
            pinPicked.Invoke(form, null);
            bool allUp = Cards(cards).FindAll(delegate(Entry e) { return e.Pinned; }).Count == 4 && picked.Count == 4;
            pinPicked.Invoke(form, null);
            bool allDown = !Cards(cards).Exists(delegate(Entry e) { return e.Pinned; });
            ClipsOf(form).KeepAtTop(wasPinned, true);   // as the sample has it
            typeof(MainForm).GetMethod("ClearPicks", hidden).Invoke(form, null);
            typeof(MainForm).GetMethod("Filter", hidden).Invoke(form, null);
            bool letGo = picked.Count == 0 && kindTabs.Visible && !pickBar.Visible;
            cards.SelectedIndex = 2;
            pick.Invoke(form, new object[] { 3, Keys.Control });
            pick.Invoke(form, new object[] { 3, Keys.Control });
            bool backToOne = picked.Count == 0 && cards.SelectedIndex == 2;
            Console.WriteLine("[{0}] several cards picked with Ctrl and Shift, their bar, kept by a refill, pinned and released together, let go",
                ranged && lessOne && kept && allUp && allDown && letGo && backToOne ? "PASS" : "FAIL");
            // The same with the button itself, as Windows tells it to the list: with Ctrl it picks, without it selects one.
            Press(cards, 4, Native.MK_CONTROL);
            bool pressedPick = picked.Count == 2 && cards.SelectedIndex == 4;
            Press(cards, 4, 0);
            Console.WriteLine("[{0}] the button pressed with Ctrl picks, without it selects one card", pressedPick && picked.Count == 0
                && cards.SelectedIndex == 4 ? "PASS" : "FAIL");

            // The places of the sidebar, and the tabs.
            Sample s = Sample.Current;
            ListBox nav = (ListBox)typeof(MainForm).GetField("navList", hidden).GetValue(form);
            Console.WriteLine("[{0}] the sidebar: history, pinned, shortcuts, saved with its folders, the programs: {1} rows",
                nav.Items.Count == 11 && ((Place)nav.Items[2]).Kind == PlaceKind.Shortcuts && ((Place)nav.Items[2]).Count == 3
                && ((Place)nav.Items[4]).Label == s.Home && ((Place)nav.Items[6]).Level == 2 && ((Place)nav.Items[7]).Kind == PlaceKind.Sources
                && ((Place)nav.Items[8]).Origin != null ? "PASS" : "FAIL", nav.Items.Count);
            // The shortcuts: the entries with a number, copied and saved, by number, without days.
            GoTo(form, PlaceKind.Shortcuts, null);
            List<Entry> numberedCards = Cards(cards);
            // The number on a card is a part of its own, which a click acts on.
            CardParts numberParts = (CardParts)typeof(MainForm).GetMethod("Lay", hidden).Invoke(form, new object[] { numberedCards[0], cards.GetItemRectangle(0) });
            Point onNumber = new Point(numberParts.Number.Left + numberParts.Number.Width / 2, numberParts.Number.Top + numberParts.Number.Height / 2);
            bool numberPart = (Part)typeof(MainForm).GetMethod("PartAt", hidden).Invoke(form, new object[] { 0, onNumber }) == Part.Number;
            Console.WriteLine("[{0}] the shortcuts, copied and saved, by number, each number a part of its card", numberedCards.Count == 3
                && cards.Items.Count == 3 && numberedCards[0].Number == 1 && numberedCards[1].Number == 2 && numberedCards[2].Number == 3
                && numberedCards.Exists(delegate(Entry e) { return !e.Saved; }) && numberPart ? "PASS" : "FAIL");
            Save(form, path + "-shortcuts.png");
            GoTo(form, PlaceKind.Saved, null);
            bool savedOnly = Cards(cards).Count == 4 && Cards(cards).TrueForAll(delegate(Entry e) { return e.Saved; }) && !(cards.Items[0] is Heading);
            GoTo(form, PlaceKind.Folder, s.Work);
            bool inWork = Cards(cards).Count == 2;
            GoTo(form, PlaceKind.Pinned, null);
            bool pinnedOnly = Cards(cards).Count == 1 && Cards(cards)[0].Pinned;
            Console.WriteLine("[{0}] saved, a folder with those inside it, pinned", savedOnly && inWork && pinnedOnly ? "PASS" : "FAIL");
            // The programs from that of the latest copy: PowerShell, then Edge.
            bool byTime = ((Place)nav.Items[8]).Origin.Name.Contains("PowerShell");
            nav.SelectedIndex = 9;
            bool fromEdge = Cards(cards).Count == 2 && byTime;
            GoTo(form, PlaceKind.History, null);
            TabStrip tabs = (TabStrip)typeof(MainForm).GetField("tabs", hidden).GetValue(form);
            tabs.Selected = 3;   // links
            bool links = Cards(cards).Count == 1 && Cards(cards)[0].Category == Category.Link;
            tabs.Selected = 0;
            Console.WriteLine("[{0}] a program, and a category of the tabs", fromEdge && links ? "PASS" : "FAIL");
            // The list hotkey opens on All, every kind, wherever the list was left (unless an option says otherwise).
            GoTo(form, PlaceKind.Folder, s.Work);
            tabs.Selected = 3;
            typeof(MainForm).GetMethod("ShowHistory", hidden).Invoke(form, null);
            Place opened = (Place)typeof(MainForm).GetField("place", hidden).GetValue(form);
            Console.WriteLine("[{0}] the list hotkey opens on the history", opened.Kind == PlaceKind.History && tabs.Selected == 0
                && Cards(cards).Count == 5 && ((Place)nav.SelectedItem).Kind == PlaceKind.History ? "PASS" : "FAIL");
            // It, and a copy, select the latest copy, with the top of the list in view: never the card selected before.
            Entry latest = null;
            foreach (Entry e in Cards(cards))
                if (!e.Saved && (latest == null || e.Time > latest.Time)) latest = e;
            cards.SelectedIndex = cards.Items.Count - 1;
            typeof(MainForm).GetMethod("ShowHistory", hidden).Invoke(form, null);
            bool onOpening = ((Entry)cards.SelectedItem).Id == latest.Id;
            cards.SelectedIndex = cards.Items.Count - 1;
            typeof(MainForm).GetField("newest", hidden).SetValue(form, true);   // as a copy does
            typeof(MainForm).GetMethod("ReloadCopied", hidden).Invoke(form, null);
            Console.WriteLine("[{0}] the list hotkey and a copy select the latest copy, the top in view", onOpening
                && ((Entry)cards.SelectedItem).Id == latest.Id && cards.TopIndex == 0 ? "PASS" : "FAIL");
            // A search in the history finds the saved texts too, after the copied ones, under a name of their own: the list
            // hotkey, a name and Enter still choose a saved password.
            TextBox find = (TextBox)typeof(MainForm).GetField("search", hidden).GetValue(form);
            find.Text = s.TestServer;
            // The name over them is a row, or, with no copied entry found before them, in the bar over the cards.
            Heading over = cards.Items.Count >= 2 && cards.Items[cards.Items.Count - 2] is Heading ? (Heading)cards.Items[cards.Items.Count - 2]
                : (Heading)typeof(MainForm).GetField("topDay", hidden).GetValue(form);
            Entry selectedFound = cards.SelectedItem as Entry;
            bool searched = over != null && over.Text == Strings.SavedTitle && selectedFound != null && selectedFound.Saved
                && selectedFound.Label == s.TestServer;
            // The words found are marked on the cards, in the name of a saved text too; found far in a long text, its card
            // shows that part of it, after an ellipsis.
            MethodInfo markedText = typeof(MainForm).GetMethod("MarkedText", hidden);
            bool nameMarked = selectedFound != null && markedText.Invoke(form, new object[] { selectedFound, 400 }) != null;
            string lastWord = s.Long.Substring(s.Long.LastIndexOf(' ') + 1);
            find.Text = lastWord;
            Application.DoEvents();
            Save(form, path + "-search.png");
            int longAt = RowOf(cards, delegate(Entry e) { return e.Text == s.Long; });
            Marked further = longAt < 0 ? null : (Marked)markedText.Invoke(form, new object[] { cards.Items[longAt], 120 });
            Marked plain = longAt < 0 ? null : (Marked)markedText.Invoke(form, new object[] { cards.Items[longAt], 4000 });
            Console.WriteLine("[{0}] the words found are marked, and a card shows where they are in a long text", nameMarked
                && further != null && further.Text.StartsWith("…") && further.Found[0].First < further.Shown
                && plain != null && !plain.Text.StartsWith("…") && plain.Lines.Count == 1 && !plain.Cut ? "PASS" : "FAIL");
            find.Text = "";
            Console.WriteLine("[{0}] a search in the history finds the saved texts too, under their name, and nothing saved without one",
                searched && !Cards(cards).Exists(delegate(Entry e) { return e.Saved; }) ? "PASS" : "FAIL");

            // A closed folder hides the folders inside it too, in the sidebar.
            List<Entry> sample = (List<Entry>)typeof(MainForm).GetField("entries", hidden).GetValue(form);
            Entry work = sample.Find(delegate(Entry e) { return e.IsGroup && e.Name == s.Work; });
            work.Open = false;
            typeof(MainForm).GetMethod("Filter", hidden).Invoke(form, null);
            int closedRows = nav.Items.Count;
            work.Open = true;
            typeof(MainForm).GetMethod("Filter", hidden).Invoke(form, null);
            Console.WriteLine("[{0}] a closed folder hides those inside it: {1} rows", closedRows == 10 ? "PASS" : "FAIL", closedRows);

            // The sidebar reduced to its signs leaves its room to the pane, with the same rows, and gets it back; its
            // width, as dragged, stays between its least and the room the pane leaves it.
            BindingFlags constant = BindingFlags.NonPublic | BindingFlags.Static;
            Control pane = (Control)typeof(MainForm).GetField("pane", hidden).GetValue(form);
            FieldInfo sideWidth = typeof(MainForm).GetField("sideWidth", hidden);
            MethodInfo placeSide = typeof(MainForm).GetMethod("PlaceSidebar", hidden);
            int wide = pane.Width, right = pane.Right, rows = nav.Items.Count;
            typeof(MainForm).GetMethod("NarrowSide", hidden).Invoke(form, new object[] { true });
            Application.DoEvents();
            Save(form, path + "-narrow.png");
            bool narrowed = pane.Width > wide && pane.Right == right && nav.Right < pane.Left && nav.Items.Count == rows
                && pane.Left == (int)((int)typeof(MainForm).GetField("NarrowSideWidth", constant).GetValue(null) * Look.Zoom);
            // Reduced, the name of a row waits for the pointer to rest, and goes when it leaves; with the names, never.
            MethodInfo rowTip = typeof(MainForm).GetMethod("RowTip", hidden);
            System.Windows.Forms.Timer tipWait = (System.Windows.Forms.Timer)typeof(MainForm).GetField("hoverTipWait", hidden).GetValue(form);
            rowTip.Invoke(form, new object[] { nav, 4, s.Home, false });
            bool waits = tipWait.Enabled && (string)typeof(MainForm).GetField("tipText", hidden).GetValue(form) == s.Home;
            typeof(MainForm).GetMethod("HideHoverTip", hidden).Invoke(form, null);
            waits &= !tipWait.Enabled;
            typeof(MainForm).GetMethod("NarrowSide", hidden).Invoke(form, new object[] { false });
            bool back = pane.Width == wide && pane.Right == right;
            rowTip.Invoke(form, new object[] { nav, 4, s.Home, false });
            waits &= !tipWait.Enabled;
            // With the names, a name cut short says itself whole all the same.
            rowTip.Invoke(form, new object[] { nav, 4, s.Home, true });
            waits &= tipWait.Enabled;
            typeof(MainForm).GetMethod("HideHoverTip", hidden).Invoke(form, null);
            int least = (int)typeof(MainForm).GetField("MinSideWidth", constant).GetValue(null);
            sideWidth.SetValue(form, 0);
            placeSide.Invoke(form, null);
            bool atLeast = (int)sideWidth.GetValue(form) == least && nav.Right < pane.Left;
            // Dragged as wide as it can be, it waits for a wider window.
            FieldInfo sizing = typeof(MainForm).GetField("sizingSide", hidden);
            sizing.SetValue(form, true);
            sideWidth.SetValue(form, 10000);
            placeSide.Invoke(form, null);
            sizing.SetValue(form, false);
            int slack = (int)typeof(MainForm).GetField("PaneSlack", constant).GetValue(null);
            bool atMost = pane.Right == right && pane.Width >= wide - (int)(slack * Look.Zoom) - 2 && pane.Width < wide;
            sideWidth.SetValue(form, typeof(MainForm).GetField("SideWidth", constant).GetValue(null));
            placeSide.Invoke(form, null);
            Console.WriteLine("[{0}] the sidebar reduced to its signs and back, its names in tooltips, dragged as narrow and as wide as it can be",
                narrowed && waits && back && atLeast && atMost && pane.Width == wide ? "PASS" : "FAIL");
            // The window narrower than designed, as far as the pane allows: further with the sidebar reduced.
            Size designed = form.Size;
            int leastPane = (int)(((int)typeof(MainForm).GetField("PaneInset", constant).GetValue(null) * 2
                + (int)typeof(MainForm).GetField("ContentWidth", constant).GetValue(null) - slack) * Look.Zoom);
            form.Width = 1;
            Application.DoEvents();
            Save(form, path + "-least.png");
            bool narrower = form.Width < designed.Width && pane.Width >= leastPane - 1 && pane.Width <= leastPane + 1;
            string steps = string.Format("names {0}/{1} pane {2} least {3}", form.Width, form.MinimumSize.Width, pane.Width, leastPane);
            // The thin bar over the scroll bar of the sidebar stays on it, whatever the width of the window.
            Control navRail = null;
            foreach (Control each in form.Controls) if (each is Rail && each.Top == nav.Top) navRail = each;
            narrower &= navRail != null && navRail.Right == nav.Right;
            steps += string.Format(", its rail ends at {0}, the sidebar at {1}", navRail == null ? -1 : navRail.Right, nav.Right);
            int withNames = form.Width;
            typeof(MainForm).GetMethod("NarrowSide", hidden).Invoke(form, new object[] { true });
            form.Width = 1;
            Application.DoEvents();
            Save(form, path + "-least-narrow.png");
            narrower &= form.Width < withNames && pane.Width >= leastPane - 1;
            steps += string.Format("; reduced {0}/{1} pane {2}", form.Width, form.MinimumSize.Width, pane.Width);
            // Given its names back in a window at its least, the sidebar keeps its width: the window widens.
            typeof(MainForm).GetMethod("NarrowSide", hidden).Invoke(form, new object[] { false });
            narrower &= (int)sideWidth.GetValue(form) == (int)typeof(MainForm).GetField("SideWidth", constant).GetValue(null)
                && form.Width >= withNames - 1;
            steps += string.Format("; names again {0}, sidebar {1}", form.Width, sideWidth.GetValue(form));
            form.Size = designed;
            placeSide.Invoke(form, null);
            Application.DoEvents();
            narrower &= navRail.Right == nav.Right;
            steps += string.Format("; its rail ends at {0}, the sidebar at {1}", navRail.Right, nav.Right);
            Console.WriteLine("[{0}] the window narrower than designed, down to the least pane: {1}; back to {2}, pane {3} of {4}",
                narrower && pane.Width == wide ? "PASS" : "FAIL", steps, form.Width, pane.Width, wide);
            TextBox search = (TextBox)typeof(MainForm).GetField("search", hidden).GetValue(form);

            // The icon menu: the saved texts between the first item and the rest, each folder a submenu.
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add(Strings.Open);
            menu.Items.Add(Strings.Exit);
            MethodInfo fill = typeof(MainForm).GetMethod("ShowSaved", hidden);
            fill.Invoke(form, new object[] { menu });
            fill.Invoke(form, new object[] { menu });   // made anew, not added twice
            // Folders side by side by name: in every language of the samples, Home comes before Work.
            ToolStripMenuItem workMenu = menu.Items[4] as ToolStripMenuItem;
            ToolStripMenuItem serverMenu = workMenu == null ? null : workMenu.DropDownItems[1] as ToolStripMenuItem;
            Console.WriteLine("[{0}] the icon menu lists the saved texts by folder: {1} items", menu.Items.Count == 7
                && menu.Items[1] is ToolStripSeparator && menu.Items[2].Text == s.Signature && workMenu != null && workMenu.Text == s.Work
                && workMenu.DropDownItems[0].Text == s.Address && serverMenu != null && serverMenu.Text == s.Servers
                && serverMenu.DropDownItems[0].Text == s.TestServer && menu.Items[3].Text == s.Home
                && menu.Items[5] is ToolStripSeparator && menu.Items[6].Text == Strings.Exit ? "PASS" : "FAIL", menu.Items.Count);
            menu.Dispose();

            // A copied picture saved under a name: among the saved ones, with its preview and its name.
            Clips sampleClips = ClipsOf(form);
            long pictureId = sampleClips.Copied(10, new string[0]).Find(delegate(Entry e) { return e.Picture; }).Id;
            sampleClips.Save(pictureId, s.Remote, 0);
            typeof(MainForm).GetMethod("Reload", hidden).Invoke(form, null);
            int copiedLeft = Cards(cards).FindAll(delegate(Entry e) { return !e.Saved; }).Count;   // gone from the history
            GoTo(form, PlaceKind.Saved, null);
            Application.DoEvents();
            Save(form, path + "-saved-picture.png");
            Entry savedPicture = Cards(cards).Find(delegate(Entry e) { return e.Picture; });
            Console.WriteLine("[{0}] a saved picture shows its preview and its name", savedPicture != null && savedPicture.Saved
                && savedPicture.Preview != null && savedPicture.Label == s.Remote && copiedLeft == 4 ? "PASS" : "FAIL");
            GoTo(form, PlaceKind.History, null);
            FillSample(form);

            // The window of the options, never shown yet, follows a change of theme all the same.
            Form options = (Form)typeof(MainForm).GetField("options", hidden).GetValue(form);
            ListBox pages = (ListBox)typeof(MainForm).GetField("pageList", hidden).GetValue(form);
            Look.Switch(!Look.Dark);
            bool followed = options.BackColor == Look.Back && pages.BackColor == Look.Well;
            Look.Switch(!Look.Dark);
            Console.WriteLine("[{0}] the window of the options follows the theme before it first shows", followed
                && options.BackColor == Look.Back && pages.BackColor == Look.Well ? "PASS" : "FAIL");
            options.Location = new Point(-30000, -30000);
            typeof(MainForm).GetMethod("ShowSize", hidden).Invoke(form, null);   // as opening them does
            options.Show();
            Application.DoEvents();
            Save(options, path + "-options.png");
            // Every page of the options, each in a picture of its own: -options-1 and on.
            for (int i = 0; i < pages.Items.Count; i++)
            {
                pages.SelectedIndex = i;
                Application.DoEvents();
                Save(options, path + "-options-" + (i + 1) + ".png");
            }
            pages.SelectedIndex = 0;
            // What the status line says, the window of the options says too; closed, it is only hidden.
            Label status = (Label)typeof(MainForm).GetField("status", hidden).GetValue(form);
            status.Text = Strings.ResumedSay;
            Label note = (Label)options.GetType().GetField("note", hidden).GetValue(options);
            bool told = note.Text == Strings.ResumedSay;
            status.Text = "";
            options.Close();
            Console.WriteLine("[{0}] the window of the options says what the status line says, and closing it only hides it",
                told && note.Text == "" && !options.Visible && !options.IsDisposed ? "PASS" : "FAIL");

            // Windows going dark or light with the window open: every colour given to a control follows.
            Look.Switch(!Look.Dark);
            Application.DoEvents();
            Save(form, path + "-switched.png");
            Panel searchCard = (Panel)search.Parent;
            Label count = (Label)typeof(MainForm).GetField("count", hidden).GetValue(form);
            Button typing = (Button)typeof(MainForm).GetField("typeButton", hidden).GetValue(form);
            Console.WriteLine("[{0}] a change of theme recolours the open window",
                form.BackColor == Look.Back && form.ForeColor == Look.Ink && cards.BackColor == Look.Well && search.BackColor == Look.Well
                && searchCard.BackColor == Look.Well && count.ForeColor == Look.Muted && typing.BackColor == Look.Accent
                && typing.FlatAppearance.BorderColor == Look.Accent ? "PASS" : "FAIL");
            Look.Switch(!Look.Dark);
            Console.WriteLine("[{0}] and back", form.BackColor == Look.Back && cards.BackColor == Look.Well ? "PASS" : "FAIL");
            // A theme chosen in the options stays, whatever Windows says; "as Windows" follows it again.
            bool wasDark = Look.Dark;
            Look.Choose(Theme.Dark);
            Look.Follow();
            bool keptDark = Look.Dark && form.BackColor == Look.Back;
            Look.Choose(Theme.Light);
            Look.Follow();
            Console.WriteLine("[{0}] a chosen theme stays when Windows changes", keptDark && !Look.Dark && form.BackColor == Look.Back
                ? "PASS" : "FAIL");
            Look.Choose(Theme.System);
            Look.Switch(wasDark);   // as the pictures were asked for

            // The status line with the longest message: two lines, and the lists give it room.
            status.Text = string.Format(Strings.DoneMany, 24) + string.Format(Strings.UnicodeMany, 2) + Strings.UnicodeHint;
            Application.DoEvents();
            Save(form, path + "-long.png");

            // The red bar over the status line, when the copied pictures fill their room: as if it were tiny.
            long space = Clips.PictureSpace;
            Clips.PictureSpace = 1;
            typeof(MainForm).GetMethod("ShowPictureRoom", hidden).Invoke(form, null);
            Application.DoEvents();
            Save(form, path + "-full.png");
            Control banner = (Control)typeof(MainForm).GetField("banner", hidden).GetValue(form);
            bool barred = banner.Visible && banner.Bottom <= status.Top && cards.Bottom <= banner.Top;
            string where = string.Format("shown {0}, cards end {1}, bar {2}-{3}, status from {4}, window shown {5}", banner.Visible, cards.Bottom,
                banner.Top, banner.Bottom, status.Top, form.Visible);
            Clips.PictureSpace = space;
            typeof(MainForm).GetMethod("ShowPictureRoom", hidden).Invoke(form, null);
            Console.WriteLine("[{0}] a red bar between the lists and the status line says the room of the pictures is full, and goes: {1}",
                barred && !banner.Visible ? "PASS" : "FAIL", where);

            status.Text = string.Format(Strings.Countdown, 3, string.Format(Strings.Quote, s.Remote));
            Application.DoEvents();
            Save(form, path + "-countdown.png");

            // The status line takes room only while it says something: silent, it leaves it to the lists.
            int crowded = cards.Height;
            status.Text = "";
            Console.WriteLine("[{0}] a silent status line leaves its room to the lists: {1} then {2}",
                cards.Height > crowded ? "PASS" : "FAIL", crowded, cards.Height);

            // The keys of a global hotkey: how they are named, which can be used, and how new ones are taken.
            BindingFlags shared = BindingFlags.NonPublic | BindingFlags.Static;
            Func<Keys, string> name = MainForm.NameOf;
            Func<Keys, bool> usable = delegate(Keys keys) { return (bool)typeof(MainForm).GetMethod("Usable", shared).Invoke(null, new object[] { keys }); };
            Console.WriteLine("[{0}] hotkey names: {1}, {2}", name(Keys.Control | Keys.Alt | Keys.V) == "Ctrl+Alt+V"
                && name(Keys.Control | Keys.Shift | Keys.D1) == "Ctrl+" + Strings.ShiftName + "+1" ? "PASS" : "FAIL",
                name(Keys.Control | Keys.Alt | Keys.V), name(Keys.Control | Keys.Shift | Keys.D1));
            Console.WriteLine("[{0}] a hotkey needs Ctrl or Alt and a letter, a digit or a function key",
                usable(Keys.Alt | Keys.F9) && !usable(Keys.V) && !usable(Keys.Shift | Keys.V) && !usable(Keys.Control | Keys.Enter) ? "PASS" : "FAIL");
            object typeBox = typeof(MainForm).GetField("hotkeyBox", hidden).GetValue(form);
            Action<Keys> press = delegate(Keys keys)
            {
                if (typeof(MainForm).GetField("capturing", hidden).GetValue(form) == null)
                    typeof(MainForm).GetMethod("AwaitKeys", hidden).Invoke(form, new object[] { typeBox });
                typeof(MainForm).GetMethod("TakeKeys", hidden).Invoke(form, new object[] { keys });
            };
            string before = MainForm.HotkeyName;
            press(Keys.Control | MainForm.SaveKey);   // the list answers to it: refused, and still waiting
            bool refusedTaken = MainForm.HotkeyName == before && typeof(MainForm).GetField("capturing", hidden).GetValue(form) != null;
            press(Keys.Control | Keys.Alt | Keys.F11);
            bool taken = MainForm.HotkeyName == "Ctrl+Alt+F11" && typeof(MainForm).GetField("capturing", hidden).GetValue(form) == null;
            press(Keys.Delete);   // back to the keys it starts with
            Console.WriteLine("[{0}] new keys for a hotkey: refused if the list uses them {1}, taken {2}, back to {3}",
                refusedTaken && taken && MainForm.HotkeyName == "Ctrl+Alt+V" ? "PASS" : "FAIL", refusedTaken, taken, MainForm.HotkeyName);
            press(Keys.Control | Keys.Shift | Keys.D3);   // the keys of a number
            bool refusedNumber = MainForm.HotkeyName == "Ctrl+Alt+V";
            press(Keys.Escape);
            // The keys of the numbers: what is held with any digit; never a combination of the other hotkeys.
            object quickBox = typeof(MainForm).GetField("quickBox", hidden).GetValue(form);
            Action<Keys> pressQuick = delegate(Keys keys)
            {
                if (typeof(MainForm).GetField("capturing", hidden).GetValue(form) == null)
                    typeof(MainForm).GetMethod("AwaitKeys", hidden).Invoke(form, new object[] { quickBox });
                typeof(MainForm).GetMethod("TakeKeys", hidden).Invoke(form, new object[] { keys });
            };
            string quickBefore = MainForm.NumbersName;
            pressQuick(Keys.Control | Keys.Alt | Keys.F11);   // not a digit
            bool noDigit = MainForm.NumbersName == quickBefore;
            pressQuick(Keys.Shift | Keys.D4);                  // nothing that is never typed
            bool shiftOnly = MainForm.NumbersName == quickBefore;
            pressQuick(Keys.Control | Keys.Alt | Keys.NumPad7);
            bool quickTaken = MainForm.NumbersName == "Ctrl+Alt+1…" + MainForm.MaxNumber;
            pressQuick(Keys.Delete);
            Console.WriteLine("[{0}] keys of the numbers: {1}; refused without a digit {2}, with Shift alone {3}, taken {4}, "
                + "and kept from the other hotkeys {5}; back to {6}", noDigit && shiftOnly && quickTaken && refusedNumber
                && MainForm.NumbersName == quickBefore && quickBefore == "Ctrl+" + Strings.ShiftName + "+1…" + MainForm.MaxNumber
                ? "PASS" : "FAIL", quickBefore, noDigit, shiftOnly, quickTaken, refusedNumber, MainForm.NumbersName);

            // From one PC to another: what the window exports, imported into a new, empty database, comes back whole,
            // but for pictures, which a text file cannot hold.
            string exported = System.IO.Path.Combine(sandbox, "to another pc.txt");
            ClipsOf(form).Save(ClipsOf(form).Copied(10, Texts.Words("github"))[0].Id, null, 0);   // a saved text without a name too
            typeof(MainForm).GetMethod("Reload", hidden).Invoke(form, null);
            List<Entry> sent = (List<Entry>)typeof(MainForm).GetMethod("Exported", hidden).Invoke(form, new object[] { true });
            BackupResult wrote = Backup.Export(exported, sent);
            using (Clips other = new Clips(System.IO.Path.Combine(sandbox, "other pc.db")))
            {
                BackupResult read = Backup.Import(other, exported, true, delegate { return false; });
                List<Entry> here = ClipsOf(form).Saved(), imported = other.Saved();
                bool same = here.Count == imported.Count;
                foreach (Entry each in here)
                {
                    // The file writes the line ends of Windows: a text with bare LF comes back with CR LF.
                    Entry match = imported.Find(delegate(Entry e)
                    {
                        return e.Name == each.Name && e.Text.Replace("\r\n", "\n") == each.Text.Replace("\r\n", "\n");
                    });
                    same &= match != null && match.FolderId != 0 == (each.FolderId != 0);
                }
                List<Folder> folders = other.Folders();
                Console.WriteLine("[{0}] exported and imported into another database: {1} saved, {2} copied, {3} pictures left out",
                    same && wrote.Saved == read.Saved && read.Copied == ClipsOf(form).CopiedCount() - wrote.Pictures
                    && wrote.Pictures == 1 && folders.Count == 3 && folders.Exists(delegate(Folder f) { return f.Name == s.Servers && f.Level == 1; })
                    ? "PASS" : "FAIL", read.Saved, read.Copied, wrote.Pictures);
            }
            FillSample(form);

            // Several copied entries moved into a folder are saved there, without a name, each in its own row, a picture
            // too, and one of the last copies as it came, formatted; the history has none left, and no bar of the day.
            GoTo(form, PlaceKind.History, null);
            List<Entry> moving = Cards(cards);
            Entry plainText = moving.Find(delegate(Entry e) { return !e.Picture && e.Category == Category.Text; });
            DataObject formattedCopy = new DataObject();
            Rich.Put(formattedCopy, "<b>" + System.Net.WebUtility.HtmlEncode(plainText.Text) + "</b>", null);
            Rich.Remember(plainText.Text, formattedCopy);
            typeof(MainForm).GetMethod("PickAll", hidden).Invoke(form, null);
            typeof(MainForm).GetMethod("MoveAll", hidden).Invoke(form, new object[] { moving, s.Work });
            long workId = ClipsOf(form).FindFolder(s.Work);
            List<Entry> nowSaved = ClipsOf(form).Saved();
            bool allThere = moving.TrueForAll(delegate(Entry m)
            {
                return nowSaved.Exists(delegate(Entry e) { return e.Id == m.Id && e.FolderId == workId && e.Name == null && e.Picture == m.Picture; });
            });
            Entry keptFormat = nowSaved.Find(delegate(Entry e) { return e.Id == plainText.Id; });
            Console.WriteLine("[{0}] copied entries moved together into a folder are saved there, a recent one formatted as it came",
                moving.Exists(delegate(Entry e) { return e.Picture; }) && allThere && keptFormat != null && keptFormat.Html != null
                && Cards(cards).Count == 0 && !dayBar.Visible && picked.Count == 0 ? "PASS" : "FAIL");
            // A long history of three days, scrolled down among the cards of yesterday: the bar says so.
            for (int i = 0; i < 30; i++) ClipsOf(form).Import(s.Long + " " + i, DateTime.Today.AddDays(-(i / 10)).AddHours(8).AddMinutes(i), false);
            typeof(MainForm).GetMethod("Filter", hidden).Invoke(form, null);
            cards.TopIndex = RowOf(cards, delegate(Entry e) { return e.Text == s.Long + " 14"; });
            showDay.Invoke(form, null);
            Application.DoEvents();
            Save(form, path + "-scrolled.png");
            Console.WriteLine("[{0}] scrolled among the cards of yesterday, the bar over them says so: {1}",
                dayBar.Text == Strings.Yesterday ? "PASS" : "FAIL", dayBar.Text);
            FillSample(form);

            // The folder of the database: moved elsewhere and back, the list going along; the setting saved at once.
            MethodInfo move = typeof(MainForm).GetMethod("MoveDataFolder", hidden);
            string elsewhere = System.IO.Path.Combine(sandbox, "elsewhere");
            int savedRows = cards.Items.Count;
            move.Invoke(form, new object[] { elsewhere });
            string written;
            using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(TestKey))
                written = key == null ? null : key.GetValue("DataFolder") as string;
            bool there = ClipsOf(form).FileName.StartsWith(elsewhere) && !System.IO.File.Exists(Clips.DefaultFile)
                && cards.Items.Count == savedRows && written == elsewhere;
            move.Invoke(form, new object[] { "" });
            Console.WriteLine("[{0}] the database moves to another folder with the list, saying so at once, and back",
                there && ClipsOf(form).FileName == Clips.DefaultFile && cards.Items.Count == savedRows
                && !System.IO.File.Exists(System.IO.Path.Combine(elsewhere, System.IO.Path.GetFileName(Clips.DefaultFile))) ? "PASS" : "FAIL");

            Console.WriteLine("form {0}x{1}, client {2}x{3}, icon {4}", form.Width, form.Height,
                form.ClientSize.Width, form.ClientSize.Height, form.Icon == null ? "none" : form.Icon.Size.ToString());
        }

        // The first start after a version before 4.0: the history files and the saved texts in the registry go into a
        // new database, and then they go, with the settings nothing reads any more.
        System.IO.File.Delete(Clips.DefaultFile);
        OldRecord(Migration.OldHistory, DateTime.Now.AddMinutes(-5), 1, false, 0, Encoding.UTF8.GetBytes("dalla versione 3"));
        OldSaved(TestKey + @"\Saved", "Dalla versione 3", Protected("vecchia password"), DateTime.Now, 0, null);
        using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(TestKey))
        {
            key.SetValue("Font", "Arial");
            key.SetValue("TextSize", 12);
            key.SetValue("SavedWidth", 200);
        }
        using (ShotForm form = new ShotForm())
        {
            bool settingsGone;
            using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(TestKey))
                settingsGone = key.OpenSubKey("Saved") == null && key.GetValue("Font") == null && key.GetValue("TextSize") == null
                    && key.GetValue("SavedWidth") == null;
            Console.WriteLine("[{0}] after the migration the old history files, saved texts and settings are gone, what they held is in the database",
                !System.IO.Directory.Exists(Migration.OldHistory) && settingsGone
                && ClipsOf(form).Copied(10, Texts.Words("versione")).Count == 1
                && ClipsOf(form).Saved().Exists(delegate(Entry e) { return e.Name == "Dalla versione 3"; }) ? "PASS" : "FAIL");
        }

        // A folder chosen for the database that is no longer there: no new, empty list in its place, but the list in
        // memory and the status line saying why.
        string gone = System.IO.Path.Combine(sandbox, "gone away");
        using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(TestKey)) key.SetValue("DataFolder", gone);
        using (ShotForm form = new ShotForm())
        {
            Label status = (Label)typeof(MainForm).GetField("status", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form);
            Console.WriteLine("[{0}] a chosen folder that is gone is not made again: the list works in memory and says so",
                ClipsOf(form).FileName == Clips.Memory && !System.IO.Directory.Exists(gone) && status.Text.IndexOf(gone) >= 0 ? "PASS" : "FAIL");
        }
        using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(TestKey)) key.DeleteValue("DataFolder", false);

        using (ShotEdit edit = new ShotEdit(SavedSample(), Sample.Current.Work, new List<string>(new string[] { Sample.Current.Work, Sample.Current.Home })))
        {
            edit.StartPosition = FormStartPosition.Manual;
            edit.Location = new Point(-30000, -30000);
            edit.Show();
            Application.DoEvents();
            Save(edit, path + "-edit.png");
        }
        // The same window for a copied picture: the picture in place of the content.
        using (Bitmap picture = SamplePicture())
        using (ShotEdit edit = new ShotEdit(new Entry(picture), null, new List<string>()))
        {
            edit.StartPosition = FormStartPosition.Manual;
            edit.Location = new Point(-30000, -30000);
            edit.Show();
            Application.DoEvents();
            Save(edit, path + "-edit-picture.png");
        }

        // The small window that shows an entry whole: a text of several lines, then a picture.
        using (Peek peek = new Peek())
        using (Bitmap picture = SamplePicture())
        {
            peek.Location = new Point(-30000, -30000);
            peek.Fill(new Entry(null, "ssh admin@srv-database-01\r\ncd /var/log\r\n\ttail -f syslog\r\n\r\n"
                + "Una riga più lunga del riquadro, che va a capo da sola quando arriva al bordo destro e continua sotto."), 1f);
            peek.Show();
            Application.DoEvents();
            Save(peek, path + "-peek.png");
            peek.Fill(new Entry(picture), 1f);
            Application.DoEvents();
            Save(peek, path + "-peek-picture.png");
        }
        return 0;
    }

    // Photographs the windows as the screen shows them: what Windows draws (title bar, themed scroll bars
    // and boxes) is missing from the pictures of 'shot'. The windows appear for a moment, without taking the focus.
    static int Real(string path)
    {
        // Behind the windows the plain background of the Store pictures, not whatever is on the screen: the
        // corners and the invisible border of a window let it show.
        using (Backdrop backdrop = new Backdrop(Screen.PrimaryScreen.Bounds))
        using (ShotForm form = new ShotForm())
        {
            backdrop.Show();
            form.TopMost = true;
            form.StartPosition = FormStartPosition.Manual;
            form.Location = new Point(80, 80);
            BindingFlags hidden = BindingFlags.NonPublic | BindingFlags.Instance;
            FillSample(form, Wider);
            form.Show();
            form.BringToFront();
            Snap(form, path + "-real-list.png");
            // An entry shown whole from the eye of its card; resting the pointer would show only one the card cuts.
            ListBox recent = (ListBox)typeof(MainForm).GetField("cardList", hidden).GetValue(form);
            int longRow = RowOf(recent, delegate(Entry e) { return e.Text == Sample.Current.Long; });
            typeof(MainForm).GetMethod("ShowPeek", hidden).Invoke(form, new object[] { recent, longRow, true });
            Form shownPeek = (Form)typeof(MainForm).GetField("peek", hidden).GetValue(form);
            Snap(form, path + "-real-peek.png");
            Console.WriteLine("[{0}] the eye shows an entry whole; resting on one that fits would not", shownPeek.Visible
                && !(bool)typeof(MainForm).GetMethod("Cut", hidden).Invoke(form, new object[] { recent, new Entry(null, "ok"), recent.GetItemRectangle(longRow) })
                ? "PASS" : "FAIL");
            // A click on the eye shows the entry whole, another one on the same eye puts it away.
            typeof(MainForm).GetMethod("HidePeek", hidden).Invoke(form, null);
            CardParts longParts = (CardParts)typeof(MainForm).GetMethod("Lay", hidden).Invoke(form,
                new object[] { recent.Items[longRow], recent.GetItemRectangle(longRow) });
            Rectangle eye = longParts.Buttons[Part.Show];
            ClickAt(recent, new Point(eye.Left + eye.Width / 2, eye.Top + eye.Height / 2));
            bool opened = shownPeek.Visible;
            ClickAt(recent, new Point(eye.Left + eye.Width / 2, eye.Top + eye.Height / 2));
            Console.WriteLine("[{0}] a click on the eye shows the entry whole, another one puts it away", opened && !shownPeek.Visible
                ? "PASS" : "FAIL");
            // A long list, with the thin bar that scrolls it, moved a little way down.
            for (int i = 0; i < 40; i++) ClipsOf(form).Import(Sample.Current.Long + " " + i, DateTime.Today.AddMinutes(i), false);
            typeof(MainForm).GetMethod("Filter", hidden).Invoke(form, null);
            recent.TopIndex = 10;
            Snap(form, path + "-real-scroll.png");
            // Reduced to its signs, the sidebar says the name of the row the pointer rests on, on its right.
            ListBox side = (ListBox)typeof(MainForm).GetField("navList", hidden).GetValue(form);
            typeof(MainForm).GetMethod("NarrowSide", hidden).Invoke(form, new object[] { true });
            typeof(MainForm).GetMethod("RowTip", hidden).Invoke(form, new object[] { side, 4, Sample.Current.Home, false });
            for (DateTime until = DateTime.Now.AddSeconds(1); DateTime.Now < until; System.Threading.Thread.Sleep(20)) Application.DoEvents();
            Snap(form, path + "-real-narrow.png");
            typeof(MainForm).GetMethod("NarrowSide", hidden).Invoke(form, new object[] { false });
            FillSample(form, Wider);
            typeof(MainForm).GetMethod("OpenOptions", hidden).Invoke(form, null);
            Form options = (Form)typeof(MainForm).GetField("options", hidden).GetValue(form);
            Snap(options, path + "-real-options.png");
            options.Hide();
            using (ShotEdit edit = new ShotEdit(SavedSample(), Sample.Current.Work, new List<string>(new string[] { Sample.Current.Work, Sample.Current.Home })))
            {
                edit.TopMost = true;
                edit.StartPosition = FormStartPosition.Manual;
                edit.Location = new Point(80, 80);
                edit.Show();
                Snap(edit, path + "-real-edit.png");
            }
            // A question of the program's own, in place of a message box of Windows.
            using (Form ask = (Form)Activator.CreateInstance(typeof(AskForm), hidden, null,
                new object[] { string.Format(Strings.AskDelete, Sample.Current.Signature), true }, null))
            {
                ask.TopMost = true;
                ask.StartPosition = FormStartPosition.Manual;
                ask.Location = new Point(80, 80);
                ask.Show();
                Snap(ask, path + "-real-ask.png");
            }
            // The menu of the icon with a folder open, and a tooltip, over the window.
            using (ContextMenuStrip menu = new ContextMenuStrip())
            {
                Look.Mark(menu.Items.Add(Strings.Open), Sign.Open);
                Look.Mark(menu.Items.Add(Strings.Pause), Sign.Pause);
                Look.Mark(menu.Items.Add(Strings.Exit), Sign.Exit);
                Look.Style(menu);
                typeof(MainForm).GetMethod("ShowSaved", hidden).Invoke(form, new object[] { menu });
                menu.Show(new Point(form.Left + 60, form.Top + 120));
                ToolStripMenuItem work = (ToolStripMenuItem)menu.Items[4];
                work.ShowDropDown();
                work.DropDownItems[0].Select();
                SnapArea(new Rectangle(form.Left + 40, form.Top + 100, 520, 300), path + "-real-menu.png");
                menu.Close();
            }
            // The menu of a saved text with the numbers it can take, each beside the entry that has it.
            ListBox saved = (ListBox)typeof(MainForm).GetField("cardList", hidden).GetValue(form);
            ContextMenuStrip listMenu = (ContextMenuStrip)typeof(MainForm).GetField("listMenu", hidden).GetValue(form);
            // A text without a number, not a folder, which has no numbers to take: among the saved ones, not in the history.
            GoTo(form, PlaceKind.Saved, null);
            saved.SelectedIndex = RowOf(saved, delegate(Entry e) { return e.Label == Sample.Current.Address; });
            listMenu.Show(saved, new Point(40, 60));
            foreach (ToolStripItem item in listMenu.Items)
            {
                ToolStripMenuItem numbers = item as ToolStripMenuItem;
                if (numbers == null || numbers.Text != Strings.NumberMenu) continue;
                numbers.ShowDropDown();
                numbers.DropDownItems[1].Select();
            }
            SnapArea(new Rectangle(saved.PointToScreen(Point.Empty).X, form.Top + 100, 640, 420), path + "-real-number.png");
            listMenu.Close();
            GoTo(form, PlaceKind.History, null);
            ToolTip tips = (ToolTip)typeof(MainForm).GetField("tips", hidden).GetValue(form);
            Control typing = (Control)typeof(MainForm).GetField("typeButton", hidden).GetValue(form);
            tips.Show(tips.GetToolTip(typing), typing, -200, -40);
            SnapArea(Rectangle.Inflate(form.Bounds, 40, 40), path + "-real-tip.png");
            tips.Hide(typing);
            using (ShotHelp help = new ShotHelp())
            {
                help.TopMost = true;
                help.Show(form);
                Snap(help, path + "-real-help.png");
                // A subject with keys in a column of their own.
                ListBox topics = (ListBox)typeof(HelpForm).GetField("topics", hidden).GetValue(help);
                topics.SelectedIndex = topics.Items.Count / 2;
                Snap(help, path + "-real-help-keys.png");
            }
        }
        return 0;
    }

    // The pictures of the Store listing: the windows as the screen shows them, with made-up entries, on a plain
    // background of the size the Store asks for (1366 x 768, or 1920 x 1080 where the windows need it), and the
    // icon at 300 x 300. The windows appear for a moment, without taking the focus; SmartClip must be closed,
    // or its hotkeys are taken and the status line says so.
    static int Store(string folder)
    {
        const int Top = 40;   // the windows high up: the Store may lay text over the bottom third
        // The pictures in a folder of their language; the icon, the same for all, beside those folders.
        string pictures = System.IO.Path.Combine(folder, Strings.Code);
        System.IO.Directory.CreateDirectory(pictures);
        BindingFlags hidden = BindingFlags.NonPublic | BindingFlags.Instance;
        Look.Use(true);   // dark, whatever Windows has, which stands out more in the Store: only the last picture is light
        SaveTile(System.IO.Path.Combine(folder, "tile-300.png"));
        using (ShotForm form = new ShotForm())
        {
            form.TopMost = true;
            form.ShowInTaskbar = false;
            form.StartPosition = FormStartPosition.Manual;
            form.Location = new Point(-30000, -30000);
            FillSample(form, Wider);
            form.Show();   // now it has the size of this screen's scale
            Rectangle screen = Screen.PrimaryScreen.Bounds;
            Size size = form.Width + 2 * Top <= 1366 && form.Height + 2 * Top <= 768 ? new Size(1366, 768) : new Size(1920, 1080);
            if (size.Width > screen.Width || size.Height > screen.Height)
            {
                Console.WriteLine("FAIL: the screen is smaller than {0} x {1}", size.Width, size.Height);
                return 1;
            }
            Rectangle area = new Rectangle(screen.Location, size);
            using (Backdrop backdrop = new Backdrop(area))
            {
                backdrop.Show();
                form.Location = new Point(area.Left + (area.Width - form.Width) / 2, area.Top + Top);
                form.BringToFront();   // above the backdrop, which came later
                Label status = (Label)typeof(MainForm).GetField("status", hidden).GetValue(form);
                status.Text = "";
                ListBox recent = (ListBox)typeof(MainForm).GetField("cardList", hidden).GetValue(form);
                recent.SelectedIndex = RowOf(recent, delegate(Entry e) { return e.Pinned; });
                SnapArea(area, System.IO.Path.Combine(pictures, "1-list.png"));

                // Typing a password: the countdown, with the button that cancels it.
                Button typeButton = (Button)typeof(MainForm).GetField("typeButton", hidden).GetValue(form);
                status.Text = string.Format(Strings.Countdown, 3, string.Format(Strings.Quote, Sample.Current.Remote));
                typeButton.Text = Strings.Cancel;
                SnapArea(area, System.IO.Path.Combine(pictures, "2-typing.png"));
                status.Text = "";
                typeButton.Text = Strings.TypeButton;

                // A copied picture shown whole, larger, from the eye of its card: its card at the top, so that it fits under
                // it. The compact cards all fit in the list, which then cannot scroll: the tab of the pictures does it.
                TabStrip kinds = (TabStrip)typeof(MainForm).GetField("tabs", hidden).GetValue(form);
                kinds.Selected = Array.IndexOf((Category?[])typeof(MainForm).GetField("TabKinds", BindingFlags.NonPublic | BindingFlags.Static)
                    .GetValue(null), (Category?)Category.Picture);
                Application.DoEvents();
                int pictureRow = RowOf(recent, delegate(Entry e) { return e.Picture; });
                recent.TopIndex = pictureRow;
                typeof(MainForm).GetMethod("ShowPeek", hidden).Invoke(form, new object[] { recent, pictureRow, true });
                SnapArea(area, System.IO.Path.Combine(pictures, "3-preview.png"));
                typeof(MainForm).GetMethod("HidePeek", hidden).Invoke(form, null);
                kinds.Selected = 0;
                Application.DoEvents();
                recent.TopIndex = 0;

                // A copied signature being saved under a name, into a folder, with its formatting: as just copied
                // from a mail, which the program remembers.
                Sample s = Sample.Current;
                string servers = Texts.Join(s.Work, s.Servers);
                DataObject copiedSignature = new DataObject();
                Rich.Put(copiedSignature, SavedSample().Html, null);
                Rich.Remember(s.SignatureText, copiedSignature);
                using (ShotEdit edit = new ShotEdit(new Entry(null, s.SignatureText), servers, new List<string>(new string[] { s.Work, servers, s.Home })))
                {
                    edit.TopMost = true;
                    edit.StartPosition = FormStartPosition.Manual;
                    ((TextBox)typeof(EditForm).GetField("nameBox", hidden).GetValue(edit)).Text = s.Signature;
                    edit.Show();
                    edit.Location = new Point(form.Left + (form.Width - edit.Width) / 2, form.Top + (form.Height - edit.Height) / 2);
                    SnapArea(area, System.IO.Path.Combine(pictures, "4-save.png"));
                }

                // The menu of the icon by the clock, with the saved texts in their folders, alone on the background.
                form.Hide();
                using (ContextMenuStrip menu = new ContextMenuStrip())
                {
                    Look.Mark(menu.Items.Add(Strings.Open), Sign.Open);   // with their signs, as MainForm gives them
                    Look.Mark(menu.Items.Add(Strings.Pause), Sign.Pause);
                    Look.Mark(menu.Items.Add(Strings.Exit), Sign.Exit);
                    Look.Style(menu);
                    typeof(MainForm).GetMethod("ShowSaved", hidden).Invoke(form, new object[] { menu });
                    menu.Show(new Point(area.Left + area.Width / 3, area.Top + Top * 3));
                    ToolStripMenuItem work = (ToolStripMenuItem)menu.Items[4];
                    work.ShowDropDown();
                    ((ToolStripMenuItem)work.DropDownItems[1]).ShowDropDown();
                    SnapArea(area, System.IO.Path.Combine(pictures, "5-tray-menu.png"));
                    menu.Close();
                }
                form.Show();
                form.BringToFront();

                // The options at the page of the hotkeys, where the numbers are pasted from.
                ListBox pages = (ListBox)typeof(MainForm).GetField("pageList", hidden).GetValue(form);
                pages.SelectedIndex = 1;
                typeof(MainForm).GetMethod("OpenOptions", hidden).Invoke(form, null);
                SnapArea(area, System.IO.Path.Combine(pictures, "6-options.png"));
                ((Form)typeof(MainForm).GetField("options", hidden).GetValue(form)).Hide();
                pages.SelectedIndex = 0;

                // The guide, open at how a password is typed: the fifth subject in every language.
                using (ShotHelp help = new ShotHelp())
                {
                    help.TopMost = true;
                    help.Show(form);
                    help.Location = new Point(area.Left + (area.Width - help.Width) / 2, area.Top + Top);
                    ((ListBox)typeof(HelpForm).GetField("topics", hidden).GetValue(help)).SelectedIndex = 4;
                    SnapArea(area, System.IO.Path.Combine(pictures, "7-guide.png"));
                }

                // The same list in the light, as when Windows is set so for its apps.
                Look.Switch(false);
                SnapArea(area, System.IO.Path.Combine(pictures, "8-light.png"));
                Look.Switch(true);
            }
        }
        Console.WriteLine("pictures in {0}", pictures);
        return 0;
    }

    // The icon for the Store listing, 300 x 300: its largest frame, a PNG inside the .ico, in the middle as it
    // is, rather than enlarged.
    static void SaveTile(string path)
    {
        byte[] bytes = System.IO.File.ReadAllBytes(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\SmartClip.ico"));
        int best = -1, side = 0;
        for (int i = 0; i < BitConverter.ToUInt16(bytes, 4); i++)
        {
            int entry = 6 + 16 * i, width = bytes[entry] == 0 ? 256 : bytes[entry];
            if (width > side) { side = width; best = entry; }
        }
        int length = BitConverter.ToInt32(bytes, best + 8), offset = BitConverter.ToInt32(bytes, best + 12);
        using (System.IO.MemoryStream png = new System.IO.MemoryStream(bytes, offset, length))
        using (Image icon = Image.FromStream(png))
        using (Bitmap tile = new Bitmap(300, 300))
        {
            using (Graphics g = Graphics.FromImage(tile)) g.DrawImage(icon, (300 - icon.Width) / 2, (300 - icon.Height) / 2, icon.Width, icon.Height);
            tile.Save(path, ImageFormat.Png);
        }
    }

    static void SnapArea(Rectangle area, string path)
    {
        for (int i = 0; i < 40; i++)
        {
            Application.DoEvents();
            Thread.Sleep(10);
        }
        using (Bitmap bmp = new Bitmap(area.Width, area.Height))
        {
            using (Graphics g = Graphics.FromImage(bmp)) g.CopyFromScreen(area.Location, Point.Empty, area.Size);
            bmp.Save(path, ImageFormat.Png);
        }
    }

    static void Snap(Form form, string path)
    {
        for (int i = 0; i < 40; i++)
        {
            Application.DoEvents();
            Thread.Sleep(10);
        }
        using (Bitmap bmp = new Bitmap(form.Width, form.Height))
        {
            using (Graphics g = Graphics.FromImage(bmp)) g.CopyFromScreen(form.Location, Point.Empty, bmp.Size);
            bmp.Save(path, ImageFormat.Png);
        }
    }

    // The pictures of the Store and of the README show the window this much wider than designed (pixels at 96 dpi),
    // half to each column, as a user would leave it: at the default size of the entries the names of the saved
    // texts are cut short.
    const int Wider = 160;

    // Fills the list with made-up entries instead of the user's own, which must not end up in a picture.
    static void FillSample(MainForm form)
    {
        FillSample(form, 0);
    }

    // The saved text the window of a saved entry shows in the pictures.
    static Entry SavedSample()
    {
        Entry signature = new Entry(Sample.Current.Signature, Sample.Current.SignatureText);
        signature.Saved = true;
        // Formatted, as a signature copied from a mail is: the greeting plain, the name standing out, the rest small.
        string[] lines = Sample.Current.SignatureText.Split('\n');
        signature.Html = lines[0] + "<br><span style=\"font-family:Georgia;font-size:15pt;font-weight:bold;color:#2e75b6\">" + lines[1]
            + "</span><br><br><span style=\"font-size:9pt;color:#7f7f7f\"><i>" + lines[3] + "</i><br>" + lines[4] + "</span>";
        return signature;
    }

    // The database of a window of the checks: one of the sandbox, never the user's.
    static Clips ClipsOf(MainForm form)
    {
        return (Clips)typeof(MainForm).GetField("clips", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form);
    }

    static void FillSample(MainForm form, int wider)
    {
        BindingFlags hidden = BindingFlags.NonPublic | BindingFlags.Instance;
        Sample s = Sample.Current;
        Clips clips = ClipsOf(form);
        clips.Data.Execute("DELETE FROM entries; DELETE FROM folders");
        // Saved texts: one outside the folders with a number, then folders, one open with another inside it and one
        // closed, each with its texts.
        clips.Number(clips.AddSaved(s.Signature, s.SignatureText, SavedSample().Html, null, 0, DateTime.Now), 1);   // the keys that choose it, shown in its row
        long work = clips.AddFolder(0, s.Work), servers = clips.AddFolder(work, s.Servers), home = clips.AddFolder(0, s.Home);
        clips.AddSaved(s.Address, "not shown", work, DateTime.Now);
        clips.Number(clips.AddSaved(s.TestServer, "not shown", servers, DateTime.Now), 2);
        clips.AddSaved(s.Wifi, "not shown", home, DateTime.Now);
        clips.OpenFolder(home, false);
        // Copied this morning, whatever the time the pictures are taken.
        DateTime morning = DateTime.Today.AddHours(10).AddMinutes(24);
        clips.Import("ssh admin@srv-database-01", morning, true);
        clips.Number(clips.Copied(1, new string[0])[0].Id, 3);
        using (Bitmap picture = SamplePicture())
        using (Bitmap preview = new Bitmap(picture, 213, 120))
            clips.AdoptPicture(Entry.ToPng(picture), Entry.ToPng(preview), picture.Width, picture.Height, morning.AddMinutes(-7), false, 0);
        clips.Import(s.Long, morning.AddMinutes(-19), false);
        clips.Import(s.TwoLines, morning.AddMinutes(-33), false);
        clips.Import("https://github.com/giongy/SmartClip", morning.AddMinutes(-52), false);
        // Each copied in a program every Windows has: the link and the picture in the browser, the command in the shell.
        string system = Environment.SystemDirectory, edge = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Microsoft\Edge\Application\msedge.exe");
        foreach (Entry entry in clips.Copied(10, new string[0]))
            clips.Attribute(entry.Id, entry.Picture || entry.Text.StartsWith("http") ? edge
                : entry.Text.StartsWith("ssh") ? System.IO.Path.Combine(system, @"WindowsPowerShell\v1.0\powershell.exe")
                : System.IO.Path.Combine(system, "notepad.exe"));
        typeof(MainForm).GetMethod("Reload", hidden).Invoke(form, null);
        // The window as designed (or wider by the given amount), not as the user left it: every picture the same.
        typeof(MainForm).GetField("savedSize", hidden).SetValue(form, wider == 0 ? Size.Empty
            : new Size((int)Math.Round(form.Width / Look.Zoom) + wider, (int)Math.Round(form.Height / Look.Zoom)));
    }

    // The entries of the cards, without the names of the days.
    static List<Entry> Cards(ListBox cards)
    {
        List<Entry> found = new List<Entry>();
        foreach (object row in cards.Items) if (row is Entry) found.Add((Entry)row);
        return found;
    }

    // The row of the first card that matches, or -1.
    static int RowOf(ListBox cards, Predicate<Entry> match)
    {
        for (int i = 0; i < cards.Items.Count; i++)
            if (cards.Items[i] is Entry && match((Entry)cards.Items[i])) return i;
        return -1;
    }

    // A click of the left button at a point of a control, as Windows tells it: down, click, up.
    static void ClickAt(Control control, Point at)
    {
        MouseEventArgs click = new MouseEventArgs(MouseButtons.Left, 1, at.X, at.Y, 0);
        foreach (string step in new string[] { "OnMouseDown", "OnMouseClick", "OnMouseUp" })
            typeof(Control).GetMethod(step, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(control, new object[] { click });
        Application.DoEvents();
    }

    // The left button pressed and released on the left of a row, as Windows sends it, with the keys held ('keys': MK_).
    static void Press(ListBox list, int row, int keys)
    {
        Rectangle area = list.GetItemRectangle(row);
        IntPtr at = (IntPtr)(((area.Top + area.Height / 2) << 16) | (area.Left + 40));
        const int MK_LBUTTON = 0x0001, WM_LBUTTONUP = 0x0202;
        Native.SendMessage(list.Handle, Native.WM_LBUTTONDOWN, (IntPtr)(MK_LBUTTON | keys), at);
        Native.SendMessage(list.Handle, WM_LBUTTONUP, (IntPtr)keys, at);
        Application.DoEvents();
    }

    // The heights the cards of the history and of the saved texts have, each once; the history shown again after.
    static List<int> CardHeights(MainForm form, ListBox cards)
    {
        List<int> heights = new List<int>();
        foreach (PlaceKind kind in new PlaceKind[] { PlaceKind.Saved, PlaceKind.History })
        {
            GoTo(form, kind, null);
            for (int i = 0; i < cards.Items.Count; i++)
                if (cards.Items[i] is Entry && !heights.Contains(cards.GetItemHeight(i))) heights.Add(cards.GetItemHeight(i));
        }
        return heights;
    }

    // Shows a place of the sidebar, as a click on it does.
    static void GoTo(MainForm form, PlaceKind kind, string path)
    {
        Place place = new Place(kind);
        place.Path = path;
        typeof(MainForm).GetMethod("Go", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(form, new object[] { place });
    }

    // Something that looks like a screenshot: a window with a title bar on a coloured desktop.
    static Bitmap SamplePicture()
    {
        Bitmap picture = new Bitmap(1920, 1080);
        using (Graphics g = Graphics.FromImage(picture))
        using (Brush desk = new System.Drawing.Drawing2D.LinearGradientBrush(new Rectangle(0, 0, 1920, 1080), Color.SteelBlue, Color.MediumPurple, 30f))
        {
            g.FillRectangle(desk, 0, 0, 1920, 1080);
            g.FillRectangle(Brushes.WhiteSmoke, 300, 180, 1100, 700);
            g.FillRectangle(Brushes.DimGray, 300, 180, 1100, 60);
        }
        return picture;
    }

    static void Save(Form form, string path)
    {
        using (Bitmap bmp = new Bitmap(form.Width, form.Height))
        {
            form.DrawToBitmap(bmp, new Rectangle(0, 0, form.Width, form.Height));
            bmp.Save(path, ImageFormat.Png);
        }
    }

    static int TypeTest()
    {
        Form form = new Form();
        form.Text = Program.AppName + " - test automatico (non toccare)";
        form.TopMost = true;
        form.StartPosition = FormStartPosition.CenterScreen;
        form.ClientSize = new Size(560, 140);
        TextBox box = new TextBox();
        box.Multiline = true;
        box.AcceptsTab = true;
        box.Dock = DockStyle.Fill;
        form.Controls.Add(box);
        form.Show();
        form.Activate();
        box.Focus();

        Stopwatch wait = Stopwatch.StartNew();
        while (Native.GetForegroundWindow() != form.Handle && wait.ElapsedMilliseconds < 2000)
        {
            Application.DoEvents();
            Thread.Sleep(20);
        }
        if (Native.GetForegroundWindow() != form.Handle)
        {
            Console.WriteLine("INCONCLUSIVE: the test window did not get the foreground");
            form.Dispose();
            return 2;
        }
        Console.WriteLine("caps lock on: {0}", (Native.GetKeyState(0x14) & 1) != 0);

        int failures = 0;
        string all = Ascii() + Extra + "\tZ";
        failures += Case(form, box, "local keys + unicode fallback", all, false, false, all);
        string odd = "a~`{}È€ñΩz";
        failures += Case(form, box, "alt codes", odd, true, false, odd);
        failures += Case(form, box, "newline + final enter", "ab\ncd", false, true, "ab\r\ncd\r\n");

        TypeOptions o = new TypeOptions();
        TypeResult lost = Typer.TypeText("abc", new IntPtr(1), o, delegate { return false; });
        bool guard = lost.Outcome == TypeOutcome.FocusLost && lost.Typed == 0;
        Console.WriteLine("[{0}] wrong target is refused: outcome={1}", guard ? "PASS" : "FAIL", lost.Outcome);
        TypeResult stopped = Typer.TypeText("abc", form.Handle, o, delegate { return true; });
        bool stop = stopped.Outcome == TypeOutcome.Cancelled && stopped.Typed == 0;
        Console.WriteLine("[{0}] cancel stops before the first key: outcome={1}", stop ? "PASS" : "FAIL", stopped.Outcome);
        Application.DoEvents();
        bool clean = box.Text == "ab\r\ncd\r\n";
        Console.WriteLine("[{0}] nothing was typed by the two refused runs", clean ? "PASS" : "FAIL");

        // Pasting, as the keys of a number do: Ctrl+V into the window. The user's clipboard text comes back after.
        string kept = Clipboard.ContainsText() ? Clipboard.GetText() : null;
        string pasted = "pasted € è\r\nsecond line";
        Clipboard.SetText(pasted);
        box.Clear();
        IntPtr window = form.Handle;
        Task<TypeOutcome> paste = Task.Run(delegate { return Typer.Paste(window, 15); });
        while (!paste.IsCompleted)
        {
            Application.DoEvents();
            Thread.Sleep(2);
        }
        for (int i = 0; i < 20 && box.Text != pasted; i++)
        {
            Application.DoEvents();
            Thread.Sleep(20);
        }
        bool pastedOk = paste.Result == TypeOutcome.Done && box.Text == pasted;
        Console.WriteLine("[{0}] Ctrl+V pastes the clipboard: outcome={1}, text={2}", pastedOk ? "PASS" : "FAIL", paste.Result, Show(box.Text));
        bool pasteGuard = Typer.Paste(new IntPtr(1), 15) == TypeOutcome.FocusLost;
        Console.WriteLine("[{0}] but not into a window that is not in front", pasteGuard ? "PASS" : "FAIL");
        if (kept != null) Clipboard.SetText(kept);
        else Clipboard.Clear();

        form.Dispose();
        return failures == 0 && guard && stop && clean && pastedOk && pasteGuard ? 0 : 1;
    }

    static int Case(Form form, TextBox box, string name, string text, bool altCodes, bool pressEnter, string expected)
    {
        box.Clear();
        Application.DoEvents();
        TypeOptions o = new TypeOptions();
        o.AltCodes = altCodes;
        o.PressEnter = pressEnter;
        IntPtr target = form.Handle;
        Task<TypeResult> task = Task.Run(delegate
        {
            return Typer.TypeText(text, target, o, delegate { return false; });
        });
        while (!task.IsCompleted)
        {
            Application.DoEvents();
            Thread.Sleep(2);
        }
        for (int i = 0; i < 20; i++)
        {
            Application.DoEvents();
            Thread.Sleep(10);
        }
        TypeResult r = task.Result;
        bool ok = r.Outcome == TypeOutcome.Done && box.Text == expected;
        Console.WriteLine("[{0}] {1}: outcome={2} typed={3} unicode={4} altcoded={5}", ok ? "PASS" : "FAIL", name,
            r.Outcome, r.Typed, r.Unicode, r.AltCoded);
        if (!ok)
        {
            Console.WriteLine("  expected: " + Show(expected));
            Console.WriteLine("  actual:   " + Show(box.Text));
        }
        return ok ? 0 : 1;
    }
}
