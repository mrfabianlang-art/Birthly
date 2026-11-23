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
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace BirthlyInstaller
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
                using (GlassSetupForm f = new GlassSetupForm(true, false))
                {
                    f.ExportPreview(args[1]);
                }
                return;
            }
            if (args != null && args.Length >= 2 && args[0] == "--preview-basic")
            {
                using (GlassSetupForm f = new GlassSetupForm(true, true))
                {
                    f.ExportPreview(args[1]);
                }
                return;
            }
            Application.Run(new GlassSetupForm(false, false));
        }
    }

    public class GlassSetupForm : Form
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

        private int winW = 580;
        private int winH = 490;
        private float cornerRadius = 36f;
        private int edgeMargin = 4;
        private int surfW, surfH;

        private int deskW, deskH;
        private Rectangle vScreen;
        private int[] blurredDesk;
        private int[] sharpDesk;
        private int[] surfaceBuf;

        // Persistent reusable UI bitmap & Win32 DIB section (ZERO per-frame allocation or lag!)
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
            public int rimW256, absorb256, tintW256, specW256, alpha255;
            public bool isEdge;
        }
        private PixelLUT[][] lutRows;

        private struct HitZone { public string Id; public RectangleF Bounds; }
        private List<HitZone> hitZones = new List<HitZone>();
        private string hoveredId = null;
        private Dictionary<string, float> hoverProgress = new Dictionary<string, float>();
        private float themeBlend = 0f; // 0 = Liquid Glass, 1 = Basic Theme
        private float popInProgress = 1f;
        private PointF localCursorPos = new PointF(-1000f, -1000f);
        private bool isCursorInside = false;
        private System.Windows.Forms.Timer animTimer;
        private Stopwatch animSw = Stopwatch.StartNew();
        private float lastAnimSec = 0f;

        private Image logoImg;
        private PrivateFontCollection pfc = new PrivateFontCollection();
        private FontFamily melonFamily;
        private readonly Dictionary<int, Font> fontCache = new Dictionary<int, Font>();
        private readonly SolidBrush sharedTextBr = new SolidBrush(Color.White);
        private readonly SolidBrush sharedShadowBr = new SolidBrush(Color.FromArgb(200, 6, 9, 16));
        private int[] cachedBackdropBuf;
        private int lastBgX = -99999, lastBgY = -99999, lastBgTb256 = -1;
        private static readonly string[] AllHoverIds = new string[] { "close", "theme_glass", "theme_basic", "browse", "opt_desk", "opt_start", "opt_auto", "opt_launch", "opt_readme", "cancel", "install" };

        // Setup state + Theme Choice (Liquid Glass Theme vs Basic Program Theme)
        private string installPath;
        [DllImport("user32.dll")]
        private static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);

        private bool useBasicTheme = false; // false = Liquid Glass (Melon Squishy), true = Basic Theme (Segoe UI)
        private string selectedLang = "EN"; // EN, DE, ES, FR, IT, PT, NL, PL, RU, JA, ZH, KO
        private bool isLangDdOpen = false;
        private string langSearch = "";
        private int langScroll = 0;
        private System.Windows.Forms.Timer liveBgTimer = null;
        private static readonly string[,] SetupLanguages = new string[,] {
            { "EN", "English" }, { "DE", "Deutsch" }, { "ES", "Español" }, { "FR", "Français" },
            { "IT", "Italiano" }, { "PT", "Português" }, { "NL", "Nederlands" }, { "PL", "Polski" },
            { "RU", "Русский" }, { "JA", "日本語" }, { "ZH", "中文" }, { "KO", "한국어" }
        };
        private string GetSetupLangName(string code)
        {
            for (int i = 0; i < SetupLanguages.GetLength(0); i++)
                if (SetupLanguages[i, 0] == code) return SetupLanguages[i, 1];
            return "English";
        }
        private readonly Dictionary<string, string> setupTransCache = new Dictionary<string, string>();
        private readonly HashSet<string> setupTransPending = new HashSet<string>();

        private string SL(string key)
        {
            if (selectedLang == "EN" || string.IsNullOrEmpty(key)) return key;
            if (selectedLang == "DE")
            {
                switch (key)
                {
                    case "Install Birthly": return "Birthly installieren";
                    case "B I R T H L Y   S E T U P": return "BIRTHLY INSTALLER";
                    case "Liquid Glass Theme  •  Melon Squishy Font  •  v0.1.0": return "Liquid-Glass-Design  •  Melon Squishy  •  v0.1.0";
                    case "Basic Program Theme  •  Segoe UI Font  •  v0.1.0": return "Basis-Design  •  Segoe UI  •  v0.1.0";
                    case "LIQUID GLASS (MELON)": return "LIQUID GLASS (MELON)";
                    case "BASIC THEME (SEGOE UI)": return "BASIS-DESIGN (SEGOE UI)";
                    case "Ready to install Birthly + README.txt + Uninstaller.": return "Bereit zum Installieren von Birthly + README.txt + Deinstaller.";
                    case "CHOOSE PROGRAM THEME & FONT (CHANGEABLE ANYTIME IN SETTINGS):": return "PROGRAMM-DESIGN & SCHRIFTART WÄHLEN (JEDERZEIT ÄNDERBAR):";
                    case "INSTALLATION FOLDER:": return "INSTALLATIONSORDNER:";
                    case "Browse...": return "Durchsuchen...";
                    case "Create Desktop shortcut": return "Desktop-Verknüpfung erstellen";
                    case "Create Start Menu shortcuts (Birthly & Uninstall)": return "Startmenü-Verknüpfungen erstellen (Birthly & Deinstallieren)";
                    case "Start Birthly automatically with Windows": return "Birthly automatisch mit Windows starten";
                    case "Launch Birthly after installation": return "Birthly nach der Installation starten";
                    case "Open README.txt after installation": return "README.txt nach der Installation öffnen";
                    case "CANCEL": return "ABBRECHEN";
                    case "CLOSE": return "SCHLIESSEN";
                    case "INSTALL BIRTHLY": return "BIRTHLY INSTALLIEREN";
                    case "INSTALLING...": return "INSTALLIERE...";
                    case "INSTALLED ✓": return "INSTALLIERT ✓";
                }
            }
            else if (selectedLang == "ES")
            {
                switch (key)
                {
                    case "Install Birthly": return "Instalar Birthly";
                    case "B I R T H L Y   S E T U P": return "INSTALADOR BIRTHLY";
                    case "Liquid Glass Theme  •  Melon Squishy Font  •  v0.1.0": return "Tema Liquid Glass  •  Melon Squishy  •  v0.1.0";
                    case "Basic Program Theme  •  Segoe UI Font  •  v0.1.0": return "Tema Básico  •  Segoe UI  •  v0.1.0";
                    case "LIQUID GLASS (MELON)": return "CRISTAL LÍQUIDO (MELON)";
                    case "BASIC THEME (SEGOE UI)": return "TEMA BÁSICO (SEGOE UI)";
                    case "Ready to install Birthly + README.txt + Uninstaller.": return "Listo para instalar Birthly + README.txt + Desinstalador.";
                    case "CHOOSE PROGRAM THEME & FONT (CHANGEABLE ANYTIME IN SETTINGS):": return "ELIGE TEMA Y FUENTE (CAMBIABLE EN AJUSTES):";
                    case "INSTALLATION FOLDER:": return "CARPETA DE INSTALACIÓN:";
                    case "Browse...": return "Examinar...";
                    case "Create Desktop shortcut": return "Crear acceso directo en el Escritorio";
                    case "Create Start Menu shortcuts (Birthly & Uninstall)": return "Crear accesos en el Menú Inicio";
                    case "Start Birthly automatically with Windows": return "Iniciar Birthly con Windows";
                    case "Launch Birthly after installation": return "Abrir Birthly al terminar";
                    case "Open README.txt after installation": return "Abrir README.txt al terminar";
                    case "CANCEL": return "CANCELAR";
                    case "CLOSE": return "CERRAR";
                    case "INSTALL BIRTHLY": return "INSTALAR BIRTHLY";
                    case "INSTALLING...": return "INSTALANDO...";
                    case "INSTALLED ✓": return "INSTALADO ✓";
                }
            }
            else if (selectedLang == "FR")
            {
                switch (key)
                {
                    case "Install Birthly": return "Installer Birthly";
                    case "CHOOSE PROGRAM THEME & FONT (CHANGEABLE ANYTIME IN SETTINGS):": return "CHOISIR LE THÈME ET LA POLICE (MODIFIABLE DANS PARAMÈTRES) :";
                    case "INSTALLATION FOLDER:": return "DOSSIER D'INSTALLATION :";
                    case "Browse...": return "Parcourir...";
                    case "Create Desktop shortcut": return "Créer un raccourci sur le Bureau";
                    case "Create Start Menu shortcuts (Birthly & Uninstall)": return "Créer des raccourcis dans le Menu Démarrer";
                    case "Start Birthly automatically with Windows": return "Démarrer Birthly avec Windows";
                    case "Launch Birthly after installation": return "Lancer Birthly après l'installation";
                    case "Open README.txt after installation": return "Ouvrir README.txt après l'installation";
                    case "CANCEL": return "ANNULER";
                    case "CLOSE": return "FERMER";
                    case "INSTALL BIRTHLY": return "INSTALLER BIRTHLY";
                    case "INSTALLING...": return "INSTALLATION...";
                    case "INSTALLED ✓": return "INSTALLÉ ✓";
                }
            }
            else if (selectedLang == "IT")
            {
                switch (key)
                {
                    case "Install Birthly": return "Installa Birthly";
                    case "CHOOSE PROGRAM THEME & FONT (CHANGEABLE ANYTIME IN SETTINGS):": return "SCEGLI TEMA E FONT (MODIFICABILE NELLE IMPOSTAZIONI):";
                    case "INSTALLATION FOLDER:": return "CARTELLA DI INSTALLAZIONE:";
                    case "Browse...": return "Sfoglia...";
                    case "Create Desktop shortcut": return "Crea collegamento sul Desktop";
                    case "Create Start Menu shortcuts (Birthly & Uninstall)": return "Crea collegamenti nel Menu Start";
                    case "Start Birthly automatically with Windows": return "Avvia Birthly con Windows";
                    case "Launch Birthly after installation": return "Avvia Birthly dopo l'installazione";
                    case "Open README.txt after installation": return "Apri README.txt dopo l'installazione";
                    case "CANCEL": return "ANNULLA";
                    case "CLOSE": return "CHIUDI";
                    case "INSTALL BIRTHLY": return "INSTALLA BIRTHLY";
                    case "INSTALLING...": return "INSTALLAZIONE...";
                    case "INSTALLED ✓": return "INSTALLATO ✓";
                }
            }
            else if (selectedLang == "PT")
            {
                switch (key)
                {
                    case "Install Birthly": return "Instalar Birthly";
                    case "INSTALLATION FOLDER:": return "PASTA DE INSTALAÇÃO:";
                    case "Browse...": return "Procurar...";
                    case "CANCEL": return "CANCELAR";
                    case "CLOSE": return "FECHAR";
                    case "INSTALL BIRTHLY": return "INSTALAR BIRTHLY";
                }
            }
            else if (selectedLang == "NL")
            {
                switch (key)
                {
                    case "Install Birthly": return "Birthly installeren";
                    case "INSTALLATION FOLDER:": return "INSTALLATIEMAP:";
                    case "Browse...": return "Bladeren...";
                    case "CANCEL": return "ANNULEREN";
                    case "CLOSE": return "SLUITEN";
                    case "INSTALL BIRTHLY": return "BIRTHLY INSTALLEREN";
                }
            }
            else if (selectedLang == "PL")
            {
                switch (key)
                {
                    case "Install Birthly": return "Zainstaluj Birthly";
                    case "INSTALLATION FOLDER:": return "FOLDER INSTALACJI:";
                    case "Browse...": return "Przeglądaj...";
                    case "CANCEL": return "ANULUJ";
                    case "CLOSE": return "ZAMKNIJ";
                    case "INSTALL BIRTHLY": return "ZAINSTALUJ BIRTHLY";
                }
            }
            else if (selectedLang == "RU")
            {
                switch (key)
                {
                    case "Install Birthly": return "Установить Birthly";
                    case "INSTALLATION FOLDER:": return "ПАПКА УСТАНОВКИ:";
                    case "Browse...": return "Обзор...";
                    case "CANCEL": return "ОТМЕНА";
                    case "CLOSE": return "ЗАКРЫТЬ";
                    case "INSTALL BIRTHLY": return "УСТАНОВИТЬ BIRTHLY";
                }
            }
            else if (selectedLang == "JA")
            {
                switch (key)
                {
                    case "Install Birthly": return "Birthly をインストール";
                    case "INSTALLATION FOLDER:": return "インストール先フォルダ:";
                    case "Browse...": return "参照...";
                    case "CANCEL": return "キャンセル";
                    case "CLOSE": return "閉じる";
                    case "INSTALL BIRTHLY": return "BIRTHLY をインストール";
                }
            }
            else if (selectedLang == "ZH")
            {
                switch (key)
                {
                    case "Install Birthly": return "安装 Birthly";
                    case "INSTALLATION FOLDER:": return "安装目录:";
                    case "Browse...": return "浏览...";
                    case "CANCEL": return "取消";
                    case "CLOSE": return "关闭";
                    case "INSTALL BIRTHLY": return "安装 BIRTHLY";
                }
            }
            else if (selectedLang == "KO")
            {
                switch (key)
                {
                    case "Install Birthly": return "Birthly 설치";
                    case "INSTALLATION FOLDER:": return "설치 폴더:";
                    case "Browse...": return "찾아보기...";
                    case "CANCEL": return "취소";
                    case "CLOSE": return "닫기";
                    case "INSTALL BIRTHLY": return "BIRTHLY 설치";
                }
            }
            string cKey = selectedLang + "|" + key;
            lock (setupTransPending)
            {
                string cached;
                if (setupTransCache.TryGetValue(cKey, out cached)) return cached;
                if (!setupTransPending.Contains(cKey))
                {
                    setupTransPending.Add(cKey);
                    string langSnap = selectedLang;
                    string gCode = langSnap == "ZH" ? "zh-CN" : langSnap.ToLowerInvariant();
                    System.Threading.ThreadPool.QueueUserWorkItem(_ => {
                        try
                        {
                            System.Net.ServicePointManager.SecurityProtocol = (System.Net.SecurityProtocolType)3072;
                            System.Net.ServicePointManager.DefaultConnectionLimit = 32;
                            string url = "https://translate.googleapis.com/translate_a/single?client=gtx&sl=en&tl=" + gCode + "&dt=t&q=" + Uri.EscapeDataString(key);
                            using (System.Net.WebClient wc = new System.Net.WebClient())
                            {
                                wc.Encoding = System.Text.Encoding.UTF8;
                                string resp = wc.DownloadString(url);
                                int q1 = resp.IndexOf("[[[\"");
                                if (q1 >= 0)
                                {
                                    int s = q1 + 4;
                                    int e = resp.IndexOf("\",\"", s);
                                    if (e > s)
                                    {
                                        string tr = resp.Substring(s, e - s).Replace("\\\"", "\"");
                                        if (!string.IsNullOrWhiteSpace(tr))
                                        {
                                            lock (setupTransPending) { setupTransCache[cKey] = tr; }
                                            try { this.BeginInvoke(new Action(() => RenderFrame(true))); } catch { }
                                        }
                                    }
                                }
                            }
                        }
                        catch { }
                    });
                }
            }
            return key;
        }
        private void LoadExistingInstallSettings()
        {
            try
            {
                // Default to Windows system language (e.g. EN, DE, FR...) if supported, otherwise EN!
                string sysIso = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToUpperInvariant();
                for (int i = 0; i < SetupLanguages.GetLength(0); i++)
                {
                    if (SetupLanguages[i, 0] == sysIso) { selectedLang = sysIso; break; }
                }

                string[] candidates = new string[] {
                    Path.Combine(installPath, "birthdays_config.json"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Birthly", "birthdays_config.json")
                };
                foreach (string c in candidates)
                {
                    if (!File.Exists(c)) continue;
                    string json = File.ReadAllText(c, System.Text.Encoding.UTF8);
                    if (json.IndexOf("\"UseBasicTheme\":true", StringComparison.OrdinalIgnoreCase) >= 0) { useBasicTheme = true; themeBlend = 1f; }
                    else if (json.IndexOf("\"UseBasicTheme\":false", StringComparison.OrdinalIgnoreCase) >= 0) { useBasicTheme = false; themeBlend = 0f; }
                    int li = json.IndexOf("\"Language\":\"", StringComparison.OrdinalIgnoreCase);
                    if (li >= 0)
                    {
                        int s = li + 12;
                        int e = json.IndexOf('"', s);
                        if (e > s && (e - s) <= 5) selectedLang = json.Substring(s, e - s).ToUpperInvariant();
                    }
                    break;
                }
            }
            catch { }
        }
        private bool optDesktop = true;
        private bool optStartMenu = true;
        private bool optAutoStart = true;
        private bool optLaunch = true;
        private bool optReadme = false;
        private bool isInstalling = false;
        private bool installDone = false;
        private float progressVal = 0f;
        private string statusText = "Ready to install Birthly + README.txt + Uninstaller.";

        // Drag state
        private bool isDragging = false;
        private Point dragStartCursor;
        private Point dragStartWin;
        private int curX, curY;

        private readonly StringFormat sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap, Trimming = StringTrimming.EllipsisCharacter };
        private readonly StringFormat sfNear = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap, Trimming = StringTrimming.EllipsisCharacter };

        public GlassSetupForm(bool previewMode, bool initialBasicTheme)
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.ShowInTaskbar = !previewMode;
            this.Text = "Birthly Setup";
            this.useBasicTheme = initialBasicTheme;
            this.themeBlend = initialBasicTheme ? 1f : 0f;
            this.popInProgress = previewMode ? 1f : 0f;
            try { this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            surfW = winW + edgeMargin * 2;
            surfH = winH + edgeMargin * 2;
            this.Size = new Size(surfW, surfH);

            Rectangle scr = Screen.PrimaryScreen.WorkingArea;
            curX = scr.Left + (scr.Width - surfW) / 2;
            curY = scr.Top + (scr.Height - surfH) / 2;
            this.Location = new Point(curX, curY);

            installPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Birthly");

            LoadEmbeddedAssets();
            vScreen = SystemInformation.VirtualScreen;
            deskW = vScreen.Width;
            deskH = vScreen.Height;
            AllocatePersistentDIB();
            CaptureDesktopBackdrop();
            if (!previewMode) LoadExistingInstallSettings();
            PrecomputeOpticalLUT();

            if (!previewMode)
            {
                animTimer = new System.Windows.Forms.Timer { Interval = 16 };
                animTimer.Tick += OnAnimTick;
                animTimer.Start();
            }
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

        // Cached font lookup — zero per-frame native HFONT allocations!
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

        // Crisp high-contrast text helper with a 1px dark drop-shadow (zero brush allocations!)
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
                using (Bitmap bmp = new Bitmap(deskW, deskH, PixelFormat.Format32bppArgb))
                {
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        Rectangle vs = SystemInformation.VirtualScreen;
                        g.CopyFromScreen(vs.X, vs.Y, 0, 0, new Size(deskW, deskH), CopyPixelOperation.SourceCopy);
                    }
                    BitmapData bd = bmp.LockBits(new Rectangle(0, 0, deskW, deskH), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                    Marshal.Copy(bd.Scan0, sharpDesk, 0, sharpDesk.Length);
                    bmp.UnlockBits(bd);
                    ok = true;
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

            // Smooth half-res 3-pass Gaussian blur
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
                BoxBlurH(b1, b2, hw, hh, 9);
                BoxBlurV(b2, b1, hw, hh, 9);
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
                        absorb256 = (int)((1f - (float)Math.Exp(-Math.Pow(inward - 3f, 2) * 0.3f) * 0.12f) * 256f),
                        tintW256 = (int)(0.05f * 256f),
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

        private void DrawSetupPill(Graphics g, RectangleF r, string text, bool active, string id, bool leftAlign = false, bool useMelonFont = true, bool isPrimaryAction = false)
        {
            float hovLerp = GetHov(id);
            float t = (float)animSw.Elapsed.TotalSeconds;
            using (GraphicsPath p = RoundRect(r, r.Height * 0.46f))
            {
                Color basicFill;
                if (isPrimaryAction) basicFill = Color.FromArgb(255, 58 + (int)(18 * hovLerp), 122 + (int)(16 * hovLerp), 228 + (int)(12 * hovLerp));
                else if (active) basicFill = Color.FromArgb(255, 38 + (int)(12 * hovLerp), 52 + (int)(14 * hovLerp), 78 + (int)(18 * hovLerp));
                else basicFill = Color.FromArgb(255, 28 + (int)(10 * hovLerp), 34 + (int)(12 * hovLerp), 48 + (int)(16 * hovLerp));

                int fa = (int)((isPrimaryAction ? 82 : (active ? 64 : 28)) + (hovLerp * 36f));
                Color glassFill = isPrimaryAction
                    ? Color.FromArgb(ClampInt(fa + 40, 0, 255), 110, 170, 255)
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
                    float sweepX = r.X - 30f + ((t * 110f) % (r.Width + 60f));
                    using (LinearGradientBrush glintBr = new LinearGradientBrush(
                        new PointF(sweepX - 24f, r.Top),
                        new PointF(sweepX + 24f, r.Bottom),
                        Color.FromArgb(0, 255, 255, 255),
                        Color.FromArgb(0, 255, 255, 255)))
                    {
                        ColorBlend cb = new ColorBlend(3);
                        cb.Colors = new Color[] {
                            Color.FromArgb(0, 255, 255, 255),
                            Color.FromArgb(ClampInt((int)(48f * hovLerp), 0, 255), 255, 255, 255),
                            Color.FromArgb(0, 255, 255, 255)
                        };
                        cb.Positions = new float[] { 0f, 0.5f, 1f };
                        glintBr.InterpolationColors = cb;
                        g.FillRectangle(glintBr, r);
                    }
                    g.ResetClip();
                }

                int ra = (int)((isPrimaryAction ? 225 : (active ? 195 : 88)) + (hovLerp * 60f));
                Color glassRim = Color.FromArgb(ClampInt(ra, 0, 255), 255, 255, 255);
                Color basicRim = (!active && !isPrimaryAction)
                    ? Color.FromArgb(ClampInt(ra, 0, 255), 115, 132, 168)
                    : glassRim;
                Color rimCol = Color.FromArgb(
                    ClampInt((int)(glassRim.A + (basicRim.A - glassRim.A) * tb), 0, 255),
                    ClampInt((int)(glassRim.R + (basicRim.R - glassRim.R) * tb), 0, 255),
                    ClampInt((int)(glassRim.G + (basicRim.G - glassRim.G) * tb), 0, 255),
                    ClampInt((int)(glassRim.B + (basicRim.B - glassRim.B) * tb), 0, 255));
                using (Pen pen = new Pen(rimCol, (active || isPrimaryAction ? 1.35f : 1.0f) + hovLerp * 0.25f)) g.DrawPath(pen, p);

                bool useMelonHere = !useBasicTheme && useMelonFont && isPrimaryAction;
                float fSize = useMelonHere ? 13f : 12.5f;
                Font f = GetFont(fSize, (active || isPrimaryAction) ? FontStyle.Bold : FontStyle.Regular, useMelonHere);
                Color tc = Color.FromArgb(ClampInt((int)((active || isPrimaryAction ? 255 : 234) + hovLerp * 21f), 0, 255), 255, 255, 255);
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

                // Header: Cake Logo + Title + Subtitle + Close button
                if (logoImg != null)
                {
                    g.DrawImage(logoImg, pad, topY, 44f, 44f);
                }
                Font tf = GetFont(useBasicTheme ? 20f : 19f, FontStyle.Bold, true);
                Font sf = GetFont(12f, FontStyle.Regular, false);
                string hdrTitle = useBasicTheme ? SL("Install Birthly") : SL("B I R T H L Y   S E T U P").Replace("B I R T H L Y   S E T U P", "BIRTHLY SETUP");
                DrawCrispString(g, hdrTitle, tf, pad + 56f, topY + 1f, Color.FromArgb(255, 255, 255, 255));
                DrawCrispString(g, useBasicTheme
                    ? SL("Basic Program Theme  •  Segoe UI Font  •  v0.1.0")
                    : SL("Liquid Glass Theme  •  Melon Squishy Font  •  v0.1.0"),
                    sf, pad + 56f, topY + 26f, Color.FromArgb(215, 232, 240, 255));

                RectangleF closeBtn = new RectangleF(edgeMargin + winW - 24f - 30f, topY + 6f, 30f, 30f);
                RectangleF langBtn = new RectangleF(closeBtn.X - 118f, topY + 6f, 110f, 30f);
                DrawSetupPill(g, langBtn, GetSetupLangName(selectedLang) + (isLangDdOpen ? " ▲" : " ▼"), isLangDdOpen, "lang_dd", false, false);
                DrawSetupPill(g, closeBtn, "×", false, "close", false, false);
                if (!isInstalling) zones.Add(new HitZone { Id = "lang_dd", Bounds = langBtn });
                zones.Add(new HitZone { Id = "close", Bounds = closeBtn });

                float divY = topY + 54f;
                using (Pen dp = new Pen(Color.FromArgb(55, 255, 255, 255), 1f))
                    g.DrawLine(dp, pad, divY, pad + innerW, divY);

                // ── THEME & DEFAULT FONT SELECTOR (Live-switches the installer AND sets Birthly's initial theme & font!) ──
                float cy = divY + 11f;
                Font lf = GetFont(11.5f, FontStyle.Bold, false);
                DrawCrispString(g, SL("CHOOSE PROGRAM THEME & FONT (CHANGEABLE ANYTIME IN SETTINGS):"), lf, pad, cy, Color.FromArgb(230, 235, 242, 255));
                cy += 19f;

                float halfW = (innerW - 10f) * 0.5f;
                RectangleF glassThemeBtn = new RectangleF(pad, cy, halfW, 32f);
                RectangleF basicThemeBtn = new RectangleF(pad + halfW + 10f, cy, halfW, 32f);
                DrawSetupPill(g, glassThemeBtn, (!useBasicTheme ? "✓  " : "") + SL("LIQUID GLASS (MELON)"), !useBasicTheme, "theme_glass", false, true);
                DrawSetupPill(g, basicThemeBtn, (useBasicTheme ? "✓  " : "") + SL("BASIC THEME (SEGOE UI)"), useBasicTheme, "theme_basic", false, false);
                if (!isInstalling)
                {
                    zones.Add(new HitZone { Id = "theme_glass", Bounds = glassThemeBtn });
                    zones.Add(new HitZone { Id = "theme_basic", Bounds = basicThemeBtn });
                }
                cy += 42f;

                // Install Folder row
                DrawCrispString(g, SL("INSTALLATION FOLDER:"), lf, pad, cy, Color.FromArgb(230, 235, 242, 255));
                cy += 19f;

                float browseW = 108f;
                RectangleF pathRect = new RectangleF(pad, cy, innerW - browseW - 8f, 30f);
                RectangleF browseBtn = new RectangleF(pathRect.Right + 8f, cy, browseW, 30f);
                // Always render file path in crisp Segoe UI so backslashes and folder names are 100% legible!
                DrawSetupPill(g, pathRect, installPath, false, "path_box", true, false);
                DrawSetupPill(g, browseBtn, SL("Browse..."), false, "browse", false, false);
                if (!isInstalling) zones.Add(new HitZone { Id = "browse", Bounds = browseBtn });
                cy += 40f;

                // Option toggle pills
                string[] optIds = new string[] { "opt_desk", "opt_start", "opt_auto", "opt_launch", "opt_readme" };
                bool[] optVals = new bool[] { optDesktop, optStartMenu, optAutoStart, optLaunch, optReadme };
                string[] optTxt = new string[] {
                    SL("Create Desktop shortcut"),
                    SL("Create Start Menu shortcuts (Birthly & Uninstall)"),
                    SL("Start Birthly automatically with Windows"),
                    SL("Launch Birthly after installation"),
                    SL("Open README.txt after installation")
                };

                for (int i = 0; i < optIds.Length; i++)
                {
                    RectangleF or = new RectangleF(pad, cy, innerW, 28f);
                    string lbl = useBasicTheme ? optTxt[i] : optTxt[i].ToUpperInvariant();
                    DrawSetupPill(g, or, (optVals[i] ? "✓   " : "○   ") + lbl, optVals[i], optIds[i], true, !useBasicTheme);
                    if (!isInstalling && !installDone) zones.Add(new HitZone { Id = optIds[i], Bounds = or });
                    cy += 33f;
                }

                // Frosted Progress Bar
                cy += 5f;
                RectangleF progTrack = new RectangleF(pad, cy, innerW, 8f);
                using (GraphicsPath tp = RoundRect(progTrack, 4f))
                using (SolidBrush tbr = new SolidBrush(Color.FromArgb(48, 255, 255, 255)))
                    g.FillPath(tbr, tp);

                if (progressVal > 0.01f)
                {
                    RectangleF progFill = new RectangleF(pad, cy, Math.Max(8f, innerW * Clamp01(progressVal)), 8f);
                    using (GraphicsPath fp = RoundRect(progFill, 4f))
                    using (SolidBrush fbr = new SolidBrush(useBasicTheme ? Color.FromArgb(255, 82, 150, 255) : Color.FromArgb(235, 255, 255, 255)))
                        g.FillPath(fbr, fp);
                }
                cy += 13f;

                Font stf = GetFont(11.5f, FontStyle.Regular, false);
                DrawCrispString(g, SL(statusText), stf, new RectangleF(pad, cy, innerW, 18f), sfNear, Color.FromArgb(230, 235, 242, 255));

                // Bottom Cancel / Install buttons
                float botY = edgeMargin + winH - 18f - 34f;
                RectangleF cancelBtn = new RectangleF(pad, botY, 118f, 34f);
                RectangleF installBtn = new RectangleF(pad + innerW - 186f, botY, 186f, 34f);
                DrawSetupPill(g, cancelBtn, installDone ? SL("CLOSE") : SL("CANCEL"), false, "cancel", false, !useBasicTheme, false);
                DrawSetupPill(g, installBtn, installDone ? SL("INSTALLED ✓") : (isInstalling ? SL("INSTALLING...") : SL("INSTALL BIRTHLY")), true, "install", false, !useBasicTheme, true);
                zones.Add(new HitZone { Id = "cancel", Bounds = cancelBtn });
                if (!isInstalling && !installDone) zones.Add(new HitZone { Id = "install", Bounds = installBtn });

                // Single-Column Searchable & Scrollable True-Liquid-Glass Language Dropdown (exact match to Main Settings!)
                if (isLangDdOpen && !isInstalling)
                {
                    List<int> matchIdx = new List<int>();
                    string lq = (langSearch ?? "").Trim();
                    for (int li = 0; li < SetupLanguages.GetLength(0); li++)
                    {
                        if (lq.Length == 0 || SetupLanguages[li, 1].IndexOf(lq, StringComparison.OrdinalIgnoreCase) >= 0 || SetupLanguages[li, 0].IndexOf(lq, StringComparison.OrdinalIgnoreCase) >= 0)
                            matchIdx.Add(li);
                    }
                    int maxVis = 6;
                    int maxScr = Math.Max(0, matchIdx.Count - maxVis);
                    if (langScroll > maxScr) langScroll = maxScr;
                    if (langScroll < 0) langScroll = 0;
                    int showCnt = Math.Min(maxVis, matchIdx.Count - langScroll);

                    float ddW = 196f;
                    float ddH = 34f + Math.Max(1, showCnt) * 28f + 10f;
                    RectangleF ddRect = new RectangleF(langBtn.Right - ddW, langBtn.Bottom + 5f, ddW, ddH);
                    using (GraphicsPath dp = RoundRect(ddRect, useBasicTheme ? 8f : 14f))
                    {
                        CompositingMode prevComp = g.CompositingMode;
                        g.CompositingMode = CompositingMode.SourceCopy;
                        using (SolidBrush clrBr = new SolidBrush(useBasicTheme ? Color.FromArgb(248, 22, 27, 40) : Color.FromArgb(0, 0, 0, 0)))
                            g.FillPath(clrBr, dp);
                        g.CompositingMode = prevComp;
                        if (!useBasicTheme)
                        {
                            using (SolidBrush glassTint = new SolidBrush(Color.FromArgb(42, 24, 30, 48)))
                                g.FillPath(glassTint, dp);
                        }
                        using (Pen dpen = new Pen(Color.FromArgb(150, 255, 255, 255), 1.15f))
                            g.DrawPath(dpen, dp);
                    }

                    // Search Bar inside Dropdown
                    RectangleF sRect = new RectangleF(ddRect.X + 6f, ddRect.Y + 6f, ddRect.Width - 12f, 24f);
                    string sDisp = string.IsNullOrEmpty(langSearch) ? "🔍 Search..." : ("🔍 " + langSearch + "|");
                    DrawSetupPill(g, sRect, sDisp, true, "lang_search", true, false);
                    zones.Add(new HitZone { Id = "lang_search", Bounds = sRect });

                    for (int mi = 0; mi < showCnt; mi++)
                    {
                        int idxL = matchIdx[langScroll + mi];
                        string lCode = SetupLanguages[idxL, 0];
                        string lName = SetupLanguages[idxL, 1];
                        RectangleF ir = new RectangleF(ddRect.X + 6f, sRect.Bottom + 5f + mi * 28f, ddRect.Width - 12f, 24f);
                        DrawSetupPill(g, ir, lName, selectedLang == lCode, "set_lang_" + lCode, false, false);
                        zones.Add(new HitZone { Id = "set_lang_" + lCode, Bounds = ir });
                    }
                }

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
            try { SetWindowDisplayAffinity(this.Handle, 0x00000011); } catch { }
            RenderFrame(true);
            if (liveBgTimer == null)
            {
                liveBgTimer = new System.Windows.Forms.Timer();
                liveBgTimer.Interval = 60;
                liveBgTimer.Tick += (s, ev) => {
                    if (!isDragging && !useBasicTheme && this.Visible)
                    {
                        CaptureDesktopBackdrop();
                        lastBgX = int.MinValue; // Force CompositeSurface to rebake cachedBackdropBuf from the new live capture!
                        RenderFrame(false);
                    }
                };
                liveBgTimer.Start();
            }
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
                    if (useBasicTheme)
                    {
                        SetWindowPos(this.Handle, IntPtr.Zero, curX, curY, 0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
                    }
                    else
                    {
                        // Fast glass refraction update without rebuilding GDI+ UI layer!
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

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (isLangDdOpen)
            {
                langScroll += (e.Delta < 0) ? 1 : -1;
                if (langScroll < 0) langScroll = 0;
                RenderFrame(true);
            }
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            base.OnKeyPress(e);
            if (!isLangDdOpen) return;
            char c = e.KeyChar;
            if (c == '\b') { if (langSearch.Length > 0) langSearch = langSearch.Substring(0, langSearch.Length - 1); langScroll = 0; }
            else if (c == 27) { isLangDdOpen = false; }
            else if (!char.IsControl(c) && langSearch.Length < 16) { langSearch += c; langScroll = 0; }
            e.Handled = true;
            RenderFrame(true);
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
            if (hid == "lang_dd")
            {
                isLangDdOpen = !isLangDdOpen;
                if (isLangDdOpen) { langSearch = ""; langScroll = 0; }
                RenderFrame(true);
                return;
            }
            if (hid == "lang_search")
            {
                return;
            }
            if (hid != null && hid.StartsWith("set_lang_"))
            {
                selectedLang = hid.Substring("set_lang_".Length);
                isLangDdOpen = false;
                langSearch = "";
                RenderFrame(true);
                return;
            }
            if (isLangDdOpen) { isLangDdOpen = false; RenderFrame(true); }
            if (hid == "theme_glass")
            {
                useBasicTheme = false;
                statusText = "Selected: Liquid Glass Theme + Melon Squishy Font.";
                WakeAnimTimer();
            }
            else if (hid == "theme_basic")
            {
                useBasicTheme = true;
                statusText = "Selected: Basic Program Theme + Clean Segoe UI Font.";
                WakeAnimTimer();
            }
            else if (hid == "opt_desk") optDesktop = !optDesktop;
            else if (hid == "opt_start") optStartMenu = !optStartMenu;
            else if (hid == "opt_auto") optAutoStart = !optAutoStart;
            else if (hid == "opt_launch") optLaunch = !optLaunch;
            else if (hid == "opt_readme") optReadme = !optReadme;
            else if (hid == "browse")
            {
                using (FolderBrowserDialog fbd = new FolderBrowserDialog())
                {
                    fbd.Description = "Choose where to install Birthly";
                    fbd.SelectedPath = installPath;
                    if (fbd.ShowDialog(this) == DialogResult.OK)
                    {
                        string chosen = fbd.SelectedPath;
                        if (!chosen.EndsWith("Birthly", StringComparison.OrdinalIgnoreCase))
                            chosen = Path.Combine(chosen, "Birthly");
                        installPath = chosen;
                    }
                }
            }
            else if (hid == "install")
            {
                RunInstallation();
                return;
            }
            RenderFrame(true);
        }

        private void WriteInitialThemeConfig()
        {
            try
            {
                string dataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Birthly");
                Directory.CreateDirectory(dataDir);
                string cfgFile = Path.Combine(dataDir, "birthdays_config.json");
                string chosenFont = useBasicTheme ? "Segoe UI" : "Melon Squishy";
                string basicBool = useBasicTheme ? "true" : "false";
                string autoStartBool = optAutoStart ? "true" : "false";

                if (!File.Exists(cfgFile))
                {
                    string json = "{"
                        + "\"AccentHex\":\"#FFFFFF\","
                        + "\"AlertSoonDays\":14,"
                        + "\"AlertUrgentDays\":3,"
                        + "\"AutoStartWithPC\":" + autoStartBool + ","
                        + "\"DarkMode\":false,"
                        + "\"EnableBirthdayAnimation\":true,"
                        + "\"EnableBirthdayMessage\":true,"
                        + "\"EnablePopupBanner\":true,"
                        + "\"EnableSoundAlerts\":false,"
                        + "\"FontName\":\"" + chosenFont + "\","
                        + "\"GlassPresetIndex\":0,"
                        + "\"Language\":\"" + selectedLang + "\","
                        + "\"HasBdayDefaultsInit\":true,"
                        + "\"HasCompletedTutorial\":false,"
                        + "\"HasPerfDefaultsInit\":true,"
                        + "\"LayerAlwaysOnTop\":false,"
                        + "\"LayerDesktopIcons\":true,"
                        + "\"LayerMode\":\"Above\","
                        + "\"LayerRainmeter\":true,"
                        + "\"LayerTargetExes\":[],"
                        + "\"LayoutMode\":\"HeroLeft\","
                        + "\"People\":[],"
                        + "\"RefreshWhileDragging\":true,"
                        + "\"ShowAgeBadge\":true,"
                        + "\"TargetRefreshRate\":0,"
                        + "\"TextColorMode\":\"Auto\","
                        + "\"TintVariant\":\"Clear\","
                        + "\"UseBasicTheme\":" + basicBool + ","
                        + "\"WidgetHeight\":500,"
                        + "\"WidgetWidth\":780,"
                        + "\"WindowX\":-9999,"
                        + "\"WindowY\":-9999"
                        + "}";
                    File.WriteAllText(cfgFile, json);
                }
                else
                {
                    // Update existing config's UseBasicTheme and FontName to match the installer selection while preserving existing people!
                    string existing = File.ReadAllText(cfgFile);
                    if (existing.Contains("\"UseBasicTheme\":"))
                    {
                        existing = System.Text.RegularExpressions.Regex.Replace(existing, "\"UseBasicTheme\"\\s*:\\s*(true|false)", "\"UseBasicTheme\":" + basicBool);
                    }
                    else
                    {
                        existing = existing.TrimEnd();
                        if (existing.EndsWith("}"))
                            existing = existing.Substring(0, existing.Length - 1) + ",\"UseBasicTheme\":" + basicBool + "}";
                    }
                    if (existing.Contains("\"FontName\":"))
                    {
                        existing = System.Text.RegularExpressions.Regex.Replace(existing, "\"FontName\"\\s*:\\s*\"[^\"]*\"", "\"FontName\":\"" + chosenFont + "\"");
                    }
                    File.WriteAllText(cfgFile, existing);
                }
            }
            catch { }
        }

        private void RunInstallation()
        {
            isInstalling = true;
            progressVal = 0.12f;
            statusText = "Stopping any running Birthly instance...";
            RenderFrame(true);
            Application.DoEvents();

            try
            {
                foreach (Process p in Process.GetProcessesByName("Birthly"))
                    try { p.Kill(); p.WaitForExit(1500); } catch { }

                progressVal = 0.35f;
                statusText = "Extracting Birthly, Fonts, README & Uninstaller...";
                RenderFrame(true);
                Application.DoEvents();

                Directory.CreateDirectory(installPath);
                Directory.CreateDirectory(Path.Combine(installPath, "fonts"));

                string exePath = Path.Combine(installPath, "Birthly.exe");
                string icoPath = Path.Combine(installPath, "Birthly.ico");
                string fontPath = Path.Combine(installPath, "fonts", "MelonSquishy.otf");
                string readmePath = Path.Combine(installPath, "README.txt");
                string uninstPath = Path.Combine(installPath, "Uninstall.exe");

                ExtractResource("Birthly.exe", exePath);
                ExtractResource("Birthly.ico", icoPath);
                ExtractResource("MelonSquishy.otf", fontPath);
                ExtractResource("README.txt", readmePath);
                ExtractResource("Uninstall.exe", uninstPath);

                // Write chosen theme (Basic vs Glass) and default font (Segoe UI vs Melon Squishy) into Birthly's config!
                WriteInitialThemeConfig();

                progressVal = 0.72f;
                statusText = "Creating shortcuts & registering with Windows...";
                RenderFrame(true);
                Application.DoEvents();

                if (optDesktop)
                {
                    string deskLnk = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Birthly.lnk");
                    CreateShortcut(deskLnk, exePath, installPath, icoPath, "Birthly - Desktop Birthday Widget");
                }
                if (optStartMenu)
                {
                    string smDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "Birthly");
                    Directory.CreateDirectory(smDir);
                    CreateShortcut(Path.Combine(smDir, "Birthly.lnk"), exePath, installPath, icoPath, "Launch Birthly");
                    CreateShortcut(Path.Combine(smDir, "README.lnk"), readmePath, installPath, "", "Birthly Guide & Shortcuts");
                    CreateShortcut(Path.Combine(smDir, "Uninstall Birthly.lnk"), uninstPath, installPath, icoPath, "Uninstall Birthly");
                }

                try
                {
                    using (RegistryKey rk = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                    {
                        if (rk != null)
                        {
                            if (optAutoStart) rk.SetValue("Birthly", "\"" + exePath + "\"");
                            else rk.DeleteValue("Birthly", false);
                        }
                    }
                }
                catch { }

                try
                {
                    using (RegistryKey uk = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\Birthly"))
                    {
                        if (uk != null)
                        {
                            uk.SetValue("DisplayName", "Birthly");
                            uk.SetValue("DisplayIcon", exePath + ",0");
                            uk.SetValue("DisplayVersion", "1.0.0");
                            uk.SetValue("Publisher", "Birthly");
                            uk.SetValue("InstallLocation", installPath);
                            uk.SetValue("UninstallString", "\"" + uninstPath + "\"");
                            uk.SetValue("NoModify", 1, RegistryValueKind.DWord);
                            uk.SetValue("NoRepair", 1, RegistryValueKind.DWord);
                            uk.SetValue("EstimatedSize", 650, RegistryValueKind.DWord);
                        }
                    }
                }
                catch { }

                progressVal = 1.0f;
                isInstalling = false;
                installDone = true;
                statusText = "Installed cleanly! You can close this window.";
                RenderFrame(true);
                Application.DoEvents();

                if (optReadme && File.Exists(readmePath))
                    try { Process.Start(new ProcessStartInfo(readmePath) { UseShellExecute = true }); } catch { }

                if (optLaunch && File.Exists(exePath))
                    try { Process.Start(new ProcessStartInfo(exePath) { WorkingDirectory = installPath, UseShellExecute = true }); } catch { }
            }
            catch (Exception ex)
            {
                isInstalling = false;
                statusText = "Error: " + ex.Message;
                RenderFrame(true);
            }
        }

        private static void ExtractResource(string resName, string destPath)
        {
            using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream(resName))
            {
                if (s == null) throw new Exception("Missing resource: " + resName);
                using (FileStream fs = new FileStream(destPath, FileMode.Create, FileAccess.Write)) s.CopyTo(fs);
            }
        }

        private static void CreateShortcut(string lnkPath, string targetPath, string workDir, string iconPath, string desc)
        {
            try
            {
                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                object shell = Activator.CreateInstance(shellType);
                object sc = shellType.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[] { lnkPath });
                Type scType = sc.GetType();
                scType.InvokeMember("TargetPath", BindingFlags.SetProperty, null, sc, new object[] { targetPath });
                scType.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, sc, new object[] { workDir });
                if (!string.IsNullOrEmpty(iconPath))
                    scType.InvokeMember("IconLocation", BindingFlags.SetProperty, null, sc, new object[] { iconPath + ",0" });
                scType.InvokeMember("Description", BindingFlags.SetProperty, null, sc, new object[] { desc });
                scType.InvokeMember("Save", BindingFlags.InvokeMethod, null, sc, null);
            }
            catch { }
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
