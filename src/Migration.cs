using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Win32;

// What the migration brought into the database.
class MigrationResult
{
    public int Saved, Folders, Copied, Pictures;
    public int Unreadable;   // history files that made no sense, left out (and written to the log)
}

// Brings into a new database what the versions before it kept elsewhere, once: the saved texts from the registry,
// encrypted as they were, with their days, numbers and folders; the copied texts and pictures from the history
// files, one per day, with their stars and numbers. It only reads: the registry and the files stay as they are,
// for a version before this one to find them still. It runs inside the transaction that creates the database
// (Clips), so it is all or nothing, and tried again at the next start if it fails.
static class Migration
{
    // Under the settings key: the saved texts by name, and beside them, by the same name, the day each was saved,
    // its number, the folder it is in; the folders by path, each 1 if open.
    const string SavedKey = "Saved", DatesKey = "Dates", NumbersKey = "Numbers", PlacesKey = "Places", FoldersKey = "Folders";
    const char Separator = '\\';   // between the names of a folder path
    // A record of a history file: kind, time of the copy (DateTime ticks, local) and length, then that many bytes. Of
    // a text, the text in UTF-8; of a picture its width, height and the length of the preview, then preview and
    // picture as PNG. The kind holds the star in its high bit and the number in the bits under it.
    const byte TextKind = 1, PictureKind = 2, KindMask = 0x07, PinnedKind = 0x80, NumberMask = 0x78;
    const int NumberShift = 3, HeaderSize = 1 + 8 + 4, PictureHeader = 3 * 4;
    const string Extension = ".clips";
    // Where the history files were, one per day; the checks use a folder of their own.
    public static string OldHistory = Path.Combine(Program.DataFolder, "History");

    public static MigrationResult Run(Clips clips, string settingsKey, string historyFolder)
    {
        MigrationResult result = new MigrationResult();
        using (RegistryKey saved = Registry.CurrentUser.OpenSubKey(settingsKey + @"\" + SavedKey))
            if (saved != null) AdoptSaved(clips, saved, result);
        if (Directory.Exists(historyFolder))
            foreach (string file in Directory.GetFiles(historyFolder, "*" + Extension))
            {
                try
                {
                    AdoptFile(clips, File.ReadAllBytes(file), result);
                }
                catch (Exception ex)
                {
                    // A file that cannot be read or makes no sense: the others come along all the same.
                    result.Unreadable++;
                    Log.Error(new InvalidDataException("Not migrated: " + file, ex));
                }
            }
        return result;
    }

    static void AdoptSaved(Clips clips, RegistryKey saved, MigrationResult result)
    {
        using (RegistryKey dates = saved.OpenSubKey(DatesKey))
        using (RegistryKey numbers = saved.OpenSubKey(NumbersKey))
        using (RegistryKey places = saved.OpenSubKey(PlacesKey))
        using (RegistryKey folders = saved.OpenSubKey(FoldersKey))
        {
            Dictionary<string, long> made = AdoptFolders(clips, folders, result);
            foreach (string name in saved.GetValueNames())
            {
                byte[] secret = saved.GetValue(name) as byte[];
                if (secret == null) continue;
                object ticks = dates == null ? null : dates.GetValue(name);
                DateTime time = ticks is long && (long)ticks > DateTime.MinValue.Ticks && (long)ticks <= DateTime.MaxValue.Ticks
                    ? new DateTime((long)ticks) : DateTime.MinValue;   // saved before the days were kept
                object number = numbers == null ? null : numbers.GetValue(name);
                // A text whose folder is gone is simply in none, as the list had it.
                string place = Clean(places == null ? null : places.GetValue(name) as string);
                long folder;
                if (place == null || !made.TryGetValue(place, out folder)) folder = 0;
                clips.AdoptSaved(name, secret, folder, time, number is int ? (int)number : 0);
                result.Saved++;
            }
        }
    }

    // The folders by path, each made after the one it is inside of, which is made too if only the paths inside
    // it name it. Returns them by path.
    static Dictionary<string, long> AdoptFolders(Clips clips, RegistryKey folders, MigrationResult result)
    {
        Dictionary<string, long> made = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        if (folders == null) return made;
        Dictionary<string, bool> open = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        foreach (string value in folders.GetValueNames())
        {
            string path = Clean(value);
            if (path != null) open[path] = !Equals(folders.GetValue(value), 0);
        }
        List<string> paths = new List<string>();
        foreach (string path in open.Keys)
            for (string each = path; each != null; each = Parent(each))
                if (!paths.Exists(delegate(string other) { return string.Equals(other, each, StringComparison.OrdinalIgnoreCase); }))
                    paths.Add(each);
        paths.Sort(delegate(string a, string b) { return a.Split(Separator).Length.CompareTo(b.Split(Separator).Length); });   // the outer ones first
        foreach (string path in paths)
        {
            string parent = Parent(path);
            long id = clips.AddFolder(parent == null ? 0 : made[parent], path.Substring(path.LastIndexOf(Separator) + 1));
            bool opened;
            if (open.TryGetValue(path, out opened) && !opened) clips.OpenFolder(id, false);
            made[path] = id;
            result.Folders++;
        }
        return made;
    }

    // A path without empty names or spaces around them, or null if nothing is left.
    static string Clean(string path)
    {
        if (path == null) return null;
        List<string> names = new List<string>();
        foreach (string name in path.Split(Separator))
            if (name.Trim().Length > 0) names.Add(name.Trim());
        return names.Count == 0 ? null : string.Join(Separator.ToString(), names);
    }

    static string Parent(string path)
    {
        int at = path.LastIndexOf(Separator);
        return at < 0 ? null : path.Substring(0, at);
    }

    // The records of a history file, up to one cut short by a crash: the deleted ones are left out.
    static void AdoptFile(Clips clips, byte[] file, MigrationResult result)
    {
        using (BinaryReader reader = new BinaryReader(new MemoryStream(file)))
        {
            long end = 0;
            while (end + HeaderSize <= file.Length)
            {
                reader.BaseStream.Position = end;
                byte kind = reader.ReadByte();
                long ticks = reader.ReadInt64();
                int size = reader.ReadInt32();
                if (size < 0 || end + HeaderSize + size > file.Length) break;
                end += HeaderSize + size;
                if (ticks < DateTime.MinValue.Ticks || ticks > DateTime.MaxValue.Ticks) continue;
                DateTime time = new DateTime(ticks);
                bool pinned = (kind & PinnedKind) != 0;
                int number = pinned ? (kind & NumberMask) >> NumberShift : 0;
                if ((kind & KindMask) == TextKind)
                {
                    clips.AdoptText(Encoding.UTF8.GetString(reader.ReadBytes(size)), time, pinned, number);
                    result.Copied++;
                }
                else if ((kind & KindMask) == PictureKind && size >= PictureHeader)
                {
                    int width = reader.ReadInt32(), height = reader.ReadInt32(), previewLength = reader.ReadInt32();
                    if (previewLength <= 0 || previewLength > size - PictureHeader) continue;
                    byte[] preview = reader.ReadBytes(previewLength);
                    byte[] png = reader.ReadBytes(size - PictureHeader - previewLength);
                    clips.AdoptPicture(png, preview, width, height, time, pinned, number);
                    result.Pictures++;
                }
            }
        }
    }
}
