using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace SuperHeroes
{
    // 배트맨 탈것 3종 연출: 배트모빌(터미블러/아캄 나이트 스타일) · 배트포드 · 배트윙(헬리콥터형).
    // 조사 메모
    //  · 터미블러: 낮고 각진 장갑 차체, 후미 제트 애프터버너, 노즈의 쌍기관포, 어택 모드에서 앞바퀴가 사출되어 배트포드가 되고 본체는 자폭.
    //  · 아캄 나이트 배트모빌: 슬라이딩 캐노피, 청·적으로 번갈아 켜지는 LED 헤드라이트, 호출하면 달려옴, 후미 애프터버너.
    //  · 배트포드: 20인치 거대 타이어 두 개, 엎드려서 타고 손은 방패 뒤 손잡이, 쌍기관포.
    //  · 배트윙: 1939년 배트자이로/배트콥터는 회전날개, 1989년 배트윙은 박쥐 날개, 다크 나이트 라이즈의 더 배트는 탠덤 콕핏 VTOL.
    internal sealed partial class HeroPainter
    {
        private static readonly Color cBody = Color.FromArgb(34, 38, 48), cBodyHi = Color.FromArgb(92, 102, 128), cGlass = Color.FromArgb(70, 120, 172);

        private void DrawWheel(float cx, float cy, float r, float rotDeg, bool knobby)
        {
            g.FillEllipse(B(Color.FromArgb(20, 22, 26)), cx - r, cy - r, r * 2, r * 2);
            if (knobby)
                for (int k2 = 0; k2 < 18; k2++)
                {
                    double a = (rotDeg + k2 * 20) * Math.PI / 180;
                    Stroke(new[] { Pt(cx + (float)Math.Cos(a) * r * .86f, cy + (float)Math.Sin(a) * r * .86f), Pt(cx + (float)Math.Cos(a) * r * 1.01f, cy + (float)Math.Sin(a) * r * 1.01f) }, Color.FromArgb(62, 66, 74), 5f);
                }
            g.DrawEllipse(Pn(Color.FromArgb(56, 60, 68), 2f), cx - r, cy - r, r * 2, r * 2);
            g.FillEllipse(B(Color.FromArgb(118, 126, 142)), cx - r * .6f, cy - r * .6f, r * 1.2f, r * 1.2f);
            g.FillEllipse(B(Color.FromArgb(40, 44, 54)), cx - r * .46f, cy - r * .46f, r * .92f, r * .92f);
            for (int k2 = 0; k2 < 5; k2++)
            {
                double a = (rotDeg + k2 * 72) * Math.PI / 180;
                Stroke(new[] { Pt(cx, cy), Pt(cx + (float)Math.Cos(a) * r * .58f, cy + (float)Math.Sin(a) * r * .58f) }, Color.FromArgb(176, 184, 198), 4.4f);
            }
            g.FillEllipse(B(Color.FromArgb(205, 62, 52)), cx - r * .15f, cy - r * .15f, r * .3f, r * .3f);
        }

        private void DustPuffs(float x, float y, float age, float a, int n, float dir)
        {
            if (a <= .02f) return;
            for (int i = 0; i < n; i++)
            {
                float ph = (age * 1.3f + i / (float)n) % 1f, px = x + dir * (14 + ph * (60 + 70 * Hash01(i, 1))), py = y - 8 - ph * (24 + 40 * Hash01(i, 2)), r = 8 + 26 * ph;
                g.FillEllipse(B(Al(Color.FromArgb(214, 214, 224), 150 * (1 - ph) * a)), CxClamp(px) - r, py - r * .8f, r * 2, r * 1.6f);
            }
        }

        private void Fireball(float x, float y, float u, float sc, int seed)
        {
            float fl = Math.Max(0f, 1f - u / .3f), fa = Math.Max(0f, 1f - u / 1.4f);
            Glow(x, y - 40 * sc, 250 * sc, 190 * sc, Al(Color.FromArgb(255, 170, 70), 230 * fl + 70 * fa));
            Glow(x, y - 40 * sc, 120 * sc, 90 * sc, Al(Color.White, 255 * fl));
            Starburst(x, y - 40 * sc, 130 * sc * Math.Min(1f, u * 9f), 56 * sc * Math.Min(1f, u * 9f), 12, 12f, Al(Color.FromArgb(255, 220, 90), 235 * fa), Al(cHell, 255 * fa), 4f);
            for (int i = 0; i < 7; i++)
            {
                float ph = Math.Min(1f, u * .8f), fx = x + (i - 3) * 30 * sc, h = (60 + 90 * Hash01(i + seed, 3)) * sc * ph * (.7f + .3f * (float)Math.Sin(u * 18 + i));
                var fire = new[] { Pt(fx - 16 * sc, y), Pt(fx - 6 * sc, y - h * .6f), Pt(fx, y - h), Pt(fx + 8 * sc, y - h * .55f), Pt(fx + 16 * sc, y) };
                g.FillPolygon(B(Al(Color.FromArgb(255, 110, 40), 210 * fa)), fire);
                g.FillPolygon(B(Al(Color.FromArgb(255, 226, 120), 230 * fa)), new[] { Pt(fx - 7 * sc, y), Pt(fx, y - h * .62f), Pt(fx + 7 * sc, y) });
            }
            for (int i = 0; i < 12; i++)   // 파편
            {
                float vx = (Hash01(i + seed, 5) * 2 - 1) * 300f, vy = -(120f + Hash01(i + seed, 6) * 360f), s0 = 5 + 8 * Hash01(i + seed, 7);
                float px = x + vx * u, py = y - 30 * sc + vy * u + 620f * u * u;
                if (py > 570f || u > 1.6f || px < -40 || px > 440) continue;
                double ra = (u * 600 * (Hash01(i + seed, 8) - .5f)) * Math.PI / 180; float cs = (float)Math.Cos(ra), sn = (float)Math.Sin(ra);
                var ch = new[] { Pt(px + (-1 * cs + .6f * sn) * s0, py + (-1 * sn - .6f * cs) * s0), Pt(px + (1 * cs + .8f * sn) * s0, py + (1 * sn - .8f * cs) * s0), Pt(px + (1 * cs - .5f * sn) * s0, py + (1 * sn + .5f * cs) * s0), Pt(px + (-.5f * cs - 1 * sn) * s0, py + (-.5f * sn + 1 * cs) * s0) };
                g.FillPolygon(B(Al(Color.FromArgb(40, 44, 54), 255 * (1 - u / 1.6f))), ch);
            }
            GroundRing(x, 568, 20 + 190 * (float)Math.Sqrt(u), .17f, Color.FromArgb(255, 180, 90), Math.Max(0f, 1 - u * .9f), 4f);
        }

        // ───────── 터미블러형 배트모빌(옆모습, 앞 = 왼쪽) ─────────

        private void DrawBatmobileCar(float x, float y, float sc, float wheelDeg, float open, float lights, float flame, float pitch, float alpha, bool broken, float T)
        {
            var st = g.Save();
            g.TranslateTransform(x, y); g.RotateTransform(pitch); g.ScaleTransform(sc, sc);
            Pen edge = Pn(Al(cBodyHi, 255 * alpha), 3f);
            // 후미 제트 불꽃(차체 뒤에서 먼저 그려 노즐이 겹치게)
            if (flame > .02f)
            {
                float L = (110 + 90 * (float)Math.Sin(T * 40) * .25f + 80) * flame;
                g.FillPolygon(B(Al(Color.FromArgb(255, 130, 40), 215 * alpha)), new[] { Pt(184, -62), Pt(184 + L, -50 + (float)Math.Sin(T * 55) * 5), Pt(184, -38) });
                g.FillPolygon(B(Al(Color.FromArgb(255, 220, 120), 235 * alpha)), new[] { Pt(184, -57), Pt(184 + L * .66f, -50), Pt(184, -43) });
                g.FillPolygon(B(Al(Color.FromArgb(205, 232, 255), 250 * alpha)), new[] { Pt(184, -53), Pt(184 + L * .34f, -50), Pt(184, -47) });
            }
            g.FillEllipse(B(Al(Color.FromArgb(14, 16, 22), 255 * alpha)), 104 - 52, -42 - 52, 104, 104);   // 바퀴 아치
            g.FillEllipse(B(Al(Color.FromArgb(14, 16, 22), 255 * alpha)), -104 - 47, -38 - 47, 94, 94);
            var outline = new[] { Pt(-168, -24), Pt(-170, -56), Pt(-138, -72), Pt(-92, -80), Pt(-52, -84), Pt(-20, -128), Pt(70, -134), Pt(110, -92), Pt(150, -90), Pt(168, -60), Pt(166, -24) };
            g.FillPolygon(B(Al(cBody, 255 * alpha)), outline);
            g.FillPolygon(B(Al(Color.FromArgb(52, 58, 74), 255 * alpha)), new[] { Pt(-168, -24), Pt(-170, -56), Pt(-138, -72), Pt(166, -60), Pt(166, -24) });   // 하부 장갑
            g.DrawPolygon(edge, outline);
            Stroke(new[] { Pt(-150, -60), Pt(150, -60) }, Al(cBodyHi, 220 * alpha), 2.4f);
            for (int i = 0; i < 3; i++) g.FillPolygon(B(Al(Color.FromArgb(14, 16, 22), 255 * alpha)), new[] { Pt(-120 + i * 22, -76 + i * .6f), Pt(-104 + i * 22, -78), Pt(-100 + i * 22, -72), Pt(-116 + i * 22, -70) });   // 후드 흡기구
            g.FillPolygon(B(Al(cBody, 255 * alpha)), new[] { Pt(118, -92), Pt(178, -100), Pt(178, -91), Pt(130, -86) });   // 스포일러
            g.DrawPolygon(edge, new[] { Pt(118, -92), Pt(178, -100), Pt(178, -91), Pt(130, -86) });
            g.FillRectangle(B(Al(Color.FromArgb(24, 26, 34), 255 * alpha)), 162, -70, 28, 34);                         // 제트 노즐
            g.DrawRectangle(Pn(Al(flame > .05f ? Color.FromArgb(255, 150, 60) : cBodyHi, 255 * alpha), 2.6f), 162, -70, 28, 34);
            g.FillRectangle(B(Al(Color.FromArgb(56, 60, 70), 255 * alpha)), -208, -50, 42, 7); g.FillRectangle(B(Al(Color.FromArgb(56, 60, 70), 255 * alpha)), -202, -38, 36, 7);   // 노즈 쌍기관포
            g.DrawRectangle(Pn(Al(cBodyHi, 255 * alpha), 1.6f), -208, -50, 42, 7); g.DrawRectangle(Pn(Al(cBodyHi, 255 * alpha), 1.6f), -202, -38, 36, 7);
            DrawBat(18, -50, 1.25f, 0f, alpha * .95f);

            // 조종석: 열린 캐노피 안쪽(어둠 + 계기판 불빛 + 시트)
            var cab = new[] { Pt(-46, -86), Pt(-20, -122), Pt(66, -126), Pt(96, -92) };
            if (open > .02f)
            {
                g.FillPolygon(B(Al(Color.FromArgb(14, 16, 22), 255 * alpha)), cab);
                g.FillRectangle(B(Al(Color.FromArgb(70, 76, 92), 255 * alpha)), 22, -118, 28, 36);
                g.FillRectangle(B(Al(Color.FromArgb(120, 230, 255), 230 * open * alpha)), -22, -104, 26, 12);
                g.FillRectangle(B(Al(Color.FromArgb(255, 150, 70), 230 * open * alpha)), -14, -90, 14, 6);
            }
            var glass = new PointF[cab.Length]; for (int i = 0; i < cab.Length; i++) glass[i] = Pt(cab[i].X + open * 84, cab[i].Y - open * 16);
            g.FillPolygon(B(Al(cGlass, 205 * alpha)), glass); g.DrawPolygon(edge, glass);
            Stroke(new[] { glass[1], Pt((glass[1].X + glass[2].X) / 2f, glass[1].Y + 18) }, Al(Color.White, 170 * alpha), 3f);
            if (broken) { Stroke(new[] { glass[1], Pt(glass[1].X + 30, glass[1].Y + 22), Pt(glass[1].X + 22, glass[1].Y + 40) }, Al(cInk, 230 * alpha), 2f); Stroke(new[] { Pt(glass[1].X + 30, glass[1].Y + 22), Pt(glass[2].X - 10, glass[2].Y + 10) }, Al(cInk, 230 * alpha), 2f); }

            // 헤드라이트(청·적 LED 번갈아 + 백색) / 루프 LED
            Color hl = lights > .05f && ((int)(T * 8)) % 2 == 0 ? Color.FromArgb(255, 70, 70) : lights > .05f ? Color.FromArgb(90, 160, 255) : Color.FromArgb(70, 76, 90);
            g.FillPolygon(B(Al(lights > .05f ? Color.White : hl, 255 * alpha)), new[] { Pt(-168, -66), Pt(-146, -76), Pt(-146, -70), Pt(-168, -60) });
            if (lights > .05f) { g.FillRectangle(B(Al(hl, 255 * alpha)), -10, -134, 22, 6); g.FillRectangle(B(Al(hl == Color.FromArgb(255, 70, 70) ? Color.FromArgb(90, 160, 255) : Color.FromArgb(255, 70, 70), 255 * alpha)), 22, -136, 22, 6); }
            DrawWheel(104, -42, 42, wheelDeg, true); DrawWheel(-104, -38, 38, wheelDeg * 1.1f, true);
            g.Restore(st);

            // 월드 좌표 발광
            Glow(x - 150 * sc, y - 66 * sc, 36 * sc, 22 * sc, Al(lights > .05f ? Color.FromArgb(220, 235, 255) : Color.FromArgb(120, 130, 150), (lights > .05f ? 210 : 50) * alpha));
            if (flame > .02f) Glow(x + 190 * sc, y - 50 * sc, (60 + 40 * flame) * sc, 36 * sc, Al(Color.FromArgb(255, 170, 80), 200 * alpha * Math.Min(1f, flame)));
            if (open > .1f) Glow(x + 20 * sc, y - 108 * sc, 60 * sc, 40 * sc, Al(Color.FromArgb(120, 220, 255), 90 * open * alpha));
        }

        private static float BatmobileX(float T)
        {
            const float park = 125f;
            if (T < .9f) return 640f;
            if (T < 2.0f) { float u = (T - .9f) / 1.1f; return 640f + (park - 640f) * (1 - (float)Math.Pow(1 - u, 3)); }
            if (T < 4.5f) return park;
            if (T < 5.7f) { float u = (T - 4.5f) / 1.2f; return park - 1000f * (float)Math.Pow(u, 2.2); }
            return -600f;
        }

        private void DrawBatmobileFx()
        {
            float T = p.SigT, bx = p.BodyX, by = p.BodyY;
            const float gy = 568f;
            float cx = BatmobileX(T);
            float arrive = T >= .9f && T < 2.0f ? 1 - (T - .9f) / 1.1f : 0f;
            float rev = T >= 3.7f && T < 4.5f ? 1f : 0f, launch = T >= 4.5f && T < 5.7f ? (T - 4.5f) / 1.2f : 0f;
            float flame = arrive > 0 ? .55f + .7f * arrive : rev > 0 ? .8f + .35f * (float)Math.Sin(T * 34) : launch > 0 ? 1.5f + launch : 0f;
            float open = Ease01((T - 2.0f) / .5f) * (1 - Ease01((T - 3.3f) / .5f));
            float lights = T >= 3.4f && T < 5.5f ? 1f : 0f;
            float shake = rev > 0 ? (float)Math.Sin(T * 70) * 1.6f : 0f;
            float pitch = (T >= 1.7f && T < 2.15f ? -4f * (float)Math.Sin((T - 1.7f) / .45f * Math.PI) : 0f) + (launch > 0 ? 5f * Math.Min(1f, launch * 4f) : 0f);
            float wheelDeg = cx / 40f * 57.3f;

            // 1. 호출: 건틀릿에서 퍼지는 신호 파동
            if (T < 1.1f)
            {
                PointF w = wrist[1]; float a = Ease01(T / .2f) * Ease01((1.1f - T) / .3f);
                Glow(w.X, w.Y, 36, 30, Al(Color.FromArgb(120, 220, 255), 200 * a));
                for (int i = 0; i < 3; i++) { float ph = (T * 2.2f + i / 3f) % 1f, r = 16 + 70 * ph; g.DrawEllipse(Pn(Al(Color.FromArgb(120, 220, 255), 200 * (1 - ph) * a), 3f), w.X - r, w.Y - r, r * 2, r * 2); }
                HudText("BATMOBILE", w.X + 14, w.Y - 54, 15, Color.FromArgb(120, 230, 255), a); HudText("CALLING...", w.X + 14, w.Y - 38, 13, Color.FromArgb(255, 210, 90), a);
            }

            // 2. 타이어 자국 + 진동하는 바닥
            if (T >= 1.0f)
            {
                float ma = Ease01((6.8f - T) / .8f) * .5f;
                if (cx < 440f && T < 5.7f) { foreach (float oy in new[] { -2f, 6f }) Stroke(new[] { PtC(cx + 100, gy + oy), PtC(440, gy + oy) }, Al(Color.FromArgb(20, 22, 28), 255 * ma), 4f); }
            }
            if (T >= .8f && T < 2.0f) for (int i = 0; i < 12; i++) { float y = 380 + 190 * Hash01(i, 3), len = 90 + 90 * Hash01(i, 4), ph = (T * 3.6f + i * .083f) % 1f, x = 460 - ph * 560; Stroke(new[] { PtC(x, y), PtC(x + len, y) }, Al(Color.White, 120 * (1 - Math.Abs(T - 1.4f)) * Math.Max(0f, 1 - Math.Abs(T - 1.4f) * 1.2f)), 2.2f); }

            // 3. 차: 도착 · 대기 · 탑승 · 시동 · 질주
            if (cx > -340f && cx < 660f)
            {
                // 질주 중 후미 화염 꼬리
                if (launch > 0f)
                {
                    var trail = new List<PointF>();
                    for (int j = 0; j < 22; j++) { float tj = T - j * .02f; if (tj < 4.5f) break; trail.Add(PtC(BatmobileX(tj) + 195, gy - 50)); }
                    if (trail.Count > 1) { var tr = trail.ToArray(); Stroke(tr, Al(Color.FromArgb(255, 120, 40), 150), 34f); Stroke(tr, Al(Color.FromArgb(255, 220, 130), 230), 14f); Stroke(tr, Al(Color.White, 255), 4f); }
                }
                DrawBatmobileCar(cx + shake * .2f, gy + shake, 1f, wheelDeg, open, lights, flame, pitch, 1f, false, T);
                if (lights > 0f) GradCone(Pt(cx - 170, gy - 66), Pt(cx - 170 - 340, gy - 66 - 80), Pt(cx - 170 - 340, gy - 66 + 80), Color.FromArgb(235, 240, 255), 130, 0);
                // 급제동: 타이어 연기와 불꽃
                if (T >= 1.35f && T < 2.5f) { DustPuffs(cx - 104, gy, T - 1.35f, 1f, 7, 1f); DustPuffs(cx + 104, gy, T - 1.35f, 1f, 7, 1f); }
                if (T >= 1.5f && T < 2.05f) for (int i = 0; i < 9; i++) { float ph = (T * 5f + i * .11f) % 1f; Stroke(new[] { Pt(cx - 80 + ph * 70, gy - 14), PtC(cx - 80 + ph * 70 + 24, gy - 14 + 8 + ph * 12) }, Al(Color.FromArgb(255, 220, 110), 255 * (1 - ph)), 2.6f); }
                if (rev > 0f) { DustPuffs(cx + 104, gy, T - 3.7f, 1f, 9, 1f); DustPuffs(cx - 104, gy, T - 3.7f, .6f, 6, -1f); ComicText("VROOOM!", cx + 40, gy - 190, 36, -6f, Color.FromArgb(255, 170, 60), Math.Min(1f, (T - 3.7f) * 4f) * Math.Min(1f, (4.5f - T) * 5f)); }
                if (launch > 0f)
                {
                    for (int i = 0; i < 14; i++) { float y = 300 + 270 * Hash01(i, 6), len = 120 + 140 * Hash01(i, 7), ph = (T * 4.2f + i * .071f) % 1f, x = 460 - ph * 640; Stroke(new[] { PtC(x, y), PtC(x + len, y) }, Al(Color.White, 190 * (1 - launch * .5f)), 2.6f); }
                    DustPuffs(cx + 140, gy, T - 4.5f, 1f, 10, 1f);
                }
            }
            if (T >= 4.55f && T < 5.5f) { float u = (T - 4.55f) / .95f; GroundRing(130, gy, 20 + 210 * (float)Math.Sqrt(u), .15f, Color.FromArgb(255, 190, 100), 1 - u, 4f); ComicText("ZOOOM!", 300, gy - 210, 38, 8f, Color.FromArgb(255, 220, 80), Math.Min(1f, (5.5f - T) * 2f)); }

            // 4. 캐노피가 닫히는 순간의 번쩍임, 떠난 자리의 잔불
            if (T >= 3.25f && T < 3.7f) { float u = (T - 3.25f) / .45f; Glint(cx + 20, gy - 118, 50 * (1 - u * .4f), Color.White, 1 - u, 45); GroundRing(cx + 20, gy - 118, 10 + 60 * u, 1f, Color.FromArgb(160, 230, 255), 1 - u, 2.6f); }
            if (T >= 5.5f && T < 7.2f)
            {
                float fa = Ease01((7.0f - T) / .9f);
                for (int i = 0; i < 7; i++) { float fx = 110 + i * 46, h = (14 + 22 * Hash01(i, 8)) * (.6f + .4f * (float)Math.Sin(T * 17 + i * 2f)) * fa; g.FillPolygon(B(Al(Color.FromArgb(255, 130, 40), 220 * fa)), new[] { Pt(fx - 8, gy), Pt(fx, gy - h), Pt(fx + 8, gy) }); }
            }

            // 5. 출격에서 돌아온 배트맨: 박쥐 연기와 함께 재등장
            if (T >= 5.85f && T < 6.9f)
            {
                float u = (T - 5.85f) / 1.05f, fl = Math.Max(0f, 1f - u / .25f);
                Glow(200, 400, 130, 190, Al(Color.White, 190 * fl));
                for (int i = 0; i < 16; i++)
                {
                    float hx = 200 + (Hash01(i, 1) * 2 - 1) * 110, hy = 540 - Hash01(i, 2) * 380, r = (26 + 30 * Hash01(i, 3)) * (1 - u * .4f);
                    g.FillEllipse(B(Al(Color.FromArgb(52, 56, 68), 210 * (1 - u))), CxClamp(hx) - r, hy - u * 60 - r * .85f, r * 2, r * 1.7f);
                }
                for (int i = 0; i < 4; i++) DrawBat(200 + (i - 1.5f) * 50 * (1 + u * 2), 360 - u * 200, 1f, (float)Math.Sin(T * 17 + i), 1 - u);
            }
        }

        // ───────── 배트포드 ─────────

        private void DrawBatpod(float x, float y, float sc, float wheelDeg, float pitch, bool flip, bool rider, float fire, float flame, float T)
        {
            var st = g.Save();
            g.TranslateTransform(x, y); g.RotateTransform(flip ? -pitch : pitch); g.ScaleTransform(flip ? -sc : sc, sc);
            float rd = flip ? -wheelDeg : wheelDeg;
            if (flame > .02f)
            {
                float L = 70 * flame;
                g.FillPolygon(B(Color.FromArgb(220, 255, 140, 50)), new[] { Pt(104, -52), Pt(104 + L, -46 + (float)Math.Sin(T * 60) * 4), Pt(104, -40) });
                g.FillPolygon(B(Color.FromArgb(235, 205, 232, 255)), new[] { Pt(104, -49), Pt(104 + L * .5f, -46), Pt(104, -43) });
            }
            // 라이더(엎드린 배트맨): 망토 → 다리 → 몸통 → 팔 → 두건
            if (rider)
            {
                float wave = (float)Math.Sin(T * 22) * 7;
                g.FillPolygon(B(Color.FromArgb(22, 24, 30)), new[] { Pt(-8, -86), Pt(40, -102), Pt(112, -96 + wave * .4f), Pt(180, -80 + wave), Pt(150, -64 + wave * .5f), Pt(100, -72), Pt(50, -66) });
                Stroke(new[] { Pt(62, -78), Pt(100, -62) }, Color.FromArgb(52, 56, 66), 16f); g.FillEllipse(B(Color.FromArgb(24, 26, 32)), 94, -70, 22, 14);
                g.FillEllipse(B(Color.FromArgb(70, 76, 90)), -34, -98, 110, 34); g.DrawEllipse(Pn(cInk, 2.4f), -34, -98, 110, 34);
                g.FillRectangle(B(cBatYel), 36, -84, 18, 8);
                Stroke(new[] { Pt(-20, -82), Pt(-58, -62) }, Color.FromArgb(70, 76, 90), 11f); g.FillEllipse(B(Color.FromArgb(30, 32, 38)), -66, -69, 14, 14);
                g.FillPolygon(B(Color.FromArgb(20, 22, 28)), new[] { Pt(-50, -106), Pt(-44, -128), Pt(-36, -108) }); g.FillPolygon(B(Color.FromArgb(20, 22, 28)), new[] { Pt(-40, -108), Pt(-30, -128), Pt(-24, -104) });
                g.FillEllipse(B(Color.FromArgb(20, 22, 28)), -62, -112, 36, 34); g.FillEllipse(B(Color.FromArgb(255, 224, 196)), -64, -96, 14, 12);
                g.FillPolygon(B(Color.White), new[] { Pt(-60, -100), Pt(-44, -96), Pt(-44, -92), Pt(-60, -96) });
            }
            // 차체·앞 포크·방패·쌍기관포
            var cowl = new[] { Pt(-64, -58), Pt(-40, -72), Pt(30, -74), Pt(86, -58), Pt(82, -34), Pt(-50, -34) };
            g.FillPolygon(B(Color.FromArgb(36, 40, 50)), cowl); g.DrawPolygon(Pn(cBodyHi, 2.6f), cowl);
            Stroke(new[] { Pt(-40, -56), Pt(70, -52) }, Color.FromArgb(205, 62, 52), 3f);
            Stroke(new[] { Pt(-78, -44), Pt(-52, -70) }, Color.FromArgb(120, 128, 142), 7f);
            g.FillPolygon(B(Color.FromArgb(54, 60, 76)), new[] { Pt(-76, -78), Pt(-40, -84), Pt(-36, -62), Pt(-72, -60) }); g.DrawPolygon(Pn(cBodyHi, 2f), new[] { Pt(-76, -78), Pt(-40, -84), Pt(-36, -62), Pt(-72, -60) });
            g.FillRectangle(B(Color.FromArgb(60, 64, 72)), -126, -62, 50, 7); g.DrawRectangle(Pn(cBodyHi, 1.6f), -126, -62, 50, 7);
            g.FillRectangle(B(Color.FromArgb(60, 64, 72)), -118, -52, 42, 6);
            if (fire > .02f)
            {
                g.FillPolygon(B(Color.FromArgb(255, 236, 140)), StarPts(-130, -59, 28 * fire, T * 400f));
                for (int i = 0; i < 3; i++) { float ph = (T * 18 + i * .33f) % 1f; Stroke(new[] { Pt(-134 - ph * 120, -59), Pt(-152 - ph * 120, -59) }, Color.FromArgb(255, 238, 160), 3f); }
            }
            DrawWheel(78, -44, 44, rd, true); DrawWheel(-78, -44, 44, rd, true);
            g.Restore(st);
            if (flame > .02f) Glow(x + (flip ? -104 : 104) * sc, y - 46 * sc, 40 * sc, 24 * sc, Al(Color.FromArgb(255, 170, 80), 190 * Math.Min(1f, flame)));
            if (fire > .02f) Glow(x + (flip ? 128 : -128) * sc, y - 59 * sc, 40 * sc, 30 * sc, Al(Color.FromArgb(255, 230, 150), 220 * fire));
        }

        private static float TumblerX(float T) { return T < .9f ? 640f : T < 1.7f ? 640f + (370f - 640f) * (1 - (float)Math.Pow(1 - (T - .9f) / .8f, 3)) : 370f; }

        private static float PodX(float T, out bool flip)
        {
            flip = false;
            if (T < 1.7f) return 250f;
            if (T < 2.7f) return 250f + (120f - 250f) * (1 - (float)Math.Pow(1 - (T - 1.7f) / 1.0f, 3));
            if (T < 4.3f) return 120f;
            if (T < 5.3f) return 120f - 800f * (float)Math.Pow((T - 4.3f) / 1.0f, 2.1);
            if (T < 6.0f) return -700f;
            if (T < 6.9f) { flip = true; return -400f + 520f * (1 - (float)Math.Pow(1 - (T - 6.0f) / .9f, 3)); }
            if (T < 7.5f) { flip = true; return 120f + 700f * (float)Math.Pow((T - 6.9f) / .6f, 2); }
            return 900f;
        }

        private void DrawBatpodFx()
        {
            float T = p.SigT, bx = p.BodyX, by = p.BodyY;
            const float gy = 568f;
            bool flip; float px = PodX(T, out flip);
            float tx = TumblerX(T);
            float revv = T >= 3.5f && T < 4.3f ? 1f : 0f, ride = T >= 4.3f && T < 5.3f ? 1f : 0f;
            float fire = T >= 3.6f && T < 5.0f && ((int)(T * 14)) % 2 == 0 ? 1f : 0f;
            float flame = revv > 0 ? .7f + .3f * (float)Math.Sin(T * 40) : ride > 0 ? 1.4f : (T >= 6.0f && T < 6.9f ? 1f : 0f);
            float pitch = (revv > 0 ? -14f * Math.Min(1f, (T - 3.5f) / .2f) : 0f) + (ride > 0 ? -8f : 0f);
            bool riderOn = T >= 3.2f && T < 6.95f;
            float wheelDeg = px / 44f * 57.3f;

            // 0. 경보: 건틀릿 + 붉은 점멸
            if (T < 1.7f)
            {
                PointF w = wrist[1]; float a = Ease01(T / .2f);
                if (T < 1.0f) { Glow(w.X, w.Y, 30, 26, Al(Color.FromArgb(255, 90, 80), 200 * a)); HudText("DAMAGE", w.X + 14, w.Y - 40, 15, cHell, a * (.5f + .5f * (((int)(T * 8)) % 2))); }
                if (T > .9f) Glow(430, 380, 180, 260, Al(cHell, 30 * (1 + (float)Math.Sin(T * 14))));
            }

            // 1. 배트포드(터미블러 앞바퀴 사출) — 터미블러 뒤에서 먼저 그린다
            if (T >= 1.7f && px > -400 && px < 700) DrawBatpod(px, gy, 1.0f, wheelDeg, pitch, flip, riderOn, fire, flame, T);
            if (T >= 1.7f && T < 2.7f) { float u = (T - 1.7f) / 1.0f; Glow(250, gy - 40, 100 * (1 - u), 60 * (1 - u), Al(Color.White, 220 * (1 - u))); if (T < 2.1f) ComicText("EJECT!", 322, 392, 42, -8f, Color.FromArgb(255, 230, 70), Math.Min(1f, (2.1f - T) * 4f)); }
            if (T >= 2.3f && T < 3.1f) { float u = (T - 2.3f) / .8f; DustPuffs(px - 40, gy, T - 2.3f, 1 - u, 7, 1f); DustPuffs(px + 40, gy, T - 2.3f, 1 - u, 7, 1f); }

            // 2. 손상된 터미블러: 연기를 내며 들어와 정차 → 자폭
            if (T >= .9f && T < 2.4f)
            {
                DrawBatmobileCar(tx, gy, .9f, tx / 40f * 57.3f, 0f, 0f, T < 1.7f ? .6f : 0f, 0f, 1f, true, T);
                for (int i = 0; i < 8; i++) { float ph = (T * .9f + i * .125f) % 1f, sx = tx + 30 + ph * 60, sy = gy - 140 - ph * 160, r = 12 + 30 * ph; g.FillEllipse(B(Al(Color.FromArgb(40, 42, 50), 190 * (1 - ph))), CxClamp(sx) - r, sy - r * .8f, r * 2, r * 1.6f); }
                for (int i = 0; i < 4; i++) { float ph = (T * 6f + i * .25f) % 1f; Stroke(new[] { Pt(tx - 30 + i * 30, gy - 130), PtC(tx - 30 + i * 30 + 20, gy - 130 + 30 + ph * 40) }, Al(Color.FromArgb(255, 220, 110), 255 * (1 - ph)), 2.6f); }
            }
            if (T >= 2.3f && T < 4.2f) { Fireball(370, gy, T - 2.3f, 1.1f, 7); if (T < 2.9f) ComicText("BOOM!", 330, 360, 46, 7f, Color.FromArgb(255, 200, 60), Math.Min(1f, (2.9f - T) * 3f)); }
            if (T >= 4.2f && T < 6.2f) { float fa = Ease01((6.0f - T) / 1.5f); for (int i = 0; i < 6; i++) { float fx = 320 + i * 22, h = (18 + 26 * Hash01(i, 8)) * (.6f + .4f * (float)Math.Sin(T * 17 + i * 2f)) * fa; g.FillPolygon(B(Al(Color.FromArgb(255, 120, 40), 220 * fa)), new[] { Pt(fx - 9, gy), Pt(fx, gy - h), Pt(fx + 9, gy) }); } }

            // 3. 탑승·질주·복귀의 효과
            if (T >= 3.0f && T < 3.5f) { float u = (T - 3.0f) / .5f; Glint(px, gy - 70, 56 * (1 - u * .4f), Color.White, 1 - u, 45); GroundRing(px, gy - 70, 10 + 70 * u, 1f, Color.FromArgb(255, 220, 120), 1 - u, 2.6f); }
            if (revv > 0f) { DustPuffs(px + 78, gy, T - 3.5f, 1f, 8, 1f); ComicText("VRRRM!", px + 60, gy - 190, 34, -6f, Color.FromArgb(255, 170, 60), Math.Min(1f, (T - 3.5f) * 4f) * Math.Min(1f, (4.3f - T) * 5f)); }
            if (T >= 3.7f && T < 5.2f) for (int i = 0; i < 4; i++) { float ph = (T * 3f + i * .25f) % 1f; Stroke(new[] { PtC(px - 150 - ph * 360, gy - 59 + (i - 1.5f) * 6), PtC(px - 150 - ph * 360 - 26, gy - 59 + (i - 1.5f) * 6) }, Al(Color.FromArgb(255, 240, 170), 255 * (1 - ph)), 3f); }
            if (ride > 0f)
            {
                for (int i = 0; i < 14; i++) { float y = 300 + 270 * Hash01(i, 6), len = 120 + 140 * Hash01(i, 7), ph = (T * 4.2f + i * .071f) % 1f, x = 460 - ph * 640; Stroke(new[] { PtC(x, y), PtC(x + len, y) }, Al(Color.White, 190), 2.6f); }
                DustPuffs(px + 120, gy, T - 4.3f, 1f, 10, 1f);
                if (T < 5.0f) ComicText("ZOOOM!", 300, gy - 200, 36, 8f, Color.FromArgb(255, 220, 80), Math.Min(1f, (5.0f - T) * 3f));
            }
            if (T >= 6.0f && T < 6.9f) { DustPuffs(px + (flip ? -90 : 90), gy, T - 6.0f, 1f, 8, flip ? -1f : 1f); if (T > 6.5f) { float u = (T - 6.5f) / .4f; GroundRing(px, gy, 10 + 130 * u, .17f, Color.FromArgb(220, 230, 245), 1 - u, 3f); } }
            if (T >= 6.9f && T < 7.6f) { Glint(120, gy - 100, 50 * (1 - (T - 6.9f) / .7f), Color.White, 1 - (T - 6.9f) / .7f, 45); }
        }

        // ───────── 배트윙(헬리콥터형) ─────────

        private void DrawRotor(float cx, float cy, float rx, float ry, float phase, float blur, float alpha)
        {
            Glow(cx, cy, rx * 1.05f, ry * 1.6f, Al(Color.FromArgb(200, 210, 230), 55 * blur * alpha));
            for (int b = 0; b < 4; b++)
            {
                double a = phase + b * Math.PI / 2;
                PointF e = Pt(cx + (float)Math.Cos(a) * rx, cy + (float)Math.Sin(a) * ry);
                float w = blur > .5f ? 4f : 7f;
                g.DrawLine(Pn(Al(Color.FromArgb(22, 24, 32), (blur > .5f ? 150 : 255) * alpha), w), cx, cy, e.X, e.Y);
            }
            g.FillEllipse(B(Al(Color.FromArgb(70, 76, 92), 255 * alpha)), cx - 9, cy - 5, 18, 10);
        }

        private void DrawBatwingCraft(float x, float y, float sc, float tilt, float rotorPhase, float rotorBlur, float hatch, float lightOn, float T, float alpha)
        {
            var st = g.Save();
            g.TranslateTransform(x, y); g.RotateTransform(tilt); g.ScaleTransform(sc, sc);
            Pen edge = Pn(Al(cBodyHi, 255 * alpha), 2.6f);
            // 스키드와 지지대
            Stroke(new[] { Pt(-96, -6), Pt(86, -6) }, Al(Color.FromArgb(120, 128, 142), 255 * alpha), 6f);
            Stroke(new[] { Pt(-62, -32), Pt(-66, -6) }, Al(Color.FromArgb(120, 128, 142), 255 * alpha), 5f); Stroke(new[] { Pt(52, -32), Pt(58, -6) }, Al(Color.FromArgb(120, 128, 142), 255 * alpha), 5f);
            // 꼬리 붐 + 박쥐 귀 꼬리날개
            var boom = new[] { Pt(110, -80), Pt(256, -94), Pt(256, -82), Pt(110, -58) };
            g.FillPolygon(B(Al(cBody, 255 * alpha)), boom); g.DrawPolygon(edge, boom);
            var fin = new[] { Pt(212, -88), Pt(250, -146), Pt(242, -88) }; g.FillPolygon(B(Al(cBody, 255 * alpha)), fin); g.DrawPolygon(edge, fin);
            var fin2 = new[] { Pt(196, -84), Pt(222, -126), Pt(222, -86) }; g.FillPolygon(B(Al(Color.FromArgb(48, 54, 68), 255 * alpha)), fin2); g.DrawPolygon(edge, fin2);
            // 박쥐 날개 스텁 윙 + 미사일 포드
            var wing = new[] { Pt(-14, -56), Pt(-70, -52), Pt(-122, -30), Pt(-86, -38), Pt(-96, -24), Pt(-50, -34), Pt(10, -38) };
            g.FillPolygon(B(Al(Color.FromArgb(44, 50, 64), 255 * alpha)), wing); g.DrawPolygon(edge, wing);
            g.FillEllipse(B(Al(Color.FromArgb(70, 76, 92), 255 * alpha)), -86, -36, 54, 14); g.FillPolygon(B(Al(Color.FromArgb(230, 232, 240), 255 * alpha)), new[] { Pt(-86, -32), Pt(-100, -29), Pt(-86, -26) });
            // 동체
            var hull = new[] { Pt(-130, -52), Pt(-112, -78), Pt(-60, -94), Pt(20, -100), Pt(100, -88), Pt(128, -62), Pt(100, -36), Pt(-20, -30), Pt(-100, -34) };
            g.FillPolygon(B(Al(cBody, 255 * alpha)), hull);
            g.FillPolygon(B(Al(Color.FromArgb(50, 56, 72), 255 * alpha)), new[] { Pt(-130, -52), Pt(-100, -34), Pt(-20, -30), Pt(100, -36), Pt(128, -62), Pt(40, -52), Pt(-60, -50) });
            g.DrawPolygon(edge, hull);
            // 하단 해치(윈치 케이블이 내려오는 구멍)
            if (hatch > .02f) { g.FillRectangle(B(Al(Color.FromArgb(255, 210, 120), 230 * hatch * alpha)), -26, -34, 56, 8 + 8 * hatch); }
            g.DrawRectangle(Pn(Al(cBodyHi, 255 * alpha), 2f), -26, -34, 56, 12);
            // 탠덤 콕핏 캐노피
            var can = new[] { Pt(-116, -62), Pt(-78, -88), Pt(-8, -96), Pt(12, -66) };
            g.FillPolygon(B(Al(cGlass, 205 * alpha)), can); g.DrawPolygon(edge, can);
            Stroke(new[] { Pt(-100, -72), Pt(-70, -88) }, Al(Color.White, 170 * alpha), 3f);
            g.FillEllipse(B(Al(Color.FromArgb(26, 28, 36), 255 * alpha)), -86, -86, 24, 24); g.FillEllipse(B(Al(Color.FromArgb(26, 28, 36), 255 * alpha)), -44, -90, 26, 26);
            DrawBat(-30, -50, 1.5f, 0f, alpha * .9f);
            // 마스트 + 주 회전날개 + 꼬리 회전날개
            Stroke(new[] { Pt(10, -98), Pt(10, -120) }, Al(Color.FromArgb(70, 76, 92), 255 * alpha), 8f);
            DrawRotor(10, -122, 176, 17, rotorPhase, rotorBlur, alpha);
            float tp = rotorPhase * 1.6f; g.FillEllipse(B(Al(Color.FromArgb(200, 208, 222), 80 * rotorBlur * alpha)), 232, -122, 28, 28);
            Stroke(new[] { Pt(246 + (float)Math.Cos(tp) * 15, -108 + (float)Math.Sin(tp) * 15), Pt(246 - (float)Math.Cos(tp) * 15, -108 - (float)Math.Sin(tp) * 15) }, Al(Color.FromArgb(22, 24, 32), 255 * alpha), 3f);
            g.Restore(st);
            // 항법등·탐조등
            bool blink = ((int)(T * 3)) % 2 == 0;
            Glow(x + 246 * sc, y - 148 * sc, 14 * sc, 14 * sc, Al(Color.FromArgb(255, 70, 70), (blink ? 230 : 60) * alpha));
            if (lightOn > .02f) Glow(x - 124 * sc, y - 46 * sc, 36 * sc, 26 * sc, Al(Color.White, 220 * lightOn * alpha));
        }

        private static void WingPose(float T, out float x, out float y, out float sc, out float tilt)
        {
            x = 700; y = -400; sc = .95f; tilt = 0;
            if (T < 1.0f) return;
            if (T < 2.5f) { float u = (T - 1.0f) / 1.5f, e = 1 - (float)Math.Pow(1 - u, 2.4); x = 440 + (190 - 440) * e; y = -300 + (62 + 300) * e; tilt = 9f * (1 - e); return; }
            if (T < 4.2f) { x = 190 + (float)Math.Sin(T * 2.3f) * 4; y = 62 + (float)Math.Sin(T * 3f) * 3; return; }
            if (T < 6.0f) { float u = (T - 4.2f) / 1.8f; x = 190 - 380 * (float)Math.Pow(u, 1.7); y = 62 - 460 * (float)Math.Pow(u, 1.4); sc = .95f - .45f * u; tilt = -14f * Math.Min(1f, u * 3f); return; }
        }

        private void DrawBatwingFx()
        {
            float T = p.SigT, bx = p.BodyX, by = p.BodyY;
            const float gy = 568f;
            float wx, wy, wsc, wtilt; WingPose(T, out wx, out wy, out wsc, out wtilt);
            bool air = T >= 1.0f && T < 6.0f && wx > -300;
            float rotBlur = Ease01((T - 1.0f) / .9f), phase = T * 28f;

            // 1. 배트 시그널: 하늘에 노랗게 박쥐 문양이 켜지고 빛줄기가 내려온다
            if (T < 2.0f)
            {
                float a = Ease01((T - .1f) / .4f) * Ease01((2.0f - T) / .5f);
                PointF sg = Pt(330, -40);
                GradCone(sg, Pt(260, 330), Pt(150, 330), Color.FromArgb(255, 240, 150), 100 * a, 0);
                Glow(sg.X, sg.Y, 90, 56, Al(Color.FromArgb(255, 236, 130), 200 * a));
                g.FillEllipse(B(Al(Color.FromArgb(255, 228, 90), 255 * a)), sg.X - 62, sg.Y - 36, 124, 72);
                DrawBat(sg.X, sg.Y + 4, 2.2f, 0f, a);
                PointF w = wrist[1]; if (T < .9f) { Glow(w.X, w.Y, 28, 24, Al(Color.FromArgb(255, 226, 100), 200 * a)); HudText("BAT-SIGNAL", w.X + 14, w.Y - 36, 14, Color.FromArgb(255, 226, 100), a); }
            }

            // 2. 하강: 아래로 퍼지는 회전날개 바람(지면 충격 고리·먼지·날아다니는 잎)
            if (T >= 1.6f && T < 5.0f)
            {
                float wa = Ease01((T - 1.6f) / .9f) * (T < 4.2f ? 1f : Ease01((5.0f - T) / .8f));
                for (int i = 0; i < 3; i++) { float ph = (T * 1.7f + i / 3f) % 1f; GroundRing(200, gy, 30 + 170 * ph, .16f, Color.FromArgb(214, 224, 240), (1 - ph) * wa, 3f); }
                for (int i = 0; i < 16; i++)
                {
                    float ph = (T * 1.4f + i * .0625f) % 1f, sd = i % 2 == 0 ? 1f : -1f, x = 200 + sd * (30 + ph * 170), y = gy - 8 - 30 * ph * Hash01(i, 3) - (float)Math.Sin(ph * Math.PI) * 24, s = 3 + 3 * Hash01(i, 4);
                    g.FillEllipse(B(Al(i % 3 == 0 ? Color.FromArgb(200, 230, 160) : Color.FromArgb(224, 214, 190), 200 * (1 - ph) * wa)), CxClamp(x) - s * 1.6f, y - s * .6f, s * 3.2f, s * 1.2f);
                }
                for (int i = 0; i < 12; i++) { float x = 40 + 330 * Hash01(i, 5), ph = (T * 2.6f + i * .083f) % 1f; Stroke(new[] { Pt(x, 120 + ph * 420), Pt(x, 150 + ph * 420) }, Al(Color.White, 130 * wa * (1 - ph)), 2f); }
            }

            // 3. 탐조등: 배트윙 콧등에서 땅의 배트맨에게
            if (T >= 2.0f && T < 4.6f && air)
            {
                float la = Ease01((T - 2.0f) / .4f) * Ease01((4.6f - T) / .4f);
                PointF lamp = Pt(wx - 124 * wsc, wy - 46 * wsc);
                GradCone(lamp, Pt(150, gy), Pt(300, gy), Color.FromArgb(255, 252, 226), 110 * la, 28 * la);
                Glow(220, gy - 4, 110, 18, Al(Color.FromArgb(255, 252, 226), 150 * la));
            }

            // 4. 배트윙 본체
            if (air)
            {
                float hatch = Ease01((T - 2.6f) / .3f) * (1 - Ease01((T - 4.15f) / .3f));
                float lightOn = T >= 1.8f && T < 5.0f ? 1f : 0f;
                DrawBatwingCraft(wx, wy, wsc, wtilt, phase, rotBlur, hatch, lightOn, T, 1f);
                // 윈치 케이블: 해치에서 내려와 배트맨의 손에 걸린다
                if (T >= 2.7f && T < 4.2f)
                {
                    PointF src = Pt(wx, wy - 30 * wsc);
                    float lowerU = Ease01((T - 2.7f) / .6f), hookY;
                    float riseY = 150 + by;
                    hookY = T < 3.3f ? src.Y + (330 - src.Y) * lowerU : (T < 3.4f ? 330 + (riseY - 330) * Ease01((T - 3.3f) / .1f) : riseY);
                    PointF hook = Pt(200 + bx + (T < 3.3f ? 0f : 0f), hookY);
                    var cable = new[] { src, Lerp2(src, hook, .5f), hook };
                    Stroke(cable, Al(cInk, 255), 5.6f); Stroke(cable, Al(Color.FromArgb(150, 160, 180), 255), 3f);
                    g.FillPolygon(B(Color.FromArgb(205, 214, 230)), new[] { Pt(hook.X - 9, hook.Y), Pt(hook.X + 9, hook.Y), Pt(hook.X + 6, hook.Y + 14), Pt(hook.X - 6, hook.Y + 14) });
                    g.DrawPolygon(Pn(cInk, 1.6f), new[] { Pt(hook.X - 9, hook.Y), Pt(hook.X + 9, hook.Y), Pt(hook.X + 6, hook.Y + 14), Pt(hook.X - 6, hook.Y + 14) });
                    if (T >= 3.3f && T < 3.6f) { float u = (T - 3.3f) / .3f; Glint(hook.X, hook.Y, 40 * (1 - u * .4f), Color.White, 1 - u, 45); }
                }
                if (T >= 4.4f && T < 5.8f) for (int i = 0; i < 8; i++) { float ph = (T * 3f + i * .125f) % 1f; Stroke(new[] { PtC(wx + 120 * wsc + ph * 160, wy - 40 * wsc + (i - 3.5f) * 6), PtC(wx + 120 * wsc + ph * 160 + 40, wy - 40 * wsc + (i - 3.5f) * 6) }, Al(Color.White, 150 * (1 - ph)), 2.4f); }
            }

            // 5. 망토 글라이더로 강하(6.3~7.4) + 착지의 충격 고리
            if (T >= 6.3f && T < 7.5f)
            {
                float u = (T - 6.3f) / 1.1f;
                for (int i = 0; i < 10; i++) { float x = 90 + 220 * Hash01(i, 2), y0 = 40 + 480 * Hash01(i, 3), len = 40 + 60 * Hash01(i, 4); float ph = (T * 3f + i * .1f) % 1f; Stroke(new[] { Pt(x, y0 + ph * 120), Pt(x, y0 + ph * 120 + len) }, Al(Color.White, 140 * (1 - u * .7f) * (1 - ph)), 2f); }
            }
            if (T >= 7.35f && T < 8.3f)
            {
                float u = (T - 7.35f) / .95f;
                GroundRing(200, gy, 20 + 190 * (float)Math.Sqrt(u), .17f, Color.FromArgb(220, 230, 245), 1 - u, 3.4f);
                for (int sd = -1; sd <= 1; sd += 2) Glow(200 + sd * (30 + 100 * (1 - (float)Math.Exp(-u * 3f))), 552 - 12 * u, 34 + 20 * u, 18, Al(Color.FromArgb(214, 214, 224), 130 * (1 - u)));
            }
        }
    }
}
