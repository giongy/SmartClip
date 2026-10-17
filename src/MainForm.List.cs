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
        ToolStripItem showItem = listMenu.Items.Add(Strings.ShowContent, null, delegate
        {
            if (savedList.SelectedIndex >= 0) ShowPeek(savedList, savedList.SelectedIndex, true);
        });
        // Only for a copied entry: the same as a click on its arrow.
        ToolStripItem pinItem = listMenu.Items.Add("", null, delegate { TogglePin(Selected); });
        // The number that chooses the entry after the list hotkey: the numbers are listed when the menu opens.
        ToolStripMenuItem numberItem = new ToolStripMenuItem(Strings.NumberMenu);
        listMenu.Items.Add(numberItem);
        // Only for a saved text, once there are folders: they are listed when the menu opens.
        ToolStripMenuItem moveItem = new ToolStripMenuItem(Strings.MoveTo);
        listMenu.Items.Add(moveItem);
        ToolStripItem deleteItem = listMenu.Items.Add(deleteButton.Text, null, delete);
        listMenu.Items.Add(newButton.Text, null, create);
        listMenu.Items.Add(Strings.NewFolder, null, delegate { EditGroup(null); });
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
            showItem.Visible = entry != null && entry.Saved && !entry.IsGroup && entry.Name != null;
            pinItem.Visible = entry != null && !entry.Saved;
            pinItem.Text = entry != null && entry.Pinned ? Strings.Unpin : Strings.Pin;
            ListNumbers(numberItem, entry);
            // A saved text or a folder, wherever else it can go: a folder never into itself or one it holds.
            List<string> groups = entry == null ? new List<string>()
                : GroupNames().FindAll(delegate(string group) { return !entry.IsGroup || !Texts.Within(group, entry.Name); });
            // Asked back, 'Visible' would answer false: the menu is not on screen yet.
            bool movable = entry != null && entry.Saved && (groups.Count > 0 || PlaceOf(entry) != null);
            moveItem.Visible = movable;
            moveItem.DropDownItems.Clear();
            if (!movable) return;
            // The place where it already is cannot be chosen.
            moveItem.DropDownItems.Add(Strings.OutOfFolders, null, delegate { MoveTo(entry, null); }).Enabled = PlaceOf(entry) != null;
            foreach (string each in groups)
            {
                string group = each;   // a variable of its own for the delegate
                moveItem.DropDownItems.Add(Texts.Display(group), null, delegate { MoveTo(entry, group); }).Enabled = CanGo(entry, group);
            }
        };
        Look.Style((ToolStripDropDownMenu)moveItem.DropDown);
        Look.Style((ToolStripDropDownMenu)numberItem.DropDown);
        Look.Style(listMenu);

        foreach (RowList each in new RowList[] { recentList, savedList })
        {
            RowList list = each;   // a variable of its own for the delegates below
            Look.Field(list);
            list.IntegralHeight = false;
            list.BorderStyle = BorderStyle.None;
            list.DrawMode = DrawMode.OwnerDrawVariable;
            list.Font = Look.EntryFont;   // the entries have a font of their own, chosen in the options
            list.ItemHeight = EntryHeight;
            list.ContextMenuStrip = listMenu;
            list.MeasureItem += delegate(object sender, MeasureItemEventArgs e)
            {
                e.ItemHeight = ((Entry)list.Items[e.Index]).Picture ? list.Font.Height * PictureLines : list.ItemHeight;
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
                int index = list.RowAt(e.Location);
                if (e.Button == MouseButtons.Right && index >= 0) list.SelectedIndex = index;
                // Windows announces the row taken by a click only when the button is released, after the
                // click has been handled: without this the click would act on the entry selected before.
                taken(list, EventArgs.Empty);
            };
            // One click leaves the window where it is, so that the entry can still be saved or deleted; a
            // double click, like Enter, also goes back to the window the list hotkey was pressed in.
            // A click on the arrow of a copied entry does not choose it: it keeps it at the top, or lets it go.
            list.MouseClick += delegate(object sender, MouseEventArgs e)
            {
                int index = list.RowAt(e.Location);
                if (e.Button != MouseButtons.Left || index < 0) return;
                if (OnPin(list, e.Location)) TogglePin((Entry)list.Items[index]);
                else Accept(false);
            };
            // The second click on an arrow finds another entry there: the first one moved the row.
            // Nor does it count on a folder, which the first click has already opened or closed.
            list.MouseDoubleClick += delegate(object sender, MouseEventArgs e)
            {
                int index = list.RowAt(e.Location);
                if (e.Button == MouseButtons.Left && index >= 0 && !OnPin(list, e.Location) && !((Entry)list.Items[index]).IsGroup)
                    Accept(true);
            };
            // The entry under the pointer lights up: a click would choose it.
            list.MouseMove += delegate(object sender, MouseEventArgs e) { Hover(list, list.RowAt(e.Location)); };
            list.MouseLeave += delegate { Hover(list, -1); };
            list.MouseWheel += delegate { HidePeek(); };      // the rows move under it
            list.Resize += delegate { list.Invalidate(); };   // the rows are drawn up to the right edge
        }
        // The pointer resting on a copied entry shows it whole; one that was asked for leaves by itself.
        rest.Tick += delegate
        {
            rest.Stop();
            if (peek.Visible || resting == null)
            {
                HidePeek();
                return;
            }
            int row = resting.Glow.Row;
            if (row >= 0 && row < resting.Items.Count && !listMenu.Visible && MouseButtons == MouseButtons.None
                && resting.RectangleToScreen(resting.GetItemRectangle(row)).Contains(Cursor.Position))
                ShowPeek(resting, row, false);
        };

        // A saved text, or a folder with all it holds, is dragged to a folder: onto the folder itself or onto
        // a text it holds. Dropped on a text outside the folders, or below them all, it leaves the one it was in.
        savedList.AllowDrop = true;
        savedList.MouseDown += delegate(object sender, MouseEventArgs e)
        {
            int index = savedList.RowAt(e.Location);
            grabbed =e.Button == MouseButtons.Left && index >= 0 ? (Entry)savedList.Items[index] : null;
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

    // Everything again, the saved entries too: after anything that changed them.
    void Reload()
    {
        Change(delegate { entries = LoadSaved(); });
        ReloadCopied();
    }

    // The copied entries again, as after a copy: the saved ones are left as they are, not read and decrypted anew.
    void ReloadCopied()
    {
        Filter();
        HookNumbers();   // a number may have come or gone, with its entry
    }

    // The saved entries as their column shows them: those outside the folders first, then each folder right
    // before its own entries and then the folders inside it. A folder is a row too (IsGroup), named by its path.
    List<Entry> LoadSaved()
    {
        List<Folder> folders = clips.Folders();
        Dictionary<long, string> paths = new Dictionary<long, string>();
        Dictionary<long, List<Entry>> inside = new Dictionary<long, List<Entry>>();
        foreach (Folder folder in folders)   // each after the one it is in
        {
            paths[folder.Id] = folder.ParentId == 0 ? folder.Name : Texts.Join(paths[folder.ParentId], folder.Name);
            inside[folder.Id] = new List<Entry>();
        }
        List<Entry> saved = new List<Entry>();
        foreach (Entry entry in clips.Saved())
        {
            List<Entry> holder;
            if (entry.FolderId != 0 && inside.TryGetValue(entry.FolderId, out holder)) entry.Group = paths[entry.FolderId];
            else holder = saved;
            holder.Add(entry);
        }
        foreach (Folder folder in folders)
        {
            Entry row = new Entry(paths[folder.Id], "", DateTime.MinValue);
            row.IsGroup = true;
            row.Saved = true;   // it belongs with the saved ones
            row.Open = folder.Open;
            row.FolderId = folder.Id;
            saved.Add(row);
            saved.AddRange(inside[folder.Id]);
        }
        return saved;
    }

    // The id of the folder at a path the list knows, or 0 for none.
    long FolderAt(string path)
    {
        return clips.FindFolder(path);
    }

    void Filter()
    {
        // The lists are refilled at every clipboard change, also the one caused by choosing an entry:
        // the selection must stay on the same entry, not jump back to the first.
        Entry selected = Selected;
        HidePeek();   // the row it belongs to may be about to move
        string[] words = Texts.Words(search.Text);   // taken from the box once, not for each entry
        bool searching = words.Length > 0;
        // The copied entries come from the database, which searches them itself: the history can hold thousands.
        List<Entry> recent = new List<Entry>();
        int copied = 0;
        Change(delegate
        {
            recent = clips.Copied(MaxShown, words);
            copied = clips.CopiedCount();
        });
        // What the titles of the columns and the rows of the folders count: the saved texts, not the folders,
        // those in a closed folder too. Counted once here rather than at every row drawn.
        int saved = 0;
        held.Clear();
        foreach (Entry entry in entries)
        {
            if (!entry.IsGroup)
            {
                saved++;
                for (string group = entry.Group; group != null; group = Texts.Parent(group))
                {
                    int count;
                    held.TryGetValue(group, out count);
                    held[group] = count + 1;
                }
            }
        }
        // Filled again for a change that keeps the selection (a copy, a folder opened), the columns stay where they
        // were scrolled to; a new search starts from the top.
        int recentTop = recentList.TopIndex, savedTop = savedList.TopIndex;
        recentList.BeginUpdate();
        savedList.BeginUpdate();
        recentList.Items.Clear();
        savedList.Items.Clear();
        foreach (Entry entry in recent) ShowRow(recentList, entry, selected);
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
            if (!Texts.Matches(entry, words)) continue;
            if (entry.Group != null)
            {
                if (!searching && !InView(entry.Group, open)) continue;
                foreach (Entry folder in waiting) ShowRow(savedList, folder, selected);
                waiting.Clear();
            }
            ShowRow(savedList, entry, selected);
        }
        if (selected != null)
        {
            Rescroll(recentList, recentTop);
            Rescroll(savedList, savedTop);
        }
        recentList.EndUpdate();
        savedList.EndUpdate();
        recentList.Glow.Reset();   // the rows changed under the pointer
        savedList.Glow.Reset();
        if (Selected == null) Pick(recentList.Items.Count > 0 ? recentList : savedList);
        ShowActions();

        recentCount.Text = recentList.Items.Count == copied ? copied.ToString()
            : string.Format(Strings.CountOf, recentList.Items.Count, copied);
        int found = 0;
        foreach (Entry entry in savedList.Items) if (!entry.IsGroup) found++;
        savedCount.Text = searching && found != saved ? string.Format(Strings.CountOf, found, saved) : saved.ToString();
        recentEmpty.Text = searching ? Strings.NoResults : !historyBox.Checked ? Strings.HistoryOffEmpty
            : paused ? Strings.PausedEmpty : Strings.CopyHint;
        savedEmpty.Text = searching ? Strings.NoResults : Strings.NoSaved;
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

    // Scrolls a list filled again back to where it was, as far as its selected row stays in view: an entry just
    // pinned, for example, has gone up to the top.
    static void Rescroll(ListBox list, int top)
    {
        if (list.Items.Count == 0) return;
        list.TopIndex = Math.Min(top, list.Items.Count - 1);
        int index = list.SelectedIndex;
        if (index < 0) return;
        Rectangle row = list.GetItemRectangle(index);
        if (row.Top < 0 || row.Bottom > list.ClientSize.Height) list.TopIndex = index;
    }

    // Adds a row to a list; if it is the entry that was selected, it is selected again: an entry by its row in the
    // database, a folder by its path.
    static void ShowRow(ListBox list, Entry entry, Entry selected)
    {
        int index = list.Items.Add(entry);
        if (selected != null && entry.IsGroup == selected.IsGroup && (entry.IsGroup ? entry.Name == selected.Name : entry.Id == selected.Id))
            list.SelectedIndex = index;
    }

    // Remembers which row of a list the pointer is on, and has the two rows involved drawn again.
    void Hover(RowList list, int index)
    {
        if (list.Glow.Row == index) return;
        list.Cursor = index >= 0 ? Cursors.Hand : Cursors.Default;
        list.Glow.Set(index);   // the row lights up gradually, the one left fades
        // What was in view belonged to the row the pointer left; an entry whose content the list shows (a copied one,
        // a saved one without a name) is shown whole if it rests there. One with a name only when asked.
        if (!peekAsked || index < 0) HidePeek();
        if (index < 0 || list != recentList && ((Entry)list.Items[index]).Name != null) return;
        resting = list;
        rest.Interval = RestMs;
        rest.Start();
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
        return entry.Label != entry.Text.Trim() || TextRenderer.MeasureText(entry.Label, list.Font, Size.Empty,
            LineFlags & ~TextFormatFlags.EndEllipsis).Width > Content(list, entry, row).Width - SignRoom;
    }

    // The folder a saved text dropped at a point of their column would go to: that of the row there, which
    // is none for a text outside the folders and below the last row.
    string GroupAt(Point where)
    {
        int row = savedList.RowAt(where);
        return row < 0 ? null : GroupOf((Entry)savedList.Items[row]);
    }

    // The folder an entry stands for or is in: a folder itself, the one of a saved text. Null for none.
    static string GroupOf(Entry entry)
    {
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
        if (!Dated(entry)) return null;
        return entry.Time.ToString(!entry.Saved && entry.Time.Date == DateTime.Today ? "HH:mm"
            : entry.Time.Year == DateTime.Today.Year ? Strings.DayFormat : Strings.YearDayFormat, Strings.Culture);
    }

    // Whether a row says when: not a folder, nor a text saved before the days were kept.
    static bool Dated(Entry entry)
    {
        return !entry.IsGroup && entry.Time != DateTime.MinValue;
    }

    // A row of the lists, but for a picture: the line of the entry, and the small one of when it was copied or
    // saved under it. Every row has room for both, also those that do not say when.
    static int EntryHeight
    {
        get { return Look.EntryFont.Height + Look.CaptionFont.Height + RowPadding; }
    }

    // The first line of a row, where its sign, its text, its number and its arrow are. A row that does not say when,
    // and a picture, which is taller, have them in the middle.
    static Rectangle Line(Entry entry, Rectangle row)
    {
        if (entry.Picture || !Dated(entry)) return row;
        return new Rectangle(row.Left, row.Top + RowPadding / 2, row.Width, Look.EntryFont.Height);
    }

    // The small line at the bottom of a row, where when it was copied or saved ends at the right edge, under the
    // arrow and the number.
    static Rectangle Caption(Rectangle row)
    {
        int bottom = row.Bottom - RowPadding / 2;
        return Rectangle.FromLTRB(row.Left, bottom - Look.CaptionFont.Height, row.Right - TimeGap, bottom);
    }

    // The part of the first line left to its sign and its text: up to the number, which comes before the arrow in a
    // row of the copied entries.
    Rectangle Content(ListBox list, Entry entry, Rectangle row)
    {
        Rectangle line = Line(entry, row);
        int left = row.Left + RowInset + GroupIndent * entry.Level;
        int right = NumberArea(list, entry, row).Left - (entry.Number > 0 ? TimeGap : 0);
        return Rectangle.FromLTRB(left, line.Top, right, line.Bottom);
    }

    // Where the number of an entry is drawn, near the right end of its first line, so that the numbers make a column;
    // without one, an empty area where it would end.
    Rectangle NumberArea(ListBox list, Entry entry, Rectangle row)
    {
        Rectangle line = Line(entry, row);
        int right = row.Right - TimeGap - (list == recentList ? PinArea(line).Width + TimeGap : 0);
        int side = list.Font.Height, width = entry.Number > 0 ? side + NumberPadding : 0;
        return new Rectangle(right - width, line.Top + (line.Height - side) / 2, width, side);
    }

    // What the sign of an entry takes at the left of its row, with the air after it.
    int SignRoom
    {
        get { return Look.EntryFont.Height + 2 * PreviewMargin; }
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
        if (entry.Saved) return Sign.Star;
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
        bool saved = entry != null && entry.Saved;
        saveButton.Text = saved && entry.IsGroup ? Strings.RenameDots : saved ? Strings.EditDots : Strings.SaveDots;
        saveButton.Sign = saved ? Sign.Edit : Sign.Star;
        saveButton.Enabled = entry != null && !entry.Picture;
        deleteButton.Enabled = entry != null;
    }

    // A row of a list: the preview of a picture, if it is one, then the text.
    void DrawEntry(object sender, DrawItemEventArgs e)
    {
        if (e.Index < 0) return;
        RowList list = (RowList)sender;
        Entry entry = (Entry)list.Items[e.Index];
        bool selected = (e.State & DrawItemState.Selected) != 0;
        // How lit by the pointer it is; fully, the folder a dragged text would be dropped into.
        float glow = entry.IsGroup && dropGroup != null && Texts.SameName(entry.Name, dropGroup) ? 1 : list.Glow.Of(e.Index);
        Look.DrawRow(e.Graphics, e.Bounds, list.BackColor, selected, glow);
        Rectangle label = Content(list, entry, e.Bounds), line = Line(entry, e.Bounds);
        string when = When(entry);
        if (when != null) TextRenderer.DrawText(e.Graphics, when, Look.CaptionFont, Caption(e.Bounds), Look.Muted, RightFlags);
        if (entry.Number > 0)
        {
            // The digit that chooses it after the list hotkey, in a rounded frame in the accent colour.
            Rectangle area = NumberArea(list, entry, e.Bounds);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath shape = Look.Rounded(Rectangle.FromLTRB(area.Left, area.Top, area.Right - 1, area.Bottom - 1)))
            using (Pen edge = new Pen(Look.Accent))
            {
                e.Graphics.DrawPath(edge, shape);
            }
            TextRenderer.DrawText(e.Graphics, entry.Number.ToString(), e.Font, area, Look.Accent, TextFormatFlags.HorizontalCenter
                | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
        }
        if (list == recentList)
        {
            // At the right end, the arrow that keeps the entry at the top for a while: in colour if it does, faint
            // on the row where a click would light it.
            Rectangle arrow = PinArea(line);
            if (entry.Pinned) Look.Draw(e.Graphics, Sign.Up, arrow, null);
            else if (selected || glow > 0)
                Look.Draw(e.Graphics, Sign.Up, arrow, Look.Blend(list.BackColor, Look.Blend(Look.Muted, list.BackColor, 0.4f), selected ? 1 : glow));
        }
        if (entry.IsGroup)
        {
            // On the right of a folder, how many texts it holds, those of the folders inside it too.
            int texts;
            held.TryGetValue(entry.Name, out texts);
            string count = texts.ToString();
            int width = TextRenderer.MeasureText(e.Graphics, count, e.Font, Size.Empty, TextFormatFlags.NoPadding).Width;
            TextRenderer.DrawText(e.Graphics, count, e.Font, label, Look.Muted, RightFlags);
            label.Width -= width + TimeGap;
        }
        if (entry.Preview != null)
        {
            // As tall as the row allows, never wider than half of it, keeping the proportions.
            Size room = new Size(label.Width / 2, e.Bounds.Height - 2 * PreviewMargin);
            float scale = Math.Min((float)room.Width / entry.Preview.Width, (float)room.Height / entry.Preview.Height);
            Size size = new Size((int)(entry.Preview.Width * scale), (int)(entry.Preview.Height * scale));
            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            e.Graphics.DrawImage(entry.Preview, label.Left, e.Bounds.Top + (e.Bounds.Height - size.Height) / 2, size.Width, size.Height);
            label = Rectangle.FromLTRB(label.Left + size.Width + 2 * PreviewMargin, label.Top, label.Right, label.Bottom);
        }
        else
        {
            int side = e.Font.Height;
            Look.Draw(e.Graphics, SignOf(entry), new Rectangle(label.Left, line.Top + (line.Height - side) / 2, side, side), null);
            label = Rectangle.FromLTRB(label.Left + SignRoom, label.Top, label.Right, label.Bottom);
        }
        // A folder stands out.
        TextRenderer.DrawText(e.Graphics, entry.Label, entry.IsGroup ? Look.StrongEntryFont : e.Font, label, list.ForeColor,
            TextFormatFlags.VerticalCenter | LineFlags);
    }

    // Where the arrow of a copied entry is drawn, at the right end of the first line of its row.
    Rectangle PinArea(Rectangle line)
    {
        int side = Look.EntryFont.Height;
        return new Rectangle(line.Right - TimeGap - side, line.Top + (line.Height - side) / 2, side, side);
    }

    // Whether the pointer is on the arrow of a row, or in the little room around it.
    bool OnPin(RowList list, Point where)
    {
        int index = list.RowAt(where);
        return list == recentList && index >= 0 && where.X >= PinArea(list.GetItemRectangle(index)).Left - TimeGap / 2;
    }

    // The numbers an entry can be given, in its menu: the one it has cannot be chosen, and beside each that
    // another entry has, that entry, which would lose it. Nothing for a folder.
    void ListNumbers(ToolStripMenuItem menu, Entry entry)
    {
        menu.DropDownItems.Clear();
        // Asked back, 'Visible' would answer false: the menu is not on screen yet.
        menu.Visible = entry != null && !entry.IsGroup;
        if (entry == null || entry.IsGroup) return;
        menu.DropDownItems.Add(Strings.NoNumber, null, delegate { Number(entry, 0); }).Enabled = entry.Number > 0;
        for (int each = 1; each <= MaxNumber; each++)
        {
            int number = each;   // a variable of its own for the delegate
            ToolStripMenuItem item = new ToolStripMenuItem(NumberName(number), null, delegate { Number(entry, number); });
            item.Enabled = entry.Number != number;
            Entry holder = Numbered(number);
            if (holder != null && holder.Id != entry.Id) item.ShortcutKeyDisplayString = Shorten(holder.Label);
            menu.DropDownItems.Add(item);
        }
    }

    // The entry a number chooses, or null: from the database, since a copied one may be beyond those listed.
    Entry Numbered(int number)
    {
        Entry found = null;
        Change(delegate { found = clips.Numbered(number); });
        return found;
    }

    // Gives an entry a number, taking it from the one that had it, or with zero takes its own away. A copied entry
    // keeps it until its days are over. If another program has the keys of that number, Reload says so.
    void Number(Entry entry, int number)
    {
        if (!Change(delegate { clips.Number(entry.Id, number); })) return;
        Say(number > 0 ? string.Format(Strings.NumberGiven, NumberName(number), Shorten(entry.Label))
            : string.Format(Strings.NumberTaken, Shorten(entry.Label)));
        numbersTaken = "";   // said again, even if it was said before: the user is choosing now
        Reload();
    }

    // Keeps a copied entry at the top of its column for a while, or lets it go back among the others. Either way
    // it goes when its days are over: what is to stay is saved.
    void TogglePin(Entry entry)
    {
        if (entry == null || entry.Saved) return;
        if (!Change(delegate { clips.Pin(entry.Id, !entry.Pinned); })) return;
        Say(string.Format(!entry.Pinned ? Strings.Pinned : Strings.Unpinned, Shorten(entry.ToString())));
        ReloadCopied();
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
            // A saved entry may be a password: marked so that Windows clipboard history and other monitors skip it.
            if (entry.Saved) data.SetData(Texts.PrivateMarker, new MemoryStream(new byte[sizeof(int)]));
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
        catch (Exception ex)   // the clipboard locked by another program, or a picture the database no longer gives
        {
            ShowError(ex);
            return false;
        }
        Say(string.Format(hotkeyBox.Checked && !entry.Picture ? Strings.OnClipboardOrType : Strings.OnClipboard,
            Shorten(entry.ToString()), HotkeyName));
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
        string group = source != null ? source.Group : GroupOf(Selected);
        using (EditForm edit = new EditForm(source, group, GroupNames()))
        {
            // Asked again, with what was written still there, if the name turns out to be taken.
            while (edit.ShowDialog(this) == DialogResult.OK)
            {
                string name = edit.EntryName;   // empty: none, and the list shows the content
                long other = 0;
                if (name.Length > 0 && !Change(delegate { other = clips.Named(name); })) continue;
                bool taken = other != 0 && (source == null || other != source.Id);
                if (taken && !Ask(string.Format(Strings.AskReplace, name))) continue;
                // All of it or nothing: the one replaced does not go unless this one is saved.
                if (!Change(delegate
                {
                    using (Transaction transaction = clips.Begin())
                    {
                        if (taken) clips.Remove(other);
                        long folder = FolderAt(edit.Group);
                        if (source == null) clips.AddSaved(name, edit.Content, folder, DateTime.Now);
                        else
                        {
                            // A copied entry becomes saved in its own row, number and all.
                            if (!source.Saved) clips.Save(source.Id, name, folder);
                            clips.Change(source.Id, name, edit.Content, folder);
                        }
                        clips.Reveal(folder);   // or the text would not be seen
                        transaction.Commit();
                    }
                })) continue;
                Say(string.Format(Strings.SavedSay, Shorten(name.Length > 0 ? name : new Entry(null, edit.Content).Label)));
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
        string why = name.IndexOf(Texts.Separator) >= 0 ? string.Format(Strings.FolderNoSeparator, Texts.Separator)
            : !Texts.SameName(path, old) && GroupNames().Exists(delegate(string other) { return Texts.SameName(other, path); })
            ? string.Format(Strings.FolderExists, Texts.Display(path)) : null;
        if (why != null) AskForm.Tell(this, why);
        return why != null;
    }

    // Asks the name of a new folder and where it goes, or another name or place for the given one. A new
    // folder is proposed inside the one the selection is in.
    void EditGroup(string old)
    {
        string parent = old != null ? Texts.Parent(old) : GroupOf(Selected);
        List<string> parents = GroupNames().FindAll(delegate(string group) { return !Texts.Within(group, old); });
        using (EditForm edit = new EditForm(old, parent, parents))
        {
            while (edit.ShowDialog(this) == DialogResult.OK)
            {
                string path = Texts.Join(edit.Group, edit.EntryName);
                if (Refused(edit.EntryName, edit.Group, old)) continue;
                if (!Change(delegate
                {
                    long parentId = FolderAt(edit.Group);
                    if (old == null) clips.AddFolder(parentId, edit.EntryName);
                    else clips.PlaceFolder(FolderAt(old), parentId, edit.EntryName);
                    clips.Reveal(parentId);   // or the folder would not be seen
                })) continue;
                Say(string.Format(old == null ? Strings.FolderCreated : Texts.SameName(Texts.Parent(path), Texts.Parent(old))
                    ? Strings.FolderRenamed : Strings.FolderMoved, Shorten(Texts.Display(path))));
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
            long folder = FolderAt(group);
            if (entry.IsGroup) clips.MoveFolder(entry.FolderId, folder);
            else clips.Move(entry.Id, folder);
            clips.Reveal(folder);
        })) return;
        Say(group == null ? string.Format(Strings.LeftFolder, Shorten(entry.Label))
            : string.Format(Strings.InFolder, Shorten(entry.Label), Shorten(Texts.Display(group))));
        Reload();
    }

    // Shows or hides the texts of a folder.
    // Nothing else changed: the lists are filled again from what they have, without reading the saved ones anew.
    void ToggleGroup(Entry group)
    {
        if (!Change(delegate { clips.OpenFolder(group.FolderId, !group.Open); })) return;
        group.Open = !group.Open;
        Filter();
    }

    void DeleteSelected()
    {
        Entry entry = Selected;
        if (entry == null) return;
        if (!entry.Saved)
        {
            if (!Change(delegate { clips.Remove(entry.Id); })) return;
        }
        else if (entry.IsGroup)
        {
            // Only the folder goes: what it holds is too easily a password to be deleted along. It moves up a level.
            bool holds = entries.Exists(delegate(Entry other)
            {
                return other != entry && (Texts.Within(other.Group, entry.Name) || other.IsGroup && Texts.Within(other.Name, entry.Name));
            });
            string parent = Texts.Parent(entry.Name);
            if (!Ask(string.Format(Strings.AskDeleteFolder, Texts.Display(entry.Name))
                + (!holds ? "" : parent == null ? Strings.DeleteFolderTop : string.Format(Strings.DeleteFolderUp, Texts.Display(parent))))
                || !Change(delegate { clips.DeleteFolder(entry.FolderId); })) return;
        }
        else if (!Ask(string.Format(Strings.AskDelete, entry.Label)) || !Change(delegate { clips.Remove(entry.Id); })) return;
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
