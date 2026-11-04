using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace SuperHeroes
{
    internal sealed partial class HeroPainter
    {
        private void DrawHeroSignatureFx()
        {
            switch (who)
            {
                case Persona.Superman:
                    DrawSupermanFx();
                    break;
                case Persona.Batman:
                    DrawBatmanFx();
                    break;
                case Persona.Spiderman:
                    DrawSpidermanFx();
                    break;
                case Persona.Thor:
                    DrawThorFx();
                    break;
            }

            // 공통 오라 (선택적)
            if (p.Aura > 0.05f)
            {
                DrawHeroAura();
            }
        }

        private void DrawSupermanFx()
        {
            float bx = p.BodyX, by = p.BodyY;
            float eyeX1 = 200 - 24 + bx;
            float eyeX2 = 200 + 24 + bx;
            float eyeY = headCy - 8 + by;

            // 1. 히트 비전 (Heat Vision)
            if (p.HeatLaser > 0.05f)
            {
                float targetX = 200 - 180 * (faceRight ? -1 : 1);
                float targetY = 560 + by;

                using (var pnOuter = Pn(Color.FromArgb((int)(180 * p.HeatLaser), 255, 30, 20), 10f))
                using (var pnCore = Pn(Color.FromArgb((int)(255 * p.HeatLaser), 255, 240, 150), 4f))
                {
                    // 눈 1에서 레이저
                    g.DrawLine(pnOuter, eyeX1, eyeY, targetX, targetY);
                    g.DrawLine(pnCore, eyeX1, eyeY, targetX, targetY);
                    // 눈 2에서 레이저
                    g.DrawLine(pnOuter, eyeX2, eyeY, targetX + 10, targetY);
                    g.DrawLine(pnCore, eyeX2, eyeY, targetX + 10, targetY);
                }

                // 바닥 충돌점 스파크
                g.FillEllipse(B(Color.FromArgb(220, 255, 200, 50)), targetX - 25, targetY - 15, 50, 30);
                g.FillEllipse(B(Color.White), targetX - 12, targetY - 8, 24, 16);
            }

            // 2. 냉기 입김 (Freeze Breath)
            if (p.FreezeMist > 0.05f)
            {
                float mX = 200 + (faceRight ? 20 : -20) + bx;
                float mY = headCy + 22 + by;
                float dir = faceRight ? 1 : -1;

                using (var path = new GraphicsPath())
                {
                    path.AddPolygon(new[]
                    {
                        Pt(mX, mY),
                        Pt(mX + dir * 160, mY - 60),
                        Pt(mX + dir * 210, mY + 20),
                        Pt(mX + dir * 180, mY + 80),
                        Pt(mX, mY + 10)
                    });
                    using (var br = new LinearGradientBrush(Pt(mX, mY), Pt(mX + dir * 200, mY),
                        Color.FromArgb((int)(180 * p.FreezeMist), 180, 235, 255),
                        Color.FromArgb(0, 100, 200, 255)))
                    {
                        g.FillPath(br, path);
                    }
                }

                // 얼음 결정 파티클
                for (int i = 0; i < 6; i++)
                {
                    float cx = mX + dir * (60 + i * 22);
                    float cy = mY + (float)Math.Sin(i * 1.5) * 30;
                    g.FillEllipse(B(Color.White), cx - 4, cy - 4, 8, 8);
                    g.DrawLine(ink, cx - 6, cy, cx + 6, cy);
                    g.DrawLine(ink, cx, cy - 6, cx, cy + 6);
                }
            }

            // 3. 초음속 비행 충격파 (Sonic Flight Punch)
            if (p.SonicGlow > 0.05f)
            {
                using (var pn = Pn(Color.FromArgb((int)(160 * p.SonicGlow), 255, 255, 255), 5f))
                {
                    g.DrawEllipse(pn, 200 - 80 + bx, 350 + by, 160, 40);
                    g.DrawEllipse(pn, 200 - 110 + bx, 390 + by, 220, 50);
                }
            }

            // 변신 섬광 (기자 → 수퍼맨)
            if (p.Transform > 0.02f) DrawTransformBurst();

            // 4. 셔츠 뜯기 광채 (Shirt Rip)
            if (p.ShirtOpen > 0.05f)
            {
                float cx = 200 + bx, cy = shY + 60 + by;
                using (var pn = Pn(Color.FromArgb((int)(200 * p.ShirtOpen), 255, 220, 60), 3.5f))
                {
                    for (int i = 0; i < 8; i++)
                    {
                        double ang = i * Math.PI / 4.0;
                        g.DrawLine(pn, cx, cy, cx + (float)Math.Cos(ang) * 90, cy + (float)Math.Sin(ang) * 90);
                    }
                }
            }
        }

        private void DrawBatmanFx()
        {
            float bx = p.BodyX, by = p.BodyY;

            // 1. 배트랭 투척 (Batarang Throw)
            if (p.Batarang > 0.05f)
            {
                float dist = p.Batarang * 240f;
                float bX = 200 + (faceRight ? dist : -dist) + bx;
                float bY = 320 + (float)Math.Sin(p.Batarang * Math.PI) * -80 + by;
                float rot = p.Batarang * 720f;

                var st = g.Save();
                g.TranslateTransform(bX, bY);
                g.RotateTransform(rot);

                // 배트랭 형상
                var brang = new GraphicsPath();
                brang.AddPolygon(new[]
                {
                    Pt(0, 0), Pt(-16, -24), Pt(-8, -26), Pt(0, -12),
                    Pt(8, -26), Pt(16, -24), Pt(0, 0),
                    Pt(16, 24), Pt(8, 26), Pt(0, 12), Pt(-8, 26), Pt(-16, 24)
                });
                g.FillPath(B(Color.FromArgb(40, 42, 48)), brang);
                g.DrawPath(ink, brang);
                g.Restore(st);

                // 비행 궤적선
                using (var pn = Pn(Color.FromArgb((int)(120 * p.Batarang), 200, 210, 230), 2f))
                {
                    g.DrawArc(pn, Math.Min(200, bX), 240, Math.Abs(bX - 200), 120, 0, 180);
                }
            }

            // 2. 그래플건 발사 (Grapnel Gun)
            if (p.Grapnel > 0.05f)
            {
                PointF gunPos = wrist[1];
                PointF targetPos = Pt(gunPos.X + (faceRight ? 140 : -140), 20);

                using (var pn = Pn(Color.FromArgb(50, 52, 60), 3f))
                {
                    g.DrawLine(pn, gunPos.X, gunPos.Y, targetPos.X, targetPos.Y);
                }
                // 갈고리
                g.FillPolygon(B(Color.Silver), new[] { Pt(targetPos.X, targetPos.Y), Pt(targetPos.X - 10, targetPos.Y + 12), Pt(targetPos.X + 10, targetPos.Y + 12) });
                g.DrawPolygon(ink, new[] { Pt(targetPos.X, targetPos.Y), Pt(targetPos.X - 10, targetPos.Y + 12), Pt(targetPos.X + 10, targetPos.Y + 12) });
            }

            // 3. 연막탄 은신 (Smoke Pellet)
            if (p.SmokeBomb > 0.05f)
            {
                float alpha = (float)Math.Sin(p.SmokeBomb * Math.PI);
                Color smk = Color.FromArgb((int)(180 * alpha), 50, 55, 65);
                g.FillEllipse(B(smk), 200 - 140 + bx, 380 + by, 280, 180);
                g.FillEllipse(B(Color.FromArgb((int)(140 * alpha), 80, 85, 95)), 200 - 100 + bx, 420 + by, 200, 140);
                g.FillEllipse(B(Color.FromArgb((int)(120 * alpha), 120, 125, 135)), 200 - 60 + bx, 450 + by, 120, 100);
            }
        }

        private void DrawSpidermanFx()
        {
            float bx = p.BodyX, by = p.BodyY;

            // 1. 웹 스윙 (Thwip Swing)
            if (p.WebSwing > 0.05f)
            {
                PointF hand = wrist[1];
                PointF anchor = Pt(200 + (faceRight ? -120 : 120), 0);

                using (var pn = Pn(Color.White, 3.2f))
                {
                    g.DrawLine(pn, hand.X, hand.Y, anchor.X, anchor.Y);
                }
                using (var pn = Pn(cInk, 1.2f))
                {
                    g.DrawLine(pn, hand.X, hand.Y, anchor.X, anchor.Y);
                }
            }

            // 2. 스파이더 센스 (Spider-Sense)
            if (p.SpiderSense > 0.05f)
            {
                float hx = 200 + p.HeadX + bx;
                float hy = headCy + p.HeadY + by;

                using (var pnY = Pn(Color.FromArgb((int)(240 * p.SpiderSense), 255, 215, 0), 4f))
                using (var pnR = Pn(Color.FromArgb((int)(200 * p.SpiderSense), 255, 40, 40), 2.5f))
                {
                    for (int i = 0; i < 6; i++)
                    {
                        double ang = -Math.PI * .85 + i * (Math.PI * .7 / 5.0);
                        float r1 = headRx * 1.15f, r2 = headRx * 1.55f;
                        PointF p1 = Pt(hx + (float)Math.Cos(ang) * r1, hy + (float)Math.Sin(ang) * r1);
                        PointF pMid = Pt(hx + (float)Math.Cos(ang + .08) * (r1 + r2) * .5f, hy + (float)Math.Sin(ang + .08) * (r1 + r2) * .5f);
                        PointF p2 = Pt(hx + (float)Math.Cos(ang) * r2, hy + (float)Math.Sin(ang) * r2);

                        g.DrawLine(pnY, p1.X, p1.Y, pMid.X, pMid.Y);
                        g.DrawLine(pnY, pMid.X, pMid.Y, p2.X, p2.Y);
                        g.DrawLine(pnR, p1.X, p1.Y, p2.X, p2.Y);
                    }
                }
            }

            // 3. 거미줄 네트 (Web Trap)
            if (p.WebNet > 0.05f)
            {
                float nX = 200 + (faceRight ? 140 : -140) + bx;
                float nY = 360 + by;
                float rad = 80f * p.WebNet;

                using (var pn = Pn(Color.FromArgb((int)(220 * p.WebNet), 250, 250, 255), 2.8f))
                {
                    for (int i = 0; i < 8; i++)
                    {
                        double ang = i * Math.PI / 4.0;
                        g.DrawLine(pn, nX, nY, nX + (float)Math.Cos(ang) * rad, nY + (float)Math.Sin(ang) * rad);
                    }
                    for (int r = 1; r <= 3; r++)
                    {
                        float cr = rad * r / 3f;
                        g.DrawEllipse(pn, nX - cr, nY - cr, cr * 2, cr * 2);
                    }
                }
            }
        }

        private void DrawThorFx()
        {
            float bx = p.BodyX, by = p.BodyY;

            // 1. 묠니르 풍차 회전 (Mjolnir Whirlwind)
            if (p.MjolnirSpin > 0.05f)
            {
                PointF hand = wrist[1];
                float radius = 55f;

                using (var pn = Pn(Color.FromArgb((int)(160 * p.MjolnirSpin), 160, 220, 255), 8f))
                {
                    g.DrawEllipse(pn, hand.X - radius, hand.Y - radius, radius * 2, radius * 2);
                }
                using (var pnWind = Pn(Color.FromArgb((int)(200 * p.MjolnirSpin), 255, 255, 255), 3f))
                {
                    g.DrawArc(pnWind, hand.X - radius * 1.2f, hand.Y - radius * 1.2f, radius * 2.4f, radius * 2.4f, (float)(p.SigPhase * 1080 % 360), 180);
                }
            }

            // 2. 천둥 벼락 소환 (Summon Lightning)
            if (p.Lightning > 0.05f) DrawThunderSummon();

            // 3. 지면 강타 충격파 (Ground Slam)
            if (p.GroundCrush > 0.05f)
            {
                float gX = 200 + bx, gY = 565 + by;
                using (var pn = Pn(Color.FromArgb((int)(220 * p.GroundCrush), 100, 190, 255), 4f))
                {
                    // 지면 크랙
                    g.DrawLine(pn, gX, gY, gX - 70, gY - 6);
                    g.DrawLine(pn, gX - 70, gY - 6, gX - 130, gY + 4);
                    g.DrawLine(pn, gX, gY, gX + 70, gY - 6);
                    g.DrawLine(pn, gX + 70, gY - 6, gX + 130, gY + 4);
                }
            }

            // 4. 묠니르 투척 (Mjolnir Throw)
            if (p.HammerThrow > 0.05f)
            {
                float dist = p.HammerThrow * 260f;
                float mX = 200 + (faceRight ? dist : -dist) + bx;
                float mY = 320 + by;

                var st = g.Save();
                g.TranslateTransform(mX, mY);
                g.RotateTransform(p.HammerThrow * 720f);

                // 묠니르 망치 헤드 & 자루
                g.FillRectangle(B(Color.FromArgb(195, 205, 215)), -18, -24, 36, 48);
                g.DrawRectangle(ink, -18, -24, 36, 48);
                g.FillRectangle(B(Color.FromArgb(120, 80, 50)), -4, 24, 8, 30);
                g.DrawRectangle(ink, -4, 24, 8, 30);

                g.Restore(st);

                // 번개 궤적
                using (var pn = Pn(Color.FromArgb((int)(160 * p.HammerThrow), 130, 220, 255), 3f))
                {
                    g.DrawLine(pn, wrist[1].X, wrist[1].Y, mX, mY);
                }
            }
            else
            {
                // 평소 손에 쥔 묠니르 망치 그리기
                DrawDefaultMjolnir();
            }
        }

        // ───────── 천둥 벼락 소환 ─────────
        // 먹구름이 모이고 → 묠니르에 전기가 차오르다가 → 세 번 점점 더 굵은 벼락이 내리꽂히며 전신 감전 + 지면 충격파.

        private void Glow(float x, float y, float rx, float ry, Color c)
        {
            if (c.A < 3 || rx < 1 || ry < 1) return;
            rx = Math.Min(rx, Math.Min(x + 40, 440 - x));   // 스프라이트 가장자리에서 잘리지 않게(리그 x ≈ -48..448)
            if (rx < 4) return;
            var path = new GraphicsPath(); path.AddEllipse(x - rx, y - ry, rx * 2, ry * 2);
            var br = new PathGradientBrush(path) { CenterColor = c, SurroundColors = new[] { Color.FromArgb(0, c.R, c.G, c.B) } };
            trash.Add(path); trash.Add(br);
            g.FillPath(br, path);
        }

        /// <summary>a→b 로 꺾여 내려가는 번개 줄기(양 끝은 고정, 가운데일수록 크게 흔들린다).</summary>
        private static PointF[] BoltPath(PointF a, PointF b, int seed, float amp, int segs)
        {
            var rnd = new Random(seed); var pts = new PointF[segs + 1];
            float dx = b.X - a.X, dy = b.Y - a.Y, len = (float)Math.Sqrt(dx * dx + dy * dy); if (len < 1) len = 1;
            float nx = -dy / len, ny = dx / len;
            for (int i = 0; i <= segs; i++)
            {
                float t = (float)i / segs, taper = (float)Math.Sin(Math.PI * t);
                float off = (i == 0 || i == segs) ? 0 : ((float)rnd.NextDouble() * 2 - 1) * amp * (.35f + .65f * taper);
                pts[i] = Pt(a.X + dx * t + nx * off, a.Y + dy * t + ny * off);
            }
            return pts;
        }

        private void Stroke(PointF[] pts, Color c, float w) { if (c.A > 2) g.DrawLines(Pn(c, w), pts); }

        /// <summary>겹겹의 빛무리 + 흰 심지로 그린 굵은 벼락.</summary>
        private void StrokeBolt(PointF[] pts, float a, float sc)
        {
            a = Math.Max(0f, Math.Min(1f, a));
            Stroke(pts, Al(Color.FromArgb(80, 150, 255), 95 * a), 64 * sc);
            Stroke(pts, Al(Color.FromArgb(100, 180, 255), 150 * a), 34 * sc);
            Stroke(pts, Al(Color.FromArgb(160, 225, 255), 230 * a), 17 * sc);
            Stroke(pts, Al(Color.White, 255 * a), Math.Max(3.2f, 7.5f * sc));
        }

        private void StrokeThin(PointF[] pts, float a, float w)
        {
            a = Math.Max(0f, Math.Min(1f, a));
            Stroke(pts, Al(Color.FromArgb(110, 190, 255), 200 * a), w * 2.2f);
            Stroke(pts, Al(Color.White, 255 * a), w);
        }

        private void DrawThunderSummon()
        {
            float lt = p.LightningT, bx = p.BodyX, by = p.BodyY;
            int frame = (int)(lt * 30);
            PointF hh = Pt(wrist[1].X, wrist[1].Y - 46);                 // 묠니르 머리 꼭대기
            float fade = Math.Max(0f, Math.Min(1f, Math.Min(lt / 1.0f, (ThorLightning.End - lt) / 0.6f)));
            float charge = Math.Min(1f, lt / 1.6f);
            float u, power; int idx = ThorLightning.Current(lt, out u, out power);
            float I = ThorLightning.Strike(lt), flash = Math.Min(1f, I);
            float cx = 200 + (hh.X - 200) * .3f, cy = -190;   // 구름은 머리 위 중앙 쪽에(해머가 한쪽 끝이어도 잘리지 않게)
            PointF cloudMouth = Pt(cx, cy + 24);

            // 1. 먹구름
            {
                Color hi = Mix(Color.FromArgb(58, 64, 90), Color.FromArgb(210, 228, 255), flash);
                Color lo = Mix(Color.FromArgb(32, 36, 56), Color.FromArgb(150, 188, 255), flash);
                float[,] puffs = { { -98, 10, 70, 34 }, { -46, -14, 78, 46 }, { 10, -24, 86, 52 }, { 64, -8, 78, 44 }, { 102, 12, 64, 31 }, { -8, 16, 112, 32 } };
                for (int pass = 0; pass < 2; pass++)
                    for (int i = 0; i < 6; i++)
                    {
                        float sway = (float)Math.Sin(lt * 1.6 + i * 1.3) * 3.5f, off = pass == 0 ? 9 : 0;
                        g.FillEllipse(B(Al(pass == 0 ? lo : hi, 240 * fade)), cx + puffs[i, 0] * 1.2f - puffs[i, 2] * 1.2f + sway, cy + puffs[i, 1] * 1.2f - puffs[i, 3] * 1.2f + off, puffs[i, 2] * 2.4f, puffs[i, 3] * 2.4f);
                    }
                if (flash > .05f) Glow(cx, cy + 28, 150, 60, Al(Color.FromArgb(235, 245, 255), 210 * flash));
            }

            // 2. 묠니르에 전기가 차오른다(구름과 묠니르 사이에 가는 전류가 깜빡이고, 머리 둘레로 불꽃이 튄다)
            {
                Glow(hh.X, hh.Y, 24 + 34 * charge, 24 + 34 * charge, Al(Color.FromArgb(170, 225, 255), (70 + 130 * charge) * fade));
                if (idx < 0 && charge > .4f && (frame / 2) % 3 == 0)
                    StrokeThin(BoltPath(cloudMouth, hh, 900 + frame, 18, 9), .55f * fade, 2f);
                var rs = new Random(frame * 13 + 1); int n = 2 + (int)(5 * charge);
                for (int i = 0; i < n; i++)
                {
                    double ang = rs.NextDouble() * Math.PI * 2; float r0 = 10 + rs.Next(10), r1 = 40 + 36 * charge + rs.Next(26);
                    PointF s = Pt(hh.X + (float)Math.Cos(ang) * r0, hh.Y + (float)Math.Sin(ang) * r0), e = Pt(hh.X + (float)Math.Cos(ang) * r1, hh.Y + (float)Math.Sin(ang) * r1);
                    StrokeThin(BoltPath(s, e, frame * 31 + i, 10, 3), (.45f + .5f * charge) * fade, 2.2f);
                }
            }

            // 3. 낙뢰: 구름 → 묠니르. 굵은 본줄기 + 갈래 + 번쩍이는 곁가지
            if (idx >= 0 && I > .02f)
            {
                float a = Math.Min(1f, I * 1.15f), sc = power;
                PointF top = Pt(cx + (idx - 1) * 26, cy + 24);
                PointF[] main = BoltPath(top, hh, 1000 + idx * 77, 30, 13);
                StrokeBolt(main, a, sc);

                var rf = new Random(500 + idx * 13); int forks = 3 + idx;
                for (int i = 0; i < forks; i++)
                {
                    PointF s = main[rf.Next(2, main.Length - 4)]; float dir = rf.Next(2) == 0 ? -1 : 1;
                    PointF e = Pt(s.X + dir * (55 + rf.Next(80)), Math.Min(hh.Y - 25, s.Y + 45 + rf.Next(80)));
                    StrokeBolt(BoltPath(s, e, 600 + idx * 41 + i, 16, 6), a * .8f, sc * .38f);
                }
                var rg = new Random(frame * 29 + idx);
                for (int i = 0; i < 2; i++)   // 프레임마다 모양이 바뀌는 곁가지 = 지직거리는 느낌
                {
                    PointF s = main[rg.Next(1, main.Length - 3)]; float dir = rg.Next(2) == 0 ? -1 : 1;
                    PointF e = Pt(s.X + dir * (30 + rg.Next(60)), Math.Min(hh.Y - 20, s.Y + 30 + rg.Next(60)));
                    StrokeThin(BoltPath(s, e, frame * 7 + i, 12, 4), a * .8f, 3f);
                }

                // 두 번째·세 번째 벼락은 몸 양옆 땅까지 내리꽂는 보조 벼락이 함께 친다
                for (int i = 0; i < idx; i++)
                {
                    float side = i == 0 ? 1 : -1; PointF land = Pt(200 + side * (150 + 22 * idx), 562);
                    StrokeBolt(BoltPath(Pt(cx + side * 60, cy + 26), land, 2000 + idx * 31 + i, 34, 14), a * .9f, sc * .6f);
                    Glow(land.X, land.Y, 60 * sc, 18 * sc, Al(Color.FromArgb(200, 235, 255), 210 * a));
                }

                // 묠니르 명중: 눈부신 폭발 + 방사형 전기
                Glow(hh.X, hh.Y, 150 * sc, 150 * sc, Al(Color.FromArgb(170, 225, 255), 215 * a));
                Glow(hh.X, hh.Y, 62 * sc, 62 * sc, Al(Color.White, 255 * a));
                var rb = new Random(frame * 7 + 3);
                for (int i = 0; i < 12; i++)
                {
                    double ang = rb.NextDouble() * Math.PI * 2; float len = (40 + rb.Next(70)) * sc;
                    float ex = Math.Max(-40f, Math.Min(440f, hh.X + (float)Math.Cos(ang) * len));
                    StrokeThin(BoltPath(hh, Pt(ex, hh.Y + (float)Math.Sin(ang) * len), frame * 11 + i, 12, 3), a, 2.8f);
                }
            }

            // 4. 전신 감전: 몸이 하얗게 번쩍이고 온몸을 타고 전기가 튄다(번개 사이에도 잔전류가 남는다)
            {
                float shock = Math.Max(I, (lt > 1.0f && lt < ThorLightning.End - .3f) ? .22f : 0f) * fade;
                if (shock > .05f)
                {
                    float ccx = 200 + bx, ccy = 335 + by;
                    if (flash > .05f) Glow(ccx, ccy, 140, 275, Al(Color.FromArgb(215, 238, 255), 110 * flash));
                    var rs = new Random(frame * 17 + 5); int n = 3 + (int)(9 * Math.Min(1f, shock));
                    for (int i = 0; i < n; i++)
                    {
                        double ang = rs.NextDouble() * Math.PI * 2;
                        PointF s = Pt(ccx + (float)Math.Cos(ang) * (60 + rs.Next(50)), ccy + (float)Math.Sin(ang) * (120 + rs.Next(95)));
                        double dir = rs.NextDouble() * Math.PI * 2; float len = 40 + rs.Next(60);
                        PointF e = Pt(s.X + (float)Math.Cos(dir) * len, s.Y + (float)Math.Sin(dir) * len);
                        StrokeThin(BoltPath(s, e, frame * 23 + i, 14, 4), Math.Min(1f, shock + .25f), 2.6f);
                    }
                }
            }

            // 5. 지면 충격파: 발밑에서 퍼져 나가는 전기 고리와 땅을 기는 방전
            if (idx >= 0)
            {
                float gx = 200, gy = 566;
                float rx = 34 + 190 * (float)Math.Sqrt(u) * (.7f + .3f * power), ga = Math.Max(0f, Math.Min(1f, (1 - u) * 1.3f)) * Math.Min(1f, power + .2f);
                Glow(gx, gy, 100 * power, 24 * power, Al(Color.FromArgb(175, 228, 255), 170 * ga));
                float ry = rx * .12f;
                g.DrawEllipse(Pn(Al(Color.FromArgb(100, 190, 255), 200 * ga), 7f), gx - rx, gy - ry, rx * 2, ry * 2);
                g.DrawEllipse(Pn(Al(Color.White, 240 * ga), 2.4f), gx - rx, gy - ry, rx * 2, ry * 2);
                var rc = new Random(frame * 19 + idx);
                for (int i = 0; i < 4 + idx * 2; i++)
                {
                    float side = i % 2 == 0 ? 1 : -1, x0 = gx + side * (14 + rc.Next(30)), x1 = gx + side * (80 + rc.Next((int)(120 + 40 * power)));
                    StrokeThin(BoltPath(Pt(x0, gy), Pt(x1, gy - rc.Next(16)), frame * 5 + i, 9, 6), ga, 2.4f);
                }
            }
        }

        private void DrawDefaultMjolnir()
        {
            PointF hand = wrist[1];
            var st = g.Save();
            g.TranslateTransform(hand.X, hand.Y);

            // 망치 자루
            g.FillRectangle(B(Color.FromArgb(120, 80, 50)), -4, -14, 8, 36);
            g.DrawRectangle(ink, -4, -14, 8, 36);

            // 망치 머리
            g.FillRectangle(B(Color.FromArgb(195, 205, 215)), -18, -44, 36, 30);
            g.DrawRectangle(ink, -18, -44, 36, 30);
            g.FillRectangle(B(Color.White), -12, -40, 24, 8); // 금속 광택

            g.Restore(st);
        }

        private void DrawHeroAura()
        {
            float bx = p.BodyX, by = p.BodyY;
            using (var pn = Pn(Color.FromArgb((int)(120 * p.Aura), p.AuraColor.R, p.AuraColor.G, p.AuraColor.B), 6f))
            {
                g.DrawEllipse(pn, 200 - 110 + bx, 120 + by, 220, 440);
            }
        }
    }
}

namespace SuperHeroes
{
    /// <summary>토르 '천둥 벼락 소환'의 시간표. lt = 번개 소환이 시작된 뒤 지난 초.</summary>
    internal static class ThorLightning
    {
        /// <summary>{시작, 지속, 세기} — 세 번 내리꽂으며 점점 강해진다. 두 번째가 "콰광--!!" 대사와 맞는다.</summary>
        internal static readonly float[][] Strikes = {
            new[] { 1.6f, .55f, .6f }, new[] { 2.7f, .75f, 1.0f }, new[] { 3.6f, .95f, 1.35f }
        };
        internal const float End = 5.1f;

        /// <summary>지금 치고 있는 벼락 번호(없으면 -1), 그 진행도(0..1), 세기.</summary>
        internal static int Current(float lt, out float u, out float power)
        {
            u = 0; power = 0;
            for (int i = 0; i < Strikes.Length; i++)
            {
                float s = Strikes[i][0], d = Strikes[i][1];
                if (lt >= s && lt < s + d) { u = (lt - s) / d; power = Strikes[i][2]; return i; }
            }
            return -1;
        }

        /// <summary>번쩍임 밝기(0..세기). 내리꽂는 순간 최대, 이후 지직거리며 사그라든다.</summary>
        internal static float Strike(float lt)
        {
            float u, power; int i = Current(lt, out u, out power);
            if (i < 0) return 0;
            float v = (float)Math.Pow(1 - u, 1.4);
            if (u < .12f) v = 1;
            else if (((int)(lt * 28)) % 3 == 0) v *= .45f;
            return v * power;
        }
    }
}
