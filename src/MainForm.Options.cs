using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;

// The options: how they are placed and explained, what their buttons do, and the settings in the registry.
partial class MainForm
{
    // The options, a page per category, chosen from a column on the left as the subjects of the guide are; the
    // page comes in as those of Windows 11 do. Adding an option takes a single line under its page. Placed here,
    // shown in a window of their own (OptionsWindow).
    void PlaceOptions()
    {
        pageList.SetBounds(Edge, Edge, NavWidth, AreaHeight);
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
        pageList.LightUnderPointer();
        Look.Wrap(pageList, ListPadding);   // the window of the options takes the card it is in
        pagePane.SetBounds(Edge + NavWidth + NavGap, Edge, PaneWidth, AreaHeight);

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
        // Where the database is, and the buttons that take it elsewhere or back to the default folder.
        Label folderLabel = NewLabel(Strings.DataFolderOption, 0, top);
        page.Controls.Add(folderLabel);
        top += RowHeight;
        folderPath.SetBounds(0, top, PaneWidth, RowHeight);
        folderPath.AutoEllipsis = true;
        folderPath.UseMnemonic = false;
        folderPath.ForeColor = Look.Muted;
        page.Controls.Add(folderPath);
        top += RowHeight;
        databaseSize.SetBounds(0, top, PaneWidth, RowHeight);
        databaseSize.AutoEllipsis = true;
        databaseSize.ForeColor = Look.Muted;
        page.Controls.Add(databaseSize);
        top += RowHeight;
        PlaceButton(changeFolderButton, Strings.ChangeFolderButton, ChangeDataFolder, 0, ref top);
        top -= NumberRow;
        PlaceButton(defaultFolderButton, Strings.DefaultFolderButton, delegate { MoveDataFolder(""); }, OptionButtonWidth + Gap, ref top);
        LinkLabel folderLink = PlaceLink(Strings.FolderLink, OpenFolder, ref top);
        folderTipped = new Control[] { folderLabel, folderPath, databaseSize, changeFolderButton, defaultFolderButton, folderLink };
        // Not a setting: what to spare when the button below is pressed.
        Label cleanLabel = PlaceNumber(cleanBox, Strings.SpareOption, Texts.MaxKeepDays, delegate { Spared = Spared; }, ref top);
        Spared = 0;
        PlaceButton(cleanButton, Strings.CleanButton, CleanHistory, 0, ref top);
        cleanButton.Sign = Sign.Delete;

        top = Page(Strings.ProgramTitle, Sign.Options);
        programPage = page;
        // Each of these has a button that shows the choice in use and opens the others, where that one cannot be
        // chosen again. The language by the name each gives itself; a font written in itself.
        Func<string, Font> itself = delegate(string name) { return new Font(name, Look.TextFont.SizeInPoints); };
        Label languageLabel = PlacePicker(languageButton, Strings.LanguageOption, Strings.Codes,
            delegate(string code) { return Strings.Names[Array.IndexOf(Strings.Codes, code)]; },
            delegate { return Strings.Code; }, ChangeLanguage, null, ref top);
        // Unlike the language, the theme changes at once.
        Label themeLabel = PlacePicker(themeButton, Strings.ThemeOption, new Theme[] { Theme.System, Theme.Light, Theme.Dark },
            ThemeName, delegate { return Look.Chosen; }, ChangeTheme, null, ref top);
        // The font of the interface and its size in pixels: the windows are built for them, so they apply from a new start.
        top += Gap;
        Label faceLabel = PlacePicker(faceButton, Strings.FaceOption, Look.FontNames, Look.FontLabel,
            delegate { return Look.InterfaceFamily; }, delegate(string name) { ChangeInterface(name, Look.InterfaceSize); }, itself, ref top);
        Label faceSizeLabel = PlacePicker(faceSizeButton, Strings.FaceSizeOption, Look.InterfaceSizes, PixelsName,
            delegate { return Look.InterfaceSize; }, delegate(int size) { ChangeInterface(Look.InterfaceFamily, size); }, null, ref top);
        // The font of the entries and its size: they change at once.
        top += Gap;
        Label fontLabel = PlacePicker(fontButton, Strings.FontOption, Look.FontNames, Look.FontLabel,
            delegate { return Look.EntryFamily; }, delegate(string name) { UseEntryFont(name, Look.EntrySize); }, itself, ref top);
        Label sizeLabel = PlacePicker(sizeButton, Strings.SizeOption, Look.EntrySizes, PixelsName,
            delegate { return Look.EntrySize; }, delegate(int size) { UseEntryFont(Look.EntryFamily, size); }, null, ref top);
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
        Tip(string.Format(Strings.FaceSizeTip, PixelsName(Look.DefaultInterfaceSize), Program.AppName), faceSizeLabel, faceSizeButton);
        Tip(Strings.FontTip, fontLabel, fontButton);
        Tip(string.Format(Strings.SizeTip, PixelsName(Look.DefaultEntrySize)), sizeLabel, sizeButton);
        Tip(Strings.HistoryTip, historyBox);
        Tip(string.Format(Strings.KeepTip, MaxShown), keepLabel, keepBox);
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
        Label name = NewLabel(title, 0, 0);
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
    // the button that shows the choice in use ('current') and opens the menu of all of them, each written by
    // 'name', in a font of its own if 'font' gives one. The one in use cannot be chosen again. Returns the
    // description, for its tooltip.
    Label PlacePicker<T>(RoundButton button, string text, T[] choices, Func<T, string> name, Func<T> current, Action<T> choose,
        Func<T, Font> font, ref int top)
    {
        ContextMenuStrip menu = new ContextMenuStrip();
        foreach (T each in choices)
        {
            T choice = each;   // a variable of its own for the delegate
            ToolStripItem item = menu.Items.Add(name(choice), null, delegate { choose(choice); });
            item.Tag = choice;
            if (font != null) item.Font = font(choice);
        }
        menu.Opening += delegate
        {
            foreach (ToolStripItem item in menu.Items) item.Enabled = !Equals(item.Tag, current());
        };
        Look.Style(menu);
        Label label = NewLabel(text, 0, top + 5);
        page.Controls.Add(label);
        PlaceButton(button, name(current()), delegate { menu.Show(button, 0, button.Height); }, PaneWidth - OptionButtonWidth, ref top);
        return label;
    }

    // A size as the options write it: "14 px".
    static string PixelsName(int size)
    {
        return string.Format(Strings.Pixels, size);
    }

    // The wait before typing, in seconds: whatever is written in its box, kept within the limits.
    int Delay
    {
        get { return Number(delayBox, MinDelay, MaxDelay, DefaultDelay); }
        set { delayBox.Text = Clamp(value, MinDelay, MaxDelay).ToString(); }
    }

    // For how many days what is copied stays, today being the first: zero keeps today's only.
    int Retention
    {
        get { return Number(keepBox, 0, Texts.MaxKeepDays, Texts.DefaultKeepDays); }
        set { keepBox.Text = Clamp(value, 0, Texts.MaxKeepDays).ToString(); }
    }

    // How many of the last days the button that cleans the history leaves alone; zero is none.
    int Spared
    {
        get { return Number(cleanBox, 0, Texts.MaxKeepDays, 0); }
        set { cleanBox.Text = Clamp(value, 0, Texts.MaxKeepDays).ToString(); }
    }

    // What is written in a box of numbers, kept within the limits; 'fallback' if it is not a number.
    static int Number(TextBox box, int min, int max, int fallback)
    {
        int written;
        return int.TryParse(box.Text, out written) ? Clamp(written, min, max) : fallback;
    }

    static int Clamp(int value, int min, int max)
    {
        return Math.Max(min, Math.Min(max, value));
    }

    // Hands the two settings of the history to the database: for how many days the copied entries stay, or none
    // at all.
    void ApplyHistory()
    {
        Change(delegate
        {
            if (historyBox.Checked) clips.Keep(Retention);
            else clips.ClearCopied();
        });
    }

    // A row with a number to write: its description on the left, its box at the right edge of the page.
    // Returns the description, for its tooltip.
    Label PlaceNumber(TextBox box, string text, int max, Action confirm, ref int top)
    {
        Label label = NewLabel(text, 0, top + 5);
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
            if (dialog.ShowDialog(Front) != DialogResult.OK) return;
            BackupResult result = null;
            if (!Change(delegate { result = Backup.Export(dialog.FileName, Exported(all)); })) return;
            string saved = Count(result.Saved, Strings.SavedOne, Strings.SavedMany);
            Say((all ? string.Format(Strings.ExportedAll, saved, Count(result.Copied, Strings.CopiedOne, Strings.CopiedMany))
                : string.Format(Strings.Exported, saved))
                + (result.Pictures > 0 ? string.Format(Strings.PicturesLeftOut, result.Pictures) : ""));
        }
    }

    // What goes into an exported file: the saved ones as their column has them, folders included, and with 'all' the
    // copied ones, all of them, not only those listed.
    List<Entry> Exported(bool all)
    {
        List<Entry> chosen = new List<Entry>(entries);
        if (all) chosen.AddRange(clips.Copied(int.MaxValue, new string[0]));
        return chosen;
    }

    // Takes in a file written by Export. With 'all' the copied texts too, if the history is on.
    void Import(bool all)
    {
        using (OpenFileDialog dialog = new OpenFileDialog())
        {
            dialog.Filter = BackupFilter;
            if (dialog.ShowDialog(Front) != DialogResult.OK) return;
            bool copied = all && historyBox.Checked;
            BackupResult result = null;
            if (!Change(delegate
            {
                result = Backup.Import(clips, dialog.FileName, copied, delegate(int clashes)
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

    // Removes at once what was copied before the days to spare; with zero, everything, the entries kept at the top
    // too. The saved ones stay.
    void CleanHistory()
    {
        int days = Spared;
        string what = days == 0 ? Strings.CleanEverything : days == 1 ? Strings.CleanBeforeToday
            : string.Format(Strings.CleanBeforeDays, days);
        if (!Ask(string.Format(Strings.AskClean, what))) return;
        int removed = 0;
        if (!Change(delegate
        {
            int before = clips.CopiedCount();
            clips.Clean(days);
            removed = before - clips.CopiedCount();
        })) return;
        Say(removed == 0 ? Strings.NothingToClean : removed == 1 ? Strings.CleanedOne : string.Format(Strings.CleanedMany, removed));
        Reload();
    }

    // Shows the folder of the database in Explorer.
    void OpenFolder()
    {
        if (Directory.Exists(DataFolder)) Change(delegate { Process.Start(DataFolder); });
        else Say(Strings.NoHistoryFolder);
    }

    // Writes where the database is, in the options and in their tooltips; the default folder cannot be chosen again.
    void ShowFolder()
    {
        folderPath.Text = DataFolder;
        defaultFolderButton.Enabled = dataFolder.Length > 0;
        Tip(string.Format(Strings.DataFolderTip, DataFolder, Path.GetDirectoryName(Clips.DefaultFile)), folderTipped);
        ShowSize();
    }

    // Writes how much the database takes, and how much of it the copied pictures, beside their room.
    void ShowSize()
    {
        Change(delegate
        {
            databaseSize.Text = string.Format(Strings.DatabaseSize, Strings.Size(clips.Size), Strings.Size(clips.PictureBytes()),
                Strings.Size(Clips.PictureSpace));
        });
    }

    // Lets the user choose another folder for the database, and takes it there.
    void ChangeDataFolder()
    {
        using (FolderBrowserDialog dialog = new FolderBrowserDialog())
        {
            dialog.Description = string.Format(Strings.ChooseDataFolder, Program.AppName);
            dialog.SelectedPath = DataFolder;
            if (dialog.ShowDialog(Front) == DialogResult.OK) MoveDataFolder(dialog.SelectedPath);
        }
    }

    // Takes the list to another folder, "" being the default one. Where there is no database yet, this one moves
    // there; where there is one, after asking, that one is used and this one stays as it is. A folder synchronised
    // online or on the network asks first. The new place is saved at once: the next start must find it.
    void MoveDataFolder(string folder)
    {
        string home = Path.GetDirectoryName(Clips.DefaultFile);
        if (folder.Length > 0 && SameFolder(folder, home)) folder = "";   // the default, even when chosen by hand
        string target = folder.Length > 0 ? Path.Combine(folder, Path.GetFileName(Clips.DefaultFile)) : Clips.DefaultFile;
        bool inMemory = clips.FileName == Clips.Memory;
        if (!inMemory && SameFolder(Path.GetDirectoryName(target), Path.GetDirectoryName(clips.FileName))) return;
        string where = Path.GetDirectoryName(target);
        if (folder.Length > 0 && Synced(folder) && !AskForm.Ask(Front, string.Format(Strings.AskSyncedFolder, folder), true)) return;
        try
        {
            bool found = File.Exists(target);
            if (found && !Ask(string.Format(Strings.AskUseExisting, where, Program.AppName))) return;
            // The one found there, or, with nothing on disk to take along, a list that begins there.
            if (found || inMemory)
            {
                Clips there = new Clips(target);   // first: if it cannot be opened, the list stays where it is
                clips.Dispose();
                clips = there;
            }
            else clips = Clips.Move(clips, target);
        }
        catch (ClipsMoveException ex)
        {
            clips = ex.Back;
            Log.Error(ex);
            status.Text = string.Format(Strings.ListMoveFailed, ex.Message);
            return;
        }
        catch (Exception ex)
        {
            ShowError(ex);
            return;
        }
        dataFolder = folder;
        unavailable = null;
        Log.FileName = Path.Combine(DataFolder, Program.AppName + ".log");
        SaveSettings();
        ApplyHistory();   // the days kept apply to the list found there too
        Reload();
        ShowFolder();
        Say(string.Format(Strings.ListMovedTo, DataFolder));
    }

    static bool SameFolder(string one, string other)
    {
        return string.Equals(Path.GetFullPath(one).TrimEnd('\\'), Path.GetFullPath(other).TrimEnd('\\'), StringComparison.OrdinalIgnoreCase);
    }

    // A folder on the network, or one a program keeps in step with the cloud: there a database can be locked or
    // damaged while it is copied, and saved texts cannot be read on another PC anyway.
    static bool Synced(string folder)
    {
        string full = Path.GetFullPath(folder);
        if (full.StartsWith(@"\\", StringComparison.Ordinal)) return true;
        foreach (string name in new string[] { "OneDrive", "OneDriveConsumer", "OneDriveCommercial" })
        {
            string cloud = Environment.GetEnvironmentVariable(name);
            if (!string.IsNullOrEmpty(cloud) && (SameFolder(full, cloud)
                || full.StartsWith(cloud.TrimEnd('\\') + @"\", StringComparison.OrdinalIgnoreCase))) return true;
        }
        foreach (string mark in new string[] { @"\Dropbox", @"\Google Drive", @"\iCloudDrive" })
            if (full.IndexOf(mark, StringComparison.OrdinalIgnoreCase) >= 0) return true;
        return false;
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
        catch (Exception ex)
        {
            Log.Error(ex);   // settings unreadable or of an unexpected type: the defaults stay
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
        catch (Exception ex)
        {
            Log.Error(ex);   // registry not writable: the settings are not remembered
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
        dataFolder = Transfer(key, save, DataFolderSetting, dataFolder);   // read by OpenClips, before the window
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

    // A setting read before the window exists, or null if there is none or it cannot be read: then the default.
    static object EarlySetting(string name)
    {
        try
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(SettingsKey))
            {
                return key == null ? null : key.GetValue(name);
            }
        }
        catch (Exception)
        {
            return null;
        }
    }

    // The language saved among the settings, or null. Read before the window exists: its texts come first.
    public static string SavedLanguage()
    {
        return EarlySetting(LanguageSetting) as string;
    }

    // The theme saved among the settings; as Windows if there is none. Read before the window exists, which is
    // built in its colours.
    public static Theme SavedTheme()
    {
        object theme = EarlySetting(ThemeSetting);
        return theme is int && Enum.IsDefined(typeof(Theme), (int)theme) ? (Theme)(int)theme : Theme.System;
    }

    // Takes the font of the interface saved among the settings, or the default one. Before the window exists,
    // which is built for it.
    public static void UseSavedInterface()
    {
        object size = EarlySetting(FaceSizeSetting);
        Look.UseInterfaceFont(EarlySetting(FaceSetting) as string, size is int ? (int)size : 0);
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
        sizeButton.Text = PixelsName(Look.EntrySize);
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
            ShowError(ex);
            return;
        }
        change();
        exiting = true;
        Close();
    }

    // Started again after a change of language or of the font of the interface: back where the user was, as soon
    // as the window shows.
    public void ShowOptions()
    {
        pageList.SelectedItem = programPage;
        optionsAtStart = true;
    }

    // The window of the options, as the guide has one: the column of the pages, the page chosen, and under them what
    // the status line says, so that what an option did can be read here too. The options themselves belong to the
    // main window (its fields, read and written with the settings): this window is made once and only hides when
    // closed. Like the guide it is not a dialog, and stays above the main window, which owns it.
    sealed class OptionsWindow : Window
    {
        readonly MainForm main;
        readonly Label note = new Label();
        readonly RoundButton closeButton = new RoundButton();

        public OptionsWindow(MainForm main)
        {
            this.main = main;
            Text = string.Format(Strings.OptionsTitle, Program.AppName);
            FormBorderStyle = FormBorderStyle.FixedSingle;   // the pages have the room they need
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ShowIcon = false;
            StartPosition = FormStartPosition.Manual;   // placed over the main window each time it opens

            int bottom = main.pagePane.Bottom + Edge;
            closeButton.Text = Strings.Close;
            closeButton.SetBounds(main.pagePane.Right - ButtonWidth, bottom + (StatusHeight - ButtonHeight) / 2, ButtonWidth, ButtonHeight);
            closeButton.Click += delegate { Close(); };
            Look.Style(closeButton, true);
            CancelButton = closeButton;   // Esc too. No AcceptButton: Enter in a box confirms its number.
            note.SetBounds(Edge, bottom, closeButton.Left - Gap - Edge, StatusHeight);
            note.UseMnemonic = false;
            note.AutoEllipsis = true;
            note.TextAlign = ContentAlignment.MiddleLeft;
            note.ForeColor = Look.Muted;
            ClientSize = new Size(main.pagePane.Right + Edge, bottom + StatusHeight + Edge);
            Controls.AddRange(new Control[] { main.pageList.Parent, main.pagePane, note, closeButton });
            ActiveControl = main.pageList;
            Finish();
            Look.Keep(this);   // a change of theme recolours it even before it first shows
        }

        // What the status line of the main window says.
        public string Note
        {
            set { note.Text = value; }
        }

        // Shows it over its owner, as the guide opens, without leaving the screen; the page comes in.
        public void Open(Form owner)
        {
            PlaceOnScreen(new Point(owner.Left + (owner.Width - Width) / 2, owner.Top + (owner.Height - Height) / 2),
                Screen.FromControl(owner));
            Show(owner);
            main.ShowPage();
        }

        // Closed by the user, it only hides: the options stay as they are, for the next time.
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            if (e.CloseReason != CloseReason.UserClosing) return;   // the program is ending
            e.Cancel = true;
            Hide();
        }

        // The keys of a hotkey being changed are taken here, as in the main window; F1 opens the guide.
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (main.capturing != null) return main.TakeKeys(keyData);
            if (keyData == HelpKey)
            {
                main.ShowHelp();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            main.EndCapture();   // keys pressed elsewhere are not for us
        }
    }
}
