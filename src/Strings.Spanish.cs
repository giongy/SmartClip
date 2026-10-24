using System.Windows.Forms;

// The texts in Spanish.
static partial class Strings
{
    static void Spanish()
    {
        ShiftName = "Mayús"; DeleteKeyName = "Supr"; EnterKeyName = "Intro"; ArrowsName = "Arriba / Abajo";
        DayFormat = "dd/MM"; YearDayFormat = "dd/MM/yy";
        Quote = "«{0}»";

        TypeButton = "Teclear portapapeles";
        SavedTitle = "Guardados"; PausedSuffix = " (en pausa)";
        New = "Nuevo…"; SaveDots = "Guardar…"; EditDots = "Editar…"; RenameDots = "Renombrar…"; Delete = "Eliminar";
        Options = "Opciones"; SearchCue = "Buscar…";
        CountOf = "{0} de {1}";
        NoResults = "Sin resultados"; HistoryOffEmpty = "El historial está desactivado";
        PausedEmpty = "En pausa: lo que copias no se recuerda"; CopyHint = "Lo que copies aparecerá aquí";
        NoSaved = "Ningún texto guardado";
        NewTip = "Escribe un texto nuevo para conservar, con nombre o sin él ({0}).";
        HistoryPlace = "Historial"; PinnedPlace = "Arriba"; SourcesPlace = "Origen"; GuideItem = "Ayuda";
        AllKinds = "Todo"; TextKinds = "Texto"; CodeKinds = "Código"; LinkKinds = "Enlaces"; PictureKinds = "Imágenes";
        TextBadge = "Texto"; CodeBadge = "Código"; LinkBadge = "Enlace"; PictureBadge = "Imagen"; FormattedBadge = "Con formato";
        CopyButton = "Copiar"; ShowFull = "Vista previa"; MoreItem = "Más";
        Today = "Hoy"; Yesterday = "Ayer"; Undated = "Antes";
        NoPinned = "Nada arriba: la flecha de una entrada copiada la trae aquí por un tiempo";
        NoneOfKind = "Nada de este tipo";
        SourcesTip = "Los programas en los que copiaste: elige uno para ver solo lo que copiaste allí.";
        PinnedTip = "Las entradas copiadas mantenidas arriba por un tiempo: se van igualmente cuando vencen sus días.";
        ShortcutsPlace = "Atajos";
        ShortcutsTip = "Las entradas con un número, que las pega desde cualquier sitio: un clic en el número lo cambia o lo quita.";
        NoShortcuts = "Ningún atajo: «{0}», en el menú de una entrada, le da un número que la pega desde cualquier sitio con {1}";
        NumberTip = "{0} la pega desde cualquier sitio: clic para cambiarlo o quitarlo";
        NarrowSideTip = "Reduce la barra lateral a sus iconos. También se puede arrastrar su borde, para ensancharla o estrecharla.";
        WideSideTip = "Vuelve a mostrar los nombres en la barra lateral.";
        Error = "Error: {0}";
        NewerDatabase = "{0} fue escrito por una versión más reciente de {1}: actualiza el programa para abrirlo.";
        UnexpectedError ="Algo ha ido mal: {0}\n\nEl programa continúa. Los detalles están en {1}.";
        SqliteMissing = "{0} no puede iniciarse: guarda lo que copias y guardas con SQLite, que forma parte de Windows 10 "
            + "y 11 (winsqlite3.dll), y este Windows no lo tiene.";
        SqliteTooOld = "{0} no puede iniciarse: necesita SQLite {1} o posterior, que forma parte de Windows 10 y 11, "
            + "y este Windows tiene la versión {2}. Actualizar Windows lo soluciona.";
        PicturesFull = "El espacio para las imágenes copiadas ({0}) está lleno: las más antiguas se borran, una a una, a "
            + "medida que copias otras nuevas.";
        PickedCount = "{0} seleccionadas"; PickNone = "Quitar la selección";

        Open = "Abrir"; Exit = "Salir"; Pause = "Pausar la captura"; Resume = "Reanudar la captura";
        ShowContent = "Mostrar el contenido"; MoveTo = "Mover a"; NewFolder = "Nueva carpeta…";
        CopyPictureText = "Copiar el texto";
        KeepTop = "Mantener arriba"; Unpin = "Quitar de arriba"; OutOfFolders = "Fuera de las carpetas"; EmptyFolder = "Vacía";
        NumberMenu = "Atajo"; NoNumber = "Ninguno";
        KindMenu = "Tipo";

        TrayReady = ": texto listo en el portapapeles"; TrayEmpty = ": sin texto en el portapapeles";
        TrayPaused = ", captura en pausa";
        TrayHint = "Sigo activo aquí. Clic para volver a abrir; clic derecho y luego {0} para cerrar.";

        StartupRefused = "Windows no lo permite: actívalo desde Configuración > Aplicaciones > Inicio.";
        StartupOn = "Al iniciar sesión en Windows, {0} se abrirá solo, sin mostrar la ventana.";
        StartupOff = "{0} ya no se inicia con Windows.";
        PausedSay = "En pausa: lo que copias no se recuerda hasta que reanudes la captura.";
        ResumedSay = "La captura se ha reanudado: lo que copias vuelve a la lista.";
        Pinned = "Se queda arriba: {0}."; Unpinned = "Ya no está arriba: {0}.";
        PinnedMany = "{0} entradas se quedan arriba."; UnpinnedMany = "{0} entradas ya no están arriba.";
        DeletedMany = "{0} entradas eliminadas.";
        PickedSaved = "{0} entradas están entre los guardados, fuera de las carpetas.";
        PickedInFolder = "{0} entradas están en la carpeta «{1}».";
        KindChanged = "Marcada como {1}: {0}.";
        NumberGiven = "{0} ahora pega «{1}» dondequiera que estés.";
        NumberTaken = "«{0}» ya no tiene atajo.";
        Pasted = "Pegado en {1}: {0}.";
        PasteFocusLost = "No se ha pegado: la ventana ha cambiado. La entrada está en el portapapeles.";
        OnClipboard = "En el portapapeles: {0}.\tPégalo con Ctrl+V.";
        ReadingPicture = "Leyendo el texto de la imagen…";
        NoPictureText = "Windows no encontró texto en la imagen.";
        NoRecognition = "Windows no sabe leer imágenes en tus idiomas: instala el reconocimiento óptico de caracteres "
            + "de uno desde sus opciones, en Configuración > Hora e idioma.";
        OnClipboardOrType = "En el portapapeles: {0}.\tPégalo con Ctrl+V o tecléalo con {1}.";
        AskReplace = "«{0}» ya existe. ¿Quieres reemplazarlo?";
        SavedSay = "Guardado «{0}».";
        FolderNoSeparator = "El nombre de una carpeta no puede contener «{0}».";
        FolderExists = "La carpeta «{0}» ya existe.";
        FolderCreated = "Carpeta «{0}» creada."; FolderRenamed = "La carpeta ahora se llama «{0}».";
        FolderMoved = "La carpeta ahora es «{0}».";
        LeftFolder = "«{0}» ya no está en una carpeta."; InFolder = "«{0}» está en la carpeta «{1}».";
        AskDeleteFolder = "¿Quieres eliminar la carpeta «{0}»?";
        DeleteFolderTop = "\n\nLo que contiene no se elimina: queda entre los guardados, fuera de las carpetas.";
        DeleteFolderUp = "\n\nLo que contiene no se elimina: pasa a la carpeta «{0}».";
        AskDelete = "¿Quieres eliminar «{0}»?";
        AskDeleteMany = "¿Quieres eliminar {0} entradas?"; DeleteManySaved = "\n\nEntre ellas, textos guardados: {0}.";
        Yes = "Sí"; No = "No"; Ok = "Aceptar";

        Cancel = "Cancelar";
        NoText = "No hay texto en el portapapeles: copia antes el texto.";
        Cancelled = "Cancelado.";
        CapsLock = "Bloq Mayús está activado: desactívalo y vuelve a intentarlo.";
        TypingInto = "Tecleando en {0}…";
        AskUnusual = "El portapapeles contiene {0}: más largo de lo que se suele teclear.\n\n¿Quieres teclearlo de todos modos?";
        OnLines = " en {0} líneas";
        Countdown = "Tecleo dentro de {0} s en {1}. Pulsa Cancelar para detener.";
        TimeUp = "Se acabó el tiempo: no has elegido la ventana en la que teclear.";
        ClickNow = "Haz clic ahora en el campo de la ventana remota…";
        Untitled = "una ventana sin título";
        KeysHeld = "Suelta las teclas que mantienes pulsadas y vuelve a intentarlo.";
        Blocked = "Windows ha bloqueado el envío de teclas. Si el programa de escritorio remoto se ejecuta como "
            + "administrador, inicia {0} de la misma forma.";
        StoppedAfter = "Detenido tras {0}."; FocusLost = "Detenido tras {0}: la ventana activa ha cambiado.";
        DoneOne = "Hecho: 1 carácter tecleado."; DoneMany = "Hecho: {0} caracteres tecleados.";
        AltCoded = " {0} con códigos Alt.";
        UnicodeOne = " Atención: 1 carácter sin tecla en el teclado, enviado como Unicode";
        UnicodeMany = " Atención: {0} caracteres sin tecla en el teclado, enviados como Unicode";
        UnicodeHint = ": si el acceso falla, prueba la opción de códigos Alt.";
        Character = "carácter"; Characters = "caracteres";

        HotkeyTip = "{0} teclea enseguida el portapapeles en la ventana activa, sin espera.\n"
            + "No funciona si el escritorio remoto está a pantalla completa y captura todos\n"
            + "los atajos: en ese caso usa el botón «{1}».";
        PickTip = "{0} trae al frente esta ventana con la lista, desde cualquier programa.\n"
            + "Elegida una entrada, el control vuelve a la ventana de la que partiste: allí la pegas\n"
            + "con Ctrl+V o la tecleas con {1}.\n"
            + "Mientras está activo, {0} ya no llega a los demás programas.";
        ChangeKeysNote = "\nEl botón de la derecha muestra sus teclas: púlsalo para darle otras.";
        KeysTip = "Pulsa aquí y luego las teclas nuevas: Ctrl o Alt, también con {0}, junto con una letra,\n"
            + "un número o una tecla de función. Esc deja las actuales, {1} vuelve a las iniciales.\n"
            + "Con Ctrl+Alt evita las letras que con AltGr escriben un símbolo (E, para el euro): el\n"
            + "atajo saltaría al escribirlo.";
        LeaveTip = "Cuando haces clic en otro programa la ventana vuelve al área de notificación, como\n"
            + "al cerrarla: la vuelves a abrir con el icono o con {0}. Desactivada, sigue abierta detrás de las demás.\n"
            + "Solo vale con la opción de arriba activa. No ocurre mientras espera o teclea, ni mientras\n"
            + "escribes un texto guardado o respondes a una pregunta: mientras tanto puedes copiar en otro sitio.";
        TypeTip = "Teclea el texto del portapapeles tecla por tecla, donde pegar no funciona:\n"
            + "copia el texto, pulsa aquí y haz clic en el campo de la ventana remota.\n"
            + "O pulsa {0} directamente en el campo.";
        SearchTip = "Escribe para buscar entre las entradas del lugar: palabras en cualquier orden, sin importar\n"
            + "mayúsculas ni acentos. En «" + HistoryPlace + "» también los textos guardados, después de lo copiado.\n"
            + "Las flechas recorren las entradas.\n"
            + "«" + CopyButton + "», {0} o un doble clic devuelven la entrada al portapapeles: luego la pegas\n"
            + "con Ctrl+V o la tecleas con {1}. Con «" + CopyButton + "» la ventana sigue abierta, con {0} o un doble\n"
            + "clic vuelves a donde estabas. Un clic solo selecciona; el clic derecho abre el menú de la entrada.\n"
            + "El ojo a la derecha de una tarjeta la muestra entera.\n"
            + "La flecha a la derecha de una entrada copiada ({2}) la mantiene un tiempo arriba de la lista:\n"
            + "se va igualmente cuando vencen sus días. Lo que deba quedarse, guárdalo.\n"
            + "Los textos guardados no vencen nunca y están cifrados para tu usuario; con un nombre,\n"
            + "solo se ve el nombre. Sus carpetas están en la barra lateral, con «" + NewFolder + "» ({3})\n"
            + "abajo: un texto entra arrastrándolo sobre una, o con «" + MoveTo + "».\n"
            + "«{5}», en el menú del clic derecho, da un número a la entrada: luego {4}\n"
            + "la pegan dondequiera que estés, sin abrir esta ventana.";
        PressKeys = "Pulsa las teclas…";
        PressKeysStatus = "Pulsa las teclas nuevas: Ctrl o Alt con una letra, un número o una tecla de función. "
            + "Esc deja las actuales, {0} vuelve a las iniciales.";
        KeysUnusable = "{0} no sirve: hace falta Ctrl o Alt con una letra, un número o una tecla de función.";
        KeysTaken = "{0} ya la usa el programa: elige otra.";
        TypeNowWith = "Para teclear el portapapeles ahora pulsa {0}."; OpenNowWith = "Para abrir la lista ahora pulsa {0}.";
        HotkeyBusyType = "{0} ya la usa otro programa: dale otras teclas en las opciones, o usa el botón.";
        HotkeyBusyPick = "{0} ya la usa otro programa: dale otras teclas en las opciones, o abre la ventana desde el icono.";
        QuickTip = "{0} pegan la entrada con ese número en la ventana en la que estás trabajando,\n"
            + "sin abrir esta: va al portapapeles y Ctrl+V sigue solo.\n"
            + "Los números se dan con «{1}», en el menú del clic derecho de una entrada.\n"
            + "Solo se toman las teclas de los números en uso: esas ya no llegan a los demás programas.";
        QuickKeysTip = "Pulsa aquí y luego las teclas que hay que mantener, junto con una cifra cualquiera: Ctrl o\n"
            + "Alt, también con {0}. La cifra no cuenta: del 1 al 9, elige la entrada.\n"
            + "Esc deja las actuales, {1} vuelve a las iniciales ({2}).\n"
            + "Ctrl+Alt es AltGr: en algunos teclados escribe símbolos con las cifras.";
        PressQuickStatus = "Pulsa las teclas que hay que mantener con una cifra cualquiera, como {0}. "
            + "Esc deja las actuales, {1} vuelve a las iniciales.";
        QuickUnusable = "{0} no sirve: mantén Ctrl o Alt, también con Mayús, y pulsa una cifra.";
        QuickNowWith = "Las entradas numeradas ahora se pegan con {0}.";
        QuickBusy = "Ya los usa otro programa: {0}. Esos números no pegarán: elige otras teclas en las opciones, "
            + "u otros números.";

        TypingTitle = "Escritura"; ProgramTitle = "Programa"; HotkeysTitle = "Atajos"; HistoryTitle = "Historial";
        AppearanceTitle = "Apariencia";
        DelayOption = "Espera antes de teclear"; DelayNote = "Segundos para hacer clic en el campo correcto";
        UsOption = "El servidor usa teclado inglés (EE. UU.)"; UsNote = "Solo si los símbolos llegan mal";
        EnterOption = "Pulsar Intro al final"; EnterNote = "Confirma enseguida el inicio de sesión";
        ClearOption = "Vaciar el portapapeles tras teclear"; ClearNote = "Cuando el tecleo ha salido bien";
        SlowOption = "Tecleo lento"; SlowNote = "Si faltan caracteres o llegan desordenados";
        AltCodeOption = "Códigos Alt para símbolos sin tecla"; AltCodeNote = "Solo para servidores con Windows";
        LanguageOption = "Idioma (Language)"; LanguageNote = "El programa se reinicia en ese idioma";
        ThemeOption = "Tema"; ThemeNote = "Colores claros u oscuros";
        ThemeSystem = "Como Windows"; ThemeLight = "Claro"; ThemeDark = "Oscuro";
        FaceOption = "Fuente de la interfaz"; FaceNote = "Botones, opciones, menús: el programa se reinicia";
        FontOption = "Fuente de las entradas"; FontNote = "Lo que copias y guardas: cambia enseguida";
        LinesOption = "Altura de las tarjetas"; LinesNote = "En líneas de texto: con una, más entradas a la vista";
        OneLine = "1 línea"; TwoLines = "2 líneas";
        RestOption = "Vista previa con el puntero quieto"; RestNote = "En una tarjeta cortada, sin el ojo";
        RestTip = "Activada, el puntero quieto sobre una tarjeta que corta su entrada la muestra entera.\n"
            + "Desactivada, solo la muestra el ojo a la derecha de la tarjeta, o su menú.";
        Pixels = "{0} px";
        TrayOption = "Seguir en el área de notificación"; TrayNote = "Al cerrar la ventana el programa sigue activo";
        LeaveOption = "Ocultar al cambiar a otro programa"; LeaveNote = "Vuelve al área de notificación";
        StartOption = "Iniciar con Windows"; StartNote = "Oculto en el área de notificación";
        BackupOption = "Exportar e importar"; BackupNote = "Tus textos en un archivo que abre el Bloc de notas";
        ExportButton = "Exportar…"; ImportButton = "Importar…";
        AboutNote = "Software libre con licencia MIT";
        TypeNote = "Teclea enseguida en la ventana activa";
        PickOption = "Abrir la lista"; PickNote = "Trae esta ventana al frente desde cualquier programa";
        WhereLeftOption = "Abrir donde la dejaste"; WhereLeftNote = "Si no, la lista se abre en " + HistoryPlace;
        WhereLeftTip = "Desactivada, el atajo de la lista la abre siempre en " + HistoryPlace + ", de todos los tipos, con la búsqueda vacía.\n"
            + "Activada, muestra el lugar y el tipo en que la dejaste.";
        QuickOption = "Pegar por número"; QuickNote = "Pega las entradas que tienen un atajo";
        HistoryOption = "Recordar textos e imágenes copiados"; HistoryNote = "Lo que copias mientras el programa está abierto";
        KeepOption = "Días que se guardan"; KeepNote = "Con 0, solo lo que copiaste hoy";
        FolderLink = "Abrir la carpeta de los datos";
        DataFolderOption = "Carpeta de las entradas copiadas y guardadas";
        ChangeFolderButton = "Cambiar…"; DefaultFolderButton = "Predeterminada";
        DatabaseSize = "Ocupa {0}; imágenes copiadas: {1} de {2}.";
        ChooseDataFolder = "La carpeta en la que {0} guarda lo que copias y guardas.";
        AskUseExisting = "En {0} ya hay una lista de {1}.\n\n¿Quieres usar esa? La tuya se queda donde está, tal como está.";
        AskSyncedFolder = "{0} parece una carpeta sincronizada en línea o en red.\n\nLa sincronización puede bloquear o "
            + "estropear la base de datos, y los textos guardados no se leen en otro PC. ¿Quieres usarla igualmente?";
        ListMovedTo = "Ahora la lista está en {0}.";
        ListMoveFailed = "No se ha podido mover la lista, que se queda donde estaba: {0}";
        FolderUnavailable = "No se puede acceder a la carpeta de la lista, {0}: lo que copies y guardes ahora se pierde al "
            + "cerrar el programa. En las opciones, en {1}, elige otra carpeta o la predeterminada.";
        SpareOption = "Limpiar el historial"; SpareNote = "Salvo los últimos días escritos al lado; 0: ninguno";
        CleanButton = "Limpiar ahora…";
        DelayTip = "Cuántos segundos debe quedarse en primer plano la ventana de destino\n"
            + "antes de que empiece el tecleo: es el tiempo para hacer clic en el campo\ncorrecto, o para cancelar.";
        UsTip = "Déjala desactivada para los escritorios remotos normales: vale el teclado de este PC.\n"
            + "Actívala si los símbolos llegan mal porque el servidor usa el teclado\n"
            + "estadounidense (pasa con las consolas VMware, iLO, iDRAC y algunas pantallas\nde inicio de sesión).";
        EnterTip = "Pulsa Intro después del último carácter, para confirmar el acceso enseguida.";
        ClearTip = "Borra el texto tecleado del portapapeles cuando el tecleo ha salido bien.\n"
            + "El historial del portapapeles de Windows (Win+V), si está activo, no se toca.";
        SlowTip = "Actívala si en el servidor faltan caracteres o llegan desordenados.";
        AltCodeTip = "Los símbolos que este teclado no tiene (por ejemplo ~ y ` en el italiano) se envían\n"
            + "normalmente como caracteres Unicode, que no todos los programas de escritorio remoto\n"
            + "aceptan. Con esta opción se componen con códigos Alt en el teclado numérico:\n"
            + "solo funciona si el servidor es Windows.";
        LanguageTip = "El idioma del programa. Al cambiarlo, {0} se reinicia y vuelve a abrirse enseguida.";
        ThemeTip = "Colores claros u oscuros. «{0}» sigue el modo que Windows fija para las aplicaciones,\n"
            + "incluso cuando cambia con el programa abierto.";
        FaceTip = "La fuente del propio programa: botones, etiquetas, opciones, la guía, los menús y las preguntas.\n"
            + "Lo que has copiado y guardado tiene la suya, más abajo. Cambiarla reinicia {0}.";
        FaceSizeTip = "Lo grande que se escribe el propio programa, en píxeles; {0} por defecto. Las ventanas crecen\n"
            + "con el texto. Lo que has copiado y guardado tiene su tamaño, más abajo. Cambiarlo reinicia {1}.";
        FontTip = "La fuente de lo que has copiado y guardado: las filas de las listas, la entrada vista entera\n"
            + "y el contenido de un texto guardado. El resto del programa mantiene la fuente de la interfaz. Cambia enseguida.";
        SizeTip = "Lo grande que se escribe lo que has copiado y guardado, en píxeles; {0} es el texto de Windows 11.\n"
            + "El resto del programa mantiene el tamaño de la interfaz. Cambia enseguida.";
        LinesTip = "Lo alta que es cada tarjeta de la lista, en líneas de texto, tenga lo que tenga: textos, imágenes, nombres.\n"
            + "Con una línea las líneas de un texto van seguidas, con un {0} tenue entre ellas; con dos se quedan como están.\n"
            + "Lo que una tarjeta corta lo muestra entero el ojo a su derecha. Cambia enseguida.";
        HistoryTip = "La lista ofrece los textos e imágenes copiados mientras el programa está abierto.\n"
            + "Las copias que otros programas marcan como privadas no se recuerdan.\n"
            + "Desactívala si copias textos privados desde programas que no lo hacen: se leerían en\n"
            + "la lista. Al desactivarla también se borra el historial en disco.";
        KeepTip = "Cuántos días se queda en el historial lo que copias, también tras cerrar,\n"
            + "contando hoy como el primero: con 0 solo queda lo que copiaste hoy.\n"
            + "También las entradas arriba se van cuando vencen sus días; los textos guardados nunca.\n"
            + "El historial se guarda en disco sin cifrar. La lista muestra las {0} más recientes:\n"
            + "las demás se encuentran buscando.";
        DataFolderTip = "Dónde está la base de datos con lo que copias y guardas, junto al registro de errores:\n{0}\n"
            + "La carpeta predeterminada es {1}. «Cambiar…» mueve la base de datos a la carpeta que elijas,\n"
            + "o usa la que ya está allí. No sirve para compartir la lista entre PC: los textos guardados\n"
            + "solo los lee este usuario de Windows en este PC, y dos programas que usan el mismo\n"
            + "archivo a la vez pueden estropearlo.";
        CleanTip = "Borra enseguida del historial lo que copiaste antes de los últimos días\n"
            + "escritos arriba: con 1 solo queda hoy, con 0 no queda nada.\n"
            + "Pide confirmación antes. También se van las entradas arriba; los textos guardados\n"
            + "no se tocan, ni el portapapeles. Los días que se guardan siguen como estaban.";
        TrayTip = "Activada: al cerrar o minimizar la ventana el programa sigue funcionando como\n"
            + "icono junto al reloj, y los atajos siguen funcionando.\n"
            + "Clic en el icono para volver a abrir la ventana; clic derecho y luego {0} para cerrar.\n"
            + "Desactivada: ventana normal, que al cerrarla termina el programa.";
        StartTip = "Inicia {0} al entrar en Windows, ya oculto en el área de notificación\n"
            + "(minimizado, si la opción de arriba está desactivada).\n"
            + "Vale para este ejecutable: si lo mueves, la entrada se actualiza sola al primer inicio desde allí.\n"
            + "Es la misma entrada del Administrador de tareas > Aplicaciones de arranque: desactivada allí,\n"
            + "aparece desactivada aquí.";
        ExportTip = "Escribe en un archivo de texto, que se abre con el Bloc de notas, los textos guardados\n"
            + "con sus carpetas; si lo pides, también lo copiado. Las imágenes no se incluyen.\n"
            + "En el archivo los textos van sin cifrar, también los privados: guárdalo en un lugar seguro.";
        ImportTip = "Lee un archivo escrito por «Exportar», también en otro PC, y añade lo que contiene\n"
            + "a lo que ya hay. Si un texto guardado tiene el nombre de uno existente, pregunta cuál conservar.\n"
            + "Lo copiado que la lista ya tiene no se repite.";
        OnlySaved = "Solo los textos guardados…"; Everything = "Todo, también lo copiado…";
        TextFiles = "Archivos de texto";
        AskExport = "El archivo contendrá los textos sin cifrar, también los privados: quien lo abra podrá leerlos.\n\n"
            + "¿Quieres continuar?";
        SavedOne = "texto guardado"; SavedMany = "textos guardados"; CopiedOne = "copiado"; CopiedMany = "copiados";
        Exported = "Exportados: {0}."; ExportedAll = "Exportados: {0}, {1}.";
        PicturesLeftOut = " Las imágenes ({0}) no se exportan.";
        ClashOne = "Un texto guardado del archivo tiene el nombre de uno que ya existe.";
        ClashMany = "{0} textos guardados del archivo tienen el nombre de uno que ya existe.";
        ClashChoice = "\n\nSí: vale el del archivo.\nNo: se queda el que hay.";
        NothingImported = "En el archivo no había nada que importar.";
        Imported = "Importados: {0}."; ImportedAll = "Importados: {0}, {1}.";
        KeptAsWere = " Dejados como estaban: {0}.";
        HistoryOffImport = " El historial está desactivado: lo copiado no se ha importado.";
        CleanEverything = "todo lo que has copiado"; CleanBeforeToday = "lo que copiaste antes de hoy";
        CleanBeforeDays = "lo que copiaste antes de los últimos {0} días (hoy incluido)";
        AskClean = "¿Quieres eliminar {0}?\n\nLos textos guardados se quedan; también se van las entradas arriba.";
        NothingToClean = "Nada que eliminar en el historial.";
        CleanedOne = "Eliminada 1 entrada del historial."; CleanedMany = "Eliminadas {0} entradas del historial.";
        NoHistoryFolder = "La carpeta de los datos todavía no existe: no hay nada en disco.";
        DatabaseUnavailable = "La lista no se puede guardar en disco ({0}): lo que copies y guardes ahora se pierde al "
            + "cerrar el programa.";

        NewTextTitle = "Nuevo texto"; SaveCopiedTitle = "Guardar el texto copiado"; EditSavedTitle = "Editar el texto guardado";
        SavePictureTitle = "Guardar la imagen copiada"; EditPictureTitle = "Editar la imagen guardada";
        PictureHintEmpty = "Sin nombre, la lista mostrará solo la imagen.";
        PictureHintNamed = "La lista mostrará la imagen con «{0}» al lado.";
        NameLabel = "Nombre (opcional):"; ContentLabel = "Contenido:";
        NameHintEmpty = "Sin nombre, la lista mostrará el contenido.";
        NameHintNamed = "La lista mostrará «{0}»; el contenido queda oculto, ideal para un texto privado.";
        BoldTip = "Negrita (Ctrl+B)"; ItalicTip = "Cursiva (Ctrl+I)"; UnderlineTip = "Subrayado (Ctrl+U)";
        TypefaceTip = "Fuente"; PointsTip = "Tamaño"; PlainTip = "Quitar el formato: de la selección, o de todo el texto";
        DefaultLook = "Predeterminado";
        FolderOf = "Carpeta: "; NoFolder = "Sin carpeta"; Inside = "Dentro de: ";
        NewFolderTitle = "Nueva carpeta"; RenameFolderTitle = "Renombrar la carpeta"; FolderNameLabel = "Nombre de la carpeta:";
        Save = "Guardar";

        PictureLabel = "Imagen {0}×{1}";
        PictureToday = "Imagen {0}×{1}, copiada a las {2}"; PictureOn = "Imagen {0}×{1}, copiada el {2} a las {3}";
        MoreOne = "… y un carácter más"; MoreMany = "… y {0} caracteres más";

        ExportHeader = "{0} {1}: exportación del {2}";
        ExportClearWarning = "Atención: aquí los textos van sin cifrar, también los privados.";
        ExportEditNote = "El texto de cada entrada está en las líneas que empiezan con una tabulación: si modificas el "
            + "archivo, déjala en su sitio.";

        HelpTitle = "Guía de {0}"; OptionsTitle = "Opciones de {0}"; Close = "Cerrar";
        ExpandAll = "Abrir todo"; CollapseAll = "Cerrar todo";
        Guide = SpanishGuide;
    }

    static string[] SpanishGuide()
    {
        string pick = MainForm.PickName, type = MainForm.HotkeyName, app = Program.AppName;
        return new string[]
        {
            "# Para qué sirve",
            app + " guarda lo que copias y los textos que eliges conservar, para devolverlos al portapapeles cuando "
                + "hacen falta. También sabe teclear el portapapeles como pulsaciones de teclas reales: sirve para "
                + "introducir un texto donde pegar no funciona, por ejemplo en las pantallas de bloqueo de los "
                + "escritorios remotos.",

            "# La lista",
            "A la izquierda está la barra lateral, a la derecha las tarjetas de las entradas del lugar elegido en ella.",
            "## Los lugares de la barra lateral",
            "- «" + HistoryPlace + "»: lo que copiaste, de lo más nuevo a lo más antiguo, cada día bajo su nombre; las "
                + "entradas mantenidas arriba van antes que las demás. Al desplazarte, el nombre del día de la primera tarjeta "
                + "queda encima de las tarjetas.",
            "- «" + PinnedPlace + "»: solo las entradas copiadas mantenidas arriba.",
            "- «" + ShortcutsPlace + "»: las entradas con un número, copiadas y guardadas, por número. Un clic en el número de una "
                + "tarjeta lo cambia o lo quita, aquí y en cualquier otro lugar.",
            "- «" + SavedTitle + "»: los textos guardados, por nombre. Debajo están sus carpetas.",
            "- «" + SourcesPlace + "»: los programas en los que copiaste, primero el de la copia más reciente, cada uno con su icono y cuántas entradas vienen "
                + "de él. Un clic en uno muestra solo lo que copiaste allí; un clic en «" + SourcesPlace + "» abre o cierra la lista. "
                + "Los programas de un servidor Citrix toman todos el nombre de Citrix en este PC, pero cada uno conserva su icono.",
            "El botón en lo alto de la barra lateral la reduce a sus iconos, y le devuelve los nombres: si dejas el puntero "
                + "sobre un icono, ves su nombre, y cada carpeta muestra la inicial de su nombre. Arrastrando el borde entre la barra y las tarjetas la ensanchas o la "
                + "estrechas; más estrecha de lo que puede, se reduce a los iconos.",
            "## Tipos de entrada",
            "Las pestañas sobre las tarjetas (" + AllKinds + ", " + TextKinds + ", " + CodeKinds + ", " + LinkKinds + ", "
                + PictureKinds + ") muestran un solo tipo de entrada. «" + CodeKinds + "» es una suposición hecha por el "
                + "aspecto de las líneas: de vez en cuando acaba ahí un texto, o un trozo de código entre los textos. "
                + "Cuando se equivoca, «" + KindMenu + "», en el menú de un texto, dice qué es: copiado de nuevo o "
                + "guardado, se queda así.",
            "## Las tarjetas",
            "Cada tarjeta muestra primero la entrada; a la derecha, en pequeño y tenue, el programa en el que se copió, con su icono (o "
                + "la carpeta de un texto guardado), la hora de la copia y qué es, si no es un texto simple. Con el puntero sobre "
                + "la tarjeta, en su lugar aparecen pequeños signos: la mantienen arriba, la muestran entera, la copian, la "
                + "guardan o la editan, la eliminan y abren su menú; dejando el puntero sobre uno, dice qué hace.",
            "## La búsqueda",
            "Escribe en la casilla de arriba para buscar entre las entradas del lugar: las palabras valen en cualquier "
                + "orden, sin importar mayúsculas ni acentos. En «" + HistoryPlace + "» aparecen también los textos guardados "
                + "que coinciden, después de lo copiado, bajo «" + SavedTitle + "». Las palabras encontradas se resaltan "
                + "en las tarjetas; encontradas muy adelante en un texto largo, la tarjeta muestra esa parte.",
            "La lista muestra las " + MainForm.MaxShown + " entradas copiadas más recientes: las demás se "
                + "encuentran buscando.",
            "## Usar una entrada",
            "- Un clic en una tarjeta la selecciona. El signo de copiar («" + CopyButton + "») pone la entrada en el portapapeles y deja la "
                + "ventana abierta, para que aún puedas guardarla o eliminarla.",
            "- " + EnterKeyName + " o un doble clic la ponen en el portapapeles y, si habías abierto la lista con "
                + pick + ", te devuelven a la ventana en la que estabas trabajando.",
            "- Luego la pegas con Ctrl+V, o la tecleas con " + type + ".",
            "- El ojo («" + ShowFull + "», también en el menú) muestra la entrada completa en un recuadro: el texto con todas sus "
                + "líneas, la imagen más grande. Se queda unos segundos, vaya donde vaya el puntero. Con «" + RestOption + "» "
                + "activada, en las opciones, dejar el puntero sobre una tarjeta que corta su texto hace lo mismo, hasta que "
                + "mueves el puntero.",
            "- Los tres puntos de la derecha, o el clic derecho, abren el menú de la entrada, sin tocar el portapapeles.",
            "## Varias tarjetas a la vez",
            "Ctrl+clic añade una tarjeta a la selección o la quita, " + ShiftName + "+clic selecciona todas las tarjetas hasta "
                + "esa (también " + ShiftName + " con las flechas), " + Key(Keys.Control | MainForm.AllKey) + " las selecciona todas. "
                + "Con varias seleccionadas, una barra en el lugar de las pestañas dice cuántas son y ofrece «" + KeepTop + "», «" + MoveTo
                + "» y «" + Delete + "», que también están en el menú del clic derecho y en sus teclas. «" + MoveTo + "» una carpeta, "
                + "o «" + SavedTitle + "», guarda allí las entradas copiadas. " + EscapeKeyName + ", o la cruz al final de la barra, "
                + "vuelve a una sola tarjeta.",
            "## El texto de una imagen",
            "«" + CopyPictureText + "», en el menú de una imagen, pone en el portapapeles el texto "
                + "que Windows lee en ella, aquí en el PC, sin internet: entra en la lista como todo lo que copias (el de "
                + "una imagen guardada es privado, como un texto guardado). El texto pequeño puede salir con algún error: "
                + "revísalo antes.",
            "## Mantener una entrada arriba",
            "La flecha de la derecha de una entrada copiada (" + Key(Keys.Control | MainForm.PinKey) + ") la "
                + "mantiene arriba un tiempo: está en color mientras lo hace, y un clic la enciende o la apaga. La entrada "
                + "se va igualmente cuando vencen los días del historial: lo que deba quedarse, guárdalo. Copiar de nuevo "
                + "el mismo texto lo devuelve arriba con la hora nueva, flecha y número incluidos.",
            "## Los números",
            "«" + NumberMenu + "», en el menú, da a un texto guardado o a una entrada copiada un "
                + "número del 1 al " + MainForm.MaxNumber + ": luego, desde cualquier programa, " + MainForm.NumbersName
                + " la pegan enseguida en la ventana en la que estás trabajando, sin abrir esta (la entrada va al "
                + "portapapeles y Ctrl+V sigue solo). El número aparece en un pequeño recuadro a la derecha de la tarjeta, "
                + "y junto al texto guardado en el menú del icono. Una entrada copiada conserva su número hasta que "
                + "vencen sus días. Cada número es de una sola entrada: si se lo das a otra, pasa a ella.",
            "## La pausa",
            "Para no recordar lo que vas a copiar, «" + Pause + "» está abajo en la barra lateral y en "
                + "el menú del icono junto al reloj: el título dice «en pausa» hasta que eliges «" + Resume
                + "». En el siguiente inicio la captura vuelve a estar activa.",

            "# Textos guardados",
            "Son lo que quieres tener siempre a mano: una firma, una dirección, un código de acceso. No vencen nunca. El "
                + "nombre es opcional: con nombre, la lista muestra solo el nombre y el contenido queda oculto, ideal "
                + "para un texto privado; sin él, la lista muestra el contenido, como para una segunda dirección de correo.",
            "## Escribirlos y cambiarlos",
            "- «" + New + "» (" + Key(Keys.Control | MainForm.NewKey) + ") escribe un texto desde cero.",
            "- «" + SaveDots + "» (" + Key(Keys.Control | MainForm.SaveKey) + "), en una entrada copiada, la conserva, "
                + "con nombre o sin él; antes de guardar puedes retocar su contenido.",
            "- «" + EditDots + "» (" + Key(Keys.Control | MainForm.SaveKey) + "), en una entrada guardada, cambia su "
                + "nombre o su contenido.",
            "- «" + Delete + "» (" + Key(Keys.Control | MainForm.DeleteKey) + ") borra el texto guardado, o quita de la "
                + "lista la entrada copiada.",
            "## Verlos y usarlos",
            "- El ojo de un texto guardado con nombre («" + ShowContent + "», también en su menú) muestra su contenido en un "
                + "recuadro unos segundos: la lista solo muestra su nombre, y pasar el puntero por encima nunca basta.",
            "- Los textos guardados también están en el menú del icono junto al reloj: elegir uno lo devuelve al "
                + "portapapeles sin abrir la ventana.",
            "## A salvo",
            "Los textos guardados no vencen nunca. Están en la base de datos del programa, cifrados para tu usuario: "
                + "otro usuario del mismo PC no puede leerlos. Cuando devuelves uno al portapapeles se marca como privado, para que el "
                + "historial de Windows (Win+V) y los demás gestores del portapapeles lo ignoren. También se puede "
                + "guardar una imagen copiada: siempre muestra su miniatura, con su nombre al lado si lo tiene.",
            "## Textos con formato",
            "Un texto guardado puede tener formato, como una firma de correo: negrita, cursiva, subrayado, fuentes y tamaños.",
            "- Copia la firma de un correo o de un documento y guárdala: conserva su formato. El historial guarda "
                + "texto sin formato, así que una entrada copiada lo tiene todavía solo mientras está entre las últimas "
                + Rich.KeptCopies + " copias desde que se abrió el programa.",
            "- En la ventana de un texto, los botones sobre el contenido ponen la selección en negrita (Ctrl+B), "
                + "cursiva (Ctrl+I) o subrayado (Ctrl+U), le dan una fuente y un tamaño, o le quitan el formato (sin "
                + "selección, a todo el texto). Lo que pegas ahí conserva su formato.",
            "- Elegido, un texto con formato va al portapapeles con formato y sin él: Ctrl+V en un correo lo pega "
                + "con formato, donde el formato no cuenta llega el texto sin formato, y " + type + " escribe el texto sin formato.",
            "- Mientras no lo cambies queda exactamente como se copió, enlaces e imágenes incluidos. Una vez "
                + "cambiado conserva solo lo que conocen los botones: enlaces, imágenes, alineación y listas se pierden.",

            "# Carpetas",
            "Cuando los textos guardados son muchos puedes reunirlos en carpetas, y las carpetas en otras carpetas. Están "
                + "en la barra lateral, bajo «" + SavedTitle + "»: un clic en una carpeta muestra los textos que contiene "
                + "directamente, no los de las carpetas que tiene dentro.",
            "## Crearlas y abrirlas",
            "- «" + NewFolder + "» está abajo en la barra lateral y en el menú del clic derecho de una carpeta "
                + "(" + Key(Keys.Control | Keys.Shift | MainForm.NewKey) + "): nace dentro de la carpeta "
                + "mostrada, pero en su ventana puedes elegir dónde ponerla.",
            "- La flecha del extremo derecho de una carpeta que contiene otras la abre y la cierra; la de «" + SavedTitle
                + "» las muestra o las oculta todas. El número dice cuántos textos hay en la carpeta misma, no en las "
                + "que tiene dentro. Abiertas o cerradas, las encuentras como las dejaste.",
            "## Mover textos y carpetas",
            "- La tarjeta de un texto guardado, o una carpeta de la barra con todo lo que contiene, se arrastra sobre una "
                + "carpeta de la barra; soltada sobre «" + SavedTitle + "», sale de las carpetas.",
            "- «" + MoveTo + "», en el menú de un texto guardado o de una carpeta, hace lo mismo. La "
                + "misma elección está en la ventana donde lo escribes, a la izquierda de los botones.",
            "## Renombrarlas y eliminarlas",
            "- En el menú del clic derecho de una carpeta, «" + RenameDots + "» cambia su nombre o su lugar y «" + Delete
                + "» quita solo la carpeta: lo que contenía, textos y carpetas, sube a la carpeta que la contenía, o queda "
                + "entre los guardados fuera de las carpetas.",
            "- El nombre de una carpeta no puede contener «" + Texts.Separator + "».",
            "## En otros lugares",
            "- En el menú del icono junto al reloj cada carpeta es un submenú con sus textos.",
            "- El nombre de una carpeta encuentra sus textos en la búsqueda.",

            "# Teclear donde pegar no funciona",
            "Donde Ctrl+V no funciona, " + app + " escribe el texto del portapapeles tecla por tecla, como harías tú "
                + "en el teclado.",
            "## Con el botón",
            "- Copia el texto.",
            "- Pulsa «" + TypeButton + "».",
            "- Haz clic en el campo de la ventana remota: tienes " + MainForm.ArmedSeconds + " segundos para hacerlo.",
            "- Cuando esa ventana ha estado en primer plano el tiempo de la espera, empieza el tecleo.",
            "## Con las teclas",
            "O pulsa " + type + " directamente en el campo: teclea enseguida, sin espera.",
            "## Nunca en el sitio equivocado",
            "Para no enviar un texto privado al sitio equivocado:",
            "- el tecleo se detiene solo si la ventana de destino pierde el primer plano;",
            "- durante la espera y el tecleo el mismo botón se convierte en «" + Cancel + "»;",
            "- el escritorio, la barra de tareas y el menú Inicio nunca son un destino;",
            "- si el portapapeles contiene más de " + MainForm.UnusualLength + " caracteres o varias líneas, se pide "
                + "confirmación;",
            "- con Bloq Mayús activado no se teclea: hay que desactivarlo antes.",

            "# Teclas",
            "Los tres primeros funcionan en todas partes; los demás, en la lista. Mientras está activo, " + pick
                + " ya no llega a los demás programas, ni tampoco las teclas de los números que tiene alguna entrada.",
            "## En todas partes",
            pick + "\tAbre la lista en " + HistoryPlace + ", desde cualquier programa.",
            type + "\tTeclea enseguida el portapapeles en la ventana activa.",
            MainForm.NumbersName + "\tPega en la ventana activa la entrada con ese número.",
            "## En la lista",
            "F1\tAbre esta guía.",
            ArrowsName + "\tRecorre las entradas, o los lugares de la barra lateral.",
            Key(Keys.Control | MainForm.AllKey) + "\tSelecciona todas las tarjetas; Ctrl o " + ShiftName + " con un clic, varias.",
            EnterKeyName + "\tElige la entrada y vuelve a la ventana de partida.",
            EscapeKeyName + "\tVuelve a la ventana de partida sin elegir; con varias tarjetas seleccionadas, a una sola.",
            Key(Keys.Control | MainForm.SearchKey) + "\tLleva el cursor al cuadro de búsqueda.",
            Key(Keys.Control | MainForm.NewKey) + "\tNuevo texto guardado.",
            Key(Keys.Control | Keys.Shift | MainForm.NewKey) + "\tNueva carpeta.",
            Key(Keys.Control | MainForm.SaveKey) + "\tGuarda la entrada copiada, o edita la guardada.",
            Key(Keys.Control | MainForm.DeleteKey) + "\tElimina la entrada seleccionada.",
            Key(Keys.Control | MainForm.PinKey) + "\tMantiene arriba la entrada copiada, o la suelta.",
            "## Cambiar los atajos",
            "En las opciones, bajo «" + HotkeysTitle + "», los tres primeros se desactivan y se cambian: el botón junto "
                + "a cada uno muestra sus teclas, y al pulsarlo espera las nuevas. Para los números se pulsan las "
                + "teclas que hay que mantener junto con una cifra cualquiera; si otro programa ya tiene alguna, dice cuáles.",
            "- Hace falta Ctrl o Alt, también con " + ShiftName + ", junto con una letra, un número o una tecla de función.",
            "- Esc deja las teclas actuales, " + DeleteKeyName + " vuelve a las iniciales.",
            "- Con Ctrl+Alt evita las letras que con AltGr escriben un símbolo, como la E del euro: el atajo saltaría "
                + "mientras lo escribes.",

            "# El historial",
            "Lo que copias se queda en la lista el número de días que decidas (" + Texts.DefaultKeepDays
                + " si no lo cambias), también después de cerrar el programa o apagar el PC.",
            "## Dónde está",
            "- Se guarda sin cifrar en una base de datos, por defecto " + Clips.DefaultFile + ", junto a los textos "
                + "guardados, que en cambio están cifrados. Los días vencidos se borran solos.",
            "- En las opciones, «" + ChangeFolderButton + "» lleva la base de datos a otra carpeta, o usa la que ya "
                + "está allí; «" + DefaultFolderButton + "» la devuelve. No sirve para compartir la lista entre PC: los "
                + "textos guardados solo se leen en este PC. Si al iniciar no se puede acceder a la carpeta elegida, la "
                + "lista trabaja en memoria y lo dice.",
            "## Qué se queda y qué se va",
            "- Con 0 días solo queda lo que copiaste hoy.",
            "- Las imágenes copiadas ocupan como máximo " + Size(Clips.PictureSpace) + ": por encima, las más antiguas se "
                + "van aunque no hayan vencido sus días, y una barra roja bajo la lista lo dice. Las imágenes guardadas no "
                + "cuentan.",
            "- Las copias que otros programas marcan como privadas no se recuerdan.",
            "- También las entradas arriba se van cuando vencen sus días: lo que deba quedarse, guárdalo.",
            "## Limpiar",
            "- En las opciones, «" + CleanButton + "» borra enseguida lo que copiaste antes de los últimos días que "
                + "escribes al lado: con 1 solo queda hoy, con 0 no queda nada. Pide confirmación; también se van las "
                + "entradas arriba, los textos guardados no se tocan nunca.",
            "- Al desactivar el historial en las opciones se vacía la lista y se borra del disco lo que copiaste.",

            "# Las opciones",
            "El botón «" + Options + "» las abre en una ventana aparte, que puede quedarse abierta junto a la lista. Cada "
                + "opción dice en pocas palabras, bajo su nombre, qué hace; si dejas el puntero encima aparece su explicación "
                + "completa. Están en cinco páginas, que se eligen en la columna de la izquierda como los temas de esta guía.",
            "## " + TypingTitle,
            "Cuántos segundos esperar antes de teclear (de " + MainForm.MinDelay + " a "
                + MainForm.MaxDelay + "), si el servidor usa el teclado estadounidense, si pulsar Intro al final, si "
                + "vaciar el portapapeles después, el tecleo lento y los símbolos compuestos con códigos Alt.",
            "## " + HotkeysTitle,
            "Activa y desactiva " + type + ", " + pick + " y " + MainForm.NumbersName
                + ", y les da otras teclas; y si " + pick + " abre la lista donde la dejaste.",
            "## " + HistoryTitle,
            "Si recordar lo que copias, durante cuántos días, la carpeta de la base de datos "
                + "(que se puede cambiar) con el enlace que la abre, y el botón que limpia enseguida el historial.",
            "## " + AppearanceTitle,
            "El idioma, el tema (claro, oscuro o «" + ThemeSystem + "»), la fuente y el tamaño "
                + "de la interfaz (botones, opciones, esta guía: en píxeles, reinician el programa), los de las entradas "
                + "(cambian enseguida), y si dejar el puntero sobre una tarjeta la muestra entera.",
            "## " + ProgramTitle,
            "Si seguir en el área de notificación y ocultarse al cambiar a otro programa, si "
                + "iniciarse con Windows, los botones para exportar e importar, y la versión del programa.",

            "# Exportar e importar",
            "En las opciones, bajo «" + ProgramTitle + "», dos botones sacan tus textos del programa y los recuperan, "
                + "como copia de seguridad o para pasarlos a otro PC. Cada uno pregunta si quieres solo los textos "
                + "guardados o todo, es decir también lo copiado.",
            "## Exportar",
            "- «" + ExportButton + "» escribe un archivo de texto que se abre con el Bloc de notas: los textos "
                + "guardados con sus carpetas (los que tienen formato, con su HTML antes del texto) y, si lo pides, "
                + "lo copiado con su hora y su flecha. Las imágenes no "
                + "se incluyen.",
            "- En el archivo los textos van sin cifrar, también los privados: guárdalo en un lugar seguro.",
            "- El archivo se puede retocar a mano: el texto de cada entrada está en las líneas que empiezan con una "
                + "tabulación, que debe quedarse en su sitio.",
            "## Importar",
            "«" + ImportButton + "» lee un archivo así y añade lo que contiene a lo que ya hay. Si un texto guardado "
                + "tiene el nombre de uno existente pregunta cuál conservar; lo copiado que la lista ya tiene no se "
                + "repite. Un archivo exportado en otro idioma se lee igual.",

            "# El icono de la bandeja",
            "Con la opción activada, cerrar o minimizar la ventana solo la oculta: el programa sigue funcionando y "
                + "los atajos siguen respondiendo.",
            "## La ventana",
            "- Un clic en el icono vuelve a abrir la ventana; clic derecho y luego «" + Exit + "» cierra de verdad.",
            "- Con «" + LeaveOption + "» la ventana vuelve ahí también cuando haces clic en otro sitio; desactivada, "
                + "sigue abierta detrás de las demás. Mientras espera y teclea, en cambio, está por encima de todo: "
                + "la cuenta atrás y «" + Cancel + "» quedan a la vista.",
            "- Iniciar el programa una segunda vez hace reaparecer la ventana del que ya está abierto.",
            "## El menú y el icono",
            "- En el menú del clic derecho también están los textos guardados, cada carpeta un submenú: elegir uno lo "
                + "devuelve al portapapeles, listo para Ctrl+V o para " + type + ", sin abrir la ventana.",
            "- Junto a un texto guardado con número aparecen sus teclas.",
            "- El icono está en color si en el portapapeles hay texto, gris si no lo hay.",

            "# Si algo no funciona",
            "La línea bajo las tarjetas dice qué acaba de pasar, y por qué. Las confirmaciones desaparecen tras "
                + "unos segundos y dejan el sitio a la lista; los avisos y errores se quedan hasta el siguiente mensaje.",
            "## El tecleo",
            "- Los símbolos llegan mal: el servidor usa otro teclado. Prueba la opción del teclado inglés (EE. UU.).",
            "- Faltan caracteres, o llegan desordenados: activa el tecleo lento.",
            "- No llegan los símbolos que tu teclado no tiene (~ y ` en el italiano): activa los códigos Alt, que "
                + "funcionan si el servidor es Windows.",
            "- " + type + " no hace nada con el escritorio remoto a pantalla completa: ese programa captura todos los "
                + "atajos. Usa el botón.",
            "- Windows bloquea el envío de teclas: el programa de escritorio remoto se ejecuta como administrador. "
                + "Inicia " + app + " de la misma forma.",
            "## Los atajos",
            "Un atajo se desactiva solo: ya lo usa otro programa. Dale otras teclas en las opciones."
        };
    }
}
