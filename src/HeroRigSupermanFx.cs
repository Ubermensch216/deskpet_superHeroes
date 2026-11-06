using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace SuperHeroes
{
    // 수퍼맨 시그니처 연출: 히트 비전 · 냉기 입김 · 초음속 펀치 · 셔츠 찢기(S 각성)
    internal sealed partial class HeroPainter
    {
        // ───────── 히트 비전 ─────────
        // 눈이 붉게 달아오르고(기를 모으는 불씨) → 두 줄기 열선이 땅을 훑으며 녹은 고랑을 파고 → 불꽃·연기·불똥이 치솟는다.

        private void DrawHeatVision()
        {
            float T = p.SigT, a = Math.Min(1f, p.HeatLaser), bx = p.BodyX, by = p.BodyY;
            int frame = (int)(T * 30);
            float ch = Math.Min(1f, T / 1.5f);
            PointF e1 = Pt(200 - 24 + bx, headCy - 8 + by), e2 = Pt(200 + 24 + bx, headCy - 8 + by);
            Color hot = Color.FromArgb(255, 70, 30), orange = Color.FromArgb(255, 150, 40), yel = Color.FromArgb(255, 236, 150);
            bool beam = T >= 1.5f && a > .05f;
            float eg = beam ? a : ch * .85f;
            float flick = .88f + .12f * (float)Math.Sin(T * 70);

            // 1. 눈빛: 붉게 달아오르다 흰빛으로 터진다 + 얼굴이 붉게 물든다
            Glow(200 + bx, headCy + by, 80, 80, Al(hot, 70 * eg));
            foreach (PointF e in new[] { e1, e2 })
            {
                Glow(e.X, e.Y, 18 + 28 * eg, 18 + 28 * eg, Al(hot, 200 * eg));
                Glow(e.X, e.Y, 8 + 12 * eg, 8 + 12 * eg, Al(Color.White, 245 * eg));
                Glint(e.X, e.Y, 14 + 40 * eg, yel, eg * flick, T * 40);
            }

            // 2. 충전: 불씨가 눈으로 빨려든다
            if (T < 1.5f)
            {
                int n = 8 + (int)(16 * ch);
                for (int i = 0; i < n; i++)
                {
                    float ph = (T * 1.6f + i / (float)n) % 1f, ang = i * 2.39996f + T * 3f, r = 14 + 130 * (1 - ph) * (1 - ph);
                    PointF c = i % 2 == 0 ? e1 : e2;
                    var s = new[] { PtC(c.X + (float)Math.Cos(ang) * r, c.Y + (float)Math.Sin(ang) * r * .8f), PtC(c.X + (float)Math.Cos(ang) * r * .82f, c.Y + (float)Math.Sin(ang) * r * .82f * .8f) };
                    Stroke(s, Al(orange, 220 * ph * ch), 4f);
                    Stroke(s, Al(Color.White, 255 * ph * ch), 1.8f);
                }
            }

            // 3. 열선: 땅 위를 앞뒤로 훑는다(1.5~5초에 두 번 왕복)
            Func<float, PointF> tgt = tt =>
            {
                float s = Math.Max(0f, Math.Min(1f, (tt - 1.5f) / 3.5f));
                return Pt(15 + 55 * (float)Math.Cos(2 * Math.PI * s), 566f);
            };
            float trenchFade = Ease01((6.4f - T) / .9f);
            if (T >= 1.6f)
            {
                // 녹아내린 고랑: 하얗게 달궈졌다가 붉게 식는다
                PointF prev = tgt(T);
                for (int j = 1; j < 34; j++)
                {
                    float tj = T - j * .07f; if (tj < 1.5f) break;
                    float heat = 1f - j * .07f / 2.2f; if (heat <= 0f) break;
                    PointF cur = tgt(tj);
                    var seg = new[] { prev, cur };
                    Color col = Mix(Color.FromArgb(150, 30, 10), Color.FromArgb(255, 236, 170), heat * heat);
                    Stroke(seg, Al(hot, 110 * heat * trenchFade), 20f);
                    Stroke(seg, Al(col, 255 * heat * trenchFade), 9f * (.5f + .5f * heat));
                    prev = cur;
                }
            }
            if (beam)
            {
                PointF I = tgt(T);
                int[] sd = { 11, 23 };
                for (int k2 = 0; k2 < 2; k2++)
                {
                    PointF e = k2 == 0 ? e1 : e2;
                    PointF[] path = BoltPath(e, PtC(I.X + (k2 == 0 ? -6 : 6), I.Y), frame * 5 + sd[k2], 5, 9);
                    Stroke(path, Al(hot, 95 * a * flick), 40f * flick);
                    Stroke(path, Al(orange, 175 * a), 22f);
                    Stroke(path, Al(yel, 245 * a), 11f);
                    Stroke(path, Al(Color.White, 255 * a), 4.4f);
                }
                // 충돌점: 눈부신 불덩이 + 퍼지는 아지랑이 고리
                Glow(I.X, I.Y - 6, 120, 64, Al(orange, 210 * a));
                Glow(I.X, I.Y - 4, 54, 30, Al(Color.White, 255 * a));
                float ph0 = (T * 1.7f) % 1f;
                GroundRing(I.X, I.Y, 20 + 90 * ph0, .28f, orange, (1 - ph0) * a, 2.4f);

                // 불똥: 포물선으로 튀어 오른다
                for (int i = 0; i < 22; i++)
                {
                    float ph = (T * 2.4f + i * .0455f) % 1f;
                    float vx = (Hash01(i, 3) * 2 - 1) * 200f, vy = -(150f + Hash01(i, 5) * 260f);
                    PointF s0 = Pt(I.X + vx * ph * .7f, I.Y - 4 + vy * ph + 620f * ph * ph * .55f);
                    PointF s1 = Pt(s0.X - vx * .035f, s0.Y - (vy + 620f * ph * 1.1f) * .035f);
                    if (s0.Y > I.Y + 2) continue;
                    Stroke(new[] { PtC(s1.X, s1.Y), PtC(s0.X, s0.Y) }, Al(Hash01(i, 9) > .5f ? yel : orange, 255 * (1 - ph) * a), 2.6f);
                }
                // 불꽃 혀: 일렁이며 솟는다
                for (int i = 0; i < 7; i++)
                {
                    float fx = I.X + (i - 3) * 14, h = (26 + 40 * Hash01(i, 11)) * (.65f + .35f * (float)Math.Sin(T * 18 + i * 1.7f)) * a;
                    var fl = new[] { Pt(fx - 9, I.Y), Pt(fx - 4, I.Y - h * .55f), Pt(fx + 2 * (float)Math.Sin(T * 14 + i), I.Y - h), Pt(fx + 5, I.Y - h * .5f), Pt(fx + 9, I.Y) };
                    g.FillPolygon(B(Al(hot, 200)), PtArr(fl));
                    var fi = new[] { Pt(fx - 4, I.Y), Pt(fx, I.Y - h * .6f), Pt(fx + 4, I.Y) };
                    g.FillPolygon(B(Al(yel, 230)), fi);
                }
            }
            // 연기: 식은 뒤에도 한동안 피어오른다
            if (T >= 1.7f && trenchFade > .02f)
            {
                PointF I = tgt(Math.Min(T, 5.0f));
                for (int i = 0; i < 9; i++)
                {
                    float ph = (T * .55f + i * .111f) % 1f;
                    float sx = I.X + 40 + (float)Math.Sin(i * 2.3f + ph * 3f) * 24 + ph * 50, sy = 556 - ph * 170, r = 14 + 34 * ph;
                    g.FillEllipse(B(Al(Color.FromArgb(70, 62, 66), 120 * (1 - ph) * trenchFade)), CxClamp(sx) - r, sy - r * .8f, r * 2, r * 1.6f);
                }
            }
        }

        private static PointF[] PtArr(PointF[] a) { return a; }

        // ───────── 냉기 입김 ─────────
        // 숨을 깊이 들이마시자 서리가 빨려들고 → 입김이 눈보라처럼 뿜어져 나가며 → 바닥에 얼음 결정이 줄줄이 돋아난다.

        private void DrawFreezeBreath()
        {
            float T = p.SigT, a = Math.Min(1f, p.FreezeMist), bx = p.BodyX, by = p.BodyY;
            float ch = Math.Min(1f, T / 1.8f);
            PointF m = Pt(200 - 18 + bx, headCy + 24 + by);
            Color ice = Color.FromArgb(150, 215, 255), snow = Color.FromArgb(236, 248, 255), deep = Color.FromArgb(90, 160, 225);
            const float gy = 568f;

            // 1. 들이마시기: 입으로 빨려드는 서리와 눈송이
            if (T < 1.9f)
            {
                Glow(m.X, m.Y, 22 + 34 * ch, 22 + 34 * ch, Al(ice, 130 * ch));
                int n = 8 + (int)(14 * ch);
                for (int i = 0; i < n; i++)
                {
                    float ph = (T * 1.4f + i / (float)n) % 1f, ang = i * 2.39996f, r = 16 + 150 * (1 - ph) * (1 - ph);
                    Snowflake(m.X + (float)Math.Cos(ang) * r, m.Y + (float)Math.Sin(ang) * r * .7f, 3 + 4 * ph, T * 120 + i * 40, snow, ph * ch);
                }
                float rp = (T * 2f) % 1f;
                g.DrawEllipse(Pn(Al(ice, 160 * (1 - rp) * ch), 3f), m.X - 70 * (1 - rp), m.Y - 50 * (1 - rp), 140 * (1 - rp), 100 * (1 - rp));
            }

            float bt = T - 1.8f;
            if (a <= .03f || bt < 0f) return;

            // 2. 얼어붙는 바닥: 서리 막 + 줄지어 돋는 얼음 결정(멀리 있는 것일수록 크고 늦게 자란다)
            float frostReach = Math.Min(1f, bt / .9f);
            float fa = Ease01(a * 1.6f);
            float fx1 = m.X - 215 * frostReach;
            g.FillEllipse(B(Al(snow, 140 * fa)), fx1, gy - 9, m.X + 18 - fx1, 20);
            Glow((fx1 + m.X) / 2, gy - 6, (m.X - fx1) / 2 + 20, 26, Al(ice, 90 * fa));
            for (int i = 0; i < 10; i++)
            {
                float x = 168 - i * 22 + (Hash01(i, 2) - .5f) * 8 + bx * .3f;
                float ts = .1f + i * .09f, grow = Ease01((bt - ts) / .4f) * fa;
                if (grow <= .01f) continue;
                float h = (34 + (i % 3) * 14 + i * 5.5f) * grow, w = 7 + 7 * Hash01(i, 6) + i * .5f;
                for (int s = -1; s <= 1; s++)
                {
                    float sx = x + s * (w * .85f), sh = h * (s == 0 ? 1f : .55f + .2f * Hash01(i, s + 9)), lean = s * 5 + (Hash01(i, 4) - .5f) * 8;
                    var crystal = new[] { Pt(sx - w * .55f, gy), Pt(sx - w * .22f + lean * .3f, gy - sh * .6f), Pt(sx + lean, gy - sh), Pt(sx + w * .3f + lean * .3f, gy - sh * .62f), Pt(sx + w * .55f, gy) };
                    g.FillPolygon(B(Al(Mix(ice, snow, .25f), 225)), crystal);
                    g.FillPolygon(B(Al(snow, 190)), new[] { Pt(sx - w * .12f, gy), Pt(sx - w * .05f + lean * .6f, gy - sh * .85f), Pt(sx + lean, gy - sh), Pt(sx + w * .2f, gy) });
                    g.DrawPolygon(Pn(Al(deep, 230), 1.8f), crystal);
                }
                Glint(x + (Hash01(i, 8) - .5f) * 10, gy - h * (.5f + .4f * Hash01(i, 12)), 7 + 7 * Hash01(i, 13), Color.White, grow * (.4f + .6f * (float)Math.Abs(Math.Sin(T * 5 + i))), 0);
            }

            // 3. 눈보라 입김: 부채꼴로 퍼지며 멀어지는 서리 구름
            GradCone(m, Pt(m.X - 220, m.Y - 74), Pt(m.X - 228, m.Y + 66), ice, 150 * a, 0);
            const int N = 46;
            for (int i = 0; i < N; i++)
            {
                float ph = (bt * 1.15f + i * (1f / N)) % 1f, sp = Hash01(i, 1) * 2 - 1;
                float d = 14 + ph * 206, x = m.X - d, y = m.Y + sp * d * .34f + ph * ph * 22;
                float r = 8 + 40 * (float)Math.Pow(ph, .8), al = a * Math.Min(1f, ph / .1f) * (float)Math.Pow(1 - ph, .8) * 175;
                g.FillEllipse(B(Al(i % 3 == 0 ? snow : ice, al)), CxClamp(x) - r, y - r * .8f, r * 2, r * 1.6f);
            }
            Glow(m.X - 18, m.Y, 50, 36, Al(snow, 200 * a));
            for (int i = 0; i < 28; i++)
            {
                float ph = (bt * .9f + i * .0357f) % 1f;
                float x = m.X - 10 - ph * 215, y = m.Y + (Hash01(i, 21) * 2 - 1) * ph * 84 + (float)Math.Sin(bt * 5 + i) * 8;
                Snowflake(x, y, 4 + 5 * Hash01(i, 22), bt * 90 + i * 30, snow, a * (1 - ph));
            }
        }

        // ───────── 초음속 펀치 ─────────
        // 웅크려 기를 모으며 땅이 갈라지고 → 폭발하듯 도약(충격파 고리) → 음속의 원뿔·증기 고리·속도선을 끌며 날아 → 주먹이 꽂히는 순간 "POW!!" 충격파.

        private void DrawSupersonicPunch()
        {
            float T = p.SigT, bx = p.BodyX, by = p.BodyY;
            int frame = (int)(T * 30);
            Color sky = Color.FromArgb(150, 215, 255), gold = Color.FromArgb(255, 226, 110);
            float cyBody = 330 + by;
            float fly = Ease01((T - 1.5f) / .3f) * (1f - Ease01((T - 4.0f) / .6f));

            // 1. 웅크림(0~1.5): 발밑이 갈라지고 기운이 소용돌이친다
            if (T < 1.6f)
            {
                float ch = Math.Min(1f, T / 1.45f);
                Glow(200 + bx, 568, 80 + 70 * ch, 16 + 10 * ch, Al(sky, 150 * ch));
                float ph0 = (T * 2.2f) % 1f;
                GroundRing(200 + bx, 568, 30 + 120 * ph0, .2f, sky, (1 - ph0) * ch, 2.2f);
                var rg = new Random(frame * 5 + 1);
                for (int i = 0; i < 3 + (int)(5 * ch); i++)
                {
                    float x1 = 200 + bx + (rg.Next(2) == 0 ? -1 : 1) * (36 + rg.Next(90));
                    StrokeThin(BoltPath(Pt(200 + bx, 568), PtC(x1, 568 + rg.Next(-8, 9)), frame * 3 + i, 8, 6), .85f * ch, 2.4f);
                }
                for (int i = 0; i < 12; i++)
                {
                    float ph = (T * 1.5f + i * .083f) % 1f, x = 200 + bx + (Hash01(i, 1) * 2 - 1) * 56, y = 566 - ph * 270;
                    Stroke(new[] { Pt(x, y), Pt(x, y + 22) }, Al(Color.White, 200 * (1 - ph) * ch), 2.4f);
                }
                Glow(wrist[1].X, wrist[1].Y, 18 + 28 * ch, 18 + 28 * ch, Al(gold, 200 * ch));
            }

            // 2. 이륙 충격파(1.5~2.6)
            if (T >= 1.5f && T < 2.7f)
            {
                float u = (T - 1.5f) / 1.2f, fa = 1 - u;
                for (int i = 0; i < 3; i++)
                {
                    float ur = u - i * .12f; if (ur <= 0) continue;
                    GroundRing(200 + bx, 568, 20 + 190 * (float)Math.Sqrt(ur), .17f, sky, (1 - ur) * fa + .1f, 4.2f - i);
                }
                for (int sd = -1; sd <= 1; sd += 2)
                    for (int j = 0; j < 3; j++)
                    {
                        float x = 200 + bx + sd * (30 + (50 + 40 * j) * (1 - (float)Math.Exp(-u * 3f))), y = 556 - j * 6 - 14 * u, r = 24 + 14 * j + 24 * u;
                        Glow(x, y, r * 1.3f, r * .8f, Al(Color.FromArgb(214, 214, 224), 130 * (1 - u)));
                    }
                Glow(200 + bx, 520, 100, 80, Al(Color.White, 230 * Math.Max(0f, 1 - (T - 1.5f) / .2f)));
            }

            // 3. 비행: 음속 원뿔 + 증기 고리 + 속도선 + 꼬리 구름
            if (fly > .02f)
            {
                Glow(200 + bx, cyBody, 130, 230, Al(sky, 90 * fly));
                GradCone(Pt(200 - 54 + bx, cyBody - 10), Pt(200 + 215, cyBody - 105), Pt(200 + 215, cyBody + 105), sky, 170 * fly, 0);
                GradCone(Pt(200 - 54 + bx, cyBody - 10), Pt(200 + 200, cyBody - 52), Pt(200 + 200, cyBody + 52), Color.White, 190 * fly, 0);
                for (int i = 0; i < 5; i++)
                {
                    float x = 200 + 34 + i * 40 + ((T * 3.2f) % 1f) * 40 + bx, rr = 10 + i * 2.5f, ry = 46 + i * 13, al = (1 - i / 5.5f) * fly;
                    g.DrawEllipse(Pn(Al(sky, 150 * al), 7f), x - rr, cyBody - ry, rr * 2, ry * 2);
                    g.DrawEllipse(Pn(Al(Color.White, 230 * al), 2.4f), x - rr, cyBody - ry, rr * 2, ry * 2);
                }
                for (int i = 0; i < 16; i++)
                {
                    float ph = (T * 3.4f + i * .0625f) % 1f, y = cyBody + (Hash01(i, 5) * 2 - 1) * 210, len = 80 + 90 * Hash01(i, 6);
                    float x = 440 - ph * (480 + len);
                    Stroke(new[] { PtC(x, y), PtC(x + len, y) }, Al(Color.White, 175 * fly * (.4f + .6f * Hash01(i, 7))), 2.2f);
                }
                for (int i = 0; i < 7; i++)
                {
                    float ph = (T * 1.8f + i * .143f) % 1f, x = 200 + 90 + ph * 190, y = cyBody + (Hash01(i, 8) * 2 - 1) * 70 * (.3f + ph), r = 14 + 26 * ph;
                    g.FillEllipse(B(Al(Color.FromArgb(232, 240, 250), 140 * (1 - ph) * fly)), CxClamp(x) - r, y - r * .8f, r * 2, r * 1.6f);
                }
            }

            // 4. 펀치(3.55~): 주먹에 기가 모이고 → 충돌 → 별 모양 폭발 + 충격 고리 + "POW!!"
            if (T >= 3.5f && T < 5.4f)
            {
                PointF F = wrist[1];
                float pre = Math.Min(1f, (T - 3.5f) / .3f), u = T - 3.8f;
                if (u < 0f) { Glow(F.X, F.Y, 20 + 36 * pre, 20 + 36 * pre, Al(gold, 220 * pre)); Glow(F.X, F.Y, 10 + 12 * pre, 10 + 12 * pre, Al(Color.White, 255 * pre)); }
                else
                {
                    float fl = Math.Max(0f, 1f - u / .28f), fade = Math.Max(0f, 1f - u / 1.1f);
                    Glow(F.X, F.Y, 170, 150, Al(Color.White, 230 * fl));
                    Glow(F.X, F.Y, 90, 80, Al(gold, 220 * fade));
                    float bs = Math.Min(1f, u * 9f);
                    Starburst(F.X, F.Y, 112 * bs, 52 * bs, 11, 12f, Al(Color.FromArgb(255, 236, 120), 235 * fade), Al(Color.FromArgb(230, 50, 40), 255 * fade), 4f);
                    Starburst(F.X, F.Y, 62 * bs, 26 * bs, 9, 30f, Al(Color.White, 255 * fade), Color.Transparent, 0);
                    for (int i = 0; i < 3; i++)
                    {
                        float ur = u - i * .07f; if (ur <= 0) continue;
                        float r = 30 + 190 * (1 - (float)Math.Exp(-ur * 4f)), rx = r * .34f, f2 = Math.Max(0f, 1 - ur / .9f);
                        g.DrawEllipse(Pn(Al(sky, 170 * f2), 9f - i * 2), F.X - rx, F.Y - r, rx * 2, r * 2);
                        g.DrawEllipse(Pn(Al(Color.White, 245 * f2), 2.6f), F.X - rx, F.Y - r, rx * 2, r * 2);
                    }
                    for (int i = 0; i < 18; i++)
                    {
                        double ang = i * Math.PI / 9 + Hash01(i, 3) * .2; float r0 = 50 + 20 * Hash01(i, 4), r1 = r0 + (50 + 90 * Hash01(i, 5)) * Math.Min(1f, u * 6f);
                        Stroke(new[] { PtC(F.X + (float)Math.Cos(ang) * r0, F.Y + (float)Math.Sin(ang) * r0), PtC(F.X + (float)Math.Cos(ang) * r1, F.Y + (float)Math.Sin(ang) * r1) }, Al(Color.White, 230 * fade), 2.6f);
                    }
                    ComicText("POW!!", 70, F.Y - 96, 50 + 8 * (float)Math.Sin(u * 30), -8f, Color.FromArgb(232, 44, 40), Math.Min(1f, fade * 1.6f));
                }
            }

            // 5. 착지(4.6~5.5): 먼지 고리
            if (T >= 4.6f && T < 5.7f)
            {
                float u = (T - 4.6f) / 1.1f;
                GroundRing(200 + bx, 568, 20 + 150 * (float)Math.Sqrt(u), .17f, sky, (1 - u) * .9f, 3.4f);
                for (int sd = -1; sd <= 1; sd += 2)
                {
                    float x = 200 + bx + sd * (30 + 80 * (1 - (float)Math.Exp(-u * 3f)));
                    Glow(x, 552 - 10 * u, 30 + 20 * u, 18, Al(Color.FromArgb(214, 214, 224), 130 * (1 - u)));
                }
            }
        }

        // ───────── 셔츠 찢기(S 각성) 보강 ─────────
        // 가슴에서 황금빛이 새어 나오고(단추가 튀고) → 변신 순간 섬광 → 황금 충격 고리와 햇살, 반짝이는 희망의 별빛.

        private void DrawShirtRipBoost()
        {
            float T = p.SigT, bx = p.BodyX, by = p.BodyY;
            Color gold = Color.FromArgb(255, 220, 80);
            PointF c = Pt(200 + bx, shY + 62 + by);

            if (T >= 1.6f && T < 3.5f)
            {
                float g1 = Ease01((T - 1.6f) / 1.5f);
                Glow(c.X, c.Y, 40 + 90 * g1, 40 + 90 * g1, Al(gold, 150 * g1));
                for (int i = 0; i < 14; i++)
                {
                    double ang = i * Math.PI * 2 / 14 + T * .5; float r1 = (28 + 110 * g1) * (.6f + .4f * Hash01(i, 2));
                    Stroke(new[] { Pt(c.X + (float)Math.Cos(ang) * 18, c.Y + (float)Math.Sin(ang) * 18), PtC(c.X + (float)Math.Cos(ang) * r1, c.Y + (float)Math.Sin(ang) * r1) }, Al(gold, 190 * g1), 3f);
                }
                if (T > 1.75f)
                    for (int i = 0; i < 6; i++)   // 뜯겨 날아가는 단추
                    {
                        float u = T - 1.75f, vx = (i % 2 == 0 ? -1 : 1) * (90 + 60 * Hash01(i, 3)), vy = -(120 + 120 * Hash01(i, 4));
                        float x = c.X + vx * u, y = c.Y + vy * u + 420 * u * u;
                        g.FillEllipse(B(Al(Color.White, 255 * Math.Max(0f, 1 - u / 1.2f))), CxClamp(x) - 4, y - 4, 8, 8);
                    }
            }

            if (T >= 3.2f)
            {
                float u = T - 3.2f;
                float fl = Math.Max(0f, 1f - u / .35f);
                Glow(c.X, c.Y, 220, 260, Al(Color.White, 230 * fl));
                for (int i = 0; i < 3; i++)
                {
                    float ur = u - i * .14f; if (ur <= 0) continue;
                    float r = Math.Min(215f, 40 + 250 * (1 - (float)Math.Exp(-ur * 2.6f))), f2 = Math.Max(0f, 1 - ur / 1.4f);
                    g.DrawEllipse(Pn(Al(gold, 190 * f2), 10f - i * 2), c.X - r, c.Y - r, r * 2, r * 2);
                    g.DrawEllipse(Pn(Al(Color.White, 245 * f2), 2.6f), c.X - r, c.Y - r, r * 2, r * 2);
                }
                float ray = Ease01(u / .5f) * Math.Max(0f, 1f - (u - 1.2f) / 1.6f);
                for (int i = 0; i < 18; i++)
                {
                    double ang = i * Math.PI / 9 + T * .35; float r0 = 46, r1 = 46 + (i % 2 == 0 ? 190 : 120) * ray;
                    var ray2 = new[] { Pt(c.X + (float)Math.Cos(ang) * r0, c.Y + (float)Math.Sin(ang) * r0), PtC(c.X + (float)Math.Cos(ang) * r1, c.Y + (float)Math.Sin(ang) * r1) };
                    Stroke(ray2, Al(gold, 120 * ray), 10f);
                    Stroke(ray2, Al(Color.White, 220 * ray), 2.6f);
                }
                float tw = Ease01((u - .2f) / .6f) * Ease01((6.2f - T) / 1f);
                for (int i = 0; i < 22; i++)
                {
                    float ph = (T * .45f + i * .0455f) % 1f, x = 200 + bx + (Hash01(i, 6) * 2 - 1) * 150, y = 520 + by - ph * 440;
                    Glint(x, y, 6 + 9 * Hash01(i, 7), i % 3 == 0 ? Color.White : gold, tw * (float)Math.Sin(ph * Math.PI), T * 60 + i * 20);
                }
            }
        }
    }
}
