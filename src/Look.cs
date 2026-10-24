using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.CompilerServices;
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

    // A window that belongs to another (a question, the guide, the options, the window of an entry): no button
    // on the taskbar, no icon, nothing to make it larger or smaller.
    protected void Secondary(FormStartPosition start)
    {
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ShowIcon = false;
        StartPosition = start;
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

// A checkbox drawn as a switch in the colours of the window: the system one cannot be coloured. Off, a grey track
// with a light knob on the left; on, the track takes the accent colour and the knob, on the right, that of what is
// written on it. The knob slides when it changes and grows a little under the pointer. On a card of the options
// (Alone) it is the switch alone: its text is then only its name, for those who cannot see it.
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

    public bool Alone { get; set; }

    int Side { get { return Font.Height; } }
    int Track { get { return (int)(Side * Stretch); } }

    public override Size GetPreferredSize(Size proposed)
    {
        if (Alone) return new Size(Track + 6, Side + 6);   // room for the ring of the focus around it
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
        Rectangle box = new Rectangle(Alone ? Width - Track - 3 : 0, (Height - Side) / 2, Track - 1, Side - 1);
        // The knob travels between its two ends, its colours with it; the focus is shown only to the keyboard.
        float on = knob.Value, size = box.Height - 2 * KnobInset, grown = size + (Enabled ? 2 * lit.Value : 0);
        float x = box.Left + KnobInset + size / 2 + on * (box.Width - 2 * KnobInset - size), y = box.Top + box.Height / 2f;
        // Off, the track a mid grey, the knob light; in the dark the track is only an edge's grey.
        Color track = Look.Dark ? Look.Border : Look.Blend(Look.Border, Look.Muted, 0.7f);
        Color light = Look.Dark ? Look.Blend(Look.Muted, Look.Ink, 0.5f) : Look.Raised;
        Color fill = Look.Blend(track, Look.Accent, on);
        if (!Enabled) fill = Look.Blend(fill, Parent.BackColor, 0.5f);
        using (GraphicsPath shape = Look.Pill(box))
        using (Brush back = new SolidBrush(fill))
        using (Brush dot = new SolidBrush(Look.Blend(light, Look.OnAccent, on)))
        {
            g.FillPath(back, shape);
            g.FillEllipse(dot, x - grown / 2, y - grown / 2, grown, grown);
            if (Focused && ShowFocusCues)
                using (GraphicsPath ring = Look.Pill(RectangleF.Inflate(box, 2, 2)))
                using (Pen pen = new Pen(Look.Accent, 1.5f))
                    g.DrawPath(pen, ring);
        }
        if (Alone) return;
        TextRenderer.DrawText(g, Text, Font, Rectangle.FromLTRB(box.Right + 1 + TextGap, 0, Width, Height), ForeColor,
            TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
    }
}

// What a message is: plain information, something done, a warning, an error.
enum Notice { Info, Done, Warning, Error }

// A message as Windows 11 shows one inside its windows (InfoBar): a rounded bar tinted with the colour of its kind,
// with the sign of that kind before the text. Empty, it shows nothing. The kind goes with the text: Tell gives both,
// a text given alone is information. After a tab (Apart) a text can say what to do next, at the right end.
class InfoBar : Label
{
    public const char Apart = '\t';     // between what happened and what to do next
    const int Inset = 10, SignGap = 8;   // around its text, and between the sign and the text, in pixels at 96 dpi
    const int HintGap = 24;              // at least, between the two parts on one line
    const float Tint = 0.16f;            // how far the bar goes from the window towards the colour of its kind
    const TextFormatFlags Flags = TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix;
    Notice next;                          // the kind of the text about to be given

    public InfoBar()
    {
        UseMnemonic = false;
        DoubleBuffered = true;
        ResizeRedraw = true;
    }

    public Notice Kind { get; private set; }

    public override string Text
    {
        get { return base.Text; }
        set
        {
            Kind = next;
            next = Notice.Info;
            base.Text = value;
            Invalidate();
        }
    }

    public void Tell(string text, Notice kind)
    {
        next = kind;
        Text = text;
    }

    int Side { get { return Font.Height; } }
    int TextLeft { get { return (int)(Inset * Look.Zoom) + Side + (int)(SignGap * Look.Zoom); } }

    // How tall it is with its text written in its width: the lines, and air above and below them.
    public int Fit(int width)
    {
        if (Text.Length == 0) return 0;
        string said, hint;
        Rectangle first, second;
        Place(width - TextLeft - (int)(Inset * Look.Zoom), out said, out hint, out first, out second);
        return Math.Max(Math.Max(first.Bottom, second.Bottom), Side) + 2 * (int)(Inset * Look.Zoom * 0.6f);
    }

    // Where the text goes in 'width', from the top of its first line: what happened from the left; what to do next
    // at the right end, on the same line when both fit there with air between, otherwise on lines of its own under it.
    void Place(int width, out string said, out string hint, out Rectangle first, out Rectangle second)
    {
        int tab = Text.LastIndexOf(Apart);
        said = tab < 0 ? Text : Text.Substring(0, tab);
        hint = tab < 0 ? "" : Text.Substring(tab + 1);
        first = new Rectangle(0, 0, width, TextRenderer.MeasureText(said, Font, new Size(width, 0), Flags).Height);
        second = Rectangle.Empty;
        if (hint.Length == 0) return;
        // Each on one line, as wide as it is.
        int saidWidth = TextRenderer.MeasureText(said, Font, Size.Empty, TextFormatFlags.NoPrefix).Width;
        int hintWidth = TextRenderer.MeasureText(hint, Font, Size.Empty, TextFormatFlags.NoPrefix).Width;
        second = saidWidth + (int)(HintGap * Look.Zoom) + hintWidth <= width ? new Rectangle(0, 0, width, first.Height)
            : new Rectangle(0, first.Bottom, width, TextRenderer.MeasureText(hint, Font, new Size(width, 0), Flags).Height);
    }

    static Color Colour(Notice kind)
    {
        return kind == Notice.Done ? Look.Green : kind == Notice.Warning ? Look.Amber : kind == Notice.Error ? Look.Red : Look.Accent;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.Clear(Parent.BackColor);
        if (Text.Length == 0) return;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        Color colour = Colour(Kind);
        using (GraphicsPath shape = Look.Rounded(new Rectangle(0, 0, Width - 1, Height - 1)))
        using (Brush fill = new SolidBrush(Look.Blend(Parent.BackColor, colour, Tint)))
        using (Pen edge = new Pen(Look.Blend(Look.Border, colour, 0.3f)))
        {
            g.FillPath(fill, shape);
            g.DrawPath(edge, shape);
        }
        int inset = (int)(Inset * Look.Zoom);
        string said, hint;
        Rectangle first, second;
        Place(Width - inset - TextLeft, out said, out hint, out first, out second);
        int top = Math.Max(0, (Height - Math.Max(first.Bottom, second.Bottom)) / 2);
        // The sign beside the first line.
        Look.Draw(g, Kind == Notice.Done ? Sign.Done : Kind == Notice.Warning ? Sign.Warning : Kind == Notice.Error ? Sign.Error : Sign.Info,
            new Rectangle(inset, top + (Font.Height - Side) / 2, Side, Side), colour);
        first.Offset(TextLeft, top);
        second.Offset(TextLeft, top);
        TextRenderer.DrawText(g, said, Font, first, Look.Ink, Flags);
        TextRenderer.DrawText(g, hint, Font, second, Look.Ink, Flags | TextFormatFlags.Right);
    }
}

// What an empty list says, in its place, as Windows 11 says it: a large faint sign of why it is empty, its words
// under it, all in the middle.
class EmptyNote : Label
{
    const float SignGrowth = 2.4f;   // how many times the height of the text the sign is
    const int SignGap = 8;           // between the sign and the words, in pixels at 96 dpi
    const TextFormatFlags Flags = TextFormatFlags.WordBreak | TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPrefix;
    Sign sign;

    public EmptyNote()
    {
        UseMnemonic = false;
        ResizeRedraw = true;
    }

    public Sign Sign
    {
        get { return sign; }
        set { sign = value; Invalidate(); }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.Clear(BackColor);
        int margin = Font.Height, width = Math.Max(1, Width - 2 * margin), side = (int)(Font.Height * SignGrowth);
        int gap = (int)(SignGap * Look.Zoom);
        int words = TextRenderer.MeasureText(g, Text, Font, new Size(width, 0), Flags).Height;
        int top = (Height - side - gap - words) / 2;
        Look.Draw(g, sign, new Rectangle((Width - side) / 2, top, side, side), Look.Blend(BackColor, Look.Muted, 0.55f));
        TextRenderer.DrawText(g, Text, Font, new Rectangle(margin, top + side + gap, width, words), ForeColor, Flags);
    }
}

// One option, a row of a card of options: its sign on the left, then its name in the heavier face and, under it in
// grey, what it does (Note); its controls at the right end. Rows placed one right under the other make one card:
// rounded only at its two ends, with a thin line between a row and the next. A row without a name only holds
// controls, placed by whoever fills it from TextLeft.
class Tile : Panel
{
    public const int Inset = 14, Air = 9;   // around its text, in pixels at 96 dpi: at its sides, above and below
    public const int ControlGap = 8;        // between its text and its controls, and between one control and the next
    const int SignSize = 18, SignGap = 16;  // its sign, and between it and the text
    public const int TextLeft = Inset + SignSize + SignGap;
    const int Joint = 2;   // at most between two rows of one card, in pixels: scaled one by one, they may not meet exactly

    public Tile()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        BackColor = Look.Raised;
    }

    // Whether another row lies right above or right under it, on the same card.
    bool Joined(bool above)
    {
        if (Parent != null)
            foreach (Control other in Parent.Controls)
                if (other != this && other is Tile && other.Left == Left && (above ? other.Bottom == Top : other.Top == Bottom))
                    return true;
        return false;
    }

    // Scaled one by one, two rows of one card may no longer meet exactly, by a pixel: once the window has its scale,
    // each is put right under the one above it, in the order they were placed.
    public static void Snap(Control page)
    {
        Control above = null;
        foreach (Control row in page.Controls)
        {
            if (!(row is Tile)) continue;
            if (above != null && Math.Abs(row.Top - above.Bottom) <= Joint) row.Top = above.Bottom;
            above = row;
        }
    }

    public string Note { get; set; }

    public Sign? Sign { get; set; }

    const TextFormatFlags NameFlags = TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis;
    const TextFormatFlags NoteFlags = TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix;

    // How tall its name and note are, written 'width' pixels wide.
    public static int TextHeight(string name, string note, int width)
    {
        int height = TextRenderer.MeasureText(name, Look.StrongFont, Size.Empty, NameFlags).Height;
        if (!string.IsNullOrEmpty(note))
            height += TextRenderer.MeasureText(note, Look.NoteFont, new Size(width, 0), NoteFlags).Height;
        return height;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.Clear(Parent.BackColor);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        // Joined to the row above, it goes up past its own top so as not to round it; joined to the row under it, it goes
        // down past its bottom, and draws the line between the two along it.
        bool above = Joined(true), under = Joined(false);
        int extra = 3 * Look.Radius;
        Rectangle area = Rectangle.FromLTRB(0, above ? -extra : 0, Width - 1, under ? Height + extra : Height - 1);
        using (GraphicsPath shape = Look.Rounded(area))
        using (Brush fill = new SolidBrush(BackColor))
        using (Pen edge = new Pen(Look.Border))
        {
            g.FillPath(fill, shape);
            g.DrawPath(edge, shape);
            if (under) g.DrawLine(edge, 0, Height - 1, Width, Height - 1);
        }
        int side = (int)(SignSize * Look.Zoom);
        if (Sign.HasValue)
            Look.Draw(g, Sign.Value, new Rectangle((int)(Inset * Look.Zoom), (Height - side) / 2, side, side), null);
        if (Text.Length == 0) return;
        // The text takes what the controls leave, in the middle of the height.
        int inset = (int)(TextLeft * Look.Zoom), right = Width - (int)(Inset * Look.Zoom);
        foreach (Control control in Controls) right = Math.Min(right, control.Left - (int)(ControlGap * Look.Zoom));
        int width = right - inset, top = (Height - TextHeight(Text, Note, width)) / 2;
        int name = TextRenderer.MeasureText(g, Text, Look.StrongFont, Size.Empty, NameFlags).Height;
        TextRenderer.DrawText(g, Text, Look.StrongFont, new Rectangle(inset, top, width, name), Look.Ink, NameFlags);
        if (!string.IsNullOrEmpty(Note))
            TextRenderer.DrawText(g, Note, Look.NoteFont, new Rectangle(inset, top + name, width, Height - top - name), Look.Muted, NoteFlags);
    }
}

// A panel drawn as a rounded shape in its own colour, with a thin edge. While what it holds has the focus, the edge
// takes the accent colour.
class Card : Panel
{
    public Card()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
    }

    // A card that holds a whole part of a window, not a field: its edge never changes.
    public bool Quiet { get; set; }

    // A part of a window that reaches its edges, as the sidebar and the area of the work: only its colour, square.
    public bool Flat { get; set; }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        if (Flat)
        {
            g.Clear(BackColor);
            base.OnPaint(e);
            return;
        }
        g.Clear(Parent.BackColor);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        bool focus = ContainsFocus && !Quiet;
        using (GraphicsPath shape = Look.Rounded(new Rectangle(0, 0, Width - 1, Height - 1)))
        using (Brush fill = new SolidBrush(BackColor))
        using (Pen edge = new Pen(focus ? Look.Accent : Look.Border))
        {
            g.FillPath(fill, shape);
            g.DrawPath(edge, shape);
        }
        base.OnPaint(e);
    }
}

// A row of tabs over a page, as Windows 11 draws them (SelectorBar): their names, the chosen one in full ink with a
// short line in the accent colour under it, the others in grey; the one under the pointer gets a faint shape.
class TabStrip : Control
{
    const int Pad = 12;                    // at either side of a name, in pixels at 96 dpi
    const int LineWidth = 16, LineHeight = 3;   // the line under the chosen one
    const TextFormatFlags Flags = TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding;
    readonly List<string> names = new List<string>();
    int selected, hover = -1;

    public event EventHandler SelectedChanged;

    public TabStrip()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        SetStyle(ControlStyles.Selectable, false);   // a click leaves the focus where it is
    }

    public void Add(string name)
    {
        names.Add(name);
        Invalidate();
    }

    public int Count
    {
        get { return names.Count; }
    }

    public int Selected
    {
        get { return selected; }
        set
        {
            if (value == selected) return;
            selected = value;
            Invalidate();
            if (SelectedChanged != null) SelectedChanged(this, EventArgs.Empty);
        }
    }

    // Where a tab is: its name with room at either side, one after the other from the left.
    public Rectangle TabAt(int index)
    {
        int pad = (int)(Pad * Look.Zoom), left = 0;
        for (int i = 0; ; i++)
        {
            int width = TextRenderer.MeasureText(names[i], Font, Size.Empty, Flags).Width + 2 * pad;
            if (i == index) return new Rectangle(left, 0, width, Height - (int)(LineHeight * Look.Zoom) - 2);
            left += width;
        }
    }

    int IndexAt(Point where)
    {
        for (int i = 0; i < names.Count; i++) if (TabAt(i).Contains(where)) return i;
        return -1;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        int index = IndexAt(e.Location);
        Cursor = index >= 0 ? Cursors.Hand : Cursors.Default;
        if (index == hover) return;
        hover = index;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        hover = -1;
        Invalidate();
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        int index = IndexAt(e.Location);
        if (e.Button == MouseButtons.Left && index >= 0) Selected = index;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.Clear(Parent.BackColor);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        for (int i = 0; i < names.Count; i++)
        {
            Rectangle tab = TabAt(i);
            if (i == hover && i != selected)
                using (GraphicsPath shape = Look.Rounded(tab))
                using (Brush back = new SolidBrush(Look.Blend(Parent.BackColor, Look.Ink, 0.07f)))
                    g.FillPath(back, shape);
            TextRenderer.DrawText(g, names[i], Font, tab, i == selected || i == hover ? Look.Ink : Look.Muted,
                Flags | TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            if (i != selected) continue;
            float width = LineWidth * Look.Zoom, height = LineHeight * Look.Zoom;
            using (GraphicsPath line = Look.Pill(new RectangleF(tab.Left + (tab.Width - width) / 2, Height - height - 1, width, height)))
            using (Brush accent = new SolidBrush(Look.Accent))
                g.FillPath(accent, line);
        }
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

    // A quiet button of a row of commands, as in the command bars of Windows 11: only its sign and text until the
    // pointer brings up its shape.
    public bool Subtle { get; set; }

    // A button that opens a list of choices and shows the one in use: drawn as a box to choose from, the choice on
    // the left and an arrow pointing down at the right end; while pressed, its edge in the accent colour.
    public bool Drops { get; set; }
    const int DropPad = 10, ArrowSize = 10;   // in pixels at 96 dpi: at its sides, and the arrow

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
        if (Subtle && !coloured)
        {
            // The shape comes up with the light, without an edge.
            float light = pressed ? 1.7f : lit.Value;
            if (Enabled && light > 0)
                using (GraphicsPath shape = Look.Rounded(area))
                using (Brush back = new SolidBrush(Look.Blend(Parent.BackColor, Look.Ink, 0.07f * light)))
                    g.FillPath(back, shape);
        }
        else
            using (GraphicsPath shape = Look.Rounded(area))
            using (Brush back = new SolidBrush(fill))
            using (Brush shade = new LinearGradientBrush(new Rectangle(0, -1, Width, Height + 1), top, bottom, 90f))
            using (Pen edge = new Pen(shade))
            {
                g.FillPath(back, shape);
                g.DrawPath(edge, shape);
            }
        if (Focused && ShowFocusCues || Drops && Enabled && pressed)
            using (GraphicsPath ring = Look.Rounded(Rectangle.Inflate(area, -1, -1)))
            using (Pen pen = new Pen(coloured ? Look.OnAccent : Drops ? Look.Accent : Look.Ink, Drops ? 1 : 2))
                g.DrawPath(pen, ring);
        // Sign and text together are centred. On a coloured button the sign takes the colour of the text.
        const TextFormatFlags flags = TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix
            | TextFormatFlags.NoPadding;
        Color ink = Enabled ? ForeColor : Look.Blend(Look.Muted, Parent.BackColor, 0.5f);
        float scale = (float)Height / Window.ButtonHeight;
        if (Drops)
        {
            // The choice on the left, the arrow at the right end.
            int pad = (int)(DropPad * scale), arrow = (int)(ArrowSize * scale);
            Look.Draw(g, global::Sign.Opened, new Rectangle(Width - pad - arrow, (Height - arrow) / 2, arrow, arrow), Look.Muted);
            TextRenderer.DrawText(g, Text, Font, Rectangle.FromLTRB(pad, 0, Width - 2 * pad - arrow, Height), ink,
                flags | TextFormatFlags.EndEllipsis);
            return;
        }
        int side = sign.HasValue ? (int)(SignSize * scale) : 0;
        int gap = sign.HasValue && Text.Length > 0 ? (int)(SignGap * scale) : 0;   // a sign alone stays in the middle
        int text = TextRenderer.MeasureText(g, Text, Font, Size.Empty, flags).Width, left = (Width - side - gap - text) / 2;
        if (sign.HasValue)
            Look.Draw(g, sign.Value, new Rectangle(left, (Height - side) / 2, side, side),
                !Enabled || coloured ? ink : (Color?)null);
        TextRenderer.DrawText(g, Text, Font, new Rectangle(left + side + gap, 0, text + 2, Height), ink, flags);
    }
}

// The small signs drawn beside entries, on buttons, beside the pages and the options, in menus (see Icons).
enum Sign { Text, Link, Star, Plus, Edit, Delete, Options, Keys, Search, Help, Opened, Closed, Bolt, History, Up,
    Bold, Italic, Underline, Erase, Formatted, Folder, NewFolder, Move, Pause, Resume, Open, Exit, Show, Check, Paint,
    Info, Done, Warning, Error, Paste, Code, Picture, Apps, More, Copy, Menu, FullFolder, Cancel,
    Timer, Globe, Enter, Unclip, Slow, Hash, Pin, Numbers, Calendar, Clean, Language, Contrast, Letters, Rows, Tray, Hide,
    Archive, Power }

// The colours the user chose in the options: those Windows sets for apps, or always the same. Saved as a number.
enum Theme { System, Light, Dark }

// Colours and touches shared by the windows: neutral, with rounded shapes and small coloured signs. Light or
// dark as the user chose (Choose), or as Windows is set for its apps, also when that changes while the program
// is open (Follow).
static class Look
{
    const int FramePadding = 6;   // between the edge of a framed text box and its text
    public const int Radius = 8;  // of the rounded corners
    const float MarkShare = 0.6f;    // how much of the height of a selected row its mark takes
    const string ModernFont = "Segoe UI Variable Text";   // what Windows 11 writes its own windows in
    // Its heavier face, for what stands out in a list, and the one of the titles, larger: Windows writes the name
    // of that one cut at 31 letters. Where they are missing (Windows 10), the semibold face of the family.
    const string StrongName = "Segoe UI Variable Text Semibold", TitleName = "Segoe UI Variable Display Semib";
    const float TitleGrowth = 1.45f;   // a title is this much larger than the text, as in Windows 11
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
    public static Font NoteFont { get; private set; }   // what an option does, under its name: smaller
    const float NoteShare = 0.85f;                      // of the size of the interface
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

    public static bool Has(string name)
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
        NoteFont = new Font(name, Points((int)Math.Round(size * NoteShare)));
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
        CodeFont = Installed(CodeFamily, points) ?? new Font(FontFamily.GenericMonospace, points);
        LinkFont = new Font(EntryFont, FontStyle.Underline);
    }

    // A piece of code on its card, in a font of fixed width; a link, underlined.
    const string CodeFamily = "Consolas";
    public static Font CodeFont { get; private set; }
    public static Font LinkFont { get; private set; }

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

    // The neutral part, warm greys: the window (the sidebar) a shade darker than the area where the work is (Well), on
    // which cards stand a shade lighter still (Raised); the selected row tinted with the accent.
    public static Color Back, Well, Soft, Border, Selected, Ink, Muted;
    public static Color Raised;   // a card standing on an area: a shade lighter than it
    // The coloured part: the accent, a warm orange, deeper in the light; what is written on it (OnAccent); the colours
    // of what a message is (done, warning, error) and of a few kinds, brighter in the dark.
    public static Color Accent, OnAccent, Green, Amber, Red, Teal, Purple;
    public static Color Link;   // an address written on its card, as links are blue
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
            Back = Color.FromArgb(25, 23, 21); Well = Color.FromArgb(35, 33, 31); Soft = Color.FromArgb(57, 54, 51);
            Raised = Color.FromArgb(45, 43, 40);
            Border = Color.FromArgb(80, 76, 72); Selected = Color.FromArgb(68, 50, 36);
            Ink = Color.FromArgb(237, 235, 231); Muted = Color.FromArgb(177, 173, 168);
            Accent = Color.FromArgb(253, 130, 4); OnAccent = Color.FromArgb(38, 27, 19);
            Green = Color.FromArgb(138, 181, 73); Amber = Color.FromArgb(235, 170, 45);
            Red = Color.FromArgb(255, 145, 137); Teal = Color.FromArgb(0, 179, 194); Purple = Color.FromArgb(206, 124, 200);
            Link = Color.FromArgb(96, 205, 255);
        }
        else
        {
            Back = Color.FromArgb(244, 239, 234); Well = Color.FromArgb(254, 253, 252); Soft = Color.FromArgb(233, 228, 222);
            Raised = Color.FromArgb(255, 255, 254);   // not quite white: a colour of the palette must be one of its own
            Border = Color.FromArgb(211, 207, 200); Selected = Color.FromArgb(247, 226, 207);
            Ink = Color.FromArgb(38, 33, 28); Muted = Color.FromArgb(102, 95, 88);
            Accent = Color.FromArgb(182, 78, 0); OnAccent = Color.FromArgb(255, 249, 240);
            Green = Color.FromArgb(83, 118, 14); Amber = Color.FromArgb(169, 107, 0);
            Red = Color.FromArgb(201, 34, 43); Teal = Color.FromArgb(0, 160, 175); Purple = Color.FromArgb(183, 97, 177);
            Link = Color.FromArgb(0, 95, 184);
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
        get { return new Color[] { Back, Well, Soft, Border, Selected, Ink, Muted, Raised, Accent, OnAccent, Green, Amber, Red, Teal, Purple, Link }; }
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
        return Rounded(area, Radius);
    }

    public static GraphicsPath Rounded(Rectangle area, int radius)
    {
        int side = 2 * radius;
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

    // The background of a row of a list, as Windows 11 draws it: the selected one a rounded shape, the one under the
    // pointer a fainter shape, which comes and goes with 'glow' (see Glow). The short mark in the accent colour on the
    // left of the selected one is drawn by its list (RowList), which slides it from row to row.
    public static void DrawRow(Graphics g, Rectangle bounds, Color back, bool selected, float glow)
    {
        using (Brush plain = new SolidBrush(back)) g.FillRectangle(plain, bounds);
        if (!selected && glow <= 0) return;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using (GraphicsPath shape = Rounded(RowShape(bounds)))
        using (Brush fill = new SolidBrush(selected ? Selected : Blend(back, Soft, glow)))
            g.FillPath(fill, shape);
    }

    static Rectangle RowShape(Rectangle bounds)
    {
        return Rectangle.FromLTRB(bounds.Left + 1, bounds.Top + 1, bounds.Right - 2, bounds.Bottom - 1);
    }

    // Where the mark of a selected row goes: on its left, in the middle of its height.
    public static RectangleF MarkIn(Rectangle bounds)
    {
        Rectangle row = RowShape(bounds);
        float height = row.Height * MarkShare;
        return new RectangleF(row.Left + 1, row.Top + (row.Height - height) / 2, 3 * Zoom, height);
    }

    public static void DrawMark(Graphics g, RectangleF mark)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using (GraphicsPath shape = Pill(mark))
        using (Brush accent = new SolidBrush(Accent))
            g.FillPath(accent, shape);
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

    // A picture drawn partly see-through: 'opacity' from 0, nothing, to 1, as it is.
    public static void DrawFaint(Graphics g, Image image, Rectangle box, float opacity)
    {
        using (System.Drawing.Imaging.ImageAttributes faint = new System.Drawing.Imaging.ImageAttributes())
        {
            System.Drawing.Imaging.ColorMatrix matrix = new System.Drawing.Imaging.ColorMatrix();
            matrix.Matrix33 = opacity;
            faint.SetColorMatrix(matrix);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(image, box, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, faint);
        }
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
        button.ForeColor = primary ? OnAccent : Ink;
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

    // The colour a sign has of its own: the signs are drawn in ink, as lines; only what a message is has a colour.
    // Those that only point somewhere (opening, closing, more) are grey.
    public static Color Own(Sign sign)
    {
        switch (sign)
        {
            case Sign.Done: return Green;
            case Sign.Warning: return Amber;
            case Sign.Error: return Red;
            case Sign.More: case Sign.Opened: case Sign.Closed: case Sign.Exit: case Sign.Cancel: return Muted;
            default: return Ink;
        }
    }

    // Draws one of the small signs in its own colour, or in the given one, inside a square box (see Icons).
    public static void Draw(Graphics g, Sign sign, Rectangle box, Color? colour)
    {
        Icons.Draw(g, sign, box, colour ?? Own(sign));
    }

    // A text box as a flat area a shade off the window. The system border cannot be coloured, so the box goes
    // borderless inside a card that takes its place; the card is what gets added to the window.
    public static Panel Frame(TextBoxBase box)
    {
        box.BorderStyle = BorderStyle.None;
        Field(box);
        int textHeight = box.Height;
        Card frame = Wrap(box, FramePadding);
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

    // A menu as Windows 11 draws its own: rounded, roomy rows, a rounded shape under the pointer, a sign before
    // the text of an entry that has one (Mark) and a tick before the one ticked. Its colours are read as it paints,
    // so it follows the light and the dark by itself.
    public static void Style(ToolStripDropDownMenu menu)
    {
        menu.Renderer = new MenuLook();
        menu.Font = TextFont;
        menu.ShowImageMargin = false;
        int edge = (int)(MenuEdge * Zoom);
        menu.Padding = new Padding(menu.Padding.Left, edge, menu.Padding.Right, edge);
        foreach (ToolStripItem item in menu.Items) Space(item);
        menu.ItemAdded += delegate(object sender, ToolStripItemEventArgs e) { Space(e.Item); };
        // The signs go in the margin a menu keeps for images, which it measures by itself: only where there are
        // signs, as known once the entries are as they will be shown. Windows rounds the menu once it exists, with
        // an edge in the colour of the moment.
        int side = TextFont.Height;
        menu.ImageScalingSize = new Size(side + 2 * (int)(MenuText * Zoom), side);   // with air on either side of the sign
        menu.Opening += delegate
        {
            menu.ShowImageMargin = HasSigns(menu);
            if (menu.IsHandleCreated) Corners(menu);
        };
        menu.HandleCreated += delegate { Corners(menu); };
    }

    static void Space(ToolStripItem item)
    {
        int row = (int)(MenuRow * Zoom);
        if (!(item is ToolStripSeparator)) item.Padding = new Padding((int)(MenuText * Zoom), row, item.Padding.Right, row);
    }

    // The signs of menu entries, as Windows 11 has them before the text of its own.
    static readonly ConditionalWeakTable<ToolStripItem, object> marks = new ConditionalWeakTable<ToolStripItem, object>();

    // Gives a menu entry its sign.
    public static void Mark(ToolStripItem item, Sign sign)
    {
        marks.Remove(item);
        marks.Add(item, sign);
        item.Invalidate();
    }

    // The sign of an entry: a tick if it is ticked, the one it was given, or none.
    static Sign? MarkOf(ToolStripItem item)
    {
        ToolStripMenuItem entry = item as ToolStripMenuItem;
        if (entry != null && entry.Checked) return Sign.Check;
        object sign;
        return marks.TryGetValue(item, out sign) ? (Sign)sign : (Sign?)null;
    }

    static bool HasSigns(ToolStrip menu)
    {
        foreach (ToolStripItem item in menu.Items)
            if (MarkOf(item).HasValue) return true;
        return false;
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
        // is written on the right of an entry (its keys) along with it, quieter. The sign of the entry, if it has
        // one, goes in the margin before the text, which keeps the same distance from it.
        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            Rectangle area = e.TextRectangle;
            int before = (int)(MenuText * Zoom);
            bool keys = e.Text != e.Item.Text;
            Color faint = Blend(Muted, Well, 0.35f);
            Sign? sign = MarkOf(e.Item);
            if (sign.HasValue && !keys)
            {
                int side = TextFont.Height;
                // Level with the text, which the menu writes a little above the middle of the row.
                Draw(e.Graphics, sign.Value, new Rectangle(area.Left - side, area.Top + (area.Height - side) / 2, side, side),
                    e.Item.Enabled ? (Color?)null : faint);
            }
            area.Offset(before, 0);
            TextRenderer.DrawText(e.Graphics, e.Text, e.TextFont, area, !e.Item.Enabled ? faint : keys ? Muted : Ink, e.TextFormat);
        }

        // The tick of an entry is its sign (MarkOf).
        protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
        {
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
        tips.Popup += delegate(object sender, PopupEventArgs e) { e.ToolTipSize = TipSize(tips.GetToolTip(e.AssociatedControl)); };
        tips.Draw += delegate(object sender, DrawToolTipEventArgs e)
        {
            using (Brush back = new SolidBrush(Well)) e.Graphics.FillRectangle(back, e.Bounds);
            using (Pen edge = new Pen(Border)) e.Graphics.DrawRectangle(edge, 0, 0, e.Bounds.Width - 1, e.Bounds.Height - 1);
            TextRenderer.DrawText(e.Graphics, e.ToolTipText, TextFont, Rectangle.Inflate(e.Bounds, -padding, -padding), Ink, TipFlags);
        };
    }

    // How large a tooltip is with its text.
    public static Size TipSize(string text)
    {
        int padding = (int)(TipPadding * Zoom);
        Size size = TextRenderer.MeasureText(text, TextFont, Size.Empty, TipFlags);
        return new Size(size.Width + 2 * padding, size.Height + 2 * padding);
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
