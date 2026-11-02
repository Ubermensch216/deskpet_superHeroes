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
        public string Key, Name, Title;       // 키, 이름, 별명
        public Color Accent;                  // 말풍선·꽃 장식 등에 어울리는 색
        public Persona Persona;               // 성격(행동·몸짓·시그니처가 캐릭터마다 다르다)
        public CharRig Rig;                   // 그림 도구
        public float HeightFactor = 1f;       // 서 있는 키(상대)
        public float SpeedFactor = 1f;        // 걷는 속도(상대)
        public string[] Traits;               // 특징 설명(도감용)
    }

    /// <summary>벽·천장 상태에서 쓰는 몸 크기 상자.</summary>
    internal sealed class Dim
    {
        public int W, H;
    }

    /// <summary>
    /// 캐릭터의 크기와 기준 치수. 그림은 DbRig*.cs 가 부위별로 그린다
    /// (이미지 한 장을 변형하는 방식이 아니라, 머리카락·팔·다리·표정·기 에너지가 따로 움직인다).
    /// </summary>
    internal sealed class CharacterArt
    {
        internal static readonly CharInfo[] Infos =
        {
            new CharInfo { Key = "goku", Name = "손오공", Title = "지구를 지키는 사이어인", Accent = Color.FromArgb(244, 124, 30), Persona = Persona.Goku, Rig = new DbRig(Persona.Goku), HeightFactor = 1f, SpeedFactor = 1.05f,
                Traits = new[] { "늘 밝게 웃고 머리카락이 살랑살랑", "가만히 있으면 머리 긁적·손 흔들기·주먹 불끈", "맛있는 걸 보면 정신을 못 차려요" } },
            new CharInfo { Key = "kid", Name = "어린 오공", Title = "꼬리 달린 장난꾸러기", Accent = Color.FromArgb(240, 100, 60), Persona = Persona.Kid, Rig = new DbRig(Persona.Kid), HeightFactor = .74f, SpeedFactor = 1.18f,
                Traits = new[] { "꼬리가 살랑살랑, 등에는 여의봉", "통통 뛰듯 총총 걷고 호기심이 많아요", "배고프면 금방 시무룩해져요" } },
            new CharInfo { Key = "roshi", Name = "무천도사", Title = "거북이 선인", Accent = Color.FromArgb(70, 170, 190), Persona = Persona.Roshi, Rig = new DbRig(Persona.Roshi), HeightFactor = .84f, SpeedFactor = .78f,
                Traits = new[] { "선글라스에 하얀 수염, 지팡이를 짚고 천천히", "수염을 쓸어내리고 등딱지를 메고 다녀요", "차 한 잔이면 만사 OK" } },
            new CharInfo { Key = "vegeta", Name = "베지터", Title = "사이어인의 왕자", Accent = Color.FromArgb(60, 96, 196), Persona = Persona.Vegeta, Rig = new DbRig(Persona.Vegeta), HeightFactor = .9f, SpeedFactor = .96f,
                Traits = new[] { "늘 찌푸린 얼굴, 가만히 서면 팔짱", "클릭하면 바로 발끈(💢)해요", "자존심이 강해서 우는 모습은 보기 힘들어요" } },
            new CharInfo { Key = "piccolo", Name = "피콜로", Title = "과묵한 나메크성인", Accent = Color.FromArgb(104, 168, 70), Persona = Persona.Piccolo, Rig = new DbRig(Persona.Piccolo), HeightFactor = 1.18f, SpeedFactor = .98f,
                Traits = new[] { "더듬이와 뾰족한 귀가 쫑긋, 망토가 펄럭", "차분히 팔짱을 끼고 있어요", "먹는 건 오직 맑은 물 한 잔" } },
        };
        internal static int Count { get { return Infos.Length; } }
        internal static float Scale = 1f;
        /// <summary>서 있을 때 키(머리카락 끝~발바닥, 화면 픽셀). 배율 1 기준.</summary>
        internal const float BaseHeight = 104f;

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
        public readonly float Zoom;       // 걷는 캐릭터 대비 확대 배율
        public readonly float K;          // 기준 그림 1픽셀 = 화면 K 픽셀
        public readonly int W, H;         // 서 있는 모습의 상자(걷기 기준) — 이동·충돌 기준
        public float Unit { get { return Scale * Zoom; } }
        public readonly int RotorH;       // 오라(기)가 머리 위로 올라오는 높이
        public readonly Dim Ceiling;
        public int CeilingTopInset { get { return 0; } }
        public int CeilingContactHeight { get { return Ceiling.H; } }

        internal static CharacterArt Zoomed(int id, float zoom)
        {
            id = ((id % Count) + Count) % Count;
            return new CharacterArt(id, zoom);
        }

        private CharacterArt(int id, float zoom = 1f)
        {
            Id = id; Info = Infos[id];
            Rig = Info.Rig;
            Zoom = zoom;
            H = (int)Math.Round(BaseHeight * Scale * Info.HeightFactor * zoom);
            K = H / Rig.RefH;
            W = (int)Math.Round(Rig.NominalW * K);
            RotorH = (int)Math.Round(H * .16f);
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
                g.TranslateTransform(n / 2f - (rig.HeadCx - rig.FootX) * k, n / 2f - (rig.HeadCy - rig.FootY) * k + n * .02f);
                rig.Draw(g, new Pose { Mouth = MouthStyle.Smile }, k, false);
            }
            return bmp;
        }
    }
}
