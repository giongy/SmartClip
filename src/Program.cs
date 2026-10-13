using System;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

[assembly: AssemblyTitle(Program.AppName)]
[assembly: AssemblyDescription("Gestore degli appunti che sa anche digitarli dove l'incolla non funziona")]
[assembly: AssemblyProduct(Program.AppName)]
// Changing this number and pushing publishes a new release (.github/workflows/release.yml).
[assembly: AssemblyVersion("2.1.0.0")]

// A small clipboard manager that can also type the clipboard content as real key presses (scancodes),
// so it works even where paste is disabled (lock screens of remote desktops).
static class Program
{
    public const string AppName = "SmartClip";
    public static readonly uint ShowMessage = Native.RegisterWindowMessage(AppName + ".Show");

    [STAThread]
    static void Main()
    {
        bool first;
        using (new Mutex(true, AppName + ".SingleInstance", out first))
        {
            if (!first)
            {
                // Already running: bring the existing window to the foreground.
                Native.AllowSetForegroundWindow(Native.ASFW_ANY);
                Native.PostMessage(Native.HWND_BROADCAST, ShowMessage, IntPtr.Zero, IntPtr.Zero);
                return;
            }

            Native.SetProcessDPIAware();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
