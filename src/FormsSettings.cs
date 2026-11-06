using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace SuperHeroes
{
    internal static class FriendUI
    {
        internal static readonly Color Paper = Color.FromArgb(255, 252, 249), Ink = Color.FromArgb(68, 50, 50);
        internal static readonly Color Orange = Color.FromArgb(244, 124, 30), OrangeDark = Color.FromArgb(206, 92, 10);
        internal static readonly Color Blue = Color.FromArgb(30, 77, 164), BlueDark = Color.FromArgb(20, 54, 122), Navy = Color.FromArgb(15, 36, 84), Sheet = Color.FromArgb(248, 250, 255), Gold = Color.FromArgb(255, 214, 10);
        internal static void Setup(Form f, Icon icon)
        {
            f.Font = new Font("Malgun Gothic", 9f); f.BackColor = Paper;
            f.StartPosition = FormStartPosition.CenterScreen; f.AutoScaleMode = AutoScaleMode.Dpi;
            f.MinimizeBox = false; f.MaximizeBox = false; f.FormBorderStyle = FormBorderStyle.FixedDialog;
            if (icon != null) f.Icon = icon;
        }
        internal static Button Button(string text, EventHandler click)
        { var b = new Button { Text = text, AutoSize = true, Padding = new Padding(6, 3, 6, 3), Margin = new Padding(4) }; b.Click += click; return b; }
        internal static Label Label(string text)
        { return new Label { Text = text, AutoSize = true, ForeColor = Ink, Margin = new Padding(4, 8, 4, 8) }; }
    }

    internal static class ControlExtensions
    {
        internal static T With<T>(this T control, Action<T> action) { action(control); return control; }
    }

    /// <summary>
    /// 바탕화면 캐릭터 토글 카드. 클릭 시 바탕화면 소환/퇴장 토글 및 도감 선택.
    /// 화면에 나와 있으면 컬러, 없으면 흑백(비활성)으로 표시된다.
    /// </summary>
    internal sealed class HeroToggleCard : Control
    {
        internal readonly int CharId;
        private readonly string heroName;
        private readonly Bitmap colorBmp;
        private readonly Bitmap grayBmp;
        private bool hot, down;
        internal bool IsActive { get; private set; }
        internal int Count { get; private set; }
        internal bool IsSelected { get; private set; }

        internal event EventHandler ToggleClicked;

        internal HeroToggleCard(int charId, ToolTip tips)
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable | ControlStyles.StandardClick, true);
            CharId = charId;
            heroName = CharacterArt.Infos[charId].Name;
            colorBmp = CharacterArt.IconBitmap(charId, 56);
            grayBmp = MakeGrayscale(colorBmp);
            Size = new Size(88, 88);
            Margin = new Padding(0, 0, 6, 0);
            Cursor = Cursors.Hand;
            TabStop = true;
            AccessibleName = heroName + " 토글";
            AccessibleRole = AccessibleRole.PushButton;
            UpdateTip(tips);
        }

        private void UpdateTip(ToolTip tips)
        {
            if (tips != null)
            {
                tips.SetToolTip(this, heroName + (IsActive ? " (현재 켜짐)\n클릭하면 화면에서 보내요." : " (현재 꺼짐)\n클릭하면 화면에 불러와요."));
            }
        }

        internal void UpdateState(bool active, int count, bool selected, ToolTip tips = null)
        {
            if (IsActive != active || Count != count || IsSelected != selected)
            {
                IsActive = active;
                Count = count;
                IsSelected = selected;
                if (tips != null) UpdateTip(tips);
                Invalidate();
            }
        }

        private static Bitmap MakeGrayscale(Bitmap src)
        {
            var bmp = new Bitmap(src.Width, src.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                var cm = new System.Drawing.Imaging.ColorMatrix(new float[][]
                {
                    new float[] { 0.299f, 0.299f, 0.299f, 0, 0 },
                    new float[] { 0.587f, 0.587f, 0.587f, 0, 0 },
                    new float[] { 0.114f, 0.114f, 0.114f, 0, 0 },
                    new float[] { 0,      0,      0,      0.50f, 0 },
                    new float[] { 0,      0,      0,      0, 1 }
                });
                using (var ia = new System.Drawing.Imaging.ImageAttributes())
                {
                    ia.SetColorMatrix(cm);
                    g.DrawImage(src, new Rectangle(0, 0, src.Width, src.Height),
                        0, 0, src.Width, src.Height, GraphicsUnit.Pixel, ia);
                }
            }
            return bmp;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.Clear(Parent != null ? Parent.BackColor : Color.White);

            float s = DeviceDpi / 96f;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = ChoiceStrip.RoundRect(rect, (int)Math.Round(7 * s)))
            {
                Color bg;
                if (IsActive) bg = down ? Color.FromArgb(220, 234, 255) : hot ? Color.FromArgb(234, 243, 255) : Color.FromArgb(244, 248, 255);
                else bg = down ? Color.FromArgb(232, 235, 240) : hot ? Color.FromArgb(240, 243, 247) : Color.FromArgb(247, 248, 250);

                using (var br = new SolidBrush(bg)) g.FillPath(br, path);

                Color border;
                float borderW = 1f;
                if (IsActive)
                {
                    border = hot || Focused ? FriendUI.BlueDark : FriendUI.Blue;
                    borderW = 1.5f;
                }
                else
                {
                    border = hot || Focused ? Color.FromArgb(170, 185, 205) : Color.FromArgb(215, 222, 232);
                }
                using (var pen = new Pen(border, borderW)) g.DrawPath(pen, path);

                if (IsSelected)
                {
                    using (var selBar = new SolidBrush(FriendUI.Gold))
                        g.FillRectangle(selBar, (Width - 28) / 2f, 1f, 28f, 2.5f);
                }

                var avatar = IsActive ? colorBmp : grayBmp;
                int imgSize = (int)Math.Round(42 * s);
                int imgX = (Width - imgSize) / 2;
                int imgY = (int)Math.Round(7 * s);
                g.DrawImage(avatar, new Rectangle(imgX, imgY, imgSize, imgSize));

                using (var fName = new Font("Malgun Gothic", 9f, FontStyle.Bold))
                using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    Color textCol = IsActive ? FriendUI.Ink : Color.FromArgb(135, 140, 150);
                    using (var br = new SolidBrush(textCol))
                        g.DrawString(heroName, fName, br, new RectangleF(0, 51 * s, Width, 18 * s), sf);
                }

                using (var fStatus = new Font("Malgun Gothic", 7.5f, FontStyle.Regular))
                using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    string statusText = IsActive ? (Count > 1 ? string.Format("● {0}명 켜짐", Count) : "● 켜짐") : "○ 꺼짐";
                    Color statusCol = IsActive ? Color.FromArgb(18, 138, 56) : Color.FromArgb(160, 165, 175);
                    using (var br = new SolidBrush(statusCol))
                        g.DrawString(statusText, fStatus, br, new RectangleF(0, 68 * s, Width, 16 * s), sf);
                }
            }
        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); hot = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hot = down = false; Invalidate(); }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left) { Focus(); down = true; Invalidate(); }
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (down)
            {
                down = false;
                Invalidate();
                if (ClientRectangle.Contains(e.Location))
                {
                    ToggleClicked?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter)
            {
                ToggleClicked?.Invoke(this, EventArgs.Empty);
                e.Handled = true;
            }
        }
        protected override bool IsInputKey(Keys keyData) { return (keyData & Keys.KeyCode) == Keys.Space || base.IsInputKey(keyData); }
        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (colorBmp != null) colorBmp.Dispose();
                if (grayBmp != null) grayBmp.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// 통합 설정창. 흩어져 있던 설정(알림 / 움직임·반응 / 친구 기록·생활 / 화면 / 메모 / AI)을 한 곳에 분류했다.
    ///   캐릭터 · 움직임 · 반응 · 생활 · 알림 · 화면·시스템 · 메모 · AI 대화
    /// 왼쪽에서 분류를 고르고, 저장 한 번으로 모든 분류가 함께 저장된다.
    /// </summary>
    internal sealed class SettingsForm : Form
    {
        internal static readonly string[] PageNames = { "캐릭터", "움직임", "반응", "생활", "알림", "화면 · 시스템", "메모", "AI 대화" };
        internal static readonly string[] PageHints = { "친구 · 도감 · ★ 기술", "이동 범위 · 속도 · 얌전 모드", "클릭 · 커서 · 숨은 반응", "친밀도 · 간식 · 배고픔", "정각 · 점심 · 퇴근 · 낮잠", "크기 · 투명도 · 자동 시작", "새 메모 기본값 · 단축키", "Ollama · 단축키 · 기억" };
        private static int lastPage;

        private readonly App app;
        private readonly ListBox nav;
        private readonly Panel host;
        private readonly Panel[] pages = new Panel[8];
        private readonly ToolTip tips = new ToolTip { AutoPopDelay = 12000 };
        private readonly Label status;
        private readonly System.Windows.Forms.Timer live;

        // 움직임
        private CheckBox cWalls, cCeiling, cWindows, cIdle, cShortRest, cPinned, cFocus, cWork, cFull;
        private NumericUpDown nSpeed, nFall, nSlide;
        // 반응
        private ComboBox clickMode; private RadioButton rEscape, rFollow, rIgnore; private NumericUpDown nDelay; private CheckBox cSecrets;
        // 생활
        private NumericUpDown nHunger; private CheckBox cHungerCry, cAffinity;
        private Label lifeSummary, lifeDetail, secretLabel; private ProgressBar satietyBar; private Button snackButton;
        // 알림
        private CheckBox cLunch, cLeave, cNap, cHourly, cPopup, cAlertPet, cGlow, cQuiet;
        private TextBox tLunch, tLunchMsg, tLeave, tLeaveMsg;
        private NumericUpDown nNap, nStart, nEnd;
        private ChoiceStrip glowTheme, glowWidth, glowStrength;
        // 화면·시스템
        private NumericUpDown nSize, nOpacity, nMemoOpacity; private CheckBox cMemoTop, cStartup;
        // 메모
        private ChoiceStrip noteColor, noteSize; private Label noteInfo;
        // AI 대화
        private ChoiceStrip cMode; private HotkeyBox hkBox;
        private CheckBox cChat, cChatKey, cChatSound, cChatProactive, cChatMemory, cChatChips, cChatDiary;
        private TextBox tChatModel, tChatUrl; private NumericUpDown nProactive; private Label chatProbe;
        private FlatButton bChatSetup, bChatSetupCancel; private ProgressBar chatSetupBar;
        private ChatRequest setupHandle;
        // 캐릭터
        private readonly HeroToggleCard[] heroCards = new HeroToggleCard[5];
        private ComboBox dexChar, sigFreq, costume; private PictureBox dexPic; private Label dexTitle, dexTraits, rosterTitle, dexStatus;
        private readonly Label[] sigName = new Label[8]; private readonly Label[] sigDesc = new Label[8]; private readonly FlowLayoutPanel[] sigRow = new FlowLayoutPanel[8];

        public SettingsForm(App app, int page = -1)
        {
            this.app = app;
            FriendUI.Setup(this, app.TrayIconImage);
            Text = "수퍼히어로 · 설정"; ClientSize = new Size(760, 640); MinimumSize = new Size(700, 560);
            FormBorderStyle = FormBorderStyle.Sizable; MaximizeBox = false; ShowInTaskbar = true; BackColor = SystemColors.Control;

            // 아래 단추줄
            var bar = new Panel { Dock = DockStyle.Bottom, Height = 52, BackColor = Color.FromArgb(232, 239, 250) };
            var bSave = new Button { Text = "저장", Width = 92, Height = 32, Anchor = AnchorStyles.Right | AnchorStyles.Top };
            var bApply = new Button { Text = "적용", Width = 92, Height = 32, Anchor = AnchorStyles.Right | AnchorStyles.Top };
            var bCancel = new Button { Text = "닫기", Width = 92, Height = 32, Anchor = AnchorStyles.Right | AnchorStyles.Top };
            status = new Label { AutoSize = false, Left = 14, Top = 8, Width = 330, Height = 36, ForeColor = Color.DimGray, TextAlign = ContentAlignment.MiddleLeft, Anchor = AnchorStyles.Left | AnchorStyles.Top };
            bar.Controls.AddRange(new Control[] { status, bSave, bApply, bCancel });
            bar.Resize += delegate { bCancel.Left = bar.Width - 14 - bCancel.Width; bApply.Left = bCancel.Left - 8 - bApply.Width; bSave.Left = bApply.Left - 8 - bSave.Width; bSave.Top = bApply.Top = bCancel.Top = 10; status.Width = Math.Max(0, bSave.Left - 8 - status.Left); };
            Controls.Add(bar);
            bSave.Click += delegate { if (Apply()) Close(); };
            bApply.Click += delegate { Apply(); };
            bCancel.Click += delegate { Close(); };
            AcceptButton = bSave; CancelButton = bCancel;

            // 왼쪽 분류 목록
            nav = new ListBox { Dock = DockStyle.Left, Width = 176, DrawMode = DrawMode.OwnerDrawFixed, ItemHeight = 54, BorderStyle = BorderStyle.None, IntegralHeight = false, BackColor = FriendUI.Navy, ForeColor = Color.White };
            nav.Items.AddRange(PageNames);
            nav.DrawItem += DrawNav;
            host = new Panel { Dock = DockStyle.Fill, BackColor = FriendUI.Sheet };
            Controls.Add(host); Controls.Add(nav);
            host.BringToFront();

            pages[0] = BuildCharacterPage(); pages[1] = BuildMovePage(); pages[2] = BuildReactPage();
            pages[3] = BuildLifePage(); pages[4] = BuildAlarmPage(); pages[5] = BuildDisplayPage();
            pages[6] = BuildMemoPage(); pages[7] = BuildChatPage();
            foreach (var pg in pages) { pg.Dock = DockStyle.Fill; pg.Visible = false; host.Controls.Add(pg); }
            nav.SelectedIndexChanged += delegate { ShowPage(nav.SelectedIndex); };
            nav.SelectedIndex = Math.Max(0, Math.Min(pages.Length - 1, page >= 0 ? page : lastPage));

            live = new System.Windows.Forms.Timer { Interval = 500 };
            live.Tick += delegate { RefreshLive(); };
            live.Start(); RefreshLive();
            FormClosed += delegate { live.Stop(); live.Dispose(); if (setupHandle != null) setupHandle.Abort(); };
        }

        internal void SelectPage(int i) { if (i >= 0 && i < pages.Length) nav.SelectedIndex = i; }

        // ───────── 분류 목록 그리기 ─────────
        private void DrawNav(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            bool sel = (e.State & DrawItemState.Selected) != 0;
            using (var bg = new SolidBrush(sel ? FriendUI.Blue : nav.BackColor)) e.Graphics.FillRectangle(bg, e.Bounds);
            if (sel) using (var bar = new SolidBrush(FriendUI.Gold)) e.Graphics.FillRectangle(bar, e.Bounds.Left, e.Bounds.Top, 5, e.Bounds.Height);
            using (var f1 = new Font("Malgun Gothic", 11f, FontStyle.Bold)) using (var f2 = new Font("Malgun Gothic", 7.8f))
            {
                string[] glyph = { "★", "▶", "☝", "♥", "♪", "▣", "✎", "✦" };
                TextRenderer.DrawText(e.Graphics, glyph[e.Index] + "  " + PageNames[e.Index], f1, new Rectangle(e.Bounds.Left + 14, e.Bounds.Top + 7, e.Bounds.Width - 16, 22), Color.White, TextFormatFlags.Left | TextFormatFlags.NoPrefix);
                TextRenderer.DrawText(e.Graphics, PageHints[e.Index], f2, new Rectangle(e.Bounds.Left + 14, e.Bounds.Top + 29, e.Bounds.Width - 16, 20), sel ? Color.FromArgb(214, 228, 250) : Color.FromArgb(150, 176, 220), TextFormatFlags.Left | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
            }
        }

        private void ShowPage(int i)
        {
            if (i < 0) return; lastPage = i;
            for (int k = 0; k < pages.Length; k++) pages[k].Visible = k == i;
            RefreshLive();
        }

        // ───────── 쪽 만들기 도구 ─────────
        private Panel NewPage(string title, string subtitle, out FlowLayoutPanel flow)
        {
            var page = new Panel { BackColor = FriendUI.Sheet };
            var head = new Panel { Dock = DockStyle.Top, Height = 62, BackColor = FriendUI.Sheet };
            head.Controls.Add(new Label { Text = title, AutoSize = true, Left = 20, Top = 10, Font = new Font("Malgun Gothic", 15f, FontStyle.Bold), ForeColor = FriendUI.BlueDark });
            head.Controls.Add(new Label { Text = subtitle, AutoSize = true, Left = 22, Top = 40, ForeColor = Color.DimGray });
            head.Paint += (s, e) => { using (var pen = new Pen(Color.FromArgb(190, 208, 238), 2)) e.Graphics.DrawLine(pen, 16, head.Height - 2, head.Width - 16, head.Height - 2); };
            flow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, Padding = new Padding(18, 6, 12, 12) };
            page.Controls.Add(flow); page.Controls.Add(head);
            head.SendToBack(); flow.BringToFront();
            return page;
        }

        private static GroupBox Group(FlowLayoutPanel parent, string title, int width = 500)
        {
            var g = new GroupBox { Text = title, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(10, 6, 10, 8), Margin = new Padding(2, 8, 2, 4), MinimumSize = new Size(width, 0), ForeColor = FriendUI.BlueDark };
            var inner = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Dock = DockStyle.Top, ForeColor = FriendUI.Ink };
            g.Controls.Add(inner); parent.Controls.Add(g);
            g.Tag = inner; return g;
        }
        private static FlowLayoutPanel In(GroupBox g) { return (FlowLayoutPanel)g.Tag; }

        private CheckBox Check(FlowLayoutPanel f, string text, bool value, string hint = null)
        {
            var c = new CheckBox { Text = text, Checked = value, AutoSize = true, Margin = new Padding(3, 3, 3, hint == null ? 3 : 0) };
            f.Controls.Add(c);
            if (hint != null) f.Controls.Add(new Label { Text = hint, AutoSize = true, ForeColor = Color.DimGray, Margin = new Padding(24, 0, 3, 5) });
            return c;
        }
        private static Label Note(FlowLayoutPanel f, string text, int maxW = 470)
        { var l = new Label { Text = text, AutoSize = true, ForeColor = Color.DimGray, Margin = new Padding(3, 3, 3, 3), MaximumSize = new Size(maxW, 0) }; f.Controls.Add(l); return l; }
        private static FlowLayoutPanel Row(FlowLayoutPanel f)
        { var r = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0, 1, 0, 1), FlowDirection = FlowDirection.LeftToRight }; f.Controls.Add(r); return r; }
        private Bitmap Glyph(char ch, Color col)
        {
            try
            {
                int px = Math.Max(16, (int)Math.Round(16 * DeviceDpi / 96.0));
                var bmp = new Bitmap(px, px);
                using (var g = Graphics.FromImage(bmp))
                using (var fnt = new Font("Segoe MDL2 Assets", px * 0.78f, FontStyle.Regular, GraphicsUnit.Pixel))
                using (var br = new SolidBrush(col))
                using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                    g.DrawString(ch.ToString(), fnt, br, new RectangleF(0, 0, px, px), sf);
                }
                return bmp;
            }
            catch { return null; }
        }
        private Button IconButton(char glyph, string text, string tip, EventHandler click)
        {
            var b = FriendUI.Button(text, click);
            var img = Glyph(glyph, FriendUI.BlueDark);
            if (img != null) { b.Image = img; b.TextImageRelation = TextImageRelation.ImageBeforeText; b.ImageAlign = ContentAlignment.MiddleLeft; b.TextAlign = ContentAlignment.MiddleRight; }
            else if (text.Length == 0) b.Text = "취소";
            if (img != null && text.Length == 0) { b.AutoSize = false; b.Size = new Size(36, b.Height > 0 ? Math.Max(b.Height, 27) : 27); b.ImageAlign = ContentAlignment.MiddleCenter; b.Padding = Padding.Empty; }
            tips.SetToolTip(b, tip); return b;
        }
        private static Label Cap(string text, int width = 0)
        { var l = new Label { Text = text, AutoSize = width == 0, Margin = new Padding(3, 6, 3, 0), ForeColor = FriendUI.Ink }; if (width > 0) { l.Width = width; l.Height = 22; } return l; }
        private static NumericUpDown Num(decimal min, decimal max, decimal value, int dec = 0, decimal inc = 1, int width = 70)
        { return new NumericUpDown { Minimum = min, Maximum = max, DecimalPlaces = dec, Increment = inc, Value = Math.Max(min, Math.Min(max, value)), Width = width, Margin = new Padding(3) }; }
        private static ComboBox Combo(string[] items, int selected, int width)
        { var c = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = width, Margin = new Padding(3) }; c.Items.AddRange(items); c.SelectedIndex = Math.Max(0, Math.Min(items.Length - 1, selected)); return c; }

        // ───────── 1. 캐릭터 ─────────
        private Panel BuildCharacterPage()
        {
            FlowLayoutPanel f; var page = NewPage("캐릭터", "함께할 친구를 부르고, 캐릭터별 ★ 시그니처 기술을 구경해요", out f);

            // 하나의 통합 섹션: 화면의 친구 · 캐릭터 도감
            var gHero = Group(f, "화면의 친구 · 캐릭터 도감"); var m = In(gHero);

            // 1) 상단 바: 제목 + [ 👁 모두 숨기기/보이기 ] [ 🎯 화면 가운데로 ] 플랫 아이콘 단추
            var headRow = new Panel { Width = 470, Height = 32, Margin = new Padding(2, 2, 2, 4) };
            rosterTitle = new Label { Text = "지금 화면에 있는 친구들", AutoSize = true, Left = 2, Top = 6, Font = new Font("Malgun Gothic", 9.2f, FontStyle.Bold), ForeColor = FriendUI.BlueDark };
            headRow.Controls.Add(rosterTitle);

            var bCenter = new FlatButton(FlatIcon.CenterScreen, "화면 가운데로", 34, 28) { Left = 470 - 34, Top = 2 };
            tips.SetToolTip(bCenter, "화면 가운데로 모으기\n화면 밖으로 나갔거나 가려진 친구들을 화면 한가운데로 다시 모아요.");
            bCenter.Click += delegate { app.RecoverVisible(); };
            headRow.Controls.Add(bCenter);

            var bHide = new FlatButton(FlatIcon.Eye, "모두 숨기기 / 보이기", 34, 28) { Left = 470 - 34 - 38, Top = 2 };
            tips.SetToolTip(bHide, "모두 숨기기 / 보이기\n화면의 모든 친구를 한 번에 숨기거나 다시 보여줘요.");
            bHide.Click += delegate { app.ToggleHidden(); RefreshRoster(); };
            headRow.Controls.Add(bHide);
            m.Controls.Add(headRow);

            // 2) 5개 캐릭터 토글 카드 행 (화면에 있으면 컬러, 없으면 흑백)
            var cardsRow = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, WrapContents = false, Margin = new Padding(0, 0, 0, 4) };
            for (int i = 0; i < CharacterArt.Count; i++)
            {
                int charId = i;
                var card = new HeroToggleCard(charId, tips);
                heroCards[i] = card;
                card.ToggleClicked += delegate { ToggleHero(charId); };
                cardsRow.Controls.Add(card);
            }
            m.Controls.Add(cardsRow);

            Note(m, "캐릭터 카드를 클릭하면 바탕화면에서 바로 켜고 끌 수 있어요.");

            // 3) 구분선 (은은한 분리)
            var div = new Panel { Height = 1, Width = 470, BackColor = Color.FromArgb(222, 230, 242), Margin = new Padding(3, 8, 3, 8) };
            m.Controls.Add(div);

            // 4) 캐릭터 도감 & ★ 시그니처 기술
            int initChar = app.LiveWindows.Count > 0 ? app.LiveWindows[0].Pet.Art.Id : 0;
            var pick = Row(m);
            pick.Controls.Add(new Label { Text = "캐릭터 도감", AutoSize = true, Font = new Font("Malgun Gothic", 9.2f, FontStyle.Bold), ForeColor = FriendUI.BlueDark, Margin = new Padding(3, 6, 6, 2) });
            dexChar = Combo(CharacterArt.Infos.Select(i => i.Name).ToArray(), initChar, 120);
            pick.Controls.Add(dexChar);
            dexStatus = new Label { AutoSize = true, ForeColor = Color.DimGray, Margin = new Padding(6, 7, 3, 2) };
            pick.Controls.Add(dexStatus);

            var head = Row(m);
            dexPic = new PictureBox { Width = 72, Height = 72, SizeMode = PictureBoxSizeMode.Zoom, Margin = new Padding(4) };
            head.Controls.Add(dexPic);
            var txt = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, WrapContents = false };
            head.Controls.Add(txt);
            dexTitle = new Label { AutoSize = true, Font = new Font("Malgun Gothic", 11f, FontStyle.Bold), ForeColor = FriendUI.Ink, Margin = new Padding(3, 3, 3, 2) };
            txt.Controls.Add(dexTitle);
            dexTraits = new Label { AutoSize = true, ForeColor = Color.DimGray, MaximumSize = new Size(380, 0), Margin = new Padding(3, 0, 3, 3) };
            txt.Controls.Add(dexTraits);

            m.Controls.Add(new Label { Text = "★ 시그니처 기술 (미리보기 단추를 누르면 바로 보여줘요)", AutoSize = true, ForeColor = Color.FromArgb(176, 108, 14), Font = new Font("Malgun Gothic", 9f, FontStyle.Bold), Margin = new Padding(3, 8, 3, 2) });
            for (int i = 0; i < 8; i++)
            {
                int idx = i; var row = Row(m); sigRow[i] = row;
                var star = new Label { Text = "★", AutoSize = true, ForeColor = FriendUI.Gold, Font = new Font("Malgun Gothic", 14f, FontStyle.Bold), Margin = new Padding(3, 0, 0, 0) };
                var box = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, WrapContents = false, Width = 330 };
                sigName[i] = new Label { AutoSize = true, Font = new Font("Malgun Gothic", 9.5f, FontStyle.Bold), ForeColor = FriendUI.Ink, Margin = new Padding(3, 2, 3, 0) };
                sigDesc[i] = new Label { AutoSize = true, ForeColor = Color.DimGray, MaximumSize = new Size(320, 0), Margin = new Padding(3, 0, 3, 4) };
                box.Controls.Add(sigName[i]); box.Controls.Add(sigDesc[i]);
                var play = FriendUI.Button("▶ 미리보기", delegate { app.PreviewSignature(dexChar.SelectedIndex, idx); });
                row.Controls.Add(star); row.Controls.Add(box); row.Controls.Add(play);
            }

            dexChar.SelectedIndexChanged += delegate
            {
                UpdateDex();
                RefreshRoster();
            };
            UpdateDex();

            var gSig = Group(f, "★ 시그니처 자동 재생"); var s = In(gSig);
            var rowS = Row(s); rowS.Controls.Add(Cap("가만히 쉴 때 저절로 보여주기"));
            sigFreq = Combo(new[] { "끄기", "드물게", "보통", "자주" }, PlayOptions.Current.SigFreq, 90); rowS.Controls.Add(sigFreq);
            Note(s, "얌전 모드에서는 자동으로 나오지 않아요. 손으로 보려면 우클릭 → ★ 시그니처 기술.");

            var gCos = Group(f, "수퍼맨 커스튬"); var cs = In(gCos);
            var rowC = Row(cs); rowC.Controls.Add(Cap("평소 입는 옷", 110));
            costume = Combo(new[] { "수퍼히어로 수트 (기본)", "기자 (클라크 켄트)" }, PlayOptions.Current.ClarkKent ? 1 : 0, 190); rowC.Controls.Add(costume);
            Note(cs, "기자 차림은 정장 · 붉은 넥타이 · 검은 뿔테 안경이에요. 어느 쪽이든 '셔츠 찢기 (S 각성)'은 기자 차림에서 시작해 수퍼맨으로 변신해요.");
            RefreshRoster();
            return page;
        }

        private void ToggleHero(int charId)
        {
            var live = app.LiveWindows.Where(w => w.Pet.Art.Id == charId).ToList();
            if (live.Count > 0)
            {
                foreach (var w in live) app.Remove(w);
            }
            else
            {
                app.SpawnFromUi(charId);
            }
            if (dexChar != null && charId >= 0 && charId < CharacterArt.Count)
            {
                dexChar.SelectedIndex = charId;
            }
            RefreshRoster();
        }

        private void UpdateDex()
        {
            if (dexChar == null || dexChar.SelectedIndex < 0) return;
            int c = Math.Max(0, dexChar.SelectedIndex); var info = CharacterArt.Infos[c];
            if (dexPic.Image != null) { var old = dexPic.Image; dexPic.Image = null; old.Dispose(); }
            dexPic.Image = CharacterArt.IconBitmap(c, 96);
            dexTitle.Text = info.Name + "  ·  " + info.Title;
            dexTraits.Text = "· " + string.Join("\n· ", info.Traits);
            var sigs = Signatures.All[c];
            for (int i = 0; i < sigName.Length; i++)
            {
                bool has = i < sigs.Length;
                if (sigRow[i] != null) sigRow[i].Visible = has;
                if (!has) continue;
                sigName[i].Text = sigs[i].Label;
                sigDesc[i].Text = sigs[i].Desc;
            }
            UpdateDexStatus();
        }

        private void UpdateDexStatus()
        {
            if (dexStatus == null || dexChar == null || dexChar.SelectedIndex < 0) return;
            int c = Math.Max(0, dexChar.SelectedIndex);
            int count = app.LiveWindows.Count(w => w.Pet.Art.Id == c);
            dexStatus.Text = count > 0 ? string.Format("(화면에 {0}명 활성)", count) : "(화면에 없음)";
            dexStatus.ForeColor = count > 0 ? FriendUI.BlueDark : Color.DimGray;
        }

        private void RefreshRoster()
        {
            var liveIds = app.LiveWindows.Select(w => w.Pet.Art.Id).ToList();
            if (rosterTitle != null)
            {
                rosterTitle.Text = liveIds.Count > 0 ? string.Format("지금 화면에 있는 친구들 ({0}명)", liveIds.Count) : "지금 화면에 있는 친구들 (비어 있음)";
            }
            int curSel = dexChar != null ? dexChar.SelectedIndex : -1;
            for (int i = 0; i < heroCards.Length; i++)
            {
                if (heroCards[i] != null)
                {
                    int count = liveIds.Count(id => id == i);
                    heroCards[i].UpdateState(count > 0, count, curSel == i, tips);
                }
            }
            UpdateDexStatus();
        }

        // ───────── 2. 움직임 ─────────
        private Panel BuildMovePage()
        {
            FlowLayoutPanel f; var page = NewPage("움직임", "어디를 돌아다니고 얼마나 빨리 움직일지 정해요", out f);
            var o = PlayOptions.Current;
            var g1 = Group(f, "돌아다니는 곳"); var a = In(g1);
            cWalls = Check(a, "화면 양옆 벽 타기", o.Walls);
            cCeiling = Check(a, "화면 천장 타기", o.Ceiling);
            cWindows = Check(a, "열린 창 옆을 올라가고 창 위에서 걷기 · 쉬기", o.Windows);
            cFull = Check(a, "전체화면 영상 · 게임을 볼 때 자동으로 숨기기", o.Fullscreen);
            var g2 = Group(f, "쉬는 방법"); var b = In(g2);
            cIdle = Check(b, "혼자 앉아서 쉬기 (앉기 · 잠자기 · 멍때리기 · 기지개 · ★ 시그니처)", o.Idle);
            cShortRest = Check(b, "앉아있는 시간 줄이기 (6~11초)", o.ShortRest);
            var g3 = Group(f, "조용히 지내기"); var c = In(g3);
            cFocus = Check(c, "얌전 모드: 바닥에만 머물고 도망가지 않기", o.Focus, "벽·천장·창 타기와 ★ 시그니처 자동 재생이 멈춰요.");
            cWork = Check(c, "평일 09:00~18:00에는 자동으로 얌전 모드", o.WorkFocus);
            cPinned = Check(c, "친구 위치 고정 (마우스로 옮길 수는 있어요)", o.PositionFixed);
            var g4 = Group(f, "속도  ·  숫자가 클수록 빨라져요"); var d = In(g4);
            Func<string, decimal, double, decimal, NumericUpDown> speedRow = (label, max, value, inc) =>
            { var r = Row(d); r.Controls.Add(Cap(label, 190)); var n = Num(.5m, max, (decimal)value, 1, .1m, 78); r.Controls.Add(n); r.Controls.Add(Cap("배")); return n; };
            nSpeed = speedRow("걷기 · 기어오르기", 2m, o.Speed, .1m);
            nFall = speedRow("비행 하강 (기를 모아 내려옴)", 3m, o.FallSpeed, .1m);
            nSlide = speedRow("질주 (커서를 피해 도망)", 3m, o.SlideSpeed, .1m);
            return page;
        }

        // ───────── 3. 반응 ─────────
        private Panel BuildReactPage()
        {
            FlowLayoutPanel f; var page = NewPage("반응", "클릭하거나 커서를 가까이 댔을 때 친구가 어떻게 반응할지 골라요", out f);
            var o = PlayOptions.Current;
            var g1 = Group(f, "마우스로 클릭했을 때"); var a = In(g1);
            clickMode = Combo(new[] { "순서대로: 놀람(발끈) → 달달 떨며 울기 → 폴짝", "무작위 반응", "놀람(발끈)만", "달달 떨며 울기만", "폴짝 뛰기만", "클릭 반응 끄기" }, (int)o.ClickMode, 400);
            a.Controls.Add(clickMode);
            Note(a, "배트맨은 경계하며 날카로운 눈빛, 다른 친구들은 당당하게 반응해요. 살짝 놓으면 망토/웹으로 천천히 착지하고, 휙 던지면 히어로 랜딩으로 착지해요.");
            var g2 = Group(f, "커서가 가까이 왔을 때"); var b = In(g2);
            rEscape = new RadioButton { Text = "슉! 질주하며 도망가기", AutoSize = true, Checked = o.Escape && !o.Follow, Margin = new Padding(3) };
            rFollow = new RadioButton { Text = "커서를 졸졸 따라오기", AutoSize = true, Checked = o.Follow && !o.Escape, Margin = new Padding(3) };
            rIgnore = new RadioButton { Text = "신경 쓰지 않기", AutoSize = true, Checked = !o.Escape && !o.Follow, Margin = new Padding(3) };
            b.Controls.Add(rEscape); b.Controls.Add(rFollow); b.Controls.Add(rIgnore);
            var r = Row(b); r.Controls.Add(Cap("도망 반응 대기", 130)); nDelay = Num(.05m, 1m, (decimal)o.EscapeDelay, 2, .05m, 78); r.Controls.Add(nDelay); r.Controls.Add(Cap("초"));
            var g3 = Group(f, "숨은 반응"); var c = In(g3);
            cSecrets = Check(c, "숨은 반응 사용", o.Secrets);
            Note(c, "어질어질: 친구 머리 위에서 마우스를 한 바퀴 빙글빙글.\n몰래 군무: 친구 둘 이상일 때 커서를 떨어뜨리고 4초.\n기 폭발: 같은 친구를 10초 안에 세 번 휙 던지기 (변신하며 천장까지 솟구쳐요).");
            return page;
        }

        // ───────── 4. 생활 ─────────
        private Panel BuildLifePage()
        {
            FlowLayoutPanel f; var page = NewPage("생활", "친밀도와 포만감, 배고픔 · 숨은 반응 도감", out f);
            var o = PlayOptions.Current;
            var g1 = Group(f, "지금 상태"); var a = In(g1);
            lifeSummary = new Label { AutoSize = true, Font = new Font("Malgun Gothic", 13f, FontStyle.Bold), ForeColor = FriendUI.Ink, Margin = new Padding(3, 4, 3, 4) }; a.Controls.Add(lifeSummary);
            var sr = Row(a); sr.Controls.Add(Cap("포만감"));
            satietyBar = new ProgressBar { Width = 220, Height = 18, Minimum = 0, Maximum = 100, Margin = new Padding(3, 7, 3, 3) }; sr.Controls.Add(satietyBar);
            snackButton = FriendUI.Button("간식 주기", delegate { app.FeedFirst(); RefreshLive(); }); sr.Controls.Add(snackButton);
            lifeDetail = new Label { AutoSize = true, ForeColor = Color.DimGray, Margin = new Padding(3, 3, 3, 3) }; a.Controls.Add(lifeDetail);
            Note(a, "간식은 포만감 +25씩(최대 100). 함께한 시간 1분마다 친밀도도 1씩 쌓여요. 100 단위마다 꽃다발!");
            var g2 = Group(f, "생활 설정"); var b = In(g2);
            var r = Row(b); r.Controls.Add(Cap("간식 없이 배고파지기까지", 190)); nHunger = Num(0, 360, o.HungerMinutes, 0, 1, 78); r.Controls.Add(nHunger); r.Controls.Add(Cap("분"));
            Note(b, "0분으로 설정하면 항상 우는 표정을 볼 수 있어요.");
            cHungerCry = Check(b, "배고프면 계속 우는 표정", o.HungerCry);
            cAffinity = Check(b, "친구 옆에 친밀도 숫자 표시", o.ShowAffinity);
            var g3 = Group(f, "발견한 숨은 반응"); var c = In(g3);
            secretLabel = new Label { AutoSize = true, ForeColor = FriendUI.Ink, MaximumSize = new Size(470, 0), Margin = new Padding(3, 3, 3, 3) }; c.Controls.Add(secretLabel);
            Note(c, "우클릭 → 동작 보기 → 숨은 반응에서도 확인할 수 있어요.");
            return page;
        }

        // ───────── 5. 알림 ─────────
        private Panel BuildAlarmPage()
        {
            FlowLayoutPanel f; var page = NewPage("알림", "정각 · 점심 · 퇴근 알림과 낮잠", out f);
            Settings s = app.Settings;
            var g1 = Group(f, "정각 알림"); var a = In(g1);

            // ① 맨 위: 켜기 + 미리보기
            var top = Row(a);
            cHourly = new CheckBox { Text = "정각 알림", Checked = s.HourlyOn, AutoSize = false, Width = 310, Height = 24, Margin = new Padding(3, 8, 3, 3) };
            top.Controls.Add(cHourly);
            var bPreview = IconButton('', "정각창 미리보기", "지금 고른 빛 · 폭 · 진하기로 정각 알림창을 미리 봐요", delegate
            {
                var n = new HourlyNotice(app.TrayIconImage); n.SetMessage(App.HourlyMessage(DateTime.Now, true, "13:00", "점심 먹으러 가자!", false, "18:00", "x") + "\n(미리보기)");
                n.EnableEdgeGlow(cGlow.Checked, glowTheme.SelectedIndex, glowWidth.SelectedIndex, glowStrength.SelectedIndex); n.Show();
            });
            bPreview.Margin = new Padding(6, 3, 3, 3); top.Controls.Add(bPreview);

            // ② 알림 방식: 두 가지를 나란히
            var methods = new TableLayoutPanel { AutoSize = true, ColumnCount = 2, Margin = Padding.Empty };
            methods.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240)); methods.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230));
            cPopup = new CheckBox { Text = "정각 알림창 띄우기 (화면 오른쪽 아래)", Checked = s.HourlyWindow, AutoSize = true, Margin = new Padding(3, 3, 3, 3) };
            cAlertPet = new CheckBox { Text = "친구들이 정각 알림 배너 들기", Checked = s.AlertPet, AutoSize = true, Margin = new Padding(3, 3, 3, 3) };
            methods.Controls.Add(cPopup, 0, 0); methods.Controls.Add(cAlertPet, 1, 0);
            a.Controls.Add(methods);
            cHourly.CheckedChanged += delegate { methods.Enabled = cHourly.Checked; }; methods.Enabled = cHourly.Checked;

            // ③ 가장자리 빛
            SubHead(a, "화면 가장자리 빛");
            cGlow = Check(a, "알림창을 닫을 때까지 화면 가장자리 빛 유지", s.EdgeGlow);
            var glowBox = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true, Margin = Padding.Empty };
            a.Controls.Add(glowBox);
            glowBox.Controls.Add(Tiny("빛 색상"));
            glowTheme = new ChoiceStrip(new[] { "파스텔", "노을", "오로라", "무지개" }, s.EdgeGlowTheme, 111, new Size(66, 38), delegate(Graphics g, Rectangle r, int i)
            { GlowThumb(g, r, EdgeGlowWindow.Palette(i), 7, 255); });
            tips.SetToolTip(glowTheme, "파스텔: 피치 · 핑크 · 보라 · 하늘");
            glowBox.Controls.Add(glowTheme);
            var size = new TableLayoutPanel { AutoSize = true, ColumnCount = 2, RowCount = 2, Margin = Padding.Empty };
            size.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 232)); size.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 238));
            glowWidth = new ChoiceStrip(new[] { "보통", "넓게", "아주 넓게" }, s.EdgeGlowWidth, 70, new Size(46, 30), delegate(Graphics g, Rectangle r, int i)
            { GlowThumb(g, r, EdgeGlowWindow.Palette(glowTheme.SelectedIndex), (i == 0 ? 3 : i == 1 ? 6 : 10) * r.Width / 46.0, 255); });
            glowStrength = new ChoiceStrip(new[] { "은은하게", "선명하게", "강하게" }, s.EdgeGlowStrength, 70, new Size(46, 30), delegate(Graphics g, Rectangle r, int i)
            { GlowThumb(g, r, EdgeGlowWindow.Palette(glowTheme.SelectedIndex), 6 * r.Width / 46.0, i == 0 ? 85 : i == 1 ? 170 : 255); });
            glowTheme.SelectedIndexChanged += delegate { glowWidth.Invalidate(); glowStrength.Invalidate(); };
            size.Controls.Add(Tiny("퍼지는 폭"), 0, 0); size.Controls.Add(Tiny("진하기"), 1, 0);
            size.Controls.Add(glowWidth, 0, 1); size.Controls.Add(glowStrength, 1, 1);
            glowBox.Controls.Add(size);
            cGlow.CheckedChanged += delegate { glowBox.Enabled = cGlow.Checked; }; glowBox.Enabled = cGlow.Checked;

            // ④ 조용한 시간 — 한 줄
            SubHead(a, "조용한 시간");
            var rq = Row(a);
            cQuiet = new CheckBox { Text = "평일(월~금) 정각 알림 끄기", Checked = s.QuietWeekdays, AutoSize = false, Width = 186, Height = 24, Margin = new Padding(3, 6, 3, 3) };
            nStart = Num(0, 23, s.QuietStart, 0, 1, 54); nEnd = Num(0, 23, s.QuietEnd, 0, 1, 54);
            rq.Controls.Add(cQuiet); rq.Controls.Add(nStart); rq.Controls.Add(Cap("시 ~")); rq.Controls.Add(nEnd); rq.Controls.Add(Cap("시 (양 끝 정각 포함)"));
            cQuiet.CheckedChanged += delegate { nStart.Enabled = nEnd.Enabled = cQuiet.Checked; }; nStart.Enabled = nEnd.Enabled = cQuiet.Checked;

            // 점심 · 퇴근: 한 줄에 켜기 · 시각 · 문구 · 미리보기
            var g2 = Group(f, "점심 · 퇴근 알림"); var b = In(g2);
            var head = Row(b); head.Controls.Add(Tiny("", 95)); head.Controls.Add(Tiny("시각", 56)); head.Controls.Add(Tiny("알림 문구"));
            var rl = Row(b);
            cLunch = new CheckBox { Text = "점심 알림", Checked = s.LunchOn, AutoSize = false, Width = 92, Height = 24, Margin = new Padding(3, 6, 3, 3) }; rl.Controls.Add(cLunch);
            tLunch = new TextBox { Text = s.LunchTime, Width = 56, Margin = new Padding(3, 4, 3, 3) }; rl.Controls.Add(tLunch);
            tLunchMsg = new TextBox { Text = s.LunchMsg, Width = 220, Margin = new Padding(3, 4, 3, 3) }; rl.Controls.Add(tLunchMsg);
            rl.Controls.Add(FriendUI.Button("미리보기", delegate { app.PreviewLunch(tLunchMsg.Text); }));
            var rv = Row(b);
            cLeave = new CheckBox { Text = "퇴근 알림", Checked = s.LeaveOn, AutoSize = false, Width = 92, Height = 24, Margin = new Padding(3, 6, 3, 3) }; rv.Controls.Add(cLeave);
            tLeave = new TextBox { Text = s.LeaveTime, Width = 56, Margin = new Padding(3, 4, 3, 3) }; rv.Controls.Add(tLeave);
            tLeaveMsg = new TextBox { Text = s.LeaveMsg, Width = 220, Margin = new Padding(3, 4, 3, 3) }; rv.Controls.Add(tLeaveMsg);
            rv.Controls.Add(FriendUI.Button("미리보기", delegate { app.PreviewLeave(tLeaveMsg.Text); }));
            Note(b, "시간은 12:00처럼 시:분으로 적어 주세요. 퇴근 10초 전에는 친구들이 화면 가운데로 모여요.");

            var g3 = Group(f, "낮잠"); var c = In(g3);
            var rn = Row(c);
            cNap = new CheckBox { Text = "자리를 오래 비우면 낮잠 자기", Checked = s.NapOn, AutoSize = false, Width = 196, Height = 24, Margin = new Padding(3, 6, 3, 3) };
            nNap = Num(1, 600, s.NapMinutes, 0, 1, 62);
            rn.Controls.Add(cNap); rn.Controls.Add(nNap); rn.Controls.Add(Cap("분 동안 입력이 없을 때"));
            cNap.CheckedChanged += delegate { nNap.Enabled = cNap.Checked; }; nNap.Enabled = cNap.Checked;
            return page;
        }

        // 작은 제목 + 오른쪽으로 이어지는 가는 선
        private static void SubHead(FlowLayoutPanel f, string text)
        {
            var p = new Panel { Width = 468, Height = 24, Margin = new Padding(0, 10, 0, 0) };
            p.Paint += (o, e) =>
            {
                using (var font = new Font("Malgun Gothic", 9f, FontStyle.Bold))
                {
                    Size sz = TextRenderer.MeasureText(e.Graphics, text, font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                    TextRenderer.DrawText(e.Graphics, text, font, new Point(3, (p.Height - sz.Height) / 2 + 2), FriendUI.Blue, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                    using (var pen = new Pen(Color.FromArgb(206, 218, 240))) e.Graphics.DrawLine(pen, sz.Width + 12, p.Height / 2 + 2, p.Width - 3, p.Height / 2 + 2);
                }
            };
            f.Controls.Add(p);
        }
        private static Label Tiny(string text, int width = 0)
        { var l = new Label { Text = text, AutoSize = width == 0, ForeColor = Color.DimGray, Margin = new Padding(3, 5, 3, 1), Font = new Font("Malgun Gothic", 8.5f) }; if (width > 0) { l.AutoSize = false; l.Width = width; l.Height = 16; } return l; }

        private static void GlowThumb(Graphics g, Rectangle r, Color[] pal, double thick, int alpha)
        {
            r = new Rectangle(r.X, r.Y, r.Width - 1, r.Height - 1);
            using (var path = ChoiceStrip.RoundRect(r, Math.Max(3, r.Height / 8)))
            {
                using (var fill = new SolidBrush(Color.FromArgb(250, 251, 255))) g.FillPath(fill, path);
                g.SetClip(path, CombineMode.Intersect);
                int steps = Math.Max(2, (int)Math.Round(thick));
                var blend = new ColorBlend(pal.Length) { Positions = pal.Select((c, i) => i / (float)(pal.Length - 1)).ToArray() };
                for (int k = 0; k < steps; k++)
                {
                    int al = (int)Math.Round(alpha * Math.Pow(1 - k / (double)steps, 1.35));
                    blend.Colors = pal.Select(c => Color.FromArgb(al, c)).ToArray();
                    using (var br = new LinearGradientBrush(new Rectangle(r.X, r.Y, Math.Max(2, r.Width), r.Height), Color.White, Color.White, LinearGradientMode.Horizontal) { InterpolationColors = blend })
                    using (var pen = new Pen(br, 1.5f)) g.DrawRectangle(pen, r.X + k, r.Y + k, r.Width - 2 * k, r.Height - 2 * k);
                }
                g.ResetClip();
                using (var pen = new Pen(Color.FromArgb(150, 165, 195), 1f)) g.DrawPath(pen, path);
            }
        }

        // ───────── 6. 화면 · 시스템 ─────────
        private Panel BuildDisplayPage()
        {
            FlowLayoutPanel f; var page = NewPage("화면 · 시스템", "크기 · 투명도 · 도우미 창 · Windows 시작", out f);
            var o = PlayOptions.Current;
            var g1 = Group(f, "캐릭터 모습"); var a = In(g1);
            var rs = Row(a); rs.Controls.Add(Cap("캐릭터 크기", 110)); nSize = Num(60, 200, o.SizePercent, 0, 10, 72); rs.Controls.Add(nSize); rs.Controls.Add(Cap("%   (바꾸면 친구들이 다시 등장해요)"));
            var rp = Row(a); rp.Controls.Add(Cap("캐릭터 진하기", 110)); nOpacity = Num(20, 100, o.PetOpacityPercent, 0, 5, 72); rp.Controls.Add(nOpacity); rp.Controls.Add(Cap("%   낮을수록 투명해요"));
            var g2 = Group(f, "도우미 창 (할 일 · 메모 · 타이머)"); var b = In(g2);
            cMemoTop = Check(b, "도우미 창을 맨 앞에 고정", o.MemoTopMost);
            var rm = Row(b); rm.Controls.Add(Cap("창 진하기", 110)); nMemoOpacity = Num(20, 100, o.MemoOpacityPercent, 0, 5, 72); rm.Controls.Add(nMemoOpacity); rm.Controls.Add(Cap("%"));
            var rcol = Row(b);
            rcol.Controls.Add(FriendUI.Button("배경색...", delegate { PickColor(true); }));
            rcol.Controls.Add(FriendUI.Button("글자색...", delegate { PickColor(false); }));
            rcol.Controls.Add(FriendUI.Button("기본 색상으로", delegate { o.HelperBackgroundArgb = FriendUI.Paper.ToArgb(); o.HelperForegroundArgb = FriendUI.Ink.ToArgb(); app.ApplyHelperOptions(); }));
            Note(b, "색상은 누르는 즉시 도우미 창에 적용돼요.");
            var g3 = Group(f, "Windows"); var c = In(g3);
            cStartup = Check(c, "Windows 로그인 시 자동으로 켜기", StartupOption.IsEnabled());
            var g4 = Group(f, "정리"); var d = In(g4);
            d.Controls.Add(FriendUI.Button("설정을 처음 상태로 되돌리기 (기록은 그대로)", delegate
            {
                if (MessageBox.Show(this, "모든 설정을 처음 값으로 되돌릴까요?\n친밀도·할 일·메모 같은 기록은 지워지지 않아요.", "수퍼히어로", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
                app.ResetAllSettings(); status.Text = "처음 설정으로 되돌렸어요."; Close(); app.OpenSettingsPage(lastPage);
            }));
            return page;
        }

        private void PickColor(bool background)
        {
            var o = PlayOptions.Current;
            using (var chooser = new ColorDialog { FullOpen = true, Color = Color.FromArgb(background ? o.HelperBackgroundArgb : o.HelperForegroundArgb) })
                if (chooser.ShowDialog(this) == DialogResult.OK)
                {
                    int argb = Color.FromArgb(chooser.Color.R, chooser.Color.G, chooser.Color.B).ToArgb();
                    if (background) o.HelperBackgroundArgb = argb; else o.HelperForegroundArgb = argb;
                    app.ApplyHelperOptions();
                }
        }

        // ───────── 7. 메모 ─────────
        // 글 대신 그림으로: 색 · 크기는 그림 카드로 고르고, 단축키는 아이콘 + 키캡 카드로 보여 준다. 자세한 설명은 마우스를 올리면 말풍선으로.
        private Panel BuildMemoPage()
        {
            FlowLayoutPanel f; var page = NewPage("메모", "바탕화면 메모의 기본 모습 · 단축키 · 저장 위치", out f);
            var mgr = app.Notes;

            var g1 = Group(f, "새 메모의 기본값", 520); var a = In(g1);
            var rc = Row(a); rc.Controls.Add(Badge(FlatIcon.Palette, "기본 색"));
            noteColor = new ChoiceStrip(NoteColors.All.Select(k => NoteColors.Label(k)).ToArray(), mgr == null ? 0 : Math.Max(0, Array.IndexOf(NoteColors.All, mgr.DefaultColor)), 50, new Size(28, 24),
                delegate(Graphics g, Rectangle r, int i) { FlatIcon.NoteShape(g, r, NoteColors.All[i]); });
            rc.Controls.Add(noteColor);
            var rs = Row(a); rs.Controls.Add(Badge(FlatIcon.Resize, "기본 크기"));
            // 크기 카드는 실제 메모 비율로 그리고, 위에서 고른 색으로 칠한다.
            Size[] dims = { new Size(NoteGeometry.SmallW, NoteGeometry.SmallH), new Size(NoteGeometry.MediumW, NoteGeometry.MediumH), new Size(NoteGeometry.LargeW, NoteGeometry.LargeH) };
            noteSize = new ChoiceStrip(new[] { "작게  240×180", "보통  320×260", "크게  420×360" }, mgr == null ? 1 : (int)mgr.DefaultSize, 140, new Size(38, 30),
                delegate(Graphics g, Rectangle r, int i)
                {
                    float w = r.Width * dims[i].Width / (float)NoteGeometry.LargeW, h = r.Height * dims[i].Height / (float)NoteGeometry.LargeH;
                    FlatIcon.NoteShape(g, new RectangleF(r.X + (r.Width - w) / 2f, r.Y + (r.Height - h) / 2f, w, h), NoteColors.All[Math.Max(0, noteColor.SelectedIndex)]);
                });
            noteColor.SelectedIndexChanged += delegate { noteSize.Invalidate(); };
            rs.Controls.Add(noteSize);
            IconText(Row(a), FlatIcon.Info, "저장한 다음에 만드는 메모부터 적용돼요.", "이미 있는 메모의 색과 크기는 그대로예요.", Color.DimGray);

            var g2 = Group(f, "어디서나 쓰는 단축키", 520); var b = In(g2);
            var hot = TileGrid(b, 2, 246);
            AddTile(hot, FlatIcon.NewNote, "새 메모", new[] { "Ctrl", "Alt", "N" }, "어디서나 새 메모를 만들어요.", true);
            AddTile(hot, FlatIcon.Library, "메모 라이브러리", new[] { "Ctrl", "Shift", "F" }, "모든 메모를 한눈에 보고 찾아요.", true);
            AddTile(hot, FlatIcon.Pin, "항상 위에 두기", new[] { "Ctrl", "Shift", "P" }, "마지막으로 쓴 메모를 항상 위에 두었다가 풀어요.", true);
            AddTile(hot, FlatIcon.BellClock, "1시간 뒤 알림", new[] { "Ctrl", "Shift", "R" }, "마지막으로 쓴 메모에 1시간 뒤 알림을 걸어요.", true);
            IconText(Row(b), FlatIcon.Warning, "다른 프로그램이 쓰는 조합이면 등록되지 않아요.", "그래도 트레이·우클릭 메뉴의 새 메모와, 메모 위젯 ⋯ 메뉴의 메모 라이브러리는 늘 쓸 수 있어요.", Color.DimGray);

            var g3 = Group(f, "메모를 쓰는 중", 520); var c = In(g3);
            var edit = TileGrid(c, 4, 122);
            AddTile(edit, FlatIcon.Bold, "굵게", new[] { "Ctrl", "B" }, "굵게", false);
            AddTile(edit, FlatIcon.Italic, "기울임", new[] { "Ctrl", "I" }, "기울임", false);
            AddTile(edit, FlatIcon.Underline, "밑줄", new[] { "Ctrl", "U" }, "밑줄", false);
            AddTile(edit, FlatIcon.Strike, "취소선", new[] { "Ctrl", "Shift", "X" }, "취소선", false);
            AddTile(edit, FlatIcon.Link, "링크", new[] { "Ctrl", "K" }, "링크", false);
            AddTile(edit, FlatIcon.Code, "코드", new[] { "Ctrl", "Shift", "K" }, "코드 블록", false);
            AddTile(edit, FlatIcon.Heading, "제목", new[] { "Ctrl", "1" }, "제목 1단계 (Ctrl+2, 3으로 2·3단계)", false);
            AddTile(edit, FlatIcon.Bullets, "글머리", new[] { "Ctrl", "Shift", "L" }, "글머리 기호 목록", false);
            AddTile(edit, FlatIcon.Checklist, "할 일 목록", new[] { "Ctrl", "Shift", "C" }, "체크리스트 만들기", false);
            AddTile(edit, FlatIcon.ToggleCheck, "체크 토글", new[] { "Ctrl", "Enter" }, "커서 줄의 체크박스 켜고 끄기", false);
            AddTile(edit, FlatIcon.EnterKey, "목록 이어쓰기", new[] { "Enter" }, "목록에서 엔터를 치면 다음 줄도 목록 (빈 줄에서 치면 끝)", false);

            var g4 = Group(f, "내 메모는 어디에", 520); var d = In(g4);
            var rWhere = Row(d);
            rWhere.Controls.Add(Badge(FlatIcon.Folder, "저장 위치"));
            var infoBox = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, Margin = new Padding(0, 10, 0, 0) };
            noteInfo = new Label { AutoSize = true, ForeColor = FriendUI.Ink, Text = mgr == null ? "" : mgr.Store.Root + "   (메모 " + mgr.Store.Count.ToString("N0") + "개)" };
            infoBox.Controls.Add(noteInfo);
            var btns = Row(infoBox);
            btns.Controls.Add(IconBtn(FlatIcon.OpenOut, "폴더 열기", delegate { try { if (mgr != null) System.Diagnostics.Process.Start("explorer.exe", "\"" + mgr.Store.Root + "\""); } catch { } }));
            btns.Controls.Add(IconBtn(FlatIcon.Copy, "백업 안내\n프로그램을 끈 뒤 이 폴더를 통째로 복사해 두면 안전해요.", delegate
            { MessageBox.Show(this, "백업하는 방법:\n\n1. 프로그램을 끕니다.\n2. 위의 '폴더 열기'로 연 폴더를 다른 디스크나 클라우드에 통째로 복사하세요.\n3. 새 PC 에서는 같은 위치에 덮어쓰면 모든 메모·태그·알림이 그대로 돌아와요.", "수퍼히어로 · 백업", MessageBoxButtons.OK, MessageBoxIcon.Information); }));
            btns.Controls.Add(IconBtn(FlatIcon.Trash, "삭제 주의\n지운 메모는 휴지통으로 가지 않고 바로 사라져요.", delegate
            { MessageBox.Show(this, "메모를 지울 때 주의:\n\n· 메모나 노트북을 지우면 휴지통을 거치지 않고 바로 삭제돼요.\n· 되돌릴 수 없으니 중요한 메모는 가끔 백업해 두세요.", "수퍼히어로 · 삭제 주의", MessageBoxButtons.OK, MessageBoxIcon.Warning); }));
            rWhere.Controls.Add(infoBox);
            return page;
        }

        // 설정 값 옆에 붙이는 그림 표지
        private IconBox Badge(FlatIcon.Painter p, string tip)
        { var b = new IconBox(p, 26) { Margin = new Padding(3, 14, 6, 3) }; tips.SetToolTip(b, tip); return b; }
        // 그림 + 한 줄 글(자세한 설명은 말풍선)
        private void IconText(FlowLayoutPanel row, FlatIcon.Painter p, string text, string tip, Color color)
        {
            var ib = new IconBox(p, 18) { Margin = new Padding(3, 4, 4, 3) };
            var l = new Label { Text = text, AutoSize = true, ForeColor = color, Margin = new Padding(0, 4, 14, 3), MaximumSize = new Size(440, 0) };
            row.Controls.Add(ib); row.Controls.Add(l);
            if (tip != null) { tips.SetToolTip(ib, tip); tips.SetToolTip(l, tip); }
        }
        private static TableLayoutPanel TileGrid(FlowLayoutPanel parent, int cols, int cellW)
        {
            var t = new TableLayoutPanel { AutoSize = true, ColumnCount = cols, Margin = new Padding(0, 2, 0, 0) };
            for (int i = 0; i < cols; i++) t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, cellW));
            parent.Controls.Add(t); return t;
        }
        private void AddTile(TableLayoutPanel t, FlatIcon.Painter p, string caption, string[] keys, string tip, bool wide)
        { var tile = new HintTile(p, caption, keys, wide); tips.SetToolTip(tile, tip); t.Controls.Add(tile); }

        // ───────── 8. AI 대화 ─────────
        // 글 대신 그림으로: 단추는 그림 단추, 설정마다 작은 아이콘을 붙이고 설명은 마우스를 올리면 말풍선으로. 스크롤 없이 한 화면에 들어간다.
        private static readonly FlatIcon.Painter[] ModeArt = { FlatIcon.Smile, FlatIcon.Heart, FlatIcon.Bulb, FlatIcon.Laugh, FlatIcon.Bolt };

        private IconBox Mark(FlatIcon.Painter p, string tip, int size = 22)
        { var b = new IconBox(p, size) { Margin = new Padding(3, 4, 2, 3) }; tips.SetToolTip(b, tip); return b; }

        private FlatButton IconBtn(FlatIcon.Painter p, string tip, EventHandler click)
        { var b = new FlatButton(p, tip); b.Click += click; tips.SetToolTip(b, tip); return b; }

        // 아이콘 + 켜고 끄는 칸. 아이콘을 눌러도 켜지고 꺼진다.
        private CheckBox IconCheck(FlowLayoutPanel row, FlatIcon.Painter p, string text, bool value, string tip)
        {
            var ib = Mark(p, tip); ib.Cursor = Cursors.Hand;
            var c = new CheckBox { Text = text, Checked = value, AutoSize = true, Margin = new Padding(2, 5, 8, 3) };
            ib.Click += delegate { c.Checked = !c.Checked; };
            tips.SetToolTip(c, tip); row.Controls.Add(ib); row.Controls.Add(c);
            return c;
        }

        private Panel BuildChatPage()
        {
            FlowLayoutPanel f; var page = NewPage("AI 대화", "내 PC의 Ollama와 이야기해요", out f);
            var o = ChatOptions.Current;

            // 연결: 켜기 · 단추 세 개 / 모델 · 서버 / 상태 한 줄
            var g1 = Group(f, "연결", 520); var a = In(g1);
            var ra = Row(a);
            ra.Controls.Add(new IconBox(FlatIcon.Chat, 24) { Margin = new Padding(3, 5, 0, 3) });
            cChat = new CheckBox { Text = "AI 대화 사용", Checked = o.Enabled, AutoSize = false, Width = 309, Height = 24, Margin = new Padding(5, 8, 3, 3) };
            tips.SetToolTip(cChat, "켜면 캐릭터를 더블클릭하거나 우클릭 → 말 걸기로 이야기할 수 있어요.");
            ra.Controls.Add(cChat);
            ra.Controls.Add(IconBtn(FlatIcon.PlugCheck, "연결 확인\n서버와 모델이 준비됐는지 봐요.", delegate { ProbeChat(); }));
            bChatSetup = IconBtn(FlatIcon.Download, "자동 설치\nOllama 와 모델이 없을 때 공식 사이트에서 받아 설치해요.\n서버가 이 PC(localhost)일 때만 되고, 받기 전에 한 번 물어봐요.", delegate { StartChatSetup(); });
            bChatSetupCancel = IconBtn(FlatIcon.Stop, "설치 취소", delegate { if (setupHandle != null) setupHandle.Abort(); }); bChatSetupCancel.Enabled = false;
            ra.Controls.Add(bChatSetup); ra.Controls.Add(bChatSetupCancel);

            var rb = Row(a);
            rb.Controls.Add(Mark(FlatIcon.Chip, "모델  (Ollama 모델 이름)"));
            tChatModel = new TextBox { Text = o.Model, Width = 150, Margin = new Padding(3, 3, 12, 3) }; rb.Controls.Add(tChatModel);
            rb.Controls.Add(Mark(FlatIcon.Globe, "서버 주소\n대화는 이 서버로만 보내져요.\n기본값(localhost)이면 이 PC 밖으로 나가지 않아요."));
            tChatUrl = new TextBox { Text = o.Endpoint, Width = 250, Margin = new Padding(3) }; rb.Controls.Add(tChatUrl);

            var rs = Row(a);
            chatProbe = new Label { Text = "○ 아직 확인 안 함", AutoSize = false, AutoEllipsis = true, Width = 330, Height = 20, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(6, 3, 3, 3), ForeColor = Color.DimGray };
            chatSetupBar = new ProgressBar { Width = 140, Height = 8, Margin = new Padding(6, 9, 3, 3), Visible = false };
            rs.Controls.Add(chatProbe); rs.Controls.Add(chatSetupBar);

            // 대화 방식: 그림 카드 다섯 장 + 단축키 한 줄
            var g2 = Group(f, "대화 방식", 520); var b = In(g2);
            cMode = new ChoiceStrip(ChatMode.All.Select(x => x.Name).ToArray(), o.Mode, 90, new Size(24, 24),
                delegate(Graphics g, Rectangle r, int i) { ModeArt[Math.Min(i, ModeArt.Length - 1)](g, new RectangleF(r.X, r.Y, r.Width, r.Height), FriendUI.BlueDark); });
            cMode.HotChanged += i =>
            {
                if (i < 0 || i >= ChatMode.All.Length) tips.Hide(cMode);
                else tips.Show(ChatMode.All[i].Desc, cMode, Math.Max(0, Math.Min(cMode.Width - 150, cMode.PointToClient(Cursor.Position).X - 20)), cMode.Height + 2, 6000);
            };
            b.Controls.Add(cMode);

            var rk = Row(b);
            rk.Controls.Add(Mark(FlatIcon.Keyboard, "단축키로 말 걸기\n입력란을 클릭한 뒤 키를 하나씩 누르세요.\nCtrl · Alt · Shift 중 2개 이상 + 글쇠 1개.\nEsc 는 취소, Backspace 는 방금 누른 키 취소예요."));
            cChatKey = new CheckBox { Text = "단축키", Checked = o.Hotkey, AutoSize = false, Width = 76, Height = 24, Margin = new Padding(2, 5, 3, 3) }; rk.Controls.Add(cChatKey);
            hkBox = new HotkeyBox(o.HotkeyMods, o.HotkeyKey) { Width = 190, Margin = new Padding(3, 3, 6, 3) }; rk.Controls.Add(hkBox);
            var bKeyReset = IconBtn(FlatIcon.Reset, "기본 단축키로 되돌리기", delegate { hkBox.SetValue(ChatHotkey.DefaultMods, ChatHotkey.DefaultKey); tips.Hide(hkBox); });
            rk.Controls.Add(bKeyReset);
            hkBox.Hint += t => { if (string.IsNullOrEmpty(t)) tips.Hide(hkBox); else tips.Show(t, hkBox, 0, hkBox.Height + 3, 30000); };
            cChatKey.CheckedChanged += delegate { hkBox.Enabled = bKeyReset.Enabled = cChatKey.Checked; };
            hkBox.Enabled = bKeyReset.Enabled = cChatKey.Checked;

            // 부가 기능: 두 칸 표
            var g3 = Group(f, "부가 기능", 520); var c = In(g3);
            var grid = new TableLayoutPanel { AutoSize = true, ColumnCount = 2, Margin = Padding.Empty };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 250)); grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 250));
            Func<FlowLayoutPanel> cell = () => { var r = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = Padding.Empty, FlowDirection = FlowDirection.LeftToRight }; return r; };
            var c00 = cell(); cChatChips = IconCheck(c00, FlatIcon.ReplyChips, "답장 추천", o.Chips, "캐릭터의 말 끝에 짧은 답장 칩을 보여 줘요.\n눌러서 바로 보내요 (Ctrl+1~3). 글자를 치면 사라져요.");
            var c10 = cell(); cChatSound = IconCheck(c10, FlatIcon.Sound, "글자 소리", o.Sound, "말할 때 글자마다 뽁뽁 소리가 나요.");
            var c01 = cell(); cChatProactive = IconCheck(c01, FlatIcon.Spark, "먼저 말 걸기", o.Proactive, "컴퓨터를 쓰는 중에 친구가 가끔 먼저 한마디 건네요.\n켜면 모델이 메모리에 올라가 있어요.");
            var c11 = cell(); c11.Controls.Add(Mark(FlatIcon.Clock, "평균 간격 (분)"));
            nProactive = Num(5, 480, o.ProactiveMinutes, 0, 5, 64); c11.Controls.Add(nProactive); c11.Controls.Add(Cap("분마다"));
            cChatProactive.CheckedChanged += delegate { nProactive.Enabled = cChatProactive.Checked; }; nProactive.Enabled = cChatProactive.Checked;
            var c02 = cell(); cChatMemory = IconCheck(c02, FlatIcon.Memory, "기억하기", o.Memory, "대화에서 알게 된 사실을 한두 줄로 적어 둬요.\n이 PC 의 파일에만 저장돼요.");
            c02.Controls.Add(IconBtn(FlatIcon.OpenOut, "기억 보기 · 지우기", delegate { new ChatMemoryForm(app.TrayIconImage).ShowDialog(this); }));
            var c12 = cell(); cChatDiary = IconCheck(c12, FlatIcon.Diary, "일기 쓰기", o.Diary, "대화가 끝나면 친구가 오늘 이야기를 일기 한두 줄로 적어요.\n이 PC 의 파일(수퍼히어로_일기.txt)에만 저장돼요.");
            c12.Controls.Add(IconBtn(FlatIcon.OpenOut, "일기장 열기", delegate { new ChatDiaryForm(app.TrayIconImage).ShowDialog(this); }));
            grid.Controls.Add(c00, 0, 0); grid.Controls.Add(c10, 1, 0);
            grid.Controls.Add(c01, 0, 1); grid.Controls.Add(c11, 1, 1);
            grid.Controls.Add(c02, 0, 2); grid.Controls.Add(c12, 1, 2);
            c.Controls.Add(grid);
            return page;
        }

        private void SetChatStatus(string text, Color color)
        { chatProbe.ForeColor = color; chatProbe.Text = text; tips.SetToolTip(chatProbe, text); }

        private void ProbeChat()
        {
            var probe = new ChatOptions { Endpoint = tChatUrl.Text.Trim().TrimEnd('/'), Model = tChatModel.Text.Trim() };
            SetChatStatus("확인하는 중...", Color.DimGray);
            new System.Threading.Thread(delegate ()
            {
                ProbeResult r = OllamaClient.Probe(probe);
                try
                {
                    BeginInvoke(new Action(delegate
                    {
                        SetChatStatus(r == ProbeResult.Ok ? "● 연결됐어요" : r == ProbeResult.ModelMissing ? "● 모델이 없어요 — 설치(↓) 단추를 눌러요" : "● 연결 안 됨 — Ollama 가 켜져 있나요?", r == ProbeResult.Ok ? Color.FromArgb(30, 130, 70) : Color.Firebrick);
                    }));
                }
                catch { }
            }) { IsBackground = true }.Start();
        }

        // ───────── AI 자동 설치 (Ollama + 모델) ─────────
        private void StartChatSetup()
        {
            if (setupHandle != null) return;
            string url = tChatUrl.Text.Trim().TrimEnd('/'), model = tChatModel.Text.Trim();
            if (url.Length == 0) url = "http://localhost:11434";
            if (model.Length == 0) model = "gemma4:e2b";
            if (!OllamaSetup.IsLocalEndpoint(url)) { Warn("자동 설치는 서버 주소가 이 PC(localhost)일 때만 쓸 수 있어요.\n다른 PC 의 서버를 쓰는 중이라면 그 PC 에서 직접 설치해 주세요."); return; }
            var opts = new ChatOptions { Endpoint = url, Model = model };
            var handle = setupHandle = new ChatRequest();
            ShowSetup("시작하는 중...", -1, true); bChatSetup.Enabled = false; chatSetupBar.Visible = true;
            new System.Threading.Thread(delegate ()
            {
                string err;
                try
                {
                    err = OllamaSetup.Run(opts, handle,
                        (text, pct, cancellable) => PostUi(delegate { ShowSetup(text, pct, cancellable); }),
                        msg =>
                        {
                            bool yes = false;
                            try { Invoke(new Action(delegate { yes = MessageBox.Show(this, msg, "수퍼히어로 · AI 자동 설치", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes; })); } catch { }
                            return yes;
                        });
                }
                catch (Exception e) { err = "예상하지 못한 오류가 났어요. (" + e.Message + ")"; }
                PostUi(delegate { FinishSetup(handle, err); });
            }) { IsBackground = true }.Start();
        }

        private void PostUi(Action a) { try { if (!IsDisposed) BeginInvoke(a); } catch { } }

        private void ShowSetup(string text, int pct, bool cancellable)
        {
            SetChatStatus(text, Color.DimGray);
            if (pct < 0) chatSetupBar.Style = ProgressBarStyle.Marquee;
            else { chatSetupBar.Style = ProgressBarStyle.Continuous; chatSetupBar.Value = Math.Min(100, pct); }
            bChatSetupCancel.Enabled = cancellable;
        }

        private void FinishSetup(ChatRequest handle, string err)
        {
            if (setupHandle == handle) setupHandle = null;
            chatSetupBar.Visible = false; bChatSetup.Enabled = true; bChatSetupCancel.Enabled = false;
            if (err == null) SetChatStatus("● 준비 끝 — 'AI 대화 사용'을 켜고 저장하세요", Color.FromArgb(30, 130, 70));
            else if (err == OllamaSetup.Cancelled) SetChatStatus("취소했어요 — 다시 누르면 이어서 받아요", Color.DimGray);
            else SetChatStatus(err, Color.Firebrick);
        }

        // ───────── 실시간 표시 ─────────
        private void RefreshLive()
        {
            if (lifeSummary == null) return;
            var d = FriendData.Current;
            lifeSummary.Text = "♥ 친밀도 " + d.Affinity.ToString("N0") + "   ·   함께 " + FriendData.FormatTogether(d.TogetherSeconds);
            satietyBar.Value = Math.Max(0, Math.Min(100, (int)Math.Floor(d.Satiety)));
            lifeDetail.Text = "포만감 " + (int)Math.Floor(d.Satiety) + " / 100   ·   다음 꽃다발: 친밀도 " + ((d.Affinity / 100 + 1) * 100).ToString("N0") + "   ·   " + (d.CanSnack ? "지금 간식을 줄 수 있어요" : "지금은 배가 꽉 찼어요");
            snackButton.Enabled = d.CanSnack;
            var names = new[] { new[] { "dance", "몰래 하는 군무" }, new[] { "dizzy", "어질어질" }, new[] { "power", "기 폭발" } };
            secretLabel.Text = "발견한 숨은 반응 " + names.Count(p => d.Discoveries.Contains(p[0])) + " / 3\n" + string.Join("  ·  ", names.Where(p => d.Discoveries.Contains(p[0])).Select(p => p[1]).ToArray());
            RefreshRoster();
        }

        // ───────── 저장 ─────────
        private static string Norm(string v) { TimeSpan t = Settings.ParseTime(v).Value; return string.Format("{0:00}:{1:00}", t.Hours, t.Minutes); }
        private void Warn(string m) { MessageBox.Show(this, m, "수퍼히어로", MessageBoxButtons.OK, MessageBoxIcon.Asterisk); }

        private bool Apply()
        {
            if (!Settings.ParseTime(tLunch.Text).HasValue) { nav.SelectedIndex = 4; Warn("점심 시간을 12:00처럼 적어 주세요."); tLunch.Focus(); return false; }
            if (!Settings.ParseTime(tLeave.Text).HasValue) { nav.SelectedIndex = 4; Warn("퇴근 시간을 18:00처럼 적어 주세요."); tLeave.Focus(); return false; }
            try { StartupOption.SetEnabled(cStartup.Checked); }
            catch (Exception ex) { nav.SelectedIndex = 5; Warn("자동 시작 설정을 저장하지 못했어요.\n" + ex.Message); return false; }

            var o = PlayOptions.Current; int oldSize = o.SizePercent;
            o.Walls = cWalls.Checked; o.Ceiling = cCeiling.Checked; o.Windows = cWindows.Checked; o.Fullscreen = cFull.Checked;
            o.Idle = cIdle.Checked; o.ShortRest = cShortRest.Checked; o.Focus = cFocus.Checked; o.WorkFocus = cWork.Checked; o.PositionFixed = cPinned.Checked;
            o.Speed = (double)nSpeed.Value; o.FallSpeed = (double)nFall.Value; o.SlideSpeed = (double)nSlide.Value;
            o.ClickMode = (ClickReaction)clickMode.SelectedIndex;
            o.Escape = rEscape.Checked; o.Follow = rFollow.Checked; o.EscapeDelay = (double)nDelay.Value; o.Secrets = cSecrets.Checked;
            o.HungerMinutes = (int)nHunger.Value; o.HungerCry = cHungerCry.Checked; o.ShowAffinity = cAffinity.Checked;
            o.ClarkKent = costume.SelectedIndex == 1;
            o.SigFreq = sigFreq.SelectedIndex; o.SizePercent = (int)nSize.Value; o.PetOpacityPercent = (int)nOpacity.Value;
            o.MemoTopMost = cMemoTop.Checked; o.MemoOpacityPercent = (int)nMemoOpacity.Value;

            Settings s = app.Settings;
            s.HourlyOn = cHourly.Checked; s.HourlyWindow = cPopup.Checked; s.AlertPet = cAlertPet.Checked; s.EdgeGlow = cGlow.Checked;
            s.EdgeGlowTheme = glowTheme.SelectedIndex; s.EdgeGlowWidth = glowWidth.SelectedIndex; s.EdgeGlowStrength = glowStrength.SelectedIndex;
            s.QuietWeekdays = cQuiet.Checked; s.QuietStart = (int)nStart.Value; s.QuietEnd = (int)nEnd.Value;
            s.LunchOn = cLunch.Checked; s.LunchTime = Norm(tLunch.Text); s.LunchMsg = tLunchMsg.Text.Trim();
            s.LeaveOn = cLeave.Checked; s.LeaveTime = Norm(tLeave.Text); s.LeaveMsg = tLeaveMsg.Text.Trim();
            s.NapOn = cNap.Checked; s.NapMinutes = (int)nNap.Value;
            if (s.LunchMsg.Length == 0) s.LunchMsg = "점심 먹으러 가자!";
            if (s.LeaveMsg.Length == 0) s.LeaveMsg = "퇴근이다! 오늘도 수고했어";

            var chat = ChatOptions.Current;
            chat.Enabled = cChat.Checked; chat.Model = tChatModel.Text.Trim().Length > 0 ? tChatModel.Text.Trim() : "gemma4:e2b";
            chat.Endpoint = tChatUrl.Text.Trim().Length > 0 ? tChatUrl.Text.Trim().TrimEnd('/') : "http://localhost:11434";
            chat.Mode = Math.Max(0, cMode.SelectedIndex); chat.Hotkey = cChatKey.Checked; chat.HotkeyMods = hkBox.Mods; chat.HotkeyKey = hkBox.Key; chat.Sound = cChatSound.Checked; chat.Proactive = cChatProactive.Checked; chat.ProactiveMinutes = (int)nProactive.Value; chat.Memory = cChatMemory.Checked; chat.Chips = cChatChips.Checked; chat.Diary = cChatDiary.Checked;

            app.OnSettingsChanged();
            app.ApplyCharacterSettings(); app.ApplyHelperOptions();
            app.Chat.ApplyOptions(); app.Chat.ModeChanged();
            if (chat.Enabled && chat.Hotkey && !app.Chat.HotkeyRegistered) Warn("단축키 " + ChatHotkey.Text(chat.HotkeyMods, chat.HotkeyKey) + " 는 다른 프로그램이 이미 쓰고 있어서 등록하지 못했어요. 다른 조합을 골라 주세요.");
            if (app.Notes != null) { app.Notes.DefaultColor = NoteColors.All[Math.Max(0, noteColor.SelectedIndex)]; app.Notes.DefaultSize = (NoteSizePreset)Math.Max(0, noteSize.SelectedIndex); }
            if (o.SizePercent != oldSize) app.RebuildForSize();
            bool ok = s.Save() & o.Save() & chat.Save();
            status.Text = ok ? "저장했어요." : "설정 파일을 저장하지 못했어요. 이번에 켜 있는 동안만 적용돼요.";
            if (!ok) Warn(status.Text);
            return true;
        }
    }
}
