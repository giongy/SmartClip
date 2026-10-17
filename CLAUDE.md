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
    - `MainForm.List.cs` — le due colonne: `WireLists` (menu, mouse, trascinamento), riempimento e disegno
      delle righe, anteprima, scelta, modifica, cartelle, tasti dell'elenco, larghezza delle colonne.
    - `MainForm.Options.cs` — `PlaceOptions` (le opzioni e i loro suggerimenti), i `Place…`, esportazione,
      importazione, pulizia, `TransferSettings` e `AdoptOldSettings`.
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
  - `Look.cs` — colori chiari e scuri, caratteri (`Look.TextFont`, `StrongFont`, `TitleFont`, `EntryFont`), icone (`Draw`),
    righe (`DrawRow`, `DrawTopic`), forme (`Rounded`, `Pill`), menu, suggerimenti e controlli disegnati: `Look`, `Card`,
    `RoundButton`, `Toggle`. Anche `Window`, la base di tutte le finestre tranne `Peek`: carattere e colori
    dell'interfaccia, barra del titolo (`Caption`), `Finish` alla fine del costruttore (la scala, vedi le trappole),
    `NewLabel`, `PlaceOnScreen`, le misure dei pulsanti.
  - `Motion.cs` — ciò che si muove: `Fade` (un valore che va verso 0 o 1 a piccoli passi), `Glow` (la luce sotto
    il puntatore nelle righe di un elenco; `Glow.Row` è la riga sotto il puntatore), `RowList` (un `ListBox` con la
    sua `Glow`, che avvisa quando scorre; `RowAt` dice la riga sotto un punto, `LightUnderPointer` basta agli
    elenchi che non chiedono altro),
    `Entrance` (una pagina che sale al suo posto), `Rail` (la barra di scorrimento sottile).
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
  copiare altrove. La guida si chiude con lei: senza pulsante sulla barra resterebbe persa.

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
  testo di ogni voce si prepara una volta sola senza accenti e in minuscolo (`Texts.Fold`, `Entry.Folded`) e poi si
  confronta ordinalmente: dieci volte più veloce del confronto culturale con `IgnoreNonSpace` (misurato: 45 → 4,6 ms
  su 5000 voci), che però trovava anche «ae» in «æ». Il
  pulsante «Opzioni»/«Elenco» alterna nella stessa area le due viste (`ShowView`, `listControls` e
  `optionControls`). Le idee vengono dal progetto vicino `..\pastepad` (WinUI 3, .NET 10), riscritte qui in
  piccolo: niente modelli, formato, temi.
- Scegliere una voce (Invio o clic, `Use`) la rimette soltanto negli appunti: poi l'utente incolla con Ctrl+V o
  digita con Ctrl+Alt+V. Non si digita né si incolla da soli (provato e scartato dall'utente; l'eccezione voluta
  sono i numeri delle voci, sotto). Un testo salvato
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
- Numeri delle voci (voluti dall'utente nella 3.9.2): un testo salvato o una voce copiata può avere un numero da 1
  a `MaxNumber` (9); i modificatori dell'opzione «Incolla per numero» (`quickKeys`, Ctrl+Maiusc di default:
  con Ctrl+Alt le cifre di molte tastiere scrivono simboli con AltGr) più la cifra sono una scorciatoia globale che
  mette la voce negli appunti e la **incolla** nella finestra in primo piano con Ctrl+V, senza mostrare la
  finestra (`PasteNumber`, `Typer.Paste`, che aspetta il rilascio dei tasti e manda Ctrl più il V della tastiera
  di quella finestra come scancode). Voluto dall'utente: la prima versione, Ctrl+D tenendo Ctrl e poi la cifra
  nell'elenco, apriva la finestra e non incollava, ed è stata scartata. È l'unico caso in cui il programma
  incolla da solo. Si registrano solo i numeri che qualche voce ha (`HookNumbers`, rifatto a ogni `Reload` e da
  `UpdateHotkeys`; id `NumberId` + numero), così gli altri restano agli altri programmi; quelli che un altro
  programma ha già si dicono nella riga di stato una volta (`numbersTaken`, azzerato quando l'utente assegna un
  numero). I tasti si cambiano come le altre scorciatoie (`AwaitKeys` con `quickBox`, `TakeQuickKeys`): si
  premono i modificatori con una cifra qualsiasi, e lì si provano tutti e nove i numeri dicendo quali sono già
  di altri; le altre due scorciatoie non possono essere modificatori + cifra (`NumberOf`). Impostazioni
  `QuickPaste` e `QuickKeys` (solo i modificatori). Si assegna e
  cambia da «Scorciatoia» nel menu del tasto destro (`ListNumbers`: «Nessuna» e i nove numeri, quello attuale
  disattivato, accanto agli altri la voce che li ha). Si vede in un riquadro d'accento a destra della prima linea della riga,
  prima della stellina (`NumberArea`), e nel menu del tray accanto al testo salvato
  (`ShortcutKeyDisplayString`, che `MenuLook` scrive in grigio). Un numero è di una voce sola: `Texts.Number` lo
  toglie prima a chi lo aveva (`Free`). Dei salvati sta in `Saved\Numbers` (nome → numero, come `Dates`), dei
  copiati nei bit 3-6 del tipo del blocco (`NumberMask`), solo se fissati: dare un numero a una voce copiata le
  mette la stellina, toglierle la stellina toglie il numero. Rinominare un testo, o salvare una voce copiata, porta
  il numero con sé (`Edit`). Non si esporta.
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
  l'elenco ha già; con la cronologia spenta non si importano.
- Ogni riga ha due linee (voluto dall'utente nella 3.9.3: con le voci a 14 px l'ora a destra si prendeva un pezzo
  importante della riga): sopra segno, testo, numero e stellina (`Line`), sotto, piccola (`Look.CaptionFont`,
  `CaptionShare` della dimensione delle voci) e grigia, l'ora o il giorno (`When`) che finisce all'estremità destra,
  sotto stellina e numero (`Caption`). L'altezza è `EntryHeight`, con un margine stretto (`RowPadding`) perché la
  seconda linea dà già aria. Le righe che non dicono quando (cartelle, salvati senza data) sono alte uguali con
  tutto al centro; le immagini hanno il testo al centro della miniatura e l'ora in basso. Le righe a due livelli
  di prima, con sotto anche caratteri e righe, erano state scartate dall'utente: il numero di caratteri non gli serve.
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
  segni (`Sign`, le icone di Windows dal carattere Segoe Fluent Icons, o Segoe MDL2 Assets su Windows 10, in
  `Look.Glyph`; senza nessuno dei due disegnati a mano su una griglia di 16) dicono cos'è una voce — testo,
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
- Opzione «Tema» in «Programma», sotto la lingua (voluta dall'utente il 16/10/2026, dopo aver cercato invano
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
  Windows al posto dei segni disegnati; caratteri seminegretti di Segoe UI Variable (`StrongFont` per titoli delle
  colonne e cartelle, `TitleFont`, più grande, per il nome di una pagina delle opzioni e i titoli della guida; GDI
  vede il Display Semibold con il nome tagliato a 31 lettere, «Segoe UI Variable Display Semib»; su Windows 10
  Segoe UI Semibold); righe e pulsanti che si illuminano e si spengono a piccoli passi (`Glow` nelle righe, `Fade`
  in `RoundButton` e `Toggle`); l'anteprima che compare in dissolvenza (`Peek.appear`, `Opacity`); le pagine delle
  opzioni che salgono al loro posto (`Entrance`); barre di scorrimento sottili (`Rail`): una linea che si allarga
  sotto il puntatore, trascinabile, sopra la barra di Windows, che resta sotto di lei. Per questo gli elenchi e il
  testo della guida tengono la barra sempre (`ScrollAlwaysVisible`, `ForcedVertical`): le righe non cambiano mai
  larghezza e la rotaia ha sempre dove stare; la linea compare solo se c'è da scorrere. La tacca della riga
  selezionata è corta e arrotondata, come nella colonna di Impostazioni (`MarkShare`).
- Aspetto (`Look`): barra del titolo nel colore della finestra
  (`DwmSetWindowAttribute`, solo Windows 11: altrove fallisce senza danni), pulsanti piatti con un solo
  pulsante in colore d'accento, righe dell'elenco disegnate a mano (`DrawEntry`) con il segno ★/▣ in colonna.
  I controlli che Windows non lascia scurire sono stati sostituiti: le caselle di testo stanno senza bordo
  dentro un pannello (`Look.Frame`), le spunte sono interruttori disegnati (`Toggle`, restano `CheckBox`; voluti
  dall'utente al posto dei quadratini), l'attesa è una casella di testo
  numerica e la tastiera del server un interruttore (prima `NumericUpDown` e `ComboBox`, rimasti chiari).
  Il carattere è `Look.TextFont`, **uguale in tutte le finestre** (voluto dall'utente: pulsanti, opzioni, guida,
  menu, domande; mai un carattere diverso per un singolo controllo, tranne le voci): vedi «Carattere
  dell'interfaccia» sotto.
- Carattere e dimensione dell'interfaccia (voluti dall'utente il 16/10/2026: la guida gli sembrava piccola accanto
  alle voci, e voleva un carattere solo per tutto il programma, che «si distingua bene» da quello delle voci): in
  «Programma», sotto il tema e separati da uno spazio, `faceButton` e `faceSizeButton` con menu come la lingua.
  Caratteri: gli stessi delle voci (`FontNames`); dimensioni `Look.InterfaceSizes` in pixel a 96 dpi (12–16),
  default 13 (`DefaultInterfaceSize`, scelto dall'utente; prima era 12, la dimensione dei messaggi di Windows).
  Le finestre sono disegnate per 12 px (`DesignedSize`) e crescono con il testo: `Look.Zoom` è la scala dello
  schermo per `InterfaceSize / DesignedSize`. Per questo si applicano **al riavvio**, come la lingua
  (`ChangeInterface` → `Restart`): `Main` le legge con `UseSavedInterface` prima di creare la finestra
  (`Look.UseInterfaceFont`), `TransferSettings` le scrive (`face`, `faceSize`). Impostazioni `InterfaceFont` e
  `InterfaceSize` (i vecchi `Font` e `TextSize` del primo tentativo della 3.9.3 non si leggono più).
- Carattere e dimensione delle voci (voluti dall'utente il 16/10/2026: il testo di ciò che si copia più grande,
  e «il classico font select con 11px, 12px…»; prima ho fatto ingrandire tutta l'interfaccia, ma l'utente la
  voleva com'era): in «Programma», sotto il tema, due pulsanti con menu come la lingua (`fontButton`,
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
  Forme arrotondate (l'utente trovava la finestra piatta e asettica, ma i colori vanno bene): ricerca, colonne
  e caselle stanno in un `Card` (bordo sottile, d'accento quando il contenuto ha il focus; `Look.Wrap`; attorno
  a una casella di testo invece, come in Windows 11, una riga d'accento sul fondo, `Card.Underline`), i
  pulsanti sono `RoundButton`, la riga selezionata è una forma arrotondata con una tacca d'accento e quella
  sotto il puntatore si illumina (`Hover`, indice e luce in `RowList.Glow`; `Look.DrawRow`). Le voci copiate mostrano sotto, a destra, l'ora
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
  `OnLoad`). Dimensione e posizione si ricordano (la dimensione dal 16/10/2026, voluta dall'utente: prima tornava
  ogni volta quella di default): impostazioni `Width` e `Height`, in pixel a 96 dpi come `SavedWidth`, applicate in
  `OnLoad` (`savedSize`), mai sotto la minima né oltre lo schermo, sul quale la finestra viene riportata. Le
  immagini dei test (`FillSample`) le ignorano, come la larghezza dei salvati: `shot` mostra la finestra come
  disegnata, `store` e `real` più larga di `Wider` (metà a ciascuna colonna; voluto dall'utente nella 3.9.3,
  perché con le voci a 14 px i nomi dei salvati si tagliavano).
  Allargandola cresce la colonna dei copiati (`Anchor`); le opzioni restano in alto a sinistra.
  Lo spazio tra le due colonne si trascina (`divider`, `PlaceColumns`; voluto dall'utente, a cui i salvati
  stavano stretti): la larghezza dei salvati si salva (`SavedWidth`, in pixel a 96 dpi) e si applica in
  `OnLoad`, quando la finestra ha la sua scala. Nessuna colonna scende sotto `MinColumn`; doppio clic sullo
  spazio torna alla larghezza di partenza.

## Regole di stile volute dall'utente

- Nessun valore duplicato: ogni default, nome o numero sta in un posto solo (costanti in cima a `MainForm`,
  costanti di Windows in `Native` con i nomi ufficiali, scorciatoia definita una volta sola).
- Aggiungere un'opzione: il campo in cima a `MainForm`, una riga `PlaceOption` (`PlaceNumber` se è un numero,
  `PlaceHotkey` se è una scorciatoia globale) in `PlaceOptions` sotto la sua pagina (`Page`), una in
  `TransferSettings` e il suggerimento `Tip`, tutto in `MainForm.Options.cs`. Sulla pagina entra da sola.
  Etichetta e suggerimento sono campi di `Strings` (inglese) tradotti nelle altre tre lingue.
- **Nessun testo visibile scritto nel codice**: ogni testo nuovo è un campo di `Strings.cs` in inglese e una riga
  in ciascuno dei tre file delle lingue, con gli stessi `{0}`. `tests.exe texts` controlla che nessuna traduzione
  chieda più valori dell'inglese (sarebbe un `FormatException` a programma avviato), che le guide abbiano gli
  stessi argomenti, ed elenca i testi rimasti uguali all'inglese. Gli spazi sono tarati sull'italiano, che è
  tra le lingue più lunghe: dopo aver cambiato etichette guardare `tests.exe shot <png> <lingua>` in tutte e
  quattro (francese e spagnolo sono i più a rischio).
  Le opzioni stanno in pagine, una per categoria (voluto dall'utente il 16/10/2026: «fatte come la guida, schede
  verticali, divise per argomento»; prima erano su due colonne, e prima ancora in un elenco unico, caotico): a
  sinistra la colonna delle pagine (`pageList`, disegnata come gli argomenti della guida con `Look.DrawTopic`, con
  l'icona di ciascuna), a destra la pagina scelta (`pagePane`, `ShowPage`), con il nome in grande in cima
  (`PageTop`) e le opzioni in una colonna larga `PaneWidth`. Ordine: Digitazione, Scorciatoie, Cronologia,
  Programma. Una pagina è un `Panel` con `Text` (il nome) e `Tag` (il segno), ed è lei l'elemento della colonna.
  Il colore d'accento resta a ciò che si clicca (il link `PlaceLink` alla cartella della cronologia). L'altezza
  della finestra è fissa (`AreaHeight`, quella di prima: l'utente non la voleva più alta), e le pagine ci stanno
  con spazio avanzato.

## Lingua

- Scelta in «Program» con un pulsante che apre l'elenco delle lingue, ognuna con il proprio nome
  (`PlacePicker`, `ChangeLanguage`); l'etichetta dice anche «Language» in ogni lingua, per chi ci arriva per
  sbaglio. Impostazione `Language` (stringa, `LanguageSetting`), letta da `Main` con `SavedLanguage` prima di
  creare la finestra; senza impostazione vale l'inglese (voluto dall'utente).
- Cambiare lingua **riavvia** il programma invece di riscrivere ogni testo al volo: `ChangeLanguage` lancia
  l'eseguibile con `/restart` (`Program.RestartArgument`) e si chiude salvando le impostazioni; il nuovo aspetta
  fino a dieci secondi che il vecchio liberi il mutex dell'istanza unica, poi si riapre sulle opzioni
  (`ShowOptions`), alla pagina «Programma».
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
- Mentre il gestore di `Application.ThreadException` è in corso, WinForms scarta in silenzio ogni altro errore,
  senza nemmeno chiamare il gestore: per questo `Report` scrive nel log e torna subito, e la finestra si apre dopo
  (`SynchronizationContext.Post`). Provato dal vivo il 17/10/2026 con due errori provocati: prima il secondo
  mancava dal log.
- Avviare il programma vero riscrive le impostazioni dell'utente alla chiusura. I test no: vedi sotto.
- `MainForm.SettingsKey` e `Texts.Folder` vanno cambiati insieme: con le impostazioni di una chiave vuota vale il
  default di 30 giorni, che sulla cartella vera della cronologia cancellerebbe i giorni più vecchi dell'utente.

## Controlli

`tests\test.cmd` compila `tests.exe` (`src\*.cs` + `Test.cs`, ingresso `TestProgram`) ed esegue `map`, `texts`
e `type`; `test.cmd ci` salta `type` (vuole un desktop su cui digitare) e le pause, ed è ciò che GitHub esegue a
ogni push. **I test non toccano mai i dati dell'utente**: `TestProgram.Main` punta `MainForm.SettingsKey` (e
`OldSettingsKey`) su `HKCU\Software\SmartClip test`, `Texts.Folder` e `Log.FileName` su una cartella temporanea,
e cancella tutto prima e dopo. Vale per ogni modo, anche `shot`, `real` e `store`, che quindi partono dalle
impostazioni di default.

- `tests.exe map` — stampa la mappa della tastiera attiva e confronta la tabella americana (solo lettere e
  cifre, se il layout USA non è installato).
- `tests.exe texts` — la logica dell'elenco, senza aprirlo e senza toccare gli appunti: formati riservati,
  testi copiati, cronologia su disco (in una cartella temporanea, non in quella dell'utente), ricerca, andata e ritorno cifrata di un testo salvato (nella chiave di prova), il log.
- `tests.exe type` — apre una finestrella e ci digita davvero: tutti i caratteri ASCII, accentate, simboli con
  AltGr, Unicode, Alt+codice, Tab, Invio; poi verifica che finestra sbagliata e annullamento blocchino tutto.
  Ruba il focus per un paio di secondi: avvisare l'utente prima di lanciarlo.
- `tests.exe shot <prefisso>` — non è in `test.cmd`: salva immagini PNG della finestra (elenco con voci
  finte, opzioni con ogni pagina in `-options-1`…`-4`, stati lunghi) per controllare l'aspetto senza mostrarla.
- `tests.exe store <cartella> <lingua>` — nemmeno: le immagini della scheda dello Store, finestre vere su uno
  sfondo nei colori dell'icona (`Backdrop`) alla misura richiesta (1366 × 768), in `<cartella>\<lingua>` (in chiaro, tranne l'ultima, `8-dark`), e
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
- Il movimento dal vivo: la luce che sfuma sulle righe e sui pulsanti, le pagine delle opzioni che salgono,
  l'anteprima in dissolvenza (con `Opacity` la finestra diventa stratificata: angoli arrotondati da guardare), la
  barra sottile trascinata, cliccata e con la rotella. Viste solo nelle immagini di `shot` e `real`, che le
  colgono ferme.
- I numeri delle voci dal vivo: Ctrl+Maiusc+cifra premuti davvero in un altro programma (anche in un desktop
  remoto), l'avviso dei tasti già usati, il clic su «Scorciatoia». Provati la logica (`texts`), la cattura dei
  tasti (`shot`), il menu aperto da `tests.exe real` e `Typer.Paste` in una finestrella (`type`).
- Il tema dal vivo: il cambio da Impostazioni di Windows a programma aperto (provati solo `Look.Switch` da
  `shot` e le immagini di `real` in chiaro e in scuro), il menu delle caselle di testo scurito da
  `SetPreferredAppMode`, l'interruttore che scorre, l'anello del focus, `AskForm` aperta dal programma vero.
