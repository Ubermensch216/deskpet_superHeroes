using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace SuperHeroes
{
    /// <summary>시간대 한 줄의 상태. 기록됨 / 기록 전(지금 시간대) / 미기록(지나감) / 미도래</summary>
    internal enum SlotStatus { Recorded, Pending, Missed, Upcoming }

    internal sealed class OvertimeSlot
    {
        internal int Hour; internal SlotStatus Status; internal string RecordedAt = "";
        internal string Label { get { return OvertimeState.HourLabel(Hour); } }
    }

    /// <summary>틱이 만들어 낸 알림 한 건.</summary>
    internal sealed class OvertimeEvent { internal string Message; internal int Hour; }

    /// <summary>
    /// 초과근무 알림 (insarang.sAIde 의 "초과근무 기록 챙기기"를 옮긴 것).
    /// 저장: %AppData%\수퍼히어로\수퍼히어로_초과근무.ini
    /// </summary>
    internal sealed class OvertimeState
    {
        internal const string FileName = "수퍼히어로_초과근무.ini";
        internal const int FirstCheckMin = 3, FirstCheckMax = 10, LastCallMinute = 45;
        public string WatchDate = "";                       // 오늘 챙기기를 켠 날(yyyyMMdd). 오늘이 아니면 꺼짐
        public int StartHour = 18, EndHour = 22;            // 초근 시간: 첫 줄의 시, 마지막 줄 다음 시(1~24)
        public int BasicStartMin = 9 * 60, BasicEndMin = 18 * 60;   // 기본근무시간(기록 대상 아님)
        public int UnusedStartMin = 0, UnusedEndMin = 0;    // 미사용 시간대(표에 안 나오고 알리지 않음). 시작==끝이면 없음, 시작>끝이면 자정을 넘김
        public string RecDate = "";                         // 아래 기록이 어느 날의 것인지
        public readonly SortedDictionary<int, string> Recorded = new SortedDictionary<int, string>();

        internal DateTime LastCheckAt = DateTime.MinValue; internal string LastCheckMsg = "";
        internal int Version; internal bool Dirty;
        private readonly HashSet<string> fired = new HashSet<string>();

        internal static string FilePath { get { return Path.Combine(Paths.Dir, FileName); } }

        // ───────── 시각 도구 ─────────
        internal static string DayKey(DateTime d) { return d.ToString("yyyyMMdd", CultureInfo.InvariantCulture); }
        internal static string HourLabel(int h) { return h.ToString("00") + ":00 ~ " + (h + 1).ToString("00") + ":00"; }
        internal static string HourText(int h) { return h.ToString("00") + ":00"; }
        internal static string MinText(int min) { return (min / 60).ToString("00") + ":" + (min % 60).ToString("00"); }

        /// <summary>"HH:mm" → 0시부터의 분(0~1439). 형식이 틀리면 false.</summary>
        internal static bool ParseHm(string v, out int minutes)
        {
            minutes = 0; if (string.IsNullOrEmpty(v)) return false;
            string[] p = v.Trim().Split(':'); int h, m;
            if (p.Length != 2 || !int.TryParse(p[0], NumberStyles.None, CultureInfo.InvariantCulture, out h) || !int.TryParse(p[1], NumberStyles.None, CultureInfo.InvariantCulture, out m) || h > 23 || m > 59) return false;
            minutes = h * 60 + m; return true;
        }

        /// <summary>그 시간대의 첫 확인 분(3~10). 날짜와 시로만 정해져 시간마다 다르고, 앱을 다시 켜도 바뀌지 않는다.</summary>
        internal static int CheckMinute(DateTime hourStart)
        {
            string seed = DayKey(hourStart) + "-" + hourStart.Hour;
            uint hash = 2166136261;
            foreach (char c in seed) hash = unchecked((hash ^ c) * 16777619);
            return FirstCheckMin + (int)(hash % (uint)(FirstCheckMax - FirstCheckMin + 1));
        }

        // ───────── 오늘 챙기기 · 휴일 판정 ─────────
        private static readonly KoreanLunisolarCalendar Lunar = new KoreanLunisolarCalendar();

        private static bool IsPublicHolidayCore(DateTime d, out bool subEligible, out bool sunOnlySub)
        {
            subEligible = false; sunOnlySub = false;
            int m = d.Month, day = d.Day;
            if (m == 1 && day == 1) return true;
            if ((m == 3 && day == 1) || (m == 5 && day == 5) || (m == 8 && day == 15) || (m == 10 && day == 3) || (m == 10 && day == 9) || (m == 12 && day == 25))
            { subEligible = true; return true; }
            if ((m == 6 && day == 6) || (m == 7 && day == 17)) return true;
            try
            {
                foreach (int offset in new[] { 0, 1 })
                {
                    DateTime t = d.AddDays(offset);
                    int ly = Lunar.GetYear(t), lm = Lunar.GetMonth(t), ld = Lunar.GetDayOfMonth(t);
                    int leap = Lunar.GetLeapMonth(ly);
                    bool isLeap = leap > 0 && lm == leap;
                    if (isLeap) continue;
                    int realMonth = (leap > 0 && lm > leap) ? lm - 1 : lm;
                    if (offset == 1 && realMonth == 1 && ld == 1) { subEligible = true; sunOnlySub = true; return true; }
                    if (offset == 0 && realMonth == 1 && (ld == 1 || ld == 2)) { subEligible = true; sunOnlySub = true; return true; }
                    if (offset == 0 && realMonth == 8 && (ld >= 14 && ld <= 16)) { subEligible = true; sunOnlySub = true; return true; }
                    if (offset == 0 && realMonth == 4 && ld == 8) { subEligible = true; return true; }
                }
            }
            catch { }
            return false;
        }

        /// <summary>오늘이 휴일(토·일요일 또는 대한민국 법정공휴일·대체공휴일)인지 판정한다.</summary>
        internal static bool IsHoliday(DateTime d)
        {
            DateTime dt = d.Date;
            if (dt.DayOfWeek == DayOfWeek.Saturday || dt.DayOfWeek == DayOfWeek.Sunday) return true;
            bool sub, sunOnly;
            if (IsPublicHolidayCore(dt, out sub, out sunOnly)) return true;
            for (int back = 1; back <= 3; back++)
            {
                DateTime prev = dt.AddDays(-back);
                bool pSub, pSunOnly;
                if (!IsPublicHolidayCore(prev, out pSub, out pSunOnly) || !pSub) continue;
                bool overlappedWeekend = pSunOnly ? prev.DayOfWeek == DayOfWeek.Sunday : (prev.DayOfWeek == DayOfWeek.Saturday || prev.DayOfWeek == DayOfWeek.Sunday);
                if (!overlappedWeekend) continue;
                bool allHolidayBetween = true;
                for (int k = 1; k < back; k++)
                {
                    DateTime mid = prev.AddDays(k); bool mSub, mSun;
                    if (mid.DayOfWeek != DayOfWeek.Saturday && mid.DayOfWeek != DayOfWeek.Sunday && !IsPublicHolidayCore(mid, out mSub, out mSun))
                    { allHolidayBetween = false; break; }
                }
                if (allHolidayBetween) return true;
            }
            return false;
        }

        internal bool WatchOn(DateTime now) { return WatchDate == DayKey(now); }

        internal void SetWatch(bool on, DateTime now)
        {
            WatchDate = on ? DayKey(now) : "";
            if (on)
            {
                if (IsHoliday(now)) { BasicStartMin = 0; BasicEndMin = 0; }
                else if (BasicStartMin == BasicEndMin) { BasicStartMin = 9 * 60; BasicEndMin = 18 * 60; }
            }
            Touch();
        }

        /// <summary>기록 표시는 그날 것만 남긴다.</summary>
        internal void EnsureToday(DateTime now)
        {
            string key = DayKey(now);
            if (RecDate == key) return;
            RecDate = key; Recorded.Clear(); fired.Clear(); Touch();
        }

        private void Touch() { Version++; Dirty = true; }

        // ───────── 초근 시간 · 근무시간(기본) ─────────
        internal bool InRange(int hour) { return hour >= StartHour && hour < EndHour; }
        internal bool IsBasicHour(int hour) { return BasicStartMin < BasicEndMin && hour * 60 >= BasicStartMin && (hour + 1) * 60 <= BasicEndMin; }
        internal bool IsUnusedHour(int hour)
        {
            int a = UnusedStartMin, b = UnusedEndMin; if (a == b) return false;
            return a < b ? hour * 60 >= a && (hour + 1) * 60 <= b : hour * 60 >= a || (hour + 1) * 60 <= b;
        }
        internal bool IsHiddenHour(int hour) { return IsBasicHour(hour) || IsUnusedHour(hour); }
        internal bool Highlighted(int hour) { return InRange(hour) && !IsHiddenHour(hour); }

        internal void SetRange(int start, int end)
        {
            start = Math.Max(0, Math.Min(23, start)); end = Math.Max(start + 1, Math.Min(24, end));
            if (start == StartHour && end == EndHour) return;
            StartHour = start; EndHour = end; Touch();
        }

        internal bool SetBasic(string from, string to)
        {
            int a, b;
            if (!ParseHm(from, out a) || !ParseHm(to, out b) || a > b) return false;
            BasicStartMin = a; BasicEndMin = b; Touch(); return true;
        }

        internal bool SetUnused(string from, string to)
        {
            int a, b;
            if (!ParseHm(from, out a) || !ParseHm(to, out b)) return false;
            if (a == UnusedStartMin && b == UnusedEndMin) return true;
            UnusedStartMin = a; UnusedEndMin = b; Touch(); return true;
        }

        internal List<int> RowHours() { return Enumerable.Range(0, 24).Where(h => !IsHiddenHour(h)).ToList(); }

        internal List<OvertimeSlot> Slots(DateTime now)
        {
            var list = new List<OvertimeSlot>();
            bool today = RecDate == DayKey(now);
            foreach (int h in RowHours())
            {
                string at; bool rec = today && Recorded.TryGetValue(h, out at);
                var slot = new OvertimeSlot { Hour = h };
                if (rec) { slot.Status = SlotStatus.Recorded; slot.RecordedAt = Recorded[h]; }
                else slot.Status = h == now.Hour ? SlotStatus.Pending : h < now.Hour ? SlotStatus.Missed : SlotStatus.Upcoming;
                list.Add(slot);
            }
            return list;
        }

        internal int PendingHour(DateTime now)
        {
            if (!WatchOn(now) || !InRange(now.Hour) || IsHiddenHour(now.Hour)) return -1;
            string at; if (RecDate == DayKey(now) && Recorded.TryGetValue(now.Hour, out at)) return -1;
            return now.Hour;
        }

        internal bool MarkRecorded(int hour, bool recorded, DateTime now)
        {
            EnsureToday(now);
            if (hour > now.Hour) return false;
            if (recorded) Recorded[hour] = now.ToString("HH:mm", CultureInfo.InvariantCulture); else Recorded.Remove(hour);
            Touch(); return true;
        }

        // ───────── 시계 ─────────
        internal List<OvertimeEvent> Tick(DateTime now)
        {
            var events = new List<OvertimeEvent>();
            EnsureToday(now);
            if (!WatchOn(now)) return events;

            DateTime hourStart = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0);
            int first = CheckMinute(hourStart);
            foreach (int minute in new[] { first, LastCallMinute })
            {
                DateTime at = hourStart.AddMinutes(minute);
                if (now < at || now >= at.AddSeconds(60)) continue;
                if (!fired.Add(DayKey(now) + "-" + now.Hour + ":" + minute)) continue;
                LastCheckAt = now; Touch();
                if (PendingHour(now) < 0)
                {
                    LastCheckMsg = IsHiddenHour(now.Hour) ? Loc.T("지금은 기본근무 · 미사용 시간대라 기록할 것이 없습니다.") : Loc.TF("{0}은 이미 기록돼 있습니다.", HourLabel(now.Hour));
                    continue;
                }
                string label = HourLabel(now.Hour);
                bool last = minute == LastCallMinute;
                LastCheckMsg = last ? Loc.TF("{0} 마지막 알림을 보냈습니다.", label) : Loc.TF("{0} 기록 알림을 보냈습니다.", label);
                events.Add(new OvertimeEvent { Hour = now.Hour, Message = last
                    ? Loc.TF("{0} 시간대가 15분 남았어!\n아직 초과근무 기록 안 했지? 인사랑에서 기록하고 나를 눌러 줘~", label)
                    : Loc.TF("{0} 초과근무 기록할 시간이야!\n인사랑에 기록하고 나를 눌러 줘. '기록됨'으로 표시할게!", label) });
            }
            return events;
        }

        internal bool SaveIfDirty() { if (!Dirty) return false; return Save(); }

        // ───────── 저장 · 불러오기 ─────────
        internal static string OneLine(string s) { return (s ?? "").Replace("\r", " ").Replace("\n", " ").Trim(); }

        internal static OvertimeState Load()
        {
            var s = new OvertimeState();
            try
            {
                if (!File.Exists(FilePath)) return s;
                var d = Settings.ReadIni(FilePath); string v; int n;
                if (d.TryGetValue("WatchDate", out v) && v.Length == 8) s.WatchDate = v;
                int a = 18, b = 22;
                if (d.TryGetValue("StartHour", out v) && int.TryParse(v, out n)) a = Math.Max(0, Math.Min(23, n));
                if (d.TryGetValue("EndHour", out v) && int.TryParse(v, out n)) b = Math.Max(1, Math.Min(24, n));
                if (a < b) { s.StartHour = a; s.EndHour = b; }
                string bs, be;
                if (d.TryGetValue("BasicStart", out bs) && d.TryGetValue("BasicEnd", out be)) s.SetBasic(bs, be);
                string us, ue;
                if (d.TryGetValue("UnusedStart", out us) && d.TryGetValue("UnusedEnd", out ue)) s.SetUnused(us, ue);
                if (d.TryGetValue("RecDate", out v) && v.Length == 8) s.RecDate = v;
                if (d.TryGetValue("Recorded", out v) && s.RecDate.Length == 8)
                    foreach (string item in v.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        string[] p = item.Split('@'); int h, m;
                        if (p.Length == 2 && int.TryParse(p[0], out h) && h >= 0 && h < 24 && ParseHm(p[1], out m)) s.Recorded[h] = p[1].Trim();
                    }
            }
            catch { }
            s.Dirty = false; s.Version = 0;
            return s;
        }

        internal bool Save()
        {
            var b = new StringBuilder();
            b.AppendLine("; 수퍼히어로 초과근무 알림 설정. 지우면 처음 상태로 돌아가요.");
            b.AppendLine("WatchDate=" + WatchDate);
            b.AppendLine("StartHour=" + StartHour); b.AppendLine("EndHour=" + EndHour);
            b.AppendLine("BasicStart=" + MinText(BasicStartMin)); b.AppendLine("BasicEnd=" + MinText(BasicEndMin));
            b.AppendLine("UnusedStart=" + MinText(UnusedStartMin)); b.AppendLine("UnusedEnd=" + MinText(UnusedEndMin));
            b.AppendLine("RecDate=" + RecDate);
            b.AppendLine("Recorded=" + string.Join("|", Recorded.Select(kv => kv.Key + "@" + kv.Value).ToArray()));
            try { Directory.CreateDirectory(Paths.Dir); File.WriteAllText(FilePath, b.ToString(), new UTF8Encoding(true)); Dirty = false; return true; }
            catch { return false; }
        }

        // ───────── 자체 점검 (superheroes.exe --selftest) ─────────
        internal static void SelfTests(Action<bool, string> check)
        {
            var t0 = new DateTime(2026, 11, 12, 18, 0, 0);
            bool range = true, stable = true; var minutes = new HashSet<int>();
            for (int h = 0; h < 24; h++) { int m = CheckMinute(t0.Date.AddHours(h)); range &= m >= 3 && m <= 10; stable &= m == CheckMinute(t0.Date.AddHours(h)); minutes.Add(m); }
            check(range && stable && minutes.Count > 1, "초과근무: 첫 확인 분은 3~10분이고 시간마다 다르며 다시 계산해도 같음");
            check(CheckMinute(t0) != 0 && LastCallMinute == 45, "초과근무: 마지막 알림은 45분");

            int mm;
            check(ParseHm("09:05", out mm) && mm == 545 && !ParseHm("24:00", out mm) && !ParseHm("9", out mm) && !ParseHm("-1:00", out mm), "초과근무: HH:mm 해석");
            var st = new OvertimeState();
            check(st.IsBasicHour(9) && st.IsBasicHour(17) && !st.IsBasicHour(18) && !st.IsBasicHour(8), "초과근무: 기본근무 09~18 은 기록 대상이 아님");
            check(!st.SetBasic("18:00", "09:00") && !st.SetBasic("abc", "10:00") && st.SetBasic("08:30", "17:30") && st.IsBasicHour(9) && !st.IsBasicHour(17) && !st.IsBasicHour(8), "초과근무: 기본근무 바꾸기(앞뒤 뒤집힘 거부)");
            st.SetBasic("09:00", "18:00");

            st.SetRange(10, 23);
            Func<IEnumerable<int>> lit = () => Enumerable.Range(0, 24).Where(h => st.Highlighted(h)).ToList();
            check(st.RowHours().SequenceEqual(Enumerable.Range(0, 24).Where(h => h < 9 || h >= 18)), "초과근무: 시간표에서 기본근무(09~18) 줄은 빠짐");
            check(lit().SequenceEqual(new[] { 18, 19, 20, 21, 22 }), "초과근무: 초근 10~23시여도 기본근무(09~18) 줄은 하이라이트 안 됨");
            st.SetRange(6, 22);
            check(lit().SequenceEqual(new[] { 6, 7, 8, 18, 19, 20, 21 }), "초과근무: 이른 아침 초근 줄은 하이라이트되고 기본근무 줄은 건너뜀");
            st.SetRange(18, 22);
            check(lit().SequenceEqual(new[] { 18, 19, 20, 21 }), "초과근무: 기본 초근 18~22시 하이라이트");
            check(st.Slots(new DateTime(2026, 11, 12, 20, 15, 0)).All(x => !st.IsBasicHour(x.Hour)) && st.Slots(new DateTime(2026, 11, 12, 20, 15, 0)).Count == 15, "초과근무: 시간표 줄에 기본근무 시간대가 없음(24-9=15줄)");
            {
                var tn = new DateTime(2026, 11, 12, 20, 15, 0); int before = st.Slots(tn).Count;
                st.SetBasic("10:00", "16:00");
                bool now10 = st.Slots(tn).Any(x => x.Hour == 9) && !st.Slots(tn).Any(x => x.Hour == 12) && st.Slots(tn).Count == 18 && before == 15;
                st.SetBasic("09:00", "18:00");
                check(now10 && st.Slots(tn).Count == 15, "초과근무: 기본근무를 바꾸면 시간표 줄이 바로 바뀜(09~18 → 10~16 이면 9시가 나타남)");
            }
            {
                var tn = new DateTime(2026, 11, 12, 20, 15, 0);
                check(!st.IsUnusedHour(0) && !st.IsUnusedHour(23) && st.Slots(tn).Count == 15, "초과근무: 미사용 시간대 기본값은 없음");
                check(st.SetUnused("00:00", "06:00") && Enumerable.Range(0, 24).Where(st.IsUnusedHour).SequenceEqual(Enumerable.Range(0, 6)) && st.Slots(tn).Count == 9 && !st.Slots(tn).Any(x => x.Hour < 6), "초과근무: 미사용 00~06시는 표에서 빠짐(15-6=9줄)");
                st.SetUnused("23:00", "07:00");
                check(Enumerable.Range(0, 24).Where(st.IsUnusedHour).SequenceEqual(new[] { 0, 1, 2, 3, 4, 5, 6, 23 }) && st.Slots(tn).Count == 7, "초과근무: 자정을 넘는 미사용 23~07시도 빠짐");
                st.SetUnused("21:30", "22:30"); check(!st.IsUnusedHour(21) && !st.IsUnusedHour(22), "초과근무: 시 단위로 통째로 안에 든 시간만 미사용");
                st.SetUnused("20:00", "22:00"); st.SetRange(18, 23);
                check(st.Slots(tn).All(x => x.Hour != 20 && x.Hour != 21) && !st.Highlighted(20) && st.Highlighted(19) && st.Highlighted(22), "초과근무: 초근 범위 안의 미사용 시간은 하이라이트도 안 됨");
                var ts = new OvertimeState(); ts.SetWatch(true, tn); ts.SetRange(18, 23); ts.SetUnused("20:00", "22:00");
                check(ts.PendingHour(new DateTime(2026, 11, 12, 20, 5, 0)) < 0 && ts.PendingHour(new DateTime(2026, 11, 12, 19, 5, 0)) == 19 && ts.PendingHour(new DateTime(2026, 11, 12, 22, 5, 0)) == 22, "초과근무: 미사용 시간대에는 기록 알림 대상이 아님");
                check(!st.SetUnused("25:00", "06:00") && !st.SetUnused("abc", "06:00"), "초과근무: 미사용 시각 형식 오류는 거부");
                st.SetUnused("00:00", "00:00");
            }
            st.SetRange(20, 5); check(st.StartHour == 20 && st.EndHour == 21, "초과근무: 초근끝은 초근시작보다 뒤로 보정");
            st.SetRange(18, 22);

            check(!st.WatchOn(t0), "초과근무: 처음엔 꺼짐");
            st.SetWatch(true, t0);
            check(st.WatchOn(t0) && st.WatchOn(t0.AddHours(5)) && !st.WatchOn(t0.AddDays(1)), "초과근무: 오늘 챙기기는 그날만 켜져 있음(다음 날 저절로 꺼짐)");
            {
                var sat = new DateTime(2026, 11, 14, 14, 0, 0);
                var xmas = new DateTime(2026, 12, 25, 10, 0, 0);
                check(!IsHoliday(t0) && IsHoliday(sat) && IsHoliday(sat.AddDays(1)) && IsHoliday(xmas), "초과근무: 평일·주말·공휴일 판정");
                var hol = new OvertimeState();
                hol.SetWatch(true, sat);
                check(hol.BasicStartMin == 0 && hol.BasicEndMin == 0 && hol.RowHours().Count == 24, "초과근무: 휴일에 켜면 근무시간이 00:00~00:00 으로 없어져 24시간 모두 표시됨");
                hol.SetWatch(true, t0);
                check(hol.BasicStartMin == 9 * 60 && hol.BasicEndMin == 18 * 60 && hol.RowHours().Count == 15, "초과근무: 평일에 다시 켜면 기본 근무시간(09:00~18:00) 복원");
            }

            var now = new DateTime(2026, 11, 12, 20, 15, 0); st.EnsureToday(now);
            var slots = st.Slots(now);
            check(slots.First(x => x.Hour == 19).Status == SlotStatus.Missed && slots.First(x => x.Hour == 20).Status == SlotStatus.Pending && slots.First(x => x.Hour == 21).Status == SlotStatus.Upcoming, "초과근무: 미기록 · 기록 전 · 미도래 판정");
            check(st.PendingHour(now) == 20, "초과근무: 지금 기록할 시간대");
            check(!st.MarkRecorded(21, true, now) && st.MarkRecorded(20, true, now) && st.Slots(now).First(x => x.Hour == 20).Status == SlotStatus.Recorded && st.Slots(now).First(x => x.Hour == 20).RecordedAt == "20:15" && st.PendingHour(now) < 0, "초과근무: 기록했어요 표시(미래 시간대는 안 됨)");
            check(st.MarkRecorded(20, false, now) && st.PendingHour(now) == 20, "초과근무: 기록 표시 되돌리기");

            st.Dirty = false; DateTime h20 = new DateTime(2026, 11, 12, 20, 0, 0); int c20 = CheckMinute(h20);
            check(st.Tick(h20.AddMinutes(c20 - 1).AddSeconds(59)).Count == 0, "초과근무: 확인 분 직전에는 안 울림");
            var ev = st.Tick(h20.AddMinutes(c20));
            check(ev.Count == 1 && ev[0].Hour == 20 && ev[0].Message.Contains("20:00 ~ 21:00") && ev[0].Message.Contains("기록") && st.Tick(h20.AddMinutes(c20).AddSeconds(30)).Count == 0, "초과근무: 첫 확인 분에 한 번만 알림");
            check(st.LastCheckAt == h20.AddMinutes(c20) && st.LastCheckMsg.Contains("20:00 ~ 21:00"), "초과근무: 마지막 확인 기록");
            var last = st.Tick(h20.AddMinutes(45));
            check(last.Count == 1 && last[0].Message.Contains("15분 남았어"), "초과근무: 45분 마지막 알림");
            st.MarkRecorded(21, true, new DateTime(2026, 11, 12, 21, 1, 0));
            check(st.Tick(new DateTime(2026, 11, 12, 21, CheckMinute(new DateTime(2026, 11, 12, 21, 0, 0)), 0)).Count == 0 && st.LastCheckMsg.Contains("이미 기록"), "초과근무: 기록한 시간대는 알리지 않음");
            check(st.Tick(new DateTime(2026, 11, 12, 22, CheckMinute(new DateTime(2026, 11, 12, 22, 0, 0)), 0)).Count == 0, "초과근무: 초근끝 이후에는 안 울림");
            check(st.Tick(new DateTime(2026, 11, 12, 17, 45, 0)).Count == 0, "초과근무: 초근시작 전에는 안 울림");
            var off = new OvertimeState(); check(off.Tick(h20.AddMinutes(c20)).Count == 0, "초과근무: 꺼져 있으면 안 울림");

            var day = new OvertimeState(); day.SetWatch(true, t0); day.EnsureToday(t0); day.MarkRecorded(18, true, t0);
            var tomorrow = t0.AddDays(1); day.EnsureToday(tomorrow);
            check(day.Recorded.Count == 0 && !day.WatchOn(tomorrow), "초과근무: 다음 날에는 기록 표시가 지워지고 챙기기가 꺼짐");

            var sv = new OvertimeState { StartHour = 19, EndHour = 23 }; sv.SetWatch(true, DateTime.Now); sv.SetBasic("08:30", "17:30"); sv.SetUnused("23:00", "06:00"); sv.EnsureToday(DateTime.Now);
            sv.MarkRecorded(DateTime.Now.Hour, true, DateTime.Now);
            check(sv.Save(), "초과근무: 저장");
            var ld = Load();
            check(ld.StartHour == 19 && ld.EndHour == 23 && ld.BasicStartMin == 510 && ld.BasicEndMin == 1050 && ld.UnusedStartMin == 1380 && ld.UnusedEndMin == 360 && ld.WatchOn(DateTime.Now) && ld.Recorded.ContainsKey(DateTime.Now.Hour), "초과근무: 불러오기 일치");
        }
    }
}
