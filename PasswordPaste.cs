using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

[assembly: AssemblyTitle(Program.AppName)]
[assembly: AssemblyDescription("Digita il contenuto degli appunti dove l'incolla non funziona")]
[assembly: AssemblyProduct(Program.AppName)]
[assembly: AssemblyVersion("1.1.0.0")]

// Types the clipboard content as real key presses (scancodes),
// so it works even where paste is disabled (lock screens of remote desktops).
static class Program
{
    public const string AppName = "PasswordPaste";
    public static readonly uint ShowMessage = Native.RegisterWindowMessage(AppName + ".Show");

    [STAThread]
    static void Main()
    {
        bool first;
        using (new Mutex(true, AppName + ".SingleInstance", out first))
        {
            if (!first)
            {
                // Already running: bring the existing window to the foreground.
                Native.AllowSetForegroundWindow(Native.ASFW_ANY);
                Native.PostMessage(Native.HWND_BROADCAST, ShowMessage, IntPtr.Zero, IntPtr.Zero);
                return;
            }

            Native.SetProcessDPIAware();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}

class MainForm : Form
{
    // The global hotkey: registration and texts all start from here.
    const uint HotkeyModifiers = Native.MOD_CONTROL | Native.MOD_ALT;
    const Keys HotkeyKey = Keys.V;
    static readonly string HotkeyName = "Ctrl+Alt+" + HotkeyKey;
    const int HotkeyId = 1;

    const int MinDelay = 1, MaxDelay = 30;             // limits, in seconds, of the wait before typing
    const int DefaultDelay = 1;                        // wait proposed until the user picks another one
    const int ArmedSeconds = 15;                       // time allowed to bring the target window to the foreground
    const int PollMs = 100;                            // how often the foreground window is checked again
    const int KeyDelayMs = 15, SlowKeyDelayMs = 60;    // pause after each key press or release
    const int UnusualLength = 100;                     // a longer text is unlikely to be a password
    const int MaxTitleLength = 45;                     // beyond this, the title of the target window is truncated
    const int Edge = 12, ContentWidth = 316;           // margin and usable width of the window, in pixels at 96 dpi
    const int RowHeight = 24;                          // distance between one option checkbox and the next
    const string ButtonText = "Digita gli appunti";
    const string SettingsKey = @"Software\" + Program.AppName;

    // Desktop, taskbar, Start menu and Alt+Tab switcher: never the intended target.
    static readonly string[] ShellClasses =
    {
        "Progman", "WorkerW", "Shell_TrayWnd", "Shell_SecondaryTrayWnd", "Windows.UI.Core.CoreWindow",
        "XamlExplorerHostIslandWindow", "MultitaskingViewFrame", "TaskSwitcherWnd"
    };
    static readonly uint OwnProcessId = (uint)Process.GetCurrentProcess().Id;
    static readonly Point OffScreen = new Point(int.MinValue, int.MinValue);

    readonly Button typeButton = new Button();
    readonly Label status = new Label();
    readonly NumericUpDown delayBox = new NumericUpDown();
    readonly ComboBox keyboardBox = new ComboBox();
    readonly int usKeyboardIndex;
    readonly CheckBox enterBox = new CheckBox();
    readonly CheckBox clearBox = new CheckBox();
    readonly CheckBox slowBox = new CheckBox();
    readonly CheckBox altCodeBox = new CheckBox();
    readonly CheckBox hotkeyBox = new CheckBox();
    readonly CheckBox trayBox = new CheckBox();
    readonly NotifyIcon tray = new NotifyIcon();
    readonly ToolTip tips = new ToolTip();
    readonly Icon readyIcon, emptyIcon, readyTrayIcon, emptyTrayIcon;
    bool busy, closePending;
    bool exiting;         // the user chose Esci (Exit) in the icon menu: the close is real, not a "hide"
    bool trayHintShown;   // the balloon explaining where the window went is shown only once
    volatile bool cancel;

    public MainForm()
    {
        SuspendLayout();
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        Text = Program.AppName;
        Font = SystemFonts.MessageBoxFont;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        TopMost = true;
        using (Stream icon = typeof(MainForm).Assembly.GetManifestResourceStream(Program.AppName + ".ico"))
        {
            if (icon != null) Icon = new Icon(icon);
        }

        typeButton.Text = ButtonText;
        typeButton.Font = new Font(Font, FontStyle.Bold);
        typeButton.SetBounds(Edge, 12, ContentWidth, 36);
        typeButton.Click += delegate { if (busy) cancel = true; else Run((int)delayBox.Value); };

        status.UseMnemonic = false;
        status.SetBounds(Edge, 56, ContentWidth, 64);

        Label line = new Label();
        line.BorderStyle = BorderStyle.Fixed3D;
        line.SetBounds(Edge, 126, ContentWidth, 2);

        Label delayLabel = NewLabel("Attesa prima di digitare (secondi):", 142);
        delayBox.SetBounds(Edge + ContentWidth - 56, 139, 56, 23);
        delayBox.Minimum = MinDelay;
        delayBox.Maximum = MaxDelay;
        delayBox.Value = DefaultDelay;

        Label keyboardLabel = NewLabel("Tastiera del server:", 173);
        keyboardBox.DropDownStyle = ComboBoxStyle.DropDownList;
        keyboardBox.Items.Add("Uguale a questo PC");
        usKeyboardIndex = keyboardBox.Items.Add("Inglese (USA)");
        keyboardBox.SelectedIndex = 0;
        keyboardBox.SetBounds(Edge + ContentWidth - 180, 170, 180, 23);

        // The checkboxes are stacked: adding one takes a single line here, the window height adapts.
        int top = 202;
        PlaceOption(enterBox, "Premi Invio alla fine", ref top);
        PlaceOption(clearBox, "Svuota gli appunti dopo aver digitato", ref top);
        PlaceOption(slowBox, "Digitazione lenta", ref top);
        PlaceOption(altCodeBox, "Alt+codice per i simboli che la tastiera non ha", ref top);
        PlaceOption(hotkeyBox, "Scorciatoia globale " + HotkeyName, ref top);
        hotkeyBox.Checked = true;
        PlaceOption(trayBox, "Resta attivo nell'area di notifica (tray)", ref top);
        trayBox.Checked = true;
        ClientSize = new Size(Edge + ContentWidth + Edge, top + 4);

        tips.AutoPopDelay = 30000;
        Tip("Per quanti secondi la finestra di destinazione deve restare in primo piano\n"
          + "prima che parta la digitazione: è il tempo che hai per cliccare nel campo\n"
          + "giusto, o per annullare.", delayLabel, delayBox);
        Tip("Lascia «Uguale a questo PC» per i normali desktop remoti.\n"
          + "Scegli «Inglese (USA)» se i simboli arrivano sbagliati perché il server usa la\n"
          + "tastiera americana (capita con le console VMware, iLO, iDRAC e con alcune\n"
          + "schermate di accesso).", keyboardLabel, keyboardBox);
        Tip("Preme Invio dopo l'ultimo carattere, per confermare subito l'accesso.", enterBox);
        Tip("Cancella la password dagli appunti quando la digitazione è riuscita.\n"
          + "La cronologia degli appunti di Windows (Win+V), se attiva, non viene toccata.", clearBox);
        Tip("Attivala se sul server arrivano caratteri mancanti o in disordine.", slowBox);
        Tip("I simboli che questa tastiera non ha (ad esempio ~ e ` su quella italiana)\n"
          + "vengono normalmente inviati come caratteri Unicode, che non tutti i programmi\n"
          + "di desktop remoto accettano. Con questa opzione vengono invece composti con\n"
          + "Alt+codice sul tastierino numerico: funziona solo se il server è Windows.", altCodeBox);
        Tip(HotkeyName + " digita subito gli appunti nella finestra attiva, senza attesa.\n"
          + "Non funziona se il desktop remoto è a schermo intero e cattura tutte le\n"
          + "scorciatoie: in quel caso usa il pulsante.\n"
          + "Disattivala se entra in conflitto con un altro programma.", hotkeyBox);
        Tip("Attiva: chiudendo o riducendo a icona la finestra il programma resta in funzione\n"
          + "come icona vicino all'orologio, e la scorciatoia continua a funzionare.\n"
          + "Clic sull'icona per riaprire la finestra, tasto destro e poi Esci per chiudere.\n"
          + "Disattiva: finestra normale, che chiudendola termina il programma.", trayBox);

        Controls.AddRange(new Control[] { typeButton, status, line, delayLabel, delayBox, keyboardLabel, keyboardBox,
            enterBox, clearBox, slowBox, altCodeBox, hotkeyBox, trayBox });
        AcceptButton = typeButton;

        LoadSettings();

        ContextMenuStrip trayMenu = new ContextMenuStrip();
        trayMenu.Items.Add("Apri", null, delegate { RestoreWindow(); });
        trayMenu.Items.Add("Esci", null, delegate { exiting = true; Close(); });
        tray.ContextMenuStrip = trayMenu;
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
        hotkeyBox.Click += delegate { UpdateHotkey(); };
        ShowIdle();
        ResumeLayout(false);
        PerformLayout();
    }

    bool UsKeyboard
    {
        get { return keyboardBox.SelectedIndex == usKeyboardIndex; }
        set { keyboardBox.SelectedIndex = value ? usKeyboardIndex : 0; }
    }

    static Label NewLabel(string text, int y)
    {
        Label label = new Label();
        label.Text = text;
        label.AutoSize = true;
        label.Location = new Point(Edge, y);
        return label;
    }

    static void PlaceOption(CheckBox box, string text, ref int top)
    {
        box.Text = text;
        box.AutoSize = true;
        box.Location = new Point(Edge, top);
        top += RowHeight;
    }

    void Tip(string text, params Control[] controls)
    {
        foreach (Control control in controls) tips.SetToolTip(control, text);
    }

    void ShowIdle()
    {
        status.Text = "Copia la password, premi il pulsante e poi clicca nel campo password della finestra remota."
            + (hotkeyBox.Checked ? " Oppure premi " + HotkeyName + " direttamente nel campo." : "");
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        UpdateHotkey();
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
        tray.Text = Program.AppName + (ready ? ": testo pronto negli appunti" : ": appunti senza testo");
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
        base.OnHandleDestroyed(e);
    }

    void UpdateHotkey()
    {
        if (!IsHandleCreated) return;
        Native.UnregisterHotKey(Handle, HotkeyId);
        bool failed = hotkeyBox.Checked
            && !Native.RegisterHotKey(Handle, HotkeyId, HotkeyModifiers | Native.MOD_NOREPEAT, (uint)HotkeyKey);
        if (failed) hotkeyBox.Checked = false;
        if (busy) return;
        if (failed) status.Text = HotkeyName + " è già usata da un altro programma: usa il pulsante.";
        else ShowIdle();
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == Native.WM_HOTKEY && m.WParam.ToInt32() == HotkeyId)
        {
            Run(0);
        }
        else if (Program.ShowMessage != 0 && (uint)m.Msg == Program.ShowMessage)
        {
            RestoreWindow();
        }
        else if (m.Msg == Native.WM_CLIPBOARDUPDATE)
        {
            ShowClipboardState();
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
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) tray.Dispose();   // otherwise the icon would stay in the notification area after exit
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
            options.UsKeyboard = UsKeyboard;
            options.AltCodes = altCodeBox.Checked;
            options.PressEnter = enterBox.Checked;
            options.KeyDelayMs = slowBox.Checked ? SlowKeyDelayMs : KeyDelayMs;

            status.Text = "Digitazione in corso in " + WindowTitle(target) + "…";
            TypeResult result = await Task.Run(delegate
            {
                return Typer.TypeText(text, target, options, delegate { return cancel; });
            });
            status.Text = Describe(result, options);

            // Cleared only if the clipboard still holds the password just typed.
            if (result.Outcome == TypeOutcome.Done && clearBox.Checked
                && Clipboard.ContainsText() && Clipboard.GetText() == copied)
                Clipboard.Clear();
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
        return "«" + (title.Length > MaxTitleLength ? title.Substring(0, MaxTitleLength - 1) + "…" : title) + "»";
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
        return characters == 1 ? "1 carattere" : characters + " caratteri";
    }

    void LoadSettings()
    {
        try
        {
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
        int delay = Transfer(key, save, "Delay", (int)delayBox.Value);
        delayBox.Value = Math.Max(MinDelay, Math.Min(MaxDelay, delay));
        UsKeyboard = Transfer(key, save, "UsKeyboard", UsKeyboard);
        enterBox.Checked = Transfer(key, save, "PressEnter", enterBox.Checked);
        clearBox.Checked = Transfer(key, save, "ClearClipboard", clearBox.Checked);
        slowBox.Checked = Transfer(key, save, "Slow", slowBox.Checked);
        altCodeBox.Checked = Transfer(key, save, "AltCodes", altCodeBox.Checked);
        hotkeyBox.Checked = Transfer(key, save, "Hotkey", hotkeyBox.Checked);
        trayBox.Checked = Transfer(key, save, "Tray", trayBox.Checked);

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

    static bool Transfer(RegistryKey key, bool save, string name, bool value)
    {
        return Transfer(key, save, name, value ? 1 : 0) != 0;
    }
}

class TypeOptions
{
    public bool UsKeyboard, AltCodes, PressEnter;
    public int KeyDelayMs;
}

enum TypeOutcome { Done, Cancelled, FocusLost, KeysHeld, Blocked }

class TypeResult
{
    public TypeOutcome Outcome;
    public int Typed, Unicode, AltCoded;

    public TypeResult Stop(TypeOutcome outcome)
    {
        Outcome = outcome;
        return this;
    }
}

// The key (and any modifiers) that produces a character.
struct Stroke
{
    public readonly int Scan;
    public readonly bool Shift, AltGr, Dead;

    public Stroke(int scan, bool shift, bool altGr, bool dead)
    {
        Scan = scan;
        Shift = shift;
        AltGr = altGr;
        Dead = dead;
    }
}

static class Typer
{
    const int ExtendedBit = 0x100;   // added to the scancode of "extended" keys
    const int ScTab = 0x0F, ScEnter = 0x1C, ScShift = 0x2A, ScAlt = 0x38, ScSpace = 0x39, ScAltGr = ScAlt | ExtendedBit;
    const int KeyReleaseTimeoutMs = 3000;   // how long to wait for the user to release the keys being held
    const byte Pressed = 0x80;              // value of a pressed key in the ToUnicodeEx state table
    static readonly int[] ScNumpad = { 0x52, 0x4F, 0x50, 0x51, 0x4B, 0x4C, 0x4D, 0x47, 0x48, 0x49 };
    static readonly Encoding Ansi =
        Encoding.GetEncoding(1252, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
    static readonly int[] TypingKeys = BuildTypingKeys();
    public static readonly Dictionary<char, Stroke> UsKeys = BuildUsKeys();

    // Types the text into the 'target' window and stops if it loses the foreground:
    // a password must never end up in another window.
    public static TypeResult TypeText(string text, IntPtr target, TypeOptions options, Func<bool> cancelled)
    {
        TypeResult result = new TypeResult();
        int delay = options.KeyDelayMs;
        if (!WaitKeysReleased()) return result.Stop(TypeOutcome.KeysHeld);

        uint processId;
        IntPtr layout = Native.GetKeyboardLayout(Native.GetWindowThreadProcessId(target, out processId));
        if (layout == IntPtr.Zero) layout = Native.GetKeyboardLayout(0);
        Dictionary<char, Stroke> keys = options.UsKeyboard ? UsKeys : MapLayout(layout);

        foreach (char c in text)
        {
            if (cancelled()) return result.Stop(TypeOutcome.Cancelled);
            if (Native.GetForegroundWindow() != target) return result.Stop(TypeOutcome.FocusLost);

            bool sent;
            Stroke stroke;
            if (c == '\n') sent = Chord(delay, ScEnter);
            else if (c == '\t') sent = Chord(delay, ScTab);
            else if (char.IsControl(c)) continue;
            else if (keys.TryGetValue(c, out stroke)) sent = Press(stroke, delay);
            else
            {
                int? code = options.AltCodes ? AnsiCode(c) : null;
                if (code.HasValue)
                {
                    sent = AltCode(code.Value, delay);
                    result.AltCoded++;
                }
                else
                {
                    sent = SendUnicode(c, delay);
                    result.Unicode++;
                }
            }

            if (!sent) return result.Stop(TypeOutcome.Blocked);
            result.Typed++;
        }

        if (options.PressEnter)
        {
            // Gives the server time to receive the last character before confirming.
            Thread.Sleep(delay * 4);
            if (Native.GetForegroundWindow() != target) return result.Stop(TypeOutcome.FocusLost);
            if (!Chord(delay, ScEnter)) return result.Stop(TypeOutcome.Blocked);
        }
        return result;
    }

    // A key the user is still holding (typically those of a hotkey) would alter what gets typed:
    // wait for them to be released.
    static bool WaitKeysReleased()
    {
        Stopwatch clock = Stopwatch.StartNew();
        while (AnyKeyHeld())
        {
            if (clock.ElapsedMilliseconds > KeyReleaseTimeoutMs) return false;
            Thread.Sleep(20);
        }
        return true;
    }

    static bool AnyKeyHeld()
    {
        int[] modifiers = { Native.VK_SHIFT, Native.VK_CONTROL, Native.VK_MENU, Native.VK_LWIN, Native.VK_RWIN };
        foreach (int key in modifiers)
            if (Native.GetAsyncKeyState(key) < 0) return true;
        for (int key = '0'; key <= 'Z'; key++)   // the key codes of digits and letters match the characters
            if (Native.GetAsyncKeyState(key) < 0) return true;
        return false;
    }

    // The keys of the alphanumeric block. The numeric keypad is left out on purpose: its decimal point, for
    // example, becomes Delete with Num Lock off (and it is the very key VkKeyScan proposes for the '.' character).
    static int[] BuildTypingKeys()
    {
        List<int> keys = new List<int>();
        for (int scan = 0x02; scan <= 0x0D; scan++) keys.Add(scan);   // number row
        for (int scan = 0x10; scan <= 0x1B; scan++) keys.Add(scan);   // Q row
        for (int scan = 0x1E; scan <= 0x29; scan++) keys.Add(scan);   // A row and the key left of 1
        for (int scan = 0x2B; scan <= 0x35; scan++) keys.Add(scan);   // Z row
        keys.AddRange(new int[] { ScSpace, 0x56, 0x73, 0x7D });      // space, the < key and the extra Brazilian/Japanese keys
        return keys.ToArray();
    }

    // Works out which key produces each character on the given layout: alone, with Shift, with AltGr
    // or with both. Characters missing from the result have no key.
    public static Dictionary<char, Stroke> MapLayout(IntPtr layout)
    {
        Dictionary<char, Stroke> keys = new Dictionary<char, Stroke>();
        Dictionary<char, Stroke> deadKeys = new Dictionary<char, Stroke>();
        char[] produced = new char[8];
        char[] discarded = new char[8];
        byte[] nothingPressed = new byte[256];

        // From the simplest combination to the most complex: if a character has several, the first one wins.
        for (int modifiers = 0; modifiers < 4; modifiers++)
        {
            bool shift = (modifiers & 1) != 0, altGr = (modifiers & 2) != 0;
            byte[] keyState = new byte[256];
            if (shift) keyState[Native.VK_SHIFT] = Pressed;
            if (altGr) keyState[Native.VK_CONTROL] = keyState[Native.VK_MENU] = Pressed;   // AltGr is equivalent to Ctrl+Alt

            foreach (int scan in TypingKeys)
            {
                uint vk = Native.MapVirtualKeyEx((uint)scan, Native.MAPVK_VSC_TO_VK, layout);
                if (vk == 0) continue;
                int count = Native.ToUnicodeEx(vk, (uint)scan, keyState, produced, produced.Length,
                    Native.TOUNICODE_KEEP_STATE, layout);
                char c = produced[0];
                if (count < 0)
                {
                    // Dead key (for example ^ on the German keyboard): it produces the character only when followed
                    // by a space. This second call clears it on Windows versions that ignore TOUNICODE_KEEP_STATE.
                    Native.ToUnicodeEx(Native.VK_SPACE, ScSpace, nothingPressed, discarded, discarded.Length,
                        Native.TOUNICODE_KEEP_STATE, layout);
                    if (!deadKeys.ContainsKey(c)) deadKeys.Add(c, new Stroke(scan, shift, altGr, true));
                }
                else if (count == 1 && !char.IsControl(c) && !keys.ContainsKey(c))
                {
                    keys.Add(c, new Stroke(scan, shift, altGr, false));
                }
            }
        }

        // A dead key is used only for characters that have no ordinary key.
        foreach (KeyValuePair<char, Stroke> dead in deadKeys)
            if (!keys.ContainsKey(dead.Key)) keys.Add(dead.Key, dead.Value);
        return keys;
    }

    // The US keyboard, for servers that use it whatever the layout of this PC.
    static Dictionary<char, Stroke> BuildUsKeys()
    {
        // For each row: scancode of the first key, characters without Shift, characters with Shift.
        Dictionary<char, Stroke> keys = new Dictionary<char, Stroke>();
        AddUsRow(keys, 0x02, "1234567890-=", "!@#$%^&*()_+");
        AddUsRow(keys, 0x10, "qwertyuiop[]", "QWERTYUIOP{}");
        AddUsRow(keys, 0x1E, "asdfghjkl;'`", "ASDFGHJKL:\"~");
        AddUsRow(keys, 0x2B, "\\zxcvbnm,./", "|ZXCVBNM<>?");
        keys[' '] = new Stroke(ScSpace, false, false, false);
        return keys;
    }

    static void AddUsRow(Dictionary<char, Stroke> keys, int firstScan, string plain, string shifted)
    {
        for (int i = 0; i < plain.Length; i++)
        {
            keys[plain[i]] = new Stroke(firstScan + i, false, false, false);
            keys[shifted[i]] = new Stroke(firstScan + i, true, false, false);
        }
    }

    // Code used to compose the character through Alt+numpad (Windows-1252), if it has one.
    static int? AnsiCode(char c)
    {
        try
        {
            byte[] encoded = Ansi.GetBytes(new char[] { c });
            if (encoded.Length == 1 && encoded[0] >= ' ') return encoded[0];
        }
        catch (EncoderFallbackException)
        {
            // The character does not exist in Windows-1252.
        }
        return null;
    }

    static bool Press(Stroke stroke, int delay)
    {
        bool sent = stroke.Shift && stroke.AltGr ? Chord(delay, ScShift, ScAltGr, stroke.Scan)
            : stroke.Shift ? Chord(delay, ScShift, stroke.Scan)
            : stroke.AltGr ? Chord(delay, ScAltGr, stroke.Scan)
            : Chord(delay, stroke.Scan);
        return sent && (!stroke.Dead || Chord(delay, ScSpace));
    }

    // Presses the keys in the given order and releases them in reverse order. Whatever was pressed is always
    // released, even if Windows rejects a send: a modifier left "pressed" would corrupt everything after it.
    static bool Chord(int delay, params int[] keys)
    {
        bool sent = true;
        int down = 0;
        while (sent && down < keys.Length)
        {
            sent = Send(keys[down], false);
            if (sent) down++;
            Thread.Sleep(delay);
        }
        while (down > 0)
        {
            sent &= Send(keys[--down], true);
            Thread.Sleep(delay);
        }
        return sent;
    }

    // Holds Alt and types the code on the numeric keypad. The leading zero tells Windows to
    // interpret it as Windows-1252, which is how AnsiCode computes it.
    static bool AltCode(int code, int delay)
    {
        if (!Send(ScAlt, false)) return false;
        Thread.Sleep(delay);
        bool sent = true;
        foreach (char digit in "0" + code)
            sent = sent && Chord(delay, ScNumpad[digit - '0']);
        sent &= Send(ScAlt, true);
        Thread.Sleep(delay);
        return sent;
    }

    static bool SendUnicode(char c, int delay)
    {
        bool sent = Inject(c, Native.KEYEVENTF_UNICODE);
        Thread.Sleep(delay);
        sent &= Inject(c, Native.KEYEVENTF_UNICODE | Native.KEYEVENTF_KEYUP);
        Thread.Sleep(delay);
        return sent;
    }

    static bool Send(int key, bool up)
    {
        uint flags = Native.KEYEVENTF_SCANCODE;
        if ((key & ExtendedBit) != 0) flags |= Native.KEYEVENTF_EXTENDEDKEY;
        if (up) flags |= Native.KEYEVENTF_KEYUP;
        return Inject((ushort)(key & ~ExtendedBit), flags);
    }

    static bool Inject(ushort scan, uint flags)
    {
        Native.INPUT[] input = new Native.INPUT[1];
        input[0].type = Native.INPUT_KEYBOARD;
        input[0].U.ki.wScan = scan;
        input[0].U.ki.dwFlags = flags;
        return Native.SendInput(1, input, Marshal.SizeOf(typeof(Native.INPUT))) == 1;
    }
}

// Windows functions and constants, with their official names.
static class Native
{
    public const int WM_HOTKEY = 0x0312, WM_CLIPBOARDUPDATE = 0x031D;
    public const uint CF_UNICODETEXT = 13;
    public const uint MOD_ALT = 0x0001, MOD_CONTROL = 0x0002, MOD_NOREPEAT = 0x4000;
    public const int VK_SHIFT = 0x10, VK_CONTROL = 0x11, VK_MENU = 0x12, VK_CAPITAL = 0x14, VK_SPACE = 0x20,
        VK_LWIN = 0x5B, VK_RWIN = 0x5C;
    public const uint INPUT_KEYBOARD = 1;
    public const uint KEYEVENTF_EXTENDEDKEY = 0x0001, KEYEVENTF_KEYUP = 0x0002, KEYEVENTF_UNICODE = 0x0004,
        KEYEVENTF_SCANCODE = 0x0008;
    public const uint MAPVK_VSC_TO_VK = 1;
    // The ToUnicodeEx flag that avoids altering the keyboard state (Windows 10 1607 and later);
    // the documentation describes it but gives it no name.
    public const uint TOUNICODE_KEEP_STATE = 0x0004;
    public const int ASFW_ANY = -1;
    public static readonly IntPtr HWND_BROADCAST = (IntPtr)0xFFFF;

    [StructLayout(LayoutKind.Sequential)]
    public struct MOUSEINPUT
    {
        public int dx, dy;
        public uint mouseData, dwFlags, time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct KEYBDINPUT
    {
        public ushort wVk, wScan;
        public uint dwFlags, time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct InputUnion
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct INPUT
    {
        public uint type;
        public InputUnion U;
    }

    [DllImport("user32.dll")]
    public static extern uint SendInput(uint count, INPUT[] inputs, int size);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern uint MapVirtualKeyEx(uint code, uint mapType, IntPtr layout);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int ToUnicodeEx(uint vk, uint scan, byte[] keyState, [Out] char[] buffer, int size, uint flags, IntPtr layout);

    [DllImport("user32.dll")]
    public static extern IntPtr GetKeyboardLayout(uint threadId);

    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int GetWindowText(IntPtr window, StringBuilder text, int capacity);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int GetClassName(IntPtr window, StringBuilder name, int capacity);

    [DllImport("user32.dll")]
    public static extern short GetKeyState(int vk);

    [DllImport("user32.dll")]
    public static extern short GetAsyncKeyState(int vk);

    [DllImport("user32.dll")]
    public static extern bool RegisterHotKey(IntPtr window, int id, uint modifiers, uint vk);

    [DllImport("user32.dll")]
    public static extern bool UnregisterHotKey(IntPtr window, int id);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern uint RegisterWindowMessage(string name);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern bool PostMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern bool AllowSetForegroundWindow(int processId);

    [DllImport("user32.dll")]
    public static extern bool SetProcessDPIAware();

    [DllImport("user32.dll")]
    public static extern bool AddClipboardFormatListener(IntPtr window);

    [DllImport("user32.dll")]
    public static extern bool RemoveClipboardFormatListener(IntPtr window);

    [DllImport("user32.dll")]
    public static extern bool IsClipboardFormatAvailable(uint format);

    [DllImport("user32.dll")]
    public static extern bool DestroyIcon(IntPtr icon);
}
