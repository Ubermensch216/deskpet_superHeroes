using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace SuperHeroes
{
    // 토르 '묠니르 풍차 회전'과 '지면 강타 충격파' 연출.
    // 천둥 벼락 소환(HeroRigFx.cs)과 같은 도구(Glow / BoltPath / StrokeBolt / StrokeThin)로 그린다.
    internal sealed partial class HeroPainter
    {
        private static float Ease01(float x) { x = Math.Max(0f, Math.Min(1f, x)); return x * x * (3 - 2 * x); }
        private static float CxClamp(float x) { return Math.Max(-40f, Math.Min(440f, x)); }
        private static PointF PtC(float x, float y) { return new PointF(CxClamp(x), y); }

        /// <summary>묠니르 머리 한가운데(손잡이 쥔 곳에서 머리 쪽으로 29*sc). angDeg 는 시계 방향 회전각(0 = 머리가 위).</summary>
        private static PointF HeadAt(PointF o, float angDeg, float sc)
        {
            double r = angDeg * Math.PI / 180.0;
            return new PointF(o.X + (float)Math.Sin(r) * 29f * sc, o.Y - (float)Math.Cos(r) * 29f * sc);
        }

        /// <summary>손잡이 쥔 곳(x,y)을 기준으로 회전·확대해 묠니르를 그린다. glow 가 크면 머리가 푸르게 달아오른다.</summary>
        private void DrawHammerAt(float x, float y, float angDeg, float sc, float alpha, float glow)
        {
            alpha = Math.Max(0f, Math.Min(255f, alpha));
            var st = g.Save();
            g.TranslateTransform(x, y);
            g.RotateTransform(angDeg);
            g.ScaleTransform(sc, sc);
            Pen line = Pn(Al(cInk, alpha), 3f);
            g.FillRectangle(B(Al(Color.FromArgb(120, 80, 50), alpha)), -4, -14, 8, 36);       // 자루
            g.DrawRectangle(line, -4, -14, 8, 36);
            g.FillRectangle(B(Al(Color.FromArgb(70, 45, 30), alpha)), -5, 12, 10, 5);          // 자루 끝 가죽 고리
            g.FillRectangle(B(Al(Color.FromArgb(195, 205, 215), alpha)), -18, -44, 36, 30);    // 머리
            g.DrawRectangle(line, -18, -44, 36, 30);
            g.FillRectangle(B(Al(Color.FromArgb(150, 160, 178), alpha)), -18, -22, 36, 8);     // 아래쪽 음영
            g.FillRectangle(B(Al(Color.White, alpha)), -12, -40, 24, 8);                       // 금속 광택
            g.Restore(st);
            if (glow > .02f)
            {
                PointF h = HeadAt(Pt(x, y), angDeg, sc);
                Glow(h.X, h.Y, 46 * sc, 40 * sc, Al(Color.FromArgb(170, 225, 255), 190 * glow * alpha / 255f));
            }
        }

        // ───────── 묠니르 풍차 회전 ─────────
        // 손목 위에서 묠니르가 점점 빨라지며 원을 그리고(잔상·꼬리·번개) → 몸 둘레에 회오리 바람과 에너지 방어막이 펼쳐지고
        // → 지면에 먼지 고리가 번지다가 → 속도가 줄며 묠니르가 다시 손으로 돌아온다.

        private void DrawMjolnirWhirl()
        {
            float k = Math.Min(1f, p.MjolnirSpin), T = p.SpinT, bx = p.BodyX, by = p.BodyY;
            int frame = (int)(T * 30);
            float ease = Ease01(k * 1.8f);                       // 반경·방향이 평소 자세에서 부드럽게 넘어오게
            PointF hand = wrist[1];
            PointF c = Pt(hand.X, hand.Y - 34f * ease);          // 회전 중심(손 바로 위)
            float rx = 100f * ease, ry = 38f * ease;
            float th = ThorWhirl.Rev(T) * 6.2831853f;
            float speed = ThorWhirl.Speed(T) / 12f;
            PointF cen = Pt(200 + bx, 345 + by);
            Color cy = Color.FromArgb(120, 205, 255), ice = Color.FromArgb(190, 235, 255);
            Func<float, PointF> orbit = a => Pt(c.X + (float)Math.Cos(a) * rx, c.Y + (float)Math.Sin(a) * ry);

            // 1. 에너지 방어막: 몸 전체를 감싸는 푸른 빛과 바깥으로 퍼지는 펄스
            Glow(cen.X, cen.Y, 150, 240, Al(cy, 55 * k));
            Glow(200 + bx, 566, 150 * k + 20, 24, Al(ice, 90 * k));
            for (int pi = 0; pi < 2; pi++)
            {
                float u = (T * 1.5f + pi * .5f) % 1f, prx = 50 + 160 * u, pry = prx * 1.5f, pa = (1 - u) * k;
                g.DrawEllipse(Pn(Al(cy, 150 * pa), 6f), cen.X - prx, cen.Y - pry, prx * 2, pry * 2);
                g.DrawEllipse(Pn(Al(Color.White, 220 * pa), 2f), cen.X - prx, cen.Y - pry, prx * 2, pry * 2);
            }

            // 2. 회오리 바람: 위로 갈수록 넓어지는 여덟 겹의 소용돌이 호
            for (int j = 0; j < 8; j++)
            {
                float tj = j / 7f, yy = 560 - 390 * tj, r = 108 + 62 * tj, ryy = r * .2f;
                float a0 = th * 57.2958f * (.2f + .03f * j) + j * 47f, x0 = 200 + bx - r;
                Pen wide = Pn(Al(cy, 110 * k), 11f), thin = Pn(Al(Color.White, 240 * k), 3f);
                g.DrawArc(wide, x0, yy - ryy, r * 2, ryy * 2, a0, 85);
                g.DrawArc(thin, x0, yy - ryy, r * 2, ryy * 2, a0, 85);
                g.DrawArc(wide, x0, yy - ryy, r * 2, ryy * 2, a0 + 190, 55);
                g.DrawArc(thin, x0, yy - ryy, r * 2, ryy * 2, a0 + 190, 55);
            }
            for (int i = 0; i < 18; i++)   // 바람에 휘말려 올라가는 나뭇잎·먼지
            {
                float h = (i * .173f + T * (.35f + .1f * (i % 3))) % 1f;
                float r = (108 + 62 * h) * (.9f + .1f * (float)Math.Sin(i * 3.1f));
                float a = i * 2.1f + T * (4f + i % 4) * (.5f + speed);
                float px = 200 + bx + (float)Math.Cos(a) * r, py = 560 - 390 * h + (float)Math.Sin(a) * r * .2f, s = 3f + (i % 3) * 1.6f;
                Color lc = i % 3 == 0 ? Color.FromArgb(200, 235, 170) : i % 3 == 1 ? Color.White : Color.FromArgb(230, 215, 170);
                g.FillEllipse(B(Al(lc, 210 * k * (float)Math.Sin(h * Math.PI))), px - s * 1.6f, py - s * .6f, s * 3.2f, s * 1.2f);
            }

            // 3. 묠니르가 그리는 궤도: 빛무리 + 길게 끌리는 꼬리
            Glow(c.X, c.Y, rx + 36, ry + 28, Al(Color.FromArgb(140, 215, 255), 95 * k));
            g.DrawEllipse(Pn(Al(cy, 85 * k), 4f), c.X - rx, c.Y - ry, rx * 2, ry * 2);
            float tail = 2.0f * (.4f + .6f * speed);
            const int segs = 20;
            for (int s2 = 0; s2 < segs; s2++)
            {
                float f = 1f - (float)s2 / segs;
                var seg = new[] { orbit(th - tail * s2 / segs), orbit(th - tail * (s2 + 1) / segs) };
                Stroke(seg, Al(cy, 190 * f * k), 30 * f + 6);
                Stroke(seg, Al(ice, 230 * f * k), 15 * f + 3);
                Stroke(seg, Al(Color.White, 255 * f * k), 8 * f + 2f);
            }

            // 4. 묠니르 본체(잔상 3개 + 본체). 가죽 끈이 손에서 묠니르까지 팽팽하게 당겨진다.
            PointF mainQ = c; float mainAng = 0, mainSc = 1;
            for (int gi = 3; gi >= 0; gi--)
            {
                float a = th - gi * .15f * (.4f + speed);
                float ox = (float)Math.Cos(a) * rx, oy = (float)Math.Sin(a) * ry;
                float len = (float)Math.Sqrt(ox * ox + oy * oy);
                if (len < 1f) { ox = 0; oy = -1; } else { ox /= len; oy /= len; }
                float bl = Math.Min(1f, ease * 1.4f);
                ox *= bl; oy = -1 + (oy + 1) * bl;               // (0,-1) → 궤도 바깥 방향으로 서서히 넘어온다
                float l2 = Math.Max(.001f, (float)Math.Sqrt(ox * ox + oy * oy)); ox /= l2; oy /= l2;
                float ang = (float)(Math.Atan2(ox, -oy) * 180.0 / Math.PI);
                PointF q = orbit(a);
                float sc = 1f + .12f * ease + .28f * (float)Math.Sin(a) * ease;   // 앞쪽(아래)일수록 크게
                if (gi == 0)
                {
                    if (ease > .08f)
                    {
                        Stroke(new[] { hand, q }, Al(cInk, 255), 6.5f);
                        Stroke(new[] { hand, q }, Color.FromArgb(150, 105, 70), 3.6f);
                    }
                    DrawHammerAt(q.X, q.Y, ang, sc, 255, .35f + .65f * k);
                    mainQ = q; mainAng = ang; mainSc = sc;
                }
                else DrawHammerAt(q.X, q.Y, ang, sc, 95f / gi * k, 0f);
            }

            // 5. 묠니르 머리에서 사방으로 튀는 번개
            PointF hp = HeadAt(mainQ, mainAng, mainSc);
            Glow(hp.X, hp.Y, 30 + 18 * k, 30 + 18 * k, Al(Color.FromArgb(190, 235, 255), 150 * k * ease));
            var rs = new Random(frame * 37 + 11);
            int nb = 2 + (int)(4 * k);
            for (int i = 0; i < nb; i++)
            {
                double a = rs.NextDouble() * Math.PI * 2; float rr = 80 + rs.Next(90);
                PointF e = PtC(cen.X + (float)Math.Cos(a) * rr * 1.1f, cen.Y + (float)Math.Sin(a) * rr * 1.7f);
                StrokeThin(BoltPath(hp, e, frame * 9 + i, 16, 6), (.55f + .45f * (float)rs.NextDouble()) * k * ease, 2.4f);
            }

            // 6. 바깥으로 쭉쭉 뻗는 바람 줄기
            int nl = (int)(4 + 9 * k);
            for (int i = 0; i < nl; i++)
            {
                double a = rs.NextDouble() * Math.PI * 2; float r0 = 125 + rs.Next(50), r1 = r0 + 35 + rs.Next(70);
                PointF p0 = PtC(cen.X + (float)Math.Cos(a) * r0, cen.Y + (float)Math.Sin(a) * r0 * 1.15f);
                PointF p1 = PtC(cen.X + (float)Math.Cos(a) * r1, cen.Y + (float)Math.Sin(a) * r1 * 1.15f);
                Stroke(new[] { p0, p1 }, Al(Color.White, (90 + rs.Next(120)) * k), 2.2f);
            }

            // 7. 지면: 빙글빙글 도는 먼지 고리
            for (int i = 0; i < 14; i++)
            {
                float a = th * .12f + i * 6.2832f / 14f, rr = 150 + (i % 3) * 14;
                float px = 200 + bx + (float)Math.Cos(a) * rr, py = 566 + (float)Math.Sin(a) * 26, s = 5 + (i % 4) * 2.5f;
                g.FillEllipse(B(Al(Color.FromArgb(220, 228, 238), 130 * k)), px - s * 1.8f, py - s * .5f, s * 3.6f, s);
            }
            g.DrawEllipse(Pn(Al(ice, 110 * k), 3f), 200 + bx - 150, 566 - 24, 300, 48);
        }

        // ───────── 지면 강타 충격파 ─────────
        // 웅크려 기를 모으고 → 번개를 두른 채 높이 도약 → 묠니르를 내리꽂아 "쾅!" → 눈부신 섬광·번개 기둥·충격파 고리·갈라지는 대지·튀는 돌덩이 → 여진.

        private void DrawGroundSlam()
        {
            float T = p.CrushT, bx = p.BodyX, by = p.BodyY;
            int frame = (int)(T * 30);
            const float gy = 568f;
            float ix = ThorSlam.ImpactX;
            PointF hand = wrist[1];
            float ang = ThorSlam.Angle(T), sc = ThorSlam.Scale(T);
            float charge = Math.Min(1f, T / 1.5f);
            float u1 = T - ThorSlam.Hit1, u2 = T - ThorSlam.Hit2;
            float outFade = Ease01((ThorSlam.End - .05f - T) / .6f);
            Color ice = Color.FromArgb(100, 190, 255), cyan = Color.FromArgb(160, 225, 255);
            PointF head = HeadAt(hand, ang, sc);

            // 1. 땅에 깔리는 층: 갈라진 대지와 퍼져 나가는 충격파 고리
            DrawSlamCracks(T, ix, gy, u1, u2, frame, outFade);
            if (u1 > 0) SlamRings(u1, 1f, ix, gy, frame, 11);
            if (u2 > 0) SlamRings(u2, .55f, ix, gy, frame, 29);

            // 2. 묠니르
            float heat = T < ThorSlam.Hit1 ? .3f + .7f * charge
                : .3f + .7f * Math.Max((float)Math.Exp(-u1 * 2.2f), u2 > 0 ? (float)Math.Exp(-u2 * 3f) : 0f);
            DrawHammerAt(hand.X, hand.Y, ang, sc, 255, heat * outFade);

            // 3. 내리꽂기 전: 발밑 방전 + 묠니르로 빨려드는 전기 + 커지는 번개 구슬
            if (T < ThorSlam.Hit1)
            {
                float ground = Math.Min(1f, T / .7f) * (T < 1.5f ? 1f : Math.Max(0f, 1f - (T - 1.5f) / .3f));
                Glow(200 + bx, gy, 90 + 70 * charge, 18 + 10 * charge, Al(cyan, 150 * ground));
                var rg = new Random(frame * 5 + 3);
                int ns = 3 + (int)(5 * ground);
                for (int i = 0; i < ns; i++)
                {
                    float x0 = 200 + bx + rg.Next(-70, 71), len = 20 + rg.Next(40);
                    StrokeThin(BoltPath(Pt(x0, gy), Pt(x0 + rg.Next(-14, 15), gy - len), frame * 3 + i, 6, 4), .8f * ground, 2.2f);
                }

                int n = 5 + (int)(11 * charge);
                for (int i = 0; i < n; i++)
                {
                    float ph = (T * 1.4f + i / (float)n) % 1f;
                    float a = i * 2.39996f + T * 2.2f, r = 12 + 150 * (1 - ph) * (1 - ph);
                    var mote = new[]
                    {
                        PtC(head.X + (float)Math.Cos(a) * r, head.Y + (float)Math.Sin(a) * r * .8f),
                        PtC(head.X + (float)Math.Cos(a) * r * .78f, head.Y + (float)Math.Sin(a) * r * .78f * .8f)
                    };
                    Stroke(mote, Al(ice, 150 * ph * charge), 5f);
                    Stroke(mote, Al(Color.White, 255 * ph * charge), 2f);
                }
                float ball = (16 + 50 * charge) * (1f + .2f * (float)Math.Sin(T * 40));
                Glow(head.X, head.Y, ball * 1.7f, ball * 1.7f, Al(cyan, 70 + 110 * charge));
                Glow(head.X, head.Y, ball * .75f, ball * .75f, Al(Color.White, 210 * charge));
                var ra = new Random(frame * 13 + 1);
                int na = 2 + (int)(6 * charge);
                for (int i = 0; i < na; i++)
                {
                    double a = ra.NextDouble() * Math.PI * 2; float r0 = 10 + ra.Next(10), r1 = 35 + 40 * charge + ra.Next(30);
                    PointF s0 = Pt(head.X + (float)Math.Cos(a) * r0, head.Y + (float)Math.Sin(a) * r0);
                    PointF e0 = PtC(head.X + (float)Math.Cos(a) * r1, head.Y + (float)Math.Sin(a) * r1);
                    StrokeThin(BoltPath(s0, e0, frame * 31 + i, 10, 3), .5f + .5f * charge, 2.2f);
                }
            }

            // 4. 내리꽂는 순간: 호를 그리는 번개 궤적 + 위에서 떨어지는 속도선
            float sw = ThorSlam.Swing(T);
            if (T >= 1.5f && T < ThorSlam.Hit1 + .12f && sw > .02f)
            {
                var trail = new List<PointF>();
                for (int j = 0; j <= 10; j++)
                    trail.Add(PtC(HeadAt(Pt(hand.X, hand.Y - j * 9f * sw), ThorSlam.Angle(T - j * .014f), sc).X,
                                  HeadAt(Pt(hand.X, hand.Y - j * 9f * sw), ThorSlam.Angle(T - j * .014f), sc).Y));
                var tr = trail.ToArray();
                Stroke(tr, Al(ice, 120 * sw), 32f);
                Stroke(tr, Al(cyan, 210 * sw), 15f);
                Stroke(tr, Al(Color.White, 255 * sw), 5f);
                var rv = new Random(frame * 7 + 5);
                for (int i = 0; i < 8; i++)
                {
                    float x = head.X + rv.Next(-46, 47), y0 = head.Y - 60 - rv.Next(160);
                    Stroke(new[] { PtC(x, y0), PtC(x, head.Y - 20) }, Al(Color.White, 170 * sw), 2f);
                }
            }

            // 5. 폭발: 섬광 · 번개 기둥 · 부채꼴 번개 · 튀는 돌덩이 · 흙먼지
            if (u1 > 0) SlamSky(u1, 1f, ix, gy, frame, 100, true, bx, by);
            if (u2 > 0) SlamSky(u2, .55f, ix, gy, frame, 200, false, bx, by);

            // 6. 땅에 박힌 묠니르에서 계속 지직거리는 전류
            if (u1 > 0)
            {
                float k = Math.Max((float)Math.Exp(-u1 * 1.1f), u2 > 0 ? (float)Math.Exp(-u2 * 1.6f) * .8f : 0f) * outFade;
                var rh = new Random(frame * 19 + 7);
                for (int i = 0; i < 3; i++)
                {
                    if (rh.NextDouble() > .75) continue;
                    double a = rh.NextDouble() * Math.PI * 2; float r1 = 30 + rh.Next(55);
                    StrokeThin(BoltPath(head, PtC(head.X + (float)Math.Cos(a) * r1, head.Y + (float)Math.Sin(a) * r1), frame * 11 + i, 9, 4), .75f * k, 2.4f);
                }
            }
        }

        /// <summary>갈라진 대지: 충돌 지점에서 사방으로 번져 나가는 균열. 처음엔 하얗게 달아올랐다가 푸른 잔광으로 식고, 여진 때 다시 달아오른다.</summary>
        private void DrawSlamCracks(float T, float ix, float gy, float u1, float u2, int frame, float outFade)
        {
            if (u1 <= 0) return;
            var ice = Color.FromArgb(100, 190, 255); var cyan = Color.FromArgb(160, 225, 255);
            var rnd = new Random(4242);
            const int first = 11, total = 18;
            var paths = new List<PointF[]>();
            float h2 = u2 > 0 ? (float)Math.Exp(-u2 * 2.2f) : 0f;
            for (int i = 0; i < total; i++)
            {
                bool second = i >= first;
                double a = second ? rnd.NextDouble() * Math.PI * 2 : (i + rnd.NextDouble() * .8) * Math.PI * 2 / first;
                float L = second ? 50 + rnd.Next(90) : 70 + rnd.Next(115);
                float ub = (second ? u2 : u1);
                if (ub <= 0) continue;
                float gr = Math.Min(1f, ub / (second ? .3f : .38f)); gr = 1 - (1 - gr) * (1 - gr);
                PointF end = PtC(ix + (float)Math.Cos(a) * L * gr, gy + (float)Math.Sin(a) * L * gr * .26f);
                PointF[] path = BoltPath(Pt(ix, gy), end, 300 + i * 17, 9, 9);
                paths.Add(path);
                float al = outFade * (.55f + .45f * Math.Max((float)Math.Exp(-ub * 1.5f), h2 * .8f));
                Stroke(path, Al(ice, 120 * al), 24f);
                Stroke(path, Al(cyan, 210 * al), 11f);
                Stroke(path, Al(Color.White, 255 * al), 3.6f);
                if (i % 2 == 0 && gr > .5f)
                {
                    PointF s = path[4];
                    PointF e = PtC(s.X + (float)Math.Cos(a + .7) * 42 * gr, s.Y + (float)Math.Sin(a + .7) * 42 * gr * .17f);
                    StrokeThin(BoltPath(s, e, 700 + i, 5, 4), al, 2.2f);
                }
            }
            float heat = outFade * (.35f + .65f * Math.Max((float)Math.Exp(-u1 * 1.4f), h2));
            Glow(ix, gy, 190, 30, Al(ice, 130 * heat));

            // 균열 틈에서 솟구치는 푸른 번개 분출
            float jp = Math.Max((float)Math.Exp(-u1 * 1.1f), u2 > 0 ? .8f * (float)Math.Exp(-u2 * 1.4f) : 0f) * outFade;
            if (jp > .08f && paths.Count > 0)
            {
                var rj = new Random(frame * 3 + 1);
                for (int j = 0; j < 4; j++)
                {
                    PointF[] path = paths[rj.Next(paths.Count)];
                    PointF b = path[rj.Next(2, path.Length)];
                    float h = (24 + rj.Next(70)) * jp;
                    StrokeThin(BoltPath(b, Pt(b.X + rj.Next(-10, 11), b.Y - h), frame * 3 + j, 7, 4), jp, 2.8f);
                    Glow(b.X, b.Y - h * .4f, 16, 18 + h * .4f, Al(cyan, 150 * jp));
                }
            }
        }

        /// <summary>충돌 지점에서 땅을 따라 퍼지는 충격파 고리(3겹). 고리를 타고 번개가 튄다.</summary>
        private void SlamRings(float u, float power, float ix, float gy, int frame, int seed)
        {
            Color ice = Color.FromArgb(100, 190, 255), cyan = Color.FromArgb(160, 225, 255);
            for (int ri = 0; ri < 3; ri++)
            {
                float ur = u - ri * .13f; if (ur <= 0) continue;
                float r = 188f * (1f - (float)Math.Exp(-ur * 3.6f)) * (.55f + .45f * power);
                float f = Math.Max(0f, 1f - ur / 1.0f);
                if (f <= .01f) continue;
                float ry = r * .17f + 2f;
                Glow(ix, gy, r * 1.05f, ry * 1.8f + 6f, Al(Color.FromArgb(120, 200, 255), 70 * f * power));
                g.DrawEllipse(Pn(Al(ice, 190 * f * Math.Min(1f, power + .25f)), 11f - ri * 3f), ix - r, gy - ry, r * 2, ry * 2);
                g.DrawEllipse(Pn(Al(Color.White, 245 * f), 2.8f), ix - r, gy - ry, r * 2, ry * 2);
                g.DrawEllipse(Pn(Al(cyan, 130 * f), 2f), ix - r, gy - ry - 8f * f, r * 2, ry * 2);
                if (ri == 0)
                {
                    var rr = new Random(seed + frame * 5);
                    for (int i = 0; i < 6; i++)
                    {
                        double a = rr.NextDouble() * Math.PI * 2;
                        PointF s0 = PtC(ix + (float)Math.Cos(a) * r, gy + (float)Math.Sin(a) * ry);
                        PointF e0 = PtC(s0.X + rr.Next(-30, 31), s0.Y - 14 - rr.Next(40));
                        StrokeThin(BoltPath(s0, e0, frame * 7 + i + seed, 6, 4), f * power, 2.6f);
                    }
                }
            }
        }

        /// <summary>충돌 직후의 폭발(공중에 뜨는 부분). power 1 = 본 타격, .55 = 여진.</summary>
        private void SlamSky(float u, float power, float ix, float gy, int frame, int seed, bool main, float bx, float by)
        {
            Color ice = Color.FromArgb(100, 190, 255), cyan = Color.FromArgb(160, 225, 255);

            // 섬광: 충돌 지점이 하얗게 터지고, 본 타격에서는 몸 전체가 하얗게 번쩍인다
            float fl = Math.Max(0f, 1f - u / .25f);
            if (fl > .02f)
            {
                Glow(ix, gy - 30, 230 * power, 170 * power, Al(Color.FromArgb(200, 235, 255), 200 * fl));
                Glow(ix, gy - 20, 100 * power, 80 * power, Al(Color.White, 255 * fl));
                if (main) Glow(200 + bx, 340 + by, 160, 300, Al(Color.White, 150 * fl));
            }

            // 번개 기둥 + 부채꼴로 뻗는 번개: 프레임마다 모양이 바뀌어 지직거린다
            float pa = Math.Max(0f, 1f - u / .6f), grow = Math.Min(1f, u * 16f);
            if (pa > .02f)
            {
                PointF top = Pt(ix + (float)Math.Sin(frame * 1.7f) * 12f, gy - (160f + 150f * power) * (.3f + .7f * grow));
                StrokeBolt(BoltPath(Pt(ix, gy - 2), top, seed + frame, 26, 12), pa, power * 1.1f);
                int n = main ? 5 : 3;
                var rf = new Random(seed + frame * 7);
                for (int i = 0; i < n; i++)
                {
                    float a = ((float)i / (n - 1) - .5f) * 2.27f, len = (130 + rf.Next(90)) * power * grow;
                    PointF s0 = Pt(ix, gy - 6);
                    PointF e0 = PtC(ix + (float)Math.Sin(a) * len, gy - 6 - (float)Math.Cos(a) * len * .9f);
                    PointF[] bolt = BoltPath(s0, e0, seed + frame * 7 + i, 14, 7);
                    StrokeBolt(bolt, pa * .85f, power * .4f);
                    StrokeThin(bolt, pa, 2.6f);
                }
            }

            // 튀어 오르는 돌덩이: 포물선으로 날아가 땅에 떨어진다
            int nd = main ? 16 : 7;
            var rd = new Random(seed);
            for (int i = 0; i < nd; i++)
            {
                float vx = (float)(rd.NextDouble() * 2 - 1) * 270f * (.5f + .5f * power);
                float vy = -(260f + rd.Next(300)) * (.6f + .4f * power);
                float sz = 5f + rd.Next(8), rv = rd.Next(-700, 700);
                float x = ix + vx * u, y = gy - 6f + vy * u + 520f * u * u;
                if (y > gy + 2f || u > 1.8f || x < -40f || x > 440f) continue;
                double r = rv * u * Math.PI / 180.0;
                float cs = (float)Math.Cos(r), sn = (float)Math.Sin(r);
                float[,] sh = { { -1f, -.6f }, { .8f, -.8f }, { 1f, .5f }, { -.5f, 1f } };
                var rock = new PointF[4];
                for (int q = 0; q < 4; q++)
                    rock[q] = Pt(x + (sh[q, 0] * cs - sh[q, 1] * sn) * sz, y + (sh[q, 0] * sn + sh[q, 1] * cs) * sz);
                g.FillPolygon(B(Color.FromArgb(92, 78, 86)), rock);
                g.DrawPolygon(Pn(Al(cyan, 200 * (1f - u / 1.8f)), 1.8f), rock);
            }

            // 흙먼지: 좌우로 번지며 옅어지는 구름
            for (int sd = -1; sd <= 1; sd += 2)
                for (int j = 0; j < 3; j++)
                {
                    float x = ix + sd * (26 + (60 + 40 * j) * (1f - (float)Math.Exp(-u * 2.6f)));
                    float y = gy - 12 - j * 5 - 12 * u, rr = 26 + 14 * j + 26 * u;
                    float al = Math.Max(0f, 1f - u / 1.5f) * (.9f - .15f * j) * Math.Min(1f, power + .3f);
                    Glow(x, y, rr * 1.3f, rr * .8f, Al(Color.FromArgb(176, 172, 182), 120 * al));
                }

            // 본 타격: 감전된 듯 온몸에 전기가 튄다
            if (main && u < 1f)
            {
                float shock = (float)Math.Exp(-u * 3.2f);
                float ccx = 200 + bx, ccy = 335 + by;
                var rs = new Random(frame * 17 + 5);
                int n = 2 + (int)(9 * shock);
                for (int i = 0; i < n; i++)
                {
                    double a = rs.NextDouble() * Math.PI * 2;
                    PointF s0 = Pt(ccx + (float)Math.Cos(a) * (60 + rs.Next(50)), ccy + (float)Math.Sin(a) * (120 + rs.Next(95)));
                    double d = rs.NextDouble() * Math.PI * 2; float len = 40 + rs.Next(60);
                    PointF e0 = PtC(s0.X + (float)Math.Cos(d) * len, s0.Y + (float)Math.Sin(d) * len);
                    StrokeThin(BoltPath(s0, e0, frame * 23 + i, 14, 4), Math.Min(1f, shock + .2f), 2.6f);
                }
            }
        }
    }

    /// <summary>토르 '묠니르 풍차 회전'의 시간표. T = 회전이 시작된 뒤 지난 초.</summary>
    internal static class ThorWhirl
    {
        internal const float Duration = 3.9f;

        private static float E(float x) { x = Math.Max(0f, Math.Min(1f, x)); return x * x * (3 - 2 * x); }

        /// <summary>초당 회전 수: 3 → 12(1.2초 동안 가속) → 유지 → 3(3.0~3.8초 감속)</summary>
        internal static float Speed(float T)
        {
            return 3f + 9f * Math.Min(1f, T / 1.2f) - 9f * Math.Max(0f, Math.Min(1f, (T - 3.0f) / .8f));
        }

        /// <summary>누적 회전 수(Speed 의 적분)</summary>
        internal static float Rev(float T)
        {
            float up = T < 1.2f ? 9f * T * T / 2.4f : 9f * (T - .6f);
            float dn = T < 3.0f ? 0f : T < 3.8f ? 9f * (T - 3f) * (T - 3f) / 1.6f : 9f * (T - 3.8f) + 3.6f;
            return 3f * T + up - dn;
        }

        /// <summary>연출 세기(0..1): 서서히 커지고, 끝에서 서서히 잦아든다.</summary>
        internal static float Intensity(float T) { return E(T / .9f) * E((Duration - T) / .7f); }
    }

    /// <summary>토르 '지면 강타 충격파'의 시간표. 시그니처 시작부터의 초.</summary>
    internal static class ThorSlam
    {
        internal const float Hit1 = 1.8f, Hit2 = 4.2f, End = 5.6f;   // 본 타격 / 여진("쿵--!!") / 연출 종료
        internal const float ImpactX = 150f, HandY = 502f;            // 내리친 자리(리그 좌표). 확대된 묠니르 머리 밑동이 땅에 닿는 손 높이

        private static float E(float x) { x = Math.Max(0f, Math.Min(1f, x)); return x * x * (3 - 2 * x); }

        /// <summary>내리치는 진행도(0..1). 가속하며 떨어져 충돌 순간 최고 속도.</summary>
        internal static float Swing(float t)
        {
            float x = Math.Max(0f, Math.Min(1f, (t - 1.5f) / (Hit1 - 1.5f)));
            return x * x;
        }

        /// <summary>묠니르 확대 배율: 기를 모으면서 커지고, 끝에서 원래대로 돌아온다.</summary>
        internal static float Scale(float t) { return 1f + .5f * E(t / .7f) * (1f - E((t - 4.8f) / .8f)); }

        /// <summary>묠니르 회전각(도): 머리가 위를 보다가 앞쪽으로 휘둘러 땅을 향한다.</summary>
        internal static float Angle(float t) { return -180f * Swing(t) * (1f - E((t - 4.8f) / .8f)); }
    }
}
