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
    public string Signature, SignatureText, Work, Address, Servers, ServerPassword, Home, Wifi, Long, TwoLines, Remote;
    public string Probe;   // a word found only in the server password, for a search

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
                    s.ServerPassword = "Password del server di prova"; s.Home = "Casa"; s.Wifi = "Wi-Fi di casa";
                    s.Long = "Una frase copiata più lunga della finestra, che finisce tagliata sul bordo destro con i puntini";
                    s.TwoLines = "prima riga\r\nseconda riga"; s.Remote = "srv-database-01 - Connessione Desktop remoto"; s.Probe = "prova";
                    break;
                case "es":
                    s.Signature = "Firma de correo"; s.SignatureText = "Saludos cordiales,\nMaría García\n\nOficina técnica\nTel. 91 123 45 67";
                    s.Work = "Trabajo"; s.Address = "Dirección de la oficina"; s.Servers = "Servidores";
                    s.ServerPassword = "Contraseña del servidor de pruebas"; s.Home = "Casa"; s.Wifi = "Wi-Fi de casa";
                    s.Long = "Una frase copiada más larga que la ventana, que termina cortada en el borde derecho con puntos suspensivos";
                    s.TwoLines = "primera línea\r\nsegunda línea"; s.Remote = "srv-database-01 - Conexión a Escritorio remoto"; s.Probe = "pruebas";
                    break;
                case "fr":
                    s.Signature = "Signature e-mail"; s.SignatureText = "Cordialement,\nMarie Dupont\n\nBureau technique\nTél. 01 23 45 67 89";
                    s.Work = "Travail"; s.Address = "Adresse du bureau"; s.Servers = "Serveurs";
                    s.ServerPassword = "Mot de passe du serveur de test"; s.Home = "Maison"; s.Wifi = "Wi-Fi de la maison";
                    s.Long = "Une phrase copiée plus longue que la fenêtre, coupée au bord droit par des points de suspension";
                    s.TwoLines = "première ligne\r\ndeuxième ligne"; s.Remote = "srv-database-01 - Connexion Bureau à distance"; s.Probe = "test";
                    break;
                default:
                    s.Signature = "Email signature"; s.SignatureText = "Kind regards,\nJohn Smith\n\nTechnical office\nTel. 020 7946 0958";
                    s.Work = "Work"; s.Address = "Office address"; s.Servers = "Servers";
                    s.ServerPassword = "Test server password"; s.Home = "Home"; s.Wifi = "Home Wi-Fi";
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

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        using (System.Drawing.Drawing2D.LinearGradientBrush brush = new System.Drawing.Drawing2D.LinearGradientBrush(
            ClientRectangle, Color.FromArgb(150, 196, 200), Color.FromArgb(208, 224, 214), 35f))   // the teal of the icon, lighter
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
        MainForm.OldSettingsKey = TestKey + @"\Old";
        Clips.DefaultFile = System.IO.Path.Combine(sandbox, Program.AppName + ".db");
        Migration.OldHistory = System.IO.Path.Combine(sandbox, "History");
        Log.FileName = System.IO.Path.Combine(sandbox, Program.AppName + ".log");
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
        MigrationTest();
        BackupTest();
        return failed == 0 ? 0 : 1;
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
            clips.Pin(two, true);
            copied = clips.Copied(10, new string[0]);
            Check(copied[0].Id == two && copied[0].Pinned, "an entry kept at the top comes first");
            clips.AddText("two");
            Check(clips.CopiedCount() == 3 && clips.Copied(10, new string[0])[0].Pinned, "copied again, it keeps its star");

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
            clips.RenameFolder(servers, "Server remoti");
            clips.MoveFolder(servers, home);
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
        Check(Database.VersionNumber >= 3008000, "Windows has a SQLite recent enough");
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
                Check(row.Step() && row.Columns == 2, "a row comes back");
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
        LanguagesTest();
        return failed == 0 ? 0 : 1;
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
            List<Entry> read = Backup.Parse(Backup.Lines(sent));
            Check(read.Count == 3 && read[0].IsGroup && read[1].Text == text.Text && read[1].Group == "A" && read[2].Pinned,
                code + ": an exported file comes back");
            if (same.Count > 0) Console.WriteLine("       same as English: " + string.Join(", ", same.ToArray()));
        }
        foreach (KeyValuePair<FieldInfo, object> pair in english) pair.Key.SetValue(null, pair.Value);
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

            // Refilling the list (it happens at every clipboard change) must not move the selection.
            ListBox recent = (ListBox)typeof(MainForm).GetField("recentList", hidden).GetValue(form);
            ListBox saved = (ListBox)typeof(MainForm).GetField("savedList", hidden).GetValue(form);
            Console.WriteLine("[{0}] copied and saved entries go to their own column: {1} and {2}",
                recent.Items.Count == 5 && saved.Items.Count == 6 ? "PASS" : "FAIL", recent.Items.Count, saved.Items.Count);
            saved.SelectedIndex = 2;
            typeof(MainForm).GetMethod("Filter", hidden).Invoke(form, null);
            Console.WriteLine("[{0}] the selection survives a refill: saved {1}, copied {2}",
                saved.SelectedIndex == 2 && recent.SelectedIndex < 0 ? "PASS" : "FAIL", saved.SelectedIndex, recent.SelectedIndex);
            recent.SelectedIndex = 0;
            Console.WriteLine("[{0}] selecting in a column clears the other", saved.SelectedIndex < 0 ? "PASS" : "FAIL");

            // A closed folder hides the folders inside it too; a search shows the folders around what it finds.
            Sample s = Sample.Current;
            List<Entry> sample = (List<Entry>)typeof(MainForm).GetField("entries", hidden).GetValue(form);
            Entry work = sample.Find(delegate(Entry e) { return e.IsGroup && e.Name == s.Work; });
            work.Open = false;
            typeof(MainForm).GetMethod("Filter", hidden).Invoke(form, null);
            int closedRows = saved.Items.Count;
            work.Open = true;
            TextBox search = (TextBox)typeof(MainForm).GetField("search", hidden).GetValue(form);
            search.Text = s.Probe;
            Console.WriteLine("[{0}] a closed folder hides those inside it: {1} rows; a search shows the folders around a result: {2}",
                closedRows == 3 && saved.Items.Count == 3 && ((Entry)saved.Items[0]).Name == s.Work && ((Entry)saved.Items[1]).Label == s.Servers
                ? "PASS" : "FAIL", closedRows, saved.Items.Count);
            search.Text = "";

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
                && serverMenu.DropDownItems[0].Text == s.ServerPassword && menu.Items[3].Text == s.Home
                && menu.Items[5] is ToolStripSeparator && menu.Items[6].Text == Strings.Exit ? "PASS" : "FAIL", menu.Items.Count);
            menu.Dispose();
            typeof(MainForm).GetMethod("ShowView", hidden).Invoke(form, new object[] { true });
            Application.DoEvents();
            Save(form, path + "-options.png");
            // Every page of the options, each in a picture of its own: -options-1 and on.
            ListBox pages = (ListBox)typeof(MainForm).GetField("pageList", hidden).GetValue(form);
            for (int i = 0; i < pages.Items.Count; i++)
            {
                pages.SelectedIndex = i;
                Application.DoEvents();
                Save(form, path + "-options-" + (i + 1) + ".png");
            }
            pages.SelectedIndex = 0;
            typeof(MainForm).GetMethod("ShowView", hidden).Invoke(form, new object[] { false });

            // Windows going dark or light with the window open: every colour given to a control follows.
            Look.Switch(!Look.Dark);
            Application.DoEvents();
            Save(form, path + "-switched.png");
            Panel searchCard = (Panel)search.Parent;
            Label count = (Label)typeof(MainForm).GetField("recentCount", hidden).GetValue(form);
            Button typing = (Button)typeof(MainForm).GetField("typeButton", hidden).GetValue(form);
            Console.WriteLine("[{0}] a change of theme recolours the open window",
                form.BackColor == Look.Back && form.ForeColor == Look.Ink && recent.BackColor == Look.Well && search.BackColor == Look.Well
                && searchCard.BackColor == Look.Well && count.ForeColor == Look.Muted && typing.BackColor == Look.Accent
                && typing.FlatAppearance.BorderColor == Look.Accent ? "PASS" : "FAIL");
            Look.Switch(!Look.Dark);
            Console.WriteLine("[{0}] and back", form.BackColor == Look.Back && recent.BackColor == Look.Well ? "PASS" : "FAIL");
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

            Label status = (Label)typeof(MainForm).GetField("status", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form);
            status.Text = string.Format(Strings.DoneMany, 24) + string.Format(Strings.UnicodeMany, 2) + Strings.UnicodeHint;
            Application.DoEvents();
            Save(form, path + "-long.png");

            status.Text = string.Format(Strings.Countdown, 3, string.Format(Strings.Quote, s.Remote));
            Application.DoEvents();
            Save(form, path + "-countdown.png");

            // The status line takes room only while it says something: silent, it leaves it to the lists.
            int crowded = recent.Parent.Height;
            status.Text = "";
            Console.WriteLine("[{0}] a silent status line leaves its room to the lists: {1} then {2}",
                recent.Parent.Height > crowded ? "PASS" : "FAIL", crowded, recent.Parent.Height);

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

            Console.WriteLine("form {0}x{1}, client {2}x{3}, icon {4}", form.Width, form.Height,
                form.ClientSize.Width, form.ClientSize.Height, form.Icon == null ? "none" : form.Icon.Size.ToString());
        }

        using (ShotEdit edit = new ShotEdit(SavedSample(), Sample.Current.Work, new List<string>(new string[] { Sample.Current.Work, Sample.Current.Home })))
        {
            edit.StartPosition = FormStartPosition.Manual;
            edit.Location = new Point(-30000, -30000);
            edit.Show();
            Application.DoEvents();
            Save(edit, path + "-edit.png");
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
            // The entry that does not fit its row, shown whole as when the pointer rests on it.
            ListBox recent = (ListBox)typeof(MainForm).GetField("recentList", hidden).GetValue(form);
            typeof(MainForm).GetMethod("ShowPeek", hidden).Invoke(form, new object[] { recent, 2, false });
            Form shownPeek = (Form)typeof(MainForm).GetField("peek", hidden).GetValue(form);
            Snap(form, path + "-real-peek.png");
            Console.WriteLine("[{0}] an entry cut by its row is shown whole, one that fits is not", shownPeek.Visible
                && !(bool)typeof(MainForm).GetMethod("Cut", hidden).Invoke(form, new object[] { recent, new Entry(null, "ok"), recent.GetItemRectangle(0) })
                ? "PASS" : "FAIL");
            // A long list, with the thin bar that scrolls it, moved a little way down.
            for (int i = 0; i < 40; i++) ClipsOf(form).Import(Sample.Current.Long + " " + i, DateTime.Today.AddMinutes(i), false);
            typeof(MainForm).GetMethod("Filter", hidden).Invoke(form, null);
            recent.TopIndex = 10;
            Snap(form, path + "-real-scroll.png");
            FillSample(form, Wider);
            typeof(MainForm).GetMethod("ShowView", hidden).Invoke(form, new object[] { true });
            Snap(form, path + "-real-options.png");
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
            typeof(MainForm).GetMethod("ShowView", hidden).Invoke(form, new object[] { false });
            using (ContextMenuStrip menu = new ContextMenuStrip())
            {
                menu.Items.Add(Strings.Open);
                menu.Items.Add(Strings.Pause);
                menu.Items.Add(Strings.Exit);
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
            ListBox saved = (ListBox)typeof(MainForm).GetField("savedList", hidden).GetValue(form);
            ContextMenuStrip listMenu = (ContextMenuStrip)typeof(MainForm).GetField("listMenu", hidden).GetValue(form);
            saved.SelectedIndex = 2;
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
            ToolTip tips = (ToolTip)typeof(MainForm).GetField("tips", hidden).GetValue(form);
            Button helpButton = (Button)typeof(MainForm).GetField("helpButton", hidden).GetValue(form);
            tips.Show(tips.GetToolTip(helpButton), helpButton, -200, -40);
            SnapArea(Rectangle.Inflate(form.Bounds, 40, 40), path + "-real-tip.png");
            tips.Hide(helpButton);
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
        Look.Use(false);   // light, whatever Windows has: only the last picture is dark
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
                ListBox recent = (ListBox)typeof(MainForm).GetField("recentList", hidden).GetValue(form);
                recent.SelectedIndex = 0;
                SnapArea(area, System.IO.Path.Combine(pictures, "1-list.png"));

                // Typing a password: the countdown, with the button that cancels it.
                Button typeButton = (Button)typeof(MainForm).GetField("typeButton", hidden).GetValue(form);
                status.Text = string.Format(Strings.Countdown, 3, string.Format(Strings.Quote, Sample.Current.Remote));
                typeButton.Text = Strings.Cancel;
                SnapArea(area, System.IO.Path.Combine(pictures, "2-typing.png"));
                status.Text = "";
                typeButton.Text = Strings.TypeButton;

                // An entry the row cuts, shown whole.
                typeof(MainForm).GetMethod("ShowPeek", hidden).Invoke(form, new object[] { recent, 2, false });
                SnapArea(area, System.IO.Path.Combine(pictures, "3-preview.png"));
                typeof(MainForm).GetMethod("HidePeek", hidden).Invoke(form, null);

                // A copied text being saved under a name, into a folder.
                Sample s = Sample.Current;
                string servers = Texts.Join(s.Work, s.Servers);
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
                    menu.Items.Add(Strings.Open);
                    menu.Items.Add(Strings.Pause);
                    menu.Items.Add(Strings.Exit);
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
                typeof(MainForm).GetMethod("ShowView", hidden).Invoke(form, new object[] { true });
                SnapArea(area, System.IO.Path.Combine(pictures, "6-options.png"));
                typeof(MainForm).GetMethod("ShowView", hidden).Invoke(form, new object[] { false });
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

                // The same list in the dark, as when Windows is set so for its apps.
                Look.Switch(true);
                SnapArea(area, System.IO.Path.Combine(pictures, "8-dark.png"));
                Look.Switch(false);
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
        clips.Number(clips.AddSaved(s.Signature, s.SignatureText, 0, DateTime.Now), 1);   // the keys that choose it, shown in its row
        long work = clips.AddFolder(0, s.Work), servers = clips.AddFolder(work, s.Servers), home = clips.AddFolder(0, s.Home);
        clips.AddSaved(s.Address, "not shown", work, DateTime.Now);
        clips.Number(clips.AddSaved(s.ServerPassword, "not shown", servers, DateTime.Now), 2);
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
        typeof(MainForm).GetMethod("Reload", hidden).Invoke(form, null);
        // The window and its columns as designed (or wider by the same amount), not as the user left them: every
        // picture the same.
        typeof(MainForm).GetField("savedSize", hidden).SetValue(form, wider == 0 ? Size.Empty
            : new Size((int)Math.Round(form.Width / Look.Zoom) + wider, (int)Math.Round(form.Height / Look.Zoom)));
        typeof(MainForm).GetField("savedColumn", hidden).SetValue(form,
            (int)typeof(MainForm).GetField("SavedWidth", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null) + wider / 2);
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
