using System;
using System.Collections.Generic;
using System.Drawing;

namespace DragonBall
{
    /// <summary>시그니처 기술 하나의 이름·설명·길이.</summary>
    internal sealed class SigDef
    {
        public string Name, Desc; public double Dur;
        public SigDef(string name, double dur, string desc) { Name = name; Dur = dur; Desc = desc; }
        /// <summary>메뉴·목록에서 일반 동작과 구분되는 ★ 표시가 붙은 이름.</summary>
        public string Label { get { return "★ " + Name; } }
    }

    /// <summary>캐릭터마다 3가지씩, 모두 15가지의 시그니처 기술.</summary>
    internal static class Signatures
    {
        internal static readonly SigDef[][] All =
        {
            new[] // 손오공
            {
                new SigDef("가메하메파", 7.8, "두 손에 푸른 기를 모아 한 번에 쏘아 보내는 대표 기술"),
                new SigDef("순간이동", 5.6, "이마에 두 손가락을 대고 슉! 사라졌다가 다른 곳에 나타남"),
                new SigDef("초사이어인 변신", 8.0, "고함과 함께 머리카락이 금빛으로 변하며 번개·오라가 폭발"),
            },
            new[] // 어린 오공
            {
                new SigDef("근두운 타기", 8.0, "근두운을 불러 올라타고 둥실둥실 하늘을 가로지름"),
                new SigDef("여의봉 휘두르기", 7.5, "여의봉을 빙글 돌리다가 하늘 높이 쭉 늘려 내리침"),
                new SigDef("보름달 대원숭이", 9.0, "보름달을 보고 거대한 원숭이로 변신해 가슴을 쿵쿵"),
            },
            new[] // 무천도사
            {
                new SigDef("원조 가메하메파", 8.0, "가메하메파의 원조! 큰 광선을 쏘고 허리를 부여잡음"),
                new SigDef("MAX 파워", 7.0, "숨을 들이마셔 온몸의 근육을 부풀리는 전성기 모습"),
                new SigDef("봉마파 (전기밥솥)", 8.5, "전기밥솥을 꺼내 사악한 기운을 빨아들여 봉인"),
            },
            new[] // 베지터
            {
                new SigDef("빅뱅 어택", 6.5, "손바닥에 기를 모아 거대한 기탄을 날려 폭발시킴"),
                new SigDef("파이널 플래시", 8.0, "두 손을 앞으로 뻗어 눈부신 황금빛 광선을 발사"),
                new SigDef("왕자의 오라", 7.0, "팔짱을 낀 채 떠올라 푸른 오라를 터뜨리는 사이어인 왕자"),
            },
            new[] // 피콜로
            {
                new SigDef("마관광살포", 7.8, "이마에 두 손가락을 대고 기를 모아 꼬인 광선을 발사"),
                new SigDef("팔 늘리기", 6.5, "팔을 고무줄처럼 쭉쭉 늘려 멀리 있는 적을 때림"),
                new SigDef("명상 공중부양", 9.0, "책상다리로 둥실 떠올라 명상하다 눈을 번쩍 뜸"),
            },
        };

        internal static SigDef Get(Persona who, int idx) { return All[(int)who][Math.Max(0, Math.Min(2, idx))]; }

        private static Tuple<double, string> S(double t, string text) { return Tuple.Create(t, text); }
        /// <summary>시간대별 외침(말풍선 두 번째 줄).</summary>
        internal static string Shout(Persona who, int idx, double t)
        {
            Tuple<double, string>[] line;
            switch ((int)who * 3 + idx)
            {
                case 0: line = new[] { S(0, "카~"), S(1.0, "카~메~"), S(1.9, "카~메~하~메~"), S(2.7, "카~메~하~메~…"), S(3.2, "파~~~!!"), S(5.8, "후~"), S(6.8, "헤헤") }; break;
                case 1: line = new[] { S(0, "집중…"), S(1.4, "슉!"), S(2.0, ""), S(3.4, "짠!"), S(4.2, "다녀왔어!") }; break;
                case 2: line = new[] { S(0, "으으으…"), S(1.0, "우오오오오!!"), S(3.1, "하앗!!"), S(3.7, "초사이어인이다!"), S(7.0, "휴~") }; break;
                case 3: line = new[] { S(0, "이얍!"), S(1.1, "근두운~!"), S(2.4, "야호~!"), S(5.5, "신난다~!"), S(6.8, "안녕~!") }; break;
                case 4: line = new[] { S(0, "여의봉!"), S(1.0, "얍! 얍!"), S(3.0, "늘어나라~!"), S(4.6, "에잇!!"), S(6.0, "원위치~") }; break;
                case 5: line = new[] { S(0, "어? 보름달이다…"), S(1.2, "우… 우오오…!"), S(3.2, "우오오오오!!"), S(7.4, "어라? 돌아왔네…") }; break;
                case 6: line = new[] { S(0, "카~"), S(1.2, "카~메~"), S(2.2, "카~메~하~메~…"), S(3.4, "파~~~!!!"), S(6.2, "허리야…") }; break;
                case 7: line = new[] { S(0, "허리야…"), S(1.0, "으으으…"), S(3.0, "MAX 파워!!"), S(5.4, "…"), S(6.2, "콜록콜록") }; break;
                case 8: line = new[] { S(0, "봉마파~"), S(2.2, "봉마파!!"), S(5.2, "탁!"), S(5.8, "휴…"), S(7.0, "헉헉…") }; break;
                case 9: line = new[] { S(0, "흥…"), S(0.8, "빅~뱅~"), S(2.8, "어택!!"), S(4.5, "흥, 별거 아니군.") }; break;
                case 10: line = new[] { S(0, "파이널~"), S(2.2, "파이널~ 플래시!!"), S(3.4, "하아아아!!!"), S(6.2, "하아… 하아…") }; break;
                case 11: line = new[] { S(0, "흐흥…"), S(1.6, "이 몸은…"), S(3.0, "사이어인의 왕자,"), S(4.2, "베지터님이시다!"), S(5.4, "덤벼라!") }; break;
                case 12: line = new[] { S(0, "마~"), S(1.2, "마~관~"), S(2.4, "마~관~광~"), S(3.6, "살~포!!"), S(6.2, "흥…") }; break;
                case 13: line = new[] { S(0, "…"), S(0.9, "늘어나라!"), S(1.8, "하앗!!"), S(3.5, "이것도 받아라!"), S(5.2, "흥…") }; break;
                default: line = new[] { S(0, "…"), S(1.4, "(명상 중)"), S(6.5, "!!"), S(7.2, "흠.") }; break;
            }
            string cur = "";
            foreach (var l in line) if (t >= l.Item1) cur = l.Item2;
            return cur;
        }
    }

    /// <summary>시그니처 기술(★)의 상태 관리와 15가지 자세.</summary>
    internal sealed partial class Pet
    {
        private int sigIndex;
        private bool sigTeleported;
        private double sigStartX;

        internal bool IsSignatureActive { get { return State == PetState.Resting && restPose == RestPose.Sig; } }
        internal int CurrentSignature { get { return sigIndex; } }

        internal void StartSignature(IWorld w, int idx)
        {
            sigIndex = Math.Max(0, Math.Min(2, idx)); sigTeleported = false; sigStartX = X;
            restPose = lastRestPose = RestPose.Sig;
            extraDuration = Signatures.Get(Who, sigIndex).Dur;
            mouseGrace = w.Now + extraDuration + .8; hoverSince = -1;
            Bubble = null;
            SetState(PetState.Resting, w);
            FriendData.Current.Like(2);
        }

        private void EndSignature(IWorld w) { sigTeleported = false; }

        /// <summary>시그니처 도중의 이동(순간이동 · 근두운 비행).</summary>
        private void AdvanceSignature(IWorld w, double dt)
        {
            double t = StateT;
            if (PlayOptions.Current.PositionFixed) return;
            if (Who == Persona.Goku && sigIndex == 1 && !sigTeleported && t >= 2.05)
            {
                sigTeleported = true;
                double half = Art.W * .6;
                double lo = Area.Left + half, hi = Area.Right - half;
                double nx = lo + w.Rng.NextDouble() * Math.Max(1, hi - lo);
                if (Math.Abs(nx - X) < Art.W * 3) nx = X < (lo + hi) / 2 ? Math.Min(hi, X + Art.W * 5) : Math.Max(lo, X - Art.W * 5);
                Dir = nx < X ? -1 : 1; X = nx; SupportWindow = IntPtr.Zero;
                Y = Area.Bottom;
            }
            if (Who == Persona.Kid && sigIndex == 0 && t > 1.0 && t < 6.6)
            {
                double half = Art.W * .6;
                X += Dir * 46 * S * dt * Math.Min(1, (t - 1.0) / .6);
                if (X < Area.Left + half) { X = Area.Left + half; Dir = 1; }
                if (X > Area.Right - half) { X = Area.Right - half; Dir = -1; }
            }
        }

        // ───────── 시간 구간 도구 ─────────
        private static double Seg(double t, double a, double b) { return t <= a ? 0 : t >= b ? 1 : (t - a) / (b - a); }
        private static double ESeg(double t, double a, double b) { return Ease(Seg(t, a, b)); }

        private void SetAura(Pose P, double amount, int kind, double t) { P.Aura = F(Math.Max(0, Math.Min(1, amount))); P.AuraKind = kind; P.AuraT = F(t); }

        /// <summary>기 모으는 자세: 몸을 옆으로 돌리고 무릎을 굽혀 발을 벌린다(k: 0..1).</summary>
        private void SigStance(Pose P, double k, double spread = 1)
        {
            P.Turn = F(k); P.View = k > .25 ? 1 : 0;
            P.BodyY = F(20 * k); P.Knee = F(18 * k);
            P.FootLX = F(-24 * k * spread); P.FootRX = F(30 * k * spread); P.FootLRot = F(10 * k); P.FootRRot = F(-8 * k);
            P.Lean = F(-4 * k); P.Breath = 0;
        }

        private void HairBlow(Pose P, double t, double amp)
        { P.EarL = F(Math.Sin(t * 19) * amp + 4); P.EarR = F(Math.Sin(t * 19 + 1.3) * amp + 4); }

        private void FaceStrain(Pose P, double open, double brow)
        { P.Eyes = EyeStyle.Open; P.OpenL = P.OpenR = 1; P.Brow = F(brow); P.Mouth = MouthStyle.Open; P.MouthOpen = F(open); P.Cross = 0; P.Gy = 0; P.Gx = -.4f; }

        // ───────── 15가지 시그니처 ─────────
        private void SigPose(Pose P, ref Look L, double t)
        {
            IdleBase(P, animT, false);
            switch ((int)Who * 3 + sigIndex)
            {
                case 0: BeamPose(P, ref L, t, CfgKame); break;
                case 1: GokuTeleport(P, ref L, t); break;
                case 2: GokuSuper(P, ref L, t); break;
                case 3: KidNimbus(P, ref L, t); break;
                case 4: KidPole(P, ref L, t); break;
                case 5: KidApe(P, ref L, t); break;
                case 6: BeamPose(P, ref L, t, CfgRoshiKame); break;
                case 7: RoshiMax(P, ref L, t); break;
                case 8: RoshiMafuba(P, ref L, t); break;
                case 9: VegetaBigBang(P, ref L, t); break;
                case 10: BeamPose(P, ref L, t, CfgFlash); break;
                case 11: VegetaPrince(P, ref L, t); break;
                case 12: BeamPose(P, ref L, t, CfgBeamCannon); break;
                case 13: PiccoloStretch(P, ref L, t); break;
                default: PiccoloMeditate(P, ref L, t); break;
            }
            L.Decor |= Decor.SigStars; L.DecorPhase = F(t);
        }

        /// <summary>시그니처 중 말풍선: ★ 기술 이름 + 시간대별 외침.</summary>
        private void SigBubble(ref Look L)
        {
            var d = Signatures.Get(Who, sigIndex);
            string shout = Signatures.Shout(Who, sigIndex, StateT);
            L.Bubble = "★ " + d.Name + " ★" + (string.IsNullOrEmpty(shout) ? "" : "\n" + shout);
            if (!string.IsNullOrEmpty(hourlyBubble)) L.Bubble = hourlyBubble + "\n" + L.Bubble;
        }

        // ── 광선 기술 공통(가메하메파·원조 가메하메파·파이널 플래시·마관광살포) ──
        private sealed class BeamCfg
        {
            public double ChargeFrom = .8, FireAt = 3.2, HoldTo = 5.8, EndAt = 6.6;
            public KiKind Kind = KiKind.Blue; public int Aura = 1;
            public Reach ChargeReach = Reach.Cup, FireReach = Reach.Front;
            public HandStyle ChargeHand = HandStyle.Open, FireHand = HandStyle.Open;
            public float KiMax = .85f, BeamW = .55f, MaxLen = 1700, KiPulse = .05f;
            public int Recover;         // 0 헉헉(무릎 짚기) · 1 허리를 부여잡고 콜록 · 2 팔짱 흐뭇
            public bool Gold;           // 금빛 오라
        }
        private static readonly BeamCfg CfgKame = new BeamCfg { Kind = KiKind.Blue, Aura = 1, Recover = 0 };
        private static readonly BeamCfg CfgRoshiKame = new BeamCfg { ChargeFrom = 1.0, FireAt = 3.6, HoldTo = 6.0, EndAt = 6.8, Kind = KiKind.Blue, Aura = 1, KiMax = 1f, BeamW = .85f, MaxLen = 1900, Recover = 1 };
        private static readonly BeamCfg CfgFlash = new BeamCfg { ChargeFrom = .8, FireAt = 3.6, HoldTo = 6.2, EndAt = 7.0, Kind = KiKind.Yellow, Aura = 2, ChargeReach = Reach.Front, FireReach = Reach.Front, KiMax = .9f, BeamW = 1.25f, MaxLen = 1900, Recover = 0 };
        private static readonly BeamCfg CfgBeamCannon = new BeamCfg { ChargeFrom = .8, FireAt = 3.7, HoldTo = 6.0, EndAt = 6.8, Kind = KiKind.Spiral, Aura = 5, ChargeReach = Reach.Forehead, FireReach = Reach.Front, ChargeHand = HandStyle.Point, FireHand = HandStyle.Point, KiMax = .8f, BeamW = .5f, MaxLen = 1800, Recover = 2 };

        private void BeamPose(Pose P, ref Look L, double t, BeamCfg c)
        {
            double dur = extraDuration;
            double stance = ESeg(t, 0, .8) * (1 - ESeg(t, c.EndAt, c.EndAt + .8));
            SigStance(P, stance);
            P.KiKind = (int)c.Kind; P.BeamT = F(t);
            if (t < c.FireAt)
            {
                double ch = ESeg(t, c.ChargeFrom, c.FireAt - .35), shake = Math.Sin(t * 52);
                P.Reach = t < c.ChargeFrom ? Reach.Guard : c.ChargeReach;
                P.HandL = P.HandR = t < c.ChargeFrom ? HandStyle.Fist : c.ChargeHand;
                if (c.ChargeHand == HandStyle.Point) P.HandL = HandStyle.Fist;
                P.Ki = F(c.KiMax * ch + c.KiPulse * Math.Sin(t * 9) * ch);
                if (c.ChargeReach == Reach.Forehead) { P.KiAt = new PointF(206, 110); P.Ki = F(P.Ki * .55); }
                SetAura(P, .1 + .65 * ch, c.Aura, t); P.Spark = F(.35 * ch);
                P.BodyX = F(shake * 1.4 * ch); HairBlow(P, t, 3 + 9 * ch);
                P.Eyes = EyeStyle.Open; P.OpenL = P.OpenR = 1; P.Brow = F(.55 * ch); P.Cross = 0; P.Gy = 0; P.Gx = -.5f;
                P.Mouth = ch > .15 ? MouthStyle.Open : MouthStyle.Neutral; P.MouthOpen = F(.25 + .18 * Math.Abs(Math.Sin(t * 5)) * ch);
                P.Sweat = ch > .5 ? F(t) : -1f;
                if (c.ChargeReach == Reach.Forehead) { P.Eyes = EyeStyle.Open; P.Brow = .8f; P.Mouth = MouthStyle.Neutral; P.OpenL = P.OpenR = .8f; }
            }
            else
            {
                double f = ESeg(t, c.FireAt, c.FireAt + .3), rel = ESeg(t, c.HoldTo, c.EndAt);
                P.Reach = rel < .85 ? c.FireReach : Reach.None; P.HandL = P.HandR = c.FireHand;
                if (c.FireHand == HandStyle.Point) P.HandL = HandStyle.Open;
                P.Ki = F(c.KiMax * (1 - rel));
                P.BeamLen = F(c.MaxLen * f * (1 - rel)); P.BeamW = F(c.BeamW * (1 - .8 * rel) * (1 + .08 * Math.Sin(t * 30)));
                P.Flash = F(Math.Max(0, 1 - (t - c.FireAt) / .35) * .85);
                SetAura(P, .95 * (1 - rel * .8), c.Aura, t); P.Spark = F(.45 * (1 - rel));
                P.Lean = F(-6 + 3 * Math.Sin(t * 41) * (1 - rel) + 10 * rel); P.BodyX = F(Math.Sin(t * 47) * 1.1 * (1 - rel));
                HairBlow(P, t, 10);
                FaceStrain(P, .7, .7);
                L.HopY = F(-Math.Abs(Math.Sin(t * 40)) * .5 * S * (1 - rel));
                P.Sweat = F(t);
            }
            if (t >= c.EndAt) RecoverPose(P, t, c);
        }

        private void RecoverPose(Pose P, double t, BeamCfg c)
        {
            double q = Seg(t, c.EndAt, extraDuration);
            P.Ki = 0; P.BeamLen = 0; P.KiKind = 0; P.Shot = -1; P.Flash = 0; P.Spark = 0; P.Reach = Reach.None; P.HandL = P.HandR = HandStyle.Open;
            P.Aura = F(Math.Max(0, .3 * (1 - q * 3))); P.AuraKind = c.Aura; P.AuraT = F(t);
            double pant = Math.Sin(t * 8);
            switch (c.Recover)
            {
                case 1:
                    // 허리를 부여잡고 콜록
                    P.BodyY = 24; P.Knee = 10; P.Lean = F(-14 + pant * 1.2); P.Reach = Reach.None; P.ArmLA = 78; P.ArmRA = 70; P.ArmLB = 70; P.ArmRB = 60;
                    P.Eyes = EyeStyle.Sleep; P.OpenL = P.OpenR = 0; P.Mouth = MouthStyle.Open; P.MouthOpen = F(.25 + .2 * Math.Abs(pant)); P.Sweat = F(t); P.HeadTilt = F(-6 + pant);
                    break;
                case 2:
                    P.Reach = Reach.Cross; P.ArmsFront = true; P.Mouth = MouthStyle.Smile; P.Brow = .45f; P.Eyes = EyeStyle.Open; P.OpenL = P.OpenR = .6f; P.Cross = .7f; P.Gy = .1f;
                    P.BodyY = 0; P.Knee = 0; P.Lean = 0; P.Turn = 0; P.View = 0; P.HeadTilt = 3;
                    break;
                default:
                    P.Reach = Reach.Lap; P.BodyY = 20; P.Knee = 12; P.Lean = F(-12 + pant * 1.5); P.Eyes = EyeStyle.Open; P.OpenL = P.OpenR = .6f;
                    P.Mouth = MouthStyle.Open; P.MouthOpen = F(.25 + .2 * Math.Abs(pant)); P.Sweat = F(t); P.HeadTilt = F(-5); P.Gy = .7f; P.Cross = .3f;
                    break;
            }
        }

        // ── 손오공 ──
        private void GokuTeleport(Pose P, ref Look L, double t)
        {
            P.Eyes = EyeStyle.Open; P.OpenL = P.OpenR = 1;
            if (t < 2.0)
            {
                // 이마에 두 손가락을 대고 정신 집중
                double c = ESeg(t, 0, .6);
                P.Reach = Reach.Forehead; P.HandR = HandStyle.Point; P.HandL = HandStyle.Fist;
                P.Eyes = EyeStyle.Sleep; P.OpenL = P.OpenR = 0; P.Brow = F(.6 * c); P.Mouth = MouthStyle.Neutral;
                SetAura(P, .1 + .4 * ESeg(t, .6, 1.8), 1, t); P.Spark = F(.35 * ESeg(t, .8, 1.8));
                HairBlow(P, t, 2 + 6 * c); P.BodyX = F(Math.Sin(t * 40) * .6 * c);
                L.Alpha = F(1 - ESeg(t, 1.5, 2.0)); P.Flash = F(ESeg(t, 1.6, 1.95) * .9);
            }
            else if (t < 3.4)
            {
                L.Alpha = 0; P.Reach = Reach.Forehead; P.HandR = HandStyle.Point; P.Eyes = EyeStyle.Sleep; P.OpenL = P.OpenR = 0;
            }
            else
            {
                double q = ESeg(t, 3.4, 3.8);
                L.Alpha = F(q); P.Flash = F(Math.Max(0, 1 - (t - 3.4) / .5) * .9);
                if (t < 4.4)
                {
                    // 짠! 하고 나타나 팔을 번쩍
                    P.ArmLA = P.ArmRA = F(150); P.ArmLB = P.ArmRB = F(150); P.Eyes = EyeStyle.Happy; P.OpenL = P.OpenR = 0;
                    P.Mouth = MouthStyle.Open; P.MouthOpen = .5f; L.HopY = F(-Math.Abs(Math.Sin((t - 3.4) * 5)) * 4 * S);
                    SetAura(P, .3 * (1 - Seg(t, 3.4, 4.4)), 1, t);
                }
                else
                {
                    // 능청스럽게 손을 흔든다
                    double wv = Math.Sin(t * 9);
                    P.ArmRA = 150; P.ArmRB = F(130 + wv * 24); P.Eyes = EyeStyle.Happy; P.OpenL = P.OpenR = 0; P.Mouth = MouthStyle.Smile; P.HeadTilt = F(Math.Sin(t * 3) * 3);
                }
            }
        }

        private void GokuSuper(Pose P, ref Look L, double t)
        {
            double up = ESeg(t, .8, 3.2), burst = Seg(t, 3.0, 3.6), down = ESeg(t, 7.0, 7.6);
            P.Super = F(ESeg(t, 3.0, 3.5) * (1 - down));
            P.HandL = P.HandR = HandStyle.Fist;
            if (t < 3.3)
            {
                // 웅크려 주먹을 쥐고 고함
                SigStance(P, 0);
                P.BodyY = F(10 + 18 * up); P.Knee = F(16 * up); P.FootLX = F(-14 * up); P.FootRX = F(14 * up);
                P.ArmLA = F(40 - 8 * up); P.ArmRA = F(40 - 8 * up); P.ArmLB = F(18); P.ArmRB = F(18);
                FaceStrain(P, .5 + .4 * Math.Abs(Math.Sin(t * 14)), .5 + .6 * up);
                P.BodyX = F(Math.Sin(t * 60) * (1 + 2.4 * up)); HairBlow(P, t, 4 + 14 * up);
                SetAura(P, .15 + .85 * up, 1, t); P.Spark = F(.2 + .7 * up);
                L.HopY = F(Math.Sin(t * 70) * .9 * S * up);
                P.Flash = F(Math.Max(0, 1 - Math.Abs(t - 3.3) / .35) * .0);
            }
            else if (t < 7.2)
            {
                // 금빛으로 우뚝
                P.Reach = Reach.Hips; P.BodyY = 4; P.Knee = 0; P.FootLX = -16; P.FootRX = 16;
                P.Eyes = EyeStyle.Open; P.OpenL = P.OpenR = 1; P.Brow = .5f; P.Mouth = MouthStyle.Smile; P.Cross = 0; P.Gy = 0; P.Gx = 0;
                SetAura(P, 1, 2, t); P.Spark = F(.55 + .2 * Math.Sin(t * 9)); HairBlow(P, t, 7);
                P.Breath = F(Math.Sin(t * 3)); L.HopY = F(Math.Sin(t * 4) * .6 * S);
            }
            else
            {
                P.Reach = Reach.None; P.ArmLA = P.ArmRA = 56; P.ArmLB = P.ArmRB = 40; P.Eyes = EyeStyle.Open; P.OpenL = P.OpenR = 1; P.Mouth = MouthStyle.Smile; P.HandL = P.HandR = HandStyle.Open;
                SetAura(P, 1 - down, 2, t); HairBlow(P, t, 4);
            }
            P.Flash = F(Math.Max(P.Flash, Math.Max(0, 1 - Math.Abs(t - 3.3) / .3) * .95 + Math.Max(0, 1 - Math.Abs(t - 7.3) / .25) * .7));
        }

        // ── 어린 오공 ──
        private void KidNimbus(Pose P, ref Look L, double t)
        {
            double s = ESeg(t, .1, .9) * (1 - ESeg(t, 6.6, 7.6));
            P.Nimbus = F(s);
            double lift = (26 * S) * s + Math.Sin(t * 2.6) * 2 * S * s;
            L.HopY = F(-lift);
            P.Sit = true; P.BodyY = 30; P.Knee = 6; P.FootLX = -10; P.FootRX = 10; P.FootLRot = 6; P.FootRRot = 6;
            P.Eyes = EyeStyle.Happy; P.OpenL = P.OpenR = 0; P.Mouth = MouthStyle.Open; P.MouthOpen = F(.45 + .15 * Math.Sin(t * 5));
            if (t < .9) { P.Sit = false; P.BodyY = 0; P.Knee = 0; P.FootLX = P.FootRX = 0; P.ArmLA = P.ArmRA = F(140); P.ArmLB = P.ArmRB = F(150); P.Eyes = EyeStyle.Open; P.OpenL = P.OpenR = 1; }
            else
            {
                double wv = Math.Sin(t * 8);
                P.ArmRA = 150; P.ArmRB = F(130 + wv * 26); P.ArmLA = F(70 + Math.Sin(t * 2.4) * 6); P.ArmLB = 50;
                P.HeadTilt = F(Math.Sin(t * 2.2) * 4); P.Lean = F(-4 + Math.Sin(t * 2.6) * 2);
                P.View = 1; P.Turn = .35f;
            }
            P.Tail = F(14 + Math.Sin(t * 6) * 18); P.TailWaveAmp = 22; P.TailWavePhase = F(t * 9);
            HairBlow(P, t, 4 + 4 * s);
            if (t > 6.6) P.Eyes = EyeStyle.Open;
            if (t > 7.4) { P.Sit = false; P.BodyY = 0; P.Knee = 0; P.FootLX = P.FootRX = 0; }
        }

        private void KidPole(Pose P, ref Look L, double t)
        {
            P.Eyes = EyeStyle.Open; P.OpenL = P.OpenR = 1; P.Mouth = MouthStyle.Open; P.MouthOpen = .4f; P.Brow = .2f; P.Cross = .3f; P.Gx = -.3f; P.Gy = .1f;
            P.Tail = F(10 + Math.Sin(t * 7) * 14); P.TailWaveAmp = 18; P.TailWavePhase = F(t * 10);
            SigStance(P, ESeg(t, .3, .8) * (1 - ESeg(t, 6.2, 6.9)), .8);
            if (t < .6)
            {
                // 등에서 여의봉을 뽑는다
                double q = Seg(t, 0, .6); P.Reach = Reach.Scratch; P.Wipe = F(t * 4);
                P.Staff = 0;
            }
            else if (t < 6.4)
            {
                P.Staff = 2; P.HandL = P.HandR = HandStyle.Fist;
                double ang, len = 1, spin = Seg(t, .6, 3.0);
                double ext = ESeg(t, 3.0, 4.2) * (1 - ESeg(t, 5.4, 6.2));
                if (t < 3.0) ang = (t - .6) * 560;                                  // 빙글빙글
                else if (t < 4.4) ang = 20 - 30 * ESeg(t, 3.0, 4.2);                // 하늘로 쭉
                else if (t < 5.4) ang = -10 - 100 * ESeg(t, 4.4, 5.0);              // 앞으로 내리침
                else ang = -110 + 110 * ESeg(t, 5.4, 6.2) - (t > 6 ? 0 : 0);
                len = 1 + 2.2 * ext;
                P.StaffAngle = F(ang); P.StaffLen = F(len);
                P.Reach = Reach.None;
                PointF pv = new PointF(150, 338 - (ext > .1 ? 10 : 0));
                P.HandLTo = new PointF(pv.X - 14, pv.Y + 4); P.HandRTo = new PointF(pv.X + 16, pv.Y - 2);
                P.Brow = F(.5 * ext); HairBlow(P, t, 4 + 5 * ext);
                if (t > 4.9 && t < 5.2) { P.Flash = F(1 - Math.Abs(t - 5.0) / .25); L.HopY = F(-Math.Sin((t - 4.9) / .3 * Math.PI) * 6 * S); }
                if (t >= 4.9 && t < 5.4) { P.BodyY += 6; }
            }
            else
            {
                P.Staff = 0; P.Reach = Reach.None; P.ArmLA = 150; P.ArmRA = 150; P.ArmLB = 120; P.ArmRB = 120; P.HandL = P.HandR = HandStyle.Open;
                P.Eyes = EyeStyle.Happy; P.OpenL = P.OpenR = 0; P.Mouth = MouthStyle.Smile;
                L.HopY = F(-Math.Abs(Math.Sin(t * 6)) * 5 * S);
            }
        }

        private void KidApe(Pose P, ref Look L, double t)
        {
            double moon = ESeg(t, .2, 1.0) * (1 - ESeg(t, 7.8, 8.8));
            L.Decor |= Decor.Moon; L.Moon = F(moon);
            double grow = ESeg(t, 1.4, 3.2) * (1 - ESeg(t, 7.4, 8.4));
            P.Ape = F(grow);
            L.Sx = L.Sy = F(1 + .8 * grow);
            P.Tail = F(8 + Math.Sin(t * 5) * 10); P.TailWaveAmp = 14; P.TailWavePhase = F(t * 7);
            if (t < 1.4)
            {
                // 보름달을 올려다본다
                P.Eyes = EyeStyle.Open; P.OpenL = P.OpenR = 1; P.EyeScale = F(1 + .15 * ESeg(t, .4, 1.3)); P.PupilR = .6f; P.Gy = -.9f; P.Cross = 0; P.HeadTilt = -7; P.HeadY = -4;
                P.Mouth = MouthStyle.Scared; P.MouthOpen = .6f; P.Sweat = F(t); P.BodyX = F(Math.Sin(t * 30) * .8 * Seg(t, .6, 1.4));
            }
            else if (t < 3.3)
            {
                // 부들부들 변신
                P.BodyX = F(Math.Sin(t * 55) * 2.6); P.BodyY = 8; P.Knee = 8; HairBlow(P, t, 14);
                FaceStrain(P, .9, .8); P.EyeScale = 1.2f; P.Reach = Reach.None; P.ArmLA = P.ArmRA = 120; P.ArmLB = P.ArmRB = 140; P.HandL = P.HandR = HandStyle.Fist;
                P.Spark = F(.5 * Seg(t, 1.4, 2.4)); P.Flash = F(Math.Max(0, 1 - Math.Abs(t - 2.6) / .3));
                SetAura(P, .6 * grow, 4, t);
            }
            else if (t < 7.5)
            {
                // 가슴을 쿵쿵 치며 포효
                double beat = Math.Sin(t * 8), a = Math.Max(0, beat);
                FaceStrain(P, .8 + .2 * Math.Abs(beat), .9); P.EyeScale = 1.15f; P.HandL = P.HandR = HandStyle.Fist;
                P.HandLTo = beat > 0 ? new PointF(176, 350) : new PointF(110, 230 - 10 * (float)a);
                P.HandRTo = beat > 0 ? new PointF(110, 230) : new PointF(224, 350);
                P.BodyY = F(8 + 4 * Math.Abs(beat)); P.Knee = 10; P.Lean = F(Math.Sin(t * 4) * 3); P.HeadTilt = F(-3 + beat * 3);
                P.FootLX = -20; P.FootRX = 20;
                L.HopY = F(-Math.Max(0, Math.Sin(t * 4)) * 4 * S); HairBlow(P, t, 6);
                if (Math.Abs(Math.Sin(t * 4)) > .97) P.Flash = .3f;
            }
            else
            {
                P.Reach = Reach.None; P.ArmLA = P.ArmRA = 56; P.ArmLB = P.ArmRB = 40; P.Eyes = EyeStyle.Open; P.OpenL = P.OpenR = 1; P.Mouth = MouthStyle.Smile; P.Sweat = F(t); P.HeadTilt = 5;
                P.Flash = F(Math.Max(0, 1 - Math.Abs(t - 7.7) / .3));
            }
        }

        // ── 무천도사 ──
        private void RoshiMax(Pose P, ref Look L, double t)
        {
            double m = ESeg(t, 1.0, 3.0) * (1 - ESeg(t, 5.4, 6.2));
            P.Muscle = F(m);
            if (t < 1.0 || t >= 6.1)
            {
                // 허리가 굽은 노인
                bool end = t >= 6.1; double cough = Math.Sin(t * 14);
                P.BodyY = 14; P.Lean = F(-10 + (end ? cough * 1.4 : 0)); P.HeadTilt = F(-6); P.Knee = 6; P.HeadY = 4;
                P.Eyes = end ? EyeStyle.Sleep : EyeStyle.Open; P.OpenL = P.OpenR = end ? 0 : 1; P.Mouth = end ? MouthStyle.Open : MouthStyle.Frown; P.MouthOpen = end ? F(.3 + .25 * Math.Abs(cough)) : 0;
                P.Sweat = end ? F(t) : -1f;
                if (end) { P.ArmRA = 56; P.ArmRB = 40; P.ArmLA = 76; P.ArmLB = 66; P.Reach = Reach.None; P.Breath = F(cough); }
            }
            else if (t < 3.0)
            {
                // 숨을 들이마시며 부푼다
                P.Breath = F(1.4 * m); P.BodyX = F(Math.Sin(t * 50) * 2 * m); P.BodyY = F(14 * (1 - m)); P.Lean = F(-10 * (1 - m));
                P.Eyes = EyeStyle.Open; P.OpenL = P.OpenR = 1; P.Brow = F(.5 * m); P.Mouth = MouthStyle.Open; P.MouthOpen = F(.3 + .3 * m);
                P.ArmLA = P.ArmRA = F(60 + 30 * m); P.ArmLB = P.ArmRB = F(43 + 40 * m); P.HandL = P.HandR = HandStyle.Fist;
                SetAura(P, .5 * m, 1, t); P.Spark = F(.4 * m);
                P.Flash = F(Math.Max(0, 1 - Math.Abs(t - 2.9) / .25) * .9);
            }
            else
            {
                // MAX 파워: 알통 포즈를 번갈아
                int pose = ((int)((t - 3.0) / 1.0)) % 2;
                P.Reach = pose == 0 ? Reach.Flex : Reach.Hips; P.HandL = P.HandR = HandStyle.Fist; P.Turn = 0; P.View = 0;
                P.Eyes = EyeStyle.Open; P.OpenL = P.OpenR = 1; P.Brow = .55f; P.Mouth = MouthStyle.Open; P.MouthOpen = F(.4 + .3 * Math.Abs(Math.Sin(t * 6)));
                P.BodyY = 4; P.Knee = 2; P.FootLX = -14; P.FootRX = 14; P.Lean = F(Math.Sin(t * 6) * 1.2);
                SetAura(P, (t < 5.4 ? 1 : 1 - Seg(t, 5.4, 6.2)) * .95, 1, t); P.Spark = F(.5 * (t < 5.4 ? 1 : 1 - Seg(t, 5.4, 6.2)));
                L.HopY = F(-Math.Max(0, Math.Sin(t * 6)) * 1.5 * S);
            }
        }

        private void RoshiMafuba(Pose P, ref Look L, double t)
        {
            double stance = ESeg(t, 1.0, 1.8) * (1 - ESeg(t, 6.0, 6.8));
            if (t < 1.0)
            {
                // 전기밥솥을 꺼낸다
                P.Reach = Reach.Belly; P.ArmsFront = true; P.Prop = 1; P.Eyes = EyeStyle.Open; P.OpenL = P.OpenR = 1; P.Brow = .3f * (float)Seg(t, .4, 1.0); P.Mouth = MouthStyle.Smile;
            }
            else if (t < 6.8)
            {
                SigStance(P, stance, .9);
                P.Reach = Reach.Front; P.HandL = P.HandR = HandStyle.Open; P.Eyes = EyeStyle.Open; P.OpenL = P.OpenR = 1; P.Brow = .6f;
                P.BodyX = F(Math.Sin(t * 44) * (t > 2.2 && t < 5.2 ? 1.6 : .4));
                if (t >= 2.2 && t < 5.4)
                {
                    P.Prop = 2; P.KiKind = (int)KiKind.Suck; P.BeamLen = F(760 * ESeg(t, 2.2, 2.7) * (1 - ESeg(t, 5.0, 5.4))); P.BeamT = F(t);
                    P.KiAt = new PointF(60, 330);
                    SetAura(P, .55, 1, t); P.Spark = .25f; HairBlow(P, t, 6);
                    P.Mouth = MouthStyle.Open; P.MouthOpen = .5f; P.Sweat = F(t);
                    P.Wipe = -1;
                }
                else if (t >= 5.2)
                {
                    P.Prop = 1; P.Flash = F(Math.Max(0, 1 - Math.Abs(t - 5.4) / .3) * .9);
                    P.Mouth = t < 5.9 ? MouthStyle.Open : MouthStyle.Frown; P.MouthOpen = .4f; P.Sweat = F(t);
                }
                else P.Mouth = MouthStyle.Neutral;
            }
            else
            {
                // 지쳐서 흔들흔들
                double w2 = Math.Sin(t * 5);
                P.Prop = 1; P.Reach = Reach.Belly; P.ArmsFront = true; P.BodyY = 16; P.Lean = F(-8 + w2 * 3); P.HeadTilt = F(-6 + w2 * 3); P.Knee = 8;
                P.Eyes = EyeStyle.Sleep; P.OpenL = P.OpenR = 0; P.Mouth = MouthStyle.Open; P.MouthOpen = F(.25 + .2 * Math.Abs(Math.Sin(t * 8))); P.Sweat = F(t);
            }
        }

        // ── 베지터 ──
        private void VegetaBigBang(Pose P, ref Look L, double t)
        {
            double stance = ESeg(t, 0, .8) * (1 - ESeg(t, 4.6, 5.3));
            SigStance(P, stance, 1);
            P.Eyes = EyeStyle.Open; P.OpenL = P.OpenR = 1; P.Brow = .8f; P.Cross = 0; P.Gy = 0; P.Gx = -.5f;
            if (t < 4.5)
            {
                float lv = (float)ESeg(t, .8, 2.8);
                P.Reach = Reach.None; P.HandL = HandStyle.Fist; P.HandR = HandStyle.Open;
                P.HandLTo = new PointF(shLx() - 6, 400); P.HandRTo = new PointF(64, 322);
                P.KiKind = (int)KiKind.Blue; P.BeamT = F(t); P.KiAt = new PointF(34, 316);
                if (t < 2.9)
                {
                    P.Ki = F(.85 * lv + .05 * Math.Sin(t * 9) * lv);
                    SetAura(P, .1 + .6 * lv, 3, t); P.Spark = F(.4 * lv); HairBlow(P, t, 3 + 6 * lv);
                    P.Mouth = MouthStyle.Open; P.MouthOpen = F(.2 + .2 * Math.Abs(Math.Sin(t * 5)) * lv); P.BodyX = F(Math.Sin(t * 50) * 1.2 * lv);
                }
                else
                {
                    P.Ki = F(Math.Max(0, .85 * (1 - Seg(t, 2.9, 3.3))));
                    P.Shot = F(Seg(t, 2.9, 4.5)); if (P.Shot >= 1) P.Shot = -1;
                    SetAura(P, .8 * (1 - Seg(t, 3.0, 4.6)), 3, t); P.Spark = .3f;
                    P.Lean = F(-7); P.Mouth = MouthStyle.Open; P.MouthOpen = .6f; HairBlow(P, t, 8);
                    P.Flash = F(Math.Max(0, 1 - (t - 2.9) / .3) * .6);
                }
            }
            else
            {
                P.Reach = Reach.Cross; P.ArmsFront = true; P.Mouth = MouthStyle.Neutral; P.Brow = .6f; P.OpenL = P.OpenR = .7f; P.Cross = .6f; P.HeadTilt = F(-3);
            }
        }
        private float shLx() { return 130; }

        private void VegetaPrince(Pose P, ref Look L, double t)
        {
            double rise = ESeg(t, .8, 2.0) * (1 - ESeg(t, 5.8, 6.8));
            L.HopY = F(-(16 * S) * rise - Math.Sin(t * 2.4) * 1.8 * S * rise);
            P.Turn = 0; P.View = 0;
            P.Reach = Reach.Cross; P.ArmsFront = true;
            if (t >= 4.8 && t < 6.0) { P.Reach = Reach.None; P.ArmsFront = false; P.HandRTo = new PointF(64, 296); P.HandR = HandStyle.Point; P.HandLTo = new PointF(166, 316); P.HandL = HandStyle.Fist; P.View = 1; P.Turn = .5f; }
            P.Eyes = t < 1.6 ? EyeStyle.Sleep : EyeStyle.Open; P.OpenL = P.OpenR = t < 1.6 ? 0 : 1; P.Brow = .7f; P.Cross = 0; P.Gy = .1f;
            P.Mouth = t > 2.8 ? MouthStyle.Open : MouthStyle.Smile; P.MouthOpen = F(.3 + .25 * Math.Abs(Math.Sin(t * 5)) * (t > 2.8 ? 1 : 0));
            P.FootLX = F(-4 * rise); P.FootRX = F(4 * rise); P.FootLY = P.FootRY = F(-6 * rise);
            double a = rise * (t < 5.8 ? 1 : 1 - Seg(t, 5.8, 6.8));
            SetAura(P, Math.Max(.1 * Math.Min(1, t), .95 * a), 3, t); P.Spark = F(.5 * a);
            HairBlow(P, t, 3 + 9 * a); P.Breath = F(Math.Sin(t * 3));
            P.Flash = F(Math.Max(0, 1 - Math.Abs(t - 2.0) / .3) * .9);
            P.Lean = F(Math.Sin(t * 2) * 1.5);
        }

        // ── 피콜로 ──
        private void PiccoloStretch(Pose P, ref Look L, double t)
        {
            P.Eyes = EyeStyle.Open; P.OpenL = P.OpenR = 1; P.Brow = .7f; P.Cross = 0; P.Gy = 0; P.Gx = -.4f; P.Mouth = MouthStyle.Neutral;
            P.Tail = F(-10 + Math.Sin(t * 6) * 6); P.TailWaveAmp = 12; P.TailWavePhase = F(t * 8);
            double stance = ESeg(t, 0, .7) * (1 - ESeg(t, 5.4, 6.2));
            SigStance(P, stance, 1.1);
            P.HandL = P.HandR = HandStyle.Fist;
            if (t < .9) { P.Reach = Reach.Guard; }
            else if (t < 1.7)
            {
                // 오른팔을 뒤로 당긴다
                P.Reach = Reach.None; P.HandLTo = new PointF(98, 330); P.HandRTo = new PointF(284, 330 + 24 * (float)Seg(t, .9, 1.7)); P.Lean = F(3 + 6 * Seg(t, .9, 1.7));
            }
            else if (t < 3.6)
            {
                // 쭉! 오른팔이 늘어나 주먹을 날린다
                double f = ESeg(t, 1.7, 2.1) * (1 - ESeg(t, 2.8, 3.5));
                P.ArmLenR = F(1 + 2.9 * f); P.HandLTo = new PointF(98, 330);
                P.HandRTo = new PointF(F(200 - 90 - 380 * f), 322);
                P.Lean = F(-8 * f + 3 * (1 - f)); P.Mouth = MouthStyle.Open; P.MouthOpen = F(.5 * f);
                P.Flash = F(Math.Max(0, 1 - Math.Abs(t - 2.15) / .15) * .55);
                SetAura(P, .4 * f, 5, t);
            }
            else if (t < 5.4)
            {
                // 이번엔 왼팔이 위로 쭉
                double f = ESeg(t, 3.8, 4.2) * (1 - ESeg(t, 4.8, 5.3));
                P.ArmLenL = F(1 + 2.3 * f); P.HandRTo = new PointF(284, 340);
                P.HandLTo = new PointF(F(110 - 130 * f), F(300 - 250 * f));
                P.Lean = F(-4 * f); P.Mouth = MouthStyle.Open; P.MouthOpen = F(.5 * f);
                SetAura(P, .4 * f, 5, t);
            }
            else
            {
                P.Reach = Reach.Cross; P.ArmsFront = true; P.Mouth = MouthStyle.Smile; P.OpenL = P.OpenR = .65f; P.Brow = .4f;
            }
        }

        private void PiccoloMeditate(Pose P, ref Look L, double t)
        {
            double rise = ESeg(t, .2, 1.4) * (1 - ESeg(t, 7.2, 8.6));
            L.HopY = F(-(30 * S) * rise - Math.Sin(t * 2.3) * 2.4 * S * rise);
            P.Sit = true; P.BodyY = F(34 * rise); P.Knee = F(38 * rise);
            P.FootLX = F(32 * rise); P.FootRX = F(-32 * rise); P.FootLY = P.FootRY = F(-2 * rise); P.FootLRot = F(-20 * rise); P.FootRRot = F(-20 * rise);
            if (rise < .05) { P.Sit = false; P.BodyY = 0; P.Knee = 0; }
            P.Tail = F(Math.Sin(t * 3) * 10 + 10 * rise); P.TailWaveAmp = F(10 + 10 * rise); P.TailWavePhase = F(t * 5);
            if (t < 6.5 || t > 7.2)
            {
                bool calm = t < 6.5 || t > 7.6;
                P.Eyes = t > 7.4 ? EyeStyle.Open : EyeStyle.Sleep; P.OpenL = P.OpenR = t > 7.4 ? .75f : 0; P.Mouth = MouthStyle.Neutral;
                P.Reach = t > 7.6 ? Reach.Cross : Reach.Lap; P.ArmsFront = true; P.HandL = P.HandR = HandStyle.Open;
                P.Breath = F(Math.Sin(t * 1.4) * .7); P.Brow = .2f; P.HeadTilt = 0;
                SetAura(P, (t < 6.5 ? .25 + .35 * ESeg(t, .6, 3.0) + .08 * Math.Sin(t * 2) : .5 * (1 - ESeg(t, 7.2, 8.4))) * (calm ? 1 : 0), 5, t);
                P.Spark = 0;
            }
            else
            {
                // 눈을 번쩍!
                double q = Seg(t, 6.5, 7.2);
                P.Eyes = EyeStyle.Open; P.OpenL = P.OpenR = 1; P.EyeScale = 1.12f; P.Brow = .9f; P.Reach = Reach.Lap; P.ArmsFront = true;
                P.Mouth = MouthStyle.Neutral; P.Cross = 0; P.Gy = 0; P.Gx = -.2f;
                SetAura(P, .75 + .25 * (1 - q), 5, t); P.Spark = F(.4 * (1 - q));
                P.Flash = F(Math.Max(0, 1 - q * 2.2) * .9);
            }
        }
    }
}
