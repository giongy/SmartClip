using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

// What an entry is, as the tabs above the list choose it: a text, a piece of a program, a web address, a picture.
// The numbers are those the database keeps for the copied entries.
enum Category { Text = 1, Code = 2, Link = 3, Picture = 4 }

// What the list offers: something copied, a text or a picture, or an entry the user saved, which may have a name.
// Also the row of a folder of saved entries (IsGroup), named by its path. Where they are kept: Clips.
class Entry
{
    const int MaxPreview = 120;      // beyond this, the text of a copied entry is truncated in the list
    const int PreviewHeight = 120;   // of the small copy of a picture shown in the list: enough for a sharp row at any scale
    public readonly string Name, Text;
    public readonly DateTime Time;   // when it was copied, or saved: DateTime.MinValue if nobody knows
    public readonly bool Picture;
    public bool Pinned;              // a copied entry kept at the top of its column for a while: it goes with its days
    public int Number;               // the digit that pastes it from anywhere: 0 for none
    public string Group;             // the path of the folder a saved entry is in, or null
    public bool IsGroup, Open;       // not an entry but a folder of saved ones, named by its path; and whether it shows them
    // In the database: its row, whether it is saved (a saved entry may have no name), and its folder (0: none). The
    // row of a folder has the folder's id as FolderId.
    public long Id;
    public bool Saved;
    public long FolderId;
    // Of a formatted saved text (Rich), the HTML it is pasted as, and the RTF if it was copied with one; null for
    // the others. Text is its plain text, as it is typed and searched.
    public string Html, Rtf;
    public Origin Origin;                // the program a copied entry was copied in: null if nobody knows, and for a saved one
    public readonly int Width, Height;   // of a picture
    public readonly Image Preview;       // null for a text
    public byte[] Png;                   // a picture just copied, as PNG
    public Func<byte[]> PngSource;       // where the whole picture is read when it is needed, once the database has it
    string line;   // what the list shows of a copied text, worked out once: rows are drawn often, and texts can be long
    string folded; // the text as the search compares it, worked out at the first search
    Category? category;   // what it is, worked out once

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
    }

    // A picture as the database gives it back: the preview only, the rest is read when it is chosen.
    public Entry(Image preview, int width, int height, DateTime time) : this(preview, width, height, time, null)
    {
    }

    // The same, saved under a name, or with 'name' null.
    public Entry(Image preview, int width, int height, DateTime time, string name)
    {
        Name = name;
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

    // The whole picture, as PNG.
    public byte[] LoadPng()
    {
        return Png ?? PngSource();
    }

    // What the list shows. Of a saved entry with a name only the name: a saved password never appears on screen.
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

    // What it is: as the database says, or else guessed from its text.
    public Category Category
    {
        get
        {
            if (!category.HasValue) category = Picture ? Category.Picture : Texts.CategoryOf(Text);
            return category.Value;
        }
        set { category = value; }
    }

    public bool Chosen;   // the user said what it is, whatever was guessed

    // How many folders it is inside of: zero for a folder or an entry outside them all.
    public int Level
    {
        get { return IsGroup ? Texts.Depth(Name) - 1 : Texts.Depth(Group); }
    }

    // The sign that tells a saved entry and a picture from a copied text.
    public string Mark
    {
        get { return IsGroup ? "" : Saved ? "★" : Picture ? "▣" : ""; }
    }

    public override string ToString()
    {
        return (Mark + " " + Label).TrimStart();
    }
}

// Around the entries: what the clipboard holds and whether it may be remembered, the search, the paths of the
// folders as the list writes them.
static class Texts
{
    public const int MaxKeepDays = 365, DefaultKeepDays = 30;
    public const int MaxLength = 100000;   // a longer text is more likely a whole document than something to use again
    public const string DayFormat = "yyyy-MM-dd";
    // Folders hold folders: the list knows a folder by its path, the names from the outermost down joined by this
    // sign, which a name cannot contain.
    public const char Separator = '\\';
    // Clipboard formats with which password managers and private browser windows mark what must not be
    // recorded: the first two by merely being there, the others by holding a zero.
    public const string PrivateMarker = "ExcludeClipboardContentFromMonitorProcessing";
    static readonly string[] PrivateIfPresent = { "Clipboard Viewer Ignore", PrivateMarker };
    static readonly string[] PrivateIfZero = { "CanIncludeInClipboardHistory", "CanUploadToCloudClipboard" };

    // Called at every clipboard change: what it holds goes to the copied entries, unless it is marked private.
    // Returns whether the copied entries may have changed, and the list has to be read again.
    public static bool Remember(Clips clips)
    {
        try
        {
            IDataObject data = Clipboard.GetDataObject();
            if (data == null || IsPrivate(data)) return false;
            string program = Origin.Find();   // now, while the program that copied still owns the clipboard
            // Programs that copy both (a spreadsheet, a word processor) mean the text. Its formatting, if it has
            // some, stays in memory for a while, in case it is saved.
            string text = data.GetData(DataFormats.UnicodeText) as string;
            if (text != null && clips.AddText(text, program))
            {
                Rich.Remember(text, data);
                return true;
            }
            using (Image picture = data.GetData(DataFormats.Bitmap) as Image)
            {
                if (picture == null) return false;
                clips.AddPicture(picture, program);
                return true;
            }
        }
        catch (ExternalException)
        {
            // The clipboard held by another program: this copy is not remembered, and that is all.
            return false;
        }
        catch (Exception ex)
        {
            // A picture that cannot be read, a database that refuses: written down, never in the way of the copy
            // just made in another program. Something may have been written all the same.
            Log.Error(ex);
            return true;
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

    // What a text is: a web address alone, a piece of a program, or any other text.
    public static Category CategoryOf(string text)
    {
        string bare = text.Trim();
        if (IsLink(bare)) return Category.Link;
        return LooksLikeCode(bare) ? Category.Code : Category.Text;
    }

    static readonly string[] LinkStarts = { "http://", "https://", "www." };

    // An address and nothing else: no space in it.
    static bool IsLink(string bare)
    {
        foreach (string start in LinkStarts)
            if (bare.StartsWith(start, StringComparison.OrdinalIgnoreCase))
            {
                foreach (char c in bare) if (char.IsWhiteSpace(c)) return false;
                return true;
            }
        return false;
    }

    // How a line of a program often begins, in the languages most copied.
    static readonly string[] CodeStarts =
    {
        "//", "/*", "#include", "#!/", "<?", "</", "def ", "function ", "class ", "import ", "from ", "using ", "namespace ",
        "public ", "private ", "protected ", "static ", "return ", "var ", "let ", "const ", "if (", "if(", "for (", "for(",
        "while (", "foreach (", "} else", "else {", "switch (", "try {", "catch (", "SELECT ", "INSERT ", "UPDATE ", "DELETE FROM ",
        "CREATE ", "$ ", "@echo", "echo ", "Get-", "Set-"
    };
    static readonly string[] CodeSigns = { "=>", "==", "!=", "&&", "||", "->", "::", "();", ") {", "){" };

    // A guess: most lines of the text end or begin as lines of programs do, or its one line does. Prose with
    // semicolons, or a list in braces, can fool it.
    static bool LooksLikeCode(string bare)
    {
        int filled = 0, code = 0;
        foreach (string line in bare.Split('\n'))
        {
            string trimmed = line.Trim();
            if (trimmed.Length == 0) continue;
            filled++;
            if (CodeLine(trimmed)) code++;
        }
        return filled == 1 ? code == 1 : code >= 2 && code * 5 >= filled * 2;   // two lines in five at least
    }

    static bool CodeLine(string line)
    {
        char last = line[line.Length - 1];
        if (last == ';' || last == '{' || last == '}' || line == ")" || line == "];") return true;
        foreach (string start in CodeStarts) if (line.StartsWith(start, StringComparison.Ordinal)) return true;
        foreach (string sign in CodeSigns) if (line.IndexOf(sign, StringComparison.Ordinal) >= 0) return true;
        return false;
    }

    // Names of saved entries and folders do not distinguish letter case.
    public static bool SameName(string one, string other)
    {
        return string.Equals(one, other, StringComparison.OrdinalIgnoreCase);
    }

    // The last name of a path: what the list shows of a folder.
    public static string Leaf(string path)
    {
        return path.Substring(path.LastIndexOf(Separator) + 1);
    }

    // The folder a folder is in, or null for one outside them all.
    public static string Parent(string path)
    {
        int at = path == null ? -1 : path.LastIndexOf(Separator);
        return at < 0 ? null : path.Substring(0, at);
    }

    // The path of a folder called 'name' inside 'parent', or outside them all if that is null.
    public static string Join(string parent, string name)
    {
        return parent == null ? name : parent + Separator + name;
    }

    // How many names a path has: zero for none.
    public static int Depth(string path)
    {
        return path == null ? 0 : path.Split(Separator).Length;
    }

    // Whether 'path' is the folder 'group' itself or one somewhere inside it.
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

    // Every word of the query must appear somewhere, in any order; case and accents do not count.
    public static bool Matches(Entry entry, string query)
    {
        return Matches(entry, Words(query));
    }

    // The same with the words already taken from the query, once for all the entries. The name of its folder finds
    // an entry too.
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
