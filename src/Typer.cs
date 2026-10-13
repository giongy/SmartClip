using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

class TypeOptions
{
    public bool UsKeyboard, AltCodes, PressEnter;
    public int KeyDelayMs;
}

enum TypeOutcome { Done, Cancelled, FocusLost, KeysHeld, Blocked }

class TypeResult
{
    public TypeOutcome Outcome;
    public int Typed, Unicode, AltCoded;

    public TypeResult Stop(TypeOutcome outcome)
    {
        Outcome = outcome;
        return this;
    }
}

// The key (and any modifiers) that produces a character.
struct Stroke
{
    public readonly int Scan;
    public readonly bool Shift, AltGr, Dead;

    public Stroke(int scan, bool shift, bool altGr, bool dead)
    {
        Scan = scan;
        Shift = shift;
        AltGr = altGr;
        Dead = dead;
    }
}

static class Typer
{
    const int ExtendedBit = 0x100;   // added to the scancode of "extended" keys
    const int ScTab = 0x0F, ScEnter = 0x1C, ScShift = 0x2A, ScAlt = 0x38, ScSpace = 0x39, ScAltGr = ScAlt | ExtendedBit;
    const int KeyReleaseTimeoutMs = 3000;   // how long to wait for the user to release the keys being held
    const byte Pressed = 0x80;              // value of a pressed key in the ToUnicodeEx state table
    static readonly int[] ScNumpad = { 0x52, 0x4F, 0x50, 0x51, 0x4B, 0x4C, 0x4D, 0x47, 0x48, 0x49 };
    static readonly Encoding Ansi =
        Encoding.GetEncoding(1252, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
    static readonly int[] TypingKeys = BuildTypingKeys();
    public static readonly Dictionary<char, Stroke> UsKeys = BuildUsKeys();

    // Types the text into the 'target' window and stops if it loses the foreground:
    // a password must never end up in another window.
    public static TypeResult TypeText(string text, IntPtr target, TypeOptions options, Func<bool> cancelled)
    {
        TypeResult result = new TypeResult();
        int delay = options.KeyDelayMs;
        if (!WaitKeysReleased()) return result.Stop(TypeOutcome.KeysHeld);

        uint processId;
        IntPtr layout = Native.GetKeyboardLayout(Native.GetWindowThreadProcessId(target, out processId));
        if (layout == IntPtr.Zero) layout = Native.GetKeyboardLayout(0);
        Dictionary<char, Stroke> keys = options.UsKeyboard ? UsKeys : MapLayout(layout);

        foreach (char c in text)
        {
            if (cancelled()) return result.Stop(TypeOutcome.Cancelled);
            if (Native.GetForegroundWindow() != target) return result.Stop(TypeOutcome.FocusLost);

            bool sent;
            Stroke stroke;
            if (c == '\n') sent = Chord(delay, ScEnter);
            else if (c == '\t') sent = Chord(delay, ScTab);
            else if (char.IsControl(c)) continue;
            else if (keys.TryGetValue(c, out stroke)) sent = Press(stroke, delay);
            else
            {
                int? code = options.AltCodes ? AnsiCode(c) : null;
                if (code.HasValue)
                {
                    sent = AltCode(code.Value, delay);
                    result.AltCoded++;
                }
                else
                {
                    sent = SendUnicode(c, delay);
                    result.Unicode++;
                }
            }

            if (!sent) return result.Stop(TypeOutcome.Blocked);
            result.Typed++;
        }

        if (options.PressEnter)
        {
            // Gives the server time to receive the last character before confirming.
            Thread.Sleep(delay * 4);
            if (Native.GetForegroundWindow() != target) return result.Stop(TypeOutcome.FocusLost);
            if (!Chord(delay, ScEnter)) return result.Stop(TypeOutcome.Blocked);
        }
        return result;
    }

    // A key the user is still holding (typically those of a hotkey) would alter what gets typed:
    // wait for them to be released.
    static bool WaitKeysReleased()
    {
        Stopwatch clock = Stopwatch.StartNew();
        while (AnyKeyHeld())
        {
            if (clock.ElapsedMilliseconds > KeyReleaseTimeoutMs) return false;
            Thread.Sleep(20);
        }
        return true;
    }

    static bool AnyKeyHeld()
    {
        int[] modifiers = { Native.VK_SHIFT, Native.VK_CONTROL, Native.VK_MENU, Native.VK_LWIN, Native.VK_RWIN };
        foreach (int key in modifiers)
            if (Native.GetAsyncKeyState(key) < 0) return true;
        for (int key = '0'; key <= 'Z'; key++)   // the key codes of digits and letters match the characters
            if (Native.GetAsyncKeyState(key) < 0) return true;
        return false;
    }

    // The keys of the alphanumeric block. The numeric keypad is left out on purpose: its decimal point, for
    // example, becomes Delete with Num Lock off (and it is the very key VkKeyScan proposes for the '.' character).
    static int[] BuildTypingKeys()
    {
        List<int> keys = new List<int>();
        for (int scan = 0x02; scan <= 0x0D; scan++) keys.Add(scan);   // number row
        for (int scan = 0x10; scan <= 0x1B; scan++) keys.Add(scan);   // Q row
        for (int scan = 0x1E; scan <= 0x29; scan++) keys.Add(scan);   // A row and the key left of 1
        for (int scan = 0x2B; scan <= 0x35; scan++) keys.Add(scan);   // Z row
        keys.AddRange(new int[] { ScSpace, 0x56, 0x73, 0x7D });      // space, the < key and the extra Brazilian/Japanese keys
        return keys.ToArray();
    }

    // Works out which key produces each character on the given layout: alone, with Shift, with AltGr
    // or with both. Characters missing from the result have no key.
    public static Dictionary<char, Stroke> MapLayout(IntPtr layout)
    {
        Dictionary<char, Stroke> keys = new Dictionary<char, Stroke>();
        Dictionary<char, Stroke> deadKeys = new Dictionary<char, Stroke>();
        char[] produced = new char[8];
        char[] discarded = new char[8];
        byte[] nothingPressed = new byte[256];

        // From the simplest combination to the most complex: if a character has several, the first one wins.
        for (int modifiers = 0; modifiers < 4; modifiers++)
        {
            bool shift = (modifiers & 1) != 0, altGr = (modifiers & 2) != 0;
            byte[] keyState = new byte[256];
            if (shift) keyState[Native.VK_SHIFT] = Pressed;
            if (altGr) keyState[Native.VK_CONTROL] = keyState[Native.VK_MENU] = Pressed;   // AltGr is equivalent to Ctrl+Alt

            foreach (int scan in TypingKeys)
            {
                uint vk = Native.MapVirtualKeyEx((uint)scan, Native.MAPVK_VSC_TO_VK, layout);
                if (vk == 0) continue;
                int count = Native.ToUnicodeEx(vk, (uint)scan, keyState, produced, produced.Length,
                    Native.TOUNICODE_KEEP_STATE, layout);
                char c = produced[0];
                if (count < 0)
                {
                    // Dead key (for example ^ on the German keyboard): it produces the character only when followed
                    // by a space. This second call clears it on Windows versions that ignore TOUNICODE_KEEP_STATE.
                    Native.ToUnicodeEx(Native.VK_SPACE, ScSpace, nothingPressed, discarded, discarded.Length,
                        Native.TOUNICODE_KEEP_STATE, layout);
                    if (!deadKeys.ContainsKey(c)) deadKeys.Add(c, new Stroke(scan, shift, altGr, true));
                }
                else if (count == 1 && !char.IsControl(c) && !keys.ContainsKey(c))
                {
                    keys.Add(c, new Stroke(scan, shift, altGr, false));
                }
            }
        }

        // A dead key is used only for characters that have no ordinary key.
        foreach (KeyValuePair<char, Stroke> dead in deadKeys)
            if (!keys.ContainsKey(dead.Key)) keys.Add(dead.Key, dead.Value);
        return keys;
    }

    // The US keyboard, for servers that use it whatever the layout of this PC.
    static Dictionary<char, Stroke> BuildUsKeys()
    {
        // For each row: scancode of the first key, characters without Shift, characters with Shift.
        Dictionary<char, Stroke> keys = new Dictionary<char, Stroke>();
        AddUsRow(keys, 0x02, "1234567890-=", "!@#$%^&*()_+");
        AddUsRow(keys, 0x10, "qwertyuiop[]", "QWERTYUIOP{}");
        AddUsRow(keys, 0x1E, "asdfghjkl;'`", "ASDFGHJKL:\"~");
        AddUsRow(keys, 0x2B, "\\zxcvbnm,./", "|ZXCVBNM<>?");
        keys[' '] = new Stroke(ScSpace, false, false, false);
        return keys;
    }

    static void AddUsRow(Dictionary<char, Stroke> keys, int firstScan, string plain, string shifted)
    {
        for (int i = 0; i < plain.Length; i++)
        {
            keys[plain[i]] = new Stroke(firstScan + i, false, false, false);
            keys[shifted[i]] = new Stroke(firstScan + i, true, false, false);
        }
    }

    // Code used to compose the character through Alt+numpad (Windows-1252), if it has one.
    static int? AnsiCode(char c)
    {
        try
        {
            byte[] encoded = Ansi.GetBytes(new char[] { c });
            if (encoded.Length == 1 && encoded[0] >= ' ') return encoded[0];
        }
        catch (EncoderFallbackException)
        {
            // The character does not exist in Windows-1252.
        }
        return null;
    }

    static bool Press(Stroke stroke, int delay)
    {
        bool sent = stroke.Shift && stroke.AltGr ? Chord(delay, ScShift, ScAltGr, stroke.Scan)
            : stroke.Shift ? Chord(delay, ScShift, stroke.Scan)
            : stroke.AltGr ? Chord(delay, ScAltGr, stroke.Scan)
            : Chord(delay, stroke.Scan);
        return sent && (!stroke.Dead || Chord(delay, ScSpace));
    }

    // Presses the keys in the given order and releases them in reverse order. Whatever was pressed is always
    // released, even if Windows rejects a send: a modifier left "pressed" would corrupt everything after it.
    static bool Chord(int delay, params int[] keys)
    {
        bool sent = true;
        int down = 0;
        while (sent && down < keys.Length)
        {
            sent = Send(keys[down], false);
            if (sent) down++;
            Thread.Sleep(delay);
        }
        while (down > 0)
        {
            sent &= Send(keys[--down], true);
            Thread.Sleep(delay);
        }
        return sent;
    }

    // Holds Alt and types the code on the numeric keypad. The leading zero tells Windows to
    // interpret it as Windows-1252, which is how AnsiCode computes it.
    static bool AltCode(int code, int delay)
    {
        if (!Send(ScAlt, false)) return false;
        Thread.Sleep(delay);
        bool sent = true;
        foreach (char digit in "0" + code)
            sent = sent && Chord(delay, ScNumpad[digit - '0']);
        sent &= Send(ScAlt, true);
        Thread.Sleep(delay);
        return sent;
    }

    static bool SendUnicode(char c, int delay)
    {
        bool sent = Inject(c, Native.KEYEVENTF_UNICODE);
        Thread.Sleep(delay);
        sent &= Inject(c, Native.KEYEVENTF_UNICODE | Native.KEYEVENTF_KEYUP);
        Thread.Sleep(delay);
        return sent;
    }

    static bool Send(int key, bool up)
    {
        uint flags = Native.KEYEVENTF_SCANCODE;
        if ((key & ExtendedBit) != 0) flags |= Native.KEYEVENTF_EXTENDEDKEY;
        if (up) flags |= Native.KEYEVENTF_KEYUP;
        return Inject((ushort)(key & ~ExtendedBit), flags);
    }

    static bool Inject(ushort scan, uint flags)
    {
        Native.INPUT[] input = new Native.INPUT[1];
        input[0].type = Native.INPUT_KEYBOARD;
        input[0].U.ki.wScan = scan;
        input[0].U.ki.dwFlags = flags;
        return Native.SendInput(1, input, Marshal.SizeOf(typeof(Native.INPUT))) == 1;
    }
}
