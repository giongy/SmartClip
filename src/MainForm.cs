using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

class MainForm : Form
{
    // The global hotkeys: registration and texts all start from here. The first types the clipboard at once,
    // the second brings up this window with the list. These are the keys they start with: the user can give
    // them others (see AwaitKeys), which are remembered among the settings.
    const Keys DefaultTypeKeys = Keys.Control | Keys.Alt | Keys.V, DefaultPickKeys = Keys.Control | Keys.D;
    const Keys HelpKey = Keys.F1;
    static Keys typeKeys = DefaultTypeKeys, pickKeys = DefaultPickKeys;
    public static string HotkeyName { get { return NameOf(typeKeys); } }
    public static string PickName { get { return NameOf(pickKeys); } }
    const int HotkeyId = 1, PickId = 2;

    public const int MinDelay = 1, MaxDelay = 30;             // limits, in seconds, of the wait before typing
    const int DefaultDelay = 1;                        // wait proposed until the user picks another one
    public const int ArmedSeconds = 15;                       // time allowed to bring the target window to the foreground
    const int PollMs = 100;                            // how often the foreground window is checked again
    const int KeyDelayMs = 15, SlowKeyDelayMs = 60;    // pause after each key press or release
    public const int UnusualLength = 100;                     // a longer text is unlikely to be a password
    const int MaxTitleLength = 45;                     // beyond this, a window title or a list entry is truncated in the status
    const int Edge = 12, ContentWidth = 620;           // margin and usable width of the window, in pixels at 96 dpi
    const int ListExtra = 115;                  // height given to the lists beyond what the options need
    const int ColumnGap = 20, ColumnWidth = (ContentWidth - ColumnGap) / 2;   // the two columns of the options
    const int TitleRow = 30, SectionGap = 10;          // the title of a category of options, and the air above it
    const int RuleGap = 8;                             // between the line under such a title and the first option
    const int SavedWidth = 250, TitleHeight = 20;      // the column of the saved texts, and the title above each column
    const int MinColumn = 185;                         // the least a column can be dragged to
    const int GripHeight = 28;                         // of the mark between the columns, where they are dragged
    const int RowPadding = 16;                         // added to the text height to get a row of the list: roomy
    const int PictureLines = 5;                        // lines of text a picture is as tall as, to make room for its preview
    const int PreviewMargin = 4;                       // around the preview, inside its row
    const int RowHeight = 24;                          // distance between one option checkbox and the next
    const int NumberWidth = 56, NumberHeight = 26;     // the box of an option that is a number
    const int NumberRow = 37;                          // distance between such an option and the next one
    const int OptionButtonWidth = 130;                 // a button among the options
    public const int MaxShown = 200;                          // copied entries listed at once
    const int SearchHeight = 30, GlyphWidth = 20;      // the search box, and the magnifying glass on its left
    const int ListPadding = 4;                         // between the rounded edge of a column and its list
    const int CountWidth = 120;                        // room for the number of entries, right of a column title
    const int GroupIndent = 14;                        // of a saved text under its folder
    const int RowInset = 12, TimeGap = 8;             // of the text in a row, and around the time on its right
    const int Gap = 6;                                 // between neighbouring controls
    const int ActionHeight = 26, ActionWidth = 92;     // the buttons in the bottom row
    const int TypeWidth = 160;                        // the button that types the clipboard, in the same row
    const int StatusHeight = 34;                       // two lines of text: the most the status line takes
    const int FadeMs = 6000;                           // how long a passing message stays in the status line
    const int RestMs = 600;                            // how long the pointer rests on a row before it is shown whole
    const int AskedMs = 15000;                         // how long a saved text stays in view once asked for
    public const Keys NewKey = Keys.N, SaveKey = Keys.S, DeleteKey = Keys.E, PinKey = Keys.P;   // in the list, all with Ctrl
    const Keys NewKeys = Keys.Control | NewKey, GroupKeys = Keys.Control | Keys.Shift | NewKey, SaveKeys = Keys.Control | SaveKey,
        DeleteKeys = Keys.Control | DeleteKey, PinKeys = Keys.Control | PinKey;
    // What the list itself answers to: a global hotkey on the same keys would take them away from it.
    static readonly Keys[] ListKeys = { NewKeys, GroupKeys, SaveKeys, DeleteKeys, PinKeys };
    public const string ButtonText = "Digita gli appunti";
    const string RecentTitle = "Copiati di recente";
    const TextFormatFlags LineFlags = TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis;   // one line of a row
    public const string SettingsKey = @"Software\" + Program.AppName;
    const string OldSettingsKey = @"Software\PasswordPaste";   // the name of the program up to version 1.2

    // Desktop, taskbar, Start menu and Alt+Tab switcher: never the intended target.
    static readonly string[] ShellClasses =
    {
        "Progman", "WorkerW", "Shell_TrayWnd", "Shell_SecondaryTrayWnd", "Windows.UI.Core.CoreWindow",
        "XamlExplorerHostIslandWindow", "MultitaskingViewFrame", "TaskSwitcherWnd"
    };
    static readonly uint OwnProcessId = (uint)Process.GetCurrentProcess().Id;
    static readonly Point OffScreen = new Point(int.MinValue, int.MinValue);

    readonly RoundButton typeButton = new RoundButton();
    readonly RoundButton viewButton = new RoundButton(), helpButton = new RoundButton();
    HelpForm help;      // the guide, while it is open
    readonly Label recentCount = new Label(), savedCount = new Label();   // how many entries each column lists
    readonly Label recentEmpty = new Label(), savedEmpty = new Label();   // what a column says while it has none
    readonly Label status = new Label();
    readonly TextBox search = new TextBox();
    readonly ListBox recentList = new ListBox(), savedList = new ListBox();   // what was copied, and what was saved
    ListBox active;     // the one of the two that holds the selected entry
    Panel recentCard, savedCard;                  // the rounded shapes the two lists are in
    Label recentTitle, savedTitle;
    readonly ContextMenuStrip listMenu = new ContextMenuStrip();
    readonly Peek peek = new Peek();              // shows whole the entry of a row
    readonly Timer rest = new Timer();            // runs while the pointer rests on a row, and while a text asked for is in view
    bool peekAsked;                               // what is in view was asked for from the menu: moving the pointer leaves it there
    readonly Timer fade = new Timer();            // runs while the status line holds a passing message
    Entry grabbed;                                // the saved text the button went down on: it may be dragged to a folder
    Point grabbedAt;
    string dropGroup;                             // the folder a dragged text would go to, while it is over one
    bool paused;                                  // what is copied is not remembered, until the user says otherwise
    readonly List<ToolStripItem> pauseItems = new List<ToolStripItem>();   // the menu entries that say so
    CheckBox capturing;                           // the hotkey whose new keys are awaited, or null
    readonly Panel divider = new Panel();         // the room between the columns: dragged, it moves their border
    int savedColumn = SavedWidth;                 // the width the user gave to the saved texts, in pixels at 96 dpi
    int dragFrom, dragWidth;                      // where the pointer and the border were when a drag started
    bool shown;                                   // the window has its final scale: the columns can be placed
    readonly RoundButton newButton = new RoundButton(), saveButton = new RoundButton(), deleteButton = new RoundButton();
    // The two views that take turns in the lower part of the window. The options add themselves as they are placed.
    readonly List<Control> optionControls = new List<Control>();
    readonly Control[] listControls;
    readonly TextBox delayBox = new TextBox(), keepBox = new TextBox(), cleanBox = new TextBox();
    readonly RoundButton cleanButton = new RoundButton(), exportButton = new RoundButton(), importButton = new RoundButton();
    readonly RoundButton typeKeysButton = new RoundButton(), pickKeysButton = new RoundButton();   // show the keys of a hotkey, and take new ones
    readonly CheckBox usBox = new Toggle();
    readonly CheckBox enterBox = new Toggle();
    readonly CheckBox clearBox = new Toggle();
    readonly CheckBox slowBox = new Toggle();
    readonly CheckBox altCodeBox = new Toggle();
    readonly CheckBox hotkeyBox = new Toggle();
    readonly CheckBox pickBox = new Toggle();
    readonly CheckBox historyBox = new Toggle();
    readonly CheckBox trayBox = new Toggle();
    readonly CheckBox startBox = new Toggle();   // not among the settings: it shows what the Run key of Windows says
    bool startHidden;   // the first request to show the window is not obeyed
    readonly NotifyIcon tray = new NotifyIcon();
    readonly ToolTip tips = new ToolTip();
    readonly Icon readyIcon, emptyIcon, readyTrayIcon, emptyTrayIcon;
    bool busy, closePending;
    bool exiting;         // the user chose Esci (Exit) in the icon menu: the close is real, not a "hide"
    bool trayHintShown;   // the balloon explaining where the window went is shown only once
    volatile bool cancel;
    bool optionsShown;
    List<Entry> entries = new List<Entry>();
    bool enterDown;     // Enter was pressed in the list, so its release belongs to it
    IntPtr previous;    // the window the list hotkey was pressed in: the foreground goes back there
    bool wasHidden;     // the list hotkey found the window hidden: after the choice it hides again
    uint ownCopy;       // clipboard sequence number of what was put there from the list: not to be remembered again
    readonly List<CheckBox> refused = new List<CheckBox>();   // hotkey checkboxes unticked by Windows, not by the user

    public MainForm() : this(false)
    {
    }

    // 'hidden': started by Windows at sign-in, so the window does not show until asked.
    public MainForm(bool hidden)
    {
        SuspendLayout();
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        Text = Program.AppName + " " + Program.Version;
        Font = Look.TextFont;
        // The window can be made larger, never smaller than it starts (see OnLoad); its size is not remembered,
        // so that it always opens as it was designed.
        StartPosition = FormStartPosition.CenterScreen;
        TopMost = true;
        KeyPreview = true;
        BackColor = Look.Back;
        ForeColor = Look.Ink;
        using (Stream icon = typeof(MainForm).Assembly.GetManifestResourceStream(Program.AppName + ".ico"))
        {
            if (icon != null) Icon = new Icon(icon);
        }

        typeButton.Text = ButtonText;
        typeButton.Click += delegate { if (busy) cancel = true; else Run(Delay); };
        viewButton.Click += delegate { ShowView(!optionsShown); };
        helpButton.Click += delegate { ShowHelp(); };
        Look.Style(typeButton, true);
        foreach (Button button in new Button[] { viewButton, helpButton, newButton, saveButton, deleteButton }) Look.Style(button, false);

        status.UseMnemonic = false;
        status.ForeColor = Look.Muted;
        // The status line takes the room of what it says. A message written with Say leaves after a while;
        // anything else stays until the next one.
        status.TextChanged += delegate
        {
            fade.Stop();
            FitStatus();
        };
        fade.Interval = FadeMs;
        fade.Tick += delegate
        {
            fade.Stop();
            if (!busy) ShowIdle();
        };

        // The options are stacked by category, in two columns: on the left what concerns typing, on the right
        // the rest. Adding one takes a single line here, the window height adapts.
        int left = Edge, top = Edge;
        PlaceTitle("Digitazione", left, ref top);
        // Confirming a number brings what was written back within its limits.
        Label delayLabel = PlaceNumber(delayBox, "Attesa prima di digitare (secondi):", MaxDelay,
            delegate { Delay = Delay; }, left, ref top);
        Delay = DefaultDelay;
        PlaceOption(usBox, "Il server usa la tastiera inglese (USA)", left, ref top);
        PlaceOption(enterBox, "Premi Invio alla fine", left, ref top);
        PlaceOption(clearBox, "Svuota gli appunti dopo aver digitato", left, ref top);
        PlaceOption(slowBox, "Digitazione lenta", left, ref top);
        PlaceOption(altCodeBox, "Alt+codice per i simboli che la tastiera non ha", left, ref top);
        // On the left too, where there is room: under the history the window would have to grow.
        PlaceTitle("Programma", left, ref top);
        PlaceOption(trayBox, "Resta attivo nell'area di notifica (tray)", left, ref top);
        trayBox.Checked = true;
        PlaceOption(startBox, "Avvia insieme a Windows", left, ref top);
        // Side by side, each with its two choices: the saved texts alone, or what was copied too.
        ContextMenuStrip exportMenu = Choice(Export), importMenu = Choice(Import);
        top += Gap;
        PlaceButton(exportButton, "Esporta…", delegate { exportMenu.Show(exportButton, 0, exportButton.Height); }, left, ref top);
        top -= NumberRow;
        PlaceButton(importButton, "Importa…", delegate { importMenu.Show(importButton, 0, importButton.Height); },
            left + OptionButtonWidth + Gap, ref top);
        int leftBottom = top;

        left += ColumnWidth + ColumnGap;
        top = Edge;
        PlaceTitle("Scorciatoie", left, ref top);
        PlaceHotkey(hotkeyBox, typeKeysButton, ButtonText, left, ref top);
        hotkeyBox.Checked = true;
        PlaceHotkey(pickBox, pickKeysButton, "Apri l'elenco", left, ref top);
        pickBox.Checked = true;
        PlaceTitle("Cronologia", left, ref top);
        PlaceOption(historyBox, "Ricorda i testi e le immagini copiati", left, ref top);
        historyBox.Checked = true;
        Label keepLabel = PlaceNumber(keepBox, "Giorni su disco (0: solo in memoria):", Texts.MaxKeepDays,
            delegate { Retention = Retention; ApplyHistory(); Reload(); }, left, ref top);
        Retention = Texts.DefaultKeepDays;
        LinkLabel folderLink = PlaceLink("Apri la cartella della cronologia", OpenFolder, left, ref top);
        // Not a setting: what to spare when the button below is pressed.
        Label cleanLabel = PlaceNumber(cleanBox, "Pulendo, tieni gli ultimi giorni (0: niente):", Texts.MaxKeepDays,
            delegate { Spared = Spared; }, left, ref top);
        Spared = 0;
        PlaceButton(cleanButton, "Pulisci ora…", CleanHistory, left, ref top);
        cleanButton.Sign = Sign.Delete;
        top = Math.Max(top, leftBottom);

        // Under the options, or the lists that take their place: the status line, then one row of small buttons.
        // On the left those that act on the selected entry, on the right typing and the switch between the views.
        int areaBottom = top + 4 + ListExtra, statusTop = areaBottom + Gap, actionTop = statusTop + StatusHeight + Gap;
        ClientSize = new Size(Edge + ContentWidth + Edge, actionTop + ActionHeight + Edge);
        status.SetBounds(Edge, statusTop, ContentWidth, StatusHeight);
        newButton.Text = "Nuovo…";
        newButton.Sign = Sign.Plus;
        deleteButton.Sign = Sign.Delete;
        typeButton.Sign = Sign.Keys;
        newButton.SetBounds(Edge, actionTop, ActionWidth, ActionHeight);
        saveButton.SetBounds(newButton.Right + Gap, actionTop, ActionWidth, ActionHeight);
        deleteButton.Text = "Elimina";
        deleteButton.SetBounds(saveButton.Right + Gap, actionTop, ActionWidth, ActionHeight);
        helpButton.Sign = Sign.Help;
        helpButton.SetBounds(Edge + ContentWidth - ActionHeight, actionTop, ActionHeight, ActionHeight);   // a square
        viewButton.SetBounds(helpButton.Left - Gap - ActionWidth, actionTop, ActionWidth, ActionHeight);
        typeButton.SetBounds(viewButton.Left - Gap - TypeWidth, actionTop, TypeWidth, ActionHeight);

        // The lists take the same area as the options, with the search box on top.
        search.SetBounds(Edge, Edge, ContentWidth, SearchHeight);
        Panel searchFrame = Look.Frame(search);
        // A magnifying glass on the left of the box, which makes room for it.
        search.Left += GlyphWidth;
        search.Width -= GlyphWidth;
        searchFrame.Paint += DrawGlyph;

        // Two columns: on the left what was copied, which comes and goes, on the right what was saved. Above
        // each its title and, on the right, how many entries it lists.
        int titleTop = searchFrame.Bottom + Gap + 2, listTop = titleTop + TitleHeight, listHeight = areaBottom - listTop;
        int savedLeft = Edge + ContentWidth - SavedWidth;
        recentTitle = NewLabel(RecentTitle, titleTop);
        savedTitle = NewLabel("Salvati", titleTop);
        savedTitle.Left = savedLeft;
        recentTitle.Font = savedTitle.Font = new Font(Font, FontStyle.Bold);
        recentList.SetBounds(Edge, listTop, savedLeft - Edge - Edge, listHeight);
        savedList.SetBounds(savedLeft, listTop, SavedWidth, listHeight);
        recentCard = Look.Wrap(recentList, ListPadding);
        savedCard = Look.Wrap(savedList, ListPadding);
        // The room between the columns can be dragged, to give the saved texts more of the window or less; a
        // double click goes back to the width they start with.
        divider.SetBounds(recentCard.Right, listTop, Edge, listHeight);
        divider.Cursor = Cursors.VSplit;
        divider.Paint += DrawGrip;
        divider.MouseDown += delegate(object sender, MouseEventArgs e)
        {
            dragFrom = Cursor.Position.X;
            dragWidth = savedCard.Width;
        };
        divider.MouseMove += delegate(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) PlaceColumns(dragWidth + dragFrom - Cursor.Position.X);
        };
        divider.DoubleClick += delegate { PlaceColumns((int)(SavedWidth * Zoom)); };
        recentCount.SetBounds(recentCard.Right - CountWidth, titleTop, CountWidth, TitleHeight);
        savedCount.SetBounds(savedCard.Right - CountWidth, titleTop, CountWidth, TitleHeight);
        foreach (Label count in new Label[] { recentCount, savedCount })
        {
            count.TextAlign = ContentAlignment.TopRight;
            count.ForeColor = Look.Muted;
        }
        // What an empty column says, in the place of its list.
        foreach (Label empty in new Label[] { recentEmpty, savedEmpty })
        {
            empty.Bounds = recentList.Bounds;
            empty.TextAlign = ContentAlignment.MiddleCenter;
            empty.ForeColor = Look.Muted;
        }
        recentCard.Controls.Add(recentEmpty);
        savedEmpty.Width = savedList.Width;
        savedCard.Controls.Add(savedEmpty);
        recentEmpty.BringToFront();
        savedEmpty.BringToFront();

        // When the window grows, the column of the copied entries takes the room; the saved ones stay on the
        // right, status and buttons at the bottom.
        const AnchorStyles toTop = AnchorStyles.Top, toBottom = AnchorStyles.Bottom, toLeft = AnchorStyles.Left, toRight = AnchorStyles.Right;
        searchFrame.Anchor = toTop | toLeft | toRight;
        recentCard.Anchor = toTop | toBottom | toLeft | toRight;
        savedCard.Anchor = toTop | toBottom | toRight;
        savedTitle.Anchor = recentCount.Anchor = savedCount.Anchor = toTop | toRight;
        divider.Anchor = toTop | toBottom | toRight;
        recentEmpty.Anchor = savedEmpty.Anchor = toTop | toBottom | toLeft | toRight;
        status.Anchor = toBottom | toLeft | toRight;
        newButton.Anchor = saveButton.Anchor = deleteButton.Anchor = toBottom | toLeft;
        viewButton.Anchor = typeButton.Anchor = helpButton.Anchor = toBottom | toRight;

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
            List<string> groups = GroupNames();
            // Asked back, 'Visible' would answer false: the menu is not on screen yet.
            bool movable = entry != null && entry.Name != null && !entry.IsGroup && groups.Count > 0;
            moveItem.Visible = movable;
            moveItem.DropDownItems.Clear();
            if (!movable) return;
            // The place where the text already is cannot be chosen.
            moveItem.DropDownItems.Add("Nessuna cartella", null, delegate { MoveTo(entry, null); }).Enabled = entry.Group != null;
            foreach (string each in groups)
            {
                string group = each;   // a variable of its own for the delegate
                moveItem.DropDownItems.Add(group, null, delegate { MoveTo(entry, group); }).Enabled = !Texts.SameName(group, entry.Group);
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
            list.SelectedIndexChanged += delegate
            {
                if (list.SelectedIndex >= 0)
                {
                    active = list;
                    Other(list).ClearSelected();
                }
                ShowActions();
            };
            // The left button chooses, the right one only selects, so that its menu acts on the entry under the pointer.
            list.MouseDown += delegate(object sender, MouseEventArgs e)
            {
                HidePeek();
                int index = RowAt(list, e.Location);
                if (e.Button == MouseButtons.Right && index >= 0) list.SelectedIndex = index;
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

        // A saved text is dragged to a folder: onto the folder itself or onto a text it holds. Dropped on a
        // text outside the folders, or below them all, it leaves the one it was in.
        savedList.AllowDrop = true;
        savedList.MouseDown += delegate(object sender, MouseEventArgs e)
        {
            int index = RowAt(savedList, e.Location);
            grabbed = e.Button == MouseButtons.Left && index >= 0 && !((Entry)savedList.Items[index]).IsGroup
                ? (Entry)savedList.Items[index] : null;
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
            bool moves = entry != null && !Texts.SameName(group, entry.Group);
            e.Effect = moves ? DragDropEffects.Move : DragDropEffects.None;
            Aim(moves ? group : null);
        };
        savedList.DragLeave += delegate { Aim(null); };
        savedList.DragDrop += delegate(object sender, DragEventArgs e)
        {
            Entry entry = e.Data.GetData(typeof(Entry)) as Entry;
            string group = GroupAt(savedList.PointToClient(new Point(e.X, e.Y)));
            Aim(null);
            if (entry != null && !Texts.SameName(group, entry.Group)) MoveTo(entry, group);
        };
        // A new search starts again from its best result.
        search.TextChanged += delegate { active = null; Filter(); };

        tips.AutoPopDelay = 30000;
        Tip("Per quanti secondi la finestra di destinazione deve restare in primo piano\n"
          + "prima che parta la digitazione: è il tempo che hai per cliccare nel campo\n"
          + "giusto, o per annullare.", delayLabel, delayBox);
        Tip("Lasciala spenta per i normali desktop remoti: vale la tastiera di questo PC.\n"
          + "Attivala se i simboli arrivano sbagliati perché il server usa la tastiera\n"
          + "americana (capita con le console VMware, iLO, iDRAC e con alcune schermate\n"
          + "di accesso).", usBox);
        Tip("Preme Invio dopo l'ultimo carattere, per confermare subito l'accesso.", enterBox);
        Tip("Cancella la password dagli appunti quando la digitazione è riuscita.\n"
          + "La cronologia degli appunti di Windows (Win+V), se attiva, non viene toccata.", clearBox);
        Tip("Attivala se sul server arrivano caratteri mancanti o in disordine.", slowBox);
        Tip("I simboli che questa tastiera non ha (ad esempio ~ e ` su quella italiana)\n"
          + "vengono normalmente inviati come caratteri Unicode, che non tutti i programmi\n"
          + "di desktop remoto accettano. Con questa opzione vengono invece composti con\n"
          + "Alt+codice sul tastierino numerico: funziona solo se il server è Windows.", altCodeBox);
        Tip("L'elenco propone i testi e le immagini copiati mentre il programma è aperto.\n"
          + "Ciò che i gestori di password segnano come riservato non viene ricordato.\n"
          + "Disattivala se copi password da programmi che non lo fanno: nell'elenco si\n"
          + "leggerebbero in chiaro. Spenta, cancella anche la cronologia su disco.", historyBox);
        Tip("Per quanti giorni ciò che copi resta nella cronologia, anche dopo la chiusura.\n"
          + "Sta in " + Texts.Folder + ",\n"
          + "un file per giorno, scritto in chiaro: i giorni scaduti si eliminano da soli.\n"
          + "Con 0 niente va su disco: restano in memoria le ultime " + Texts.MaxRecent + " cose copiate, di cui al\n"
          + "massimo " + Texts.MaxPictures + " immagini, e spariscono alla chiusura.\n"
          + "L'elenco mostra le " + MaxShown + " più recenti: le altre si trovano con la ricerca.", keepLabel, keepBox);
        Tip("Apre in Esplora file la cartella con i file della cronologia, uno per giorno:\n"
          + Texts.Folder, folderLink);
        Tip("Elimina subito dalla cronologia, in memoria e su disco, quello che hai copiato prima\n"
          + "degli ultimi giorni scritti qui sopra: con 1 resta solo oggi, con 0 non resta niente.\n"
          + "Prima chiede conferma. Le voci con la stellina e i testi salvati non vengono toccati,\n"
          + "e nemmeno gli appunti. I giorni su disco restano quelli impostati.", cleanLabel, cleanBox, cleanButton);
        Tip("Attiva: chiudendo o riducendo a icona la finestra il programma resta in funzione\n"
          + "come icona vicino all'orologio, e la scorciatoia continua a funzionare.\n"
          + "Clic sull'icona per riaprire la finestra, tasto destro e poi Esci per chiudere.\n"
          + "Disattiva: finestra normale, che chiudendola termina il programma.", trayBox);
        Tip("Avvia " + Program.AppName + " quando accedi a Windows, già nascosto nell'area di notifica\n"
          + "(ridotto a icona, se l'opzione qui sopra è spenta).\n"
          + "Vale per questo eseguibile: se lo sposti, al primo avvio da lì la voce si aggiorna da sola.\n"
          + "È la stessa voce di Gestione attività > App di avvio: spenta lì, risulta spenta anche qui.", startBox);
        Tip("Scrive in un file di testo, che si apre con il Blocco note, i testi salvati con le loro\n"
          + "cartelle; se lo chiedi, anche le cose copiate. Le immagini non ci vanno.\n"
          + "Nel file i testi sono in chiaro, password comprese: tienilo in un posto sicuro.", exportButton);
        Tip("Legge un file scritto da «Esporta», anche su un altro PC, e aggiunge ciò che contiene\n"
          + "a quello che c'è già. Se un testo salvato ha il nome di uno esistente, chiede quale tenere.\n"
          + "Le cose copiate che l'elenco ha già non vengono ripetute.", importButton);
        Tip("Scrive un testo nuovo da tenere, con il suo nome (Ctrl+" + NewKey + ").", newButton);
        Tip("Su una voce copiata: la salva con un nome, e puoi ritoccarne il contenuto.\n"
          + "Su una voce salvata: ne cambia il nome o il contenuto (Ctrl+" + SaveKey + ").\n"
          + "Su una cartella: le cambia il nome.\n"
          + "Le immagini restano solo in memoria e non si salvano.", saveButton);
        Tip("Apre la guida: tutto quello che il programma sa fare (" + HelpKey + ").", helpButton);
        Tip("Elimina il testo salvato, o toglie dall'elenco la voce copiata (Ctrl+" + DeleteKey + ").\n"
          + "Di una cartella elimina solo la cartella: i suoi testi restano tra i salvati.", deleteButton);

        Tip("Trascina per dare più o meno spazio ai testi salvati.\n"
          + "Doppio clic: la larghezza di partenza.", divider);
        listControls = new Control[] { searchFrame, recentTitle, savedTitle, recentCount, savedCount, recentCard, savedCard, divider,
            newButton, saveButton, deleteButton };
        Controls.AddRange(new Control[] { typeButton, viewButton, helpButton, status });
        Controls.AddRange(listControls);
        Controls.AddRange(optionControls.ToArray());
        // No AcceptButton: Enter among the options would start typing the clipboard.

        LoadSettings();
        NameHotkeys();
        ApplyHistory();
        Change(Texts.Read);
        historyBox.CheckedChanged += delegate
        {
            ApplyHistory();
            Reload();
        };
        Reload();
        ShowView(false);
        ActiveControl = search;

        ContextMenuStrip trayMenu = new ContextMenuStrip();
        trayMenu.Items.Add("Apri", null, delegate { RestoreWindow(); });
        pauseItems.Add(trayMenu.Items.Add("", null, delegate { Pause(!paused); }));
        trayMenu.Items.Add("Esci", null, delegate { exiting = true; Close(); });
        ShowPause();
        tray.ContextMenuStrip = trayMenu;
        Look.Style(trayMenu);
        tray.Text = Program.AppName;
        // The coloured icon says the clipboard holds text ready to type, the grey one that it holds none.
        readyIcon = Icon;
        emptyIcon = Gray(Icon, SystemInformation.IconSize);
        readyTrayIcon = new Icon(Icon, SystemInformation.SmallIconSize);
        emptyTrayIcon = Gray(Icon, SystemInformation.SmallIconSize);
        tray.Icon = readyTrayIcon;
        tray.MouseClick += delegate(object sender, MouseEventArgs e) { if (e.Button == MouseButtons.Left) RestoreWindow(); };
        tray.Visible = trayBox.Checked;
        trayBox.CheckedChanged += delegate { tray.Visible = trayBox.Checked; };

        // Click rather than CheckedChanged: it must fire only when the user changes the checkbox.
        hotkeyBox.Click += delegate { UpdateHotkeys(); };
        pickBox.Click += delegate { UpdateHotkeys(); };
        ShowIdle();
        // Starting with Windows is not a setting of ours: the checkbox shows and changes what Windows has.
        Change(delegate { startBox.Checked = Startup.Enabled; });
        startBox.Click += delegate
        {
            bool wanted = startBox.Checked;
            bool done = Change(delegate { Startup.Enabled = wanted; });
            Change(delegate { startBox.Checked = Startup.Enabled; });
            if (done) Say(wanted ? "All'accesso a Windows " + Program.AppName + " partirà da solo, senza mostrare la finestra."
                : Program.AppName + " non parte più insieme a Windows.");
        };
        // Started at sign-in: hidden in the notification area, or minimised if that option is off.
        if (hidden && trayBox.Checked) startHidden = true;
        else if (hidden) WindowState = FormWindowState.Minimized;
        ResumeLayout(false);
        PerformLayout();
    }

    // The wait before typing, in seconds: whatever is written in its box, kept within the limits.
    int Delay
    {
        get { return Number(delayBox, MinDelay, MaxDelay, DefaultDelay); }
        set { delayBox.Text = Math.Max(MinDelay, Math.Min(MaxDelay, value)).ToString(); }
    }

    // For how many days what is copied stays on disk; zero keeps it only in memory.
    int Retention
    {
        get { return Number(keepBox, 0, Texts.MaxKeepDays, Texts.DefaultKeepDays); }
        set { keepBox.Text = Math.Max(0, Math.Min(Texts.MaxKeepDays, value)).ToString(); }
    }

    // How many of the last days the button that cleans the history leaves alone; zero is none.
    int Spared
    {
        get { return Number(cleanBox, 0, Texts.MaxKeepDays, 0); }
        set { cleanBox.Text = Math.Max(0, Math.Min(Texts.MaxKeepDays, value)).ToString(); }
    }

    static int Number(TextBox box, int min, int max, int fallback)
    {
        int written;
        return int.TryParse(box.Text, out written) ? Math.Max(min, Math.Min(max, written)) : fallback;
    }

    // Hands the two settings of the history to what keeps it, in memory and on disk.
    void ApplyHistory()
    {
        Change(delegate
        {
            if (historyBox.Checked) Texts.Keep(Retention);
            else Texts.Clear();
        });
    }

    // Opens the guide, or brings it forward if it is already open. It is not a dialog: it can stay beside
    // the window while the user tries what it says.
    void ShowHelp()
    {
        if (help == null || help.IsDisposed)
        {
            help = new HelpForm();
            help.Show(this);
        }
        else help.Activate();
    }

    // The lower part of the window shows either the list or the options.
    void ShowView(bool options)
    {
        EndCapture();
        HidePeek();
        optionsShown = options;
        foreach (Control control in optionControls) control.Visible = options;
        foreach (Control control in listControls) control.Visible = !options;
        viewButton.Text = options ? "Elenco" : "Opzioni";
        viewButton.Sign = options ? Sign.List : Sign.Options;
        if (!options) search.Focus();
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
        Entry group = null;   // the folder whose texts are being listed: it comes just before them
        bool listed = false;  // whether its own row is in the list
        foreach (Entry entry in entries)
        {
            // A folder is always there, except in a search: then it appears only with what was found in it.
            if (entry.IsGroup)
            {
                group = entry;
                listed = !searching;
                if (listed) ShowRow(savedList, entry, selected);
                continue;
            }
            if (!Texts.Matches(entry, search.Text)) continue;
            // The history can hold thousands: the search reaches the others. The copied ones come last.
            if (entry.Name == null && recentList.Items.Count == MaxShown) break;
            if (entry.Group != null)
            {
                if (!searching && !group.Open) continue;
                if (!listed) ShowRow(savedList, group, selected);
                listed = true;
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
        int left = row.Left + RowInset + (entry.Group != null ? GroupIndent : 0);
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
            // On the right of a folder, how many texts it holds.
            string count = entries.FindAll(delegate(Entry other) { return Texts.SameName(other.Group, entry.Name); }).Count.ToString();
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
                    if (edit.Group != null) Texts.SetGroup(edit.Group, true);   // or the text would not be seen
                })) continue;
                if (source != null && source.Name == null) Texts.Remove(source);   // it now has a line among the saved ones
                Say("Salvato «" + Shorten(name) + "».");
                break;
            }
        }
        Reload();
    }

    // The folders of the saved texts, by name.
    List<string> GroupNames()
    {
        return entries.FindAll(delegate(Entry entry) { return entry.IsGroup; }).ConvertAll(delegate(Entry entry) { return entry.Name; });
    }

    // Asks the name of a new folder, or another name for the given one.
    void EditGroup(string old)
    {
        using (EditForm edit = new EditForm(old))
        {
            while (edit.ShowDialog(this) == DialogResult.OK)
            {
                string name = edit.EntryName;
                if (!Texts.SameName(name, old) && GroupNames().Exists(delegate(string other) { return Texts.SameName(other, name); }))
                {
                    MessageBox.Show(this, "La cartella «" + name + "» esiste già.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    continue;
                }
                if (!Change(delegate
                {
                    if (old == null) Texts.SetGroup(name, true);
                    else Texts.RenameGroup(old, name);
                })) continue;
                Say((old == null ? "Creata la cartella «" : "La cartella ora si chiama «") + Shorten(name) + "».");
                break;
            }
        }
        Reload();
    }

    // Puts a saved text into a folder, which opens to show it, or into none.
    void MoveTo(Entry entry, string group)
    {
        if (!Change(delegate
        {
            Texts.Move(entry.Name, group);
            if (group != null) Texts.SetGroup(group, true);
        })) return;
        Say(group == null ? "«" + Shorten(entry.Name) + "» non è più in una cartella."
            : "«" + Shorten(entry.Name) + "» è nella cartella «" + Shorten(group) + "».");
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
            // Only the folder goes: what it holds is too easily a password to be deleted along.
            bool holds = entries.Exists(delegate(Entry other) { return Texts.SameName(other.Group, entry.Name); });
            if (!Ask("Vuoi eliminare la cartella «" + entry.Name + "»?"
                + (holds ? "\n\nI testi che contiene non vengono eliminati: restano tra i salvati, fuori dalla cartella." : ""))
                || !Change(delegate { Texts.RenameGroup(entry.Name, null); })) return;
        }
        else if (!Ask("Vuoi eliminare «" + entry.Name + "»?") || !Change(delegate { Texts.Delete(entry.Name); })) return;
        Reload();
    }

    // Runs something that touches the saved texts; if the registry refuses it, says why and returns false.
    bool Change(Action change)
    {
        try
        {
            change();
            return true;
        }
        catch (Exception ex)
        {
            status.Text = "Errore: " + ex.Message;
            return false;
        }
    }

    bool Ask(string question)
    {
        return MessageBox.Show(this, question, Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question,
            MessageBoxDefaultButton.Button2) == DialogResult.Yes;
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

    // A key combination as the user reads it: "Ctrl+Alt+V".
    static string NameOf(Keys keys)
    {
        Keys code = keys & Keys.KeyCode;
        string key = code >= Keys.D0 && code <= Keys.D9 ? (code - Keys.D0).ToString() : code.ToString();
        return ((keys & Keys.Control) != 0 ? "Ctrl+" : "") + ((keys & Keys.Alt) != 0 ? "Alt+" : "")
            + ((keys & Keys.Shift) != 0 ? "Maiusc+" : "") + key;
    }

    // Whether a combination can be a global hotkey: a letter, a digit or a function key, with Ctrl or Alt
    // so that it is never something one types.
    static bool Usable(Keys keys)
    {
        Keys code = keys & Keys.KeyCode;
        bool key = code >= Keys.A && code <= Keys.Z || code >= Keys.D0 && code <= Keys.D9 || code >= Keys.F1 && code <= Keys.F12;
        return key && (keys & (Keys.Control | Keys.Alt)) != 0;
    }

    // Writes the keys of the global hotkeys wherever they are named: on their buttons and in the tooltips.
    void NameHotkeys()
    {
        typeKeysButton.Text = HotkeyName;
        pickKeysButton.Text = PickName;
        const string change = "\nIl pulsante a destra mostra i suoi tasti: premilo per dargliene altri.";
        Tip(HotkeyName + " digita subito gli appunti nella finestra attiva, senza attesa.\n"
          + "Non funziona se il desktop remoto è a schermo intero e cattura tutte le\n"
          + "scorciatoie: in quel caso usa il pulsante «" + ButtonText + "»." + change, hotkeyBox);
        Tip(PickName + " porta in primo piano questa finestra con l'elenco, da qualsiasi programma.\n"
          + "Scelta una voce, il controllo torna alla finestra da cui sei partito: lì la incolli\n"
          + "con Ctrl+V o la digiti con " + HotkeyName + ".\n"
          + "Finché è attiva, " + PickName + " non arriva più agli altri programmi." + change, pickBox);
        Tip("Premi qui e poi i tasti nuovi: Ctrl o Alt, anche con Maiusc, insieme a una lettera,\n"
          + "una cifra o un tasto funzione. Esc lascia quelli di adesso, Canc torna a quelli di partenza.\n"
          + "Con Ctrl+Alt evita le lettere che con AltGr scrivono un simbolo (E, per l'euro): la\n"
          + "scorciatoia scatterebbe scrivendolo.", typeKeysButton, pickKeysButton);
        Tip("Digita il testo degli appunti un tasto alla volta, dove incollare non funziona:\n"
          + "copia la password, premi qui e clicca nel campo della finestra remota.\n"
          + "Oppure premi " + HotkeyName + " direttamente nel campo.", typeButton);
        Tip("Scrivi per cercare nelle due colonne: parole in qualsiasi ordine, senza badare a\n"
          + "maiuscole e accenti. Le frecce scorrono la colonna, Tab passa all'altra.\n"
          + "Invio o un clic rimettono la voce negli appunti: poi la incolli con Ctrl+V o la\n"
          + "digiti con " + HotkeyName + ". Con un clic la finestra resta aperta, con Invio o un\n"
          + "doppio clic torni dov'eri. Il tasto destro apre il menu della voce.\n"
          + "Fermando il puntatore su una voce copiata la vedi per intero.\n"
          + "La stellina a destra di una voce copiata (Ctrl+" + PinKey + ") la tiene in cima all'elenco,\n"
          + "anche oltre i giorni della cronologia, finché non la togli.\n"
          + "I testi salvati restano nel registro di Windows, cifrati per il tuo utente, e\n"
          + "se ne vede solo il nome. Si possono raccogliere in cartelle: «Nuova cartella» è nel\n"
          + "menu del tasto destro (Ctrl+Maiusc+" + NewKey + "); un testo ci entra trascinandolo, o con\n"
          + "«Sposta in». Un clic sulla cartella la apre e la chiude.", search);
    }

    // Waits for the keys of a global hotkey: the next combination pressed becomes it. Meanwhile both
    // hotkeys are off, or Windows would take them before this window sees them. Asked again, gives up.
    void AwaitKeys(CheckBox box)
    {
        bool again = capturing == box;
        EndCapture();
        if (again || busy) return;
        capturing = box;
        Native.UnregisterHotKey(Handle, HotkeyId);
        Native.UnregisterHotKey(Handle, PickId);
        (box == hotkeyBox ? typeKeysButton : pickKeysButton).Text = "Premi i tasti…";
        status.Text = "Premi i tasti nuovi: Ctrl o Alt con una lettera, una cifra o un tasto funzione. "
            + "Esc lascia quelli di adesso, Canc torna a quelli di partenza.";
    }

    // Stops waiting for keys: the hotkeys work again, with the keys they have now.
    void EndCapture()
    {
        if (capturing != null) UpdateHotkeys();
    }

    // A key pressed while a hotkey awaits its new keys. Always true: none of them does anything else.
    bool TakeKeys(Keys keys)
    {
        Keys code = keys & Keys.KeyCode;
        if (code == Keys.ControlKey || code == Keys.ShiftKey || code == Keys.Menu) return true;   // the key is still to come
        bool typing = capturing == hotkeyBox;
        if (keys == Keys.Escape)
        {
            EndCapture();
            return true;
        }
        if (keys == Keys.Delete) keys = typing ? DefaultTypeKeys : DefaultPickKeys;
        if (!Usable(keys))
            status.Text = NameOf(keys) + " non va bene: serve Ctrl o Alt con una lettera, una cifra o un tasto funzione.";
        else if (keys == (typing ? pickKeys : typeKeys) || Array.IndexOf(ListKeys, keys) >= 0)
            status.Text = NameOf(keys) + " serve già al programma: scegline un'altra.";
        else
        {
            CheckBox box = capturing;
            if (typing) typeKeys = keys;
            else pickKeys = keys;
            box.Checked = true;   // new keys are given to a hotkey that is wanted
            EndCapture();
            // Unticked again if Windows refused the keys: the status line already says so.
            if (box.Checked) Say((typing ? "Per digitare gli appunti ora premi " : "Per aprire l'elenco ora premi ") + NameOf(keys) + ".");
        }
        return true;
    }

    // Stops remembering what is copied, or starts again. Not a setting: the next start always listens.
    void Pause(bool pause)
    {
        paused = pause;
        ShowPause();
        Filter();   // an empty column says why
        Say(pause ? "In pausa: quello che copi non viene ricordato, finché non riprendi la cattura."
            : "La cattura è ripresa: quello che copi torna nell'elenco.");
    }

    // Says whether the capture is paused where it shows: in the menus, above the column and on the icon.
    void ShowPause()
    {
        foreach (ToolStripItem item in pauseItems) item.Text = paused ? "Riprendi la cattura" : "Sospendi la cattura";
        recentTitle.Text = RecentTitle + (paused ? " (in pausa)" : "");
        if (IsHandleCreated) ShowClipboardState();
    }

    // A passing message: after a while it leaves the status line, and its room goes back to the lists.
    void Say(string text)
    {
        status.Text = text;
        fade.Start();
    }

    // Gives the status line the room of what it says, one line or two, and none while it is silent; the
    // lists take what is left above it.
    void FitStatus()
    {
        if (!shown) return;
        int full = (int)(StatusHeight * Zoom), gap = (int)(Gap * Zoom), bottom = typeButton.Top - gap;
        int height = status.Text.Length == 0 ? 0 : TextRenderer.MeasureText(status.Text, status.Font, new Size(status.Width, 0),
            TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix).Height > status.Font.Height * 3 / 2 ? full : full / 2;
        status.SetBounds(status.Left, bottom - height, status.Width, height);
        foreach (Control column in new Control[] { recentCard, savedCard, divider }) column.Height = status.Top - gap - column.Top;
        divider.Invalidate();   // its mark stays in the middle
    }

    static Label NewLabel(string text, int y)
    {
        Label label = new Label();
        label.Text = text;
        label.AutoSize = true;
        label.Location = new Point(Edge, y);
        return label;
    }

    // The name of a category of options, with some air above it unless it opens its column.
    void PlaceTitle(string text, int left, ref int top)
    {
        if (top > Edge) top += SectionGap;
        Label title = NewLabel(text, top);
        title.Left = left;
        title.Font = new Font(Font, FontStyle.Bold);
        // A thin line under the title, as wide as the column: the accent colour is left to what can be clicked.
        Label rule = new Label();
        rule.SetBounds(left, top + TitleRow - RuleGap, ColumnWidth, 1);
        rule.BackColor = Look.Border;
        optionControls.Add(title);
        optionControls.Add(rule);
        top += TitleRow;
    }

    // A row with a number to write: its description on the left, its box at the right edge of the column.
    // Returns the description, for its tooltip.
    Label PlaceNumber(TextBox box, string text, int max, Action confirm, int left, ref int top)
    {
        Label label = NewLabel(text, top + 5);
        label.Left = left;
        box.SetBounds(left + ColumnWidth - NumberWidth, top, NumberWidth, NumberHeight);
        box.TextAlign = HorizontalAlignment.Center;
        box.MaxLength = max.ToString().Length;
        box.KeyPress += delegate(object sender, KeyPressEventArgs e)
        {
            bool enter = e.KeyChar == (char)Keys.Enter;
            if (enter) confirm();   // Enter confirms what was written, and nothing else
            e.Handled = enter || !char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar);
        };
        box.Leave += delegate { confirm(); };
        top += NumberRow;
        optionControls.Add(label);
        optionControls.Add(Look.Frame(box));   // the frame of the box is what goes into the window
        return label;
    }

    // A row with a global hotkey: the switch that turns it on and, at the right edge of the column, the
    // button that shows its keys and takes new ones.
    void PlaceHotkey(CheckBox box, RoundButton keys, string text, int left, ref int top)
    {
        int row = top + 4;   // the switch level with the text of the button
        PlaceOption(box, text, left, ref row);
        PlaceButton(keys, "", delegate { AwaitKeys(box); }, left + ColumnWidth - OptionButtonWidth, ref top);
    }

    // A row with a link, for something to open rather than to set.
    LinkLabel PlaceLink(string text, Action open, int left, ref int top)
    {
        LinkLabel link = new LinkLabel();
        link.Text = text;
        link.AutoSize = true;
        link.Location = new Point(left, top);
        link.LinkColor = Look.Accent;
        link.ActiveLinkColor = Look.Ink;
        link.LinkBehavior = LinkBehavior.AlwaysUnderline;   // or it would pass for the title of a category
        link.LinkClicked += delegate { open(); };
        optionControls.Add(link);
        top += RowHeight;
        return link;
    }

    // A row with a small button, for something to do at once rather than to set.
    void PlaceButton(RoundButton button, string text, Action act, int left, ref int top)
    {
        button.Text = text;
        button.SetBounds(left, top, OptionButtonWidth, ActionHeight);
        Look.Style(button, false);
        button.Click += delegate { act(); };
        optionControls.Add(button);
        top += NumberRow;
    }

    // The menu under the buttons that export and import: what to take, then on with it.
    static ContextMenuStrip Choice(Action<bool> act)
    {
        ContextMenuStrip menu = new ContextMenuStrip();
        menu.Items.Add("Solo i testi salvati…", null, delegate { act(false); });
        menu.Items.Add("Tutto, anche le cose copiate…", null, delegate { act(true); });
        Look.Style(menu);
        return menu;
    }

    static string BackupFilter
    {
        get { return "File di testo (*" + Backup.Extension + ")|*" + Backup.Extension; }
    }

    // Writes the saved texts, and with 'all' what was copied too, to a text file chosen by the user.
    void Export(bool all)
    {
        if (!Ask("Il file conterrà i testi in chiaro, password comprese: chiunque lo apra potrà leggerli.\n\nVuoi continuare?")) return;
        using (SaveFileDialog dialog = new SaveFileDialog())
        {
            dialog.Filter = BackupFilter;
            dialog.FileName = Program.AppName + " " + DateTime.Today.ToString(Texts.DayFormat) + Backup.Extension;
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            BackupResult result = null;
            if (!Change(delegate { result = Backup.Export(dialog.FileName, all); })) return;
            Say("Esportati: " + Count(result.Saved, "testo salvato", "testi salvati")
                + (all ? ", " + Count(result.Copied, "copiato", "copiati") : "")
                + (result.Pictures > 0 ? ". Le immagini (" + result.Pictures + ") non si esportano" : "") + ".");
        }
    }

    // Takes in a file written by Export. With 'all' the copied texts too, if the history is on.
    void Import(bool all)
    {
        using (OpenFileDialog dialog = new OpenFileDialog())
        {
            dialog.Filter = BackupFilter;
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            bool copied = all && historyBox.Checked;
            BackupResult result = null;
            if (!Change(delegate
            {
                result = Backup.Import(dialog.FileName, copied, delegate(int clashes)
                {
                    return Ask((clashes == 1 ? "Un testo salvato del file ha il nome di uno che esiste già."
                        : clashes + " testi salvati del file hanno il nome di uno che esiste già.")
                        + "\n\nSì: vale quello del file.\nNo: resta quello che c'è.");
                });
            })) return;
            Say(result.Saved + result.Copied + result.Kept == 0 ? "Nel file non c'era niente da importare."
                : "Importati: " + Count(result.Saved, "testo salvato", "testi salvati")
                + (copied ? ", " + Count(result.Copied, "copiato", "copiati") : "")
                + (result.Kept > 0 ? ". Lasciati com'erano: " + result.Kept : "")
                + (all && !copied ? ". La cronologia è spenta: i copiati non sono stati importati" : "") + ".");
        }
        Reload();
    }

    static string Count(int number, string one, string many)
    {
        return number + " " + (number == 1 ? one : many);
    }

    // Removes at once what was copied before the days to spare; with zero, everything. Pinned entries stay.
    void CleanHistory()
    {
        int days = Spared;
        string what = days == 0 ? "tutto quello che hai copiato" : days == 1 ? "quello che hai copiato prima di oggi"
            : "quello che hai copiato prima degli ultimi " + days + " giorni (oggi compreso)";
        if (!Ask("Vuoi eliminare " + what + "?\n\nLe voci con la stellina e i testi salvati restano.")) return;
        int before = Texts.Recent.Count;
        if (!Change(delegate { Texts.Clean(days); })) return;
        int removed = before - Texts.Recent.Count;
        Say(removed == 0 ? "Niente da eliminare nella cronologia."
            : removed == 1 ? "Eliminata 1 voce dalla cronologia." : "Eliminate " + removed + " voci dalla cronologia.");
        Reload();
    }

    // Shows the folder of the history files in Explorer.
    void OpenFolder()
    {
        if (Directory.Exists(Texts.Folder)) Change(delegate { Process.Start(Texts.Folder); });
        else Say("La cartella della cronologia non esiste ancora: su disco non c'è niente.");
    }

    void PlaceOption(CheckBox box, string text, int left, ref int top)
    {
        box.Text = text;
        box.AutoSize = true;
        box.Location = new Point(left, top);
        optionControls.Add(box);
        top += RowHeight;
    }

    void Tip(string text, params Control[] controls)
    {
        foreach (Control control in controls) tips.SetToolTip(control, text);
    }

    // With nothing to say the status line is silent, and leaves its room to the lists: how to type a
    // password is in the tooltip of its button and in the guide.
    void ShowIdle()
    {
        status.Text = "";
    }

    // Started hidden: the window that Application.Run wants to show stays invisible, but its handle is created
    // all the same, because hotkeys and clipboard notifications hang on it.
    protected override void SetVisibleCore(bool value)
    {
        if (startHidden)
        {
            startHidden = false;
            if (!IsHandleCreated) CreateHandle();
            value = false;
        }
        base.SetVisibleCore(value);
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        MinimumSize = Size;   // as designed, at the scale of this screen
        shown = true;
        PlaceColumns((int)(savedColumn * Zoom));
        FitStatus();
    }

    // How much larger than designed this screen draws the window.
    float Zoom
    {
        get { return CurrentAutoScaleDimensions.Width / 96f; }
    }

    // Gives the column of the saved texts the asked width, as far as both columns stay usable; the copied
    // ones take the rest of the window.
    void PlaceColumns(int width)
    {
        int gap = savedCard.Left - recentCard.Right, right = savedCard.Right, least = (int)(MinColumn * Zoom);
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

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        Look.Caption(this);
        Native.SendMessage(search.Handle, Native.EM_SETCUEBANNER, (IntPtr)1, "Cerca…");   // 1: shown also while the box has the focus
        UpdateHotkeys();
        Native.AddClipboardFormatListener(Handle);
        ShowClipboardState();
    }

    // Windows notifies every clipboard change; here we only look at whether it holds text, without opening or
    // reading it, so as not to get in the way of other programs.
    void ShowClipboardState()
    {
        bool ready = Native.IsClipboardFormatAvailable(Native.CF_UNICODETEXT);
        Icon = ready ? readyIcon : emptyIcon;
        tray.Icon = ready ? readyTrayIcon : emptyTrayIcon;
        tray.Text = Program.AppName + (ready ? ": testo pronto negli appunti" : ": appunti senza testo")
            + (paused ? ", cattura in pausa" : "");
    }

    // Greyscale copy of the icon, in the requested size.
    static Icon Gray(Icon source, Size size)
    {
        using (Icon sized = new Icon(source, size))
        using (Bitmap color = sized.ToBitmap())
        using (Bitmap gray = new Bitmap(color.Width, color.Height))
        {
            using (Graphics graphics = Graphics.FromImage(gray))
            using (ImageAttributes attributes = new ImageAttributes())
            {
                // Each channel becomes the brightness of the pixel; transparency stays as it is.
                attributes.SetColorMatrix(new ColorMatrix(new float[][]
                {
                    new float[] { 0.30f, 0.30f, 0.30f, 0, 0 },
                    new float[] { 0.59f, 0.59f, 0.59f, 0, 0 },
                    new float[] { 0.11f, 0.11f, 0.11f, 0, 0 },
                    new float[] { 0, 0, 0, 1, 0 },
                    new float[] { 0, 0, 0, 0, 1 }
                }));
                graphics.DrawImage(color, new Rectangle(Point.Empty, gray.Size), 0, 0, color.Width, color.Height,
                    GraphicsUnit.Pixel, attributes);
            }
            // The icon created by GetHicon must be destroyed by hand: we keep a copy that manages itself.
            IntPtr handle = gray.GetHicon();
            try
            {
                return (Icon)Icon.FromHandle(handle).Clone();
            }
            finally
            {
                Native.DestroyIcon(handle);
            }
        }
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        Native.RemoveClipboardFormatListener(Handle);
        Native.UnregisterHotKey(Handle, HotkeyId);
        Native.UnregisterHotKey(Handle, PickId);
        base.OnHandleDestroyed(e);
    }

    void UpdateHotkeys()
    {
        if (!IsHandleCreated) return;
        if (capturing != null)
        {
            // New keys were being awaited: no longer, and the buttons show again the ones in use.
            capturing = null;
            NameHotkeys();
        }
        bool typeFailed = !Register(hotkeyBox, HotkeyId, typeKeys);
        bool pickFailed = !Register(pickBox, PickId, pickKeys);
        if (busy) return;
        const string remedy = " è già usata da un altro programma: dalle altri tasti nelle opzioni, oppure ";
        if (typeFailed) status.Text = HotkeyName + remedy + "usa il pulsante.";
        else if (pickFailed) status.Text = PickName + remedy + "apri la finestra dall'icona.";
        else ShowIdle();
    }

    // Registers a hotkey if its checkbox is ticked. If Windows refuses it, unticks the checkbox and returns false;
    // the checkbox is remembered as 'refused', so that the setting is saved as the user left it and the next
    // start tries again.
    bool Register(CheckBox box, int id, Keys keys)
    {
        Native.UnregisterHotKey(Handle, id);
        if (!box.Checked) return true;
        uint modifiers = ((keys & Keys.Control) != 0 ? Native.MOD_CONTROL : 0) | ((keys & Keys.Alt) != 0 ? Native.MOD_ALT : 0)
            | ((keys & Keys.Shift) != 0 ? Native.MOD_SHIFT : 0);
        if (Native.RegisterHotKey(Handle, id, modifiers | Native.MOD_NOREPEAT, (uint)(keys & Keys.KeyCode)))
        {
            refused.Remove(box);
            return true;
        }
        box.Checked = false;
        if (!refused.Contains(box)) refused.Add(box);
        return false;
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == Native.WM_HOTKEY && m.WParam.ToInt32() == HotkeyId)
        {
            Run(0);
        }
        else if (m.Msg == Native.WM_HOTKEY && m.WParam.ToInt32() == PickId)
        {
            OpenList();
        }
        else if (Program.ShowMessage != 0 && (uint)m.Msg == Program.ShowMessage)
        {
            RestoreWindow();
        }
        else if (m.Msg == Native.WM_ACTIVATEAPP && m.WParam == IntPtr.Zero)
        {
            // The user moved on to another program: a later choice must not jump back to where the list
            // hotkey was pressed long before.
            previous = IntPtr.Zero;
        }
        else if (m.Msg == Native.WM_CLIPBOARDUPDATE)
        {
            ShowClipboardState();
            if (historyBox.Checked && !paused && Native.GetClipboardSequenceNumber() != ownCopy) Texts.Remember();
            Reload();
        }
        base.WndProc(ref m);
    }

    // Brings the window to the foreground, whether it is minimised or hidden in the notification area.
    void RestoreWindow()
    {
        Show();
        if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal;
        Activate();
    }

    void HideToTray()
    {
        Hide();
        if (trayHintShown) return;
        trayHintShown = true;
        tray.ShowBalloonTip(4000, Program.AppName,
            "Sono ancora attivo qui. Clic per riaprire, tasto destro e poi Esci per chiudere.", ToolTipIcon.Info);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (WindowState == FormWindowState.Minimized && trayBox.Checked) HideToTray();
        // A narrower window may leave too little to the copied entries: the saved ones give some back.
        else if (shown && WindowState != FormWindowState.Minimized)
        {
            PlaceColumns(savedCard.Width);
            FitStatus();   // in a wider window the same message may take one line less
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            tray.Dispose();   // otherwise the icon would stay in the notification area after exit
            peek.Dispose();
            rest.Dispose();
            fade.Dispose();
        }
        base.Dispose(disposing);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // With the icon active the X only hides: exit is from the icon menu (or at Windows shutdown).
        if (trayBox.Checked && !exiting && !closePending && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            HideToTray();
            return;
        }
        if (busy)
        {
            // Closing halfway through a key would leave Shift or Alt "pressed": it closes as soon as typing stops.
            cancel = true;
            closePending = true;
            e.Cancel = true;
            return;
        }
        SaveSettings();
        base.OnFormClosing(e);
    }

    async void Run(int seconds)
    {
        if (busy) return;
        EndCapture();
        busy = true;
        cancel = false;
        typeButton.Text = "Annulla";
        try
        {
            string copied = Clipboard.ContainsText() ? Clipboard.GetText() : "";
            string text = copied.Replace("\r\n", "\n").Replace('\r', '\n').TrimEnd('\n');
            if (text.Length == 0)
            {
                status.Text = "Negli appunti non c'è testo: copia prima la password.";
                return;
            }
            if ((text.Length > UnusualLength || text.IndexOf('\n') >= 0) && !ConfirmUnusual(text))
            {
                status.Text = "Annullato.";
                return;
            }

            IntPtr target = await WaitForTarget(seconds);
            if (target == IntPtr.Zero) return;
            if ((Native.GetKeyState(Native.VK_CAPITAL) & 1) != 0)   // the low bit tells whether the lock is on
            {
                status.Text = "Bloc Maiusc è attivo: disattivalo e riprova.";
                return;
            }

            TypeOptions options = new TypeOptions();
            options.UsKeyboard = usBox.Checked;
            options.AltCodes = altCodeBox.Checked;
            options.PressEnter = enterBox.Checked;
            options.KeyDelayMs = slowBox.Checked ? SlowKeyDelayMs : KeyDelayMs;

            status.Text = "Digitazione in corso in " + WindowTitle(target) + "…";
            TypeResult result = await Task.Run(delegate
            {
                return Typer.TypeText(text, target, options, delegate { return cancel; });
            });
            status.Text = Describe(result, options);

            // Cleared only if the clipboard still holds the password just typed; the list forgets it too.
            if (result.Outcome == TypeOutcome.Done && clearBox.Checked
                && Clipboard.ContainsText() && Clipboard.GetText() == copied)
            {
                Clipboard.Clear();
                Texts.Forget(copied);
            }
        }
        catch (Exception ex)   // for example the clipboard locked by another program
        {
            status.Text = "Errore: " + ex.Message;
        }
        finally
        {
            busy = false;
            typeButton.Text = ButtonText;
            if (closePending) Close();
        }
    }

    bool ConfirmUnusual(string text)
    {
        // A message box whose owner is hidden or minimised would stay invisible.
        RestoreWindow();
        int lines = text.Split('\n').Length;
        string what = Count(text.Length) + (lines > 1 ? " su " + lines + " righe" : "");
        return MessageBox.Show(this,
            "Gli appunti contengono " + what + ": non sembra una password.\n\nVuoi digitarli comunque?",
            Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.Yes;
    }

    // Waits for another program's window to stay in the foreground for 'seconds' seconds and returns it.
    // Returns IntPtr.Zero (and explains why in the status line) if the user cancels or does not choose in time.
    async Task<IntPtr> WaitForTarget(int seconds)
    {
        Stopwatch clock = Stopwatch.StartNew();
        IntPtr candidate = IntPtr.Zero;
        double candidateSince = 0;
        while (!cancel)
        {
            double now = clock.Elapsed.TotalSeconds;
            IntPtr foreground = Native.GetForegroundWindow();
            if (IsValidTarget(foreground))
            {
                if (foreground != candidate)
                {
                    candidate = foreground;
                    candidateSince = now;
                }
                double left = seconds - (now - candidateSince);
                if (left <= 0) return foreground;
                status.Text = "Digito tra " + Math.Ceiling(left) + " s in " + WindowTitle(foreground)
                    + ". Premi Annulla per fermare.";
            }
            else if (now > ArmedSeconds)
            {
                status.Text = "Tempo scaduto: non hai scelto la finestra in cui digitare.";
                return IntPtr.Zero;
            }
            else
            {
                // The right window still has to be selected: even when started from the hotkey, which has no
                // wait of its own, time is needed to click into the field.
                candidate = IntPtr.Zero;
                seconds = Math.Max(seconds, MinDelay);
                status.Text = "Clicca ora nel campo password della finestra remota…";
            }
            await Task.Delay(PollMs);
        }
        status.Text = "Annullato.";
        return IntPtr.Zero;
    }

    static bool IsValidTarget(IntPtr window)
    {
        if (window == IntPtr.Zero) return false;
        uint processId;
        Native.GetWindowThreadProcessId(window, out processId);
        if (processId == OwnProcessId) return false;
        StringBuilder className = new StringBuilder(256);
        Native.GetClassName(window, className, className.Capacity);
        return Array.IndexOf(ShellClasses, className.ToString()) < 0;
    }

    static string WindowTitle(IntPtr window)
    {
        StringBuilder buffer = new StringBuilder(256);
        Native.GetWindowText(window, buffer, buffer.Capacity);
        string title = buffer.ToString().Trim();
        if (title.Length == 0) return "una finestra senza titolo";
        return "«" + Shorten(title) + "»";
    }

    static string Shorten(string text)
    {
        return text.Length > MaxTitleLength ? text.Substring(0, MaxTitleLength - 1) + "…" : text;
    }

    static string Describe(TypeResult result, TypeOptions options)
    {
        switch (result.Outcome)
        {
            case TypeOutcome.KeysHeld:
                return "Rilascia i tasti che stai tenendo premuti e riprova.";
            case TypeOutcome.Blocked:
                return "Windows ha bloccato l'invio dei tasti. Se il programma di desktop remoto è avviato come "
                    + "amministratore, avvia così anche " + Program.AppName + ".";
            case TypeOutcome.Cancelled:
                return result.Typed == 0 ? "Annullato." : "Interrotto dopo " + Count(result.Typed) + ".";
            case TypeOutcome.FocusLost:
                return "Interrotto dopo " + Count(result.Typed) + ": la finestra attiva è cambiata.";
        }

        string done = "Fatto: " + Count(result.Typed) + (result.Typed == 1 ? " digitato." : " digitati.");
        if (result.AltCoded > 0)
            done += " " + Count(result.AltCoded) + " con Alt+codice.";
        if (result.Unicode > 0)
            done += " Attenzione: " + Count(result.Unicode) + " senza tasto sulla tastiera, "
                + (result.Unicode == 1 ? "inviato" : "inviati") + " come Unicode"
                + (options.AltCodes ? "." : ": se l'accesso fallisce prova «Alt+codice».");
        return done;
    }

    static string Count(int characters)
    {
        return Count(characters, "carattere", "caratteri");
    }

    // The program used to be called PasswordPaste: as long as its registry key exists, the settings and saved
    // texts in it move here. What is already here stays: the new key may have been written to in the meantime.
    static void AdoptOldSettings()
    {
        using (RegistryKey old = Registry.CurrentUser.OpenSubKey(OldSettingsKey))
        {
            if (old == null) return;
            using (RegistryKey current = Registry.CurrentUser.CreateSubKey(SettingsKey))
            {
                CopyMissing(old, current);
            }
        }
        // Reached only if the copy went through to the end.
        Registry.CurrentUser.DeleteSubKeyTree(OldSettingsKey, false);
    }

    static void CopyMissing(RegistryKey from, RegistryKey to)
    {
        foreach (string name in from.GetValueNames())
            if (to.GetValue(name) == null) to.SetValue(name, from.GetValue(name), from.GetValueKind(name));
        foreach (string name in from.GetSubKeyNames())
        {
            using (RegistryKey source = from.OpenSubKey(name))
            using (RegistryKey target = to.CreateSubKey(name))
            {
                CopyMissing(source, target);
            }
        }
    }

    void LoadSettings()
    {
        try
        {
            AdoptOldSettings();
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(SettingsKey))
            {
                if (key != null) TransferSettings(key, false);
            }
        }
        catch (Exception)
        {
            // Settings unreadable or of an unexpected type: the defaults stay.
        }
    }

    void SaveSettings()
    {
        try
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(SettingsKey))
            {
                TransferSettings(key, true);
            }
        }
        catch (Exception)
        {
            // Registry not writable: the settings are not remembered.
        }
    }

    // Writes the settings to the registry ('save') or reads them back. Each one is listed here only once, so
    // reading and writing cannot diverge; when reading, whatever was never saved stays as it is.
    void TransferSettings(RegistryKey key, bool save)
    {
        Delay = Transfer(key, save, "Delay", Delay);
        usBox.Checked = Transfer(key, save, "UsKeyboard", usBox.Checked);
        enterBox.Checked = Transfer(key, save, "PressEnter", enterBox.Checked);
        clearBox.Checked = Transfer(key, save, "ClearClipboard", clearBox.Checked);
        slowBox.Checked = Transfer(key, save, "Slow", slowBox.Checked);
        altCodeBox.Checked = Transfer(key, save, "AltCodes", altCodeBox.Checked);
        hotkeyBox.Checked = Transfer(key, save, "Hotkey", hotkeyBox.Checked || refused.Contains(hotkeyBox));
        pickBox.Checked = Transfer(key, save, "PickHotkey", pickBox.Checked || refused.Contains(pickBox));
        typeKeys = Sound(Transfer(key, save, "HotkeyKeys", (int)typeKeys), DefaultTypeKeys);
        pickKeys = Sound(Transfer(key, save, "PickKeys", (int)pickKeys), DefaultPickKeys);
        historyBox.Checked = Transfer(key, save, "History", historyBox.Checked);
        Retention = Transfer(key, save, "KeepDays", Retention);
        trayBox.Checked = Transfer(key, save, "Tray", trayBox.Checked);
        savedColumn = Transfer(key, save, "SavedWidth", savedColumn);   // applied when the window loads

        // Window position. Reading starts from a point outside every screen: it stays there, and is ignored,
        // if the registry holds nothing; the same happens if the screen of that time is no longer connected.
        Point corner = !save ? OffScreen : WindowState == FormWindowState.Normal ? Location : RestoreBounds.Location;
        corner = new Point(Transfer(key, save, "Left", corner.X), Transfer(key, save, "Top", corner.Y));
        if (save) return;
        foreach (Screen screen in Screen.AllScreens)
        {
            if (!screen.WorkingArea.Contains(corner)) continue;
            StartPosition = FormStartPosition.Manual;
            Location = corner;
        }
    }

    static int Transfer(RegistryKey key, bool save, string name, int value)
    {
        if (!save) return (int)key.GetValue(name, value);
        key.SetValue(name, value);
        return value;
    }

    // The keys of a hotkey as the registry gives them, or the ones it starts with if they cannot be used.
    static Keys Sound(int keys, Keys fallback)
    {
        return Usable((Keys)keys) ? (Keys)keys : fallback;
    }

    static bool Transfer(RegistryKey key, bool save, string name, bool value)
    {
        return Transfer(key, save, name, value ? 1 : 0) != 0;
    }
}
