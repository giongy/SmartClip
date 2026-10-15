using System;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

[assembly: AssemblyTitle(Program.AppName)]
[assembly: AssemblyDescription("Clipboard manager that can also type the clipboard where paste does not work")]
[assembly: AssemblyProduct(Program.AppName)]
// Changing this number and pushing publishes a new release (.github/workflows/release.yml).
[assembly: AssemblyVersion("3.9.1")]

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
            Native.SetProcessDPIAware();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Startup.Follow();
            MainForm form = new MainForm(hidden);
            if (restarting) form.ShowOptions();   // the language was changed there
            Application.Run(form);
        }
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
