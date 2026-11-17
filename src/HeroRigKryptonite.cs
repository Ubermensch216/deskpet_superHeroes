using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace SuperHeroes
{
    /// <summary>수퍼맨의 크립토나이트 반응: 바닥의 초록 돌·빛, 창백한 초록 안색, 핏줄·식은땀·찡그린 눈.</summary>
    internal sealed partial class HeroPainter
    {
        private bool SickFace { get { return who == Persona.Superman && !reporter && p.Sick > .01f; } }

        /// <summary>힘이 빠질수록 피부는 창백한 초록빛, 수트와 망토는 빛이 바랜다.</summary>
        private void ApplyKryptoTint()
        {
            if (!SickFace) return;
            float s = Math.Min(1f, p.Sick);
            cSkin = Mix(cSkin, Color.FromArgb(186, 220, 150), s * .78f);
            cSkinSh = Mix(cSkinSh, Color.FromArgb(146, 186, 118), s * .78f);
            cGloves = cSkin;
            cSuit = Mix(cSuit, Color.FromArgb(72, 104, 122), s * .30f);
            cSuitSh = Mix(cSuitSh, Color.FromArgb(50, 76, 96), s * .30f);
            cCape = Mix(cCape, Color.FromArgb(124, 52, 62), s * .32f);
            cCapeInner = Mix(cCapeInner, Color.FromArgb(94, 40, 50), s * .32f);
        }

        /// <summary>얼굴에 번지는 초록 핏줄(영화 속 크립토나이트 중독 표현).</summary>
        private void DrawKryptoVeins(float rx, float ry)
        {
            if (!SickFace || p.Sick < .25f) return;
            float v = Math.Min(1f, (p.Sick - .25f) / .45f);
            using (var pn = new Pen(Color.FromArgb((int)(190 * v), 38, 128, 52), 2.6f) { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                foreach (float sd in new[] { -1f, 1f })
                {
                    g.DrawLines(pn, new[] { Pt(sd * rx * .56f, ry * .02f), Pt(sd * rx * .44f, ry * .26f), Pt(sd * rx * .52f, ry * .46f), Pt(sd * rx * .40f, ry * .62f) });
                    g.DrawLines(pn, new[] { Pt(sd * rx * .44f, ry * .26f), Pt(sd * rx * .24f, ry * .34f) });
                    g.DrawLines(pn, new[] { Pt(sd * rx * .52f, ry * .46f), Pt(sd * rx * .70f, ry * .52f) });
                }
                g.DrawLines(pn, new[] { Pt(-rx * .14f, -ry * .56f), Pt(-rx * .06f, -ry * .40f), Pt(-rx * .20f, -ry * .28f) });
            }
        }

        /// <summary>관자놀이에서 흘러내리는 식은땀 방울.</summary>
        private void DrawKryptoSweat(float rx, float ry)
        {
            if (!SickFace || p.Sick < .2f) return;
            float strength = Math.Min(1f, p.Sick * 3f);
            float[] xs = { -.92f, .90f, -.60f, .66f };
            for (int i = 0; i < xs.Length; i++)
            {
                float ph = (p.KryptoT * 1.25f + i * .31f) % 1f;
                float x = rx * xs[i], y = -ry * .42f + ph * ry * 1.25f;
                var drop = new[] { Pt(x, y - 12), Pt(x - 7, y + 1), Pt(x - 3, y + 8), Pt(x + 3, y + 8), Pt(x + 7, y + 1) };
                int al = (int)(235 * strength * (1 - ph * .55f));
                g.FillPolygon(B(Color.FromArgb(al, 170, 224, 255)), drop);
                using (var pn = new Pen(Color.FromArgb(al, 60, 110, 160), 1.8f) { LineJoin = LineJoin.Round }) g.DrawPolygon(pn, drop);
            }
        }

        /// <summary>눈을 질끈 감아 찌푸린 '> <' 모양과 걱정스레 처진 눈썹. 그렸으면 true.</summary>
        private bool DrawSickEyes(float eyeSep, float eyeY)
        {
            float worry = Math.Min(1f, p.Sick * 1.6f);
            bool squint = p.Sick > .32f;
            if (squint)
            {
                using (var pn = Pn(cInk, 4.8f))
                {
                    g.DrawLines(pn, new[] { Pt(-eyeSep - 14, eyeY - 9), Pt(-eyeSep + 8, eyeY + 1), Pt(-eyeSep - 14, eyeY + 11) });
                    g.DrawLines(pn, new[] { Pt(eyeSep + 14, eyeY - 9), Pt(eyeSep - 8, eyeY + 1), Pt(eyeSep + 14, eyeY + 11) });
                }
            }
            using (var pn = Pn(cInk, 4.4f))
            {
                float browY = eyeY - 13;
                // 바깥쪽은 처지고 안쪽은 올라간 ‘八’자 눈썹
                g.DrawLine(pn, -eyeSep - 15, browY + 2 + 5 * worry, -eyeSep + 12, browY + 2 - 11 * worry);
                g.DrawLine(pn, eyeSep + 15, browY + 2 + 5 * worry, eyeSep - 12, browY + 2 - 11 * worry);
            }
            return squint;
        }

        /// <summary>바닥에 놓인 크립토나이트 조각. 초록 빛무리·수퍼맨 쪽으로 뻗는 빛줄기·떠오르는 반짝임.</summary>
        private void DrawKryptoniteFx()
        {
            float a = Math.Min(1f, p.Krypto), t = p.KryptoT;
            if (a < .01f) return;
            float dir = faceRight ? 1f : -1f;
            float cx = 200 + dir * 152f, gy = 571f;
            float pulse = .5f + .5f * (float)Math.Sin(t * 8.5);
            float pop = Math.Min(1f, t / .5f);
            float lift = pop < 1f ? 74f * (float)Math.Sin(Math.PI * pop) : 0f;        // 톡 튀어 올랐다 떨어진다
            float sc = (.3f + .7f * a) * (1f + .1f * (float)Math.Sin(Math.PI * pop));
            PointF core = Pt(cx, gy - 38 - lift * .6f);

            // 바닥에 퍼지는 빛무리
            using (var gp = new GraphicsPath())
            {
                gp.AddEllipse(cx - 118, gy - 30, 236, 60);
                using (var pg = new PathGradientBrush(gp))
                {
                    pg.CenterColor = Color.FromArgb((int)(170 * a * (.72f + .28f * pulse)), 120, 255, 96);
                    pg.SurroundColors = new[] { Color.FromArgb(0, 90, 255, 80) };
                    g.FillPath(pg, gp);
                }
            }
            // 돌 주위의 둥근 후광
            using (var gp = new GraphicsPath())
            {
                float r = 88f * (.8f + .2f * pulse) * (.4f + .6f * a);
                gp.AddEllipse(core.X - r, core.Y - r, r * 2, r * 2);
                using (var pg = new PathGradientBrush(gp))
                {
                    pg.CenterColor = Color.FromArgb((int)(150 * a), 150, 255, 120);
                    pg.SurroundColors = new[] { Color.FromArgb(0, 70, 230, 70) };
                    g.FillPath(pg, gp);
                }
            }

            // 수퍼맨의 가슴 쪽으로 뻗는 초록 빛줄기(가슴이 점점 약해지는 느낌)
            PointF target = Pt(200, 372);
            float dx = target.X - core.X, dy = target.Y - core.Y, len = (float)Math.Sqrt(dx * dx + dy * dy);
            if (len > 1f)
            {
                float nx = -dy / len, ny = dx / len;
                float[] widths = { 70f, 42f, 18f };
                for (int i = 0; i < widths.Length; i++)
                {
                    float wd = widths[i] * (.78f + .22f * (float)Math.Sin(t * 7 + i * 1.7f));
                    var wedge = new[] { core, Pt(target.X + nx * wd, target.Y + ny * wd), Pt(target.X - nx * wd, target.Y - ny * wd) };
                    using (var br = new LinearGradientBrush(core, target, Color.FromArgb((int)((86 - i * 12) * a), 130, 255, 100), Color.FromArgb(0, 80, 230, 80)))
                        g.FillPolygon(br, wedge);
                }
            }

            // 떠오르는 반짝임
            for (int i = 0; i < 10; i++)
            {
                float ph = (t * .85f + i * .137f) % 1f;
                float x = cx + (float)Math.Sin(i * 5.3f + t * 1.8f) * (22f + i * 3f);
                float y = gy - 14 - ph * 150f;
                float r = (6f - 4f * ph) * sc;
                int al = (int)(220 * (1 - ph) * a);
                var spark = new[] { Pt(x, y - r * 1.5f), Pt(x + r, y), Pt(x, y + r * 1.5f), Pt(x - r, y) };
                g.FillPolygon(B(Color.FromArgb(al, 190, 255, 160)), spark);
            }

            // 등장 순간의 번쩍임
            if (t < .4f)
            {
                float f = 1f - t / .4f, r = 80f * (1f - f * f) + 18f;
                using (var gp = new GraphicsPath())
                {
                    gp.AddEllipse(core.X - r, core.Y - r, r * 2, r * 2);
                    using (var pg = new PathGradientBrush(gp))
                    {
                        pg.CenterColor = Color.FromArgb((int)(210 * f), 235, 255, 210);
                        pg.SurroundColors = new[] { Color.FromArgb(0, 150, 255, 120) };
                        g.FillPath(pg, gp);
                    }
                }
            }

            // 돌 자체
            var st = g.Save();
            g.TranslateTransform(cx, gy - lift);
            g.ScaleTransform(sc, sc);
            var main = new[] { Pt(-17, 0), Pt(-24, -30), Pt(-9, -62), Pt(7, -76), Pt(20, -46), Pt(26, 0) };
            var left = new[] { Pt(-34, 0), Pt(-38, -18), Pt(-26, -34), Pt(-19, -14), Pt(-17, 0) };
            var right = new[] { Pt(26, 0), Pt(23, -22), Pt(36, -36), Pt(44, -12), Pt(40, 0) };
            Color dark = Color.FromArgb(34, 142, 52), mid = Color.FromArgb(76, 214, 84), light = Color.FromArgb(200, 255, 170);
            Color mixTint = Mix(mid, light, .35f * pulse);
            g.FillPolygon(B(dark), left); g.DrawPolygon(ink, left);
            g.FillPolygon(B(dark), right); g.DrawPolygon(ink, right);
            g.FillPolygon(B(mixTint), main); g.DrawPolygon(ink, main);
            // 면의 밝은 쪽과 날카로운 반사
            var facet = new[] { Pt(-9, -62), Pt(7, -76), Pt(2, -30), Pt(-12, -8), Pt(-20, -28) };
            g.FillPolygon(B(Color.FromArgb(150 + (int)(70 * pulse), 220, 255, 190)), facet);
            g.DrawLine(ink, Pt(7, -76), Pt(2, -30));
            g.DrawLine(ink, Pt(-12, -8), Pt(2, -30));
            g.FillEllipse(B(Color.FromArgb(230, 255, 255, 235)), -8, -58, 7, 11);
            g.Restore(st);
        }
    }
}
