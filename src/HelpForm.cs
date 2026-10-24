using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

// The window with the guide: what the program does, one subject at a time, chosen from the column on the
// left. It stays open beside the main window, so that it can be read while trying things out.
class HelpForm : Window
{
    const int Edge = 8, TextWidth = 540, TextHeight = 470;   // in pixels at 96 dpi; the margin as in the main window
    const int TopicsWidth = 190, Gap = 8;      // the column of the subjects, and the room between it and the text
    const int TopicPadding = 14, TopicInset = 12;   // added to the text height to get a subject's row; text from its left
    const int TopicsPadding = 5;              // between the column and its list

    // The guide itself, in the language in use (see Strings.Guide for how its lines are written). Made anew each
    // time the guide opens, because the user can change the keys of the hotkeys.
    readonly string[] Lines = Strings.Guide();

    readonly GuidePage page = new GuidePage();
    readonly RowList topics = new RowList();       // the subjects: each item is the index of its heading in Lines
    readonly Button closeButton = new RoundButton();

    public HelpForm()
    {
        Text = string.Format(Strings.HelpTitle, Program.AppName);
        Secondary(FormStartPosition.Manual);   // placed over its owner when it loads; it stays above it

        // On the left the subjects, drawn like the rows of the main list; the arrows move among them.
        topics.BorderStyle = BorderStyle.None;
        topics.IntegralHeight = false;
        topics.DrawMode = DrawMode.OwnerDrawFixed;
        for (int i = 0; i < Lines.Length; i++)
            if (GuidePage.Is(Lines[i], GuidePage.Heading)) topics.Items.Add(i);
        topics.SetBounds(Edge, Edge, TopicsWidth, TextHeight);
        Look.Field(topics);
        // As the sidebar of the main window: the window's own colour, from its top to its bottom (placed below).
        topics.BackColor = Look.Back;
        Card topicsCard = Look.Wrap(topics, TopicsPadding);
        topicsCard.Flat = true;
        topicsCard.BackColor = Look.Back;
        topics.DrawItem += delegate(object sender, DrawItemEventArgs e)
        {
            if (e.Index >= 0)
                Look.DrawTopic(e, topics, Lines[(int)topics.Items[e.Index]].Substring(GuidePage.Heading.Length), null,
                    (int)(TopicInset * Look.Zoom));
        };
        topics.SelectedIndexChanged += delegate { if (topics.SelectedIndex >= 0) page.Show(Lines, (int)topics.SelectedItem); };
        topics.LightUnderPointer();

        // On the right the subject chosen, on the area of the work.
        page.SetBounds(topicsCard.Right + Gap, Edge, TextWidth, TextHeight);

        int buttonTop = page.Bottom + Edge;
        closeButton.Text = Strings.Close;
        closeButton.SetBounds(page.Right - ButtonWidth, buttonTop, ButtonWidth, ButtonHeight);
        closeButton.Click += delegate { Close(); };
        Look.Style(closeButton, true);
        ClientSize = new Size(page.Right + Edge, buttonTop + ButtonHeight + Edge);
        // The page and the button on the area of the work; the subjects on a column that reaches the edges.
        BackColor = Look.Well;
        topicsCard.SetBounds(0, 0, topicsCard.Right, ClientSize.Height);
        topics.Bounds = Rectangle.FromLTRB(Edge, Edge, topicsCard.Width - TopicsPadding, topicsCard.Height - Edge);
        AcceptButton = closeButton;
        CancelButton = closeButton;   // Esc closes too

        topicsCard.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
        page.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        closeButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        Controls.AddRange(new Control[] { topicsCard, page, closeButton });
        Rail.Over(page);
        ActiveControl = topics;
        Finish();
    }

    // It can be made larger, never smaller than it starts, and opens over the window it belongs to without
    // leaving the screen.
    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        MinimumSize = Size;
        // Rows as tall as the window's scale wants them; choosing the first subject shows it.
        topics.ItemHeight = topics.Font.Height + (int)(TopicPadding * Look.Zoom);
        topics.SelectedIndex = 0;
        if (Owner != null)
            PlaceOnScreen(new Point(Owner.Left + (Owner.Width - Width) / 2, Owner.Top + (Owner.Height - Height) / 2),
                Screen.FromControl(Owner));
    }
}

// A subject of the guide, drawn by hand: its name in large, the few lines that say what it is about, then its sections,
// each a card with its name that a click opens and closes, as the expanders of Windows 11 do. The first section of a
// subject is open the first time it is shown; then each stays as it was left while the window is open. Above the
// sections, at the right end, a link opens or closes them all. The keyboard: the arrows go from section to section,
// Enter or the space opens and closes the one with the focus, the pages and Home/End scroll.
class GuidePage : Control, Scrolling
{
    public const string Heading = "# ", Section = "## ", Bullet = "- ";   // how a line of the guide says what it is
    const char KeyEnd = '\t';   // in a line that explains a key, between the key and what it does
    const int Pad = 18, TitleGap = 10;              // around the page, and under its name, in pixels at 96 dpi
    const int CardGap = 6, CardInset = 14, HeadAir = 11, BodyAir = 12;   // between cards; inside one: at its sides,
                                                    // above and below its name, under its text
    const int ChevronSize = 12;                     // the arrow at the right end of the name of a section
    const int BulletIndent = 14, KeyColumn = 110, KeyGap = 14;   // a point's text; the least column of the keys
    const float KeyShare = 0.45f;                   // the most of its width the column of the keys takes
    const int WheelLines = 3;                       // a notch of the wheel scrolls this many lines
    // The page is drawn moved up by the scroll, which TextRenderer follows only when asked to.
    const TextFormatFlags Flags = TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding
        | TextFormatFlags.PreserveGraphicsTranslateTransform;
    const TextFormatFlags OneLine = TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding
        | TextFormatFlags.EndEllipsis | TextFormatFlags.PreserveGraphicsTranslateTransform;
    // Measured without the dots: asked how wide a text is with them, TextRenderer answers the width of the dots alone.
    const TextFormatFlags Measured = TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding;

    // The subject shown: its index in the lines of the guide, its name, the lines before its first section, the sections.
    int subject = -1;
    string title = "";
    readonly List<string> intro = new List<string>();
    readonly List<string> names = new List<string>();
    readonly List<List<string>> bodies = new List<List<string>>();
    readonly HashSet<string> opened = new HashSet<string>(), seen = new HashSet<string>();   // by subject and section

    // Where things were drawn, in the coordinates of the whole page: the name of each section, and the link.
    readonly List<Rectangle> heads = new List<Rectangle>();
    Rectangle allLink;
    int height;               // of the whole page
    int top;                  // how far it is scrolled
    int hover = -1;           // the section whose name is under the pointer; AllHover: the link
    int focus;                // the section with the focus of the keyboard
    const int AllHover = -2;

    public event EventHandler Scrolled;

    public GuidePage()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint
            | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
        TabStop = true;
    }

    // A scroll bar of Windows, always there, under the thin one that stands over it (Rail).
    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams made = base.CreateParams;
            made.Style |= Native.WS_VSCROLL;
            return made;
        }
    }

    public static bool Is(string line, string kind)
    {
        return line.StartsWith(kind, StringComparison.Ordinal);
    }

    // Shows the subject whose heading is at 'start' of 'lines', from its top.
    public void Show(string[] lines, int start)
    {
        subject = start;
        title = lines[start].Substring(Heading.Length);
        intro.Clear();
        names.Clear();
        bodies.Clear();
        for (int i = start + 1; i < lines.Length && !Is(lines[i], Heading); i++)
        {
            if (Is(lines[i], Section))
            {
                names.Add(lines[i].Substring(Section.Length));
                bodies.Add(new List<string>());
            }
            else (bodies.Count > 0 ? bodies[bodies.Count - 1] : intro).Add(lines[i]);
        }
        // The first time, the first section is open.
        if (seen.Add(subject.ToString()) && names.Count > 0) opened.Add(Key(0));
        focus = 0;
        top = 0;
        Changed();
    }

    string Key(int section)
    {
        return subject + ":" + section;
    }

    bool IsOpen(int section)
    {
        return opened.Contains(Key(section));
    }

    bool AllOpen
    {
        get
        {
            for (int i = 0; i < names.Count; i++) if (!IsOpen(i)) return false;
            return true;
        }
    }

    // Opens a section, or closes it.
    void Toggle(int section)
    {
        if (!opened.Remove(Key(section))) opened.Add(Key(section));
        Changed();
        Reveal(section);
    }

    void ToggleAll()
    {
        bool open = !AllOpen;
        for (int i = 0; i < names.Count; i++)
            if (open) opened.Add(Key(i));
            else opened.Remove(Key(i));
        Changed();
    }

    // What is shown changed: measured again, the scroll bar told, drawn again.
    void Changed()
    {
        using (Graphics g = CreateGraphics()) height = Lay(g, false);
        ScrollTo(top);
        Invalidate();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (IsHandleCreated) Changed();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        Changed();
    }

    // --- Drawing ---

    int Px(int size)
    {
        return (int)Math.Round(size * Look.Zoom);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(Parent.BackColor);
        e.Graphics.TranslateTransform(0, -top);
        Lay(e.Graphics, true);
    }

    // Lays the page out, drawing it if 'draw' (the graphics already moved by the scroll), and keeps where the names of
    // the sections and the link are. Returns how tall it is.
    int Lay(Graphics g, bool draw)
    {
        int pad = Px(Pad), left = pad, width = Math.Max(1, ClientSize.Width - 2 * pad), y = pad;
        heads.Clear();
        allLink = Rectangle.Empty;
        // The name, and the link that opens or closes all the sections at the right end of its line.
        int titleHeight = TextRenderer.MeasureText(g, title.Length > 0 ? title : " ", Look.TitleFont, Size.Empty, Measured).Height;
        int titleWidth = width;
        if (names.Count > 1)
        {
            string link = AllOpen ? Strings.CollapseAll : Strings.ExpandAll;
            Size size = TextRenderer.MeasureText(g, link, Font, Size.Empty, Measured);
            allLink = new Rectangle(left + width - size.Width, y + (titleHeight - size.Height) / 2, size.Width, size.Height);
            titleWidth = allLink.Left - left - Px(KeyGap);
            if (draw)
                TextRenderer.DrawText(g, link, Font, allLink, hover == AllHover ? Look.Ink : Look.Accent, OneLine);
        }
        if (draw) TextRenderer.DrawText(g, title, Look.TitleFont, new Rectangle(left, y, titleWidth, titleHeight), Look.Ink, OneLine);
        y += titleHeight + Px(TitleGap);
        y = Write(g, intro, left, width, y, draw);
        if (intro.Count > 0) y += Px(TitleGap);

        // The sections, each a card: its name, and its text under it when it is open.
        int inset = Px(CardInset), air = Px(HeadAir), chevron = Px(ChevronSize);
        for (int i = 0; i < names.Count; i++)
        {
            int nameHeight = TextRenderer.MeasureText(g, names[i], Look.StrongFont, Size.Empty, Measured).Height;
            Rectangle head = new Rectangle(left, y, width, nameHeight + 2 * air);
            heads.Add(head);
            bool open = IsOpen(i);
            int bottom = head.Bottom;
            if (open) bottom = Write(g, bodies[i], left + inset, width - 2 * inset, head.Bottom, false) + Px(BodyAir);
            if (draw)
            {
                Rectangle card = new Rectangle(left, y, width - 1, bottom - y - 1);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath shape = Look.Rounded(card))
                using (Brush fill = new SolidBrush(Look.Raised))
                using (Pen edge = new Pen(Look.Border))
                {
                    g.FillPath(fill, shape);
                    // The name lights up under the pointer.
                    if (hover == i)
                    {
                        g.SetClip(shape);
                        using (Brush lit = new SolidBrush(Look.Blend(Look.Raised, Look.Soft, 0.6f)))
                            g.FillRectangle(lit, head);
                        g.ResetClip();
                    }
                    g.DrawPath(edge, shape);
                    if (open) g.DrawLine(edge, card.Left, head.Bottom - 1, card.Right, head.Bottom - 1);
                    if (Focused && ShowFocusCues && focus == i)
                        using (GraphicsPath ring = Look.Rounded(Rectangle.Inflate(new Rectangle(head.Left, head.Top, head.Width - 1,
                            head.Height - 1), -2, -2)))
                        using (Pen pen = new Pen(Look.Accent, 2))
                            g.DrawPath(pen, ring);
                }
                Look.Draw(g, open ? Sign.Opened : Sign.Closed, new Rectangle(head.Right - inset - chevron,
                    head.Top + (head.Height - chevron) / 2, chevron, chevron), Look.Muted);
                TextRenderer.DrawText(g, names[i], Look.StrongFont, new Rectangle(head.Left + inset, head.Top + air,
                    head.Width - 3 * inset - chevron, nameHeight), Look.Ink, OneLine);
                if (open) Write(g, bodies[i], left + inset, width - 2 * inset, head.Bottom, true);
            }
            y = bottom + Px(CardGap);
        }
        return y - Px(CardGap) + pad;
    }

    // Writes the lines of a part of the page from 'y', each with the look of what it is, and returns where they end:
    // a paragraph, a point of a list, or a key in bold in a column of its own with what it does beside it. Some air
    // before a paragraph, and before the first line; a list stays close to what opens it, its points a little apart.
    int Write(Graphics g, List<string> lines, int left, int width, int y, bool draw)
    {
        int air = Font.Height / 2, bullet = Px(BulletIndent), keyGap = Px(KeyGap);
        // The column of the keys, as wide as the widest of them, within its limits.
        int keys = 0;
        foreach (string line in lines)
        {
            int end = line.IndexOf(KeyEnd);
            if (end >= 0) keys = Math.Max(keys, TextRenderer.MeasureText(g, line.Substring(0, end), Look.StrongFont, Size.Empty, Measured).Width);
        }
        keys = Math.Min(Math.Max(keys + keyGap, Px(KeyColumn)), (int)(width * KeyShare));
        for (int i = 0; i < lines.Count; i++)
        {
            string line = lines[i];
            bool point = Is(line, Bullet);
            int end = line.IndexOf(KeyEnd);
            if (i == 0 || !point && end < 0) y += air;
            else y += air / 3;   // the points, and the keys, a little apart from each other
            if (point)
            {
                string said = line.Substring(Bullet.Length);
                int tall = Tall(g, said, Font, width - bullet);
                if (draw)
                {
                    TextRenderer.DrawText(g, "•", Font, new Point(left, y), Look.Muted, OneLine);
                    TextRenderer.DrawText(g, said, Font, new Rectangle(left + bullet, y, width - bullet, tall), Look.Ink, Flags);
                }
                y += tall;
            }
            else if (end >= 0)
            {
                string key = line.Substring(0, end), does = line.Substring(end + 1);
                int tall = Math.Max(Tall(g, key, Look.StrongFont, keys - keyGap), Tall(g, does, Font, width - keys));
                if (draw)
                {
                    TextRenderer.DrawText(g, key, Look.StrongFont, new Rectangle(left, y, keys - keyGap, tall), Look.Ink, Flags);
                    TextRenderer.DrawText(g, does, Font, new Rectangle(left + keys, y, width - keys, tall), Look.Ink, Flags);
                }
                y += tall;
            }
            else
            {
                int tall = Tall(g, line, Font, width);
                if (draw) TextRenderer.DrawText(g, line, Font, new Rectangle(left, y, width, tall), Look.Ink, Flags);
                y += tall;
            }
        }
        return y;
    }

    static int Tall(Graphics g, string text, Font font, int width)
    {
        return TextRenderer.MeasureText(g, text.Length > 0 ? text : " ", font, new Size(Math.Max(1, width), 0), Flags).Height;
    }

    // --- Scrolling ---

    int Span
    {
        get { return Math.Max(0, height - ClientSize.Height); }
    }

    void ScrollTo(int wanted)
    {
        int was = top;
        top = Math.Max(0, Math.Min(Span, wanted));
        if (IsHandleCreated)
        {
            Native.SCROLLINFO info = new Native.SCROLLINFO();
            info.cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(Native.SCROLLINFO));
            info.fMask = Native.SIF_ALL | Native.SIF_DISABLENOSCROLL;
            info.nMin = 0;
            info.nMax = Math.Max(0, height - 1);
            info.nPage = (uint)Math.Max(0, ClientSize.Height);
            info.nPos = top;
            Native.SetScrollInfo(Handle, Native.SB_VERT, ref info, true);
        }
        if (top != was) Invalidate();
        if (Scrolled != null) Scrolled(this, EventArgs.Empty);
    }

    // Brings a section's name into view.
    void Reveal(int section)
    {
        if (section < 0 || section >= heads.Count) return;
        Rectangle head = heads[section];
        if (head.Top - Px(CardGap) < top) ScrollTo(head.Top - Px(CardGap));
        else if (head.Bottom + Px(CardGap) > top + ClientSize.Height) ScrollTo(head.Bottom + Px(CardGap) - ClientSize.Height);
    }

    int Line
    {
        get { return Font.Height; }
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == Native.WM_VSCROLL)
        {
            int request = (int)((long)m.WParam & 0xFFFF), position = (int)(((long)m.WParam >> 16) & 0xFFFF);
            switch (request)
            {
                case Native.SB_LINEUP: ScrollTo(top - Line); break;
                case Native.SB_LINEDOWN: ScrollTo(top + Line); break;
                case Native.SB_PAGEUP: ScrollTo(top - ClientSize.Height); break;
                case Native.SB_PAGEDOWN: ScrollTo(top + ClientSize.Height); break;
                case Native.SB_THUMBPOSITION:
                case Native.SB_THUMBTRACK: ScrollTo(position); break;
                case Native.SB_TOP: ScrollTo(0); break;
                case Native.SB_BOTTOM: ScrollTo(Span); break;
            }
            return;
        }
        base.WndProc(ref m);
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        ScrollTo(top - e.Delta * WheelLines * Line / SystemInformation.MouseWheelScrollDelta);
    }

    // --- Pointer and keys ---

    // The section whose name is under a point of the control, AllHover for the link, or -1.
    int At(Point point)
    {
        Point on = new Point(point.X, point.Y + top);
        if (allLink.Contains(on)) return AllHover;
        for (int i = 0; i < heads.Count; i++) if (heads[i].Contains(on)) return i;
        return -1;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        int now = At(e.Location);
        Cursor = now == -1 ? Cursors.Default : Cursors.Hand;
        if (now == hover) return;
        hover = now;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        hover = -1;
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        int at = At(e.Location);
        if (e.Button != MouseButtons.Left || at == -1) return;
        if (at == AllHover)
        {
            ToggleAll();
            return;
        }
        focus = at;
        Toggle(at);
    }

    protected override bool IsInputKey(Keys keyData)
    {
        switch (keyData)
        {
            case Keys.Up: case Keys.Down: case Keys.Enter: case Keys.Space: case Keys.PageUp: case Keys.PageDown:
            case Keys.Home: case Keys.End:
                return true;
        }
        return base.IsInputKey(keyData);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        switch (e.KeyCode)
        {
            case Keys.Up:
            case Keys.Down:
                if (names.Count == 0) break;
                focus = Math.Max(0, Math.Min(names.Count - 1, focus + (e.KeyCode == Keys.Up ? -1 : 1)));
                Reveal(focus);
                Invalidate();
                break;
            case Keys.Enter:
            case Keys.Space:
                if (focus < names.Count) Toggle(focus);
                break;
            case Keys.PageUp: ScrollTo(top - ClientSize.Height); break;
            case Keys.PageDown: ScrollTo(top + ClientSize.Height); break;
            case Keys.Home: ScrollTo(0); break;
            case Keys.End: ScrollTo(Span); break;
            default: return;
        }
        e.Handled = true;
    }

    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        Invalidate();
    }

    protected override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
        Invalidate();
    }
}
