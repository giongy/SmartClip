using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;

// The options: how they are placed and explained, what their buttons do, and the settings in the registry.
partial class MainForm
{
    // The options are stacked by category, in two columns: on the left what concerns typing, on the right
    // the rest. Adding one takes a single line here, the window height adapts. Returns where they end.
    int PlaceOptions()
    {
        int left = Edge, top = Edge;
        PlaceTitle(Strings.TypingTitle, left, ref top);
        // Confirming a number brings what was written back within its limits.
        Label delayLabel = PlaceNumber(delayBox, Strings.DelayOption, MaxDelay, delegate { Delay = Delay; }, left, ref top);
        Delay = DefaultDelay;
        PlaceOption(usBox, Strings.UsOption, left, ref top);
        PlaceOption(enterBox, Strings.EnterOption, left, ref top);
        PlaceOption(clearBox, Strings.ClearOption, left, ref top);
        PlaceOption(slowBox, Strings.SlowOption, left, ref top);
        PlaceOption(altCodeBox, Strings.AltCodeOption, left, ref top);
        // On the left too, where there is room: under the history the window would have to grow.
        PlaceTitle(Strings.ProgramTitle, left, ref top);
        // The language: its button shows the one in use and opens the list, each by the name it gives itself.
        ContextMenuStrip languageMenu = new ContextMenuStrip();
        for (int i = 0; i < Strings.Codes.Length; i++)
        {
            string code = Strings.Codes[i];   // a variable of its own for the delegate
            languageMenu.Items.Add(Strings.Names[i], null, delegate { ChangeLanguage(code); }).Enabled = code != Strings.Code;
        }
        Look.Style(languageMenu);
        Label languageLabel = PlacePicker(languageButton, Strings.Names[Array.IndexOf(Strings.Codes, Strings.Code)],
            Strings.LanguageOption, languageMenu, left, ref top);
        // The theme, under it in the same way; the one in use cannot be chosen again.
        ContextMenuStrip themeMenu = new ContextMenuStrip();
        foreach (Theme each in new Theme[] { Theme.System, Theme.Light, Theme.Dark })
        {
            Theme theme = each;   // a variable of its own for the delegate
            themeMenu.Items.Add(ThemeName(theme), null, delegate { ChangeTheme(theme); }).Tag = theme;
        }
        themeMenu.Opening += delegate
        {
            foreach (ToolStripItem item in themeMenu.Items) item.Enabled = (Theme)item.Tag != Look.Chosen;
        };
        Look.Style(themeMenu);
        Label themeLabel = PlacePicker(themeButton, ThemeName(Look.Chosen), Strings.ThemeOption, themeMenu, left, ref top);
        PlaceOption(trayBox, Strings.TrayOption, left, ref top);
        trayBox.Checked = true;
        PlaceOption(leaveBox, Strings.LeaveOption, left, ref top);
        leaveBox.Checked = true;
        PlaceOption(startBox, Strings.StartOption, left, ref top);
        // Side by side, each with its two choices: the saved texts alone, or what was copied too.
        ContextMenuStrip exportMenu = Choice(Export), importMenu = Choice(Import);
        top += Gap;
        PlaceButton(exportButton, Strings.ExportButton, delegate { exportMenu.Show(exportButton, 0, exportButton.Height); }, left, ref top);
        top -= NumberRow;
        PlaceButton(importButton, Strings.ImportButton, delegate { importMenu.Show(importButton, 0, importButton.Height); },
            left + OptionButtonWidth + Gap, ref top);
        int leftBottom = top;

        left += ColumnWidth + ColumnGap;
        top = Edge;
        PlaceTitle(Strings.HotkeysTitle, left, ref top);
        PlaceHotkey(hotkeyBox, typeKeysButton, Strings.TypeButton, left, ref top);
        hotkeyBox.Checked = true;
        PlaceHotkey(pickBox, pickKeysButton, Strings.PickOption, left, ref top);
        pickBox.Checked = true;
        PlaceTitle(Strings.HistoryTitle, left, ref top);
        PlaceOption(historyBox, Strings.HistoryOption, left, ref top);
        historyBox.Checked = true;
        Label keepLabel = PlaceNumber(keepBox, Strings.KeepOption, Texts.MaxKeepDays,
            delegate { Retention = Retention; ApplyHistory(); Reload(); }, left, ref top);
        Retention = Texts.DefaultKeepDays;
        LinkLabel folderLink = PlaceLink(Strings.FolderLink, OpenFolder, left, ref top);
        // Not a setting: what to spare when the button below is pressed.
        Label cleanLabel = PlaceNumber(cleanBox, Strings.SpareOption, Texts.MaxKeepDays, delegate { Spared = Spared; }, left, ref top);
        Spared = 0;
        PlaceButton(cleanButton, Strings.CleanButton, CleanHistory, left, ref top);
        cleanButton.Sign = Sign.Delete;
        top = Math.Max(top, leftBottom);

        Tip(Strings.DelayTip, delayLabel, delayBox);
        Tip(Strings.UsTip, usBox);
        Tip(Strings.EnterTip, enterBox);
        Tip(Strings.ClearTip, clearBox);
        Tip(Strings.SlowTip, slowBox);
        Tip(Strings.AltCodeTip, altCodeBox);
        Tip(string.Format(Strings.LanguageTip, Program.AppName), languageLabel, languageButton);
        Tip(string.Format(Strings.ThemeTip, Strings.ThemeSystem), themeLabel, themeButton);
        Tip(Strings.HistoryTip, historyBox);
        Tip(string.Format(Strings.KeepTip, Texts.Folder, Texts.MaxRecent, Texts.MaxPictures, MaxShown), keepLabel, keepBox);
        Tip(string.Format(Strings.FolderTip, Texts.Folder), folderLink);
        Tip(Strings.CleanTip, cleanLabel, cleanBox, cleanButton);
        Tip(string.Format(Strings.TrayTip, Strings.Exit), trayBox);
        Tip(string.Format(Strings.StartTip, Program.AppName), startBox);
        Tip(Strings.ExportTip, exportButton);
        Tip(Strings.ImportTip, importButton);
        return top;
    }

    // A row with something to choose from a menu: its description on the left, at the right edge of the column
    // the button that shows the choice made and opens the menu. Returns the description, for its tooltip.
    Label PlacePicker(RoundButton button, string choice, string text, ContextMenuStrip menu, int left, ref int top)
    {
        Label label = NewLabel(text, top + 5);
        label.Left = left;
        optionControls.Add(label);
        PlaceButton(button, choice, delegate { menu.Show(button, 0, button.Height); }, left + ColumnWidth - OptionButtonWidth, ref top);
        return label;
    }

    // The wait before typing, in seconds: whatever is written in its box, kept within the limits.
    int Delay
    {
        get { return Number(delayBox, MinDelay, MaxDelay, DefaultDelay); }
        set { delayBox.Text = Math.Max(MinDelay, Math.Min(MaxDelay, value)).ToString(); }
    }

    // For how many days what is copied stays on disk; zero keeps it only in memory.
    int Retention
    {
        get { return Number(keepBox, 0, Texts.MaxKeepDays, Texts.DefaultKeepDays); }
        set { keepBox.Text = Math.Max(0, Math.Min(Texts.MaxKeepDays, value)).ToString(); }
    }

    // How many of the last days the button that cleans the history leaves alone; zero is none.
    int Spared
    {
        get { return Number(cleanBox, 0, Texts.MaxKeepDays, 0); }
        set { cleanBox.Text = Math.Max(0, Math.Min(Texts.MaxKeepDays, value)).ToString(); }
    }

    static int Number(TextBox box, int min, int max, int fallback)
    {
        int written;
        return int.TryParse(box.Text, out written) ? Math.Max(min, Math.Min(max, written)) : fallback;
    }

    // Hands the two settings of the history to what keeps it, in memory and on disk.
    void ApplyHistory()
    {
        Change(delegate
        {
            if (historyBox.Checked) Texts.Keep(Retention);
            else Texts.Clear();
        });
    }

    // The name of a category of options, with some air above it unless it opens its column.
    void PlaceTitle(string text, int left, ref int top)
    {
        if (top > Edge) top += SectionGap;
        Label title = NewLabel(text, top);
        title.Left = left;
        title.Font = new Font(Font, FontStyle.Bold);
        // A thin line under the title, as wide as the column: the accent colour is left to what can be clicked.
        Label rule = new Label();
        rule.SetBounds(left, top + TitleRow - RuleGap, ColumnWidth, 1);
        rule.BackColor = Look.Border;
        optionControls.Add(title);
        optionControls.Add(rule);
        top += TitleRow;
    }

    // A row with a number to write: its description on the left, its box at the right edge of the column.
    // Returns the description, for its tooltip.
    Label PlaceNumber(TextBox box, string text, int max, Action confirm, int left, ref int top)
    {
        Label label = NewLabel(text, top + 5);
        label.Left = left;
        box.SetBounds(left + ColumnWidth - NumberWidth, top, NumberWidth, NumberHeight);
        box.TextAlign = HorizontalAlignment.Center;
        box.MaxLength = max.ToString().Length;
        box.KeyPress += delegate(object sender, KeyPressEventArgs e)
        {
            bool enter = e.KeyChar == (char)Keys.Enter;
            if (enter) confirm();   // Enter confirms what was written, and nothing else
            e.Handled = enter || !char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar);
        };
        box.Leave += delegate { confirm(); };
        top += NumberRow;
        optionControls.Add(label);
        optionControls.Add(Look.Frame(box));   // the frame of the box is what goes into the window
        return label;
    }

    // A row with a global hotkey: the switch that turns it on and, at the right edge of the column, the
    // button that shows its keys and takes new ones.
    void PlaceHotkey(CheckBox box, RoundButton keys, string text, int left, ref int top)
    {
        int row = top + 4;   // the switch level with the text of the button
        PlaceOption(box, text, left, ref row);
        PlaceButton(keys, "", delegate { AwaitKeys(box); }, left + ColumnWidth - OptionButtonWidth, ref top);
    }

    // A row with a link, for something to open rather than to set.
    LinkLabel PlaceLink(string text, Action open, int left, ref int top)
    {
        LinkLabel link = new LinkLabel();
        link.Text = text;
        link.AutoSize = true;
        link.Location = new Point(left, top);
        link.LinkColor = Look.Accent;
        link.ActiveLinkColor = Look.Ink;
        link.LinkBehavior = LinkBehavior.AlwaysUnderline;   // or it would pass for the title of a category
        link.LinkClicked += delegate { open(); };
        optionControls.Add(link);
        top += RowHeight;
        return link;
    }

    // A row with a small button, for something to do at once rather than to set.
    void PlaceButton(RoundButton button, string text, Action act, int left, ref int top)
    {
        button.Text = text;
        button.SetBounds(left, top, OptionButtonWidth, ActionHeight);
        Look.Style(button, false);
        button.Click += delegate { act(); };
        optionControls.Add(button);
        top += NumberRow;
    }

    // The menu under the buttons that export and import: what to take, then on with it.
    static ContextMenuStrip Choice(Action<bool> act)
    {
        ContextMenuStrip menu = new ContextMenuStrip();
        menu.Items.Add(Strings.OnlySaved, null, delegate { act(false); });
        menu.Items.Add(Strings.Everything, null, delegate { act(true); });
        Look.Style(menu);
        return menu;
    }

    static string BackupFilter
    {
        get { return Strings.TextFiles + " (*" + Backup.Extension + ")|*" + Backup.Extension; }
    }

    // Writes the saved texts, and with 'all' what was copied too, to a text file chosen by the user.
    void Export(bool all)
    {
        if (!Ask(Strings.AskExport)) return;
        using (SaveFileDialog dialog = new SaveFileDialog())
        {
            dialog.Filter = BackupFilter;
            dialog.FileName = Program.AppName + " " + DateTime.Today.ToString(Texts.DayFormat) + Backup.Extension;
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            BackupResult result = null;
            if (!Change(delegate { result = Backup.Export(dialog.FileName, all); })) return;
            string saved = Count(result.Saved, Strings.SavedOne, Strings.SavedMany);
            Say((all ? string.Format(Strings.ExportedAll, saved, Count(result.Copied, Strings.CopiedOne, Strings.CopiedMany))
                : string.Format(Strings.Exported, saved))
                + (result.Pictures > 0 ? string.Format(Strings.PicturesLeftOut, result.Pictures) : ""));
        }
    }

    // Takes in a file written by Export. With 'all' the copied texts too, if the history is on.
    void Import(bool all)
    {
        using (OpenFileDialog dialog = new OpenFileDialog())
        {
            dialog.Filter = BackupFilter;
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            bool copied = all && historyBox.Checked;
            BackupResult result = null;
            if (!Change(delegate
            {
                result = Backup.Import(dialog.FileName, copied, delegate(int clashes)
                {
                    return Ask((clashes == 1 ? Strings.ClashOne : string.Format(Strings.ClashMany, clashes)) + Strings.ClashChoice);
                });
            })) return;
            string saved = Count(result.Saved, Strings.SavedOne, Strings.SavedMany);
            Say(result.Saved + result.Copied + result.Kept == 0 ? Strings.NothingImported
                : (copied ? string.Format(Strings.ImportedAll, saved, Count(result.Copied, Strings.CopiedOne, Strings.CopiedMany))
                    : string.Format(Strings.Imported, saved))
                + (result.Kept > 0 ? string.Format(Strings.KeptAsWere, result.Kept) : "")
                + (all && !copied ? Strings.HistoryOffImport : ""));
        }
        Reload();
    }

    static string Count(int number, string one, string many)
    {
        return number + " " + (number == 1 ? one : many);
    }

    // Removes at once what was copied before the days to spare; with zero, everything. Pinned entries stay.
    void CleanHistory()
    {
        int days = Spared;
        string what = days == 0 ? Strings.CleanEverything : days == 1 ? Strings.CleanBeforeToday
            : string.Format(Strings.CleanBeforeDays, days);
        if (!Ask(string.Format(Strings.AskClean, what))) return;
        int before = Texts.Recent.Count;
        if (!Change(delegate { Texts.Clean(days); })) return;
        int removed = before - Texts.Recent.Count;
        Say(removed == 0 ? Strings.NothingToClean : removed == 1 ? Strings.CleanedOne : string.Format(Strings.CleanedMany, removed));
        Reload();
    }

    // Shows the folder of the history files in Explorer.
    void OpenFolder()
    {
        if (Directory.Exists(Texts.Folder)) Change(delegate { Process.Start(Texts.Folder); });
        else Say(Strings.NoHistoryFolder);
    }

    void PlaceOption(CheckBox box, string text, int left, ref int top)
    {
        box.Text = text;
        box.AutoSize = true;
        box.Location = new Point(left, top);
        optionControls.Add(box);
        top += RowHeight;
    }

    // The program used to be called PasswordPaste: as long as its registry key exists, the settings and saved
    // texts in it move here. What is already here stays: the new key may have been written to in the meantime.
    static void AdoptOldSettings()
    {
        using (RegistryKey old = Registry.CurrentUser.OpenSubKey(OldSettingsKey))
        {
            if (old == null) return;
            using (RegistryKey current = Registry.CurrentUser.CreateSubKey(SettingsKey))
            {
                CopyMissing(old, current);
            }
        }
        // Reached only if the copy went through to the end.
        Registry.CurrentUser.DeleteSubKeyTree(OldSettingsKey, false);
    }

    static void CopyMissing(RegistryKey from, RegistryKey to)
    {
        foreach (string name in from.GetValueNames())
            if (to.GetValue(name) == null) to.SetValue(name, from.GetValue(name), from.GetValueKind(name));
        foreach (string name in from.GetSubKeyNames())
        {
            using (RegistryKey source = from.OpenSubKey(name))
            using (RegistryKey target = to.CreateSubKey(name))
            {
                CopyMissing(source, target);
            }
        }
    }

    void LoadSettings()
    {
        try
        {
            AdoptOldSettings();
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(SettingsKey))
            {
                if (key != null) TransferSettings(key, false);
            }
        }
        catch (Exception)
        {
            // Settings unreadable or of an unexpected type: the defaults stay.
        }
    }

    void SaveSettings()
    {
        try
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(SettingsKey))
            {
                TransferSettings(key, true);
            }
        }
        catch (Exception)
        {
            // Registry not writable: the settings are not remembered.
        }
    }

    // Writes the settings to the registry ('save') or reads them back. Each one is listed here only once, so
    // reading and writing cannot diverge; when reading, whatever was never saved stays as it is.
    void TransferSettings(RegistryKey key, bool save)
    {
        Delay = Transfer(key, save, "Delay", Delay);
        usBox.Checked = Transfer(key, save, "UsKeyboard", usBox.Checked);
        enterBox.Checked = Transfer(key, save, "PressEnter", enterBox.Checked);
        clearBox.Checked = Transfer(key, save, "ClearClipboard", clearBox.Checked);
        slowBox.Checked = Transfer(key, save, "Slow", slowBox.Checked);
        altCodeBox.Checked = Transfer(key, save, "AltCodes", altCodeBox.Checked);
        hotkeyBox.Checked = Transfer(key, save, "Hotkey", hotkeyBox.Checked || refused.Contains(hotkeyBox));
        pickBox.Checked = Transfer(key, save, "PickHotkey", pickBox.Checked || refused.Contains(pickBox));
        typeKeys = Sound(Transfer(key, save, "HotkeyKeys", (int)typeKeys), DefaultTypeKeys);
        pickKeys = Sound(Transfer(key, save, "PickKeys", (int)pickKeys), DefaultPickKeys);
        historyBox.Checked = Transfer(key, save, "History", historyBox.Checked);
        Retention = Transfer(key, save, "KeepDays", Retention);
        trayBox.Checked = Transfer(key, save, "Tray", trayBox.Checked);
        leaveBox.Checked = Transfer(key, save, "HideOnLeave", leaveBox.Checked);
        language = Transfer(key, save, LanguageSetting, language);   // read again by SavedLanguage, before the window
        Transfer(key, save, ThemeSetting, (int)Look.Chosen);         // read by SavedTheme, before the window
        savedColumn = Transfer(key, save, "SavedWidth", savedColumn);   // applied when the window loads

        // Window position. Reading starts from a point outside every screen: it stays there, and is ignored,
        // if the registry holds nothing; the same happens if the screen of that time is no longer connected.
        Point corner = !save ? OffScreen : WindowState == FormWindowState.Normal ? Location : RestoreBounds.Location;
        corner = new Point(Transfer(key, save, "Left", corner.X), Transfer(key, save, "Top", corner.Y));
        if (save) return;
        foreach (Screen screen in Screen.AllScreens)
        {
            if (!screen.WorkingArea.Contains(corner)) continue;
            StartPosition = FormStartPosition.Manual;
            Location = corner;
        }
    }

    static int Transfer(RegistryKey key, bool save, string name, int value)
    {
        if (!save) return (int)key.GetValue(name, value);
        key.SetValue(name, value);
        return value;
    }

    // The keys of a hotkey as the registry gives them, or the ones it starts with if they cannot be used.
    static Keys Sound(int keys, Keys fallback)
    {
        return Usable((Keys)keys) ? (Keys)keys : fallback;
    }

    static bool Transfer(RegistryKey key, bool save, string name, bool value)
    {
        return Transfer(key, save, name, value ? 1 : 0) != 0;
    }

    static string Transfer(RegistryKey key, bool save, string name, string value)
    {
        if (!save) return key.GetValue(name, value) as string ?? value;
        key.SetValue(name, value);
        return value;
    }

    // The language saved among the settings, or null. Read before the window exists: its texts come first.
    public static string SavedLanguage()
    {
        try
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(SettingsKey))
            {
                return key == null ? null : key.GetValue(LanguageSetting) as string;
            }
        }
        catch (Exception)
        {
            return null;   // unreadable: the default language
        }
    }

    // The theme saved among the settings; as Windows if there is none. Read before the window exists, which is
    // built in its colours.
    public static Theme SavedTheme()
    {
        try
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(SettingsKey))
            {
                object theme = key == null ? null : key.GetValue(ThemeSetting);
                return theme is int && Enum.IsDefined(typeof(Theme), (int)theme) ? (Theme)(int)theme : Theme.System;
            }
        }
        catch (Exception)
        {
            return Theme.System;
        }
    }

    static string ThemeName(Theme theme)
    {
        return theme == Theme.Light ? Strings.ThemeLight : theme == Theme.Dark ? Strings.ThemeDark : Strings.ThemeSystem;
    }

    // Another theme: unlike the language, at once, without starting again.
    void ChangeTheme(Theme theme)
    {
        Look.Choose(theme);
        themeButton.Text = ThemeName(theme);
    }

    // Another language: the settings are saved with it and the program starts again, which shows every text
    // in it at once. The new start waits for this one to close (Program.RestartArgument).
    void ChangeLanguage(string code)
    {
        if (busy || code == Strings.Code) return;
        try
        {
            Process.Start(Application.ExecutablePath, Program.RestartArgument);
        }
        catch (Exception ex)
        {
            status.Text = string.Format(Strings.Error, ex.Message);
            return;
        }
        language = code;
        exiting = true;
        Close();
    }

    // Started again after a change of language: back where the user was.
    public void ShowOptions()
    {
        ShowView(true);
    }
}
