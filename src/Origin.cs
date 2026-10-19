using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

// The program something was copied in: its name and its icon, as the row of a copied entry shows them under its
// text. Clips keeps each program once, in its table of apps, and the entries copied in it point there.
sealed class Origin
{
    public readonly string Name;
    public readonly Image Icon;   // null if the program has none that could be read

    public Origin(string name, Image icon)
    {
        Name = name;
        Icon = icon;
    }

    // The executable of the program that put on the clipboard what is there now, or null if nobody knows: that of
    // the window owning the clipboard. A program that copies without one is the program in the foreground, where
    // the copy was made; so is one that copies through the clipboard service of Windows, which then owns it.
    // A remote program in the foreground is followed by the print of the icon of its window (Within).
    public static string Find()
    {
        IntPtr front = Native.GetForegroundWindow();
        string owner = ProgramOf(Native.GetClipboardOwner());
        string program = owner != null && !IsHost(owner) ? owner : ProgramOf(front);
        return IsRemote(program) && string.Equals(ProgramOf(front), program, StringComparison.OrdinalIgnoreCase)
            ? Within(program, front) : program;
    }

    // The host of the services of Windows (svchost.exe), where the clipboard service runs: never the program the user
    // copied in (seen 18/10/2026 as «Host Process for Windows Services»).
    const string ServiceHost = "svchost.exe";

    public static bool IsHost(string path)
    {
        return string.Equals(Path.GetFileName(Executable(path)), ServiceHost, StringComparison.OrdinalIgnoreCase);
    }

    // The programs that show on this PC the windows of programs running on a server: Citrix, its HDX Engine and its
    // viewer of a whole remote desktop. For Windows, what is copied in any of those windows is theirs; but each window
    // has the icon of the program it shows. So a remote program is still known by the name of the one on this PC, but
    // there is one for each icon met (wanted by the user on 19/10/2026: "Citrix HDX Engine" with the icon of the real
    // program, so as to know both that it comes from there and from which program).
    static readonly string[] Remote = { "wfica32.exe", "wfica.exe", "CDViewer.exe" };
    // Between the executable and the print of the icon of the window, in what Clips knows a remote program by. Windows
    // never has it in a path.
    const char IconMark = '*';
    const int PrintBytes = 8;     // of the hash of the icon: enough to tell the programs of one server apart
    const uint AskMs = 100;       // how long a window may take to say its icon, before it is taken for hung
    // The icons of the windows of remote programs met in this session, by what Clips knows the program by: Clips
    // describes it once per session (IconOf), always right after Find met it.
    static readonly Dictionary<string, byte[]> windowIcons = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);

    static bool IsRemote(string path)
    {
        if (path == null) return false;
        string file = Path.GetFileName(path);
        foreach (string remote in Remote)
            if (string.Equals(file, remote, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    // What Clips knows the program of 'window' by, whose executable is 'program': the executable followed by the print
    // of the icon of the window. Without an icon, the executable alone.
    internal static string Within(string program, IntPtr window)
    {
        byte[] icon = WindowIcon(window);
        if (icon == null) return program;
        byte[] hash;
        using (SHA256 sha = SHA256.Create()) hash = sha.ComputeHash(icon);
        string key = program + IconMark + BitConverter.ToString(hash, 0, PrintBytes).Replace("-", "");
        windowIcons[key] = icon;
        return key;
    }

    // The executable in what Clips knows a program by.
    static string Executable(string program)
    {
        int mark = program.IndexOf(IconMark);
        return mark < 0 ? program : program.Substring(0, mark);
    }

    // The icon a window shows in its title bar and on the taskbar, as PNG, or null: the large one if it has one. Never
    // that of its class, which would be the icon of the program on this PC.
    static byte[] WindowIcon(IntPtr window)
    {
        foreach (int size in new int[] { Native.ICON_BIG, Native.ICON_SMALL2, Native.ICON_SMALL })
        {
            IntPtr handle;
            if (Native.SendMessageTimeout(window, Native.WM_GETICON, (IntPtr)size, IntPtr.Zero, Native.SMTO_ABORTIFHUNG, AskMs,
                out handle) == IntPtr.Zero || handle == IntPtr.Zero) continue;
            try
            {
                using (System.Drawing.Icon icon = System.Drawing.Icon.FromHandle(handle))
                using (Bitmap picture = icon.ToBitmap())
                    return Entry.ToPng(picture);
            }
            catch (ArgumentException)
            {
                // The window has changed its icon in the meantime: the next size.
            }
            catch (ExternalException)
            {
            }
        }
        return null;
    }

    // The executable of the program a window belongs to, or null. Also of a program run as administrator: the little
    // asked of it Windows grants to anyone.
    public static string ProgramOf(IntPtr window)
    {
        if (window == IntPtr.Zero) return null;
        uint id;
        Native.GetWindowThreadProcessId(window, out id);
        if (id == 0) return null;
        IntPtr process = Native.OpenProcess(Native.PROCESS_QUERY_LIMITED_INFORMATION, false, id);
        if (process == IntPtr.Zero) return null;
        try
        {
            StringBuilder path = new StringBuilder(1024);
            int length = path.Capacity;
            return Native.QueryFullProcessImageName(process, 0, path, ref length) ? path.ToString(0, length) : null;
        }
        finally
        {
            Native.CloseHandle(process);
        }
    }

    // What the user knows a program by: the description in its file ("Google Chrome"), or else its product, or
    // else the name of the file. A remote program goes by the name of the program on this PC.
    public static string NameOf(string program)
    {
        string path = Executable(program);
        try
        {
            FileVersionInfo info = FileVersionInfo.GetVersionInfo(path);
            foreach (string name in new string[] { info.FileDescription, info.ProductName })
                if (name != null && name.Trim().Length > 0) return name.Trim();
        }
        catch (IOException)
        {
            // The file is gone, or cannot be read: its name will do.
        }
        catch (UnauthorizedAccessException)
        {
        }
        return Path.GetFileNameWithoutExtension(path);
    }

    // The icon of a program as PNG, as large as Windows gives it (32 pixels, more on a larger scale), or null. That of
    // a remote program is the icon of its window.
    public static byte[] IconOf(string program)
    {
        byte[] shown;
        if (windowIcons.TryGetValue(program, out shown)) return shown;
        try
        {
            using (System.Drawing.Icon icon = System.Drawing.Icon.ExtractAssociatedIcon(Executable(program)))
            {
                if (icon == null) return null;
                using (Bitmap picture = icon.ToBitmap()) return Entry.ToPng(picture);
            }
        }
        catch (ArgumentException)
        {
            return null;   // not a path Windows takes an icon from
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }
}
