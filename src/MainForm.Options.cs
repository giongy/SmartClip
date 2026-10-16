using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;

// The options: how they are placed and explained, what their buttons do, and the settings in the registry.
partial class MainForm
{
    // The options, a page per category, chosen from a column on the left as the subjects of the guide are; the
    // page comes in as those of Windows 11 do. Adding an option takes a single line under its page.
    void PlaceOptions(int bottom)
    {
        pageList.SetBounds(Edge, Edge, NavWidth, bottom - Edge);
        Look.Field(pageList);
        pageList.BorderStyle = BorderStyle.None;
        pageList.IntegralHeight = false;
        pageList.DrawMode = DrawMode.OwnerDrawFixed;
        pageList.ItemHeight = Font.Height + NavPadding;
        pageList.DrawItem += delegate(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            Panel shown = (Panel)pageList.Items[e.Index];
            Look.DrawTopic(e, pageList, shown.Text, (Sign)shown.Tag, (int)(NavInset * Zoom));
        };
        pageList.SelectedIndexChanged += delegate { ShowPage(); };
        // The row under the pointer lights up.
        pageList.MouseMove += delegate(object sender, MouseEventArgs e)
        {
            int row = RowAt(pageList, e.Location);
            pageList.Cursor = row >= 0 ? Cursors.Hand : Cursors.Default;
            pageList.Glow.Set(row);
        };
        pageList.MouseLeave += delegate { pageList.Glow.Set(-1); };
        optionControls.Add(Look.Wrap(pageList, ListPadding));
        pagePane.SetBounds(Edge + NavWidth + NavGap, Edge, PaneWidth, bottom - Edge);
        optionControls.Add(pagePane);

        int top = Page(Strings.TypingTitle, Sign.Keys);
        // Confirming a number brings what was written back within its limits.
        Label delayLabel = PlaceNumber(delayBox, Strings.DelayOption, MaxDelay, delegate { Delay = Delay; }, ref top);
        Delay = DefaultDelay;
        PlaceOption(usBox, Strings.UsOption, ref top);
        PlaceOption(enterBox, Strings.EnterOption, ref top);
        PlaceOption(clearBox, Strings.ClearOption, ref top);
        PlaceOption(slowBox, Strings.SlowOption, ref top);
        PlaceOption(altCodeBox, Strings.AltCodeOption, ref top);

        top = Page(Strings.HotkeysTitle, Sign.Bolt);
        PlaceHotkey(hotkeyBox, typeKeysButton, Strings.TypeButton, ref top);
        hotkeyBox.Checked = true;
        PlaceHotkey(pickBox, pickKeysButton, Strings.PickOption, ref top);
        pickBox.Checked = true;
        PlaceHotkey(quickBox, quickKeysButton, Strings.QuickOption, ref top);
        quickBox.Checked = true;   // nothing is taken from other programs until an entry has a number

        top = Page(Strings.HistoryTitle, Sign.History);
        PlaceOption(historyBox, Strings.HistoryOption, ref top);
        historyBox.Checked = true;
        Label keepLabel = PlaceNumber(keepBox, Strings.KeepOption, Texts.MaxKeepDays,
            delegate { Retention = Retention; ApplyHistory(); Reload(); }, ref top);
        Retention = Texts.DefaultKeepDays;
        LinkLabel folderLink = PlaceLink(Strings.FolderLink, OpenFolder, ref top);
        // Not a setting: what to spare when the button below is pressed.
        Label cleanLabel = PlaceNumber(cleanBox, Strings.SpareOption, Texts.MaxKeepDays, delegate { Spared = Spared; }, ref top);
        Spared = 0;
        PlaceButton(cleanButton, Strings.CleanButton, CleanHistory, 0, ref top);
        cleanButton.Sign = Sign.Delete;

        top = Page(Strings.ProgramTitle, Sign.Options);
        programPage = page;
        // The language: its button shows the one in use and opens the list, each by the name it gives itself.
        ContextMenuStrip languageMenu = new ContextMenuStrip();
        for (int i = 0; i < Strings.Codes.Length; i++)
        {
            string code = Strings.Codes[i];   // a variable of its own for the delegate
            languageMenu.Items.Add(Strings.Names[i], null, delegate { ChangeLanguage(code); }).Enabled = code != Strings.Code;
        }
        Look.Style(languageMenu);
        Label languageLabel = PlacePicker(languageButton, Strings.Names[Array.IndexOf(Strings.Codes, Strings.Code)],
            Strings.LanguageOption, languageMenu, ref top);
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
        Label themeLabel = PlacePicker(themeButton, ThemeName(Look.Chosen), Strings.ThemeOption, themeMenu, ref top);
        // The font of the interface, each written in itself, and its size in pixels: the windows are built for
        // them, so they apply from a new start. The one in use cannot be chosen again.
        top += Gap;
        ContextMenuStrip faceMenu = new ContextMenuStrip();
        foreach (string each in Look.FontNames)
        {
            string name = each;   // a variable of its own for the delegate
            faceMenu.Items.Add(Look.FontLabel(name), null, delegate { ChangeInterface(name, Look.InterfaceSize); }).Font =
                new Font(name, Look.TextFont.SizeInPoints);
        }
        faceMenu.Opening += delegate
        {
            for (int i = 0; i < faceMenu.Items.Count; i++) faceMenu.Items[i].Enabled = Look.FontNames[i] != Look.InterfaceFamily;
        };
        Look.Style(faceMenu);
        Label faceLabel = PlacePicker(faceButton, Look.FontLabel(Look.InterfaceFamily), Strings.FaceOption, faceMenu, ref top);
        ContextMenuStrip faceSizeMenu = new ContextMenuStrip();
        foreach (int each in Look.InterfaceSizes)
        {
            int size = each;
            faceSizeMenu.Items.Add(string.Format(Strings.Pixels, size), null, delegate { ChangeInterface(Look.InterfaceFamily, size); })
                .Enabled = size != Look.InterfaceSize;
        }
        Look.Style(faceSizeMenu);
        Label faceSizeLabel = PlacePicker(faceSizeButton, string.Format(Strings.Pixels, Look.InterfaceSize), Strings.FaceSizeOption,
            faceSizeMenu, ref top);
        // The font of the entries, each written in itself, and its size in pixels: they change at once. The one
        // in use cannot be chosen again.
        top += Gap;
        ContextMenuStrip fontMenu = new ContextMenuStrip();
        foreach (string each in Look.FontNames)
        {
            string name = each;   // a variable of its own for the delegate
            fontMenu.Items.Add(Look.FontLabel(name), null, delegate { UseEntryFont(name, Look.EntrySize); }).Font =
                new Font(name, Look.TextFont.SizeInPoints);
        }
        fontMenu.Opening += delegate
        {
            for (int i = 0; i < fontMenu.Items.Count; i++) fontMenu.Items[i].Enabled = Look.FontNames[i] != Look.EntryFamily;
        };
        Look.Style(fontMenu);
        Label fontLabel = PlacePicker(fontButton, "", Strings.FontOption, fontMenu, ref top);
        ContextMenuStrip sizeMenu = new ContextMenuStrip();
        foreach (int each in Look.EntrySizes)
        {
            int size = each;
            sizeMenu.Items.Add(string.Format(Strings.Pixels, size), null, delegate { UseEntryFont(Look.EntryFamily, size); }).Tag = size;
        }
        sizeMenu.Opening += delegate
        {
            foreach (ToolStripItem item in sizeMenu.Items) item.Enabled = (int)item.Tag != Look.EntrySize;
        };
        Look.Style(sizeMenu);
        Label sizeLabel = PlacePicker(sizeButton, "", Strings.SizeOption, sizeMenu, ref top);
        PlaceOption(trayBox, Strings.TrayOption, ref top);
        trayBox.Checked = true;
        PlaceOption(leaveBox, Strings.LeaveOption, ref top);
        leaveBox.Checked = true;
        PlaceOption(startBox, Strings.StartOption, ref top);
        // Side by side, each with its two choices: the saved texts alone, or what was copied too.
        ContextMenuStrip exportMenu = Choice(Export), importMenu = Choice(Import);
        top += Gap;
        PlaceButton(exportButton, Strings.ExportButton, delegate { exportMenu.Show(exportButton, 0, exportButton.Height); }, 0, ref top);
        top -= NumberRow;
        PlaceButton(importButton, Strings.ImportButton, delegate { importMenu.Show(importButton, 0, importButton.Height); },
            OptionButtonWidth + Gap, ref top);
        pageList.SelectedIndex = 0;

        Tip(Strings.DelayTip, delayLabel, delayBox);
        Tip(Strings.UsTip, usBox);
        Tip(Strings.EnterTip, enterBox);
        Tip(Strings.ClearTip, clearBox);
        Tip(Strings.SlowTip, slowBox);
        Tip(Strings.AltCodeTip, altCodeBox);
        Tip(string.Format(Strings.LanguageTip, Program.AppName), languageLabel, languageButton);
        Tip(string.Format(Strings.ThemeTip, Strings.ThemeSystem), themeLabel, themeButton);
        Tip(string.Format(Strings.FaceTip, Program.AppName), faceLabel, faceButton);
        Tip(string.Format(Strings.FaceSizeTip, string.Format(Strings.Pixels, Look.DefaultInterfaceSize), Program.AppName),
            faceSizeLabel, faceSizeButton);
        Tip(Strings.FontTip, fontLabel, fontButton);
        Tip(string.Format(Strings.SizeTip, string.Format(Strings.Pixels, Look.DefaultEntrySize)), sizeLabel, sizeButton);
        Tip(Strings.HistoryTip, historyBox);
        Tip(string.Format(Strings.KeepTip, Texts.Folder, Texts.MaxRecent, Texts.MaxPictures, MaxShown), keepLabel, keepBox);
        Tip(string.Format(Strings.FolderTip, Texts.Folder), folderLink);
        Tip(Strings.CleanTip, cleanLabel, cleanBox, cleanButton);
        Tip(string.Format(Strings.TrayTip, Strings.Exit), trayBox);
        Tip(string.Format(Strings.StartTip, Program.AppName), startBox);
        Tip(Strings.ExportTip, exportButton);
        Tip(Strings.ImportTip, importButton);
    }

    // Opens a new page of options, with its name on top and its row in the column on the left; what is placed
    // next goes on it. Returns where its options begin.
    int Page(string title, Sign sign)
    {
        page = new Panel();
        page.Bounds = new Rectangle(Point.Empty, pagePane.Size);
        page.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        page.Text = title;   // what its row says
        page.Tag = sign;
        Label name = NewLabel(title, 0);
        name.Left = 0;
        name.Font = Look.TitleFont;
        page.Controls.Add(name);
        pagePane.Controls.Add(page);
        pageList.Items.Add(page);
        return PageTop;
    }

    // Shows the page chosen on the left, which rises into place.
    void ShowPage()
    {
        EndCapture();   // keys awaited on a page that leaves are not for it
        foreach (Control each in pagePane.Controls) each.Visible = each == pageList.SelectedItem;
        if (pageList.SelectedItem != null) Entrance.Play((Control)pageList.SelectedItem, 0);
    }

    // A row with something to choose from a menu: its description on the left, at the right edge of the page
    // the button that shows the choice made and opens the menu. Returns the description, for its tooltip.
    Label PlacePicker(RoundButton button, string choice, string text, ContextMenuStrip menu, ref int top)
    {
        Label label = NewLabel(text, top + 5);
        label.Left = 0;
        page.Controls.Add(label);
        PlaceButton(button, choice, delegate { menu.Show(button, 0, button.Height); }, PaneWidth - OptionButtonWidth, ref top);
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

    // A row with a number to write: its description on the left, its box at the right edge of the page.
    // Returns the description, for its tooltip.
    Label PlaceNumber(TextBox box, string text, int max, Action confirm, ref int top)
    {
        Label label = NewLabel(text, top + 5);
        label.Left = 0;
        box.SetBounds(PaneWidth - NumberWidth, top, NumberWidth, NumberHeight);
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
        page.Controls.Add(label);
        page.Controls.Add(Look.Frame(box));   // the frame of the box is what goes on the page
        return label;
    }

    // A row with a global hotkey: the switch that turns it on and, at the right edge of the page, the
    // button that shows its keys and takes new ones.
    void PlaceHotkey(CheckBox box, RoundButton keys, string text, ref int top)
    {
        int row = top + 4;   // the switch level with the text of the button
        PlaceOption(box, text, ref row);
        PlaceButton(keys, "", delegate { AwaitKeys(box); }, PaneWidth - OptionButtonWidth, ref top);
    }

    // A row with a link, for something to open rather than to set.
    LinkLabel PlaceLink(string text, Action open, ref int top)
    {
        LinkLabel link = new LinkLabel();
        link.Text = text;
        link.AutoSize = true;
        link.Location = new Point(0, top);
        link.LinkColor = Look.Accent;
        link.ActiveLinkColor = Look.Ink;
        link.LinkBehavior = LinkBehavior.AlwaysUnderline;   // or it would pass for a label
        link.LinkClicked += delegate { open(); };
        page.Controls.Add(link);
        top += RowHeight;
        return link;
    }

    // A row with a small button, for something to do at once rather than to set. 'left' is where it starts on
    // the page: buttons can stand side by side.
    void PlaceButton(RoundButton button, string text, Action act, int left, ref int top)
    {
        button.Text = text;
        button.SetBounds(left, top, OptionButtonWidth, ActionHeight);
        Look.Style(button, false);
        button.Click += delegate { act(); };
        page.Controls.Add(button);
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

    void PlaceOption(CheckBox box, string text, ref int top)
    {
        box.Text = text;
        box.AutoSize = true;
        box.Location = new Point(0, top);
        page.Controls.Add(box);
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
        quickBox.Checked = Transfer(key, save, "QuickPaste", quickBox.Checked);
        // Only what is held: with a digit, it must make a hotkey.
        quickKeys = Sound(Transfer(key, save, "QuickKeys", (int)quickKeys) | (int)Keys.D1, DefaultQuickKeys | Keys.D1) & Keys.Modifiers;
        historyBox.Checked = Transfer(key, save, "History", historyBox.Checked);
        Retention = Transfer(key, save, "KeepDays", Retention);
        trayBox.Checked = Transfer(key, save, "Tray", trayBox.Checked);
        leaveBox.Checked = Transfer(key, save, "HideOnLeave", leaveBox.Checked);
        language = Transfer(key, save, LanguageSetting, language);   // read again by SavedLanguage, before the window
        Transfer(key, save, ThemeSetting, (int)Look.Chosen);         // read by SavedTheme, before the window
        face = Transfer(key, save, FaceSetting, face);                // read by UseSavedInterface, before the window
        faceSize = Transfer(key, save, FaceSizeSetting, faceSize);
        string entryFamily = Transfer(key, save, "EntryFont", Look.EntryFamily);
        int entrySize = Transfer(key, save, "EntrySize", Look.EntrySize);
        if (!save) UseEntryFont(entryFamily, entrySize);
        savedColumn = Transfer(key, save, "SavedWidth", savedColumn);   // applied when the window loads

        // Window size, in pixels at 96 dpi like the column: applied when the window loads (see OnLoad). Zero if
        // never saved.
        Size size = !save ? Size.Empty : WindowState == FormWindowState.Normal ? Size : RestoreBounds.Size;
        savedSize = new Size(Transfer(key, save, "Width", (int)Math.Round(size.Width / Zoom)),
            Transfer(key, save, "Height", (int)Math.Round(size.Height / Zoom)));

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

    // Takes the font of the interface saved among the settings, or the default one. Before the window exists,
    // which is built for it.
    public static void UseSavedInterface()
    {
        string name = null;
        int size = 0;
        try
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(SettingsKey))
            {
                if (key != null)
                {
                    name = key.GetValue(FaceSetting) as string;
                    object saved = key.GetValue(FaceSizeSetting);
                    if (saved is int) size = (int)saved;
                }
            }
        }
        catch (Exception)
        {
            // unreadable: the default font
        }
        Look.UseInterfaceFont(name, size);
    }

    // Another font of the interface, or size: like the language, from a new start.
    void ChangeInterface(string name, int size)
    {
        if (name != Look.InterfaceFamily || size != Look.InterfaceSize) Restart(delegate { face = name; faceSize = size; });
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

    // Writes the entries in another font or size, at once: the lists are filled again, which measures their rows
    // anew, and an entry shown whole takes the font when it is shown next.
    void UseEntryFont(string family, int size)
    {
        Look.UseEntryFont(family, size);
        foreach (ListBox list in new ListBox[] { recentList, savedList })
        {
            list.Font = Look.EntryFont;
            list.ItemHeight = EntryHeight;
        }
        fontButton.Text = Look.FontLabel(Look.EntryFamily);
        sizeButton.Text = string.Format(Strings.Pixels, Look.EntrySize);
        Filter();
    }

    // Another language: the settings are saved with it and the program starts again, which shows every text
    // in it at once.
    void ChangeLanguage(string code)
    {
        if (code != Strings.Code) Restart(delegate { language = code; });
    }

    // Starts the program again with a setting that applies only from the start: 'change' sets it, and it is
    // saved with the others as this one closes. The new start waits for this one to close
    // (Program.RestartArgument) and opens again on the options.
    void Restart(Action change)
    {
        if (busy) return;
        try
        {
            Process.Start(Application.ExecutablePath, Program.RestartArgument);
        }
        catch (Exception ex)
        {
            status.Text = string.Format(Strings.Error, ex.Message);
            return;
        }
        change();
        exiting = true;
        Close();
    }

    // Started again after a change of language or of the font of the interface: back where the user was.
    public void ShowOptions()
    {
        pageList.SelectedItem = programPage;
        ShowView(true);
    }
}
