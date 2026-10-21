using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;

namespace DragonBall
{
    [Flags]
    internal enum Decor
    {
        None = 0, Stars = 8, Flower = 16, Notes = 64, Speed = 256, Sleepy = 512, Dots = 2048, Moon = 4096, SigStars = 8192
    }

    internal sealed partial class Pet
    {
        /// <summary>한 순간의 모습: 어떤 자세로 그리고, 어떻게 돌리고, 어떤 장식·말풍선을 얹을지.</summary>
        internal struct Look
        {
            public CharacterArt Art; public Pose Pose;
            public bool FaceRight;               // 그림은 왼쪽을 보는 모습이 기준 — 오른쪽이면 좌우 반전
            public bool Upside;                  // 천장에 거꾸로 매달림
            public float Sx, Sy, Angle, Px, Py, HopY;
            public float Alpha, Moon;            // 몸의 투명도(순간이동)·보름달 밝기
            public string Bubble;
            public int ZPhase; public float ZTime;
            public Decor Decor; public float DecorPhase;
            public string AffinityLabel; public bool AffinityOnLeft;

            public string Key()
            {
                return string.Format("{0}|{1}|{2}|{3:0.00}|{4:0.00}|{5:0.0}|{6:0}|{7:0}|{8}|{9}|{10}|{11}|{12}|{13}|{14:0.00}|{15:0.00}",
                    Pose.Hash(), FaceRight ? 1 : 0, Upside ? 1 : 0, Sx, Sy, Angle, Px, Py, Bubble ?? "", ZPhase, (int)Decor,
                    Decor == Decor.None ? 0 : (int)(DecorPhase * 24), AffinityLabel, AffinityOnLeft, Alpha, Moon);
            }
        }

        private double ClimbPhase { get { return gait * Math.PI * 2; } }

        public Look GetLook(double now)
        {
            var L = new Look { Art = Art, Pose = new Pose(), Sx = 1, Sy = 1, Alpha = 1, ZPhase = -1, FaceRight = Dir > 0 };
            Pose P = L.Pose;
            lookJitterX = 0;
            double t = animT;
            float h = Art.H;
            switch (State)
            {
                case PetState.Walk: case PetState.Gather:
                    IdleBase(P, t, false); ApplyWalk(P, gait, walkAmp, 22, 16, 12, 3f);
                    break;
                case PetState.Pause: case PetState.WaitHome:
                    if (State == PetState.WaitHome) WaitPose(P); else IdleBase(P, t, true);
                    ApplyWalk(P, gait, walkAmp, 22, 16, 12, 3f);
                    break;
                case PetState.Climb:
                    L.FaceRight = climbDir > 0; IdleBase(P, t, false); ApplyWalk(P, gait, walkAmp, 22, 16, 12, 3f);
                    break;
                case PetState.Falling: case PetState.PowerRise:
                    // 기를 모아 둥실 떠서 천천히 흔들리며 내려온다(어린 오공은 근두운을 타고).
                    L.FaceRight = false;
                    FallingPose(P);
                    L.Angle = (float)(3 * Math.Sin(StateT * 2.4)); L.Px = 0; L.Py = -h * .5f;
                    break;
                case PetState.Squish:
                    SquishPose(P, ref L);
                    break;
                case PetState.Drag:
                    DragPose(P);
                    L.Angle = (float)angle; L.Px = (float)DragPx; L.Py = (float)DragPy; L.FaceRight = false;
                    break;
                case PetState.Thrown:
                    ThrownPose(P);
                    L.FaceRight = throwVx > 0; L.Px = 0; L.Py = -h * .5f;
                    L.Angle = (float)(Math.Max(-32, Math.Min(32, throwVx / (900 * S) * 30)) + Math.Sin(StateT * 13) * 6);
                    break;
                case PetState.Sleep:
                    SleepPose(P, animT); L.ZPhase = (int)(animT * 20) % 100000; L.ZTime = (float)animT; L.Decor |= Decor.Sleepy;
                    break;
                case PetState.Waking:
                    if (StateT < .8) { double q = Math.Sin(Math.PI * StateT / .8); StretchPose(P, q); }
                    else IdleBase(P, t, false);
                    break;
                case PetState.Cheer:
                    CheerPose(P, now);
                    if (now < cheerHopUntil) L.HopY = (float)(-Math.Sin(Math.PI * (StateT % .42 / .42)) * 11 * S);
                    L.FaceRight = false;
                    break;
                case PetState.Resting:
                    RestLook(ref L, now);
                    break;
                case PetState.Eating:
                    EatPose(P); L.FaceRight = false;
                    break;
                case PetState.IdleCry:
                {
                    bool cry = FriendData.Current.Hungry || previewIdleCry;
                    L.FaceRight = false;
                    if (cry) HungryCryPose(P, StateT); else SitIdlePose(P, t);
                    break;
                }
                case PetState.TouchCry:
                    // 달달 떨며 운다: 처음에 움찔 후 흐느낌(기준 그림 2의 겁먹은 모습).
                    TouchCryPose(P); L.FaceRight = false;
                    lookJitterX = Math.Sin(StateT * 52) * .6 * S; L.HopY = (float)(Math.Sin(StateT * 40) * .4 * S);
                    break;
                case PetState.Hop:
                {
                    double q = StateT / extraDuration;
                    HopPose(P);
                    if (q >= .16 && q < .84) L.HopY = (float)(-Math.Sin((q - .16) / .68 * Math.PI) * 22 * S);
                    break;
                }
                case PetState.Sliding:
                    DashPose(P); L.HopY = (float)(Math.Sin(animT * 20) * .5 * S);
                    L.Decor |= Decor.Speed; L.DecorPhase = (float)animT;
                    break;
                case PetState.Wall: case PetState.WindowClimb:
                {
                    // 발을 벽에 대고 몸을 옆으로 눕혀 걷는다(머리는 벽 반대쪽). 오를 땐 위, 내릴 땐 아래를 보며 손발을 번갈아 뻗는다.
                    int wallDir = State == PetState.Wall ? wallSide : -approachSide;
                    bool down = State == PetState.Wall && wallDescending;
                    ClimbBody(P, down);
                    L.FaceRight = down ? wallDir < 0 : wallDir > 0;
                    L.Angle = (float)(-wallDir * (down ? WallDescendDeg : WallAscendDeg) + 3 * Math.Sin(ClimbPhase));
                    break;
                }
                case PetState.Ceiling:
                    // 천장에 발을 붙이고 거꾸로 걷는다: 뒤뚱의 축은 천장에 닿은 발끝.
                    ClimbBody(P, false);
                    L.Upside = true; L.FaceRight = Dir < 0;
                    L.Px = 0; L.Py = -Art.Ceiling.H; L.Angle = (float)(3 * Math.Sin(ClimbPhase));
                    break;
                case PetState.Dizzy:
                    DizzyPose(P); L.FaceRight = false;
                    L.Decor |= Decor.Stars; L.DecorPhase = (float)StateT;
                    break;
                case PetState.SecretDance:
                {
                    double phase = now - danceStarted;
                    L.FaceRight = (int)(phase / .6) % 2 == 0;
                    L.HopY = (float)(-Math.Abs(Math.Sin(phase * Math.PI * 2.5)) * 12 * S);
                    DancePose(P, phase);
                    L.Decor |= Decor.Notes; L.DecorPhase = (float)phase;
                    break;
                }
            }
            L.Bubble = PersistentBubble(hourlyBubble, Bubble);
            CompanionLook(ref L, now);
            ApplyPersona(ref L, now);
            return L;
        }

        /// <summary>앉아 쉬는 네 가지 자세: 앉기 · 잠자기 · 멍때리기 · 기지개.</summary>
        private void RestLook(ref Look L, double now)
        {
            Pose P = L.Pose; double t = StateT;
            switch (restPose)
            {
                case RestPose.Sleep:
                    SleepPose(P, t); L.ZPhase = (int)(t * 20) % 100000; L.ZTime = (float)t; L.Decor |= Decor.Sleepy;
                    break;
                case RestPose.Daydream:
                    // 멍때리기: 위를 쳐다보며 몸이 아주 느리게 흔들리고 생각 점 세 개가 차례로 떠오른다.
                    DaydreamPose(P, t); L.Decor |= Decor.Dots; L.DecorPhase = (float)t;
                    break;
                case RestPose.Stretch:
                {
                    // 기지개: 팔을 쭉 올려 하품하며 몸을 젖혔다가 한 번 털고 내려온다.
                    double u = extraDuration <= 0 ? 1 : t / extraDuration, q;
                    if (u < .05) q = 0;
                    else if (u < .35) q = Ease((u - .05) / .3);
                    else if (u < .65) q = 1;
                    else if (u < .9) q = 1 - Ease((u - .65) / .25);
                    else q = 0;
                    StretchPose(P, q);
                    if (u >= .9) { double sh = Math.Sin((u - .9) * 150) * (1 - (u - .9) / .1); P.Lean += F(sh * 3); P.HeadTilt += F(sh * 5); P.EarL += F(sh * 10); P.EarR -= F(sh * 10); }
                    break;
                }
                case RestPose.Sig:
                    SigPose(P, ref L, t);
                    break;
                default:
                    SitIdlePose(P, animT);
                    break;
            }
        }

        private void SadOverlay(Pose P, double t)
        {
            double sob = Math.Sin(t * 9);
            P.Brow = -.85f; P.Mouth = MouthStyle.Frown; P.Tears = F(t);
            P.EarL = F(-16 + sob * 2); P.EarR = F(16 - sob * 2); P.Tail = Math.Max(P.Tail, 22); P.TailWaveAmp *= .4f;
            P.PupilR = .9f; P.EyeScale = 1.04f; P.HeadY += F(3 + Math.Abs(sob));
            P.Gx = 0; P.Gy = Math.Max(P.Gy, .4f);
        }

        private void CompanionLook(ref Look look, double now)
        {
            if (PlayOptions.Current.ShowAffinity)
            {
                look.AffinityLabel = FriendData.Current.Affinity.ToString("N0");
                look.AffinityOnLeft = X + Art.W * .5 + (22 + look.AffinityLabel.Length * 8) * S > Area.Right;
            }
            bool hungry = FriendData.Current.Hungry;
            if (hungry && IsCompanionFloorPose)
            {
                if (State == PetState.Squish) look.Pose.Tears = F(animT);
                else if (State == PetState.Walk || (State == PetState.Pause && walkAmp > .05)) SadOverlay(look.Pose, animT);
                else if (State == PetState.Pause) { SadOverlay(look.Pose, animT); }
                else
                {
                    look.Pose = new Pose(); HungryCryPose(look.Pose, animT); look.FaceRight = false;
                    look.Angle = 0; look.Sx = look.Sy = 1; look.HopY = 0;
                }
                lookJitterX = Math.Sin(animT * 21) * .45 * S;
            }
            if (hungry && (IsCompanionFloorPose || State == PetState.TouchCry) && string.IsNullOrEmpty(look.Bubble) && now >= flowerUntil)
                look.Bubble = "배고파! 간식줘 ㅠㅠ";
            if (now < flowerUntil && IsCompanionFloorPose && !previewIdleCry)
            {
                look.Pose = new Pose(); FlowerPose(look.Pose, now);
                look.FaceRight = false; look.Decor |= Decor.Flower; look.DecorPhase = (float)(now - flowerStarted);
                look.Angle = 0; look.Sx = look.Sy = 1; look.HopY = 0; lookJitterX = 0;
            }
        }

        internal static string PersistentBubble(string hourly, string ordinary)
        {
            if (string.IsNullOrEmpty(hourly)) return ordinary;
            if (string.IsNullOrEmpty(ordinary) || hourly == ordinary || hourly.IndexOf(ordinary, StringComparison.Ordinal) >= 0) return hourly;
            return hourly + "\n" + ordinary;
        }

        // ───────────────────────── 그리기 ─────────────────────────
        private static Font bubbleFont, affinityFont;
        private static float fontScale;
        private static void EnsureFonts()
        {
            float s = CharacterArt.Scale;
            if (bubbleFont != null && fontScale == s) return;
            fontScale = s;
            try { bubbleFont = new Font("Malgun Gothic", 11f * s, FontStyle.Bold, GraphicsUnit.Pixel); }
            catch { bubbleFont = new Font(FontFamily.GenericSansSerif, 11f * s, FontStyle.Bold, GraphicsUnit.Pixel); }
            try { affinityFont = new Font("Segoe UI", 10f * s, FontStyle.Bold, GraphicsUnit.Pixel); }
            catch { affinityFont = new Font(FontFamily.GenericSansSerif, 10f * s, FontStyle.Bold, GraphicsUnit.Pixel); }
        }
        private static FontFamily zFamily;
        private static FontFamily ZFamily
        {
            get
            {
                if (zFamily == null) { try { zFamily = new FontFamily("Segoe UI"); } catch { zFamily = FontFamily.GenericSansSerif; } }
                return zFamily;
            }
        }

        private static GraphicsPath RoundRect(RectangleF r, float rad)
        {
            var p = new GraphicsPath(); float d = Math.Min(rad * 2, Math.Min(r.Width, r.Height));
            p.AddArc(r.Left, r.Top, d, d, 180, 90); p.AddArc(r.Right - d, r.Top, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); p.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure(); return p;
        }

        /// <summary>장식이 몸 상자 바깥으로 얼마나 나가는지 (스프라이트 좌표, 바닥 가운데 원점).</summary>
        private static RectangleF DecorExtent(Look L, float w, float h, float s, int rotorH)
        {
            float l = 0, t = 0, r = 0, b = 0;
            if ((L.Decor & Decor.SigStars) != 0) { l = Math.Min(l, -w * .66f); r = Math.Max(r, w * .66f); t = Math.Min(t, -h * 1.12f); }
            if ((L.Decor & Decor.Dots) != 0) { r = Math.Max(r, w * .6f); t = Math.Min(t, -h - 14 * s); }
            if ((L.Decor & Decor.Stars) != 0) { l = Math.Min(l, -w * .45f); r = Math.Max(r, w * .45f); t = Math.Min(t, -h - 14 * s); }
            if ((L.Decor & Decor.Flower) != 0) { l = Math.Min(l, -w * .3f); r = Math.Max(r, w * .3f); t = Math.Min(t, -h * .6f); }
            if ((L.Decor & Decor.Notes) != 0) { l = Math.Min(l, -w * .47f - 20 * s); r = Math.Max(r, w * .47f + 24 * s); t = Math.Min(t, -h * 1.0f - 18 * s); }
            if ((L.Decor & Decor.Speed) != 0) { l = Math.Min(l, -w * .5f - 24 * s); r = Math.Max(r, w * .5f + 24 * s); }
            return new RectangleF(l, t, r - l, b - t);
        }

        public static Bitmap Render(Look L, out PointF origin)
        {
            EnsureFonts();
            float scale = CharacterArt.Scale;
            CharacterArt art = L.Art;
            float w = art.W, h = art.H;
            int rotorH = art.RotorH;
            var matrix = new Matrix();
            if (L.Angle != 0) { matrix.Translate(L.Px, L.Py); matrix.Rotate(L.Angle); matrix.Translate(-L.Px, -L.Py); }
            matrix.Scale(L.Sx, L.Sy);
            // 팔을 벌리거나 꼬리가 흔들려도 잘리지 않도록 몸 상자보다 넉넉하게 잡는다.
            float padX = w * .42f, padTop = 3 * scale, padBottom = 3 * scale;
            Pose ps = L.Pose; float K = art.K;
            if (ps.Nimbus > .01f) padBottom += 34 * K;                                              // 근두운이 발 밑으로 내려옴
            if (ps.Aura > .01f || ps.Spark > .01f || ps.Flash > .01f) padTop += 70 * K;              // 오라·번쩍임이 머리 위로
            bool lying = Math.Abs(L.Pose.Lie) > .01f;
            // 누우면 몸이 옆으로 길어진다
            float bx = lying ? Math.Max(w / 2 + padX, h * .62f) : w / 2 + padX, by = h + padTop;
            var corners = new[] { new PointF(-bx, -by), new PointF(bx, -by), new PointF(bx, padBottom), new PointF(-bx, padBottom) };
            matrix.TransformPoints(corners);
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            foreach (PointF p in corners) { minX = Math.Min(minX, p.X); minY = Math.Min(minY, p.Y); maxX = Math.Max(maxX, p.X); maxY = Math.Max(maxY, p.Y); }
            // 말풍선·Z 는 몸의 맨 위(귀 끝)를 기준으로 얹는다
            float topY0 = lying ? -by : -h;
            var tops = new[] { new PointF(0, topY0), new PointF(-w / 2, topY0), new PointF(w / 2, topY0) };
            matrix.TransformPoints(tops);
            float spriteTop = Math.Min(tops[0].Y, Math.Min(tops[1].Y, tops[2].Y));

            RectangleF ext = DecorExtent(L, w, h, scale, rotorH);
            if (!ext.IsEmpty)
            {
                var ep = new[] { new PointF(ext.Left, ext.Top), new PointF(ext.Right, ext.Top) };
                matrix.TransformPoints(ep);
                foreach (PointF p in ep) { minX = Math.Min(minX, p.X); maxX = Math.Max(maxX, p.X); minY = Math.Min(minY, p.Y); }
                spriteTop = Math.Min(spriteTop, minY);
            }
            // 광선·기탄·늘어난 팔·여의봉은 몸 밖으로 멀리 뻗는다(앞쪽 = 바라보는 쪽)
            float fwd = 0, up = 0;
            if (ps.BeamLen > 1) fwd = Math.Max(fwd, (ps.BeamLen + 280) * K);
            if (ps.Shot >= 0) fwd = Math.Max(fwd, (1000 + 260 + 200) * K);
            if (ps.Staff >= 1.5f) { fwd = Math.Max(fwd, (250 * ps.StaffLen + 140) * K); up = Math.Max(up, (250 * ps.StaffLen + 160) * K); }
            if (ps.ArmLenL > 1.15f || ps.ArmLenR > 1.15f) { float m = Math.Max(ps.ArmLenL, ps.ArmLenR); fwd = Math.Max(fwd, (m * 130 + 150) * K); up = Math.Max(up, (m * 130 + 120) * K); }
            if (fwd > 0 || up > 0)
            {
                var fp = new[] { new PointF(L.FaceRight ? fwd : -fwd, -h * .55f), new PointF(0, -up - h * .3f) };
                matrix.TransformPoints(fp);
                minX = Math.Min(minX, fp[0].X); maxX = Math.Max(maxX, fp[0].X);
                if (up > 0) { minY = Math.Min(minY, fp[1].Y); spriteTop = Math.Min(spriteTop, fp[1].Y + h * .3f); }
            }
            float moonR = h * .24f; PointF moonC = PointF.Empty;
            if ((L.Decor & Decor.Moon) != 0)
            {
                moonC = new PointF(w * .5f * L.Sx + moonR * 1.35f, spriteTop + moonR * .3f);
                maxX = Math.Max(maxX, moonC.X + moonR * 1.7f); minY = Math.Min(minY, moonC.Y - moonR * 1.7f);
            }

            SizeF textSize = SizeF.Empty; RectangleF bubble = RectangleF.Empty;
            float tail = 7 * scale;
            if (!string.IsNullOrEmpty(L.Bubble))
            {
                using (var tmp = new Bitmap(1, 1))
                using (var g = Graphics.FromImage(tmp))
                {
                    g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                    textSize = g.MeasureString(L.Bubble, bubbleFont, (int)(170 * scale), StringFormat.GenericTypographic);
                }
                float pad = 7 * scale;
                float bw = (float)Math.Ceiling(textSize.Width + pad * 2 + 2 * scale), bh = (float)Math.Ceiling(textSize.Height + pad * 1.4f + 2 * scale);
                bubble = new RectangleF(-bw / 2, spriteTop - 3 * scale - tail - bh, bw, bh);
                minX = Math.Min(minX, bubble.Left); maxX = Math.Max(maxX, bubble.Right); minY = Math.Min(minY, bubble.Top);
            }
            if (L.ZPhase >= 0) { minY = Math.Min(minY, spriteTop - 44 * scale); maxX = Math.Max(maxX, w / 2 + 22 * scale); minX = Math.Min(minX, -w / 2 - 22 * scale); }
            RectangleF badge = AffinityBadgeBounds(L, w, h, scale);
            if (!badge.IsEmpty) { minX = Math.Min(minX, badge.Left); maxX = Math.Max(maxX, badge.Right); minY = Math.Min(minY, badge.Top); maxY = Math.Max(maxY, badge.Bottom); }

            const int margin = 3;
            int bwid = (int)Math.Ceiling(maxX - minX) + margin * 2, bhei = (int)Math.Ceiling(maxY - minY) + margin * 2;
            origin = new PointF(-minX + margin, -minY + margin);
            var result = new Bitmap(Math.Max(1, bwid), Math.Max(1, bhei), PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(result))
            {
                g.Clear(Color.Transparent);
                CharacterArt.Quality(g);
                var full = new Matrix(); full.Translate(origin.X, origin.Y); full.Multiply(matrix);
                g.Transform = full;
                if (L.Alpha < .995f)
                {
                    // 순간이동: 몸만 투명하게(장식·말풍선은 그대로)
                    var gtr = g.Transform;
                    using (var layer = new Bitmap(result.Width, result.Height, PixelFormat.Format32bppArgb))
                    {
                        using (var lg = Graphics.FromImage(layer))
                        {
                            lg.Clear(Color.Transparent); CharacterArt.Quality(lg); lg.Transform = gtr;
                            if (L.Upside) { lg.TranslateTransform(0, -h / 2); lg.RotateTransform(180); lg.TranslateTransform(0, h / 2); }
                            art.Rig.Draw(lg, L.Pose, art.K, L.FaceRight);
                        }
                        if (L.Alpha > .01f)
                            using (var ia = new ImageAttributes())
                            {
                                ia.SetColorMatrix(new ColorMatrix { Matrix33 = Math.Max(0, Math.Min(1, L.Alpha)) });
                                g.ResetTransform();
                                g.DrawImage(layer, new Rectangle(0, 0, layer.Width, layer.Height), 0, 0, layer.Width, layer.Height, GraphicsUnit.Pixel, ia);
                                g.Transform = gtr;
                            }
                    }
                }
                else
                {
                    if (L.Upside) { g.TranslateTransform(0, -h / 2); g.RotateTransform(180); g.TranslateTransform(0, h / 2); }
                    art.Rig.Draw(g, L.Pose, art.K, L.FaceRight);
                }

                // 몸에 붙어 따라 움직이는 장식
                g.Transform = full;
                DrawDecor(g, L, art, w, h, scale, rotorH);
                g.ResetTransform(); g.TranslateTransform(origin.X, origin.Y);
                if ((L.Decor & Decor.Moon) != 0) DrawMoon(g, moonC, moonR, L.Moon);
                DrawAffinityBadge(g, L, badge, scale);
                if (L.ZPhase >= 0) DrawZ(g, L, w, h, scale);
                if (!bubble.IsEmpty) DrawBubble(g, L, bubble, textSize, tail, scale);
            }
            return result;
        }

        private static void DrawBubble(Graphics g, Look L, RectangleF r, SizeF textSize, float tail, float scale)
        {
            using (GraphicsPath path = RoundRect(r, 8 * scale))
            {
                var tri = new[] { new PointF(-5 * scale, r.Bottom - 1), new PointF(1 * scale, r.Bottom + tail), new PointF(6 * scale, r.Bottom - 1) };
                using (var pen = new Pen(Color.FromArgb(255, 30, 30, 30), 2 * scale) { LineJoin = LineJoin.Round })
                using (var fill = new SolidBrush(Color.White))
                {
                    g.FillPath(fill, path); g.DrawPath(pen, path);
                    g.FillPolygon(fill, tri); g.DrawLine(pen, tri[0], tri[1]); g.DrawLine(pen, tri[1], tri[2]);
                    using (var cover = new Pen(gold ? Color.FromArgb(255, 255, 244, 196) : Color.White, 2.2f * scale))
                        g.DrawLine(cover, tri[0].X + 2.2f * scale, r.Bottom - .5f * scale, tri[2].X - 2.2f * scale, r.Bottom - .5f * scale);
                }
            }
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            using (var text = new SolidBrush(L.Bubble != null && L.Bubble.StartsWith("★") ? Color.FromArgb(255, 96, 52, 8) : Color.FromArgb(255, 40, 40, 40)))
            using (var fmt = new StringFormat(StringFormat.GenericTypographic) { Alignment = StringAlignment.Center })
            {
                var box = new RectangleF(r.Left + (r.Width - textSize.Width) / 2, r.Top + (r.Height - textSize.Height) / 2, textSize.Width + 2, textSize.Height + 2);
                g.DrawString(L.Bubble, bubbleFont, text, box, fmt);
            }
        }

        // 잠: Z 세 개가 시차를 두고(0 / 0.9 / 1.8초) 떠올라 흩어진다.
        private static void DrawZ(Graphics g, Look L, float w, float h, float s)
        {
            float dir = L.FaceRight ? 1 : -1, baseX = dir * w * .30f, baseY = -h * .76f;
            if (Math.Abs(L.Pose.Lie) > .5f) { baseX = dir * -h * .42f; baseY = -w * .55f; }
            float[] size = { 8f, 11f, 14f }; float[] phase = { 0f, .9f, 1.8f };
            for (int i = 0; i < 3; i++)
            {
                float t = L.ZTime;
                if (t < phase[i]) continue;
                float cycle = (((t - phase[i]) % 2.7f) + 2.7f) % 2.7f, k = cycle / 2.7f;
                int alpha = (int)(255 * Math.Max(0f, Math.Min(1f, Math.Min(cycle * 3f, 1 - k) * (k < .85f ? 1f : (1 - k) / .15f))));
                if (alpha <= 0) continue;
                float sz = size[i] * s, x = baseX + dir * (3 + cycle * 4.5f) * s, y = baseY - cycle * 10.5f * s;
                using (var path = new GraphicsPath())
                {
                    path.AddString(i == 0 ? "z" : "Z", ZFamily, 1, sz, new PointF(x - sz * .3f, y - sz), StringFormat.GenericTypographic);
                    using (var pen = new Pen(Color.FromArgb(alpha, 255, 255, 255), 2.4f * s) { LineJoin = LineJoin.Round })
                    using (var brush = new SolidBrush(Color.FromArgb(alpha, 70, 100, 170))) { g.DrawPath(pen, path); g.FillPath(brush, path); }
                }
            }
        }

        // 멍때리기: 생각 점 세 개가 차례로 (작은 것부터) 떠올랐다 사라진다.
        private static void DrawDots(Graphics g, float w, float h, float s, float t)
        {
            float[] radius = { 1.7f, 2.4f, 3.4f }, phase = { 0f, .75f, 1.5f };
            PointF[] at = { new PointF(w * .26f, -h * .86f), new PointF(w * .36f, -h * .95f), new PointF(w * .5f, -h * 1.06f) };
            for (int i = 0; i < 3; i++)
            {
                if (t < phase[i]) continue;
                float cycle = (((t - phase[i]) % 3.6f) + 3.6f) % 3.6f;
                int alpha = (int)(255 * Math.Max(0, Math.Min(1, Math.Sin(cycle / 3.6f * Math.PI))));
                if (alpha <= 0) continue;
                float r = radius[i] * s;
                using (var fill = new SolidBrush(Color.FromArgb(alpha, 255, 255, 255)))
                using (var edge = new Pen(Color.FromArgb(alpha, 110, 104, 120), 1.3f * s)) { g.FillEllipse(fill, at[i].X - r, at[i].Y - r, r * 2, r * 2); g.DrawEllipse(edge, at[i].X - r, at[i].Y - r, r * 2, r * 2); }
            }
        }

        private static RectangleF AffinityBadgeBounds(Look L, float w, float h, float s)
        {
            if (string.IsNullOrEmpty(L.AffinityLabel)) return RectangleF.Empty;
            float bw;
            using (var img = new Bitmap(1, 1)) using (var g = Graphics.FromImage(img))
                bw = Math.Max(38 * s, g.MeasureString(L.AffinityLabel, affinityFont, PointF.Empty, StringFormat.GenericTypographic).Width + 22 * s);
            return new RectangleF(L.AffinityOnLeft ? -w * .5f - 3 * s - bw : w * .5f + 3 * s, -h * .57f - 9 * s, (float)Math.Ceiling(bw), 20 * s);
        }

        private static void DrawAffinityBadge(Graphics g, Look L, RectangleF b, float s)
        {
            if (b.IsEmpty) return;
            using (var path = RoundRect(b, 8 * s))
            using (var fill = new SolidBrush(Color.FromArgb(245, 255, 248, 246)))
            using (var border = new Pen(Color.FromArgb(225, 237, 196, 207), s)) { g.FillPath(fill, path); g.DrawPath(border, path); }
            using (var outline = new Pen(Color.FromArgb(218, 105, 139), s)) DrawHeart(g, b.Left + 8 * s, b.Top + 10 * s, 4 * s, outline);
            using (var text = new SolidBrush(Color.FromArgb(113, 73, 91)))
            using (var fmt = new StringFormat(StringFormat.GenericTypographic) { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Center })
            {
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                g.DrawString(L.AffinityLabel, affinityFont, text, new RectangleF(b.Left + 16 * s, b.Top, b.Width - 17 * s, b.Height), fmt);
            }
        }

        private static void DrawHeart(Graphics g, float x, float y, float size, Pen outline)
        {
            using (var path = new GraphicsPath()) using (var fill = new SolidBrush(Color.FromArgb(235, 126, 154)))
            {
                path.AddBezier(x, y + size * .7f, x - size * 1.25f, y - size * .1f, x - size * .5f, y - size, x, y - size * .32f);
                path.AddBezier(x, y - size * .32f, x + size * .5f, y - size, x + size * 1.25f, y - size * .1f, x, y + size * .7f);
                g.FillPath(fill, path); g.DrawPath(outline, path);
            }
        }

        private static void DrawDecor(Graphics g, Look L, CharacterArt art, float w, float h, float s, int rotorH)
        {
            if (L.Decor == Decor.None) return;
            float t = L.DecorPhase;
            using (var ink = new Pen(Color.FromArgb(43, 36, 42), 2.2f * s) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
            {
                if ((L.Decor & Decor.Speed) != 0)
                {
                    float side = L.FaceRight ? -1 : 1;   // 달리는 방향의 반대쪽(뒤)에 그린다
                    for (int k = 0; k < 4; k++)   // 바닥에서 피어오르는 먼지
                    {
                        float p = (float)((t * 3.2 + k * .25) % 1), px = side * (w * .42f + (2 + p * 14 + k * 2) * s), py = -1.5f * s - p * 5 * s;
                        using (var dust = new SolidBrush(Color.FromArgb((int)(150 * (1 - p)), 232, 224, 212)))
                            g.FillEllipse(dust, px - (1.8f + p * 2.4f) * s, py - (1.5f + p * 2f) * s, (3.6f + p * 4.8f) * s, (3f + p * 4f) * s);
                    }
                    using (var line = new Pen(Color.FromArgb(190, 120, 120, 135), 1.8f * s) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                        for (int i = 0; i < 3; i++)
                        {
                            float y = -h * (.22f + i * .26f), off = (float)((t * 70 + i * 9) % 12) * s;
                            float x0 = side * (w * .5f + 3 * s + off);
                            g.DrawLine(line, x0, y, x0 + side * (8 + i * 3) * s, y);
                        }
                }
                if ((L.Decor & Decor.SigStars) != 0) DrawSigStars(g, w, h, s, t);
                if ((L.Decor & Decor.Dots) != 0) DrawDots(g, w, h, s, t);
                if ((L.Decor & Decor.Stars) != 0)
                    using (var stars = new Pen(Color.FromArgb(222, 172, 71), 2 * s) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                        for (int i = 0; i < 3; i++)
                        {
                            double a = t * 3 + i * Math.PI * 2 / 3;
                            float x = (float)Math.Cos(a) * w * .35f, y = -h - 6 * s + (float)Math.Sin(a) * 6 * s;
                            g.DrawLine(stars, x - 3 * s, y, x + 3 * s, y); g.DrawLine(stars, x, y - 3 * s, x, y + 3 * s);
                        }
                if ((L.Decor & Decor.Flower) != 0) DrawBouquet(g, art, w, h, s, ink);
                if ((L.Decor & Decor.Notes) != 0)
                    using (var note = new SolidBrush(Color.FromArgb(176, 111, 160)))
                    using (var stem = new Pen(Color.FromArgb(176, 111, 160), 2 * s))
                        for (int side = -1; side <= 1; side += 2)
                        {
                            float x = side * (w * .47f + 8 * s), y = -h * .8f + (float)Math.Sin(t * 5 + side) * 4 * s;
                            g.FillEllipse(note, x - 3 * s, y, 6 * s, 4 * s);
                            g.DrawLine(stem, x + 2 * s, y + 2 * s, x + 2 * s, y - 10 * s); g.DrawLine(stem, x + 2 * s, y - 10 * s, x + 7 * s, y - 8 * s);
                        }
            }
        }

        /// <summary>시그니처(★) 기술 중 몸 둘레를 돌며 반짝이는 금빛 별.</summary>
        private static void DrawSigStars(Graphics g, float w, float h, float s, float t)
        {
            for (int i = 0; i < 5; i++)
            {
                double a = t * 1.9 + i * Math.PI * 2 / 5;
                float x = (float)Math.Cos(a) * w * .6f, y = -h * .52f + (float)Math.Sin(a) * h * .52f;
                float tw = .75f + .25f * (float)Math.Sin(t * 8 + i * 1.7), r = (4.2f + (i % 2) * 1.6f) * s * tw * 1.35f;
                var pts = new PointF[10];
                for (int k = 0; k < 10; k++)
                {
                    double an = -Math.PI / 2 + k * Math.PI / 5; float rr = k % 2 == 0 ? r : r * .42f;
                    pts[k] = new PointF(x + (float)Math.Cos(an) * rr, y + (float)Math.Sin(an) * rr);
                }
                using (var glow = new SolidBrush(Color.FromArgb(70, 255, 220, 90))) g.FillEllipse(glow, x - r * 1.5f, y - r * 1.5f, r * 3, r * 3);
                using (var fill = new SolidBrush(Color.FromArgb(255, 255, 214, 48)))
                using (var pen = new Pen(Color.FromArgb(255, 176, 108, 14), 1.2f * s) { LineJoin = LineJoin.Round })
                { g.FillPolygon(fill, pts); g.DrawPolygon(pen, pts); }
            }
        }

        /// <summary>보름달(대원숭이 변신): 부드러운 빛 번짐과 분화구.</summary>
        private static void DrawMoon(Graphics g, PointF c, float r, float a)
        {
            if (a <= .01f) return;
            int al = (int)(255 * Math.Min(1, a)); r *= .6f + .4f * Math.Min(1, a);
            using (var path = new GraphicsPath())
            {
                path.AddEllipse(c.X - r * 1.7f, c.Y - r * 1.7f, r * 3.4f, r * 3.4f);
                using (var pg = new PathGradient(path) { CenterColor = Color.FromArgb(al * 150 / 255, 255, 248, 190), SurroundColors = new[] { Color.FromArgb(0, 255, 248, 190) } }) g.FillPath(pg, path);
            }
            using (var body = new SolidBrush(Color.FromArgb(al, 255, 247, 205)))
            using (var rim = new Pen(Color.FromArgb(al, 214, 190, 110), Math.Max(1.5f, r * .06f)))
            using (var crater = new SolidBrush(Color.FromArgb(al * 150 / 255, 226, 208, 150)))
            {
                g.FillEllipse(body, c.X - r, c.Y - r, r * 2, r * 2); g.DrawEllipse(rim, c.X - r, c.Y - r, r * 2, r * 2);
                g.FillEllipse(crater, c.X - r * .5f, c.Y - r * .45f, r * .46f, r * .4f); g.FillEllipse(crater, c.X + r * .15f, c.Y + r * .1f, r * .56f, r * .5f);
                g.FillEllipse(crater, c.X - r * .3f, c.Y + r * .4f, r * .3f, r * .26f); g.FillEllipse(crater, c.X + r * .3f, c.Y - r * .55f, r * .24f, r * .22f);
            }
        }
        /// <summary>두 손으로 안아 든 꽃다발(손은 Rig 가 배 앞에 모아 쥐고 있다).</summary>
        private static void DrawBouquet(Graphics g, CharacterArt art, float w, float h, float s, Pen ink)
        {
            float handY = (art.Rig.BellyY - art.Rig.FootY) * art.K, x = 3 * s, y = handY - 20 * s;
            using (var stem = new Pen(Color.FromArgb(90, 141, 97), 2.4f * s))
                for (int j = -1; j <= 1; j++) g.DrawLine(stem, x - 3 * s, y + 25 * s, x + j * 9 * s, y - (j == 0 ? 7 : 0) * s);
            using (var wrap = new SolidBrush(Color.FromArgb(255, 224, 231)))
            {
                var pts = new[] { new PointF(x - 13 * s, y + 8 * s), new PointF(x + 10 * s, y + 8 * s), new PointF(x - 2 * s, y + 28 * s) };
                g.FillPolygon(wrap, pts); g.DrawPolygon(ink, pts);
            }
            using (var paw = new SolidBrush(Color.FromArgb(255, 224, 190)))
                for (int sd = -1; sd <= 1; sd += 2)
                { float px = x - 2 * s + sd * 7 * s; g.FillEllipse(paw, px - 4.5f * s, y + 18 * s, 9 * s, 7 * s); g.DrawEllipse(ink, px - 4.5f * s, y + 18 * s, 9 * s, 7 * s); }
            for (int j = -1; j <= 1; j++)
            {
                float fx = x + j * 9 * s, fy = y - (j == 0 ? 9 : 0) * s;
                for (int i = 0; i < 5; i++)
                {
                    double a = i * Math.PI * 2 / 5 - Math.PI / 2;
                    float px = fx + (float)Math.Cos(a) * 4 * s, py = fy + (float)Math.Sin(a) * 4 * s;
                    using (var petal = new SolidBrush(j == 0 ? Color.FromArgb(208, 183, 238) : Color.FromArgb(245, 164, 185)))
                    { g.FillEllipse(petal, px - 3.5f * s, py - 3.5f * s, 7 * s, 7 * s); g.DrawEllipse(ink, px - 3.5f * s, py - 3.5f * s, 7 * s, 7 * s); }
                }
                using (var center = new SolidBrush(Color.FromArgb(255, 218, 117))) { g.FillEllipse(center, fx - 3 * s, fy - 3 * s, 6 * s, 6 * s); g.DrawEllipse(ink, fx - 3 * s, fy - 3 * s, 6 * s, 6 * s); }
            }
        }
    }
}
