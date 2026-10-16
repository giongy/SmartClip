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

    // --- Dates and quotes ---
    public static string DayFormat = "d MMM", YearDayFormat = "d MMM yy";   // a day in a row of the list
    public static string Quote = "“{0}”";

    // --- Main window ---
    public static string TypeButton = "Type clipboard";
    public static string RecentTitle = "Recently copied", SavedTitle = "Saved", PausedSuffix = " (paused)";
    public static string New = "New…", SaveDots = "Save…", EditDots = "Edit…", RenameDots = "Rename…", Delete = "Delete";
    public static string Options = "Options", List = "List", SearchCue = "Search…";
    public static string CountOf = "{0} of {1}";
    public static string NoResults = "No results", HistoryOffEmpty = "The history is off",
        PausedEmpty = "Paused: what you copy is not remembered", CopyHint = "What you copy will appear here",
        NoSaved = "No saved texts";
    public static string NewTip = "Writes a new text to keep, with its name ({0}).";
    public static string SaveTip = "On a copied entry: saves it under a name, and you can touch up its content.\n"
        + "On a saved entry: changes its name or content ({0}).\n"
        + "On a folder: renames it or moves it.\n"
        + "Pictures stay in memory only and cannot be saved.";
    public static string HelpTip = "Opens the guide: everything the program can do ({0}).";
    public static string DeleteTip = "Deletes the saved text, or drops the copied entry from the list ({0}).\n"
        + "Of a folder it deletes only the folder: what it holds moves up one level.";
    public static string DividerTip = "Drag to give the saved texts more or less room.\nDouble-click: the starting width.";
    public static string Error = "Error: {0}";

    // --- Menus ---
    public static string Open = "Open", Exit = "Exit", Pause = "Pause capture", Resume = "Resume capture";
    public static string ShowContent = "Show the content", MoveTo = "Move to", NewFolder = "New folder…",
        Pin = "Keep at the top", Unpin = "Release from the top", OutOfFolders = "Out of the folders", EmptyFolder = "Empty";
    public static string NumberMenu = "Shortcut", NoNumber = "None";

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
    public static string NumberGiven = "{0} now pastes “{1}” wherever you are.";
    public static string NumberTaken = "“{0}” no longer has a shortcut.";
    public static string Pasted = "Pasted into {1}: {0}.";
    public static string PasteFocusLost = "Not pasted: the window changed. The entry is on the clipboard.";
    public static string OnClipboard = "On the clipboard: {0}. Paste it with Ctrl+V.";
    public static string OnClipboardOrType = "On the clipboard: {0}. Paste it with Ctrl+V or type it with {1}.";
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
    // The buttons of the questions, which the program asks in a window of its own (AskForm).
    public static string Yes = "Yes", No = "No", Ok = "OK";

    // --- Typing ---
    public static string Cancel = "Cancel";
    public static string NoText = "There is no text on the clipboard: copy the password first.";
    public static string Cancelled = "Cancelled.";
    public static string CapsLock = "Caps Lock is on: turn it off and try again.";
    public static string TypingInto = "Typing into {0}…";
    public static string AskUnusual = "The clipboard holds {0}: it does not look like a password.\n\nType it anyway?";
    public static string OnLines = " on {0} lines";
    public static string Countdown = "Typing in {0} s into {1}. Press Cancel to stop.";
    public static string TimeUp = "Time is up: you did not choose the window to type into.";
    public static string ClickNow = "Click now into the password field of the remote window…";
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
        + "copy the password, press here and click into the field of the remote window.\n"
        + "Or press {0} right in the field.";
    public static string SearchTip = "Type to search both columns: words in any order, ignoring case and accents.\n"
        + "The arrows move through the column, Tab switches to the other.\n"
        + "{0} or a click put the entry back on the clipboard: then paste it with Ctrl+V or\n"
        + "type it with {1}. With a click the window stays open, with {0} or a double click\n"
        + "you go back where you were. Right-click opens the entry's menu.\n"
        + "Resting the pointer on a copied entry shows it whole.\n"
        + "The star at the right of a copied entry ({2}) keeps it at the top of the list,\n"
        + "also beyond the days of the history, until you remove it.\n"
        + "Saved texts stay in the Windows registry, encrypted for your user, and only\n"
        + "their name is shown. They can be gathered in folders: “New folder” is in the\n"
        + "right-click menu ({3}); a text goes in by dragging it, or with “Move to”.\n"
        + "A click on a folder opens and closes it.\n"
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
    public static string DelayOption = "Wait before typing (seconds):";
    public static string UsOption = "The server uses a US English keyboard";
    public static string EnterOption = "Press Enter at the end";
    public static string ClearOption = "Clear the clipboard after typing";
    public static string SlowOption = "Slow typing";
    public static string AltCodeOption = "Alt codes for symbols with no key";
    public static string LanguageOption = "Language:";
    public static string ThemeOption = "Theme:", ThemeSystem = "Same as Windows", ThemeLight = "Light", ThemeDark = "Dark";
    public static string FontOption = "Font:", SizeOption = "Text size:";
    public static string SizeSmall = "Small", SizeNormal = "Normal", SizeLarge = "Large", SizeLarger = "Extra large";
    public static string TrayOption = "Stay in the notification area (tray)";
    public static string LeaveOption = "Hide when switching to another program";
    public static string StartOption = "Start with Windows";
    public static string ExportButton = "Export…", ImportButton = "Import…";
    public static string PickOption = "Open the list";
    public static string QuickOption = "Paste by number";
    public static string HistoryOption = "Remember copied texts and pictures";
    public static string KeepOption = "Days on disk (0: memory only):";
    public static string FolderLink = "Open the history folder";
    public static string SpareOption = "Keep the last days when cleaning (0: none):";
    public static string CleanButton = "Clean now…";
    public static string DelayTip = "How many seconds the target window must stay in the foreground\n"
        + "before typing starts: your time to click into the right field,\nor to cancel.";
    public static string UsTip = "Leave it off for ordinary remote desktops: the keyboard of this PC applies.\n"
        + "Turn it on if symbols arrive wrong because the server assumes a US\n"
        + "keyboard (it happens with VMware consoles, iLO, iDRAC and some sign-in\nscreens).";
    public static string EnterTip = "Presses Enter after the last character, to confirm the sign-in at once.";
    public static string ClearTip = "Erases the password from the clipboard once typing has succeeded.\n"
        + "Windows clipboard history (Win+V), if on, is not touched.";
    public static string SlowTip = "Turn it on if characters arrive missing or out of order on the server.";
    public static string AltCodeTip = "Symbols this keyboard has no key for (for example ~ and ` on an Italian\n"
        + "keyboard) are normally sent as Unicode characters, which not every remote\n"
        + "desktop program accepts. With this option they are composed with Alt codes\n"
        + "on the numeric keypad instead: it works only if the server runs Windows.";
    public static string LanguageTip = "The language of the program. Changing it restarts {0}, which opens again at once.";
    public static string ThemeTip = "Light or dark colours. “{0}” follows the mode Windows sets for apps,\n"
        + "also when it changes while the program is open.";
    public static string FontTip = "The font of the windows. Changing it restarts {0}, which opens again at once.";
    public static string SizeTip = "How large the text is: the windows grow with it. “{1}” is the size of the text of Windows 11.\n"
        + "Changing it restarts {0}, which opens again at once.";
    public static string HistoryTip = "The list offers the texts and pictures copied while the program is open.\n"
        + "What password managers mark as private is not remembered.\n"
        + "Turn it off if you copy passwords from programs that do not: they would be\n"
        + "readable in the list. Turning it off also deletes the history on disk.";
    public static string KeepTip = "How many days what you copy stays in the history, also after closing.\n"
        + "It is kept in {0},\n"
        + "one file per day, unencrypted: expired days are deleted by themselves.\n"
        + "With 0 nothing goes to disk: the last {1} copied items, of which at most\n"
        + "{2} pictures, stay in memory and are gone when the program closes.\n"
        + "The list shows the {3} most recent: search finds the others.";
    public static string FolderTip = "Opens in File Explorer the folder with the history files, one per day:\n{0}";
    public static string CleanTip = "Deletes at once from the history, in memory and on disk, what you copied before\n"
        + "the last days written above: with 1 only today stays, with 0 nothing does.\n"
        + "It asks first. Starred entries and saved texts are not touched, nor is the\n"
        + "clipboard. The days on disk stay as set.";
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
        + "In the file the texts are unencrypted, passwords included: keep it somewhere safe.";
    public static string ImportTip = "Reads a file written by “Export”, also on another PC, and adds what it holds\n"
        + "to what is already here. If a saved text has the name of an existing one, it asks which to keep.\n"
        + "Copied texts the list already has are not repeated.";
    public static string OnlySaved = "Saved texts only…", Everything = "Everything, copied items too…";
    public static string TextFiles = "Text files";
    public static string AskExport = "The file will hold the texts unencrypted, passwords included: anyone who opens it "
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
    public static string AskClean = "Delete {0}?\n\nStarred entries and saved texts stay.";
    public static string NothingToClean = "Nothing to delete in the history.";
    public static string CleanedOne = "1 entry deleted from the history.", CleanedMany = "{0} entries deleted from the history.";
    public static string NoHistoryFolder = "The history folder does not exist yet: nothing is on disk.";

    // --- The window of a saved text or a folder ---
    public static string NewTextTitle = "New text", SaveCopiedTitle = "Save the copied text", EditSavedTitle = "Edit the saved text";
    public static string NameLabel = "Name (what the list shows):", ContentLabel = "Content:";
    public static string FolderOf = "Folder: ", NoFolder = "No folder", Inside = "Inside: ";
    public static string NewFolderTitle = "New folder", RenameFolderTitle = "Rename the folder", FolderNameLabel = "Folder name:";
    public static string Save = "Save";

    // --- Entries ---
    public static string PictureLabel = "Picture {0}×{1}";
    public static string PictureToday = "Picture {0}×{1}, copied at {2}", PictureOn = "Picture {0}×{1}, copied on {2} at {3}";
    public static string MoreOne = "… and one more character", MoreMany = "… and {0} more characters";

    // --- Export file ---
    public static string ExportHeader = "{0} {1}: export of {2}";
    public static string ExportClearWarning = "Warning: the texts here are unencrypted, passwords included.";
    public static string ExportEditNote = "The text of each entry is in the lines that start with a tab: if you edit the "
        + "file, leave it in place.";

    // --- The guide ---
    public static string HelpTitle = "{0} guide", Close = "Close";
    public static Func<string[]> Guide = EnglishGuide;

    // A line starting with "# " begins a subject and names it, one starting with "- " is a point of a list, one
    // with a tab a key and what it does. Built when the guide opens: the user can change the hotkeys.
    static string[] EnglishGuide()
    {
        string pick = MainForm.PickName, type = MainForm.HotkeyName, app = Program.AppName;
        return new string[]
        {
            "# What it is for",
            app + " keeps what you copy and the texts you choose to save, to put them back on the clipboard when "
                + "you need them. It can also type the clipboard as real key presses: that is how a password gets "
                + "in where paste does not work, for example on the lock screens of remote desktops.",

            "# The list",
            "On the left are the recently copied items, texts and pictures, newest first. On the right the saved "
                + "texts, which stay until you delete them.",
            "- Type in the box at the top to search both columns: words count in any order, ignoring case and accents.",
            "- A click puts the entry on the clipboard and leaves the window open, so that you can still save or delete it.",
            "- " + EnterKeyName + " or a double click put it on the clipboard and, if you opened the list with " + pick
                + ", take you back to the window you were working in.",
            "- Then paste it with Ctrl+V, or type it with " + type + ".",
            "- Right-click selects the entry and opens its menu, without touching the clipboard.",
            "- Resting the pointer on a copied entry its row cuts short shows it whole after a moment, in a small "
                + "box: the text with all its lines, the picture larger. It goes as soon as you move the pointer.",
            "- The star at the right end of a copied entry (" + Key(Keys.Control | MainForm.PinKey) + ") keeps it at "
                + "the top of the column: it shows faintly when the pointer is over it, and a click turns it on or "
                + "off. A starred entry does not expire with the days of the history and stays until you remove the "
                + "star; copying the same text again does not move it.",
            "- “" + NumberMenu + "”, in the right-click menu, gives a saved text or a copied entry a number from 1 to "
                + MainForm.MaxNumber + ": then, from any program, " + MainForm.NumbersName + " paste it at once into "
                + "the window you are working in, without opening this one (the entry goes on the clipboard and Ctrl+V "
                + "follows by itself). The number shows in a small frame at the right of the row, and beside the saved "
                + "text in the icon menu; a copied entry that gets one also gets the star, which keeps it. Each number "
                + "belongs to one entry: given to another, it moves.",
            "The coloured sign on the left says what the entry is: a text, a web address, a saved text. Pictures "
                + "show a thumbnail. On the right is the time of the copy (the day, if not today) or the day it was saved.",
            "Not to remember what you are about to copy, “" + Pause + "” is in the right-click menu of the list and "
                + "in that of the icon by the clock: the column says “paused” until you choose “" + Resume + "”. At "
                + "the next start capturing is on again.",
            "The gap between the two columns can be dragged, to give the saved texts more or less room: the width "
                + "stays at the next start too, and a double click there goes back to the starting one.",
            "The column of the copied items shows the " + MainForm.MaxShown + " most recent: search finds the others.",

            "# Saved texts",
            "They are what you want always at hand: a signature, an address, a password. Each has a name, and "
                + "only the name shows in the list.",
            "- “" + New + "” (" + Key(Keys.Control | MainForm.NewKey) + ") writes a text from scratch.",
            "- “" + SaveDots + "” (" + Key(Keys.Control | MainForm.SaveKey) + "), on a copied entry, keeps it under a "
                + "name; before saving you can touch up its content.",
            "- “" + EditDots + "” (" + Key(Keys.Control | MainForm.SaveKey) + "), on a saved entry, changes its name or content.",
            "- “" + Delete + "” (" + Key(Keys.Control | MainForm.DeleteKey) + ") deletes the saved text, or drops the "
                + "copied entry from the list.",
            "- “" + ShowContent + "”, in the right-click menu of a saved text, shows it in a small box for a few "
                + "seconds: the list shows only its name, and resting the pointer on it is not enough.",
            "- Saved texts are also in the menu of the icon by the clock: choosing one puts it back on the "
                + "clipboard without opening the window.",
            "Saved texts are kept in the Windows registry, encrypted for your user: another user of the same PC "
                + "cannot read them. When you put one back on the clipboard it is marked as private, so that Windows "
                + "clipboard history (Win+V) and other clipboard managers ignore it. Pictures cannot be saved.",

            "# Folders",
            "When the saved texts become many you can gather them in folders, and folders in other folders.",
            "- “" + NewFolder + "” is in the right-click menu (" + Key(Keys.Control | Keys.Shift | MainForm.NewKey)
                + "): it is created inside the selected folder, or that of the selected text, but its window lets "
                + "you choose where it goes.",
            "- A click on the name of a folder, or " + EnterKeyName + ", opens and closes it; the number on the right "
                + "says how many texts it holds, those in the folders inside it too. Open or closed, you find them as "
                + "you left them.",
            "- A saved text, or a folder with all it holds, can be dragged into another folder: onto the folder "
                + "itself or onto one of its texts. Dropped on a text outside the folders, or below the last row, it "
                + "leaves the one it was in.",
            "- “" + MoveTo + "”, in the right-click menu of a saved text or a folder, does the same. The same choice "
                + "is in the window where you write it, to the left of the buttons.",
            "- With a folder selected, “" + RenameDots + "” changes its name or place and “" + Delete + "” removes "
                + "the folder only: what it held, texts and folders, moves up into the folder that held it, or stays "
                + "among the saved texts outside the folders.",
            "- The name of a folder cannot contain “" + Texts.Separator + "”.",
            "- In the menu of the icon by the clock each folder is a submenu with its texts.",
            "- Search also looks inside closed folders, and the name of a folder finds its texts.",

            "# Typing a password",
            "Where Ctrl+V does not work, " + app + " writes the clipboard text one key at a time, as you would on the keyboard.",
            "- Copy the password.",
            "- Press “" + TypeButton + "”.",
            "- Click into the field of the remote window: you have " + MainForm.ArmedSeconds + " seconds to do it.",
            "- When that window has stayed in the foreground for the wait, typing starts.",
            "Otherwise press " + type + " right in the field: it types at once, with no wait.",
            "Not to send a password to the wrong place:",
            "- typing stops by itself if the target window loses the foreground;",
            "- while waiting and typing the same button becomes “" + Cancel + "”;",
            "- the desktop, the taskbar and the Start menu are never a target;",
            "- if the clipboard holds more than " + MainForm.UnusualLength + " characters or several lines, it asks first;",
            "- nothing is typed while Caps Lock is on: turn it off first.",

            "# Keys",
            pick + "\tOpens the list, from any program.",
            type + "\tTypes the clipboard into the active window at once.",
            MainForm.NumbersName + "\tPastes the entry with that number into the active window.",
            "F1\tOpens this guide.",
            ArrowsName + "\tMoves through the column.",
            "Tab\tSwitches to the other column.",
            EnterKeyName + "\tChooses the entry and goes back to the window you came from.",
            "Esc\tGoes back to that window without choosing.",
            Key(Keys.Control | MainForm.NewKey) + "\tNew saved text.",
            Key(Keys.Control | Keys.Shift | MainForm.NewKey) + "\tNew folder.",
            Key(Keys.Control | MainForm.SaveKey) + "\tSaves the copied entry, or edits the saved one.",
            Key(Keys.Control | MainForm.DeleteKey) + "\tDeletes the selected entry.",
            Key(Keys.Control | MainForm.PinKey) + "\tKeeps the copied entry at the top, or lets it go.",
            "The first three work everywhere; the others in the list. While it is on, " + pick + " no longer reaches "
                + "other programs, nor do the keys of the numbers that some entry has.",
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
            "- It is kept in " + Texts.Folder + ", one file per day, unencrypted. Expired days are deleted by themselves.",
            "- With 0 days nothing goes to disk: the last " + Texts.MaxRecent + " copied items, of which at most "
                + Texts.MaxPictures + " pictures, stay in memory and are gone when the program closes.",
            "- What password managers mark as private is not remembered.",
            "- Starred entries stay beyond those days too.",
            "- In the options, “" + CleanButton + "” deletes at once what you copied before the last days written "
                + "beside it: with 1 only today stays, with 0 nothing does. It asks first, and touches neither "
                + "starred entries nor saved texts.",
            "- Turning the history off in the options empties the list and the files on disk, stars included.",

            "# Options",
            "The “" + Options + "” button shows them in place of the list, and “" + List + "” goes back. Resting the "
                + "pointer on an option shows its full explanation. They are in four pages, chosen from the column on the left as "
                + "the subjects of this guide are:",
            "- " + TypingTitle + ": how many seconds to wait before typing (" + MainForm.MinDelay + " to "
                + MainForm.MaxDelay + "), whether the server uses a US keyboard, whether to press Enter at the end, "
                + "whether to clear the clipboard afterwards, slow typing and symbols composed with Alt codes.",
            "- " + HotkeysTitle + ": turns " + type + ", " + pick + " and " + MainForm.NumbersName + " on and off, and "
                + "gives them other keys.",
            "- " + HistoryTitle + ": whether to remember what you copy, for how many days, the link to the folder of "
                + "the files and the button that cleans it at once.",
            "- " + ProgramTitle + ": the language, the theme (light, dark or “" + ThemeSystem + "”), the font and the size of the text, whether to stay in the notification area and hide when you switch "
                + "to another program, whether to start with Windows, and the buttons to export and import.",

            "# Export and import",
            "In the options, under “" + ProgramTitle + "”, two buttons take your texts out of the program and bring "
                + "them back, as a backup or to move them to another PC. Each asks whether you want the saved texts "
                + "only or everything, that is the copied items too.",
            "- “" + ExportButton + "” writes a text file that Notepad opens: the saved texts with their folders "
                + "and, if you ask, the copied items with their time and star. Pictures are left out.",
            "- In the file the texts are unencrypted, passwords included: keep it somewhere safe.",
            "- “" + ImportButton + "” reads such a file and adds what it holds to what is already here. If a saved "
                + "text has the name of an existing one it asks which to keep; copied items the list already has "
                + "are not repeated. A file exported in another language is read just the same.",
            "- The file can be edited by hand: the text of each entry is in the lines that start with a tab, "
                + "which must stay in place.",

            "# The tray icon",
            "With the option on, closing or minimising the window only hides it: the program keeps running and "
                + "the hotkeys keep answering.",
            "- A click on the icon reopens the window; right-click and then “" + Exit + "” really quits.",
            "- With “" + LeaveOption + "” the window goes back there also when you click elsewhere; off, it stays "
                + "open behind the others. While it waits and types, instead, it stays on top of everything: the "
                + "countdown and “" + Cancel + "” remain in view.",
            "- The right-click menu also lists the saved texts, each folder a submenu: choosing one puts it back on "
                + "the clipboard, ready for Ctrl+V or " + type + ", without opening the window.",
            "- Beside a saved text with a number are its keys.",
            "- The icon is in colour when the clipboard holds text, grey when it does not.",
            "- Starting the program a second time brings back the window of the one already open.",

            "# If something goes wrong",
            "- Symbols arrive wrong: the server uses another keyboard. Try the US keyboard option.",
            "- Characters go missing, or arrive out of order: turn on slow typing.",
            "- Symbols your keyboard has no key for do not arrive (~ and ` on an Italian keyboard): turn on Alt "
                + "codes, which work if the server runs Windows.",
            "- " + type + " does nothing with a full-screen remote desktop: that program captures every hotkey. Use the button.",
            "- Windows blocks the keystrokes: the remote desktop program runs as administrator. Start " + app
                + " that way too.",
            "- A hotkey turns itself off: another program already uses it. Give it other keys in the options.",
            "The line above the buttons says what just happened, and why. Confirmations go after a few seconds and "
                + "leave the room to the list; warnings and errors stay until the next message."
        };
    }
}
