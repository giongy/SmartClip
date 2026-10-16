using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Microsoft.Win32;

// A checkbox drawn as a switch in the colours of the window: the system one cannot be coloured. The knob is
// on the right, on the accent colour, while it is on; it slides there when it changes, and grows a little
// under the pointer, as the switches of Windows 11 do.
class Toggle : CheckBox
{
    const int TextGap = 8;        // between the switch and its text, in pixels at 96 dpi
    const float Stretch = 1.8f;   // how many times its height the switch is wide
    const int KnobInset = 3;      // between the edge of the switch and its knob
    const float SlideStep = 0.2f; // of the way the knob goes at each tick: five ticks from one end to the other
    const int SlideMs = 15;
    float knob;                   // where the knob is: 0 on the left, 1 on the right
    bool hover;
    Timer slide;

    int Side { get { return Font.Height; } }
    int Track { get { return (int)(Side * Stretch); } }

    public override Size GetPreferredSize(Size proposed)
    {
        Size text = TextRenderer.MeasureText(Text, Font, Size.Empty, TextFormatFlags.NoPrefix);
        return new Size(Track + TextGap + text.Width, Math.Max(Side, text.Height) + 2);
    }

    protected override void OnCheckedChanged(EventArgs e)
    {
        base.OnCheckedChanged(e);
        // Out of sight there is nothing to watch: the knob is simply where it belongs.
        if (!Visible || !IsHandleCreated)
        {
            knob = Checked ? 1 : 0;
            return;
        }
        if (slide == null)
        {
            slide = new Timer();
            slide.Interval = SlideMs;
            slide.Tick += delegate
            {
                float goal = Checked ? 1 : 0;
                knob = Math.Abs(goal - knob) <= SlideStep ? goal : knob + Math.Sign(goal - knob) * SlideStep;
                if (knob == goal) slide.Stop();
                Invalidate();
            };
        }
        slide.Start();
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
        Rectangle box = new Rectangle(0, (Height - Side) / 2, Track - 1, Side - 1);
        // The knob travels between its two ends, its colours with it; the focus is shown only to the keyboard.
        float size = box.Height - 2 * KnobInset, grown = size + (hover && Enabled ? 2 : 0);
        float x = box.Left + KnobInset + size / 2 + knob * (box.Width - 2 * KnobInset - size), y = box.Top + box.Height / 2f;
        bool ring = Focused && ShowFocusCues;
        using (GraphicsPath shape = new GraphicsPath())
        using (Brush fill = new SolidBrush(Look.Blend(Look.Well, Look.Accent, knob)))
        using (Pen edge = new Pen(ring ? Look.Ink : Look.Blend(Look.Border, Look.Accent, knob), ring ? 2 : 1))
        using (Brush dot = new SolidBrush(Look.Blend(Look.Muted, Color.White, knob)))
        {
            shape.AddArc(box.Left, box.Top, box.Height, box.Height, 90, 180);
            shape.AddArc(box.Right - box.Height, box.Top, box.Height, box.Height, 270, 180);
            shape.CloseFigure();
            g.FillPath(fill, shape);
            g.DrawPath(edge, shape);
            g.FillEllipse(dot, x - grown / 2, y - grown / 2, grown, grown);
        }
        Rectangle text = Rectangle.FromLTRB(box.Right + 1 + TextGap, 0, Width, Height);
        TextRenderer.DrawText(g, Text, Font, text, ForeColor,
            TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && slide != null) slide.Dispose();
        base.Dispose(disposing);
    }
}

// A panel drawn as a rounded shape a shade off the window, with a thin edge. While what it holds has the
// focus, the edge takes the accent colour; around a text box, as in Windows 11, a line along its bottom does.
class Card : Panel
{
    const int LineWidth = 2;   // of that line, in pixels at 96 dpi

    public Card()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
    }

    public bool Underline { get; set; }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.Clear(Parent.BackColor);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        bool focus = ContainsFocus;
        using (GraphicsPath shape = Look.Rounded(new Rectangle(0, 0, Width - 1, Height - 1)))
        using (Brush fill = new SolidBrush(Look.Well))
        using (Pen edge = new Pen(focus && !Underline ? Look.Accent : Look.Border))
        using (Brush line = new SolidBrush(Look.Accent))
        {
            g.FillPath(fill, shape);
            g.DrawPath(edge, shape);
            if (focus && Underline)
            {
                int width = (int)Math.Round(LineWidth * Look.Zoom);
                g.SetClip(shape);
                g.FillRectangle(line, 0, Height - width, Width, width);
                g.ResetClip();
            }
        }
        base.OnPaint(e);
    }
}

// A button drawn as a rounded shape in its own colours, a little different under the pointer, with a small
// sign before its text if it is given one. The edge of a plain one is darker at the bottom, as if it stood
// out of the window; a ring shows the focus, only to the keyboard.
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
        bool pressed = hover && Capture, coloured = BackColor == Look.Accent;
        Color fill = !Enabled ? Look.Blend(BackColor, Parent.BackColor, 0.6f)
            : pressed ? Look.Blend(BackColor, Look.Ink, 0.16f) : hover ? Look.Blend(BackColor, Look.Ink, 0.07f) : BackColor;
        // A light edge at the top and a darker one at the bottom; on a dark window the light comes from above.
        Color top = !Enabled ? Look.Border : coloured ? FlatAppearance.BorderColor
            : Look.Dark ? Look.Blend(Look.Border, Look.Ink, 0.12f) : Look.Blend(Look.Border, fill, 0.4f);
        Color bottom = !Enabled ? Look.Border : coloured ? FlatAppearance.BorderColor
            : Look.Dark ? Look.Blend(Look.Border, fill, 0.3f) : Look.Blend(Look.Border, Look.Ink, 0.12f);
        Rectangle area = new Rectangle(0, 0, Width - 1, Height - 1);
        using (GraphicsPath shape = Look.Rounded(area))
        using (Brush back = new SolidBrush(fill))
        using (Brush shade = new LinearGradientBrush(new Rectangle(0, -1, Width, Height + 1), top, bottom, 90f))
        using (Pen edge = new Pen(shade))
        {
            g.FillPath(back, shape);
            g.DrawPath(edge, shape);
        }
        if (Focused && ShowFocusCues)
            using (GraphicsPath ring = Look.Rounded(Rectangle.Inflate(area, -1, -1)))
            using (Pen pen = new Pen(coloured ? Color.White : Look.Ink, 2))
                g.DrawPath(pen, ring);
        // Sign and text together are centred. On a coloured button the sign takes the colour of the text.
        const TextFormatFlags flags = TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix
            | TextFormatFlags.NoPadding;
        Color ink = Enabled ? ForeColor : Look.Blend(Look.Muted, Parent.BackColor, 0.5f);
        float scale = (float)Height / DesignHeight;
        int side = sign.HasValue ? (int)(SignSize * scale) : 0;
        int gap = sign.HasValue && Text.Length > 0 ? (int)(SignGap * scale) : 0;   // a sign alone stays in the middle
        int text = TextRenderer.MeasureText(g, Text, Font, Size.Empty, flags).Width, left = (Width - side - gap - text) / 2;
        if (sign.HasValue)
            Look.Draw(g, sign.Value, new Rectangle(left, (Height - side) / 2, side, side),
                !Enabled || coloured ? ink : (Color?)null);
        TextRenderer.DrawText(g, Text, Font, new Rectangle(left + side + gap, 0, text + 2, Height), ink, flags);
    }
}

// The small signs drawn beside entries and on buttons: they are where the colour is.
enum Sign { Text, Link, Star, Plus, Edit, Delete, Options, List, Keys, Search, Help, Opened, Closed }

// The colours the user chose in the options: those Windows sets for apps, or always the same. Saved as a number.
enum Theme { System, Light, Dark }

// Colours and touches shared by the windows: neutral, with rounded shapes and small coloured signs. Light or
// dark as the user chose (Choose), or as Windows is set for its apps, also when that changes while the program
// is open (Follow).
static class Look
{
    const int FramePadding = 6;   // between the edge of a framed text box and its text
    const int Radius = 6;         // of the rounded corners
    const string ModernFont = "Segoe UI Variable Text";   // what Windows 11 writes its own windows in
    // Air above and below a menu, above and below each of its rows and before their text; beside the shape
    // under the pointer.
    const int MenuEdge = 4, MenuRow = 5, MenuText = 8, MenuInset = 4;
    const int TipPadding = 7;     // between the edge of a tooltip and its text
    const TextFormatFlags TipFlags = TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding;
    const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    const string LightSetting = "AppsUseLightTheme";   // 0 when the user picked the dark mode for apps
    public const string ThemeChange = "ImmersiveColorSet";   // what WM_SETTINGCHANGE names when that changes
    const int AppModeBuild = 18362;   // Windows 10 1903, from which SetPreferredAppMode is what it is

    // How much larger than designed this screen draws: the program is aware of the scale of the system.
    public static readonly float Zoom = SystemZoom();

    // The font of every window: the modern one where Windows has it, in the size the user set for message
    // boxes; elsewhere the font of the message boxes itself.
    public static readonly Font TextFont = FindFont();

    static float SystemZoom()
    {
        using (Graphics screen = Graphics.FromHwnd(IntPtr.Zero)) return screen.DpiX / 96f;
    }

    static Font FindFont()
    {
        Font usual = SystemFonts.MessageBoxFont, modern = new Font(ModernFont, usual.SizeInPoints);
        return modern.Name == ModernFont ? modern : usual;   // a font that is not there is replaced without a word
    }

    // The neutral part: a warm grey window, neither too dark nor white, with lighter areas where text is
    // written or listed; in the dark a neutral grey, the areas again a shade lighter, as in Windows 11.
    public static Color Back, Well, Soft, Border, Selected, Ink, Muted;
    // The coloured part: the accent, the same blue in both, and the colours of the signs, brighter in the dark.
    public static Color Accent, Green, Amber, Red, Teal, Purple;
    public static bool Dark { get; private set; }   // what Windows draws (title bar, scroll bars, menus) follows
    public static Theme Chosen { get; private set; }   // in the options; until then, as Windows
    public static event EventHandler Changed;       // after the colours changed with those of Windows

    static Look()
    {
        Use(WindowsDark());
    }

    // Takes the light or the dark colours. Called by itself at the start; the checks call it to take pictures.
    public static void Use(bool dark)
    {
        Dark = dark;
        if (dark)
        {
            Back = Color.FromArgb(32, 32, 32); Well = Color.FromArgb(43, 43, 43); Soft = Color.FromArgb(56, 56, 56);
            Border = Color.FromArgb(68, 68, 68); Selected = Color.FromArgb(42, 56, 86);
            Ink = Color.FromArgb(238, 238, 238); Muted = Color.FromArgb(160, 160, 160);
            Accent = Color.FromArgb(70, 116, 224); Green = Color.FromArgb(72, 190, 132); Amber = Color.FromArgb(236, 168, 58);
            Red = Color.FromArgb(234, 104, 100); Teal = Color.FromArgb(48, 184, 194); Purple = Color.FromArgb(160, 126, 240);
        }
        else
        {
            Back = Color.FromArgb(232, 230, 225); Well = Color.FromArgb(247, 246, 243); Soft = Color.FromArgb(219, 216, 210);
            Border = Color.FromArgb(198, 194, 186); Selected = Color.FromArgb(214, 224, 244);
            Ink = Color.FromArgb(38, 40, 46); Muted = Color.FromArgb(108, 110, 116);
            Accent = Color.FromArgb(62, 108, 214); Green = Color.FromArgb(40, 158, 104); Amber = Color.FromArgb(222, 148, 30);
            Red = Color.FromArgb(214, 76, 72); Teal = Color.FromArgb(22, 152, 164); Purple = Color.FromArgb(132, 92, 220);
        }
        // The menus Windows draws itself, such as the one of a text box, in the same mode.
        if (WindowsBuild() < AppModeBuild) return;
        try
        {
            Native.SetPreferredAppMode(dark ? Native.ForceDark : Native.ForceLight);
            Native.FlushMenuThemes();
        }
        catch (EntryPointNotFoundException)
        {
        }
    }

    // Whether Windows wants its apps dark. Without the setting (older versions) they are light.
    static bool WindowsDark()
    {
        try
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(PersonalizeKey))
            {
                object light = key == null ? null : key.GetValue(LightSetting);
                return light is int && (int)light == 0;
            }
        }
        catch (Exception)
        {
            return false;
        }
    }

    static int WindowsBuild()
    {
        try
        {
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
            {
                int build;
                return key != null && int.TryParse(key.GetValue("CurrentBuildNumber") as string, out build) ? build : 0;
            }
        }
        catch (Exception)
        {
            return 0;
        }
    }

    // Every colour of the palette, always in the same order: the old and the new are matched by place.
    static Color[] Palette
    {
        get { return new Color[] { Back, Well, Soft, Border, Selected, Ink, Muted, Accent, Green, Amber, Red, Teal, Purple }; }
    }

    // The theme chosen in the options: before the first window, from the settings, and then at once.
    public static void Choose(Theme theme)
    {
        Chosen = theme;
        Switch(theme == Theme.Dark || theme == Theme.System && WindowsDark());
    }

    // Windows went from light to dark or back: if the user leaves the choice to it, so does the program.
    public static void Follow()
    {
        if (Chosen == Theme.System) Switch(WindowsDark());
    }

    // The open windows take the light or the dark colours at once. A colour given to a control is one of the
    // palette, so the old one says which new one it becomes; what is drawn by hand reads the palette as it
    // paints. Those with their own needs listen to Changed. The checks call it directly, whatever Windows says.
    public static void Switch(bool dark)
    {
        if (dark == Dark) return;
        Color[] before = Palette;
        Use(dark);
        Color[] after = Palette;
        foreach (Form form in Application.OpenForms)
        {
            Recolor(form, before, after);
            if (form.IsHandleCreated)
            {
                if (form.FormBorderStyle == FormBorderStyle.None) Round(form);
                else Caption(form);
            }
            form.Invalidate(true);
        }
        if (Changed != null) Changed(null, EventArgs.Empty);
    }

    static void Recolor(Control control, Color[] before, Color[] after)
    {
        // A colour the control inherits reads as that of its parent, already new: found nowhere, it stays inherited.
        Color back = Swap(control.BackColor, before, after), fore = Swap(control.ForeColor, before, after);
        if (back.ToArgb() != control.BackColor.ToArgb()) control.BackColor = back;
        if (fore.ToArgb() != control.ForeColor.ToArgb()) control.ForeColor = fore;
        Button button = control as Button;
        if (button != null) button.FlatAppearance.BorderColor = Swap(button.FlatAppearance.BorderColor, before, after);
        LinkLabel link = control as LinkLabel;
        if (link != null)
        {
            link.LinkColor = Swap(link.LinkColor, before, after);
            link.ActiveLinkColor = Swap(link.ActiveLinkColor, before, after);
        }
        if ((control is TextBoxBase || control is ListBox) && control.IsHandleCreated) Tint(control);
        foreach (Control child in control.Controls) Recolor(child, before, after);
    }

    static Color Swap(Color colour, Color[] before, Color[] after)
    {
        for (int i = 0; i < before.Length; i++)
            if (before[i].ToArgb() == colour.ToArgb()) return after[i];
        return colour;
    }

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
        field.HandleCreated += delegate { Tint(field); };
    }

    static void Tint(Control field)
    {
        Native.SetWindowTheme(field.Handle, Dark ? "DarkMode_Explorer" : "Explorer", null);
    }

    // Draws one of the small signs in its own colour, or in the given one, inside a square box. Each is laid
    // out on a grid of 16.
    public static void Draw(Graphics g, Sign sign, Rectangle box, Color? colour)
    {
        float u = box.Width / 16f, x = box.Left, y = box.Top;
        Color own = sign == Sign.Text || sign == Sign.Search || sign == Sign.Keys ? Accent
            : sign == Sign.Link || sign == Sign.Opened || sign == Sign.Closed ? Teal
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
                case Sign.Opened:   // of a folder: an arrowhead pointing down, or to the right when it is closed
                    g.DrawLines(pen, new PointF[] { new PointF(x + 4 * u, y + 6 * u), new PointF(x + 8 * u, y + 10 * u),
                        new PointF(x + 12 * u, y + 6 * u) });
                    break;
                case Sign.Closed:
                    g.DrawLines(pen, new PointF[] { new PointF(x + 6 * u, y + 4 * u), new PointF(x + 10 * u, y + 8 * u),
                        new PointF(x + 6 * u, y + 12 * u) });
                    break;
                case Sign.Help:   // a question mark
                    g.DrawArc(pen, x + 5 * u, y + 2 * u, 6 * u, 6 * u, 180, 270);
                    g.DrawLine(pen, x + 8 * u, y + 8 * u, x + 8 * u, y + 10 * u);
                    g.FillEllipse(fill, x + 6.9f * u, y + 12.2f * u, 2.2f * u, 2.2f * u);
                    break;
            }
        }
        g.SmoothingMode = mode;
    }

    // A text box as a flat area a shade off the window. The system border cannot be coloured, so the box goes
    // borderless inside a card that takes its place; the card is what gets added to the window.
    public static Panel Frame(TextBox box)
    {
        box.BorderStyle = BorderStyle.None;
        Field(box);
        int textHeight = box.Height;
        Card frame = Wrap(box, FramePadding);
        frame.Underline = true;
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
    public static Card Wrap(Control inner, int padding)
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

    // A menu as Windows 11 draws its own: rounded, roomy rows, a rounded shape under the pointer. Its colours
    // are read as it paints, so it follows the light and the dark by itself.
    public static void Style(ToolStripDropDownMenu menu)
    {
        menu.Renderer = new MenuLook();
        menu.Font = TextFont;
        menu.ShowImageMargin = false;
        int edge = (int)(MenuEdge * Zoom);
        menu.Padding = new Padding(menu.Padding.Left, edge, menu.Padding.Right, edge);
        foreach (ToolStripItem item in menu.Items) Space(item);
        menu.ItemAdded += delegate(object sender, ToolStripItemEventArgs e) { Space(e.Item); };
        // Windows rounds it once it exists, with an edge in the colour of the moment.
        menu.HandleCreated += delegate { Corners(menu); };
        menu.Opening += delegate { if (menu.IsHandleCreated) Corners(menu); };
    }

    static void Space(ToolStripItem item)
    {
        int row = (int)(MenuRow * Zoom);
        if (!(item is ToolStripSeparator)) item.Padding = new Padding((int)(MenuText * Zoom), row, item.Padding.Right, row);
    }

    static bool roundedMenus;   // Windows rounds them and draws their edge (Windows 11)

    static void Corners(ToolStripDropDownMenu menu)
    {
        int round = Native.DWMWCP_ROUNDSMALL, edge = ColorTranslator.ToWin32(Border);
        roundedMenus = Native.DwmSetWindowAttribute(menu.Handle, Native.DWMWA_WINDOW_CORNER_PREFERENCE, ref round, sizeof(int)) == 0
            && Native.DwmSetWindowAttribute(menu.Handle, Native.DWMWA_BORDER_COLOR, ref edge, sizeof(int)) == 0;
    }

    class MenuLook : ToolStripProfessionalRenderer
    {
        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            e.Graphics.Clear(Well);
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            if (roundedMenus) return;
            using (Pen edge = new Pen(Border)) e.Graphics.DrawRectangle(edge, 0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
        }

        protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
        {
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (!e.Item.Enabled || !e.Item.Selected && !e.Item.Pressed) return;
            int inset = (int)(MenuInset * Zoom);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath shape = Rounded(new Rectangle(inset, 0, e.Item.Width - 2 * inset - 1, e.Item.Height - 1)))
            using (Brush fill = new SolidBrush(Soft))
                e.Graphics.FillPath(fill, shape);
        }

        // The menu leaves the room asked before the text (Space) at its end: the text moves into it.
        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            Rectangle area = e.TextRectangle;
            area.Offset((int)(MenuText * Zoom), 0);
            TextRenderer.DrawText(e.Graphics, e.Text, e.TextFont, area, e.Item.Enabled ? Ink : Blend(Muted, Well, 0.35f), e.TextFormat);
        }

        // The arrow of a submenu as the sign of a closed folder.
        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
        {
            int side = TextFont.Height * 3 / 4;
            Rectangle area = e.ArrowRectangle;
            Draw(e.Graphics, Sign.Closed, new Rectangle(area.Left + (area.Width - side) / 2, area.Top + (area.Height - side) / 2,
                side, side), e.Item.Enabled ? Muted : Blend(Muted, Well, 0.35f));
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            int inset = (int)(MenuInset * Zoom), y = e.Item.Height / 2;
            using (Pen line = new Pen(Border)) e.Graphics.DrawLine(line, inset, y, e.Item.Width - inset - 1, y);
        }
    }

    // Tooltips drawn in the colours of the window, with some air around their text.
    public static void Style(ToolTip tips)
    {
        tips.OwnerDraw = true;
        int padding = (int)(TipPadding * Zoom);
        tips.Popup += delegate(object sender, PopupEventArgs e)
        {
            Size text = TextRenderer.MeasureText(tips.GetToolTip(e.AssociatedControl), TextFont, Size.Empty, TipFlags);
            e.ToolTipSize = new Size(text.Width + 2 * padding, text.Height + 2 * padding);
        };
        tips.Draw += delegate(object sender, DrawToolTipEventArgs e)
        {
            using (Brush back = new SolidBrush(Well)) e.Graphics.FillRectangle(back, e.Bounds);
            using (Pen edge = new Pen(Border)) e.Graphics.DrawRectangle(edge, 0, 0, e.Bounds.Width - 1, e.Bounds.Height - 1);
            TextRenderer.DrawText(e.Graphics, e.ToolTipText, TextFont, Rectangle.Inflate(e.Bounds, -padding, -padding), Ink, TipFlags);
        };
    }

    // Title bar in the colour of the window (Windows 11; earlier versions refuse and keep their own).
    public static void Caption(Form form)
    {
        int back = ColorTranslator.ToWin32(form.BackColor), text = ColorTranslator.ToWin32(form.ForeColor), dark = Dark ? 1 : 0;
        Native.DwmSetWindowAttribute(form.Handle, Native.DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, sizeof(int));
        Native.DwmSetWindowAttribute(form.Handle, Native.DWMWA_CAPTION_COLOR, ref back, sizeof(int));
        Native.DwmSetWindowAttribute(form.Handle, Native.DWMWA_TEXT_COLOR, ref text, sizeof(int));
    }

    // Rounded corners and a thin edge for a window without a frame (Windows 11). Returns false where Windows
    // refuses: the window then draws an edge of its own.
    public static bool Round(Form form)
    {
        int round = Native.DWMWCP_ROUND, edge = ColorTranslator.ToWin32(Border);
        return Native.DwmSetWindowAttribute(form.Handle, Native.DWMWA_WINDOW_CORNER_PREFERENCE, ref round, sizeof(int)) == 0
            && Native.DwmSetWindowAttribute(form.Handle, Native.DWMWA_BORDER_COLOR, ref edge, sizeof(int)) == 0;
    }
}
