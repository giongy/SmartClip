using System;
using System.Drawing;
using System.Windows.Forms;

// The window where a saved text gets its name and its content.
class EditForm : Form
{
    const int Edge = 12, FieldWidth = 420, ContentHeight = 190;   // in pixels at 96 dpi
    const int ButtonWidth = 90, ButtonHeight = 26, Gap = 6;
    const int MaxNameLength = 60;
    readonly TextBox nameBox = new TextBox(), contentBox = new TextBox();
    readonly Button saveButton = new RoundButton(), cancelButton = new RoundButton();

    public EditForm(Entry source)
    {
        SuspendLayout();
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = SystemFonts.MessageBoxFont;
        Text = source == null ? "Nuovo testo" : source.Name == null ? "Salva il testo copiato" : "Modifica il testo salvato";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        TopMost = true;
        BackColor = Look.Back;
        ForeColor = Look.Ink;

        Label nameLabel = NewLabel("Nome (è ciò che si vede nell'elenco):", Edge);
        nameBox.MaxLength = MaxNameLength;
        nameBox.SetBounds(Edge, 32, FieldWidth, 26);
        Label contentLabel = NewLabel("Contenuto:", 69);
        contentBox.Multiline = true;
        contentBox.AcceptsReturn = true;
        contentBox.ScrollBars = ScrollBars.Vertical;
        contentBox.SetBounds(Edge, 89, FieldWidth, ContentHeight);
        Panel nameFrame = Look.Frame(nameBox), contentFrame = Look.Frame(contentBox);

        int buttonTop = contentFrame.Bottom + Edge;
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

        if (source != null)
        {
            nameBox.Text = source.Name ?? "";
            // The box breaks lines only at CR LF, while a copied text may hold bare LF.
            contentBox.Text = source.Text.Replace("\r\n", "\n").Replace("\n", "\r\n");
        }
        // Nothing to save until both are written.
        EventHandler check = delegate { saveButton.Enabled = EntryName.Length > 0 && Content.Trim().Length > 0; };
        nameBox.TextChanged += check;
        contentBox.TextChanged += check;
        check(this, EventArgs.Empty);

        Controls.AddRange(new Control[] { nameLabel, nameFrame, contentLabel, contentFrame, saveButton, cancelButton });
        ResumeLayout(false);
        PerformLayout();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        Look.Caption(this);
    }

    public string EntryName { get { return nameBox.Text.Trim(); } }
    public string Content { get { return contentBox.Text; } }

    static Label NewLabel(string text, int y)
    {
        Label label = new Label();
        label.Text = text;
        label.AutoSize = true;
        label.Location = new Point(Edge, y);
        return label;
    }
}
