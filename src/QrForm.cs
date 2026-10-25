using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

// A text as a QR code, for a phone to read off the screen (see Qr). The code stands on white whatever the theme:
// phones read dark modules on light. Its modules are whole pixels, so that their edges stay sharp, and never fewer
// than LeastModule each: a dense code makes the window larger rather than the modules smaller.
class QrForm : Window
{
    const int Edge = 20, Side = 320, Gap = 12, ButtonEdge = 14;   // in pixels at 96 dpi
    const int LeastModule = 3;                                     // in pixels of the screen

    QrForm(Qr code)
    {
        Text = Strings.QrMenu;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        Secondary(FormStartPosition.CenterParent);   // and above it, as its owner

        int modules = code.Size + 2 * Qr.Quiet;
        int side = Math.Max(Side, (int)Math.Ceiling(modules * LeastModule / Look.Zoom));
        // The note measured as it will be drawn, then brought back to 96 dpi: the window scales everything itself.
        const TextFormatFlags flags = TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix | TextFormatFlags.HorizontalCenter;
        int note = (int)Math.Ceiling(TextRenderer.MeasureText(Strings.QrNote, Look.NoteFont,
            new Size((int)(side * Look.Zoom), 0), flags).Height / Look.Zoom) + 1;

        Panel body = new Panel();
        body.SetBounds(0, 0, Edge + side + Edge, Edge + side + Gap + note + Edge);
        body.BackColor = Look.Well;
        body.Paint += delegate(object sender, PaintEventArgs e)
        {
            Rectangle written = new Rectangle(Edge, Edge + side + Gap, side, note);
            TextRenderer.DrawText(e.Graphics, Strings.QrNote, Look.NoteFont, Scaled(written), Look.Muted, flags);
        };
        Panel board = new Panel();
        board.SetBounds(Edge, Edge, side, side);
        board.BackColor = Look.Well;
        board.Paint += delegate(object sender, PaintEventArgs e) { Draw(e.Graphics, board.ClientRectangle, code); };
        body.Controls.Add(board);

        int buttonTop = body.Bottom + ButtonEdge;
        ClientSize = new Size(body.Width, buttonTop + ButtonHeight + ButtonEdge);
        Controls.Add(body);
        RoundButton close = new RoundButton();
        close.Text = Strings.Close;
        close.DialogResult = DialogResult.OK;
        close.SetBounds(body.Width - Edge - ButtonWidth, buttonTop, ButtonWidth, ButtonHeight);
        Look.Style(close, true);
        Controls.Add(close);
        CancelButton = close;   // Esc too
        ActiveControl = close;
        Finish();
    }

    static Rectangle Scaled(Rectangle designed)
    {
        return new Rectangle((int)(designed.X * Look.Zoom), (int)(designed.Y * Look.Zoom),
            (int)(designed.Width * Look.Zoom), (int)(designed.Height * Look.Zoom));
    }

    // The code on a white rounded square, with its quiet border, as large as whole modules let it be.
    static void Draw(Graphics g, Rectangle area, Qr code)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using (GraphicsPath shape = Look.Rounded(area)) g.FillPath(Brushes.White, shape);
        g.SmoothingMode = SmoothingMode.None;
        int module = Math.Max(1, Math.Min(area.Width, area.Height) / (code.Size + 2 * Qr.Quiet));
        int left = area.Left + (area.Width - module * code.Size) / 2, top = area.Top + (area.Height - module * code.Size) / 2;
        for (int y = 0; y < code.Size; y++)
            for (int x = 0; x < code.Size; x++)
            {
                if (!code[x, y]) continue;
                int run = 1;   // the dark modules that follow, in one stroke
                while (x + run < code.Size && code[x + run, y]) run++;
                g.FillRectangle(Brushes.Black, left + x * module, top + y * module, run * module, module);
                x += run - 1;
            }
    }

    // Shows a text as a QR code, until it is closed; false, showing nothing, when the text is too long for one.
    public static bool Display(IWin32Window owner, string text)
    {
        Qr code = Qr.Of(text);
        if (code == null) return false;
        using (QrForm form = new QrForm(code)) form.ShowDialog(owner);
        return true;
    }
}
