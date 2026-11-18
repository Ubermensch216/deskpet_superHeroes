using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

[assembly: System.Reflection.AssemblyTitle("수퍼히어로")]
[assembly: System.Reflection.AssemblyProduct("수퍼히어로")]
[assembly: System.Reflection.AssemblyFileVersion("1.0.0.0")]
[assembly: System.Reflection.AssemblyVersion("1.0.0.0")]

namespace SuperHeroes
{
    internal static class Program
    {
                private static void Log(string msg)
        {
            try { File.AppendAllText(Path.Combine(Paths.Dir, "debug.log"), DateTime.Now.ToString("HH:mm:ss.fff") + " " + msg + Environment.NewLine); } catch { }
        }

        [STAThread]
        private static void Main(string[] args)
        {
            try { Native.SetProcessDPIAware(); } catch { }
            string lang = null; var rest = new List<string>();
            for (int a = 0; a < args.Length; a++)
            {
                if (args[a] == "--lang" && a + 1 < args.Length) { lang = args[++a]; continue; }
                if (args[a].StartsWith("--lang=")) { lang = args[a].Substring(7); continue; }
                rest.Add(args[a]);
            }
            args = rest.ToArray();
            if (lang != null) Loc.Force(lang);
            Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
            if (args.Length >= 2 && args[0] == "--make-icon") { DevTools.MakeIcon(args[1]); return; }
            if (args.Length >= 2 && args[0] == "--i18n-test") { Environment.Exit(DevToolsI18n.Run(args[1])); return; }
            if (args.Length >= 2 && args[0] == "--perf") { DevTools.Perf(args[1]); return; }
            if (args.Length >= 2 && args[0] == "--render-test") { DevTools.RenderSheet(args[1], args.Length >= 3 ? float.Parse(args[2]) : Native.SystemDpiScale()); return; }
            if (args.Length >= 5 && args[0] == "--sig-shots") { DevTools.SigShots(args[1], args[2], int.Parse(args[3]), args[4], args.Length >= 6 ? float.Parse(args[5]) : 2f); return; }
            if (args.Length >= 2 && args[0] == "--selftest") { Environment.Exit(DevTools.SelfTest(args[1])); return; }
            if (args.Length >= 2 && args[0] == "--ui-test") { Environment.Exit(DevTools.UiTest(args[1])); return; }
            if (args.Length >= 2 && args[0] == "--shot-settings") { DevTools.ShotSettings(args[1], args.Length >= 3 ? int.Parse(args[2]) : 0); return; }
            Loc.Load();
            int cliExit;
            if (ApiServer.TryForwardCliCommand(args, out cliExit)) { Environment.Exit(cliExit); return; }
            Log("Main start");
            bool created;
            using (var mutex = new Mutex(true, "SuperHeroes_DeskFriend_single_instance", out created))
            {
                Log("Mutex created: " + created);
                if (!created)
                {
                    Log("Exiting: mutex not created");
                    MessageBox.Show(Loc.T("수퍼히어로가 이미 실행 중이야.\n작업표시줄 알림 영역의 친구 아이콘을 우클릭해서 ‘화면 가운데로 다시 부르기’를 눌러줘."), Loc.T("수퍼히어로"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException += delegate (object s, ThreadExceptionEventArgs e) {
                    Log("ThreadException: " + e.Exception);
                    FeatureLog.Write(e.Exception);
                };
                AppDomain.CurrentDomain.UnhandledException += delegate (object s, UnhandledExceptionEventArgs e) {
                    Log("Unhandled: " + e.ExceptionObject);
                };
                try
                {
                    Log("Loading options...");
                    PlayOptions.Load(); FriendData.Load();
                    CharacterArt.Reset(Native.SystemDpiScale(), PlayOptions.Current.SizeScale);
                    Log("Creating App...");
                    var app = new App();
                    Log("Starting Notes...");
                    app.Notes = new NoteManager(app); app.Notes.Start(); app.StartApi();
                    Log("Running Application.Run...");
                    Application.Run(app);
                    Log("Application.Run returned.");
                }
                catch (Exception error)
                {
                    Log("Caught in Main: " + error);
                    FeatureLog.Write(error);
                    MessageBox.Show(Loc.T("친구를 화면에 표시하지 못했어.\n\n") + error.Message + Loc.T("\n\n기록: ") + Path.Combine(Paths.Dir, "수퍼히어로_오류기록.txt"), Loc.T("수퍼히어로"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                GC.KeepAlive(mutex);
            }
            Log("Main ended.");
        }
    }

    /// <summary>화면 없이 돌려 보는 가짜 세계 (자체 점검·렌더링 확인용).</summary>
    internal sealed class FakeWorld : IWorld
    {
        public List<Rectangle> Areas { get; set; }
        public Random Rng { get; set; }
        public double Now { get; set; }
        public FakeWorld() { Areas = new List<Rectangle> { new Rectangle(0, 0, 1280, 720) }; Rng = new Random(7); }
    }

    internal static class DevTools
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern void mouse_event(int flags, int dx, int dy, int data, UIntPtr extra);
        [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern IntPtr WindowFromPoint(Point p);
        [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern void keybd_event(byte vk, byte scan, int flags, UIntPtr extra);

        internal static void ShotSettings(string outPath, int page = 0)
        {
            PlayOptions.Load(); FriendData.Load(); CharacterArt.Reset(Native.SystemDpiScale());
            var app = new App(true);
            app.Spawn(1); // Batman
            var f = new SettingsForm(app, page);
            f.Show();
            for (int i = 0; i < 25; i++) { Application.DoEvents(); Thread.Sleep(20); }
            using (var b = new Bitmap(f.Width, f.Height))
            {
                f.DrawToBitmap(b, new Rectangle(0, 0, f.Width, f.Height));
                b.Save(outPath, ImageFormat.Png);
            }
            f.Close();
        }

        /// <summary>걷는 모습을 계속 그리는 데 드는 시간 측정(결과: perf.txt).</summary>
        internal static void Perf(string dir)
        {
            Directory.CreateDirectory(dir);
            var sb = new StringBuilder();
            foreach (float scale in new[] { 1f, 1.5f, 2f })
            {
                CharacterArt.Reset(scale);
                PlayOptions.Current = new PlayOptions();
                DesktopSurfaces.TestProvider = () => new List<SurfaceWindow>(); DesktopSurfaces.TestMouse = () => new Point(-9999, -9999);
                FriendData.Current = new FriendData();
                for (int c = 0; c < CharacterArt.Count; c++)
                {
                    var w = new FakeWorld(); var pet = new Pet(w, c, 640, 720) { Area = w.Areas[0] };
                    pet.Y = 720; pet.SetState(PetState.Walk, w);
                    var sw = System.Diagnostics.Stopwatch.StartNew(); int n = 200;
                    for (int i = 0; i < n; i++) { w.Now += .03; pet.Update(w, .03, null, 0); PointF o; using (Pet.Render(pet.GetLook(w.Now), out o)) { } }
                    sb.AppendLine(string.Format("scale {0} {1}: {2:0.00} ms/frame", scale, CharacterArt.Infos[c].Name, sw.Elapsed.TotalMilliseconds / n));
                }
            }
            File.WriteAllText(Path.Combine(dir, "perf.txt"), sb.ToString());
        }

        /// <summary>수퍼히어로 얼굴로 PNG 압축 .ico 를 만든다(빌드할 때 한 번 쓰임).</summary>
        internal static void MakeIcon(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            int[] sizes = { 16, 24, 32, 48, 64, 128, 256 };
            var pngs = new List<byte[]>();
            foreach (int n in sizes)
                using (Bitmap bmp = CharacterArt.IconBitmap(0, n))
                using (var ms = new MemoryStream()) { bmp.Save(ms, ImageFormat.Png); pngs.Add(ms.ToArray()); }
            using (var fs = File.Create(path))
            using (var bw = new BinaryWriter(fs))
            {
                bw.Write((ushort)0); bw.Write((ushort)1); bw.Write((ushort)sizes.Length);
                int offset = 6 + 16 * sizes.Length;
                for (int i = 0; i < sizes.Length; i++)
                {
                    byte b = sizes[i] >= 256 ? (byte)0 : (byte)sizes[i];
                    bw.Write(b); bw.Write(b); bw.Write((byte)0); bw.Write((byte)0);
                    bw.Write((ushort)1); bw.Write((ushort)32); bw.Write((uint)pngs[i].Length); bw.Write((uint)offset);
                    offset += pngs[i].Length;
                }
                foreach (byte[] p in pngs) bw.Write(p);
            }
        }

        /// <summary>캐릭터별·상태별 모습을 한 장의 시트 PNG 로 저장한다.</summary>
        /// <summary>시그니처 동작을 지정한 시각(초)마다 크게 한 컷씩 뽑아 한 장으로 잇는다. 예: --sig-shots out Thor 1 1.8,3,3.5,4.5,5.5,6.4</summary>
        internal static void SigShots(string dir, string who, int sig, string times, float scale)
        {
            Directory.CreateDirectory(dir);
            CharacterArt.Reset(scale);
            PlayOptions.Current = new PlayOptions();
            DesktopSurfaces.TestProvider = () => new List<SurfaceWindow>(); DesktopSurfaces.TestMouse = () => new Point(-9999, -9999);
            FriendData.Current = new FriendData();
            int c = 0; for (int i = 0; i < CharacterArt.Count; i++) if (CharacterArt.Infos[i].Persona.ToString() == who) c = i;
            var ts = new List<double>(); foreach (string s in times.Split(',')) ts.Add(double.Parse(s, System.Globalization.CultureInfo.InvariantCulture));
            var shots = new List<Bitmap>();
            var w = new FakeWorld { Rng = new Random(5) }; w.Areas[0] = new Rectangle(0, 0, 1280, 720);
            var pet = new Pet(w, c, 640, 720) { Area = w.Areas[0] };
            pet.SetState(PetState.Pause, w); pet.X = 640; pet.Y = 720; pet.Demonstrate(w, sig < 0 ? "krypto" : "sig" + sig);
            double now = 0;
            foreach (double t in ts)
            {
                for (; now < t - 1e-6; now += 0.02) { w.Now += 0.02; pet.Update(w, 0.02, null, 0); }
                PointF origin; shots.Add(Pet.Render(pet.GetLook(w.Now), out origin));
            }
            int W = 0, H = 0; foreach (var b in shots) { W += b.Width; H = Math.Max(H, b.Height); }
            using (var sheet = new Bitmap(W, H, PixelFormat.Format32bppArgb))
            using (var g = Graphics.FromImage(sheet))
            {
                g.Clear(Color.FromArgb(70, 80, 100)); int x = 0;
                foreach (var b in shots) { g.DrawImage(b, x, H - b.Height, b.Width, b.Height); x += b.Width; b.Dispose(); }
                sheet.Save(Path.Combine(dir, "shots_" + who + "_" + sig + ".png"), ImageFormat.Png);
            }
            Console.WriteLine("saved " + dir);
        }

        internal static void RenderSheet(string dir, float scale)
        {
            Directory.CreateDirectory(dir);
            CharacterArt.Reset(scale);
            PlayOptions.Current = new PlayOptions();
            DesktopSurfaces.TestProvider = () => new List<SurfaceWindow>(); DesktopSurfaces.TestMouse = () => new Point(-9999, -9999);
            FriendData.Current = new FriendData();
            var states = new List<KeyValuePair<string, Action<Pet, FakeWorld>>>
            {
                K("걷기", (p, w) => { p.Y = 720; p.SetState(PetState.Walk, w); Adv(p, w, .5); }),
                K("서기", (p, w) => p.SetState(PetState.Pause, w)),
                K("앉기", (p, w) => p.Demonstrate(w, "sit")),
                K("낮잠", (p, w) => { p.Y = 720; p.SetState(PetState.Sleep, w); Adv(p, w, 1.2); }),
                K("눌림", (p, w) => { p.StartSquish(w, .4, .24, PetState.Walk, false); w.Now += .2; p.Update(w, .2, null, 0); }),
                K("우는중", (p, w) => p.Demonstrate(w, "cry")),
                K("배고픔", (p, w) => p.Demonstrate(w, "weep")),
                K("폴짝", (p, w) => { p.Demonstrate(w, "hop"); p.Update(w, .25, null, 0); }),
                K("슬라이딩", (p, w) => { p.Y = 720; p.Demonstrate(w, "slide"); Adv(p, w, .3); }),
                K("어질어질", (p, w) => p.Demonstrate(w, "dizzy")),
                K("비행하강", (p, w) => { p.Demonstrate(w, "fall"); Adv(p, w, .6); }),
                K("벽타기", (p, w) => p.Demonstrate(w, "wall")),
                K("천장", (p, w) => p.Demonstrate(w, "ceiling")),
                K("먹는중", (p, w) => { FriendData.Current.Satiety = 40; p.Feed(w); }),
                K("꽃다발", (p, w) => p.StartFlower(w, 100)),
                K("말풍선", (p, w) => p.Cheer(w, "점심 먹으러 가자!", 8)),
                K("★기술1", (p, w) => { p.Y = 720; p.Demonstrate(w, "sig0"); Adv(p, w, 3.9); }),
                K("★기술2", (p, w) => { p.Y = 720; p.Demonstrate(w, "sig1"); Adv(p, w, 3.8); }),
                K("★기술3", (p, w) => { p.Y = 720; p.Demonstrate(w, "sig2"); Adv(p, w, 3.6); }),
                K("★기술4", (p, w) => { p.Y = 720; p.Demonstrate(w, "sig3"); Adv(p, w, 3.5); }),
                K("우는중2", (p, w) => { p.Y = 720; p.Demonstrate(w, "cry"); Adv(p, w, .7); }),
                K("눌림2", (p, w) => { p.Y = 720; p.StartSquish(w, .42, .24, PetState.Walk, false); Adv(p, w, .2); }),
                K("앉기2", (p, w) => { p.Y = 720; p.Demonstrate(w, "sit"); Adv(p, w, 5); }),
                K("먹는중2", (p, w) => { p.Y = 720; FriendData.Current.Satiety = 40; p.Feed(w); Adv(p, w, 2.2); }),
                K("멍때리기", (p, w) => { p.Y = 720; p.Demonstrate(w, "daydream"); Adv(p, w, 2.5); }),
                K("기지개", (p, w) => { p.Y = 720; p.Demonstrate(w, "stretch"); Adv(p, w, 1.9); }),
                K("★1-휘청", (p, w) => { p.Y = 720; p.Demonstrate(w, "sig0"); Adv(p, w, 7.5); }),
                K("★1-헉헉", (p, w) => { p.Y = 720; p.Demonstrate(w, "sig0"); Adv(p, w, 9.0); }),
                K("★1-지팡이", (p, w) => { p.Y = 720; p.Demonstrate(w, "sig0"); Adv(p, w, 11.0); }),
                K("★2-휘청", (p, w) => { p.Y = 720; p.Demonstrate(w, "sig1"); Adv(p, w, 6.7); }),
                K("★2-헉헉", (p, w) => { p.Y = 720; p.Demonstrate(w, "sig1"); Adv(p, w, 8.2); }),
                K("★2-지팡이", (p, w) => { p.Y = 720; p.Demonstrate(w, "sig1"); Adv(p, w, 10.4); }),
            };
            int cell = (int)Math.Round(150 * scale), cols = 8, rows = (int)Math.Ceiling(states.Count / (double)cols);
            for (int c = 0; c < CharacterArt.Count; c++)
            {
                using (var sheet = new Bitmap(cell * cols, cell * rows, PixelFormat.Format32bppArgb))
                using (var g = Graphics.FromImage(sheet))
                using (var font = new Font("Malgun Gothic", 9f * scale))
                {
                    g.Clear(Color.FromArgb(206, 226, 214)); g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                    for (int i = 0; i < states.Count; i++)
                    {
                        var w = new FakeWorld(); w.Areas[0] = new Rectangle(0, 0, 1280, 720);
                        var pet = new Pet(w, c, 640, 400) { Area = w.Areas[0] };
                        pet.SetState(PetState.Pause, w);
                        pet.X = 640; pet.Y = 400;
                        states[i].Value(pet, w);
                        FriendData.Current.Satiety = 100;
                        PointF origin; using (Bitmap b = Pet.Render(pet.GetLook(w.Now), out origin))
                        {
                            int cx = (i % cols) * cell, cy = (i / cols) * cell;
                            float f = Math.Min(1f, Math.Min((cell - 10f) / b.Width, (cell - 30f) / b.Height));
                            g.DrawImage(b, cx + (cell - b.Width * f) / 2, cy + cell - 18 * scale - b.Height * f, b.Width * f, b.Height * f);
                            g.DrawString(CharacterArt.Infos[c].Name + " · " + states[i].Key, font, Brushes.Black, cx + 4, cy + cell - 18 * scale);
                        }
                    }
                    sheet.Save(Path.Combine(dir, "sheet_" + CharacterArt.Infos[c].Key + ".png"), ImageFormat.Png);
                }
            }
            // 벽·천장 타기 한 주기(약 0.6초)를 10컷으로
            // 시간 흐름에 따른 연속 컷(캐릭터별 한 장): 걷기·벽 오르기/내리기·천장·낙하·점프·기지개·멍때리기·잠자기
            for (int c = 0; c < CharacterArt.Count; c++)
            {
                var rowDefs = new List<Tuple<string, Action<Pet, FakeWorld>, double, double>>
                {
                    Tuple.Create<string, Action<Pet, FakeWorld>, double, double>("걷기", (p, w) => { p.Y = 720; p.SetState(PetState.Walk, w); }, 0.55, 0),
                    Tuple.Create<string, Action<Pet, FakeWorld>, double, double>("벽 오르기", (p, w) => p.Demonstrate(w, "wall"), 0.55, 0),
                    Tuple.Create<string, Action<Pet, FakeWorld>, double, double>("벽 내려오기", (p, w) => p.Demonstrate(w, "walldown"), 0.55, 0),
                    Tuple.Create<string, Action<Pet, FakeWorld>, double, double>("천장", (p, w) => p.Demonstrate(w, "ceiling"), 0.55, 0),
                    Tuple.Create<string, Action<Pet, FakeWorld>, double, double>("낙하", (p, w) => p.Demonstrate(w, "fall"), 2.6, 0),
                    Tuple.Create<string, Action<Pet, FakeWorld>, double, double>("점프", (p, w) => { p.Y = 720; p.Demonstrate(w, "hop"); }, 0.7, 0),
                    Tuple.Create<string, Action<Pet, FakeWorld>, double, double>("기지개", (p, w) => { p.Y = 720; p.Demonstrate(w, "stretch"); }, 4.2, 0),
                    Tuple.Create<string, Action<Pet, FakeWorld>, double, double>("멍때리기", (p, w) => { p.Y = 720; p.Demonstrate(w, "daydream"); }, 3.6, 0),
                    Tuple.Create<string, Action<Pet, FakeWorld>, double, double>("잠자기", (p, w) => { p.Y = 720; p.Demonstrate(w, "sleep"); }, 3.6, 0),
                    Tuple.Create<string, Action<Pet, FakeWorld>, double, double>("★1 " + Signatures.All[c][0].Name, (p, w) => { p.Y = 720; p.Demonstrate(w, "sig0"); }, Signatures.All[c][0].Dur, 0),
                    Tuple.Create<string, Action<Pet, FakeWorld>, double, double>("★2 " + Signatures.All[c][1].Name, (p, w) => { p.Y = 720; p.Demonstrate(w, "sig1"); }, Signatures.All[c][1].Dur, 0),
                    Tuple.Create<string, Action<Pet, FakeWorld>, double, double>("★3 " + Signatures.All[c][2].Name, (p, w) => { p.Y = 720; p.Demonstrate(w, "sig2"); }, Signatures.All[c][2].Dur, 0),
                    Tuple.Create<string, Action<Pet, FakeWorld>, double, double>("★4 " + Signatures.All[c][3].Name, (p, w) => { p.Y = 720; p.Demonstrate(w, "sig3"); }, Signatures.All[c][3].Dur, 0),
                    Tuple.Create<string, Action<Pet, FakeWorld>, double, double>("슬라이딩", (p, w) => { p.Y = 720; p.Demonstrate(w, "slide"); }, .84, 0),
                    Tuple.Create<string, Action<Pet, FakeWorld>, double, double>("눌림(클릭)", (p, w) => { p.Y = 720; p.StartSquish(w, .42, .24, PetState.Walk, false); }, .42, 0),
                    Tuple.Create<string, Action<Pet, FakeWorld>, double, double>("우는중", (p, w) => { p.Y = 720; p.Demonstrate(w, "cry"); }, 2.4, 0),
                    Tuple.Create<string, Action<Pet, FakeWorld>, double, double>("먹는중", (p, w) => { p.Y = 720; FriendData.Current.Satiety = 40; p.Feed(w); }, 3.0, 0),
                };
                int cw = (int)Math.Round(100 * scale), ch = (int)Math.Round(92 * scale), frameCols = 12;
                using (var strip = new Bitmap(cw * frameCols, ch * rowDefs.Count, PixelFormat.Format32bppArgb))
                using (var g = Graphics.FromImage(strip))
                using (var font = new Font("Malgun Gothic", 8f * scale))
                {
                    g.Clear(Color.FromArgb(206, 226, 214)); g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                    for (int r = 0; r < rowDefs.Count; r++)
                    {
                        var w = new FakeWorld { Rng = new Random(5) };
                        var pet = new Pet(w, c, 640, 720) { Area = w.Areas[0] };
                        pet.SetState(PetState.Pause, w); pet.X = 640; pet.Y = 720;
                        rowDefs[r].Item2(pet, w);
                        g.DrawString(rowDefs[r].Item1, font, Brushes.Black, 2, r * ch + 2);
                        double dt = rowDefs[r].Item3 / frameCols;
                        for (int i = 0; i < frameCols; i++)
                        {
                            for (double s2 = 0; s2 < dt - 1e-6; s2 += 0.02) { w.Now += 0.02; pet.Update(w, 0.02, null, 0); }
                            PointF origin; using (Bitmap b = Pet.Render(pet.GetLook(w.Now), out origin))
                            {
                                float f = Math.Min(1f, Math.Min((cw - 4f) / b.Width, (ch - 14f) / b.Height));
                                g.DrawImage(b, i * cw + (cw - b.Width * f) / 2, r * ch + ch - 2 - b.Height * f, b.Width * f, b.Height * f);
                            }
                        }
                    }
                    strip.Save(Path.Combine(dir, "motion_" + CharacterArt.Infos[c].Key + ".png"), ImageFormat.Png);
                }
            }            Console.WriteLine("saved " + dir);
        }

        /// <summary>실제 창들을 만들어 띄우고 그림으로 남긴다 (레이아웃·실행 오류 확인).</summary>
        internal static int UiTest(string dir)
        {
            Directory.CreateDirectory(dir);
            Paths.StorageOverride = Path.Combine(dir, "data");
            var log = new StringBuilder(); int failures = 0;
            Action<string, Action> step = (name, a) => { try { a(); log.AppendLine("OK   " + name); } catch (Exception e) { failures++; log.AppendLine("FAIL " + name + " " + e); } };
            Action<double> pump = secs => { var end = DateTime.UtcNow.AddSeconds(secs); while (DateTime.UtcNow < end) { Application.DoEvents(); Thread.Sleep(15); } };
            Action<Form, string> shot = (f, name) =>
            {
                f.Show(); pump(.4);
                using (var b = new Bitmap(f.Width, f.Height)) { f.DrawToBitmap(b, new Rectangle(0, 0, f.Width, f.Height)); b.Save(Path.Combine(dir, name + ".png"), ImageFormat.Png); }
            };
            Action<string> screen = name =>
            {
                Rectangle r = Screen.PrimaryScreen.Bounds;
                using (var b = new Bitmap(r.Width, r.Height)) using (var g = Graphics.FromImage(b)) { g.CopyFromScreen(r.Location, Point.Empty, r.Size); b.Save(Path.Combine(dir, name + ".png"), ImageFormat.Png); }
            };
            PlayOptions.Load(); FriendData.Load(); CharacterArt.Reset(Native.SystemDpiScale() * PlayOptions.Current.SizeScale);
            App app = null;
            step("앱 생성(펫 5마리)", () => { app = new App(true); app.Spawn(0); app.Spawn(1); app.Spawn(2); app.Spawn(3); app.Spawn(4); pump(1); });
            for (int pg = 0; pg < SettingsForm.PageNames.Length; pg++) { int page = pg; step("설정 창 - " + SettingsForm.PageNames[pg], () => { var f = new SettingsForm(app, page); shot(f, "ui_settings_" + page); f.Close(); }); }
            step("펫 전원: 끄면 캐릭터 창이 숨고 켜면 돌아옴", () =>
            {
                bool before = PlayOptions.Current.PetPower;
                try
                {
                    app.SetPetPower(true); pump(.2);
                    if (app.PetOff || app.LiveWindows.Count == 0 || app.LiveWindows.Any(w => !w.Visible)) throw new Exception("power on: pets not visible");
                    app.SetPetPower(false); pump(.2);
                    if (!app.PetOff || !app.AllHidden || app.LiveWindows.Any(w => w.Visible)) throw new Exception("power off: pets still visible");
                    var f = new SettingsForm(app, 0); shot(f, "ui_settings_power_off"); f.Close();   // 전원이 꺼진 캐릭터 쪽
                    app.SetPetPower(true); pump(.2);
                    if (app.PetOff || app.LiveWindows.Any(w => !w.Visible)) throw new Exception("power back on: pets hidden");
                }
                finally { if (PlayOptions.Current.PetPower != before) app.SetPetPower(before); }
            });
            step("마릿수: 도감에서 고른 캐릭터를 +/−", () =>
            {
                int n = app.LiveWindows.Count;
                app.SpawnFromUi(0); pump(.2);
                if (app.LiveWindows.Count != n + 1) throw new Exception("spawn +1 failed");
                app.Remove(app.LiveWindows.Last()); pump(.2);
                if (app.LiveWindows.Count != n) throw new Exception("remove -1 failed");
            });
            step("정각 알림: 별도 창 없이 활동 중인 캐릭터가 말풍선으로", () =>
            {
                int forms = Application.OpenForms.Count;
                app.BeginHourly("지금은 오후 1시야!"); pump(.4);
                bool bubbles = app.LiveWindows.Count > 0 && app.LiveWindows.All(w => w.Pet.hourlyBubble == "지금은 오후 1시야!");
                int formsAfter = Application.OpenForms.Count;
                app.ClearHourly(); pump(.2);
                if (!bubbles) throw new Exception("pets did not show hourly bubble");
                if (formsAfter != forms) throw new Exception("a separate window was opened: " + forms + " -> " + formsAfter);
            });
            step("도우미 창", () => { FriendData.Current.Todos.Add(new TodoItem { Text = "첫 할 일", Done = true }); FriendData.Current.Todos.Add(new TodoItem { Text = "둘째 할 일" }); var f = new FriendPanel(app); shot(f, "ui_helper"); f.Close(); });
            step("정각 카드 + 가장자리 빛", () =>
            {
                var n = new HourlyNotice(app.TrayIconImage); n.SetMessage(App.HourlyMessage(DateTime.Now, true, "13:00", "점심 먹으러 가자!", false, "18:00", "x"));
                n.EnableEdgeGlow(true, 1, 1, 1); n.Show(); pump(1.2); screen("ui_notice_glow"); if (!n.GlowRunning) throw new Exception("glow not running"); n.Close(); pump(.3);
            });
            step("펫 걷기 관찰", () => { pump(20); screen("ui_pets"); });
            // 실제 마우스를 쓰는 점검이라 사용자가 동시에 마우스를 움직이면 어긋날 수 있다 → 최대 3번 시도
            Action<string, Action> stepRetry = (name, a) =>
            {
                for (int attempt = 1; ; attempt++)
                {
                    try { a(); log.AppendLine("OK   " + name + (attempt > 1 ? " (" + attempt + "번째 시도)" : "")); return; }
                    catch (Exception e) { if (attempt >= 3) { failures++; log.AppendLine("FAIL " + name + " " + e); return; } pump(1); }
                }
            };
            stepRetry("실제 마우스: 클릭·드래그·던지기", () =>
            {
                PlayOptions.Current.Escape = false; PlayOptions.Current.Idle = false;   // 커서를 가까이 대도 도망가지 않게
                // 투명한 곳은 클릭이 통과하고, 다른 프로그램의 맨 위 창이 가리고 있을 수도 있으므로
                // 지금 그려진 그림에서 불투명하고 실제로 이 창이 받는 점(가운데에 가까운 순)을 찾아 누른다.
                Func<PetWindow, Point?> hitPoint = w =>
                {
                    PointF o; using (Bitmap b = Pet.Render(w.Pet.GetLook(app.Now), out o))
                    {
                        var cand = new List<KeyValuePair<double, Point>>();
                        for (int y = 0; y < b.Height; y++) for (int x = 0; x < b.Width; x++)
                            if (b.GetPixel(x, y).A > 240) cand.Add(new KeyValuePair<double, Point>(Math.Pow(x - b.Width / 2.0, 2) + Math.Pow(y - b.Height * .55, 2), new Point(x, y)));
                        cand.Sort((p, q) => p.Key.CompareTo(q.Key));
                        int tries = 0;
                        foreach (var c in cand)
                        {
                            if (tries++ > 400) break;
                            Point screenPt = new Point(w.Left + c.Value.X, w.Top + c.Value.Y);
                            if (WindowFromPoint(screenPt) == w.Handle) return screenPt;
                        }
                        return null;
                    }
                };
                PetWindow target = null;
                foreach (PetWindow w in app.LiveWindows.Where(w => w.Pet.State == PetState.Walk || w.Pet.State == PetState.Pause).ToList())
                {
                    w.Pet.SupportWindow = IntPtr.Zero; w.Pet.Y = w.Pet.Area.Bottom; w.Pet.SetState(PetState.Pause, app); pump(.25);
                    if (hitPoint(w).HasValue) { target = w; break; }
                }
                if (target == null) throw new Exception("클릭할 수 있는 친구를 찾지 못함(다른 창이 모두 가리고 있음)");
                Func<Point> center = () => hitPoint(target) ?? new Point(target.Left + target.Width / 2, target.Top + target.Height / 2);
                Action<int> mouse = flags => mouse_event(flags, 0, 0, 0, UIntPtr.Zero);
                Action<Point> go = p => { Cursor.Position = p; pump(.03); };
                // 클릭
                target.Pet.SupportWindow = IntPtr.Zero; target.Pet.Y = target.Pet.Area.Bottom; target.Pet.SetState(PetState.Pause, app); pump(.25);
                Point cp = center(); go(cp); pump(.1); mouse(2); pump(.05); mouse(4); pump(.1);
                if (target.Pet.State != PetState.Squish && target.Pet.State != PetState.TouchCry && target.Pet.State != PetState.Hop) { screen("ui_clickfail"); throw new Exception("click did not react: " + target.Pet.State + " " + target.Pet.Art.Info.Name + " click@" + cp + " win@" + target.Bounds + " cursor@" + Cursor.Position + " hitWin=" + WindowFromPoint(cp) + " target=" + target.Handle + " others=" + string.Join(",", app.LiveWindows.Select(w => w.Pet.Art.Info.Name + "@" + w.Bounds))); }
                pump(2.5);
                // 드래그 → 던지기
                target.Pet.SupportWindow = IntPtr.Zero; target.Pet.Y = target.Pet.Area.Bottom; target.Pet.SetState(PetState.Pause, app); pump(.25);
                Point start = center(); go(start); pump(.1); mouse(2); pump(.05);
                for (int i = 1; i <= 8; i++) { go(new Point(start.X + i * 14, start.Y - i * 5)); pump(.012); }
                if (target.Pet.State != PetState.Drag) throw new Exception("drag not started: " + target.Pet.State);
                mouse(4); pump(.05);
                if (target.Pet.State != PetState.Thrown && target.Pet.State != PetState.Falling) throw new Exception("not thrown: " + target.Pet.State);
                pump(1.2); screen("ui_thrown");
                // 우클릭 메뉴
                app.ShowPetMenu(target, new Point(Screen.PrimaryScreen.WorkingArea.Width / 2, 300)); pump(.6); screen("ui_menu");
                keybd_event(0x1B, 0, 0, UIntPtr.Zero); keybd_event(0x1B, 0, 2, UIntPtr.Zero); pump(.4);
            });
            step("종료", () => { app.Quit(); });
            log.AppendLine(failures == 0 ? "ALL OK" : failures + " FAILED");
            File.WriteAllText(Path.Combine(dir, "uitest_result.txt"), log.ToString(), new UTF8Encoding(true));
            return failures == 0 ? 0 : 1;
        }

        private static void Adv(Pet p, FakeWorld w, double secs)
        { for (double s = 0; s < secs - 1e-6; s += .02) { w.Now += .02; p.Update(w, .02, null, 0); } }

        private static KeyValuePair<string, Action<Pet, FakeWorld>> K(string name, Action<Pet, FakeWorld> a)
        { return new KeyValuePair<string, Action<Pet, FakeWorld>>(name, a); }

        /// <summary>저장·친밀도 계산·상태 전이 등을 화면 없이 점검. 실패가 있으면 1 을 돌려준다.</summary>
        internal static int SelfTest(string dir)
        {
            Directory.CreateDirectory(dir);
            Paths.StorageOverride = Path.Combine(dir, "data");
            var log = new StringBuilder(); int failures = 0;
            Action<bool, string> check = (ok, name) => { log.AppendLine((ok ? "PASS " : "FAIL ") + name); if (!ok) failures++; };
            try
            {
                CharacterArt.Reset(1f);
                DesktopSurfaces.TestProvider = () => new List<SurfaceWindow>(); DesktopSurfaces.TestMouse = () => new Point(-9999, -9999);

                // 설정 저장·불러오기
                var s = new Settings { LunchTime = "11:30", LeaveMsg = "가자", NapMinutes = 7, EdgeGlowTheme = 3, QuietWeekdays = true, QuietStart = 22, QuietEnd = 6, Roster = "1,3" };
                check(s.Save(), "설정 저장");
                var s2 = Settings.Load();
                check(s2.LunchTime == "11:30" && s2.LeaveMsg == "가자" && s2.NapMinutes == 7 && s2.EdgeGlowTheme == 3 && s2.QuietStart == 22 && s2.Roster == "1,3", "설정 불러오기 일치");
                check(Settings.ParseTime("9:05") == new TimeSpan(9, 5, 0) && !Settings.ParseTime("25:00").HasValue && !Settings.ParseTime("abc").HasValue, "시간 해석");
                var quiet = new Settings { QuietWeekdays = true, QuietStart = 9, QuietEnd = 18 };
                check(!quiet.HourAllowed(new DateTime(2026, 10, 21, 10, 0, 0)) && quiet.HourAllowed(new DateTime(2026, 10, 24, 10, 0, 0)) && quiet.HourAllowed(new DateTime(2026, 10, 21, 20, 0, 0)), "평일 제외 시간대");
                check(App.HourlyMessage(new DateTime(2026, 10, 21, 12, 0, 0), true, "12:00", "점심!", true, "18:00", "퇴근!") == "지금은 오후 12시야!\n점심!", "정각 문구+점심 병합");
                check(App.HourlyText(new DateTime(2026, 10, 21, 0, 0, 0)) == "지금은 오전 12시야!", "정각 문구 0시");

                var o = new PlayOptions { Walls = false, Speed = 1.5, ClickMode = ClickReaction.Cry, HungerMinutes = 30, PetOpacityPercent = 70, SizePercent = 150, FixedX = 123.5, FixedY = 456.25, PetPower = false };
                PlayOptions.Current = o; check(o.Save(), "움직임 설정 저장");
                PlayOptions.Load(); var l = PlayOptions.Current;
                check(!l.Walls && l.Speed == 1.5 && l.ClickMode == ClickReaction.Cry && l.HungerMinutes == 30 && l.PetOpacityPercent == 70 && l.SizePercent == 150 && l.FixedX == 123.5 && l.FixedY == 456.25, "움직임 설정 불러오기 일치");
                check(!l.PetPower && new PlayOptions().PetPower, "펫 전원: 기본은 켜짐, 꺼 둔 상태는 저장·불러오기로 유지");

                // 친구 기록
                PlayOptions.Current = new PlayOptions { HungerMinutes = 60 };
                var d = FriendData.Current = new FriendData();
                d.AdvanceLife(60);
                check(d.Affinity == 1 && d.TogetherSeconds == 60, "함께한 1분 = 친밀도 +1");
                check(Math.Abs(d.Satiety - (100 - 60 * 80.0 / 3600)) < 1e-6, "포만감 감소량");
                d.Satiety = 78; check(d.TrySnack() && d.Satiety == 100 && d.Affinity == 6, "간식: 포만감 +25(최대 100), 친밀도 +5");
                check(!d.TrySnack(), "배가 꽉 차면 간식 거절");
                d.Satiety = 80; check(!d.TrySnack(), "포만감 80 이상이면 간식 거절");
                d.Affinity = 250; d.CelebratedHundreds = 1; check(d.TakeMilestone() && d.CelebratedHundreds == 2 && !d.TakeMilestone(), "100 단위 꽃다발 한 번씩");
                d.Satiety = 15; check(d.Hungry && !d.VeryHungry && !d.Starving && Math.Abs(d.HungerSpeedMultiplier - .75) < 1e-6, "포만감 20 이하 = 배고픔(속도 0.75)");
                d.Satiety = 10; check(d.Hungry && d.VeryHungry && !d.Starving && Math.Abs(d.HungerSpeedMultiplier - .40) < 1e-6, "포만감 10 이하 = 기력 저하(속도 0.40)");
                d.Satiety = 5; check(d.Hungry && d.VeryHungry && d.Starving && d.HungerSpeedMultiplier == 0, "포만감 5 이하 = 탈진 정지");
                PlayOptions.Current.HungerMinutes = 0; d.Satiety = 100; check(d.Hungry && !d.VeryHungry && !d.Starving && d.HungerSpeedMultiplier == 1.0, "0분 = 항상 우는 표정(속도는 정상)");
                PlayOptions.Current.HungerMinutes = 60;
                d.Todos.Add(new TodoItem { Text = "첫 할 일", Done = true }); d.Todos.Add(new TodoItem { Text = "둘째", Done = false });
                d.Notes = "메모\n두 줄"; d.TimerText = "쉬자"; d.TimerUtc = DateTime.UtcNow.AddMinutes(5); d.Discover("dizzy");
                check(d.Save(), "기록 저장");
                FriendData.Load(); var r = FriendData.Current;
                check(r.Affinity == d.Affinity && r.Todos.Count == 2 && r.Todos[0].Done && !r.Todos[1].Done && r.Notes == "메모\n두 줄" && r.TimerText == "쉬자" && r.Discoveries.Contains("dizzy") && r.TimerUtc > DateTime.UtcNow, "기록 불러오기 일치(할 일·메모·타이머·비밀)");
                d.Save(); check(File.Exists(FriendData.DataPath + ".bak"), "이전 기록 .bak 유지");
                FriendData.Current = new FriendData();

                // 스프라이트
                for (int c = 0; c < CharacterArt.Count; c++)
                {
                    var a = CharacterArt.Get(c);
                    var probe = new Pet(new FakeWorld(), c, 640, 400);
                    PointF org; Pet.Look stand = probe.GetLook(0);
                    using (Bitmap bmp = Pet.Render(stand, out org))
                    {
                        Rectangle ink = CharacterArt.Bounds(bmp);
                        check(a.W > 30 && a.H > 30 && ink.Height > a.H * .8 && ink.Width > a.W * .4 && a.Ceiling.H == a.H, CharacterArt.Infos[c].Name + " 그림 생성");
                    }
                }

                // 상태 전이
                PlayOptions.Current = new PlayOptions { Escape = false, Idle = false, Windows = false, Walls = false, Ceiling = false };
                var w = new FakeWorld(); var pet = new Pet(w, 1, 640, 40);
                var all = new List<Pet> { pet }; var seen = new HashSet<PetState>();
                Action<double> run = secs => { for (double t = 0; t < secs; t += .03) { w.Now += .03; pet.Update(w, .03, all, 0); seen.Add(pet.State); } };
                run(.3); check(pet.State == PetState.Falling, "시작은 비행 하강");
                run(25); check(pet.Y == w.Areas[0].Bottom && pet.State != PetState.Falling, "바닥에 착지");
                pet.Poke(w); check(pet.State == PetState.Squish, "첫 클릭 = 눌리기"); run(1);
                pet.Poke(w); check(pet.State == PetState.TouchCry, "둘째 클릭 = 달달 떨며 울기"); run(3);
                pet.Poke(w); check(pet.State == PetState.Hop, "셋째 클릭 = 폴짝 (" + pet.State + ")"); run(1.5);
                FriendData.Current.Satiety = 50; pet.Feed(w); check(pet.State == PetState.Eating && FriendData.Current.Satiety == 75, "간식 먹기 (" + pet.State + " " + FriendData.Current.Satiety + ")"); run(4);
                pet.BeginDrag(w, pet.X, pet.Y - 20); check(pet.State == PetState.Drag, "드래그 시작");
                for (int i = 0; i < 6; i++) { w.Now += .016; pet.DragTo(w, pet.X + pet.DragPx + 30, pet.Y + pet.DragPy - 4); }
                pet.EndDrag(w); check(pet.State == PetState.Thrown, "빠르게 놓으면 던지기"); run(.8); run(25);
                check(seen.Contains(PetState.Thrown) && pet.State != PetState.Thrown && pet.Y <= w.Areas[0].Bottom + .1, "던진 뒤 둥실 내려와 착지");
                pet.Demonstrate(w, "dizzy"); check(pet.State == PetState.Dizzy && FriendData.Current.Discoveries.Contains("dizzy"), "어질어질"); run(5);
                pet.SetState(PetState.Pause, w); pet.FallAsleep(w); check(pet.State == PetState.Sleep, "낮잠"); pet.WakeUp(w); check(pet.State == PetState.Waking, "깨어남");
                for (int i = 0; i < 3; i++) { pet.BeginDrag(w, pet.X, pet.Y - 20); for (int k = 0; k < 6; k++) { w.Now += .016; pet.DragTo(w, pet.X + pet.DragPx + 30, pet.Y + pet.DragPy - 4); } pet.EndDrag(w); run(.6); }
                PlayOptions.Current.Walls = true; PlayOptions.Current.Ceiling = true;
                check(seen.Contains(PetState.PowerRise) && FriendData.Current.Discoveries.Contains("power"), "기 폭발: 10초 안 3번 던지기");
                run(20); check(pet.State == PetState.Ceiling || seen.Contains(PetState.Ceiling), "변신하며 천장까지 올라감");

                // 크립토나이트: 수퍼맨만 5번 연타하면 힘이 빠지고, 시간이 지나면 일어선다
                PlayOptions.Current = new PlayOptions { Escape = false, Idle = false, Windows = false, Walls = false, Ceiling = false };
                var wk = new FakeWorld(); var supe = new Pet(wk, 0, 640, 720) { Area = wk.Areas[0] }; var bat = new Pet(wk, 1, 300, 720) { Area = wk.Areas[0] };
                var allk = new List<Pet> { supe, bat };
                supe.Y = 720; supe.SetState(PetState.Pause, wk); bat.Y = 720; bat.SetState(PetState.Pause, wk);
                for (int i = 0; i < 5; i++) { wk.Now += .3; bat.Poke(wk); }
                check(bat.State != PetState.Kryptonite, "크립토나이트: 수퍼맨이 아니면 반응 없음");
                for (int i = 0; i < 4; i++) { wk.Now += .3; supe.Poke(wk); }
                check(supe.State != PetState.Kryptonite, "크립토나이트: 4번은 아직");
                wk.Now += .3; supe.Poke(wk);
                check(supe.State == PetState.Kryptonite && FriendData.Current.Discoveries.Contains("krypto"), "크립토나이트: 5번 연타");
                bool krypRendered = false;
                for (double t = 0; t < 9.5; t += .03)
                {
                    wk.Now += .03; supe.Update(wk, .03, allk, 0);
                    if (supe.State == PetState.Kryptonite && ((int)(t * 100) % 70) == 0) { PointF o2; using (Bitmap kb = Pet.Render(supe.GetLook(wk.Now), out o2)) krypRendered |= kb.Width > 10; }
                }
                check(krypRendered && supe.State != PetState.Kryptonite, "크립토나이트: 그려지고 끝나면 일어섬 (" + supe.State + ")");

                // 오래 돌려서 벽·천장·쉬기가 모두 나오는지
                PlayOptions.Current = new PlayOptions { Escape = false, Idle = true };
                var w2 = new FakeWorld(); w2.Rng = new Random(3); var p2 = new Pet(w2, 2, 300, 40); var all2 = new List<Pet> { p2 }; var seen2 = new HashSet<PetState>();
                for (double t = 0; t < 600; t += .03) { w2.Now += .03; p2.Update(w2, .03, all2, 0); seen2.Add(p2.State); }
                check(seen2.Contains(PetState.Walk) && seen2.Contains(PetState.Pause) && seen2.Contains(PetState.Wall) && seen2.Contains(PetState.Ceiling) && seen2.Contains(PetState.Resting), "10분 시뮬레이션: 걷기·쉬기·벽·천장·앉기 모두 등장");
                check(p2.X >= -50 && p2.X <= 1330 && p2.Y >= -50 && p2.Y <= 770, "화면 밖으로 나가지 않음");

                // 위치 고정
                PlayOptions.Current = new PlayOptions { PositionFixed = true };
                var w3 = new FakeWorld(); var p3 = new Pet(w3, 0, 500, 300); var all3 = new List<Pet> { p3 };
                double px = p3.X, py = p3.Y; for (double t = 0; t < 20; t += .03) { w3.Now += .03; p3.Update(w3, .03, all3, 0); }
                check(Math.Abs(p3.X - px) < 1 && Math.Abs(p3.Y - py) < 1 || p3.Y <= w3.Areas[0].Bottom, "위치 고정 중에는 자동 이동 없음");

                // 포만감 저하(10 이하 · 5 이하): 속도 감소 · 벽타기 차단 · 제자리 멈춤 · 슬픈 동작
                {
                    PlayOptions.Current = new PlayOptions { Escape = false, Idle = false, Windows = false, Walls = true, Ceiling = true };
                    Func<double, double> measureWalk = sat =>
                    {
                        FriendData.Current.Satiety = sat;
                        var wh = new FakeWorld(); var hp = new Pet(wh, 0, 500, 720) { Area = wh.Areas[0], Dir = 1 };
                        hp.SetState(PetState.Walk, wh);
                        double startX = hp.X;
                        for (double t = 0; t < 1.2; t += .03) { wh.Now += .03; hp.Update(wh, .03, new List<Pet> { hp }, 0); }
                        return Math.Abs(hp.X - startX);
                    };
                    double dxFull = measureWalk(100), dxHungry = measureWalk(15), dxVery = measureWalk(8), dxStarve = measureWalk(4);
                    check(dxHungry < dxFull * .82 && dxVery < dxHungry * .65 && dxStarve < .01,
                        string.Format("포만감 단계별 이동 속도 감소 (100:{0:F1} > 15:{1:F1} > 8:{2:F1} > 4:{3:F1})", dxFull, dxHungry, dxVery, dxStarve));

                    // 10 이하: 벽으로 걸어가도 벽타기 불가 + 이미 벽에 붙어 있었으면 즉시 떨어짐
                    FriendData.Current.Satiety = 10;
                    var wWall = new FakeWorld(); var wp = new Pet(wWall, 2, 1240, 720) { Area = wWall.Areas[0], Dir = 1 };
                    wp.SetState(PetState.Walk, wWall);
                    var seenWall = new HashSet<PetState>();
                    for (double t = 0; t < 10; t += .03) { wWall.Now += .03; wp.Update(wWall, .03, new List<Pet> { wp }, 0); seenWall.Add(wp.State); }
                    check(!seenWall.Contains(PetState.Wall) && !seenWall.Contains(PetState.Ceiling), "포만감 10 이하: 벽타기·천장타기 안 함");

                    FriendData.Current.Satiety = 100;
                    wp.Demonstrate(wWall, "wall");
                    check(wp.State == PetState.Wall, "포만감 100에서는 벽타기 가능");
                    FriendData.Current.Satiety = 9;
                    wWall.Now += .03; wp.Update(wWall, .03, new List<Pet> { wp }, 0);
                    check(wp.State == PetState.Falling, "벽타기 중 포만감 10 이하가 되면 힘이 빠져 떨어짐");

                    // 5 이하: 한 곳에 멈춘 채 힘없고 서글픈 동작 순환 + 5명 모두 정상 렌더링 + 간식 먹으면 다시 걸음
                    PlayOptions.Current = new PlayOptions { Escape = false, Idle = true, Windows = false, Walls = true, Ceiling = true };
                    FriendData.Current.Satiety = 3;
                    var wStarve = new FakeWorld(); var sp = new Pet(wStarve, 1, 600, 720) { Area = wStarve.Areas[0] };
                    sp.SetState(PetState.Pause, wStarve);
                    double sx0 = sp.X; var starveStates = new HashSet<PetState>();
                    for (double t = 0; t < 20; t += .03) { wStarve.Now += .03; sp.Update(wStarve, .03, new List<Pet> { sp }, 0); starveStates.Add(sp.State); }
                    check(Math.Abs(sp.X - sx0) < .01 && !starveStates.Contains(PetState.Walk) && starveStates.Contains(PetState.IdleCry), "포만감 5 이하: 한 곳에 멈춰 힘없는 동작 유지");

                    for (int c = 0; c < CharacterArt.Count; c++)
                    {
                        var rpWeak = new Pet(wStarve, c, 600, 720) { Area = wStarve.Areas[0] };
                        rpWeak.SetState(PetState.IdleCry, wStarve);
                        PointF org; using (Bitmap b = Pet.Render(rpWeak.GetLook(wStarve.Now), out org)) check(b.Width > 20 && b.Height > 20, CharacterArt.Infos[c].Name + " 배고픔 탈진 렌더링");
                    }
                    sp.Feed(wStarve);
                    for (double t = 0; t < 4; t += .03) { wStarve.Now += .03; sp.Update(wStarve, .03, new List<Pet> { sp }, 0); }
                    check(FriendData.Current.Satiety == 28 && sp.State == PetState.Walk, "간식 후 포만감 회복 및 다시 이동");
                    FriendData.Current = new FriendData();
                }

                // 두 마리 충돌 분리
                PlayOptions.Current = new PlayOptions { Escape = false, Idle = false, Windows = false };
                var w4 = new FakeWorld(); var a1 = new Pet(w4, 0, 500, 700) { Area = w4.Areas[0] }; var a2 = new Pet(w4, 1, 505, 700) { Area = w4.Areas[0] };
                a1.SetState(PetState.Pause, w4); a2.SetState(PetState.Pause, w4); a1.Y = a2.Y = 720;
                App.ResolveCompanionContacts(w4, new List<Pet> { a1, a2 }, null);
                check(a2.ContactBody(0).Left - a1.ContactBody(0).Right > -1 || Math.Abs(a1.X - a2.X) > a1.Art.W * .6, "겹친 친구는 서로 떨어짐");

                // 벽에 붙은 친구의 몸통은 X 를 중심으로 하지 않는다: 그 몸통 안쪽에 낀 바닥 친구도 떨어져야 함
                var w5 = new FakeWorld(); var wallPet = new Pet(w5, 0, 5, 700) { Area = w5.Areas[0] }; var floorPet = new Pet(w5, 1, 60, 720) { Area = w5.Areas[0] };
                wallPet.Demonstrate(w5, "wall"); floorPet.SetState(PetState.Pause, w5); floorPet.Y = 720;
                for (double t = 0; t < 3 && wallPet.State != PetState.Wall; t += .03) { w5.Now += .03; wallPet.Update(w5, .03, new List<Pet> { wallPet }, 0); }
                if (wallPet.State == PetState.Wall)
                {
                    wallPet.Y = 690; floorPet.X = wallPet.X + 40;
                    App.ResolveCompanionContacts(w5, new List<Pet> { wallPet, floorPet }, null);
                    check(!wallPet.ContactBody(w5.Now).IntersectsWith(floorPet.ContactBody(w5.Now)), "벽에 붙은 친구 몸통 안에 낀 친구도 떨어짐");
                }

                // 여러 마리가 오래 돌아다니며 마주쳐도 서로 엉킨 채 오래 머물지 않음
                {
                    double worst = 0; string worstInfo = "";
                    foreach (int mode in new[] { 0, 1, 2 })
                        foreach (int count in new[] { 2, 3, 5 })
                            foreach (int seed in new[] { 1, 2, 3, 4 })
                            {
                                PlayOptions.Current = new PlayOptions { Escape = mode == 1, Follow = mode == 2, Idle = true, Windows = false, Walls = seed % 2 == 0 };
                                int mouseX = 200 + seed * 170; double clock = 0;
                                DesktopSurfaces.TestMouse = mode == 0 ? (Func<Point>)(() => new Point(-9999, -9999)) : () => new Point(mouseX + (int)(Math.Sin(clock / 9) * (seed % 2 == 0 ? 250 : 0)), 690);
                                var wt = new FakeWorld { Rng = new Random(seed) };
                                if (count == 5) wt.Areas[0] = new Rectangle(0, 0, 900, 720);
                                var tall = new List<Pet>();
                                for (int k = 0; k < count; k++) tall.Add(new Pet(wt, k % CharacterArt.Count, 100 + k * 130, 720) { Area = wt.Areas[0] });
                                var stuck = new double[count, count];
                                for (double t = 0; t < 1800; t += .03)
                                {
                                    wt.Now += .03; clock = wt.Now;
                                    var old = tall.ToDictionary(q => q, q => new PointF((float)q.X, (float)q.Y));
                                    var before = tall.Select(q => q.X).ToArray();
                                    foreach (Pet q in tall) q.Update(wt, .03, tall, 0);
                                    App.ResolveCompanionContacts(wt, tall, old);
                                    for (int i = 0; i < count; i++)
                                        for (int j2 = i + 1; j2 < count; j2++)
                                        {
                                            Pet pa = tall[i], pb = tall[j2];
                                            bool skip = pa.IsGhost(wt.Now) || pb.IsGhost(wt.Now) || pa.State == PetState.Falling || pb.State == PetState.Falling || pa.State == PetState.Wall || pb.State == PetState.Wall || pa.State == PetState.WindowClimb || pb.State == PetState.WindowClimb;
                                            double near = (pa.Art.W + pb.Art.W) * .85 * .5 * 1.3 + 6;
                                            bool moved = Math.Abs(pa.X - before[i]) + Math.Abs(pb.X - before[j2]) > .3;
                                            bool ovl = pa.ContactBody(wt.Now).IntersectsWith(pb.ContactBody(wt.Now));
                                            bool jam = !skip && Math.Abs(pa.X - pb.X) < near && (ovl || (!moved && (pa.State == PetState.Walk || pb.State == PetState.Walk)));
                                            stuck[i, j2] = jam ? stuck[i, j2] + .03 : 0;
                                            if (stuck[i, j2] > worst) { worst = stuck[i, j2]; worstInfo = "모드" + mode + " " + count + "마리 seed " + seed + " [" + pa.Art.Id + "(" + pa.State + ")+" + pb.Art.Id + "(" + pb.State + ")]"; }
                                        }
                                }
                            }
                    DesktopSurfaces.TestMouse = null;
                    log.AppendLine("  [정보] 붙은 채 정지 최장 " + worst.ToString("F1") + "초 " + worstInfo);
                    check(worst < 4, "여러 마리가 붙은 채 오래 정지하지 않음 (최장 " + worst.ToString("F1") + "초)");
                }

                // ── AI 대화 & EXAONE ──
                var j = Json.Parse("{\"message\":{\"role\":\"assistant\",\"content\":\"[기쁨] \\\"안녕\\\"\\n\\u0041\"},\"done\":false,\"n\":-1.5e2}");
                check(Json.Str(Json.Get(j, "message"), "content") == "[기쁨] \"안녕\"\nA" && !Json.Flag(j, "done") && (double)Json.Get(j, "n") == -150, "JSON 읽기(이스케이프·숫자)");
                check(Json.Parse("{깨진") == null, "깨진 JSON 은 null");
                check(Json.Quote("a\"b\n") == "\"a\\\"b\\n\"", "JSON 문자열 만들기");
                var rp = new ReplyParser(ChatMode.All[0].MaxSentences, ChatMode.All[0].MaxChars); bool c1 = rp.Feed("["); rp.Feed("기"); rp.Feed("쁨"); bool c2 = rp.Feed("] 안녕"); rp.Feed("! 반가워. 또 왔네! 네 번째 문장");
                check(!c1 && c2 && rp.Mood == ChatMood.Joy && rp.Text == "안녕! 반가워. 또 왔네!", "감정 태그가 토큰으로 쪼개져 와도 읽고, 세 문장까지만 남김: " + rp.Text);
                check(ReplyParser.Clean("**좋아** 😀 (웃으며) 그래\n수퍼맨: 둘째 줄") == "좋아 그래", "마크다운·이모지·괄호 지문·둘째 줄 제거");
                var rq = new ReplyParser(); rq.Feed("힘들었겠다 ㅠㅠ");
                check(rq.Mood == ChatMood.Sad && !rq.HasTag, "태그가 없으면 글에서 감정을 짐작");
                ChatOptions.Current = new ChatOptions { Provider = "exaone", ExaoneKey = "test-key-123", ExaoneEffort = "high" }; ChatOptions.Current.Save(); ChatOptions.Load();
                check(ChatOptions.Current.IsExaone && ChatOptions.Current.ExaoneKey == "test-key-123" && ChatOptions.Current.ExaoneEffort == "high" && !File.ReadAllText(ChatOptions.FilePath).Contains("test-key-123"), "EXAONE 설정 저장·불러오기(키 암호화)");
                string exBody = ExaoneClient.Body(ChatOptions.Current, new List<ChatMessage> { new ChatMessage("system", "s"), new ChatMessage("user", "안녕") }, true, 170, .85);
                object exJ = Json.Parse(exBody);
                check(Json.Str(exJ, "model") == "k-exaone_v2.5" && Json.Flag(exJ, "stream") && (double)Json.Get(exJ, "max_tokens") > 170 && Json.Str(exJ, "reasoning_effort") == "high" && ((List<object>)Json.Get(exJ, "messages")).Count == 2, "EXAONE 요청 본문");
                var lc = new ExaoneClient.LineCutter(); string cutText = lc.Feed("\n\n") + lc.Feed("[기쁨] 안") + lc.Feed("녕!\n수퍼맨: 또") + lc.Feed("더");
                check(cutText == "[기쁨] 안녕!" && lc.Ended, "EXAONE 답: 앞 빈 줄 건너뛰고 첫 줄에서 끊기: " + cutText);
                check(ExaoneClient.Probe(new ChatOptions { Provider = "exaone", ExaoneKey = "" }) == ProbeResult.NoKey || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("EXAONE_API_KEY")), "EXAONE 키가 없으면 NoKey");
                ChatOptions.Current = new ChatOptions();

                OvertimeState.SelfTests(check);
                OvertimeUiTests.Run(check);
                ApiSelfTests(check);
            }
            catch (Exception e) { log.AppendLine("EXCEPTION " + e); failures++; }
            log.AppendLine(failures == 0 ? "ALL PASSED" : failures + " FAILED");
            File.WriteAllText(Path.Combine(dir, "selftest_result.txt"), log.ToString(), new UTF8Encoding(true));
            Console.Write(log.ToString());
            return failures == 0 ? 0 : 1;
        }

        private static void ApiSelfTests(Action<bool, string> check)
        {
            check(ApiServer.IsAllowedOrigin(null) && ApiServer.IsAllowedOrigin(""), "API Origin: 데스크톱 앱(Origin 없음) 허용");
            check(ApiServer.IsAllowedOrigin("chrome-extension://abcdefghijklmnop") && ApiServer.IsAllowedOrigin("edge-extension://123456"), "API Origin: 브라우저 확장(*.sAIde) 허용");
            check(!ApiServer.IsAllowedOrigin("https://evil.example.com") && !ApiServer.IsAllowedOrigin("http://localhost:3000"), "API Origin: 일반 웹페이지 차단");

            var app = new App(true);
            app.Notes = new NoteManager(app);
            app.Notes.Start(false);
            app.Settings.ApiPort = 19485;
            app.StartApi();
            bool pump = true;
            var pumper = new Thread(delegate () { while (pump) { try { app.Api.Tick(); } catch { } Thread.Sleep(5); } }) { IsBackground = true };
            pumper.Start();
            try
            {
                check(app.Api != null && app.Api.IsRunning && app.Api.Port >= 1024, "API 서버 시작 (127.0.0.1:" + (app.Api != null ? app.Api.Port : 0) + ")");
                string baseUrl = "http://127.0.0.1:" + app.Api.Port;

                Func<string, string, string, string, KeyValuePair<int, string>> call = (method, path, body, origin) =>
                {
                    var req = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(baseUrl + path);
                    req.Method = method; req.Timeout = 5000; req.Proxy = null;
                    if (!string.IsNullOrEmpty(origin)) req.Headers["Origin"] = origin;
                    if (body != null)
                    {
                        byte[] b = new UTF8Encoding(false).GetBytes(body);
                        req.ContentType = "application/json; charset=utf-8"; req.ContentLength = b.Length;
                        using (var s = req.GetRequestStream()) s.Write(b, 0, b.Length);
                    }
                    try
                    {
                        using (var resp = (System.Net.HttpWebResponse)req.GetResponse())
                        using (var sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                            return new KeyValuePair<int, string>((int)resp.StatusCode, sr.ReadToEnd());
                    }
                    catch (System.Net.WebException wex)
                    {
                        var r = wex.Response as System.Net.HttpWebResponse;
                        if (r != null) using (var sr = new StreamReader(r.GetResponseStream(), Encoding.UTF8)) return new KeyValuePair<int, string>((int)r.StatusCode, sr.ReadToEnd());
                        throw;
                    }
                };

                var h = call("GET", "/api/health", null, null);
                check(h.Key == 200 && Json.Flag(Json.Parse(h.Value), "ok"), "GET /api/health 응답 확인");

                var forbidden = call("POST", "/api/alert", "{\"message\":\"침입\"}", "https://malicious.example.com");
                check(forbidden.Key == 403, "일반 웹 Origin 403 차단 확인");

                var extOk = call("POST", "/api/alert", "{\"message\":\"공문 도착!\",\"style\":\"bubble\",\"seconds\":5}", "chrome-extension://onnara-saide-ext");
                object extJson = Json.Parse(extOk.Value);
                string alertId = Json.Str(extJson, "alertId");
                check(extOk.Key == 200 && Json.Flag(extJson, "ok") && !string.IsNullOrEmpty(alertId), "POST /api/alert (브라우저 확장 Origin) 성공");

                var alertGet = call("GET", "/api/alert/" + alertId, null, null);
                check(alertGet.Key == 200 && Json.Str(Json.Parse(alertGet.Value), "message") == "공문 도착!", "GET /api/alert/{id} 상태 조회");

                var noteCreate = call("POST", "/api/notes", "{\"content\":\"# 회의 메모\\n- [ ] 안건 정리 #업무\",\"color\":\"yellow\",\"open\":false}", null);
                object ncJson = Json.Parse(noteCreate.Value);
                string noteId = Json.Str(Json.Get(ncJson, "note"), "id");
                check(noteCreate.Key == 201 && !string.IsNullOrEmpty(noteId), "POST /api/notes 메모 생성 성공");

                var noteList = call("GET", "/api/notes?tag=업무", null, null);
                check(noteList.Key == 200 && noteList.Value.Contains("회의 메모"), "GET /api/notes 태그 검색 확인");

                var notePatch = call("PATCH", "/api/notes/" + noteId, "{\"append\":\"- [ ] 추가 항목\"}", null);
                check(notePatch.Key == 200 && notePatch.Value.Contains("추가 항목"), "PATCH /api/notes/{id} 본문 덧붙이기 확인");

                var noteRemind = call("POST", "/api/notes/" + noteId + "/remind", "{\"minutes\":30}", null);
                check(noteRemind.Key == 200 && Json.Flag(Json.Parse(noteRemind.Value), "ok"), "POST /api/notes/{id}/remind 알림 설정 확인");

                var noteDel = call("DELETE", "/api/notes/" + noteId, null, null);
                check(noteDel.Key == 200 && Json.Flag(Json.Parse(noteDel.Value), "ok"), "DELETE /api/notes/{id} 메모 삭제 확인");

                var actList = call("GET", "/api/pet/actions", null, null);
                check(actList.Key == 200 && actList.Value.Contains("sit") && actList.Value.Contains("sig0"), "GET /api/pet/actions 동작 목록 조회");

                var actRun = call("POST", "/api/pet/action", "{\"action\":\"sit\",\"say\":\"결재 완료!\"}", null);
                check(actRun.Key == 200 && Json.Flag(Json.Parse(actRun.Value), "ok") && app.LiveWindows[0].Pet.Bubble == "결재 완료!", "POST /api/pet/action 캐릭터 동작·말풍선 유도 확인");
            }
            finally
            {
                pump = false;
                try { pumper.Join(200); } catch { }
                try { if (app.Api != null) app.Api.Dispose(); } catch { }
                try { if (app.Notes != null) app.Notes.Suspend(); } catch { }
                foreach (var w in app.LiveWindows.ToArray()) { try { w.Close(); w.Dispose(); } catch { } }
            }
        }
    }
}
