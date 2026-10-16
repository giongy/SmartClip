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
        if (mode == "real") return Real(args[1]);
        if (mode == "store") return Store(args[1]);
        Console.WriteLine("usage: map | texts | shot <png> [lang] | real <png> [lang] | store <folder> [lang] | type");
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

    // The logic behind the list, without opening it and without touching the clipboard.
    static int TextsTest()
    {
        Check(!Texts.IsPrivate(Copied(null, 0)), "plain text is not private");
        Check(Texts.IsPrivate(Copied("ExcludeClipboardContentFromMonitorProcessing", 1)), "a marker format makes it private");
        Check(Texts.IsPrivate(Copied("Clipboard Viewer Ignore", 1)), "the older marker format too");
        Check(Texts.IsPrivate(Copied("CanIncludeInClipboardHistory", 0)), "history allowed = 0 makes it private");
        Check(!Texts.IsPrivate(Copied("CanIncludeInClipboardHistory", 1)), "history allowed = 1 does not");
        Check(Texts.IsPrivate(Copied("CanUploadToCloudClipboard", 0)), "cloud upload = 0 makes it private");

        Texts.Recent.Clear();
        Texts.Add("one");
        Texts.Add("two");
        Texts.Add("one");
        Check(Texts.Recent.Count == 2 && Texts.Recent[0].Text == "one" && Texts.Recent[1].Text == "two", "copied again moves to the top");
        Texts.Add((string)null);
        Texts.Add("  \r\n ");
        Texts.Add(new string('x', 1000000));
        Check(Texts.Recent.Count == 2, "nothing, blanks and huge texts are not remembered");
        for (int i = 0; i < Texts.MaxRecent + 10; i++) Texts.Add("n" + i);
        Check(Texts.Recent.Count == Texts.MaxRecent && Texts.Recent[0].Text == "n" + (Texts.MaxRecent + 9)
            && !Texts.Recent.Exists(delegate(Entry e) { return e.Text == "one"; }), "the oldest ones drop out");
        Texts.Forget("n" + (Texts.MaxRecent + 9));
        Check(Texts.Recent.Count == Texts.MaxRecent - 1 && Texts.Recent[0].Text == "n" + (Texts.MaxRecent + 8),
            "a typed and cleared password is forgotten");

        Texts.Recent.Clear();
        Texts.Add("text");
        using (Bitmap picture = new Bitmap(40, 30))
        {
            picture.SetPixel(1, 1, Color.Red);
            Texts.Add(picture);
            Texts.Add(picture);
            Check(Texts.Recent.Count == 2 && Texts.Recent[0].Picture, "the same picture announced twice is kept once");
            Check(Texts.Recent[0].Preview != null && Texts.Recent[0].Preview.Width == 40 && Texts.Recent[1].Preview == null,
            "a picture has a preview, a text has none");
        Check(Texts.Recent[0].ToString().IndexOf("40×30") > 0 && Texts.Matches(Texts.Recent[0], "picture 40"),
                "a picture is described by its size and found by it");
            using (Image back = Image.FromStream(new System.IO.MemoryStream(Texts.Recent[0].LoadPng())))
            using (Bitmap pixels = new Bitmap(back))
                Check(back.Width == 40 && back.Height == 30 && pixels.GetPixel(1, 1).ToArgb() == Color.Red.ToArgb(),
                    "and comes back identical from memory");
            for (int i = 0; i < Texts.MaxPictures + 3; i++)
            {
                picture.SetPixel(2, 2, Color.FromArgb(i, i, i));
                Texts.Add(picture);
            }
            Check(Texts.Recent.FindAll(delegate(Entry e) { return e.Picture; }).Count == Texts.MaxPictures
                && Texts.Recent.Count == Texts.MaxPictures + 1, "only the last few pictures stay, texts are untouched");
        }
        HistoryTest();
        Texts.Recent.Clear();
        Texts.Add("copied");

        Entry city = new Entry("Città di Torino", "riga uno\nSeconda RIGA");
        Check(Texts.Matches(city, ""), "an empty search matches everything");
        Check(Texts.Matches(city, "citta"), "accents do not count");
        Check(Texts.Matches(city, "TORINO città"), "words in any order, any case");
        Check(Texts.Matches(city, "riga seconda torino"), "name and text are both searched");
        Check(!Texts.Matches(city, "torino milano"), "every word must be there");
        Check(Texts.Matches(new Entry(null, "perché"), "perche"), "a copied text has no name and still matches");
        Check(city.ToString().IndexOf("riga") < 0, "the list shows only the name of a saved text");
        Check(new Entry(null, " a\r\nb\tc ").ToString() == "a  b c", "a copied text is shown on one line");

        // The exported file: what goes out comes back, also a text that looks like the file itself.
        List<Entry> sent = new List<Entry>();
        Entry sentFolder = new Entry("Lavoro", "", DateTime.MinValue);
        sentFolder.IsGroup = true;
        Entry sentSaved = new Entry("Firma", "[Salvato]\r\nNome: finto\r\n\r\n\tcon tabulazione\r\nultima riga\r\n\r\n", new DateTime(2026, 3, 4, 5, 6, 7));
        sentSaved.Group = "Lavoro";
        Entry sentCopied = new Entry(null, "copiato è", new DateTime(2026, 1, 2, 3, 4, 5));
        sentCopied.Pinned = true;
        sent.AddRange(new Entry[] { new Entry("Senza data", "uno", DateTime.MinValue), sentFolder, sentSaved, sentCopied });
        using (Bitmap picture = new Bitmap(4, 4)) sent.Add(new Entry(picture));
        List<Entry> read = Backup.Parse(Backup.Lines(sent, true));
        Check(read.Count == 4 && read[0].Name == "Senza data" && read[0].Text == "uno" && read[0].Time == DateTime.MinValue
            && read[0].Group == null, "an exported text comes back, a picture is left out");
        Check(read[1].IsGroup && read[1].Name == "Lavoro", "a folder comes back");
        Check(read[2].Name == "Firma" && read[2].Text == sentSaved.Text && read[2].Group == "Lavoro" && read[2].Time == sentSaved.Time,
            "a text that looks like the file comes back identical, with folder and day");
        Check(read[3].Name == null && read[3].Text == "copiato è" && read[3].Pinned && read[3].Time == sentCopied.Time,
            "a copied text comes back with its time and its star");
        Check(Backup.Parse(Backup.Lines(sent, false)).Count == 3, "saved texts alone, when asked");
        List<string> edited = Backup.Lines(sent, true);
        edited.Insert(edited.IndexOf("\tuno") + 1, "");
        edited.Add("righe senza senso");
        Check(Backup.Parse(edited).Count == 4 && Backup.Parse(edited)[0].Text == "uno", "a file changed by hand is still read");

        // Round trip through the registry, under a name nobody would use; removed afterwards.
        string name = "~test " + Guid.NewGuid();
        string text = "pässwörd €\nseconda riga ~`";
        string group = "~test folder " + Guid.NewGuid();
        try
        {
            Texts.Save(name, text, null);
            Entry saved = Texts.Load().Find(delegate(Entry e) { return e.Name == name; });
            Check(saved != null && saved.Text == text, "a saved text comes back identical");
            Check(saved != null && saved.Time.Date == DateTime.Today, "with the day it was saved");
            using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(MainForm.SettingsKey + @"\Saved"))
            {
                byte[] raw = key.GetValue(name) as byte[];
                Check(raw != null && Encoding.UTF8.GetString(raw).IndexOf("seconda") < 0
                    && Encoding.Unicode.GetString(raw).IndexOf("seconda") < 0, "and is not readable in the registry");
            }
            // Folders: a text follows its own, through a new name too, and outlives it.
            Texts.SetGroup(group, false);
            Texts.Save(name, text, group);
            List<Entry> listed = Texts.Load();
            int at = listed.FindIndex(delegate(Entry e) { return e.IsGroup && e.Name == group; });
            Check(at >= 0 && !listed[at].Open && listed[at + 1].Name == name && listed[at + 1].Group == group,
                "a text in a folder is listed right after it");
            Check(Texts.Matches(listed[at + 1], "~test folder"), "and is found by the name of its folder");
            Texts.RenameGroup(group, group + " 2");
            Check(Texts.Load().Exists(delegate(Entry e) { return e.Name == name && e.Group == group + " 2"; })
                && !Texts.Groups().Contains(group), "a renamed folder takes its texts along");
            Texts.RenameGroup(group + " 2", null);
            Check(Texts.Load().Exists(delegate(Entry e) { return e.Name == name && e.Group == null; })
                && !Texts.Groups().Contains(group + " 2"), "a deleted folder leaves its texts among the saved ones");

            // Folders inside folders, all inside the test folder: each comes before what it holds, and takes it
            // along wherever it goes.
            string outer = Texts.Join(group, "a"), inner = Texts.Join(outer, "dentro"), deepest = Texts.Join(inner, "in fondo");
            string renamed = Texts.Join(group, "b"), moved = Texts.Join(group, "dentro");
            Texts.SetGroup(deepest, true);
            Texts.Save(name, text, inner);
            List<string> paths = Texts.Groups();
            Check(paths.IndexOf(group) >= 0 && paths.IndexOf(group) + 1 == paths.IndexOf(outer) && paths.IndexOf(outer) + 1 == paths.IndexOf(inner)
                && paths.IndexOf(inner) + 1 == paths.IndexOf(deepest), "the folders a folder is inside of are there too, each before those it holds");
            listed = Texts.Load();
            at = listed.FindIndex(delegate(Entry e) { return e.IsGroup && e.Name == inner; });
            Check(at >= 0 && listed[at].Label == "dentro" && listed[at].Level == 2 && listed[at + 1].Name == name
                && listed[at + 1].Level == 3 && listed[at + 2].Name == deepest, "a folder shows its own name, its texts come before the folders inside it");
            Texts.RenameGroup(outer, renamed);
            Check(Texts.Load().Exists(delegate(Entry e) { return e.Name == name && e.Group == Texts.Join(renamed, "dentro"); })
                && Texts.Groups().Contains(Texts.Join(Texts.Join(renamed, "dentro"), "in fondo"))
                && !Texts.Groups().Exists(delegate(string path) { return Texts.Within(path, outer); }),
                "a renamed folder takes along the folders inside it and their texts");
            Texts.RenameGroup(Texts.Join(renamed, "dentro"), moved);
            Check(Texts.Load().Exists(delegate(Entry e) { return e.Name == name && e.Group == moved; })
                && Texts.Groups().Contains(Texts.Join(moved, "in fondo")), "a folder moves out of the one it was in");
            Texts.RenameGroup(moved, Texts.Join(renamed, "dentro"));
            Texts.RenameGroup(renamed, null);
            Check(Texts.Load().Exists(delegate(Entry e) { return e.Name == name && e.Group == moved; })
                && Texts.Groups().Contains(Texts.Join(moved, "in fondo")) && !Texts.Groups().Contains(renamed),
                "a deleted folder leaves what it holds one level up");
            Check(Texts.Within(deepest, group) && Texts.Within(group, group) && !Texts.Within(group + " 2", group)
                && Texts.Parent(group) == null && Texts.Parent(inner) == outer && Texts.Display(inner) == group + " › a › dentro"
                && Texts.CleanPath(@" a \\ b\ ") == @"a\b" && Texts.CleanPath(@" \ ") == null, "paths of folders");
            Texts.Save(name, text, null);
            Texts.Save(name, "other", null);
            Check(Texts.Load().FindAll(delegate(Entry e) { return e.Name == name; }).Count == 1, "saving again replaces");
            Texts.Save(name.ToUpper(), "other", null);
            Check(Texts.Load().FindAll(delegate(Entry e) { return string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase); })
                .TrueForAll(delegate(Entry e) { return e.Name == name.ToUpper(); }), "saving with another letter case renames");
        }
        finally
        {
            Texts.Delete(name);
            // From the innermost, so that nothing moves up out of the test folders.
            foreach (string top in new string[] { group, group + " 2" })
                for (string last; (last = Texts.Groups().FindLast(delegate(string other) { return Texts.Within(other, top); })) != null; )
                    Texts.RenameGroup(last, null);
        }
        Check(!Texts.Load().Exists(delegate(Entry e) { return e.Name == name; }), "a deleted text is gone");
        using (Microsoft.Win32.RegistryKey dates = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(MainForm.SettingsKey + @"\Saved\Dates"))
            Check(dates != null && Array.IndexOf(dates.GetValueNames(), name.ToUpper()) < 0 && Array.IndexOf(dates.GetValueNames(), name) < 0,
                "and so is its date");
        List<Entry> all = Texts.Load();
        int firstCopied = all.FindIndex(delegate(Entry e) { return e.Name == null; });
        Check(firstCopied >= 0 && !all.GetRange(firstCopied, all.Count - firstCopied).Exists(delegate(Entry e) { return e.Name != null; }),
            "saved texts come before the copied ones");
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
        folder.IsGroup = true;
        Entry text = new Entry("B", "riga\r\n\tuna tabulazione", new DateTime(2026, 5, 6, 7, 8, 9));
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
            List<Entry> read = Backup.Parse(Backup.Lines(sent, true));
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

    // The history on disk, in a folder of its own instead of the user's.
    static void HistoryTest()
    {
        string real = Texts.Folder, folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "SmartClip test " + Guid.NewGuid());
        string today = System.IO.Path.Combine(folder, DateTime.Today.ToString("yyyy-MM-dd") + ".clips");
        string old = System.IO.Path.Combine(folder, DateTime.Today.AddDays(-8).ToString("yyyy-MM-dd") + ".clips");
        Texts.Folder = folder;
        try
        {
            Texts.Clear();
            Texts.Keep(7);
            for (int i = 0; i < Texts.MaxRecent + 10; i++) Texts.Add("kept " + i);
            Texts.Add("sécret wörd");
            using (Bitmap picture = new Bitmap(40, 30))
            {
                picture.SetPixel(1, 1, Color.Red);
                Texts.Add(picture);
                Texts.Add(picture);
            }
            Texts.Add("last");
            int count = Texts.MaxRecent + 13;
            Check(Texts.Recent.Count == count && System.IO.Directory.GetFiles(folder).Length == 1 && System.IO.File.Exists(today),
                "with days to keep nothing drops out, and it all goes to one file per day");
            Check(Texts.Recent[1].Png == null, "a stored picture leaves the memory");

            System.IO.File.WriteAllBytes(old, new byte[0]);
            long length = new System.IO.FileInfo(today).Length;
            using (System.IO.FileStream cut = new System.IO.FileStream(today, System.IO.FileMode.Append)) cut.Write(new byte[] { 1, 2, 3, 4, 5 }, 0, 5);
            Texts.Recent.Clear();
            Texts.Keep(7);
            Texts.Read();
            Check(Texts.Recent.Count == count && Texts.Recent[0].Text == "last" && Texts.Recent[count - 1].Text == "kept 0",
                "the history comes back from disk, newest first");
            Check(!System.IO.File.Exists(old), "an expired day is deleted");
            Check(new System.IO.FileInfo(today).Length == length, "a record cut short is removed");
            Entry stored = Texts.Recent[1];
            using (Image back = Image.FromStream(new System.IO.MemoryStream(stored.LoadPng())))
            using (Bitmap pixels = new Bitmap(back))
                Check(stored.Picture && stored.Preview != null && stored.Text.IndexOf("40×30") > 0 && back.Width == 40
                    && pixels.GetPixel(1, 1).ToArgb() == Color.Red.ToArgb(), "a picture comes back with its preview, and whole when asked");

            Texts.Forget("sécret wörd");
            Check(Encoding.UTF8.GetString(System.IO.File.ReadAllBytes(today)).IndexOf("sécret") < 0, "a forgotten text is wiped from the file");
            Texts.Add("after");
            Texts.Recent.Clear();
            Texts.Read();
            Check(Texts.Recent.Count == count && Texts.Recent[0].Text == "after"
                && !Texts.Recent.Exists(delegate(Entry e) { return e.Text == "sécret wörd"; }), "and stays out when read again");

            Texts.Keep(0);
            Check(Texts.Recent.Count == Texts.MaxRecent && System.IO.Directory.GetFiles(folder).Length == 0,
                "zero days: files deleted, memory keeps the last few");
            Entry loose = Texts.Recent.Find(delegate(Entry e) { return e.Picture; });
            Check(loose != null && loose.LoadPng().Length > 0, "and the picture is in memory again");
            Texts.Keep(7);
            Check(System.IO.File.Exists(today) && loose.Png == null, "days again: what is in memory is written");
            Texts.Clear();
            Check(Texts.Recent.Count == 0 && System.IO.Directory.GetFiles(folder).Length == 0, "clearing removes list and files");

            // Pinned entries: first in the list, untouched by a new copy, and kept beyond their days.
            DateTime past = DateTime.Now.AddDays(-20);
            string pastFile = System.IO.Path.Combine(folder, past.ToString("yyyy-MM-dd") + ".clips");
            Entry aged = new Entry(null, "aged text", past), star = new Entry(null, "star text", past);
            Texts.Recent.Add(aged);
            Texts.Recent.Add(star);
            Texts.Keep(30);
            Texts.Add("fresh");
            Texts.Pin(star, true);
            Check(Texts.Load().Find(delegate(Entry e) { return e.Name == null; }) == star, "a pinned entry comes first");
            Texts.Add("star text");
            Texts.Forget("star text");
            Check(Texts.Recent.Count == 3 && Texts.Recent.Contains(star), "copied again or forgotten, it stays as it is");
            Texts.Keep(7);
            string left = Encoding.UTF8.GetString(System.IO.File.ReadAllBytes(pastFile));
            Check(Texts.Recent.Count == 2 && Texts.Recent.Contains(star) && left.IndexOf("aged") < 0 && left.IndexOf("star") > 0,
                "it outlives its days, while the others of its file are wiped");
            Texts.Recent.Clear();
            Texts.Read();
            star =Texts.Recent.Find(delegate(Entry e) { return e.Text == "star text"; });
            Check(Texts.Recent.Count == 2 && star != null && star.Pinned, "and comes back pinned from disk");
            Texts.Pin(star, false);
            Check(Texts.Recent.Count == 1 && !System.IO.File.Exists(pastFile), "unpinned, it leaves with its day");

            // Cleaning on request: the last days are spared, and so is what is pinned.
            Entry earlier = new Entry(null, "earlier", DateTime.Now.AddDays(-2));
            Texts.Recent.Add(earlier);
            Texts.Keep(7);
            Texts.Clean(3);
            Check(Texts.Recent.Count == 2, "cleaning spares the last days");
            Texts.Clean(1);
            Check(Texts.Recent.Count == 1 && Texts.Recent[0].Text == "fresh" && System.IO.Directory.GetFiles(folder).Length == 1,
                "with one day only today stays");
            Texts.Add("kept on top");
            Texts.Pin(Texts.Recent[0], true);
            Texts.Clean(0);
            Check(Texts.Recent.Count == 1 && Texts.Recent[0].Pinned
                && Encoding.UTF8.GetString(System.IO.File.ReadAllBytes(today)).IndexOf("fresh") < 0, "with none only the pinned ones stay");
            // 13 bytes of header before the text of a record.
            Check(new System.IO.FileInfo(today).Length == 13 + "kept on top".Length && System.IO.Directory.GetFiles(folder).Length == 1,
                "and their file is shrunk to them, not filled with zeros");
            Texts.Pin(Texts.Recent[0], false);
            Texts.Recent.Clear();
            Texts.Read();
            Check(Texts.Recent.Count == 1 && Texts.Recent[0].Text == "kept on top" && !Texts.Recent[0].Pinned,
                "a shrunk file is still written and read in the right place");
            Texts.Clean(0);
            Check(System.IO.Directory.GetFiles(folder).Length == 0, "with nothing pinned the files are deleted");

            // Copied texts from an exported file: back at their time, unless the list has them.
            Texts.Add("here\nalready");
            List<Entry> restored = new List<Entry>();
            restored.Add(new Entry(null, "here\r\nalready", DateTime.Now.AddDays(-1)));
            restored.Add(new Entry(null, "from the file", DateTime.Now.AddDays(-1)));
            Check(Texts.Restore(restored) == 1 && Texts.Recent.Count == 2 && Texts.Recent[1].Text == "from the file"
                && System.IO.Directory.GetFiles(folder).Length == 2, "imported copies go to their own day, once");
        }
        finally
        {
            Texts.Clear();
            Texts.Folder = real;
            if (System.IO.Directory.Exists(folder)) System.IO.Directory.Delete(folder, true);
        }
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
            ToolStripMenuItem workMenu = menu.Items[3] as ToolStripMenuItem;
            ToolStripMenuItem serverMenu = workMenu == null ? null : workMenu.DropDownItems[1] as ToolStripMenuItem;
            Console.WriteLine("[{0}] the icon menu lists the saved texts by folder: {1} items", menu.Items.Count == 7
                && menu.Items[1] is ToolStripSeparator && menu.Items[2].Text == s.Signature && workMenu != null && workMenu.Text == s.Work
                && workMenu.DropDownItems[0].Text == s.Address && serverMenu != null && serverMenu.Text == s.Servers
                && serverMenu.DropDownItems[0].Text == s.ServerPassword && menu.Items[4].Text == s.Home
                && menu.Items[5] is ToolStripSeparator && menu.Items[6].Text == Strings.Exit ? "PASS" : "FAIL", menu.Items.Count);
            menu.Dispose();
            typeof(MainForm).GetMethod("ShowView", hidden).Invoke(form, new object[] { true });
            Application.DoEvents();
            Save(form, path + "-options.png");
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

            Console.WriteLine("form {0}x{1}, client {2}x{3}, icon {4}", form.Width, form.Height,
                form.ClientSize.Width, form.ClientSize.Height, form.Icon == null ? "none" : form.Icon.Size.ToString());
        }

        using (ShotEdit edit = new ShotEdit(new Entry(Sample.Current.Signature, Sample.Current.SignatureText), Sample.Current.Work, new List<string>(new string[] { Sample.Current.Work, Sample.Current.Home })))
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
            FillSample(form);
            form.Show();
            form.BringToFront();
            Snap(form, path + "-real-list.png");
            // The entry that does not fit its row, shown whole as when the pointer rests on it.
            ListBox recent = (ListBox)typeof(MainForm).GetField("recentList", hidden).GetValue(form);
            typeof(MainForm).GetMethod("ShowPeek", hidden).Invoke(form, new object[] { recent, 2, false });
            Form shownPeek = (Form)typeof(MainForm).GetField("peek", hidden).GetValue(form);
            Snap(form, path + "-real-peek.png");
            Console.WriteLine("[{0}] an entry cut by its row is shown whole, one that fits is not", shownPeek.Visible
                && !(bool)typeof(MainForm).GetMethod("Cut", hidden).Invoke(form, new object[] { recent, recent.Items[0], recent.GetItemRectangle(0) })
                ? "PASS" : "FAIL");
            typeof(MainForm).GetMethod("ShowView", hidden).Invoke(form, new object[] { true });
            Snap(form, path + "-real-options.png");
            using (ShotEdit edit = new ShotEdit(new Entry(Sample.Current.Signature, Sample.Current.SignatureText), Sample.Current.Work, new List<string>(new string[] { Sample.Current.Work, Sample.Current.Home })))
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
                ToolStripMenuItem work = (ToolStripMenuItem)menu.Items[3];
                work.ShowDropDown();
                work.DropDownItems[0].Select();
                SnapArea(new Rectangle(form.Left + 40, form.Top + 100, 520, 300), path + "-real-menu.png");
                menu.Close();
            }
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
            FillSample(form);
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
                    ToolStripMenuItem work = (ToolStripMenuItem)menu.Items[3];
                    work.ShowDropDown();
                    ((ToolStripMenuItem)work.DropDownItems[1]).ShowDropDown();
                    SnapArea(area, System.IO.Path.Combine(pictures, "5-tray-menu.png"));
                    menu.Close();
                }
                form.Show();
                form.BringToFront();

                typeof(MainForm).GetMethod("ShowView", hidden).Invoke(form, new object[] { true });
                SnapArea(area, System.IO.Path.Combine(pictures, "6-options.png"));
                typeof(MainForm).GetMethod("ShowView", hidden).Invoke(form, new object[] { false });

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

    // Fills the list with made-up entries instead of the user's own, which must not end up in a picture.
    static void FillSample(MainForm form)
    {
        BindingFlags hidden = BindingFlags.NonPublic | BindingFlags.Instance;
        Sample s = Sample.Current;
        List<Entry> entries = new List<Entry>();
        entries.Add(new Entry(s.Signature, s.SignatureText));
        // Folders, one open with another inside it and one closed, each right before its texts.
        string server = Texts.Join(s.Work, s.Servers);
        foreach (string name in new string[] { s.Work, s.Address, server, s.ServerPassword, s.Home, s.Wifi })
        {
            bool folder = name == s.Work || name == server || name == s.Home;
            Entry entry = new Entry(name, folder ? "" : "not shown", folder ? DateTime.MinValue : DateTime.Now);
            entry.IsGroup = folder;
            entry.Open = name != s.Home;
            if (!entry.IsGroup) entry.Group = entries[entries.Count - 1].IsGroup ? entries[entries.Count - 1].Name : entries[entries.Count - 1].Group;
            entries.Add(entry);
        }
        // Copied this morning, whatever the time the pictures are taken.
        DateTime morning = DateTime.Today.AddHours(10).AddMinutes(24);
        Entry pinned = new Entry(null, "ssh admin@srv-database-01", morning);
        pinned.Pinned = true;
        entries.Add(pinned);
        using (Bitmap picture = SamplePicture())
            entries.Add(new Entry(new Bitmap(picture, 213, 120), picture.Width, picture.Height, morning.AddMinutes(-7)));
        entries.Add(new Entry(null, s.Long, morning.AddMinutes(-19)));
        entries.Add(new Entry(null, s.TwoLines, morning.AddMinutes(-33)));
        entries.Add(new Entry(null, "https://github.com/giongy/SmartClip", morning.AddMinutes(-52)));
        typeof(MainForm).GetField("entries", hidden).SetValue(form, entries);
        typeof(MainForm).GetMethod("Filter", hidden).Invoke(form, null);
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

        form.Dispose();
        return failures == 0 && guard && stop && clean ? 0 : 1;
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
