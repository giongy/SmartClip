# Pubblicare SmartClip sul Microsoft Store

Tutto quello che serve per la scheda dello Store, pronto da copiare in Partner Center, e i passi per arrivarci.
Il programma parla inglese (la lingua di default), spagnolo, francese e italiano: c'è una scheda per lingua, ognuna
con le sue schermate in [`images/<lingua>`](images). Si rifanno a programma chiuso con
`tests\tests.exe store store\images <en|es|fr|it>`, una lingua alla volta.

Ciò che si carica in Partner Center sta tutto in `store\upload` (fuori da git): il pacchetto `.msix`, scaricato
da GitHub con `get-package.ps1`, e la cartella `SmartClip-listing` (CSV + immagini) scritta da `make-listing.ps1`.
A ogni nuova versione i passi li fa la skill `partner-center` (`.claude\skills\partner-center`): basta chiedere a
Claude di preparare Partner Center.

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
4. **Pacchetto**: fai push (o avvia a mano il flusso *Release* da GitHub → Actions → *Run workflow*), poi
   `powershell -ExecutionPolicy Bypass -File store\get-package.ps1` scarica l'artifact **SmartClip-msix** in
   `store\upload\SmartClip-<versione>.msix` (con `-Build` avvia prima il flusso e lo aspetta). Ogni invio allo
   Store vuole una versione più alta della precedente, con la quarta cifra a zero (`3.8.4` va bene).
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
- **Schermate**: le otto della cartella della lingua, nell'ordine dei nomi (1366 × 768); l'ultima in tema scuro.
- **Copyright e marchi**: `© 2026 Luca Andreoli`
- **Termini di licenza aggiuntivi**: `Open source software under the MIT license: https://github.com/giongy/SmartClip/blob/main/LICENSE`
- **Sviluppato da**: `Luca Andreoli`
- **Novità di questa versione**: in ogni scheda, il blocco dopo i termini di ricerca (`make-listing.ps1` lo mette
  in `ReleaseNotes`); da riscrivere a ogni versione inviata.

## English (en-US) — la scheda principale

Schermate: [`images/en`](images/en).

### Product name

```
SmartClip – Clipboard & Typer
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

The texts and pictures you use often, such as a signature, an address or a password, can be saved, under a name if you like, and arranged in folders and subfolders. They are also in the menu of the icon by the clock, ready to take without even opening the window.

Built with privacy in mind:
• saved texts are encrypted for your Windows user;
• what password managers mark as private is not remembered;
• you can pause capturing and choose how many days to keep the history, or keep only today's;
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
Entries kept at the top for a while with an arrow
Saved texts and copied entries pasted anywhere with Ctrl+Shift+1…9
Encrypted saved texts and pictures, with or without a name, in folders and subfolders
Saved texts also in the tray icon menu, without opening the window
Full preview of long texts and pictures
Customizable hotkeys
Private copies from password managers are not remembered
Pause capturing with one click
Export and import to a text file, and the database in the folder you choose
Starts with Windows, hidden in the notification area
Built-in guide, organized by topic
Windows 11 look with the icons of Windows, light or dark theme, fonts and sizes of the interface and of the entries of your choice
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

### What's new in this version

```
A sturdier base: everything you copy and save now lives in one database, the SQLite that comes with Windows. Your history and saved texts move in by themselves at the first start.

Saved entries no longer need a name: without one, the list shows their content, handy for a second e-mail address. Copied pictures can be saved too, in folders like the texts.

The star is now an arrow: it keeps an entry at the top for a while, until its days are over. With 0 days the history keeps today's copies.

Under Options > History, choose where the database lives and see how much room it takes. Copied pictures take at most 200 MB: beyond that the oldest go, and a red bar under the list says so.

The options open in a window of their own, beside the list. Every other row of the copied entries is a shade apart, and the windows have slimmer margins.
```

### Screenshot captions

| File | Caption |
| --- | --- |
| `1-list.png` | Recently copied items on the left, saved texts on the right: search, pick and paste. |
| `2-typing.png` | Where Ctrl+V does not work, SmartClip types the clipboard key by key, after giving you time to click the right field. |
| `3-preview.png` | Rest the pointer on an entry to see all of it. |
| `4-save.png` | Save a text under a name and put it in its folder. |
| `5-tray-menu.png` | Saved texts are also in the menu of the icon by the clock. |
| `6-options.png` | Options in pages, like Windows Settings: typing, hotkeys, history and the program. |
| `7-guide.png` | A guide organized by topic, always one F1 away. |
| `8-dark.png` | Light or dark: choose it in the options, or let it follow Windows. |

## Italiano (it-IT)

Schermate: [`images/it`](images/it).

### Nome del prodotto

```
SmartClip – Clipboard & Typer
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

I testi e le immagini che usi spesso, come una firma, un indirizzo o una password, li salvi, con un nome se vuoi, e li organizzi in cartelle e sottocartelle. Li trovi anche nel menu dell'icona vicino all'orologio, da prendere senza nemmeno aprire la finestra.

Pensato per la riservatezza:
• i testi salvati sono cifrati per il tuo utente di Windows;
• ciò che i gestori di password segnano come riservato non viene ricordato;
• puoi sospendere la cattura e scegliere per quanti giorni tenere la cronologia, o tenere solo quella di oggi;
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
Voci tenute in cima per un po' con una freccia
Testi salvati e voci copiate incollati ovunque con Ctrl+Maiusc+1…9
Testi e immagini salvati cifrati, con o senza nome, in cartelle e sottocartelle
Testi salvati anche nel menu dell'icona, senza aprire la finestra
Anteprima completa di testi lunghi e immagini
Scorciatoie da tastiera personalizzabili
Le copie riservate dei gestori di password non vengono ricordate
Cattura sospendibile con un clic
Esportazione e importazione in un file di testo, e il database nella cartella che scegli
Avvio con Windows, nascosto nell'area di notifica
Guida integrata, divisa per argomenti
Aspetto da Windows 11 con le icone di Windows, tema chiaro o scuro, caratteri e dimensioni di interfaccia e voci a scelta
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

### Novità di questa versione

```
Una base più solida: tutto ciò che copi e salvi ora sta in un unico database, il SQLite incluso in Windows. Cronologia e testi salvati ci si spostano da soli al primo avvio.

Le voci salvate non hanno più bisogno di un nome: senza, l'elenco ne mostra il contenuto, comodo per un secondo indirizzo email. Anche le immagini copiate si possono salvare, in cartelle come i testi.

La stellina ora è una freccia: tiene una voce in cima per un po', finché non scadono i suoi giorni. Con 0 giorni la cronologia tiene le copie di oggi.

In Opzioni > Cronologia scegli dove tenere il database e vedi quanto spazio occupa. Le immagini copiate occupano al massimo 200 MB: oltre, le più vecchie se ne vanno, e una barra rossa sotto l'elenco lo dice.

Le opzioni si aprono in una finestra a parte, accanto all'elenco. Le righe delle voci copiate si alternano con una tinta leggera, e le finestre hanno margini più sottili.
```

### Didascalie delle schermate

| File | Didascalia |
| --- | --- |
| `1-list.png` | Le cose copiate di recente a sinistra, i testi salvati a destra: cerca, scegli e incolla. |
| `2-typing.png` | Dove Ctrl+V non funziona, SmartClip digita gli appunti tasto per tasto, dopo averti dato il tempo di cliccare nel campo giusto. |
| `3-preview.png` | Fermati su una voce per vederla per intero. |
| `4-save.png` | Salva un testo con un nome e mettilo nella sua cartella. |
| `5-tray-menu.png` | I testi salvati anche nel menu dell'icona vicino all'orologio. |
| `6-options.png` | Opzioni divise in pagine, come in Impostazioni di Windows: digitazione, scorciatoie, cronologia e programma. |
| `7-guide.png` | Una guida divisa per argomenti, sempre a portata di F1. |
| `8-dark.png` | Chiaro o scuro: si sceglie nelle opzioni, o segue Windows. |

## Español (es-ES)

Schermate: [`images/es`](images/es).

### Nombre del producto

```
SmartClip – Clipboard & Typer
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

Los textos y las imágenes que usas a menudo, como una firma, una dirección o una contraseña, los guardas, con un nombre si quieres, y los organizas en carpetas y subcarpetas. También están en el menú del icono junto al reloj, listos para tomar sin abrir siquiera la ventana.

Pensado para la privacidad:
• los textos guardados están cifrados para tu usuario de Windows;
• lo que los gestores de contraseñas marcan como privado no se recuerda;
• puedes pausar la captura y elegir cuántos días conservar el historial, o solo el de hoy;
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
Entradas mantenidas arriba un tiempo con una flecha
Textos guardados y entradas copiadas pegados en cualquier sitio con Ctrl+Mayús+1…9
Textos e imágenes guardados cifrados, con o sin nombre, en carpetas y subcarpetas
Textos guardados también en el menú del icono, sin abrir la ventana
Vista previa completa de textos largos e imágenes
Atajos de teclado personalizables
Las copias privadas de los gestores de contraseñas no se recuerdan
Captura que se pausa con un clic
Exportación e importación a un archivo de texto, y la base de datos en la carpeta que elijas
Inicio con Windows, oculto en el área de notificación
Guía integrada, dividida por temas
Aspecto de Windows 11 con los iconos de Windows, tema claro u oscuro, fuentes y tamaños de la interfaz y de las entradas a elegir
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

### Novedades de esta versión

```
Una base más sólida: todo lo que copias y guardas está ahora en una sola base de datos, el SQLite que trae Windows. El historial y los textos guardados se trasladan solos en el primer inicio.

Las entradas guardadas ya no necesitan un nombre: sin él, la lista muestra su contenido, útil para una segunda dirección de correo. También las imágenes copiadas se pueden guardar, en carpetas como los textos.

La estrella ahora es una flecha: mantiene una entrada arriba un tiempo, hasta que vencen sus días. Con 0 días el historial guarda las copias de hoy.

En Opciones > Historial eliges dónde guardar la base de datos y ves cuánto espacio ocupa. Las imágenes copiadas ocupan como máximo 200 MB: por encima, las más antiguas se van, y una barra roja bajo la lista lo dice.

Las opciones se abren en una ventana aparte, junto a la lista. Las filas de las entradas copiadas se alternan con un tono suave, y las ventanas tienen márgenes más finos.
```

### Leyendas de las capturas

| File | Leyenda |
| --- | --- |
| `1-list.png` | Lo copiado recientemente a la izquierda, los textos guardados a la derecha: busca, elige y pega. |
| `2-typing.png` | Donde Ctrl+V no funciona, SmartClip teclea el portapapeles tecla por tecla, tras darte tiempo para hacer clic en el campo correcto. |
| `3-preview.png` | Deja el puntero sobre una entrada para verla entera. |
| `4-save.png` | Guarda un texto con un nombre y ponlo en su carpeta. |
| `5-tray-menu.png` | Los textos guardados también están en el menú del icono junto al reloj. |
| `6-options.png` | Opciones en páginas, como en Configuración de Windows: tecleo, atajos, historial y programa. |
| `7-guide.png` | Una guía dividida por temas, siempre a un F1 de distancia. |
| `8-dark.png` | Claro u oscuro: se elige en las opciones, o sigue a Windows. |

## Français (fr-FR)

Schermate: [`images/fr`](images/fr).

### Nom du produit

```
SmartClip – Clipboard & Typer
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

Les textes et les images que vous utilisez souvent, comme une signature, une adresse ou un mot de passe, s'enregistrent, sous un nom si vous voulez, et se rangent dans des dossiers et sous-dossiers. Ils sont aussi dans le menu de l'icône près de l'horloge, prêts à prendre sans même ouvrir la fenêtre.

Pensé pour la confidentialité :
• les textes enregistrés sont chiffrés pour votre utilisateur Windows ;
• ce que les gestionnaires de mots de passe marquent comme privé n'est pas mémorisé ;
• vous pouvez suspendre la capture et choisir combien de jours garder l'historique, ou seulement celui d'aujourd'hui ;
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
Éléments gardés un moment en haut avec une flèche
Textes enregistrés et éléments copiés collés partout avec Ctrl+Maj+1…9
Textes et images enregistrés chiffrés, avec ou sans nom, dans des dossiers et sous-dossiers
Textes enregistrés aussi dans le menu de l'icône, sans ouvrir la fenêtre
Aperçu complet des textes longs et des images
Raccourcis clavier personnalisables
Les copies privées des gestionnaires de mots de passe ne sont pas mémorisées
Capture suspendue en un clic
Export et import dans un fichier texte, et la base de données dans le dossier de votre choix
Démarrage avec Windows, caché dans la zone de notification
Guide intégré, organisé par sujet
Aspect Windows 11 avec les icônes de Windows, thème clair ou sombre, polices et tailles de l'interface et des éléments au choix
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

### Nouveautés de cette version

```
Une base plus solide : tout ce que vous copiez et enregistrez est maintenant dans une seule base de données, le SQLite fourni avec Windows. L'historique et les textes enregistrés y passent tout seuls au premier démarrage.

Les éléments enregistrés n'ont plus besoin d'un nom : sans nom, la liste montre leur contenu, pratique pour une deuxième adresse e-mail. Les images copiées peuvent aussi être enregistrées, dans des dossiers comme les textes.

L'étoile est devenue une flèche : elle garde un élément en haut un moment, jusqu'à la fin de ses jours. Avec 0 jour, l'historique garde les copies d'aujourd'hui.

Dans Options > Historique, choisissez où garder la base de données et voyez la place qu'elle occupe. Les images copiées occupent au maximum 200 Mo : au-delà, les plus anciennes partent, et une barre rouge sous la liste le signale.

Les options s'ouvrent dans une fenêtre à part, à côté de la liste. Les lignes des éléments copiés alternent avec une teinte légère, et les fenêtres ont des marges plus fines.
```

### Légendes des captures

| File | Légende |
| --- | --- |
| `1-list.png` | Les éléments copiés récemment à gauche, les textes enregistrés à droite : cherchez, choisissez et collez. |
| `2-typing.png` | Là où Ctrl+V ne marche pas, SmartClip saisit le presse-papiers touche par touche, après vous avoir laissé le temps de cliquer dans le bon champ. |
| `3-preview.png` | Laissez le pointeur sur un élément pour le voir en entier. |
| `4-save.png` | Enregistrez un texte sous un nom et rangez-le dans son dossier. |
| `5-tray-menu.png` | Les textes enregistrés sont aussi dans le menu de l'icône près de l'horloge. |
| `6-options.png` | Des options en pages, comme dans les Paramètres de Windows : saisie, raccourcis, historique et programme. |
| `7-guide.png` | Un guide organisé par sujet, toujours à portée de F1. |
| `8-dark.png` | Clair ou sombre : à choisir dans les options, ou comme Windows. |

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
