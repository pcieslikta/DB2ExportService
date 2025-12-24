using System;
using System.Drawing;
using System.Windows.Forms;

namespace DB2ExportConfigurator
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        // Main layout
        private FluentSidebar sidebar;
        private Panel contentPanel;
        private Panel headerPanel;
        private Label lblTitle;
        private Button btnToggleTheme;
        private Button btnToggleSidebar;
        private Button btnSave;

        // Content panels for each section
        private Panel panelDB2;
        private Panel panelExport;
        private Panel panelVehicles;
        private Panel panelService;

        // DB2 Controls
        private TextBox txtDatabase;
        private TextBox txtHostname;
        private TextBox txtPort;
        private TextBox txtUser;
        private TextBox txtPassword;
        private CheckBox chkUseCredentialManager;
        private TextBox txtCredentialKey;

        // Export Controls
        private TextBox txtExportPath;
        private TextBox txtLogPath;
        private TextBox txtScheduleTime;
        private NumericUpDown numDaysBack;
        private TextBox txtKodExportu;

        // Vehicles Controls
        private ComboBox cmbPojazdyMode;
        private NumericUpDown numPojazdyStart;
        private NumericUpDown numPojazdyEnd;
        private TextBox txtPojazdyLista;

        // Service Controls
        private Label lblServiceStatus;
        private Button btnStartService;
        private Button btnStopService;
        private Button btnRestartService;
        private Button btnViewLogs;
        private Button btnInstallService;
        private Button btnUninstallService;
        private Button btnRunConsole;
        private Button btnDiagnostics;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();

            // Form settings
            this.Text = "DB2 Export Service - Konfigurator";
            this.Size = new Size(1200, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(1000, 600);
            this.Font = new Font("Segoe UI", 9F);

            // Header Panel
            CreateHeaderPanel();

            // Sidebar
            CreateSidebar();

            // Content Panel
            CreateContentPanel();

            // Create all content panels
            CreateDB2Panel();
            CreateExportPanel();
            CreateVehiclesPanel();
            CreateServicePanel();

            // Add controls to form
            this.Controls.Add(contentPanel);
            this.Controls.Add(sidebar);
            this.Controls.Add(headerPanel);

            // Show first panel
            ShowPanel("db2");
        }

        private void CreateHeaderPanel()
        {
            headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.FromArgb(52, 73, 94)
            };

            // Title
            lblTitle = new Label
            {
                Text = "🗄️ Konfiguracja DB2",
                Location = new Point(60, 15),
                Size = new Size(400, 30),
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = false
            };
            headerPanel.Controls.Add(lblTitle);

            // Toggle Sidebar Button
            btnToggleSidebar = new Button
            {
                Text = "☰",
                Location = new Point(10, 15),
                Size = new Size(40, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 14F),
                Cursor = Cursors.Hand
            };
            btnToggleSidebar.FlatAppearance.BorderSize = 0;
            btnToggleSidebar.FlatAppearance.MouseOverBackColor = Color.FromArgb(70, 90, 110);
            btnToggleSidebar.Click += (s, e) => sidebar.Toggle();
            headerPanel.Controls.Add(btnToggleSidebar);

            // Toggle Theme Button
            btnToggleTheme = new Button
            {
                Text = "🌙",
                Location = new Point(1100, 15),
                Size = new Size(40, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 14F),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnToggleTheme.FlatAppearance.BorderSize = 0;
            btnToggleTheme.FlatAppearance.MouseOverBackColor = Color.FromArgb(70, 90, 110);
            btnToggleTheme.Click += BtnToggleTheme_Click;
            headerPanel.Controls.Add(btnToggleTheme);

            // Save Button
            btnSave = new Button
            {
                Text = "💾 Zapisz",
                Location = new Point(1000, 15),
                Size = new Size(90, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += (s, e) => SaveSettings();
            headerPanel.Controls.Add(btnSave);
        }

        private void CreateSidebar()
        {
            sidebar = new FluentSidebar
            {
                Dock = DockStyle.Left
            };

            sidebar.NavigationChanged += Sidebar_NavigationChanged;
        }

        private void CreateContentPanel()
        {
            contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                AutoScroll = true
            };
        }

        private void CreateDB2Panel()
        {
            panelDB2 = new Panel
            {
                Location = new Point(0, 0),
                Size = contentPanel.ClientSize,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                AutoScroll = true,
                Visible = false
            };

            int y = 20;

            // Section Title
            var lblSection = new Label
            {
                Text = "Konfiguracja połączenia DB2",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                Location = new Point(20, y),
                Size = new Size(600, 30),
                ForeColor = Color.FromArgb(52, 73, 94)
            };
            panelDB2.Controls.Add(lblSection);
            y += 50;

            // Database
            AddLabel(panelDB2, "Baza danych:", y);
            txtDatabase = AddTextBox(panelDB2, y, "SAMPLE");
            y += 40;

            // Hostname
            AddLabel(panelDB2, "Hostname:", y);
            txtHostname = AddTextBox(panelDB2, y, "localhost");
            y += 40;

            // Port
            AddLabel(panelDB2, "Port:", y);
            txtPort = AddTextBox(panelDB2, y, "50000", 150);
            y += 40;

            // User
            AddLabel(panelDB2, "Użytkownik:", y);
            txtUser = AddTextBox(panelDB2, y);
            y += 40;

            // Password
            AddLabel(panelDB2, "Hasło:", y);
            txtPassword = AddTextBox(panelDB2, y);
            txtPassword.UseSystemPasswordChar = true;
            y += 40;

            // Credential Manager
            chkUseCredentialManager = new CheckBox
            {
                Text = "Użyj Windows Credential Manager",
                Location = new Point(200, y),
                Size = new Size(400, 25),
                Checked = true,
                Font = new Font("Segoe UI", 10F)
            };
            chkUseCredentialManager.CheckedChanged += (s, e) =>
            {
                txtUser.Enabled = !chkUseCredentialManager.Checked;
                txtPassword.Enabled = !chkUseCredentialManager.Checked;
                txtCredentialKey.Enabled = chkUseCredentialManager.Checked;
            };
            panelDB2.Controls.Add(chkUseCredentialManager);
            y += 35;

            // Credential Key
            AddLabel(panelDB2, "Credential Key:", y);
            txtCredentialKey = AddTextBox(panelDB2, y, "DB2Export_PROD");
            y += 40;

            contentPanel.Controls.Add(panelDB2);
        }

        private void CreateExportPanel()
        {
            panelExport = new Panel
            {
                Location = new Point(0, 0),
                Size = contentPanel.ClientSize,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                AutoScroll = true,
                Visible = false
            };

            int y = 20;

            var lblSection = new Label
            {
                Text = "Konfiguracja eksportu",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                Location = new Point(20, y),
                Size = new Size(600, 30),
                ForeColor = Color.FromArgb(52, 73, 94)
            };
            panelExport.Controls.Add(lblSection);
            y += 50;

            // Export Path
            AddLabel(panelExport, "Ścieżka eksportu:", y);
            txtExportPath = AddTextBox(panelExport, y, @"C:\EXPORT\", 500);
            y += 40;

            // Log Path
            AddLabel(panelExport, "Ścieżka logów:", y);
            txtLogPath = AddTextBox(panelExport, y, @"C:\EXPORT\LOG\", 500);
            y += 40;

            // Schedule Time
            AddLabel(panelExport, "Godzina eksportu:", y);
            txtScheduleTime = AddTextBox(panelExport, y, "13:15", 150);
            var lblHint = new Label
            {
                Text = "(format: HH:mm)",
                Location = new Point(370, y + 5),
                Size = new Size(150, 20),
                ForeColor = Color.Gray
            };
            panelExport.Controls.Add(lblHint);
            y += 40;

            // Days Back
            AddLabel(panelExport, "Dni wstecz:", y);
            numDaysBack = new NumericUpDown
            {
                Location = new Point(200, y),
                Size = new Size(150, 25),
                Minimum = 1,
                Maximum = 30,
                Value = 2
            };
            panelExport.Controls.Add(numDaysBack);
            y += 40;

            // Kod Exportu
            AddLabel(panelExport, "Kod eksportu:", y);
            txtKodExportu = AddTextBox(panelExport, y, "SOSNO", 200);
            y += 40;

            contentPanel.Controls.Add(panelExport);
        }

        private void CreateVehiclesPanel()
        {
            panelVehicles = new Panel
            {
                Location = new Point(0, 0),
                Size = contentPanel.ClientSize,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                AutoScroll = true,
                Visible = false
            };

            int y = 20;

            var lblSection = new Label
            {
                Text = "Konfiguracja pojazdów",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                Location = new Point(20, y),
                Size = new Size(600, 30),
                ForeColor = Color.FromArgb(52, 73, 94)
            };
            panelVehicles.Controls.Add(lblSection);
            y += 50;

            // Mode
            AddLabel(panelVehicles, "Tryb wyboru:", y);
            cmbPojazdyMode = new ComboBox
            {
                Location = new Point(200, y),
                Size = new Size(200, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbPojazdyMode.Items.AddRange(new object[] { "lista", "zakres" });
            cmbPojazdyMode.SelectedIndex = 0;
            cmbPojazdyMode.SelectedIndexChanged += (s, e) => UpdateVehicleModeControls();
            panelVehicles.Controls.Add(cmbPojazdyMode);
            y += 50;

            // Range Group
            var grpRange = new GroupBox
            {
                Text = "Zakres pojazdów",
                Location = new Point(20, y),
                Size = new Size(700, 100),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };

            var lblFrom = new Label
            {
                Text = "Od:",
                Location = new Point(20, 35),
                Size = new Size(80, 20)
            };
            grpRange.Controls.Add(lblFrom);

            numPojazdyStart = new NumericUpDown
            {
                Location = new Point(110, 35),
                Size = new Size(120, 25),
                Minimum = 1,
                Maximum = 9999,
                Value = 2209
            };
            grpRange.Controls.Add(numPojazdyStart);

            var lblTo = new Label
            {
                Text = "Do:",
                Location = new Point(280, 35),
                Size = new Size(80, 20)
            };
            grpRange.Controls.Add(lblTo);

            numPojazdyEnd = new NumericUpDown
            {
                Location = new Point(370, 35),
                Size = new Size(120, 25),
                Minimum = 1,
                Maximum = 9999,
                Value = 2238
            };
            grpRange.Controls.Add(numPojazdyEnd);

            panelVehicles.Controls.Add(grpRange);
            y += 110;

            // List Group
            var grpList = new GroupBox
            {
                Text = "Lista pojazdów (oddzielone przecinkami)",
                Location = new Point(20, y),
                Size = new Size(700, 120),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };

            txtPojazdyLista = new TextBox
            {
                Location = new Point(15, 30),
                Size = new Size(670, 70),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Text = "598, 599, 600, 601, 602, 841, 842, 843, 844, 845, 846, 2107, 2108, 2600, 2601, 2602, 2603"
            };
            grpList.Controls.Add(txtPojazdyLista);

            panelVehicles.Controls.Add(grpList);

            contentPanel.Controls.Add(panelVehicles);
        }

        private void CreateServicePanel()
        {
            panelService = new Panel
            {
                Location = new Point(0, 0),
                Size = contentPanel.ClientSize,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                AutoScroll = true,
                Visible = false
            };

            int y = 20;

            var lblSection = new Label
            {
                Text = "Zarządzanie serwisem Windows",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                Location = new Point(20, y),
                Size = new Size(600, 30),
                ForeColor = Color.FromArgb(52, 73, 94)
            };
            panelService.Controls.Add(lblSection);
            y += 50;

            // Status
            lblServiceStatus = new Label
            {
                Text = "Status: Sprawdzanie...",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                Location = new Point(20, y),
                Size = new Size(500, 30),
                ForeColor = Color.Gray
            };
            panelService.Controls.Add(lblServiceStatus);
            y += 50;

            // Control Buttons Group
            var grpControl = new GroupBox
            {
                Text = "Kontrola serwisu",
                Location = new Point(20, y),
                Size = new Size(700, 90),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };

            btnStartService = new Button
            {
                Text = "▶️ Uruchom",
                Location = new Point(20, 30),
                Size = new Size(150, 45),
                Font = new Font("Segoe UI", 10F),
                BackColor = Color.FromArgb(39, 174, 96),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnStartService.FlatAppearance.BorderSize = 0;
            btnStartService.Click += (s, e) => ControlService(ServiceAction.Start);
            grpControl.Controls.Add(btnStartService);

            btnStopService = new Button
            {
                Text = "⏹️ Zatrzymaj",
                Location = new Point(190, 30),
                Size = new Size(150, 45),
                Font = new Font("Segoe UI", 10F),
                BackColor = Color.FromArgb(231, 76, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnStopService.FlatAppearance.BorderSize = 0;
            btnStopService.Click += (s, e) => ControlService(ServiceAction.Stop);
            grpControl.Controls.Add(btnStopService);

            btnRestartService = new Button
            {
                Text = "🔄 Restart",
                Location = new Point(360, 30),
                Size = new Size(150, 45),
                Font = new Font("Segoe UI", 10F),
                BackColor = Color.FromArgb(243, 156, 18),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnRestartService.FlatAppearance.BorderSize = 0;
            btnRestartService.Click += (s, e) => ControlService(ServiceAction.Restart);
            grpControl.Controls.Add(btnRestartService);

            btnViewLogs = new Button
            {
                Text = "📄 Logi",
                Location = new Point(530, 30),
                Size = new Size(150, 45),
                Font = new Font("Segoe UI", 10F),
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnViewLogs.FlatAppearance.BorderSize = 0;
            btnViewLogs.Click += (s, e) =>
            {
                try
                {
                    var logPath = txtLogPath.Text;
                    if (System.IO.Directory.Exists(logPath))
                    {
                        System.Diagnostics.Process.Start("explorer.exe", logPath);
                    }
                    else
                    {
                        MessageBox.Show($"Katalog nie istnieje:\n{logPath}", "Błąd",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Błąd otwierania katalogu:\n{ex.Message}", "Błąd",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            grpControl.Controls.Add(btnViewLogs);

            panelService.Controls.Add(grpControl);
            y += 100;

            // Installation Group
            var grpInstall = new GroupBox
            {
                Text = "Instalacja i narzędzia",
                Location = new Point(20, y),
                Size = new Size(700, 90),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };

            btnInstallService = new Button
            {
                Text = "📥 Instaluj",
                Location = new Point(20, 30),
                Size = new Size(150, 45),
                Font = new Font("Segoe UI", 10F),
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnInstallService.FlatAppearance.BorderSize = 0;
            btnInstallService.Click += (s, e) => ControlService(ServiceAction.Install);
            grpInstall.Controls.Add(btnInstallService);

            btnUninstallService = new Button
            {
                Text = "🗑️ Odinstaluj",
                Location = new Point(190, 30),
                Size = new Size(150, 45),
                Font = new Font("Segoe UI", 10F),
                BackColor = Color.FromArgb(149, 165, 166),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnUninstallService.FlatAppearance.BorderSize = 0;
            btnUninstallService.Click += (s, e) => ControlService(ServiceAction.Uninstall);
            grpInstall.Controls.Add(btnUninstallService);

            btnRunConsole = new Button
            {
                Text = "🖥️ Konsola",
                Location = new Point(360, 30),
                Size = new Size(150, 45),
                Font = new Font("Segoe UI", 10F),
                BackColor = Color.FromArgb(52, 73, 94),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnRunConsole.FlatAppearance.BorderSize = 0;
            btnRunConsole.Click += (s, e) => ControlService(ServiceAction.RunConsole);
            grpInstall.Controls.Add(btnRunConsole);

            btnDiagnostics = new Button
            {
                Text = "🔍 Diagnostyka",
                Location = new Point(530, 30),
                Size = new Size(150, 45),
                Font = new Font("Segoe UI", 10F),
                BackColor = Color.FromArgb(155, 89, 182),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnDiagnostics.FlatAppearance.BorderSize = 0;
            btnDiagnostics.Click += (s, e) => ControlService(ServiceAction.Diagnostics);
            grpInstall.Controls.Add(btnDiagnostics);

            panelService.Controls.Add(grpInstall);

            contentPanel.Controls.Add(panelService);
        }

        // Helper methods
        private Label AddLabel(Panel panel, string text, int y)
        {
            var label = new Label
            {
                Text = text,
                Location = new Point(20, y + 5),
                Size = new Size(170, 20),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 73, 94)
            };
            panel.Controls.Add(label);
            return label;
        }

        private TextBox AddTextBox(Panel panel, int y, string defaultValue = "", int width = 400)
        {
            var textBox = new TextBox
            {
                Location = new Point(200, y),
                Size = new Size(width, 25),
                Text = defaultValue,
                Font = new Font("Segoe UI", 10F)
            };
            panel.Controls.Add(textBox);
            return textBox;
        }

        private void ShowPanel(string tag)
        {
            // Hide all panels
            panelDB2.Visible = false;
            panelExport.Visible = false;
            panelVehicles.Visible = false;
            panelService.Visible = false;

            // Update title and show appropriate panel
            switch (tag)
            {
                case "db2":
                    lblTitle.Text = "🗄️ Konfiguracja DB2";
                    panelDB2.Visible = true;
                    break;
                case "export":
                    lblTitle.Text = "📊 Konfiguracja eksportu";
                    panelExport.Visible = true;
                    break;
                case "vehicles":
                    lblTitle.Text = "🚌 Konfiguracja pojazdów";
                    panelVehicles.Visible = true;
                    break;
                case "service":
                    lblTitle.Text = "⚙️ Zarządzanie serwisem";
                    panelService.Visible = true;
                    UpdateServiceStatus();
                    break;
            }
        }

        private void Sidebar_NavigationChanged(object? sender, string tag)
        {
            ShowPanel(tag);
        }

        private void BtnToggleTheme_Click(object? sender, EventArgs e)
        {
            ThemeManager.ToggleTheme();
            var theme = ThemeManager.GetCurrentTheme();

            // Update theme button icon
            btnToggleTheme.Text = ThemeManager.CurrentTheme == AppTheme.Dark ? "☀️" : "🌙";

            // Apply theme to all controls
            ThemeManager.ApplyTheme(this);

            // Update sidebar theme
            sidebar.IsDarkMode = (ThemeManager.CurrentTheme == AppTheme.Dark);

            // Update Windows 11 title bar
            Windows11ThemeHelper.UseImmersiveDarkMode(this, ThemeManager.CurrentTheme == AppTheme.Dark);

            // Save preference
            ThemeManager.SaveThemePreference();
        }
    }
}
