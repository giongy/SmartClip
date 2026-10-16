using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

// What an export wrote, or an import took in.
class BackupResult
{
    public int Saved, Copied;   // texts of the two kinds
    public int Pictures;        // left out of an export: the file is text
    public int Kept;            // saved texts of the file left out of an import, because the name was taken
}

// The words that structure an exported file, in one language: the mark that opens each entry and the names
// of its fields.
class BackupWords
{
    public readonly string Group, Saved, Copied, Name, Folder, Time, Text, Pinned;

    public BackupWords(string group, string saved, string copied, string name, string folder, string time, string text, string pinned)
    {
        Group = group;
        Saved = saved;
        Copied = copied;
        Name = name;
        Folder = folder;
        Time = time;
        Text = text;
        Pinned = pinned;
    }
}

// The saved texts, and on request what was copied, written as they are to a text file that Notepad shows,
// and read back from it. Each entry is a mark on a line of its own, a few fields, then its text: one line of
// the file per line of text, each after a tab, which is what tells it from the rest. The file is written in
// the language in use and read in any of them: a file can go to a PC set to another language, and those
// written before there were languages are in Italian.
static class Backup
{
    static readonly Dictionary<string, BackupWords> Words = new Dictionary<string, BackupWords>
    {
        { "en", new BackupWords("[Folder]", "[Saved]", "[Copied]", "Name: ", "Folder: ", "Date: ", "Text:", "Pinned: yes") },
        { "es", new BackupWords("[Carpeta]", "[Guardado]", "[Copiado]", "Nombre: ", "Carpeta: ", "Fecha: ", "Texto:", "Arriba: sí") },
        { "fr", new BackupWords("[Dossier]", "[Enregistré]", "[Copié]", "Nom : ", "Dossier : ", "Date : ", "Texte :", "En haut : oui") },
        { "it", new BackupWords("[Cartella]", "[Salvato]", "[Copiato]", "Nome: ", "Cartella: ", "Data: ", "Testo:", "In alto: sì") }
    };
    const int NoMark = 0, GroupMark = 1, SavedMark = 2, CopiedMark = 3;   // what an entry of the file is
    const string Indent = "\t";
    const string TimeFormat = "yyyy-MM-dd HH:mm:ss";
    public const string Extension = ".txt";

    // 'all': what was copied too, not only the saved texts.
    public static BackupResult Export(string path, bool all)
    {
        List<Entry> entries = Texts.Load();
        BackupResult result = new BackupResult();
        foreach (Entry entry in entries)
        {
            if (entry.IsGroup || entry.Name == null && !all) continue;
            if (entry.Name != null) result.Saved++;
            else if (entry.Picture) result.Pictures++;
            else result.Copied++;
        }
        File.WriteAllLines(path, Lines(entries, all), new UTF8Encoding(true));   // with the mark that tells Notepad the encoding
        return result;
    }

    public static List<string> Lines(List<Entry> entries, bool all)
    {
        BackupWords words;
        if (!Words.TryGetValue(Strings.Code, out words)) words = Words["en"];
        List<string> lines = new List<string>();
        lines.Add(string.Format(Strings.ExportHeader, Program.AppName, Program.Version, DateTime.Now.ToString("g", Strings.Culture)));
        lines.Add(Strings.ExportClearWarning);
        lines.Add(Strings.ExportEditNote);
        foreach (Entry entry in entries)
        {
            if (entry.Picture || entry.Name == null && !all) continue;
            lines.Add("");
            lines.Add(entry.IsGroup ? words.Group : entry.Name != null ? words.Saved : words.Copied);
            if (entry.Name != null) lines.Add(words.Name + entry.Name);
            if (entry.Group != null) lines.Add(words.Folder + entry.Group);
            if (entry.Time != DateTime.MinValue) lines.Add(words.Time + entry.Time.ToString(TimeFormat, CultureInfo.InvariantCulture));
            if (entry.Pinned) lines.Add(words.Pinned);
            if (entry.IsGroup) continue;
            lines.Add(words.Text);
            foreach (string line in entry.Text.Replace("\r\n", "\n").Split('\n')) lines.Add(Indent + line);
        }
        return lines;
    }

    // What a line opens, in any of the languages: an entry of the file, or none.
    static int MarkOf(string line)
    {
        foreach (BackupWords words in Words.Values)
        {
            if (line == words.Group) return GroupMark;
            if (line == words.Saved) return SavedMark;
            if (line == words.Copied) return CopiedMark;
        }
        return NoMark;
    }

    // The value of a field, if the line is that field in any of the languages; otherwise null.
    static string Field(string line, Func<BackupWords, string> name)
    {
        foreach (BackupWords words in Words.Values)
        {
            string field = name(words);
            if (line.StartsWith(field)) return line.Substring(field.Length).Trim();
        }
        return null;
    }

    // Whether the line is exactly that word, in any of the languages.
    static bool Is(string line, Func<BackupWords, string> word)
    {
        foreach (BackupWords words in Words.Values)
            if (line == word(words)) return true;
        return false;
    }

    // The entries of an exported file, as it is or after the user changed it by hand: what makes no sense is
    // skipped. A folder comes as an entry of its own, like in the list.
    public static List<Entry> Parse(IEnumerable<string> lines)
    {
        List<Entry> entries = new List<Entry>();
        int mark = NoMark;
        string name = null, group = null;
        DateTime time = DateTime.MinValue;
        bool pinned = false;
        List<string> text = null;   // the lines of the text, once its field is met
        int blanks = 0;             // empty lines met inside a text: they count only if more text follows
        Action close = delegate
        {
            string content = text == null ? "" : string.Join("\r\n", text);
            if (mark == GroupMark && Texts.CleanPath(name) != null)
            {
                Entry folder = new Entry(Texts.CleanPath(name), "", DateTime.MinValue);
                folder.IsGroup = true;
                entries.Add(folder);
            }
            else if (mark == SavedMark && !string.IsNullOrEmpty(name) && content.Trim().Length > 0)
            {
                Entry saved = new Entry(name, content, time);
                saved.Group = Texts.CleanPath(group);   // a folder inside another is its path, as in the registry
                entries.Add(saved);
            }
            else if (mark == CopiedMark && content.Trim().Length > 0)
            {
                Entry copied = new Entry(null, content, time == DateTime.MinValue ? DateTime.Now : time);
                copied.Pinned = pinned;
                entries.Add(copied);
            }
            mark = NoMark;
            name = group = null;
            time = DateTime.MinValue;
            pinned = false;
            text = null;
            blanks = 0;
        };
        foreach (string line in lines)
        {
            string value;
            if (text != null && line.StartsWith(Indent))
            {
                for (; blanks > 0; blanks--) text.Add("");
                text.Add(line.Substring(Indent.Length));
            }
            else if (text != null && line.Length == 0) blanks++;
            else if (MarkOf(line) != NoMark)
            {
                close();
                mark = MarkOf(line);
            }
            else if (mark == NoMark || text != null) continue;   // the heading of the file, or something after a text
            else if ((value = Field(line, delegate(BackupWords words) { return words.Name; })) != null) name = value;
            else if ((value = Field(line, delegate(BackupWords words) { return words.Folder; })) != null) group = value;
            else if ((value = Field(line, delegate(BackupWords words) { return words.Time; })) != null)
                DateTime.TryParseExact(value, TimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out time);
            else if (Is(line, delegate(BackupWords words) { return words.Pinned; })) pinned = true;
            else if (Is(line, delegate(BackupWords words) { return words.Text; })) text = new List<string>();
        }
        close();
        return entries;
    }

    // Takes in an exported file. A saved text whose name is taken replaces the one there only if 'replace',
    // told how many there are, says so. 'all': the copied texts too, those the list does not hold already.
    public static BackupResult Import(string path, bool all, Func<int, bool> replace)
    {
        List<Entry> found = Parse(File.ReadAllLines(path, Encoding.UTF8)), copied = new List<Entry>();
        // The names of the saved texts there are, as the registry compares them.
        HashSet<string> present = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Entry entry in Texts.Load())
            if (entry.Name != null && !entry.IsGroup) present.Add(entry.Name);
        Predicate<Entry> taken = delegate(Entry entry) { return entry.Name != null && !entry.IsGroup && present.Contains(entry.Name); };
        int clashes = found.FindAll(taken).Count;
        bool overwrite = clashes > 0 && replace(clashes);
        HashSet<string> groups = new HashSet<string>(Texts.Groups(), StringComparer.OrdinalIgnoreCase);
        BackupResult result = new BackupResult();
        foreach (Entry entry in found)
        {
            if (entry.Name == null)
            {
                if (all) copied.Add(entry);
                continue;
            }
            // A folder of the file, or one a text names without the file having it.
            string group = entry.IsGroup ? entry.Name : entry.Group;
            if (group != null && groups.Add(group)) Texts.SetGroup(group, true);
            if (entry.IsGroup) continue;
            if (taken(entry) && !overwrite)
            {
                result.Kept++;
                continue;
            }
            Texts.Save(entry.Name, entry.Text, entry.Group, entry.Time);
            result.Saved++;
        }
        result.Copied = Texts.Restore(copied);
        return result;
    }
}
