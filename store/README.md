# Pubblicare SmartClip sul Microsoft Store

Tutto quello che serve per la scheda dello Store, pronto da copiare in Partner Center, e i passi per arrivarci.
Le immagini sono in [`images`](images): si rifanno con `tests\tests.exe store store\images`, a programma chiuso.

## Passi

1. **Iscrizione** su [storedeveloper.microsoft.com](https://storedeveloper.microsoft.com) come sviluppatore
   individuale (gratuita: documento d'identità e selfie).
2. **Prenota il nome** in Partner Center: *Apps and games → New product → MSIX or PWA app* → «SmartClip» (se
   è preso, un'alternativa come «SmartClip Appunti»: va cambiato anche `DisplayName` nel manifesto).
3. **Identità del pacchetto**: in *Product management → Product identity* ci sono tre valori. Vanno in
   [`packaging/AppxManifest.xml`](../packaging/AppxManifest.xml) al posto dei segnaposto:
   - `Package/Identity/Name` ← il valore di **Package/Identity/Name**
   - `Package/Identity/Publisher` ← **Package/Identity/Publisher** (inizia con `CN=`)
   - `Package/Properties/PublisherDisplayName` ← **Package/Properties/PublisherDisplayName**
4. **Pacchetto**: fai push (o avvia a mano il flusso *Release* da GitHub → Actions → *Run workflow*) e scarica
   l'artifact **SmartClip-msix** dall'esecuzione: dentro c'è `SmartClip-<versione>.msix`. Ogni invio allo Store
   vuole una versione più alta della precedente, con la quarta cifra a zero (`3.8.2` va bene).
5. **Prova prima di inviare** (facoltativo): con *Impostazioni → Sistema → Per sviluppatori → Modalità
   sviluppatore* attiva, dopo `build.cmd` e `powershell -ExecutionPolicy Bypass -File packaging\pack.ps1`:
   `Add-AppxPackage -Register packaging\out\layout\AppxManifest.xml`. Il programma compare nel menu Start come
   se venisse dallo Store; si toglie da *Impostazioni → App*. Da provare: avvio con Windows (la spunta nelle
   opzioni), «Apri la cartella della cronologia», ritorno dopo un riavvio.
6. **Nuovo invio** in Partner Center, sezione per sezione come qui sotto, poi *Submit to the Store*. La verifica
   di Microsoft richiede di solito da qualche ora a qualche giorno.

## Prezzi e disponibilità

- **Prezzo**: gratuito.
- **Mercati**: tutti, oppure solo Italia, Svizzera e San Marino. L'interfaccia è solo in italiano, e la
  descrizione lo dice; la scelta è tua.
- **Visibilità**: pubblica.

## Proprietà

- **Categoria**: Utilità e strumenti (*Utilities & tools*).
- **Informativa sulla privacy**: `https://github.com/giongy/SmartClip/blob/main/PRIVACY.md`
- **Sito web**: `https://github.com/giongy/SmartClip`
- **Contatto per il supporto**: `https://github.com/giongy/SmartClip/issues`
- **Il prodotto accede, raccoglie o trasmette informazioni personali?** Sì: legge gli appunti (resta tutto sul
  PC, come dice l'informativa).
- **Requisiti di sistema**: tastiera (minima e consigliata). Windows 10 versione 1809 o successiva, già
  indicata dal pacchetto.
- Nessuna delle altre dichiarazioni (acquisti, accesso con account, contenuti per adulti) si applica.

## Classificazione per età

Questionario IARC: categoria **Utilità** (non un gioco, non social, non un browser). Rispondi **No** a tutto:
niente violenza, sesso, linguaggio, droghe, gioco d'azzardo; nessuna comunicazione tra utenti, nessun contenuto
condiviso, nessuna posizione, nessun acquisto, nessun accesso libero a Internet. Il risultato atteso è PEGI 3 /
ESRB Everyone.

## Scheda in italiano

### Nome del prodotto

```
SmartClip
```

### Descrizione breve

```
Tutto quello che copi, sempre a portata di mano. E dove incollare non funziona, come nelle schermate di accesso dei desktop remoti, SmartClip digita gli appunti tasto per tasto al posto tuo.
```

### Descrizione

```
SmartClip è un gestore degli appunti leggero per Windows, con un talento in più: sa digitare il testo degli appunti come se lo scrivessi tu sulla tastiera.

Copia una password, clicca nel campo della finestra remota e lascia fare a SmartClip. Funziona anche dove Ctrl+V è bloccato: nelle schermate di blocco dei desktop remoti, nelle console delle macchine virtuali, nelle pagine di gestione dei server. Hai il tempo di cliccare nel campo giusto, e la digitazione si ferma da sola se cambi finestra: una password non finisce mai nel posto sbagliato.

Tutto quello che copi, testi e immagini, resta in un elenco che si apre da qualsiasi programma con una scorciatoia. Scrivi qualche parola per trovarlo, premi Invio e torni dov'eri con la voce già negli appunti, pronta da incollare o da digitare.

I testi che usi spesso, come una firma, un indirizzo o una password, li salvi con un nome e li organizzi in cartelle e sottocartelle. Li trovi anche nel menu dell'icona vicino all'orologio, da prendere senza nemmeno aprire la finestra.

Pensato per la riservatezza:
• i testi salvati sono cifrati per il tuo utente di Windows;
• ciò che i gestori di password segnano come riservato non viene ricordato;
• puoi sospendere la cattura, scegliere per quanti giorni tenere la cronologia o tenerla solo in memoria;
• nessun account, nessuna pubblicità, nessuna connessione a Internet: i tuoi dati restano sul tuo PC.

SmartClip è open source, con licenza MIT. L'interfaccia è in italiano.
```

### Caratteristiche del prodotto

Una per riga, senza punti elenco (li aggiunge lo Store):

```
Digita gli appunti tasto per tasto dove incollare non funziona
Pensato per desktop remoti, console di macchine virtuali e pagine di gestione dei server
Attesa regolabile prima di digitare, e la digitazione si ferma se cambi finestra
Tastiera americana sul server e simboli composti con Alt+codice
Cronologia di testi e immagini copiati, con ricerca istantanea
Elenco richiamabile da qualsiasi programma con una scorciatoia
Voci importanti fissate in cima con una stellina
Testi salvati cifrati, organizzati in cartelle e sottocartelle
Testi salvati anche nel menu dell'icona, senza aprire la finestra
Anteprima completa di testi lunghi e immagini
Scorciatoie da tastiera personalizzabili
Le copie riservate dei gestori di password non vengono ricordate
Cattura sospendibile con un clic
Esportazione e importazione in un file di testo
Avvio con Windows, nascosto nell'area di notifica
Guida integrata, divisa per argomenti
Nessun account, nessuna pubblicità, nessuna connessione a Internet
```

### Termini di ricerca

Sette al massimo:

```
appunti
clipboard
password
desktop remoto
cronologia appunti
gestore appunti
digitare password
```

### Novità di questa versione

Vuoto al primo invio. Dagli aggiornamenti in poi, le novità della release.

### Schermate e didascalie

Nell'ordine, da [`images`](images) (1366 × 768):

| File | Didascalia |
| --- | --- |
| `1-elenco.png` | Le cose copiate di recente a sinistra, i testi salvati a destra: cerca, scegli e incolla. |
| `2-digitazione.png` | Dove Ctrl+V non funziona, SmartClip digita gli appunti tasto per tasto, dopo averti dato il tempo di cliccare nel campo giusto. |
| `3-anteprima.png` | Fermati su una voce per vederla per intero. |
| `4-salva.png` | Salva un testo con un nome e mettilo nella sua cartella. |
| `5-menu-tray.png` | I testi salvati anche nel menu dell'icona vicino all'orologio. (Facoltativa: il menu è piccolo sull'immagine intera.) |
| `6-opzioni.png` | Opzioni per la digitazione, le scorciatoie, la cronologia e l'avvio con Windows. |
| `7-guida.png` | Una guida divisa per argomenti, sempre a portata di F1. |

### Logo dello Store

*Icona del riquadro dell'app 1:1 (300 × 300)*: [`images/tile-300.png`](images/tile-300.png).

### Informazioni aggiuntive

- **Copyright e marchi**: `© 2026 Luca Andreoli`
- **Termini di licenza aggiuntivi**: `Software open source con licenza MIT: https://github.com/giongy/SmartClip/blob/main/LICENSE`
- **Sviluppato da**: `Luca Andreoli`

## Scheda in inglese (facoltativa)

Per i mercati fuori dall'Italia. Partner Center la accetta anche se il pacchetto è solo in italiano; la
descrizione lo dice.

### Short description

```
Everything you copy, always at hand. And where paste does not work, such as remote desktop sign-in screens, SmartClip types the clipboard for you, key by key.
```

### Description

```
SmartClip is a lightweight clipboard manager for Windows with one extra talent: it can type the clipboard text as if you were typing it on the keyboard.

Copy a password, click into the field of the remote window and let SmartClip do the rest. It works where Ctrl+V is blocked: remote desktop lock screens, virtual machine consoles, server management pages. You get time to click the right field, and typing stops by itself if the window changes, so a password never ends up in the wrong place.

Everything you copy, texts and pictures, stays in a list you can bring up from any program with a hotkey. Type a few words to find it, press Enter and you are back where you were, with the entry on the clipboard, ready to paste or to type.

The texts you use often, such as a signature, an address or a password, can be saved under a name and arranged in folders and subfolders. They are also in the menu of the icon by the clock, ready to take without even opening the window.

Built with privacy in mind:
• saved texts are encrypted for your Windows user;
• what password managers mark as private is not remembered;
• you can pause capturing, choose how many days to keep the history, or keep it in memory only;
• no account, no ads, no Internet connection: your data stays on your PC.

SmartClip is open source, under the MIT license. The user interface is in Italian.
```

### Product features

```
Types the clipboard key by key where paste does not work
Made for remote desktops, virtual machine consoles and server management pages
Adjustable wait before typing, and typing stops if the window changes
US keyboard on the server, and symbols composed with Alt codes
History of copied texts and pictures, with instant search
List available from any program with a hotkey
Important entries pinned to the top with a star
Encrypted saved texts, arranged in folders and subfolders
Saved texts also in the tray icon menu, without opening the window
Full preview of long texts and pictures
Customizable hotkeys
Private copies from password managers are not remembered
Pause capturing with one click
Export and import to a text file
Starts with Windows, hidden in the notification area
Built-in guide, organized by topic
No account, no ads, no Internet connection
User interface in Italian
```

### Search terms

```
clipboard
clipboard manager
clipboard history
password
remote desktop
type clipboard
paste blocked
```

### Screenshot captions

| File | Caption |
| --- | --- |
| `1-elenco.png` | Recently copied items on the left, saved texts on the right: search, pick and paste. |
| `2-digitazione.png` | Where Ctrl+V does not work, SmartClip types the clipboard key by key, after giving you time to click the right field. |
| `3-anteprima.png` | Rest the pointer on an entry to see all of it. |
| `4-salva.png` | Save a text under a name and put it in its folder. |
| `5-menu-tray.png` | Saved texts are also in the menu of the icon by the clock. |
| `6-opzioni.png` | Options for typing, hotkeys, history and starting with Windows. |
| `7-guida.png` | A guide organized by topic, always one F1 away. |

## Note per la verifica

Nell'invio, *Submission options → Notes for certification*. In inglese, perché le leggono i tester di Microsoft:

```
SmartClip is a clipboard manager that can also type the clipboard text as simulated key presses (SendInput), for sign-in screens of remote desktops and virtual machine consoles where paste is blocked. It is a .NET Framework 4 Windows Forms desktop app packaged as MSIX (runFullTrust). The user interface is in Italian; no account, no network access, no purchases.

How to test:
1. Launch SmartClip and copy some text in any program: it appears under "Copiati di recente" (recently copied).
2. Press Ctrl+D in any program: the window comes up with the list; type to search, Enter puts the entry back on the clipboard and returns to the previous window.
3. Typing: copy a text, open Notepad, click "Digita gli appunti" (type the clipboard) in SmartClip, then click into Notepad within 15 seconds; after a 1-second wait the text is typed. Alternatively press Ctrl+Alt+V inside Notepad. Typing stops if the foreground window changes.
4. Saved texts: "Nuovo..." (new) creates one; right-click > "Nuova cartella..." creates a folder. Saved texts also appear in the menu of the tray icon (right-click), each folder a submenu.

Global hotkeys: Ctrl+Alt+V (type the clipboard) and Ctrl+D (open the list), both configurable in Opzioni > Scorciatoie. Closing the window keeps the program in the notification area; to exit, right-click the tray icon > "Esci". F1 opens the built-in guide.
```

### Perché serve runFullTrust

Se Partner Center chiede di giustificare la capability riservata:

```
SmartClip is a classic desktop application (.NET Framework 4, Windows Forms) packaged with MSIX. Full trust is needed to be notified of clipboard changes (AddClipboardFormatListener), to register global hotkeys (RegisterHotKey) and to send keyboard input to the window the user chooses (SendInput), which is the main purpose of the app: typing a password where paste is not available, such as remote desktop sign-in screens.
```
