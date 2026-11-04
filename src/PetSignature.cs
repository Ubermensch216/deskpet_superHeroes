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
                default: // 묠니르 투척 & 귀환
                    line = new[] { S(0, "받아라, 묠니르!"), S(1.3, "날아가라--!!"), S(3.8, "돌아오너라!"), S(5.0, "자격을 갖춘 자만이 들 수 있지.") }; break;
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
            }
        }

        private void ApplySupermanSig(Pose P, ref Look L, double t)
        {
            switch (sigIndex)
            {
                case 0: // 히트 비전
                    if (t < 1.5)
                    {
                        P.Brow = 0.6f; P.Eyes = EyeStyle.Narrow; P.Mouth = MouthStyle.Neutral;
                        P.HeatLaser = (float)(t / 1.5) * 0.4f;
                    }
                    else if (t < 5.0)
                    {
                        P.Brow = 0.9f; P.Mouth = MouthStyle.Open;
                        P.HeatLaser = 1.0f;
                        P.Reach = Reach.Flex;
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
                        P.Breath = (float)(t / 1.8); P.Mouth = MouthStyle.Open;
                        P.Reach = Reach.Chest;
                    }
                    else if (t < 4.8)
                    {
                        P.Mouth = MouthStyle.Pout; P.FreezeMist = 1.0f;
                        P.HeadTilt = -4f;
                    }
                    else
                    {
                        P.FreezeMist = (float)Math.Max(0, 1 - (t - 4.8) / 1.5);
                        P.Mouth = MouthStyle.Smile;
                    }
                    break;

                case 2: // 초음속 펀치
                    if (t < 1.5)
                    {
                        P.Reach = Reach.Hips; P.BodyY = 15;
                    }
                    else if (t < 3.8)
                    {
                        P.BodyY = -80; P.SonicGlow = 1.0f;
                        P.Reach = Reach.Overhead;
                        P.CapeWind = 30f;
                    }
                    else if (t < 5.5)
                    {
                        P.BodyY = 20; P.Reach = Reach.Flex;
                        P.Mouth = MouthStyle.Open;
                    }
                    else
                    {
                        P.Reach = Reach.Hips; P.Mouth = MouthStyle.Smile;
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
                    if (t < 1.4)
                    {
                        P.Reach = Reach.Hips; P.Brow = 0.5f;
                    }
                    else if (t < 4.2)
                    {
                        P.Reach = Reach.Flex;
                        P.Batarang = (float)((t - 1.4) / 2.8);
                    }
                    else
                    {
                        P.Reach = Reach.Cross; P.Mouth = MouthStyle.Neutral;
                    }
                    break;

                case 1: // 그래플건
                    if (t < 1.5)
                    {
                        P.Reach = Reach.Front; P.HeadTilt = -15f;
                    }
                    else if (t < 4.5)
                    {
                        P.Grapnel = 1.0f; P.BodyY = -50;
                        P.CapeWind = 25f;
                    }
                    else
                    {
                        P.BodyY = 0; P.Reach = Reach.Hips;
                    }
                    break;

                case 2: // 연막탄
                    if (t < 1.2)
                    {
                        P.Reach = Reach.Lap;
                    }
                    else if (t < 4.8)
                    {
                        P.SmokeBomb = (float)((t - 1.2) / 3.6);
                        P.Reach = Reach.Cross;
                    }
                    else
                    {
                        P.SmokeBomb = (float)Math.Max(0, 1 - (t - 4.8) / 2.0);
                        P.Reach = Reach.Cross;
                    }
                    break;

                case 3: // 가고일 응시
                    if (t < 1.8)
                    {
                        P.BodyY = 30; P.Knee = 1.2f;
                    }
                    else if (t < 5.5)
                    {
                        P.BodyY = 35; P.CapeWing = 1.0f;
                        P.Brow = 0.7f;
                    }
                    else
                    {
                        P.CapeWing = (float)Math.Max(0, 1 - (t - 5.5) / 1.8);
                        P.BodyY = 0;
                    }
                    break;
            }
        }

        private void ApplySpidermanSig(Pose P, ref Look L, double t)
        {
            switch (sigIndex)
            {
                case 0: // 웹 스윙
                    if (t < 1.2)
                    {
                        P.Reach = Reach.WebThrow; P.HandR = HandStyle.Web;
                    }
                    else if (t < 4.5)
                    {
                        P.WebSwing = 1.0f;
                        float sw = (float)Math.Sin((t - 1.2) * 2.5);
                        P.BodyY = -35 + sw * 25;
                        P.Lean = sw * 20;
                    }
                    else
                    {
                        P.BodyY = 15; P.Knee = 1.4f;
                    }
                    break;

                case 1: // 스파이더 센스
                    if (t < 1.8)
                    {
                        P.SpiderSense = 1.0f; P.Eyes = EyeStyle.Shock;
                        P.HeadTilt = (float)Math.Sin(t * 20) * 8f;
                    }
                    else if (t < 4.2)
                    {
                        P.BodyY = -40; P.Lean = (float)((t - 1.8) * 180);
                    }
                    else
                    {
                        P.Eyes = EyeStyle.Happy; P.Reach = Reach.Chin;
                    }
                    break;

                case 2: // 거꾸로 매달리기
                    if (t < 1.4)
                    {
                        P.Reach = Reach.Overhead;
                    }
                    else if (t < 5.0)
                    {
                        P.Lie = 1.0f; P.BodyY = -80;
                        P.HeadTilt = (float)Math.Sin(t * 3) * 12f;
                        P.Eyes = EyeStyle.Happy;
                    }
                    else
                    {
                        P.Lie = 0; P.BodyY = 0;
                    }
                    break;

                case 3: // 거미줄 네트
                    if (t < 1.2)
                    {
                        P.Reach = Reach.Cross;
                    }
                    else if (t < 4.5)
                    {
                        P.Reach = Reach.Front; P.WebNet = (float)Math.Min(1, (t - 1.2) / 1.5);
                    }
                    else
                    {
                        P.WebNet = (float)Math.Max(0, 1 - (t - 4.5) / 1.8);
                        P.Reach = Reach.Hips;
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
                        P.Reach = Reach.Hammer;
                    }
                    else if (t < 4.8)
                    {
                        P.MjolnirSpin = 1.0f;
                        P.CapeWind = 35f;
                    }
                    else
                    {
                        P.MjolnirSpin = 0; P.Reach = Reach.Hips;
                        P.Mouth = MouthStyle.Smile;
                    }
                    break;

                case 1: // 천둥 벼락 소환
                    if (t < 1.8)
                    {
                        P.Reach = Reach.Overhead; P.HeadTilt = -12f;
                    }
                    else if (t < 6.9)
                    {
                        // 먹구름이 모이고(충전) → 세 번 내리꽂히는 벼락(점점 강해짐) → 감전 잔광
                        float lt = (float)(t - 1.8), strike = ThorLightning.Strike(lt);
                        P.Reach = Reach.Overhead;
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
                        P.Lightning = 0; P.Reach = Reach.Hammer;
                    }
                    break;

                case 2: // 지면 강타 충격파
                    if (t < 1.6)
                    {
                        P.BodyY = -60; P.Reach = Reach.Overhead;
                    }
                    else if (t < 4.6)
                    {
                        P.BodyY = 25; P.GroundCrush = 1.0f;
                        P.Reach = Reach.Lap;
                    }
                    else
                    {
                        P.GroundCrush = 0; P.BodyY = 0; P.Reach = Reach.Hips;
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
    }
}
