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

// A button drawn as a rounded shape in its own colours, a little lighter under the pointer.
class RoundButton : Button
{
    bool hover;

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
            : pressed ? Look.Blend(BackColor, Look.Back, 0.25f) : hover ? Look.Blend(BackColor, Color.White, 0.12f) : BackColor;
        using (GraphicsPath shape = Look.Rounded(new Rectangle(0, 0, Width - 1, Height - 1)))
        using (Brush back = new SolidBrush(fill))
        using (Pen edge = new Pen(Focused ? Look.Ink : Enabled ? FlatAppearance.BorderColor : fill))
        {
            g.FillPath(back, shape);
            g.DrawPath(edge, shape);
        }
        TextRenderer.DrawText(g, Text, Font, ClientRectangle, Enabled ? ForeColor : Look.Border, TextFormatFlags.HorizontalCenter
            | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
    }
}

// Colours and touches shared by the windows: dark, with pastel accents and rounded shapes.
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
    public static readonly Color Back = Color.FromArgb(30, 30, 46), Well = Color.FromArgb(24, 24, 37),
        Soft = Color.FromArgb(49, 50, 68), Border = Color.FromArgb(69, 71, 90), Selected = Color.FromArgb(58, 62, 92),
        Ink = Color.FromArgb(205, 214, 244), Muted = Color.FromArgb(166, 173, 200),
        Accent = Color.FromArgb(137, 180, 250);

    // 'primary' is the one button that stands out; the others stay quiet.
    public static void Style(Button button, bool primary)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderColor = primary ? Accent : Border;
        button.BackColor = primary ? Accent : Soft;
        button.ForeColor = primary ? Back : Ink;
    }

    // A place where text is written or listed: a shade darker than the window, with dark scroll bars.
    public static void Field(Control field)
    {
        field.BackColor = Well;
        field.ForeColor = Ink;
        field.HandleCreated += delegate { Native.SetWindowTheme(field.Handle, "DarkMode_Explorer", null); };
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
        int back = ColorTranslator.ToWin32(form.BackColor), text = ColorTranslator.ToWin32(form.ForeColor), dark = 1;
        Native.DwmSetWindowAttribute(form.Handle, Native.DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, sizeof(int));
        Native.DwmSetWindowAttribute(form.Handle, Native.DWMWA_CAPTION_COLOR, ref back, sizeof(int));
        Native.DwmSetWindowAttribute(form.Handle, Native.DWMWA_TEXT_COLOR, ref text, sizeof(int));
    }
}
