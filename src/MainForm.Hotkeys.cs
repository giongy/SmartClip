using System;
using System.Windows.Forms;

// The two global hotkeys: their names, the new keys the user gives them, their registration with Windows.
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

    // The keys of a number as the user reads them: those of the list hotkey and the digit, "Ctrl+D+1", which
    // is how they are pressed, holding Ctrl down.
    public static string NumberName(int number)
    {
        return PickName + "+" + number;
    }

    // The number that a combination chooses in the list, with 'pick' as the list hotkey: its modifiers with a
    // digit from 1, of the row above the letters or of the keypad. Zero if none.
    static int NumberOf(Keys keys, Keys pick)
    {
        Keys code = keys & Keys.KeyCode;
        if ((keys & Keys.Modifiers) != (pick & Keys.Modifiers)) return 0;
        int number = code >= Keys.D1 && code <= Keys.D9 ? code - Keys.D0 : code >= Keys.NumPad1 && code <= Keys.NumPad9 ? code - Keys.NumPad0 : 0;
        return number <= MaxNumber ? number : 0;
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
        Tip(string.Format(Strings.HotkeyTip, HotkeyName, Strings.TypeButton) + Strings.ChangeKeysNote, hotkeyBox);
        Tip(string.Format(Strings.PickTip, PickName, HotkeyName) + Strings.ChangeKeysNote, pickBox);
        Tip(string.Format(Strings.KeysTip, Strings.ShiftName, Strings.DeleteKeyName), typeKeysButton, pickKeysButton);
        Tip(string.Format(Strings.LeaveTip, PickName), leaveBox);
        Tip(string.Format(Strings.TypeTip, HotkeyName), typeButton);
        Tip(string.Format(Strings.SearchTip, Strings.EnterKeyName, HotkeyName, NameOf(PinKeys), NameOf(GroupKeys),
            NumberName(1), MaxNumber, Strings.NumberMenu), search);
    }

    // Waits for the keys of a global hotkey: the next combination pressed becomes it. Meanwhile both
    // hotkeys are off, or Windows would take them before this window sees them. Asked again, gives up.
    void AwaitKeys(CheckBox box)
    {
        bool again = capturing == box;
        EndCapture();
        if (again || busy) return;
        capturing = box;
        Native.UnregisterHotKey(Handle, HotkeyId);
        Native.UnregisterHotKey(Handle, PickId);
        (box == hotkeyBox ? typeKeysButton : pickKeysButton).Text = Strings.PressKeys;
        status.Text = string.Format(Strings.PressKeysStatus, Strings.DeleteKeyName);
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
        bool typing = capturing == hotkeyBox;
        if (keys == Keys.Escape)
        {
            EndCapture();
            return true;
        }
        if (keys == Keys.Delete) keys = typing ? DefaultTypeKeys : DefaultPickKeys;
        if (!Usable(keys))
            status.Text = string.Format(Strings.KeysUnusable, NameOf(keys));
        // Nor the keys of the numbers: those of the list hotkey with a digit, which it would then take for itself.
        else if (keys == (typing ? pickKeys : typeKeys) || Array.IndexOf(ListKeys, keys) >= 0
            || (typing ? NumberOf(keys, pickKeys) : NumberOf(typeKeys, keys) + NumberOf(keys, keys)) > 0)
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
        if (busy) return;
        if (typeFailed) status.Text = string.Format(Strings.HotkeyBusyType, HotkeyName);
        else if (pickFailed) status.Text = string.Format(Strings.HotkeyBusyPick, PickName);
        else ShowIdle();
    }

    // Registers a hotkey if its checkbox is ticked. If Windows refuses it, unticks the checkbox and returns false;
    // the checkbox is remembered as 'refused', so that the setting is saved as the user left it and the next
    // start tries again.
    bool Register(CheckBox box, int id, Keys keys)
    {
        Native.UnregisterHotKey(Handle, id);
        if (!box.Checked) return true;
        uint modifiers = ((keys & Keys.Control) != 0 ? Native.MOD_CONTROL : 0) | ((keys & Keys.Alt) != 0 ? Native.MOD_ALT : 0)
            | ((keys & Keys.Shift) != 0 ? Native.MOD_SHIFT : 0);
        if (Native.RegisterHotKey(Handle, id, modifiers | Native.MOD_NOREPEAT, (uint)(keys & Keys.KeyCode)))
        {
            refused.Remove(box);
            return true;
        }
        box.Checked = false;
        if (!refused.Contains(box)) refused.Add(box);
        return false;
    }
}
