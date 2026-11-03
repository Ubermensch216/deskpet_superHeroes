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
            if (p.Lightning > 0.05f)
            {
                PointF hammerHead = Pt(wrist[1].X + 15, wrist[1].Y - 50);

                using (var pnBolt = Pn(Color.FromArgb((int)(250 * p.Lightning), 120, 210, 255), 6f))
                using (var pnCore = Pn(Color.White, 3f))
                {
                    // 하늘에서 묠니르로 떨어지는 지그재그 벼락
                    PointF p0 = Pt(hammerHead.X - 30, 0);
                    PointF p1 = Pt(hammerHead.X + 20, 80);
                    PointF p2 = Pt(hammerHead.X - 15, 160);
                    PointF p3 = hammerHead;

                    g.DrawLine(pnBolt, p0.X, p0.Y, p1.X, p1.Y); g.DrawLine(pnCore, p0.X, p0.Y, p1.X, p1.Y);
                    g.DrawLine(pnBolt, p1.X, p1.Y, p2.X, p2.Y); g.DrawLine(pnCore, p1.X, p1.Y, p2.X, p2.Y);
                    g.DrawLine(pnBolt, p2.X, p2.Y, p3.X, p3.Y); g.DrawLine(pnCore, p2.X, p2.Y, p3.X, p3.Y);
                }
            }

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
