using System;
using System.Drawing;

namespace DragonBall
{
    /// <summary>
    /// 캐릭터마다 다른 몸짓. 공통 자세(PetPoses.cs)를 만든 뒤에 그 위에 덧씌운다.
    ///  · 손오공: 늘 웃는 얼굴, 머리 긁적·손 흔들기·주먹 불끈
    ///  · 어린 오공: 꼬리를 살랑이며 통통 뛰듯 걷는다, 놀라면 눈이 동그래짐
    ///  · 무천도사: 허리 굽혀 지팡이를 짚고 느릿느릿, 수염을 쓸어내림
    ///  · 베지터: 늘 찌푸린 얼굴, 가만히 서면 팔짱, 건드리면 바로 발끈(💢)
    ///  · 피콜로: 더듬이가 쫑긋, 망토가 펄럭, 팔짱을 끼고 차분히
    /// </summary>
    internal sealed partial class Pet
    {
        private int pauseAct;
        private Persona Who { get { return Art.Persona; } }
        private bool AngryType { get { return Who == Persona.Vegeta || Who == Persona.Piccolo; } }

        /// <summary>서 있거나 앉아서 가만히 있는 때(몸짓을 끼워 넣어도 되는 때).</summary>
        private bool IdleLike
        { get { return State == PetState.Pause || State == PetState.WaitHome || (State == PetState.Resting && (restPose == RestPose.Sit || restPose == RestPose.Daydream)); } }

        /// <summary>멈춰 선 동안의 잔동작: 0 없음 · 1 머리 긁적 · 2 팔짱 · 3 손 흔들기 · 4 주먹 불끈(알통) · 5 수염 쓰다듬기.</summary>
        private int ChoosePauseAct(IWorld w)
        {
            if (w.Rng.NextDouble() < .52) return 0;
            int[] pool;
            switch (Who)
            {
                case Persona.Goku: pool = new[] { 1, 3, 4 }; break;
                case Persona.Kid: pool = new[] { 1, 3, 4, 3 }; break;
                case Persona.Roshi: pool = new[] { 5, 5, 3, 2 }; break;
                case Persona.Vegeta: pool = new[] { 2, 2, 4 }; break;
                default: pool = new[] { 2, 2, 2, 4 }; break;
            }
            return pool[w.Rng.Next(pool.Length)];
        }

        private void PauseActPose(Pose P, double t)
        {
            double e = Math.Min(1, StateT / .35) * (1 - Seg(StateT, Math.Max(.5, stateDur - .35), stateDur));
            if (e < .05) return;
            switch (pauseAct)
            {
                case 1: // 머리 긁적
                    P.Reach = Reach.Scratch; P.Wipe = F(t * 2.6); P.Eyes = EyeStyle.Happy; P.OpenL = P.OpenR = 0; P.Mouth = MouthStyle.Smile;
                    P.HeadTilt = F(-6 * e + P.HeadTilt); P.Sweat = F(t);
                    break;
                case 2: // 팔짱
                    P.Reach = Reach.Cross; P.ArmsFront = true; P.Mouth = MouthStyle.Neutral; P.Brow = F(.2 + P.Brow); P.OpenL = P.OpenR = Math.Min(P.OpenL, .75f); P.Gy = .1f; P.Cross = .5f;
                    P.FootRY = F(-4 * Math.Max(0, Math.Sin(t * 6.5)) * (Math.Sin(t * .9) > 0 ? 1 : 0));
                    break;
                case 3: // 손 흔들기
                    P.ArmRA = 150; P.ArmRB = F(130 + Math.Sin(t * 9) * 24); P.Mouth = MouthStyle.Smile; P.Eyes = EyeStyle.Open; P.HeadTilt = F(Math.Sin(t * 3) * 3);
                    break;
                case 4: // 주먹 불끈 · 알통
                    if (Who == Persona.Vegeta) { P.Reach = Reach.Hips; P.HandL = P.HandR = HandStyle.Fist; P.Mouth = MouthStyle.Neutral; P.Brow = .3f; P.HeadTilt = F(4); break; }
                    P.Reach = Reach.Flex; P.HandL = P.HandR = HandStyle.Fist; P.Mouth = MouthStyle.Smile; P.Eyes = EyeStyle.Happy; P.OpenL = P.OpenR = 0;
                    P.BodyY = F(3 + Math.Sin(t * 8) * 2);
                    break;
                case 5: // 수염 쓰다듬기
                    P.Reach = Reach.Chin; P.Wipe = F(t * 1.3); P.Eyes = EyeStyle.Happy; P.OpenL = P.OpenR = 0; P.HeadTilt = F(P.HeadTilt + 3);
                    break;
            }
        }

        private void ApplyPersona(ref Look L, double now)
        {
            if (IsSignatureActive) { SigBubble(ref L); return; }
            Pose P = L.Pose; double t = animT;
            bool walking = walkAmp > .02 && (State == PetState.Walk || State == PetState.Gather || State == PetState.Pause);
            float a = F(walkAmp), ph = F(gait * Tau);
            switch (Who)
            {
                case Persona.Goku:
                    if (IdleLike && P.Tears < 0 && P.Mouth == MouthStyle.Neutral && P.Eyes == EyeStyle.Open) P.Mouth = MouthStyle.Smile;
                    if (walking) { P.EarL += F(Math.Sin(ph * 2) * 3 * a); P.EarR += F(Math.Sin(ph * 2 + 1) * 3 * a); P.Mouth = MouthStyle.Smile; }
                    break;
                case Persona.Kid:
                    if (walking) { P.BodyY += F(-Math.Abs(Math.Sin(ph)) * 3.5 * a); P.Tail += F(Math.Sin(ph * 2) * 14 * a); P.TailWaveAmp += 10 * a; P.Mouth = MouthStyle.Smile; }
                    if (IdleLike && P.Tears < 0) { P.Tail += F(Math.Sin(t * 5) * 8); P.TailWaveAmp += 8; if (P.Mouth == MouthStyle.Neutral) P.Mouth = MouthStyle.Smile; }
                    break;
                case Persona.Roshi:
                    if (walking) { P.Lean += F(-6 * a); P.BodyY += F(5 * a); P.HeadTilt += F(-4 * a); }
                    break;
                case Persona.Vegeta:
                    if (walking) { P.Lean += F(1.5 * a); P.Brow = Math.Max(P.Brow, .1f); }
                    break;
                case Persona.Piccolo:
                    P.EarL += Twitch(t, 4.7, blinkOffset) * .4f; P.EarR += Twitch(t, 6.1, blinkOffset * 2 + 1) * .4f;
                    if (walking) { P.Tail += F(Math.Sin(ph - .4) * 10 * a); P.TailWaveAmp += 8 * a; }
                    break;
            }
            if (State == PetState.Pause && pauseAct != 0 && walkAmp < .05 && P.Tears < 0 && !(FriendData.Current.Hungry && IsCompanionFloorPose)) PauseActPose(P, t);
        }
    }
}
