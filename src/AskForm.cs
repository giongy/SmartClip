using System;
using System.Drawing;
using System.Media;
using System.Windows.Forms;

// A question, or a piece of news, in a small window of the program's own. A message box of Windows would stay
// white in the dark and answer in the language of Windows rather than that of the program. As in the message
// boxes of Windows 11, the text stands on a lighter area and the buttons below it; the coloured button is the
// one Enter presses, and in a question it is "No": a slip of the finger changes nothing.
class AskForm : Form
{
    const int Edge = 20, MaxTextWidth = 380, MinTextWidth = 240;   // in pixels at 96 dpi
    const int ButtonWidth = 90, ButtonHeight = 26, Gap = 6, ButtonEdge = 14;
    const TextFormatFlags Flags = TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix;

    AskForm(string text, bool question)
    {
        SuspendLayout();
        // Built as designed, then made larger at the end by Look.Zoom, the scale of the screen and of the text: left to
        // Windows Forms, the controls that inherit the font would be scaled twice.
        AutoScaleMode = AutoScaleMode.None;
        Font = Look.TextFont;
        Text = Program.AppName;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ShowIcon = false;
        StartPosition = FormStartPosition.CenterParent;   // and above it, as its owner
        BackColor = Look.Back;
        ForeColor = Look.Ink;

        // Measured as it will be drawn, then brought back to 96 dpi: the window scales everything itself.
        Size measured = TextRenderer.MeasureText(text, Font, new Size((int)(MaxTextWidth * Look.Zoom), 0), Flags);
        int width = Math.Max(MinTextWidth, (int)Math.Ceiling(measured.Width / Look.Zoom) + 1);
        int height = (int)Math.Ceiling(measured.Height / Look.Zoom) + 1;
        Panel body = new Panel();
        body.SetBounds(0, 0, Edge + width + Edge, Edge + height + Edge);
        body.BackColor = Look.Well;
        body.Paint += delegate(object sender, PaintEventArgs e)
        {
            int edge = (int)(Edge * Look.Zoom);
            TextRenderer.DrawText(e.Graphics, text, Font, Rectangle.Inflate(body.ClientRectangle, -edge, -edge), Look.Ink, Flags);
        };
        int buttonTop = body.Bottom + ButtonEdge;
        ClientSize = new Size(body.Width, buttonTop + ButtonHeight + ButtonEdge);
        Controls.Add(body);

        RoundButton answer = NewButton(question ? Strings.No : Strings.Ok, question ? DialogResult.No : DialogResult.OK, true,
            body.Width - Edge, buttonTop);
        if (question) NewButton(Strings.Yes, DialogResult.Yes, false, answer.Left - Gap, buttonTop);
        CancelButton = answer;   // Esc too
        ActiveControl = answer;
        Scale(new SizeF(Look.Zoom, Look.Zoom));
        ResumeLayout(false);
        PerformLayout();
    }

    // A button whose right edge is at 'right'.
    RoundButton NewButton(string text, DialogResult result, bool primary, int right, int top)
    {
        RoundButton button = new RoundButton();
        button.Text = text;
        button.DialogResult = result;
        button.SetBounds(right - ButtonWidth, top, ButtonWidth, ButtonHeight);
        Look.Style(button, primary);
        Controls.Add(button);
        return button;
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        Look.Caption(this);
    }

    // Asks a question and says whether the answer is yes. A 'warning' rings, as the message boxes of Windows do
    // for something that may go wrong.
    public static bool Ask(IWin32Window owner, string question, bool warning)
    {
        using (AskForm form = new AskForm(question, true))
        {
            if (warning) form.Shown += delegate { SystemSounds.Exclamation.Play(); };
            return form.ShowDialog(owner) == DialogResult.Yes;
        }
    }

    // Says something that only needs to be read.
    public static void Tell(IWin32Window owner, string news)
    {
        using (AskForm form = new AskForm(news, false)) form.ShowDialog(owner);
    }
}
