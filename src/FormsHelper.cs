using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SuperHeroes
{
    /// <summary>체크하면 회색 취소선이 남는 할 일 목록.</summary>
    internal sealed class TodoCheckList : CheckedListBox
    {
        private Font completedFont;
        protected override void OnFontChanged(EventArgs e)
        { if (completedFont != null) { completedFont.Dispose(); completedFont = null; } base.OnFontChanged(e); }
        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= Items.Count) return;
            TodoItem task = Items[e.Index] as TodoItem;
            bool done = task != null && task.Done;
            e.DrawBackground();
            int glyph = Math.Max(13, Font.Height - 1), inset = 3;
            var check = new Rectangle(e.Bounds.Left + inset, e.Bounds.Top + (e.Bounds.Height - glyph) / 2, glyph, glyph);
            ControlPaint.DrawCheckBox(e.Graphics, check, done ? ButtonState.Checked : ButtonState.Normal);
            if (completedFont == null) completedFont = new Font(Font, Font.Style | FontStyle.Strikeout);
            bool selected = (e.State & DrawItemState.Selected) != 0;
            Color muted = Color.FromArgb((ForeColor.R * 2 + BackColor.R) / 3, (ForeColor.G * 2 + BackColor.G) / 3, (ForeColor.B * 2 + BackColor.B) / 3);
            Color color = selected ? SystemColors.HighlightText : done ? muted : ForeColor;
            var text = Rectangle.FromLTRB(check.Right + 6, e.Bounds.Top, e.Bounds.Right - 3, e.Bounds.Bottom);
            TextRenderer.DrawText(e.Graphics, Items[e.Index].ToString(), done ? completedFont : Font, text, color,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            e.DrawFocusRectangle();
        }
        protected override void Dispose(bool disposing)
        { if (disposing && completedFont != null) { completedFont.Dispose(); completedFont = null; } base.Dispose(disposing); }
    }

    /// <summary>도우미 창: 할 일 / 메모 / 타이머 / 도우미 설정.</summary>
    internal sealed class FriendPanel : Form
    {
        private readonly CheckedListBox tasks;
        private readonly TextBox notes;
        private readonly Label remaining;
        private readonly System.Windows.Forms.Timer timer;
        private readonly Label taskHeader;
        private bool populating, notesDirty;
        private DateTime notesChanged;

        internal FriendPanel(App app)
        {
            FriendUI.Setup(this, app.TrayIconImage); Text = "수퍼히어로 · 오늘의 작은 도우미"; ClientSize = new Size(550, 540);
            MinimizeBox = true; ShowInTaskbar = true;
            var tabs = new TabControl { Name = "friendTabs", Dock = DockStyle.Fill, Padding = new Point(12, 7) }; Controls.Add(tabs);

            var todo = new TabPage("할 일") { BackColor = BackColor, Padding = new Padding(12) }; tabs.TabPages.Add(todo);
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); todo.Controls.Add(layout);
            taskHeader = FriendUI.Label(""); layout.Controls.Add(taskHeader, 0, 0);
            tasks = new TodoCheckList { Name = "todoList", Dock = DockStyle.Fill, CheckOnClick = true, IntegralHeight = false, BorderStyle = BorderStyle.FixedSingle, DrawMode = DrawMode.OwnerDrawFixed };
            layout.Controls.Add(tasks, 0, 1);
            var entryRow = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = false };
            var entry = new TextBox { Width = 230, Margin = new Padding(3, 9, 3, 3), MaxLength = 200 };
            entryRow.Controls.Add(entry); entryRow.Controls.Add(FriendUI.Button("추가", delegate { AddTask(entry); }));
            entryRow.Controls.Add(FriendUI.Button("삭제", delegate { int i = tasks.SelectedIndex; if (i >= 0) { FriendData.Current.Todos.RemoveAt(i); RefreshTasks(); SaveOrWarn(); } }));
            entry.KeyDown += delegate (object sender, KeyEventArgs e) { if (e.KeyCode == Keys.Enter) { AddTask(entry); e.SuppressKeyPress = true; } };
            layout.Controls.Add(entryRow, 0, 2);
            RefreshTasks();
            tasks.ItemCheck += delegate (object sender, ItemCheckEventArgs e)
            {
                if (populating || e.Index >= FriendData.Current.Todos.Count) return;
                FriendData.Current.Todos[e.Index].Done = e.NewValue == CheckState.Checked;
                if (e.NewValue == CheckState.Checked) { FriendData.Current.Like(2); app.Announce("하나 끝냈다! 수고했어"); }
                SaveOrWarn(); tasks.Invalidate(); UpdateTaskSummary();
            };

            var memo = new TabPage("메모") { BackColor = BackColor, Padding = new Padding(12) }; tabs.TabPages.Add(memo);
            notes = new TextBox { Name = "memoText", Dock = DockStyle.Fill, Multiline = true, AcceptsReturn = true, ScrollBars = ScrollBars.Vertical, Text = FriendData.Current.Notes, MaxLength = 100000 };
            memo.Controls.Add(notes); notes.TextChanged += delegate { FriendData.Current.Notes = notes.Text; notesDirty = true; notesChanged = DateTime.UtcNow; };

            var time = new TabPage("타이머") { BackColor = BackColor, Padding = new Padding(15) }; tabs.TabPages.Add(time);
            var clock = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false }; time.Controls.Add(clock);
            clock.Controls.Add(FriendUI.Label("집중 시간이나 잠깐 쉬는 시간을 정해줘."));
            var duration = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
            var minutes = new NumericUpDown { Name = "timerMinutes", Minimum = 1, Maximum = 240, Value = 25, Width = 80, Margin = new Padding(4) };
            duration.Controls.Add(minutes); duration.Controls.Add(FriendUI.Label("분 뒤에 알려주기")); clock.Controls.Add(duration);
            var message = new TextBox { Name = "timerMessage", Width = 400, Text = FriendData.Current.TimerText, MaxLength = 200, Margin = new Padding(4, 8, 4, 8) }; clock.Controls.Add(message);
            var actions = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
            var startTimer = FriendUI.Button("시작", delegate
            { FriendData.Current.TimerText = message.Text; FriendData.Current.TimerUtc = DateTime.UtcNow.AddMinutes((double)minutes.Value); SaveOrWarn(); UpdateTimer(); });
            startTimer.Name = "startTimer"; actions.Controls.Add(startTimer);
            actions.Controls.Add(FriendUI.Button("타이머 취소", delegate { FriendData.Current.TimerUtc = DateTime.MinValue; SaveOrWarn(); UpdateTimer(); })); clock.Controls.Add(actions);
            remaining = FriendUI.Label(""); remaining.Font = new Font(Font.FontFamily, 20, FontStyle.Bold); clock.Controls.Add(remaining);
            clock.Controls.Add(FriendUI.Label("시간이 되면 직접 닫는 알림창이 떠요.\n도우미 창을 닫아도 타이머는 계속 돌아가요."));

            StyleTabs(tabs); ApplyOptions();
            timer = new System.Windows.Forms.Timer { Interval = 500 };
            timer.Tick += delegate { UpdateTimer(); UpdateTaskSummary(); if (notesDirty && (DateTime.UtcNow - notesChanged).TotalSeconds > 1) { notesDirty = false; SaveOrWarn(); } };
            timer.Start();
            FormClosed += delegate { timer.Stop(); timer.Dispose(); FriendData.Current.Notes = notes.Text; FriendData.Current.Save(); };
            UpdateTimer();
        }

        private void AddTask(TextBox entry)
        {
            if (string.IsNullOrWhiteSpace(entry.Text)) return;
            FriendData.Current.Todos.Add(new TodoItem { Text = entry.Text.Trim() }); entry.Clear(); RefreshTasks(); SaveOrWarn(); entry.Focus();
        }
        private void RefreshTasks()
        { populating = true; tasks.Items.Clear(); foreach (var t in FriendData.Current.Todos) tasks.Items.Add(t, t.Done); populating = false; UpdateTaskSummary(); }
        private void UpdateTaskSummary()
        {
            if (taskHeader == null) return;
            int done = FriendData.Current.Todos.Count(t => t.Done);
            taskHeader.Text = "하나씩 끝내자!   진행 " + (FriendData.Current.Todos.Count - done) + "  ·  완료 " + done;
        }
        private void UpdateTimer()
        {
            var d = FriendData.Current.TimerUtc;
            if (d == DateTime.MinValue) remaining.Text = "타이머 대기 중";
            else { int s = Math.Max(0, (int)Math.Ceiling((d - DateTime.UtcNow).TotalSeconds)); remaining.Text = string.Format("{0:00}:{1:00} 남았어", s / 60, s % 60); }
        }
        private void SaveOrWarn()
        { if (!FriendData.Current.Save()) MessageBox.Show(this, "파일에 저장하지 못했어. 저장 위치의 쓰기 권한을 확인해 줘.", "수퍼히어로"); }

        // ───── 도우미 창 설정은 통합 설정창(화면 · 시스템)으로 옮겼다 ─────
        private void StyleTabs(TabControl tabs)
        {
            tabs.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabs.DrawItem += delegate (object sender, DrawItemEventArgs e)
            {
                using (var brush = new SolidBrush(BackColor)) e.Graphics.FillRectangle(brush, e.Bounds);
                TextRenderer.DrawText(e.Graphics, tabs.TabPages[e.Index].Text, tabs.Font, e.Bounds, ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
                if (e.Index == tabs.SelectedIndex) using (var pen = new Pen(ForeColor, 2)) e.Graphics.DrawLine(pen, e.Bounds.Left + 5, e.Bounds.Bottom - 2, e.Bounds.Right - 5, e.Bounds.Bottom - 2);
            };
        }

        /// <summary>설정창에서 바꾼 맨 앞 고정 · 진하기 · 색상을 열려 있는 도우미 창에 적용.</summary>
        internal void ApplyOptions()
        {
            TopMost = PlayOptions.Current.MemoTopMost; Opacity = PlayOptions.Current.MemoOpacityPercent / 100.0;
            ApplyHelperColors();
        }
        private void ApplyHelperColors()
        {
            Color bg = Color.FromArgb(PlayOptions.Current.HelperBackgroundArgb), fg = Color.FromArgb(PlayOptions.Current.HelperForegroundArgb);
            SuspendLayout(); ApplyColors(this, bg, fg); ResumeLayout(); Invalidate(true);
        }

        private static void ApplyColors(Control control, Color bg, Color fg)
        {
            control.BackColor = bg; control.ForeColor = fg;
            var button = control as Button; if (button != null) button.UseVisualStyleBackColor = false;
            foreach (Control child in control.Controls) ApplyColors(child, bg, fg);
        }
    }
}
