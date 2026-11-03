using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SuperHeroes
{
    /// <summary>말 걸기 단축키 규칙: 보조키(Ctrl · Alt · Shift) 2개 이상 + 글쇠 1개, 모두 합쳐 3개 이상. 값은 RegisterHotKey 의 modifier / 가상 키 코드.</summary>
    internal static class ChatHotkey
    {
        internal const uint Alt = 1, Ctrl = 2, Shift = 4;
        internal const uint DefaultMods = Ctrl | Alt, DefaultKey = (uint)'C';
        internal const int MinKeys = 3;

        // 같이 쓰는 다른 전역 단축키(메모). 겹치면 둘 중 하나가 등록되지 않는다.
        private static readonly KeyValuePair<uint, uint>[] Taken =
        {
            new KeyValuePair<uint, uint>(Ctrl | Alt, (uint)'N'), new KeyValuePair<uint, uint>(Ctrl | Shift, (uint)'F'),
            new KeyValuePair<uint, uint>(Ctrl | Shift, (uint)'P'), new KeyValuePair<uint, uint>(Ctrl | Shift, (uint)'R')
        };

        internal static int ModCount(uint mods) { return ((mods & Ctrl) != 0 ? 1 : 0) + ((mods & Alt) != 0 ? 1 : 0) + ((mods & Shift) != 0 ? 1 : 0); }

        internal static bool IsModifierKey(Keys k) { return k == Keys.ControlKey || k == Keys.Menu || k == Keys.ShiftKey || k == Keys.LControlKey || k == Keys.RControlKey || k == Keys.LMenu || k == Keys.RMenu || k == Keys.LShiftKey || k == Keys.RShiftKey; }

        /// <summary>쓸 수 있는 조합인가. 안 되면 이유를 reason 에 돌려준다.</summary>
        internal static bool Valid(uint mods, uint key, out string reason)
        {
            reason = null;
            if (key == 0 || ModCount(mods) + 1 < MinKeys) { reason = "키를 " + MinKeys + "개 이상 눌러야 해요 (예: Ctrl + Alt + C)"; return false; }
            Keys k = (Keys)key;
            if (k == Keys.Escape || k == Keys.Back || k == Keys.Tab || k == Keys.LWin || k == Keys.RWin)
            { reason = "이 키는 단축키로 쓸 수 없어요"; return false; }
            if (Taken.Any(t => t.Key == mods && t.Value == key)) { reason = "메모 단축키와 겹쳐요. 다른 조합을 눌러 주세요"; return false; }
            return true;
        }

        internal static string KeyName(uint vk)
        {
            Keys k = (Keys)vk;
            if (k >= Keys.D0 && k <= Keys.D9) return ((char)('0' + (k - Keys.D0))).ToString();
            if (k >= Keys.NumPad0 && k <= Keys.NumPad9) return "Num" + (k - Keys.NumPad0);
            switch (k)
            {
                case Keys.Oemtilde: return "`"; case Keys.OemMinus: return "-"; case Keys.Oemplus: return "=";
                case Keys.OemOpenBrackets: return "["; case Keys.OemCloseBrackets: return "]"; case Keys.OemPipe: return "\\";
                case Keys.OemSemicolon: return ";"; case Keys.OemQuotes: return "'"; case Keys.Oemcomma: return ",";
                case Keys.OemPeriod: return "."; case Keys.OemQuestion: return "/"; case Keys.Return: return "Enter";
                case Keys.Next: return "PageDown"; case Keys.Prior: return "PageUp"; case Keys.Capital: return "CapsLock";
            }
            return k.ToString();
        }

        internal static IEnumerable<string> ModNames(uint mods)
        {
            if ((mods & Ctrl) != 0) yield return "Ctrl";
            if ((mods & Alt) != 0) yield return "Alt";
            if ((mods & Shift) != 0) yield return "Shift";
        }

        /// <summary>"Ctrl + Alt + C" 처럼 사람이 읽는 글. sep 으로 구분 기호를 고른다(메뉴는 "+", 설정은 " + ").</summary>
        internal static string Text(uint mods, uint key, string sep = " + ")
        {
            return string.Join(sep, ModNames(mods).Concat(new[] { KeyName(key) }).ToArray());
        }
    }

    /// <summary>
    /// 단축키를 누른 키로 정하는 입력란. 클릭하면 비워지고, 키를 하나씩 누를 때마다 "Ctrl + " 처럼 + 를 붙여 다음 키를 기다린다.
    /// 보조키 두 개 이상 + 글쇠를 누르면 확정된다(Esc: 취소 · Backspace: 방금 누른 키 취소).
    /// </summary>
    internal sealed class HotkeyBox : TextBox
    {
        private uint mods, key, pending;
        private readonly List<uint> order = new List<uint>();
        private bool capturing;

        internal uint Mods { get { return mods; } }
        internal uint Key { get { return key; } }
        /// <summary>안내·거절 이유를 알릴 때(없으면 null).</summary>
        internal event Action<string> Hint;
        internal event Action Changed;

        internal HotkeyBox(uint m, uint k)
        {
            ReadOnly = true; ShortcutsEnabled = false; Cursor = Cursors.Hand; BackColor = SystemColors.Window; TabStop = true; Width = 190; Margin = new Padding(3);
            SetValue(m, k);
        }

        internal void SetValue(uint m, uint k)
        {
            mods = m; key = k; capturing = false; pending = 0; order.Clear();
            ForeColor = SystemColors.WindowText; Text = ChatHotkey.Text(m, k);
        }

        private void Begin()
        {
            if (capturing) return;
            capturing = true; pending = 0; order.Clear(); ShowPending(); Notify("키를 하나씩 누르세요. 보조키(Ctrl · Alt · Shift) 2개 이상 + 글쇠 1개");
        }

        private void Cancel() { if (!capturing) return; capturing = false; ForeColor = SystemColors.WindowText; Text = ChatHotkey.Text(mods, key); Notify(null); }

        private void ShowPending()
        {
            ForeColor = Color.FromArgb(54, 96, 190);
            Text = pending == 0 ? "키를 누르세요..." : string.Join(" + ", ChatHotkey.ModNames(pending).ToArray()) + " + ";
            SelectionStart = Text.Length;
        }

        private void Notify(string text) { if (Hint != null) Hint(text); }

        protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); Focus(); Begin(); }
        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); if (MouseButtons == MouseButtons.None) Begin(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Cancel(); }

        protected override bool IsInputKey(Keys keyData) { return capturing || base.IsInputKey(keyData); }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (!capturing) return base.ProcessCmdKey(ref msg, keyData);
            Keys k = keyData & Keys.KeyCode;
            if (k == Keys.Escape) { Cancel(); return true; }
            if (k == Keys.Back) { if (order.Count > 0) { pending &= ~order[order.Count - 1]; order.RemoveAt(order.Count - 1); } ShowPending(); Notify(null); return true; }
            uint bit = k == Keys.ControlKey || k == Keys.LControlKey || k == Keys.RControlKey ? ChatHotkey.Ctrl
                     : k == Keys.Menu || k == Keys.LMenu || k == Keys.RMenu ? ChatHotkey.Alt
                     : k == Keys.ShiftKey || k == Keys.LShiftKey || k == Keys.RShiftKey ? ChatHotkey.Shift : 0;
            if (bit != 0)
            {
                if ((pending & bit) == 0) { pending |= bit; order.Add(bit); }
                ShowPending(); Notify(null);
                return true;
            }
            string reason;
            if (!ChatHotkey.Valid(pending, (uint)k, out reason))
            {
                pending = 0; order.Clear(); ShowPending();
                Notify(reason);
                return true;
            }
            mods = pending; key = (uint)k; capturing = false; pending = 0; order.Clear();
            ForeColor = SystemColors.WindowText; Text = ChatHotkey.Text(mods, key);
            Notify(null);
            if (Changed != null) Changed();
            return true;
        }
    }
}
