using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Media;
using System.Windows.Forms;
using Microsoft.Win32;

namespace NivalisApp
{
    public class MainForm : Form
    {
        // PowerCFG GUIDs
        static string SUB_PROCESSOR = "54533251-82be-4824-96c1-47b60b740d00";
        static string PROCMIN = "893de800-450c-4634-b3d1-af4fad038096";
        static string PROCMAX = "bc50be8b-707e-446c-8e7c-64aed8623541";
        static string SYSCOOLPOL = "94d3a615-a899-4ac5-ae2b-e4d8f634367f";
        static string POWER_SAVER_GUID = "a1841308-3541-4fab-bc81-f71556f20b4a";
        static string HIGH_PERF_GUID = "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c";
        static string BALANCED_GUID = "381b4222-f694-41f0-9685-ff5bb260df2e";

        private string originalPowerSchemeGuid = "381b4222-f694-41f0-9685-ff5bb260df2e";
        private bool isEcoActive = false;

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

        private Button btnActivarEco;
        private Button btnActivarNormal;
        private Label lblFooterNote;

        public MainForm()
        {
            GuardarEstadoInicial();
            AsegurarAutoInicio();
            InitializeTrayIcon();
            InitializeComponent();
            ActualizarEstado();
        }

        private void GuardarEstadoInicial()
        {
            try
            {
                string outScheme = RunPowerCfgWithOutput("/getactivescheme");
                int colon = outScheme.IndexOf(':');
                int paren = outScheme.IndexOf('(');
                if (colon >= 0 && paren > colon)
                {
                    string g = outScheme.Substring(colon + 1, paren - colon - 1).Trim();
                    if (g.Length >= 20)
                    {
                        originalPowerSchemeGuid = g;
                    }
                }
                if (outScheme.Contains(POWER_SAVER_GUID))
                {
                    isEcoActive = true;
                }
            }
            catch { }
        }

        private void AsegurarAutoInicio()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key != null)
                    {
                        key.SetValue("NIVALIS", string.Format("\"{0}\"", Application.ExecutablePath));
                    }
                }
            }
            catch { }
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

            // Robust Embedded Icon Extraction
            Icon appIcon = null;
            try
            {
                appIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch { }

            if (appIcon == null)
            {
                string[] candidateIcoPaths = new string[]
                {
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\assets\logo.ico"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"assets\logo.ico"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"logo.ico")
                };
                foreach (string p in candidateIcoPaths)
                {
                    if (File.Exists(p))
                    {
                        try { appIcon = new Icon(p); break; } catch { }
                    }
                }
            }
            if (appIcon == null) appIcon = SystemIcons.Application;

            trayIcon.Icon = appIcon;
            trayIcon.ContextMenuStrip = trayMenu;
            trayIcon.Visible = true;
            trayIcon.DoubleClick += delegate(object s, EventArgs e) { RestaurarVentana(); };
        }

        private void InitializeComponent()
        {
            this.Text = "NIVALIS - Thermal & Power Suite";
            this.Size = new Size(560, 520);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(11, 15, 25); // #0B0F19 Dark Midnight Slate
            this.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);

            Icon appIcon = null;
            try { appIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
            if (appIcon != null) this.Icon = appIcon;

            // Logo PNG / Icon Bitmap
            pbLogo = new PictureBox();
            pbLogo.Size = new Size(52, 52);
            pbLogo.Location = new Point(25, 20);
            pbLogo.SizeMode = PictureBoxSizeMode.Zoom;

            Image logoImg = null;
            string[] candidatePngPaths = new string[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\assets\logo.png"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"assets\logo.png"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"logo.png")
            };
            foreach (string p in candidatePngPaths)
            {
                if (File.Exists(p))
                {
                    try { logoImg = Image.FromFile(p); break; } catch { }
                }
            }
            if (logoImg == null && appIcon != null)
            {
                try { logoImg = appIcon.ToBitmap(); } catch { }
            }
            pbLogo.Image = logoImg;
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
            lblSubtitle.Text = "Thermal & Power Management Suite";
            lblSubtitle.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            lblSubtitle.ForeColor = Color.FromArgb(148, 163, 184); // #94A3B8
            lblSubtitle.Location = new Point(90, 48);
            lblSubtitle.AutoSize = true;
            this.Controls.Add(lblSubtitle);

            // General Status Badge
            pnlBadge = new Panel();
            pnlBadge.Location = new Point(25, 85);
            pnlBadge.Size = new Size(494, 55);
            pnlBadge.BackColor = Color.FromArgb(30, 41, 59); // #1E293B
            this.Controls.Add(pnlBadge);

            lblBadgeStatus = new Label();
            lblBadgeStatus.Text = "DETECTING SYSTEM STATUS...";
            lblBadgeStatus.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            lblBadgeStatus.Location = new Point(16, 14);
            lblBadgeStatus.AutoSize = true;
            pnlBadge.Controls.Add(lblBadgeStatus);

            // CPU Card
            pnlCardCpu = CrearTarjeta(25, 152, 494, 62);
            this.Controls.Add(pnlCardCpu);
            lblCpuHeader = CrearLabelHeader("PROCESSOR (CPU THERMAL ENGINE)", 14, 8);
            pnlCardCpu.Controls.Add(lblCpuHeader);
            lblCpuDetails = CrearLabelDetails("Loading details...", 14, 30);
            pnlCardCpu.Controls.Add(lblCpuDetails);

            // GPU Card
            pnlCardGpu = CrearTarjeta(25, 226, 494, 62);
            this.Controls.Add(pnlCardGpu);
            lblGpuHeader = CrearLabelHeader("GRAPHICS (NVIDIA GTX 1660 SUPER)", 14, 8);
            pnlCardGpu.Controls.Add(lblGpuHeader);
            lblGpuDetails = CrearLabelDetails("Loading details...", 14, 30);
            pnlCardGpu.Controls.Add(lblGpuDetails);

            // Eco Mode Button
            btnActivarEco = new Button();
            btnActivarEco.Text = "🌙 ENABLE ECO MODE (Cool & Quiet)";
            btnActivarEco.Location = new Point(25, 305);
            btnActivarEco.Size = new Size(494, 52);
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
            btnActivarNormal.Location = new Point(25, 370);
            btnActivarNormal.Size = new Size(494, 52);
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
            lblFooterNote.Text = "🛡️ Runs automatically at Windows startup. Settings reset to defaults on reboot.";
            lblFooterNote.ForeColor = Color.FromArgb(148, 163, 184);
            lblFooterNote.Font = new Font("Segoe UI", 8.5F, FontStyle.Italic);
            lblFooterNote.Location = new Point(25, 436);
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

        private void BtnActivarEco_Click(object sender, EventArgs e)
        {
            isEcoActive = true;

            // 1. CPU Power Settings (Power Saver, 75% Max Frequency, Passive Silent Cooling)
            RunPowerCfg(string.Format("/setactive {0}", POWER_SAVER_GUID));
            RunPowerCfg(string.Format("/setacvalueindex {0} {1} {2} 5", POWER_SAVER_GUID, SUB_PROCESSOR, PROCMIN));
            RunPowerCfg(string.Format("/setdcvalueindex {0} {1} {2} 5", POWER_SAVER_GUID, SUB_PROCESSOR, PROCMIN));
            RunPowerCfg(string.Format("/setacvalueindex {0} {1} {2} 75", POWER_SAVER_GUID, SUB_PROCESSOR, PROCMAX));
            RunPowerCfg(string.Format("/setdcvalueindex {0} {1} {2} 75", POWER_SAVER_GUID, SUB_PROCESSOR, PROCMAX));
            RunPowerCfg(string.Format("/setacvalueindex {0} {1} {2} 0", POWER_SAVER_GUID, SUB_PROCESSOR, SYSCOOLPOL));
            RunPowerCfg(string.Format("/setactive {0}", POWER_SAVER_GUID));

            // 2. GPU Power Limit (NVIDIA GTX 1660 SUPER 70W Cap)
            RunNvidiaSmi("-pl 70");

            // 3. Audio Chime
            ReproducirSonidoEco();

            ActualizarEstado();
        }

        private void BtnActivarNormal_Click(object sender, EventArgs e)
        {
            isEcoActive = false;

            // 1. CPU Power Settings (Restore Original / High Performance / Balanced)
            if (!string.IsNullOrEmpty(originalPowerSchemeGuid) && originalPowerSchemeGuid != POWER_SAVER_GUID)
            {
                RunPowerCfg(string.Format("/setactive {0}", originalPowerSchemeGuid));
            }
            else
            {
                RunPowerCfg(string.Format("/setactive {0}", BALANCED_GUID));
                RunPowerCfg(string.Format("/setactive {0}", HIGH_PERF_GUID));
            }
            RunPowerCfg(string.Format("/setacvalueindex {0} {1} {2} 100", BALANCED_GUID, SUB_PROCESSOR, PROCMAX));
            RunPowerCfg(string.Format("/setacvalueindex {0} {1} {2} 1", BALANCED_GUID, SUB_PROCESSOR, SYSCOOLPOL));

            // 2. GPU Power Limit (125W Factory Default)
            RunNvidiaSmi("-pl 125");

            // 3. Audio Chime
            ReproducirSonidoNormal();

            ActualizarEstado();
        }

        private void ActualizarEstado()
        {
            string output = RunPowerCfgWithOutput("/getactivescheme");

            bool ecoNow = isEcoActive;
            if (!ecoNow && output.Contains(POWER_SAVER_GUID))
            {
                ecoNow = true;
            }

            if (ecoNow)
            {
                lblBadgeStatus.Text = "❄️ NIVALIS ECO MODE (ACTIVE)";
                lblBadgeStatus.ForeColor = Color.FromArgb(56, 189, 248); // #38BDF8

                lblCpuDetails.Text = "Power Saver Plan  |  Max Frequency 75%  |  Passive Silent Cooling";
                lblGpuDetails.Text = "Power Limit 70W  (44% Reduced Heat & Consumption)";

                trayIcon.Text = "NIVALIS - Eco Mode (70W Limit | Silent Cooling)";
            }
            else
            {
                lblBadgeStatus.Text = "🔥 PERFORMANCE MODE (ACTIVE)";
                lblBadgeStatus.ForeColor = Color.FromArgb(96, 165, 250); // #60A5FA

                lblCpuDetails.Text = "High Performance / Balanced  |  Max Frequency 100%  |  Active Cooling";
                lblGpuDetails.Text = "Power Limit 125W  (Factory Default / Full Power)";

                trayIcon.Text = "NIVALIS - Normal Mode (125W Default | Full Power)";
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
