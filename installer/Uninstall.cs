using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace BirthlyUninstaller
{
    static class Program
    {
        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [STAThread]
        static void Main(string[] args)
        {
            try { SetProcessDPIAware(); } catch { }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            if (args != null && args.Length >= 2 && args[0] == "--preview")
            {
                using (GlassUninstallForm f = new GlassUninstallForm(true, false))
                {
                    f.ExportPreview(args[1]);
                }
                return;
            }
            if (args != null && args.Length >= 2 && args[0] == "--preview-basic")
            {
                using (GlassUninstallForm f = new GlassUninstallForm(true, true))
                {
                    f.ExportPreview(args[1]);
                }
                return;
            }
            Application.Run(new GlassUninstallForm(false, null));
        }
    }

    public class GlassUninstallForm : Form
    {
        private const int WS_EX_LAYERED = 0x00080000;
        private const byte AC_SRC_OVER = 0x00;
        private const byte AC_SRC_ALPHA = 0x01;
        private const int ULW_ALPHA = 0x02;

        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_NOACTIVATE = 0x0010;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct BLENDFUNCTION { public byte BlendOp, BlendFlags, SourceConstantAlpha, AlphaFormat; }
        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int X, Y; public POINT(int x, int y) { X = x; Y = y; } }
        [StructLayout(LayoutKind.Sequential)]
        private struct SIZE { public int Width, Height; public SIZE(int w, int h) { Width = w; Height = h; } }
        [StructLayout(LayoutKind.Sequential)]
        private struct BITMAPINFOHEADER
        {
            public uint biSize; public int biWidth, biHeight; public ushort biPlanes, biBitCount;
            public uint biCompression, biSizeImage; public int biXPelsPerMeter, biYPelsPerMeter;
            public uint biClrUsed, biClrImportant;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct BITMAPINFO { public BITMAPINFOHEADER bmiHeader; public uint bmiColors; }

        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst, ref POINT pptDst, ref SIZE psize, IntPtr hdcSrc, ref POINT pptSrc, uint crKey, ref BLENDFUNCTION pblend, uint dwFlags);
        [DllImport("user32.dll", ExactSpelling = true)] private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        [DllImport("user32.dll", ExactSpelling = true)] private static extern IntPtr GetDC(IntPtr hWnd);
        [DllImport("user32.dll", ExactSpelling = true)] private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
        [DllImport("gdi32.dll", ExactSpelling = true)] private static extern IntPtr CreateCompatibleDC(IntPtr hDC);
        [DllImport("gdi32.dll", ExactSpelling = true)] private static extern bool DeleteDC(IntPtr hdc);
        [DllImport("gdi32.dll", ExactSpelling = true)] private static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);
        [DllImport("gdi32.dll", ExactSpelling = true)] private static extern bool DeleteObject(IntPtr hObject);
        [DllImport("gdi32.dll", ExactSpelling = true)] private static extern IntPtr CreateDIBSection(IntPtr hdc, ref BITMAPINFO pbmi, uint iUsage, out IntPtr ppvBits, IntPtr hSection, uint dwOffset);
        [DllImport("user32.dll")] private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll")] private static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, uint nFlags);

        private int winW = 560;
        private int winH = 385;
        private float cornerRadius = 36f;
        private int edgeMargin = 4;
        private int surfW, surfH;

        private int deskW, deskH;
        private Rectangle vScreen;
        private int[] blurredDesk;
        private int[] sharpDesk;
        private int[] surfaceBuf;
        private int[] cachedBackdropBuf;
        private int lastBgX = -99999, lastBgY = -99999, lastBgTb256 = -1;

        private Bitmap reusableUiBmp;
        private int[] reusableUiPx;
        private IntPtr persistentScreenDc = IntPtr.Zero;
        private IntPtr persistentMemDc = IntPtr.Zero;
        private IntPtr persistentDib = IntPtr.Zero;
        private IntPtr persistentOldBmp = IntPtr.Zero;
        private IntPtr persistentBitsPtr = IntPtr.Zero;

        private struct PixelLUT
        {
            public int idx, px, py, dispX, dispY, caX, caY;
            public int rimW256, specW256, alpha255;
            public bool isEdge;
        }
        private PixelLUT[][] lutRows;

        private struct HitZone { public string Id; public RectangleF Bounds; }
        private List<HitZone> hitZones = new List<HitZone>();
        private string hoveredId = null;
        private Dictionary<string, float> hoverProgress = new Dictionary<string, float>();
        private float themeBlend = 0f;
        private float popInProgress = 1f;
        private PointF localCursorPos = new PointF(-1000f, -1000f);
        private bool isCursorInside = false;
        private System.Windows.Forms.Timer animTimer;
        private Stopwatch animSw = Stopwatch.StartNew();
        private float lastAnimSec = 0f;

        private static readonly string[] AllHoverIds = new string[] { "close", "theme_glass", "theme_basic", "opt_data", "cancel", "uninstall" };

        private Image logoImg;
        private PrivateFontCollection pfc = new PrivateFontCollection();
        private FontFamily melonFamily;
        private readonly Dictionary<int, Font> fontCache = new Dictionary<int, Font>();
        private readonly SolidBrush sharedTextBr = new SolidBrush(Color.White);
        private readonly SolidBrush sharedShadowBr = new SolidBrush(Color.FromArgb(200, 6, 9, 16));

        // Uninstall state + Saved Theme Auto-Detection
        private string installDir;
        private bool useBasicTheme = false;
        private bool darkMode = false;
        private string tintVariant = "Clear";
        private bool removeUserData = false;
        private bool isUninstalling = false;
        private bool uninstallDone = false;
        private float progressVal = 0f;
        private string statusText = "Ready to remove Birthly, shortcuts & auto-start entry.";

        // Drag state
        private bool isDragging = false;
        private Point dragStartCursor;
        private Point dragStartWin;
        private int curX, curY;

        private readonly StringFormat sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap, Trimming = StringTrimming.EllipsisCharacter };
        private readonly StringFormat sfNear = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap, Trimming = StringTrimming.EllipsisCharacter };

        public GlassUninstallForm(bool previewMode, bool? forceBasicTheme)
        {
            installDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\');
            LoadSavedUserTheme();
            if (forceBasicTheme.HasValue)
            {
                useBasicTheme = forceBasicTheme.Value;
            }

            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.ShowInTaskbar = !previewMode;
            this.Text = "Uninstall Birthly";
            this.themeBlend = useBasicTheme ? 1f : 0f;
            this.popInProgress = previewMode ? 1f : 0f;
            try { this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            surfW = winW + edgeMargin * 2;
            surfH = winH + edgeMargin * 2;
            this.Size = new Size(surfW, surfH);

            Rectangle scr = Screen.PrimaryScreen.WorkingArea;
            curX = scr.Left + (scr.Width - surfW) / 2;
            curY = scr.Top + (scr.Height - surfH) / 2;
            this.Location = new Point(curX, curY);

            LoadEmbeddedAssets();
            vScreen = SystemInformation.VirtualScreen;
            deskW = vScreen.Width;
            deskH = vScreen.Height;
            AllocatePersistentDIB();
            CaptureDesktopBackdrop();
            PrecomputeOpticalLUT();

            if (!previewMode)
            {
                animTimer = new System.Windows.Forms.Timer { Interval = 12 };
                animTimer.Tick += OnAnimTick;
                animTimer.Start();
            }
        }

        private void LoadSavedUserTheme()
        {
            try
            {
                string cfgPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Birthly", "birthdays_config.json");
                if (File.Exists(cfgPath))
                {
                    string json = File.ReadAllText(cfgPath);
                    Match mBasic = Regex.Match(json, "\"UseBasicTheme\"\\s*:\\s*(true|false)", RegexOptions.IgnoreCase);
                    if (mBasic.Success) useBasicTheme = mBasic.Groups[1].Value.Equals("true", StringComparison.OrdinalIgnoreCase);

                    Match mDark = Regex.Match(json, "\"DarkMode\"\\s*:\\s*(true|false)", RegexOptions.IgnoreCase);
                    if (mDark.Success) darkMode = mDark.Groups[1].Value.Equals("true", StringComparison.OrdinalIgnoreCase);

                    Match mTint = Regex.Match(json, "\"TintVariant\"\\s*:\\s*\"([^\"]+)\"", RegexOptions.IgnoreCase);
                    if (mTint.Success) tintVariant = mTint.Groups[1].Value;
                }
            }
            catch { }
        }

        private void GetOpticalTintRgb(out int tr, out int tg, out int tb, out int extraWeight256)
        {
            if (tintVariant == "Ice") { tr = 155; tg = 215; tb = 255; extraWeight256 = 46; return; }
            if (tintVariant == "Rose") { tr = 255; tg = 165; tb = 200; extraWeight256 = 46; return; }
            if (tintVariant == "Mint") { tr = 150; tg = 240; tb = 205; extraWeight256 = 44; return; }
            if (tintVariant == "Amber") { tr = 255; tg = 208; tb = 135; extraWeight256 = 46; return; }
            if (tintVariant == "Violet") { tr = 195; tg = 165; tb = 255; extraWeight256 = 48; return; }
            if (tintVariant == "Obsidian") { tr = 18; tg = 22; tb = 32; extraWeight256 = 68; return; }
            tr = 245; tg = 250; tb = 255; extraWeight256 = 0;
        }

        private void OnAnimTick(object sender, EventArgs e)
        {
            float now = (float)animSw.Elapsed.TotalSeconds;
            float dt = Math.Max(0.004f, Math.Min(0.05f, now - lastAnimSec));
            lastAnimSec = now;

            bool needsRedraw = false;
            if (popInProgress < 1f)
            {
                popInProgress = Math.Min(1f, popInProgress + dt * 5.2f);
                needsRedraw = true;
            }

            float targetTheme = useBasicTheme ? 1f : 0f;
            if (Math.Abs(themeBlend - targetTheme) > 0.002f)
            {
                themeBlend += (targetTheme - themeBlend) * Math.Min(1f, dt * 14f);
                if (Math.Abs(themeBlend - targetTheme) <= 0.003f) themeBlend = targetTheme;
                needsRedraw = true;
            }

            for (int i = 0; i < AllHoverIds.Length; i++)
            {
                string id = AllHoverIds[i];
                float targetH = (hoveredId == id) ? 1f : 0f;
                float curH = 0f;
                hoverProgress.TryGetValue(id, out curH);
                if (Math.Abs(curH - targetH) > 0.004f)
                {
                    curH += (targetH - curH) * Math.Min(1f, dt * 16f);
                    if (curH < 0.004f) curH = 0f;
                    else if (curH > 0.996f) curH = 1f;
                    hoverProgress[id] = curH;
                    needsRedraw = true;
                }
            }

            if (hoveredId != null) needsRedraw = true;

            if (needsRedraw)
            {
                RenderFrame(true);
            }
            else if (!isCursorInside && popInProgress >= 0.999f && Math.Abs(themeBlend - targetTheme) <= 0.003f)
            {
                animTimer.Stop();
            }
        }

        private void WakeAnimTimer()
        {
            if (animTimer != null && !animTimer.Enabled)
            {
                lastAnimSec = (float)animSw.Elapsed.TotalSeconds;
                animTimer.Start();
            }
        }

        private float GetHov(string id)
        {
            float v = 0f;
            if (hoverProgress.TryGetValue(id, out v)) return v;
            return hoveredId == id ? 1f : 0f;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= WS_EX_LAYERED;
                return cp;
            }
        }

        private void AllocatePersistentDIB()
        {
            reusableUiBmp = new Bitmap(surfW, surfH, PixelFormat.Format32bppPArgb);
            reusableUiPx = new int[surfW * surfH];

            persistentScreenDc = GetDC(IntPtr.Zero);
            persistentMemDc = CreateCompatibleDC(persistentScreenDc);

            BITMAPINFO bmi = new BITMAPINFO();
            bmi.bmiHeader.biSize = (uint)Marshal.SizeOf(typeof(BITMAPINFOHEADER));
            bmi.bmiHeader.biWidth = surfW;
            bmi.bmiHeader.biHeight = -surfH;
            bmi.bmiHeader.biPlanes = 1;
            bmi.bmiHeader.biBitCount = 32;
            bmi.bmiHeader.biCompression = 0;

            persistentDib = CreateDIBSection(persistentScreenDc, ref bmi, 0, out persistentBitsPtr, IntPtr.Zero, 0);
            if (persistentDib != IntPtr.Zero)
            {
                persistentOldBmp = SelectObject(persistentMemDc, persistentDib);
            }
        }

        private void FreePersistentDIB()
        {
            if (persistentDib != IntPtr.Zero)
            {
                SelectObject(persistentMemDc, persistentOldBmp);
                DeleteObject(persistentDib);
                persistentDib = IntPtr.Zero;
                persistentBitsPtr = IntPtr.Zero;
            }
            if (persistentMemDc != IntPtr.Zero)
            {
                DeleteDC(persistentMemDc);
                persistentMemDc = IntPtr.Zero;
            }
            if (persistentScreenDc != IntPtr.Zero)
            {
                ReleaseDC(IntPtr.Zero, persistentScreenDc);
                persistentScreenDc = IntPtr.Zero;
            }
        }

        private void LoadEmbeddedAssets()
        {
            try
            {
                Assembly asm = Assembly.GetExecutingAssembly();
                using (Stream ls = asm.GetManifestResourceStream("BirthlyLogo.png"))
                {
                    if (ls != null) logoImg = Image.FromStream(ls);
                }
                using (Stream fs = asm.GetManifestResourceStream("MelonSquishy.otf"))
                {
                    if (fs != null)
                    {
                        byte[] data = new byte[fs.Length];
                        fs.Read(data, 0, data.Length);
                        IntPtr ptr = Marshal.AllocCoTaskMem(data.Length);
                        Marshal.Copy(data, 0, ptr, data.Length);
                        pfc.AddMemoryFont(ptr, data.Length);
                        Marshal.FreeCoTaskMem(ptr);
                        if (pfc.Families.Length > 0) melonFamily = pfc.Families[0];
                    }
                }
            }
            catch { }
        }

        private Font GetFont(float px, FontStyle style, bool allowMelon = true)
        {
            bool useMelon = !useBasicTheme && allowMelon && melonFamily != null;
            int key = ((int)(px * 10f) & 0xFFFF) | (((int)style & 0xF) << 16) | (useMelon ? (1 << 24) : 0);
            Font cached;
            if (fontCache.TryGetValue(key, out cached) && cached != null) return cached;
            Font created;
            if (useMelon)
            {
                try
                {
                    created = melonFamily.IsStyleAvailable(style)
                        ? new Font(melonFamily, px, style, GraphicsUnit.Pixel)
                        : new Font(melonFamily, px, FontStyle.Regular, GraphicsUnit.Pixel);
                }
                catch { created = new Font("Segoe UI", px, style, GraphicsUnit.Pixel); }
            }
            else
            {
                created = new Font("Segoe UI", px, style, GraphicsUnit.Pixel);
            }
            fontCache[key] = created;
            return created;
        }

        private void DrawCrispString(Graphics g, string text, Font f, RectangleF r, StringFormat sf, Color col)
        {
            if (string.IsNullOrEmpty(text)) return;
            int shA = (col.A * 200) / 255;
            RectangleF shR = new RectangleF(r.X + 1.0f, r.Y + 1.3f, r.Width, r.Height);
            sharedShadowBr.Color = Color.FromArgb(ClampInt(shA, 0, 255), 6, 9, 16);
            g.DrawString(text, f, sharedShadowBr, shR, sf);
            sharedTextBr.Color = col;
            g.DrawString(text, f, sharedTextBr, r, sf);
        }

        private void DrawCrispString(Graphics g, string text, Font f, float x, float y, Color col)
        {
            if (string.IsNullOrEmpty(text)) return;
            int shA = (col.A * 200) / 255;
            sharedShadowBr.Color = Color.FromArgb(ClampInt(shA, 0, 255), 6, 9, 16);
            g.DrawString(text, f, sharedShadowBr, x + 1.0f, y + 1.3f);
            sharedTextBr.Color = col;
            g.DrawString(text, f, sharedTextBr, x, y);
        }

        private void CaptureDesktopBackdrop()
        {
            sharpDesk = new int[deskW * deskH];
            blurredDesk = new int[deskW * deskH];
            bool ok = false;

            try
            {
                IntPtr progman = FindWindow("Progman", null);
                if (progman != IntPtr.Zero)
                {
                    using (Bitmap bmp = new Bitmap(deskW, deskH, PixelFormat.Format32bppArgb))
                    {
                        using (Graphics g = Graphics.FromImage(bmp))
                        {
                            IntPtr hdc = g.GetHdc();
                            ok = PrintWindow(progman, hdc, 2);
                            g.ReleaseHdc(hdc);
                        }
                        if (ok)
                        {
                            BitmapData bd = bmp.LockBits(new Rectangle(0, 0, deskW, deskH), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                            Marshal.Copy(bd.Scan0, sharpDesk, 0, sharpDesk.Length);
                            bmp.UnlockBits(bd);
                            int c = sharpDesk[(deskH / 2) * deskW + (deskW / 2)];
                            if (((c >> 16) & 0xFF) + ((c >> 8) & 0xFF) + (c & 0xFF) < 12) ok = false;
                        }
                    }
                }
            }
            catch { ok = false; }

            if (!ok)
            {
                try
                {
                    string wp = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft\Windows\Themes\TranscodedWallpaper");
                    if (File.Exists(wp))
                    {
                        using (Bitmap bmp = new Bitmap(deskW, deskH, PixelFormat.Format32bppArgb))
                        using (FileStream fs = new FileStream(wp, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        using (Image img = Image.FromStream(fs))
                        {
                            using (Graphics g = Graphics.FromImage(bmp))
                            {
                                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                                float sc = Math.Max((float)deskW / img.Width, (float)deskH / img.Height);
                                int dw = (int)(img.Width * sc), dh = (int)(img.Height * sc);
                                g.DrawImage(img, (deskW - dw) / 2, (deskH - dh) / 2, dw, dh);
                            }
                            BitmapData bd = bmp.LockBits(new Rectangle(0, 0, deskW, deskH), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                            Marshal.Copy(bd.Scan0, sharpDesk, 0, sharpDesk.Length);
                            bmp.UnlockBits(bd);
                            ok = true;
                        }
                    }
                }
                catch { }
            }

            if (!ok)
            {
                for (int y = 0; y < deskH; y++)
                {
                    int r = 38 + (y * 28) / Math.Max(1, deskH);
                    int g = 52 + (y * 36) / Math.Max(1, deskH);
                    int b = 82 + (y * 45) / Math.Max(1, deskH);
                    int col = (0xFF << 24) | (r << 16) | (g << 8) | b;
                    for (int x = 0; x < deskW; x++) sharpDesk[y * deskW + x] = col;
                }
            }

            int hw = Math.Max(1, deskW / 2), hh = Math.Max(1, deskH / 2);
            int[] b1 = new int[hw * hh], b2 = new int[hw * hh];
            Parallel.For(0, hh, hy => {
                int sy = hy * 2, sy1 = Math.Min(deskH - 1, sy + 1);
                for (int hx = 0; hx < hw; hx++)
                {
                    int sx = hx * 2, sx1 = Math.Min(deskW - 1, sx + 1);
                    int c0 = sharpDesk[sy * deskW + sx], c1 = sharpDesk[sy * deskW + sx1];
                    int c2 = sharpDesk[sy1 * deskW + sx], c3 = sharpDesk[sy1 * deskW + sx1];
                    int r = (((c0 >> 16) & 0xFF) + ((c1 >> 16) & 0xFF) + ((c2 >> 16) & 0xFF) + ((c3 >> 16) & 0xFF)) >> 2;
                    int gr = (((c0 >> 8) & 0xFF) + ((c1 >> 8) & 0xFF) + ((c2 >> 8) & 0xFF) + ((c3 >> 8) & 0xFF)) >> 2;
                    int b = ((c0 & 0xFF) + (c1 & 0xFF) + (c2 & 0xFF) + (c3 & 0xFF)) >> 2;
                    b1[hy * hw + hx] = (0xFF << 24) | (r << 16) | (gr << 8) | b;
                }
            });
            for (int p = 0; p < 3; p++)
            {
                BoxBlurH(b1, b2, hw, hh, 4);
                BoxBlurV(b2, b1, hw, hh, 4);
            }
            Parallel.For(0, deskH, y => {
                int hy = Math.Min(hh - 1, y / 2), dRow = y * deskW, hRow = hy * hw;
                for (int x = 0; x < deskW; x++) blurredDesk[dRow + x] = b1[hRow + Math.Min(hw - 1, x / 2)];
            });
        }

        private void BoxBlurH(int[] src, int[] dst, int w, int h, int rad)
        {
            float inv = 1f / (rad * 2 + 1);
            Parallel.For(0, h, y => {
                int row = y * w;
                int fv = src[row];
                int vr = ((fv >> 16) & 0xFF) * (rad + 1), vg = ((fv >> 8) & 0xFF) * (rad + 1), vb = (fv & 0xFF) * (rad + 1);
                for (int j = 0; j < rad; j++) { int c = src[row + Math.Min(w - 1, j)]; vr += (c >> 16) & 0xFF; vg += (c >> 8) & 0xFF; vb += c & 0xFF; }
                for (int x = 0; x < w; x++)
                {
                    int ca = src[row + Math.Min(w - 1, x + rad)];
                    int cs = x - rad - 1 < 0 ? fv : src[row + x - rad - 1];
                    vr += ((ca >> 16) & 0xFF) - ((cs >> 16) & 0xFF);
                    vg += ((ca >> 8) & 0xFF) - ((cs >> 8) & 0xFF);
                    vb += (ca & 0xFF) - (cs & 0xFF);
                    dst[row + x] = (0xFF << 24) | (ClampInt((int)(vr * inv), 0, 255) << 16) | (ClampInt((int)(vg * inv), 0, 255) << 8) | ClampInt((int)(vb * inv), 0, 255);
                }
            });
        }

        private void BoxBlurV(int[] src, int[] dst, int w, int h, int rad)
        {
            float inv = 1f / (rad * 2 + 1);
            Parallel.For(0, w, x => {
                int fv = src[x];
                int vr = ((fv >> 16) & 0xFF) * (rad + 1), vg = ((fv >> 8) & 0xFF) * (rad + 1), vb = (fv & 0xFF) * (rad + 1);
                for (int j = 0; j < rad; j++) { int c = src[Math.Min(h - 1, j) * w + x]; vr += (c >> 16) & 0xFF; vg += (c >> 8) & 0xFF; vb += c & 0xFF; }
                for (int y = 0; y < h; y++)
                {
                    int ca = src[Math.Min(h - 1, y + rad) * w + x];
                    int cs = y - rad - 1 < 0 ? fv : src[(y - rad - 1) * w + x];
                    vr += ((ca >> 16) & 0xFF) - ((cs >> 16) & 0xFF);
                    vg += ((ca >> 8) & 0xFF) - ((cs >> 8) & 0xFF);
                    vb += (ca & 0xFF) - (cs & 0xFF);
                    dst[y * w + x] = (0xFF << 24) | (ClampInt((int)(vr * inv), 0, 255) << 16) | (ClampInt((int)(vg * inv), 0, 255) << 8) | ClampInt((int)(vb * inv), 0, 255);
                }
            });
        }

        private static float Clamp01(float v) { return v < 0f ? 0f : (v > 1f ? 1f : v); }
        private static int ClampInt(int v, int min, int max) { return v < min ? min : (v > max ? max : v); }

        private static float RoundedBoxDist(float px, float py, float hw, float hh, float r)
        {
            float dx = Math.Abs(px) - (hw - r), dy = Math.Abs(py) - (hh - r);
            return (float)Math.Sqrt(Math.Max(dx, 0f) * Math.Max(dx, 0f) + Math.Max(dy, 0f) * Math.Max(dy, 0f)) + Math.Min(Math.Max(dx, dy), 0f) - r;
        }

        private void PrecomputeOpticalLUT()
        {
            surfaceBuf = new int[surfW * surfH];
            lutRows = new PixelLUT[surfH][];
            float cx = surfW * 0.5f, cy = surfH * 0.5f, hw = winW * 0.5f, hh = winH * 0.5f;

            Parallel.For(0, surfH, y => {
                float py = y - cy;
                List<PixelLUT> row = new List<PixelLUT>(surfW);
                for (int x = 0; x < surfW; x++)
                {
                    float px = x - cx;
                    float sdf = RoundedBoxDist(px, py, hw, hh, cornerRadius);
                    if (sdf >= 1.6f) continue;

                    float cov = sdf < -1.4f ? 1f : Clamp01(0.5f - sdf * 0.85f);
                    cov = cov * cov * (3f - 2f * cov);
                    int a255 = (int)(cov * 255f + 0.5f);
                    if (a255 <= 0) continue;

                    float inward = Math.Max(0f, -sdf);
                    float edgeRatio = Clamp01(1f - inward / 36f);
                    float men = edgeRatio * edgeRatio * edgeRatio * (edgeRatio * (edgeRatio * 6f - 15f) + 10f);

                    float gx = px >= 0 ? 1f : -1f, gy = py >= 0 ? 1f : -1f;
                    float dx = Math.Abs(px) - (hw - cornerRadius), dy = Math.Abs(py) - (hh - cornerRadius);
                    float sDx = (float)Math.Log(1.0 + Math.Exp(dx * 0.16f)) / 0.16f;
                    float sDy = (float)Math.Log(1.0 + Math.Exp(dy * 0.16f)) / 0.16f;
                    float gl = (float)Math.Sqrt(sDx * sDx + sDy * sDy) + 1e-6f;
                    gx *= sDx / gl; gy *= sDy / gl;

                    float nx = gx * men * 0.82f, ny = gy * men * 0.82f;
                    float nz = (float)Math.Sqrt(Math.Max(0.05f, 1f - nx * nx - ny * ny));
                    float oneM = 1f - nz;
                    float spec = Clamp01((float)Math.Pow(Math.Max(0f, -nx * 0.45f - ny * 0.50f + nz * 0.74f), 14f) * men * 0.42f + (oneM * oneM * oneM) * 0.25f);

                    row.Add(new PixelLUT {
                        idx = y * surfW + x, px = x, py = y,
                        dispX = (int)Math.Round(-nx * 38f * (1f + oneM * 2.2f)),
                        dispY = (int)Math.Round(-ny * 38f * (1f + oneM * 2.2f)),
                        caX = (int)Math.Round(-nx * 1.5f), caY = (int)Math.Round(-ny * 1.5f),
                        rimW256 = (int)(Clamp01(oneM * 1.85f) * 256f),
                        specW256 = (int)(spec * 130f),
                        alpha255 = a255, isEdge = a255 < 255
                    });
                }
                lutRows[y] = row.ToArray();
            });
        }

        private GraphicsPath RoundRect(RectangleF r, float rad)
        {
            GraphicsPath p = new GraphicsPath();
            float d = Math.Min(rad * 2f, Math.Min(r.Width, r.Height));
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        private void DrawPill(Graphics g, RectangleF r, string text, bool active, string id, bool leftAlign = false, bool useMelonFont = true, bool isDangerAction = false)
        {
            float hovLerp = GetHov(id);
            float t = (float)animSw.Elapsed.TotalSeconds;
            using (GraphicsPath p = RoundRect(r, r.Height * 0.46f))
            {
                Color basicFill;
                if (isDangerAction) basicFill = Color.FromArgb(255, 198 + (int)(22 * hovLerp), 56 + (int)(16 * hovLerp), 78 + (int)(16 * hovLerp));
                else if (active) basicFill = Color.FromArgb(255, 42 + (int)(14 * hovLerp), 56 + (int)(14 * hovLerp), 84 + (int)(18 * hovLerp));
                else basicFill = Color.FromArgb(255, 28 + (int)(10 * hovLerp), 34 + (int)(12 * hovLerp), 48 + (int)(16 * hovLerp));

                int fa = (int)((isDangerAction ? 96 : (active ? 64 : 28)) + (hovLerp * 36f));
                Color glassFill = isDangerAction
                    ? Color.FromArgb(ClampInt(fa + 48, 0, 255), 232, 72, 96)
                    : Color.FromArgb(ClampInt(fa, 0, 255), 255, 255, 255);

                float tb = Clamp01(themeBlend);
                Color fillCol = Color.FromArgb(
                    ClampInt((int)(glassFill.A + (basicFill.A - glassFill.A) * tb), 0, 255),
                    ClampInt((int)(glassFill.R + (basicFill.R - glassFill.R) * tb), 0, 255),
                    ClampInt((int)(glassFill.G + (basicFill.G - glassFill.G) * tb), 0, 255),
                    ClampInt((int)(glassFill.B + (basicFill.B - glassFill.B) * tb), 0, 255));
                using (SolidBrush br = new SolidBrush(fillCol)) g.FillPath(br, p);

                if (hovLerp > 0.02f)
                {
                    g.SetClip(p);
                    float spotX = isCursorInside ? Math.Max(r.Left, Math.Min(r.Right, localCursorPos.X)) : (r.X + r.Width * 0.5f);
                    float spotY = isCursorInside ? Math.Max(r.Top, Math.Min(r.Bottom, localCursorPos.Y)) : (r.Y + r.Height * 0.5f);
                    float rad = Math.Max(r.Width * 0.55f, 36f);
                    using (GraphicsPath gp = new GraphicsPath())
                    {
                        gp.AddEllipse(spotX - rad, spotY - rad, rad * 2f, rad * 2f);
                        using (PathGradientBrush pgb = new PathGradientBrush(gp))
                        {
                            pgb.CenterColor = Color.FromArgb(ClampInt((int)(56f * hovLerp), 0, 255), 255, 255, 255);
                            pgb.SurroundColors = new Color[] { Color.FromArgb(0, 255, 255, 255) };
                            g.FillPath(pgb, gp);
                        }
                    }
                    g.ResetClip();
                }

                int ra = (int)((isDangerAction ? 228 : (active ? 195 : 88)) + (hovLerp * 60f));
                Color glassRim = isDangerAction
                    ? Color.FromArgb(ClampInt(ra, 0, 255), 255, 195, 208)
                    : Color.FromArgb(ClampInt(ra, 0, 255), 255, 255, 255);
                Color basicRim = (!active && !isDangerAction)
                    ? Color.FromArgb(ClampInt(ra, 0, 255), 115, 132, 168)
                    : glassRim;
                Color rimCol = Color.FromArgb(
                    ClampInt((int)(glassRim.A + (basicRim.A - glassRim.A) * tb), 0, 255),
                    ClampInt((int)(glassRim.R + (basicRim.R - glassRim.R) * tb), 0, 255),
                    ClampInt((int)(glassRim.G + (basicRim.G - glassRim.G) * tb), 0, 255),
                    ClampInt((int)(glassRim.B + (basicRim.B - glassRim.B) * tb), 0, 255));
                using (Pen pen = new Pen(rimCol, (active || isDangerAction ? 1.35f : 1.0f) + hovLerp * 0.25f)) g.DrawPath(pen, p);

                bool useMelonHere = !useBasicTheme && useMelonFont && isDangerAction;
                float fSize = useMelonHere ? 13f : 12.5f;
                Font f = GetFont(fSize, (active || isDangerAction) ? FontStyle.Bold : FontStyle.Regular, useMelonHere);
                Color tc = Color.FromArgb(ClampInt((int)((active || isDangerAction ? 255 : 234) + hovLerp * 21f), 0, 255), 255, 255, 255);
                if (leftAlign) DrawCrispString(g, text, f, new RectangleF(r.X + 14f, r.Y, r.Width - 24f, r.Height), sfNear, tc);
                else DrawCrispString(g, text, f, r, sfCenter, tc);
            }
        }

        private int[] RenderUILayer()
        {
            List<HitZone> zones = new List<HitZone>();

            using (Graphics g = Graphics.FromImage(reusableUiBmp))
            {
                g.Clear(Color.Transparent);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;

                if (popInProgress < 0.999f)
                {
                    float p = Clamp01(popInProgress);
                    float inv = 1f - p;
                    float ease = 1f - inv * inv * inv;
                    float sc = 0.94f + 0.06f * ease;
                    float cx = surfW * 0.5f, cyMid = surfH * 0.5f;
                    g.TranslateTransform(cx, cyMid);
                    g.ScaleTransform(sc, sc);
                    g.TranslateTransform(-cx, -cyMid);
                }

                float pad = edgeMargin + 24f;
                float innerW = winW - 48f;
                float topY = edgeMargin + 18f;

                if (logoImg != null)
                {
                    g.DrawImage(logoImg, pad, topY, 44f, 44f);
                }
                Font tf = GetFont(useBasicTheme ? 20f : 18.5f, FontStyle.Bold, true);
                Font sf = GetFont(12f, FontStyle.Regular, false);
                DrawCrispString(g, useBasicTheme ? "Uninstall Birthly" : "U N I N S T A L L   B I R T H L Y", tf, pad + 56f, topY + 1f, Color.FromArgb(255, 255, 255, 255));
                DrawCrispString(g, useBasicTheme
                    ? "Basic Program Theme  •  Clean Windows Removal"
                    : "Liquid Glass Theme  •  Clean Windows Removal",
                    sf, pad + 56f, topY + 26f, Color.FromArgb(215, 232, 240, 255));

                RectangleF closeBtn = new RectangleF(edgeMargin + winW - 24f - 30f, topY + 6f, 30f, 30f);
                DrawPill(g, closeBtn, "×", false, "close", false, false);
                zones.Add(new HitZone { Id = "close", Bounds = closeBtn });

                float divY = topY + 54f;
                using (Pen dp = new Pen(Color.FromArgb(55, 255, 255, 255), 1f))
                    g.DrawLine(dp, pad, divY, pad + innerW, divY);

                // Live Theme Preview Toggle (Liquid Glass vs Basic Theme)
                float cy = divY + 12f;
                Font lf = GetFont(11.5f, FontStyle.Bold, false);
                DrawCrispString(g, "UNINSTALLER APPEARANCE:", lf, pad, cy, Color.FromArgb(230, 235, 242, 255));
                cy += 19f;

                float halfW = (innerW - 10f) * 0.5f;
                RectangleF glassBtn = new RectangleF(pad, cy, halfW, 30f);
                RectangleF basicBtn = new RectangleF(pad + halfW + 10f, cy, halfW, 30f);
                DrawPill(g, glassBtn, (!useBasicTheme ? "✓  " : "") + "LIQUID GLASS (MELON)", !useBasicTheme, "theme_glass", false, false);
                DrawPill(g, basicBtn, (useBasicTheme ? "✓  " : "") + "BASIC THEME (SEGOE UI)", useBasicTheme, "theme_basic", false, false);
                if (!isUninstalling)
                {
                    zones.Add(new HitZone { Id = "theme_glass", Bounds = glassBtn });
                    zones.Add(new HitZone { Id = "theme_basic", Bounds = basicBtn });
                }
                cy += 42f;

                // Target Install Directory Info
                DrawCrispString(g, "REMOVING PROGRAM FILES, SHORTCUTS & AUTO-START FROM:", lf, pad, cy, Color.FromArgb(230, 235, 242, 255));
                cy += 19f;

                RectangleF pathRect = new RectangleF(pad, cy, innerW, 30f);
                DrawPill(g, pathRect, installDir, false, "path_box", true, false);
                cy += 42f;

                // Optional User Data Removal Toggle Pill
                RectangleF dataRect = new RectangleF(pad, cy, innerW, 32f);
                string dataTxt = useBasicTheme
                    ? "Also delete saved birthdays, photos & settings (%LocalAppData%\\Birthly)"
                    : "ALSO DELETE SAVED BIRTHDAYS, PHOTOS & SETTINGS";
                DrawPill(g, dataRect, (removeUserData ? "✓   " : "○   ") + dataTxt, removeUserData, "opt_data", true, false, false);
                if (!isUninstalling && !uninstallDone) zones.Add(new HitZone { Id = "opt_data", Bounds = dataRect });
                cy += 44f;

                // Frosted Progress Bar
                RectangleF progTrack = new RectangleF(pad, cy, innerW, 8f);
                using (GraphicsPath tp = RoundRect(progTrack, 4f))
                using (SolidBrush tbr = new SolidBrush(Color.FromArgb(48, 255, 255, 255)))
                    g.FillPath(tbr, tp);

                if (progressVal > 0.01f)
                {
                    RectangleF progFill = new RectangleF(pad, cy, Math.Max(8f, innerW * Clamp01(progressVal)), 8f);
                    using (GraphicsPath fp = RoundRect(progFill, 4f))
                    using (SolidBrush fbr = new SolidBrush(Color.FromArgb(245, 235, 88, 112)))
                        g.FillPath(fbr, fp);
                }
                cy += 14f;

                Font stf = GetFont(11.5f, FontStyle.Regular, false);
                DrawCrispString(g, statusText, stf, new RectangleF(pad, cy, innerW, 18f), sfNear, Color.FromArgb(230, 235, 242, 255));

                // Bottom Cancel / Uninstall buttons
                float botY = edgeMargin + winH - 18f - 34f;
                RectangleF cancelBtn = new RectangleF(pad, botY, 118f, 34f);
                RectangleF uninstBtn = new RectangleF(pad + innerW - 196f, botY, 196f, 34f);
                DrawPill(g, cancelBtn, uninstallDone ? "CLOSE" : "CANCEL", false, "cancel", false, false, false);
                DrawPill(g, uninstBtn, uninstallDone ? "UNINSTALLED ✓" : (isUninstalling ? "UNINSTALLING..." : "UNINSTALL BIRTHLY"), true, "uninstall", false, !useBasicTheme, true);
                zones.Add(new HitZone { Id = "cancel", Bounds = cancelBtn });
                if (!isUninstalling && !uninstallDone) zones.Add(new HitZone { Id = "uninstall", Bounds = uninstBtn });

                BitmapData bd = reusableUiBmp.LockBits(new Rectangle(0, 0, surfW, surfH), ImageLockMode.ReadOnly, PixelFormat.Format32bppPArgb);
                Marshal.Copy(bd.Scan0, reusableUiPx, 0, reusableUiPx.Length);
                reusableUiBmp.UnlockBits(bd);
            }

            hitZones = zones;
            return reusableUiPx;
        }

        private void CompositeSurface(bool rebuildUi = true)
        {
            int[] uiPx = rebuildUi ? RenderUILayer() : reusableUiPx;
            int offX = curX - vScreen.X, offY = curY - vScreen.Y;
            int maxW = deskW - 1, maxH = deskH - 1;
            int tb256 = ClampInt((int)(themeBlend * 256f), 0, 256);

            int optTintR, optTintG, optTintB, optTintW;
            GetOpticalTintRgb(out optTintR, out optTintG, out optTintB, out optTintW);

            if (cachedBackdropBuf == null || cachedBackdropBuf.Length != surfW * surfH)
            {
                cachedBackdropBuf = new int[surfW * surfH];
                lastBgTb256 = -1;
            }

            if (curX != lastBgX || curY != lastBgY || tb256 != lastBgTb256)
            {
                Parallel.For(0, surfH, y => {
                    PixelLUT[] row = lutRows[y];
                    if (row == null) return;
                    int gradShift = (y * 10) / Math.Max(1, surfH);
                    for (int i = 0; i < row.Length; i++)
                    {
                        PixelLUT fp = row[i];
                        int br = 24 - (gradShift * 2) / 3;
                        int bg = 30 - (gradShift * 2) / 3;
                        int bb = 44 - gradShift;
                        if (optTintW > 0)
                        {
                            int btw = optTintW / 2;
                            br = (br * (256 - btw) + optTintR * btw) >> 8;
                            bg = (bg * (256 - btw) + optTintG * btw) >> 8;
                            bb = (bb * (256 - btw) + optTintB * btw) >> 8;
                        }
                        int brimW = fp.rimW256 / 3;
                        if (brimW > 6)
                        {
                            br = (br * (256 - brimW) + 125 * brimW) >> 8;
                            bg = (bg * (256 - brimW) + 142 * brimW) >> 8;
                            bb = (bb * (256 - brimW) + 180 * brimW) >> 8;
                        }

                        int r, g, b;
                        if (tb256 >= 255)
                        {
                            r = br; g = bg; b = bb;
                        }
                        else
                        {
                            int bx = offX + fp.px + fp.dispX, by = offY + fp.py + fp.dispY;
                            int xR = ClampInt(bx - fp.caX, 0, maxW), yR = ClampInt(by - fp.caY, 0, maxH);
                            int xG = ClampInt(bx, 0, maxW), yG = ClampInt(by, 0, maxH);
                            int xB = ClampInt(bx + fp.caX, 0, maxW), yB = ClampInt(by + fp.caY, 0, maxH);

                            r = (blurredDesk[yR * deskW + xR] >> 16) & 0xFF;
                            g = (blurredDesk[yG * deskW + xG] >> 8) & 0xFF;
                            b = blurredDesk[yB * deskW + xB] & 0xFF;

                            if (fp.rimW256 > 4)
                            {
                                int sr = (sharpDesk[yR * deskW + xR] >> 16) & 0xFF;
                                int sg = (sharpDesk[yG * deskW + xG] >> 8) & 0xFF;
                                int sb = sharpDesk[yB * deskW + xB] & 0xFF;
                                r = (r * (256 - fp.rimW256) + sr * fp.rimW256) >> 8;
                                g = (g * (256 - fp.rimW256) + sg * fp.rimW256) >> 8;
                                b = (b * (256 - fp.rimW256) + sb * fp.rimW256) >> 8;
                            }

                            r = (r * 142 + 16 * 114) >> 8;
                            g = (g * 142 + 21 * 114) >> 8;
                            b = (b * 142 + 34 * 114) >> 8;

                            if (optTintW > 0)
                            {
                                int invTw = 256 - optTintW;
                                r = (r * invTw + optTintR * optTintW) >> 8;
                                g = (g * invTw + optTintG * optTintW) >> 8;
                                b = (b * invTw + optTintB * optTintW) >> 8;
                            }

                            if (fp.specW256 > 0)
                            {
                                r = (r * (256 - fp.specW256) + 250 * fp.specW256) >> 8;
                                g = (g * (256 - fp.specW256) + 252 * fp.specW256) >> 8;
                                b = (b * (256 - fp.specW256) + 255 * fp.specW256) >> 8;
                            }

                            if (tb256 > 0)
                            {
                                int invTb = 256 - tb256;
                                r = (r * invTb + br * tb256) >> 8;
                                g = (g * invTb + bg * tb256) >> 8;
                                b = (b * invTb + bb * tb256) >> 8;
                            }
                        }
                        cachedBackdropBuf[fp.idx] = (r << 16) | (g << 8) | b;
                    }
                });
                lastBgX = curX; lastBgY = curY; lastBgTb256 = tb256;
            }

            Parallel.For(0, surfH, y => {
                PixelLUT[] row = lutRows[y];
                if (row == null) return;
                for (int i = 0; i < row.Length; i++)
                {
                    PixelLUT fp = row[i];
                    int bgc = cachedBackdropBuf[fp.idx];
                    int r = (bgc >> 16) & 0xFF, g = (bgc >> 8) & 0xFF, b = bgc & 0xFF;

                    int u = uiPx[fp.idx];
                    int ua = (u >> 24) & 0xFF;
                    if (ua > 0)
                    {
                        int inv = 256 - ua;
                        r = Math.Min(255, ((u >> 16) & 0xFF) + ((r * inv) >> 8));
                        g = Math.Min(255, ((u >> 8) & 0xFF) + ((g * inv) >> 8));
                        b = Math.Min(255, (u & 0xFF) + ((b * inv) >> 8));
                    }

                    if (!fp.isEdge) surfaceBuf[fp.idx] = (255 << 24) | (r << 16) | (g << 8) | b;
                    else
                    {
                        int a = fp.alpha255;
                        int a256 = a + 1;
                        surfaceBuf[fp.idx] = (a << 24) | (((r * a256) >> 8) << 16) | (((g * a256) >> 8) << 8) | ((b * a256) >> 8);
                    }
                }
            });
        }

        private void RenderFrame(bool rebuildUi = true)
        {
            if (!this.IsHandleCreated || persistentBitsPtr == IntPtr.Zero) return;
            CompositeSurface(rebuildUi);

            Marshal.Copy(surfaceBuf, 0, persistentBitsPtr, surfaceBuf.Length);

            SIZE sz = new SIZE(surfW, surfH);
            POINT src = new POINT(0, 0);
            POINT dst = new POINT(curX, curY);
            BLENDFUNCTION bf = new BLENDFUNCTION { BlendOp = AC_SRC_OVER, SourceConstantAlpha = 255, AlphaFormat = AC_SRC_ALPHA };
            UpdateLayeredWindow(this.Handle, persistentScreenDc, ref dst, ref sz, persistentMemDc, ref src, 0, ref bf, ULW_ALPHA);
        }

        public void ExportPreview(string outPath)
        {
            CompositeSurface(true);
            int pad = 50;
            using (Bitmap comp = new Bitmap(surfW + pad * 2, surfH + pad * 2, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(comp))
            {
                using (Bitmap bg = new Bitmap(deskW, deskH, PixelFormat.Format32bppArgb))
                {
                    BitmapData bbd = bg.LockBits(new Rectangle(0, 0, deskW, deskH), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                    Marshal.Copy(sharpDesk, 0, bbd.Scan0, sharpDesk.Length);
                    bg.UnlockBits(bbd);
                    g.DrawImage(bg, new Rectangle(0, 0, comp.Width, comp.Height), new Rectangle(Math.Max(0, curX - vScreen.X - pad), Math.Max(0, curY - vScreen.Y - pad), comp.Width, comp.Height), GraphicsUnit.Pixel);
                }
                using (Bitmap sf = new Bitmap(surfW, surfH, PixelFormat.Format32bppPArgb))
                {
                    BitmapData sbd = sf.LockBits(new Rectangle(0, 0, surfW, surfH), ImageLockMode.WriteOnly, PixelFormat.Format32bppPArgb);
                    Marshal.Copy(surfaceBuf, 0, sbd.Scan0, surfaceBuf.Length);
                    sf.UnlockBits(sbd);
                    g.DrawImage(sf, pad, pad);
                }
                comp.Save(outPath, ImageFormat.Png);
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            RenderFrame(true);
        }

        private string HitTest(int x, int y)
        {
            for (int i = hitZones.Count - 1; i >= 0; i--)
                if (hitZones[i].Bounds.Contains(x, y)) return hitZones[i].Id;
            return null;
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            isCursorInside = false;
            if (hoveredId != null)
            {
                hoveredId = null;
                this.Cursor = Cursors.Default;
                WakeAnimTimer();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            localCursorPos = new PointF(e.X, e.Y);
            isCursorInside = true;
            if (isDragging)
            {
                Point p = Cursor.Position;
                int nx = dragStartWin.X + (p.X - dragStartCursor.X);
                int ny = dragStartWin.Y + (p.Y - dragStartCursor.Y);
                if (nx != curX || ny != curY)
                {
                    curX = nx;
                    curY = ny;
                    if (useBasicTheme && themeBlend >= 0.99f)
                    {
                        SetWindowPos(this.Handle, IntPtr.Zero, curX, curY, 0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
                    }
                    else
                    {
                        RenderFrame(false);
                    }
                }
                return;
            }
            string hid = HitTest(e.X, e.Y);
            if (hid != hoveredId)
            {
                hoveredId = hid;
                this.Cursor = hid != null ? Cursors.Hand : Cursors.Default;
                WakeAnimTimer();
            }
            else if (hid != null)
            {
                WakeAnimTimer();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (isDragging) { isDragging = false; this.Capture = false; this.Location = new Point(curX, curY); }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;

            string hid = HitTest(e.X, e.Y);
            if (hid == null)
            {
                isDragging = true;
                dragStartCursor = Cursor.Position;
                dragStartWin = new Point(curX, curY);
                this.Capture = true;
                return;
            }

            if (hid == "close" || hid == "cancel") { this.Close(); return; }
            if (hid == "theme_glass")
            {
                useBasicTheme = false;
                WakeAnimTimer();
            }
            else if (hid == "theme_basic")
            {
                useBasicTheme = true;
                WakeAnimTimer();
            }
            else if (hid == "opt_data")
            {
                removeUserData = !removeUserData;
            }
            else if (hid == "uninstall")
            {
                RunUninstallation();
                return;
            }
            RenderFrame(true);
        }

        private void RunUninstallation()
        {
            isUninstalling = true;
            progressVal = 0.20f;
            statusText = "Stopping any running Birthly instance...";
            RenderFrame(true);
            Application.DoEvents();

            try
            {
                foreach (Process p in Process.GetProcessesByName("Birthly"))
                {
                    try { p.Kill(); p.WaitForExit(1500); } catch { }
                }
            }
            catch { }

            progressVal = 0.48f;
            statusText = "Removing shortcuts & Windows registry entries...";
            RenderFrame(true);
            Application.DoEvents();

            try
            {
                using (RegistryKey rk = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (rk != null) rk.DeleteValue("Birthly", false);
                }
            }
            catch { }

            try
            {
                Registry.CurrentUser.DeleteSubKeyTree(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\Birthly", false);
            }
            catch { }

            string desktopLnk = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Birthly.lnk");
            string startupLnk = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "Birthly.lnk");
            string startMenuDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "Birthly");

            TryDeleteFile(desktopLnk);
            TryDeleteFile(startupLnk);
            TryDeleteDir(startMenuDir);

            progressVal = 0.78f;
            statusText = removeUserData ? "Deleting program files & saved user data..." : "Deleting program files...";
            RenderFrame(true);
            Application.DoEvents();

            if (removeUserData)
            {
                string dataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Birthly");
                TryDeleteDir(dataDir);
            }

            string selfPath = Application.ExecutablePath;
            try
            {
                foreach (string f in Directory.GetFiles(installDir))
                {
                    if (!f.Equals(selfPath, StringComparison.OrdinalIgnoreCase)) TryDeleteFile(f);
                }
                foreach (string d in Directory.GetDirectories(installDir))
                {
                    TryDeleteDir(d);
                }
            }
            catch { }

            progressVal = 1.0f;
            isUninstalling = false;
            uninstallDone = true;
            statusText = "Birthly has been uninstalled cleanly! You can close this window.";
            RenderFrame(true);
            Application.DoEvents();

            // Schedule self-deletion of Uninstall.exe and installDir from %TEMP% so cmd.exe never locks installDir!
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("cmd.exe",
                    string.Format("/C cd /d \"%TEMP%\" & for /L %i in (1,1,20) do (if exist \"{1}\" (ping 127.0.0.1 -n 2 > nul & del /f /q \"{0}\" 2>nul & rmdir /s /q \"{1}\" 2>nul))", selfPath, installDir))
                {
                    WorkingDirectory = Path.GetTempPath(),
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                Process.Start(psi);
            }
            catch { }
        }

        private static void TryDeleteFile(string p)
        {
            try { if (File.Exists(p)) File.Delete(p); } catch { }
        }

        private static void TryDeleteDir(string p)
        {
            try { if (Directory.Exists(p)) Directory.Delete(p, true); } catch { }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try { if (animTimer != null) { animTimer.Stop(); animTimer.Dispose(); } } catch { }
                FreePersistentDIB();
                try { if (reusableUiBmp != null) reusableUiBmp.Dispose(); } catch { }
                try { if (logoImg != null) logoImg.Dispose(); } catch { }
                try { foreach (Font f in fontCache.Values) f.Dispose(); fontCache.Clear(); } catch { }
                try { sharedTextBr.Dispose(); sharedShadowBr.Dispose(); } catch { }
                try { pfc.Dispose(); sfCenter.Dispose(); sfNear.Dispose(); } catch { }
            }
            base.Dispose(disposing);
        }
    }
}
