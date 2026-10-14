using System;
using System.Windows.Forms;

// The two global hotkeys: their names, the new keys the user gives them, their registration with Windows.
partial class MainForm
{
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
}
