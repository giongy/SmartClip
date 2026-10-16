using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;

// The global hotkeys: their names, the new keys the user gives them, their registration with Windows. Two
// are fixed, typing and the list; those of the numbers are as many as the entries that have a number.
partial class MainForm
{
    // A key combination as the user reads it, with Shift as the keyboards of the language label it: "Ctrl+Alt+V".
    public static string NameOf(Keys keys)
    {
        Keys code = keys & Keys.KeyCode;
        string key = code >= Keys.D0 && code <= Keys.D9 ? (code - Keys.D0).ToString() : code.ToString();
        return ((keys & Keys.Control) != 0 ? "Ctrl+" : "") + ((keys & Keys.Alt) != 0 ? "Alt+" : "")
            + ((keys & Keys.Shift) != 0 ? Strings.ShiftName + "+" : "") + key;
    }

    // The keys that paste the entry with a number, and how the user reads them: "Ctrl+Shift+1".
    static Keys NumberKeys(int number)
    {
        return quickKeys | (Keys.D0 + number);
    }

    public static string NumberName(int number)
    {
        return NameOf(NumberKeys(number));
    }

    // All of them at once: "Ctrl+Shift+1…9".
    public static string NumbersName
    {
        get { return NumberName(1) + "…" + MaxNumber; }
    }

    // The number that a combination would paste with 'modifiers' as the keys of the numbers, or zero.
    static int NumberOf(Keys keys, Keys modifiers)
    {
        Keys code = keys & Keys.KeyCode;
        return (keys & Keys.Modifiers) == modifiers && code >= Keys.D1 && code <= Keys.D0 + MaxNumber ? code - Keys.D0 : 0;
    }

    // Whether a combination can be a global hotkey: a letter, a digit or a function key, with Ctrl or Alt
    // so that it is never something one types.
    static bool Usable(Keys keys)
    {
        Keys code = keys & Keys.KeyCode;
        bool key = code >= Keys.A && code <= Keys.Z || code >= Keys.D0 && code <= Keys.D9 || code >= Keys.F1 && code <= Keys.F12;
        return key && (keys & (Keys.Control | Keys.Alt)) != 0;
    }

    // Writes the keys of the global hotkeys wherever they are named: on their buttons and in the tooltips.
    void NameHotkeys()
    {
        typeKeysButton.Text = HotkeyName;
        pickKeysButton.Text = PickName;
        quickKeysButton.Text = NumbersName;
        Tip(string.Format(Strings.HotkeyTip, HotkeyName, Strings.TypeButton) + Strings.ChangeKeysNote, hotkeyBox);
        Tip(string.Format(Strings.PickTip, PickName, HotkeyName) + Strings.ChangeKeysNote, pickBox);
        Tip(string.Format(Strings.QuickTip, NumbersName, Strings.NumberMenu) + Strings.ChangeKeysNote, quickBox);
        Tip(string.Format(Strings.KeysTip, Strings.ShiftName, Strings.DeleteKeyName), typeKeysButton, pickKeysButton);
        Tip(string.Format(Strings.QuickKeysTip, Strings.ShiftName, Strings.DeleteKeyName, NameOf(DefaultQuickKeys | Keys.D1)),
            quickKeysButton);
        Tip(string.Format(Strings.LeaveTip, PickName), leaveBox);
        Tip(string.Format(Strings.TypeTip, HotkeyName), typeButton);
        Tip(string.Format(Strings.SearchTip, Strings.EnterKeyName, HotkeyName, NameOf(PinKeys), NameOf(GroupKeys),
            NumbersName, Strings.NumberMenu), search);
    }

    // Waits for the keys of a global hotkey: the next combination pressed becomes it. Meanwhile all the
    // hotkeys are off, or Windows would take them before this window sees them. Asked again, gives up.
    void AwaitKeys(CheckBox box)
    {
        bool again = capturing == box;
        EndCapture();
        if (again || busy) return;
        capturing = box;
        Native.UnregisterHotKey(Handle, HotkeyId);
        Native.UnregisterHotKey(Handle, PickId);
        HookNumbers();   // none, while capturing
        (box == hotkeyBox ? typeKeysButton : box == pickBox ? pickKeysButton : quickKeysButton).Text = Strings.PressKeys;
        status.Text = box == quickBox ? string.Format(Strings.PressQuickStatus, NameOf(DefaultQuickKeys | Keys.D1), Strings.DeleteKeyName)
            : string.Format(Strings.PressKeysStatus, Strings.DeleteKeyName);
    }

    // Stops waiting for keys: the hotkeys work again, with the keys they have now.
    void EndCapture()
    {
        if (capturing != null) UpdateHotkeys();
    }

    // A key pressed while a hotkey awaits its new keys. Always true: none of them does anything else.
    bool TakeKeys(Keys keys)
    {
        Keys code = keys & Keys.KeyCode;
        if (code == Keys.ControlKey || code == Keys.ShiftKey || code == Keys.Menu) return true;   // the key is still to come
        if (keys == Keys.Escape)
        {
            EndCapture();
            return true;
        }
        if (capturing == quickBox)
        {
            TakeQuickKeys(keys);
            return true;
        }
        bool typing = capturing == hotkeyBox;
        if (keys == Keys.Delete) keys = typing ? DefaultTypeKeys : DefaultPickKeys;
        if (!Usable(keys))
            status.Text = string.Format(Strings.KeysUnusable, NameOf(keys));
        // Nor the keys of the numbers, which would paste instead.
        else if (keys == (typing ? pickKeys : typeKeys) || Array.IndexOf(ListKeys, keys) >= 0 || NumberOf(keys, quickKeys) > 0)
            status.Text = string.Format(Strings.KeysTaken, NameOf(keys));
        else
        {
            CheckBox box = capturing;
            if (typing) typeKeys = keys;
            else pickKeys = keys;
            box.Checked = true;   // new keys are given to a hotkey that is wanted
            EndCapture();
            // Unticked again if Windows refused the keys: the status line already says so.
            if (box.Checked) Say(string.Format(typing ? Strings.TypeNowWith : Strings.OpenNowWith, NameOf(keys)));
        }
        return true;
    }

    // The keys of the numbers are pressed with any digit, which is left out: what counts is what is held with it.
    // Those that another program already has with some digit are named, so that other keys can be chosen.
    void TakeQuickKeys(Keys keys)
    {
        Keys modifiers = keys == Keys.Delete ? DefaultQuickKeys : keys & Keys.Modifiers;
        Keys code = keys & Keys.KeyCode;
        if (keys != Keys.Delete && !(code >= Keys.D0 && code <= Keys.D9 || code >= Keys.NumPad0 && code <= Keys.NumPad9)
            || !Usable(modifiers | Keys.D1))
        {
            status.Text = string.Format(Strings.QuickUnusable, NameOf(keys));
            return;
        }
        if (NumberOf(typeKeys, modifiers) > 0 || NumberOf(pickKeys, modifiers) > 0)
        {
            status.Text = string.Format(Strings.KeysTaken, NameOf(modifiers | Keys.D1));
            return;
        }
        // Tried one by one while all of ours are off: a number another program has refuses itself.
        List<string> taken = new List<string>();
        for (int number = 1; number <= MaxNumber; number++)
        {
            Keys tried = modifiers | (Keys.D0 + number);
            if (Hook(NumberId, tried)) Native.UnregisterHotKey(Handle, NumberId);
            else taken.Add(NameOf(tried));
        }
        quickKeys = modifiers;
        quickBox.Checked = true;
        EndCapture();
        if (taken.Count > 0) status.Text = string.Format(Strings.QuickBusy, string.Join(", ", taken));
        else Say(string.Format(Strings.QuickNowWith, NumbersName));
    }

    void UpdateHotkeys()
    {
        if (!IsHandleCreated) return;
        if (capturing != null)
        {
            // New keys were being awaited: no longer, and the buttons show again the ones in use.
            capturing = null;
            NameHotkeys();
        }
        bool typeFailed = !Register(hotkeyBox, HotkeyId, typeKeys);
        bool pickFailed = !Register(pickBox, PickId, pickKeys);
        numbersTaken = "";
        HookNumbers();   // says by itself which numbers another program has
        if (busy) return;
        if (typeFailed) status.Text = string.Format(Strings.HotkeyBusyType, HotkeyName);
        else if (pickFailed) status.Text = string.Format(Strings.HotkeyBusyPick, PickName);
        else if (numbersTaken.Length == 0) ShowIdle();
    }

    // Registers a hotkey if its checkbox is ticked. If Windows refuses it, unticks the checkbox and returns false;
    // the checkbox is remembered as 'refused', so that the setting is saved as the user left it and the next
    // start tries again.
    bool Register(CheckBox box, int id, Keys keys)
    {
        Native.UnregisterHotKey(Handle, id);
        if (!box.Checked) return true;
        if (Hook(id, keys))
        {
            refused.Remove(box);
            return true;
        }
        box.Checked = false;
        if (!refused.Contains(box)) refused.Add(box);
        return false;
    }

    // Asks Windows for a hotkey; false if another program has it.
    bool Hook(int id, Keys keys)
    {
        uint modifiers = ((keys & Keys.Control) != 0 ? Native.MOD_CONTROL : 0) | ((keys & Keys.Alt) != 0 ? Native.MOD_ALT : 0)
            | ((keys & Keys.Shift) != 0 ? Native.MOD_SHIFT : 0);
        return Native.RegisterHotKey(Handle, id, modifiers | Native.MOD_NOREPEAT, (uint)(keys & Keys.KeyCode));
    }

    // The keys of the numbers that some entry has, as hotkeys while the option is on: those of the others are
    // left to the other programs. Called again whenever the entries change. Which ones another program already
    // has is said once, not at every copy.
    void HookNumbers()
    {
        if (!IsHandleCreated) return;
        List<string> taken = new List<string>();
        for (int number = 1; number <= MaxNumber; number++)
        {
            Native.UnregisterHotKey(Handle, NumberId + number);
            if (!quickBox.Checked || capturing != null || Numbered(number) == null) continue;
            if (!Hook(NumberId + number, NumberKeys(number))) taken.Add(NumberName(number));
        }
        string said = string.Join(", ", taken);
        if (said.Length > 0 && said != numbersTaken && !busy) status.Text = string.Format(Strings.QuickBusy, said);
        numbersTaken = said;
    }

    // The keys of a number, from any program: its entry goes on the clipboard and is pasted into the window in
    // front, without showing this one. Where there is nothing to paste into (the desktop, this window) the
    // clipboard is enough.
    async void PasteNumber(int number)
    {
        Entry entry = Numbered(number);
        if (entry == null || busy || pasting) return;
        IntPtr target = Native.GetForegroundWindow();
        if (!Use(entry) || !IsValidTarget(target)) return;
        pasting = true;
        try
        {
            int delay = slowBox.Checked ? SlowKeyDelayMs : KeyDelayMs;
            TypeOutcome outcome = await Task.Run(delegate { return Typer.Paste(target, delay); });
            if (outcome == TypeOutcome.Done) Say(string.Format(Strings.Pasted, Shorten(entry.ToString()), WindowTitle(target)));
            else if (outcome == TypeOutcome.KeysHeld) status.Text = Strings.KeysHeld;
            else if (outcome == TypeOutcome.FocusLost) status.Text = Strings.PasteFocusLost;
            else status.Text = string.Format(Strings.Blocked, Program.AppName);
        }
        finally
        {
            pasting = false;
        }
    }
}
