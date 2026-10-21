using System;
using System.Drawing;

namespace DragonBall
{
    /// <summary>
    /// 상태마다 드래곤볼의 자세(Pose)를 만든다. 발은 움직인 거리에 맞춰 걷고(미끄러지지 않음),
    /// 눈 깜빡임·시선·귀 쫑긋·꼬리 흔들림·숨쉬기는 모든 자세에 겹쳐 흐른다.
    /// </summary>
    internal sealed partial class Pet
    {
        // ───────── 공통 도구 ─────────
        private static double Ease(double x) { x = Math.Max(0, Math.Min(1, x)); return x * x * (3 - 2 * x); }
        private static float F(double v) { return (float)v; }
        private static double Tau { get { return Math.PI * 2; } }

        private double Hash01(double seg)
        { double v = Math.Sin(seg * 12.9898 + blinkOffset * 78.233) * 43758.5453; return v - Math.Floor(v); }

        // 걸음 위상(걸음 한 주기 = 발 두 번). 움직인 거리로 올려서 발이 땅에서 미끄러지지 않는다.
        private double gait, lastGaitX, lastGaitY;
        private bool gaitStarted;

        private void UpdateGait(double dt)
        {
            if (!gaitStarted) { lastGaitX = X; lastGaitY = Y; gaitStarted = true; }
            double moved = Math.Sqrt((X - lastGaitX) * (X - lastGaitX) + (Y - lastGaitY) * (Y - lastGaitY));
            lastGaitX = X; lastGaitY = Y;
            switch (State)
            {
                case PetState.Walk: case PetState.Gather: case PetState.Climb:
                case PetState.Wall: case PetState.WindowClimb: case PetState.Ceiling:
                    gait += Math.Min(moved, 130 * S * Math.Max(dt, .001)) / (74.0 * Art.K);
                    break;
                case PetState.Sliding:
                    gait += dt * 6.5;
                    break;
            }
        }

        /// <summary>눈 깜빡임 정도(1 = 활짝, 0 = 감음). 약 4초에 한 번, 가끔 두 번 연달아.</summary>
        private float BlinkOpen(double t)
        {
            double c = (t + blinkOffset) % 4.3;
            Func<double, double> shape = x => x < 0 ? 1 : x < .05 ? 1 - x / .05 : x < .09 ? 0 : x < .16 ? (x - .09) / .07 : 1;
            double o = shape(c);
            if (((int)((t + blinkOffset) / 4.3) % 3) == 0) o = Math.Min(o, shape(c - 1.1));
            return F(o);
        }

        private float Twitch(double t, double period, double seed)
        {
            double c = (t + seed) % period;
            if (c > .4) return 0;
            return F(Math.Sin(c * 36) * (1 - c / .4) * 10);
        }

        /// <summary>두리번거리는 시선: 2.4초마다 새 곳을 보고, 눈동자는 빠르게 옮겨 간다.</summary>
        private void Wander(Pose P, double t, double range)
        {
            double u = t / 2.4, seg = Math.Floor(u), e = Ease((u - seg) / .1);
            Func<int, double, double, double> pick = (salt, lo, hi) =>
            {
                double a = lo + Hash01(seg - 1 + salt) * (hi - lo), b = lo + Hash01(seg + salt) * (hi - lo);
                return a + (b - a) * e;
            };
            P.Gx = F(pick(0, -1, 1) * range);
            P.Gy = F(pick(100, -.2, .75) * Math.Min(1, range + .3));
            P.Cross = F(pick(200, .3, .85));
        }

        // ───────── 기본 서 있는 자세 ─────────
        private void IdleBase(Pose P, double t, bool lookAround)
        {
            P.Breath = F(Math.Sin(animT * 2.0));
            P.OpenL = P.OpenR = BlinkOpen(animT);
            if (lookAround) Wander(P, t, 1); else { P.Gy = .45f; P.Cross = .7f; }
            P.HeadTilt = F(P.Gx * 2.2 + Math.Sin(t * .8) * 1.2);
            P.EarL = F(Math.Sin(animT * 1.7 + blinkOffset) * 2.2); P.EarR = F(Math.Sin(animT * 1.7 + blinkOffset + 1.1) * 2.2);
            P.Tail = F(3 + Math.Sin(t * .9) * 4); P.TailWaveAmp = 9; P.TailWavePhase = F(t * 2.1);
            P.ArmLA = F(60 + Math.Sin(t * 2.0 + .4) * 1.5); P.ArmRA = F(60 + Math.Sin(t * 2.0 + .1) * 1.5);
            P.ArmLB = F(43 + Math.Sin(t * 2.0) * 2); P.ArmRB = F(43 + Math.Sin(t * 2.0 + .3) * 2);
        }

        private static void SitDown(Pose P)
        {
            P.Sit = true; P.BodyY = 34; P.Knee = 10;
            P.FootLX = -8; P.FootRX = 8; P.FootLY = 2; P.FootRY = 2; P.FootLRot = 8; P.FootRRot = 8;
        }

        // ───────── 걷기 ─────────
        /// <summary>발 한 쪽의 걸음: 땅에서 뒤로 밀고(60%) → 들어 앞으로 내딛는다(40%).</summary>
        private static void Foot(double phase, float S, float H, out float dx, out float dy, out float rot)
        {
            double th = phase - Math.Floor(phase), fwd, lift = 0, r;
            const double stance = .6;
            if (th < stance) { double u = th / stance; fwd = S * (1 - 2 * u); r = 8 - 20 * u; }
            else
            {
                double u = (th - stance) / (1 - stance), e = (1 - Math.Cos(Math.PI * u)) / 2;
                fwd = -S + 2 * S * e; lift = Math.Sin(Math.PI * u); r = -12 + 26 * Math.Sin(Math.PI * Math.Min(1, u * 1.15));
            }
            dx = F(-fwd);                // 그림은 왼쪽(−x)이 앞
            dy = F(-H * lift); rot = F(r);
        }

        private void ApplyWalk(Pose P, double g, double ampD, float S, float H, float swing, float lean)
        {
            float amp = F(ampD); if (amp <= .005f) return;
            float dx, dy, rot;
            Foot(g, S, H, out dx, out dy, out rot); P.FootLX += dx * amp; P.FootLY += dy * amp; P.FootLRot += rot * amp;
            Foot(g + .5, S, H, out dx, out dy, out rot); P.FootRX += dx * amp; P.FootRY += dy * amp; P.FootRRot += rot * amp;
            double ph = g * Tau;
            float bob = F(-Math.Abs(Math.Sin(ph)) * 3.4);
            P.BodyY += bob * amp; P.HeadY += -bob * .35f * amp;
            P.Lean += (lean + F(Math.Sin(ph) * 2.0)) * amp;
            P.BodyX += F(Math.Sin(ph) * 1.2) * amp;
            // 팔: 걸음과 반대로 번갈아 흔든다(위팔이 앞서고 아래팔이 한 박자 늦게 따라온다)
            P.ArmLA += F(Math.Sin(ph) * swing) * amp; P.ArmRA -= F(Math.Sin(ph) * swing) * amp;
            P.ArmLB += F(Math.Sin(ph - .8) * swing * 1.15) * amp; P.ArmRB -= F(Math.Sin(ph - .8) * swing * 1.15) * amp;
            P.Tail += F(Math.Sin(ph - 1.0) * 7) * amp; P.TailCurl += F(10 * amp); P.TailWaveAmp += 8 * amp; P.TailWavePhase = F(ph);
            P.EarL += F(Math.Sin(2 * ph - .8) * 4) * amp; P.EarR += F(Math.Sin(2 * ph - 1.1) * 4) * amp;
            P.HeadTilt += F(Math.Sin(ph + .5) * 1.5) * amp;
            if (amp > .35f) { P.View = 1; P.Turn = .42f * amp; P.Gx = -.25f; P.Gy = 0; P.Cross = 0; }
        }

        // ───────── 상태별 자세 ─────────
        private void SquishPose(Pose P, ref Look L)
        {
            double k = Math.Sin(Math.PI * Math.Min(1, StateT / squishDur));
            bool poke = squishAmp > .2;        // 클릭해서 눌린 것(착지·부딪힘보다 크게 눌림)
            if (poke)
            {
                // 콕 찔려 발끈: 몸을 웅크리고 주먹을 쥐며 💢
                IdleBase(P, animT, false);
                P.BodyY = F(20 * k); P.Knee = F(12 * k); P.Lean = F(5 * k);
                if (AngryType)
                {
                    P.HandL = P.HandR = HandStyle.Fist; P.ArmLA = F(60 - 22 * k); P.ArmRA = F(60 - 22 * k); P.ArmLB = F(30 - 20 * k); P.ArmRB = F(30 - 20 * k);
                    P.Brow = F(.95 * Math.Min(1, k * 2.2)); P.Mouth = MouthStyle.Frown; P.EyeScale = 1f; P.PupilR = F(.85 - .15 * k);
                    P.Cross = 0; P.Gy = 0; P.Anger = F(Math.Min(1, StateT * 3 + .15)) * (k > .05 ? 1f : 0f);
                }
                else
                {
                    // 깜짝 놀라 눈이 동그래지고 팔이 번쩍
                    P.ArmLA = F(60 + 40 * k); P.ArmRA = F(60 + 40 * k); P.ArmLB = F(43 + 46 * k); P.ArmRB = F(43 + 46 * k);
                    P.Brow = F(-.4 * k); P.Mouth = MouthStyle.Scared; P.MouthOpen = F(.6 * k); P.EyeScale = F(1 + .16 * k); P.PupilR = .7f;
                    P.Cross = 0; P.Gy = F(-.2 * k); P.Sweat = F(StateT);
                }
                P.EarL = F(-10 * k); P.EarR = F(10 * k); P.Tail = F(-14 * k); P.TailWaveAmp = 16; P.TailWavePhase = F(animT * 26);
                lookJitterX = Math.Sin(StateT * 70) * .5 * S * k;
                P.HeadY = F(4 * k);
                return;
            }
            // 착지·부딪힘: 무릎을 굽히며 팔로 균형을 잡는다
            IdleBase(P, animT, false);
            P.BodyY = F(24 * k * Math.Min(1.6, squishAmp / .15)); P.Knee = F(14 * k);
            P.FootLRot = P.FootRRot = F(4 * k);
            P.ArmLA = F(60 + 28 * k); P.ArmRA = F(60 + 28 * k); P.ArmLB = F(43 + 30 * k); P.ArmRB = F(43 + 30 * k);
            P.OpenL = P.OpenR = F(1 - .7 * k * Math.Min(1, StateT * 8));
            P.EarL = F(10 * k); P.EarR = F(-10 * k);
            P.Tail = F(-10 * k);
            P.Mouth = k > .4 ? MouthStyle.Pout : MouthStyle.Neutral;
        }

        private void ThrownPose(Pose P)
        {
            // 팔다리를 쫙 벌리고 날아간다
            double sh = Math.Sin(StateT * 22);
            P.Breath = 0; P.OpenL = P.OpenR = 1; P.EyeScale = 1.08f; P.PupilR = .7f; P.Gy = -.3f; P.Cross = 0; P.Gx = 0;
            P.ArmLA = F(128 + sh * 8); P.ArmRA = F(128 - sh * 8); P.ArmLB = F(150 + sh * 10); P.ArmRB = F(150 - sh * 10);
            P.FootLX = -16; P.FootRX = 16; P.FootLY = -12; P.FootRY = -10; P.FootLRot = -8; P.FootRRot = -8; P.Knee = 6;
            P.Mouth = MouthStyle.Open; P.MouthOpen = F(.55 + sh * .1);
            P.EarL = F(-26 + sh * 6); P.EarR = F(26 - sh * 6);
            P.Tail = F(-20 + sh * 6); P.TailCurl = -16; P.TailWaveAmp = 18; P.TailWavePhase = F(StateT * 24);
            P.Brow = -.3f; P.Sweat = F(StateT);
        }

        private void FallingPose(Pose P)
        {
            // 프로펠러 아래 매달려 천천히 흔들리며 내려온다: 팔은 균형 잡듯 벌리고 다리는 달랑달랑
            double t = StateT, sway = Math.Sin(t * 2.4);
            P.Breath = F(Math.Sin(t * 3));
            P.OpenL = P.OpenR = BlinkOpen(t); P.Gy = -.95f; P.Gx = F(Math.Sin(t * 1.3) * .2); P.Cross = .15f;
            P.PupilR = .85f;
            P.ArmLA = F(78 + sway * 6); P.ArmRA = F(78 - sway * 6); P.ArmLB = F(70 + sway * 8); P.ArmRB = F(70 - sway * 8);
            P.FootLX = F(-3 - sway * 3); P.FootRX = F(3 - sway * 3); P.FootLY = F(-9 + Math.Sin(t * 3.1) * 3); P.FootRY = F(-9 + Math.Sin(t * 3.1 + 1.3) * 3);
            P.FootLRot = F(-14 + sway * 5); P.FootRRot = F(-14 - sway * 5); P.Knee = 3;
            double flutter = Math.Sin(t * 30);
            P.EarL = F(-4 + flutter * 5); P.EarR = F(4 - flutter * 5);
            P.Tail = F(-10 + sway * 8); P.TailWaveAmp = 20; P.TailWavePhase = F(t * 9); P.TailCurl = 12;
            P.Mouth = MouthStyle.Neutral; P.MouthOpen = .3f;
            // 기를 모아 둥실 떠 내려온다: 오라(어린 오공은 근두운)
            int aura = Who == Persona.Vegeta ? 3 : Who == Persona.Piccolo ? 5 : 1;
            if (Who == Persona.Kid) P.Nimbus = F(Math.Min(1, t * 4));
            else { P.Aura = F(.55 + .1 * Math.Sin(t * 5)); P.AuraKind = aura; P.AuraT = F(t); }
            if (IsPowerFlight)
            {
                // 기 폭발: 변신하며 천장까지 솟구친다
                P.Super = 1; P.Aura = 1; P.AuraKind = 2; P.AuraT = F(t); P.Spark = .6f; P.Muscle = 1; P.Nimbus = 0;
                P.Mouth = MouthStyle.Smile; P.Gy = -.2f; P.Gx = 0; P.Cross = 0; P.Brow = .5f;
                P.ArmLA = F(40); P.ArmRA = F(40); P.ArmLB = 20; P.ArmRB = 20; P.HandL = P.HandR = HandStyle.Fist;
            }
        }

        private void DragPose(Pose P)
        {
            // 들려 올라가서 허우적: 흔들리는 방향의 반대로 팔다리가 늘어지고 발을 버둥거린다
            double sw = Math.Max(-1, Math.Min(1, angle / 30)), t = animT;
            double kick = Math.Sin(t * 7.5);
            P.Breath = F(Math.Sin(t * 5));
            P.OpenL = P.OpenR = BlinkOpen(t); P.EyeScale = 1.06f; P.PupilR = .75f; P.Gy = -.35f; P.Cross = .1f; P.Gx = F(-sw * .6);
            P.ArmLA = F(105 + kick * 14 + sw * 14); P.ArmRA = F(105 - kick * 14 - sw * 14);
            P.ArmLB = F(130 + kick * 18); P.ArmRB = F(130 - kick * 18);
            P.FootLX = F(-5 + kick * 7 - sw * 3); P.FootRX = F(5 - kick * 7 - sw * 3);
            P.FootLY = F(-14 - kick * 3); P.FootRY = F(-14 + kick * 3); P.FootLRot = F(-16 + kick * 6); P.FootRRot = F(-16 - kick * 6); P.Knee = 4;
            P.Mouth = MouthStyle.Scared; P.MouthOpen = F(.6 + .3 * Math.Sin(t * 9));
            P.EarL = F(-14 - sw * 10 + Math.Sin(t * 11) * 5); P.EarR = F(14 - sw * 10 - Math.Sin(t * 11) * 5);
            P.Tail = F(sw * 28); P.TailCurl = F(-sw * 26); P.TailWaveAmp = 14; P.TailWavePhase = F(t * 8);
            P.Sweat = F(t);
        }

        private void SleepPose(Pose P, double t)
        {
            SitDown(P);
            P.Eyes = EyeStyle.Sleep; P.OpenL = P.OpenR = 0;
            double nod = Math.Sin(t * 1.15);
            P.Breath = F(Math.Sin(t * 1.7) * 1.4);
            P.HeadTilt = F(7 + nod * 3.5); P.HeadY = F(9 + nod * 2); P.HeadX = F(-3);
            P.Mouth = MouthStyle.Neutral; P.Gy = 0;
            P.EarL = F(-6 + Math.Sin(t * 1.15) * 1.5); P.EarR = F(6 + Math.Sin(t * 1.15) * 1.5);
            P.ArmsFront = true; P.ArmLA = 24; P.ArmRA = 24; P.Reach = Reach.Lap;
            P.Tail = F(24 + Math.Sin(t * 1.7) * 2); P.TailCurl = 36; P.TailWaveAmp = 3; P.TailWavePhase = F(t * 1.7);
            P.Flush = .35f;
        }

        private void StretchPose(Pose P, double q)
        {
            // q: 0..1..0 로 폈다가 접는 정도. 팔을 쭉 올리고 하품하며 몸을 뒤로 젖힌다.
            IdleBase(P, animT, false);
            P.Eyes = q > .25 ? EyeStyle.Sleep : EyeStyle.Open; P.OpenL = P.OpenR = q > .25 ? 0 : 1;
            P.ArmLA = F(60 + 118 * q); P.ArmRA = F(60 + 118 * q); P.ArmLB = F(43 + 134 * q); P.ArmRB = F(43 + 134 * q);
            P.Lean = F(7 * q); P.HeadTilt = F(9 * q); P.BodyY = F(-4 * q); P.HeadY = F(-2 * q);
            P.FootLY = P.FootRY = F(-5 * q); P.FootLRot = P.FootRRot = F(-18 * q);
            P.Mouth = MouthStyle.Open; P.MouthOpen = F(Math.Max(0, q - .15) * 1.1); if (q < .2) P.Mouth = MouthStyle.Neutral;
            P.Tail = F(-24 * q); P.TailCurl = F(-20 * q); P.EarL = F(14 * q); P.EarR = F(-14 * q);
            if (q > .6) { P.ArmLA += F(Math.Sin(animT * 30) * 1.5); P.ArmRA -= F(Math.Sin(animT * 30) * 1.5); }
        }

        private void DaydreamPose(Pose P, double t)
        {
            SitDown(P);
            P.Breath = F(Math.Sin(t * 1.1) * .6);
            P.OpenL = P.OpenR = BlinkOpen(t * .6);
            P.Gx = F(Math.Sin(t * .45) * .55); P.Gy = F(-.55 + Math.Sin(t * .31) * .2); P.Cross = .2f;
            P.HeadTilt = F(-4 + Math.Sin(t * .7) * 3); P.HeadY = F(-2);
            P.Mouth = MouthStyle.Pout; P.MouthOpen = .3f;
            P.EarL = Twitch(t, 6.2, 1.3) * .6f; P.EarR = Twitch(t, 8.4, 4.1) * .6f + F(Math.Sin(t * .9) * 2);
            P.Tail = F(14 + Math.Sin(t * .8) * 8); P.TailCurl = 22; P.TailWaveAmp = 7; P.TailWavePhase = F(t * 1.6);
            P.ArmLA = F(48 + Math.Sin(t * .6) * 3); P.ArmRA = F(48 - Math.Sin(t * .6) * 3); P.ArmLB = 36; P.ArmRB = 36;
        }

        private void SitIdlePose(Pose P, double t)
        {
            SitDown(P);
            IdleBase(P, t, true);
            P.Sit = true; P.BodyY = 34;
            P.ArmLA = F(46 + Math.Sin(t * 2.0) * 1.5); P.ArmRA = F(46 + Math.Sin(t * 2.0 + .3) * 1.5); P.ArmLB = 30; P.ArmRB = 30;
            P.Tail = F(16 + Math.Sin(t * 1.1) * 6); P.TailCurl = 26; P.TailWaveAmp = 6; P.TailWavePhase = F(t * 1.9);
            // 가끔 한 번 크게 하품
            double yc = (animT + blinkOffset * 2) % 11;
            if (yc < 1.7)
            {
                double q = Math.Sin(Math.PI * yc / 1.7);
                P.Mouth = MouthStyle.Open; P.MouthOpen = F(q * .9); P.Eyes = q > .5 ? EyeStyle.Sleep : EyeStyle.Open; P.OpenL = P.OpenR = q > .5 ? 0 : 1;
                P.HeadTilt = F(P.HeadTilt - 5 * q); P.ArmLA += F(30 * q); P.ArmRA += F(30 * q);
            }
        }

        private void EatPose(Pose P)
        {
            SitDown(P);
            double chew = Math.Sin(animT * 2.4 * Tau), prog = Math.Min(1, StateT / Math.Max(1, extraDuration));
            P.Breath = F(chew * .3);
            P.Eyes = EyeStyle.Happy; P.OpenL = P.OpenR = 0; P.Mouth = MouthStyle.Neutral;
            P.HeadY = F(1.5 + chew * 1.6); P.HeadTilt = F(chew * 1.4);
            P.ArmsFront = true; P.Reach = Reach.Mouth;
            P.Cookie = F(prog); P.Flush = .7f;
            P.EarL = F(-3 + chew * 3); P.EarR = F(3 - chew * 3);
            P.Tail = F(14 + Math.Sin(animT * 3.4) * 9); P.TailCurl = 24; P.TailWaveAmp = 9; P.TailWavePhase = F(animT * 6);
        }

        private void HungryCryPose(Pose P, double t)
        {
            SitDown(P);
            double sob = Math.Sin(t * 9);
            P.Breath = F(Math.Sin(t * 5) * .8);
            P.OpenL = P.OpenR = 1; P.EyeScale = 1.04f; P.PupilR = .9f; P.Gy = .5f; P.Cross = .5f; P.Gx = 0;
            P.Brow = -.85f; P.Mouth = MouthStyle.Frown; P.MouthOpen = 0;
            P.HeadTilt = F(-4 + sob * 1.2); P.HeadY = F(5 + Math.Abs(sob) * 1.5);
            P.ArmsFront = true; P.ArmLA = 30; P.ArmRA = 30; P.Reach = Reach.Belly;
            P.Tears = F(t); P.EarL = F(-16 + sob * 2); P.EarR = F(16 - sob * 2);
            P.Tail = F(30); P.TailCurl = 12; P.TailWaveAmp = 3; P.TailWavePhase = F(t * 4);
            lookJitterX = Math.Sin(t * 21) * .45 * S;
        }

        private void TouchCryPose(Pose P)
        {
            // 기준 그림 2: 놀라 눈이 커지고 눈동자가 위로 올라가며 손을 모아 쥔다. 달달 떨며 운다.
            double t = StateT, shiver = Math.Sin(t * 52);
            P.Breath = F(Math.Sin(t * 14) * .8);
            P.OpenL = P.OpenR = F(t < .12 ? 1 : BlinkOpen(t + 1.7)); P.EyeScale = 1.1f; P.PupilR = .62f; P.Gy = -.78f; P.Cross = .05f; P.Gx = F(Math.Sin(t * 3.1) * .25);
            P.Brow = -.7f; P.Mouth = MouthStyle.Scared; P.MouthOpen = F(.5 + .5 * Math.Sin(t * 18));
            P.Clasp = true; P.ArmsFront = true;
            P.EarL = F(-14 + shiver * 2); P.EarR = F(14 - shiver * 2);
            P.HeadTilt = F(-3 + Math.Sin(t * 9) * 1.6); P.HeadY = F(4); P.BodyY = F(5 + Math.Abs(shiver) * .6);
            P.Knee = 5; P.FootLRot = P.FootRRot = 5;
            P.Tears = F(t); P.Sweat = F(t);
            P.Tail = F(26); P.TailCurl = 16; P.TailWaveAmp = 6; P.TailWavePhase = F(t * 30);
            if (t < .18) { double q = Math.Sin(t / .18 * Math.PI); P.BodyY += F(7 * q); P.EyeScale = F(1.1 + .08 * q); }
        }

        private void HopPose(Pose P)
        {
            double t = StateT / extraDuration;
            IdleBase(P, animT, false);
            P.Eyes = EyeStyle.Happy; P.OpenL = P.OpenR = 0; P.Mouth = MouthStyle.Smile;
            if (t < .16)
            {
                double q = Math.Sin(t / .16 * Math.PI);                     // 웅크리기
                P.BodyY = F(24 * q); P.Knee = F(14 * q); P.ArmLA = F(60 - 24 * q); P.ArmRA = F(60 - 24 * q); P.ArmLB = F(30); P.ArmRB = F(30);
                P.Eyes = EyeStyle.Open; P.OpenL = P.OpenR = 1; P.Mouth = MouthStyle.Neutral; P.Brow = F(.4 * q);
            }
            else if (t < .84)
            {
                double q = (t - .16) / .68, up = Math.Sin(q * Math.PI);     // 공중: 팔을 번쩍
                P.ArmLA = F(60 + 100 * up); P.ArmRA = F(60 + 100 * up); P.ArmLB = F(43 + 120 * up); P.ArmRB = F(43 + 120 * up);
                P.FootLY = F(-12 * up); P.FootRY = F(-12 * up); P.FootLRot = P.FootRRot = F(-14 * up); P.FootLX = F(-5 * up); P.FootRX = F(5 * up);
                P.EarL = F(10 * up); P.EarR = F(-10 * up); P.Tail = F(-18 * up);
                P.Mouth = MouthStyle.Open; P.MouthOpen = F(.55 * up); P.Lean = F(-3 * Math.Sin(q * Math.PI * 2));
            }
            else
            {
                double q = Math.Sin((t - .84) / .16 * Math.PI);
                P.BodyY = F(22 * q); P.Knee = F(12 * q); P.ArmLA = F(60 + 20 * q); P.ArmRA = F(60 + 20 * q);
            }
        }

        private void CheerPose(Pose P, double now)
        {
            IdleBase(P, animT, false);
            P.Eyes = EyeStyle.Happy; P.OpenL = P.OpenR = 0; P.Mouth = MouthStyle.Open; P.MouthOpen = .5f; P.Flush = .5f;
            if (now < cheerHopUntil)
            {
                double q = StateT % .42 / .42, up = Math.Sin(Math.PI * q);
                P.ArmLA = F(60 + 105 * up); P.ArmRA = F(60 + 105 * up); P.ArmLB = F(43 + 125 * up); P.ArmRB = F(43 + 125 * up);
                P.FootLY = F(-9 * up); P.FootRY = F(-9 * up); P.FootLRot = P.FootRRot = F(-12 * up);
                P.BodyY = F(q < .12 ? 12 * (1 - q / .12) : 0);
                P.EarL = F(8 * up); P.EarR = F(-8 * up); P.Tail = F(-14 * up);
                P.Lean = F(Math.Sin(q * Tau) * 2.5);
            }
            else { P.ArmLA = 120; P.ArmRA = 120; P.ArmLB = 150; P.ArmRB = 150; P.Eyes = EyeStyle.Happy; }
        }

        private void DashPose(Pose P)
        {
            // 도망: 옆모습, 앞으로 숙이고 다리를 빠르게 구르며 혀를 내민다(기준 그림 4)
            double g = gait, ph = g * Tau, t = animT;
            P.View = 1; P.Breath = 0;
            P.Lean = F(-12 + Math.Sin(ph) * 1.5); P.BodyY = F(-Math.Abs(Math.Sin(ph)) * 3 - 3);
            float dx, dy, rot;
            Foot(g, 30, 22, out dx, out dy, out rot); P.FootLX = dx; P.FootLY = dy; P.FootLRot = rot;
            Foot(g + .5, 30, 22, out dx, out dy, out rot); P.FootRX = dx; P.FootRY = dy; P.FootRRot = rot;
            P.ArmLA = F(120 + Math.Sin(ph) * 22); P.ArmRA = F(120 - Math.Sin(ph) * 22); P.ArmLB = F(142 + Math.Sin(ph - .6) * 24); P.ArmRB = F(142 - Math.Sin(ph - .6) * 24);
            P.Eyes = EyeStyle.Open; P.OpenL = P.OpenR = 1; P.EyeScale = 1.04f; P.Gx = -.9f; P.Gy = 0; P.Cross = 0; P.PupilR = .85f;
            P.Mouth = MouthStyle.Open; P.MouthOpen = F(.35 + Math.Sin(ph * 2) * .06); P.Turn = .85f;
            P.Aura = .35f; P.AuraKind = Who == Persona.Vegeta ? 3 : Who == Persona.Piccolo ? 5 : 1; P.AuraT = F(t);
            P.EarL = F(26 + Math.Sin(ph * 2) * 5); P.EarR = F(26 + Math.Sin(ph * 2 + .5) * 5);
            P.Tail = F(-8 + Math.Sin(ph - 1) * 6); P.TailCurl = -6; P.TailWaveAmp = 10; P.TailWavePhase = F(ph * 2);
            P.HeadTilt = F(-4); P.HeadX = -4; P.Sweat = F(t);
        }

        private void ClimbBody(Pose P, bool descending)
        {
            // 벽·천장·창을 타는 자세: 옆모습으로 손발을 번갈아 뻗는다(몸 전체를 눕히는 회전은 Look 이 담당)
            IdleBase(P, animT, false);
            P.View = 1; P.Gx = -.3f; P.Gy = 0; P.Cross = 0; P.PupilR = .9f;
            ApplyWalk(P, gait, 1f, 24, 20, 30, 0);
            P.ArmLA += 30; P.ArmRA += 30;
            P.Mouth = MouthStyle.Neutral;
            P.Tail = F(P.Tail + 6); P.Brow = F(.25);
            if (descending) P.Brow = 0;
        }

        private void DizzyPose(Pose P)
        {
            double t = StateT, sw = Math.Sin(t * 5.2);
            P.Eyes = EyeStyle.Spiral; P.SpiralT = F(t * 11);
            P.Breath = F(Math.Sin(t * 4));
            P.BodyX = F(sw * 7); P.Lean = F(sw * 9); P.HeadTilt = F(-sw * 7 + Math.Sin(t * 3) * 3); P.HeadX = F(sw * 3);
            P.ArmLA = F(95 + Math.Sin(t * 5.2 + 1) * 28); P.ArmRA = F(95 + Math.Sin(t * 5.2 + 3.1) * 28);
            P.ArmLB = F(110 + Math.Sin(t * 5.2) * 20); P.ArmRB = F(110 - Math.Sin(t * 5.2) * 20);
            float dx, dy, rot;
            Foot(t * 1.3, 10, 7, out dx, out dy, out rot); P.FootLX = dx; P.FootLY = dy; P.FootLRot = rot * .4f;
            Foot(t * 1.3 + .5, 10, 7, out dx, out dy, out rot); P.FootRX = dx; P.FootRY = dy; P.FootRRot = rot * .4f;
            P.Mouth = MouthStyle.Pout; P.MouthOpen = F(.5 + .4 * Math.Sin(t * 6));
            P.EarL = F(Math.Sin(t * 5.2 + .6) * 9); P.EarR = F(-Math.Sin(t * 5.2 + .6) * 9);
            P.Tail = F(-sw * 20); P.TailWaveAmp = 14; P.TailWavePhase = F(t * 8);
        }

        private void DancePose(Pose P, double phase)
        {
            double beat = phase / .6, hop = Math.Abs(Math.Sin(phase * Math.PI * 2.5)), sw = Math.Sin(phase * Math.PI * 2.5);
            IdleBase(P, animT, false);
            P.Eyes = EyeStyle.Happy; P.OpenL = P.OpenR = 0; P.Mouth = MouthStyle.Open; P.MouthOpen = F(.45 + .2 * hop); P.Flush = .6f;
            bool leftUp = ((int)(phase / .3)) % 2 == 0;
            P.ArmLA = leftUp ? 150f : 38f; P.ArmRA = leftUp ? 38f : 150f; P.ArmLB = leftUp ? 168f : 25f; P.ArmRB = leftUp ? 25f : 168f;
            P.Lean = F(sw * 6); P.HeadTilt = F(sw * 8); P.BodyY = F(-hop * 2);
            P.FootLY = F(-hop * 10 * (leftUp ? 1 : .3)); P.FootRY = F(-hop * 10 * (leftUp ? .3 : 1)); P.FootLRot = P.FootRRot = F(-10 * hop);
            P.EarL = F(hop * 10); P.EarR = F(-hop * 10); P.Tail = F(sw * 22); P.TailWaveAmp = 14; P.TailWavePhase = F(phase * 14);
        }

        private void FlowerPose(Pose P, double now)
        {
            double t = now - flowerStarted;
            IdleBase(P, animT, false);
            P.Eyes = EyeStyle.Happy; P.OpenL = P.OpenR = 0; P.Mouth = MouthStyle.Smile; P.Flush = .8f;
            P.ArmsFront = true; P.ArmLA = 30; P.ArmRA = 30; P.Reach = Reach.Belly;
            P.HeadTilt = F(Math.Sin(t * 2.2) * 3.5); P.BodyY = F(-Math.Abs(Math.Sin(t * 3.2)) * 2.5);
            P.EarL = F(Math.Sin(t * 3.2) * 4); P.EarR = F(-Math.Sin(t * 3.2) * 4);
            P.Tail = F(Math.Sin(t * 4) * 10); P.TailWaveAmp = 16; P.TailWavePhase = F(t * 8);
        }

        private void WaitPose(Pose P)
        {
            // 퇴근 시간을 기다리며 두리번, 발끝으로 톡톡
            IdleBase(P, animT, true);
            double tap = Math.Max(0, Math.Sin(animT * 6.5)) * (Math.Sin(animT * .9) > 0 ? 1 : 0);
            P.FootRY = F(-5 * tap); P.FootRRot = F(10 * tap);
            P.Mouth = MouthStyle.Smile;
        }
    }
}
