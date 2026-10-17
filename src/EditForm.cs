using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

// The window where a saved text gets its name, its content and its folder; a saved picture its name and its
// folder, with the picture in place of the content. The same window, without the content, asks what a folder is
// called and which folder it is in.
class EditForm : Window
{
    const int Edge = 12, FieldWidth = 420, ContentHeight = 190, Gap = 6;   // in pixels at 96 dpi
    const int PictureHeight = 150;                 // the picture shown in place of the content
    const int LabelRow = 20, NameHeight = 26;      // a description above its box, and the box of a name
    const int HintHeight = 34;                     // the line under the name that says what a name does: two at most
    const int MaxNameLength = 60;
    const int HintNameLength = 30;                 // beyond this, the name is cut in that line
    const int MaxGroupLength = 24;                 // the name of a folder has to fit on the button that shows it
    const int ButtonPadding = 16;                  // left to the edges of that button, beyond its text
    readonly TextBox nameBox = new TextBox(), contentBox = new TextBox();
    readonly Button saveButton = new RoundButton(), cancelButton = new RoundButton();
    readonly Button groupButton = new RoundButton();
    readonly Label hint = new Label();             // what the name does, said as it is written
    readonly bool nameOnly;
    readonly Image picture;                        // of a picture to save, its preview; null for a text
    readonly string within, outside;               // what the folder button says: before a folder, and with none

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
        // The box breaks lines only at CR LF, while a copied text may hold bare LF.
        if (source != null && picture == null) contentBox.Text = source.Text.Replace("\r\n", "\n").Replace("\n", "\r\n");
        Choose(group);
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
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;   // and above it, as its owner

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
            Label contentLabel = NewLabel(Strings.ContentLabel, Edge, hint.Bottom + Gap);
            contentBox.Multiline = true;
            contentBox.AcceptsReturn = true;
            contentBox.ScrollBars = ScrollBars.Vertical;
            contentBox.Font = Look.EntryFont;   // what is written is an entry: in its font
            contentBox.SetBounds(Edge, contentLabel.Top + LabelRow, FieldWidth, ContentHeight);
            Panel contentFrame = Look.Frame(contentBox);
            Controls.AddRange(new Control[] { contentLabel, contentFrame });
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

        // Nothing to save until what is needed has been written: the name of a folder, the content of an entry.
        EventHandler check = delegate
        {
            saveButton.Enabled = nameOnly ? EntryName.Length > 0 : picture != null || Content.Trim().Length > 0;
            string shown = EntryName.Length > HintNameLength ? EntryName.Substring(0, HintNameLength - 1) + "…" : EntryName;
            // A picture shows itself either way, with its name beside it: the name hides nothing.
            hint.Text = EntryName.Length == 0 ? (picture != null ? Strings.PictureHintEmpty : Strings.NameHintEmpty)
                : string.Format(picture != null ? Strings.PictureHintNamed : Strings.NameHintNamed, shown);
        };
        nameBox.TextChanged += check;
        contentBox.TextChanged += check;
        check(this, EventArgs.Empty);
        Finish();
    }

    // The button says the path of the folder chosen; if it is too long for it, the outer folders give way.
    void Choose(string group)
    {
        Group = group;
        if (group == null)
        {
            groupButton.Text = outside;
            return;
        }
        string shown = Texts.Display(group);
        for (string inner = group; ; )
        {
            groupButton.Text = within + shown;
            if (TextRenderer.MeasureText(groupButton.Text, Font).Width + ButtonPadding <= groupButton.Width) return;
            int at = inner.IndexOf(Texts.Separator);
            if (at < 0) return;   // only its own name is left, and that is short enough by itself
            inner = inner.Substring(at + 1);
            shown = "… › " + Texts.Display(inner);
        }
    }

    public string EntryName { get { return nameBox.Text.Trim(); } }
    public string Content { get { return contentBox.Text; } }
    public string Group { get; private set; }   // the folder chosen for the text, or for the folder; or null
}
