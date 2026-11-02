using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace DragonBall
{
    /// <summary>
    /// 메모 본문 상자. RichTextBox 에 세 가지를 더한다: 붙여넣기를 글자만 받게 가로채기(서식 · 이미지는 위에서 처리),
    /// 서식 명령 키를 상자가 먼저 먹지 못하게 가로채기, 실행 취소가 되는 편집과 글자 서식 읽기·쓰기.
    /// </summary>
    internal sealed class NoteBox : RichTextBox
    {
        private const int WM_IME_STARTCOMPOSITION = 0x010D, WM_IME_ENDCOMPOSITION = 0x010E, WM_KILLFOCUS = 0x0008;
        private const int WM_PASTE = 0x0302, WM_SETREDRAW = 0x000B, EM_REPLACESEL = 0x00C2, EM_SETSEL = 0x00B1, EM_GETCHARFORMAT = 0x043A, EM_SETCHARFORMAT = 0x0444, EM_EMPTYUNDOBUFFER = 0x00CD;
        private const int SCF_SELECTION = 1, SCF_ALL = 4;
        internal const int CFM_BOLD = 1, CFM_ITALIC = 2, CFM_UNDERLINE = 4, CFM_STRIKEOUT = 8, CFM_COLOR = 0x40000000;
        internal const int CFE_BOLD = 1, CFE_ITALIC = 2, CFE_UNDERLINE = 4, CFE_STRIKEOUT = 8;
        internal const int FourStyles = CFM_BOLD | CFM_ITALIC | CFM_UNDERLINE | CFM_STRIKEOUT;
        private static readonly Regex LoneLf = new Regex("\r?\n");

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct CHARFORMAT2
        {
            public int cbSize; public int dwMask; public int dwEffects; public int yHeight; public int yOffset; public int crTextColor;
            public byte bCharSet; public byte bPitchAndFamily;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string szFaceName;
            public short wWeight; public short sSpacing; public int crBackColor; public int lcid; public int dwReserved; public short sStyle; public short wKerning;
            public byte bUnderlineType; public byte bAnimation; public byte bRevAuthor; public byte bUnderlineColor;
        }

        [DllImport("user32.dll", EntryPoint = "SendMessageW", CharSet = CharSet.Unicode)] private static extern IntPtr SendMessageW(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam);
        [DllImport("user32.dll", EntryPoint = "SendMessageW", CharSet = CharSet.Unicode)] private static extern IntPtr SendMessageFmt(IntPtr hwnd, int msg, IntPtr wparam, ref CHARFORMAT2 lparam);

        internal Func<bool> PasteHandler;
        internal Func<Keys, bool> CommandKey;

        internal bool Composing { get; private set; }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_PASTE && PasteHandler != null && PasteHandler()) return;
            if (m.Msg == WM_IME_STARTCOMPOSITION) Composing = true;
            base.WndProc(ref m);
            if (m.Msg == WM_IME_ENDCOMPOSITION || m.Msg == WM_KILLFOCUS)
            {
                bool was = Composing; Composing = false;
                if (was && CompositionEnded != null) CompositionEnded();
            }
        }

        internal Action CompositionEnded;

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (CommandKey != null && CommandKey(keyData)) return true;
            return base.ProcessCmdKey(ref msg, keyData);
        }

        internal void SetRedraw(bool on)
        {
            SendMessageW(Handle, WM_SETREDRAW, new IntPtr(on ? 1 : 0), IntPtr.Zero);
            if (on) Invalidate();
        }

        internal void ReplaceRange(int start, int length, string text)
        {
            Select(start, length);
            IntPtr p = Marshal.StringToHGlobalUni(LoneLf.Replace(text ?? "", "\r"));
            try { SendMessageW(Handle, EM_REPLACESEL, new IntPtr(1), p); } finally { Marshal.FreeHGlobal(p); }
        }

        internal void ClearUndoStack() { ClearUndo(); SendMessageW(Handle, EM_EMPTYUNDOBUFFER, IntPtr.Zero, IntPtr.Zero); }

        internal void GetEffects(out int mask, out int effects)
        {
            var cf = new CHARFORMAT2 { cbSize = Marshal.SizeOf(typeof(CHARFORMAT2)), szFaceName = "" };
            SendMessageFmt(Handle, EM_GETCHARFORMAT, new IntPtr(SCF_SELECTION), ref cf);
            mask = cf.dwMask & FourStyles; effects = cf.dwEffects & FourStyles;
        }

        internal void SetEffects(int bit, bool on, bool all = false)
        {
            var cf = new CHARFORMAT2 { cbSize = Marshal.SizeOf(typeof(CHARFORMAT2)), dwMask = bit, dwEffects = on ? bit : 0, szFaceName = "" };
            SendMessageFmt(Handle, EM_SETCHARFORMAT, new IntPtr(all ? SCF_ALL : SCF_SELECTION), ref cf);
        }

        internal void PaintInk(Color ink)
        {
            var cf = new CHARFORMAT2 { cbSize = Marshal.SizeOf(typeof(CHARFORMAT2)), dwMask = CFM_COLOR, crTextColor = ColorTranslator.ToWin32(ink), szFaceName = "" };
            SendMessageFmt(Handle, EM_SETCHARFORMAT, new IntPtr(SCF_ALL), ref cf);
            SendMessageFmt(Handle, EM_SETCHARFORMAT, new IntPtr(SCF_SELECTION), ref cf);
        }
    }

    /// <summary>
    /// 메모 본문 편집기. 저장은 마크다운이고 화면은 강조를 그린다: ~~취소선~~ 은 취소선으로 보이고 물결표는 숨는다.
    /// 마크다운 ↔ 화면 변환은 이 클래스만 안다. 위쪽(서식 명령 · 저장 · 기록)은 전부 "보이는 글 + 선택" 만으로 일한다.
    /// </summary>
    internal sealed class NoteEditor
    {
        private readonly NoteBox box;
        private string shown, markdown;
        internal bool Busy;

        internal NoteEditor(NoteBox box) { this.box = box; }

        internal NoteBox Box { get { return box; } }
        internal string Text { get { if (shown == null) shown = NoteContent.NormalizeLineEndings(box.Text); return shown; } }
        internal string Markdown { get { if (markdown == null) markdown = NoteRichText.ToMarkdown(Text, ReadSpans()); return markdown; } }
        internal int SelectionStart { get { return Math.Max(0, box.SelectionStart); } }
        internal int SelectionLength { get { return Math.Max(0, box.SelectionLength); } }
        internal NoteTextState State { get { return new NoteTextState(Text, SelectionStart, SelectionLength).Clamped(); } }

        internal void Invalidate() { shown = null; markdown = null; }

        internal void Load(string md, Color ink)
        {
            bool was = Busy; Busy = true;
            try
            {
                var rich = NoteRichText.FromMarkdown(md);
                box.SetRedraw(false);
                box.Text = rich.Text;
                foreach (var s in rich.Spans.Where(x => x.Length > 0))
                {
                    box.Select(s.Start, s.Length);
                    SetStyle(s.Style);
                }
                box.PaintInk(ink); box.Select(0, 0);
                box.ClearUndoStack();
            }
            finally { box.SetRedraw(true); Busy = was; Invalidate(); }
        }

        private void SetStyle(InlineStyle style)
        {
            box.SetEffects(NoteBox.CFE_BOLD, (style & InlineStyle.Bold) != 0);
            box.SetEffects(NoteBox.CFE_ITALIC, (style & InlineStyle.Italic) != 0);
            box.SetEffects(NoteBox.CFE_UNDERLINE, (style & InlineStyle.Underline) != 0);
            box.SetEffects(NoteBox.CFE_STRIKEOUT, (style & InlineStyle.Strikethrough) != 0);
        }

        internal void Select(int start, int length) { bool was = Busy; Busy = true; try { box.Select(start, length); } finally { Busy = was; } }

        internal void Apply(NoteTextState next)
        {
            bool was = Busy; Busy = true;
            try
            {
                var edit = TextDiff.Minimal(Text, next.Text);
                if (!edit.IsEmpty) box.ReplaceRange(edit.Start, edit.Length, edit.Insert);
                box.Select(next.SelectionStart, next.SelectionLength);
            }
            finally { Busy = was; Invalidate(); }
        }

        internal void ToggleInline(InlineStyle style)
        {
            int bit = style == InlineStyle.Bold ? NoteBox.CFE_BOLD : style == InlineStyle.Italic ? NoteBox.CFE_ITALIC : style == InlineStyle.Underline ? NoteBox.CFE_UNDERLINE : NoteBox.CFE_STRIKEOUT;
            int mask, effects; box.GetEffects(out mask, out effects);
            bool currentlyOn = (mask & bit) != 0 && (effects & bit) != 0;
            box.SetEffects(bit, !currentlyOn);
            Invalidate();
        }

        internal void SetInk(Color ink) { box.ForeColor = ink; box.PaintInk(ink); }

        private List<RichSpan> ReadSpans()
        {
            var spans = new List<RichSpan>(); int length = Text.Length; if (length == 0) return spans;
            bool was = Busy; Busy = true; int s0 = box.SelectionStart, l0 = box.SelectionLength;
            try { box.SetRedraw(false); Collect(0, length, spans); }
            finally { box.Select(s0, l0); box.SetRedraw(true); Busy = was; }
            return spans;
        }

        private void Collect(int start, int end, List<RichSpan> spans)
        {
            if (end <= start) return;
            box.Select(start, end - start);
            int mask, effects; box.GetEffects(out mask, out effects);
            if ((mask & NoteBox.FourStyles) == NoteBox.FourStyles || end - start == 1)
            {
                var style = InlineStyle.None;
                if ((mask & effects & NoteBox.CFE_BOLD) != 0) style |= InlineStyle.Bold;
                if ((mask & effects & NoteBox.CFE_ITALIC) != 0) style |= InlineStyle.Italic;
                if ((mask & effects & NoteBox.CFE_UNDERLINE) != 0) style |= InlineStyle.Underline;
                if ((mask & effects & NoteBox.CFE_STRIKEOUT) != 0) style |= InlineStyle.Strikethrough;
                if (style != InlineStyle.None) spans.Add(new RichSpan(start, end - start, style));
                return;
            }
            int mid = start + (end - start) / 2; Collect(start, mid, spans); Collect(mid, end, spans);
        }
    }
}
