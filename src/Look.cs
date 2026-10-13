using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

// A checkbox drawn in the colours of the window: the system one cannot be made dark.
class Toggle : CheckBox
{
    const int TextGap = 7;   // between the box and its text, in pixels at 96 dpi

    int Side { get { return Font.Height; } }

    public override Size GetPreferredSize(Size proposed)
    {
        Size text = TextRenderer.MeasureText(Text, Font, Size.Empty, TextFormatFlags.NoPrefix);
        return new Size(Side + TextGap + text.Width, Math.Max(Side, text.Height) + 2);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.Clear(Parent.BackColor);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        Rectangle box = new Rectangle(0, (Height - Side) / 2, Side - 1, Side - 1);
        using (Brush fill = new SolidBrush(Checked ? Look.Accent : Look.Well))
        using (Pen edge = new Pen(Checked ? Look.Accent : Focused ? Look.Ink : Look.Border))
        using (Pen tick = new Pen(Look.Back, Side / 8f))
        {
            g.FillRectangle(fill, box);
            g.DrawRectangle(edge, box);
            float unit = box.Width / 16f;
            if (Checked)
                g.DrawLines(tick, new PointF[]
                {
                    new PointF(box.Left + 4 * unit, box.Top + 8.5f * unit),
                    new PointF(box.Left + 7 * unit, box.Top + 11.5f * unit),
                    new PointF(box.Left + 12.5f * unit, box.Top + 5 * unit)
                });
        }
        Rectangle text = Rectangle.FromLTRB(box.Right + 1 + TextGap, 0, Width, Height);
        TextRenderer.DrawText(g, Text, Font, text, ForeColor,
            TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
    }
}

// A panel drawn as a rounded shape a shade darker than the window, with a thin edge that takes the accent
// colour while what it holds has the focus.
class Card : Panel
{
    public Card()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.Clear(Parent.BackColor);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using (GraphicsPath shape = Look.Rounded(new Rectangle(0, 0, Width - 1, Height - 1)))
        using (Brush fill = new SolidBrush(Look.Well))
        using (Pen edge = new Pen(ContainsFocus ? Look.Accent : Look.Border))
        {
            g.FillPath(fill, shape);
            g.DrawPath(edge, shape);
        }
        base.OnPaint(e);
    }
}

// A button drawn as a rounded shape in its own colours, a little different under the pointer, with a small
// sign before its text if it is given one.
class RoundButton : Button
{
    const int SignSize = 14, SignGap = 6;   // in pixels at 96 dpi, like the height the button is designed for
    const int DesignHeight = 26;
    bool hover;
    Sign? sign;

    public Sign? Sign
    {
        get { return sign; }
        set { sign = value; Invalidate(); }
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        hover = true;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        hover = false;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.Clear(Parent.BackColor);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        bool pressed = hover && Capture;
        Color fill = !Enabled ? Look.Blend(BackColor, Parent.BackColor, 0.6f)
            : pressed ? Look.Blend(BackColor, Look.Ink, 0.16f) : hover ? Look.Blend(BackColor, Look.Ink, 0.07f) : BackColor;
        using (GraphicsPath shape = Look.Rounded(new Rectangle(0, 0, Width - 1, Height - 1)))
        using (Brush back = new SolidBrush(fill))
        using (Pen edge = new Pen(Focused ? Look.Ink : Enabled ? FlatAppearance.BorderColor : Look.Border))
        {
            g.FillPath(back, shape);
            g.DrawPath(edge, shape);
        }
        // Sign and text together are centred. On a coloured button the sign takes the colour of the text.
        const TextFormatFlags flags = TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix
            | TextFormatFlags.NoPadding;
        Color ink = Enabled ? ForeColor : Look.Blend(Look.Muted, Parent.BackColor, 0.5f);
        float scale = (float)Height / DesignHeight;
        int side = sign.HasValue ? (int)(SignSize * scale) : 0, gap = sign.HasValue ? (int)(SignGap * scale) : 0;
        int text = TextRenderer.MeasureText(g, Text, Font, Size.Empty, flags).Width, left = (Width - side - gap - text) / 2;
        if (sign.HasValue)
            Look.Draw(g, sign.Value, new Rectangle(left, (Height - side) / 2, side, side),
                !Enabled || BackColor == Look.Accent ? ink : (Color?)null);
        TextRenderer.DrawText(g, Text, Font, new Rectangle(left + side + gap, 0, text + 2, Height), ink, flags);
    }
}

// The small signs drawn beside entries and on buttons: they are where the colour is.
enum Sign { Text, Link, Star, Plus, Edit, Delete, Options, List, Keys, Search }

// Colours and touches shared by the windows: neutral, with rounded shapes and small coloured signs.
static class Look
{
    const int FramePadding = 6;   // between the edge of a framed text box and its text
    const int Radius = 6;         // of the rounded corners

    public static GraphicsPath Rounded(Rectangle area)
    {
        int side = 2 * Radius;
        GraphicsPath path = new GraphicsPath();
        path.AddArc(area.Left, area.Top, side, side, 180, 90);
        path.AddArc(area.Right - side, area.Top, side, side, 270, 90);
        path.AddArc(area.Right - side, area.Bottom - side, side, side, 0, 90);
        path.AddArc(area.Left, area.Bottom - side, side, side, 90, 90);
        path.CloseFigure();
        return path;
    }

    // A colour part of the way from one to another.
    public static Color Blend(Color from, Color to, float amount)
    {
        return Color.FromArgb((int)(from.R + (to.R - from.R) * amount), (int)(from.G + (to.G - from.G) * amount),
            (int)(from.B + (to.B - from.B) * amount));
    }

    // The neutral part: a warm grey window, neither dark nor white, with lighter areas where text is written
    // or listed.
    public static readonly Color Back = Color.FromArgb(232, 230, 225), Well = Color.FromArgb(247, 246, 243),
        Soft = Color.FromArgb(219, 216, 210), Border = Color.FromArgb(198, 194, 186), Selected = Color.FromArgb(214, 224, 244),
        Ink = Color.FromArgb(38, 40, 46), Muted = Color.FromArgb(108, 110, 116);
    // The coloured part: the accent, and the colours of the signs.
    public static readonly Color Accent = Color.FromArgb(62, 108, 214), Green = Color.FromArgb(40, 158, 104),
        Amber = Color.FromArgb(222, 148, 30), Red = Color.FromArgb(214, 76, 72), Teal = Color.FromArgb(22, 152, 164),
        Purple = Color.FromArgb(132, 92, 220);
    static readonly bool Dark = Back.GetBrightness() < 0.5f;   // what Windows draws (title bar, scroll bars) follows

    // 'primary' is the one button that stands out; the others stay quiet.
    public static void Style(Button button, bool primary)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderColor = primary ? Accent : Border;
        button.BackColor = primary ? Accent : Well;
        button.ForeColor = primary ? Color.White : Ink;
    }

    // A place where text is written or listed: a shade off the window, with scroll bars to match.
    public static void Field(Control field)
    {
        field.BackColor = Well;
        field.ForeColor = Ink;
        field.HandleCreated += delegate { Native.SetWindowTheme(field.Handle, Dark ? "DarkMode_Explorer" : "Explorer", null); };
    }

    // Draws one of the small signs in its own colour, or in the given one, inside a square box. Each is laid
    // out on a grid of 16.
    public static void Draw(Graphics g, Sign sign, Rectangle box, Color? colour)
    {
        float u = box.Width / 16f, x = box.Left, y = box.Top;
        Color own = sign == Sign.Text || sign == Sign.Search || sign == Sign.Keys ? Accent : sign == Sign.Link ? Teal
            : sign == Sign.Star || sign == Sign.Edit ? Amber : sign == Sign.Plus ? Green : sign == Sign.Delete ? Red : Purple;
        SmoothingMode mode = g.SmoothingMode;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using (Pen pen = new Pen(colour ?? own, 1.7f * u))
        using (Brush fill = new SolidBrush(colour ?? own))
        {
            pen.StartCap = pen.EndCap = LineCap.Round;
            pen.LineJoin = LineJoin.Round;
            switch (sign)
            {
                case Sign.Text:
                case Sign.List:
                    g.DrawLine(pen, x + 2.5f * u, y + 4 * u, x + 13.5f * u, y + 4 * u);
                    g.DrawLine(pen, x + 2.5f * u, y + 8 * u, x + 13.5f * u, y + 8 * u);
                    g.DrawLine(pen, x + 2.5f * u, y + 12 * u, x + (sign == Sign.Text ? 9 : 13.5f) * u, y + 12 * u);
                    break;
                case Sign.Link:
                    g.DrawLine(pen, x + 4 * u, y + 12 * u, x + 12 * u, y + 4 * u);
                    g.DrawLines(pen, new PointF[] { new PointF(x + 6 * u, y + 4 * u), new PointF(x + 12 * u, y + 4 * u),
                        new PointF(x + 12 * u, y + 10 * u) });
                    break;
                case Sign.Star:
                    PointF[] star = new PointF[10];
                    for (int i = 0; i < star.Length; i++)
                    {
                        double angle = -Math.PI / 2 + i * Math.PI / 5, radius = (i % 2 == 0 ? 7 : 3) * u;
                        star[i] = new PointF(x + 8 * u + (float)(radius * Math.Cos(angle)), y + 8.5f * u + (float)(radius * Math.Sin(angle)));
                    }
                    g.FillPolygon(fill, star);
                    break;
                case Sign.Plus:
                    g.DrawLine(pen, x + 8 * u, y + 3 * u, x + 8 * u, y + 13 * u);
                    g.DrawLine(pen, x + 3 * u, y + 8 * u, x + 13 * u, y + 8 * u);
                    break;
                case Sign.Edit:   // a pencil, and the line it leaves
                    pen.Width = 2.6f * u;
                    g.DrawLine(pen, x + 5.5f * u, y + 10.5f * u, x + 11.5f * u, y + 4.5f * u);
                    pen.Width = 1.4f * u;
                    g.DrawLine(pen, x + 3 * u, y + 13.5f * u, x + 13 * u, y + 13.5f * u);
                    break;
                case Sign.Delete:
                    g.DrawLine(pen, x + 4 * u, y + 4 * u, x + 12 * u, y + 12 * u);
                    g.DrawLine(pen, x + 12 * u, y + 4 * u, x + 4 * u, y + 12 * u);
                    break;
                case Sign.Options:   // two sliders
                    g.DrawLine(pen, x + 2.5f * u, y + 5 * u, x + 13.5f * u, y + 5 * u);
                    g.DrawLine(pen, x + 2.5f * u, y + 11 * u, x + 13.5f * u, y + 11 * u);
                    g.FillEllipse(fill, x + 8 * u, y + 2.5f * u, 5 * u, 5 * u);
                    g.FillEllipse(fill, x + 3 * u, y + 8.5f * u, 5 * u, 5 * u);
                    break;
                case Sign.Keys:   // a keyboard
                    pen.Width = 1.4f * u;
                    g.DrawRectangle(pen, x + 1.5f * u, y + 3.5f * u, 13 * u, 9 * u);
                    foreach (float key in new float[] { 4.5f, 8, 11.5f })
                        g.FillEllipse(fill, x + (key - 0.9f) * u, y + 5.6f * u, 1.8f * u, 1.8f * u);
                    g.DrawLine(pen, x + 5 * u, y + 10 * u, x + 11 * u, y + 10 * u);
                    break;
                case Sign.Search:
                    g.DrawEllipse(pen, x + 2 * u, y + 2 * u, 8.5f * u, 8.5f * u);
                    g.DrawLine(pen, x + 9.6f * u, y + 9.6f * u, x + 14 * u, y + 14 * u);
                    break;
            }
        }
        g.SmoothingMode = mode;
    }

    // A text box as a flat darker area. The system border cannot be coloured, so the box goes borderless
    // inside a panel that takes its place; the panel is what gets added to the window.
    public static Panel Frame(TextBox box)
    {
        box.BorderStyle = BorderStyle.None;
        Field(box);
        int textHeight = box.Height;
        Panel frame = Wrap(box, FramePadding);
        // A single-line box keeps the height of its text, whatever it is asked: the frame is built around it.
        if (!box.Multiline)
        {
            frame.Height = textHeight + 2 * FramePadding;
            box.Top = FramePadding;
            box.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        }
        return frame;
    }

    // Puts a control inside a rounded card that takes its place; the card is what gets added to the window.
    public static Panel Wrap(Control inner, int padding)
    {
        Card card = new Card();
        card.Bounds = inner.Bounds;
        card.BackColor = Well;
        inner.Bounds = Rectangle.Inflate(new Rectangle(Point.Empty, card.Size), -padding, -padding);
        inner.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;   // it follows its card
        inner.GotFocus += delegate { card.Invalidate(); };
        inner.LostFocus += delegate { card.Invalidate(); };
        card.Controls.Add(inner);
        return card;
    }

    public static void Style(ContextMenuStrip menu)
    {
        menu.Renderer = new ToolStripProfessionalRenderer(new MenuColors());
        menu.ForeColor = Ink;
        menu.ShowImageMargin = false;
    }

    class MenuColors : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground { get { return Soft; } }
        public override Color MenuItemSelected { get { return Selected; } }
        public override Color MenuItemBorder { get { return Selected; } }
        public override Color MenuBorder { get { return Border; } }
    }

    // Title bar in the colour of the window (Windows 11; earlier versions refuse and keep their own).
    public static void Caption(Form form)
    {
        int back = ColorTranslator.ToWin32(form.BackColor), text = ColorTranslator.ToWin32(form.ForeColor), dark = Dark ? 1 : 0;
        Native.DwmSetWindowAttribute(form.Handle, Native.DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, sizeof(int));
        Native.DwmSetWindowAttribute(form.Handle, Native.DWMWA_CAPTION_COLOR, ref back, sizeof(int));
        Native.DwmSetWindowAttribute(form.Handle, Native.DWMWA_TEXT_COLOR, ref text, sizeof(int));
    }
}
