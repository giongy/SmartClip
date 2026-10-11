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

// Digita il contenuto degli appunti come pressioni di tasti reali (scancode),
// così funziona anche dove l'incolla è disabilitato (schermate di blocco dei desktop remoti).
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
                // Già in esecuzione: riporta in primo piano la finestra esistente.
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
    // La scorciatoia globale: registrazione e testi partono tutti da qui.
    const uint HotkeyModifiers = Native.MOD_CONTROL | Native.MOD_ALT;
    const Keys HotkeyKey = Keys.V;
    static readonly string HotkeyName = "Ctrl+Alt+" + HotkeyKey;
    const int HotkeyId = 1;

    const int MinDelay = 1, MaxDelay = 30;             // limiti, in secondi, dell'attesa prima di digitare
    const int DefaultDelay = 1;                        // attesa proposta finché l'utente non ne sceglie un'altra
    const int ArmedSeconds = 15;                       // tempo concesso per portare in primo piano la finestra di destinazione
    const int PollMs = 100;                            // ogni quanto si ricontrolla quale finestra è in primo piano
    const int KeyDelayMs = 15, SlowKeyDelayMs = 60;    // pausa dopo ogni tasto premuto o rilasciato
    const int UnusualLength = 100;                     // un testo più lungo difficilmente è una password
    const int MaxTitleLength = 45;                     // oltre, il titolo della finestra di destinazione viene troncato
    const int Edge = 12, ContentWidth = 316;           // margine e larghezza utile della finestra, in pixel a 96 dpi
    const int RowHeight = 24;                          // distanza tra una casella di opzione e la successiva
    const string ButtonText = "Digita gli appunti";
    const string SettingsKey = @"Software\" + Program.AppName;

    // Desktop, barra delle applicazioni, menu Start e selettore Alt+Tab: non sono mai la destinazione voluta.
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
    bool exiting;         // l'utente ha scelto Esci dal menu dell'icona: la chiusura è vera, non un "nascondi"
    bool trayHintShown;   // il fumetto che spiega dov'è finita la finestra si mostra una volta sola
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

        // Le caselle sono impilate: per aggiungerne una basta una riga qui, l'altezza della finestra si adegua.
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
        // L'icona a colori dice che negli appunti c'è del testo pronto, quella grigia che non c'è nulla.
        readyIcon = Icon;
        emptyIcon = Gray(Icon, SystemInformation.IconSize);
        readyTrayIcon = new Icon(Icon, SystemInformation.SmallIconSize);
        emptyTrayIcon = Gray(Icon, SystemInformation.SmallIconSize);
        tray.Icon = readyTrayIcon;
        tray.MouseClick += delegate(object sender, MouseEventArgs e) { if (e.Button == MouseButtons.Left) RestoreWindow(); };
        tray.Visible = trayBox.Checked;
        trayBox.CheckedChanged += delegate { tray.Visible = trayBox.Checked; };

        // Click e non CheckedChanged: deve scattare solo quando è l'utente a cambiare la casella.
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

    // Windows avvisa a ogni cambio degli appunti; qui si guarda solo se contengono testo, senza aprirli né
    // leggerli, per non intralciare gli altri programmi.
    void ShowClipboardState()
    {
        bool ready = Native.IsClipboardFormatAvailable(Native.CF_UNICODETEXT);
        Icon = ready ? readyIcon : emptyIcon;
        tray.Icon = ready ? readyTrayIcon : emptyTrayIcon;
        tray.Text = Program.AppName + (ready ? ": testo pronto negli appunti" : ": appunti senza testo");
    }

    // Copia in scala di grigi dell'icona, nella dimensione richiesta.
    static Icon Gray(Icon source, Size size)
    {
        using (Icon sized = new Icon(source, size))
        using (Bitmap color = sized.ToBitmap())
        using (Bitmap gray = new Bitmap(color.Width, color.Height))
        {
            using (Graphics graphics = Graphics.FromImage(gray))
            using (ImageAttributes attributes = new ImageAttributes())
            {
                // Ogni canale diventa la luminosità del punto; la trasparenza resta com'è.
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
            // L'icona creata da GetHicon va distrutta a mano: se ne tiene una copia che si gestisce da sola.
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

    // Riporta la finestra in primo piano, che sia ridotta a icona o nascosta nell'area di notifica.
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
        if (disposing) tray.Dispose();   // altrimenti l'icona resterebbe nell'area di notifica dopo l'uscita
        base.Dispose(disposing);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // Con l'icona attiva la X nasconde soltanto: si esce dal menu dell'icona (o allo spegnimento di Windows).
        if (trayBox.Checked && !exiting && !closePending && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            HideToTray();
            return;
        }
        if (busy)
        {
            // Chiudere a metà di un tasto lascerebbe Maiusc o Alt "premuti": si chiude appena la digitazione si ferma.
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
            if ((Native.GetKeyState(Native.VK_CAPITAL) & 1) != 0)   // il bit basso dice se il blocco è attivo
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

            // Si svuota solo se negli appunti c'è ancora la password appena digitata.
            if (result.Outcome == TypeOutcome.Done && clearBox.Checked
                && Clipboard.ContainsText() && Clipboard.GetText() == copied)
                Clipboard.Clear();
        }
        catch (Exception ex)   // ad esempio appunti bloccati da un altro programma
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
        // Una finestra di messaggio il cui proprietario è nascosto o ridotto a icona resterebbe invisibile.
        RestoreWindow();
        int lines = text.Split('\n').Length;
        string what = Count(text.Length) + (lines > 1 ? " su " + lines + " righe" : "");
        return MessageBox.Show(this,
            "Gli appunti contengono " + what + ": non sembra una password.\n\nVuoi digitarli comunque?",
            Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.Yes;
    }

    // Attende che la finestra di un altro programma resti in primo piano per 'seconds' secondi e la restituisce.
    // Restituisce IntPtr.Zero (e spiega perché nella riga di stato) se l'utente annulla o non sceglie in tempo.
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
                // Bisogna ancora passare alla finestra giusta: anche partendo dalla scorciatoia, che di suo
                // non prevede attesa, serve il tempo di cliccare nel campo.
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
            // Impostazioni illeggibili o di tipo inatteso: restano i valori predefiniti.
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
            // Registro non scrivibile: le impostazioni non vengono ricordate.
        }
    }

    // Scrive le impostazioni nel registro ('save') oppure le rilegge. Ognuna è elencata qui una volta sola, così
    // lettura e scrittura non possono divergere; in lettura, ciò che non è mai stato salvato resta com'è.
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

        // Posizione della finestra. In lettura si parte da un punto fuori da ogni schermo: resta tale, e viene
        // ignorato, se nel registro non c'è nulla; lo stesso accade se lo schermo di allora non è più collegato.
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

// Il tasto (ed eventuali modificatori) che produce un carattere.
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
    const int ExtendedBit = 0x100;   // aggiunto allo scancode dei tasti "estesi"
    const int ScTab = 0x0F, ScEnter = 0x1C, ScShift = 0x2A, ScAlt = 0x38, ScSpace = 0x39, ScAltGr = ScAlt | ExtendedBit;
    const int KeyReleaseTimeoutMs = 3000;   // quanto si aspetta che l'utente lasci i tasti che sta premendo
    const byte Pressed = 0x80;              // valore di un tasto premuto nella tabella di stato di ToUnicodeEx
    static readonly int[] ScNumpad = { 0x52, 0x4F, 0x50, 0x51, 0x4B, 0x4C, 0x4D, 0x47, 0x48, 0x49 };
    static readonly Encoding Ansi =
        Encoding.GetEncoding(1252, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
    static readonly int[] TypingKeys = BuildTypingKeys();
    public static readonly Dictionary<char, Stroke> UsKeys = BuildUsKeys();

    // Digita il testo nella finestra 'target' e si ferma se questa perde il primo piano:
    // una password non deve mai finire in un'altra finestra.
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
            // Lascia al server il tempo di ricevere l'ultimo carattere prima di confermare.
            Thread.Sleep(delay * 4);
            if (Native.GetForegroundWindow() != target) return result.Stop(TypeOutcome.FocusLost);
            if (!Chord(delay, ScEnter)) return result.Stop(TypeOutcome.Blocked);
        }
        return result;
    }

    // Un tasto che l'utente tiene ancora premuto (tipicamente quelli di una scorciatoia) altererebbe ciò che
    // viene digitato: si aspetta che li rilasci.
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
        for (int key = '0'; key <= 'Z'; key++)   // i codici dei tasti di cifre e lettere coincidono con i caratteri
            if (Native.GetAsyncKeyState(key) < 0) return true;
        return false;
    }

    // I tasti del blocco alfanumerico. Il tastierino numerico è escluso di proposito: il suo punto, ad esempio,
    // con Bloc Num spento diventa Canc (ed è proprio il tasto che VkKeyScan propone per il carattere '.').
    static int[] BuildTypingKeys()
    {
        List<int> keys = new List<int>();
        for (int scan = 0x02; scan <= 0x0D; scan++) keys.Add(scan);   // fila dei numeri
        for (int scan = 0x10; scan <= 0x1B; scan++) keys.Add(scan);   // fila Q
        for (int scan = 0x1E; scan <= 0x29; scan++) keys.Add(scan);   // fila A e tasto a sinistra dell'1
        for (int scan = 0x2B; scan <= 0x35; scan++) keys.Add(scan);   // fila Z
        keys.AddRange(new int[] { ScSpace, 0x56, 0x73, 0x7D });      // spazio, tasto < e tasti extra brasiliani/giapponesi
        return keys.ToArray();
    }

    // Ricostruisce quale tasto produce ogni carattere sulla tastiera indicata: da solo, con Maiusc, con AltGr
    // o con entrambi. I caratteri che non compaiono nel risultato non hanno un tasto.
    public static Dictionary<char, Stroke> MapLayout(IntPtr layout)
    {
        Dictionary<char, Stroke> keys = new Dictionary<char, Stroke>();
        Dictionary<char, Stroke> deadKeys = new Dictionary<char, Stroke>();
        char[] produced = new char[8];
        char[] discarded = new char[8];
        byte[] nothingPressed = new byte[256];

        // Dalla combinazione più semplice alla più complessa: se un carattere si ottiene in più modi vince la prima.
        for (int modifiers = 0; modifiers < 4; modifiers++)
        {
            bool shift = (modifiers & 1) != 0, altGr = (modifiers & 2) != 0;
            byte[] keyState = new byte[256];
            if (shift) keyState[Native.VK_SHIFT] = Pressed;
            if (altGr) keyState[Native.VK_CONTROL] = keyState[Native.VK_MENU] = Pressed;   // AltGr equivale a Ctrl+Alt

            foreach (int scan in TypingKeys)
            {
                uint vk = Native.MapVirtualKeyEx((uint)scan, Native.MAPVK_VSC_TO_VK, layout);
                if (vk == 0) continue;
                int count = Native.ToUnicodeEx(vk, (uint)scan, keyState, produced, produced.Length,
                    Native.TOUNICODE_KEEP_STATE, layout);
                char c = produced[0];
                if (count < 0)
                {
                    // Tasto morto (ad esempio ^ sulla tastiera tedesca): produce il carattere solo se seguito da
                    // spazio. Questa seconda chiamata lo azzera sui Windows che ignorano TOUNICODE_KEEP_STATE.
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

        // Un tasto morto si usa solo per i caratteri che non hanno un tasto normale.
        foreach (KeyValuePair<char, Stroke> dead in deadKeys)
            if (!keys.ContainsKey(dead.Key)) keys.Add(dead.Key, dead.Value);
        return keys;
    }

    // La tastiera americana, per i server che la usano qualunque sia quella di questo PC.
    static Dictionary<char, Stroke> BuildUsKeys()
    {
        // Per ogni fila: scancode del primo tasto, caratteri senza Maiusc, caratteri con Maiusc.
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

    // Codice con cui comporre il carattere tramite Alt+tastierino (Windows-1252), se ne ha uno.
    static int? AnsiCode(char c)
    {
        try
        {
            byte[] encoded = Ansi.GetBytes(new char[] { c });
            if (encoded.Length == 1 && encoded[0] >= ' ') return encoded[0];
        }
        catch (EncoderFallbackException)
        {
            // Il carattere non esiste in Windows-1252.
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

    // Preme i tasti nell'ordine dato e li rilascia in ordine inverso. Ciò che è stato premuto viene sempre
    // rilasciato, anche se Windows rifiuta un invio: un modificatore rimasto "premuto" falserebbe tutto il resto.
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

    // Tiene premuto Alt e batte il codice sul tastierino numerico. Lo zero iniziale dice a Windows di
    // interpretarlo come Windows-1252, cioè come lo calcola AnsiCode.
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

// Funzioni e costanti di Windows, con i loro nomi ufficiali.
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
    // Il flag di ToUnicodeEx che evita di alterare lo stato della tastiera (da Windows 10 1607 in poi);
    // la documentazione lo descrive ma non gli dà un nome.
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
