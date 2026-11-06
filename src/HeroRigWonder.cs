using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace SuperHeroes
{
    /// <summary>
    /// 원더우먼(다이애나, 테미스키라의 아마존 전사) 그리기.
    /// 고증: 붉은 별이 박힌 황금 티아라 · 긴 흑발 · 황금 독수리가 새겨진 붉은 흉갑 · 별무늬 푸른 스커트 ·
    /// 황금 벨트와 진실의 올가미 · 은색 팔찌(Bracelets of Submission) · 흰 줄무늬 붉은 부츠 · 검과 방패.
    /// 앞(바라보는 쪽)은 리그 좌표에서 항상 -x 방향이다(좌우 반전은 Run 이 해 준다).
    /// </summary>
    internal sealed partial class HeroPainter
    {
        private static readonly Color cBracer = Color.FromArgb(206, 212, 226);       // 은팔찌
        private static readonly Color cGold = Color.FromArgb(240, 196, 60);
        private static readonly Color cWonderHair = Color.FromArgb(26, 22, 32);
        private static readonly Color cWonderHairHi = Color.FromArgb(78, 70, 98);

        private static float Lerp(float a, float b, float t) { return a + (b - a) * t; }

        private static PointF[] StarPts(float cx, float cy, float r, float rotDeg = -90f)
        {
            var pts = new PointF[10];
            for (int i = 0; i < 10; i++)
            {
                double a = (rotDeg + i * 36) * Math.PI / 180.0; float rr = i % 2 == 0 ? r : r * .42f;
                pts[i] = Pt(cx + (float)Math.Cos(a) * rr, cy + (float)Math.Sin(a) * rr);
            }
            return pts;
        }

        private void Star(float cx, float cy, float r, Color fill, bool edge = false, float rot = -90f)
        {
            var pts = StarPts(cx, cy, r, rot);
            g.FillPolygon(B(fill), pts);
            if (edge) g.DrawPolygon(ink, pts);
        }

        // ───────── 몸 ─────────

        /// <summary>붉은 흉갑 + 황금 독수리 (어깨와 팔은 맨살).</summary>
        private void DrawWonderChest(float cx, float cy)
        {
            float wy = waistY + p.BodyY, top = cy + 26;
            var bustier = new[]
            {
                Pt(cx - topW * .50f, top + 6), Pt(cx - topW * .26f, top - 6), Pt(cx, top + 8), Pt(cx + topW * .26f, top - 6), Pt(cx + topW * .50f, top + 6),
                Pt(cx + botW * .52f, wy), Pt(cx - botW * .52f, wy)
            };
            Fill(bustier, cSuit);
            using (var pn = Pn(cGold, 3.4f))
                g.DrawLines(pn, new[] { Pt(cx - topW * .50f, top + 6), Pt(cx - topW * .26f, top - 6), Pt(cx, top + 8), Pt(cx + topW * .26f, top - 6), Pt(cx + topW * .50f, top + 6) });

            // 황금 독수리: 활짝 편 날개
            float ey = cy + 46;
            var wingL = new[]
            {
                Pt(cx - 3, ey + 2), Pt(cx - 15, ey - 9), Pt(cx - 31, ey - 11), Pt(cx - 46, ey - 4), Pt(cx - 39, ey + 4), Pt(cx - 50, ey + 11),
                Pt(cx - 37, ey + 13), Pt(cx - 43, ey + 22), Pt(cx - 27, ey + 19), Pt(cx - 16, ey + 25), Pt(cx - 3, ey + 30)
            };
            var eagle = new PointF[wingL.Length * 2];
            for (int i = 0; i < wingL.Length; i++) { eagle[i] = wingL[i]; eagle[eagle.Length - 1 - i] = Pt(2 * cx - wingL[i].X, wingL[i].Y); }
            g.FillPolygon(B(cGold), eagle);
            g.DrawPolygon(ink, eagle);
            var body = new[] { Pt(cx - 4, ey - 2), Pt(cx + 4, ey - 2), Pt(cx + 7, ey + 28), Pt(cx, ey + 38), Pt(cx - 7, ey + 28) };
            g.FillPolygon(B(Color.FromArgb(252, 218, 96)), body);
            g.DrawPolygon(ink, body);
            g.FillEllipse(B(Color.FromArgb(252, 218, 96)), cx - 4.5f, ey - 9, 9, 9);
            g.DrawEllipse(ink, cx - 4.5f, ey - 9, 9, 9);
        }

        /// <summary>황금 벨트 + 별무늬 푸른 스커트 + 허리에 걸린 진실의 올가미.</summary>
        private void DrawWonderSkirt(float cx, float wy, float hy)
        {
            float sb = hy + 52;
            var skirt = new[]
            {
                Pt(cx - botW * .5f, wy), Pt(cx - botW * .80f, sb), Pt(cx - botW * .34f, sb + 9), Pt(cx, sb + 4),
                Pt(cx + botW * .34f, sb + 9), Pt(cx + botW * .80f, sb), Pt(cx + botW * .5f, wy)
            };
            Fill(skirt, cSuitAccent);
            // 하얀 별들
            Star(cx - 34, wy + 24, 6, cWhite); Star(cx + 6, wy + 16, 5.5f, cWhite); Star(cx + 38, wy + 30, 6, cWhite);
            Star(cx - 10, wy + 40, 5.5f, cWhite); Star(cx - 52, wy + 46, 5, cWhite); Star(cx + 54, wy + 48, 5, cWhite); Star(cx + 22, wy + 52, 4.5f, cWhite);

            // 황금 벨트와 가운데 붉은 별
            var belt = new RectangleF(cx - botW * .5f, wy - 4, botW, 15);
            g.FillRectangle(B(cGold), belt);
            g.DrawRectangle(ink, belt.X, belt.Y, belt.Width, belt.Height);
            g.FillEllipse(B(cGold), cx - 14, wy - 7, 28, 21); g.DrawEllipse(ink, cx - 14, wy - 7, 28, 21);
            Star(cx, wy + 3.5f, 7.5f, cSuit, true);

            // 허리춤의 황금 올가미 (쥐고 있는 동안에는 손에 있다)
            if (p.LassoSpin < .05f)
            {
                float lx = cx + botW * .5f + 8, ly = wy + 26;
                using (var pn = Pn(Color.FromArgb(214, 160, 40), 5.5f))
                {
                    g.DrawEllipse(pn, lx - 15, ly - 17, 30, 34);
                    g.DrawEllipse(pn, lx - 10, ly - 12, 20, 24);
                    g.DrawLine(pn, lx - 8, ly + 14, lx - 18, ly + 40);
                }
                using (var pn = Pn(Color.FromArgb(255, 230, 130), 2f)) g.DrawEllipse(pn, lx - 15, ly - 17, 30, 34);
            }
        }

        /// <summary>은색 팔찌 (Bracelets of Submission).</summary>
        private void DrawBracer(PointF wr, PointF elbow)
        {
            var pts = new[]
            {
                Pt(elbow.X - armW * .46f, elbow.Y + 10), Pt(wr.X - armW * .42f, wr.Y + 3),
                Pt(wr.X + armW * .42f, wr.Y + 3), Pt(elbow.X + armW * .46f, elbow.Y + 10)
            };
            Fill(pts, cBracer);
            using (var pn = Pn(Color.FromArgb(190, 255, 255, 255), 2.4f))
                g.DrawLine(pn, Lerp(elbow.X, wr.X, .15f) - armW * .26f, Lerp(elbow.Y + 10, wr.Y, .12f), wr.X - armW * .24f, wr.Y - 2);
            Star((elbow.X + wr.X) / 2f, Lerp(elbow.Y + 10, wr.Y, .5f), 5.5f, cGold);
        }

        // ───────── 머리 ─────────

        private void DrawWonderHead()
        {
            float rx = headRx * .86f, ry = headRy * .92f;
            float sway = p.CapeWind * .5f + (float)Math.Sin(p.SigPhase * 12) * 3f;

            // 목
            // (망토 깃이 없어서 목이 흉갑 윗단까지 이어져야 머리가 몸에서 떠 보이지 않는다)
            var neck = new[] { Pt(-22, 50), Pt(-26, 118), Pt(26, 118), Pt(22, 50) };
            Fill(neck, cSkinSh);
            g.FillPolygon(B(cSkin), new[] { Pt(-20, 76), Pt(-23, 114), Pt(23, 114), Pt(20, 76) });

            // 머리 뒤쪽의 풍성한 머리카락
            var hairBack = new[]
            {
                Pt(-rx * 1.02f, -ry * .55f), Pt(-rx * 1.2f, 0), Pt(-rx * 1.18f + sway * .4f, ry * .75f),
                Pt(rx * 1.18f + sway * .4f, ry * .75f), Pt(rx * 1.2f, 0), Pt(rx * 1.02f, -ry * .55f), Pt(0, -ry * 1.12f)
            };
            g.FillPolygon(B(cWonderHair), hairBack); g.DrawPolygon(ink, hairBack);

            // 얼굴
            var face = new[]
            {
                Pt(0, ry), Pt(-rx * .72f, ry * .8f), Pt(-rx * .98f, 0), Pt(-rx * .84f, -ry * .75f),
                Pt(0, -ry), Pt(rx * .84f, -ry * .75f), Pt(rx * .98f, 0), Pt(rx * .72f, ry * .8f)
            };
            Fill(face, cSkin);

            // 귀 + 황금 귀걸이
            g.FillEllipse(B(cSkin), -rx - 6, -12, 14, 26); g.DrawEllipse(ink, -rx - 6, -12, 14, 26);
            g.FillEllipse(B(cSkin), rx - 8, -12, 14, 26); g.DrawEllipse(ink, rx - 8, -12, 14, 26);
            g.FillEllipse(B(cGold), -rx - 5, 15, 9, 9); g.DrawEllipse(ink, -rx - 5, 15, 9, 9);
            g.FillEllipse(B(cGold), rx - 4, 15, 9, 9); g.DrawEllipse(ink, rx - 4, 15, 9, 9);

            // 눈 · 눈썹 · 속눈썹 · 입
            DrawEyesAndEyebrows(Color.FromArgb(46, 128, 214), false);
            float eyeY = -headRy * .05f;
            using (var pn = Pn(cInk, 2.6f))
            {
                g.DrawLine(pn, -40, eyeY - 1, -48, eyeY - 8);
                g.DrawLine(pn, -39, eyeY + 3, -47, eyeY - 1);
                g.DrawLine(pn, 40, eyeY - 1, 48, eyeY - 8);
                g.DrawLine(pn, 39, eyeY + 3, 47, eyeY - 1);
            }
            DrawHeroMouth();

            // 윤기 나는 흑발: 이마를 덮는 비대칭 가르마 앞머리
            var cap = new[]
            {
                Pt(-rx * 1.0f, -8), Pt(-rx * 1.04f, -ry * .8f), Pt(-rx * .5f, -ry * 1.16f), Pt(rx * .5f, -ry * 1.16f), Pt(rx * 1.04f, -ry * .8f), Pt(rx * 1.0f, -8),
                Pt(rx * .86f, -ry * .42f), Pt(rx * .34f, -ry * .62f), Pt(-rx * .26f, -ry * .56f), Pt(-rx * .62f, -ry * .34f), Pt(-rx * .86f, -ry * .30f)
            };
            g.FillPolygon(B(cWonderHair), cap); g.DrawPolygon(ink, cap);
            using (var pn = Pn(cWonderHairHi, 3f))
            {
                var hl = new GraphicsPath();
                hl.AddBezier(Pt(-rx * .55f, -ry * 1.0f), Pt(-rx * .2f, -ry * 1.08f), Pt(rx * .25f, -ry * 1.08f), Pt(rx * .6f, -ry * .96f));
                g.DrawPath(pn, hl);
            }

            // 어깨 위로 흘러내리는 긴 머리 (바람에 날린다)
            var lockL = new[] { Pt(-rx * .98f, -ry * .1f), Pt(-rx * 1.18f, ry * .4f), Pt(-rx * 1.1f + sway * .5f, ry * 1.28f), Pt(-rx * .8f + sway * .5f, ry * 1.24f), Pt(-rx * .74f, ry * .5f), Pt(-rx * .86f, 0) };
            var lockR = new[] { Pt(rx * .98f, -ry * .1f), Pt(rx * 1.18f, ry * .4f), Pt(rx * 1.1f + sway * .5f, ry * 1.28f), Pt(rx * .8f + sway * .5f, ry * 1.24f), Pt(rx * .74f, ry * .5f), Pt(rx * .86f, 0) };
            g.FillPolygon(B(cWonderHair), lockL); g.DrawPolygon(ink, lockL);
            g.FillPolygon(B(cWonderHair), lockR); g.DrawPolygon(ink, lockR);

            // 붉은 별이 박힌 황금 티아라
            var tiara = new[]
            {
                Pt(-rx * .88f, -ry * .40f), Pt(-rx * .44f, -ry * .56f), Pt(0, -ry * .74f), Pt(rx * .44f, -ry * .56f), Pt(rx * .88f, -ry * .40f),
                Pt(rx * .88f, -ry * .30f), Pt(rx * .44f, -ry * .44f), Pt(0, -ry * .58f), Pt(-rx * .44f, -ry * .44f), Pt(-rx * .88f, -ry * .30f)
            };
            g.FillPolygon(B(cGold), tiara); g.DrawPolygon(ink, tiara);
            Star(0, -ry * .565f, 10.5f, Color.FromArgb(214, 30, 48), true);
        }

        // ───────── 시그니처 이펙트 ─────────

        private void DrawWonderFx()
        {
            float bx = p.BodyX, by = p.BodyY;
            if (p.Bracers > .05f) DrawBracerDeflect();
            if (p.LassoSpin > .05f) DrawLassoOfTruth();
            if (p.ShieldGuard > .05f) DrawSwordAndShield();
            if (p.AmazonFlight > .05f) DrawAmazonFlight(bx, by);
        }

        /// <summary>날아오는 총알을 은팔찌로 튕겨 내는 연출.</summary>
        private void DrawBracerDeflect()
        {
            float a = p.Bracers, bt = p.BracerT;
            PointF imp = Pt((wrist[0].X + wrist[1].X) * .5f - 14, (wrist[0].Y + wrist[1].Y) * .5f - 4);
            Glow(imp.X, imp.Y, 70, 62, Al(Color.FromArgb(255, 226, 120), 120 * a));

            // 팔찌에서 퍼지는 동그란 파문
            float rp = (bt * 2.6f) % 1f;
            g.DrawEllipse(Pn(Al(Color.FromArgb(255, 240, 170), 190 * (1 - rp) * a), 4f), imp.X - 22 - 60 * rp, imp.Y - 22 - 60 * rp, 44 + 120 * rp, 44 + 120 * rp);

            float[] lane = { -26, 8, -8, 24, -18, 14, 0 };
            for (int i = 0; i < 7; i++)
            {
                float ph = (bt * 2.3f + i * .431f) % 1f;
                float ly = lane[i] * .9f;
                if (ph < .5f)
                {
                    // 날아오는 총알과 꼬리
                    float u = ph / .5f, x = imp.X - 300 + 290 * u, y = imp.Y + ly * (1 - u);
                    g.DrawLine(Pn(Al(Color.FromArgb(255, 245, 200), 170 * a), 3f), x - 56, y - ly * .18f * (1 - u), x, y);
                    g.FillEllipse(B(Al(Color.FromArgb(255, 214, 90), 255 * a)), x - 7, y - 3.5f, 14, 7);
                    g.FillEllipse(B(Al(Color.White, 240 * a)), x - 3, y - 2, 6, 4);
                }
                else
                {
                    // 팅! 튕겨 나가는 총알
                    float u = (ph - .5f) / .5f, dirY = i % 2 == 0 ? -1f : .9f;
                    float x = imp.X - 8 + 120 * u * .55f, y = imp.Y + dirY * 150 * u;
                    float fade = (1 - u) * a;
                    g.DrawLine(Pn(Al(Color.FromArgb(255, 230, 140), 190 * fade), 2.4f), x, y, x + 26 * .55f, y - dirY * 26);
                    g.FillEllipse(B(Al(Color.White, 240 * fade)), x - 3, y - 3, 6, 6);
                    if (u < .26f)
                    {
                        // 부딪히는 순간: 황금 충격 고리 + 번쩍임 + "TING!"
                        float uu = u / .26f, rr = 20 + 84 * uu;
                        g.DrawEllipse(Pn(Al(Color.FromArgb(255, 220, 110), 200 * (1 - uu) * a), 6f), imp.X - rr, imp.Y + ly * .2f - rr, rr * 2, rr * 2);
                        g.DrawEllipse(Pn(Al(Color.White, 245 * (1 - uu) * a), 2.2f), imp.X - rr, imp.Y + ly * .2f - rr, rr * 2, rr * 2);
                        Glint(imp.X, imp.Y + ly * .2f, 30 + 26 * (1 - uu), Color.FromArgb(255, 232, 140), (1 - uu) * a, i * 23f);
                        if (i % 3 == 0) ComicText("TING!", imp.X + 64, imp.Y - 58 + ly, 26, -8f + i * 4f, Color.FromArgb(255, 200, 60), (1 - uu) * a);
                        // 부딪히는 순간의 불꽃
                        float sr = 12 + 22 * (u / .26f);
                        Star(imp.X, imp.Y + ly * .2f, sr, Al(Color.FromArgb(255, 238, 150), 255 * (1 - u / .26f) * a), false, i * 17f);
                        Star(imp.X, imp.Y + ly * .2f, sr * .5f, Al(Color.White, 255 * (1 - u / .26f) * a), false, i * 17f + 18);
                    }
                }
            }
        }

        /// <summary>진실의 올가미: 꺼내기 → 머리 위로 휘돌리기 → 던지기 → 황금빛 진실의 빛.</summary>
        private void DrawLassoOfTruth()
        {
            float lt = p.LassoT, a = p.LassoSpin;
            PointF hand = wrist[1];
            Color rope = Color.FromArgb(230, 176, 44), shine = Color.FromArgb(255, 238, 150);
            Action<PointF[], float> pass = (pts, w) =>
            {
                g.DrawLines(Pn(Al(Color.FromArgb(120, 70, 12), 220 * a), w + 3f), pts);
                g.DrawLines(Pn(Al(rope, 255 * a), w), pts);
                g.DrawLines(Pn(Al(shine, 230 * a), Math.Max(1.4f, w * .35f)), pts);
            };

            if (lt < 1.4f)
            {
                // 허리에서 올가미를 풀어 손에 쥔다
                float u = lt / 1.4f;
                PointF hip = Pt(200 + botW * .5f + 8 + p.BodyX, waistY + 26 + p.BodyY);
                var coil = new[] { hand, Pt(Lerp(hip.X, hand.X, u) - 18, Lerp(hip.Y, hand.Y, u) + 28), hip };
                pass(coil, 5f);
                g.DrawEllipse(Pn(Al(rope, 255 * a), 5f), hand.X - 16, hand.Y - 14, 32, 28);
            }
            else if (lt < 3.3f)
            {
                // 머리 위에서 빙글빙글 도는 올가미
                float spin = (lt - 1.4f) * 15f;
                PointF c = Pt(hand.X, hand.Y - 76);
                for (int k = 0; k < 3; k++)
                {
                    var st = g.Save();
                    g.TranslateTransform(c.X, c.Y); g.RotateTransform(spin * 57.3f * .5f + k * 24);
                    g.DrawEllipse(Pn(Al(rope, (200 - k * 55) * a), 5.5f - k), -92, -26, 184, 52);
                    g.DrawEllipse(Pn(Al(shine, (160 - k * 50) * a), 2f), -92, -26, 184, 52);
                    g.Restore(st);
                }
                g.DrawLine(Pn(Al(rope, 255 * a), 5f), hand.X, hand.Y, c.X, c.Y);
                Glow(c.X, c.Y, 110, 44, Al(Color.FromArgb(255, 220, 110), 110 * a));
                for (int i = 0; i < 6; i++)
                {
                    double ang = spin + i * 1.05; float sx = c.X + (float)Math.Cos(ang) * 92, sy = c.Y + (float)Math.Sin(ang) * 26;
                    Star(sx, sy, 5.5f, Al(Color.White, 240 * a));
                }
            }
            else
            {
                // 던지기 → 거짓을 꿰뚫는 포박
                float u = Math.Min(1f, (lt - 3.3f) / 1.1f), e = 1 - (1 - u) * (1 - u);
                PointF tgt = Pt(200 - 70 - 200 * e + p.BodyX, 392 + p.BodyY - 30 * (1 - e));
                PointF mid = Pt((hand.X + tgt.X) / 2, Math.Max(hand.Y, tgt.Y) - 70 * (1 - e) + 20);
                var path = new GraphicsPath(); path.AddBezier(hand, Pt(hand.X - 40, hand.Y - 40), Pt(mid.X, mid.Y - 20 * (1 - u)), tgt);
                path.Flatten(); trash.Add(path);
                pass(path.PathPoints, 5f);
                float lr = 36 - 10 * Math.Max(0, (lt - 4.4f) / 1.2f); if (lr < 20) lr = 20;
                g.DrawEllipse(Pn(Al(rope, 255 * a), 6f), tgt.X - lr, tgt.Y - lr * .7f, lr * 2, lr * 1.4f);
                g.DrawEllipse(Pn(Al(shine, 230 * a), 2f), tgt.X - lr, tgt.Y - lr * .7f, lr * 2, lr * 1.4f);
                if (lt > 4.1f)
                {
                    // 진실의 광채: 퍼지는 황금빛 고리와 방사형 빛
                    float tq = Math.Min(1f, (lt - 4.1f) / 1.6f), fade = (1 - tq * .7f) * a;
                    Glow(tgt.X, tgt.Y, 120 * (.5f + tq), 100 * (.5f + tq), Al(Color.FromArgb(255, 228, 120), 170 * fade));
                    for (int i = 0; i < 10; i++)
                    {
                        double ang = i * Math.PI / 5 + lt * .6; float r1 = lr + 6, r2 = lr + 30 + 34 * tq;
                        g.DrawLine(Pn(Al(shine, 230 * fade), 3f), tgt.X + (float)Math.Cos(ang) * r1, tgt.Y + (float)Math.Sin(ang) * r1 * .8f, tgt.X + (float)Math.Cos(ang) * r2, tgt.Y + (float)Math.Sin(ang) * r2 * .8f);
                    }
                    Star(tgt.X, tgt.Y - 4, 16 + 6 * (float)Math.Sin(lt * 9), Al(Color.White, 255 * fade));
                }
            }
        }

        /// <summary>아마존의 방패와 검.</summary>
        private void DrawSwordAndShield()
        {
            float sg = Math.Min(1f, p.ShieldGuard);

            // 방패: 둥근 황금 테두리 + 붉은 바탕 + 별
            PointF sc = Pt(wrist[0].X - 26, wrist[0].Y - 6);
            float r = 62 * sg;
            if (r > 4)
            {
                g.FillEllipse(B(cGold), sc.X - r, sc.Y - r, r * 2, r * 2); g.DrawEllipse(ink, sc.X - r, sc.Y - r, r * 2, r * 2);
                float r2 = r * .8f; g.FillEllipse(B(cSuit), sc.X - r2, sc.Y - r2, r2 * 2, r2 * 2); g.DrawEllipse(ink, sc.X - r2, sc.Y - r2, r2 * 2, r2 * 2);
                float r3 = r * .5f; g.DrawEllipse(Pn(Al(cGold, 255), 3f), sc.X - r3, sc.Y - r3, r3 * 2, r3 * 2);
                Star(sc.X, sc.Y, r * .36f, cGold, true);
                g.DrawArc(Pn(Al(Color.White, 190), 3f), sc.X - r * .9f, sc.Y - r * .9f, r * 1.8f, r * 1.8f, 200, 55);   // 광택
            }

            // 검: 오른손에 쥐고 휘두른다 (0 도 = 위, 음수 = 앞쪽(-x)으로 기울어짐)
            PointF h = wrist[1];
            float a0 = 28f, a1 = -128f, ang = Lerp(a0, a1, Math.Max(0f, Math.Min(1f, p.SwordSlash)));
            if (p.SwordSlash > .08f && p.SwordSlash < .98f)
            {
                float swp = ang - a0;
                g.DrawArc(Pn(Al(Color.White, 150), 11f), h.X - 132, h.Y - 132, 264, 264, -90 + a0, swp);
                g.DrawArc(Pn(Al(Color.FromArgb(255, 222, 110), 230), 4f), h.X - 132, h.Y - 132, 264, 264, -90 + a0, swp);
            }
            // 방패에 막혀 튀는 충격(1.7~2.5초)과 검기(두 번의 베기에서 날아가는 황금 초승달)
            float T = p.SigT;
            if (T >= 1.7f && T < 2.5f)
            {
                float u = (T - 1.7f) / .8f, fa = 1 - u;
                g.DrawEllipse(Pn(Al(Color.FromArgb(255, 220, 110), 200 * fa * sg), 6f), sc.X - r - 26 * u, sc.Y - r - 26 * u, (r + 26 * u) * 2, (r + 26 * u) * 2);
                for (int i = 0; i < 6; i++) Glint(sc.X + (Hash01(i, 1) * 2 - 1) * r * 1.2f, sc.Y + (Hash01(i, 2) * 2 - 1) * r * 1.2f, 10 + 10 * Hash01(i, 3), Color.FromArgb(255, 232, 140), fa * (float)Math.Abs(Math.Sin(T * 30 + i)), i * 30f);
            }
            float[] wave = { 3.25f, 4.55f };
            for (int i = 0; i < wave.Length; i++)
            {
                float u = (T - wave[i]) / .55f; if (T <= 0 || u < 0 || u > 1) continue;
                float x = h.X - 40 - u * 260f, fa = 1 - u * u;
                for (int k = 0; k < 3; k++)
                {
                    float rad = 70 + k * 12, off = k * 12;
                    g.DrawArc(Pn(Al(Color.FromArgb(255, 222, 110), (170 - k * 40) * fa), 14f - k * 4), CxClamp(x + off) - rad, h.Y - 30 - rad, rad * 2, rad * 2, 120, 120);
                    g.DrawArc(Pn(Al(Color.White, (250 - k * 60) * fa), 4f - k), CxClamp(x + off) - rad, h.Y - 30 - rad, rad * 2, rad * 2, 120, 120);
                }
                Glow(CxClamp(x), h.Y - 30, 60, 90, Al(Color.FromArgb(255, 226, 130), 110 * fa));
            }
            var st = g.Save();
            g.TranslateTransform(h.X, h.Y); g.RotateTransform(ang);
            g.FillRectangle(B(Color.FromArgb(120, 76, 44)), -4, -6, 8, 30); g.DrawRectangle(ink, -4, -6, 8, 30);              // 손잡이
            g.FillRectangle(B(cGold), -17, -14, 34, 8); g.DrawRectangle(ink, -17, -14, 34, 8);                                  // 날밑
            var blade = new[] { Pt(-6, -14), Pt(6, -14), Pt(5, -138), Pt(0, -156), Pt(-5, -138) };
            g.FillPolygon(B(Color.FromArgb(222, 228, 238)), blade); g.DrawPolygon(ink, blade);
            g.DrawLine(Pn(Al(Color.White, 220), 2f), 0, -20, 0, -140);
            g.Restore(st);
        }

        /// <summary>별빛 꼬리를 끌며 나는 비행.</summary>
        private void DrawAmazonFlight(float bx, float by)
        {
            float f = p.AmazonFlight, ft = p.FlightT;
            Glow(200 + bx, 520 + by, 100, 30, Al(Color.FromArgb(255, 220, 110), 100 * f));

            // 이륙·착지 충격파(황금 고리 + 먼지)와 비행 중 황금 속도선
            if (p.SigT > 0)
            {
                float T = p.SigT;
                foreach (float t0 in new[] { 1.4f, 5.0f })
                {
                    float u = (T - t0) / (t0 < 2f ? 1.0f : 1.1f); if (u < 0 || u > 1) continue;
                    for (int i = 0; i < 2; i++)
                    {
                        float ur = u - i * .12f; if (ur <= 0) continue;
                        GroundRing(200 + bx, 568, 20 + 170 * (float)Math.Sqrt(ur), .17f, Color.FromArgb(255, 214, 100), 1 - ur, 4f - i);
                    }
                    for (int sd = -1; sd <= 1; sd += 2) Glow(200 + bx + sd * (30 + 80 * (1 - (float)Math.Exp(-u * 3f))), 556 - 12 * u, 30 + 20 * u, 18, Al(Color.FromArgb(236, 226, 206), 130 * (1 - u)));
                    if (t0 < 2f) Glow(200 + bx, 520, 110, 90, Al(Color.White, 220 * Math.Max(0f, 1 - u / .2f)));
                }
                if (f > .3f && T > 1.8f && T < 5.2f)
                    for (int i = 0; i < 10; i++)
                    {
                        float ph = (T * 2.2f + i * .1f) % 1f, y = 160 + by + Hash01(i, 5) * 360, len = 70 + 90 * Hash01(i, 6), x = 440 - ph * (480 + len);
                        Stroke(new[] { PtC(x, y), PtC(x + len, y) }, Al(Color.FromArgb(255, 226, 130), 150 * f), 2.4f);
                    }
            }
            for (int i = 0; i < 5; i++)
            {
                float x = 200 + bx + (i - 2) * 30, ph = (ft * 2.2f + i * .21f) % 1f;
                g.DrawLine(Pn(Al(Color.White, 150 * (1 - ph) * f), 2.6f), x, 480 + by + ph * 30, x, 560 + by + ph * 90);
            }
            for (int i = 0; i < 14; i++)
            {
                float ph = (ft * 1.7f + i * .0714f) % 1f;
                float x = 200 + bx + (i % 5 - 2) * 30 + (float)Math.Sin(i * 2.1 + ft * 3) * 12, y = shY + 130 + by + ph * 300;
                Star(x, y, 3 + 7 * (1 - ph), Al(i % 3 == 0 ? Color.White : Color.FromArgb(255, 220, 100), 245 * (1 - ph) * f), false, ft * 90 + i * 20);
            }
        }
    }
}
