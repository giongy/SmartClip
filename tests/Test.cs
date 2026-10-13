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
        if (mode == "map") return MapTest();
        if (mode == "shot") return Shot(args[1]);
        if (mode == "type") return TypeTest();
        if (mode == "texts") return TextsTest();
        if (mode == "real") return Real(args[1]);
        Console.WriteLine("usage: map | texts | shot <png> | type");
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
        Check(Texts.Recent[0].ToString().IndexOf("40×30") > 0 && Texts.Matches(Texts.Recent[0], "immagine 40"),
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
            Texts.Save(name, "other", null);
            Check(Texts.Load().FindAll(delegate(Entry e) { return e.Name == name; }).Count == 1, "saving again replaces");
            Texts.Save(name.ToUpper(), "other", null);
            Check(Texts.Load().FindAll(delegate(Entry e) { return string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase); })
                .TrueForAll(delegate(Entry e) { return e.Name == name.ToUpper(); }), "saving with another letter case renames");
        }
        finally
        {
            Texts.Delete(name);
            Texts.RenameGroup(group, null);
            Texts.RenameGroup(group + " 2", null);
        }
        Check(!Texts.Load().Exists(delegate(Entry e) { return e.Name == name; }), "a deleted text is gone");
        using (Microsoft.Win32.RegistryKey dates = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(MainForm.SettingsKey + @"\Saved\Dates"))
            Check(dates != null && Array.IndexOf(dates.GetValueNames(), name.ToUpper()) < 0 && Array.IndexOf(dates.GetValueNames(), name) < 0,
                "and so is its date");
        List<Entry> all = Texts.Load();
        int firstCopied = all.FindIndex(delegate(Entry e) { return e.Name == null; });
        Check(firstCopied >= 0 && !all.GetRange(firstCopied, all.Count - firstCopied).Exists(delegate(Entry e) { return e.Name != null; }),
            "saved texts come before the copied ones");
        return failed == 0 ? 0 : 1;
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
                recent.Items.Count == 5 && saved.Items.Count == 5 ? "PASS" : "FAIL", recent.Items.Count, saved.Items.Count);
            saved.SelectedIndex = 2;
            typeof(MainForm).GetMethod("Filter", hidden).Invoke(form, null);
            Console.WriteLine("[{0}] the selection survives a refill: saved {1}, copied {2}",
                saved.SelectedIndex == 2 && recent.SelectedIndex < 0 ? "PASS" : "FAIL", saved.SelectedIndex, recent.SelectedIndex);
            recent.SelectedIndex = 0;
            Console.WriteLine("[{0}] selecting in a column clears the other", saved.SelectedIndex < 0 ? "PASS" : "FAIL");
            typeof(MainForm).GetMethod("ShowView", hidden).Invoke(form, new object[] { true });
            Application.DoEvents();
            Save(form, path + "-options.png");
            typeof(MainForm).GetMethod("ShowView", hidden).Invoke(form, new object[] { false });

            Label status = (Label)typeof(MainForm).GetField("status", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form);
            status.Text = "Fatto: 24 caratteri digitati. Attenzione: 2 caratteri senza tasto sulla tastiera, inviati come Unicode: se l'accesso fallisce prova «Alt+codice».";
            Application.DoEvents();
            Save(form, path + "-long.png");

            status.Text = "Digito tra 3 s in «srv-database-produzione-01 - Connessione Desktop remo…». Premi Annulla per fermare.";
            Application.DoEvents();
            Save(form, path + "-countdown.png");

            Console.WriteLine("form {0}x{1}, client {2}x{3}, icon {4}", form.Width, form.Height,
                form.ClientSize.Width, form.ClientSize.Height, form.Icon == null ? "none" : form.Icon.Size.ToString());
        }

        using (ShotEdit edit = new ShotEdit(new Entry("Firma email", "Cordiali saluti,\nMario Rossi\n\nUfficio tecnico\nTel. 02 1234567"), "Lavoro", new List<string>(new string[] { "Lavoro", "Casa" })))
        {
            edit.StartPosition = FormStartPosition.Manual;
            edit.Location = new Point(-30000, -30000);
            edit.Show();
            Application.DoEvents();
            Save(edit, path + "-edit.png");
        }
        return 0;
    }

    // Photographs the windows as the screen shows them: what Windows draws (title bar, themed scroll bars
    // and boxes) is missing from the pictures of 'shot'. The windows appear for a moment, without taking the focus.
    static int Real(string path)
    {
        using (ShotForm form = new ShotForm())
        {
            form.StartPosition = FormStartPosition.Manual;
            form.Location = new Point(80, 80);
            BindingFlags hidden = BindingFlags.NonPublic | BindingFlags.Instance;
            FillSample(form);
            form.Show();
            Snap(form, path + "-real-list.png");
            typeof(MainForm).GetMethod("ShowView", hidden).Invoke(form, new object[] { true });
            Snap(form, path + "-real-options.png");
            using (ShotEdit edit = new ShotEdit(new Entry("Firma email", "Cordiali saluti,\nMario Rossi\n\nUfficio tecnico\nTel. 02 1234567"), "Lavoro", new List<string>(new string[] { "Lavoro", "Casa" })))
            {
                edit.StartPosition = FormStartPosition.Manual;
                edit.Location = new Point(80, 80);
                edit.Show();
                Snap(edit, path + "-real-edit.png");
            }
            using (ShotHelp help = new ShotHelp())
            {
                help.Show(form);
                Snap(help, path + "-real-help.png");
            }
        }
        return 0;
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
        List<Entry> entries = new List<Entry>();
        entries.Add(new Entry("Firma email", "Cordiali saluti"));
        // Two folders, one open and one closed, each right before its texts.
        foreach (string name in new string[] { "Lavoro", "Password del server di prova", "Indirizzo ufficio", "Casa", "Wi-Fi di casa" })
        {
            bool folder = name == "Lavoro" || name == "Casa";
            Entry entry = new Entry(name, folder ? "" : "not shown", folder ? DateTime.MinValue : DateTime.Now);
            entry.IsGroup = folder;
            entry.Open = name == "Lavoro";
            if (!entry.IsGroup) entry.Group = entries[entries.Count - 1].IsGroup ? entries[entries.Count - 1].Name : entries[entries.Count - 1].Group;
            entries.Add(entry);
        }
        Entry pinned = new Entry(null, "ssh admin@srv-database-01");
        pinned.Pinned = true;
        entries.Add(pinned);
        using (Bitmap picture = SamplePicture()) entries.Add(new Entry(picture));
        entries.Add(new Entry(null, "Una frase copiata più lunga della finestra, che finisce tagliata sul bordo destro con i puntini"));
        entries.Add(new Entry(null, "prima riga\r\nseconda riga"));
        entries.Add(new Entry(null, "https://github.com/giongy/SmartClip"));
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
