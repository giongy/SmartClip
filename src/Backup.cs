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
    public readonly string Group, Saved, Copied, Name, Folder, Time, Text, Pinned, Html;

    public BackupWords(string group, string saved, string copied, string name, string folder, string time, string text, string pinned,
        string html)
    {
        Group = group;
        Saved = saved;
        Copied = copied;
        Name = name;
        Folder = folder;
        Time = time;
        Text = text;
        Pinned = pinned;
        Html = html;
    }
}

// The saved texts, and on request what was copied, written as they are to a text file that Notepad shows,
// and read back from it. Each entry is a mark on a line of its own, a few fields, then its text: one line of
// the file per line of text, each after a tab, which is what tells it from the rest. A formatted text has its
// HTML before its text, in the same way: a version that does not know the field passes over it. The file is written in
// the language in use and read in any of them: a file can go to a PC set to another language, and those
// written before there were languages are in Italian.
static class Backup
{
    static readonly Dictionary<string, BackupWords> Words = new Dictionary<string, BackupWords>
    {
        { "en", new BackupWords("[Folder]", "[Saved]", "[Copied]", "Name: ", "Folder: ", "Date: ", "Text:", "Pinned: yes", "HTML:") },
        { "es", new BackupWords("[Carpeta]", "[Guardado]", "[Copiado]", "Nombre: ", "Carpeta: ", "Fecha: ", "Texto:", "Arriba: sí", "HTML:") },
        { "fr", new BackupWords("[Dossier]", "[Enregistré]", "[Copié]", "Nom : ", "Dossier : ", "Date : ", "Texte :", "En haut : oui", "HTML :") },
        { "it", new BackupWords("[Cartella]", "[Salvato]", "[Copiato]", "Nome: ", "Cartella: ", "Data: ", "Testo:", "In alto: sì", "HTML:") }
    };
    const int NoMark = 0, GroupMark = 1, SavedMark = 2, CopiedMark = 3;   // what an entry of the file is
    const string Indent = "\t";
    const string TimeFormat = "yyyy-MM-dd HH:mm:ss";
    public const string Extension = ".txt";

    // Writes the entries given, the saved ones with the rows of their folders and, if asked, the copied ones.
    public static BackupResult Export(string path, List<Entry> entries)
    {
        BackupResult result = new BackupResult();
        foreach (Entry entry in entries)
        {
            if (entry.IsGroup) continue;
            if (entry.Picture) result.Pictures++;
            else if (entry.Saved) result.Saved++;
            else result.Copied++;
        }
        File.WriteAllLines(path, Lines(entries), new UTF8Encoding(true));   // with the mark that tells Notepad the encoding
        return result;
    }

    public static List<string> Lines(List<Entry> entries)
    {
        BackupWords words;
        if (!Words.TryGetValue(Strings.Code, out words)) words = Words["en"];
        List<string> lines = new List<string>();
        lines.Add(string.Format(Strings.ExportHeader, Program.AppName, Program.Version, DateTime.Now.ToString("g", Strings.Culture)));
        lines.Add(Strings.ExportClearWarning);
        lines.Add(Strings.ExportEditNote);
        foreach (Entry entry in entries)
        {
            if (entry.Picture) continue;
            lines.Add("");
            lines.Add(entry.IsGroup ? words.Group : entry.Saved ? words.Saved : words.Copied);
            if (entry.Name != null) lines.Add(words.Name + entry.Name);
            if (entry.Group != null) lines.Add(words.Folder + entry.Group);
            if (entry.Time != DateTime.MinValue) lines.Add(words.Time + entry.Time.ToString(TimeFormat, CultureInfo.InvariantCulture));
            if (entry.Pinned) lines.Add(words.Pinned);
            if (entry.IsGroup) continue;
            if (entry.Saved && entry.Html != null)
            {
                lines.Add(words.Html);
                Indented(lines, entry.Html);
            }
            lines.Add(words.Text);
            Indented(lines, entry.Text);
        }
        return lines;
    }

    static void Indented(List<string> lines, string text)
    {
        foreach (string line in text.Replace("\r\n", "\n").Split('\n')) lines.Add(Indent + line);
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
            if (line.StartsWith(field, StringComparison.Ordinal)) return line.Substring(field.Length).Trim();
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
        List<string> html = null;   // the same of the HTML of a formatted text, which comes before it
        List<string> into = null;   // which of the two the lines after a tab go to
        int blanks = 0;             // empty lines met inside a text: they count only if more text follows
        Action close = delegate
        {
            string content = text == null ? "" : string.Join("\r\n", text);
            if (mark == GroupMark && Texts.CleanPath(name) != null)
            {
                Entry folder = new Entry(Texts.CleanPath(name), "", DateTime.MinValue);
                folder.IsGroup = true;
                folder.Saved = true;
                entries.Add(folder);
            }
            else if (mark == SavedMark && content.Trim().Length > 0)   // the name is optional
            {
                Entry saved = new Entry(name, content, time);
                saved.Saved = true;
                saved.Group = Texts.CleanPath(group);   // a folder inside another is its path, as the list writes it
                if (html != null && string.Concat(html).Trim().Length > 0) saved.Html = string.Join("\r\n", html);
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
            text = html = into = null;
            blanks = 0;
        };
        foreach (string line in lines)
        {
            string value;
            int opens;
            if (into != null && line.StartsWith(Indent, StringComparison.Ordinal))
            {
                for (; blanks > 0; blanks--) into.Add("");
                into.Add(line.Substring(Indent.Length));
            }
            else if (into != null && line.Length == 0) blanks++;
            else if ((opens = MarkOf(line)) != NoMark)
            {
                close();
                mark = opens;
            }
            else if (mark == NoMark || text != null) continue;   // the heading of the file, or something after a text
            else if ((value = Field(line, delegate(BackupWords words) { return words.Name; })) != null) name = value;
            else if ((value = Field(line, delegate(BackupWords words) { return words.Folder; })) != null) group = value;
            else if ((value = Field(line, delegate(BackupWords words) { return words.Time; })) != null)
                DateTime.TryParseExact(value, TimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out time);
            else if (Is(line, delegate(BackupWords words) { return words.Pinned; })) pinned = true;
            else if (Is(line, delegate(BackupWords words) { return words.Html; })) into = html = new List<string>();
            else if (Is(line, delegate(BackupWords words) { return words.Text; }))
            {
                into = text = new List<string>();
                blanks = 0;   // those after the HTML belong to neither
            }
        }
        close();
        return entries;
    }

    // Takes in an exported file, all of it or nothing. A saved text whose name is taken replaces the one there only
    // if 'replace', told how many there are, says so. 'all': the copied texts too, those the list does not hold
    // already; those whose days are over go at once.
    public static BackupResult Import(Clips clips, string path, bool all, Func<int, bool> replace)
    {
        List<Entry> found = Parse(File.ReadAllLines(path, Encoding.UTF8));
        Predicate<Entry> taken = delegate(Entry entry) { return entry.Saved && !entry.IsGroup && Named(clips, entry) != 0; };
        int clashes = found.FindAll(taken).Count;
        bool overwrite = clashes > 0 && replace(clashes);
        // Saved entries without a name have nothing to be known by but their content: one the list has is not repeated.
        HashSet<string> unnamed = new HashSet<string>();
        foreach (Entry entry in clips.Saved())
            if (entry.Name == null && !entry.Picture) unnamed.Add(entry.Text.Replace("\r\n", "\n"));
        BackupResult result = new BackupResult();
        using (Transaction transaction = clips.Begin())
        {
            foreach (Entry entry in found)
            {
                if (!entry.Saved)
                {
                    if (all && clips.Import(entry.Text, entry.Time, entry.Pinned)) result.Copied++;
                    continue;
                }
                // A folder of the file, or one a text names without the file having it.
                long folder = clips.MakeFolder(entry.IsGroup ? entry.Name : entry.Group);
                if (entry.IsGroup) continue;
                long same = Named(clips, entry);
                if (entry.Name == null && !unnamed.Add(entry.Text.Replace("\r\n", "\n"))) continue;
                if (same != 0 && !overwrite) result.Kept++;
                else
                {
                    if (same != 0) clips.Change(same, entry.Name, entry.Text, entry.Html, null, folder);
                    else clips.AddSaved(entry.Name, entry.Text, entry.Html, null, folder, entry.Time);
                    result.Saved++;
                }
            }
            transaction.Commit();
        }
        clips.Prune();
        return result;
    }

    // The saved entry with the name of one of the file, or 0. One without a name clashes with none.
    static long Named(Clips clips, Entry entry)
    {
        return entry.Name == null ? 0 : clips.Named(entry.Name);
    }
}
