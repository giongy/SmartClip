using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

// The two columns: filling and drawing their rows, choosing, saving and moving entries, the keys of the list.
partial class MainForm
{
    // What the lists answer to: buttons, right-click menu, mouse, dragging a saved text into a folder.
    void WireLists()
    {
        // The same three actions from the buttons, from the right-click menu and from the keyboard.
        EventHandler create = delegate { Edit(null); };
        EventHandler change = delegate { Edit(Selected); };
        EventHandler delete = delegate { DeleteSelected(); };
        newButton.Click += create;
        saveButton.Click += change;
        deleteButton.Click += delete;
        ToolStripItem changeItem = listMenu.Items.Add("", null, change);
        // Only for a saved text, of which the list shows the name alone: its content, for a while.
        ToolStripItem showItem = listMenu.Items.Add("Mostra il contenuto", null, delegate
        {
            if (savedList.SelectedIndex >= 0) ShowPeek(savedList, savedList.SelectedIndex, true);
        });
        // Only for a copied entry: the same as a click on its star.
        ToolStripItem pinItem = listMenu.Items.Add("", null, delegate { TogglePin(Selected); });
        // Only for a saved text, once there are folders: they are listed when the menu opens.
        ToolStripMenuItem moveItem = new ToolStripMenuItem("Sposta in");
        listMenu.Items.Add(moveItem);
        ToolStripItem deleteItem = listMenu.Items.Add(deleteButton.Text, null, delete);
        listMenu.Items.Add(newButton.Text, null, create);
        listMenu.Items.Add("Nuova cartella…", null, delegate { EditGroup(null); });
        listMenu.Items.Add(new ToolStripSeparator());
        pauseItems.Add(listMenu.Items.Add("", null, delegate { Pause(!paused); }));
        // With nothing selected the menu still opens: a first folder can be made in an empty list.
        listMenu.Opening += delegate
        {
            HidePeek();
            Entry entry = Selected;
            changeItem.Text = saveButton.Text;
            changeItem.Enabled = saveButton.Enabled;
            changeItem.Visible = deleteItem.Visible = entry != null;
            showItem.Visible = entry != null && entry.Name != null && !entry.IsGroup;
            pinItem.Visible = entry != null && entry.Name == null;
            pinItem.Text = entry != null && entry.Pinned ? "Togli dall'alto" : "Tieni in alto";
            // A saved text or a folder, wherever else it can go: a folder never into itself or one it holds.
            List<string> groups = entry == null ? new List<string>()
                : GroupNames().FindAll(delegate(string group) { return !entry.IsGroup || !Texts.Within(group, entry.Name); });
            // Asked back, 'Visible' would answer false: the menu is not on screen yet.
            bool movable = entry != null && entry.Name != null && (groups.Count > 0 || PlaceOf(entry) != null);
            moveItem.Visible = movable;
            moveItem.DropDownItems.Clear();
            if (!movable) return;
            // The place where it already is cannot be chosen.
            moveItem.DropDownItems.Add("Fuori dalle cartelle", null, delegate { MoveTo(entry, null); }).Enabled = PlaceOf(entry) != null;
            foreach (string each in groups)
            {
                string group = each;   // a variable of its own for the delegate
                moveItem.DropDownItems.Add(Texts.Display(group), null, delegate { MoveTo(entry, group); }).Enabled = CanGo(entry, group);
            }
        };
        Look.Style((ToolStripDropDownMenu)moveItem.DropDown);
        Look.Style(listMenu);

        foreach (ListBox each in new ListBox[] { recentList, savedList })
        {
            ListBox list = each;   // a variable of its own for the delegates below
            Look.Field(list);
            list.IntegralHeight = false;
            list.BorderStyle = BorderStyle.None;
            list.DrawMode = DrawMode.OwnerDrawVariable;
            list.ItemHeight = Font.Height + RowPadding;
            list.ContextMenuStrip = listMenu;
            list.MeasureItem += delegate(object sender, MeasureItemEventArgs e)
            {
                e.ItemHeight = ((Entry)list.Items[e.Index]).Picture ? Font.Height * PictureLines : list.ItemHeight;
            };
            list.DrawItem += DrawEntry;
            // One entry is selected in the whole window: selecting in a column clears the other.
            EventHandler taken = delegate
            {
                if (list.SelectedIndex >= 0)
                {
                    active = list;
                    Other(list).ClearSelected();
                }
                ShowActions();
            };
            list.SelectedIndexChanged += taken;
            // The left button chooses, the right one only selects, so that its menu acts on the entry under the pointer.
            list.MouseDown += delegate(object sender, MouseEventArgs e)
            {
                HidePeek();
                int index = RowAt(list, e.Location);
                if (e.Button == MouseButtons.Right && index >= 0) list.SelectedIndex = index;
                // Windows announces the row taken by a click only when the button is released, after the
                // click has been handled: without this the click would act on the entry selected before.
                taken(list, EventArgs.Empty);
            };
            // One click leaves the window where it is, so that the entry can still be saved or deleted; a
            // double click, like Enter, also goes back to the window the list hotkey was pressed in.
            // A click on the star of a copied entry does not choose it: it keeps it at the top, or lets it go.
            list.MouseClick += delegate(object sender, MouseEventArgs e)
            {
                int index = RowAt(list, e.Location);
                if (e.Button != MouseButtons.Left || index < 0) return;
                if (OnStar(list, e.Location)) TogglePin((Entry)list.Items[index]);
                else Accept(false);
            };
            // The second click on a star finds another entry there: the first one moved the row.
            // Nor does it count on a folder, which the first click has already opened or closed.
            list.MouseDoubleClick += delegate(object sender, MouseEventArgs e)
            {
                int index = RowAt(list, e.Location);
                if (e.Button == MouseButtons.Left && index >= 0 && !OnStar(list, e.Location) && !((Entry)list.Items[index]).IsGroup)
                    Accept(true);
            };
            // The entry under the pointer lights up: a click would choose it.
            list.MouseMove += delegate(object sender, MouseEventArgs e) { Hover(list, RowAt(list, e.Location)); };
            list.MouseLeave += delegate { Hover(list, -1); };
            list.MouseWheel += delegate { HidePeek(); };      // the rows move under it
            list.Resize += delegate { list.Invalidate(); };   // the rows are drawn up to the right edge
        }
        // The pointer resting on a copied entry shows it whole; one that was asked for leaves by itself.
        rest.Tick += delegate
        {
            rest.Stop();
            int row = recentList.Tag is int ? (int)recentList.Tag : -1;
            if (peek.Visible) HidePeek();
            else if (row >= 0 && row < recentList.Items.Count && !listMenu.Visible && MouseButtons == MouseButtons.None
                && recentList.RectangleToScreen(recentList.GetItemRectangle(row)).Contains(Cursor.Position))
                ShowPeek(recentList, row, false);
        };

        // A saved text, or a folder with all it holds, is dragged to a folder: onto the folder itself or onto
        // a text it holds. Dropped on a text outside the folders, or below them all, it leaves the one it was in.
        savedList.AllowDrop = true;
        savedList.MouseDown += delegate(object sender, MouseEventArgs e)
        {
            int index = RowAt(savedList, e.Location);
            grabbed = e.Button == MouseButtons.Left && index >= 0 ? (Entry)savedList.Items[index] : null;
            grabbedAt = e.Location;
        };
        savedList.MouseUp += delegate { grabbed = null; };
        savedList.MouseMove += delegate(object sender, MouseEventArgs e)
        {
            // Only once the pointer has really moved: a click must stay a click.
            Rectangle still = new Rectangle(grabbedAt, Size.Empty);
            still.Inflate(SystemInformation.DragSize);
            if (grabbed == null || e.Button != MouseButtons.Left || still.Contains(e.Location)) return;
            Entry entry = grabbed;
            grabbed = null;
            HidePeek();
            savedList.DoDragDrop(entry, DragDropEffects.Move);
            Aim(null);
        };
        savedList.DragOver += delegate(object sender, DragEventArgs e)
        {
            Entry entry = e.Data.GetData(typeof(Entry)) as Entry;
            string group = GroupAt(savedList.PointToClient(new Point(e.X, e.Y)));
            bool moves = entry != null && CanGo(entry, group);
            e.Effect = moves ? DragDropEffects.Move : DragDropEffects.None;
            Aim(moves ? group : null);
        };
        savedList.DragLeave += delegate { Aim(null); };
        savedList.DragDrop += delegate(object sender, DragEventArgs e)
        {
            Entry entry = e.Data.GetData(typeof(Entry)) as Entry;
            string group = GroupAt(savedList.PointToClient(new Point(e.X, e.Y)));
            Aim(null);
            if (entry != null && CanGo(entry, group)) MoveTo(entry, group);
        };
        // A new search starts again from its best result.
        search.TextChanged += delegate { active = null; Filter(); };
    }

    void Reload()
    {
        Change(delegate { entries = Texts.Load(); });
        Filter();
    }

    void Filter()
    {
        // The lists are refilled at every clipboard change, also the one caused by choosing an entry:
        // the selection must stay on the same entry, not jump back to the first.
        Entry selected = Selected;
        HidePeek();   // the row it belongs to may be about to move
        recentList.BeginUpdate();
        savedList.BeginUpdate();
        recentList.Items.Clear();
        savedList.Items.Clear();
        bool searching = search.Text.Trim().Length > 0;
        // A folder is there unless one it is inside of is closed, and in a search only with what was found in
        // it: its row, and those of the folders around it, wait here until something of theirs is listed.
        Dictionary<string, bool> open = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        List<Entry> waiting = new List<Entry>();   // from the outermost: each folder comes just before what it holds
        foreach (Entry entry in entries)
        {
            if (entry.IsGroup)
            {
                open[entry.Name] = entry.Open;
                waiting.RemoveAll(delegate(Entry other) { return !Texts.Within(entry.Name, other.Name); });
                if (searching) waiting.Add(entry);
                else if (InView(Texts.Parent(entry.Name), open)) ShowRow(savedList, entry, selected);
                continue;
            }
            if (!Texts.Matches(entry, search.Text)) continue;
            // The history can hold thousands: the search reaches the others. The copied ones come last.
            if (entry.Name == null && recentList.Items.Count == MaxShown) break;
            if (entry.Group != null)
            {
                if (!searching && !InView(entry.Group, open)) continue;
                foreach (Entry folder in waiting) ShowRow(savedList, folder, selected);
                waiting.Clear();
            }
            ShowRow(entry.Name == null ? recentList : savedList, entry, selected);
        }
        recentList.EndUpdate();
        savedList.EndUpdate();
        recentList.Tag = savedList.Tag = null;   // the rows changed under the pointer
        if (Selected == null) Pick(recentList.Items.Count > 0 ? recentList : savedList);
        ShowActions();

        int copied = entries.FindAll(delegate(Entry entry) { return entry.Name == null; }).Count;
        recentCount.Text = recentList.Items.Count == copied ? copied.ToString() : recentList.Items.Count + " di " + copied;
        // Of the saved ones the texts are counted, not the folders; those in a closed folder are there all the same.
        int saved = entries.FindAll(delegate(Entry entry) { return entry.Name != null && !entry.IsGroup; }).Count, found = 0;
        foreach (Entry entry in savedList.Items) if (!entry.IsGroup) found++;
        savedCount.Text = searching && found != saved ? found + " di " + saved : saved.ToString();
        recentEmpty.Text = searching ? "Nessun risultato" : !historyBox.Checked ? "La cronologia è spenta"
            : paused ? "In pausa: quello che copi non viene ricordato" : "Quello che copi comparirà qui";
        savedEmpty.Text = searching ? "Nessun risultato" : "Nessun testo salvato";
        recentEmpty.Visible = recentList.Items.Count == 0;
        savedEmpty.Visible = savedList.Items.Count == 0;
    }

    // Whether what a folder holds is in view: that folder and every one it is inside of are open. Null is
    // outside the folders, always in view.
    static bool InView(string group, Dictionary<string, bool> open)
    {
        for (; group != null; group = Texts.Parent(group))
        {
            bool opened;
            if (open.TryGetValue(group, out opened) && !opened) return false;
        }
        return true;
    }

    // Adds a row to a list; if it is the entry that was selected, it is selected again.
    static void ShowRow(ListBox list, Entry entry, Entry selected)
    {
        int index = list.Items.Add(entry);
        if (selected != null && entry.Name == selected.Name && entry.Text == selected.Text && entry.IsGroup == selected.IsGroup)
            list.SelectedIndex = index;
    }

    // Remembers which row of a list the pointer is on, and has the two rows involved drawn again.
    void Hover(ListBox list, int index)
    {
        int old = list.Tag is int ? (int)list.Tag : -1;
        if (old == index) return;
        list.Tag = index;
        list.Cursor = index >= 0 ? Cursors.Hand : Cursors.Default;
        foreach (int row in new int[] { old, index })
            if (row >= 0 && row < list.Items.Count) list.Invalidate(list.GetItemRectangle(row));
        // What was in view belonged to the row the pointer left; a copied entry is shown whole if it rests there.
        if (!peekAsked || index < 0) HidePeek();
        if (index < 0 || list != recentList) return;
        rest.Interval = RestMs;
        rest.Start();
    }

    // The row of a list at a point, or -1: the list itself answers with the nearest one, also below the last.
    static int RowAt(ListBox list, Point where)
    {
        int index = list.IndexFromPoint(where);
        return index >= 0 && index < list.Items.Count && list.GetItemRectangle(index).Contains(where) ? index : -1;
    }

    // Shows whole the entry of a row, in a small window under it. A text its row already shows whole needs
    // none, unless it was asked for: then it stays in view for a while, wherever the pointer goes.
    void ShowPeek(ListBox list, int row, bool asked)
    {
        Entry entry = (Entry)list.Items[row];
        Rectangle area = list.GetItemRectangle(row);
        if (entry.IsGroup || !asked && !entry.Picture && !Cut(list, entry, area)) return;
        peek.Reveal(entry, list.RectangleToScreen(area), Zoom);
        peekAsked = asked && peek.Visible;
        if (!peekAsked) return;
        rest.Interval = AskedMs;
        rest.Start();
    }

    void HidePeek()
    {
        rest.Stop();
        peekAsked = false;
        if (peek.Visible) peek.Hide();
    }

    // Whether the row of a copied text shows less than the text: more lines, or more than fits.
    bool Cut(ListBox list, Entry entry, Rectangle row)
    {
        // Measured without the ellipsis of the row: asked how wide the text is, with it the answer is the dots alone.
        return entry.Label != entry.Text.Trim() || TextRenderer.MeasureText(entry.Label, Font, Size.Empty,
            LineFlags & ~TextFormatFlags.EndEllipsis).Width > Content(list, entry, row).Width - SignRoom;
    }

    // The folder a saved text dropped at a point of their column would go to: that of the row there, which
    // is none for a text outside the folders and below the last row.
    string GroupAt(Point where)
    {
        int row = RowAt(savedList, where);
        Entry entry = row < 0 ? null : (Entry)savedList.Items[row];
        return entry == null ? null : entry.IsGroup ? entry.Name : entry.Group;
    }

    // Lights up the folder a dragged text is over, as the pointer does.
    void Aim(string group)
    {
        if (dropGroup == group) return;
        dropGroup = group;
        savedList.Invalidate();
    }

    // When, as a row says it at its right: of a copied entry the time today and the day before that, of a
    // saved text always the day it was saved. The year only when it is not this one. Null if nobody knows.
    static string When(Entry entry)
    {
        if (entry.IsGroup || entry.Time == DateTime.MinValue) return null;
        return entry.Time.ToString(entry.Name == null && entry.Time.Date == DateTime.Today ? "HH:mm"
            : entry.Time.Year == DateTime.Today.Year ? "dd/MM" : "dd/MM/yy");
    }

    // What that takes of the row, with the air before it.
    int WhenRoom(Entry entry)
    {
        string when = When(entry);
        return when == null ? 0 : TimeGap + TextRenderer.MeasureText(when, Font, Size.Empty, TextFormatFlags.NoPadding).Width;
    }

    // The part of a row left to its sign and its text: up to when it was copied or saved, which comes
    // before the star in a row of the copied entries.
    Rectangle Content(ListBox list, Entry entry, Rectangle row)
    {
        int left = row.Left + RowInset + GroupIndent * entry.Level;
        int right = row.Right - TimeGap - (list == recentList ? StarArea(row).Width + TimeGap : 0) - WhenRoom(entry);
        return Rectangle.FromLTRB(left, row.Top, right, row.Bottom);
    }

    // What the sign of an entry takes at the left of its row, with the air after it.
    int SignRoom
    {
        get { return Font.Height + 2 * PreviewMargin; }
    }

    void DrawGlyph(object sender, PaintEventArgs e)
    {
        Control frame = (Control)sender;
        int side = Font.Height;
        Look.Draw(e.Graphics, Sign.Search, new Rectangle(RowInset - 3, (frame.Height - side) / 2, side, side), null);
    }

    // The sign that tells what an entry is: a saved text, an address, any other text. A picture shows itself.
    static Sign SignOf(Entry entry)
    {
        if (entry.IsGroup) return entry.Open ? Sign.Opened : Sign.Closed;
        if (entry.Name != null) return Sign.Star;
        string start = entry.Text.TrimStart();
        foreach (string prefix in new string[] { "http://", "https://", "www." })
            if (start.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return Sign.Link;
        return Sign.Text;
    }

    Entry Selected
    {
        get { return active == null ? null : active.SelectedItem as Entry; }
    }

    ListBox Other(ListBox list)
    {
        return list == recentList ? savedList : recentList;
    }

    // Moves the selection to a column: to the entry it held last, or to its first.
    void Pick(ListBox list)
    {
        if (list.Items.Count > 0) list.SelectedIndex = Math.Max(0, list.SelectedIndex);
    }

    // The buttons under the list follow the selected entry: a copied text can be saved, a saved one changed.
    void ShowActions()
    {
        Entry entry = Selected;
        bool saved = entry != null && entry.Name != null;
        saveButton.Text = saved && entry.IsGroup ? "Rinomina…" : saved ? "Modifica…" : "Salva…";
        saveButton.Sign = saved ? Sign.Edit : Sign.Star;
        saveButton.Enabled = entry != null && !entry.Picture;
        deleteButton.Enabled = entry != null;
    }

    // A row of a list: the preview of a picture, if it is one, then the text.
    void DrawEntry(object sender, DrawItemEventArgs e)
    {
        if (e.Index < 0) return;
        ListBox list = (ListBox)sender;
        Entry entry = (Entry)list.Items[e.Index];
        bool selected = (e.State & DrawItemState.Selected) != 0;
        // Under the pointer, or the folder a dragged text would be dropped into.
        bool hovered = Equals(list.Tag, e.Index) || entry.IsGroup && dropGroup != null && Texts.SameName(entry.Name, dropGroup);
        using (Brush back = new SolidBrush(list.BackColor))
        {
            e.Graphics.FillRectangle(back, e.Bounds);
        }
        // The selected row is a rounded shape with a mark in the accent colour; the one under the pointer a fainter shape.
        if (selected || hovered)
        {
            Rectangle row = Rectangle.FromLTRB(e.Bounds.Left + 1, e.Bounds.Top + 1, e.Bounds.Right - 2, e.Bounds.Bottom - 1);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath shape = Look.Rounded(row))
            using (Brush fill = new SolidBrush(selected ? Look.Selected : Look.Soft))
            using (Brush mark = new SolidBrush(Look.Accent))
            {
                e.Graphics.FillPath(fill, shape);
                if (selected) e.Graphics.FillRectangle(mark, row.Left + 3, row.Top + 5, 3, row.Height - 10);
            }
        }
        Rectangle label = Content(list, entry, e.Bounds);
        string when = When(entry);
        if (when != null)
            TextRenderer.DrawText(e.Graphics, when, e.Font, Rectangle.FromLTRB(label.Right, label.Top, label.Right + WhenRoom(entry),
                label.Bottom), Look.Muted, TextFormatFlags.VerticalCenter | TextFormatFlags.Right | TextFormatFlags.SingleLine
                | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
        if (list == recentList)
        {
            // At the right end, the star that keeps the entry at the top: in colour if it does, faint on the
            // row where a click would light it.
            Rectangle star = StarArea(e.Bounds);
            if (entry.Pinned) Look.Draw(e.Graphics, Sign.Star, star, null);
            else if (selected || hovered) Look.Draw(e.Graphics, Sign.Star, star, Look.Blend(Look.Muted, list.BackColor, 0.4f));
        }
        if (entry.IsGroup)
        {
            // On the right of a folder, how many texts it holds, those of the folders inside it too.
            string count = entries.FindAll(delegate(Entry other) { return Texts.Within(other.Group, entry.Name); }).Count.ToString();
            int width = TextRenderer.MeasureText(e.Graphics, count, e.Font, Size.Empty, TextFormatFlags.NoPadding).Width;
            TextRenderer.DrawText(e.Graphics, count, e.Font, label, Look.Muted, TextFormatFlags.VerticalCenter
                | TextFormatFlags.Right | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
            label.Width -= width + TimeGap;
        }
        if (entry.Preview != null)
        {
            // As tall as the row allows, never wider than half of it, keeping the proportions.
            Size room = new Size(label.Width / 2, e.Bounds.Height - 2 * PreviewMargin);
            float scale = Math.Min((float)room.Width / entry.Preview.Width, (float)room.Height / entry.Preview.Height);
            Size size = new Size((int)(entry.Preview.Width * scale), (int)(entry.Preview.Height * scale));
            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            e.Graphics.DrawImage(entry.Preview, label.Left, e.Bounds.Top + (e.Bounds.Height - size.Height) / 2, size.Width, size.Height);
            label = Rectangle.FromLTRB(label.Left + size.Width + 2 * PreviewMargin, label.Top, label.Right, label.Bottom);
        }
        else
        {
            int side = e.Font.Height;
            Look.Draw(e.Graphics, SignOf(entry), new Rectangle(label.Left, e.Bounds.Top + (e.Bounds.Height - side) / 2, side, side), null);
            label = Rectangle.FromLTRB(label.Left + SignRoom, label.Top, label.Right, label.Bottom);
        }
        using (Font font = new Font(e.Font, entry.IsGroup ? FontStyle.Bold : FontStyle.Regular))   // a folder stands out
        {
            TextRenderer.DrawText(e.Graphics, entry.Label, font, label, list.ForeColor, TextFormatFlags.VerticalCenter | LineFlags);
        }
    }

    // Where the star of a copied entry is drawn, at the right end of its row.
    Rectangle StarArea(Rectangle row)
    {
        int side = Font.Height;
        return new Rectangle(row.Right - TimeGap - side, row.Top + (row.Height - side) / 2, side, side);
    }

    // Whether the pointer is on the star of a row, or in the little room around it.
    bool OnStar(ListBox list, Point where)
    {
        int index = list.IndexFromPoint(where);
        if (list != recentList || index < 0) return false;
        Rectangle row = list.GetItemRectangle(index);
        return row.Contains(where) && where.X >= StarArea(row).Left - TimeGap / 2;
    }

    // Keeps a copied entry at the top of its column, or lets it go back among the others.
    void TogglePin(Entry entry)
    {
        if (entry == null || entry.Name != null) return;
        Change(delegate { Texts.Pin(entry, !entry.Pinned); });
        Say((entry.Pinned ? "Resta in alto: " : "Non è più in alto: ") + Shorten(entry.ToString()) + ".");
        Reload();
    }

    void Step(int by)
    {
        if (active == null) return;
        int index = active.SelectedIndex + by;
        if (index >= 0 && index < active.Items.Count) active.SelectedIndex = index;
    }

    void Accept(bool leave)
    {
        Entry entry = Selected;
        if (entry != null && entry.IsGroup) ToggleGroup(entry);   // a folder is not chosen: it opens and closes
        else if (entry != null && !busy && Use(entry) && leave) GoBack();
    }

    // Makes an entry the clipboard content again: from there Ctrl+V pastes it and the hotkey types it.
    bool Use(Entry entry)
    {
        try
        {
            DataObject data = new DataObject();
            // A saved text may be a password: marked so that Windows clipboard history and other monitors skip it.
            if (entry.Name != null) data.SetData(Texts.PrivateMarker, new MemoryStream(new byte[sizeof(int)]));
            if (!entry.Picture)
            {
                data.SetText(entry.Text);
                Clipboard.SetDataObject(data, true);
            }
            else
            {
                using (MemoryStream png = new MemoryStream(entry.LoadPng()))
                using (Image picture = Image.FromStream(png))
                {
                    data.SetImage(picture);
                    Clipboard.SetDataObject(data, true);   // 'true' copies the picture, which can then be released
                }
            }
            ownCopy = Native.GetClipboardSequenceNumber();
        }
        catch (Exception ex)   // the clipboard locked by another program, or a picture the history file no longer gives
        {
            status.Text = "Errore: " + ex.Message;
            return false;
        }
        Say("Negli appunti: " + Shorten(entry.ToString()) + ". Incollalo con Ctrl+V"
            + (hotkeyBox.Checked && !entry.Picture ? " o digitalo con " + HotkeyName : "") + ".");
        return true;
    }

    // The list hotkey: shows the window with the list, remembering where the user was.
    void OpenList()
    {
        if (busy) return;
        IntPtr foreground = Native.GetForegroundWindow();
        // Pressed again with the list already in front: where to go back to is still what the first press found.
        if (foreground != Handle)
        {
            previous = IsValidTarget(foreground) ? foreground : IntPtr.Zero;
            wasHidden = !Visible;
        }
        search.Clear();
        ShowView(false);
        RestoreWindow();
        search.Focus();
    }

    // After the list hotkey: gives the foreground back to the window the user came from, and hides the
    // window again if that is how the hotkey found it. The foreground is handed over before hiding, while
    // this program still has the right to assign it.
    void GoBack()
    {
        if (previous == IntPtr.Zero) return;
        Native.SetForegroundWindow(previous);
        previous = IntPtr.Zero;
        if (wasHidden) Hide();
    }

    // Opens the window with name and content: for a new text ('source' is null), to keep a copied one,
    // or to change a saved one.
    void Edit(Entry source)
    {
        if (source != null && source.Picture) return;
        if (source != null && source.IsGroup)
        {
            EditGroup(source.Name);
            return;
        }
        // A new text is proposed for the folder the selection is in; any text can then be put elsewhere.
        Entry near = Selected;
        string group = source != null ? source.Group : near == null ? null : near.IsGroup ? near.Name : near.Group;
        using (EditForm edit = new EditForm(source, group, GroupNames()))
        {
            // Asked again, with what was written still there, if the name turns out to be taken.
            while (edit.ShowDialog(this) == DialogResult.OK)
            {
                string name = edit.EntryName, oldName = source == null ? null : source.Name;
                bool renamed = !Texts.SameName(name, oldName);
                bool taken = renamed && entries.Exists(delegate(Entry entry) { return !entry.IsGroup && Texts.SameName(entry.Name, name); });
                if (taken && !Ask("«" + name + "» esiste già. Vuoi sostituirlo?")) continue;
                if (!Change(delegate
                {
                    Texts.Save(name, edit.Content, edit.Group);   // first, so that a failure leaves the old one in place
                    if (renamed && oldName != null) Texts.Delete(oldName);
                    Texts.Reveal(edit.Group);   // or the text would not be seen
                })) continue;
                if (source != null && source.Name == null) Texts.Remove(source);   // it now has a line among the saved ones
                Say("Salvato «" + Shorten(name) + "».");
                break;
            }
        }
        Reload();
    }

    // The folders of the saved texts, by path, each just before those inside it.
    List<string> GroupNames()
    {
        return entries.FindAll(delegate(Entry entry) { return entry.IsGroup; }).ConvertAll(delegate(Entry entry) { return entry.Name; });
    }

    // The folder a saved text or a folder is in, or null for none.
    static string PlaceOf(Entry entry)
    {
        return entry.IsGroup ? Texts.Parent(entry.Name) : entry.Group;
    }

    // Whether a saved text or a folder can go into a folder (null: out of them all). Not where it is already,
    // and a folder neither into itself nor into one it holds.
    static bool CanGo(Entry entry, string group)
    {
        return !Texts.SameName(group, PlaceOf(entry)) && !(entry.IsGroup && Texts.Within(group, entry.Name));
    }

    // Says why a folder cannot be called 'name' inside 'parent', or returns false if it can. 'old' is the path
    // it has now, null for a new one.
    bool Refused(string name, string parent, string old)
    {
        string path = Texts.Join(parent, name);
        string why = name.IndexOf(Texts.Separator) >= 0 ? "Il nome di una cartella non può contenere «" + Texts.Separator + "»."
            : !Texts.SameName(path, old) && GroupNames().Exists(delegate(string other) { return Texts.SameName(other, path); })
            ? "La cartella «" + Texts.Display(path) + "» esiste già." : null;
        if (why != null) MessageBox.Show(this, why, Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        return why != null;
    }

    // Asks the name of a new folder and where it goes, or another name or place for the given one. A new
    // folder is proposed inside the one the selection is in.
    void EditGroup(string old)
    {
        Entry near = Selected;
        string parent = old != null ? Texts.Parent(old) : near == null ? null : near.IsGroup ? near.Name : near.Group;
        List<string> parents = GroupNames().FindAll(delegate(string group) { return !Texts.Within(group, old); });
        using (EditForm edit = new EditForm(old, parent, parents))
        {
            while (edit.ShowDialog(this) == DialogResult.OK)
            {
                string path = Texts.Join(edit.Group, edit.EntryName);
                if (Refused(edit.EntryName, edit.Group, old)) continue;
                if (!Change(delegate
                {
                    if (old == null) Texts.SetGroup(path, true);
                    else Texts.RenameGroup(old, path);
                    Texts.Reveal(edit.Group);   // or the folder would not be seen
                })) continue;
                Say((old == null ? "Creata la cartella «" : Texts.SameName(Texts.Parent(path), Texts.Parent(old))
                    ? "La cartella ora si chiama «" : "La cartella ora è «") + Shorten(Texts.Display(path)) + "».");
                break;
            }
        }
        Reload();
    }

    // Puts a saved text or a folder into a folder, which opens to show it, or out of them all.
    void MoveTo(Entry entry, string group)
    {
        if (entry.IsGroup && Refused(Texts.Leaf(entry.Name), group, entry.Name)) return;
        if (!Change(delegate
        {
            if (entry.IsGroup) Texts.RenameGroup(entry.Name, Texts.Join(group, Texts.Leaf(entry.Name)));
            else Texts.Move(entry.Name, group);
            Texts.Reveal(group);
        })) return;
        string what = "«" + Shorten(entry.Label) + "»";
        Say(group == null ? what + " non è più in una cartella." : what + " è nella cartella «" + Shorten(Texts.Display(group)) + "».");
        Reload();
    }

    // Shows or hides the texts of a folder.
    void ToggleGroup(Entry group)
    {
        Change(delegate { Texts.SetGroup(group.Name, !group.Open); });
        Reload();
    }

    void DeleteSelected()
    {
        Entry entry = Selected;
        if (entry == null) return;
        if (entry.Name == null) Texts.Remove(entry);
        else if (entry.IsGroup)
        {
            // Only the folder goes: what it holds is too easily a password to be deleted along. It moves up a level.
            bool holds = entries.Exists(delegate(Entry other)
            {
                return other != entry && (Texts.Within(other.Group, entry.Name) || other.IsGroup && Texts.Within(other.Name, entry.Name));
            });
            string parent = Texts.Parent(entry.Name);
            if (!Ask("Vuoi eliminare la cartella «" + Texts.Display(entry.Name) + "»?"
                + (!holds ? "" : parent == null ? "\n\nQuello che contiene non viene eliminato: resta tra i salvati, fuori dalle cartelle."
                    : "\n\nQuello che contiene non viene eliminato: passa nella cartella «" + Texts.Display(parent) + "»."))
                || !Change(delegate { Texts.RenameGroup(entry.Name, null); })) return;
        }
        else if (!Ask("Vuoi eliminare «" + entry.Name + "»?") || !Change(delegate { Texts.Delete(entry.Name); })) return;
        Reload();
    }

    // The keys of the list, while the search box or the list itself has the focus.
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        HidePeek();
        if (capturing != null) return TakeKeys(keyData);
        if (keyData == HelpKey)   // from the options too
        {
            ShowHelp();
            return true;
        }
        if (optionsShown || !(search.Focused || recentList.Focused || savedList.Focused)) return base.ProcessCmdKey(ref msg, keyData);
        switch (keyData)
        {
            case Keys.Escape:
                GoBack();
                return true;
            case Keys.Enter:
                enterDown = true;
                return true;
            case Keys.Tab:
                if (active != null) Pick(Other(active));
                return true;
            case Keys.Up:
                Step(-1);
                return true;
            case Keys.Down:
                Step(1);
                return true;
            case NewKeys:
                Edit(null);
                return true;
            case GroupKeys:
                EditGroup(null);
                return true;
            case SaveKeys:
                Edit(Selected);
                return true;
            case DeleteKeys:
                DeleteSelected();
                return true;
            case PinKeys:
                TogglePin(Selected);
                return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    // Enter acts when released: held down, it would go on repeating into the window the foreground goes
    // back to, and could confirm a login there.
    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        if (e.KeyCode != Keys.Enter || !enterDown) return;
        enterDown = false;
        Accept(true);
    }

    protected override void OnDeactivate(EventArgs e)
    {
        base.OnDeactivate(e);
        enterDown = false;
        HidePeek();
        EndCapture();   // keys pressed elsewhere are not for us
    }

    // Gives the column of the saved texts the asked width, as far as both columns stay usable; the copied
    // ones take the rest of the window. Everything is measured from the window and from what never moves
    // (the left margin, the width of the divider), not from where the columns are: so they cannot drift.
    void PlaceColumns(int width)
    {
        if (WindowState == FormWindowState.Minimized) return;
        int gap = divider.Width, right = ClientSize.Width - recentCard.Left, least = (int)(MinColumn * Zoom);
        width = Math.Max(least, Math.Min(width, right - recentCard.Left - gap - least));
        savedCard.SetBounds(right - width, savedCard.Top, width, savedCard.Height);
        recentCard.Width = savedCard.Left - gap - recentCard.Left;
        savedTitle.Left = savedCard.Left;
        recentCount.Left = recentCard.Right - recentCount.Width;
        divider.Left = recentCard.Right;
        savedColumn = (int)Math.Round(width / Zoom);
    }

    // A short line in the middle of the room between the columns: the sign that it can be dragged.
    void DrawGrip(object sender, PaintEventArgs e)
    {
        int height = (int)(GripHeight * Zoom);
        using (Pen pen = new Pen(Look.Border, 2 * Zoom))
        {
            pen.StartCap = pen.EndCap = LineCap.Round;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.DrawLine(pen, divider.Width / 2f, (divider.Height - height) / 2f, divider.Width / 2f, (divider.Height + height) / 2f);
        }
    }
}
