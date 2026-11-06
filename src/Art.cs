using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace SuperHeroes
{
    internal sealed class CharInfo
    {
        public string Key, Name, Title;       // 키, 이름, 별명
        public Color Accent;                  // 말풍선·테마 등에 어울리는 색
        public Persona Persona;               // 성격(행동·몸짓·시그니처)
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
    /// 캐릭터의 크기와 기준 치수. 그림은 HeroRig*.cs 가 부위별로 그린다
    /// (머리카락·망토·사지·눈빛·무기가 따로 움직이는 벡터 리그 방식).
    /// </summary>
    internal sealed class CharacterArt
    {
        internal static readonly CharInfo[] Infos =
        {
            new CharInfo
            {
                Key = "superman",
                Name = "수퍼맨",
                Title = "희망의 강철의 사나이",
                Accent = Color.FromArgb(16, 92, 184),
                Persona = Persona.Superman,
                Rig = new HeroRig(Persona.Superman),
                HeightFactor = 1.05f,
                SpeedFactor = 1.05f,
                Traits = new[]
                {
                    "이마의 S자 앞머리 컬과 붉은 망토",
                    "가슴의 찬란한 희망의 S 심볼",
                    "히트 비전, 냉기 입김, 초음속 펀치, 셔츠 찢기"
                }
            },
            new CharInfo
            {
                Key = "batman",
                Name = "배트맨",
                Title = "어둠의 기사",
                Accent = Color.FromArgb(70, 75, 85),
                Persona = Persona.Batman,
                Rig = new HeroRig(Persona.Batman),
                HeightFactor = 1.02f,
                SpeedFactor = 0.98f,
                Traits = new[]
                {
                    "뾰족한 박쥐 귀 카울과 톱니 망토",
                    "황금빛 유틸리티 벨트와 건틀릿 블레이드",
                    "배트랭·그래플건·연막탄·가고일 + 중무장 슈트, 배트모빌·배트포드·배트윙 탑승"
                }
            },
            new CharInfo
            {
                Key = "spiderman",
                Name = "스파이더맨",
                Title = "친절한 이웃",
                Accent = Color.FromArgb(214, 38, 44),
                Persona = Persona.Spiderman,
                Rig = new HeroRig(Persona.Spiderman),
                HeightFactor = 0.92f,
                SpeedFactor = 1.15f,
                Traits = new[]
                {
                    "거미줄 패턴의 레드&블루 수트와 커다란 흰 눈",
                    "날렵하고 유연한 아크로바틱 체형",
                    "웹 스윙 (THWIP!), 스파이더 센스, 거꾸로 매달리기, 거미줄 네트"
                }
            },
            new CharInfo
            {
                Key = "thor",
                Name = "토르",
                Title = "아스가르드의 천둥의 신",
                Accent = Color.FromArgb(220, 175, 40),
                Persona = Persona.Thor,
                Rig = new HeroRig(Persona.Thor),
                HeightFactor = 1.08f,
                SpeedFactor = 0.96f,
                Traits = new[]
                {
                    "날개 달린 은색 투구, 풍성한 금발과 턱수염",
                    "6원형 디스크 흉갑과 진홍빛 망토",
                    "묠니르 풍차 회전, 천둥 벼락 소환, 지면 강타 충격파, 묠니르 투척"
                }
            },
            new CharInfo
            {
                Key = "wonderwoman",
                Name = "원더우먼",
                Title = "테미스키라의 아마존 전사",
                Accent = Color.FromArgb(196, 30, 58),
                Persona = Persona.WonderWoman,
                Rig = new HeroRig(Persona.WonderWoman),
                HeightFactor = 1.06f,
                SpeedFactor = 1.04f,
                Traits = new[]
                {
                    "붉은 별이 박힌 황금 티아라와 윤기 나는 긴 흑발",
                    "황금 독수리 흉갑, 별무늬 푸른 스커트, 은색 팔찌, 붉은 부츠",
                    "은팔찌 방어, 진실의 올가미, 검과 방패, 아마존의 비행"
                }
            },
        };

        internal static int Count { get { return Infos.Length; } }
        internal static float Scale = 1f;
        internal const float BaseHeight = 108f;

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
        public readonly float Zoom;
        public readonly float K;
        public readonly int W, H;
        public float Unit { get { return Scale * Zoom; } }
        public readonly int RotorH;
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
            Id = id;
            Info = Infos[id];
            Rig = Info.Rig;
            Zoom = zoom;
            H = (int)Math.Round(BaseHeight * Scale * Info.HeightFactor * zoom);
            K = H / Rig.RefH;
            W = (int)Math.Round(Rig.NominalW * K);
            RotorH = (int)Math.Round(H * .18f);
            Ceiling = new Dim { W = W, H = H };
        }

        internal static void Quality(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        }

        internal static Rectangle Bounds(Bitmap b, int threshold = 30)
        {
            int l = b.Width, t = b.Height, r = -1, bot = -1;
            var data = b.LockBits(new Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                var bytes = new byte[data.Stride * b.Height];
                System.Runtime.InteropServices.Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);
                for (int y = 0; y < b.Height; y++)
                    for (int x = 0; x < b.Width; x++)
                        if (bytes[y * data.Stride + x * 4 + 3] > threshold)
                        {
                            if (x < l) l = x; if (y < t) t = y;
                            if (x > r) r = x; if (y > bot) bot = y;
                        }
            }
            finally { b.UnlockBits(data); }
            return r < l ? new Rectangle(0, 0, b.Width, b.Height) : Rectangle.FromLTRB(l, t, r + 1, bot + 1);
        }

        internal static Icon MakeIcon(int id)
        {
            try
            {
                int n = Math.Max(16, (int)Math.Round(16 * Scale));
                using (Bitmap bmp = IconBitmap(id, n))
                {
                    IntPtr hicon = bmp.GetHicon();
                    Icon icon = (Icon)Icon.FromHandle(hicon).Clone();
                    Native.DestroyIcon(hicon);
                    return icon;
                }
            }
            catch { return SystemIcons.Application; }
        }

        internal static Bitmap IconBitmap(int id, int n)
        {
            CharInfo info = Infos[((id % Count) + Count) % Count];
            CharRig rig = info.Rig;
            var bmp = new Bitmap(n, n, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                Quality(g);
                g.Clear(Color.Transparent);
                float k = (n - 1.5f) / rig.HeadSize;
                g.TranslateTransform(n / 2f - (rig.HeadCx - rig.FootX) * k, n / 2f - (rig.HeadCy - rig.FootY) * k + n * .02f);
                rig.Draw(g, new Pose { Mouth = MouthStyle.Smile }, k, false);
            }
            return bmp;
        }
    }
}
