# SmartClip

Piccolo gestore degli appunti per Windows che sa anche digitarne il contenuto come pressioni di tasti reali, per
inserire password dove l'incolla non funziona (schermate di blocco dei desktop remoti). Fino alla versione 1.2
si chiamava PasswordPaste (il repository GitHub è `giongy/SmartClip`), e alla
prima apertura `AdoptOldSettings` sposta impostazioni e testi salvati da `HKCU\Software\PasswordPaste`. L'utente è italiano: risposte in
italiano. L'interfaccia è in inglese (la lingua di default, voluta dall'utente per renderla internazionale),
spagnolo, francese e italiano. I commenti nel codice e nei file di configurazione sono in inglese (il progetto è
condiviso su GitHub).

## File

- `src\` — il programma, un file per responsabilità (fino alla 2.0 era un solo `SmartClip.cs`, diviso quando ha
  superato le 2000 righe; nessuno spazio dei nomi, le classi si vedono tutte tra loro):
  - `Program.cs` — avvio, istanza unica, attributi dell'assembly, la cartella dei dati (`Program.DataFolder`:
    `%LOCALAPPDATA%\SmartClip`, dal pacchetto `LocalState`), gli errori che nessuno ha preso
    (`Application.ThreadException` → `Report`: nel log e in una `AskForm` con `Strings.UnexpectedError`, solo il
    primo per sessione; il programma continua).
  - `Log.cs` — il registro degli errori, `SmartClip.log` nella cartella dei dati (ricomincia oltre 256 KB, il
    precedente resta come `.old`). Mai il contenuto di ciò che si copia o si salva. Lo scrivono `Report`,
    `ShowError` (gli errori della riga di stato), le impostazioni illeggibili, i file della cronologia rifiutati
    dal disco o illeggibili. Non lo scrive `Texts.Remember`: gli appunti occupati da un altro programma sono normali.
  - `MainForm*.cs` — la finestra, una sola classe `partial` divisa in cinque file quando ha superato di nuovo
    le 2000 righe (i nomi dei membri non sono cambiati: i test li cercano per riflessione):
    - `MainForm.cs` — costanti e campi (tutti qui, in cima), costruttore con la disposizione, viste, riga di
      stato, tray, icona, messaggi di Windows, chiusura.
    - `MainForm.List.cs` — le schede delle voci (`cardList`): `WireLists` (menu, mouse), riempimento (`Filter`,
      `Arrange`, `ShowRows`), misura e disegno delle schede (`CardHeight`, `Lay`, `DrawEntry`, `PartAt`), anteprima,
      scelta, modifica, cartelle, tasti dell'elenco.
    - `MainForm.Nav.cs` — la barra laterale (`navList`, i posti `Place`/`PlaceKind`; `footList` sotto), il suo
      menu (`navMenu`) e il trascinamento nelle cartelle (`Draggable`).
    - `MainForm.Options.cs` — `PlaceOptions` (le opzioni e i loro suggerimenti), i `Place…`, esportazione,
      importazione, pulizia, `TransferSettings` e `AdoptOldSettings`, e `OptionsWindow`, la finestra delle
      opzioni (classe annidata in `MainForm`, così usa i suoi campi).
    - `MainForm.Hotkeys.cs` — le scorciatoie globali: nomi, cambio dei tasti, registrazione, e l'incolla delle voci numerate (`PasteNumber`).
    - `MainForm.Typing.cs` — la digitazione: `Run`, `WaitForTarget`, esito.
  - `EditForm.cs` — la finestra «Nuovo/Modifica».
  - `HelpForm.cs` — la guida (pulsante «?» o F1), divisa in argomenti scelti da una colonna a sinistra (voluta
    dall'utente: tutta di fila era illeggibile), disegnata come le righe dell'elenco (`DrawTopic`); a destra
    solo l'argomento scelto (`Write`). Il testo è `Strings.Guide()`, uno per lingua (`EnglishGuide` in
    `Strings.cs`, gli altri nei file delle lingue), una riga per paragrafo (`# ` apre un argomento e gli dà il
    nome, che deve stare nella colonna; `- ` punto, tabulazione tra un tasto e ciò che fa), e prende nomi, tasti
    e numeri dalle costanti pubbliche di `MainForm` e `Texts` e dagli altri campi di `Strings`. Le quattro guide
    hanno gli stessi argomenti nello stesso ordine (lo controlla `tests.exe texts`). Non è modale: resta aperta
    accanto alla finestra. **Va aggiornata in tutte le lingue quando cambia una funzione**, come il README.
  - `Strings.cs` — **ogni testo che il programma mostra**, come campo statico con il valore inglese (inglese =
    default ed elenco unico dei testi), più la scelta della lingua (`Use`, una volta sola all'avvio da `Main`,
    prima di ogni finestra). `Strings.Italian.cs`, `.Spanish.cs`, `.French.cs` sostituiscono i valori; ciò che una
    lingua dimentica resta in inglese. I testi con valori usano `{0}`, `{1}` e `string.Format`. Le date dei
    giorni (`DayFormat`) e le virgolette (`Quote`) cambiano con la lingua; i nomi dei tasti Maiusc/Canc/Invio
    sono quelli delle tastiere di ogni lingua (`ShiftName`, usato anche da `MainForm.NameOf`).
  - `Look.cs` — colori chiari e scuri, caratteri (`Look.TextFont`, `StrongFont`, `TitleFont`, `NoteFont`, `EntryFont`),
    icone (`Draw`, colore proprio in `Own`), righe (`DrawRow`, `DrawTopic`, la tacca `MarkIn`/`DrawMark`), forme
    (`Rounded`, `Pill`), menu (con i segni delle voci, `Look.Mark`), suggerimenti e controlli disegnati: `Look`,
    `Card` (anche `Quiet`, senza bordo di focus: il pannello di destra), `RoundButton` (anche `Subtle`), `Toggle`
    (anche `SaysState`), `Tile` (una scheda delle opzioni), `TabStrip` (le schede dei tipi sopra le voci),
    `InfoBar` (un messaggio con il suo tipo, `Notice`), `EmptyNote` (ciò che dice un elenco vuoto). Anche
    `Window`, la base di tutte le finestre tranne `Peek`: carattere e colori
    dell'interfaccia, barra del titolo (`Caption`), `Finish` alla fine del costruttore (la scala, vedi le trappole),
    `Secondary` (le finestre che appartengono a un'altra: niente pulsante sulla barra, icona, ingrandisci e riduci),
    `NewLabel`, `PlaceOnScreen`, le misure dei pulsanti.
  - `Motion.cs` — ciò che si muove: `Fade` (un valore che va verso 0 o 1 a piccoli passi), `Glow` (la luce sotto
    il puntatore nelle righe di un elenco; `Glow.Row` è la riga sotto il puntatore), `RowList` (un `ListBox` con la
    sua `Glow`, che avvisa quando scorre e disegna la tacca della riga selezionata, facendola scivolare alla
    nuova; `RowAt` dice la riga sotto un punto, `LightUnderPointer` basta agli elenchi che non chiedono altro),
    `Entrance` (una pagina che sale al suo posto), `Rail` (la barra di scorrimento sottile).
  - `AskForm.cs` — le domande e gli avvisi, al posto di `MessageBox` (`AskForm.Ask`, `AskForm.Tell`).
  - `Peek.cs` — il riquadro che mostra una voce per intero (`Peek`), accanto alla sua riga.
  - `Texts.cs` — `Entry` (una voce dell'elenco, o la riga di una cartella) e `Texts`: la lettura degli appunti
    (`Remember`, che passa la copia a `Clips`), i formati riservati, la ricerca, i percorsi delle cartelle, il tipo
    di un testo (`Category`, `Texts.CategoryOf`: link se è solo un indirizzo, codice se due righe su cinque hanno
    l'aspetto di codice o la sola riga ce l'ha, un'ipotesi).
  - `Backup.cs` — esportazione e importazione in un file di testo (`Backup`, `BackupResult`).
  - `Rich.cs` — i testi salvati formattati (vedi sotto): `Run` (un pezzo di testo tutto uguale; ciò che non
    imposta è «predefinito»), `Rich` (HTML e RTF degli appunti, l'intestazione di «HTML Format» in byte UTF-8,
    le ultime copie formattate in memoria, conversioni HTML/RTF/casella ↔ `Run`, con `HtmlReader` che legge
    l'HTML come lo mostra un browser) e `RichBox`, la casella del contenuto di `EditForm`.
  - `Typer.cs` — mappa dei tasti e invio (`Typer`, `Stroke`, `TypeOptions`, `TypeResult`).
  - `Native.cs` — funzioni e costanti di Windows.
  - `Sqlite.cs` — lo strato sul SQLite incluso in Windows (`winsqlite3.dll`, `DllImport` con `StdCall`, provato
    anche a 32 bit): `Database` (apre, `Execute` anche di più istruzioni, `Prepare`, `Value`, `Begin` per le
    transazioni, che sono savepoint e quindi si annidano), `Statement` (`Step`, `Run` per ripeterla con altri
    valori, letture per colonna), `SqliteException` (`IsConstraint`). I valori si legano sempre a `?1`, `?2`…,
    mai scritti nel testo dell'istruzione, e i messaggi d'errore riportano l'istruzione ma non i valori. Testi
    legati con la loro lunghezza (il carattere zero non li taglia), byte vuoti con `zeroblob` (altrimenti
    diventerebbero NULL). Chiudere il database finalizza le istruzioni rimaste aperte, così il file si libera.
    Lo usa `Clips`. Senza la DLL, o con una più vecchia di `Database.MinimumVersion` (3.8.0, gli indici
    parziali), `Main` lo dice con `Strings.SqliteMissing`/`SqliteTooOld` e non parte (`Program.SqliteProblem`):
    nemmeno il ripiego in memoria funzionerebbe. Portarsene una propria non serve (ogni Windows 10 e 11 ce l'ha, e
    Windows Update la tiene aggiornata).
  - `Clips.cs` — i dati dell'elenco in un file SQLite (dalla 4.0; `docs\piano-sqlite.md`). Una tabella `entries` per copiati e salvati (`saved` 0/1; un salvato può non
    avere nome, è cifrato con DPAPI, contenuto e miniatura, e non scade mai; un copiato scade anche se in alto) e
    una `folders` ad albero (`parent_id`, 0 = fuori da tutte). Le regole le fa rispettare lo schema stesso
    (`Clips.Schema`: CHECK, UNIQUE, chiave esterna): nome e cartella solo sui salvati, niente freccia, testo per
    la ricerca né impronta sui salvati, numeri 1–`MaxNumber` unici, nomi unici senza maiuscole (`Key`, perché
    NOCASE di SQLite conosce solo le lettere inglesi). Versione in `PRAGMA user_version` (`SchemaVersion`): un
    file più nuovo si rifiuta (`Strings.NewerDatabase`), uno più vecchio si aggiorna all'apertura (`Upgrade`, in una
    transazione). Versione 2 (17/10/2026): nelle righe di `entries` le colonne grandi stanno in fondo (`hash`,
    `folded`, `preview`, `content` ultimo), perché SQLite tiene un valore grande in una catena di pagine e la legge
    tutta per arrivare a qualunque colonna dopo di lui: con `content` al terzo posto ogni ricarica dell'elenco e ogni
    ricerca leggevano le immagini intere. **Le colonne nuove vanno messe prima di `hash`**. Versione 3: `rtf` e
    `html` dei testi salvati formattati, cifrati (lo schema li vuole solo su un salvato di testo, l'RTF solo
    accanto all'HTML); `Upgrade` rifà la tabella anche per questo, perché `ADD COLUMN` li metterebbe in fondo. Versione 4 (18/10/2026):
    `app_id` di un copiato, verso la tabella `apps` (percorso dell'eseguibile, nome, icona in PNG; mai su un salvato);
    `Upgrade` crea `apps` e rifà di nuovo la tabella. Versione 5 (18/10/2026): `category` di un copiato (`Category`:
    testo, codice, link, immagine; mai su un salvato), per le schede dei tipi; `Upgrade` la calcola per le righe che
    c'erano (`Categorize`). È nata separata dalla 4 perché il database vero dell'utente era già passato a una 4 senza
    di lei (errore «no such column: category»): **una versione già aperta da un eseguibile non si ridefinisce mai**,
    se ne fa una nuova. Versione 6 (18/10/2026): `chosen`, il tipo detto dall'utente con «Tipo» nel menu di un testo
    (`ListKinds`, `Clips.Choose`; voluto dall'utente, perché l'ipotesi sul codice sbaglia): vale anche su un salvato,
    **in chiaro** (deciso con l'utente: non dice più del nome, che è già in chiaro); un salvato senza scelta non ha
    `category` e si indovina in memoria. Copiato di nuovo o salvato, il tipo scelto resta. Ore in UTC, millisecondi dal 1970. Copiare di nuovo
    aggiorna la stessa riga (`hash`). `Entry` ha in più `Id`, `Saved`, `FolderId`. Un database nuovo si riempie
    dentro la transazione che lo crea (`new Clips(path, fill)`): se il riempimento fallisce non resta niente, e al
    prossimo avvio è di nuovo nuovo (`Created`).
  - `Migration.cs` — porta nel database nuovo, una volta sola, ciò che le versioni precedenti tenevano altrove
    (passo 3 del piano): i salvati dal registro, ancora cifrati così com'erano (anche quelli che qui non si
    leggono), con giorno, numero e cartella (i percorsi diventano l'albero); i copiati dai file `.clips`, con
    stellina (che diventa la freccia) e numero, senza i record eliminati e fino a un record troncato. **Legge
    soltanto** (lo controlla un test byte per byte): se fallisce, al prossimo avvio i dati vecchi ci sono ancora.
    Dopo, appena un database è aperto, `OpenClips` chiama `Migration.ForgetOld`, che **cancella la cartella
    `History`** (voluto dall'utente il 17/10/2026: «nessun History»), anche agli avvii successivi se c'è ancora; dal
    20/10/2026 (passo 8 del piano, voluto dall'utente) anche la chiave `Saved` del registro e le impostazioni che
    nessuno legge più (`OldSettings`: `Font`, `TextSize`, `SavedWidth`). Un'impostazione che smette di servire va
    aggiunta lì. Nessuna scadenza durante la
    migrazione. Un numero conteso va alla prima voce, un nome già preso prende « (2)», un file illeggibile si salta e
    va nel log. È l'unico a conoscere il formato vecchio (la cartella è `Migration.OldHistory`). Provata il
    17/10/2026 in sola lettura sui dati veri dell'utente (4 salvati, 16 copiati, 2 immagini: tutto, dati vecchi
    intatti).
  - `Packaged.cs` — quando il programma gira dal pacchetto del Microsoft Store (`Packaged.Family` non nullo):
    avvio con Windows con la `StartupTask` del pacchetto invece della chiave `Run` (`Startup` lo chiama), avvio
    all'accesso riconosciuto dall'attivazione invece che da `/tray`, database e log in `LocalState` del pacchetto
    (quella sotto `%LOCALAPPDATA%` Windows la sposterebbe in una copia nascosta che Esplora file non vede). I suoi
    tipi di Windows Runtime fuori dal pacchetto non vanno chiamati (quelli di `Ocr` sì).
  - `Origin.cs` — il programma in cui è stata copiata una voce (`Origin`: nome e icona), vedi «App di provenienza» sotto.
  - `Ocr.cs` — il testo di un'immagine letto dal riconoscimento di Windows (`Windows.Media.Ocr`, sul PC, senza
    rete; funziona anche fuori dal pacchetto): vedi «Copia il testo» sotto.
- `packaging\` — il pacchetto MSIX per lo Store: `AppxManifest.xml` (identità da Partner Center, *Product
  management → Product identity*: `lsgag.SmartClipClipboardTyper`, editore `ls_gag`; il nome sullo Store è «SmartClip – Clipboard & Typer», scelto
  dall'utente perché «SmartClip» era preso e «ClipSmart» è già un gestore di appunti per Chrome: sta in
  `DisplayName` identico a quello prenotato, mentre nel programma resta «SmartClip») e `pack.ps1` (immagini da `SmartClip.ico`, versione da
  `AssemblyVersion`, poi `makepri` e `makeappx` del Windows SDK; senza SDK scrive solo `out\layout`). Lo Store
  vuole la quarta cifra della versione a zero. Il flusso di release crea il pacchetto e lo lascia tra gli
  artifact (non nella release: senza la firma dello Store non si installa); lanciato a mano, lo crea anche se
  la release esiste già.
- `store\` — la scheda dello Store pronta da copiare in Partner Center (`README.md`: passi, una scheda per lingua
  con descrizioni, caratteristiche, termini di ricerca e didascalie, note per i tester sull'interfaccia
  inglese) e le immagini (`images\<lingua>\`, da `tests.exe store store\images <lingua>` a programma chiuso;
  `images\tile-300.png` è unica). Ciò che si carica in Partner Center sta tutto in `store\upload` (fuori da git,
  voluto dall'utente: una cartella sola): `make-listing.ps1 [<csv esportato da Partner Center>]` scrive
  `store\upload\SmartClip-listing` (CSV compilato per tutte le lingue + immagini) da importare con *Store
  listings → Import listings → Import folder*; senza argomento riparte dal `listing.csv` scritto la volta prima
  (il CSV si legge con `TextFieldParser`: le descrizioni stanno su più righe). `get-package.ps1` (`-Build` per
  avviare il flusso) scarica lì accanto il `.msix` della versione di `Program.cs` dagli artifact di GitHub. I
  passi di un invio li segue la skill `partner-center` (`.claude\skills\partner-center\SKILL.md`). `make-listing.ps1` legge i testi da `README.md`, quindi i blocchi di codice di
  ogni lingua vanno tenuti in quell'ordine (nome, descrizione breve, descrizione, caratteristiche, termini, novità della versione da riscrivere a ogni invio) e i
  titoli delle sezioni con il codice tra parentesi, «## English (en-US)». `PRIVACY.md` è l'informativa indicata allo Store: va tenuta vera (oggi il programma non
  usa la rete).
- `build.cmd` — compila `src\*.cs` in `SmartClip.exe`. `SmartClip.ico` è facoltativa. L'eseguibile non sta
  più in git (`.gitignore`): si scarica dalle release.
- `.github/workflows/release.yml` — pubblica l'eseguibile come release di GitHub. **Per fare una release si
  cambia `AssemblyVersion` in `src\Program.cs` e si fa push**: il flusso esegue prima i controlli
  (`tests\test.cmd ci`, a ogni push, anche senza release: se falliscono la release non si fa), poi compila con
  `build.cmd` e crea la
  release `vX.Y.Z` solo se con quel numero non esiste ancora (scelta dell'utente, invece di una release a
  ogni push). Il numero di versione sta solo lì; le opzioni lo mostrano in fondo alla pagina «Programma»
  (`Program.Version`; fino alla 4.2.1 stava nel titolo della finestra, che ora dice solo «SmartClip»). La
  quarta cifra conta solo se non è zero: `2.1.0.0` è `v2.1.0`, `2.1.0.1` è `v2.1.0.1`.
- `tests/Test.cs`, `tests/test.cmd` — controlli automatici (vedi sotto).
- `LICENSE` — licenza MIT.
- `README.md` — presentazione per GitHub, in inglese. Cita le etichette inglesi delle opzioni: va aggiornato
  quando se ne aggiunge o rinomina una. Mostra `docs/screenshot.png`, `docs/screenshot-options.png` e `docs/screenshot-edit.png`, fatti con
  `tests.exe real <prefisso> en` (immagini `-real-list`, `-real-options` e `-real-edit`, con voci finte e a
  programma chiuso: altrimenti le scorciatoie risultano occupate)
  compilato aggiungendo `/resource:SmartClip.ico,SmartClip.ico`, altrimenti manca l'icona.

- `.github/workflows/codeql.yml` — analisi CodeQL su GitHub con compilazione vera (`build-mode: manual`, lancia
  `build.cmd < NUL` su Windows). Richiede che la configurazione predefinita di Code scanning sia disattivata.
  `.github/dependabot.yml` aggiorna solo le azioni di quel flusso: il programma non ha pacchetti.

## Vincoli di compilazione

- Si usa il `csc.exe` incluso in Windows (.NET Framework 4, **C# 5**): niente interpolazione di stringhe, `?.`,
  `out var`, membri a espressione, `readonly struct`, costruttori primari. I suggerimenti di VS Code che li
  propongono vanno ignorati. Lo scopo è poter ricompilare sul PC del lavoro senza installare nulla.
- Il sorgente è UTF-8 senza BOM con lettere accentate: serve `/codepage:65001` (già in `build.cmd`).
- `Packaged.cs` e `Ocr.cs` usano Windows Runtime: `build.cmd` e `test.cmd` riferiscono i `.winmd` che servono
  (`ApplicationModel`, `Foundation`, `Globalization`, `Graphics`, `Media`, `Security`, `Storage`) da
  `%WINDIR%\System32\WinMetadata` e `System.Runtime.dll` accanto a `csc`, tutti già in Windows 10 e 11. Niente
  `AsTask` né `AsBuffer` (vorrebbero `Windows.winmd` dell'SDK): le operazioni asincrone si aspettano con
  `Completed` (`Packaged.Wait`), i byte diventano un `IBuffer` con `CryptographicBuffer.CreateFromByteArray`.
  Compilando a mano vanno aggiunti gli stessi `/r:`.
- `build.cmd` e `test.cmd` finiscono con `pause`: da riga di comando lanciarli con `< NUL`.
- La compilazione fallisce se `SmartClip.exe` è in esecuzione (può essere nascosto nel tray): controllare
  prima con `Get-Process SmartClip`.

## Come funziona

- Pulsante: legge gli appunti, poi aspetta che la finestra di un altro programma resti in primo piano per N
  secondi (`WaitForTarget`), quindi digita. Scorciatoia Ctrl+Alt+V: digita subito nella finestra attiva.
- La digitazione si ferma se la finestra di destinazione perde il primo piano: una password non deve finire
  altrove. Desktop, barra delle applicazioni e menu Start non sono mai destinazioni valide.
- I tasti si inviano come scancode (`SendInput`), AltGr come Alt destro esteso. La mappa carattere → tasto si
  costruisce con `ToUnicodeEx` sui soli tasti del blocco alfanumerico (`MapLayout`).
- Caratteri senza tasto (su tastiera italiana: `~`, `` ` ``, `È`): inviati come Unicode, oppure con Alt+codice
  sul tastierino se l'opzione è attiva.
- «Tastiera del server: Inglese (USA)» usa una tabella fissa (`UsKeys`) invece della tastiera locale.
- Impostazioni in `HKCU\Software\SmartClip`, lette e scritte da un unico elenco (`TransferSettings`).
- Tray: con l'opzione attiva, X e riduzione a icona nascondono la finestra; si esce da «Esci» nel menu
  dell'icona. Una sola istanza: un secondo avvio fa ricomparire la finestra esistente.
- La finestra non è più sempre in primo piano (l'utente non lo voleva): `TopMost` vale solo mentre `Run`
  aspetta e digita, perché conto alla rovescia e «Annulla» restino visibili sopra la finestra remota. Guida e
  `EditForm` stanno sopra perché ne sono proprietarie. «Nascondi passando a un altro programma» (`leaveBox`,
  impostazione `HideOnLeave`, accesa di default, scelta dell'utente) nasconde la finestra nel tray a
  `WM_ACTIVATEAPP`, solo con il tray attivo, mai durante la digitazione né con la finestra disabilitata da
  una finestra modale nostra (`IsWindowEnabled`: «Modifica», domande, scelta del file), così intanto si può
  copiare altrove. La guida si chiude e le opzioni si nascondono con lei (`PutAwayOthers`, anche nascondendola nel
  tray): senza pulsante sulla barra resterebbero perse.

- Avvio con Windows (`Startup`, in `Program.cs`): un valore nella chiave `Run` dell'utente, con l'argomento
  `/tray`. Non è tra le impostazioni (`TransferSettings`): la spunta mostra ciò che dice Windows, così non
  esistono due verità. Risulta spenta anche se la voce è stata disattivata da Gestione attività
  (`StartupApproved\Run`, primo byte dispari); riattivandola da qui quel blocco si toglie. A ogni avvio il
  comando si riallinea al percorso dell'eseguibile (`Follow`, chiamato da `Main` e non da `MainForm`: i test
  lo punterebbero su `tests.exe`). Con `/tray` la finestra non si mostra (`SetVisibleCore`, che crea comunque
  l'handle: scorciatoie e appunti dipendono da quello); con il tray spento parte ridotta a icona. Un secondo
  avvio con `/tray` non fa ricomparire la finestra di quello già aperto.

- Icona (tray e finestra) a colori se negli appunti c'è testo, grigia se non c'è: `ShowClipboardState`, avvisato
  da `WM_CLIPBOARDUPDATE`. Guarda solo se il formato testo è disponibile, senza aprire gli appunti. La versione
  grigia si genera all'avvio dall'icona normale (`Gray`).

- La finestra principale è l'elenco, con una barra laterale e le voci in schede (voluto dall'utente il 18/10/2026;
  prima erano due colonne affiancate, copiati e salvati). A sinistra la barra laterale (`navList`,
  `MainForm.Nav.cs`): «Cronologia» (`PlaceKind.History`, fino alla 4.3.5 «Tutti»), «In alto» (i copiati con la
  freccia), «Scorciatoie» (`PlaceKind.Shortcuts`: le voci con un numero, copiate e salvate, per numero, senza giorni;
  voluto dall'utente il 19/10/2026 per vederle e gestirle in un posto solo; `Sieve.Numbered`), «Salvati» con sotto l'albero delle cartelle, «Provenienza» con sotto i programmi (icona e quante voci),
  ognuno un `Place`; la freccia a destra di una riga apre
  e chiude ciò che ha sotto (Salvati e Provenienza nelle impostazioni `SavedOpen`, `SourcesOpen`; le cartelle nel
  database), un clic su «Provenienza» pure. Ridotta, una linea sottile sopra «Salvati» e sopra «Provenienza» separa
  cartelle e programmi, che con le sole icone si confondevano, e ogni cartella è piena (`Sign.FullFolder`) con
  l'iniziale del nome ritagliata nel colore della riga (`InitialShare`, `InitialDrop`; voluto dall'utente il 19/10/2026:
  tutte uguali, non si distinguevano; nella cartella vuota la lettera toccava i bordi); con i nomi, un nome tagliato compare intero nel
  suggerimento (`NameCut`, `RowTip`; `PlaceParts` misura la riga per disegno e suggerimento). Sotto, `footList`: Nuova cartella, Sospendi/Riprendi la cattura, Opzioni, Guida. A destra, su un
  pannello più chiaro (`pane`, un `Card` `Quiet`): titolo del posto con accanto, piccolo e grigio, il conteggio (`PlaceTitle`,
  sulla stessa linea di base; prima stava all'altro capo del pannello), ricerca con «Nuovo…» e «Digita gli
  appunti», le schede dei tipi (`tabs`, `TabStrip`: Tutti, Testo, Codice, Link, Immagini, scelte dell'utente),
  l'elenco delle schede delle voci (`cardList`), la riga di stato. «Cronologia» mostra solo i copiati (voluto
  dall'utente il 19/10/2026: prima mescolava copiati e salvati, e una password salvata mesi fa finiva in fondo tra le
  copie vecchie), quelli in alto prima e il nome del giorno sopra ogni giorno (`Heading`, mai selezionabile). Il primo
  nome non è una riga: lo dice la barra fissa sopra le schede (`dayBar`, `DayBar`, `topDay` tolto da `rows` in `Filter`,
  disegnata come le righe da `DrawHeading`; le schede le fanno posto sotto, `PlaceDay`), che scorrendo dice il giorno
  della prima scheda in vista, cioè l'ultimo nome prima di `TopIndex` (`ShowDay`, da `RowList.Scrolled`; voluta
  dall'utente il 20/10/2026: tra le schede di ieri, che dicono solo l'ora, il nome del giorno era salito fuori vista).
  Un `ListBox` scorre per righe intere, quindi una barra sopra l'elenco ne avrebbe coperto la prima scheda: per questo
  le schede partono sotto di lei, e con un nome in cima, subito sotto la barra, la barra dice ancora il giorno prima;
  la rotella sopra di lei scorre le schede. **Cercando**
  vi compaiono anche i salvati che corrispondono, dopo i copiati, per nome sotto un `Heading` «Salvati» (scelta
  dell'utente: Ctrl+D, il nome e Invio devono continuare a trovare una password salvata). Le parole trovate sono
  evidenziate sulle schede, nel testo e nel nome di un salvato (voluto dall'utente il 19/10/2026: una scheda trovata
  per una parola alla quinta riga non diceva perché c'era): `Texts.Find` dice dove stanno nel testo vero, accenti
  compresi; il testo segnato si dispone a mano (`MarkedText`, `LayMarked`, `Fit`, con gli stessi a capo di Windows) e si
  scrive a pezzi su una forma tinta d'accento (`DrawMarked`, `MarkTint`); trovate oltre ciò che la scheda mostra, la
  scheda parte da poco prima della prima (`MarkContext`) dopo «…». Si ricorda per scheda fino alla ricerca dopo
  (`marks`); senza ricerca, e per le immagini, la scheda si scrive come sempre. «Salvati» e una
  cartella (con le sue sottocartelle) mostrano i salvati per nome, senza giorni. I copiati li filtra il database
  (`Clips.Copied` con un `Sieve`: parole, tipo, programma, solo in alto; al massimo `MaxShown`), i salvati la memoria
  (`Texts.Matches`: parole in qualsiasi ordine, senza maiuscole né accenti). `Filter` rifà anche la barra (`FillNav`,
  i conteggi cambiano a ogni copia); un posto che sparisce (cartella eliminata, programma senza più voci) lascia
  «Cronologia». Il
  testo di ogni voce si prepara una volta sola senza accenti e in minuscolo (`Texts.Fold`, `Entry.Folded`) e poi si
  confronta ordinalmente: dieci volte più veloce del confronto culturale con `IgnoreNonSpace` (misurato: 45 → 4,6 ms
  su 5000 voci), che però trovava anche «ae» in «æ». Il
  pulsante «Opzioni» apre le opzioni in una finestra a parte (voluto dall'utente il 17/10/2026, «come la guida»;
  prima prendevano il posto dell'elenco, con un pulsante «Elenco» per tornare): vedi sotto. Le idee vengono dal progetto vicino `..\pastepad` (WinUI 3, .NET 10), riscritte qui in
  piccolo: niente modelli, formato, temi.
- Scegliere una voce («Copia», doppio clic o Invio, `Use`; un clic sulla scheda la seleziona soltanto, voluto dall'utente il 18/10/2026: copiava anche lui e «Copia» era ridondante) la rimette soltanto negli appunti: poi l'utente incolla con Ctrl+V o
  digita con Ctrl+Alt+V. Non si digita né si incolla da soli (provato e scartato dall'utente; l'eccezione voluta
  sono i numeri delle voci, sotto). Un testo salvato
  torna negli appunti con il formato che lo segna riservato; ciò che il programma stesso mette negli appunti
  non rientra nell'elenco (`ownCopy`, numero di sequenza degli appunti).
- Più schede insieme (voluto dall'utente il 20/10/2026, scelte sue: come Esplora file, con una barra, e «Sposta in»
  salva i copiati): Ctrl+clic ne aggiunge o toglie una, Maiusc+clic (o Maiusc con le frecce, `PickStep`) prende quelle
  dalla scheda di partenza (`pickFrom`) a questa, Ctrl+A nelle schede tutte (`PickAll`, `AllKeys`, tra i `ListKeys`).
  Il `ListBox` resta a selezione singola: gli id scelti stanno in `picked` (mai uno solo: allora è la scheda
  selezionata, `ShowPicks`; `Many`), e i clic con Ctrl o Maiusc non arrivano a Windows (`RowList.Picking`, da
  `WM_LBUTTONDOWN`), così tutto ciò che usa `SelectedIndex` non è cambiato. Un clic senza tasti, le frecce, un altro
  posto, una ricerca o un'altra scheda dei tipi tornano a una scheda sola (`ClearPicks`, anche da `SelectedIndexChanged`
  fuori da `keepPicks`); una ricarica tiene quelle ancora in elenco (`ShowRows`). Le scelte hanno il bordo d'accento
  (`DrawEntry`); al posto delle schede dei tipi compare `pickBar` (`ShowPickBar`, `PlacePickBar`): quante, «Tieni in
  alto» (solo se c'è un copiato; tutte in alto, o tutte giù se lo sono già: `PinSelected`), «Sposta in» (`ListMoves`,
  `MoveAll`: un copiato si salva lì senza nome, formattato se è tra le ultime copie, `Rich.Kept`), «Elimina»
  (`DeleteSelected`, chiede sempre per più di una e dice quanti salvati ci sono) e ✕ (`Sign.Cancel`). Le stesse nel menu
  del tasto destro (un clic destro su una scelta le tiene) e su Ctrl+P, Ctrl+E; Esc le lascia prima di tornare indietro;
  Invio e Ctrl+S non fanno niente. I pulsanti di una scheda agiscono solo su lei (il clic toglie le altre). Il
  trascinamento in una cartella resta di una scheda sola.
- Ctrl+D (`OpenList`) porta la finestra in primo piano da qualsiasi programma; dopo la scelta con Invio o
  doppio clic `GoBack` restituisce il primo piano alla finestra di partenza e rinasconde la finestra se era
  nascosta. «Copia» no (voluto dall'utente: la finestra spariva e la voce non si poteva più salvare).
  Ctrl+D apre sempre su «Cronologia», scheda dei tipi «Tutti», ricerca vuota (`ShowHistory`; voluto dall'utente il 19/10/2026),
  con selezionata l'ultima copia e la cima dell'elenco in vista; lo stesso dopo ogni copia entrata (`newest`, letto da
  `ShowRows`, anche se la copia è in alto sotto altre). Voluto dall'utente il 19/10/2026: la scheda selezionata prima
  restava selezionata e l'elenco scorreva fino a lei, così la copia nuova restava fuori vista finché non si cambiava
  scheda; e a elenco già su «Tutti» (oggi «Cronologia») Ctrl+D non lo riempiva di nuovo. Le altre ricariche tengono la selezione.
  Con l'opzione «Riapri dove l'hai lasciato» (`whereLeftBox`, nelle Scorciatoie, spenta di default, impostazione
  `OpenWhereLeft`) tiene posto e tipo. Vale solo per la scorciatoia: riaprire dal tray o con un secondo avvio lascia
  tutto com'era.
- Copiati e salvati stanno in un solo database SQLite (dalla 4.0, `docs\piano-sqlite.md`; prima i salvati
  nel registro e la cronologia in un file al giorno): `Clips.DefaultFile`, cioè `SmartClip.db` nella cartella
  dei dati (`Program.DataFolder`), o nella cartella scelta dall'utente (vedi sotto), sempre con il log accanto. `MainForm` lo apre in `OpenClips`; la prima volta vi porta i
  dati vecchi (`Migration`, vedi sopra). Se non si apre (bloccato, rovinato, scritto da una versione più nuova,
  migrazione fallita) l'elenco lavora su un database in memoria (`Clips.Memory`) e la riga di stato lo dice
  sempre (`unavailable`, che `ShowIdle` rimette); il log ha il perché, e al prossimo avvio si riprova.
- La cartella del database si sceglie (voluto dall'utente, passo 6 del piano): impostazione `DataFolder` (vuota
  = predefinita), letta da `OpenClips` prima delle altre (`EarlySetting`) e scritta subito quando cambia
  (`SaveSettings` in `MoveDataFolder`). Nella pagina Cronologia la riga «Cartella delle voci copiate e salvate»
  (`folderPath`, `ShowFolder`), sotto quanto occupa il file e quanto le immagini copiate (`databaseSize`,
  `ShowSize`, `Clips.Size`, `Strings.Size`), con «Cambia…» (`ChangeDataFolder`, `FolderBrowserDialog`) e «Predefinita».
  Dove non c'è un database, quello in uso ci si sposta (`Clips.Move`: chiude, copia, apre la copia e la controlla
  con `quick_check`, solo allora cancella il vecchio; se qualcosa fallisce `ClipsMoveException` riporta indietro
  il vecchio, intatto); dove c'è già, chiede se usare quello e lascia il proprio com'è (la terza scelta del piano,
  «sostituiscilo», non c'è: troppo facile perdere dati). Una cartella in rete o sincronizzata (OneDrive, Dropbox,
  Google Drive, iCloud: `Synced`) chiede prima. **Solo nella cartella predefinita nasce un database nuovo** (e solo
  lì la migrazione): se una cartella scelta non ha più il suo file all'avvio, l'elenco lavora in memoria e lo dice
  (`FolderUnavailable`), così non si ritrova un elenco vuoto senza saperlo.
- La cronologia dei copiati resta per i giorni dell'opzione (`Clips.Keep`, 30 di default), contando oggi: **0 vuol
  dire solo le cose di oggi** (deciso con l'utente il 17/10/2026; prima era «solo in memoria», che non esiste
  più). È **in chiaro** (l'utente non vuole cifratura: «sono cose mie sul mio PC»). La scadenza (`Prune`)
  passa a ogni avvio e al primo inserimento di ogni giorno; una voce tolta si sovrascrive nel file
  (`secure_delete`) e il file si restringe a poco a poco (`auto_vacuum = INCREMENTAL`, `Compact`). Copiare di nuovo
  la stessa cosa aggiorna la stessa riga (impronta `hash`): torna in cima con l'ora nuova, tenendo freccia e
  numero. Si scartano le copie che i gestori di password segnano come riservate (`Texts.IsPrivate`). L'opzione
  spenta cancella tutti i copiati (`ClearCopied`). L'elenco ne chiede al database solo `MaxShown`, già filtrati
  dalla ricerca (`Clips.Copied`, con `instr` sul testo per la ricerca `folded`): a ogni copia si rileggono solo i
  copiati (`ReloadCopied`), mai i salvati, e solo se la copia è entrata (`Texts.Remember` dice se qualcosa è
  cambiato: non con la cronologia spenta o in pausa, né per una copia riservata o fatta dall'elenco).
- Una voce copiata si può tenere in cima **per un po'** (`Entry.Pinned`, la freccia, voluta
  dall'utente al posto della stellina il 17/10/2026; nella barra il posto «In alto»): freccia a destra della prima riga della
  scheda (`Part.Pin`: lì il clic non sceglie la voce, `Sign.Up`), menu e Ctrl+P, tutti in `TogglePin`. **Scade come
  le altre**, e «Pulisci ora…» la toglie (l'utente: «se vuoi salvarlo lo salvi con l'apposita funzione; la freccia
  serve temporaneamente a tenere in alto una voce»). `Forget` (la password digitata) non la toglie, né toglie una
  voce con un numero.
- Numeri delle voci (voluti dall'utente nella 3.9.2): un testo salvato o una voce copiata può avere un numero da 1
  a `MaxNumber` (9); i modificatori dell'opzione «Incolla per numero» (`quickKeys`, Ctrl+Maiusc di default:
  con Ctrl+Alt le cifre di molte tastiere scrivono simboli con AltGr) più la cifra sono una scorciatoia globale che
  mette la voce negli appunti e la **incolla** nella finestra in primo piano con Ctrl+V, senza mostrare la
  finestra (`PasteNumber`, `Typer.Paste`, che aspetta il rilascio dei tasti e manda Ctrl più il V della tastiera
  di quella finestra come scancode). Voluto dall'utente: la prima versione, Ctrl+D tenendo Ctrl e poi la cifra
  nell'elenco, apriva la finestra e non incollava, ed è stata scartata. È l'unico caso in cui il programma
  incolla da solo. Si registrano solo i numeri che qualche voce ha (`HookNumbers`, con una sola domanda al database,
  `Clips.Numbers`, perché passa a ogni copia; rifatto a ogni `Reload` e da
  `UpdateHotkeys`; id `NumberId` + numero), così gli altri restano agli altri programmi; quelli che un altro
  programma ha già si dicono nella riga di stato una volta (`numbersTaken`, azzerato quando l'utente assegna un
  numero). I tasti si cambiano come le altre scorciatoie (`AwaitKeys` con `quickBox`, `TakeQuickKeys`): si
  premono i modificatori con una cifra qualsiasi, e lì si provano tutti e nove i numeri dicendo quali sono già
  di altri; le altre due scorciatoie non possono essere modificatori + cifra (`NumberOf`). Impostazioni
  `QuickPaste` e `QuickKeys` (solo i modificatori). Si assegna e
  cambia da «Scorciatoia» nel menu del tasto destro (`ListNumbers`: «Nessuna» e i nove numeri, quello attuale
  spuntato e senza effetto, accanto agli altri la voce che li ha), e dal numero stesso sulla scheda, che un clic
  apre lo stesso elenco (`Part.Number`, il suggerimento dice i tasti, `NumberTip`). Si vede in un riquadro d'accento a destra della prima riga,
  prima della riga piccola e dei pulsanti, sempre, anche senza il puntatore (`CardParts.Number`), e nel menu del tray accanto al testo salvato
  (`ShortcutKeyDisplayString`, che `MenuLook` scrive in grigio). Un numero è di una voce sola: `Clips.Number` lo
  toglie prima a chi lo aveva, e lo schema lo vuole `UNIQUE`. **Un numero non salva una voce copiata** (deciso con
  l'utente): resta finché la voce non scade, e se ne va con lei. `Numbered` lo chiede al database, perché una
  voce copiata con un numero può stare oltre le `MaxShown` elencate. Salvare una voce copiata, o rinominare un
  testo, tiene il numero: è la stessa riga. Non si esporta.
- «Pulisci ora…» nelle opzioni (voluto dall'utente): elimina subito ciò che è stato copiato prima degli ultimi
  N giorni scritti nella casella accanto (`Spared`, non è un'impostazione: riparte da 0, cioè tutto), dopo una
  conferma. `Clips.Clean` e la scadenza passano dallo stesso `Drop`; vanno anche le voci in alto, mai i salvati.
  Casella e pulsante stanno sulla stessa scheda (`PlaceNumber` con il pulsante in fondo, fatto da `Act`).
- Anche le immagini copiate entrano nel database, compresse in PNG, con una miniatura; il loro `Text` è la
  descrizione che si vede, e la ricerca le trova per misura. Se negli appunti c'è anche testo vince il testo.
  In memoria resta solo la miniatura: il PNG si legge quando serve (`Entry.LoadPng` → `PngSource` → `Clips.Png`).
  Le miniature dei copiati elencati restano decodificate da una ricarica all'altra (`Clips.previews`, per id: senza,
  ogni copia ridecodificava fino a 200 PNG e lasciava le bitmap vecchie alla memoria nativa); `Insert` toglie l'id
  della riga nuova, perché SQLite può riusare quello dell'ultima cancellata.
  La stessa copia annunciata più volte da Windows è la stessa riga (impronta del PNG).
  Le immagini copiate hanno un tetto (voluto dall'utente il 17/10/2026, «non più di 200 MB»): `Clips.PictureSpace`,
  PNG e miniature contati con `length()` senza leggerli (`PictureBytes`). Oltre il tetto `TrimPictures` toglie le
  più vecchie, anche con freccia o numero, dopo ogni immagine copiata e a ogni `Prune`; le salvate non contano.
  Nessuna domanda né avviso: finché un'altra immagine grande come la più grande non ci starebbe (`PicturesFull`),
  sotto le schede compare una barra rossa (`banner`, una `InfoBar` di tipo errore come quelle della riga di
  stato, messa a posto da `FitStatus`, accesa da `ShowPictureRoom` a ogni `Filter`).
  Nell'elenco sono alte come le altre schede, con la miniatura (`Entry.Preview`) tagliata a riempire il suo riquadro. **Si salvano** come i testi (dopo la 4.0.0):
  `EditForm` mostra l'immagine al posto del contenuto e chiede solo nome e cartella; un'immagine salvata mostra
  **sempre** la miniatura, con accanto il nome se c'è (decisione dell'utente: per le immagini il nome non nasconde
  niente, `PictureHintEmpty`/`PictureHintNamed`), e l'anteprima compare passandoci sopra. Non si esportano.
- «Copia il testo» (voluto dall'utente il 18/10/2026), nel menu del tasto destro di un'immagine, copiata o salvata
  (`CopyText`): il testo che il riconoscimento di Windows ci legge (`Ocr.Read`, una riga per riga) va negli appunti
  **come una copia dell'utente** (niente `ownCopy`), così entra nell'elenco; quello di un'immagine salvata porta il
  formato riservato (`NewData`, lo stesso di `Use`). Si legge in un thread a parte (una schermata intera, 120 ms) e
  il risultato torna con `BeginInvoke`; intanto la riga di stato lo dice. Nessun testo, o nessuna lingua che
  Windows sappia leggere, è un avviso. Scelte provate (prototipo del 18/10/2026, 240 immagini di testo dello
  schermo, ClearType, 8–12 pt): la lingua è quella del programma se Windows ha il suo riconoscimento (quello
  inglese legge «Città» male), altrimenti quelle del profilo; l'immagine **non si ingrandisce** (nitida non
  migliora, sfumata non si legge più niente) ma si mette su un margine bianco di 400 px (`Ocr.Margin`: letture
  esatte da 86 a 144 su 240), e una trasparente su bianco. Il testo sotto i 10 pt esce comunque con qualche
  errore («second fine here»): è il motore di Windows, e la guida lo dice. La ricerca nel testo delle immagini
  non c'è (vorrebbe leggerle tutte alla copia).
- App di provenienza (voluta dall'utente il 18/10/2026: «serve per ogni voce copiata»): nella riga piccola sotto la scheda di ogni
  voce copiata, icona e nome del programma in cui è stata copiata (`DrawEntry`); nella barra laterale, sotto
  «Provenienza», un posto per programma (`Clips.Sources`, con quante voci; prima quello della copia più recente, voluto dall'utente il 19/10/2026, prima erano per nome). `Texts.Remember`
  chiede a `Origin.Find` l'eseguibile del proprietario degli appunti (`GetClipboardOwner`; senza, o se è il servizio degli appunti di Windows in `svchost.exe`, `IsHost`; le voci che le versioni fino alla 4.3.2 gli avevano attribuito lo dimenticano a ogni `Prune`, `ForgetHosts`, la finestra in primo
  piano), con `QueryFullProcessImageName` (funziona anche con i programmi da amministratore). Il nome è la descrizione
  del file («Microsoft Edge»), o il prodotto, o il nome del file (`Origin.NameOf`); l'icona quella di
  `ExtractAssociatedIcon` (`IconOf`). Ogni programma sta una volta sola nella tabella `apps` (`Clips.App`, descritto
  di nuovo alla prima copia di ogni sessione, così un aggiornamento cambia nome e icona); quelli da cui non viene più
  nessun copiato se ne vanno con la scadenza (`DropApps`). Copiare di nuovo la stessa cosa altrove sposta la voce al
  programma nuovo; un programma sconosciuto lascia quello di prima. Salvare una voce la toglie (regola dello schema:
  un salvato non dice niente di sé). I programmi di un server Citrix (voluto dall'utente il 19/10/2026: al lavoro
  tutto risultava «Citrix HDX Engine»): per Windows ciò che vi si copia è di `wfica32.exe` (o `wfica.exe`,
  `CDViewer.exe`, `Origin.Remote`), ma ogni sua finestra ha l'icona del programma remoto. Se il programma trovato è uno
  di quelli ed è anche quello della finestra in primo piano, `Origin.Within` lo chiama eseguibile + `*` + impronta
  dell'icona della finestra (`WM_GETICON`, con `SendMessageTimeout`; mai l'icona della classe), e tiene l'icona per la
  sessione (`windowIcons`, che `IconOf` legge): così nella barra ci sono tante «Citrix HDX Engine» quante icone, il nome
  è sempre quello di Citrix (scelta dell'utente: «so che viene da lì, ma capisco da quale applicazione»), e lo schema
  non cambia. Ricavare il nome vero dal titolo della finestra è stato
  scartato (Esplora file e gli emulatori di terminale danno il nome della cartella o «[62 x 160]»). Non si esporta, e la ricerca non trova le voci per programma (si potrebbe: ma
  «code» troverebbe tutto ciò che è copiato in VS Code). Le voci finte delle immagini (`FillSample`) vengono da Edge,
  PowerShell e Blocco note (`Clips.Attribute`).
- I testi salvati (`Entry.Saved`) stanno nello stesso database, contenuto e miniatura cifrati con DPAPI per
  l'utente (`ProtectedData`, in `System.Security.dll` che `csc` riferisce già da solo); non hanno testo per la
  ricerca né impronta, che li tradirebbero. Non scadono mai. L'elenco li tiene in memoria decifrati (`entries`,
  `LoadSaved`, rifatto da `Reload` solo quando cambiano) e la ricerca li scorre lì (`Texts.Matches`). La scheda
  ne mostra in cima la cartella (o «Salvati») e il nome, in basso il giorno in cui sono stati salvati (sotto il nome
  di un giorno solo l'ora; quelli salvati prima che si tenesse il giorno restano senza).
  Una voce si riconosce dal suo `Id`, non più dal nome.
- Cartelle dei testi salvati (volute dall'utente), anche una dentro l'altra (sottocartelle volute dopo, dalla
  3.8). Nel codice si chiamano «group». Nel database sono un albero (`folders`, `parent_id`, `Folder`), ma la
  finestra continua a conoscerle per **percorso**, i nomi uniti da `Texts.Separator` (`\`, vietato nei nomi,
  `Refused`): `Lavoro\Server`. `LoadSaved` costruisce i percorsi e `FolderAt` (`Clips.FindFolder`) fa il
  contrario; una riga di cartella ha in `FolderId` l'id della cartella. Gli aiuti sui percorsi stanno in `Texts`
  (`Parent`, `Leaf`, `Join`, `Within`, `Depth`, `Display` con « › », `CleanPath` per i file esportati). In `entries`
  una cartella è una `Entry` con `IsGroup` (e `Saved`) e `Name` uguale al percorso (`Label` ne mostra l'ultimo
  nome), messa subito prima dei suoi testi e poi delle sue sottocartelle, quelle affiancate
  per nome: per questo dove si scorre `entries` cercando i testi va escluso `IsGroup`. Le cartelle stanno solo
  nella barra laterale (dal 18/10/2026, scelta dell'utente), sotto «Salvati», rientrate per livello: un clic mostra i
  testi della cartella e delle sue sottocartelle; la freccia a destra apre e chiude le sottocartelle (una chiusa
  nasconde anche quelle dentro, `InView`). Il menu del tasto destro di una cartella (`navMenu`) ha «Nuova cartella…»,
  «Rinomina…», «Sposta in» ed «Elimina»; quello di «Salvati» solo la prima. La cartella mostrata segue i propri
  spostamenti e cambi di nome (`Follow`); eliminata, si torna a «Salvati». Rinominare e spostare insieme è `PlaceFolder` (un
  solo aggiornamento, così un nome uguale solo a metà strada non conta); `DeleteFolder` elimina la cartella e il
  contenuto sale di un livello (scelta dell'utente: i testi potrebbero essere password), riempiendo una cartella
  con lo stesso nome se lassù c'è già. «Nuova cartella…»
  (Ctrl+Maiusc+N, in fondo alla barra, nasce dentro la cartella mostrata: `FolderHere`) e «Sposta in» (anche per le
  cartelle, mai dentro se stesse: `CanGo`, `ListPlaces`) «Sposta in» sta anche nel menu delle voci, che si apre anche senza selezione (voluto dall'utente: «Nuova cartella…» e la pausa no, stanno in fondo alla barra). In
  `EditForm` la cartella si sceglie da un pulsante che apre un menu (un `ComboBox` resterebbe chiaro), con il
  percorso accorciato da sinistra se non ci sta; la stessa finestra, senza contenuto, chiede nome e posto di
  una cartella. In una ricerca le cartelle compaiono solo se contengono risultati, anche da chiuse, insieme a
  quelle che le contengono.
- Testi salvati formattati (voluti dall'utente il 17/10/2026, per le firme di posta: grassetto, corsivo, caratteri e
  dimensioni diversi; scelte sue: **solo i salvati**, modifica semplificata, HTML nell'esportazione). Nel database
  `html` (sempre, se formattato) e `rtf` (solo se è arrivato con uno), cifrati; `Entry.Text` resta il testo semplice,
  per la ricerca, l'elenco e la digitazione. I copiati restano testo semplice: la formattazione delle ultime
  `Rich.KeptCopies` copie della sessione sta **solo in memoria** (`Rich.Remember` da `Texts.Remember`, `Rich.Recent`),
  così salvare una copia recente la salva formattata. `Use` mette negli appunti testo, «HTML Format» (con
  l'intestazione in byte, `Rich.Document`) e RTF. In `EditForm` il contenuto è una `RichBox`: si mostra dai `Run`
  (`Rich.ToRtf`, nei colori del tema: il nero o quasi si legge come predefinito, `DarkLimit`), i pulsanti sopra (B, I,
  U, «Carattere» e «Dimensione» scritti per nome, ognuno con un menu con «Predefinito», la gomma; come due icone si
  somigliavano troppo) usano `EM_SETCHARFORMAT` sulla selezione, Ctrl+B/I/U
  pure, l'incolla tiene la formattazione (RTF prima, poi HTML) e i tasti di RichEdit per i paragrafi non fanno
  niente. La formattazione vale solo se dice lo stesso testo del testo semplice (`Rich.Matches`, solo lettere e
  cifre): copiando dalla barra degli indirizzi, Chrome ed Edge danno l'indirizzo come testo e come HTML un link con
  il titolo della pagina, che salvando prendeva il posto dell'indirizzo (trovato dall'utente il 17/10/2026). Al salvataggio (`Conclude`): senza formattazione è testo semplice; **se l'RTF della casella è quello di
  quando si è aperta** si tengono HTML e RTF arrivati (link, tabelle, immagini compresi); altrimenti l'HTML si riscrive
  dai `Run` e l'RTF si lascia (scriverlo dai `Run` vorrebbe un carattere predefinito che non c'è). Ciò che non ha
  carattere, dimensione o colore propri non ne ha neanche nell'HTML: prende quelli di dove si incolla. La dimensione
  predefinita della casella si misura passando dal suo RTF (`SizeIn`: la casella arrotonda ai pixel, 11 pt diventava
  11,5) e la gomma copia il formato predefinito della casella (`SCF_DEFAULT`). La casella si legge a pezzi (`Walk`,
  `Stretch`): di una selezione `EM_GETCHARFORMAT` dice ciò che è uguale in tutta, quindi ogni pezzo si allunga
  raddoppiando e poi dimezzando (letto un carattere alla volta, un testo di 32.000 caratteri prendeva mezzo
  secondo, ora 25 ms: il controllo è in `RichTest`). Esportati, l'HTML sta prima del testo
  (`HTML:`), così le versioni vecchie lo saltano; l'importazione lo riprende, senza RTF.
- I testi salvati stanno anche nel menu dell'icona del tray (voluto dall'utente: «prenderli senza aprire
  l'app»), dopo «Apri», ogni cartella un sottomenu, aperta o chiusa che sia: `ShowSaved`, rifatto a ogni
  apertura del menu da `entries`. La scelta passa da `Use` come nell'elenco: solo appunti, niente digitazione.
  Si vedono solo i nomi.
- «Esporta…» e «Importa…» nelle opzioni, sotto «Programma» (voluti dall'utente, con la scelta «solo i testi
  salvati» o «tutto» in un menu sotto ciascun pulsante, `Choice`). Il file è testo UTF-8 con BOM, leggibile dal
  Blocco note e **in chiaro** (prima di esportare si chiede conferma): un segno per voce (`[Folder]`,
  `[Saved]`, `[Copied]` in inglese, `[Cartella]`… in italiano: il file si scrive nella lingua in uso e si legge
  in tutte, `BackupWords` in `Backup.cs`; quelli scritti prima delle lingue sono in italiano), pochi campi, poi il testo, una riga per riga dopo una tabulazione: è la
  tabulazione a distinguerlo dal resto, quindi un testo può contenere qualsiasi cosa. Le immagini non si
  esportano. L'importazione aggiunge: per i salvati con un nome già preso chiede una volta sola se sostituire;
  i copiati tornano con la loro ora e la freccia (`Clips.Import`), tranne quelli che l'elenco ha già, e quelli
  scaduti se ne vanno subito; con la cronologia spenta non si importano. Tutta l'importazione è una transazione
  (`Backup.Import`), e le cartelle nominate dal file si creano (`MakeFolder`).
- Ogni voce è una scheda (dal 18/10/2026; scelta dell'utente: con i pulsanti, anche se se ne vedono
  meno): disegnata a mano in una riga di `cardList` (`OwnerDrawVariable`, `DrawEntry`), un po' più chiara del
  pannello (`Look.Raised`, nuovo colore delle due tavolozze) e con il bordo d'accento se selezionata (`RowList.Marked`
  falso: niente tacca). **Tutte le schede sono alte uguali**, qualunque cosa contengano (voluto dall'utente il
  20/10/2026: prima un'immagine era alta quasi il doppio di un testo e un testo di due righe di più): alte
  `cardLines` righe di testo, l'opzione «Altezza delle schede» in «Aspetto» (`linesButton`, `UseCardLines`,
  `CardLineChoices` 1 o 2, `DefaultCardLines` 1 scelto dall'utente per vedere più voci; impostazione `CardLines`).
  `CardHeight` non misura più niente. Con una riga le righe di un testo si seguono, quelle vuote saltate, con in mezzo
  un `Joint` (« ↵ ») scritto tenue (`Plain`, `Write`; il testo così unito passa da `LayMarked`/`DrawMarked` anche
  senza ricerca); con due restano com'erano. L'immagine riempie un riquadro alto quanto le righe e largo
  `PictureShape` volte tanto, tagliata al centro, con gli angoli arrotondati (`DrawPicture`). **Prima la voce**,
  grande (il testo, `CardText`; il nome di un salvato con nome; la miniatura con la misura); all'estremità destra
  della prima riga, piccola e tenue, sempre lì (`Lay`; schede compatte volute dall'utente il 19/10/2026, «molti
  spazi interni nelle celle»: margini `CardPad`; prima una scheda di testo era alta 73 px, ora 36 a una riga) (`MetaShare`, anche l'icona semitrasparente, `Look.DrawFaint`; voluto dall'utente il 19/10/2026, «per rendere ancora più chiaro cosa è copiato»), una riga sola: icona e nome del programma
  (o stella e cartella di un salvato) · ora (`When`) · tipo (`BadgeOf`; niente per un testo semplice, che detto su
  quasi ogni scheda non diceva niente). **Sotto il puntatore** i piccoli segni senza testo prendono il suo posto,
  l'una che sfuma mentre gli altri compaiono con la luce della scheda (`Glow`): la freccia (solo dei copiati, colorata se
  la tiene in alto), anteprima (l'occhio), copia, salva/modifica, elimina, «⋯» (`CardButtons`, `CopiedButtons`). Voluto
  dall'utente il 19/10/2026: sempre lì su ogni scheda, l'elenco sembrava una barra degli strumenti; non sulla scheda
  selezionata, che così dice programma e ora (Ctrl+D seleziona proprio l'ultima copia). Lo spazio è largo quanto il
  più largo dei due (la riga si taglia oltre `MetaRoom` della scheda), così la scheda non cambia altezza sotto il
  puntatore e tutte hanno la riga sulla prima riga: prima andava accanto ai pulsanti o sotto, secondo lo spazio, e le
  schede dello stesso tipo avevano altezze diverse (il controllo è in `shot`). Prima della riga, sempre, il numero.
  Ogni segno ha il nome in un suggerimento sotto di sé (`PartText`, mostrato a
  mano da `HoverTip` come i nomi della barra ridotta: cambiare il testo dell'unico suggerimento della lista non lo
  faceva ricomparire, trovato dall'utente il 19/10/2026). Voluto
  dall'utente il 18/10/2026: con il programma in grassetto in cima e una fila di pulsanti in fondo, «il
  vero contenuto si perde» (scelte proposte: icone a destra). Le parti si misurano in un posto solo (`Lay`, `CardParts`) che serve a disegno e clic (`PartAt`);
  quella sotto il puntatore si illumina (`hoverRow`, `hoverPart`). Un clic altrove sulla scheda la seleziona soltanto; il doppio clic fa come Invio (`Accept`).
  **Una riga di `ListBox` non supera 255 pixel** (`MaxRow`): `CardHeight` vi taglia la scheda. Sopra i giorni un `Heading`, che una selezione salta (`lastCard`, `Step`).
  Le righe alternate e le colonne di prima non ci sono più.
- Anteprima: l'occhio a destra di ogni scheda, testi e immagini (`Part.Show`, voluto dall'utente il 19/10/2026), e
  «Anteprima» nel menu (`ShowFull`; di un salvato con nome «Mostra il contenuto», `ShowText`) mostrano la voce per
  intero in `Peek`, sotto la scheda (`ShowPeek` con `asked`), e la lasciano in vista `AskedMs` ovunque vada il
  puntatore (`peekAsked`). Un altro clic sull'occhio della stessa scheda la toglie soltanto (voluto dall'utente il
  20/10/2026: la pressione la toglieva e il clic la rimostrava subito; `peekRow`, `eyeCloses` decisi alla pressione). Il puntatore fermo `RestMs` su una voce che la scheda taglia (`Cut`; timer `rest` avviato
  da `Hover`), voluto all'inizio dall'utente «con il mouse che resta sopra un po'», dal 19/10/2026 è un'opzione
  spenta di default (`restBox`, «Anteprima con il puntatore fermo» in «Aspetto», impostazione `PeekOnRest`): l'utente
  la voleva togliere o rendere facoltativa. `Peek` non prende il primo piano e lascia passare il puntatore
  (`HTTRANSPARENT`), così l'elenco continua a seguire il mouse; sparisce a ogni tasto, clic, rotella, ricarica
  dell'elenco. Dei testi salvati con nome **non** compare mai passandoci sopra (potrebbero essere password).
- Pausa della cattura (voluta dall'utente): «Sospendi/Riprendi la cattura» nel menu dell'icona e in fondo
  alla barra laterale (`Pause`, `paused`, `pauseItems` e `PauseRow` di `footList`). Non è un'impostazione: a ogni avvio si riparte in ascolto.
  Lo dicono il titolo (dove ci sono copiati), l'elenco vuoto e il suggerimento dell'icona.
- La scheda di un testo salvato, o una cartella della barra, si trascina su una cartella della barra (voluto
  dall'utente; su «Salvati» esce dalle cartelle): `DoDragDrop` parte da `MouseMove` solo oltre `DragSize`, così un
  clic resta un clic (`Draggable`); `DropAt` dice la riga sotto il puntatore, `Aim` la illumina, il rilascio passa
  da `MoveTo` come «Sposta in».
- I testi salvati si scrivono in `EditForm` (nome e contenuto), aperta da `Edit`: testo nuovo, voce copiata
  da tenere, voce salvata da cambiare. **Il nome è facoltativo** (deciso con l'utente il 17/10/2026): con il
  nome l'elenco mostra il nome e nasconde il contenuto (adatto alle password), senza mostra il contenuto (la mail
  secondaria). Sotto la casella una riga grigia lo dice mentre si scrive (`hint`, `NameHintEmpty`,
  `NameHintNamed`); «Salva» vuole solo il contenuto. L'anteprima al passaggio del mouse compare anche sui salvati
  senza nome (`Hover`, `resting`), «Mostra il contenuto» solo su quelli con il nome. Nel file esportato un salvato
  senza nome non ha la riga `Name:`; reimportato non si ripete (si riconosce dal contenuto). Si salva solo dalla
  finestra: un «Salva subito» senza finestra è stato proposto e scartato dall'utente. Le stesse tre azioni stanno in
  «Nuovo…» accanto alla ricerca e nei pulsanti delle schede, nel menu del tasto destro e in Ctrl+N/S/E. Il nome scritto nella casella di ricerca è stato provato e scartato dall'utente
  (poco chiaro). Il clic, sinistro o destro, seleziona soltanto; il destro apre anche il menu.

- Tema (voluto dall'utente, a cui lo scuro con accenti pastello di prima non piaceva più, ma nemmeno il
  bianco): grigio caldo neutro con tocchi di colore nelle icone. I colori stanno tutti in cima a `Look`; i
  segni (`Sign`, le icone di Windows dal carattere Segoe Fluent Icons, o Segoe MDL2 Assets su Windows 10, in
  `Look.Glyph`; senza nessuno dei due disegnati a mano su una griglia di 16) dicono cos'è una voce — testo,
  indirizzo, testo formattato — e stanno sui pulsanti (`RoundButton.Sign`). Barra del titolo e barre di scorrimento
  seguono da sole la luminosità dello sfondo (`Look.Dark`).
- Chiaro o scuro come Windows per le app (voluto dall'utente il 15/10/2026: «il più moderno possibile»
  restando in WinForms, rinunciando a WinUI per non perdere la compilazione senza installare nulla), dalla
  chiave `Themes\Personalize`, `AppsUseLightTheme`. Lo scuro è un grigio neutro alla Windows 11 (aree un
  tono più chiare della finestra), con i segni un po' più accesi; l'accento resta lo stesso blu fisso (scelta
  dell'utente, non quello di Windows). Le due tavolozze stanno in `Look.Use`. Il cambio si segue a programma
  aperto: `WM_SETTINGCHANGE` con «ImmersiveColorSet» → `Look.Follow` → `Switch`, che ricolora ogni finestra
  aperta (`Recolor`): un colore assegnato a un controllo è sempre uno della tavolozza, e quello vecchio dice per
  posizione (`Palette`) quale diventa. **Un colore nuovo va messo in tutte e due le tavolozze e in `Palette`**,
  e le due non devono avere colori in comune, altrimenti `Recolor` sbaglia. Ciò che è disegnato a mano legge la
  tavolozza mentre dipinge; chi ha bisogni suoi ascolta `Look.Changed` (la guida, il cui testo ha il colore
  con cui è stato scritto); `Peek` prende i colori a ogni `Reveal`. I menu che disegna Windows (quello delle
  caselle di testo) si scuriscono con `SetPreferredAppMode` di uxtheme, non documentata e chiamata per numero,
  solo da Windows 10 1903. I test fotografano l'uno o l'altro con `dark`/`light` in fondo agli argomenti, e
  `shot` prova il cambio a finestra aperta (`Look.Switch`).
- Opzione «Tema» in «Aspetto», sotto la lingua (voluta dall'utente il 16/10/2026, dopo aver cercato invano
  come cambiarlo): «Come Windows» (default), «Chiaro», «Scuro», con un pulsante e un menu come la lingua
  (`ChangeTheme`, `Look.Choose`, `Look.Chosen`, enum `Theme`). A differenza della lingua cambia subito, senza
  riavvio. Impostazione `Theme` (numero), letta da `Main` con `SavedTheme` prima di creare la finestra, che
  nasce già nei suoi colori; `TransferSettings` la scrive soltanto. Con un tema scelto, `Follow` ignora i
  cambi di Windows.
- Modernità alla Windows 11 (stessa richiesta): menu arrotondati da Windows (`DWMWCP_ROUNDSMALL`), con righe
  alte, testo spostato a destra e una forma arrotondata sotto il puntatore (`MenuLook`, `Look.Style` del
  menu); suggerimenti disegnati nei colori della finestra (`Look.Style` del `ToolTip`); le domande in una
  finestra del programma (`AskForm`) invece di `MessageBox`, che resterebbe bianca nello scuro: testo su
  un'area più chiara e pulsanti sotto, con quello colorato su «No», che è anche quello di Invio ed Esc;
  interruttori con la manopola che scorre e cresce sotto il puntatore; pulsanti con il bordo più scuro in
  basso; anello del focus solo usando la tastiera (`ShowFocusCues`). Mica non si usa: con i controlli GDI lo
  sfondo trasparente non regge (un finto Mica dallo sfondo del desktop è stato proposto e non scelto).
- Ancora più Windows 11 (voluto dall'utente il 16/10/2026, «restando fedeli al progetto light»): le icone di
  Windows al posto dei segni disegnati; caratteri seminegretti di Segoe UI Variable (`StrongFont` per i
  giorni nell'elenco e i tasti della guida, `TitleFont`, più grande, per il nome di una pagina delle opzioni e i titoli della guida; GDI
  vede il Display Semibold con il nome tagliato a 31 lettere, «Segoe UI Variable Display Semib»; su Windows 10
  Segoe UI Semibold); righe e pulsanti che si illuminano e si spengono a piccoli passi (`Glow` nelle righe, `Fade`
  in `RoundButton` e `Toggle`); l'anteprima che compare in dissolvenza (`Peek.appear`, `Opacity`); le pagine delle
  opzioni che salgono al loro posto (`Entrance`); barre di scorrimento sottili (`Rail`): una linea che si allarga
  sotto il puntatore, trascinabile, sopra la barra di Windows, che resta sotto di lei. Per questo gli elenchi e il
  testo della guida tengono la barra sempre (`ScrollAlwaysVisible`, `ForcedVertical`): le righe non cambiano mai
  larghezza e la rotaia ha sempre dove stare; la linea compare solo se c'è da scorrere. La tacca della riga
  selezionata è corta e arrotondata, come nella colonna di Impostazioni (`MarkShare`).
- Più moderno ancora (voluto dall'utente il 18/10/2026, «restando nella filosofia della leggerezza»; scelto dal mio
  piano: icone nei menu, opzioni come Impostazioni, riga di stato come InfoBar, tacca che scivola, rifiniture):
  - Menu: un segno prima di ogni voce, come i menu di Windows 11 (`Look.Mark`, tenuti in una
    `ConditionalWeakTable`; nel margine delle immagini del menu, acceso solo dove qualche voce ha un segno, perché
    lì WinForms ne conta la larghezza). Nei menu di scelta (lingua, tema, caratteri, numero di una voce) quella in
    uso ha la spunta (`Checked`, disegnata come segno `Check`) invece di essere disattivata, e sceglierla non fa niente.
  - Opzioni come le Impostazioni di Windows 11: ogni opzione è una scheda (`Tile`, messa da `PlaceTile`) con il nome,
    sotto in grigio e più piccola che cosa fa in poche parole (`…Note` in `Strings`; la spiegazione completa resta
    nel suggerimento, su scheda e controllo: `TipOption`) e i controlli all'estremità destra; gli interruttori
    dicono «Attivato/Disattivato» prima di sé (`Toggle.SaysState`, `Switch`). Carattere e dimensione stanno su una
    scheda sola. Cinque pagine: è nata «Aspetto» (lingua, tema, caratteri), perché «Programma» non ci stava più.
  - Riga di stato come l'InfoBar di Windows 11 (`InfoBar`): una barra arrotondata tinta del colore del tipo, con il
    suo segno (`Notice`: informazione, fatto, avviso, errore). `status.Tell(testo, tipo)`; `Say` è «fatto», `Warn`
    un avviso, `ShowError` un errore, un testo assegnato da solo è informazione (conto alla rovescia, digitazione in
    corso); l'esito di una digitazione ha il tipo di `KindOf`. La finestra delle opzioni lo ripete con lo stesso tipo.
    Dopo una tabulazione (`InfoBar.Apart`, `\t` nel testo) viene ciò che fare dopo, scritto all'estremità destra
    (voluto dall'utente il 18/10/2026, per staccare «Negli appunti: …» da «Incollalo con Ctrl+V…»): sulla stessa riga
    se tutti e due ci stanno con almeno `HintGap` in mezzo, altrimenti sulle righe sotto, sempre a destra (`Place`).
  - La tacca della riga selezionata scivola alla nuova (`RowList`, in tutti gli elenchi: barra laterale, pagine delle
    opzioni, argomenti della guida): prima il capo davanti, poi l'altro, così si allunga e si raccoglie, come in
    NavigationView. La forma della riga selezionata cambia subito.
  - Rifiniture: i pulsanti della fila in basso, tranne «Digita gli appunti», sono sottili (`RoundButton.Subtle`: solo
    segno e testo finché il puntatore non ne fa comparire la forma); Ctrl+F porta il cursore nella ricerca, che lo
    mostra in un riquadrino a destra (`SearchKeys`, tra i `ListKeys`); un elenco vuoto ha sopra le parole un
    segno grande e tenue del perché (`EmptyNote`); la versione è passata dal titolo alle opzioni.
- Aspetto (`Look`): barra del titolo nel colore della finestra
  (`DwmSetWindowAttribute`, solo Windows 11: altrove fallisce senza danni), pulsanti piatti con un solo
  pulsante in colore d'accento, schede delle voci disegnate a mano (`DrawEntry`, vedi sopra); il segno di una voce dice che cos'è: testo, indirizzo, codice, immagine, testo formattato (`SignOf`, nel menu del tray e sulle schede senza programma).
  I controlli che Windows non lascia scurire sono stati sostituiti: le caselle di testo stanno senza bordo
  dentro un pannello (`Look.Frame`), le spunte sono interruttori disegnati (`Toggle`, restano `CheckBox`; voluti
  dall'utente al posto dei quadratini), l'attesa è una casella di testo
  numerica e la tastiera del server un interruttore (prima `NumericUpDown` e `ComboBox`, rimasti chiari).
  Il carattere è `Look.TextFont`, **uguale in tutte le finestre** (voluto dall'utente: pulsanti, opzioni, guida,
  menu, domande; mai un carattere diverso per un singolo controllo, tranne le voci): vedi «Carattere
  dell'interfaccia» sotto.
- Carattere e dimensione dell'interfaccia (voluti dall'utente il 16/10/2026: la guida gli sembrava piccola accanto
  alle voci, e voleva un carattere solo per tutto il programma, che «si distingua bene» da quello delle voci): in
  «Aspetto», sotto il tema, sulla stessa scheda `faceButton` e `faceSizeButton` con menu come la lingua.
  Caratteri: gli stessi delle voci (`FontNames`); dimensioni `Look.InterfaceSizes` in pixel a 96 dpi (12–16),
  default 13 (`DefaultInterfaceSize`, scelto dall'utente; prima era 12, la dimensione dei messaggi di Windows).
  Le finestre sono disegnate per 12 px (`DesignedSize`) e crescono con il testo: `Look.Zoom` è la scala dello
  schermo per `InterfaceSize / DesignedSize`. Per questo si applicano **al riavvio**, come la lingua
  (`ChangeInterface` → `Restart`): `Main` le legge con `UseSavedInterface` prima di creare la finestra
  (`Look.UseInterfaceFont`), `TransferSettings` le scrive (`face`, `faceSize`). Impostazioni `InterfaceFont` e
  `InterfaceSize` (i vecchi `Font` e `TextSize` del primo tentativo della 3.9.3 non si leggono più).
- Carattere e dimensione delle voci (voluti dall'utente il 16/10/2026: il testo di ciò che si copia più grande,
  e «il classico font select con 11px, 12px…»; prima ho fatto ingrandire tutta l'interfaccia, ma l'utente la
  voleva com'era): in «Aspetto», in fondo, due pulsanti sulla stessa scheda con menu come la lingua (`fontButton`,
  `sizeButton`; nel menu ogni carattere è scritto in se stesso, `Look.FontLabel` toglie «Text» a Segoe UI
  Variable). Valgono per le righe dei due elenchi, l'anteprima (`Peek`) e il contenuto in `EditForm`:
  `Look.EntryFont` e `StrongEntryFont` (per le cartelle). Caratteri: quelli di `Look.Families` che Windows ha
  (`FontNames`), monospaziati compresi. Dimensioni: `Look.EntrySizes`, in pixel a 96 dpi (11–20), default 14
  (`DefaultEntrySize`, il testo di Windows 11, prima era 12). Cambiano subito (`UseEntryFont` di `MainForm`, che
  rimisura le righe riempiendo di nuovo gli elenchi). Impostazioni `EntryFont` (nome) e `EntrySize` (pixel);
  valori sconosciuti tornano ai default. Tutto ciò che sta in una riga si misura da `Look.EntryFont`, non dal
  carattere della finestra.
- Le finestre si costruiscono come disegnate e alla fine del costruttore `Finish` (`Scale(Look.Zoom)`), con
  `AutoScaleMode.None` (vedi le trappole); `Look.Zoom` è la scala dello schermo per quella del testo
  dell'interfaccia. `Restart` (riavvio con `/restart`) serve a `ChangeLanguage` e `ChangeInterface`.
  Barra del titolo e barre di scorrimento scure non compaiono nelle immagini di `tests.exe shot`: per
  vederle c'è `tests.exe real <prefisso>`, che mostra le finestre un attimo e fotografa lo schermo.
  Forme arrotondate (l'utente trovava la finestra piatta e asettica, ma i colori vanno bene): ricerca, pannello
  e caselle stanno in un `Card` (bordo sottile, d'accento quando il contenuto ha il focus; `Look.Wrap`; attorno
  a una casella di testo invece, come in Windows 11, una riga d'accento sul fondo, `Card.Underline`), i
  pulsanti sono `RoundButton`, la riga selezionata è una forma arrotondata con una tacca d'accento e quella
  sotto il puntatore si illumina (`Hover`, indice e luce in `RowList.Glow`; `Look.DrawRow`). Le voci copiate mostrano sotto, a destra, l'ora
  (o il giorno, se non è oggi); accanto al titolo il numero di voci; un elenco vuoto dice perché (`emptyNote`).
  La descrizione sotto il pulsante deve stare in due righe: è il motivo della larghezza (`ContentWidth`).

- Disposizione (dal 18/10/2026, vedi sopra): la barra laterale a sinistra, con in fondo Nuova cartella,
  la pausa, Opzioni e Guida (`PlaceSidebar` la mette a posto quando la finestra ha la sua scala); a destra il pannello con
  titolo, ricerca, «Nuovo…» sottile e «Digita gli appunti» d'accento, schede dei tipi, voci e riga di stato in fondo.
  Prima la ricerca stava in cima e sotto le colonne c'era una fila di pulsanti piccoli.
  La riga di stato (una `InfoBar`) occupa solo lo spazio di ciò che dice (`FitStatus` con `InfoBar.Fit`: niente,
  o le righe del testo nella loro barra; il resto va alle voci) e a riposo tace (`ShowIdle`): come si digita
  una password sta nel suggerimento del pulsante. Le conferme si scrivono con `Say` e spariscono dopo `FadeMs`;
  avvisi (`Warn`), errori (`ShowError`) e ciò che riguarda la digitazione restano fino al messaggio dopo (l'esito
  di una digitazione fatta a finestra nascosta deve potersi leggere riaprendola).

- La finestra è ridimensionabile (voluto dall'utente), mai meno alta di come nasce; larga almeno quanto la barra laterale
  più il pannello più stretto (vedi sotto: dal 19/10/2026, voluto dall'utente, anche meno di come nasce) (`MinimumSize` in
  `OnLoad`). Dimensione e posizione si ricordano (la dimensione dal 16/10/2026, voluta dall'utente: prima tornava
  ogni volta quella di default): impostazioni `Width` e `Height`, in pixel a 96 dpi, applicate in
  `OnLoad` (`savedSize`), mai sotto la minima né oltre lo schermo, sul quale la finestra viene riportata. Le
  immagini dei test (`FillSample`) le ignorano: `shot` mostra la finestra come disegnata, `store` e `real` più
  larga di `Wider`. Allargandola crescono il pannello e le schede (`Anchor`); la barra laterale resta com'è.

- La barra laterale si allarga e si stringe trascinandone il bordo (lo spazio tra lei e il pannello, il puntatore
  diventa ↔; `OnSideEdge` e gli `OnMouse…` della finestra), da `MinSideWidth` a `MaxSideWidth`; e il pulsante ☰ in
  cima (`sideButton`, `Sign.Menu`) la riduce alle sole icone (`NarrowSideWidth`, `sideNarrow`, `NarrowSide`), come
  NavigationView di Windows 11 (voluti dall'utente il 19/10/2026; il pulsante era una sua idea, chiesto il mio
  parere). Trascinata sotto il minimo si riduce, tirata fuori riprende i nomi e la larghezza di prima (`sideBefore`).
  Ridotta ha **le stesse righe** (cartelle aperte e programmi compresi, senza rientri né frecce), così scelta dei
  posti, menu e trascinamento nelle cartelle restano quelli; il nome dell'icona su cui il puntatore si ferma compare a
  destra della riga (`RowTip`, con il `ToolTip` delle parti, `hoverTips`, mostrato a mano da `HoverTip` dopo `InitialDelay`; di una cartella il
  percorso intero). Non con il testo di un unico suggerimento della lista cambiato a ogni riga, come si fa sulle
  schede: Windows mostra il suggerimento di un controllo una volta sola finché il puntatore non ne esce, e passando
  da un'icona all'altra non ricompariva (trovato dall'utente il 19/10/2026). Il pannello cede alla barra al massimo `PaneSlack` della sua
  larghezza disegnata: oltre, per allargare la barra si allarga prima la finestra, e la finestra non si stringe più
  di così (`MinimumSize` rifatta da `PlaceSidebar`; `leastSize`, la misura con cui nasce, conta solo per l'altezza).
  Così la finestra si stringe sotto la misura con cui nasce, e di più con la barra stretta o ridotta. Solo trascinando
  il bordo la barra aspetta una finestra più larga; ridata con i nomi a finestra stretta, tiene la sua larghezza e la
  finestra si allarga (prima la barra restava schiacciata per sempre). La `Rail` di un elenco ne prende l'ancoraggio
  (prima sempre a destra: quella della barra laterale seguiva il bordo destro della finestra, una striscia scura sul
  pannello, trovata dall'utente il 19/10/2026). Impostazioni
  `SideWidth` (pixel a 96 dpi) e `SideNarrow`. Lo spazio trascinabile tra le due colonne di prima (`SavedWidth`) non
  c'era più dal 18/10/2026.

## Regole di stile volute dall'utente

- Nessun valore duplicato: ogni default, nome o numero sta in un posto solo (costanti in cima a `MainForm`,
  costanti di Windows in `Native` con i nomi ufficiali, scorciatoia definita una volta sola).
- Aggiungere un'opzione: il campo in cima a `MainForm`, una riga `PlaceOption` (`PlaceNumber` se è un numero,
  `PlaceHotkey` se è una scorciatoia globale, `PlaceTile` con `Picker` se si sceglie da un menu) in `PlaceOptions`
  sotto la sua pagina (`Page`), una in `TransferSettings` e il suggerimento `TipOption`, tutto in
  `MainForm.Options.cs`. Sulla pagina entra da sola, su una scheda alta quanto serve al suo testo.
  Nome, descrizione breve (`…Note`) e suggerimento sono campi di `Strings` (inglese) tradotti nelle altre tre lingue.
- **Nessun testo visibile scritto nel codice**: ogni testo nuovo è un campo di `Strings.cs` in inglese e una riga
  in ciascuno dei tre file delle lingue, con gli stessi `{0}`. `tests.exe texts` controlla che nessuna traduzione
  chieda più valori dell'inglese (sarebbe un `FormatException` a programma avviato), che le guide abbiano gli
  stessi argomenti, ed elenca i testi rimasti uguali all'inglese. Gli spazi sono tarati sull'italiano, che è
  tra le lingue più lunghe: dopo aver cambiato etichette guardare `tests.exe shot <png> <lingua>` in tutte e
  quattro (francese e spagnolo sono i più a rischio).
  Le opzioni stanno in pagine, una per categoria (voluto dall'utente il 16/10/2026: «fatte come la guida, schede
  verticali, divise per argomento»; prima erano su due colonne, e prima ancora in un elenco unico, caotico): a
  sinistra la colonna delle pagine (`pageList`, disegnata come gli argomenti della guida con `Look.DrawTopic`, in un
  `Card` `Quiet` come quella della guida: con il bordo d'accento quando aveva il focus spiccava più della pagina; con
  l'icona di ciascuna), a destra la pagina scelta (`pagePane`, `ShowPage`), con il nome in grande in cima
  (`PageTop`) e le schede delle opzioni (`Tile`) larghe `PaneWidth`, una sotto l'altra. Ordine: Digitazione,
  Scorciatoie, Cronologia, Aspetto, Programma. Una pagina è un `Panel` con `Text` (il nome) e `Tag` (il segno), ed è
  lei l'elemento della colonna. Il colore d'accento resta a ciò che si clicca (il link alla cartella dei dati, sulla
  scheda della cartella, `PlaceFolder`). Le pagine sono alte `AreaHeight` (come l'elenco): in tutte e quattro le
  lingue ci stanno (la pagina più piena è Digitazione, sei schede).
  Stanno in una finestra loro (`OptionsWindow`, dal 17/10/2026, voluta dall'utente «come la guida»): non modale,
  sopra la finestra principale che ne è proprietaria, centrata su di lei a ogni apertura (`OpenOptions`), Chiudi
  ed Esc. Le opzioni restano campi di `MainForm` (le leggono `TransferSettings`, la digitazione, il tray): la
  finestra nasce una volta sola nel costruttore, si prende la colonna e le pagine fatte da `PlaceOptions`, e
  chiusa dall'utente si nasconde soltanto. Sotto le pagine ripete ciò che dice la riga di stato (`Note`), perché
  l'esito di un'opzione (esporta, sposta la cartella, avvio con Windows) non finisca sotto di lei; domande e
  scelte di file aperte da lì le appartengono (`Front`). Prende i tasti delle scorciatoie da cambiare e F1 come
  la principale (`ProcessCmdKey`). Ctrl+D la nasconde: starebbe sopra l'elenco.

## Lingua

- Scelta in «Appearance» con un pulsante che apre l'elenco delle lingue, ognuna con il proprio nome
  (`Picker`, `ChangeLanguage`); l'etichetta dice anche «Language» in ogni lingua, per chi ci arriva per
  sbaglio. Impostazione `Language` (stringa, `LanguageSetting`), letta da `Main` con `SavedLanguage` prima di
  creare la finestra; senza impostazione vale l'inglese (voluto dall'utente).
- Cambiare lingua **riavvia** il programma invece di riscrivere ogni testo al volo: `ChangeLanguage` lancia
  l'eseguibile con `/restart` (`Program.RestartArgument`) e si chiude salvando le impostazioni; il nuovo aspetta
  fino a dieci secondi che il vecchio liberi il mutex dell'istanza unica, poi si riapre sulle opzioni
  (`ShowOptions`), alla pagina «Aspetto» (`restartPage`), dove sono anche carattere e dimensione dell'interfaccia.
- Le domande non usano `MessageBox`, i cui pulsanti Sì/No seguirebbero la lingua di Windows: `AskForm` scrive
  `Strings.Yes`, `No`, `Ok`. Restano nella lingua di Windows le finestre di scelta dei file.

## Trappole già incontrate

- WinForms con `AutoScaleMode.Dpi` scala **due volte** i controlli che ereditano il carattere, se il carattere
  della finestra è stato impostato prima della scala (provato con una finestra minima: un'etichetta di 120
  diventava 213 invece di 160 al 133%; anche un carattere identico a quello di default basta). A 100% non si
  vedeva, perché il fattore era 1. Per questo le finestre (`Window`) usano `AutoScaleMode.None` e chiamano `Scale`
  da sole alla fine del costruttore (`Finish`), che scala tutto una volta sola. `Form.Scale` non sposta una finestra di primo livello.
- `Control.DrawToBitmap` (le immagini di `tests.exe shot`) disegna la barra di Windows degli elenchi sopra la
  `Rail`: le frecce che vi si vedono non ci sono a finestra vera (`real`).

- `NotifyIcon.Text` rifiuta più di 63 caratteri (eccezione): il suggerimento dell'icona si taglia
  (`TrayTextLimit`), perché in spagnolo e francese supera il limite.

- `VkKeyScan` per `.` propone il punto del tastierino numerico, che con Bloc Num spento è Canc: per questo la
  mappa dei tasti non lo usa.
- `GetKeyState(VK_CAPITAL)` è già aggiornato anche in secondo piano (verificato): non servono letture "a vuoto".
- Un `WM_CLOSE` esterno (`CloseMainWindow`) arriva come `TaskManagerClosing` e chiude davvero anche in modalità
  tray; la X vera è `WM_SYSCOMMAND` `SC_CLOSE` (0xF060) e nasconde. Dopo una chiusura annullata WinForms
  ricorda `UserClosing` anche per i `WM_CLOSE` successivi.
- In PowerShell 5.1 `reg import ... 2>&1` con `$ErrorActionPreference = 'Stop'` solleva un errore anche quando
  riesce.
- Ctrl+Alt equivale ad AltGr: una scorciatoia con una lettera che sulla tastiera italiana ha un simbolo con
  AltGr (E per €, è ò à +) scatterebbe scrivendo quel simbolo. V non ne ha.
- La scorciatoia dell'elenco nasce Ctrl+D, scelta dall'utente al posto di Ditto: da globale toglie Ctrl+D agli
  altri programmi, ed è scritto nel suggerimento. Per questo nell'elenco «elimina» è Ctrl+E.
- Le due scorciatoie globali (e i modificatori dei numeri, vedi sopra) si cambiano dalle opzioni (voluto dall'utente): il pulsante accanto
  all'interruttore mostra i tasti e ne aspetta di nuovi (`AwaitKeys`, `TakeKeys` da `ProcessCmdKey`). Durante
  l'attesa le due scorciatoie si tolgono, altrimenti Windows le prenderebbe prima della finestra. Valgono Ctrl
  o Alt (anche con Maiusc) più lettera, cifra o tasto funzione (`Usable`); si rifiutano i tasti dell'altra e
  quelli dell'elenco (`ListKeys`). Sono `typeKeys` e `pickKeys`, **statici** perché la guida ne legge i nomi
  (`HotkeyName`, `PickName`): per questo `HelpForm.Lines` non è più statico e i suggerimenti che li nominano
  si riscrivono in `NameHotkeys`. Impostazioni `HotkeyKeys` e `PickKeys`.
- `TextRenderer.MeasureText` con `EndEllipsis` e nessuna larghezza proposta risponde la larghezza dei soli
  puntini: per sapere se un testo sta nella riga (`Cut`) si misura senza.
- `TextRenderer.MeasureText` **senza** un `Graphics` ignora `NoPadding` e conta lo spazio che Windows lascia attorno
  al testo («x»: 15 pixel invece di 6, misurato il 19/10/2026): i pezzi del testo segnato dalla ricerca si misurano su
  un `Graphics` (`Wide`, `measurer`), altrimenti gli spazi tra un pezzo e l'altro venivano larghi.
- Le righe degli elenchi disegnate direttamente sullo schermo sfarfallavano a ogni passo della luce sotto il
  puntatore (prima lo sfondo, poi il resto; notato dall'utente sulle cartelle dei salvati il 17/10/2026): `RowList`
  le disegna in un `BufferedGraphics` e le copia in un colpo (`OnDrawItem`). Il buffer sposta l'origine con una
  trasformazione che `TextRenderer` (GDI) ignora: senza `ResetTransform` e la riga passata con l'origine a zero, testo
  e icone finivano fuori dal buffer. Per questo chi disegna una riga deve misurare tutto da `e.Bounds`.
- WinForms di .NET Framework (compilato con il `csc` di Windows) legge e scrive «HTML Format» nella codepage di
  Windows, mentre tutti i programmi lo scrivono e lo leggono in UTF-8, e la sua intestazione conta byte UTF-8.
  Leggendo, una lettera accentata o uno spazio indivisibile diventava «Ã¨»/«Â », il testo non corrispondeva più a
  quello semplice (`Rich.Matches`) e la formattazione si perdeva (trovato dall'utente il 18/10/2026 copiando dalla
  chat di VS Code); scrivendo, «è» arrivava a Outlook o Word come «�». Per questo `Rich.Html` legge i byte veri
  (`Rich.Bytes`: lo stream messo da noi, o l'HGLOBAL chiesto a Windows con `ComTypes.IDataObject`) e `Rich.Put`
  scrive un `MemoryStream` di byte UTF-8. Un `DataObject` avvolto in un altro non passa da Windows: il percorso degli
  appunti veri i test non lo vedono, provato a mano.
- Ciò che si copia da una pagina in tema scuro (la chat di VS Code) porta con sé sfondi scuri e testo grigio chiaro:
  conservato com'era e incollato in Word, usciva grigio su riquadri neri (trovato dall'utente il 18/10/2026). Ogni
  HTML che entra dagli appunti passa da `Rich.Lighten`: via gli sfondi più scuri di `DarkBack` e i grigi più chiari
  di `LightGrey`, che come il nero diventano il colore predefinito (`Rich.Default`, anche in `Tidy`); i colori veri e
  i grigi medi delle firme restano.
- `ListBox.IndexFromPoint` risponde la riga più vicina anche sotto l'ultima: per sapere su quale riga è il
  puntatore c'è `RowAt` (prima un clic nel vuoto sceglieva di nuovo la voce selezionata).
- Nell'elenco Invio agisce al rilascio (`OnKeyUp`, con `enterDown`): alla pressione il tasto tenuto giù
  continuerebbe a ripetersi nella finestra a cui torna il primo piano. Il primo piano si restituisce prima di
  nascondere la finestra, finché il programma ha ancora il diritto di assegnarlo.
- Niente `AcceptButton`: con «Digita gli appunti» come pulsante predefinito, Invio nella casella dell'attesa o su
  una spunta delle opzioni faceva partire la digitazione.
- `previous` si azzera quando il programma perde il primo piano (`WM_ACTIVATEAPP`): altrimenti una scelta fatta
  molto dopo salterebbe alla finestra da cui era stato premuto Ctrl+D. Un secondo Ctrl+D a elenco aperto non lo
  tocca.
- Una scorciatoia rifiutata da Windows toglie la spunta ma si salva come ancora voluta (`refused`): al prossimo
  avvio si riprova, invece di restare spenta per sempre.
- `COLLATE NOCASE` di SQLite ignora le maiuscole solo delle lettere inglesi: per i nomi dei salvati e delle
  cartelle lo schema ha una colonna `name_key` scritta dal C# (`Clips.Key`, `ToUpperInvariant`).
- Un testo di C# legato a SQLite senza la sua lunghezza si ferma al carattere zero, e dei byte vuoti diventano
  NULL: `Statement.Bind` passa sempre la lunghezza e usa `zeroblob` per i vuoti.
- In un file `.cmd` `if errorlevel 1` vale per i codici da 1 in su: un programma che va in crash esce con un
  codice negativo e passerebbe per riuscito. `test.cmd` usa `if %errorlevel% neq 0` (trovato il 17/10/2026, quando
  un test andato in crash ha scritto «TUTTI I CONTROLLI SUPERATI»).
- I file `.cmd` con fine riga LF fanno perdere a `cmd.exe` le etichette dei `goto` («The system cannot find the
  batch label specified»): `.gitattributes` li vuole CRLF (`*.cmd text eol=crlf`), anche su GitHub. Scrivendoli
  con strumenti che salvano LF, riconvertirli.
- `using System.IO` e una proprietà `Path` nella stessa classe si nascondono a vicenda: in `Clips` il percorso
  del file è `FileName`, e `System.IO.Path` si scrive per intero.
- `Control.Visible` risponde falso se la finestra è nascosta. Lo stesso vale per le voci di un menu mentre si sta aprendo (`Opening`): «Sposta in» restava vuoto perché
  il codice rileggeva `moveItem.Visible` appena assegnato.
- Una finestra mai mostrata non è in `Application.OpenForms`, nemmeno con l'handle già creato: `Look.Switch` non
  la ricolorerebbe. Per questo la finestra delle opzioni si iscrive con `Look.Keep` (trovato il 17/10/2026 dal
  controllo di `shot` che cambia tema prima di aprirla).
- Mentre il gestore di `Application.ThreadException` è in corso, WinForms scarta in silenzio ogni altro errore,
  senza nemmeno chiamare il gestore: per questo `Report` scrive nel log e torna subito, e la finestra si apre dopo
  (`SynchronizationContext.Post`). Provato dal vivo il 17/10/2026 con due errori provocati: prima il secondo
  mancava dal log.
- Avviare il programma vero riscrive le impostazioni dell'utente alla chiusura. I test no: vedi sotto.
- `MainForm.SettingsKey` e `Clips.DefaultFile` vanno cambiati insieme: con le impostazioni di una chiave vuota vale
  il default di 30 giorni, che sul database vero cancellerebbe i giorni più vecchi dell'utente.

## Controlli

`tests\test.cmd` compila `tests.exe` (`src\*.cs` + `Test.cs`, ingresso `TestProgram`) ed esegue `map`, `texts`,
`data` e `type`; `test.cmd ci` salta `type` (vuole un desktop su cui digitare) e le pause, ed è ciò che GitHub esegue a
ogni push. **I test non toccano mai i dati dell'utente**: `TestProgram.Main` punta `MainForm.SettingsKey` (e
`OldSettingsKey`) su `HKCU\Software\SmartClip test`, `Clips.DefaultFile`, `Migration.OldHistory` e `Log.FileName` su
una cartella temporanea, e cancella tutto prima e dopo. Vale per ogni modo, anche `shot`, `real` e `store`, che
quindi partono dalle impostazioni di default e scrivono le voci finte (`FillSample`) nel database di prova: le
cartelle vi stanno in ordine di nome, quindi «Casa» viene prima di «Lavoro».

- `tests.exe map` — stampa la mappa della tastiera attiva e confronta la tabella americana (solo lettere e
  cifre, se il layout USA non è installato).
- `tests.exe texts` — la logica attorno all'elenco, senza aprirlo e senza toccare gli appunti: formati riservati,
  ricerca, percorsi delle cartelle, file esportato, caratteri, testi formattati (`RichTest`), lingue, il log, il
  testo delle immagini (`OcrTest`: due righe a 16 px, una trasparente, una vuota, le accentate in italiano; dove
  Windows non ha lingue da leggere, come forse il server di GitHub, scrive `[SKIP]`).
- `tests.exe data` — il database, in file della cartella di prova: per ora lo strato SQLite (`SqliteTest`: testi
  e byte in andata e ritorno, NULL e byte vuoti, regole rifiutate, transazioni annidate, file liberato alla
  chiusura) e `Clips` (`ClipsTest`: copiati, freccia, immagini, numeri, regole dello schema provate scrivendo
  SQL a mano, salvati con e senza nome, giorni, cartelle) e la migrazione (`MigrationTest`: i dati vecchi li
  scrive il `Texts` di oggi nella chiave e nella cartella di prova, più i casi difficili scritti a mano; poi
  il confronto voce per voce, i dati vecchi intatti, una migrazione fallita a metà che al riavvio riparte) e
  i testi salvati formattati (`FormattedTest`), l'importazione di un file esportato (`BackupTest`) e lo spostamento del database in un'altra cartella
  (`MoveTest`). Lo spostamento dalla finestra, la cartella scelta che all'avvio non c'è più e il giro completo
  «esporta dalla finestra, importa in un database vuoto» (come tra due PC: `Exported`) li prova `shot`.
- `tests.exe type` — apre una finestrella e ci digita davvero: tutti i caratteri ASCII, accentate, simboli con
  AltGr, Unicode, Alt+codice, Tab, Invio; poi verifica che finestra sbagliata e annullamento blocchino tutto.
  Ruba il focus per un paio di secondi: avvisare l'utente prima di lanciarlo.
- `tests.exe shot <prefisso>` — non è in `test.cmd`: salva immagini PNG della finestra (elenco con voci
  finte, una scheda sotto il puntatore in `-hover`, le schede alte due righe in `-two`, una ricerca con la parola evidenziata in `-search`,
  quattro schede scelte con la loro barra in `-picked`, un elenco di tre giorni scorso fino a ieri in `-scrolled`, opzioni con ogni pagina in `-options-1`…`-5`, stati lunghi) per
  controllare l'aspetto senza mostrarla.
- `tests.exe store <cartella> <lingua>` — nemmeno: le immagini della scheda dello Store, finestre vere su uno
  sfondo nei colori dell'icona (`Backdrop`) alla misura richiesta (1366 × 768), in `<cartella>\<lingua>` (in scuro, che sullo Store fa più effetto, tranne l'ultima, `8-light`; lo sfondo si scurisce con il tema), e
  l'icona 300 × 300 in `<cartella>`. Mostra le finestre qualche secondo; a programma chiuso, come `real`, perché le
  scorciatoie non risultino occupate.
- `shot`, `real` e `store` accettano la lingua come ultimo argomento (`en`, `es`, `fr`, `it`; senza, inglese) e
  usano voci finte in quella lingua (`Sample`). Per le immagini vere `tests.exe` va compilato con
  `/resource:SmartClip.ico,SmartClip.ico`, altrimenti la barra del titolo ha l'icona generica; `real` mette dietro
  le finestre lo stesso sfondo di `store`, perché ai bordi non compaia lo schermo. Dopo la lingua accettano
  `dark` o `light` (senza, il tema di Windows). `real` fotografa anche una domanda (`-real-ask`), il menu del
  tray con una cartella aperta (`-real-menu`), la scelta del numero di una voce
  (`-real-number`), un elenco lungo con la barra sottile (`-real-scroll`) e un suggerimento (`-real-tip`).

Il tray è stato verificato con un programmino usa e getta (non nel progetto) che crea `MainForm`, legge i campi
`tray` e `trayBox` per riflessione e aziona le voci del menu con `PerformClick`: X nasconde, Apri riapre anche
da ridotta a icona, Esci chiude, opzione spenta toglie l'icona e la X torna a chiudere.

## Mai verificato

- Se il client remoto inoltra i caratteri Unicode. La digitazione in una sessione remota vera invece funziona:
  provata dall'utente il 14/10/2026.
- I simboli della tabella americana (controllati a mano), i tasti morti, schermi oltre il 100% di scala.
- Il clic reale sull'icona del tray e il fumetto di avviso.
- Il passaggio dell'icona a grigio con gli appunti vuoti (visto solo lo stato "testo presente" e le due immagini).
- L'elenco dal vivo: clic e Invio che rimettono la voce negli appunti, Ctrl+S, Ctrl+D, apertura con la
  scorciatoia e ritorno del primo piano alla finestra di partenza. Controllate solo la logica (`texts`) e
  l'immagine.
- Che il formato «riservato» messo sui testi salvati tenga davvero fuori la cronologia di Windows (Win+V).
- I formati riservati sugli appunti veri (provati solo su oggetti in memoria) e la cattura a ogni copia.
- Le immagini dal vivo: cattura di una schermata vera e ritorno negli appunti.
- «Copia il testo» dal vivo: il clic sulla voce del menu, la copia che entra nell'elenco, un Windows senza lingue
  per il riconoscimento (l'avviso), Windows 10, il pacchetto dello Store. Provati `Ocr.Read` (`texts`, e una
  schermata intera letta da un programmino usa e getta) e la compilazione.
- L'app di provenienza dal vivo: provati `Origin.ProgramOf` su una finestra dei test, `Clips` con eseguibili veri e
  l'immagine di `shot`; mai una copia vera da Chrome, Office, un'app dello Store (il proprietario potrebbe essere
  `ApplicationFrameHost`, e l'icona del suo eseguibile quella generica), un desktop remoto (dovrebbe dire «Connessione
  Desktop remoto»), un programma da amministratore.
- Citrix dal vivo (19/10/2026): provato `Origin.Within` solo su finestre dei test con icone diverse. Mai su Citrix vero:
  se le finestre di `wfica32.exe` rispondono a `WM_GETICON` con l'icona del programma remoto (Gestione attività la
  mostra), se l'impronta resta la stessa da una copia e da una sessione all'altra (altrimenti lo stesso programma
  comparirebbe più volte), chi possiede gli appunti dopo una copia remota.
- I pulsanti delle schede solo sotto il puntatore (19/10/2026) dal vivo: vista l'immagine di `shot` con la luce portata
  a mano a 1 e provato che l'altezza non cambia; mai la dissolvenza vera tra riga piccola e pulsanti, né il clic su un
  segno appena comparso. Nemmeno il suggerimento di un nome tagliato nella barra laterale con i nomi, né il clic sul
  numero di una scheda (provato solo che `PartAt` lo riconosce) con il suo elenco che si apre lì sotto.
- La barra laterale trascinata e ridotta (19/10/2026) dal vivo: provati da `shot` riduzione, ritorno e limiti della
  larghezza, vista l'immagine ridotta; mai il bordo trascinato con il mouse vero, il cursore ↔, i suggerimenti delle
  icone, il clic sul ☰.
- La finestra con la barra laterale e le schede (18/10/2026) dal vivo: viste le immagini di `shot` in chiaro e in scuro, provati da `shot`
  posti, schede dei tipi, giorni e selezione; mai i clic veri sui pulsanti delle schede, sulle frecce della barra,
  il trascinamento sulla barra, il menu di una cartella, schermi oltre il 100% (le schede alte più di 255 pixel si
  tagliano), Windows 10.
- Più schede scelte e la barra del giorno (20/10/2026) dal vivo: provati da `shot` `Pick` (Ctrl, Maiusc, una che resta
  sola), la barra, la ricarica, «Tieni in alto» avanti e indietro, `MoveAll` con un'immagine e un testo formattato,
  `ShowDay` scorrendo, i messaggi del tasto sinistro mandati alla lista con e senza Ctrl, e viste le immagini in quattro
  lingue; mai il mouse vero, Ctrl+A e Maiusc+frecce premuti davvero, la domanda di «Elimina», il menu di «Sposta in» aperto dal
  pulsante, la rotella sopra la barra del giorno.
- L'avviso di SQLite mancante o troppo vecchio: nessun Windows a portata di mano ne è privo.
- Il database con migliaia di voci (la ricerca, la scadenza, il file che si restringe): provato solo con le
  poche dei test. E dal pacchetto dello Store, dove sta in `LocalState`.

- Le ultime aggiunte dal vivo: l'anteprima con il puntatore fermo (vista solo chiamando `ShowPeek` da
  `tests.exe real`), il puntatore che la attraversa, il trascinamento in una cartella, i tasti nuovi premuti
  davvero (provata solo la logica, da `tests.exe shot`), la pausa, le conferme che spariscono.
- Il cambio di lingua dal vivo: provato con il programma vero che un avvio con `/restart` aspetta la chiusura di
  quello aperto e si riapre nella lingua salvata sulle opzioni; mai il clic sul menu delle lingue (che fa
  `Process.Start` e chiude), né dal pacchetto, dove l'eseguibile avviato così dovrebbe restare nel pacchetto. Le
  traduzioni spagnola e francese non le ha rilette un madrelingua.
- Il pacchetto dello Store: `pack.ps1` provato solo senza SDK (cartella e immagini), mai `makepri`/`makeappx`
  né il pacchetto installato; dal pacchetto non sono mai state provate la `StartupTask`, l'avvio all'accesso
  riconosciuto da `Packaged.StartedAtSignIn`, la cartella `LocalState`, né se il registro (impostazioni, testi
  salvati) finisce virtualizzato nel pacchetto o nel registro vero.
- La finestra che si nasconde cliccando altrove, dal vivo: in particolare il clic sull'icona del tray e sul
  pulsante della barra delle applicazioni a finestra aperta (Explorer prende il primo piano: la finestra si
  nasconde e poi il clic la riapre?) e la riapertura senza `TopMost`.
- Le sottocartelle e il menu del tray dal vivo: provati solo `Clips` (cartelle), l'ordine, `Filter` e la struttura
  del menu (`texts`, `shot`), mai il clic vero su una voce del tray, il trascinamento di una cartella, la
  finestra di una cartella con la scelta del posto.
- L'avvio vero all'accesso a Windows: provati la voce nel registro e l'avvio nascosto con un programmino usa e
  getta, mai una disconnessione e un nuovo accesso.
- Il movimento dal vivo: la luce che sfuma sulle righe e sui pulsanti, le pagine delle opzioni che salgono,
  l'anteprima in dissolvenza (con `Opacity` la finestra diventa stratificata: angoli arrotondati da guardare), la
  barra sottile trascinata, cliccata e con la rotella. Viste solo nelle immagini di `shot` e `real`, che le
  colgono ferme.
- I numeri delle voci dal vivo: Ctrl+Maiusc+cifra premuti davvero in un altro programma (anche in un desktop
  remoto), l'avviso dei tasti già usati, il clic su «Scorciatoia». Provati la logica (`texts`), la cattura dei
  tasti (`shot`), il menu aperto da `tests.exe real` e `Typer.Paste` in una finestrella (`type`).
- I testi formattati dal vivo: la copia di una firma vera da Outlook, Gmail o Word e il suo salvataggio, l'incolla
  formattato in Outlook, Gmail e Word, i pulsanti cliccati e Ctrl+B/I/U premuti davvero, i menu di carattere e
  dimensione. Provati la logica (`texts`: HTML letto e riscritto, la casella con i pulsanti chiamati dal codice,
  l'intestazione di «HTML Format»), il database e l'esportazione (`data`), la finestra con la firma di prova
  (`real`). Mai cambiato il tema a finestra aperta: `ForeColor` di una `RichTextBox` ricolora tutto il testo.
- Il tema dal vivo: il cambio da Impostazioni di Windows a programma aperto (provati solo `Look.Switch` da
  `shot` e le immagini di `real` in chiaro e in scuro), il menu delle caselle di testo scurito da
  `SetPreferredAppMode`, l'interruttore che scorre, l'anello del focus, `AskForm` aperta dal programma vero.
- Le novità del 18/10/2026 dal vivo: i segni nei menu visti solo nelle immagini di `real` (menu del tray e
  dell'elenco), la tacca che scivola solo in quattro fotogrammi di un programmino usa e getta, mai le schede delle
  opzioni cliccate davvero né Ctrl+F premuto, i pulsanti sottili sotto il puntatore, i tipi della riga di stato
  durante una digitazione vera. Da Windows 10 (Segoe MDL2 Assets) i glifi nuovi non sono mai stati guardati.
