using System;
using System.Drawing;

namespace SuperHeroes
{
    /// <summary>
    /// 수퍼히어로 캐릭터별 고유 몸짓과 개성 (Idle, Walk, Pause).
    /// </summary>
    internal sealed partial class Pet
    {
        private int pauseAct;
        private Persona Who { get { return Art.Persona; } }
        private bool SeriousType { get { return Who == Persona.Batman; } }
        private bool AngryType { get { return Who == Persona.Batman; } }

        private bool IdleLike
        {
            get
            {
                return State == PetState.Pause || State == PetState.WaitHome ||
                       (State == PetState.Resting && (restPose == RestPose.Sit || restPose == RestPose.Daydream));
            }
        }

        /// <summary>
        /// 멈춰 선 동안의 잔동작:
        /// 0 없음 · 1 허리춤 당당 포즈 · 2 팔짱/경계 · 3 손 흔들기 · 4 웹슈터/장비 점검 · 5 수염 쓰다듬기
        /// </summary>
        private int ChoosePauseAct(IWorld w)
        {
            if (w.Rng.NextDouble() < .45) return 0;
            int[] pool;
            switch (Who)
            {
                case Persona.Superman:
                    pool = new[] { 1, 1, 3, 1 }; // 허리춤, 손 흔들기
                    break;
                case Persona.Batman:
                    pool = new[] { 2, 2, 4, 2 }; // 팔짱, 건틀릿 점검
                    break;
                case Persona.Spiderman:
                    pool = new[] { 3, 4, 4, 1 }; // 손 흔들기, 웹슈터 점검
                    break;
                case Persona.Thor:
                    pool = new[] { 5, 5, 1, 3 }; // 수염 쓰다듬기, 당당 포즈, 손 흔들기
                    break;
                case Persona.WonderWoman:
                    pool = new[] { 1, 1, 2, 3 }; // 당당 포즈, 팔짱(경계), 손 흔들기
                    break;
                default:
                    pool = new[] { 1, 2, 3 };
                    break;
            }
            return pool[w.Rng.Next(pool.Length)];
        }

        private void PauseActPose(Pose P, double t)
        {
            double e = Math.Min(1, StateT / .35) * (1 - Seg(StateT, Math.Max(.5, stateDur - .35), stateDur));
            if (e < .05) return;

            switch (pauseAct)
            {
                case 1: // 양손 허리춤 당당 포즈
                    P.Reach = Reach.Hips;
                    P.Mouth = MouthStyle.Smile;
                    P.HeadTilt = (float)(-4 * e);
                    P.CapeWind = 12f;
                    break;

                case 2: // 팔짱 및 주변 경계
                    P.Reach = Reach.Cross;
                    P.ArmsFront = true;
                    P.Mouth = MouthStyle.Neutral;
                    P.Brow = 0.4f;
                    P.HeadTilt = (float)(Math.Sin(t * 1.5) * 4);
                    break;

                case 3: // 손 흔들기
                    P.ArmRA = 150;
                    P.ArmRB = (float)(130 + Math.Sin(t * 9) * 24);
                    P.Mouth = MouthStyle.Smile;
                    P.Eyes = EyeStyle.Happy;
                    P.HeadTilt = (float)(Math.Sin(t * 3) * 3);
                    break;

                case 4: // 웹슈터 / 건틀릿 점검
                    P.Reach = Reach.Chin;
                    P.HeadTilt = 8f;
                    if (Who == Persona.Spiderman)
                    {
                        P.HandR = HandStyle.Web;
                        P.Eyes = EyeStyle.Narrow;
                    }
                    break;

                case 5: // 턱수염 쓰다듬기 (토르)
                    P.Reach = Reach.Chin;
                    P.Wipe = (float)(t * 1.5);
                    P.Eyes = EyeStyle.Happy;
                    P.Mouth = MouthStyle.Smile;
                    P.HeadTilt = (float)(Math.Sin(t * 2) * 4);
                    break;
            }
        }

        private void ApplyPersona(ref Look L, double now)
        {
            if (IsSignatureActive) { SigBubble(ref L); return; }

            Pose P = L.Pose;
            double t = animT;
            bool walking = walkAmp > .02 && (State == PetState.Walk || State == PetState.Gather || State == PetState.Pause);
            float a = (float)walkAmp;
            float ph = (float)(gait * Tau);

            switch (Who)
            {
                case Persona.Superman:
                    if (IdleLike && P.Mouth == MouthStyle.Neutral) P.Mouth = MouthStyle.Smile;
                    if (walking)
                    {
                        P.CapeWind = 18f * a;
                        P.Mouth = MouthStyle.Smile;
                    }
                    break;

                case Persona.Batman:
                    P.Brow = Math.Max(P.Brow, 0.3f);
                    P.Mouth = MouthStyle.Neutral;
                    if (walking)
                    {
                        P.CapeWind = 14f * a;
                        P.HeadTilt = -2f;
                    }
                    break;

                case Persona.Spiderman:
                    if (walking)
                    {
                        // 통통 튀는 경쾌한 걸음
                        P.BodyY += (float)(-Math.Abs(Math.Sin(ph)) * 4f * a);
                        P.HeadTilt += (float)(Math.Sin(ph) * 5f * a);
                    }
                    if (IdleLike)
                    {
                        P.HeadTilt += (float)(Math.Sin(t * 3) * 4);
                    }
                    break;

                case Persona.Thor:
                    if (walking)
                    {
                        P.CapeWind = 22f * a;
                        P.BodyY += (float)(Math.Sin(ph) * 2f * a);
                    }
                    if (IdleLike && P.Mouth == MouthStyle.Neutral)
                    {
                        P.Mouth = MouthStyle.Smile;
                    }
                    break;

                case Persona.WonderWoman:
                    if (IdleLike && P.Mouth == MouthStyle.Neutral) P.Mouth = MouthStyle.Smile;
                    if (walking)
                    {
                        P.CapeWind = 16f * a;                       // 긴 머리카락이 바람에 휘날린다
                        P.Mouth = MouthStyle.Smile;
                    }
                    break;
            }

            if (State == PetState.Pause && pauseAct != 0 && walkAmp < .05 && P.Tears < 0)
            {
                PauseActPose(P, t);
            }
        }
    }
}
