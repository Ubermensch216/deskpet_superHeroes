using System;
using System.Drawing;

namespace SuperHeroes
{
    /// <summary>
    /// 수퍼맨의 숨은 반응 '크립토나이트': 수퍼맨을 짧은 간격으로 5번 연달아 클릭하면
    /// 초록 빛나는 돌이 나타나 힘이 빠지고, 무릎을 꿇고 주저앉았다가, 돌이 사라지면 기운을 되찾는다.
    /// </summary>
    internal sealed partial class Pet
    {
        internal const double KryptoDuration = 8.6;
        private const int KryptoClicks = 5;
        private const double KryptoClickGap = 1.0;

        private double krypCooldown, lastPokeAt;
        private int pokeStreak, krypPhase;

        internal bool IsKryptonite { get { return State == PetState.Kryptonite; } }

        /// <summary>수퍼맨을 1초 안 간격으로 5번 클릭했는지 센다. 조건이 채워지면 true.</summary>
        private bool CountKryptoClick(IWorld w)
        {
            if (Who != Persona.Superman || !PlayOptions.Current.Secrets || w.Now < krypCooldown) { pokeStreak = 0; return false; }
            if (pokeStreak > 0 && w.Now - lastPokeAt > KryptoClickGap) pokeStreak = 0;
            lastPokeAt = w.Now; pokeStreak++;
            if (pokeStreak < KryptoClicks) return false;
            pokeStreak = 0;
            return true;
        }

        internal void StartKryptonite(IWorld w)
        {
            krypCooldown = w.Now + 25; pokeStreak = 0; krypPhase = 0;
            orbitSeen = false; orbitTotal = 0;
            extraDuration = KryptoDuration; mouseGrace = w.Now + KryptoDuration + 1.5; hoverSince = -1;
            Bubble = null;
            SetState(PetState.Kryptonite, w); FriendData.Current.Discover("krypto");
        }

        /// <summary>단계가 바뀔 때마다 한마디씩.</summary>
        private void KryptoBubble(IWorld w)
        {
            double t = StateT; string say = null; int phase = 0;
            if (t >= .9) { phase = 1; say = Loc.T("으윽… 이 초록 빛은…!"); }
            if (t >= 3.3) { phase = 2; say = Loc.T("힘이… 빠져…"); }
            if (t >= 6.5) { phase = 3; say = Loc.T("휴… 살았다!"); }
            if (phase == krypPhase) return;
            krypPhase = phase;
            if (say != null) { Bubble = say; BubbleUntil = w.Now + (phase == 2 ? 3.0 : 2.4); }
        }

        /// <summary>
        /// 0–.9 초록 돌이 톡 튀어나와 놀람 → .9–3 힘이 빠져 떨며 가슴을 움켜쥠 → 3–3.9 무릎을 꿇고 주저앉음
        /// → 3.9–6.3 고개를 떨군 채 신음 → 6.3–7 돌이 사라짐 → 6.9–8.2 기운을 되찾고 일어섬.
        /// </summary>
        private void KryptoPose(Pose P)
        {
            double t = StateT;
            double glowIn = Ease(Seg(t, 0, .5)), fade = Ease(Seg(t, 6.3, 7.0));
            double weak = Ease(Seg(t, .9, 3.0)), slump = Ease(Seg(t, 3.0, 3.9)), rec = Ease(Seg(t, 6.9, 8.2));
            double w = weak * (1 - rec), s = slump * (1 - rec);
            double startle = Math.Sin(Math.PI * Math.Min(1, Math.Max(0, (t - .25) / .65)));
            double shake = Math.Sin(t * 41) * (.7 + 3.3 * w) * (1 - .5 * s);
            double groan = Math.Sin(t * 5.5);

            P.Krypto = F(glowIn * (1 - fade)); P.KryptoT = F(t);
            P.Sick = F(Math.Min(1, weak * 1.15) * (1 - Ease(Seg(t, 7.2, 8.4))));
            P.Breath = F(Math.Sin(t * (4 + 6 * w)) * (.4 + 2.2 * w));

            // 몸: 떨며 무릎이 꺾이고, 주저앉으며 앞으로 숙인다
            P.BodyX = F(shake * .7);
            P.BodyY = F(24 * w + 42 * s + Math.Max(0, groan) * 2 * s);
            P.Lean = F(-startle * 7 + 6 * w + 9 * s + shake * .3);
            P.HeadTilt = F(-startle * 4 + (6 * w + 10 * s) + Math.Sin(t * 2.2) * 2 * s + shake * .4);
            P.HeadY = F(5 * w + 16 * s);
            P.HeadX = F(-2 * s);
            P.Knee = F(1.0 * w + 1.8 * s);
            P.FootLX = F(8 * s + Math.Sin(t * 33) * 1.6 * w); P.FootRX = F(-8 * s - Math.Sin(t * 33 + 1) * 1.6 * w);
            P.FootLY = F(-26 * s); P.FootRY = F(-26 * s);
            P.CapeWind = F(startle * 18);

            // 손: 놀라 올림 → 한 손은 가슴 움켜쥐고 다른 손은 돌을 막듯 내밂 → 무릎을 짚고 버팀 → 힘 되찾으며 내림
            float by = P.BodyY;
            PointF stand = new PointF(102, 395), standR = new PointF(298, 395);
            PointF up = new PointF(84, 292), upR = new PointF(316, 292);
            PointF reach = new PointF(46, 340 + by), clutch = new PointF(214, 346 + by);
            PointF knee = new PointF(98, 470), kneeR = new PointF(302, 470);
            float a1 = F(Ease(Seg(t, .25, .65)) * (1 - Ease(Seg(t, 1.0, 1.6))));   // 놀라 올린 손
            float a2 = F(Ease(Seg(t, 1.0, 1.8)) * (1 - slump));                     // 가슴 움켜쥐기 / 막아내기
            float a3 = F(slump);
            PointF L = Mix(stand, up, a1); L = Mix(L, reach, a2); L = Mix(L, knee, a3);
            PointF R = Mix(standR, upR, a1); R = Mix(R, clutch, a2); R = Mix(R, kneeR, a3);
            float back = F(rec);
            L = Mix(L, stand, back); R = Mix(R, standR, back);
            L.X += F(shake * .9); R.X += F(shake * .9); L.Y += F(shake * .5);
            P.HandLTo = L; P.HandRTo = R;
            P.HandL = P.HandR = HandStyle.Fist;

            // 표정: 놀람 → 신음 → 안도의 미소
            P.Mouth = rec > .65 ? MouthStyle.Smile : (w > .08 || startle > .4 ? MouthStyle.Open : MouthStyle.Neutral);
            P.Brow = F(-.7 * Math.Min(1, weak * 1.4));
            P.Gy = -.1f; P.Cross = 0;
        }

        private static PointF Mix(PointF a, PointF b, float t)
        { return new PointF(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t); }
    }
}
