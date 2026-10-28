using System;
using System.Drawing;
using System.Windows.Forms;

// The window of the recovery phrase (Vault): to choose it, or to unlock with it the saved texts another PC encrypted.
// What it is for on top, on a lighter area as in the questions (AskForm); then the box, and under it what the phrase
// will be, as it is compared (three words, joined by Vault.WordSeparator, whatever the letter case), or what is wrong.
class PhraseForm : Window
{
    const int Edge = 20, TextWidth = 380, Gap = 6, ButtonEdge = 14;   // in pixels at 96 dpi
    const int LabelRow = 20, BoxHeight = 26, HintHeight = 20, ButtonPadding = 24;
    const TextFormatFlags Flags = TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix;
    readonly TextBox box = new TextBox();
    readonly Label hint = new Label();
    readonly RoundButton okButton = new RoundButton(), cancelButton = new RoundButton();
    readonly Func<string, bool> unlock;   // to unlock: tries the phrase written; null to choose one

    // To choose the phrase.
    public PhraseForm() : this(Strings.PhraseTitle, string.Format(Strings.PhraseExplain, Strings.PhraseExample), Strings.Save, null)
    {
    }

    // To unlock the saved texts with it: 'unlock' tries a phrase. The button on the left, for one who does not have the
    // phrase, closes the window with Abort.
    public PhraseForm(Func<string, bool> unlock) : this(Strings.UnlockTitle, Strings.UnlockExplain, Strings.Unlock, unlock)
    {
    }

    PhraseForm(string title, string explanation, string ok, Func<string, bool> unlock)
    {
        this.unlock = unlock;
        Text = title;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        Secondary(FormStartPosition.CenterParent);   // and above it, as its owner

        // Measured as it will be drawn, then brought back to 96 dpi: the window scales everything itself.
        Size measured = TextRenderer.MeasureText(explanation, Font, new Size((int)(TextWidth * Look.Zoom), 0), Flags);
        int height = (int)Math.Ceiling(measured.Height / Look.Zoom) + 1;
        Panel body = new Panel();
        body.SetBounds(0, 0, Edge + TextWidth + Edge, Edge + height + Edge);
        body.BackColor = Look.Well;
        body.Paint += delegate(object sender, PaintEventArgs e)
        {
            int edge = (int)(Edge * Look.Zoom);
            TextRenderer.DrawText(e.Graphics, explanation, Font, Rectangle.Inflate(body.ClientRectangle, -edge, -edge), Look.Ink, Flags);
        };
        Controls.Add(body);

        Label label = NewLabel(Strings.PhraseLabel, Edge, body.Bottom + ButtonEdge);
        box.SetBounds(Edge, label.Top + LabelRow, TextWidth, BoxHeight);
        Panel frame = Look.Frame(box);
        hint.SetBounds(Edge, frame.Bottom + Gap, TextWidth, HintHeight);
        hint.UseMnemonic = false;
        hint.ForeColor = Look.Muted;
        Controls.AddRange(new Control[] { label, frame, hint });

        int buttonTop = hint.Bottom + ButtonEdge, right = Edge + TextWidth;
        Place(cancelButton, Strings.Cancel, DialogResult.Cancel, false, right, buttonTop);
        Place(okButton, ok, unlock == null ? DialogResult.OK : DialogResult.None, true, cancelButton.Left - Gap, buttonTop);
        if (unlock != null)
        {
            RoundButton none = new RoundButton();
            Place(none, Strings.NoPhraseButton, DialogResult.Abort, false, 0, buttonTop);
            none.Left = Edge;
            okButton.Click += delegate { Try(); };
        }
        ClientSize = new Size(body.Width, buttonTop + ButtonHeight + ButtonEdge);
        AcceptButton = okButton;
        CancelButton = cancelButton;
        box.TextChanged += delegate { Check(); };
        Check();
        ActiveControl = box;
        Finish();
    }

    // The phrase as written; Vault.Phrase gives it as it is compared.
    public string Phrase
    {
        get { return box.Text; }
    }

    // A button whose right edge is at 'right', as wide as its text needs and never narrower than the others.
    void Place(RoundButton button, string text, DialogResult result, bool primary, int right, int top)
    {
        int width = Math.Max(ButtonWidth, (int)Math.Ceiling(TextRenderer.MeasureText(text, Font).Width / Look.Zoom) + ButtonPadding);
        button.Text = text;
        button.DialogResult = result;
        button.SetBounds(right - width, top, width, ButtonHeight);
        Look.Style(button, primary);
        Controls.Add(button);
    }

    // Under the box, what the phrase will be once compared, or that it is not three words yet; nothing to save or try
    // until it is.
    void Check()
    {
        string phrase = Vault.Phrase(box.Text);
        okButton.Enabled = phrase != null;
        hint.ForeColor = Look.Muted;
        hint.Text = box.Text.Trim().Length == 0 ? "" : phrase == null ? Strings.PhraseNotThree : string.Format(Strings.PhraseWillBe, phrase);
    }

    // Tries the phrase: the window closes if it unlocks the saved texts; otherwise it says it is not theirs.
    void Try()
    {
        Cursor = Cursors.WaitCursor;   // the phrase is slow to try on purpose: most of a second
        bool done;
        try
        {
            done = unlock(box.Text);
        }
        finally
        {
            Cursor = Cursors.Default;
        }
        if (done)
        {
            DialogResult = DialogResult.OK;
            return;
        }
        hint.ForeColor = Look.Own(Sign.Error);
        hint.Text = Strings.PhraseWrong;
        box.SelectAll();
        box.Focus();
    }
}
