using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace SuperHeroes
{
    // 배트맨 시그니처 연출: 배트랭 · 그래플건 · 연막탄 · 가고일 응시
    internal sealed partial class HeroPainter
    {
        private static PointF Lerp2(PointF a, PointF b, float t) { return new PointF(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t); }

        /// <summary>박쥐 실루엣(날갯짓 flap -1..1).</summary>
        private void DrawBat(float x, float y, float sc, float flap, float a)
        {
            if (a <= .02f || x < clampLo - 4f || x > clampHi + 4f) return;
            var wl = new[] { Pt(0, -2), Pt(-10, -6 - 10 * flap), Pt(-24, -2 - 15 * flap), Pt(-19, 2 - 5 * flap), Pt(-13, 0), Pt(-8, 6), Pt(-3, 3) };
            var wr = new PointF[wl.Length];
            for (int i = 0; i < wl.Length; i++) wr[i] = Pt(-wl[i].X, wl[i].Y);
            var tf = new Func<PointF[], PointF[]>(src => { var o = new PointF[src.Length]; for (int i = 0; i < src.Length; i++) o[i] = Pt(x + src[i].X * sc, y + src[i].Y * sc); return o; });
            Color c = Color.FromArgb(18, 20, 28);
            g.FillPolygon(B(Al(c, 255 * a)), tf(wl)); g.FillPolygon(B(Al(c, 255 * a)), tf(wr));
            g.FillEllipse(B(Al(c, 255 * a)), x - 3.5f * sc, y - 5 * sc, 7 * sc, 11 * sc);
            g.FillPolygon(B(Al(c, 255 * a)), tf(new[] { Pt(-3, -4), Pt(-4, -9), Pt(-1, -6) })); g.FillPolygon(B(Al(c, 255 * a)), tf(new[] { Pt(3, -4), Pt(4, -9), Pt(1, -6) }));
        }

        /// <summary>카울 속에서 하얗게 번뜩이는 눈(연막·어둠 속에서도 보이는 배트맨의 시선).</summary>
        private void DrawCowlEyes(float a)
        {
            if (a <= .02f) return;
            float bx = p.BodyX, by = p.BodyY, ey = headCy - 4 + by;
            foreach (int s in new[] { -1, 1 })
            {
                float x = 200 + s * 26 + bx;
                Glow(x, ey, 30, 18, Al(Color.White, 120 * a));
                g.FillPolygon(B(Al(Color.White, 255 * a)), new[] { Pt(x - s * 14, ey - 6), Pt(x + s * 12, ey + 2), Pt(x + s * 12, ey + 7), Pt(x - s * 14, ey - 1) });
            }
        }

        // ───────── 배트랭 ─────────
        // 목표에 붉은 조준 장치가 조여들며 락온 → 배트랭 세 개가 부채꼴로 날아가 과녁을 꿰뚫고 → 메인 배트랭이 되돌아와 손에 척 잡힌다.

        private static PointF BrangPos(float T, PointF H0, PointF tgt, out float spin)
        {
            const float tOut = 1.45f, tHit = 2.45f, tBack = 2.7f, tCatch = 3.8f;
            PointF pos; spin = T * 42f;
            if (T < tHit)
            {
                float u = Math.Max(0f, (T - tOut) / (tHit - tOut));
                pos = Lerp2(H0, tgt, u); pos.Y -= 64f * (float)Math.Sin(Math.PI * u);
            }
            else if (T < tBack) { pos = tgt; spin = tHit * 42f + (T - tHit) * 8f; }
            else
            {
                float v = Math.Min(1f, (T - tBack) / (tCatch - tBack)), e = v * v * (3 - 2 * v);
                pos = Lerp2(tgt, H0, e); pos.Y += 78f * (float)Math.Sin(Math.PI * v);
            }
            return pos;
        }

        private void DrawBatarangShape(float x, float y, float rot, float sc, float a)
        {
            var st = g.Save();
            g.TranslateTransform(x, y); g.RotateTransform(rot); g.ScaleTransform(sc, sc);
            var wing = new[]
            {
                Pt(0, -9), Pt(-7, -15), Pt(-13, -10), Pt(-31, -17), Pt(-46, -4), Pt(-35, -7), Pt(-29, 3), Pt(-21, -2), Pt(-12, 9), Pt(-4, 2),
                Pt(0, 11), Pt(4, 2), Pt(12, 9), Pt(21, -2), Pt(29, 3), Pt(35, -7), Pt(46, -4), Pt(31, -17), Pt(13, -10), Pt(7, -15)
            };
            g.FillPolygon(B(Al(Color.FromArgb(34, 38, 50), 255 * a)), wing);
            g.DrawPolygon(Pn(Al(Color.FromArgb(205, 214, 230), 255 * a), 2.4f / Math.Max(.6f, sc) + 1f), wing);
            g.FillEllipse(B(Al(Color.FromArgb(205, 214, 230), 230 * a)), -4, -4, 8, 8);
            g.Restore(st);
        }

        private void DrawBatarangFx()
        {
            float T = p.SigT, bx = p.BodyX, by = p.BodyY;
            int frame = (int)(T * 30);
            PointF tgt = Pt(34, 296), H0 = Pt(92, 270);
            Color red = Color.FromArgb(255, 60, 60), steel = Color.FromArgb(205, 214, 230), glow = Color.FromArgb(120, 170, 255);
            const float tHit = 2.45f;

            // 1. 과녁(받침대 + 과녁판)과 붉은 락온 조준
            float showT = Ease01((T - .15f) / .5f) * Ease01((4.6f - T) / .6f);
            if (showT > .02f)
            {
                g.DrawLine(Pn(Al(Color.FromArgb(70, 72, 82), 255 * showT), 7f), tgt.X, tgt.Y + 40, tgt.X, 568);
                float rr = 40 * (.75f + .25f * showT);
                Color[] bands = { Color.FromArgb(236, 236, 240), Color.FromArgb(210, 50, 50), Color.FromArgb(236, 236, 240), Color.FromArgb(210, 50, 50) };
                for (int i = 0; i < 4; i++)
                {
                    float r = rr * (1 - i * .24f);
                    g.FillEllipse(B(Al(bands[i], 255 * showT)), tgt.X - r, tgt.Y - r, r * 2, r * 2);
                    g.DrawEllipse(Pn(Al(cInk, 255 * showT), 1.8f), tgt.X - r, tgt.Y - r, r * 2, r * 2);
                }
                if (T < tHit)
                {
                    float lock1 = Ease01((T - .3f) / 1.1f), pulse = .6f + .4f * (float)Math.Abs(Math.Sin(T * 9));
                    float rl = 44 + 86 * (1 - lock1);
                    g.DrawEllipse(Pn(Al(red, 220 * pulse * showT), 3f), tgt.X - rl, tgt.Y - rl, rl * 2, rl * 2);
                    g.DrawEllipse(Pn(Al(red, 140 * showT), 1.6f), tgt.X - rl * .62f, tgt.Y - rl * .62f, rl * 1.24f, rl * 1.24f);
                    float ext = 18 + 26 * (1 - lock1);
                    foreach (float sx in new[] { -1f, 1f })
                    {
                        Stroke(new[] { Pt(tgt.X + sx * (rl - ext), tgt.Y), Pt(tgt.X + sx * (rl + 12), tgt.Y) }, Al(red, 230 * showT), 2.4f);
                        Stroke(new[] { Pt(tgt.X, tgt.Y + sx * (rl - ext)), Pt(tgt.X, tgt.Y + sx * (rl + 12)) }, Al(red, 230 * showT), 2.4f);
                    }
                    double rot = T * 1.2;
                    for (int i = 0; i < 4; i++)
                    {
                        double ang = rot + i * Math.PI / 2 + Math.PI / 4; float bxx = tgt.X + (float)Math.Cos(ang) * rl * .9f, byy = tgt.Y + (float)Math.Sin(ang) * rl * .9f;
                        g.FillEllipse(B(Al(red, 240 * showT)), bxx - 3, byy - 3, 6, 6);
                    }
                    if (lock1 > .95f && (frame / 4) % 2 == 0) Glint(tgt.X, tgt.Y, 22, red, showT, 45);
                }
            }

            // 2. 배트랭 3개: 가운데는 되돌아오는 메인, 양옆 둘은 과녁에 박힌다
            if (T >= 1.45f && T < 4.0f)
            {
                float sp; PointF mp = BrangPos(T, H0, tgt, out sp);
                bool flying = T < tHit || T >= 2.7f;
                if (flying)
                {
                    // 곡선 꼬리
                    var trail = new List<PointF>();
                    for (int j = 0; j < 16; j++)
                    {
                        float tj = T - j * .022f; if (tj < 1.45f) break; float s2; trail.Add(PtC(BrangPos(tj, H0, tgt, out s2).X, BrangPos(tj, H0, tgt, out s2).Y));
                    }
                    if (trail.Count > 1)
                    {
                        var tr = trail.ToArray();
                        Stroke(tr, Al(glow, 110), 22f); Stroke(tr, Al(Color.FromArgb(190, 215, 255), 190), 9f); Stroke(tr, Al(Color.White, 245), 3f);
                    }
                    for (int gi = 3; gi >= 1; gi--) DrawBatarangShape(mp.X, mp.Y, sp - gi * 22f, 1.5f, .22f / gi);
                }
                DrawBatarangShape(mp.X, mp.Y, sp, 1.55f, 1f);
                Glow(mp.X, mp.Y, 38, 30, Al(glow, 90));
            }
            for (int s = 0; s < 2; s++)   // 곁가지 배트랭
            {
                float t0 = 1.55f + s * .12f, hit = 2.33f + s * .1f; float off = s == 0 ? -20 : 22;
                PointF dst = Pt(tgt.X + (s == 0 ? -12 : 14), tgt.Y + off);
                if (T >= t0 && T < hit)
                {
                    float u = (T - t0) / (hit - t0); PointF q = Lerp2(H0, dst, u); q.Y -= (s == 0 ? 46 : -26) * (float)Math.Sin(Math.PI * u);
                    var tr = new[] { PtC(Lerp2(H0, dst, Math.Max(0, u - .12f)).X, Lerp2(H0, dst, Math.Max(0, u - .12f)).Y - (s == 0 ? 46 : -26) * (float)Math.Sin(Math.PI * Math.Max(0, u - .12f))), PtC(q.X, q.Y) };
                    Stroke(tr, Al(glow, 130), 12f); Stroke(tr, Al(Color.White, 235), 3f);
                    DrawBatarangShape(q.X, q.Y, T * 42f + s * 40, 1.1f, 1f);
                }
                else if (T >= hit && T < 3.9f) DrawBatarangShape(dst.X, dst.Y, s == 0 ? -25 : 20, 1.1f, Ease01((4.0f - T) / .4f));
            }

            // 3. 명중: 과녁이 갈라지며 불꽃과 충격 고리, "THWACK!"
            if (T >= tHit && T < 3.6f)
            {
                float u = T - tHit, fl = Math.Max(0f, 1f - u / .25f), fa = Math.Max(0f, 1f - u / 1.0f);
                Glow(tgt.X, tgt.Y, 130, 110, Al(Color.White, 230 * fl));
                Starburst(tgt.X, tgt.Y, 78 * Math.Min(1f, u * 10), 34 * Math.Min(1f, u * 10), 10, 15f, Al(Color.FromArgb(255, 238, 150), 235 * fa), Al(Color.FromArgb(220, 60, 40), 255 * fa), 3.4f);
                for (int i = 0; i < 2; i++)
                {
                    float ur = u - i * .08f; if (ur <= 0) continue;
                    float r = 30 + 110 * (1 - (float)Math.Exp(-ur * 4.2f)), f2 = Math.Max(0f, 1 - ur / .8f);
                    g.DrawEllipse(Pn(Al(glow, 170 * f2), 7f), tgt.X - r, tgt.Y - r, r * 2, r * 2); g.DrawEllipse(Pn(Al(Color.White, 240 * f2), 2.2f), tgt.X - r, tgt.Y - r, r * 2, r * 2);
                }
                for (int i = 0; i < 9; i++)   // 갈라진 금
                {
                    double ang = i * Math.PI * 2 / 9 + .3; float len = (26 + 34 * Hash01(i, 3)) * Math.Min(1f, u * 7);
                    Stroke(BoltPath(Pt(tgt.X, tgt.Y), PtC(tgt.X + (float)Math.Cos(ang) * len, tgt.Y + (float)Math.Sin(ang) * len), 40 + i, 6, 4), Al(cInk, 255 * Ease01((3.4f - T) / .4f)), 2.4f);
                }
                for (int i = 0; i < 18; i++)   // 불꽃
                {
                    float vx = (Hash01(i, 5) * 2 - 1) * 240f, vy = -(60f + Hash01(i, 6) * 220f);
                    PointF s0 = Pt(tgt.X + vx * u, tgt.Y + vy * u + 560f * u * u), s1 = Pt(s0.X - vx * .03f, s0.Y - (vy + 1120f * u) * .03f);
                    Stroke(new[] { PtC(s1.X, s1.Y), PtC(s0.X, s0.Y) }, Al(Color.FromArgb(255, 226, 120), 255 * fa), 2.4f);
                }
                ComicText("THWACK!", 80, tgt.Y - 74, 40, -9f, Color.FromArgb(250, 220, 60), Math.Min(1f, fa * 1.8f));
            }

            // 4. 척 하고 받는 순간
            if (T >= 3.75f && T < 4.3f)
            {
                float u = (T - 3.75f) / .55f;
                Glow(H0.X, H0.Y, 70 * (1 - u * .4f), 60 * (1 - u * .4f), Al(Color.White, 230 * (1 - u)));
                g.DrawEllipse(Pn(Al(steel, 220 * (1 - u)), 3.4f), H0.X - 14 - 60 * u, H0.Y - 14 - 60 * u, 28 + 120 * u, 28 + 120 * u);
                Glint(H0.X, H0.Y, 40 * (1 - u * .5f), Color.White, 1 - u, 45);
                DrawBatarangShape(H0.X, H0.Y, 20f, 1.4f, 1 - u * .5f);
            }
        }

        // ───────── 그래플건 ─────────
        // 위를 겨냥해 발사 → 케이블이 옥상 모서리로 쏜살같이 날아가 박히고 → 팽팽한 케이블에 매달려 단숨에 끌려 올라가 → 옥상에 안착, 박쥐 떼가 흩어진다.

        private void DrawGrapnelFx()
        {
            float T = p.SigT, bx = p.BodyX, by = p.BodyY;
            int frame = (int)(T * 30);
            PointF gun = wrist[1], A = Pt(70, 50);
            Color cable = Color.FromArgb(44, 48, 60), metal = Color.FromArgb(200, 208, 222);
            float roofA = Ease01((T - 1.3f) / .4f) * Ease01((5.0f - T) / .5f);

            // 1. 옥상 모서리(벽돌 벽돌 + 밝은 윗면)
            if (roofA > .02f)
            {
                var slab = new[] { Pt(-44, -6), Pt(108, -6), Pt(108, 40), Pt(98, 50), Pt(-44, 50) };
                g.FillPolygon(B(Al(Color.FromArgb(70, 72, 84), 255 * roofA)), slab);
                g.FillPolygon(B(Al(Color.FromArgb(104, 108, 124), 255 * roofA)), new[] { Pt(-44, -6), Pt(108, -6), Pt(108, 6), Pt(-44, 6) });
                for (int i = 0; i < 6; i++) Stroke(new[] { Pt(-34 + i * 26, 8), Pt(-34 + i * 26, 48) }, Al(Color.FromArgb(40, 42, 52), 200 * roofA), 2f);
                Stroke(new[] { Pt(-44, 28), Pt(104, 28) }, Al(Color.FromArgb(40, 42, 52), 200 * roofA), 2f);
                g.DrawPolygon(Pn(Al(cInk, 255 * roofA), 2.6f), slab);
            }

            // 2. 발사: 총구 섬광 → 케이블이 날아간다 → 갈고리가 박힌다
            float shoot = Math.Min(1f, Math.Max(0f, (T - 1.5f) / .32f));
            float slack = Ease01((T - 4.15f) / .6f);
            if (T >= 1.5f && T < 5.0f)
            {
                float u = shoot, e = 1 - (1 - u) * (1 - u);
                PointF tip = Lerp2(gun, A, e);
                var rope = new PointF[17];
                float amp = (T < 1.85f ? 0 : 11f * (float)Math.Exp(-(T - 1.85f) * 3.2f)) + 90f * slack * (1 - slack) * 2f;
                PointF perp = Pt(-(A.Y - gun.Y), A.X - gun.X); float pl = Math.Max(1f, (float)Math.Sqrt(perp.X * perp.X + perp.Y * perp.Y)); perp = Pt(perp.X / pl, perp.Y / pl);
                for (int i = 0; i <= 16; i++)
                {
                    float f = i / 16f; PointF b0 = Lerp2(gun, tip, f);
                    float w = (float)Math.Sin(f * Math.PI * 3 - T * 38) * amp * (float)Math.Sin(f * Math.PI) + (float)Math.Sin(f * Math.PI) * 60f * slack;
                    rope[i] = Pt(b0.X + perp.X * w, b0.Y + perp.Y * w);
                }
                if (slack < .98f)
                {
                    Stroke(rope, Al(cInk, 255 * (1 - slack)), 6.6f); Stroke(rope, Al(cable, 255 * (1 - slack)), 4.2f); Stroke(rope, Al(Color.FromArgb(150, 160, 180), 200 * (1 - slack)), 1.4f);
                }
                if (u < 1f) { Stroke(new[] { Lerp2(gun, tip, Math.Max(0, e - .3f)), tip }, Al(Color.White, 230), 3f); }
                // 갈고리(3갈래 닻)
                var st = g.Save(); g.TranslateTransform(tip.X, tip.Y); g.RotateTransform(u < 1f ? T * 600f : 0f);
                g.FillPolygon(B(Al(metal, 255 * (1 - slack * .6f))), new[] { Pt(-3, 4), Pt(-14, -10), Pt(-9, -12), Pt(-2, -2), Pt(0, -14), Pt(2, -2), Pt(9, -12), Pt(14, -10), Pt(3, 4) });
                g.DrawPolygon(Pn(Al(cInk, 255), 1.8f), new[] { Pt(-3, 4), Pt(-14, -10), Pt(-9, -12), Pt(-2, -2), Pt(0, -14), Pt(2, -2), Pt(9, -12), Pt(14, -10), Pt(3, 4) });
                g.Restore(st);
            }
            if (T >= 1.5f && T < 1.66f)
            {
                float u = (T - 1.5f) / .16f;
                Glow(gun.X, gun.Y, 50, 44, Al(Color.FromArgb(255, 240, 180), 230 * (1 - u)));
                Glint(gun.X, gun.Y, 40, Color.FromArgb(255, 230, 140), 1 - u, 45);
            }
            if (T >= 1.82f && T < 2.7f)   // 박히는 순간: 파편과 충격 고리
            {
                float u = T - 1.82f, fa = Math.Max(0f, 1 - u / .8f);
                Glow(A.X, A.Y, 90, 70, Al(Color.White, 210 * Math.Max(0f, 1 - u / .2f)));
                g.DrawEllipse(Pn(Al(metal, 220 * fa), 3f), A.X - 12 - 70 * u * 1.5f, A.Y - 12 - 70 * u * 1.5f, 24 + 210 * u, 24 + 210 * u);
                for (int i = 0; i < 10; i++)
                {
                    float vx = (Hash01(i, 4) * 2 - 1) * 120f, vy = 40f + Hash01(i, 5) * 160f;
                    g.FillRectangle(B(Al(Color.FromArgb(112, 114, 128), 255 * fa)), CxClamp(A.X + vx * u) - 3, A.Y + vy * u * u * 2.6f - 3, 6, 5);
                }
                for (int i = 0; i < 12; i++)
                {
                    double ang = i * Math.PI / 6; float r0 = 12, r1 = 12 + 50 * Math.Min(1f, u * 5);
                    Stroke(new[] { Pt(A.X + (float)Math.Cos(ang) * r0, A.Y + (float)Math.Sin(ang) * r0), PtC(A.X + (float)Math.Cos(ang) * r1, A.Y + (float)Math.Sin(ang) * r1) }, Al(Color.White, 240 * fa), 2.4f);
                }
            }

            // 3. 끌려 올라가는 동안: 아래로 흐르는 속도선 + 바닥 먼지
            float pull = Ease01((T - 2.1f) / .4f) * (1 - Ease01((T - 3.5f) / .35f));
            if (pull > .02f)
            {
                for (int i = 0; i < 14; i++)
                {
                    float ph = (T * 2.6f + i * .0714f) % 1f, x = 200 + bx + (Hash01(i, 2) * 2 - 1) * 120, y0 = 200 + by + ph * 380;
                    Stroke(new[] { Pt(x, y0), Pt(x, y0 + 40 + 60 * Hash01(i, 3)) }, Al(Color.White, 175 * pull * (1 - ph * .5f)), 2.2f);
                }
                Glow(200 + bx, 330 + by, 110, 210, Al(Color.FromArgb(150, 180, 255), 40 * pull));
            }
            if (T >= 2.05f && T < 2.8f)
            {
                float u = (T - 2.05f) / .75f;
                GroundRing(200, 568, 20 + 120 * (float)Math.Sqrt(u), .17f, Color.FromArgb(190, 200, 220), 1 - u, 3f);
            }

            // 4. 안착(3.6~): 옥상 위로 사뿐히, 박쥐 떼가 흩어진다
            if (T >= 4.2f && T < 6.8f)
            {
                for (int i = 0; i < 9; i++)
                {
                    float t0 = 4.2f + i * .12f, u = (T - t0); if (u < 0) continue;
                    float x = 150 + u * (80 + 70 * Hash01(i, 1)) * (i % 2 == 0 ? 1 : -1), y = 60 - u * (20 + 60 * Hash01(i, 2)) + (float)Math.Sin(u * 5 + i) * 14;
                    DrawBat(x, y, .8f + .7f * Hash01(i, 3), (float)Math.Sin(T * 17 + i * 1.3f), Ease01((6.6f - T) / .5f));
                }
            }
        }

        // ───────── 연막탄 ─────────
        // 연막탄을 발밑에 툭 던지면 "퍽!" 하고 터지고 → 짙은 연기가 몸을 휘감아 피어오르며 → 연기 속에서 흰 눈빛만 번뜩이다가 → 서서히 걷힌다.

        private void DrawSmokeFx()
        {
            float T = p.SigT, a = Math.Max(0f, Math.Min(1f, p.SmokeBomb)), bx = p.BodyX, by = p.BodyY;
            const float tPop = 1.55f;
            float cover = Ease01((T - tPop) / 1.5f) * Ease01((6.3f - T) / 1.5f);

            // 1. 연막탄: 손에서 포물선을 그리며 발밑으로 → 퍽!
            if (T >= 1.2f && T < tPop)
            {
                float u = (T - 1.2f) / (tPop - 1.2f);
                PointF H = wrist[1], dst = Pt(138, 560), q = Lerp2(H, dst, u); q.Y -= 70 * (float)Math.Sin(Math.PI * u);
                g.FillEllipse(B(Color.FromArgb(30, 32, 40)), q.X - 8, q.Y - 8, 16, 16); g.DrawEllipse(Pn(Al(Color.FromArgb(180, 190, 210), 255), 2f), q.X - 8, q.Y - 8, 16, 16);
                Glint(q.X + 6, q.Y - 10, 10, Color.FromArgb(255, 200, 80), 1f, T * 300);
            }
            if (T >= tPop && T < tPop + .6f)
            {
                float u = (T - tPop) / .6f, fl = Math.Max(0f, 1f - u / .35f);
                Glow(150, 548, 190, 120, Al(Color.White, 240 * fl));
                GroundRing(150, 566, 20 + 180 * (float)Math.Sqrt(u), .22f, Color.FromArgb(210, 214, 226), 1 - u, 3.4f);
                Starburst(150, 548, 70 * Math.Min(1f, u * 8), 30 * Math.Min(1f, u * 8), 9, 0f, Al(Color.White, 230 * (1 - u)), Color.Transparent, 0);
                ComicText("PFFT!", 70, 470, 36, -10f, Color.FromArgb(220, 224, 236), Math.Min(1f, (1 - u) * 2f));
            }

            // 2. 피어오르는 연기: 아래쪽부터 몸을 뒤덮는 먹구름 같은 연기 덩어리
            if (T >= tPop && a > .02f)
            {
                float st = T - tPop;
                g.FillEllipse(B(Al(Color.FromArgb(48, 52, 62), 190 * a * Ease01(st / .5f))), 200 + bx - 215 * Math.Min(1f, st / .6f), 548, 430 * Math.Min(1f, st / .6f), 44);
                for (int i = 0; i < 46; i++)
                {
                    float hx = 200 + (Hash01(i, 1) * 2 - 1) * 135 + bx, hy = 548 - Hash01(i, 2) * 470, delay = (548 - hy) / 470f * 1.3f;
                    float sc = Ease01((st - delay) / .8f); if (sc <= .01f) continue;
                    float r = (34 + 40 * Hash01(i, 3)) * sc;
                    float x = hx + (float)Math.Sin(T * 1.3f + i) * 9 + (hx - 200) * .15f * st * .3f, y = hy - st * 8f;
                    g.FillEllipse(B(Al(Color.FromArgb(44, 48, 58), 225 * a * sc)), CxClamp(x) - r, y - r * .85f, r * 2, r * 1.7f);
                    g.FillEllipse(B(Al(Color.FromArgb(84, 90, 104), 160 * a * sc)), CxClamp(x) - r * .72f, y - r * .85f, r * 1.3f, r * 1.0f);
                }
                for (int i = 0; i < 6; i++)   // 가장자리에서 꼬리처럼 풀려나는 연기
                {
                    float ph = (T * .5f + i * .167f) % 1f;
                    Glow(CxClamp(200 + (i % 2 == 0 ? -1 : 1) * (80 + ph * 110)), 520 - ph * 380, 50 + 30 * ph, 40 + 30 * ph, Al(Color.FromArgb(70, 76, 90), 150 * (1 - ph) * a));
                }
            }

            // 3. 연기 속에서 번뜩이는 눈
            DrawCowlEyes(Ease01((cover - .55f) / .35f) * (.75f + .25f * (float)Math.Sin(T * 9)));
        }

        // ───────── 가고일 응시 ─────────
        // 비 내리는 고담의 밤: 쏟아지는 빗줄기, 번쩍이는 번개(전신 실루엣이 하얗게 떠오른다), 박쥐 떼, 어둠 속 하얀 눈빛, 발밑에 번지는 빗방울 파문.

        private void DrawGargoyleFx()
        {
            float T = p.SigT, bx = p.BodyX, by = p.BodyY;
            float fade = Ease01(T / .8f) * Ease01((7.4f - T) / .8f);
            float[] bolts = { 1.9f, 3.5f, 5.1f };

            // 번개
            float flash = 0f; int bi = -1;
            for (int i = 0; i < bolts.Length; i++) { float u = T - bolts[i]; if (u >= 0 && u < .55f) { bi = i; flash = (u < .1f ? 1f : (((int)(T * 30)) % 3 == 0 ? .4f : .75f)) * (1 - u / .55f); } }
            if (flash > .02f)
            {
                Glow(200, 300, 230, 330, Al(Color.FromArgb(214, 226, 255), 120 * flash));
                float bxp = bi == 0 ? 8f : bi == 1 ? 380f : 20f;
                PointF[] bolt = BoltPath(Pt(bxp, -70), Pt(bxp + (bi == 1 ? -60 : 60), 300), 700 + bi * 13, 30, 12);
                StrokeBolt(bolt, flash, .55f);
            }

            // 빗줄기
            for (int i = 0; i < 80; i++)
            {
                float sp = 520 + 300 * Hash01(i, 2), x0 = -50 + Hash01(i, 1) * 520, y = ((Hash01(i, 3) * 640 + T * sp) % 660) - 40, x = x0 - y * .28f;
                Stroke(new[] { PtC(x, y), PtC(x - 7, y + 24 + 16 * Hash01(i, 4)) }, Al(Color.FromArgb(178, 196, 224), (70 + 90 * Hash01(i, 5)) * fade), 1.7f);
            }

            // 발밑 빗방울 파문
            for (int i = 0; i < 7; i++)
            {
                float ph = (T * 1.1f + i * .143f) % 1f, x = 200 + bx + (Hash01(i, 6) * 2 - 1) * 120;
                g.DrawEllipse(Pn(Al(Color.FromArgb(190, 206, 230), 150 * (1 - ph) * fade), 1.8f), x - 4 - 20 * ph, 566 - 1.5f - 5 * ph, 8 + 40 * ph, 3 + 10 * ph);
            }

            // 박쥐 떼
            for (int i = 0; i < 8; i++)
            {
                float t0 = 2.1f + i * .35f, u = T - t0; if (u < 0 || u > 4.2f) continue;
                float x = 460 - u * (130 + 60 * Hash01(i, 1)), y = 36 + Hash01(i, 2) * 190 + (float)Math.Sin(u * 2.6f + i) * 22;
                DrawBat(x, y, .8f + .6f * Hash01(i, 3), (float)Math.Sin(T * 16 + i * 1.7f), fade);
            }

            // 어둠 속 하얀 눈빛
            DrawCowlEyes(Ease01((T - 1.2f) / .6f) * fade * (.8f + .2f * (float)Math.Sin(T * 6)) * (1f + flash * .3f));
        }
    }
}
