using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

// The window with the guide: what the program does, one subject at a time, chosen from the column on the
// left. It stays open beside the main window, so that it can be read while trying things out.
class HelpForm : Form
{
    const int Edge = 12, TextWidth = 520, TextHeight = 470;   // in pixels at 96 dpi
    const int TopicsWidth = 190, Gap = 10;     // the column of the subjects, and the room between it and the text
    const int TopicPadding = 14, TopicInset = 12;   // added to the text height to get a subject's row; text from its left
    const int ButtonWidth = 90, ButtonHeight = 26;
    const int TextPadding = 10;                // between the rounded edge and the text
    const int BulletIndent = 14, KeyColumn = 120;   // where the text of a point starts, and that of a key
    const float HeadingGrowth = 1.15f;         // a heading is this much larger than the text
    const string Heading = "# ", Bullet = "- ";    // how a line of the guide says what it is
    const char KeyEnd = '\t';                       // in a line that explains a key, between the key and what it does

    // The guide itself, in the language in use (see Strings.Guide for how its lines are written). Made anew each
    // time the guide opens, because the user can change the keys of the hotkeys.
    readonly string[] Lines = Strings.Guide();

    readonly RichTextBox text = new RichTextBox();
    readonly ListBox topics = new ListBox();       // the subjects: each item is the index of its heading in Lines
    readonly Button closeButton = new RoundButton();

    public HelpForm()
    {
        SuspendLayout();
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = Look.TextFont;
        Text = string.Format(Strings.HelpTitle, Program.AppName);
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ShowIcon = false;
        StartPosition = FormStartPosition.Manual;   // placed over its owner when it loads; it stays above it
        BackColor = Look.Back;
        ForeColor = Look.Ink;

        // On the left the subjects, drawn like the rows of the main list; the arrows move among them.
        topics.BorderStyle = BorderStyle.None;
        topics.IntegralHeight = false;
        topics.DrawMode = DrawMode.OwnerDrawFixed;
        for (int i = 0; i < Lines.Length; i++)
            if (Lines[i].StartsWith(Heading)) topics.Items.Add(i);
        topics.SetBounds(Edge, Edge, TopicsWidth, TextHeight);
        Look.Field(topics);
        Panel topicsCard = Look.Wrap(topics, TextPadding / 2);
        topics.DrawItem += DrawTopic;
        topics.SelectedIndexChanged += delegate { Write(); };
        topics.MouseMove += delegate(object sender, MouseEventArgs e)
        {
            int index = topics.IndexFromPoint(e.Location);
            int row = index >= 0 && topics.GetItemRectangle(index).Contains(e.Location) ? index : -1;
            if (Equals(topics.Tag, row)) return;
            topics.Tag = row;   // the row under the pointer, which lights up
            topics.Cursor = row >= 0 ? Cursors.Hand : Cursors.Default;
            topics.Invalidate();
        };
        topics.MouseLeave += delegate
        {
            topics.Tag = null;
            topics.Invalidate();
        };

        text.ReadOnly = true;
        text.BorderStyle = BorderStyle.None;
        text.ScrollBars = RichTextBoxScrollBars.Vertical;
        text.DetectUrls = false;
        text.TabStop = false;
        text.SetBounds(topicsCard.Right + Gap, Edge, TextWidth, TextHeight);
        Look.Field(text);
        Panel card = Look.Wrap(text, TextPadding);

        int buttonTop = card.Bottom + Edge;
        closeButton.Text = Strings.Close;
        closeButton.SetBounds(card.Right - ButtonWidth, buttonTop, ButtonWidth, ButtonHeight);
        closeButton.Click += delegate { Close(); };
        Look.Style(closeButton, true);
        ClientSize = new Size(card.Right + Edge, buttonTop + ButtonHeight + Edge);
        AcceptButton = closeButton;
        CancelButton = closeButton;   // Esc closes too

        topicsCard.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
        card.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        closeButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        Controls.AddRange(new Control[] { topicsCard, card, closeButton });
        ActiveControl = topics;
        ResumeLayout(false);
        PerformLayout();
    }

    // A subject in the column: its name, on a rounded shape with a mark in the accent colour when it is the
    // one shown, on a fainter shape under the pointer.
    void DrawTopic(object sender, DrawItemEventArgs e)
    {
        if (e.Index < 0) return;
        bool selected = (e.State & DrawItemState.Selected) != 0, hovered = Equals(topics.Tag, e.Index);
        using (Brush back = new SolidBrush(topics.BackColor))
        {
            e.Graphics.FillRectangle(back, e.Bounds);
        }
        Rectangle row = Rectangle.FromLTRB(e.Bounds.Left + 1, e.Bounds.Top + 1, e.Bounds.Right - 2, e.Bounds.Bottom - 1);
        if (selected || hovered)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath shape = Look.Rounded(row))
            using (Brush fill = new SolidBrush(selected ? Look.Selected : Look.Soft))
            using (Brush mark = new SolidBrush(Look.Accent))
            {
                e.Graphics.FillPath(fill, shape);
                if (selected) e.Graphics.FillRectangle(mark, row.Left + 3, row.Top + 5, 3, row.Height - 10);
            }
        }
        float scale = CurrentAutoScaleDimensions.Width / 96f;
        string name = Lines[(int)topics.Items[e.Index]].Substring(Heading.Length);
        TextRenderer.DrawText(e.Graphics, name, e.Font, Rectangle.FromLTRB(row.Left + (int)(TopicInset * scale), row.Top,
            row.Right - (int)(TopicInset * scale) / 2, row.Bottom), Look.Ink, TextFormatFlags.VerticalCenter
            | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        Look.Caption(this);
    }

    // It can be made larger, never smaller than it starts, and opens over the window it belongs to without
    // leaving the screen.
    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        MinimumSize = Size;
        // Rows as tall as the window's scale wants them; choosing the first subject writes it.
        topics.ItemHeight = topics.Font.Height + (int)(TopicPadding * CurrentAutoScaleDimensions.Width / 96f);
        topics.SelectedIndex = 0;
        if (Owner == null) return;
        Rectangle screen = Screen.FromControl(Owner).WorkingArea;
        int x = Owner.Left + (Owner.Width - Width) / 2, y = Owner.Top + (Owner.Height - Height) / 2;
        Location = new Point(Math.Max(screen.Left, Math.Min(x, screen.Right - Width)),
            Math.Max(screen.Top, Math.Min(y, screen.Bottom - Height)));
    }

    // Fills the box with the subject chosen on the left, from its heading in 'Lines' to the next one, each line
    // with the look of what it is. Written once the window has its scale.
    void Write()
    {
        if (topics.SelectedIndex < 0) return;
        float scale = CurrentAutoScaleDimensions.Width / 96f;   // the sizes above are for 96 dpi
        int bullet = (int)(BulletIndent * scale), key = (int)(KeyColumn * scale);
        int start = (int)topics.SelectedItem;
        text.Clear();
        using (Font heading = new Font(Font.FontFamily, Font.Size * HeadingGrowth, FontStyle.Bold))
        using (Font strong = new Font(Font, FontStyle.Bold))
        using (Font air = new Font(Font.FontFamily, Font.Size / 2))   // half a line, between paragraphs
        {
            Add(Lines[start].Substring(Heading.Length), heading, 0, 0);
            for (int i = start + 1; i < Lines.Length && !Lines[i].StartsWith(Heading); i++)
            {
                string line = Lines[i];
                // Some air under the heading and before a paragraph; a list stays with the paragraph that opens it.
                if (i == start + 1 || !line.StartsWith(Bullet) && line.IndexOf(KeyEnd) < 0) Add("", air, 0, 0);
                if (line.StartsWith(Bullet))
                {
                    Add("•\t" + line.Substring(Bullet.Length), Font, bullet, bullet);
                }
                else if (line.IndexOf(KeyEnd) >= 0)
                {
                    // The key in bold, what it does in a column of its own.
                    int end = line.IndexOf(KeyEnd);
                    Add(line.Substring(0, end + 1), strong, key, key, false);
                    Add(line.Substring(end + 1), Font, key, key);
                }
                else
                {
                    Add(line, Font, 0, 0);
                }
            }
        }
        text.Select(0, 0);
    }

    void Add(string words, Font font, int tab, int hanging)
    {
        Add(words, font, tab, hanging, true);
    }

    // Appends some text in the given font. 'hanging' is where the lines after the first start, 'tab' where
    // the text after a tab does; 'ends' closes the paragraph.
    void Add(string words, Font font, int tab, int hanging, bool ends)
    {
        text.Select(text.TextLength, 0);
        text.SelectionFont = font;
        text.SelectionColor = Look.Ink;
        text.SelectionIndent = 0;
        text.SelectionHangingIndent = hanging;
        text.SelectionTabs = tab > 0 ? new int[] { tab } : new int[0];
        text.SelectedText = words + (ends ? "\n" : "");
    }
}
