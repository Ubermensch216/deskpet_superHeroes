using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SuperHeroes
{
    /// <summary>
    /// 설정창 메모 쪽에서 쓰는 납작한(flat) 벡터 아이콘. 아이콘 글꼴에 기대지 않고 GDI+ 로 직접 그려서 어느 PC 에서나 같게 보인다.
    /// 모든 그림은 받은 사각형 안에서 비율(0~1)로 그리므로 크기와 DPI 에 상관없이 또렷하다. 설정창 전용 색이라 캐릭터 가이드북 색 규정과는 무관하다.
    /// </summary>
    internal static partial class FlatIcon
    {
        internal delegate void Painter(Graphics g, RectangleF r, Color ink);

        private static readonly Color Danger = Color.FromArgb(206, 74, 74);

        private static float X(RectangleF r, float t) { return r.X + r.Width * t; }
        private static float Y(RectangleF r, float t) { return r.Y + r.Height * t; }
        private static PointF Pt(RectangleF r, float tx, float ty) { return new PointF(X(r, tx), Y(r, ty)); }
        private static RectangleF Box(RectangleF r, float x0, float y0, float x1, float y1) { return new RectangleF(X(r, x0), Y(r, y0), r.Width * (x1 - x0), r.Height * (y1 - y0)); }
        private static Pen Line(RectangleF r, Color c, float k = .09f) { return new Pen(c, Math.Max(1.4f, Math.Min(r.Width, r.Height) * k)) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round }; }

        private static GraphicsPath Round(RectangleF b, float radius)
        {
            float d = Math.Max(1f, Math.Min(radius * 2, Math.Min(b.Width, b.Height)));
            var p = new GraphicsPath();
            p.AddArc(b.Left, b.Top, d, d, 180, 90); p.AddArc(b.Right - d, b.Top, d, d, 270, 90);
            p.AddArc(b.Right - d, b.Bottom - d, d, d, 0, 90); p.AddArc(b.Left, b.Bottom - d, d, d, 90, 90);
            p.CloseFigure(); return p;
        }

        /// <summary>그림 하나를 작은 비트맵으로 구워 단추 · 라벨 이미지로 쓴다.</summary>
        internal static Bitmap Bitmap(Painter paint, int px, Color ink)
        {
            var bmp = new Bitmap(px, px);
            using (var g = Graphics.FromImage(bmp)) { g.SmoothingMode = SmoothingMode.AntiAlias; paint(g, new RectangleF(0, 0, px, px), ink); }
            return bmp;
        }

        // ───────── 메모 모양 ─────────
        /// <summary>접힌 모서리가 있는 메모지. 색 키를 주면 실제 메모의 바탕 · 테두리 · 접힘 색으로 칠한다.</summary>
        internal static void NoteShape(Graphics g, RectangleF r, string colorKey)
        {
            Color fill = NoteColors.Background(colorKey), edge = NoteColors.Border(colorKey), fold = NoteColors.Swatch(colorKey);
            float w = r.Width, h = r.Height, f = Math.Min(w, h) * .3f;
            float x0 = r.X + .5f, y0 = r.Y + .5f, x1 = r.Right - .5f, y1 = r.Bottom - .5f;
            using (var body = new GraphicsPath())
            {
                body.AddPolygon(new[] { new PointF(x0, y0), new PointF(x1 - f, y0), new PointF(x1, y0 + f), new PointF(x1, y1), new PointF(x0, y1) });
                using (var br = new SolidBrush(fill)) g.FillPath(br, body);
                using (var pen = new Pen(edge, 1.2f) { LineJoin = LineJoin.Round }) g.DrawPath(pen, body);
            }
            using (var br = new SolidBrush(fold)) g.FillPolygon(br, new[] { new PointF(x1 - f, y0), new PointF(x1, y0 + f), new PointF(x1 - f, y0 + f) });
            using (var pen = new Pen(Color.FromArgb(120, fold), Math.Max(1.2f, h * .06f)) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                float lx = x0 + w * .18f;
                g.DrawLine(pen, lx, y0 + h * .46f, x1 - w * .2f, y0 + h * .46f);
                g.DrawLine(pen, lx, y0 + h * .64f, x1 - w * .2f, y0 + h * .64f);
                g.DrawLine(pen, lx, y0 + h * .82f, x0 + w * .55f, y0 + h * .82f);
            }
        }

        private static void NoteIcon(Graphics g, RectangleF r, Color ink) { NoteShape(g, Box(r, .14f, .06f, .86f, .94f), NoteColors.Yellow); }

        // ───────── 새 메모의 기본값 ─────────
        internal static void Palette(Graphics g, RectangleF r, Color ink)
        {
            using (var path = new GraphicsPath())
            {
                path.AddEllipse(Box(r, .06f, .12f, .94f, .90f));
                using (var br = new SolidBrush(Color.FromArgb(236, 242, 255))) g.FillPath(br, path);
                using (var pen = Line(r, ink, .07f)) g.DrawPath(pen, path);
            }
            using (var br = new SolidBrush(Color.FromArgb(248, 250, 255))) g.FillEllipse(br, Box(r, .60f, .56f, .84f, .80f));
            using (var pen = Line(r, ink, .07f)) g.DrawEllipse(pen, Box(r, .60f, .56f, .84f, .80f));
            Color[] dots = { Color.FromArgb(236, 98, 98), FriendUI.Gold, Color.FromArgb(76, 175, 120), FriendUI.Blue };
            PointF[] at = { Pt(r, .28f, .42f), Pt(r, .46f, .28f), Pt(r, .68f, .28f), Pt(r, .26f, .64f) };
            for (int i = 0; i < dots.Length; i++) using (var br = new SolidBrush(dots[i])) g.FillEllipse(br, at[i].X - r.Width * .07f, at[i].Y - r.Width * .07f, r.Width * .14f, r.Width * .14f);
        }

        internal static void Resize(Graphics g, RectangleF r, Color ink)
        {
            using (var path = Round(Box(r, .08f, .08f, .92f, .92f), r.Width * .1f)) using (var pen = Line(r, Color.FromArgb(150, ink), .07f)) { pen.DashStyle = DashStyle.Dash; g.DrawPath(pen, path); }
            using (var path = Round(Box(r, .08f, .50f, .50f, .92f), r.Width * .08f))
            {
                using (var br = new SolidBrush(Color.FromArgb(236, 242, 255))) g.FillPath(br, path);
                using (var pen = Line(r, ink, .08f)) g.DrawPath(pen, path);
            }
            using (var pen = Line(r, FriendUI.Blue, .09f))
            {
                g.DrawLine(pen, Pt(r, .46f, .54f), Pt(r, .80f, .20f));
                g.DrawLines(pen, new[] { Pt(r, .58f, .20f), Pt(r, .80f, .20f), Pt(r, .80f, .42f) });
            }
        }

        // ───────── 어디서나 쓰는 단축키 ─────────
        internal static void NewNote(Graphics g, RectangleF r, Color ink)
        {
            NoteShape(g, Box(r, .10f, .04f, .74f, .84f), NoteColors.Yellow);
            var b = Box(r, .50f, .50f, .96f, .96f);
            using (var br = new SolidBrush(FriendUI.Blue)) g.FillEllipse(br, b);
            using (var pen = Line(r, Color.White, .08f)) { g.DrawLine(pen, Pt(r, .73f, .60f), Pt(r, .73f, .86f)); g.DrawLine(pen, Pt(r, .60f, .73f), Pt(r, .86f, .73f)); }
        }

        internal static void Library(Graphics g, RectangleF r, Color ink)
        {
            string[] keys = { NoteColors.Yellow, NoteColors.Blue, NoteColors.Green, NoteColors.Pink };
            for (int i = 0; i < 4; i++)
            {
                float cx = i % 2 == 0 ? .06f : .54f, cy = i < 2 ? .06f : .54f;
                NoteShape(g, Box(r, cx, cy, cx + .40f, cy + .40f), keys[i]);
            }
        }

        internal static void Pin(Graphics g, RectangleF r, Color ink)
        {
            var st = g.Save();
            g.TranslateTransform(X(r, .5f), Y(r, .5f)); g.RotateTransform(35);
            float w = r.Width, h = r.Height;
            using (var br = new SolidBrush(ink))
            {
                using (var p = Round(new RectangleF(-w * .13f, -h * .42f, w * .26f, h * .34f), w * .05f)) g.FillPath(br, p);
                using (var p = Round(new RectangleF(-w * .24f, -h * .10f, w * .48f, h * .09f), w * .04f)) g.FillPath(br, p);
            }
            using (var pen = new Pen(ink, Math.Max(1.4f, w * .07f)) { StartCap = LineCap.Round, EndCap = LineCap.Round }) g.DrawLine(pen, 0, -h * .02f, 0, h * .44f);
            g.Restore(st);
        }

        /// <summary>종 + 작은 시계: "1시간 뒤 알림"</summary>
        internal static void BellClock(Graphics g, RectangleF r, Color ink)
        {
            using (var bell = new GraphicsPath())
            {
                bell.AddArc(Box(r, .16f, .10f, .62f, .56f), 180, 180);
                bell.AddLine(Pt(r, .62f, .34f), Pt(r, .70f, .66f)); bell.AddLine(Pt(r, .70f, .66f), Pt(r, .08f, .66f)); bell.AddLine(Pt(r, .08f, .66f), Pt(r, .16f, .34f));
                bell.CloseFigure();
                using (var br = new SolidBrush(ink)) { g.FillPath(br, bell); g.FillEllipse(br, Box(r, .27f, .70f, .47f, .86f)); g.FillEllipse(br, Box(r, .31f, .03f, .47f, .15f)); }
            }
            var c = Box(r, .52f, .50f, .98f, .96f);
            using (var br = new SolidBrush(FriendUI.Blue)) g.FillEllipse(br, c);
            using (var pen = Line(r, Color.White, .06f))
            {
                g.DrawLine(pen, Pt(r, .75f, .73f), Pt(r, .75f, .59f)); g.DrawLine(pen, Pt(r, .75f, .73f), Pt(r, .85f, .79f));
            }
        }

        // ───────── 메모를 쓰는 중 ─────────
        private static void Letter(Graphics g, RectangleF r, Color ink, string s, FontStyle style, float size = .82f)
        {
            using (var f = new Font("Segoe UI", r.Height * size, style, GraphicsUnit.Pixel))
            using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap })
            using (var br = new SolidBrush(ink))
            {
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                g.DrawString(s, f, br, new RectangleF(r.X, r.Y + r.Height * .04f, r.Width, r.Height), sf);
            }
        }
        internal static void Bold(Graphics g, RectangleF r, Color ink) { Letter(g, r, ink, "B", FontStyle.Bold); }
        internal static void Italic(Graphics g, RectangleF r, Color ink) { Letter(g, r, ink, "I", FontStyle.Italic); }
        internal static void Underline(Graphics g, RectangleF r, Color ink) { Letter(g, r, ink, "U", FontStyle.Underline); }
        internal static void Strike(Graphics g, RectangleF r, Color ink) { Letter(g, r, ink, "S", FontStyle.Strikeout); }

        internal static void Heading(Graphics g, RectangleF r, Color ink)
        {
            Letter(g, Box(r, 0f, 0f, .74f, 1f), ink, "H", FontStyle.Bold, .8f);
            Letter(g, Box(r, .58f, .34f, 1f, 1f), FriendUI.Blue, "1", FontStyle.Bold, .62f);
        }

        internal static void Link(Graphics g, RectangleF r, Color ink)
        {
            var st = g.Save();
            g.TranslateTransform(X(r, .5f), Y(r, .5f)); g.RotateTransform(-45);
            float w = r.Width, h = r.Height;
            using (var pen = Line(r, ink, .09f))
            {
                using (var p = Round(new RectangleF(-w * .44f, -h * .14f, w * .54f, h * .28f), h * .14f)) g.DrawPath(pen, p);
                using (var p = Round(new RectangleF(-w * .10f, -h * .14f, w * .54f, h * .28f), h * .14f)) g.DrawPath(pen, p);
            }
            g.Restore(st);
        }

        internal static void Code(Graphics g, RectangleF r, Color ink)
        {
            using (var pen = Line(r, ink, .1f))
            {
                g.DrawLines(pen, new[] { Pt(r, .30f, .26f), Pt(r, .08f, .50f), Pt(r, .30f, .74f) });
                g.DrawLines(pen, new[] { Pt(r, .70f, .26f), Pt(r, .92f, .50f), Pt(r, .70f, .74f) });
            }
            using (var pen = Line(r, FriendUI.Blue, .1f)) g.DrawLine(pen, Pt(r, .56f, .20f), Pt(r, .44f, .80f));
        }

        internal static void Bullets(Graphics g, RectangleF r, Color ink)
        {
            float[] ys = { .24f, .50f, .76f };
            using (var br = new SolidBrush(FriendUI.Blue)) using (var pen = Line(r, ink, .08f))
                foreach (float y in ys)
                {
                    g.FillEllipse(br, X(r, .10f), Y(r, y) - r.Width * .06f, r.Width * .12f, r.Width * .12f);
                    g.DrawLine(pen, Pt(r, .38f, y), Pt(r, .92f, y));
                }
        }

        private static void CheckBox(Graphics g, RectangleF r, RectangleF b, bool on, Color ink)
        {
            using (var p = Round(b, b.Width * .22f))
            {
                if (on) using (var br = new SolidBrush(FriendUI.Blue)) g.FillPath(br, p);
                else using (var pen = Line(r, ink, .07f)) g.DrawPath(pen, p);
            }
            if (on) using (var pen = new Pen(Color.White, Math.Max(1.4f, b.Width * .16f)) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
                g.DrawLines(pen, new[] { new PointF(b.X + b.Width * .24f, b.Y + b.Height * .52f), new PointF(b.X + b.Width * .43f, b.Y + b.Height * .70f), new PointF(b.X + b.Width * .76f, b.Y + b.Height * .32f) });
        }

        internal static void Checklist(Graphics g, RectangleF r, Color ink)
        {
            float[] ys = { .24f, .50f, .76f };
            for (int i = 0; i < 3; i++)
            {
                CheckBox(g, r, Box(r, .06f, ys[i] - .12f, .30f, ys[i] + .12f), i == 0, ink);
                using (var pen = Line(r, ink, .08f)) g.DrawLine(pen, Pt(r, .42f, ys[i]), Pt(r, .94f, ys[i]));
            }
        }

        /// <summary>빈 상자 → 체크된 상자: 한 번 누를 때마다 뒤집힌다.</summary>
        internal static void ToggleCheck(Graphics g, RectangleF r, Color ink)
        {
            CheckBox(g, r, Box(r, .02f, .30f, .36f, .70f), false, ink);
            using (var pen = Line(r, FriendUI.Blue, .08f)) { g.DrawLine(pen, Pt(r, .44f, .50f), Pt(r, .58f, .50f)); g.DrawLines(pen, new[] { Pt(r, .53f, .42f), Pt(r, .60f, .50f), Pt(r, .53f, .58f) }); }
            CheckBox(g, r, Box(r, .64f, .30f, .98f, .70f), true, ink);
        }

        /// <summary>줄바꿈(↵): 엔터로 목록이 이어진다.</summary>
        internal static void EnterKey(Graphics g, RectangleF r, Color ink)
        {
            using (var pen = Line(r, ink, .1f))
            {
                g.DrawLines(pen, new[] { Pt(r, .86f, .18f), Pt(r, .86f, .58f), Pt(r, .20f, .58f) });
                g.DrawLines(pen, new[] { Pt(r, .38f, .40f), Pt(r, .20f, .58f), Pt(r, .38f, .76f) });
            }
            using (var br = new SolidBrush(FriendUI.Blue)) g.FillEllipse(br, X(r, .08f), Y(r, .18f) - r.Width * .06f, r.Width * .12f, r.Width * .12f);
            using (var pen = Line(r, FriendUI.Blue, .08f)) g.DrawLine(pen, Pt(r, .30f, .18f), Pt(r, .66f, .18f));
        }

        // ───────── 내 메모는 어디에 ─────────
        internal static void Folder(Graphics g, RectangleF r, Color ink)
        {
            using (var back = new GraphicsPath())
            {
                back.AddPolygon(new[] { Pt(r, .06f, .20f), Pt(r, .38f, .20f), Pt(r, .48f, .32f), Pt(r, .94f, .32f), Pt(r, .94f, .86f), Pt(r, .06f, .86f) });
                using (var br = new SolidBrush(FriendUI.Gold)) g.FillPath(br, back);
            }
            using (var p = Round(Box(r, .06f, .40f, .94f, .86f), r.Width * .06f)) using (var br = new SolidBrush(Color.FromArgb(255, 230, 96))) g.FillPath(br, p);
        }

        internal static void Copy(Graphics g, RectangleF r, Color ink)
        {
            using (var pen = Line(r, ink, .08f))
            {
                using (var p = Round(Box(r, .08f, .08f, .58f, .68f), r.Width * .07f)) g.DrawPath(pen, p);
                using (var p = Round(Box(r, .40f, .30f, .92f, .92f), r.Width * .07f)) { using (var br = new SolidBrush(Color.FromArgb(236, 242, 255))) g.FillPath(br, p); g.DrawPath(pen, p); }
            }
        }

        internal static void Trash(Graphics g, RectangleF r, Color ink)
        {
            using (var pen = Line(r, Danger, .09f))
            {
                g.DrawLine(pen, Pt(r, .14f, .24f), Pt(r, .86f, .24f)); g.DrawLines(pen, new[] { Pt(r, .38f, .24f), Pt(r, .38f, .12f), Pt(r, .62f, .12f), Pt(r, .62f, .24f) });
                g.DrawLines(pen, new[] { Pt(r, .22f, .34f), Pt(r, .28f, .88f), Pt(r, .72f, .88f), Pt(r, .78f, .34f) });
                g.DrawLine(pen, Pt(r, .42f, .46f), Pt(r, .42f, .74f)); g.DrawLine(pen, Pt(r, .58f, .46f), Pt(r, .58f, .74f));
            }
        }

        internal static void Info(Graphics g, RectangleF r, Color ink)
        {
            using (var br = new SolidBrush(FriendUI.Blue)) g.FillEllipse(br, Box(r, .06f, .06f, .94f, .94f));
            using (var br = new SolidBrush(Color.White)) { g.FillEllipse(br, X(r, .5f) - r.Width * .06f, Y(r, .26f) - r.Width * .06f, r.Width * .12f, r.Width * .12f); }
            using (var pen = Line(r, Color.White, .11f)) g.DrawLine(pen, Pt(r, .5f, .43f), Pt(r, .5f, .73f));
        }

        internal static void Warning(Graphics g, RectangleF r, Color ink)
        {
            using (var tri = new GraphicsPath())
            {
                tri.AddPolygon(new[] { Pt(r, .5f, .08f), Pt(r, .95f, .88f), Pt(r, .05f, .88f) });
                using (var br = new SolidBrush(FriendUI.Gold)) g.FillPath(br, tri);
                using (var pen = new Pen(Color.FromArgb(214, 160, 0), Math.Max(1.2f, r.Width * .06f)) { LineJoin = LineJoin.Round }) g.DrawPath(pen, tri);
            }
            using (var pen = Line(r, FriendUI.Navy, .1f)) g.DrawLine(pen, Pt(r, .5f, .38f), Pt(r, .5f, .60f));
            using (var br = new SolidBrush(FriendUI.Navy)) g.FillEllipse(br, X(r, .5f) - r.Width * .055f, Y(r, .73f) - r.Width * .055f, r.Width * .11f, r.Width * .11f);
        }

        internal static void NoteGlyph(Graphics g, RectangleF r, Color ink) { NoteIcon(g, r, ink); }

        // ───────── AI 대화 ─────────
        private static readonly Color Green = Color.FromArgb(76, 175, 120), Rose = Color.FromArgb(236, 98, 98), Soft = Color.FromArgb(236, 242, 255);

        private static void Dot(Graphics g, RectangleF r, float cx, float cy, float rad, Color c)
        { using (var br = new SolidBrush(c)) g.FillEllipse(br, X(r, cx) - r.Width * rad, Y(r, cy) - r.Width * rad, r.Width * rad * 2, r.Width * rad * 2); }

        private static GraphicsPath Star(RectangleF r, float cx, float cy, float outer, float inner)
        {
            var pts = new PointF[8];
            for (int i = 0; i < 8; i++)
            {
                double a = -Math.PI / 2 + i * Math.PI / 4; float k = (i % 2 == 0 ? outer : inner) * r.Width;
                pts[i] = new PointF(X(r, cx) + (float)Math.Cos(a) * k, Y(r, cy) + (float)Math.Sin(a) * k);
            }
            var p = new GraphicsPath(); p.AddPolygon(pts); return p;
        }

        /// <summary>말풍선 몸통(둥근 사각형) + 아래 왼쪽 꼬리</summary>
        private static GraphicsPath Bubble(RectangleF r, float x0, float y0, float x1, float y1, float rad, float tx)
        {
            var b = Box(r, x0, y0, x1, y1); float d = r.Width * rad * 2;
            var p = new GraphicsPath();
            p.AddArc(b.Left, b.Top, d, d, 180, 90); p.AddArc(b.Right - d, b.Top, d, d, 270, 90); p.AddArc(b.Right - d, b.Bottom - d, d, d, 0, 90);
            p.AddLines(new[] { Pt(r, tx + .14f, y1), Pt(r, tx - .08f, y1 + .2f), Pt(r, tx, y1) });
            p.AddArc(b.Left, b.Bottom - d, d, d, 90, 90); p.CloseFigure(); return p;
        }

        private static void HeartShape(Graphics g, RectangleF b, Color fill)
        {
            using (var p = new GraphicsPath())
            {
                float x = b.X, y = b.Y, w = b.Width, h = b.Height;
                p.AddBezier(x + w * .5f, y + h, x - w * .1f, y + h * .45f, x + w * .2f, y - h * .1f, x + w * .5f, y + h * .28f);
                p.AddBezier(x + w * .5f, y + h * .28f, x + w * .8f, y - h * .1f, x + w * 1.1f, y + h * .45f, x + w * .5f, y + h);
                p.CloseFigure();
                using (var br = new SolidBrush(fill)) g.FillPath(br, p);
            }
        }

        private static void FaceDisc(Graphics g, RectangleF r, Color ink)
        {
            using (var br = new SolidBrush(FriendUI.Gold)) g.FillEllipse(br, Box(r, .06f, .06f, .94f, .94f));
            using (var pen = Line(r, ink, .07f)) g.DrawEllipse(pen, Box(r, .06f, .06f, .94f, .94f));
        }

        // 연결
        internal static void Chat(Graphics g, RectangleF r, Color ink)
        {
            using (var p = Bubble(r, .06f, .12f, .94f, .70f, .14f, .30f))
            {
                using (var br = new SolidBrush(Soft)) g.FillPath(br, p);
                using (var pen = Line(r, ink, .07f)) g.DrawPath(pen, p);
            }
            Dot(g, r, .30f, .41f, .06f, FriendUI.Blue); Dot(g, r, .50f, .41f, .06f, FriendUI.Blue); Dot(g, r, .70f, .41f, .06f, FriendUI.Blue);
        }

        /// <summary>칩 모양: 모델</summary>
        internal static void Chip(Graphics g, RectangleF r, Color ink)
        {
            using (var pen = Line(r, ink, .08f))
            {
                foreach (float t in new[] { .38f, .62f })
                {
                    g.DrawLine(pen, Pt(r, t, .08f), Pt(r, t, .26f)); g.DrawLine(pen, Pt(r, t, .74f), Pt(r, t, .92f));
                    g.DrawLine(pen, Pt(r, .08f, t), Pt(r, .26f, t)); g.DrawLine(pen, Pt(r, .74f, t), Pt(r, .92f, t));
                }
                using (var p = Round(Box(r, .22f, .22f, .78f, .78f), r.Width * .1f)) { using (var br = new SolidBrush(Soft)) g.FillPath(br, p); g.DrawPath(pen, p); }
            }
            using (var p = Round(Box(r, .38f, .38f, .62f, .62f), r.Width * .04f)) using (var br = new SolidBrush(FriendUI.Blue)) g.FillPath(br, p);
        }

        /// <summary>지구본: 서버 주소</summary>
        internal static void Globe(Graphics g, RectangleF r, Color ink)
        {
            using (var pen = Line(r, ink, .07f))
            {
                using (var br = new SolidBrush(Soft)) g.FillEllipse(br, Box(r, .08f, .08f, .92f, .92f));
                g.DrawEllipse(pen, Box(r, .08f, .08f, .92f, .92f));
                g.DrawEllipse(pen, Box(r, .32f, .08f, .68f, .92f));
                g.DrawLine(pen, Pt(r, .08f, .50f), Pt(r, .92f, .50f));
            }
            using (var pen = Line(r, FriendUI.Blue, .06f)) g.DrawLine(pen, Pt(r, .15f, .29f), Pt(r, .85f, .29f));
        }

        /// <summary>플러그 + 초록 체크: 연결 확인</summary>
        internal static void PlugCheck(Graphics g, RectangleF r, Color ink)
        {
            using (var pen = Line(r, ink, .08f))
            {
                g.DrawLine(pen, Pt(r, .22f, .30f), Pt(r, .22f, .10f)); g.DrawLine(pen, Pt(r, .46f, .30f), Pt(r, .46f, .10f));
                g.DrawLine(pen, Pt(r, .34f, .58f), Pt(r, .34f, .74f));
            }
            using (var p = Round(Box(r, .08f, .28f, .60f, .60f), r.Width * .1f)) using (var br = new SolidBrush(FriendUI.Blue)) g.FillPath(br, p);
            Dot(g, r, .74f, .74f, .24f, Green);
            using (var pen = Line(r, Color.White, .08f)) g.DrawLines(pen, new[] { Pt(r, .63f, .74f), Pt(r, .72f, .83f), Pt(r, .87f, .65f) });
        }

        internal static void Download(Graphics g, RectangleF r, Color ink)
        {
            using (var pen = Line(r, ink, .09f)) g.DrawLines(pen, new[] { Pt(r, .14f, .62f), Pt(r, .14f, .86f), Pt(r, .86f, .86f), Pt(r, .86f, .62f) });
            using (var pen = Line(r, FriendUI.Blue, .11f))
            {
                g.DrawLine(pen, Pt(r, .5f, .10f), Pt(r, .5f, .64f));
                g.DrawLines(pen, new[] { Pt(r, .28f, .44f), Pt(r, .5f, .66f), Pt(r, .72f, .44f) });
            }
        }

        internal static void Stop(Graphics g, RectangleF r, Color ink)
        { using (var p = Round(Box(r, .22f, .22f, .78f, .78f), r.Width * .1f)) using (var br = new SolidBrush(Danger)) g.FillPath(br, p); }

        // 대화 방식 (친구 · 공감 · 설명 · 장난 · 간결)
        internal static void Smile(Graphics g, RectangleF r, Color ink)
        {
            FaceDisc(g, r, ink);
            Dot(g, r, .35f, .42f, .055f, ink); Dot(g, r, .65f, .42f, .055f, ink);
            using (var pen = Line(r, ink, .08f)) g.DrawArc(pen, Box(r, .28f, .36f, .72f, .74f), 25, 130);
        }
        internal static void Laugh(Graphics g, RectangleF r, Color ink)
        {
            FaceDisc(g, r, ink);
            using (var pen = Line(r, ink, .08f)) { g.DrawArc(pen, Box(r, .22f, .32f, .42f, .52f), 200, 140); g.DrawArc(pen, Box(r, .58f, .32f, .78f, .52f), 200, 140); }
            using (var p = new GraphicsPath()) { p.AddArc(Box(r, .27f, .42f, .73f, .84f), 0, 180); p.CloseFigure(); using (var br = new SolidBrush(ink)) g.FillPath(br, p); }
        }
        internal static void Heart(Graphics g, RectangleF r, Color ink) { HeartShape(g, Box(r, .08f, .14f, .92f, .88f), Rose); }
        internal static void Bulb(Graphics g, RectangleF r, Color ink)
        {
            var b = Box(r, .18f, .06f, .82f, .68f);
            using (var br = new SolidBrush(FriendUI.Gold)) g.FillEllipse(br, b);
            using (var pen = Line(r, ink, .07f))
            {
                g.DrawEllipse(pen, b);
                g.DrawLine(pen, Pt(r, .40f, .62f), Pt(r, .40f, .80f)); g.DrawLine(pen, Pt(r, .60f, .62f), Pt(r, .60f, .80f));
                g.DrawLine(pen, Pt(r, .38f, .80f), Pt(r, .62f, .80f)); g.DrawLine(pen, Pt(r, .43f, .92f), Pt(r, .57f, .92f));
            }
            using (var pen = Line(r, FriendUI.Blue, .06f)) g.DrawLines(pen, new[] { Pt(r, .40f, .54f), Pt(r, .46f, .36f), Pt(r, .54f, .54f), Pt(r, .60f, .36f) });
        }
        internal static void Bolt(Graphics g, RectangleF r, Color ink)
        {
            using (var p = new GraphicsPath())
            {
                p.AddPolygon(new[] { Pt(r, .60f, .04f), Pt(r, .20f, .56f), Pt(r, .46f, .56f), Pt(r, .38f, .96f), Pt(r, .80f, .40f), Pt(r, .54f, .40f) });
                using (var br = new SolidBrush(FriendUI.Gold)) g.FillPath(br, p);
                using (var pen = Line(r, ink, .06f)) g.DrawPath(pen, p);
            }
        }

        // 말 걸기
        internal static void Keyboard(Graphics g, RectangleF r, Color ink)
        {
            using (var p = Round(Box(r, .04f, .22f, .96f, .80f), r.Width * .1f))
            {
                using (var br = new SolidBrush(Soft)) g.FillPath(br, p);
                using (var pen = Line(r, ink, .07f)) g.DrawPath(pen, p);
            }
            foreach (float y in new[] { .38f, .52f }) foreach (float x in new[] { .20f, .38f, .56f, .74f }) Dot(g, r, x, y, .04f, ink);
            using (var pen = Line(r, FriendUI.Blue, .07f)) g.DrawLine(pen, Pt(r, .28f, .67f), Pt(r, .72f, .67f));
        }

        /// <summary>되돌림 화살표: 기본값</summary>
        internal static void Reset(Graphics g, RectangleF r, Color ink)
        {
            using (var pen = Line(r, ink, .10f))
            {
                g.DrawArc(pen, Box(r, .16f, .16f, .84f, .84f), 300, 280);
                double a = 220 * Math.PI / 180, dx = -Math.Sin(a), dy = Math.Cos(a);   // 끝점에서 진행 방향
                PointF tip = new PointF(X(r, .5f) + (float)Math.Cos(a) * r.Width * .34f, Y(r, .5f) + (float)Math.Sin(a) * r.Height * .34f);
                float L = r.Width * .22f;
                foreach (double s in new[] { -0.55, 0.55 })
                {
                    double ca = Math.Cos(s), sa = Math.Sin(s);
                    float bx = (float)(dx * ca - dy * sa), by = (float)(dx * sa + dy * ca);
                    g.DrawLine(pen, tip, new PointF(tip.X - bx * L, tip.Y - by * L));
                }
            }
        }

        internal static void Clock(Graphics g, RectangleF r, Color ink)
        {
            using (var br = new SolidBrush(Soft)) g.FillEllipse(br, Box(r, .08f, .08f, .92f, .92f));
            using (var pen = Line(r, ink, .08f)) g.DrawEllipse(pen, Box(r, .08f, .08f, .92f, .92f));
            using (var pen = Line(r, FriendUI.Blue, .09f)) g.DrawLines(pen, new[] { Pt(r, .50f, .26f), Pt(r, .50f, .50f), Pt(r, .68f, .60f) });
        }

        /// <summary>반짝이: 먼저 말 걸기</summary>
        internal static void Spark(Graphics g, RectangleF r, Color ink)
        {
            using (var p = Star(r, .42f, .56f, .40f, .11f)) using (var br = new SolidBrush(FriendUI.Gold)) g.FillPath(br, p);
            using (var p = Star(r, .80f, .22f, .17f, .05f)) using (var br = new SolidBrush(FriendUI.Blue)) g.FillPath(br, p);
        }

        internal static void Sound(Graphics g, RectangleF r, Color ink)
        {
            using (var p = new GraphicsPath())
            {
                p.AddPolygon(new[] { Pt(r, .06f, .38f), Pt(r, .24f, .38f), Pt(r, .44f, .20f), Pt(r, .44f, .80f), Pt(r, .24f, .62f), Pt(r, .06f, .62f) });
                using (var br = new SolidBrush(ink)) g.FillPath(br, p);
            }
            using (var pen = Line(r, FriendUI.Blue, .08f))
            {
                g.DrawArc(pen, Box(r, .30f, .34f, .62f, .66f), -50, 100);
                g.DrawArc(pen, Box(r, .16f, .20f, .78f, .80f), -50, 100);
            }
        }

        /// <summary>알약 칩 세 개: 답장 추천</summary>
        internal static void ReplyChips(Graphics g, RectangleF r, Color ink)
        {
            using (var pen = Line(r, FriendUI.Blue, .07f))
            {
                using (var p = Round(Box(r, .08f, .10f, .72f, .34f), r.Width * .12f)) { using (var br = new SolidBrush(Soft)) g.FillPath(br, p); g.DrawPath(pen, p); }
                using (var p = Round(Box(r, .28f, .40f, .92f, .64f), r.Width * .12f)) using (var br = new SolidBrush(FriendUI.Blue)) g.FillPath(br, p);
                using (var p = Round(Box(r, .08f, .70f, .60f, .94f), r.Width * .12f)) { using (var br = new SolidBrush(Soft)) g.FillPath(br, p); g.DrawPath(pen, p); }
            }
        }

        /// <summary>생각 구름 + 하트: 기억</summary>
        internal static void Memory(Graphics g, RectangleF r, Color ink)
        {
            RectangleF[] cl = { Box(r, .06f, .24f, .46f, .56f), Box(r, .24f, .06f, .68f, .48f), Box(r, .50f, .20f, .94f, .56f), Box(r, .18f, .32f, .84f, .62f) };
            float h = r.Width * .045f;
            using (var br = new SolidBrush(ink)) foreach (var c in cl) g.FillEllipse(br, c.X - h, c.Y - h, c.Width + 2 * h, c.Height + 2 * h);
            using (var br = new SolidBrush(Soft)) foreach (var c in cl) g.FillEllipse(br, c);
            HeartShape(g, Box(r, .37f, .22f, .63f, .48f), Rose);
            Dot(g, r, .26f, .76f, .07f, ink); Dot(g, r, .13f, .90f, .045f, ink);
        }

        /// <summary>책 + 책갈피: 일기장</summary>
        internal static void Diary(Graphics g, RectangleF r, Color ink)
        {
            using (var p = Round(Box(r, .16f, .06f, .86f, .94f), r.Width * .07f)) using (var br = new SolidBrush(FriendUI.Blue)) g.FillPath(br, p);
            using (var br = new SolidBrush(FriendUI.BlueDark)) g.FillRectangle(br, X(r, .16f), Y(r, .06f), r.Width * .12f, r.Height * .88f);
            using (var pen = Line(r, Color.White, .07f)) { g.DrawLine(pen, Pt(r, .42f, .40f), Pt(r, .72f, .40f)); g.DrawLine(pen, Pt(r, .42f, .56f), Pt(r, .72f, .56f)); g.DrawLine(pen, Pt(r, .42f, .72f), Pt(r, .62f, .72f)); }
            using (var p = new GraphicsPath()) { p.AddPolygon(new[] { Pt(r, .58f, .06f), Pt(r, .74f, .06f), Pt(r, .74f, .32f), Pt(r, .66f, .25f), Pt(r, .58f, .32f) }); using (var br = new SolidBrush(FriendUI.Gold)) g.FillPath(br, p); }
        }

        /// <summary>눈 모양: 모두 숨기기 / 보이기</summary>
        internal static void Eye(Graphics g, RectangleF r, Color ink)
        {
            using (var pen = Line(r, ink, .09f))
            {
                using (var path = new GraphicsPath())
                {
                    path.AddBezier(Pt(r, .10f, .50f), Pt(r, .32f, .22f), Pt(r, .68f, .22f), Pt(r, .90f, .50f));
                    path.AddBezier(Pt(r, .90f, .50f), Pt(r, .68f, .78f), Pt(r, .32f, .78f), Pt(r, .10f, .50f));
                    path.CloseFigure();
                    g.DrawPath(pen, path);
                }
                float d = r.Width * .26f;
                using (var br = new SolidBrush(ink))
                    g.FillEllipse(br, X(r, .50f) - d / 2, Y(r, .50f) - d / 2, d, d);
            }
        }

        /// <summary>화면과 중심점 모양: 화면 가운데로 모으기</summary>
        internal static void CenterScreen(Graphics g, RectangleF r, Color ink)
        {
            using (var pen = Line(r, ink, .09f))
            {
                var box = Box(r, .12f, .16f, .88f, .72f);
                using (var path = Round(box, 3f)) g.DrawPath(pen, path);
                g.DrawLine(pen, Pt(r, .50f, .72f), Pt(r, .50f, .88f));
                g.DrawLine(pen, Pt(r, .32f, .88f), Pt(r, .68f, .88f));
                float d = r.Width * .16f;
                using (var br = new SolidBrush(ink))
                    g.FillEllipse(br, X(r, .50f) - d / 2, Y(r, .44f) - d / 2, d, d);
            }
        }

        /// <summary>상자에서 나가는 화살표: 열기</summary>
        internal static void OpenOut(Graphics g, RectangleF r, Color ink)
        {
            using (var pen = Line(r, ink, .09f)) g.DrawLines(pen, new[] { Pt(r, .40f, .22f), Pt(r, .12f, .22f), Pt(r, .12f, .88f), Pt(r, .78f, .88f), Pt(r, .78f, .60f) });
            using (var pen = Line(r, FriendUI.Blue, .10f))
            {
                g.DrawLine(pen, Pt(r, .40f, .60f), Pt(r, .88f, .12f));
                g.DrawLines(pen, new[] { Pt(r, .62f, .12f), Pt(r, .88f, .12f), Pt(r, .88f, .38f) });
            }
        }
    }

    /// <summary>그림만 있는 납작한 단추. 이름은 말풍선과 접근성 이름으로만 쓴다.</summary>
    internal sealed class FlatButton : Control
    {
        private readonly FlatIcon.Painter paint; private bool hot, down;

        internal FlatButton(FlatIcon.Painter paint, string name, int w = 38, int h = 30)
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable | ControlStyles.StandardClick, true);
            this.paint = paint; AccessibleName = name; AccessibleRole = AccessibleRole.PushButton;
            Size = new Size(w, h); Margin = new Padding(3); TabStop = true; Cursor = Cursors.Hand;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; g.Clear(BackColor);
            float s = DeviceDpi / 96f; var card = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = ChoiceStrip.RoundRect(card, (int)Math.Round(7 * s)))
            {
                using (var fill = new SolidBrush(down ? Color.FromArgb(214, 228, 250) : hot ? Color.FromArgb(232, 240, 255) : Color.White)) g.FillPath(fill, path);
                using (var pen = new Pen(hot || Focused ? FriendUI.Blue : Color.FromArgb(200, 212, 235), 1f)) g.DrawPath(pen, path);
                float d = 18 * s;
                paint(g, new RectangleF((Width - d) / 2f, (Height - d) / 2f, d, d), FriendUI.BlueDark);
                if (!Enabled) using (var veil = new SolidBrush(Color.FromArgb(165, 248, 250, 255))) g.FillPath(veil, path);
            }
        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); hot = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hot = down = false; Invalidate(); }
        protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); if (e.Button == MouseButtons.Left) { Focus(); down = true; Invalidate(); } }
        protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); down = false; Invalidate(); }
        protected override void OnKeyDown(KeyEventArgs e) { base.OnKeyDown(e); if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter) { OnClick(EventArgs.Empty); e.Handled = true; } }
        protected override bool IsInputKey(Keys keyData) { return (keyData & Keys.KeyCode) == Keys.Space || base.IsInputKey(keyData); }
        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
        protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); Invalidate(); }
    }

    /// <summary>작은 아이콘 하나만 그리는 컨트롤(글 옆 표지).</summary>
    internal sealed class IconBox : Control
    {
        private readonly FlatIcon.Painter paint;
        internal IconBox(FlatIcon.Painter paint, int size = 22)
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
            this.paint = paint; Size = new Size(size, size); Margin = new Padding(3, 2, 3, 2); BackColor = Color.Transparent; TabStop = false;
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var r = new RectangleF(1, 1, Width - 2, Height - 2);
            paint(e.Graphics, r, FriendUI.BlueDark);
        }
    }

    /// <summary>
    /// 그림 + 짧은 이름 + 키캡으로 보여 주는 단축키 카드. wide 는 어디서나 쓰는 큰 카드(동그란 아이콘 바탕), 아니면 글 쓰는 중에 쓰는 작은 타일.
    /// 자세한 설명은 마우스를 올렸을 때의 말풍선에 둔다.
    /// </summary>
    internal sealed class HintTile : Control
    {
        private readonly FlatIcon.Painter icon; private readonly string caption; private readonly string[] keys; private readonly bool wide;
        private bool hot;

        internal HintTile(FlatIcon.Painter icon, string caption, string[] keys, bool wide)
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            this.icon = icon; this.caption = caption; this.keys = keys; this.wide = wide;
            Size = wide ? new Size(240, 62) : new Size(116, 62); Margin = new Padding(0, 0, 6, 6); TabStop = false;
            AccessibleName = caption + " " + string.Join("+", keys);
        }

        private float S { get { return DeviceDpi / 96f; } }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; g.Clear(BackColor);
            float s = S; var card = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = ChoiceStrip.RoundRect(card, (int)Math.Round(8 * s)))
            {
                using (var fill = new SolidBrush(Color.White)) g.FillPath(fill, path);
                using (var pen = new Pen(hot ? Color.FromArgb(120, 160, 230) : Color.FromArgb(206, 218, 240), 1f)) g.DrawPath(pen, path);
            }
            using (var font = new Font("Malgun Gothic", wide ? 9.5f : 8.5f, FontStyle.Bold))
            {
                int keyH = (int)Math.Round(20 * s);
                if (wide)
                {
                    float d = 40 * s, cy = (Height - d) / 2f;
                    using (var br = new SolidBrush(Color.FromArgb(232, 240, 255))) g.FillEllipse(br, 10 * s, cy, d, d);
                    icon(g, new RectangleF(10 * s + d * .2f, cy + d * .2f, d * .6f, d * .6f), FriendUI.BlueDark);
                    int tx = (int)Math.Round(10 * s + d + 10 * s);
                    TextRenderer.DrawText(g, caption, font, new Rectangle(tx, (int)Math.Round(8 * s), Width - tx - 6, (int)Math.Round(20 * s)), FriendUI.Ink, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
                    DrawKeys(g, tx, (int)Math.Round(Height - 9 * s - keyH), keyH, s);
                }
                else
                {
                    float d = 24 * s;
                    icon(g, new RectangleF(10 * s, 8 * s, d, d), FriendUI.BlueDark);
                    int tx = (int)Math.Round(10 * s + d + 6 * s);
                    TextRenderer.DrawText(g, caption, font, new Rectangle(tx, (int)Math.Round(8 * s), Width - tx - 4, (int)Math.Round(d)), FriendUI.Ink, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
                    DrawKeys(g, (int)Math.Round(10 * s), (int)Math.Round(Height - 8 * s - keyH), keyH, s);
                }
            }
        }

        // 키캡: 아래쪽에 한 줄 그림자를 깔아 눌리는 단추처럼 보이게 한다.
        private void DrawKeys(Graphics g, int x, int y, int h, float s)
        {
            using (var font = new Font("Segoe UI", 8f, FontStyle.Bold))
                foreach (string k in keys)
                {
                    int w = Math.Max(h, TextRenderer.MeasureText(g, k, font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix).Width + (int)Math.Round(8 * s));
                    int sh = Math.Max(2, (int)Math.Round(2 * s));
                    var cap = new Rectangle(x, y - sh, w, h - sh + 1);
                    using (var p = ChoiceStrip.RoundRect(new Rectangle(x, y, w, h - sh), (int)Math.Round(4 * s))) using (var br = new SolidBrush(Color.FromArgb(190, 204, 232))) g.FillPath(br, p);
                    using (var p = ChoiceStrip.RoundRect(cap, (int)Math.Round(4 * s)))
                    {
                        using (var br = new SolidBrush(Color.FromArgb(247, 249, 254))) g.FillPath(br, p);
                        using (var pen = new Pen(Color.FromArgb(176, 192, 222), 1f)) g.DrawPath(pen, p);
                    }
                    TextRenderer.DrawText(g, k, font, cap, FriendUI.BlueDark, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                    x += w + (int)Math.Round(3 * s);
                }
        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); hot = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hot = false; Invalidate(); }
    }
}
