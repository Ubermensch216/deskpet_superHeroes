using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace SuperHeroes
{
    internal enum EyeStyle { Open, Happy, Sleep, Spiral, Narrow, Shock }
    internal enum MouthStyle { Neutral, Smile, Open, Scared, Frown, Pout, Grit }
    internal enum HandStyle { Open, Fist, Point, Web }
    internal enum Reach { None, Lap, Belly, Mouth, Chest, Scratch, Chin, Cross, Overhead, Forehead, Hips, Cup, Front, Flex, Fist, Guard, Hammer, WebThrow, Umbrella }
    internal enum Persona { Superman = 0, Batman = 1, Spiderman = 2, Thor = 3 }

    /// <summary>
    /// 한 순간의 자세. 모든 길이 값은 기준 그림(키 571)의 단위이고,
    /// 회전은 화면 기준 시계 방향이 +, 그림은 "왼쪽을 보는" 모습이 기준이다(오른쪽은 좌우 반전해서 그린다).
    /// </summary>
    internal sealed class Pose
    {
        // 몸
        public float BodyX, BodyY, Lean;       // 상체 이동·기울임(발을 기준으로 한 엉덩이 축)
        public float Breath;                   // 숨쉬기 (-1..1)
        public bool Sit;                       // 앉은 자세(다리를 앞으로)
        public float Turn;                     // 몸 돌림: 0 정면 .. 1 3/4 옆모습
        // 머리
        public int View;                       // 0 정면, 1 3/4 옆모습(왼쪽을 봄)
        public float HeadX, HeadY, HeadTilt;
        public float EarL, EarR;               // 머리카락·망토 펄럭임
        // 눈
        public EyeStyle Eyes;
        public float OpenL = 1, OpenR = 1;     // 눈 뜬 정도
        public float Gx, Gy, Cross = .7f;      // 시선(화면 기준 x,y)과 모아보기
        public float PupilR = 1, EyeScale = 1; // 눈동자·눈 크기
        public float Brow;                     // + 화남/카리스마, - 슬픔/걱정
        public float SpiralT;                  // 소용돌이 눈 회전 위상
        // 입
        public MouthStyle Mouth;
        public float MouthOpen;                // 0..1
        public float Tongue;                   // 0..1 혀 내밀기
        // 팔
        public float ArmLA = 60, ArmLB = 43, ArmRA = 60, ArmRB = 43;
        public float ArmLenL = 1, ArmLenR = 1;
        public HandStyle HandL, HandR;
        public Reach Reach;                    // 손 위치 프리셋
        public PointF? HandLTo, HandRTo;
        public bool Clasp;
        public bool ArmsFront;                 // 팔을 몸통 앞에 그리기
        public bool Umbrella;                  // 낙하 모션 등
        public float UmbrellaTilt;
        // 다리
        public float FootLX, FootLY, FootRX, FootRY, FootLRot, FootRRot;
        public float Knee;
        // 망토 및 꼬리 호환 필드
        public float CapeWing;                 // 망토 넓게 펼치기(배트맨 가고일 등) 0..1
        public float CapeWind;                 // 망토 펄럭임 바람 세기
        public float Tail, TailCurl, TailWaveAmp, TailWavePhase;
        // 장식 효과
        public float Tears = -1;
        public float Anger;
        public float Sweat = -1;
        public float Cookie = -1;
        public float Flush;
        public float Lie;                      // 누운 정도 (0..1)
        public float Wipe = -1;

        // ── 수퍼히어로 전용 이펙트 & 시그니처 파라미터 ──
        public int SigAct = -1;                // 현재 실행 중인 시그니처 (0..3)
        public float SigPhase;                 // 시그니처 진행도 (0..1)
        public float HeatLaser;                // 수퍼맨: 히트 비전 (0..1)
        public float FreezeMist;               // 수퍼맨: 냉기 입김 (0..1)
        public float ShirtOpen;                // 수퍼맨: 셔츠 뜯기 (0..1)
        public float SonicGlow;                // 수퍼맨: 초음속 비행 오라 (0..1)
        
        public float Batarang;                 // 배트맨: 배트랭 던지기/날아감 (0..1)
        public float Grapnel;                  // 배트맨: 그래플건 와이어 (0..1)
        public float SmokeBomb;                // 배트맨: 연막탄 (0..1)
        
        public float WebSwing;                 // 스파이더맨: 웹 스윙 (0..1)
        public float SpiderSense;              // 스파이더맨: 스파이더 센스 찌릿 (0..1)
        public float UpsideDown;               // 스파이더맨: 거꾸로 매달리기 (0..1)
        public float WebNet;                   // 스파이더맨: 거미줄 네트 (0..1)

        public float MjolnirSpin;              // 토르: 묠니르 회전 (0..1)
        public float Lightning;                // 토르: 번개 소환 (0..1)
        public float GroundCrush;              // 토르: 지면 강타 충격파 (0..1)
        public float HammerThrow;              // 토르: 묠니르 투척 (0..1)

        public float Aura;                     // 공통 에너지 오라 (0..1)
        public int AuraKind;                   // 오라 색상 인덱스
        public float AuraT;                    // 오라 위상
        public Color AuraColor = Color.Gold;

        // 호환용 더미 필드
        public float Nimbus, Spark, Flash, BeamLen, Shot, Staff, StaffLen;

        public Pose Clone() { return (Pose)MemberwiseClone(); }

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
                add((int)Reach, 1); add(Clasp ? 1 : 0, 1); add(ArmsFront ? 1 : 0, 1);
                add(FootLX, 2); add(FootLY, 2); add(FootRX, 2); add(FootRY, 2); add(FootLRot, 2); add(FootRRot, 2); add(Knee, 4);
                add(CapeWing, 10); add(CapeWind, 5); add(HeatLaser, 10); add(FreezeMist, 10); add(ShirtOpen, 10); add(SonicGlow, 10);
                add(Batarang, 10); add(Grapnel, 10); add(SmokeBomb, 10);
                add(WebSwing, 10); add(SpiderSense, 10); add(WebNet, 10);
                add(MjolnirSpin, 10); add(Lightning, 10); add(GroundCrush, 10); add(HammerThrow, 10);
                add(Aura, 10); add(Lie, 10);
                return h;
            }
        }
    }

    /// <summary>GDI+ 그래픽 곡선 및 리본 그리기 유틸리티.</summary>
    internal static class RigGfx
    {
        public static PointF[] Ribbon(PointF[] s, float[] w, bool roundEnd = true)
        {
            if (s == null || s.Length < 2) return new PointF[0];
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
        public abstract float RefH { get; }
        public abstract float FootX { get; }
        public abstract float FootY { get; }
        public abstract float HeadAxisDx { get; }
        public abstract float GripY { get; }
        public abstract float BodyRefW { get; }
        public abstract float NominalW { get; }
        public abstract float BellyY { get; }
        public abstract float HeadCx { get; }
        public abstract float HeadCy { get; }
        public abstract float HeadSize { get; }
        public float U { get { return RefH / 571f; } }
        protected abstract void DrawBody(Graphics g, Pose p, float k, bool faceRight);

        public void Draw(Graphics g, Pose p, float k, bool faceRight)
        {
            if (Math.Abs(p.Lie) < .01f) { DrawBody(g, p, k, faceRight); return; }
            var st = g.Save();
            float ang = (faceRight ? -90f : 90f) * p.Lie, a = Math.Abs(p.Lie);
            g.TranslateTransform(-Math.Sign(ang) * RefH * k * .5f * a, -BodyRefW * .5f * k * a);
            g.RotateTransform(ang);
            DrawBody(g, p, k, faceRight);
            g.Restore(st);
        }
    }

    /// <summary>공통 역기구학(IK) 계산.</summary>
    internal static class Kit
    {
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
