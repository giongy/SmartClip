using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

// What the sidebar lists: all the entries, those kept at the top, the saved ones and their folders, the programs
// things were copied in.
enum PlaceKind { All, Pinned, Saved, Folder, Sources, Source }

// A row of the sidebar. Sources only gathers the programs under it: it opens and closes, and shows nothing itself.
class Place
{
    public readonly PlaceKind Kind;
    public string Path;      // of a folder
    public long Id;          // of a folder, or of a program among the apps
    public Origin Origin;    // of a program
    public int Count, Level;
    public bool Opens, Open;   // it has rows under it, and shows them

    public Place(PlaceKind kind)
    {
        Kind = kind;
    }

    // The same place, also after the sidebar was filled again.
    public bool Is(Place other)
    {
        return other != null && other.Kind == Kind && Id == other.Id && (Kind != PlaceKind.Folder || Texts.SameName(Path, other.Path));
    }

    public string Label
    {
        get
        {
            switch (Kind)
            {
                case PlaceKind.All: return Strings.AllPlace;
                case PlaceKind.Pinned: return Strings.PinnedPlace;
                case PlaceKind.Saved: return Strings.SavedTitle;
                case PlaceKind.Folder: return Texts.Leaf(Path);
                case PlaceKind.Sources: return Strings.SourcesPlace;
                default: return Origin.Name;
            }
        }
    }

    // As the title over the cards says it: a folder by its whole path.
    public string Title
    {
        get { return Kind == PlaceKind.Folder ? Texts.Display(Path) : Label; }
    }

    public Sign Sign
    {
        get
        {
            switch (Kind)
            {
                case PlaceKind.All: return Sign.History;
                case PlaceKind.Pinned: return Sign.Up;
                case PlaceKind.Saved: return Sign.Star;
                case PlaceKind.Folder: return Sign.Folder;
                default: return Sign.Apps;
            }
        }
    }

    // Whether the place lists copied entries, saved ones, or both.
    public bool HasCopied
    {
        get { return Kind == PlaceKind.All || Kind == PlaceKind.Pinned || Kind == PlaceKind.Source; }
    }

    public bool HasSaved
    {
        get { return Kind == PlaceKind.All || Kind == PlaceKind.Saved || Kind == PlaceKind.Folder; }
    }
}

// The sidebar, as in the apps of Windows 11: the places, a folder under the one it is in, the programs under Sources;
// a click shows a place, the arrow at the right end of a row opens or closes what is under it. A saved text, or a
// folder, is dragged onto a folder to go in it, onto Saved to leave the folders. Under it, a new folder, the
// options, the guide.
partial class MainForm
{
    // The commands at the bottom of the sidebar: a new folder, the pause of the capture, the options, the guide.
    static readonly Sign[] FootSigns = { Sign.NewFolder, Sign.Pause, Sign.Options, Sign.Help };
    const int PauseRow = 1;

    void WireSidebar()
    {
        foreach (RowList list in new RowList[] { navList, footList })
        {
            Look.Field(list);
            list.BackColor = Look.Back;   // on the window, beside the pane
            list.IntegralHeight = false;
            list.BorderStyle = BorderStyle.None;
            list.DrawMode = DrawMode.OwnerDrawFixed;
            list.ItemHeight = Font.Height + NavPadding;
        }
        navList.DrawItem += DrawPlace;
        navList.SelectedIndexChanged += delegate { if (!fillingNav) Go(navList.SelectedItem as Place); };
        navList.MouseMove += delegate(object sender, MouseEventArgs e)
        {
            int row = navList.RowAt(e.Location);
            navList.Cursor = row >= 0 ? Cursors.Hand : Cursors.Default;
            navList.Glow.Set(row);
        };
        navList.MouseLeave += delegate { navList.Glow.Set(-1); };
        // The arrow at the right end opens and closes; anywhere else the row is shown (SelectedIndexChanged), and
        // Sources, which has nothing to show, opens and closes there.
        navList.MouseClick += delegate(object sender, MouseEventArgs e)
        {
            int row = navList.RowAt(e.Location);
            if (e.Button != MouseButtons.Left || row < 0) return;
            Place at = (Place)navList.Items[row];
            if (at.Opens && at.Kind != PlaceKind.Sources && e.X >= ChevronArea(navList.GetItemRectangle(row)).Left) Toggle(at);
        };
        navList.MouseDown += delegate(object sender, MouseEventArgs e)
        {
            int row = navList.RowAt(e.Location);
            Place at = row < 0 ? null : (Place)navList.Items[row];
            if (e.Button == MouseButtons.Right && at != null && at.Kind != PlaceKind.Sources) navList.SelectedIndex = row;
            grabbed = e.Button == MouseButtons.Left && at != null && at.Kind == PlaceKind.Folder ? FolderEntry(at.Path) : null;
            grabbedAt = e.Location;
        };
        Draggable(navList);
        navList.ContextMenuStrip = navMenu;
        WireNavMenu();

        // The pause says what it would do, and changes with it (ShowPause).
        footList.Items.AddRange(new object[] { Strings.NewFolder, Strings.Pause, Strings.Options, Strings.GuideItem });
        footList.Marked = false;
        footList.TabStop = false;
        footList.LightUnderPointer();
        footList.DrawItem += delegate(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            // A command, never left selected.
            Look.DrawTopic(new DrawItemEventArgs(e.Graphics, e.Font, e.Bounds, e.Index, DrawItemState.None), footList,
                (string)footList.Items[e.Index], e.Index == PauseRow ? (paused ? Sign.Resume : Sign.Pause) : FootSigns[e.Index],
                (int)(NavInset * Zoom));
        };
        footList.MouseClick += delegate(object sender, MouseEventArgs e)
        {
            int row = footList.RowAt(e.Location);
            footList.SelectedIndex = -1;
            if (e.Button != MouseButtons.Left) return;
            if (row == 0) EditGroup(null);
            else if (row == PauseRow) Pause(!paused);
            else if (row == 2) OpenOptions();
            else if (row == 3) ShowHelp();
        };
        Tip(Strings.PinnedTip + "\n" + Strings.SourcesTip, navList);
    }

    // The commands at the bottom of the sidebar, as tall as their rows, and the places above them: once the window
    // has its scale.
    void PlaceSidebar()
    {
        int height = footList.Items.Count * footList.ItemHeight, bottom = pane.Bottom;
        footList.SetBounds(navList.Left, bottom - height, navList.Width, height);
        navList.Height = footList.Top - (int)(Gap * Zoom) - navList.Top;
    }

    // Shows a place; Sources, which shows nothing, only opens or closes.
    void Go(Place chosen)
    {
        if (chosen == null) return;
        if (chosen.Kind == PlaceKind.Sources)
        {
            Toggle(chosen);
            return;
        }
        if (chosen.Is(place)) return;
        place = chosen;
        Filter();
    }

    // Opens or closes what is under a row: the folders of Saved, the programs of Sources, the folders inside one.
    void Toggle(Place at)
    {
        if (at.Kind == PlaceKind.Saved) savedOpen = !savedOpen;
        else if (at.Kind == PlaceKind.Sources) sourcesOpen = !sourcesOpen;
        else if (at.Kind == PlaceKind.Folder)
        {
            if (!Change(delegate { clips.OpenFolder(at.Id, !at.Open); })) return;
            Entry folder = FolderEntry(at.Path);
            if (folder != null) folder.Open = !at.Open;
        }
        FillNav();
    }

    // The sidebar again, with the counts of now: after every change of the entries. The place shown stays selected;
    // one that is gone (a folder deleted, a program none of whose entries are left) gives way to All.
    void FillNav()
    {
        List<Place> rows = new List<Place>();
        int saved = 0;
        held.Clear();
        foreach (Entry entry in entries)
        {
            if (entry.IsGroup) continue;
            saved++;
            for (string group = entry.Group; group != null; group = Texts.Parent(group))
            {
                int count;
                held.TryGetValue(group, out count);
                held[group] = count + 1;
            }
        }
        int copied = 0, pinned = 0;
        List<Source> sources = new List<Source>();
        Change(delegate
        {
            copied = clips.CopiedCount();
            Sieve atTop = new Sieve();
            atTop.Pinned = true;
            pinned = clips.CopiedCount(atTop);
            sources = clips.Sources();
        });

        Place all = new Place(PlaceKind.All);
        all.Count = copied + saved;
        Place kept = new Place(PlaceKind.Pinned);
        kept.Count = pinned;
        Place savedRow = new Place(PlaceKind.Saved);
        savedRow.Count = saved;
        rows.AddRange(new Place[] { all, kept, savedRow });
        // The folders, each under the one it is in; those inside a closed one are not there.
        List<Entry> folders = entries.FindAll(delegate(Entry e) { return e.IsGroup; });
        savedRow.Opens = folders.Count > 0;
        savedRow.Open = savedOpen;
        Dictionary<string, bool> open = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        foreach (Entry folder in folders)
        {
            open[folder.Name] = folder.Open;
            if (!savedOpen || !InView(Texts.Parent(folder.Name), open)) continue;
            Place row = new Place(PlaceKind.Folder);
            row.Path = folder.Name;
            row.Id = folder.FolderId;
            row.Level = Texts.Depth(folder.Name);
            row.Open = folder.Open;
            row.Opens = folders.Exists(delegate(Entry other) { return Texts.SameName(Texts.Parent(other.Name), folder.Name); });
            held.TryGetValue(folder.Name, out row.Count);
            rows.Add(row);
        }
        if (sources.Count > 0)
        {
            Place gathered = new Place(PlaceKind.Sources);
            gathered.Opens = true;
            gathered.Open = sourcesOpen;
            rows.Add(gathered);
            if (sourcesOpen)
                foreach (Source source in sources)
                {
                    Place row = new Place(PlaceKind.Source);
                    row.Id = source.App;
                    row.Origin = source.Origin;
                    row.Count = source.Count;
                    row.Level = 1;
                    rows.Add(row);
                }
        }

        // The place shown, if it is still there: a folder inside a closed one, or a program in a closed list, is
        // shown all the same, though its row is not.
        Place found = rows.Find(delegate(Place row) { return row.Is(place); });
        bool gone = found == null && (place.Kind == PlaceKind.Folder ? FolderEntry(place.Path) == null
            : place.Kind == PlaceKind.Source && !sources.Exists(delegate(Source s) { return s.App == place.Id; }));
        int top = navList.TopIndex;
        fillingNav = true;
        navList.BeginUpdate();
        navList.Items.Clear();
        foreach (Place row in rows) navList.Items.Add(row);
        navList.SelectedIndex = found == null ? -1 : rows.IndexOf(found);
        if (navList.Items.Count > 0) navList.TopIndex = Math.Min(top, navList.Items.Count - 1);
        navList.EndUpdate();
        navList.Glow.Reset();
        fillingNav = false;
        if (found != null) place = found;
        if (!gone) return;
        place = all;
        navList.SelectedIndex = 0;   // which fills the cards again
    }

    // The folder Entry of a path, or null.
    Entry FolderEntry(string path)
    {
        return entries.Find(delegate(Entry e) { return e.IsGroup && Texts.SameName(e.Name, path); });
    }

    // A row of the sidebar: its sign or the icon of its program, its name, how many entries it has and, if it has
    // rows under it, the arrow that opens and closes them. Sources stands as the title of the programs.
    void DrawPlace(object sender, DrawItemEventArgs e)
    {
        if (e.Index < 0) return;
        Place at = (Place)navList.Items[e.Index];
        bool selected = (e.State & DrawItemState.Selected) != 0 && at.Kind != PlaceKind.Sources;
        // Fully lit, the folder a dragged text would be dropped into.
        float glow = dropPlace != null && dropPlace.Is(at) ? 1 : navList.Glow.Of(e.Index);
        Look.DrawRow(e.Graphics, e.Bounds, navList.BackColor, selected, glow);
        int inset = (int)(NavInset * Zoom), side = e.Font.Height;
        int left = e.Bounds.Left + inset + (int)(SideIndent * Zoom) * at.Level, right = e.Bounds.Right - inset / 2;
        Rectangle box = new Rectangle(left, e.Bounds.Top + (e.Bounds.Height - side) / 2, side, side);
        if (at.Kind != PlaceKind.Source) Look.Draw(e.Graphics, at.Sign, box, null);
        else if (at.Origin.Icon != null)
        {
            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            e.Graphics.DrawImage(at.Origin.Icon, box);
        }
        if (at.Opens)
        {
            Rectangle chevron = ChevronArea(e.Bounds);
            int small = side * 3 / 4;
            Look.Draw(e.Graphics, at.Open ? Sign.Opened : Sign.Closed, new Rectangle(chevron.Left + (chevron.Width - small) / 2,
                e.Bounds.Top + (e.Bounds.Height - small) / 2, small, small), Look.Muted);
            right = chevron.Left;
        }
        if (at.Kind != PlaceKind.Sources && at.Count > 0)
        {
            string number = at.Count.ToString();
            int width = TextRenderer.MeasureText(e.Graphics, number, e.Font, Size.Empty, TextFormatFlags.NoPadding).Width;
            TextRenderer.DrawText(e.Graphics, number, e.Font, Rectangle.FromLTRB(right - width, e.Bounds.Top, right, e.Bounds.Bottom),
                Look.Muted, RightFlags);
            right -= width + (int)(TimeGap * Zoom);
        }
        TextRenderer.DrawText(e.Graphics, at.Label, at.Kind == PlaceKind.Sources ? Look.StrongFont : e.Font,
            Rectangle.FromLTRB(box.Right + inset, e.Bounds.Top, right, e.Bounds.Bottom), Look.Ink, TextFormatFlags.VerticalCenter | LineFlags);
    }

    // Where the arrow of a row that opens is: at its right end.
    Rectangle ChevronArea(Rectangle row)
    {
        int width = Font.Height + (int)(NavInset * Zoom);
        return Rectangle.FromLTRB(row.Right - width, row.Top, row.Right, row.Bottom);
    }

    // The menu of a folder in the sidebar, and of Saved: a new folder in it; of a folder also another name or place,
    // another folder to go in, or none.
    void WireNavMenu()
    {
        ToolStripItem newItem = navMenu.Items.Add(Strings.NewFolder, null, delegate { EditGroup(null); });
        ToolStripItem renameItem = navMenu.Items.Add(Strings.RenameDots, null, delegate { EditGroup(place.Path); });
        ToolStripMenuItem moveItem = new ToolStripMenuItem(Strings.MoveTo);
        navMenu.Items.Add(moveItem);
        ToolStripItem deleteItem = navMenu.Items.Add(Strings.Delete, null, delegate { DeleteFolder(FolderEntry(place.Path)); });
        Look.Mark(newItem, Sign.NewFolder);
        Look.Mark(renameItem, Sign.Edit);
        Look.Mark(moveItem, Sign.Move);
        Look.Mark(deleteItem, Sign.Delete);
        Look.Style((ToolStripDropDownMenu)moveItem.DropDown);
        Look.Style(navMenu);
        navMenu.Opening += delegate(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Place at = navList.SelectedItem as Place;
            bool folder = at != null && at.Kind == PlaceKind.Folder && at.Is(place);
            // Only on a folder, or on Saved.
            if (!folder && (at == null || at.Kind != PlaceKind.Saved))
            {
                e.Cancel = true;
                return;
            }
            renameItem.Visible = deleteItem.Visible = folder;
            Entry entry = folder ? FolderEntry(at.Path) : null;
            ListPlaces(moveItem, entry);
        };
    }

    // The folders a saved text or a folder can go into, in the menu entry that moves it: none at all, or each that
    // is not where it is already, nor the folder itself or one inside it.
    void ListPlaces(ToolStripMenuItem menu, Entry entry)
    {
        List<string> groups = entry == null ? new List<string>()
            : GroupNames().FindAll(delegate(string group) { return !entry.IsGroup || !Texts.Within(group, entry.Name); });
        // Asked back, 'Visible' would answer false: the menu is not on screen yet.
        bool movable = entry != null && entry.Saved && (groups.Count > 0 || PlaceOf(entry) != null);
        menu.Visible = movable;
        menu.DropDownItems.Clear();
        if (!movable) return;
        menu.DropDownItems.Add(Strings.OutOfFolders, null, delegate { MoveTo(entry, null); }).Enabled = PlaceOf(entry) != null;
        foreach (string each in groups)
        {
            string group = each;   // a variable of its own for the delegate
            menu.DropDownItems.Add(Texts.Display(group), null, delegate { MoveTo(entry, group); }).Enabled = CanGo(entry, group);
        }
    }

    // A saved text dragged from the cards, or a folder from the sidebar, goes into the folder it is dropped on; onto
    // Saved, out of the folders.
    void Draggable(RowList list)
    {
        list.MouseUp += delegate { grabbed = null; };
        list.MouseMove += delegate(object sender, MouseEventArgs e)
        {
            // Only once the pointer has really moved: a click must stay a click.
            Rectangle still = new Rectangle(grabbedAt, Size.Empty);
            still.Inflate(SystemInformation.DragSize);
            if (grabbed == null || e.Button != MouseButtons.Left || still.Contains(e.Location)) return;
            Entry entry = grabbed;
            grabbed = null;
            HidePeek();
            list.DoDragDrop(entry, DragDropEffects.Move);
            Aim(null);
        };
        if (list != navList) return;
        navList.AllowDrop = true;
        navList.DragOver += delegate(object sender, DragEventArgs e)
        {
            Entry entry = e.Data.GetData(typeof(Entry)) as Entry;
            Place target = DropAt(navList.PointToClient(new Point(e.X, e.Y)));
            bool moves = entry != null && target != null && CanGo(entry, target.Path);
            e.Effect = moves ? DragDropEffects.Move : DragDropEffects.None;
            Aim(moves ? target : null);
        };
        navList.DragLeave += delegate { Aim(null); };
        navList.DragDrop += delegate(object sender, DragEventArgs e)
        {
            Entry entry = e.Data.GetData(typeof(Entry)) as Entry;
            Place target = DropAt(navList.PointToClient(new Point(e.X, e.Y)));
            Aim(null);
            if (entry != null && target != null && CanGo(entry, target.Path)) MoveTo(entry, target.Path);
        };
    }

    // The row of the sidebar something dragged would go to: a folder, or Saved for none.
    Place DropAt(Point where)
    {
        int row = navList.RowAt(where);
        Place at = row < 0 ? null : (Place)navList.Items[row];
        return at != null && (at.Kind == PlaceKind.Folder || at.Kind == PlaceKind.Saved) ? at : null;
    }

    // Lights up the row a dragged text is over, as the pointer does.
    void Aim(Place target)
    {
        if (target == dropPlace || target != null && target.Is(dropPlace)) return;
        dropPlace = target;
        navList.Invalidate();
    }
}
