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
    // with a tab a key and what it does. Names, keys and numbers come from where the program defines them;
    // it is written anew each time the guide opens, because the user can change the keys of the hotkeys.
    readonly string[] Lines =
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
        "- Fermando il puntatore su una voce copiata che nella riga non ci sta tutta, dopo un attimo compare "
            + "per intero in un riquadro: il testo con tutte le sue righe, l'immagine più grande. Sparisce "
            + "appena sposti il puntatore.",
        "- La stellina all'estremità destra di una voce copiata (Ctrl+" + MainForm.PinKey + ") la tiene in cima "
            + "alla colonna: compare sbiadita quando ci passi sopra, e un clic la accende o la spegne. Una voce "
            + "con la stellina non scade con i giorni della cronologia e resta lì finché non la togli; copiare "
            + "di nuovo lo stesso testo non la sposta.",
        "Il segno colorato a sinistra dice che cos'è la voce: un testo, un indirizzo web, un testo salvato. Le "
            + "immagini mostrano una miniatura. A destra c'è l'ora della copia (il giorno, se non è di oggi) o il "
            + "giorno del salvataggio.",
        "Per non ricordare quello che stai per copiare, «Sospendi la cattura» sta nel menu del tasto destro "
            + "dell'elenco e in quello dell'icona vicino all'orologio: sopra la colonna compare «in pausa» finché "
            + "non scegli «Riprendi la cattura». Al prossimo avvio la cattura è di nuovo attiva.",
        "Lo spazio tra le due colonne si trascina, per dare più o meno posto ai testi salvati: la larghezza "
            + "scelta resta anche al prossimo avvio, e un doppio clic lì torna a quella di partenza.",
        "La colonna dei copiati mostra le " + MainForm.MaxShown + " voci più recenti: le altre si trovano con la ricerca.",

        "# I testi salvati",
        "Sono le cose che vuoi avere sempre a portata di mano: una firma, un indirizzo, una password. Ognuno ha "
            + "un nome, ed è solo il nome che si vede nell'elenco.",
        "- «Nuovo» (Ctrl+" + MainForm.NewKey + ") scrive un testo da zero.",
        "- «Salva» (Ctrl+" + MainForm.SaveKey + "), su una voce copiata, la tiene con un nome; prima di salvare "
            + "puoi ritoccarne il contenuto.",
        "- «Modifica» (Ctrl+" + MainForm.SaveKey + "), su una voce salvata, ne cambia il nome o il contenuto.",
        "- «Elimina» (Ctrl+" + MainForm.DeleteKey + ") cancella il testo salvato, oppure toglie dall'elenco la voce copiata.",
        "Quando diventano tanti puoi raccoglierli in cartelle, e le cartelle in altre cartelle:",
        "- «Nuova cartella» sta nel menu del tasto destro (Ctrl+Maiusc+" + MainForm.NewKey + "): nasce dentro la "
            + "cartella selezionata, o quella del testo selezionato, ma nella sua finestra puoi scegliere dove metterla.",
        "- Un clic sul nome di una cartella, o Invio, la apre e la chiude; il numero a destra dice quanti testi "
            + "contiene, anche nelle cartelle al suo interno. Aperte e chiuse, le ritrovi come le hai lasciate.",
        "- Un testo salvato, o una cartella con tutto quello che contiene, si trascina dentro un'altra cartella: "
            + "sulla cartella stessa o su uno dei suoi testi. Lasciato su un testo fuori dalle cartelle, o sotto "
            + "l'ultima riga, esce da quella in cui era.",
        "- «Sposta in», nel menu del tasto destro di un testo salvato o di una cartella, fa lo stesso. La stessa "
            + "scelta c'è nella finestra in cui lo scrivi, a sinistra dei pulsanti.",
        "- Con una cartella selezionata, «Rinomina» le cambia il nome o il posto ed «Elimina» toglie la cartella "
            + "soltanto: quello che conteneva, testi e cartelle, sale nella cartella che la conteneva, o resta "
            + "tra i salvati fuori dalle cartelle.",
        "- Nel nome di una cartella non può esserci «" + Texts.Separator + "».",
        "- I testi salvati sono anche nel menu dell'icona vicino all'orologio, con le stesse cartelle: "
            + "sceglierne uno lo rimette negli appunti senza aprire la finestra.",
        "- La ricerca trova anche dentro le cartelle chiuse, e il nome di una cartella trova i suoi testi.",
        "- «Mostra il contenuto», nel menu del tasto destro di un testo salvato, lo fa vedere in un riquadro "
            + "per qualche secondo: l'elenco ne mostra solo il nome, e passarci sopra con il puntatore non basta.",
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
        "Ctrl+Maiusc+" + MainForm.NewKey + "\tNuova cartella.",
        "Ctrl+" + MainForm.SaveKey + "\tSalva la voce copiata, o modifica quella salvata.",
        "Ctrl+" + MainForm.DeleteKey + "\tElimina la voce selezionata.",
        "Ctrl+" + MainForm.PinKey + "\tTiene in alto la voce copiata, o la lascia andare.",
        "Le prime due funzionano ovunque; le altre valgono nell'elenco. Finché è attiva, " + MainForm.PickName
            + " non arriva più agli altri programmi.",
        "Nelle opzioni, sotto «Scorciatoie», le prime due si spengono e si cambiano: il pulsante accanto a "
            + "ciascuna mostra i suoi tasti, e premendolo aspetta quelli nuovi.",
        "- Servono Ctrl o Alt, anche con Maiusc, insieme a una lettera, una cifra o un tasto funzione.",
        "- Esc lascia i tasti di adesso, Canc torna a quelli di partenza.",
        "- Con Ctrl+Alt evita le lettere che con AltGr scrivono un simbolo, come la E dell'euro: la "
            + "scorciatoia scatterebbe mentre lo scrivi.",

        "# La cronologia",
        "Quello che copi resta nell'elenco per un numero di giorni che decidi tu (" + Texts.DefaultKeepDays
            + " se non lo cambi), anche dopo aver chiuso il programma o spento il PC.",
        "- Sta in " + Texts.Folder + ", un file per giorno, scritto in chiaro. I giorni scaduti si eliminano da soli.",
        "- Con 0 giorni niente va su disco: restano in memoria le ultime " + Texts.MaxRecent + " cose copiate, di "
            + "cui al massimo " + Texts.MaxPictures + " immagini, e spariscono alla chiusura.",
        "- Quello che i gestori di password segnano come riservato non viene ricordato.",
        "- Le voci con la stellina restano anche oltre quei giorni.",
        "- Nelle opzioni, «Pulisci ora» elimina subito quello che hai copiato prima degli ultimi giorni che "
            + "scrivi accanto: con 1 resta solo oggi, con 0 non resta niente. Chiede conferma, e non tocca le "
            + "voci con la stellina né i testi salvati.",
        "- Spegnendo la cronologia nelle opzioni si svuotano l'elenco e i file su disco, stelline comprese.",

        "# Le opzioni",
        "Il pulsante «Opzioni» le mostra al posto dell'elenco, e «Elenco» torna indietro. Fermando il puntatore "
            + "su un'opzione ne compare la spiegazione completa.",
        "- Digitazione: quanti secondi aspettare prima di digitare (da " + MainForm.MinDelay + " a "
            + MainForm.MaxDelay + "), se il server usa la tastiera americana, se premere Invio alla fine, se "
            + "svuotare gli appunti dopo, la digitazione lenta e i simboli composti con Alt+codice.",
        "- Scorciatoie: accende e spegne " + MainForm.HotkeyName + " e " + MainForm.PickName + ", e dà loro altri tasti.",
        "- Cronologia: se ricordare ciò che copi, per quanti giorni, il collegamento alla cartella dei file e "
            + "il pulsante che la pulisce subito.",
        "- Programma: se restare attivo nell'area di notifica, se partire insieme a Windows, e i pulsanti per "
            + "esportare e importare.",

        "# Esportare e importare",
        "Nelle opzioni, sotto «Programma», due pulsanti portano i tuoi testi fuori dal programma e li riprendono, "
            + "per una copia di sicurezza o per passarli a un altro PC. Ognuno chiede se vuoi solo i testi salvati "
            + "o tutto, cioè anche le cose copiate.",
        "- «Esporta» scrive un file di testo che si apre con il Blocco note: i testi salvati con le loro "
            + "cartelle e, se lo chiedi, le cose copiate con l'ora e la stellina. Le immagini non ci vanno.",
        "- Nel file i testi sono in chiaro, password comprese: tienilo in un posto sicuro.",
        "- «Importa» legge un file così e aggiunge ciò che contiene a quello che c'è già. Se un testo salvato "
            + "ha il nome di uno esistente chiede quale tenere; le cose copiate che l'elenco ha già non si ripetono.",
        "- Il file si può ritoccare a mano: il testo di ogni voce sta nelle righe che iniziano con una "
            + "tabulazione, che va lasciata al suo posto.",

        "# L'icona vicino all'orologio",
        "Con l'opzione attiva, chiudere o ridurre a icona la finestra la nasconde soltanto: il programma resta "
            + "in funzione e le scorciatoie continuano a rispondere.",
        "- Un clic sull'icona riapre la finestra; tasto destro e poi «Esci» chiude davvero.",
        "- Con «Nascondi passando a un altro programma» la finestra torna qui anche quando clicchi altrove; "
            + "spenta, resta aperta dietro le altre. Mentre aspetta e digita, invece, sta sopra a tutto: "
            + "il conto alla rovescia e «Annulla» restano in vista.",
        "- Nel menu del tasto destro ci sono anche i testi salvati, ogni cartella un sottomenu: sceglierne uno "
            + "lo rimette negli appunti, pronto per Ctrl+V o per " + MainForm.HotkeyName + ", senza aprire la finestra.",
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
        "- Una scorciatoia si spegne da sola: la sta già usando un altro programma. Dalle altri tasti nelle opzioni.",
        "La riga sopra i pulsanti dice che cosa è appena successo, e perché. Le conferme spariscono dopo "
            + "qualche secondo e lasciano il posto all'elenco; avvisi ed errori restano fino al messaggio dopo."
    };

    readonly RichTextBox text = new RichTextBox();
    readonly Button closeButton = new RoundButton();

    public HelpForm()
    {
        SuspendLayout();
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = Look.TextFont;
        Text = "Guida di " + Program.AppName;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ShowIcon = false;
        StartPosition = FormStartPosition.Manual;   // placed over its owner when it loads; it stays above it
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
        float scale = CurrentAutoScaleDimensions.Width / 96f;   // the sizes above are for 96 dpi
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
