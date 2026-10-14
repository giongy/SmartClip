using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;

// The options: how they are placed and explained, what their buttons do, and the settings in the registry.
partial class MainForm
{
    // The options are stacked by category, in two columns: on the left what concerns typing, on the right
    // the rest. Adding one takes a single line here, the window height adapts. Returns where they end.
    int PlaceOptions()
    {
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
        PlaceOption(leaveBox, "Nascondi passando a un altro programma", left, ref top);
        leaveBox.Checked = true;
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
          + "Disattiva: finestra normale, che chiudendola termina il programma.", trayBox);        Tip("Avvia " + Program.AppName + " quando accedi a Windows, già nascosto nell'area di notifica\n"
          + "(ridotto a icona, se l'opzione qui sopra è spenta).\n"
          + "Vale per questo eseguibile: se lo sposti, al primo avvio da lì la voce si aggiorna da sola.\n"
          + "È la stessa voce di Gestione attività > App di avvio: spenta lì, risulta spenta anche qui.", startBox);
        Tip("Scrive in un file di testo, che si apre con il Blocco note, i testi salvati con le loro\n"
          + "cartelle; se lo chiedi, anche le cose copiate. Le immagini non ci vanno.\n"
          + "Nel file i testi sono in chiaro, password comprese: tienilo in un posto sicuro.", exportButton);
        Tip("Legge un file scritto da «Esporta», anche su un altro PC, e aggiunge ciò che contiene\n"
          + "a quello che c'è già. Se un testo salvato ha il nome di uno esistente, chiede quale tenere.\n"
          + "Le cose copiate che l'elenco ha già non vengono ripetute.", importButton);
        return top;
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
        leaveBox.Checked = Transfer(key, save, "HideOnLeave", leaveBox.Checked);
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
