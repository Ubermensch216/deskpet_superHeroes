using System;
using System.Drawing;

namespace DragonBall
{
    internal enum ChatPhase { None, Listening, Thinking, Talking, Afterglow }

    /// <summary>
    /// AI 와 이야기하는 캐릭터.
    /// 말을 걸면 돌아보고(Listening), 생각하는 동안 생각 점이 떠오르며(Thinking), 글자가 한 자씩 말풍선에 찍히고
    /// 감정에 맞는 표정·몸짓·오라(Talking · Afterglow)가 나온다.
    /// </summary>
    internal sealed partial class Pet
    {
        internal ChatPhase Phase = ChatPhase.None;
        internal ChatMood Mood = ChatMood.Neutral;
        /// <summary>입력창이 있는 쪽(+1 오른쪽, -1 왼쪽). 캐릭터가 그쪽을 바라본다.</summary>
        internal int ChatSide = 1;
        /// <summary>글자가 한 자 찍힐 때마다 부른다(말소리 효과음용).</summary>
        internal Action<char> OnChatChar;

        private double chatPresence, chatPhaseT, chatMoodT, chatReveal, chatTypeEnergy;
        private string chatText = "";
        private bool chatStreamDone = true, chatTypeDeleted;
        private int chatTypedLen;

        internal bool ChatActive { get { return Phase != ChatPhase.None; } }
        internal string ChatText { get { return chatText; } }
        internal bool ChatRevealing { get { return chatText.Length > 0 && chatReveal < chatText.Length; } }
        internal double ChatQuietSeconds { get { return Phase == ChatPhase.Listening ? chatPhaseT : 0; } }
        internal float ChatPresence { get { return (float)chatPresence; } }

        /// <summary>지금 말을 걸어도 되는 자세인가(낙하·던져짐·벽/천장 타기 중이 아닐 때).</summary>
        internal bool CanChat
        {
            get
            {
                if (Hidden) return false;
                switch (State)
                {
                    case PetState.Falling: case PetState.Drag: case PetState.Thrown: case PetState.Climb:
                    case PetState.Wall: case PetState.Ceiling: case PetState.WindowClimb:
                        return false;
                }
                return true;
            }
        }

        internal bool BeginChat(IWorld w, int side)
        {
            if (!CanChat) return false;
            ChatSide = side >= 0 ? 1 : -1;
            if (State != PetState.Chat) { ReleaseFeatures(w); SetState(PetState.Chat, w); }
            Phase = ChatPhase.Listening; Mood = ChatMood.Neutral; chatPhaseT = 0; chatMoodT = 0;
            chatText = ""; chatReveal = 0; chatStreamDone = true; chatTypeEnergy = 0; chatTypedLen = 0;
            return true;
        }

        internal void EndChat(IWorld w)
        {
            if (Phase == ChatPhase.None) return;
            if (!string.IsNullOrEmpty(chatText) && Bubble == chatText) Bubble = null;
            Phase = ChatPhase.None; chatText = ""; chatReveal = 0; Mood = ChatMood.Neutral;
            if (State == PetState.Chat) { SetState(PlayOptions.Current.PositionFixed ? PetState.Pause : PetState.Walk, w); DelayIdle(w); }
        }

        internal void ChatThink()
        {
            if (Phase == ChatPhase.None) return;
            if (!string.IsNullOrEmpty(chatText) && Bubble == chatText) Bubble = null;
            chatText = ""; chatReveal = 0; chatStreamDone = false;
            Phase = ChatPhase.Thinking; chatPhaseT = 0; SetMood(ChatMood.Think);
        }

        internal void ChatSay(ChatMood mood, string text, bool done)
        {
            if (Phase == ChatPhase.None) return;
            if (text == null) text = "";
            if (text.Length > 0 && Phase != ChatPhase.Talking && Phase != ChatPhase.Afterglow) { Phase = ChatPhase.Talking; chatPhaseT = 0; }
            if (text.Length > 0 && Phase == ChatPhase.Afterglow && !text.StartsWith(chatText)) { chatReveal = 0; Phase = ChatPhase.Talking; chatPhaseT = 0; }
            if (text.Length > 0) SetMood(mood);
            if (text.Length < chatText.Length && !text.Equals(chatText)) chatReveal = Math.Min(chatReveal, text.Length);
            chatText = text; chatStreamDone = done;
            if (done && text.Length == 0) { Phase = ChatPhase.Listening; chatPhaseT = 0; SetMood(ChatMood.Neutral); }
        }

        internal void ChatSayInstant(ChatMood mood, string text)
        {
            if (Phase == ChatPhase.None) return;
            chatReveal = 0; Phase = ChatPhase.Talking; chatPhaseT = 0; SetMood(mood);
            chatText = text ?? ""; chatStreamDone = true;
        }

        private void SetMood(ChatMood m) { if (m != Mood) { Mood = m; chatMoodT = 0; } }

        internal void ChatTyping(int length)
        {
            if (Phase != ChatPhase.Listening && Phase != ChatPhase.Afterglow) { chatTypedLen = length; return; }
            if (length != chatTypedLen) { chatTypeDeleted = length < chatTypedLen; chatTypeEnergy = 1; }
            chatTypedLen = length;
        }

        private void UpdateChat(IWorld w, double dt)
        {
            double target = Phase != ChatPhase.None ? 1 : 0;
            if (chatPresence != target) chatPresence += Math.Sign(target - chatPresence) * Math.Min(Math.Abs(target - chatPresence), dt * (target > chatPresence ? 2.6 : 3.5));
            if (Phase == ChatPhase.None) return;
            chatPhaseT += dt; chatMoodT += dt;
            chatTypeEnergy = Math.Max(0, chatTypeEnergy - dt * 1.3);
            if (chatText.Length > 0)
            {
                double rate = Mood == ChatMood.Sleepy ? 9 : (Mood == ChatMood.Joy || Mood == ChatMood.Surprise || Mood == ChatMood.Pout) ? 19 : 15;
                if (chatText.Length - chatReveal > 30) rate *= 1.8;
                int before = (int)chatReveal;
                chatReveal = Math.Min(chatText.Length, chatReveal + rate * dt);
                for (int i = before; i < (int)chatReveal && i < chatText.Length; i++)
                    if (!char.IsWhiteSpace(chatText[i]) && OnChatChar != null) { try { OnChatChar(chatText[i]); } catch { } }
                Bubble = chatText; BubbleUntil = w.Now + 1.5;
                if (chatReveal >= chatText.Length && chatStreamDone && Phase == ChatPhase.Talking) { Phase = ChatPhase.Afterglow; chatPhaseT = 0; }
            }
            if (Phase == ChatPhase.Afterglow && chatPhaseT > 6.5 + chatText.Length * .08)
            {
                if (Bubble == chatText) Bubble = null;
                chatText = ""; chatReveal = 0; Phase = ChatPhase.Listening; chatPhaseT = 0; SetMood(ChatMood.Neutral);
            }
        }

        internal int ChatRevealCount
        {
            get
            {
                if (Phase == ChatPhase.None || chatText.Length == 0 || Bubble != chatText) return -1;
                return chatReveal >= chatText.Length ? -1 : (int)chatReveal;
            }
        }

        // ───────── 자세 ─────────
        private void ChatPose(Pose P, ref Look L, double now)
        {
            double t = animT;
            L.FaceRight = false;
            IdleBase(P, t, false);
            int side = ChatSide;
            if (Phase == ChatPhase.Listening || Phase == ChatPhase.None) ListeningPose(P, ref L, t, side);
            else if (Phase == ChatPhase.Thinking) ThinkingPose(P, ref L, t, side);
            else
            {
                double k = Ease(chatMoodT / .3);
                if (Phase == ChatPhase.Afterglow) k *= 1 - Ease((chatPhaseT - 3.6) / 1.6);
                ListeningPose(P, ref L, t, side);
                MoodPose(P, ref L, t, side, k);
            }
        }

        private void ListeningPose(Pose P, ref Look L, double t, int side)
        {
            double e = chatTypeEnergy;
            P.Gx = F(side * .65); P.Gy = F(.25 + .3 * e);
            P.HeadTilt = F(P.HeadTilt + side * 2.5 + (chatTypeDeleted ? -side * 4 * e : side * 1.5 * e));
            P.Brow = F(chatTypeDeleted ? -.25 * e : 0);
            if (chatTypedLen >= 24) { P.Eyes = EyeStyle.Open; P.EyeScale = 1.05f; }
            if (Who == Persona.Vegeta || Who == Persona.Piccolo) P.Reach = Reach.Cross;
            else if (Who == Persona.Roshi) P.Reach = Reach.Chin;
        }

        private void ThinkingPose(Pose P, ref Look L, double t, int side)
        {
            double e = Ease(chatPhaseT / .35);
            P.Gx = F(side * .4); P.Gy = F(-.45 * e);
            P.HeadTilt = F(P.HeadTilt - 3 * e * side);
            P.Brow = F(.2 * e);
            L.Decor |= Decor.Dots; L.DecorPhase = (float)t;

            if (Who == Persona.Vegeta) { P.Reach = Reach.Cross; P.Brow = .4f; }
            else if (Who == Persona.Piccolo) { P.Reach = Reach.Cross; }
            else if (Who == Persona.Roshi) { P.Reach = Reach.Chin; }
            else if (Who == Persona.Kid) { P.Reach = Reach.Chin; }
            else { P.Reach = Reach.Scratch; }
        }

        private void MoodPose(Pose P, ref Look L, double t, int side, double k)
        {
            if (k <= .01) return;
            double mt = chatMoodT;
            switch (Mood)
            {
                case ChatMood.Joy:
                    P.Eyes = EyeStyle.Happy; P.Mouth = MouthStyle.Open; P.MouthOpen = F(.6 * k); P.Flush = F(.35 * k);
                    L.HopY = F(-Math.Sin(Math.PI * ((mt % .72) / .72)) * 6 * S * k);
                    break;
                case ChatMood.Shy:
                    P.Eyes = EyeStyle.Open; P.OpenL = P.OpenR = F(.7); P.Flush = F(.9 * k); P.Brow = F(-.2 * k);
                    P.Reach = Reach.Scratch;
                    break;
                case ChatMood.Sad:
                    P.Brow = F(-.85 * k); P.Mouth = MouthStyle.Frown; P.Tears = F(mt); P.Gy = F(.4);
                    break;
                case ChatMood.Surprise:
                    P.Eyes = EyeStyle.Open; P.EyeScale = F(1.08); P.PupilR = .65f; P.Mouth = MouthStyle.Open; P.MouthOpen = F(.8 * k); P.Sweat = F(mt);
                    break;
                case ChatMood.Sleepy:
                    P.Eyes = EyeStyle.Sleep; P.OpenL = P.OpenR = .35f;
                    L.Decor |= Decor.Sleepy; L.ZTime = (float)t; L.ZPhase = (int)(t * 20) % 100000;
                    break;
                case ChatMood.Think:
                    P.Brow = F(.2 * k); P.Gy = F(-.45 * k); P.Reach = Reach.Chin;
                    break;
                case ChatMood.Pout:
                    P.Brow = F(.9 * k); P.Mouth = MouthStyle.Pout; P.Anger = F(.8 * k);
                    break;
                case ChatMood.Hungry:
                    P.Mouth = MouthStyle.Open; P.MouthOpen = F(.7 * k); P.Brow = -.3f; P.Cookie = 0;
                    break;
                case ChatMood.Battle:
                    P.Aura = F(.5 * k); P.AuraKind = Who == Persona.Vegeta ? 3 : Who == Persona.Piccolo ? 5 : 1; P.AuraT = F(t);
                    P.Mouth = MouthStyle.Smile; P.Brow = .4f; P.HandL = P.HandR = HandStyle.Fist;
                    break;
                case ChatMood.Cheer:
                    P.ArmLA = F(20); P.ArmRA = F(20); P.HandL = P.HandR = HandStyle.Fist; P.Mouth = MouthStyle.Open; P.MouthOpen = F(.5 * k);
                    break;
                case ChatMood.Praise:
                    P.Reach = Reach.Flex; P.Eyes = EyeStyle.Happy; P.Mouth = MouthStyle.Smile;
                    break;
                case ChatMood.Love:
                    P.Flush = F(.8 * k); P.Eyes = EyeStyle.Happy; P.Reach = Reach.Chest;
                    break;
                case ChatMood.Laugh:
                    P.Eyes = EyeStyle.Happy; P.Mouth = MouthStyle.Open; P.MouthOpen = F(.8 * k);
                    break;
                case ChatMood.Proud:
                    P.Reach = Reach.Hips; P.Brow = .2f; P.Mouth = MouthStyle.Smile;
                    break;
                case ChatMood.Blank:
                    P.Eyes = EyeStyle.Open; P.OpenL = P.OpenR = .5f; P.Gy = .4f;
                    break;
                default:
                    P.Mouth = MouthStyle.Smile;
                    break;
            }
        }
    }
}
