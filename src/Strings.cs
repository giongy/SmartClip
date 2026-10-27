using System;
using System.Globalization;
using System.Windows.Forms;

// Every text the program shows. The values here are English, the default language; each other language has a
// file of its own (Strings.Italian.cs and the others) whose method replaces them, so that a text a language
// lacks shows in English rather than not at all. The language is chosen once, when the program starts (Use):
// changing it in the options starts the program again. Texts with {0}, {1}... are filled by string.Format.
static partial class Strings
{
    // The languages: their codes, the name each gives itself, and the culture that writes their dates.
    public static readonly string[] Codes = { "en", "es", "fr", "it" };
    public static readonly string[] Names = { "English", "Español", "Français", "Italiano" };
    static readonly string[] Cultures = { "en-US", "es-ES", "fr-FR", "it-IT" };
    public static string Code = Codes[0];
    public static CultureInfo Culture = CultureInfo.GetCultureInfo(Cultures[0]);

    // Switches to a language by its code; an unknown one leaves English. Once, before any window opens.
    public static void Use(string code)
    {
        int index = Array.IndexOf(Codes, code);
        if (index <= 0) return;
        if (code == "it") Italian();
        else if (code == "es") Spanish();
        else if (code == "fr") French();
        Code = code;
        Culture = CultureInfo.GetCultureInfo(Cultures[index]);
    }

    // A key combination as this language names it: "Ctrl+Shift+N".
    static string Key(Keys keys)
    {
        return MainForm.NameOf(keys);
    }

    // --- Keys, as the keyboards of each language label them ---
    public static string ShiftName = "Shift", DeleteKeyName = "Del", EnterKeyName = "Enter", ArrowsName = "Up / Down";
    public static string EscapeKeyName = "Esc";
    public static string WithKeys = "{0} ({1})";   // what a button does, and its keys

    // --- Dates, quotes and sizes ---
    public static string DayFormat = "d MMM", YearDayFormat = "d MMM yy";   // a day in a row of the list
    public static string Quote = "“{0}”";
    public static string Megabytes = "{0} MB", Kilobytes = "{0} KB";

    // A size on disk as this language writes it: "12.4 MB", "830 KB".
    public static string Size(long bytes)
    {
        const double Kilo = 1024, Mega = Kilo * Kilo;
        return bytes >= Mega ? string.Format(Megabytes, (bytes / Mega).ToString("0.#", Culture))
            : string.Format(Kilobytes, Math.Ceiling(bytes / Kilo).ToString(Culture));
    }

    // --- Main window ---
    public static string TypeButton = "Type clipboard";
    public static string SavedTitle = "Saved", PausedSuffix = " (paused)";
    public static string New = "New…", SaveDots = "Save…", EditDots = "Edit…", RenameDots = "Rename…", Delete = "Delete";
    public static string Options = "Options", SearchCue = "Search…";
    public static string CountOf = "{0} of {1}";
    public static string NoResults = "No results", HistoryOffEmpty = "The history is off",
        PausedEmpty = "Paused: what you copy is not remembered", CopyHint = "What you copy will appear here",
        NoSaved = "No saved texts";
    public static string NewTip = "Writes a new text to keep, with a name or without ({0}).";
    // The sidebar, the tabs above the list, and the cards of the entries.
    public static string HistoryPlace = "History", PinnedPlace = "Pinned", SourcesPlace = "Source", GuideItem = "Help";
    public static string AllKinds = "All", TextKinds = "Text", CodeKinds = "Code", LinkKinds = "Links", PictureKinds = "Images";
    public static string TextBadge = "Text", CodeBadge = "Code", LinkBadge = "Link", PictureBadge = "Image";
    public static string CopyButton = "Copy", ShowFull = "Preview", MoreItem = "More";
    public static string Today = "Today", Yesterday = "Yesterday", Undated = "Earlier";
    public static string NoPinned = "Nothing kept at the top: the arrow of a copied entry brings it here for a while",
        NoneOfKind = "Nothing of this kind";
    public static string SourcesTip = "The programs you copied in: choose one to see only what you copied there.";
    public static string PinnedTip = "Copied entries kept at the top for a while: they still go when their days are over.";
    public static string ShortcutsPlace = "Shortcuts";
    public static string ShortcutsTip = "Entries with a number, which pastes them from anywhere: a click on the number changes it or takes it away.";
    public static string NoShortcuts = "No shortcuts: “{0}”, in the menu of an entry, gives it a number that pastes it from anywhere with {1}";
    public static string NumberTip = "{0} pastes it from anywhere: click to change it or take it away";
    public static string NarrowSideTip = "Reduces the sidebar to its icons. Its edge can also be dragged, to make it wider or narrower.";
    public static string WideSideTip = "Shows the names in the sidebar again.";
    public static string Error = "Error: {0}";
    public static string NewerDatabase = "{0} was written by a newer version of {1}: update the program to open it.";
    public static string UnexpectedError ="Something went wrong: {0}\n\nThe program goes on. The details are in {1}.";
    public static string SqliteMissing = "{0} cannot start: it keeps what you copy and save with SQLite, which is part of "
        + "Windows 10 and 11 (winsqlite3.dll), and this Windows does not have it.";
    public static string SqliteTooOld = "{0} cannot start: it needs SQLite {1} or later, which is part of Windows 10 and 11, "
        + "and this Windows has version {2}. Updating Windows solves it.";
    public static string PicturesFull = "The room for copied pictures ({0}) is full: the oldest are deleted, one at a time, "
        + "as you copy new ones.";
    // The bar of the cards picked together, in the place of the tabs: how many, and taking them back.
    public static string PickedCount = "{0} selected", PickNone = "Clear the selection";
    public static string CopyPickedTip = "Puts their texts on the clipboard, one after the other";

    // --- Menus ---
    public static string Open = "Open", Exit = "Exit", Pause = "Pause capture", Resume = "Resume capture";
    public static string ShowContent = "Show the content", MoveTo = "Move to", NewFolder = "New folder…",
        KeepTop = "Keep at the top", Unpin = "Release from the top", OutOfFolders = "Out of the folders", EmptyFolder = "Empty";
    public static string NumberMenu = "Shortcut", NoNumber = "None", KindMenu = "Type";
    public static string CopyPictureText = "Copy the text";
    public static string QrMenu = "QR code";
    public static string QrNote = "Scan it with your phone to have the text there.";

    // --- Tray ---
    public static string TrayReady = ": text ready on the clipboard", TrayEmpty = ": no text on the clipboard",
        TrayPaused = ", capture paused";
    public static string TrayHint = "I'm still running here. Click to reopen; right-click and then {0} to quit.";

    // --- What the status line says ---
    public static string StartupRefused = "Windows does not allow it: turn it on from Settings > Apps > Startup.";
    public static string StartupOn = "At Windows sign-in {0} will start by itself, without showing the window.";
    public static string StartupOff = "{0} no longer starts with Windows.";
    public static string PausedSay = "Paused: what you copy is not remembered until you resume capturing.";
    public static string ResumedSay = "Capturing has resumed: what you copy comes back to the list.";
    public static string Pinned = "Kept at the top: {0}.", Unpinned = "No longer at the top: {0}.";
    public static string PinnedMany = "{0} entries kept at the top.", UnpinnedMany = "{0} entries no longer at the top.";
    public static string DeletedMany = "{0} entries deleted.";
    // The texts of several cards copied together: then, after the tab, what to do with them.
    public static string JoinedOnClipboard = "{0} texts on the clipboard, one after the other.",
        JoinedWithoutPictures = "The pictures are left out.",
        PasteThem = "Paste them with Ctrl+V.", PasteOrTypeThem = "Paste them with Ctrl+V or type them with {0}.";
    public static string PickedSaved = "{0} entries are among the saved texts, outside the folders.",
        PickedInFolder = "{0} entries are in the folder “{1}”.";
    public static string KindChanged = "Marked as {1}: {0}.";
    public static string NumberGiven = "{0} now pastes “{1}” wherever you are.";
    public static string NumberTaken = "“{0}” no longer has a shortcut.";
    public static string Pasted = "Pasted into {1}: {0}.";
    public static string PasteFocusLost = "Not pasted: the window changed. The entry is on the clipboard.";
    // After the tab, what to do next: the status line writes it at its right end (InfoBar.Apart).
    public static string OnClipboard = "On the clipboard: {0}.\tPaste it with Ctrl+V.";
    public static string OnClipboardOrType = "On the clipboard: {0}.\tPaste it with Ctrl+V or type it with {1}.";
    public static string ReadingPicture = "Reading the text in the picture…";
    public static string NoPictureText = "Windows found no text in the picture.";
    public static string QrTooLong = "Too long for a QR code: one holds up to {0} characters, and an accented letter counts as two.";
    public static string NoRecognition = "Windows cannot read pictures in your languages: install the optical character recognition "
        + "of one from its options, in Settings > Time & language.";
    public static string AskReplace = "“{0}” already exists. Replace it?";
    public static string SavedSay = "Saved “{0}”.";
    public static string FolderNoSeparator = "The name of a folder cannot contain “{0}”.";
    public static string FolderExists = "The folder “{0}” already exists.";
    public static string FolderCreated = "Folder “{0}” created.", FolderRenamed = "The folder is now called “{0}”.",
        FolderMoved = "The folder is now “{0}”.";
    public static string LeftFolder = "“{0}” is no longer in a folder.", InFolder = "“{0}” is in the folder “{1}”.";
    public static string AskDeleteFolder = "Delete the folder “{0}”?";
    public static string DeleteFolderTop = "\n\nWhat it holds is not deleted: it stays among the saved texts, outside the folders.";
    public static string DeleteFolderUp = "\n\nWhat it holds is not deleted: it moves into the folder “{0}”.";
    public static string AskDelete = "Delete “{0}”?";
    public static string AskDeleteMany = "Delete {0} entries?", DeleteManySaved = "\n\nAmong them, saved texts: {0}.";
    // The buttons of the questions, which the program asks in a window of its own (AskForm).
    public static string Yes = "Yes", No = "No", Ok = "OK";

    // --- Typing ---
    public static string Cancel = "Cancel";
    public static string NoText = "There is no text on the clipboard: copy the text first.";
    public static string Cancelled = "Cancelled.";
    public static string CapsLock = "Caps Lock is on: turn it off and try again.";
    public static string TypingInto = "Typing into {0}…";
    public static string AskUnusual = "The clipboard holds {0}: longer than what is usually typed.\n\nType it anyway?";
    public static string OnLines = " on {0} lines";
    public static string Countdown = "Typing in {0} s into {1}. Press Cancel to stop.";
    public static string TimeUp = "Time is up: you did not choose the window to type into.";
    public static string ClickNow = "Click now into the field of the remote window…";
    public static string Untitled = "an untitled window";
    public static string KeysHeld = "Release the keys you are holding down and try again.";
    public static string Blocked = "Windows blocked the keystrokes. If the remote desktop program runs as administrator, "
        + "start {0} that way too.";
    public static string StoppedAfter = "Stopped after {0}.", FocusLost = "Stopped after {0}: the active window changed.";
    public static string DoneOne = "Done: 1 character typed.", DoneMany = "Done: {0} characters typed.";
    public static string AltCoded = " {0} with Alt codes.";
    public static string UnicodeOne = " Warning: 1 character with no key on the keyboard, sent as Unicode",
        UnicodeMany = " Warning: {0} characters with no key on the keyboard, sent as Unicode";
    public static string UnicodeHint = ": if the sign-in fails, try the Alt codes option.";
    public static string Character = "character", Characters = "characters";

    // --- Hotkeys ---
    public static string HotkeyTip = "{0} types the clipboard into the active window at once, with no wait.\n"
        + "It does not work if the remote desktop is full screen and captures every\n"
        + "hotkey: use the “{1}” button then.";
    public static string PickTip = "{0} brings this window with the list to the foreground, from any program.\n"
        + "Once an entry is chosen, control goes back to the window you came from: paste it\n"
        + "there with Ctrl+V or type it with {1}.\n"
        + "While it is on, {0} no longer reaches other programs.";
    public static string ChangeKeysNote = "\nThe button on the right shows its keys: press it to give it others.";
    public static string KeysTip = "Press here and then the new keys: Ctrl or Alt, also with {0}, together with a letter,\n"
        + "a digit or a function key. Esc keeps the current ones, {1} goes back to the defaults.\n"
        + "With Ctrl+Alt avoid letters that type a symbol with AltGr (E, for the euro): the\n"
        + "hotkey would fire while you type it.";
    public static string LeaveTip = "When you click another program the window goes back to the notification area,\n"
        + "as if closed: reopen it from the icon or with {0}. Off, it stays open behind the others.\n"
        + "It applies only with the option above on. It does not happen while it waits or types,\n"
        + "nor while you write a saved text or answer a question: meanwhile you can copy elsewhere.";
    public static string TypeTip = "Types the clipboard text one key at a time, where paste does not work:\n"
        + "copy the text, press here and click into the field of the remote window.\n"
        + "Or press {0} right in the field.";
    public static string SearchTip = "Type to search the cards of the place: words in any order, ignoring case and accents.\n"
        + "In " + HistoryPlace + " the saved texts too, after what you copied.\n"
        + "The arrows move through the cards.\n"
        + "“" + CopyButton + "”, {0} or a double click put the entry back on the clipboard: then paste it with\n"
        + "Ctrl+V or type it with {1}. With “" + CopyButton + "” the window stays open, with {0} or a double click\n"
        + "you go back where you were. A click only selects; right-click opens the entry's menu.\n"
        + "The eye at the right of a card shows it whole.\n"
        + "The arrow at the right of a copied entry ({2}) keeps it at the top of the list\n"
        + "for a while: it still goes when its days are over. What is to stay, save it.\n"
        + "Saved texts never expire and are kept encrypted for your user; with a name, only\n"
        + "the name is shown. Their folders are in the sidebar; “" + NewFolder + "” ({3}) is above\n"
        + "their cards: a text goes in by dragging it onto one, or with “" + MoveTo + "”.\n"
        + "“{5}”, in the right-click menu, gives an entry a number: then {4}\n"
        + "paste it wherever you are, without opening this window.";
    public static string PressKeys = "Press the keys…";
    public static string PressKeysStatus = "Press the new keys: Ctrl or Alt with a letter, a digit or a function key. "
        + "Esc keeps the current ones, {0} goes back to the defaults.";
    public static string KeysUnusable = "{0} won't do: it needs Ctrl or Alt with a letter, a digit or a function key.";
    public static string KeysTaken = "{0} is already used by the program: choose another.";
    public static string TypeNowWith = "To type the clipboard, now press {0}.", OpenNowWith = "To open the list, now press {0}.";
    public static string HotkeyBusyType = "{0} is already used by another program: give it other keys in the options, "
        + "or use the button.";
    public static string HotkeyBusyPick = "{0} is already used by another program: give it other keys in the options, "
        + "or open the window from the icon.";
    public static string QuickTip = "{0} paste the entry with that number into the window you are working in,\n"
        + "without opening this one: it goes on the clipboard and Ctrl+V follows by itself.\n"
        + "The numbers are given with “{1}”, in the right-click menu of an entry.\n"
        + "Only the keys of the numbers in use are taken: those no longer reach other programs.";
    public static string QuickKeysTip = "Press here and then the keys to hold, together with any digit: Ctrl or Alt,\n"
        + "also with {0}. The digit does not count: from 1 to 9, it picks the entry.\n"
        + "Esc keeps the current ones, {1} goes back to the defaults ({2}).\n"
        + "Ctrl+Alt is AltGr: on some keyboards it types symbols with the digits.";
    public static string PressQuickStatus = "Press the keys to hold with any digit, as {0}. "
        + "Esc keeps the current ones, {1} goes back to the defaults.";
    public static string QuickUnusable = "{0} won't do: hold Ctrl or Alt, also with Shift, and press a digit.";
    public static string QuickNowWith = "The numbered entries now paste with {0}.";
    public static string QuickBusy = "Already used by another program: {0}. Those numbers will not paste: choose other "
        + "keys in the options, or other numbers.";

    // --- Options ---
    public static string TypingTitle = "Typing", ProgramTitle = "Program", HotkeysTitle = "Hotkeys", HistoryTitle = "History";
    public static string AppearanceTitle = "Appearance";
    // Each option has its name and, under it, what it does in a few words: the tooltip says the rest.
    public static string DelayOption = "Wait before typing", DelayNote = "Seconds to click into the right field";
    public static string UsOption = "The server uses a US English keyboard", UsNote = "Only if symbols arrive wrong";
    public static string EnterOption = "Press Enter at the end", EnterNote = "Confirms the sign-in at once";
    public static string ClearOption = "Clear the clipboard after typing", ClearNote = "Once typing has succeeded";
    public static string SlowOption = "Slow typing", SlowNote = "If characters go missing or arrive out of order";
    public static string AltCodeOption = "Alt codes for symbols with no key", AltCodeNote = "Only for servers that run Windows";
    public static string LanguageOption = "Language", LanguageNote = "The program starts again in it";
    public static string ThemeOption = "Theme", ThemeNote = "Light or dark colours";
    public static string ThemeSystem = "Same as Windows", ThemeLight = "Light", ThemeDark = "Dark";
    public static string FaceOption = "Font of the interface", FaceNote = "Buttons, options, menus: the program starts again";
    public static string FontOption = "Font of the entries", FontNote = "What you copy and save: it changes at once";
    public static string RestOption = "Preview when the pointer rests", RestNote = "On a card cut short, without the eye";
    public static string LinesOption = "Height of the cards", LinesNote = "In lines of text: with one, more entries in view";
    public static string OneLine = "1 line", TwoLines = "2 lines";
    public static string RestTip = "On, the pointer resting on a card that cuts its entry short shows the entry whole.\n"
        + "Off, it shows only from the eye at the right of the card, or from its menu.";
    public static string Pixels = "{0} px";
    public static string TrayOption = "Stay in the notification area (tray)", TrayNote = "Closing the window leaves the program running";
    public static string LeaveOption = "Hide when switching to another program", LeaveNote = "Back to the notification area";
    public static string StartOption = "Start with Windows", StartNote = "Hidden in the notification area";
    public static string BackupOption = "Export and import", BackupNote = "Your texts in a file that Notepad opens";
    public static string ExportButton = "Export…", ImportButton = "Import…";
    public static string AboutNote = "Free software under the MIT license";
    public static string TypeNote = "Types into the active window at once";
    public static string PickOption = "Open the list", PickNote = "Brings this window up from any program";
    public static string WhereLeftOption = "Open where you left it", WhereLeftNote = "Otherwise the list opens on " + HistoryPlace;
    public static string WhereLeftTip = "Off, the hotkey of the list always opens it on " + HistoryPlace + ", every kind, with the search empty.\n"
        + "On, it shows the place and the kind you left it on.";
    public static string QuickOption = "Paste by number", QuickNote = "Pastes the entries that have a shortcut";
    public static string HistoryOption = "Remember copied texts and pictures", HistoryNote = "What you copy while the program is open";
    public static string KeepOption = "Days kept", KeepNote = "With 0, only what you copied today";
    public static string FolderLink = "Open the data folder";
    public static string DataFolderOption = "Folder of the copied and saved entries";
    public static string ChangeFolderButton = "Change…", DefaultFolderButton = "Default";
    public static string DatabaseSize = "Takes {0}; copied pictures: {1} of {2}.";
    public static string ChooseDataFolder = "The folder where {0} keeps what you copy and save.";
    public static string AskUseExisting = "{0} already holds a list of {1}.\n\nUse that one? Yours stays where it is, as it is.";
    public static string AskSyncedFolder = "{0} looks like a folder synchronised online or on the network.\n\nThe "
        + "synchronisation can lock or damage the database, and the saved texts cannot be read on another PC. Use it all the same?";
    public static string ListMovedTo = "The list is now kept in {0}.";
    public static string ListMoveFailed = "The list could not be moved, and stays where it was: {0}";
    public static string FolderUnavailable = "The folder of the list, {0}, cannot be reached: what you copy and save now is "
        + "lost when the program closes. In the options, under {1}, choose another folder or the default one.";
    public static string SpareOption = "Clean the history", SpareNote = "Except the last days written beside; 0: none";
    public static string CleanButton = "Clean now…";
    public static string DelayTip = "How many seconds the target window must stay in the foreground\n"
        + "before typing starts: your time to click into the right field,\nor to cancel.";
    public static string UsTip = "Leave it off for ordinary remote desktops: the keyboard of this PC applies.\n"
        + "Turn it on if symbols arrive wrong because the server assumes a US\n"
        + "keyboard (it happens with VMware consoles, iLO, iDRAC and some sign-in\nscreens).";
    public static string EnterTip = "Presses Enter after the last character, to confirm the sign-in at once.";
    public static string ClearTip = "Erases the typed text from the clipboard once typing has succeeded.\n"
        + "Windows clipboard history (Win+V), if on, is not touched.";
    public static string SlowTip = "Turn it on if characters arrive missing or out of order on the server.";
    public static string AltCodeTip = "Symbols this keyboard has no key for (for example ~ and ` on an Italian\n"
        + "keyboard) are normally sent as Unicode characters, which not every remote\n"
        + "desktop program accepts. With this option they are composed with Alt codes\n"
        + "on the numeric keypad instead: it works only if the server runs Windows.";
    public static string LanguageTip = "The language of the program. Changing it restarts {0}, which opens again at once.";
    public static string ThemeTip = "Light or dark colours. “{0}” follows the mode Windows sets for apps,\n"
        + "also when it changes while the program is open.";
    public static string FaceTip = "The font of the program itself: buttons, labels, options, the guide, menus and questions.\n"
        + "What you copied and saved has its own, below. Changing it restarts {0}.";
    public static string FaceSizeTip = "How large the program itself is written, in pixels; {0} by default. The windows grow\n"
        + "with the text. What you copied and saved has its own size, below. Changing it restarts {1}.";
    public static string FontTip = "The font of what you copied and saved: the rows of the lists, an entry shown whole\n"
        + "and the content of a saved text. The rest of the program keeps the font of the interface. It changes at once.";
    public static string SizeTip = "How large what you copied and saved is written, in pixels; {0} is the text of Windows 11.\n"
        + "The rest of the program keeps the size of the interface. It changes at once.";
    public static string LinesTip = "How tall every card of the list is, in lines of text, whatever it holds: texts, pictures, names.\n"
        + "With one line the lines of a text follow one another, a faint {0} between them; with two they stay as they are.\n"
        + "The eye at the right of a card shows whole what it cuts short. It changes at once.";
    public static string HistoryTip = "The list offers the texts and pictures copied while the program is open.\n"
        + "Copies that other programs mark as private are not remembered.\n"
        + "Turn it off if you copy private texts from programs that do not: they would be\n"
        + "readable in the list. Turning it off also deletes the history on disk.";
    public static string KeepTip = "How many days what you copy stays in the history, also after closing,\n"
        + "today being the first: with 0 only what you copied today stays.\n"
        + "Entries kept at the top go when their days are over too; saved texts never do.\n"
        + "The history is kept on disk unencrypted. The list shows the {0} most recent:\n"
        + "search finds the others.";
    public static string DataFolderTip = "Where the database with what you copy and save is kept, with the error log:\n{0}\n"
        + "The default is {1}. “Change…” moves the database to the folder you choose,\n"
        + "or uses the one already there. It is not a way to share the list between PCs:\n"
        + "saved texts can be read only by this Windows user on this PC, and two programs\n"
        + "using one file at once can damage it.";
    public static string CleanTip = "Deletes at once from the history what you copied before the last days\n"
        + "written above: with 1 only today stays, with 0 nothing does.\n"
        + "It asks first. Entries kept at the top go too; saved texts are not touched,\n"
        + "nor is the clipboard. The days kept stay as set.";
    public static string TrayTip = "On: closing or minimising the window keeps the program running as an icon\n"
        + "by the clock, and the hotkeys keep working.\n"
        + "Click the icon to reopen the window; right-click and then {0} to quit.\n"
        + "Off: an ordinary window, which ends the program when closed.";
    public static string StartTip = "Starts {0} when you sign in to Windows, already hidden in the notification area\n"
        + "(minimised, if the option above is off).\n"
        + "It applies to this executable: if you move it, the entry follows at the first start from there.\n"
        + "It is the same entry as Task Manager > Startup apps: off there, it shows off here too.";
    public static string ExportTip = "Writes the saved texts with their folders to a text file that Notepad opens;\n"
        + "if you ask, what was copied too. Pictures are left out.\n"
        + "In the file the texts are unencrypted, private ones included: keep it somewhere safe.";
    public static string ImportTip = "Reads a file written by “Export”, also on another PC, and adds what it holds\n"
        + "to what is already here. If a saved text has the name of an existing one, it asks which to keep.\n"
        + "Copied texts the list already has are not repeated.";
    public static string OnlySaved = "Saved texts only…", Everything = "Everything, copied items too…";
    public static string TextFiles = "Text files";
    public static string AskExport = "The file will hold the texts unencrypted, private ones included: anyone who opens it "
        + "can read them.\n\nContinue?";
    public static string SavedOne = "saved text", SavedMany = "saved texts", CopiedOne = "copied item", CopiedMany = "copied items";
    public static string Exported = "Exported: {0}.", ExportedAll = "Exported: {0}, {1}.";
    public static string PicturesLeftOut = " The pictures ({0}) are not exported.";
    public static string ClashOne = "A saved text in the file has the name of one that already exists.";
    public static string ClashMany = "{0} saved texts in the file have the name of one that already exists.";
    public static string ClashChoice = "\n\nYes: the one in the file wins.\nNo: the one here stays.";
    public static string NothingImported = "There was nothing to import in the file.";
    public static string Imported = "Imported: {0}.", ImportedAll = "Imported: {0}, {1}.";
    public static string KeptAsWere = " Left as they were: {0}.";
    public static string HistoryOffImport = " The history is off: the copied items were not imported.";
    public static string CleanEverything = "everything you copied", CleanBeforeToday = "what you copied before today",
        CleanBeforeDays = "what you copied before the last {0} days (today included)";
    public static string AskClean = "Delete {0}?\n\nSaved texts stay; entries kept at the top go too.";
    public static string NothingToClean = "Nothing to delete in the history.";
    public static string CleanedOne = "1 entry deleted from the history.", CleanedMany = "{0} entries deleted from the history.";
    public static string NoHistoryFolder = "The data folder does not exist yet: nothing is on disk.";
    public static string DatabaseUnavailable = "The list cannot be kept on disk ({0}): what you copy and save now is lost "
        + "when the program closes.";

    // --- The window of a saved text or a folder ---
    public static string NewTextTitle = "New text", SaveCopiedTitle = "Save the copied text", EditSavedTitle = "Edit the saved text";
    public static string SavePictureTitle = "Save the copied picture", EditPictureTitle = "Edit the saved picture";
    public static string PictureHintEmpty = "Without a name, the list shows the picture alone.";
    public static string PictureHintNamed = "The list shows the picture with “{0}” beside it.";
    public static string NameLabel = "Name (optional):", ContentLabel = "Content:";
    public static string NameHintEmpty = "Without a name, the list shows the content.";
    public static string NameHintNamed = "The list shows “{0}”; the content stays hidden, which suits a private text.";
    public static string BoldTip = "Bold (Ctrl+B)", ItalicTip = "Italic (Ctrl+I)", UnderlineTip = "Underline (Ctrl+U)";
    public static string TypefaceTip = "Font", PointsTip = "Size", PlainTip = "Remove the formatting: of the selection, or of all the text";
    public static string DefaultLook = "Default", Points = "{0} pt";   // in the menus of font and size
    public static string FolderOf = "Folder: ", NoFolder = "No folder", Inside = "Inside: ";
    public static string NewFolderTitle = "New folder", RenameFolderTitle = "Rename the folder", FolderNameLabel = "Folder name:";
    public static string Save = "Save";

    // --- Entries ---
    public static string PictureLabel = "Picture {0}×{1}";
    public static string PictureToday = "Picture {0}×{1}, copied at {2}", PictureOn = "Picture {0}×{1}, copied on {2} at {3}";
    public static string MoreOne = "… and one more character", MoreMany = "… and {0} more characters";

    // --- Export file ---
    public static string ExportHeader = "{0} {1}: export of {2}";
    public static string ExportClearWarning = "Warning: the texts here are unencrypted, private ones included.";
    public static string ExportEditNote = "The text of each entry is in the lines that start with a tab: if you edit the "
        + "file, leave it in place.";

    // --- The guide ---
    public static string HelpTitle = "{0} guide", OptionsTitle = "{0} options", Close = "Close";
    public static string ExpandAll = "Open all", CollapseAll = "Close all";   // the sections of a subject of the guide
    public static Func<string[]> Guide = EnglishGuide;

    // A line starting with "# " begins a subject and names it, one starting with "## " a section of it, which the
    // guide shows as a card that opens and closes: the lines before the first section are the few that say what the
    // subject is about, always in view. One starting with "- " is a point of a list, one with a tab a key and what it
    // does. Every language has the same subjects with the same sections, in the same order. Built when the guide
    // opens: the user can change the hotkeys.
    static string[] EnglishGuide()
    {
        string pick = MainForm.PickName, type = MainForm.HotkeyName, app = Program.AppName;
        return new string[]
        {
            "# What it is for",
            app + " keeps what you copy and the texts you choose to save, to put them back on the clipboard when "
                + "you need them. It can also type the clipboard as real key presses: that is how a text gets "
                + "in where paste does not work, for example on the lock screens of remote desktops.",

            "# The list",
            "On the left the sidebar, on the right the cards of the place chosen there.",
            "## The places of the sidebar",
            "- " + HistoryPlace + ": what you copied, newest first, each day under its name; the entries kept at the top come "
                + "before the others. While you scroll, the name of the day of the top card stays above the cards.",
            "- " + PinnedPlace + ": only the copied entries kept at the top.",
            "- " + ShortcutsPlace + ": the entries with a number, copied and saved, by number. A click on the number of a card "
                + "changes it or takes it away, here and everywhere else.",
            "- " + SavedTitle + ": the saved texts, by name. Under it are their folders.",
            "- " + SourcesPlace + ": the programs you copied in, that of the latest copy first, each with its icon and how many entries come from it. A "
                + "click on one shows only what you copied there; a click on " + SourcesPlace + " opens or closes the list. "
                + "The programs of a Citrix server all take the name of Citrix on this PC, but each keeps its own icon.",
            "The button at the top of the sidebar reduces it to its icons, and gives it its names back: resting the pointer "
                + "on an icon says its name, and each folder shows the first letter of its name. Dragging the edge between the sidebar and the cards makes it wider or narrower; "
                + "dragged narrower than it can be, it is reduced to its icons.",
            "## Kinds of entry",
            "The tabs above the cards (" + AllKinds + ", " + TextKinds + ", " + CodeKinds + ", " + LinkKinds + ", "
                + PictureKinds + ") show one kind of entry only. " + CodeKinds + " is a guess from how the lines look: now "
                + "and then a text ends up there, or a piece of code among the texts. When it guesses wrong, “" + KindMenu
                + "”, in the menu of a text, says what it is: copied again or saved, it stays so.",
            "## The cards",
            "Each card shows the entry first; on its right, small and faint, the program it was copied in, with its icon (or the folder "
                + "of a saved text) and the time of the copy. Code is written in a font of fixed width, a link underlined in blue. "
                + "With the pointer on the card, "
                + "small signs take their place: they keep it at the top, show it whole, copy it, save or edit it, delete it and "
                + "open its menu; resting the pointer on one says what it does.",
            "## Search",
            "Type in the box at the top to search the cards of the place: words count in any order, ignoring case and accents. "
                + "In " + HistoryPlace + " the saved texts that match come too, after what you copied, under " + SavedTitle + ". "
                + "The words found are highlighted on the cards; found far into a long text, its card shows that part of it.",
            "The list shows the " + MainForm.MaxShown + " most recent copied entries: search finds the others.",
            "## Using an entry",
            "- A click on a card selects it. The copy sign (“" + CopyButton + "”, or " + Key(Keys.Control | MainForm.CopyKey)
                + ") puts the entry on the clipboard and leaves the window open, so that you can still save or delete it.",
            "- " + EnterKeyName + " or a double click put it on the clipboard and, if you opened the list with " + pick
                + ", take you back to the window you were working in.",
            "- Then paste it with Ctrl+V, or type it with " + type + ".",
            "- The eye (“" + ShowFull + "”, also in the menu) shows the entry whole in a small box: the text with all its lines, "
                + "the picture larger. It stays a few seconds, wherever the pointer goes. With “" + RestOption + "” on, in the "
                + "options, resting the pointer on a card that cuts its text short does the same, until the pointer moves.",
            "- The three dots on the right, or right-click, open the menu of the entry, without touching the clipboard.",
            "## Several cards at once",
            "Ctrl+click adds a card to the selection or takes it away, " + ShiftName + "+click selects all the cards up to that one "
                + "(" + ShiftName + " with the arrows too), " + Key(Keys.Control | MainForm.AllKey) + " selects them all. With several "
                + "selected, a bar in the place of the tabs says how many and offers “" + CopyButton + "”, “" + KeepTop + "”, “"
                + MoveTo + "” and “" + Delete + "”, which are also in the right-click menu and on their keys. “" + CopyButton
                + "” puts their texts on the clipboard from the oldest to the latest, with an empty line between them; "
                + "the pictures are left out. “" + MoveTo + "” a folder, or " + SavedTitle
                + ", saves the copied entries there. " + EscapeKeyName + ", or the cross at the end of the bar, goes back to one card.",
            "## The text of a picture",
            "“" + CopyPictureText + "”, in the menu of a picture, puts on the clipboard the text Windows "
                + "reads in it, here on the PC, without the internet: it comes into the list as anything you copy (that of "
                + "a saved picture is private, as a saved text is). Small text can come out with mistakes: look at it first.",
            "## To the phone",
            "“" + QrMenu + "”, in the menu of a text, shows it as a QR code: scan it with your phone (its camera, or an app "
                + "that reads QR codes) and the text is there, to copy, or to open if it is a link. Nothing goes through the "
                + "internet. A code holds up to " + Qr.MaxBytes + " characters, and an accented letter counts as two; the "
                + "longer the text, the denser the code: if the phone struggles, bring it closer.",
            "## Keeping an entry at the top",
            "The arrow on the right of a copied entry (" + Key(Keys.Control | MainForm.PinKey) + ") keeps it at "
                + "the top for a while: in colour while it does, and a click turns it on or off. The entry still goes when "
                + "the days of the history are over: what is to stay, save it. Copying the same text again brings it back "
                + "to the top with the new time, arrow and number included.",
            "## Numbers",
            "“" + NumberMenu + "”, in the menu, gives a saved text or a copied entry a number from 1 to "
                + MainForm.MaxNumber + ": then, from any program, " + MainForm.NumbersName + " paste it at once into "
                + "the window you are working in, without opening this one (the entry goes on the clipboard and Ctrl+V "
                + "follows by itself). The number shows in a small frame on the right of the card, and beside the saved "
                + "text in the icon menu. A copied entry keeps its number until its days are over. Each number "
                + "belongs to one entry: given to another, it moves.",
            "## Pausing",
            "Not to remember what you are about to copy, “" + Pause + "” is at the bottom of the sidebar and "
                + "in the menu of the icon by the clock: the title says “paused” until you choose “" + Resume + "”. At "
                + "the next start capturing is on again.",

            "# Saved texts",
            "They are what you want always at hand: a signature, an address, an access code. They never expire. The "
                + "name is optional: with one, the list shows only the name and the content stays hidden, which suits "
                + "a private text; without, the list shows the content, as for a second email address.",
            "## Writing and changing them",
            "- “" + New + "” (" + Key(Keys.Control | MainForm.NewKey) + ") writes a text from scratch.",
            "- “" + SaveDots + "” (" + Key(Keys.Control | MainForm.SaveKey) + "), on a copied entry, keeps it, with "
                + "a name or without; before saving you can touch up its content.",
            "- “" + EditDots + "” (" + Key(Keys.Control | MainForm.SaveKey) + "), on a saved entry, changes its name or content.",
            "- “" + Delete + "” (" + Key(Keys.Control | MainForm.DeleteKey) + ") deletes the saved text, or drops the "
                + "copied entry from the list.",
            "## Seeing and using them",
            "- The eye of a saved text with a name (“" + ShowContent + "”, also in its menu) shows its content in a small box "
                + "for a few seconds: the list shows only its name, and resting the pointer on it is never enough.",
            "- Saved texts are also in the menu of the icon by the clock: choosing one puts it back on the "
                + "clipboard without opening the window.",
            "## Kept safe",
            "Saved texts never expire. They are kept in the program's database, encrypted for your user: another "
                + "user of the same PC cannot read them. When you put one back on the clipboard it is marked as private, so that Windows "
                + "clipboard history (Win+V) and other clipboard managers ignore it. A copied picture can be saved too: it always shows its thumbnail, with "
                + "its name beside it if it has one.",
            "## Formatted texts",
            "A saved text can be formatted, as a mail signature is: bold, italic, underlined, fonts and sizes.",
            "- Copy the signature from a mail or a document and save it: it keeps its formatting. The history keeps "
                + "plain text, so a copied entry still has it only while it is among the last " + Rich.KeptCopies
                + " copies since the program started.",
            "- In the window of a text, the buttons above the content make the selection bold (Ctrl+B), italic "
                + "(Ctrl+I) or underlined (Ctrl+U), give it a font and a size, or take its formatting away (with nothing "
                + "selected, from all the text). What you paste there keeps its formatting.",
            "- Chosen, a formatted text goes on the clipboard both formatted and plain: Ctrl+V in a mail pastes it "
                + "formatted, where formatting does not count the plain text arrives, and " + type + " types the plain text.",
            "- As long as you do not change it, it stays exactly as it was copied, links and pictures included. Once "
                + "changed, it keeps only what the buttons know: links, pictures, alignment and lists are lost.",

            "# Folders",
            "When the saved texts become many you can gather them in folders, and folders in other folders. They are in "
                + "the sidebar, under " + SavedTitle + ", and among the cards, as in File Explorer: " + SavedTitle + " shows its "
                + "folders and then the texts outside them, a folder the folders and the texts in it itself, not those of the "
                + "folders inside it. A search looks inside the folders too.",
            "## Making and opening them",
            "- “" + NewFolder + "” is at the right end of the tabs above the saved texts and in the right-click menu of a folder "
                + "(" + Key(Keys.Control | Keys.Shift | MainForm.NewKey) + "): it is created inside the folder shown, "
                + "but its window lets you choose where it goes.",
            "- The card of a folder opens it with a double click or Enter, as a click on its row in the sidebar does.",
            "- The arrow at the right end of a folder that holds others opens and closes it; that of " + SavedTitle
                + " shows or hides all of them. The number says how many texts are in a folder itself, not in the folders "
                + "inside it; that of " + SavedTitle + ", those outside the folders. Open or closed, you find them as you left them.",
            "## Moving texts and folders",
            "- The card of a saved text, or a folder with all it holds, from the sidebar or among the cards, can be dragged "
                + "onto a folder of the sidebar or onto the card of a folder; dropped onto " + SavedTitle + ", it leaves the folders.",
            "- “" + MoveTo + "”, in the menu of a saved text or of a folder, does the same. The same choice "
                + "is in the window where you write it, to the left of the buttons.",
            "## Renaming and deleting",
            "- In the right-click menu of a folder, in the sidebar or among the cards, “" + RenameDots + "” changes its name or place and “" + Delete
                + "” removes the folder only: what it held, texts and folders, moves up into the folder that held it, or "
                + "stays among the saved texts outside the folders.",
            "- The name of a folder cannot contain “" + Texts.Separator + "”.",
            "## Elsewhere",
            "- In the menu of the icon by the clock each folder is a submenu with its texts.",
            "- The name of a folder finds its texts in a search.",

            "# Typing where paste does not work",
            "Where Ctrl+V does not work, " + app + " writes the clipboard text one key at a time, as you would on the keyboard.",
            "## With the button",
            "- Copy the text.",
            "- Press “" + TypeButton + "”.",
            "- Click into the field of the remote window: you have " + MainForm.ArmedSeconds + " seconds to do it.",
            "- When that window has stayed in the foreground for the wait, typing starts.",
            "## With the keys",
            "Or press " + type + " right in the field: it types at once, with no wait.",
            "## Never in the wrong place",
            "Not to send a private text to the wrong place:",
            "- typing stops by itself if the target window loses the foreground;",
            "- while waiting and typing the same button becomes “" + Cancel + "”;",
            "- the desktop, the taskbar and the Start menu are never a target;",
            "- if the clipboard holds more than " + MainForm.UnusualLength + " characters or several lines, it asks first;",
            "- nothing is typed while Caps Lock is on: turn it off first.",

            "# Keys",
            "The first three work everywhere; the others in the list. While it is on, " + pick + " no longer reaches "
                + "other programs, nor do the keys of the numbers that some entry has.",
            "## Everywhere",
            pick + "\tOpens the list on " + HistoryPlace + ", from any program.",
            type + "\tTypes the clipboard into the active window at once.",
            MainForm.NumbersName + "\tPastes the entry with that number into the active window.",
            "## In the list",
            "F1\tOpens this guide.",
            ArrowsName + "\tMoves through the cards, or the places of the sidebar.",
            Key(Keys.Control | MainForm.AllKey) + "\tSelects all the cards; Ctrl or " + ShiftName + " with a click, several.",
            EnterKeyName + "\tChooses the entry and goes back to the window you came from.",
            EscapeKeyName + "\tGoes back to that window without choosing; with several cards selected, back to one.",
            Key(Keys.Control | MainForm.SearchKey) + "\tTakes the cursor to the search box.",
            Key(Keys.Control | MainForm.NewKey) + "\tNew saved text.",
            Key(Keys.Control | Keys.Shift | MainForm.NewKey) + "\tNew folder.",
            Key(Keys.Control | MainForm.SaveKey) + "\tSaves the copied entry, or edits the saved one.",
            Key(Keys.Control | MainForm.DeleteKey) + "\tDeletes the selected entry.",
            Key(Keys.Control | MainForm.PinKey) + "\tKeeps the copied entry at the top, or lets it go.",
            "## Changing the hotkeys",
            "In the options, under “" + HotkeysTitle + "”, the first three can be turned off and changed: the button "
                + "beside each shows its keys, and pressing it waits for new ones. For the numbers you press the keys "
                + "to hold together with any digit; if another program already has some of them, it says which.",
            "- They take Ctrl or Alt, also with " + ShiftName + ", together with a letter, a digit or a function key.",
            "- Esc keeps the current keys, " + DeleteKeyName + " goes back to the defaults.",
            "- With Ctrl+Alt avoid letters that type a symbol with AltGr, such as the E of the euro: the hotkey "
                + "would fire while you type it.",

            "# History",
            "What you copy stays in the list for a number of days you decide (" + Texts.DefaultKeepDays
                + " unless you change it), also after closing the program or turning off the PC.",
            "## Where it is kept",
            "- It is kept unencrypted in a database, by default " + Clips.DefaultFile + ", together with the saved "
                + "texts, which are encrypted. Expired days are deleted by themselves.",
            "- In the options, “" + ChangeFolderButton + "” takes the database to another folder, or uses the one "
                + "already there; “" + DefaultFolderButton + "” brings it back. It is not a way to share the list "
                + "between PCs: saved texts can be read only on this PC. If the folder chosen cannot be reached at the "
                + "start, the list works in memory and says so.",
            "## What stays and what goes",
            "- With 0 days only what you copied today stays.",
            "- Copied pictures take at most " + Size(Clips.PictureSpace) + ": beyond that the oldest go, even before their "
                + "days are over, and a red bar under the list says so. Saved pictures do not count.",
            "- Copies that other programs mark as private are not remembered.",
            "- Entries kept at the top go when their days are over too: what is to stay, save it.",
            "## Cleaning",
            "- In the options, “" + CleanButton + "” deletes at once what you copied before the last days written "
                + "beside it: with 1 only today stays, with 0 nothing does. It asks first; entries kept at the top "
                + "go too, saved texts are never touched.",
            "- Turning the history off in the options empties the list and deletes from disk what you copied.",

            "# Options",
            "The “" + Options + "” button opens them in a window of their own, which can stay open beside the list. Each option "
                + "says in a few words what it does, under its name; resting the pointer on it shows its full explanation. They "
                + "are in five pages, chosen from the column on the left as the subjects of this guide are.",
            "## " + TypingTitle,
            "How many seconds to wait before typing (" + MainForm.MinDelay + " to "
                + MainForm.MaxDelay + "), whether the server uses a US keyboard, whether to press Enter at the end, "
                + "whether to clear the clipboard afterwards, slow typing and symbols composed with Alt codes.",
            "## " + HotkeysTitle,
            "Turns " + type + ", " + pick + " and " + MainForm.NumbersName + " on and off, and "
                + "gives them other keys; and whether " + pick + " opens the list where you left it.",
            "## " + HistoryTitle,
            "Whether to remember what you copy, for how many days, the folder of the database "
                + "(which can be changed) with the link that opens it, and the button that cleans the history at once.",
            "## " + AppearanceTitle,
            "The language, the theme (light, dark or “" + ThemeSystem + "”), the font and the size of "
                + "the interface (buttons, options, this guide: in pixels, they restart the program), those of the entries "
                + "(they change at once), and whether resting the pointer on a card shows it whole.",
            "## " + ProgramTitle,
            "Whether to stay in the notification area and hide when you switch to another program, "
                + "whether to start with Windows, the buttons to export and import, and the version of the program.",

            "# Export and import",
            "In the options, under “" + ProgramTitle + "”, two buttons take your texts out of the program and bring "
                + "them back, as a backup or to move them to another PC. Each asks whether you want the saved texts "
                + "only or everything, that is the copied items too.",
            "## Exporting",
            "- “" + ExportButton + "” writes a text file that Notepad opens: the saved texts with their folders "
                + "(formatted ones with their HTML before the text) and, if you ask, the copied items with their time "
                + "and arrow. Pictures are left out.",
            "- In the file the texts are unencrypted, private ones included: keep it somewhere safe.",
            "- The file can be edited by hand: the text of each entry is in the lines that start with a tab, "
                + "which must stay in place.",
            "## Importing",
            "“" + ImportButton + "” reads such a file and adds what it holds to what is already here. If a saved "
                + "text has the name of an existing one it asks which to keep; copied items the list already has "
                + "are not repeated. A file exported in another language is read just the same.",

            "# The tray icon",
            "With the option on, closing or minimising the window only hides it: the program keeps running and "
                + "the hotkeys keep answering.",
            "## The window",
            "- A click on the icon reopens the window; right-click and then “" + Exit + "” really quits.",
            "- With “" + LeaveOption + "” the window goes back there also when you click elsewhere; off, it stays "
                + "open behind the others. While it waits and types, instead, it stays on top of everything: the "
                + "countdown and “" + Cancel + "” remain in view.",
            "- Starting the program a second time brings back the window of the one already open.",
            "## The menu and the icon",
            "- The right-click menu also lists the saved texts, each folder a submenu: choosing one puts it back on "
                + "the clipboard, ready for Ctrl+V or " + type + ", without opening the window.",
            "- Beside a saved text with a number are its keys.",
            "- The icon is in colour when the clipboard holds text, grey when it does not.",

            "# If something goes wrong",
            "The line under the cards says what just happened, and why. Confirmations go after a few seconds and "
                + "leave the room to the list; warnings and errors stay until the next message.",
            "## Typing",
            "- Symbols arrive wrong: the server uses another keyboard. Try the US keyboard option.",
            "- Characters go missing, or arrive out of order: turn on slow typing.",
            "- Symbols your keyboard has no key for do not arrive (~ and ` on an Italian keyboard): turn on Alt "
                + "codes, which work if the server runs Windows.",
            "- " + type + " does nothing with a full-screen remote desktop: that program captures every hotkey. Use the button.",
            "- Windows blocks the keystrokes: the remote desktop program runs as administrator. Start " + app
                + " that way too.",
            "## Hotkeys",
            "A hotkey turns itself off: another program already uses it. Give it other keys in the options."
        };
    }
}
