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
  - `Program.cs` — avvio, istanza unica, attributi dell'assembly.
  - `MainForm*.cs` — la finestra, una sola classe `partial` divisa in cinque file quando ha superato di nuovo
    le 2000 righe (i nomi dei membri non sono cambiati: i test li cercano per riflessione):
    - `MainForm.cs` — costanti e campi (tutti qui, in cima), costruttore con la disposizione, viste, riga di
      stato, tray, icona, messaggi di Windows, chiusura.
    - `MainForm.List.cs` — le due colonne: `WireLists` (menu, mouse, trascinamento), riempimento e disegno
      delle righe, anteprima, scelta, modifica, cartelle, tasti dell'elenco, larghezza delle colonne.
    - `MainForm.Options.cs` — `PlaceOptions` (le opzioni e i loro suggerimenti), i `Place…`, esportazione,
      importazione, pulizia, `TransferSettings` e `AdoptOldSettings`.
    - `MainForm.Hotkeys.cs` — le scorciatoie globali: nomi, cambio dei tasti, registrazione.
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
  - `Look.cs` — colori chiari e scuri, carattere (`Look.TextFont`), menu, suggerimenti e controlli disegnati:
    `Look`, `Card`, `RoundButton`, `Toggle`.
  - `AskForm.cs` — le domande e gli avvisi, al posto di `MessageBox` (`AskForm.Ask`, `AskForm.Tell`).
  - `Peek.cs` — il riquadro che mostra una voce per intero (`Peek`), accanto alla sua riga.
  - `Texts.cs` — `Entry` e `Texts`: le voci dell'elenco, la cronologia su disco, i testi salvati.
  - `Backup.cs` — esportazione e importazione in un file di testo (`Backup`, `BackupResult`).
  - `Typer.cs` — mappa dei tasti e invio (`Typer`, `Stroke`, `TypeOptions`, `TypeResult`).
  - `Native.cs` — funzioni e costanti di Windows.
  - `Packaged.cs` — quando il programma gira dal pacchetto del Microsoft Store (`Packaged.Family` non nullo):
    avvio con Windows con la `StartupTask` del pacchetto invece della chiave `Run` (`Startup` lo chiama), avvio
    all'accesso riconosciuto dall'attivazione invece che da `/tray`, cronologia in `LocalState` del pacchetto
    (quella sotto `%LOCALAPPDATA%` Windows la sposterebbe in una copia nascosta che Esplora file non vede). È
    l'unico file con tipi di Windows Runtime: fuori dal pacchetto non vanno chiamati.
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
  `images\tile-300.png` è unica). `make-listing.ps1 <csv esportato da Partner Center>` scrive
  `store\import\SmartClip-listing` (CSV compilato per tutte le lingue + immagini) da importare con *Store
  listings → Import listings → Import folder*: legge i testi da `README.md`, quindi i blocchi di codice di
  ogni lingua vanno tenuti in quell'ordine (nome, descrizione breve, descrizione, caratteristiche, termini, novità della versione da riscrivere a ogni invio) e i
  titoli delle sezioni con il codice tra parentesi, «## English (en-US)». `PRIVACY.md` è l'informativa indicata allo Store: va tenuta vera (oggi il programma non
  usa la rete).
- `build.cmd` — compila `src\*.cs` in `SmartClip.exe`. `SmartClip.ico` è facoltativa. L'eseguibile non sta
  più in git (`.gitignore`): si scarica dalle release.
- `.github/workflows/release.yml` — pubblica l'eseguibile come release di GitHub. **Per fare una release si
  cambia `AssemblyVersion` in `src\Program.cs` e si fa push**: il flusso compila con `build.cmd` e crea la
  release `vX.Y.Z` solo se con quel numero non esiste ancora (scelta dell'utente, invece di una release a
  ogni push). Il numero di versione sta solo lì; la finestra lo mostra nel titolo (`Program.Version`). La
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
- `Packaged.cs` usa Windows Runtime: `build.cmd` e `test.cmd` riferiscono `Windows.ApplicationModel.winmd` e
  `Windows.Foundation.winmd` da `%WINDIR%\System32\WinMetadata` e `System.Runtime.dll` accanto a `csc`, tutti già
  in Windows 10 e 11. Niente `AsTask` (vorrebbe `Windows.winmd` dell'SDK): le operazioni asincrone si aspettano
  con `Completed` (`Packaged.Wait`). Compilando a mano vanno aggiunti gli stessi `/r:`.
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
  copiare altrove. La guida si chiude con lei: senza pulsante sulla barra resterebbe persa. Per farci stare la
  riga senza alzare la finestra è sceso ancora `ListExtra`.

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

- La finestra principale è l'elenco (voluto dall'utente), su due colonne affiancate (anche questo voluto: i
  salvati non devono stare in cima ai copiati): a sinistra `recentList` con le cose copiate (le 200 più recenti che corrispondono alla ricerca, `MaxShown`), a
  destra `savedList` con i testi salvati. La voce selezionata è una sola in tutta la finestra (`active`,
  `Selected`); Tab passa da una colonna all'altra. Tutte e due si filtrano mentre si scrive (`Texts.Matches`: parole in qualsiasi ordine, senza maiuscole né accenti). Il
  pulsante «Opzioni»/«Elenco» alterna nella stessa area le due viste (`ShowView`, `listControls` e
  `optionControls`). Le idee vengono dal progetto vicino `..\pastepad` (WinUI 3, .NET 10), riscritte qui in
  piccolo: niente modelli, formato, temi.
- Scegliere una voce (Invio o clic, `Use`) la rimette soltanto negli appunti: poi l'utente incolla con Ctrl+V o
  digita con Ctrl+Alt+V. Non si digita né si incolla da soli (provato e scartato dall'utente). Un testo salvato
  torna negli appunti con il formato che lo segna riservato; ciò che il programma stesso mette negli appunti
  non rientra nell'elenco (`ownCopy`, numero di sequenza degli appunti).
- Ctrl+D (`OpenList`) porta la finestra in primo piano da qualsiasi programma; dopo la scelta con Invio o
  doppio clic `GoBack` restituisce il primo piano alla finestra di partenza e rinasconde la finestra se era
  nascosta. Un clic singolo no (voluto dall'utente: la finestra spariva e la voce non si poteva più salvare).
- La cronologia dei copiati (`Texts.Recent`) resta su disco per i giorni dell'opzione (`Texts.Keep`, 30 di
  default): scelta dell'utente, che prima la voleva solo in memoria. Sta in `%LOCALAPPDATA%\SmartClip\History`,
  **in chiaro** (l'utente non vuole cifratura: «sono cose mie sul mio PC») e in **un file al giorno**
  (`2026-10-13.clips`), non uno per voce: l'utente copia centinaia di cose al giorno. Ogni copia si aggiunge in
  coda (`Store`); la retention elimina i file dei giorni scaduti (`Prune`); una voce tolta si azzera sul posto
  e il suo blocco si segna eliminato (`Erase`); un blocco troncato in coda si taglia alla lettura (`ReadFile`).
  Con 0 giorni niente va su disco e valgono i limiti di memoria (30 voci, 5 immagini). Si scartano le copie che
  i gestori di password segnano come riservate (`Texts.IsPrivate`). L'opzione spenta svuota elenco e disco.
- Una voce copiata si può tenere in cima alla colonna (`Entry.Pinned`, voluto dall'utente): stellina
  all'estremità destra della riga (`StarArea`, `OnStar`: lì il clic non sceglie la voce), menu del tasto destro
  e Ctrl+P, tutti in `TogglePin`. Nel file è un bit del tipo del blocco (`PinnedKind`), cambiato sul posto
  (`Texts.Pin`). Una voce fissata non scade: `Prune` tiene il file del suo giorno ma lo riscrive con le sole
  voci fissate (`Shrink`; l'utente non voleva file pieni di zeri, i file senza stelline si cancellano); per questo non fa nulla finché `Read` non ha letto i file (`loaded`), altrimenti all'avvio li
  cancellerebbe senza sapere delle stelline. Ricopiare lo stesso testo non la sposta e `Forget` non la toglie;
  i limiti di memoria non la contano. L'ordine (fissate prima) lo dà `Texts.Load`.
- «Pulisci ora…» nelle opzioni (voluto dall'utente): elimina subito ciò che è stato copiato prima degli ultimi
  N giorni scritti nella casella accanto (`Spared`, non è un'impostazione: riparte da 0, cioè tutto), dopo una
  conferma. `Texts.Clean` e la retention passano dallo stesso `Drop`; le voci fissate restano. Il pulsante si
  mette con `PlaceButton`.
- Anche le immagini copiate entrano in `Texts.Recent`, compresse in PNG; il loro `Text` è la descrizione che si
  vede e si cerca. Se negli appunti c'è anche testo vince il testo. Non diventano testi salvati. Una volta nel
  file, in memoria resta solo la miniatura: il PNG si rilegge quando serve (`Entry.LoadPng`).
  La stessa copia annunciata più volte da Windows si riconosce confrontando i byte del PNG.
  Nell'elenco occupano tre righe, con una miniatura (`Entry.Preview`, creata una volta alla copia).
- I testi salvati stanno in `HKCU\Software\SmartClip\Saved`, un valore binario per nome, cifrato con DPAPI
  per l'utente (`ProtectedData`, in `System.Security.dll` che `csc` riferisce già da solo). L'elenco ne mostra
  solo il nome e, a destra, il giorno in cui è stato salvato: sta nella sottochiave `Saved\Dates`, sotto lo
  stesso nome, così il formato dei testi non cambia e quelli salvati prima restano semplicemente senza data.
- Cartelle dei testi salvati (volute dall'utente), anche una dentro l'altra (sottocartelle volute dopo, dalla
  3.8). Nel codice si chiamano «group», perché
  `Texts.Folder` è già la cartella della cronologia. Stanno in due sottochiavi di `Saved`: `Folders` (un valore
  per cartella, 1 se aperta: lo stato si ricorda) e `Places` (nome del testo → cartella), così il formato dei
  testi non cambia. Una cartella è il suo **percorso**, i nomi uniti da `Texts.Separator` (`\`, vietato nei
  nomi, `Refused`): `Lavoro\Server`. Le cartelle di prima sono semplicemente al primo livello. Gli aiuti
  stanno in `Texts` (`Parent`, `Leaf`, `Join`, `Within`, `Depth`, `Display` con « › », `CleanPath` per i file
  esportati); `Groups` ordina nome per nome (`ByPath`) e aggiunge le cartelle esterne mancanti. Nell'elenco una
  cartella è una `Entry` con `IsGroup` e `Name` uguale al percorso (`Label` ne mostra l'ultimo nome, `Level`
  dà il rientro), messa da `Texts.Load` subito prima dei suoi testi e poi delle sue sottocartelle: per questo
  dove si scorre `entries` cercando i testi va escluso `IsGroup`. Una cartella chiusa nasconde anche quelle
  dentro (`InView`). Clic o Invio la
  aprono e chiudono (`Accept`, `ToggleGroup`), «Rinomina…» ed «Elimina» sono gli stessi pulsanti dei testi.
  `RenameGroup` cambia il percorso di una cartella e di tutto ciò che contiene (`Rebase`): è anche lo
  spostamento; con `null` elimina la cartella e il contenuto sale di un livello (scelta dell'utente: i testi
  potrebbero essere password). «Nuova cartella…»
  (Ctrl+Maiusc+N, nasce dentro la cartella della selezione) e «Sposta in» (anche per le cartelle, mai dentro
  se stesse: `CanGo`) stanno nel menu del tasto destro, che ora si apre anche senza selezione. In
  `EditForm` la cartella si sceglie da un pulsante che apre un menu (un `ComboBox` resterebbe chiaro), con il
  percorso accorciato da sinistra se non ci sta; la stessa finestra, senza contenuto, chiede nome e posto di
  una cartella. In una ricerca le cartelle compaiono solo se contengono risultati, anche da chiuse, insieme a
  quelle che le contengono.
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
  i copiati tornano con la loro ora e la stellina nel file del loro giorno (`Texts.Restore`), tranne quelli che
  l'elenco ha già; con la cronologia spenta non si importano. Per farci stare i pulsanti senza alzare la
  finestra è sceso `ListExtra`.
- Le voci stanno su una riga sola, con una spaziatura comoda (`RowPadding`) e l'ora a destra (`When`). Le righe
  a due livelli, con sotto ora, caratteri e righe, sono state provate e scartate dall'utente: il numero di
  caratteri non gli serve.
- Anteprima (voluta dall'utente «con il mouse che resta sopra un po'»): il puntatore fermo `RestMs` su una voce
  copiata che la riga taglia (`Cut`) la mostra per intero in `Peek`, sotto la riga (`ShowPeek`, timer `rest`
  avviato da `Hover`). `Peek` non prende il primo piano e lascia passare il puntatore (`HTTRANSPARENT`), così
  l'elenco continua a seguire il mouse; sparisce a ogni tasto, clic, rotella, ricarica dell'elenco. Dei testi
  salvati **non** compare passandoci sopra (potrebbero essere password, scelta mia da confermare con l'utente):
  c'è «Mostra il contenuto» nel menu del tasto destro, che lo lascia in vista `AskedMs` (`peekAsked`).
- Pausa della cattura (voluta dall'utente): «Sospendi/Riprendi la cattura» nel menu dell'icona e in quello
  dell'elenco (`Pause`, `paused`, `pauseItems`). Non è un'impostazione: a ogni avvio si riparte in ascolto.
  Lo dicono il titolo della colonna, la colonna vuota e il suggerimento dell'icona.
- Un testo salvato, o una cartella, si trascina in una cartella (voluto dall'utente): `DoDragDrop` parte da `MouseMove` solo
  oltre `DragSize`, così un clic resta un clic; `GroupAt` dice la cartella della riga sotto il puntatore
  (nessuna fuori dalle cartelle), `Aim` la illumina, il rilascio passa da `MoveTo` come «Sposta in».
- I testi salvati si scrivono in `EditForm` (nome e contenuto), aperta da `Edit`: testo nuovo, voce copiata
  da tenere, voce salvata da cambiare. Le stesse tre azioni stanno nei pulsanti sotto l'elenco, nel menu del
  tasto destro e in Ctrl+N/S/E. Il nome scritto nella casella di ricerca è stato provato e scartato dall'utente
  (poco chiaro). Il clic sinistro sceglie la voce, il destro la seleziona soltanto.

- Tema (voluto dall'utente, a cui lo scuro con accenti pastello di prima non piaceva più, ma nemmeno il
  bianco): grigio caldo neutro con tocchi di colore nelle icone. I colori stanno tutti in cima a `Look`; i
  segni (`Sign`, disegnati da `Look.Draw` su una griglia di 16, senza immagini) dicono cos'è una voce — testo,
  indirizzo, salvato — e stanno sui pulsanti (`RoundButton.Sign`). Barra del titolo e barre di scorrimento
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
- Modernità alla Windows 11 (stessa richiesta): menu arrotondati da Windows (`DWMWCP_ROUNDSMALL`), con righe
  alte, testo spostato a destra e una forma arrotondata sotto il puntatore (`MenuLook`, `Look.Style` del
  menu); suggerimenti disegnati nei colori della finestra (`Look.Style` del `ToolTip`); le domande in una
  finestra del programma (`AskForm`) invece di `MessageBox`, che resterebbe bianca nello scuro: testo su
  un'area più chiara e pulsanti sotto, con quello colorato su «No», che è anche quello di Invio ed Esc;
  interruttori con la manopola che scorre e cresce sotto il puntatore; pulsanti con il bordo più scuro in
  basso; anello del focus solo usando la tastiera (`ShowFocusCues`). Mica non si usa: con i controlli GDI lo
  sfondo trasparente non regge.
- Aspetto (`Look`): barra del titolo nel colore della finestra
  (`DwmSetWindowAttribute`, solo Windows 11: altrove fallisce senza danni), pulsanti piatti con un solo
  pulsante in colore d'accento, righe dell'elenco disegnate a mano (`DrawEntry`) con il segno ★/▣ in colonna.
  I controlli che Windows non lascia scurire sono stati sostituiti: le caselle di testo stanno senza bordo
  dentro un pannello (`Look.Frame`), le spunte sono interruttori disegnati (`Toggle`, restano `CheckBox`; voluti
  dall'utente al posto dei quadratini), l'attesa è una casella di testo
  numerica e la tastiera del server un interruttore (prima `NumericUpDown` e `ComboBox`, rimasti chiari).
  Il carattere è Segoe UI Variable Text dove c'è (Windows 11), altrimenti quello dei messaggi di sistema:
  `Look.TextFont`, usato da tutte le finestre.
  Barra del titolo e barre di scorrimento scure non compaiono nelle immagini di `tests.exe shot`: per
  vederle c'è `tests.exe real <prefisso>`, che mostra le finestre un attimo e fotografa lo schermo.
  Forme arrotondate (l'utente trovava la finestra piatta e asettica, ma i colori vanno bene): ricerca, colonne
  e caselle stanno in un `Card` (bordo sottile, d'accento quando il contenuto ha il focus; `Look.Wrap`; attorno
  a una casella di testo invece, come in Windows 11, una riga d'accento sul fondo, `Card.Underline`), i
  pulsanti sono `RoundButton`, la riga selezionata è una forma arrotondata con una tacca d'accento e quella
  sotto il puntatore si illumina (`Hover`, indice in `ListBox.Tag`). Le voci copiate mostrano a destra l'ora
  (o il giorno, se non è oggi); sopra ogni colonna il numero di voci; una colonna vuota dice perché.
  La descrizione sotto il pulsante deve stare in due righe: è il motivo della larghezza (`ContentWidth`).

- Disposizione (voluta dall'utente: «Digita gli appunti» in cima era troppo ingombrante): in alto la ricerca,
  poi le colonne (o le opzioni), sotto la riga di stato e un'unica fila di pulsanti piccoli: a sinistra quelli
  della voce selezionata, a destra «Digita gli appunti», «Opzioni»/«Elenco» e il quadratino «?» della guida
  (una scritta «Guida» nella fila non ci sta).
  La riga di stato occupa solo lo spazio di ciò che dice (`FitStatus`: nessuna, una o due righe; il resto va
  alle colonne) e a riposo tace (`ShowIdle`): come si digita una password sta nel suggerimento del pulsante.
  Le conferme si scrivono con `Say` e spariscono dopo `FadeMs`; avvisi, errori e ciò che riguarda la
  digitazione si assegnano a `status.Text` e restano fino al messaggio dopo (l'esito di una digitazione fatta
  a finestra nascosta deve potersi leggere riaprendola).

- La finestra è ridimensionabile (voluto dall'utente), mai più piccola di come nasce (`MinimumSize` in
  `OnLoad`). La dimensione non si salva: a ogni avvio torna quella di default; si ricorda solo la posizione.
  Allargandola cresce la colonna dei copiati (`Anchor`); le opzioni restano in alto a sinistra.
  Lo spazio tra le due colonne si trascina (`divider`, `PlaceColumns`; voluto dall'utente, a cui i salvati
  stavano stretti): la larghezza dei salvati si salva (`SavedWidth`, in pixel a 96 dpi) e si applica in
  `OnLoad`, quando la finestra ha la sua scala. Nessuna colonna scende sotto `MinColumn`; doppio clic sullo
  spazio torna alla larghezza di partenza.

## Regole di stile volute dall'utente

- Nessun valore duplicato: ogni default, nome o numero sta in un posto solo (costanti in cima a `MainForm`,
  costanti di Windows in `Native` con i nomi ufficiali, scorciatoia definita una volta sola).
- Aggiungere un'opzione: il campo in cima a `MainForm`, una riga `PlaceOption` (`PlaceNumber` se è un numero,
  `PlaceHotkey` se è una scorciatoia globale) in `PlaceOptions` sotto il titolo della sua
  categoria (`PlaceTitle`), una in `TransferSettings` e il suggerimento `Tip`, tutto in `MainForm.Options.cs`. In `optionControls` entra da sola.
  Etichetta e suggerimento sono campi di `Strings` (inglese) tradotti nelle altre tre lingue.
- **Nessun testo visibile scritto nel codice**: ogni testo nuovo è un campo di `Strings.cs` in inglese e una riga
  in ciascuno dei tre file delle lingue, con gli stessi `{0}`. `tests.exe texts` controlla che nessuna traduzione
  chieda più valori dell'inglese (sarebbe un `FormatException` a programma avviato), che le guide abbiano gli
  stessi argomenti, ed elenca i testi rimasti uguali all'inglese. Gli spazi sono tarati sull'italiano, che è
  tra le lingue più lunghe: dopo aver cambiato etichette guardare `tests.exe shot <png> <lingua>` in tutte e
  quattro (francese e spagnolo sono i più a rischio).
  Le categorie sono su due colonne (volute dall'utente, l'elenco unico era caotico): a sinistra «Digitazione»
  e «Programma», a destra «Scorciatoie» e «Cronologia» (così le due colonne sono alte uguali e la finestra non
  cresce: l'utente non la voleva più alta). I titoli sono in grassetto con una riga sotto: il colore
  d'accento resta a ciò che si clicca (il link `PlaceLink` alla cartella della cronologia, prima indistinguibile
  dai titoli). L'altezza della finestra si calcola da sola.

## Lingua

- Scelta in «Program» con un pulsante che apre l'elenco delle lingue, ognuna con il proprio nome
  (`PlacePicker`, `ChangeLanguage`); l'etichetta dice anche «Language» in ogni lingua, per chi ci arriva per
  sbaglio. Impostazione `Language` (stringa, `LanguageSetting`), letta da `Main` con `SavedLanguage` prima di
  creare la finestra; senza impostazione vale l'inglese (voluto dall'utente).
- Cambiare lingua **riavvia** il programma invece di riscrivere ogni testo al volo: `ChangeLanguage` lancia
  l'eseguibile con `/restart` (`Program.RestartArgument`) e si chiude salvando le impostazioni; il nuovo aspetta
  fino a dieci secondi che il vecchio liberi il mutex dell'istanza unica, poi si riapre sulle opzioni
  (`ShowOptions`). Per far stare la riga è sceso ancora `ListExtra`.
- Le domande non usano `MessageBox`, i cui pulsanti Sì/No seguirebbero la lingua di Windows: `AskForm` scrive
  `Strings.Yes`, `No`, `Ok`. Restano nella lingua di Windows le finestre di scelta dei file.

## Trappole già incontrate

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
- Le due scorciatoie globali si cambiano dalle opzioni (voluto dall'utente): il pulsante accanto
  all'interruttore mostra i tasti e ne aspetta di nuovi (`AwaitKeys`, `TakeKeys` da `ProcessCmdKey`). Durante
  l'attesa le due scorciatoie si tolgono, altrimenti Windows le prenderebbe prima della finestra. Valgono Ctrl
  o Alt (anche con Maiusc) più lettera, cifra o tasto funzione (`Usable`); si rifiutano i tasti dell'altra e
  quelli dell'elenco (`ListKeys`). Sono `typeKeys` e `pickKeys`, **statici** perché la guida ne legge i nomi
  (`HotkeyName`, `PickName`): per questo `HelpForm.Lines` non è più statico e i suggerimenti che li nominano
  si riscrivono in `NameHotkeys`. Impostazioni `HotkeyKeys` e `PickKeys`.
- `TextRenderer.MeasureText` con `EndEllipsis` e nessuna larghezza proposta risponde la larghezza dei soli
  puntini: per sapere se un testo sta nella riga (`Cut`) si misura senza.
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
- Il registro non cambia maiuscole e minuscole di un nome che ha già: `Texts.Save` toglie il valore prima di
  riscriverlo.
- `Control.Visible` risponde falso se la finestra è nascosta: quale vista è attiva lo dice `optionsShown`.
  Lo stesso vale per le voci di un menu mentre si sta aprendo (`Opening`): «Sposta in» restava vuoto perché
  il codice rileggeva `moveItem.Visible` appena assegnato.
- Avviare il programma vero o creare `MainForm` nei test riscrive le impostazioni dell'utente alla chiusura:
  salvarle prima e ripristinarle dopo.

## Controlli

`tests\test.cmd` compila `tests.exe` (`src\*.cs` + `Test.cs`, ingresso `TestProgram`) ed esegue:

- `tests.exe map` — stampa la mappa della tastiera attiva e confronta la tabella americana (solo lettere e
  cifre, se il layout USA non è installato).
- `tests.exe texts` — la logica dell'elenco, senza aprirlo e senza toccare gli appunti: formati riservati,
  testi copiati, cronologia su disco (in una cartella temporanea, non in quella dell'utente), ricerca, andata e ritorno cifrata di un testo salvato (scrive e toglie un valore `~test …`).
- `tests.exe type` — apre una finestrella e ci digita davvero: tutti i caratteri ASCII, accentate, simboli con
  AltGr, Unicode, Alt+codice, Tab, Invio; poi verifica che finestra sbagliata e annullamento blocchino tutto.
  Ruba il focus per un paio di secondi: avvisare l'utente prima di lanciarlo.
- `tests.exe shot <prefisso>` — non è in `test.cmd`: salva immagini PNG della finestra (elenco con voci
  finte, opzioni, stati lunghi) per controllare l'aspetto senza mostrarla.
- `tests.exe store <cartella> <lingua>` — nemmeno: le immagini della scheda dello Store, finestre vere su uno
  sfondo nei colori dell'icona (`Backdrop`) alla misura richiesta (1366 × 768), in `<cartella>\<lingua>` (in chiaro, tranne l'ultima, `8-dark`), e
  l'icona 300 × 300 in `<cartella>`. Mostra le finestre qualche secondo; a programma chiuso e salvando prima le
  impostazioni, come `real`.
- `shot`, `real` e `store` accettano la lingua come ultimo argomento (`en`, `es`, `fr`, `it`; senza, inglese) e
  usano voci finte in quella lingua (`Sample`). Per le immagini vere `tests.exe` va compilato con
  `/resource:SmartClip.ico,SmartClip.ico`, altrimenti la barra del titolo ha l'icona generica; `real` mette dietro
  le finestre lo stesso sfondo di `store`, perché ai bordi non compaia lo schermo. Dopo la lingua accettano
  `dark` o `light` (senza, il tema di Windows). `real` fotografa anche una domanda (`-real-ask`), il menu del
  tray con una cartella aperta (`-real-menu`) e un suggerimento (`-real-tip`).

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
- La cronologia su disco dal vivo: provata solo da `tests.exe texts`, mai con migliaia di voci né con il
  programma vero che scrive a ogni copia.

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
- Le sottocartelle e il menu del tray dal vivo: provati solo `RenameGroup`, l'ordine, `Filter` e la struttura
  del menu (`texts`, `shot`), mai il clic vero su una voce del tray, il trascinamento di una cartella, la
  finestra di una cartella con la scelta del posto.
- L'avvio vero all'accesso a Windows: provati la voce nel registro e l'avvio nascosto con un programmino usa e
  getta, mai una disconnessione e un nuovo accesso.
- Il tema dal vivo: il cambio da Impostazioni di Windows a programma aperto (provati solo `Look.Switch` da
  `shot` e le immagini di `real` in chiaro e in scuro), il menu delle caselle di testo scurito da
  `SetPreferredAppMode`, l'interruttore che scorre, l'anello del focus, `AskForm` aperta dal programma vero.
