using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SuperHeroes
{
    /// <summary>우클릭·트레이 메뉴의 섹션 종류. 각각 아이콘과 강조색이 있다.</summary>
    internal enum MenuKind { Status, Social, Work, Settings }

    /// <summary>
    /// 메뉴의 섹션 머리글(누를 수 없는 줄). 왼쪽 바에 동그란 아이콘 배지가 붙고, 오른쪽에 제목이 놓인다.
    /// Status 는 맨 위 친밀도 줄로, 제목 대신 친밀도 문구를 보여 준다.
    /// </summary>
    internal sealed class MenuSection : ToolStripMenuItem
    {
        internal readonly MenuKind Kind;
        internal MenuSection(MenuKind kind, string text) { Kind = kind; Text = text; AutoSize = true; Margin = Padding.Empty; Padding = Padding.Empty; ImageScaling = ToolStripItemImageScaling.None; Image = BarSlot(); }
        /// <summary>왼쪽 바 폭을 정하는 투명 그림(보통 항목은 그림이 없어 바가 좁게 잡힌다). 쓰고 나서도 그대로 둔다(작은 비트맵 하나).</summary>
        private static Bitmap slot;
        private static Bitmap BarSlot() { if (slot == null) { int w = (int)Math.Round(22 * Math.Max(1f, Native.SystemDpiScale())); slot = new Bitmap(w, w); } return slot; }

        public override bool CanSelect { get { return false; } }
        private float Scale { get { return Owner != null ? Owner.DeviceDpi / 96f : 1f; } }
        internal bool First { get { return Owner != null && Owner.Items.Count > 0 && Owner.Items[0] == this; } }
        /// <summary>머리글 줄 위쪽의 여백(첫 줄은 없음). 그 아래 RowHeight 만큼이 실제 줄이다.</summary>
        internal int Gap { get { return First ? (int)Math.Round(3 * Scale) : (int)Math.Round(9 * Scale); } }
        /// <summary>친밀도 줄만 실제 줄이 있다. 교류·일·설정 머리글은 구분 여백(Gap)만 차지하고, 배지는 섹션 첫 항목 옆 왼쪽 바에 그린다.</summary>
        internal int RowHeight { get { return Kind == MenuKind.Status ? (int)Math.Round(24 * Scale) : 0; } }

        public override Size GetPreferredSize(Size constrainingSize)
        {
            Size t = TextRenderer.MeasureText(Text ?? "", Font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
            return new Size(t.Width + (int)Math.Round(46 * Scale), Gap + RowHeight);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var r = Owner != null ? Owner.Renderer as HeroMenuRenderer : null;
            if (r == null) return;
            r.PaintSection(e.Graphics, this);
        }
    }

    /// <summary>
    /// 메뉴 모양: 흰 바탕 · 얇은 테두리 · 둥근 선택 막대 · 왼쪽 바(섹션마다 가는 강조 줄과 아이콘 배지).
    /// 빨강 · 민트 · 노랑 · 파랑을 배지 색으로 쓴 일반 UI 이며, 캐릭터 그림은 건드리지 않는다.
    /// </summary>
    internal sealed class HeroMenuRenderer : ToolStripProfessionalRenderer
    {
        // 메뉴 바탕·글자 색 (캐릭터 그림이 아니라 일반 UI 색)                                                    //ui
        private static readonly Color Back = Color.White, BarBack = Color.FromArgb(238, 243, 250), Line = Color.FromArgb(222, 228, 238);   //ui
        private static readonly Color Border = Color.FromArgb(198, 206, 220), Hover = Color.FromArgb(228, 238, 252), Muted = Color.FromArgb(104, 116, 138);   //ui
        // 섹션 강조색(배지 · 왼쪽 줄 · 체크 표시)과 노랑 배지 위의 검정 그림
        private static readonly Color BgRed = Color.FromArgb(200, 30, 45), BgMint = Color.FromArgb(56, 186, 156), BgYellow = Color.FromArgb(254, 193, 11), BgBlue = Color.FromArgb(30, 77, 164), Black = Color.FromArgb(33, 29, 31);   //ui

        // 메뉴(하위 메뉴 포함)마다 왼쪽 바 오른쪽 끝 위치가 다를 수 있어 따로 기억한다
        private readonly System.Runtime.CompilerServices.ConditionalWeakTable<ToolStrip, object> margins = new System.Runtime.CompilerServices.ConditionalWeakTable<ToolStrip, object>();
        private int MarginOf(ToolStrip t, float s) { object v; return t != null && margins.TryGetValue(t, out v) ? (int)v : (int)Math.Round(32 * s); }

        internal HeroMenuRenderer() : base(new ProfessionalColorTable { UseSystemColors = false }) { RoundedEdges = false; }

        internal static Color Accent(MenuKind k)
        {
            switch (k)
            {
                case MenuKind.Status: return BgRed;      
                case MenuKind.Social: return BgMint;     
                case MenuKind.Work: return BgYellow;     
                default: return BgBlue;                  
            }
        }

        /// <summary>메뉴 하나에 이 렌더러를 붙인다. 아이콘 칸을 넉넉히 잡도록 이미지 크기를 키운다.</summary>
        internal static void Apply(ContextMenuStrip menu)
        {
            menu.Renderer = new HeroMenuRenderer();
            menu.ShowImageMargin = true;
            menu.ImageScalingSize = new Size((int)Math.Round(18 * menu.DeviceDpi / 96f), (int)Math.Round(18 * menu.DeviceDpi / 96f));
        }

        // ───────── 바탕 · 테두리 · 구분선 ─────────
        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            using (var b = new SolidBrush(Back)) e.Graphics.FillRectangle(b, e.AffectedBounds);
            // 섹션 사이 윗선: 항목 폭은 글자 폭만큼이라 잘리므로, 메뉴 전체 폭으로 여기서 긋는다 (왼쪽 바는 이어서 바가 덮는다)
            float s = Scale(e.ToolStrip); int x0 = MarginOf(e.ToolStrip, s) + (int)(6 * s), x1 = e.ToolStrip.Width - (int)(8 * s);
            using (var p = new Pen(Line)) foreach (ToolStripItem it in e.ToolStrip.Items) { var sec = it as MenuSection; if (sec != null && !sec.First) { int y = sec.Bounds.Top + sec.Gap - (int)(4 * s); e.Graphics.DrawLine(p, x0, y, x1, y); } }
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            using (var p = new Pen(Border)) e.Graphics.DrawRectangle(p, 0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            var g = e.Graphics; int y = e.Item.Height / 2;
            using (var p = new Pen(Line)) g.DrawLine(p, MarginOf(e.ToolStrip, Scale(e.ToolStrip)) - e.Item.Bounds.Left + (int)(6 * Scale(e.ToolStrip)), y, e.Item.Width - (int)(8 * Scale(e.ToolStrip)), y);
        }

        // ───────── 왼쪽 바 ─────────
        protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
        {
            var g = e.Graphics; Rectangle m = e.AffectedBounds; float s = Scale(e.ToolStrip);
            margins.Remove(e.ToolStrip); margins.Add(e.ToolStrip, m.Right);
            using (var b = new SolidBrush(BarBack)) g.FillRectangle(b, m);
            using (var p = new Pen(Line)) g.DrawLine(p, m.Right - 1, m.Top, m.Right - 1, m.Bottom);

            // 섹션마다 왼쪽 가장자리에 가는 강조 줄(머리글 줄부터 다음 섹션 직전까지)
            ToolStripItemCollection items = e.ToolStrip.Items;
            for (int i = 0; i < items.Count; i++)
            {
                var sec = items[i] as MenuSection; if (sec == null) continue;
                int top = sec.Bounds.Top + sec.Gap + 2, bottom = m.Bottom - 4;
                for (int j = i + 1; j < items.Count; j++) if (items[j] is MenuSection) { bottom = items[j].Bounds.Top + ((MenuSection)items[j]).Gap - 3; break; }
                if (sec.Kind == MenuKind.Status) bottom = Math.Min(bottom, sec.Bounds.Bottom - 2);   // 친밀도 줄은 줄 하나만
                using (var b = new SolidBrush(Accent(sec.Kind))) g.FillRectangle(b, m.Left, top, Math.Max(2, (int)Math.Round(3 * s)), Math.Max(2, bottom - top));
                if (sec.Kind != MenuKind.Status && i + 1 < items.Count) { g.SmoothingMode = SmoothingMode.AntiAlias; int d = (int)Math.Round(20 * s); Rectangle row = items[i + 1].Bounds; DrawBadge(g, new Rectangle(m.Left + (m.Width - d) / 2 + 1, row.Top + (row.Height - d) / 2, d, d), sec.Kind); }   // 배지는 섹션 첫 항목 옆
            }
        }

        /// <summary>머리글 줄: 왼쪽 바에 아이콘 배지, 오른쪽에 제목(또는 친밀도 문구)과 윗선.</summary>
        internal void PaintSection(Graphics g, MenuSection sec)
        {
            float s = Scale(sec.Owner); int mr = MarginOf(sec.Owner, s) - sec.Bounds.Left;
            int rowTop = sec.Gap, rowH = sec.RowHeight, right = sec.Owner.Width - sec.Bounds.Left - 1;   // 항목 폭이 아니라 메뉴 폭 기준
            g.SmoothingMode = SmoothingMode.AntiAlias; g.PixelOffsetMode = PixelOffsetMode.HighQuality; g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;


            // 배지: 왼쪽 바 한가운데(바 폭은 mr)
            int d = (int)Math.Round(20 * s);
            var badge = new Rectangle((mr - d) / 2 + 1, rowTop + (rowH - d) / 2, d, d);
            if (sec.Kind != MenuKind.Status) return;   // 교류·일·설정: 줄 없이 여백만, 배지는 OnRenderImageMargin 에서
            DrawBadge(g, badge, sec.Kind);

            bool status = sec.Kind == MenuKind.Status;
            if (!status) return;   // 교류·일·설정은 제목 글자 없이 아이콘 배지만 보여 준다 (사용자 요청)
            using (var f = new Font(sec.Font.FontFamily, sec.Font.Size * (status ? 1f : .95f), status ? FontStyle.Regular : FontStyle.Bold))
            {
                Color c = status ? Color.FromArgb(60, 48, 52) : Muted;
                var tr = new Rectangle(mr + (int)(8 * s), rowTop, right - mr - (int)(16 * s), rowH);
                TextRenderer.DrawText(g, sec.Text, f, tr, c, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
            }
        }

        // ───────── 선택 · 체크 ─────────
        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (!e.Item.Selected || !e.Item.Enabled) return;
            float s = Scale(e.ToolStrip); var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            int mr = MarginOf(e.ToolStrip, s) - e.Item.Bounds.Left;
            var r = new Rectangle(mr + (int)(3 * s), 1, e.Item.Width - mr - (int)(7 * s), e.Item.Height - 2);
            using (var path = RoundRect(r, (int)(5 * s))) using (var b = new SolidBrush(Hover)) g.FillPath(b, path);
        }

        protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
        {
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; float s = Scale(e.ToolStrip);
            int d = (int)Math.Round(16 * s);
            var box = new Rectangle(e.ImageRectangle.Left + (e.ImageRectangle.Width - d) / 2, e.ImageRectangle.Top + (e.ImageRectangle.Height - d) / 2, d, d);
            using (var path = RoundRect(box, (int)(4 * s))) using (var b = new SolidBrush(BgBlue)) g.FillPath(b, path);   
            using (var p = new Pen(Color.White, Math.Max(1.6f, 1.8f * s)) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
                g.DrawLines(p, new[] { new PointF(box.Left + d * .26f, box.Top + d * .54f), new PointF(box.Left + d * .43f, box.Top + d * .71f), new PointF(box.Left + d * .76f, box.Top + d * .30f) });
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            if (e.Item is MenuSection) return;
            if (e.Item.Enabled) e.TextColor = Color.FromArgb(40, 36, 42);   //ui
            else e.TextColor = Color.FromArgb(150, 156, 170);               //ui
            base.OnRenderItemText(e);
        }

        // ───────── 아이콘 배지 (동그라미 + 흰/검은 선 그림) ─────────
        private static void DrawBadge(Graphics g, Rectangle r, MenuKind kind)
        {
            Color ink = kind == MenuKind.Work ? Black : Color.White;   // 노랑 바탕에는 검정 그림
            using (var b = new SolidBrush(Accent(kind))) g.FillEllipse(b, r);
            float u = r.Width / 20f; float cx = r.Left + r.Width / 2f, cy = r.Top + r.Height / 2f;
            using (var p = new Pen(ink, Math.Max(1.4f, 1.5f * u)) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
            using (var fill = new SolidBrush(ink))
            {
                switch (kind)
                {
                    case MenuKind.Status:   // 하트
                        using (var path = new GraphicsPath())
                        {
                            float w = 11f * u, h = 10f * u, x = cx - w / 2, y = cy - h / 2 + .5f * u;
                            path.AddBezier(cx, y + h, x - .5f * u, y + h * .55f, x - .5f * u, y - h * .05f, x + w * .27f, y);
                            path.AddBezier(x + w * .27f, y, cx - .2f * u, y - h * .05f, cx, y + h * .22f, cx, y + h * .28f);
                            path.AddBezier(cx, y + h * .28f, cx, y + h * .22f, cx + .2f * u, y - h * .05f, x + w * .73f, y);
                            path.AddBezier(x + w * .73f, y, x + w + .5f * u, y - h * .05f, x + w + .5f * u, y + h * .55f, cx, y + h);
                            g.FillPath(fill, path);
                        }
                        break;
                    case MenuKind.Social:   // 말풍선
                        {
                            var bub = new RectangleF(cx - 5.5f * u, cy - 4.8f * u, 11f * u, 7.6f * u);
                            using (var path = new GraphicsPath())
                            {
                                float rad = 2.6f * u;
                                path.AddArc(bub.Left, bub.Top, rad * 2, rad * 2, 180, 90); path.AddArc(bub.Right - rad * 2, bub.Top, rad * 2, rad * 2, 270, 90);
                                path.AddArc(bub.Right - rad * 2, bub.Bottom - rad * 2, rad * 2, rad * 2, 0, 90);
                                path.AddLine(cx + 1.5f * u, bub.Bottom, cx - 2.5f * u, bub.Bottom + 3f * u); path.AddLine(cx - 2.5f * u, bub.Bottom + 3f * u, cx - 2.2f * u, bub.Bottom);
                                path.AddArc(bub.Left, bub.Bottom - rad * 2, rad * 2, rad * 2, 90, 90); path.CloseFigure();
                                g.FillPath(fill, path);
                            }
                            using (var dot = new SolidBrush(Accent(kind))) for (int i = -1; i <= 1; i++) g.FillEllipse(dot, cx + i * 3f * u - .85f * u, cy - 1.8f * u - .85f * u + .9f * u, 1.7f * u, 1.7f * u);
                        }
                        break;
                    case MenuKind.Work:     // 서류 가방
                        {
                            var body = new RectangleF(cx - 5.5f * u, cy - 2.6f * u, 11f * u, 7.6f * u);
                            using (var path = RoundRectF(body, 1.6f * u)) g.FillPath(fill, path);
                            using (var hp = new Pen(ink, Math.Max(1.4f, 1.5f * u)) { LineJoin = LineJoin.Round }) g.DrawLines(hp, new[] { new PointF(cx - 2.2f * u, cy - 2.6f * u), new PointF(cx - 2.2f * u, cy - 4.6f * u), new PointF(cx + 2.2f * u, cy - 4.6f * u), new PointF(cx + 2.2f * u, cy - 2.6f * u) });
                            using (var bp = new Pen(Accent(kind), Math.Max(1f, 1.1f * u))) g.DrawLine(bp, cx - 5.5f * u, cy + .9f * u, cx + 5.5f * u, cy + .9f * u);
                            using (var kb = new SolidBrush(Accent(kind))) g.FillRectangle(kb, cx - 1f * u, cy + .1f * u, 2f * u, 1.8f * u);
                        }
                        break;
                    default:                // 톱니바퀴
                        {
                            const int teeth = 8; float ro = 6.4f * u, ri = 4.7f * u;
                            using (var path = new GraphicsPath())
                            {
                                var pts = new PointF[teeth * 4];
                                for (int i = 0; i < teeth; i++)
                                {
                                    double a = i * 2 * Math.PI / teeth, w = Math.PI / teeth * .42, v = Math.PI / teeth * .24;
                                    pts[i * 4] = new PointF(cx + (float)(ri * Math.Cos(a - w)), cy + (float)(ri * Math.Sin(a - w)));
                                    pts[i * 4 + 1] = new PointF(cx + (float)(ro * Math.Cos(a - v)), cy + (float)(ro * Math.Sin(a - v)));
                                    pts[i * 4 + 2] = new PointF(cx + (float)(ro * Math.Cos(a + v)), cy + (float)(ro * Math.Sin(a + v)));
                                    pts[i * 4 + 3] = new PointF(cx + (float)(ri * Math.Cos(a + w)), cy + (float)(ri * Math.Sin(a + w)));
                                }
                                path.AddPolygon(pts); g.FillPath(fill, path);
                            }
                            using (var hole = new SolidBrush(Accent(kind))) g.FillEllipse(hole, cx - 2.3f * u, cy - 2.3f * u, 4.6f * u, 4.6f * u);
                        }
                        break;
                }
            }
        }

        private static float Scale(ToolStrip t) { return t != null ? t.DeviceDpi / 96f : 1f; }
        private static GraphicsPath RoundRect(Rectangle r, int rad) { return RoundRectF(r, rad); }
        private static GraphicsPath RoundRectF(RectangleF r, float rad)
        {
            var p = new GraphicsPath(); float d = Math.Max(1f, rad * 2);
            p.AddArc(r.Left, r.Top, d, d, 180, 90); p.AddArc(r.Right - d, r.Top, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); p.AddArc(r.Left, r.Bottom - d, d, d, 90, 90); p.CloseFigure();
            return p;
        }
    }
}
