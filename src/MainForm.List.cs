using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Threading;
using System.Windows.Forms;

// A part of a card, where a click does something of its own.
enum Part { None, Body, Show, Copy, Save, Delete, Pin, More }

// The name of a day among the cards: those under it were copied, or saved, that day.
class Heading
{
    public readonly string Text;

    public Heading(string text)
    {
        Text = text;
    }

    public override string ToString()
    {
        return Text;
    }
}

// Where the parts of a card are, in its row: the card itself, with air under it; the entry, first; at the right of
// its first line its number, its arrow and the small buttons; at the bottom the small line of where, when and what,
// drawn at its right end, under the buttons.
class CardParts
{
    public Rectangle Card, Body, Meta, Number, Pin;
    public int Least;   // how tall what is on the right of the entry is, from the top of the entry
    public readonly Dictionary<Part, Rectangle> Buttons = new Dictionary<Part, Rectangle>();
}

// The entries of the place chosen in the sidebar, as cards: filling them, drawing them, what a click on each part
// does; choosing, saving and moving entries, the keys of the list.
partial class MainForm
{
    // The categories of the tabs above the cards, in their order: all of them first.
    static readonly Category?[] TabKinds = { null, Category.Text, Category.Code, Category.Link, Category.Picture };
    List<object> rows = new List<object>();   // what the cards show: entries, and the names of their days
    bool headed;                               // the cards are under the names of their days: each says its time only
    int lastCard = -1;                         // the card selected last, where a click on the name of a day leaves it
    bool newest;                               // the next filling selects the latest copy, with the top of the list in view

    // What the cards answer to: buttons, right-click menu, mouse, dragging a saved text into a folder.
    void WireLists()
    {
        EventHandler create = delegate { Edit(null); };
        EventHandler change = delegate { Edit(Selected); };
        EventHandler delete = delegate { DeleteSelected(); };
        newButton.Click += create;
        ToolStripItem changeItem = listMenu.Items.Add("", null, change);
        // The whole entry, for a while: what the card cuts short, or the content of a saved text it shows by its name.
        ToolStripItem showItem = listMenu.Items.Add(Strings.ShowFull, null, delegate { PeekSelected(); });
        // Only for a picture: the text Windows reads in it.
        ToolStripItem readItem = listMenu.Items.Add(Strings.CopyPictureText, null, delegate { CopyText(Selected); });
        // Only for a copied entry: the same as a click on its arrow.
        ToolStripItem pinItem = listMenu.Items.Add("", null, delegate { TogglePin(Selected); });
        // The number that pastes the entry from anywhere: the numbers are listed when the menu opens.
        ToolStripMenuItem numberItem = new ToolStripMenuItem(Strings.NumberMenu);
        listMenu.Items.Add(numberItem);
        // Only for a text: what it is, when the guess is wrong.
        ToolStripMenuItem kindItem = new ToolStripMenuItem(Strings.KindMenu);
        listMenu.Items.Add(kindItem);
        // Only for a saved text, once there are folders: they are listed when the menu opens.
        ToolStripMenuItem moveItem = new ToolStripMenuItem(Strings.MoveTo);
        listMenu.Items.Add(moveItem);
        ToolStripItem deleteItem = listMenu.Items.Add(Strings.Delete, null, delete);
        ToolStripItem newItem = listMenu.Items.Add(Strings.New, null, create);
        // Each with its sign, as in the menus of Windows 11.
        Look.Mark(showItem, Sign.Show);
        Look.Mark(readItem, Sign.Text);
        Look.Mark(pinItem, Sign.Up);
        Look.Mark(numberItem, Sign.Keys);
        Look.Mark(kindItem, Sign.Code);
        Look.Mark(moveItem, Sign.Move);
        Look.Mark(deleteItem, Sign.Delete);
        Look.Mark(newItem, Sign.Plus);
        // With nothing selected the menu still opens: a first text can be written from an empty list.
        listMenu.Opening += delegate
        {
            HidePeek();
            Entry entry = Selected;
            changeItem.Text = entry != null && entry.Saved ? Strings.EditDots : Strings.SaveDots;
            Look.Mark(changeItem, entry != null && entry.Saved ? Sign.Edit : Sign.Star);
            changeItem.Visible = deleteItem.Visible = entry != null;
            showItem.Visible = entry != null;
            if (entry != null) showItem.Text = ShowText(entry);
            readItem.Visible = entry != null && entry.Picture;
            pinItem.Visible = entry != null && !entry.Saved;
            pinItem.Text = entry != null && entry.Pinned ? Strings.Unpin : Strings.Pin;
            ListNumbers(numberItem, entry);
            ListKinds(kindItem, entry);
            ListPlaces(moveItem, entry);
        };
        Look.Style((ToolStripDropDownMenu)moveItem.DropDown);
        Look.Style((ToolStripDropDownMenu)numberItem.DropDown);
        Look.Style((ToolStripDropDownMenu)kindItem.DropDown);
        Look.Style(listMenu);

        Look.Field(cardList);
        cardList.IntegralHeight = false;
        cardList.BorderStyle = BorderStyle.None;
        cardList.DrawMode = DrawMode.OwnerDrawVariable;
        cardList.Font = Look.EntryFont;   // the entries have a font of their own, chosen in the options
        cardList.Marked = false;          // a selected card shows it by its edge
        cardList.ContextMenuStrip = listMenu;
        cardList.MeasureItem += delegate(object sender, MeasureItemEventArgs e)
        {
            e.ItemHeight = CardHeight(cardList.Items[e.Index], cardList.ClientSize.Width);
        };
        cardList.DrawItem += DrawEntry;
        // The name of a day is not chosen: the selection stays where it was.
        cardList.SelectedIndexChanged += delegate
        {
            if (cardList.SelectedItem is Heading) cardList.SelectedIndex = lastCard < cardList.Items.Count ? lastCard : -1;
            else lastCard = cardList.SelectedIndex;
        };
        // The left button acts, the right one only selects, so that its menu acts on the card under the pointer.
        cardList.MouseDown += delegate(object sender, MouseEventArgs e)
        {
            HidePeek();
            HideHoverTip();
            int index = cardList.RowAt(e.Location);
            if (e.Button == MouseButtons.Right && index >= 0 && !(cardList.Items[index] is Heading)) cardList.SelectedIndex = index;
            // A saved text may be dragged to a folder of the sidebar.
            Entry entry = index < 0 ? null : cardList.Items[index] as Entry;
            grabbed = e.Button == MouseButtons.Left && entry != null && entry.Saved && PartAt(index, e.Location) == Part.Body ? entry : null;
            grabbedAt = e.Location;
        };
        Draggable(cardList);
        // A click on a button of a card does what it says; anywhere else on the card it only selects it (wanted by
        // the user: the entry goes on the clipboard from Copy, a double click or Enter). Copy leaves the window where
        // it is, so that the entry can still be saved or deleted; a double click, like Enter, also goes back to the
        // window the list hotkey was pressed in.
        cardList.MouseClick += delegate(object sender, MouseEventArgs e)
        {
            int index = cardList.RowAt(e.Location);
            if (e.Button != MouseButtons.Left || index < 0 || cardList.Items[index] is Heading) return;
            Entry entry = (Entry)cardList.Items[index];
            switch (PartAt(index, e.Location))
            {
                case Part.Show: ShowPeek(cardList, index, true); break;
                case Part.Copy: Use(entry); break;
                case Part.Save: Edit(entry); break;
                case Part.Delete: DeleteSelected(); break;
                case Part.Pin: TogglePin(entry); break;
                case Part.More: listMenu.Show(cardList, e.Location); break;
            }
        };
        cardList.MouseDoubleClick += delegate(object sender, MouseEventArgs e)
        {
            int index = cardList.RowAt(e.Location);
            if (e.Button == MouseButtons.Left && index >= 0 && PartAt(index, e.Location) == Part.Body) Accept();
        };
        // The card under the pointer lights up, and the part of it a click would act on.
        cardList.MouseMove += delegate(object sender, MouseEventArgs e)
        {
            int index = cardList.RowAt(e.Location);
            Part part = PartAt(index, e.Location);
            cardList.Cursor = part != Part.None && part != Part.Body ? Cursors.Hand : Cursors.Default;   // the card itself is only selected
            Hover(index < 0 || cardList.Items[index] is Heading ? -1 : index);
            if (index == hoverRow && part == hoverPart) return;
            if (hoverRow >= 0 && hoverRow < cardList.Items.Count) cardList.Invalidate(cardList.GetItemRectangle(hoverRow));
            hoverRow = index;
            hoverPart = part;
            if (index >= 0) cardList.Invalidate(cardList.GetItemRectangle(index));
            // The small buttons are signs alone: their tooltip, under each, says what they do.
            Entry entry = index < 0 ? null : cardList.Items[index] as Entry;
            string text = entry == null ? null : PartText(part, entry);
            if (text == null)
            {
                HideHoverTip();
                return;
            }
            CardParts parts = Lay(entry, cardList.GetItemRectangle(index));
            Rectangle button = part == Part.Pin ? parts.Pin : parts.Buttons[part];
            HoverTip(cardList, index * 16 + (int)part, text, new Point(button.Left + (button.Width - Look.TipSize(text).Width) / 2,
                button.Bottom + (int)(Gap * Zoom)));
        };
        cardList.MouseLeave += delegate
        {
            HideHoverTip();
            Hover(-1);
            if (hoverRow >= 0 && hoverRow < cardList.Items.Count) cardList.Invalidate(cardList.GetItemRectangle(hoverRow));
            hoverRow = -1;
        };
        cardList.MouseWheel += delegate { HidePeek(); HideHoverTip(); };   // the cards move under it
        // Wider or narrower, a text may take another number of lines: the cards are measured again.
        int measuredWidth = 0;
        cardList.Resize += delegate
        {
            if (cardList.ClientSize.Width == measuredWidth || !shown) return;
            measuredWidth = cardList.ClientSize.Width;
            ShowRows(Selected);
        };
        // The pointer resting on a card shows the entry whole, if the option says so; one that was asked for leaves by
        // itself.
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

        // A new search, or another category, starts again from its best result.
        search.TextChanged += delegate { cardList.ClearSelected(); Filter(); };
        tabs.SelectedChanged += delegate { cardList.ClearSelected(); Filter(); };
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

    // The saved entries, each folder right before its own entries and then the folders inside it. A folder is an
    // entry too (IsGroup), named by its path: the sidebar and the menus of the icon and of the cards list them.
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

    // The cards of the place chosen in the sidebar, of the category of the tabs, with the words of the search: the
    // copied entries from the database, which searches them itself (the history can hold thousands), the saved ones
    // from those in memory, whose content only this program can read.
    void Filter()
    {
        Entry selected = Selected;
        HidePeek();   // the card it belongs to may be about to move
        string[] words = Texts.Words(search.Text);   // taken from the box once, not for each entry
        Category? kind = TabKinds[tabs.Selected];
        List<Entry> copied = new List<Entry>();
        int total = 0;
        if (place.HasCopied)
        {
            Sieve sieve = new Sieve();
            sieve.Words = words;
            sieve.Category = kind;
            sieve.Pinned = place.Kind == PlaceKind.Pinned;
            sieve.App = place.Kind == PlaceKind.Source ? place.Id : 0;
            Change(delegate
            {
                copied = clips.Copied(MaxShown, sieve);
                total = clips.CopiedCount(sieve);
            });
        }
        List<Entry> saved = new List<Entry>();
        if (place.HasSaved)
            foreach (Entry entry in entries)
                if (!entry.IsGroup && (place.Kind != PlaceKind.Folder || Texts.Within(entry.Group, place.Path))
                    && (!kind.HasValue || entry.Category == kind.Value) && Texts.Matches(entry, words))
                    saved.Add(entry);
        total += saved.Count;
        rows = Arrange(copied, saved);
        ShowRows(selected);

        ShowTitle();
        int shownCount = copied.Count + saved.Count;
        count.Text = shownCount == total ? total.ToString() : string.Format(Strings.CountOf, shownCount, total);
        // What the copied pictures take may have changed: in the red bar, and in the options if they are open.
        ShowPictureRoom();
        if (options.Visible) ShowSize();
        bool searching = words.Length > 0;
        emptyNote.Text = searching ? Strings.NoResults : kind.HasValue ? Strings.NoneOfKind
            : place.Kind == PlaceKind.Pinned ? Strings.NoPinned
            : place.Kind == PlaceKind.Folder ? Strings.EmptyFolder : place.Kind == PlaceKind.Saved ? Strings.NoSaved
            : !historyBox.Checked ? Strings.HistoryOffEmpty : paused ? Strings.PausedEmpty : Strings.CopyHint;
        emptyNote.Sign = searching ? Sign.Search : kind.HasValue ? Sign.Search : place.Kind == PlaceKind.Pinned ? Sign.Up
            : place.HasSaved && place.Kind != PlaceKind.All ? Sign.Star
            : !historyBox.Checked ? Sign.History : paused ? Sign.Pause : Sign.Paste;
        emptyNote.Visible = shownCount == 0;
        FillNav();   // the counts of the sidebar, and the programs, may have changed
    }

    // The cards in their order. In Saved and its folders the texts by name; elsewhere those kept at the top first,
    // then the others from the newest, each day under its name.
    List<object> Arrange(List<Entry> copied, List<Entry> saved)
    {
        List<object> arranged = new List<object>();
        headed = place.HasCopied;
        if (!headed)
        {
            saved.Sort(delegate(Entry a, Entry b) { return StringComparer.CurrentCultureIgnoreCase.Compare(a.Label, b.Label); });
            arranged.AddRange(saved.ToArray());
            return arranged;
        }
        List<Entry> rest = new List<Entry>();
        foreach (Entry entry in copied)
        {
            if (entry.Pinned && place.Kind != PlaceKind.Pinned)
            {
                if (arranged.Count == 0) arranged.Add(new Heading(Strings.PinnedPlace));
                arranged.Add(entry);
            }
            else rest.Add(entry);
        }
        rest.AddRange(saved);
        // The newest first; those nobody knows the day of last. Sorted as a whole, the order of equal times by row.
        rest.Sort(delegate(Entry a, Entry b)
        {
            int byTime = b.Time.CompareTo(a.Time);
            return byTime != 0 ? byTime : b.Id.CompareTo(a.Id);
        });
        string day = null;
        foreach (Entry entry in rest)
        {
            string name = DayName(entry.Time);
            if (name != day) arranged.Add(new Heading(name));
            day = name;
            arranged.Add(entry);
        }
        return arranged;
    }

    // How a day is named over its cards: today, yesterday, or the day itself as the language writes it.
    static string DayName(DateTime time)
    {
        if (time == DateTime.MinValue) return Strings.Undated;
        if (time.Date == DateTime.Today) return Strings.Today;
        if (time.Date == DateTime.Today.AddDays(-1)) return Strings.Yesterday;
        string day = time.ToString(time.Year == DateTime.Today.Year ? "M" : "D", Strings.Culture);
        return day.Substring(0, 1).ToUpper(Strings.Culture) + day.Substring(1);
    }

    // Fills the cards with the rows of the place; the entry that was selected is selected again, and the list stays
    // where it was scrolled to as far as that entry stays in view. Without one, the first entry is. After a copy and
    // the list hotkey (newest), the latest copied entry is, also when kept at the top under others, and the list
    // shows its top (wanted by the user on 19/10/2026: the card selected before kept the new copy out of view).
    void ShowRows(Entry selected)
    {
        bool toNewest = newest;
        newest = false;
        int top = cardList.TopIndex;
        cardList.BeginUpdate();
        cardList.Items.Clear();
        int found = -1, first = -1, latest = -1;
        foreach (object row in rows)
        {
            int index = cardList.Items.Add(row);
            Entry entry = row as Entry;
            if (entry == null) continue;
            if (first < 0) first = index;
            if (selected != null && entry.Id == selected.Id) found = index;
            if (!entry.Saved && (latest < 0 || entry.Time > ((Entry)rows[latest]).Time)) latest = index;
        }
        lastCard = -1;
        if (toNewest && latest >= 0)
        {
            cardList.SelectedIndex = latest;
            Rescroll(cardList, 0);
        }
        else
        {
            cardList.SelectedIndex = found >= 0 ? found : first;
            if (found >= 0) Rescroll(cardList, top);
        }
        cardList.EndUpdate();
        cardList.Glow.Reset();   // the rows changed under the pointer
        hoverRow = -1;
    }

    // The name of the place over the cards, and that capturing is paused where copied entries are shown.
    void ShowTitle()
    {
        title.Text = place.Title + (paused && place.HasCopied ? Strings.PausedSuffix : "");
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

    // Remembers which card the pointer is on, and lets it light up. With the option on, an entry whose content the
    // card shows (a copied one, a saved one without a name, a picture) is shown whole if the pointer rests there; a
    // text with a name only when asked, from the eye of its card or its menu.
    void Hover(int index)
    {
        if (cardList.Glow.Row == index) return;
        cardList.Glow.Set(index);   // the card lights up gradually, the one left fades
        if (!peekAsked || index < 0) HidePeek();
        if (index < 0 || !restBox.Checked) return;
        Entry entry = (Entry)cardList.Items[index];
        if (entry.Saved && entry.Name != null && !entry.Picture) return;
        resting = cardList;
        rest.Interval = RestMs;
        rest.Start();
    }

    // The content of the selected saved text, asked for from its menu.
    void PeekSelected()
    {
        if (Selected != null) ShowPeek(cardList, cardList.SelectedIndex, true);
    }

    // Shows whole the entry of a card, in a small window under it. A text its card already shows whole needs none,
    // unless it was asked for: then it stays in view for a while, wherever the pointer goes.
    void ShowPeek(ListBox list, int row, bool asked)
    {
        Entry entry = list.Items[row] as Entry;
        Rectangle area = list.GetItemRectangle(row);
        if (entry == null || !asked && !entry.Picture && !Cut(list, entry, area)) return;
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

    // Whether the card of a text shows less than the text: more lines than it has, or more than fit.
    bool Cut(ListBox list, Entry entry, Rectangle row)
    {
        if (entry.Picture || entry.Saved && entry.Name != null) return false;
        Rectangle body = Lay(entry, row).Body;
        return CardText(entry) != entry.Text.Trim() || TextRenderer.MeasureText(entry.Text.Trim(), list.Font, new Size(body.Width, 0),
            BodyFlags).Height > body.Height;
    }

    // When, as a card says it at the right end of its buttons: under the name of its day only the time; otherwise
    // of a copied entry the time today and the day before that, of a saved text always the day it was saved. The
    // year only when it is not this one. Null if nobody knows.
    string When(Entry entry)
    {
        if (!Dated(entry)) return null;
        return entry.Time.ToString(headed || !entry.Saved && entry.Time.Date == DateTime.Today ? "HH:mm"
            : entry.Time.Year == DateTime.Today.Year ? Strings.DayFormat : Strings.YearDayFormat, Strings.Culture);
    }

    // Whether an entry says when: not a folder, nor a text saved before the days were kept.
    static bool Dated(Entry entry)
    {
        return !entry.IsGroup && entry.Time != DateTime.MinValue;
    }

    void DrawGlyph(object sender, PaintEventArgs e)
    {
        Control frame = (Control)sender;
        int side = Font.Height;
        Look.Draw(e.Graphics, Sign.Search, new Rectangle(RowInset - 3, (frame.Height - side) / 2, side, side), null);
        // At the right end, in a small frame, the keys that bring the cursor here from anywhere in the window.
        const TextFormatFlags flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine
            | TextFormatFlags.NoPrefix;
        string keys = NameOf(SearchKeys);
        Size text = TextRenderer.MeasureText(e.Graphics, keys, Look.NoteFont, Size.Empty, flags);
        int padding = (int)(KeysPadding * Zoom), width = text.Width + padding, height = text.Height + padding / 2;
        Rectangle badge = new Rectangle(frame.Width - (int)(RowInset * Zoom) - width, (frame.Height - height) / 2, width, height);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using (GraphicsPath shape = Look.Rounded(badge))
        using (Pen edge = new Pen(Look.Border))
            e.Graphics.DrawPath(edge, shape);
        TextRenderer.DrawText(e.Graphics, keys, Look.NoteFont, badge, Look.Muted, flags);
    }

    // The sign that tells what an entry is: a formatted text, an address, a piece of code, a picture, any other text;
    // and that of a folder, open or closed.
    static Sign SignOf(Entry entry)
    {
        if (entry.IsGroup) return entry.Open ? Sign.Opened : Sign.Closed;
        if (entry.Html != null) return Sign.Formatted;
        switch (entry.Category)
        {
            case Category.Link: return Sign.Link;
            case Category.Code: return Sign.Code;
            case Category.Picture: return Sign.Picture;
            default: return Sign.Text;
        }
    }

    // What a card says the entry is, at the right end of its top line: a formatted text says so, unless the user
    // said what it is.
    static string BadgeOf(Entry entry)
    {
        return entry.Html != null && !entry.Chosen ? Strings.FormattedBadge : KindName(entry.Category);
    }

    static string KindName(Category category)
    {
        switch (category)
        {
            case Category.Link: return Strings.LinkBadge;
            case Category.Code: return Strings.CodeBadge;
            case Category.Picture: return Strings.PictureBadge;
            default: return Strings.TextBadge;
        }
    }

    // The categories a text can be given, in its menu: the one it has is ticked, and choosing it does nothing.
    // Nothing for a picture, which is always one.
    void ListKinds(ToolStripMenuItem menu, Entry entry)
    {
        menu.DropDownItems.Clear();
        bool text = entry != null && !entry.Picture;
        menu.Visible = text;   // asked back, 'Visible' would answer false: the menu is not on screen yet
        if (!text) return;
        foreach (Category each in new Category[] { Category.Text, Category.Code, Category.Link })
        {
            Category kind = each;   // a variable of its own for the delegate
            ToolStripMenuItem item = new ToolStripMenuItem(KindName(kind), null, delegate { Choose(entry, kind); });
            item.Checked = entry.Category == kind;
            menu.DropDownItems.Add(item);
        }
    }

    // Says what a text is, whatever was guessed: it goes under that tab, copied again or saved.
    void Choose(Entry entry, Category kind)
    {
        if (entry.Category == kind) return;   // the one ticked in the menu: nothing changes
        if (!Change(delegate { clips.Choose(entry.Id, kind); })) return;
        Say(string.Format(Strings.KindChanged, Shorten(entry.Label), KindName(kind)));
        Reload();
    }

    Entry Selected
    {
        get { return cardList.SelectedItem as Entry; }
    }

    // --- The cards ---

    const TextFormatFlags BodyFlags = TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix | TextFormatFlags.TextBoxControl;
    const int MaxBody = 600;   // of a long text, what a card takes to show its first lines
    const string Dot = " · ";  // between the parts of the small line under the entry
    const float MetaShare = 0.6f;   // how far that line goes from the card towards Muted, and how opaque its icon is
    // The small buttons at the right of a card, from the left: they are signs alone, named by their tooltips.
    static readonly Part[] CardButtons = { Part.Show, Part.Copy, Part.Save, Part.Delete, Part.More };

    // What the card shows of an entry: of a saved one with a name only the name, so that a password never appears;
    // of a text its beginning, its lines kept.
    static string CardText(Entry entry)
    {
        if (entry.Saved && entry.Name != null || entry.Picture) return entry.Label;
        string text = entry.Text.Trim().Replace("\r\n", "\n").Replace('\t', ' ');
        return text.Length > MaxBody ? text.Substring(0, MaxBody) : text;
    }

    // How tall the row of a card is, its air included, or that of the name of a day: never taller than a list box
    // of Windows allows, which leaves less of the entry.
    int CardHeight(object row, int width)
    {
        if (row is Heading) return Look.StrongFont.Height + (int)((HeadingAir + CardLineGap) * Zoom);
        Entry entry = (Entry)row;
        CardParts parts = Lay(entry, new Rectangle(0, 0, width, MaxRow));
        int body;
        if (entry.Picture) body = (int)(CardPicture * Zoom);
        else if (entry.Saved && entry.Name != null) body = Look.StrongEntryFont.Height;
        else
        {
            int line = Look.EntryFont.Height;
            int tall = TextRenderer.MeasureText(CardText(entry), Look.EntryFont, new Size(Math.Max(1, parts.Body.Width), 0), BodyFlags).Height;
            body = Math.Max(1, Math.Min(BodyLines, (tall + line - 1) / line)) * line;
        }
        return Math.Min(MaxRow, 2 * (int)(CardPad * Zoom) + Math.Max(body, parts.Least) + (int)(CardGap * Zoom));
    }

    // The small line under or beside the entry: the program it was copied in, or the folder of a saved one; when;
    // what it is.
    string MetaText(Entry entry)
    {
        string source = entry.Saved ? (entry.Group != null ? Texts.Display(entry.Group) : Strings.SavedTitle)
            : entry.Origin != null ? entry.Origin.Name : null;
        List<string> said = new List<string>();
        foreach (string part in new string[] { source, When(entry), BadgeOf(entry) }) if (part != null) said.Add(part);
        return string.Join(Dot, said.ToArray());
    }

    const TextFormatFlags MetaFlags = TextFormatFlags.VerticalCenter | LineFlags | TextFormatFlags.NoPadding;

    // The parts of the card of an entry in its row (see CardParts): the entry first, at the top left; at the right of
    // its first line its number, its arrow and the small buttons; the small line of where and when right before them
    // if the entry keeps most of the card, otherwise under them. DrawEntry writes that line at the right end of Meta.
    CardParts Lay(Entry entry, Rectangle row)
    {
        int inset = (int)(CardInset * Zoom), pad = (int)(CardPad * Zoom), gap = (int)(CardLineGap * Zoom), side = (int)(PartHeight * Zoom);
        int between = (int)(PartGap * Zoom) / 2;
        CardParts parts = new CardParts();
        parts.Card = Rectangle.FromLTRB(row.Left + 1, row.Top, row.Right - 2, row.Bottom - (int)(CardGap * Zoom));
        int left = parts.Card.Left + inset, right = parts.Card.Right - inset, top = parts.Card.Top + pad;
        // The buttons in the middle of the first line of the entry.
        int middle = top + Look.EntryFont.Height / 2, end = right;
        for (int i = CardButtons.Length - 1; i >= 0; i--)
        {
            parts.Buttons[CardButtons[i]] = new Rectangle(end - side, middle - side / 2, side, side);
            end -= side + between;
        }
        if (!entry.Saved)
        {
            parts.Pin = new Rectangle(end - side, middle - side / 2, side, side);
            end = parts.Pin.Left - between;
        }
        if (entry.Number > 0)
        {
            int high = Font.Height, width = high + NumberPadding;
            parts.Number = new Rectangle(end - width, middle - high / 2, width, high);
            end = parts.Number.Left - between;
        }
        int meta = Look.NoteFont.Height, room = right - left, bottom = Math.Max(top, parts.Card.Bottom - pad);
        int wide = meta + (int)(PartGap * Zoom) + TextRenderer.MeasureText(MetaText(entry), Look.NoteFont, Size.Empty, MetaFlags & ~TextFormatFlags.EndEllipsis).Width;
        if (end - 3 * gap - wide - left >= room * InlineShare)
        {
            parts.Meta = new Rectangle(end - gap - wide, middle - meta / 2, wide, meta);
            parts.Body = Rectangle.FromLTRB(left, top, parts.Meta.Left - 2 * gap, bottom);
            parts.Least = Look.EntryFont.Height;
        }
        else
        {
            int column = Math.Max(right - end, Math.Min(wide, (int)(room * ColumnShare)));
            parts.Meta = new Rectangle(right - column, middle + side / 2, column, meta);
            parts.Body = Rectangle.FromLTRB(left, top, right - column - gap, bottom);
            parts.Least = parts.Meta.Bottom - top;
        }
        return parts;
    }

    // What a small button does, in its tooltip.
    static string PartText(Part part, Entry entry)
    {
        switch (part)
        {
            case Part.Show: return ShowText(entry);
            case Part.Copy: return Strings.CopyButton;
            case Part.Save: return entry.Saved ? Strings.EditDots : Strings.SaveDots;
            case Part.Delete: return Strings.Delete;
            case Part.Pin: return entry.Pinned ? Strings.Unpin : Strings.Pin;
            case Part.More: return Strings.MoreItem;
            default: return null;
        }
    }

    // What showing an entry whole is called: of a saved text with a name, showing its content, which the card hides.
    static string ShowText(Entry entry)
    {
        return entry.Saved && entry.Name != null && !entry.Picture ? Strings.ShowContent : Strings.ShowFull;
    }

    static Sign PartSignOf(Part part, Entry entry)
    {
        switch (part)
        {
            case Part.Show: return Sign.Show;
            case Part.Copy: return Sign.Copy;
            case Part.Save: return entry.Saved ? Sign.Edit : Sign.Star;
            case Part.Delete: return Sign.Delete;
            default: return Sign.More;
        }
    }

    // The part of a card at a point, Body anywhere else on the card, None outside the cards.
    Part PartAt(int index, Point where)
    {
        Entry entry = index < 0 ? null : cardList.Items[index] as Entry;
        if (entry == null) return Part.None;
        CardParts parts = Lay(entry, cardList.GetItemRectangle(index));
        foreach (KeyValuePair<Part, Rectangle> button in parts.Buttons)
            if (button.Value.Contains(where)) return button.Key;
        if (!entry.Saved && parts.Pin.Contains(where)) return Part.Pin;
        return parts.Card.Contains(where) ? Part.Body : Part.None;
    }

    // A row of the list: the name of a day, or the card of an entry.
    void DrawEntry(object sender, DrawItemEventArgs e)
    {
        if (e.Index < 0) return;
        Graphics g = e.Graphics;
        using (Brush plain = new SolidBrush(cardList.BackColor)) g.FillRectangle(plain, e.Bounds);
        Heading heading = cardList.Items[e.Index] as Heading;
        if (heading != null)
        {
            // Its name low in its row, with a thin line under it, across.
            int height = Look.StrongFont.Height, bottom = e.Bounds.Bottom - (int)(CardLineGap * Zoom);
            TextRenderer.DrawText(g, heading.Text, Look.StrongFont, new Rectangle(e.Bounds.Left + 2, bottom - height - 2, e.Bounds.Width, height),
                Look.Ink, TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
            using (Pen line = new Pen(Look.Border)) g.DrawLine(line, e.Bounds.Left, bottom, e.Bounds.Right - 2, bottom);
            return;
        }
        Entry entry = (Entry)cardList.Items[e.Index];
        bool selected = (e.State & DrawItemState.Selected) != 0;
        float glow = cardList.Glow.Of(e.Index);
        CardParts parts = Lay(entry, e.Bounds);
        // The card: a shade lighter than the pane, a little more under the pointer; the selected one has an edge in
        // the accent colour.
        g.SmoothingMode = SmoothingMode.AntiAlias;
        Color fill = Look.Blend(Look.Raised, Look.Soft, 0.4f * glow);
        using (GraphicsPath shape = Look.Rounded(parts.Card))
        using (Brush back = new SolidBrush(fill))
        using (Pen edge = new Pen(selected ? Look.Accent : Look.Border, selected ? 2 * Zoom : 1))
        {
            g.FillPath(back, shape);
            g.DrawPath(edge, shape);
        }

        // First and largest, the entry: a picture with its size beside it, the name of a saved text, or the lines of a text.
        if (entry.Preview != null)
        {
            Size room = new Size(parts.Body.Width / 2, parts.Body.Height);
            float scale = Math.Min((float)room.Width / entry.Preview.Width, (float)room.Height / entry.Preview.Height);
            Size size = new Size((int)(entry.Preview.Width * scale), (int)(entry.Preview.Height * scale));
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(entry.Preview, parts.Body.Left, parts.Body.Top + (parts.Body.Height - size.Height) / 2, size.Width, size.Height);
            TextRenderer.DrawText(g, entry.Label, e.Font, Rectangle.FromLTRB(parts.Body.Left + size.Width + 2 * PreviewMargin, parts.Body.Top,
                parts.Body.Right, parts.Body.Bottom), Look.Ink, TextFormatFlags.VerticalCenter | LineFlags);
        }
        else if (entry.Saved && entry.Name != null)
            TextRenderer.DrawText(g, entry.Name, Look.StrongEntryFont, new Rectangle(parts.Body.Left, parts.Body.Top, parts.Body.Width,
                Look.StrongEntryFont.Height), Look.Ink, TextFormatFlags.VerticalCenter | LineFlags);
        else
            TextRenderer.DrawText(g, CardText(entry), e.Font, parts.Body, Look.Ink, BodyFlags | TextFormatFlags.EndEllipsis);

        // Before the buttons or under them, at the right end, small and faint, so that the entry is what stands out: the
        // program it was copied in with its icon, or the folder of a saved one; when; what it is.
        string meta = MetaText(entry);
        int side = parts.Meta.Height, gap = (int)(PartGap * Zoom);
        int width = Math.Min(parts.Meta.Width - side - gap, TextRenderer.MeasureText(g, meta, Look.NoteFont, Size.Empty,
            MetaFlags & ~TextFormatFlags.EndEllipsis).Width);
        Rectangle text = Rectangle.FromLTRB(parts.Meta.Right - width, parts.Meta.Top, parts.Meta.Right, parts.Meta.Bottom);
        Rectangle icon = new Rectangle(text.Left - gap - side, parts.Meta.Top, side, side);
        Color faint = Look.Blend(fill, Look.Muted, MetaShare);
        if (entry.Saved) Look.Draw(g, Sign.Star, icon, faint);
        else if (entry.Origin != null && entry.Origin.Icon != null) Look.DrawFaint(g, entry.Origin.Icon, icon, MetaShare);
        else Look.Draw(g, entry.Origin != null ? Sign.Apps : SignOf(entry), icon, faint);
        TextRenderer.DrawText(g, meta, Look.NoteFont, text, faint, MetaFlags);

        // At the right of the first line: the number, the arrow and the small buttons, grey but on the card the pointer
        // is on, or the selected one.
        if (entry.Number > 0)
        {
            // The digit that pastes it from anywhere, in a rounded frame in the accent colour.
            using (GraphicsPath shape = Look.Rounded(Rectangle.FromLTRB(parts.Number.Left, parts.Number.Top, parts.Number.Right - 1, parts.Number.Bottom - 1)))
            using (Pen edge = new Pen(Look.Accent))
                g.DrawPath(edge, shape);
            TextRenderer.DrawText(g, entry.Number.ToString(), Font, parts.Number, Look.Accent, TextFormatFlags.HorizontalCenter
                | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
        }
        bool awake = selected || glow > 0.5f;
        Color quiet = Look.Blend(fill, Look.Muted, 0.8f);
        // The arrow that keeps the entry at the top for a while: in colour if it does, faint otherwise.
        if (!entry.Saved) DrawPart(g, e.Index, Part.Pin, parts.Pin, Sign.Up, entry.Pinned ? (Color?)null : Look.Blend(fill, Look.Muted, 0.5f), fill);
        foreach (KeyValuePair<Part, Rectangle> button in parts.Buttons)
            DrawPart(g, e.Index, button.Key, button.Value, PartSignOf(button.Key, entry),
                awake && button.Key != Part.More || IsHovered(e.Index, button.Key) ? (Color?)null : quiet, fill);
    }

    bool IsHovered(int index, Part part)
    {
        return index == hoverRow && part == hoverPart;
    }

    // A small button of a card: its sign, in its own colour or the one given, on a faint shape under the pointer.
    void DrawPart(Graphics g, int index, Part part, Rectangle area, Sign sign, Color? colour, Color fill)
    {
        if (IsHovered(index, part))
            using (GraphicsPath path = Look.Rounded(Rectangle.FromLTRB(area.Left, area.Top, area.Right - 1, area.Bottom - 1)))
            using (Brush brush = new SolidBrush(Look.Blend(fill, Look.Ink, 0.08f)))
                g.FillPath(brush, path);
        int side = Font.Height * 4 / 5;
        Look.Draw(g, sign, new Rectangle(area.Left + (area.Width - side) / 2, area.Top + (area.Height - side) / 2, side, side),
            part == Part.More && IsHovered(index, part) ? Look.Ink : colour);
    }

    // The numbers an entry can be given, in its menu: the one it has is ticked, and beside each that another entry
    // has, that entry, which would lose it. Nothing for a folder.
    void ListNumbers(ToolStripMenuItem menu, Entry entry)
    {
        menu.DropDownItems.Clear();
        // Asked back, 'Visible' would answer false: the menu is not on screen yet.
        menu.Visible = entry != null && !entry.IsGroup;
        if (entry == null || entry.IsGroup) return;
        ToolStripMenuItem none = new ToolStripMenuItem(Strings.NoNumber, null, delegate { Number(entry, 0); });
        none.Checked = entry.Number == 0;
        menu.DropDownItems.Add(none);
        for (int each = 1; each <= MaxNumber; each++)
        {
            int number = each;   // a variable of its own for the delegate
            ToolStripMenuItem item = new ToolStripMenuItem(NumberName(number), null, delegate { Number(entry, number); });
            item.Checked = entry.Number == number;
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
        if (number == entry.Number) return;   // the one ticked in the menu: nothing changes
        if (!Change(delegate { clips.Number(entry.Id, number); })) return;
        Say(number > 0 ? string.Format(Strings.NumberGiven, NumberName(number), Shorten(entry.Label))
            : string.Format(Strings.NumberTaken, Shorten(entry.Label)));
        numbersTaken = "";   // said again, even if it was said before: the user is choosing now
        Reload();
    }

    // Keeps a copied entry at the top of the list for a while, or lets it go back among the others. Either way
    // it goes when its days are over: what is to stay is saved.
    void TogglePin(Entry entry)
    {
        if (entry == null || entry.Saved) return;
        if (!Change(delegate { clips.Pin(entry.Id, !entry.Pinned); })) return;
        Say(string.Format(!entry.Pinned ? Strings.Pinned : Strings.Unpinned, Shorten(entry.ToString())));
        ReloadCopied();
    }

    // The card before or after the selected one, over the names of the days.
    void Step(int by)
    {
        for (int index = cardList.SelectedIndex + by; index >= 0 && index < cardList.Items.Count; index += by)
            if (cardList.Items[index] is Entry)
            {
                cardList.SelectedIndex = index;
                return;
            }
    }

    // Enter, or a double click: the entry on the clipboard, and back to the window the list hotkey was pressed in.
    void Accept()
    {
        Entry entry = Selected;
        if (entry != null && !busy && Use(entry)) GoBack();
    }

    // Makes an entry the clipboard content again: from there Ctrl+V pastes it and the hotkey types it.
    bool Use(Entry entry)
    {
        try
        {
            DataObject data = NewData(entry.Saved);
            if (!entry.Picture)
            {
                // A formatted text is there as plain text too: what takes no formatting, and the typing, use that.
                data.SetText(entry.Text);
                Rich.Put(data, entry.Html, entry.Rtf);
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

    // What goes onto the clipboard. Of a saved entry, which may be a password, marked so that the clipboard history
    // of Windows and other monitors skip it.
    static DataObject NewData(bool saved)
    {
        DataObject data = new DataObject();
        if (saved) data.SetData(Texts.PrivateMarker, new MemoryStream(new byte[sizeof(int)]));
        return data;
    }

    // The text Windows reads in a picture, onto the clipboard as if the user had copied it: so it also comes into
    // the list, unless the picture is a saved one. Read away from the window, which a large picture would hold still.
    void CopyText(Entry entry)
    {
        byte[] png;
        try
        {
            png = entry.LoadPng();
        }
        catch (Exception ex)   // a picture the database no longer gives
        {
            ShowError(ex);
            return;
        }
        bool saved = entry.Saved;
        status.Text = Strings.ReadingPicture;
        ThreadPool.QueueUserWorkItem(delegate
        {
            string text = null;
            Exception error = null;
            try
            {
                text = Ocr.Read(png);
            }
            catch (Exception ex)   // a Windows without the recognition, or a picture it does not take
            {
                error = ex;
            }
            try
            {
                BeginInvoke((MethodInvoker)delegate { CopyRead(text, error, saved); });
            }
            catch (InvalidOperationException)
            {
                // the window closed meanwhile
            }
        });
    }

    void CopyRead(string text, Exception error, bool saved)
    {
        if (error != null) ShowError(error);
        else if (text == null) Warn(Strings.NoRecognition);
        else if (text.Length == 0) Warn(Strings.NoPictureText);
        else
        {
            try
            {
                DataObject data = NewData(saved);
                data.SetText(text);
                Clipboard.SetDataObject(data, true);
            }
            catch (Exception ex)   // the clipboard locked by another program
            {
                ShowError(ex);
                return;
            }
            Say(string.Format(hotkeyBox.Checked ? Strings.OnClipboardOrType : Strings.OnClipboard,
                Shorten(new Entry(null, text).ToString()), HotkeyName));
        }
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
        if (!whereLeftBox.Checked) ShowAll();   // unless the option keeps where the list was left
        options.Hide();   // above the list, it would cover it
        RestoreWindow();
        search.Focus();
    }

    // All the entries, of every kind, from the latest copy: where the list hotkey opens, wanted by the user.
    void ShowAll()
    {
        place = new Place(PlaceKind.All);
        cardList.ClearSelected();
        newest = true;
        if (tabs.Selected != 0) tabs.Selected = 0;   // which fills the cards and the sidebar again
        else Filter();
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


    // Opens the window with name and content: for a new text ('source' is null), to keep a copied text or picture,
    // or to change a saved one.
    void Edit(Entry source)
    {
        // A new text is proposed for the folder shown; any text can then be put elsewhere.
        string group = source != null ? source.Group : FolderHere();
        using (EditForm edit = new EditForm(source, group, GroupNames()))
        {
            // Asked again, with what was written still there, if the name turns out to be taken.
            while (edit.ShowDialog(this) == DialogResult.OK)
            {
                string name = edit.EntryName;   // empty: none, and the card shows the content
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
                        if (source == null) clips.AddSaved(name, edit.Content, edit.Html, edit.Rtf, folder, DateTime.Now);
                        else
                        {
                            // A copied entry becomes saved in its own row, number and all.
                            if (!source.Saved) clips.Save(source.Id, name, folder);
                            // A picture keeps itself.
                            clips.Change(source.Id, name, source.Picture ? null : edit.Content, edit.Html, edit.Rtf, folder);
                        }
                        clips.Reveal(folder);   // or its folder would not be seen in the sidebar
                        transaction.Commit();
                    }
                })) continue;
                Say(string.Format(Strings.SavedSay, Shorten(name.Length > 0 ? name
                    : source != null && source.Picture ? source.Label : new Entry(null, edit.Content).Label)));
                break;
            }
        }
        Reload();
    }

    // The folder something new goes into: the one shown, or else that of the selected saved text. Null for none.
    string FolderHere()
    {
        if (place.Kind == PlaceKind.Folder) return place.Path;
        Entry entry = Selected;
        return entry != null && entry.Saved ? entry.Group : null;
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
    // folder is proposed inside the one shown.
    void EditGroup(string old)
    {
        string parent = old != null ? Texts.Parent(old) : FolderHere();
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
                if (old != null) Follow(old, path);
                break;
            }
        }
        if (old == null) savedOpen = true;   // the new folder is seen
        Reload();
    }

    // A folder that was renamed or moved, or one inside it: if it is the one shown, it stays shown at its new path.
    void Follow(string old, string path)
    {
        if (place.Kind == PlaceKind.Folder && Texts.Within(place.Path, old)) place.Path = path + place.Path.Substring(old.Length);
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
        if (entry.IsGroup) Follow(entry.Name, Texts.Join(group, Texts.Leaf(entry.Name)));
        Reload();
    }

    void DeleteSelected()
    {
        Entry entry = Selected;
        if (entry == null) return;
        if (!entry.Saved)
        {
            if (!Change(delegate { clips.Remove(entry.Id); })) return;
        }
        else if (!Ask(string.Format(Strings.AskDelete, entry.Label)) || !Change(delegate { clips.Remove(entry.Id); })) return;
        Reload();
    }

    // Deletes a folder, only the folder: what it holds is too easily a password to be deleted along. It moves up a
    // level, and Saved is shown.
    void DeleteFolder(Entry folder)
    {
        if (folder == null) return;
        bool holds = entries.Exists(delegate(Entry other)
        {
            return other != folder && (Texts.Within(other.Group, folder.Name) || other.IsGroup && Texts.Within(other.Name, folder.Name));
        });
        string parent = Texts.Parent(folder.Name);
        if (!Ask(string.Format(Strings.AskDeleteFolder, Texts.Display(folder.Name))
            + (!holds ? "" : parent == null ? Strings.DeleteFolderTop : string.Format(Strings.DeleteFolderUp, Texts.Display(parent))))
            || !Change(delegate { clips.DeleteFolder(folder.FolderId); })) return;
        place = new Place(PlaceKind.Saved);
        Reload();
    }

    // The keys of the list, while the search box, the cards or the sidebar have the focus.
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        HidePeek();
        if (capturing != null) return TakeKeys(keyData);
        if (keyData == HelpKey)
        {
            ShowHelp();
            return true;
        }
        if (keyData == SearchKeys)
        {
            search.Focus();
            search.SelectAll();
            return true;
        }
        bool inCards = search.Focused || cardList.Focused;
        if (!inCards && !navList.Focused) return base.ProcessCmdKey(ref msg, keyData);
        switch (keyData)
        {
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
        // The arrows of the sidebar move among its places.
        if (!inCards) return base.ProcessCmdKey(ref msg, keyData);
        switch (keyData)
        {
            case Keys.Escape:
                GoBack();
                return true;
            case Keys.Enter:
                enterDown = true;
                return true;
            case Keys.Up:
                Step(-1);
                return true;
            case Keys.Down:
                Step(1);
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
        Accept();
    }

    protected override void OnDeactivate(EventArgs e)
    {
        base.OnDeactivate(e);
        enterDown = false;
        HidePeek();
        EndCapture();   // keys pressed elsewhere are not for us
    }
}
