using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Media;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace NivalisApp
{
    public class MainForm : Form
    {
        // Struct Win32 DEVMODEW
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct DEVMODEW
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
            public uint dmDCOpenGLFlags;
            public uint dmReserved1;
            public uint dmReserved2;
            public uint dmPanningWidth;
            public uint dmPanningHeight;
        }

        // Interop CCD API
        [StructLayout(LayoutKind.Sequential)]
        public struct LUID { public uint LowPart; public int HighPart; }
        [StructLayout(LayoutKind.Sequential)]
        public struct DISPLAYCONFIG_RATIONAL { public uint Numerator; public uint Denominator; }
        [StructLayout(LayoutKind.Sequential)]
        public struct DISPLAYCONFIG_PATH_SOURCE_INFO { public LUID adapterId; public uint id; public uint modeInfoIdx; public uint statusFlags; }
        [StructLayout(LayoutKind.Sequential)]
        public struct DISPLAYCONFIG_PATH_TARGET_INFO { public LUID adapterId; public uint id; public uint modeInfoIdx; public uint outputTechnology; public uint rotation; public uint scaling; public DISPLAYCONFIG_RATIONAL refreshRate; public uint scanLineOrdering; public bool targetAvailable; public uint statusFlags; }
        [StructLayout(LayoutKind.Sequential)]
        public struct DISPLAYCONFIG_PATH_INFO { public DISPLAYCONFIG_PATH_SOURCE_INFO sourceInfo; public DISPLAYCONFIG_PATH_TARGET_INFO targetInfo; public uint flags; }

        public const int ENUM_CURRENT_SETTINGS = -1;
        public const uint DM_BITSPERPEL = 0x00040000;
        public const uint DM_PELSWIDTH = 0x00080000;
        public const uint DM_PELSHEIGHT = 0x00100000;
        public const uint DM_DISPLAYFREQUENCY = 0x00400000;
        public const int CDS_UPDATEREGISTRY = 0x00000001;

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern bool EnumDisplaySettingsW(string deviceName, int modeNum, ref DEVMODEW devMode);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int ChangeDisplaySettingsExW(string lpszDeviceName, ref DEVMODEW lpDevMode, IntPtr hwnd, int dwflags, IntPtr lParam);
        [DllImport("user32.dll")]
        public static extern int GetDisplayConfigBufferSizes(uint flags, out uint numPathArrayElements, out uint numModeInfoArrayElements);
        [DllImport("user32.dll")]
        public static extern int QueryDisplayConfig(uint flags, ref uint numPathArrayElements, [Out] DISPLAYCONFIG_PATH_INFO[] pathArray, ref uint numModeInfoArrayElements, byte[] modeInfoArray, IntPtr currentTopologyId);
        [DllImport("user32.dll")]
        public static extern int SetDisplayConfig(uint numPathArrayElements, [In] DISPLAYCONFIG_PATH_INFO[] pathArray, uint numModeInfoArrayElements, byte[] modeInfoArray, uint flags);

        public const uint QDC_ONLY_ACTIVE_PATHS = 0x00000002;
        public const uint SDC_APPLY = 0x00000080;
        public const uint SDC_USE_SUPPLIED_DISPLAY_CONFIG = 0x00000020;
        public const uint SDC_ALLOW_CHANGES = 0x00000400;

        // GUIDs de PowerCFG
        static string SUB_PROCESSOR = "54533251-82be-4824-96c1-47b60b740d00";
        static string PROCMIN = "893de800-450c-4634-b3d1-af4fad038096";
        static string PROCMAX = "bc50be8b-707e-446c-8e7c-64aed8623541";
        static string SYSCOOLPOL = "94d3a615-a899-4ac5-ae2b-e4d8f634367f";
        static string POWER_SAVER_GUID = "a1841308-3541-4fab-bc81-f71556f20b4a";
        static string HIGH_PERF_GUID = "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c";

        private double originalHz = 144.0;
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;
        private PictureBox pbLogo;
        private Label lblTitle;
        private Label lblSubtitle;

        private Panel pnlBadge;
        private Label lblBadgeStatus;

        private Panel pnlCardCpu;
        private Label lblCpuHeader;
        private Label lblCpuDetails;

        private Panel pnlCardGpu;
        private Label lblGpuHeader;
        private Label lblGpuDetails;

        private Panel pnlCardMonitor;
        private Label lblMonitorHeader;
        private Label lblMonitorDetails;

        private Panel pnlToggles;
        private CheckBox chkCpu;
        private CheckBox chkGpu;
        private CheckBox chkHz;
        private CheckBox chkStartup;

        private Button btnActivarEco;
        private Button btnActivarNormal;
        private Label lblFooterNote;

        public MainForm()
        {
            GuardarHzOriginales();
            InitializeTrayIcon();
            InitializeComponent();
            ActualizarEstado();
        }

        private void GuardarHzOriginales()
        {
            double current = GetCurrentMonitorHz();
            if (current > 30)
            {
                originalHz = current;
            }
        }

        private void InitializeTrayIcon()
        {
            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("❄️ NIVALIS (Open Dashboard)", null, delegate(object s, EventArgs e) { RestaurarVentana(); });
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add("🌙 Enable Eco Mode", null, delegate(object s, EventArgs e) { BtnActivarEco_Click(s, e); });
            trayMenu.Items.Add("⚡ Enable Normal Mode", null, delegate(object s, EventArgs e) { BtnActivarNormal_Click(s, e); });
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add("🚪 Quit NIVALIS", null, delegate(object s, EventArgs e) { Application.Exit(); });

            trayIcon = new NotifyIcon();
            trayIcon.Text = "NIVALIS - Thermal & Power Suite";

            string icoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\assets\logo.ico");
            if (!File.Exists(icoPath))
            {
                icoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\assets\icon.ico");
            }
            if (File.Exists(icoPath))
            {
                try { trayIcon.Icon = new Icon(icoPath); } catch { trayIcon.Icon = SystemIcons.Application; }
            }
            else
            {
                trayIcon.Icon = SystemIcons.Application;
            }

            trayIcon.ContextMenuStrip = trayMenu;
            trayIcon.Visible = true;
            trayIcon.DoubleClick += delegate(object s, EventArgs e) { RestaurarVentana(); };
        }

        private void InitializeComponent()
        {
            this.Text = "NIVALIS - Universal Thermal & Power Suite";
            this.Size = new Size(580, 670);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(11, 15, 25); // #0B0F19 Dark Midnight Slate
            this.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);

            string icoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\assets\logo.ico");
            if (!File.Exists(icoPath))
            {
                icoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\assets\icon.ico");
            }
            if (File.Exists(icoPath))
            {
                try { this.Icon = new Icon(icoPath); } catch { }
            }

            // Logo PNG
            pbLogo = new PictureBox();
            pbLogo.Size = new Size(52, 52);
            pbLogo.Location = new Point(25, 20);
            pbLogo.SizeMode = PictureBoxSizeMode.Zoom;
            string pngPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\assets\logo.png");
            if (File.Exists(pngPath))
            {
                try { pbLogo.Image = Image.FromFile(pngPath); } catch { }
            }
            this.Controls.Add(pbLogo);

            // Title
            lblTitle = new Label();
            lblTitle.Text = "NIVALIS";
            lblTitle.Font = new Font("Segoe UI", 18F, FontStyle.Bold, GraphicsUnit.Point);
            lblTitle.ForeColor = Color.FromArgb(56, 189, 248); // #38BDF8 Ice Cyan
            lblTitle.Location = new Point(88, 16);
            lblTitle.AutoSize = true;
            this.Controls.Add(lblTitle);

            // Subtitle
            lblSubtitle = new Label();
            lblSubtitle.Text = "Universal Thermal & Power Management Suite";
            lblSubtitle.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            lblSubtitle.ForeColor = Color.FromArgb(148, 163, 184); // #94A3B8
            lblSubtitle.Location = new Point(90, 48);
            lblSubtitle.AutoSize = true;
            this.Controls.Add(lblSubtitle);

            // General Status Badge
            pnlBadge = new Panel();
            pnlBadge.Location = new Point(25, 85);
            pnlBadge.Size = new Size(514, 52);
            pnlBadge.BackColor = Color.FromArgb(30, 41, 59); // #1E293B
            this.Controls.Add(pnlBadge);

            lblBadgeStatus = new Label();
            lblBadgeStatus.Text = "DETECTING SYSTEM STATUS...";
            lblBadgeStatus.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            lblBadgeStatus.Location = new Point(16, 13);
            lblBadgeStatus.AutoSize = true;
            pnlBadge.Controls.Add(lblBadgeStatus);

            // CPU Card
            pnlCardCpu = CrearTarjeta(25, 147, 514, 55);
            this.Controls.Add(pnlCardCpu);
            lblCpuHeader = CrearLabelHeader("PROCESSOR (CPU ENGINE)", 14, 7);
            pnlCardCpu.Controls.Add(lblCpuHeader);
            lblCpuDetails = CrearLabelDetails("Loading details...", 14, 26);
            pnlCardCpu.Controls.Add(lblCpuDetails);

            // GPU Card
            pnlCardGpu = CrearTarjeta(25, 210, 514, 55);
            this.Controls.Add(pnlCardGpu);
            lblGpuHeader = CrearLabelHeader("GRAPHICS (NVIDIA GTX 1660 SUPER)", 14, 7);
            pnlCardGpu.Controls.Add(lblGpuHeader);
            lblGpuDetails = CrearLabelDetails("Loading details...", 14, 26);
            pnlCardGpu.Controls.Add(lblGpuDetails);

            // Monitor Card
            pnlCardMonitor = CrearTarjeta(25, 273, 514, 55);
            this.Controls.Add(pnlCardMonitor);
            lblMonitorHeader = CrearLabelHeader("DISPLAY MONITOR (REFRESH RATE)", 14, 7);
            pnlCardMonitor.Controls.Add(lblMonitorHeader);
            lblMonitorDetails = CrearLabelDetails("Loading details...", 14, 26);
            pnlCardMonitor.Controls.Add(lblMonitorDetails);

            // Modular Toggles Panel
            pnlToggles = CrearTarjeta(25, 338, 514, 105);
            this.Controls.Add(pnlToggles);

            Label lblTogglesHeader = CrearLabelHeader("MODULAR ECO MODULE TOGGLES", 14, 8);
            pnlToggles.Controls.Add(lblTogglesHeader);

            chkCpu = new CheckBox();
            chkCpu.Text = "Manage CPU Thermal & Power Saver";
            chkCpu.Checked = true;
            chkCpu.ForeColor = Color.FromArgb(226, 232, 240);
            chkCpu.Font = new Font("Segoe UI", 8.8F);
            chkCpu.Location = new Point(14, 28);
            chkCpu.AutoSize = true;
            pnlToggles.Controls.Add(chkCpu);

            chkGpu = new CheckBox();
            chkGpu.Text = "Manage GPU Power Limit (70W Eco)";
            chkGpu.Checked = true;
            chkGpu.ForeColor = Color.FromArgb(226, 232, 240);
            chkGpu.Font = new Font("Segoe UI", 8.8F);
            chkGpu.Location = new Point(270, 28);
            chkGpu.AutoSize = true;
            pnlToggles.Controls.Add(chkGpu);

            chkHz = new CheckBox();
            chkHz.Text = "Switch Display Hz (Uncheck to preserve color profile)";
            chkHz.Checked = false; // Default unchecked to preserve user's color profile and saturation!
            chkHz.ForeColor = Color.FromArgb(226, 232, 240);
            chkHz.Font = new Font("Segoe UI", 8.8F);
            chkHz.Location = new Point(14, 54);
            chkHz.AutoSize = true;
            pnlToggles.Controls.Add(chkHz);

            chkStartup = new CheckBox();
            chkStartup.Text = "Launch NIVALIS at Windows Startup (Tray)";
            chkStartup.Checked = EsAutoInicioConfigurado();
            chkStartup.ForeColor = Color.FromArgb(226, 232, 240);
            chkStartup.Font = new Font("Segoe UI", 8.8F);
            chkStartup.Location = new Point(14, 78);
            chkStartup.AutoSize = true;
            chkStartup.CheckedChanged += ChkStartup_CheckedChanged;
            pnlToggles.Controls.Add(chkStartup);

            // Eco Mode Button
            btnActivarEco = new Button();
            btnActivarEco.Text = "🌙 ENABLE ECO MODE (Cool & Quiet)";
            btnActivarEco.Location = new Point(25, 455);
            btnActivarEco.Size = new Size(514, 50);
            btnActivarEco.BackColor = Color.FromArgb(14, 165, 233); // #0EA5E9
            btnActivarEco.ForeColor = Color.White;
            btnActivarEco.FlatStyle = FlatStyle.Flat;
            btnActivarEco.FlatAppearance.BorderSize = 0;
            btnActivarEco.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            btnActivarEco.Cursor = Cursors.Hand;
            btnActivarEco.Click += BtnActivarEco_Click;
            this.Controls.Add(btnActivarEco);

            // Normal Mode Button
            btnActivarNormal = new Button();
            btnActivarNormal.Text = "⚡ ENABLE NORMAL MODE (Full Power)";
            btnActivarNormal.Location = new Point(25, 515);
            btnActivarNormal.Size = new Size(514, 50);
            btnActivarNormal.BackColor = Color.FromArgb(59, 130, 246); // #3B82F6
            btnActivarNormal.ForeColor = Color.White;
            btnActivarNormal.FlatStyle = FlatStyle.Flat;
            btnActivarNormal.FlatAppearance.BorderSize = 0;
            btnActivarNormal.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            btnActivarNormal.Cursor = Cursors.Hand;
            btnActivarNormal.Click += BtnActivarNormal_Click;
            this.Controls.Add(btnActivarNormal);

            // Footer Note
            lblFooterNote = new Label();
            lblFooterNote.Text = "🛡️ All settings automatically reset to factory defaults upon PC reboot.";
            lblFooterNote.ForeColor = Color.FromArgb(148, 163, 184);
            lblFooterNote.Font = new Font("Segoe UI", 8.5F, FontStyle.Italic);
            lblFooterNote.Location = new Point(25, 580);
            lblFooterNote.AutoSize = true;
            this.Controls.Add(lblFooterNote);

            this.FormClosing += MainForm_FormClosing;
        }

        private Panel CrearTarjeta(int x, int y, int w, int h)
        {
            Panel p = new Panel();
            p.Location = new Point(x, y);
            p.Size = new Size(w, h);
            p.BackColor = Color.FromArgb(30, 41, 59); // #1E293B
            return p;
        }

        private Label CrearLabelHeader(string text, int x, int y)
        {
            Label l = new Label();
            l.Text = text;
            l.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            l.ForeColor = Color.FromArgb(148, 163, 184);
            l.Location = new Point(x, y);
            l.AutoSize = true;
            return l;
        }

        private Label CrearLabelDetails(string text, int x, int y)
        {
            Label l = new Label();
            l.Text = text;
            l.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            l.ForeColor = Color.FromArgb(226, 232, 240);
            l.Location = new Point(x, y);
            l.AutoSize = true;
            return l;
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
                trayIcon.ShowBalloonTip(2000, "NIVALIS", "Nivalis is running silently in the system tray near the clock.", ToolTipIcon.Info);
            }
        }

        private void RestaurarVentana()
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.BringToFront();
        }

        private bool EsAutoInicioConfigurado()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false))
                {
                    return key != null && key.GetValue("NIVALIS") != null;
                }
            }
            catch { return false; }
        }

        private void ChkStartup_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key != null)
                    {
                        if (chkStartup.Checked)
                        {
                            key.SetValue("NIVALIS", string.Format("\"{0}\"", Application.ExecutablePath));
                        }
                        else
                        {
                            key.DeleteValue("NIVALIS", false);
                        }
                    }
                }
            }
            catch { }
        }

        private void BtnActivarEco_Click(object sender, EventArgs e)
        {
            // 1. CPU
            if (chkCpu.Checked)
            {
                RunPowerCfg(string.Format("/setactive {0}", POWER_SAVER_GUID));
                RunPowerCfg(string.Format("/setacvalueindex {0} {1} {2} 5", POWER_SAVER_GUID, SUB_PROCESSOR, PROCMIN));
                RunPowerCfg(string.Format("/setdcvalueindex {0} {1} {2} 5", POWER_SAVER_GUID, SUB_PROCESSOR, PROCMIN));
                RunPowerCfg(string.Format("/setacvalueindex {0} {1} {2} 75", POWER_SAVER_GUID, SUB_PROCESSOR, PROCMAX));
                RunPowerCfg(string.Format("/setdcvalueindex {0} {1} {2} 75", POWER_SAVER_GUID, SUB_PROCESSOR, PROCMAX));
                RunPowerCfg(string.Format("/setacvalueindex {0} {1} {2} 0", POWER_SAVER_GUID, SUB_PROCESSOR, SYSCOOLPOL));
                RunPowerCfg(string.Format("/setactive {0}", POWER_SAVER_GUID));
            }

            // 2. GPU
            if (chkGpu.Checked)
            {
                RunNvidiaSmi("-pl 70");
            }

            // 3. Display Hz (Optional)
            if (chkHz.Checked)
            {
                SetMonitorHz(60);
            }

            // 4. Audio Chime
            ReproducirSonidoEco();

            ActualizarEstado();
        }

        private void BtnActivarNormal_Click(object sender, EventArgs e)
        {
            // 1. CPU
            if (chkCpu.Checked)
            {
                RunPowerCfg(string.Format("/setactive {0}", HIGH_PERF_GUID));
                RunPowerCfg(string.Format("/setacvalueindex {0} {1} {2} 100", HIGH_PERF_GUID, SUB_PROCESSOR, PROCMAX));
                RunPowerCfg(string.Format("/setdcvalueindex {0} {1} {2} 100", HIGH_PERF_GUID, SUB_PROCESSOR, PROCMAX));
                RunPowerCfg(string.Format("/setacvalueindex {0} {1} {2} 1", HIGH_PERF_GUID, SUB_PROCESSOR, SYSCOOLPOL));
                RunPowerCfg(string.Format("/setactive {0}", HIGH_PERF_GUID));
            }

            // 2. GPU
            if (chkGpu.Checked)
            {
                RunNvidiaSmi("-pl 125");
            }

            // 3. Display Hz
            if (chkHz.Checked)
            {
                SetMonitorHz((int)Math.Round(originalHz));
            }

            // 4. Audio Chime
            ReproducirSonidoNormal();

            ActualizarEstado();
        }

        private double GetCurrentMonitorHz()
        {
            try
            {
                uint pathCount = 0;
                uint modeCount = 0;
                if (GetDisplayConfigBufferSizes(QDC_ONLY_ACTIVE_PATHS, out pathCount, out modeCount) == 0 && pathCount > 0)
                {
                    DISPLAYCONFIG_PATH_INFO[] paths = new DISPLAYCONFIG_PATH_INFO[pathCount];
                    byte[] modes = new byte[modeCount * 64];
                    if (QueryDisplayConfig(QDC_ONLY_ACTIVE_PATHS, ref pathCount, paths, ref modeCount, modes, IntPtr.Zero) == 0 && pathCount > 0)
                    {
                        uint num = paths[0].targetInfo.refreshRate.Numerator;
                        uint den = paths[0].targetInfo.refreshRate.Denominator;
                        if (den > 0)
                        {
                            return Math.Round(num / (double)den);
                        }
                    }
                }
            }
            catch { }
            return 144.0;
        }

        private void SetMonitorHz(int targetHz)
        {
            try
            {
                DEVMODEW currentMode = new DEVMODEW();
                currentMode.dmSize = (ushort)Marshal.SizeOf(typeof(DEVMODEW));
                if (EnumDisplaySettingsW(@"\\.\DISPLAY1", ENUM_CURRENT_SETTINGS, ref currentMode))
                {
                    int modeNum = 0;
                    DEVMODEW tempMode = new DEVMODEW();
                    tempMode.dmSize = (ushort)Marshal.SizeOf(typeof(DEVMODEW));
                    while (EnumDisplaySettingsW(@"\\.\DISPLAY1", modeNum, ref tempMode))
                    {
                        if (tempMode.dmPelsWidth == currentMode.dmPelsWidth &&
                            tempMode.dmPelsHeight == currentMode.dmPelsHeight &&
                            tempMode.dmDisplayFrequency == targetHz)
                        {
                            tempMode.dmFields = DM_BITSPERPEL | DM_PELSWIDTH | DM_PELSHEIGHT | DM_DISPLAYFREQUENCY;
                            ChangeDisplaySettingsExW(@"\\.\DISPLAY1", ref tempMode, IntPtr.Zero, CDS_UPDATEREGISTRY, IntPtr.Zero);
                            break;
                        }
                        modeNum++;
                    }
                }
            }
            catch { }
        }

        private void ActualizarEstado()
        {
            string output = RunPowerCfgWithOutput("/getactivescheme");
            int currentHz = (int)Math.Round(GetCurrentMonitorHz());

            if (output.Contains(POWER_SAVER_GUID))
            {
                lblBadgeStatus.Text = "❄️ NIVALIS ECO MODE (ACTIVE)";
                lblBadgeStatus.ForeColor = Color.FromArgb(56, 189, 248); // #38BDF8

                lblCpuDetails.Text = chkCpu.Checked ? "Power Saver Plan  |  Max Frequency 75%  |  Passive Cooling" : "CPU Management Disabled (Unchecked)";
                lblGpuDetails.Text = chkGpu.Checked ? "Power Limit 70W  (44% Reduced Heat & Consumption)" : "GPU Management Disabled (Unchecked)";
                lblMonitorDetails.Text = string.Format("{0} Hz  {1}", currentHz, chkHz.Checked ? "(Eco Refresh Rate)" : "(Color Profile Intact)");

                trayIcon.Text = string.Format("NIVALIS - Eco Mode ({0}Hz)", currentHz);
            }
            else
            {
                lblBadgeStatus.Text = "🔥 PERFORMANCE MODE (ACTIVE)";
                lblBadgeStatus.ForeColor = Color.FromArgb(96, 165, 250); // #60A5FA

                lblCpuDetails.Text = chkCpu.Checked ? "High Performance  |  Max Frequency 100%  |  Active Cooling" : "CPU Management Disabled (Unchecked)";
                lblGpuDetails.Text = chkGpu.Checked ? "Power Limit 125W  (Factory Default / Full Power)" : "GPU Management Disabled (Unchecked)";
                lblMonitorDetails.Text = string.Format("{0} Hz  {1}", currentHz, chkHz.Checked ? "(Normal Refresh Rate)" : "(Color Profile Intact)");

                trayIcon.Text = string.Format("NIVALIS - Normal Mode ({0}Hz)", currentHz);
            }
        }

        private static void RunPowerCfg(string args)
        {
            ProcessStartInfo psi = new ProcessStartInfo("powercfg.exe", args)
            {
                CreateNoWindow = true,
                UseShellExecute = false
            };
            using (Process p = Process.Start(psi))
            {
                p.WaitForExit();
            }
        }

        private static string RunPowerCfgWithOutput(string args)
        {
            ProcessStartInfo psi = new ProcessStartInfo("powercfg.exe", args)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true
            };
            using (Process p = Process.Start(psi))
            {
                string result = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                return result;
            }
        }

        private static void RunNvidiaSmi(string args)
        {
            try
            {
                string nvsmiPath = @"C:\Windows\System32\nvidia-smi.exe";
                if (File.Exists(nvsmiPath))
                {
                    ProcessStartInfo psi = new ProcessStartInfo(nvsmiPath, args)
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    using (Process p = Process.Start(psi))
                    {
                        p.WaitForExit();
                    }
                }
            }
            catch { }
        }

        private void ReproducirSonidoEco()
        {
            try
            {
                byte[] wavData = GenerarWavChime(new double[] { 784.0, 523.25 }, new double[] { 0.12, 0.22 });
                using (MemoryStream ms = new MemoryStream(wavData))
                {
                    using (SoundPlayer player = new SoundPlayer(ms))
                    {
                        player.Play();
                    }
                }
            }
            catch { }
        }

        private void ReproducirSonidoNormal()
        {
            try
            {
                byte[] wavData = GenerarWavChime(new double[] { 523.25, 1046.5 }, new double[] { 0.10, 0.25 });
                using (MemoryStream ms = new MemoryStream(wavData))
                {
                    using (SoundPlayer player = new SoundPlayer(ms))
                    {
                        player.Play();
                    }
                }
            }
            catch { }
        }

        private byte[] GenerarWavChime(double[] freqs, double[] durations)
        {
            int sampleRate = 22050;
            int totalSamples = 0;
            for (int i = 0; i < durations.Length; i++)
            {
                totalSamples += (int)(sampleRate * durations[i]);
            }

            short[] samples = new short[totalSamples];
            int currentSample = 0;

            for (int n = 0; n < freqs.Length; n++)
            {
                int count = (int)(sampleRate * durations[n]);
                double freq = freqs[n];
                for (int i = 0; i < count; i++)
                {
                    double t = i / (double)sampleRate;
                    double envelope = Math.Sin(Math.PI * (i / (double)count));
                    double wave = Math.Sin(2.0 * Math.PI * freq * t) * envelope;
                    samples[currentSample++] = (short)(wave * 12000);
                }
            }

            MemoryStream ms = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(ms);

            bw.Write(new char[] { 'R', 'I', 'F', 'F' });
            bw.Write(36 + totalSamples * 2);
            bw.Write(new char[] { 'W', 'A', 'V', 'E' });
            bw.Write(new char[] { 'f', 'm', 't', ' ' });
            bw.Write(16);
            bw.Write((short)1);
            bw.Write((short)1);
            bw.Write(sampleRate);
            bw.Write(sampleRate * 2);
            bw.Write((short)2);
            bw.Write((short)16);
            bw.Write(new char[] { 'd', 'a', 't', 'a' });
            bw.Write(totalSamples * 2);

            for (int i = 0; i < totalSamples; i++)
            {
                bw.Write(samples[i]);
            }

            bw.Flush();
            return ms.ToArray();
        }

        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
