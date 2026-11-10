using System;
using System.Drawing;
using System.Windows.Forms;

namespace SuperHeroes
{
    /// <summary>
    /// 우클릭 메뉴의 "알람 → 직접 정하기…" 창: 시각 + 내용(+ 매일 반복)을 정해 알람 하나를 만든다.
    /// 설정창 "내 알람"과 같은 알람(UserAlarm)이고, 만들면 바로 저장된다.
    /// </summary>
    internal sealed class AlarmComposer : Form
    {
        private readonly DateTimePicker time; private readonly TextBox msg; private readonly CheckBox repeat; private readonly Label summary;
        private readonly Func<UserAlarm, bool> create;

        internal AlarmComposer(Icon icon, Func<UserAlarm, bool> create)
        {
            this.create = create; FriendUI.Setup(this, icon);
            Text = "수퍼히어로 · 알람 만들기"; ClientSize = new Size(380, 222); ShowInTaskbar = false; TopMost = true;
            Controls.Add(new Label { Text = "몇 시에 알려 줄까?", AutoSize = true, Left = 14, Top = 14, ForeColor = FriendUI.Ink });
            DateTime first = DateTime.Now.AddMinutes(30);
            time = new DateTimePicker { Left = 14, Top = 38, Width = 84, Format = DateTimePickerFormat.Custom, CustomFormat = "HH:mm", ShowUpDown = true, Value = DateTime.Today.AddHours(first.Hour).AddMinutes(first.Minute / 5 * 5) };
            repeat = new CheckBox { Text = "매일 반복", Left = 116, Top = 40, AutoSize = true, ForeColor = FriendUI.Ink };
            Controls.Add(time); Controls.Add(repeat);
            Controls.Add(new Label { Text = "알람 내용", AutoSize = true, Left = 14, Top = 78, ForeColor = FriendUI.Ink });
            msg = new TextBox { Left = 14, Top = 100, Width = 352, MaxLength = 60, Text = "알람 시간이야!" };
            Controls.Add(msg);
            summary = new Label { Left = 14, Top = 134, Width = 352, Height = 40, ForeColor = Color.DimGray };
            Controls.Add(summary);
            var ok = new Button { Text = "만들기", Left = 200, Top = 182, Width = 80, Height = 28 };
            var cancel = new Button { Text = "취소", Left = 288, Top = 182, Width = 76, Height = 28, DialogResult = DialogResult.Cancel };
            Controls.Add(ok); Controls.Add(cancel); AcceptButton = ok; CancelButton = cancel;
            EventHandler update = delegate { UpdateSummary(); };
            time.ValueChanged += update; repeat.CheckedChanged += update;
            ok.Click += delegate
            {
                var a = new UserAlarm { On = true, Repeat = repeat.Checked, Time = TimeText, Msg = msg.Text.Trim() };
                if (create(a)) Close();
            };
            Shown += delegate { msg.Focus(); msg.SelectAll(); };
            UpdateSummary();
        }

        private string TimeText { get { return time.Value.ToString("HH:mm", System.Globalization.CultureInfo.InvariantCulture); } }

        private void UpdateSummary()
        {
            DateTime now = DateTime.Now, next = now.Date + time.Value.TimeOfDay;
            bool tomorrow = next.AddSeconds(60) <= now;   // 이미 지난 시각이면 다음 날
            summary.Text = (repeat.Checked ? "매일 " + TimeText + "에 알려 줘요." : (tomorrow ? "내일 " : "오늘 ") + TimeText + "에 한 번만 알려 주고 꺼져요.")
                + (repeat.Checked && tomorrow ? "\n오늘은 이미 지난 시각이라 내일부터 울려요." : "");
        }
    }
}
