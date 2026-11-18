using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace SuperHeroes
{
    /// <summary>
    /// 화면 언어 (한국어 ko · English en).
    /// 한국어 원문이 곧 키다: <c>Loc.T("저장")</c> 은 영어 모드에서 사전(Loc.En.cs)의 영어를, 한국어 모드이거나 항목이 없으면 원문을 돌려준다.
    /// 글 안에 값이 끼면 <c>Loc.TF("친밀도 {0}", n)</c> 처럼 자리표시자로 쓴다(영어는 어순이 달라서 문자열을 이어 붙이지 않는다).
    /// 언어는 수퍼히어로_설정.ini 의 Language=ko|en 에 저장하고, 없으면 Windows 표시 언어를 따른다(한국어면 ko, 그 밖은 en).
    /// 이 파일명·폴더명(수퍼히어로_*.ini, %AppData%\수퍼히어로)은 기존 사용자 데이터를 지키려고 언어와 상관없이 그대로 쓴다.
    /// </summary>
    internal static partial class Loc
    {
        internal const string Ko = "ko", En = "en";
        internal static readonly string[] Codes = { Ko, En };
        internal static readonly string[] NativeNames = { "한국어", "English" };

        /// <summary>현재 언어 코드.</summary>
        internal static string Code = Ko;
        internal static bool IsEn { get { return Code == En; } }
        /// <summary>화면 언어가 바뀐 뒤 알린다(열려 있는 창이 글을 다시 읽도록).</summary>
        internal static event Action Changed;
        /// <summary>--lang 으로 정했으면 파일·OS 설정보다 우선한다(점검용).</summary>
        private static string forced;
        /// <summary>영어 모드에서 사전에 없어 한국어 그대로 나온 문장(점검 모드가 모아 본다).</summary>
        internal static readonly HashSet<string> Missed = new HashSet<string>();
        private static Dictionary<string, string> table;
        private static Dictionary<string, string> Table { get { return table ?? (table = BuildEn()); } }

        internal static CultureInfo Culture { get { return IsEn ? CultureInfo.GetCultureInfo("en-US") : CultureInfo.GetCultureInfo("ko-KR"); } }

        internal static string Normalize(string code)
        { return string.Equals((code ?? "").Trim(), En, StringComparison.OrdinalIgnoreCase) ? En : Ko; }

        /// <summary>Windows 표시 언어가 한국어면 ko, 아니면 en.</summary>
        internal static string DetectOs()
        {
            try { return DetectOs(CultureInfo.CurrentUICulture.Name); }
            catch { return Ko; }
        }

        internal static string DetectOs(string cultureName)
        {
            try { return CultureInfo.GetCultureInfo(cultureName).TwoLetterISOLanguageName == "ko" ? Ko : En; }
            catch { return Ko; }
        }

        /// <summary>프로그램을 시작할 때 한 번: --lang → 저장된 Language → OS 언어 순서로 정한다.</summary>
        internal static void Load()
        {
            if (forced != null) { Code = forced; return; }
            string saved = null;
            try
            {
                string path = Settings.FilePath;
                if (File.Exists(path)) { string v; if (Settings.ReadIni(path).TryGetValue("Language", out v)) saved = v; }
            }
            catch { }
            Code = saved != null ? Normalize(saved) : DetectOs();
        }

        /// <summary>점검용: 저장 파일과 상관없이 이 언어로 고정한다.</summary>
        internal static void Force(string code) { forced = Normalize(code); Code = forced; }

        /// <summary>설정창에서 언어를 바꿀 때. 실제로 바뀌었으면 true.</summary>
        internal static bool Set(string code)
        {
            code = Normalize(code);
            if (code == Code) return false;
            Code = code;
            var h = Changed; if (h != null) h();
            return true;
        }

        /// <summary>한국어 원문을 현재 언어로.</summary>
        internal static string T(string ko)
        {
            if (!IsEn || string.IsNullOrEmpty(ko)) return ko;
            string en;
            if (Table.TryGetValue(ko, out en) && en.Length > 0) return en;
            if (HasHangul(ko)) lock (Missed) Missed.Add(ko);
            return ko;
        }

        /// <summary>자리표시자({0}…)가 있는 한국어 원문을 현재 언어로 바꾼 뒤 값을 채운다.</summary>
        internal static string TF(string ko, params object[] args)
        {
            try { return string.Format(Culture, T(ko), args); }
            catch (FormatException) { return string.Format(Culture, ko, args); }
        }

        /// <summary>한국어와 영어를 곁들여 바로 쓴다(사전에 올리기엔 한 곳에서만 쓰는 글).</summary>
        internal static string P(string ko, string en) { return IsEn ? en : ko; }

        /// <summary>저장된 글이 한국어·영어 기본 문구 그대로면 지금 언어의 기본 문구로 바꿔 준다(사용자가 고친 글은 그대로).</summary>
        internal static string Retarget(string stored, string ko)
        {
            string en; Table.TryGetValue(ko, out en);
            return stored == ko || (en != null && stored == en) ? T(ko) : stored;
        }

        /// <summary>영어 사전 전체(점검용).</summary>
        internal static IEnumerable<KeyValuePair<string, string>> Entries { get { return Table; } }

        internal static bool HasHangul(string s)
        {
            if (s == null) return false;
            foreach (char c in s) if ((c >= '가' && c <= '힣') || (c >= 'ㄱ' && c <= 'ㅣ')) return true;
            return false;
        }

        /// <summary>영어는 같은 뜻이 글자 수로 2~3배 길다: 한국어 기준 글자 수 한도를 언어에 맞게 늘린다.</summary>
        internal static int Chars(int koChars) { return IsEn ? (int)Math.Round(koChars * 2.6) : koChars; }
    }
}
