using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.Win32;

namespace LiquidGlassWidget
{
    static class Program
    {
        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [STAThread]
        static void Main(string[] args)
        {
            try { SetProcessDPIAware(); } catch { }
            if (args != null && args.Length >= 2 && args[0] == "--preview")
            {
                using (GlassWidgetForm f = new GlassWidgetForm(true))
                {
                    f.ExportPreviewImage(args[1]);
                }
                return;
            }
            if (args != null && args.Length >= 2 && args[0] == "--preview-delete")
            {
                using (GlassWidgetForm f = new GlassWidgetForm(true))
                {
                    f.SwitchToDeleteConfirmForPreview();
                    f.ExportPreviewImage(args[1]);
                }
                return;
            }
            if (args != null && args.Length >= 2 && args[0] == "--preview-settings")
            {
                using (GlassWidgetForm f = new GlassWidgetForm(true))
                {
                    f.SwitchToSettingsForPreview();
                    f.ExportPreviewImage(args[1]);
                }
                return;
            }
            if (args != null && args.Length >= 2 && args[0] == "--preview-usericon")
            {
                using (GlassWidgetForm f = new GlassWidgetForm(true))
                {
                    f.SwitchToUserIconPreview();
                    f.ExportPreviewImage(args[1]);
                }
                return;
            }
            if (args != null && args.Length >= 2 && args[0] == "--preview-calendar")
            {
                using (GlassWidgetForm f = new GlassWidgetForm(true))
                {
                    f.SwitchToCalendarForPreview();
                    f.ExportPreviewImage(args[1]);
                }
                return;
            }
            if (args != null && args.Length >= 2 && args[0] == "--preview-appearance")
            {
                using (GlassWidgetForm f = new GlassWidgetForm(true))
                {
                    f.SwitchToSettingsCategoryForPreview(1);
                    f.ExportPreviewImage(args[1]);
                }
                return;
            }
            if (args != null && args.Length >= 2 && args[0] == "--preview-layers")
            {
                using (GlassWidgetForm f = new GlassWidgetForm(true))
                {
                    f.SwitchToSettingsCategoryForPreview(2);
                    f.ExportPreviewImage(args[1]);
                }
                return;
            }
            if (args != null && args.Length >= 2 && args[0] == "--preview-whitebg")
            {
                using (GlassWidgetForm f = new GlassWidgetForm(true))
                {
                    f.ForceWhiteBackgroundForPreview();
                    f.ExportPreviewImage(args[1]);
                }
                return;
            }
            if (args != null && args.Length >= 2 && args[0] == "--preview-dropdown")
            {
                using (GlassWidgetForm f = new GlassWidgetForm(true))
                {
                    f.SwitchToDropdownForPreview();
                    f.ExportPreviewImage(args[1]);
                }
                return;
            }
            if (args != null && args.Length >= 2 && args[0] == "--preview-basic")
            {
                using (GlassWidgetForm f = new GlassWidgetForm(true))
                {
                    f.SwitchToBasicThemeForPreview();
                    f.ExportPreviewImage(args[1]);
                }
                return;
            }
            if (args != null && args.Length >= 2 && args[0] == "--preview-bday")
            {
                using (GlassWidgetForm f = new GlassWidgetForm(true))
                {
                    f.SwitchToBirthdayCelebrationPreview();
                    f.ExportPreviewImage(args[1]);
                }
                return;
            }
            if (args != null && args.Length >= 2 && args[0] == "--preview-basic-light")
            {
                using (GlassWidgetForm f = new GlassWidgetForm(true))
                {
                    f.SwitchToBasicLightDropdownPreview();
                    f.ExportPreviewImage(args[1]);
                }
                return;
            }
            bool createdNew;
            using (Mutex mutex = new Mutex(true, "LiquidGlassDesktopWidget_SingleInstance", out createdNew))
            {
                if (!createdNew) return;
                try
                {
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.Run(new GlassWidgetForm(false));
                }
                catch (Exception ex)
                {
                    try
                    {
                        string logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Birthly");
                        Directory.CreateDirectory(logDir);
                        File.WriteAllText(Path.Combine(logDir, "crash.log"), ex.ToString());
                    }
                    catch { }
                }
            }
        }
    }

    [DataContract]
    public class PersonEntry
    {
        [DataMember] public string Id { get; set; }
        [DataMember] public string Name { get; set; }
        [DataMember] public int Day { get; set; }
        [DataMember] public int Month { get; set; }
        [DataMember] public int BirthYear { get; set; }
        [DataMember] public string Category { get; set; }
        [DataMember] public string Notes { get; set; }
        [DataMember] public string IconType { get; set; }
        [DataMember] public string ImagePath { get; set; }
        [DataMember] public string AvatarColorHex { get; set; }

        public DateTime GetNextBirthday(DateTime now)
        {
            int year = now.Year;
            int m = Math.Max(1, Math.Min(12, Month));
            int maxDay = DateTime.DaysInMonth(year, m);
            int d = Math.Max(1, Math.Min(Day, maxDay));
            DateTime next = new DateTime(year, m, d);
            if (next.Date < now.Date)
            {
                year++;
                maxDay = DateTime.DaysInMonth(year, m);
                d = Math.Max(1, Math.Min(Day, maxDay));
                next = new DateTime(year, m, d);
            }
            return next;
        }

        public int GetDaysUntil(DateTime now)
        {
            DateTime next = GetNextBirthday(now);
            return (int)(next.Date - now.Date).TotalDays;
        }

        public int GetCurrentAge(DateTime now)
        {
            if (BirthYear < 1900 || BirthYear > now.Year) return 0;
            int age = now.Year - BirthYear;
            int m = Math.Max(1, Math.Min(12, Month));
            int maxDay = DateTime.DaysInMonth(now.Year, m);
            int d = Math.Max(1, Math.Min(Day, maxDay));
            DateTime bdayThisYear = new DateTime(now.Year, m, d);
            if (now.Date < bdayThisYear.Date) age--;
            return Math.Max(0, age);
        }

        public int GetTurningAge(DateTime now)
        {
            if (BirthYear < 1900 || BirthYear > now.Year + 1) return 0;
            DateTime next = GetNextBirthday(now);
            return Math.Max(0, next.Year - BirthYear);
        }

        public int GetDaysLived(DateTime now)
        {
            if (BirthYear < 1900 || BirthYear > now.Year) return -1;
            int m = Math.Max(1, Math.Min(12, Month));
            int maxDay = DateTime.DaysInMonth(BirthYear, m);
            int d = Math.Max(1, Math.Min(Day, maxDay));
            DateTime born = new DateTime(BirthYear, m, d);
            if (born.Date > now.Date) return -1;
            return (int)(now.Date - born.Date).TotalDays;
        }

        private static readonly int[] SpecialDayMilestones = new int[] {
            100, 222, 333, 500, 777, 1000, 1111, 2000, 2222, 3000, 3333,
            4000, 4444, 5000, 5555, 6000, 6666, 7000, 7777, 8000, 8888,
            9000, 9999, 10000, 11111, 12345, 15000, 20000, 22222, 25000, 30000, 33333, 36500, 40000
        };

        public bool GetNextMilestone(DateTime now, out int milestoneDays, out int daysUntil)
        {
            milestoneDays = 0;
            daysUntil = 999999;
            int lived = GetDaysLived(now);
            if (lived < 0) return false;
            for (int i = 0; i < SpecialDayMilestones.Length; i++)
            {
                if (SpecialDayMilestones[i] >= lived)
                {
                    milestoneDays = SpecialDayMilestones[i];
                    daysUntil = SpecialDayMilestones[i] - lived;
                    return true;
                }
            }
            return false;
        }
    }

    [DataContract]
    public class WidgetConfig
    {
        [DataMember] public List<PersonEntry> People { get; set; }
        [DataMember] public int AlertSoonDays { get; set; }
        [DataMember] public int AlertUrgentDays { get; set; }
        [DataMember] public bool EnableSoundAlerts { get; set; }
        [DataMember] public bool EnablePopupBanner { get; set; }
        [DataMember] public bool AutoStartWithPC { get; set; }
        [DataMember] public string AccentHex { get; set; }
        [DataMember] public int GlassPresetIndex { get; set; }
        [DataMember] public bool ShowAgeBadge { get; set; }
        [DataMember] public int WindowX { get; set; }
        [DataMember] public int WindowY { get; set; }
        [DataMember] public int WidgetWidth { get; set; }
        [DataMember] public int WidgetHeight { get; set; }

        // Appearance & Layout
        [DataMember] public string TintVariant { get; set; }      // Clear, Ice, Rose, Mint, Amber, Violet, Obsidian
        [DataMember] public string FontName { get; set; }         // Melon Squishy, Segoe UI, Bahnschrift, Consolas, Georgia
        [DataMember] public string TextColorMode { get; set; }    // Auto, White, Dark, Ice, Gold
        [DataMember] public string LayoutMode { get; set; }       // HeroLeft, HeroRight, HeroTop, HeroBottom

        // Layer System (Z-Order)
        [DataMember] public string LayerMode { get; set; }        // Above, Below
        [DataMember] public bool LayerDesktopIcons { get; set; }
        [DataMember] public bool LayerRainmeter { get; set; }
        [DataMember] public bool LayerAlwaysOnTop { get; set; }
        [DataMember] public List<string> LayerTargetExes { get; set; }

        // Performance & Refresh Rate
        [DataMember] public int TargetRefreshRate { get; set; }   // 0 = Auto (Monitor Native Hz), or 60, 120, 144, 240
        [DataMember] public bool RefreshWhileDragging { get; set; }
        [DataMember] public string Language { get; set; }         // EN, DE, ES, FR
        [DataMember] public bool EnableClickSounds { get; set; }
        [DataMember] public string CustomClickSoundPath { get; set; }
        [DataMember] public string CursorStyle { get; set; }      // Glass, System, Custom
        [DataMember] public string CustomCursorPath { get; set; }
        [DataMember] public bool HasPerfDefaultsInit { get; set; }

        // Theme Mode (Basic Program Theme vs Liquid Glass Theme, Dark Glass), Window Mode (Widget vs Real Window), First-Launch Tutorial & Birthday Celebration/Banner
        [DataMember] public bool UseBasicTheme { get; set; }
        [DataMember] public bool DarkMode { get; set; }
        [DataMember] public bool AppWindowMode { get; set; }
        [DataMember] public bool EnableBirthdayAnimation { get; set; }
        [DataMember] public bool EnableBirthdayMessage { get; set; }
        [DataMember] public bool HasBdayDefaultsInit { get; set; }
        [DataMember] public bool HasCompletedTutorial { get; set; }
        [DataMember] public bool AdaptTextToBrightBg { get; set; }
        [DataMember] public int BrightBgDarkenLevel { get; set; }
        [DataMember] public int BrightBgAmplifier { get; set; }
        [DataMember] public bool HasAdaptDefaultsInit { get; set; }
    }

    [DataContract]
    public class BackupBundle
    {
        [DataMember] public int FormatVersion { get; set; }
        [DataMember] public string ExportedAt { get; set; }
        [DataMember] public WidgetConfig Config { get; set; }
        [DataMember] public Dictionary<string, string> EmbeddedImagesBase64 { get; set; }
    }

    public class GlassWidgetForm : Form
    {
        // Win32 Constants
        private const int WS_EX_LAYERED = 0x00080000;
        private const int WS_EX_APPWINDOW = 0x00040000;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_NOACTIVATE = 0x08000000;

        private const int WM_WINDOWPOSCHANGING = 0x0046;
        private const int WM_DISPLAYCHANGE = 0x007E;
        private const int WM_SETTINGCHANGE = 0x001A;
        private const int WM_ERASEBKGND = 0x0014;
        private const int WM_MOUSEACTIVATE = 0x0021;
        private const int MA_ACTIVATE = 1;
        private const int MA_NOACTIVATE = 3;

        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_SHOWWINDOW = 0x0040;
        private static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);

        private const byte AC_SRC_OVER = 0x00;
        private const byte AC_SRC_ALPHA = 0x01;
        private const int ULW_ALPHA = 0x02;

        [StructLayout(LayoutKind.Sequential)]
        private struct WINDOWPOS
        {
            public IntPtr hwnd;
            public IntPtr hwndInsertAfter;
            public int x;
            public int y;
            public int cx;
            public int cy;
            public uint flags;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct BLENDFUNCTION
        {
            public byte BlendOp;
            public byte BlendFlags;
            public byte SourceConstantAlpha;
            public byte AlphaFormat;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
            public POINT(int x, int y) { X = x; Y = y; }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SIZE
        {
            public int Width;
            public int Height;
            public SIZE(int w, int h) { Width = w; Height = h; }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left, Top, Right, Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BITMAPINFOHEADER
        {
            public uint biSize;
            public int biWidth;
            public int biHeight;
            public ushort biPlanes;
            public ushort biBitCount;
            public uint biCompression;
            public uint biSizeImage;
            public int biXPelsPerMeter;
            public int biYPelsPerMeter;
            public uint biClrUsed;
            public uint biClrImportant;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BITMAPINFO
        {
            public BITMAPINFOHEADER bmiHeader;
            public uint bmiColors;
        }

        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst, ref POINT pptDst, ref SIZE psize, IntPtr hdcSrc, ref POINT pptSrc, uint crKey, ref BLENDFUNCTION pblend, uint dwFlags);

        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll", ExactSpelling = true)]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr CreateCompatibleDC(IntPtr hDC);

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern bool DeleteDC(IntPtr hdc);

        [DllImport("gdi32.dll", ExactSpelling = true)]
        private static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern bool DeleteObject(IntPtr hObject);

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr CreateDIBSection(IntPtr hdc, ref BITMAPINFO pbmi, uint iUsage, out IntPtr ppvBits, IntPtr hSection, uint dwOffset);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        [DllImport("user32.dll")]
        private static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, uint nFlags);

        [DllImport("dwmapi.dll")]
        private static extern int DwmFlush();

        [DllImport("dwmapi.dll")]
        private static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out int pvAttribute, int cbAttribute);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);

        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, uint dwRop);

        [DllImport("winmm.dll")]
        private static extern uint timeBeginPeriod(uint uPeriod);

        [DllImport("winmm.dll")]
        private static extern uint timeEndPeriod(uint uPeriod);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        private struct DEVMODE
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmDeviceName;
            public ushort dmSpecVersion;
            public ushort dmDriverVersion;
            public ushort dmSize;
            public ushort dmDriverExtra;
            public uint dmFields;
            public int dmPositionX;
            public int dmPositionY;
            public uint dmDisplayOrientation;
            public uint dmDisplayFixedOutput;
            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmFormName;
            public ushort dmLogPixels;
            public uint dmBitsPerPel;
            public uint dmPelsWidth;
            public uint dmPelsHeight;
            public uint dmDisplayFlags;
            public uint dmDisplayFrequency;
            public uint dmICMMethod;
            public uint dmICMIntent;
            public uint dmMediaType;
            public uint dmDitherType;
            public uint dmReserved1;
            public uint dmReserved2;
            public uint dmPanningWidth;
            public uint dmPanningHeight;
        }

        [DllImport("user32.dll", CharSet = CharSet.Ansi)]
        private static extern bool EnumDisplaySettings(string lpszDeviceName, int iModeNum, ref DEVMODE lpDevMode);

        private const int ENUM_CURRENT_SETTINGS = -1;
        private const int DWMWA_CLOAKED = 14;
        private const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;

        [DllImport("user32.dll")]
        private static extern IntPtr CreateIconIndirect(ref ICONINFO icon);

        [DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr hIcon);

        [StructLayout(LayoutKind.Sequential)]
        private struct ICONINFO
        {
            public bool fIcon;
            public int xHotspot;
            public int yHotspot;
            public IntPtr hbmMask;
            public IntPtr hbmColor;
        }

        private const uint GW_HWNDPREV = 3;

        // Portable Per-User Data Paths (%LocalAppData%\Birthly) + Install Folder for bundled fonts
        private readonly string installDir = AppDomain.CurrentDomain.BaseDirectory;
        private readonly string appDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Birthly");
        private readonly string configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Birthly", "birthdays_config.json");
        private readonly string avatarsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Birthly", "avatars");

        // First-Launch Interactive Tutorial State
        private bool showTutorialOverlay = false;
        private int tutorialStep = 0;

        // User's Downloaded Font (Melon Squishy) + Fallback Fonts
        private PrivateFontCollection privateFonts = new PrivateFontCollection();
        private FontFamily melonSquishyFamily;
        private FontFamily quicksandFamily;
        private FontFamily aquaticoFamily;
        private FontFamily anuratiFamily;

        [DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
        private static extern int AddFontResourceEx(string lpszFilename, uint fl, IntPtr pdv);

        // Widget Dimensions
        private int widgetWidth = 780;
        private int widgetHeight = 500;
        private float cornerRadius = 50f;
        private int edgeMargin = 4;

        // Top-Left Move Grip
        private float gizmoOffsetX = 44f;
        private float gizmoOffsetY = 32f;
        private float gizmoHalfW = 16f;
        private float gizmoHalfH = 10f;
        private volatile bool isGizmoHovered = false;
        private volatile bool isDragging = false;
        private volatile bool isShuttingDown = false;
        private volatile bool needFullScreenCapture = true;
        private volatile bool isSnappingToScreen = false;
        private float snapFloatX, snapFloatY;
        private int snapTargetX, snapTargetY;
        private Point dragStartCursor;
        private Point dragStartLocation;
        private volatile int currentWinX;
        private volatile int currentWinY;
        private int dirtyFrame = 1;

        // Optical Liquid Glass Parameters
        private float refractionStrength = 44.0f;
        private float chromaticStrength = 1.65f;
        private float blurSigma = 2.6f;
        private float fresnelIntensity = 0.55f;

        // Double-Buffered Live Desktop Backdrop Arrays
        private volatile int[] blurredPixels;
        private volatile int[] sharpPixels;
        private int deskW, deskH;
        private Rectangle virtualScreenRect;

        // Persistent Full-Desktop Capture DIB & Pre-Allocated Blur Buffers (Zero GC allocation per frame -> 60 FPS Live Raindrops!)
        private IntPtr captureMemDc = IntPtr.Zero;
        private IntPtr captureDib = IntPtr.Zero;
        private IntPtr captureOldBmp = IntPtr.Zero;
        private IntPtr captureBitsPtr = IntPtr.Zero;
        private int[] captureStagingA;
        private int[] captureStagingB;
        private int[] blurOutA;
        private int[] blurOutB;
        private int[] quarterBuf1;
        private int[] quarterBuf2;
        private int activeBufferIdx = 0;

        // Real-Time Window Occlusion Culling State (skips rendering covered parts & sleeps when fully covered!)
        private volatile bool isFullyOccluded = false;
        private volatile Rectangle[] occludedLocalRects = new Rectangle[0];
        private Stopwatch occlusionCheckTimer = Stopwatch.StartNew();

        // Cached Rainmeter Overlay Buffer (Refreshed once per second so we don't stall live raindrop capture!)
        private int[] cachedRainTextPixels;
        private RECT cachedRainRect;
        private bool hasCachedRain = false;
        private Stopwatch rainRefreshTimer = Stopwatch.StartNew();

        // Precomputed Optical LUT
        private struct FastPixelLUT
        {
            public int index;
            public int px, py;
            public int dispX, dispY;
            public int caX, caY;
            public int rimClarity256;
            public int absorb256;
            public int tintWeight256;
            public int specWeight256;
            public int alpha255;
            public bool isEdge;
            public bool isGizmoZone;
            public byte gizmoLightNormal;
            public byte gizmoLightHover;
        }

        private volatile FastPixelLUT[][] rowLUTs;
        private int surfaceW, surfaceH;
        private int[] surfaceBuffer;

        // Pre-rendered Minimal Monochrome UI Overlay Layer (Persistent Reusable Buffers — Zero GC Churn!)
        private volatile int[] uiOverlayPixels;
        private Bitmap reusableUiBmp;
        private int[] reusableUiPixelsA;
        private int[] reusableUiPixelsB;
        private int activeUiBufIdx = 0;
        private float lastUiRebuildSec = -10f;
        private int backdropFrameSeq = 0;
        private int lastCompositedBackdropSeq = -1;
        private int uiFrameSeq = 0;
        private int lastCompositedUiSeq = -1;
        private readonly object uiLock = new object();
        private Dictionary<string, Image> avatarCache = new Dictionary<string, Image>();

        // 60 FPS Animation Engine State (Popup Spring Scale, Hover Sweep, Cursor Ripple — NO vertical bobbing!)
        private Stopwatch animClock = Stopwatch.StartNew();
        private float lastAnimTime = 0f;
        private float viewPopupProgress = 1.0f;    // 0 -> 1 smooth spring pop-up scale & fade when opening menus
        private float deleteModalProgress = 0.0f;  // 0 -> 1 smooth pop-up scale & backdrop fade for delete confirmation
        private PersonEntry lastDeleteModalPerson = null;
        private float filterDropdownProgress = 0.0f; // 0 -> 1 smooth spring pop-up scale & fade for filter dropdown
        private float settingsTabProgress = 1.0f;    // 0 -> 1 smooth spring slide/scale when switching settings categories
        private float calendarViewProgress = 1.0f;   // 0 -> 1 smooth transition when switching List <-> Calendar
        private float heroStepperProgress = 1.0f;    // 0 -> 1 smooth slide/scale when stepping ‹ / › on Hero card
        private float tutorialStepProgress = 1.0f;   // 0 -> 1 smooth slide/scale when stepping tutorial pages
        private RectangleF animDashHeroRect = RectangleF.Empty;
        private RectangleF animDashPanelRect = RectangleF.Empty;
        private RectangleF targetDashHeroRect = RectangleF.Empty;
        private RectangleF targetDashPanelRect = RectangleF.Empty;
        private RectangleF animPrevHeroRect = RectangleF.Empty;
        private RectangleF animPrevListRect = RectangleF.Empty;
        private RectangleF targetPrevHeroRect = RectangleF.Empty;
        private RectangleF targetPrevListRect = RectangleF.Empty;
        private Dictionary<string, float> hoverProgress = new Dictionary<string, float>();
        private PointF localCursorPos = new PointF(-1000f, -1000f);
        private bool isCursorInside = false;

        // Birthday Celebration Burst Animation & Banner State
        private float bdayCelebrationStartSec = -100f;
        private const float BdayCelebrationDurationSec = 10.0f;
        private bool hasActiveBdayParticles = false;
        private bool bdayBannerDismissed = false;
        private bool stagedBdayAnim = true;
        private bool stagedBdayMsg = true;
        private bool origBdayAnim = true;
        private bool origBdayMsg = true;

        // Click Ripple Effect (expanding frosted glass ring on click)
        private PointF clickRipplePos = new PointF(-1000f, -1000f);
        private float clickRippleProgress = 1.0f;

        // Custom Programmatic Liquid-Glass Cursors for Every Scenario
        private IntPtr customCursorHicon = IntPtr.Zero;
        private Cursor curGlassDefault;
        private Cursor curGlassPointer;
        private Cursor curGlassText;
        private Cursor curGlassGrab;
        private Cursor curGlassGrabbing;
        private Cursor curGlassDelete;

        // Reusable GDI+ StringFormats, Font Cache, Brushes & Wake Event (eliminates 150+ per-frame native handle allocations!)
        private readonly StringFormat sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap, Trimming = StringTrimming.EllipsisCharacter };
        private readonly StringFormat sfCenterWrap = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };
        private readonly StringFormat sfNearCenter = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap, Trimming = StringTrimming.EllipsisCharacter };
        private readonly StringFormat sfNearNear = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Near, FormatFlags = StringFormatFlags.NoWrap, Trimming = StringTrimming.EllipsisCharacter };
        private readonly Dictionary<string, Font> fontCache = new Dictionary<string, Font>();
        private readonly SolidBrush sharedMainBrush = new SolidBrush(Color.White);
        private readonly SolidBrush sharedShadowBrush = new SolidBrush(Color.Black);
        private readonly SolidBrush sharedFillBrush = new SolidBrush(Color.White);
        private readonly Pen sharedRimPen = new Pen(Color.White, 1f);
        private readonly AutoResetEvent renderWakeEvent = new AutoResetEvent(false);
        private volatile bool hasActiveHoverTransition = false;
        private IntPtr cachedProgmanHwnd = IntPtr.Zero;
        private long lastRoiBackdropHash = 0;

        // Pre-baked Optical Glass Base Cache (recomputed ONLY when backdrop/position/theme changes; UI hover frames composite in <0.3ms!)
        private int[] cachedGlassBasePixels;
        private int lastGlassBaseBgSeq = -1;
        private int lastGlassBaseWinX = -99999;
        private int lastGlassBaseWinY = -99999;
        private bool lastGlassBaseBasic = false;
        private bool lastGlassBaseDark = false;
        private string lastGlassBaseTint = "";
        private bool lastGlassBaseGizmo = false;
        private int lastGlassBasePreset = -1;

        private IntPtr cachedMyHwnd = IntPtr.Zero;

        private void MarkDirty()
        {
            Interlocked.Exchange(ref dirtyFrame, 1);
            try { renderWakeEvent.Set(); } catch { }
        }

        // Live Search Bar State (Dashboard)
        private string searchQuery = "";
        private bool isSearchFocused = false;

        // Hero Spotlight Stepper + One-Click Copy Wish Feedback + Calendar Shared-Day Cycling
        private int heroSpotlightIndex = 0;
        private float wishCopiedUntilSec = -10f;
        private int calSharedPersonIndex = 0;
        private string editDayStr = "1";
        private string editorErrorMsg = "";

        private struct GlassMote
        {
            public float X, Y;
            public float SpeedY, DriftPhase, DriftAmp;
            public float Size;
            public float BaseAlpha;
        }
        private GlassMote[] ambientMotes;

        // Interactive Hit Regions
        private struct HitZone
        {
            public string Id;
            public RectangleF Bounds;
            public PersonEntry Person;
        }
        private volatile List<HitZone> hitZones = new List<HitZone>();
        private string hoveredZoneId = null;

        // UI View State + In-Glass Delete Confirmation Prompt
        private enum ActiveView { Dashboard, AddOrEditPerson, Settings }
        private ActiveView currentView = ActiveView.Dashboard;
        private PersonEntry pendingDeletePerson = null; // When non-null, shows the in-glass "ARE YOU SURE?" delete confirmation overlay!
        private string selectedFilter = "All";
        private int listScrollOffset = 0;

        // In-Glass Add/Edit Person Form State
        private PersonEntry editingPerson = null;
        private bool isNewPerson = true;
        private string editName = "";
        private int editDay = 1;
        private int editMonth = 1;
        private string editYearStr = "";
        private string editNotes = "";
        private int editCaretPos = 0;
        private string editIconType = "initials";
        private string editImagePath = "";
        private string activeTextField = "name";

        // Staged Settings State (for Cancel & Save Changes in Main Settings!)
        private int stagedUrgentDays = 3;
        private int stagedSoonDays = 14;
        private bool stagedShowAge = true;
        private bool stagedAutoStart = true;
        private int stagedPresetIndex = 0;
        private int stagedWidth = 780;
        private int stagedHeight = 500;
        private float stagedRadius = 50f;
        private int origPresetIndex = 0;
        private int origWidth = 780;
        private int origHeight = 500;
        private float origRadius = 50f;

        // Adaptive Background Luminance + Brightest Spot Color (0..255) behind the widget!
        private volatile float smoothedBgLuma = 55f;
        private volatile float smoothedPeakLuma = 55f;
        private volatile float smoothedPeakR = 55f;
        private volatile float smoothedPeakG = 60f;
        private volatile float smoothedPeakB = 72f;
        private bool stagedBasicTheme = false;
        private bool origBasicTheme = false;
        private bool stagedDarkMode = false;
        private bool origDarkMode = false;
        private bool stagedAppWindowMode = false;
        private bool origAppWindowMode = false;
        private bool stagedAdaptBright = false;
        private int stagedDarkenLevel = 1;
        private int stagedBrightAmp = 100;
        private bool origAdaptBright = false;
        private int origDarkenLevel = 1;
        private int origBrightAmp = 100;

        // Dashboard View Mode (List vs Calendar) & Filter Dropdown
        private bool isCalendarView = false;
        private int calYear = DateTime.Now.Year;
        private int calMonth = DateTime.Now.Month;
        private int calSelectedDay = DateTime.Now.Day;
        private string selectedSortFilter = "Nearest"; // Nearest, Furthest, Milestones, Alpha, Age
        private bool isFilterDropdownOpen = false;

        // In-Glass Right-Click Context Menu State (replaces default WinForms ContextMenuStrip!)
        private bool isContextMenuOpen = false;
        private PointF contextMenuPos = PointF.Empty;
        private float contextMenuProgress = 0.0f;

        // Settings Sidebar Categories
        private enum SettingsCategory { General, Appearance, Layers, Optics, Backup }
        private SettingsCategory currentSettingsCat = SettingsCategory.General;
        private string backupStatusMsg = "";

        // Staged Appearance Settings (for Cancel & Save Changes)
        private string stagedTint = "Clear";
        private string stagedFont = "Melon Squishy";
        private string stagedTextColor = "Auto";
        private string stagedLayout = "HeroLeft";
        private string origTint = "Clear";
        private string origFont = "Melon Squishy";
        private string origTextColor = "Auto";
        private string origLayout = "HeroLeft";

        // Interactive Mini-Preview Dragging State (Appearance Tab)
        private bool isDraggingPreviewBlock = false;
        private RectangleF lastPreviewBoxRect = RectangleF.Empty;
        private PointF previewDragCursor = PointF.Empty;

        // Staged Layer System Settings
        private string stagedLayerMode = "Above"; // Above or Below
        private bool stagedLayerDesktopIcons = true;
        private bool stagedLayerRainmeter = true;
        private bool stagedLayerAlwaysOnTop = false;
        private List<string> stagedLayerTargets = new List<string>();
        private string layerSearchQuery = "";
        private bool isLayerSearchFocused = false;
        private int layerListScroll = 0;
        private List<string> cachedRunningExes = new List<string>();
        private DateTime lastRunningExeRefresh = DateTime.MinValue;

        // Monitor Native Refresh Rate & Staged Performance Settings
        private int detectedMonitorHz = 60;
        private int stagedRefreshRate = 0; // 0 = Auto (Monitor Native Hz)
        private bool stagedRefreshWhileDragging = true;

        // Language & Modern Settings Controls (Numeric Near-Days Input + Language Dropdown)
        private string stagedLanguage = "EN";
        private bool isLangDropdownOpen = false;
        private bool isNearDaysFocused = false;
        private string stagedNearDaysText = "3";
        private string customSizeFocus = ""; // "", "w", "h"
        private string customWText = "780";
        private string customHText = "500";

        // Satisfying Click Sounds & Custom Cursor
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadCursorFromFile(string lpFileName);
        private bool stagedClickSounds = true;
        private string stagedCustomSoundPath = "";
        private string stagedCursorStyle = "Glass";
        private string stagedCustomCursorPath = "";
        private bool stagedUseCustomSize = false;
        private string langSearchQuery = "";
        private bool isLangSearchFocused = false;
        private int langScrollOffset = 0;
        private bool showResetConfirmModal = false;
        private static readonly string[,] AllLanguages = new string[,] {
            { "EN", "ENGLISH (EN)" }, { "DE", "DEUTSCH (DE)" }, { "ES", "ESPAÑOL (ES)" }, { "FR", "FRANÇAIS (FR)" },
            { "IT", "ITALIANO (IT)" }, { "PT", "PORTUGUÊS (PT)" }, { "NL", "NEDERLANDS (NL)" }, { "PL", "POLSKI (PL)" },
            { "RU", "РУССКИЙ (RU)" }, { "UK", "УКРАЇНСЬКА (UK)" }, { "TR", "TÜRKÇE (TR)" }, { "AR", "العربية (AR)" },
            { "HI", "हिन्दी (HI)" }, { "ZH", "中文 (ZH)" }, { "JA", "日本語 (JA)" }, { "KO", "한국어 (KO)" },
            { "SV", "SVENSKA (SV)" }, { "NO", "NORSK (NO)" }, { "DA", "DANSK (DA)" }, { "FI", "SUOMI (FI)" },
            { "CS", "ČEŠTINA (CS)" }, { "SK", "SLOVENČINA (SK)" }, { "HU", "MAGYAR (HU)" }, { "RO", "ROMÂNĂ (RO)" },
            { "BG", "БЪЛГАРСКИ (BG)" }, { "HR", "HRVATSKI (HR)" }, { "SR", "СРПСКИ (SR)" }, { "EL", "ΕΛΛΗΝΙΚΑ (EL)" },
            { "HE", "עברית (HE)" }, { "TH", "ไทย (TH)" }, { "VI", "TIẾNG VIỆT (VI)" }, { "ID", "BAHASA INDONESIA (ID)" },
            { "MS", "BAHASA MELAYU (MS)" }, { "TL", "TAGALOG (TL)" }, { "FA", "فارسی (FA)" }, { "SW", "KISWAHILI (SW)" },
            { "LA", "LATINA (LA)" }, { "EO", "ESPERANTO (EO)" },
            { "PIR", "PIRATE SPEAK (ARRR)" }, { "TLH", "KLINGON (tlhIngan)" }, { "QYA", "ELVISH / QUENYA" },
            { "VAL", "HIGH VALYRIAN" }, { "DOT", "DOTHRAKI" }, { "YOD", "YODA SPEAK" }, { "MIN", "MINIONESE" }
        };
        private string GetLanguageDisplayName(string code)
        {
            for (int i = 0; i < AllLanguages.GetLength(0); i++)
                if (AllLanguages[i, 0] == code) return AllLanguages[i, 1];
            return "ENGLISH (EN)";
        }
        private byte[] builtInClickWavBytes = null;
        private Cursor userCustomCursor = null;

        private byte[] BuildSoftGlassClickWav()
        {
            const int sampleRate = 22050;
            const int numSamples = 485; // ~22ms crisp bubble pop
            byte[] wav = new byte[44 + numSamples * 2];
            using (MemoryStream ms = new MemoryStream(wav))
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                bw.Write(Encoding.ASCII.GetBytes("RIFF"));
                bw.Write(36 + numSamples * 2);
                bw.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
                bw.Write(16);
                bw.Write((short)1); // PCM
                bw.Write((short)1); // Mono
                bw.Write(sampleRate);
                bw.Write(sampleRate * 2);
                bw.Write((short)2);
                bw.Write((short)16);
                bw.Write(Encoding.ASCII.GetBytes("data"));
                bw.Write(numSamples * 2);
                double phase = 0.0;
                for (int i = 0; i < numSamples; i++)
                {
                    double t = (double)i / sampleRate;
                    double env = Math.Sin(Math.Min(1.0, t / 0.0022) * Math.PI * 0.5) * Math.Exp(-t * 175.0);
                    double freq = 240.0 + 760.0 * (1.0 - Math.Exp(-t * 210.0));
                    phase += 2.0 * Math.PI * freq / sampleRate;
                    double sig = Math.Sin(phase);
                    short s = (short)ClampInt((int)(sig * env * 18500.0), -32767, 32767);
                    bw.Write(s);
                }
            }
            return wav;
        }

        private void PlaySatisfyingClickSound()
        {
            bool enabled = (currentView == ActiveView.Settings) ? stagedClickSounds : (config == null || config.EnableClickSounds);
            if (!enabled) return;
            string customPath = (currentView == ActiveView.Settings) ? stagedCustomSoundPath : (config != null ? config.CustomClickSoundPath : "");
            try
            {
                if (!string.IsNullOrEmpty(customPath) && File.Exists(customPath))
                {
                    using (System.Media.SoundPlayer sp = new System.Media.SoundPlayer(customPath))
                    {
                        sp.Play();
                    }
                    return;
                }
                if (builtInClickWavBytes == null) builtInClickWavBytes = BuildSoftGlassClickWav();
                MemoryStream ms = new MemoryStream(builtInClickWavBytes, false);
                System.Media.SoundPlayer p = new System.Media.SoundPlayer(ms);
                p.Play();
            }
            catch { }
        }

        private void ApplyCustomCursor(string path)
        {
            try
            {
                string style = (currentView == ActiveView.Settings) ? stagedCursorStyle : (config != null && !string.IsNullOrEmpty(config.CursorStyle) ? config.CursorStyle : "Glass");
                if (userCustomCursor != null) { try { userCustomCursor.Dispose(); } catch { } userCustomCursor = null; }
                if (style == "System")
                {
                    this.Cursor = Cursors.Default;
                    return;
                }
                if (style == "Custom" && !string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    string ext = Path.GetExtension(path).ToLowerInvariant();
                    if (ext == ".cur" || ext == ".ani")
                    {
                        IntPtr hCur = LoadCursorFromFile(path);
                        if (hCur != IntPtr.Zero) userCustomCursor = new Cursor(hCur);
                    }
                    else
                    {
                        using (Bitmap raw = new Bitmap(path))
                        using (Bitmap resized = new Bitmap(raw, new Size(32, 32)))
                        {
                            userCustomCursor = new Cursor(resized.GetHicon());
                        }
                    }
                }
                this.Cursor = userCustomCursor ?? curGlassDefault ?? Cursors.Default;
            }
            catch { }
        }

        private string GetActiveLanguage()
        {
            if (currentView == ActiveView.Settings && !string.IsNullOrEmpty(stagedLanguage)) return stagedLanguage;
            if (config != null && !string.IsNullOrEmpty(config.Language)) return config.Language;
            return "EN";
        }

        private static readonly Dictionary<string, string[]> LangDict = new Dictionary<string, string[]>
        {
            // Order: 0=GENERAL, 1=APPEARANCE, 2=WINDOW, 3=LAYERS, 4=BACKUP, 5=SETTINGS, 6=CANCEL, 7=SAVE CHANGES, 8=COPY WISH, 9=WISH, 10=COPIED ✓, 11=EDIT, 12=GENERAL & BIRTHDAYS, 13=LANGUAGE, 14=VERY NEAR HIGHLIGHT (DAYS):, 15=DISPLAY & STARTUP, 16=SHOW AGE BADGE, 17=AUTO-START WITH PC, 18=BIRTHDAY ANIMATION, 19=BIRTHDAY MESSAGE, 20=WINDOW & PERFORMANCE, 21=REAL WINDOW MODE (TASKBAR), 22=LIVE BACKGROUND ON DRAG
            { "DE", new string[] { "ALLGEMEIN","AUSSEHEN","FENSTER","EBENEN","BACKUP","EINSTELLUNGEN","ABBRECHEN","SPEICHERN","WUNSCH KOPIEREN","WUNSCH","KOPIERT ✓","BEARBEITEN","ALLGEMEIN & GEBURTSTAGE","SPRACHE","BALD-HIGHLIGHT (TAGE):","ANZEIGE & AUTOSTART","ALTER ANZEIGEN","MIT PC STARTEN","GEBURTSTAGS-ANIMATION","GEBURTSTAGS-BANNER","FENSTER & LEISTUNG","ECHTES FENSTER (TASKLEISTE)","HINTERGRUND BEIM ZIEHEN" } },
            { "ES", new string[] { "GENERAL","APARIENCIA","VENTANA","CAPAS","RESPALDO","AJUSTES","CANCELAR","GUARDAR","COPIAR DESEO","DESEO","COPIADO ✓","EDITAR","GENERAL Y CUMPLEAÑOS","IDIOMA","AVISO CERCANO (DÍAS):","PANTALLA E INICIO","MOSTRAR EDAD","INICIAR CON PC","ANIMACIÓN CUMPLE","MENSAJE CUMPLE","VENTANA Y RENDIMIENTO","MODO VENTANA REAL","FONDO AL ARRASTRAR" } },
            { "FR", new string[] { "GÉNÉRAL","APPARENCE","FENÊTRE","CALQUES","SAUVEGARDE","PARAMÈTRES","ANNULER","ENREGISTRER","COPIER VŒU","VŒU","COPIÉ ✓","MODIFIER","GÉNÉRAL & ANNIVERSAIRES","LANGUE","ALERTE PROCHE (JOURS):","AFFICHAGE & DÉMARRAGE","AFFICHER ÂGE","DÉMARRER AVEC PC","ANIMATION FÊTE","MESSAGE FÊTE","FENÊTRE & PERFORMANCE","MODE FENÊTRE RÉELLE","FOND AU GLISSER" } },
            { "IT", new string[] { "GENERALE","ASPETTO","FINESTRA","LIVELLI","BACKUP","IMPOSTAZIONI","ANNULLA","SALVA","COPIA AUGURI","AUGURI","COPIATO ✓","MODIFICA","GENERALE E COMPLEANNI","LINGUA","AVVISO VICINO (GIORNI):","SCHERMO E AVVIO","MOSTRA ETÀ","AVVIO CON PC","ANIMAZIONE FESTA","MESSAGGIO FESTA","FINESTRA E PRESTAZIONI","MODO FINESTRA REALE","SFONDO AL TRASCINARE" } },
            { "PT", new string[] { "GERAL","APARÊNCIA","JANELA","CAMADAS","BACKUP","CONFIGURAÇÕES","CANCELAR","SALVAR","COPIAR DESEJO","DESEJO","COPIADO ✓","EDITAR","GERAL E ANIVERSÁRIOS","IDIOMA","DESTAQUE PRÓXIMO (DIAS):","TELA E INÍCIO","MOSTRAR IDADE","INICIAR COM PC","ANIMAÇÃO FESTA","MENSAGEM FESTA","JANELA E DESEMPENHO","MODO JANELA REAL","FUNDO AO ARRASTAR" } },
            { "NL", new string[] { "ALGEMEEN","UITERLIJK","VENSTER","LAGEN","BACKUP","INSTELLINGEN","ANNULEREN","OPSLAAN","WENS KOPIËREN","WENS","GEKOPIEERD ✓","BEWERKEN","ALGEMEEN & VERJAARDAGEN","TAAL","BIJNA JARIG (DAGEN):","WEERGAVE & START","LEEFTIJD TONEN","START MET PC","VERJAARDAG ANIMATIE","VERJAARDAG BERICHT","VENSTER & PRESTATIES","ECHT VENSTER","ACHTERGROND BIJ SLEPEN" } },
            { "PL", new string[] { "OGÓLNE","WYGLĄD","OKNO","WARSTWY","KOPIA","USTAWIENIA","ANULUJ","ZAPISZ","KOPIUJ ŻYCZENIA","ŻYCZENIA","SKOPIOWANO ✓","EDYTUJ","OGÓLNE I URODZINY","JĘZYK","BLISKO URODZIN (DNI):","WYGLĄD I START","POKAŻ WIEK","AUTOSTART PC","ANIMACJA URODZIN","WIADOMOŚĆ URODZIN","OKNO I WYDAJNOŚĆ","TRYB OKNA","TŁO PRZY PRZECIĄGANIU" } },
            { "RU", new string[] { "ОБЩИЕ","ВИД","ОКНО","СЛОИ","БЭКАП","НАСТРОЙКИ","ОТМЕНА","СОХРАНИТЬ","КОПИРОВАТЬ","ПОЖЕЛАНИЕ","СКОПИРОВАНО ✓","ИЗМЕНИТЬ","ОБЩИЕ И ДНИ РОЖДЕНИЯ","ЯЗЫК","НАПОМИНАНИЕ (ДНИ):","ЭКРАН И ЗАПУСК","ПОКАЗАТЬ ВОЗРАСТ","АВТОЗАПУСК","АНИМАЦИЯ","БАННЕР","ОКНО И FPS","РЕЖИМ ОКНА","ФОН ПРИ ПЕРЕТАСКИВАНИИ" } },
            { "UK", new string[] { "ЗАГАЛЬНІ","ВИГЛЯД","ВІКНО","ШАРИ","БЕКАП","НАЛАШТУВАННЯ","СКАСУВАТИ","ЗБЕРЕГТИ","КОПІЮВАТИ","ПРИВІТАННЯ","СКОПІЙОВАНО ✓","ЗМІНИТИ","ЗАГАЛЬНІ ТА ДНІ НАРОДЖЕННЯ","МОВА","НАГАДУВАННЯ (ДНІ):","ЕКРАН ТА ЗАПУСК","ПОКАЗАТИ ВІК","АВТОЗАПУСК","АНІМАЦІЯ","БАНЕР","ВІКНО ТА FPS","РЕЖИМ ВІКНА","ФОН ПРИ ПЕРЕТЯГУВАННІ" } },
            { "TR", new string[] { "GENEL","GÖRÜNÜM","PENCERE","KATMANLAR","YEDEK","AYARLAR","İPTAL","KAYDET","DİLEK KOPYALA","DİLEK","KOPYALANDI ✓","DÜZENLE","GENEL VE DOĞUM GÜNLERİ","DİL","YAKIN UYARI (GÜN):","EKRAN VE BAŞLATMA","YAŞI GÖSTER","PC İLE BAŞLAT","DOĞUM GÜNÜ ANİMASYONU","DOĞUM GÜNÜ MESAJI","PENCERE VE PERFORMANS","GERÇEK PENCERE","SÜRÜKLERKEN ARKA PLAN" } },
            { "JA", new string[] { "一般","外観","ウィンドウ","レイヤー","バックアップ","設定","キャンセル","保存","メッセージをコピー","お祝い","コピー完了 ✓","編集","一般・誕生日","言語","直前ハイライト (日):","表示と起動","年齢を表示","PC起動時に開始","誕生日アニメ","誕生日メッセージ","ウィンドウと性能","通常ウィンドウ","ドラッグ中に更新" } },
            { "ZH", new string[] { "常规","外观","窗口","图层","备份","设置","取消","保存更改","复制祝福","祝福","已复制 ✓","编辑","常规与生日","语言","临近提醒 (天):","显示与启动","显示年龄","开机自启","生日动画","生日横幅","窗口与性能","真实窗口模式","拖动时刷新背景" } },
            { "KO", new string[] { "일반","외관","창","레이어","백업","설정","취소","저장","축하 복사","축하","복사됨 ✓","편집","일반 및 생일","언어","임박 강조 (일):","표시 및 시작","나이 표시","PC 시작 시 실행","생일 애니메이션","생일 메시지","창 및 성능","일반 창 모드","드래그 시 배경 갱신" } },
            { "AR", new string[] { "عام","المظهر","النافذة","الطبقات","النسخ","الإعدادات","إلغاء","حفظ","نسخ التهنئة","تهنئة","تم النسخ ✓","تعديل","عام وأعياد الميلاد","اللغة","تنبيه قريب (أيام):","العرض والبدء","إظهار العمر","بدء مع النظام","حركة العيد","رسالة العيد","النافذة والأداء","وضع النافذة","تحديث الخلفية" } },
            { "HI", new string[] { "सामान्य","रूप","विंडो","लेयर्स","बैकअप","सेटिंग्स","रद्द करें","सहेजें","शुभकामना कॉपी","शुभकामना","कॉपी किया ✓","संपादित","सामान्य और जन्मदिन","भाषा","निकट अलर्ट (दिन):","प्रदर्शन और स्टार्ट","आयु दिखाएं","PC के साथ शुरू","जन्मदिन एनीमेशन","जन्मदिन संदेश","विंडो और प्रदर्शन","रियल विंडो मोड","खींचते समय पृष्ठभूमि" } },
            { "SV", new string[] { "ALLMÄNT","UTSEENDE","FÖNSTER","LAGER","BACKUP","INSTÄLLNINGAR","AVBRYT","SPARA","KOPIERA ÖNSKAN","ÖNSKAN","KOPIERAD ✓","REDIGERA","ALLMÄNT & FÖDELSEDAGAR","SPRÅK","NÄRA VARNING (DAGAR):","VISNING & START","VISA ÅLDER","AUTOSTARTA","FÖDELSEDAGSANIMATION","FÖDELSEDAGSMEDDELANDE","FÖNSTER & PRESTANDA","RIKTIGT FÖNSTER","BAKGRUND VID DRAG" } },
            { "NO", new string[] { "GENERELT","UTSEENDE","VINDU","LAG","BACKUP","INNSTILLINGER","AVBRYT","LAGRE","KOPIER HILSEN","HILSEN","KOPIERT ✓","REDIGER","GENERELT & BURSDAGER","SPRÅK","NÆR VARSEL (DAGER):","VISNING & START","VIS ALDER","AUTOSTART","BURSDAGSANIMASJON","BURSDAGSMELDING","VINDU & YTELSE","EKTE VINDU","BAKGRUNN VED DRAING" } },
            { "DA", new string[] { "GENERELT","UDSEENDE","VINDUE","LAG","BACKUP","INDSTILLINGER","ANNULLER","GEM","KOPIER HILSEN","HILSEN","KOPIERET ✓","REDIGER","GENERELT & FØDSELSDAGE","SPROG","NÆR ADVARSEL (DAGE):","VISNING & START","VIS ALDER","AUTOSTART","FØDSELSDAGSANIMATION","FØDSELSDAGSBESKED","VINDUE & YDEEVNE","RIGTIGT VINDUE","BAGGRUND VED TRÆK" } },
            { "FI", new string[] { "YLEISET","ULKOASU","IKKUNA","TASOT","VARMUUS","ASETUKSET","PERUUTA","TALLENNA","KOPIOI TOIVOTUS","TOIVOTUS","KOPIOITU ✓","MUOKKAA","YLEISET & SYNTYMÄPÄIVÄT","KIELI","LÄHELLÄ (PÄIVÄÄ):","NÄYTTÖ & KÄYNNISTYS","NÄYTÄ IKÄ","KÄYNNISTÄ PC:LLÄ","SYNTYMÄPÄIVÄANIMAATIO","SYNTYMÄPÄIVÄVIESTI","IKKUNA & TEHO","OIKEA IKKUNA","TAUSTA VEDETTÄESSÄ" } },
            { "CS", new string[] { "OBECNÉ","VZHLED","OKNO","VRSTVY","ZÁLOHA","NASTAVENÍ","ZRUŠIT","ULOŽIT","KOPÍROVAT PŘÁNÍ","PŘÁNÍ","ZKOPÍROVÁNO ✓","UPRAVIT","OBECNÉ A NAROZENINY","JAZYK","BLÍZKO (DNŮ):","ZOBRAZENÍ A START","ZOBRAZIT VĚK","SPUSTIT S PC","ANIMACE NAROZENIN","ZPRÁVA NAROZENIN","OKNO A VÝKON","REŽIM OKNA","POZADÍ PŘI TAŽENÍ" } },
            { "SK", new string[] { "VŠEOBECNÉ","VZHĽAD","OKNO","VRSTVY","ZÁLOHA","NASTAVENIA","ZRUŠIŤ","ULOŽIŤ","KOPÍROVAŤ PRIANIE","PRIANIE","SKOPÍROVANÉ ✓","UPRAVIŤ","VŠEOBECNÉ A NARODENINY","JAZYK","BLÍZKO (DNÍ):","ZOBRAZENIE A ŠTART","ZOBRAZIŤ VEK","SPUSTIŤ S PC","ANIMÁCIA NARODENÍN","SPRÁVA NARODENÍN","OKNO A VÝKON","REŽIM OKNA","POZADIE PRI ŤAHANÍ" } },
            { "HU", new string[] { "ÁLTALÁNOS","MEGJELENÉS","ABLAK","RÉTEGEK","MENTÉS","BEÁLLÍTÁSOK","MÉGSE","MENTÉS","KÍVÁNSÁG MÁSOLÁSA","KÍVÁNSÁG","MÁSOLVA ✓","SZERKESZTÉS","ÁLTALÁNOS ÉS SZÜLINAPOK","NYELV","KÖZELI JELZÉS (NAP):","MEGJELENÍTÉS","KOR MUTATÁSA","INDÍTÁS PC-VEL","SZÜLINAP ANIMÁCIÓ","SZÜLINAP ÜZENET","ABLAK ÉS TELJESÍTMÉNY","VALÓDI ABLAK","HÁTTÉR HÚZÁSKOR" } },
            { "RO", new string[] { "GENERAL","ASPECT","FEREASTRĂ","STRATURI","BACKUP","SETĂRI","ANULEAZĂ","SALVEAZĂ","COPIAZĂ URAREA","URARE","COPIAT ✓","EDITEAZĂ","GENERAL ȘI ANIVERSĂRI","LIMBĂ","ALERTĂ APROAPE (ZILE):","AFIȘARE ȘI PORNIRE","ARATĂ VÂRSTA","PORNIRE CU PC","ANIMAȚIE ZI","MESAJ ZI","FEREASTRĂ ȘI PERFORMANȚĂ","MOD FEREASTRĂ","FUNDAL LA TRAGERE" } },
            { "BG", new string[] { "ОБЩИ","ИЗГЛЕД","ПРОЗОРЕЦ","СЛОЕВЕ","БЕКЪП","НАСТРОЙКИ","ОТКАЗ","ЗАПАЗИ","КОПИРАЙ","ПОЖЕЛАНИЕ","КОПИРАНО ✓","РЕДАКТИРАЙ","ОБЩИ И РОЖДЕНИ ДНИ","ЕЗИК","НАПОМНЯНЕ (ДНИ):","ИЗГЛЕД И СТАРТ","ПОКАЖИ ВЪЗРАСТ","СТАРТ С PC","АНИМАЦИЯ","СЪОБЩЕНИЕ","ПРОЗОРЕЦ","РЕАЛЕН ПРОЗОРЕЦ","ФОН ПРИ ВЛАЧЕНЕ" } },
            { "HR", new string[] { "OPĆENITO","IZGLED","PROZOR","SLOJEVI","SIG. KOPIJA","POSTAVKE","ODUSTANI","SPREMI","KOPIRAJ ŽELJU","ŽELJA","KOPIRANO ✓","UREDI","OPĆENITO I ROĐENDANI","JEZIK","BLIZU (DANA):","PRIKAZ I POKRETANJE","PRIKAŽI DOB","POKRETANJE S PC","ANIMACIJA","PORUKA","PROZOR I PERFORMANSE","PRAVI PROZOR","POZADINA PRI POVLAČENJU" } },
            { "SR", new string[] { "ОПШТЕ","ИЗГЛЕД","ПРОЗОР","СЛОЈЕВИ","БЕКАП","ПОДЕШАВАЊА","ОТКАЖИ","САЧУВАЈ","КОПИРАЈ ЖЕЉУ","ЖЕЉА","КОПИРАНО ✓","УРЕДИ","ОПШТЕ И РОЂЕНДАНИ","ЈЕЗИК","БЛИЗУ (ДАНА):","ПРИКАЗ И ПОКРЕТАЊЕ","ПРИКАЖИ ГОДИНЕ","ПОКРЕНИ СА PC","АНИМАЦИЈА","ПОРУКА","ПРОЗОР","ПРАВИ ПРОЗОР","ПОЗАДИНА ПРИ ВУЧЕЊУ" } },
            { "EL", new string[] { "ΓΕΝΙΚΑ","ΕΜΦΑΝΙΣΗ","ΠΑΡΑΘΥΡΟ","ΕΠΙΠΕΔΑ","ΑΝΤΙΓΡΑΦΟ","ΡΥΘΜΙΣΕΙΣ","ΑΚΥΡΩΣΗ","ΑΠΟΘΗΚΕΥΣΗ","ΑΝΤΙΓΡΑΦΗ ΕΥΧΗΣ","ΕΥΧΗ","ΑΝΤΙΓΡΑΦΗΚΕ ✓","ΕΠΕΞΕΡΓΑΣΙΑ","ΓΕΝΙΚΑ & ΓΕΝΕΘΛΙΑ","ΓΛΩΣΣΑ","ΚΟΝΤΙΝΗ ΕΙΔΟΠΟΙΗΣΗ (ΜΕΡΕΣ):","ΟΘΟΝΗ & ΕΚΚΙΝΗΣΗ","ΕΜΦΑΝΙΣΗ ΗΛΙΚΙΑΣ","ΕΚΚΙΝΗΣΗ ΜΕ PC","ΚΙΝΗΣΗ ΓΕΝΕΘΛΙΩΝ","ΜΗΝΥΜΑ ΓΕΝΕΘΛΙΩΝ","ΠΑΡΑΘΥΡΟ & ΑΠΟΔΟΣΗ","ΚΑΝΟΝΙΚΟ ΠΑΡΑΘΥΡΟ","ΦΟΝΤΟ ΣΤΟ ΣΥΡΣΙΜΟ" } },
            { "HE", new string[] { "כללי","מראה","חלון","שכבות","גיבוי","הגדרות","ביטול","שמור","העתק ברכה","ברכה","הועתק ✓","ערוך","כללי וימי הולדת","שפה","התראה קרובה (ימים):","תצוגה והפעלה","הצג גיל","הפעל עם המחשב","אנימציית יום הולדת","הודעת יום הולדת","חלון וביצועים","מצב חלון רגיל","רקע בגרירה" } },
            { "TH", new string[] { "ทั่วไป","รูปลักษณ์","หน้าต่าง","เลเยอร์","สำรอง","การตั้งค่า","ยกเลิก","บันทึก","คัดลอกคำอวยพร","คำอวยพร","คัดลอกแล้ว ✓","แก้ไข","ทั่วไปและวันเกิด","ภาษา","แจ้งเตือนใกล้ถึง (วัน):","การแสดงผล","แสดงอายุ","เปิดพร้อม PC","แอนิเมชันวันเกิด","ข้อความวันเกิด","หน้าต่างและประสิทธิภาพ","โหมดหน้าต่างจริง","พื้นหลังขณะลาก" } },
            { "VI", new string[] { "CHUNG","GIAO DIỆN","CỬA SỔ","LỚP","SAO LƯU","CÀI ĐẶT","HỦY","LƯU","SAO CHÉP LỜI CHÚC","LỜI CHÚC","ĐÃ SAO CHÉP ✓","SỬA","CHUNG & SINH NHẬT","NGÔN NGỮ","NHẮC GẦN (NGÀY):","HIỂN THỊ & KHỞI ĐỘNG","HIỆN TUỔI","CHẠY CÙNG PC","HIỆU ỨNG SINH NHẬT","THÔNG BÁO SINH NHẬT","CỬA SỔ & HIỆU NĂNG","CỬA SỔ THẬT","NỀN KHI KÉO" } },
            { "ID", new string[] { "UMUM","TAMPILAN","JENDELA","LAPISAN","CADANGAN","PENGATURAN","BATAL","SIMPAN","SALIN UCAPAN","UCAPAN","DISALIN ✓","UBAH","UMUM & ULANG TAHUN","BAHASA","SOROT DEKAT (HARI):","TAMPILAN & MULAI","TAMPILKAN USIA","MULAI DENGAN PC","ANIMASI ULTAH","PESAN ULTAH","JENDELA & PERFORMA","MODE JENDELA","LATAR SAAT SERET" } },
            { "MS", new string[] { "UMUM","PENAMPILAN","TINGKAP","LAPISAN","SANDARAN","TETAPAN","BATAL","SIMPAN","SALIN UCAPAN","UCAPAN","DISALIN ✓","SUNYING","UMUM & HARI LAHIR","BAHASA","AMARAN DEKAT (HARI):","PAPARAN & MULA","TUNJUK UMUR","MULA DENGAN PC","ANIMASI HARI LAHIR","MESEJ HARI LAHIR","TINGKAP & PRESTASI","MOD TINGKAP","LATAR SEMASA SERET" } },
            { "TL", new string[] { "PANGKALAHATAN","ANYO","BINTANA","MGA LAYER","BACKUP","MGA SETTING","KANSELAHIN","I-SAVE","KOPYAHIN ANG BATI","BATI","NAKOPYA ✓","I-EDIT","PANGKALAHATAN AT KAARAWAN","WIKA","MALAPIT NA PAALALA (ARAW):","DISPLAY AT STARTUP","IPAKITA ANG EDAD","SIMULA SA PC","ANIMASYON","MENSAHE","BINTANA AT BILIS","TUNAY NA BINTANA","BACKGROUND SA HILA" } },
            { "FA", new string[] { "عمومی","ظاهر","پنجره","لایه‌ها","پشتیبان","تنظیمات","لغو","ذخیره","کپی تبریک","تبریک","کپی شد ✓","ویرایش","عمومی و تولدها","زبان","هشدار نزدیک (روز):","نمایش و شروع","نمایش سن","شروع با ویندوز","انیمیشن تولد","پیام تولد","پنجره و عملکرد","حالت پنجره","پس‌زمینه هنگام کشیدن" } },
            { "SW", new string[] { "JUMLA","MUONEKANO","DIRISHA","TABAKA","HIFADHI","MIPANGILIO","GHAIRI","HIFADHI","NAKILI SALAMU","SALAMU","IMENAKILIWA ✓","HARIRI","JUMLA NA SIKU YA KUZALIWA","LUGHA","ARIFA KARIBU (SIKU):","ONYESHO NA MWANZO","ONYESHA UMRI","ANZA NA PC","UHUIISHAJI","UJUMBE","DIRISHA NA KASI","DIRISHA HALISI","USULI UKIBURUTA" } },
            { "LA", new string[] { "GENERALE","SPECIES","FENESTRA","STRATA","RESERVA","OPTIONES","ABROGA","SERVA","EXSCRIBE VOTUM","VOTUM","EXSCRIPTUM ✓","MUTE","GENERALE ET NATALES","LINGUA","PROPE DIES:","VISUS ET INITIUM","AETATEM OSTENDE","INCIPE CUM PC","ANIMATIO NATALIS","NUNTIUS NATALIS","FENESTRA ET VIS","FENESTRA VERA","FUNDUS IN TRACTU" } },
            { "EO", new string[] { "ĜENERALA","ASPEKTO","FENESTRO","TAVOLOJ","SEKURKOPIO","AGORDOJ","NULIGI","KONSERVI","KOPII DEZIRON","DEZIRO","KOPITA ✓","REDAKTI","ĜENERALA & NASKIĜTAGOJ","LINGVO","PROKSIMA (TAGOJ):","EKRANO & KOMENCO","MONTRI AĜON","KOMENCI KUN PC","NASKIĜTAGA ANIMACIO","NASKIĜTAGA MESAĜO","FENESTRO & RAPIDO","VERA FENESTRO","FONO DUM TRENO" } },
            { "PIR", new string[] { "CAP'N'S DECK","SHIP PAINT","PORTHOLE","DECKS","TREASURE CHEST","SHIP'S LOG","ABANDON","STOW LOOT","PLUNDER WISH","WISH","PLUNDERED ✓","SCRIBBLE","CAP'N'S DECK & B'DAYS","TONGUE","NEARBY LOOT (DAYS):","SPYGLASS & SAIL","SHOW YEARS","SAIL ON BOOT","PARTY CANNONS","PARROT MESSAGE","PORTHOLE & SPEED","REAL CABIN MODE","SEA WHILE DRAGGIN'" } },
            { "TLH", new string[] { "HOCH","QAB","QORWAGH","PATLH","POL","DUH","QIL","POL","QIN QON","QIN","QONTA' ✓","CHO'","HOCH & QO'","HOL","SUM (JAJ):","CHA' & TAGH","BEN CHA'","DEW'I' TAGH","QO' VIHTA'","QO' QIN","QORWAGH & HO'","QORWAGH NAQ","DUQ YIH" } },
            { "QYA", new string[] { "ILQUA","CANTË","HENET","TALMA","REHTIË","TECMAR","NÁVA","REHTA","TECA MERYA","MERYA","TECINA ✓","AHYA","ILQUA & NOSTARË","LAMBË","HARA (AURË):","CÉNA & YESTA","CÉNA YÉN","YESTA PC","NOSTARË LINDË","NOSTARË QUENTA","HENET & TÚRË","HENET ANWA","TALMA TULCA" } },
            { "VAL", new string[] { "TOLVIE","SYZ","VĒZOS","BANTOS","KESSA","GĒLENKA","DAOR","KESSA","RĪÑA","RĪÑA","RĪÑAGON ✓","HĒNKA","TOLVIE & RŌVYS","VALYRIO","TOLYS (TUBIS):","ŪNDĒGON","KĒLI","PC UMBAGON","RŌVYS VĒZOS","RŌVYS UDRIR","VĒZOS & KOSTOS","VĒZOS ANOGAR","VĒZOS DRAG" } },
            { "DOT", new string[] { "ATHCHOMAR","CHOMAK","VAES","SHIERAK","ZALAT","KHALA","VOS","ZALAT","ASTAT","ASTAT","ASTAT ✓","FIN","ATHCHOMAR & SHIERAK","LEKH","ASSHEKH (ASAVVA):","TIHAT","SHIERAK","PC TIHAT","SHIERAK ZALAT","SHIERAK AST","VAES & ZALAT","VAES ANHA","VAES DRAG" } },
            { "YOD", new string[] { "GENERAL, IT IS","LOOKS, THE","WINDOW, THE","LAYERS, MANY","BACKUP, YOUR","SETTINGS, THESE","CANCEL, YOU MUST","SAVE, DO","WISH, COPY","WISH","COPIED, IT IS ✓","EDIT, YOU MAY","BIRTHDAYS & GENERAL","LANGUAGE, YOUR","NEAR HIGHLIGHT (DAYS):","DISPLAY & STARTUP","AGE BADGE, SHOW","WITH PC, AUTO-START","ANIMATION, BIRTHDAY","MESSAGE, BIRTHDAY","PERFORMANCE & WINDOW","REAL WINDOW, USE","ON DRAG, REFRESH" } },
            { "MIN", new string[] { "BANANA","BELLO LOOK","POOPAYE WIN","LAYA","BAPPLE SAVE","TULALILOO","NOPE","OKAY SAVE","COPY PAPOI","PAPOI","COPIED BANANA ✓","EDITO","BANANA & BDAY","LINGO","NEAR DAYS:","SHOW & GO","SHOW AGE","AUTO PC","BDAY PARTY","BDAY BELLO","WINDOW SPEED","REAL WINDOW","DRAGGY BG" } }
        };

        private static readonly string[] LangKeys = new string[] {
            "GENERAL","APPEARANCE","WINDOW","LAYERS","BACKUP","SETTINGS","CANCEL","SAVE CHANGES","COPY WISH","WISH","COPIED ✓","EDIT",
            "GENERAL & BIRTHDAYS","LANGUAGE","VERY NEAR HIGHLIGHT (DAYS):","DISPLAY & STARTUP","SHOW AGE BADGE","AUTO-START WITH PC",
            "BIRTHDAY ANIMATION","BIRTHDAY MESSAGE","WINDOW & PERFORMANCE","REAL WINDOW MODE (TASKBAR)","LIVE BACKGROUND ON DRAG"
        };

        private string L(string key)
        {
            string lang = GetActiveLanguage();
            if (lang == "EN") return key;
            string[] vals;
            if (LangDict.TryGetValue(lang, out vals))
            {
                for (int i = 0; i < LangKeys.Length && i < vals.Length; i++)
                {
                    if (LangKeys[i] == key) return vals[i];
                }
            }
            return key;
        }

        private GraphicsPath RoundedRectPath(RectangleF r, float rad)
        {
            GraphicsPath p = new GraphicsPath();
            float d = Math.Max(2f, Math.Min(rad * 2f, Math.Min(r.Width, r.Height)));
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        private Color GetThemeFillColor(int alpha)
        {
            return GetCardSurfaceColor(ClampInt(alpha, 0, 255));
        }

        private void DrawDisabledGlassButton(Graphics g, RectangleF rect, string text)
        {
            float rad = IsBasicThemeEnabled() ? Math.Min(7f, rect.Height * 0.35f) : (rect.Height * 0.5f);
            using (GraphicsPath path = RoundedRectPath(rect, rad))
            {
                Color fill = IsDarkModeEnabled() ? Color.FromArgb(24, 180, 185, 200) : Color.FromArgb(26, 120, 130, 145);
                Color rim = IsDarkModeEnabled() ? Color.FromArgb(45, 200, 205, 220) : Color.FromArgb(48, 120, 130, 145);
                using (SolidBrush br = new SolidBrush(fill)) g.FillPath(br, path);
                using (Pen p = new Pen(rim, 1.0f)) g.DrawPath(p, path);
            }
            Font bf = GetCachedFont(Math.Max(9.5f, Math.Min(11.5f, rect.Height * 0.38f)), FontStyle.Bold);
            using (SolidBrush tb = new SolidBrush(GetTextColor(95)))
            {
                g.DrawString(text, bf, tb, rect, sfCenter);
            }
        }

        private void DrawGlassToggleSwitch(Graphics g, RectangleF rowRect, string label, bool isOn, float hoverProg)
        {
            float rad = IsBasicThemeEnabled() ? 7f : (rowRect.Height * 0.5f);
            using (GraphicsPath rowPath = RoundedRectPath(rowRect, rad))
            {
                Color bg = GetThemeFillColor(ClampInt((isOn ? 22 : 12) + (int)(hoverProg * 16), 0, 255));
                Color rim = GetThemeRimColor(ClampInt((isOn ? 115 : 75) + (int)(hoverProg * 30), 0, 255));
                using (SolidBrush br = new SolidBrush(bg)) g.FillPath(br, rowPath);
                using (Pen p = new Pen(rim, 1.0f)) g.DrawPath(p, rowPath);
            }

            float trackW = 40f;
            float trackH = 20f;
            RectangleF trackRect = new RectangleF(rowRect.Right - trackW - 10f, rowRect.Y + (rowRect.Height - trackH) * 0.5f, trackW, trackH);
            using (GraphicsPath tp = RoundedRectPath(trackRect, trackH * 0.5f))
            {
                Color trackFill = isOn
                    ? GetCardSurfaceColor(IsDarkModeEnabled() ? 64 : 72)
                    : GetCardSurfaceColor(IsDarkModeEnabled() ? 20 : 24);
                using (SolidBrush tbr = new SolidBrush(trackFill)) g.FillPath(tbr, tp);
                using (Pen tpen = new Pen(GetThemeRimColor(isOn ? 175 : 85), isOn ? 1.25f : 1.0f)) g.DrawPath(tpen, tp);
            }

            float knobSize = trackH - 6f;
            float knobX = isOn ? (trackRect.Right - knobSize - 3f) : (trackRect.X + 3f);
            RectangleF knobRect = new RectangleF(knobX, trackRect.Y + 3f, knobSize, knobSize);
            Color knobCol = GetThemeRimColor(isOn ? 215 : 115);
            using (SolidBrush kbr = new SolidBrush(knobCol))
            {
                g.FillEllipse(kbr, knobRect);
            }

            Font lf = GetCachedFont(10.5f, FontStyle.Bold);
            RectangleF txtRect = new RectangleF(rowRect.X + 12f, rowRect.Y, Math.Max(40f, trackRect.X - rowRect.X - 18f), rowRect.Height);
            DrawAdaptiveString(g, label, lf, txtRect, sfNearCenter, 235);
        }

        // Persistent Output GDI DIB Section
        private IntPtr persistentScreenDc = IntPtr.Zero;
        private IntPtr persistentMemDc = IntPtr.Zero;
        private IntPtr persistentDib = IntPtr.Zero;
        private IntPtr persistentOldBmp = IntPtr.Zero;
        private IntPtr persistentBitsPtr = IntPtr.Zero;
        private readonly object renderLock = new object();

        private Thread liveCaptureThread;
        private Thread vsyncDragThread;
        private System.Windows.Forms.Timer desktopCheckTimer;
        private ContextMenuStrip contextMenu;
        private WidgetConfig config;

        public GlassWidgetForm(bool isPreviewMode)
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.Manual;
            this.Text = "Birthly";
            this.KeyPreview = true;

            Directory.CreateDirectory(appDir);
            Directory.CreateDirectory(avatarsDir);

            try { timeBeginPeriod(1); } catch { }
            detectedMonitorHz = DetectMonitorRefreshRate();

            LoadCustomMondFonts();
            BuildCustomGlassCursors();
            InitAmbientGlassMotes();
            LoadOrInitializeConfig();

            widgetWidth = config.WidgetWidth >= 680 ? config.WidgetWidth : 780;
            widgetHeight = config.WidgetHeight >= 440 ? config.WidgetHeight : 500;
            cornerRadius = widgetWidth <= 710 ? 46f : (widgetWidth >= 860 ? 54f : 50f);
            ApplyPresetValues(config.GlassPresetIndex);

            surfaceW = widgetWidth + edgeMargin * 2;
            surfaceH = widgetHeight + edgeMargin * 2;
            this.Size = new Size(surfaceW, surfaceH);

            Rectangle screen = Screen.PrimaryScreen.WorkingArea;
            if (config.WindowX != -9999 && config.WindowY != -9999)
            {
                currentWinX = config.WindowX;
                currentWinY = config.WindowY;
            }
            else
            {
                currentWinX = screen.Left + (screen.Width - surfaceW) / 2;
                currentWinY = screen.Top + (int)(screen.Height * 0.56f) - surfaceH / 2;
            }
            this.Location = new Point(currentWinX, currentWinY);

            virtualScreenRect = SystemInformation.VirtualScreen;
            deskW = virtualScreenRect.Width;
            deskH = virtualScreenRect.Height;

            AllocatePersistentCaptureBuffers();
            AllocatePersistentDIB();
            BuildContextMenu();
            PrecomputeOpticalTables();
            RebuildUIOverlay(0.016f);
            CaptureLiveDesktopFrameFast();

            if (!isPreviewMode)
            {
                PinInFrontOfRainmeterClock();

                liveCaptureThread = new Thread(LiveCaptureLoop);
                liveCaptureThread.IsBackground = true;
                liveCaptureThread.Priority = ThreadPriority.BelowNormal;
                liveCaptureThread.Start();

                vsyncDragThread = new Thread(VSyncRenderLoop);
                vsyncDragThread.IsBackground = true;
                vsyncDragThread.Priority = ThreadPriority.Normal;
                vsyncDragThread.Start();

                desktopCheckTimer = new System.Windows.Forms.Timer();
                desktopCheckTimer.Interval = 1500;
                desktopCheckTimer.Tick += (s, e) => {
                    if (!isDragging && this.Visible && currentView == ActiveView.Dashboard)
                        PinInFrontOfRainmeterClock();
                };
                desktopCheckTimer.Start();
            }
        }

        private void AllocatePersistentCaptureBuffers()
        {
            int total = deskW * deskH;
            captureStagingA = new int[total];
            captureStagingB = new int[total];
            blurOutA = new int[total];
            blurOutB = new int[total];

            int hw = Math.Max(1, deskW / 2);
            int hh = Math.Max(1, deskH / 2);
            quarterBuf1 = new int[hw * hh];
            quarterBuf2 = new int[hw * hh];

            IntPtr sdc = GetDC(IntPtr.Zero);
            captureMemDc = CreateCompatibleDC(sdc);

            BITMAPINFO bmi = new BITMAPINFO();
            bmi.bmiHeader.biSize = (uint)Marshal.SizeOf(typeof(BITMAPINFOHEADER));
            bmi.bmiHeader.biWidth = deskW;
            bmi.bmiHeader.biHeight = -deskH;
            bmi.bmiHeader.biPlanes = 1;
            bmi.bmiHeader.biBitCount = 32;
            bmi.bmiHeader.biCompression = 0;

            captureDib = CreateDIBSection(sdc, ref bmi, 0, out captureBitsPtr, IntPtr.Zero, 0);
            if (captureDib != IntPtr.Zero)
            {
                captureOldBmp = SelectObject(captureMemDc, captureDib);
            }
            ReleaseDC(IntPtr.Zero, sdc);
        }

        private void FreePersistentCaptureBuffers()
        {
            if (captureDib != IntPtr.Zero)
            {
                SelectObject(captureMemDc, captureOldBmp);
                DeleteObject(captureDib);
                captureDib = IntPtr.Zero;
                captureBitsPtr = IntPtr.Zero;
            }
            if (captureMemDc != IntPtr.Zero)
            {
                DeleteDC(captureMemDc);
                captureMemDc = IntPtr.Zero;
            }
        }

        private void LoadCustomMondFonts()
        {
            try
            {
                string[] candidates = new string[] {
                    Path.Combine(installDir, @"fonts\MelonSquishy.otf"),
                    Path.Combine(installDir, @"fonts\Melon Squishy.otf"),
                    Path.Combine(appDir, @"fonts\MelonSquishy.otf")
                };
                for (int i = 0; i < candidates.Length; i++)
                {
                    if (File.Exists(candidates[i]))
                    {
                        try { AddFontResourceEx(candidates[i], 0x10, IntPtr.Zero); } catch { }
                        privateFonts.AddFontFile(candidates[i]);
                        break;
                    }
                }

                foreach (FontFamily ff in privateFonts.Families)
                {
                    string n = ff.Name.ToLowerInvariant();
                    if (n.Contains("melon")) melonSquishyFamily = ff;
                    else if (n.Contains("quicksand")) quicksandFamily = ff;
                    else if (n.Contains("aquatico")) aquaticoFamily = ff;
                    else if (n.Contains("anurati")) anuratiFamily = ff;
                }
            }
            catch { }
        }

        private string GetActiveFontName()
        {
            if (currentView == ActiveView.Settings && !string.IsNullOrEmpty(stagedFont)) return stagedFont;
            return (config != null && !string.IsNullOrEmpty(config.FontName)) ? config.FontName : "Melon Squishy";
        }

        private string GetActiveTint()
        {
            if (currentView == ActiveView.Settings && !string.IsNullOrEmpty(stagedTint)) return stagedTint;
            return (config != null && !string.IsNullOrEmpty(config.TintVariant)) ? config.TintVariant : "Clear";
        }

        private string GetActiveLayoutMode()
        {
            if (currentView == ActiveView.Settings && !string.IsNullOrEmpty(stagedLayout)) return stagedLayout;
            return (config != null && !string.IsNullOrEmpty(config.LayoutMode)) ? config.LayoutMode : "HeroLeft";
        }

        // Samples the exact background RGB pixel directly behind a widget-local point (accounting for active glass tint)
        private void SampleBgRgbAtPoint(float localX, float localY, out int r, out int g, out int b)
        {
            int[] buf = blurredPixels;
            if (buf == null || deskW <= 0 || deskH <= 0)
            {
                r = 60; g = 65; b = 80;
                return;
            }
            int px = (currentWinX - virtualScreenRect.X) + (int)localX;
            int py = (currentWinY - virtualScreenRect.Y) + (int)localY;
            if (px < 0) px = 0; else if (px >= deskW) px = deskW - 1;
            if (py < 0) py = 0; else if (py >= deskH) py = deskH - 1;

            int c = buf[py * deskW + px];
            r = (c >> 16) & 0xFF;
            g = (c >> 8) & 0xFF;
            b = c & 0xFF;

            int optTintR, optTintG, optTintB, optTintW;
            GetOpticalTintRgb(out optTintR, out optTintG, out optTintB, out optTintW);
            if (optTintW > 0)
            {
                int invTw = 256 - optTintW;
                r = (r * invTw + optTintR * optTintW) >> 8;
                g = (g * invTw + optTintG * optTintW) >> 8;
                b = (b * invTw + optTintB * optTintW) >> 8;
            }
        }

        private bool IsBasicThemeEnabled()
        {
            if (currentView == ActiveView.Settings) return stagedBasicTheme;
            return config != null && config.UseBasicTheme;
        }

        private bool IsDarkModeEnabled()
        {
            if (currentView == ActiveView.Settings) return stagedDarkMode;
            return config != null && config.DarkMode;
        }

        private bool IsBasicLightMode()
        {
            return IsBasicThemeEnabled() && !IsDarkModeEnabled();
        }

        private int GetBrightBgGlassDarken256()
        {
            // Pure crystal-clear Light Glass (0 darkening, just like before!), and sleek smoked dark glass (124) when Dark Glass is enabled!
            return IsDarkModeEnabled() ? 124 : 0;
        }

        private Color ComputePeakAdaptiveTextColor(int alpha)
        {
            int a = ClampInt(alpha, 0, 255);
            if (IsBasicLightMode())
            {
                int tr, tg, tb, tw;
                GetOpticalTintRgb(out tr, out tg, out tb, out tw);
                if (tw > 0 && GetActiveTint() != "Obsidian")
                {
                    return Color.FromArgb(a, 16 + (tr / 18), 22 + (tg / 18), 34 + (tb / 16));
                }
                return Color.FromArgb(a, 20, 26, 40);
            }
            return Color.FromArgb(a, 255, 255, 255);
        }

        private Color GetAdaptiveLetterColor(float localX, float localY, int alpha)
        {
            return ComputePeakAdaptiveTextColor(alpha);
        }

        // Crisp, ultra-readable text rendering (deep slate ink with subtle light highlight in Basic Light Mode, or pure white with crisp dark shadow in Glass / Dark Basic — ZERO brush allocations!)
        private void DrawAdaptiveString(Graphics g, string text, Font font, RectangleF rect, StringFormat sf, int alpha)
        {
            if (string.IsNullOrEmpty(text)) return;
            int a = ClampInt(alpha, 0, 255);
            if (IsBasicLightMode())
            {
                int hlA = (a * 140) / 255;
                if (hlA > 8)
                {
                    RectangleF hlRect = new RectangleF(rect.X, rect.Y + 1.0f, rect.Width, rect.Height);
                    sharedShadowBrush.Color = Color.FromArgb(ClampInt(hlA, 0, 255), 255, 255, 255);
                    g.DrawString(text, font, sharedShadowBrush, hlRect, sf);
                }
                sharedMainBrush.Color = ComputePeakAdaptiveTextColor(a);
                g.DrawString(text, font, sharedMainBrush, rect, sf);
                return;
            }
            int shadowA = IsBasicThemeEnabled() ? (a * 115) / 255 : (a * 185) / 255;
            if (shadowA > 8)
            {
                RectangleF shRect = new RectangleF(rect.X + 1.0f, rect.Y + 1.25f, rect.Width, rect.Height);
                sharedShadowBrush.Color = Color.FromArgb(ClampInt(shadowA, 0, 255), 8, 12, 20);
                g.DrawString(text, font, sharedShadowBrush, shRect, sf);
            }
            sharedMainBrush.Color = Color.FromArgb(a, 255, 255, 255);
            g.DrawString(text, font, sharedMainBrush, rect, sf);
        }

        private void DrawAdaptiveString(Graphics g, string text, Font font, float x, float y, int alpha)
        {
            if (string.IsNullOrEmpty(text)) return;
            DrawAdaptiveString(g, text, font, new RectangleF(x, y, 600f, 40f), sfNearNear, alpha);
        }

        private bool IsLocalDarkInk(float localX, float localY, float w, float h)
        {
            return false;
        }

        private bool IsLocalDarkInk(RectangleF r)
        {
            return false;
        }

        private bool IsAdaptiveDarkInk()
        {
            return false;
        }

        private Color GetTextColor(int alpha)
        {
            return ComputePeakAdaptiveTextColor(alpha);
        }

        private Color GetAdaptiveTextColorAt(RectangleF r, int alpha)
        {
            return GetAdaptiveLetterColor(r.X + r.Width * 0.5f, r.Y + r.Height * 0.5f, alpha);
        }

        private Color GetAdaptiveTextColorAt(float x, float y, float w, float h, int alpha)
        {
            return GetAdaptiveLetterColor(x + w * 0.5f, y + h * 0.5f, alpha);
        }

        // Areas, cards, and buttons NEVER turn dark — they stay pure clean frosted liquid glass!
        private Color GetCardSurfaceColorAt(RectangleF r, int alpha)
        {
            return GetCardSurfaceColor(alpha);
        }

        private Color GetCardSurfaceColor(int alpha)
        {
            int a = ClampInt(alpha, 0, 255);
            string tint = GetActiveTint();
            bool dark = IsDarkModeEnabled();
            bool basic = IsBasicThemeEnabled();

            if (basic && !dark)
            {
                // Basic Light Mode: subtle tinted slate-accented card & pill fills
                if (tint == "Ice") return Color.FromArgb(ClampInt(a + 28, 0, 255), 110, 175, 235);
                if (tint == "Rose") return Color.FromArgb(ClampInt(a + 28, 0, 255), 225, 130, 165);
                if (tint == "Mint") return Color.FromArgb(ClampInt(a + 28, 0, 255), 95, 195, 155);
                if (tint == "Amber") return Color.FromArgb(ClampInt(a + 28, 0, 255), 225, 165, 85);
                if (tint == "Violet") return Color.FromArgb(ClampInt(a + 28, 0, 255), 155, 125, 225);
                if (tint == "Obsidian") return Color.FromArgb(ClampInt(a + 24, 0, 255), 115, 128, 152);
                return Color.FromArgb(ClampInt(a + 22, 0, 255), 145, 162, 192);
            }

            if (tint == "Ice") return dark ? Color.FromArgb(ClampInt(a + 14, 0, 255), 65, 128, 185) : Color.FromArgb(ClampInt(a + 12, 0, 255), 165, 222, 255);
            if (tint == "Rose") return dark ? Color.FromArgb(ClampInt(a + 14, 0, 255), 168, 78, 118) : Color.FromArgb(ClampInt(a + 12, 0, 255), 255, 178, 210);
            if (tint == "Mint") return dark ? Color.FromArgb(ClampInt(a + 14, 0, 255), 65, 152, 122) : Color.FromArgb(ClampInt(a + 12, 0, 255), 165, 248, 215);
            if (tint == "Amber") return dark ? Color.FromArgb(ClampInt(a + 14, 0, 255), 168, 122, 58) : Color.FromArgb(ClampInt(a + 12, 0, 255), 255, 215, 155);
            if (tint == "Violet") return dark ? Color.FromArgb(ClampInt(a + 14, 0, 255), 122, 85, 185) : Color.FromArgb(ClampInt(a + 12, 0, 255), 205, 182, 255);
            if (tint == "Obsidian") return Color.FromArgb(ClampInt(a + 22, 0, 255), 24, 30, 42);
            if (basic) return Color.FromArgb(ClampInt(a + 18, 0, 255), 42, 52, 74);
            if (dark) return Color.FromArgb(ClampInt(a + 16, 0, 255), 32, 40, 56);
            return Color.FromArgb(a, 255, 255, 255);
        }

        // Uniform theme-tinted rim/border/divider/mote accent color so the selected Glass Color Variant is seamlessly implemented across EVERY area of the widget (cards, buttons, borders, dividers, motes, countdown tiles) without being overpowering!
        private Color GetThemeRimColor(int alpha)
        {
            int a = ClampInt(alpha, 0, 255);
            string tint = GetActiveTint();
            bool basicLight = IsBasicLightMode();
            if (basicLight)
            {
                if (tint == "Ice") return Color.FromArgb(a, 45, 108, 172);
                if (tint == "Rose") return Color.FromArgb(a, 165, 62, 102);
                if (tint == "Mint") return Color.FromArgb(a, 38, 132, 98);
                if (tint == "Amber") return Color.FromArgb(a, 162, 105, 32);
                if (tint == "Violet") return Color.FromArgb(a, 98, 68, 168);
                return Color.FromArgb(a, 72, 88, 118);
            }
            if (tint == "Ice") return Color.FromArgb(a, 200, 238, 255);
            if (tint == "Rose") return Color.FromArgb(a, 255, 208, 228);
            if (tint == "Mint") return Color.FromArgb(a, 198, 255, 232);
            if (tint == "Amber") return Color.FromArgb(a, 255, 232, 188);
            if (tint == "Violet") return Color.FromArgb(a, 226, 208, 255);
            if (tint == "Obsidian") return Color.FromArgb(a, 185, 202, 232);
            return Color.FromArgb(a, 255, 255, 255);
        }

        // Theme-aware popup/dropdown/modal fill color so dropdowns & popups match Basic Light, Basic Dark, Dark Glass, or Tinted Light Glass!
        private Color GetPopupBackdropFillColor(float progress = 1.0f)
        {
            float p = Clamp01(progress);
            bool basic = IsBasicThemeEnabled();
            bool dark = IsDarkModeEnabled();
            string tint = GetActiveTint();
            int tr, tg, tb, tw;
            GetOpticalTintRgb(out tr, out tg, out tb, out tw);

            if (basic)
            {
                int a = (int)(246 * p);
                if (!dark)
                {
                    int lr = 236, lg = 241, lb = 249;
                    if (tw > 0 && tint != "Obsidian")
                    {
                        lr = (lr * 208 + tr * 48) >> 8;
                        lg = (lg * 208 + tg * 48) >> 8;
                        lb = (lb * 208 + tb * 48) >> 8;
                    }
                    return Color.FromArgb(a, lr, lg, lb);
                }
                int dr = 16, dg = 20, db = 30;
                if (tw > 0)
                {
                    dr = (dr * 212 + tr * 44) >> 8;
                    dg = (dg * 212 + tg * 44) >> 8;
                    db = (db * 212 + tb * 44) >> 8;
                }
                return Color.FromArgb(a, dr, dg, db);
            }
            if (dark || tint == "Obsidian")
            {
                int a = (int)(176 * p);
                int dr = 18, dg = 23, db = 34;
                if (tw > 0 && tint != "Obsidian")
                {
                    dr = (dr * 210 + tr * 46) >> 8;
                    dg = (dg * 210 + tg * 46) >> 8;
                    db = (db * 210 + tb * 46) >> 8;
                }
                return Color.FromArgb(a, dr, dg, db);
            }
            int fr = 195, fg = 215, fb = 245;
            if (tw > 0)
            {
                fr = (fr * 160 + tr * 96) >> 8;
                fg = (fg * 160 + tg * 96) >> 8;
                fb = (fb * 160 + tb * 96) >> 8;
            }
            return Color.FromArgb((int)(68 * p), fr, fg, fb);
        }

        private void GetOpticalTintRgb(out int tr, out int tg, out int tb, out int extraWeight256)
        {
            string tint = GetActiveTint();
            if (tint == "Ice") { tr = 155; tg = 215; tb = 255; extraWeight256 = 46; return; }
            if (tint == "Rose") { tr = 255; tg = 165; tb = 200; extraWeight256 = 46; return; }
            if (tint == "Mint") { tr = 150; tg = 240; tb = 205; extraWeight256 = 44; return; }
            if (tint == "Amber") { tr = 255; tg = 208; tb = 135; extraWeight256 = 46; return; }
            if (tint == "Violet") { tr = 195; tg = 165; tb = 255; extraWeight256 = 48; return; }
            if (tint == "Obsidian") { tr = 18; tg = 22; tb = 32; extraWeight256 = 68; return; }
            tr = 245; tg = 250; tb = 255; extraWeight256 = 0;
        }

        private Font GetCachedFont(float sizePx, FontStyle style, bool isDisplayHeader = false)
        {
            string chosen = GetActiveFontName();
            int qSz = (int)(sizePx * 2f + 0.5f);
            string key = string.Concat(chosen, "|", qSz.ToString(), "|", ((int)style).ToString(), isDisplayHeader ? "|1" : "|0");
            Font cached;
            if (fontCache.TryGetValue(key, out cached) && cached != null)
            {
                return cached;
            }
            Font created = CreateFittingFont(sizePx, style, isDisplayHeader);
            fontCache[key] = created;
            return created;
        }

        private Font CreateFittingFont(float sizePx, FontStyle style, bool isDisplayHeader = false)
        {
            string chosen = GetActiveFontName();
            try
            {
                if (chosen == "Melon Squishy")
                {
                    // Melon Squishy is a decorative display font; for small labels/buttons (< 14.5px), use clean Segoe UI so every label is 100% crisp and readable!
                    if (!isDisplayHeader && sizePx < 14.5f)
                    {
                        return new Font("Segoe UI", sizePx, style, GraphicsUnit.Pixel);
                    }
                    if (melonSquishyFamily != null)
                    {
                        if (melonSquishyFamily.IsStyleAvailable(style))
                            return new Font(melonSquishyFamily, sizePx, style, GraphicsUnit.Pixel);
                        if (melonSquishyFamily.IsStyleAvailable(FontStyle.Regular))
                            return new Font(melonSquishyFamily, sizePx, FontStyle.Regular, GraphicsUnit.Pixel);
                    }
                }
                else
                {
                    return new Font(chosen, sizePx, style, GraphicsUnit.Pixel);
                }
                if (isDisplayHeader && aquaticoFamily != null && aquaticoFamily.IsStyleAvailable(FontStyle.Regular))
                {
                    return new Font(aquaticoFamily, sizePx, FontStyle.Regular, GraphicsUnit.Pixel);
                }
                if (quicksandFamily != null)
                {
                    if (quicksandFamily.IsStyleAvailable(style))
                        return new Font(quicksandFamily, sizePx, style, GraphicsUnit.Pixel);
                    if (quicksandFamily.IsStyleAvailable(FontStyle.Regular))
                        return new Font(quicksandFamily, sizePx, FontStyle.Regular, GraphicsUnit.Pixel);
                }
            }
            catch { }
            return new Font("Segoe UI", sizePx, style, GraphicsUnit.Pixel);
        }

        private void InitAmbientGlassMotes()
        {
            Random rng = new Random(42);
            ambientMotes = new GlassMote[22];
            for (int i = 0; i < ambientMotes.Length; i++)
            {
                ambientMotes[i] = new GlassMote
                {
                    X = (float)rng.NextDouble(),
                    Y = (float)rng.NextDouble(),
                    SpeedY = 0.012f + (float)rng.NextDouble() * 0.022f,
                    DriftPhase = (float)(rng.NextDouble() * Math.PI * 2.0),
                    DriftAmp = 0.015f + (float)rng.NextDouble() * 0.025f,
                    Size = 1.6f + (float)rng.NextDouble() * 2.6f,
                    BaseAlpha = 14f + (float)rng.NextDouble() * 26f
                };
            }
        }

        private void TriggerViewFade()
        {
            viewPopupProgress = 0.0f;
        }

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr CreateBitmap(int nWidth, int nHeight, uint cPlanes, uint cBitsPerPel, IntPtr lpvBits);

        private Cursor CreateCursorFromBitmap(Bitmap bmp, int hotX, int hotY)
        {
            int w = bmp.Width;
            int h = bmp.Height;
            int[] pixels = ExtractPixels(bmp);

            IntPtr screenDc = GetDC(IntPtr.Zero);
            IntPtr hDib = IntPtr.Zero;
            IntPtr hMask = IntPtr.Zero;
            try
            {
                // True 32-bit top-down ARGB DIBSection (preserves full 8-bit alpha gradients with ZERO 1-bit pixelation!)
                BITMAPINFO bmi = new BITMAPINFO();
                bmi.bmiHeader.biSize = (uint)Marshal.SizeOf(typeof(BITMAPINFOHEADER));
                bmi.bmiHeader.biWidth = w;
                bmi.bmiHeader.biHeight = -h;
                bmi.bmiHeader.biPlanes = 1;
                bmi.bmiHeader.biBitCount = 32;
                bmi.bmiHeader.biCompression = 0;

                IntPtr ppvBits;
                hDib = CreateDIBSection(screenDc, ref bmi, 0, out ppvBits, IntPtr.Zero, 0);
                if (hDib != IntPtr.Zero && ppvBits != IntPtr.Zero)
                {
                    Marshal.Copy(pixels, 0, ppvBits, pixels.Length);
                    // Pure 1-bit blank AND mask so Windows uses the 32-bit alpha channel smoothly
                    hMask = CreateBitmap(w, h, 1, 1, IntPtr.Zero);

                    ICONINFO ii = new ICONINFO();
                    ii.fIcon = false;
                    ii.xHotspot = hotX;
                    ii.yHotspot = hotY;
                    ii.hbmMask = hMask;
                    ii.hbmColor = hDib;
                    IntPtr hCur = CreateIconIndirect(ref ii);
                    if (hCur != IntPtr.Zero)
                    {
                        customCursorHicon = hCur;
                        return new Cursor(hCur);
                    }
                }
            }
            catch { }
            finally
            {
                ReleaseDC(IntPtr.Zero, screenDc);
                if (hDib != IntPtr.Zero) DeleteObject(hDib);
                if (hMask != IntPtr.Zero) DeleteObject(hMask);
            }
            return Cursors.Default;
        }

        // Generates a Razor-Smooth 16x-SSAA Borderless Translucent White Liquid-Glass Orb Cursor (Zero Pixelation, Zero Outline!)
        private void BuildCustomGlassCursors()
        {
            try
            {
                int size = 32;
                int[] px = new int[size * size];
                float cx = 15.5f;
                float cy = 15.5f;
                float radius = 8.0f; // 16px diameter liquid-glass droplet

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        // 4x4 stratified sub-pixel super-sampling (16 samples per pixel) for silky smooth anti-aliasing
                        float covAccum = 0f;
                        float glowAccum = 0f;
                        for (int sy = 0; sy < 4; sy++)
                        {
                            float dy = (y + (sy - 1.5f) * 0.25f) - cy;
                            for (int sx = 0; sx < 4; sx++)
                            {
                                float dx = (x + (sx - 1.5f) * 0.25f) - cx;
                                float dist = (float)Math.Sqrt(dx * dx + dy * dy);

                                // Soft outer liquid-glass refraction aura (no hard outline)
                                if (dist < radius + 3.5f)
                                {
                                    float haloT = Clamp01(1f - Math.Max(0f, dist - radius) / 3.5f);
                                    glowAccum += haloT * haloT * 0.14f;
                                }

                                // Smooth sub-pixel circle edge
                                float sdf = dist - radius;
                                float sc = Clamp01(0.5f - sdf * 0.85f);
                                covAccum += sc * sc * (3f - 2f * sc);
                            }
                        }

                        float cov = covAccum * (1f / 16f);
                        float halo = glowAccum * (1f / 16f);

                        if (cov <= 0.001f && halo <= 0.001f)
                        {
                            px[y * size + x] = 0;
                            continue;
                        }

                        // Compute 3D spherical liquid-glass dome normal for subtle specular meniscus sheen (NO hard outline!)
                        float nx = (x - cx) / radius;
                        float ny = (y - cy) / radius;
                        float r2 = nx * nx + ny * ny;
                        float nz = (float)Math.Sqrt(Math.Max(0.01f, 1f - Math.Min(1f, r2)));

                        // Soft top-left liquid glass highlight + gentle Fresnel translucency
                        float lightDot = Math.Max(0f, nx * -0.55f + ny * -0.60f + nz * 0.58f);
                        float sheen = (float)Math.Pow(lightDot, 5.0f) * 0.22f;
                        float fresnelEdge = (float)Math.Pow(1f - nz, 2.5f) * 0.12f;

                        // Semi-transparent luminous white liquid-glass alpha (~76% core opacity, rising slightly at specular glint)
                        float bodyAlpha = Clamp01(0.72f + sheen + fresnelEdge) * cov;
                        float totalAlpha = Clamp01(Math.Max(bodyAlpha, halo));

                        // Clean pure white with a faint cool crystal tint
                        int r = 250 + (int)(sheen * 22f);
                        int g = 252 + (int)(sheen * 13f);
                        int b = 255;
                        if (r > 255) r = 255;
                        if (g > 255) g = 255;

                        int a = (int)(totalAlpha * 255f + 0.5f);
                        px[y * size + x] = (a << 24) | (r << 16) | (g << 8) | b;
                    }
                }

                using (Bitmap b = new Bitmap(size, size, PixelFormat.Format32bppArgb))
                {
                    BitmapData bd = b.LockBits(new Rectangle(0, 0, size, size), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                    Marshal.Copy(px, 0, bd.Scan0, px.Length);
                    b.UnlockBits(bd);

                    Cursor circleCur = CreateCursorFromBitmap(b, 16, 16);
                    curGlassDefault = circleCur;
                    curGlassPointer = circleCur;
                    curGlassText = circleCur;
                    curGlassGrab = circleCur;
                    curGlassGrabbing = circleCur;
                    curGlassDelete = circleCur;
                }
            }
            catch
            {
                curGlassDefault = Cursors.Default;
                curGlassPointer = Cursors.Default;
                curGlassText = Cursors.Default;
                curGlassGrab = Cursors.Default;
                curGlassGrabbing = Cursors.Default;
                curGlassDelete = Cursors.Default;
            }
        }

        private void LoadOrInitializeConfig()
        {
            if (File.Exists(configPath))
            {
                try
                {
                    using (FileStream fs = new FileStream(configPath, FileMode.Open, FileAccess.Read))
                    {
                        DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(WidgetConfig));
                        config = (WidgetConfig)ser.ReadObject(fs);
                    }
                }
                catch { config = null; }
            }

            if (config == null)
            {
                DateTime sampleBday = DateTime.Today.AddDays(5);
                config = new WidgetConfig
                {
                    People = new List<PersonEntry>
                    {
                        new PersonEntry
                        {
                            Name = "Example",
                            Month = sampleBday.Month,
                            Day = sampleBday.Day,
                            BirthYear = sampleBday.Year - 24
                        }
                    },
                    AlertSoonDays = 14,
                    AlertUrgentDays = 3,
                    EnableSoundAlerts = false,
                    EnablePopupBanner = true,
                    AutoStartWithPC = true,
                    AccentHex = "#FFFFFF",
                    GlassPresetIndex = 0,
                    ShowAgeBadge = true,
                    WindowX = -9999,
                    WindowY = -9999,
                    WidgetWidth = 780,
                    WidgetHeight = 500,
                    TintVariant = "Clear",
                    FontName = "Melon Squishy",
                    TextColorMode = "Auto",
                    LayoutMode = "HeroLeft",
                    DarkMode = false,
                    EnableBirthdayAnimation = true,
                    EnableBirthdayMessage = true,
                    HasBdayDefaultsInit = true,
                    HasCompletedTutorial = false,
                    LayerMode = "Above",
                    LayerDesktopIcons = true,
                    LayerRainmeter = true,
                    LayerAlwaysOnTop = false,
                    LayerTargetExes = new List<string>(),
                    TargetRefreshRate = 0,
                    RefreshWhileDragging = true,
                    HasPerfDefaultsInit = true
                };

                SaveConfig();
            }
            else
            {
                if (config.People == null) config.People = new List<PersonEntry>();
                if (string.IsNullOrEmpty(config.TintVariant)) config.TintVariant = "Clear";
                if (string.IsNullOrEmpty(config.FontName)) config.FontName = "Melon Squishy";
                if (string.IsNullOrEmpty(config.TextColorMode)) config.TextColorMode = "Auto";
                if (string.IsNullOrEmpty(config.LayoutMode)) config.LayoutMode = "HeroLeft";
                if (string.IsNullOrEmpty(config.LayerMode))
                {
                    config.LayerMode = "Above";
                    config.LayerDesktopIcons = true;
                    config.LayerRainmeter = true;
                    config.LayerAlwaysOnTop = false;
                }
                if (config.LayerTargetExes == null) config.LayerTargetExes = new List<string>();
                if (!config.HasPerfDefaultsInit)
                {
                    config.TargetRefreshRate = 0; // Auto = Monitor Native Hz
                    config.RefreshWhileDragging = true;
                    config.HasPerfDefaultsInit = true;
                }
                if (!config.HasBdayDefaultsInit)
                {
                    config.EnableBirthdayAnimation = true;
                    config.EnableBirthdayMessage = true;
                    config.HasBdayDefaultsInit = true;
                }
            }

            showTutorialOverlay = !config.HasCompletedTutorial;
            tutorialStep = 0;
            bdayCelebrationStartSec = 0f;
            bdayBannerDismissed = false;
        }

        private int DetectMonitorRefreshRate()
        {
            try
            {
                DEVMODE dm = new DEVMODE();
                dm.dmSize = (ushort)Marshal.SizeOf(typeof(DEVMODE));
                if (EnumDisplaySettings(null, ENUM_CURRENT_SETTINGS, ref dm) && dm.dmDisplayFrequency >= 30 && dm.dmDisplayFrequency <= 500)
                {
                    return (int)dm.dmDisplayFrequency;
                }
            }
            catch { }
            return 60;
        }

        private int GetEffectiveTargetHz()
        {
            int cfgHz = (config != null) ? config.TargetRefreshRate : 0;
            if (cfgHz <= 0) return Math.Max(60, detectedMonitorHz);
            return cfgHz;
        }

        private void SaveConfig()
        {
            try
            {
                config.WindowX = currentWinX;
                config.WindowY = currentWinY;
                config.WidgetWidth = widgetWidth;
                config.WidgetHeight = widgetHeight;
                using (FileStream fs = new FileStream(configPath, FileMode.Create, FileAccess.Write))
                {
                    DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(WidgetConfig));
                    ser.WriteObject(fs, config);
                }
            }
            catch { }
        }

        private void ApplyPresetValues(int index)
        {
            config.GlassPresetIndex = index;
            if (index == 1)
            {
                refractionStrength = 54f;
                chromaticStrength = 2.10f;
                blurSigma = 1.2f;
                fresnelIntensity = 0.65f;
            }
            else if (index == 2)
            {
                refractionStrength = 34f;
                chromaticStrength = 1.35f;
                blurSigma = 7.0f;
                fresnelIntensity = 0.50f;
            }
            else
            {
                refractionStrength = 44f;
                chromaticStrength = 1.65f;
                blurSigma = 2.6f;
                fresnelIntensity = 0.55f;
            }
        }

        private bool IsAppWindowModeEnabled()
        {
            return config != null && config.AppWindowMode;
        }

        private void ApplyWindowModeStyle()
        {
            try
            {
                bool appWin = IsAppWindowModeEnabled();
                this.ShowInTaskbar = appWin;
                if (this.IsHandleCreated)
                {
                    int exStyle = GetWindowLong(this.Handle, -20);
                    if (appWin)
                    {
                        exStyle = (exStyle & ~(WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE)) | WS_EX_APPWINDOW;
                        SetWindowLong(this.Handle, -20, exStyle);
                        try { SetWindowDisplayAffinity(this.Handle, 0x00000011); } catch { } // WDA_EXCLUDEFROMCAPTURE so live BitBlt sees everything behind Birthly!
                        SetWindowPos(this.Handle, HWND_NOTOPMOST, 0, 0, 0, 0,
                            SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW | 0x0020);
                    }
                    else
                    {
                        exStyle = (exStyle & ~WS_EX_APPWINDOW) | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
                        SetWindowLong(this.Handle, -20, exStyle);
                        try { SetWindowDisplayAffinity(this.Handle, 0x00000000); } catch { } // WDA_NONE
                        SetWindowPos(this.Handle, IntPtr.Zero, 0, 0, 0, 0,
                            SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE | 0x0020);
                        PinInFrontOfRainmeterClock();
                    }
                    needFullScreenCapture = true;
                }
            }
            catch { }
        }

        private void CheckAndStartSnapBackToScreen(bool immediate = false)
        {
            try
            {
                Point centerPt = new Point(currentWinX + surfaceW / 2, currentWinY + surfaceH / 2);
                Rectangle wa = Screen.FromPoint(centerPt).WorkingArea;
                int pad = 20;
                int minX = wa.Left + pad - edgeMargin;
                int maxX = wa.Right - surfaceW - pad + edgeMargin;
                int minY = wa.Top + pad - edgeMargin;
                int maxY = wa.Bottom - surfaceH - pad + edgeMargin;
                if (maxX < minX) { minX = wa.Left + (wa.Width - surfaceW) / 2; maxX = minX; }
                if (maxY < minY) { minY = wa.Top + (wa.Height - surfaceH) / 2; maxY = minY; }

                int clampedX = ClampInt(currentWinX, minX, maxX);
                int clampedY = ClampInt(currentWinY, minY, maxY);

                // If overhanging or pushed into a corner past the safe margin, smoothly glide back into full view!
                if (clampedX != currentWinX || clampedY != currentWinY)
                {
                    if (immediate)
                    {
                        currentWinX = clampedX;
                        currentWinY = clampedY;
                        this.Location = new Point(currentWinX, currentWinY);
                        isSnappingToScreen = false;
                    }
                    else
                    {
                        snapFloatX = currentWinX;
                        snapFloatY = currentWinY;
                        snapTargetX = clampedX;
                        snapTargetY = clampedY;
                        isSnappingToScreen = true;
                        MarkDirty();
                    }
                }
            }
            catch { }
        }

        private void SetKeyboardActivationEnabled(bool enableKeyboard)
        {
            try
            {
                if (IsAppWindowModeEnabled())
                {
                    if (enableKeyboard) { SetForegroundWindow(this.Handle); this.Focus(); }
                    return;
                }
                int exStyle = GetWindowLong(this.Handle, -20);
                if (enableKeyboard)
                {
                    SetWindowLong(this.Handle, -20, exStyle & ~WS_EX_NOACTIVATE);
                    SetForegroundWindow(this.Handle);
                    this.Focus();
                }
                else
                {
                    SetWindowLong(this.Handle, -20, exStyle | WS_EX_NOACTIVATE);
                    PinInFrontOfRainmeterClock();
                }
            }
            catch { }
        }

        private void OpenInGlassSettings()
        {
            pendingDeletePerson = null;
            isFilterDropdownOpen = false;
            isLayerSearchFocused = false;
            layerSearchQuery = "";
            layerListScroll = 0;
            backupStatusMsg = "";

            stagedUrgentDays = config.AlertUrgentDays;
            stagedNearDaysText = Math.Max(1, Math.Min(30, stagedUrgentDays)).ToString();
            stagedLanguage = string.IsNullOrEmpty(config.Language) ? "EN" : config.Language;
            stagedClickSounds = config.EnableClickSounds;
            stagedCustomSoundPath = config.CustomClickSoundPath ?? "";
            stagedCursorStyle = string.IsNullOrEmpty(config.CursorStyle) ? "Glass" : config.CursorStyle;
            stagedCustomCursorPath = config.CustomCursorPath ?? "";
            stagedUseCustomSize = !(widgetWidth == 700 && widgetHeight == 450)
                && !(widgetWidth == 780 && widgetHeight == 500)
                && !(widgetWidth == 880 && widgetHeight == 560);
            isLangDropdownOpen = false;
            isNearDaysFocused = false;
            stagedSoonDays = config.AlertSoonDays;
            stagedShowAge = config.ShowAgeBadge;
            stagedAutoStart = config.AutoStartWithPC;
            stagedPresetIndex = config.GlassPresetIndex;
            stagedWidth = widgetWidth;
            stagedHeight = widgetHeight;
            customWText = stagedWidth.ToString();
            customHText = stagedHeight.ToString();
            customSizeFocus = "";
            stagedRadius = cornerRadius;

            stagedTint = config.TintVariant ?? "Clear";
            stagedFont = config.FontName ?? (config.UseBasicTheme ? "Segoe UI" : "Melon Squishy");
            stagedTextColor = config.TextColorMode ?? "Auto";
            stagedLayout = config.LayoutMode ?? "HeroLeft";
            stagedBasicTheme = config.UseBasicTheme;
            stagedDarkMode = config.DarkMode;
            stagedAppWindowMode = config.AppWindowMode;
            stagedAdaptBright = config.AdaptTextToBrightBg;
            stagedDarkenLevel = config.BrightBgDarkenLevel;
            stagedBrightAmp = config.BrightBgAmplifier >= 25 ? config.BrightBgAmplifier : 100;

            origTint = stagedTint;
            origFont = stagedFont;
            origTextColor = stagedTextColor;
            origLayout = stagedLayout;
            origBasicTheme = stagedBasicTheme;
            origDarkMode = stagedDarkMode;
            origAppWindowMode = stagedAppWindowMode;
            origAdaptBright = stagedAdaptBright;
            origDarkenLevel = stagedDarkenLevel;
            origBrightAmp = stagedBrightAmp;

            stagedLayerMode = config.LayerMode ?? "Above";
            stagedLayerDesktopIcons = config.LayerDesktopIcons;
            stagedLayerRainmeter = config.LayerRainmeter;
            stagedLayerAlwaysOnTop = config.LayerAlwaysOnTop;
            stagedLayerTargets = config.LayerTargetExes != null ? new List<string>(config.LayerTargetExes) : new List<string>();

            stagedRefreshRate = config.TargetRefreshRate;
            stagedRefreshWhileDragging = config.RefreshWhileDragging;
            stagedBdayAnim = config.EnableBirthdayAnimation;
            stagedBdayMsg = config.EnableBirthdayMessage;
            origBdayAnim = stagedBdayAnim;
            origBdayMsg = stagedBdayMsg;
            detectedMonitorHz = DetectMonitorRefreshRate();

            origPresetIndex = config.GlassPresetIndex;
            origWidth = widgetWidth;
            origHeight = widgetHeight;
            origRadius = cornerRadius;

            RefreshRunningProcesses(true);
            currentView = ActiveView.Settings;
            TriggerViewFade();
            SetKeyboardActivationEnabled(false);
            Interlocked.Exchange(ref dirtyFrame, 1);
        }

        private void CancelInGlassSettings()
        {
            bool presetChanged = (config.GlassPresetIndex != origPresetIndex) || (stagedBasicTheme != origBasicTheme);
            bool sizeChanged = (widgetWidth != origWidth || widgetHeight != origHeight);

            config.GlassPresetIndex = origPresetIndex;
            ApplyPresetValues(origPresetIndex);
            stagedTint = origTint;
            stagedFont = origFont;
            stagedTextColor = origTextColor;
            stagedLayout = origLayout;
            stagedBasicTheme = origBasicTheme;
            stagedDarkMode = origDarkMode;
            stagedAppWindowMode = origAppWindowMode;
            stagedBdayAnim = origBdayAnim;
            stagedBdayMsg = origBdayMsg;
            stagedAdaptBright = origAdaptBright;
            stagedDarkenLevel = origDarkenLevel;
            stagedBrightAmp = origBrightAmp;
            isLayerSearchFocused = false;
            isDraggingPreviewBlock = false;

            currentView = ActiveView.Dashboard;
            TriggerViewFade();
            SetKeyboardActivationEnabled(false);

            if (sizeChanged)
            {
                ResizeWidget(origWidth, origHeight, origRadius, false);
            }
            else if (presetChanged)
            {
                PrecomputeOpticalTables();
            }
            Interlocked.Exchange(ref dirtyFrame, 1);
        }

        private void SaveInGlassSettings()
        {
            if (stagedUseCustomSize)
            {
                int pw, ph;
                if (!string.IsNullOrEmpty(customWText) && int.TryParse(customWText, out pw))
                    stagedWidth = ClampInt(pw, 560, 1600);
                if (!string.IsNullOrEmpty(customHText) && int.TryParse(customHText, out ph))
                    stagedHeight = ClampInt(ph, 380, 1000);
                customWText = stagedWidth.ToString();
                customHText = stagedHeight.ToString();
            }
            customSizeFocus = "";

            int parsedNear = stagedUrgentDays;
            if (!string.IsNullOrEmpty(stagedNearDaysText) && int.TryParse(stagedNearDaysText, out parsedNear))
            {
                stagedUrgentDays = ClampInt(parsedNear, 1, 30);
            }
            config.AlertUrgentDays = stagedUrgentDays;
            config.Language = string.IsNullOrEmpty(stagedLanguage) ? "EN" : stagedLanguage;
            config.EnableClickSounds = stagedClickSounds;
            config.CustomClickSoundPath = stagedCustomSoundPath ?? "";
            config.CursorStyle = string.IsNullOrEmpty(stagedCursorStyle) ? "Glass" : stagedCursorStyle;
            config.CustomCursorPath = stagedCustomCursorPath ?? "";
            ApplyCustomCursor(config.CustomCursorPath);
            isLangDropdownOpen = false;
            isNearDaysFocused = false;
            config.AlertSoonDays = stagedSoonDays;
            config.ShowAgeBadge = stagedShowAge;
            if (config.AutoStartWithPC != stagedAutoStart)
            {
                config.AutoStartWithPC = stagedAutoStart;
                SetAutoStartRegistry(config.AutoStartWithPC);
            }
            config.GlassPresetIndex = stagedPresetIndex;
            ApplyPresetValues(stagedPresetIndex);

            config.TintVariant = stagedTint;
            config.FontName = stagedFont;
            config.TextColorMode = stagedTextColor;
            config.LayoutMode = stagedLayout;
            bool themeTypeChanged = (config.UseBasicTheme != stagedBasicTheme);
            bool winModeChanged = (config.AppWindowMode != stagedAppWindowMode);
            config.UseBasicTheme = stagedBasicTheme;
            config.DarkMode = stagedDarkMode;
            config.AppWindowMode = stagedAppWindowMode;
            config.AdaptTextToBrightBg = stagedAdaptBright;
            config.BrightBgDarkenLevel = stagedDarkenLevel;
            config.BrightBgAmplifier = stagedBrightAmp;
            config.HasAdaptDefaultsInit = true;

            config.LayerMode = stagedLayerMode;
            config.LayerDesktopIcons = stagedLayerDesktopIcons;
            config.LayerRainmeter = stagedLayerRainmeter;
            config.LayerAlwaysOnTop = stagedLayerAlwaysOnTop;
            config.LayerTargetExes = new List<string>(stagedLayerTargets);

            config.TargetRefreshRate = stagedRefreshRate;
            config.RefreshWhileDragging = stagedRefreshWhileDragging;
            config.EnableBirthdayAnimation = stagedBdayAnim;
            config.EnableBirthdayMessage = stagedBdayMsg;
            config.HasBdayDefaultsInit = true;
            config.HasPerfDefaultsInit = true;
            if (stagedBdayAnim) bdayCelebrationStartSec = (float)animClock.Elapsed.TotalSeconds;
            if (stagedBdayMsg) bdayBannerDismissed = false;

            isLayerSearchFocused = false;
            isLangSearchFocused = false;
            showResetConfirmModal = false;
            isDraggingPreviewBlock = false;
            if (stagedWidth != widgetWidth || stagedHeight != widgetHeight)
            {
                ResizeWidget(stagedWidth, stagedHeight, stagedRadius, false);
            }
            currentView = ActiveView.Dashboard;
            TriggerViewFade();
            SetKeyboardActivationEnabled(false);
            SaveConfig();
            if (themeTypeChanged) PrecomputeOpticalTables();
            if (winModeChanged) ApplyWindowModeStyle();
            else PinInFrontOfRainmeterClock();
            MarkDirty();
        }

        private void RefreshRunningProcesses(bool force)
        {
            if (!force && (DateTime.Now - lastRunningExeRefresh).TotalSeconds < 4.0) return;
            lastRunningExeRefresh = DateTime.Now;
            try
            {
                HashSet<string> names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                uint myPid = (uint)Process.GetCurrentProcess().Id;
                EnumWindows((h, l) => {
                    if (!IsWindowVisible(h) || h == this.Handle) return true;
                    uint pid = 0;
                    GetWindowThreadProcessId(h, out pid);
                    if (pid > 0 && pid != myPid)
                    {
                        try
                        {
                            using (Process p = Process.GetProcessById((int)pid))
                            {
                                if (!string.IsNullOrEmpty(p.ProcessName))
                                {
                                    string exe = p.ProcessName + ".exe";
                                    if (!exe.Equals("Birthly.exe", StringComparison.OrdinalIgnoreCase))
                                        names.Add(exe);
                                }
                            }
                        }
                        catch { }
                    }
                    return true;
                }, IntPtr.Zero);

                // Also ensure any user-added targets are visible in the list
                if (stagedLayerTargets != null)
                {
                    foreach (string t in stagedLayerTargets) names.Add(t);
                }

                List<string> list = names.OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();
                cachedRunningExes = list;
            }
            catch { }
        }

        private void PickCustomExeForLayer()
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "Select Application (.exe) for Layer Order";
                ofd.Filter = "Applications (*.exe)|*.exe|All Files (*.*)|*.*";
                if (ofd.ShowDialog(this) == DialogResult.OK)
                {
                    string fileName = Path.GetFileName(ofd.FileName);
                    if (!string.IsNullOrEmpty(fileName))
                    {
                        if (!stagedLayerTargets.Any(x => x.Equals(fileName, StringComparison.OrdinalIgnoreCase)))
                        {
                            stagedLayerTargets.Add(fileName);
                        }
                        if (!cachedRunningExes.Any(x => x.Equals(fileName, StringComparison.OrdinalIgnoreCase)))
                        {
                            cachedRunningExes.Add(fileName);
                            cachedRunningExes.Sort(StringComparer.OrdinalIgnoreCase);
                        }
                        Interlocked.Exchange(ref dirtyFrame, 1);
                    }
                }
            }
        }

        private void ExportBackupBundle()
        {
            try
            {
                using (SaveFileDialog sfd = new SaveFileDialog())
                {
                    sfd.Title = "Export Birthly Backup (People, Settings & Images)";
                    sfd.Filter = "Birthly Backup (*.birthly)|*.birthly|All Files (*.*)|*.*";
                    sfd.DefaultExt = "birthly";
                    sfd.FileName = "Birthly_backup_" + DateTime.Now.ToString("yyyy-MM-dd") + ".birthly";

                    if (sfd.ShowDialog(this) != DialogResult.OK) return;

                    BackupBundle bundle = new BackupBundle
                    {
                        FormatVersion = 1,
                        ExportedAt = DateTime.Now.ToString("o"),
                        Config = config,
                        EmbeddedImagesBase64 = new Dictionary<string, string>()
                    };

                    int imgCount = 0;
                    if (config.People != null)
                    {
                        foreach (PersonEntry p in config.People)
                        {
                            if (p.IconType == "image" && !string.IsNullOrEmpty(p.ImagePath) && File.Exists(p.ImagePath))
                            {
                                try
                                {
                                    // Embed downscaled <=256x256 PNG bytes directly into the single backup file
                                    using (MemoryStream ms = new MemoryStream())
                                    using (FileStream fs = new FileStream(p.ImagePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                                    using (Image rawImg = Image.FromStream(fs))
                                    {
                                        int srcSize = Math.Min(rawImg.Width, rawImg.Height);
                                        int srcX = (rawImg.Width - srcSize) / 2;
                                        int srcY = (rawImg.Height - srcSize) / 2;
                                        int targetDim = Math.Min(256, Math.Max(64, srcSize));
                                        using (Bitmap bmp = new Bitmap(targetDim, targetDim, PixelFormat.Format32bppArgb))
                                        {
                                            using (Graphics g = Graphics.FromImage(bmp))
                                            {
                                                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                                                g.SmoothingMode = SmoothingMode.HighQuality;
                                                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                                                g.DrawImage(rawImg, new Rectangle(0, 0, targetDim, targetDim), new Rectangle(srcX, srcY, srcSize, srcSize), GraphicsUnit.Pixel);
                                            }
                                            bmp.Save(ms, ImageFormat.Png);
                                        }
                                        string key = !string.IsNullOrEmpty(p.Id) ? p.Id : Guid.NewGuid().ToString("N").Substring(0, 8);
                                        p.Id = key;
                                        bundle.EmbeddedImagesBase64[key] = Convert.ToBase64String(ms.ToArray());
                                        imgCount++;
                                    }
                                }
                                catch { }
                            }
                        }
                    }

                    using (FileStream outFs = new FileStream(sfd.FileName, FileMode.Create, FileAccess.Write))
                    {
                        DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(BackupBundle));
                        ser.WriteObject(outFs, bundle);
                    }

                    int peopleCount = config.People != null ? config.People.Count : 0;
                    backupStatusMsg = string.Format("EXPORTED {0} PEOPLE + {1} IMAGES TO {2}", peopleCount, imgCount, Path.GetFileName(sfd.FileName).ToUpper());
                    Interlocked.Exchange(ref dirtyFrame, 1);
                }
            }
            catch
            {
                backupStatusMsg = "EXPORT FAILED — CHECK FILE PERMISSIONS";
                Interlocked.Exchange(ref dirtyFrame, 1);
            }
        }

        private void ImportBackupBundle()
        {
            try
            {
                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    ofd.Title = "Import Birthly Backup (.birthly)";
                    ofd.Filter = "Birthly Backup (*.birthly;*.btime)|*.birthly;*.btime|All Files (*.*)|*.*";

                    if (ofd.ShowDialog(this) != DialogResult.OK) return;

                    BackupBundle bundle = null;
                    using (FileStream inFs = new FileStream(ofd.FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(BackupBundle));
                        bundle = (BackupBundle)ser.ReadObject(inFs);
                    }

                    if (bundle == null || bundle.Config == null)
                    {
                        backupStatusMsg = "INVALID OR EMPTY BACKUP FILE";
                        Interlocked.Exchange(ref dirtyFrame, 1);
                        return;
                    }

                    Directory.CreateDirectory(avatarsDir);
                    int imgRestored = 0;

                    if (bundle.Config.People == null) bundle.Config.People = new List<PersonEntry>();
                    HashSet<string> usedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    int fileSeq = 0;
                    foreach (PersonEntry p in bundle.Config.People)
                    {
                        string origId = p.Id;
                        if (string.IsNullOrWhiteSpace(p.Id) || !usedIds.Add(p.Id))
                        {
                            p.Id = Guid.NewGuid().ToString("N").Substring(0, 8);
                            usedIds.Add(p.Id);
                        }
                        p.Name = (p.Name ?? "Unnamed").Trim();
                        p.Month = Math.Max(1, Math.Min(12, p.Month));
                        p.Day = Math.Max(1, Math.Min(DateTime.DaysInMonth(2024, p.Month), p.Day));
                        if (p.BirthYear != 0 && (p.BirthYear < 1900 || p.BirthYear > DateTime.Now.Year)) p.BirthYear = 0;

                        bool restoredImg = false;
                        if (bundle.EmbeddedImagesBase64 != null && !string.IsNullOrEmpty(origId))
                        {
                            string b64;
                            if (bundle.EmbeddedImagesBase64.TryGetValue(origId, out b64) && !string.IsNullOrEmpty(b64))
                            {
                                try
                                {
                                    byte[] bytes = Convert.FromBase64String(b64);
                                    using (MemoryStream checkMs = new MemoryStream(bytes))
                                    using (Image validImg = Image.FromStream(checkMs))
                                    {
                                        if (validImg.Width > 0 && validImg.Height > 0)
                                        {
                                            string outImg = Path.Combine(avatarsDir, "imp_" + Guid.NewGuid().ToString("N").Substring(0, 10) + "_" + (++fileSeq) + ".png");
                                            File.WriteAllBytes(outImg, bytes);
                                            p.ImagePath = outImg;
                                            p.IconType = "image";
                                            imgRestored++;
                                            restoredImg = true;
                                        }
                                    }
                                }
                                catch { }
                            }
                        }
                        if (!restoredImg && p.IconType == "image" && (string.IsNullOrEmpty(p.ImagePath) || !File.Exists(p.ImagePath)))
                        {
                            p.IconType = "initials";
                            p.ImagePath = "";
                        }
                    }

                    // Keep current window position on this PC so it doesn't jump off-screen if imported from another monitor
                    bundle.Config.WindowX = currentWinX;
                    bundle.Config.WindowY = currentWinY;
                    if (string.IsNullOrEmpty(bundle.Config.TintVariant)) bundle.Config.TintVariant = "Clear";
                    if (string.IsNullOrEmpty(bundle.Config.FontName)) bundle.Config.FontName = "Melon Squishy";
                    if (string.IsNullOrEmpty(bundle.Config.LayoutMode)) bundle.Config.LayoutMode = "HeroLeft";
                    if (string.IsNullOrEmpty(bundle.Config.LayerMode)) bundle.Config.LayerMode = "Above";
                    if (bundle.Config.LayerTargetExes == null) bundle.Config.LayerTargetExes = new List<string>();
                    bundle.Config.AlertUrgentDays = ClampInt(bundle.Config.AlertUrgentDays <= 0 ? 3 : bundle.Config.AlertUrgentDays, 1, 30);
                    bundle.Config.AlertSoonDays = ClampInt(bundle.Config.AlertSoonDays <= 0 ? 14 : bundle.Config.AlertSoonDays, 1, 90);
                    if (!bundle.Config.HasBdayDefaultsInit)
                    {
                        bundle.Config.EnableBirthdayAnimation = true;
                        bundle.Config.EnableBirthdayMessage = true;
                        bundle.Config.HasBdayDefaultsInit = true;
                    }
                    if (!bundle.Config.HasPerfDefaultsInit)
                    {
                        bundle.Config.TargetRefreshRate = 0;
                        bundle.Config.RefreshWhileDragging = true;
                        bundle.Config.HasPerfDefaultsInit = true;
                    }

                    int impW = bundle.Config.WidgetWidth >= 850 ? 880 : (bundle.Config.WidgetWidth <= 720 && bundle.Config.WidgetWidth >= 680 ? 700 : 780);
                    int impH = impW == 880 ? 560 : (impW == 700 ? 450 : 500);
                    float impR = impW == 880 ? 54f : (impW == 700 ? 46f : 50f);
                    bundle.Config.WidgetWidth = impW;
                    bundle.Config.WidgetHeight = impH;

                    bool needResize = (impW != widgetWidth || impH != widgetHeight);

                    lock (uiLock)
                    {
                        foreach (Image img in avatarCache.Values) { try { img.Dispose(); } catch { } }
                        avatarCache.Clear();

                        config = bundle.Config;

                        stagedUrgentDays = config.AlertUrgentDays;
                        stagedSoonDays = config.AlertSoonDays;
                        stagedShowAge = config.ShowAgeBadge;
                        stagedAutoStart = config.AutoStartWithPC;
                        stagedPresetIndex = config.GlassPresetIndex;
                        stagedTint = config.TintVariant;
                        stagedFont = config.FontName;
                        stagedTextColor = config.TextColorMode ?? "Auto";
                        stagedLayout = config.LayoutMode;
                        stagedBasicTheme = config.UseBasicTheme;
                        stagedDarkMode = config.DarkMode;
                        stagedBdayAnim = config.EnableBirthdayAnimation;
                        stagedBdayMsg = config.EnableBirthdayMessage;
                        stagedLayerMode = config.LayerMode;
                        stagedLayerDesktopIcons = config.LayerDesktopIcons;
                        stagedLayerRainmeter = config.LayerRainmeter;
                        stagedLayerAlwaysOnTop = config.LayerAlwaysOnTop;
                        stagedLayerTargets = new List<string>(config.LayerTargetExes);
                        stagedRefreshRate = config.TargetRefreshRate;
                        stagedRefreshWhileDragging = config.RefreshWhileDragging;
                        stagedWidth = impW;
                        stagedHeight = impH;
                        stagedRadius = impR;

                        origTint = stagedTint;
                        origFont = stagedFont;
                        origTextColor = stagedTextColor;
                        origLayout = stagedLayout;
                        origBasicTheme = stagedBasicTheme;
                        origDarkMode = stagedDarkMode;
                        origBdayAnim = stagedBdayAnim;
                        origBdayMsg = stagedBdayMsg;
                        origPresetIndex = stagedPresetIndex;
                        origWidth = impW;
                        origHeight = impH;
                        origRadius = impR;
                        listScrollOffset = 0;
                        heroSpotlightIndex = 0;
                        backupStatusMsg = string.Format("IMPORTED {0} PEOPLE + {1} IMAGES!", config.People.Count, imgRestored);
                    }

                    ApplyPresetValues(config.GlassPresetIndex);
                    if (needResize) ResizeWidget(impW, impH, impR, false);
                    else PrecomputeOpticalTables();
                    SaveConfig();
                    SetAutoStartRegistry(config.AutoStartWithPC);
                    PinInFrontOfRainmeterClock();
                    Interlocked.Exchange(ref dirtyFrame, 1);
                }
            }
            catch
            {
                backupStatusMsg = "IMPORT FAILED — INVALID .BIRTHLY FILE";
                Interlocked.Exchange(ref dirtyFrame, 1);
            }
        }

        private void StartInGlassPersonEditor(PersonEntry existing, int presetMonth = 0, int presetDay = 0)
        {
            pendingDeletePerson = null;
            isFilterDropdownOpen = false;
            isContextMenuOpen = false;
            editorErrorMsg = "";
            if (existing != null)
            {
                isNewPerson = false;
                editingPerson = existing;
                editName = existing.Name ?? "";
                editDay = Math.Max(1, Math.Min(31, existing.Day));
                editMonth = Math.Max(1, Math.Min(12, existing.Month));
                editYearStr = existing.BirthYear > 0 ? existing.BirthYear.ToString() : "";
                editNotes = existing.Notes ?? "";
                editIconType = string.IsNullOrEmpty(existing.IconType) ? "initials" : existing.IconType;
                editImagePath = existing.ImagePath ?? "";
            }
            else
            {
                isNewPerson = true;
                editingPerson = new PersonEntry
                {
                    Id = Guid.NewGuid().ToString("N").Substring(0, 8)
                };
                editName = "";
                editMonth = presetMonth >= 1 && presetMonth <= 12 ? presetMonth : DateTime.Now.Month;
                int maxD = DateTime.DaysInMonth(2024, editMonth);
                editDay = presetDay >= 1 && presetDay <= maxD ? presetDay : DateTime.Now.Day;
                editYearStr = "";
                editNotes = "";
                editIconType = "initials";
                editImagePath = "";
            }
            editDayStr = editDay.ToString("00");
            activeTextField = "name";
            currentView = ActiveView.AddOrEditPerson;
            TriggerViewFade();
            SetKeyboardActivationEnabled(true);
            Interlocked.Exchange(ref dirtyFrame, 1);
        }

        private void SaveInGlassPersonEditor()
        {
            if (string.IsNullOrWhiteSpace(editName))
            {
                editorErrorMsg = "PLEASE ENTER A NAME FIRST";
                activeTextField = "name";
                Interlocked.Exchange(ref dirtyFrame, 1);
                return;
            }

            int parsedYear = 0;
            if (!string.IsNullOrWhiteSpace(editYearStr))
            {
                if (!int.TryParse(editYearStr.Trim(), out parsedYear) || parsedYear < 1900 || parsedYear > DateTime.Now.Year)
                {
                    editorErrorMsg = "YEAR MUST BE 1900–" + DateTime.Now.Year;
                    activeTextField = "year";
                    Interlocked.Exchange(ref dirtyFrame, 1);
                    return;
                }
            }

            int dayCheckYear = parsedYear > 0 ? parsedYear : 2024;
            int maxD = DateTime.DaysInMonth(dayCheckYear, Math.Max(1, Math.Min(12, editMonth)));
            int finalDay = editDay;
            int typedDay;
            if (int.TryParse((editDayStr ?? "").Trim(), out typedDay) && typedDay >= 1)
                finalDay = typedDay;
            finalDay = Math.Max(1, Math.Min(maxD, finalDay));

            lock (uiLock)
            {
                editingPerson.Name = editName.Trim();
                editingPerson.Day = finalDay;
                editingPerson.Month = Math.Max(1, Math.Min(12, editMonth));
                editingPerson.BirthYear = parsedYear;
                editingPerson.Notes = (editNotes ?? "").Trim();
                editingPerson.IconType = editIconType;
                editingPerson.ImagePath = editImagePath;
                editingPerson.AvatarColorHex = "#FFFFFF";

                if (isNewPerson)
                {
                    config.People.Add(editingPerson);
                }
            }

            editorErrorMsg = "";
            SaveConfig();
            currentView = ActiveView.Dashboard;
            TriggerViewFade();
            SetKeyboardActivationEnabled(false);
            Interlocked.Exchange(ref dirtyFrame, 1);
        }

        private float GetHoverProgress(string id, float dt)
        {
            float target = (hoveredZoneId == id) ? 1.0f : 0.0f;
            float cur = 0f;
            hoverProgress.TryGetValue(id, out cur);
            cur += (target - cur) * Math.Min(1f, dt * 16f);
            if (cur < 0.001f) cur = 0f;
            else if (cur > 0.999f) cur = 1f;
            hoverProgress[id] = cur;
            return cur;
        }

        private static RectangleF LerpRect(RectangleF cur, RectangleF target, float factor)
        {
            if (cur.IsEmpty || cur.Width <= 1f || cur.Height <= 1f) return target;
            float f = Clamp01(factor);
            float nx = cur.X + (target.X - cur.X) * f;
            float ny = cur.Y + (target.Y - cur.Y) * f;
            float nw = cur.Width + (target.Width - cur.Width) * f;
            float nh = cur.Height + (target.Height - cur.Height) * f;
            if (Math.Abs(nx - target.X) < 0.25f && Math.Abs(ny - target.Y) < 0.25f &&
                Math.Abs(nw - target.Width) < 0.25f && Math.Abs(nh - target.Height) < 0.25f)
            {
                return target;
            }
            return new RectangleF(nx, ny, nw, nh);
        }

        private static bool IsRectAnimating(RectangleF cur, RectangleF target)
        {
            if (cur.IsEmpty) return false;
            return Math.Abs(cur.X - target.X) >= 0.35f ||
                   Math.Abs(cur.Y - target.Y) >= 0.35f ||
                   Math.Abs(cur.Width - target.Width) >= 0.35f ||
                   Math.Abs(cur.Height - target.Height) >= 0.35f;
        }

        private static float EaseOutCubic(float p)
        {
            float c = Clamp01(p);
            float inv = 1f - c;
            return 1f - inv * inv * inv;
        }

        // Animated UI Overlay Renderer (Popup Spring Scale, Hover Sweep Shimmer, Live Search Bar, Click Ripple — ZERO vertical bobbing!)
        private void RebuildUIOverlay(float dt)
        {
            lock (uiLock)
            {
                int curSurfW = surfaceW;
                int curSurfH = surfaceH;
                int curWidW = widgetWidth;
                int curWidH = widgetHeight;

                float t = (float)animClock.Elapsed.TotalSeconds;

                if (viewPopupProgress < 1.0f)
                {
                    viewPopupProgress = Math.Min(1.0f, viewPopupProgress + dt * 11.5f);
                }
                if (settingsTabProgress < 1.0f)
                {
                    settingsTabProgress = Math.Min(1.0f, settingsTabProgress + dt * 12.5f);
                }
                if (calendarViewProgress < 1.0f)
                {
                    calendarViewProgress = Math.Min(1.0f, calendarViewProgress + dt * 6.5f);
                }
                if (heroStepperProgress < 1.0f)
                {
                    heroStepperProgress = Math.Min(1.0f, heroStepperProgress + dt * 7.5f);
                }
                if (tutorialStepProgress < 1.0f)
                {
                    tutorialStepProgress = Math.Min(1.0f, tutorialStepProgress + dt * 6.8f);
                }

                float ddTarget = isFilterDropdownOpen ? 1.0f : 0.0f;
                if (Math.Abs(filterDropdownProgress - ddTarget) > 0.001f)
                {
                    filterDropdownProgress += (ddTarget - filterDropdownProgress) * Math.Min(1.0f, dt * 18f);
                    if (filterDropdownProgress < 0.01f && ddTarget == 0f) filterDropdownProgress = 0f;
                    else if (filterDropdownProgress > 0.99f && ddTarget == 1f) filterDropdownProgress = 1f;
                }

                if (pendingDeletePerson != null) lastDeleteModalPerson = pendingDeletePerson;
                float delTarget = pendingDeletePerson != null ? 1.0f : 0.0f;
                if (Math.Abs(deleteModalProgress - delTarget) > 0.001f)
                {
                    deleteModalProgress += (delTarget - deleteModalProgress) * Math.Min(1.0f, dt * 15f);
                    if (deleteModalProgress < 0.01f && delTarget == 0f) { deleteModalProgress = 0f; lastDeleteModalPerson = null; }
                    else if (deleteModalProgress > 0.99f && delTarget == 1f) deleteModalProgress = 1f;
                }

                if (clickRippleProgress < 1.0f)
                {
                    clickRippleProgress = Math.Min(1.0f, clickRippleProgress + dt * 3.4f);
                }

                float ctxTarget = isContextMenuOpen ? 1.0f : 0.0f;
                if (Math.Abs(contextMenuProgress - ctxTarget) > 0.001f)
                {
                    contextMenuProgress += (ctxTarget - contextMenuProgress) * Math.Min(1.0f, dt * 18f);
                    if (contextMenuProgress < 0.01f && ctxTarget == 0f) contextMenuProgress = 0f;
                    else if (contextMenuProgress > 0.99f && ctxTarget == 1f) contextMenuProgress = 1f;
                }

                if (reusableUiBmp == null || reusableUiBmp.Width != curSurfW || reusableUiBmp.Height != curSurfH)
                {
                    if (reusableUiBmp != null) reusableUiBmp.Dispose();
                    reusableUiBmp = new Bitmap(curSurfW, curSurfH, PixelFormat.Format32bppPArgb);
                    reusableUiPixelsA = new int[curSurfW * curSurfH];
                    reusableUiPixelsB = new int[curSurfW * curSurfH];
                }

                using (Graphics g = Graphics.FromImage(reusableUiBmp))
                {
                    g.Clear(Color.Transparent);
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                    // ── 0. AMBIENT FLOATING LIQUID-GLASS LIGHT MOTES (Glass Mode only — hidden in Basic Mode for a clean native look!) ──
                    if (!IsBasicThemeEnabled() && ambientMotes != null)
                    {
                        for (int i = 0; i < ambientMotes.Length; i++)
                        {
                            GlassMote m = ambientMotes[i];
                            float my = (m.Y - t * m.SpeedY) % 1.0f;
                            if (my < 0f) my += 1.0f;
                            float mx = m.X + (float)Math.Sin(t * 0.9f + m.DriftPhase) * m.DriftAmp;

                            float px = edgeMargin + 36f + mx * (curWidW - 72f);
                            float py = edgeMargin + 36f + my * (curWidH - 72f);

                            float edgeFade = (float)Math.Sin(my * Math.PI);
                            float pulse = 0.65f + 0.35f * (float)Math.Sin(t * 2.2f + m.DriftPhase);
                            int alpha = (int)Clamp(m.BaseAlpha * edgeFade * pulse, 0f, 55f);
                            if (alpha > 1)
                            {
                                sharedFillBrush.Color = GetThemeRimColor(alpha);
                                g.FillEllipse(sharedFillBrush, px, py, m.Size, m.Size);
                            }
                        }
                    }

                    List<HitZone> zones = new List<HitZone>();
                    DateTime now = DateTime.Now;

                    List<PersonEntry> sorted = config.People
                        .OrderBy(p => p.GetDaysUntil(now))
                        .ThenBy(p => p.Name)
                        .ToList();

                    float leftPad = edgeMargin + 26f;
                    float topPad = edgeMargin + 18f;
                    float contentW = curWidW - 52f;

                    // ── 1. HEADER BAR ──
                    Font titleFont = GetCachedFont(13.5f, FontStyle.Bold, true);
                    Font subFont = GetCachedFont(12f, FontStyle.Regular, false);
                    DrawAdaptiveString(g, "B I R T H L Y", titleFont, edgeMargin + 74f, topPad + 6f, 245);
                    DrawAdaptiveString(g, "•   " + now.ToString("dd MMMM yyyy").ToUpper(), subFont, edgeMargin + 178f, topPad + 6f, 175);

                    float btnY = topPad;
                    float winCtrlOffset = IsAppWindowModeEnabled() ? 66f : 0f;
                    float setBtnW = 94f;
                    float addBtnW = 88f;
                    float setBtnX = edgeMargin + curWidW - 26f - winCtrlOffset - setBtnW;
                    float addBtnX = setBtnX - 10f - addBtnW;

                    if (IsAppWindowModeEnabled())
                    {
                        RectangleF minWinRect = new RectangleF(edgeMargin + curWidW - 26f - 58f, btnY, 26f, 28f);
                        RectangleF clsWinRect = new RectangleF(edgeMargin + curWidW - 26f - 26f, btnY, 26f, 28f);
                        float minHov = GetHoverProgress("btn_win_min", dt);
                        float clsHov = GetHoverProgress("btn_win_close", dt);
                        DrawAnimatedGlassButton(g, minWinRect, "", false, minHov, t);
                        DrawAnimatedGlassButton(g, clsWinRect, "", false, clsHov, t);

                        // Dead-center vector minimize (—) and close (×) icons
                        float mcx = minWinRect.X + minWinRect.Width * 0.5f;
                        float mcy = minWinRect.Y + minWinRect.Height * 0.5f;
                        using (Pen mp = new Pen(ComputePeakAdaptiveTextColor((int)(220 + minHov * 35)), 1.6f))
                        {
                            mp.StartCap = LineCap.Round; mp.EndCap = LineCap.Round;
                            g.DrawLine(mp, mcx - 4.5f, mcy, mcx + 4.5f, mcy);
                        }
                        float ccx = clsWinRect.X + clsWinRect.Width * 0.5f;
                        float ccy = clsWinRect.Y + clsWinRect.Height * 0.5f;
                        using (Pen cp = new Pen(ComputePeakAdaptiveTextColor((int)(220 + clsHov * 35)), 1.6f))
                        {
                            cp.StartCap = LineCap.Round; cp.EndCap = LineCap.Round;
                            g.DrawLine(cp, ccx - 4.0f, ccy - 4.0f, ccx + 4.0f, ccy + 4.0f);
                            g.DrawLine(cp, ccx + 4.0f, ccy - 4.0f, ccx - 4.0f, ccy + 4.0f);
                        }

                        zones.Add(new HitZone { Id = "btn_win_min", Bounds = minWinRect });
                        zones.Add(new HitZone { Id = "btn_win_close", Bounds = clsWinRect });
                    }

                    if (pendingDeletePerson == null && currentView == ActiveView.Dashboard)
                    {
                        RectangleF addRect = new RectangleF(addBtnX, btnY, addBtnW, 28f);
                        DrawAnimatedGlassButton(g, addRect, "+ ADD", false, GetHoverProgress("btn_add", dt), t);
                        zones.Add(new HitZone { Id = "btn_add", Bounds = addRect });

                        RectangleF setRect = new RectangleF(setBtnX, btnY, setBtnW, 28f);
                        DrawAnimatedGlassButton(g, setRect, "SETTINGS", false, GetHoverProgress("btn_settings", dt), t);
                        zones.Add(new HitZone { Id = "btn_settings", Bounds = setRect });
                    }

                    // Header divider line (+ animated travelling specular shimmer in Liquid Glass Mode only!)
                    float headerBottom = topPad + 42f;
                    sharedRimPen.Color = GetThemeRimColor(IsBasicLightMode() ? 55 : 32);
                    sharedRimPen.Width = 1f;
                    g.DrawLine(sharedRimPen, leftPad, headerBottom, leftPad + contentW, headerBottom);
                    float shimmerCenter = leftPad + ((t * 140f) % (contentW + 200f)) - 100f;
                    if (!IsBasicThemeEnabled() && shimmerCenter + 80f > leftPad && shimmerCenter - 80f < leftPad + contentW)
                    {
                        Color shimCol = GetThemeRimColor(IsBasicLightMode() ? 120 : 90);
                        Color shimZero = Color.FromArgb(0, shimCol.R, shimCol.G, shimCol.B);
                        using (LinearGradientBrush sBr = new LinearGradientBrush(
                            new PointF(shimmerCenter - 80f, headerBottom),
                            new PointF(shimmerCenter + 80f, headerBottom),
                            shimZero, shimCol))
                        {
                            ColorBlend cb = new ColorBlend(3);
                            cb.Colors = new Color[] { shimZero, shimCol, shimZero };
                            cb.Positions = new float[] { 0f, 0.5f, 1f };
                            sBr.InterpolationColors = cb;
                            float sx = Math.Max(leftPad, shimmerCenter - 80f);
                            float sw = Math.Min(160f, leftPad + contentW - sx);
                            if (sw > 1f) g.FillRectangle(sBr, sx, headerBottom - 0.5f, sw, 1.5f);
                        }
                    }

                    // Smooth integer-pixel slide transition when opening Menus (Settings / Add Person / Dashboard) — zero GDI+ text-scale morphing!
                    float bodyY = headerBottom + 16f;
                    float bodyH = edgeMargin + curWidH - bodyY - 22f;

                    float popProg = viewPopupProgress;
                    GraphicsState savedState = g.Save();
                    if (popProg < 0.999f)
                    {
                        float popEase = EaseOutCubic(popProg);
                        float slideY = (float)Math.Round((1.0f - popEase) * 12.0f);
                        if (slideY > 0.5f) g.TranslateTransform(0f, slideY);
                    }

                    if (currentView == ActiveView.Dashboard)
                    {
                        RenderMinimalDashboard(g, zones, sorted, now, leftPad, bodyY, contentW, bodyH, dt, t);
                    }
                    else if (currentView == ActiveView.AddOrEditPerson)
                    {
                        RenderInGlassPersonEditor(g, zones, leftPad, bodyY, contentW, bodyH, dt, t);
                    }
                    else if (currentView == ActiveView.Settings)
                    {
                        RenderInGlassSettings(g, zones, leftPad, bodyY, contentW, bodyH, dt, t);
                    }
                    g.Restore(savedState);

                    // ── ANIMATED POP-UP "ARE YOU SURE?" DELETE CONFIRMATION OVERLAY (renders smooth scale-out even on dismiss!) ──
                    if (deleteModalProgress > 0.01f && (pendingDeletePerson != null || lastDeleteModalPerson != null))
                    {
                        if (pendingDeletePerson != null) zones.Clear();
                        RenderDeleteConfirmationOverlay(g, zones, leftPad, bodyY, contentW, bodyH, dt, t, deleteModalProgress);
                    }

                    // ── FIRST-LAUNCH INTERACTIVE TUTORIAL OVERLAY ──
                    if (showTutorialOverlay && pendingDeletePerson == null)
                    {
                        zones.Clear();
                        RenderFirstLaunchTutorial(g, zones, leftPad, bodyY, contentW, bodyH, dt, t);
                    }

                    // ── ANIMATED IN-GLASS RIGHT-CLICK CONTEXT MENU (renders smooth scale-out even on dismiss!) ──
                    if (contextMenuProgress > 0.01f)
                    {
                        RenderInGlassContextMenu(g, zones, curWidW, curWidH, dt, t, contextMenuProgress);
                    }

                    hitZones = zones;

                    // Update lock-free hover transition flag & prune settled 0-progress hover keys
                    bool anyHovTrans = false;
                    List<string> zeroKeys = null;
                    foreach (KeyValuePair<string, float> kv in hoverProgress)
                    {
                        float tgt = (kv.Key == hoveredZoneId) ? 1.0f : 0.0f;
                        if (Math.Abs(kv.Value - tgt) > 0.005f)
                        {
                            anyHovTrans = true;
                        }
                        else if (tgt == 0f && kv.Value <= 0.001f)
                        {
                            if (zeroKeys == null) zeroKeys = new List<string>();
                            zeroKeys.Add(kv.Key);
                        }
                    }
                    if (zeroKeys != null)
                    {
                        for (int k = 0; k < zeroKeys.Count; k++) hoverProgress.Remove(zeroKeys[k]);
                    }
                    hasActiveHoverTransition = anyHovTrans;

                    int nextUiBuf = 1 - activeUiBufIdx;
                    int[] dstUi = nextUiBuf == 0 ? reusableUiPixelsA : reusableUiPixelsB;
                    BitmapData bd = reusableUiBmp.LockBits(new Rectangle(0, 0, curSurfW, curSurfH), ImageLockMode.ReadOnly, PixelFormat.Format32bppPArgb);
                    Marshal.Copy(bd.Scan0, dstUi, 0, dstUi.Length);
                    reusableUiBmp.UnlockBits(bd);
                    uiOverlayPixels = dstUi;
                    activeUiBufIdx = nextUiBuf;
                    Interlocked.Increment(ref uiFrameSeq);
                }
            }
        }

        private void RenderFirstLaunchTutorial(Graphics g, List<HitZone> zones, float x, float y, float w, float h, float dt, float t)
        {
            string[] titles = new string[] {
                "WELCOME TO BIRTHLY",
                "ADD PEOPLE & SWITCH VIEWS",
                "CUSTOMIZE, LAYERS & BACKUPS"
            };
            string[] bodies = new string[] {
                "DRAG THE TOP-LEFT  :::  GRIP TO MOVE BIRTHLY ANYWHERE ON YOUR DESKTOP.\nRIGHT-CLICK ANYWHERE FOR QUICK ACTIONS.",
                "CLICK '+ ADD' TO SAVE FRIENDS & FAMILY WITH PHOTOS OR ICONS.\nSEARCH, FILTER BY NEAREST / MILESTONES, OR SWITCH BETWEEN LIST & CALENDAR.",
                "OPEN 'SETTINGS' FOR GLASS TINTS, FONTS, LIGHT/DARK MODE, DRAGGABLE LAYOUT,\nWINDOW LAYERS (ABOVE/BELOW APPS), AND SINGLE-FILE .BIRTHLY BACKUPS!"
            };

            int step = ClampInt(tutorialStep, 0, titles.Length - 1);
            float boxW = Math.Min(480f, w - 36f);
            float boxH = 236f;
            RectangleF boxRect = new RectangleF(x + (w - boxW) * 0.5f, y + (h - boxH) * 0.5f, boxW, boxH);

            float tutEase = EaseOutCubic(tutorialStepProgress);
            GraphicsState tutSt = g.Save();
            if (tutEase < 0.999f)
            {
                float ts = 0.94f + 0.06f * tutEase;
                float tcx = boxRect.X + boxRect.Width * 0.5f;
                float tcy = boxRect.Y + boxRect.Height * 0.5f;
                g.TranslateTransform(tcx, tcy + (1f - tutEase) * 6f);
                g.ScaleTransform(ts, ts);
                g.TranslateTransform(-tcx, -tcy);
            }

            // Clear underlying UI pixels inside tutorial card for pure theme-matched frosted glass
            using (GraphicsPath bp = CreateRoundRectPath(boxRect, 24f))
            {
                g.SetClip(bp);
                g.CompositingMode = CompositingMode.SourceCopy;
                using (SolidBrush clrBr = new SolidBrush(Color.Transparent)) g.FillRectangle(clrBr, boxRect);
                g.CompositingMode = CompositingMode.SourceOver;
                using (SolidBrush frostBg = new SolidBrush(GetPopupBackdropFillColor(1.0f))) g.FillPath(frostBg, bp);
                g.ResetClip();
            }
            DrawAnimatedGlassCard(g, boxRect, 24f, true, 0.3f, t, 0f);

            Font stepF = GetCachedFont(10.5f, FontStyle.Bold);
            Font titleF = GetCachedFont(18f, FontStyle.Bold);
            Font bodyF = GetCachedFont(11.5f, FontStyle.Regular);
            DrawAdaptiveString(g, string.Format("STEP {0} OF {1}", step + 1, titles.Length), stepF, new RectangleF(boxRect.X + 24f, boxRect.Y + 20f, boxW - 48f, 18f), sfCenter, 175);
            DrawAdaptiveString(g, titles[step], titleF, new RectangleF(boxRect.X + 24f, boxRect.Y + 46f, boxW - 48f, 28f), sfCenter, 252);
            DrawAdaptiveString(g, bodies[step], bodyF, new RectangleF(boxRect.X + 28f, boxRect.Y + 84f, boxW - 56f, 74f), sfCenterWrap, 225);

            // Step indicator dots (theme-tinted!)
            float dotSz = 7f;
            float dotGap = 10f;
            float dotsTotalW = titles.Length * dotSz + (titles.Length - 1) * dotGap;
            float dotStartX = boxRect.X + (boxW - dotsTotalW) * 0.5f;
            float dotY = boxRect.Bottom - 68f;
            for (int i = 0; i < titles.Length; i++)
            {
                using (SolidBrush dBr = new SolidBrush(GetThemeRimColor(i == step ? 245 : 90)))
                {
                    g.FillEllipse(dBr, dotStartX + i * (dotSz + dotGap), dotY, dotSz, dotSz);
                }
            }

            float btnY = boxRect.Bottom - 46f;
            RectangleF skipBtn = new RectangleF(boxRect.X + 22f, btnY, 96f, 30f);
            RectangleF nextBtn = new RectangleF(boxRect.Right - 22f - 134f, btnY, 134f, 30f);
            DrawAnimatedGlassButton(g, skipBtn, step == 0 ? "SKIP" : "BACK", false, GetHoverProgress("tut_left", dt), t);
            DrawAnimatedGlassButton(g, nextBtn, step == titles.Length - 1 ? "GET STARTED" : "NEXT  ›", true, GetHoverProgress("tut_next", dt), t);
            g.Restore(tutSt);
            zones.Add(new HitZone { Id = "tut_left", Bounds = skipBtn });
            zones.Add(new HitZone { Id = "tut_next", Bounds = nextBtn });
        }

        private void RenderInGlassContextMenu(Graphics g, List<HitZone> zones, float widW, float widH, float dt, float t, float prog)
        {
            string[] ids = new string[] { "ctx_add", "ctx_settings", "ctx_toggle_cal", "ctx_exit" };
            string[] labels = new string[] {
                "+ ADD PERSON",
                currentView == ActiveView.Settings ? "BACK TO HOME" : "SETTINGS",
                isCalendarView ? "SWITCH TO LIST" : "SWITCH TO CALENDAR",
                "CLOSE WIDGET"
            };

            float menuW = 186f;
            float itemH = 30f;
            float headerH = 22f;
            float pad = 8f;
            float menuH = pad * 2f + headerH + ids.Length * itemH + (ids.Length - 1) * 4f + 6f;

            // Clamp menu position cleanly inside the widget bounds
            float minX = edgeMargin + 16f;
            float minY = edgeMargin + 16f;
            float maxX = edgeMargin + widW - menuW - 16f;
            float maxY = edgeMargin + widH - menuH - 16f;
            float mx = Clamp(contextMenuPos.X, minX, Math.Max(minX, maxX));
            float my = Clamp(contextMenuPos.Y, minY, Math.Max(minY, maxY));
            RectangleF menuRect = new RectangleF(mx, my, menuW, menuH);

            // Smooth spring pop-up scale from top-left anchor
            float scale = 0.90f + 0.10f * prog;
            GraphicsState st = g.Save();
            if (Math.Abs(scale - 1.0f) > 0.001f)
            {
                g.TranslateTransform(mx, my);
                g.ScaleTransform(scale, scale);
                g.TranslateTransform(-mx, -my);
            }

            // Clear underlying UI pixels inside menuRect so background text never bleeds through!
            using (GraphicsPath mp = CreateRoundRectPath(menuRect, 16f))
            {
                g.SetClip(mp);
                g.CompositingMode = CompositingMode.SourceCopy;
                using (SolidBrush clrBr = new SolidBrush(Color.Transparent))
                {
                    g.FillRectangle(clrBr, menuRect);
                }
                g.CompositingMode = CompositingMode.SourceOver;
                using (SolidBrush frostBg = new SolidBrush(GetPopupBackdropFillColor(prog)))
                {
                    g.FillPath(frostBg, mp);
                }
                g.ResetClip();
            }

            DrawAnimatedGlassCard(g, menuRect, 16f, true, 0.25f, t, 0f);

            // Title header
            Font hf = GetCachedFont(9.5f, FontStyle.Bold);
            DrawAdaptiveString(g, "BIRTHLY", hf, new RectangleF(mx + pad, my + pad - 1f, menuW - pad * 2f, headerH - 4f), sfCenter, 175);
            sharedRimPen.Color = GetThemeRimColor(IsBasicLightMode() ? 75 : 52);
            sharedRimPen.Width = 1f;
            g.DrawLine(sharedRimPen, mx + pad + 4f, my + pad + headerH - 2f, mx + menuW - pad - 4f, my + pad + headerH - 2f);

            if (isContextMenuOpen)
            {
                zones.RemoveAll(z => z.Bounds.IntersectsWith(menuRect));
            }

            float curY = my + pad + headerH + 4f;
            for (int i = 0; i < ids.Length; i++)
            {
                RectangleF ir = new RectangleF(mx + pad, curY, menuW - pad * 2f, itemH);
                DrawAnimatedGlassButton(g, ir, labels[i], false, GetHoverProgress(ids[i], dt), t);
                if (isContextMenuOpen) zones.Add(new HitZone { Id = ids[i], Bounds = ir });
                curY += itemH + 4f;
            }

            g.Restore(st);
        }

        private void RenderDeleteConfirmationOverlay(Graphics g, List<HitZone> zones, float x, float y, float w, float h, float dt, float t, float modalProg)
        {
            PersonEntry targetPerson = pendingDeletePerson ?? lastDeleteModalPerson;
            if (targetPerson == null) return;

            // Soft translucent theme-aware veil
            RectangleF veilRect = new RectangleF(x, y, w, h);
            bool isDarkSurface = !IsBasicLightMode() && (IsBasicThemeEnabled() || IsDarkModeEnabled() || GetActiveTint() == "Obsidian");
            using (GraphicsPath vp = CreateRoundRectPath(veilRect, 24f))
            using (SolidBrush vBr = new SolidBrush(isDarkSurface
                ? Color.FromArgb((int)(110 * modalProg), 12, 16, 24)
                : Color.FromArgb((int)(45 * modalProg), 215, 228, 248)))
            {
                g.FillPath(vBr, vp);
            }

            // Centered Liquid Glass Confirmation Card with Spring Pop-Up Scale!
            float boxW = Math.Min(380f, w - 40f);
            float boxH = 215f;
            RectangleF boxRect = new RectangleF(x + (w - boxW) * 0.5f, y + (h - boxH) * 0.5f, boxW, boxH);

            float scale = 0.88f + 0.12f * modalProg;
            GraphicsState st = g.Save();
            if (Math.Abs(scale - 1.0f) > 0.001f)
            {
                float cx = boxRect.X + boxRect.Width * 0.5f;
                float cy = boxRect.Y + boxRect.Height * 0.5f;
                g.TranslateTransform(cx, cy);
                g.ScaleTransform(scale, scale);
                g.TranslateTransform(-cx, -cy);
            }

            // Clear underlying UI pixels inside the modal card so background cards never bleed through!
            using (GraphicsPath bp = CreateRoundRectPath(boxRect, 24f))
            {
                g.SetClip(bp);
                g.CompositingMode = CompositingMode.SourceCopy;
                using (SolidBrush clrBr = new SolidBrush(Color.Transparent))
                {
                    g.FillRectangle(clrBr, boxRect);
                }
                g.CompositingMode = CompositingMode.SourceOver;
                using (SolidBrush frostedBg = new SolidBrush(GetPopupBackdropFillColor(modalProg)))
                {
                    g.FillPath(frostedBg, bp);
                }
                g.ResetClip();
            }
            DrawAnimatedGlassCard(g, boxRect, 24f, true, 0.35f * modalProg, t, 0f);

            float avSize = 54f;
            RectangleF avRect = new RectangleF(boxRect.X + (boxW - avSize) * 0.5f, boxRect.Y + 20f, avSize, avSize);
            DrawMinimalAvatar(g, targetPerson, avRect, t);

            Font titleF = GetCachedFont(15.5f, FontStyle.Bold);
            Font subF = GetCachedFont(11.5f, FontStyle.Regular);
            string promptTitle = "DELETE " + (targetPerson.Name ?? "PROFILE").ToUpper() + "?";
            DrawAdaptiveString(g, promptTitle, titleF, new RectangleF(boxRect.X + 16f, boxRect.Y + 84f, boxW - 32f, 24f), sfCenter, 250);
            DrawAdaptiveString(g, "ARE YOU SURE YOU WANT TO REMOVE THIS BIRTHDAY?", subF, new RectangleF(boxRect.X + 16f, boxRect.Y + 112f, boxW - 32f, 20f), sfCenter, 185);

            float btnW = 132f;
            float btnH = 34f;
            float gap = 16f;
            float startX = boxRect.X + (boxW - (btnW * 2 + gap)) * 0.5f;
            float btnY = boxRect.Bottom - 50f;

            RectangleF keepRect = new RectangleF(startX, btnY, btnW, btnH);
            RectangleF confirmDelRect = new RectangleF(startX + btnW + gap, btnY, btnW, btnH);

            DrawAnimatedGlassButton(g, keepRect, "CANCEL", false, GetHoverProgress("confirm_del_no", dt), t);
            DrawAnimatedGlassButton(g, confirmDelRect, "YES, DELETE", true, GetHoverProgress("confirm_del_yes", dt), t);

            g.Restore(st);

            if (pendingDeletePerson != null)
            {
                zones.Add(new HitZone { Id = "confirm_del_no", Bounds = keepRect });
                zones.Add(new HitZone { Id = "confirm_del_yes", Bounds = confirmDelRect, Person = pendingDeletePerson });
            }
        }

        private static float Clamp(float v, float min, float max)
        {
            return v < min ? min : (v > max ? max : v);
        }

        private void RenderMinimalDashboard(Graphics g, List<HitZone> zones, List<PersonEntry> sorted, DateTime now, float x, float y, float w, float h, float dt, float t)
        {
            if (sorted.Count == 0)
            {
                RectangleF emptyRect = new RectangleF(x, y, w, h);
                DrawAnimatedGlassCard(g, emptyRect, 22f, false, 0f, t, 0f);
                Font f1 = GetCachedFont(16f, FontStyle.Bold);
                Font f2 = GetCachedFont(12.5f, FontStyle.Regular);
                DrawAdaptiveString(g, "NO BIRTHDAYS ADDED", f1, new RectangleF(x, y + h * 0.38f, w, 26f), sfCenter, 235);
                DrawAdaptiveString(g, "CLICK '+ ADD' IN THE TOP RIGHT TO ADD A PERSON", f2, new RectangleF(x, y + h * 0.48f, w, 22f), sfCenter, 155);
                return;
            }

            if (heroSpotlightIndex < 0 || heroSpotlightIndex >= sorted.Count) heroSpotlightIndex = 0;

            // ── BIRTHDAY TODAY MESSAGE BANNER (Top of Dashboard, toggleable in Settings & dismissible!) ──
            List<PersonEntry> bdayTodayList = sorted.Where(p => p.GetDaysUntil(now) == 0).ToList();
            if (config.EnableBirthdayMessage && !bdayBannerDismissed && bdayTodayList.Count > 0)
            {
                float bannerH = 34f;
                RectangleF bannerRect = new RectangleF(x, y, w, bannerH);
                float bPulse = 0.5f + 0.5f * (float)Math.Sin(t * 3.8f);
                DrawAnimatedGlassCard(g, bannerRect, 14f, true, 0.35f + bPulse * 0.25f, t, 0f);

                PersonEntry bp = bdayTodayList[0];
                int bTurn = bp.GetTurningAge(now);
                string bMsg;
                if (bdayTodayList.Count == 1)
                {
                    bMsg = bTurn > 0
                        ? string.Format("★  TODAY IS {0}'S BIRTHDAY!  •  TURNS {1} TODAY!", (bp.Name ?? "FRIEND").ToUpper(), bTurn)
                        : string.Format("★  TODAY IS {0}'S BIRTHDAY!  •  DON'T FORGET TO WISH THEM!", (bp.Name ?? "FRIEND").ToUpper());
                }
                else
                {
                    string allNames = string.Join(" & ", bdayTodayList.Select(p => (p.Name ?? "FRIEND").ToUpper()).ToArray());
                    bMsg = string.Format("★  {0} HAVE THEIR BIRTHDAY TODAY!", allNames);
                }

                RectangleF bDismissBtn = new RectangleF(bannerRect.Right - 32f, bannerRect.Y + 4f, 26f, 26f);
                RectangleF bWishBtn = new RectangleF(bDismissBtn.X - 96f, bannerRect.Y + 4f, 90f, 26f);
                bool bHasWish = bp != null && !string.IsNullOrWhiteSpace(bp.Notes);
                bool bWishCopied = bHasWish && (t < wishCopiedUntilSec);
                if (bHasWish)
                {
                    DrawAnimatedGlassButton(g, bWishBtn, bWishCopied ? L("COPIED ✓") : L("COPY WISH"), true, GetHoverProgress("btn_banner_wish", dt), t);
                    zones.Add(new HitZone { Id = "btn_copy_wish", Bounds = bWishBtn, Person = bp });
                }
                else
                {
                    DrawDisabledGlassButton(g, bWishBtn, L("COPY WISH"));
                }
                DrawAnimatedGlassButton(g, bDismissBtn, "×", false, GetHoverProgress("btn_banner_close", dt), t);
                zones.Add(new HitZone { Id = "btn_banner_close", Bounds = bDismissBtn });

                Font bf = GetCachedFont(11.5f, FontStyle.Bold);
                RectangleF bTxtRect = new RectangleF(bannerRect.X + 14f, bannerRect.Y, Math.Max(60f, bWishBtn.X - bannerRect.X - 20f), bannerRect.Height);
                DrawAdaptiveString(g, bMsg, bf, bTxtRect, sfNearCenter, 252);

                y += bannerH + 10f;
                h -= (bannerH + 10f);
            }

            string layout = GetActiveLayoutMode();
            float gap = 16f;
            RectangleF targetHeroRect;
            RectangleF targetPanelRect;
            bool horizontalHero = (layout == "HeroTop" || layout == "HeroBottom");

            if (layout == "HeroRight")
            {
                float heroW = Math.Max(235f, Math.Min(275f, w * 0.355f));
                targetPanelRect = new RectangleF(x, y, w - heroW - gap, h);
                targetHeroRect = new RectangleF(targetPanelRect.Right + gap, y, heroW, h);
            }
            else if (layout == "HeroTop")
            {
                float heroH = Math.Min(116f, h * 0.30f);
                targetHeroRect = new RectangleF(x, y, w, heroH);
                targetPanelRect = new RectangleF(x, targetHeroRect.Bottom + 12f, w, h - heroH - 12f);
            }
            else if (layout == "HeroBottom")
            {
                float heroH = Math.Min(116f, h * 0.30f);
                targetPanelRect = new RectangleF(x, y, w, h - heroH - 12f);
                targetHeroRect = new RectangleF(x, targetPanelRect.Bottom + 12f, w, heroH);
            }
            else // HeroLeft (default)
            {
                float heroW = Math.Max(235f, Math.Min(275f, w * 0.355f));
                targetHeroRect = new RectangleF(x, y, heroW, h);
                targetPanelRect = new RectangleF(x + heroW + gap, y, w - heroW - gap, h);
            }

            // Smooth spring-interpolated layout transition when switching between HeroLeft / HeroRight / HeroTop / HeroBottom!
            float layLerp = Math.Min(1.0f, dt * 15.5f);
            targetDashHeroRect = targetHeroRect;
            targetDashPanelRect = targetPanelRect;
            animDashHeroRect = LerpRect(animDashHeroRect, targetHeroRect, layLerp);
            animDashPanelRect = LerpRect(animDashPanelRect, targetPanelRect, layLerp);
            RectangleF heroRect = animDashHeroRect;
            RectangleF panelRect = animDashPanelRect;

            // ── HERO CARD (with Spotlight Stepper + Accurate Exact Countdown + Prominent Age Display + One-Click Copy Wish!) ──
            PersonEntry nextPerson = sorted[heroSpotlightIndex];
            string nextNameSafe = (nextPerson.Name ?? "UNNAMED").ToUpper();
            int nextDays = nextPerson.GetDaysUntil(now);
            DateTime nextDate = nextPerson.GetNextBirthday(now);
            TimeSpan exactDiff = nextDays == 0 ? (nextDate.Date.AddDays(1) - now) : (nextDate.Date - now);
            bool isNear = nextDays <= config.AlertUrgentDays;
            DrawAnimatedGlassCard(g, heroRect, 24f, isNear, 0f, t, 0f);

            // Smooth subtle cubic ease-out pop-scale when cycling Hero spotlight ‹ / ›
            float heroEase = EaseOutCubic(heroStepperProgress);
            GraphicsState heroSt = g.Save();
            if (heroEase < 0.999f)
            {
                float hs = 0.95f + 0.05f * heroEase;
                float hcx = heroRect.X + heroRect.Width * 0.5f;
                float hcy = heroRect.Y + heroRect.Height * 0.5f;
                g.TranslateTransform(hcx, hcy);
                g.ScaleTransform(hs, hs);
                g.TranslateTransform(-hcx, -hcy);
            }

            string statusLabel = nextDays == 0
                ? "TODAY"
                : (nextDays == 1 ? "TOMORROW" : (heroSpotlightIndex > 0 ? ("#" + (heroSpotlightIndex + 1) + " UPCOMING") : (isNear ? "IN " + nextDays + " DAYS" : "NEXT UPCOMING")));
            float pulseGlow = isNear ? (0.5f + 0.5f * (float)Math.Sin(t * 3.5f)) : (0.25f + 0.25f * (float)Math.Sin(t * 1.8f));

            // Accurate countdown: use exactDiff.Days so 10 hours before a birthday shows "00 DAYS 10 HRS", never "01 DAYS 10 HRS"!
            int exactDays = nextDays == 0 ? 0 : Math.Max(0, (int)exactDiff.TotalDays);
            string dStr = exactDays.ToString("00");
            string hStr = nextDays == 0 ? "00" : Math.Max(0, exactDiff.Hours).ToString("00");
            string mStr = nextDays == 0 ? "00" : Math.Max(0, exactDiff.Minutes).ToString("00");
            string sStr = nextDays == 0 ? "00" : Math.Max(0, exactDiff.Seconds).ToString("00");
            float secPhase = (float)(now.Millisecond / 1000.0);
            float secPulse = (float)Math.Exp(-secPhase * 4.5f);
            bool wishCopied = (t < wishCopiedUntilSec);

            if (horizontalHero)
            {
                bool tightHoriz = heroRect.Height < 102f;
                float avSz = Math.Min(tightHoriz ? 58f : 68f, heroRect.Height - 24f);
                RectangleF avRect = new RectangleF(heroRect.X + 16f, heroRect.Y + (heroRect.Height - avSz) * 0.5f, avSz, avSz);
                DrawMinimalAvatar(g, nextPerson, avRect, t);

                float tileW = tightHoriz ? 44f : 48f;
                float tileH = tightHoriz ? 44f : 48f;
                float tileGap = 6f;
                float tilesW = tileW * 4 + tileGap * 3;
                float editW = 64f;
                float wishW = 80f;
                float editX = heroRect.Right - 14f - editW;
                float wishX = editX - 6f - wishW;
                float stepW = sorted.Count > 1 ? 54f : 0f;
                float tileStartX = wishX - 10f - tilesW;
                float tileY = heroRect.Y + (heroRect.Height - tileH) * 0.5f;

                if (sorted.Count > 1)
                {
                    float stY = heroRect.Y + (heroRect.Height - 24f) * 0.5f;
                    RectangleF prevHeroBtn = new RectangleF(tileStartX - 58f, stY, 24f, 24f);
                    RectangleF nextHeroBtn = new RectangleF(tileStartX - 30f, stY, 24f, 24f);
                    DrawAnimatedGlassButton(g, prevHeroBtn, "‹", false, GetHoverProgress("hero_prev", dt), t);
                    DrawAnimatedGlassButton(g, nextHeroBtn, "›", false, GetHoverProgress("hero_next", dt), t);
                    zones.Add(new HitZone { Id = "hero_prev", Bounds = prevHeroBtn });
                    zones.Add(new HitZone { Id = "hero_next", Bounds = nextHeroBtn });
                }

                float infoX = avRect.Right + 12f;
                float infoMaxW = Math.Max(60f, tileStartX - stepW - infoX - 10f);
                Font nameFont = GetCachedFont(tightHoriz ? 15.5f : 17f, FontStyle.Bold);
                Font dateFont = GetCachedFont(tightHoriz ? 10.5f : 11f, FontStyle.Regular);
                Font ageBigF = GetCachedFont(tightHoriz ? 12f : 13f, FontStyle.Bold);
                DrawAdaptiveString(g, nextNameSafe, nameFont, new RectangleF(infoX, heroRect.Y + (tightHoriz ? 10f : 14f), infoMaxW, 22f), sfNearCenter, 248);
                int age = nextPerson.GetCurrentAge(now);
                int turnAge = nextPerson.GetTurningAge(now);
                string dateLine = statusLabel + "  •  " + nextDate.ToString("dd MMM").ToUpper();
                DrawAdaptiveString(g, dateLine, dateFont, new RectangleF(infoX, heroRect.Y + (tightHoriz ? 32f : 38f), infoMaxW, 16f), sfNearCenter, 175);
                if (config.ShowAgeBadge && nextPerson.BirthYear >= 1900)
                {
                    string ageLine = nextDays == 0
                        ? string.Format("TURNS {0} TODAY!", turnAge)
                        : string.Format("AGE {0}  →  TURNS {1}", age, turnAge);
                    DrawAdaptiveString(g, ageLine, ageBigF, new RectangleF(infoX, heroRect.Y + (tightHoriz ? 50f : 58f), infoMaxW, 19f), sfNearCenter, 238);
                }

                DrawMinimalCountdownTile(g, new RectangleF(tileStartX, tileY, tileW, tileH), dStr, "DAYS", 0f, t);
                DrawMinimalCountdownTile(g, new RectangleF(tileStartX + (tileW + tileGap), tileY, tileW, tileH), hStr, "HRS", 0f, t + 0.5f);
                DrawMinimalCountdownTile(g, new RectangleF(tileStartX + (tileW + tileGap) * 2f, tileY, tileW, tileH), mStr, "MIN", 0f, t + 1.0f);
                DrawMinimalCountdownTile(g, new RectangleF(tileStartX + (tileW + tileGap) * 3f, tileY, tileW, tileH), sStr, "SEC", secPulse, t + 1.5f);

                RectangleF wishBtn = new RectangleF(wishX, heroRect.Y + (heroRect.Height - 28f) * 0.5f, wishW, 28f);
                RectangleF editBtn = new RectangleF(editX, heroRect.Y + (heroRect.Height - 28f) * 0.5f, editW, 28f);
                bool hasWish = !string.IsNullOrWhiteSpace(nextPerson.Notes);
                if (hasWish)
                {
                    DrawAnimatedGlassButton(g, wishBtn, wishCopied ? L("COPIED ✓") : L("WISH"), wishCopied, GetHoverProgress("btn_copy_wish", dt), t);
                    zones.Add(new HitZone { Id = "btn_copy_wish", Bounds = wishBtn, Person = nextPerson });
                }
                else
                {
                    DrawDisabledGlassButton(g, wishBtn, L("WISH"));
                }
                DrawAnimatedGlassButton(g, editBtn, L("EDIT"), false, GetHoverProgress("btn_hero_edit", dt), t);
                zones.Add(new HitZone { Id = "btn_hero_edit", Bounds = editBtn, Person = nextPerson });
            }
            else
            {
                float heroW = heroRect.Width;
                float badgeW = Math.Min(138f, heroW - 76f);
                RectangleF badgeRect = new RectangleF(heroRect.X + (heroW - badgeW) / 2f, heroRect.Y + 16f, badgeW, 24f);
                DrawAnimatedGlassButton(g, badgeRect, statusLabel, isNear, pulseGlow, t);
                if (heroSpotlightIndex > 0) zones.Add(new HitZone { Id = "hero_reset", Bounds = badgeRect });

                if (sorted.Count > 1)
                {
                    RectangleF prevHeroBtn = new RectangleF(badgeRect.X - 28f, heroRect.Y + 16f, 24f, 24f);
                    RectangleF nextHeroBtn = new RectangleF(badgeRect.Right + 4f, heroRect.Y + 16f, 24f, 24f);
                    DrawAnimatedGlassButton(g, prevHeroBtn, "‹", false, GetHoverProgress("hero_prev", dt), t);
                    DrawAnimatedGlassButton(g, nextHeroBtn, "›", false, GetHoverProgress("hero_next", dt), t);
                    zones.Add(new HitZone { Id = "hero_prev", Bounds = prevHeroBtn });
                    zones.Add(new HitZone { Id = "hero_next", Bounds = nextHeroBtn });
                }

                bool compactH = heroRect.Height < 375f;
                float avSize = compactH ? 66f : 78f;
                float avTop = heroRect.Y + (compactH ? 46f : 52f);
                RectangleF avRect = new RectangleF(heroRect.X + (heroW - avSize) / 2f, avTop, avSize, avSize);
                DrawMinimalAvatar(g, nextPerson, avRect, t);

                float nameY = avRect.Bottom + (compactH ? 8f : 12f);
                Font nameFont = GetCachedFont(compactH ? 18.5f : 20.5f, FontStyle.Bold);
                Font dateFont = GetCachedFont(11.5f, FontStyle.Regular);
                DrawAdaptiveString(g, nextNameSafe, nameFont, new RectangleF(heroRect.X + 12f, nameY, heroW - 24f, 25f), sfCenter, 248);

                string dateLine = nextDate.ToString("ddd, dd MMMM").ToUpper();
                DrawAdaptiveString(g, dateLine, dateFont, new RectangleF(heroRect.X + 12f, nameY + 25f, heroW - 24f, 18f), sfCenter, 180);

                float tileGap = 6f;
                float tileW = Math.Min(52f, (heroW - 32f - tileGap * 3f) / 4f);
                float tileH = compactH ? 46f : 50f;
                float totalTilesW = tileW * 4 + tileGap * 3;
                float startTileX = heroRect.X + (heroW - totalTilesW) / 2f;
                float tileY = nameY + (compactH ? 47f : 52f);

                DrawMinimalCountdownTile(g, new RectangleF(startTileX, tileY, tileW, tileH), dStr, "DAYS", 0f, t);
                DrawMinimalCountdownTile(g, new RectangleF(startTileX + (tileW + tileGap), tileY, tileW, tileH), hStr, "HRS", 0f, t + 0.5f);
                DrawMinimalCountdownTile(g, new RectangleF(startTileX + (tileW + tileGap) * 2f, tileY, tileW, tileH), mStr, "MIN", 0f, t + 1.0f);
                DrawMinimalCountdownTile(g, new RectangleF(startTileX + (tileW + tileGap) * 3f, tileY, tileW, tileH), sStr, "SEC", secPulse, t + 1.5f);

                // ── PROMINENT BIG AGE DISPLAY (at the exact place of the old note display below DAYS / HRS / MIN / SEC!) ──
                float ageBoxTop = tileY + tileH + (compactH ? 7f : 10f);
                float footBtnY = heroRect.Bottom - (compactH ? 38f : 42f);
                float availAgeH = footBtnY - ageBoxTop - (compactH ? 6f : 8f);
                if (config.ShowAgeBadge && availAgeH >= 22f)
                {
                    int curAge = nextPerson.GetCurrentAge(now);
                    int turnAge = nextPerson.GetTurningAge(now);
                    bool hasYr = nextPerson.BirthYear >= 1900;
                    float pillH = Math.Min(hasYr ? 54f : 36f, availAgeH);
                    RectangleF ageCardRect = new RectangleF(startTileX, ageBoxTop + (availAgeH - pillH) * 0.5f, totalTilesW, pillH);
                    DrawAnimatedGlassCard(g, ageCardRect, 11f, nextDays == 0 || isNear, 0.15f, t, 0.4f);

                    string bigAgeTxt;
                    if (hasYr)
                    {
                        bigAgeTxt = nextDays == 0
                            ? string.Format("★  TURNS {0} TODAY!  ★", turnAge)
                            : string.Format("AGE {0}   •   TURNS {1}", curAge, turnAge);
                        int bm = ClampInt(nextPerson.Month, 1, 12);
                        int bd = ClampInt(nextPerson.Day, 1, DateTime.DaysInMonth(nextPerson.BirthYear, bm));
                        int daysLived = Math.Max(0, (int)(now.Date - new DateTime(nextPerson.BirthYear, bm, bd)).TotalDays);
                        if (pillH >= 40f)
                        {
                            Font bigAgeF = GetCachedFont(compactH ? 11.5f : 12.5f, FontStyle.Bold);
                            Font livedF = GetCachedFont(compactH ? 10.5f : 11.5f, FontStyle.Bold);
                            DrawAdaptiveString(g, bigAgeTxt, bigAgeF, new RectangleF(ageCardRect.X, ageCardRect.Y + 4f, ageCardRect.Width, pillH * 0.46f), sfCenter, 248);
                            DrawAdaptiveString(g, string.Format("{0:N0} DAYS LIVED", daysLived), livedF, new RectangleF(ageCardRect.X, ageCardRect.Y + pillH * 0.48f, ageCardRect.Width, pillH * 0.46f), sfCenter, 232);
                        }
                        else
                        {
                            Font bigAgeF = GetCachedFont(compactH ? 11f : 12f, FontStyle.Bold);
                            DrawAdaptiveString(g, string.Format("{0} • {1:N0}D", bigAgeTxt, daysLived), bigAgeF, ageCardRect, sfCenter, 248);
                        }
                    }
                    else
                    {
                        bigAgeTxt = nextDays == 0 ? "★  BIRTHDAY TODAY!  ★" : "ADD BIRTH YEAR FOR AGE";
                        Font bigAgeF = GetCachedFont(compactH ? 12.5f : 13.5f, FontStyle.Bold);
                        DrawAdaptiveString(g, bigAgeTxt, bigAgeF, ageCardRect, sfCenter, 165);
                    }
                }

                float footGap = 8f;
                float footTotalW = heroW - 36f;
                float wishBtnW = footTotalW * 0.54f;
                float editBtnW = footTotalW - wishBtnW - footGap;
                RectangleF wishBtn = new RectangleF(heroRect.X + 18f, footBtnY, wishBtnW, 28f);
                RectangleF editBtn = new RectangleF(wishBtn.Right + footGap, footBtnY, editBtnW, 28f);
                bool hasWish = !string.IsNullOrWhiteSpace(nextPerson.Notes);
                if (hasWish)
                {
                    DrawAnimatedGlassButton(g, wishBtn, wishCopied ? L("COPIED ✓") : L("COPY WISH"), wishCopied, GetHoverProgress("btn_copy_wish", dt), t);
                    zones.Add(new HitZone { Id = "btn_copy_wish", Bounds = wishBtn, Person = nextPerson });
                }
                else
                {
                    DrawDisabledGlassButton(g, wishBtn, L("COPY WISH"));
                }
                DrawAnimatedGlassButton(g, editBtn, L("EDIT"), false, GetHoverProgress("btn_hero_edit", dt), t);
                zones.Add(new HitZone { Id = "btn_hero_edit", Bounds = editBtn, Person = nextPerson });
            }
            g.Restore(heroSt);

            // ── SHORT-TIME BIRTHDAY CELEBRATION ANIMATION (Sparkles & Floating Confetti Burst when someone has a birthday or is previewed!) ──
            bool hasBdayForAnim = (nextDays == 0) || (bdayTodayList.Count > 0);
            float bdayAnimElapsed = t - bdayCelebrationStartSec;
            hasActiveBdayParticles = config.EnableBirthdayAnimation && hasBdayForAnim && bdayAnimElapsed >= 0f && bdayAnimElapsed <= BdayCelebrationDurationSec;
            if (hasActiveBdayParticles)
            {
                float fadeOut = Clamp01((BdayCelebrationDurationSec - bdayAnimElapsed) / 2.2f);
                float fadeIn = Clamp01(bdayAnimElapsed / 0.5f);
                float masterA = fadeIn * fadeOut;
                RenderBirthdayCelebrationParticles(g, heroRect, bdayAnimElapsed, masterA);
            }

            // ── MAIN PANEL TOP CONTROLS: SEARCH BAR + FILTER DROPDOWN + LIST/CALENDAR SWITCHER ──
            float rightX = panelRect.X;
            float rightY = panelRect.Y;
            float rightW = panelRect.Width;
            float rightH = panelRect.Height;

            float viewToggleW = 92f;
            float filterBtnW = 122f;
            float ctrlGap = 8f;
            float searchW = Math.Max(110f, rightW - viewToggleW - filterBtnW - ctrlGap * 2f);

            RectangleF searchRect = new RectangleF(rightX, rightY, searchW, 28f);
            DrawInGlassSearchBox(g, searchRect, searchQuery, isSearchFocused, GetHoverProgress("search_bar", dt), t);
            zones.Add(new HitZone { Id = "search_bar", Bounds = searchRect });

            if ( !string.IsNullOrEmpty(searchQuery))
            {
                RectangleF clearSearchRect = new RectangleF(searchRect.Right - 26f, searchRect.Y + 1f, 24f, 26f);
                float clrHov = GetHoverProgress("search_clear", dt);
                Font xf = GetCachedFont(13f, FontStyle.Regular);
                DrawAdaptiveString(g, "×", xf, clearSearchRect, sfCenter, (int)(145 + clrHov * 110));
                zones.Add(new HitZone { Id = "search_clear", Bounds = clearSearchRect });
            }

            string filterLabel = selectedSortFilter == "Furthest" ? "FURTHEST ▾"
                : (selectedSortFilter == "Milestones" ? "MILESTONES ▾"
                : (selectedSortFilter == "Alpha" ? "NAME A-Z ▾"
                : (selectedSortFilter == "AlphaDesc" ? "NAME Z-A ▾"
                : (selectedSortFilter == "Age" ? "AGE (OLD) ▾"
                : (selectedSortFilter == "AgeYoung" ? "AGE (YOUNG) ▾"
                : (selectedSortFilter == "Month" ? "BY MONTH ▾"
                : (selectedSortFilter == "ThisMonth" ? "THIS MONTH ▾" : "NEAREST ▾")))))));
            RectangleF filterBtnRect = new RectangleF(searchRect.Right + ctrlGap, rightY, filterBtnW, 28f);
            DrawAnimatedGlassButton(g, filterBtnRect, filterLabel, isFilterDropdownOpen || selectedSortFilter != "Nearest", GetHoverProgress("btn_filter_dd", dt), t);
            zones.Add(new HitZone { Id = "btn_filter_dd", Bounds = filterBtnRect });

            float swX = filterBtnRect.Right + ctrlGap;
            float swHalf = (viewToggleW - 4f) * 0.5f;
            RectangleF listSwRect = new RectangleF(swX, rightY, swHalf, 28f);
            RectangleF calSwRect = new RectangleF(swX + swHalf + 4f, rightY, swHalf, 28f);
            DrawAnimatedGlassButton(g, listSwRect, "LIST", !isCalendarView, GetHoverProgress("view_list", dt), t);
            DrawAnimatedGlassButton(g, calSwRect, "CAL", isCalendarView, GetHoverProgress("view_cal", dt), t);
            zones.Add(new HitZone { Id = "view_list", Bounds = listSwRect });
            zones.Add(new HitZone { Id = "view_cal", Bounds = calSwRect });

            float bodyAreaY = rightY + 38f;
            float bodyAreaH = rightH - 38f;

            // Smooth cubic ease-out pop-scale transition when switching between LIST and CAL views!
            float calEase = EaseOutCubic(calendarViewProgress);
            GraphicsState bodySt = g.Save();
            if (calEase < 0.999f)
            {
                float cs = 0.95f + 0.05f * calEase;
                float bcx = rightX + rightW * 0.5f;
                float bcy = bodyAreaY + bodyAreaH * 0.5f;
                g.TranslateTransform(bcx, bcy);
                g.ScaleTransform(cs, cs);
                g.TranslateTransform(-bcx, -bcy);
            }

            if (isCalendarView)
            {
                RenderCalendarView(g, zones, sorted, now, rightX, bodyAreaY, rightW, bodyAreaH, dt, t);
            }
            else
            {
                IEnumerable<PersonEntry> queryList = sorted.Where(p => {
                    if (selectedSortFilter == "ThisMonth" && p.Month != now.Month) return false;
                    if (!string.IsNullOrWhiteSpace(searchQuery))
                    {
                        string q = searchQuery.Trim().ToLowerInvariant();
                        string n = (p.Name ?? "").ToLowerInvariant();
                        string notes = (p.Notes ?? "").ToLowerInvariant();
                        string bDate = p.GetNextBirthday(now).ToString("dd MMMM").ToLowerInvariant();
                        if (!n.Contains(q) && !notes.Contains(q) && !bDate.Contains(q)) return false;
                    }
                    return true;
                });

                List<PersonEntry> filtered;
                if (selectedSortFilter == "Furthest")
                {
                    filtered = queryList.OrderByDescending(p => p.GetDaysUntil(now)).ThenBy(p => p.Name ?? "").ToList();
                }
                else if (selectedSortFilter == "Milestones")
                {
                    filtered = queryList.OrderBy(p => {
                        int ms, du;
                        return p.GetNextMilestone(now, out ms, out du) ? du : 999999;
                    }).ThenBy(p => p.GetDaysUntil(now)).ToList();
                }
                else if (selectedSortFilter == "Alpha")
                {
                    filtered = queryList.OrderBy(p => p.Name ?? "", StringComparer.OrdinalIgnoreCase).ToList();
                }
                else if (selectedSortFilter == "AlphaDesc")
                {
                    filtered = queryList.OrderByDescending(p => p.Name ?? "", StringComparer.OrdinalIgnoreCase).ToList();
                }
                else if (selectedSortFilter == "Age")
                {
                    filtered = queryList.OrderByDescending(p => p.BirthYear >= 1900 ? p.GetCurrentAge(now) : -1).ThenBy(p => p.GetDaysUntil(now)).ToList();
                }
                else if (selectedSortFilter == "AgeYoung")
                {
                    filtered = queryList.OrderBy(p => p.BirthYear >= 1900 ? p.GetCurrentAge(now) : 9999).ThenBy(p => p.GetDaysUntil(now)).ToList();
                }
                else if (selectedSortFilter == "Month")
                {
                    filtered = queryList.OrderBy(p => p.Month).ThenBy(p => p.Day).ThenBy(p => p.Name ?? "").ToList();
                }
                else if (selectedSortFilter == "ThisMonth")
                {
                    filtered = queryList.OrderBy(p => p.GetDaysUntil(now)).ThenBy(p => p.Day).ToList();
                }
                else
                {
                    filtered = queryList.OrderBy(p => p.GetDaysUntil(now)).ThenBy(p => p.Name ?? "").ToList();
                }

                bool compactList = bodyAreaH < 215f;
                float rowH = compactList ? 48f : 58f;
                float rowGap = compactList ? 6f : 8f;
                int maxRows = Math.Max(1, (int)((bodyAreaH + rowGap) / (rowH + rowGap)));
                bool hasScroll = filtered.Count > maxRows;
                float effRowW = hasScroll ? (rightW - 8f) : rightW;

                if (listScrollOffset > Math.Max(0, filtered.Count - maxRows))
                    listScrollOffset = Math.Max(0, filtered.Count - maxRows);

                if (filtered.Count == 0)
                {
                    RectangleF noRect = new RectangleF(rightX, bodyAreaY, rightW, bodyAreaH);
                    DrawAnimatedGlassCard(g, noRect, 18f, false, 0f, t, 1f);
                    Font nf = GetCachedFont(12.5f, FontStyle.Regular);
                    string emptyMsg = "NO PEOPLE MATCHING \"" + searchQuery.Trim().ToUpper() + "\"";
                    DrawAdaptiveString(g, emptyMsg, nf, noRect, sfCenter, 165);
                }
                else
                {
                    int endIdx = Math.Min(filtered.Count, listScrollOffset + maxRows);
                    for (int i = listScrollOffset; i < endIdx; i++)
                    {
                        PersonEntry p = filtered[i];
                        int rIdx = i - listScrollOffset;
                        float rowHovProg = GetHoverProgress("row_" + p.Id, dt);

                        float ry = bodyAreaY + rIdx * (rowH + rowGap);
                        RectangleF rowRect = new RectangleF(rightX, ry, effRowW, rowH);

                        int days = p.GetDaysUntil(now);
                        bool pNear = days <= config.AlertUrgentDays;

                        int msDays = 0, msUntil = 999999;
                        bool hasMs = (selectedSortFilter == "Milestones") && p.GetNextMilestone(now, out msDays, out msUntil);
                        bool highlightRow = hasMs ? (msUntil <= 14) : pNear;

                        DrawAnimatedGlassCard(g, rowRect, compactList ? 14f : 16f, highlightRow, rowHovProg, t, rIdx * 0.7f);

                        float avSz = compactList ? 32f : 38f;
                        RectangleF rAvRect = new RectangleF(rowRect.X + 12f, rowRect.Y + (rowH - avSz) * 0.5f, avSz, avSz);
                        DrawMinimalAvatar(g, p, rAvRect, t);

                        string countTxt;
                        float pillW = 78f;
                        if (selectedSortFilter == "Milestones" && hasMs)
                        {
                            pillW = 104f;
                            countTxt = msUntil == 0 ? (msDays.ToString("N0") + "D TODAY!") : ("IN " + msUntil + "D");
                        }
                        else
                        {
                            countTxt = days == 0 ? "TODAY" : (days == 1 ? "TOMORROW" : days + "D");
                        }
                        float pillH = compactList ? 24f : 28f;
                        float pillY = rowRect.Y + (rowH - pillH) * 0.5f;
                        RectangleF cPill = new RectangleF(rowRect.Right - 42f - pillW, pillY, pillW, pillH);
                        float textLeft = rAvRect.Right + 10f;
                        float textMaxW = Math.Max(60f, cPill.X - textLeft - 8f);

                        Font nf = GetCachedFont(compactList ? 13f : 14f, FontStyle.Bold);
                        Font df = GetCachedFont(compactList ? 10.5f : 11.5f, FontStyle.Regular);
                        DrawAdaptiveString(g, (p.Name ?? "UNNAMED").ToUpper(), nf, new RectangleF(textLeft, rowRect.Y + (compactList ? 5f : 8f), textMaxW, 19f), sfNearCenter, 245);

                        DateTime nbDate = p.GetNextBirthday(now);
                        string subInfo;
                        if (selectedSortFilter == "Milestones")
                        {
                            int lived = p.GetDaysLived(now);
                            if (hasMs)
                                subInfo = string.Format("{0:N0} DAYS LIVED  •  NEXT: {1:N0}D", lived, msDays);
                            else if (lived >= 0)
                                subInfo = string.Format("{0:N0} DAYS LIVED", lived);
                            else
                                subInfo = "ADD BIRTH YEAR FOR DAY MILESTONES";
                        }
                        else
                        {
                            subInfo = nbDate.ToString("dd MMM").ToUpper();
                            int cAge = p.GetCurrentAge(now);
                            int lived = p.GetDaysLived(now);
                            if (config.ShowAgeBadge && p.BirthYear >= 1900)
                                subInfo += string.Format("  •  AGE {0}  •  {1:N0}D LIVED", cAge, Math.Max(0, lived));
                        }

                        DrawAdaptiveString(g, subInfo, df, new RectangleF(textLeft, rowRect.Y + (compactList ? 25f : 30f), textMaxW, 17f), sfNearCenter, 175);

                        DrawAnimatedGlassButton(g, cPill, countTxt, highlightRow, rowHovProg * 0.6f, t);

                        RectangleF delRect = new RectangleF(rowRect.Right - 36f, pillY, 26f, pillH);
                        float delHov = GetHoverProgress("del_" + p.Id, dt);
                        Font xf = GetCachedFont(14f, FontStyle.Regular);
                        DrawAdaptiveString(g, "×", xf, delRect, sfCenter, (int)(130 + delHov * 125));

                        zones.Add(new HitZone { Id = "row_" + p.Id, Bounds = new RectangleF(rowRect.X, rowRect.Y, rowRect.Width - 42f, rowRect.Height), Person = p });
                        zones.Add(new HitZone { Id = "del_" + p.Id, Bounds = delRect, Person = p });
                    }

                    // Thin 3px frosted glass scroll indicator on the right edge when list is scrollable
                    if (hasScroll)
                    {
                        float trackH = Math.Max(20f, bodyAreaH - 8f);
                        float thumbRatio = Math.Max(0.18f, (float)maxRows / filtered.Count);
                        float thumbH = trackH * thumbRatio;
                        float maxOff = Math.Max(1, filtered.Count - maxRows);
                        float thumbY = bodyAreaY + 4f + (trackH - thumbH) * (listScrollOffset / maxOff);
                        RectangleF thumbRect = new RectangleF(rightX + rightW - 3.5f, thumbY, 3f, thumbH);
                        using (GraphicsPath tp = CreateRoundRectPath(thumbRect, 1.5f))
                        using (SolidBrush tbr = new SolidBrush(GetThemeRimColor(115)))
                        {
                            g.FillPath(tbr, tp);
                        }
                    }
                }
            }
            g.Restore(bodySt);

            // ── ANIMATED FLOATING FILTER DROPDOWN POPUP (Spring scale & fade, 2-column 9-mode sorting grid!) ──
            if (filterDropdownProgress > 0.01f)
            {
                string[] optIds = new string[] {
                    "Nearest", "Furthest",
                    "ThisMonth", "Month",
                    "Alpha", "AlphaDesc",
                    "Age", "AgeYoung",
                    "Milestones"
                };
                string[] optLabels = new string[] {
                    "NEAREST FIRST", "FURTHEST FIRST",
                    "THIS MONTH ONLY", "BY MONTH (JAN–DEC)",
                    "NAME A–Z", "NAME Z–A",
                    "AGE (OLDEST)", "AGE (YOUNGEST)",
                    "SPECIAL MILESTONES (10K / 222D)"
                };

                float ddW = Math.Min(308f, rightW);
                float itemH = 26f;
                float itemGap = 4f;
                int rows = 5;
                float ddH = rows * itemH + (rows - 1) * itemGap + 14f;
                float ddX = Math.Max(rightX, Math.Min(rightX + rightW - ddW, filterBtnRect.Right - ddW));
                float ddY = Math.Min(filterBtnRect.Bottom + 6f, y + h - ddH - 2f);
                RectangleF ddRect = new RectangleF(ddX, ddY, ddW, ddH);

                float ddScale = 0.88f + 0.12f * filterDropdownProgress;
                GraphicsState ddSt = g.Save();
                if (Math.Abs(ddScale - 1.0f) > 0.001f)
                {
                    float ax = filterBtnRect.X + filterBtnRect.Width * 0.5f;
                    float ay = ddY;
                    g.TranslateTransform(ax, ay);
                    g.ScaleTransform(ddScale, ddScale);
                    g.TranslateTransform(-ax, -ay);
                }

                using (GraphicsPath dp = CreateRoundRectPath(ddRect, 16f))
                {
                    g.SetClip(dp);
                    g.CompositingMode = CompositingMode.SourceCopy;
                    using (SolidBrush clrBr = new SolidBrush(Color.Transparent))
                    {
                        g.FillRectangle(clrBr, ddRect);
                    }
                    g.CompositingMode = CompositingMode.SourceOver;
                    using (SolidBrush bg = new SolidBrush(GetPopupBackdropFillColor(filterDropdownProgress)))
                    {
                        g.FillPath(bg, dp);
                    }
                    g.ResetClip();
                }
                DrawAnimatedGlassCard(g, ddRect, 16f, true, 0.25f * filterDropdownProgress, t, 0f);

                if (isFilterDropdownOpen)
                {
                    zones.RemoveAll(z => z.Bounds.IntersectsWith(ddRect));
                }

                float colW = (ddW - 14f - 5f) * 0.5f;
                for (int i = 0; i < optIds.Length; i++)
                {
                    string zId = "sort_" + optIds[i];
                    RectangleF ir;
                    if (i == optIds.Length - 1)
                    {
                        ir = new RectangleF(ddX + 7f, ddY + 7f + 4 * (itemH + itemGap), ddW - 14f, itemH);
                    }
                    else
                    {
                        int r = i / 2;
                        int c = i % 2;
                        ir = new RectangleF(ddX + 7f + c * (colW + 5f), ddY + 7f + r * (itemH + itemGap), colW, itemH);
                    }
                    bool act = selectedSortFilter == optIds[i];
                    DrawAnimatedGlassButton(g, ir, optLabels[i], act, GetHoverProgress(zId, dt), t);
                    if (isFilterDropdownOpen)
                    {
                        zones.Add(new HitZone { Id = zId, Bounds = ir });
                    }
                }

                g.Restore(ddSt);
            }
        }

        private void RenderBirthdayCelebrationParticles(Graphics g, RectangleF heroRect, float elapsed, float masterAlpha)
        {
            if (masterAlpha <= 0.01f) return;
            float cx = heroRect.X + heroRect.Width * 0.5f;
            float cy = heroRect.Y + heroRect.Height * 0.36f;

            // 26 festive glass confetti & 4-point star sparkles radiating & gently drifting inside the Hero card!
            g.SetClip(heroRect);
            Color[] palette = new Color[] {
                Color.FromArgb(255, 255, 215, 110),
                Color.FromArgb(255, 255, 150, 195),
                Color.FromArgb(255, 140, 225, 255),
                Color.FromArgb(255, 155, 250, 205),
                Color.FromArgb(255, 215, 185, 255),
                Color.FromArgb(255, 255, 255, 255)
            };

            for (int i = 0; i < 26; i++)
            {
                float seed = i * 1.371f;
                float cycle = (elapsed * (0.42f + (i % 5) * 0.08f) + (i * 0.13f)) % 1.0f;
                float ang = seed * 2.4f + (float)Math.Sin(elapsed * 1.5f + i) * 0.25f;
                float maxR = Math.Max(heroRect.Width, heroRect.Height) * 0.56f;
                float r = 14f + cycle * maxR;
                float px = cx + (float)Math.Cos(ang) * r * 0.85f;
                float py = cy + (float)Math.Sin(ang) * r * 0.75f + cycle * cycle * 48f;

                float lifeFade = (float)Math.Sin(cycle * Math.PI);
                float twinkle = 0.55f + 0.45f * (float)Math.Sin(elapsed * 7.5f + i * 1.9f);
                int a = ClampInt((int)(240f * masterAlpha * lifeFade * twinkle), 0, 255);
                if (a <= 4) continue;

                Color baseCol = palette[i % palette.Length];
                Color col = Color.FromArgb(a, baseCol.R, baseCol.G, baseCol.B);
                float sz = 3.2f + (i % 4) * 1.35f;

                if (i % 2 == 0)
                {
                    // 4-point star sparkle
                    using (Pen sp = new Pen(col, 1.4f))
                    {
                        g.DrawLine(sp, px - sz, py, px + sz, py);
                        g.DrawLine(sp, px, py - sz, px, py + sz);
                    }
                    using (SolidBrush sb = new SolidBrush(Color.FromArgb(ClampInt(a + 30, 0, 255), 255, 255, 255)))
                    {
                        g.FillEllipse(sb, px - 1.5f, py - 1.5f, 3f, 3f);
                    }
                }
                else
                {
                    // Glowing confetti orb
                    using (SolidBrush sb = new SolidBrush(col))
                    {
                        g.FillEllipse(sb, px - sz * 0.5f, py - sz * 0.5f, sz, sz);
                    }
                }
            }
            g.ResetClip();
        }

        private void RenderCalendarView(Graphics g, List<HitZone> zones, List<PersonEntry> allPeople, DateTime now, float x, float y, float w, float h, float dt, float t)
        {
            RectangleF calCard = new RectangleF(x, y, w, h);
            DrawAnimatedGlassCard(g, calCard, 20f, false, 0f, t, 0f);

            bool compactCal = h < 230f;
            bool ultraCompactCal = h < 190f;
            float pad = ultraCompactCal ? 6f : (compactCal ? 8f : 12f);
            float navH = ultraCompactCal ? 18f : (compactCal ? 20f : 24f);
            float navY = y + pad;
            RectangleF prevBtn = new RectangleF(x + pad, navY, 28f, navH);
            RectangleF nextBtn = new RectangleF(x + w - pad - 28f, navY, 28f, navH);
            RectangleF todayBtn = new RectangleF(nextBtn.X - 62f, navY, 56f, navH);

            DrawAnimatedGlassButton(g, prevBtn, "‹", false, GetHoverProgress("cal_prev", dt), t);
            DrawAnimatedGlassButton(g, nextBtn, "›", false, GetHoverProgress("cal_next", dt), t);
            DrawAnimatedGlassButton(g, todayBtn, "TODAY", calYear == now.Year && calMonth == now.Month, GetHoverProgress("cal_today", dt), t);
            zones.Add(new HitZone { Id = "cal_prev", Bounds = prevBtn });
            zones.Add(new HitZone { Id = "cal_next", Bounds = nextBtn });
            zones.Add(new HitZone { Id = "cal_today", Bounds = todayBtn });

            DateTime firstOfMonth = new DateTime(calYear, calMonth, 1);
            Font mf = GetCachedFont(ultraCompactCal ? 11f : (compactCal ? 12f : 13.5f), FontStyle.Bold);
            RectangleF titleR = new RectangleF(prevBtn.Right + 6f, navY, Math.Max(40f, todayBtn.X - prevBtn.Right - 12f), navH);
            DrawAdaptiveString(g, firstOfMonth.ToString("MMMM yyyy").ToUpper(), mf, titleR, sfCenter, 245);

            string[] dow = new string[] { "MO", "TU", "WE", "TH", "FR", "SA", "SU" };
            float gridX = x + pad;
            float gridW = w - pad * 2f;
            float cellW = gridW / 7f;
            float dowY = navY + navH + (ultraCompactCal ? 2f : (compactCal ? 3f : 6f));
            float dowH = ultraCompactCal ? 11f : (compactCal ? 13f : 16f);

            Font df = GetCachedFont(ultraCompactCal ? 8.5f : (compactCal ? 9f : 10f), FontStyle.Bold);
            for (int c = 0; c < 7; c++)
            {
                DrawAdaptiveString(g, dow[c], df, new RectangleF(gridX + c * cellW, dowY, cellW, dowH), sfCenter, 155);
            }

            int daysInMonth = DateTime.DaysInMonth(calYear, calMonth);
            int startCol = ((int)firstOfMonth.DayOfWeek + 6) % 7;
            int numWeekRows = Math.Max(4, (startCol + daysInMonth + 6) / 7);

            // Responsive grid height so rows NEVER overlap the bottom info strip even on 700x450 HeroTop/Bottom with Birthday Banner!
            float stripH = ultraCompactCal ? 26f : (compactCal ? 30f : 40f);
            float gridTop = dowY + dowH + 2f;
            float gridH = Math.Max(54f, (y + h - pad - stripH - (ultraCompactCal ? 3f : 5f)) - gridTop);
            float cellH = gridH / numWeekRows;

            Dictionary<int, List<PersonEntry>> bdayMap = new Dictionary<int, List<PersonEntry>>();
            foreach (PersonEntry p in allPeople)
            {
                if (p.Month == calMonth)
                {
                    int d = Math.Max(1, Math.Min(daysInMonth, p.Day));
                    if (!bdayMap.ContainsKey(d)) bdayMap[d] = new List<PersonEntry>();
                    bdayMap[d].Add(p);
                }
            }

            Font numF = GetCachedFont(ultraCompactCal ? 9.5f : (compactCal ? 10f : 11.5f), FontStyle.Bold);
            Font miniNameF = GetCachedFont(7.5f, FontStyle.Bold);
            for (int day = 1; day <= daysInMonth; day++)
            {
                int idx = startCol + (day - 1);
                int r = idx / 7;
                int c = idx % 7;
                RectangleF cell = new RectangleF(gridX + c * cellW + 2f, gridTop + r * cellH + 1.5f, cellW - 4f, cellH - 3f);

                bool isToday = (calYear == now.Year && calMonth == now.Month && day == now.Day);
                bool isSel = (day == calSelectedDay);
                bool hasBday = bdayMap.ContainsKey(day);
                float hov = GetHoverProgress("cal_day_" + day, dt);

                using (GraphicsPath cp = CreateRoundRectPath(cell, 8f))
                {
                    int fa = (int)((isSel ? 54 : (hasBday ? 34 : (isToday ? 24 : 9))) + hov * 24f);
                    sharedFillBrush.Color = GetCardSurfaceColor(fa);
                    g.FillPath(sharedFillBrush, cp);
                    int rimA = isSel ? 220 : (isToday ? 175 : (hasBday ? 135 : (int)(32 + hov * 65f)));
                    sharedRimPen.Color = GetThemeRimColor(rimA);
                    sharedRimPen.Width = (isSel || isToday) ? 1.35f : 0.9f;
                    g.DrawPath(sharedRimPen, cp);
                }

                bool showMiniName = hasBday && cellH >= 30f;
                bool sideDot = cellH < 20f;
                DrawAdaptiveString(g, day.ToString(), numF, new RectangleF(cell.X, cell.Y, cell.Width, cell.Height - (showMiniName ? 11f : (hasBday && !sideDot ? 5f : 0f))), sfCenter, hasBday || isToday || isSel ? 250 : 185);

                if (hasBday)
                {
                    if (showMiniName)
                    {
                        List<PersonEntry> bpList = bdayMap[day];
                        string tag = bpList.Count > 1
                            ? (bpList[0].Name ?? "").Split(' ')[0].ToUpper() + "+" + (bpList.Count - 1)
                            : (bpList[0].Name ?? "").Split(' ')[0].ToUpper();
                        if (tag.Length > 7) tag = tag.Substring(0, 7);
                        DrawAdaptiveString(g, tag, miniNameF, new RectangleF(cell.X + 2f, cell.Bottom - 13f, cell.Width - 4f, 11f), sfCenter, 225);
                    }
                    else
                    {
                        float dotSz = compactCal ? 3.5f : 5f;
                        sharedFillBrush.Color = GetThemeRimColor(250);
                        if (sideDot)
                            g.FillEllipse(sharedFillBrush, cell.Right - dotSz - 3f, cell.Y + (cell.Height - dotSz) * 0.5f, dotSz, dotSz);
                        else
                            g.FillEllipse(sharedFillBrush, cell.X + (cell.Width - dotSz) * 0.5f, cell.Bottom - dotSz - 2f, dotSz, dotSz);
                    }
                }

                zones.Add(new HitZone { Id = "cal_day_" + day, Bounds = cell });
            }

            // Bottom Selected-Day Info Strip (Quick-Add on empty date + View/Edit & Shared-Day Cycling when 1+ people!)
            RectangleF stripRect = new RectangleF(gridX, y + h - pad - stripH, gridW, stripH);
            DrawAnimatedGlassCard(g, stripRect, 12f, bdayMap.ContainsKey(calSelectedDay), 0f, t, 0f);

            float btnH = Math.Min(26f, stripH - 6f);
            float btnY = stripRect.Y + (stripH - btnH) * 0.5f;

            List<PersonEntry> dayPeople;
            if (bdayMap.TryGetValue(calSelectedDay, out dayPeople) && dayPeople.Count > 0)
            {
                if (calSharedPersonIndex < 0 || calSharedPersonIndex >= dayPeople.Count) calSharedPersonIndex = 0;
                PersonEntry activeP = dayPeople[calSharedPersonIndex];

                float rightCtrlW = 136f;
                RectangleF editBtn = new RectangleF(stripRect.Right - 64f, btnY, 58f, btnH);
                RectangleF viewBtn = new RectangleF(editBtn.X - 64f, btnY, 58f, btnH);
                DrawAnimatedGlassButton(g, viewBtn, "VIEW", false, GetHoverProgress("row_" + activeP.Id, dt), t);
                DrawAnimatedGlassButton(g, editBtn, L("EDIT"), false, GetHoverProgress("btn_hero_edit", dt), t);
                zones.Add(new HitZone { Id = "row_" + activeP.Id, Bounds = viewBtn, Person = activeP });
                zones.Add(new HitZone { Id = "btn_hero_edit", Bounds = editBtn, Person = activeP });

                if (dayPeople.Count > 1)
                {
                    RectangleF nextShared = new RectangleF(viewBtn.X - 28f, btnY, 24f, btnH);
                    RectangleF prevShared = new RectangleF(nextShared.X - 26f, btnY, 24f, btnH);
                    DrawAnimatedGlassButton(g, prevShared, "‹", false, GetHoverProgress("cal_sh_prev", dt), t);
                    DrawAnimatedGlassButton(g, nextShared, "›", false, GetHoverProgress("cal_sh_next", dt), t);
                    zones.Add(new HitZone { Id = "cal_sh_prev", Bounds = prevShared });
                    zones.Add(new HitZone { Id = "cal_sh_next", Bounds = nextShared });
                    rightCtrlW += 56f;
                }

                string labelText = dayPeople.Count > 1
                    ? string.Format("{0:00} {1}:  {2}  ({3}/{4})", calSelectedDay, firstOfMonth.ToString("MMM").ToUpper(), (activeP.Name ?? "").ToUpper(), calSharedPersonIndex + 1, dayPeople.Count)
                    : string.Format("{0:00} {1}:  {2}", calSelectedDay, firstOfMonth.ToString("MMM").ToUpper(), (activeP.Name ?? "").ToUpper());

                Font sf = GetCachedFont(compactCal ? 10.5f : 12f, FontStyle.Bold);
                DrawAdaptiveString(g, labelText, sf, new RectangleF(stripRect.X + 12f, stripRect.Y, Math.Max(40f, stripRect.Width - rightCtrlW - 18f), stripRect.Height), sfNearCenter, 248);
            }
            else
            {
                RectangleF quickAddBtn = new RectangleF(stripRect.Right - 72f, btnY, 66f, btnH);
                DrawAnimatedGlassButton(g, quickAddBtn, "+ ADD", false, GetHoverProgress("cal_quick_add", dt), t);
                zones.Add(new HitZone { Id = "cal_quick_add", Bounds = quickAddBtn });

                Font sf = GetCachedFont(compactCal ? 10f : 11f, FontStyle.Regular);
                DrawAdaptiveString(g, string.Format("{0:00} {1}  •  NO BIRTHDAYS ON THIS DAY", calSelectedDay, firstOfMonth.ToString("MMM").ToUpper()),
                    sf, new RectangleF(stripRect.X + 12f, stripRect.Y, Math.Max(40f, stripRect.Width - 90f), stripRect.Height), sfNearCenter, 155);
            }
        }

        // Responsive In-Glass Add/Edit Person View (Scales cleanly to 700x450, 780x500, and 880x560!)
        private void RenderInGlassPersonEditor(Graphics g, List<HitZone> zones, float x, float y, float w, float h, float dt, float t)
        {
            RectangleF card = new RectangleF(x, y, w, h);
            DrawAnimatedGlassCard(g, card, 24f, false, 0f, t, 0f);

            float padX = x + 24f;
            float innerW = w - 48f;
            bool compact = h < 395f;
            float rowStep = compact ? 38f : 46f;
            float curY = y + (compact ? 16f : 22f);

            Font secFont = GetCachedFont(15f, FontStyle.Bold);
            Font lblFont = GetCachedFont(11f, FontStyle.Regular);
            using (SolidBrush whiteBr = new SolidBrush(GetTextColor(245)))
            using (SolidBrush subBr = new SolidBrush(GetTextColor(155)))
            {
                g.DrawString(isNewPerson ? "ADD PERSON" : "EDIT PERSON", secFont, whiteBr, padX, curY);
                g.DrawString("CLICK ANY FIELD BELOW AND TYPE ON YOUR KEYBOARD", lblFont, subBr, padX + 135f, curY + 2f);
                curY += (compact ? 30f : 36f);

                float avPreviewSpace = 104f;
                float fieldsW = innerW - avPreviewSpace;
                float nameW = fieldsW * 0.65f;
                float yearW = fieldsW - nameW - 14f;

                g.DrawString("NAME", lblFont, subBr, padX, curY);
                g.DrawString("BIRTH YEAR (OPTIONAL)", lblFont, subBr, padX + nameW + 14f, curY);
                curY += 20f;

                RectangleF nameBox = new RectangleF(padX, curY, nameW, 34f);
                DrawInGlassTextBox(g, nameBox, editName, "Type name...", activeTextField == "name", GetHoverProgress("field_name", dt), t);
                zones.Add(new HitZone { Id = "field_name", Bounds = nameBox });

                RectangleF yearBox = new RectangleF(padX + nameW + 14f, curY, yearW, 34f);
                DrawInGlassTextBox(g, yearBox, editYearStr, "e.g. 2000", activeTextField == "year", GetHoverProgress("field_year", dt), t);
                zones.Add(new HitZone { Id = "field_year", Bounds = yearBox });

                int pvY = 0;
                int.TryParse((editYearStr ?? "").Trim(), out pvY);
                PersonEntry previewEntry = new PersonEntry
                {
                    Name = string.IsNullOrWhiteSpace(editName) ? "A" : editName,
                    Day = editDay,
                    Month = editMonth,
                    BirthYear = (pvY >= 1900 && pvY <= DateTime.Now.Year) ? pvY : 0,
                    IconType = editIconType,
                    ImagePath = editImagePath
                };
                RectangleF prevAvRect = new RectangleF(x + w - 104f, curY - 10f, 76f, 76f);
                DrawMinimalAvatar(g, previewEntry, prevAvRect, t);

                // Live turning-age / countdown preview right under the avatar!
                int pvDays = previewEntry.GetDaysUntil(DateTime.Now);
                int pvTurn = previewEntry.GetTurningAge(DateTime.Now);
                string pvHint = pvTurn > 0
                    ? string.Format("TURNS {0} • {1}D", pvTurn, pvDays)
                    : (pvDays == 0 ? "TODAY!" : ("IN " + pvDays + " DAYS"));
                Font pvF = GetCachedFont(9.5f, FontStyle.Bold);
                DrawAdaptiveString(g, pvHint, pvF, new RectangleF(x + w - 118f, prevAvRect.Bottom + 4f, 104f, 16f), sfCenter, 195);

                curY += rowStep;

                g.DrawString("MONTH", lblFont, subBr, padX, curY);
                curY += 20f;
                string[] shortMonths = new string[] { "JAN", "FEB", "MAR", "APR", "MAY", "JUN", "JUL", "AUG", "SEP", "OCT", "NOV", "DEC" };
                float mGap = 5f;
                float mBtnW = Math.Min(42f, (fieldsW - mGap * 11f) / 12f);
                for (int m = 0; m < 12; m++)
                {
                    string mId = "em_" + (m + 1);
                    RectangleF mr = new RectangleF(padX + m * (mBtnW + mGap), curY, mBtnW, 28f);
                    bool act = editMonth == (m + 1);
                    DrawAnimatedGlassButton(g, mr, shortMonths[m], act, GetHoverProgress(mId, dt), t);
                    zones.Add(new HitZone { Id = mId, Bounds = mr });
                }
                curY += (compact ? 34f : 40f);

                g.DrawString("DAY (CLICK TO TYPE)", lblFont, subBr, padX, curY);
                g.DrawString("NOTE / GIFT IDEA (OPTIONAL)", lblFont, subBr, padX + 170f, curY);
                curY += 20f;

                RectangleF dayMinus = new RectangleF(padX, curY, 34f, 32f);
                RectangleF dayDisp  = new RectangleF(padX + 40f, curY, 60f, 32f);
                RectangleF dayPlus  = new RectangleF(padX + 106f, curY, 34f, 32f);
                DrawAnimatedGlassButton(g, dayMinus, "−", false, GetHoverProgress("day_minus", dt), t);
                DrawInGlassTextBox(g, dayDisp, editDayStr, "01", activeTextField == "day", GetHoverProgress("field_day", dt), t);
                DrawAnimatedGlassButton(g, dayPlus, "+", false, GetHoverProgress("day_plus", dt), t);
                zones.Add(new HitZone { Id = "day_minus", Bounds = dayMinus });
                zones.Add(new HitZone { Id = "field_day", Bounds = dayDisp });
                zones.Add(new HitZone { Id = "day_plus", Bounds = dayPlus });

                RectangleF notesBox = new RectangleF(padX + 170f, curY, innerW - 170f, 32f);
                DrawInGlassTextBox(g, notesBox, editNotes, "Optional note...", activeTextField == "notes", GetHoverProgress("field_notes", dt), t);
                zones.Add(new HitZone { Id = "field_notes", Bounds = notesBox });
                curY += rowStep;

                g.DrawString("AVATAR STYLE", lblFont, subBr, padX, curY);
                curY += 20f;

                string[] icTypes = new string[] { "initials", "user", "star", "heart", "gift" };
                string[] icNames = new string[] { "INITIALS", "USER", "STAR", "HEART", "GIFT" };
                float icGap = 6f;
                float uploadBtnW = 135f;
                float icBtnW = Math.Min(86f, (innerW - uploadBtnW - icGap * 5f) / 5f);
                float ix = padX;
                for (int i = 0; i < icTypes.Length; i++)
                {
                    string icId = "eic_" + icTypes[i];
                    RectangleF ir = new RectangleF(ix, curY, icBtnW, 28f);
                    bool act = string.Equals(editIconType, icTypes[i], StringComparison.OrdinalIgnoreCase);
                    DrawAnimatedGlassButton(g, ir, icNames[i], act, GetHoverProgress(icId, dt), t);
                    zones.Add(new HitZone { Id = icId, Bounds = ir });
                    ix += icBtnW + icGap;
                }

                RectangleF upImgRect = new RectangleF(ix + 2f, curY, uploadBtnW, 28f);
                bool hasCustomImg = editIconType == "image" && !string.IsNullOrEmpty(editImagePath);
                DrawAnimatedGlassButton(g, upImgRect, hasCustomImg ? "IMAGE SELECTED" : "CHOOSE IMAGE...", hasCustomImg, GetHoverProgress("btn_pick_img", dt), t);
                zones.Add(new HitZone { Id = "btn_pick_img", Bounds = upImgRect });

                float botY = y + h - 42f;
                RectangleF cancelBtn = new RectangleF(padX, botY, 110f, 30f);
                RectangleF saveBtn   = new RectangleF(x + w - 24f - 148f, botY, 148f, 30f);
                DrawAnimatedGlassButton(g, cancelBtn, L("CANCEL"), false, GetHoverProgress("btn_cancel_edit", dt), t);
                DrawAnimatedGlassButton(g, saveBtn, isNewPerson ? "ADD PERSON" : L("SAVE CHANGES"), true, GetHoverProgress("btn_save_edit", dt), t);
                if (!string.IsNullOrEmpty(editorErrorMsg))
                {
                    RectangleF errRect = new RectangleF(cancelBtn.Right + 10f, botY, Math.Max(40f, saveBtn.X - cancelBtn.Right - 20f), 30f);
                    Color errCol = IsBasicLightMode() ? Color.FromArgb(245, 195, 32, 52) : Color.FromArgb(245, 255, 195, 195);
                    using (SolidBrush errBr = new SolidBrush(errCol))
                    {
                        g.DrawString(editorErrorMsg, lblFont, errBr, errRect, sfCenter);
                    }
                }
                zones.Add(new HitZone { Id = "btn_cancel_edit", Bounds = cancelBtn });
                zones.Add(new HitZone { Id = "btn_save_edit", Bounds = saveBtn });
            }
        }

        // Redesigned In-Glass Settings: Left Category Sidebar + Right Category Content + Live Interactive Preview!
        private void RenderInGlassSettings(Graphics g, List<HitZone> zones, float x, float y, float w, float h, float dt, float t)
        {
            RectangleF card = new RectangleF(x, y, w, h);
            DrawAnimatedGlassCard(g, card, 24f, false, 0f, t, 0f);

            float pad = 18f;
            float sideW = 144f;
            float contentX = x + pad + sideW + 16f;
            float contentW = w - pad * 2f - sideW - 16f;
            float contentTop = y + pad;
            float botBarY = y + h - 44f;
            float contentH = botBarY - contentTop - 10f;

            // Vertical divider between sidebar & content
            using (Pen divPen = new Pen(GetTextColor(32), 1f))
            {
                g.DrawLine(divPen, x + pad + sideW + 8f, y + pad, x + pad + sideW + 8f, botBarY - 8f);
            }

            // ── LEFT SIDEBAR CATEGORIES ──
            Font sideTitleF = GetCachedFont(11f, FontStyle.Bold);
            sharedMainBrush.Color = GetTextColor(145);
            g.DrawString(L("SETTINGS"), sideTitleF, sharedMainBrush, x + pad + 4f, contentTop + 2f);

            SettingsCategory[] cats = new SettingsCategory[] {
                SettingsCategory.General,
                SettingsCategory.Appearance,
                SettingsCategory.Optics,
                SettingsCategory.Layers,
                SettingsCategory.Backup
            };
            string[] catNames = new string[] { L("GENERAL"), L("APPEARANCE"), L("WINDOW"), L("LAYERS"), L("BACKUP") };
            for (int i = 0; i < cats.Length; i++)
            {
                string cid = "set_cat_" + ((int)cats[i]);
                RectangleF cr = new RectangleF(x + pad, contentTop + 26f + i * 38f, sideW, 32f);
                bool active = currentSettingsCat == cats[i];
                DrawAnimatedGlassButton(g, cr, catNames[i], active, GetHoverProgress(cid, dt), t);
                zones.Add(new HitZone { Id = cid, Bounds = cr });
            }

            // ── RIGHT CATEGORY CONTENT (crisp integer-pixel slide transition when switching tabs — zero text-scale morphing!) ──
            float tabEase = EaseOutCubic(settingsTabProgress);
            GraphicsState tabSt = g.Save();
            if (tabEase < 0.999f)
            {
                float slideY = (float)Math.Round((1.0f - tabEase) * 8.0f);
                if (slideY > 0.5f) g.TranslateTransform(0f, slideY);
            }

            Font secFont = GetCachedFont(14.5f, FontStyle.Bold);
            Font lblFont = GetCachedFont(10.5f, FontStyle.Regular);
            using (SolidBrush whiteBr = new SolidBrush(GetTextColor(248)))
            using (SolidBrush subBr = new SolidBrush(GetTextColor(165)))
            {
                if (currentSettingsCat == SettingsCategory.General)
                {
                    float cy = contentTop;
                    g.DrawString(L("GENERAL & BIRTHDAYS"), secFont, whiteBr, contentX, cy);
                    cy += 26f;

                    float halfW = (contentW - 12f) * 0.5f;

                    // ROW 1: Numeric Input for Very Near Highlight Days (left) + Language Dropdown (right)
                    g.DrawString(L("VERY NEAR HIGHLIGHT (DAYS):"), lblFont, subBr, contentX, cy);
                    g.DrawString(L("LANGUAGE") + ":", lblFont, subBr, contentX + halfW + 12f, cy);
                    cy += 18f;

                    // Numeric text field with - / + steppers
                    RectangleF decBtn = new RectangleF(contentX, cy, 30f, 30f);
                    RectangleF numBox = new RectangleF(decBtn.Right + 5f, cy, halfW - 70f, 30f);
                    RectangleF incBtn = new RectangleF(numBox.Right + 5f, cy, 30f, 30f);
                    DrawAnimatedGlassButton(g, decBtn, "−", false, GetHoverProgress("btn_near_days_dec", dt), t);
                    DrawAnimatedGlassButton(g, incBtn, "+", false, GetHoverProgress("btn_near_days_inc", dt), t);
                    using (GraphicsPath np = RoundedRectPath(numBox, IsBasicThemeEnabled() ? 7f : 15f))
                    {
                        Color nFill = GetThemeFillColor(isNearDaysFocused ? 28 : 14);
                        Color nRim = isNearDaysFocused ? GetThemeRimColor(165) : GetThemeRimColor(85);
                        using (SolidBrush nb = new SolidBrush(nFill)) g.FillPath(nb, np);
                        using (Pen npen = new Pen(nRim, isNearDaysFocused ? 1.5f : 1.0f)) g.DrawPath(npen, np);
                    }
                    string numDisp = isNearDaysFocused
                        ? (stagedNearDaysText + "|")
                        : ((string.IsNullOrEmpty(stagedNearDaysText) ? stagedUrgentDays.ToString() : stagedNearDaysText) + " D");
                    DrawAdaptiveString(g, numDisp, GetCachedFont(11.5f, FontStyle.Bold), numBox, sfCenter, 248);
                    zones.Add(new HitZone { Id = "btn_near_days_dec", Bounds = decBtn });
                    zones.Add(new HitZone { Id = "btn_near_days_input", Bounds = numBox });
                    zones.Add(new HitZone { Id = "btn_near_days_inc", Bounds = incBtn });

                    // Language Dropdown Button
                    RectangleF langBtn = new RectangleF(contentX + halfW + 12f, cy, halfW, 30f);
                    string langLabel = GetLanguageDisplayName(stagedLanguage);
                    DrawAnimatedGlassButton(g, langBtn, langLabel + (isLangDropdownOpen ? "  ▲" : "  ▼"), isLangDropdownOpen, GetHoverProgress("btn_lang_dd", dt), t);
                    zones.Add(new HitZone { Id = "btn_lang_dd", Bounds = langBtn });
                    cy += 38f;

                    // ROW 2: Toggle Switches (Show Age Badge + Auto-Start With PC)
                    g.DrawString(L("DISPLAY & STARTUP") + ":", lblFont, subBr, contentX, cy);
                    cy += 17f;
                    RectangleF ageToggle = new RectangleF(contentX, cy, halfW, 30f);
                    DrawGlassToggleSwitch(g, ageToggle, L("SHOW AGE BADGE"), stagedShowAge, GetHoverProgress("set_toggle_age", dt));
                    zones.Add(new HitZone { Id = "set_toggle_age", Bounds = ageToggle });

                    RectangleF startupToggle = new RectangleF(contentX + halfW + 12f, cy, halfW, 30f);
                    DrawGlassToggleSwitch(g, startupToggle, L("AUTO-START WITH PC"), stagedAutoStart, GetHoverProgress("set_toggle_autostart", dt));
                    zones.Add(new HitZone { Id = "set_toggle_autostart", Bounds = startupToggle });
                    cy += 36f;

                    // ROW 3: Toggle Switches (Birthday Animation + Birthday Message)
                    RectangleF bdayAnimToggle = new RectangleF(contentX, cy, halfW, 30f);
                    DrawGlassToggleSwitch(g, bdayAnimToggle, L("BIRTHDAY ANIMATION"), stagedBdayAnim, GetHoverProgress("set_toggle_bdayanim", dt));
                    zones.Add(new HitZone { Id = "set_toggle_bdayanim", Bounds = bdayAnimToggle });

                    RectangleF bdayMsgToggle = new RectangleF(contentX + halfW + 12f, cy, halfW, 30f);
                    DrawGlassToggleSwitch(g, bdayMsgToggle, L("BIRTHDAY MESSAGE"), stagedBdayMsg, GetHoverProgress("set_toggle_bdaymsg", dt));
                    zones.Add(new HitZone { Id = "set_toggle_bdaymsg", Bounds = bdayMsgToggle });
                    cy += 38f;

                    // ROW 4: Dedicated SOUND section (left) & CURSOR section (right)
                    g.DrawString("CLICK SOUND:", lblFont, subBr, contentX, cy);
                    g.DrawString("CURSOR STYLE:", lblFont, subBr, contentX + halfW + 12f, cy);
                    cy += 17f;
                    float sndToggleW = halfW * 0.58f;
                    float wavBtnW = halfW - sndToggleW - 6f;
                    RectangleF clickSndToggle = new RectangleF(contentX, cy, sndToggleW, 28f);
                    DrawGlassToggleSwitch(g, clickSndToggle, "POP", stagedClickSounds, GetHoverProgress("set_toggle_clicksound", dt));
                    zones.Add(new HitZone { Id = "set_toggle_clicksound", Bounds = clickSndToggle });

                    RectangleF pickSndBtn = new RectangleF(clickSndToggle.Right + 6f, cy, wavBtnW, 28f);
                    string sndLbl = string.IsNullOrEmpty(stagedCustomSoundPath) ? ".WAV" : "✓ .WAV";
                    DrawAnimatedGlassButton(g, pickSndBtn, sndLbl, !string.IsNullOrEmpty(stagedCustomSoundPath), GetHoverProgress("set_pick_sound", dt), t);
                    zones.Add(new HitZone { Id = "set_pick_sound", Bounds = pickSndBtn });

                    float curBtnW = (halfW - 8f) / 3f;
                    RectangleF curGlassBtn = new RectangleF(contentX + halfW + 12f, cy, curBtnW, 28f);
                    RectangleF curSysBtn   = new RectangleF(curGlassBtn.Right + 4f, cy, curBtnW, 28f);
                    RectangleF curCustBtn  = new RectangleF(curSysBtn.Right + 4f, cy, curBtnW, 28f);
                    DrawAnimatedGlassButton(g, curGlassBtn, "GLASS", stagedCursorStyle == "Glass", GetHoverProgress("set_cur_glass", dt), t);
                    DrawAnimatedGlassButton(g, curSysBtn,   "SYSTEM", stagedCursorStyle == "System", GetHoverProgress("set_cur_sys", dt), t);
                    DrawAnimatedGlassButton(g, curCustBtn,  "CUSTOM", stagedCursorStyle == "Custom", GetHoverProgress("set_pick_cursor", dt), t);
                    zones.Add(new HitZone { Id = "set_cur_glass", Bounds = curGlassBtn });
                    zones.Add(new HitZone { Id = "set_cur_sys", Bounds = curSysBtn });
                    zones.Add(new HitZone { Id = "set_pick_cursor", Bounds = curCustBtn });
                    cy += 36f;

                    // ROW 5: Replay Tutorial + Reset Everything (opens confirmation modal like Delete Person!)
                    RectangleF tutBtn = new RectangleF(contentX, cy, halfW, 28f);
                    RectangleF rstBtn = new RectangleF(contentX + halfW + 12f, cy, halfW, 28f);
                    DrawAnimatedGlassButton(g, tutBtn, "REPLAY TUTORIAL", false, GetHoverProgress("set_replay_tutorial", dt), t);
                    DrawAnimatedGlassButton(g, rstBtn, "RESET EVERYTHING", false, GetHoverProgress("set_reset_all", dt), t);
                    zones.Add(new HitZone { Id = "set_replay_tutorial", Bounds = tutBtn });
                    zones.Add(new HitZone { Id = "set_reset_all", Bounds = rstBtn });

                    // Floating Theme-Matched Language Dropdown Menu with Live Search Bar & Mouse-Wheel Scrolling!
                    if (isLangDropdownOpen)
                    {
                        List<int> matchIdx = new List<int>();
                        string lq = (langSearchQuery ?? "").Trim();
                        for (int li = 0; li < AllLanguages.GetLength(0); li++)
                        {
                            if (lq.Length == 0 || AllLanguages[li, 1].IndexOf(lq, StringComparison.OrdinalIgnoreCase) >= 0 || AllLanguages[li, 0].IndexOf(lq, StringComparison.OrdinalIgnoreCase) >= 0)
                                matchIdx.Add(li);
                        }
                        int maxVis = 6;
                        int maxScroll = Math.Max(0, matchIdx.Count - maxVis);
                        if (langScrollOffset > maxScroll) langScrollOffset = maxScroll;
                        if (langScrollOffset < 0) langScrollOffset = 0;
                        int showCnt = Math.Min(maxVis, matchIdx.Count - langScrollOffset);
                        float ddH = 34f + Math.Max(1, showCnt) * 28f + 10f;
                        RectangleF ddMenu = new RectangleF(langBtn.X, langBtn.Bottom + 4f, langBtn.Width, ddH);
                        using (GraphicsPath dp = RoundedRectPath(ddMenu, IsBasicThemeEnabled() ? 8f : 14f))
                        {
                            CompositingMode prevComp = g.CompositingMode;
                            g.CompositingMode = CompositingMode.SourceCopy;
                            using (SolidBrush clrBr = new SolidBrush(Color.FromArgb(0, 0, 0, 0))) g.FillPath(clrBr, dp);
                            g.CompositingMode = prevComp;
                        }
                        DrawAnimatedGlassCard(g, ddMenu, IsBasicThemeEnabled() ? 8f : 14f, false, 0.35f, t, 0f);
                        RectangleF lSearchR = new RectangleF(ddMenu.X + 6f, ddMenu.Y + 6f, ddMenu.Width - 12f, 24f);
                        DrawInGlassSearchBox(g, lSearchR, langSearchQuery, isLangSearchFocused, GetHoverProgress("btn_lang_search", dt), t, "SEARCH LANGUAGE...");
                        zones.Add(new HitZone { Id = "btn_lang_search", Bounds = lSearchR });

                        for (int mi = 0; mi < showCnt; mi++)
                        {
                            int idxL = matchIdx[langScrollOffset + mi];
                            string lCode = AllLanguages[idxL, 0];
                            string lName = AllLanguages[idxL, 1];
                            string lid = "set_lang_" + lCode;
                            RectangleF itemR = new RectangleF(ddMenu.X + 6f, lSearchR.Bottom + 5f + mi * 28f, ddMenu.Width - 12f, 24f);
                            DrawAnimatedGlassButton(g, itemR, lName, stagedLanguage == lCode, GetHoverProgress(lid, dt), t);
                            zones.Add(new HitZone { Id = lid, Bounds = itemR });
                        }
                    }

                    // Centered Reset Everything Confirmation Modal (same style as Delete Person!)
                    if (showResetConfirmModal)
                    {
                        zones.Clear();
                        using (SolidBrush dimBr = new SolidBrush(Color.FromArgb(145, 10, 12, 20)))
                            g.FillRectangle(dimBr, x, y, w, h);
                        float mw = 340f, mh = 148f;
                        RectangleF mr = new RectangleF(x + (w - mw) * 0.5f, y + (h - mh) * 0.5f, mw, mh);
                        using (GraphicsPath mp = RoundedRectPath(mr, IsBasicThemeEnabled() ? 10f : 20f))
                        {
                            Color rimC = GetThemeRimColor(255);
                            int bR = IsDarkModeEnabled() ? 24 : 230, bG = IsDarkModeEnabled() ? 26 : 234, bB = IsDarkModeEnabled() ? 36 : 244;
                            Color mBg = Color.FromArgb(242, (bR * 84 + rimC.R * 16) / 100, (bG * 84 + rimC.G * 16) / 100, (bB * 84 + rimC.B * 16) / 100);
                            using (SolidBrush mb = new SolidBrush(mBg)) g.FillPath(mb, mp);
                            using (Pen mpen = new Pen(GetThemeRimColor(165), 1.3f)) g.DrawPath(mpen, mp);
                        }
                        DrawAdaptiveString(g, "RESET EVERYTHING?", GetCachedFont(14f, FontStyle.Bold), new RectangleF(mr.X + 16f, mr.Y + 18f, mr.Width - 32f, 26f), sfCenter, 250);
                        DrawAdaptiveString(g, "All people & settings will be reset to default.", GetCachedFont(10.5f, FontStyle.Regular), new RectangleF(mr.X + 16f, mr.Y + 48f, mr.Width - 32f, 36f), sfCenter, 190);
                        float mbW = (mr.Width - 42f) * 0.5f;
                        RectangleF mCancel = new RectangleF(mr.X + 16f, mr.Bottom - 44f, mbW, 30f);
                        RectangleF mConfirm = new RectangleF(mCancel.Right + 10f, mr.Bottom - 44f, mbW, 30f);
                        DrawAnimatedGlassButton(g, mCancel, L("CANCEL"), false, GetHoverProgress("btn_reset_cancel", dt), t);
                        DrawAnimatedGlassButton(g, mConfirm, "RESET", true, GetHoverProgress("btn_reset_confirm", dt), t);
                        zones.Add(new HitZone { Id = "btn_reset_cancel", Bounds = mCancel });
                        zones.Add(new HitZone { Id = "btn_reset_confirm", Bounds = mConfirm });
                    }
                }
                else if (currentSettingsCat == SettingsCategory.Appearance)
                {
                    float leftW = contentW * 0.55f;
                    float prevX = contentX + leftW + 14f;
                    float prevW = contentW - leftW - 14f;
                    float cy = contentTop;

                    g.DrawString("APPEARANCE & THEME", secFont, whiteBr, contentX, cy);
                    cy += 22f;

                    // PROGRAM THEME (LIQUID GLASS vs BASIC PROGRAM THEME — automatically pairs with Melon vs Segoe font!)
                    g.DrawString("PROGRAM THEME (AUTO FONT):", lblFont, subBr, contentX, cy);
                    cy += 16f;
                    float ptHalf = (leftW - 6f) * 0.5f;
                    RectangleF glassThemeBtn = new RectangleF(contentX, cy, ptHalf, 26f);
                    RectangleF basicThemeBtn = new RectangleF(contentX + ptHalf + 6f, cy, ptHalf, 26f);
                    DrawAnimatedGlassButton(g, glassThemeBtn, "LIQUID GLASS", !stagedBasicTheme, GetHoverProgress("app_theme_glass", dt), t);
                    DrawAnimatedGlassButton(g, basicThemeBtn, "BASIC THEME", stagedBasicTheme, GetHoverProgress("app_theme_basic", dt), t);
                    zones.Add(new HitZone { Id = "app_theme_glass", Bounds = glassThemeBtn });
                    zones.Add(new HitZone { Id = "app_theme_basic", Bounds = basicThemeBtn });
                    cy += 32f;

                    // TINTED COLOR VARIANTS
                    g.DrawString(stagedBasicTheme ? "ACCENT TINT VARIANT:" : "TINTED GLASS VARIANT:", lblFont, subBr, contentX, cy);
                    cy += 16f;
                    string[] tints = new string[] { "Clear", "Ice", "Rose", "Mint", "Amber", "Violet", "Obsidian" };
                    float tGap = 5f;
                    int tPerRow = 4;
                    float tBtnW = (leftW - tGap * (tPerRow - 1)) / tPerRow;
                    for (int i = 0; i < tints.Length; i++)
                    {
                        int r = i / tPerRow;
                        int c = i % tPerRow;
                        string tid = "app_tint_" + tints[i];
                        RectangleF tr = new RectangleF(contentX + c * (tBtnW + tGap), cy + r * 26f, tBtnW, 23f);
                        DrawAnimatedGlassButton(g, tr, tints[i].ToUpper(), stagedTint == tints[i], GetHoverProgress(tid, dt), t);
                        zones.Add(new HitZone { Id = tid, Bounds = tr });
                    }
                    cy += 56f;

                    // FONT FAMILY
                    g.DrawString("FONT FAMILY:", lblFont, subBr, contentX, cy);
                    cy += 16f;
                    string[] fonts = new string[] { "Melon Squishy", "Segoe UI", "Bahnschrift", "Consolas", "Georgia" };
                    string[] fontLabels = new string[] { "MELON", "SEGOE", "BAHN", "MONO", "SERIF" };
                    float fGap = 5f;
                    float fBtnW = (leftW - fGap * 2f) / 3f;
                    for (int i = 0; i < fonts.Length; i++)
                    {
                        int r = i / 3;
                        int c = i % 3;
                        string fid = "app_font_" + fonts[i];
                        RectangleF fr = new RectangleF(contentX + c * (fBtnW + fGap), cy + r * 26f, fBtnW, 23f);
                        DrawAnimatedGlassButton(g, fr, fontLabels[i], stagedFont == fonts[i], GetHoverProgress(fid, dt), t);
                        zones.Add(new HitZone { Id = fid, Bounds = fr });
                    }
                    cy += 56f;

                    // GLASS / BASIC CONTRAST (LIGHT vs DARK)
                    g.DrawString("SURFACE MODE (LIGHT / DARK):", lblFont, subBr, contentX, cy);
                    cy += 16f;
                    float tmHalf = (leftW - 6f) * 0.5f;
                    RectangleF lightBtn = new RectangleF(contentX, cy, tmHalf, 26f);
                    RectangleF darkBtn  = new RectangleF(contentX + tmHalf + 6f, cy, tmHalf, 26f);
                    DrawAnimatedGlassButton(g, lightBtn, stagedBasicTheme ? "LIGHT BASIC" : "LIGHT GLASS", !stagedDarkMode, GetHoverProgress("app_mode_light", dt), t);
                    DrawAnimatedGlassButton(g, darkBtn,  stagedBasicTheme ? "DARK BASIC" : "DARK GLASS", stagedDarkMode, GetHoverProgress("app_mode_dark", dt), t);
                    zones.Add(new HitZone { Id = "app_mode_light", Bounds = lightBtn });
                    zones.Add(new HitZone { Id = "app_mode_dark", Bounds = darkBtn });

                    // ── LIVE MINI-PREVIEW & DRAGGABLE LAYOUT EDITOR ──
                    g.DrawString("LIVE PREVIEW (DRAG HERO)", lblFont, subBr, prevX, contentTop + 4f);
                    float prevBoxH = Math.Min(172f, contentH - 64f);
                    RectangleF prevBox = new RectangleF(prevX, contentTop + 24f, prevW, prevBoxH);
                    lastPreviewBoxRect = prevBox;

                    // Draw live miniature widget preview reflecting staged tint, font, text color & layout!
                    RenderInteractiveLayoutMiniPreview(g, zones, prevBox, dt, t);

                    // Quick Layout Snap Buttons below mini-preview
                    float lbY = prevBox.Bottom + 8f;
                    string[] layIds = new string[] { "HeroLeft", "HeroRight", "HeroTop", "HeroBottom" };
                    string[] layLbls = new string[] { "LEFT", "RIGHT", "TOP", "BOT" };
                    float lGap = 4f;
                    float lW = (prevW - lGap * 3f) / 4f;
                    for (int i = 0; i < layIds.Length; i++)
                    {
                        string lid = "app_lay_" + layIds[i];
                        RectangleF lr = new RectangleF(prevX + i * (lW + lGap), lbY, lW, 24f);
                        DrawAnimatedGlassButton(g, lr, layLbls[i], stagedLayout == layIds[i], GetHoverProgress(lid, dt), t);
                        zones.Add(new HitZone { Id = lid, Bounds = lr });
                    }
                }
                else if (currentSettingsCat == SettingsCategory.Layers)
                {
                    RefreshRunningProcesses(false);
                    float cy = contentTop;
                    g.DrawString("WINDOW LAYER SYSTEM", secFont, whiteBr, contentX, cy);
                    cy += 24f;

                    // ABOVE vs BELOW mode switch
                    float halfW = (contentW - 8f) * 0.5f;
                    RectangleF modeAbove = new RectangleF(contentX, cy, halfW, 28f);
                    RectangleF modeBelow = new RectangleF(contentX + halfW + 8f, cy, halfW, 28f);
                    DrawAnimatedGlassButton(g, modeAbove, "STAY ABOVE SELECTED", stagedLayerMode == "Above", GetHoverProgress("lay_mode_above", dt), t);
                    DrawAnimatedGlassButton(g, modeBelow, "STAY BELOW SELECTED", stagedLayerMode == "Below", GetHoverProgress("lay_mode_below", dt), t);
                    zones.Add(new HitZone { Id = "lay_mode_above", Bounds = modeAbove });
                    zones.Add(new HitZone { Id = "lay_mode_below", Bounds = modeBelow });
                    cy += 34f;

                    // QUICK LAYER TARGETS (NO DESKTOP ICONS, NO ALWAYS ON TOP)
                    g.DrawString("QUICK TARGETS:", lblFont, subBr, contentX, cy);
                    cy += 16f;
                    float bGap = 8f;
                    float bW = (contentW - bGap) * 0.5f;
                    RectangleF bRain = new RectangleF(contentX, cy, bW, 26f);
                    RectangleF bSpot = new RectangleF(contentX + bW + bGap, cy, bW, 26f);
                    bool hasSpotify = stagedLayerTargets.Any(tExe => tExe.Equals("Spotify.exe", StringComparison.OrdinalIgnoreCase));
                    DrawAnimatedGlassButton(g, bRain, (stagedLayerRainmeter ? "✓ " : "") + "RAINMETER", stagedLayerRainmeter, GetHoverProgress("lay_basic_rain", dt), t);
                    DrawAnimatedGlassButton(g, bSpot, (hasSpotify ? "✓ " : "") + "SPOTIFY", hasSpotify, GetHoverProgress("lay_exe_Spotify.exe", dt), t);
                    zones.Add(new HitZone { Id = "lay_basic_rain", Bounds = bRain });
                    zones.Add(new HitZone { Id = "lay_exe_Spotify.exe", Bounds = bSpot });
                    cy += 34f;

                    // SEARCH RUNNING APPS / BROWSE .EXE
                    g.DrawString("OPEN WINDOWS / .EXE (CLICK TO ADD):", lblFont, subBr, contentX, cy);
                    cy += 16f;
                    float browseW = 114f;
                    RectangleF lSearchRect = new RectangleF(contentX, cy, contentW - browseW - 8f, 26f);
                    RectangleF browseBtn = new RectangleF(lSearchRect.Right + 8f, cy, browseW, 26f);
                    DrawInGlassSearchBox(g, lSearchRect, layerSearchQuery, isLayerSearchFocused, GetHoverProgress("lay_search", dt), t, "SEARCH .EXE...");
                    DrawAnimatedGlassButton(g, browseBtn, "+ BROWSE .EXE", false, GetHoverProgress("lay_browse_exe", dt), t);
                    zones.Add(new HitZone { Id = "lay_search", Bounds = lSearchRect });
                    zones.Add(new HitZone { Id = "lay_browse_exe", Bounds = browseBtn });
                    cy += 32f;

                    // Filter running exes
                    List<string> visibleExes = cachedRunningExes.Where(exe =>
                        string.IsNullOrWhiteSpace(layerSearchQuery) ||
                        exe.IndexOf(layerSearchQuery.Trim(), StringComparison.OrdinalIgnoreCase) >= 0
                    ).ToList();

                    float listAvailH = Math.Max(48f, (botBarY - 8f) - cy);
                    float itemH = 26f;
                    float itemGap = 4f;
                    int cols = 2;
                    int maxRows = Math.Max(1, (int)((listAvailH + itemGap) / (itemH + itemGap)));
                    int perPage = maxRows * cols;
                    if (layerListScroll > Math.Max(0, visibleExes.Count - perPage))
                        layerListScroll = Math.Max(0, visibleExes.Count - perPage);

                    float colW = (contentW - 8f) * 0.5f;
                    int endIdx = Math.Min(visibleExes.Count, layerListScroll + perPage);
                    for (int i = layerListScroll; i < endIdx; i++)
                    {
                        string exe = visibleExes[i];
                        int rel = i - layerListScroll;
                        int r = rel / cols;
                        int c = rel % cols;
                        bool selected = stagedLayerTargets.Any(tExe => tExe.Equals(exe, StringComparison.OrdinalIgnoreCase));
                        string eid = "lay_exe_" + exe;
                        RectangleF er = new RectangleF(contentX + c * (colW + 8f), cy + r * (itemH + itemGap), colW, itemH);
                        DrawAnimatedGlassButton(g, er, (selected ? "✓ " : "+ ") + exe.ToUpper(), selected, GetHoverProgress(eid, dt), t);
                        zones.Add(new HitZone { Id = eid, Bounds = er });
                    }
                }
                else if (currentSettingsCat == SettingsCategory.Optics)
                {
                    float cy = contentTop;
                    g.DrawString(L("WINDOW & PERFORMANCE"), secFont, whiteBr, contentX, cy);
                    cy += 24f;

                    float halfW = (contentW - 10f) * 0.5f;
                    RectangleF winModeSwitch = new RectangleF(contentX, cy, halfW, 32f);
                    DrawGlassToggleSwitch(g, winModeSwitch, L("REAL WINDOW MODE (TASKBAR)"), stagedAppWindowMode, GetHoverProgress("set_toggle_winmode", dt));
                    zones.Add(new HitZone { Id = "set_toggle_winmode", Bounds = winModeSwitch });

                    RectangleF dragRefSwitch = new RectangleF(contentX + halfW + 10f, cy, halfW, 32f);
                    DrawGlassToggleSwitch(g, dragRefSwitch, L("LIVE BACKGROUND ON DRAG"), stagedRefreshWhileDragging, GetHoverProgress("set_toggle_dragref", dt));
                    zones.Add(new HitZone { Id = "set_toggle_dragref", Bounds = dragRefSwitch });
                    cy += 42f;

                    // WINDOW SIZE: Toggle between Standard Presets & Custom Size (mutually greyed out!)
                    RectangleF customSzToggle = new RectangleF(contentX, cy, contentW * 0.44f, 26f);
                    DrawGlassToggleSwitch(g, customSzToggle, stagedUseCustomSize ? "SIZE: CUSTOM" : "SIZE: STANDARD", stagedUseCustomSize, GetHoverProgress("set_toggle_customsz", dt));
                    zones.Add(new HitZone { Id = "set_toggle_customsz", Bounds = customSzToggle });

                    // Custom W × H numeric inputs right beside the toggle (greyed out when Standard!)
                    float remX = customSzToggle.Right + 10f;
                    float remW = (contentX + contentW) - remX;
                    float boxW = (remW - 14f) * 0.5f;
                    RectangleF cwRect = new RectangleF(remX, cy, boxW, 26f);
                    RectangleF chRect = new RectangleF(cwRect.Right + 14f, cy, boxW, 26f);
                    if (stagedUseCustomSize)
                    {
                        using (GraphicsPath wp = RoundedRectPath(cwRect, IsBasicThemeEnabled() ? 7f : 13f))
                        {
                            bool focW = customSizeFocus == "w";
                            using (SolidBrush br = new SolidBrush(GetThemeFillColor(focW ? 28 : 16))) g.FillPath(br, wp);
                            using (Pen p = new Pen(GetThemeRimColor(focW ? 165 : 105), focW ? 1.4f : 1.0f)) g.DrawPath(p, wp);
                        }
                        using (GraphicsPath hp = RoundedRectPath(chRect, IsBasicThemeEnabled() ? 7f : 13f))
                        {
                            bool focH = customSizeFocus == "h";
                            using (SolidBrush br = new SolidBrush(GetThemeFillColor(focH ? 28 : 16))) g.FillPath(br, hp);
                            using (Pen p = new Pen(GetThemeRimColor(focH ? 165 : 105), focH ? 1.4f : 1.0f)) g.DrawPath(p, hp);
                        }
                        DrawAdaptiveString(g, "W " + customWText + (customSizeFocus == "w" ? "|" : ""), GetCachedFont(9.5f, FontStyle.Bold), cwRect, sfCenter, 240);
                        DrawAdaptiveString(g, "×", GetCachedFont(10f, FontStyle.Bold), new RectangleF(cwRect.Right, cy, 14f, 26f), sfCenter, 165);
                        DrawAdaptiveString(g, "H " + customHText + (customSizeFocus == "h" ? "|" : ""), GetCachedFont(9.5f, FontStyle.Bold), chRect, sfCenter, 240);
                        zones.Add(new HitZone { Id = "set_sz_custom_w", Bounds = cwRect });
                        zones.Add(new HitZone { Id = "set_sz_custom_h", Bounds = chRect });
                    }
                    else
                    {
                        DrawDisabledGlassButton(g, cwRect, "W " + customWText);
                        DrawAdaptiveString(g, "×", GetCachedFont(10f, FontStyle.Bold), new RectangleF(cwRect.Right, cy, 14f, 26f), sfCenter, 95);
                        DrawDisabledGlassButton(g, chRect, "H " + customHText);
                    }
                    cy += 32f;

                    float szGap = 8f;
                    float presetW = (contentW - szGap * 2f) / 3f;
                    RectangleF sz1 = new RectangleF(contentX, cy, presetW, 25f);
                    RectangleF sz2 = new RectangleF(sz1.Right + szGap, cy, presetW, 25f);
                    RectangleF sz3 = new RectangleF(sz2.Right + szGap, cy, presetW, 25f);
                    if (!stagedUseCustomSize)
                    {
                        DrawAnimatedGlassButton(g, sz1, "700 × 450", stagedWidth == 700 && stagedHeight == 450, GetHoverProgress("set_sz_1", dt), t);
                        DrawAnimatedGlassButton(g, sz2, "780 × 500", stagedWidth == 780 && stagedHeight == 500, GetHoverProgress("set_sz_2", dt), t);
                        DrawAnimatedGlassButton(g, sz3, "880 × 560", stagedWidth == 880 && stagedHeight == 560, GetHoverProgress("set_sz_3", dt), t);
                        zones.Add(new HitZone { Id = "set_sz_1", Bounds = sz1 });
                        zones.Add(new HitZone { Id = "set_sz_2", Bounds = sz2 });
                        zones.Add(new HitZone { Id = "set_sz_3", Bounds = sz3 });
                    }
                    else
                    {
                        DrawDisabledGlassButton(g, sz1, "700 × 450");
                        DrawDisabledGlassButton(g, sz2, "780 × 500");
                        DrawDisabledGlassButton(g, sz3, "880 × 560");
                    }
                    cy += 33f;

                    g.DrawString(string.Format("REFRESH RATE (MONITOR: {0} HZ):", detectedMonitorHz), lblFont, subBr, contentX, cy);
                    cy += 17f;
                    int[] hzOpts = new int[] { 0, 60, 120, 144, 240 };
                    string[] hzLabels = new string[] { "AUTO (" + detectedMonitorHz + ")", "60 HZ", "120 HZ", "144 HZ", "240 HZ" };
                    float hzGap = 5f;
                    float hzBtnW = (contentW - hzGap * 4f) / 5f;
                    for (int i = 0; i < hzOpts.Length; i++)
                    {
                        string hzId = "set_hz_" + hzOpts[i];
                        RectangleF hr = new RectangleF(contentX + i * (hzBtnW + hzGap), cy, hzBtnW, 26f);
                        DrawAnimatedGlassButton(g, hr, hzLabels[i], stagedRefreshRate == hzOpts[i], GetHoverProgress(hzId, dt), t);
                        zones.Add(new HitZone { Id = hzId, Bounds = hr });
                    }
                    cy += 36f;

                    g.DrawString("LIQUID GLASS OPTICS:", lblFont, subBr, contentX, cy);
                    cy += 17f;
                    string[] presetNames = new string[] { "BALANCED", "CRYSTAL LENS", "SOFT FROST" };
                    float pGap = 8f;
                    float pBtnW = (contentW - pGap * 2f) / 3f;
                    for (int pIdx = 0; pIdx < presetNames.Length; pIdx++)
                    {
                        string pId = "set_preset_" + pIdx;
                        RectangleF pr = new RectangleF(contentX + pIdx * (pBtnW + pGap), cy, pBtnW, 26f);
                        DrawAnimatedGlassButton(g, pr, presetNames[pIdx], stagedPresetIndex == pIdx, GetHoverProgress(pId, dt), t);
                        zones.Add(new HitZone { Id = pId, Bounds = pr });
                    }
                }
                else if (currentSettingsCat == SettingsCategory.Backup)
                {
                    float cy = contentTop;
                    g.DrawString("BACKUP & RESTORE (SINGLE FILE)", secFont, whiteBr, contentX, cy);
                    cy += 26f;

                    int totalPeople = config != null && config.People != null ? config.People.Count : 0;
                    int totalImgs = config != null && config.People != null
                        ? config.People.Count(p => p.IconType == "image" && !string.IsNullOrEmpty(p.ImagePath) && File.Exists(p.ImagePath))
                        : 0;

                    g.DrawString(string.Format("CURRENT DATA:  {0} PEOPLE  •  {1} EMBEDDED IMAGES", totalPeople, totalImgs), lblFont, subBr, contentX, cy);
                    cy += 20f;
                    g.DrawString("SAVES ALL PEOPLE, BIRTHDAYS, SETTINGS & AVATAR PHOTOS INTO ONE .BIRTHLY FILE.", lblFont, subBr, new RectangleF(contentX, cy, contentW, 32f));
                    cy += 34f;

                    RectangleF expBtn = new RectangleF(contentX, cy, contentW, 38f);
                    DrawAnimatedGlassButton(g, expBtn, "EXPORT ALL DATA + IMAGES (.BIRTHLY)", true, GetHoverProgress("btn_export_backup", dt), t);
                    zones.Add(new HitZone { Id = "btn_export_backup", Bounds = expBtn });
                    cy += 48f;

                    RectangleF impBtn = new RectangleF(contentX, cy, contentW, 38f);
                    DrawAnimatedGlassButton(g, impBtn, "IMPORT BACKUP FILE (.BIRTHLY)", false, GetHoverProgress("btn_import_backup", dt), t);
                    zones.Add(new HitZone { Id = "btn_import_backup", Bounds = impBtn });
                    cy += 50f;

                    if (!string.IsNullOrEmpty(backupStatusMsg))
                    {
                        RectangleF stBox = new RectangleF(contentX, cy, contentW, 32f);
                        DrawAnimatedGlassCard(g, stBox, 12f, true, 0f, t, 0f);
                        DrawAdaptiveString(g, backupStatusMsg, lblFont, stBox, sfCenter, 240);
                    }
                }
            }
            g.Restore(tabSt);

            // Bottom Cancel & Save Changes Bar
            RectangleF cancelSetBtn = new RectangleF(x + pad, botBarY, 118f, 30f);
            RectangleF saveSetBtn   = new RectangleF(x + w - pad - 148f, botBarY, 148f, 30f);
            DrawAnimatedGlassButton(g, cancelSetBtn, L("CANCEL"), false, GetHoverProgress("btn_cancel_settings", dt), t);
            DrawAnimatedGlassButton(g, saveSetBtn, L("SAVE CHANGES"), true, GetHoverProgress("btn_save_settings", dt), t);
            zones.Add(new HitZone { Id = "btn_cancel_settings", Bounds = cancelSetBtn });
            zones.Add(new HitZone { Id = "btn_save_settings", Bounds = saveSetBtn });
        }

        private void RenderInteractiveLayoutMiniPreview(Graphics g, List<HitZone> zones, RectangleF box, float dt, float t)
        {
            // Tinted mini-glass frame
            using (GraphicsPath bp = CreateRoundRectPath(box, 14f))
            {
                Color bgCol = GetCardSurfaceColor(36);
                using (SolidBrush br = new SolidBrush(bgCol)) g.FillPath(br, bp);
                using (Pen pen = new Pen(GetThemeRimColor(135), 1.15f)) g.DrawPath(pen, bp);
            }

            // Mini header bar
            float pad = 8f;
            float hdrH = 14f;
            Font hf = GetCachedFont(7.5f, FontStyle.Bold);
            sharedMainBrush.Color = GetTextColor(210);
            g.DrawString("BIRTHLY", hf, sharedMainBrush, box.X + pad + 2f, box.Y + pad);

            float innerX = box.X + pad;
            float innerY = box.Y + pad + hdrH + 4f;
            float innerW = box.Width - pad * 2f;
            float innerH = box.Height - pad * 2f - hdrH - 4f;
            float gap = 6f;

            RectangleF targetMHero, targetMList;
            if (stagedLayout == "HeroRight")
            {
                float hw = innerW * 0.38f;
                targetMList = new RectangleF(innerX, innerY, innerW - hw - gap, innerH);
                targetMHero = new RectangleF(targetMList.Right + gap, innerY, hw, innerH);
            }
            else if (stagedLayout == "HeroTop")
            {
                float hh = innerH * 0.36f;
                targetMHero = new RectangleF(innerX, innerY, innerW, hh);
                targetMList = new RectangleF(innerX, targetMHero.Bottom + gap, innerW, innerH - hh - gap);
            }
            else if (stagedLayout == "HeroBottom")
            {
                float hh = innerH * 0.36f;
                targetMList = new RectangleF(innerX, innerY, innerW, innerH - hh - gap);
                targetMHero = new RectangleF(innerX, targetMList.Bottom + gap, innerW, hh);
            }
            else
            {
                float hw = innerW * 0.38f;
                targetMHero = new RectangleF(innerX, innerY, hw, innerH);
                targetMList = new RectangleF(targetMHero.Right + gap, innerY, innerW - hw - gap, innerH);
            }

            // Smooth spring-interpolated sliding animation in the live layout mini-preview!
            float prevLerp = Math.Min(1.0f, dt * 16f);
            targetPrevHeroRect = targetMHero;
            targetPrevListRect = targetMList;
            animPrevHeroRect = LerpRect(animPrevHeroRect, targetMHero, prevLerp);
            animPrevListRect = LerpRect(animPrevListRect, targetMList, prevLerp);
            RectangleF mHero = animPrevHeroRect;
            RectangleF mList = animPrevListRect;

            // Draw List mini-block
            using (GraphicsPath lp = CreateRoundRectPath(mList, 8f))
            {
                sharedFillBrush.Color = GetCardSurfaceColor(30);
                g.FillPath(sharedFillBrush, lp);
                sharedRimPen.Color = GetThemeRimColor(90);
                sharedRimPen.Width = 1f;
                g.DrawPath(sharedRimPen, lp);
                Font mf = GetCachedFont(8.5f, FontStyle.Regular);
                sharedMainBrush.Color = GetTextColor(210);
                g.DrawString("LIST / CAL", mf, sharedMainBrush, mList, sfCenter);
            }

            // Draw draggable Hero mini-block
            float heroHov = GetHoverProgress("prev_drag_hero", dt);
            using (GraphicsPath hp = CreateRoundRectPath(mHero, 8f))
            {
                sharedFillBrush.Color = GetCardSurfaceColor(isDraggingPreviewBlock ? 90 : (int)(52 + heroHov * 30));
                g.FillPath(sharedFillBrush, hp);
                sharedRimPen.Color = GetThemeRimColor(isDraggingPreviewBlock ? 245 : 185);
                sharedRimPen.Width = 1.35f;
                g.DrawPath(sharedRimPen, hp);
                Font mf = GetCachedFont(8.5f, FontStyle.Bold);
                sharedMainBrush.Color = GetTextColor(250);
                g.DrawString("HERO ↔", mf, sharedMainBrush, mHero, sfCenter);
            }

            zones.Add(new HitZone { Id = "prev_drag_hero", Bounds = box });
        }

        private void DrawAnimatedGlassCard(Graphics g, RectangleF rect, float radius, bool isHighlighted, float hoverLerp, float t, float phaseOffset)
        {
            bool basic = IsBasicThemeEnabled();
            float effRad = basic ? Math.Min(radius, 8f) : radius;
            using (GraphicsPath path = CreateRoundRectPath(rect, effRad))
            {
                float breath = basic ? 0f : (0.5f + 0.5f * (float)Math.Sin(t * 1.6f + phaseOffset));
                int fillAlpha = (int)((isHighlighted ? 30 : 18) + hoverLerp * 18f + breath * 4f);
                sharedFillBrush.Color = GetCardSurfaceColorAt(rect, fillAlpha);
                g.FillPath(sharedFillBrush, path);

                // Top specular highlight line (Glass Mode only!)
                if (!basic)
                {
                    float inset = Math.Min(effRad * 0.78f, rect.Width * 0.25f);
                    if (rect.Width > inset * 2f + 4f)
                    {
                        int sheenA = (int)((isHighlighted ? 56 : 30) + hoverLerp * 26f);
                        sharedRimPen.Color = GetThemeRimColor(ClampInt(sheenA, 0, 255));
                        sharedRimPen.Width = 1.0f;
                        g.DrawLine(sharedRimPen, rect.X + inset, rect.Y + 0.8f, rect.Right - inset, rect.Y + 0.8f);
                    }
                }

                int borderA = (int)((isHighlighted ? 142 : 76) + hoverLerp * 62f);
                sharedRimPen.Color = GetThemeRimColor(ClampInt(borderA, 0, 255));
                sharedRimPen.Width = (basic ? 1.0f : (isHighlighted ? 1.35f : 1.05f));
                g.DrawPath(sharedRimPen, path);
            }
        }

        private static int ClampInt(int v, int min, int max)
        {
            return v < min ? min : (v > max ? max : v);
        }

        // Animated Frosted Glass Pill Button (or Crisp Flat Rounded-Rect Button in Basic Mode!)
        private void DrawAnimatedGlassButton(Graphics g, RectangleF rect, string text, bool active, float hoverLerp, float t)
        {
            bool basic = IsBasicThemeEnabled();
            float btnRad = basic ? 6f : rect.Height * 0.48f;
            using (GraphicsPath path = CreateRoundRectPath(rect, btnRad))
            {
                bool darkSurf = (IsDarkModeEnabled() || basic || GetActiveTint() == "Obsidian") && !IsBasicLightMode();
                int fillAlpha = darkSurf
                    ? (int)((active ? 34 : 17) + hoverLerp * 24f)
                    : (int)((active ? 42 : 20) + hoverLerp * 32f);
                sharedFillBrush.Color = GetCardSurfaceColor(fillAlpha);
                g.FillPath(sharedFillBrush, path);

                // Dynamic Cursor-Tracking Specular Spotlight (Glass Mode only — Basic Mode uses clean flat hover fill!)
                if (!basic && hoverLerp > 0.02f)
                {
                    g.SetClip(path);
                    float spotX = isCursorInside ? Clamp(localCursorPos.X, rect.Left, rect.Right) : (rect.X + rect.Width * 0.5f);
                    float spotY = isCursorInside ? Clamp(localCursorPos.Y, rect.Top, rect.Bottom) : (rect.Y + rect.Height * 0.5f);
                    float rad = Math.Max(rect.Width * 0.55f, 38f);

                    using (GraphicsPath glowP = new GraphicsPath())
                    {
                        glowP.AddEllipse(spotX - rad, spotY - rad, rad * 2f, rad * 2f);
                        using (PathGradientBrush pgb = new PathGradientBrush(glowP))
                        {
                            Color gc = GetCardSurfaceColor((int)((darkSurf ? 52f : 68f) * hoverLerp));
                            pgb.CenterColor = gc;
                            pgb.SurroundColors = new Color[] { Color.FromArgb(0, gc.R, gc.G, gc.B) };
                            g.FillPath(pgb, glowP);
                        }
                    }
                    g.ResetClip();
                }

                int rimAlpha = (int)((active ? 200 : 76) + hoverLerp * 100f);
                sharedRimPen.Color = GetThemeRimColor(ClampInt(rimAlpha, 0, 255));
                sharedRimPen.Width = basic ? (active ? 1.25f : 1.0f) : ((active ? 1.35f : 1.0f) + hoverLerp * 0.30f);
                g.DrawPath(sharedRimPen, path);

                float btnFontSz = (!string.IsNullOrEmpty(text) && ((rect.Width < 64f && text.Length >= 7) || (rect.Width < 150f && text.Length >= 16))) ? 9.5f : 10.5f;
                Font f = GetCachedFont(btnFontSz, active ? FontStyle.Bold : FontStyle.Regular);
                DrawAdaptiveString(g, text, f, rect, sfCenter, (int)((active ? 252 : 220) + hoverLerp * 30f));
            }
        }

        // Sleek In-Glass Search Bar (or Crisp Flat Search Box in Basic Mode)
        private void DrawInGlassSearchBox(Graphics g, RectangleF rect, string query, bool focused, float hoverLerp, float t, string placeholder = "SEARCH PEOPLE...")
        {
            using (GraphicsPath path = CreateRoundRectPath(rect, IsBasicThemeEnabled() ? 6f : rect.Height * 0.48f))
            {
                int fillA = (int)((focused ? 38 : 18) + hoverLerp * 18f);
                sharedFillBrush.Color = GetCardSurfaceColor(fillA);
                g.FillPath(sharedFillBrush, path);

                int rimA = ClampInt((int)((focused ? 175 : 76) + hoverLerp * 55f), 0, 255);
                sharedRimPen.Color = GetThemeRimColor(rimA);
                sharedRimPen.Width = focused ? 1.3f : 1.0f;
                g.DrawPath(sharedRimPen, path);

                // Minimal Vector Magnifying Glass Icon on the left
                float mx = rect.X + 12f;
                float my = rect.Y + rect.Height * 0.5f - 1f;
                using (Pen mp = new Pen(GetAdaptiveLetterColor(mx, my, focused ? 235 : 170), 1.45f))
                {
                    mp.StartCap = LineCap.Round;
                    mp.EndCap = LineCap.Round;
                    g.DrawEllipse(mp, mx - 4.2f, my - 4.2f, 8.4f, 8.4f);
                    g.DrawLine(mp, mx + 3.0f, my + 3.0f, mx + 6.2f, my + 6.2f);
                }

                bool isEmpty = string.IsNullOrEmpty(query);
                string disp = isEmpty ? placeholder : query;
                if (focused)
                {
                    bool showCaret = ((int)(t * 2.2f) % 2) == 0;
                    disp = (query ?? "") + (showCaret ? "|" : "");
                }

                Font f = GetCachedFont(11f, FontStyle.Regular);
                RectangleF txtRect = new RectangleF(rect.X + 26f, rect.Y, Math.Max(20f, rect.Width - 52f), rect.Height);
                DrawAdaptiveString(g, disp, f, txtRect, sfNearCenter, isEmpty && !focused ? 145 : 245);
            }
        }

        private void DrawInGlassTextBox(Graphics g, RectangleF rect, string value, string placeholder, bool focused, float hoverLerp, float t)
        {
            using (GraphicsPath path = CreateRoundRectPath(rect, 11f))
            {
                int fillA = (int)((focused ? 36 : 18) + hoverLerp * 16f);
                sharedFillBrush.Color = GetCardSurfaceColor(fillA);
                g.FillPath(sharedFillBrush, path);

                int rimA = ClampInt((int)((focused ? 175 : 76) + hoverLerp * 55f), 0, 255);
                sharedRimPen.Color = GetThemeRimColor(rimA);
                sharedRimPen.Width = focused ? 1.35f : 1.0f;
                g.DrawPath(sharedRimPen, path);

                bool isEmpty = string.IsNullOrEmpty(value);
                string val = value ?? "";
                string disp = isEmpty ? placeholder : val;
                if (focused)
                {
                    int cp = ClampInt(editCaretPos, 0, val.Length);
                    bool showCaret = ((int)(t * 2.2f) % 2) == 0;
                    string caretChar = showCaret ? "|" : " ";
                    // Keep a rolling visible window around the caret so even 10,000-character gifts/wishes scroll smoothly!
                    int maxVis = Math.Max(14, (int)((rect.Width - 28f) / 7.5f));
                    if (val.Length <= maxVis)
                    {
                        disp = val.Substring(0, cp) + caretChar + val.Substring(cp);
                    }
                    else
                    {
                        int winStart = Math.Max(0, Math.Min(val.Length - maxVis, cp - maxVis + 6));
                        int winLen = Math.Min(maxVis, val.Length - winStart);
                        int relCp = ClampInt(cp - winStart, 0, winLen);
                        string slice = val.Substring(winStart, winLen);
                        disp = slice.Substring(0, relCp) + caretChar + slice.Substring(relCp);
                    }
                }

                Font f = GetCachedFont(12.5f, FontStyle.Regular);
                RectangleF txtRect = new RectangleF(rect.X + 12f, rect.Y, rect.Width - 24f, rect.Height);
                DrawAdaptiveString(g, disp, f, txtRect, sfNearCenter, isEmpty && !focused ? 130 : 245);
            }
        }

        private void DrawMinimalCountdownTile(Graphics g, RectangleF rect, string numText, string unitText, float tickPulse, float t)
        {
            DrawAnimatedGlassCard(g, rect, 12f, false, tickPulse * 0.65f, t, 0f);

            Font nf = GetCachedFont(17f, FontStyle.Bold);
            Font uf = GetCachedFont(8.5f, FontStyle.Bold);
            DrawAdaptiveString(g, numText, nf, new RectangleF(rect.X, rect.Y + 5f, rect.Width, 24f), sfCenter, 248);
            DrawAdaptiveString(g, unitText, uf, new RectangleF(rect.X, rect.Y + 30f, rect.Width, 15f), sfCenter, 165);
        }

        private void DrawMinimalAvatar(Graphics g, PersonEntry p, RectangleF rect, float t)
        {
            // Quantize diameter to even multiples of 2px so spring layout transitions never trigger dozens of 16x-SSAA image re-rasterizations!
            int targetSize = Math.Max(16, (((int)Math.Round(rect.Width) + 1) / 2) * 2);
            Image customImg = (p.IconType == "image" && !string.IsNullOrEmpty(p.ImagePath))
                ? GetCachedCircularAvatarImage(p.ImagePath, targetSize)
                : null;

            if (customImg != null && p.IconType == "image")
            {
                g.DrawImage(customImg, rect.X, rect.Y, rect.Width, rect.Height);
            }
            else
            {
                string icType = (p.IconType ?? "initials").ToLowerInvariant();
                if (icType == "user" || icType == "star" || icType == "heart" || icType == "gift")
                {
                    DrawCleanVectorIcon(g, icType, rect);
                }
                else
                {
                    string initials = GetCleanInitials(p.Name);
                    float fontSize = rect.Width * 0.44f;
                    Font icFont = GetCachedFont(fontSize, FontStyle.Bold, true);
                    sharedMainBrush.Color = GetAdaptiveTextColorAt(rect, 245);
                    g.DrawString(initials, icFont, sharedMainBrush, rect, sfCenter);
                }
            }
        }

        private void DrawCleanVectorIcon(Graphics g, string type, RectangleF rect)
        {
            float cx = rect.X + rect.Width * 0.5f;
            float cy = rect.Y + rect.Height * 0.5f;
            float r = rect.Width * 0.24f;

            using (Pen p = new Pen(GetAdaptiveTextColorAt(rect, 240), Math.Max(1.6f, rect.Width * 0.036f)))
            using (SolidBrush fillBr = new SolidBrush(GetCardSurfaceColorAt(rect, 45)))
            {
                p.StartCap = LineCap.Round;
                p.EndCap = LineCap.Round;
                p.LineJoin = LineJoin.Round;

                if (type == "user")
                {
                    // Clean Apple-style User Silhouette (Head circle + curved shoulders & bust!)
                    float headR = r * 0.46f;
                    RectangleF headRect = new RectangleF(cx - headR, cy - r * 0.88f, headR * 2f, headR * 2f);
                    g.FillEllipse(fillBr, headRect);
                    g.DrawEllipse(p, headRect);

                    using (GraphicsPath bodyPath = new GraphicsPath())
                    {
                        float shW = r * 1.65f;
                        float topShoulderY = cy + r * 0.08f;
                        float bottomBustY  = cy + r * 0.88f;
                        float archH = (bottomBustY - topShoulderY) * 2.0f;
                        RectangleF shRect = new RectangleF(cx - shW * 0.5f, topShoulderY, shW, archH);
                        // Add top half of ellipse (180 -> 180) and close flat along the bottom at bottomBustY
                        bodyPath.AddArc(shRect, 180f, 180f);
                        bodyPath.CloseFigure();
                        g.FillPath(fillBr, bodyPath);
                        g.DrawPath(p, bodyPath);
                    }
                }
                else if (type == "star")
                {
                    PointF[] pts = new PointF[10];
                    for (int i = 0; i < 10; i++)
                    {
                        double ang = -Math.PI / 2 + i * Math.PI / 5;
                        float rad = (i % 2 == 0) ? r * 1.05f : r * 0.45f;
                        pts[i] = new PointF(cx + (float)Math.Cos(ang) * rad, cy + (float)Math.Sin(ang) * rad);
                    }
                    g.FillPolygon(fillBr, pts);
                    g.DrawPolygon(p, pts);
                }
                else if (type == "heart")
                {
                    using (GraphicsPath hp = new GraphicsPath())
                    {
                        float s = r * 0.95f;
                        hp.AddBezier(cx, cy + s * 0.85f, cx - s * 1.3f, cy + s * 0.1f, cx - s * 0.7f, cy - s * 0.95f, cx, cy - s * 0.35f);
                        hp.AddBezier(cx, cy - s * 0.35f, cx + s * 0.7f, cy - s * 0.95f, cx + s * 1.3f, cy + s * 0.1f, cx, cy + s * 0.85f);
                        g.FillPath(fillBr, hp);
                        g.DrawPath(p, hp);
                    }
                }
                else if (type == "gift")
                {
                    float bw = r * 1.5f;
                    float bh = r * 1.3f;
                    RectangleF boxR = new RectangleF(cx - bw * 0.5f, cy - bh * 0.3f, bw, bh);
                    g.FillRectangle(fillBr, boxR);
                    g.DrawRectangle(p, boxR.X, boxR.Y, boxR.Width, boxR.Height);
                    g.DrawLine(p, cx, cy - bh * 0.3f, cx, cy + bh * 0.7f);
                    g.DrawLine(p, cx - bw * 0.5f, cy + bh * 0.15f, cx + bw * 0.5f, cy + bh * 0.15f);
                    g.DrawEllipse(p, cx - r * 0.45f, cy - bh * 0.65f, r * 0.45f, bh * 0.35f);
                    g.DrawEllipse(p, cx, cy - bh * 0.65f, r * 0.45f, bh * 0.35f);
                }
            }
        }

        private string GetCleanInitials(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "B";
            string[] parts = name.Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
                return (parts[0][0].ToString() + parts[parts.Length - 1][0].ToString()).ToUpper();
            return parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpper();
        }

        private Image GetCachedCircularAvatarImage(string path, int diameter)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
            string cacheKey = path + "|" + diameter;
            Image cached;
            if (avatarCache.TryGetValue(cacheKey, out cached)) return cached;

            try
            {
                // Auto-downscale oversized existing avatar images (> 256px) on first load so they stay fast and lightweight!
                try
                {
                    if (path.StartsWith(avatarsDir, StringComparison.OrdinalIgnoreCase))
                    {
                        bool needsShrink = false;
                        using (FileStream checkFs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        using (Image probe = Image.FromStream(checkFs, false, false))
                        {
                            if (probe.Width > 256 || probe.Height > 256) needsShrink = true;
                        }
                        if (needsShrink)
                        {
                            string tmpPath = path + ".tmp.png";
                            DownscaleAndSaveAvatar(path, tmpPath, 256);
                            File.Copy(tmpPath, path, true);
                            File.Delete(tmpPath);
                        }
                    }
                }
                catch { }

                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (Image rawImg = Image.FromStream(fs))
                using (Bitmap scaledBmp = new Bitmap(diameter, diameter, PixelFormat.Format32bppArgb))
                {
                    using (Graphics g = Graphics.FromImage(scaledBmp))
                    {
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.SmoothingMode = SmoothingMode.HighQuality;
                        g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                        int srcSize = Math.Min(rawImg.Width, rawImg.Height);
                        int srcX = (rawImg.Width - srcSize) / 2;
                        int srcY = (rawImg.Height - srcSize) / 2;
                        g.DrawImage(rawImg, new Rectangle(0, 0, diameter, diameter), new Rectangle(srcX, srcY, srcSize, srcSize), GraphicsUnit.Pixel);
                    }

                    int[] px = ExtractPixels(scaledBmp);
                    float rad = diameter * 0.5f;
                    float cx = rad - 0.5f;
                    float cy = rad - 0.5f;
                    float innerRad = rad - 0.6f;

                    for (int y = 0; y < diameter; y++)
                    {
                        for (int x = 0; x < diameter; x++)
                        {
                            float accum = 0f;
                            for (int sy = 0; sy < 4; sy++)
                            {
                                float dy = (y + (sy - 1.5f) * 0.25f) - cy;
                                for (int sx = 0; sx < 4; sx++)
                                {
                                    float dx = (x + (sx - 1.5f) * 0.25f) - cx;
                                    float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                                    float sdf = dist - innerRad;
                                    float sc = Clamp01(0.5f - sdf * 0.95f);
                                    accum += sc * sc * (3f - 2f * sc);
                                }
                            }
                            float cov = accum * (1f / 16f);
                            int idx = y * diameter + x;
                            if (cov <= 0.001f)
                            {
                                px[idx] = 0;
                            }
                            else
                            {
                                int c = px[idx];
                                int r = (c >> 16) & 0xFF;
                                int gr = (c >> 8) & 0xFF;
                                int b = c & 0xFF;
                                int a = (int)(cov * 255f + 0.5f);
                                px[idx] = (a << 24) | (r << 16) | (gr << 8) | b;
                            }
                        }
                    }

                    Bitmap circleBmp = new Bitmap(diameter, diameter, PixelFormat.Format32bppArgb);
                    BitmapData bd = circleBmp.LockBits(new Rectangle(0, 0, diameter, diameter), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                    Marshal.Copy(px, 0, bd.Scan0, px.Length);
                    circleBmp.UnlockBits(bd);

                    avatarCache[cacheKey] = circleBmp;
                    return circleBmp;
                }
            }
            catch { return null; }
        }

        private GraphicsPath CreateRoundRectPath(RectangleF r, float rad)
        {
            GraphicsPath path = new GraphicsPath();
            float d = Math.Min(rad * 2f, Math.Min(r.Width, r.Height));
            if (d <= 1f)
            {
                path.AddRectangle(r);
                return path;
            }
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void PickAvatarImageFile()
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "Select Avatar Photo";
                ofd.Filter = "Image Files (*.png;*.jpg;*.jpeg;*.bmp;*.gif)|*.png;*.jpg;*.jpeg;*.bmp;*.gif";
                if (ofd.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        string idStr = editingPerson != null ? editingPerson.Id : "img";
                        string destFile = Path.Combine(avatarsDir, idStr + "_" + DateTime.Now.Ticks + ".png");
                        DownscaleAndSaveAvatar(ofd.FileName, destFile, 256);
                        editImagePath = destFile;
                        editIconType = "image";
                        Interlocked.Exchange(ref dirtyFrame, 1);
                    }
                    catch { }
                }
            }
        }

        // Automatically center-crops and high-quality downscales high-resolution images to <= maxDim x maxDim PNG!
        private void DownscaleAndSaveAvatar(string srcPath, string destPath, int maxDim)
        {
            using (FileStream fs = new FileStream(srcPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (Image rawImg = Image.FromStream(fs))
            {
                int srcSize = Math.Min(rawImg.Width, rawImg.Height);
                int srcX = (rawImg.Width - srcSize) / 2;
                int srcY = (rawImg.Height - srcSize) / 2;
                int targetDim = Math.Min(maxDim, Math.Max(64, srcSize));

                using (Bitmap outBmp = new Bitmap(targetDim, targetDim, PixelFormat.Format32bppArgb))
                {
                    using (Graphics g = Graphics.FromImage(outBmp))
                    {
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.SmoothingMode = SmoothingMode.HighQuality;
                        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        g.CompositingQuality = CompositingQuality.HighQuality;
                        g.DrawImage(rawImg, new Rectangle(0, 0, targetDim, targetDim), new Rectangle(srcX, srcY, srcSize, srcSize), GraphicsUnit.Pixel);
                    }
                    outBmp.Save(destPath, ImageFormat.Png);
                }
            }
        }

        private void SetAutoStartRegistry(bool enabled)
        {
            try
            {
                string exePath = Application.ExecutablePath;
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key != null)
                    {
                        key.DeleteValue("LiquidGlassWidget", false);
                        key.DeleteValue("birthdaytime", false);
                        if (enabled) key.SetValue("Birthly", "\"" + exePath + "\"");
                        else key.DeleteValue("Birthly", false);
                    }
                }
            }
            catch { }
        }

        private void AllocatePersistentDIB()
        {
            lock (renderLock)
            {
                FreePersistentDIB();
                persistentScreenDc = GetDC(IntPtr.Zero);
                persistentMemDc = CreateCompatibleDC(persistentScreenDc);

                BITMAPINFO bmi = new BITMAPINFO();
                bmi.bmiHeader.biSize = (uint)Marshal.SizeOf(typeof(BITMAPINFOHEADER));
                bmi.bmiHeader.biWidth = surfaceW;
                bmi.bmiHeader.biHeight = -surfaceH;
                bmi.bmiHeader.biPlanes = 1;
                bmi.bmiHeader.biBitCount = 32;
                bmi.bmiHeader.biCompression = 0;

                persistentDib = CreateDIBSection(persistentScreenDc, ref bmi, 0, out persistentBitsPtr, IntPtr.Zero, 0);
                if (persistentDib != IntPtr.Zero)
                {
                    persistentOldBmp = SelectObject(persistentMemDc, persistentDib);
                }
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

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            cachedMyHwnd = this.Handle;
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            cachedMyHwnd = IntPtr.Zero;
            base.OnHandleDestroyed(e);
        }

        protected override void Dispose(bool disposing)
        {
            isShuttingDown = true;
            try { renderWakeEvent.Set(); } catch { }
            try
            {
                if (liveCaptureThread != null && liveCaptureThread.IsAlive) liveCaptureThread.Join(350);
                if (vsyncDragThread != null && vsyncDragThread.IsAlive) vsyncDragThread.Join(350);
            }
            catch { }

            try { timeEndPeriod(1); } catch { }

            if (disposing)
            {
                try { if (desktopCheckTimer != null) { desktopCheckTimer.Stop(); desktopCheckTimer.Dispose(); desktopCheckTimer = null; } } catch { }
                try { if (contextMenu != null) { contextMenu.Dispose(); contextMenu = null; } } catch { }
                try { sfCenter.Dispose(); sfCenterWrap.Dispose(); sfNearCenter.Dispose(); sfNearNear.Dispose(); } catch { }
                try { sharedMainBrush.Dispose(); sharedShadowBrush.Dispose(); sharedFillBrush.Dispose(); sharedRimPen.Dispose(); renderWakeEvent.Dispose(); } catch { }

                lock (uiLock)
                {
                    if (reusableUiBmp != null) { reusableUiBmp.Dispose(); reusableUiBmp = null; }
                    foreach (Image img in avatarCache.Values) { try { img.Dispose(); } catch { } }
                    avatarCache.Clear();
                    foreach (Font f in fontCache.Values) { try { f.Dispose(); } catch { } }
                    fontCache.Clear();
                }

                try { if (curGlassDefault != null && curGlassDefault != Cursors.Default) curGlassDefault.Dispose(); } catch { }
                if (customCursorHicon != IntPtr.Zero) { try { DestroyIcon(customCursorHicon); } catch { } customCursorHicon = IntPtr.Zero; }
                try { if (privateFonts != null) { privateFonts.Dispose(); privateFonts = null; } } catch { }
            }

            lock (renderLock)
            {
                FreePersistentDIB();
                FreePersistentCaptureBuffers();
            }
            base.Dispose(disposing);
        }

        private void UpdateOcclusionState()
        {
            IntPtr myHwnd = cachedMyHwnd;
            if (isDragging || isCursorInside || myHwnd == IntPtr.Zero)
            {
                isFullyOccluded = false;
                occludedLocalRects = new Rectangle[0];
                return;
            }
            if (occlusionCheckTimer.ElapsedMilliseconds < 450) return;
            occlusionCheckTimer.Restart();

            try
            {
                int wx = currentWinX;
                int wy = currentWinY;
                int sw = surfaceW;
                int sh = surfaceH;
                Rectangle myRect = new Rectangle(wx, wy, sw, sh);

                List<Rectangle> coverLocal = new List<Rectangle>();
                IntPtr cur = GetWindow(myHwnd, GW_HWNDPREV);
                int safety = 0;

                while (cur != IntPtr.Zero && safety++ < 120)
                {
                    if (IsWindowVisible(cur) && !IsIconic(cur))
                    {
                        // Check rect overlap & layered style FIRST before invoking any DwmGetWindowAttribute RPC!
                        int exStyle = GetWindowLong(cur, -20);
                        if ((exStyle & WS_EX_LAYERED) == 0)
                        {
                            RECT wr;
                            if (GetWindowRect(cur, out wr))
                            {
                                int ww = wr.Right - wr.Left;
                                int wh = wr.Bottom - wr.Top;
                                if (ww > 24 && wh > 24)
                                {
                                    Rectangle wRect = new Rectangle(wr.Left + 8, wr.Top + 2, Math.Max(0, ww - 16), Math.Max(0, wh - 10));
                                    Rectangle inter = Rectangle.Intersect(myRect, wRect);
                                    if (inter.Width > 0 && inter.Height > 0)
                                    {
                                        int cloaked = 0;
                                        try { DwmGetWindowAttribute(cur, DWMWA_CLOAKED, out cloaked, sizeof(int)); } catch { cloaked = 0; }
                                        if (cloaked == 0)
                                        {
                                            coverLocal.Add(new Rectangle(inter.X - wx, inter.Y - wy, inter.Width, inter.Height));
                                        }
                                    }
                                }
                            }
                        }
                    }
                    cur = GetWindow(cur, GW_HWNDPREV);
                }

                Rectangle[] arr = coverLocal.ToArray();
                occludedLocalRects = arr;

                if (arr.Length == 0)
                {
                    isFullyOccluded = false;
                }
                else
                {
                    bool allCovered = true;
                    for (int gy = 0; gy < 5 && allCovered; gy++)
                    {
                        int py = (sh * (gy * 2 + 1)) / 10;
                        for (int gx = 0; gx < 5; gx++)
                        {
                            int px = (sw * (gx * 2 + 1)) / 10;
                            bool hit = false;
                            for (int k = 0; k < arr.Length; k++)
                            {
                                if (arr[k].Contains(px, py)) { hit = true; break; }
                            }
                            if (!hit) { allCovered = false; break; }
                        }
                    }
                    isFullyOccluded = allCovered;
                }
            }
            catch
            {
                isFullyOccluded = false;
                occludedLocalRects = new Rectangle[0];
            }
        }

        // High-refresh live desktop/window capture loop (paces to user's TargetRefreshRate / monitor Hz, e.g. 60-165Hz, and refreshes smoothly while dragging when RefreshWhileDragging is ON!)
        private long lastBackdropHash = 0;
        private int staticBackdropStreak = 0;

        private void LiveCaptureLoop()
        {
            Stopwatch capSw = Stopwatch.StartNew();
            while (!isShuttingDown)
            {
                try
                {
                    UpdateOcclusionState();

                    // Basic Theme uses a solid surface — zero desktop capture or blur needed!
                    if (IsBasicThemeEnabled())
                    {
                        Thread.Sleep(250);
                        continue;
                    }
                    if (isDragging && (!needFullScreenCapture || (config != null && !config.RefreshWhileDragging)))
                    {
                        Thread.Sleep(24);
                        continue;
                    }
                    if (isFullyOccluded && !isDragging)
                    {
                        Thread.Sleep(250);
                        continue;
                    }
                    IntPtr myHwnd = cachedMyHwnd;
                    if (myHwnd != IntPtr.Zero && IsIconic(myHwnd))
                    {
                        Thread.Sleep(200);
                        continue;
                    }

                    capSw.Restart();
                    if (CaptureLiveDesktopFrameFast())
                    {
                        staticBackdropStreak = 0;
                        Interlocked.Increment(ref backdropFrameSeq);
                        try { renderWakeEvent.Set(); } catch { }
                    }
                    else
                    {
                        if (staticBackdropStreak < 100) staticBackdropStreak++;
                    }

                    // Adaptive ultra-low-CPU capture cadence (never stalls DWM or tanks game/video FPS!):
                    // - Active video/changes behind glass: 30 Hz (33 ms)
                    // - Static desktop / wallpaper: backs off to ~12 Hz (85 ms) with instant dirty-hash skip!
                    double minFrameMs = (staticBackdropStreak >= 6 && !isDragging) ? 85.0 : 33.0;
                    double elapsed = capSw.Elapsed.TotalMilliseconds;
                    int sleepMs = (int)(minFrameMs - elapsed);
                    if (sleepMs >= 2) Thread.Sleep(sleepMs);
                }
                catch
                {
                    Thread.Sleep(60);
                }
            }
        }

        private bool HasActiveUIAnimation()
        {
            if (isSnappingToScreen) return true;
            if (hasActiveHoverTransition) return true;
            if (viewPopupProgress < 0.999f) return true;
            if (settingsTabProgress < 0.999f) return true;
            if (calendarViewProgress < 0.999f) return true;
            if (heroStepperProgress < 0.999f) return true;
            if (tutorialStepProgress < 0.999f) return true;
            if (isFilterDropdownOpen && filterDropdownProgress < 0.999f) return true;
            if (!isFilterDropdownOpen && filterDropdownProgress > 0.001f) return true;
            if (pendingDeletePerson != null && deleteModalProgress < 0.999f) return true;
            if (pendingDeletePerson == null && deleteModalProgress > 0.001f) return true;
            if (isContextMenuOpen && contextMenuProgress < 0.999f) return true;
            if (!isContextMenuOpen && contextMenuProgress > 0.001f) return true;
            if (currentView == ActiveView.Dashboard && (IsRectAnimating(animDashHeroRect, targetDashHeroRect) || IsRectAnimating(animDashPanelRect, targetDashPanelRect))) return true;
            if (currentView == ActiveView.Settings && currentSettingsCat == SettingsCategory.Appearance && (IsRectAnimating(animPrevHeroRect, targetPrevHeroRect) || IsRectAnimating(animPrevListRect, targetPrevListRect))) return true;
            if (isSearchFocused || isLayerSearchFocused || currentView == ActiveView.AddOrEditPerson) return true;
            float nowSec = (float)animClock.Elapsed.TotalSeconds;
            if (nowSec < wishCopiedUntilSec + 0.05f) return true;
            if (hasActiveBdayParticles) return true;
            return false;
        }

        private void VSyncRenderLoop()
        {
            Stopwatch frameSw = Stopwatch.StartNew();
            while (!isShuttingDown)
            {
                try
                {
                    frameSw.Restart();
                    float nowSec = (float)animClock.Elapsed.TotalSeconds;
                    float dt = Math.Max(0.001f, Math.Min(0.1f, nowSec - lastAnimTime));
                    lastAnimTime = nowSec;

                    bool moved = false;
                    if (isDragging)
                    {
                        if ((GetAsyncKeyState(0x01) & 0x8000) == 0)
                        {
                            this.BeginInvoke(new Action(EndDrag));
                        }
                        else
                        {
                            Point cur = Cursor.Position;
                            int newX = dragStartLocation.X + (cur.X - dragStartCursor.X);
                            int newY = dragStartLocation.Y + (cur.Y - dragStartCursor.Y);
                            if (newX != currentWinX || newY != currentWinY)
                            {
                                currentWinX = newX;
                                currentWinY = newY;
                                moved = true;
                            }
                        }
                    }
                    else if (isSnappingToScreen)
                    {
                        float snapFactor = Math.Min(1.0f, dt * 14.0f);
                        snapFloatX += (snapTargetX - snapFloatX) * snapFactor;
                        snapFloatY += (snapTargetY - snapFloatY) * snapFactor;
                        int nx = (int)Math.Round(snapFloatX);
                        int ny = (int)Math.Round(snapFloatY);
                        if (Math.Abs(snapTargetX - snapFloatX) < 0.75f && Math.Abs(snapTargetY - snapFloatY) < 0.75f)
                        {
                            nx = snapTargetX;
                            ny = snapTargetY;
                            isSnappingToScreen = false;
                            needFullScreenCapture = true;
                            try { this.BeginInvoke(new Action(() => { this.Location = new Point(nx, ny); SaveConfig(); })); } catch { }
                        }
                        if (nx != currentWinX || ny != currentWinY)
                        {
                            currentWinX = nx;
                            currentWinY = ny;
                            moved = true;
                        }
                    }

                    IntPtr myHwnd = cachedMyHwnd;
                    if (myHwnd != IntPtr.Zero)
                    {
                        if (IsIconic(myHwnd))
                        {
                            renderWakeEvent.WaitOne(120);
                            continue;
                        }
                        if (isFullyOccluded && !isDragging)
                        {
                            Thread.Sleep(150);
                            continue;
                        }

                        bool isDirty = Interlocked.Exchange(ref dirtyFrame, 0) == 1;
                        bool activeAnim = HasActiveUIAnimation();
                        int targetHz = GetEffectiveTargetHz();
                        float animInterval = 1.0f / Math.Max(60, targetHz);
                        // Rebuild UI at full monitor Hz while animating/hovering, or 2 Hz when idle (ultra-low CPU, instant <0.1ms wakeup on input!)
                        bool uiTimeDue = (nowSec - lastUiRebuildSec) >= (activeAnim ? animInterval : 0.50f);

                        if (!isDragging && (isDirty || uiTimeDue))
                        {
                            float uiDt = Math.Max(0.002f, Math.Min(0.1f, nowSec - lastUiRebuildSec));
                            lastUiRebuildSec = nowSec;
                            RebuildUIOverlay(uiDt);
                            activeAnim = HasActiveUIAnimation();
                        }

                        int curBgSeq = Volatile.Read(ref backdropFrameSeq);
                        int curUiSeq = Volatile.Read(ref uiFrameSeq);
                        bool basicTheme = IsBasicThemeEnabled();
                        if (isDragging && (basicTheme || (config != null && !config.RefreshWhileDragging)))
                        {
                            if (moved)
                            {
                                SetWindowPos(myHwnd, IntPtr.Zero, currentWinX, currentWinY, 0, 0,
                                    SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE | 0x4000);
                            }
                        }
                        else if (moved || isDirty || (!basicTheme && curBgSeq != lastCompositedBackdropSeq) || curUiSeq != lastCompositedUiSeq)
                        {
                            lastCompositedBackdropSeq = curBgSeq;
                            lastCompositedUiSeq = curUiSeq;
                            RenderLiquidGlassFrame();
                        }

                        // Instant event-driven wakeup via renderWakeEvent (<0.1ms input response, ~0% idle CPU!)
                        if (!isDragging && !isSnappingToScreen && !activeAnim && Volatile.Read(ref dirtyFrame) == 0)
                        {
                            renderWakeEvent.WaitOne(120);
                        }
                        else
                        {
                            double targetMs = 1000.0 / Math.Max(60, targetHz);
                            double rem = targetMs - frameSw.Elapsed.TotalMilliseconds;
                            if (rem > 1.5)
                            {
                                renderWakeEvent.WaitOne((int)rem);
                            }
                        }
                    }
                    else
                    {
                        Thread.Sleep(16);
                    }
                }
                catch
                {
                    Thread.Sleep(8);
                }
            }
        }

        // Zero-Allocation Fast Live Desktop Capture + Multi-Core Quarter-Res Gaussian Blur (~1.5ms blur, ROI-bounded Marshal.Copy + dirty hash!)
        private bool CaptureLiveDesktopFrameFast()
        {
            int w = deskW;
            int h = deskH;
            int nextIdx = 1 - activeBufferIdx;
            int[] dstSharp = nextIdx == 0 ? captureStagingA : captureStagingB;
            int[] dstBlur  = nextIdx == 0 ? blurOutA : blurOutB;

            int yStart = 0, yEnd = h, xStart = 0, xEnd = w;
            bool fullCapture = needFullScreenCapture || backdropFrameSeq <= 2;
            bool useRoi = !fullCapture;
            if (useRoi)
            {
                int margin = isDragging ? 280 : 140;
                int wx0 = (currentWinX - virtualScreenRect.X) - margin;
                int wy0 = (currentWinY - virtualScreenRect.Y) - margin;
                xStart = ClampInt(wx0, 0, w) & ~1;
                yStart = ClampInt(wy0, 0, h) & ~1;
                xEnd = ClampInt(wx0 + surfaceW + margin * 2, xStart, w) & ~1;
                yEnd = ClampInt(wy0 + surfaceH + margin * 2, yStart, h) & ~1;
            }

            int roiW = xEnd - xStart;
            int roiH = yEnd - yStart;
            if (roiW <= 2 || roiH <= 2) return false;

            bool capturedLive = false;

            // 1. Capture live backdrop: in Real Window Mode, BitBlt from screen DC (with WDA_EXCLUDEFROMCAPTURE) so Liquid Glass refracts whatever windows are actually behind Birthly!
            if (captureMemDc != IntPtr.Zero && captureBitsPtr != IntPtr.Zero)
            {
                try
                {
                    if (IsAppWindowModeEnabled())
                    {
                        IntPtr screenDc = GetDC(IntPtr.Zero);
                        if (screenDc != IntPtr.Zero)
                        {
                            try
                            {
                                bool bltOk = BitBlt(captureMemDc, xStart, yStart, roiW, roiH, screenDc, virtualScreenRect.X + xStart, virtualScreenRect.Y + yStart, 0x00CC0020);
                                if (bltOk)
                                {
                                    if (!useRoi)
                                    {
                                        Marshal.Copy(captureBitsPtr, dstSharp, 0, dstSharp.Length);
                                    }
                                    else
                                    {
                                        int startIdx = yStart * w + xStart;
                                        int copyLen = (yEnd - 1) * w + xEnd - startIdx;
                                        if (copyLen > 0)
                                        {
                                            IntPtr srcPtr = new IntPtr(captureBitsPtr.ToInt64() + (long)startIdx * 4L);
                                            Marshal.Copy(srcPtr, dstSharp, startIdx, copyLen);
                                        }
                                    }
                                    capturedLive = true;
                                }
                            }
                            finally
                            {
                                ReleaseDC(IntPtr.Zero, screenDc);
                            }
                        }
                    }
                    else
                    {
                        if (cachedProgmanHwnd == IntPtr.Zero || !IsWindowVisible(cachedProgmanHwnd))
                        {
                            cachedProgmanHwnd = FindWindow("Progman", null);
                        }
                        if (cachedProgmanHwnd != IntPtr.Zero)
                        {
                            if (PrintWindow(cachedProgmanHwnd, captureMemDc, 2))
                            {
                                if (!useRoi)
                                {
                                    Marshal.Copy(captureBitsPtr, dstSharp, 0, dstSharp.Length);
                                }
                                else
                                {
                                    int startIdx = yStart * w + xStart;
                                    int copyLen = (yEnd - 1) * w + xEnd - startIdx;
                                    if (copyLen > 0)
                                    {
                                        IntPtr srcPtr = new IntPtr(captureBitsPtr.ToInt64() + (long)startIdx * 4L);
                                        Marshal.Copy(srcPtr, dstSharp, startIdx, copyLen);
                                    }
                                }
                                int sampleX = (xStart + xEnd) >> 1;
                                int sampleY = (yStart + yEnd) >> 1;
                                int centerPx = dstSharp[sampleY * w + sampleX];
                                if (((centerPx >> 16) & 0xFF) + ((centerPx >> 8) & 0xFF) + (centerPx & 0xFF) > 15)
                                {
                                    capturedLive = true;
                                }
                            }
                        }
                    }
                }
                catch { }
            }

            // 2. Fallback for wallpaper if PrintWindow is unavailable
            if (!capturedLive && backdropFrameSeq <= 2)
            {
                try
                {
                    string wpPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft\Windows\Themes\TranscodedWallpaper");

                    if (File.Exists(wpPath))
                    {
                        using (Bitmap frameBmp = new Bitmap(w, h, PixelFormat.Format32bppArgb))
                        {
                            using (FileStream fs = new FileStream(wpPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                            using (Image wpImg = Image.FromStream(fs))
                            using (Graphics g = Graphics.FromImage(frameBmp))
                            {
                                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                                float scale = Math.Max((float)w / wpImg.Width, (float)h / wpImg.Height);
                                int drawW = (int)(wpImg.Width * scale);
                                int drawH = (int)(wpImg.Height * scale);
                                int drawX = (w - drawW) / 2;
                                int drawY = (h - drawH) / 2;
                                g.DrawImage(wpImg, new Rectangle(drawX, drawY, drawW, drawH));
                            }
                            BitmapData bd = frameBmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                            Marshal.Copy(bd.Scan0, dstSharp, 0, dstSharp.Length);
                            frameBmp.UnlockBits(bd);
                        }
                    }
                }
                catch { }
            }

            // 3. Refresh cached Rainmeter clock once per 2.5 seconds (only needed in Desktop Widget mode; Real Window BitBlt already captures everything behind Birthly!)
            if (!IsAppWindowModeEnabled() && (!hasCachedRain || rainRefreshTimer.ElapsedMilliseconds > 2500))
            {
                rainRefreshTimer.Restart();
                try
                {
                    StringBuilder cls = new StringBuilder(128);
                    EnumWindows((hwnd, lParam) => {
                        if (!IsWindowVisible(hwnd)) return true;
                        cls.Length = 0;
                        GetClassName(hwnd, cls, 128);
                        if (cls.ToString() == "RainmeterMeterWindow")
                        {
                            RECT r;
                            if (GetWindowRect(hwnd, out r))
                            {
                                int rw = r.Right - r.Left;
                                int rh = r.Bottom - r.Top;
                                if (rw > 0 && rh > 0 && rw < 3000 && rh < 3000)
                                {
                                    using (Bitmap rBmp = new Bitmap(rw, rh, PixelFormat.Format32bppArgb))
                                    {
                                        bool ok = false;
                                        using (Graphics rg = Graphics.FromImage(rBmp))
                                        {
                                            IntPtr hdc = rg.GetHdc();
                                            ok = PrintWindow(hwnd, hdc, 2);
                                            rg.ReleaseHdc(hdc);
                                        }
                                        if (ok)
                                        {
                                            cachedRainTextPixels = ExtractPixels(rBmp);
                                            cachedRainRect = r;
                                            hasCachedRain = true;
                                        }
                                    }
                                }
                            }
                        }
                        return true;
                    }, IntPtr.Zero);
                }
                catch { }
            }

            // Composite cached Rainmeter text directly onto dstSharp array (clamped to ROI, Desktop Widget mode only!)
            if (!IsAppWindowModeEnabled() && hasCachedRain && cachedRainTextPixels != null)
            {
                int rw = cachedRainRect.Right - cachedRainRect.Left;
                int rh = cachedRainRect.Bottom - cachedRainRect.Top;
                int dstX = cachedRainRect.Left - virtualScreenRect.X;
                int dstY = cachedRainRect.Top - virtualScreenRect.Y;

                int ry0 = Math.Max(0, yStart - dstY);
                int ry1 = Math.Min(rh, yEnd - dstY);
                int rx0 = Math.Max(0, xStart - dstX);
                int rx1 = Math.Min(rw, xEnd - dstX);

                for (int y = ry0; y < ry1; y++)
                {
                    int dy = dstY + y;
                    int sRow = y * rw;
                    int dRow = dy * w;

                    for (int x = rx0; x < rx1; x++)
                    {
                        int dx = dstX + x;
                        int sc = cachedRainTextPixels[sRow + x];
                        int sR = (sc >> 16) & 0xFF;
                        int sG = (sc >> 8) & 0xFF;
                        int sB = sc & 0xFF;
                        int effA = Math.Max(sR, Math.Max(sG, sB));
                        if (effA <= 6) continue;

                        int dc = dstSharp[dRow + dx];
                        int dR = (dc >> 16) & 0xFF;
                        int dG = (dc >> 8) & 0xFF;
                        int dB = dc & 0xFF;

                        int outR = (sR * effA + dR * (255 - effA)) / 255;
                        int outG = (sG * effA + dG * (255 - effA)) / 255;
                        int outB = (sB * effA + dB * (255 - effA)) / 255;

                        dstSharp[dRow + dx] = (0xFF << 24) | (outR << 16) | (outG << 8) | outB;
                    }
                }
            }

            // Fast 64-sample sparse dirty-hash: if nothing behind the window actually changed, skip blur & buffer swap completely (0% CPU + zero drag jitter!)
            if (!fullCapture && capturedLive)
            {
                long hHash = 1469598103934665603L;
                int stepX = Math.Max(1, roiW / 8);
                int stepY = Math.Max(1, roiH / 8);
                for (int sy = yStart + (stepY >> 1); sy < yEnd; sy += stepY)
                {
                    int rowOff = sy * w;
                    for (int sx = xStart + (stepX >> 1); sx < xEnd; sx += stepX)
                    {
                        hHash = (hHash ^ (uint)dstSharp[rowOff + sx]) * 1099511628211L;
                    }
                }
                hHash ^= ((long)xStart << 32) ^ ((long)yStart << 16) ^ ((long)roiW << 8) ^ roiH;
                if (hHash == lastBackdropHash)
                {
                    return false;
                }
                lastBackdropHash = hHash;
            }
            else
            {
                lastBackdropHash = 0;
            }

            // 4. Ultra-Fast Multi-Core Half-Resolution Gaussian Blur (~0.8 ms total, zero GDI+ Bitmap allocations!)
            ComputeFastArrayGaussianBlur(dstSharp, dstBlur, w, h, blurSigma, xStart, yStart, xEnd, yEnd);

            // When doing a full-screen capture (startup or right after drag), also sync the second buffer so future ROI updates never expose stale pixels on drag!
            if (fullCapture)
            {
                int[] otherSharp = nextIdx == 0 ? captureStagingB : captureStagingA;
                int[] otherBlur  = nextIdx == 0 ? blurOutB : blurOutA;
                if (otherSharp != null && otherSharp.Length == dstSharp.Length) Array.Copy(dstSharp, otherSharp, dstSharp.Length);
                if (otherBlur != null && otherBlur.Length == dstBlur.Length) Array.Copy(dstBlur, otherBlur, dstBlur.Length);
                needFullScreenCapture = false;
            }

            // Atomically swap active buffers
            sharpPixels = dstSharp;
            blurredPixels = dstBlur;
            activeBufferIdx = nextIdx;
            return true;
        }

        private void ComputeFastArrayGaussianBlur(int[] srcFull, int[] dstFull, int w, int h, float sigma, int xStart, int yStart, int xEnd, int yEnd)
        {
            int hw = Math.Max(1, w / 2);
            int hh = Math.Max(1, h / 2);

            if (xEnd - xStart <= 2 || yEnd - yStart <= 2) return;

            // Strictly stay inside the freshly captured ROI so we never sample stale pixels from the other double buffer!
            int hyStart = ClampInt(yStart / 2, 0, hh);
            int hyEnd   = ClampInt(yEnd / 2, hyStart, hh);
            int hxStart = ClampInt(xStart / 2, 0, hw);
            int hxEnd   = ClampInt(xEnd / 2, hxStart, hw);
            if (hxEnd - hxStart <= 1 || hyEnd - hyStart <= 1) return;

            // 1. Parallel FULL 2x2 box downsample into half-res buffer (ROI-only when steady!)
            Parallel.For(hyStart, hyEnd, hy =>
            {
                int sy0 = hy * 2;
                int hRow = hy * hw;
                int sRow0 = sy0 * w;
                int sRow1 = Math.Min(h - 1, sy0 + 1) * w;

                for (int hx = hxStart; hx < hxEnd; hx++)
                {
                    int sx0 = hx * 2;
                    int sx1 = Math.Min(w - 1, sx0 + 1);

                    int c0 = srcFull[sRow0 + sx0];
                    int c1 = srcFull[sRow0 + sx1];
                    int c2 = srcFull[sRow1 + sx0];
                    int c3 = srcFull[sRow1 + sx1];

                    int r = (((c0 >> 16) & 0xFF) + ((c1 >> 16) & 0xFF) + ((c2 >> 16) & 0xFF) + ((c3 >> 16) & 0xFF)) >> 2;
                    int g = (((c0 >> 8) & 0xFF)  + ((c1 >> 8) & 0xFF)  + ((c2 >> 8) & 0xFF)  + ((c3 >> 8) & 0xFF)) >> 2;
                    int b = ((c0 & 0xFF)         + (c1 & 0xFF)         + (c2 & 0xFF)         + (c3 & 0xFF)) >> 2;

                    quarterBuf1[hRow + hx] = (0xFF << 24) | (r << 16) | (g << 8) | b;
                }
            });

            // 2. Two-pass separable box blur at half resolution (parallel ROI-bounded = <0.8ms smooth Gaussian!)
            int radius = Math.Max(2, (int)Math.Round(sigma * 1.05f));
            for (int pass = 0; pass < 2; pass++)
            {
                BoxBlurHorizontalRoi(quarterBuf1, quarterBuf2, hw, hyStart, hyEnd, hxStart, hxEnd, radius);
                BoxBlurVerticalRoi(quarterBuf2, quarterBuf1, hw, hyStart, hyEnd, hxStart, hxEnd, radius);
            }

            // 3. Parallel smooth bilinear upsample from half-res to dstFull
            Parallel.For(yStart, yEnd, y =>
            {
                float fy = (y * 0.5f);
                int y0 = (int)fy;
                if (y0 >= hh - 1) y0 = hh - 2;
                if (y0 < 0) y0 = 0;
                int y1 = Math.Min(hh - 1, y0 + 1);
                int wy = (int)((fy - y0) * 256f);
                int invWy = 256 - wy;

                int row0 = y0 * hw;
                int row1 = y1 * hw;
                int dRow = y * w;

                for (int x = xStart; x < xEnd; x++)
                {
                    float fx = (x * 0.5f);
                    int x0 = (int)fx;
                    if (x0 >= hw - 1) x0 = hw - 2;
                    if (x0 < 0) x0 = 0;
                    int x1 = Math.Min(hw - 1, x0 + 1);
                    int wx = (int)((fx - x0) * 256f);
                    int invWx = 256 - wx;

                    int c00 = quarterBuf1[row0 + x0];
                    int c10 = quarterBuf1[row0 + x1];
                    int c01 = quarterBuf1[row1 + x0];
                    int c11 = quarterBuf1[row1 + x1];

                    int r0 = (((c00 >> 16) & 0xFF) * invWx + ((c10 >> 16) & 0xFF) * wx) >> 8;
                    int r1 = (((c01 >> 16) & 0xFF) * invWx + ((c11 >> 16) & 0xFF) * wx) >> 8;
                    int r  = (r0 * invWy + r1 * wy) >> 8;

                    int g0 = (((c00 >> 8) & 0xFF) * invWx + ((c10 >> 8) & 0xFF) * wx) >> 8;
                    int g1 = (((c01 >> 8) & 0xFF) * invWx + ((c11 >> 8) & 0xFF) * wx) >> 8;
                    int gr = (g0 * invWy + g1 * wy) >> 8;

                    int b0 = ((c00 & 0xFF) * invWx + (c10 & 0xFF) * wx) >> 8;
                    int b1 = ((c01 & 0xFF) * invWx + (c11 & 0xFF) * wx) >> 8;
                    int b  = (b0 * invWy + b1 * wy) >> 8;

                    dstFull[dRow + x] = (0xFF << 24) | (r << 16) | (gr << 8) | b;
                }
            });
        }

        private void BoxBlurHorizontalRoi(int[] src, int[] dst, int stride, int y0, int y1, int x0, int x1, int r)
        {
            int roiW = x1 - x0;
            if (roiW <= 1) return;
            float iarr = 1f / (r + r + 1);
            int maxX = x1 - 1;
            Parallel.For(y0, y1, y =>
            {
                int rowBase = y * stride;
                int fv = src[rowBase + x0];
                int lv = src[rowBase + maxX];
                int fR = (fv >> 16) & 0xFF, fG = (fv >> 8) & 0xFF, fB = fv & 0xFF;
                int lR = (lv >> 16) & 0xFF, lG = (lv >> 8) & 0xFF, lB = lv & 0xFF;

                int valR = (r + 1) * fR;
                int valG = (r + 1) * fG;
                int valB = (r + 1) * fB;
                for (int j = 0; j < r; j++)
                {
                    int c = src[rowBase + Math.Min(maxX, x0 + j)];
                    valR += (c >> 16) & 0xFF;
                    valG += (c >> 8) & 0xFF;
                    valB += c & 0xFF;
                }
                for (int x = x0; x < x1; x++)
                {
                    int cAdd = src[rowBase + Math.Min(maxX, x + r)];
                    int cSub = (x - r - 1 < x0) ? fv : src[rowBase + x - r - 1];
                    valR += ((cAdd >> 16) & 0xFF) - ((cSub >> 16) & 0xFF);
                    valG += ((cAdd >> 8) & 0xFF) - ((cSub >> 8) & 0xFF);
                    valB += (cAdd & 0xFF) - (cSub & 0xFF);
                    dst[rowBase + x] = (0xFF << 24) | (ClampInt((int)(valR * iarr), 0, 255) << 16) | (ClampInt((int)(valG * iarr), 0, 255) << 8) | ClampInt((int)(valB * iarr), 0, 255);
                }
            });
        }

        private void BoxBlurVerticalRoi(int[] src, int[] dst, int stride, int y0, int y1, int x0, int x1, int r)
        {
            int roiH = y1 - y0;
            if (roiH <= 1) return;
            float iarr = 1f / (r + r + 1);
            int maxY = y1 - 1;
            Parallel.For(x0, x1, x =>
            {
                int fv = src[y0 * stride + x];
                int fR = (fv >> 16) & 0xFF, fG = (fv >> 8) & 0xFF, fB = fv & 0xFF;

                int valR = (r + 1) * fR;
                int valG = (r + 1) * fG;
                int valB = (r + 1) * fB;
                for (int j = 0; j < r; j++)
                {
                    int c = src[Math.Min(maxY, y0 + j) * stride + x];
                    valR += (c >> 16) & 0xFF;
                    valG += (c >> 8) & 0xFF;
                    valB += c & 0xFF;
                }
                for (int y = y0; y < y1; y++)
                {
                    int cAdd = src[Math.Min(maxY, y + r) * stride + x];
                    int cSub = (y - r - 1 < y0) ? fv : src[(y - r - 1) * stride + x];
                    valR += ((cAdd >> 16) & 0xFF) - ((cSub >> 16) & 0xFF);
                    valG += ((cAdd >> 8) & 0xFF) - ((cSub >> 8) & 0xFF);
                    valB += (cAdd & 0xFF) - (cSub & 0xFF);
                    dst[y * stride + x] = (0xFF << 24) | (ClampInt((int)(valR * iarr), 0, 255) << 16) | (ClampInt((int)(valG * iarr), 0, 255) << 8) | ClampInt((int)(valB * iarr), 0, 255);
                }
            });
        }

        public void SwitchToSettingsForPreview()
        {
            showTutorialOverlay = false;
            config.DarkMode = true;
            OpenInGlassSettings();
            viewPopupProgress = 1.0f;
            RebuildUIOverlay(0.016f);
        }

        public void SwitchToCalendarForPreview()
        {
            currentView = ActiveView.Dashboard;
            isCalendarView = true;
            viewPopupProgress = 1.0f;
            RebuildUIOverlay(0.016f);
        }

        public void SwitchToSettingsCategoryForPreview(int catIdx)
        {
            showTutorialOverlay = false;
            OpenInGlassSettings();
            currentSettingsCat = (SettingsCategory)ClampInt(catIdx, 0, 4);
            viewPopupProgress = 1.0f;
            RebuildUIOverlay(0.016f);
        }

        public void ForceWhiteBackgroundForPreview()
        {
            int lightGrey = unchecked((int)0xFFE8EAEE);
            if (sharpPixels != null) for (int i = 0; i < sharpPixels.Length; i++) sharpPixels[i] = lightGrey;
            if (blurredPixels != null) for (int i = 0; i < blurredPixels.Length; i++) blurredPixels[i] = lightGrey;
            smoothedBgLuma = 234f;
            smoothedPeakLuma = 234f;
            smoothedPeakR = 232f;
            smoothedPeakG = 234f;
            smoothedPeakB = 238f;
            viewPopupProgress = 1.0f;
            RebuildUIOverlay(0.016f);
        }

        public void SwitchToDropdownForPreview()
        {
            showTutorialOverlay = false;
            config.DarkMode = true;
            if (config.People == null || config.People.Count == 0)
            {
                config.People = new List<PersonEntry> {
                    new PersonEntry { Id = "p1", Name = "Emma", Day = DateTime.Now.AddDays(3).Day, Month = DateTime.Now.AddDays(3).Month, BirthYear = 2001, IconType = "initials" }
                };
            }
            currentView = ActiveView.Dashboard;
            isFilterDropdownOpen = true;
            viewPopupProgress = 1.0f;
            RebuildUIOverlay(0.016f);
        }

        public void SwitchToBasicThemeForPreview()
        {
            showTutorialOverlay = false;
            config.UseBasicTheme = true;
            config.FontName = "Segoe UI";
            if (config.People == null || config.People.Count == 0)
            {
                config.People = new List<PersonEntry> {
                    new PersonEntry { Id = "p1", Name = "Emma", Day = DateTime.Now.AddDays(3).Day, Month = DateTime.Now.AddDays(3).Month, BirthYear = 2001, IconType = "initials", Notes = "Surprise party!" },
                    new PersonEntry { Id = "p2", Name = "Lucas", Day = DateTime.Now.AddDays(18).Day, Month = DateTime.Now.AddDays(18).Month, BirthYear = 1998, IconType = "star" }
                };
            }
            currentView = ActiveView.Dashboard;
            viewPopupProgress = 1.0f;
            RebuildUIOverlay(0.016f);
        }

        public void SwitchToBirthdayCelebrationPreview()
        {
            showTutorialOverlay = false;
            config.UseBasicTheme = false;
            config.DarkMode = false;
            config.TintVariant = "Rose";
            config.ShowAgeBadge = true;
            config.EnableBirthdayAnimation = true;
            config.EnableBirthdayMessage = true;
            config.People = new List<PersonEntry> {
                new PersonEntry { Id = "p1", Name = "Emma", Day = DateTime.Now.Day, Month = DateTime.Now.Month, BirthYear = DateTime.Now.Year - 25, IconType = "heart", Notes = "Cake & gifts!" },
                new PersonEntry { Id = "p2", Name = "Lucas", Day = DateTime.Now.AddDays(6).Day, Month = DateTime.Now.AddDays(6).Month, BirthYear = DateTime.Now.Year - 28, IconType = "star", Notes = "Dinner at 7pm" },
                new PersonEntry { Id = "p3", Name = "Sophia", Day = DateTime.Now.AddDays(19).Day, Month = DateTime.Now.AddDays(19).Month, BirthYear = DateTime.Now.Year - 22, IconType = "gift" }
            };
            bdayCelebrationStartSec = (float)animClock.Elapsed.TotalSeconds - 2.4f;
            bdayBannerDismissed = false;
            animDashHeroRect = RectangleF.Empty;
            animDashPanelRect = RectangleF.Empty;
            currentView = ActiveView.Dashboard;
            viewPopupProgress = 1.0f;
            RebuildUIOverlay(0.016f);
        }

        public void SwitchToBasicLightDropdownPreview()
        {
            showTutorialOverlay = false;
            config.UseBasicTheme = true;
            config.DarkMode = false;
            config.TintVariant = "Ice";
            config.FontName = "Segoe UI";
            config.ShowAgeBadge = true;
            config.People = new List<PersonEntry> {
                new PersonEntry { Id = "p1", Name = "Emma", Day = DateTime.Now.AddDays(3).Day, Month = DateTime.Now.AddDays(3).Month, BirthYear = DateTime.Now.Year - 24, IconType = "initials", Notes = "Surprise party!" },
                new PersonEntry { Id = "p2", Name = "Lucas", Day = DateTime.Now.AddDays(18).Day, Month = DateTime.Now.AddDays(18).Month, BirthYear = DateTime.Now.Year - 28, IconType = "star" }
            };
            animDashHeroRect = RectangleF.Empty;
            animDashPanelRect = RectangleF.Empty;
            currentView = ActiveView.Dashboard;
            isFilterDropdownOpen = true;
            filterDropdownProgress = 1.0f;
            viewPopupProgress = 1.0f;
            RebuildUIOverlay(0.016f);
        }

        public void SwitchToUserIconPreview()
        {
            StartInGlassPersonEditor(null);
            editName = "Alex";
            editIconType = "user";
            viewPopupProgress = 1.0f;
            RebuildUIOverlay(0.016f);
        }

        public void SwitchToDeleteConfirmForPreview()
        {
            currentView = ActiveView.Dashboard;
            if (config.People.Count > 0)
                pendingDeletePerson = config.People[0];
            viewPopupProgress = 1.0f;
            deleteModalProgress = 1.0f;
            RebuildUIOverlay(0.016f);
        }

        public void ExportPreviewImage(string outPath)
        {
            viewPopupProgress = 1.0f;
            RebuildUIOverlay(0.016f);
            RenderLiquidGlassFrame();

            int pad = 60;
            int outW = surfaceW + pad * 2;
            int outH = surfaceH + pad * 2;
            using (Bitmap comp = new Bitmap(outW, outH, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(comp))
            {
                using (Bitmap bg = new Bitmap(deskW, deskH, PixelFormat.Format32bppArgb))
                {
                    BitmapData bbd = bg.LockBits(new Rectangle(0, 0, deskW, deskH), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                    Marshal.Copy(sharpPixels, 0, bbd.Scan0, sharpPixels.Length);
                    bg.UnlockBits(bbd);

                    int srcX = Math.Max(0, (currentWinX - virtualScreenRect.X) - pad);
                    int srcY = Math.Max(0, (currentWinY - virtualScreenRect.Y) - pad);
                    g.DrawImage(bg, new Rectangle(0, 0, outW, outH), new Rectangle(srcX, srcY, outW, outH), GraphicsUnit.Pixel);
                }

                using (Bitmap surf = new Bitmap(surfaceW, surfaceH, PixelFormat.Format32bppPArgb))
                {
                    BitmapData bd = surf.LockBits(new Rectangle(0, 0, surfaceW, surfaceH), ImageLockMode.WriteOnly, PixelFormat.Format32bppPArgb);
                    Marshal.Copy(surfaceBuffer, 0, bd.Scan0, surfaceBuffer.Length);
                    surf.UnlockBits(bd);
                    g.DrawImage(surf, pad, pad);
                }
                comp.Save(outPath, ImageFormat.Png);
            }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.Style |= 0x00020000; // WS_MINIMIZEBOX for native taskbar minimize/restore
                if (IsAppWindowModeEnabled())
                    cp.ExStyle |= WS_EX_LAYERED | WS_EX_APPWINDOW;
                else
                    cp.ExStyle |= WS_EX_LAYERED | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
                return cp;
            }
        }

        protected override bool ShowWithoutActivation
        {
            get { return !IsAppWindowModeEnabled(); }
        }

        protected override void OnPaintBackground(PaintEventArgs e) { }
        protected override void OnPaint(PaintEventArgs e) { }

        private void BuildContextMenu()
        {
            contextMenu = new ContextMenuStrip();
            contextMenu.ShowImageMargin = false;

            var titleItem = new ToolStripMenuItem("Liquid Glass Birthdays") { Enabled = false };
            contextMenu.Items.Add(titleItem);
            contextMenu.Items.Add(new ToolStripSeparator());

            contextMenu.Items.Add("+ Add Person", null, (s, e) => StartInGlassPersonEditor(null));
            contextMenu.Items.Add("Settings", null, (s, e) => {
                if (currentView == ActiveView.Settings) CancelInGlassSettings();
                else OpenInGlassSettings();
            });

            contextMenu.Items.Add(new ToolStripSeparator());
            var exitItem = new ToolStripMenuItem("Close Widget", null, (s, e) => Application.Exit());
            contextMenu.Items.Add(exitItem);
        }

        // Thread-Safe Atomic Resize (Acquires both renderLock and uiLock so changing widget size NEVER breaks or desyncs strides!)
        private void ResizeWidget(int w, int h, float radius, bool saveConfig = true)
        {
            lock (renderLock)
            {
                lock (uiLock)
                {
                    widgetWidth = w;
                    widgetHeight = h;
                    cornerRadius = radius;
                    surfaceW = widgetWidth + edgeMargin * 2;
                    surfaceH = widgetHeight + edgeMargin * 2;
                    this.Size = new Size(surfaceW, surfaceH);
                    AllocatePersistentDIB();
                    PrecomputeOpticalTables();
                    RebuildUIOverlay(0.016f);
                    if (saveConfig) SaveConfig();
                    Interlocked.Exchange(ref dirtyFrame, 1);
                }
            }
            RenderLiquidGlassFrame();
        }

        private void PinInFrontOfRainmeterClock()
        {
            try
            {
                if (config == null || config.AppWindowMode) return;

                // 1. Always On Top override
                if (config.LayerAlwaysOnTop)
                {
                    SetWindowPos(this.Handle, HWND_TOPMOST, 0, 0, 0, 0,
                        SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
                    return;
                }

                // Ensure not stuck in topmost (only when actually topmost — avoids redundant WM_WINDOWPOSCHANGING every 1.5s!)
                if ((GetWindowLong(this.Handle, -20) & 0x00000008) != 0)
                {
                    SetWindowPos(this.Handle, HWND_NOTOPMOST, 0, 0, 0, 0,
                        SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
                }

                bool isBelowMode = string.Equals(config.LayerMode, "Below", StringComparison.OrdinalIgnoreCase);
                HashSet<string> targetExes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (config.LayerTargetExes != null)
                {
                    foreach (string exe in config.LayerTargetExes)
                    {
                        if (!string.IsNullOrWhiteSpace(exe))
                            targetExes.Add(Path.GetFileNameWithoutExtension(exe.Trim()));
                    }
                }

                List<IntPtr> matchedTargets = new List<IntPtr>();
                List<IntPtr> rainWindows = new List<IntPtr>();

                EnumWindows((h, l) => {
                    if (h == this.Handle || !IsWindowVisible(h)) return true;

                    StringBuilder cls = new StringBuilder(128);
                    GetClassName(h, cls, 128);
                    string clsName = cls.ToString();

                    if (clsName == "RainmeterMeterWindow")
                    {
                        rainWindows.Add(h);
                        if (config.LayerRainmeter)
                        {
                            matchedTargets.Add(h);
                            return true;
                        }
                    }

                    if (config.LayerDesktopIcons && (clsName == "Progman" || clsName == "WorkerW"))
                    {
                        if (FindWindowEx(h, IntPtr.Zero, "SHELLDLL_DefView", null) != IntPtr.Zero)
                        {
                            matchedTargets.Add(h);
                            return true;
                        }
                    }

                    if (targetExes.Count > 0)
                    {
                        uint pid = 0;
                        GetWindowThreadProcessId(h, out pid);
                        if (pid > 0)
                        {
                            try
                            {
                                using (Process p = Process.GetProcessById((int)pid))
                                {
                                    if (targetExes.Contains(p.ProcessName))
                                    {
                                        matchedTargets.Add(h);
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                    return true;
                }, IntPtr.Zero);

                if (matchedTargets.Count > 0)
                {
                    if (!isBelowMode)
                    {
                        // ABOVE mode: stay just above the highest-Z matched target (never re-orders other apps' windows!)
                        IntPtr topTarget = matchedTargets[0];
                        IntPtr prevAbove = GetWindow(topTarget, GW_HWNDPREV);
                        if (prevAbove != IntPtr.Zero && prevAbove != this.Handle)
                        {
                            SetWindowPos(this.Handle, prevAbove, 0, 0, 0, 0,
                                SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
                        }
                        else if (prevAbove == IntPtr.Zero)
                        {
                            SetWindowPos(this.Handle, IntPtr.Zero, 0, 0, 0, 0,
                                SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
                        }
                    }
                    else
                    {
                        // BELOW mode: stay immediately below the lowest-Z matched target
                        IntPtr bottomTarget = matchedTargets[matchedTargets.Count - 1];
                        SetWindowPos(this.Handle, bottomTarget, 0, 0, 0, 0,
                            SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
                    }
                }
                else
                {
                    SetWindowPos(this.Handle, HWND_BOTTOM, 0, 0, 0, 0,
                        SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
                }
            }
            catch { }
        }

        private int[] ExtractPixels(Bitmap bmp)
        {
            int w = bmp.Width;
            int h = bmp.Height;
            int[] pixels = new int[w * h];
            BitmapData bd = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            Marshal.Copy(bd.Scan0, pixels, 0, pixels.Length);
            bmp.UnlockBits(bd);
            return pixels;
        }

        private void BoxBlurHorizontal(int[] src, int[] dst, int w, int h, int r)
        {
            float iarr = 1f / (r + r + 1);
            for (int y = 0; y < h; y++)
            {
                int ti = y * w;
                int li = ti;
                int ri = ti + r;
                int fv = src[ti];
                int lv = src[ti + w - 1];

                int fR = (fv >> 16) & 0xFF, fG = (fv >> 8) & 0xFF, fB = fv & 0xFF;
                int lR = (lv >> 16) & 0xFF, lG = (lv >> 8) & 0xFF, lB = lv & 0xFF;

                int valR = (r + 1) * fR;
                int valG = (r + 1) * fG;
                int valB = (r + 1) * fB;

                for (int j = 0; j < r; j++)
                {
                    int c = src[ti + Math.Min(w - 1, j)];
                    valR += (c >> 16) & 0xFF;
                    valG += (c >> 8) & 0xFF;
                    valB += c & 0xFF;
                }

                for (int j = 0; j <= r; j++)
                {
                    int c = src[ti + Math.Min(w - 1, ri - ti)];
                    valR += ((c >> 16) & 0xFF) - fR;
                    valG += ((c >> 8) & 0xFF) - fG;
                    valB += (c & 0xFF) - fB;
                    dst[ti++] = (0xFF << 24) | ((int)(valR * iarr) << 16) | ((int)(valG * iarr) << 8) | (int)(valB * iarr);
                    ri++;
                }

                for (int j = r + 1; j < w - r; j++)
                {
                    int cAdd = src[ri++];
                    int cSub = src[li++];
                    valR += ((cAdd >> 16) & 0xFF) - ((cSub >> 16) & 0xFF);
                    valG += ((cAdd >> 8) & 0xFF) - ((cSub >> 8) & 0xFF);
                    valB += (cAdd & 0xFF) - (cSub & 0xFF);
                    dst[ti++] = (0xFF << 24) | ((int)(valR * iarr) << 16) | ((int)(valG * iarr) << 8) | (int)(valB * iarr);
                }

                for (int j = w - r; j < w; j++)
                {
                    int cSub = src[li++];
                    valR += lR - ((cSub >> 16) & 0xFF);
                    valG += lG - ((cSub >> 8) & 0xFF);
                    valB += lB - (cSub & 0xFF);
                    dst[ti++] = (0xFF << 24) | ((int)(valR * iarr) << 16) | ((int)(valG * iarr) << 8) | (int)(valB * iarr);
                }
            }
        }

        private void BoxBlurVertical(int[] src, int[] dst, int w, int h, int r)
        {
            float iarr = 1f / (r + r + 1);
            for (int x = 0; x < w; x++)
            {
                int ti = x;
                int li = ti;
                int ri = ti + r * w;
                int fv = src[ti];
                int lv = src[ti + w * (h - 1)];

                int fR = (fv >> 16) & 0xFF, fG = (fv >> 8) & 0xFF, fB = fv & 0xFF;
                int lR = (lv >> 16) & 0xFF, lG = (lv >> 8) & 0xFF, lB = lv & 0xFF;

                int valR = (r + 1) * fR;
                int valG = (r + 1) * fG;
                int valB = (r + 1) * fB;

                for (int j = 0; j < r; j++)
                {
                    int c = src[ti + Math.Min(h - 1, j) * w];
                    valR += (c >> 16) & 0xFF;
                    valG += (c >> 8) & 0xFF;
                    valB += c & 0xFF;
                }

                for (int j = 0; j <= r; j++)
                {
                    int row = Math.Min(h - 1, j + r);
                    int c = src[x + row * w];
                    valR += ((c >> 16) & 0xFF) - fR;
                    valG += ((c >> 8) & 0xFF) - fG;
                    valB += (c & 0xFF) - fB;
                    dst[ti] = (0xFF << 24) | ((int)(valR * iarr) << 16) | ((int)(valG * iarr) << 8) | (int)(valB * iarr);
                    ti += w;
                    ri += w;
                }

                for (int j = r + 1; j < h - r; j++)
                {
                    int cAdd = src[ri];
                    int cSub = src[li];
                    valR += ((cAdd >> 16) & 0xFF) - ((cSub >> 16) & 0xFF);
                    valG += ((cAdd >> 8) & 0xFF) - ((cSub >> 8) & 0xFF);
                    valB += (cAdd & 0xFF) - (cSub & 0xFF);
                    dst[ti] = (0xFF << 24) | ((int)(valR * iarr) << 16) | ((int)(valG * iarr) << 8) | (int)(valB * iarr);
                    li += w;
                    ri += w;
                    ti += w;
                }

                for (int j = h - r; j < h; j++)
                {
                    int cSub = src[li];
                    valR += lR - ((cSub >> 16) & 0xFF);
                    valG += lG - ((cSub >> 8) & 0xFF);
                    valB += lB - (cSub & 0xFF);
                    dst[ti] = (0xFF << 24) | ((int)(valR * iarr) << 16) | ((int)(valG * iarr) << 8) | (int)(valB * iarr);
                    li += w;
                    ti += w;
                }
            }
        }

        private float RoundedBoxDistanceOnly(float px, float py, float halfW, float halfH, float r)
        {
            float dx = Math.Abs(px) - (halfW - r);
            float dy = Math.Abs(py) - (halfH - r);
            float maxDx = dx > 0f ? dx : 0f;
            float maxDy = dy > 0f ? dy : 0f;
            float outLen = (float)Math.Sqrt(maxDx * maxDx + maxDy * maxDy);
            float inDist = (dx > dy ? dx : dy);
            if (inDist > 0f) inDist = 0f;
            return outLen + inDist - r;
        }

        private float RoundedBoxSDF(float px, float py, float halfW, float halfH, float r, out float gradX, out float gradY)
        {
            float dx = Math.Abs(px) - (halfW - r);
            float dy = Math.Abs(py) - (halfH - r);

            float maxDx = Math.Max(dx, 0f);
            float maxDy = Math.Max(dy, 0f);
            float outLen = (float)Math.Sqrt(maxDx * maxDx + maxDy * maxDy);
            float inDist = Math.Min(Math.Max(dx, dy), 0f);
            float sdf = outLen + inDist - r;

            float sx = px >= 0f ? 1f : -1f;
            float sy = py >= 0f ? 1f : -1f;

            float k = 0.16f;
            float sDx = (float)Math.Log(1.0 + Math.Exp(dx * k)) / k;
            float sDy = (float)Math.Log(1.0 + Math.Exp(dy * k)) / k;

            float gx = sx * sDx;
            float gy = sy * sDy;
            float gLen = (float)Math.Sqrt(gx * gx + gy * gy) + 1e-6f;
            gradX = gx / gLen;
            gradY = gy / gLen;

            return sdf;
        }

        private void PrecomputeOpticalTables()
        {
            lock (renderLock)
            {
                PrecomputeOpticalTablesCore();
            }
        }

        private void PrecomputeOpticalTablesCore()
        {
            int curSurfW = surfaceW;
            int curSurfH = surfaceH;
            surfaceBuffer = new int[curSurfW * curSurfH];
            FastPixelLUT[][] newRowLUTs = new FastPixelLUT[curSurfH][];

            float cx = curSurfW * 0.5f;
            float cy = curSurfH * 0.5f;
            float halfW = widgetWidth * 0.5f;
            float halfH = widgetHeight * 0.5f;
            float effRadius = IsBasicThemeEnabled() ? 10f : cornerRadius;
            float bevelWidth = IsBasicThemeEnabled() ? 12.0f : 42.0f;

            float gizmoCx = edgeMargin + gizmoOffsetX;
            float gizmoCy = edgeMargin + gizmoOffsetY;

            float l1x = -0.60f, l1y = -0.66f, l1z = 0.45f;
            float l2x = 0.58f,  l2y = 0.58f,  l2z = 0.57f;

            float h1x = l1x, h1y = l1y, h1z = l1z + 1f;
            float h1Len = 1f / (float)Math.Sqrt(h1x * h1x + h1y * h1y + h1z * h1z);
            h1x *= h1Len; h1y *= h1Len; h1z *= h1Len;

            float h2x = l2x, h2y = l2y, h2z = l2z + 1f;
            float h2Len = 1f / (float)Math.Sqrt(h2x * h2x + h2y * h2y + h2z * h2z);
            h2x *= h2Len; h2y *= h2Len; h2z *= h2Len;

            Parallel.For(0, curSurfH, y =>
            {
                float py = y - cy;
                List<FastPixelLUT> rowList = new List<FastPixelLUT>(curSurfW);

                for (int x = 0; x < curSurfW; x++)
                {
                    int idx = y * curSurfW + x;
                    float px = x - cx;

                    float gx, gy;
                    float sdf = RoundedBoxSDF(px, py, halfW, halfH, effRadius, out gx, out gy);

                    if (sdf < 1.75f)
                    {
                        float cov;
                        if (sdf < -1.5f)
                        {
                            cov = 1.0f;
                        }
                        else
                        {
                            float accum = 0f;
                            for (int sy = 0; sy < 4; sy++)
                            {
                                float subY = py + (sy - 1.5f) * 0.25f;
                                for (int sx = 0; sx < 4; sx++)
                                {
                                    float subX = px + (sx - 1.5f) * 0.25f;
                                    float subSdf = RoundedBoxDistanceOnly(subX, subY, halfW, halfH, effRadius);
                                    float sc = Clamp01(0.5f - subSdf * 0.92f);
                                    sc = sc * sc * (3f - 2f * sc);
                                    accum += sc;
                                }
                            }
                            cov = accum * (1f / 16f);
                        }

                        int alpha255 = (int)(cov * 255f + 0.5f);
                        if (alpha255 <= 0) continue;

                        float inwardDist = Math.Max(0f, -sdf);

                        float edgeRatio = Clamp01(1f - inwardDist / bevelWidth);
                        float meniscus = edgeRatio * edgeRatio * edgeRatio * (edgeRatio * (edgeRatio * 6f - 15f) + 10f);

                        float globalDomeX = (px / halfW) * 0.06f;
                        float globalDomeY = (py / halfH) * 0.06f;

                        float slope = meniscus * 0.85f;
                        float nx = gx * slope + globalDomeX * (1f - meniscus);
                        float ny = gy * slope + globalDomeY * (1f - meniscus);
                        float nzSq = Math.Max(0.04f, 1f - (nx * nx + ny * ny));
                        float nz = (float)Math.Sqrt(nzSq);

                        float invLen = 1f / (float)Math.Sqrt(nx * nx + ny * ny + nz * nz);
                        nx *= invLen;
                        ny *= invLen;
                        nz *= invLen;

                        float oneMinusCos = 1f - nz;
                        float oneMinusCos2 = oneMinusCos * oneMinusCos;
                        float fresnel = Clamp01((0.01f + 0.99f * (oneMinusCos2 * oneMinusCos2 * oneMinusCos)) * fresnelIntensity);

                        float ndoth1 = Math.Max(0f, nx * h1x + ny * h1y + nz * h1z);
                        float ndoth2 = Math.Max(0f, nx * h2x + ny * h2y + nz * h2z);

                        float spec1 = (float)Math.Pow(ndoth1, 18f) * 0.34f + (float)Math.Pow(ndoth1, 6f) * 0.10f;
                        float spec2 = (float)Math.Pow(ndoth2, 16f) * 0.18f;
                        float bevelHighlight = Clamp01((spec1 + spec2) * meniscus);

                        float causticDist = inwardDist - 8.0f;
                        float innerCaustic = Clamp01((float)Math.Exp(-(causticDist * causticDist) * 0.08f) * 0.09f * (0.45f + 0.55f * Math.Max(0f, gx * 0.5f + gy * 0.7f)));

                        float grooveDist = inwardDist - 3.5f;
                        float innerDepthRim = Clamp01((float)Math.Exp(-(grooveDist * grooveDist) * 0.30f) * 0.14f);

                        float bodySheen = Clamp01((-py / halfH) * 0.5f + (-px / halfW) * 0.2f + 0.12f);
                        bodySheen = bodySheen * bodySheen * 0.035f * (1f - meniscus * 0.5f);

                        float lensBoost = 1f + (1f - nz) * 2.65f;
                        int dispX = (int)Math.Round(-nx * refractionStrength * lensBoost);
                        int dispY = (int)Math.Round(-ny * refractionStrength * lensBoost);

                        int caX = (int)Math.Round(-nx * chromaticStrength * (0.65f + (1f - nz) * 1.55f));
                        int caY = (int)Math.Round(-ny * chromaticStrength * (0.65f + (1f - nz) * 1.55f));

                        float rimClarity = Clamp01((1f - nz) * 1.95f);
                        float glassBodyTint = 0.03f + innerCaustic + bodySheen;
                        float specular = Clamp01(bevelHighlight + fresnel * 0.16f);

                        FastPixelLUT fp = new FastPixelLUT();
                        fp.index = idx;
                        fp.px = x;
                        fp.py = y;
                        fp.dispX = dispX;
                        fp.dispY = dispY;
                        fp.caX = caX;
                        fp.caY = caY;
                        fp.rimClarity256 = (int)(rimClarity * 256f);
                        fp.absorb256 = (int)((1f - innerDepthRim) * 256f);
                        fp.tintWeight256 = (int)(Clamp01(glassBodyTint * 0.45f) * 256f);
                        fp.specWeight256 = (int)(Clamp01(specular * 0.45f) * 256f);
                        fp.alpha255 = alpha255;
                        fp.isEdge = alpha255 < 255;

                        float gdx = x - gizmoCx;
                        float gdy = y - gizmoCy;
                        float pillSdf = RoundedBoxDistanceOnly(gdx, gdy, gizmoHalfW, gizmoHalfH, gizmoHalfH);
                        if (pillSdf <= 1.5f)
                        {
                            fp.isGizmoZone = true;

                            float pillMask = Clamp01(0.5f - pillSdf * 0.85f);

                            float colX = Math.Abs(gdx);
                            if (colX > 2.5f) colX = Math.Abs(colX - 5.0f);
                            float rowY = Math.Abs(gdy) - 2.3f;

                            float dotBase = 0f;
                            if (Math.Abs(gdx) <= 7.5f && Math.Abs(gdy) <= 4.8f)
                            {
                                float dotDist = (float)Math.Sqrt(colX * colX + rowY * rowY);
                                float dm = Clamp01((1.45f - dotDist) * 1.4f);
                                dotBase = dm * dm * (3f - 2f * dm) * 0.72f;
                            }

                            float hoverPillGlow = pillMask * 0.14f;
                            fp.gizmoLightNormal = (byte)(Clamp01(dotBase) * 255f);
                            fp.gizmoLightHover  = (byte)(Clamp01(Math.Max(dotBase * 1.35f, hoverPillGlow)) * 255f);
                        }

                        rowList.Add(fp);
                    }
                }
                newRowLUTs[y] = rowList.ToArray();
            });

            rowLUTs = newRowLUTs;
        }

        private static float Clamp01(float v)
        {
            if (v < 0f) return 0f;
            if (v > 1f) return 1f;
            return v;
        }

        private void RenderLiquidGlassFrame()
        {
            lock (renderLock)
            {
                FastPixelLUT[][] currentRows = rowLUTs;
                int[] blurBuf = blurredPixels;
                int[] sharpBuf = sharpPixels;
                int[] uiBuf = uiOverlayPixels;
                int[] outBuf = surfaceBuffer;
                if (currentRows == null || blurBuf == null || sharpBuf == null || outBuf == null) return;

                int snapWinX = currentWinX;
                int snapWinY = currentWinY;
                int winOffsetX = snapWinX - virtualScreenRect.X;
                int winOffsetY = snapWinY - virtualScreenRect.Y;
                int maxW = deskW - 1;
                int maxH = deskH - 1;
                int dw = deskW;
                bool gizmoActive = isDragging || isGizmoHovered;
                int h = currentRows.Length;
                int outLen = outBuf.Length;

                int optTintR, optTintG, optTintB, optTintExtraW;
                GetOpticalTintRgb(out optTintR, out optTintG, out optTintB, out optTintExtraW);

                int brightGlassDark256 = GetBrightBgGlassDarken256();
                int invBrightDark256 = 256 - brightGlassDark256;
                bool basicTheme = IsBasicThemeEnabled();
                bool darkMode = IsDarkModeEnabled();
                string activeTint = GetActiveTint();
                int curBgSeq = Volatile.Read(ref backdropFrameSeq);
                int curPreset = (currentView == ActiveView.Settings) ? stagedPresetIndex : (config != null ? config.GlassPresetIndex : 0);

                if (cachedGlassBasePixels == null || cachedGlassBasePixels.Length != outLen)
                {
                    cachedGlassBasePixels = new int[outLen];
                    lastGlassBaseBgSeq = -1;
                }

                bool needRebakeGlassBase = (curBgSeq != lastGlassBaseBgSeq)
                    || (snapWinX != lastGlassBaseWinX)
                    || (snapWinY != lastGlassBaseWinY)
                    || (basicTheme != lastGlassBaseBasic)
                    || (darkMode != lastGlassBaseDark)
                    || (activeTint != lastGlassBaseTint)
                    || (gizmoActive != lastGlassBaseGizmo)
                    || (curPreset != lastGlassBasePreset);

                int[] baseBuf = cachedGlassBasePixels;

                if (needRebakeGlassBase)
                {
                    Color themeRimCol = GetThemeRimColor(255);
                    int gzR = themeRimCol.R, gzG = themeRimCol.G, gzB = themeRimCol.B;

                    Parallel.For(0, h, y =>
                    {
                        FastPixelLUT[] row = currentRows[y];
                        if (row == null) return;
                        int rowLen = row.Length;
                        int gradShift = (y * 10) / Math.Max(1, h);

                        for (int i = 0; i < rowLen; i++)
                        {
                            FastPixelLUT fp = row[i];
                            if (fp.index >= outLen) continue;

                            int r, g, b;
                            if (basicTheme)
                            {
                                if (!darkMode)
                                {
                                    r = 236 - (gradShift * 4) / 3;
                                    g = 240 - (gradShift * 4) / 3;
                                    b = 248 - (gradShift * 3) / 2;
                                    if (optTintExtraW > 0)
                                    {
                                        int tw = optTintR < 40 ? 18 : optTintExtraW;
                                        r = ((r * (256 - tw)) + (optTintR * tw)) >> 8;
                                        g = ((g * (256 - tw)) + (optTintG * tw)) >> 8;
                                        b = ((b * (256 - tw)) + (optTintB * tw)) >> 8;
                                    }
                                    int rimW = fp.rimClarity256 / 3;
                                    if (rimW > 6)
                                    {
                                        int rr = optTintExtraW > 0 && optTintR >= 40 ? ((175 + optTintR) / 2) : 175;
                                        int rg = optTintExtraW > 0 && optTintR >= 40 ? ((190 + optTintG) / 2) : 190;
                                        int rb = optTintExtraW > 0 && optTintR >= 40 ? ((218 + optTintB) / 2) : 218;
                                        r = ((r * (256 - rimW)) + (rr * rimW)) >> 8;
                                        g = ((g * (256 - rimW)) + (rg * rimW)) >> 8;
                                        b = ((b * (256 - rimW)) + (rb * rimW)) >> 8;
                                    }
                                }
                                else
                                {
                                    r = 20 - (gradShift * 2) / 3;
                                    g = 25 - (gradShift * 2) / 3;
                                    b = 37 - gradShift;
                                    if (optTintExtraW > 0)
                                    {
                                        int tw = (optTintExtraW * 2) / 3;
                                        r = ((r * (256 - tw)) + (optTintR * tw)) >> 8;
                                        g = ((g * (256 - tw)) + (optTintG * tw)) >> 8;
                                        b = ((b * (256 - tw)) + (optTintB * tw)) >> 8;
                                    }
                                    int rimW = fp.rimClarity256 / 3;
                                    if (rimW > 6)
                                    {
                                        int rr = optTintExtraW > 0 ? ((125 + optTintR) / 2) : 120;
                                        int rg = optTintExtraW > 0 ? ((140 + optTintG) / 2) : 138;
                                        int rb = optTintExtraW > 0 ? ((175 + optTintB) / 2) : 175;
                                        r = ((r * (256 - rimW)) + (rr * rimW)) >> 8;
                                        g = ((g * (256 - rimW)) + (rg * rimW)) >> 8;
                                        b = ((b * (256 - rimW)) + (rb * rimW)) >> 8;
                                    }
                                }
                            }
                            else
                            {
                                int bx = winOffsetX + fp.px + fp.dispX;
                                int by = winOffsetY + fp.py + fp.dispY;

                                int xR = bx - fp.caX; if (xR < 0) xR = 0; else if (xR > maxW) xR = maxW;
                                int yR = by - fp.caY; if (yR < 0) yR = 0; else if (yR > maxH) yR = maxH;

                                int xG = bx;          if (xG < 0) xG = 0; else if (xG > maxW) xG = maxW;
                                int yG = by;          if (yG < 0) yG = 0; else if (yG > maxH) yG = maxH;

                                int xB = bx + fp.caX; if (xB < 0) xB = 0; else if (xB > maxW) xB = maxW;
                                int yB = by + fp.caY; if (yB < 0) yB = 0; else if (yB > maxH) yB = maxH;

                                int iR = yR * dw + xR;
                                int iG = yG * dw + xG;
                                int iB = yB * dw + xB;

                                r = (blurBuf[iR] >> 16) & 0xFF;
                                g = (blurBuf[iG] >> 8) & 0xFF;
                                b = blurBuf[iB] & 0xFF;

                                int rimW = fp.rimClarity256;
                                if (rimW > 4)
                                {
                                    int sr = (sharpBuf[iR] >> 16) & 0xFF;
                                    int sg = (sharpBuf[iG] >> 8) & 0xFF;
                                    int sb = sharpBuf[iB] & 0xFF;

                                    r = ((r * (256 - rimW)) + (sr * rimW)) >> 8;
                                    g = ((g * (256 - rimW)) + (sg * rimW)) >> 8;
                                    b = ((b * (256 - rimW)) + (sb * rimW)) >> 8;
                                }

                                int luma = (77 * r + 150 * g + 29 * b) >> 8;
                                r = luma + (((r - luma) * 307) >> 8);
                                g = luma + (((g - luma) * 307) >> 8);
                                b = luma + (((b - luma) * 307) >> 8);

                                int absW = fp.absorb256;
                                if (absW < 255)
                                {
                                    r = (r * absW) >> 8;
                                    g = (g * absW) >> 8;
                                    b = (b * Math.Min(256, absW + 6)) >> 8;
                                }

                                if (brightGlassDark256 > 0)
                                {
                                    r = ((r * invBrightDark256) + (20 * brightGlassDark256)) >> 8;
                                    g = ((g * invBrightDark256) + (25 * brightGlassDark256)) >> 8;
                                    b = ((b * invBrightDark256) + (36 * brightGlassDark256)) >> 8;
                                }

                                int tintW = fp.tintWeight256;
                                if (tintW > 0)
                                {
                                    int tr = optTintExtraW > 0 ? ((245 + optTintR) >> 1) : 245;
                                    int tg = optTintExtraW > 0 ? ((250 + optTintG) >> 1) : 250;
                                    int tb = optTintExtraW > 0 ? ((255 + optTintB) >> 1) : 255;
                                    r = ((r * (256 - tintW)) + (tr * tintW)) >> 8;
                                    g = ((g * (256 - tintW)) + (tg * tintW)) >> 8;
                                    b = ((b * (256 - tintW)) + (tb * tintW)) >> 8;
                                }

                                if (optTintExtraW > 0)
                                {
                                    int invTw = 256 - optTintExtraW;
                                    r = ((r * invTw) + (optTintR * optTintExtraW)) >> 8;
                                    g = ((g * invTw) + (optTintG * optTintExtraW)) >> 8;
                                    b = ((b * invTw) + (optTintB * optTintExtraW)) >> 8;
                                }

                                int specW = fp.specWeight256;
                                if (specW > 0)
                                {
                                    int sr = optTintExtraW > 0 && optTintR >= 40 ? ((250 * 3 + optTintR) >> 2) : 250;
                                    int sg = optTintExtraW > 0 && optTintR >= 40 ? ((252 * 3 + optTintG) >> 2) : 252;
                                    int sb = optTintExtraW > 0 && optTintR >= 40 ? ((255 * 3 + optTintB) >> 2) : 255;
                                    r = ((r * (256 - specW)) + (sr * specW)) >> 8;
                                    g = ((g * (256 - specW)) + (sg * specW)) >> 8;
                                    b = ((b * (256 - specW)) + (sb * specW)) >> 8;
                                }
                            }

                            if (fp.isGizmoZone)
                            {
                                int lightW = gizmoActive ? fp.gizmoLightHover : fp.gizmoLightNormal;
                                if (lightW > 0)
                                {
                                    r = ((r * (256 - lightW)) + (gzR * lightW)) >> 8;
                                    g = ((g * (256 - lightW)) + (gzG * lightW)) >> 8;
                                    b = ((b * (256 - lightW)) + (gzB * lightW)) >> 8;
                                }
                            }

                            if (r > 255) r = 255; else if (r < 0) r = 0;
                            if (g > 255) g = 255; else if (g < 0) g = 0;
                            if (b > 255) b = 255; else if (b < 0) b = 0;

                            baseBuf[fp.index] = (r << 16) | (g << 8) | b;
                        }
                    });

                    lastGlassBaseBgSeq = curBgSeq;
                    lastGlassBaseWinX = snapWinX;
                    lastGlassBaseWinY = snapWinY;
                    lastGlassBaseBasic = basicTheme;
                    lastGlassBaseDark = darkMode;
                    lastGlassBaseTint = activeTint;
                    lastGlassBaseGizmo = gizmoActive;
                    lastGlassBasePreset = curPreset;
                }

                // Ultra-fast UI Overlay Composite over pre-baked Glass Base (~0.3ms using bit-shifts!)
                Parallel.For(0, h, y =>
                {
                    FastPixelLUT[] row = currentRows[y];
                    if (row == null) return;
                    int rowLen = row.Length;

                    for (int i = 0; i < rowLen; i++)
                    {
                        FastPixelLUT fp = row[i];
                        int idx = fp.index;
                        if (idx >= outLen) continue;

                        int baseCol = baseBuf[idx];
                        int r = (baseCol >> 16) & 0xFF;
                        int g = (baseCol >> 8) & 0xFF;
                        int b = baseCol & 0xFF;

                        if (uiBuf != null && idx < uiBuf.Length)
                        {
                            int uiPx = uiBuf[idx];
                            int uiA = (uiPx >> 24) & 0xFF;
                            if (uiA > 0)
                            {
                                int invUiA = 256 - uiA;
                                r = ((uiPx >> 16) & 0xFF) + ((r * invUiA) >> 8);
                                g = ((uiPx >> 8) & 0xFF) + ((g * invUiA) >> 8);
                                b = (uiPx & 0xFF) + ((b * invUiA) >> 8);
                                if (r > 255) r = 255;
                                if (g > 255) g = 255;
                                if (b > 255) b = 255;
                            }
                        }

                        if (!fp.isEdge)
                        {
                            outBuf[idx] = (255 << 24) | (r << 16) | (g << 8) | b;
                        }
                        else
                        {
                            int a = fp.alpha255;
                            int a256 = a + 1;
                            outBuf[idx] = (a << 24) | (((r * a256) >> 8) << 16) | (((g * a256) >> 8) << 8) | ((b * a256) >> 8);
                        }
                    }
                });

                CommitLayeredWindowFast(snapWinX, snapWinY);
            }
        }

        private void CommitLayeredWindowFast(int targetX, int targetY)
        {
            IntPtr myHwnd = cachedMyHwnd;
            if (myHwnd == IntPtr.Zero || IsIconic(myHwnd) || persistentBitsPtr == IntPtr.Zero || surfaceBuffer == null) return;

            Marshal.Copy(surfaceBuffer, 0, persistentBitsPtr, surfaceBuffer.Length);

            SIZE size = new SIZE(surfaceW, surfaceH);
            POINT pointSource = new POINT(0, 0);
            POINT topPos = new POINT(targetX, targetY);

            BLENDFUNCTION blend = new BLENDFUNCTION();
            blend.BlendOp = AC_SRC_OVER;
            blend.BlendFlags = 0;
            blend.SourceConstantAlpha = 255;
            blend.AlphaFormat = AC_SRC_ALPHA;

            UpdateLayeredWindow(myHwnd, persistentScreenDc, ref topPos, ref size, persistentMemDc, ref pointSource, 0, ref blend, ULW_ALPHA);
        }

        private bool IsPointOnMoveGizmo(int mouseX, int mouseY)
        {
            float gizmoCx = edgeMargin + gizmoOffsetX;
            float gizmoCy = edgeMargin + gizmoOffsetY;
            float dx = Math.Abs(mouseX - gizmoCx);
            float dy = Math.Abs(mouseY - gizmoCy);
            return dx <= (gizmoHalfW + 8f) && dy <= (gizmoHalfH + 8f);
        }

        private HitZone? FindHitZone(int x, int y)
        {
            List<HitZone> zones = hitZones;
            if (zones == null) return null;
            // Reverse order so topmost/overlay hit zones (like search_clear ×, dropdowns, context menu) always win!
            for (int i = zones.Count - 1; i >= 0; i--)
            {
                if (zones[i].Bounds.Contains(x, y)) return zones[i];
            }
            return null;
        }

        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            bool changed = false;
            if (isSearchFocused) { isSearchFocused = false; changed = true; }
            if (isLayerSearchFocused) { isLayerSearchFocused = false; changed = true; }
            if (isContextMenuOpen) { isContextMenuOpen = false; changed = true; }
            if (isFilterDropdownOpen) { isFilterDropdownOpen = false; changed = true; }
            if (changed)
            {
                if (currentView != ActiveView.AddOrEditPerson) SetKeyboardActivationEnabled(false);
                Interlocked.Exchange(ref dirtyFrame, 1);
            }
        }

        private void StartDrag()
        {
            dragStartCursor = Cursor.Position;
            dragStartLocation = new Point(currentWinX, currentWinY);
            this.Capture = true;
            isDragging = true;
            needFullScreenCapture = true;
            this.Cursor = curGlassGrabbing ?? Cursors.SizeAll;
            Interlocked.Exchange(ref dirtyFrame, 1);
        }

        private void EndDrag()
        {
            if (!isDragging) return;
            isDragging = false;
            this.Capture = false;
            this.Location = new Point(currentWinX, currentWinY);
            this.Cursor = isGizmoHovered ? (curGlassGrab ?? Cursors.SizeAll) : (curGlassDefault ?? Cursors.Default);
            SaveConfig();
            if (currentView == ActiveView.Dashboard && !isSearchFocused) PinInFrontOfRainmeterClock();
            needFullScreenCapture = true;
            CheckAndStartSnapBackToScreen(false);
            MarkDirty();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            currentWinX = this.Left;
            currentWinY = this.Top;
            this.Cursor = curGlassDefault ?? Cursors.Default;
            ApplyWindowModeStyle();
            CheckAndStartSnapBackToScreen(false);
            MarkDirty();
        }

        private int GetEditorMaxDaysInMonth()
        {
            int yr = 2024;
            int py;
            if (!string.IsNullOrWhiteSpace(editYearStr) && int.TryParse(editYearStr.Trim(), out py) && py >= 1900 && py <= 9999)
                yr = py;
            return DateTime.DaysInMonth(yr, Math.Max(1, Math.Min(12, editMonth)));
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            base.OnKeyPress(e);
            if (pendingDeletePerson != null)
            {
                if (e.KeyChar == 27)
                {
                    pendingDeletePerson = null;
                    MarkDirty();
                }
                return;
            }

            // Language Search Typing in Settings -> General Dropdown
            if (currentView == ActiveView.Settings && isLangDropdownOpen && isLangSearchFocused)
            {
                char lc = e.KeyChar;
                if (lc == '\b') { if (langSearchQuery.Length > 0) langSearchQuery = langSearchQuery.Substring(0, langSearchQuery.Length - 1); }
                else if (lc == 27) { isLangDropdownOpen = false; isLangSearchFocused = false; SetKeyboardActivationEnabled(false); }
                else if (!char.IsControl(lc) && langSearchQuery.Length < 18) { langSearchQuery += lc; }
                e.Handled = true;
                MarkDirty();
                return;
            }

            // Numeric Near-Days Input in Settings -> General
            if (currentView == ActiveView.Settings && isNearDaysFocused)
            {
                char nc = e.KeyChar;
                if (nc == '\b')
                {
                    if (stagedNearDaysText.Length > 0) stagedNearDaysText = stagedNearDaysText.Substring(0, stagedNearDaysText.Length - 1);
                }
                else if (nc == 27 || nc == '\r' || nc == '\n')
                {
                    int pv;
                    if (int.TryParse(stagedNearDaysText, out pv)) stagedUrgentDays = ClampInt(pv, 1, 30);
                    stagedNearDaysText = stagedUrgentDays.ToString();
                    isNearDaysFocused = false;
                    SetKeyboardActivationEnabled(false);
                }
                else if (char.IsDigit(nc) && stagedNearDaysText.Length < 2)
                {
                    stagedNearDaysText += nc;
                    int pv;
                    if (int.TryParse(stagedNearDaysText, out pv)) stagedUrgentDays = ClampInt(pv, 1, 30);
                }
                e.Handled = true;
                MarkDirty();
                return;
            }

            // Custom Window Size (W x H) Numeric Input in Settings -> Window
            if (currentView == ActiveView.Settings && !string.IsNullOrEmpty(customSizeFocus))
            {
                char sc = e.KeyChar;
                string curS = customSizeFocus == "w" ? customWText : customHText;
                if (sc == '\b')
                {
                    if (curS.Length > 0) curS = curS.Substring(0, curS.Length - 1);
                }
                else if (sc == 27 || sc == '\r' || sc == '\n')
                {
                    int pv;
                    if (customSizeFocus == "w")
                    {
                        if (int.TryParse(curS, out pv)) stagedWidth = ClampInt(pv, 560, 1600);
                        curS = stagedWidth.ToString();
                    }
                    else
                    {
                        if (int.TryParse(curS, out pv)) stagedHeight = ClampInt(pv, 380, 1000);
                        curS = stagedHeight.ToString();
                    }
                    customSizeFocus = "";
                    SetKeyboardActivationEnabled(false);
                }
                else if (char.IsDigit(sc) && curS.Length < 4)
                {
                    curS += sc;
                    int pv;
                    if (int.TryParse(curS, out pv))
                    {
                        if (customSizeFocus == "w" && pv >= 560 && pv <= 1600) stagedWidth = pv;
                        else if (customSizeFocus == "h" && pv >= 380 && pv <= 1000) stagedHeight = pv;
                    }
                }
                if (customSizeFocus == "w") customWText = curS;
                else if (customSizeFocus == "h") customHText = curS;
                e.Handled = true;
                MarkDirty();
                return;
            }

            // Live Search Bar Typing on Dashboard
            if (currentView == ActiveView.Dashboard && isSearchFocused)
            {
                char sc = e.KeyChar;
                if (sc == '\b')
                {
                    if (searchQuery.Length > 0) searchQuery = searchQuery.Substring(0, searchQuery.Length - 1);
                    listScrollOffset = 0;
                }
                else if (sc == 27 || sc == '\r' || sc == '\n')
                {
                    if (sc == 27) searchQuery = "";
                    isSearchFocused = false;
                    SetKeyboardActivationEnabled(false);
                }
                else if (!char.IsControl(sc) && searchQuery.Length < 26)
                {
                    searchQuery += sc;
                    listScrollOffset = 0;
                }
                MarkDirty();
                return;
            }

            // Live .EXE Search Bar Typing in Settings -> Layers
            if (currentView == ActiveView.Settings && currentSettingsCat == SettingsCategory.Layers && isLayerSearchFocused)
            {
                char lc = e.KeyChar;
                if (lc == '\b')
                {
                    if (layerSearchQuery.Length > 0) layerSearchQuery = layerSearchQuery.Substring(0, layerSearchQuery.Length - 1);
                    layerListScroll = 0;
                }
                else if (lc == 27 || lc == '\r' || lc == '\n')
                {
                    if (lc == 27) layerSearchQuery = "";
                    isLayerSearchFocused = false;
                    SetKeyboardActivationEnabled(false);
                }
                else if (!char.IsControl(lc) && layerSearchQuery.Length < 28)
                {
                    layerSearchQuery += lc;
                    layerListScroll = 0;
                }
                MarkDirty();
                return;
            }

            if (currentView != ActiveView.AddOrEditPerson) return;

            char c = e.KeyChar;
            if (c == '\b')
            {
                editorErrorMsg = "";
                if (activeTextField == "name" && editName.Length > 0)
                {
                    int cp = ClampInt(editCaretPos, 0, editName.Length);
                    if (cp > 0) { editName = editName.Remove(cp - 1, 1); editCaretPos = cp - 1; }
                }
                else if (activeTextField == "year" && editYearStr.Length > 0)
                {
                    int cp = ClampInt(editCaretPos, 0, editYearStr.Length);
                    if (cp > 0) { editYearStr = editYearStr.Remove(cp - 1, 1); editCaretPos = cp - 1; }
                }
                else if (activeTextField == "day" && editDayStr.Length > 0)
                {
                    int cp = ClampInt(editCaretPos, 0, editDayStr.Length);
                    if (cp > 0) { editDayStr = editDayStr.Remove(cp - 1, 1); editCaretPos = cp - 1; }
                    int d; if (int.TryParse(editDayStr, out d) && d >= 1) editDay = Math.Min(GetEditorMaxDaysInMonth(), d);
                }
                else if (activeTextField == "notes" && editNotes.Length > 0)
                {
                    int cp = ClampInt(editCaretPos, 0, editNotes.Length);
                    if (cp > 0) { editNotes = editNotes.Remove(cp - 1, 1); editCaretPos = cp - 1; }
                }
            }
            else if (c == '\r' || c == '\n')
            {
                SaveInGlassPersonEditor();
                return;
            }
            else if (c == 27)
            {
                currentView = ActiveView.Dashboard;
                TriggerViewFade();
                SetKeyboardActivationEnabled(false);
            }
            else if (!char.IsControl(c))
            {
                editorErrorMsg = "";
                if (activeTextField == "name" && editName.Length < 28)
                {
                    int cp = ClampInt(editCaretPos, 0, editName.Length);
                    editName = editName.Insert(cp, c.ToString());
                    editCaretPos = cp + 1;
                }
                else if (activeTextField == "year" && char.IsDigit(c) && editYearStr.Length < 4)
                {
                    int cp = ClampInt(editCaretPos, 0, editYearStr.Length);
                    editYearStr = editYearStr.Insert(cp, c.ToString());
                    editCaretPos = cp + 1;
                }
                else if (activeTextField == "day" && char.IsDigit(c))
                {
                    if (editDayStr.Length >= 2) { editDayStr = c.ToString(); editCaretPos = 1; }
                    else
                    {
                        int cp = ClampInt(editCaretPos, 0, editDayStr.Length);
                        editDayStr = editDayStr.Insert(cp, c.ToString());
                        editCaretPos = cp + 1;
                    }
                    int d;
                    if (int.TryParse(editDayStr, out d) && d >= 1)
                    {
                        int maxD = GetEditorMaxDaysInMonth();
                        editDay = Math.Min(maxD, d);
                        if (d > maxD) { editDayStr = maxD.ToString("00"); editCaretPos = editDayStr.Length; }
                    }
                }
                else if (activeTextField == "notes" && editNotes.Length < 10000)
                {
                    int cp = ClampInt(editCaretPos, 0, editNotes.Length);
                    editNotes = editNotes.Insert(cp, c.ToString());
                    editCaretPos = cp + 1;
                }
            }

            MarkDirty();
        }

        private string DeleteLastWord(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            string trimmed = s.TrimEnd();
            int idx = trimmed.LastIndexOf(' ');
            return idx >= 0 ? trimmed.Substring(0, idx) : "";
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            // Global Esc priority ladder: closes any open popup, modal, search, editor, or settings cleanly!
            if (e.KeyCode == Keys.Escape)
            {
                if (isContextMenuOpen) { isContextMenuOpen = false; Interlocked.Exchange(ref dirtyFrame, 1); e.Handled = true; e.SuppressKeyPress = true; return; }
                if (isFilterDropdownOpen) { isFilterDropdownOpen = false; Interlocked.Exchange(ref dirtyFrame, 1); e.Handled = true; e.SuppressKeyPress = true; return; }
                if (pendingDeletePerson != null) { pendingDeletePerson = null; SetKeyboardActivationEnabled(false); Interlocked.Exchange(ref dirtyFrame, 1); e.Handled = true; e.SuppressKeyPress = true; return; }
                if (isLayerSearchFocused) { layerSearchQuery = ""; isLayerSearchFocused = false; SetKeyboardActivationEnabled(false); Interlocked.Exchange(ref dirtyFrame, 1); e.Handled = true; e.SuppressKeyPress = true; return; }
                if (isSearchFocused) { searchQuery = ""; isSearchFocused = false; SetKeyboardActivationEnabled(false); Interlocked.Exchange(ref dirtyFrame, 1); e.Handled = true; e.SuppressKeyPress = true; return; }
                if (currentView == ActiveView.Settings) { CancelInGlassSettings(); e.Handled = true; e.SuppressKeyPress = true; return; }
                if (currentView == ActiveView.AddOrEditPerson) { currentView = ActiveView.Dashboard; TriggerViewFade(); SetKeyboardActivationEnabled(false); Interlocked.Exchange(ref dirtyFrame, 1); e.Handled = true; e.SuppressKeyPress = true; return; }
            }

            // Arrow keys, Home, End, and Delete for natural cursor movement in Person Editor!
            if (currentView == ActiveView.AddOrEditPerson)
            {
                string curVal = activeTextField == "name" ? editName
                    : (activeTextField == "notes" ? editNotes
                    : (activeTextField == "year" ? editYearStr
                    : (activeTextField == "day" ? editDayStr : "")));
                int len = curVal != null ? curVal.Length : 0;
                if (e.KeyCode == Keys.Left)
                {
                    editCaretPos = ClampInt(editCaretPos - 1, 0, len);
                    e.Handled = true; e.SuppressKeyPress = true; MarkDirty(); return;
                }
                if (e.KeyCode == Keys.Right)
                {
                    editCaretPos = ClampInt(editCaretPos + 1, 0, len);
                    e.Handled = true; e.SuppressKeyPress = true; MarkDirty(); return;
                }
                if (e.KeyCode == Keys.Home)
                {
                    editCaretPos = 0;
                    e.Handled = true; e.SuppressKeyPress = true; MarkDirty(); return;
                }
                if (e.KeyCode == Keys.End)
                {
                    editCaretPos = len;
                    e.Handled = true; e.SuppressKeyPress = true; MarkDirty(); return;
                }
                if (e.KeyCode == Keys.Delete && len > 0)
                {
                    int cp = ClampInt(editCaretPos, 0, len);
                    if (cp < len)
                    {
                        if (activeTextField == "name") editName = editName.Remove(cp, 1);
                        else if (activeTextField == "notes") editNotes = editNotes.Remove(cp, 1);
                        else if (activeTextField == "year") editYearStr = editYearStr.Remove(cp, 1);
                        else if (activeTextField == "day") editDayStr = editDayStr.Remove(cp, 1);
                    }
                    e.Handled = true; e.SuppressKeyPress = true; MarkDirty(); return;
                }
            }

            // Ctrl+F focuses Dashboard search or Layers .exe search
            if (e.Control && e.KeyCode == Keys.F)
            {
                if (currentView == ActiveView.Dashboard)
                {
                    isSearchFocused = true;
                    SetKeyboardActivationEnabled(true);
                    Interlocked.Exchange(ref dirtyFrame, 1);
                    e.Handled = true; e.SuppressKeyPress = true;
                    return;
                }
                if (currentView == ActiveView.Settings && currentSettingsCat == SettingsCategory.Layers)
                {
                    isLayerSearchFocused = true;
                    SetKeyboardActivationEnabled(true);
                    Interlocked.Exchange(ref dirtyFrame, 1);
                    e.Handled = true; e.SuppressKeyPress = true;
                    return;
                }
            }

            // Ctrl+V paste into active text field
            if (e.Control && e.KeyCode == Keys.V)
            {
                try
                {
                    if (Clipboard.ContainsText())
                    {
                        string clip = Clipboard.GetText().Replace("\r", "").Replace("\n", " ").Trim();
                        if (!string.IsNullOrEmpty(clip))
                        {
                            if (currentView == ActiveView.Dashboard && isSearchFocused)
                            {
                                searchQuery = (searchQuery + clip);
                                if (searchQuery.Length > 26) searchQuery = searchQuery.Substring(0, 26);
                                listScrollOffset = 0;
                            }
                            else if (currentView == ActiveView.Settings && isLayerSearchFocused)
                            {
                                layerSearchQuery = (layerSearchQuery + clip);
                                if (layerSearchQuery.Length > 28) layerSearchQuery = layerSearchQuery.Substring(0, 28);
                                layerListScroll = 0;
                            }
                            else if (currentView == ActiveView.AddOrEditPerson)
                            {
                                editorErrorMsg = "";
                                if (activeTextField == "name") { int cp = ClampInt(editCaretPos, 0, editName.Length); editName = editName.Insert(cp, clip); if (editName.Length > 28) editName = editName.Substring(0, 28); editCaretPos = Math.Min(editName.Length, cp + clip.Length); }
                                else if (activeTextField == "notes") { int cp = ClampInt(editCaretPos, 0, editNotes.Length); editNotes = editNotes.Insert(cp, clip); if (editNotes.Length > 10000) editNotes = editNotes.Substring(0, 10000); editCaretPos = Math.Min(editNotes.Length, cp + clip.Length); }
                                else if (activeTextField == "year") { string digits = new string(clip.Where(char.IsDigit).ToArray()); editYearStr = (editYearStr + digits); if (editYearStr.Length > 4) editYearStr = editYearStr.Substring(0, 4); }
                                else if (activeTextField == "day")
                                {
                                    string digits = new string(clip.Where(char.IsDigit).ToArray());
                                    if (!string.IsNullOrEmpty(digits))
                                    {
                                        if (digits.Length > 2) digits = digits.Substring(0, 2);
                                        int d;
                                        if (int.TryParse(digits, out d) && d >= 1)
                                        {
                                            editDay = Math.Min(GetEditorMaxDaysInMonth(), d);
                                            editDayStr = editDay.ToString("00");
                                        }
                                    }
                                }
                            }
                            MarkDirty();
                        }
                    }
                }
                catch { }
                e.Handled = true; e.SuppressKeyPress = true;
                return;
            }

            // Ctrl+Backspace deletes last word
            if (e.Control && e.KeyCode == Keys.Back)
            {
                if (currentView == ActiveView.Dashboard && isSearchFocused) { searchQuery = DeleteLastWord(searchQuery); listScrollOffset = 0; }
                else if (currentView == ActiveView.Settings && isLayerSearchFocused) { layerSearchQuery = DeleteLastWord(layerSearchQuery); layerListScroll = 0; }
                else if (currentView == ActiveView.AddOrEditPerson)
                {
                    if (activeTextField == "name") editName = DeleteLastWord(editName);
                    else if (activeTextField == "notes") editNotes = DeleteLastWord(editNotes);
                    else if (activeTextField == "year") editYearStr = "";
                    else if (activeTextField == "day") editDayStr = "";
                }
                Interlocked.Exchange(ref dirtyFrame, 1);
                e.Handled = true; e.SuppressKeyPress = true;
                return;
            }

            // Tab / Shift+Tab cycling in Person Editor
            if (currentView == ActiveView.AddOrEditPerson && e.KeyCode == Keys.Tab)
            {
                string[] order = new string[] { "name", "year", "day", "notes" };
                int idx = Array.IndexOf(order, activeTextField);
                if (idx < 0) idx = 0;
                idx = e.Shift ? (idx + order.Length - 1) % order.Length : (idx + 1) % order.Length;
                activeTextField = order[idx];
                Interlocked.Exchange(ref dirtyFrame, 1);
                e.Handled = true; e.SuppressKeyPress = true;
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (currentView == ActiveView.Settings && isLangDropdownOpen)
            {
                langScrollOffset += (e.Delta < 0) ? 1 : -1;
                if (langScrollOffset < 0) langScrollOffset = 0;
                Interlocked.Exchange(ref dirtyFrame, 1);
                return;
            }
            if (pendingDeletePerson != null) return;
            if (currentView == ActiveView.Dashboard && !isCalendarView)
            {
                if (e.Delta < 0) listScrollOffset++;
                else if (e.Delta > 0 && listScrollOffset > 0) listScrollOffset--;
                Interlocked.Exchange(ref dirtyFrame, 1);
            }
            else if (currentView == ActiveView.Settings && currentSettingsCat == SettingsCategory.Layers)
            {
                if (e.Delta < 0) layerListScroll += 2;
                else if (e.Delta > 0) layerListScroll = Math.Max(0, layerListScroll - 2);
                Interlocked.Exchange(ref dirtyFrame, 1);
            }
            else if (currentView == ActiveView.AddOrEditPerson)
            {
                int maxD = GetEditorMaxDaysInMonth();
                if (e.Delta > 0) editDay = editDay >= maxD ? 1 : editDay + 1;
                else if (e.Delta < 0) editDay = editDay <= 1 ? maxD : editDay - 1;
                editDayStr = editDay.ToString("00");
                MarkDirty();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            isCursorInside = false;
            if (isGizmoHovered && !isDragging) isGizmoHovered = false;
            if (hoveredZoneId != null) hoveredZoneId = null;
            hasActiveHoverTransition = true;
            this.Cursor = curGlassDefault ?? Cursors.Default;
            MarkDirty();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Right)
            {
                PlaySatisfyingClickSound();
            }
            if (e.Button == MouseButtons.Left)
            {
                // Skip move gizmo if a popup menu or delete modal is open so top-left menu items are never hijacked by drag!
                if (!isContextMenuOpen && pendingDeletePerson == null && IsPointOnMoveGizmo(e.X, e.Y))
                {
                    StartDrag();
                    return;
                }

                HitZone? hit = FindHitZone(e.X, e.Y);

                // If context menu or filter dropdown is open and user clicks outside it, dismiss cleanly without triggering underlying buttons!
                if (isContextMenuOpen && (!hit.HasValue || !hit.Value.Id.StartsWith("ctx_")))
                {
                    isContextMenuOpen = false;
                    MarkDirty();
                    return;
                }
                if (isFilterDropdownOpen && (!hit.HasValue || (!hit.Value.Id.StartsWith("sort_") && hit.Value.Id != "btn_filter_dd")))
                {
                    isFilterDropdownOpen = false;
                    MarkDirty();
                    return;
                }

                if (hit.HasValue)
                {
                    if (hit.Value.Id == "prev_drag_hero")
                    {
                        isDraggingPreviewBlock = true;
                        previewDragCursor = new PointF(e.X, e.Y);
                        UpdateLayoutFromPreviewDrag(e.X, e.Y);
                        MarkDirty();
                        return;
                    }
                    HandleZoneClick(hit.Value);
                    try { renderWakeEvent.Set(); } catch { }
                }
                else
                {
                    bool changed = false;
                    if (isSearchFocused) { isSearchFocused = false; SetKeyboardActivationEnabled(false); changed = true; }
                    if (isLayerSearchFocused) { isLayerSearchFocused = false; SetKeyboardActivationEnabled(false); changed = true; }
                    if (changed) MarkDirty();
                }
            }
            else if (e.Button == MouseButtons.Right)
            {
                if (pendingDeletePerson != null) return;
                isFilterDropdownOpen = false;
                isContextMenuOpen = true;
                contextMenuPos = new PointF(e.X, e.Y);
                contextMenuProgress = 0.0f;
                MarkDirty();
            }
        }

        private void UpdateLayoutFromPreviewDrag(float mx, float my)
        {
            if (lastPreviewBoxRect.IsEmpty) return;
            float nx = (mx - lastPreviewBoxRect.X) / Math.Max(1f, lastPreviewBoxRect.Width);
            float ny = (my - lastPreviewBoxRect.Y) / Math.Max(1f, lastPreviewBoxRect.Height);
            float dx = nx - 0.5f;
            float dy = ny - 0.5f;
            if (Math.Abs(dx) >= Math.Abs(dy))
                stagedLayout = dx < 0f ? "HeroLeft" : "HeroRight";
            else
                stagedLayout = dy < 0f ? "HeroTop" : "HeroBottom";
        }

        private void HandleZoneClick(HitZone zone)
        {
            string id = zone.Id;

            if (isContextMenuOpen && !id.StartsWith("ctx_"))
            {
                isContextMenuOpen = false;
            }
            if (id == "tut_left")
            {
                if (tutorialStep <= 0)
                {
                    showTutorialOverlay = false;
                    config.HasCompletedTutorial = true;
                    SaveConfig();
                }
                else
                {
                    tutorialStep--;
                    tutorialStepProgress = 0.0f;
                }
                Interlocked.Exchange(ref dirtyFrame, 1);
                return;
            }
            if (id == "tut_next")
            {
                if (tutorialStep >= 2)
                {
                    showTutorialOverlay = false;
                    config.HasCompletedTutorial = true;
                    SaveConfig();
                }
                else
                {
                    tutorialStep++;
                    tutorialStepProgress = 0.0f;
                }
                Interlocked.Exchange(ref dirtyFrame, 1);
                return;
            }
            if (id == "ctx_add")
            {
                isContextMenuOpen = false;
                if (currentView == ActiveView.Settings) CancelInGlassSettings();
                StartInGlassPersonEditor(null);
                return;
            }
            if (id == "ctx_settings")
            {
                isContextMenuOpen = false;
                if (currentView == ActiveView.Settings) CancelInGlassSettings();
                else OpenInGlassSettings();
                return;
            }
            if (id == "ctx_toggle_cal")
            {
                isContextMenuOpen = false;
                isSearchFocused = false;
                isLayerSearchFocused = false;
                isFilterDropdownOpen = false;
                if (currentView == ActiveView.Settings) CancelInGlassSettings();
                else { currentView = ActiveView.Dashboard; SetKeyboardActivationEnabled(false); }
                isCalendarView = !isCalendarView;
                calendarViewProgress = 0.0f;
                Interlocked.Exchange(ref dirtyFrame, 1);
                return;
            }
            if (id == "ctx_exit")
            {
                isContextMenuOpen = false;
                Application.Exit();
                return;
            }
            if (id == "btn_banner_close")
            {
                bdayBannerDismissed = true;
                Interlocked.Exchange(ref dirtyFrame, 1);
                return;
            }
            if (id == "hero_prev")
            {
                int cnt = config != null && config.People != null ? config.People.Count : 0;
                if (cnt > 1)
                {
                    heroSpotlightIndex = (heroSpotlightIndex - 1 + cnt) % cnt;
                    heroStepperProgress = 0.0f;
                    bdayCelebrationStartSec = (float)animClock.Elapsed.TotalSeconds;
                }
                Interlocked.Exchange(ref dirtyFrame, 1);
                return;
            }
            if (id == "hero_next")
            {
                int cnt = config != null && config.People != null ? config.People.Count : 0;
                if (cnt > 1)
                {
                    heroSpotlightIndex = (heroSpotlightIndex + 1) % cnt;
                    heroStepperProgress = 0.0f;
                    bdayCelebrationStartSec = (float)animClock.Elapsed.TotalSeconds;
                }
                Interlocked.Exchange(ref dirtyFrame, 1);
                return;
            }
            if (id == "hero_reset")
            {
                heroSpotlightIndex = 0;
                heroStepperProgress = 0.0f;
                bdayCelebrationStartSec = (float)animClock.Elapsed.TotalSeconds;
                Interlocked.Exchange(ref dirtyFrame, 1);
                return;
            }
            if (id == "btn_copy_wish" && zone.Person != null)
            {
                if (string.IsNullOrWhiteSpace(zone.Person.Notes)) return;
                try
                {
                    Clipboard.SetText(zone.Person.Notes.Trim());
                    wishCopiedUntilSec = (float)animClock.Elapsed.TotalSeconds + 1.8f;
                }
                catch { }
                Interlocked.Exchange(ref dirtyFrame, 1);
                return;
            }
            if (id == "cal_quick_add")
            {
                StartInGlassPersonEditor(null, calMonth, calSelectedDay);
                return;
            }
            if (id == "cal_sh_prev")
            {
                calSharedPersonIndex--;
                Interlocked.Exchange(ref dirtyFrame, 1);
                return;
            }
            if (id == "cal_sh_next")
            {
                calSharedPersonIndex++;
                Interlocked.Exchange(ref dirtyFrame, 1);
                return;
            }

            if (id != "search_bar" && id != "search_clear" && isSearchFocused)
            {
                isSearchFocused = false;
                if (currentView == ActiveView.Dashboard) SetKeyboardActivationEnabled(false);
            }
            if (id != "lay_search" && isLayerSearchFocused)
            {
                isLayerSearchFocused = false;
                if (currentView == ActiveView.Settings) SetKeyboardActivationEnabled(false);
            }
            if (id != "btn_filter_dd" && !id.StartsWith("sort_") && isFilterDropdownOpen)
            {
                isFilterDropdownOpen = false;
            }

            if (id == "search_bar")
            {
                isSearchFocused = true;
                SetKeyboardActivationEnabled(true);
                Interlocked.Exchange(ref dirtyFrame, 1);
                return;
            }
            if (id == "search_clear")
            {
                searchQuery = "";
                listScrollOffset = 0;
                Interlocked.Exchange(ref dirtyFrame, 1);
                return;
            }
            if (id == "btn_filter_dd")
            {
                isFilterDropdownOpen = !isFilterDropdownOpen;
                Interlocked.Exchange(ref dirtyFrame, 1);
                return;
            }
            if (id.StartsWith("sort_"))
            {
                selectedSortFilter = id.Substring("sort_".Length);
                isFilterDropdownOpen = false;
                listScrollOffset = 0;
                Interlocked.Exchange(ref dirtyFrame, 1);
                return;
            }
            if (id == "view_list")
            {
                if (isCalendarView) calendarViewProgress = 0.0f;
                isCalendarView = false;
                Interlocked.Exchange(ref dirtyFrame, 1);
                return;
            }
            if (id == "view_cal")
            {
                if (!isCalendarView) calendarViewProgress = 0.0f;
                isCalendarView = true;
                Interlocked.Exchange(ref dirtyFrame, 1);
                return;
            }
            if (id == "cal_prev")
            {
                calMonth--;
                if (calMonth < 1) { calMonth = 12; calYear--; }
                calSelectedDay = Math.Min(calSelectedDay, DateTime.DaysInMonth(calYear, calMonth));
                calendarViewProgress = 0.0f;
                Interlocked.Exchange(ref dirtyFrame, 1);
                return;
            }
            if (id == "cal_next")
            {
                calMonth++;
                if (calMonth > 12) { calMonth = 1; calYear++; }
                calSelectedDay = Math.Min(calSelectedDay, DateTime.DaysInMonth(calYear, calMonth));
                calendarViewProgress = 0.0f;
                Interlocked.Exchange(ref dirtyFrame, 1);
                return;
            }
            if (id == "cal_today")
            {
                calYear = DateTime.Now.Year;
                calMonth = DateTime.Now.Month;
                calSelectedDay = DateTime.Now.Day;
                calendarViewProgress = 0.0f;
                Interlocked.Exchange(ref dirtyFrame, 1);
                return;
            }
            if (id.StartsWith("cal_day_"))
            {
                int d;
                if (int.TryParse(id.Substring("cal_day_".Length), out d))
                {
                    calSelectedDay = d;
                    calSharedPersonIndex = 0;
                    Interlocked.Exchange(ref dirtyFrame, 1);
                }
                return;
            }

            // Settings Sidebar Categories
            if (id.StartsWith("set_cat_"))
            {
                int cIdx;
                if (int.TryParse(id.Substring("set_cat_".Length), out cIdx))
                {
                    SettingsCategory nextCat = (SettingsCategory)ClampInt(cIdx, 0, 4);
                    if (nextCat != currentSettingsCat) settingsTabProgress = 0.0f;
                    currentSettingsCat = nextCat;
                    isLangDropdownOpen = false;
                    isNearDaysFocused = false;
                    if (currentSettingsCat == SettingsCategory.Layers) RefreshRunningProcesses(true);
                    Interlocked.Exchange(ref dirtyFrame, 1);
                }
                return;
            }
            if (id == "btn_near_days_input")
            {
                isNearDaysFocused = true;
                isLangDropdownOpen = false;
                SetKeyboardActivationEnabled(true);
                Interlocked.Exchange(ref dirtyFrame, 1);
                return;
            }
            if (id == "btn_near_days_dec")
            {
                isNearDaysFocused = false;
                stagedUrgentDays = ClampInt(stagedUrgentDays - 1, 1, 30);
                stagedNearDaysText = stagedUrgentDays.ToString();
                Interlocked.Exchange(ref dirtyFrame, 1);
                return;
            }
            if (id == "btn_near_days_inc")
            {
                isNearDaysFocused = false;
                stagedUrgentDays = ClampInt(stagedUrgentDays + 1, 1, 30);
                stagedNearDaysText = stagedUrgentDays.ToString();
                Interlocked.Exchange(ref dirtyFrame, 1);
                return;
            }
            if (id == "btn_lang_dd")
            {
                isLangDropdownOpen = !isLangDropdownOpen;
                isNearDaysFocused = false;
                isLangSearchFocused = isLangDropdownOpen;
                if (isLangDropdownOpen) { langSearchQuery = ""; SetKeyboardActivationEnabled(true); }
                else SetKeyboardActivationEnabled(false);
                Interlocked.Exchange(ref dirtyFrame, 1);
                return;
            }
            if (id == "btn_lang_search")
            {
                isLangSearchFocused = true;
                SetKeyboardActivationEnabled(true);
                Interlocked.Exchange(ref dirtyFrame, 1);
                return;
            }
            if (id == "set_replay_tutorial")
            {
                showTutorialOverlay = true;
                tutorialStep = 0;
                tutorialStepProgress = 0.0f;
                currentView = ActiveView.Dashboard;
                TriggerViewFade();
                SetKeyboardActivationEnabled(false);
                Interlocked.Exchange(ref dirtyFrame, 1);
                return;
            }
            if (id == "set_reset_all")
            {
                showResetConfirmModal = true;
                isLangDropdownOpen = false;
                isLangSearchFocused = false;
                Interlocked.Exchange(ref dirtyFrame, 1);
                return;
            }
            if (id == "btn_reset_cancel")
            {
                showResetConfirmModal = false;
                Interlocked.Exchange(ref dirtyFrame, 1);
                return;
            }
            if (id == "btn_reset_confirm")
            {
                showResetConfirmModal = false;
                try { if (File.Exists(configPath)) File.Delete(configPath); } catch { }
                DateTime sampleBday = DateTime.Today.AddDays(5);
                config = new WidgetConfig
                {
                    People = new List<PersonEntry> { new PersonEntry { Name = "Example", Month = sampleBday.Month, Day = sampleBday.Day, BirthYear = sampleBday.Year - 24 } },
                    AlertSoonDays = 14, AlertUrgentDays = 3, EnablePopupBanner = true, AutoStartWithPC = true,
                    AccentHex = "#FFFFFF", GlassPresetIndex = 0, ShowAgeBadge = true, WindowX = -9999, WindowY = -9999,
                    WidgetWidth = 780, WidgetHeight = 500, TintVariant = "Clear", FontName = "Melon Squishy",
                    TextColorMode = "Auto", LayoutMode = "HeroLeft", DarkMode = false, EnableBirthdayAnimation = true,
                    EnableBirthdayMessage = true, HasBdayDefaultsInit = true, HasCompletedTutorial = false,
                    LayerMode = "Above", LayerDesktopIcons = true, LayerRainmeter = true, LayerAlwaysOnTop = false,
                    LayerTargetExes = new List<string>(), TargetRefreshRate = 0, RefreshWhileDragging = true,
                    HasPerfDefaultsInit = true, Language = "EN", EnableClickSounds = true, CursorStyle = "Glass"
                };
                SaveConfig();
                ApplyCustomCursor("");
                ResizeWidget(780, 500, 48f, false);
                currentView = ActiveView.Dashboard;
                TriggerViewFade();
                SetKeyboardActivationEnabled(false);
                MarkDirty();
                return;
            }
            if (id.StartsWith("set_lang_"))
            {
                stagedLanguage = id.Substring("set_lang_".Length);
                isLangDropdownOpen = false;
                Interlocked.Exchange(ref dirtyFrame, 1);
                return;
            }
            if (id == "set_toggle_winmode")
            {
                stagedAppWindowMode = !stagedAppWindowMode;
                Interlocked.Exchange(ref dirtyFrame, 1);
                return;
            }
            if (id == "set_toggle_clicksound")
            {
                stagedClickSounds = !stagedClickSounds;
                if (stagedClickSounds) PlaySatisfyingClickSound();
                Interlocked.Exchange(ref dirtyFrame, 1);
                return;
            }
            if (id == "set_pick_sound")
            {
                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    ofd.Title = "Choose Custom Click Sound (.wav) — Cancel to Reset to Default";
                    ofd.Filter = "WAV Audio (*.wav)|*.wav";
                    if (ofd.ShowDialog(this) == DialogResult.OK)
                    {
                        stagedCustomSoundPath = ofd.FileName;
                        stagedClickSounds = true;
                        PlaySatisfyingClickSound();
                    }
                    else if (!string.IsNullOrEmpty(stagedCustomSoundPath))
                    {
                        stagedCustomSoundPath = "";
                    }
                }
                Interlocked.Exchange(ref dirtyFrame, 1);
                return;
            }
            if (id == "set_cur_glass")
            {
                stagedCursorStyle = "Glass";
                ApplyCustomCursor("");
                Interlocked.Exchange(ref dirtyFrame, 1);
                return;
            }
            if (id == "set_cur_sys")
            {
                stagedCursorStyle = "System";
                ApplyCustomCursor("");
                Interlocked.Exchange(ref dirtyFrame, 1);
                return;
            }
            if (id == "set_toggle_customsz")
            {
                stagedUseCustomSize = !stagedUseCustomSize;
                customSizeFocus = "";
                if (!stagedUseCustomSize && !(stagedWidth == 700 && stagedHeight == 450) && !(stagedWidth == 780 && stagedHeight == 500) && !(stagedWidth == 880 && stagedHeight == 560))
                {
                    stagedWidth = 780; stagedHeight = 500;
                    customWText = "780"; customHText = "500";
                }
                Interlocked.Exchange(ref dirtyFrame, 1);
                return;
            }
            if (id == "set_pick_cursor")
            {
                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    ofd.Title = "Choose Custom Cursor (.cur, .ani, .png)";
                    ofd.Filter = "Cursor / Image (*.cur;*.ani;*.png;*.ico)|*.cur;*.ani;*.png;*.ico";
                    if (ofd.ShowDialog(this) == DialogResult.OK)
                    {
                        stagedCustomCursorPath = ofd.FileName;
                        stagedCursorStyle = "Custom";
                        ApplyCustomCursor(stagedCustomCursorPath);
                    }
                    else if (!string.IsNullOrEmpty(stagedCustomCursorPath))
                    {
                        stagedCursorStyle = "Custom";
                        ApplyCustomCursor(stagedCustomCursorPath);
                    }
                }
                Interlocked.Exchange(ref dirtyFrame, 1);
                return;
            }
            if (id == "set_sz_custom_w")
            {
                customSizeFocus = "w";
                isNearDaysFocused = false;
                SetKeyboardActivationEnabled(true);
                Interlocked.Exchange(ref dirtyFrame, 1);
                return;
            }
            if (id == "set_sz_custom_h")
            {
                customSizeFocus = "h";
                isNearDaysFocused = false;
                SetKeyboardActivationEnabled(true);
                Interlocked.Exchange(ref dirtyFrame, 1);
                return;
            }
            if (id == "btn_export_backup")
            {
                ExportBackupBundle();
                return;
            }
            if (id == "btn_import_backup")
            {
                ImportBackupBundle();
                return;
            }

            // Appearance Settings
            if (id == "btn_win_min")
            {
                this.Location = new Point(currentWinX, currentWinY);
                ShowWindow(this.Handle, 6); // SW_MINIMIZE
                return;
            }
            if (id == "btn_win_close")
            {
                Application.Exit();
                return;
            }
            if (id == "app_theme_glass")
            {
                stagedBasicTheme = false;
                stagedFont = "Melon Squishy";
                PrecomputeOpticalTables();
                MarkDirty();
                return;
            }
            if (id == "app_theme_basic")
            {
                stagedBasicTheme = true;
                stagedFont = "Segoe UI";
                PrecomputeOpticalTables();
                MarkDirty();
                return;
            }
            if (id.StartsWith("app_tint_"))
            {
                stagedTint = id.Substring("app_tint_".Length);
                Interlocked.Exchange(ref dirtyFrame, 1);
                return;
            }
            if (id.StartsWith("app_font_"))
            {
                stagedFont = id.Substring("app_font_".Length);
                Interlocked.Exchange(ref dirtyFrame, 1);
                return;
            }
            if (id.StartsWith("app_tcol_"))
            {
                stagedTextColor = id.Substring("app_tcol_".Length);
                Interlocked.Exchange(ref dirtyFrame, 1);
                return;
            }
            if (id.StartsWith("app_lay_"))
            {
                stagedLayout = id.Substring("app_lay_".Length);
                Interlocked.Exchange(ref dirtyFrame, 1);
                return;
            }
            if (id == "app_mode_light")
            {
                stagedDarkMode = false;
                Interlocked.Exchange(ref dirtyFrame, 1);
                return;
            }
            if (id == "app_mode_dark")
            {
                stagedDarkMode = true;
                Interlocked.Exchange(ref dirtyFrame, 1);
                return;
            }

            // Layer System Settings
            if (id == "lay_mode_above") { stagedLayerMode = "Above"; Interlocked.Exchange(ref dirtyFrame, 1); return; }
            if (id == "lay_mode_below") { stagedLayerMode = "Below"; Interlocked.Exchange(ref dirtyFrame, 1); return; }
            if (id == "lay_basic_icons") { stagedLayerDesktopIcons = !stagedLayerDesktopIcons; Interlocked.Exchange(ref dirtyFrame, 1); return; }
            if (id == "lay_basic_rain")  { stagedLayerRainmeter = !stagedLayerRainmeter; Interlocked.Exchange(ref dirtyFrame, 1); return; }
            if (id == "lay_basic_top")   { stagedLayerAlwaysOnTop = !stagedLayerAlwaysOnTop; Interlocked.Exchange(ref dirtyFrame, 1); return; }
            if (id == "lay_search")
            {
                isLayerSearchFocused = true;
                SetKeyboardActivationEnabled(true);
                Interlocked.Exchange(ref dirtyFrame, 1);
                return;
            }
            if (id == "lay_browse_exe")
            {
                PickCustomExeForLayer();
                return;
            }
            if (id.StartsWith("lay_exe_"))
            {
                string exe = id.Substring("lay_exe_".Length);
                int existingIdx = stagedLayerTargets.FindIndex(x => x.Equals(exe, StringComparison.OrdinalIgnoreCase));
                if (existingIdx >= 0) stagedLayerTargets.RemoveAt(existingIdx);
                else stagedLayerTargets.Add(exe);
                Interlocked.Exchange(ref dirtyFrame, 1);
                return;
            }

            // Handle Delete Confirmation Dialog Buttons
            if (id == "confirm_del_no")
            {
                pendingDeletePerson = null;
                SetKeyboardActivationEnabled(false);
                Interlocked.Exchange(ref dirtyFrame, 1);
                return;
            }
            if (id == "confirm_del_yes")
            {
                if (pendingDeletePerson != null)
                {
                    lock (uiLock)
                    {
                        config.People.RemoveAll(p => p.Id == pendingDeletePerson.Id);
                        pendingDeletePerson = null;
                        heroSpotlightIndex = 0;
                    }
                    SaveConfig();
                }
                SetKeyboardActivationEnabled(false);
                Interlocked.Exchange(ref dirtyFrame, 1);
                return;
            }

            if (id == "btn_add")
            {
                StartInGlassPersonEditor(null);
                return;
            }
            if (id == "btn_settings")
            {
                OpenInGlassSettings();
                return;
            }
            else if (id == "btn_cancel_settings")
            {
                CancelInGlassSettings();
                return;
            }
            else if (id == "btn_save_settings")
            {
                SaveInGlassSettings();
                return;
            }
            else if (id == "btn_back" || id == "btn_cancel_edit")
            {
                pendingDeletePerson = null;
                currentView = ActiveView.Dashboard;
                TriggerViewFade();
                SetKeyboardActivationEnabled(false);
            }
            else if (id == "btn_save_edit")
            {
                SaveInGlassPersonEditor();
                return;
            }
            else if (id == "field_name")
            {
                activeTextField = "name";
                SetKeyboardActivationEnabled(true);
            }
            else if (id == "field_year")
            {
                activeTextField = "year";
                SetKeyboardActivationEnabled(true);
            }
            else if (id == "field_day")
            {
                activeTextField = "day";
                SetKeyboardActivationEnabled(true);
            }
            else if (id == "field_notes")
            {
                activeTextField = "notes";
                SetKeyboardActivationEnabled(true);
            }
            else if (id.StartsWith("em_"))
            {
                int m;
                if (int.TryParse(id.Substring(3), out m))
                {
                    editMonth = Math.Max(1, Math.Min(12, m));
                    int maxD = GetEditorMaxDaysInMonth();
                    if (editDay > maxD) editDay = maxD;
                    editDayStr = editDay.ToString("00");
                }
            }
            else if (id == "day_minus")
            {
                int maxD = GetEditorMaxDaysInMonth();
                editDay = editDay <= 1 ? maxD : editDay - 1;
                editDayStr = editDay.ToString("00");
            }
            else if (id == "day_plus")
            {
                int maxD = GetEditorMaxDaysInMonth();
                editDay = editDay >= maxD ? 1 : editDay + 1;
                editDayStr = editDay.ToString("00");
            }
            else if (id.StartsWith("eic_"))
            {
                editIconType = id.Substring(4);
            }
            else if (id == "btn_pick_img")
            {
                PickAvatarImageFile();
                return;
            }
            else if (id == "btn_hero_edit")
            {
                if (zone.Person != null)
                {
                    StartInGlassPersonEditor(zone.Person);
                    return;
                }
            }
            else if (id.StartsWith("row_"))
            {
                if (zone.Person != null && config != null && config.People != null)
                {
                    DateTime now = DateTime.Now;
                    List<PersonEntry> sorted = config.People.OrderBy(p => p.GetDaysUntil(now)).ThenBy(p => p.Name).ToList();
                    int idx = sorted.IndexOf(zone.Person);
                    if (idx >= 0)
                    {
                        heroSpotlightIndex = idx;
                        heroStepperProgress = 0.0f;
                        bdayCelebrationStartSec = (float)animClock.Elapsed.TotalSeconds;
                        Interlocked.Exchange(ref dirtyFrame, 1);
                    }
                    return;
                }
            }
            else if (id.StartsWith("del_"))
            {
                if (zone.Person != null)
                {
                    pendingDeletePerson = zone.Person;
                    deleteModalProgress = 0.0f;
                    SetKeyboardActivationEnabled(true);
                }
            }
            else if (id.StartsWith("filter_"))
            {
                selectedFilter = id.Substring("filter_".Length);
                listScrollOffset = 0;
            }
            else if (id == "scroll_up")
            {
                if (listScrollOffset > 0) listScrollOffset--;
            }
            else if (id == "scroll_dn")
            {
                listScrollOffset++;
            }
            else if (id.StartsWith("set_urg_"))
            {
                int val;
                if (int.TryParse(id.Substring("set_urg_".Length), out val))
                {
                    stagedUrgentDays = val;
                }
            }
            else if (id.StartsWith("set_soon_"))
            {
                int val;
                if (int.TryParse(id.Substring("set_soon_".Length), out val))
                {
                    stagedSoonDays = val;
                }
            }
            else if (id == "set_toggle_age")
            {
                stagedShowAge = !stagedShowAge;
            }
            else if (id == "set_toggle_autostart")
            {
                stagedAutoStart = !stagedAutoStart;
            }
            else if (id == "set_toggle_bdayanim")
            {
                stagedBdayAnim = !stagedBdayAnim;
            }
            else if (id == "set_toggle_bdaymsg")
            {
                stagedBdayMsg = !stagedBdayMsg;
            }
            else if (id.StartsWith("set_hz_"))
            {
                int hzVal;
                if (int.TryParse(id.Substring("set_hz_".Length), out hzVal))
                {
                    stagedRefreshRate = hzVal;
                }
            }
            else if (id == "set_toggle_dragref")
            {
                stagedRefreshWhileDragging = !stagedRefreshWhileDragging;
            }
            else if (id == "set_toggle_winmode")
            {
                stagedAppWindowMode = !stagedAppWindowMode;
            }
            else if (id == "set_winmode_widget")
            {
                stagedAppWindowMode = false;
            }
            else if (id == "set_winmode_real")
            {
                stagedAppWindowMode = true;
            }
            else if (id.StartsWith("set_preset_"))
            {
                int pIdx;
                if (int.TryParse(id.Substring("set_preset_".Length), out pIdx))
                {
                    stagedPresetIndex = pIdx;
                    ApplyPresetValues(pIdx);
                    PrecomputeOpticalTables();
                }
            }
            else if (id == "set_sz_1") { stagedWidth = 700; stagedHeight = 450; stagedRadius = 46f; ResizeWidget(700, 450, 46f, false); return; }
            else if (id == "set_sz_2") { stagedWidth = 780; stagedHeight = 500; stagedRadius = 50f; ResizeWidget(780, 500, 50f, false); return; }
            else if (id == "set_sz_3") { stagedWidth = 880; stagedHeight = 560; stagedRadius = 54f; ResizeWidget(880, 560, 54f, false); return; }

            MarkDirty();
        }

        private Cursor GetScenarioCursor(bool overGizmo, string zoneId)
        {
            if (isDragging) return curGlassGrabbing ?? Cursors.SizeAll;
            if (overGizmo) return curGlassGrab ?? Cursors.SizeAll;
            if (string.IsNullOrEmpty(zoneId)) return curGlassDefault ?? Cursors.Default;
            if (zoneId == "search_bar" || zoneId.StartsWith("field_")) return curGlassText ?? Cursors.IBeam;
            if (zoneId.StartsWith("del_") || zoneId == "confirm_del_yes" || zoneId == "search_clear") return curGlassDelete ?? Cursors.Hand;
            return curGlassPointer ?? Cursors.Hand;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            PointF prevPos = localCursorPos;
            localCursorPos = new PointF(e.X, e.Y);
            isCursorInside = true;

            if (isDragging) return;
            if (isDraggingPreviewBlock)
            {
                previewDragCursor = new PointF(e.X, e.Y);
                UpdateLayoutFromPreviewDrag(e.X, e.Y);
                MarkDirty();
                return;
            }

            bool overGizmo = IsPointOnMoveGizmo(e.X, e.Y);
            HitZone? hit = overGizmo ? null : FindHitZone(e.X, e.Y);
            string newZoneId = hit.HasValue ? hit.Value.Id : null;

            bool changed = false;
            if (overGizmo != isGizmoHovered) { isGizmoHovered = overGizmo; changed = true; }
            if (newZoneId != hoveredZoneId) { hoveredZoneId = newZoneId; hasActiveHoverTransition = true; changed = true; }
            bool movedOnButton = newZoneId != null && (Math.Abs(prevPos.X - e.X) >= 2f || Math.Abs(prevPos.Y - e.Y) >= 2f);
            if (changed || movedOnButton)
            {
                MarkDirty();
            }

            this.Cursor = GetScenarioCursor(overGizmo, newZoneId);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left)
            {
                if (isDraggingPreviewBlock)
                {
                    isDraggingPreviewBlock = false;
                    Interlocked.Exchange(ref dirtyFrame, 1);
                }
                if (isDragging)
                {
                    EndDrag();
                }
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_ERASEBKGND)
            {
                m.Result = new IntPtr(1);
                return;
            }
            // In Desktop Widget Mode, block Windows minimize (Win+D / Show Desktop / Minimize All) — widget can only be closed!
            if (!IsAppWindowModeEnabled())
            {
                if (m.Msg == 0x0112 && ((m.WParam.ToInt32() & 0xFFF0) == 0xF020)) // WM_SYSCOMMAND SC_MINIMIZE
                {
                    return;
                }
                if (m.Msg == 0x0005 && m.WParam.ToInt32() == 1) // WM_SIZE SIZE_MINIMIZED
                {
                    try { ShowWindow(this.Handle, 4); } catch { }
                    return;
                }
            }
            bool needsKeyboard = IsAppWindowModeEnabled()
                || (currentView == ActiveView.AddOrEditPerson)
                || (pendingDeletePerson != null)
                || (currentView == ActiveView.Dashboard && isSearchFocused)
                || (currentView == ActiveView.Settings && (isLayerSearchFocused || isNearDaysFocused || isLangSearchFocused || !string.IsNullOrEmpty(customSizeFocus)));
            if (m.Msg == WM_MOUSEACTIVATE)
            {
                m.Result = new IntPtr(needsKeyboard ? MA_ACTIVATE : MA_NOACTIVATE);
                return;
            }
            if (m.Msg == WM_WINDOWPOSCHANGING && !isDragging && this.Visible)
            {
                WINDOWPOS wp = (WINDOWPOS)Marshal.PtrToStructure(m.LParam, typeof(WINDOWPOS));
                if (!needsKeyboard)
                {
                    wp.flags |= SWP_NOACTIVATE;
                }
                // In Real Window Mode, pin restored position directly to (currentWinX, currentWinY) so it NEVER teleports from center!
                if (IsAppWindowModeEnabled() && !isSnappingToScreen && !IsIconic(this.Handle) && (wp.flags & 0x0002) == 0)
                {
                    wp.x = currentWinX;
                    wp.y = currentWinY;
                }
                Marshal.StructureToPtr(wp, m.LParam, false);
            }
            else if (m.Msg == WM_DISPLAYCHANGE || m.Msg == WM_SETTINGCHANGE)
            {
                Interlocked.Exchange(ref dirtyFrame, 1);
            }

            base.WndProc(ref m);
        }
    }
}
