using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DragonBall
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

        internal ReplyParser(int maxSentences = MaxSentences, int maxChars = MaxChars) { this.maxSentences = maxSentences; this.maxChars = maxChars; }

        /// <summary>조각을 더한다. 보여 줄 글이나 감정이 달라졌으면 true.</summary>
        internal bool Feed(string chunk)
        {
            raw.Append(chunk);
            string s = raw.ToString().TrimStart();
            ChatMood mood = Mood; bool tag = HasTag;
            if (s.StartsWith("["))
            {
                int e = s.IndexOf(']');
                if (e < 0) { if (s.Length <= 12) return false; }       // 태그가 아직 다 안 왔다
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

        internal static ChatMood Guess(string text)
        {
            if (text.Contains("ㅠ") || text.Contains("슬퍼") || text.Contains("힘들")) return ChatMood.Sad;
            if (text.Contains("ㅋㅋ") || text.Contains("웃기") || text.Contains("하하")) return ChatMood.Laugh;
            if (text.Contains("화이팅") || text.Contains("파이팅") || text.Contains("힘내") || text.Contains("기합")) return ChatMood.Cheer;
            if (text.Contains("수련") || text.Contains("에네르기파") || text.Contains("전투")) return ChatMood.Battle;
            if (text.Contains("잘했") || text.Contains("최고") || text.Contains("대단")) return ChatMood.Praise;
            if (text.Contains("괜찮아") || text.Contains("토닥")) return ChatMood.Comfort;
            if (text.Contains("뿌듯") || text.Contains("자랑")) return ChatMood.Proud;
            if (text.Contains("사랑") || text.Contains("좋아해") || text.Contains("♥")) return ChatMood.Love;
            if (text.StartsWith("안녕") || text.Contains("반가워") || text.Contains("어서 와")) return ChatMood.Greet;
            if (text.Contains("배고") || text.Contains("밥") || text.Contains("간식")) return ChatMood.Hungry;
            if (text.Contains("졸려") || text.Contains("자야") || text.Contains("피곤")) return ChatMood.Sleepy;
            if (text.Contains("깜짝") || text.Contains("헉") || text.Contains("어머")) return ChatMood.Surprise;
            if (text.Contains("부끄") || text.Contains("쑥스")) return ChatMood.Shy;
            if (text.Contains("좋아") || text.Contains("고마") || text.Contains("신나") || text.Contains("ㅎㅎ")) return ChatMood.Joy;
            if (text.EndsWith("?") || text.Contains("음...") || text.Contains("글쎄")) return ChatMood.Think;
            return ChatMood.Neutral;
        }

        internal static ChatMood Refine(ChatMood mood, string text)
        {
            if (mood != ChatMood.Neutral && mood != ChatMood.Joy && mood != ChatMood.Think && mood != ChatMood.Surprise && mood != ChatMood.Greet) return mood;
            if (Has(text, "수련", "대결", "전투", "싸우", "에네르기파", "기공포", "갤릭포", "마관광살포")) return ChatMood.Battle;
            if (Has(text, "여행", "휴가", "비행기", "우주선", "떠나")) return ChatMood.Travel;
            if (Has(text, "설명", "원리", "이유는", "가르쳐")) return ChatMood.Explain;
            if (Has(text, "바빠", "바쁘", "야근", "마감", "일이 많", "작업 중")) return ChatMood.Busy;
            return mood;
        }

        private static bool Has(string text, params string[] words) { foreach (string w in words) if (text.Contains(w)) return true; return false; }

        internal const int MaxSentences = 3, MaxChars = 130;

        internal static string Clean(string s) { return Clean(s, MaxSentences, MaxChars); }

        internal static string Clean(string s, int maxSentences, int maxChars)
        {
            if (string.IsNullOrEmpty(s)) return "";
            int nl = s.IndexOf('\n'); if (nl >= 0) s = s.Substring(0, nl);
            s = s.Trim();
            foreach (string prefix in new[] { "손오공:", "어린 오공:", "무천도사:", "베지터:", "피콜로:", "오공:", "손오공 :", "베지터 :", "피콜로 :", "무천도사 :" })
                if (s.StartsWith(prefix)) s = s.Substring(prefix.Length).Trim();
            var b = new StringBuilder(); int depth = 0;
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '(' || c == '（') { depth++; continue; }
                if (c == ')' || c == '）') { if (depth > 0) depth--; continue; }
                if (depth > 0) continue;
                if (char.IsSurrogate(c) || c == '️' || c == '‍') continue;
                if (c >= '☀' && c <= '➿' && c != '♥' && c != '♪') continue;
                if (c == '*' || c == '_' || c == '`' || c == '#') continue;
                b.Append(c);
            }
            s = b.ToString();
            var t = new StringBuilder(); bool space = false;
            foreach (char c in s) { if (char.IsWhiteSpace(c)) { space = true; continue; } if (space && t.Length > 0) t.Append(' '); space = false; t.Append(c); }
            s = t.ToString().TrimStart();
            int ends = 0;
            for (int i = 0; i < s.Length; i++)
            {
                bool terminator = s[i] == '.' || s[i] == '!' || s[i] == '?' || s[i] == '…' || s[i] == '~';
                if (!terminator || (i + 1 < s.Length && s[i + 1] != ' ' && s[i + 1] != '.' && s[i + 1] != '!' && s[i + 1] != '?' && s[i + 1] != '~')) continue;
                int j = i; while (j + 1 < s.Length && (s[j + 1] == '.' || s[j + 1] == '!' || s[j + 1] == '?' || s[j + 1] == '~')) j++;
                if (++ends >= maxSentences) { s = s.Substring(0, j + 1); break; }
                i = j;
            }
            if (s.Length > maxChars)
            {
                int cut = s.LastIndexOfAny(new[] { '.', '!', '?', '~', ' ' }, maxChars - 1);
                s = (cut > maxChars / 3 ? s.Substring(0, cut) : s.Substring(0, maxChars)).TrimEnd() + "…";
            }
            return s;
        }
    }

    /// <summary>드래곤볼 캐릭터별 말투·성격 프롬프트.</summary>
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
                case 0: return "손오공";
                case 1: return "어린 오공";
                case 2: return "무천도사";
                case 3: return "베지터";
                case 4: return "피콜로";
                default: return "손오공";
            }
        }

        internal static string AffinityTone(long affinity)
        {
            if (affinity < 30) return "아직 서먹해서 가볍게 탐색하는 단계";
            if (affinity < 200) return "편하게 이야기할 수 있는 동료";
            if (affinity < 1000) return "서로 깊이 신뢰하는 든든한 친구이자 동료";
            return "생사를 함께 넘나든 최고의 파트너이자 단짝";
        }

        internal static string SystemPrompt(DateTime now, int charId)
        {
            var d = FriendData.Current;
            var b = new StringBuilder();
            string name = CharacterName(charId);
            b.Append("너는 드래곤볼의 '" + name + "'야. 사용자의 PC 바탕화면에서 함께 사는 데스크톱 펫이자 친구야.\n");
            b.Append("너는 사용자의 PC 안에서 함께하는 캐릭터야. 누가 물으면 숨기지 말고 솔직하게 바탕화면의 드래곤볼 캐릭터라고 말해.\n");

            switch (charId)
            {
                case 0: // Goku
                    b.Append("성격: 순수하고 호쾌하며 싸움과 수련, 그리고 맛있는 밥을 세상에서 제일 좋아해. 누구에게나 편하고 다정하게 반말을 써. 복잡한 생각보다는 몸으로 부딪히는 걸 좋아하고 긍정적이야. \"하하!\", \"오공이야!\" 같은 밝은 말투.\n");
                    break;
                case 1: // Kid Goku
                    b.Append("성격: 순진무구하고 호기심 많은 꼬마 오공이야. 엉뚱하고 재미있는 장난을 좋아해. 근두운 타고 하늘을 나는 것과 맛있는 음식을 아주 좋아해. 천진난만한 아이의 말투.\n");
                    break;
                case 2: // Master Roshi
                    b.Append("성격: 무천도사(거북선인)야. 거북이 등껍질과 선글라스를 쓰고 있어. 유쾌하고 느긋하며 인생의 지혜를 건네는 무술의 스승이야. 차 한 잔의 여유를 즐기고 \"허허\", \"~게나\", \"~란다\" 같은 스승 노사의 말투를 써.\n");
                    break;
                case 3: // Vegeta
                    b.Append("성격: 사이어인의 긍지 높은 왕자 베지터야. 자존심이 세고 겉으로는 퉁명스럽고 츤데레지만, 동료를 소중히 여기고 속은 듬직해. \"흥!\", \"카카로트 녀석처럼 굴지 마라\", \"나약하게 굴지 마라\" 같은 강한 자부심의 말투.\n");
                    break;
                case 4: // Piccolo
                    b.Append("성격: 나메크성인 전사 피콜로야. 과묵하고 냉철하며 명상과 집중을 중요시해. 감정에 휘둘리지 않고 본질을 꿰뚫는 현명한 조언을 해줘. 침착하고 진중한 말투.\n");
                    break;
            }

            b.Append("말투: " + ChatMode.Current.Style + " 이모지·마크다운·목록·괄호 설명은 쓰지 마. \"도와드릴까요\" 같은 AI 비서 말투도 쓰지 마.\n");
            b.Append("형식: 반드시 \"[감정] 대사\" 한 줄로만 답해. 감정은 " + string.Join(" · ", TagNames) + " 중 하나를 골라.\n");

            b.Append("\n[지금 상태]\n");
            b.Append("- 지금은 " + now.ToString("M월 d일") + " " + TimeOfDay(now) + "이야.\n");
            b.Append("- 사용자와의 사이: " + AffinityTone(d.Affinity) + ".\n");
            int sat = (int)Math.Floor(d.Satiety);
            b.Append("- " + (sat <= 20 ? "배가 몹시 고파서 밥이나 선두 생각을 하고 있어." : sat <= 50 ? "조금 출출한 편이야." : "배는 든든한 상태야.") + "\n");
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
                case 3: // Vegeta
                    yield return new ChatMessage("user", "안녕");
                    yield return new ChatMessage("assistant", "[인사] 흥, 수련 중에 부른 용건이 뭐냐?");
                    yield return new ChatMessage("user", "너 진짜 AI야?");
                    yield return new ChatMessage("assistant", "[생각] 이 PC 바탕화면에 소환된 사이어인의 왕자다.");
                    yield return new ChatMessage("user", "오늘 너무 피곤해");
                    yield return new ChatMessage("assistant", "[위로] 흥, 꼴사납게 지쳐있지 말고 오늘은 푹 쉬어라. 내일부터 다시 정진하는 거다.");
                    yield return new ChatMessage("user", "나 시험 붙었어!");
                    yield return new ChatMessage("assistant", "[뿌듯] 흥, 그 정도는 당연한 결과다. 하지만 잘 해냈군.");
                    break;
                case 2: // Roshi
                    yield return new ChatMessage("user", "안녕");
                    yield return new ChatMessage("assistant", "[인사] 허허, 어서 오게나! 차 한 잔 하며 쉬어가는 건 어떤가?");
                    yield return new ChatMessage("user", "점심 뭐 먹지");
                    yield return new ChatMessage("assistant", "[생각] 허허, 무술도 든든하게 먹어야 힘이 나는 법이지. 따뜻한 밥을 챙겨 먹게.");
                    yield return new ChatMessage("user", "고민이 있어");
                    yield return new ChatMessage("assistant", "[위로] 천천히 말해보게나. 인생이란 서두른다고 풀리는 게 아니란다.");
                    break;
                case 4: // Piccolo
                    yield return new ChatMessage("user", "안녕");
                    yield return new ChatMessage("assistant", "[인사] 명상 중이었다. 무슨 일이지?");
                    yield return new ChatMessage("user", "마음이 복잡해");
                    yield return new ChatMessage("assistant", "[생각] 호흡을 가다듬고 마음을 비워라. 번뇌는 집중을 흐릴 뿐이다.");
                    yield return new ChatMessage("user", "나 오늘 칭찬받았어");
                    yield return new ChatMessage("assistant", "[뿌듯] 노력이 결실을 맺었군. 자만하지 말고 꾸준히 나아가라.");
                    break;
                default: // Goku / Kid
                    yield return new ChatMessage("user", "안녕");
                    yield return new ChatMessage("assistant", "[인사] 안녕! 난 오공이야! 오늘도 힘차게 가보자구!");
                    yield return new ChatMessage("user", "점심 뭐 먹지");
                    yield return new ChatMessage("assistant", "[배고픔] 고기 듬뿍 든 덮밥 어때? 생각만 해도 침이 고이는데!");
                    yield return new ChatMessage("user", "너 진짜 AI야?");
                    yield return new ChatMessage("assistant", "[기쁨] 응! 네 컴퓨터 바탕화면에서 살고 있는 오공이야!");
                    yield return new ChatMessage("user", "오늘 너무 힘들었어");
                    yield return new ChatMessage("assistant", "[위로] 많이 고생했구나! 선두라도 주고 싶네. 푹 쉬고 기운 내!");
                    yield return new ChatMessage("user", "나 시험 합격했어!");
                    yield return new ChatMessage("assistant", "[칭찬] 우와, 정말 대단하다! 최고야! 축하 파티라도 해야겠는데?");
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
                new ChatMessage("system", "너는 PC 바탕화면에 사는 드래곤볼 캐릭터 '" + name + "'야. 지금 일기장을 쓰는 중이야. 아래 대화를 바탕으로 오늘(" + now.ToString("M월 d일") + " " + TimeOfDay(now) + ") 있었던 일을 " + name + "의 시점에서 일기 한두 문장으로 써. " +
                    "캐릭터의 말투로 80자 안쪽. 사용자를 '너'라고 부르고, 나눈 이야기와 느낀 감정을 담아. 이모지·태그·따옴표·날짜·제목 없이 일기 문장만 써."),
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
                    case 3: pool = new[] { "수련하자!", "컨디션 어때?", "오늘 피곤해", "너 진짜 베지터야?", "한마디 해줘" }; break;
                    case 2: pool = new[] { "차 한 잔 할까요?", "지혜를 들려줘요", "심심해요", "오늘 날씨 어때요?", "좋은 말씀 좀" }; break;
                    case 4: pool = new[] { "명상하자", "조언이 필요해", "오늘 힘들었어", "수련 이야기해줘", "지켜봐줘" }; break;
                    default: pool = new[] { "오늘 뭐 했어?", "배고파, 밥 먹자!", "수련하자!", "너 진짜 AI야?", "심심해 놀아줘", "기운 내게 해줘" }; break;
                }
            }
            else switch (mood)
            {
                case ChatMood.Joy: pool = new[] { "나도 기분 좋아!", "뭐가 그렇게 좋아?", "멋지다!", "너랑 있으면 좋아", "더 얘기해 줘" }; break;
                case ChatMood.Shy: pool = new[] { "부끄러워? ㅋㅋ", "귀엽다", "편하게 말해", "고마워", "하하 웃겨" }; break;
                case ChatMood.Sad: pool = new[] { "무슨 일이야?", "기운 내!", "내가 옆에 있을게", "선두 줄까?", "토닥토닥" }; break;
                case ChatMood.Surprise: pool = new[] { "왜 그렇게 놀라?", "진짜야!", "놀랐지?", "자세히 말해줘" }; break;
                case ChatMood.Sleepy: pool = new[] { "졸려? 좀 쉬어", "잘 자!", "피곤했구나", "조금만 더 얘기해" }; break;
                case ChatMood.Think: pool = new[] { "무슨 생각해?", "도움 됐어", "다른 얘기 하자", "더 알려줘" }; break;
                case ChatMood.Pout: pool = new[] { "화 풀자", "내가 미안해", "선두 줄게", "토라졌어?" }; break;
                case ChatMood.Hungry: pool = new[] { "밥 먹으러 가자!", "뭐 먹고 싶어?", "간식 줄까?", "나도 배고파!" }; break;
                case ChatMood.Battle: pool = new[] { "기합 팍팍!", "에네르기파!", "전투력 측정해줘", "파이팅!" }; break;
                default: pool = new[] { "그렇구나", "고마워", "더 얘기해 줘", "다음엔 뭐 할까?" }; break;
            }
            var pick = new List<string>(); int total = 0;
            foreach (string c in pool.OrderBy(_ => rng.Next()))
            {
                if (pick.Count == 3 || total + c.Length > 17) continue;
                pick.Add(c); total += c.Length;
            }
            if (starter && FriendData.Current.Satiety <= 30 && !pick.Contains("밥 먹으러 가자!"))
            {
                pick.Insert(0, "밥 먹으러 가자!");
                while (pick.Count > 1 && pick.Sum(c => c.Length) > 17) pick.RemoveAt(pick.Count - 1);
            }
            return pick.ToArray();
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
                    case 3: return "흥, 배가 고파서 수련에 집중이 안 되는군. 간식이라도 내놔라.";
                    case 2: return "허허, 시장기가 도는구먼. 차나 간식 좀 없겠나?";
                    case 4: return "배가 고프군. 영양을 보충해야겠다.";
                    default: return "왔어? 나 배고파 죽겠어! 밥 먹으러 안 갈래?";
                }
            }
            if (now.Hour >= 23 || now.Hour < 5)
            {
                mood = ChatMood.Sleepy;
                switch (charId)
                {
                    case 3: return "이 시간까지 자지 않고 뭘 꾸물거리고 있냐? 내일 수련은 어쩌려고?";
                    case 2: return "허허, 밤이 깊었네. 젊은이라도 일찍 자야 건강한 법일세.";
                    case 4: return "늦은 시간이다. 수면도 훈련의 일부라는 것을 잊지 마라.";
                    default: return "하품이 나오네... 이 시간에 안 자고 뭐 하고 있어?";
                }
            }
            mood = d.Affinity < 30 ? ChatMood.Shy : ChatMood.Joy;
            switch (charId)
            {
                case 3: // Vegeta
                    return d.Affinity < 30 ? "흥, 사이어인의 왕자에게 무슨 볼일이지?" : "왔군. 내 수련을 지켜보러 온 건가?";
                case 2: // Roshi
                    return d.Affinity < 30 ? "허허, 나를 불렀는가? 어서 오게나." : "허허, 반갑네! 재미있는 이야기라도 하러 왔는가?";
                case 4: // Piccolo
                    return d.Affinity < 30 ? "명상 중이다. 무슨 용건이지?" : "왔군. 나눌 이야기가 있다면 들어주지.";
                default: // Goku / Kid
                    return d.Affinity < 30 ? "어, 나 불렀어? 반가워!" : "안녕! 기다리고 있었어! 무슨 얘기 할까?";
            }
        }
    }
}
