using System;
using System.IO;
using System.Text;
using System.Threading;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;

// When the program runs from its Microsoft Store package (MSIX), some things work otherwise: Windows starts it
// at sign-in through a startup task declared in the package, not through the Run key, and its own data folder
// is the one Windows gives the package. Only here are the Windows Runtime types used: outside a package they
// throw, so nothing calls them there.
static class Packaged
{
    // As declared in packaging\AppxManifest.xml.
    const string StartupTaskId = "SmartClipStartup";

    // The package family name ("Publisher.SmartClip_hash"), or null when not run from a package.
    public static readonly string Family = FamilyName();

    static string FamilyName()
    {
        try
        {
            int length = 0;
            if (Native.GetCurrentPackageFamilyName(ref length, null) != Native.ERROR_INSUFFICIENT_BUFFER) return null;
            StringBuilder name = new StringBuilder(length);
            return Native.GetCurrentPackageFamilyName(ref length, name) == Native.ERROR_SUCCESS ? name.ToString() : null;
        }
        catch (EntryPointNotFoundException)
        {
            return null;   // Windows 7: no packages at all
        }
    }

    // The data folder of the package, which Explorer sees as it is: what is written under the usual
    // %LOCALAPPDATA% would be moved by Windows into a hidden copy of its own.
    public static string DataFolder
    {
        get
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Packages", Family, "LocalState");
        }
    }

    // Whether Windows started the program at sign-in, through the startup task.
    public static bool StartedAtSignIn
    {
        get
        {
            try
            {
                IActivatedEventArgs activation = AppInstance.GetActivatedEventArgs();
                return activation != null && activation.Kind == ActivationKind.StartupTask;
            }
            catch (Exception)
            {
                return false;   // a Windows that does not tell: the window shows, which is the safe side
            }
        }
    }

    // Whether the startup task runs at sign-in. Turning it on can fail: once the user has turned it off in
    // the Settings of Windows, or the organisation has, only there can it be turned on again.
    public static bool StartsWithWindows
    {
        get
        {
            StartupTaskState state = Task().State;
            return state == StartupTaskState.Enabled || state == StartupTaskState.EnabledByPolicy;
        }
        set
        {
            StartupTask task = Task();
            if (!value) task.Disable();
            else if (task.State == StartupTaskState.Disabled) Wait(task.RequestEnableAsync());
        }
    }

    static StartupTask Task()
    {
        return Wait(StartupTask.GetAsync(StartupTaskId));
    }

    // Waits for an asynchronous call of Windows. AsTask would need the metadata of the Windows SDK, which
    // the compiler included in Windows does not have.
    static T Wait<T>(IAsyncOperation<T> operation)
    {
        using (ManualResetEvent done = new ManualResetEvent(false))
        {
            // Set on an operation already finished, the handler is called at once.
            operation.Completed = delegate(IAsyncOperation<T> finished, AsyncStatus status) { done.Set(); };
            done.WaitOne();
        }
        return operation.GetResults();
    }
}
