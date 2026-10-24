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
- **Schermate**: le otto della cartella della lingua, nell'ordine dei nomi (1366 × 768); in tema scuro, l'ultima in chiaro.
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

Copy the text, click into the field of the remote window and let SmartClip do the rest. It works where Ctrl+V is blocked: remote desktop lock screens, virtual machine consoles, server management pages. You get time to click the right field, and typing stops by itself if the window changes, so what you type never ends up in the wrong place.

Everything you copy, texts and pictures, stays in a list you can bring up from any program with a hotkey. Type a few words to find it, press Enter and you are back where you were, with the entry on the clipboard, ready to paste or to type.

The texts and pictures you use often, such as a signature with its bold and its fonts, an address or an access code, can be saved, under a name if you like, and arranged in folders and subfolders. They are also in the menu of the icon by the clock, ready to take without even opening the window.

Built with privacy in mind:
• saved texts are encrypted for your Windows user;
• copies that other programs mark as private are not remembered;
• you can pause capturing and choose how many days to keep the history, or keep only today's;
• no account, no ads, no Internet connection: your data stays on your PC.

SmartClip is open source, under the MIT license. It speaks English, Spanish, French and Italian.
```

### Product features

```
Types the clipboard key by key where paste does not work: remote desktops, virtual machine consoles, server management pages
Adjustable wait before typing, and typing stops if the window changes
US keyboard on the server, and symbols composed with Alt codes
History of copied texts and pictures, with instant search that also finds saved texts and highlights the words found
A sidebar like the apps of Windows 11, to widen or reduce to icons: history, pinned, shortcuts, saved texts in folders, the programs you copied in
Compact cards all of one height, one line or two, the content first and the buttons under the pointer, with tabs for texts, code, links and pictures
List available from any program with a hotkey, and hotkeys of your choice
Entries kept at the top for a while with an arrow, and saved texts or copied entries pasted anywhere with Ctrl+Shift+1…9
Several cards selected at once, with Ctrl or Shift, to keep at the top, move into a folder or delete together
Encrypted saved texts and pictures, with or without a name, in folders and subfolders
Formatted saved texts, such as mail signatures: bold, italic, fonts and sizes, pasted as they are
Saved texts also in the tray icon menu, without opening the window
Full preview of long texts and pictures, and the text in a picture copied with the text recognition of Windows
Copies that other programs mark as private are not remembered, and capturing pauses with one click
Export and import to a text file, and the database in the folder you choose
Starts with Windows, hidden in the notification area
Built-in guide by topic, in short sections that open and close
Clean look in warm greys with an orange accent, line icons, options grouped like Windows Settings, light or dark theme, fonts and sizes of your choice
In English, Spanish, French and Italian
No account, no ads, no Internet connection
```

### Search terms

```
clipboard
clipboard manager
clipboard history
autotype
remote desktop
type clipboard
paste blocked
```

### What's new in this version

```
Bug fixes.
```

### Screenshot captions

| File | Caption |
| --- | --- |
| `1-list.png` | The history of what you copy: places in the sidebar, kinds in the tabs, and the program each entry was copied in. |
| `2-typing.png` | Where Ctrl+V does not work, SmartClip types the clipboard key by key, after giving you time to click the right field. |
| `3-preview.png` | The eye on a card shows the entry whole: every line of a text, a larger picture. |
| `4-save.png` | Save a text under a name, in its folder, formatted too: a mail signature keeps its bold and its fonts. |
| `5-tray-menu.png` | Saved texts are also in the menu of the icon by the clock. |
| `6-options.png` | Options grouped like Windows Settings, each with its icon: typing, hotkeys, history, appearance and the program. |
| `7-guide.png` | A guide by topic, in short sections that open and close, always one F1 away. |
| `8-light.png` | Dark or light: choose it in the options, or let it follow Windows. |

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

Copia il testo, clicca nel campo della finestra remota e lascia fare a SmartClip. Funziona anche dove Ctrl+V è bloccato: nelle schermate di blocco dei desktop remoti, nelle console delle macchine virtuali, nelle pagine di gestione dei server. Hai il tempo di cliccare nel campo giusto, e la digitazione si ferma da sola se cambi finestra: ciò che digiti non finisce mai nel posto sbagliato.

Tutto quello che copi, testi e immagini, resta in un elenco che si apre da qualsiasi programma con una scorciatoia. Scrivi qualche parola per trovarlo, premi Invio e torni dov'eri con la voce già negli appunti, pronta da incollare o da digitare.

I testi e le immagini che usi spesso, come una firma con i suoi grassetti e i suoi caratteri, un indirizzo o un codice di accesso, li salvi, con un nome se vuoi, e li organizzi in cartelle e sottocartelle. Li trovi anche nel menu dell'icona vicino all'orologio, da prendere senza nemmeno aprire la finestra.

Pensato per la riservatezza:
• i testi salvati sono cifrati per il tuo utente di Windows;
• le copie che altri programmi segnano come riservate non vengono ricordate;
• puoi sospendere la cattura e scegliere per quanti giorni tenere la cronologia, o tenere solo quella di oggi;
• nessun account, nessuna pubblicità, nessuna connessione a Internet: i tuoi dati restano sul tuo PC.

SmartClip è open source, con licenza MIT. Parla italiano, inglese, spagnolo e francese.
```

### Caratteristiche del prodotto

```
Digita gli appunti tasto per tasto dove incollare non funziona: desktop remoti, console di macchine virtuali, pagine di gestione dei server
Attesa regolabile prima di digitare, e la digitazione si ferma se cambi finestra
Tastiera americana sul server e simboli composti con Alt+codice
Cronologia di testi e immagini copiati, con una ricerca istantanea che trova anche i testi salvati ed evidenzia le parole trovate
Una barra laterale come nelle app di Windows 11, da allargare o ridurre alle icone: cronologia, in alto, scorciatoie, salvati in cartelle, i programmi in cui hai copiato
Voci in schede compatte tutte alte uguali, una riga o due, prima il contenuto e i pulsanti sotto il puntatore, con le schede per testi, codice, link e immagini
Elenco richiamabile da qualsiasi programma con una scorciatoia, e scorciatoie a tua scelta
Voci tenute in cima per un po' con una freccia, e testi salvati o voci copiate incollati ovunque con Ctrl+Maiusc+1…9
Più schede selezionate insieme, con Ctrl o Maiusc, da tenere in cima, spostare in una cartella o eliminare in una volta
Testi e immagini salvati cifrati, con o senza nome, in cartelle e sottocartelle
Testi salvati formattati, come le firme di posta: grassetto, corsivo, caratteri e dimensioni, incollati così come sono
Testi salvati anche nel menu dell'icona, senza aprire la finestra
Anteprima completa di testi lunghi e immagini, e il testo di un'immagine copiato con il riconoscimento di Windows
Le copie che altri programmi segnano come riservate non vengono ricordate, e la cattura si sospende con un clic
Esportazione e importazione in un file di testo, e il database nella cartella che scegli
Avvio con Windows, nascosto nell'area di notifica
Guida integrata per argomenti, in sezioni brevi che si aprono e si chiudono
Aspetto pulito in grigi caldi con un accento arancio, icone a linee, opzioni raggruppate come Impostazioni, tema chiaro o scuro, caratteri e dimensioni a scelta
In italiano, inglese, spagnolo e francese
Nessun account, nessuna pubblicità, nessuna connessione a Internet
```

### Termini di ricerca

```
appunti
clipboard
digitare appunti
desktop remoto
cronologia appunti
gestore appunti
incolla bloccato
```

### Novità di questa versione

```
Correzioni di bug.
```

### Didascalie delle schermate

| File | Didascalia |
| --- | --- |
| `1-list.png` | La cronologia di ciò che copi: i posti nella barra laterale, i tipi nelle schede e il programma in cui è stata copiata ogni voce. |
| `2-typing.png` | Dove Ctrl+V non funziona, SmartClip digita gli appunti tasto per tasto, dopo averti dato il tempo di cliccare nel campo giusto. |
| `3-preview.png` | L'occhio di una scheda mostra la voce per intero: ogni riga di un testo, un'immagine più grande. |
| `4-save.png` | Salva un testo con un nome, nella sua cartella, anche formattato: una firma di posta tiene grassetti e caratteri. |
| `5-tray-menu.png` | I testi salvati anche nel menu dell'icona vicino all'orologio. |
| `6-options.png` | Opzioni raggruppate come in Impostazioni di Windows, ognuna con la sua icona: digitazione, scorciatoie, cronologia, aspetto e programma. |
| `7-guide.png` | Una guida per argomenti, in sezioni brevi che si aprono e si chiudono, sempre a portata di F1. |
| `8-light.png` | Scuro o chiaro: si sceglie nelle opzioni, o segue Windows. |

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

Copia el texto, haz clic en el campo de la ventana remota y deja que SmartClip haga el resto. Funciona también donde Ctrl+V está bloqueado: pantallas de bloqueo de escritorios remotos, consolas de máquinas virtuales, páginas de gestión de servidores. Tienes tiempo para hacer clic en el campo correcto, y el tecleo se detiene solo si cambias de ventana: lo que tecleas nunca acaba en el sitio equivocado.

Todo lo que copias, textos e imágenes, queda en una lista que se abre desde cualquier programa con un atajo. Escribe unas palabras para encontrarlo, pulsa Intro y vuelves a donde estabas con la entrada ya en el portapapeles, lista para pegar o teclear.

Los textos y las imágenes que usas a menudo, como una firma con sus negritas y sus fuentes, una dirección o un código de acceso, los guardas, con un nombre si quieres, y los organizas en carpetas y subcarpetas. También están en el menú del icono junto al reloj, listos para tomar sin abrir siquiera la ventana.

Pensado para la privacidad:
• los textos guardados están cifrados para tu usuario de Windows;
• las copias que otros programas marcan como privadas no se recuerdan;
• puedes pausar la captura y elegir cuántos días conservar el historial, o solo el de hoy;
• sin cuenta, sin publicidad, sin conexión a Internet: tus datos se quedan en tu PC.

SmartClip es de código abierto, con licencia MIT. Habla español, inglés, francés e italiano.
```

### Características del producto

```
Teclea el portapapeles tecla por tecla donde pegar no funciona: escritorios remotos, consolas de máquinas virtuales, páginas de gestión de servidores
Espera ajustable antes de teclear, y el tecleo se detiene si cambia la ventana
Teclado estadounidense en el servidor y símbolos compuestos con códigos Alt
Historial de textos e imágenes copiados, con una búsqueda instantánea que también encuentra los textos guardados y resalta las palabras encontradas
Una barra lateral como en las apps de Windows 11, para ensanchar o reducir a iconos: historial, arriba, atajos, guardados en carpetas, los programas donde copiaste
Entradas en tarjetas compactas de la misma altura, una línea o dos, primero el contenido y los botones bajo el puntero, con pestañas para textos, código, enlaces e imágenes
Lista disponible desde cualquier programa con un atajo, y atajos a tu elección
Entradas mantenidas arriba un tiempo con una flecha, y textos guardados o entradas copiadas pegados en cualquier sitio con Ctrl+Mayús+1…9
Varias tarjetas seleccionadas a la vez, con Ctrl o Mayús, para mantener arriba, mover a una carpeta o eliminar juntas
Textos e imágenes guardados cifrados, con o sin nombre, en carpetas y subcarpetas
Textos guardados con formato, como las firmas de correo: negrita, cursiva, fuentes y tamaños, pegados tal cual
Textos guardados también en el menú del icono, sin abrir la ventana
Vista previa completa de textos largos e imágenes, y el texto de una imagen copiado con el reconocimiento de Windows
Las copias que otros programas marcan como privadas no se recuerdan, y la captura se pausa con un clic
Exportación e importación a un archivo de texto, y la base de datos en la carpeta que elijas
Inicio con Windows, oculto en el área de notificación
Guía integrada por temas, en secciones breves que se abren y se cierran
Aspecto limpio en grises cálidos con un acento naranja, iconos de línea, opciones agrupadas como Configuración, tema claro u oscuro, fuentes y tamaños a elegir
En español, inglés, francés e italiano
Sin cuenta, sin publicidad, sin conexión a Internet
```

### Términos de búsqueda

```
portapapeles
historial portapapeles
gestor portapapeles
teclear portapapeles
escritorio remoto
pegar bloqueado
clipboard
```

### Novedades de esta versión

```
Corrección de errores.
```

### Leyendas de las capturas

| File | Leyenda |
| --- | --- |
| `1-list.png` | El historial de lo que copias: los lugares en la barra lateral, los tipos en las pestañas y el programa en el que se copió cada entrada. |
| `2-typing.png` | Donde Ctrl+V no funciona, SmartClip teclea el portapapeles tecla por tecla, tras darte tiempo para hacer clic en el campo correcto. |
| `3-preview.png` | El ojo de una tarjeta muestra la entrada entera: cada línea de un texto, una imagen más grande. |
| `4-save.png` | Guarda un texto con un nombre, en su carpeta, también con formato: una firma de correo conserva sus negritas y sus fuentes. |
| `5-tray-menu.png` | Los textos guardados también están en el menú del icono junto al reloj. |
| `6-options.png` | Opciones agrupadas como en Configuración de Windows, cada una con su icono: tecleo, atajos, historial, apariencia y programa. |
| `7-guide.png` | Una guía por temas, en secciones breves que se abren y se cierran, siempre a un F1 de distancia. |
| `8-light.png` | Oscuro o claro: se elige en las opciones, o sigue a Windows. |

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

Copiez le texte, cliquez dans le champ de la fenêtre distante et laissez faire SmartClip. Il marche aussi là où Ctrl+V est bloqué : écrans de verrouillage des bureaux à distance, consoles de machines virtuelles, pages d'administration des serveurs. Vous avez le temps de cliquer dans le bon champ, et la saisie s'arrête toute seule si la fenêtre change : ce que vous saisissez ne finit jamais au mauvais endroit.

Tout ce que vous copiez, textes et images, reste dans une liste qui s'ouvre depuis n'importe quel programme avec un raccourci. Tapez quelques mots pour le retrouver, appuyez sur Entrée et vous revenez où vous étiez, l'élément déjà dans le presse-papiers, prêt à coller ou à saisir.

Les textes et les images que vous utilisez souvent, comme une signature avec son gras et ses polices, une adresse ou un code d'accès, s'enregistrent, sous un nom si vous voulez, et se rangent dans des dossiers et sous-dossiers. Ils sont aussi dans le menu de l'icône près de l'horloge, prêts à prendre sans même ouvrir la fenêtre.

Pensé pour la confidentialité :
• les textes enregistrés sont chiffrés pour votre utilisateur Windows ;
• les copies que d'autres programmes marquent comme privées ne sont pas mémorisées ;
• vous pouvez suspendre la capture et choisir combien de jours garder l'historique, ou seulement celui d'aujourd'hui ;
• pas de compte, pas de publicité, pas de connexion à Internet : vos données restent sur votre PC.

SmartClip est open source, sous licence MIT. Il parle français, anglais, espagnol et italien.
```

### Fonctionnalités du produit

```
Saisit le presse-papiers touche par touche là où coller ne marche pas : bureaux à distance, consoles de machines virtuelles, pages d'administration
Attente réglable avant la saisie, qui s'arrête si la fenêtre change
Clavier américain sur le serveur et symboles composés avec des codes Alt
Historique des textes et images copiés, avec une recherche instantanée qui trouve aussi les textes enregistrés et surligne les mots trouvés
Une barre latérale comme les apps de Windows 11, à élargir ou réduire aux icônes : historique, en haut, raccourcis, enregistrés en dossiers, les programmes où vous avez copié
Éléments en cartes compactes de la même hauteur, une ligne ou deux, le contenu d'abord et les boutons sous le pointeur, avec des onglets pour textes, code, liens et images
Liste accessible depuis n'importe quel programme avec un raccourci, et raccourcis à votre choix
Éléments gardés un moment en haut avec une flèche, et textes enregistrés ou éléments copiés collés partout avec Ctrl+Maj+1…9
Plusieurs cartes sélectionnées à la fois, avec Ctrl ou Maj, à garder en haut, déplacer dans un dossier ou supprimer ensemble
Textes et images enregistrés chiffrés, avec ou sans nom, dans des dossiers et sous-dossiers
Textes enregistrés mis en forme, comme les signatures de courriel : gras, italique, polices et tailles, collés tels quels
Textes enregistrés aussi dans le menu de l'icône, sans ouvrir la fenêtre
Aperçu complet des textes longs et des images, et le texte d'une image copié grâce à la reconnaissance de Windows
Les copies que d'autres programmes marquent comme privées ne sont pas mémorisées, et la capture se suspend en un clic
Export et import dans un fichier texte, et la base de données dans le dossier de votre choix
Démarrage avec Windows, caché dans la zone de notification
Guide intégré par sujet, en courtes sections qui s'ouvrent et se ferment
Aspect épuré en gris chauds avec un accent orange, icônes au trait, options regroupées comme les Paramètres, thème clair ou sombre, polices et tailles au choix
En français, anglais, espagnol et italien
Pas de compte, pas de publicité, pas de connexion à Internet
```

### Termes de recherche

```
presse-papiers
historique presse-papiers
gestionnaire presse-papiers
saisir presse-papiers
bureau à distance
coller bloqué
clipboard
```

### Nouveautés de cette version

```
Corrections de bugs.
```

### Légendes des captures

| File | Légende |
| --- | --- |
| `1-list.png` | L'historique de ce que vous copiez : les endroits dans la barre latérale, les types dans les onglets et le programme où chaque élément a été copié. |
| `2-typing.png` | Là où Ctrl+V ne marche pas, SmartClip saisit le presse-papiers touche par touche, après vous avoir laissé le temps de cliquer dans le bon champ. |
| `3-preview.png` | L'œil d'une carte montre l'élément en entier : chaque ligne d'un texte, une image plus grande. |
| `4-save.png` | Enregistrez un texte sous un nom, dans son dossier, même mis en forme : une signature de courriel garde son gras et ses polices. |
| `5-tray-menu.png` | Les textes enregistrés sont aussi dans le menu de l'icône près de l'horloge. |
| `6-options.png` | Des options regroupées comme dans les Paramètres de Windows, chacune avec son icône : saisie, raccourcis, historique, apparence et programme. |
| `7-guide.png` | Un guide par sujet, en courtes sections qui s'ouvrent et se ferment, toujours à portée de F1. |
| `8-light.png` | Sombre ou clair : à choisir dans les options, ou comme Windows. |

## Note per la verifica

Nell'invio, *Submission options → Notes for certification*. In inglese, perché le leggono i tester di Microsoft, e
con le etichette dell'interfaccia inglese, quella che vedono al primo avvio:

```
SmartClip is a clipboard manager that can also type the clipboard text as simulated key presses (SendInput), for sign-in screens of remote desktops and virtual machine consoles where paste is blocked. It is a .NET Framework 4 Windows Forms desktop app packaged as MSIX (runFullTrust). The user interface is in English by default, and also in Spanish, French and Italian (Options > Appearance > Language). No account, no network access, no purchases.

How to test:
1. Launch SmartClip and copy some text in any program: it appears at the top of the list (History), with the program it was copied in.
2. Press Ctrl+D in any program: the window comes up with the list on History; type to search, Enter puts the entry back on the clipboard and returns to the previous window.
3. Typing: copy a text, open Notepad, click "Type clipboard" in SmartClip, then click into Notepad within 15 seconds; after a 1-second wait the text is typed. Alternatively press Ctrl+Alt+V inside Notepad. Typing stops if the foreground window changes.
4. Saved texts: "New..." creates one; "New folder..." at the bottom of the sidebar creates a folder. Saved texts also appear in the menu of the tray icon (right-click), each folder a submenu.

Global hotkeys: Ctrl+Alt+V (type the clipboard) and Ctrl+D (open the list), both configurable in Options > Hotkeys. By default clicking another program hides the window to the notification area; to exit, right-click the tray icon > "Exit". F1 opens the built-in guide. Changing the language restarts the app.
```

## Perché serve runFullTrust

Se Partner Center chiede di giustificare la capability riservata:

```
SmartClip is a classic desktop application (.NET Framework 4, Windows Forms) packaged with MSIX. Full trust is needed to be notified of clipboard changes (AddClipboardFormatListener), to register global hotkeys (RegisterHotKey) and to send keyboard input to the window the user chooses (SendInput), which is the main purpose of the app: typing text where paste is not available, such as remote desktop sign-in screens.
```
