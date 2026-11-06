using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace SuperHeroes
{
    // 수퍼맨·배트맨·스파이더맨 시그니처 연출이 함께 쓰는 작은 도구들.
    // (Glow / BoltPath / Stroke / StrokeBolt / StrokeThin 은 HeroRigFx.cs, PtC / Ease01 / SigOn 계열은 여기와 HeroRigThorFx.cs)
    internal sealed partial class HeroPainter
    {
        /// <summary>지금 이 히어로가 idx 번 시그니처를 실행 중인가(시간 정보가 있는 새 연출용).</summary>
        private bool SigOn(int idx) { return p.SigAct == idx && p.SigT > .0005f; }

        /// <summary>결정론적 의사 난수 0..1 (같은 i 는 항상 같은 값 — 프레임마다 떨리지 않는 배치용).</summary>
        private static float Hash01(int i, int salt = 0)
        {
            unchecked
            {
                uint x = (uint)(i * 73856093 ^ salt * 19349663 ^ 0x5bd1e995);
                x ^= x >> 13; x *= 0x5bd1e995; x ^= x >> 15;
                return (x & 0xffff) / 65535f;
            }
        }

        /// <summary>들쭉날쭉한 만화식 폭발 모양.</summary>
        private void Starburst(float cx, float cy, float rOut, float rIn, int spikes, float rotDeg, Color fill, Color edge, float edgeW)
        {
            if (rOut < 2f) return;
            var pts = new PointF[spikes * 2];
            for (int i = 0; i < pts.Length; i++)
            {
                double ang = rotDeg * Math.PI / 180.0 + i * Math.PI / spikes;
                float r = (i % 2 == 0 ? rOut : rIn) * (.82f + .36f * Hash01(i, spikes));
                pts[i] = Pt(CxClamp(cx + (float)Math.Cos(ang) * r), cy + (float)Math.Sin(ang) * r);
            }
            g.FillPolygon(B(fill), pts);
            if (edge.A > 3 && edgeW > 0) g.DrawPolygon(Pn(edge, edgeW), pts);
        }

        /// <summary>외곽선이 두꺼운 만화 의성어. (좌우 반전된 리그에서도 글자가 뒤집히지 않는다)</summary>
        private void ComicText(string s, float x, float y, float size, float rot, Color fill, float a)
        {
            if (a <= .02f) return;
            a = Math.Min(1f, a);
            var path = new GraphicsPath(); trash.Add(path);
            using (var ff = new FontFamily("Arial"))
                path.AddString(s, ff, (int)FontStyle.Bold, size, new PointF(0, 0), StringFormat.GenericDefault);
            RectangleF rc = path.GetBounds();
            var st = g.Save();
            g.TranslateTransform(x, y); g.RotateTransform(rot); g.ScaleTransform(faceRight ? -1f : 1f, 1f);
            g.TranslateTransform(-(rc.X + rc.Width / 2f), -(rc.Y + rc.Height / 2f));
            g.DrawPath(Pn(Al(cInk, 255 * a), size * .30f), path);
            g.DrawPath(Pn(Al(Color.White, 255 * a), size * .17f), path);
            g.FillPath(B(Al(fill, 255 * a)), path);
            g.Restore(st);
        }

        /// <summary>네 갈래 별빛(렌즈 플레어·반짝임).</summary>
        private void Glint(float x, float y, float r, Color c, float a, float rot = 0f)
        {
            if (a <= .02f || r < 1f) return;
            double q = rot * Math.PI / 180.0;
            for (int i = 0; i < 2; i++)
            {
                double ang = q + i * Math.PI / 2; float rr = i == 0 ? r : r * .6f;
                var seg = new[] { Pt(CxClamp(x - (float)Math.Cos(ang) * rr), y - (float)Math.Sin(ang) * rr), Pt(CxClamp(x + (float)Math.Cos(ang) * rr), y + (float)Math.Sin(ang) * rr) };
                Stroke(seg, Al(c, 200 * a), Math.Max(1.6f, r * .16f));
                Stroke(seg, Al(Color.White, 255 * a), Math.Max(1f, r * .07f));
            }
        }

        /// <summary>6갈래 눈송이.</summary>
        private void Snowflake(float x, float y, float r, float rot, Color c, float a)
        {
            if (a <= .02f) return;
            for (int i = 0; i < 3; i++)
            {
                double ang = (rot + i * 60) * Math.PI / 180.0; float dx = (float)Math.Cos(ang) * r, dy = (float)Math.Sin(ang) * r;
                Stroke(new[] { PtC(x - dx, y - dy), PtC(x + dx, y + dy) }, Al(c, 255 * a), Math.Max(1.4f, r * .22f));
            }
        }

        /// <summary>땅 위에서 퍼지는 납작한 충격 고리(두꺼운 빛무리 + 흰 심지).</summary>
        private void GroundRing(float x, float y, float r, float flat, Color c, float a, float w)
        {
            if (a <= .02f || r < 2f) return;
            r = Math.Min(r, Math.Min(x + 40f, 440f - x));
            float ry = r * flat + 1.5f;
            g.DrawEllipse(Pn(Al(c, 170 * a), w * 2.2f), x - r, y - ry, r * 2, ry * 2);
            g.DrawEllipse(Pn(Al(Color.White, 245 * a), w), x - r, y - ry, r * 2, ry * 2);
        }

        /// <summary>끝으로 갈수록 투명해지는 부채꼴/원뿔 (현재 변환 좌표계).</summary>
        private void GradCone(PointF tip, PointF a, PointF b, Color c0, float a0, float a1)
        {
            var cone = new GraphicsPath(); cone.AddPolygon(new[] { tip, a, b }); trash.Add(cone);
            PointF mid = Pt((a.X + b.X) / 2f, (a.Y + b.Y) / 2f);
            if (Math.Abs(mid.X - tip.X) + Math.Abs(mid.Y - tip.Y) < 2f) return;
            var br = new LinearGradientBrush(tip, mid, Al(c0, a0), Al(c0, a1)); trash.Add(br);
            g.FillPath(br, cone);
        }
    }
}
