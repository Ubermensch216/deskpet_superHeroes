using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace SuperHeroes
{
    // 배트맨 '중무장 아머 슈트' 연출.
    // 조사 메모: 1989년 수트는 가슴 갑옷·황동 벨트·노란 타원 엠블럼, 코믹스의 중장갑은 건틀릿 사출물·부츠 칼날·어깨 추진기/발사기,
    // 헬뱃(XE) 수트는 어두운 강철판에 붉게 빛나는 라인과 빛나는 렌즈가 특징이다. 그것들을 모아 '부품이 날아와 찰칵 장착 → 시스템 온라인 → 무장 시험' 으로 연출한다.
    internal sealed partial class HeroPainter
    {
        private static readonly Color cSteel = Color.FromArgb(50, 56, 72), cSteelHi = Color.FromArgb(128, 140, 166), cSteelLo = Color.FromArgb(26, 30, 40);
        private static readonly Color cHell = Color.FromArgb(255, 72, 58), cBatYel = Color.FromArgb(255, 214, 60);

        /// <summary>HUD 느낌의 모노스페이스 글씨(좌우 반전 리그에서도 글자가 뒤집히지 않는다).</summary>
        private void HudText(string s, float x, float y, float size, Color c, float a)
        {
            if (a <= .02f) return;
            var st = g.Save();
            g.TranslateTransform(x, y); g.ScaleTransform(faceRight ? -1f : 1f, 1f);
            using (var f = new Font("Consolas", size, FontStyle.Bold, GraphicsUnit.Pixel))
                g.DrawString(s, f, B(Al(c, 255 * a)), 0, 0);
            g.Restore(st);
        }

        /// <summary>강철 장갑판 한 장: 어두운 면 + 밝은 모서리, 켜지면 붉게 발광한다.</summary>
        private void ArmorPoly(PointF[] pts, float ox, float oy, float a, float lit)
        {
            if (a <= .02f) return;
            var q = new PointF[pts.Length];
            for (int i = 0; i < pts.Length; i++) q[i] = Pt(pts[i].X + ox, pts[i].Y + oy);
            g.FillPolygon(B(Al(cSteel, 255 * a)), q);
            g.DrawPolygon(Pn(Al(cSteelHi, 255 * a), 2.4f), q);
            if (lit > .02f)
            {
                g.DrawPolygon(Pn(Al(cHell, 150 * lit * a), 7f), q);
                g.DrawPolygon(Pn(Al(Color.FromArgb(255, 210, 190), 255 * lit * a), 1.8f), q);
            }
        }

        private void ArmorEllipse(float cx, float cy, float rx, float ry, float rot, float ox, float oy, float a, float lit)
        {
            if (a <= .02f) return;
            var st = g.Save();
            g.TranslateTransform(cx + ox, cy + oy); g.RotateTransform(rot);
            g.FillEllipse(B(Al(cSteel, 255 * a)), -rx, -ry, rx * 2, ry * 2);
            g.DrawEllipse(Pn(Al(cSteelHi, 255 * a), 2.4f), -rx, -ry, rx * 2, ry * 2);
            g.DrawArc(Pn(Al(Color.White, 150 * a), 2f), -rx * .8f, -ry * .8f, rx * 1.6f, ry * 1.6f, 200, 70);
            if (lit > .02f)
            {
                g.DrawEllipse(Pn(Al(cHell, 150 * lit * a), 7f), -rx, -ry, rx * 2, ry * 2);
                g.DrawEllipse(Pn(Al(Color.FromArgb(255, 210, 190), 255 * lit * a), 1.8f), -rx, -ry, rx * 2, ry * 2);
            }
            g.Restore(st);
        }

        private void DrawBatArmorFx()
        {
            float T = p.SigT, bx = p.BodyX, by = p.BodyY;
            float cx = 200 + bx, sY = shY + by, waist = waistY + by;
            PointF headC = Pt(200 + p.HeadX + bx, headCy + p.HeadY + by);

            // 부품별 장착 시각표 {시작, 길이}
            float[] t0 = { .95f, 1.45f, 1.95f, 2.45f, 2.95f }, du = { .55f, .55f, .55f, .55f, .5f };
            Func<int, float> E = i => Ease01((T - t0[i]) / du[i]);
            Func<float, float> fly = e => (1 - e) * (1 - e);
            float on = Ease01((T - 3.4f) / .4f) * (.78f + .22f * (float)Math.Sin(T * 8f));
            float tail = Ease01((8.0f - T) / .4f);

            // 0. 전신 스캔 격자: 시작 때 푸른 홀로그램 틀이 몸을 훑는다
            if (T < 1.6f)
            {
                float sa = Ease01(T / .25f) * Ease01((1.6f - T) / .4f), sy = 90 + 470 * Ease01(T / 1.3f);
                var scan = new Color[] { Color.FromArgb(90, 190, 255) }[0];
                for (int i = 0; i < 5; i++) Stroke(new[] { Pt(cx - 96, 90 + i * 118), Pt(cx + 96, 90 + i * 118) }, Al(scan, 70 * sa), 1.6f);
                for (int i = 0; i < 3; i++) Stroke(new[] { Pt(cx - 96 + i * 96, 90), Pt(cx - 96 + i * 96, 560) }, Al(scan, 60 * sa), 1.6f);
                Stroke(new[] { Pt(cx - 110, sy), Pt(cx + 110, sy) }, Al(scan, 220 * sa), 4f);
                Glow(cx, sy, 120, 16, Al(scan, 130 * sa));
                HudText("SCAN " + (int)(100 * Ease01(T / 1.3f)) + "%", cx + 84, 96, 15, scan, sa);
            }

            // 1. 부품: 정강이 보호대 → 건틀릿 → 어깨 갑옷 → 가슴 갑옷 → 투구 장갑
            float e0 = E(0), e1 = E(1), e2 = E(2), e3 = E(3), e4 = E(4);
            foreach (int s in new[] { -1, 1 })
            {
                float xs = 200 + s * 48;
                ArmorPoly(new[] { Pt(xs - 22, 488), Pt(xs + 22, 488), Pt(xs + 16, 558), Pt(xs - 16, 558) }, s * 160 * fly(e0), 160 * fly(e0), Math.Min(1f, e0 * 3), on);
                ArmorEllipse(xs, 482, 23, 15, 0, s * 160 * fly(e0), 160 * fly(e0), Math.Min(1f, e0 * 3), on);
                if (e0 > .02f) Stroke(new[] { Pt(xs - 12, 500 + (e0 >= 1 ? 0 : 160 * fly(e0))), Pt(xs - 8, 548 + (e0 >= 1 ? 0 : 160 * fly(e0))) }, Al(Color.White, 130 * Math.Min(1f, e0 * 3)), 2f);
            }
            for (int i = 0; i < 2; i++)   // 건틀릿 (i=0: 뒤쪽 팔, 1: 앞쪽 팔)
            {
                PointF w = wrist[i], sh = i == 0 ? shL : shR;
                float dx = w.X - sh.X, dy = w.Y - sh.Y, len = Math.Max(1f, (float)Math.Sqrt(dx * dx + dy * dy)); dx /= len; dy /= len;
                float px = -dy, py = dx, ox = (i == 0 ? -1 : 1) * 180 * fly(e1), oy = -60 * fly(e1);
                PointF a0 = Pt(w.X - dx * 72, w.Y - dy * 72), a1 = Pt(w.X - dx * 4, w.Y - dy * 4);
                var gaunt = new[] { Pt(a0.X + px * 15, a0.Y + py * 15), Pt(a1.X + px * 20, a1.Y + py * 20), Pt(a1.X - px * 20, a1.Y - py * 20), Pt(a0.X - px * 15, a0.Y - py * 15) };
                ArmorPoly(gaunt, ox, oy, Math.Min(1f, e1 * 3), on);
                for (int k2 = 0; k2 < 3; k2++)   // 건틀릿 핀(박쥐 날개 모양 지느러미)
                {
                    PointF b0 = Pt(a0.X + dx * (16 + k2 * 17), a0.Y + dy * (16 + k2 * 17));
                    ArmorPoly(new[] { Pt(b0.X + px * 20, b0.Y + py * 20), Pt(b0.X + px * 36 - dx * 12, b0.Y + py * 36 - dy * 12), Pt(b0.X + px * 20 + dx * 12, b0.Y + py * 20 + dy * 12) }, ox, oy, Math.Min(1f, e1 * 3), on * .6f);
                }
                // 건틀릿 블레이드: 찰칵! 하고 펼쳐진다
                float bl = Ease01((T - 4.0f) / .14f) * (1 - Ease01((T - 6.7f) / .3f));
                if (bl > .02f)
                    for (int k2 = -1; k2 <= 1; k2++)
                    {
                        PointF b0 = Pt(w.X + px * k2 * 11, w.Y + py * k2 * 11), b1 = Pt(b0.X + dx * 86 * bl, b0.Y + dy * 86 * bl);
                        var blade = new[] { Pt(b0.X + px * 4, b0.Y + py * 4), b1, Pt(b0.X - px * 4, b0.Y - py * 4) };
                        g.FillPolygon(B(Color.FromArgb(214, 224, 240)), blade); g.DrawPolygon(Pn(Color.White, 1.6f), blade);
                        Glow(b1.X, b1.Y, 14, 14, Al(Color.White, 170 * bl));
                    }
            }
            foreach (int s in new[] { -1, 1 })   // 어깨 갑옷 (2겹)
            {
                PointF sh = s < 0 ? shL : shR; float ox = s * 150 * fly(e2), oy = -150 * fly(e2);
                ArmorEllipse(sh.X + s * 8, sh.Y - 6, 48, 27, s * 20, ox, oy, Math.Min(1f, e2 * 3), on);
                ArmorEllipse(sh.X + s * 14, sh.Y - 16, 38, 20, s * 20, ox, oy, Math.Min(1f, e2 * 3), on);
            }
            {
                float ox = 0, oy = -170 * fly(e3), a3 = Math.Min(1f, e3 * 3);
                ArmorPoly(new[] { Pt(cx - 72, sY + 4), Pt(cx - 24, sY - 6), Pt(cx, sY + 8), Pt(cx + 24, sY - 6), Pt(cx + 72, sY + 4), Pt(cx + 58, waist - 6), Pt(cx - 58, waist - 6) }, ox, oy, a3, on);
                for (int i = 1; i <= 3; i++) Stroke(new[] { Pt(cx - 50 + i * 4, sY + 62 + i * 18 + oy), Pt(cx + 50 - i * 4, sY + 62 + i * 18 + oy) }, Al(cSteelHi, 200 * a3), 2.2f);   // 복근 마디
                Stroke(new[] { Pt(cx, sY + 8 + oy), Pt(cx, waist - 8 + oy) }, Al(cSteelLo, 255 * a3), 3f);
                if (a3 > .05f)
                {
                    g.FillEllipse(B(Al(cBatYel, 255 * a3)), cx - 32, sY + 40 + oy, 64, 40); g.DrawEllipse(Pn(Al(cInk, 255 * a3), 2.4f), cx - 32, sY + 40 + oy, 64, 40);
                    DrawBat(cx, sY + 60 + oy, 1.5f, 0f, a3);
                    if (on > .02f) Glow(cx, sY + 60 + oy, 56, 40, Al(cBatYel, 110 * on * a3));
                }
            }
            {
                float oy = -190 * fly(e4), a4 = Math.Min(1f, e4 * 3);   // 투구 이마 장갑 + 광대 보호대
                ArmorPoly(new[] { Pt(headC.X - 62, headC.Y - 34), Pt(headC.X - 30, headC.Y - 52), Pt(headC.X + 30, headC.Y - 52), Pt(headC.X + 62, headC.Y - 34), Pt(headC.X + 50, headC.Y - 22), Pt(headC.X, headC.Y - 31), Pt(headC.X - 50, headC.Y - 22) }, 0, oy, a4, on);
                foreach (int s in new[] { -1, 1 })
                    ArmorPoly(new[] { Pt(headC.X + s * 62, headC.Y - 8), Pt(headC.X + s * 74, headC.Y + 10), Pt(headC.X + s * 54, headC.Y + 38), Pt(headC.X + s * 46, headC.Y + 18) }, s * 60 * fly(e4), oy, a4, on);
            }

            // 2. 장착 순간마다 찰칵! 번쩍이는 불꽃
            for (int i = 0; i < 5; i++)
            {
                float ts = t0[i] + du[i], u = (T - ts) / .3f; if (u < 0 || u > 1) continue;
                PointF c2 = i == 0 ? Pt(cx, 520) : i == 1 ? Pt(wrist[1].X, wrist[1].Y) : i == 2 ? Pt(shR.X, shR.Y - 10) : i == 3 ? Pt(cx, sY + 60) : Pt(headC.X, headC.Y - 30);
                Glint(c2.X, c2.Y, 46 * (1 - u * .4f), Color.White, 1 - u, 45);
                GroundRing(c2.X, c2.Y, 10 + 60 * u, 1f, cBatYel, 1 - u, 2.6f);
                for (int k2 = 0; k2 < 6; k2++) { double ang = k2 * Math.PI / 3 + i; Stroke(new[] { Pt(c2.X + (float)Math.Cos(ang) * 10, c2.Y + (float)Math.Sin(ang) * 10), PtC(c2.X + (float)Math.Cos(ang) * (10 + 40 * u), c2.Y + (float)Math.Sin(ang) * (10 + 40 * u)) }, Al(Color.FromArgb(255, 236, 150), 255 * (1 - u)), 2.4f); }
            }

            // 3. 시스템 온라인(3.4~): 붉은 라인이 켜지는 충격 고리 + 눈이 번쩍
            if (T >= 3.35f && T < 4.5f)
            {
                float u = (T - 3.35f) / 1.15f, fl = Math.Max(0f, 1f - u / .22f);
                Glow(cx, sY + 70, 210, 280, Al(Color.White, 150 * fl));
                for (int i = 0; i < 3; i++)
                {
                    float ur = u - i * .12f; if (ur <= 0) continue;
                    float r = 30 + 200 * (float)Math.Sqrt(ur), f2 = 1 - ur;
                    g.DrawEllipse(Pn(Al(cHell, 200 * f2), 8f - i * 2), cx - r, sY + 70 - r, r * 2, r * 2);
                    g.DrawEllipse(Pn(Al(Color.White, 240 * f2), 2.2f), cx - r, sY + 70 - r, r * 2, r * 2);
                }
                HudText("SYSTEM ONLINE", cx + 66, 118, 17, cHell, Math.Min(1f, (1 - u) * 2.2f));
            }
            float eyeA = Ease01((T - 3.3f) / .3f) * tail;
            DrawCowlEyes(eyeA);
            if (eyeA > .02f) { Glow(200 + bx - 26, headCy - 4 + by, 26, 16, Al(cHell, 130 * eyeA)); Glow(200 + bx + 26, headCy - 4 + by, 26, 16, Al(cHell, 130 * eyeA)); }
            if (on > .02f)
            {
                Glow(cx, sY + 100, 120, 200, Al(cHell, 38 * on * tail));
                for (int i = 0; i < 8; i++)   // 갑옷 틈에서 새는 붉은 열기
                {
                    float ph = (T * .9f + i * .125f) % 1f, x = cx + (Hash01(i, 1) * 2 - 1) * 90, y = sY + 160 - ph * 190;
                    Glint(x, y, 5 + 5 * Hash01(i, 2), cHell, (1 - ph) * on * tail * .8f, T * 60 + i * 30);
                }
            }

            // 4. 무장 시험: 어깨 미사일 2발 → 왼 건틀릿의 배트 수리검 3장
            PointF pod = Pt(shR.X + 4, shR.Y - 34);
            float[] rk = { 4.7f, 5.0f };
            for (int i = 0; i < 2; i++)
            {
                float u = (T - rk[i]) / .5f; if (u < 0 || u > 1.5f) continue;
                float yOff = i == 0 ? -34f : -14f;
                if (u < 1f)
                {
                    PointF q = Pt(pod.X - 40 - 520 * u * u * .5f - 300 * u, pod.Y + yOff + 20 - 60 * u + 90 * u * u);
                    var path = new List<PointF>();
                    for (int j = 0; j < 8; j++) { float uj = Math.Max(0, u - j * .04f); path.Add(PtC(pod.X - 40 - 520 * uj * uj * .5f - 300 * uj, pod.Y + yOff + 20 - 60 * uj + 90 * uj * uj)); }
                    var trail = path.ToArray();
                    Stroke(trail, Al(Color.FromArgb(255, 150, 60), 200), 12f); Stroke(trail, Al(Color.White, 240), 3.6f);
                    g.FillEllipse(B(Color.FromArgb(230, 234, 244)), CxClamp(q.X) - 9, q.Y - 4, 18, 8);
                    Glow(CxClamp(q.X) + 10, q.Y, 24, 16, Al(Color.FromArgb(255, 190, 90), 220));
                }
                else
                {
                    float v = (u - 1f) / .5f; PointF q = Pt(-8, pod.Y + yOff + 50);
                    Glow(q.X, q.Y, 100 * (1 - v * .3f), 80 * (1 - v * .3f), Al(Color.FromArgb(255, 200, 100), 230 * (1 - v)));
                    Starburst(q.X, q.Y, 60 * Math.Min(1f, v * 6), 26 * Math.Min(1f, v * 6), 9, 10f, Al(Color.FromArgb(255, 226, 120), 230 * (1 - v)), Al(cHell, 255 * (1 - v)), 3f);
                }
                if (u >= 0 && u < .25f) { Glow(pod.X, pod.Y + yOff, 40, 30, Al(Color.White, 240 * (1 - u / .25f))); Glint(pod.X, pod.Y + yOff, 34, Color.FromArgb(255, 220, 140), 1 - u / .25f, 0); }
            }
            if (T >= 5.0f && T < 5.5f) ComicText("WHOOSH!", 128, sY - 6, 32, -8f, Color.FromArgb(255, 150, 70), Math.Min(1f, (5.5f - T) * 3f));
            for (int i = 0; i < 3; i++)   // 배트 수리검
            {
                float t1 = 5.45f + i * .06f, u = (T - t1) / .6f; if (u < 0 || u > 1) continue;
                PointF h0 = wrist[0]; float vy = (i - 1) * 90f;
                PointF q = Pt(h0.X - 540 * u, h0.Y + vy * u);
                Stroke(new[] { PtC(q.X + 90, q.Y - vy * .15f), PtC(q.X, q.Y) }, Al(Color.FromArgb(150, 190, 255), 160 * (1 - u * .5f)), 8f);
                DrawBatarangShape(CxClamp(q.X), q.Y, T * 900f + i * 50, 1.05f, 1f - u * .4f);
            }
            if (T >= 5.4f && T < 5.8f) ComicText("SHING!", 128, sY + 50, 28, 8f, Color.FromArgb(190, 220, 255), Math.Min(1f, (5.8f - T) * 3.5f));
            if (T >= 4.0f && T < 4.45f) ComicText("SNIKT!", 84, wrist[1].Y - 80, 32, -10f, Color.FromArgb(224, 234, 250), Math.Min(1f, (4.45f - T) * 4f));

            // 5. 마무리: 갑옷 틈에서 뿜는 증기 + HUD 점검 목록 + 박쥐 떼
            if (T >= 6.0f)
            {
                float va = Ease01((T - 6f) / .4f) * tail;
                foreach (int s in new[] { -1, 1 })
                    for (int i = 0; i < 5; i++)
                    {
                        float ph = (T * .7f + i * .2f + (s > 0 ? .1f : 0f)) % 1f, x = (s < 0 ? shL.X : shR.X) + s * (10 + ph * 22), y = (s < 0 ? shL.Y : shR.Y) - 20 - ph * 120, r = 8 + 18 * ph;
                        g.FillEllipse(B(Al(Color.FromArgb(228, 234, 244), 150 * (1 - ph) * va)), CxClamp(x) - r, y - r * .8f, r * 2, r * 1.6f);
                    }
                string[] lines = { "ARMOR......100%", "BLADES.....READY", "MISSILES...READY", "BATARANGS..READY" };
                for (int i = 0; i < lines.Length; i++)
                {
                    float la = Ease01((T - 6.2f - i * .25f) / .25f) * tail; int chars = (int)Math.Min(lines[i].Length, Math.Max(0, (T - 6.2f - i * .25f) * 40));
                    HudText(lines[i].Substring(0, chars), 292, 150 + i * 20, 15, i == 0 ? Color.FromArgb(255, 210, 90) : Color.FromArgb(120, 230, 150), la);
                }
                for (int i = 0; i < 6; i++)
                {
                    float u = T - 6.3f - i * .15f; if (u < 0) continue;
                    DrawBat(130 + u * (50 + 60 * Hash01(i, 1)) * (i % 2 == 0 ? 1 : -1), 100 - u * (20 + 50 * Hash01(i, 2)) + (float)Math.Sin(u * 5 + i) * 12, .8f + .6f * Hash01(i, 3), (float)Math.Sin(T * 17 + i * 1.3f), tail);
                }
            }
        }
    }
}
