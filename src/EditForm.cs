using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

// The window where a saved text gets its name, its content and its folder. The same window, with the name
// alone, asks what a folder is called.
class EditForm : Form
{
    const int Edge = 12, FieldWidth = 420, ContentHeight = 190;   // in pixels at 96 dpi
    const int ButtonWidth = 90, ButtonHeight = 26, Gap = 6;
    const int LabelRow = 20, NameHeight = 26;      // a description above its box, and the box of a name
    const int MaxNameLength = 60;
    const int MaxGroupLength = 24;                 // the name of a folder has to fit on the button that shows it
    readonly TextBox nameBox = new TextBox(), contentBox = new TextBox();
    readonly Button saveButton = new RoundButton(), cancelButton = new RoundButton();
    readonly Button groupButton = new RoundButton();
    readonly bool nameOnly;

    // For a new text ('source' is null), a copied one to keep or a saved one to change. 'group' is the folder
    // proposed for it, 'groups' those it can go into.
    public EditForm(Entry source, string group, List<string> groups)
    {
        Build(source == null ? "Nuovo testo" : source.Name == null ? "Salva il testo copiato" : "Modifica il testo salvato",
            "Nome (è ciò che si vede nell'elenco):", MaxNameLength, source == null ? "" : source.Name ?? "", groups);
        // The box breaks lines only at CR LF, while a copied text may hold bare LF.
        if (source != null) contentBox.Text = source.Text.Replace("\r\n", "\n").Replace("\n", "\r\n");
        Choose(group);
    }

    // For a new folder ('name' is null), or to call one otherwise.
    public EditForm(string name)
    {
        nameOnly = true;
        Build(name == null ? "Nuova cartella" : "Rinomina la cartella", "Nome della cartella:", MaxGroupLength, name ?? "", null);
    }

    void Build(string title, string description, int maxLength, string name, List<string> groups)
    {
        SuspendLayout();
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = Look.TextFont;
        Text = title;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        TopMost = true;
        BackColor = Look.Back;
        ForeColor = Look.Ink;

        Label nameLabel = NewLabel(description, Edge);
        nameBox.MaxLength = maxLength;
        nameBox.Text = name;
        nameBox.SetBounds(Edge, Edge + LabelRow, FieldWidth, NameHeight);
        Panel nameFrame = Look.Frame(nameBox);
        Controls.AddRange(new Control[] { nameLabel, nameFrame });
        int buttonTop = nameFrame.Bottom + Edge;
        if (!nameOnly)
        {
            Label contentLabel = NewLabel("Contenuto:", nameFrame.Bottom + Edge);
            contentBox.Multiline = true;
            contentBox.AcceptsReturn = true;
            contentBox.ScrollBars = ScrollBars.Vertical;
            contentBox.SetBounds(Edge, contentLabel.Top + LabelRow, FieldWidth, ContentHeight);
            Panel contentFrame = Look.Frame(contentBox);
            Controls.AddRange(new Control[] { contentLabel, contentFrame });
            buttonTop = contentFrame.Bottom + Edge;
        }

        cancelButton.Text = "Annulla";
        cancelButton.DialogResult = DialogResult.Cancel;
        cancelButton.SetBounds(Edge + FieldWidth - ButtonWidth, buttonTop, ButtonWidth, ButtonHeight);
        saveButton.Text = "Salva";
        saveButton.DialogResult = DialogResult.OK;
        saveButton.SetBounds(cancelButton.Left - Gap - ButtonWidth, buttonTop, ButtonWidth, ButtonHeight);
        ClientSize = new Size(Edge + FieldWidth + Edge, buttonTop + ButtonHeight + Edge);
        AcceptButton = saveButton;
        CancelButton = cancelButton;
        Look.Style(saveButton, true);
        Look.Style(cancelButton, false);
        Controls.AddRange(new Control[] { saveButton, cancelButton });

        // On the left of the buttons, the folder of the text: a click lists those there are. Without folders
        // there is nothing to choose.
        if (groups != null && groups.Count > 0)
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            Look.Style(menu);
            menu.Items.Add(NoGroup, null, delegate { Choose(null); });
            foreach (string each in groups)
            {
                string group = each;   // a variable of its own for the delegate
                menu.Items.Add(group, null, delegate { Choose(group); });
            }
            groupButton.SetBounds(Edge, buttonTop, saveButton.Left - Edge - Edge, ButtonHeight);
            groupButton.Click += delegate { menu.Show(groupButton, 0, groupButton.Height); };
            Look.Style(groupButton, false);
            Controls.Add(groupButton);
        }

        // Nothing to save until all there is has been written.
        EventHandler check = delegate { saveButton.Enabled = EntryName.Length > 0 && (nameOnly || Content.Trim().Length > 0); };
        nameBox.TextChanged += check;
        contentBox.TextChanged += check;
        check(this, EventArgs.Empty);
        ResumeLayout(false);
        PerformLayout();
    }

    const string NoGroup = "Nessuna cartella";

    void Choose(string group)
    {
        Group = group;
        groupButton.Text = group == null ? NoGroup : "Cartella: " + group;
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        Look.Caption(this);
    }

    public string EntryName { get { return nameBox.Text.Trim(); } }
    public string Content { get { return contentBox.Text; } }
    public string Group { get; private set; }   // the folder chosen for the text, or null

    static Label NewLabel(string text, int y)
    {
        Label label = new Label();
        label.Text = text;
        label.AutoSize = true;
        label.Location = new Point(Edge, y);
        return label;
    }
}
