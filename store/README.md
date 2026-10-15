# Pubblicare SmartClip sul Microsoft Store

Tutto quello che serve per la scheda dello Store, pronto da copiare in Partner Center, e i passi per arrivarci.
Il programma parla inglese (la lingua di default), spagnolo, francese e italiano: c'è una scheda per lingua, ognuna
con le sue schermate in [`images/<lingua>`](images). Si rifanno a programma chiuso con
`tests\tests.exe store store\images <en|es|fr|it>`, una lingua alla volta.

## Passi

1. **Iscrizione** su [storedeveloper.microsoft.com](https://storedeveloper.microsoft.com) come sviluppatore
   individuale (gratuita: documento d'identità e selfie).
2. **Prenota il nome** in Partner Center: *Apps and games → New product → MSIX or PWA app* →
   «SmartClip – Clipboard & Typer» («SmartClip» da solo è già preso). Va scritto identico, con la lineetta
   lunga «–» e la «&»: lo stesso nome sta in `DisplayName` nel manifesto, e Partner Center rifiuta un pacchetto
   il cui nome non coincide con quello prenotato. Nel programma (titolo della finestra, impostazioni) resta
   «SmartClip».
3. **Identità del pacchetto**: in *Product management → Product identity* ci sono tre valori. Vanno in
   [`packaging/AppxManifest.xml`](../packaging/AppxManifest.xml) al posto dei segnaposto:
   - `Package/Identity/Name` ← il valore di **Package/Identity/Name**
   - `Package/Identity/Publisher` ← **Package/Identity/Publisher** (inizia con `CN=`)
   - `Package/Properties/PublisherDisplayName` ← **Package/Properties/PublisherDisplayName**
4. **Pacchetto**: fai push (o avvia a mano il flusso *Release* da GitHub → Actions → *Run workflow*) e scarica
   l'artifact **SmartClip-msix** dall'esecuzione: dentro c'è `SmartClip-<versione>.msix`. Ogni invio allo Store
   vuole una versione più alta della precedente, con la quarta cifra a zero (`3.8.4` va bene).
5. **Prova prima di inviare** (facoltativo): con *Impostazioni → Sistema → Per sviluppatori → Modalità
   sviluppatore* attiva, dopo `build.cmd` e `powershell -ExecutionPolicy Bypass -File packaging\pack.ps1`:
   `Add-AppxPackage -Register packaging\out\layout\AppxManifest.xml`. Il programma compare nel menu Start come
   se venisse dallo Store; si toglie da *Impostazioni → App*. Da provare: avvio con Windows (l'interruttore nelle
   opzioni), il collegamento alla cartella della cronologia, il cambio di lingua (riavvia il programma).
6. **Lingue delle schede**: il pacchetto dichiara le quattro lingue, e Partner Center propone una scheda per
   ciascuna. Compilale tutte con i testi qui sotto, ognuna con le schermate della sua cartella.
7. **Nuovo invio** in Partner Center, sezione per sezione come qui sotto, poi *Submit to the Store*. La verifica
   di Microsoft richiede di solito da qualche ora a qualche giorno.

## Prezzi e disponibilità

- **Prezzo**: gratuito.
- **Mercati**: tutti.
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

## In tutte le schede

- **Logo** (*Icona del riquadro dell'app 1:1, 300 × 300*): [`images/tile-300.png`](images/tile-300.png).
- **Schermate**: le sette della cartella della lingua, nell'ordine dei nomi (1366 × 768).
- **Copyright e marchi**: `© 2026 Luca Andreoli`
- **Termini di licenza aggiuntivi**: `Open source software under the MIT license: https://github.com/giongy/SmartClip/blob/main/LICENSE`
- **Sviluppato da**: `Luca Andreoli`
- **Novità di questa versione**: vuoto al primo invio; dagli aggiornamenti in poi, le novità della release.

## English (en-US) — la scheda principale

Schermate: [`images/en`](images/en).

### Product name

```
SmartClip
```

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

SmartClip is open source, under the MIT license. It speaks English, Spanish, French and Italian.
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
In English, Spanish, French and Italian
No account, no ads, no Internet connection
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
| `1-list.png` | Recently copied items on the left, saved texts on the right: search, pick and paste. |
| `2-typing.png` | Where Ctrl+V does not work, SmartClip types the clipboard key by key, after giving you time to click the right field. |
| `3-preview.png` | Rest the pointer on an entry to see all of it. |
| `4-save.png` | Save a text under a name and put it in its folder. |
| `5-tray-menu.png` | Saved texts are also in the menu of the icon by the clock. |
| `6-options.png` | Options for typing, hotkeys, history, language and starting with Windows. |
| `7-guide.png` | A guide organized by topic, always one F1 away. |

## Italiano (it-IT)

Schermate: [`images/it`](images/it).

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

SmartClip è open source, con licenza MIT. Parla italiano, inglese, spagnolo e francese.
```

### Caratteristiche del prodotto

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
In italiano, inglese, spagnolo e francese
Nessun account, nessuna pubblicità, nessuna connessione a Internet
```

### Termini di ricerca

```
appunti
clipboard
password
desktop remoto
cronologia appunti
gestore appunti
digitare password
```

### Didascalie delle schermate

| File | Didascalia |
| --- | --- |
| `1-list.png` | Le cose copiate di recente a sinistra, i testi salvati a destra: cerca, scegli e incolla. |
| `2-typing.png` | Dove Ctrl+V non funziona, SmartClip digita gli appunti tasto per tasto, dopo averti dato il tempo di cliccare nel campo giusto. |
| `3-preview.png` | Fermati su una voce per vederla per intero. |
| `4-save.png` | Salva un testo con un nome e mettilo nella sua cartella. |
| `5-tray-menu.png` | I testi salvati anche nel menu dell'icona vicino all'orologio. |
| `6-options.png` | Opzioni per la digitazione, le scorciatoie, la cronologia, la lingua e l'avvio con Windows. |
| `7-guide.png` | Una guida divisa per argomenti, sempre a portata di F1. |

## Español (es-ES)

Schermate: [`images/es`](images/es).

### Nombre del producto

```
SmartClip
```

### Descripción breve

```
Todo lo que copias, siempre a mano. Y donde pegar no funciona, como en las pantallas de inicio de sesión de los escritorios remotos, SmartClip teclea el portapapeles por ti, tecla por tecla.
```

### Descripción

```
SmartClip es un gestor del portapapeles ligero para Windows con un talento más: sabe teclear el texto del portapapeles como si lo escribieras tú en el teclado.

Copia una contraseña, haz clic en el campo de la ventana remota y deja que SmartClip haga el resto. Funciona también donde Ctrl+V está bloqueado: pantallas de bloqueo de escritorios remotos, consolas de máquinas virtuales, páginas de gestión de servidores. Tienes tiempo para hacer clic en el campo correcto, y el tecleo se detiene solo si cambias de ventana: una contraseña nunca acaba en el sitio equivocado.

Todo lo que copias, textos e imágenes, queda en una lista que se abre desde cualquier programa con un atajo. Escribe unas palabras para encontrarlo, pulsa Intro y vuelves a donde estabas con la entrada ya en el portapapeles, lista para pegar o teclear.

Los textos que usas a menudo, como una firma, una dirección o una contraseña, los guardas con un nombre y los organizas en carpetas y subcarpetas. También están en el menú del icono junto al reloj, listos para tomar sin abrir siquiera la ventana.

Pensado para la privacidad:
• los textos guardados están cifrados para tu usuario de Windows;
• lo que los gestores de contraseñas marcan como privado no se recuerda;
• puedes pausar la captura, elegir cuántos días conservar el historial o tenerlo solo en memoria;
• sin cuenta, sin publicidad, sin conexión a Internet: tus datos se quedan en tu PC.

SmartClip es de código abierto, con licencia MIT. Habla español, inglés, francés e italiano.
```

### Características del producto

```
Teclea el portapapeles tecla por tecla donde pegar no funciona
Pensado para escritorios remotos, consolas de máquinas virtuales y páginas de gestión de servidores
Espera ajustable antes de teclear, y el tecleo se detiene si cambia la ventana
Teclado estadounidense en el servidor y símbolos compuestos con códigos Alt
Historial de textos e imágenes copiados, con búsqueda instantánea
Lista disponible desde cualquier programa con un atajo
Entradas importantes fijadas arriba con una estrella
Textos guardados cifrados, organizados en carpetas y subcarpetas
Textos guardados también en el menú del icono, sin abrir la ventana
Vista previa completa de textos largos e imágenes
Atajos de teclado personalizables
Las copias privadas de los gestores de contraseñas no se recuerdan
Captura que se pausa con un clic
Exportación e importación a un archivo de texto
Inicio con Windows, oculto en el área de notificación
Guía integrada, dividida por temas
En español, inglés, francés e italiano
Sin cuenta, sin publicidad, sin conexión a Internet
```

### Términos de búsqueda

```
portapapeles
historial portapapeles
gestor portapapeles
contraseña
escritorio remoto
teclear contraseña
clipboard
```

### Leyendas de las capturas

| File | Leyenda |
| --- | --- |
| `1-list.png` | Lo copiado recientemente a la izquierda, los textos guardados a la derecha: busca, elige y pega. |
| `2-typing.png` | Donde Ctrl+V no funciona, SmartClip teclea el portapapeles tecla por tecla, tras darte tiempo para hacer clic en el campo correcto. |
| `3-preview.png` | Deja el puntero sobre una entrada para verla entera. |
| `4-save.png` | Guarda un texto con un nombre y ponlo en su carpeta. |
| `5-tray-menu.png` | Los textos guardados también están en el menú del icono junto al reloj. |
| `6-options.png` | Opciones de tecleo, atajos, historial, idioma e inicio con Windows. |
| `7-guide.png` | Una guía dividida por temas, siempre a un F1 de distancia. |

## Français (fr-FR)

Schermate: [`images/fr`](images/fr).

### Nom du produit

```
SmartClip
```

### Description courte

```
Tout ce que vous copiez, toujours sous la main. Et là où coller ne marche pas, comme sur les écrans de connexion des bureaux à distance, SmartClip saisit le presse-papiers pour vous, touche par touche.
```

### Description

```
SmartClip est un gestionnaire de presse-papiers léger pour Windows, avec un talent de plus : il sait saisir le texte du presse-papiers comme si vous le tapiez au clavier.

Copiez un mot de passe, cliquez dans le champ de la fenêtre distante et laissez faire SmartClip. Il marche aussi là où Ctrl+V est bloqué : écrans de verrouillage des bureaux à distance, consoles de machines virtuelles, pages d'administration des serveurs. Vous avez le temps de cliquer dans le bon champ, et la saisie s'arrête toute seule si la fenêtre change : un mot de passe ne finit jamais au mauvais endroit.

Tout ce que vous copiez, textes et images, reste dans une liste qui s'ouvre depuis n'importe quel programme avec un raccourci. Tapez quelques mots pour le retrouver, appuyez sur Entrée et vous revenez où vous étiez, l'élément déjà dans le presse-papiers, prêt à coller ou à saisir.

Les textes que vous utilisez souvent, comme une signature, une adresse ou un mot de passe, s'enregistrent sous un nom et se rangent dans des dossiers et sous-dossiers. Ils sont aussi dans le menu de l'icône près de l'horloge, prêts à prendre sans même ouvrir la fenêtre.

Pensé pour la confidentialité :
• les textes enregistrés sont chiffrés pour votre utilisateur Windows ;
• ce que les gestionnaires de mots de passe marquent comme privé n'est pas mémorisé ;
• vous pouvez suspendre la capture, choisir combien de jours garder l'historique, ou le garder seulement en mémoire ;
• pas de compte, pas de publicité, pas de connexion à Internet : vos données restent sur votre PC.

SmartClip est open source, sous licence MIT. Il parle français, anglais, espagnol et italien.
```

### Fonctionnalités du produit

```
Saisit le presse-papiers touche par touche là où coller ne marche pas
Conçu pour les bureaux à distance, les consoles de machines virtuelles et les pages d'administration des serveurs
Attente réglable avant la saisie, qui s'arrête si la fenêtre change
Clavier américain sur le serveur et symboles composés avec des codes Alt
Historique des textes et images copiés, avec recherche instantanée
Liste accessible depuis n'importe quel programme avec un raccourci
Éléments importants épinglés en haut avec une étoile
Textes enregistrés chiffrés, rangés en dossiers et sous-dossiers
Textes enregistrés aussi dans le menu de l'icône, sans ouvrir la fenêtre
Aperçu complet des textes longs et des images
Raccourcis clavier personnalisables
Les copies privées des gestionnaires de mots de passe ne sont pas mémorisées
Capture suspendue en un clic
Export et import dans un fichier texte
Démarrage avec Windows, caché dans la zone de notification
Guide intégré, organisé par sujet
En français, anglais, espagnol et italien
Pas de compte, pas de publicité, pas de connexion à Internet
```

### Termes de recherche

```
presse-papiers
historique presse-papiers
gestionnaire presse-papiers
mot de passe
bureau à distance
saisir mot de passe
clipboard
```

### Légendes des captures

| File | Légende |
| --- | --- |
| `1-list.png` | Les éléments copiés récemment à gauche, les textes enregistrés à droite : cherchez, choisissez et collez. |
| `2-typing.png` | Là où Ctrl+V ne marche pas, SmartClip saisit le presse-papiers touche par touche, après vous avoir laissé le temps de cliquer dans le bon champ. |
| `3-preview.png` | Laissez le pointeur sur un élément pour le voir en entier. |
| `4-save.png` | Enregistrez un texte sous un nom et rangez-le dans son dossier. |
| `5-tray-menu.png` | Les textes enregistrés sont aussi dans le menu de l'icône près de l'horloge. |
| `6-options.png` | Options de saisie, raccourcis, historique, langue et démarrage avec Windows. |
| `7-guide.png` | Un guide organisé par sujet, toujours à portée de F1. |

## Note per la verifica

Nell'invio, *Submission options → Notes for certification*. In inglese, perché le leggono i tester di Microsoft, e
con le etichette dell'interfaccia inglese, quella che vedono al primo avvio:

```
SmartClip is a clipboard manager that can also type the clipboard text as simulated key presses (SendInput), for sign-in screens of remote desktops and virtual machine consoles where paste is blocked. It is a .NET Framework 4 Windows Forms desktop app packaged as MSIX (runFullTrust). The user interface is in English by default, and also in Spanish, French and Italian (Options > Program > Language). No account, no network access, no purchases.

How to test:
1. Launch SmartClip and copy some text in any program: it appears under "Recently copied".
2. Press Ctrl+D in any program: the window comes up with the list; type to search, Enter puts the entry back on the clipboard and returns to the previous window.
3. Typing: copy a text, open Notepad, click "Type clipboard" in SmartClip, then click into Notepad within 15 seconds; after a 1-second wait the text is typed. Alternatively press Ctrl+Alt+V inside Notepad. Typing stops if the foreground window changes.
4. Saved texts: "New..." creates one; right-click > "New folder..." creates a folder. Saved texts also appear in the menu of the tray icon (right-click), each folder a submenu.

Global hotkeys: Ctrl+Alt+V (type the clipboard) and Ctrl+D (open the list), both configurable in Options > Hotkeys. By default clicking another program hides the window to the notification area; to exit, right-click the tray icon > "Exit". F1 opens the built-in guide. Changing the language restarts the app.
```

## Perché serve runFullTrust

Se Partner Center chiede di giustificare la capability riservata:

```
SmartClip is a classic desktop application (.NET Framework 4, Windows Forms) packaged with MSIX. Full trust is needed to be notified of clipboard changes (AddClipboardFormatListener), to register global hotkeys (RegisterHotKey) and to send keyboard input to the window the user chooses (SendInput), which is the main purpose of the app: typing a password where paste is not available, such as remote desktop sign-in screens.
```
