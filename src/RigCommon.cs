using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace DragonBall
{
    internal enum EyeStyle { Open, Happy, Sleep, Spiral }
    internal enum MouthStyle { Neutral, Smile, Open, Scared, Frown, Pout }
    internal enum HandStyle { Open, Fist, Point }
    /// <summary>손이 모이는 곳(캐릭터마다 몸 크기가 달라서 이름으로 지정하고, 리그가 좌표로 바꾼다).</summary>
    internal enum Reach { None, Lap, Belly, Mouth, Chest, Scratch, Chin, Cross, Overhead, Forehead, Hips, Cup, Front, Flex, Guard }
    /// <summary>캐릭터 성격(행동·몸짓·시그니처 기술이 캐릭터마다 다르다).</summary>
    internal enum Persona { Goku, Kid, Roshi, Vegeta, Piccolo }
    /// <summary>기(氣) 에너지의 종류 — 구슬과 광선의 색·모양을 정한다.</summary>
    internal enum KiKind { None, Blue, Purple, Yellow, Spiral, Suck }

    /// <summary>
    /// 한 순간의 자세. 모든 길이 값은 기준 그림(키 571)의 단위이고,
    /// 회전은 화면 기준 시계 방향이 +, 그림은 "왼쪽을 보는" 모습이 기준이다(오른쪽은 좌우 반전해서 그린다).
    /// 팔 각도(ArmLA 등)는 '동물 팔' 기준(60 = 내려뜨림)으로 쓰이고, 리그가 사람 팔 각도로 바꿔 그린다.
    /// </summary>
    internal sealed class Pose
    {
        // 몸
        public float BodyX, BodyY, Lean;       // 상체 이동·기울임(발을 기준으로 한 엉덩이 축)
        public float Breath;                   // 숨쉬기 (-1..1)
        public bool Sit;                       // 앉은 자세(다리를 앞으로)
        public float Turn;                     // 몸 돌림: 0 정면 .. 1 3/4 옆모습(기 모으기·발사 자세)
        // 머리
        public int View;                       // 0 정면, 1 3/4 옆모습(왼쪽을 봄)
        public float HeadX, HeadY, HeadTilt;
        public float EarL, EarR;               // 머리카락 끝·더듬이 흔들림(각도)
        // 눈
        public EyeStyle Eyes;
        public float OpenL = 1, OpenR = 1;     // 눈 뜬 정도
        public float Gx, Gy, Cross = .7f;      // 시선(화면 기준 x,y)과 모아보기
        public float PupilR = 1, EyeScale = 1; // 눈동자·눈 크기
        public float Brow;                     // + 화남(눈 안쪽이 내려감), - 슬픔
        public float SpiralT;                  // 소용돌이 눈 회전 위상
        // 입
        public MouthStyle Mouth;
        public float MouthOpen;                // 0..1
        public float Tongue;                   // 0..1 혀 내밀기
        // 팔: 어깨에서 위팔 각도, 팔꿈치 이후 아래팔 각도(둘 다 '아래쪽'을 0으로, 바깥쪽이 +)
        public float ArmLA = 60, ArmLB = 43, ArmRA = 60, ArmRB = 43;
        public float ArmLenL = 1, ArmLenR = 1; // 팔 길이 배율(피콜로의 늘어나는 팔)
        public HandStyle HandL, HandR;
        public Reach Reach;                    // 있으면 팔 각도 대신 손을 그곳으로 모은다
        public PointF? HandLTo, HandRTo;       // 손목 목표(직접 지정하거나 Reach 로 채워진다)
        public bool Clasp;                     // 가슴 앞에서 깍지
        public bool ArmsFront;                 // 팔을 몸통 앞에 그림
        // 다리
        public float FootLX, FootLY, FootRX, FootRY, FootLRot, FootRRot;
        public float Knee;                     // 무릎이 바깥으로 벌어지는 정도
        // 꼬리·망토
        public float Tail, TailCurl, TailWaveAmp, TailWavePhase;
        // 얹는 장식
        public float Tears = -1;               // >=0 이면 눈물 흐름(위상)
        public float Anger;                    // 0..1 화남 표시(💢)
        public float Sweat = -1;               // >=0 이면 식은땀
        public float Cookie = -1;              // >=0 이면 간식(먹은 정도 0..1)
        public float Flush;                    // 볼 붉어짐 0..1
        public float Lie;                      // 누운 정도(0 서기 .. 1 등을 대고 누움)
        public float Wipe = -1;                // >=0 이면 손을 흔들며 비비는 동작의 위상
        // ── 드래곤볼 전용 ──
        public float Aura; public int AuraKind; public float AuraT;   // 기 오라(0..1), 색 종류(1 흰 2 금 3 파랑 4 보라 5 초록), 일렁임 위상
        public float Super;                    // 변신 정도(0..1): 머리가 금색·눈이 청록(사이어인) / 피콜로는 망토 벗기
        public float Muscle;                   // 무천도사 MAX 파워 근육(0..1)
        public float Ape;                      // 어린 오공 대원숭이 변신(0..1)
        public float Ki; public int KiKind;    // 손 사이의 기 구슬 크기(0..1)와 종류
        public PointF? KiAt;                   // 구슬 위치(없으면 두 손 사이)
        public float BeamLen, BeamW, BeamT;    // 광선 길이(기준 단위)·굵기(0..1)·일렁임 위상
        public float Shot = -1;                // >=0 이면 날아가는 기탄(0..1 = 비행 진행, 1 에서 폭발)
        public float Flash;                    // 번쩍임(0..1)
        public float Spark;                    // 번개 스파크(0..1)
        public float Staff; public float StaffAngle, StaffLen = 1;   // 여의봉: 0 없음 · 1 들고 있음(무천도사 지팡이) · 2 자유롭게
        public int Prop;                       // 1 전기밥솥(닫힘) 2 전기밥솥(열림)
        public float Nimbus;                   // 근두운(0..1)

        public static float Lerp(float a, float b, float t) { return a + (b - a) * t; }

        /// <summary>그림이 달라지는 값만 모아 낮은 해상도로 해시 — 같은 모습이면 다시 그리지 않는다.</summary>
        public long Hash()
        {
            unchecked
            {
                long h = 1469598103934665603L;
                Action<float, float> add = (v, q) => { h = (h ^ (long)Math.Round(v * q)) * 1099511628211L; };
                add(BodyX, 2); add(BodyY, 2); add(Lean, 4); add(Breath, 20); add(Sit ? 1 : 0, 1); add(View, 1); add(Turn, 10);
                add(HeadX, 2); add(HeadY, 2); add(HeadTilt, 4); add(EarL, 3); add(EarR, 3);
                add((int)Eyes, 1); add(OpenL, 12); add(OpenR, 12); add(Gx, 10); add(Gy, 10); add(Cross, 10);
                add(PupilR, 10); add(EyeScale, 20); add(Brow, 10); add(SpiralT, 5); add((int)Mouth, 1); add(MouthOpen, 12);
                add(Tongue, 8); add(ArmLA, 3); add(ArmLB, 3); add(ArmRA, 3); add(ArmRB, 3); add(ArmLenL, 20); add(ArmLenR, 20); add((int)HandL, 1); add((int)HandR, 1);
                add(HandLTo.HasValue ? HandLTo.Value.X : -999, 2); add(HandLTo.HasValue ? HandLTo.Value.Y : -999, 2);
                add(HandRTo.HasValue ? HandRTo.Value.X : -999, 2); add(HandRTo.HasValue ? HandRTo.Value.Y : -999, 2);
                add(Clasp ? 1 : 0, 1); add(ArmsFront ? 1 : 0, 1);
                add(FootLX, 2); add(FootLY, 2); add(FootRX, 2); add(FootRY, 2); add(FootLRot, 3); add(FootRRot, 3); add(Knee, 3);
                add(Tail, 3); add(TailCurl, 3); add(TailWaveAmp, 3); add(TailWavePhase, 6);
                add(Lie, 20); add(Wipe, 12); add((int)Reach, 1); add(Tears, 8); add(Anger, 8); add(Sweat, 8); add(Cookie, 20); add(Flush, 8);
                add(Aura, 10); add(AuraKind, 1); add(AuraT, 10); add(Super, 12); add(Muscle, 12); add(Ape, 12);
                add(Ki, 14); add(KiKind, 1); add(KiAt.HasValue ? KiAt.Value.X : -999, 2); add(KiAt.HasValue ? KiAt.Value.Y : -999, 2);
                add(BeamLen, 1); add(BeamW, 12); add(BeamT, 12); add(Shot, 40); add(Flash, 12); add(Spark, 10);
                add(Staff, 2); add(StaffAngle, 4); add(StaffLen, 20); add(Prop, 1); add(Nimbus, 12);
                return h;
            }
        }
    }

    /// <summary>다각형 계산 보조.</summary>
    internal static class Geo
    {
        public static PointF P(float x, float y) { return new PointF(x, y); }

        private static PointF CR(PointF p0, PointF p1, PointF p2, PointF p3, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            return new PointF(
                .5f * ((2 * p1.X) + (-p0.X + p2.X) * t + (2 * p0.X - 5 * p1.X + 4 * p2.X - p3.X) * t2 + (-p0.X + 3 * p1.X - 3 * p2.X + p3.X) * t3),
                .5f * ((2 * p1.Y) + (-p0.Y + p2.Y) * t + (2 * p0.Y - 5 * p1.Y + 4 * p2.Y - p3.Y) * t2 + (-p0.Y + 3 * p1.Y - 3 * p2.Y + p3.Y) * t3));
        }

        /// <summary>제어점을 지나는 부드러운 곡선(Catmull-Rom)을 촘촘한 꺾은선으로 바꾼다.</summary>
        public static PointF[] Smooth(PointF[] c, bool closed, int sub = 7)
        {
            int n = c.Length; var res = new List<PointF>();
            int segs = closed ? n : n - 1;
            for (int i = 0; i < segs; i++)
            {
                PointF p0 = c[closed ? (i - 1 + n) % n : Math.Max(i - 1, 0)], p1 = c[i];
                PointF p2 = c[closed ? (i + 1) % n : i + 1], p3 = c[closed ? (i + 2) % n : Math.Min(i + 2, n - 1)];
                for (int s = 0; s < sub; s++) res.Add(CR(p0, p1, p2, p3, s / (float)sub));
            }
            if (!closed) res.Add(c[n - 1]);
            return res.ToArray();
        }

        public static PointF[] Mirror(PointF[] p, float axis)
        {
            var r = new PointF[p.Length];
            for (int i = 0; i < p.Length; i++) r[i] = new PointF(2 * axis - p[p.Length - 1 - i].X, p[p.Length - 1 - i].Y);
            return r;
        }

        public static PointF[] Ellipse(float cx, float cy, float rx, float ry, int n = 24)
        {
            var r = new PointF[n];
            for (int i = 0; i < n; i++) { double a = i * Math.PI * 2 / n; r[i] = new PointF(cx + (float)Math.Cos(a) * rx, cy + (float)Math.Sin(a) * ry); }
            return r;
        }

        public static PointF[] Map(PointF[] p, Func<PointF, PointF> f)
        {
            var r = new PointF[p.Length];
            for (int i = 0; i < p.Length; i++) r[i] = f(p[i]);
            return r;
        }

        /// <summary>a*x + b*y + c &gt;= 0 인 쪽만 남긴다(Sutherland-Hodgman).</summary>
        public static PointF[] ClipHalf(PointF[] poly, float a, float b, float c)
        {
            var res = new List<PointF>();
            for (int i = 0; i < poly.Length; i++)
            {
                PointF cur = poly[i], nxt = poly[(i + 1) % poly.Length];
                float dc = a * cur.X + b * cur.Y + c, dn = a * nxt.X + b * nxt.Y + c;
                if (dc >= 0) res.Add(cur);
                if ((dc >= 0) != (dn >= 0))
                {
                    float t = dc / (dc - dn);
                    res.Add(new PointF(cur.X + (nxt.X - cur.X) * t, cur.Y + (nxt.Y - cur.Y) * t));
                }
            }
            return res.ToArray();
        }

        /// <summary>뼈대(spine)를 따라 굵기(w)를 준 몸통 윤곽. 끝은 둥글게 닫을 수 있다.</summary>
        public static PointF[] Limb(PointF[] spine, float[] w, bool roundEnd, int sub = 6)
        {
            PointF[] s = Smooth(spine, false, sub);
            int m = s.Length;
            var left = new PointF[m]; var right = new PointF[m];
            for (int i = 0; i < m; i++)
            {
                PointF a = s[Math.Max(i - 1, 0)], b = s[Math.Min(i + 1, m - 1)];
                float tx = b.X - a.X, ty = b.Y - a.Y, len = (float)Math.Sqrt(tx * tx + ty * ty);
                if (len < 1e-4f) { tx = 1; ty = 0; len = 1; }
                tx /= len; ty /= len;
                float u = i / (float)(m - 1) * (w.Length - 1); int i0 = Math.Min((int)u, w.Length - 2); float f = u - i0;
                float wd = (w.Length == 1 ? w[0] : w[i0] + (w[i0 + 1] - w[i0]) * f) / 2;
                left[i] = new PointF(s[i].X - ty * wd, s[i].Y + tx * wd);
                right[i] = new PointF(s[i].X + ty * wd, s[i].Y - tx * wd);
            }
            var res = new List<PointF>(left);
            if (roundEnd)
            {
                PointF a = s[m - 2], b = s[m - 1];
                double ang = Math.Atan2(b.Y - a.Y, b.X - a.X); float wd = (w[w.Length - 1]) / 2;
                for (int i = 1; i < 8; i++)
                {
                    double q = ang + Math.PI / 2 - Math.PI * i / 8.0;
                    res.Add(new PointF(b.X + (float)Math.Cos(q) * wd, b.Y + (float)Math.Sin(q) * wd));
                }
            }
            for (int i = m - 1; i >= 0; i--) res.Add(right[i]);
            return res.ToArray();
        }
    }

    /// <summary>캐릭터 한 명의 그림 도구. 자세(Pose)의 길이 값은 '키 571 기준'이다.</summary>
    internal abstract class CharRig
    {
        public abstract float RefH { get; }            // 서 있을 때 머리 맨 위 ~ 발바닥
        public abstract float FootX { get; }
        public abstract float FootY { get; }
        public abstract float HeadAxisDx { get; }
        public abstract float GripY { get; }           // 머리 윗부분의 기준 그림 y (장식 위치)
        public abstract float BodyRefW { get; }        // 부딪힘 판정에 쓰는 몸 너비
        public abstract float NominalW { get; }        // 이동 기준 너비
        public abstract float BellyY { get; }          // 배 앞에 손을 모을 때의 기준 그림 y
        public abstract float HeadCx { get; }          // 아이콘용: 머리 가운데 x
        public abstract float HeadCy { get; }
        public abstract float HeadSize { get; }
        public float U { get { return RefH / 571f; } }
        protected abstract void DrawBody(Graphics g, Pose p, float k, bool faceRight);

        /// <summary>g 의 원점이 발 가운데(픽셀). 눕기(Lie)는 몸 전체를 90° 눕혀서 바닥에 붙인다.</summary>
        public void Draw(Graphics g, Pose p, float k, bool faceRight)
        {
            if (Math.Abs(p.Lie) < .01f) { DrawBody(g, p, k, faceRight); return; }
            var st = g.Save();
            float ang = (faceRight ? -90f : 90f) * p.Lie, a = Math.Abs(p.Lie);
            // 누우면 몸이 바닥에 붙고, 몸 가운데가 서 있던 자리에 오도록 옆으로 옮긴다
            g.TranslateTransform(-Math.Sign(ang) * RefH * k * .5f * a, -BodyRefW * .5f * k * a);
            g.RotateTransform(ang);
            DrawBody(g, p, k, faceRight);
            g.Restore(st);
        }
    }

    /// <summary>여러 캐릭터가 같이 쓰는 팔 계산.</summary>
    internal static class Kit
    {
        /// <summary>두 마디 팔: 어깨 S 에서 손목이 T 에 닿도록 위팔(A)·아래팔(B) 각도를 구한다(아래쪽 0°, 몸 바깥이 +).</summary>
        public static void IK(PointF S, PointF T, float L1, float L2, int outSign, out float A, out float B)
        {
            float dx = T.X - S.X, dy = T.Y - S.Y, raw = (float)Math.Sqrt(dx * dx + dy * dy), dist = raw;
            dist = Math.Max(Math.Abs(L1 - L2) + .5f, Math.Min(L1 + L2 - .5f, dist));
            double th = Math.Atan2(dy, dx), alpha = Math.Acos(Math.Max(-1, Math.Min(1, (L1 * L1 + dist * dist - L2 * L2) / (2 * L1 * dist))));
            PointF best = PointF.Empty; double bestScore = double.MinValue;
            for (int sg = -1; sg <= 1; sg += 2)
            {
                double a = th + sg * alpha;
                PointF e = new PointF(S.X + (float)Math.Cos(a) * L1, S.Y + (float)Math.Sin(a) * L1);
                double score = (e.X - S.X) * outSign + (e.Y - S.Y) * .6;
                if (score > bestScore) { bestScore = score; best = e; }
            }
            PointF v1 = new PointF(best.X - S.X, best.Y - S.Y);
            float k = raw < 1e-3f ? 0 : dist / raw;
            PointF wr = new PointF(S.X + dx * k, S.Y + dy * k);
            PointF v2 = new PointF(wr.X - best.X, wr.Y - best.Y);
            A = (float)(Math.Atan2(outSign * v1.X, v1.Y) * 180 / Math.PI);
            B = (float)(Math.Atan2(outSign * v2.X, v2.Y) * 180 / Math.PI);
        }
    }
}
