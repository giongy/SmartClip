using System.Windows.Forms;

// The texts in French.
static partial class Strings
{
    static void French()
    {
        ShiftName = "Maj"; DeleteKeyName = "Suppr"; EnterKeyName = "Entrée"; ArrowsName = "Haut / Bas";
        DayFormat = "dd/MM"; YearDayFormat = "dd/MM/yy";
        Quote = "« {0} »";
        Megabytes = "{0} Mo"; Kilobytes = "{0} Ko";

        TypeButton = "Saisir le presse-papiers";
        SavedTitle = "Enregistrés"; PausedSuffix = " (en pause)";
        New = "Nouveau…"; SaveDots = "Garder…"; EditDots = "Modifier…"; RenameDots = "Renommer…"; Delete = "Supprimer";
        Options = "Options"; SearchCue = "Rechercher…";
        CountOf = "{0} sur {1}";
        NoResults = "Aucun résultat"; HistoryOffEmpty = "L'historique est désactivé";
        PausedEmpty = "En pause : ce que vous copiez n'est pas mémorisé"; CopyHint = "Ce que vous copiez apparaîtra ici";
        NoSaved = "Aucun texte enregistré";
        NewTip = "Écrit un nouveau texte à garder, avec ou sans nom ({0}).";
        AllPlace = "Tout"; PinnedPlace = "En haut"; SourcesPlace = "Provenance"; GuideItem = "Aide";
        AllKinds = "Tout"; TextKinds = "Texte"; CodeKinds = "Code"; LinkKinds = "Liens"; PictureKinds = "Images";
        TextBadge = "Texte"; CodeBadge = "Code"; LinkBadge = "Lien"; PictureBadge = "Image"; FormattedBadge = "Mis en forme";
        CopyButton = "Copier"; ShowFull = "Aperçu"; MoreItem = "Plus";
        Today = "Aujourd'hui"; Yesterday = "Hier"; Undated = "Avant";
        NoPinned = "Rien en haut : la flèche d'un élément copié l'amène ici pour un moment";
        NoneOfKind = "Rien de ce type";
        SourcesTip = "Les programmes dans lesquels vous avez copié : choisissez-en un pour ne voir que ce que vous y avez copié.";
        PinnedTip = "Les éléments copiés gardés en haut pour un moment : ils partent quand même à la fin de leurs jours.";
        NarrowSideTip = "Réduit la barre latérale à ses icônes. On peut aussi faire glisser son bord, pour l'élargir ou la rétrécir.";
        WideSideTip = "Affiche de nouveau les noms dans la barre latérale.";
        Error = "Erreur : {0}";
        NewerDatabase = "{0} a été écrit par une version plus récente de {1} : mettez le programme à jour pour l'ouvrir.";
        UnexpectedError ="Une erreur s'est produite : {0}\n\nLe programme continue. Les détails sont dans {1}.";
        SqliteMissing = "{0} ne peut pas démarrer : il conserve ce que vous copiez et enregistrez avec SQLite, qui fait "
            + "partie de Windows 10 et 11 (winsqlite3.dll), et ce Windows ne l'a pas.";
        SqliteTooOld = "{0} ne peut pas démarrer : il a besoin de SQLite {1} ou plus récent, qui fait partie de Windows 10 "
            + "et 11, et ce Windows a la version {2}. Mettre Windows à jour résout le problème.";
        PicturesFull = "L'espace des images copiées ({0}) est plein : les plus anciennes sont supprimées, une à une, au fur "
            + "et à mesure que vous en copiez de nouvelles.";

        Open = "Ouvrir"; Exit = "Quitter"; Pause = "Suspendre la capture"; Resume = "Reprendre la capture";
        ShowContent = "Afficher le contenu"; MoveTo = "Déplacer vers"; NewFolder = "Nouveau dossier…";
        CopyPictureText = "Copier le texte";
        Pin = "Garder en haut"; Unpin = "Retirer du haut"; OutOfFolders = "Hors des dossiers"; EmptyFolder = "Vide";
        NumberMenu = "Raccourci"; NoNumber = "Aucun";
        KindMenu = "Type";

        TrayReady = " : texte prêt dans le presse-papiers"; TrayEmpty = " : pas de texte dans le presse-papiers";
        TrayPaused = ", capture en pause";
        TrayHint = "Je suis toujours actif ici. Cliquez pour rouvrir ; clic droit puis {0} pour fermer.";

        StartupRefused = "Windows ne le permet pas : activez-le dans Paramètres > Applications > Démarrage.";
        StartupOn = "À l'ouverture de session Windows, {0} démarrera tout seul, sans afficher la fenêtre.";
        StartupOff = "{0} ne démarre plus avec Windows.";
        PausedSay = "En pause : ce que vous copiez n'est pas mémorisé tant que vous ne reprenez pas la capture.";
        ResumedSay = "La capture a repris : ce que vous copiez revient dans la liste.";
        Pinned = "Reste en haut : {0}."; Unpinned = "N'est plus en haut : {0}.";
        KindChanged = "Marqué comme {1} : {0}.";
        NumberGiven = "{0} colle maintenant « {1} » où que vous soyez.";
        NumberTaken = "« {0} » n'a plus de raccourci.";
        Pasted = "Collé dans {1} : {0}.";
        PasteFocusLost = "Rien n'a été collé : la fenêtre a changé. L'élément est dans le presse-papiers.";
        OnClipboard = "Dans le presse-papiers : {0}.\tCollez-le avec Ctrl+V.";
        ReadingPicture = "Lecture du texte de l'image…";
        NoPictureText = "Windows n'a trouvé aucun texte dans l'image.";
        NoRecognition = "Windows ne sait pas lire les images dans vos langues : installez la reconnaissance optique de caractères "
            + "de l'une d'elles depuis ses options, dans Paramètres > Heure et langue.";
        OnClipboardOrType = "Dans le presse-papiers : {0}.\tCollez-le avec Ctrl+V ou saisissez-le avec {1}.";
        AskReplace = "« {0} » existe déjà. Voulez-vous le remplacer ?";
        SavedSay = "« {0} » enregistré.";
        FolderNoSeparator = "Le nom d'un dossier ne peut pas contenir « {0} ».";
        FolderExists = "Le dossier « {0} » existe déjà.";
        FolderCreated = "Dossier « {0} » créé."; FolderRenamed = "Le dossier s'appelle maintenant « {0} ».";
        FolderMoved = "Le dossier est maintenant « {0} ».";
        LeftFolder = "« {0} » n'est plus dans un dossier."; InFolder = "« {0} » est dans le dossier « {1} ».";
        AskDeleteFolder = "Voulez-vous supprimer le dossier « {0} » ?";
        DeleteFolderTop = "\n\nSon contenu n'est pas supprimé : il reste parmi les textes enregistrés, hors des dossiers.";
        DeleteFolderUp = "\n\nSon contenu n'est pas supprimé : il passe dans le dossier « {0} ».";
        AskDelete = "Voulez-vous supprimer « {0} » ?";
        Yes = "Oui"; No = "Non"; Ok = "OK";

        Cancel = "Annuler";
        NoText = "Il n'y a pas de texte dans le presse-papiers : copiez d'abord le mot de passe.";
        Cancelled = "Annulé.";
        CapsLock = "Verr. Maj est activé : désactivez-le et réessayez.";
        TypingInto = "Saisie en cours dans {0}…";
        AskUnusual = "Le presse-papiers contient {0} : cela ne ressemble pas à un mot de passe.\n\n"
            + "Voulez-vous le saisir quand même ?";
        OnLines = " sur {0} lignes";
        Countdown = "Saisie dans {0} s dans {1}. Appuyez sur Annuler pour arrêter.";
        TimeUp = "Temps écoulé : vous n'avez pas choisi la fenêtre où saisir.";
        ClickNow = "Cliquez maintenant dans le champ du mot de passe de la fenêtre distante…";
        Untitled = "une fenêtre sans titre";
        KeysHeld = "Relâchez les touches que vous maintenez enfoncées et réessayez.";
        Blocked = "Windows a bloqué l'envoi des touches. Si le programme de bureau à distance est lancé en tant "
            + "qu'administrateur, lancez aussi {0} ainsi.";
        StoppedAfter = "Arrêté après {0}."; FocusLost = "Arrêté après {0} : la fenêtre active a changé.";
        DoneOne = "Terminé : 1 caractère saisi."; DoneMany = "Terminé : {0} caractères saisis.";
        AltCoded = " {0} avec des codes Alt.";
        UnicodeOne = " Attention : 1 caractère sans touche sur le clavier, envoyé en Unicode";
        UnicodeMany = " Attention : {0} caractères sans touche sur le clavier, envoyés en Unicode";
        UnicodeHint = " : si la connexion échoue, essayez l'option des codes Alt.";
        Character = "caractère"; Characters = "caractères";

        HotkeyTip = "{0} saisit aussitôt le presse-papiers dans la fenêtre active, sans attente.\n"
            + "Cela ne marche pas si le bureau à distance est en plein écran et capture tous\n"
            + "les raccourcis : utilisez alors le bouton « {1} ».";
        PickTip = "{0} amène au premier plan cette fenêtre avec la liste, depuis n'importe quel programme.\n"
            + "Une fois un élément choisi, la main revient à la fenêtre d'où vous êtes parti : collez-le\n"
            + "là avec Ctrl+V ou saisissez-le avec {1}.\n"
            + "Tant qu'il est actif, {0} n'arrive plus aux autres programmes.";
        ChangeKeysNote = "\nLe bouton à droite montre ses touches : appuyez dessus pour lui en donner d'autres.";
        KeysTip = "Appuyez ici puis sur les nouvelles touches : Ctrl ou Alt, aussi avec {0}, avec une lettre,\n"
            + "un chiffre ou une touche de fonction. Échap garde les actuelles, {1} revient à celles de départ.\n"
            + "Avec Ctrl+Alt, évitez les lettres qui écrivent un symbole avec AltGr (E, pour l'euro) : le\n"
            + "raccourci se déclencherait en l'écrivant.";
        LeaveTip = "Quand vous cliquez sur un autre programme, la fenêtre retourne dans la zone de notification,\n"
            + "comme si vous la fermiez : rouvrez-la avec l'icône ou avec {0}. Désactivée, elle reste ouverte\n"
            + "derrière les autres. Seulement avec l'option ci-dessus active. Cela n'arrive pas pendant\n"
            + "l'attente ou la saisie, ni pendant que vous écrivez un texte enregistré ou répondez à une\n"
            + "question : vous pouvez copier ailleurs en attendant.";
        TypeTip = "Saisit le texte du presse-papiers une touche à la fois, là où coller ne marche pas :\n"
            + "copiez le mot de passe, appuyez ici et cliquez dans le champ de la fenêtre distante.\n"
            + "Ou appuyez sur {0} directement dans le champ.";
        SearchTip = "Tapez pour chercher parmi les éléments de l'endroit : mots dans n'importe quel ordre, sans tenir\n"
            + "compte des majuscules ni des accents. Les flèches parcourent les éléments.\n"
            + "« " + CopyButton + " », {0} ou un double-clic remettent l'élément dans le presse-papiers : collez-le\n"
            + "ensuite avec Ctrl+V ou saisissez-le avec {1}. Avec « " + CopyButton + " » la fenêtre reste ouverte, avec {0}\n"
            + "ou un double-clic vous revenez où vous étiez. Un clic sélectionne seulement ; le clic droit ouvre le menu\n"
            + "de l'élément.\n"
            + "L'œil à droite d'une carte la montre en entier.\n"
            + "La flèche à droite d'un élément copié ({2}) le garde un moment en haut de la liste :\n"
            + "il part quand même quand ses jours expirent. Ce qui doit rester, enregistrez-le.\n"
            + "Les textes enregistrés n'expirent jamais et sont chiffrés pour votre utilisateur ; avec un\n"
            + "nom, seul le nom est visible. Leurs dossiers sont dans la barre latérale, avec « " + NewFolder + " »\n"
            + "({3}) en bas : un texte y entre en le faisant glisser sur l'un d'eux, ou avec « " + MoveTo + " ».\n"
            + "« {5} », dans le menu du clic droit, donne un numéro à l'élément : ensuite {4}\n"
            + "le collent où que vous soyez, sans ouvrir cette fenêtre.";
        PressKeys = "Appuyez sur les touches…";
        PressKeysStatus = "Appuyez sur les nouvelles touches : Ctrl ou Alt avec une lettre, un chiffre ou une touche de "
            + "fonction. Échap garde les actuelles, {0} revient à celles de départ.";
        KeysUnusable = "{0} ne convient pas : il faut Ctrl ou Alt avec une lettre, un chiffre ou une touche de fonction.";
        KeysTaken = "{0} sert déjà au programme : choisissez-en un autre.";
        TypeNowWith = "Pour saisir le presse-papiers, appuyez maintenant sur {0}.";
        OpenNowWith = "Pour ouvrir la liste, appuyez maintenant sur {0}.";
        HotkeyBusyType = "{0} est déjà utilisé par un autre programme : donnez-lui d'autres touches dans les options, "
            + "ou utilisez le bouton.";
        HotkeyBusyPick = "{0} est déjà utilisé par un autre programme : donnez-lui d'autres touches dans les options, "
            + "ou ouvrez la fenêtre depuis l'icône.";
        QuickTip = "{0} collent l'élément qui a ce numéro dans la fenêtre où vous travaillez,\n"
            + "sans ouvrir celle-ci : il va dans le presse-papiers et Ctrl+V suit tout seul.\n"
            + "Les numéros se donnent avec « {1} », dans le menu du clic droit d'un élément.\n"
            + "Seules les touches des numéros utilisés sont prises : elles n'arrivent plus aux autres programmes.";
        QuickKeysTip = "Appuyez ici puis sur les touches à maintenir, avec un chiffre quelconque : Ctrl ou Alt,\n"
            + "aussi avec {0}. Le chiffre ne compte pas : de 1 à 9, il choisit l'élément.\n"
            + "Échap garde les touches actuelles, {1} revient à celles de départ ({2}).\n"
            + "Ctrl+Alt équivaut à AltGr : sur certains claviers, il écrit des symboles avec les chiffres.";
        PressQuickStatus = "Appuyez sur les touches à maintenir avec un chiffre quelconque, comme {0}. "
            + "Échap garde les touches actuelles, {1} revient à celles de départ.";
        QuickUnusable = "{0} ne convient pas : maintenez Ctrl ou Alt, aussi avec Maj, et appuyez sur un chiffre.";
        QuickNowWith = "Les éléments numérotés se collent maintenant avec {0}.";
        QuickBusy = "Déjà utilisés par un autre programme : {0}. Ces numéros ne colleront pas : choisissez d'autres "
            + "touches dans les options, ou d'autres numéros.";

        TypingTitle = "Saisie"; ProgramTitle = "Programme"; HotkeysTitle = "Raccourcis"; HistoryTitle = "Historique";
        AppearanceTitle = "Apparence";
        On = "Activé"; Off = "Désactivé";
        DelayOption = "Attente avant la saisie"; DelayNote = "Secondes pour cliquer dans le bon champ";
        UsOption = "Le serveur a un clavier anglais (US)"; UsNote = "Seulement si les symboles arrivent faux";
        EnterOption = "Appuyer sur Entrée à la fin"; EnterNote = "Valide aussitôt la connexion";
        ClearOption = "Vider le presse-papiers après la saisie"; ClearNote = "Une fois la saisie réussie";
        SlowOption = "Saisie lente"; SlowNote = "Si des caractères manquent ou arrivent dans le désordre";
        AltCodeOption = "Codes Alt pour les symboles sans touche"; AltCodeNote = "Seulement pour les serveurs Windows";
        LanguageOption = "Langue (Language)"; LanguageNote = "Le programme redémarre dans cette langue";
        ThemeOption = "Thème"; ThemeNote = "Couleurs claires ou sombres";
        ThemeSystem = "Comme Windows"; ThemeLight = "Clair"; ThemeDark = "Sombre";
        FaceOption = "Police de l'interface"; FaceNote = "Boutons, options, menus : le programme redémarre";
        FontOption = "Police des éléments"; FontNote = "Ce que vous copiez et enregistrez : change aussitôt";
        RestOption = "Aperçu quand le pointeur s'arrête"; RestNote = "Sur une carte coupée, sans l'œil";
        RestTip = "Activée, le pointeur arrêté sur une carte qui coupe son élément le montre en entier.\n"
            + "Désactivée, seul l'œil à droite de la carte, ou son menu, le montre.";
        Pixels = "{0} px";
        TrayOption = "Rester dans la zone de notification"; TrayNote = "Fermer la fenêtre laisse le programme actif";
        LeaveOption = "Masquer en passant à un autre programme"; LeaveNote = "Retour dans la zone de notification";
        StartOption = "Démarrer avec Windows"; StartNote = "Masqué dans la zone de notification";
        BackupOption = "Exporter et importer"; BackupNote = "Vos textes dans un fichier que le Bloc-notes ouvre";
        ExportButton = "Exporter…"; ImportButton = "Importer…";
        AboutNote = "Logiciel libre sous licence MIT";
        TypeNote = "Saisit aussitôt dans la fenêtre active";
        PickOption = "Ouvrir la liste"; PickNote = "Amène cette fenêtre au premier plan depuis tout programme";
        WhereLeftOption = "Rouvrir là où vous l'avez laissée"; WhereLeftNote = "Sinon la liste s'ouvre sur " + AllPlace;
        WhereLeftTip = "Désactivée, le raccourci de la liste l'ouvre toujours sur " + AllPlace + ", de tous les types, avec la recherche vide.\n"
            + "Activée, il montre l'endroit et le type où vous l'avez laissée.";
        QuickOption = "Coller par numéro"; QuickNote = "Colle les éléments qui ont un raccourci";
        HistoryOption = "Mémoriser les textes et images copiés"; HistoryNote = "Ce que vous copiez pendant que le programme est ouvert";
        KeepOption = "Jours gardés"; KeepNote = "Avec 0, seulement ce que vous avez copié aujourd'hui";
        FolderLink = "Ouvrir le dossier des données";
        DataFolderOption = "Dossier des éléments copiés et enregistrés";
        ChangeFolderButton = "Changer…"; DefaultFolderButton = "Par défaut";
        DatabaseSize = "Occupe {0} ; images copiées : {1} sur {2}.";
        ChooseDataFolder = "Le dossier où {0} garde ce que vous copiez et enregistrez.";
        AskUseExisting = "{0} contient déjà une liste de {1}.\n\nUtiliser celle-ci ? La vôtre reste où elle est, telle quelle.";
        AskSyncedFolder = "{0} semble être un dossier synchronisé en ligne ou sur le réseau.\n\nLa synchronisation peut "
            + "bloquer ou abîmer la base de données, et les textes enregistrés ne se lisent pas sur un autre PC. L'utiliser quand même ?";
        ListMovedTo = "La liste est maintenant dans {0}.";
        ListMoveFailed = "La liste n'a pas pu être déplacée et reste où elle était : {0}";
        FolderUnavailable = "Le dossier de la liste, {0}, est inaccessible : ce que vous copiez et enregistrez maintenant "
            + "sera perdu à la fermeture du programme. Dans les options, sous {1}, choisissez un autre dossier ou celui par défaut.";
        SpareOption = "Nettoyer l'historique"; SpareNote = "Sauf les derniers jours écrits à côté ; 0 : aucun";
        CleanButton = "Nettoyer…";
        DelayTip = "Combien de secondes la fenêtre cible doit rester au premier plan avant que\n"
            + "la saisie commence : le temps de cliquer dans le bon champ, ou d'annuler.";
        UsTip = "Laissez-la désactivée pour les bureaux à distance ordinaires : le clavier de ce PC s'applique.\n"
            + "Activez-la si les symboles arrivent faux parce que le serveur utilise un clavier\n"
            + "américain (c'est le cas des consoles VMware, iLO, iDRAC et de certains écrans\nde connexion).";
        EnterTip = "Appuie sur Entrée après le dernier caractère, pour valider la connexion aussitôt.";
        ClearTip = "Efface le mot de passe du presse-papiers une fois la saisie réussie.\n"
            + "L'historique du presse-papiers de Windows (Win+V), s'il est actif, n'est pas touché.";
        SlowTip = "Activez-la si des caractères manquent ou arrivent dans le désordre sur le serveur.";
        AltCodeTip = "Les symboles sans touche sur ce clavier (par exemple ~ et ` sur un clavier italien) sont\n"
            + "normalement envoyés comme caractères Unicode, que tous les programmes de bureau à\n"
            + "distance n'acceptent pas. Avec cette option, ils sont composés avec des codes Alt sur\n"
            + "le pavé numérique : cela ne marche que si le serveur est sous Windows.";
        LanguageTip = "La langue du programme. La changer redémarre {0}, qui se rouvre aussitôt.";
        ThemeTip = "Couleurs claires ou sombres. « {0} » suit le mode que Windows fixe pour les applications,\n"
            + "même quand il change pendant que le programme est ouvert.";
        FaceTip = "La police du programme lui-même : boutons, libellés, options, le guide, les menus et les questions.\n"
            + "Ce que vous avez copié et enregistré a la sienne, plus bas. La changer redémarre {0}.";
        FaceSizeTip = "La taille du texte du programme lui-même, en pixels ; {0} par défaut. Les fenêtres grandissent\n"
            + "avec le texte. Ce que vous avez copié et enregistré a sa taille, plus bas. La changer redémarre {1}.";
        FontTip = "La police de ce que vous avez copié et enregistré : les lignes des listes, l'élément vu en entier\n"
            + "et le contenu d'un texte enregistré. Le reste du programme garde la police de l'interface. Elle change aussitôt.";
        SizeTip = "La taille de ce que vous avez copié et enregistré, en pixels ; {0} est le texte de Windows 11.\n"
            + "Le reste du programme garde la taille de l'interface. Elle change aussitôt.";
        HistoryTip = "La liste propose les textes et images copiés pendant que le programme est ouvert.\n"
            + "Ce que les gestionnaires de mots de passe marquent comme privé n'est pas mémorisé.\n"
            + "Désactivez-la si vous copiez des mots de passe depuis des programmes qui ne le font pas :\n"
            + "ils seraient lisibles dans la liste. Désactivée, elle efface aussi l'historique sur disque.";
        KeepTip = "Combien de jours ce que vous copiez reste dans l'historique, même après la fermeture,\n"
            + "aujourd'hui compris : avec 0, seul ce que vous avez copié aujourd'hui reste.\n"
            + "Les éléments en haut partent aussi quand leurs jours expirent ; les textes enregistrés jamais.\n"
            + "L'historique est gardé sur disque en clair. La liste montre les {0} plus récents :\n"
            + "la recherche trouve les autres.";
        DataFolderTip = "Où se trouve la base de données avec ce que vous copiez et enregistrez, avec le journal des erreurs :\n{0}\n"
            + "Le dossier par défaut est {1}. « Changer… » déplace la base de données dans le dossier choisi,\n"
            + "ou utilise celle qui s'y trouve déjà. Ce n'est pas un moyen de partager la liste entre PC : les\n"
            + "textes enregistrés ne sont lisibles que par cet utilisateur Windows sur ce PC, et deux programmes\n"
            + "utilisant le même fichier en même temps peuvent l'abîmer.";
        CleanTip = "Efface aussitôt de l'historique ce que vous avez copié avant les derniers jours\n"
            + "indiqués ci-dessus : avec 1, seul aujourd'hui reste ; avec 0, rien.\n"
            + "Une confirmation est demandée. Les éléments en haut partent aussi ; les textes enregistrés\n"
            + "ne sont pas touchés, ni le presse-papiers. Les jours gardés restent inchangés.";
        TrayTip = "Activée : fermer ou réduire la fenêtre laisse le programme tourner sous forme d'icône\n"
            + "près de l'horloge, et les raccourcis continuent de fonctionner.\n"
            + "Cliquez sur l'icône pour rouvrir la fenêtre ; clic droit puis {0} pour fermer.\n"
            + "Désactivée : fenêtre ordinaire, dont la fermeture termine le programme.";
        StartTip = "Démarre {0} à l'ouverture de session Windows, déjà caché dans la zone de notification\n"
            + "(réduit, si l'option ci-dessus est désactivée).\n"
            + "Cela vaut pour cet exécutable : si vous le déplacez, l'entrée suit au premier démarrage depuis là.\n"
            + "C'est la même entrée que Gestionnaire des tâches > Applications de démarrage : désactivée là,\n"
            + "elle l'est ici aussi.";
        ExportTip = "Écrit dans un fichier texte, lisible avec le Bloc-notes, les textes enregistrés avec leurs\n"
            + "dossiers ; si vous le demandez, aussi ce qui a été copié. Les images n'y vont pas.\n"
            + "Dans le fichier les textes sont en clair, mots de passe compris : gardez-le en lieu sûr.";
        ImportTip = "Lit un fichier écrit par « Exporter », même sur un autre PC, et ajoute son contenu à ce\n"
            + "qui existe déjà. Si un texte enregistré porte le nom d'un texte existant, il demande lequel garder.\n"
            + "Les éléments copiés que la liste a déjà ne sont pas répétés.";
        OnlySaved = "Textes enregistrés seulement…"; Everything = "Tout, éléments copiés compris…";
        TextFiles = "Fichiers texte";
        AskExport = "Le fichier contiendra les textes en clair, mots de passe compris : quiconque l'ouvre pourra les "
            + "lire.\n\nVoulez-vous continuer ?";
        SavedOne = "texte enregistré"; SavedMany = "textes enregistrés"; CopiedOne = "élément copié"; CopiedMany = "éléments copiés";
        Exported = "Exportés : {0}."; ExportedAll = "Exportés : {0}, {1}.";
        PicturesLeftOut = " Les images ({0}) ne s'exportent pas.";
        ClashOne = "Un texte enregistré du fichier porte le nom d'un texte qui existe déjà.";
        ClashMany = "{0} textes enregistrés du fichier portent le nom d'un texte qui existe déjà.";
        ClashChoice = "\n\nOui : celui du fichier l'emporte.\nNon : celui d'ici reste.";
        NothingImported = "Il n'y avait rien à importer dans le fichier.";
        Imported = "Importés : {0}."; ImportedAll = "Importés : {0}, {1}.";
        KeptAsWere = " Laissés tels quels : {0}.";
        HistoryOffImport = " L'historique est désactivé : les éléments copiés n'ont pas été importés.";
        CleanEverything = "tout ce que vous avez copié"; CleanBeforeToday = "ce que vous avez copié avant aujourd'hui";
        CleanBeforeDays = "ce que vous avez copié avant les {0} derniers jours (aujourd'hui compris)";
        AskClean = "Voulez-vous supprimer {0} ?\n\nLes textes enregistrés restent ; les éléments en haut partent aussi.";
        NothingToClean = "Rien à supprimer dans l'historique.";
        CleanedOne = "1 élément supprimé de l'historique."; CleanedMany = "{0} éléments supprimés de l'historique.";
        NoHistoryFolder = "Le dossier des données n'existe pas encore : rien n'est sur le disque.";
        DatabaseUnavailable = "La liste ne peut pas être gardée sur disque ({0}) : ce que vous copiez et enregistrez "
            + "maintenant sera perdu à la fermeture du programme.";

        NewTextTitle = "Nouveau texte"; SaveCopiedTitle = "Enregistrer le texte copié"; EditSavedTitle = "Modifier le texte enregistré";
        SavePictureTitle = "Enregistrer l'image copiée"; EditPictureTitle = "Modifier l'image enregistrée";
        PictureHintEmpty = "Sans nom, la liste montrera seulement l'image.";
        PictureHintNamed = "La liste montrera l'image avec « {0} » à côté.";
        NameLabel = "Nom (facultatif) :"; ContentLabel = "Contenu :";
        NameHintEmpty = "Sans nom, la liste montrera le contenu.";
        NameHintNamed = "La liste montrera « {0} » ; le contenu reste caché, ce qui convient à un mot de passe.";
        BoldTip = "Gras (Ctrl+B)"; ItalicTip = "Italique (Ctrl+I)"; UnderlineTip = "Souligné (Ctrl+U)";
        TypefaceTip = "Police"; PointsTip = "Taille"; PlainTip = "Supprimer la mise en forme : de la sélection, ou de tout le texte";
        DefaultLook = "Par défaut";
        FolderOf = "Dossier : "; NoFolder = "Aucun dossier"; Inside = "Dans : ";
        NewFolderTitle = "Nouveau dossier"; RenameFolderTitle = "Renommer le dossier"; FolderNameLabel = "Nom du dossier :";
        Save = "Enregistrer";

        PictureLabel = "Image {0}×{1}";
        PictureToday = "Image {0}×{1}, copiée à {2}"; PictureOn = "Image {0}×{1}, copiée le {2} à {3}";
        MoreOne = "… et un caractère de plus"; MoreMany = "… et {0} caractères de plus";

        ExportHeader = "{0} {1} : export du {2}";
        ExportClearWarning = "Attention : ici les textes sont en clair, mots de passe compris.";
        ExportEditNote = "Le texte de chaque élément est dans les lignes qui commencent par une tabulation : si vous "
            + "modifiez le fichier, laissez-la en place.";

        HelpTitle = "Guide de {0}"; OptionsTitle = "Options de {0}"; Close = "Fermer";
        Guide = FrenchGuide;
    }

    static string[] FrenchGuide()
    {
        string pick = MainForm.PickName, type = MainForm.HotkeyName, app = Program.AppName;
        return new string[]
        {
            "# À quoi ça sert",
            app + " garde ce que vous copiez et les textes que vous choisissez d'enregistrer, pour les remettre dans le "
                + "presse-papiers quand il le faut. Il sait aussi saisir le presse-papiers comme de vraies frappes de "
                + "touches : c'est ainsi qu'un mot de passe entre là où coller ne marche pas, par exemple sur les "
                + "écrans de verrouillage des bureaux à distance.",

            "# La liste",
            "À gauche la barre latérale, à droite les cartes des éléments de l'endroit choisi dans celle-ci :",
            "- « " + AllPlace + " » : tout, copié et enregistré, du plus récent au plus ancien, chaque jour sous son nom ; "
                + "les éléments gardés en haut passent avant les autres.",
            "- « " + PinnedPlace + " » : seulement les éléments copiés gardés en haut.",
            "- « " + SavedTitle + " » : les textes enregistrés, par nom. En dessous se trouvent leurs dossiers.",
            "- « " + SourcesPlace + " » : les programmes dans lesquels vous avez copié, chacun avec son icône et le nombre "
                + "d'éléments qui en viennent. Un clic sur l'un d'eux ne montre que ce que vous y avez copié ; un clic sur « "
                + SourcesPlace + " » ouvre ou ferme la liste.",
            "Le bouton en haut de la barre latérale la réduit à ses icônes, et lui rend ses noms : en laissant le pointeur "
                + "sur une icône, vous voyez son nom. En faisant glisser le bord entre la barre et les cartes, vous l'élargissez "
                + "ou la rétrécissez ; plus étroite qu'elle ne peut l'être, elle se réduit à ses icônes.",
            "Les onglets au-dessus des cartes (" + AllKinds + ", " + TextKinds + ", " + CodeKinds + ", " + LinkKinds + ", "
                + PictureKinds + ") ne montrent qu'un type d'élément. « " + CodeKinds + " » est une supposition faite d'après "
                + "l'allure des lignes : de temps en temps un texte s'y retrouve, ou un morceau de code parmi les textes. "
                + "Quand elle se trompe, « " + KindMenu + " », dans le menu d'un texte, dit ce qu'il est : copié à "
                + "nouveau ou enregistré, il le reste.",
            "Chaque carte montre d'abord l'élément ; en bas à droite, en petit et discret, le programme dans lequel il a été copié, avec "
                + "son icône (ou le dossier d'un texte enregistré), l'heure de la copie et ce qu'il est. À droite, de petits "
                + "signes le montrent en entier, le copient, l'enregistrent ou le modifient, le suppriment et ouvrent son menu : en laissant le "
                + "pointeur sur l'un d'eux, il dit ce qu'il fait.",
            "- Tapez dans la case du haut pour chercher parmi les éléments de l'endroit : les mots comptent dans "
                + "n'importe quel ordre, sans tenir compte des majuscules ni des accents.",
            "- Un clic sur une carte la sélectionne. Le signe de copie (« " + CopyButton + " ») met l'élément dans le presse-papiers et laisse la "
                + "fenêtre ouverte, pour que vous puissiez encore l'enregistrer ou le supprimer.",
            "- " + EnterKeyName + " ou un double-clic le mettent dans le presse-papiers et, si vous aviez ouvert la "
                + "liste avec " + pick + ", vous ramènent à la fenêtre où vous travailliez.",
            "- Collez-le alors avec Ctrl+V, ou saisissez-le avec " + type + ".",
            "- L'œil (« " + ShowFull + " », aussi dans le menu) montre l'élément en entier dans un petit cadre : le texte avec "
                + "toutes ses lignes, l'image en plus grand. Il reste quelques secondes, où que vous alliez avec le pointeur. "
                + "Avec « " + RestOption + " » activée, dans les options, laisser le pointeur sur une carte qui coupe son texte "
                + "fait de même, jusqu'à ce que vous bougiez le pointeur.",
            "- Les trois points à droite, ou le clic droit, ouvrent le menu de l'élément, sans toucher au presse-papiers.",
            "- « " + CopyPictureText + " », dans le menu d'une image, met dans le presse-papiers le texte "
                + "que Windows y lit, ici sur le PC, sans internet : il entre dans la liste comme tout ce que vous copiez "
                + "(celui d'une image enregistrée est privé, comme un texte enregistré). Un petit texte peut sortir avec "
                + "des erreurs : vérifiez-le d'abord.",
            "- La flèche à droite d'un élément copié (" + Key(Keys.Control | MainForm.PinKey) + ") le garde "
                + "en haut un moment : elle est en couleur tant qu'elle le fait, et un clic l'allume ou l'éteint. "
                + "L'élément part quand même quand les jours de l'historique expirent : ce qui doit rester, "
                + "enregistrez-le. Copier à nouveau le même texte le ramène en haut avec la nouvelle heure, flèche et "
                + "numéro compris.",
            "- « " + NumberMenu + " », dans le menu, donne à un texte enregistré ou à un élément copié "
                + "un numéro de 1 à " + MainForm.MaxNumber + " : ensuite, depuis n'importe quel programme, "
                + MainForm.NumbersName + " le collent aussitôt dans la fenêtre où vous travaillez, sans ouvrir celle-ci "
                + "(l'élément va dans le presse-papiers et Ctrl+V suit tout seul). Le numéro apparaît dans un petit "
                + "cadre à droite de la carte, et à côté du texte enregistré dans le menu de l'icône. Un élément copié "
                + "garde son numéro jusqu'à ce que ses jours expirent. Chaque numéro appartient à un seul élément : "
                + "donné à un autre, il passe à celui-ci.",
            "Pour ne pas mémoriser ce que vous allez copier, « " + Pause + " » est en bas de la barre latérale "
                + "et dans le menu de l'icône près de l'horloge : le titre indique « en pause » "
                + "jusqu'à ce que vous choisissiez « " + Resume + " ». Au démarrage suivant, la capture est de nouveau active.",
            "La liste montre les " + MainForm.MaxShown + " éléments copiés les plus récents : la recherche trouve les autres.",

            "# Les textes enregistrés",
            "Ce sont les choses que vous voulez toujours avoir sous la main : une signature, une adresse, un mot de "
                + "passe. Ils n'expirent jamais. Le nom est facultatif : avec un nom, la liste ne montre que le nom et le "
                + "contenu reste caché, ce qui convient à un mot de passe ; sans, la liste montre le contenu, comme pour "
                + "une deuxième adresse e-mail.",
            "- « " + New + " » (" + Key(Keys.Control | MainForm.NewKey) + ") écrit un texte de zéro.",
            "- « " + SaveDots + " » (" + Key(Keys.Control | MainForm.SaveKey) + "), sur un élément copié, le garde, avec "
                + "ou sans nom ; avant d'enregistrer, vous pouvez retoucher son contenu.",
            "- « " + EditDots + " » (" + Key(Keys.Control | MainForm.SaveKey) + "), sur un élément enregistré, change "
                + "son nom ou son contenu.",
            "- « " + Delete + " » (" + Key(Keys.Control | MainForm.DeleteKey) + ") supprime le texte enregistré, ou "
                + "retire l'élément copié de la liste.",
            "- L'œil d'un texte enregistré avec un nom (« " + ShowContent + " », aussi dans son menu) montre son contenu dans "
                + "un petit cadre pendant quelques secondes : la liste ne montre que son nom, et le survoler ne suffit jamais.",
            "- Les textes enregistrés sont aussi dans le menu de l'icône près de l'horloge : en choisir un le remet "
                + "dans le presse-papiers sans ouvrir la fenêtre.",
            "Les textes enregistrés n'expirent jamais. Ils sont dans la base de données du programme, chiffrés pour "
                + "votre utilisateur : un autre utilisateur du même PC ne peut pas les lire. Quand vous en remettez un dans le presse-papiers, il "
                + "est marqué comme privé, pour que l'historique de Windows (Win+V) et les autres gestionnaires de "
                + "presse-papiers l'ignorent. Une image copiée peut aussi être enregistrée : elle montre "
                + "toujours sa miniature, avec son nom à côté si elle en a un.",
            "Un texte enregistré peut être mis en forme, comme une signature de courriel : gras, italique, souligné, polices et tailles.",
            "- Copiez la signature depuis un courriel ou un document et enregistrez-la : elle garde sa mise en forme. "
                + "L'historique garde le texte brut, donc un élément copié l'a encore seulement tant qu'il est parmi les "
                + Rich.KeptCopies + " dernières copies depuis le démarrage du programme.",
            "- Dans la fenêtre d'un texte, les boutons au-dessus du contenu mettent la sélection en gras (Ctrl+B), en "
                + "italique (Ctrl+I) ou soulignée (Ctrl+U), lui donnent une police et une taille, ou lui ôtent sa mise en "
                + "forme (sans sélection, à tout le texte). Ce que vous y collez garde sa mise en forme.",
            "- Choisi, un texte mis en forme va dans le presse-papiers mis en forme et en texte brut : Ctrl+V dans un "
                + "courriel le colle mis en forme, là où la mise en forme ne compte pas arrive le texte brut, et " + type
                + " tape le texte brut.",
            "- Tant que vous ne le modifiez pas, il reste exactement tel qu'il a été copié, liens et images compris. "
                + "Une fois modifié, il garde seulement ce que les boutons connaissent : liens, images, alignement et "
                + "listes sont perdus.",

            "# Les dossiers",
            "Quand les textes enregistrés deviennent nombreux, vous pouvez les regrouper en dossiers, et les dossiers "
                + "dans d'autres dossiers. Ils sont dans la barre latérale, sous « " + SavedTitle + " » : un clic sur un "
                + "dossier montre ses textes, et ceux des dossiers qu'il contient.",
            "- « " + NewFolder + " » est en bas de la barre latérale et dans le menu du clic droit d'un dossier "
                + "(" + Key(Keys.Control | Keys.Shift | MainForm.NewKey) + ") : il est créé dans le dossier "
                + "affiché, mais sa fenêtre permet de choisir où le mettre.",
            "- La flèche à l'extrémité droite d'un dossier qui en contient d'autres l'ouvre et le ferme ; celle de « "
                + SavedTitle + " » les montre ou les cache tous. Le nombre dit combien de textes contient un dossier, y "
                + "compris dans les dossiers qu'il contient. Ouverts ou fermés, vous les retrouvez comme vous les avez laissés.",
            "- La carte d'un texte enregistré, ou un dossier de la barre avec tout son contenu, se fait glisser sur un "
                + "dossier de la barre ; lâché sur « " + SavedTitle + " », il sort des dossiers.",
            "- « " + MoveTo + " », dans le menu d'un texte enregistré ou d'un dossier, fait de même. Le "
                + "même choix est dans la fenêtre où vous l'écrivez, à gauche des boutons.",
            "- Dans le menu du clic droit d'un dossier, « " + RenameDots + " » change son nom ou sa place et « " + Delete
                + " » retire seulement le dossier : son contenu, textes et dossiers, remonte dans le dossier qui le "
                + "contenait, ou reste parmi les textes enregistrés hors des dossiers.",
            "- Le nom d'un dossier ne peut pas contenir « " + Texts.Separator + " ».",
            "- Dans le menu de l'icône près de l'horloge, chaque dossier est un sous-menu avec ses textes.",
            "- Le nom d'un dossier trouve ses textes dans la recherche.",

            "# Saisir un mot de passe",
            "Là où Ctrl+V ne marche pas, " + app + " écrit le texte du presse-papiers une touche à la fois, comme vous "
                + "le feriez au clavier.",
            "- Copiez le mot de passe.",
            "- Appuyez sur « " + TypeButton + " ».",
            "- Cliquez dans le champ de la fenêtre distante : vous avez " + MainForm.ArmedSeconds + " secondes pour le faire.",
            "- Quand cette fenêtre est restée au premier plan le temps de l'attente, la saisie commence.",
            "Sinon, appuyez sur " + type + " directement dans le champ : la saisie démarre aussitôt, sans attente.",
            "Pour ne pas envoyer un mot de passe au mauvais endroit :",
            "- la saisie s'arrête toute seule si la fenêtre cible perd le premier plan ;",
            "- pendant l'attente et la saisie, le même bouton devient « " + Cancel + " » ;",
            "- le bureau, la barre des tâches et le menu Démarrer ne sont jamais une cible ;",
            "- si le presse-papiers contient plus de " + MainForm.UnusualLength + " caractères ou plusieurs lignes, une "
                + "confirmation est demandée ;",
            "- rien n'est saisi tant que Verr. Maj est activé : désactivez-le d'abord.",

            "# Les touches",
            pick + "\tOuvre la liste sur " + AllPlace + ", depuis n'importe quel programme.",
            type + "\tSaisit aussitôt le presse-papiers dans la fenêtre active.",
            MainForm.NumbersName + "\tColle dans la fenêtre active l'élément qui a ce numéro.",
            "F1\tOuvre ce guide.",
            ArrowsName + "\tParcourt les éléments, ou les endroits de la barre latérale.",
            EnterKeyName + "\tChoisit l'élément et revient à la fenêtre de départ.",
            "Échap\tRevient à la fenêtre de départ sans choisir.",
            Key(Keys.Control | MainForm.SearchKey) + "\tAmène le curseur dans la zone de recherche.",
            Key(Keys.Control | MainForm.NewKey) + "\tNouveau texte enregistré.",
            Key(Keys.Control | Keys.Shift | MainForm.NewKey) + "\tNouveau dossier.",
            Key(Keys.Control | MainForm.SaveKey) + "\tEnregistre l'élément copié, ou modifie l'élément enregistré.",
            Key(Keys.Control | MainForm.DeleteKey) + "\tSupprime l'élément sélectionné.",
            Key(Keys.Control | MainForm.PinKey) + "\tGarde l'élément copié en haut, ou le laisse partir.",
            "Les trois premiers marchent partout ; les autres dans la liste. Tant qu'il est actif, " + pick
                + " n'arrive plus aux autres programmes, pas plus que les touches des numéros qu'un élément possède.",
            "Dans les options, sous « " + HotkeysTitle + " », les trois premiers se désactivent et se changent : le "
                + "bouton à côté de chacun montre ses touches, et en appuyant dessus il attend les nouvelles. Pour les "
                + "numéros, on appuie sur les touches à maintenir avec un chiffre quelconque ; si un autre programme en "
                + "a déjà certaines, il dit lesquelles.",
            "- Il faut Ctrl ou Alt, aussi avec " + ShiftName + ", avec une lettre, un chiffre ou une touche de fonction.",
            "- Échap garde les touches actuelles, " + DeleteKeyName + " revient à celles de départ.",
            "- Avec Ctrl+Alt, évitez les lettres qui écrivent un symbole avec AltGr, comme le E de l'euro : le "
                + "raccourci se déclencherait pendant que vous l'écrivez.",

            "# L'historique",
            "Ce que vous copiez reste dans la liste pendant un nombre de jours que vous choisissez ("
                + Texts.DefaultKeepDays + " si vous ne le changez pas), même après avoir fermé le programme ou éteint le PC.",
            "- Il est gardé en clair dans une base de données, par défaut " + Clips.DefaultFile + ", avec les textes "
                + "enregistrés, qui eux sont chiffrés. Les jours expirés s'effacent tout seuls.",
            "- Dans les options, « " + ChangeFolderButton + " » emmène la base de données dans un autre dossier, ou "
                + "utilise celle qui s'y trouve déjà ; « " + DefaultFolderButton + " » la ramène. Ce n'est pas un moyen "
                + "de partager la liste entre PC : les textes enregistrés ne se lisent que sur ce PC. Si le dossier "
                + "choisi est inaccessible au démarrage, la liste travaille en mémoire et le dit.",
            "- Avec 0 jour, seul ce que vous avez copié aujourd'hui reste.",
            "- Les images copiées occupent au maximum " + Size(Clips.PictureSpace) + " : au-delà, les plus anciennes "
                + "partent même avant la fin de leurs jours, et une barre rouge sous la liste le signale. Les images "
                + "enregistrées ne comptent pas.",
            "- Ce que les gestionnaires de mots de passe marquent comme privé n'est pas mémorisé.",
            "- Les éléments en haut partent aussi quand leurs jours expirent : ce qui doit rester, enregistrez-le.",
            "- Dans les options, « " + CleanButton + " » efface aussitôt ce que vous avez copié avant les derniers jours "
                + "indiqués à côté : avec 1, seul aujourd'hui reste ; avec 0, rien. Une confirmation est demandée ; les "
                + "éléments en haut partent aussi, les textes enregistrés ne sont jamais touchés.",
            "- Désactiver l'historique dans les options vide la liste et efface du disque ce que vous avez copié.",

            "# Les options",
            "Le bouton « " + Options + " » les ouvre dans une fenêtre à part, qui peut rester ouverte à côté de la liste. "
                + "Chaque option dit en quelques mots, sous son nom, ce qu'elle fait ; en laissant le pointeur dessus, son "
                + "explication complète apparaît. Elles sont réparties en cinq pages, choisies dans la colonne de gauche comme "
                + "les sujets de ce guide :",
            "- " + TypingTitle + " : combien de secondes attendre avant la saisie (de " + MainForm.MinDelay + " à "
                + MainForm.MaxDelay + "), si le serveur a un clavier américain, s'il faut appuyer sur Entrée à la fin, "
                + "s'il faut vider le presse-papiers ensuite, la saisie lente et les symboles composés avec des codes Alt.",
            "- " + HotkeysTitle + " : active et désactive " + type + ", " + pick + " et " + MainForm.NumbersName
                + ", et leur donne d'autres touches ; et si " + pick + " rouvre la liste là où vous l'avez laissée.",
            "- " + HistoryTitle + " : s'il faut mémoriser ce que vous copiez, pendant combien de jours, le dossier de la base de données "
                + "(qu'on peut changer) avec le lien qui l'ouvre, et le bouton qui nettoie aussitôt l'historique.",
            "- " + AppearanceTitle + " : la langue, le thème (clair, sombre ou « " + ThemeSystem + " »), la police et la "
                + "taille de l'interface (boutons, options, ce guide : en pixels, elles redémarrent le programme), celles des "
                + "éléments (elles changent aussitôt), et si laisser le pointeur sur une carte la montre en entier.",
            "- " + ProgramTitle + " : s'il faut rester dans la zone de notification et se masquer en passant à un autre "
                + "programme, s'il faut démarrer avec Windows, les boutons pour exporter et importer, et la version du programme.",

            "# Exporter et importer",
            "Dans les options, sous « " + ProgramTitle + " », deux boutons sortent vos textes du programme et les "
                + "reprennent, comme sauvegarde ou pour les passer à un autre PC. Chacun demande si vous voulez "
                + "seulement les textes enregistrés ou tout, c'est-à-dire aussi les éléments copiés.",
            "- « " + ExportButton + " » écrit un fichier texte que le Bloc-notes ouvre : les textes enregistrés avec "
                + "leurs dossiers (ceux mis en forme avec leur HTML avant le texte) et, si vous le demandez, les "
                + "éléments copiés avec leur heure et leur flèche. Les "
                + "images n'y vont pas.",
            "- Dans le fichier, les textes sont en clair, mots de passe compris : gardez-le en lieu sûr.",
            "- « " + ImportButton + " » lit un tel fichier et ajoute son contenu à ce qui existe déjà. Si un texte "
                + "enregistré porte le nom d'un texte existant, il demande lequel garder ; les éléments copiés que la "
                + "liste a déjà ne sont pas répétés. Un fichier exporté dans une autre langue se lit de la même façon.",
            "- Le fichier peut être retouché à la main : le texte de chaque élément est dans les lignes qui commencent "
                + "par une tabulation, qui doit rester en place.",

            "# L'icône de notification",
            "Avec l'option active, fermer ou réduire la fenêtre la masque seulement : le programme continue de tourner "
                + "et les raccourcis continuent de répondre.",
            "- Un clic sur l'icône rouvre la fenêtre ; clic droit puis « " + Exit + " » ferme vraiment.",
            "- Avec « " + LeaveOption + " », la fenêtre y retourne aussi quand vous cliquez ailleurs ; désactivée, elle "
                + "reste ouverte derrière les autres. Pendant l'attente et la saisie, en revanche, elle reste au-dessus "
                + "de tout : le compte à rebours et « " + Cancel + " » restent visibles.",
            "- Le menu du clic droit contient aussi les textes enregistrés, chaque dossier un sous-menu : en choisir un "
                + "le remet dans le presse-papiers, prêt pour Ctrl+V ou pour " + type + ", sans ouvrir la fenêtre.",
            "- À côté d'un texte enregistré qui a un numéro figurent ses touches.",
            "- L'icône est en couleur quand le presse-papiers contient du texte, grise sinon.",
            "- Lancer le programme une seconde fois fait réapparaître la fenêtre de celui déjà ouvert.",

            "# Si quelque chose ne va pas",
            "- Les symboles arrivent faux : le serveur utilise un autre clavier. Essayez l'option du clavier anglais (US).",
            "- Des caractères manquent, ou arrivent dans le désordre : activez la saisie lente.",
            "- Les symboles que votre clavier n'a pas n'arrivent pas (~ et ` sur un clavier italien) : activez les "
                + "codes Alt, qui marchent si le serveur est sous Windows.",
            "- " + type + " ne fait rien avec un bureau à distance en plein écran : ce programme capture tous les "
                + "raccourcis. Utilisez le bouton.",
            "- Windows bloque l'envoi des touches : le programme de bureau à distance est lancé en tant "
                + "qu'administrateur. Lancez aussi " + app + " ainsi.",
            "- Un raccourci se désactive tout seul : un autre programme l'utilise déjà. Donnez-lui d'autres touches "
                + "dans les options.",
            "La ligne sous les cartes dit ce qui vient de se passer, et pourquoi. Les confirmations disparaissent "
                + "après quelques secondes et laissent la place à la liste ; les avertissements et les erreurs restent "
                + "jusqu'au message suivant."
        };
    }
}
