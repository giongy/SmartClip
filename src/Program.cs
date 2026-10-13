using System;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

[assembly: AssemblyTitle(Program.AppName)]
[assembly: AssemblyDescription("Gestore degli appunti che sa anche digitarli dove l'incolla non funziona")]
[assembly: AssemblyProduct(Program.AppName)]
// Changing this number and pushing publishes a new release (.github/workflows/release.yml).
[assembly: AssemblyVersion("3.2.0.0")]

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

    [STAThread]
    static void Main(string[] args)
    {
        // Started by Windows at sign-in: the window stays out of the way.
        bool hidden = Array.IndexOf(args, Startup.HiddenArgument) >= 0;
        bool first;
        using (new Mutex(true, AppName + ".SingleInstance", out first))
        {
            if (!first)
            {
                // Already running: bring the existing window to the foreground, unless nobody asked to see it.
                if (hidden) return;
                Native.AllowSetForegroundWindow(Native.ASFW_ANY);
                Native.PostMessage(Native.HWND_BROADCAST, ShowMessage, IntPtr.Zero, IntPtr.Zero);
                return;
            }

            Native.SetProcessDPIAware();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Startup.Follow();
            Application.Run(new MainForm(hidden));
        }
    }
}

// Starting with Windows: a value under the Run key of the user, which is also where the setting lives.
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
    public static void Follow()
    {
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
