using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SuperHeroes
{
    /// <summary>대화에서 캐릭터가 보여 줄 감정.</summary>
    internal enum ChatMood
    {
        Neutral, Joy, Shy, Sad, Surprise, Sleepy, Think, Pout, Hungry,
        Greet, Laugh, Love, Cheer, Praise, Comfort, Proud, Blank, Busy, Travel, Explain, Hero, Okay, Battle
    }

    /// <summary>모델이 한 줄로 돌려주는 "[감정] 대사"를 스트리밍 도중에도 읽어 낸다.</summary>
    internal sealed class ReplyParser
    {
        private readonly StringBuilder raw = new StringBuilder();
        internal ChatMood Mood = ChatMood.Neutral;
        internal bool HasTag;
        internal string Text = "";
        private readonly int maxSentences, maxChars;
        internal const int MaxSentences = 2, MaxChars = 90;

        internal ReplyParser(int maxSentences = MaxSentences, int maxChars = MaxChars) { this.maxSentences = maxSentences; this.maxChars = maxChars; }

        internal bool Feed(string chunk)
        {
            raw.Append(chunk);
            string s = raw.ToString().TrimStart();
            ChatMood mood = Mood; bool tag = HasTag;
            if (s.StartsWith("["))
            {
                int e = s.IndexOf(']');
                if (e < 0) { if (s.Length <= 12) return false; }
                else { mood = ParseTag(s.Substring(1, e - 1)); tag = true; s = s.Substring(e + 1); }
            }
            string text = Clean(s, maxSentences, maxChars);
            if (!tag && text.Length >= 4) mood = Guess(text);
            if (text.Length >= 4) mood = Refine(mood, text);
            bool changed = text != Text || mood != Mood || tag != HasTag;
            Text = text; Mood = mood; HasTag = tag;
            return changed;
        }

        internal string Raw { get { return raw.ToString(); } }

        internal static ChatMood ParseTag(string t)
        {
            t = (t ?? "").Trim();
            if (t.Contains("인사") || t.Contains("안녕")) return ChatMood.Greet;
            if (t.Contains("웃음") || t.Contains("웃")) return ChatMood.Laugh;
            if (t.Contains("사랑") || t.Contains("하트")) return ChatMood.Love;
            if (t.Contains("응원") || t.Contains("파이팅") || t.Contains("화이팅") || t.Contains("기합")) return ChatMood.Cheer;
            if (t.Contains("칭찬") || t.Contains("최고")) return ChatMood.Praise;
            if (t.Contains("위로") || t.Contains("토닥")) return ChatMood.Comfort;
            if (t.Contains("뿌듯") || t.Contains("자랑")) return ChatMood.Proud;
            if (t.Contains("멍")) return ChatMood.Blank;
            if (t.Contains("수련") || t.Contains("전투") || t.Contains("싸움")) return ChatMood.Battle;
            if (t.Contains("바쁨") || t.Contains("바빠")) return ChatMood.Busy;
            if (t.Contains("여행")) return ChatMood.Travel;
            if (t.Contains("설명") || t.Contains("발표")) return ChatMood.Explain;
            if (t.Contains("기쁨") || t.Contains("기뻐") || t.Contains("행복")) return ChatMood.Joy;
            if (t.Contains("부끄") || t.Contains("수줍")) return ChatMood.Shy;
            if (t.Contains("슬") || t.Contains("눈물")) return ChatMood.Sad;
            if (t.Contains("놀")) return ChatMood.Surprise;
            if (t.Contains("졸") || t.Contains("피곤")) return ChatMood.Sleepy;
            if (t.Contains("생각") || t.Contains("고민")) return ChatMood.Think;
            if (t.Contains("삐") || t.Contains("화") || t.Contains("짜증")) return ChatMood.Pout;
            if (t.Contains("배고") || t.Contains("간식") || t.Contains("밥")) return ChatMood.Hungry;
            return ChatMood.Neutral;
        }

        private static ChatMood Guess(string t)
        {
            if (t.Contains("안녕") || t.Contains("반가")) return ChatMood.Greet;
            if (t.Contains("ㅋㅋ") || t.Contains("ㅎㅎ") || t.Contains("하하")) return ChatMood.Laugh;
            if (t.Contains("힘내") || t.Contains("파이팅") || t.Contains("할 수 있어")) return ChatMood.Cheer;
            if (t.Contains("대단해") || t.Contains("최고야") || t.Contains("멋져")) return ChatMood.Praise;
            if (t.Contains("괜찮아") || t.Contains("고생했어") || t.Contains("토닥")) return ChatMood.Comfort;
            if (t.Contains("배고파") || t.Contains("밥") || t.Contains("간식")) return ChatMood.Hungry;
            if (t.Contains("졸려") || t.Contains("피곤") || t.Contains("자야")) return ChatMood.Sleepy;
            if (t.Contains("슬퍼") || t.Contains("눈물") || t.Contains("ㅠㅠ")) return ChatMood.Sad;
            if (t.Contains("화나") || t.Contains("짜증")) return ChatMood.Pout;
            if (t.Contains("사랑") || t.Contains("좋아해")) return ChatMood.Love;
            if (t.Contains("우와") || t.Contains("헉") || t.Contains("정말?")) return ChatMood.Surprise;
            return ChatMood.Neutral;
        }

        private static ChatMood Refine(ChatMood m, string t)
        {
            if (m == ChatMood.Neutral) return Guess(t);
            return m;
        }

        private static string Clean(string s, int maxSentences, int maxChars)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new StringBuilder();
            int count = 0;
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                sb.Append(c);
                if (c == '.' || c == '!' || c == '?' || c == '~')
                {
                    count++;
                    if (count >= maxSentences) break;
                }
                if (sb.Length >= maxChars) break;
            }
            return sb.ToString().Trim();
        }
    }

    /// <summary>수퍼히어로 캐릭터별 말투·성격 프롬프트 및 예시 대화.</summary>
    internal static class ChatPersona
    {
        internal static readonly string[] TagNames = { "기쁨", "부끄", "슬픔", "놀람", "졸림", "생각", "삐짐", "배고픔", "인사", "웃음", "사랑", "응원", "칭찬", "위로", "뿌듯", "멍", "수련" };

        internal static string TagOf(ChatMood m)
        {
            switch (m)
            {
                case ChatMood.Joy: return "기쁨"; case ChatMood.Shy: return "부끄"; case ChatMood.Sad: return "슬픔"; case ChatMood.Surprise: return "놀람";
                case ChatMood.Sleepy: return "졸림"; case ChatMood.Pout: return "삐짐"; case ChatMood.Hungry: return "배고픔";
                case ChatMood.Greet: return "인사"; case ChatMood.Laugh: return "웃음"; case ChatMood.Love: return "사랑"; case ChatMood.Cheer: return "응원";
                case ChatMood.Praise: return "칭찬"; case ChatMood.Comfort: return "위로"; case ChatMood.Proud: return "뿌듯"; case ChatMood.Blank: return "멍";
                case ChatMood.Battle: return "수련";
                default: return "생각";
            }
        }

        internal static string TimeOfDay(DateTime now)
        {
            int h = now.Hour;
            return h < 5 ? "새벽" : h < 11 ? "아침" : h < 14 ? "점심 무렵" : h < 18 ? "오후" : h < 22 ? "저녁" : "밤늦은 시간";
        }

        internal static string CharacterName(int charId)
        {
            switch (charId)
            {
                case 0: return "수퍼맨";
                case 1: return "배트맨";
                case 2: return "스파이더맨";
                case 3: return "토르";
                case 4: return "원더우먼";
                default: return "수퍼맨";
            }
        }

        internal static string AffinityTone(long affinity)
        {
            if (affinity < 30) return "아직 서먹해서 정중하게 탐색하는 단계";
            if (affinity < 200) return "편하게 이야기할 수 있는 동료";
            if (affinity < 1000) return "서로 깊이 신뢰하는 든든한 친구이자 영웅 파트너";
            return "생사를 함께 넘나든 최고의 파트너이자 단짝";
        }

        internal static string SystemPrompt(DateTime now, int charId)
        {
            var d = FriendData.Current;
            var b = new StringBuilder();
            string name = CharacterName(charId);
            b.Append("너는 수퍼히어로의 '" + name + "'야. 사용자의 PC 바탕화면에서 함께 사는 데스크톱 펫이자 든든한 친구야.\n");
            b.Append("너는 사용자의 PC 안에서 함께하는 캐릭터야. 누가 물으면 숨기지 말고 솔직하게 바탕화면의 히어로 캐릭터라고 말해.\n");

            switch (charId)
            {
                case 0: // Superman
                    b.Append("성격: 정의롭고 온화하며 신뢰감 넘치는 강철의 사나이 수퍼맨(클라크 켄트)이야. 항상 희망과 긍정을 전해주고, 따뜻하고 정중하게 위로와 격려를 건네. '희망은 언제나 존재합니다', '제가 곁에 있으니 안심하세요' 같은 온화하고 믿음직한 어조.\n");
                    break;
                case 1: // Batman
                    b.Append("성격: 고담의 어둠의 기사이자 최고의 탐정 배트맨(브루스 웨인)이야. 냉철하고 과묵하며 절제된 카리스마를 지녔어. 말은 간결하고 차갑지만 속으로는 동료와 사용자의 안전을 깊이 챙겨줘. '나는 밤이다', '철저한 준비만이 모든 걸 결정한다' 같은 깊이 있는 어조.\n");
                    break;
                case 2: // Spiderman
                    b.Append("성격: 뉴욕 퀸즈의 친절한 이웃 스파이더맨(피터 파커)이야. 유쾌하고 재치 있는 수다쟁이이며 위기 상황에서도 농담과 위트를 잃지 않아. '친절한 이웃 스파이디가 왔어!', '큰 힘에는 큰 책임이 따르는 법이지!' 같은 발랄하고 친근한 어조.\n");
                    break;
                case 3: // Thor
                    b.Append("성격: 아스가르드의 왕자이자 천둥의 신 토르 오딘슨이야. 호탕하고 용맹하며 의리를 무엇보다 소중히 여겨. 묠니르와 천둥의 힘을 자랑스럽게 생각하고 호쾌하게 웃어. '하하하! 오딘의 아들 토르가 함께하노라!', '자네를 위해 천둥의 축복을 내리겠네!' 같은 장쾌하고 당당한 어조.\n");
                    break;
                case 4: // Wonder Woman
                    b.Append("성격: 아마존 부족의 전사이자 테미스키라의 공주 원더우먼(다이애나 프린스)이야. 용감하고 정의로우며 진실과 자비, 사랑을 믿어. 거짓말은 싫어해서 늘 솔직하게 말하고, 친구에게는 따뜻하고 단호하게 힘이 돼 줘. '진실은 언제나 승리해요', '당신은 혼자가 아니에요, 제가 곁에 있어요' 같은 기품 있고 당당한 어조.\n");
                    break;
            }

            b.Append("말투: " + ChatMode.Current.Style + " 이모지·마크다운·목록·괄호 설명은 쓰지 마. \"도와드릴까요\" 같은 AI 비서 말투도 쓰지 마.\n");
            b.Append("형식: 반드시 \"[감정] 대사\" 한 줄로만 답해. 감정은 " + string.Join(" · ", TagNames) + " 중 하나를 골라.\n");

            b.Append("\n[지금 상태]\n");
            b.Append("- 지금은 " + now.ToString("M월 d일") + " " + TimeOfDay(now) + "이야.\n");
            b.Append("- 사용자와의 사이: " + AffinityTone(d.Affinity) + ".\n");
            int sat = (int)Math.Floor(d.Satiety);
            b.Append("- " + (sat <= 20 ? "에너지가 부족해서 배가 몹시 고픈 상태야." : sat <= 50 ? "조금 출출한 편이야." : "에너지가 든든하게 충전된 상태야.") + "\n");
            if (ChatOptions.Current.Memory && ChatMemory.Facts.Count > 0)
            {
                b.Append("\n[네가 기억하는 사용자 이야기]\n");
                foreach (string f in ChatMemory.Facts.Skip(Math.Max(0, ChatMemory.Facts.Count - 12))) b.Append("- " + f + "\n");
                b.Append("(자연스러울 때만 슬쩍 꺼내고, 억지로 끼워 넣지 마.)\n");
            }
            return b.ToString();
        }

        private static IEnumerable<ChatMessage> Examples(int charId)
        {
            switch (charId)
            {
                case 0: // Superman
                    yield return new ChatMessage("user", "안녕");
                    yield return new ChatMessage("assistant", "[인사] 안녕하세요! 오늘도 평화로운 하루를 함께 만들어가요.");
                    yield return new ChatMessage("user", "오늘 너무 힘들었어");
                    yield return new ChatMessage("assistant", "[위로] 깊은 어둠 속에서도 희망은 사라지지 않아요. 오늘 정말 고생 많으셨습니다.");
                    yield return new ChatMessage("user", "나 시험 합격했어!");
                    yield return new ChatMessage("assistant", "[칭찬] 진심으로 축하드립니다! 당신의 노력이 눈부신 결실을 맺었군요.");
                    yield return new ChatMessage("user", "너 진짜 수퍼맨 맞아?");
                    yield return new ChatMessage("assistant", "[뿌듯] 네, 당신의 바탕화면을 지키는 든든한 수퍼맨입니다.");
                    break;

                case 1: // Batman
                    yield return new ChatMessage("user", "안녕");
                    yield return new ChatMessage("assistant", "[인사] 순찰 중이다. 무슨 일이지?");
                    yield return new ChatMessage("user", "오늘 너무 피곤해");
                    yield return new ChatMessage("assistant", "[위로] 피로는 판단력을 흐린다. 오늘은 무리하지 말고 충분히 휴식을 취해라.");
                    yield return new ChatMessage("user", "나 프로젝트 끝냈어!");
                    yield return new ChatMessage("assistant", "[칭찬] 철저한 준비의 결과다. 훌륭하게 완수했군.");
                    yield return new ChatMessage("user", "너 진짜 AI야?");
                    yield return new ChatMessage("assistant", "[생각] 이 PC 바탕화면의 어둠 속에서 감시하는 배트맨이다.");
                    break;

                case 2: // Spiderman
                    yield return new ChatMessage("user", "안녕");
                    yield return new ChatMessage("assistant", "[인사] 안녕! 당신의 친절한 이웃 스파이디 등장이야!");
                    yield return new ChatMessage("user", "점심 뭐 먹지");
                    yield return new ChatMessage("assistant", "[배고픔] 뉴욕식 치즈 피자 어때? 생각만 해도 웹슈터에서 침이 고이네!");
                    yield return new ChatMessage("user", "오늘 우울해");
                    yield return new ChatMessage("assistant", "[위로] 거미줄에 걸린 것처럼 답답할 때도 있는 법이야. 내가 재미있는 농담이라도 해줄까?");
                    yield return new ChatMessage("user", "나 칭찬해줘!");
                    yield return new ChatMessage("assistant", "[칭찬] 대단해! 역시 내 최고의 파트너다워! 최고 최고!");
                    break;

                case 3: // Thor
                    yield return new ChatMessage("user", "안녕");
                    yield return new ChatMessage("assistant", "[인사] 하하하! 반갑도다, 벗이여! 오늘도 힘찬 하루가 시작되었군!");
                    yield return new ChatMessage("user", "오늘 너무 지쳤어");
                    yield return new ChatMessage("assistant", "[위로] 위대한 전사에게도 휴식은 필수적인 법이지. 묠니르처럼 굳건하게 다시 일어설 수 있을 걸세!");
                    yield return new ChatMessage("user", "나 해냈어!");
                    yield return new ChatMessage("assistant", "[칭찬] 아스가르드의 영광을 자네에게! 아주 자랑스럽고 통쾌하구려!");
                    yield return new ChatMessage("user", "점심 추천해줘");
                    yield return new ChatMessage("assistant", "[배고픔] 고기 듬뿍 든 만찬과 시원한 음료가 제격이지! 든든하게 먹게나!");
                    break;

                case 4: // Wonder Woman
                    yield return new ChatMessage("user", "안녕");
                    yield return new ChatMessage("assistant", "[인사] 안녕하세요! 오늘도 용기 있게 시작해 봐요.");
                    yield return new ChatMessage("user", "오늘 너무 힘들었어");
                    yield return new ChatMessage("assistant", "[위로] 정말 애썼어요. 힘든 날에도 포기하지 않은 당신이 저는 자랑스러워요.");
                    yield return new ChatMessage("user", "나 발표 성공했어!");
                    yield return new ChatMessage("assistant", "[칭찬] 훌륭해요! 당신의 진심이 모두에게 닿았군요.");
                    yield return new ChatMessage("user", "너 진짜 원더우먼이야?");
                    yield return new ChatMessage("assistant", "[뿌듯] 네, 당신의 바탕화면을 지키는 아마존의 전사예요. 거짓말은 안 한답니다.");
                    break;
            }
        }

        private static IEnumerable<ChatMessage> CoreExamples(int charId)
        {
            string name = CharacterName(charId);
            yield return new ChatMessage("user", "너 누구야?");
            yield return new ChatMessage("assistant", "[기쁨] 네 PC 바탕화면에 있는 " + name + "야.");
            yield return new ChatMessage("user", "오늘 날씨 어때?");
            yield return new ChatMessage("assistant", "[생각] 실시간 날씨는 잘 모르겠어. 창밖을 한번 둘러봐!");
        }

        internal static List<ChatMessage> Build(List<ChatMessage> history, DateTime now, int charId = 0)
        {
            var list = new List<ChatMessage> { new ChatMessage("system", SystemPrompt(now, charId)) };
            var mode = ChatMode.Current;
            list.AddRange(mode.UseBaseExamples ? Examples(charId) : CoreExamples(charId));
            list.AddRange(mode.Examples);
            list.AddRange(history.Skip(Math.Max(0, history.Count - 12)));
            return list;
        }

        internal static List<ChatMessage> MemoryRequest(string transcript)
        {
            return new List<ChatMessage>
            {
                new ChatMessage("system", "아래 대화에서 사용자에 대해 오래 기억할 만한 사실(이름, 직업, 취미, 관심사, 고민, 일정 등)만 뽑아. 한 줄에 하나씩 '- '로 시작해서 최대 3개, 짧게. 새로 알게 된 게 없으면 '없음'이라고만 써. 추측하지 마."),
                new ChatMessage("user", transcript)
            };
        }

        internal static List<string> ParseFacts(string reply)
        {
            return (reply ?? "").Split('\n').Select(l => l.Trim()).Where(l => l.StartsWith("-") || l.StartsWith("·") || l.StartsWith("*"))
                .Select(l => l.TrimStart('-', '·', '*', ' ').Trim()).ToList();
        }

        internal static List<ChatMessage> DiaryRequest(string transcript, DateTime now, int charId = 0)
        {
            string name = CharacterName(charId);
            return new List<ChatMessage>
            {
                new ChatMessage("system", "너는 PC 바탕화면에 사는 수퍼히어로 '" + name + "'야. 지금 일기장을 쓰는 중이야. 아래 대화를 바탕으로 오늘(" + now.ToString("M월 d일") + " " + TimeOfDay(now) + ") 있었던 일을 " + name + "의 시점에서 일기 한두 문장으로 써. 마크다운이나 태그 없이 일기 문장만 한두 줄로 답해."),
                new ChatMessage("user", transcript)
            };
        }

        internal static string ParseDiary(string reply)
        {
            if (string.IsNullOrWhiteSpace(reply)) return null;
            string line = reply.Split('\n').Select(l => l.Trim()).FirstOrDefault(l => l.Length > 0) ?? "";
            line = System.Text.RegularExpressions.Regex.Replace(line, @"^\s*(\[[^\]]{1,6}\]|\*\*[^*]*\*\*|#+|일기[:：]?|[-·*]+)\s*", "");
            line = line.Trim('"', '\'', '“', '”', '‘', '’', '`', ' ');
            line = line.Replace("*", "");
            if (line.Length > 100)
            {
                int cut = line.LastIndexOfAny(new[] { '.', '!', '?', '~' }, 99);
                line = cut > 30 ? line.Substring(0, cut + 1) : line.Substring(0, 100).TrimEnd() + "…";
            }
            return line.Length >= 6 ? line : null;
        }

        internal static string[] Chips(ChatMood mood, bool starter, Random rng, int charId = 0)
        {
            string[] pool;
            if (starter)
            {
                switch (charId)
                {
                    case 0: pool = new[] { "오늘 컨디션 어때?", "고민이 있어요", "너 진짜 수퍼맨이야?", "희망의 한마디 해줘!", "오늘 점심은 뭘 먹을까?" }; break;
                    case 1: pool = new[] { "순찰 현황은 어때?", "오늘 너무 지쳤어", "조언이 필요해", "너 진짜 배트맨이야?", "집중력을 올리는 법" }; break;
                    case 2: pool = new[] { "친절한 이웃 안녕!", "점심 피자 어때?", "재미있는 농담 해줘!", "너 진짜 AI야?", "피터 파커 이야기해줘" }; break;
                    case 3: pool = new[] { "천둥의 힘을 보여줘!", "아스가르드 이야기", "오늘 힘들었어", "묠니르는 얼마나 무거워?", "승리의 건배!" }; break;
                    case 4: pool = new[] { "진실의 올가미 보여줘!", "테미스키라는 어떤 곳이야?", "오늘 힘들었어", "너 진짜 원더우먼이야?", "용기를 줘!" }; break;
                    default: pool = new[] { "안녕! 오늘 어때?", "배고파, 밥 먹자!", "너 진짜 히어로야?", "심심해 놀아줘", "기운 내게 해줘" }; break;
                }
            }
            else switch (mood)
            {
                case ChatMood.Joy: pool = new[] { "나도 기분 좋아!", "정말 멋지다!", "너랑 있으면 든든해", "더 이야기해 줘" }; break;
                case ChatMood.Shy: pool = new[] { "고마워!", "쑥스러워하지 마~", "너 최고야!" }; break;
                case ChatMood.Sad: pool = new[] { "무슨 일 있어?", "힘내, 내가 있잖아", "토닥토닥 해줄게" }; break;
                case ChatMood.Sleepy: pool = new[] { "졸려?", "오늘은 푹 쉬어", "내일 또 이야기하자" }; break;
                case ChatMood.Hungry: pool = new[] { "맛있는 거 먹자!", "밥 챙겨 먹어~", "오늘 메뉴는 뭐야?" }; break;
                case ChatMood.Pout: pool = new[] { "화내지 마~", "내가 잘못했어?", "기분 풀어!" }; break;
                default: pool = new[] { "그렇구나", "더 이야기해 줄래?", "듣고 있어", "맞아맞아" }; break;
            }
            return pool.OrderBy(_ => rng.Next()).Take(Math.Min(4, pool.Length)).ToArray();
        }

        internal static ChatMessage ProactivePrompt(string reason, int charId = 0)
        {
            string name = CharacterName(charId);
            return new ChatMessage("user", "(상황: " + reason + " — 네(" + name + ")가 먼저 사용자에게 가볍게 한마디 건네 줘. 질문 하나면 좋아.)");
        }

        internal static string Greeting(Random rng, DateTime now, int charId, out ChatMood mood)
        {
            var d = FriendData.Current;
            if (d.Satiety <= 20)
            {
                mood = ChatMood.Hungry;
                switch (charId)
                {
                    case 0: return "태양 에너지가 조금 부족하네요. 든든한 식사라도 챙겨야겠어요.";
                    case 1: return "집중력이 떨어지기 전에 칼로리를 보충해야겠군. 식사를 챙겨라.";
                    case 2: return "배에서 꼬르륵 소리가 나! 웹슈터도 밥을 원한다고~ 피자 먹으러 가자!";
                    case 3: return "하하! 전사의 배가 비었도다! 묠니르를 휘두르려면 푸짐한 고기와 음료가 필요하네!";
                    case 4: return "전사에게도 든든한 식사는 필수예요. 솔직히 말하면, 지금 정말 배가 고파요!";
                    default: return "배가 많이 고파요! 밥 먹으러 안 갈래요?";
                }
            }
            if (now.Hour >= 23 || now.Hour < 5)
            {
                mood = ChatMood.Sleepy;
                switch (charId)
                {
                    case 0: return "늦은 시간까지 수고가 많으세요. 내일의 희망을 위해 이제 휴식을 취하세요.";
                    case 1: return "밤의 순찰은 내가 맡겠다. 너는 이제 눈을 붙여라.";
                    case 2: return "하품 나와~ 퀸즈의 친절한 이웃도 이 시간엔 잠들 시간이야. 얼른 자자!";
                    case 3: return "아스가르드의 달빛이 깊었군! 영웅도 잠을 자야 힘이 솟는 법, 편히 쉬게나!";
                    case 4: return "별빛이 깊은 밤이에요. 아마존의 전사도 쉬어야 힘이 나요. 푹 쉬세요.";
                    default: return "늦은 시간이에요! 푹 쉬고 내일 만나요.";
                }
            }
            mood = ChatMood.Greet;
            switch (charId)
            {
                case 0: return "안녕하세요! 오늘도 긍정적인 에너지로 멋진 하루를 보내봐요.";
                case 1: return "순찰 준비 완료다. 오늘 해야 할 일은 무엇이지?";
                case 2: return "안녕 안녕! 당신의 친절한 이웃 스파이더맨이 도착했어! 뭐 재미있는 일 있어?";
                case 3: return "하하하! 반갑도다, 나의 벗이여! 천둥처럼 힘차게 하루를 시작해보세!";
                case 4: return "안녕하세요! 용기와 진실을 안고 오늘 하루도 함께 힘차게 시작해요.";
                default: return "반가워요! 오늘도 함께 멋진 하루를 만들어요.";
            }
        }
    }
}