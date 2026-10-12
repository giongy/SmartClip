# SmartClip

Piccolo gestore degli appunti per Windows che sa anche digitarne il contenuto come pressioni di tasti reali, per
inserire password dove l'incolla non funziona (schermate di blocco dei desktop remoti). Fino alla versione 1.2
si chiamava PasswordPaste: la cartella locale ha ancora quel nome (il repository GitHub è `giongy/SmartClip`), e alla
prima apertura `AdoptOldSettings` sposta impostazioni e testi salvati da `HKCU\Software\PasswordPaste`. L'utente è italiano: interfaccia e
risposte in italiano. I commenti nel codice e nei file di configurazione sono in inglese (il progetto è condiviso
su GitHub).

## File

- `SmartClip.cs` — tutto il programma, in un solo file: `Program`, `MainForm` (finestra con elenco e
  opzioni, tray, impostazioni), `Entry` e `Texts` (le voci dell'elenco), `Typer` (mappa dei tasti e invio),
  `Native` (funzioni e costanti di Windows).
- `build.cmd` — compila `SmartClip.exe`. `SmartClip.ico` è facoltativa.
- `tests/Test.cs`, `tests/test.cmd` — controlli automatici (vedi sotto).
- `LICENSE` — licenza MIT.
- `README.md` — presentazione per GitHub, in inglese. Traduce le etichette delle opzioni: va aggiornato quando se
  ne aggiunge o rinomina una. Mostra `docs/screenshot.png`, `docs/screenshot-options.png` e `docs/screenshot-edit.png`, fatti con
  `tests.exe real` (immagini `-real-list`, `-real-options` e `-real-edit`, con voci finte e a programma
  chiuso: altrimenti le scorciatoie risultano occupate)
  compilato aggiungendo `/resource:SmartClip.ico,SmartClip.ico`, altrimenti manca l'icona.

- `.github/workflows/codeql.yml` — analisi CodeQL su GitHub con compilazione vera (`build-mode: manual`, lancia
  `build.cmd < NUL` su Windows). Richiede che la configurazione predefinita di Code scanning sia disattivata.
  `.github/dependabot.yml` aggiorna solo le azioni di quel flusso: il programma non ha pacchetti.

## Vincoli di compilazione

- Si usa il `csc.exe` incluso in Windows (.NET Framework 4, **C# 5**): niente interpolazione di stringhe, `?.`,
  `out var`, membri a espressione, `readonly struct`, costruttori primari. I suggerimenti di VS Code che li
  propongono vanno ignorati. Lo scopo è poter ricompilare sul PC del lavoro senza installare nulla.
- Il sorgente è UTF-8 senza BOM con lettere accentate: serve `/codepage:65001` (già in `build.cmd`).
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

- Icona (tray e finestra) a colori se negli appunti c'è testo, grigia se non c'è: `ShowClipboardState`, avvisato
  da `WM_CLIPBOARDUPDATE`. Guarda solo se il formato testo è disponibile, senza aprire gli appunti. La versione
  grigia si genera all'avvio dall'icona normale (`Gray`).

- La finestra principale è l'elenco (voluto dall'utente), su due colonne affiancate (anche questo voluto: i
  salvati non devono stare in cima ai copiati): a sinistra `recentList` con le ultime 30 cose copiate, a
  destra `savedList` con i testi salvati. La voce selezionata è una sola in tutta la finestra (`active`,
  `Selected`); Tab passa da una colonna all'altra. Tutte e due si filtrano mentre si scrive (`Texts.Matches`: parole in qualsiasi ordine, senza maiuscole né accenti). Il
  pulsante «Opzioni»/«Elenco» alterna nella stessa area le due viste (`ShowView`, `listControls` e
  `optionControls`). Le idee vengono dal progetto vicino `..\pastepad` (WinUI 3, .NET 10), riscritte qui in
  piccolo: niente modelli, formato, temi.
- Scegliere una voce (Invio o clic, `Use`) la rimette soltanto negli appunti: poi l'utente incolla con Ctrl+V o
  digita con Ctrl+Alt+V. Non si digita né si incolla da soli (provato e scartato dall'utente). Un testo salvato
  torna negli appunti con il formato che lo segna riservato; ciò che il programma stesso mette negli appunti
  non rientra nell'elenco (`ownCopy`, numero di sequenza degli appunti).
- Ctrl+D (`OpenList`) porta la finestra in primo piano da qualsiasi programma; dopo la scelta `GoBack`
  restituisce il primo piano alla finestra di partenza e rinasconde la finestra se era nascosta.
- I testi copiati stanno solo in memoria (`Texts.Recent`), per scelta dell'utente: mai su disco. Si scartano
  quelli che i gestori di password segnano come riservati (`Texts.IsPrivate`). L'opzione spenta svuota l'elenco.
- Anche le immagini copiate entrano in `Texts.Recent`, al massimo 5, compresse in PNG (`Entry.Png`); il loro
  `Text` è la descrizione che si vede e si cerca. Se negli appunti c'è anche testo vince il testo. Non si
  salvano, per scelta dell'utente.
  La stessa copia annunciata più volte da Windows si riconosce confrontando i byte del PNG.
  Nell'elenco occupano tre righe, con una miniatura (`Entry.Preview`, creata una volta alla copia).
- I testi salvati stanno in `HKCU\Software\SmartClip\Saved`, un valore binario per nome, cifrato con DPAPI
  per l'utente (`ProtectedData`, in `System.Security.dll` che `csc` riferisce già da solo). L'elenco ne mostra
  solo il nome.
- I testi salvati si scrivono in `EditForm` (nome e contenuto), aperta da `Edit`: testo nuovo, voce copiata
  da tenere, voce salvata da cambiare. Le stesse tre azioni stanno nei pulsanti sotto l'elenco, nel menu del
  tasto destro e in Ctrl+N/S/E. Il nome scritto nella casella di ricerca è stato provato e scartato dall'utente
  (poco chiaro). Il clic sinistro sceglie la voce, il destro la seleziona soltanto.

- Aspetto (`Look`, voluto dall'utente: scuro con accenti pastello): barra del titolo nel colore della finestra
  (`DwmSetWindowAttribute`, solo Windows 11: altrove fallisce senza danni), pulsanti piatti con un solo
  pulsante in colore d'accento, righe dell'elenco disegnate a mano (`DrawEntry`) con il segno ★/▣ in colonna.
  I controlli che Windows non lascia scurire sono stati sostituiti: le caselle di testo stanno senza bordo
  dentro un pannello (`Look.Frame`), le spunte sono disegnate (`Toggle`), l'attesa è una casella di testo
  numerica e la tastiera del server una spunta (prima `NumericUpDown` e `ComboBox`, rimasti chiari).
  Barra del titolo e barre di scorrimento scure non compaiono nelle immagini di `tests.exe shot`: per
  vederle c'è `tests.exe real <prefisso>`, che mostra le finestre un attimo e fotografa lo schermo.
  La descrizione sotto il pulsante deve stare in due righe: è il motivo della larghezza (`ContentWidth`).

- Disposizione (voluta dall'utente: «Digita gli appunti» in cima era troppo ingombrante): in alto la ricerca,
  poi le colonne (o le opzioni), sotto la riga di stato e un'unica fila di pulsanti piccoli: a sinistra quelli
  della voce selezionata, a destra «Digita gli appunti» e «Opzioni»/«Elenco».

## Regole di stile volute dall'utente

- Nessun valore duplicato: ogni default, nome o numero sta in un posto solo (costanti in cima a `MainForm`,
  costanti di Windows in `Native` con i nomi ufficiali, scorciatoia definita una volta sola).
- Aggiungere un'opzione: una riga `PlaceOption`, una in `TransferSettings`, il suggerimento `Tip` e la casella
  in `optionControls`. L'altezza della finestra si calcola da sola.

## Trappole già incontrate

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
- La scorciatoia dell'elenco è Ctrl+D, scelta dall'utente al posto di Ditto: da globale toglie Ctrl+D agli
  altri programmi, ed è scritto nel suggerimento. Per questo nell'elenco «elimina» è Ctrl+E.
- Nell'elenco Invio agisce al rilascio (`OnKeyUp`, con `enterDown`): alla pressione il tasto tenuto giù
  continuerebbe a ripetersi nella finestra a cui torna il primo piano. Il primo piano si restituisce prima di
  nascondere la finestra, finché il programma ha ancora il diritto di assegnarlo.
- `Control.Visible` risponde falso se la finestra è nascosta: quale vista è attiva lo dice `optionsShown`.
- Avviare il programma vero o creare `MainForm` nei test riscrive le impostazioni dell'utente alla chiusura:
  salvarle prima e ripristinarle dopo.

## Controlli

`tests\test.cmd` compila `tests.exe` (sorgente del programma + `Test.cs`, ingresso `TestProgram`) ed esegue:

- `tests.exe map` — stampa la mappa della tastiera attiva e confronta la tabella americana (solo lettere e
  cifre, se il layout USA non è installato).
- `tests.exe texts` — la logica dell'elenco, senza aprirlo e senza toccare gli appunti: formati riservati,
  testi copiati, ricerca, andata e ritorno cifrata di un testo salvato (scrive e toglie un valore `~test …`).
- `tests.exe type` — apre una finestrella e ci digita davvero: tutti i caratteri ASCII, accentate, simboli con
  AltGr, Unicode, Alt+codice, Tab, Invio; poi verifica che finestra sbagliata e annullamento blocchino tutto.
  Ruba il focus per un paio di secondi: avvisare l'utente prima di lanciarlo.
- `tests.exe shot <prefisso>` — non è in `test.cmd`: salva immagini PNG della finestra (elenco con voci
  finte, opzioni, stati lunghi) per controllare l'aspetto senza mostrarla.

Il tray è stato verificato con un programmino usa e getta (non nel progetto) che crea `MainForm`, legge i campi
`tray` e `trayBox` per riflessione e aziona le voci del menu con `PerformClick`: X nasconde, Apri riapre anche
da ridotta a icona, Esci chiude, opzione spenta toglie l'icona e la X torna a chiudere.

## Mai verificato

- Una sessione remota vera (RDP o altro), compreso se il client inoltra i caratteri Unicode.
- I simboli della tabella americana (controllati a mano), i tasti morti, schermi oltre il 100% di scala.
- Il clic reale sull'icona del tray e il fumetto di avviso.
- Il passaggio dell'icona a grigio con gli appunti vuoti (visto solo lo stato "testo presente" e le due immagini).
- L'elenco dal vivo: clic e Invio che rimettono la voce negli appunti, Ctrl+S, Ctrl+D, apertura con la
  scorciatoia e ritorno del primo piano alla finestra di partenza. Controllate solo la logica (`texts`) e
  l'immagine.
- Che il formato «riservato» messo sui testi salvati tenga davvero fuori la cronologia di Windows (Win+V).
- I formati riservati sugli appunti veri (provati solo su oggetti in memoria) e la cattura a ogni copia.
- Le immagini dal vivo: cattura di una schermata vera e ritorno negli appunti.

## Idee lasciate aperte

- Avvio direttamente nascosto nel tray (oggi la finestra si mostra sempre all'avvio).
