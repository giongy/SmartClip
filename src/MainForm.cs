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

partial class MainForm : Form
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
    const int ListExtra = 17;                   // height given to the lists beyond what the options need
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
    const TextFormatFlags LineFlags = TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis;   // one line of a row
    public const string SettingsKey = @"Software\" + Program.AppName;
    const string OldSettingsKey = @"Software\PasswordPaste";   // the name of the program up to version 1.2
    const string LanguageSetting = "Language";                 // read before the window exists: see SavedLanguage
    const string ThemeSetting = "Theme";                       // the same: see SavedTheme
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
    readonly CheckBox leaveBox = new Toggle();   // hides the window when another program comes to the foreground
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
    readonly List<ToolStripItem> traySaved = new List<ToolStripItem>();   // what the icon menu lists of the saved texts
    readonly RoundButton languageButton = new RoundButton();   // shows the language and opens the list of the others
    readonly RoundButton themeButton = new RoundButton();      // the same for the theme
    string language = Strings.Code;   // saved with the settings: another one only on the way to a restart

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
        // An ordinary window, which others can cover: it stays on top only while it waits and types (see Run),
        // so that the countdown and the cancel button are in view over the remote window.
        KeyPreview = true;
        BackColor = Look.Back;
        ForeColor = Look.Ink;
        using (Stream icon = typeof(MainForm).Assembly.GetManifestResourceStream(Program.AppName + ".ico"))
        {
            if (icon != null) Icon = new Icon(icon);
        }

        typeButton.Text = Strings.TypeButton;
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

        // The options, stacked by category: the height of the window follows from them.
        int top = PlaceOptions();

        // Under the options, or the lists that take their place: the status line, then one row of small buttons.
        // On the left those that act on the selected entry, on the right typing and the switch between the views.
        int areaBottom = top + 4 + ListExtra, statusTop = areaBottom + Gap, actionTop = statusTop + StatusHeight + Gap;
        ClientSize = new Size(Edge + ContentWidth + Edge, actionTop + ActionHeight + Edge);
        status.SetBounds(Edge, statusTop, ContentWidth, StatusHeight);
        newButton.Text = Strings.New;
        newButton.Sign = Sign.Plus;
        deleteButton.Sign = Sign.Delete;
        typeButton.Sign = Sign.Keys;
        newButton.SetBounds(Edge, actionTop, ActionWidth, ActionHeight);
        saveButton.SetBounds(newButton.Right + Gap, actionTop, ActionWidth, ActionHeight);
        deleteButton.Text = Strings.Delete;
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
        recentTitle = NewLabel(Strings.RecentTitle, titleTop);
        savedTitle = NewLabel(Strings.SavedTitle, titleTop);
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

        WireLists();

        tips.AutoPopDelay = 30000;
        Look.Style(tips);
        Tip(string.Format(Strings.NewTip, NameOf(NewKeys)), newButton);
        Tip(string.Format(Strings.SaveTip, NameOf(SaveKeys)), saveButton);
        Tip(string.Format(Strings.HelpTip, NameOf(HelpKey)), helpButton);
        Tip(string.Format(Strings.DeleteTip, NameOf(DeleteKeys)), deleteButton);
        Tip(Strings.DividerTip, divider);
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
        trayMenu.Items.Add(Strings.Open, null, delegate { RestoreWindow(); });
        pauseItems.Add(trayMenu.Items.Add("", null, delegate { Pause(!paused); }));
        trayMenu.Items.Add(Strings.Exit, null, delegate { exiting = true; Close(); });
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
                status.Text = Strings.StartupRefused;
            else if (done) Say(string.Format(wanted ? Strings.StartupOn : Strings.StartupOff, Program.AppName));
        };
        // Started at sign-in: hidden in the notification area, or minimised if that option is off.
        if (hidden && trayBox.Checked) startHidden = true;
        else if (hidden) WindowState = FormWindowState.Minimized;
        ResumeLayout(false);
        PerformLayout();
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
        viewButton.Text = options ? Strings.List : Strings.Options;
        viewButton.Sign = options ? Sign.List : Sign.Options;
        if (!options) search.Focus();
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
            status.Text = string.Format(Strings.Error, ex.Message);
            return false;
        }
    }

    bool Ask(string question)
    {
        return AskForm.Ask(this, question, false);
    }

    // Stops remembering what is copied, or starts again. Not a setting: the next start always listens.
    void Pause(bool pause)
    {
        paused = pause;
        ShowPause();
        Filter();   // an empty column says why
        Say(pause ? Strings.PausedSay : Strings.ResumedSay);
    }

    // Says whether the capture is paused where it shows: in the menus, above the column and on the icon.
    void ShowPause()
    {
        foreach (ToolStripItem item in pauseItems) item.Text = paused ? Strings.Resume : Strings.Pause;
        recentTitle.Text = Strings.RecentTitle + (paused ? Strings.PausedSuffix : "");
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
        // Minimised, the window has no room and its controls are squeezed to nothing: placing them now would
        // leave them wrong when it comes back. OnResize calls again then.
        if (!shown || WindowState == FormWindowState.Minimized) return;
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

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        Look.Caption(this);
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
        string tip = Program.AppName + (ready ? Strings.TrayReady : Strings.TrayEmpty) + (paused ? Strings.TrayPaused : "");
        tray.Text = tip.Length > TrayTextLimit ? tip.Substring(0, TrayTextLimit - 1) + "…" : tip;
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
            // With the option, the window goes to the notification area. Not while it waits and types, when
            // the user is meant to click elsewhere, nor while a window of ours (a text being written, a
            // question, a file to choose) holds it disabled: something may be copied elsewhere for it.
            if (leaveBox.Checked && trayBox.Checked && Visible && !busy && Native.IsWindowEnabled(Handle)
                && WindowState != FormWindowState.Minimized)
            {
                if (help != null && !help.IsDisposed) help.Close();   // with no taskbar button, it would be lost
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
            if (each.Name == null) continue;
            Entry entry = each;   // a variable of its own for the delegate
            string place = PlaceOf(entry);
            ToolStripMenuItem holder;
            if (place == null || !folders.TryGetValue(place, out holder)) holder = outside;
            if (entry.IsGroup)
            {
                ToolStripMenuItem folder = new ToolStripMenuItem(entry.Label);
                Look.Style((ToolStripDropDownMenu)folder.DropDown);
                folders[entry.Name] = folder;
                holder.DropDownItems.Add(folder);
            }
            else holder.DropDownItems.Add(entry.Label, null, delegate { Use(entry); });
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
        // A narrower window may leave too little to the copied entries: the saved ones give some back.
        else if (shown && WindowState != FormWindowState.Minimized)
        {
            PlaceColumns((int)(savedColumn * Zoom));
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
}
