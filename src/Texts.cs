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
    public readonly int Width, Height;   // of a picture
    public readonly Image Preview;       // null for a text
    public byte[] Png;                   // a picture while it is only in memory: null once the history file holds it
    // Where the history file holds a copied entry: FilePath is null while it is only in memory. Offset and
    // Length are those of its record, PngOffset and PngLength those of the picture inside it.
    public string FilePath;
    public long Offset, PngOffset;
    public int Length, PngLength;

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
        Text = "Immagine " + width + "×" + height + ", copiata "
            + (time.Date == DateTime.Today ? "" : "il " + time.ToString("dd/MM") + " ") + "alle " + time.ToString("HH:mm:ss");
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
            if (Name != null) return Name;
            if (Picture) return "Immagine " + Width + "×" + Height;   // when it was copied is shown beside it
            string line = Text.Trim().Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ');
            return line.Length > MaxPreview ? line.Substring(0, MaxPreview) + "…" : line;
        }
    }

    // The sign that tells a saved text and a picture from a copied text.
    public string Mark
    {
        get { return Name != null ? "★" : Picture ? "▣" : ""; }
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
    const int HeaderSize = sizeof(byte) + sizeof(long) + sizeof(int), PictureHeader = 3 * sizeof(int);
    const string DayFormat = "yyyy-MM-dd", Extension = ".clips";
    public static string Folder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Program.AppName, "History");
    static int keepDays;       // zero: nothing goes to disk
    static DateTime pruned;    // the day the expired files were last removed
    static bool loaded;        // the history files were read: before that, nobody knows which hold a pinned entry
    const string SavedKey = MainForm.SettingsKey + @"\Saved";
    const string DatesKey = "Dates";   // under the saved texts: when each was saved, under the same name
    // Clipboard formats with which password managers and private browser windows mark what must not be
    // recorded: the first two by merely being there, the others by holding a zero.
    public const string PrivateMarker = "ExcludeClipboardContentFromMonitorProcessing";
    static readonly string[] PrivateIfPresent = { "Clipboard Viewer Ignore", PrivateMarker };
    static readonly string[] PrivateIfZero = { "CanIncludeInClipboardHistory", "CanUploadToCloudClipboard" };
    static readonly CompareInfo Comparer = CultureInfo.InvariantCulture.CompareInfo;
    const CompareOptions Loose = CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;

    public static readonly List<Entry> Recent = new List<Entry>();   // newest first

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
    // once, if its days are over.
    public static void Pin(Entry entry, bool pinned)
    {
        entry.Pinned = pinned;
        if (entry.FilePath != null)
        {
            try
            {
                using (FileStream stream = new FileStream(entry.FilePath, FileMode.Open, FileAccess.Write))
                {
                    stream.Position = entry.Offset;
                    stream.WriteByte(KindOf(entry));
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
        if (!pinned && keepDays > 0) Prune();
    }

    static byte KindOf(Entry entry)
    {
        return (byte)((entry.Picture ? PictureKind : TextKind) | (entry.Pinned ? PinnedKind : 0));
    }

    // Forgets everything that was copied, on disk too, and keeps nothing on disk from now on.
    public static void Clear()
    {
        keepDays = 0;
        Recent.Clear();
        DeleteFiles(DateTime.MaxValue, new List<string>());
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
            DeleteFiles(DateTime.MaxValue, new List<string>());
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
        // Newest first. The clock is coarse: copies with the same time keep the order they have in the file.
        Recent.Sort(delegate(Entry a, Entry b)
        {
            int byTime = b.Time.CompareTo(a.Time);
            return byTime != 0 ? byTime : b.Offset.CompareTo(a.Offset);
        });
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
                    if (pinned) kind -= PinnedKind;
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
        try
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
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    // Wipes an entry from its history file: zeros over its content, and its record marked as deleted. The
    // file keeps its length and goes away with its day.
    static void Erase(Entry entry)
    {
        if (entry.FilePath == null) return;
        try
        {
            using (FileStream stream = new FileStream(entry.FilePath, FileMode.Open, FileAccess.Write))
            {
                stream.Position = entry.Offset;
                stream.WriteByte(DeletedKind);
                stream.Position = entry.Offset + HeaderSize;
                stream.Write(new byte[entry.Length], 0, entry.Length);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
        entry.FilePath = null;
    }

    // Removes the days that are over, from the list and from the disk. A pinned entry outlives its day, and
    // its file with it: the other entries of that file are wiped one by one.
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
        List<string> held = new List<string>();
        foreach (Entry entry in Recent)
            if (entry.Pinned && entry.FilePath != null && entry.Time.Date < oldest) held.Add(Path.GetFileName(entry.FilePath));
        Recent.RemoveAll(delegate(Entry entry)
        {
            if (entry.Pinned || entry.Time.Date >= oldest) return false;
            if (entry.FilePath != null && held.Contains(Path.GetFileName(entry.FilePath))) Erase(entry);
            return true;
        });
        DeleteFiles(oldest, held);
    }

    // Deletes the history files of the days before the given one, except those named in 'held'.
    static void DeleteFiles(DateTime before, List<string> held)
    {
        if (!Directory.Exists(Folder)) return;
        foreach (string file in Directory.GetFiles(Folder, "*" + Extension))
        {
            DateTime day;
            if (!DateTime.TryParseExact(Path.GetFileNameWithoutExtension(file), DayFormat, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out day) || day >= before || held.Contains(Path.GetFileName(file))) continue;
            try
            {
                File.Delete(file);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
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

    // Everything the list can offer: the saved texts by name, then the copied ones from the newest, the
    // pinned ones before the others.
    public static List<Entry> Load()
    {
        List<Entry> entries = new List<Entry>();
        using (RegistryKey key = Registry.CurrentUser.OpenSubKey(SavedKey))
        using (RegistryKey dates = key == null ? null : key.OpenSubKey(DatesKey))
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
                    entries.Add(new Entry(name, Encoding.UTF8.GetString(plain), dated ? new DateTime((long)ticks) : DateTime.MinValue));
                }
                catch (CryptographicException)
                {
                    // Saved by another user or on another PC: it cannot be read here.
                }
            }
        }
        entries.AddRange(Recent.FindAll(delegate(Entry entry) { return entry.Pinned; }));
        entries.AddRange(Recent.FindAll(delegate(Entry entry) { return !entry.Pinned; }));
        return entries;
    }

    public static void Save(string name, string text)
    {
        byte[] secret = ProtectedData.Protect(Encoding.UTF8.GetBytes(text), null, DataProtectionScope.CurrentUser);
        using (RegistryKey key = Registry.CurrentUser.CreateSubKey(SavedKey))
        using (RegistryKey dates = key.CreateSubKey(DatesKey))
        {
            key.DeleteValue(name, false);   // otherwise the registry keeps the letter case of the name it already has
            key.SetValue(name, secret);
            dates.DeleteValue(name, false);
            dates.SetValue(name, DateTime.Now.Ticks, RegistryValueKind.QWord);
        }
    }

    public static void Delete(string name)
    {
        using (RegistryKey key = Registry.CurrentUser.CreateSubKey(SavedKey))
        using (RegistryKey dates = key.CreateSubKey(DatesKey))
        {
            key.DeleteValue(name, false);
            dates.DeleteValue(name, false);
        }
    }

    // Every word of the query must appear somewhere, in any order; case and accents do not count.
    public static bool Matches(Entry entry, string query)
    {
        string where = entry.Name + "\n" + entry.Text;
        foreach (string word in query.Split((char[])null, StringSplitOptions.RemoveEmptyEntries))
            if (Comparer.IndexOf(where, word, Loose) < 0) return false;
        return true;
    }
}
