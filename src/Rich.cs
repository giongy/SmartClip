using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using ComTypes = System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

// A piece of formatted text that looks the same all through. What it does not set is left to where it is pasted:
// no font, no size, no colour of its own.
class Run
{
    public string Text = "";
    public string Font;       // null: the default
    public float Size;        // in points; 0: the default
    public FontStyle Style;   // bold, italic, underlined, struck out
    public Color Color;       // Color.Empty: the default

    public bool Plain
    {
        get { return Font == null && Size == 0 && Style == FontStyle.Regular && Color.IsEmpty; }
    }

    // Whether the two look the same, whatever their text.
    public bool Looks(Run other)
    {
        return string.Equals(Font, other.Font, StringComparison.OrdinalIgnoreCase) && Size == other.Size
            && Style == other.Style && Color.ToArgb() == other.Color.ToArgb();
    }

    // The same look, without the text.
    public Run Like()
    {
        Run copy = new Run();
        copy.Font = Font;
        copy.Size = Size;
        copy.Style = Style;
        copy.Color = Color;
        return copy;
    }
}

// The formatted saved texts (a mail signature): what the clipboard offers of them, as HTML (browsers, web mail)
// and RTF (Word, Outlook), and back. They are edited as a list of runs, which the edit box shows (RichBox) and
// from which the HTML is written again when they change; as long as they do not, what was copied is kept as it
// is, links, tables and pictures included. Only the formatting of characters counts: paragraphs, alignment and
// lists are not kept by the editing.
static class Rich
{
    const string StartMarker = "<!--StartFragment-->", EndMarker = "<!--EndFragment-->";
    const string HtmlHeader = "Version:0.9\r\nStartHTML:{0:D10}\r\nEndHTML:{1:D10}\r\nStartFragment:{2:D10}\r\nEndFragment:{3:D10}\r\n";
    public const int KeptCopies = 10; // the last copies whose formatting is kept in memory, to be saved with them
    const int DarkLimit = 64;         // a colour darker than this is the black text that is everyone's default
    const int LightGrey = 160, GreySpread = 24;   // a grey this light, its channels this close, is text for a dark page
    const int DarkBack = 96;          // a background darker than this belongs to a dark page
    const float BaseSize = 12;        // the size of text in a browser, in points, from which relative sizes start
    const float Step = 1.2f;          // from one size to the next larger, in CSS
    const string Monospace = "Courier New";
    static readonly Color LinkColor = Color.FromArgb(5, 99, 193);   // that of Word and Outlook
    // The fonts and sizes the edit box offers: those that every PC that receives a mail is likely to have.
    public static readonly string[] Fonts = Array.FindAll(new string[] { "Arial", "Aptos", "Calibri", "Cambria",
        "Courier New", "Georgia", "Segoe UI", "Tahoma", "Times New Roman", "Trebuchet MS", "Verdana" }, Look.Has);
    public static readonly float[] Sizes = { 8, 9, 10, 11, 12, 14, 16, 18, 20, 24 };
    // What of the formatting of an edit box is kept: what a selection has to have the same throughout to be one run.
    public const uint Formatting = Native.CFM_FACE | Native.CFM_SIZE | Native.CFM_COLOR | Native.CFM_BOLD | Native.CFM_ITALIC
        | Native.CFM_UNDERLINE | Native.CFM_STRIKEOUT;

    // --- The clipboard ---

    // The HTML the clipboard holds, the fragment that was copied without the page around it; or null. Every program
    // writes it in UTF-8, while Windows Forms would read it in the code page of Windows: an accented letter, or a
    // space that does not break, became two letters, and the text no longer matched the plain one (Matches), so the
    // formatting was dropped (found by the user on 18/10/2026, copying from VS Code). Read as the bytes it is.
    public static string Html(IDataObject data)
    {
        byte[] bytes = data.GetDataPresent(DataFormats.Html) ? Bytes(data, DataFormats.Html) : null;
        return bytes == null ? null : Lighten(Fragment(Encoding.UTF8.GetString(bytes)));
    }

    // What was copied from a page in a dark theme, made fit for any page: its dark backgrounds go, and the light grey
    // of its text, written for them, becomes the default. Copied from the chat of VS Code and pasted into Word, it
    // came out light grey on black boxes (found by the user on 18/10/2026). Colours that are colours stay, and so do
    // the darker greys of a signature. Only the declarations of the inline styles change: the rest is as it came.
    public static string Lighten(string html)
    {
        return StyleAttribute.Replace(html, delegate(Match style)
        {
            List<string> kept = new List<string>();
            foreach (string declaration in style.Groups[3].Value.Split(';'))
            {
                int colon = declaration.IndexOf(':');
                string property = colon < 0 ? "" : declaration.Substring(0, colon).Trim().ToLowerInvariant();
                string value = colon < 0 ? "" : declaration.Substring(colon + 1);
                bool dark = false;
                if (property == "background" || property == "background-color")
                    foreach (Match token in ColourToken.Matches(value))
                    {
                        Run look = new Run();
                        HtmlReader.Colour(token.Value, look);
                        dark |= !look.Color.IsEmpty && Light(look.Color) < DarkBack;
                    }
                Run text = new Run();
                if (property == "color") HtmlReader.Colour(value, text);
                if (!dark && !(property == "color" && !text.Color.IsEmpty && Default(text.Color))) kept.Add(declaration);
            }
            return style.Groups[1].Value + style.Groups[2].Value + string.Join(";", kept) + style.Groups[2].Value;
        });
    }

    static readonly Regex StyleAttribute = new Regex(@"(\bstyle\s*=\s*)([""'])(.*?)\2", RegexOptions.IgnoreCase | RegexOptions.Singleline);
    static readonly Regex ColourToken = new Regex(@"rgba?\([^)]*\)|#[0-9a-f]{3,8}\b|\b[a-z]+\b", RegexOptions.IgnoreCase);

    // How light a colour looks, from 0 to 255.
    static double Light(Color colour)
    {
        return colour.R * 0.299 + colour.G * 0.587 + colour.B * 0.114;
    }

    // A colour that is the plain text of some page rather than a choice: black or nearly, which everyone has, or a
    // grey light enough to have been written on a dark background. Kept as the default, it takes that of wherever it
    // is pasted.
    static bool Default(Color colour)
    {
        int spread = Math.Max(colour.R, Math.Max(colour.G, colour.B)) - Math.Min(colour.R, Math.Min(colour.G, colour.B));
        return Light(colour) < DarkLimit || spread <= GreySpread && Light(colour) >= LightGrey;
    }

    // What a format holds, as the bytes written there, up to the zero that ends it; null if it cannot be read. Put by
    // this program, it is a stream of those bytes; on the clipboard, they are asked of Windows.
    static byte[] Bytes(IDataObject data, string format)
    {
        MemoryStream stream = data.GetData(format) as MemoryStream;
        byte[] bytes = stream != null ? stream.ToArray() : Global(data as ComTypes.IDataObject, format);
        if (bytes == null) return null;
        int end = Array.IndexOf(bytes, (byte)0);
        if (end >= 0) Array.Resize(ref bytes, end);
        return bytes;
    }

    static byte[] Global(ComTypes.IDataObject data, string format)
    {
        if (data == null) return null;
        ComTypes.FORMATETC wanted = new ComTypes.FORMATETC();
        wanted.cfFormat = (short)DataFormats.GetFormat(format).Id;
        wanted.dwAspect = ComTypes.DVASPECT.DVASPECT_CONTENT;
        wanted.lindex = -1;
        wanted.tymed = ComTypes.TYMED.TYMED_HGLOBAL;
        ComTypes.STGMEDIUM medium;
        try
        {
            data.GetData(ref wanted, out medium);
        }
        catch (COMException)
        {
            return null;
        }
        try
        {
            if (medium.tymed != ComTypes.TYMED.TYMED_HGLOBAL) return null;
            IntPtr memory = Native.GlobalLock(medium.unionmember);
            if (memory == IntPtr.Zero) return null;
            try
            {
                byte[] bytes = new byte[(int)Native.GlobalSize(medium.unionmember)];
                Marshal.Copy(memory, bytes, 0, bytes.Length);
                return bytes;
            }
            finally
            {
                Native.GlobalUnlock(medium.unionmember);
            }
        }
        finally
        {
            Native.ReleaseStgMedium(ref medium);
        }
    }

    // The RTF the clipboard holds, or null.
    public static string Rtf(IDataObject data)
    {
        return data.GetDataPresent(DataFormats.Rtf) ? data.GetData(DataFormats.Rtf) as string : null;
    }

    // Puts a formatted text on the clipboard beside its plain text: each program takes what it prefers. The HTML as
    // its bytes in UTF-8, as its header counts them and every program reads them: given as a string, Windows Forms
    // would write it in the code page of Windows, and an accented letter would arrive as «�».
    public static void Put(DataObject data, string html, string rtf)
    {
        if (html != null) data.SetData(DataFormats.Html, new MemoryStream(Encoding.UTF8.GetBytes(Document(html) + "\0")));
        if (rtf != null) data.SetData(DataFormats.Rtf, rtf);
    }

    // The fragment of an HTML clipboard document: where its header says, counted in bytes of UTF-8, or else
    // between the markers; the whole of it if it has neither.
    public static string Fragment(string document)
    {
        int start = Offset(document, "StartFragment:"), end = Offset(document, "EndFragment:");
        byte[] bytes = Encoding.UTF8.GetBytes(document);
        if (start >= 0 && end > start && end <= bytes.Length) return Encoding.UTF8.GetString(bytes, start, end - start);
        int from = document.IndexOf(StartMarker, StringComparison.OrdinalIgnoreCase);
        int to = document.IndexOf(EndMarker, StringComparison.OrdinalIgnoreCase);
        if (from >= 0 && to > from) return document.Substring(from + StartMarker.Length, to - from - StartMarker.Length);
        return document;
    }

    static int Offset(string document, string field)
    {
        int at = document.IndexOf(field, StringComparison.Ordinal);
        if (at < 0) return -1;
        int end = at += field.Length;
        while (end < document.Length && char.IsDigit(document[end])) end++;
        int value;
        return int.TryParse(document.Substring(at, end - at), NumberStyles.None, CultureInfo.InvariantCulture, out value) ? value : -1;
    }

    // A fragment as an HTML clipboard document: a page around it, and the header that says where each is in bytes.
    public static string Document(string fragment)
    {
        string before = "<html><body>\r\n" + StartMarker, after = EndMarker + "\r\n</body></html>";
        int header = string.Format(CultureInfo.InvariantCulture, HtmlHeader, 0, 0, 0, 0).Length;   // ASCII: a byte a letter
        int start = header + Encoding.UTF8.GetByteCount(before), end = start + Encoding.UTF8.GetByteCount(fragment);
        return string.Format(CultureInfo.InvariantCulture, HtmlHeader, header, end + Encoding.UTF8.GetByteCount(after), start, end)
            + before + fragment + after;
    }

    // --- The last copies ---

    // The copied entries are kept as plain text: the formatting of the last few copies stays in memory only, so
    // that saving one of them saves it formatted.
    static readonly List<string[]> recent = new List<string[]>();   // text, HTML, RTF; the newest first

    public static void Remember(string text, IDataObject data)
    {
        recent.RemoveAll(delegate(string[] copy) { return copy[0] == text; });
        string html = Html(data), rtf = Rtf(data);
        if (html == null && rtf == null) return;
        recent.Insert(0, new string[] { text, html, rtf });
        if (recent.Count > KeptCopies) recent.RemoveAt(recent.Count - 1);
    }

    // The formatting a copied text had, if it is one of the last copies.
    public static bool Recent(string text, out string html, out string rtf)
    {
        string[] copy = recent.Find(delegate(string[] each) { return each[0] == text; });
        html = copy == null ? null : copy[1];
        rtf = copy == null ? null : copy[2];
        return copy != null;
    }

    // The formatting a copied text had, as it came, if it is one of the last copies and says what the text says (see
    // Matches): what saving it without its window keeps. Both null for none; the RTF only beside the HTML.
    public static bool Kept(string text, out string html, out string rtf)
    {
        if (Recent(text, out html, out rtf))
        {
            List<Run> runs = rtf != null ? FromRtf(rtf) : null;
            if (runs == null && html != null) runs = FromHtml(html);
            if (runs != null && !Plain(runs) && TextOf(runs).Length > 0 && Matches(runs, text))
            {
                if (html == null) html = ToHtml(runs);
                return true;
            }
        }
        html = rtf = null;
        return false;
    }

    // --- Runs ---

    public static bool Plain(List<Run> runs)
    {
        return runs.TrueForAll(delegate(Run run) { return run.Plain; });
    }

    public static string TextOf(List<Run> runs)
    {
        StringBuilder text = new StringBuilder();
        foreach (Run run in runs) text.Append(run.Text);
        return text.ToString();
    }

    // Whether the runs say what the plain text copied with them says: the formatted version of the same thing, or
    // something else. A browser copying from its address bar gives the address as plain text and, as HTML, a link
    // with the title of the page. Only letters and digits count: spaces, line ends and the bullets of a list differ.
    public static bool Matches(List<Run> runs, string text)
    {
        return Letters(TextOf(runs)) == Letters(text);
    }

    static string Letters(string text)
    {
        StringBuilder letters = new StringBuilder();
        foreach (char c in text) if (char.IsLetterOrDigit(c)) letters.Append(c);
        return letters.ToString();
    }

    // Runs as they are kept: neighbours that look the same joined, no empty ones, nothing blank at the two ends,
    // spaces that must not break as plain spaces (they are written back where needed), black and light grey as the
    // default (Default).
    static List<Run> Tidy(List<Run> runs)
    {
        List<Run> tidy = new List<Run>();
        foreach (Run run in runs)
        {
            run.Text = run.Text.Replace("\r", "").Replace("\uFFFC", "").Replace('\u00A0', ' ');
            if (!run.Color.IsEmpty && Default(run.Color)) run.Color = Color.Empty;
            if (run.Text.Length == 0) continue;
            Run last = tidy.Count > 0 ? tidy[tidy.Count - 1] : null;
            if (last != null && (last.Looks(run) || run.Text.Trim('\n').Length == 0)) last.Text += run.Text;
            else tidy.Add(run);
        }
        while (tidy.Count > 0 && (tidy[0].Text = tidy[0].Text.TrimStart(' ', '\n', '\t')).Length == 0) tidy.RemoveAt(0);
        while (tidy.Count > 0 && (tidy[tidy.Count - 1].Text = tidy[tidy.Count - 1].Text.TrimEnd(' ', '\n', '\t')).Length == 0)
            tidy.RemoveAt(tidy.Count - 1);
        return tidy;
    }

    // The runs of an HTML fragment, as a browser would show its text.
    public static List<Run> FromHtml(string html)
    {
        HtmlReader reader = new HtmlReader();
        reader.Read(html);
        return Tidy(reader.Runs);
    }

    // The runs of RTF, read by a rich edit box; null if it is not RTF.
    public static List<Run> FromRtf(string rtf)
    {
        using (RichTextBox box = Hidden())
        {
            try
            {
                box.Rtf = rtf;
            }
            catch (ArgumentException)
            {
                return null;
            }
            return Walk(box, null, Color.Empty);
        }
    }

    // The runs of an edit box: what is written in its own font and colour has the default ones.
    public static List<Run> FromBox(RichTextBox box, Font font, Color colour)
    {
        // A copy is read, so that the selection of the box stays where it is.
        using (RichTextBox copy = Hidden())
        {
            copy.Rtf = box.Rtf;
            return Walk(copy, font, colour);
        }
    }

    static RichTextBox Hidden()
    {
        RichTextBox box = new RichTextBox();
        GC.KeepAlive(box.Handle);   // a box never shown has no window yet: it needs one to be read
        return box;
    }

    // The size text in a font has once a rich edit box wrote it as RTF, as FromBox reads it: not always that of
    // the font, which the box rounds to whole pixels of the screen and back.
    static float SizeIn(Font font)
    {
        using (RichTextBox box = Hidden())
        using (RichTextBox copy = Hidden())
        {
            box.Font = font;
            box.Text = "x";
            copy.Rtf = box.Rtf;
            copy.Select(0, 1);
            return Half(FormatOf(copy).yHeight / 20f);
        }
    }

    // The formatting of the selection as the box keeps it: also the name of a font this PC does not have, which
    // SelectionFont would give as the font Windows puts in its place.
    static Native.CHARFORMAT2 FormatOf(RichTextBox box)
    {
        Native.CHARFORMAT2 format = NewFormat();
        Native.SendMessage(box.Handle, Native.EM_GETCHARFORMAT, (IntPtr)Native.SCF_SELECTION, ref format);
        return format;
    }

    public static Native.CHARFORMAT2 NewFormat()
    {
        Native.CHARFORMAT2 format = new Native.CHARFORMAT2();
        format.cbSize = Marshal.SizeOf(typeof(Native.CHARFORMAT2));
        format.szFaceName = "";
        return format;
    }

    // A piece at a time: a rich edit box tells the formatting only of what is selected, and of a longer selection
    // only what is the same all through it. Each piece goes as far as the formatting of its first character, found
    // by doubling and then halving: a long text pasted is read in a few steps, not one per character.
    static List<Run> Walk(RichTextBox box, Font font, Color colour)
    {
        float ownSize = font == null ? 0 : SizeIn(font);
        List<Run> runs = new List<Run>();
        string text = box.Text;
        Run last = null;
        StringBuilder written = new StringBuilder();
        for (int i = 0, length; i < text.Length; i += length)
        {
            length = 1;
            if (text[i] == '\n' && last != null)
            {
                written.Append('\n');   // a line end takes the look of what it ends
                continue;
            }
            box.Select(i, 1);
            Native.CHARFORMAT2 shown = FormatOf(box);
            length = Stretch(box, i, text.Length - i);
            Run look = new Run();
            float size = Half(shown.yHeight / 20f);   // in twentieths of a point
            bool own = font != null && string.Equals(shown.szFaceName, font.Name, StringComparison.OrdinalIgnoreCase);
            look.Font = own || string.IsNullOrEmpty(shown.szFaceName) ? null : shown.szFaceName;
            look.Size = size == ownSize ? 0 : size;
            look.Style = (FontStyle)(shown.dwEffects & (Native.CFE_BOLD | Native.CFE_ITALIC | Native.CFE_UNDERLINE | Native.CFE_STRIKEOUT));
            // Without a colour of its own, text has that of Windows: the default too.
            Color seen = ColorTranslator.FromWin32(shown.crTextColor);
            bool automatic = (shown.dwEffects & Native.CFE_AUTOCOLOR) != 0;
            look.Color = automatic || seen.ToArgb() == colour.ToArgb() ? Color.Empty : Color.FromArgb(seen.R, seen.G, seen.B);
            if (last == null || !last.Looks(look))
            {
                if (last != null) last.Text = written.ToString();
                written.Length = 0;
                runs.Add(last = look);
            }
            written.Append(text, i, length);
        }
        if (last != null) last.Text = written.ToString();
        return Tidy(runs);
    }

    // How many characters from 'start', at most 'most', are formatted as the first: what a selection that long
    // keeps the same throughout. Any shorter one does too, which is what lets the length be halved towards it.
    static int Stretch(RichTextBox box, int start, int most)
    {
        int alike = 1, unlike = most + 1;
        for (int step = 1; alike < most; step *= 2)
        {
            int tried = Math.Min(most, alike + step);
            if (!Uniform(box, start, tried))
            {
                unlike = tried;
                break;
            }
            alike = tried;
        }
        while (unlike - alike > 1)
        {
            int tried = (alike + unlike) / 2;
            if (Uniform(box, start, tried)) alike = tried;
            else unlike = tried;
        }
        return alike;
    }

    static bool Uniform(RichTextBox box, int start, int length)
    {
        box.Select(start, length);
        return (FormatOf(box).dwMask & Formatting) == Formatting;
    }

    static float Half(float points)
    {
        return (float)Math.Round(points * 2) / 2;
    }

    // HTML that shows the runs: a span for each that has a look of its own.
    public static string ToHtml(List<Run> runs)
    {
        StringBuilder html = new StringBuilder();
        char last = '\n';
        foreach (Run run in runs)
        {
            string css = Css(run);
            if (css.Length > 0) html.Append("<span style=\"").Append(css).Append("\">");
            foreach (char c in run.Text)
            {
                switch (c)
                {
                    case '&': html.Append("&amp;"); break;
                    case '<': html.Append("&lt;"); break;
                    case '>': html.Append("&gt;"); break;
                    case '"': html.Append("&quot;"); break;
                    case '\n': html.Append("<br>\r\n"); break;
                    case '\t': html.Append("&nbsp;&nbsp;&nbsp;&nbsp;"); break;
                    // HTML makes one space of many, and none at the start of a line.
                    case ' ': html.Append(last == ' ' || last == '\n' ? "&nbsp;" : " "); break;
                    default: html.Append(c); break;
                }
                last = c;
            }
            if (css.Length > 0) html.Append("</span>");
        }
        return html.ToString();
    }

    static string Css(Run run)
    {
        List<string> parts = new List<string>();
        if (run.Font != null) parts.Add("font-family:'" + run.Font.Replace("'", "").Replace("\"", "") + "'");
        if (run.Size > 0) parts.Add("font-size:" + run.Size.ToString(CultureInfo.InvariantCulture) + "pt");
        if (!run.Color.IsEmpty) parts.Add(string.Format("color:#{0:x2}{1:x2}{2:x2}", run.Color.R, run.Color.G, run.Color.B));
        if ((run.Style & FontStyle.Bold) != 0) parts.Add("font-weight:bold");
        if ((run.Style & FontStyle.Italic) != 0) parts.Add("font-style:italic");
        string lines = ((run.Style & FontStyle.Underline) != 0 ? " underline" : "") + ((run.Style & FontStyle.Strikeout) != 0 ? " line-through" : "");
        if (lines.Length > 0) parts.Add("text-decoration:" + lines.Substring(1));
        return string.Join(";", parts.ToArray());
    }

    // RTF that shows the runs in an edit box: what has no font, size or colour of its own in those given.
    public static string ToRtf(List<Run> runs, Font font, Color colour)
    {
        List<string> fonts = new List<string> { font.Name };
        List<int> colours = new List<int> { colour.ToArgb() };
        foreach (Run run in runs)
        {
            if (run.Font != null && fonts.FindIndex(delegate(string f) { return string.Equals(f, run.Font, StringComparison.OrdinalIgnoreCase); }) < 0)
                fonts.Add(run.Font);
            if (!run.Color.IsEmpty && !colours.Contains(run.Color.ToArgb())) colours.Add(run.Color.ToArgb());
        }
        StringBuilder rtf = new StringBuilder(@"{\rtf1\ansi\deff0\uc1{\fonttbl");
        for (int i = 0; i < fonts.Count; i++) rtf.Append(@"{\f").Append(i).Append(@"\fnil\fcharset0 ").Append(fonts[i].Replace(";", "")).Append(";}");
        rtf.Append(@"}{\colortbl ;");
        foreach (int each in colours)
        {
            Color c = Color.FromArgb(each);
            rtf.AppendFormat(@"\red{0}\green{1}\blue{2};", c.R, c.G, c.B);
        }
        rtf.Append("}\r\n");
        float own = SizeIn(font);
        foreach (Run run in runs)
        {
            int face = run.Font == null ? 0 : fonts.FindIndex(delegate(string f) { return string.Equals(f, run.Font, StringComparison.OrdinalIgnoreCase); });
            rtf.Append(@"\plain\f").Append(face).Append(@"\fs").Append((int)Math.Round((run.Size > 0 ? run.Size : own) * 2))
                .Append(@"\cf").Append(colours.IndexOf(run.Color.IsEmpty ? colour.ToArgb() : run.Color.ToArgb()) + 1);
            if ((run.Style & FontStyle.Bold) != 0) rtf.Append(@"\b");
            if ((run.Style & FontStyle.Italic) != 0) rtf.Append(@"\i");
            if ((run.Style & FontStyle.Underline) != 0) rtf.Append(@"\ul");
            if ((run.Style & FontStyle.Strikeout) != 0) rtf.Append(@"\strike");
            rtf.Append(' ');
            foreach (char c in run.Text)
            {
                if (c == '\\' || c == '{' || c == '}') rtf.Append('\\').Append(c);
                else if (c == '\n') rtf.Append("\\par\r\n");
                else if (c == '\t') rtf.Append(@"\tab ");
                else if (c >= ' ' && c < 128) rtf.Append(c);
                else if (c >= ' ') rtf.Append(@"\u").Append((short)c).Append('?');
            }
        }
        return rtf.Append('}').ToString();
    }

    // Reads HTML as a browser shows it, without the layout: the text, with the look each piece of it inherits from
    // the elements around it and their inline styles. Style sheets are not read: the text copied from a web page
    // carries its look inline.
    sealed class HtmlReader
    {
        class Frame
        {
            public string Tag;
            public Run Look;
            public bool Hidden, Pre;
        }

        static readonly string[] Blocks = { "address", "article", "aside", "blockquote", "center", "dd", "div", "dl", "dt",
            "fieldset", "figure", "footer", "form", "h1", "h2", "h3", "h4", "h5", "h6", "header", "hr", "li", "main", "nav", "ol",
            "p", "pre", "section", "table", "tr", "ul" };
        static readonly string[] Empty = { "area", "base", "br", "col", "embed", "hr", "img", "input", "link", "meta", "param",
            "source", "track", "wbr" };
        static readonly string[] Unseen = { "script", "style", "title", "template", "noscript", "xml" };   // skipped, content and all
        static readonly float[] HeadingShares = { 2, 1.5f, 1.17f, 1, 0.83f, 0.67f };
        static readonly float[] FontSizes = { 7.5f, 10, 12, 13.5f, 18, 24, 36 };   // of <font size="1"> to "7"

        readonly List<Frame> open = new List<Frame>();
        public readonly List<Run> Runs = new List<Run>();
        readonly StringBuilder written = new StringBuilder();   // the text of the last run
        bool space;              // white space met, written only if more text follows on the same line
        Run spaceLook;           // the look of where that space was: not underlined because what follows is
        bool lineStart = true;   // nothing written yet on this line

        public HtmlReader()
        {
            Frame root = new Frame();
            root.Look = new Run();
            open.Add(root);
        }

        Frame Top
        {
            get { return open[open.Count - 1]; }
        }

        public void Read(string html)
        {
            int at = 0;
            while (at < html.Length)
            {
                int tag = html.IndexOf('<', at);
                if (tag < 0) tag = html.Length;
                if (tag > at) Text(html.Substring(at, tag - at));
                at = tag < html.Length ? Tag(html, tag) : tag;
            }
            Flush();
        }

        void Text(string raw)
        {
            Frame top = Top;
            if (top.Hidden) return;
            string text = WebUtility.HtmlDecode(raw);
            if (top.Pre)
            {
                foreach (char c in text.Replace("\r\n", "\n").Replace('\r', '\n')) Write(c, top.Look);
                return;
            }
            foreach (char c in text)
            {
                if (c == ' ' || c == '\t' || c == '\n' || c == '\r' || c == '\f')
                {
                    if (!lineStart && !space)
                    {
                        space = true;
                        spaceLook = top.Look;
                    }
                    continue;
                }
                if (space) Write(' ', spaceLook);
                Write(c, top.Look);
            }
        }

        void Write(char c, Run look)
        {
            space = false;
            Run last = Runs.Count > 0 ? Runs[Runs.Count - 1] : null;
            if (last == null || c != '\n' && !last.Looks(look))
            {
                Flush();
                Runs.Add(look.Like());
            }
            written.Append(c);
            lineStart = c == '\n';
        }

        void Flush()
        {
            if (Runs.Count > 0) Runs[Runs.Count - 1].Text = written.ToString();
            written.Length = 0;
        }

        // A new line, unless one has just begun: between blocks.
        void Break()
        {
            if (!lineStart) Write('\n', Top.Look);
            space = false;
        }

        // Reads the tag at 'at' and returns where what follows it starts.
        int Tag(string html, int at)
        {
            if (string.CompareOrdinal(html, at, "<!--", 0, 4) == 0)
            {
                int end = html.IndexOf("-->", at + 4, StringComparison.Ordinal);
                return end < 0 ? html.Length : end + 3;
            }
            int i = at + 1;
            bool closing = i < html.Length && html[i] == '/';
            if (closing) i++;
            if (i < html.Length && (html[i] == '!' || html[i] == '?'))
            {
                int end = html.IndexOf('>', i);
                return end < 0 ? html.Length : end + 1;
            }
            if (i >= html.Length || !char.IsLetter(html[i]))
            {
                Text("&lt;");   // not a tag: the sign itself
                return at + 1;
            }
            int start = i;
            while (i < html.Length && (char.IsLetterOrDigit(html[i]) || html[i] == ':' || html[i] == '-')) i++;
            string name = html.Substring(start, i - start).ToLowerInvariant();
            Dictionary<string, string> attributes = new Dictionary<string, string>();
            bool alone = false;   // <tag />
            while (i < html.Length && html[i] != '>')
            {
                char c = html[i];
                if (char.IsWhiteSpace(c) || c == '/')
                {
                    alone = c == '/';
                    i++;
                    continue;
                }
                alone = false;
                int key = i;
                while (i < html.Length && !char.IsWhiteSpace(html[i]) && html[i] != '=' && html[i] != '>' && html[i] != '/') i++;
                string attribute = html.Substring(key, i - key).ToLowerInvariant(), value = "";
                while (i < html.Length && char.IsWhiteSpace(html[i])) i++;
                if (i < html.Length && html[i] == '=')
                {
                    for (i++; i < html.Length && char.IsWhiteSpace(html[i]); i++) { }
                    if (i < html.Length && (html[i] == '"' || html[i] == '\''))
                    {
                        int end = html.IndexOf(html[i], i + 1);
                        if (end < 0) end = html.Length;
                        value = html.Substring(i + 1, end - i - 1);
                        i = Math.Min(end + 1, html.Length);
                    }
                    else
                    {
                        int from = i;
                        while (i < html.Length && !char.IsWhiteSpace(html[i]) && html[i] != '>') i++;
                        value = html.Substring(from, i - from);
                    }
                }
                if (attribute.Length > 0 && !attributes.ContainsKey(attribute)) attributes[attribute] = WebUtility.HtmlDecode(value);
            }
            int after = Math.Min(i + 1, html.Length);
            if (closing) Close(name);
            else if (Array.IndexOf(Unseen, name) >= 0)
            {
                int end = html.IndexOf("</" + name, after, StringComparison.OrdinalIgnoreCase);
                if (end < 0) return html.Length;
                end = html.IndexOf('>', end);
                return end < 0 ? html.Length : end + 1;
            }
            else Open(name, attributes, alone);
            return after;
        }

        void Open(string name, Dictionary<string, string> attributes, bool alone)
        {
            Frame parent = Top, frame = new Frame();
            frame.Tag = name;
            frame.Look = parent.Look.Like();
            frame.Hidden = parent.Hidden || attributes.ContainsKey("hidden");
            frame.Pre = parent.Pre;
            Shape(frame, attributes);
            if (!frame.Hidden)
            {
                if (name == "br") Write('\n', parent.Look);
                else if (Array.IndexOf(Blocks, name) >= 0) Break();
                else if ((name == "td" || name == "th") && !lineStart && !space)   // cells side by side
                {
                    space = true;
                    spaceLook = parent.Look;
                }
            }
            if (Array.IndexOf(Empty, name) >= 0 || alone) return;
            open.Add(frame);
            if (name == "li" && !frame.Hidden) Text("&bull; ");
        }

        void Close(string name)
        {
            for (int i = open.Count - 1; i > 0; i--)
                if (open[i].Tag == name)
                {
                    bool shown = !open[i].Hidden;
                    open.RemoveRange(i, open.Count - i);
                    if (shown && Array.IndexOf(Blocks, name) >= 0) Break();
                    return;
                }
        }

        // The look an element gives to what it holds: by what it is, then by its attributes and its inline style.
        void Shape(Frame frame, Dictionary<string, string> attributes)
        {
            Run look = frame.Look;
            string value;
            switch (frame.Tag)
            {
                case "b": case "strong": look.Style |= FontStyle.Bold; break;
                case "i": case "em": case "cite": case "var": case "dfn": look.Style |= FontStyle.Italic; break;
                case "u": case "ins": look.Style |= FontStyle.Underline; break;
                case "s": case "strike": case "del": look.Style |= FontStyle.Strikeout; break;
                case "code": case "kbd": case "samp": case "tt": look.Font = Monospace; break;
                case "pre": look.Font = Monospace; frame.Pre = true; break;
                case "small": look.Size = Half(Inherited(look) / Step); break;
                case "big": look.Size = Half(Inherited(look) * Step); break;
                case "a":
                    if (!attributes.ContainsKey("href")) break;
                    look.Style |= FontStyle.Underline;
                    look.Color = LinkColor;
                    break;
                case "font":
                    if (attributes.TryGetValue("face", out value) && Family(value) != null) look.Font = Family(value);
                    if (attributes.TryGetValue("size", out value)) look.Size = FontSize(value.Trim());
                    if (attributes.TryGetValue("color", out value)) Colour(value, look);
                    break;
            }
            if (frame.Tag.Length == 2 && frame.Tag[0] == 'h' && frame.Tag[1] >= '1' && frame.Tag[1] <= '6')
            {
                look.Style |= FontStyle.Bold;
                look.Size = Half(Inherited(look) * HeadingShares[frame.Tag[1] - '1']);
            }
            if (attributes.TryGetValue("style", out value)) Declarations(value, frame);
        }

        static float Inherited(Run look)
        {
            return look.Size > 0 ? look.Size : BaseSize;
        }

        // <font size>: 1 to 7, or more or less than the 3 of normal text.
        static float FontSize(string value)
        {
            int size;
            if (!int.TryParse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out size)) return 0;
            if (value.Length > 0 && (value[0] == '+' || value[0] == '-')) size += 3;
            return FontSizes[Math.Max(1, Math.Min(FontSizes.Length, size)) - 1];
        }

        void Declarations(string css, Frame frame)
        {
            Run look = frame.Look;
            foreach (string declaration in css.Split(';'))
            {
                int colon = declaration.IndexOf(':');
                if (colon < 0) continue;
                string property = declaration.Substring(0, colon).Trim().ToLowerInvariant();
                string value = declaration.Substring(colon + 1).Trim();
                int important = value.IndexOf("!important", StringComparison.OrdinalIgnoreCase);
                if (important >= 0) value = value.Substring(0, important).Trim();
                string lower = value.ToLowerInvariant();
                switch (property)
                {
                    case "font-family":
                        if (Family(value) != null) look.Font = Family(value);
                        break;
                    case "font-size":
                        float size = CssSize(lower, look);
                        if (size > 0) look.Size = size;
                        break;
                    case "font-weight":
                        int weight;
                        bool bold = lower == "bold" || lower == "bolder" || int.TryParse(lower, out weight) && weight >= 600;
                        look.Style = bold ? look.Style | FontStyle.Bold : look.Style & ~FontStyle.Bold;
                        break;
                    case "font-style":
                        bool italic = Begins(lower, "italic") || Begins(lower, "oblique");
                        look.Style = italic ? look.Style | FontStyle.Italic : look.Style & ~FontStyle.Italic;
                        break;
                    case "text-decoration":
                    case "text-decoration-line":
                        if (lower.Contains("underline")) look.Style |= FontStyle.Underline;
                        if (lower.Contains("line-through")) look.Style |= FontStyle.Strikeout;
                        if (Begins(lower, "none")) look.Style &= ~(FontStyle.Underline | FontStyle.Strikeout);
                        break;
                    case "color":
                        Colour(lower, look);
                        break;
                    case "display":
                        if (lower == "none") frame.Hidden = true;
                        break;
                    case "visibility":
                        if (lower == "hidden" || lower == "collapse") frame.Hidden = true;
                        break;
                    case "mso-hide":
                        if (lower == "all") frame.Hidden = true;
                        break;
                    case "white-space":
                        frame.Pre = lower == "pre" || lower == "pre-wrap" || lower == "break-spaces";
                        break;
                }
            }
        }

        // The first family of a list, as a font Windows knows: the generic ones as the fonts browsers take for them.
        static string Family(string value)
        {
            string first = value.Split(',')[0].Trim().Trim('"', '\'').Trim();
            switch (first.ToLowerInvariant())
            {
                case "": case "inherit": case "initial": case "unset": return null;
                case "serif": return "Times New Roman";
                case "sans-serif": return "Arial";
                case "monospace": return Monospace;
                case "system-ui": case "-apple-system": return "Segoe UI";
                default: return first;
            }
        }

        // A CSS size in points, or 0 if it is not understood.
        static float CssSize(string value, Run look)
        {
            float inherited = Inherited(look);
            switch (value)
            {
                case "xx-small": return 7;
                case "x-small": return 7.5f;
                case "small": return 10;
                case "medium": return 12;
                case "large": return 13.5f;
                case "x-large": return 18;
                case "xx-large": return 24;
                case "xxx-large": return 36;
                case "smaller": return Half(inherited / Step);
                case "larger": return Half(inherited * Step);
            }
            int end = 0;
            while (end < value.Length && (char.IsDigit(value[end]) || value[end] == '.')) end++;
            float number;
            if (!float.TryParse(value.Substring(0, end), NumberStyles.Float, CultureInfo.InvariantCulture, out number) || number <= 0) return 0;
            switch (value.Substring(end).Trim())
            {
                case "pt": break;
                case "": case "px": number *= 0.75f; break;
                case "em": number *= inherited; break;
                case "rem": number *= BaseSize; break;
                case "%": number *= inherited / 100; break;
                case "pc": number *= 12; break;
                case "in": number *= 72; break;
                case "cm": number *= 72 / 2.54f; break;
                case "mm": number *= 72 / 25.4f; break;
                default: return 0;
            }
            return Half(number);
        }

        // A CSS colour: #rgb, #rrggbb, rgb(…) or a name. What is not understood leaves the colour as it was.
        public static void Colour(string value, Run look)
        {
            value = value.Trim().ToLowerInvariant();
            int r, g, b;
            if (Begins(value, "#"))
            {
                string hex = value.Substring(1);
                if (hex.Length == 3 || hex.Length == 4) hex = new string(new char[] { hex[0], hex[0], hex[1], hex[1], hex[2], hex[2] });
                int rgb;
                if (hex.Length < 6 || !int.TryParse(hex.Substring(0, 6), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out rgb)) return;
                look.Color = Color.FromArgb(rgb >> 16, (rgb >> 8) & 0xFF, rgb & 0xFF);
            }
            else if (Begins(value, "rgb"))
            {
                int open = value.IndexOf('('), close = value.IndexOf(')');
                if (open < 0 || close < open) return;
                string[] parts = value.Substring(open + 1, close - open - 1).Split(new char[] { ',', ' ', '/' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 3 || !Channel(parts[0], out r) || !Channel(parts[1], out g) || !Channel(parts[2], out b)) return;
                look.Color = Color.FromArgb(r, g, b);
            }
            else if (value.Length > 0 && char.IsLetter(value[0]))
            {
                Color named = Color.FromName(value.Replace("grey", "gray"));
                if (named.IsKnownColor && named.A == 255) look.Color = Color.FromArgb(named.R, named.G, named.B);
            }
        }

        // CSS is compared as written, letter by letter: not as the language of Windows would.
        static bool Begins(string value, string start)
        {
            return value.StartsWith(start, StringComparison.Ordinal);
        }

        static bool Channel(string part, out int value)
        {
            float number;
            bool percent = part.EndsWith("%", StringComparison.Ordinal);
            value = 0;
            if (!float.TryParse(percent ? part.TrimEnd('%') : part, NumberStyles.Float, CultureInfo.InvariantCulture, out number)) return false;
            value = Math.Max(0, Math.Min(255, (int)Math.Round(percent ? number * 2.55f : number)));
            return true;
        }
    }
}

// The box where a saved text is written, with the formatting of its characters: bold, italic, underlined, font
// and size. What is pasted keeps the formatting it comes with, from RTF or HTML; plain text takes that of where it
// goes. The keys a rich edit box has for paragraphs (alignment, spacing, lists) do nothing: the formatting of
// paragraphs is not kept.
class RichBox : RichTextBox
{
    // Formatting given or pasted: the text may be formatted from now on.
    public event EventHandler Formatted;

    public RichBox()
    {
        DetectUrls = false;   // a link would be underlined and coloured by itself
        ScrollBars = RichTextBoxScrollBars.Vertical;
        HideSelection = false;   // what the formatting buttons will change stays in sight while they are clicked
    }

    // The colour of text without one of its own: the box's colour of text, read as the theme was when it opened.
    public Color Ink { get; set; }

    // Enter breaks the line, instead of pressing the button of the window that saves.
    protected override bool IsInputKey(Keys keys)
    {
        return (keys & Keys.KeyCode) == Keys.Return && (keys & Keys.Alt) == 0 || base.IsInputKey(keys);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keys)
    {
        switch (keys)
        {
            case Keys.Control | Keys.V:
            case Keys.Shift | Keys.Insert:
                PasteKeeping();
                return true;
            case Keys.Control | Keys.B: Toggle(FontStyle.Bold); return true;
            case Keys.Control | Keys.I: Toggle(FontStyle.Italic); return true;
            case Keys.Control | Keys.U: Toggle(FontStyle.Underline); return true;
            case Keys.Control | Keys.E: case Keys.Control | Keys.J: case Keys.Control | Keys.L: case Keys.Control | Keys.R:
            case Keys.Control | Keys.D1: case Keys.Control | Keys.D2: case Keys.Control | Keys.D5:
            case Keys.Control | Keys.Shift | Keys.L: case Keys.Control | Keys.Oemplus: case Keys.Control | Keys.Shift | Keys.Oemplus:
                return true;
        }
        return base.ProcessCmdKey(ref msg, keys);
    }

    void PasteKeeping()
    {
        List<Run> runs;
        string text;
        try
        {
            IDataObject data = Clipboard.GetDataObject();
            if (data == null) return;
            string rtf = Rich.Rtf(data), html = rtf == null ? Rich.Html(data) : null;
            runs = rtf != null ? Rich.FromRtf(rtf) : html != null ? Rich.FromHtml(html) : null;
            text = data.GetData(DataFormats.UnicodeText) as string;
        }
        catch (ExternalException)
        {
            return;   // the clipboard held by another program: nothing is pasted, as in any box
        }
        if (runs != null && !Rich.Plain(runs) && Rich.TextOf(runs).Length > 0 && (text == null || Rich.Matches(runs, text)))
        {
            SelectedRtf = Rich.ToRtf(runs, Font, Ink);
            Changed();
        }
        else if (text != null) SelectedText = text.Replace("\r\n", "\n");
    }

    // Bold, italic or underlined, or no longer: the selection all of it, or what is written from here.
    public void Toggle(FontStyle style)
    {
        uint mask = (uint)style;   // the bits of FontStyle are those of the effects of the box
        Native.CHARFORMAT2 now = Rich.NewFormat();
        SendMessage(Native.EM_GETCHARFORMAT, ref now);
        Native.CHARFORMAT2 change = Rich.NewFormat();
        change.dwMask = mask;
        change.dwEffects = (now.dwMask & mask) != 0 && (now.dwEffects & mask) != 0 ? 0 : mask;
        Apply(change);
    }

    // The font of the selection; null: that of the box, which is the default.
    public void Face(string name)
    {
        Native.CHARFORMAT2 change = name == null ? Own() : Rich.NewFormat();
        change.dwMask = Native.CFM_FACE;
        if (name != null) change.szFaceName = name;
        Apply(change);
    }

    // The size of the selection in points; 0: that of the box, which is the default.
    public void Points(float points)
    {
        Native.CHARFORMAT2 change = points > 0 ? Rich.NewFormat() : Own();
        change.dwMask = Native.CFM_SIZE;
        if (points > 0) change.yHeight = (int)Math.Round(points * 20);   // in twentieths of a point
        Apply(change);
    }

    // Takes away the formatting of the selection, or with nothing selected of all the text: what is left is as
    // what is written into a box without formatting.
    public void Plain()
    {
        int start = SelectionStart, length = SelectionLength;
        if (length == 0) SelectAll();
        Native.CHARFORMAT2 change = Own();
        change.dwMask = Rich.Formatting;
        change.dwEffects = 0;
        change.crTextColor = ColorTranslator.ToWin32(Ink);
        Apply(change);
        Select(start, length);
    }

    // The formatting of the box itself, which text without formatting has.
    Native.CHARFORMAT2 Own()
    {
        Native.CHARFORMAT2 format = Rich.NewFormat();
        Native.SendMessage(Handle, Native.EM_GETCHARFORMAT, (IntPtr)Native.SCF_DEFAULT, ref format);
        return format;
    }

    void Apply(Native.CHARFORMAT2 change)
    {
        SendMessage(Native.EM_SETCHARFORMAT, ref change);
        Changed();
    }

    void SendMessage(int message, ref Native.CHARFORMAT2 format)
    {
        Native.SendMessage(Handle, message, (IntPtr)Native.SCF_SELECTION, ref format);
    }

    void Changed()
    {
        if (Formatted != null) Formatted(this, EventArgs.Empty);
    }
}
