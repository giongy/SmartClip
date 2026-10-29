using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

[assembly: AssemblyTitle(Program.AppName)]
[assembly: AssemblyDescription("Clipboard manager that can also type the clipboard where paste does not work")]
[assembly: AssemblyProduct(Program.AppName)]
// Changing this number and pushing publishes a new release (.github/workflows/release.yml).
[assembly: AssemblyVersion("4.3.20")]

// A small clipboard manager that can also type the clipboard content as real key presses (scancodes),
// so it works even where paste is disabled (lock screens of remote desktops).
static class Program
{
    public const string AppName = "SmartClip";
    // The version as the window shows it and as the release is tagged: three numbers, the fourth only when
    // it is not zero.
    public static readonly string Version = ShortVersion();

    static string ShortVersion()
    {
        Version version = typeof(Program).Assembly.GetName().Version;
        return version.Revision == 0 ? version.ToString(3) : version.ToString();
    }

    // Where the program keeps what it writes to disk: the history, the log. From the Store package, inside the data
    // folder Windows gives it, which Explorer can open as it is.
    public static readonly string DataFolder = Packaged.Family != null ? Packaged.DataFolder
        : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppName);

    public static readonly uint ShowMessage = Native.RegisterWindowMessage(AppName + ".Show");
    const string SingleInstance = AppName + ".SingleInstance";
    // The program starting itself again, after a change of language: it waits for the old one to close.
    public const string RestartArgument = "/restart";
    const int RestartPollMs = 100, RestartTries = 100;   // ten seconds at most

    [STAThread]
    static void Main(string[] args)
    {
        // Started by Windows at sign-in: the window stays out of the way. From the package, the startup task
        // says so instead of the argument.
        bool hidden = Array.IndexOf(args, Startup.HiddenArgument) >= 0 || Packaged.Family != null && Packaged.StartedAtSignIn;
        bool restarting = Array.IndexOf(args, RestartArgument) >= 0;
        bool first;
        Mutex single = new Mutex(true, SingleInstance, out first);
        for (int i = 0; !first && restarting && i < RestartTries; i++)
        {
            single.Dispose();
            Thread.Sleep(RestartPollMs);
            single = new Mutex(true, SingleInstance, out first);
        }
        using (single)
        {
            if (!first)
            {
                // Already running: bring the existing window to the foreground, unless nobody asked to see it.
                if (hidden) return;
                Native.AllowSetForegroundWindow(Native.ASFW_ANY);
                Native.PostMessage(Native.HWND_BROADCAST, ShowMessage, IntPtr.Zero, IntPtr.Zero);
                return;
            }

            Strings.Use(MainForm.SavedLanguage());   // before any window: every text comes from here
            // What nobody caught: written to the log and said in the program's own words, instead of the window of
            // Windows Forms with its stack trace in English. The program goes on.
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += delegate(object sender, ThreadExceptionEventArgs e) { Report(e.Exception); };
            AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs e)
            {
                Log.Error(e.ExceptionObject as Exception);   // on another thread: the program is ending
            };
            Native.SetProcessDPIAware();
            Look.Choose(MainForm.SavedTheme());   // the windows are built in its colours
            MainForm.UseSavedInterface();         // and for the font of the interface
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            string missing = SqliteProblem();
            if (missing != null)
            {
                AskForm.Tell(null, missing);
                return;
            }
            Startup.Follow();
            bool firstStart = MainForm.FirstStart();   // before the window, which writes the settings when it closes
            MainForm form = new MainForm(hidden);
            form.AskSetup(firstStart);
            if (restarting) form.ShowOptions();   // the language was changed there
            Application.Run(form);
        }
    }

    // SQLite comes with Windows 10 and 11, and the list cannot work without it, not even in memory: if it is missing
    // or too old, what the program says before giving up, written to the log too. Null when it is there.
    static string SqliteProblem()
    {
        try
        {
            if (Database.VersionNumber >= Database.MinimumVersion) return null;
            Log.Error(new NotSupportedException("SQLite " + Database.Version + " is older than " + Database.MinimumName));
            return string.Format(Strings.SqliteTooOld, AppName, Database.MinimumName, Database.Version);
        }
        catch (Exception ex)
        {
            // The library is not there, lacks the function, or is not for this kind of process.
            if (!(ex is DllNotFoundException || ex is EntryPointNotFoundException || ex is BadImageFormatException)) throw;
            Log.Error(ex);
            return string.Format(Strings.SqliteMissing, AppName);
        }
    }

    static bool reported;   // the first error is said, the next ones only written: one that repeats must not flood the screen

    // The window is shown a moment later, from the message loop: while this handler runs, Windows Forms drops any
    // other error without a word, so it must not stay open for as long as the window does.
    static void Report(Exception ex)
    {
        Log.Error(ex);
        if (reported) return;
        reported = true;
        string text = string.Format(Strings.UnexpectedError, ex.Message, Log.FileName);
        SendOrPostCallback tell = delegate
        {
            try
            {
                AskForm.Tell(null, text);
            }
            catch (Exception)
            {
                // Not even the window can be shown: what went wrong is in the log all the same.
            }
        };
        if (SynchronizationContext.Current != null) SynchronizationContext.Current.Post(tell, null);
        else tell(null);
    }
}

// Starting with Windows: a value under the Run key of the user, which is also where the setting lives. From
// the Store package, the startup task that the package declares instead (see Packaged).
static class Startup
{
    public const string HiddenArgument = "/tray";
    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    // Where Task Manager and Settings switch a startup program off, leaving its Run value in place.
    const string ApprovedKey = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";

    static string Command
    {
        get { return "\"" + Application.ExecutablePath + "\" " + HiddenArgument; }
    }

    public static bool Enabled
    {
        get
        {
            if (Packaged.Family != null) return Packaged.StartsWithWindows;
            using (RegistryKey run = Registry.CurrentUser.OpenSubKey(RunKey))
            using (RegistryKey approved = Registry.CurrentUser.OpenSubKey(ApprovedKey))
            {
                if (run == null || run.GetValue(Program.AppName) == null) return false;
                // Switched off by Windows when the first byte is odd.
                byte[] state = approved == null ? null : approved.GetValue(Program.AppName) as byte[];
                return state == null || state.Length == 0 || state[0] % 2 == 0;
            }
        }
        set
        {
            if (Packaged.Family != null)
            {
                Packaged.StartsWithWindows = value;
                return;
            }
            using (RegistryKey run = Registry.CurrentUser.CreateSubKey(RunKey))
            using (RegistryKey approved = Registry.CurrentUser.CreateSubKey(ApprovedKey))
            {
                if (value) run.SetValue(Program.AppName, Command);
                else run.DeleteValue(Program.AppName, false);
                approved.DeleteValue(Program.AppName, false);   // whatever Windows had decided about the old value
            }
        }
    }

    // The program may have been moved since it was told to start with Windows: the command follows it.
    // A package never moves, and does not use the Run key.
    public static void Follow()
    {
        if (Packaged.Family != null) return;
        try
        {
            using (RegistryKey run = Registry.CurrentUser.OpenSubKey(RunKey, true))
            {
                if (run != null && run.GetValue(Program.AppName) != null && !Command.Equals(run.GetValue(Program.AppName)))
                    run.SetValue(Program.AppName, Command);
            }
        }
        catch (Exception)
        {
            // Registry not writable: the old command stays.
        }
    }
}
