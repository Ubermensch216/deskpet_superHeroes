using System;
using System.Collections.Generic;
using System.Drawing;

namespace SuperHeroes
{
    /// <summary>시그니처 기술 정의: 이름, 설명, 재생 시간.</summary>
    internal sealed class SigDef
    {
        public string Name, Desc;
        public double Dur;
        public SigDef(string name, double dur, string desc) { Name = name; Dur = dur; Desc = desc; }
        public string Label { get { return "★ " + Name; } }
    }

    /// <summary>수퍼히어로 4인 각 4가지씩, 총 16가지의 철저한 고증 시그니처 행동.</summary>
    internal static class Signatures
    {
        internal static readonly SigDef[][] All =
        {
            // 0: 수퍼맨
            new[]
            {
                new SigDef("히트 비전", 7.0, "두 눈이 붉게 타오르며 바닥으로 강렬한 고열 레이저 빔을 방출"),
                new SigDef("냉기 입김", 6.5, "가슴 깊이 숨을 들이쉰 뒤 입으로 푸른 냉기 폭풍을 뿜어 얼음 결정 생성"),
                new SigDef("초음속 펀치", 7.2, "하늘로 로켓처럼 솟구쳐 소닉 붐을 일으킨 뒤 급강하 슈퍼맨 펀치"),
                new SigDef("셔츠 찢기 (S 각성)", 6.8, "기자 클라크 켄트가 정장 셔츠를 힘차게 찢어 안경을 벗고 가슴 속 희망의 S 심볼과 함께 수퍼맨으로 각성"),
            },
            // 1: 배트맨
            new[]
            {
                new SigDef("배트랭 투척", 6.5, "벨트에서 배트랭을 꺼내 회전 투척하고 날아 돌아온 배트랭을 완벽 캐치"),
                new SigDef("그래플건 발사", 6.8, "그래플건을 위쪽 창문으로 발사하여 와이어를 타고 밤하늘로 솟구침"),
                new SigDef("연막탄 은신", 7.0, "바닥에 연막탄을 터뜨려 짙은 연무 속으로 숨었다가 번뜩이는 안광과 재등장"),
                new SigDef("가고일 응시", 7.5, "석조 가고일 위에서 망토를 박쥐 날개처럼 거대하게 펼치며 도시를 감시"),
            },
            // 2: 스파이더맨
            new[]
            {
                new SigDef("웹 스윙 (THWIP!)", 6.8, "손가락 제스처로 거미줄을 쏘아 매달려 포물선으로 경쾌하게 도심 스윙"),
                new SigDef("스파이더 센스", 6.2, "머리 주위에 찌릿찌릿 위험 감지 파형이 번쩍이며 아크로바틱 백플립 회피"),
                new SigDef("거꾸로 매달리기", 7.2, "거미줄 한 가닥에 거꾸로 매달려 흔들거리며 손을 흔들고 피자를 냠냠"),
                new SigDef("거미줄 네트", 6.5, "양손 웹슈터를 교차 발사해 전방에 거대한 방사형 거미줄 그물망 전개"),
            },
            // 3: 토르
            new[]
            {
                new SigDef("묠니르 풍차 회전", 7.0, "가죽 스트랩을 잡고 묠니르를 고속 회전시켜 에너지 방어막과 바람 폭풍 형성"),
                new SigDef("천둥 벼락 소환", 7.5, "묠니르를 하늘 높이 치켜들자 하늘에서 거대한 벼락이 내리꽂히며 전신 감전"),
                new SigDef("지면 강타 충격파", 7.0, "도약 후 묠니르로 땅을 쾅 내리쳐 대지 균열과 함께 푸른 번개 분출"),
                new SigDef("묠니르 투척 & 귀환", 6.8, "묠니르를 회전 투척해 표적을 강타하고 손을 뻗어 번개 궤적으로 되돌려 받음"),
            },
            // 4: 원더우먼
            new[]
            {
                new SigDef("은팔찌 방어", 6.6, "양팔을 교차해 은팔찌로 날아오는 총알을 연달아 튕겨 내며 황금빛 불꽃을 터뜨림"),
                new SigDef("진실의 올가미", 6.8, "허리의 황금 올가미를 꺼내 머리 위로 휘돌려 던져 거짓을 꿰뚫는 빛으로 포박"),
                new SigDef("검과 방패", 7.0, "아마존의 방패로 막아 서고 검을 크게 휘둘러 호쾌한 참격을 그림"),
                new SigDef("아마존의 비행", 7.0, "몸을 낮췄다 솟구쳐 별빛 꼬리를 끌며 하늘을 가르고 사뿐히 착지"),
            },
        };

        internal static SigDef Get(Persona who, int idx)
        {
            int pIdx = (int)who;
            if (pIdx < 0 || pIdx >= All.Length) pIdx = 0;
            return All[pIdx][Math.Max(0, Math.Min(3, idx))];
        }

        private static Tuple<double, string> S(double t, string text) { return Tuple.Create(t, text); }

        internal static string Shout(Persona who, int idx, double t)
        {
            Tuple<double, string>[] line;
            int key = (int)who * 4 + idx;
            switch (key)
            {
                // 수퍼맨
                case 0: // 히트 비전
                    line = new[] { S(0, "집중... 열을 모은다"), S(1.5, "히트 비전 발사--!!"), S(4.8, "후..."), S(5.5, "정리 완료!") }; break;
                case 1: // 냉기 입김
                    line = new[] { S(0, "흡...!"), S(1.8, "슈퍼 브레스--!!"), S(4.5, "휴~"), S(5.2, "시원하지?") }; break;
                case 2: // 초음속 펀치
                    line = new[] { S(0, "하늘 높이!"), S(1.5, "초음속 돌파--!!"), S(3.8, "슈퍼맨 펀치!!"), S(5.5, "정의는 승리합니다.") }; break;
                case 3: // 셔츠 찢기
                    line = new[] { S(0, "취재 중... 어? 위험 신호!"), S(1.6, "이곳엔 수퍼맨이 필요해!"), S(3.5, "희망은 사라지지 않아!"), S(5.2, "내가 곁에 있습니다.") }; break;

                // 배트맨
                case 4: // 배트랭
                    line = new[] { S(0, "타겟 확인..."), S(1.4, "배트랭 투척!"), S(3.8, "돌아왔군."), S(4.8, "정확한 명중이다.") }; break;
                case 5: // 그래플건
                    line = new[] { S(0, "탈출 루트 확보."), S(1.5, "그래플건 발사!"), S(3.8, "옥상 진입."), S(5.0, "어둠 속으로 사라진다.") }; break;
                case 6: // 연막탄
                    line = new[] { S(0, "사라질 시간이다."), S(1.2, "연막 전개...!"), S(3.5, "어둠이 나를 감싼다."), S(5.2, "나는 밤이다.") }; break;
                case 7: // 가고일
                    line = new[] { S(0, "고담의 밤을 내려다본다."), S(1.8, "범죄자들은 두려움에 떨 것이다."), S(5.2, "순찰을 계속하지.") }; break;

                // 스파이더맨
                case 8: // 웹 스윙
                    line = new[] { S(0, "웹슈터 장전!"), S(1.2, "THWIP! 도심 스윙이다!"), S(3.5, "야호~! 신난다!"), S(5.0, "친절한 이웃 도착!") }; break;
                case 9: // 스파이더 센스
                    line = new[] { S(0, "어? 찌릿찌릿한데...?"), S(1.5, "스파이더 센스 발동!"), S(3.0, "백플립 회피 성공!"), S(4.8, "내 감각은 못 속이지~") }; break;
                case 10: // 거꾸로 매달리기
                    line = new[] { S(0, "거꾸로 매달리기 모드!"), S(1.4, "흔들흔들~"), S(2.8, "피자 한 조각 냠냠!"), S(5.2, "다시 출동해볼까!") }; break;
                case 11: // 거미줄 네트
                    line = new[] { S(0, "조준 완료!"), S(1.2, "거미줄 그물망 발사!!"), S(3.2, "꼼짝 마라!"), S(4.8, "포획 완벽해!") }; break;

                // 토르
                case 12: // 묠니르 풍차
                    line = new[] { S(0, "묠니르여, 힘을 보여다오!"), S(1.5, "아스가르드의 돌풍을 받아라--!!"), S(4.5, "하하하! 상쾌하구나!"), S(5.6, "천둥의 힘이 용솟음친다!") }; break;
                case 13: // 천둥 벼락 소환
                    line = new[] { S(0, "천둥의 힘이여, 내게 오라!"), S(1.8, "오딘의 이름으로 벼락을 소환하노라!!"), S(4.5, "콰광--!!"), S(5.6, "이것이 천둥의 신이다!") }; break;
                case 14: // 지면 강타 충격파
                    line = new[] { S(0, "대지를 뒤흔들리라!"), S(1.6, "크아앗! 천둥 강타!!"), S(4.2, "쿵--!!"), S(5.4, "대지가 요동치는군!") }; break;
                case 15: // 묠니르 투척 & 귀환
                    line = new[] { S(0, "받아라, 묠니르!"), S(1.3, "날아가라--!!"), S(3.8, "돌아오너라!"), S(5.0, "자격을 갖춘 자만이 들 수 있지.") }; break;

                // 원더우먼
                case 16: // 은팔찌 방어
                    line = new[] { S(0, "총알? 얼마든지 쏴 봐."), S(1.4, "아마존의 팔찌는 뚫리지 않아!"), S(3.9, "팅! 팅! 팅--!!"), S(5.3, "그 정도론 어림없지.") }; break;
                case 17: // 진실의 올가미
                    line = new[] { S(0, "진실의 올가미, 준비."), S(1.5, "거짓은 통하지 않아--!!"), S(3.3, "자, 솔직하게 말해 봐!"), S(5.1, "진실은 언제나 빛나는 법이야.") }; break;
                case 18: // 검과 방패
                    line = new[] { S(0, "방패를 들어라!"), S(1.5, "아마존의 검을 받아라--!!"), S(4.0, "테미스키라의 영광을 위해!"), S(5.5, "전사는 물러서지 않아.") }; break;
                default: // 아마존의 비행
                    line = new[] { S(0, "도약 준비..."), S(1.4, "하늘로--!!"), S(2.8, "별빛처럼 날아오른다!"), S(5.4, "사랑과 정의로 지킬게.") }; break;
            }

            string cur = "";
            foreach (var l in line) if (t >= l.Item1) cur = l.Item2;
            return cur;
        }
    }

    /// <summary>Pet 클래스의 시그니처 실행 및 업데이트 부분 구현.</summary>
    internal sealed partial class Pet
    {
        private int pendingSig = -1;
        internal int PendingSig { get { return pendingSig; } set { pendingSig = value; } }

        private bool ConsumePendingSig(IWorld w)
        {
            if (pendingSig < 0 || (State != PetState.Walk && State != PetState.Pause)) return false;
            int idx = pendingSig; pendingSig = -1; StartSignature(w, idx); return true;
        }

        private static double Seg(double t, double a, double b) { return t <= a ? 0 : t >= b ? 1 : (t - a) / (b - a); }

        private void SigPose(Pose P, ref Look L, double t)
        {
            ApplySignature(ref L, t);
        }

        private int sigIndex;
        private double sigTime, sigDuration;
        private bool sigTeleported;

        public bool IsSignatureActive { get { return State == PetState.Resting && restPose == RestPose.Sig; } }
        public int CurrentSigIndex { get { return sigIndex; } }

        internal void StartSignature(IWorld w, int idx)
        {
            idx = Math.Max(0, Math.Min(3, idx));
            sigIndex = idx;
            SigDef def = Signatures.Get(Art.Persona, idx);
            sigDuration = def.Dur;
            sigTime = 0;
            sigTeleported = false;
            State = PetState.Resting;
            restPose = RestPose.Sig;
            StateT = 0;
            extraDuration = sigDuration;
            stateDur = sigDuration;
            Bubble = def.Name;
            BubbleUntil = w.Now + sigDuration;
        }

        private void EndSignature(IWorld w)
        {
            sigTeleported = false;
            SetState(PetState.Pause, w);
            stateDur = 1.5;
            Bubble = null;
        }

        internal void RequestSignature(IWorld w, int idx)
        {
            if (State == PetState.Drag) return;
            StartSignature(w, idx);
        }

        private void AdvanceSignature(IWorld w, double dt)
        {
            sigTime += dt;
            if (sigTime >= sigDuration)
            {
                EndSignature(w);
                return;
            }
            Bubble = Signatures.Shout(Art.Persona, sigIndex, sigTime);
        }

        private void SigBubble(ref Look L)
        {
            if (!IsSignatureActive) return;
            L.Bubble = Signatures.Shout(Art.Persona, sigIndex, sigTime);
            L.BubbleTitle = Signatures.Get(Art.Persona, sigIndex).Name;
        }

        private void ApplySignature(ref Look L, double now)
        {
            if (!IsSignatureActive) return;

            Pose P = L.Pose;
            double t = sigTime > 0 ? sigTime : StateT;
            float phase = (float)(t / sigDuration);
            P.SigAct = sigIndex;
            P.SigPhase = phase;
            P.SigT = (float)Math.Max(.001, t);

            switch (Art.Persona)
            {
                case Persona.Superman:
                    ApplySupermanSig(P, ref L, t);
                    break;
                case Persona.Batman:
                    ApplyBatmanSig(P, ref L, t);
                    break;
                case Persona.Spiderman:
                    ApplySpidermanSig(P, ref L, t);
                    break;
                case Persona.Thor:
                    ApplyThorSig(P, ref L, t);
                    break;
                case Persona.WonderWoman:
                    ApplyWonderSig(P, ref L, t);
                    break;
            }
        }

        private void ApplySupermanSig(Pose P, ref Look L, double t)
        {
            switch (sigIndex)
            {
                case 0: // 히트 비전
                    if (t < 1.5)
                    {
                        float ch = (float)(t / 1.5);
                        P.Brow = 0.6f; P.Eyes = EyeStyle.Narrow; P.Mouth = MouthStyle.Neutral;
                        P.HeatLaser = ch * 0.4f; P.BodyX = (float)(Math.Sin(t * 60) * 1.3 * ch);
                        P.Aura = .5f * ch; P.AuraColor = Color.FromArgb(255, 90, 50);
                    }
                    else if (t < 5.0)
                    {
                        // 열선을 뿜는 동안 몸이 반동으로 떨리고 눈은 붉게 타오른다
                        P.Brow = 0.9f; P.Mouth = MouthStyle.Grit;
                        P.HeatLaser = Smooth((t - 1.5) / .2);
                        P.Reach = Reach.Flex; P.CapeWind = 26f;
                        P.BodyX = (float)(Math.Sin(t * 70) * 1.8); P.Aura = .55f; P.AuraColor = Color.FromArgb(255, 90, 50);
                    }
                    else
                    {
                        P.HeatLaser = (float)Math.Max(0, 1 - (t - 5.0) / 1.5);
                        P.Mouth = MouthStyle.Smile;
                    }
                    break;

                case 1: // 냉기 입김
                    if (t < 1.8)
                    {
                        // 가슴을 한껏 부풀리며 숨을 들이마신다(몸이 뒤로 젖혀진다)
                        float ch = Smooth(t / 1.8);
                        P.Breath = (float)(t / 1.8); P.Mouth = MouthStyle.Open;
                        P.Reach = Reach.Chest; P.HeadTilt = 5f * ch; P.Aura = .4f * ch; P.AuraColor = Color.FromArgb(150, 215, 255);
                    }
                    else if (t < 4.8)
                    {
                        P.Mouth = MouthStyle.Pout; P.FreezeMist = 1.0f;
                        P.HeadTilt = -5f; P.CapeWind = 16f; P.Aura = .45f; P.AuraColor = Color.FromArgb(150, 215, 255);
                        P.BodyX = (float)(Math.Sin(t * 40) * 1.0);
                    }
                    else
                    {
                        P.FreezeMist = (float)Math.Max(0, 1 - (t - 4.8) / 1.5);
                        P.Mouth = MouthStyle.Smile;
                    }
                    break;

                case 2: // 초음속 펀치: 웅크려 기를 모음 → 도약 → 초음속 비행 → 주먹 → 착지
                    {
                        float tt = (float)t;
                        float crouch = Smooth(tt / 1.3) * (1 - Smooth((tt - 1.45) / .12));
                        float rise = Smooth((tt - 1.5) / .35), land = Smooth((tt - 4.2) / .8);
                        float alt = -100f * rise * (1 - land);
                        if (tt >= 2.0f && tt < 4.0f) alt += (float)Math.Sin((tt - 2.0) * 3) * 5;
                        P.BodyY = 26f * crouch + alt;
                        P.SonicGlow = rise * (1 - Smooth((tt - 4.0) / .5));
                        P.HandL = HandStyle.Fist; P.HandR = HandStyle.Fist;
                        if (tt < 1.5f)
                        {
                            P.Reach = Reach.Lap; P.Brow = .7f; P.Mouth = MouthStyle.Grit;
                            P.HandRTo = new PointF(262, 326); P.HandLTo = new PointF(132, 332);   // 주먹을 뒤로 당긴다
                            P.BodyX = (float)(Math.Sin(t * 55) * 1.6 * crouch); P.Knee = 1.4f * crouch;
                            P.Aura = .7f * crouch; P.AuraColor = Color.FromArgb(150, 215, 255);
                        }
                        else if (tt < 5.5f)
                        {
                            float punch = Smooth((tt - 3.45) / .3) * (1 - Smooth((tt - 4.2) / .5));
                            P.Reach = Reach.Lap;
                            P.HandRTo = new PointF(95 - 80 * punch, 252 + 34 * punch); P.HandLTo = new PointF(290, 300);
                            P.FootLY = P.FootRY = Math.Min(0f, P.BodyY * .85f); P.FootLX = -10; P.FootRX = 18;
                            P.CapeWind = 46f * rise * (1 - land) + 8f; P.HeadTilt = -5f * rise * (1 - land);
                            P.Mouth = MouthStyle.Open; P.Brow = .9f;
                            P.Aura = .8f * rise * (1 - land); P.AuraColor = Color.FromArgb(160, 220, 255);
                            if (tt > 3.8f && tt < 4.3f) P.BodyX = (float)(Math.Sin(t * 80) * 4.0 * (1 - (tt - 3.8) / .5));
                        }
                        else
                        {
                            P.Reach = Reach.Hips; P.Mouth = MouthStyle.Smile;
                        }
                    }
                    break;

                case 3: // 셔츠 찢기 (S 각성) — 기자 클라크 켄트에서 수퍼맨으로
                    if (t < 1.6)
                    {
                        // 신문사 기자 차림으로 취재 중: 정장·넥타이·뿔테 안경, 문득 위험 신호를 느낀다
                        P.Disguise = 1f; P.Reach = Reach.Lap;
                        P.HeadTilt = (float)(3 + Math.Sin(t * 5) * 2); P.Eyes = EyeStyle.Open;
                        if (t > 1.0) { P.Brow = 0.5f; P.Mouth = MouthStyle.Open; }
                    }
                    else if (t < 3.45)
                    {
                        // 양손으로 셔츠를 좌우로 힘껏 찢어 젖힌다 → 가슴 속 S 가 드러나고 안경·넥타이가 날아간다
                        float open = (float)Math.Min(1, (t - 1.6) / 1.2);
                        P.Disguise = 1f; P.ShirtOpen = open;
                        P.HandLTo = new PointF(200 - 26 - open * 52, 322 - open * 6);
                        P.HandRTo = new PointF(200 + 26 + open * 52, 322 - open * 6);
                        P.Brow = 0.9f; P.Mouth = MouthStyle.Grit; P.BodyY = (float)(-3 * open);
                        P.Aura = 0.8f * open; P.AuraColor = Color.FromArgb(255, 220, 50);
                        if (t > 3.2) P.Transform = (float)Math.Min(1, (t - 3.2) / 0.25);
                    }
                    else
                    {
                        // 변신 섬광 → 수퍼맨 수트, 희망의 광채
                        float fade = (float)Math.Max(0, Math.Min(1, 1 - (t - 5.0) / 1.2));
                        P.Disguise = 0f; P.ShirtOpen = fade; P.Reach = Reach.Hips;
                        P.Transform = (float)Math.Max(0, Math.Min(1, 1 - (t - 3.45) / 0.8));
                        P.Mouth = MouthStyle.Smile; P.Brow = 0.4f; P.CapeWind = 20f * (float)Math.Max(0, Math.Min(1, 1 - (t - 3.45) / 1.5));
                        P.Aura = 0.8f * Math.Max(fade, 0.25f); P.AuraColor = Color.FromArgb(255, 220, 50);
                    }
                    break;
            }
        }

        private void ApplyBatmanSig(Pose P, ref Look L, double t)
        {
            switch (sigIndex)
            {
                case 0: // 배트랭 투척
                    {
                        // 조준(락온) → 팔을 뒤로 젖혔다가 휘둘러 던짐 → 팔짱 끼고 지켜봄 → 되돌아온 배트랭을 척 받는다
                        float tt = (float)t;
                        float wind = Smooth((tt - 1.15) / .3), thrust = Smooth((tt - 1.45) / .12), catchUp = Smooth((tt - 3.45) / .3) * (1 - Smooth((tt - 3.95) / .35));
                        P.HandL = HandStyle.Fist; P.HandR = HandStyle.Fist;
                        if (tt < 1.15f) { P.Reach = Reach.Hips; P.Brow = .6f; P.HeadTilt = -4f * Smooth(tt / 1f); P.Mouth = MouthStyle.Neutral; }
                        else if (tt < 3.45f)
                        {
                            P.Reach = Reach.Lap; P.Brow = .8f; P.Mouth = MouthStyle.Grit;
                            P.HandRTo = new PointF(262 + (92 - 262) * thrust, 224 + (270 - 224) * thrust);   // 뒤로 젖힌 팔 → 앞으로 휘두름
                            P.HandLTo = new PointF(246, 346);
                            P.BodyX = -7f * thrust + (tt > 1.45f && tt < 1.9f ? (float)Math.Sin(t * 70) * 1.5f : 0f);
                            P.Aura = .0f;
                        }
                        else if (tt < 4.3f)
                        {
                            P.Reach = Reach.Lap; P.Brow = .6f;
                            P.HandRTo = new PointF(250 - 158 * catchUp, 330 - 60 * catchUp); P.HandLTo = new PointF(246, 346);
                            P.CapeWind = 10f * catchUp;
                        }
                        else { P.Reach = Reach.Cross; P.Mouth = MouthStyle.Neutral; }
                        P.Batarang = (tt >= 1.45f && tt < 4.0f) ? 1f : 0f;
                    }
                    break;

                case 1: // 그래플건: 겨냥 → 발사 → 끌려 올라감 → 옥상 안착
                    {
                        float tt = (float)t;
                        float rise = Smooth((tt - 2.1) / 1.2), settle = Smooth((tt - 3.9) / .5);
                        float alt = -100f * rise * (1 - settle);
                        if (tt < 1.5f)
                        {
                            P.Reach = Reach.Lap; P.HeadTilt = -10f * Smooth(tt / 1.2); P.Brow = .6f;
                            P.HandRTo = new PointF(160 - 20 * Smooth(tt / 1.4), 250 - 40 * Smooth(tt / 1.4)); P.HandLTo = new PointF(256, 340);   // 위를 겨냥
                        }
                        else if (tt < 4.5f)
                        {
                            P.Grapnel = 1f; P.Reach = Reach.Lap;
                            P.HandRTo = new PointF(140, 206); P.HandLTo = new PointF(176, 214);     // 두 손으로 총을 꽉 쥔다
                            P.BodyY = alt; P.FootLY = P.FootRY = alt * .85f; P.FootLX = -8; P.FootRX = 10;
                            P.BodyX = -10f * rise * (1 - settle) + (float)Math.Sin(tt * 3.2) * 4f * rise * (1 - settle);
                            P.CapeWind = 30f * rise; P.CapeWing = .5f * rise * (1 - settle); P.Brow = .8f; P.Mouth = MouthStyle.Grit;
                        }
                        else { P.BodyY = 0; P.Reach = Reach.Hips; P.Mouth = MouthStyle.Neutral; }
                    }
                    break;

                case 2: // 연막탄: 툭 던져 터뜨리고 → 망토로 몸을 감싸며 연기 속으로 사라진다
                    {
                        float tt = (float)t;
                        float cover = Smooth((tt - 1.55) / 1.5) * Smooth((6.3 - tt) / 1.5);
                        P.SmokeBomb = tt >= 1.2f ? Math.Min(1f, Smooth((tt - 1.2) / .6) * Smooth((7.0 - tt) / 1.4)) : 0f;
                        if (tt < 1.2f) { P.Reach = Reach.Hips; P.Brow = .5f; P.HandRTo = null; }
                        else if (tt < 1.7f)
                        {
                            float th = Smooth((tt - 1.2) / .25);
                            P.Reach = Reach.Lap; P.HandRTo = new PointF(250 - 110 * th, 300 + 80 * th); P.HandLTo = new PointF(250, 350); P.BodyY = 8f * th; P.Brow = .7f;
                        }
                        else
                        {
                            P.Reach = Reach.Cross; P.CapeWing = .85f * cover; P.CapeWind = 22f * cover; P.BodyY = 10f * cover; P.Brow = .7f;
                            P.BodyX = (float)Math.Sin(t * 2.2) * 3f * cover;
                        }
                    }
                    break;

                case 3: // 가고일 응시: 비 내리는 옥상 난간에 웅크려 고담의 밤을 굽어본다
                    {
                        float tt = (float)t, perch = Smooth(tt / 1.4), rel = Smooth((tt - 6.4) / 1.0);
                        P.BodyY = 35f * perch * (1 - rel); P.Knee = 1.2f * perch * (1 - rel);
                        P.CapeWing = Math.Min(1f, Smooth((tt - 1.0) / 1.0)) * (1 - Smooth((tt - 6.4) / 1.4));
                        P.Brow = .7f * perch; P.Mouth = MouthStyle.Neutral;
                        P.HeadTilt = (float)(Math.Sin(tt * .9) * 7.0 * perch * (1 - rel));
                        P.CapeWind = 14f * perch;
                        P.Aura = .25f * perch * (1 - rel); P.AuraColor = Color.FromArgb(130, 150, 220);
                        if (tt >= 1.9f && tt < 2.3f) P.BodyX = (float)Math.Sin(t * 90) * 1.5f;
                    }
                    break;
            }
        }

        private void ApplySpidermanSig(Pose P, ref Look L, double t)
        {
            switch (sigIndex)
            {
                case 0: // 웹 스윙
                    {
                        // 손목으로 거미줄을 쏘고(THWIP!) → 시계추처럼 크게 흔들리며 스윙 → 가볍게 착지
                        float tt = (float)t, ramp = SpiderMoves.SwingRamp(tt);
                        if (tt < .95f) { P.Reach = Reach.WebThrow; P.HandR = HandStyle.Web; P.Brow = .5f; P.HeadTilt = -3f * Smooth(tt / .8); }
                        else if (tt < 4.5f)
                        {
                            P.WebSwing = 1.0f; P.Reach = Reach.Lap; P.HandR = HandStyle.Web;
                            float dx = SpiderMoves.SwingDx(tt), dy = SpiderMoves.SwingDy(tt), sw = SpiderMoves.SwingSin(tt);
                            P.BodyX = dx; P.BodyY = dy;
                            P.HandRTo = new PointF(112 + dx * .35f, 232 + dy * .6f);          // 줄을 쥔 팔을 위로 쭉 뻗는다
                            P.HandLTo = new PointF(262 - sw * 14f, 330 + dy * .3f);
                            P.FootLY = P.FootRY = dy * .85f; P.FootLX = -14 + sw * 10; P.FootRX = 18 + sw * 10; P.FootLRot = P.FootRRot = -14f * sw;
                            P.Knee = 1.2f * ramp; P.HeadTilt = -8f * sw; P.Mouth = MouthStyle.Open; P.Eyes = EyeStyle.Happy;
                        }
                        else if (tt < 5.4f)
                        {
                            float land = 1 - Smooth((tt - 4.55) / .8);
                            P.BodyY = 16f * land; P.Knee = 1.4f * land; P.Reach = Reach.Hips; P.Mouth = MouthStyle.Smile;
                        }
                        else { P.Reach = Reach.Hips; P.Mouth = MouthStyle.Smile; }
                    }
                    break;

                case 1: // 스파이더 센스: 찌릿 → 수리검 감지 → 백플립 회피 → 안도
                    {
                        float tt = (float)t, u = (tt - 1.8f) / 1.6f;
                        if (tt < 1.8f)
                        {
                            P.SpiderSense = 1.0f; P.Eyes = EyeStyle.Shock; P.Reach = Reach.Cross;
                            P.HeadTilt = (float)Math.Sin(t * 22) * 7f; P.BodyX = (float)Math.Sin(t * 45) * 1.8f * Smooth(tt / .3);
                            P.BodyY = tt > 1.25f ? 12f * Smooth((tt - 1.25) / .5) : 0f; P.Knee = tt > 1.25f ? 1.3f : 0f;   // 웅크려 뛸 준비
                        }
                        else if (tt < 3.4f)
                        {
                            float arc = (float)Math.Sin(Math.PI * u);
                            P.SpiderSense = Math.Max(0f, 1f - (tt - 1.8f) / .6f); P.Eyes = EyeStyle.Shock; P.Reach = Reach.Cross;
                            P.BodyY = 12f * (1 - Smooth((tt - 1.8) / .15)) - 92f * arc; P.Flip = SpiderMoves.FlipAngle(tt);
                            P.FootLY = P.FootRY = Math.Min(0f, P.BodyY * .85f); P.Knee = 1.6f;
                        }
                        else if (tt < 4.2f)
                        {
                            float land = 1 - Smooth((tt - 3.45) / .6);
                            P.BodyY = 22f * land; P.Knee = 1.5f * land; P.Reach = Reach.Hips; P.Eyes = EyeStyle.Happy;
                        }
                        else { P.Eyes = EyeStyle.Happy; P.Reach = Reach.Chin; P.Mouth = MouthStyle.Smile; }
                    }
                    break;

                case 2: // 거꾸로 매달리기: 천장에 줄을 쏘고 → 거꾸로 뒤집혀 흔들리며 → 피자 냠냠 → 되돌아와 출동
                    {
                        float tt = (float)t;
                        float flip = Smooth((tt - 1.25) / .75) * (1 - Smooth((tt - 6.0) / .7));
                        P.UpsideDown = flip;
                        if (tt < 1.25f)
                        {
                            P.Reach = Reach.Lap; P.HandR = HandStyle.Web; P.HandRTo = new PointF(150, 196 - 70 * Smooth((tt - .3) / .25)); P.HandLTo = new PointF(262, 340);
                            P.Brow = .5f; P.HeadTilt = -6f * Smooth(tt / 1f);
                        }
                        else
                        {
                            P.HangSwing = (float)Math.Sin((tt - 1.6) * 2.2) * 13f * flip;
                            P.Reach = Reach.Lap; P.Eyes = EyeStyle.Happy; P.Mouth = tt > 2.8f && tt < 5.4f ? MouthStyle.Open : MouthStyle.Smile;
                            // 뒤집힌 몸에서 팔은 머리 쪽(리그 위쪽)으로 늘어진다: 오른손은 피자, 왼손은 흔든다
                            P.HandRTo = new PointF(248, 140 + (float)Math.Sin(tt * 6) * 4f);
                            P.HandLTo = new PointF(150 - (float)Math.Sin(tt * 4) * 8f, 120 + (float)Math.Sin(tt * 5) * 14f);
                            P.HeadTilt = (float)Math.Sin(tt * 3) * 8f;
                        }
                    }
                    break;

                case 3: // 거미줄 네트: 양손 동시 발사 → 그물이 펼쳐져 달라붙고 → 악당이 버둥대는 걸 팽팽히 당긴다
                    {
                        float tt = (float)t;
                        float net = tt < 1.2f ? 0f : tt < 6.0f ? 1f : Math.Max(0f, 1f - (tt - 6.0f) / .5f);
                        if (tt < 1.2f) { P.Reach = Reach.Cross; P.Brow = .6f; }
                        else if (tt < 4.7f)
                        {
                            P.WebNet = net; P.Reach = Reach.Lap; P.HandL = HandStyle.Web; P.HandR = HandStyle.Web;
                            P.HandLTo = new PointF(116, 300); P.HandRTo = new PointF(100, 286);
                            P.Brow = .7f; P.Mouth = tt > 2.4f ? MouthStyle.Grit : MouthStyle.Open;
                            P.BodyX = (tt < 1.5f ? 6f * (1f - (tt - 1.2f) / .3f) : 0f) + (tt > 2.4f ? (float)Math.Sin(t * 13) * 2.2f : 0f);
                            P.Knee = 1.0f;
                        }
                        else
                        {
                            P.WebNet = net; P.Reach = Reach.Hips; P.Mouth = MouthStyle.Smile;
                        }
                    }
                    break;
            }
        }

        private void ApplyThorSig(Pose P, ref Look L, double t)
        {
            switch (sigIndex)
            {
                case 0: // 묠니르 풍차 회전
                    if (t < 1.5)
                    {
                        // 자루 끈을 쥐고 묠니르를 치켜들며 기를 모은다
                        float w = Smooth(t / 1.5);
                        P.Reach = Reach.Hammer; P.Aura = .5f * w; P.AuraColor = Color.FromArgb(140, 210, 255);
                        P.CapeWind = 12f * w; P.Brow = .5f * w; P.Mouth = MouthStyle.Grit;
                    }
                    else if (t < 1.5 + ThorWhirl.Duration)
                    {
                        // 회전이 빨라질수록 방어막·돌풍이 커지고, 몸은 원심력에 휘청인다
                        float T = (float)(t - 1.5), k = ThorWhirl.Intensity(T);
                        P.Reach = Reach.Hammer; P.MjolnirSpin = Math.Max(.06f, k); P.SpinT = Math.Max(.001f, T);
                        P.CapeWind = 35f + 35f * k; P.Aura = k; P.AuraColor = Color.FromArgb(150, 220, 255);
                        P.Mouth = MouthStyle.Open; P.Brow = .6f; P.HeadTilt = -4f * k;
                        P.BodyX = (float)(Math.Sin(T * 7.5) * 4.0 * k + Math.Sin(t * 61) * 1.4 * k);
                        P.BodyY = (float)(Math.Abs(Math.Sin(T * 5.0)) * -3.0 * k);
                    }
                    else
                    {
                        P.Reach = Reach.Hips;
                        P.Mouth = MouthStyle.Smile;
                    }
                    break;

                case 1: // 천둥 벼락 소환
                    if (t < 1.8)
                    {
                        // 묠니르를 쥔 팔을 하늘로 쭉 뻗어 올린다(반대 손은 주먹)
                        double up = Ease(t / 1.3);
                        P.Raise = F(up); P.HeadTilt = F(-12 * up); P.Gy = F(-.6 * up); P.HandL = HandStyle.Fist;
                    }
                    else if (t < 6.9)
                    {
                        // 먹구름이 모이고(충전) → 세 번 내리꽂히는 벼락(점점 강해짐) → 감전 잔광
                        float lt = (float)(t - 1.8), strike = ThorLightning.Strike(lt);
                        P.Raise = 1; P.HeadTilt = -10f; P.Gy = -.6f; P.HandL = HandStyle.Fist;
                        P.Lightning = 1.0f; P.LightningT = lt;
                        P.Aura = 1.0f; P.AuraColor = strike > .3f ? Color.FromArgb(220, 240, 255) : Color.FromArgb(120, 200, 255);
                        P.Mouth = MouthStyle.Open;
                        if (strike > 0.05f)
                        {
                            // 감전: 온몸이 파르르 떨리고 눈이 번쩍 뜨인다
                            P.BodyX = (float)(Math.Sin(t * 95) * 5.5 * strike);
                            P.BodyY = (float)(Math.Cos(t * 83) * 3.0 * strike);
                            P.Eyes = EyeStyle.Open; P.EyeScale = 1.12f; P.PupilR = .45f;
                        }
                    }
                    else
                    {
                        P.Lightning = 0; P.Reach = Reach.Hammer; P.Raise = F(Math.Max(0, 1 - (t - 6.9) / .5));   // 팔을 천천히 내린다
                    }
                    break;

                case 2: // 지면 강타 충격파
                    if (t < ThorSlam.End)
                    {
                        // 웅크려 기를 모음 → 번개를 두른 채 높이 도약 → 묠니르를 내리꽂음 → 충격에 휘청 → 여진 → 일어섬
                        float tt = (float)t, s = ThorSlam.Swing(tt);
                        float crouch = Smooth(tt / .7), lift = Smooth((tt - .7) / .55), rise = Smooth((tt - 4.8) / .8);
                        float u1 = tt - ThorSlam.Hit1, u2 = tt - ThorSlam.Hit2;
                        float sh = (u1 > 0 ? (float)Math.Exp(-u1 * 4.5) : 0f) + (u2 > 0 ? .55f * (float)Math.Exp(-u2 * 5.5) : 0f);
                        P.GroundCrush = 1f; P.CrushT = Math.Max(.001f, tt);
                        P.HandL = HandStyle.Fist; P.Mouth = MouthStyle.Open; P.Brow = .7f;
                        P.Aura = tt < ThorSlam.Hit1 ? .5f + .5f * crouch : .45f + .55f * sh; P.AuraColor = Color.FromArgb(150, 215, 255);

                        float by;
                        if (tt < .7f) by = 18f * crouch;
                        else if (tt < 1.5f) by = 18f + (-85f - 18f) * lift;
                        else if (tt < ThorSlam.Hit1) by = -85f + (22f + 85f) * s;
                        else by = 22f * (1f - rise);
                        P.BodyY = by + (float)Math.Cos(t * 77) * 3.5f * sh + 6f * (u2 > 0 && u2 < .25f ? 1f : 0f);
                        P.BodyX = (float)Math.Sin(t * 95) * 6f * sh;
                        P.FootLY = P.FootRY = Math.Min(0f, by * .85f); P.Knee = Math.Max(0f, by) / 18f;
                        P.HeadTilt = -9f * lift * (1f - s);
                        P.CapeWind = 20f + 30f * lift + 40f * sh;

                        if (tt < 1.5f)
                        {
                            P.Reach = Reach.Hammer; P.Raise = .35f * crouch + .65f * lift;   // 묠니르를 하늘 높이 치켜든다
                        }
                        else
                        {
                            P.Reach = Reach.Lap; P.Raise = 1f - s;
                            PointF tgt = new PointF(ThorSlam.ImpactX, ThorSlam.HandY);
                            P.HandRTo = new PointF(tgt.X + (252f - tgt.X) * rise, tgt.Y + (418f - tgt.Y) * rise);
                        }
                    }
                    else
                    {
                        P.Reach = Reach.Hips; P.Mouth = MouthStyle.Smile;
                    }
                    break;

                case 3: // 묠니르 투척 & 귀환
                    if (t < 1.3)
                    {
                        P.Reach = Reach.Flex;
                    }
                    else if (t < 3.8)
                    {
                        P.HammerThrow = (float)((t - 1.3) / 2.5);
                        P.Reach = Reach.Front;
                    }
                    else if (t < 5.4)
                    {
                        P.HammerThrow = (float)Math.Max(0, 1 - (t - 3.8) / 1.6);
                        P.Reach = Reach.Front;
                    }
                    else
                    {
                        P.HammerThrow = 0; P.Reach = Reach.Hammer;
                        P.Mouth = MouthStyle.Smile;
                    }
                    break;
            }
        }

        private static float Smooth(double x) { float v = (float)Math.Max(0, Math.Min(1, x)); return v * v * (3 - 2 * v); }

        private void ApplyWonderSig(Pose P, ref Look L, double t)
        {
            switch (sigIndex)
            {
                case 0: // 은팔찌 방어: 양팔을 X 로 교차해 날아오는 총알을 연달아 튕겨 낸다
                    if (t < 1.2)
                    {
                        P.Reach = Reach.Hips; P.Brow = 0.5f; P.Mouth = MouthStyle.Neutral;
                    }
                    else
                    {
                        float on = t < 5.2 ? Smooth((t - 1.2) / 0.4) : 1 - Smooth((t - 5.2) / 0.6);
                        P.Bracers = on; P.BracerT = (float)(t - 1.2);
                        if (on > 0.05f)
                        {
                            P.HandLTo = new PointF(200 + 30, 292); P.HandRTo = new PointF(200 - 30, 288);
                            P.Brow = 0.8f; P.Mouth = MouthStyle.Grit; P.HeadTilt = -2f;
                            P.BodyX = (float)(Math.Sin(t * 47) * 1.6 * on);     // 총알이 부딪힐 때마다 움찔
                        }
                        else { P.Reach = Reach.Hips; P.Mouth = MouthStyle.Smile; }
                    }
                    break;

                case 1: // 진실의 올가미: 허리에서 꺼내 → 머리 위로 휘돌려 → 던져 포박
                    {
                        float on = t < 0.4 ? Smooth(t / 0.4) : t < 6.3 ? 1 : 1 - Smooth((t - 6.3) / 0.5);
                        P.LassoSpin = on; P.LassoT = (float)Math.Min(t, 6.3);
                        if (t < 1.4) { P.HandRTo = new PointF(262, 420); P.Brow = 0.4f; }
                        else if (t < 3.3) { P.HandRTo = new PointF(200 + 36, 196); P.Brow = 0.6f; P.Mouth = MouthStyle.Smile; P.HeadTilt = -4f; }
                        else if (t < 6.3) { P.HandRTo = new PointF(200 - 56, 262); P.Mouth = MouthStyle.Open; P.Brow = 0.7f; P.BodyX = (float)(t < 3.6 ? -4 : 0); }
                        else { P.Reach = Reach.Hips; P.Mouth = MouthStyle.Smile; }
                    }
                    break;

                case 2: // 검과 방패: 방패를 세워 막고 검을 두 번 크게 휘두른다
                    {
                        float sh = t < 1.3 ? Smooth(t / 1.3) : t < 6.2 ? 1 : 1 - Smooth((t - 6.2) / 0.6);
                        float s1 = Smooth((t - 2.6) / 0.8) * (1 - Smooth((t - 3.4) / 0.5));
                        float s2 = Smooth((t - 3.9) / 0.6) * (1 - Smooth((t - 5.4) / 0.7));
                        float sl = Math.Max(s1, s2);
                        P.ShieldGuard = sh; P.SwordSlash = sl;
                        if (sh > 0.05f)
                        {
                            P.HandLTo = new PointF(200 - 62, 322); P.HandRTo = new PointF(200 + 56 - 96 * sl, 272 + 26 * sl);
                            P.Brow = 0.7f; P.Mouth = sl > 0.15f ? MouthStyle.Open : MouthStyle.Grit;
                            if (t > 1.7 && t < 2.5) P.BodyX = (float)(Math.Sin(t * 52) * 2.0);     // 방패에 부딪히는 충격
                            P.BodyX += -6f * sl;
                        }
                        else { P.Reach = Reach.Hips; P.Mouth = MouthStyle.Smile; }
                    }
                    break;

                case 3: // 아마존의 비행: 웅크렸다 솟구쳐 별빛 꼬리를 끌며 날고 사뿐히 착지
                    {
                        float crouch = 14f * Smooth(t / 1.2) * (1 - Smooth((t - 1.4) / 0.3));
                        float rise = Smooth((t - 1.4) / 1.0), land = Smooth((t - 5.0) / 1.0);
                        float alt = -92f * rise * (1 - land);
                        if (t >= 2.4 && t < 5.0) alt += (float)Math.Sin((t - 2.4) * 2.4) * 8;
                        P.BodyY = crouch + alt;
                        float fl = Smooth((t - 1.4) / 0.8) * (1 - 0.7f * land);
                        if (t > 6.0) fl = 0;
                        P.AmazonFlight = fl; P.FlightT = (float)Math.Max(0, t - 1.4);
                        if (rise > 0.05f && land < 0.9f)
                        {
                            P.HandLTo = new PointF(200 - 92, 136); P.HandRTo = new PointF(200 + 44, 330);
                            P.FootLY = P.FootRY = P.BodyY * 0.85f; P.FootLX = -12; P.FootRX = 16; P.FootLRot = P.FootRRot = -12;
                            P.CapeWind = 42f; P.HeadTilt = -6f; P.Mouth = MouthStyle.Smile; P.Brow = 0.5f;
                            P.Aura = 0.8f * (1 - land); P.AuraColor = Color.FromArgb(255, 214, 90);
                        }
                        else if (t < 1.4) { P.Reach = Reach.Hips; P.Brow = 0.5f; }
                        else { P.Reach = Reach.Hips; P.Mouth = MouthStyle.Smile; if (t < 6.3) P.BodyY += 6; }
                    }
                    break;
            }
        }
    }
}
