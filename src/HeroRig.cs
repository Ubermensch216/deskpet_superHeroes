using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace SuperHeroes
{
    /// <summary>
    /// 수퍼히어로 4인 (수퍼맨, 배트맨, 스파이더맨, 토르) 벡터 그래픽 리그.
    /// 기준 좌표계: 키 571, 발바닥 (200, 571).
    /// </summary>
    internal sealed class HeroRig : CharRig
    {
        private readonly Persona who;
        public HeroRig(Persona who) { this.who = who; }
        public Persona Who { get { return who; } }

        public override float RefH { get { return 571f; } }
        public override float FootX { get { return 200f; } }
        public override float FootY { get { return 571f; } }
        public override float HeadAxisDx { get { return 0; } }
        public override float GripY { get { return 70; } }
        public override float BodyRefW { get { return 200f; } }
        public override float NominalW { get { return 270f; } }
        public override float BellyY { get { return 430; } }
        public override float HeadCx { get { return 200; } }
        public override float HeadCy { get { return 160; } }
        public override float HeadSize { get { return 240; } }

        protected override void DrawBody(Graphics g, Pose p, float k, bool faceRight)
        {
            using (var painter = new HeroPainter(g, p, who, k, faceRight)) painter.Run();
        }
    }

    /// <summary>히어로의 몸체, 머리, 사지, 망토, 이펙트를 그리는 화가.</summary>
    internal sealed partial class HeroPainter : IDisposable
    {
        private readonly Graphics g;
        private readonly Pose p;
        private readonly Persona who;
        private readonly float k;
        private readonly bool faceRight;
        private readonly List<IDisposable> trash = new List<IDisposable>();

        private Pen ink;
        private static readonly Color cInk = Color.FromArgb(30, 30, 36);
        private static readonly Color cWhite = Color.FromArgb(250, 250, 252);

        // 체형 치수
        private float shY = 275, shDx = 76, topW = 152, botW = 120, waistY = 410, hipY = 430, hipDx = 38, footDx = 48, legW = 44, l1 = 66, l2 = 64, armW = 40;
        private float headCy = 160, headRx = 88, headRy = 94;

        // 히어로별 색상
        private Color cSkin, cSkinSh, cSuit, cSuitSh, cSuitAccent, cCape, cCapeInner, cBelt, cBoots, cGloves;
        private float turn;
        private PointF shL, shR;
        private readonly PointF[] wrist = new PointF[2], hdir = new PointF[2];
        private float torsoCx { get { return 200 - 32 * turn; } }

        internal HeroPainter(Graphics g, Pose p, Persona who, float k, bool faceRight)
        {
            this.g = g; this.p = p; this.who = who; this.k = k; this.faceRight = faceRight;
            Setup();
        }

        public void Dispose()
        {
            foreach (var d in trash) d.Dispose();
            trash.Clear();
        }

        private SolidBrush B(Color c) { var b = new SolidBrush(c); trash.Add(b); return b; }
        private Pen Pn(Color c, float w) { var pn = new Pen(c, w) { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round }; trash.Add(pn); return pn; }
        private static Color Mix(Color a, Color b, float t)
        {
            t = Math.Max(0, Math.Min(1, t));
            return Color.FromArgb((int)(a.A + (b.A - a.A) * t), (int)(a.R + (b.R - a.R) * t), (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t));
        }
        private static Color Al(Color c, float a) { return Color.FromArgb((int)Math.Max(0, Math.Min(255, a)), c.R, c.G, c.B); }
        private static PointF Pt(float x, float y) { return new PointF(x, y); }

        private void Fill(PointF[] pts, Color fill, bool edge = true)
        {
            g.FillPolygon(B(fill), pts);
            if (edge) g.DrawPolygon(ink, pts);
        }

        private void Setup()
        {
            turn = Math.Max(0, Math.Min(1, p.Turn));
            ink = Pn(cInk, 3.8f);

            // 공통 피부색
            cSkin = Color.FromArgb(255, 224, 196);
            cSkinSh = Color.FromArgb(226, 185, 154);

            switch (who)
            {
                case Persona.Superman:
                    // 수퍼맨: 클래식 로열 블루 수트, 진홍색 망토, 노란 벨트, 붉은 부츠/팬츠
                    cSuit = Color.FromArgb(16, 92, 184);
                    cSuitSh = Color.FromArgb(10, 62, 138);
                    cSuitAccent = Color.FromArgb(216, 32, 42); // 가슴 S 및 팬츠 레드
                    cCape = Color.FromArgb(200, 24, 34);
                    cCapeInner = Color.FromArgb(150, 16, 24);
                    cBelt = Color.FromArgb(246, 196, 24);
                    cBoots = Color.FromArgb(190, 20, 30);
                    cGloves = cSkin; // 장갑 없음(맨손)
                    shDx = 82; topW = 160; waistY = 412; hipY = 432;
                    reporter = p.Disguise > .5f;
                    if (reporter)
                    {
                        // 기자(클라크 켄트): 네이비 정장 + 검정 구두 (망토·수트는 숨긴다)
                        cSuit = cJacket; cSuitSh = cJacketSh; cBoots = cShoes;
                    }
                    break;

                case Persona.Batman:
                    // 배트맨: 짙은 차콜 수트, 검은 망토/카울, 황금빛 유틸리티 벨트, 검은 부츠/장갑
                    cSuit = Color.FromArgb(64, 68, 76);
                    cSuitSh = Color.FromArgb(42, 45, 52);
                    cSuitAccent = Color.FromArgb(24, 24, 28); // 가슴 박쥐 엠블럼 블랙
                    cCape = Color.FromArgb(28, 30, 36);
                    cCapeInner = Color.FromArgb(18, 19, 24);
                    cBelt = Color.FromArgb(228, 180, 42);
                    cBoots = Color.FromArgb(24, 25, 30);
                    cGloves = Color.FromArgb(32, 34, 40);
                    shDx = 80; topW = 156; waistY = 412; hipY = 432;
                    break;

                case Persona.Spiderman:
                    // 스파이더맨: 강렬한 레드 & 코발트 블루 투톤, 거미줄 문양, 가슴 거미 엠블럼
                    cSuit = Color.FromArgb(214, 38, 44);       // 가슴/마스크 레드
                    cSuitSh = Color.FromArgb(170, 24, 30);
                    cSuitAccent = Color.FromArgb(26, 78, 172);  // 옆구리/하의 블루
                    cCape = Color.Transparent;                 // 망토 없음
                    cCapeInner = Color.Transparent;
                    cBelt = Color.Transparent;
                    cBoots = Color.FromArgb(205, 34, 40);
                    cGloves = Color.FromArgb(214, 38, 44);
                    shDx = 74; topW = 144; botW = 114; waistY = 415; hipY = 435; l1 = 64; l2 = 64; // 날렵하고 유연한 체형
                    break;

                case Persona.Thor:
                    // 토르: 6원형 디스크가 박힌 은검정 갑옷, 진홍색 망토, 금색 벨트, 푸른 바지
                    cSuit = Color.FromArgb(44, 48, 56);
                    cSuitSh = Color.FromArgb(28, 32, 38);
                    cSuitAccent = Color.FromArgb(200, 210, 225); // 은색 디스크
                    cCape = Color.FromArgb(184, 28, 36);
                    cCapeInner = Color.FromArgb(136, 18, 24);
                    cBelt = Color.FromArgb(220, 175, 40);
                    cBoots = Color.FromArgb(110, 80, 50);
                    cGloves = Color.FromArgb(180, 185, 195);     // 은색 건틀릿
                    shDx = 86; topW = 168; waistY = 412; hipY = 432; legW = 48; // 건장한 신의 체격
                    break;
            }
        }

        public void Run()
        {
            var state = g.Save();
            g.ScaleTransform(faceRight ? -k : k, k);
            g.TranslateTransform(-200, -571);

            float ow = Math.Max(3.2f, .9f / k);
            ink = Pn(cInk, ow);

            // 어깨 위치 계산
            float bx = p.BodyX, by = p.BodyY;
            float sY = shY + by;
            shL = Pt(torsoCx - shDx * (1 - turn * .35f) + bx, sY);
            shR = Pt(torsoCx + shDx * (1 - turn * .15f) + bx, sY);

            // 손 위치 IK 계산
            SolveArms();

            // 1. 뒤쪽 망토
            DrawBackCape();

            // 2. 뒤쪽 다리 & 부츠
            DrawLeg(false);

            // 3. 앞쪽 다리 & 부츠
            DrawLeg(true);

            // 4. 뒤쪽 팔
            DrawArm(false);

            // 5. 몸통 및 수트/엠블럼/벨트
            DrawTorso();

            // 6. 앞쪽 팔 (또는 가슴 앞 손)
            DrawArm(true);

            // 7. 앞쪽 망토 깃
            DrawFrontCape();

            // 8. 머리 & 얼굴 & 투구/카울/마스크
            DrawHead();

            // 9. 슈퍼히어로 시그니처 FX 및 장비
            DrawHeroSignatureFx();

            g.Restore(state);
        }

        private void DrawBackCape()
        {
            if (cCape == Color.Transparent || reporter) return;

            float bx = p.BodyX, by = p.BodyY;
            float tilt = p.HeadTilt * .4f;
            float wind = p.CapeWind + (float)Math.Sin(p.SigPhase * 10) * 15f;

            if (p.CapeWing > 0.05f && who == Persona.Batman)
            {
                // 배트맨 가고일 모드: 박쥐 날개 모양으로 양옆에 크게 펼쳐진 망토!
                float wspan = 140f * p.CapeWing;
                var bpath = new GraphicsPath();
                bpath.AddPolygon(new[]
                {
                    Pt(shL.X - 10, shL.Y + 10),
                    Pt(torsoCx - shDx - wspan, shL.Y + 120),
                    Pt(torsoCx - shDx - wspan * .8f, waistY + 100),
                    Pt(torsoCx - 40, waistY + 140),
                    Pt(torsoCx, waistY + 120),
                    Pt(torsoCx + 40, waistY + 140),
                    Pt(torsoCx + shDx + wspan * .8f, waistY + 100),
                    Pt(torsoCx + shDx + wspan, shL.Y + 120),
                    Pt(shR.X + 10, shR.Y + 10)
                });
                g.FillPath(B(cCape), bpath);
                g.DrawPath(ink, bpath);
                return;
            }

            // 일반 상태 망토
            var pts = new[]
            {
                Pt(shL.X + 10, shL.Y + 15),
                Pt(torsoCx - topW * .6f - wind * .8f, waistY + 60),
                Pt(torsoCx - topW * .7f - wind * 1.4f, 540 + by),
                Pt(torsoCx + topW * .4f - wind * .4f, 545 + by),
                Pt(torsoCx + topW * .5f, waistY + 65),
                Pt(shR.X - 10, shR.Y + 15)
            };
            g.FillPolygon(B(cCapeInner), pts);
            g.DrawPolygon(ink, pts);
        }

        private void DrawFrontCape()
        {
            if (cCape == Color.Transparent || reporter) return;

            // 어깨 고정부 / 망토 깃
            if (who == Persona.Thor)
            {
                // 토르의 황금 원형 망토 버클
                g.FillEllipse(B(cBelt), shL.X - 6, shL.Y - 2, 22, 22);
                g.DrawEllipse(ink, shL.X - 6, shL.Y - 2, 22, 22);
                g.FillEllipse(B(cBelt), shR.X - 16, shR.Y - 2, 22, 22);
                g.DrawEllipse(ink, shR.X - 16, shR.Y - 2, 22, 22);
            }
            else if (who == Persona.Superman || who == Persona.Batman)
            {
                // 어깨를 감싸는 망토 깃
                g.FillPolygon(B(cCape), new[] { Pt(shL.X - 6, shL.Y + 2), Pt(shL.X + 22, shL.Y - 6), Pt(torsoCx - 20, shL.Y + 16) });
                g.DrawPolygon(ink, new[] { Pt(shL.X - 6, shL.Y + 2), Pt(shL.X + 22, shL.Y - 6), Pt(torsoCx - 20, shL.Y + 16) });
                g.FillPolygon(B(cCape), new[] { Pt(shR.X + 6, shR.Y + 2), Pt(shR.X - 22, shR.Y - 6), Pt(torsoCx + 20, shL.Y + 16) });
                g.DrawPolygon(ink, new[] { Pt(shR.X + 6, shR.Y + 2), Pt(shR.X - 22, shR.Y - 6), Pt(torsoCx + 20, shL.Y + 16) });
            }
        }

        private void DrawLeg(bool front)
        {
            float side = front ? 1 : -1;
            float fx = front ? p.FootRX : p.FootLX;
            float fy = front ? p.FootRY : p.FootLY;
            float rot = front ? p.FootRRot : p.FootLRot;

            float hipX = torsoCx + hipDx * side * (1 - turn * .3f) + p.BodyX;
            float hipYPos = hipY + p.BodyY;

            float footBaseX = 200 + footDx * side + fx;
            float footBaseY = 571 + fy;

            // 허벅지 및 종아리 연결
            PointF h = Pt(hipX, hipYPos);
            PointF knee = Pt((hipX + footBaseX) * .5f + side * p.Knee * 12, (hipYPos + footBaseY) * .5f - 10);
            PointF foot = Pt(footBaseX, footBaseY);

            // 다리 윤곽
            var pts = new[]
            {
                Pt(h.X - legW * .4f, h.Y),
                Pt(knee.X - legW * .36f, knee.Y),
                Pt(foot.X - legW * .32f, foot.Y - 14),
                Pt(foot.X + legW * .32f, foot.Y - 14),
                Pt(knee.X + legW * .36f, knee.Y),
                Pt(h.X + legW * .4f, h.Y)
            };

            Color pantsCol = (who == Persona.Spiderman) ? cSuitAccent : (who == Persona.Thor) ? Color.FromArgb(36, 42, 54) : cSuit;
            Fill(pts, pantsCol);

            // 부츠 그리기
            // 기자 차림이면 롱부츠 대신 발목까지만 오는 검정 구두
            float bootTopY = reporter ? foot.Y - 26 : knee.Y + 20;
            float bootTopX = reporter ? foot.X : knee.X;
            var bootPts = new[]
            {
                Pt(bootTopX - legW * .35f, bootTopY),
                Pt(foot.X - legW * .35f, foot.Y - 12),
                Pt(foot.X - legW * .45f, foot.Y),
                Pt(foot.X + legW * .42f, foot.Y),
                Pt(foot.X + legW * .35f, foot.Y - 12),
                Pt(bootTopX + legW * .35f, bootTopY)
            };
            Fill(bootPts, cBoots);

            if (who == Persona.Thor)
            {
                // 토르의 부츠 십자 끈 디테일
                using (var pn = Pn(Color.FromArgb(210, 180, 70), 2.4f))
                {
                    g.DrawLine(pn, knee.X - 10, knee.Y + 25, foot.X + 8, foot.Y - 4);
                    g.DrawLine(pn, knee.X + 10, knee.Y + 25, foot.X - 8, foot.Y - 4);
                }
            }
            else if (who == Persona.Spiderman)
            {
                // 스파이더맨 부츠 거미줄 격자
                DrawWebGrid(foot.X - 15, knee.Y + 20, 30, foot.Y - knee.Y - 20);
            }
        }

        private void DrawTorso()
        {
            float bx = p.BodyX, by = p.BodyY;
            float tcX = torsoCx + bx;

            // 상체 외곽선 (역삼각형 히어로 체형)
            var chestPts = new[]
            {
                Pt(tcX - topW * .52f, shY + by),
                Pt(tcX - topW * .55f, shY + by + 35),
                Pt(tcX - botW * .5f, waistY + by),
                Pt(tcX + botW * .5f, waistY + by),
                Pt(tcX + topW * .55f, shY + by + 35),
                Pt(tcX + topW * .52f, shY + by)
            };

            Color torsoCol = cSuit;
            Fill(chestPts, torsoCol);

            // 캐릭터별 상체 특징
            switch (who)
            {
                case Persona.Superman:
                    if (reporter) DrawReporterChest(tcX, shY + by); else DrawSupermanChest(tcX, shY + by);
                    break;
                case Persona.Batman:
                    DrawBatmanChest(tcX, shY + by);
                    break;
                case Persona.Spiderman:
                    DrawSpidermanChest(tcX, shY + by);
                    break;
                case Persona.Thor:
                    DrawThorChest(tcX, shY + by);
                    break;
            }

            // 벨트 및 하의 삼각팬츠
            DrawBeltAndHips(tcX, waistY + by, hipY + by);
        }

        private void DrawSupermanChest(float cx, float cy)
        {
            DrawSupermanEmblem(cx, cy);
            // 셔츠 찢기는 기자 차림(DrawReporterChest)에서 그린다 — 수트 차림에는 찢을 셔츠가 없다.
        }

        private void DrawSupermanEmblem(float cx, float cy)
        {
            // 수퍼맨 S 다이아몬드 방패 엠블럼
            float embY = cy + 45;
            var shield = new[]
            {
                Pt(cx, embY + 54),
                Pt(cx - 36, embY + 22),
                Pt(cx - 28, embY),
                Pt(cx + 28, embY),
                Pt(cx + 36, embY + 22)
            };
            g.FillPolygon(B(cBelt), shield); // 황금색 바탕
            g.DrawPolygon(ink, shield);

            // 붉은색 테두리 및 'S' 심볼
            using (var pn = Pn(cSuitAccent, 4f))
            {
                g.DrawPolygon(pn, shield);
                // S자 곡선
                var spath = new GraphicsPath();
                spath.AddBezier(Pt(cx + 18, embY + 12), Pt(cx - 16, embY + 14), Pt(cx - 16, embY + 28), Pt(cx, embY + 30));
                spath.AddBezier(Pt(cx, embY + 30), Pt(cx + 18, embY + 32), Pt(cx + 16, embY + 46), Pt(cx - 18, embY + 46));
                g.DrawPath(pn, spath);
            }
        }

        private void DrawBatmanChest(float cx, float cy)
        {
            // 배트맨 박쥐 엠블럼
            float embY = cy + 48;
            var bat = new GraphicsPath();
            bat.AddPolygon(new[]
            {
                Pt(cx, embY + 20),
                Pt(cx - 14, embY + 32),
                Pt(cx - 38, embY + 16),
                Pt(cx - 28, embY + 8),
                Pt(cx - 12, embY + 14),
                Pt(cx - 6, embY + 4),  // 왼쪽 귀
                Pt(cx - 2, embY + 10),
                Pt(cx + 2, embY + 10),
                Pt(cx + 6, embY + 4),  // 오른쪽 귀
                Pt(cx + 12, embY + 14),
                Pt(cx + 28, embY + 8),
                Pt(cx + 38, embY + 16),
                Pt(cx + 14, embY + 32)
            });
            g.FillPath(B(cSuitAccent), bat);
            g.DrawPath(ink, bat);

            // 복근 및 가슴 음영 라인
            using (var pn = Pn(Color.FromArgb(32, 34, 40), 2.2f))
            {
                g.DrawLine(pn, cx, cy + 70, cx, cy + 115);
                g.DrawLine(pn, cx - 22, cy + 85, cx + 22, cy + 85);
                g.DrawLine(pn, cx - 20, cy + 102, cx + 20, cy + 102);
            }
        }

        private void DrawSpidermanChest(float cx, float cy)
        {
            // 옆구리 블루 영역
            var blueL = new[] { Pt(cx - topW * .52f, cy), Pt(cx - 32, cy + 20), Pt(cx - 26, cy + 120), Pt(cx - botW * .5f, cy + 120) };
            var blueR = new[] { Pt(cx + topW * .52f, cy), Pt(cx + 32, cy + 20), Pt(cx + 26, cy + 120), Pt(cx + botW * .5f, cy + 120) };
            g.FillPolygon(B(cSuitAccent), blueL); g.DrawPolygon(ink, blueL);
            g.FillPolygon(B(cSuitAccent), blueR); g.DrawPolygon(ink, blueR);

            // 거미줄 격자 패턴
            DrawWebGrid(cx - 30, cy + 10, 60, 110);

            // 중앙 검은 거미 엠블럼
            float embY = cy + 46;
            g.FillEllipse(B(cInk), cx - 4, embY + 4, 8, 14); // 몸통
            using (var pn = Pn(cInk, 2f))
            {
                // 거미 다리 8개
                g.DrawLine(pn, cx - 3, embY + 6, cx - 18, embY - 4);
                g.DrawLine(pn, cx - 3, embY + 10, cx - 22, embY + 8);
                g.DrawLine(pn, cx - 3, embY + 13, cx - 20, embY + 22);
                g.DrawLine(pn, cx - 3, embY + 16, cx - 14, embY + 30);

                g.DrawLine(pn, cx + 3, embY + 6, cx + 18, embY - 4);
                g.DrawLine(pn, cx + 3, embY + 10, cx + 22, embY + 8);
                g.DrawLine(pn, cx + 3, embY + 13, cx + 20, embY + 22);
                g.DrawLine(pn, cx + 3, embY + 16, cx + 14, embY + 30);
            }
        }

        private void DrawThorChest(float cx, float cy)
        {
            // 토르 흉갑: 6개의 원형 은색 갑옷 디스크
            float dY = cy + 30;
            float r = 14f;
            PointF[] discs = new[]
            {
                Pt(cx - 28, dY),      Pt(cx + 28, dY),
                Pt(cx - 30, dY + 36), Pt(cx + 30, dY + 36),
                Pt(cx - 24, dY + 72), Pt(cx + 24, dY + 72)
            };
            foreach (var pt in discs)
            {
                g.FillEllipse(B(cSuitAccent), pt.X - r, pt.Y - r, r * 2, r * 2);
                g.DrawEllipse(ink, pt.X - r, pt.Y - r, r * 2, r * 2);
                g.FillEllipse(B(Color.White), pt.X - r * .4f, pt.Y - r * .4f, r * .6f, r * .6f);
            }
        }

        private void DrawBeltAndHips(float cx, float wy, float hy)
        {
            if (who == Persona.Superman && reporter)
            {
                DrawReporterBelt(cx, wy, hy);
            }
            else if (who == Persona.Superman)
            {
                // 수퍼맨 붉은색 트렁크(하의)
                var trunks = new[]
                {
                    Pt(cx - botW * .5f, wy),
                    Pt(cx - botW * .52f, hy + 18),
                    Pt(cx, hy + 28),
                    Pt(cx + botW * .52f, hy + 18),
                    Pt(cx + botW * .5f, wy)
                };
                Fill(trunks, cSuitAccent);
                // 노란 벨트
                var beltRect = new RectangleF(cx - botW * .5f, wy - 4, botW, 16);
                g.FillRectangle(B(cBelt), beltRect);
                g.DrawRectangle(ink, beltRect.X, beltRect.Y, beltRect.Width, beltRect.Height);
                // 타원 버클
                g.FillEllipse(B(Color.FromArgb(255, 230, 80)), cx - 12, wy - 5, 24, 18);
                g.DrawEllipse(ink, cx - 12, wy - 5, 24, 18);
            }
            else if (who == Persona.Batman)
            {
                // 배트맨 유틸리티 벨트 (파우치 디테일)
                var beltRect = new RectangleF(cx - botW * .5f, wy - 2, botW, 18);
                g.FillRectangle(B(cBelt), beltRect);
                g.DrawRectangle(ink, beltRect.X, beltRect.Y, beltRect.Width, beltRect.Height);
                // 벨트 파우치들
                for (int i = -3; i <= 3; i++)
                {
                    if (i == 0) continue;
                    g.FillRectangle(B(Color.FromArgb(190, 150, 30)), cx + i * 16 - 6, wy, 12, 14);
                    g.DrawRectangle(ink, cx + i * 16 - 6, wy, 12, 14);
                }
                // 중앙 사각 버클
                g.FillRectangle(B(Color.FromArgb(240, 205, 60)), cx - 10, wy - 3, 20, 20);
                g.DrawRectangle(ink, cx - 10, wy - 3, 20, 20);
            }
            else if (who == Persona.Thor)
            {
                // 토르 아스가르드 벨트
                var beltRect = new RectangleF(cx - botW * .5f, wy, botW, 16);
                g.FillRectangle(B(cBelt), beltRect);
                g.DrawRectangle(ink, beltRect.X, beltRect.Y, beltRect.Width, beltRect.Height);
                g.FillEllipse(B(cSuitAccent), cx - 14, wy - 3, 28, 22);
                g.DrawEllipse(ink, cx - 14, wy - 3, 28, 22);
            }
        }

        private void DrawWebGrid(float x, float y, float w, float h)
        {
            using (var pn = Pn(Color.FromArgb(30, 30, 35), 1.6f))
            {
                // 세로선
                int cols = 4;
                for (int i = 0; i <= cols; i++)
                {
                    float gx = x + w * i / cols;
                    g.DrawLine(pn, gx, y, gx, y + h);
                }
                // 가로 호선
                int rows = 5;
                for (int i = 0; i <= rows; i++)
                {
                    float gy = y + h * i / rows;
                    g.DrawArc(pn, x, gy - 6, w, 12, 0, 180);
                }
            }
        }

        private void SolveArms()
        {
            // 기본 팔 끝 위치 (내려뜨림)
            wrist[0] = Pt(shL.X - 16, shL.Y + l1 + l2 - 10);
            wrist[1] = Pt(shR.X + 16, shR.Y + l1 + l2 - 10);

            // Reach 또는 프리셋 처리
            if (p.Reach == Reach.Hips)
            {
                // 양손 허리춤 척 당당한 슈퍼히어로 포즈
                wrist[0] = Pt(torsoCx - botW * .48f, waistY + 6);
                wrist[1] = Pt(torsoCx + botW * .48f, waistY + 6);
            }
            else if (p.Reach == Reach.Cross)
            {
                // 팔짱 끼기
                wrist[0] = Pt(torsoCx + 24, waistY - 14);
                wrist[1] = Pt(torsoCx - 24, waistY - 14);
            }
            else if (p.Reach == Reach.Hammer && who == Persona.Thor)
            {
                // 토르 묠니르 치켜들기
                wrist[1] = Pt(shR.X + 20, shR.Y - 80);
            }
            else if (p.Reach == Reach.WebThrow && who == Persona.Spiderman)
            {
                // 스파이더맨 웹슈터 정면 발사
                wrist[1] = Pt(torsoCx - 100, shR.Y + 20);
            }
            else if (p.HandLTo.HasValue || p.HandRTo.HasValue)
            {
                if (p.HandLTo.HasValue) wrist[0] = p.HandLTo.Value;
                if (p.HandRTo.HasValue) wrist[1] = p.HandRTo.Value;
            }
        }

        private void DrawArm(bool front)
        {
            int idx = front ? 1 : 0;
            PointF sh = front ? shR : shL;
            PointF wr = wrist[idx];
            float side = front ? 1 : -1;

            // 팔꿈치 위치
            PointF elbow = Pt((sh.X + wr.X) * .5f + side * 22, (sh.Y + wr.Y) * .5f);

            // 어깨~팔꿈치~손목 다각형
            var armPts = new[]
            {
                Pt(sh.X - armW * .45f, sh.Y),
                Pt(elbow.X - armW * .4f, elbow.Y),
                Pt(wr.X - armW * .32f, wr.Y),
                Pt(wr.X + armW * .32f, wr.Y),
                Pt(elbow.X + armW * .4f, elbow.Y),
                Pt(sh.X + armW * .45f, sh.Y)
            };

            Color armCol = (who == Persona.Spiderman) ? cSuit : cSuit;
            Fill(armPts, armCol);

            // 장갑/건틀릿
            DrawGlove(wr, elbow, side, front);
        }

        private void DrawGlove(PointF wr, PointF elbow, float side, bool front)
        {
            var glPts = new[]
            {
                Pt(elbow.X - armW * .38f, elbow.Y + 14),
                Pt(wr.X - armW * .34f, wr.Y),
                Pt(wr.X + armW * .34f, wr.Y),
                Pt(elbow.X + armW * .38f, elbow.Y + 14)
            };

            if (who != Persona.Superman)
            {
                Fill(glPts, cGloves);
            }

            if (reporter) DrawReporterCuff(wr);

            // 주먹/손바닥
            float hx = wr.X, hy = wr.Y + 10;
            if (who == Persona.Batman)
            {
                // 배트맨 건틀릿 핀 (블레이드 3개)
                using (var pn = Pn(cInk, 2.5f))
                {
                    g.DrawLine(pn, elbow.X + side * 14, elbow.Y + 4, elbow.X + side * 28, elbow.Y - 2);
                    g.DrawLine(pn, elbow.X + side * 13, elbow.Y + 16, elbow.X + side * 27, elbow.Y + 10);
                    g.DrawLine(pn, elbow.X + side * 12, elbow.Y + 28, elbow.X + side * 26, elbow.Y + 22);
                }
            }

            // 손 그리기
            g.FillEllipse(B(cGloves), hx - 14, hy - 12, 28, 24);
            g.DrawEllipse(ink, hx - 14, hy - 12, 28, 24);

            if (who == Persona.Spiderman && (p.HandR == HandStyle.Web || p.Reach == Reach.WebThrow))
            {
                // 스파이더맨 웹슈터 발사 손가락 (검지/소지 펴기)
                using (var pn = Pn(cSuit, 4f))
                {
                    g.DrawLine(pn, hx - 8, hy + 4, hx - 14, hy + 18);
                    g.DrawLine(pn, hx + 8, hy + 4, hx + 14, hy + 18);
                }
            }
        }
    }
}
