using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

[assembly: AssemblyTitle(Program.AppName)]
[assembly: AssemblyDescription("Gestore degli appunti che sa anche digitarli dove l'incolla non funziona")]
[assembly: AssemblyProduct(Program.AppName)]
[assembly: AssemblyVersion("2.0.0.0")]

// A small clipboard manager that can also type the clipboard content as real key presses (scancodes),
// so it works even where paste is disabled (lock screens of remote desktops).
static class Program
{
    public const string AppName = "SmartClip";
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
    // The global hotkeys: registration and texts all start from here. The first types the clipboard at once,
    // the second brings up this window with the list.
    const uint HotkeyModifiers = Native.MOD_CONTROL | Native.MOD_ALT, PickModifiers = Native.MOD_CONTROL;
    const Keys HotkeyKey = Keys.V, PickKey = Keys.D;
    static readonly string HotkeyName = "Ctrl+Alt+" + HotkeyKey, PickName = "Ctrl+" + PickKey;
    const int HotkeyId = 1, PickId = 2;

    const int MinDelay = 1, MaxDelay = 30;             // limits, in seconds, of the wait before typing
    const int DefaultDelay = 1;                        // wait proposed until the user picks another one
    const int ArmedSeconds = 15;                       // time allowed to bring the target window to the foreground
    const int PollMs = 100;                            // how often the foreground window is checked again
    const int KeyDelayMs = 15, SlowKeyDelayMs = 60;    // pause after each key press or release
    const int UnusualLength = 100;                     // a longer text is unlikely to be a password
    const int MaxTitleLength = 45;                     // beyond this, a window title or a list entry is truncated in the status
    const int Edge = 12, ContentWidth = 620;           // margin and usable width of the window, in pixels at 96 dpi
    const int ListExtra = 60;                          // height given to the lists beyond what the options need
    const int SavedWidth = 230, TitleHeight = 20;      // the column of the saved texts, and the title above each column
    const int RowPadding = 10;                         // added to the text height to get a row of the list
    const int PictureRows = 3;                         // rows taken by a picture, to make room for its preview
    const int PreviewMargin = 4;                       // around the preview, inside its row
    const int RowHeight = 24;                          // distance between one option checkbox and the next
    const int Gap = 6;                                 // between neighbouring controls
    const int ActionHeight = 26, ActionWidth = 84;     // the buttons in the bottom row
    const int TypeWidth = 140;                         // the button that types the clipboard, in the same row
    const int StatusHeight = 34;                       // two lines of text
    const Keys NewKey = Keys.N, SaveKey = Keys.S, DeleteKey = Keys.E;   // in the list, all with Ctrl
    const string ButtonText = "Digita gli appunti";
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

    readonly Button typeButton = new Button();
    readonly Button viewButton = new Button();
    readonly Label status = new Label();
    readonly TextBox search = new TextBox();
    readonly ListBox recentList = new ListBox(), savedList = new ListBox();   // what was copied, and what was saved
    ListBox active;     // the one of the two that holds the selected entry
    readonly Button newButton = new Button(), saveButton = new Button(), deleteButton = new Button();
    readonly Control[] optionControls, listControls;   // the two views that take turns in the lower part of the window
    readonly TextBox delayBox = new TextBox();
    readonly CheckBox usBox = new Toggle();
    readonly CheckBox enterBox = new Toggle();
    readonly CheckBox clearBox = new Toggle();
    readonly CheckBox slowBox = new Toggle();
    readonly CheckBox altCodeBox = new Toggle();
    readonly CheckBox hotkeyBox = new Toggle();
    readonly CheckBox pickBox = new Toggle();
    readonly CheckBox historyBox = new Toggle();
    readonly CheckBox trayBox = new Toggle();
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
        Look.Style(typeButton, true);
        foreach (Button button in new Button[] { viewButton, newButton, saveButton, deleteButton }) Look.Style(button, false);

        status.UseMnemonic = false;
        status.ForeColor = Look.Muted;

        Label delayLabel = NewLabel("Attesa prima di digitare (secondi):", Edge + 5);
        delayBox.SetBounds(Edge + ContentWidth - 56, Edge, 56, 26);
        delayBox.TextAlign = HorizontalAlignment.Center;
        delayBox.MaxLength = MaxDelay.ToString().Length;
        Panel delayFrame = Look.Frame(delayBox);
        Delay = DefaultDelay;
        delayBox.KeyPress += delegate(object sender, KeyPressEventArgs e) { e.Handled = !char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar); };
        delayBox.Leave += delegate { Delay = Delay; };   // what was written, brought back within the limits

        // The checkboxes are stacked: adding one takes a single line here, the window height adapts.
        int top = 49;
        PlaceOption(usBox, "Il server usa la tastiera inglese (USA)", ref top);
        PlaceOption(enterBox, "Premi Invio alla fine", ref top);
        PlaceOption(clearBox, "Svuota gli appunti dopo aver digitato", ref top);
        PlaceOption(slowBox, "Digitazione lenta", ref top);
        PlaceOption(altCodeBox, "Alt+codice per i simboli che la tastiera non ha", ref top);
        PlaceOption(hotkeyBox, "Scorciatoia globale " + HotkeyName, ref top);
        hotkeyBox.Checked = true;
        PlaceOption(pickBox, "Scorciatoia " + PickName + " per aprire l'elenco", ref top);
        pickBox.Checked = true;
        PlaceOption(historyBox, "Ricorda gli ultimi testi e immagini copiati", ref top);
        historyBox.Checked = true;
        PlaceOption(trayBox, "Resta attivo nell'area di notifica (tray)", ref top);
        trayBox.Checked = true;

        // Under the options, or the lists that take their place: the status line, then one row of small buttons.
        // On the left those that act on the selected entry, on the right typing and the switch between the views.
        int areaBottom = top + 4 + ListExtra, statusTop = areaBottom + Gap, actionTop = statusTop + StatusHeight + Gap;
        ClientSize = new Size(Edge + ContentWidth + Edge, actionTop + ActionHeight + Edge);
        status.SetBounds(Edge, statusTop, ContentWidth, StatusHeight);
        newButton.Text = "Nuovo…";
        newButton.SetBounds(Edge, actionTop, ActionWidth, ActionHeight);
        saveButton.SetBounds(newButton.Right + Gap, actionTop, ActionWidth, ActionHeight);
        deleteButton.Text = "Elimina";
        deleteButton.SetBounds(saveButton.Right + Gap, actionTop, ActionWidth, ActionHeight);
        viewButton.SetBounds(Edge + ContentWidth - ActionWidth, actionTop, ActionWidth, ActionHeight);
        typeButton.SetBounds(viewButton.Left - Gap - TypeWidth, actionTop, TypeWidth, ActionHeight);

        // The lists take the same area as the options, with the search box on top.
        search.SetBounds(Edge, Edge, ContentWidth, 26);
        Panel searchFrame = Look.Frame(search);

        // Two columns: on the left what was copied, which comes and goes, on the right what was saved.
        int listTop = searchFrame.Bottom + Gap + TitleHeight, listHeight = areaBottom - listTop;
        int savedLeft = Edge + ContentWidth - SavedWidth;
        Label recentTitle = NewLabel("Copiati di recente", searchFrame.Bottom + Gap);
        Label savedTitle = NewLabel("Salvati", searchFrame.Bottom + Gap);
        savedTitle.Left = savedLeft;
        recentTitle.ForeColor = savedTitle.ForeColor = Look.Muted;
        recentList.SetBounds(Edge, listTop, savedLeft - Edge - Edge, listHeight);
        savedList.SetBounds(savedLeft, listTop, SavedWidth, listHeight);

        // The same three actions from the buttons, from the right-click menu and from the keyboard.
        EventHandler create = delegate { Edit(null); };
        EventHandler change = delegate { Edit(Selected); };
        EventHandler delete = delegate { DeleteSelected(); };
        newButton.Click += create;
        saveButton.Click += change;
        deleteButton.Click += delete;
        ContextMenuStrip listMenu = new ContextMenuStrip();
        ToolStripItem changeItem = listMenu.Items.Add("", null, change);
        listMenu.Items.Add(deleteButton.Text, null, delete);
        listMenu.Items.Add(newButton.Text, null, create);
        listMenu.Opening += delegate(object sender, System.ComponentModel.CancelEventArgs e)
        {
            changeItem.Text = saveButton.Text;
            changeItem.Enabled = saveButton.Enabled;
            e.Cancel = Selected == null;
        };
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
                e.ItemHeight = list.ItemHeight * (((Entry)list.Items[e.Index]).Png == null ? 1 : PictureRows);
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
                int index = list.IndexFromPoint(e.Location);
                if (e.Button == MouseButtons.Right && index >= 0) list.SelectedIndex = index;
            };
            list.MouseClick += delegate(object sender, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left && list.IndexFromPoint(e.Location) >= 0) Accept();
            };
        }
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
        Tip(HotkeyName + " digita subito gli appunti nella finestra attiva, senza attesa.\n"
          + "Non funziona se il desktop remoto è a schermo intero e cattura tutte le\n"
          + "scorciatoie: in quel caso usa il pulsante.\n"
          + "Disattivala se entra in conflitto con un altro programma.", hotkeyBox);
        Tip(PickName + " porta in primo piano questa finestra con l'elenco, da qualsiasi programma.\n"
          + "Scelta una voce, il controllo torna alla finestra da cui sei partito: lì la incolli\n"
          + "con Ctrl+V o la digiti con " + HotkeyName + ".\n"
          + "Finché è attiva, " + PickName + " non arriva più agli altri programmi: disattivala\n"
          + "se ti serve lì.", pickBox);
        Tip("L'elenco propone le ultime " + Texts.MaxRecent + " cose copiate mentre il programma è aperto, di cui al\n"
          + "massimo " + Texts.MaxPictures + " immagini.\n"
          + "Restano solo in memoria: non vengono mai scritte su disco e spariscono alla chiusura.\n"
          + "Ciò che i gestori di password segnano come riservato non viene ricordato.\n"
          + "Disattivala se copi password da programmi che non lo fanno: nell'elenco si\n"
          + "leggerebbero in chiaro.", historyBox);
        Tip("Attiva: chiudendo o riducendo a icona la finestra il programma resta in funzione\n"
          + "come icona vicino all'orologio, e la scorciatoia continua a funzionare.\n"
          + "Clic sull'icona per riaprire la finestra, tasto destro e poi Esci per chiudere.\n"
          + "Disattiva: finestra normale, che chiudendola termina il programma.", trayBox);
        Tip("A sinistra le ultime cose copiate, a destra i testi che hai salvato.\n"
          + "Scrivi per cercare in tutte e due: parole in qualsiasi ordine, senza badare a\n"
          + "maiuscole e accenti. Le frecce scorrono la colonna, Tab passa all'altra.\n"
          + "Invio o un clic rimettono la voce negli appunti: poi la incolli con Ctrl+V o la\n"
          + "digiti con " + HotkeyName + ". Il tasto destro apre il menu della voce.\n"
          + "I testi salvati restano nel registro di Windows, cifrati per il tuo utente, e\n"
          + "se ne vede solo il nome.", search, recentList, savedList);
        Tip("Scrive un testo nuovo da tenere, con il suo nome (Ctrl+" + NewKey + ").", newButton);
        Tip("Su una voce copiata: la salva con un nome, e puoi ritoccarne il contenuto.\n"
          + "Su una voce salvata: ne cambia il nome o il contenuto (Ctrl+" + SaveKey + ").\n"
          + "Le immagini restano solo in memoria e non si salvano.", saveButton);
        Tip("Elimina il testo salvato, o toglie dall'elenco la voce copiata (Ctrl+" + DeleteKey + ").", deleteButton);

        optionControls = new Control[] { delayLabel, delayFrame, usBox,
            enterBox, clearBox, slowBox, altCodeBox, hotkeyBox, pickBox, historyBox, trayBox };
        listControls = new Control[] { searchFrame, recentTitle, savedTitle, recentList, savedList,
            newButton, saveButton, deleteButton };
        Controls.AddRange(new Control[] { typeButton, viewButton, status });
        Controls.AddRange(listControls);
        Controls.AddRange(optionControls);
        AcceptButton = typeButton;

        LoadSettings();
        historyBox.CheckedChanged += delegate
        {
            if (!historyBox.Checked) Texts.Recent.Clear();
            Reload();
        };
        Reload();
        ShowView(false);
        ActiveControl = search;

        ContextMenuStrip trayMenu = new ContextMenuStrip();
        trayMenu.Items.Add("Apri", null, delegate { RestoreWindow(); });
        trayMenu.Items.Add("Esci", null, delegate { exiting = true; Close(); });
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
        ResumeLayout(false);
        PerformLayout();
    }

    // The wait before typing, in seconds: whatever is written in its box, kept within the limits.
    int Delay
    {
        get
        {
            int written;
            return int.TryParse(delayBox.Text, out written) ? Math.Max(MinDelay, Math.Min(MaxDelay, written)) : DefaultDelay;
        }
        set { delayBox.Text = Math.Max(MinDelay, Math.Min(MaxDelay, value)).ToString(); }
    }

    // The lower part of the window shows either the list or the options.
    void ShowView(bool options)
    {
        optionsShown = options;
        foreach (Control control in optionControls) control.Visible = options;
        foreach (Control control in listControls) control.Visible = !options;
        viewButton.Text = options ? "Elenco" : "Opzioni";
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
        recentList.BeginUpdate();
        savedList.BeginUpdate();
        recentList.Items.Clear();
        savedList.Items.Clear();
        foreach (Entry entry in entries)
        {
            if (!Texts.Matches(entry, search.Text)) continue;
            ListBox list = entry.Name == null ? recentList : savedList;
            int index = list.Items.Add(entry);
            if (selected != null && entry.Name == selected.Name && entry.Text == selected.Text) list.SelectedIndex = index;
        }
        recentList.EndUpdate();
        savedList.EndUpdate();
        if (Selected == null) Pick(recentList.Items.Count > 0 ? recentList : savedList);
        ShowActions();
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
        saveButton.Text = entry != null && entry.Name != null ? "Modifica…" : "Salva…";
        saveButton.Enabled = entry != null && entry.Png == null;
        deleteButton.Enabled = entry != null;
    }

    // A row of a list: the preview of a picture, if it is one, then the text.
    void DrawEntry(object sender, DrawItemEventArgs e)
    {
        if (e.Index < 0) return;
        ListBox list = (ListBox)sender;
        Entry entry = (Entry)list.Items[e.Index];
        bool selected = (e.State & DrawItemState.Selected) != 0;
        using (Brush back = new SolidBrush(selected ? Look.Selected : list.BackColor))
        {
            e.Graphics.FillRectangle(back, e.Bounds);
        }
        Rectangle label = Rectangle.FromLTRB(e.Bounds.Left + 2 * PreviewMargin, e.Bounds.Top, e.Bounds.Right, e.Bounds.Bottom);
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
        TextRenderer.DrawText(e.Graphics, entry.Label, e.Font, label, list.ForeColor, TextFormatFlags.VerticalCenter
            | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
    }

    void Step(int by)
    {
        if (active == null) return;
        int index = active.SelectedIndex + by;
        if (index >= 0 && index < active.Items.Count) active.SelectedIndex = index;
    }

    void Accept()
    {
        Entry entry = Selected;
        if (entry != null && !busy) Use(entry);
    }

    // Makes an entry the clipboard content again: from there Ctrl+V pastes it and the hotkey types it.
    void Use(Entry entry)
    {
        try
        {
            DataObject data = new DataObject();
            // A saved text may be a password: marked so that Windows clipboard history and other monitors skip it.
            if (entry.Name != null) data.SetData(Texts.PrivateMarker, new MemoryStream(new byte[sizeof(int)]));
            if (entry.Png == null)
            {
                data.SetText(entry.Text);
                Clipboard.SetDataObject(data, true);
            }
            else
            {
                using (MemoryStream png = new MemoryStream(entry.Png))
                using (Image picture = Image.FromStream(png))
                {
                    data.SetImage(picture);
                    Clipboard.SetDataObject(data, true);   // 'true' copies the picture, which can then be released
                }
            }
            ownCopy = Native.GetClipboardSequenceNumber();
        }
        catch (ExternalException ex)   // the clipboard locked by another program
        {
            status.Text = "Errore: " + ex.Message;
            return;
        }
        status.Text = "Negli appunti: " + Shorten(entry.ToString()) + ". Incollalo con Ctrl+V"
            + (hotkeyBox.Checked && entry.Png == null ? " o digitalo con " + HotkeyName : "") + ".";
        GoBack();
    }

    // The list hotkey: shows the window with the list, remembering where the user was.
    void OpenList()
    {
        if (busy) return;
        IntPtr foreground = Native.GetForegroundWindow();
        previous = IsValidTarget(foreground) ? foreground : IntPtr.Zero;
        wasHidden = !Visible;
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
        if (source != null && source.Png != null) return;
        using (EditForm edit = new EditForm(source))
        {
            // Asked again, with what was written still there, if the name turns out to be taken.
            while (edit.ShowDialog(this) == DialogResult.OK)
            {
                string name = edit.EntryName, oldName = source == null ? null : source.Name;
                // Registry value names do not distinguish case.
                bool renamed = !string.Equals(name, oldName, StringComparison.OrdinalIgnoreCase);
                bool taken = renamed && entries.Exists(delegate(Entry entry) { return string.Equals(entry.Name, name, StringComparison.OrdinalIgnoreCase); });
                if (taken && !Ask("«" + name + "» esiste già. Vuoi sostituirlo?")) continue;
                if (!Change(delegate
                {
                    Texts.Save(name, edit.Content);   // first, so that a failure leaves the old one in place
                    if (renamed && oldName != null) Texts.Delete(oldName);
                })) continue;
                if (source != null && source.Name == null) Texts.Recent.Remove(source);   // it now has a line among the saved ones
                status.Text = "Salvato «" + Shorten(name) + "».";
                break;
            }
        }
        Reload();
    }

    void DeleteSelected()
    {
        Entry entry = Selected;
        if (entry == null) return;
        if (entry.Name == null) Texts.Recent.Remove(entry);
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
            case Keys.Control | NewKey:
                Edit(null);
                return true;
            case Keys.Control | SaveKey:
                Edit(Selected);
                return true;
            case Keys.Control | DeleteKey:
                DeleteSelected();
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
        status.Text = "Per digitare una password: copiala, premi «" + ButtonText + "» e clicca nel campo della finestra remota."
            + (hotkeyBox.Checked ? " Oppure premi " + HotkeyName + " direttamente nel campo." : "");
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
        Native.UnregisterHotKey(Handle, PickId);
        base.OnHandleDestroyed(e);
    }

    void UpdateHotkeys()
    {
        if (!IsHandleCreated) return;
        bool typeFailed = !Register(hotkeyBox, HotkeyId, HotkeyModifiers, HotkeyKey);
        bool pickFailed = !Register(pickBox, PickId, PickModifiers, PickKey);
        if (busy) return;
        if (typeFailed) status.Text = HotkeyName + " è già usata da un altro programma: usa il pulsante.";
        else if (pickFailed) status.Text = PickName + " è già usata da un altro programma: apri la finestra dall'icona.";
        else ShowIdle();
    }

    // Registers a hotkey if its checkbox is ticked. If Windows refuses it, unticks the checkbox and returns false.
    bool Register(CheckBox box, int id, uint modifiers, Keys key)
    {
        Native.UnregisterHotKey(Handle, id);
        if (!box.Checked || Native.RegisterHotKey(Handle, id, modifiers | Native.MOD_NOREPEAT, (uint)key))
            return true;
        box.Checked = false;
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
        else if (m.Msg == Native.WM_CLIPBOARDUPDATE)
        {
            ShowClipboardState();
            if (historyBox.Checked && Native.GetClipboardSequenceNumber() != ownCopy) Texts.Remember();
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
        return characters == 1 ? "1 carattere" : characters + " caratteri";
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
        hotkeyBox.Checked = Transfer(key, save, "Hotkey", hotkeyBox.Checked);
        pickBox.Checked = Transfer(key, save, "PickHotkey", pickBox.Checked);
        historyBox.Checked = Transfer(key, save, "History", historyBox.Checked);
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

// What the list offers: a text saved by the user under a name, or something just copied (Name is null),
// which can also be a picture.
class Entry
{
    const int MaxPreview = 120;   // beyond this, the text of a copied entry is truncated in the list
    public readonly string Name, Text;
    const int PreviewHeight = 120;   // of the small copy shown in the list: enough for a sharp row at any scale
    public readonly byte[] Png;      // null for a text
    public readonly Image Preview;   // null for a text

    public Entry(string name, string text)
    {
        Name = name;
        Text = text;
    }

    // A copied picture, kept as PNG because it takes far less memory than the bitmap. Its text describes it.
    public Entry(Image picture)
    {
        using (MemoryStream png = new MemoryStream())
        {
            picture.Save(png, ImageFormat.Png);
            Png = png.ToArray();
        }
        int height = Math.Min(PreviewHeight, picture.Height), width = Math.Max(1, picture.Width * height / picture.Height);
        Preview = new Bitmap(picture, width, height);
        Text = "Immagine " + picture.Width + "×" + picture.Height + ", copiata alle " + DateTime.Now.ToString("HH:mm:ss");
    }

    // What the list shows. Of a saved text only the name: a saved password never appears on screen.
    public string Label
    {
        get
        {
            if (Name != null) return Name;
            if (Png != null) return Text;
            string line = Text.Trim().Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ');
            return line.Length > MaxPreview ? line.Substring(0, MaxPreview) + "…" : line;
        }
    }

    // The sign that tells a saved text and a picture from a copied text.
    public string Mark
    {
        get { return Name != null ? "★" : Png != null ? "▣" : ""; }
    }

    public override string ToString()
    {
        return (Mark + " " + Label).TrimStart();
    }
}

// A checkbox drawn in the colours of the window: the system one cannot be made dark.
class Toggle : CheckBox
{
    const int TextGap = 7;   // between the box and its text, in pixels at 96 dpi

    int Side { get { return Font.Height; } }

    public override Size GetPreferredSize(Size proposed)
    {
        Size text = TextRenderer.MeasureText(Text, Font, Size.Empty, TextFormatFlags.NoPrefix);
        return new Size(Side + TextGap + text.Width, Math.Max(Side, text.Height) + 2);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.Clear(Parent.BackColor);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        Rectangle box = new Rectangle(0, (Height - Side) / 2, Side - 1, Side - 1);
        using (Brush fill = new SolidBrush(Checked ? Look.Accent : Look.Well))
        using (Pen edge = new Pen(Checked ? Look.Accent : Focused ? Look.Ink : Look.Border))
        using (Pen tick = new Pen(Look.Back, Side / 8f))
        {
            g.FillRectangle(fill, box);
            g.DrawRectangle(edge, box);
            float unit = box.Width / 16f;
            if (Checked)
                g.DrawLines(tick, new PointF[]
                {
                    new PointF(box.Left + 4 * unit, box.Top + 8.5f * unit),
                    new PointF(box.Left + 7 * unit, box.Top + 11.5f * unit),
                    new PointF(box.Left + 12.5f * unit, box.Top + 5 * unit)
                });
        }
        Rectangle text = Rectangle.FromLTRB(box.Right + 1 + TextGap, 0, Width, Height);
        TextRenderer.DrawText(g, Text, Font, text, ForeColor,
            TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
    }
}

// Colours and touches shared by the windows: dark, flat, with pastel accents.
static class Look
{
    const int FramePadding = 6;   // between the edge of a framed text box and its text
    public static readonly Color Back = Color.FromArgb(30, 30, 46), Well = Color.FromArgb(24, 24, 37),
        Soft = Color.FromArgb(49, 50, 68), Border = Color.FromArgb(69, 71, 90), Selected = Color.FromArgb(58, 62, 92),
        Ink = Color.FromArgb(205, 214, 244), Muted = Color.FromArgb(166, 173, 200),
        Accent = Color.FromArgb(137, 180, 250);

    // 'primary' is the one button that stands out; the others stay quiet.
    public static void Style(Button button, bool primary)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderColor = primary ? Accent : Border;
        button.BackColor = primary ? Accent : Soft;
        button.ForeColor = primary ? Back : Ink;
    }

    // A place where text is written or listed: a shade darker than the window, with dark scroll bars.
    public static void Field(Control field)
    {
        field.BackColor = Well;
        field.ForeColor = Ink;
        field.HandleCreated += delegate { Native.SetWindowTheme(field.Handle, "DarkMode_Explorer", null); };
    }

    // A text box as a flat darker area. The system border cannot be coloured, so the box goes borderless
    // inside a panel that takes its place; the panel is what gets added to the window.
    public static Panel Frame(TextBox box)
    {
        Panel frame = new Panel();
        frame.Bounds = box.Bounds;
        frame.BackColor = Well;
        box.BorderStyle = BorderStyle.None;
        Field(box);
        Rectangle inside = Rectangle.Inflate(new Rectangle(Point.Empty, frame.Size), -FramePadding, -FramePadding);
        // A single-line box keeps the height of its text: it is centred instead of stretched.
        box.Bounds = box.Multiline ? inside : new Rectangle(inside.Left, (frame.Height - box.Height) / 2, inside.Width, box.Height);
        frame.Controls.Add(box);
        return frame;
    }

    public static void Style(ContextMenuStrip menu)
    {
        menu.Renderer = new ToolStripProfessionalRenderer(new MenuColors());
        menu.ForeColor = Ink;
        menu.ShowImageMargin = false;
    }

    class MenuColors : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground { get { return Soft; } }
        public override Color MenuItemSelected { get { return Selected; } }
        public override Color MenuItemBorder { get { return Selected; } }
        public override Color MenuBorder { get { return Border; } }
    }

    // Title bar in the colour of the window (Windows 11; earlier versions refuse and keep their own).
    public static void Caption(Form form)
    {
        int back = ColorTranslator.ToWin32(form.BackColor), text = ColorTranslator.ToWin32(form.ForeColor), dark = 1;
        Native.DwmSetWindowAttribute(form.Handle, Native.DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, sizeof(int));
        Native.DwmSetWindowAttribute(form.Handle, Native.DWMWA_CAPTION_COLOR, ref back, sizeof(int));
        Native.DwmSetWindowAttribute(form.Handle, Native.DWMWA_TEXT_COLOR, ref text, sizeof(int));
    }
}

// What is behind the list: the last texts and pictures copied, kept only in memory, and the texts saved by
// the user, kept in the registry and encrypted for the current Windows user.
static class Texts
{
    public const int MaxRecent = 30, MaxPictures = 5;   // pictures are heavy: only a few of the recent ones
    const int MaxLength = 2000;   // a longer text is not something to type key by key
    const string SavedKey = MainForm.SettingsKey + @"\Saved";
    // Clipboard formats with which password managers and private browser windows mark what must not be
    // recorded: the first two by merely being there, the others by holding a zero.
    public const string PrivateMarker = "ExcludeClipboardContentFromMonitorProcessing";
    static readonly string[] PrivateIfPresent = { "Clipboard Viewer Ignore", PrivateMarker };
    static readonly string[] PrivateIfZero = { "CanIncludeInClipboardHistory", "CanUploadToCloudClipboard" };
    static readonly CompareInfo Comparer = CultureInfo.InvariantCulture.CompareInfo;
    const CompareOptions Loose = CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;

    public static readonly List<Entry> Recent = new List<Entry>();   // newest first

    // Called at every clipboard change.
    public static void Remember()
    {
        try
        {
            IDataObject data = Clipboard.GetDataObject();
            if (data == null || IsPrivate(data)) return;
            // Programs that copy both (a spreadsheet, a word processor) mean the text.
            string text = data.GetData(DataFormats.UnicodeText) as string;
            if (text != null) Add(text);
            else
            {
                using (Image picture = data.GetData(DataFormats.Bitmap) as Image)
                {
                    if (picture != null) Add(picture);
                }
            }
        }
        catch (ExternalException)
        {
            // Clipboard held by another program, or a picture that cannot be read: this copy is not remembered.
        }
    }

    public static void Add(string text)
    {
        if (text == null || text.Trim().Length == 0 || text.Length > MaxLength) return;
        Forget(text);   // copied again: it moves to the top instead of appearing twice
        Insert(new Entry(null, text));
    }

    public static void Add(Image picture)
    {
        Entry entry = new Entry(picture);
        // The same picture again, also because Windows announces one copy several times.
        Recent.RemoveAll(delegate(Entry other) { return other.Png != null && other.Png.SequenceEqual(entry.Png); });
        Insert(entry);
    }

    static void Insert(Entry entry)
    {
        Recent.Insert(0, entry);
        if (Recent.FindAll(IsPicture).Count > MaxPictures) Recent.RemoveAt(Recent.FindLastIndex(IsPicture));
        if (Recent.Count > MaxRecent) Recent.RemoveAt(MaxRecent);
    }

    static bool IsPicture(Entry entry)
    {
        return entry.Png != null;
    }

    public static void Forget(string text)
    {
        Recent.RemoveAll(delegate(Entry entry) { return entry.Text == text; });
    }

    public static bool IsPrivate(IDataObject data)
    {
        foreach (string format in PrivateIfPresent)
            if (data.GetDataPresent(format)) return true;
        foreach (string format in PrivateIfZero)
        {
            if (!data.GetDataPresent(format)) continue;
            // The format is there: a zero, or a value that cannot be read, both mean "no".
            MemoryStream value = data.GetData(format) as MemoryStream;
            if (value == null || value.Length < sizeof(int) || BitConverter.ToInt32(value.ToArray(), 0) == 0) return true;
        }
        return false;
    }

    // Everything the list can offer: the saved texts by name, then the copied ones from the newest.
    public static List<Entry> Load()
    {
        List<Entry> entries = new List<Entry>();
        using (RegistryKey key = Registry.CurrentUser.OpenSubKey(SavedKey))
        {
            string[] names = key == null ? new string[0] : key.GetValueNames();
            Array.Sort(names, StringComparer.CurrentCultureIgnoreCase);
            foreach (string name in names)
            {
                byte[] data = key.GetValue(name) as byte[];
                if (data == null) continue;
                try
                {
                    byte[] plain = ProtectedData.Unprotect(data, null, DataProtectionScope.CurrentUser);
                    entries.Add(new Entry(name, Encoding.UTF8.GetString(plain)));
                }
                catch (CryptographicException)
                {
                    // Saved by another user or on another PC: it cannot be read here.
                }
            }
        }
        entries.AddRange(Recent);
        return entries;
    }

    public static void Save(string name, string text)
    {
        using (RegistryKey key = Registry.CurrentUser.CreateSubKey(SavedKey))
        {
            key.SetValue(name, ProtectedData.Protect(Encoding.UTF8.GetBytes(text), null, DataProtectionScope.CurrentUser));
        }
    }

    public static void Delete(string name)
    {
        using (RegistryKey key = Registry.CurrentUser.CreateSubKey(SavedKey))
        {
            key.DeleteValue(name, false);
        }
    }

    // Every word of the query must appear somewhere, in any order; case and accents do not count.
    public static bool Matches(Entry entry, string query)
    {
        string where = entry.Name + "\n" + entry.Text;
        foreach (string word in query.Split((char[])null, StringSplitOptions.RemoveEmptyEntries))
            if (Comparer.IndexOf(where, word, Loose) < 0) return false;
        return true;
    }
}

// The window where a saved text gets its name and its content.
class EditForm : Form
{
    const int Edge = 12, FieldWidth = 420, ContentHeight = 190;   // in pixels at 96 dpi
    const int ButtonWidth = 90, ButtonHeight = 26, Gap = 6;
    const int MaxNameLength = 60;
    readonly TextBox nameBox = new TextBox(), contentBox = new TextBox();
    readonly Button saveButton = new Button(), cancelButton = new Button();

    public EditForm(Entry source)
    {
        SuspendLayout();
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = SystemFonts.MessageBoxFont;
        Text = source == null ? "Nuovo testo" : source.Name == null ? "Salva il testo copiato" : "Modifica il testo salvato";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        TopMost = true;
        BackColor = Look.Back;
        ForeColor = Look.Ink;

        Label nameLabel = NewLabel("Nome (è ciò che si vede nell'elenco):", Edge);
        nameBox.MaxLength = MaxNameLength;
        nameBox.SetBounds(Edge, 32, FieldWidth, 26);
        Label contentLabel = NewLabel("Contenuto:", 69);
        contentBox.Multiline = true;
        contentBox.AcceptsReturn = true;
        contentBox.ScrollBars = ScrollBars.Vertical;
        contentBox.SetBounds(Edge, 89, FieldWidth, ContentHeight);
        Panel nameFrame = Look.Frame(nameBox), contentFrame = Look.Frame(contentBox);

        int buttonTop = contentFrame.Bottom + Edge;
        cancelButton.Text = "Annulla";
        cancelButton.DialogResult = DialogResult.Cancel;
        cancelButton.SetBounds(Edge + FieldWidth - ButtonWidth, buttonTop, ButtonWidth, ButtonHeight);
        saveButton.Text = "Salva";
        saveButton.DialogResult = DialogResult.OK;
        saveButton.SetBounds(cancelButton.Left - Gap - ButtonWidth, buttonTop, ButtonWidth, ButtonHeight);
        ClientSize = new Size(Edge + FieldWidth + Edge, buttonTop + ButtonHeight + Edge);
        AcceptButton = saveButton;
        CancelButton = cancelButton;
        Look.Style(saveButton, true);
        Look.Style(cancelButton, false);

        if (source != null)
        {
            nameBox.Text = source.Name ?? "";
            // The box breaks lines only at CR LF, while a copied text may hold bare LF.
            contentBox.Text = source.Text.Replace("\r\n", "\n").Replace("\n", "\r\n");
        }
        // Nothing to save until both are written.
        EventHandler check = delegate { saveButton.Enabled = EntryName.Length > 0 && Content.Trim().Length > 0; };
        nameBox.TextChanged += check;
        contentBox.TextChanged += check;
        check(this, EventArgs.Empty);

        Controls.AddRange(new Control[] { nameLabel, nameFrame, contentLabel, contentFrame, saveButton, cancelButton });
        ResumeLayout(false);
        PerformLayout();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        Look.Caption(this);
    }

    public string EntryName { get { return nameBox.Text.Trim(); } }
    public string Content { get { return contentBox.Text; } }

    static Label NewLabel(string text, int y)
    {
        Label label = new Label();
        label.Text = text;
        label.AutoSize = true;
        label.Location = new Point(Edge, y);
        return label;
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
    public const int WM_HOTKEY = 0x0312, WM_CLIPBOARDUPDATE = 0x031D, EM_SETCUEBANNER = 0x1501;
    public const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20, DWMWA_CAPTION_COLOR = 35, DWMWA_TEXT_COLOR = 36;
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

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr window);

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
    public static extern uint GetClipboardSequenceNumber();

    [DllImport("user32.dll")]
    public static extern bool DestroyIcon(IntPtr icon);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr SendMessage(IntPtr window, int message, IntPtr wParam, string lParam);

    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
    public static extern int SetWindowTheme(IntPtr window, string appName, string idList);

    [DllImport("dwmapi.dll")]
    public static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int size);
}
