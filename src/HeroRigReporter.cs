using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace SuperHeroes
{
    /// <summary>
    /// 수퍼맨의 인간 모습: 데일리 플래닛 기자 클라크 켄트.
    /// 고증: 차분한 네이비/차콜 정장 + 흰 드레스셔츠 + 붉은 넥타이 + 검은 뿔테 안경 + 단정한 가르마 머리(+ 기자증).
    /// 셔츠를 좌우로 찢어 젖히면 안쪽의 수퍼맨 수트와 S 심볼이 드러난다(Pose.ShirtOpen).
    /// </summary>
    internal sealed partial class HeroPainter
    {
        private bool reporter;   // 지금 기자 차림으로 그리는 중 (Pose.Disguise > .5, 수퍼맨 전용)

        private static readonly Color cJacket = Color.FromArgb(54, 64, 94);      // 네이비 정장
        private static readonly Color cJacketSh = Color.FromArgb(36, 44, 68);
        private static readonly Color cShoes = Color.FromArgb(30, 28, 34);
        private static readonly Color cTie = Color.FromArgb(196, 32, 44);         // 붉은 넥타이
        private static readonly Color cTieSh = Color.FromArgb(150, 22, 32);
        private static readonly Color cHeroBlue = Color.FromArgb(16, 92, 184);    // 셔츠 속 수퍼맨 수트

        /// <summary>정장 상의: 흰 셔츠, 라펠, 넥타이, 기자증 — 셔츠를 찢으면 안쪽 수트와 S 심볼이 보인다.</summary>
        private void DrawReporterChest(float cx, float y0)
        {
            float open = Math.Max(0, Math.Min(1, p.ShirtOpen));
            float y1 = waistY + p.BodyY;
            float gw = 2 + 36 * open;   // 가운데 벌어진 틈의 반폭

            // 가운데: 닫혀 있으면 셔츠, 찢으면 수퍼맨 수트 + S 심볼
            var gap = new[] { Pt(cx - gw, y0), Pt(cx + gw, y0), Pt(cx + gw, y1), Pt(cx - gw, y1) };
            if (open < .04f)
            {
                g.FillPolygon(B(cWhite), gap);
            }
            else
            {
                g.FillPolygon(B(cHeroBlue), gap);
                var st = g.Save();
                g.SetClip(new RectangleF(cx - gw, y0, gw * 2, y1 - y0), CombineMode.Intersect);
                DrawSupermanEmblem(cx, y0);
                g.Restore(st);
                g.DrawPolygon(ink, gap);
            }

            // 양쪽: 셔츠 깃 + 정장 라펠
            for (int sgn = -1; sgn <= 1; sgn += 2)
            {
                float s = sgn;
                var shirt = new[] { Pt(cx + s * gw, y0), Pt(cx + s * (gw + 22), y0), Pt(cx + s * (gw + 6), y0 + 66), Pt(cx + s * (gw + 3), y1), Pt(cx + s * gw, y1) };
                g.FillPolygon(B(cWhite), shirt);
                g.DrawPolygon(ink, shirt);
                var lapel = new[] { Pt(cx + s * (gw + 22), y0), Pt(cx + s * (gw + 6), y0 + 66), Pt(cx + s * (gw + 24), y0 + 54), Pt(cx + s * (gw + 34), y0 + 6) };
                g.FillPolygon(B(cJacketSh), lapel);
                g.DrawPolygon(ink, lapel);
            }

            // 단추(닫혀 있을 때만)
            if (open < .25f)
            {
                float a = 1 - open / .25f;
                g.FillEllipse(B(Al(cJacketSh, 255 * a)), cx + 10, y0 + 84, 7, 7);
                g.FillEllipse(B(Al(cJacketSh, 255 * a)), cx + 10, y0 + 104, 7, 7);
            }

            // 왼쪽 가슴 주머니 + 기자증(PRESS)
            float px = cx - (gw + 30), py = y0 + 70;
            var pocket = new[] { Pt(px - 12, py), Pt(px + 12, py), Pt(px + 12, py + 22), Pt(px - 12, py + 22) };
            g.FillPolygon(B(cJacketSh), pocket);
            g.DrawPolygon(ink, pocket);
            var card = new RectangleF(px - 8, py - 12, 16, 22);
            g.FillRectangle(B(cWhite), card);
            g.FillRectangle(B(Color.FromArgb(214, 40, 48)), card.X, card.Y, card.Width, 6);   // 빨간 PRESS 띠
            using (var pn = Pn(Color.FromArgb(90, 96, 110), 1.6f))
            {
                g.DrawLine(pn, card.X + 3, card.Y + 11, card.Right - 3, card.Y + 11);
                g.DrawLine(pn, card.X + 3, card.Y + 15, card.Right - 5, card.Y + 15);
            }
            g.DrawRectangle(ink, card.X, card.Y, card.Width, card.Height);
            g.FillPolygon(B(cJacketSh), pocket);   // 주머니 앞면이 기자증 아랫부분을 가린다
            g.DrawPolygon(ink, pocket);

            // 넥타이: 찢는 순간 휙 날아간다
            if (open < .4f)
            {
                float fly = open / .4f;
                var st = g.Save();
                g.TranslateTransform(cx + fly * 70, y0 + 6 - fly * 40);
                g.RotateTransform(fly * 55);
                int al = (int)(255 * (1 - fly));
                var knot = new[] { Pt(-7, -4), Pt(7, -4), Pt(5, 10), Pt(-5, 10) };
                var blade = new[] { Pt(-5, 10), Pt(5, 10), Pt(11, 82), Pt(0, 96), Pt(-11, 82) };
                using (var pn = new Pen(Color.FromArgb(al, cInk.R, cInk.G, cInk.B), 3f) { LineJoin = LineJoin.Round })
                {
                    g.FillPolygon(B(Al(cTie, al)), blade); g.DrawPolygon(pn, blade);
                    g.FillPolygon(B(Al(cTieSh, al)), knot); g.DrawPolygon(pn, knot);
                    g.DrawLine(pn, -4, 38, 5, 36); g.DrawLine(pn, -6, 56, 7, 54);   // 사선 줄무늬 느낌
                }
                g.Restore(st);
            }
        }

        /// <summary>목 둘레 흰 셔츠 칼라와 넥타이 매듭 (머리 좌표계).</summary>
        private void DrawReporterCollar()
        {
            var left = new[] { Pt(-26, 80), Pt(-2, 124), Pt(-40, 114) };
            var right = new[] { Pt(26, 80), Pt(2, 124), Pt(40, 114) };
            g.FillPolygon(B(cWhite), left); g.DrawPolygon(ink, left);
            g.FillPolygon(B(cWhite), right); g.DrawPolygon(ink, right);
            if (p.ShirtOpen < .1f)
            {
                var knot = new[] { Pt(-8, 112), Pt(8, 112), Pt(6, 130), Pt(-6, 130) };
                g.FillPolygon(B(cTieSh), knot); g.DrawPolygon(ink, knot);
            }
        }

        /// <summary>검은 뿔테 안경 (머리 좌표계). 눈 위치는 DrawEyesAndEyebrows 와 같다.</summary>
        private void DrawReporterGlasses(float rx)
        {
            float eyeY = -headRy * .05f + 1, sep = 26f, w = 44f, h = 34f;
            Color rim = Color.FromArgb(22, 22, 28);
            for (int sgn = -1; sgn <= 1; sgn += 2)
            {
                var r = new RectangleF(sgn * sep - w / 2, eyeY - h / 2, w, h);
                using (var path = RoundRect(r, 9))
                {
                    g.FillPath(B(Color.FromArgb(46, 190, 225, 255)), path);          // 옅은 렌즈
                    using (var pn = Pn(rim, 6.5f)) g.DrawPath(pn, path);              // 굵은 검정 테
                    using (var hl = Pn(Color.FromArgb(150, 255, 255, 255), 2.2f))      // 반사광
                        g.DrawLine(hl, r.X + 9, r.Y + 9, r.X + 17, r.Y + 5);
                }
                // 안경다리 → 귀
                using (var pn = Pn(rim, 4.5f)) g.DrawLine(pn, sgn * (sep + w / 2), eyeY - 2, sgn * (rx + 4), eyeY - 2);
            }
            using (var pn = Pn(rim, 5f)) g.DrawLine(pn, -sep + w / 2, eyeY - 3, sep - w / 2, eyeY - 3);   // 콧대 브리지
        }

        private static GraphicsPath RoundRect(RectangleF r, float rad)
        {
            float d = rad * 2;
            var path = new GraphicsPath();
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseAllFigures();
            return path;
        }

        /// <summary>정장 바지 윗단과 검정 가죽 벨트.</summary>
        private void DrawReporterBelt(float cx, float wy, float hy)
        {
            var trousers = new[] { Pt(cx - botW * .5f, wy), Pt(cx - botW * .52f, hy + 18), Pt(cx, hy + 28), Pt(cx + botW * .52f, hy + 18), Pt(cx + botW * .5f, wy) };
            Fill(trousers, cSuit);
            var beltRect = new RectangleF(cx - botW * .5f, wy - 2, botW, 12);
            g.FillRectangle(B(Color.FromArgb(34, 30, 30)), beltRect);
            g.DrawRectangle(ink, beltRect.X, beltRect.Y, beltRect.Width, beltRect.Height);
            g.FillRectangle(B(Color.FromArgb(200, 200, 206)), cx - 8, wy - 3, 16, 14);   // 은색 버클
            g.DrawRectangle(ink, cx - 8, wy - 3, 16, 14);
        }

        /// <summary>와이셔츠 소매 끝동.</summary>
        private void DrawReporterCuff(PointF wr)
        {
            var cuff = new[] { Pt(wr.X - armW * .36f, wr.Y - 10), Pt(wr.X + armW * .36f, wr.Y - 10), Pt(wr.X + armW * .34f, wr.Y + 2), Pt(wr.X - armW * .34f, wr.Y + 2) };
            Fill(cuff, cWhite);
        }

        /// <summary>변신 순간의 하얀 섬광.</summary>
        private void DrawTransformBurst()
        {
            float t = Math.Max(0, Math.Min(1, p.Transform));
            float cx = 200 + p.BodyX, cy = shY + 70 + p.BodyY, r = 60 + 170 * t;
            using (var path = new GraphicsPath())
            {
                path.AddEllipse(cx - r, cy - r, r * 2, r * 2);
                using (var br = new PathGradientBrush(path))
                {
                    br.CenterColor = Color.FromArgb((int)(235 * t), 255, 252, 225);
                    br.SurroundColors = new[] { Color.FromArgb(0, 255, 230, 120) };
                    g.FillPath(br, path);
                }
            }
            using (var pn = Pn(Color.FromArgb((int)(220 * t), 255, 255, 255), 4f))
                for (int i = 0; i < 12; i++)
                {
                    double ang = i * Math.PI / 6.0;
                    g.DrawLine(pn, cx + (float)Math.Cos(ang) * r * .35f, cy + (float)Math.Sin(ang) * r * .35f, cx + (float)Math.Cos(ang) * r, cy + (float)Math.Sin(ang) * r);
                }
        }
    }
}
