using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

partial class MainForm : Window
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
    // A saved text or a pinned entry can have a number: these keys held with that digit paste it into the
    // active window, from any program, without showing this one. They too can be changed and are remembered.
    public const int MaxNumber = 9;
    const Keys DefaultQuickKeys = Keys.Control | Keys.Shift;   // with Ctrl+Alt the digits would clash with AltGr
    static Keys quickKeys = DefaultQuickKeys;
    const int NumberId = 10;   // the hotkey of each number is this plus the number

    public const int MinDelay = 1, MaxDelay = 30;      // limits, in seconds, of the wait before typing
    const int DefaultDelay = 1;                        // wait proposed until the user picks another one
    public const int ArmedSeconds = 15;                // time allowed to bring the target window to the foreground
    const int PollMs = 100;                            // how often the foreground window is checked again
    const int KeyDelayMs = 15, SlowKeyDelayMs = 60;    // pause after each key press or release
    public const int UnusualLength = 100;              // a longer text is unlikely to be a password
    const int MaxTitleLength = 45;                     // beyond this, a window title or a list entry is truncated in the status
    const int Edge = 8, ContentWidth = 620;            // margin and usable width of the window, in pixels at 96 dpi
    const int AreaHeight = 437;                        // of the lists, and of the pages of the options in their window
    // The options are in pages, one per category, chosen from a column on the left as the subjects of the guide.
    // On a page each option is a card (Tile), with room for what it does under its name.
    const int NavWidth = 170, NavGap = 20, PaneWidth = 500;
    const int NavInset = 12, NavPadding = 20;          // where a row of that column starts its sign; added to the text height for the row
    const int PageTop = 48;                            // under the name of a page, where its options begin
    // The window: on the left the sidebar, the places of the entries; on the right, on a lighter pane, the title of the
    // place, the search with its buttons, the tabs of the categories, the cards of the entries and the status line.
    // The sidebar, its margin on the left included: as it starts, how narrow and how wide its edge drags it, and
    // reduced to its signs. The pane beside it gives up at most PaneSlack of its width to it.
    const int SideWidth = 210, MinSideWidth = 160, MaxSideWidth = 400, NarrowSideWidth = 72, PaneSlack = 120;
    const int PaneInset = 16;                          // inside the pane, around what it holds
    const int TitleRow = 34, TabsHeight = 32;          // the title of the place, and the tabs under the search
    const int ListHeight = 380;                        // the cards, as the window starts
    const int SideIndent = 16;                         // of a folder in the sidebar, for each folder it is inside of
    // A card: the entry first, its buttons on the right of its first line, under it the program or folder; between
    // the cards some air.
    const int CardGap = 8, CardInset = 12, CardLineGap = 6;
    const int PartHeight = 26, PartGap = 6;            // the buttons of a card, and the room between them
    const int BodyLines = 2;                           // lines of text a card shows of its entry, at most
    const int CardPicture = 72;                        // how tall a picture is shown in its card
    const int HeadingAir = 12;                         // above the name of a day, among the cards
    const int MaxRow = 255;                            // the tallest row a list box of Windows can have, in pixels
    const int PreviewMargin = 4;                       // around the preview, inside its card
    const int TileHeight = 52, TileGap = 4;            // the least a card of the options is tall, and between two cards
    const int RowHeight = 22;                          // a line of text on the card of the folder of the database
    const int NoteHeight = 18;                         // the same for a line of what it says, smaller
    const int NumberWidth = 56, NumberHeight = 26;     // the box of an option that is a number
    const int OptionButtonWidth = 130;                 // a button among the options
    const int SizeButtonWidth = 72;                    // the same for a size in pixels
    public const int MaxShown = 200;                   // copied entries listed at once
    const int SearchHeight = 30, GlyphWidth = 20;      // the search box, and the magnifying glass on its left
    const int KeysWidth = 64, KeysPadding = 10;        // the frame of its keys on its right, and the air around them
    const int ListPadding = 3;                         // between the rounded edge of the pages of the options and their list
    const int CountWidth = 120;                        // room for the number of entries, right of the title
    const int RowInset = 12, TimeGap = 8;              // of the text in a row, and around the time on its right
    const int NumberPadding = 2;                       // added to the text height for the width of the frame of a number
    const int Gap = 5;                                 // between neighbouring controls
    const int ActionHeight = ButtonHeight, ActionWidth = 92;   // a button beside the search
    const int TypeWidth = 160;                         // the button that types the clipboard, in the same row
    const int StatusHeight = 44;                       // two lines of text in their bar: the room the status line starts with
    const int FadeMs = 6000;                           // how long a passing message stays in the status line
    const int RestMs = 600;                            // how long the pointer rests on a row before it is shown whole
    const int AskedMs = 15000;                         // how long a saved text stays in view once asked for
    public const Keys NewKey = Keys.N, SaveKey = Keys.S, DeleteKey = Keys.E, PinKey = Keys.P;   // in the list, all with Ctrl
    public const Keys SearchKey = Keys.F;   // with Ctrl, anywhere in the window: to the search box
    const Keys NewKeys = Keys.Control | NewKey, GroupKeys = Keys.Control | Keys.Shift | NewKey, SaveKeys = Keys.Control | SaveKey,
        DeleteKeys = Keys.Control | DeleteKey, PinKeys = Keys.Control | PinKey, SearchKeys = Keys.Control | SearchKey;
    // What the list itself answers to: a global hotkey on the same keys would take them away from it.
    static readonly Keys[] ListKeys = { NewKeys, GroupKeys, SaveKeys, DeleteKeys, PinKeys, SearchKeys };
    const TextFormatFlags LineFlags = TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis;   // one line of a row
    // What a row says at its right end: when it was copied, how many texts a folder holds.
    const TextFormatFlags RightFlags = TextFormatFlags.VerticalCenter | TextFormatFlags.Right | TextFormatFlags.SingleLine
        | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding;
    // The checks point both elsewhere, so that they never touch the settings and the saved texts of the user.
    public static string SettingsKey = @"Software\" + Program.AppName;
    public static string OldSettingsKey = @"Software\PasswordPaste";   // the name of the program up to version 1.2
    const string LanguageSetting = "Language";                 // read before the window exists: see SavedLanguage
    const string ThemeSetting = "Theme";                       // the same: see SavedTheme
    const string FaceSetting = "InterfaceFont", FaceSizeSetting = "InterfaceSize";   // and see UseSavedInterface
    const string DataFolderSetting = "DataFolder";             // the same: the database is opened before the settings are read
    const int TrayTextLimit = 63;                              // the most a NotifyIcon accepts as its tooltip

    // Desktop, taskbar, Start menu and Alt+Tab switcher: never the intended target.
    static readonly string[] ShellClasses =
    {
        "Progman", "WorkerW", "Shell_TrayWnd", "Shell_SecondaryTrayWnd", "Windows.UI.Core.CoreWindow",
        "XamlExplorerHostIslandWindow", "MultitaskingViewFrame", "TaskSwitcherWnd"
    };
    static readonly uint OwnProcessId = (uint)Process.GetCurrentProcess().Id;
    static readonly Point OffScreen = new Point(int.MinValue, int.MinValue);

    readonly RoundButton typeButton = new RoundButton();
    HelpForm help;      // the guide, while it is open
    readonly OptionsWindow options;   // the window of the options: made once, hidden when closed
    bool optionsAtStart;              // it opens as soon as this window shows
    readonly Card pane = new Card();              // the lighter area on the right of the sidebar, where the entries are
    readonly Label title = new Label();           // the name of the place shown
    readonly Label count = new Label();           // how many entries it lists
    readonly EmptyNote emptyNote = new EmptyNote();   // what the list says while it has none
    readonly InfoBar status = new InfoBar();   // what just happened, tinted by its kind
    readonly InfoBar banner = new InfoBar();   // red, under the list: the room for copied pictures is full
    bool bannerShown;                      // asked back, Visible would answer false while the window is hidden
    readonly TextBox search = new TextBox();
    readonly TabStrip tabs = new TabStrip();      // the categories: all, texts, code, links, pictures
    readonly RowList cardList = new RowList();    // the entries of the place, as cards, with the names of the days among them
    readonly RowList navList = new RowList();     // the sidebar: the places (see Place)
    readonly RowList footList = new RowList();    // under it: a new folder, the options, the guide
    Place place = new Place(PlaceKind.All);       // the place shown
    bool savedOpen = true, sourcesOpen = true;    // the sidebar shows the folders, and the programs
    bool fillingNav;                              // the sidebar is being filled: its selection is not the user's
    readonly RoundButton sideButton = new RoundButton();   // on top of the sidebar: reduces it to its signs, and back
    int sideWidth = SideWidth;                    // as the user dragged it, in pixels at 96 dpi
    bool sideNarrow;                              // only the signs of the places show
    bool sizingSide;                              // its edge is being dragged
    int sideGrab, sideBefore;                     // from the pointer to the edge, and the width the drag started from
    // A tooltip shown by hand where the pointer rests (see HoverTip): the control, which part of it, what it says
    // and where.
    readonly ToolTip hoverTips = new ToolTip();
    readonly Timer hoverTipWait = new Timer();    // runs while the pointer rests there, before the tooltip shows
    Control tipOwner;
    int tipKey = -1;
    string tipText;
    Point tipAt;
    Size leastSize;                               // the window as designed: it is never made smaller
    readonly ContextMenuStrip listMenu = new ContextMenuStrip();
    readonly ContextMenuStrip navMenu = new ContextMenuStrip();   // of a folder in the sidebar
    readonly Peek peek = new Peek();              // shows whole the entry of a card
    readonly Timer rest = new Timer();            // runs while the pointer rests on a card, and while a text asked for is in view
    RowList resting;                              // the list whose row the pointer rests on
    bool peekAsked;                               // what is in view was asked for: moving the pointer leaves it there
    readonly Timer fade = new Timer();            // runs while the status line holds a passing message
    Entry grabbed;                                // the saved text or folder the button went down on: it may be dragged to a folder
    Point grabbedAt;
    Place dropPlace;                              // the folder a dragged text would go to, while it is over one; Saved for none
    int hoverRow = -1;                            // the card the pointer is on, and the part of it
    Part hoverPart;
    bool paused;                                  // what is copied is not remembered, until the user says otherwise
    readonly List<ToolStripItem> pauseItems = new List<ToolStripItem>();   // the menu entries that say so
    CheckBox capturing;                           // the hotkey whose new keys are awaited, or null
    Size savedSize;                               // the size the user left the window at, empty until there is one
    bool shown;                                   // the window has its final scale
    readonly RoundButton newButton = new RoundButton();
    // The options, which belong to this window although they are shown in their own (see OptionsWindow).
    readonly RowList pageList = new RowList();   // the categories of the options: each item is the page of one
    readonly Panel pagePane = new Panel();       // where the pages take turns
    Panel page;                                  // the page being filled, while the options are placed
    Panel restartPage;                           // where the language is: the options reopen there after changing it
    readonly TextBox delayBox = new TextBox(), keepBox = new TextBox(), cleanBox = new TextBox();
    readonly RoundButton cleanButton = new RoundButton(), exportButton = new RoundButton(), importButton = new RoundButton();
    readonly RoundButton typeKeysButton = new RoundButton(), pickKeysButton = new RoundButton();   // show the keys of a hotkey, and take new ones
    readonly RoundButton quickKeysButton = new RoundButton();   // the same for the keys of the numbers
    readonly CheckBox usBox = new Toggle();
    readonly CheckBox enterBox = new Toggle();
    readonly CheckBox clearBox = new Toggle();
    readonly CheckBox slowBox = new Toggle();
    readonly CheckBox altCodeBox = new Toggle();
    readonly CheckBox hotkeyBox = new Toggle();
    readonly CheckBox pickBox = new Toggle();
    readonly CheckBox quickBox = new Toggle();   // the numbered entries are pasted by their keys
    readonly CheckBox historyBox = new Toggle();
    readonly CheckBox trayBox = new Toggle();
    readonly CheckBox leaveBox = new Toggle();   // hides the window when another program comes to the foreground
    readonly CheckBox restBox = new Toggle();    // the pointer resting on a card shows the entry whole
    readonly CheckBox whereLeftBox = new Toggle();   // the list hotkey opens on the place and kind left, not on All
    readonly CheckBox startBox = new Toggle();   // not among the settings: it shows what the Run key of Windows says
    bool startHidden;   // the first request to show the window is not obeyed
    readonly NotifyIcon tray = new NotifyIcon();
    readonly ToolTip tips = new ToolTip();
    readonly Icon readyIcon, emptyIcon, readyTrayIcon, emptyTrayIcon;
    bool busy, closePending;
    bool exiting;         // the user chose Esci (Exit) in the icon menu: the close is real, not a "hide"
    bool trayHintShown;   // the balloon explaining where the window went is shown only once
    volatile bool cancel;
    Clips clips;          // what the list keeps: copied and saved entries, folders
    string unavailable;   // why the database could not be opened: the status line says it whenever it is idle
    string dataFolder = "";   // the folder the user chose for the database; empty: the default one
    readonly Label folderPath = new Label();   // in the options, where the database is
    readonly Label databaseSize = new Label(); // and how much it takes
    readonly RoundButton changeFolderButton = new RoundButton(), defaultFolderButton = new RoundButton();
    Control[] folderTipped;   // what the tooltip about that folder is on
    List<Entry> entries = new List<Entry>();   // the saved entries and the folders, each folder right before what it holds
    // How many texts each folder holds, those of the folders inside it too: counted when the sidebar is filled,
    // drawn on its row.
    readonly Dictionary<string, int> held = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    bool enterDown;    // Enter was pressed in the list, so its release belongs to it
    bool pasting;       // the entry of a number is being pasted
    string numbersTaken = "";   // the keys of numbers another program has: said once, not at every copy
    IntPtr previous;    // the window the list hotkey was pressed in: the foreground goes back there
    bool wasHidden;     // the list hotkey found the window hidden: after the choice it hides again
    uint ownCopy;       // clipboard sequence number of what was put there from the list: not to be remembered again
    readonly List<CheckBox> refused = new List<CheckBox>();   // hotkey checkboxes unticked by Windows, not by the user
    readonly List<ToolStripItem> traySaved = new List<ToolStripItem>();   // what the icon menu lists of the saved texts
    readonly RoundButton languageButton = new RoundButton();   // shows the language and opens the list of the others
    readonly RoundButton themeButton = new RoundButton();      // the same for the theme
    readonly RoundButton faceButton = new RoundButton(), faceSizeButton = new RoundButton();   // for the font of the interface and its size
    readonly RoundButton fontButton = new RoundButton(), sizeButton = new RoundButton();   // and for the font of the entries and its size
    string language = Strings.Code;   // saved with the settings: another one only on the way to a restart
    string face = Look.InterfaceFamily;   // the same for the font of the interface
    int faceSize = Look.InterfaceSize;

    public MainForm() : this(false)
    {
    }

    // 'hidden': started by Windows at sign-in, so the window does not show until asked.
    public MainForm(bool hidden)
    {
        Text = Program.AppName;   // the version is in the options, at the end of the program page
        // The window can be made larger, never smaller than it starts, and keeps the size the user gave it (see
        // OnLoad).
        StartPosition = FormStartPosition.CenterScreen;
        // An ordinary window, which others can cover: it stays on top only while it waits and types (see Run),
        // so that the countdown and the cancel button are in view over the remote window.
        KeyPreview = true;
        using (Stream icon = typeof(MainForm).Assembly.GetManifestResourceStream(Program.AppName + ".ico"))
        {
            if (icon != null) Icon = new Icon(icon);
        }

        typeButton.Text = Strings.TypeButton;
        typeButton.Sign = Sign.Keys;
        typeButton.Click += delegate { if (busy) cancel = true; else Run(Delay); };
        Look.Style(typeButton, true);
        // Beside it a new text, a command of a bar, quiet until the pointer reaches it.
        newButton.Text = Strings.New;
        newButton.Sign = Sign.Plus;
        Look.Style(newButton, false);
        newButton.Subtle = true;

        // The status line takes the room of what it says. A message written with Say leaves after a while;
        // anything else stays until the next one. The window of the options says it too: what an option did must
        // not end up under it.
        status.TextChanged += delegate
        {
            fade.Stop();
            FitStatus();
            if (options != null) options.Note(status.Text, status.Kind);
        };
        fade.Interval = FadeMs;
        fade.Tick += delegate
        {
            fade.Stop();
            if (!busy) ShowIdle();
        };
        // The red bar, placed with the status line (FitStatus).
        banner.Visible = false;
        banner.Tell(string.Format(Strings.PicturesFull, Strings.Size(Clips.PictureSpace)), Notice.Error);

        PlaceOptions();
        options = new OptionsWindow(this);

        // On the right a lighter pane, as in the apps of Windows 11: on top the name of the place and how many entries
        // it has, then the search with a new text and the typing of the clipboard beside it, the tabs of the
        // categories, the cards, and at the bottom the status line.
        int paneWidth = PaneInset + ContentWidth + PaneInset;
        pane.Quiet = true;
        pane.BackColor = Look.Well;
        title.SetBounds(PaneInset, PaneInset, ContentWidth - CountWidth, TitleRow);
        title.Font = Look.TitleFont;
        title.UseMnemonic = false;
        title.AutoEllipsis = true;
        title.TextAlign = ContentAlignment.MiddleLeft;
        count.SetBounds(PaneInset + ContentWidth - CountWidth, PaneInset, CountWidth, TitleRow);
        count.TextAlign = ContentAlignment.MiddleRight;
        count.ForeColor = Look.Muted;
        int searchTop = title.Bottom + Gap;
        search.SetBounds(PaneInset, searchTop, ContentWidth - TypeWidth - ActionWidth - 2 * Gap, SearchHeight);
        Panel searchFrame = Look.Frame(search);
        // A magnifying glass on the left of the box, and its keys on the right: it makes room for them.
        search.Left += GlyphWidth;
        search.Width -= GlyphWidth + KeysWidth;
        searchFrame.Paint += DrawGlyph;
        typeButton.SetBounds(PaneInset + ContentWidth - TypeWidth, searchTop, TypeWidth, searchFrame.Height);
        newButton.SetBounds(typeButton.Left - Gap - ActionWidth, searchTop, ActionWidth, searchFrame.Height);
        tabs.SetBounds(PaneInset, searchFrame.Bottom + Gap, ContentWidth, TabsHeight);
        foreach (string name in new string[] { Strings.AllKinds, Strings.TextKinds, Strings.CodeKinds, Strings.LinkKinds, Strings.PictureKinds })
            tabs.Add(name);
        int listTop = tabs.Bottom + Gap, statusTop = listTop + ListHeight + Gap;
        cardList.SetBounds(PaneInset, listTop, ContentWidth, ListHeight);
        // What an empty list says, in its place.
        emptyNote.Bounds = cardList.Bounds;
        emptyNote.TextAlign = ContentAlignment.MiddleCenter;
        emptyNote.BackColor = Look.Well;
        emptyNote.ForeColor = Look.Muted;
        status.SetBounds(PaneInset, statusTop, ContentWidth, StatusHeight);
        pane.SetBounds(SideWidth, Edge, paneWidth, statusTop + StatusHeight + PaneInset);
        pane.Controls.AddRange(new Control[] { title, count, searchFrame, newButton, typeButton, tabs, cardList, emptyNote, banner, status });
        emptyNote.BringToFront();
        ClientSize = new Size(SideWidth + paneWidth + Edge, Edge + pane.Height + Edge);

        // On the left the sidebar: the button that reduces it, the places, and under them a new folder, the options
        // and the guide. Placed for good, at their width, once their rows have their height (PlaceSidebar).
        sideButton.SetBounds(Edge, Edge, ButtonHeight, ButtonHeight);
        sideButton.Sign = Sign.Menu;
        Look.Style(sideButton, false);
        sideButton.Subtle = true;
        sideButton.TabStop = false;
        sideButton.Click += delegate { NarrowSide(!sideNarrow); };
        navList.SetBounds(Edge, sideButton.Bottom + Gap, SideWidth - 2 * Edge, pane.Bottom - sideButton.Bottom - Gap);
        footList.SetBounds(Edge, Edge, SideWidth - 2 * Edge, 0);
        Controls.AddRange(new Control[] { pane, sideButton, navList, footList });
        // Thin scroll bars over those of the lists, which always have one so that their rows never change width.
        cardList.ScrollAlwaysVisible = navList.ScrollAlwaysVisible = true;
        Rail.Over(cardList);
        Rail.Over(navList);

        // When the window grows, the pane and its cards take the room; the sidebar stays as wide as it is.
        const AnchorStyles toTop = AnchorStyles.Top, toBottom = AnchorStyles.Bottom, toLeft = AnchorStyles.Left, toRight = AnchorStyles.Right;
        pane.Anchor = cardList.Anchor = emptyNote.Anchor = toTop | toBottom | toLeft | toRight;
        title.Anchor = searchFrame.Anchor = tabs.Anchor = toTop | toLeft | toRight;
        count.Anchor = newButton.Anchor = typeButton.Anchor = toTop | toRight;
        status.Anchor = banner.Anchor = toBottom | toLeft | toRight;
        navList.Anchor = toTop | toBottom | toLeft;
        footList.Anchor = toBottom | toLeft;

        WireHoverTips();
        WireLists();
        WireSidebar();

        tips.AutoPopDelay = 30000;
        Look.Style(tips);
        Tip(string.Format(Strings.NewTip, NameOf(NewKeys)), newButton);
        // No AcceptButton: Enter chooses an entry, and in the search box it must not start typing the clipboard.

        OpenClips();
        UseEntryFont(Look.EntryFamily, Look.EntrySize);   // the default, until the settings say otherwise
        LoadSettings();
        ShowFolder();
        NameHotkeys();
        ApplyHistory();
        historyBox.CheckedChanged += delegate
        {
            ApplyHistory();
            Reload();
        };
        Reload();
        ActiveControl = search;

        ContextMenuStrip trayMenu = new ContextMenuStrip();
        Look.Mark(trayMenu.Items.Add(Strings.Open, null, delegate { RestoreWindow(); }), Sign.Open);
        pauseItems.Add(trayMenu.Items.Add("", null, delegate { Pause(!paused); }));
        Look.Mark(trayMenu.Items.Add(Strings.Exit, null, delegate { exiting = true; Close(); }), Sign.Exit);
        trayMenu.Opening += delegate { ShowSaved(trayMenu); };
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
        quickBox.Click += delegate { UpdateHotkeys(); };
        ShowIdle();
        // Starting with Windows is not a setting of ours: the checkbox shows and changes what Windows has.
        Change(delegate { startBox.Checked = Startup.Enabled; });
        startBox.Click += delegate
        {
            bool wanted = startBox.Checked;
            bool done = Change(delegate { Startup.Enabled = wanted; });
            Change(delegate { startBox.Checked = Startup.Enabled; });
            // Windows may say no: once the user, or the organisation, has turned it off in its Settings.
            if (done && startBox.Checked != wanted)
                Warn(Strings.StartupRefused);
            else if (done) Say(string.Format(wanted ? Strings.StartupOn : Strings.StartupOff, Program.AppName));
        };
        // Started at sign-in: hidden in the notification area, or minimised if that option is off.
        if (hidden && trayBox.Checked) startHidden = true;
        else if (hidden) WindowState = FormWindowState.Minimized;
        Finish();
    }

    // Opens the database of the list, in the folder the user chose or in the default one. Only there is a new one
    // made, and the first time what the versions before kept in the registry and in the history files moves in; if
    // that fails, nothing is left half done and the next start tries again. In a folder of the user's choosing the
    // database must be there already: if it is not (a drive removed, a network away), no new and empty one takes
    // its place unseen. A database that cannot be opened (missing, locked, damaged, written by a newer version)
    // leaves the list working in memory, and the status line says so until it is solved.
    void OpenClips()
    {
        dataFolder = EarlySetting(DataFolderSetting) as string ?? "";
        try
        {
            if (dataFolder.Length > 0 && !File.Exists(DatabaseFile))
            {
                unavailable = string.Format(Strings.FolderUnavailable, dataFolder, Strings.HistoryTitle);
                Log.Error(new FileNotFoundException(unavailable, DatabaseFile));
            }
            else
            {
                clips = dataFolder.Length > 0 ? new Clips(DatabaseFile)
                    : new Clips(DatabaseFile, delegate(Clips fresh) { Migration.Run(fresh, SettingsKey, Migration.OldHistory); });
                Log.FileName = Path.Combine(DataFolder, Program.AppName + ".log");
                Migration.ForgetOldHistory();   // the database holds it now
                return;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex);
            unavailable = string.Format(Strings.DatabaseUnavailable, ex.Message);
        }
        clips = new Clips(Clips.Memory);
    }

    // The file of the database: in the folder the user chose, or the default one.
    string DatabaseFile
    {
        get { return dataFolder.Length > 0 ? Path.Combine(dataFolder, Path.GetFileName(Clips.DefaultFile)) : Clips.DefaultFile; }
    }

    // Its folder, where the log is too.
    string DataFolder
    {
        get { return Path.GetDirectoryName(DatabaseFile); }
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

    // Opens the window of the options over this one, or brings it forward if it is already open. Like the guide it
    // is not a dialog: it can stay open beside the list.
    void OpenOptions()
    {
        HidePeek();
        ShowSize();   // what the file takes now
        if (options.Visible) options.Activate();
        else options.Open(this);
    }

    // The guide and the options go with this window when it hides: with no taskbar button of their own, they
    // would be left alone on the screen.
    void PutAwayOthers()
    {
        if (help != null && !help.IsDisposed) help.Close();
        options.Hide();
    }

    // Runs something that may fail (the database, the registry, the clipboard): if it does, says why and returns false.
    bool Change(Action change)
    {
        try
        {
            change();
            return true;
        }
        catch (Exception ex)
        {
            ShowError(ex);
            return false;
        }
    }

    // What went wrong stays in the status line until the next message, and in the log.
    void ShowError(Exception ex)
    {
        Log.Error(ex);
        status.Tell(string.Format(Strings.Error, ex.Message), Notice.Error);
    }

    bool Ask(string question)
    {
        return AskForm.Ask(Front, question, false);
    }

    // What a question or the choice of a file belongs to: the window of the options while the user is in it, so
    // that the answer brings the user back there.
    IWin32Window Front
    {
        get { return options.Visible && ActiveForm == options ? (IWin32Window)options : this; }
    }

    // Stops remembering what is copied, or starts again. Not a setting: the next start always listens.
    void Pause(bool pause)
    {
        paused = pause;
        ShowPause();
        Filter();   // an empty list says why
        Say(pause ? Strings.PausedSay : Strings.ResumedSay);
    }

    // Says whether the capture is paused where it shows: in the menus, at the bottom of the sidebar, in the title and on the icon.
    void ShowPause()
    {
        foreach (ToolStripItem item in pauseItems)
        {
            item.Text = paused ? Strings.Resume : Strings.Pause;
            Look.Mark(item, paused ? Sign.Resume : Sign.Pause);
        }
        footList.Items[PauseRow] = paused ? Strings.Resume : Strings.Pause;
        ShowTitle();
        if (IsHandleCreated) ShowClipboardState();
    }

    // A passing message: after a while it leaves the status line, and its room goes back to the lists.
    void Say(string text)
    {
        status.Tell(text, Notice.Done);
        fade.Start();
    }

    // Something that went wrong, or may: it stays until the next message.
    void Warn(string text)
    {
        status.Tell(text, Notice.Warning);
    }

    // Gives the status line the room of what it says, in its bar, and none while it is silent; above it the red bar,
    // while it has something to say, as tall as its text; the cards take what is left.
    void FitStatus()
    {
        // Minimised, the window has no room and its controls are squeezed to nothing: placing them now would
        // leave them wrong when it comes back. OnResize calls again then.
        if (!shown || WindowState == FormWindowState.Minimized) return;
        int gap = (int)(Gap * Zoom), height = status.Fit(status.Width);
        int bottom = pane.ClientSize.Height - (int)(PaneInset * Zoom) + (height == 0 ? gap : 0);   // silent, it leaves its gap too
        status.SetBounds(status.Left, bottom - height, status.Width, height);
        int end = status.Top - (height == 0 ? 0 : gap);   // where the cards end
        if (bannerShown)
        {
            int tall = banner.Fit(status.Width);
            banner.SetBounds(status.Left, end - tall, status.Width, tall);
            end = banner.Top - gap;
        }
        cardList.Height = end - cardList.Top;
        emptyNote.Height = cardList.Height;
    }

    // Shows the red bar while the copied pictures fill their room, and only while the history is on.
    void ShowPictureRoom()
    {
        bool full = false;
        if (historyBox.Checked) Change(delegate { full = clips.PicturesFull; });
        if (full == bannerShown) return;
        bannerShown = full;
        banner.Visible = full;
        FitStatus();
    }

    void Tip(string text, params Control[] controls)
    {
        foreach (Control control in controls) tips.SetToolTip(control, text);
    }

    // With nothing to say the status line is silent, and leaves its room to the lists: how to type a
    // password is in the tooltip of its button and in the guide. Only a database that could not be opened is
    // said always.
    void ShowIdle()
    {
        if (unavailable != null) Warn(unavailable);
        else status.Text = "";
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
        MinimumSize = leastSize = Size;   // as designed, at the scale of this screen
        // The size the user left it at, never smaller than designed nor larger than the screen, which it stays on.
        if (savedSize.Width > 0 && savedSize.Height > 0)
        {
            Screen screen = Screen.FromPoint(Location);
            Size = new Size(Math.Min(screen.WorkingArea.Width, Math.Max(MinimumSize.Width, (int)(savedSize.Width * Zoom))),
                Math.Min(screen.WorkingArea.Height, Math.Max(MinimumSize.Height, (int)(savedSize.Height * Zoom))));
            PlaceOnScreen(Location, screen);
        }
        PlaceSidebar();   // at the width it was left at, once the window has its size
        shown = true;
        FitStatus();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        if (optionsAtStart) OpenOptions();   // started again from there
        optionsAtStart = false;
    }

    // How much larger than designed this screen draws the window.
    float Zoom
    {
        get { return Look.Zoom; }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        Native.SendMessage(search.Handle, Native.EM_SETCUEBANNER, (IntPtr)1, Strings.SearchCue);   // 1: shown also while the box has the focus
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
        tray.Text = Shorten(Program.AppName + (ready ? Strings.TrayReady : Strings.TrayEmpty) + (paused ? Strings.TrayPaused : ""),
            TrayTextLimit);
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
        Unhook();
        base.OnHandleDestroyed(e);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == Native.WM_HOTKEY)
        {
            int id = m.WParam.ToInt32();
            if (id == HotkeyId) Run(0);
            else if (id == PickId) OpenList();
            else if (id > NumberId && id <= NumberId + MaxNumber) PasteNumber(id - NumberId);
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
            // With the option, the window goes to the notification area. Not while it waits and types, when
            // the user is meant to click elsewhere, nor while a window of ours (a text being written, a
            // question, a file to choose) holds it disabled: something may be copied elsewhere for it.
            if (leaveBox.Checked && trayBox.Checked && Visible && !busy && Native.IsWindowEnabled(Handle)
                && WindowState != FormWindowState.Minimized)
            {
                PutAwayOthers();
                Hide();
            }
        }
        else if (m.Msg == Native.WM_SETTINGCHANGE && m.LParam != IntPtr.Zero
            && System.Runtime.InteropServices.Marshal.PtrToStringUni(m.LParam) == Look.ThemeChange)
        {
            Look.Follow();   // Windows went from light to dark or back: so do the open windows
        }
        else if (m.Msg == Native.WM_CLIPBOARDUPDATE)
        {
            ShowClipboardState();
            // Only the copied entries can have changed, and only if something was remembered: the saved ones are
            // not read again, nor the copied ones after a copy that is left out (paused, private, chosen in the list).
            if (historyBox.Checked && !paused && Native.GetClipboardSequenceNumber() != ownCopy && Texts.Remember(clips))
                ReloadCopied();
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
        PutAwayOthers();
        Hide();
        if (trayHintShown) return;
        trayHintShown = true;
        tray.ShowBalloonTip(4000, Program.AppName,
            string.Format(Strings.TrayHint, Strings.Exit), ToolTipIcon.Info);
    }

    // The saved texts in the icon menu, after «Apri», as the list has them: each folder a submenu, open or
    // closed alike. A click puts the text in the clipboard, as choosing it in the list does, without showing
    // the window. Only the names are shown, as in the list. Made anew each time the menu opens.
    void ShowSaved(ContextMenuStrip menu)
    {
        foreach (ToolStripItem item in traySaved)
        {
            menu.Items.Remove(item);
            item.Dispose();
        }
        traySaved.Clear();
        ToolStripMenuItem outside = new ToolStripMenuItem();   // holds for a while what goes into the menu itself
        Dictionary<string, ToolStripMenuItem> folders = new Dictionary<string, ToolStripMenuItem>(StringComparer.OrdinalIgnoreCase);
        foreach (Entry each in entries)
        {
            if (!each.Saved) continue;
            Entry entry = each;   // a variable of its own for the delegate
            string place = PlaceOf(entry);
            ToolStripMenuItem holder;
            if (place == null || !folders.TryGetValue(place, out holder)) holder = outside;
            if (entry.IsGroup)
            {
                ToolStripMenuItem folder = new ToolStripMenuItem(entry.Label);
                Look.Mark(folder, Sign.Folder);
                Look.Style((ToolStripDropDownMenu)folder.DropDown);
                folders[entry.Name] = folder;
                holder.DropDownItems.Add(folder);
            }
            else
            {
                ToolStripMenuItem text = new ToolStripMenuItem(entry.Label, null, delegate { Use(entry); });
                Look.Mark(text, SignOf(entry));   // what it is, as in the list
                if (entry.Number > 0) text.ShortcutKeyDisplayString = NumberName(entry.Number);   // the keys that choose it from anywhere
                holder.DropDownItems.Add(text);
            }
        }
        // An empty folder says so: with no submenu it would look like a text to click.
        foreach (ToolStripMenuItem folder in folders.Values)
            if (folder.DropDownItems.Count == 0) folder.DropDownItems.Add(Strings.EmptyFolder).Enabled = false;
        foreach (ToolStripItem item in outside.DropDownItems) traySaved.Add(item);
        if (traySaved.Count > 0)   // set apart from what the menu always has
        {
            traySaved.Insert(0, new ToolStripSeparator());
            traySaved.Add(new ToolStripSeparator());
        }
        for (int i = 0; i < traySaved.Count; i++) menu.Items.Insert(1 + i, traySaved[i]);   // which takes them from 'outside'
        outside.Dispose();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (WindowState == FormWindowState.Minimized && trayBox.Checked) HideToTray();
        // In a wider window the same message may take one line less.
        else if (shown && WindowState != FormWindowState.Minimized) FitStatus();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            tray.Dispose();   // otherwise the icon would stay in the notification area after exit
            options.Dispose();
            peek.Dispose();
            rest.Dispose();
            fade.Dispose();
            if (clips != null) clips.Dispose();
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
}
