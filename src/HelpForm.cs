using System;
using System.Drawing;
using System.Windows.Forms;

// The window with the guide: what the program does, one section per subject. It stays open beside the main
// window, so that it can be read while trying things out.
class HelpForm : Form
{
    const int Edge = 12, TextWidth = 560, TextHeight = 470;   // in pixels at 96 dpi
    const int ButtonWidth = 90, ButtonHeight = 26;
    const int TextPadding = 10;                // between the rounded edge and the text
    const int BulletIndent = 14, KeyColumn = 120;   // where the text of a point starts, and that of a key
    const float HeadingGrowth = 1.15f;         // a heading is this much larger than the text
    const string Heading = "# ", Bullet = "- ";    // how a line of the guide says what it is
    const char KeyEnd = '\t';                       // in a line that explains a key, between the key and what it does

    // The guide itself. A line starting with '#' is a heading, one starting with '-' a point of a list, one
    // with a tab a key and what it does. Names, keys and numbers come from where the program defines them.
    static readonly string[] Lines =
    {
        "# A cosa serve",
        Program.AppName + " tiene da parte quello che copi e i testi che scegli di salvare, per riaverli negli "
            + "appunti quando servono. Sa anche digitare gli appunti come pressioni di tasti veri: serve per "
            + "inserire una password dove incollare non funziona, ad esempio nelle schermate di blocco dei "
            + "desktop remoti.",

        "# L'elenco",
        "A sinistra ci sono le cose copiate di recente, testi e immagini, dalla più nuova. A destra i testi "
            + "salvati, che restano finché non li elimini.",
        "- Scrivi nella casella in alto per cercare in tutte e due le colonne: le parole valgono in qualsiasi "
            + "ordine, senza badare a maiuscole e accenti.",
        "- Un clic mette la voce negli appunti e lascia la finestra aperta, così puoi ancora salvarla o eliminarla.",
        "- Invio o un doppio clic la mettono negli appunti e, se avevi aperto l'elenco con " + MainForm.PickName
            + ", ti riportano alla finestra in cui stavi lavorando.",
        "- A quel punto la incolli con Ctrl+V, oppure la digiti con " + MainForm.HotkeyName + ".",
        "- Il tasto destro seleziona la voce e apre il suo menu, senza toccare gli appunti.",
        "- La stellina all'estremità destra di una voce copiata (Ctrl+" + MainForm.PinKey + ") la tiene in cima "
            + "alla colonna: compare sbiadita quando ci passi sopra, e un clic la accende o la spegne. Una voce "
            + "con la stellina non scade con i giorni della cronologia e resta lì finché non la togli; copiare "
            + "di nuovo lo stesso testo non la sposta.",
        "Il segno colorato a sinistra dice che cos'è la voce: un testo, un indirizzo web, un testo salvato. Le "
            + "immagini mostrano una miniatura. A destra c'è l'ora della copia (il giorno, se non è di oggi) o il "
            + "giorno del salvataggio.",
        "La colonna dei copiati mostra le " + MainForm.MaxShown + " voci più recenti: le altre si trovano con la ricerca.",

        "# I testi salvati",
        "Sono le cose che vuoi avere sempre a portata di mano: una firma, un indirizzo, una password. Ognuno ha "
            + "un nome, ed è solo il nome che si vede nell'elenco.",
        "- «Nuovo» (Ctrl+" + MainForm.NewKey + ") scrive un testo da zero.",
        "- «Salva» (Ctrl+" + MainForm.SaveKey + "), su una voce copiata, la tiene con un nome; prima di salvare "
            + "puoi ritoccarne il contenuto.",
        "- «Modifica» (Ctrl+" + MainForm.SaveKey + "), su una voce salvata, ne cambia il nome o il contenuto.",
        "- «Elimina» (Ctrl+" + MainForm.DeleteKey + ") cancella il testo salvato, oppure toglie dall'elenco la voce copiata.",
        "I testi salvati stanno nel registro di Windows, cifrati per il tuo utente: un altro utente dello stesso "
            + "PC non li può leggere. Quando ne rimetti uno negli appunti viene segnato come riservato, perché "
            + "la cronologia di Windows (Win+V) e gli altri gestori di appunti lo ignorino. Le immagini non si "
            + "possono salvare.",

        "# Digitare una password",
        "Dove Ctrl+V non funziona, " + Program.AppName + " scrive il testo degli appunti un tasto alla volta, "
            + "come faresti tu sulla tastiera.",
        "- Copia la password.",
        "- Premi «" + MainForm.ButtonText + "».",
        "- Clicca nel campo della finestra remota: hai " + MainForm.ArmedSeconds + " secondi per farlo.",
        "- Quando quella finestra è rimasta in primo piano per il tempo dell'attesa, parte la digitazione.",
        "In alternativa premi " + MainForm.HotkeyName + " direttamente nel campo: digita subito, senza attesa.",
        "Per non mandare una password nel posto sbagliato:",
        "- la digitazione si ferma da sola se la finestra di destinazione perde il primo piano;",
        "- durante l'attesa e la digitazione lo stesso pulsante diventa «Annulla»;",
        "- desktop, barra delle applicazioni e menu Start non sono mai una destinazione;",
        "- se gli appunti contengono più di " + MainForm.UnusualLength + " caratteri o più righe, viene chiesta una conferma;",
        "- con Bloc Maiusc attivo non si digita: va spento prima.",

        "# I tasti",
        MainForm.PickName + "\tApre l'elenco, da qualsiasi programma.",
        MainForm.HotkeyName + "\tDigita subito gli appunti nella finestra attiva.",
        "F1\tApre questa guida.",
        "Su / Giù\tScorre la colonna.",
        "Tab\tPassa all'altra colonna.",
        "Invio\tSceglie la voce e torna alla finestra di partenza.",
        "Esc\tTorna alla finestra di partenza senza scegliere.",
        "Ctrl+" + MainForm.NewKey + "\tNuovo testo salvato.",
        "Ctrl+" + MainForm.SaveKey + "\tSalva la voce copiata, o modifica quella salvata.",
        "Ctrl+" + MainForm.DeleteKey + "\tElimina la voce selezionata.",
        "Ctrl+" + MainForm.PinKey + "\tTiene in alto la voce copiata, o la lascia andare.",
        "Le prime due funzionano ovunque, e si possono spegnere nelle opzioni; le altre valgono nell'elenco. Finché "
            + "è attiva, " + MainForm.PickName + " non arriva più agli altri programmi.",

        "# La cronologia",
        "Quello che copi resta nell'elenco per un numero di giorni che decidi tu (" + Texts.DefaultKeepDays
            + " se non lo cambi), anche dopo aver chiuso il programma o spento il PC.",
        "- Sta in " + Texts.Folder + ", un file per giorno, scritto in chiaro. I giorni scaduti si eliminano da soli.",
        "- Con 0 giorni niente va su disco: restano in memoria le ultime " + Texts.MaxRecent + " cose copiate, di "
            + "cui al massimo " + Texts.MaxPictures + " immagini, e spariscono alla chiusura.",
        "- Quello che i gestori di password segnano come riservato non viene ricordato.",
        "- Le voci con la stellina restano anche oltre quei giorni.",
        "- Spegnendo la cronologia nelle opzioni si svuotano l'elenco e i file su disco, stelline comprese.",

        "# Le opzioni",
        "Il pulsante «Opzioni» le mostra al posto dell'elenco, e «Elenco» torna indietro. Fermando il puntatore "
            + "su un'opzione ne compare la spiegazione completa.",
        "- Digitazione: quanti secondi aspettare prima di digitare (da " + MainForm.MinDelay + " a "
            + MainForm.MaxDelay + "), se il server usa la tastiera americana, se premere Invio alla fine, se "
            + "svuotare gli appunti dopo, la digitazione lenta e i simboli composti con Alt+codice.",
        "- Scorciatoie: accende e spegne " + MainForm.HotkeyName + " e " + MainForm.PickName + ".",
        "- Cronologia: se ricordare ciò che copi, per quanti giorni, e il collegamento alla cartella dei file.",
        "- Programma: se restare attivo nell'area di notifica e se partire insieme a Windows.",

        "# L'icona vicino all'orologio",
        "Con l'opzione attiva, chiudere o ridurre a icona la finestra la nasconde soltanto: il programma resta "
            + "in funzione e le scorciatoie continuano a rispondere.",
        "- Un clic sull'icona riapre la finestra; tasto destro e poi «Esci» chiude davvero.",
        "- L'icona è a colori se negli appunti c'è del testo, grigia se non c'è.",
        "- Avviare il programma una seconda volta fa ricomparire la finestra di quello già aperto.",

        "# Se qualcosa non va",
        "- I simboli arrivano sbagliati: il server usa un'altra tastiera. Prova l'opzione della tastiera inglese (USA).",
        "- Mancano caratteri, o arrivano in disordine: attiva la digitazione lenta.",
        "- Non arrivano i simboli che la tua tastiera non ha (~ e ` su quella italiana): attiva Alt+codice, che "
            + "funziona se il server è Windows.",
        "- " + MainForm.HotkeyName + " non fa niente con il desktop remoto a schermo intero: quel programma "
            + "cattura tutte le scorciatoie. Usa il pulsante.",
        "- Windows blocca l'invio dei tasti: il programma di desktop remoto è avviato come amministratore. Avvia "
            + "così anche " + Program.AppName + ".",
        "- Una scorciatoia si spegne da sola: la sta già usando un altro programma.",
        "La riga sopra i pulsanti dice sempre che cosa è appena successo, e perché."
    };

    readonly RichTextBox text = new RichTextBox();
    readonly Button closeButton = new RoundButton();

    public HelpForm()
    {
        SuspendLayout();
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = SystemFonts.MessageBoxFont;
        Text = "Guida di " + Program.AppName;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ShowIcon = false;
        StartPosition = FormStartPosition.Manual;   // placed over its owner when it loads
        TopMost = true;
        BackColor = Look.Back;
        ForeColor = Look.Ink;

        text.ReadOnly = true;
        text.BorderStyle = BorderStyle.None;
        text.ScrollBars = RichTextBoxScrollBars.Vertical;
        text.DetectUrls = false;
        text.TabStop = false;
        text.SetBounds(Edge, Edge, TextWidth, TextHeight);
        Look.Field(text);
        Panel card = Look.Wrap(text, TextPadding);

        int buttonTop = card.Bottom + Edge;
        closeButton.Text = "Chiudi";
        closeButton.SetBounds(Edge + TextWidth - ButtonWidth, buttonTop, ButtonWidth, ButtonHeight);
        closeButton.Click += delegate { Close(); };
        Look.Style(closeButton, true);
        ClientSize = new Size(Edge + TextWidth + Edge, buttonTop + ButtonHeight + Edge);
        AcceptButton = closeButton;
        CancelButton = closeButton;   // Esc closes too

        card.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        closeButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        Controls.AddRange(new Control[] { card, closeButton });
        ActiveControl = closeButton;
        ResumeLayout(false);
        PerformLayout();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        Look.Caption(this);
    }

    // It can be made larger, never smaller than it starts, and opens over the window it belongs to without
    // leaving the screen.
    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        MinimumSize = Size;
        Write();
        if (Owner == null) return;
        Rectangle screen = Screen.FromControl(Owner).WorkingArea;
        int x = Owner.Left + (Owner.Width - Width) / 2, y = Owner.Top + (Owner.Height - Height) / 2;
        Location = new Point(Math.Max(screen.Left, Math.Min(x, screen.Right - Width)),
            Math.Max(screen.Top, Math.Min(y, screen.Bottom - Height)));
    }

    // Fills the box from 'Lines', each with the look of what it is. Written once the window has its scale.
    void Write()
    {
        float scale = CurrentAutoScaleDimensions.Width / AutoScaleDimensions.Width;
        int bullet = (int)(BulletIndent * scale), key = (int)(KeyColumn * scale);
        using (Font heading = new Font(Font.FontFamily, Font.Size * HeadingGrowth, FontStyle.Bold))
        using (Font strong = new Font(Font, FontStyle.Bold))
        {
            foreach (string line in Lines)
            {
                if (line.StartsWith(Heading))
                {
                    if (text.TextLength > 0) Add("", Font, 0, 0);   // air above a section
                    Add(line.Substring(Heading.Length), heading, 0, 0);
                }
                else if (line.StartsWith(Bullet))
                {
                    Add("•\t" + line.Substring(Bullet.Length), Font, bullet, bullet);
                }
                else if (line.IndexOf(KeyEnd) >= 0)
                {
                    // The key in bold, what it does in a column of its own.
                    int end = line.IndexOf(KeyEnd);
                    Add(line.Substring(0, end + 1), strong, key, key, false);
                    Add(line.Substring(end + 1), Font, key, key);
                }
                else
                {
                    Add(line, Font, 0, 0);
                }
            }
        }
        text.Select(0, 0);
    }

    void Add(string words, Font font, int tab, int hanging)
    {
        Add(words, font, tab, hanging, true);
    }

    // Appends some text in the given font. 'hanging' is where the lines after the first start, 'tab' where
    // the text after a tab does; 'ends' closes the paragraph.
    void Add(string words, Font font, int tab, int hanging, bool ends)
    {
        text.Select(text.TextLength, 0);
        text.SelectionFont = font;
        text.SelectionColor = Look.Ink;
        text.SelectionIndent = 0;
        text.SelectionHangingIndent = hanging;
        text.SelectionTabs = tab > 0 ? new int[] { tab } : new int[0];
        text.SelectedText = words + (ends ? "\n" : "");
    }
}
