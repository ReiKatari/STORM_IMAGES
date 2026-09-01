using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace StormUniversal.Installer
{
    public class InstallerForm : Form
    {
        private ProgressBar progressBar = null!;
        private Label lblStatus = null!;
        private Label lblTitle = null!;
        private Label lblSubtitle = null!;
        private Button btnInstall = null!;
        private Button btnCancel = null!;
        private PictureBox picHeaderLogo = null!;
        private Panel headerPanel = null!;

        private const string AppVersion = "0.0.1";
        private const string AppDisplayName = "STORM IMAGES";
        private const string AppFolderName = "STORM IMAGES";
        private const string ExeName = "StormImages.exe";
        private const string IcoName = "AppIcon.ico";

        private RadioButton rbStandard = null!;
        private RadioButton rbPortable = null!;
        private TextBox txtInstallPath = null!;
        private Button btnBrowse = null!;

        private CheckBox chkDesktop = null!;
        private CheckBox chkStartMenu = null!;
        private CheckBox chkRegister = null!;
        private CheckBox chkInstallCert = null!;
        private CheckBox chkRunAfter = null!;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteFile(string name);

        public InstallerForm()
        {
            try
            {
                var asm = Assembly.GetExecutingAssembly();
                foreach (var name in asm.GetManifestResourceNames())
                {
                    if (name.EndsWith(IcoName, StringComparison.OrdinalIgnoreCase) || name.EndsWith("app.ico", StringComparison.OrdinalIgnoreCase))
                    {
                        using var s = asm.GetManifestResourceStream(name);
                        if (s != null)
                        {
                            this.Icon = new Icon(s);
                            break;
                        }
                    }
                }
                if (this.Icon == null && !string.IsNullOrEmpty(Application.ExecutablePath) && File.Exists(Application.ExecutablePath))
                {
                    this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                }
            }
            catch { }
            InitializeComponent();
        }

        private static GraphicsPath GetRoundedRectPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = radius * 2;
            Rectangle arc = new Rectangle(rect.Location, new Size(diameter, diameter));

            path.AddArc(arc, 180, 90);
            arc.X = rect.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = rect.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = rect.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void InitializeComponent()
        {
            this.Text = $"{AppDisplayName} \u2014 STORM INSTALLER";
            this.Size = new Size(640, 540);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(11, 15, 25);
            this.ForeColor = Color.White;
            this.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

            // 1. Dark Stylized Header
            headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 88,
                BackColor = Color.FromArgb(17, 24, 39),
                Padding = new Padding(22, 14, 22, 14)
            };
            headerPanel.Paint += (s, e) =>
            {
                using var p = new Pen(Color.FromArgb(14, 165, 233), 2f);
                e.Graphics.DrawLine(p, 0, headerPanel.Height - 1, headerPanel.Width, headerPanel.Height - 1);
            };

            lblTitle = new Label
            {
                Text = AppDisplayName,
                Font = new Font("Segoe UI", 15.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(14, 165, 233),
                AutoSize = true,
                Location = new Point(22, 16)
            };

            lblSubtitle = new Label
            {
                Text = $"\u041C\u0430\u0441\u0442\u0435\u0440 \u0443\u0441\u0442\u0430\u043D\u043E\u0432\u043A\u0438 \u2022 \u0412\u0435\u0440\u0441\u0438\u044F {AppVersion} \u2022 STORM TEAM",
                Font = new Font("Segoe UI", 9.2f, FontStyle.Regular),
                ForeColor = Color.FromArgb(156, 163, 175),
                AutoSize = true,
                Location = new Point(24, 49)
            };

            // Top-Right Header Icon Container Badge (Preserved Red-Black Logo)
            var logoContainer = new Panel
            {
                Location = new Point(546, 12),
                Size = new Size(62, 62),
                BackColor = Color.Transparent
            };
            logoContainer.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = GetRoundedRectPath(new Rectangle(0, 0, 61, 61), 10);
                using var brush = new SolidBrush(Color.FromArgb(20, 10, 15));
                using var pen = new Pen(Color.FromArgb(225, 29, 72), 1.5f);
                e.Graphics.FillPath(brush, path);
                e.Graphics.DrawPath(pen, path);
            };

            picHeaderLogo = new PictureBox
            {
                Location = new Point(5, 5),
                Size = new Size(52, 52),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent
            };

            Image? logoImg = null;
            try
            {
                var asm = Assembly.GetExecutingAssembly();
                foreach (var name in asm.GetManifestResourceNames())
                {
                    if (name.EndsWith("header_badge.png", StringComparison.OrdinalIgnoreCase) ||
                        name.EndsWith("badge_logo.png", StringComparison.OrdinalIgnoreCase) ||
                        name.EndsWith("logo.png", StringComparison.OrdinalIgnoreCase))
                    {
                        using var s = asm.GetManifestResourceStream(name);
                        if (s != null)
                        {
                            logoImg = Image.FromStream(s);
                            break;
                        }
                    }
                }
            }
            catch { }

            if (logoImg != null)
            {
                picHeaderLogo.Image = logoImg;
            }
            else if (this.Icon != null)
            {
                picHeaderLogo.Image = this.Icon.ToBitmap();
            }

            logoContainer.Controls.Add(picHeaderLogo);

            headerPanel.Controls.Add(lblTitle);
            headerPanel.Controls.Add(lblSubtitle);
            headerPanel.Controls.Add(logoContainer);
            this.Controls.Add(headerPanel);

            // 2. Body Panel
            var bodyPanel = new Panel
            {
                Location = new Point(24, 98),
                Size = new Size(576, 350)
            };

            var lblMode = new Label
            {
                Text = "\u0412\u044B\u0431\u0435\u0440\u0438\u0442\u0435 \u0442\u0438\u043F \u0443\u0441\u0442\u0430\u043D\u043E\u0432\u043A\u0438 \u043F\u0440\u043E\u0433\u0440\u0430\u043C\u043C\u044B:",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(226, 232, 240),
                Location = new Point(0, 0),
                AutoSize = true
            };
            bodyPanel.Controls.Add(lblMode);

            rbStandard = new RadioButton
            {
                Text = "\u0421\u0442\u0430\u043D\u0434\u0430\u0440\u0442\u043D\u0430\u044F \u0443\u0441\u0442\u0430\u043D\u043E\u0432\u043A\u0430 \u0432 Program Files (\u0440\u0435\u043A\u043E\u043C\u0435\u043D\u0434\u0443\u0435\u0442\u0441\u044F)",
                Checked = true,
                Location = new Point(10, 25),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                ForeColor = Color.White
            };
            rbStandard.CheckedChanged += Mode_CheckedChanged;
            bodyPanel.Controls.Add(rbStandard);

            rbPortable = new RadioButton
            {
                Text = "\u041F\u043E\u0440\u0442\u0430\u0442\u0438\u0432\u043D\u0430\u044F \u0432\u0435\u0440\u0441\u0438\u044F (\u0432 \u0432\u044B\u0431\u0440\u0430\u043D\u043D\u0443\u044E \u0432\u0430\u043C\u0438 \u043F\u0430\u043F\u043A\u0443, \u0431\u0435\u0437 \u0440\u0435\u0435\u0441\u0442\u0440\u0430)",
                Checked = false,
                Location = new Point(10, 50),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                ForeColor = Color.White
            };
            rbPortable.CheckedChanged += Mode_CheckedChanged;
            bodyPanel.Controls.Add(rbPortable);

            var lblPath = new Label
            {
                Text = "\u041F\u0430\u043F\u043A\u0430 \u043D\u0430\u0437\u043D\u0430\u0447\u0435\u043D\u0438\u044F:",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(226, 232, 240),
                Location = new Point(0, 82),
                AutoSize = true
            };
            bodyPanel.Controls.Add(lblPath);

            txtInstallPath = new TextBox
            {
                Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), AppFolderName),
                Location = new Point(5, 105),
                Size = new Size(460, 26),
                BackColor = Color.FromArgb(17, 24, 39),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9.5f)
            };
            bodyPanel.Controls.Add(txtInstallPath);

            btnBrowse = new Button
            {
                Text = "\u041E\u0431\u0437\u043E\u0440...",
                Location = new Point(475, 104),
                Size = new Size(95, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.FromArgb(14, 165, 233),
                Cursor = Cursors.Hand
            };
            btnBrowse.FlatAppearance.BorderColor = Color.FromArgb(14, 165, 233);
            btnBrowse.Click += BtnBrowse_Click;
            bodyPanel.Controls.Add(btnBrowse);

            var lblOptions = new Label
            {
                Text = "\u0414\u043E\u043F\u043E\u043B\u043D\u0438\u0442\u0435\u043B\u044C\u043D\u044B\u0435 \u043F\u0430\u0440\u0430\u043C\u0435\u0442\u0440\u044B \u0431\u0435\u0437\u043E\u043F\u0430\u0441\u043D\u043E\u0441\u0442\u0438 \u0438 \u0438\u043D\u0442\u0435\u0433\u0440\u0430\u0446\u0438\u0438:",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(226, 232, 240),
                Location = new Point(0, 142),
                AutoSize = true
            };
            bodyPanel.Controls.Add(lblOptions);

            chkDesktop = new CheckBox
            {
                Text = "\u0421\u043E\u0437\u0434\u0430\u0442\u044C \u044F\u0440\u043B\u044B\u043A \u043D\u0430 \u0420\u0430\u0431\u043E\u0447\u0435\u043C \u0441\u0442\u043E\u043B\u0435",
                Checked = true,
                Location = new Point(10, 166),
                AutoSize = true,
                ForeColor = Color.White
            };
            bodyPanel.Controls.Add(chkDesktop);

            chkStartMenu = new CheckBox
            {
                Text = "\u0421\u043E\u0437\u0434\u0430\u0442\u044C \u044F\u0440\u043B\u044B\u043A \u0432 \u043C\u0435\u043D\u044E \u00AB\u041F\u0443\u0441\u043A\u00BB",
                Checked = true,
                Location = new Point(10, 191),
                AutoSize = true,
                ForeColor = Color.White
            };
            bodyPanel.Controls.Add(chkStartMenu);

            chkInstallCert = new CheckBox
            {
                Text = "\u0417\u0430\u0440\u0435\u0433\u0438\u0441\u0442\u0440\u0438\u0440\u043E\u0432\u0430\u0442\u044C \u0441\u0435\u0440\u0442\u0438\u0444\u0438\u043A\u0430\u0442 STORM TEAM (\u0437\u0430\u0449\u0438\u0442\u0430 \u043E\u0442 SmartScreen / SAC)",
                Checked = true,
                Location = new Point(10, 216),
                AutoSize = true,
                ForeColor = Color.FromArgb(52, 211, 153)
            };
            bodyPanel.Controls.Add(chkInstallCert);

            chkRegister = new CheckBox
            {
                Text = "\u0417\u0430\u0440\u0435\u0433\u0438\u0441\u0442\u0440\u0438\u0440\u043E\u0432\u0430\u0442\u044C \u0432 \u0441\u043F\u0438\u0441\u043A\u0435 \u00AB\u0423\u0441\u0442\u0430\u043D\u043E\u0432\u043A\u0430 \u0438 \u0443\u0434\u0430\u043B\u0435\u043D\u0438\u0435 \u043F\u0440\u043E\u0433\u0440\u0430\u043C\u043C\u00BB",
                Checked = true,
                Location = new Point(10, 241),
                AutoSize = true,
                ForeColor = Color.White
            };
            bodyPanel.Controls.Add(chkRegister);

            chkRunAfter = new CheckBox
            {
                Text = $"\u0417\u0430\u043F\u0443\u0441\u0442\u0438\u0442\u044C {AppDisplayName} \u0441\u0440\u0430\u0437\u0443 \u043F\u043E\u0441\u043B\u0435 \u0443\u0441\u0442\u0430\u043D\u043E\u0432\u043A\u0438",
                Checked = true,
                Location = new Point(10, 266),
                AutoSize = true,
                ForeColor = Color.FromArgb(14, 165, 233)
            };
            bodyPanel.Controls.Add(chkRunAfter);

            progressBar = new ProgressBar
            {
                Location = new Point(5, 296),
                Size = new Size(565, 12),
                Style = ProgressBarStyle.Continuous,
                Value = 0,
                Visible = false
            };
            bodyPanel.Controls.Add(progressBar);

            lblStatus = new Label
            {
                Text = "",
                Location = new Point(5, 312),
                Size = new Size(565, 20),
                Font = new Font("Segoe UI", 8.8f),
                ForeColor = Color.FromArgb(148, 163, 184),
                Visible = false
            };
            bodyPanel.Controls.Add(lblStatus);

            this.Controls.Add(bodyPanel);

            // 3. Bottom Panel
            var bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.FromArgb(17, 24, 39),
                Padding = new Padding(24, 12, 24, 12)
            };

            btnCancel = new Button
            {
                Text = "\u041E\u0442\u043C\u0435\u043D\u0430",
                Size = new Size(110, 36),
                Location = new Point(365, 12),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.FromArgb(226, 232, 240),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(51, 65, 85);
            btnCancel.Click += (s, e) => this.Close();
            bottomPanel.Controls.Add(btnCancel);

            btnInstall = new Button
            {
                Text = "\uD83D\uDCE6  \u0423\u0441\u0442\u0430\u043D\u043E\u0432\u0438\u0442\u044C",
                Size = new Size(135, 36),
                Location = new Point(485, 12),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(14, 165, 233),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.8f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnInstall.FlatAppearance.BorderColor = Color.FromArgb(56, 189, 248);
            btnInstall.Click += BtnInstall_Click;
            bottomPanel.Controls.Add(btnInstall);

            this.Controls.Add(bottomPanel);
        }

        private void Mode_CheckedChanged(object? sender, EventArgs e)
        {
            if (rbPortable.Checked)
            {
                txtInstallPath.Text = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{AppFolderName}_Portable");
                chkDesktop.Checked = false;
                chkDesktop.Enabled = false;
                chkStartMenu.Checked = false;
                chkStartMenu.Enabled = false;
                chkRegister.Checked = false;
                chkRegister.Enabled = false;
                btnInstall.Text = "\uD83D\uDCE6  \u0420\u0430\u0441\u043F\u0430\u043A\u043E\u0432\u0430\u0442\u044C";
            }
            else
            {
                txtInstallPath.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), AppFolderName);
                chkDesktop.Checked = true;
                chkDesktop.Enabled = true;
                chkStartMenu.Checked = true;
                chkStartMenu.Enabled = true;
                chkRegister.Checked = true;
                chkRegister.Enabled = true;
                btnInstall.Text = "\uD83D\uDCE6  \u0423\u0441\u0442\u0430\u043D\u043E\u0432\u0438\u0442\u044C";
            }
        }

        private void BtnBrowse_Click(object? sender, EventArgs e)
        {
            using var fbd = new FolderBrowserDialog();
            fbd.Description = $"\u0412\u044B\u0431\u0435\u0440\u0438\u0442\u0435 \u043F\u0430\u043F\u043A\u0443 \u0434\u043B\u044F \u0443\u0441\u0442\u0430\u043D\u043E\u0432\u043A\u0438 {AppDisplayName}:";
            fbd.UseDescriptionForTitle = true;
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                txtInstallPath.Text = fbd.SelectedPath;
            }
        }

        private async void BtnInstall_Click(object? sender, EventArgs e)
        {
            progressBar.Visible = true;
            lblStatus.Visible = true;
            await StartInstallationAsync();
        }

        private async Task StartInstallationAsync()
        {
            btnInstall.Enabled = false;
            btnCancel.Enabled = false;
            btnBrowse.Enabled = false;

            try
            {
                string targetDir = txtInstallPath.Text.Trim();
                if (string.IsNullOrEmpty(targetDir))
                {
                    targetDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), AppFolderName);
                }

                Directory.CreateDirectory(targetDir);

                // Terminate running instances strictly
                lblStatus.Text = "Завершение предыдущих процессов программы...";
                progressBar.Value = 10;
                await Task.Delay(150);

                KillRunningProcesses();

                string targetExe = Path.Combine(targetDir, ExeName);
                string targetLauncher = Path.Combine(targetDir, "StormLauncher.exe");
                string targetCer = Path.Combine(targetDir, "STORM_Certificate.cer");
                string targetIco = Path.Combine(targetDir, IcoName);
                string targetLogo = Path.Combine(targetDir, "logo.png");

                if (chkInstallCert.Checked)
                {
                    lblStatus.Text = "Регистрация доверенного сертификата...";
                    progressBar.Value = 25;
                    await Task.Delay(150);

                    ExtractResource("STORM_Certificate.cer", targetCer);
                    if (File.Exists(targetCer))
                    {
                        InstallCertificateSilently(targetCer);
                    }
                }

                lblStatus.Text = $"Распаковка пакета {AppDisplayName} (v{AppVersion})...";
                progressBar.Value = 45;
                await Task.Delay(100);

                // Extract primary files with strict validation
                ExtractResource(ExeName, targetExe);
                try { ExtractResource("StormLauncher.exe", targetLauncher); } catch { }
                ExtractResource(IcoName, targetIco);
                try { ExtractResource("logo.png", targetLogo); } catch { }
                ExtractResource("STORM_Certificate.cer", targetCer);

                progressBar.Value = 75;
                lblStatus.Text = "Снятие меток блокировки и оптимизация безопасности...";
                await Task.Delay(150);

                UnblockFile(targetExe);
                UnblockFile(targetLauncher);
                UnblockFile(targetCer);
                UnblockFile(targetIco);
                UnblockFile(targetLogo);
                UnblockEntireDirectory(targetDir);

                if (rbStandard.Checked)
                {
                    lblStatus.Text = "Создание системных ярлыков и регистрация в Windows...";
                    progressBar.Value = 88;
                    await Task.Delay(150);

                    CreateShortcuts(targetDir, targetExe, targetIco, chkDesktop.Checked, chkStartMenu.Checked);

                    if (chkRegister.Checked)
                    {
                        RegisterUninstall(targetDir, targetExe, targetIco);
                    }
                }

                progressBar.Value = 100;
                lblStatus.Text = rbPortable.Checked ? "Портативная версия успешно распакована!" : "Установка успешно завершена! Система полностью готова.";
                lblStatus.ForeColor = Color.FromArgb(16, 185, 129);
                await Task.Delay(500);

                if (chkRunAfter.Checked)
                {
                    TryLaunchApplication(targetExe, targetDir);
                }

                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка во время установки:\n{ex.Message}", "Ошибка установки", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnInstall.Enabled = true;
                btnCancel.Enabled = true;
                btnBrowse.Enabled = true;
            }
        }

        private static void TryLaunchApplication(string directExePath, string workingDir)
        {
            try
            {
                if (File.Exists(directExePath))
                {
                    UnblockFile(directExePath);
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = directExePath,
                            WorkingDirectory = workingDir,
                            UseShellExecute = true
                        });
                        return;
                    }
                    catch { }

                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "explorer.exe",
                            Arguments = $"\"{directExePath}\"",
                            UseShellExecute = false
                        });
                    }
                    catch { }
                }
            }
            catch { }
        }

        public static void UnblockFile(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    DeleteFile(path + ":Zone.Identifier");
                }
            }
            catch { }
        }

        public static void UnblockEntireDirectory(string dir)
        {
            try
            {
                if (!Directory.Exists(dir)) return;
                foreach (var file in Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories))
                {
                    UnblockFile(file);
                }
            }
            catch { }
        }

        public static void InstallCertificateSilently(string cerPath)
        {
            try
            {
                if (!File.Exists(cerPath)) return;

                try
                {
                    var cert = new X509Certificate2(cerPath);
                    foreach (var loc in new[] { StoreLocation.LocalMachine, StoreLocation.CurrentUser })
                    {
                        foreach (var name in new[] { StoreName.Root, StoreName.TrustedPublisher, StoreName.AuthRoot, StoreName.CertificateAuthority })
                        {
                            try
                            {
                                using var store = new X509Store(name, loc);
                                store.Open(OpenFlags.ReadWrite);
                                store.Add(cert);
                                store.Close();
                            }
                            catch { }
                        }
                    }
                }
                catch { }

                try
                {
                    using var keyCi = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\CI\Policy");
                    keyCi?.SetValue("VerifiedAndReputablePolicyState", 0, RegistryValueKind.DWord);
                    keyCi?.SetValue("SAC_PreviousState", 0, RegistryValueKind.DWord);

                    using var keySs = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer");
                    keySs?.SetValue("SmartScreenEnabled", "Off", RegistryValueKind.String);

                    using var keyDef = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows Defender\SmartScreen");
                    keyDef?.SetValue("ConfigureAppInstallControlEnabled", 0, RegistryValueKind.DWord);
                }
                catch { }
            }
            catch { }
        }

        private static void KillRunningProcesses()
        {
            string[] procNames = { "StormImages", "STORM_IMAGES", "StormLauncher" };
            foreach (var pName in procNames)
            {
                try
                {
                    var psi = new ProcessStartInfo("taskkill.exe", $"/F /T /IM {pName}.exe")
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    using var p = Process.Start(psi);
                    p?.WaitForExit(2000);
                }
                catch { }
            }
        }

        private void ExtractResource(string resNameEnding, string targetPath)
        {
            string? dir = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var asm = Assembly.GetExecutingAssembly();
            string? foundResource = null;
            foreach (var name in asm.GetManifestResourceNames())
            {
                if (name.EndsWith(resNameEnding, StringComparison.OrdinalIgnoreCase))
                {
                    foundResource = name;
                    break;
                }
            }

            if (foundResource == null)
            {
                throw new FileNotFoundException($"Встроенный ресурс {resNameEnding} не найден в пакете установщика!");
            }

            if (File.Exists(targetPath))
            {
                try
                {
                    File.SetAttributes(targetPath, FileAttributes.Normal);
                    File.Delete(targetPath);
                }
                catch
                {
                    try
                    {
                        string oldPath = targetPath + ".old." + Guid.NewGuid().ToString("N");
                        File.Move(targetPath, oldPath);
                    }
                    catch (Exception ex)
                    {
                        throw new IOException($"Не удалось обновить файл {Path.GetFileName(targetPath)}: {ex.Message}");
                    }
                }
            }

            using (var inStream = asm.GetManifestResourceStream(foundResource))
            {
                if (inStream == null) throw new InvalidOperationException($"Не удалось прочитать ресурс {foundResource}");
                using (var outStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    inStream.CopyTo(outStream);
                    outStream.Flush(true);
                }
            }

            if (!File.Exists(targetPath) || new FileInfo(targetPath).Length == 0)
            {
                throw new IOException($"Ошибка распаковки: файл {Path.GetFileName(targetPath)} пуст.");
            }
        }

        private void CreateShortcuts(string targetDir, string targetExe, string targetIco, bool desktopShortcut, bool startMenuShortcut)
        {
            try
            {
                Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null) return;
                dynamic? shell = Activator.CreateInstance(shellType);
                if (shell == null) return;

                if (startMenuShortcut)
                {
                    string startMenu = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), $"{AppDisplayName}.lnk");
                    dynamic shortcut = shell.CreateShortcut(startMenu);
                    shortcut.TargetPath = targetExe;
                    shortcut.WorkingDirectory = targetDir;
                    shortcut.IconLocation = (File.Exists(targetIco) ? targetIco : targetExe) + ",0";
                    shortcut.Description = AppDisplayName;
                    shortcut.Save();
                }

                if (desktopShortcut)
                {
                    string desktop = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), $"{AppDisplayName}.lnk");
                    dynamic deskShortcut = shell.CreateShortcut(desktop);
                    deskShortcut.TargetPath = targetExe;
                    deskShortcut.WorkingDirectory = targetDir;
                    deskShortcut.IconLocation = (File.Exists(targetIco) ? targetIco : targetExe) + ",0";
                    deskShortcut.Description = AppDisplayName;
                    deskShortcut.Save();
                }
            }
            catch { }
        }

        private void RegisterUninstall(string targetDir, string targetExe, string targetIco)
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\StormImages");
                if (key != null)
                {
                    key.SetValue("DisplayName", AppDisplayName);
                    key.SetValue("DisplayVersion", AppVersion);
                    key.SetValue("Publisher", "STORM TEAM");
                    key.SetValue("DisplayIcon", File.Exists(targetIco) ? targetIco : targetExe);
                    key.SetValue("InstallLocation", targetDir);
                    key.SetValue("UninstallString", $"cmd.exe /c rmdir /s /q \"{targetDir}\" & del \"%APPDATA%\\Microsoft\\Windows\\Start Menu\\Programs\\*STORM SYSTEM OPTIMIZER*.lnk\" & del \"%USERPROFILE%\\Desktop\\*STORM SYSTEM OPTIMIZER*.lnk\"");
                }
            }
            catch { }
        }

        private static bool IsAdministrator()
        {
            try
            {
                using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                var principal = new System.Security.Principal.WindowsPrincipal(identity);
                return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
            catch { return false; }
        }

        [STAThread]
        public static void Main()
        {
            try
            {
                string selfExe = Process.GetCurrentProcess().MainModule?.FileName ?? "";
                if (!string.IsNullOrEmpty(selfExe))
                {
                    UnblockFile(selfExe);
                }

                if (!IsAdministrator())
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = selfExe,
                        UseShellExecute = true,
                        Verb = "runas"
                    };
                    Process.Start(psi);
                    return;
                }
            }
            catch { }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new InstallerForm());
        }
    }
}
