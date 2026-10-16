using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

// What the list offers: a text saved by the user under a name, or something just copied (Name is null),
// which can also be a picture.
class Entry
{
    const int MaxPreview = 120;   // beyond this, the text of a copied entry is truncated in the list
    public readonly string Name, Text;
    public readonly DateTime Time;   // when it was copied, or saved: DateTime.MinValue if nobody knows
    const int PreviewHeight = 120;   // of the small copy shown in the list: enough for a sharp row at any scale
    public readonly bool Picture;
    public bool Pinned;              // a copied entry the user keeps at the top of its column
    public int Number;               // after the list hotkey, the digit that chooses it: 0 for none
    public string Group;             // the folder a saved text is in, or null
    public bool IsGroup, Open;       // not a text but a folder of saved texts, named by Name; and whether it shows them
    public readonly int Width, Height;   // of a picture
    public readonly Image Preview;       // null for a text
    public byte[] Png;                   // a picture while it is only in memory: null once the history file holds it
    // Where the history file holds a copied entry: FilePath is null while it is only in memory. Offset and
    // Length are those of its record, PngOffset and PngLength those of the picture inside it.
    public string FilePath;
    public long Offset, PngOffset;
    public int Length, PngLength;
    string line;   // what the list shows of a copied text, worked out once: rows are drawn often, and texts can be long
    string folded; // the text as the search compares it, worked out at the first search

    public Entry(string name, string text) : this(name, text, DateTime.Now)
    {
    }

    public Entry(string name, string text, DateTime time)
    {
        Name = name;
        Text = text;
        Time = time;
    }

    // A copied picture, kept as PNG because it takes far less memory than the bitmap. Its text describes it.
    public Entry(Image picture) : this(new Bitmap(picture, PreviewSize(picture)), picture.Width, picture.Height, DateTime.Now)
    {
        Png = ToPng(picture);
        PngLength = Png.Length;
    }

    // A picture as the history file gives it back: the preview only, the rest is read when it is chosen.
    public Entry(Image preview, int width, int height, DateTime time)
    {
        Picture = true;
        Preview = preview;
        Width = width;
        Height = height;
        Time = time;
        Text = time.Date == DateTime.Today ? string.Format(Strings.PictureToday, width, height, time.ToString("HH:mm:ss"))
            : string.Format(Strings.PictureOn, width, height, time.ToString(Strings.DayFormat, Strings.Culture), time.ToString("HH:mm:ss"));
    }

    static Size PreviewSize(Image picture)
    {
        int height = Math.Min(PreviewHeight, picture.Height);
        return new Size(Math.Max(1, picture.Width * height / picture.Height), height);
    }

    public static byte[] ToPng(Image picture)
    {
        using (MemoryStream png = new MemoryStream())
        {
            picture.Save(png, ImageFormat.Png);
            return png.ToArray();
        }
    }

    // The whole picture: from memory, or from the history file.
    public byte[] LoadPng()
    {
        if (Png != null) return Png;
        using (FileStream stream = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (BinaryReader reader = new BinaryReader(stream))
        {
            stream.Position = PngOffset;
            return reader.ReadBytes(PngLength);
        }
    }

    // What the list shows. Of a saved text only the name: a saved password never appears on screen.
    public string Label
    {
        get
        {
            if (Name != null) return IsGroup ? Texts.Leaf(Name) : Name;   // a folder by its own name, not its path
            if (Picture) return string.Format(Strings.PictureLabel, Width, Height);   // when it was copied is shown beside it
            if (line != null) return line;
            string joined = Text.Trim().Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ');
            line = joined.Length > MaxPreview ? joined.Substring(0, MaxPreview) + "…" : joined;
            return line;
        }
    }

    public string Folded
    {
        get { return folded ?? (folded = Texts.Fold(Text)); }
    }

    // How many folders it is inside of: zero for a folder or a text outside them all.
    public int Level
    {
        get { return IsGroup ? Texts.Depth(Name) - 1 : Texts.Depth(Group); }
    }

    // The sign that tells a saved text and a picture from a copied text.
    public string Mark
    {
        get { return IsGroup ? "" : Name != null ? "★" : Picture ? "▣" : ""; }
    }

    public override string ToString()
    {
        return (Mark + " " + Label).TrimStart();
    }
}

// What is behind the list: the texts and pictures copied, and the texts saved by the user, kept in the registry
// and encrypted for the current Windows user. What was copied stays for a number of days in the history
// folder, as it is, one file per day; with zero days it stays only in memory, and only the last few.
static class Texts
{
    public const int MaxRecent = 30, MaxPictures = 5;   // while only in memory, where pictures are heavy
    public const int MaxKeepDays = 365, DefaultKeepDays = 30;
    const int MaxLength = 100000;   // a longer text is more likely a whole document than something to use again
    // A record of a history file: kind, time of the copy and length, then that many bytes. Of a text, the
    // text itself; of a picture its width, height and the length of the preview, then preview and picture as PNG.
    const byte DeletedKind = 0, TextKind = 1, PictureKind = 2;
    const byte PinnedKind = 0x80;   // added to the kind of an entry kept at the top
    // Of a pinned entry, its number in the bits between: those below are the kind.
    const byte KindMask = 0x07, NumberMask = 0x78;
    const int NumberShift = 3;
    const int HeaderSize = sizeof(byte) + sizeof(long) + sizeof(int), PictureHeader = 3 * sizeof(int);
    public const string DayFormat = "yyyy-MM-dd";
    const string Extension = ".clips";
    // From the Store package, inside the data folder Windows gives it, which Explorer can open as it is.
    public static string Folder = Path.Combine(Packaged.Family != null ? Packaged.DataFolder
        : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Program.AppName), "History");
    static int keepDays;       // zero: nothing goes to disk
    static DateTime pruned;    // the day the expired files were last removed
    static bool loaded;        // the history files were read: before that, nobody knows which hold a pinned entry
    const string SavedKey = MainForm.SettingsKey + @"\Saved";
    const string DatesKey = "Dates";   // under the saved texts: when each was saved, under the same name
    const string NumbersKey = "Numbers";   // the same: the number of each that has one
    // The folders of the saved texts ("groups" here: Folder is where the history is). Under the saved texts,
    // the groups by name, each with 1 if it is open, and the group of each text that has one, under its name.
    const string GroupsKey = "Folders", PlacesKey = "Places";
    // Groups hold groups too: a group is known by its path, the names from the outermost down joined by this
    // sign, which a name cannot contain. Those made before there were groups inside groups are outermost.
    public const char Separator = '\\';
    // Clipboard formats with which password managers and private browser windows mark what must not be
    // recorded: the first two by merely being there, the others by holding a zero.
    public const string PrivateMarker = "ExcludeClipboardContentFromMonitorProcessing";
    static readonly string[] PrivateIfPresent = { "Clipboard Viewer Ignore", PrivateMarker };
    static readonly string[] PrivateIfZero = { "CanIncludeInClipboardHistory", "CanUploadToCloudClipboard" };

    public static readonly List<Entry> Recent = new List<Entry>();   // newest first

    // The path of one of the keys under the saved texts: dates, numbers, groups, places.
    static string Below(string name)
    {
        return SavedKey + @"\" + name;
    }

    // Called at every clipboard change.
    public static void Remember()
    {
        try
        {
            IDataObject data = Clipboard.GetDataObject();
            if (data == null || IsPrivate(data)) return;
            // Programs that copy both (a spreadsheet, a word processor) mean the text.
            string text = data.GetData(DataFormats.UnicodeText) as string;
            if (text != null) Add(text);
            else
            {
                using (Image picture = data.GetData(DataFormats.Bitmap) as Image)
                {
                    if (picture != null) Add(picture);
                }
            }
        }
        catch (Exception)
        {
            // Clipboard held by another program, or a picture that cannot be read or is too large: this copy is
            // not remembered. Whatever the failure, it must not surface at every copy made in another program.
        }
    }

    public static void Add(string text)
    {
        if (text == null || text.Trim().Length == 0 || text.Length > MaxLength) return;
        // Copied again: it moves to the top instead of appearing twice. A pinned one is at the top already.
        if (Recent.Exists(delegate(Entry entry) { return entry.Pinned && entry.Text == text; })) return;
        Forget(text);
        Insert(new Entry(null, text));
    }

    public static void Add(Image picture)
    {
        Entry entry = new Entry(picture);
        // The same picture again, also because Windows announces one copy several times. Only those of the
        // same length are read back from the history file to be compared. A pinned one stays as it is.
        bool pinned = false;
        Recent.RemoveAll(delegate(Entry other)
        {
            if (!other.Picture || other.PngLength != entry.PngLength || !other.LoadPng().SequenceEqual(entry.Png)) return false;
            if (other.Pinned)
            {
                pinned = true;
                return false;
            }
            Erase(other);
            return true;
        });
        if (!pinned) Insert(entry);
    }

    static void Insert(Entry entry)
    {
        Recent.Insert(0, entry);
        if (keepDays == 0)
        {
            Limit();
            return;
        }
        if (pruned != DateTime.Today) Prune();
        Store(entry);
    }

    // What memory alone is allowed to hold: the newest pictures, then the newest entries. The pinned ones
    // are not counted.
    static void Limit()
    {
        int pictures = 0, kept = 0;
        Recent.RemoveAll(delegate(Entry entry) { return !entry.Pinned && entry.Picture && ++pictures > MaxPictures; });
        Recent.RemoveAll(delegate(Entry entry) { return !entry.Pinned && ++kept > MaxRecent; });
    }

    // Forgets a copied text, unless the user pinned it.
    public static void Forget(string text)
    {
        Recent.RemoveAll(delegate(Entry entry)
        {
            if (entry.Text != text || entry.Pinned) return false;
            Erase(entry);
            return true;
        });
    }

    public static void Remove(Entry entry)
    {
        Recent.Remove(entry);
        Erase(entry);
    }

    // Keeps a copied entry at the top of its column, or lets it go back among the others: then it leaves at
    // once, if its days are over. Its number goes with the star: it could not stay without it.
    public static void Pin(Entry entry, bool pinned)
    {
        entry.Pinned = pinned;
        if (!pinned) entry.Number = 0;
        Mark(entry);
        if (!pinned && keepDays > 0) Prune();
    }

    // Writes again the kind of a copied entry in its history file, where star and number are.
    static void Mark(Entry entry)
    {
        if (entry.FilePath == null) return;
        OnDisk(delegate
        {
            using (FileStream stream = new FileStream(entry.FilePath, FileMode.Open, FileAccess.Write))
            {
                stream.Position = entry.Offset;
                stream.WriteByte(KindOf(entry));
            }
        });
    }

    // Writes to the history files. If the disk refuses, what is in memory stays as it is: the list works on.
    static void OnDisk(Action write)
    {
        try
        {
            write();
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    static byte KindOf(Entry entry)
    {
        return (byte)((entry.Picture ? PictureKind : TextKind) | (entry.Pinned ? PinnedKind | entry.Number << NumberShift : 0));
    }

    // Gives an entry the number that chooses it after the list hotkey, taking it from whatever had it; zero
    // takes its own away. A copied entry gets the star along: without it, it would go with its day.
    public static void Number(Entry entry, int number)
    {
        if (entry.Name != null)
        {
            Number(entry.Name, number);
            return;
        }
        Free(number);
        entry.Number = number;
        entry.Pinned |= number > 0;
        Mark(entry);
    }

    // The same for a saved text, by its name.
    public static void Number(string name, int number)
    {
        Free(number);
        using (RegistryKey numbers = Registry.CurrentUser.CreateSubKey(Below(NumbersKey)))
        {
            numbers.DeleteValue(name, false);
            if (number > 0) numbers.SetValue(name, number, RegistryValueKind.DWord);
        }
    }

    // Takes a number away from whatever has it: it chooses one entry only.
    static void Free(int number)
    {
        if (number == 0) return;
        foreach (Entry entry in Recent)
        {
            if (entry.Number != number) continue;
            entry.Number = 0;
            Mark(entry);
        }
        using (RegistryKey numbers = Registry.CurrentUser.OpenSubKey(Below(NumbersKey), true))
        {
            if (numbers == null) return;
            foreach (string name in numbers.GetValueNames())
                if (Equals(numbers.GetValue(name), number)) numbers.DeleteValue(name, false);
        }
    }

    // Forgets everything that was copied, on disk too, and keeps nothing on disk from now on.
    public static void Clear()
    {
        keepDays = 0;
        Recent.Clear();
        DeleteFiles(DateTime.MaxValue);
    }

    // For how many days what is copied stays on disk. Zero brings back to memory the little it can hold and
    // removes the files; otherwise the expired days go and what was only in memory is written.
    public static void Keep(int days)
    {
        keepDays = days;
        if (days == 0)
        {
            Limit();
            foreach (Entry entry in Recent)
            {
                if (entry.Picture) entry.Png = entry.LoadPng();
                entry.FilePath = null;
            }
            DeleteFiles(DateTime.MaxValue);
            return;
        }
        Prune();
        for (int i = Recent.Count - 1; i >= 0; i--)   // from the oldest, the order they were copied in
            if (Recent[i].FilePath == null) Store(Recent[i]);
    }

    // Reads the history files into the list: once, when the program starts.
    public static void Read()
    {
        loaded = true;
        if (keepDays == 0 || !Directory.Exists(Folder)) return;
        foreach (string file in Directory.GetFiles(Folder, "*" + Extension)) ReadFile(file);
        Prune();
        Sort();
    }

    // Newest first. The clock is coarse: copies with the same time keep the order they have in the file.
    static void Sort()
    {
        Recent.Sort(delegate(Entry a, Entry b)
        {
            int byTime = b.Time.CompareTo(a.Time);
            return byTime != 0 ? byTime : b.Offset.CompareTo(a.Offset);
        });
    }

    // Puts back copied texts read from an exported file, each at the time it was copied; those the list
    // holds already are left out. Returns how many went in.
    public static int Restore(List<Entry> entries)
    {
        // Line ends do not count: the file has its own.
        HashSet<string> held = new HashSet<string>();
        foreach (Entry entry in Recent)
            if (!entry.Picture) held.Add(entry.Text.Replace("\r\n", "\n"));
        int added = 0;
        foreach (Entry entry in entries)
        {
            if (entry.Text.Trim().Length == 0 || entry.Text.Length > MaxLength || !held.Add(entry.Text.Replace("\r\n", "\n"))) continue;
            Recent.Add(entry);
            if (keepDays > 0) Store(entry);
            added++;
        }
        Sort();
        if (keepDays == 0) Limit();
        else Prune();
        return added;
    }

    static void ReadFile(string file)
    {
        long end = 0, length = 0;
        try
        {
            using (FileStream stream = new FileStream(file, FileMode.Open, FileAccess.Read))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                length = stream.Length;
                while (end + HeaderSize <= length)
                {
                    byte kind = reader.ReadByte();
                    bool pinned = (kind & PinnedKind) != 0;
                    int number = pinned ? (kind & NumberMask) >> NumberShift : 0;
                    kind &= KindMask;
                    DateTime time = new DateTime(reader.ReadInt64());
                    int size = reader.ReadInt32();
                    if (size < 0 || end + HeaderSize + size > length) break;
                    Entry entry = null;
                    if (kind == TextKind) entry = new Entry(null, Encoding.UTF8.GetString(reader.ReadBytes(size)), time);
                    else if (kind == PictureKind)
                    {
                        int width = reader.ReadInt32(), height = reader.ReadInt32(), previewLength = reader.ReadInt32();
                        // Copied into a bitmap of its own, which does not need the stream to stay open.
                        using (MemoryStream packed = new MemoryStream(reader.ReadBytes(previewLength)))
                        using (Image preview = Image.FromStream(packed))
                        {
                            entry = new Entry(new Bitmap(preview), width, height, time);
                        }
                        entry.PngOffset = stream.Position;
                        entry.PngLength = size - PictureHeader - previewLength;
                    }
                    if (entry != null)
                    {
                        entry.Pinned = pinned;
                        entry.Number = number <= MainForm.MaxNumber ? number : 0;
                        entry.FilePath = file;
                        entry.Offset = end;
                        entry.Length = size;
                        Recent.Add(entry);
                    }
                    end += HeaderSize + size;
                    stream.Position = end;
                }
            }
            // A record cut short, by a crash while it was being written: removed, or whatever is added after
            // it could never be read.
            if (end < length)
                using (FileStream stream = new FileStream(file, FileMode.Open, FileAccess.Write))
                {
                    stream.SetLength(end);
                }
        }
        catch (Exception)
        {
            // A file that cannot be opened or makes no sense: what was read up to here stays, the file too.
        }
    }

    // Adds an entry to the file of its day. If the disk refuses, it simply stays in memory.
    static void Store(Entry entry)
    {
        OnDisk(delegate
        {
            Directory.CreateDirectory(Folder);
            string file = Path.Combine(Folder, entry.Time.ToString(DayFormat, CultureInfo.InvariantCulture) + Extension);
            using (FileStream stream = new FileStream(file, FileMode.Append, FileAccess.Write))
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                long offset = stream.Position;
                byte[] preview = entry.Picture ? Entry.ToPng(entry.Preview) : null;
                byte[] content = entry.Picture ? entry.Png : Encoding.UTF8.GetBytes(entry.Text);
                int length = entry.Picture ? PictureHeader + preview.Length + content.Length : content.Length;
                writer.Write(KindOf(entry));
                writer.Write(entry.Time.Ticks);
                writer.Write(length);
                if (entry.Picture)
                {
                    writer.Write(entry.Width);
                    writer.Write(entry.Height);
                    writer.Write(preview.Length);
                    writer.Write(preview);
                }
                writer.Write(content);
                writer.Flush();
                entry.PngOffset = stream.Position - content.Length;
                entry.Offset = offset;
                entry.Length = length;
                entry.FilePath = file;
            }
            entry.Png = null;   // from now on read from the file when needed
        });
    }

    // Wipes an entry from its history file: zeros over its content, and its record marked as deleted. The
    // file keeps its length and goes away with its day.
    static void Erase(Entry entry)
    {
        if (entry.FilePath == null) return;
        OnDisk(delegate
        {
            using (FileStream stream = new FileStream(entry.FilePath, FileMode.Open, FileAccess.Write))
            {
                stream.Position = entry.Offset;
                stream.WriteByte(DeletedKind);
                stream.Position = entry.Offset + HeaderSize;
                stream.Write(new byte[entry.Length], 0, entry.Length);
            }
        });
        entry.FilePath = null;
    }

    // Removes the days that are over, from the list and from the disk. A pinned entry outlives its day, and
    // its file with it, rewritten without the other entries.
    static void Prune()
    {
        if (!loaded) return;
        pruned = DateTime.Today;
        Drop(pruned.AddDays(-keepDays));
    }

    // Asked by the user: removes at once what was copied before the last 'days' days, today being the first;
    // with zero, all of it. What is pinned stays.
    public static void Clean(int days)
    {
        Drop(DateTime.Today.AddDays(1 - days));
    }

    static void Drop(DateTime oldest)
    {
        Recent.RemoveAll(delegate(Entry entry) { return !entry.Pinned && entry.Time.Date < oldest; });
        DeleteFiles(oldest);
    }

    // Deletes the history files of the days before the given one. A file that still holds entries of the
    // list (the pinned ones) is not deleted but shrunk to them.
    static void DeleteFiles(DateTime before)
    {
        if (!Directory.Exists(Folder)) return;
        // The entries of the list by the name of their file, gathered once for all the files.
        Dictionary<string, List<Entry>> held = new Dictionary<string, List<Entry>>(StringComparer.OrdinalIgnoreCase);
        foreach (Entry entry in Recent)
        {
            if (entry.FilePath == null) continue;
            List<Entry> same;
            string name = Path.GetFileName(entry.FilePath);
            if (!held.TryGetValue(name, out same)) held[name] = same = new List<Entry>();
            same.Add(entry);
        }
        foreach (string file in Directory.GetFiles(Folder, "*" + Extension))
        {
            DateTime day;
            if (!DateTime.TryParseExact(Path.GetFileNameWithoutExtension(file), DayFormat, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out day) || day >= before) continue;
            List<Entry> kept;
            if (!held.TryGetValue(Path.GetFileName(file), out kept)) OnDisk(delegate { File.Delete(file); });
            else OnDisk(delegate { Shrink(file, kept); });
        }
    }

    // Rewrites a history file with only the given entries, which then learn where they are now. Nothing is
    // done to a file that holds nothing else already.
    static void Shrink(string file, List<Entry> kept)
    {
        kept.Sort(delegate(Entry a, Entry b) { return a.Offset.CompareTo(b.Offset); });
        long[] moved = new long[kept.Count];
        long size = 0;
        foreach (Entry entry in kept) size += HeaderSize + entry.Length;
        string copy = file + ".new";
        using (FileStream source = new FileStream(file, FileMode.Open, FileAccess.Read))
        {
            if (source.Length == size) return;
            using (BinaryReader reader = new BinaryReader(source))
            using (FileStream target = new FileStream(copy, FileMode.Create, FileAccess.Write))
            {
                for (int i = 0; i < kept.Count; i++)
                {
                    source.Position = kept[i].Offset;
                    moved[i] = target.Position - kept[i].Offset;
                    byte[] record = reader.ReadBytes(HeaderSize + kept[i].Length);
                    target.Write(record, 0, record.Length);
                }
            }
        }
        File.Delete(file);
        File.Move(copy, file);
        for (int i = 0; i < kept.Count; i++)
        {
            kept[i].Offset += moved[i];
            kept[i].PngOffset += moved[i];
        }
    }

    public static bool IsPrivate(IDataObject data)
    {
        foreach (string format in PrivateIfPresent)
            if (data.GetDataPresent(format)) return true;
        foreach (string format in PrivateIfZero)
        {
            if (!data.GetDataPresent(format)) continue;
            // The format is there: a zero, or a value that cannot be read, both mean "no".
            MemoryStream value = data.GetData(format) as MemoryStream;
            if (value == null || value.Length < sizeof(int) || BitConverter.ToInt32(value.ToArray(), 0) == 0) return true;
        }
        return false;
    }

    // Everything the list can offer: the saved texts by name, those in no group first and then each group
    // followed by its own texts and then by the groups inside it; after them the copied ones from the newest,
    // the pinned ones before the others.
    public static List<Entry> Load()
    {
        List<Entry> entries = new List<Entry>();
        List<string> known = Groups();
        // The texts in each group, in the order of their names.
        Dictionary<string, List<Entry>> grouped = new Dictionary<string, List<Entry>>(StringComparer.OrdinalIgnoreCase);
        foreach (string group in known) grouped[group] = new List<Entry>();
        using (RegistryKey key = Registry.CurrentUser.OpenSubKey(SavedKey))
        using (RegistryKey dates = key == null ? null : key.OpenSubKey(DatesKey))
        using (RegistryKey numbers = key == null ? null : key.OpenSubKey(NumbersKey))
        using (RegistryKey groups = key == null ? null : key.OpenSubKey(GroupsKey))
        using (RegistryKey places = key == null ? null : key.OpenSubKey(PlacesKey))
        {
            string[] names = key == null ? new string[0] : key.GetValueNames();
            Array.Sort(names, StringComparer.CurrentCultureIgnoreCase);
            foreach (string name in names)
            {
                byte[] data = key.GetValue(name) as byte[];
                if (data == null) continue;
                // A text saved before the dates were kept has none.
                object ticks = dates == null ? null : dates.GetValue(name);
                bool dated = ticks is long && (long)ticks > DateTime.MinValue.Ticks && (long)ticks <= DateTime.MaxValue.Ticks;
                try
                {
                    byte[] plain = ProtectedData.Unprotect(data, null, DataProtectionScope.CurrentUser);
                    Entry entry = new Entry(name, Encoding.UTF8.GetString(plain), dated ? new DateTime((long)ticks) : DateTime.MinValue);
                    object number = numbers == null ? null : numbers.GetValue(name);
                    if (number is int && (int)number > 0 && (int)number <= MainForm.MaxNumber) entry.Number = (int)number;
                    // A text whose group is gone is simply in none.
                    string group = places == null ? null : places.GetValue(name) as string;
                    List<Entry> inside;
                    if (group != null && grouped.TryGetValue(group, out inside)) entry.Group = group;
                    else inside = entries;
                    inside.Add(entry);
                }
                catch (CryptographicException)
                {
                    // Saved by another user or on another PC: it cannot be read here.
                }
            }
            foreach (string group in known)
            {
                Entry header = new Entry(group, "", DateTime.MinValue);
                header.IsGroup = true;
                header.Open = groups == null || !Equals(groups.GetValue(group), 0);
                entries.Add(header);
                entries.AddRange(grouped[group]);
            }
        }
        entries.AddRange(Recent.FindAll(delegate(Entry entry) { return entry.Pinned; }));
        entries.AddRange(Recent.FindAll(delegate(Entry entry) { return !entry.Pinned; }));
        return entries;
    }

    // Registry value names do not distinguish case, and so neither the names of texts and groups.
    public static bool SameName(string one, string other)
    {
        return string.Equals(one, other, StringComparison.OrdinalIgnoreCase);
    }

    // 'group' is the group the text goes into, or null for none.
    public static void Save(string name, string text, string group)
    {
        Save(name, text, group, DateTime.Now);
    }

    // With the day it was saved, when it comes from an exported file: DateTime.MinValue if nobody knows.
    public static void Save(string name, string text, string group, DateTime time)
    {
        byte[] secret = ProtectedData.Protect(Encoding.UTF8.GetBytes(text), null, DataProtectionScope.CurrentUser);
        using (RegistryKey key = Registry.CurrentUser.CreateSubKey(SavedKey))
        using (RegistryKey dates = key.CreateSubKey(DatesKey))
        {
            key.DeleteValue(name, false);   // otherwise the registry keeps the letter case of the name it already has
            key.SetValue(name, secret);
            dates.DeleteValue(name, false);
            dates.SetValue(name, time.Ticks, RegistryValueKind.QWord);
        }
        Move(name, group);
    }

    public static void Delete(string name)
    {
        using (RegistryKey key = Registry.CurrentUser.CreateSubKey(SavedKey))
        using (RegistryKey dates = key.CreateSubKey(DatesKey))
        using (RegistryKey numbers = key.CreateSubKey(NumbersKey))
        {
            key.DeleteValue(name, false);
            dates.DeleteValue(name, false);
            numbers.DeleteValue(name, false);
        }
        Move(name, null);
    }

    // Puts a saved text into a group, or into none.
    public static void Move(string name, string group)
    {
        using (RegistryKey places = Registry.CurrentUser.CreateSubKey(Below(PlacesKey)))
        {
            places.DeleteValue(name, false);
            if (group != null) places.SetValue(name, group);
        }
    }

    // The paths of the groups, in the order the list shows them: each just before what it holds, those side by
    // side by name. A group whose outer one is missing from the registry (written by hand) has it all the same.
    public static List<string> Groups()
    {
        using (RegistryKey groups = Registry.CurrentUser.OpenSubKey(Below(GroupsKey)))
        {
            List<string> paths = new List<string>();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string value in groups == null ? new string[0] : groups.GetValueNames())
                for (string path = value; path != null; path = Parent(path))
                    if (seen.Add(path)) paths.Add(path);
            paths.Sort(ByPath);
            return paths;
        }
    }

    // Name by name from the outermost, so that a group comes right before those inside it.
    static int ByPath(string one, string other)
    {
        string[] a = one.Split(Separator), b = other.Split(Separator);
        for (int i = 0; i < Math.Min(a.Length, b.Length); i++)
        {
            int order = StringComparer.CurrentCultureIgnoreCase.Compare(a[i], b[i]);
            if (order != 0) return order;
        }
        return a.Length.CompareTo(b.Length);
    }

    // The last name of a path: what the list shows of a group.
    public static string Leaf(string path)
    {
        return path.Substring(path.LastIndexOf(Separator) + 1);
    }

    // The group a group is in, or null for one outside them all.
    public static string Parent(string path)
    {
        int at = path == null ? -1 : path.LastIndexOf(Separator);
        return at < 0 ? null : path.Substring(0, at);
    }

    // The path of a group called 'name' inside 'parent', or outside them all if that is null.
    public static string Join(string parent, string name)
    {
        return parent == null ? name : parent + Separator + name;
    }

    // How many names a path has: zero for none.
    public static int Depth(string path)
    {
        return path == null ? 0 : path.Split(Separator).Length;
    }

    // Whether 'path' is the group 'group' itself or one somewhere inside it.
    public static bool Within(string path, string group)
    {
        return path != null && group != null
            && (SameName(path, group) || path.StartsWith(group + Separator, StringComparison.OrdinalIgnoreCase));
    }

    // A path as the user reads it: "Lavoro › Server".
    public static string Display(string path)
    {
        return path.Replace(Separator.ToString(), " › ");
    }

    // A path written by hand, in an exported file: without empty names or spaces around them. Null if nothing is left.
    public static string CleanPath(string path)
    {
        if (path == null) return null;
        List<string> names = new List<string>();
        foreach (string name in path.Split(Separator))
            if (name.Trim().Length > 0) names.Add(name.Trim());
        return names.Count == 0 ? null : string.Join(Separator.ToString(), names);
    }

    // Creates a group, or opens and closes one that is there.
    public static void SetGroup(string name, bool open)
    {
        using (RegistryKey groups = Registry.CurrentUser.CreateSubKey(Below(GroupsKey)))
        {
            groups.SetValue(name, open ? 1 : 0, RegistryValueKind.DWord);
        }
    }

    // Opens a group and those it is inside of, so that what it holds is seen; null is no group.
    public static void Reveal(string path)
    {
        for (; path != null; path = Parent(path)) SetGroup(path, true);
    }

    // Gives a group another path, which moves it if its outer part changes, along with the groups inside it
    // and every text they hold. With 'name' null the group goes, and what it held moves up into the group
    // that held it: its texts are too easily passwords to go along.
    public static void RenameGroup(string old, string name)
    {
        using (RegistryKey groups = Registry.CurrentUser.CreateSubKey(Below(GroupsKey)))
        using (RegistryKey places = Registry.CurrentUser.CreateSubKey(Below(PlacesKey)))
        {
            foreach (string group in groups.GetValueNames())
            {
                string moved = Rebase(group, old, name);
                if (moved == group) continue;   // compared exactly: a change of letter case alone counts
                object open = groups.GetValue(group);
                groups.DeleteValue(group, false);   // first, or a change of letter case alone would not show
                if (name != null || !SameName(group, old)) groups.SetValue(moved, open, RegistryValueKind.DWord);
            }
            foreach (string text in places.GetValueNames())
            {
                string place = places.GetValue(text) as string, moved = Rebase(place, old, name);
                if (moved == place) continue;
                if (moved != null) places.SetValue(text, moved);
                else places.DeleteValue(text, false);
            }
        }
    }

    // Where a path goes when the group 'old' becomes 'name': the same place under the new path. With 'name'
    // null, one level up, which for the group itself is the group that held it.
    static string Rebase(string path, string old, string name)
    {
        if (!Within(path, old)) return path;
        string root = name ?? Parent(old);
        return SameName(path, old) ? root : Join(root, path.Substring(old.Length + 1));
    }

    // Every word of the query must appear somewhere, in any order; case and accents do not count.
    public static bool Matches(Entry entry, string query)
    {
        return Matches(entry, Words(query));
    }

    // The same with the words already taken from the query, once for all the entries. The name of its folder finds
    // a text too.
    public static bool Matches(Entry entry, string[] words)
    {
        if (words.Length == 0) return true;
        string group = Fold(entry.Group), name = Fold(entry.Name);   // short, unlike the text
        foreach (string word in words)
            if (!Holds(group, word) && !Holds(name, word) && !Holds(entry.Folded, word)) return false;
        return true;
    }

    // The words of a query, as Fold writes them.
    public static string[] Words(string query)
    {
        string[] words = query.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < words.Length; i++) words[i] = Fold(words[i]);
        return words;
    }

    static bool Holds(string where, string word)
    {
        return where != null && where.IndexOf(word, StringComparison.Ordinal) >= 0;
    }

    // A text as the search compares it: without accents and in small letters, "Città" as "citta". Done once per
    // text, it makes the search many times faster than comparing while ignoring them.
    public static string Fold(string text)
    {
        if (text == null) return null;
        string decomposed;
        try
        {
            decomposed = text.Normalize(NormalizationForm.FormD);   // a letter and its accent apart
        }
        catch (ArgumentException)
        {
            decomposed = text;   // half of a surrogate pair, which cannot be normalised: as it is
        }
        StringBuilder plain = new StringBuilder(decomposed.Length);
        foreach (char c in decomposed)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) plain.Append(c);
        return plain.ToString().ToLowerInvariant();
    }
}
