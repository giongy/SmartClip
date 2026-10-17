# Piano: tutti i dati in un database SQLite

Stato: proposta, 17/10/2026. Niente è ancora stato scritto nel codice.

## Obiettivo

Oggi i dati stanno in tre sistemi con regole proprie: le impostazioni nel registro, i testi salvati in cinque
sottochiavi del registro tenute allineate a mano, i copiati in file binari fatti in casa (uno al giorno). Lo scopo è
mettere testi salvati e copiati **in un unico database SQLite**, con regole che il database fa rispettare da solo, e
dare all'utente la scelta della cartella in cui tenerlo.

Le impostazioni **restano nel registro**: sono una trentina di valori semplici, il registro è il posto standard per
queste cose su Windows, e `TransferSettings` funziona già bene.

## Il motore: `winsqlite3.dll` di Windows

- Windows 10 e 11 contengono SQLite in `C:\Windows\System32\winsqlite3.dll` (sul PC dell'utente: 3.51.1). Si chiama
  con `DllImport` come le altre funzioni di Windows: niente pacchetti, niente DLL da distribuire, `build.cmd` resta
  com'è. Rispetta il vincolo «compilare senza installare nulla».
- Provato il 17/10/2026 dal C# di Windows (processo a 64 bit): apertura, `CHECK` e `UNIQUE` che rifiutano dati
  sbagliati, testo Unicode (accenti, emoji) in andata e ritorno, `instr`, API di backup.
- Convenzione di chiamata `StdCall` (conta solo a 32 bit), testi in UTF-16 con le funzioni `…16`
  (`sqlite3_open16`, `sqlite3_prepare16_v2`, `sqlite3_bind_text16`, `sqlite3_column_text16`), BLOB con
  `SQLITE_TRANSIENT`.
- Lo Store chiede almeno Windows 10 1809, con un SQLite più vecchio: si usano solo funzioni presenti da anni
  (≥ 3.8). Niente `UPSERT`, `RETURNING`, `STRICT`, `VACUUM INTO`, funzioni finestra. All'avvio si legge
  `sqlite3_libversion_number` e sotto il minimo si dice perché il programma non può partire.
- Se la DLL manca (`DllNotFoundException`), stesso trattamento: un messaggio chiaro, non un crash.

## Il modello dei dati

### Le regole (decise con l'utente il 17/10/2026)

| | Voce copiata | Voce salvata |
|---|---|---|
| Scade con i giorni | sì, anche se in alto | **mai** |
| Freccia «in alto» (oggi la stellina) | sì: cambia solo l'ordine | no |
| Cartelle e sottocartelle | no | sì |
| Nome | no | **facoltativo**: se c'è, l'elenco mostra il nome e nasconde il contenuto |
| Scorciatoia 1–9 | sì, finché la voce non scade | sì |
| Testo o immagine | entrambi | entrambi (le immagini oggi non si possono salvare) |
| Cifrata (DPAPI) | no, in chiaro | sì: contenuto e miniatura |

- «Salvato» è una colonna esplicita, non «ha un nome».
- La freccia non protegge dalla pulizia: tiene in cima alla colonna una cosa temporanea.
- Un numero su una voce copiata non la salva: quando la voce scade, il numero si libera.
- Ricopiare un testo già salvato crea anche una voce copiata separata: la cronologia registra ciò che si copia.

### Lo schema

```sql
-- Versione dello schema in PRAGMA user_version (1 per questa).

CREATE TABLE folders (
    id        INTEGER PRIMARY KEY,
    parent_id INTEGER NOT NULL DEFAULT 0,   -- 0: al primo livello (nessuna chiave esterna su 0)
    name      TEXT NOT NULL,
    name_key  TEXT NOT NULL,                -- name in maiuscolo invariante: unicità senza maiuscole
    open      INTEGER NOT NULL DEFAULT 1,
    UNIQUE (parent_id, name_key)
);

CREATE TABLE entries (
    id        INTEGER PRIMARY KEY,
    kind      INTEGER NOT NULL CHECK (kind IN (1, 2)),   -- 1 testo, 2 immagine
    content   BLOB NOT NULL,     -- testo UTF-8 o PNG; cifrato con DPAPI se salvata
    preview   BLOB,              -- miniatura PNG delle immagini; cifrata se salvata
    width     INTEGER,
    height    INTEGER,
    folded    TEXT,              -- testo per la ricerca (Texts.Fold) dei soli copiati: NULL se salvata
    hash      BLOB,              -- impronta del contenuto per riconoscere le copie doppie: NULL se salvata
    copied_at INTEGER NOT NULL,  -- UTC, millisecondi
    saved     INTEGER NOT NULL DEFAULT 0 CHECK (saved IN (0, 1)),
    saved_at  INTEGER,
    name      TEXT,
    name_key  TEXT UNIQUE,       -- più voci senza nome vanno bene: UNIQUE ignora i NULL
    folder_id INTEGER REFERENCES folders (id),
    pinned    INTEGER NOT NULL DEFAULT 0,
    number    INTEGER UNIQUE CHECK (number BETWEEN 1 AND 9),
    CHECK (saved = 1 OR (name IS NULL AND folder_id IS NULL AND saved_at IS NULL)),
    CHECK (saved = 0 OR pinned = 0),
    CHECK (saved = 0 OR (folded IS NULL AND hash IS NULL)),
    CHECK ((kind = 2) = (preview IS NOT NULL))
);

CREATE INDEX entries_recent ON entries (saved, pinned DESC, copied_at DESC);
CREATE INDEX entries_hash   ON entries (hash) WHERE hash IS NOT NULL;
```

Note sulle scelte:

- **Cartelle ad albero** (`parent_id`): rinominare o spostare una cartella cambia una riga sola. Oggi una cartella
  è il suo percorso (`Lavoro\Server`), scritto in ogni testo, e `RenameGroup`/`Rebase` riscrivono tutto.
- **`name_key`**: `COLLATE NOCASE` di SQLite ignora le maiuscole solo per l'alfabeto inglese, quindi «Città» e
  «CITTÀ» risulterebbero diversi. La chiave la calcola il C# con `ToUpperInvariant`, come il registro oggi.
- **Niente ricerca né impronta sul contenuto cifrato**: un'impronta dei salvati permetterebbe di verificare se
  una password è uguale a un'altra. I salvati si cercano in memoria, dopo averli decifrati una volta.
- **Ore in UTC**: niente sorprese al cambio dell'ora legale. Il giorno di scadenza e l'ora mostrata si calcolano
  nell'ora locale.
- Ogni connessione imposta `PRAGMA foreign_keys = ON`, `secure_delete = ON` (una voce cancellata viene
  sovrascritta, come oggi `Erase` la azzera) e `synchronous = FULL`.
- Journal di tipo rollback (quello di default), **non WAL**: WAL non funziona sulle cartelle di rete, e l'utente
  potrà sceglierne una.

## La cartella del database

### Default

| Da dove gira | Cartella | File |
|---|---|---|
| Eseguibile normale | `%LOCALAPPDATA%\SmartClip` | `SmartClip.db` |
| Pacchetto dello Store | `%LOCALAPPDATA%\Packages\<famiglia>\LocalState` (`Packaged.DataFolder`) | `SmartClip.db` |

La cartella è locale, non *roaming*: i salvati cifrati con DPAPI si leggono solo con l'utente e sul PC che li
ha cifrati.

### Scelta dell'utente

- **Impostazione** `DataFolder` (stringa nel registro, vuota = default). Sta nel registro e non nel database perché
  serve per trovare il database. Si legge prima di creare la finestra, come la lingua.
- **Nelle opzioni**, pagina «Cronologia», che mantiene il suo nome (deciso con l'utente), in quest'ordine:
  «Cartella delle voci copiate e salvate» con il percorso (accorciato, intero nel suggerimento), pulsanti
  **«Cambia…»** e **«Predefinita»**, link «Apri la cartella» (quello che oggi apre `History`), poi cronologia
  sì/no, giorni, «Pulisci ora…». Da controllare che tutto stia in `AreaHeight` in tutte e quattro le lingue.
- **Cambiare cartella** (`FolderBrowserDialog`):
  - la cartella nuova non contiene `SmartClip.db`: il programma chiude il database, lo copia lì, lo riapre dalla
    copia, controlla che sia integro (`PRAGMA quick_check`), salva l'impostazione e solo alla fine cancella il
    vecchio file. Se qualcosa fallisce si resta sul vecchio, che è intatto;
  - la cartella contiene già un `SmartClip.db` (per esempio quello di un'installazione precedente) e si chiede:
    «Usa quello» (il proprio resta dov'è, non viene cancellato), «Sostituiscilo con il mio» (quello che c'era
    diventa `SmartClip.old.db`), «Annulla»;
  - «Predefinita» segue la stessa strada verso la cartella di default.
- **Avvertenze** nel suggerimento e alla scelta di una cartella di OneDrive, Dropbox o di rete (percorso `\\…` o
  dentro `%OneDrive%`). La scelta è permessa, ma:
  - non è un modo per condividere i dati tra PC: i salvati, cifrati per questo PC, altrove non si leggono, e due
    programmi aperti sullo stesso file da due PC possono rovinarlo;
  - i programmi di sincronizzazione possono bloccare il file mentre lo copiano.
- **Cartella irraggiungibile all'avvio** (chiavetta tolta, rete assente): il programma **non** crea in silenzio
  un database vuoto altrove, perché i dati finirebbero divisi in due. Parte senza dati, lo dice nella riga di stato
  (in modo permanente, finché non si risolve) e nella colonna vuota, e nelle opzioni si può scegliere un'altra
  cartella o tornare alla predefinita.

## La migrazione

Al primo avvio della versione nuova, se il database non esiste ancora:

1. Crea il database e lo schema.
2. In **un'unica transazione**:
   - cartelle da `Saved\Folders`: i percorsi diventano l'albero, con lo stato aperta o chiusa;
   - testi salvati da `Saved`, con data (`Dates`), numero (`Numbers`) e cartella (`Places`). Il contenuto è già
     cifrato con DPAPI e **si copia così com'è, senza decifrarlo**. Così si migrano anche quelli che qui non si
     possono leggere (cifrati da un altro utente), che oggi vengono saltati in silenzio;
   - copiati dai file `.clips` (saltando i record eliminati), con miniatura e immagine. La stellina diventa la
     freccia (decisione dell'utente: le voci scadranno con i loro giorni) e il numero resta.
3. Alla fine scrive la versione dello schema. Se qualcosa fallisce la transazione si annulla, il database
   incompleto si cancella e al prossimo avvio si riprova. Il codice che legge il formato vecchio non c'è più,
   quindi intanto il programma parte senza dati e lo dice nella riga di stato. È il motivo per cui la migrazione
   va provata a fondo.
4. **I dati vecchi non si toccano** (registro `Saved`, cartella `History`): restano per una o due versioni, così
   un ritorno alla versione precedente li ritrova, senza però le modifiche fatte nel frattempo. Una versione
   successiva li cancellerà.

La migrazione si prova su dati finti: una chiave di registro e una cartella di prova, mai quelle dell'utente.

## Il codice

### File nuovi

- `src\Sqlite.cs`: lo strato su `winsqlite3.dll`, circa 200 righe in C# 5. `Database` (apre, esegue, transazioni
  con `using`, chiude) e `Statement` (prepara, `Bind` per testo, numero, BLOB e NULL, `Step`, legge le colonne,
  `Dispose` che finalizza). Gli errori diventano eccezioni con il messaggio di SQLite. Le dichiarazioni
  `DllImport` stanno qui e non in `Native.cs`, che è per le funzioni di Windows.
- `src\Clips.cs`: i dati del programma sopra il database, **non statici**. Si crea dal percorso del file, quindi
  i test possono usarne uno temporaneo. Fa: carica i salvati (decifrati una volta, tenuti in memoria finché non
  cambiano); carica i copiati da mostrare (in alto prima, poi i più recenti, al massimo `MaxShown`, filtrati dalla
  ricerca); aggiunge una copia (con le doppie riconosciute dall'impronta); salva, rinomina, sposta, cancella;
  freccia; numero; cartelle; scadenza; pulizia.
- `src\Migration.cs`: crea e aggiorna lo schema (`user_version`) e fa l'importazione una tantum dal vecchio
  formato.

### File che cambiano

- `Texts.cs`: perde tutto ciò che riguarda registro e file. Restano la ricerca (`Fold`, `Matches`, `Words`), il
  controllo dei formati riservati (`IsPrivate`), la cattura dagli appunti (`Remember`), che passa la copia a
  `Clips`. Gli aiuti sui percorsi diventano aiuti sull'albero (il percorso da mostrare, «Lavoro › Server», si
  ricava salendo i genitori).
- `Entry`: prende `Id` (è lui a identificare una voce, non più nome e testo), `Saved` e `FolderId` al posto di
  `Group`, oltre a `Pinned` e `Number`. Al posto di `Name == null` c'è `Saved`. `IsGroup` resta solo per le righe
  delle cartelle nell'elenco.
- `MainForm.List.cs`: tutto ciò che oggi passa il nome (`Texts.Move(name…)`, `Number(name…)`, la selezione
  ritrovata confrontando nome e testo) passa l'`Id`. `Edit` smette di comporre da sola salva, cancella, sposta il
  numero: chiede a `Clips` una sola operazione.
- `MainForm.cs`: a ogni copia aggiunge la voce e ricarica **solo la colonna dei copiati**, senza rileggere e
  decifrare i salvati come fa oggi.
- `Backup.cs`: salvati senza nome (il campo `Name:` diventa facoltativo), freccia (il vecchio campo «in alto» si
  legge come freccia), immagini sempre escluse come oggi. I file scritti dalle versioni precedenti si leggono
  ancora.

### Cronologia spenta e «0 giorni»

Deciso con l'utente il 17/10/2026: **0 giorni vuol dire «solo le cose di oggi»**. Tutto va nel database, e a
mezzanotte quelle di ieri scadono. È già la regola di oggi per gli altri valori (si tiene oggi più N giorni prima),
quindi la formula della scadenza non cambia.

La modalità «solo in memoria» di oggi sparisce: con 0 giorni non si scriveva niente su disco e si tenevano in memoria
30 voci e 5 immagini (`MaxRecent`, `MaxPictures`, `Limit`). Così resta una sola strada nel codice. Chi non vuole
niente su disco spegne la cronologia: i copiati vengono cancellati e non se ne registrano altri, come oggi. Il
suggerimento dei giorni (`KeepTip`) va riscritto nelle quattro lingue.

## L'interfaccia

- **Freccia al posto della stellina** nei copiati: icona «Up» di Segoe Fluent, voce di menu «Tieni in alto» /
  «Togli dall'alto», Ctrl+P, stesso posto nella riga. Il suggerimento spiega che la voce scadrà comunque.
- **Salvare**: solo con la finestra (deciso con l'utente: niente «Salva subito»). Ctrl+S e «Salva…» aprono
  `EditForm` come oggi, ma il nome è facoltativo: «Salva» si attiva anche senza nome, purché ci sia il contenuto.
  Per un'immagine `EditForm` mostra nome e cartella, senza la casella del contenuto.
- **La finestra dice cosa fa il nome**: una riga grigia sotto la casella del nome, che cambia mentre si scrive.
  - Vuota: «Senza nome: nell'elenco si vedrà il contenuto.»
  - Con un nome: «Nell'elenco si vedrà “Banca”; il contenuto resta nascosto, adatto alle password.»

  L'etichetta della casella diventa «Nome (facoltativo)». Lo stesso concetto va nel suggerimento del pulsante
  (`SaveTip`) e nella guida. Va controllato che la riga stia su una o due linee in tutte e quattro le lingue.
- **Riga di un salvato**: con il nome si vede il nome (contenuto nascosto, come oggi); senza nome il contenuto,
  come un copiato, con il segno dei salvati. Un'immagine salvata senza nome mostra la miniatura, con il nome no.
- **Anteprima al passaggio del mouse**: compare anche sui salvati senza nome, il cui contenuto è già in vista. Su
  quelli con il nome solo da «Mostra il contenuto», come oggi.
- **Menu del tray**: i salvati senza nome compaiono con l'inizio del contenuto.
- **Testi nuovi o cambiati** in `Strings.cs` e nelle tre lingue; guida (`EnglishGuide` e le altre tre) e README
  aggiornati, come chiede il progetto.

## Ordine dei lavori

Ogni passo si chiude con i test che passano e lascia il programma funzionante.

0. **Preparazione** — fatto il 17/10/2026:
   - `test.cmd ci` (tutto tranne la digitazione) nel flusso di release, a ogni push;
   - gestore globale degli errori (`Program.Report`) e `Log.cs`, con `SmartClip.log` nella cartella dei dati
     (`Program.DataFolder`, dove andrà anche il database), così gli errori della migrazione si potranno leggere;
   - test isolati: chiave di registro e cartella temporanea loro, mai i dati dell'utente.
1. **`Sqlite.cs` e i suoi test** (`tests.exe data`) — fatto il 17/10/2026, provato anche in un processo a 32
   bit. Nessun cambiamento visibile.
2. **Schema e `Clips.cs` con i test** (regole del database, scadenza, freccia, numeri che si liberano, albero delle
   cartelle). Non ancora collegati alla finestra. — Fatto il 17/10/2026. Scelte prese scrivendolo:
   - **Copiare di nuovo** un testo (o un'immagine) già tra i copiati aggiorna la stessa riga con l'ora nuova: torna
     in cima e tiene freccia e numero, invece di cancellare la vecchia e crearne una nuova (che perderebbe il
     numero).
   - **«Pulisci ora…» toglie anche le voci in alto**, come la scadenza: in alto vuol dire temporanea. Oggi invece
     le fissate restano. Da confermare con l'utente.
   - **Eliminare una cartella** porta su di un livello ciò che contiene; se lassù c'è già una cartella con lo stesso
     nome, la riempie (oggi, con i percorsi, le due si fondevano da sole).
   - Dopo una pulizia il file si restringe a poco a poco (`auto_vacuum = INCREMENTAL`), senza ricostruirlo tutto.
   - «Dimentica» (la password digitata e tolta dagli appunti) non tocca le voci in alto né quelle con un numero.
3. **`Migration.cs` con i test** su registro e cartella di prova.
4. **Il cambio**: la finestra usa `Clips`, la migrazione parte al primo avvio, il codice vecchio di registro e
   `.clips` sparisce. L'interfaccia resta quella di oggi, salvo ciò che la migrazione impone (la stellina diventa
   freccia e scade). Release a sé, da provare dal vivo prima di andare avanti.
5. **Le funzioni nuove**: salvati senza nome, immagini salvabili, righe e anteprima dei salvati senza nome,
   esportazione aggiornata.
6. **La cartella del database**: impostazione, pagina «Dati», spostamento, avvertenze, cartella irraggiungibile.
7. **Documentazione**: guida e README nelle quattro lingue, `PRIVACY.md` (dove stanno i dati), scheda dello Store
   se cita la cronologia, `CLAUDE.md` (sezioni su cronologia, salvati, cartelle, stellina, numeri, trappole).
8. **Una o due versioni dopo**: cancellazione dei dati vecchi (registro `Saved`, cartella `History`) e delle
   impostazioni rimaste orfane (`Font`, `TextSize`).

## Test da aggiungere

- `Sqlite`: apertura di un file temporaneo, testo Unicode, BLOB grandi, NULL, transazione annullata da
  un'eccezione, errore SQL che diventa eccezione.
- Schema: il database rifiuta nome o cartella su un copiato, freccia su un salvato, nome doppio (anche con
  maiuscole diverse), numero doppio o fuori da 1–9.
- `Clips`: la scadenza cancella i copiati vecchi anche se in alto, mai i salvati; con 0 giorni restano solo quelli
  di oggi; un numero su un copiato scaduto si
  libera; una copia doppia sale in cima invece di raddoppiare; rinomina, spostamento ed eliminazione di una
  cartella (il contenuto sale di un livello); salvare una voce copiata.
- Migrazione: dati finti con cartelle annidate, testi con e senza data, numero e cartella, un testo che non si
  decifra, file `.clips` con record eliminati e troncati, immagini, stelline e numeri. Poi il confronto voce per
  voce.
- Cartella del database: spostamento in una cartella vuota, cartella con un database già presente (le due
  scelte), cartella irraggiungibile.
- Esportazione e importazione: salvati senza nome, file delle versioni precedenti.

## Da decidere con l'utente

Niente, per ora: le scelte aperte sono state decise il 17/10/2026 e sono scritte nelle sezioni sopra.
