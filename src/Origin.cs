using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
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
    public static string Find()
    {
        string owner = ProgramOf(Native.GetClipboardOwner());
        return owner != null && !IsHost(owner) ? owner : ProgramOf(Native.GetForegroundWindow());
    }

    // The host of the services of Windows (svchost.exe), where the clipboard service runs: never the program the user
    // copied in (seen 18/10/2026 as «Host Process for Windows Services»).
    const string ServiceHost = "svchost.exe";

    public static bool IsHost(string path)
    {
        return string.Equals(Path.GetFileName(path), ServiceHost, StringComparison.OrdinalIgnoreCase);
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
    // else the name of the file.
    public static string NameOf(string path)
    {
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

    // The icon of a program as PNG, as large as Windows gives it (32 pixels, more on a larger scale), or null.
    public static byte[] IconOf(string path)
    {
        try
        {
            using (System.Drawing.Icon icon = System.Drawing.Icon.ExtractAssociatedIcon(path))
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
