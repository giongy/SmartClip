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

// The saved texts, and on request what was copied, written as they are to a text file that Notepad shows,
// and read back from it. Each entry is a mark on a line of its own, a few fields, then its text: one line of
// the file per line of text, each after a tab, which is what tells it from the rest.
static class Backup
{
    const string GroupMark = "[Cartella]", SavedMark = "[Salvato]", CopiedMark = "[Copiato]";
    const string NameField = "Nome: ", GroupField = "Cartella: ", TimeField = "Data: ", TextField = "Testo:";
    const string PinnedField = "In alto: sì";
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
        List<string> lines = new List<string>();
        lines.Add(Program.AppName + " " + Program.Version + ": esportazione del " + DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
        lines.Add("Attenzione: qui i testi sono in chiaro, password comprese.");
        lines.Add("Il testo di ogni voce sta nelle righe che iniziano con una tabulazione: se modifichi il file, lasciala al suo posto.");
        foreach (Entry entry in entries)
        {
            if (entry.Picture || entry.Name == null && !all) continue;
            lines.Add("");
            lines.Add(entry.IsGroup ? GroupMark : entry.Name != null ? SavedMark : CopiedMark);
            if (entry.Name != null) lines.Add(NameField + entry.Name);
            if (entry.Group != null) lines.Add(GroupField + entry.Group);
            if (entry.Time != DateTime.MinValue) lines.Add(TimeField + entry.Time.ToString(TimeFormat, CultureInfo.InvariantCulture));
            if (entry.Pinned) lines.Add(PinnedField);
            if (entry.IsGroup) continue;
            lines.Add(TextField);
            foreach (string line in entry.Text.Replace("\r\n", "\n").Split('\n')) lines.Add(Indent + line);
        }
        return lines;
    }

    // The entries of an exported file, as it is or after the user changed it by hand: what makes no sense is
    // skipped. A folder comes as an entry of its own, like in the list.
    public static List<Entry> Parse(IEnumerable<string> lines)
    {
        List<Entry> entries = new List<Entry>();
        string mark = null, name = null, group = null;
        DateTime time = DateTime.MinValue;
        bool pinned = false;
        List<string> text = null;   // the lines of the text, once its field is met
        int blanks = 0;             // empty lines met inside a text: they count only if more text follows
        Action close = delegate
        {
            string content = text == null ? "" : string.Join("\r\n", text);
            if (mark == GroupMark && !string.IsNullOrEmpty(name))
            {
                Entry folder = new Entry(name, "", DateTime.MinValue);
                folder.IsGroup = true;
                entries.Add(folder);
            }
            else if (mark == SavedMark && !string.IsNullOrEmpty(name) && content.Trim().Length > 0)
            {
                Entry saved = new Entry(name, content, time);
                saved.Group = string.IsNullOrEmpty(group) ? null : group;
                entries.Add(saved);
            }
            else if (mark == CopiedMark && content.Trim().Length > 0)
            {
                Entry copied = new Entry(null, content, time == DateTime.MinValue ? DateTime.Now : time);
                copied.Pinned = pinned;
                entries.Add(copied);
            }
            mark = name = group = null;
            time = DateTime.MinValue;
            pinned = false;
            text = null;
            blanks = 0;
        };
        foreach (string line in lines)
        {
            if (text != null && line.StartsWith(Indent))
            {
                for (; blanks > 0; blanks--) text.Add("");
                text.Add(line.Substring(Indent.Length));
            }
            else if (text != null && line.Length == 0) blanks++;
            else if (line == GroupMark || line == SavedMark || line == CopiedMark)
            {
                close();
                mark = line;
            }
            else if (mark == null || text != null) continue;   // the heading of the file, or something after a text
            else if (line.StartsWith(NameField)) name = line.Substring(NameField.Length).Trim();
            else if (line.StartsWith(GroupField)) group = line.Substring(GroupField.Length).Trim();
            else if (line.StartsWith(TimeField))
                DateTime.TryParseExact(line.Substring(TimeField.Length).Trim(), TimeFormat, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out time);
            else if (line == PinnedField) pinned = true;
            else if (line == TextField) text = new List<string>();
        }
        close();
        return entries;
    }

    // Takes in an exported file. A saved text whose name is taken replaces the one there only if 'replace',
    // told how many there are, says so. 'all': the copied texts too, those the list does not hold already.
    public static BackupResult Import(string path, bool all, Func<int, bool> replace)
    {
        List<Entry> found = Parse(File.ReadAllLines(path, Encoding.UTF8)), copied = new List<Entry>();
        List<Entry> present = Texts.Load();
        Predicate<Entry> taken = delegate(Entry entry)
        {
            return entry.Name != null && !entry.IsGroup
                && present.Exists(delegate(Entry other) { return !other.IsGroup && Texts.SameName(other.Name, entry.Name); });
        };
        int clashes = found.FindAll(taken).Count;
        bool overwrite = clashes > 0 && replace(clashes);
        List<string> groups = Texts.Groups();
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
            if (group != null && !groups.Exists(delegate(string other) { return Texts.SameName(other, group); }))
            {
                Texts.SetGroup(group, true);
                groups.Add(group);
            }
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
