using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace SuperHeroes
{
    // 스파이더맨 시그니처 연출: 웹 스윙 · 스파이더 센스 · 거꾸로 매달리기 · 거미줄 네트
    internal sealed partial class HeroPainter
    {
        private static readonly Color cWeb = Color.FromArgb(246, 248, 255);
        private static readonly Color cSpRed = Color.FromArgb(222, 40, 46), cSpBlue = Color.FromArgb(36, 92, 196);

        /// <summary>거미줄 한 가닥(그림자 + 흰 줄 + 가는 반사광).</summary>
        private void WebStrand(PointF[] pts, float a, float w)
        {
            if (pts.Length < 2 || a <= .02f) return;
            Stroke(pts, Al(Color.FromArgb(90, 100, 130), 150 * a), w + 2.4f);
            Stroke(pts, Al(cWeb, 255 * a), w);
            Stroke(pts, Al(Color.White, 220 * a), Math.Max(1f, w * .35f));
        }

        private PointF[] WebLine(PointF a, PointF b, float sag, float wob, float T, int n = 14)
        {
            var o = new PointF[n + 1];
            float dx = b.X - a.X, dy = b.Y - a.Y, len = Math.Max(1f, (float)Math.Sqrt(dx * dx + dy * dy)), nx = -dy / len, ny = dx / len;
            for (int i = 0; i <= n; i++)
            {
                float f = i / (float)n, env = (float)Math.Sin(f * Math.PI);
                float off = sag * env + (float)Math.Sin(f * Math.PI * 3 - T * 38) * wob * env;
                o[i] = Pt(a.X + dx * f + nx * off, a.Y + dy * f + ny * off);
            }
            return o;
        }

        /// <summary>사방으로 퍼지는 거미줄 자국(앵커가 박히는 자리, 맞은 곳 등).</summary>
        private void WebSplat(float x, float y, float r, float a)
        {
            if (a <= .02f || r < 2f) return;
            for (int i = 0; i < 8; i++)
            {
                double ang = i * Math.PI / 4 + .2;
                Stroke(new[] { Pt(x, y), PtC(x + (float)Math.Cos(ang) * r, y + (float)Math.Sin(ang) * r) }, Al(cWeb, 255 * a), 2.2f);
            }
            for (int k2 = 1; k2 <= 2; k2++)
            {
                var ring = new PointF[9];
                for (int i = 0; i <= 8; i++) { double ang = i * Math.PI / 4 + .2; float rr = r * k2 / 2.4f * (i % 2 == 0 ? 1f : .86f); ring[i] = PtC(x + (float)Math.Cos(ang) * rr, y + (float)Math.Sin(ang) * rr); }
                Stroke(ring, Al(cWeb, 220 * a), 1.8f);
            }
        }

        private void DrawShuriken(float x, float y, float rot, float sc, float a)
        {
            var st = g.Save(); g.TranslateTransform(x, y); g.RotateTransform(rot); g.ScaleTransform(sc, sc);
            var star = new[] { Pt(0, -16), Pt(4, -4), Pt(16, 0), Pt(4, 4), Pt(0, 16), Pt(-4, 4), Pt(-16, 0), Pt(-4, -4) };
            g.FillPolygon(B(Al(Color.FromArgb(40, 42, 52), 255 * a)), star);
            g.DrawPolygon(Pn(Al(Color.FromArgb(210, 218, 232), 255 * a), 1.8f), star);
            g.FillEllipse(B(Al(Color.FromArgb(230, 50, 50), 255 * a)), -3, -3, 6, 6);
            g.Restore(st);
        }

        // ───────── 웹 스윙 ─────────
        // "THWIP!" 손목에서 거미줄이 쏘아져 천장에 척 달라붙고 → 팽팽한 줄에 매달려 시계추처럼 크게 흔들리며(빨강·파랑 잔상 + 속도선 + 도심의 불빛) → 가볍게 착지.

        private void DrawWebSwingFx()
        {
            float T = p.SigT, bx = p.BodyX, by = p.BodyY;
            PointF A = Pt(70, 0), hand = wrist[1];
            float ramp = SpiderMoves.SwingRamp(T);
            float hold = Ease01((T - .9f) / .25f) * (1 - Ease01((T - 4.4f) / .6f));

            // 1. 속도선·도심 불빛: 진자 속도에 비례해 흐른다
            if (ramp > .02f)
            {
                float vel = (float)Math.Cos((T - 1.2f) * 2.5f), spd = Math.Abs(vel) * ramp;
                for (int i = 0; i < 18; i++)
                {
                    float ph = (T * 2.4f * (vel >= 0 ? -1f : 1f) + i * .0556f); ph -= (float)Math.Floor(ph);
                    float y = 20 + Hash01(i, 5) * 520, len = 70 + 110 * Hash01(i, 6), x = -40 + ph * (480 + len) - len;
                    Stroke(new[] { PtC(x, y), PtC(x + len, y) }, Al(i % 4 == 0 ? Color.FromArgb(255, 230, 150) : Color.White, 150 * spd * (.4f + .6f * Hash01(i, 7))), 2f);
                }
            }

            // 2. 빨강·파랑 잔상 리본
            if (T > 1.4f && T < 4.7f)
            {
                var pts = new List<PointF>();
                for (int j = 0; j < 18; j++) { float tj = T - j * .035f; if (tj < 1.2f) break; pts.Add(Pt(200 + SpiderMoves.SwingDx(tj), 330 + SpiderMoves.SwingDy(tj))); }
                if (pts.Count > 3)
                {
                    for (int j = 0; j < pts.Count - 1; j++)
                    {
                        float f = 1f - j / (float)pts.Count;
                        var seg = new[] { pts[j], pts[j + 1] };
                        Stroke(seg, Al(cSpRed, 150 * f * ramp), 48 * f + 4); Stroke(new[] { Pt(seg[0].X, seg[0].Y + 10), Pt(seg[1].X, seg[1].Y + 10) }, Al(cSpBlue, 130 * f * ramp), 36 * f + 3);
                        Stroke(seg, Al(Color.White, 170 * f * ramp), 10 * f + 1);
                    }
                }
            }

            // 3. 거미줄: 쏘아 보냄 → 앵커에 척 → 흔들리는 팽팽한 줄
            if (hold > .02f)
            {
                float u = Math.Min(1f, Math.Max(0f, (T - .95f) / .22f)), e = 1 - (1 - u) * (1 - u);
                PointF tip = Lerp2(hand, A, e);
                float sag = (T < 1.4f ? 14f * (1 - u) : 6f * (float)Math.Exp(-(T - 1.4f) * 2f)), wob = T < 1.5f ? 0f : 7f * (float)Math.Exp(-(T - 1.5f) * 3f);
                WebStrand(WebLine(hand, tip, sag, wob, T), hold, 3.6f);
                if (u >= 1f) WebSplat(A.X, A.Y + 4, 26, hold);
                if (u < 1f) Glint(tip.X, tip.Y, 16, Color.White, 1f, T * 300);
            }

            // 4. THWIP!: 손목에서 터지는 파문과 만화 글씨
            if (T >= .95f && T < 1.9f)
            {
                float u = (T - .95f) / .95f, fa = 1 - u;
                g.DrawEllipse(Pn(Al(cWeb, 230 * fa), 3f), hand.X - 14 - 70 * u, hand.Y - 14 - 70 * u, 28 + 140 * u, 28 + 140 * u);
                Starburst(hand.X, hand.Y, 44 * Math.Min(1f, u * 7), 20 * Math.Min(1f, u * 7), 9, 8f, Al(Color.FromArgb(255, 238, 140), 220 * fa), Al(cSpRed, 255 * fa), 3f);
                ComicText("THWIP!", 98, hand.Y - 72, 44 + 6 * (float)Math.Sin(u * 20), -10f, cSpRed, Math.Min(1f, fa * 2f));
            }
            if (T >= 2.3f && T < 3.1f)
                ComicText("WHOOSH!", 316, 160, 30, 9f, Color.FromArgb(120, 190, 255), Math.Min(1f, (3.1f - T) * 1.6f) * Math.Min(1f, (T - 2.3f) * 5f));

            // 5. 착지(4.5~): 먼지 고리와 "THUD!"
            if (T >= 4.5f && T < 5.6f)
            {
                float u = (T - 4.5f) / 1.1f;
                GroundRing(200 + bx, 568, 20 + 130 * (float)Math.Sqrt(u), .17f, Color.FromArgb(214, 224, 240), (1 - u), 3f);
                ComicText("THUD!", 300, 506, 28, -7f, Color.FromArgb(255, 220, 90), Math.Min(1f, (1 - u) * 2.2f));
            }
        }

        // ───────── 스파이더 센스 ─────────
        // 머리 위에 "!!", 머리 주변에 물결치는 위험 파동과 찌릿찌릿한 번개 → 날아오는 수리검을 감지 → 백플립으로 아슬아슬하게 피한다(회전 궤적).

        private void DrawSpiderSenseFx()
        {
            float T = p.SigT, bx = p.BodyX, by = p.BodyY;
            int frame = (int)(T * 30);
            Color yel = Color.FromArgb(255, 218, 40), red = Color.FromArgb(255, 56, 50);
            float alert = Ease01(T / .3f) * (1 - Ease01((T - 2.4f) / .8f));
            PointF hc = Pt(200 + p.HeadX + bx, headCy + p.HeadY + by);

            // 1. 위험 경보: 붉은 빛과 머리 주변 파동
            if (alert > .02f)
            {
                Glow(200 + bx, 330, 160, 270, Al(red, (40 + 36 * (float)Math.Abs(Math.Sin(T * 11))) * alert));
                for (int i = 0; i < 3; i++)
                {
                    float ph = (T * 1.9f + i / 3f) % 1f, r = 40 + 118 * ph;
                    g.DrawEllipse(Pn(Al(i % 2 == 0 ? yel : red, 200 * (1 - ph) * alert), 4.4f), hc.X - r, hc.Y - r * .92f, r * 2, r * 1.84f);
                }
                var rs = new Random(frame * 13 + 3);
                for (int i = 0; i < 16; i++)
                {
                    double ang = rs.NextDouble() * Math.PI * 2; float r0 = 78 + rs.Next(22), r1 = r0 + 26 + rs.Next(26);
                    PointF s0 = Pt(hc.X + (float)Math.Cos(ang) * r0, hc.Y + (float)Math.Sin(ang) * r0 * .92f), e0 = Pt(hc.X + (float)Math.Cos(ang) * r1, hc.Y + (float)Math.Sin(ang) * r1 * .92f);
                    StrokeThin(BoltPath(s0, e0, frame * 7 + i, 7, 3), (i % 3 == 0 ? .9f : .7f) * alert, 2.8f);
                }
            }

            // 2. 머리 위의 "!!"
            if (T >= .12f && T < 1.9f)
            {
                float u = (T - .12f) / 1.78f, pop = Ease01(u * 9f), fa = Math.Min(1f, (1 - u) * 3.5f);
                float cy = hc.Y - 130;
                Starburst(hc.X, cy, 46 * pop, 24 * pop, 10, 10f * (float)Math.Sin(T * 30), Al(yel, 245 * fa), Al(red, 255 * fa), 4f);
                ComicText("!!", hc.X, cy, 52 * pop + 1, 0f, red, fa * pop);
            }

            // 3. 날아오는 수리검: 몸이 있던 자리를 아슬아슬하게 스쳐 지나간다
            float[] t0s = { 1.55f, 2.0f, 2.5f }, ys = { 330f, 255f, 395f };
            for (int i = 0; i < 3; i++)
            {
                float u = (T - t0s[i]) / .55f; if (u < 0 || u > 1) continue;
                float x = 470 - u * 520, y = ys[i];
                Stroke(new[] { PtC(x + 120, y), PtC(x, y) }, Al(red, 180), 8f); Stroke(new[] { PtC(x + 120, y), PtC(x, y) }, Al(Color.White, 230), 2.4f);
                DrawShuriken(x, y, T * 900f + i * 40, 1.3f, 1f);
                if (Math.Abs(x - 200) < 40) { Glint(200, y, 34, Color.White, 1 - Math.Abs(x - 200) / 40f, 45); }
            }

            // 4. 백플립: 회전 궤적(머리 쪽·발 쪽 두 줄기)과 도약·착지의 먼지
            float fu = (T - 1.8f) / 1.6f;
            if (fu > 0 && fu < 1.15f)
            {
                float ang = SpiderMoves.FlipAngle(T), fa = Math.Min(1f, (1.15f - fu) * 3f) * Math.Min(1f, fu * 8f);
                float cy = 340f;
                for (int k2 = 0; k2 < 2; k2++)
                    for (int j = 0; j < 14; j++)
                    {
                        float a0 = (k2 == 0 ? -90f : 90f) + ang - j * 7f, f = 1f - j / 14f;
                        float rad = (k2 == 0 ? 132f : 118f);
                        double r0 = a0 * Math.PI / 180, r1 = (a0 - 7f) * Math.PI / 180;
                        var seg = new[] { PtC(200 + (float)Math.Cos(r0) * rad, cy + (float)Math.Sin(r0) * rad), PtC(200 + (float)Math.Cos(r1) * rad, cy + (float)Math.Sin(r1) * rad) };
                        Stroke(seg, Al(k2 == 0 ? Color.FromArgb(150, 200, 255) : cSpRed, 150 * f * fa), 18 * f + 3);
                        Stroke(seg, Al(Color.White, 230 * f * fa), 5 * f + 1);
                    }
            }
            if (T >= 1.75f && T < 2.5f) GroundRing(200, 568, 20 + 120 * (float)Math.Sqrt((T - 1.75f) / .75f), .17f, Color.FromArgb(220, 230, 245), 1 - (T - 1.75f) / .75f, 3f);
            if (T >= 3.35f && T < 4.3f)
            {
                float u = (T - 3.35f) / .95f;
                GroundRing(200, 568, 20 + 130 * (float)Math.Sqrt(u), .17f, Color.FromArgb(220, 230, 245), 1 - u, 3.2f);
                for (int i = 0; i < 8; i++) Glint(200 + (Hash01(i, 1) * 2 - 1) * 120, 540 - u * 120 * Hash01(i, 2), 8 + 8 * Hash01(i, 3), yel, 1 - u, T * 90 + i * 30);
                ComicText("SAFE!", 316, 470, 32, 8f, Color.FromArgb(120, 230, 120), Math.Min(1f, (1 - u) * 2.2f));
            }
        }

        // ───────── 거꾸로 매달리기 ─────────
        // 천장에 쏜 거미줄 한 가닥에 거꾸로 매달려 시계추처럼 흔들리며 → 피자 한 조각을 한 입씩 냠냠(김이 모락모락, 반짝이는 별).

        private void DrawHangFx()
        {
            float T = p.SigT, u = Math.Min(1f, p.UpsideDown);
            Color cheese = Color.FromArgb(255, 206, 70), crust = Color.FromArgb(214, 150, 70), pep = Color.FromArgb(196, 52, 44);

            // 1. 매달리기 전: 천장으로 쏘는 거미줄
            if (T >= .55f && T < 1.6f)
            {
                PointF hand = wrist[1], ceil = Pt(170, -210);
                float s = Ease01((T - .55f) / .3f), fa = 1 - Ease01((T - 1.25f) / .35f);
                WebStrand(WebLine(hand, Lerp2(hand, ceil, s), 0, 0, T, 10), fa, 3.4f);
                if (s >= 1f) WebSplat(ceil.X, ceil.Y, 28, fa);
                if (T < .95f) ComicText("THWIP!", 120, 70, 38, -8f, cSpRed, Math.Min(1f, (.95f - T) * 5f));
            }

            if (u < .5f) return;
            // (이후는 뒤집힌 좌표계: 리그 아래쪽(+y) = 화면 위쪽(천장))
            // 2. 발목에서 천장까지 한 가닥 줄 + 천장 고정점
            float ta = Ease01((T - 1.5f) / .4f) * Ease01((7.0f - T) / .5f);
            PointF ft = Pt(200, 566), top = Pt(200, 571 + 250);
            WebStrand(new[] { ft, Lerp2(ft, top, .5f), top }, ta, 3.6f);
            WebSplat(top.X, top.Y, 34, ta);
            for (int i = 0; i < 4; i++) Glint(200 + (Hash01(i, 1) - .5f) * 8, 571 + 40 + i * 52, 6, Color.White, ta * (.4f + .6f * (float)Math.Abs(Math.Sin(T * 3 + i))), 0);

            // 3. 피자 한 조각(오른손) — 한 입씩 줄어든다
            PointF h = wrist[1];
            float[] bites = { 2.9f, 3.6f, 4.3f, 5.0f };
            int nb = 0; float chew = 0f;
            for (int i = 0; i < bites.Length; i++) { if (T >= bites[i]) nb++; if (T >= bites[i] && T < bites[i] + .4f) chew = 1 - (T - bites[i]) / .4f; }
            float pa = Ease01((T - 2.0f) / .4f) * Ease01((5.9f - T) / .4f);
            if (pa > .02f)
            {
                float sc = 1f - nb * .17f;
                // 슬라이스는 손 위쪽(리그 y 감소 = 화면 아래)이 아니라, 손에서 바깥으로 뻗는다
                var st = g.Save(); g.TranslateTransform(h.X, h.Y + 6); g.RotateTransform(-18f); g.ScaleTransform(sc, sc);
                var tri = new[] { Pt(0, 0), Pt(-30, -62), Pt(30, -62) };
                g.FillPolygon(B(Al(cheese, 255 * pa)), tri);
                g.FillPolygon(B(Al(crust, 255 * pa)), new[] { Pt(-30, -62), Pt(30, -62), Pt(28, -72), Pt(-28, -72) });
                g.DrawPolygon(Pn(Al(cInk, 255 * pa), 2.4f), tri);
                g.FillEllipse(B(Al(pep, 255 * pa)), -9, -38, 14, 14); g.FillEllipse(B(Al(pep, 255 * pa)), 4, -52, 12, 12); g.FillEllipse(B(Al(pep, 255 * pa)), -20, -56, 11, 11);
                for (int i = 0; i < nb; i++)   // 베어 먹은 자국
                {
                    float by2 = -64 + i * 0f; float bxs = (i % 2 == 0 ? -1 : 1) * (24 - i * 4);
                    g.FillEllipse(B(Al(Color.FromArgb(70, 80, 110), 255 * pa)), bxs - 9, -68 - i * 7, 18, 18);
                }
                g.Restore(st);
                // 김
                for (int i = 0; i < 3; i++)
                {
                    float ph = (T * .8f + i * .33f) % 1f;
                    Stroke(new[] { Pt(h.X - 12 + i * 12, h.Y + 22), Pt(h.X - 12 + i * 12 + (float)Math.Sin(ph * 6 + i) * 6, h.Y + 46 + ph * 30), Pt(h.X - 12 + i * 12 + (float)Math.Sin(ph * 6 + i + 2) * 6, h.Y + 70 + ph * 30) }, Al(Color.White, 170 * (1 - ph) * pa), 3f);
                }
            }
            // 4. 냠냠: 한 입 베어 물 때마다 반짝이는 별과 하트
            if (chew > .02f)
                for (int i = 0; i < 6; i++)
                    Glint(200 + (Hash01(i, 2) * 2 - 1) * 60, 150 + 22 + (Hash01(i, 3)) * 50, 8 + 8 * Hash01(i, 4), i % 2 == 0 ? Color.FromArgb(255, 220, 90) : Color.FromArgb(255, 150, 190), chew, T * 100 + i * 25);
        }

        // ───────── 거미줄 네트 ─────────
        // 양손 웹슈터를 동시에 쏘면 → 거미줄 뭉치가 날아가 활짝 펼쳐지며 → 방사형 그물이 팽팽하게 달라붙고 → 그물에 걸린 악당이 버둥댄다(꼼짝 마라!).

        private void DrawWebNetFx()
        {
            float T = p.SigT, bx = p.BodyX, by = p.BodyY;
            PointF N = Pt(86, 332), hl = wrist[0], hr = wrist[1];
            float net = Ease01((T - 1.55f) / .85f), hold = Ease01((6.2f - T) / .6f);
            float R = (118f - 20f * Ease01((T - 3.0f) / .8f)) * net;
            float jolt = T > 2.4f ? (float)Math.Exp(-(T - 2.4f) * 2.6f) * (float)Math.Sin((T - 2.4f) * 38) * 4f : 0f;

            // 1. 발사: 양손에서 거미줄 뭉치가 날아간다
            if (T >= 1.2f && T < 1.65f)
            {
                float u = (T - 1.2f) / .45f;
                foreach (PointF h0 in new[] { hl, hr })
                {
                    PointF q = Lerp2(h0, N, u * u);
                    WebStrand(WebLine(h0, q, 0, 0, T, 8), 1f, 3.4f);
                    g.FillEllipse(B(cWeb), q.X - 9, q.Y - 9, 18, 18); g.DrawEllipse(Pn(Al(cInk, 255), 1.6f), q.X - 9, q.Y - 9, 18, 18);
                }
                g.DrawEllipse(Pn(Al(cWeb, 220 * (1 - u)), 3f), hr.X - 12 - 60 * u, hr.Y - 12 - 60 * u, 24 + 120 * u, 24 + 120 * u);
                if (T < 1.55f) ComicText("THWIP!", 130, hr.Y - 70, 36, -9f, cSpRed, Math.Min(1f, (1.55f - T) * 7f));
            }

            // 2. 그물: 방사형 살 + 안쪽으로 처진 동심원
            if (net > .02f && hold > .02f)
            {
                float spin = (1 - net) * 1.3f;
                const int spokes = 12;
                for (int i = 0; i < spokes; i++)
                {
                    double ang = i * Math.PI * 2 / spokes + spin;
                    WebStrand(new[] { Pt(N.X + jolt, N.Y), PtC(N.X + jolt + (float)Math.Cos(ang) * R * .5f, N.Y + (float)Math.Sin(ang) * R * .5f * .98f), PtC(N.X + jolt + (float)Math.Cos(ang) * R, N.Y + (float)Math.Sin(ang) * R) }, hold, 2.8f);
                }
                for (int k2 = 1; k2 <= 6; k2++)
                {
                    float rr = R * k2 / 6f;
                    var ring = new List<PointF>();
                    for (int i = 0; i <= spokes; i++)
                    {
                        double a0 = i * Math.PI * 2 / spokes + spin, a1 = (i + .5) * Math.PI * 2 / spokes + spin;
                        ring.Add(PtC(N.X + jolt + (float)Math.Cos(a0) * rr, N.Y + (float)Math.Sin(a0) * rr));
                        if (i < spokes) ring.Add(PtC(N.X + jolt + (float)Math.Cos(a1) * rr * .9f, N.Y + (float)Math.Sin(a1) * rr * .9f));
                    }
                    WebStrand(ring.ToArray(), hold, 2.4f);
                }
                for (int i = 0; i < 10; i++) Glint(N.X + (Hash01(i, 1) * 2 - 1) * R * .85f, N.Y + (Hash01(i, 2) * 2 - 1) * R * .85f, 6 + 6 * Hash01(i, 3), Color.White, hold * net * (.3f + .7f * (float)Math.Abs(Math.Sin(T * 4 + i))), 0);
                // 그물을 팽팽히 당기는 두 줄기
                float tug = T > 2.4f ? 1f : 0f;
                foreach (PointF h0 in new[] { hl, hr })
                {
                    PointF edge = PtC(N.X + R * .86f, N.Y + (h0 == hl ? -.5f : .5f) * R * .5f);
                    WebStrand(WebLine(h0, edge, 4f * (1 - tug), 0f, T, 8), hold, 3f);
                }
            }

            // 3. 충돌: 철퍼덕!
            if (T >= 2.35f && T < 3.2f)
            {
                float u = (T - 2.35f) / .85f, fa = 1 - u;
                Starburst(N.X, N.Y, 100 * Math.Min(1f, u * 8), 44 * Math.Min(1f, u * 8), 11, 6f, Al(Color.FromArgb(255, 238, 150), 210 * fa), Al(cSpRed, 255 * fa), 3.2f);
                ComicText("SPLAT!", N.X + 6, N.Y - 126, 38, -6f, Color.FromArgb(255, 220, 70), Math.Min(1f, fa * 2f));
                for (int i = 0; i < 14; i++)
                {
                    double ang = i * Math.PI * 2 / 14; float r0 = 24, r1 = 24 + 80 * Math.Min(1f, u * 4);
                    Stroke(new[] { PtC(N.X + (float)Math.Cos(ang) * r0, N.Y + (float)Math.Sin(ang) * r0), PtC(N.X + (float)Math.Cos(ang) * r1, N.Y + (float)Math.Sin(ang) * r1) }, Al(Color.White, 230 * fa), 2.4f);
                }
            }

            // 4. 그물에 갇힌 악당: 거미줄에 칭칭 감겨 버둥댄다
            float cocoon = Ease01((T - 2.6f) / .5f) * hold;
            if (cocoon > .02f)
            {
                float wig = (float)Math.Sin(T * 13) * 5f * (T < 4.8f ? 1f : Math.Max(0f, 1 - (T - 4.8f) / .8f)), cx = N.X + wig * .4f, cy = N.Y + 6;
                var st = g.Save(); g.TranslateTransform(cx, cy); g.RotateTransform(wig);
                g.FillEllipse(B(Al(Color.FromArgb(78, 82, 108), 255 * cocoon)), -27, -42, 54, 84); g.DrawEllipse(Pn(Al(cInk, 255 * cocoon), 2.4f), -27, -42, 54, 84);
                g.FillRectangle(B(Al(Color.FromArgb(52, 54, 72), 255 * cocoon)), -18, 38, 11, 16); g.FillRectangle(B(Al(Color.FromArgb(52, 54, 72), 255 * cocoon)), 7, 38, 11, 16);   // 삐져나온 다리
                for (int i = 0; i < 5; i++) Stroke(new[] { Pt(-27 + i * 4, -26 + i * 15), Pt(27 - i * 4, -14 + i * 15) }, Al(cWeb, 245 * cocoon), 4f);
                for (int i = 0; i < 3; i++) Stroke(new[] { Pt(-20 + i * 20, -40), Pt(-20 + i * 20 + 8, 40) }, Al(cWeb, 200 * cocoon), 2.4f);
                g.FillEllipse(B(Al(Color.White, 255 * cocoon)), -17, -30, 14, 16); g.FillEllipse(B(Al(Color.White, 255 * cocoon)), 3, -30, 14, 16);   // 휘둥그레진 눈
                float pr = (float)Math.Sin(T * 7) * 2.4f;
                g.FillEllipse(B(Al(cInk, 255 * cocoon)), -13 + pr, -24, 6, 7); g.FillEllipse(B(Al(cInk, 255 * cocoon)), 7 + pr, -24, 6, 7);
                g.Restore(st);
                if (T > 3.2f) ComicText("?!", cx + 40, cy - 56, 30, 8f, cSpRed, cocoon * (.6f + .4f * (float)Math.Abs(Math.Sin(T * 6))));
                if (T > 4.8f) for (int i = 0; i < 6; i++) Glint(cx + (Hash01(i, 5) * 2 - 1) * 60, cy - 60 + (Hash01(i, 6) * 2 - 1) * 40, 8, Color.FromArgb(255, 220, 90), cocoon * Ease01((T - 4.8f) / .4f), T * 90 + i * 30);
            }
        }
    }

    /// <summary>스파이더맨 시그니처의 몸 움직임 시간표(포즈와 연출이 같은 식을 쓴다).</summary>
    internal static class SpiderMoves
    {
        private static float E(float x) { x = Math.Max(0f, Math.Min(1f, x)); return x * x * (3 - 2 * x); }

        /// <summary>웹 스윙의 흔들림 세기: 1.2초부터 서서히 커졌다가 4.2~4.5초에 잦아든다.</summary>
        internal static float SwingRamp(float t) { return E((t - 1.2f) / .4f) * (1 - E((t - 4.2f) / .3f)); }
        internal static float SwingSin(float t) { return (float)Math.Sin((t - 1.2f) * 2.5f); }
        internal static float SwingDx(float t) { return 38f * SwingSin(t) * SwingRamp(t); }
        internal static float SwingDy(float t) { return -(8f + 38f * Math.Abs(SwingSin(t))) * SwingRamp(t); }

        /// <summary>백플립 회전각(도): 1.8~3.4초 동안 한 바퀴.</summary>
        internal static float FlipAngle(float t) { return 360f * E((t - 1.8f) / 1.6f); }
    }
}
