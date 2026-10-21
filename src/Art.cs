using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace DragonBall
{
    internal sealed class CharInfo
    {
        public string Key, Name;
        public Color Accent;                  // 말풍선·꽃 장식 등에 어울리는 색
        public Persona Persona;               // 성격(행동·몸짓이 캐릭터마다 다르다)
        public CharRig Rig;                   // 그림 도구
        public float HeightFactor = 1f;       // 서 있는 키(상대)
    }

    /// <summary>벽·천장 상태에서 쓰는 몸 크기 상자.</summary>
    internal sealed class Dim
    {
        public int W, H;
    }

    /// <summary>
    /// 드래곤볼의 크기와 기준 치수. 그림은 Rig.cs 가 부위별로 그린다
    /// (원본 감천이처럼 이미지 한 장을 변형하는 방식이 아니라, 팔·다리·꼬리·귀·눈·입이 따로 움직인다).
    /// </summary>
    internal sealed class CharacterArt
    {
        internal static readonly CharInfo[] Infos =
        {
            new CharInfo { Key = "neoburi", Name = "드래곤볼", Accent = Color.FromArgb(232, 150, 60), Persona = Persona.Raccoon, Rig = new RaccoonRig(), HeightFactor = 1f },
            new CharInfo { Key = "bonobono", Name = "보노보노", Accent = Color.FromArgb(82, 178, 243), Persona = Persona.Bono, Rig = new BonoRig(), HeightFactor = 1.04f },
            new CharInfo { Key = "squirrel", Name = "다람쥐", Accent = Color.FromArgb(238, 108, 132), Persona = Persona.Squirrel, Rig = new SquirrelRig(), HeightFactor = .8f },
        };
        internal static int Count { get { return Infos.Length; } }
        internal static float Scale = 1f;
        /// <summary>서 있을 때 키(귀끝~발바닥, 화면 픽셀). 배율 1 기준.</summary>
        internal const float BaseHeight = 100f;

        private static readonly Dictionary<int, CharacterArt> cache = new Dictionary<int, CharacterArt>();
        internal static CharacterArt Get(int id)
        {
            id = ((id % Count) + Count) % Count;
            CharacterArt a;
            if (!cache.TryGetValue(id, out a)) cache[id] = a = new CharacterArt(id);
            return a;
        }
        internal static void Reset(float scale) { Scale = scale; cache.Clear(); }

        public readonly int Id;
        public readonly CharInfo Info;
        public readonly CharRig Rig;
        public Persona Persona { get { return Info.Persona; } }
        public readonly float K;          // 기준 그림 1픽셀 = 화면 K 픽셀
        public readonly int W, H;         // 서 있는 모습의 상자(걷기 기준) — 이동·충돌 기준
        public readonly int RotorH;       // 프로펠러가 머리 위로 올라오는 높이
        public readonly Dim Ceiling;
        public int CeilingTopInset { get { return 0; } }
        public int CeilingContactHeight { get { return Ceiling.H; } }

        private CharacterArt(int id)
        {
            Id = id; Info = Infos[id];
            Rig = Info.Rig;
            H = (int)Math.Round(BaseHeight * Scale * Info.HeightFactor);
            K = H / Rig.RefH;
            W = (int)Math.Round(Rig.NominalW * K);
            RotorH = (int)Math.Round(H * .30f);
            Ceiling = new Dim { W = W, H = H };
        }

        internal static void Quality(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias; g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.CompositingQuality = CompositingQuality.HighQuality; g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        }

        /// <summary>투명하지 않은 부분의 경계.</summary>
        internal static Rectangle Bounds(Bitmap b, int threshold = 30)
        {
            int l = b.Width, t = b.Height, r = -1, bot = -1;
            var data = b.LockBits(new Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                var bytes = new byte[data.Stride * b.Height]; System.Runtime.InteropServices.Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);
                for (int y = 0; y < b.Height; y++)
                    for (int x = 0; x < b.Width; x++)
                        if (bytes[y * data.Stride + x * 4 + 3] > threshold) { if (x < l) l = x; if (y < t) t = y; if (x > r) r = x; if (y > bot) bot = y; }
            }
            finally { b.UnlockBits(data); }
            return r < l ? new Rectangle(0, 0, b.Width, b.Height) : Rectangle.FromLTRB(l, t, r + 1, bot + 1);
        }

        /// <summary>트레이·창 아이콘용 작은 그림.</summary>
        internal static Icon MakeIcon(int id)
        {
            try
            {
                int n = Math.Max(16, (int)Math.Round(16 * Scale));
                using (Bitmap bmp = IconBitmap(id, n))
                {
                    IntPtr hicon = bmp.GetHicon();
                    Icon icon = (Icon)Icon.FromHandle(hicon).Clone(); Native.DestroyIcon(hicon); return icon;
                }
            }
            catch { return SystemIcons.Application; }
        }

        /// <summary>얼굴이 잘 보이도록 머리를 확대한 아이콘 그림.</summary>
        internal static Bitmap IconBitmap(int id, int n)
        {
            CharInfo info = Infos[((id % Count) + Count) % Count];
            CharRig rig = info.Rig;
            var bmp = new Bitmap(n, n, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                Quality(g); g.Clear(Color.Transparent);
                float k = (n - 1.5f) / rig.HeadSize;
                g.TranslateTransform(n / 2f - (rig.HeadCx - rig.FootX) * k, n / 2f - (rig.HeadCy - rig.FootY) * k);
                rig.Draw(g, new Pose { Cross = .55f, Gy = .35f }, k, false);
            }
            return bmp;
        }
    }
}
