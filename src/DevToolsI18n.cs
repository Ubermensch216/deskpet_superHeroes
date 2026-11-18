using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SuperHeroes
{
    /// <summary>
    /// 영어/한국어 UI·프롬프트 다국어 점검(--i18n-test &lt;폴더&gt;).
    /// 설정창 6개 페이지, 초과근무 탭, 도우미 창, 메모·알림 창, 히어로 5명 × 대화 모드 5가지에
    /// 미번역 한글이나 깨진 {0} 자리가 남지 않았는지 확인한다.
    /// </summary>
    internal static class DevToolsI18n
    {
        internal static int Run(string dir)
        {
            Directory.CreateDirectory(dir);
            Paths.StorageOverride = Path.Combine(dir, "data");
            string keepLang = Loc.Code;
            var log = new StringBuilder();
            int failures = 0;
            Action<bool, string> check = (ok, name) => { log.AppendLine((ok ? "PASS " : "FAIL ") + name); if (!ok) failures++; };
            try
            {
                // 1. OS 언어 판별 · 저장 · Retarget
                check(Loc.DetectOs("en-US") == Loc.En && Loc.DetectOs("en-GB") == Loc.En && Loc.DetectOs("fr-FR") == Loc.En && Loc.DetectOs("ko-KR") == Loc.Ko, "OS 표시 언어 판별");
                Loc.Set(Loc.En); new Settings().Save(); Loc.Load();
                check(Loc.Code == Loc.En && Loc.IsEn, "영어 설정 저장·불러오기");
                Loc.Set(Loc.Ko); new Settings().Save(); Loc.Load();
                check(Loc.Code == Loc.Ko && !Loc.IsEn, "한국어 설정 저장·불러오기");
                check(Loc.Retarget("점심 먹으러 가자!", "점심 먹으러 가자!") == "점심 먹으러 가자!", "한국어 Retarget");
                Loc.Force(Loc.En);
                check(Loc.Retarget("점심 먹으러 가자!", "점심 먹으러 가자!") == Loc.T("점심 먹으러 가자!"), "영어 Retarget");

                // 2. 사전 항목 검증: 빈 번역 · {0} 자리맞춤 오류 없음
                var badEntries = new List<string>();
                foreach (var kv in Loc.Entries)
                {
                    if (string.IsNullOrEmpty(kv.Value)) { badEntries.Add("empty:" + kv.Key); continue; }
                    if (Placeholders(kv.Key) != Placeholders(kv.Value)) badEntries.Add("placeholders:" + kv.Key);
                }
                check(badEntries.Count == 0, "영어 사전 항목 점검" + (badEntries.Count == 0 ? " (" + Loc.Entries.Count() + "개)" : ": " + string.Join(", ", badEntries.Take(8))));

                // 3. 영어 모드 UI 폼 전수 검사 (미번역 한글 탐색)
                Loc.Force(Loc.En);
                CharacterArt.Reset(1f, 1f);
                PlayOptions.Current = new PlayOptions();
                FriendData.Current = new FriendData();
                ChatOptions.Current = new ChatOptions();
                var app = new App(true);
                app.Notes = new NoteManager(app);
                app.Notes.Start(false);

                var leaked = new List<string>();
                using (var sf = new SettingsForm(app, 0))
                {
                    sf.Show();
                    for (int p = 0; p < 6; p++)
                    {
                        sf.SelectPage(p);
                        Application.DoEvents();
                        ScanHangul(sf, "SettingsPage[" + p + "]", leaked);
                    }
                    sf.SelectPage(SettingsForm.AlarmPageIndex);
                    sf.SelectAlarmTab(1);
                    Application.DoEvents();
                    ScanHangul(sf, "OvertimeTab", leaked);
                }
                using (var lib = new NoteLibraryForm(app.Notes)) { lib.Show(); Application.DoEvents(); ScanHangul(lib, "NoteLibrary", leaked); }
                using (var fp = new FriendPanel(app)) { fp.Show(); Application.DoEvents(); ScanHangul(fp, "FriendPanel", leaked); }
                using (var df = new ChatDiaryForm(app.TrayIconImage)) { df.Show(); Application.DoEvents(); ScanHangul(df, "DiaryForm", leaked); }
                using (var mf = new ChatMemoryForm(app.TrayIconImage)) { mf.Show(); Application.DoEvents(); ScanHangul(mf, "MemoryForm", leaked); }
                using (var ac = new AlarmComposer(app.TrayIconImage, _ => false)) { ac.Show(); Application.DoEvents(); ScanHangul(ac, "AlarmComposer", leaked); }
                using (var rf = new ReminderForm((_, __) => { })) { rf.Show(); Application.DoEvents(); ScanHangul(rf, "ReminderForm", leaked); }
                using (var hn = new HourlyNotice(app.TrayIconImage)) { hn.Show(); Application.DoEvents(); ScanHangul(hn, "HourlyNotice", leaked); }

                if (app.Notes != null) app.Notes.Suspend();
                foreach (var w in app.LiveWindows.ToArray()) { try { w.Close(); w.Dispose(); } catch { } }
                check(leaked.Count == 0, "영어 UI 한글 미노출 점검" + (leaked.Count == 0 ? "" : ": " + string.Join(" | ", leaked.Take(12))));

                // 4. 영어 모드 AI 프롬프트·대화 모드 점검 (히어로 5명 × 모드 5개)
                bool promptOk = true; string promptWhy = "";
                for (int c = 0; c < CharacterArt.Count; c++)
                {
                    string expectedName = ChatPersona.CharacterName(c);
                    for (int m = 0; m < ChatMode.All.Length; m++)
                    {
                        ChatOptions.Current.Mode = m;
                        var msgs = ChatPersona.Build(new List<ChatMessage> { new ChatMessage("user", "Hello") }, new DateTime(2026, 6, 1, 14, 0, 0), c);
                        if (!msgs[0].Content.Contains(expectedName)) { promptOk = false; promptWhy = "missing name c=" + c + " m=" + m; break; }
                        foreach (var cm in msgs)
                            if (Loc.HasHangul(cm.Content)) { promptOk = false; promptWhy = "hangul in prompt c=" + c + " m=" + m + ": " + cm.Content; break; }
                        if (!promptOk) break;
                    }
                    if (!promptOk) break;
                }
                check(promptOk, "영어 AI 프롬프트 5히어로 × 5모드 한글 없음" + (promptOk ? "" : " (" + promptWhy + ")"));
            }
            catch (Exception e)
            {
                log.AppendLine("EXCEPTION " + e);
                failures++;
            }
            finally
            {
                Loc.Force(keepLang ?? Loc.Ko);
                Paths.StorageOverride = null;
            }
            log.AppendLine(failures == 0 ? "ALL OK" : failures + " FAILED");
            File.WriteAllText(Path.Combine(dir, "i18n_result.txt"), log.ToString(), new UTF8Encoding(true));
            Console.Write(log.ToString());
            return failures == 0 ? 0 : 1;
        }

        private static string Placeholders(string s)
        {
            var sb = new StringBuilder();
            for (int i = 0; i + 2 < s.Length; i++)
                if (s[i] == '{' && char.IsDigit(s[i + 1]))
                {
                    int end = s.IndexOf('}', i + 1);
                    if (end > i && end - i <= 8) { sb.Append(s.Substring(i, end - i + 1)); i = end; }
                }
            return sb.ToString();
        }

        private static bool AllowedHangul(string s)
        {
            if (s == "한국어" || s == "한국어 / Korean") return true;
            if (s.Length >= 4 && s[1] == ':' && s[2] == '\\' && s.EndsWith("\\메모")) return true;
            return false;
        }

        private static void ScanHangul(Control c, string scope, List<string> leaked)
        {
            if (c == null) return;
            CheckText(c.Text, scope + ":" + c.GetType().Name, leaked);
            if (!string.IsNullOrEmpty(c.AccessibleName)) CheckText(c.AccessibleName, scope + ":" + c.GetType().Name + ".Acc", leaked);
            var lb = c as ListBox;
            if (lb != null) foreach (object item in lb.Items) CheckText(item == null ? "" : item.ToString(), scope + ":ListItem", leaked);
            var cb = c as ComboBox;
            if (cb != null) foreach (object item in cb.Items) CheckText(item == null ? "" : item.ToString(), scope + ":ComboItem", leaked);
            foreach (Control child in c.Controls) ScanHangul(child, scope, leaked);
        }

        private static void CheckText(string s, string where, List<string> leaked)
        {
            if (string.IsNullOrEmpty(s) || AllowedHangul(s)) return;
            if (Loc.HasHangul(s)) leaked.Add(where + "=" + s.Replace('\r', ' ').Replace('\n', ' '));
        }
    }
}
