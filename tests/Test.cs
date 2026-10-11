using System;
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
        Console.WriteLine("usage: map | shot <png> | type");
        return 64;
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
            form.Show();
            Application.DoEvents();
            Save(form, path + "-idle.png");

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
        return 0;
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
        form.Text = "PasswordPaste - test automatico (non toccare)";
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
