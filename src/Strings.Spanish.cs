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
        RecentTitle = "Copiado recientemente"; SavedTitle = "Guardados"; PausedSuffix = " (en pausa)";
        New = "Nuevo…"; SaveDots = "Guardar…"; EditDots = "Editar…"; RenameDots = "Renombrar…"; Delete = "Eliminar";
        Options = "Opciones"; SearchCue = "Buscar…";
        CountOf = "{0} de {1}";
        NoResults = "Sin resultados"; HistoryOffEmpty = "El historial está desactivado";
        PausedEmpty = "En pausa: lo que copias no se recuerda"; CopyHint = "Lo que copies aparecerá aquí";
        NoSaved = "Ningún texto guardado";
        NewTip = "Escribe un texto nuevo para conservar, con su nombre ({0}).";
        SaveTip = "En una entrada copiada: la guarda para que no venza nunca, con nombre o sin él,\n"
            + "y puedes retocar su contenido. Con nombre la lista muestra el nombre, no el contenido.\n"
            + "En una entrada guardada: cambia su nombre o su contenido ({0}).\n"
            + "En una carpeta: cambia su nombre o su lugar.\n"
            + "También se guarda una imagen: se sigue viendo, con su nombre al lado.";
        HelpTip = "Abre la guía: todo lo que sabe hacer el programa ({0}).";
        DeleteTip = "Elimina el texto guardado, o quita de la lista la entrada copiada ({0}).\n"
            + "De una carpeta elimina solo la carpeta: lo que contiene sube un nivel.";
        DividerTip = "Arrastra para dar más o menos espacio a los textos guardados.\nDoble clic: el ancho inicial.";
        Error = "Error: {0}";
        NewerDatabase = "{0} fue escrito por una versión más reciente de {1}: actualiza el programa para abrirlo.";
        UnexpectedError ="Algo ha ido mal: {0}\n\nEl programa continúa. Los detalles están en {1}.";
        SqliteMissing = "{0} no puede iniciarse: guarda lo que copias y guardas con SQLite, que forma parte de Windows 10 "
            + "y 11 (winsqlite3.dll), y este Windows no lo tiene.";
        SqliteTooOld = "{0} no puede iniciarse: necesita SQLite {1} o posterior, que forma parte de Windows 10 y 11, "
            + "y este Windows tiene la versión {2}. Actualizar Windows lo soluciona.";
        PicturesFull = "El espacio para las imágenes copiadas ({0}) está lleno: las más antiguas se borran, una a una, a "
            + "medida que copias otras nuevas.";

        Open = "Abrir"; Exit = "Salir"; Pause = "Pausar la captura"; Resume = "Reanudar la captura";
        ShowContent = "Mostrar el contenido"; MoveTo = "Mover a"; NewFolder = "Nueva carpeta…";
        Pin = "Mantener arriba"; Unpin = "Quitar de arriba"; OutOfFolders = "Fuera de las carpetas"; EmptyFolder = "Vacía";
        NumberMenu = "Atajo"; NoNumber = "Ninguno";

        TrayReady = ": texto listo en el portapapeles"; TrayEmpty = ": sin texto en el portapapeles";
        TrayPaused = ", captura en pausa";
        TrayHint = "Sigo activo aquí. Clic para volver a abrir; clic derecho y luego {0} para cerrar.";

        StartupRefused = "Windows no lo permite: actívalo desde Configuración > Aplicaciones > Inicio.";
        StartupOn = "Al iniciar sesión en Windows, {0} se abrirá solo, sin mostrar la ventana.";
        StartupOff = "{0} ya no se inicia con Windows.";
        PausedSay = "En pausa: lo que copias no se recuerda hasta que reanudes la captura.";
        ResumedSay = "La captura se ha reanudado: lo que copias vuelve a la lista.";
        Pinned = "Se queda arriba: {0}."; Unpinned = "Ya no está arriba: {0}.";
        NumberGiven = "{0} ahora pega «{1}» dondequiera que estés.";
        NumberTaken = "«{0}» ya no tiene atajo.";
        Pasted = "Pegado en {1}: {0}.";
        PasteFocusLost = "No se ha pegado: la ventana ha cambiado. La entrada está en el portapapeles.";
        OnClipboard = "En el portapapeles: {0}. Pégalo con Ctrl+V.";
        OnClipboardOrType = "En el portapapeles: {0}. Pégalo con Ctrl+V o tecléalo con {1}.";
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
        Yes = "Sí"; No = "No"; Ok = "Aceptar";

        Cancel = "Cancelar";
        NoText = "No hay texto en el portapapeles: copia antes la contraseña.";
        Cancelled = "Cancelado.";
        CapsLock = "Bloq Mayús está activado: desactívalo y vuelve a intentarlo.";
        TypingInto = "Tecleando en {0}…";
        AskUnusual = "El portapapeles contiene {0}: no parece una contraseña.\n\n¿Quieres teclearlo de todos modos?";
        OnLines = " en {0} líneas";
        Countdown = "Tecleo dentro de {0} s en {1}. Pulsa Cancelar para detener.";
        TimeUp = "Se acabó el tiempo: no has elegido la ventana en la que teclear.";
        ClickNow = "Haz clic ahora en el campo de contraseña de la ventana remota…";
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
            + "copia la contraseña, pulsa aquí y haz clic en el campo de la ventana remota.\n"
            + "O pulsa {0} directamente en el campo.";
        SearchTip = "Escribe para buscar en las dos columnas: palabras en cualquier orden, sin importar\n"
            + "mayúsculas ni acentos. Las flechas recorren la columna, Tab pasa a la otra.\n"
            + "{0} o un clic devuelven la entrada al portapapeles: luego la pegas con Ctrl+V o la\n"
            + "tecleas con {1}. Con un clic la ventana sigue abierta, con {0} o un doble\n"
            + "clic vuelves a donde estabas. El clic derecho abre el menú de la entrada.\n"
            + "Si dejas el puntero sobre una entrada copiada, la ves entera.\n"
            + "La flecha a la derecha de una entrada copiada ({2}) la mantiene un tiempo arriba de la lista:\n"
            + "se va igualmente cuando vencen sus días. Lo que deba quedarse, guárdalo.\n"
            + "Los textos guardados no vencen nunca, están cifrados para tu usuario, y\n"
            + "solo se ve su nombre. Se pueden reunir en carpetas: «Nueva carpeta» está en el\n"
            + "menú del clic derecho ({3}); un texto entra arrastrándolo, o con «Mover a».\n"
            + "Un clic en la carpeta la abre y la cierra.\n"
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
        DelayOption = "Espera antes de teclear (segundos):";
        UsOption = "El servidor usa teclado inglés (EE. UU.)";
        EnterOption = "Pulsar Intro al final";
        ClearOption = "Vaciar el portapapeles tras teclear";
        SlowOption = "Tecleo lento";
        AltCodeOption = "Códigos Alt para símbolos sin tecla";
        LanguageOption = "Idioma (Language):";
        ThemeOption = "Tema:"; ThemeSystem = "Como Windows"; ThemeLight = "Claro"; ThemeDark = "Oscuro";
        FaceOption = "Fuente de la interfaz:"; FaceSizeOption = "Tamaño de la interfaz:";
        FontOption = "Fuente de las entradas:"; SizeOption = "Tamaño de las entradas:"; Pixels = "{0} px";
        TrayOption = "Seguir en el área de notificación";
        LeaveOption = "Ocultar al cambiar a otro programa";
        StartOption = "Iniciar con Windows";
        ExportButton = "Exportar…"; ImportButton = "Importar…";
        PickOption = "Abrir la lista";
        QuickOption = "Pegar por número";
        HistoryOption = "Recordar textos e imágenes copiados";
        KeepOption = "Días que se guardan (0: solo hoy):";
        FolderLink = "Abrir la carpeta de los datos";
        DataFolderOption = "Carpeta de las entradas copiadas y guardadas:";
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
        SpareOption = "Al limpiar, conservar los últimos días:";
        CleanButton = "Limpiar ahora…";
        DelayTip = "Cuántos segundos debe quedarse en primer plano la ventana de destino\n"
            + "antes de que empiece el tecleo: es el tiempo para hacer clic en el campo\ncorrecto, o para cancelar.";
        UsTip = "Déjala desactivada para los escritorios remotos normales: vale el teclado de este PC.\n"
            + "Actívala si los símbolos llegan mal porque el servidor usa el teclado\n"
            + "estadounidense (pasa con las consolas VMware, iLO, iDRAC y algunas pantallas\nde inicio de sesión).";
        EnterTip = "Pulsa Intro después del último carácter, para confirmar el acceso enseguida.";
        ClearTip = "Borra la contraseña del portapapeles cuando el tecleo ha salido bien.\n"
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
        HistoryTip = "La lista ofrece los textos e imágenes copiados mientras el programa está abierto.\n"
            + "Lo que los gestores de contraseñas marcan como privado no se recuerda.\n"
            + "Desactívala si copias contraseñas desde programas que no lo hacen: se leerían en\n"
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
            + "En el archivo los textos van sin cifrar, contraseñas incluidas: guárdalo en un lugar seguro.";
        ImportTip = "Lee un archivo escrito por «Exportar», también en otro PC, y añade lo que contiene\n"
            + "a lo que ya hay. Si un texto guardado tiene el nombre de uno existente, pregunta cuál conservar.\n"
            + "Lo copiado que la lista ya tiene no se repite.";
        OnlySaved = "Solo los textos guardados…"; Everything = "Todo, también lo copiado…";
        TextFiles = "Archivos de texto";
        AskExport = "El archivo contendrá los textos sin cifrar, contraseñas incluidas: quien lo abra podrá leerlos.\n\n"
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
        NameHintNamed = "La lista mostrará «{0}»; el contenido queda oculto, ideal para una contraseña.";
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
        ExportClearWarning = "Atención: aquí los textos van sin cifrar, contraseñas incluidas.";
        ExportEditNote = "El texto de cada entrada está en las líneas que empiezan con una tabulación: si modificas el "
            + "archivo, déjala en su sitio.";

        HelpTitle = "Guía de {0}"; OptionsTitle = "Opciones de {0}"; Close = "Cerrar";
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
                + "introducir una contraseña donde pegar no funciona, por ejemplo en las pantallas de bloqueo de los "
                + "escritorios remotos.",

            "# La lista",
            "A la izquierda está lo copiado recientemente, textos e imágenes, de lo más nuevo a lo más antiguo. A la "
                + "derecha los textos guardados, que se quedan hasta que los eliminas.",
            "- Escribe en la casilla de arriba para buscar en las dos columnas: las palabras valen en cualquier orden, "
                + "sin importar mayúsculas ni acentos.",
            "- Un clic pone la entrada en el portapapeles y deja la ventana abierta, para que aún puedas guardarla o "
                + "eliminarla.",
            "- " + EnterKeyName + " o un doble clic la ponen en el portapapeles y, si habías abierto la lista con "
                + pick + ", te devuelven a la ventana en la que estabas trabajando.",
            "- Luego la pegas con Ctrl+V, o la tecleas con " + type + ".",
            "- El clic derecho selecciona la entrada y abre su menú, sin tocar el portapapeles.",
            "- Si dejas el puntero sobre una entrada copiada que no cabe entera en su fila, al momento aparece "
                + "completa en un recuadro: el texto con todas sus líneas, la imagen más grande. Desaparece en cuanto "
                + "mueves el puntero.",
            "- La flecha del extremo derecho de una entrada copiada (" + Key(Keys.Control | MainForm.PinKey) + ") la "
                + "mantiene un tiempo arriba de la columna: aparece atenuada al pasar por encima, y un clic la enciende "
                + "o la apaga. La entrada se va igualmente cuando vencen los días del historial: lo que deba quedarse, "
                + "guárdalo. Copiar de nuevo el mismo texto lo devuelve arriba con la hora nueva, flecha y número incluidos.",
            "- «" + NumberMenu + "», en el menú del clic derecho, da a un texto guardado o a una entrada copiada un "
                + "número del 1 al " + MainForm.MaxNumber + ": luego, desde cualquier programa, " + MainForm.NumbersName
                + " la pegan enseguida en la ventana en la que estás trabajando, sin abrir esta (la entrada va al "
                + "portapapeles y Ctrl+V sigue solo). El número aparece en un pequeño recuadro a la derecha de la fila, "
                + "y junto al texto guardado en el menú del icono. Una entrada copiada conserva su número hasta que "
                + "vencen sus días. Cada número es de una sola entrada: si se lo das a otra, pasa a ella.",
            "El signo de color de la izquierda dice qué es la entrada: un texto, una dirección web, un texto "
                + "guardado. Las imágenes muestran una miniatura. Debajo de la entrada, en pequeño a la derecha, está la hora de la copia (el día, si "
                + "no es de hoy) o el día en que se guardó.",
            "Para no recordar lo que vas a copiar, «" + Pause + "» está en el menú del clic derecho de la lista y en "
                + "el del icono junto al reloj: sobre la columna aparece «en pausa» hasta que eliges «" + Resume
                + "». En el siguiente inicio la captura vuelve a estar activa.",
            "El espacio entre las dos columnas se arrastra, para dar más o menos sitio a los textos guardados: el "
                + "ancho elegido se mantiene en el siguiente inicio, y un doble clic ahí vuelve al inicial.",
            "La columna de lo copiado muestra las " + MainForm.MaxShown + " entradas más recientes: las demás se "
                + "encuentran buscando.",

            "# Textos guardados",
            "Son lo que quieres tener siempre a mano: una firma, una dirección, una contraseña. No vencen nunca. El "
                + "nombre es opcional: con nombre, la lista muestra solo el nombre y el contenido queda oculto, ideal "
                + "para una contraseña; sin él, la lista muestra el contenido, como para una segunda dirección de correo.",
            "- «" + New + "» (" + Key(Keys.Control | MainForm.NewKey) + ") escribe un texto desde cero.",
            "- «" + SaveDots + "» (" + Key(Keys.Control | MainForm.SaveKey) + "), en una entrada copiada, la conserva, "
                + "con nombre o sin él; antes de guardar puedes retocar su contenido.",
            "- «" + EditDots + "» (" + Key(Keys.Control | MainForm.SaveKey) + "), en una entrada guardada, cambia su "
                + "nombre o su contenido.",
            "- «" + Delete + "» (" + Key(Keys.Control | MainForm.DeleteKey) + ") borra el texto guardado, o quita de la "
                + "lista la entrada copiada.",
            "- «" + ShowContent + "», en el menú del clic derecho de un texto guardado, lo muestra en un recuadro "
                + "unos segundos: la lista solo muestra su nombre, y pasar el puntero por encima no basta.",
            "- Los textos guardados también están en el menú del icono junto al reloj: elegir uno lo devuelve al "
                + "portapapeles sin abrir la ventana.",
            "Los textos guardados no vencen nunca. Están en la base de datos del programa, cifrados para tu usuario: "
                + "otro usuario del mismo PC no puede leerlos. Cuando devuelves uno al portapapeles se marca como privado, para que el "
                + "historial de Windows (Win+V) y los demás gestores del portapapeles lo ignoren. También se puede "
                + "guardar una imagen copiada: siempre muestra su miniatura, con su nombre al lado si lo tiene.",
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
            "Cuando los textos guardados son muchos puedes reunirlos en carpetas, y las carpetas en otras carpetas.",
            "- «" + NewFolder + "» está en el menú del clic derecho (" + Key(Keys.Control | Keys.Shift | MainForm.NewKey)
                + "): nace dentro de la carpeta seleccionada, o la del texto seleccionado, pero en su ventana puedes "
                + "elegir dónde ponerla.",
            "- Un clic en el nombre de una carpeta, o " + EnterKeyName + ", la abre y la cierra; el número de la "
                + "derecha dice cuántos textos contiene, también en las carpetas que tiene dentro. Abiertas o "
                + "cerradas, las encuentras como las dejaste.",
            "- Un texto guardado, o una carpeta con todo lo que contiene, se arrastra dentro de otra carpeta: sobre "
                + "la carpeta misma o sobre uno de sus textos. Soltado sobre un texto fuera de las carpetas, o debajo "
                + "de la última fila, sale de la carpeta en la que estaba.",
            "- «" + MoveTo + "», en el menú del clic derecho de un texto guardado o de una carpeta, hace lo mismo. La "
                + "misma elección está en la ventana donde lo escribes, a la izquierda de los botones.",
            "- Con una carpeta seleccionada, «" + RenameDots + "» cambia su nombre o su lugar y «" + Delete + "» quita "
                + "solo la carpeta: lo que contenía, textos y carpetas, sube a la carpeta que la contenía, o queda "
                + "entre los guardados fuera de las carpetas.",
            "- El nombre de una carpeta no puede contener «" + Texts.Separator + "».",
            "- En el menú del icono junto al reloj cada carpeta es un submenú con sus textos.",
            "- La búsqueda encuentra también dentro de las carpetas cerradas, y el nombre de una carpeta encuentra "
                + "sus textos.",

            "# Teclear una contraseña",
            "Donde Ctrl+V no funciona, " + app + " escribe el texto del portapapeles tecla por tecla, como harías tú "
                + "en el teclado.",
            "- Copia la contraseña.",
            "- Pulsa «" + TypeButton + "».",
            "- Haz clic en el campo de la ventana remota: tienes " + MainForm.ArmedSeconds + " segundos para hacerlo.",
            "- Cuando esa ventana ha estado en primer plano el tiempo de la espera, empieza el tecleo.",
            "Si no, pulsa " + type + " directamente en el campo: teclea enseguida, sin espera.",
            "Para no enviar una contraseña al sitio equivocado:",
            "- el tecleo se detiene solo si la ventana de destino pierde el primer plano;",
            "- durante la espera y el tecleo el mismo botón se convierte en «" + Cancel + "»;",
            "- el escritorio, la barra de tareas y el menú Inicio nunca son un destino;",
            "- si el portapapeles contiene más de " + MainForm.UnusualLength + " caracteres o varias líneas, se pide "
                + "confirmación;",
            "- con Bloq Mayús activado no se teclea: hay que desactivarlo antes.",

            "# Teclas",
            pick + "\tAbre la lista, desde cualquier programa.",
            type + "\tTeclea enseguida el portapapeles en la ventana activa.",
            MainForm.NumbersName + "\tPega en la ventana activa la entrada con ese número.",
            "F1\tAbre esta guía.",
            ArrowsName + "\tRecorre la columna.",
            "Tab\tPasa a la otra columna.",
            EnterKeyName + "\tElige la entrada y vuelve a la ventana de partida.",
            "Esc\tVuelve a la ventana de partida sin elegir.",
            Key(Keys.Control | MainForm.NewKey) + "\tNuevo texto guardado.",
            Key(Keys.Control | Keys.Shift | MainForm.NewKey) + "\tNueva carpeta.",
            Key(Keys.Control | MainForm.SaveKey) + "\tGuarda la entrada copiada, o edita la guardada.",
            Key(Keys.Control | MainForm.DeleteKey) + "\tElimina la entrada seleccionada.",
            Key(Keys.Control | MainForm.PinKey) + "\tMantiene arriba la entrada copiada, o la suelta.",
            "Los tres primeros funcionan en todas partes; los demás, en la lista. Mientras está activo, " + pick
                + " ya no llega a los demás programas, ni tampoco las teclas de los números que tiene alguna entrada.",
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
            "- Se guarda sin cifrar en una base de datos, por defecto " + Clips.DefaultFile + ", junto a los textos "
                + "guardados, que en cambio están cifrados. Los días vencidos se borran solos.",
            "- En las opciones, «" + ChangeFolderButton + "» lleva la base de datos a otra carpeta, o usa la que ya "
                + "está allí; «" + DefaultFolderButton + "» la devuelve. No sirve para compartir la lista entre PC: los "
                + "textos guardados solo se leen en este PC. Si al iniciar no se puede acceder a la carpeta elegida, la "
                + "lista trabaja en memoria y lo dice.",
            "- Con 0 días solo queda lo que copiaste hoy.",
            "- Las imágenes copiadas ocupan como máximo " + Size(Clips.PictureSpace) + ": por encima, las más antiguas se "
                + "van aunque no hayan vencido sus días, y una barra roja bajo la lista lo dice. Las imágenes guardadas no "
                + "cuentan.",
            "- Lo que los gestores de contraseñas marcan como privado no se recuerda.",
            "- También las entradas arriba se van cuando vencen sus días: lo que deba quedarse, guárdalo.",
            "- En las opciones, «" + CleanButton + "» borra enseguida lo que copiaste antes de los últimos días que "
                + "escribes al lado: con 1 solo queda hoy, con 0 no queda nada. Pide confirmación; también se van las "
                + "entradas arriba, los textos guardados no se tocan nunca.",
            "- Al desactivar el historial en las opciones se vacía la lista y se borra del disco lo que copiaste.",

            "# Las opciones",
            "El botón «" + Options + "» las abre en una ventana aparte, que puede quedarse abierta junto a la lista. Si dejas el "
                + "puntero sobre una opción aparece su explicación completa. Están en cuatro páginas, que se eligen en la "
                + "columna de la izquierda como los temas de esta guía:",
            "- " + TypingTitle + ": cuántos segundos esperar antes de teclear (de " + MainForm.MinDelay + " a "
                + MainForm.MaxDelay + "), si el servidor usa el teclado estadounidense, si pulsar Intro al final, si "
                + "vaciar el portapapeles después, el tecleo lento y los símbolos compuestos con códigos Alt.",
            "- " + HotkeysTitle + ": activa y desactiva " + type + ", " + pick + " y " + MainForm.NumbersName
                + ", y les da otras teclas.",
            "- " + HistoryTitle + ": si recordar lo que copias, durante cuántos días, la carpeta de la base de datos "
                + "(que se puede cambiar) con el enlace que la abre, y el botón que limpia enseguida el historial.",
            "- " + ProgramTitle + ": el idioma, el tema (claro, oscuro o «" + ThemeSystem + "»), la fuente y el tamaño de la interfaz (botones, opciones, esta guía: en píxeles, reinician el programa) y los de las entradas (cambian enseguida), si seguir en el área de notificación y ocultarse al cambiar a otro "
                + "programa, si iniciarse con Windows, y los botones para exportar e importar.",

            "# Exportar e importar",
            "En las opciones, bajo «" + ProgramTitle + "», dos botones sacan tus textos del programa y los recuperan, "
                + "como copia de seguridad o para pasarlos a otro PC. Cada uno pregunta si quieres solo los textos "
                + "guardados o todo, es decir también lo copiado.",
            "- «" + ExportButton + "» escribe un archivo de texto que se abre con el Bloc de notas: los textos "
                + "guardados con sus carpetas (los que tienen formato, con su HTML antes del texto) y, si lo pides, "
                + "lo copiado con su hora y su flecha. Las imágenes no "
                + "se incluyen.",
            "- En el archivo los textos van sin cifrar, contraseñas incluidas: guárdalo en un lugar seguro.",
            "- «" + ImportButton + "» lee un archivo así y añade lo que contiene a lo que ya hay. Si un texto guardado "
                + "tiene el nombre de uno existente pregunta cuál conservar; lo copiado que la lista ya tiene no se "
                + "repite. Un archivo exportado en otro idioma se lee igual.",
            "- El archivo se puede retocar a mano: el texto de cada entrada está en las líneas que empiezan con una "
                + "tabulación, que debe quedarse en su sitio.",

            "# El icono de la bandeja",
            "Con la opción activada, cerrar o minimizar la ventana solo la oculta: el programa sigue funcionando y "
                + "los atajos siguen respondiendo.",
            "- Un clic en el icono vuelve a abrir la ventana; clic derecho y luego «" + Exit + "» cierra de verdad.",
            "- Con «" + LeaveOption + "» la ventana vuelve ahí también cuando haces clic en otro sitio; desactivada, "
                + "sigue abierta detrás de las demás. Mientras espera y teclea, en cambio, está por encima de todo: "
                + "la cuenta atrás y «" + Cancel + "» quedan a la vista.",
            "- En el menú del clic derecho también están los textos guardados, cada carpeta un submenú: elegir uno lo "
                + "devuelve al portapapeles, listo para Ctrl+V o para " + type + ", sin abrir la ventana.",
            "- Junto a un texto guardado con número aparecen sus teclas.",
            "- El icono está en color si en el portapapeles hay texto, gris si no lo hay.",
            "- Iniciar el programa una segunda vez hace reaparecer la ventana del que ya está abierto.",

            "# Si algo no funciona",
            "- Los símbolos llegan mal: el servidor usa otro teclado. Prueba la opción del teclado inglés (EE. UU.).",
            "- Faltan caracteres, o llegan desordenados: activa el tecleo lento.",
            "- No llegan los símbolos que tu teclado no tiene (~ y ` en el italiano): activa los códigos Alt, que "
                + "funcionan si el servidor es Windows.",
            "- " + type + " no hace nada con el escritorio remoto a pantalla completa: ese programa captura todos los "
                + "atajos. Usa el botón.",
            "- Windows bloquea el envío de teclas: el programa de escritorio remoto se ejecuta como administrador. "
                + "Inicia " + app + " de la misma forma.",
            "- Un atajo se desactiva solo: ya lo usa otro programa. Dale otras teclas en las opciones.",
            "La línea encima de los botones dice qué acaba de pasar, y por qué. Las confirmaciones desaparecen tras "
                + "unos segundos y dejan el sitio a la lista; los avisos y errores se quedan hasta el siguiente mensaje."
        };
    }
}
