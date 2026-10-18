using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

// Typing the clipboard: waiting for the target window, sending the keys, saying how it went.
partial class MainForm
{
    async void Run(int seconds)
    {
        if (busy) return;
        EndCapture();
        busy = true;
        cancel = false;
        typeButton.Text = Strings.Cancel;
        TopMost = true;   // countdown and cancel button in view over the window being clicked into
        try
        {
            string copied = Clipboard.ContainsText() ? Clipboard.GetText() : "";
            string text = copied.Replace("\r\n", "\n").Replace('\r', '\n').TrimEnd('\n');
            if (text.Length == 0)
            {
                Warn(Strings.NoText);
                return;
            }
            if ((text.Length > UnusualLength || text.IndexOf('\n') >= 0) && !ConfirmUnusual(text))
            {
                status.Text = Strings.Cancelled;
                return;
            }

            IntPtr target = await WaitForTarget(seconds);
            if (target == IntPtr.Zero) return;
            if ((Native.GetKeyState(Native.VK_CAPITAL) & 1) != 0)   // the low bit tells whether the lock is on
            {
                Warn(Strings.CapsLock);
                return;
            }

            TypeOptions options = new TypeOptions();
            options.UsKeyboard = usBox.Checked;
            options.AltCodes = altCodeBox.Checked;
            options.PressEnter = enterBox.Checked;
            options.KeyDelayMs = KeyDelay;

            status.Text = string.Format(Strings.TypingInto, WindowTitle(target));
            TypeResult result = await Task.Run(delegate
            {
                return Typer.TypeText(text, target, options, delegate { return cancel; });
            });
            status.Tell(Describe(result, options), KindOf(result));

            // Cleared only if the clipboard still holds the password just typed; the list forgets it too.
            if (result.Outcome == TypeOutcome.Done && clearBox.Checked
                && Clipboard.ContainsText() && Clipboard.GetText() == copied)
            {
                Clipboard.Clear();
                clips.Forget(copied);
                ReloadCopied();
            }
        }
        catch (Exception ex)   // for example the clipboard locked by another program
        {
            ShowError(ex);
        }
        finally
        {
            busy = false;
            typeButton.Text = Strings.TypeButton;
            TopMost = false;
            if (closePending) Close();
        }
    }

    // The pause after each key pressed or released, as the options want it.
    int KeyDelay
    {
        get { return slowBox.Checked ? SlowKeyDelayMs : KeyDelayMs; }
    }

    bool ConfirmUnusual(string text)
    {
        // A question whose owner is hidden or minimised would stay invisible.
        RestoreWindow();
        int lines = text.Split('\n').Length;
        string what = Count(text.Length) + (lines > 1 ? string.Format(Strings.OnLines, lines) : "");
        return AskForm.Ask(this, string.Format(Strings.AskUnusual, what), true);
    }

    // Waits for another program's window to stay in the foreground for 'seconds' seconds and returns it.
    // Returns IntPtr.Zero (and explains why in the status line) if the user cancels or does not choose in time.
    async Task<IntPtr> WaitForTarget(int seconds)
    {
        Stopwatch clock = Stopwatch.StartNew();
        IntPtr candidate = IntPtr.Zero;
        double candidateSince = 0;
        while (!cancel)
        {
            double now = clock.Elapsed.TotalSeconds;
            IntPtr foreground = Native.GetForegroundWindow();
            if (IsValidTarget(foreground))
            {
                if (foreground != candidate)
                {
                    candidate = foreground;
                    candidateSince = now;
                }
                double left = seconds - (now - candidateSince);
                if (left <= 0) return foreground;
                status.Text = string.Format(Strings.Countdown, Math.Ceiling(left), WindowTitle(foreground));
            }
            else if (now > ArmedSeconds)
            {
                Warn(Strings.TimeUp);
                return IntPtr.Zero;
            }
            else
            {
                // The right window still has to be selected: even when started from the hotkey, which has no
                // wait of its own, time is needed to click into the field.
                candidate = IntPtr.Zero;
                seconds = Math.Max(seconds, MinDelay);
                status.Text = Strings.ClickNow;
            }
            await Task.Delay(PollMs);
        }
        status.Text = Strings.Cancelled;
        return IntPtr.Zero;
    }

    static bool IsValidTarget(IntPtr window)
    {
        if (window == IntPtr.Zero) return false;
        uint processId;
        Native.GetWindowThreadProcessId(window, out processId);
        if (processId == OwnProcessId) return false;
        StringBuilder className = new StringBuilder(256);
        Native.GetClassName(window, className, className.Capacity);
        return Array.IndexOf(ShellClasses, className.ToString()) < 0;
    }

    static string WindowTitle(IntPtr window)
    {
        StringBuilder buffer = new StringBuilder(256);
        Native.GetWindowText(window, buffer, buffer.Capacity);
        string title = buffer.ToString().Trim();
        return title.Length == 0 ? Strings.Untitled : string.Format(Strings.Quote, Shorten(title));
    }

    // A text as long as a window title or an entry may be in the status line.
    static string Shorten(string text)
    {
        return Shorten(text, MaxTitleLength);
    }

    // At most 'limit' characters, the last of them the ellipsis if it is cut.
    static string Shorten(string text, int limit)
    {
        return text.Length > limit ? text.Substring(0, limit - 1) + "…" : text;
    }

    static string Describe(TypeResult result, TypeOptions options)
    {
        switch (result.Outcome)
        {
            case TypeOutcome.KeysHeld:
                return Strings.KeysHeld;
            case TypeOutcome.Blocked:
                return string.Format(Strings.Blocked, Program.AppName);
            case TypeOutcome.Cancelled:
                return result.Typed == 0 ? Strings.Cancelled : string.Format(Strings.StoppedAfter, Count(result.Typed));
            case TypeOutcome.FocusLost:
                return string.Format(Strings.FocusLost, Count(result.Typed));
        }

        string done = result.Typed == 1 ? Strings.DoneOne : string.Format(Strings.DoneMany, result.Typed);
        if (result.AltCoded > 0)
            done += string.Format(Strings.AltCoded, Count(result.AltCoded));
        if (result.Unicode > 0)
            done += (result.Unicode == 1 ? Strings.UnicodeOne : string.Format(Strings.UnicodeMany, result.Unicode))
                + (options.AltCodes ? "." : Strings.UnicodeHint);
        return done;
    }

    // What kind of message the outcome is: done, cancelled by the user, stopped by something else, blocked.
    static Notice KindOf(TypeResult result)
    {
        switch (result.Outcome)
        {
            case TypeOutcome.Done: return Notice.Done;
            case TypeOutcome.Cancelled: return Notice.Info;
            case TypeOutcome.Blocked: return Notice.Error;
            default: return Notice.Warning;
        }
    }

    static string Count(int characters)
    {
        return Count(characters, Strings.Character, Strings.Characters);
    }
}
