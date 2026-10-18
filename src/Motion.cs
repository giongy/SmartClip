using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

// What moves, as in Windows 11: lights that come up under the pointer and fade when it leaves, pages that rise
// into place, thin scroll bars that widen when the pointer reaches them. All of it goes in small steps on a
// timer, redrawing at each one.

// A value that goes towards its goal, between 0 and 1, a step at a time, redrawing its control at each.
class Fade
{
    public const int StepMs = 15;
    const float Step = 0.2f;   // five steps from one end to the other
    readonly Control owner;
    readonly Timer timer = new Timer();
    float goal;

    public Fade(Control owner)
    {
        this.owner = owner;
        timer.Interval = StepMs;
        timer.Tick += delegate
        {
            Value = Toward(Value, goal);
            if (Value == goal) timer.Stop();
            owner.Invalidate();
        };
        owner.Disposed += delegate { timer.Dispose(); };
    }

    public float Value { get; private set; }

    // Out of sight there is nothing to watch: the value is simply where it belongs.
    public void To(float target)
    {
        goal = target;
        if (owner.Visible && owner.IsHandleCreated)
        {
            if (Value != goal) timer.Start();
            return;
        }
        timer.Stop();
        Value = goal;
        owner.Invalidate();
    }

    // One step from a value towards its goal.
    public static float Toward(float value, float goal)
    {
        return Math.Abs(goal - value) <= Step ? goal : value + Math.Sign(goal - value) * Step;
    }
}

// The light under the pointer in the rows of a list: the row it comes to brightens, the one it left fades.
class Glow
{
    readonly ListBox list;
    readonly Dictionary<int, float> levels = new Dictionary<int, float>();   // the rows that are lit, even a little
    readonly Timer timer = new Timer();

    public Glow(ListBox list)
    {
        this.list = list;
        Row = -1;
        timer.Interval = Fade.StepMs;
        timer.Tick += delegate { Advance(); };
        list.Disposed += delegate { timer.Dispose(); };
    }

    public int Row { get; private set; }   // the one the pointer is on, or -1

    public void Set(int index)
    {
        if (index == Row) return;
        Row = index;
        if (index >= 0 && !levels.ContainsKey(index)) levels[index] = 0;
        timer.Start();
    }

    // The rows changed under it: what was lit belongs to no row now.
    public void Reset()
    {
        timer.Stop();
        levels.Clear();
        Row = -1;
    }

    // How lit a row is: 0 not at all, 1 fully.
    public float Of(int index)
    {
        float level;
        return levels.TryGetValue(index, out level) ? level : 0;
    }

    void Advance()
    {
        bool moving = false;
        foreach (int index in new List<int>(levels.Keys))
        {
            float goal = index == Row ? 1 : 0, level = Fade.Toward(levels[index], goal);
            if (level == 0 && goal == 0) levels.Remove(index);
            else levels[index] = level;
            moving |= level != goal;
            if (index < list.Items.Count) list.Invalidate(list.GetItemRectangle(index));
        }
        if (!moving) timer.Stop();
    }
}

// A list drawn by hand, row by row: its rows light up under the pointer (Glow), the mark of the selected one slides
// to the row selected next, and a Rail can stand over its scroll bar, which it keeps told of where the rows are.
class RowList : ListBox
{
    const int SlideSteps = 12;
    public readonly Glow Glow;
    public event EventHandler Scrolled;   // after anything that may have moved the rows
    readonly Timer slide = new Timer();
    RectangleF from, to;   // where the mark slides from and to, in the client area
    int step = -1;         // how far it has gone, or -1 while it stays on the selected row
    int marked = -1;       // the row the mark was last brought to

    public RowList()
    {
        Glow = new Glow(this);
        slide.Interval = Fade.StepMs;
        slide.Tick += delegate
        {
            Invalidate(Strip());
            if (++step < SlideSteps) return;
            step = -1;
            slide.Stop();
        };
        Disposed += delegate { slide.Dispose(); };
    }

    // As in the lists of Windows 11 the mark goes from the row selected before to the new one: its leading end
    // first, the other after it, so that it stretches and gathers again. Without a row before, or out of sight, it
    // is simply there.
    protected override void OnSelectedIndexChanged(EventArgs e)
    {
        base.OnSelectedIndexChanged(e);
        int index = SelectedIndex;
        bool known = marked >= 0 && marked < Items.Count;
        RectangleF before = step >= 0 ? Mark() : known ? Look.MarkIn(GetItemRectangle(marked)) : RectangleF.Empty;
        marked = index;
        if (index < 0 || !known || !Visible || !IsHandleCreated)
        {
            step = -1;
            slide.Stop();
            return;
        }
        from = before;
        to = Look.MarkIn(GetItemRectangle(index));
        if (from == to) return;
        step = 0;
        Invalidate(Strip());
        slide.Start();
    }

    // Where the mark is while it slides.
    RectangleF Mark()
    {
        float done = (float)step / SlideSteps;
        float lead = Ease(Math.Min(1, done * 1.5f)), trail = Ease(Math.Max(0, done * 1.5f - 0.5f));
        bool down = to.Top > from.Top;
        float top = from.Top + (to.Top - from.Top) * (down ? trail : lead);
        float bottom = from.Bottom + (to.Bottom - from.Bottom) * (down ? lead : trail);
        return RectangleF.FromLTRB(to.Left, top, to.Right, bottom);
    }

    // Fast at first, slow at the end.
    static float Ease(float done)
    {
        float left = 1 - done;
        return 1 - left * left * left;
    }

    // The band the mark slides along, to be drawn again.
    Rectangle Strip()
    {
        RectangleF band = RectangleF.Union(from, to);
        return Rectangle.FromLTRB(0, (int)band.Top - 2, (int)band.Right + 2, (int)Math.Ceiling(band.Bottom) + 2);
    }

    // Only the room below the last row is erased: each row paints its own background, and erasing under the rows
    // first would flash for an instant every time the list is filled again.
    void EraseBelowRows(IntPtr dc)
    {
        int top = Items.Count == 0 ? 0 : Math.Max(0, GetItemRectangle(Items.Count - 1).Bottom);
        if (top >= ClientSize.Height) return;
        using (Graphics g = Graphics.FromHdc(dc))
        using (Brush back = new SolidBrush(BackColor))
            g.FillRectangle(back, 0, top, ClientSize.Width, ClientSize.Height - top);
    }

    // Each row is drawn out of sight and then put on the screen at once. Drawn there directly, it would show its
    // background alone for an instant before the rest, at every step of the light under the pointer: a flicker.
    // Out of sight the row starts at the corner: the text of TextRenderer would not follow a moved origin. Last
    // comes the mark: on the selected row, or the part of it that is on this row while it slides.
    protected override void OnDrawItem(DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Bounds.Width <= 0 || e.Bounds.Height <= 0)
        {
            base.OnDrawItem(e);
            return;
        }
        using (BufferedGraphics buffer = BufferedGraphicsManager.Current.Allocate(e.Graphics, e.Bounds))
        {
            Graphics g = buffer.Graphics;
            g.ResetTransform();
            Rectangle row = new Rectangle(Point.Empty, e.Bounds.Size);
            base.OnDrawItem(new DrawItemEventArgs(g, e.Font, row, e.Index, e.State, e.ForeColor, e.BackColor));
            if (step >= 0)
            {
                RectangleF mark = Mark();
                mark.Offset(0, -e.Bounds.Top);
                g.SetClip(row);
                if (mark.IntersectsWith(row)) Look.DrawMark(g, mark);
                g.ResetClip();
            }
            else if ((e.State & DrawItemState.Selected) != 0) Look.DrawMark(g, Look.MarkIn(row));
            buffer.Render(e.Graphics);
        }
    }

    // The row at a point, or -1: IndexFromPoint answers with the nearest one, also below the last.
    public int RowAt(Point where)
    {
        int index = IndexFromPoint(where);
        return index >= 0 && index < Items.Count && GetItemRectangle(index).Contains(where) ? index : -1;
    }

    // For a list that needs nothing more: the row under the pointer lights up, and the pointer says a click
    // would choose it.
    public void LightUnderPointer()
    {
        MouseMove += delegate(object sender, MouseEventArgs e)
        {
            int row = RowAt(e.Location);
            Cursor = row >= 0 ? Cursors.Hand : Cursors.Default;
            Glow.Set(row);
        };
        MouseLeave += delegate { Glow.Set(-1); };
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == Native.WM_ERASEBKGND)
        {
            EraseBelowRows(m.WParam);
            m.Result = (IntPtr)1;
            return;
        }
        base.WndProc(ref m);
        if ((m.Msg == Native.WM_PAINT || m.Msg == Native.WM_VSCROLL || m.Msg == Native.WM_MOUSEWHEEL) && Scrolled != null)
            Scrolled(this, EventArgs.Empty);
    }
}

// Brings a control into place from a little lower down, slowing as it arrives: how the pages of Windows 11 come in.
static class Entrance
{
    const int Rise = 24;   // where it starts from, below its place, in pixels at 96 dpi
    const int Steps = 10;
    static readonly Dictionary<Control, Timer> playing = new Dictionary<Control, Timer>();

    public static void Play(Control control, int home)
    {
        Timer old;
        if (playing.TryGetValue(control, out old))
        {
            old.Dispose();
            playing.Remove(control);
        }
        control.Top = home;
        if (!control.Visible || !control.IsHandleCreated) return;   // out of sight it is simply in its place
        int rise = (int)(Rise * Look.Zoom), step = 0;
        Timer timer = new Timer();
        timer.Interval = Fade.StepMs;
        timer.Tick += delegate
        {
            step++;
            double left = 1 - (double)step / Steps;
            control.Top = home + (int)Math.Round(rise * left * left * left);   // eased: fast at first, slow at the end
            if (step < Steps) return;
            timer.Dispose();
            playing.Remove(control);
        };
        playing[control] = timer;
        control.Top = home + rise;
        timer.Start();
    }
}

// A thin scroll bar, as in Windows 11, standing over the system one of a list or a text box, which stays out of
// sight beneath it: a line that widens under the pointer and can be dragged. A click beside it moves a page;
// the wheel scrolls. The control must keep its bar always (ScrollAlwaysVisible, ForcedVertical), so that it
// does not change width; the line shows only when there is something to scroll.
class Rail : Control
{
    const int Thin = 2, Wide = 6;      // the line, and the bar under the pointer, in pixels at 96 dpi
    const int Edge = 3, MinThumb = 24; // its distance from the edges, and its least length
    readonly Control target;
    readonly Fade wide;
    Native.SCROLLINFO info;   // as last drawn
    int grab = -1;            // while the bar is dragged, where in it the pointer took it

    Rail(Control target)
    {
        this.target = target;
        wide = new Fade(this);
        DoubleBuffered = true;
        TabStop = false;
        SetStyle(ControlStyles.Selectable, false);   // a click leaves the focus where it is
        Cursor = Cursors.Default;
    }

    // Puts a rail over the scroll bar of a control, in the same parent, which follows it as it scrolls.
    public static Rail Over(Control target)
    {
        Rail rail = new Rail(target);
        target.Parent.Controls.Add(rail);
        rail.BringToFront();
        target.Resize += delegate { rail.Place(); };
        target.HandleCreated += delegate { rail.Place(); };
        RowList rows = target as RowList;
        if (rows != null) rows.Scrolled += delegate { rail.Follow(); };
        RichTextBox text = target as RichTextBox;
        if (text != null)
        {
            text.VScroll += delegate { rail.Follow(); };
            text.TextChanged += delegate { rail.Follow(); };
        }
        rail.Place();
        return rail;
    }

    void Place()
    {
        int bar = target.Width - target.ClientSize.Width;
        Bounds = new Rectangle(target.Right - bar, target.Top, bar, target.Height);
        Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
        Follow();
        Invalidate();
    }

    // Called whenever what the control shows may have moved: drawn again if the bar has to go elsewhere.
    public void Follow()
    {
        Native.SCROLLINFO now = Read();
        if (now.nPos == info.nPos && now.nMin == info.nMin && now.nMax == info.nMax && now.nPage == info.nPage) return;
        info = now;
        Invalidate();
    }

    Native.SCROLLINFO Read()
    {
        Native.SCROLLINFO read = new Native.SCROLLINFO();
        read.cbSize = (uint)Marshal.SizeOf(typeof(Native.SCROLLINFO));
        read.fMask = Native.SIF_ALL;
        if (!target.IsHandleCreated || !Native.GetScrollInfo(target.Handle, Native.SB_VERT, ref read)) return new Native.SCROLLINFO();
        return read;
    }

    int Range { get { return info.nMax - info.nMin + 1; } }
    int Span { get { return Range - (int)info.nPage; } }   // how far the first visible line can go
    bool Scrolls { get { return info.nPage > 0 && Span > 0; } }

    static int Px(int size)
    {
        return (int)Math.Round(size * Look.Zoom);
    }

    // Where the bar is, at its widest: as long as the part shown is of the whole, where that part is.
    Rectangle Thumb()
    {
        int edge = Px(Edge), track = Math.Max(1, Height - 2 * edge);
        int length = Math.Min(track, Math.Max(Px(MinThumb), (int)((long)track * info.nPage / Range)));
        int top = edge + (int)((long)(track - length) * (info.nPos - info.nMin) / Math.Max(1, Span));
        return new Rectangle(Width - edge - Px(Wide), top, Px(Wide), length);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Color back = target.BackColor;
        e.Graphics.Clear(back);
        if (!Scrolls) return;
        Rectangle thumb = Thumb();
        float width = Px(Thin) + (Px(Wide) - Px(Thin)) * wide.Value;
        RectangleF bar = new RectangleF(thumb.Right - width, thumb.Top, width, thumb.Height);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using (GraphicsPath shape = Look.Pill(bar))
        using (Brush fill = new SolidBrush(Look.Blend(back, Look.Muted, 0.55f + 0.35f * wide.Value)))
            e.Graphics.FillPath(fill, shape);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        wide.To(1);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        if (grab < 0) wide.To(0);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        info = Read();
        if (e.Button != MouseButtons.Left || !Scrolls) return;
        Rectangle thumb = Thumb();
        if (e.Y >= thumb.Top && e.Y < thumb.Bottom) grab = e.Y - thumb.Top;
        else Send(e.Y < thumb.Top ? Native.SB_PAGEUP : Native.SB_PAGEDOWN, 0);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (grab < 0 || e.Button != MouseButtons.Left) return;
        Rectangle thumb = Thumb();
        int edge = Px(Edge), room = Math.Max(1, Height - 2 * edge - thumb.Height);
        int position = info.nMin + (int)Math.Round((double)(e.Y - grab - edge) * Span / room);
        Send(Native.SB_THUMBPOSITION, Math.Max(info.nMin, Math.Min(info.nMin + Span, position)));
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        grab = -1;
        if (!ClientRectangle.Contains(e.Location)) wide.To(0);
    }

    // The wheel over the bar scrolls what it belongs to.
    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        Point screen = PointToScreen(e.Location);
        Native.SendMessage(target.Handle, Native.WM_MOUSEWHEEL, (IntPtr)(e.Delta << 16),
            (IntPtr)((screen.Y << 16) | (screen.X & 0xFFFF)));
        Follow();
    }

    void Send(int request, int position)
    {
        Native.SendMessage(target.Handle, Native.WM_VSCROLL, (IntPtr)((position << 16) | request), IntPtr.Zero);
        Native.SendMessage(target.Handle, Native.WM_VSCROLL, (IntPtr)Native.SB_ENDSCROLL, IntPtr.Zero);
        Follow();
    }
}
