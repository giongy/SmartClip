using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;

// The first start (wanted by the user on 28/10/2026): where the list is kept, what it remembers, what it encrypts, in a
// window of steps over the main one, made of the same rows as the options. A list of a version before is asked only
// what to encrypt, once (Clips.ProtectionChosen).
partial class MainForm
{
    enum Setup { None, FirstStart, Protection }
    Setup setupAtStart;   // what to ask as soon as this window shows
    const int PhraseBoxWidth = 220;   // the box of the recovery phrase, on its row

    // Whether the program never ran here: it has written no settings yet. Asked by Main before the window is made.
    public static bool FirstStart()
    {
        using (RegistryKey key = Registry.CurrentUser.OpenSubKey(SettingsKey)) return key == null;
    }

    // From Main: on the first start every step is asked once the window shows; with a list whose protection was never
    // chosen, that alone. Not from the checks, which make the window without Main.
    public void AskSetup(bool firstStart)
    {
        if (firstStart) setupAtStart = Setup.FirstStart;
        else if (!clips.ProtectionChosen && clips.FileName != Clips.Memory) setupAtStart = Setup.Protection;
    }

    void RunSetup()
    {
        Setup kind = setupAtStart;
        setupAtStart = Setup.None;
        if (kind == Setup.None) return;
        using (SetupWindow window = new SetupWindow(this, kind == Setup.FirstStart)) window.ShowDialog(this);
    }

    static string ProtectionName(Protection protection)
    {
        return protection == Protection.Nothing ? Strings.ProtectNothing : protection == Protection.Saved ? Strings.ProtectSaved
            : Strings.ProtectAll;
    }

    // What a choice of protection means, in a few lines.
    static string ProtectionExplained(Protection protection)
    {
        return protection == Protection.Nothing ? Strings.ProtectNothingExplain
            : protection == Protection.Saved ? Strings.ProtectSavedExplain : Strings.ProtectAllExplain;
    }

    // The steps: a name and what it is about on top, the rows of what it asks under it, and at the bottom how far along
    // it is, back and next. Every step is as tall as the tallest, so that the window does not change size. What is
    // chosen takes effect at the end, but the folder at once: the list there may already say what it encrypts.
    sealed class SetupWindow : Window
    {
        const int Edge = 20, TitleRow = 36, Gap = 12, ButtonEdge = 14;   // in pixels at 96 dpi
        const TextFormatFlags Flags = TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix;
        readonly MainForm main;
        readonly bool first;
        readonly Panel[] pages = new Panel[3];   // where, what to remember, what to encrypt
        readonly Label title = new Label(), about = new Label(), step = new Label();
        readonly RoundButton back = new RoundButton(), next = new RoundButton();
        readonly RoundButton changeButton = new RoundButton(), homeButton = new RoundButton();
        readonly Label where = new Label();
        readonly CheckBox history = new Toggle();
        readonly TextBox days = new TextBox();
        readonly RoundButton protect = new RoundButton();
        readonly TextBox phrase = new TextBox();
        readonly Label phraseHint = new Label();
        Tile protectTile, phraseTile;   // the phrase only where something is encrypted
        readonly string fresh;   // the list made at this start, empty: if another is used in its place, it goes
        Protection choice;
        List<int> shown;         // the steps asked, as indexes of 'pages'
        int at;                  // which of them is in view

        public SetupWindow(MainForm main, bool first)
        {
            this.main = main;
            this.first = first;
            fresh = main.clips.Created && main.clips.FileName != Clips.Memory ? main.clips.FileName : null;
            choice = first ? Protection.Nothing : main.clips.Protection;   // the user's choice: nothing encrypted, unless kept
            Text = first ? string.Format(Strings.SetupTitle, Program.AppName) : Strings.UpgradeTitle;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Secondary(FormStartPosition.CenterParent);
            BackColor = Look.Well;

            title.SetBounds(Edge, Edge, PaneWidth, TitleRow);
            title.Font = Look.TitleFont;
            title.UseMnemonic = false;
            int aboutHeight = 0;
            foreach (string text in new string[] { FolderAbout, HistoryAbout, ProtectAbout(Protection.Nothing),
                ProtectAbout(Protection.Saved), ProtectAbout(Protection.All) })
                aboutHeight = Math.Max(aboutHeight, (int)Math.Ceiling(TextRenderer.MeasureText(text, Look.TextFont,
                    new Size((int)(PaneWidth * Look.Zoom), 0), Flags).Height / Look.Zoom) + 1);
            about.SetBounds(Edge, title.Bottom, PaneWidth, aboutHeight);
            about.UseMnemonic = false;
            Controls.AddRange(new Control[] { title, about });

            Panel saved = main.page;   // the rows are placed where the options place theirs
            int top = about.Bottom + Gap, tallest = 0;
            for (int i = 0; i < pages.Length; i++)
            {
                pages[i] = new Panel();
                pages[i].Location = new Point(Edge, top);
                main.page = pages[i];
                int bottom = i == 0 ? PlaceFolder() : i == 1 ? PlaceHistory() : PlaceProtection();
                pages[i].Size = new Size(PaneWidth, bottom);
                tallest = Math.Max(tallest, bottom);
                Controls.Add(pages[i]);
            }
            main.page = saved;

            int buttonTop = top + tallest + ButtonEdge;
            next.SetBounds(Edge + PaneWidth - ButtonWidth, buttonTop, ButtonWidth, ButtonHeight);
            back.SetBounds(next.Left - Gap / 2 - ButtonWidth, buttonTop, ButtonWidth, ButtonHeight);
            back.Text = Strings.BackButton;
            Look.Style(next, true);
            Look.Style(back, false);
            back.Click += delegate { ShowStep(at - 1); };
            next.Click += delegate { Next(); };
            step.SetBounds(Edge, buttonTop, back.Left - Edge, ButtonHeight);
            step.TextAlign = ContentAlignment.MiddleLeft;
            step.ForeColor = Look.Muted;
            Controls.AddRange(new Control[] { step, back, next });
            ClientSize = new Size(Edge + PaneWidth + Edge, buttonTop + ButtonHeight + ButtonEdge);
            Finish();
            foreach (Panel page in pages) Tile.Snap(page);
            ShowStep(0);
        }

        string FolderAbout
        {
            get { return string.Format(Strings.SetupFolderExplain, Program.AppName); }
        }

        string HistoryAbout
        {
            get { return string.Format(Strings.SetupHistoryExplain, Program.AppName); }
        }

        // What the step of the protection says, as the choice changes: after a version before, first what changed.
        string ProtectAbout(Protection protection)
        {
            string said = ProtectionExplained(protection);
            return first ? said : Strings.UpgradeExplain + "\n\n" + said;
        }

        // Where the list is: changed at once, as the options do it, which asks before using a list found there and
        // unlocks its saved texts with their phrase.
        int PlaceFolder()
        {
            int top = 0;
            main.Act(changeButton, Strings.ChangeFolderButton, ChooseFolder);
            main.Act(homeButton, Strings.DefaultFolderButton, delegate { MoveTo(""); });
            main.PlaceTile(Sign.Folder, Strings.SetupFolderOption, Strings.SetupFolderNote, ref top, changeButton, homeButton);
            where.SetBounds(Tile.Inset, top + Gap, PaneWidth - 2 * Tile.Inset, NoteHeight);
            where.AutoEllipsis = true;
            where.UseMnemonic = false;
            where.ForeColor = Look.Muted;
            main.page.Controls.Add(where);
            ShowWhere();
            return where.Bottom;
        }

        int PlaceHistory()
        {
            int top = 0;
            history.Checked = main.historyBox.Checked;
            main.PlaceTile(Sign.History, Strings.HistoryOption, Strings.HistoryNote, ref top, main.Switch(history, Strings.HistoryOption));
            days.Text = main.Retention.ToString();
            main.PlaceNumber(Sign.Calendar, days, Strings.KeepOption, Strings.KeepNote, Texts.MaxKeepDays, delegate
            {
                days.Text = Number(days, 0, Texts.MaxKeepDays, Texts.DefaultKeepDays).ToString();
            }, ref top);
            history.CheckedChanged += delegate { days.Enabled = history.Checked; };
            return top;
        }

        int PlaceProtection()
        {
            int top = 0;
            main.Picker(protect, new Protection[] { Protection.Nothing, Protection.Saved, Protection.All }, ProtectionName,
                delegate { return choice; }, delegate(Protection chosen)
                {
                    choice = chosen;
                    protect.Text = ProtectionName(chosen);
                    ShowChoice();
                }, null);
            protectTile = main.PlaceTile(Sign.Lock, Strings.ProtectOption, Strings.ProtectNote, ref top, protect);
            phrase.SetBounds(0, 0, PhraseBoxWidth, NumberHeight);
            phraseTile = main.PlaceTile(Sign.Key, Strings.PhraseOptional, Strings.PhraseOptionalNote, ref top, Look.Frame(phrase));
            phraseHint.SetBounds(Tile.Inset, top + Gap, PaneWidth - 2 * Tile.Inset, NoteHeight);
            phraseHint.UseMnemonic = false;
            phraseHint.ForeColor = Look.Muted;
            main.page.Controls.Add(phraseHint);
            phrase.TextChanged += delegate { ShowChoice(); };
            return phraseHint.Bottom;
        }

        // The folder of the list, and whether the step of the protection is asked: not of a list that says it already.
        void ShowWhere()
        {
            where.Text = main.DataFolder;
            shown = new List<int>();
            if (first) shown.AddRange(new int[] { 0, 1 });
            if (!first || !main.clips.ProtectionChosen) shown.Add(2);
        }

        void ChooseFolder()
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = string.Format(Strings.ChooseDataFolder, Program.AppName);
                dialog.SelectedPath = main.DataFolder;
                if (dialog.ShowDialog(this) == DialogResult.OK) MoveTo(dialog.SelectedPath);
            }
        }

        void MoveTo(string folder)
        {
            main.MoveDataFolder(folder);
            ShowWhere();
            ShowStep(at);
        }

        // What the protection chosen means, said on top; the phrase only where something is encrypted, and under it
        // what it will be. Not finished with a phrase that is not three words.
        void ShowChoice()
        {
            if (shown != null && shown[at] == 2) about.Text = ProtectAbout(choice);
            bool saved = choice != Protection.Nothing;   // something is encrypted: the phrase can keep its key
            phraseTile.Visible = phraseHint.Visible = saved;
            protectTile.Invalidate();   // alone, or one card with the phrase: its corners change
            string words = Vault.Phrase(phrase.Text);
            bool written = saved && phrase.Text.Trim().Length > 0;
            phraseHint.Text = !written ? "" : words == null ? Strings.PhraseNotThree : string.Format(Strings.PhraseWillBe, words);
            next.Enabled = shown == null || shown[at] != 2 || !written || words != null;
        }

        // Shows a step of those asked.
        void ShowStep(int index)
        {
            at = Math.Max(0, Math.Min(index, shown.Count - 1));
            int page = shown[at];
            for (int i = 0; i < pages.Length; i++) pages[i].Visible = i == page;
            title.Text = page == 0 ? Strings.SetupFolderTitle : page == 1 ? Strings.SetupHistoryTitle : Strings.SetupProtectTitle;
            about.Text = page == 0 ? FolderAbout : page == 1 ? HistoryAbout : ProtectAbout(choice);
            step.Text = shown.Count > 1 ? string.Format(Strings.SetupStep, at + 1, shown.Count) : "";
            back.Visible = at > 0;
            bool last = at == shown.Count - 1;
            next.Text = last ? Strings.DoneButton : Strings.NextButton;
            AcceptButton = next;
            ShowChoice();
        }

        void Next()
        {
            if (at < shown.Count - 1)
            {
                ShowStep(at + 1);
                return;
            }
            Conclude();
            DialogResult = DialogResult.OK;
        }

        // What was chosen, at once: what to remember, what to encrypt with its phrase.
        void Conclude()
        {
            if (first)
            {
                main.historyBox.Checked = history.Checked;   // which applies it
                main.Retention = Number(days, 0, Texts.MaxKeepDays, Texts.DefaultKeepDays);
                main.ApplyHistory();
            }
            if (shown.Contains(2))
                main.Change(delegate
                {
                    main.clips.SetProtection(choice);
                    if (choice != Protection.Nothing && Vault.Phrase(phrase.Text) != null) main.clips.SetPhrase(phrase.Text);
                });
            main.SaveSettings();
        }

        // Closed without finishing: a list of a version before keeps what it had, and is not asked again; on the first
        // start what was chosen so far stays, and the protection is asked at the next. Either way the empty list made at
        // this start goes, if another took its place.
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            if (DialogResult != DialogResult.OK && !first) main.Change(delegate { main.clips.SetProtection(main.clips.Protection); });
            if (fresh != null && !SameFolder(fresh, main.clips.FileName) && File.Exists(fresh))
                try
                {
                    File.Delete(fresh);
                }
                catch (Exception ex)
                {
                    Log.Error(ex);   // left there: an empty list, nothing lost
                }
            main.Reload();
        }
    }
}
