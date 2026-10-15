using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

// The small window that shows an entry whole, a text or a picture, by the row it belongs to. It never takes
// the focus, and the pointer goes through it to what is underneath.
class Peek : Form
{
    const int MaxWidth = 460, MaxHeight = 320;   // of what it shows, in pixels at 96 dpi
    const int Inset = 10, RowGap = 2;            // around what it shows, and between the row and the window
    const int MaxLines = 20, MaxLength = 2000;   // of a text: beyond these it is cut, and says so
    const TextFormatFlags Flags = TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl | TextFormatFlags.NoPrefix
        | TextFormatFlags.ExpandTabs;

    string text, more;    // the text shown, and the line under it that tells how much was left out
    Image picture;
    Size content;         // the room taken by the text or the picture
    bool plain;           // Windows gave the window no edge: it draws one

    public Peek()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        Font = Look.TextFont;
        BackColor = Look.Well;
        ForeColor = Look.Ink;
        DoubleBuffered = true;
    }

    protected override bool ShowWithoutActivation
    {
        get { return true; }
    }

    // Above the main window, which is topmost itself, and out of the taskbar and of Alt+Tab.
    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams values = base.CreateParams;
            values.ExStyle |= Native.WS_EX_TOPMOST | Native.WS_EX_TOOLWINDOW | Native.WS_EX_NOACTIVATE;
            values.ClassStyle |= Native.CS_DROPSHADOW;
            return values;
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        plain = !Look.Round(this);
    }

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);
        if (m.Msg == Native.WM_NCHITTEST) m.Result = (IntPtr)Native.HTTRANSPARENT;
    }

    // Takes an entry and the size it needs, up to a limit; 'zoom' is how much larger than designed the
    // screen draws. Returns false if there is nothing to show: a picture the history file no longer gives.
    public bool Fill(Entry entry, float zoom)
    {
        if (picture != null) picture.Dispose();
        picture = null;
        text = more = null;
        Size room = new Size((int)(MaxWidth * zoom), (int)(MaxHeight * zoom));
        int inset = (int)(Inset * zoom);
        if (entry.Picture)
        {
            try
            {
                using (MemoryStream png = new MemoryStream(entry.LoadPng()))
                using (Image whole = Image.FromStream(png))
                {
                    // Made smaller to fit, never larger than it is.
                    float scale = Math.Min(1, Math.Min((float)room.Width / whole.Width, (float)room.Height / whole.Height));
                    picture = new Bitmap(whole, Math.Max(1, (int)(whole.Width * scale)), Math.Max(1, (int)(whole.Height * scale)));
                }
            }
            catch (Exception)
            {
                return false;
            }
            content = picture.Size;
        }
        else
        {
            string whole = entry.Text.Replace("\r\n", "\n").Replace('\r', '\n').Trim('\n');
            string[] lines = whole.Split('\n');
            text = string.Join("\n", lines, 0, Math.Min(lines.Length, MaxLines));
            if (text.Length > MaxLength) text = text.Substring(0, MaxLength);
            int left = whole.Length - text.Length;
            Size needed = TextRenderer.MeasureText(text, Font, new Size(room.Width, 0), Flags);
            content = new Size(Math.Min(needed.Width, room.Width), Math.Min(needed.Height, room.Height));
            if (left > 0) more = left == 1 ? Strings.MoreOne : string.Format(Strings.MoreMany, left);
            else if (needed.Height > room.Height) more = "…";
        }
        ClientSize = new Size(content.Width + 2 * inset, content.Height + 2 * inset + (more == null ? 0 : Font.Height));
        Invalidate();
        return true;
    }

    // Shows an entry under the given row of the screen, or above it where there is no room below.
    public void Reveal(Entry entry, Rectangle row, float zoom)
    {
        if (!Fill(entry, zoom))
        {
            Hide();
            return;
        }
        Rectangle screen = Screen.FromRectangle(row).WorkingArea;
        int gap = (int)(RowGap * zoom), x = Math.Max(screen.Left, Math.Min(row.Left, screen.Right - Width));
        Location = new Point(x, row.Bottom + gap + Height <= screen.Bottom ? row.Bottom + gap : row.Top - gap - Height);
        Visible = true;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        int inset = (ClientSize.Width - content.Width) / 2;
        if (picture != null) e.Graphics.DrawImageUnscaled(picture, inset, inset);
        else TextRenderer.DrawText(e.Graphics, text ?? "", Font, new Rectangle(new Point(inset, inset), content), ForeColor, Flags);
        if (more != null)
            TextRenderer.DrawText(e.Graphics, more, Font, new Rectangle(inset, inset + content.Height, content.Width, Font.Height),
                Look.Muted, TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
        if (!plain) return;
        using (Pen edge = new Pen(Look.Border))
        {
            e.Graphics.DrawRectangle(edge, 0, 0, ClientSize.Width - 1, ClientSize.Height - 1);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && picture != null) picture.Dispose();
        base.Dispose(disposing);
    }
}
