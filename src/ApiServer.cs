using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SuperHeroes
{
    /// <summary>
    /// 트레이 프로세스(superheroes.exe) 안에 내장된 로컬 HTTP API 서버(127.0.0.1 전용).
    /// M-deskcal · M-Dir · M-Finder 같은 로컬 데스크톱 앱과 onNara.sAIde · onTalk.sAIde 같은 브라우저 확장이
    /// 별도 서버 실행 없이 알림 · 메모 · 캐릭터 액션 · AI 대화를 호출할 수 있게 한다.
    /// </summary>
    internal sealed class ApiServer : IDisposable
    {
        private sealed class UiJob
        {
            internal Func<object> Work;
            internal object Result;
            internal Exception Error;
            internal readonly ManualResetEventSlim Done = new ManualResetEventSlim(false);
        }

        private sealed class AlertRecord
        {
            internal string Id;
            internal string Message;
            internal string Style;
            internal DateTime CreatedUtc;
            internal bool Acknowledged;
            internal bool Closed;
        }

        private readonly App app;
        private readonly ConcurrentQueue<UiJob> queue = new ConcurrentQueue<UiJob>();
        private readonly ConcurrentDictionary<string, AlertRecord> alerts = new ConcurrentDictionary<string, AlertRecord>(StringComparer.OrdinalIgnoreCase);
        private TcpListener listener;
        private Thread acceptThread;
        private volatile bool running;
        private int boundPort;
        private string lastAlertId;

        internal int Port { get { return boundPort; } }
        internal bool IsRunning { get { return running; } }
        internal static string DiscoveryPath { get { return Path.Combine(Paths.Dir, "수퍼히어로_api.json"); } }

        internal static readonly string[] PoseNames =
        {
            "sit", "sleep", "daydream", "stretch",
            "scratch", "cross", "wave", "flex", "beard",
            "cry", "weep", "hop", "slide",
            "dizzy", "krypto", "fall", "wall", "walldown", "ceiling", "window"
        };

        internal ApiServer(App app)
        {
            this.app = app;
        }

        internal void Start()
        {
            if (running || !app.Settings.ApiOn) return;
            int preferred = app.Settings.ApiPort > 0 ? app.Settings.ApiPort : Settings.DefaultApiPort;
            int[] candidates = new int[10];
            candidates[0] = preferred;
            for (int i = 1; i < candidates.Length; i++) candidates[i] = Settings.DefaultApiPort + i - 1;

            foreach (int p in candidates.Distinct())
            {
                try
                {
                    var l = new TcpListener(IPAddress.Loopback, p);
                    l.Start();
                    listener = l;
                    boundPort = p;
                    running = true;
                    break;
                }
                catch { }
            }
            if (!running) return;

            WriteDiscoveryFile();
            acceptThread = new Thread(AcceptLoop) { IsBackground = true, Name = "SuperHeroesApiAccept" };
            acceptThread.Start();
        }

        private void WriteDiscoveryFile()
        {
            try
            {
                Directory.CreateDirectory(Paths.Dir);
                string json = "{\"ok\":true,\"port\":" + boundPort.ToString(CultureInfo.InvariantCulture)
                    + ",\"url\":" + Json.Quote("http://127.0.0.1:" + boundPort.ToString(CultureInfo.InvariantCulture))
                    + ",\"pid\":" + System.Diagnostics.Process.GetCurrentProcess().Id.ToString(CultureInfo.InvariantCulture) + "}";
                File.WriteAllText(DiscoveryPath, json, new UTF8Encoding(false));
            }
            catch { }
        }

        /// <summary>App.OnTick(UI 스레드)에서 주기적으로 호출되어 큐에 쌓인 API 작업을 안전하게 실행한다.</summary>
        internal void Tick()
        {
            UiJob job;
            int count = 0;
            while (count++ < 16 && queue.TryDequeue(out job))
            {
                try { job.Result = job.Work(); }
                catch (Exception ex) { job.Error = ex; }
                finally { job.Done.Set(); }
            }
        }

        private T RunOnUi<T>(Func<T> func, int timeoutMs = 8000)
        {
            var job = new UiJob { Work = () => func() };
            queue.Enqueue(job);
            if (!job.Done.Wait(timeoutMs)) throw new TimeoutException("UI thread timeout");
            if (job.Error != null) throw job.Error;
            return (T)job.Result;
        }

        private void AcceptLoop()
        {
            while (running)
            {
                try
                {
                    TcpClient client = listener.AcceptTcpClient();
                    ThreadPool.QueueUserWorkItem(delegate { HandleClient(client); });
                }
                catch
                {
                    if (!running) break;
                }
            }
        }

        private void HandleClient(TcpClient client)
        {
            using (client)
            {
                try
                {
                    client.ReceiveTimeout = 10000;
                    client.SendTimeout = 60000;
                    using (NetworkStream ns = client.GetStream())
                    {
                        string method, rawTarget, body, origin;
                        Dictionary<string, string> headers;
                        if (!ReadHttpRequest(ns, out method, out rawTarget, out headers, out body)) return;
                        headers.TryGetValue("origin", out origin);

                        if (!IsAllowedOrigin(origin))
                        {
                            WriteHttpResponse(ns, 403, "Forbidden", "{\"ok\":false,\"error\":\"origin_not_allowed\"}", origin);
                            return;
                        }

                        if (string.Equals(method, "OPTIONS", StringComparison.OrdinalIgnoreCase))
                        {
                            WriteHttpResponse(ns, 204, "No Content", "", origin);
                            return;
                        }

                        string path = rawTarget;
                        string query = "";
                        int qIdx = rawTarget.IndexOf('?');
                        if (qIdx >= 0) { path = rawTarget.Substring(0, qIdx); query = rawTarget.Substring(qIdx + 1); }
                        path = path.TrimEnd('/');
                        if (path.Length == 0) path = "/";

                        int status = 200;
                        string respJson = Dispatch(method.ToUpperInvariant(), path, ParseQuery(query), body, out status);
                        WriteHttpResponse(ns, status, StatusText(status), respJson, origin);
                    }
                }
                catch (Exception ex)
                {
                    FeatureLog.Write(ex);
                }
            }
        }

        internal static bool IsAllowedOrigin(string origin)
        {
            if (string.IsNullOrEmpty(origin)) return true;
            string o = origin.Trim().ToLowerInvariant();
            if (o.StartsWith("chrome-extension://") || o.StartsWith("edge-extension://") || o.StartsWith("moz-extension://") || o.StartsWith("safari-web-extension://"))
                return true;
            return false;
        }

        private static bool ReadHttpRequest(Stream stream, out string method, out string target, out Dictionary<string, string> headers, out string body)
        {
            method = null; target = null; headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); body = "";
            var headBytes = new List<byte>(512);
            int matched = 0;
            while (headBytes.Count < 32768)
            {
                int b = stream.ReadByte();
                if (b < 0) return false;
                headBytes.Add((byte)b);
                if ((matched == 0 && b == '\r') || (matched == 1 && b == '\n') || (matched == 2 && b == '\r') || (matched == 3 && b == '\n'))
                {
                    matched++;
                    if (matched == 4) break;
                }
                else matched = b == '\r' ? 1 : 0;
            }
            if (matched < 4) return false;

            string headText = Encoding.ASCII.GetString(headBytes.ToArray(), 0, headBytes.Count - 4);
            string[] lines = headText.Split(new[] { "\r\n" }, StringSplitOptions.None);
            if (lines.Length == 0) return false;
            string[] reqParts = lines[0].Split(' ');
            if (reqParts.Length < 2) return false;
            method = reqParts[0].Trim();
            target = reqParts[1].Trim();

            for (int i = 1; i < lines.Length; i++)
            {
                int colon = lines[i].IndexOf(':');
                if (colon > 0) headers[lines[i].Substring(0, colon).Trim()] = lines[i].Substring(colon + 1).Trim();
            }

            string lenStr;
            int contentLength = 0;
            if (headers.TryGetValue("Content-Length", out lenStr)) int.TryParse(lenStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out contentLength);
            if (contentLength > 0)
            {
                if (contentLength > 2 * 1024 * 1024) return false;
                byte[] buf = new byte[contentLength];
                int read = 0;
                while (read < contentLength)
                {
                    int n = stream.Read(buf, read, contentLength - read);
                    if (n <= 0) break;
                    read += n;
                }
                body = Encoding.UTF8.GetString(buf, 0, read);
            }
            return true;
        }

        private static void WriteHttpResponse(Stream stream, int status, string statusText, string jsonBody, string origin)
        {
            byte[] payload = string.IsNullOrEmpty(jsonBody) ? new byte[0] : new UTF8Encoding(false).GetBytes(jsonBody);
            var sb = new StringBuilder();
            sb.Append("HTTP/1.1 ").Append(status.ToString(CultureInfo.InvariantCulture)).Append(' ').Append(statusText).Append("\r\n");
            sb.Append("Content-Type: application/json; charset=utf-8\r\n");
            sb.Append("Content-Length: ").Append(payload.Length.ToString(CultureInfo.InvariantCulture)).Append("\r\n");
            sb.Append("Access-Control-Allow-Origin: ").Append(string.IsNullOrEmpty(origin) ? "*" : origin).Append("\r\n");
            sb.Append("Access-Control-Allow-Methods: GET, POST, PUT, PATCH, DELETE, OPTIONS\r\n");
            sb.Append("Access-Control-Allow-Headers: Content-Type, Authorization, X-Bugi-Client, X-SuperHeroes-Client\r\n");
            sb.Append("Access-Control-Allow-Private-Network: true\r\n");
            sb.Append("Connection: close\r\n\r\n");
            byte[] headerBytes = Encoding.ASCII.GetBytes(sb.ToString());
            stream.Write(headerBytes, 0, headerBytes.Length);
            if (payload.Length > 0) stream.Write(payload, 0, payload.Length);
            stream.Flush();
        }

        private static string StatusText(int code)
        {
            switch (code)
            {
                case 200: return "OK";
                case 201: return "Created";
                case 204: return "No Content";
                case 400: return "Bad Request";
                case 403: return "Forbidden";
                case 404: return "Not Found";
                default: return "Internal Server Error";
            }
        }

        private static Dictionary<string, string> ParseQuery(string q)
        {
            var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(q)) return d;
            foreach (string part in q.Split('&'))
            {
                if (part.Length == 0) continue;
                int eq = part.IndexOf('=');
                if (eq < 0) d[Uri.UnescapeDataString(part.Replace('+', ' '))] = "";
                else d[Uri.UnescapeDataString(part.Substring(0, eq).Replace('+', ' '))] = Uri.UnescapeDataString(part.Substring(eq + 1).Replace('+', ' '));
            }
            return d;
        }

        // ───────── 라우팅 ─────────

        private string Dispatch(string method, string path, Dictionary<string, string> query, string body, out int status)
        {
            status = 200;
            try
            {
                object payload = string.IsNullOrWhiteSpace(body) ? null : Json.Parse(body);
                if (method == "GET" && (path == "/" || path == "/api" || path == "/api/health" || path == "/api/status"))
                    return HandleHealth();

                if (path == "/api/alert")
                {
                    if (method == "POST") return HandleAlertCreate(payload, out status);
                    if (method == "GET") return HandleAlertGet(null, out status);
                    if (method == "DELETE") return HandleAlertClear();
                }
                if (path.StartsWith("/api/alert/", StringComparison.OrdinalIgnoreCase) && method == "GET")
                    return HandleAlertGet(path.Substring("/api/alert/".Length), out status);

                if (path == "/api/notes")
                {
                    if (method == "GET") return HandleNotesList(query);
                    if (method == "POST") return HandleNoteCreate(payload, out status);
                }
                if (path.StartsWith("/api/notes/", StringComparison.OrdinalIgnoreCase))
                {
                    string tail = path.Substring("/api/notes/".Length);
                    if (tail.EndsWith("/remind", StringComparison.OrdinalIgnoreCase) && method == "POST")
                    {
                        string idPart = tail.Substring(0, tail.Length - "/remind".Length);
                        return HandleNoteRemind(idPart, payload, out status);
                    }
                    if (method == "GET") return HandleNoteGet(tail, out status);
                    if (method == "PATCH" || method == "PUT" || method == "POST") return HandleNoteUpdate(tail, payload, out status);
                    if (method == "DELETE") return HandleNoteDelete(tail, out status);
                }

                if (path == "/api/pet/actions" && method == "GET")
                    return HandlePetActionsList();
                if (path == "/api/pet/action" && method == "POST")
                    return HandlePetAction(payload, out status);

                if (path == "/api/chat" && method == "POST")
                    return HandleChat(payload, out status);

                status = 404;
                return "{\"ok\":false,\"error\":\"not_found\",\"path\":" + Json.Quote(path) + "}";
            }
            catch (Exception ex)
            {
                status = 500;
                return "{\"ok\":false,\"error\":" + Json.Quote(ex.Message) + "}";
            }
        }

        // ───────── 0. 상태 확인 (/api/health) ─────────

        private string HandleHealth()
        {
            return RunOnUi(() =>
            {
                var b = new StringBuilder();
                b.Append("{\"ok\":true,\"app\":\"superheroes\",\"port\":").Append(boundPort.ToString(CultureInfo.InvariantCulture));
                b.Append(",\"petCount\":").Append(app.LiveWindows.Count.ToString(CultureInfo.InvariantCulture));
                b.Append(",\"petPower\":").Append(PlayOptions.Current.PetPower ? "true" : "false");
                b.Append(",\"hidden\":").Append(app.AllHidden ? "true" : "false");
                b.Append(",\"affinity\":").Append(FriendData.Current.Affinity.ToString(CultureInfo.InvariantCulture));
                b.Append(",\"satiety\":").Append(((int)Math.Floor(FriendData.Current.Satiety)).ToString(CultureInfo.InvariantCulture));
                b.Append(",\"openNotes\":").Append((app.Notes != null ? app.Notes.OpenWindowCount : 0).ToString(CultureInfo.InvariantCulture));
                b.Append(",\"totalNotes\":").Append((app.Notes != null && app.Notes.Store != null ? app.Notes.Store.Count : 0).ToString(CultureInfo.InvariantCulture));
                b.Append(",\"chatEnabled\":").Append(ChatOptions.Current.Enabled ? "true" : "false");
                b.Append(",\"chatProvider\":").Append(Json.Quote(ChatOptions.Current.IsExaone ? "exaone" : "ollama"));
                b.Append(",\"chatModel\":").Append(Json.Quote(ChatOptions.Current.ActiveModel));
                b.Append('}');
                return b.ToString();
            });
        }

        // ───────── 1. 알림 (/api/alert) ─────────

        private string HandleAlertCreate(object payload, out int status)
        {
            string msg = (Json.Str(payload, "message") ?? Json.Str(payload, "text") ?? "").Trim();
            string style = (Json.Str(payload, "style") ?? "big").Trim().ToLowerInvariant();
            string alarmTime = (Json.Str(payload, "alarmTime") ?? "").Trim();
            bool alarmRepeat = Json.Get(payload, "alarmRepeat") is bool ? (bool)Json.Get(payload, "alarmRepeat") : false;
            double seconds = Num(payload, "seconds", 8.0);

            if (string.IsNullOrEmpty(msg) && string.IsNullOrEmpty(alarmTime))
            {
                status = 400;
                return "{\"ok\":false,\"error\":\"message_required\"}";
            }

            status = 200;
            return RunOnUi(() =>
            {
                if (!string.IsNullOrEmpty(alarmTime) && Settings.ParseTime(alarmTime).HasValue)
                {
                    bool added = app.RegisterAlarm(new UserAlarm { On = true, Time = alarmTime, Msg = msg, Repeat = alarmRepeat });
                    if (string.IsNullOrEmpty(msg))
                        return "{\"ok\":" + (added ? "true" : "false") + ",\"alarmTime\":" + Json.Quote(alarmTime) + "}";
                }

                string id = Guid.NewGuid().ToString("N");
                var rec = new AlertRecord { Id = id, Message = msg, Style = style, CreatedUtc = DateTime.UtcNow };
                alerts[id] = rec;
                lastAlertId = id;

                if (style == "bubble")
                {
                    if (app.LiveWindows.Count == 0 && !app.PetOff) app.Spawn();
                    foreach (var w in app.LiveWindows) w.Pet.Cheer(app, msg, Math.Max(2.0, Math.Min(60.0, seconds)));
                    rec.Closed = false;
                }
                else if (style == "window")
                {
                    app.ShowHourWindow(msg);
                }
                else
                {
                    AlertPetWindow win = app.ShowAlert(msg);
                    if (win != null)
                    {
                        win.Acknowledged += delegate { rec.Acknowledged = true; };
                        win.FormClosed += delegate { rec.Closed = true; };
                    }
                }

                return "{\"ok\":true,\"alertId\":" + Json.Quote(id) + ",\"style\":" + Json.Quote(style) + ",\"message\":" + Json.Quote(msg) + "}";
            });
        }

        private string HandleAlertGet(string id, out int status)
        {
            status = 200;
            string targetId = string.IsNullOrEmpty(id) ? lastAlertId : id;
            if (string.IsNullOrEmpty(targetId))
                return "{\"ok\":true,\"active\":false}";
            AlertRecord rec;
            if (!alerts.TryGetValue(targetId, out rec))
            {
                status = 404;
                return "{\"ok\":false,\"error\":\"alert_not_found\"}";
            }
            return "{\"ok\":true,\"alertId\":" + Json.Quote(rec.Id)
                + ",\"message\":" + Json.Quote(rec.Message)
                + ",\"style\":" + Json.Quote(rec.Style)
                + ",\"active\":" + (!rec.Closed ? "true" : "false")
                + ",\"acknowledged\":" + (rec.Acknowledged ? "true" : "false")
                + ",\"closed\":" + (rec.Closed ? "true" : "false") + "}";
        }

        private string HandleAlertClear()
        {
            return RunOnUi(() =>
            {
                app.ClearHourly();
                return "{\"ok\":true}";
            });
        }

        // ───────── 2. 메모 (/api/notes) ─────────

        private string HandleNotesList(Dictionary<string, string> query)
        {
            string q; query.TryGetValue("q", out q);
            string tag; query.TryGetValue("tag", out tag);
            string openStr; query.TryGetValue("openOnly", out openStr);
            bool openOnly = string.Equals(openStr, "true", StringComparison.OrdinalIgnoreCase) || openStr == "1";
            string limitStr; int limit = 100;
            if (query.TryGetValue("limit", out limitStr)) int.TryParse(limitStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out limit);
            limit = Math.Max(1, Math.Min(500, limit));

            return RunOnUi(() =>
            {
                if (app.Notes == null || app.Notes.Store == null) return "{\"ok\":true,\"notes\":[]}";
                string normTag = string.IsNullOrWhiteSpace(tag) ? null : TagParser.Normalize(tag.TrimStart('#'));
                List<Note> list = app.Notes.Store.Query(q, null, normTag, NoteSort.UpdatedDescending, limit);
                if (openOnly) list = list.Where(n => app.Notes.IsOpen(n.Id) || n.IsOpen).ToList();
                var sb = new StringBuilder("{\"ok\":true,\"count\":" + list.Count.ToString(CultureInfo.InvariantCulture) + ",\"notes\":[");
                for (int i = 0; i < list.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    AppendNoteJson(sb, list[i], app.Notes);
                }
                sb.Append("]}");
                return sb.ToString();
            });
        }

        private string HandleNoteGet(string idStr, out int status)
        {
            Guid id;
            if (!Guid.TryParse(idStr, out id)) { status = 400; return "{\"ok\":false,\"error\":\"invalid_note_id\"}"; }
            int code = 200;
            string res = RunOnUi(() =>
            {
                var n = app.Notes != null && app.Notes.Store != null ? app.Notes.Store.Get(id) : null;
                if (n == null) { code = 404; return "{\"ok\":false,\"error\":\"note_not_found\"}"; }
                var sb = new StringBuilder("{\"ok\":true,\"note\":");
                AppendNoteJson(sb, n, app.Notes);
                sb.Append('}');
                return sb.ToString();
            });
            status = code;
            return res;
        }

        private string HandleNoteCreate(object payload, out int status)
        {
            string content = Json.Str(payload, "content") ?? Json.Str(payload, "text") ?? "";
            string title = Json.Str(payload, "title");
            if (!string.IsNullOrEmpty(title) && !content.StartsWith(title, StringComparison.Ordinal))
                content = string.IsNullOrEmpty(content) ? "# " + title : "# " + title + "\n" + content;
            string color = Json.Str(payload, "color");
            string sizeStr = Json.Str(payload, "size");
            bool open = Json.Get(payload, "open") is bool ? (bool)Json.Get(payload, "open") : true;
            bool focus = Json.Get(payload, "focus") is bool ? (bool)Json.Get(payload, "focus") : false;
            bool? pinned = Json.Get(payload, "pinned") is bool ? (bool?)(bool)Json.Get(payload, "pinned") : null;
            double remindMin = Num(payload, "remindMinutes", -1);
            string remindAt = Json.Str(payload, "remindAt");
            string remindRule = Json.Str(payload, "remindRule");

            NoteSizePreset? preset = null;
            NoteSizePreset parsedPreset;
            if (!string.IsNullOrEmpty(sizeStr) && Enum.TryParse(sizeStr, true, out parsedPreset)) preset = parsedPreset;

            status = 201;
            return RunOnUi(() =>
            {
                Note note = app.Notes.CreateNoteFromApi(content, preset, color, open, focus, pinned);
                DateTime due;
                if (!string.IsNullOrEmpty(remindAt) && DateTime.TryParse(remindAt, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out due))
                    app.Notes.AddReminderAt(note.Id, due, remindRule);
                else if (remindMin > 0)
                    app.Notes.AddReminder(note.Id, TimeSpan.FromMinutes(remindMin));

                var sb = new StringBuilder("{\"ok\":true,\"note\":");
                AppendNoteJson(sb, note, app.Notes);
                sb.Append('}');
                return sb.ToString();
            });
        }

        private string HandleNoteUpdate(string idStr, object payload, out int status)
        {
            Guid id;
            if (!Guid.TryParse(idStr, out id)) { status = 400; return "{\"ok\":false,\"error\":\"invalid_note_id\"}"; }
            int code = 200;
            string res = RunOnUi(() =>
            {
                var n = app.Notes != null && app.Notes.Store != null ? app.Notes.Store.Get(id) : null;
                if (n == null) { code = 404; return "{\"ok\":false,\"error\":\"note_not_found\"}"; }

                string newContent = Json.Str(payload, "content");
                string append = Json.Str(payload, "append");
                if (newContent != null) app.Notes.RestoreContent(id, newContent);
                else if (!string.IsNullOrEmpty(append))
                {
                    string merged = string.IsNullOrEmpty(n.Content) ? append : n.Content.TrimEnd() + "\n" + append;
                    app.Notes.RestoreContent(id, merged);
                }

                string color = Json.Str(payload, "color");
                double? opacity = Json.Get(payload, "opacity") is double ? (double?)(double)Json.Get(payload, "opacity") : null;
                bool? pinned = Json.Get(payload, "pinned") is bool ? (bool?)(bool)Json.Get(payload, "pinned") : null;
                if (color != null || opacity.HasValue || pinned.HasValue)
                    app.Notes.SetAppearanceFromApi(id, color, opacity, pinned);

                bool focus = Json.Get(payload, "focus") is bool && (bool)Json.Get(payload, "focus");
                if (Json.Get(payload, "open") is bool)
                    app.Notes.SetOpenFromApi(id, (bool)Json.Get(payload, "open"), focus);
                else if (focus)
                    app.Notes.Focus(id);

                n = app.Notes.Store.Get(id);
                var sb = new StringBuilder("{\"ok\":true,\"note\":");
                AppendNoteJson(sb, n, app.Notes);
                sb.Append('}');
                return sb.ToString();
            });
            status = code;
            return res;
        }

        private string HandleNoteDelete(string idStr, out int status)
        {
            Guid id;
            if (!Guid.TryParse(idStr, out id)) { status = 400; return "{\"ok\":false,\"error\":\"invalid_note_id\"}"; }
            int code = 200;
            string res = RunOnUi(() =>
            {
                var n = app.Notes != null && app.Notes.Store != null ? app.Notes.Store.Get(id) : null;
                if (n == null) { code = 404; return "{\"ok\":false,\"error\":\"note_not_found\"}"; }
                app.Notes.DeleteNotes(new[] { id });
                return "{\"ok\":true,\"deleted\":" + Json.Quote(id.ToString("N")) + "}";
            });
            status = code;
            return res;
        }

        private string HandleNoteRemind(string idStr, object payload, out int status)
        {
            Guid id;
            if (!Guid.TryParse(idStr, out id)) { status = 400; return "{\"ok\":false,\"error\":\"invalid_note_id\"}"; }
            double minutes = Num(payload, "minutes", -1);
            string atStr = Json.Str(payload, "at") ?? Json.Str(payload, "due");
            string rule = Json.Str(payload, "rule");

            int code = 200;
            string res = RunOnUi(() =>
            {
                var n = app.Notes != null && app.Notes.Store != null ? app.Notes.Store.Get(id) : null;
                if (n == null) { code = 404; return "{\"ok\":false,\"error\":\"note_not_found\"}"; }
                DateTime due;
                if (!string.IsNullOrEmpty(atStr) && DateTime.TryParse(atStr, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out due))
                    app.Notes.AddReminderAt(id, due, rule);
                else if (minutes >= 0)
                    app.Notes.AddReminder(id, TimeSpan.FromMinutes(minutes));
                else
                {
                    code = 400;
                    return "{\"ok\":false,\"error\":\"minutes_or_at_required\"}";
                }
                DateTime? next = app.Notes.Store.NextReminder(id, DateTime.Now);
                return "{\"ok\":true,\"noteId\":" + Json.Quote(id.ToString("N"))
                    + ",\"nextReminder\":" + (next.HasValue ? Json.Quote(next.Value.ToString("o", CultureInfo.InvariantCulture)) : "null") + "}";
            });
            status = code;
            return res;
        }

        private static void AppendNoteJson(StringBuilder sb, Note n, NoteManager mgr)
        {
            DateTime? next = mgr != null && mgr.Store != null ? mgr.Store.NextReminder(n.Id, DateTime.Now) : null;
            sb.Append("{\"id\":").Append(Json.Quote(n.Id.ToString("N")));
            sb.Append(",\"title\":").Append(Json.Quote(n.Title ?? ""));
            sb.Append(",\"content\":").Append(Json.Quote(n.Content ?? ""));
            sb.Append(",\"color\":").Append(Json.Quote(n.ColorKey ?? "yellow"));
            sb.Append(",\"isOpen\":").Append((mgr != null && mgr.IsOpen(n.Id)) || n.IsOpen ? "true" : "false");
            sb.Append(",\"pinned\":").Append(n.AlwaysOnTop ? "true" : "false");
            sb.Append(",\"updatedUtc\":").Append(Json.Quote(n.UpdatedUtc.ToString("o", CultureInfo.InvariantCulture)));
            sb.Append(",\"createdUtc\":").Append(Json.Quote(n.CreatedUtc.ToString("o", CultureInfo.InvariantCulture)));
            sb.Append(",\"nextReminder\":").Append(next.HasValue ? Json.Quote(next.Value.ToString("o", CultureInfo.InvariantCulture)) : "null");
            sb.Append(",\"tags\":[");
            for (int i = 0; i < n.Tags.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(Json.Quote(n.Tags[i]));
            }
            sb.Append("]}");
        }

        // ───────── 3. 캐릭터 액션 유도 (/api/pet/actions, /api/pet/action) ─────────

        private string HandlePetActionsList()
        {
            var sb = new StringBuilder("{\"ok\":true,\"poses\":[");
            for (int i = 0; i < PoseNames.Length; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(Json.Quote(PoseNames[i]));
            }
            sb.Append("],\"signatures\":[");
            bool firstSig = true;
            for (int c = 0; c < Signatures.All.Length && c < CharacterArt.Infos.Length; c++)
            {
                for (int i = 0; i < Signatures.All[c].Length; i++)
                {
                    if (!firstSig) sb.Append(',');
                    firstSig = false;
                    sb.Append("{\"character\":").Append(Json.Quote(CharacterArt.Infos[c].Key))
                      .Append(",\"charId\":").Append(c.ToString(CultureInfo.InvariantCulture))
                      .Append(",\"index\":").Append(i.ToString(CultureInfo.InvariantCulture))
                      .Append(",\"key\":").Append(Json.Quote("sig" + i.ToString(CultureInfo.InvariantCulture)))
                      .Append(",\"name\":").Append(Json.Quote(Loc.T(Signatures.All[c][i].Name))).Append('}');
                }
            }
            sb.Append("],\"commands\":[\"feed\",\"wake\",\"sleep\",\"recover\",\"dance\",\"spawn\",\"powerOn\",\"powerOff\",\"show\",\"hide\"]}");
            return sb.ToString();
        }

        private string HandlePetAction(object payload, out int status)
        {
            string action = (Json.Str(payload, "action") ?? Json.Str(payload, "pose") ?? "").Trim();
            string sigStr = (Json.Str(payload, "signature") ?? Json.Str(payload, "sig") ?? Json.Str(payload, "skit") ?? "").Trim();
            string say = Json.Str(payload, "say") ?? Json.Str(payload, "message");
            double seconds = Num(payload, "seconds", 6.0);
            int petIndex = (int)Num(payload, "petIndex", 0);
            string costumeStr = Json.Str(payload, "costume");

            if (!string.IsNullOrEmpty(sigStr) && string.IsNullOrEmpty(action))
                action = "sig:" + sigStr;

            if (string.IsNullOrEmpty(action) && string.IsNullOrEmpty(say) && string.IsNullOrEmpty(costumeStr))
            {
                status = 400;
                return "{\"ok\":false,\"error\":\"action_or_say_required\"}";
            }

            status = 200;
            return RunOnUi(() =>
            {
                string actLower = action.ToLowerInvariant();
                if (actLower == "poweroff") { app.SetPetPower(false); return "{\"ok\":true,\"action\":\"powerOff\"}"; }
                if (actLower == "poweron") { app.SetPetPower(true); return "{\"ok\":true,\"action\":\"powerOn\"}"; }
                if (actLower == "hide") { if (!app.AllHidden) app.ToggleHidden(); return "{\"ok\":true,\"action\":\"hide\"}"; }
                if (actLower == "show") { if (app.PetOff) app.SetPetPower(true); else if (app.AllHidden) app.ToggleHidden(); return "{\"ok\":true,\"action\":\"show\"}"; }
                if (actLower == "recover") { app.RecoverVisible(); return "{\"ok\":true,\"action\":\"recover\"}"; }
                if (actLower == "spawn") { app.SpawnFromUi((int)Num(payload, "character", -1)); return "{\"ok\":true,\"action\":\"spawn\",\"petCount\":" + app.LiveWindows.Count.ToString(CultureInfo.InvariantCulture) + "}"; }

                if (app.PetOff) app.SetPetPower(true);
                if (app.LiveWindows.Count == 0) app.Spawn();
                Pet pet = app.LiveWindows[Math.Max(0, Math.Min(app.LiveWindows.Count - 1, petIndex))].Pet;

                if (!string.IsNullOrEmpty(costumeStr))
                {
                    PlayOptions.Current.ClarkKent = string.Equals(costumeStr, "reporter", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(costumeStr, "clark", StringComparison.OrdinalIgnoreCase);
                    PlayOptions.Current.Save();
                }

                string executed = action;
                if (!string.IsNullOrEmpty(action))
                {
                    int sigIdx;
                    if (actLower == "feed") app.FeedFirst(pet);
                    else if (actLower == "sleep") pet.FallAsleep(app);
                    else if (actLower == "wake") pet.WakeUp(app);
                    else if (actLower == "dance") app.DemonstrateSecretDance(pet);
                    else if (TryResolveSignature(pet, action, out sigIdx))
                    {
                        pet.RequestSignature(app, sigIdx);
                        executed = "sig" + sigIdx.ToString(CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        pet.Demonstrate(app, actLower);
                        executed = actLower;
                    }
                }

                if (!string.IsNullOrEmpty(say))
                {
                    if (string.IsNullOrEmpty(action)) pet.Cheer(app, say, Math.Max(2.0, Math.Min(60.0, seconds)));
                    else { pet.Bubble = say; pet.BubbleUntil = app.Now + Math.Max(2.0, Math.Min(60.0, seconds)); }
                }

                return "{\"ok\":true,\"action\":" + Json.Quote(executed ?? "") + ",\"say\":" + Json.Quote(say ?? "") + "}";
            });
        }

        private static bool TryResolveSignature(Pet pet, string action, out int index)
        {
            index = -1;
            string arg = action;
            if (arg.StartsWith("sig:", StringComparison.OrdinalIgnoreCase)) arg = arg.Substring(4).Trim();
            else if (arg.StartsWith("signature:", StringComparison.OrdinalIgnoreCase)) arg = arg.Substring(10).Trim();
            else if (arg.StartsWith("sig", StringComparison.OrdinalIgnoreCase) && arg.Length > 3 && char.IsDigit(arg[3])) arg = arg.Substring(3).Trim();
            else if (arg.StartsWith("skit:", StringComparison.OrdinalIgnoreCase)) arg = arg.Substring(5).Trim();
            else return false;

            int parsed;
            if (int.TryParse(arg, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed) && parsed >= 0 && parsed < Signatures.Count(pet.Art.Persona))
            {
                index = parsed;
                return true;
            }
            var sigs = Signatures.All[(int)pet.Art.Persona];
            for (int i = 0; i < sigs.Length; i++)
            {
                if (string.Equals(sigs[i].Name, arg, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(Loc.T(sigs[i].Name), arg, StringComparison.OrdinalIgnoreCase))
                {
                    index = i;
                    return true;
                }
            }
            index = 0;
            return true;
        }

        // ───────── 4. AI 채팅 (/api/chat) ─────────

        private string HandleChat(object payload, out int status)
        {
            string message = (Json.Str(payload, "message") ?? Json.Str(payload, "prompt") ?? Json.Str(payload, "text") ?? "").Trim();
            bool openInput = Json.Get(payload, "openInput") is bool && (bool)Json.Get(payload, "openInput");
            bool showOnScreen = Json.Get(payload, "showOnScreen") is bool ? (bool)Json.Get(payload, "showOnScreen") : true;
            bool waitReply = Json.Get(payload, "waitReply") is bool ? (bool)Json.Get(payload, "waitReply") : !openInput;
            string modeStr = Json.Str(payload, "mode");

            if (string.IsNullOrEmpty(message) && !openInput)
            {
                status = 400;
                return "{\"ok\":false,\"error\":\"message_required\"}";
            }

            if (!waitReply)
            {
                status = 200;
                bool started = RunOnUi(() =>
                {
                    ApplyChatModeIfRequested(modeStr);
                    return app.Chat.SendFromUiApi(message, null, openInput);
                });
                return "{\"ok\":" + (started ? "true" : "false") + ",\"started\":" + (started ? "true" : "false") + "}";
            }

            ChatOptions opts = null;
            ChatMode mode = null;
            List<ChatMessage> msgs = null;
            RunOnUi(() =>
            {
                ApplyChatModeIfRequested(modeStr);
                opts = ChatOptions.Current;
                mode = ChatMode.Current;
                int charId = app.LiveWindows.Count > 0 ? app.LiveWindows[0].Pet.Art.Id : 0;
                msgs = app.Chat.BuildPromptForApi(message, charId);
                if (showOnScreen && app.LiveWindows.Count > 0)
                {
                    Pet p = app.LiveWindows[0].Pet;
                    if (!p.Hidden) { p.Bubble = Loc.T("음... 생각 중이야!"); p.BubbleUntil = app.Now + 15; }
                }
                return true;
            });

            var parser = new ReplyParser(mode.MaxSentences, mode.MaxChars);
            var req = new ChatRequest();
            string err = LlmClient.Stream(opts, msgs, tok => parser.Feed(tok), req, mode.NumPredict, mode.Temperature);
            if (err != null)
            {
                status = 502;
                return "{\"ok\":false,\"error\":" + Json.Quote(err) + "}";
            }

            ChatMood mood = parser.Mood;
            string reply = parser.Text;
            if (string.IsNullOrEmpty(reply))
            {
                mood = ChatMood.Think;
                reply = Loc.T("음... 뭐라고 해야 할지 모르겠어.");
            }

            string tag = ChatPersona.TagOf(mood);
            RunOnUi(() =>
            {
                app.Chat.RecordApiTurn(message, mood, reply, showOnScreen);
                return true;
            });

            status = 200;
            return "{\"ok\":true,\"reply\":" + Json.Quote(reply)
                + ",\"mood\":" + Json.Quote(mood.ToString())
                + ",\"tag\":" + Json.Quote(tag) + "}";
        }

        private void ApplyChatModeIfRequested(string modeStr)
        {
            if (string.IsNullOrEmpty(modeStr)) return;
            string m = modeStr.Trim().ToLowerInvariant();
            int idx = -1;
            if (m == "friend" || m == "친구" || m == "0") idx = 0;
            else if (m == "empathy" || m == "공감" || m == "1") idx = 1;
            else if (m == "explain" || m == "설명" || m == "2") idx = 2;
            else if (m == "playful" || m == "장난" || m == "3") idx = 3;
            else if (m == "concise" || m == "간결" || m == "4") idx = 4;
            if (idx >= 0) app.SetChatMode(idx);
        }

        private static double Num(object obj, string key, double fallback)
        {
            object v = Json.Get(obj, key);
            if (v is double) return (double)v;
            if (v is int) return (int)v;
            string s = v as string;
            double d;
            if (s != null && double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out d)) return d;
            return fallback;
        }

        public void Dispose()
        {
            running = false;
            try { if (listener != null) listener.Stop(); } catch { }
            try { if (File.Exists(DiscoveryPath)) File.Delete(DiscoveryPath); } catch { }
        }

        // ───────── CLI 전달 도우미 (이미 실행 중인 트레이 수퍼히어로에게 명령 전달) ─────────

        internal static bool TryForwardCliCommand(string[] args, out int exitCode)
        {
            exitCode = 0;
            if (args == null || args.Length == 0) return false;
            string cmd = args[0];
            if (cmd != "--alert" && cmd != "--note" && cmd != "--action" && cmd != "--chat") return false;

            string arg1 = args.Length >= 2 ? args[1].Replace("\\n", "\n") : "";
            string arg2 = args.Length >= 3 ? args[2] : "";
            int port = ReadRunningPort();

            string path = "/api/health";
            string json = "{}";
            if (cmd == "--alert")
            {
                path = "/api/alert";
                string style = string.IsNullOrEmpty(arg2) ? "big" : arg2;
                json = "{\"message\":" + Json.Quote(arg1) + ",\"style\":" + Json.Quote(style) + "}";
            }
            else if (cmd == "--note")
            {
                path = "/api/notes";
                json = "{\"content\":" + Json.Quote(arg1) + ",\"open\":true}";
            }
            else if (cmd == "--action")
            {
                path = "/api/pet/action";
                json = "{\"action\":" + Json.Quote(arg1) + ",\"say\":" + Json.Quote(arg2) + "}";
            }
            else if (cmd == "--chat")
            {
                path = "/api/chat";
                json = "{\"message\":" + Json.Quote(arg1) + ",\"showOnScreen\":true,\"waitReply\":true}";
            }

            try
            {
                var req = (HttpWebRequest)WebRequest.Create("http://127.0.0.1:" + port.ToString(CultureInfo.InvariantCulture) + path);
                req.Method = "POST";
                req.Timeout = 30000;
                req.Proxy = null;
                byte[] bytes = new UTF8Encoding(false).GetBytes(json);
                req.ContentType = "application/json; charset=utf-8";
                req.ContentLength = bytes.Length;
                using (Stream s = req.GetRequestStream()) s.Write(bytes, 0, bytes.Length);
                using (var resp = (HttpWebResponse)req.GetResponse())
                using (var sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                {
                    Console.WriteLine(sr.ReadToEnd());
                }
                exitCode = 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                exitCode = 1;
            }
            return true;
        }

        private static int ReadRunningPort()
        {
            try
            {
                if (File.Exists(DiscoveryPath))
                {
                    object j = Json.Parse(File.ReadAllText(DiscoveryPath, Encoding.UTF8));
                    object p = Json.Get(j, "port");
                    if (p is double && (double)p >= 1024) return (int)(double)p;
                }
            }
            catch { }
            try
            {
                Settings s = Settings.Load();
                if (s.ApiPort >= 1024) return s.ApiPort;
            }
            catch { }
            return Settings.DefaultApiPort;
        }
    }
}
