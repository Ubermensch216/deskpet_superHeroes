using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace SuperHeroes
{
    internal sealed partial class HeroPainter
    {
        private void DrawHead()
        {
            float bx = p.BodyX, by = p.BodyY;
            float hx = 200 + p.HeadX + bx;
            float hy = headCy + p.HeadY + by;
            float tilt = p.HeadTilt;

            var state = g.Save();
            g.TranslateTransform(hx, hy);
            g.RotateTransform(tilt);

            switch (who)
            {
                case Persona.Superman:
                    DrawSupermanHead();
                    break;
                case Persona.Batman:
                    DrawBatmanHead();
                    break;
                case Persona.Spiderman:
                    DrawSpidermanHead();
                    break;
                case Persona.Thor:
                    DrawThorHead();
                    break;
            }

            g.Restore(state);
        }

        private void DrawSupermanHead()
        {
            float rx = headRx * .88f, ry = headRy * .92f;

            // 목
            var neck = new[] { Pt(-24, 50), Pt(-26, 90), Pt(26, 90), Pt(24, 50) };
            Fill(neck, cSkinSh);

            // 얼굴 윤곽
            var face = new[]
            {
                Pt(0, ry),
                Pt(-rx * .75f, ry * .78f),
                Pt(-rx, 0),
                Pt(-rx * .85f, -ry * .75f),
                Pt(0, -ry),
                Pt(rx * .85f, -ry * .75f),
                Pt(rx, 0),
                Pt(rx * .75f, ry * .78f)
            };
            Fill(face, cSkin);

            // 귀
            g.FillEllipse(B(cSkin), -rx - 8, -14, 16, 28);
            g.DrawEllipse(ink, -rx - 8, -14, 16, 28);
            g.FillEllipse(B(cSkin), rx - 8, -14, 16, 28);
            g.DrawEllipse(ink, rx - 8, -14, 16, 28);

            // 눈 & 눈썹
            DrawEyesAndEyebrows(Color.FromArgb(18, 90, 200), false);

            // 입
            DrawHeroMouth();

            // 흑발 헤어 & 상징적인 'S'자 앞머리 컬 (Superman Curl)
            var hairCol = Color.FromArgb(20, 22, 28);
            var hairBack = new[]
            {
                Pt(-rx * .95f, -10),
                Pt(-rx * .98f, -ry * .85f),
                Pt(-rx * .4f, -ry * 1.12f),
                Pt(rx * .4f, -ry * 1.12f),
                Pt(rx * .98f, -ry * .85f),
                Pt(rx * .95f, -10),
                Pt(rx * .75f, -ry * .6f),
                Pt(-rx * .75f, -ry * .6f)
            };
            g.FillPolygon(B(hairCol), hairBack);
            g.DrawPolygon(ink, hairBack);

            // 앞머리 볼륨 및 이마 앞 'S' 컬
            var hairFront = new[]
            {
                Pt(-rx * .8f, -ry * .45f),
                Pt(-rx * .4f, -ry * .85f),
                Pt(0, -ry * .7f),
                Pt(rx * .5f, -ry * .88f),
                Pt(rx * .85f, -ry * .45f),
                Pt(rx * .6f, -ry * .68f),
                Pt(0, -ry * .58f),
                Pt(-rx * .55f, -ry * .65f)
            };
            g.FillPolygon(B(hairCol), hairFront);
            g.DrawPolygon(ink, hairFront);

            // S자 앞머리 컬 (이마 중앙으로 내려오는 컬)
            using (var pn = Pn(hairCol, 6.5f))
            {
                var curl = new GraphicsPath();
                curl.AddBezier(Pt(8, -ry * .6f), Pt(-12, -ry * .42f), Pt(-2, -ry * .22f), Pt(-14, -ry * .16f));
                g.DrawPath(pn, curl);
            }
        }

        private void DrawBatmanHead()
        {
            float rx = headRx * .88f, ry = headRy * .94f;
            Color cowlCol = Color.FromArgb(26, 28, 34);

            // 카울 전체 윤곽
            var cowl = new[]
            {
                Pt(0, ry),
                Pt(-rx * .78f, ry * .78f),
                Pt(-rx, 0),
                Pt(-rx * .88f, -ry * .75f),
                Pt(-rx * .75f, -ry * 1.35f), // 왼쪽 뾰족한 박쥐 귀
                Pt(-rx * .45f, -ry * .95f),
                Pt(0, -ry * .98f),
                Pt(rx * .45f, -ry * .95f),
                Pt(rx * .75f, -ry * 1.35f),  // 오른쪽 뾰족한 박쥐 귀
                Pt(rx * .88f, -ry * .75f),
                Pt(rx, 0),
                Pt(rx * .78f, ry * .78f)
            };
            Fill(cowl, cowlCol);

            // 노출된 턱과 입 (삼각형 살구색 영역)
            var chinArea = new[]
            {
                Pt(0, ry - 2),
                Pt(-rx * .52f, ry * .65f),
                Pt(-rx * .35f, ry * .24f),
                Pt(rx * .35f, ry * .24f),
                Pt(rx * .52f, ry * .65f)
            };
            Fill(chinArea, cSkin);

            // 다부지게 다문 입 & 턱선
            using (var pn = Pn(cInk, 3.2f))
            {
                g.DrawLine(pn, -18, ry * .68f, 18, ry * .68f); // 입
                g.DrawLine(pn, -10, ry * .84f, 10, ry * .84f); // 턱선
            }

            // 배트맨 시그니처: 흰색 삼각형 안광 렌즈 눈!
            float eyeY = -ry * .08f;
            var eyeL = new[] { Pt(-34, eyeY), Pt(-12, eyeY + 8), Pt(-14, eyeY - 6) };
            var eyeR = new[] { Pt(34, eyeY), Pt(12, eyeY + 8), Pt(14, eyeY - 6) };
            g.FillPolygon(B(Color.White), eyeL);
            g.DrawPolygon(ink, eyeL);
            g.FillPolygon(B(Color.White), eyeR);
            g.DrawPolygon(ink, eyeR);
        }

        private void DrawSpidermanHead()
        {
            float rx = headRx * .88f, ry = headRy * .96f;
            Color maskCol = Color.FromArgb(214, 38, 44);

            // 마스크 두상
            g.FillEllipse(B(maskCol), -rx, -ry, rx * 2, ry * 2);
            g.DrawEllipse(ink, -rx, -ry, rx * 2, ry * 2);

            // 마스크 거미줄 패턴 (미간 중심 방사선 8줄 + 동심원)
            using (var pn = Pn(Color.FromArgb(32, 34, 40), 1.8f))
            {
                PointF center = Pt(0, -ry * .12f);
                for (int i = 0; i < 8; i++)
                {
                    double ang = i * Math.PI / 4.0;
                    g.DrawLine(pn, center.X, center.Y, center.X + (float)Math.Cos(ang) * rx * 1.1f, center.Y + (float)Math.Sin(ang) * ry * 1.1f);
                }
                for (int r = 1; r <= 3; r++)
                {
                    float radX = rx * r / 3.4f;
                    float radY = ry * r / 3.4f;
                    g.DrawEllipse(pn, center.X - radX, center.Y - radY, radX * 2, radY * 2);
                }
            }

            // 스파이더맨 커다란 흰색 렌즈 눈 (검은 테두리)
            float eyeY = -ry * .05f;
            float eyeW = 34f, eyeH = 26f;
            if (p.Eyes == EyeStyle.Narrow) eyeH = 12f;
            else if (p.Eyes == EyeStyle.Shock) { eyeW = 40f; eyeH = 34f; }

            var eyeL = new[]
            {
                Pt(-46, eyeY - eyeH * .8f),
                Pt(-12, eyeY + eyeH * .4f),
                Pt(-14, eyeY + eyeH * .9f),
                Pt(-48, eyeY + eyeH * .2f)
            };
            var eyeR = new[]
            {
                Pt(46, eyeY - eyeH * .8f),
                Pt(12, eyeY + eyeH * .4f),
                Pt(14, eyeY + eyeH * .9f),
                Pt(48, eyeY + eyeH * .2f)
            };

            using (var pn = Pn(cInk, 5.5f))
            {
                g.FillPolygon(B(Color.White), eyeL);
                g.DrawPolygon(pn, eyeL);
                g.FillPolygon(B(Color.White), eyeR);
                g.DrawPolygon(pn, eyeR);
            }
        }

        private void DrawThorHead()
        {
            float rx = headRx * .88f, ry = headRy * .92f;
            Color blonde = Color.FromArgb(244, 202, 64);
            Color blondeDark = Color.FromArgb(204, 160, 36);

            // 목
            var neck = new[] { Pt(-24, 50), Pt(-26, 90), Pt(26, 90), Pt(24, 50) };
            Fill(neck, cSkinSh);

            // 풍성한 어깨 금발 머리칼 (뒤 레이어)
            var hairBackL = new[] { Pt(-rx * .9f, -10), Pt(-rx * 1.25f, 40), Pt(-rx * 1.3f, 100), Pt(-rx * .6f, 90), Pt(-rx * .5f, 40) };
            var hairBackR = new[] { Pt(rx * .9f, -10), Pt(rx * 1.25f, 40), Pt(rx * 1.3f, 100), Pt(rx * .6f, 90), Pt(rx * .5f, 40) };
            g.FillPolygon(B(blondeDark), hairBackL); g.DrawPolygon(ink, hairBackL);
            g.FillPolygon(B(blondeDark), hairBackR); g.DrawPolygon(ink, hairBackR);

            // 얼굴
            var face = new[]
            {
                Pt(0, ry), Pt(-rx * .75f, ry * .78f), Pt(-rx, 0), Pt(-rx * .85f, -ry * .75f),
                Pt(0, -ry), Pt(rx * .85f, -ry * .75f), Pt(rx, 0), Pt(rx * .75f, ry * .78f)
            };
            Fill(face, cSkin);

            // 눈 & 눈썹
            DrawEyesAndEyebrows(Color.FromArgb(30, 130, 230), true);

            // 금빛 턱수염 & 콧수염
            var beard = new[]
            {
                Pt(0, ry + 6),
                Pt(-rx * .6f, ry * .75f),
                Pt(-rx * .45f, ry * .3f),
                Pt(-10, ry * .35f),
                Pt(0, ry * .42f),
                Pt(10, ry * .35f),
                Pt(rx * .45f, ry * .3f),
                Pt(rx * .6f, ry * .75f)
            };
            Fill(beard, blonde);

            // 콧수염
            g.FillPolygon(B(blondeDark), new[] { Pt(0, 18), Pt(-22, 28), Pt(-6, 32), Pt(0, 24), Pt(6, 32), Pt(22, 28) });
            g.DrawPolygon(ink, new[] { Pt(0, 18), Pt(-22, 28), Pt(-6, 32), Pt(0, 24), Pt(6, 32), Pt(22, 28) });

            // 아스가르드 은색 투구 (Winged Helm)
            Color helmCol = Color.FromArgb(215, 222, 232);
            var helmCap = new[]
            {
                Pt(-rx * .95f, -12),
                Pt(-rx * .98f, -ry * .75f),
                Pt(0, -ry * 1.15f),
                Pt(rx * .98f, -ry * .75f),
                Pt(rx * .95f, -12),
                Pt(rx * .8f, -ry * .35f),
                Pt(0, -ry * .45f),
                Pt(-rx * .8f, -ry * .35f)
            };
            Fill(helmCap, helmCol);

            // 투구 양옆 날개 장식 (Wings)
            var wingL = new[]
            {
                Pt(-rx * .8f, -ry * .4f),
                Pt(-rx * 1.55f, -ry * .95f),
                Pt(-rx * 1.6f, -ry * 1.45f),
                Pt(-rx * 1.25f, -ry * 1.2f),
                Pt(-rx * .7f, -ry * .8f)
            };
            var wingR = new[]
            {
                Pt(rx * .8f, -ry * .4f),
                Pt(rx * 1.55f, -ry * .95f),
                Pt(rx * 1.6f, -ry * 1.45f),
                Pt(rx * 1.25f, -ry * 1.2f),
                Pt(rx * .7f, -ry * .8f)
            };
            Fill(wingL, Color.White);
            Fill(wingR, Color.White);
        }

        private void DrawEyesAndEyebrows(Color irisCol, bool blondeBrow)
        {
            float eyeY = -headRy * .05f;
            float eyeSep = 26f;

            // 히트 비전 발동 시: 눈이 붉게 타오름!
            if (p.HeatLaser > 0.05f)
            {
                irisCol = Color.Red;
                // 붉은 발광 효과
                g.FillEllipse(B(Color.FromArgb(160, 255, 60, 60)), -eyeSep - 18, eyeY - 14, 36, 28);
                g.FillEllipse(B(Color.FromArgb(160, 255, 60, 60)), eyeSep - 18, eyeY - 14, 36, 28);
            }

            // 흰자
            g.FillEllipse(B(Color.White), -eyeSep - 13, eyeY - 8, 26, 18);
            g.DrawEllipse(ink, -eyeSep - 13, eyeY - 8, 26, 18);
            g.FillEllipse(B(Color.White), eyeSep - 13, eyeY - 8, 26, 18);
            g.DrawEllipse(ink, eyeSep - 13, eyeY - 8, 26, 18);

            // 동공 & 홍채
            g.FillEllipse(B(irisCol), -eyeSep - 6, eyeY - 6, 14, 14);
            g.FillEllipse(B(irisCol), eyeSep - 8, eyeY - 6, 14, 14);
            g.FillEllipse(B(Color.White), -eyeSep - 3, eyeY - 4, 4, 4);
            g.FillEllipse(B(Color.White), eyeSep - 5, eyeY - 4, 4, 4);

            // 눈썹
            Color browCol = blondeBrow ? Color.FromArgb(190, 150, 30) : cInk;
            using (var pn = Pn(browCol, 4.2f))
            {
                float browY = eyeY - 12;
                g.DrawLine(pn, -eyeSep - 14, browY - 2, -eyeSep + 12, browY + 4);
                g.DrawLine(pn, eyeSep + 14, browY - 2, eyeSep - 12, browY + 4);
            }
        }

        private void DrawHeroMouth()
        {
            float mY = 22f;
            using (var pn = Pn(cInk, 3.4f))
            {
                if (p.Mouth == MouthStyle.Smile)
                {
                    g.DrawArc(pn, -16, mY - 4, 32, 16, 20, 140);
                }
                else if (p.Mouth == MouthStyle.Open)
                {
                    g.FillEllipse(B(Color.FromArgb(160, 40, 40)), -12, mY, 24, 16);
                    g.DrawEllipse(pn, -12, mY, 24, 16);
                }
                else
                {
                    g.DrawLine(pn, -14, mY + 2, 14, mY + 2);
                }
            }
        }
    }
}
