using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Microsoft.Win32;

// What every window of the program has in common: the font and the colours of the interface, the title bar in
// the colour of the window. Built as designed, then made larger at the end (Finish) by Look.Zoom, the scale of
// the screen and of the text: left to Windows Forms, the controls that inherit the font would be scaled twice.
class Window : Form
{
    public const int ButtonHeight = 26;    // of a button, in pixels at 96 dpi
    protected const int ButtonWidth = 90;

    protected Window()
    {
        SuspendLayout();
        AutoScaleMode = AutoScaleMode.None;
        Font = Look.TextFont;
        BackColor = Look.Back;
        ForeColor = Look.Ink;
    }

    // Called at the end of the constructor, once everything is placed as designed.
    protected void Finish()
    {
        Scale(new SizeF(Look.Zoom, Look.Zoom));
        ResumeLayout(false);
        PerformLayout();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        Look.Caption(this);
    }

    protected static Label NewLabel(string text, int left, int top)
    {
        Label label = new Label();
        label.Text = text;
        label.AutoSize = true;
        label.Location = new Point(left, top);
        return label;
    }

    // Moves the window as near as it can to 'wanted' without leaving the working area of 'screen'.
    protected void PlaceOnScreen(Point wanted, Screen screen)
    {
        Rectangle area = screen.WorkingArea;
        Location = new Point(Math.Max(area.Left, Math.Min(wanted.X, area.Right - Width)),
            Math.Max(area.Top, Math.Min(wanted.Y, area.Bottom - Height)));
    }
}

// A checkbox drawn as a switch in the colours of the window: the system one cannot be coloured. The knob is
// on the right, on the accent colour, while it is on; it slides there when it changes, and grows a little
// under the pointer, as the switches of Windows 11 do.
class Toggle : CheckBox
{
    const int TextGap = 8;        // between the switch and its text, in pixels at 96 dpi
    const float Stretch = 1.8f;   // how many times its height the switch is wide
    const int KnobInset = 3;      // between the edge of the switch and its knob
    readonly Fade knob, lit;      // where the knob is, 0 on the left and 1 on the right; how much the pointer lights it

    public Toggle()
    {
        knob = new Fade(this);
        lit = new Fade(this);
    }

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
        knob.To(Checked ? 1 : 0);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        lit.To(1);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        lit.To(0);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.Clear(Parent.BackColor);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        Rectangle box = new Rectangle(0, (Height - Side) / 2, Track - 1, Side - 1);
        // The knob travels between its two ends, its colours with it; the focus is shown only to the keyboard.
        float on = knob.Value, size = box.Height - 2 * KnobInset, grown = size + (Enabled ? 2 * lit.Value : 0);
        float x = box.Left + KnobInset + size / 2 + on * (box.Width - 2 * KnobInset - size), y = box.Top + box.Height / 2f;
        bool ring = Focused && ShowFocusCues;
        using (GraphicsPath shape = Look.Pill(box))
        using (Brush fill = new SolidBrush(Look.Blend(Look.Well, Look.Accent, on)))
        using (Pen edge = new Pen(ring ? Look.Ink : Look.Blend(Look.Border, Look.Accent, on), ring ? 2 : 1))
        using (Brush dot = new SolidBrush(Look.Blend(Look.Muted, Color.White, on)))
        {
            g.FillPath(fill, shape);
            g.DrawPath(edge, shape);
            g.FillEllipse(dot, x - grown / 2, y - grown / 2, grown, grown);
        }
        Rectangle text = Rectangle.FromLTRB(box.Right + 1 + TextGap, 0, Width, Height);
        TextRenderer.DrawText(g, Text, Font, text, ForeColor,
            TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
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
    bool hover;
    readonly Fade lit;   // the pointer brightens it gradually, and the brightness fades when it leaves
    Sign? sign;

    public RoundButton()
    {
        lit = new Fade(this);
    }

    public Sign? Sign
    {
        get { return sign; }
        set { sign = value; Invalidate(); }
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        hover = true;
        lit.To(1);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        hover = false;
        lit.To(0);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.Clear(Parent.BackColor);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        bool pressed = hover && Capture, coloured = BackColor == Look.Accent;
        Color fill = !Enabled ? Look.Blend(BackColor, Parent.BackColor, 0.6f)
            : pressed ? Look.Blend(BackColor, Look.Ink, 0.16f) : Look.Blend(BackColor, Look.Ink, 0.07f * lit.Value);
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
        float scale = (float)Height / Window.ButtonHeight;
        int side = sign.HasValue ? (int)(SignSize * scale) : 0;
        int gap = sign.HasValue && Text.Length > 0 ? (int)(SignGap * scale) : 0;   // a sign alone stays in the middle
        int text = TextRenderer.MeasureText(g, Text, Font, Size.Empty, flags).Width, left = (Width - side - gap - text) / 2;
        if (sign.HasValue)
            Look.Draw(g, sign.Value, new Rectangle(left, (Height - side) / 2, side, side),
                !Enabled || coloured ? ink : (Color?)null);
        TextRenderer.DrawText(g, Text, Font, new Rectangle(left + side + gap, 0, text + 2, Height), ink, flags);
    }
}

// The small signs drawn beside entries, on buttons and beside the pages of the options: they are where the colour is.
enum Sign { Text, Link, Star, Plus, Edit, Delete, Options, Keys, Search, Help, Opened, Closed, Bolt, History, Up }

// The colours the user chose in the options: those Windows sets for apps, or always the same. Saved as a number.
enum Theme { System, Light, Dark }

// Colours and touches shared by the windows: neutral, with rounded shapes and small coloured signs. Light or
// dark as the user chose (Choose), or as Windows is set for its apps, also when that changes while the program
// is open (Follow).
static class Look
{
    const int FramePadding = 6;   // between the edge of a framed text box and its text
    const int Radius = 6;         // of the rounded corners
    const float MarkShare = 0.45f;   // how much of the height of a selected row its mark takes
    const float StripeShare = 0.5f; // how far every other row of the copied entries goes towards Soft
    const string ModernFont = "Segoe UI Variable Text";   // what Windows 11 writes its own windows in
    // Its heavier face, for what stands out in a list, and the one of the titles, larger: Windows writes the name
    // of that one cut at 31 letters. Where they are missing (Windows 10), the semibold face of the family.
    const string StrongName = "Segoe UI Variable Text Semibold", TitleName = "Segoe UI Variable Display Semib";
    const float TitleGrowth = 1.45f;   // a title is this much larger than the text, as in Windows 11
    // The signs are the icons of Windows itself, from its font of icons: that of Windows 11, or the older one of
    // Windows 10. Without either they are drawn by hand.
    const string IconName = "Segoe Fluent Icons", OldIconName = "Segoe MDL2 Assets";
    // Air above and below a menu, above and below each of its rows and before their text; beside the shape
    // under the pointer.
    const int MenuEdge = 4, MenuRow = 5, MenuText = 8, MenuInset = 4;
    const int TipPadding = 7;     // between the edge of a tooltip and its text
    const TextFormatFlags TipFlags = TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding;
    const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    const string LightSetting = "AppsUseLightTheme";   // 0 when the user picked the dark mode for apps
    public const string ThemeChange = "ImmersiveColorSet";   // what WM_SETTINGCHANGE names when that changes
    const int AppModeBuild = 18362;   // Windows 10 1903, from which SetPreferredAppMode is what it is

    static readonly float ScreenZoom = SystemZoom();   // the program is aware of the scale of the system

    // How much larger than designed the windows are drawn: the scale of the screen times that of the text of
    // the interface.
    public static float Zoom
    {
        get { return ScreenZoom * InterfaceSize / DesignedSize; }
    }
    static readonly Font IconProbe = Installed(IconName, 10) ?? Installed(OldIconName, 10);   // null: no font of icons
    static readonly Dictionary<int, Font> IconFonts = new Dictionary<int, Font>();   // by size in pixels

    // The font of the interface, the same in every window: buttons, labels, options, the guide, menus, questions.
    // The user chooses it among those of Families that Windows has, with its size in pixels at 96 dpi; the first
    // family Windows has is the default. Set before the first window by UseInterfaceFont: the windows are built
    // for it, so another one takes a new start. Its heavier face, for what stands out, and the larger one of
    // titles; where Segoe UI Variable is missing (Windows 10), the semibold face of the family.
    public static readonly int[] InterfaceSizes = { 12, 13, 14, 15, 16 };
    public const int DefaultInterfaceSize = 13;
    const int DesignedSize = 12;   // the windows were designed for the text of Windows message boxes, 12 pixels
    public static Font TextFont { get; private set; }
    public static Font StrongFont { get; private set; }
    public static Font TitleFont { get; private set; }
    public static string InterfaceFamily { get; private set; }   // one of FontNames
    public static int InterfaceSize { get; private set; }        // one of InterfaceSizes

    // The entries of the lists, the preview of one and the content of a saved text have a font of their own,
    // chosen in the same way; it changes at once.
    static readonly string[] Families = { ModernFont, "Segoe UI", "Aptos", "Calibri", "Arial", "Verdana", "Tahoma",
        "Cascadia Mono", "Consolas" };
    public static readonly int[] EntrySizes = { 11, 12, 13, 14, 15, 16, 18, 20 };
    public const int DefaultEntrySize = 14;   // the text of Windows 11: a little larger than that of the windows
    const float CaptionShare = 0.8f;          // of the size of the entries, the small line under each
    public static Font EntryFont { get; private set; }
    public static Font StrongEntryFont { get; private set; }   // for a folder among the entries
    public static Font CaptionFont { get; private set; }       // for when an entry was copied or saved, under it
    public static string EntryFamily { get; private set; }     // one of FontNames
    public static int EntrySize { get; private set; }          // one of EntrySizes

    // The fonts that can be chosen: those of Families that Windows has. Looked for once: each try makes a font.
    public static readonly string[] FontNames = Array.FindAll(Families, Has);

    // A font as the options name it: Segoe UI Variable without the name of its face for text.
    public static string FontLabel(string name)
    {
        return name == ModernFont ? name.Substring(0, name.LastIndexOf(' ')) : name;
    }

    static bool Has(string name)
    {
        Font font = Installed(name, 10);
        if (font != null) font.Dispose();
        return font != null;
    }

    // A family that can be chosen, or the default one: the first Windows has of Families, or else that of its
    // message boxes.
    static string Family(string name)
    {
        string[] names = FontNames;
        return Array.IndexOf(names, name) >= 0 ? name : names.Length > 0 ? names[0] : SystemFonts.MessageBoxFont.Name;
    }

    // Takes the font and the size of the interface, before any window: what is not known or not installed falls
    // back to the defaults.
    public static void UseInterfaceFont(string name, int size)
    {
        name = Family(name);
        if (Array.IndexOf(InterfaceSizes, size) < 0) size = DefaultInterfaceSize;
        InterfaceFamily = name;
        InterfaceSize = size;
        float points = Points(size);
        TextFont = new Font(name, points);
        StrongFont = Heavy(name, points, false);
        TitleFont = Heavy(name, points * TitleGrowth, true);
    }

    // Takes the font and the size of the entries: what is not known or not installed falls back to the defaults.
    public static void UseEntryFont(string name, int size)
    {
        name = Family(name);
        if (Array.IndexOf(EntrySizes, size) < 0) size = DefaultEntrySize;
        EntryFamily = name;
        EntrySize = size;
        float points = Points(size);
        EntryFont = new Font(name, points);
        StrongEntryFont = Heavy(name, points, false);
        CaptionFont = new Font(name, Points((int)Math.Round(size * CaptionShare)));
    }

    // The size of a font in points, from pixels at 96 dpi: Windows makes it larger with the screen.
    static float Points(int pixels)
    {
        return pixels * 72f / 96;
    }

    // The heavier face of a family: those of Segoe UI Variable (the Display one for titles), the semibold face
    // of the family if it has one, or else its bold.
    static Font Heavy(string family, float points, bool title)
    {
        Font found = family == ModernFont ? Installed(title ? TitleName : StrongName, points)
            : Installed(family + " Semibold", points);
        return found ?? new Font(family, points, FontStyle.Bold);
    }

    // A font by name, if Windows has it; otherwise null.
    static Font Installed(string name, float size)
    {
        Font font = new Font(name, size);
        if (font.Name == name) return font;   // a font that is not there is replaced without a word
        font.Dispose();
        return null;
    }

    static float SystemZoom()
    {
        using (Graphics screen = Graphics.FromHwnd(IntPtr.Zero)) return screen.DpiX / 96f;
    }

    // The neutral part: a warm grey window, neither too dark nor white, with lighter areas where text is
    // written or listed; in the dark a neutral grey, the areas again a shade lighter, as in Windows 11.
    public static Color Back, Well, Soft, Border, Selected, Ink, Muted;
    // The coloured part: the accent, the same blue in both, and the colours of the signs, brighter in the dark.
    public static Color Accent, Green, Amber, Red, Teal, Purple;
    public static bool Dark { get; private set; }   // what Windows draws (title bar, scroll bars, menus) follows
    public static Theme Chosen { get; private set; }   // in the options; until then, as Windows
    public static event EventHandler Changed;       // after the colours changed with those of Windows
    // Windows made to be shown later (the options): Windows Forms counts as open only those shown once.
    static readonly List<Form> kept = new List<Form>();

    static Look()
    {
        UseInterfaceFont(null, DefaultInterfaceSize);   // until the settings are read
        UseEntryFont(null, DefaultEntrySize);
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

    // A window that changes with the theme even while it has never been shown, until it is disposed.
    public static void Keep(Form form)
    {
        kept.Add(form);
        form.Disposed += delegate { kept.Remove(form); };
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
        List<Form> forms = new List<Form>(kept);
        foreach (Form form in Application.OpenForms)
            if (!forms.Contains(form)) forms.Add(form);
        foreach (Form form in forms)
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

    // A shape with round ends: lying if it is wider than tall, standing otherwise.
    public static GraphicsPath Pill(RectangleF area)
    {
        GraphicsPath path = new GraphicsPath();
        if (area.Width >= area.Height)
        {
            float side = area.Height;
            path.AddArc(area.Left, area.Top, side, side, 90, 180);
            path.AddArc(area.Right - side, area.Top, side, side, 270, 180);
        }
        else
        {
            float side = area.Width;
            path.AddArc(area.Left, area.Top, side, side, 180, 180);
            path.AddArc(area.Left, area.Bottom - side, side, side, 0, 180);
        }
        path.CloseFigure();
        return path;
    }

    // The background of a row of a list, as Windows 11 draws it: the selected one a rounded shape with a short
    // mark in the accent colour on its left, the one under the pointer a fainter shape, which comes and goes
    // with 'glow' (see Glow).
    public static void DrawRow(Graphics g, Rectangle bounds, Color back, bool selected, float glow)
    {
        using (Brush plain = new SolidBrush(back)) g.FillRectangle(plain, bounds);
        if (!selected && glow <= 0) return;
        Rectangle row = Rectangle.FromLTRB(bounds.Left + 1, bounds.Top + 1, bounds.Right - 2, bounds.Bottom - 1);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using (GraphicsPath shape = Rounded(row))
        using (Brush fill = new SolidBrush(selected ? Selected : Blend(back, Soft, glow)))
            g.FillPath(fill, shape);
        if (!selected) return;
        float height = row.Height * MarkShare;
        using (GraphicsPath mark = Pill(new RectangleF(row.Left + 3, row.Top + (row.Height - height) / 2, 3 * Zoom, height)))
        using (Brush accent = new SolidBrush(Accent))
            g.FillPath(accent, mark);
    }

    // Every other row of a long list, a shade apart, so the eye keeps to one row across it. Worked out while drawing,
    // not a colour of the palette: in the dark it is a shade lighter.
    public static Color Stripe(Color back)
    {
        return Blend(back, Soft, StripeShare);
    }

    // A subject in a column of subjects (the guide, the pages of the options): its sign if it has one, then
    // its name, on a row drawn as DrawRow does. 'inset' is where the sign or the name starts.
    public static void DrawTopic(DrawItemEventArgs e, RowList list, string name, Sign? sign, int inset)
    {
        if (e.Index < 0) return;
        DrawRow(e.Graphics, e.Bounds, list.BackColor, (e.State & DrawItemState.Selected) != 0, list.Glow.Of(e.Index));
        int left = e.Bounds.Left + inset, side = e.Font.Height;
        if (sign.HasValue)
        {
            Draw(e.Graphics, sign.Value, new Rectangle(left, e.Bounds.Top + (e.Bounds.Height - side) / 2, side, side), null);
            left += side + inset;
        }
        TextRenderer.DrawText(e.Graphics, name, e.Font, Rectangle.FromLTRB(left, e.Bounds.Top, e.Bounds.Right - inset / 2,
            e.Bounds.Bottom), Ink, TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix
            | TextFormatFlags.EndEllipsis);
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

    // The icon of each sign in the fonts of icons of Windows, which share them.
    static string Glyph(Sign sign)
    {
        switch (sign)
        {
            case Sign.Text: return "";      // Document
            case Sign.Link: return "";      // Link
            case Sign.Star: return "";      // FavoriteStarFill
            case Sign.Plus: return "";      // Add
            case Sign.Edit: return "";      // Edit
            case Sign.Delete: return "";    // Delete
            case Sign.Options: return "";   // Settings
            case Sign.Keys: return "";      // KeyboardClassic
            case Sign.Search: return "";    // Search
            case Sign.Help: return "";      // Help
            case Sign.Opened: return "";    // ChevronDown
            case Sign.Closed: return "";    // ChevronRight
            case Sign.Bolt: return "";      // LightningBolt
            case Sign.Up: return "";   // Up
            default: return "";             // History
        }
    }

    // Draws one of the small signs in its own colour, or in the given one, inside a square box: the icon of
    // Windows where there is one, otherwise drawn by hand on a grid of 16.
    public static void Draw(Graphics g, Sign sign, Rectangle box, Color? colour)
    {
        float u = box.Width / 16f, x = box.Left, y = box.Top;
        Color own = sign == Sign.Text || sign == Sign.Search || sign == Sign.Keys || sign == Sign.Up ? Accent
            : sign == Sign.Link || sign == Sign.Opened || sign == Sign.Closed || sign == Sign.History ? Teal
            : sign == Sign.Star || sign == Sign.Edit || sign == Sign.Bolt ? Amber : sign == Sign.Plus ? Green
            : sign == Sign.Delete ? Red : Purple;
        if (IconProbe != null)
        {
            Font icons;
            if (!IconFonts.TryGetValue(box.Height, out icons))
                IconFonts[box.Height] = icons = new Font(IconProbe.FontFamily, box.Height, GraphicsUnit.Pixel);
            TextRenderer.DrawText(g, Glyph(sign), icons, box, colour ?? own, TextFormatFlags.HorizontalCenter
                | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding
                | TextFormatFlags.NoPrefix | TextFormatFlags.NoClipping);
            return;
        }
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
                    g.DrawLine(pen, x + 2.5f * u, y + 4 * u, x + 13.5f * u, y + 4 * u);
                    g.DrawLine(pen, x + 2.5f * u, y + 8 * u, x + 13.5f * u, y + 8 * u);
                    g.DrawLine(pen, x + 2.5f * u, y + 12 * u, x + 9 * u, y + 12 * u);
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
                case Sign.Bolt:
                    g.DrawLines(pen, new PointF[] { new PointF(x + 9.5f * u, y + 2 * u), new PointF(x + 5 * u, y + 9 * u),
                        new PointF(x + 11 * u, y + 7.5f * u), new PointF(x + 6.5f * u, y + 14 * u) });
                    break;
                case Sign.Up:   // an arrow pointing up
                    g.DrawLine(pen, x + 8 * u, y + 13 * u, x + 8 * u, y + 3.5f * u);
                    g.DrawLines(pen, new PointF[] { new PointF(x + 3.5f * u, y + 8 * u), new PointF(x + 8 * u, y + 3.5f * u),
                        new PointF(x + 12.5f * u, y + 8 * u) });
                    break;
                case Sign.History:   // a clock
                    g.DrawEllipse(pen, x + 2 * u, y + 2 * u, 12 * u, 12 * u);
                    g.DrawLines(pen, new PointF[] { new PointF(x + 8 * u, y + 4.5f * u), new PointF(x + 8 * u, y + 8 * u),
                        new PointF(x + 10.5f * u, y + 9.5f * u) });
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
        roundedMenus = Round(menu.Handle, Native.DWMWCP_ROUNDSMALL);
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

        // The menu leaves the room asked before the text (Space) at its end: the text moves into it, and what
        // is written on the right of an entry (its keys) along with it, quieter.
        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            Rectangle area = e.TextRectangle;
            area.Offset((int)(MenuText * Zoom), 0);
            TextRenderer.DrawText(e.Graphics, e.Text, e.TextFont, area,
                !e.Item.Enabled ? Blend(Muted, Well, 0.35f) : e.Text != e.Item.Text ? Muted : Ink, e.TextFormat);
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
        return Round(form.Handle, Native.DWMWCP_ROUND);
    }

    // The same for a window by its handle, with corners as round as 'corners' asks.
    static bool Round(IntPtr window, int corners)
    {
        int edge = ColorTranslator.ToWin32(Border);
        return Native.DwmSetWindowAttribute(window, Native.DWMWA_WINDOW_CORNER_PREFERENCE, ref corners, sizeof(int)) == 0
            && Native.DwmSetWindowAttribute(window, Native.DWMWA_BORDER_COLOR, ref edge, sizeof(int)) == 0;
    }
}
