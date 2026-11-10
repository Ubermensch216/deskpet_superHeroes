using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace SuperHeroes
{
    /// <summary>
    /// 설정창 "행동" 쪽(움직임 · 반응 · 생활)에서 쓰는 납작한 벡터 아이콘. SettingsArt.cs 의 FlatIcon 에 이어 GDI+ 로 직접 그린다.
    /// 설정창 전용 그림이라 캐릭터 가이드북 색 규정과는 무관하다(캐릭터 본체는 여기서 그리지 않고, 노란 동그라미로 "친구"를 나타낸다).
    /// </summary>
    internal static partial class FlatIcon
    {
        private static readonly Color CookieCol = Color.FromArgb(238, 186, 98), Choc = Color.FromArgb(120, 82, 52);

        // 선 + 화살촉
        private static void Arrow(Graphics g, RectangleF r, Pen pen, float x0, float y0, float x1, float y1, float head = .16f)
        {
            PointF a = Pt(r, x0, y0), b = Pt(r, x1, y1);
            g.DrawLine(pen, a, b);
            double ang = Math.Atan2(b.Y - a.Y, b.X - a.X), len = r.Width * head;
            foreach (double s in new[] { -0.6, 0.6 })
                g.DrawLine(pen, b, new PointF(b.X - (float)(Math.Cos(ang + s) * len), b.Y - (float)(Math.Sin(ang + s) * len)));
        }

        // 마우스 커서 화살표
        private static readonly PointF[] Ptr = { new PointF(.18f, .08f), new PointF(.18f, .80f), new PointF(.36f, .63f), new PointF(.48f, .92f), new PointF(.61f, .86f), new PointF(.49f, .58f), new PointF(.75f, .58f) };
        private static void PointerShape(Graphics g, RectangleF r, float ox, float oy, float k, Color ink)
        {
            var pts = new PointF[Ptr.Length];
            for (int i = 0; i < pts.Length; i++) pts[i] = Pt(r, ox + Ptr[i].X * k, oy + Ptr[i].Y * k);
            using (var br = new SolidBrush(Color.White)) g.FillPolygon(br, pts);
            using (var pen = Line(r, ink, .07f)) g.DrawPolygon(pen, pts);
        }

        // 친구(노란 동그라미)
        private static void Friend(Graphics g, RectangleF r, float cx, float cy, float rad, Color ink)
        {
            var b = new RectangleF(X(r, cx) - r.Width * rad, Y(r, cy) - r.Width * rad, r.Width * rad * 2, r.Width * rad * 2);
            using (var br = new SolidBrush(FriendUI.Gold)) g.FillEllipse(br, b);
            using (var pen = Line(r, ink, .06f)) g.DrawEllipse(pen, b);
        }

        // ───────── 줄 머리표 ─────────
        internal static void PlacePin(Graphics g, RectangleF r, Color ink)
        {
            using (var p = new GraphicsPath())
            {
                p.AddBezier(Pt(r, .50f, .94f), Pt(r, .12f, .58f), Pt(r, .16f, .08f), Pt(r, .50f, .08f));
                p.AddBezier(Pt(r, .50f, .08f), Pt(r, .84f, .08f), Pt(r, .88f, .58f), Pt(r, .50f, .94f));
                p.CloseFigure();
                using (var br = new SolidBrush(FriendUI.Blue)) g.FillPath(br, p);
            }
            Dot(g, r, .50f, .38f, .12f, Color.White);
        }

        internal static void Cup(Graphics g, RectangleF r, Color ink)
        {
            using (var pen = Line(r, ink, .07f))
            {
                using (var p = new GraphicsPath())
                {
                    p.AddPolygon(new[] { Pt(r, .14f, .38f), Pt(r, .66f, .38f), Pt(r, .60f, .86f), Pt(r, .20f, .86f) });
                    using (var br = new SolidBrush(Soft)) g.FillPath(br, p);
                    g.DrawPath(pen, p);
                }
                g.DrawArc(pen, Box(r, .52f, .42f, .88f, .70f), -90, 180);
            }
            using (var pen = Line(r, FriendUI.Blue, .07f))
                foreach (float x in new[] { .26f, .40f, .54f }) g.DrawLine(pen, Pt(r, x, .28f), Pt(r, x, .12f));
        }

        internal static void Gauge(Graphics g, RectangleF r, Color ink)
        {
            using (var pen = Line(r, ink, .09f)) g.DrawArc(pen, Box(r, .08f, .20f, .92f, 1.04f), 180, 180);
            using (var pen = Line(r, FriendUI.Blue, .09f)) g.DrawLine(pen, Pt(r, .50f, .62f), Pt(r, .72f, .34f));
            Dot(g, r, .50f, .62f, .07f, ink);
            using (var pen = Line(r, ink, .07f)) g.DrawLine(pen, Pt(r, .12f, .62f), Pt(r, .88f, .62f));
        }

        internal static void Tap(Graphics g, RectangleF r, Color ink)
        {
            PointF tip = Pt(r, .30f + .18f * .7f, .28f + .08f * .7f);
            using (var pen = Line(r, FriendUI.Blue, .08f))
                foreach (float rad in new[] { .14f, .26f })
                {
                    float d = r.Width * rad;
                    g.DrawArc(pen, tip.X - d, tip.Y - d, d * 2, d * 2, 180, 90);
                }
            PointerShape(g, r, .30f, .28f, .7f, ink);
        }

        internal static void CursorRing(Graphics g, RectangleF r, Color ink)
        {
            using (var pen = Line(r, FriendUI.Blue, .07f)) { pen.DashStyle = DashStyle.Dash; g.DrawEllipse(pen, Box(r, .06f, .06f, .94f, .94f)); }
            PointerShape(g, r, .22f, .14f, .78f, ink);
        }

        internal static void Question(Graphics g, RectangleF r, Color ink) { Letter(g, r, FriendUI.Blue, "?", FontStyle.Bold, .95f); }

        // ───────── 움직임: 어디서 ─────────
        internal static void Wall(Graphics g, RectangleF r, Color ink)
        {
            using (var p = Round(Box(r, .08f, .06f, .36f, .94f), r.Width * .05f))
            {
                using (var br = new SolidBrush(Soft)) g.FillPath(br, p);
                using (var pen = Line(r, ink, .07f))
                {
                    g.DrawPath(pen, p);
                    foreach (float y in new[] { .30f, .52f, .74f }) g.DrawLine(pen, Pt(r, .08f, y), Pt(r, .36f, y));
                }
            }
            using (var pen = Line(r, FriendUI.Blue, .09f)) Arrow(g, r, pen, .68f, .88f, .68f, .16f);
        }

        internal static void Ceiling(Graphics g, RectangleF r, Color ink)
        {
            using (var p = Round(Box(r, .06f, .08f, .94f, .36f), r.Width * .05f))
            {
                using (var br = new SolidBrush(Soft)) g.FillPath(br, p);
                using (var pen = Line(r, ink, .07f))
                {
                    g.DrawPath(pen, p);
                    foreach (float x in new[] { .28f, .50f, .72f }) g.DrawLine(pen, Pt(r, x, .08f), Pt(r, x, .36f));
                }
            }
            using (var pen = Line(r, FriendUI.Blue, .09f)) { Arrow(g, r, pen, .50f, .70f, .84f, .70f); Arrow(g, r, pen, .50f, .70f, .16f, .70f); }
        }

        internal static void Window(Graphics g, RectangleF r, Color ink)
        {
            using (var p = Round(Box(r, .08f, .28f, .92f, .92f), r.Width * .07f))
            {
                using (var br = new SolidBrush(Soft)) g.FillPath(br, p);
                using (var pen = Line(r, ink, .07f)) g.DrawPath(pen, p);
            }
            using (var p = Round(Box(r, .08f, .28f, .92f, .46f), r.Width * .07f)) using (var br = new SolidBrush(FriendUI.Blue)) g.FillPath(br, p);
            using (var pen = Line(r, ink, .05f)) { g.DrawLine(pen, Pt(r, .50f, .46f), Pt(r, .50f, .92f)); g.DrawLine(pen, Pt(r, .08f, .69f), Pt(r, .92f, .69f)); }
            Friend(g, r, .30f, .16f, .10f, ink);
        }

        internal static void FullScreen(Graphics g, RectangleF r, Color ink)
        {
            using (var pen = Line(r, ink, .08f))
            {
                g.DrawLines(pen, new[] { Pt(r, .10f, .34f), Pt(r, .10f, .10f), Pt(r, .34f, .10f) });
                g.DrawLines(pen, new[] { Pt(r, .66f, .10f), Pt(r, .90f, .10f), Pt(r, .90f, .34f) });
                g.DrawLines(pen, new[] { Pt(r, .90f, .66f), Pt(r, .90f, .90f), Pt(r, .66f, .90f) });
                g.DrawLines(pen, new[] { Pt(r, .34f, .90f), Pt(r, .10f, .90f), Pt(r, .10f, .66f) });
            }
            using (var br = new SolidBrush(FriendUI.Blue)) g.FillPolygon(br, new[] { Pt(r, .42f, .34f), Pt(r, .42f, .66f), Pt(r, .68f, .50f) });
        }

        // ───────── 움직임: 쉬기 · 조용히 ─────────
        internal static void Zzz(Graphics g, RectangleF r, Color ink)
        {
            using (var pen = Line(r, FriendUI.Blue, .08f))
                foreach (float[] z in new[] { new[] { .10f, .58f, .22f }, new[] { .36f, .34f, .28f }, new[] { .64f, .08f, .28f } })
                    g.DrawLines(pen, new[] { Pt(r, z[0], z[1]), Pt(r, z[0] + z[2], z[1]), Pt(r, z[0], z[1] + z[2]), Pt(r, z[0] + z[2], z[1] + z[2]) });
        }

        internal static void Hourglass(Graphics g, RectangleF r, Color ink)
        {
            using (var br = new SolidBrush(FriendUI.Gold)) g.FillPolygon(br, new[] { Pt(r, .36f, .86f), Pt(r, .64f, .86f), Pt(r, .50f, .64f) });
            using (var pen = Line(r, ink, .08f))
            {
                g.DrawLine(pen, Pt(r, .20f, .10f), Pt(r, .80f, .10f)); g.DrawLine(pen, Pt(r, .20f, .90f), Pt(r, .80f, .90f));
                g.DrawPolygon(pen, new[] { Pt(r, .28f, .10f), Pt(r, .72f, .10f), Pt(r, .53f, .50f), Pt(r, .72f, .90f), Pt(r, .28f, .90f), Pt(r, .47f, .50f) });
            }
            using (var pen = Line(r, FriendUI.Blue, .05f)) g.DrawLine(pen, Pt(r, .50f, .40f), Pt(r, .50f, .62f));
        }

        internal static void Moon(Graphics g, RectangleF r, Color ink)
        {
            using (var outer = new GraphicsPath()) using (var inner = new GraphicsPath())
            {
                outer.AddEllipse(Box(r, .08f, .12f, .82f, .86f)); inner.AddEllipse(Box(r, .34f, .02f, .98f, .66f));
                using (var rg = new Region(outer)) { rg.Exclude(inner); using (var br = new SolidBrush(FriendUI.Gold)) g.FillRegion(br, rg); }
            }
            using (var p = Star(r, .76f, .66f, .17f, .05f)) using (var br = new SolidBrush(FriendUI.Blue)) g.FillPath(br, p);
        }

        internal static void Briefcase(Graphics g, RectangleF r, Color ink)
        {
            using (var pen = Line(r, ink, .07f))
            {
                g.DrawLines(pen, new[] { Pt(r, .34f, .32f), Pt(r, .34f, .16f), Pt(r, .66f, .16f), Pt(r, .66f, .32f) });
                using (var p = Round(Box(r, .08f, .32f, .92f, .88f), r.Width * .08f)) { using (var br = new SolidBrush(Soft)) g.FillPath(br, p); g.DrawPath(pen, p); }
                g.DrawLine(pen, Pt(r, .08f, .56f), Pt(r, .92f, .56f));
            }
            using (var p = Round(Box(r, .42f, .50f, .58f, .64f), r.Width * .03f)) using (var br = new SolidBrush(FriendUI.Blue)) g.FillPath(br, p);
        }

        // ───────── 움직임: 속도 ─────────
        private static void Foot(Graphics g, RectangleF r, float cx, float cy, Color ink)
        {
            using (var br = new SolidBrush(ink)) g.FillEllipse(br, Box(r, cx - .14f, cy - .04f, cx + .14f, cy + .32f));
            Dot(g, r, cx - .10f, cy - .17f, .05f, ink); Dot(g, r, cx, cy - .22f, .05f, ink); Dot(g, r, cx + .10f, cy - .17f, .05f, ink);
        }
        internal static void Feet(Graphics g, RectangleF r, Color ink) { Foot(g, r, .28f, .60f, ink); Foot(g, r, .70f, .38f, FriendUI.Blue); }

        internal static void Feather(Graphics g, RectangleF r, Color ink)
        {
            using (var p = new GraphicsPath())
            {
                p.AddBezier(Pt(r, .20f, .88f), Pt(r, .08f, .30f), Pt(r, .50f, .08f), Pt(r, .88f, .10f));
                p.AddBezier(Pt(r, .88f, .10f), Pt(r, .90f, .52f), Pt(r, .56f, .92f), Pt(r, .20f, .88f));
                p.CloseFigure();
                using (var br = new SolidBrush(Soft)) g.FillPath(br, p);
                using (var pen = Line(r, ink, .07f)) g.DrawPath(pen, p);
            }
            using (var pen = Line(r, FriendUI.Blue, .07f)) g.DrawLine(pen, Pt(r, .16f, .92f), Pt(r, .74f, .26f));
        }

        internal static void Slide(Graphics g, RectangleF r, Color ink)
        {
            using (var pen = Line(r, ink, .08f))
            {
                g.DrawLine(pen, Pt(r, .08f, .30f), Pt(r, .44f, .30f)); g.DrawLine(pen, Pt(r, .18f, .50f), Pt(r, .56f, .50f)); g.DrawLine(pen, Pt(r, .08f, .70f), Pt(r, .44f, .70f));
            }
            using (var pen = Line(r, FriendUI.Blue, .10f))
            {
                g.DrawLines(pen, new[] { Pt(r, .62f, .28f), Pt(r, .76f, .50f), Pt(r, .62f, .72f) });
                g.DrawLines(pen, new[] { Pt(r, .78f, .28f), Pt(r, .92f, .50f), Pt(r, .78f, .72f) });
            }
        }

        // ───────── 반응: 클릭 ─────────
        internal static void Cycle(Graphics g, RectangleF r, Color ink)
        {
            Dot(g, r, .15f, .50f, .07f, ink); Dot(g, r, .50f, .50f, .09f, ink); Dot(g, r, .85f, .50f, .11f, FriendUI.Blue);
            using (var pen = Line(r, FriendUI.Blue, .07f)) { Arrow(g, r, pen, .26f, .50f, .36f, .50f, .1f); Arrow(g, r, pen, .62f, .50f, .70f, .50f, .1f); }
        }

        internal static void Dice(Graphics g, RectangleF r, Color ink)
        {
            using (var p = Round(Box(r, .10f, .10f, .90f, .90f), r.Width * .16f))
            {
                using (var br = new SolidBrush(Soft)) g.FillPath(br, p);
                using (var pen = Line(r, ink, .07f)) g.DrawPath(pen, p);
            }
            foreach (float[] d in new[] { new[] { .32f, .32f }, new[] { .68f, .32f }, new[] { .50f, .50f }, new[] { .32f, .68f }, new[] { .68f, .68f } }) Dot(g, r, d[0], d[1], .065f, FriendUI.Blue);
        }

        internal static void Squash(Graphics g, RectangleF r, Color ink)
        {
            var b = Box(r, .06f, .58f, .94f, .90f);
            using (var br = new SolidBrush(FriendUI.Gold)) g.FillEllipse(br, b);
            using (var pen = Line(r, ink, .07f)) g.DrawEllipse(pen, b);
            using (var pen = Line(r, FriendUI.Blue, .09f)) Arrow(g, r, pen, .50f, .06f, .50f, .46f);
        }

        internal static void Tear(Graphics g, RectangleF r, Color ink)
        {
            using (var p = new GraphicsPath())
            {
                p.AddBezier(Pt(r, .50f, .06f), Pt(r, .90f, .50f), Pt(r, .84f, .94f), Pt(r, .50f, .94f));
                p.AddBezier(Pt(r, .50f, .94f), Pt(r, .16f, .94f), Pt(r, .10f, .50f), Pt(r, .50f, .06f));
                p.CloseFigure();
                using (var br = new SolidBrush(FriendUI.Blue)) g.FillPath(br, p);
            }
            using (var pen = Line(r, Color.White, .07f)) g.DrawLine(pen, Pt(r, .36f, .62f), Pt(r, .38f, .76f));
        }

        internal static void Hop(Graphics g, RectangleF r, Color ink)
        {
            using (var pen = Line(r, ink, .07f)) g.DrawLine(pen, Pt(r, .08f, .88f), Pt(r, .92f, .88f));
            using (var pen = Line(r, FriendUI.Blue, .07f)) { pen.DashStyle = DashStyle.Dot; g.DrawBezier(pen, Pt(r, .14f, .84f), Pt(r, .24f, .04f), Pt(r, .76f, .04f), Pt(r, .86f, .84f)); }
            Friend(g, r, .50f, .26f, .11f, ink);
        }

        internal static void Ban(Graphics g, RectangleF r, Color ink)
        {
            using (var pen = Line(r, Danger, .09f)) { g.DrawEllipse(pen, Box(r, .12f, .12f, .88f, .88f)); g.DrawLine(pen, Pt(r, .24f, .24f), Pt(r, .76f, .76f)); }
        }

        // ───────── 반응: 커서가 가까이 ─────────
        internal static void Escape(Graphics g, RectangleF r, Color ink)
        {
            using (var pen = Line(r, FriendUI.Blue, .07f)) { g.DrawLine(pen, Pt(r, .34f, .72f), Pt(r, .50f, .72f)); g.DrawLine(pen, Pt(r, .24f, .84f), Pt(r, .46f, .84f)); }
            PointerShape(g, r, .00f, .04f, .62f, ink);
            Friend(g, r, .72f, .74f, .15f, ink);
        }

        internal static void Follow(Graphics g, RectangleF r, Color ink)
        {
            using (var pen = Line(r, FriendUI.Blue, .08f)) { pen.DashStyle = DashStyle.Dot; g.DrawLine(pen, Pt(r, .38f, .62f), Pt(r, .56f, .30f)); }
            PointerShape(g, r, .50f, .04f, .62f, ink);
            Friend(g, r, .26f, .74f, .15f, ink);
        }

        internal static void Ignore(Graphics g, RectangleF r, Color ink)
        {
            PointerShape(g, r, .56f, .02f, .52f, ink);
            Friend(g, r, .34f, .62f, .22f, ink);
            using (var pen = Line(r, ink, .07f)) { g.DrawLine(pen, Pt(r, .24f, .58f), Pt(r, .32f, .58f)); g.DrawLine(pen, Pt(r, .38f, .58f), Pt(r, .46f, .58f)); }
        }

        // ───────── 생활 ─────────
        internal static void Snack(Graphics g, RectangleF r, Color ink)
        {
            var b = Box(r, .08f, .08f, .92f, .92f);
            using (var br = new SolidBrush(CookieCol)) g.FillEllipse(br, b);
            using (var pen = Line(r, ink, .07f)) g.DrawEllipse(pen, b);
            Dot(g, r, .34f, .36f, .06f, Choc); Dot(g, r, .64f, .30f, .05f, Choc); Dot(g, r, .60f, .60f, .06f, Choc); Dot(g, r, .34f, .66f, .05f, Choc); Dot(g, r, .78f, .52f, .04f, Choc);
        }

        internal static void FoodClock(Graphics g, RectangleF r, Color ink)
        {
            Snack(g, Box(r, .00f, .30f, .68f, .98f), ink);
            Clock(g, Box(r, .48f, .02f, 1f, .54f), ink);
        }

        internal static void SadFace(Graphics g, RectangleF r, Color ink)
        {
            FaceDisc(g, r, ink);
            Dot(g, r, .35f, .42f, .055f, ink); Dot(g, r, .65f, .42f, .055f, ink);
            using (var pen = Line(r, ink, .08f)) g.DrawArc(pen, Box(r, .32f, .60f, .68f, .88f), 200, 140);
            using (var p = new GraphicsPath())
            {
                p.AddBezier(Pt(r, .76f, .50f), Pt(r, .86f, .62f), Pt(r, .84f, .74f), Pt(r, .76f, .74f));
                p.AddBezier(Pt(r, .76f, .74f), Pt(r, .68f, .74f), Pt(r, .66f, .62f), Pt(r, .76f, .50f));
                using (var br = new SolidBrush(FriendUI.Blue)) g.FillPath(br, p);
            }
        }

        internal static void HeartNumber(Graphics g, RectangleF r, Color ink)
        {
            HeartShape(g, Box(r, .04f, .20f, .60f, .76f), Rose);
            Letter(g, Box(r, .52f, .36f, 1f, .94f), FriendUI.Blue, "#", FontStyle.Bold, .9f);
        }

        // ───────── 숨은 반응 ─────────
        internal static void Dance(Graphics g, RectangleF r, Color ink)
        {
            using (var pen = Line(r, ink, .07f))
                foreach (float cx in new[] { .30f, .70f })
                {
                    g.DrawLine(pen, Pt(r, cx, .44f), Pt(r, cx, .66f));
                    g.DrawLines(pen, new[] { Pt(r, cx - .13f, .30f), Pt(r, cx, .48f), Pt(r, cx + .13f, .30f) });
                    g.DrawLine(pen, Pt(r, cx, .66f), Pt(r, cx - .10f, .90f)); g.DrawLine(pen, Pt(r, cx, .66f), Pt(r, cx + .10f, .90f));
                    Friend(g, r, cx, .30f, .09f, ink);
                }
        }

        internal static void Dizzy(Graphics g, RectangleF r, Color ink)
        {
            var pts = new PointF[48];
            for (int i = 0; i < pts.Length; i++)
            {
                double t = i / (double)(pts.Length - 1), a = t * 5 * Math.PI, rad = .05 + t * .40;
                pts[i] = Pt(r, .50f + (float)(Math.Cos(a) * rad), .50f + (float)(Math.Sin(a) * rad));
            }
            using (var pen = Line(r, FriendUI.Blue, .08f)) g.DrawLines(pen, pts);
        }

        internal static void Goggles(Graphics g, RectangleF r, Color ink)
        {
            using (var pen = Line(r, ink, .08f))
            {
                g.DrawLine(pen, Pt(r, .44f, .52f), Pt(r, .56f, .52f));
                g.DrawLine(pen, Pt(r, .04f, .50f), Pt(r, .00f, .38f)); g.DrawLine(pen, Pt(r, .96f, .50f), Pt(r, 1.00f, .38f));
                foreach (var b in new[] { Box(r, .04f, .30f, .46f, .74f), Box(r, .54f, .30f, .96f, .74f) })
                {
                    using (var br = new SolidBrush(FriendUI.Blue)) g.FillEllipse(br, b);
                    g.DrawEllipse(pen, b);
                    using (var br = new SolidBrush(Color.White)) g.FillEllipse(br, b.X + b.Width * .22f, b.Y + b.Height * .20f, b.Width * .20f, b.Height * .20f);
                }
            }
        }
    }

    /// <summary>켜고 끄는 그림 카드. 켜지면 파란 테두리 · 체크 배지 · 컬러 그림, 꺼지면 흑백으로 흐려진다. 설명은 말풍선에 둔다.</summary>
    internal sealed class ToggleTile : Control
    {
        private readonly FlatIcon.Painter paint; private readonly string caption;
        private bool hot, down, on; private Bitmap dim; private int dimPx;
        internal event EventHandler CheckedChanged;

        internal ToggleTile(FlatIcon.Painter paint, string caption, bool value, int w = 92, int h = 56)
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable | ControlStyles.StandardClick, true);
            this.paint = paint; this.caption = caption; on = value;
            Size = new Size(w, h); Margin = new Padding(0, 0, 6, 0); TabStop = true; Cursor = Cursors.Hand;
            AccessibleName = caption; AccessibleRole = AccessibleRole.CheckButton;
        }

        internal bool Checked
        {
            get { return on; }
            set { if (on == value) return; on = value; Invalidate(); var h = CheckedChanged; if (h != null) h(this, EventArgs.Empty); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; g.Clear(BackColor);
            float s = DeviceDpi / 96f; var card = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = ChoiceStrip.RoundRect(card, (int)Math.Round(8 * s)))
            {
                Color fill = on ? (down ? Color.FromArgb(214, 228, 250) : hot ? Color.FromArgb(222, 234, 255) : Color.FromArgb(232, 240, 255))
                                : (down ? Color.FromArgb(236, 241, 250) : hot ? Color.FromArgb(244, 248, 254) : Color.White);
                using (var br = new SolidBrush(fill)) g.FillPath(br, path);
                using (var pen = new Pen(on ? FriendUI.Blue : hot || Focused ? Color.FromArgb(120, 160, 230) : Color.FromArgb(200, 212, 235), on ? 2f : 1f)) g.DrawPath(pen, path);
            }
            int d = (int)Math.Round(26 * s), ix = (Width - d) / 2, iy = (int)Math.Round(7 * s);
            if (on) paint(g, new RectangleF(ix, iy, d, d), FriendUI.BlueDark);
            else DrawDim(g, ix, iy, d);
            using (var font = new Font("Malgun Gothic", 8.5f))
                TextRenderer.DrawText(g, caption, font, new Rectangle(2, iy + d + (int)Math.Round(2 * s), Width - 4, (int)Math.Round(18 * s)), on ? FriendUI.BlueDark : Color.FromArgb(112, 124, 148),
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
            if (on) ChoiceStrip.DrawBadge(g, card, s);
            if (Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(g, Rectangle.Inflate(card, -3, -3));
            if (!Enabled) using (var veil = new SolidBrush(Color.FromArgb(150, BackColor))) g.FillRectangle(veil, ClientRectangle);
        }

        // 꺼진 그림: 한 번 구워 두었다가 흑백 · 반투명으로 그린다.
        private void DrawDim(Graphics g, int x, int y, int d)
        {
            if (dim == null || dimPx != d)
            {
                if (dim != null) dim.Dispose();
                dim = new Bitmap(d, d);
                using (var gg = Graphics.FromImage(dim)) { gg.SmoothingMode = SmoothingMode.AntiAlias; paint(gg, new RectangleF(0, 0, d, d), FriendUI.BlueDark); }
                dimPx = d;
            }
            var cm = new ColorMatrix(new float[][]
            {
                new float[] { 0.299f, 0.299f, 0.299f, 0, 0 }, new float[] { 0.587f, 0.587f, 0.587f, 0, 0 }, new float[] { 0.114f, 0.114f, 0.114f, 0, 0 },
                new float[] { 0, 0, 0, 0.75f, 0 }, new float[] { 0, 0, 0, 0, 1 }
            });
            using (var ia = new ImageAttributes()) { ia.SetColorMatrix(cm); g.DrawImage(dim, new Rectangle(x, y, d, d), 0, 0, d, d, GraphicsUnit.Pixel, ia); }
        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); hot = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hot = down = false; Invalidate(); }
        protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); if (e.Button == MouseButtons.Left) { Focus(); down = true; Invalidate(); } }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (down) { down = false; Invalidate(); if (ClientRectangle.Contains(e.Location)) Checked = !on; }
        }
        protected override void OnKeyDown(KeyEventArgs e) { base.OnKeyDown(e); if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter) { Checked = !on; e.Handled = true; } }
        protected override bool IsInputKey(Keys keyData) { return (keyData & Keys.KeyCode) == Keys.Space || base.IsInputKey(keyData); }
        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
        protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); Invalidate(); }
        protected override void Dispose(bool disposing) { if (disposing && dim != null) { dim.Dispose(); dim = null; } base.Dispose(disposing); }
    }

    /// <summary>줄 맨 앞에 붙는 동그란 머리표(그림). 이름은 말풍선으로만 알려 준다.</summary>
    internal sealed class RailIcon : Control
    {
        private readonly FlatIcon.Painter paint;
        internal RailIcon(FlatIcon.Painter paint, int size = 36)
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            this.paint = paint; Size = new Size(size, size); TabStop = false;
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; g.Clear(BackColor);
            using (var br = new SolidBrush(Color.FromArgb(225, 235, 252))) g.FillEllipse(br, 0, 0, Width - 1, Height - 1);
            float d = Width * .56f;
            paint(g, new RectangleF((Width - d) / 2f, (Height - d) / 2f, d, d), FriendUI.BlueDark);
        }
    }

    /// <summary>하얀 둥근 카드 바탕(안에 다른 컨트롤을 올린다).</summary>
    internal sealed class RoundCard : Panel
    {
        internal RoundCard(int w, int h)
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            Size = new Size(w, h); BackColor = Color.White;
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Parent != null ? Parent.BackColor : FriendUI.Sheet);
            float s = DeviceDpi / 96f;
            using (var path = ChoiceStrip.RoundRect(new Rectangle(0, 0, Width - 1, Height - 1), (int)Math.Round(9 * s)))
            {
                using (var br = new SolidBrush(Color.White)) g.FillPath(br, path);
                using (var pen = new Pen(Color.FromArgb(206, 218, 240), 1f)) g.DrawPath(pen, path);
            }
        }
    }

    /// <summary>0~100 막대(포만감). 둥근 막대 안을 노랑으로 채운다.</summary>
    internal sealed class MeterBar : Control
    {
        private int level;
        internal MeterBar() { SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true); Size = new Size(160, 14); TabStop = false; }
        internal int Value { get { return level; } set { level = Math.Max(0, Math.Min(100, value)); Invalidate(); } }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; g.Clear(BackColor);
            var rc = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = ChoiceStrip.RoundRect(rc, Math.Max(2, rc.Height / 2)))
            {
                using (var br = new SolidBrush(Color.FromArgb(222, 230, 244))) g.FillPath(br, path);
                g.SetClip(path, CombineMode.Intersect);
                using (var br = new SolidBrush(FriendUI.Gold)) g.FillRectangle(br, rc.X, rc.Y, (int)Math.Round(rc.Width * level / 100.0), rc.Height + 1);
                g.ResetClip();
                using (var pen = new Pen(Color.FromArgb(190, 204, 232), 1f)) g.DrawPath(pen, path);
            }
        }
    }

    /// <summary>찾은 숨은 반응을 보여 주는 동그란 도감 칸. 못 찾았으면 회색 물음표.</summary>
    internal sealed class SecretBadge : Control
    {
        private readonly FlatIcon.Painter paint; private bool found;
        internal SecretBadge(FlatIcon.Painter paint, int size = 40)
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            this.paint = paint; Size = new Size(size, size); TabStop = false;
        }
        internal bool Found { get { return found; } set { if (found == value) return; found = value; Invalidate(); } }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; g.Clear(BackColor);
            var rc = new Rectangle(1, 1, Width - 3, Height - 3);
            using (var br = new SolidBrush(found ? Color.FromArgb(255, 246, 208) : Color.FromArgb(234, 238, 246))) g.FillEllipse(br, rc);
            using (var pen = new Pen(found ? FriendUI.Gold : Color.FromArgb(206, 214, 228), found ? 2f : 1f)) g.DrawEllipse(pen, rc);
            float d = Width * .56f; var ir = new RectangleF((Width - d) / 2f, (Height - d) / 2f, d, d);
            if (found) paint(g, ir, FriendUI.BlueDark);
            else using (var font = new Font("Malgun Gothic", Width * .30f, FontStyle.Bold, GraphicsUnit.Pixel))
                TextRenderer.DrawText(g, "?", font, rc, Color.FromArgb(160, 170, 190), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
        }
    }
}
