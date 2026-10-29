using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Windows.Forms;

// A part of a card, where a click does something of its own.
enum Part { None, Body, Show, Copy, Save, Delete, Arrow, More, Number }

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

// A folder among the cards of Saved or of a folder, one directly inside it, before its texts, as in File Explorer
// (wanted by the user on 27/10/2026). Not an entry: what acts on entries (copy, type, delete, pick) passes it by.
class FolderCard
{
    public readonly Entry Folder;   // its row among the saved entries (IsGroup)
    public readonly int Count;      // the texts directly inside it, as the sidebar counts them

    public FolderCard(Entry folder, int count)
    {
        Folder = folder;
        Count = count;
    }

    public override string ToString()
    {
        return Folder.Label;
    }
}

// Among the cards of Saved or of a folder, the thin line between its folders and its texts: a name over each looked
// heavy for a few rows (wanted by the user on 29/10/2026). A heading without a name, so passed by as they are.
class Rule : Heading
{
    public Rule() : base("")
    {
    }
}

// Over the cards of Saved and of a folder, under the title of Saved, the way there from Saved, the folder shown last, and after it,
// small and grey, how many folders and texts it holds (wanted by the user on 29/10/2026, in place of a row among the
// cards that went back up, which looked like a folder inside it; first in the title, then here, so that the title says
// Saved as the others say their place). The folders it is in are faint, each a way back there, lit under the pointer
// or while what is dragged over it would go into it. What does not fit gives way from the left, to an ellipsis. Drawn
// as the names of the days, low with a thin line under it, across.
class Trail : Control
{
    const string Between = " › ", Cut = "…";
    const int Inset = 2;                                       // where the names start, as those of the days
    public readonly List<string> Paths = new List<string>();   // of each folder from the top, null for Saved; the one shown last
    public string Note = "";                                   // after it, how many it holds
    public int Foot;                                           // how far above its bottom the line under it is
    public event Action<string> Chosen;
    int lit = -1;
    readonly List<Rectangle> spots = new List<Rectangle>();    // where each is written, empty if it gave way
    Rectangle noted;                                           // and the note
    int first;                                                 // the first written, after the ellipsis if not 0

    public Trail()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        SetStyle(ControlStyles.Selectable, false);   // a click leaves the focus where it is
    }

    static string Called(string path)
    {
        return path == null ? Strings.SavedTitle : Texts.Leaf(path);
    }

    int Wide(string text)
    {
        return TextRenderer.MeasureText(text, Font).Width;
    }

    int NoteWide
    {
        get { return Note.Length == 0 ? 0 : TextRenderer.MeasureText(Note, Look.NoteFont).Width + Font.Height / 3; }
    }

    // Where each one is written: all of them if they fit; otherwise the folder shown, and those it is in from the nearest
    // back as far as they fit after the ellipsis.
    void Lay()
    {
        spots.Clear();
        int between = Wide(Between), cut = Wide(Cut) + between, last = Paths.Count - 1, room = Width - Inset;
        int[] wide = new int[Paths.Count];
        int needed = NoteWide;
        for (int i = 0; i < Paths.Count; i++)
        {
            wide[i] = Wide(Called(Paths[i]));
            needed += wide[i] + (i < last ? between : 0);
        }
        first = 0;
        if (needed > room && last > 0)
        {
            first = last;
            for (int free = room - NoteWide - wide[last] - cut, i = last - 1; i >= 0 && free >= wide[i] + between; i--)
            {
                free -= wide[i] + between;
                first = i;
            }
        }
        int left = Inset + (first > 0 ? cut : 0);
        for (int i = 0; i < Paths.Count; i++)
        {
            spots.Add(i < first ? Rectangle.Empty : new Rectangle(left, 0, i == last ? Math.Max(0, Math.Min(wide[i], Width - NoteWide - left)) : wide[i], Height));
            if (i >= first) left += spots[i].Width + between;
        }
        noted = new Rectangle(last < 0 ? 0 : spots[last].Right + Font.Height / 3, 0, NoteWide, Height);
    }

    // The folder at a point that it is a way back to, -1 for none: not the one shown.
    public int At(Point where)
    {
        Lay();
        for (int i = 0; i < spots.Count - 1; i++)
            if (spots[i].Contains(where)) return i;
        return -1;
    }

    // Lights one up, -1 none.
    public void Light(int index)
    {
        if (index == lit) return;
        lit = index;
        Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        int index = At(e.Location);
        Cursor = index >= 0 ? Cursors.Hand : Cursors.Default;
        Light(index);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        Light(-1);
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        int index = At(e.Location);
        if (e.Button == MouseButtons.Left && index >= 0 && Chosen != null) Chosen(Paths[index]);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        using (Brush back = new SolidBrush(BackColor)) g.FillRectangle(back, ClientRectangle);
        Lay();
        const TextFormatFlags flags = TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis;
        int line = Height - Foot, top = line - Font.Height - 2;
        using (Pen pen = new Pen(Look.Border)) g.DrawLine(pen, 0, line, Width - 2, line);
        if (first > 0) TextRenderer.DrawText(g, Cut + Between, Font, new Rectangle(Inset, top, Wide(Cut + Between), Font.Height), Look.Muted, flags);
        int last = Paths.Count - 1;
        for (int i = first; i <= last; i++)
        {
            TextRenderer.DrawText(g, Called(Paths[i]), Font, new Rectangle(spots[i].Left, top, spots[i].Width, Font.Height),
                i == last || i == lit ? Look.Ink : Look.Muted, flags);
            if (i < last) TextRenderer.DrawText(g, Between, Font, new Rectangle(spots[i].Right, top, Wide(Between), Font.Height), Look.Muted, flags);
        }
        // The note on the line the names stand on.
        if (Note.Length == 0) return;
        top += MainForm.Ascent(Font) - MainForm.Ascent(Look.NoteFont);
        TextRenderer.DrawText(g, Note, Look.NoteFont, new Rectangle(noted.Left, top, noted.Width, Look.NoteFont.Height), Look.Muted,
            TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
    }
}

// The name of the day of the top card, standing over the cards while they scroll under it: drawn as the names of
// the days among them (see MainForm.ShowDay). The wheel over it scrolls them.
class DayBar : Control
{
    public Control Under;

    public DayBar()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        SetStyle(ControlStyles.Selectable, false);   // a click leaves the focus where it is
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == Native.WM_MOUSEWHEEL && Under != null && Under.IsHandleCreated)
        {
            m.Result = Native.SendMessage(Under.Handle, m.Msg, m.WParam, m.LParam);
            return;
        }
        base.WndProc(ref m);
    }
}

// Where the parts of a card are, in its row: the card itself, with air under it; the entry, first; at the right of
// its first line its number, then the small line of where, when and what, drawn at its right end, in whose room the
// small buttons show while the pointer is on the card.
class CardParts
{
    public Rectangle Card, Body, Meta, Number;
    public readonly Dictionary<Part, Rectangle> Buttons = new Dictionary<Part, Rectangle>();
}

// The text of a card laid out by hand, so that the words a search found can be marked in it: what it shows (its
// beginning, or a little before the first word found when that would be out of view), where those words are in it,
// and its lines.
class Marked
{
    public string Text;
    public List<CharacterRange> Found, Lines;
    public bool Cut;     // there is more after its last line, which ends with an ellipsis
    public int Width;    // what it was laid out for
    public Font Font;

    // Where what its lines show ends.
    public int Shown
    {
        get
        {
            CharacterRange last = Lines[Lines.Count - 1];
            return last.First + last.Length;
        }
    }
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
    string[] searched = new string[0];         // the words of the search the cards were filled with, marked on them
    readonly Dictionary<Entry, Marked> marks = new Dictionary<Entry, Marked>();   // how each card writes them, until the next filling
    readonly Dictionary<Entry, Marked> laid = new Dictionary<Entry, Marked>();    // and its text without them, when laid out by hand
    static Graphics measurer;                  // what their pieces are measured on (see Wide)
    int metaWide;                              // how wide the small line of every card is (see MetaWide)

    // What the cards answer to: buttons, right-click menu, mouse, dragging a saved text into a folder.
    void WireLists()
    {
        EventHandler create = delegate { Edit(null); };
        EventHandler change = delegate { Edit(Selected); };
        EventHandler delete = delegate { DeleteSelected(); };
        newButton.Click += create;
        // Only for several cards picked: their texts onto the clipboard, one after the other.
        ToolStripItem copyItem = listMenu.Items.Add(Strings.CopyButton, null, delegate { CopyPicked(); });
        ToolStripItem changeItem = listMenu.Items.Add("", null, change);
        // The whole entry, for a while: what the card cuts short, or the content of a saved text it shows by its name.
        ToolStripItem showItem = listMenu.Items.Add(Strings.ShowFull, null, delegate { PeekSelected(); });
        // Only for a picture: the text Windows reads in it.
        ToolStripItem readItem = listMenu.Items.Add(Strings.CopyPictureText, null, delegate { CopyText(Selected); });
        // Only for a text: a QR code of it, for a phone to take it.
        ToolStripItem qrItem = listMenu.Items.Add(Strings.QrMenu, null, delegate { ShowQr(Selected); });
        // Only for a copied entry: the same as a click on its arrow.
        ToolStripItem pinItem = listMenu.Items.Add("", null, delegate { PinSelected(); });
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
        Look.Mark(copyItem, Sign.Copy);
        Look.Mark(showItem, Sign.Show);
        Look.Mark(readItem, Sign.Text);
        Look.Mark(qrItem, Sign.Qr);
        Look.Mark(pinItem, Sign.Up);
        Look.Mark(numberItem, Sign.Keys);
        Look.Mark(kindItem, Sign.Code);
        Look.Mark(moveItem, Sign.Move);
        Look.Mark(deleteItem, Sign.Delete);
        Look.Mark(newItem, Sign.Plus);
        // With nothing selected the menu still opens: a first text can be written from an empty list. With several cards
        // picked, it has only what acts on all of them, as their bar does.
        listMenu.Opening += delegate
        {
            HidePeek();
            bool many = Many;
            Entry entry = many ? null : Selected;
            changeItem.Text = entry != null && entry.Saved ? Strings.EditDots : Strings.SaveDots;
            Look.Mark(changeItem, entry != null && entry.Saved ? Sign.Edit : Sign.Star);
            changeItem.Visible = entry != null;
            copyItem.Visible = many && PickedTexts().Count > 0;
            deleteItem.Visible = entry != null || many;
            showItem.Visible = entry != null;
            if (entry != null) showItem.Text = ShowText(entry);
            readItem.Visible = entry != null && entry.Picture;
            qrItem.Visible = entry != null && !entry.Picture;
            pinItem.Visible = entry != null ? !entry.Saved : many && PickedCopied().Count > 0;
            pinItem.Text = (entry != null ? entry.Pinned : many && AllPinned()) ? Strings.Unpin : Strings.KeepTop;
            newItem.Visible = !many;
            ListNumbers(numberItem, entry);
            ListKinds(kindItem, entry);
            if (many) moveItem.Visible = ListMoves(moveItem.DropDownItems, Targets());
            else ListPlaces(moveItem, entry);
        };
        Look.Style((ToolStripDropDownMenu)moveItem.DropDown);
        Look.Style((ToolStripDropDownMenu)numberItem.DropDown);
        Look.Style((ToolStripDropDownMenu)kindItem.DropDown);
        Look.Style(listMenu);
        WireCardMenu();

        Look.Field(cardList);
        cardList.IntegralHeight = false;
        cardList.BorderStyle = BorderStyle.None;
        cardList.DrawMode = DrawMode.OwnerDrawVariable;
        cardList.Font = Look.EntryFont;   // the entries have a font of their own, chosen in the options
        cardList.Marked = false;          // a selected card shows it by its edge
        cardList.ContextMenuStrip = listMenu;
        cardList.RowHeight = delegate(int row) { return CardHeight(cardList.Items[row]); };
        cardList.DrawItem += DrawEntry;
        // The name of a day is not chosen: the selection stays where it was. Moved otherwise than by picking (a click, the
        // arrows), the selection is of one card again, where a range picked with Shift starts.
        cardList.SelectedIndexChanged += delegate
        {
            if (cardList.SelectedItem is Heading) cardList.SelectedIndex = lastCard < cardList.Items.Count ? lastCard : -1;
            else lastCard = cardList.SelectedIndex;
            cardList.ContextMenuStrip = SelectedFolder != null ? cardMenu : listMenu;   // also for the menu key
            if (keepPicks) return;
            pickFrom = cardList.SelectedIndex;
            ClearPicks();
        };
        // Ctrl or Shift with a click pick several cards (wanted by the user on 20/10/2026), as in File Explorer.
        cardList.Picking = Pick;
        // The left button acts, the right one only selects, so that its menu acts on the card under the pointer; on a card
        // among those picked, it leaves them picked, and its menu acts on all of them.
        cardList.MouseDown += delegate(object sender, MouseEventArgs e)
        {
            int index = cardList.RowAt(e.Location);
            // The eye of the card whose entry is in view puts it away, as any click does, and the click does no more
            // (wanted by the user on 20/10/2026: it showed it again at once).
            eyeCloses = e.Button == MouseButtons.Left && peek.Showing && index == peekRow && PartAt(index, e.Location) == Part.Show;
            HidePeek();
            HideHoverTip();
            Entry entry = index < 0 ? null : cardList.Items[index] as Entry;
            FolderCard card = index < 0 ? null : cardList.Items[index] as FolderCard;
            if (e.Button == MouseButtons.Left) ClearPicks();   // also on the card already selected, which Windows leaves as it is
            else if (e.Button == MouseButtons.Right && (entry != null && !picked.Contains(entry.Id) || card != null))
            {
                ClearPicks();
                cardList.SelectedIndex = index;
            }
            // The menu of a folder is that of the folders; anywhere else that of the entries.
            cardList.ContextMenuStrip = card != null ? cardMenu : listMenu;
            // A saved text, or a folder, may be dragged to a folder of the sidebar or among the cards.
            if (card != null) entry = card.Folder;
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
            Entry entry = index < 0 ? null : cardList.Items[index] as Entry;
            if (e.Button != MouseButtons.Left || entry == null) return;   // a folder is only selected, as a card is
            switch (PartAt(index, e.Location))
            {
                case Part.Show: if (!eyeCloses) ShowPeek(cardList, index, true); break;
                case Part.Copy: Use(entry); break;
                case Part.Save: Edit(entry); break;
                case Part.Delete: DeleteSelected(); break;
                case Part.Arrow: TogglePin(entry); break;
                case Part.More: listMenu.Show(cardList, e.Location); break;
                case Part.Number:
                    // The numbers it can have, as in its menu: another one, or none.
                    ListNumbers(numberItem, entry);
                    Rectangle number = Lay(entry, cardList.GetItemRectangle(index)).Number;
                    numberItem.DropDown.Show(cardList, new Point(number.Left, number.Bottom));
                    break;
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
            Rectangle button = part == Part.Number ? parts.Number : parts.Buttons[part];
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
        // Scrolling, the name of the day of the top card stays over them (wanted by the user on 20/10/2026: among the cards
        // of yesterday, which say their time only, the name of the day had gone up out of view).
        cardList.Scrolled += delegate { ShowDay(); };
        dayBar.Paint += delegate(object sender, PaintEventArgs e)
        {
            using (Brush back = new SolidBrush(cardList.BackColor)) e.Graphics.FillRectangle(back, dayBar.ClientRectangle);
            DrawHeading(e.Graphics, dayBar.Text, dayBar.ClientRectangle);
        };
        WirePicks();
        // The pointer resting on a card shows the entry whole, if the option says so; one that was asked for leaves by
        // itself.
        rest.Tick += delegate
        {
            rest.Stop();
            if (peekAsked || resting == null)
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
        search.TextChanged += delegate { ClearPicks(); cardList.ClearSelected(); Filter(); };
        tabs.SelectedChanged += delegate { ClearPicks(); cardList.ClearSelected(); Filter(); };
    }

    // Everything again, the saved entries too: after anything that changed them.
    void Reload()
    {
        Change(delegate { entries = LoadSaved(); });
        ShowPhrase();   // the key may have been unlocked, or the database changed
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

    // The cards of the place chosen in the sidebar, of the category of the tabs, with the words of the search: the
    // copied entries from the database, which searches them itself (the history can hold thousands), the saved ones
    // from those in memory, whose content only this program can read.
    void Filter()
    {
        object selected = cardList.SelectedItem;   // an entry, or a folder
        HidePeek();   // the card it belongs to may be about to move
        string[] words = Texts.Words(search.Text);   // taken from the box once, not for each entry
        searched = words;
        marks.Clear();
        laid.Clear();
        Category? kind = TabKinds[tabs.Selected];
        List<Entry> copied = new List<Entry>();
        int total = 0;
        // Shortcuts: the entries with a number, copied or saved (wanted by the user on 19/10/2026, to see and manage them
        // in one place).
        bool numbered = place.Kind == PlaceKind.Shortcuts;
        if (place.HasCopied || numbered)
        {
            Sieve sieve = new Sieve();
            sieve.Words = words;
            sieve.Category = kind;
            sieve.Pinned = place.Kind == PlaceKind.Pinned;
            sieve.Numbered = numbered;
            sieve.App = place.Kind == PlaceKind.Source ? place.Id : 0;
            Change(delegate
            {
                copied = clips.Copied(MaxShown, sieve);
                // Fewer than the most listed are all of them: counting would read every search text once more.
                total = copied.Count < MaxShown ? copied.Count : clips.CopiedCount(sieve);
            });
        }
        // In History only those a search finds (wanted by the user on 19/10/2026: what is saved is not history, but the list
        // hotkey, a name and Enter must still find a saved password). In Saved those outside the folders, in a folder
        // those directly in it, after the folders directly inside it, as in File Explorer (wanted by the user on
        // 27/10/2026); a search finds them inside the folders too, and shows no folders.
        List<Entry> saved = new List<Entry>();
        List<FolderCard> folders = new List<FolderCard>();
        string shown = place.Kind == PlaceKind.Folder ? place.Path : null;   // null in Saved: outside the folders
        if (place.HasSaved || numbered || place.Kind == PlaceKind.History && words.Length > 0)
            foreach (Entry entry in entries)
                if (!entry.IsGroup && (!place.HasSaved || (words.Length > 0 ? shown == null || Texts.Within(entry.Group, shown)
                    : Texts.SameName(entry.Group, shown)))
                    && (!numbered || entry.Number > 0) && (!kind.HasValue || entry.Category == kind.Value) && Texts.Matches(entry, words))
                    saved.Add(entry);
        if (place.HasSaved && words.Length == 0 && !kind.HasValue)
            foreach (Entry folder in entries)
                if (folder.IsGroup && Texts.SameName(Texts.Parent(folder.Name), shown))
                    folders.Add(new FolderCard(folder, entries.FindAll(delegate(Entry entry)
                    {
                        return !entry.IsGroup && Texts.SameName(entry.Group, folder.Name);
                    }).Count));
        total += saved.Count;
        rows = Arrange(copied, saved, folders);
        // The name the cards begin under is said by the bar over them, which says the name of the day of the top card.
        topDay = rows.Count > 0 ? rows[0] as Heading : null;
        if (topDay != null) rows.RemoveAt(0);
        ShowRows(selected);

        ShowTitle();
        int shownCount = copied.Count + saved.Count;
        string counted = shownCount != total ? string.Format(Strings.CountOf, shownCount, total)
            : folders.Count == 0 ? total.ToString()
            // Folders and texts each by their name: one number alone did not say which it counted.
            : total == 0 ? Count(folders.Count, Strings.FolderOne, Strings.FolderMany)
            : string.Format(Strings.CountBoth, Count(folders.Count, Strings.FolderOne, Strings.FolderMany), Count(total, Strings.TextOne, Strings.TextMany));
        // Where saved texts are shown, after the name of the folder on the way there: beside the title, in a folder, it would
        // seem to count all of Saved.
        count.Text = place.HasSaved ? "" : counted;
        trail.Note = place.HasSaved ? counted : "";
        trail.Invalidate();
        PlaceTitle();
        // What the copied pictures take may have changed: in the red bar, and in the options if they are open.
        ShowPictureRoom();
        if (options.Visible) ShowSize();
        SayWhyEmpty(words.Length > 0, kind.HasValue);
        emptyNote.Visible = shownCount + folders.Count == 0;
        FillNav();   // the counts of the sidebar, and the programs, may have changed
    }

    // What an empty list says, and the sign over it: why it is empty, the first reason that holds.
    void SayWhyEmpty(bool searching, bool ofKind)
    {
        if (searching) SayEmpty(Strings.NoResults, Sign.Search);
        else if (ofKind) SayEmpty(Strings.NoneOfKind, Sign.Search);
        else if (place.Kind == PlaceKind.Pinned) SayEmpty(Strings.NoPinned, Sign.Up);
        else if (place.Kind == PlaceKind.Shortcuts) SayEmpty(string.Format(Strings.NoShortcuts, Strings.NumberMenu, NumbersName), Sign.Keys);
        else if (place.Kind == PlaceKind.Folder) SayEmpty(Strings.EmptyFolder, Sign.Star);
        else if (place.Kind == PlaceKind.Saved) SayEmpty(Strings.NoSaved, Sign.Star);
        else if (!historyBox.Checked) SayEmpty(Strings.HistoryOffEmpty, Sign.History);
        else if (paused) SayEmpty(Strings.PausedEmpty, Sign.Pause);
        else SayEmpty(Strings.CopyHint, Sign.Paste);
    }

    void SayEmpty(string text, Sign sign)
    {
        emptyNote.Text = text;
        emptyNote.Sign = sign;
    }

    // The cards in their order. In Shortcuts by number; in Saved and its folders the folders by name, then the texts by
    // name, a thin line between them; elsewhere those kept at the top first, then the others from the newest, each day under its name, and last,
    // by name under their own, the saved texts a search in History found.
    List<object> Arrange(List<Entry> copied, List<Entry> saved, List<FolderCard> folders)
    {
        List<object> arranged = new List<object>();
        headed = place.HasCopied;
        saved.Sort(delegate(Entry a, Entry b) { return StringComparer.CurrentCultureIgnoreCase.Compare(a.Label, b.Label); });
        folders.Sort(delegate(FolderCard a, FolderCard b) { return StringComparer.CurrentCultureIgnoreCase.Compare(a.Folder.Label, b.Folder.Label); });
        if (place.Kind == PlaceKind.Shortcuts)
        {
            // Copied and saved together, by their numbers.
            List<Entry> both = new List<Entry>(copied);
            both.AddRange(saved);
            both.Sort(delegate(Entry a, Entry b) { return a.Number.CompareTo(b.Number); });
            arranged.AddRange(both);
            return arranged;
        }
        // The folders, drawn as rows and not as cards, then a thin line, then the texts.
        if (!headed)
        {
            arranged.AddRange(folders);
            if (folders.Count > 0 && saved.Count > 0) arranged.Add(new Rule());
            arranged.AddRange(saved);
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
        // The newest first. Sorted as a whole, the order of equal times by row.
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
        if (saved.Count == 0) return arranged;
        arranged.Add(new Heading(Strings.SavedTitle));
        arranged.AddRange(saved);
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
    // The cards picked stay picked, those still there. A folder selected stays selected as well; without an entry, the
    // first folder is.
    void ShowRows(object selected)
    {
        Entry selectedEntry = selected as Entry;
        FolderCard selectedFolder = selected as FolderCard;
        bool toNewest = newest;
        newest = false;
        int top = cardList.TopIndex;
        keepPicks = true;
        metaWide = MetaWide();
        cardList.BeginUpdate();
        cardList.Items.Clear();
        int found = -1, first = -1, latest = -1, firstFolder = -1;
        HashSet<long> still = new HashSet<long>();
        foreach (object row in rows)
        {
            int index = cardList.Items.Add(row);
            FolderCard card = row as FolderCard;
            if (card != null)
            {
                if (firstFolder < 0) firstFolder = index;
                if (selectedFolder != null && card.Folder.FolderId == selectedFolder.Folder.FolderId) found = index;
            }
            Entry entry = row as Entry;
            if (entry == null) continue;
            if (first < 0) first = index;
            if (selectedEntry != null && entry.Id == selectedEntry.Id) found = index;
            if (!entry.Saved && (latest < 0 || entry.Time > ((Entry)rows[latest]).Time)) latest = index;
            if (picked.Contains(entry.Id)) still.Add(entry.Id);
        }
        lastCard = -1;
        if (first < 0) first = firstFolder;
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
        keepPicks = false;
        picked.IntersectWith(still);
        if (picked.Count < 2) picked.Clear();
        if (!Many || pickFrom >= cardList.Items.Count) pickFrom = cardList.SelectedIndex;
        cardList.Glow.Reset();   // the rows changed under the pointer
        hoverRow = -1;
        ShowPickBar();
        PlaceDay();
    }

    // The bar of the day over the cards, where they begin under the name of a day, or of Saved and its folders the way there: the cards
    // make room for it, below it. Placed once the window has its scale, its height that of a name among the cards. An
    // empty folder says so under the way there, which stays in sight.
    void PlaceDay()
    {
        dayBar.Visible = topDay != null;
        trail.Visible = place.HasSaved;
        if (!shown) return;
        int top = tabs.Bottom + (int)(Gap * Zoom), bottom = cardList.Bottom;
        int tall = topDay != null ? CardHeight(topDay) : trail.Visible ? CardHeight(new Heading("")) : 0;
        dayBar.SetBounds(cardList.Left, top, cardList.ClientSize.Width, tall);
        trail.SetBounds(cardList.Left, top, cardList.ClientSize.Width, tall);
        trail.Foot = (int)(CardLineGap * Zoom);   // as under the names of the days (DrawHeading)
        if (cardList.Top != top + tall) cardList.SetBounds(cardList.Left, top + tall, cardList.Width, bottom - top - tall);
        emptyNote.SetBounds(cardList.Left, top + tall, cardList.Width, bottom - top - tall);
        ShowDay();
    }

    // What the bar over the cards says: the name of the day of the top card, the last name before it, or the one the cards
    // begin under. With the name of a day itself at the top, right under the bar, the bar still says the day before.
    void ShowDay()
    {
        if (topDay == null) return;
        string day = topDay.Text;
        for (int i = Math.Min(cardList.TopIndex, cardList.Items.Count) - 1; i >= 0; i--)
        {
            Heading heading = cardList.Items[i] as Heading;
            if (heading == null) continue;
            day = heading.Text;
            break;
        }
        if (day == dayBar.Text) return;
        dayBar.Text = day;
        dayBar.Invalidate();
    }

    // The name of the place over the cards, Saved also for a folder, and that capturing is paused where copied entries are
    // shown; over the cards of Saved and of a folder, the way there from Saved.
    void ShowTitle()
    {
        title.Text = (place.HasSaved ? Strings.SavedTitle : place.Label) + (paused && place.HasCopied ? Strings.PausedSuffix : "");
        trail.Paths.Clear();
        if (place.HasSaved)   // Saved too, alone, so that its cards begin where those of its folders do
            for (string path = place.Path; ; path = Texts.Parent(path))
            {
                trail.Paths.Insert(0, path);
                if (path == null) break;
            }
        trail.Invalidate();
        PlaceTitle();
    }

    // The name of the place, and right after it, smaller and grey, how many entries it lists, on the same baseline: at
    // the other end of the pane it was far from what it counts. The name gives way to the count when there is no room.
    void PlaceTitle()
    {
        int gap = (int)(Gap * Zoom), counted = TextRenderer.MeasureText(count.Text, count.Font).Width;
        int named = TextRenderer.MeasureText(title.Text, title.Font).Width;
        title.Width = Math.Max(0, Math.Min(named, typeButton.Right - title.Left - gap - counted));
        count.SetBounds(title.Right + gap, title.Top + (count.Font.Height - title.Font.Height) / 2 + Ascent(title.Font) - Ascent(count.Font),
            counted, title.Height);
    }

    // How far below the top of its line a font writes the foot of its letters.
    internal static int Ascent(Font font)
    {
        FontFamily family = font.FontFamily;
        return font.Height * family.GetCellAscent(font.Style) / family.GetLineSpacing(font.Style);
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
        Entry entry = index < 0 ? null : cardList.Items[index] as Entry;
        if (entry == null || !restBox.Checked) return;   // a folder has nothing more to show
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
        RevealPeek(entry, list.RectangleToScreen(area));
        peekRow = peek.Showing ? row : -1;
        peekAsked = asked && peekRow >= 0;
        if (!peekAsked) return;
        rest.Interval = AskedMs;
        rest.Start();
    }

    // Something now and then (a remote desktop, the screens that change) left the preview unseen until a restart, with
    // the eye too. So the window is a new one each time the preview comes into view, which nothing before can have
    // spoiled; if it fails all the same, or Windows does not have it on the screen, once more with another new one. The
    // first failure of a session goes to the log.
    void RevealPeek(Entry entry, Rectangle row)
    {
        for (int attempt = 0; attempt < 2; attempt++)
        {
            Exception failure;
            try
            {
                if (attempt > 0 || !peek.Showing) RenewPeek();
                peek.Reveal(entry, row, Zoom);
                if (!peek.Visible || peek.Showing) return;   // nothing to show (a picture no longer there), or in view
                failure = new InvalidOperationException(PeekUnseen);
            }
            catch (Exception ex)
            {
                failure = ex;
            }
            if (!peekFailed) Log.Error(failure);
            peekFailed = true;
        }
        try
        {
            peek.Hide();   // out of the way: the next card tries again
        }
        catch (Exception)
        {
        }
    }

    void RenewPeek()
    {
        Peek old = peek;
        peek = new Peek();
        try
        {
            old.Dispose();
        }
        catch (Exception ex)
        {
            Log.Error(ex);   // a broken window that will not go is no reason to show no preview
        }
    }

    void HidePeek()
    {
        rest.Stop();
        peekAsked = false;
        peekRow = -1;
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

    // When, as a card says it at the right end of its first line: of a copied entry under the name of its day only the
    // time, otherwise the time today and the day before that; of a saved text always the day it was saved. The year
    // only when it is not this one. Null if nobody knows.
    string When(Entry entry)
    {
        if (!Dated(entry)) return null;
        return entry.Time.ToString(!entry.Saved && (headed || entry.Time.Date == DateTime.Today) ? "HH:mm"
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
    const char JointSign = '↵';
    static readonly string Joint = " " + JointSign + " ";   // between the lines of a text on a card of one line
    const string Hidden = "••••••••";   // after the name of a saved text: the content it hides
    const float MetaShare = 0.6f;   // how far that line goes from the card towards Muted, and how opaque its icon is
    // The small buttons at the right of a card, from the left: they are signs alone, named by their tooltips. A copied
    // entry has its arrow first.
    static readonly Part[] CardButtons = { Part.Show, Part.Copy, Part.Save, Part.Delete, Part.More };
    static readonly Part[] CopiedButtons = { Part.Arrow, Part.Show, Part.Copy, Part.Save, Part.Delete, Part.More };

    // What the card shows of an entry: of a saved one with a name only the name, so that a password never appears;
    // of a text its beginning.
    string CardText(Entry entry)
    {
        if (entry.Saved && entry.Name != null || entry.Picture) return entry.Label;
        string text = Plain(entry.Text);
        return text.Length > MaxBody ? text.Substring(0, MaxBody) : text;
    }

    // A text as a card writes it, a tab a space: on cards of one line its lines one after the other, a faint Joint
    // between them and the empty ones left out, so that what follows the first is seen too; otherwise as they are.
    string Plain(string text)
    {
        text = text.Trim().Replace('\t', ' ');
        if (cardLines > 1) return text.Replace("\r\n", "\n");
        List<string> lines = new List<string>();
        foreach (string line in text.Split('\r', '\n'))
            if (line.Trim().Length > 0) lines.Add(line.Trim());
        return string.Join(Joint, lines.ToArray());
    }

    // --- The words a search found, marked on the cards ---

    const string Ellipsis = "…";
    const int MarkContext = 30;       // of a text found further in, how much of it the card shows before the first word found
    const int MaxMarked = 100000;     // how much of a long text is looked through for it
    const float MarkTint = 0.35f;     // how far the shape under a word found goes from the card towards the accent colour
    const TextFormatFlags MarkFlags = TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding;

    // The text of a card laid out with the words the search found marked in it, or null if it shows none of them, or
    // nothing is searched: then the card writes it as usual. Of a saved text with a name, its name on one line; of a
    // text its lines, from a little before the first word found if from its beginning that one would be out of view
    // (wanted by the user on 19/10/2026: a card found by a word in its fifth line did not say why it was there). A
    // picture is found by its size, which its card says anyway.
    Marked MarkedText(Entry entry, int width)
    {
        if (searched.Length == 0 || entry.Picture || width <= 0) return null;
        bool named = entry.Saved && entry.Name != null;
        Font font = BodyFont(entry);
        Marked marked;
        if (marks.TryGetValue(entry, out marked) && marked.Width == width && marked.Font == font)
            return marked.Found.Count > 0 ? marked : null;
        int most = named ? 1 : cardLines;
        string text = named ? entry.Name : CardText(entry);
        marked = LayMarked(text, Texts.Find(text, searched), font, width, most);
        if (!named && (marked.Found.Count == 0 || marked.Found[0].First >= marked.Shown))
        {
            string whole = Plain(entry.Text);
            if (whole.Length > MaxMarked) whole = whole.Substring(0, MaxMarked);
            List<CharacterRange> found = Texts.Find(whole, searched);
            if (found.Count > 0 && found[0].First >= marked.Shown)
            {
                // At the start of a word, if one starts in that little before.
                int first = found[0].First, start = Math.Max(0, first - MarkContext);
                int space = whole.IndexOfAny(new char[] { ' ', '\n' }, start);
                if (start > 0 && space >= 0 && space < first) start = space + 1;
                string part = Ellipsis + whole.Substring(start, Math.Min(MaxBody, whole.Length - start));
                marked = LayMarked(part, Texts.Find(part, searched), font, width, most);
                // On a narrow card that little before can take all the lines: then from the word itself.
                if (marked.Found.Count == 0 || marked.Found[0].First >= marked.Shown)
                {
                    part = Ellipsis + whole.Substring(first, Math.Min(MaxBody, whole.Length - first));
                    marked = LayMarked(part, Texts.Find(part, searched), font, width, most);
                }
            }
        }
        marked.Width = width;
        marked.Font = font;
        marks[entry] = marked;
        return marked.Found.Count > 0 ? marked : null;
    }

    // The text of a card laid out by LayMarked when nothing is searched, kept until the next filling: laying it out
    // measures it many times.
    Marked Laid(Entry entry, string text, Font font, int width)
    {
        Marked marked;
        if (laid.TryGetValue(entry, out marked) && marked.Width == width && marked.Font == font && marked.Text == text) return marked;
        marked = LayMarked(text, new List<CharacterRange>(), font, width, cardLines);
        marked.Width = width;
        marked.Font = font;
        laid[entry] = marked;
        return marked;
    }

    // Lays a text out as a card writes it, in at most 'most' lines 'width' wide: a line breaks after a space, or inside
    // a word alone wider than it; the last one ends with an ellipsis if there is more.
    static Marked LayMarked(string text, List<CharacterRange> found, Font font, int width, int most)
    {
        Marked marked = new Marked();
        marked.Text = text;
        marked.Found = found;
        marked.Lines = new List<CharacterRange>();
        int at = 0;
        bool done = false;
        while (!done && marked.Lines.Count < most)
        {
            int paragraph = text.IndexOf('\n', at);
            if (paragraph < 0) paragraph = text.Length;
            int end = Fit(text, at, paragraph, font, width), last = end;
            while (last > at && text[last - 1] == ' ') last--;
            marked.Lines.Add(new CharacterRange(at, last - at));
            at = end;
            while (at < paragraph && text[at] == ' ') at++;
            if (at == paragraph)
            {
                done = paragraph == text.Length;
                at = paragraph + 1;
            }
        }
        marked.Cut = !done;
        if (marked.Cut)
        {
            // Room for the ellipsis, the line giving up its last letters.
            int line = marked.Lines.Count - 1;
            CharacterRange cut = marked.Lines[line];
            int length = cut.Length;
            while (length > 0 && Wide(text.Substring(cut.First, length) + Ellipsis, font) > width) length--;
            marked.Lines[line] = new CharacterRange(cut.First, length);
        }
        return marked;
    }

    // Where a line that starts at 'start' ends, in a paragraph that ends at 'stop': after the last space that lets it
    // fit, or else after as many letters of its first word as fit, one at least.
    static int Fit(string text, int start, int stop, Font font, int width)
    {
        int best = -1;
        for (int i = start; i <= stop; i++)
        {
            if (i < stop && text[i] != ' ') continue;
            if (Wide(text.Substring(start, i - start), font) > width) break;
            if (i == stop) return stop;
            best = i;
        }
        if (best > start) return best;
        int end = Math.Min(stop, start + 1);
        while (end < stop && Wide(text.Substring(start, end + 1 - start), font) <= width) end++;
        return end;
    }

    // How wide a piece of text is written, without the room Windows leaves around it. Measured on a Graphics: without
    // one, MeasureText counts that room whatever the flags say ("x": 15 pixels instead of 6).
    static int Wide(string text, Font font)
    {
        if (text.Length == 0) return 0;
        if (measurer == null) measurer = Graphics.FromImage(new Bitmap(1, 1));
        return TextRenderer.MeasureText(measurer, text, font, Size.Empty, MarkFlags).Width;
    }

    // Writes a text laid out by LayMarked in the body of a card, a faint shape of the accent colour under each word found,
    // the joints of its lines faint.
    void DrawMarked(Graphics g, Marked marked, Font font, Rectangle body, Color fill, Color ink)
    {
        // Where Windows would start it, with the room it leaves before a text.
        int lead = (TextRenderer.MeasureText(g, "x", font).Width - Wide("x", font)) / 2, y = body.Top, k = 0;
        List<CharacterRange> found = marked.Found;
        Color faint = Look.Blend(fill, Look.Muted, MetaShare);
        using (Brush tint = new SolidBrush(Look.Blend(fill, Look.Accent, MarkTint)))
            for (int i = 0; i < marked.Lines.Count; i++, y += font.Height)
            {
                CharacterRange line = marked.Lines[i];
                int x = body.Left + lead, at = line.First, end = line.First + line.Length;
                while (at < end)
                {
                    while (k < found.Count && found[k].First + found[k].Length <= at) k++;
                    bool inside = k < found.Count && found[k].First <= at;
                    int stop = Math.Min(end, inside ? found[k].First + found[k].Length : k < found.Count ? found[k].First : end);
                    string piece = marked.Text.Substring(at, stop - at);
                    int wide = Wide(piece, font);
                    if (inside)
                        using (GraphicsPath shape = Look.Rounded(new Rectangle(x - 1, y, wide + 2, font.Height - 1)))
                            g.FillPath(tint, shape);
                    Write(g, piece, font, new Point(x, y), ink, faint);
                    x += wide;
                    at = stop;
                }
                if (marked.Cut && i == marked.Lines.Count - 1) TextRenderer.DrawText(g, Ellipsis, font, new Point(x, y), ink, MarkFlags);
            }
    }

    // Writes a piece of the text of a card from 'at', the signs of the joints between its lines 'faint'.
    static void Write(Graphics g, string piece, Font font, Point at, Color ink, Color faint)
    {
        int x = at.X, start = 0;
        while (start < piece.Length)
        {
            int sign = piece.IndexOf(JointSign, start), end = sign < 0 ? piece.Length : sign;
            string part = piece.Substring(start, end - start);
            TextRenderer.DrawText(g, part, font, new Point(x, at.Y), ink, MarkFlags);
            if (sign < 0) return;
            x += Wide(part, font);
            TextRenderer.DrawText(g, JointSign.ToString(), font, new Point(x, at.Y), faint, MarkFlags);
            x += Wide(JointSign.ToString(), font);
            start = sign + 1;
        }
    }

    // Draws the preview of a picture at the left of the body of its card, in a box as tall as the body that it fills,
    // cut to its shape around its middle, with rounded corners and a thin edge. Returns the box.
    Rectangle DrawPicture(Graphics g, Image preview, Rectangle body)
    {
        Rectangle box = new Rectangle(body.Left, body.Top, (int)(body.Height * PictureShape), body.Height);
        float scale = Math.Max((float)box.Width / preview.Width, (float)box.Height / preview.Height);
        float wide = box.Width / scale, high = box.Height / scale;
        RectangleF taken = new RectangleF((preview.Width - wide) / 2, (preview.Height - high) / 2, wide, high);
        GraphicsState before = g.Save();
        using (GraphicsPath shape = Look.Rounded(box, (int)(PictureRadius * Zoom)))
        using (ImageAttributes edges = new ImageAttributes())
        using (Pen edge = new Pen(Look.Border))
        {
            edges.SetWrapMode(WrapMode.TileFlipXY);   // no halo at the edges of the box
            g.SetClip(shape, CombineMode.Intersect);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(preview, box, taken.X, taken.Y, taken.Width, taken.Height, GraphicsUnit.Pixel, edges);
            g.Restore(before);
            g.DrawPath(edge, shape);
        }
        return box;
    }

    // How tall the row of a card is, its air included, or that of the name of a day. Every card the same, the lines of
    // the options tall, whatever it holds; never taller than a list box of Windows allows.
    int CardHeight(object row)
    {
        if (row is Rule) return (int)((CardGap + 2 * CardLineGap) * Zoom);
        if (row is Heading) return Look.StrongFont.Height + (int)((HeadingAir + CardLineGap) * Zoom);
        int body = Math.Max(cardLines * Look.EntryFont.Height, Look.StrongEntryFont.Height);
        return Math.Min(MaxRow, 2 * (int)(CardPad * Zoom) + body + (int)(CardGap * Zoom));
    }

    // Where an entry comes from, in the small line beside it: the program it was copied in, or the folder of a saved
    // one; cut short after SourceLetters. Null for a saved text in the place shown itself, which the title already says
    // (wanted by the user on 28/10/2026: "Saved" on every card of Saved); one a search found deeper says its folder.
    string Source(Entry entry)
    {
        if (entry.Saved && place.HasSaved && Texts.SameName(entry.Group, place.Kind == PlaceKind.Folder ? place.Path : null)) return null;
        string source = entry.Saved ? (entry.Group != null ? Texts.Display(entry.Group) : Strings.SavedTitle)
            : entry.Origin != null ? entry.Origin.Name : null;
        return source != null && source.Length > SourceLetters ? source.Substring(0, SourceLetters).TrimEnd() + Ellipsis : source;
    }

    // How wide the small line of the cards is, the same on all of them: as the widest of the rows needs, so that its
    // parts make columns with no hole in the middle (wanted by the user on 24/10/2026: MetaWidth on every card left a
    // short name far from its time); MetaWidth at most.
    int MetaWide()
    {
        int side = Look.NoteFont.Height, gap = (int)(PartGap * Zoom), widest = 0;
        foreach (object row in rows)
        {
            Entry entry = row as Entry;
            if (entry == null) continue;
            string source = Source(entry), when = When(entry);
            widest = Math.Max(widest, (source == null ? 0 : Wide(source, Look.NoteFont)) + (when == null ? 0 : Wide(when, Look.NoteFont)));
        }
        return Math.Min((int)(MetaWidth * Zoom), side + 3 * gap + widest + 1);
    }

    // What a card writes its entry in, which says what it is (chosen by the user on 24/10/2026 among a sign and both;
    // before, a word at the end of the small line, which pushed the program and the time of those cards to the left):
    // a piece of code in a font of fixed width, a link underlined in blue. The name of a saved text is written as any
    // text, the content it hides as faint dots after it (Hidden).
    Font BodyFont(Entry entry)
    {
        if (entry.Saved && entry.Name != null) return Look.EntryFont;
        if (entry.Category == Category.Code) return Look.CodeFont;
        if (entry.Category == Category.Link) return Look.LinkFont;
        return Look.EntryFont;
    }

    Color BodyInk(Entry entry)
    {
        return BodyFont(entry) == Look.LinkFont ? Look.Link : Look.Ink;
    }

    const TextFormatFlags MetaFlags = TextFormatFlags.VerticalCenter | LineFlags | TextFormatFlags.NoPadding;

    // The parts of the card of an entry in its row (see CardParts): the entry first, at the top left; at the right end
    // of its first line the small line of where and when, as wide on every card (MetaWide) unless the entry would keep
    // less than its share, and the small buttons in the same room, wider if they need it; before them its number.
    // The same whether the pointer is on the card or not, so that no card changes height under it, and every card
    // has its line on its first line (wanted by the user on 19/10/2026: beside the buttons or under them as there was
    // room, the cards of the same kind were of different heights). DrawEntry writes that line at the right end of Meta.
    CardParts Lay(Entry entry, Rectangle row)
    {
        int inset = (int)(CardInset * Zoom), pad = (int)(CardPad * Zoom), gap = (int)(CardLineGap * Zoom), side = (int)(PartHeight * Zoom);
        int between = (int)(PartGap * Zoom) / 2;
        CardParts parts = new CardParts();
        parts.Card = CardBox(row);
        int left = parts.Card.Left + inset, right = parts.Card.Right - inset, top = parts.Card.Top + pad;
        // The buttons in the middle of the first line of the entry.
        int middle = top + Look.EntryFont.Height / 2, end = right;
        Part[] buttons = entry.Saved ? CardButtons : CopiedButtons;
        for (int i = buttons.Length - 1; i >= 0; i--)
        {
            parts.Buttons[buttons[i]] = new Rectangle(end - side, middle - side / 2, side, side);
            end -= side + between;
        }
        int meta = Look.NoteFont.Height, room = right - left;
        int area = Math.Max(right - end - between, Math.Min(metaWide, (int)(room * MetaRoom)));
        parts.Meta = new Rectangle(right - area, middle - meta / 2, area, meta);
        end = parts.Meta.Left - gap;
        if (entry.Number > 0)
        {
            int high = Font.Height, width = high + NumberPadding;
            parts.Number = new Rectangle(end - width, middle - high / 2, width, high);
            end = parts.Number.Left - between;
        }
        parts.Body = Rectangle.FromLTRB(left, top, end - (int)(BodyGap * Zoom), Math.Max(top, parts.Card.Bottom - pad));
        return parts;
    }

    // The card in its row, with air under it.
    Rectangle CardBox(Rectangle row)
    {
        return Rectangle.FromLTRB(row.Left + 1, row.Top, row.Right - 2, row.Bottom - (int)(CardGap * Zoom));
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
            case Part.Arrow: return entry.Pinned ? Strings.Unpin : Strings.KeepTop;
            case Part.More: return Strings.MoreItem;
            case Part.Number: return string.Format(Strings.NumberTip, NumberName(entry.Number));
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
            case Part.Arrow: return Sign.Up;
            default: return Sign.More;
        }
    }

    // The part of a card at a point, Body anywhere else on the card, None outside the cards.
    Part PartAt(int index, Point where)
    {
        object row = index < 0 ? null : cardList.Items[index];
        if (row is FolderCard) return CardBox(cardList.GetItemRectangle(index)).Contains(where) ? Part.Body : Part.None;
        Entry entry = row as Entry;
        if (entry == null) return Part.None;
        CardParts parts = Lay(entry, cardList.GetItemRectangle(index));
        foreach (KeyValuePair<Part, Rectangle> button in parts.Buttons)
            if (button.Value.Contains(where)) return button.Key;
        if (entry.Number > 0 && parts.Number.Contains(where)) return Part.Number;
        return parts.Card.Contains(where) ? Part.Body : Part.None;
    }

    // A row of the list: the name of a day, or the card of an entry.
    void DrawEntry(object sender, DrawItemEventArgs e)
    {
        if (e.Index < 0) return;
        Graphics g = e.Graphics;
        using (Brush plain = new SolidBrush(cardList.BackColor)) g.FillRectangle(plain, e.Bounds);
        if (cardList.Items[e.Index] is Rule)
        {
            // In the middle of the air between the folders and the texts, as wide as the cards.
            int middle = e.Bounds.Top + (e.Bounds.Height - (int)(CardGap * Zoom)) / 2;
            using (Pen line = new Pen(Look.Border)) g.DrawLine(line, e.Bounds.Left + 1, middle, e.Bounds.Right - 2, middle);
            return;
        }
        Heading heading = cardList.Items[e.Index] as Heading;
        if (heading != null)
        {
            DrawHeading(g, heading.Text, e.Bounds);
            return;
        }
        FolderCard card = cardList.Items[e.Index] as FolderCard;
        if (card != null)
        {
            DrawFolderRow(g, e, card.Folder.Label, card.Count > 0 ? Count(card.Count, Strings.TextOne, Strings.TextMany) : null);
            return;
        }
        Entry entry = (Entry)cardList.Items[e.Index];
        bool selected = Many ? picked.Contains(entry.Id) : (e.State & DrawItemState.Selected) != 0;
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
            Rectangle box = DrawPicture(g, entry.Preview, parts.Body);
            TextRenderer.DrawText(g, entry.Label, e.Font, Rectangle.FromLTRB(box.Right + (int)(2 * PreviewMargin * Zoom), parts.Body.Top,
                parts.Body.Right, parts.Body.Bottom), Look.Ink, TextFormatFlags.VerticalCenter | LineFlags);
        }
        else
        {
            // With what a search found in it marked, laid out by hand (MarkedText); so too the lines of a text joined on
            // one, for their faint joints, and a text on cards of more lines (Windows, cutting it with an ellipsis, wrote
            // what did not fit on its last line, the line breaks left out: the third line of a piece of code followed
            // the second); otherwise as Windows writes it.
            Marked marked = MarkedText(entry, parts.Body.Width);
            bool named = entry.Saved && entry.Name != null;
            string text = CardText(entry);
            Font font = BodyFont(entry);
            Color ink = BodyInk(entry);
            if (marked != null) DrawMarked(g, marked, font, parts.Body, fill, ink);
            else if (named)
            {
                // The name, and after it, faint, the dots of what it hides, as long as the name leaves room for them.
                int dots = Wide(Hidden, font), gap = (int)(PartGap * Zoom);
                int name = TextRenderer.MeasureText(g, entry.Name, font, Size.Empty, LineFlags & ~TextFormatFlags.EndEllipsis).Width;
                int wide = Math.Min(name, Math.Max(0, parts.Body.Width - dots - gap));
                if (wide == 0) wide = parts.Body.Width;
                TextRenderer.DrawText(g, entry.Name, font, new Rectangle(parts.Body.Left, parts.Body.Top, wide, font.Height),
                    ink, TextFormatFlags.VerticalCenter | LineFlags);
                if (parts.Body.Left + wide + gap + dots <= parts.Body.Right)
                    TextRenderer.DrawText(g, Hidden, font, new Point(parts.Body.Left + wide + gap, parts.Body.Top),
                        Look.Blend(fill, Look.Muted, MetaShare), MarkFlags);
            }
            else if (cardLines > 1 || text.IndexOf(JointSign) >= 0) DrawMarked(g, Laid(entry, text, font, parts.Body.Width), font, parts.Body, fill, ink);
            else TextRenderer.DrawText(g, text, font, parts.Body, ink, LineFlags);
        }

        // At the right end of the first line, small and faint, so that the entry is what stands out: the program it was
        // copied in with its icon, or the folder of a saved one; when; last, in a place of its own, the sign of what it
        // is. Under the pointer the small buttons take its place, the one going as the other comes (wanted by the user
        // on 19/10/2026: always there on every card, they made the list look like a toolbar).
        float buttons = Math.Max(0, Math.Min(1, 2 * glow - 1)), written = Math.Max(0, 1 - 2 * glow);
        if (written > 0)
        {
            // In columns, the same on every card (wanted by the user on 24/10/2026: the icons were where the length of
            // the line put them): the icon at the left of the room, the name after it; the time at its right end.
            int side = parts.Meta.Height, gap = (int)(PartGap * Zoom);
            string when = When(entry) ?? "", source = Source(entry);
            int timeWide = TextRenderer.MeasureText(g, when, Look.NoteFont, Size.Empty, MetaFlags & ~TextFormatFlags.EndEllipsis).Width;
            Rectangle time = Rectangle.FromLTRB(parts.Meta.Right - timeWide, parts.Meta.Top, parts.Meta.Right, parts.Meta.Bottom);
            Rectangle icon = new Rectangle(parts.Meta.Left, parts.Meta.Top, side, side);
            Rectangle name = Rectangle.FromLTRB(icon.Right + gap, parts.Meta.Top, time.Left - 2 * gap, parts.Meta.Bottom);
            Color faint = Look.Blend(fill, Look.Muted, MetaShare * written);
            if (entry.Saved && source != null) Look.Draw(g, Sign.Star, icon, faint);
            else if (entry.Origin != null && entry.Origin.Icon != null) Look.DrawFaint(g, entry.Origin.Icon, icon, MetaShare * written);
            else if (entry.Origin != null) Look.Draw(g, Sign.Apps, icon, faint);
            if (source != null && name.Width > 0) TextRenderer.DrawText(g, source, Look.NoteFont, name, faint, MetaFlags);
            TextRenderer.DrawText(g, when, Look.NoteFont, time, faint, MetaFlags);
        }

        // Before them, always, the number; then the buttons in their own colours, the arrow faint unless it keeps the
        // entry at the top (which the name over the cards says too), "more" grey.
        if (entry.Number > 0)
        {
            // The digit that pastes it from anywhere, in a rounded frame in the accent colour.
            // A click on it changes it or takes it away: under the pointer it is filled faintly, as the buttons are.
            using (GraphicsPath shape = Look.Rounded(Rectangle.FromLTRB(parts.Number.Left, parts.Number.Top, parts.Number.Right - 1, parts.Number.Bottom - 1)))
            using (Pen edge = new Pen(Look.Accent))
            using (Brush lit = new SolidBrush(Look.Blend(fill, Look.Accent, 0.15f)))
            {
                if (IsHovered(e.Index, Part.Number)) g.FillPath(lit, shape);
                g.DrawPath(edge, shape);
            }
            TextRenderer.DrawText(g, entry.Number.ToString(), Font, parts.Number, Look.Accent, TextFormatFlags.HorizontalCenter
                | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
        }
        if (buttons <= 0) return;
        foreach (KeyValuePair<Part, Rectangle> button in parts.Buttons)
        {
            Sign sign = PartSignOf(button.Key, entry);
            bool hovered = IsHovered(e.Index, button.Key);
            Color colour = button.Key == Part.More ? (hovered ? Look.Ink : Look.Blend(fill, Look.Muted, 0.8f))
                : button.Key == Part.Arrow ? (entry.Pinned ? Look.Accent : hovered ? Look.Ink : Look.Blend(fill, Look.Muted, 0.5f))
                : Look.Own(sign);
            DrawPart(g, hovered, button.Value, sign, Look.Blend(fill, colour, buttons), fill);
        }
    }

    // The row of a folder: not a card, but a place to go into, drawn as the rows of the sidebar (wanted by the user on
    // 28/10/2026: as cards, the folders looked like the texts with names). Lit under the pointer, or fully when a card
    // dragged would go into it; the selected one tinted, with its mark. Its sign in the accent colour, its name, and at the
    // right end, faint, a note (how many texts it holds itself, as the sidebar counts them) and the arrow that says it
    // opens.
    void DrawFolderRow(Graphics g, DrawItemEventArgs e, string name, string note)
    {
        bool selected = !Many && (e.State & DrawItemState.Selected) != 0;
        float glow = e.Index == dropCard ? 1 : cardList.Glow.Of(e.Index);
        Rectangle box = CardBox(e.Bounds);
        Look.DrawRow(g, box, cardList.BackColor, selected, glow);
        if (selected) Look.DrawMark(g, Look.MarkIn(box));
        Color back = selected ? Look.Selected : Look.Blend(cardList.BackColor, Look.Soft, glow);
        int inset = (int)(CardInset * Zoom), side = Look.EntryFont.Height, middle = box.Top + box.Height / 2;
        int right = box.Right - inset, small = side * 3 / 4;
        Look.Draw(g, Sign.Closed, new Rectangle(right - small, middle - small / 2, small, small), Look.Muted);
        right -= small + (int)(PartGap * Zoom);
        if (note != null)
        {
            int wide = TextRenderer.MeasureText(g, note, Look.NoteFont, Size.Empty, MetaFlags & ~TextFormatFlags.EndEllipsis).Width;
            TextRenderer.DrawText(g, note, Look.NoteFont, Rectangle.FromLTRB(right - wide, middle - side / 2, right, middle + side / 2),
                Look.Blend(back, Look.Muted, MetaShare), MetaFlags);
            right -= wide + (int)(BodyGap * Zoom);
        }
        Look.Draw(g, Sign.FullFolder, new Rectangle(box.Left + inset, middle - side / 2, side, side), Look.Accent);
        int left = box.Left + inset + side + (int)(PartGap * Zoom);
        TextRenderer.DrawText(g, name, Look.EntryFont, Rectangle.FromLTRB(left, middle - side / 2, right, middle + side / 2),
            Look.Ink, TextFormatFlags.VerticalCenter | LineFlags);
    }

    // The name of a day, in its row among the cards or in the bar over them: low in it, with a thin line under it, across.
    void DrawHeading(Graphics g, string text, Rectangle bounds)
    {
        int height = Look.StrongFont.Height, bottom = bounds.Bottom - (int)(CardLineGap * Zoom);
        TextRenderer.DrawText(g, text, Look.StrongFont, new Rectangle(bounds.Left + 2, bottom - height - 2, bounds.Width, height),
            Look.Ink, TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
        using (Pen line = new Pen(Look.Border)) g.DrawLine(line, bounds.Left, bottom, bounds.Right - 2, bottom);
    }

    bool IsHovered(int index, Part part)
    {
        return index == hoverRow && part == hoverPart;
    }

    // A small button of a card: its sign in the colour given, on a faint shape under the pointer.
    void DrawPart(Graphics g, bool hovered, Rectangle area, Sign sign, Color colour, Color fill)
    {
        if (hovered)
            using (GraphicsPath path = Look.Rounded(Rectangle.FromLTRB(area.Left, area.Top, area.Right - 1, area.Bottom - 1)))
            using (Brush brush = new SolidBrush(Look.Blend(fill, Look.Ink, 0.08f)))
                g.FillPath(brush, path);
        int side = Font.Height * 4 / 5;
        Look.Draw(g, sign, new Rectangle(area.Left + (area.Width - side) / 2, area.Top + (area.Height - side) / 2, side, side), colour);
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
        if (!Change(delegate { clips.KeepAtTop(entry.Id, !entry.Pinned); })) return;
        Say(string.Format(!entry.Pinned ? Strings.Pinned : Strings.Unpinned, Shorten(entry.ToString())));
        ReloadCopied();
    }

    // --- Several cards at once ---

    // Whether several cards are picked: then they are the selection, drawn with its edge, and what acts on it (the bar in
    // the place of the tabs, the menu, the keys) acts on all of them. Never one alone: that is the selected card.
    bool Many
    {
        get { return picked.Count > 0; }
    }

    // What the commands act on: the cards picked, in their order, or else the selected one.
    List<Entry> Targets()
    {
        List<Entry> found = new List<Entry>();
        if (!Many)
        {
            if (Selected != null) found.Add(Selected);
            return found;
        }
        foreach (object row in cardList.Items)
        {
            Entry entry = row as Entry;
            if (entry != null && picked.Contains(entry.Id)) found.Add(entry);
        }
        return found;
    }

    // The copied entries among them, which have an arrow, and whether all of those are at the top already.
    List<Entry> PickedCopied()
    {
        return Targets().FindAll(delegate(Entry entry) { return !entry.Saved; });
    }

    // The texts among them, which can be copied together: the pictures cannot.
    List<Entry> PickedTexts()
    {
        return Targets().FindAll(delegate(Entry entry) { return !entry.Picture; });
    }

    bool AllPinned()
    {
        List<Entry> copied = PickedCopied();
        return copied.Count > 0 && copied.TrueForAll(delegate(Entry entry) { return entry.Pinned; });
    }

    // A click on a card with Ctrl adds it to the cards picked, or takes it away; with Shift picks those from the card the
    // range starts at to this one, and with Ctrl too adds them to those picked. The card selected so far is the first
    // picked. The card clicked becomes the selected one.
    void Pick(int row, Keys keys)
    {
        HidePeek();
        HideHoverTip();
        Entry entry = cardList.Items[row] as Entry;
        if (entry == null) return;   // the name of a day
        if (!Many && Selected != null) picked.Add(Selected.Id);
        if ((keys & Keys.Shift) != 0)
        {
            int from = pickFrom >= 0 && pickFrom < cardList.Items.Count ? pickFrom : row;
            if ((keys & Keys.Control) == 0) picked.Clear();
            for (int i = Math.Min(from, row); i <= Math.Max(from, row); i++)
            {
                Entry each = cardList.Items[i] as Entry;
                if (each != null) picked.Add(each.Id);
            }
        }
        else
        {
            if (!picked.Remove(entry.Id)) picked.Add(entry.Id);
            pickFrom = row;
        }
        ShowPicks(row);
    }

    // Shift with an arrow: the range goes on to the card before or after the selected one.
    void PickStep(int by)
    {
        for (int index = cardList.SelectedIndex + by; cardList.SelectedIndex >= 0 && index >= 0 && index < cardList.Items.Count; index += by)
            if (cardList.Items[index] is Entry)
            {
                Pick(index, Keys.Shift);
                return;
            }
    }

    void PickAll()
    {
        foreach (object row in cardList.Items)
        {
            Entry entry = row as Entry;
            if (entry != null) picked.Add(entry.Id);
        }
        ShowPicks(cardList.SelectedIndex);
    }

    // After picking: the card clicked selected, among those picked. Left with one, that is the selected card again; with
    // none, nothing is selected.
    void ShowPicks(int row)
    {
        int only = -1;
        if (picked.Count == 1)
        {
            foreach (long id in picked) only = CardRow(id);
            picked.Clear();
        }
        keepPicks = true;
        if (only >= 0) cardList.SelectedIndex = only;
        else if (!Many) cardList.ClearSelected();
        else cardList.SelectedIndex = row;
        keepPicks = false;
        cardList.Invalidate();
        ShowPickBar();
    }

    // The row of the card of an entry, or -1.
    int CardRow(long id)
    {
        for (int i = 0; i < cardList.Items.Count; i++)
        {
            Entry entry = cardList.Items[i] as Entry;
            if (entry != null && entry.Id == id) return i;
        }
        return -1;
    }

    // Back to one card selected: the one that was, the last clicked.
    void ClearPicks()
    {
        if (!Many) return;
        picked.Clear();
        cardList.Invalidate();
        ShowPickBar();
    }

    // The arrow of the selected entry, or of the copied ones among the cards picked: all of them at the top, or, if they
    // are already, all back among the others.
    void PinSelected()
    {
        if (!Many)
        {
            TogglePin(Selected);
            return;
        }
        List<Entry> copied = PickedCopied();
        if (copied.Count == 0) return;
        bool up = !AllPinned();
        if (!Change(delegate
        {
            using (Transaction transaction = clips.Begin())
            {
                foreach (Entry entry in copied) clips.KeepAtTop(entry.Id, up);
                transaction.Commit();
            }
        })) return;
        Say(string.Format(up ? Strings.PinnedMany : Strings.UnpinnedMany, copied.Count));
        ReloadCopied();
    }

    // Where the cards picked can go, in a menu: among the saved texts out of the folders, or into a folder; a copied entry
    // going there is saved there (wanted by the user on 20/10/2026). Each place where some of them are not already. Says
    // whether there is any.
    bool ListMoves(ToolStripItemCollection items, List<Entry> targets)
    {
        items.Clear();
        bool copied = targets.Exists(delegate(Entry entry) { return !entry.Saved; }), any = false;
        List<string> places = GroupNames();
        places.Insert(0, null);
        foreach (string each in places)
        {
            string group = each;   // a variable of its own for the delegate
            bool goes = targets.Exists(delegate(Entry entry) { return !entry.Saved || !Texts.SameName(entry.Group, group); });
            items.Add(group != null ? Texts.Display(group) : copied ? Strings.SavedTitle : Strings.OutOfFolders, null,
                delegate { MoveAll(targets, group); }).Enabled = goes;
            any |= goes;
        }
        return any;
    }

    // Puts the cards picked into a folder, or among the saved texts out of the folders, all of them or none: the copied
    // ones are saved there without a name, formatted if they came so and are among the last copies.
    void MoveAll(List<Entry> targets, string group)
    {
        if (!Change(delegate
        {
            using (Transaction transaction = clips.Begin())
            {
                long folder = clips.FindFolder(group);
                foreach (Entry entry in targets)
                {
                    if (entry.Saved)
                    {
                        clips.Move(entry.Id, folder);
                        continue;
                    }
                    clips.Save(entry.Id, null, folder);   // its number too stays
                    string html, rtf;
                    if (!entry.Picture && Rich.Kept(entry.Text, out html, out rtf)) clips.Change(entry.Id, null, entry.Text, html, rtf, folder);
                }
                clips.Reveal(folder);   // or its folder would not be seen in the sidebar
                transaction.Commit();
            }
        })) return;
        picked.Clear();
        Say(group == null ? string.Format(Strings.PickedSaved, targets.Count)
            : string.Format(Strings.PickedInFolder, targets.Count, Shorten(Texts.Display(group))));
        Reload();
    }

    // The texts of the cards picked onto the clipboard, one after the other (Texts.Concatenate); the pictures stay out.
    // Plain text only, as a typing would take it, and private if one of them is a saved text. Of one card alone, the
    // entry itself, as its copy sign does. The cards stay picked, so that something else can still be done with them.
    void CopyPicked()
    {
        if (!Many)
        {
            if (Selected != null) Use(Selected);
            return;
        }
        List<Entry> texts = PickedTexts();
        if (texts.Count == 0) return;
        try
        {
            DataObject data = NewData(texts.Exists(delegate(Entry entry) { return entry.Saved; }));
            data.SetText(Texts.Concatenate(texts));
            Clipboard.SetDataObject(data, true);
            ownCopy = Native.GetClipboardSequenceNumber();
        }
        catch (Exception ex)   // the clipboard locked by another program
        {
            ShowError(ex);
            return;
        }
        int pictures = Targets().Count - texts.Count;
        Say(string.Format(Strings.JoinedOnClipboard, texts.Count) + (pictures > 0 ? " " + Strings.JoinedWithoutPictures : "") + "\t"
            + (hotkeyBox.Checked ? string.Format(Strings.PasteOrTypeThem, HotkeyName) : Strings.PasteThem));
    }

    // The bar of the cards picked: how many, then what to do with all of them, each with its sign. The list keeps the
    // focus, so that Esc and the arrows still reach it.
    void WirePicks()
    {
        pickCount.Font = Look.StrongFont;
        pickCount.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        pickCopy.Sign = Sign.Copy;
        pickCopy.Text = Strings.CopyButton;
        pickPin.Sign = Sign.Up;
        pickMove.Sign = Sign.Move;
        pickMove.Text = Strings.MoveTo;
        pickDelete.Sign = Sign.Delete;
        pickDelete.Text = Strings.Delete;
        pickNone.Sign = Sign.Cancel;   // its tooltip says what it does
        foreach (RoundButton button in new RoundButton[] { pickCopy, pickPin, pickMove, pickDelete, pickNone })
        {
            Look.Style(button, false);
            button.Subtle = true;
            button.TabStop = false;
            button.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        }
        pickCopy.Click += delegate { cardList.Focus(); CopyPicked(); };
        pickPin.Click += delegate { cardList.Focus(); PinSelected(); };
        pickMove.Click += delegate
        {
            cardList.Focus();
            ListMoves(pickPlaces.Items, Targets());
            pickPlaces.Show(pickMove, new Point(0, pickMove.Height));
        };
        pickDelete.Click += delegate { cardList.Focus(); DeleteSelected(); };
        pickNone.Click += delegate { cardList.Focus(); ClearPicks(); };
        Look.Style(pickPlaces);
        Tip(string.Format(Strings.WithKeys, Strings.CopyPickedTip, NameOf(CopyKeys)), pickCopy);
        Tip(string.Format(Strings.WithKeys, Strings.Delete, NameOf(DeleteKeys)), pickDelete);
        Tip(string.Format(Strings.WithKeys, Strings.PickNone, Strings.EscapeKeyName), pickNone);
    }

    // While cards are picked their bar takes the place of the tabs, which a search or another tab would not keep anyway.
    void ShowPickBar()
    {
        bool many = Many;
        pickBar.Visible = many;
        tabs.Visible = !many;
        ShowFolderButton();
        if (!many) return;
        List<Entry> targets = Targets();
        pickCount.Text = string.Format(Strings.PickedCount, targets.Count);
        pickPin.Text = AllPinned() ? Strings.Unpin : Strings.KeepTop;
        Tip(string.Format(Strings.WithKeys, pickPin.Text, NameOf(PinKeys)), pickPin);
        pickMove.Enabled = ListMoves(pickPlaces.Items, targets);
        PlacePickBar(PickedCopied().Count > 0, PickedTexts().Count > 0);
    }

    // Its buttons at its right end, each as wide as what it says, the arrow only where some card picked is a copied one,
    // the copy only where some is a text; the count takes the rest.
    void PlacePickBar(bool arrow, bool copy)
    {
        if (!shown) return;
        int gap = (int)(Gap * Zoom), high = (int)(ButtonHeight * Zoom), top = (pickBar.Height - high) / 2, right = pickBar.Width;
        pickPin.Visible = arrow;
        pickCopy.Visible = copy;
        foreach (RoundButton button in new RoundButton[] { pickNone, pickDelete, pickMove, pickPin, pickCopy })
        {
            if (button == pickPin && !arrow || button == pickCopy && !copy) continue;
            int wide = button.Text.Length == 0 ? high : TextRenderer.MeasureText(button.Text, button.Font).Width + (int)(PickPadding * Zoom);
            right -= wide;
            button.SetBounds(right, top, wide, high);
            right -= gap;
        }
        int inset = (int)(2 * Zoom);   // where the names of the days start, under it
        pickCount.SetBounds(inset, 0, Math.Max(0, right - inset), pickBar.Height);
    }

    // Where saved texts are shown, a new folder at the right end of the tabs, as wide as what it says.
    void ShowFolderButton()
    {
        folderButton.Visible = place.HasSaved;
        if (!shown) return;
        int high = (int)(ButtonHeight * Zoom);
        int wide = TextRenderer.MeasureText(folderButton.Text, folderButton.Font).Width + (int)(PickPadding * Zoom);
        folderButton.SetBounds(tabs.ClientSize.Width - wide, (tabs.Height - high) / 2, wide, high);
    }

    // The card before or after the selected one, a folder too, over the names of the days.
    void Step(int by)
    {
        for (int index = cardList.SelectedIndex + by; index >= 0 && index < cardList.Items.Count; index += by)
            if (!(cardList.Items[index] is Heading))
            {
                cardList.SelectedIndex = index;
                return;
            }
    }

    // Enter, or a double click: the entry on the clipboard, and back to the window the list hotkey was pressed in.
    void Accept()
    {
        if (SelectedFolder != null)
        {
            OpenCard(SelectedFolder);
            return;
        }
        Entry entry = Selected;
        if (entry != null && !busy && !Many && Use(entry)) GoBack();   // of several cards picked, none is the one chosen
    }

    // A folder among the cards opens as its row in the sidebar would; the folders it is in open too, so that its row is
    // seen there.
    void OpenCard(FolderCard card)
    {
        Entry folder = card.Folder;
        if (!Change(delegate
        {
            for (string parent = Texts.Parent(folder.Name); parent != null; parent = Texts.Parent(parent))
            {
                Entry above = FolderEntry(parent);
                if (above == null || above.Open) continue;
                clips.OpenFolder(above.FolderId, true);
                above.Open = true;
            }
        })) return;
        savedOpen = true;
        Place at = new Place(PlaceKind.Folder);
        at.Path = folder.Name;
        at.Id = folder.FolderId;
        Go(at);
    }

    // From a folder to the one it is in, or to Saved. Elsewhere nothing.
    void GoUp()
    {
        if (place.Kind == PlaceKind.Folder) GoUpTo(Texts.Parent(place.Path));
    }

    // From a folder to one it is in, or to Saved (null); the folder the way down went through is the one selected there.
    void GoUpTo(string path)
    {
        if (place.Kind != PlaceKind.Folder) return;
        string from = place.Path;
        while (from != null && !Texts.SameName(Texts.Parent(from), path)) from = Texts.Parent(from);
        Entry above = path == null ? null : FolderEntry(path);
        Place at = new Place(path == null ? PlaceKind.Saved : PlaceKind.Folder);
        at.Path = path;
        if (above != null) at.Id = above.FolderId;
        Go(at);
        for (int i = 0; i < cardList.Items.Count; i++)
        {
            FolderCard card = cardList.Items[i] as FolderCard;
            if (card == null || !Texts.SameName(card.Folder.Name, from)) continue;
            cardList.SelectedIndex = i;
            return;
        }
    }

    // The menu of a folder among the cards: open it, or what the menu of its row in the sidebar does.
    void WireCardMenu()
    {
        ToolStripItem openItem = cardMenu.Items.Add(Strings.Open, null, delegate { OpenCard(SelectedFolder); });
        ToolStripItem renameItem = cardMenu.Items.Add(Strings.RenameDots, null, delegate { EditGroup(SelectedFolder.Folder.Name); });
        ToolStripMenuItem moveItem = new ToolStripMenuItem(Strings.MoveTo);
        cardMenu.Items.Add(moveItem);
        ToolStripItem deleteItem = cardMenu.Items.Add(Strings.Delete, null, delegate { DeleteFolder(SelectedFolder.Folder); });
        Look.Mark(openItem, Sign.Open);
        Look.Mark(renameItem, Sign.Edit);
        Look.Mark(moveItem, Sign.Move);
        Look.Mark(deleteItem, Sign.Delete);
        Look.Style((ToolStripDropDownMenu)moveItem.DropDown);
        Look.Style(cardMenu);
        cardMenu.Opening += delegate(object sender, System.ComponentModel.CancelEventArgs e)
        {
            HidePeek();
            if (SelectedFolder == null) e.Cancel = true;
            else ListPlaces(moveItem, SelectedFolder.Folder);
        };
    }

    FolderCard SelectedFolder
    {
        get { return cardList.SelectedItem as FolderCard; }
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

    // A text as a QR code, for a phone to read off the screen (wanted by the user on 25/10/2026, to take an entry from
    // the PC to the phone); too long for one, the status line says how much fits.
    void ShowQr(Entry entry)
    {
        if (entry == null) return;
        if (!QrForm.Display(this, entry.Text)) Warn(string.Format(Strings.QrTooLong, Qr.MaxBytes));
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
        if (!whereLeftBox.Checked) ShowHistory();   // unless the option keeps where the list was left
        options.Hide();   // above the list, it would cover it
        RestoreWindow();
        search.Focus();
    }

    // All the entries, of every kind, from the latest copy: where the list hotkey opens, wanted by the user.
    void ShowHistory()
    {
        place = new Place(PlaceKind.History);
        ClearPicks();
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
                        long folder = clips.FindFolder(edit.Group);
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
                    long parentId = clips.FindFolder(edit.Group);
                    if (old == null) clips.AddFolder(parentId, edit.EntryName);
                    else clips.PlaceFolder(clips.FindFolder(old), parentId, edit.EntryName);
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
            long folder = clips.FindFolder(group);
            if (entry.IsGroup) clips.MoveFolder(entry.FolderId, folder);
            else clips.Move(entry.Id, folder);
            clips.Reveal(folder);
        })) return;
        Say(group == null ? string.Format(Strings.LeftFolder, Shorten(entry.Label))
            : string.Format(Strings.InFolder, Shorten(entry.Label), Shorten(Texts.Display(group))));
        if (entry.IsGroup) Follow(entry.Name, Texts.Join(group, Texts.Leaf(entry.Name)));
        Reload();
    }

    // Deletes the selected entry, or the cards picked: a copied entry alone without asking, a saved one or several after
    // asking, those picked all of them or none.
    void DeleteSelected()
    {
        if (SelectedFolder != null && !Many)
        {
            DeleteFolder(SelectedFolder.Folder);
            return;
        }
        List<Entry> targets = Targets();
        if (targets.Count == 0) return;
        Entry entry = targets[0];
        if (targets.Count > 1)
        {
            int saved = targets.FindAll(delegate(Entry each) { return each.Saved; }).Count;
            if (!Ask(string.Format(Strings.AskDeleteMany, targets.Count) + (saved > 0 ? string.Format(Strings.DeleteManySaved, saved) : ""))
                || !Change(delegate
                {
                    using (Transaction transaction = clips.Begin())
                    {
                        foreach (Entry each in targets) clips.Remove(each.Id);
                        transaction.Commit();
                    }
                })) return;
            picked.Clear();
            Say(string.Format(Strings.DeletedMany, targets.Count));
        }
        else if (!entry.Saved)
        {
            if (!Change(delegate { clips.Remove(entry.Id); })) return;
        }
        else if (!Ask(string.Format(Strings.AskDelete, entry.Label)) || !Change(delegate { clips.Remove(entry.Id); })) return;
        Reload();
    }

    // Deletes a folder, only the folder: what it holds is too easily a password to be deleted along. It moves up a
    // level; if the folder was shown, or one inside it, Saved is shown.
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
        if (place.Kind == PlaceKind.Folder && Texts.Within(place.Path, folder.Name)) place = new Place(PlaceKind.Saved);
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
        if (!inCards && !navList.Focused && !homeList.Focused) return base.ProcessCmdKey(ref msg, keyData);
        switch (keyData)
        {
            case NewKeys:
                Edit(null);
                return true;
            case GroupKeys:
                EditGroup(null);
                return true;
            case SaveKeys:
                if (!Many) Edit(Selected);   // one at a time, in its window
                return true;
            case DeleteKeys:
                DeleteSelected();
                return true;
            case PinKeys:
                PinSelected();
                return true;
        }
        // The arrows of the sidebar move among its places.
        if (!inCards) return base.ProcessCmdKey(ref msg, keyData);
        switch (keyData)
        {
            case Keys.Escape:
                if (Many) ClearPicks();   // the cards picked first, then back
                else GoBack();
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
            case Keys.Shift | Keys.Up:
                PickStep(-1);
                return true;
            case Keys.Shift | Keys.Down:
                PickStep(1);
                return true;
            case AllKeys:
                if (!cardList.Focused) break;   // in the search box, all its text
                PickAll();
                return true;
            case CopyKeys:
                if (!cardList.Focused) break;   // in the search box, the text selected there
                CopyPicked();
                return true;
            case BackKey:
                if (!cardList.Focused) break;   // in the search box, it rubs out
                GoUp();
                return true;
            case UpKeys:
                GoUp();
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
