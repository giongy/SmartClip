using System.Windows.Forms;

// The texts in Italian: the language the program was written in.
static partial class Strings
{
    static void Italian()
    {
        ShiftName = "Maiusc"; DeleteKeyName = "Canc"; EnterKeyName = "Invio"; ArrowsName = "Su / Giù";
        DayFormat = "dd/MM"; YearDayFormat = "dd/MM/yy";
        Quote = "«{0}»";

        TypeButton = "Digita gli appunti";
        SavedTitle = "Salvati"; PausedSuffix = " (in pausa)";
        New = "Nuovo…"; SaveDots = "Salva…"; EditDots = "Modifica…"; RenameDots = "Rinomina…"; Delete = "Elimina";
        Options = "Opzioni"; SearchCue = "Cerca…";
        CountOf = "{0} di {1}";
        NoResults = "Nessun risultato"; HistoryOffEmpty = "La cronologia è spenta";
        PausedEmpty = "In pausa: quello che copi non viene ricordato"; CopyHint = "Quello che copi comparirà qui";
        NoSaved = "Nessun testo salvato";
        NewTip = "Scrive un testo nuovo da tenere, con il suo nome ({0}).";
        AllPlace = "Tutti"; PinnedPlace = "In alto"; SourcesPlace = "Provenienza"; GuideItem = "Guida";
        AllKinds = "Tutti"; TextKinds = "Testo"; CodeKinds = "Codice"; LinkKinds = "Link"; PictureKinds = "Immagini";
        TextBadge = "Testo"; CodeBadge = "Codice"; LinkBadge = "Link"; PictureBadge = "Immagine"; FormattedBadge = "Formattato";
        CopyButton = "Copia"; ShowFull = "Mostra tutto"; MoreItem = "Altro";
        Today = "Oggi"; Yesterday = "Ieri"; Undated = "Prima";
        NoPinned = "Niente in alto: la freccia di una voce copiata la porta qui per un po'";
        NoneOfKind = "Niente di questo tipo";
        SourcesTip = "I programmi in cui hai copiato: scegline uno per vedere solo ciò che hai copiato lì.";
        PinnedTip = "Le voci copiate tenute in alto per un po': se ne vanno comunque quando scadono i loro giorni.";
        Error = "Errore: {0}";
        NewerDatabase = "{0} è stato scritto da una versione più nuova di {1}: aggiorna il programma per aprirlo.";
        UnexpectedError ="Qualcosa è andato storto: {0}\n\nIl programma continua. I dettagli sono in {1}.";
        SqliteMissing = "{0} non può partire: tiene ciò che copi e salvi con SQLite, che fa parte di Windows 10 e 11 "
            + "(winsqlite3.dll), e questo Windows non ce l'ha.";
        SqliteTooOld = "{0} non può partire: gli serve SQLite {1} o più recente, che fa parte di Windows 10 e 11, "
            + "e questo Windows ha la versione {2}. Aggiornare Windows risolve.";
        PicturesFull = "Lo spazio per le immagini copiate ({0}) è pieno: le più vecchie vengono cancellate, una alla volta, "
            + "man mano che ne copi di nuove.";

        Open = "Apri"; Exit = "Esci"; Pause = "Sospendi la cattura"; Resume = "Riprendi la cattura";
        ShowContent = "Mostra il contenuto"; MoveTo = "Sposta in"; NewFolder = "Nuova cartella…";
        CopyPictureText = "Copia il testo";
        Pin = "Tieni in alto"; Unpin = "Togli dall'alto"; OutOfFolders = "Fuori dalle cartelle"; EmptyFolder = "Vuota";
        NumberMenu = "Scorciatoia"; NoNumber = "Nessuna";
        KindMenu = "Tipo";

        TrayReady = ": testo pronto negli appunti"; TrayEmpty = ": appunti senza testo"; TrayPaused = ", cattura in pausa";
        TrayHint = "Sono ancora attivo qui. Clic per riaprire, tasto destro e poi {0} per chiudere.";

        StartupRefused = "Windows non lo permette: attivalo da Impostazioni > App > Avvio.";
        StartupOn = "All'accesso a Windows {0} partirà da solo, senza mostrare la finestra.";
        StartupOff = "{0} non parte più insieme a Windows.";
        PausedSay = "In pausa: quello che copi non viene ricordato, finché non riprendi la cattura.";
        ResumedSay = "La cattura è ripresa: quello che copi torna nell'elenco.";
        Pinned = "Resta in alto: {0}."; Unpinned = "Non è più in alto: {0}.";
        KindChanged = "Segnata come {1}: {0}.";
        NumberGiven = "{0} ora incolla «{1}» ovunque tu sia.";
        NumberTaken = "«{0}» non ha più una scorciatoia.";
        Pasted = "Incollato in {1}: {0}.";
        PasteFocusLost = "Non incollato: la finestra è cambiata. La voce è negli appunti.";
        OnClipboard = "Negli appunti: {0}.\tIncollalo con Ctrl+V.";
        ReadingPicture = "Lettura del testo nell'immagine…";
        NoPictureText = "Windows non ha trovato testo nell'immagine.";
        NoRecognition = "Windows non sa leggere le immagini nelle tue lingue: installa il riconoscimento ottico dei caratteri "
            + "di una dalle sue opzioni, in Impostazioni > Ora e lingua.";
        OnClipboardOrType = "Negli appunti: {0}.\tIncollalo con Ctrl+V o digitalo con {1}.";
        AskReplace = "«{0}» esiste già. Vuoi sostituirlo?";
        SavedSay = "Salvato «{0}».";
        FolderNoSeparator = "Il nome di una cartella non può contenere «{0}».";
        FolderExists = "La cartella «{0}» esiste già.";
        FolderCreated = "Creata la cartella «{0}»."; FolderRenamed = "La cartella ora si chiama «{0}».";
        FolderMoved = "La cartella ora è «{0}».";
        LeftFolder = "«{0}» non è più in una cartella."; InFolder = "«{0}» è nella cartella «{1}».";
        AskDeleteFolder = "Vuoi eliminare la cartella «{0}»?";
        DeleteFolderTop = "\n\nQuello che contiene non viene eliminato: resta tra i salvati, fuori dalle cartelle.";
        DeleteFolderUp = "\n\nQuello che contiene non viene eliminato: passa nella cartella «{0}».";
        AskDelete = "Vuoi eliminare «{0}»?";
        Yes = "Sì"; No = "No"; Ok = "OK";

        Cancel = "Annulla";
        NoText = "Negli appunti non c'è testo: copia prima la password.";
        Cancelled = "Annullato.";
        CapsLock = "Bloc Maiusc è attivo: disattivalo e riprova.";
        TypingInto = "Digitazione in corso in {0}…";
        AskUnusual = "Gli appunti contengono {0}: non sembra una password.\n\nVuoi digitarli comunque?";
        OnLines = " su {0} righe";
        Countdown = "Digito tra {0} s in {1}. Premi Annulla per fermare.";
        TimeUp = "Tempo scaduto: non hai scelto la finestra in cui digitare.";
        ClickNow = "Clicca ora nel campo password della finestra remota…";
        Untitled = "una finestra senza titolo";
        KeysHeld = "Rilascia i tasti che stai tenendo premuti e riprova.";
        Blocked = "Windows ha bloccato l'invio dei tasti. Se il programma di desktop remoto è avviato come "
            + "amministratore, avvia così anche {0}.";
        StoppedAfter = "Interrotto dopo {0}."; FocusLost = "Interrotto dopo {0}: la finestra attiva è cambiata.";
        DoneOne = "Fatto: 1 carattere digitato."; DoneMany = "Fatto: {0} caratteri digitati.";
        AltCoded = " {0} con Alt+codice.";
        UnicodeOne = " Attenzione: 1 carattere senza tasto sulla tastiera, inviato come Unicode";
        UnicodeMany = " Attenzione: {0} caratteri senza tasto sulla tastiera, inviati come Unicode";
        UnicodeHint = ": se l'accesso fallisce prova l'opzione Alt+codice.";
        Character = "carattere"; Characters = "caratteri";

        HotkeyTip = "{0} digita subito gli appunti nella finestra attiva, senza attesa.\n"
            + "Non funziona se il desktop remoto è a schermo intero e cattura tutte le\n"
            + "scorciatoie: in quel caso usa il pulsante «{1}».";
        PickTip = "{0} porta in primo piano questa finestra con l'elenco, da qualsiasi programma.\n"
            + "Scelta una voce, il controllo torna alla finestra da cui sei partito: lì la incolli\n"
            + "con Ctrl+V o la digiti con {1}.\n"
            + "Finché è attiva, {0} non arriva più agli altri programmi.";
        ChangeKeysNote = "\nIl pulsante a destra mostra i suoi tasti: premilo per dargliene altri.";
        KeysTip = "Premi qui e poi i tasti nuovi: Ctrl o Alt, anche con {0}, insieme a una lettera,\n"
            + "una cifra o un tasto funzione. Esc lascia quelli di adesso, {1} torna a quelli di partenza.\n"
            + "Con Ctrl+Alt evita le lettere che con AltGr scrivono un simbolo (E, per l'euro): la\n"
            + "scorciatoia scatterebbe scrivendolo.";
        LeaveTip = "Quando clicchi su un altro programma la finestra torna nell'area di notifica, come\n"
            + "chiudendola: la riapri con l'icona o con {0}. Spenta, resta aperta dietro le altre.\n"
            + "Vale solo con l'opzione qui sopra attiva. Non succede mentre aspetta o digita, né mentre\n"
            + "scrivi un testo salvato o rispondi a una domanda: intanto puoi copiare altrove.";
        TypeTip = "Digita il testo degli appunti un tasto alla volta, dove incollare non funziona:\n"
            + "copia la password, premi qui e clicca nel campo della finestra remota.\n"
            + "Oppure premi {0} direttamente nel campo.";
        SearchTip = "Scrivi per cercare tra le voci del posto: parole in qualsiasi ordine, senza badare a\n"
            + "maiuscole e accenti. Le frecce scorrono le voci.\n"
            + "«" + CopyButton + "», {0} o un doppio clic rimettono la voce negli appunti: poi la incolli\n"
            + "con Ctrl+V o la digiti con {1}. Con «" + CopyButton + "» la finestra resta aperta, con {0} o un\n"
            + "doppio clic torni dov'eri. Un clic seleziona soltanto; il tasto destro apre il menu della voce.\n"
            + "Fermando il puntatore su una voce copiata la vedi per intero.\n"
            + "La freccia a destra di una voce copiata ({2}) la tiene per un po' in cima all'elenco:\n"
            + "se ne va comunque quando scadono i suoi giorni. Ciò che deve restare, salvalo.\n"
            + "I testi salvati non scadono mai, sono cifrati per il tuo utente, e\n"
            + "se ne vede solo il nome. Si possono raccogliere in cartelle: «Nuova cartella» è nel\n"
            + "menu del tasto destro ({3}); un testo ci entra trascinandolo, o con\n"
            + "«Sposta in». Un clic sulla cartella la apre e la chiude.\n"
            + "«{5}», nel menu del tasto destro, dà un numero alla voce: poi {4}\n"
            + "la incollano ovunque tu sia, senza aprire questa finestra.";
        PressKeys = "Premi i tasti…";
        PressKeysStatus = "Premi i tasti nuovi: Ctrl o Alt con una lettera, una cifra o un tasto funzione. "
            + "Esc lascia quelli di adesso, {0} torna a quelli di partenza.";
        KeysUnusable = "{0} non va bene: serve Ctrl o Alt con una lettera, una cifra o un tasto funzione.";
        KeysTaken = "{0} serve già al programma: scegline un'altra.";
        TypeNowWith = "Per digitare gli appunti ora premi {0}."; OpenNowWith = "Per aprire l'elenco ora premi {0}.";
        HotkeyBusyType = "{0} è già usata da un altro programma: dalle altri tasti nelle opzioni, oppure usa il pulsante.";
        HotkeyBusyPick = "{0} è già usata da un altro programma: dalle altri tasti nelle opzioni, oppure apri la "
            + "finestra dall'icona.";
        QuickTip = "{0} incollano la voce con quel numero nella finestra in cui stai lavorando,\n"
            + "senza aprire questa: va negli appunti e Ctrl+V segue da solo.\n"
            + "I numeri si danno con «{1}», nel menu del tasto destro di una voce.\n"
            + "Si prendono solo i tasti dei numeri in uso: quelli non arrivano più agli altri programmi.";
        QuickKeysTip = "Premi qui e poi i tasti da tenere premuti, insieme a una cifra qualsiasi: Ctrl o Alt,\n"
            + "anche con {0}. La cifra non conta: da 1 a 9, sceglie la voce.\n"
            + "Esc lascia quelli di adesso, {1} torna a quelli di partenza ({2}).\n"
            + "Ctrl+Alt è AltGr: su alcune tastiere con le cifre scrive dei simboli.";
        PressQuickStatus = "Premi i tasti da tenere premuti con una cifra qualsiasi, come {0}. "
            + "Esc lascia quelli di adesso, {1} torna a quelli di partenza.";
        QuickUnusable = "{0} non va: tieni premuto Ctrl o Alt, anche con Maiusc, e premi una cifra.";
        QuickNowWith = "Le voci numerate ora si incollano con {0}.";
        QuickBusy = "Già usati da un altro programma: {0}. Quei numeri non incolleranno: scegli altri tasti nelle "
            + "opzioni, o altri numeri.";

        TypingTitle = "Digitazione"; ProgramTitle = "Programma"; HotkeysTitle = "Scorciatoie"; HistoryTitle = "Cronologia";
        AppearanceTitle = "Aspetto";
        On = "Attivato"; Off = "Disattivato";
        DelayOption = "Attesa prima di digitare"; DelayNote = "Secondi per cliccare nel campo giusto";
        UsOption = "Il server usa la tastiera inglese (USA)"; UsNote = "Solo se i simboli arrivano sbagliati";
        EnterOption = "Premi Invio alla fine"; EnterNote = "Conferma subito l'accesso";
        ClearOption = "Svuota gli appunti dopo aver digitato"; ClearNote = "Quando la digitazione è riuscita";
        SlowOption = "Digitazione lenta"; SlowNote = "Se i caratteri si perdono o arrivano in disordine";
        AltCodeOption = "Alt+codice per i simboli che la tastiera non ha"; AltCodeNote = "Solo per i server con Windows";
        LanguageOption = "Lingua (Language)"; LanguageNote = "Il programma riparte nella lingua scelta";
        ThemeOption = "Tema"; ThemeNote = "Colori chiari o scuri";
        ThemeSystem = "Come Windows"; ThemeLight = "Chiaro"; ThemeDark = "Scuro";
        FaceOption = "Carattere dell'interfaccia"; FaceNote = "Pulsanti, opzioni, menu: il programma riparte";
        FontOption = "Carattere delle voci"; FontNote = "Ciò che copi e salvi: cambia subito";
        Pixels = "{0} px";
        TrayOption = "Resta attivo nell'area di notifica (tray)"; TrayNote = "Chiudendo la finestra il programma resta attivo";
        LeaveOption = "Nascondi passando a un altro programma"; LeaveNote = "Torna nell'area di notifica";
        StartOption = "Avvia insieme a Windows"; StartNote = "Nascosto nell'area di notifica";
        BackupOption = "Esporta e importa"; BackupNote = "I tuoi testi in un file che si apre con il Blocco note";
        ExportButton = "Esporta…"; ImportButton = "Importa…";
        AboutNote = "Software libero con licenza MIT";
        TypeNote = "Digita subito nella finestra attiva";
        PickOption = "Apri l'elenco"; PickNote = "Porta questa finestra in primo piano da ogni programma";
        QuickOption = "Incolla per numero"; QuickNote = "Incolla le voci che hanno una scorciatoia";
        HistoryOption = "Ricorda i testi e le immagini copiati"; HistoryNote = "Ciò che copi mentre il programma è aperto";
        KeepOption = "Giorni da tenere"; KeepNote = "Con 0, solo ciò che hai copiato oggi";
        FolderLink = "Apri la cartella dei dati";
        DataFolderOption = "Cartella delle voci copiate e salvate";
        ChangeFolderButton = "Cambia…"; DefaultFolderButton = "Predefinita";
        DatabaseSize = "Occupa {0}; immagini copiate: {1} su {2}.";
        ChooseDataFolder = "La cartella in cui {0} tiene ciò che copi e salvi.";
        AskUseExisting = "In {0} c'è già un elenco di {1}.\n\nVuoi usare quello? Il tuo resta dov'è, così com'è.";
        AskSyncedFolder = "{0} sembra una cartella sincronizzata online o in rete.\n\nLa sincronizzazione può bloccare o "
            + "rovinare il database, e i testi salvati non si leggono su un altro PC. Vuoi usarla lo stesso?";
        ListMovedTo = "Ora l'elenco sta in {0}.";
        ListMoveFailed = "Non è stato possibile spostare l'elenco, che resta dov'era: {0}";
        FolderUnavailable = "La cartella dell'elenco, {0}, non è raggiungibile: ciò che copi e salvi ora si perde alla "
            + "chiusura del programma. Nelle opzioni, sotto {1}, scegli un'altra cartella o quella predefinita.";
        SpareOption = "Pulisci la cronologia"; SpareNote = "Tranne gli ultimi giorni scritti accanto; 0: nessuno";
        CleanButton = "Pulisci ora…";
        DelayTip = "Per quanti secondi la finestra di destinazione deve restare in primo piano\n"
            + "prima che parta la digitazione: è il tempo che hai per cliccare nel campo\ngiusto, o per annullare.";
        UsTip = "Lasciala spenta per i normali desktop remoti: vale la tastiera di questo PC.\n"
            + "Attivala se i simboli arrivano sbagliati perché il server usa la tastiera\n"
            + "americana (capita con le console VMware, iLO, iDRAC e con alcune schermate\ndi accesso).";
        EnterTip = "Preme Invio dopo l'ultimo carattere, per confermare subito l'accesso.";
        ClearTip = "Cancella la password dagli appunti quando la digitazione è riuscita.\n"
            + "La cronologia degli appunti di Windows (Win+V), se attiva, non viene toccata.";
        SlowTip = "Attivala se sul server arrivano caratteri mancanti o in disordine.";
        AltCodeTip = "I simboli che questa tastiera non ha (ad esempio ~ e ` su quella italiana)\n"
            + "vengono normalmente inviati come caratteri Unicode, che non tutti i programmi\n"
            + "di desktop remoto accettano. Con questa opzione vengono invece composti con\n"
            + "Alt+codice sul tastierino numerico: funziona solo se il server è Windows.";
        LanguageTip = "La lingua del programma. Cambiandola {0} si riavvia, e si riapre subito.";
        ThemeTip = "Colori chiari o scuri. «{0}» segue la modalità che Windows imposta per le app,\n"
            + "anche quando cambia a programma aperto.";
        FaceTip = "Il carattere del programma stesso: pulsanti, etichette, opzioni, la guida, i menu e le domande.\n"
            + "Ciò che hai copiato e salvato ha il suo, qui sotto. Cambiarlo riavvia {0}.";
        FaceSizeTip = "Quanto è grande il testo del programma stesso, in pixel; {0} di default. Le finestre crescono\n"
            + "con il testo. Ciò che hai copiato e salvato ha la sua dimensione, qui sotto. Cambiarla riavvia {1}.";
        FontTip = "Il carattere di ciò che hai copiato e salvato: le righe degli elenchi, la voce vista per intero\n"
            + "e il contenuto di un testo salvato. Il resto del programma tiene il carattere dell'interfaccia. Cambia subito.";
        SizeTip = "Quanto è grande ciò che hai copiato e salvato, in pixel; {0} è il testo di Windows 11.\n"
            + "Il resto del programma tiene la dimensione dell'interfaccia. Cambia subito.";
        HistoryTip = "L'elenco propone i testi e le immagini copiati mentre il programma è aperto.\n"
            + "Ciò che i gestori di password segnano come riservato non viene ricordato.\n"
            + "Disattivala se copi password da programmi che non lo fanno: nell'elenco si\n"
            + "leggerebbero in chiaro. Spenta, cancella anche la cronologia su disco.";
        KeepTip = "Per quanti giorni ciò che copi resta nella cronologia, anche dopo la chiusura,\n"
            + "contando oggi come primo: con 0 resta solo quello che hai copiato oggi.\n"
            + "Anche le voci in alto se ne vanno quando scadono i loro giorni; i testi salvati mai.\n"
            + "La cronologia sta su disco in chiaro. L'elenco mostra le {0} più recenti:\n"
            + "le altre si trovano con la ricerca.";
        DataFolderTip = "Dove sta il database con ciò che copi e salvi, insieme al registro degli errori:\n{0}\n"
            + "La cartella predefinita è {1}. «Cambia…» sposta il database nella cartella che scegli,\n"
            + "o usa quello che c'è già lì. Non serve a condividere l'elenco tra PC: i testi salvati\n"
            + "li legge solo questo utente di Windows su questo PC, e due programmi che usano\n"
            + "lo stesso file insieme possono rovinarlo.";
        CleanTip = "Elimina subito dalla cronologia quello che hai copiato prima degli ultimi giorni\n"
            + "scritti qui sopra: con 1 resta solo oggi, con 0 non resta niente.\n"
            + "Prima chiede conferma. Vanno anche le voci in alto; i testi salvati non vengono\n"
            + "toccati, e nemmeno gli appunti. I giorni da tenere restano quelli impostati.";
        TrayTip = "Attiva: chiudendo o riducendo a icona la finestra il programma resta in funzione\n"
            + "come icona vicino all'orologio, e le scorciatoie continuano a funzionare.\n"
            + "Clic sull'icona per riaprire la finestra, tasto destro e poi {0} per chiudere.\n"
            + "Disattiva: finestra normale, che chiudendola termina il programma.";
        StartTip = "Avvia {0} quando accedi a Windows, già nascosto nell'area di notifica\n"
            + "(ridotto a icona, se l'opzione qui sopra è spenta).\n"
            + "Vale per questo eseguibile: se lo sposti, al primo avvio da lì la voce si aggiorna da sola.\n"
            + "È la stessa voce di Gestione attività > App di avvio: spenta lì, risulta spenta anche qui.";
        ExportTip = "Scrive in un file di testo, che si apre con il Blocco note, i testi salvati con le loro\n"
            + "cartelle; se lo chiedi, anche le cose copiate. Le immagini non ci vanno.\n"
            + "Nel file i testi sono in chiaro, password comprese: tienilo in un posto sicuro.";
        ImportTip = "Legge un file scritto da «Esporta», anche su un altro PC, e aggiunge ciò che contiene\n"
            + "a quello che c'è già. Se un testo salvato ha il nome di uno esistente, chiede quale tenere.\n"
            + "Le cose copiate che l'elenco ha già non vengono ripetute.";
        OnlySaved = "Solo i testi salvati…"; Everything = "Tutto, anche le cose copiate…";
        TextFiles = "File di testo";
        AskExport = "Il file conterrà i testi in chiaro, password comprese: chiunque lo apra potrà leggerli.\n\nVuoi continuare?";
        SavedOne = "testo salvato"; SavedMany = "testi salvati"; CopiedOne = "copiato"; CopiedMany = "copiati";
        Exported = "Esportati: {0}."; ExportedAll = "Esportati: {0}, {1}.";
        PicturesLeftOut = " Le immagini ({0}) non si esportano.";
        ClashOne = "Un testo salvato del file ha il nome di uno che esiste già.";
        ClashMany = "{0} testi salvati del file hanno il nome di uno che esiste già.";
        ClashChoice = "\n\nSì: vale quello del file.\nNo: resta quello che c'è.";
        NothingImported = "Nel file non c'era niente da importare.";
        Imported = "Importati: {0}."; ImportedAll = "Importati: {0}, {1}.";
        KeptAsWere = " Lasciati com'erano: {0}.";
        HistoryOffImport = " La cronologia è spenta: i copiati non sono stati importati.";
        CleanEverything = "tutto quello che hai copiato"; CleanBeforeToday = "quello che hai copiato prima di oggi";
        CleanBeforeDays = "quello che hai copiato prima degli ultimi {0} giorni (oggi compreso)";
        AskClean = "Vuoi eliminare {0}?\n\nI testi salvati restano; vanno anche le voci in alto.";
        NothingToClean = "Niente da eliminare nella cronologia.";
        CleanedOne = "Eliminata 1 voce dalla cronologia."; CleanedMany = "Eliminate {0} voci dalla cronologia.";
        NoHistoryFolder = "La cartella dei dati non esiste ancora: su disco non c'è niente.";
        DatabaseUnavailable = "L'elenco non si può tenere su disco ({0}): ciò che copi e salvi ora si perde alla "
            + "chiusura del programma.";

        NewTextTitle = "Nuovo testo"; SaveCopiedTitle = "Salva il testo copiato"; EditSavedTitle = "Modifica il testo salvato";
        SavePictureTitle = "Salva l'immagine copiata"; EditPictureTitle = "Modifica l'immagine salvata";
        PictureHintEmpty = "Senza nome, nell'elenco si vedrà solo l'immagine.";
        PictureHintNamed = "Nell'elenco si vedrà l'immagine con accanto «{0}».";
        NameLabel = "Nome (facoltativo):"; ContentLabel = "Contenuto:";
        NameHintEmpty = "Senza nome, nell'elenco si vedrà il contenuto.";
        NameHintNamed = "Nell'elenco si vedrà «{0}»; il contenuto resta nascosto, adatto alle password.";
        BoldTip = "Grassetto (Ctrl+B)"; ItalicTip = "Corsivo (Ctrl+I)"; UnderlineTip = "Sottolineato (Ctrl+U)";
        TypefaceTip = "Carattere"; PointsTip = "Dimensione"; PlainTip = "Togli la formattazione: della selezione, o di tutto il testo";
        DefaultLook = "Predefinito";
        FolderOf = "Cartella: "; NoFolder = "Nessuna cartella"; Inside = "Dentro: ";
        NewFolderTitle = "Nuova cartella"; RenameFolderTitle = "Rinomina la cartella"; FolderNameLabel = "Nome della cartella:";
        Save = "Salva";

        PictureLabel = "Immagine {0}×{1}";
        PictureToday = "Immagine {0}×{1}, copiata alle {2}"; PictureOn = "Immagine {0}×{1}, copiata il {2} alle {3}";
        MoreOne = "… e un altro carattere"; MoreMany = "… e altri {0} caratteri";

        ExportHeader = "{0} {1}: esportazione del {2}";
        ExportClearWarning = "Attenzione: qui i testi sono in chiaro, password comprese.";
        ExportEditNote = "Il testo di ogni voce sta nelle righe che iniziano con una tabulazione: se modifichi il file, "
            + "lasciala al suo posto.";

        HelpTitle = "Guida di {0}"; OptionsTitle = "Opzioni di {0}"; Close = "Chiudi";
        Guide = ItalianGuide;
    }

    static string[] ItalianGuide()
    {
        string pick = MainForm.PickName, type = MainForm.HotkeyName, app = Program.AppName;
        return new string[]
        {
            "# A cosa serve",
            app + " tiene da parte quello che copi e i testi che scegli di salvare, per riaverli negli appunti quando "
                + "servono. Sa anche digitare gli appunti come pressioni di tasti veri: serve per inserire una password "
                + "dove incollare non funziona, ad esempio nelle schermate di blocco dei desktop remoti.",

            "# L'elenco",
            "A sinistra c'è la barra laterale, a destra le schede delle voci del posto scelto lì:",
            "- «" + AllPlace + "»: tutto, copiato e salvato, dal più nuovo, ogni giorno sotto il suo nome; le voci tenute "
                + "in alto vengono prima delle altre.",
            "- «" + PinnedPlace + "»: solo le voci copiate tenute in alto.",
            "- «" + SavedTitle + "»: i testi salvati, per nome. Sotto ci sono le loro cartelle.",
            "- «" + SourcesPlace + "»: i programmi in cui hai copiato, ognuno con la sua icona e quante voci vengono da "
                + "lui. Un clic su uno mostra solo ciò che hai copiato lì; un clic su «" + SourcesPlace + "» apre o chiude l'elenco.",
            "Le schede sopra le voci (" + AllKinds + ", " + TextKinds + ", " + CodeKinds + ", " + LinkKinds + ", "
                + PictureKinds + ") mostrano un solo tipo di voce. «" + CodeKinds + "» è un'ipotesi fatta dall'aspetto delle "
                + "righe: ogni tanto ci finisce un testo, o un pezzo di codice tra i testi. Quando sbaglia, «" + KindMenu
                + "», nel menu di un testo, dice che cos'è: copiato di nuovo o salvato, resta così.",
            "Ogni scheda mostra prima la voce; sotto, in piccolo, il programma in cui è stata copiata, con la sua icona (o "
                + "la cartella di un testo salvato), l'ora della copia e che cos'è. A destra, piccoli segni la copiano, la "
                + "salvano o la modificano, la eliminano e aprono il suo menu: fermando il puntatore su uno, dice che cosa fa.",
            "- Scrivi nella casella in alto per cercare tra le voci del posto: le parole valgono in qualsiasi "
                + "ordine, senza badare a maiuscole e accenti.",
            "- Un clic su una scheda la seleziona. Il segno della copia («" + CopyButton + "») mette la voce negli appunti e lascia la "
                + "finestra aperta, così puoi ancora salvarla o eliminarla.",
            "- " + EnterKeyName + " o un doppio clic la mettono negli appunti e, se avevi aperto l'elenco con " + pick
                + ", ti riportano alla finestra in cui stavi lavorando.",
            "- A quel punto la incolli con Ctrl+V, oppure la digiti con " + type + ".",
            "- «" + ShowFull + "», nel menu, mostra la voce per intero in un riquadro: il testo con tutte le sue righe, l'immagine "
                + "più grande. Fermare il puntatore su una scheda che taglia il suo testo fa lo stesso. Sparisce appena "
                + "sposti il puntatore.",
            "- I tre puntini a destra, o il tasto destro, aprono il menu della voce, senza toccare gli appunti.",
            "- «" + CopyPictureText + "», nel menu di un'immagine, mette negli appunti il testo che "
                + "Windows ci legge, qui sul PC, senza internet: entra nell'elenco come ogni cosa che copi (quello di "
                + "un'immagine salvata è riservato, come un testo salvato). Il testo piccolo può uscire con qualche errore: "
                + "guardalo prima.",
            "- La freccia a destra di una voce copiata (" + Key(Keys.Control | MainForm.PinKey) + ") la "
                + "tiene in alto per un po': è colorata finché lo fa, e un clic la accende o la spegne. La voce se ne va "
                + "comunque quando scadono i giorni della cronologia: ciò che deve restare, salvalo. Copiare di nuovo lo "
                + "stesso testo lo riporta in cima con l'ora nuova, freccia e numero compresi.",
            "- «" + NumberMenu + "», nel menu, dà a un testo salvato o a una voce copiata un numero "
                + "da 1 a " + MainForm.MaxNumber + ": poi, da qualsiasi programma, " + MainForm.NumbersName + " la "
                + "incollano subito nella finestra in cui stai lavorando, senza aprire questa (la voce va negli appunti "
                + "e Ctrl+V segue da solo). Il numero compare in un riquadro a destra della scheda, e accanto al testo "
                + "salvato nel menu dell'icona. Una voce copiata tiene il suo numero finché non scadono i suoi giorni. "
                + "Ogni numero è di una voce sola: dato a un'altra, passa a lei.",
            "Per non ricordare quello che stai per copiare, «" + Pause + "» sta in fondo alla barra laterale "
                + "e nel menu dell'icona vicino all'orologio: il titolo dice «in pausa» finché non scegli «"
                + Resume + "». Al prossimo avvio la cattura è di nuovo attiva.",
            "L'elenco mostra le " + MainForm.MaxShown + " voci copiate più recenti: le altre si trovano con la ricerca.",

            "# I testi salvati",
            "Sono le cose che vuoi avere sempre a portata di mano: una firma, un indirizzo, una password. Non scadono "
                + "mai. Il nome è facoltativo: con il nome, l'elenco mostra solo il nome e il contenuto resta nascosto, "
                + "adatto a una password; senza, l'elenco mostra il contenuto, come per un secondo indirizzo email.",
            "- «" + New + "» (" + Key(Keys.Control | MainForm.NewKey) + ") scrive un testo da zero.",
            "- «" + SaveDots + "» (" + Key(Keys.Control | MainForm.SaveKey) + "), su una voce copiata, la tiene, con un "
                + "nome o senza; prima di salvare puoi ritoccarne il contenuto.",
            "- «" + EditDots + "» (" + Key(Keys.Control | MainForm.SaveKey) + "), su una voce salvata, ne cambia il nome "
                + "o il contenuto.",
            "- «" + Delete + "» (" + Key(Keys.Control | MainForm.DeleteKey) + ") cancella il testo salvato, oppure toglie "
                + "dall'elenco la voce copiata.",
            "- «" + ShowContent + "», nel menu del tasto destro di un testo salvato, lo fa vedere in un riquadro per "
                + "qualche secondo: l'elenco ne mostra solo il nome, e passarci sopra con il puntatore non basta.",
            "- I testi salvati sono anche nel menu dell'icona vicino all'orologio: sceglierne uno lo rimette negli "
                + "appunti senza aprire la finestra.",
            "I testi salvati non scadono mai. Stanno nel database del programma, cifrati per il tuo utente: un altro "
                + "utente dello stesso PC non li può leggere. Quando ne rimetti uno negli appunti viene segnato come riservato, perché la "
                + "cronologia di Windows (Win+V) e gli altri gestori di appunti lo ignorino. Si può salvare anche un'immagine copiata: mostra sempre la "
                + "sua miniatura, con accanto il nome se ne ha uno.",
            "Un testo salvato può essere formattato, come una firma di posta: grassetto, corsivo, sottolineato, caratteri e dimensioni.",
            "- Copia la firma da una mail o da un documento e salvala: tiene la sua formattazione. La cronologia tiene "
                + "il testo semplice, quindi una voce copiata ce l'ha ancora solo finché è tra le ultime " + Rich.KeptCopies
                + " copie da quando il programma è partito.",
            "- Nella finestra di un testo, i pulsanti sopra il contenuto mettono la selezione in grassetto (Ctrl+B), "
                + "corsivo (Ctrl+I) o sottolineato (Ctrl+U), le danno un carattere e una dimensione, o le tolgono la "
                + "formattazione (senza selezione, a tutto il testo). Ciò che incolli lì tiene la sua formattazione.",
            "- Scelto, un testo formattato va negli appunti sia formattato sia semplice: Ctrl+V in una mail lo incolla "
                + "formattato, dove la formattazione non conta arriva il testo semplice, e " + type + " digita il testo semplice.",
            "- Finché non lo cambi resta esattamente com'era stato copiato, link e immagini compresi. Una volta "
                + "cambiato tiene solo ciò che i pulsanti conoscono: link, immagini, allineamento ed elenchi si perdono.",

            "# Le cartelle",
            "Quando i testi salvati diventano tanti puoi raccoglierli in cartelle, e le cartelle in altre cartelle. Stanno "
                + "nella barra laterale, sotto «" + SavedTitle + "»: un clic su una cartella mostra i suoi testi, e quelli "
                + "delle cartelle al suo interno.",
            "- «" + NewFolder + "» sta in fondo alla barra laterale e nel menu del tasto destro di una cartella "
                + "(" + Key(Keys.Control | Keys.Shift | MainForm.NewKey) + "): nasce dentro la cartella mostrata, "
                + "ma nella sua finestra puoi scegliere dove metterla.",
            "- La freccia all'estremità destra di una cartella che ne contiene altre la apre e la chiude; quella di «"
                + SavedTitle + "» le mostra o le nasconde tutte. Il numero dice quanti testi contiene una cartella, anche "
                + "nelle cartelle al suo interno. Aperte e chiuse, le ritrovi come le hai lasciate.",
            "- La scheda di un testo salvato, o una cartella della barra con tutto quello che contiene, si trascina su una "
                + "cartella della barra; lasciata su «" + SavedTitle + "», esce dalle cartelle.",
            "- «" + MoveTo + "», nel menu di un testo salvato o di una cartella, fa lo stesso. La stessa "
                + "scelta c'è nella finestra in cui lo scrivi, a sinistra dei pulsanti.",
            "- Nel menu del tasto destro di una cartella, «" + RenameDots + "» le cambia il nome o il posto ed «" + Delete
                + "» toglie la cartella soltanto: quello che conteneva, testi e cartelle, sale nella cartella che la "
                + "conteneva, o resta tra i salvati fuori dalle cartelle.",
            "- Nel nome di una cartella non può esserci «" + Texts.Separator + "».",
            "- Nel menu dell'icona vicino all'orologio ogni cartella è un sottomenu con i suoi testi.",
            "- Il nome di una cartella trova i suoi testi nella ricerca.",

            "# Digitare una password",
            "Dove Ctrl+V non funziona, " + app + " scrive il testo degli appunti un tasto alla volta, come faresti tu "
                + "sulla tastiera.",
            "- Copia la password.",
            "- Premi «" + TypeButton + "».",
            "- Clicca nel campo della finestra remota: hai " + MainForm.ArmedSeconds + " secondi per farlo.",
            "- Quando quella finestra è rimasta in primo piano per il tempo dell'attesa, parte la digitazione.",
            "In alternativa premi " + type + " direttamente nel campo: digita subito, senza attesa.",
            "Per non mandare una password nel posto sbagliato:",
            "- la digitazione si ferma da sola se la finestra di destinazione perde il primo piano;",
            "- durante l'attesa e la digitazione lo stesso pulsante diventa «" + Cancel + "»;",
            "- desktop, barra delle applicazioni e menu Start non sono mai una destinazione;",
            "- se gli appunti contengono più di " + MainForm.UnusualLength + " caratteri o più righe, viene chiesta una conferma;",
            "- con Bloc Maiusc attivo non si digita: va spento prima.",

            "# I tasti",
            pick + "\tApre l'elenco, da qualsiasi programma.",
            type + "\tDigita subito gli appunti nella finestra attiva.",
            MainForm.NumbersName + "\tIncolla nella finestra attiva la voce con quel numero.",
            "F1\tApre questa guida.",
            ArrowsName + "\tScorre le voci, o i posti della barra laterale.",
            EnterKeyName + "\tSceglie la voce e torna alla finestra di partenza.",
            "Esc\tTorna alla finestra di partenza senza scegliere.",
            Key(Keys.Control | MainForm.SearchKey) + "\tPorta il cursore nella casella di ricerca.",
            Key(Keys.Control | MainForm.NewKey) + "\tNuovo testo salvato.",
            Key(Keys.Control | Keys.Shift | MainForm.NewKey) + "\tNuova cartella.",
            Key(Keys.Control | MainForm.SaveKey) + "\tSalva la voce copiata, o modifica quella salvata.",
            Key(Keys.Control | MainForm.DeleteKey) + "\tElimina la voce selezionata.",
            Key(Keys.Control | MainForm.PinKey) + "\tTiene in alto la voce copiata, o la lascia andare.",
            "Le prime tre funzionano ovunque; le altre valgono nell'elenco. Finché è attiva, " + pick + " non arriva "
                + "più agli altri programmi, e nemmeno i tasti dei numeri che qualche voce ha.",
            "Nelle opzioni, sotto «" + HotkeysTitle + "», le prime tre si spengono e si cambiano: il pulsante accanto a "
                + "ciascuna mostra i suoi tasti, e premendolo aspetta quelli nuovi. Per i numeri si premono i tasti da "
                + "tenere premuti insieme a una cifra qualsiasi; se un altro programma ne ha già qualcuno, dice quali.",
            "- Servono Ctrl o Alt, anche con " + ShiftName + ", insieme a una lettera, una cifra o un tasto funzione.",
            "- Esc lascia i tasti di adesso, " + DeleteKeyName + " torna a quelli di partenza.",
            "- Con Ctrl+Alt evita le lettere che con AltGr scrivono un simbolo, come la E dell'euro: la scorciatoia "
                + "scatterebbe mentre lo scrivi.",

            "# La cronologia",
            "Quello che copi resta nell'elenco per un numero di giorni che decidi tu (" + Texts.DefaultKeepDays
                + " se non lo cambi), anche dopo aver chiuso il programma o spento il PC.",
            "- Sta in chiaro in un database, di solito " + Clips.DefaultFile + ", insieme ai testi salvati, che invece "
                + "sono cifrati. I giorni scaduti si eliminano da soli.",
            "- Nelle opzioni, «" + ChangeFolderButton + "» porta il database in un'altra cartella, o usa quello che c'è "
                + "già lì; «" + DefaultFolderButton + "» lo riporta indietro. Non serve a condividere l'elenco tra PC: i "
                + "testi salvati si leggono solo su questo PC. Se all'avvio la cartella scelta non è raggiungibile, "
                + "l'elenco lavora in memoria e lo dice.",
            "- Con 0 giorni resta solo quello che hai copiato oggi.",
            "- Le immagini copiate occupano al massimo " + Size(Clips.PictureSpace) + ": oltre, le più vecchie se ne vanno "
                + "anche prima che scadano i loro giorni, e una barra rossa sotto l'elenco lo dice. Le immagini salvate non "
                + "contano.",
            "- Quello che i gestori di password segnano come riservato non viene ricordato.",
            "- Anche le voci in alto se ne vanno quando scadono i loro giorni: ciò che deve restare, salvalo.",
            "- Nelle opzioni, «" + CleanButton + "» elimina subito quello che hai copiato prima degli ultimi giorni che "
                + "scrivi accanto: con 1 resta solo oggi, con 0 non resta niente. Chiede conferma; vanno anche le voci "
                + "in alto, i testi salvati non vengono mai toccati.",
            "- Spegnendo la cronologia nelle opzioni si svuota l'elenco e si elimina dal disco ciò che hai copiato.",

            "# Le opzioni",
            "Il pulsante «" + Options + "» le apre in una finestra a parte, che può restare aperta accanto all'elenco. Ogni "
                + "opzione dice in poche parole, sotto il nome, che cosa fa; fermandoci sopra il puntatore ne compare la "
                + "spiegazione completa. Stanno in cinque pagine, che si scelgono dalla colonna a sinistra come gli argomenti "
                + "di questa guida:",
            "- " + TypingTitle + ": quanti secondi aspettare prima di digitare (da " + MainForm.MinDelay + " a "
                + MainForm.MaxDelay + "), se il server usa la tastiera americana, se premere Invio alla fine, se "
                + "svuotare gli appunti dopo, la digitazione lenta e i simboli composti con Alt+codice.",
            "- " + HotkeysTitle + ": accende e spegne " + type + ", " + pick + " e " + MainForm.NumbersName
                + ", e dà loro altri tasti.",
            "- " + HistoryTitle + ": se ricordare ciò che copi, per quanti giorni, la cartella del database "
                + "(che si può cambiare) con il collegamento che la apre, e il pulsante che pulisce subito la cronologia.",
            "- " + AppearanceTitle + ": la lingua, il tema (chiaro, scuro o «" + ThemeSystem + "»), il carattere e la "
                + "dimensione dell'interfaccia (pulsanti, opzioni, questa guida: in pixel, riavviano il programma) e quelli "
                + "delle voci (cambiano subito).",
            "- " + ProgramTitle + ": se restare attivo nell'area di notifica e nascondersi passando a un altro programma, "
                + "se partire insieme a Windows, i pulsanti per esportare e importare, e la versione del programma.",

            "# Esportare e importare",
            "Nelle opzioni, sotto «" + ProgramTitle + "», due pulsanti portano i tuoi testi fuori dal programma e li "
                + "riprendono, per una copia di sicurezza o per passarli a un altro PC. Ognuno chiede se vuoi solo i "
                + "testi salvati o tutto, cioè anche le cose copiate.",
            "- «" + ExportButton + "» scrive un file di testo che si apre con il Blocco note: i testi salvati con le "
                + "loro cartelle (quelli formattati con il loro HTML prima del testo) e, se lo chiedi, le cose copiate "
                + "con l'ora e la freccia. Le immagini non ci vanno.",
            "- Nel file i testi sono in chiaro, password comprese: tienilo in un posto sicuro.",
            "- «" + ImportButton + "» legge un file così e aggiunge ciò che contiene a quello che c'è già. Se un testo "
                + "salvato ha il nome di uno esistente chiede quale tenere; le cose copiate che l'elenco ha già non si "
                + "ripetono. Un file esportato in un'altra lingua si legge lo stesso.",
            "- Il file si può ritoccare a mano: il testo di ogni voce sta nelle righe che iniziano con una "
                + "tabulazione, che va lasciata al suo posto.",

            "# L'icona del tray",
            "Con l'opzione attiva, chiudere o ridurre a icona la finestra la nasconde soltanto: il programma resta in "
                + "funzione e le scorciatoie continuano a rispondere.",
            "- Un clic sull'icona riapre la finestra; tasto destro e poi «" + Exit + "» chiude davvero.",
            "- Con «" + LeaveOption + "» la finestra torna qui anche quando clicchi altrove; spenta, resta aperta "
                + "dietro le altre. Mentre aspetta e digita, invece, sta sopra a tutto: il conto alla rovescia e «"
                + Cancel + "» restano in vista.",
            "- Nel menu del tasto destro ci sono anche i testi salvati, ogni cartella un sottomenu: sceglierne uno lo "
                + "rimette negli appunti, pronto per Ctrl+V o per " + type + ", senza aprire la finestra.",
            "- Accanto a un testo salvato con un numero ci sono i suoi tasti.",
            "- L'icona è a colori se negli appunti c'è del testo, grigia se non c'è.",
            "- Avviare il programma una seconda volta fa ricomparire la finestra di quello già aperto.",

            "# Se qualcosa non va",
            "- I simboli arrivano sbagliati: il server usa un'altra tastiera. Prova l'opzione della tastiera inglese (USA).",
            "- Mancano caratteri, o arrivano in disordine: attiva la digitazione lenta.",
            "- Non arrivano i simboli che la tua tastiera non ha (~ e ` su quella italiana): attiva Alt+codice, che "
                + "funziona se il server è Windows.",
            "- " + type + " non fa niente con il desktop remoto a schermo intero: quel programma cattura tutte le "
                + "scorciatoie. Usa il pulsante.",
            "- Windows blocca l'invio dei tasti: il programma di desktop remoto è avviato come amministratore. Avvia "
                + "così anche " + app + ".",
            "- Una scorciatoia si spegne da sola: la sta già usando un altro programma. Dalle altri tasti nelle opzioni.",
            "La riga sopra i pulsanti dice che cosa è appena successo, e perché. Le conferme spariscono dopo qualche "
                + "secondo e lasciano il posto all'elenco; avvisi ed errori restano fino al messaggio dopo."
        };
    }
}
