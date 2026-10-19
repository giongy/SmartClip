using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

// The window where a saved text gets its name, its content and its folder; a saved picture its name and its
// folder, with the picture in place of the content. The same window, without the content, asks what a folder is
// called and which folder it is in.
//
// The content can be formatted (Rich): buttons above it make the selection bold, italic, underlined, give it a
// font and a size, or take its formatting away; what is pasted keeps its own. A text that came formatted (a
// signature copied from a mail) is saved as it came unless it is changed here; changed, its HTML is written anew.
class EditForm : Window
{
    const int Edge = 12, FieldWidth = 420, ContentHeight = 190, Gap = 6;   // in pixels at 96 dpi
    const int PictureHeight = 150;                 // the picture shown in place of the content
    const int LabelRow = 20, NameHeight = 26;      // a description above its box, and the box of a name
    const int ToolGap = 4, GroupGap = 12;          // between the formatting buttons, and between their groups
    const int HintHeight = 34;                     // the line under the name that says what a name does: two at most
    const int MaxNameLength = 60;
    const int HintNameLength = 30;                 // beyond this, the name is cut in that line
    const int MaxGroupLength = 24;                 // the name of a folder has to fit on the button that shows it
    const int ButtonPadding = 16;                  // left to the edges of that button, beyond its text
    readonly TextBox nameBox = new TextBox();
    readonly RichBox contentBox = new RichBox();
    readonly Button saveButton = new RoundButton(), cancelButton = new RoundButton();
    readonly Button groupButton = new RoundButton();
    readonly Label hint = new Label();             // what the name does, said as it is written
    readonly ToolTip tips = new ToolTip();         // what each formatting button does
    readonly bool nameOnly;
    readonly Image picture;                        // of a picture to save, its preview; null for a text
    readonly string within, outside;               // what the folder button says: before a folder, and with none
    string cameHtml, cameRtf;   // the formatting the text came with, saved as it is while the text does not change
    string shownRtf;            // what the box held when it was shown: the same at the end, nothing changed
    bool formatted;             // formatting came with the text, or was given or pasted since

    // For a new text ('source' is null), a copied entry to keep or a saved one to change, a text or a picture.
    // 'group' is the folder proposed for it, 'groups' those it can go into.
    public EditForm(Entry source, string group, List<string> groups)
    {
        within = Strings.FolderOf;
        outside = Strings.NoFolder;
        if (source != null && source.Picture) picture = source.Preview;
        string title = source == null ? Strings.NewTextTitle
            : !source.Saved ? (picture != null ? Strings.SavePictureTitle : Strings.SaveCopiedTitle)
            : picture != null ? Strings.EditPictureTitle : Strings.EditSavedTitle;
        Build(title, Strings.NameLabel, MaxNameLength, source == null ? "" : source.Name ?? "", groups);
        if (source != null && picture == null)
        {
            // A copied entry is kept plain: its formatting is in memory if it is one of the last copies.
            if (source.Saved)
            {
                cameHtml = source.Html;
                cameRtf = source.Rtf;
            }
            else Rich.Recent(source.Text, out cameHtml, out cameRtf);
            Fill(source.Text);
            Check();
        }
        Choose(group);
    }

    // The box shows the text formatted, if it came so; otherwise plain.
    void Fill(string text)
    {
        List<Run> runs = cameRtf != null ? Rich.FromRtf(cameRtf) : null;
        if (runs == null && cameHtml != null) runs = Rich.FromHtml(cameHtml);
        // Formatting that says something else than the text (a link with the title of a page, for its address) is not its.
        if (runs != null && !Rich.Plain(runs) && Rich.TextOf(runs).Length > 0 && Rich.Matches(runs, text))
        {
            contentBox.Rtf = Rich.ToRtf(runs, contentBox.Font, contentBox.Ink);
            formatted = true;
        }
        else
        {
            cameHtml = cameRtf = null;   // nothing worth keeping
            contentBox.Text = text.Replace("\r\n", "\n");
        }
    }

    // For a new folder ('path' is null), or to call one otherwise or move it. 'parent' is the folder proposed
    // to hold it, 'parents' those that can.
    public EditForm(string path, string parent, List<string> parents)
    {
        nameOnly = true;
        within = Strings.Inside;
        outside = Strings.OutOfFolders;
        Build(path == null ? Strings.NewFolderTitle : Strings.RenameFolderTitle, Strings.FolderNameLabel, MaxGroupLength,
            path == null ? "" : Texts.Leaf(path), parents);
        Choose(parent);
    }

    void Build(string title, string description, int maxLength, string name, List<string> groups)
    {
        Text = title;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        Secondary(FormStartPosition.CenterParent);   // and above it, as its owner

        Label nameLabel = NewLabel(description, Edge, Edge);
        nameBox.MaxLength = maxLength;
        nameBox.Text = name;
        nameBox.SetBounds(Edge, Edge + LabelRow, FieldWidth, NameHeight);
        Panel nameFrame = Look.Frame(nameBox);
        Controls.AddRange(new Control[] { nameLabel, nameFrame });
        int buttonTop = nameFrame.Bottom + Edge;
        if (!nameOnly)
        {
            // The name of an entry is optional: under it, what having one or not means for the list.
            hint.SetBounds(Edge, nameFrame.Bottom + Gap, FieldWidth, HintHeight);
            hint.ForeColor = Look.Muted;
            hint.UseMnemonic = false;
            Controls.Add(hint);
        }
        if (picture != null)
        {
            // A picture is saved as it is: in place of its content, the picture itself.
            PictureBox shown = new PictureBox();
            shown.Image = picture;
            shown.SizeMode = PictureBoxSizeMode.Zoom;
            shown.SetBounds(Edge, hint.Bottom + Gap, FieldWidth, PictureHeight);
            Controls.Add(shown);
            buttonTop = shown.Bottom + Edge;
        }
        else if (!nameOnly)
        {
            // The description of the content, and on the same line the buttons of its formatting.
            int row = hint.Bottom + Gap;
            Label contentLabel = NewLabel(Strings.ContentLabel, Edge, row + (ButtonHeight - LabelRow) / 2 + Gap / 2);
            contentBox.Font = Look.EntryFont;   // what is written is an entry: in its font; before the text, or it would take it
            contentBox.SetBounds(Edge, row + ButtonHeight + Gap, FieldWidth, ContentHeight);
            Panel contentFrame = Look.Frame(contentBox);
            contentBox.Ink = contentBox.ForeColor;
            contentBox.Formatted += delegate { formatted = true; };
            Controls.AddRange(new Control[] { contentLabel, contentFrame });
            PlaceTools(row);
            buttonTop = contentFrame.Bottom + Edge;
        }

        cancelButton.Text = Strings.Cancel;
        cancelButton.DialogResult = DialogResult.Cancel;
        cancelButton.SetBounds(Edge + FieldWidth - ButtonWidth, buttonTop, ButtonWidth, ButtonHeight);
        saveButton.Text = Strings.Save;
        saveButton.DialogResult = DialogResult.OK;
        saveButton.SetBounds(cancelButton.Left - Gap - ButtonWidth, buttonTop, ButtonWidth, ButtonHeight);
        ClientSize = new Size(Edge + FieldWidth + Edge, buttonTop + ButtonHeight + Edge);
        AcceptButton = saveButton;
        CancelButton = cancelButton;
        Look.Style(saveButton, true);
        Look.Style(cancelButton, false);
        Controls.AddRange(new Control[] { saveButton, cancelButton });

        // On the left of the buttons, the folder of the text: a click lists those there are, each by its path.
        // Without folders there is nothing to choose.
        if (groups != null && groups.Count > 0)
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            Look.Style(menu);
            menu.Items.Add(outside, null, delegate { Choose(null); });
            foreach (string each in groups)
            {
                string group = each;   // a variable of its own for the delegate
                menu.Items.Add(Texts.Display(group), null, delegate { Choose(group); });
            }
            groupButton.SetBounds(Edge, buttonTop, saveButton.Left - Edge - Edge, ButtonHeight);
            groupButton.Click += delegate { menu.Show(groupButton, 0, groupButton.Height); };
            Look.Style(groupButton, false);
            Controls.Add(groupButton);
        }

        nameBox.TextChanged += delegate { Check(); };
        contentBox.TextChanged += delegate { Check(); };
        Check();
        Finish();
    }

    // Nothing to save until what is needed has been written: the name of a folder, the content of an entry. Called
    // also once the content is in the box: a rich edit box that has no window yet does not tell its text changed.
    void Check()
    {
        saveButton.Enabled = nameOnly ? EntryName.Length > 0 : picture != null || Content.Trim().Length > 0;
        string shown = EntryName.Length > HintNameLength ? EntryName.Substring(0, HintNameLength - 1) + "…" : EntryName;
        // A picture shows itself either way, with its name beside it: the name hides nothing.
        hint.Text = EntryName.Length == 0 ? (picture != null ? Strings.PictureHintEmpty : Strings.NameHintEmpty)
            : string.Format(picture != null ? Strings.PictureHintNamed : Strings.NameHintNamed, shown);
    }

    // The formatting buttons, on the line at 'top', ending where the box ends: bold, italic, underlined; font and
    // size, each from a menu; then the rubber that takes formatting away.
    void PlaceTools(int top)
    {
        Look.Style(tips);
        Disposed += delegate { tips.Dispose(); };
        ContextMenuStrip faces = NewMenu(), sizes = NewMenu();
        faces.Items.Add(Strings.DefaultLook, null, delegate { contentBox.Face(null); contentBox.Focus(); });
        foreach (string each in Rich.Fonts)
        {
            string name = each;   // a variable of its own for the delegate
            faces.Items.Add(name, null, delegate { contentBox.Face(name); contentBox.Focus(); });
        }
        sizes.Items.Add(Strings.DefaultLook, null, delegate { contentBox.Points(0); contentBox.Focus(); });
        foreach (float each in Rich.Sizes)
        {
            float size = each;
            sizes.Items.Add(string.Format(Strings.Points, size), null, delegate { contentBox.Points(size); contentBox.Focus(); });
        }
        Button erase = Tool(Sign.Erase, Strings.PlainTip, Edge + FieldWidth, top);
        Clicked(erase, delegate { contentBox.Plain(); });
        int right = erase.Left - GroupGap;
        // Font and size by their names: as two signs they looked the same.
        right = Opens(Named(Strings.PointsTip, right, top), sizes).Left - ToolGap;
        right = Opens(Named(Strings.TypefaceTip, right, top), faces).Left - GroupGap;
        foreach (FontStyle each in new FontStyle[] { FontStyle.Underline, FontStyle.Italic, FontStyle.Bold })
        {
            FontStyle style = each;
            Button tool = Tool(style == FontStyle.Bold ? Sign.Bold : style == FontStyle.Italic ? Sign.Italic : Sign.Underline,
                style == FontStyle.Bold ? Strings.BoldTip : style == FontStyle.Italic ? Strings.ItalicTip : Strings.UnderlineTip, right, top);
            Clicked(tool, delegate { contentBox.Toggle(style); });
            right = tool.Left - ToolGap;
        }
    }

    static ContextMenuStrip NewMenu()
    {
        ContextMenuStrip menu = new ContextMenuStrip();
        Look.Style(menu);
        return menu;
    }

    // A square button with a sign, ending at 'right'.
    Button Tool(Sign sign, string tip, int right, int top)
    {
        RoundButton tool = new RoundButton();
        tool.Sign = sign;
        tool.TabStop = false;   // Tab goes from the name to the content, as before
        tool.SetBounds(right - ButtonHeight, top, ButtonHeight, ButtonHeight);
        Look.Style(tool, false);
        tips.SetToolTip(tool, tip);
        Controls.Add(tool);
        return tool;
    }

    // A button with a word, as wide as the word, ending at 'right'.
    Button Named(string text, int right, int top)
    {
        RoundButton tool = new RoundButton();
        tool.Text = text;
        tool.TabStop = false;
        int width = (int)Math.Ceiling(TextRenderer.MeasureText(text, Font).Width / Look.Zoom) + ButtonPadding;   // as designed: the window is scaled later
        tool.SetBounds(right - width, top, width, ButtonHeight);
        Look.Style(tool, false);
        Controls.Add(tool);
        return tool;
    }

    // A click does 'act', and gives the text back its focus, with the selection as it was.
    void Clicked(Button tool, Action act)
    {
        tool.Click += delegate
        {
            act();
            contentBox.Focus();
        };
    }

    // A click opens the menu under the button; its entries give the focus back.
    static Button Opens(Button tool, ContextMenuStrip menu)
    {
        tool.Click += delegate { menu.Show(tool, 0, tool.Height); };
        return tool;
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        if (shownRtf == null && contentBox.Parent != null) shownRtf = contentBox.Rtf;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        if (DialogResult == DialogResult.OK && contentBox.Parent != null) Conclude();
    }

    // How the content is saved: plain, unless it has some formatting; as it came, if nothing changed it.
    void Conclude()
    {
        Html = Rtf = null;
        if (!formatted) return;
        List<Run> runs = Rich.FromBox(contentBox, contentBox.Font, contentBox.Ink);
        if (Rich.Plain(runs)) return;
        bool same = contentBox.Rtf == shownRtf && (cameHtml != null || cameRtf != null);
        // What the box cannot show (links, tables, pictures) stays in what came.
        Html = same && cameHtml != null ? cameHtml : Rich.ToHtml(runs);
        Rtf = same ? cameRtf : null;
    }

    // The button says the path of the folder chosen; if it is too long for it, the outer folders give way.
    void Choose(string group)
    {
        Group = group;
        if (group == null || groupButton.Parent == null)   // or there are no folders, and no button
        {
            groupButton.Text = outside;
            return;
        }
        string shown = Texts.Display(group);
        int padding = (int)(ButtonPadding * Look.Zoom);   // the button has its scale already, the constant not
        for (string inner = group; ; )
        {
            groupButton.Text = within + shown;
            if (TextRenderer.MeasureText(groupButton.Text, Font).Width + padding <= groupButton.Width) return;
            int at = inner.IndexOf(Texts.Separator);
            if (at < 0) return;   // only its own name is left, and that is short enough by itself
            inner = inner.Substring(at + 1);
            shown = "… › " + Texts.Display(inner);
        }
    }

    public string EntryName { get { return nameBox.Text.Trim(); } }
    public string Content { get { return contentBox.Text.Replace("\r\n", "\n").Replace("\n", "\r\n"); } }
    // Of a formatted content, once saved: its HTML, and its RTF if it came with one; both null for plain text.
    public string Html { get; private set; }
    public string Rtf { get; private set; }
    public string Group { get; private set; }   // the folder chosen for the text, or for the folder; or null
}
