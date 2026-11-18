using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace SuperHeroes
{
    /// <summary>
    /// 대화 상대를 고른다: 로컬 Ollama 또는 LG AI연구원 EXAONE API. 대화 흐름(ChatController)은 이 클래스만 부른다.
    /// 둘 다 호출한 스레드에서 막혀 기다리므로 일꾼 스레드에서 부른다.
    /// </summary>
    internal static class LlmClient
    {
        internal static ProbeResult Probe(ChatOptions o) { return o.IsExaone ? ExaoneClient.Probe(o) : OllamaClient.Probe(o); }
        internal static void Warmup(ChatOptions o) { if (!o.IsExaone) OllamaClient.Warmup(o); }   // 클라우드는 미리 올릴 게 없다
        internal static string Stream(ChatOptions o, List<ChatMessage> msgs, Action<string> onToken, ChatRequest handle, int numPredict = 170, double temperature = .85)
        { return o.IsExaone ? ExaoneClient.Stream(o, msgs, onToken, handle, numPredict, temperature) : OllamaClient.Stream(o, msgs, onToken, handle, numPredict, temperature); }
        internal static string Complete(ChatOptions o, List<ChatMessage> msgs, int numPredict, double temperature)
        { return o.IsExaone ? ExaoneClient.Complete(o, msgs, numPredict, temperature) : OllamaClient.Complete(o, msgs, numPredict, temperature); }
    }

    /// <summary>
    /// EXAONE API Platform (https://docs.exaone.ai) — OpenAI 호환 POST {Endpoint}/v1/chat/completions, "Authorization: Bearer 키".
    /// 모델 k-exaone_v2.5 는 추론(reasoning)이 늘 켜져 있고 max_tokens 가 추론+답 전체라서, 답 길이에 추론 몫을 더해 보낸다.
    /// stop 매개변수가 없어서 "한 줄로 말하기"는 받은 글에서 첫 줄바꿈을 만나면 끊는 식으로 맞춘다.
    /// 추론 글(reasoning_content)은 버리고 답(content)만 넘긴다.
    /// </summary>
    internal static class ExaoneClient
    {
        internal const string DefaultEndpoint = "https://api.exaone.ai";
        internal const string DefaultModel = "k-exaone_v2.5";
        internal static readonly string[] Efforts = { "low", "high", "max" };

        /// <summary>추론에 줄 토큰 몫(추론 강도별). 모자라면 답이 나오기 전에 끊긴다.</summary>
        private static int ReasoningBudget(string effort) { return effort == "max" ? 8192 : effort == "high" ? 4096 : 1536; }

        private static HttpWebRequest Make(ChatOptions o, int timeoutMs)
        {
            ServicePointManager.SecurityProtocol |= (SecurityProtocolType)3072;   // TLS 1.2: 이 앱은 .NET 4.0 호환 모드라 기본이 Ssl3/Tls 뿐이어서 api.exaone.ai 와 TLS 연결이 안 된다
            var r = (HttpWebRequest)WebRequest.Create(o.ExaoneEndpoint.TrimEnd('/') + "/v1/chat/completions");
            r.Method = "POST"; r.Timeout = timeoutMs; r.ReadWriteTimeout = 90000; r.UserAgent = "SuperHeroes-DeskPet";
            r.Headers["Authorization"] = "Bearer " + o.ExaoneKeyEffective;
            r.Accept = "application/json, text/event-stream";
            var sp = r.ServicePoint;
            sp.Expect100Continue = false; sp.UseNagleAlgorithm = false;
            if (sp.ConnectionLimit < 6) sp.ConnectionLimit = 6;
            return r;
        }

        private static void WriteBody(HttpWebRequest r, string json)
        {
            byte[] bytes = new UTF8Encoding(false).GetBytes(json);
            r.ContentType = "application/json"; r.ContentLength = bytes.Length;
            using (Stream s = r.GetRequestStream()) s.Write(bytes, 0, bytes.Length);
        }

        internal static string Body(ChatOptions o, List<ChatMessage> msgs, bool stream, int numPredict, double temperature)
        {
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            string effort = Array.IndexOf(Efforts, o.ExaoneEffort) >= 0 ? o.ExaoneEffort : "low";
            var b = new StringBuilder("{\"model\":" + Json.Quote(o.ExaoneModel) + ",\"stream\":" + (stream ? "true" : "false"));
            b.Append(",\"max_tokens\":" + (numPredict + ReasoningBudget(effort)) + ",\"reasoning_effort\":" + Json.Quote(effort));
            b.Append(",\"temperature\":" + temperature.ToString("0.0#", inv) + ",\"top_p\":0.95");
            b.Append(",\"messages\":[");
            for (int i = 0; i < msgs.Count; i++)
            { if (i > 0) b.Append(','); b.Append("{\"role\":" + Json.Quote(msgs[i].Role) + ",\"content\":" + Json.Quote(msgs[i].Content) + "}"); }
            return b.Append("]}").ToString();
        }

        /// <summary>오류 응답 본문에서 사람이 읽을 이유를 꺼낸다({"error":{"message"}} 또는 {"result_code","description"}).</summary>
        private static string ErrorText(WebException e, out int status)
        {
            status = 0;
            var resp = e.Response as HttpWebResponse;
            if (resp == null) return e.Message;
            status = (int)resp.StatusCode;
            try
            {
                using (var sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                {
                    object j = Json.Parse(sr.ReadToEnd());
                    string m = Json.Str(Json.Get(j, "error"), "message") ?? Json.Str(j, "description") ?? Json.Str(j, "error") ?? Json.Str(j, "message");
                    if (status == 429) return Loc.T("HTTP 429: EXAONE 서버가 지금 너무 바빠요(요청이 몰렸어요). 잠시 뒤에 다시 해 보세요");
                    if (!string.IsNullOrEmpty(m)) return "HTTP " + status + ": " + m;
                }
            }
            catch { }
            if (status == 429) return Loc.T("HTTP 429: EXAONE 서버가 지금 너무 바빠요(요청이 몰렸어요). 잠시 뒤에 다시 해 보세요");
            return "HTTP " + status + ": " + e.Message;
        }

        /// <summary>
        /// 키와 주소가 맞는지 본다. 빈 messages 로 요청하면 키가 맞을 때는 400/422(입력 오류)가, 틀리면 401 이 와서 토큰을 쓰지 않고 확인할 수 있다.
        /// </summary>
        internal static ProbeResult Probe(ChatOptions o)
        {
            if (string.IsNullOrEmpty(o.ExaoneKeyEffective)) return ProbeResult.NoKey;
            try
            {
                var r = Make(o, 10000);
                WriteBody(r, "{\"model\":" + Json.Quote(o.ExaoneModel) + ",\"max_tokens\":1,\"messages\":[]}");
                using (r.GetResponse()) { }
                return ProbeResult.Ok;
            }
            catch (WebException e)
            {
                int status; ErrorText(e, out status);
                if (status == 401 || status == 403) return ProbeResult.AuthFailed;
                if (status == 404) return ProbeResult.ModelMissing;
                if (status == 400 || status == 422 || status == 429) return ProbeResult.Ok;
                return ProbeResult.Offline;
            }
            catch { return ProbeResult.Offline; }
        }

        /// <summary>
        /// 받은 답 조각을 한 줄 대답으로 다듬는다: 앞쪽 빈 줄은 건너뛰고, 첫 줄바꿈이 나오면 거기서 끝(Ollama 의 stop "\n" 대신).
        /// </summary>
        internal sealed class LineCutter
        {
            private bool started, ended;
            internal bool Ended { get { return ended; } }
            internal string Feed(string piece)
            {
                if (ended || string.IsNullOrEmpty(piece)) return "";
                if (!started) { piece = piece.TrimStart(); if (piece.Length == 0) return ""; started = true; }
                int nl = piece.IndexOfAny(new[] { '\n', '\r' });
                if (nl >= 0) { ended = true; piece = piece.Substring(0, nl); }
                return piece;
            }
        }

        /// <summary>SSE 로 받아 답 조각마다 onToken 을 부른다. 성공하면 null, 실패하면 이유.</summary>
        internal static string Stream(ChatOptions o, List<ChatMessage> msgs, Action<string> onToken, ChatRequest handle, int numPredict = 170, double temperature = .85)
        {
            string err = null;
            for (int attempt = 0; attempt < 4; attempt++)
            {
                if (attempt > 0) { System.Threading.Thread.Sleep(1200); if (handle.Cancelled) return "cancelled"; }
                err = StreamOnce(o, msgs, onToken, handle, numPredict, temperature);
                if (err == null || !err.StartsWith("HTTP 429")) return err;
            }
            return err;
        }

        private static string StreamOnce(ChatOptions o, List<ChatMessage> msgs, Action<string> onToken, ChatRequest handle, int numPredict, double temperature)
        {
            if (string.IsNullOrEmpty(o.ExaoneKeyEffective)) return Loc.T("EXAONE API 키가 없어요");
            HttpWebRequest r = null; bool cutShort = false;
            try
            {
                r = Make(o, 90000);
                handle.Attach(r);
                WriteBody(r, Body(o, msgs, true, numPredict, temperature));
                var cut = new LineCutter(); bool any = false; string finish = null;
                using (var resp = (HttpWebResponse)r.GetResponse())
                using (var sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (handle.Cancelled) return "cancelled";
                        if (!line.StartsWith("data:")) continue;
                        string data = line.Substring(5).Trim();
                        if (data == "[DONE]") break;
                        object j = Json.Parse(data);
                        if (j == null) continue;
                        string err = Json.Str(Json.Get(j, "error"), "message") ?? Json.Str(j, "error"); if (err != null) return err;
                        var choices = Json.Get(j, "choices") as List<object>;
                        if (choices == null || choices.Count == 0) continue;
                        finish = Json.Str(choices[0], "finish_reason") ?? finish;
                        string piece = cut.Feed(Json.Str(Json.Get(choices[0], "delta"), "content"));
                        if (piece.Length > 0) { any = true; onToken(piece); }
                        if (cut.Ended) { cutShort = true; try { r.Abort(); } catch { } break; }
                    }
                }
                if (!any && finish == "length") return Loc.T("추론하다가 토큰이 모자랐어요(추론 강도를 낮춰 보세요)");
                return null;
            }
            catch (WebException e)
            {
                if (cutShort) return null;
                if (handle.Cancelled) return "cancelled";
                int status; return ErrorText(e, out status);
            }
            catch (Exception e) { return cutShort ? null : handle.Cancelled ? "cancelled" : e.Message; }
        }

        /// <summary>스트리밍 없이 한 번에 받는다(기억·일기 같은 보조 호출). 실패하면 null.</summary>
        internal static string Complete(ChatOptions o, List<ChatMessage> msgs, int numPredict, double temperature)
        {
            if (string.IsNullOrEmpty(o.ExaoneKeyEffective)) return null;
            for (int attempt = 0; attempt < 4; attempt++)
            {
                if (attempt > 0) System.Threading.Thread.Sleep(1200);
                bool busy;
                string c = CompleteOnce(o, msgs, numPredict, temperature, out busy);
                if (!busy) return c;
            }
            return null;
        }

        private static string CompleteOnce(ChatOptions o, List<ChatMessage> msgs, int numPredict, double temperature, out bool busy)
        {
            busy = false;
            try
            {
                var r = Make(o, 90000);
                WriteBody(r, Body(o, msgs, false, numPredict, temperature));
                using (var resp = (HttpWebResponse)r.GetResponse())
                using (var sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                {
                    var choices = Json.Get(Json.Parse(sr.ReadToEnd()), "choices") as List<object>;
                    if (choices == null || choices.Count == 0) return null;
                    string c = Json.Str(Json.Get(choices[0], "message"), "content");
                    return c == null ? null : c.Trim();
                }
            }
            catch (WebException e) { var resp = e.Response as HttpWebResponse; busy = resp != null && (int)resp.StatusCode == 429; return null; }
            catch { return null; }
        }
    }

    /// <summary>API 키를 Windows 사용자 계정으로 암호화해(DPAPI) 설정 파일에 둔다. 다른 사용자·다른 PC 에서는 풀 수 없다.</summary>
    internal static class SecretBox
    {
        private static readonly byte[] Salt = Encoding.UTF8.GetBytes("SuperHeroes-DeskPet/EXAONE");

        internal static string Protect(string plain)
        {
            if (string.IsNullOrEmpty(plain)) return "";
            try { return Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(plain), Salt, DataProtectionScope.CurrentUser)); }
            catch { return ""; }
        }

        internal static string Unprotect(string stored)
        {
            if (string.IsNullOrEmpty(stored)) return "";
            try { return Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(stored), Salt, DataProtectionScope.CurrentUser)); }
            catch { return ""; }
        }
    }
}
