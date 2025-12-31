using System;
using System.Drawing;
using System.Windows.Forms;

#pragma warning disable CS8669 // Nullable annotations in generated code
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
        private Button btnTestConnection;
        private Label lblConnectionStatus;

        // Export Controls - Basic
        private TextBox txtExportPath;
        private TextBox txtLogPath;
        private TextBox txtScheduleTime;
        private NumericUpDown numDaysBack;
        private TextBox txtKodExportu;

        // Export Controls - File Management
        private CheckBox chkEnableZipCompression;
        private NumericUpDown numFileRetentionDays;
        private CheckBox chkEnableAutoArchiving;
        private TextBox txtArchivePath;
        private Button btnBrowseArchivePath;

        // Export Controls - Performance
        private NumericUpDown numMaxParallelTasks;
        private NumericUpDown numBatchSize;

        // Export Controls - Resilience
        private NumericUpDown numRetryCount;
        private NumericUpDown numRetryDelaySeconds;
        private NumericUpDown numCircuitBreakerFailures;
        private NumericUpDown numCircuitBreakerDuration;

        // Export Controls - Monitoring
        private CheckBox chkEnableDetailedLogging;
        private CheckBox chkEnableMetrics;
        private CheckBox chkEnableEmailNotifications;
        private TextBox txtNotificationEmail;

        // Export Controls - Periodic Monitoring & Triggers
        private CheckBox chkEnablePeriodicMonitoring;
        private NumericUpDown numMonitoringIntervalMinutes;
        private NumericUpDown numMonitoringDaysBack;
        private TextBox txtTriggerFolderPath;
        private Button btnBrowseTriggerFolder;

        // Export Controls - Enabled Export Types
        private CheckBox chkExportTypeBramkiBasic;
        private CheckBox chkExportTypeBramkiDetail;
        private CheckBox chkExportTypePunktualnosc;

        // Vehicles Controls - Old (deprecated, kept for compatibility)
        private ComboBox cmbPojazdyMode;
        private NumericUpDown numPojazdyStart;
        private NumericUpDown numPojazdyEnd;
        private TextBox txtPojazdyLista;  // Hidden, used for config storage

        // Vehicles Controls - New Unified Interface
        private TextBox txtVehicleInput;
        private Button btnParseAndFetch;
        private Label lblParseStatus;
        private DataGridView dgvVehicles;
        private CheckBox chkSelectAll;
        private Button btnApplySelection;

        // Vehicles - Fetch from DB2
        private NumericUpDown numFetchNbFrom;
        private NumericUpDown numFetchNbTo;
        private CheckBox chkFetchActiveOnly;
        private Button btnFetchVehicles;
        private Label lblFetchStatus;

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
                Size = new Size(110, 35),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(39, 174, 96),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnSave.Location = new Point(this.ClientSize.Width - btnSave.Width - 20, 12);
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.FlatAppearance.MouseOverBackColor = Color.FromArgb(46, 204, 113);
            btnSave.Click += (s, e) => SaveSettings();
            headerPanel.Controls.Add(btnSave);

            // Update button position when form resizes
            this.Resize += (s, e) =>
            {
                btnSave.Location = new Point(this.ClientSize.Width - btnSave.Width - 20, 12);
            };
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
            y += 60;

            // Test Connection Button
            btnTestConnection = new Button
            {
                Text = "🔌 Test połączenia",
                Location = new Point(200, y),
                Size = new Size(180, 40),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnTestConnection.FlatAppearance.BorderSize = 0;
            btnTestConnection.FlatAppearance.MouseOverBackColor = Color.FromArgb(41, 128, 185);
            btnTestConnection.Click += BtnTestConnection_Click;
            panelDB2.Controls.Add(btnTestConnection);

            lblConnectionStatus = new Label
            {
                Text = "Kliknij przycisk aby przetestować połączenie",
                Location = new Point(390, y + 10),
                Size = new Size(450, 25),
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 9F)
            };
            panelDB2.Controls.Add(lblConnectionStatus);
            y += 60;

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
            y += 60;

            // === FILE MANAGEMENT SECTION ===
            var grpFileManagement = new GroupBox
            {
                Text = "Zarządzanie plikami",
                Location = new Point(20, y),
                Size = new Size(750, 180),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };

            chkEnableZipCompression = new CheckBox
            {
                Text = "Kompresja ZIP",
                Location = new Point(20, 30),
                Size = new Size(200, 25),
                Checked = true,
                Font = new Font("Segoe UI", 9F)
            };

            chkEnableAutoArchiving = new CheckBox
            {
                Text = "Auto-archiwizacja",
                Location = new Point(20, 60),
                Size = new Size(200, 25),
                Checked = true,
                Font = new Font("Segoe UI", 9F)
            };

            var lblRetention = new Label
            {
                Text = "Retencja plików (dni):",
                Location = new Point(20, 95),
                Size = new Size(150, 20),
                Font = new Font("Segoe UI", 9F)
            };
            numFileRetentionDays = new NumericUpDown
            {
                Location = new Point(180, 92),
                Size = new Size(100, 25),
                Minimum = 1,
                Maximum = 365,
                Value = 90
            };

            var lblArchivePath = new Label
            {
                Text = "Ścieżka archiwum:",
                Location = new Point(20, 125),
                Size = new Size(150, 20),
                Font = new Font("Segoe UI", 9F)
            };
            txtArchivePath = new TextBox
            {
                Location = new Point(180, 122),
                Size = new Size(400, 25),
                Font = new Font("Segoe UI", 9F)
            };
            btnBrowseArchivePath = new Button
            {
                Text = "...",
                Location = new Point(590, 122),
                Size = new Size(40, 25)
            };
            btnBrowseArchivePath.Click += (s, e) =>
            {
                using var dialog = new FolderBrowserDialog();
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    txtArchivePath.Text = dialog.SelectedPath;
                }
            };

            grpFileManagement.Controls.AddRange(new Control[] {
                chkEnableZipCompression, chkEnableAutoArchiving,
                lblRetention, numFileRetentionDays,
                lblArchivePath, txtArchivePath, btnBrowseArchivePath
            });
            panelExport.Controls.Add(grpFileManagement);
            y += 200;

            // === PERFORMANCE SECTION ===
            var grpPerformance = new GroupBox
            {
                Text = "Wydajność",
                Location = new Point(20, y),
                Size = new Size(750, 100),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };

            var lblParallelTasks = new Label
            {
                Text = "Maks. zadań równoległych:",
                Location = new Point(20, 30),
                Size = new Size(180, 20),
                Font = new Font("Segoe UI", 9F)
            };
            numMaxParallelTasks = new NumericUpDown
            {
                Location = new Point(210, 27),
                Size = new Size(100, 25),
                Minimum = 1,
                Maximum = 10,
                Value = 3
            };

            var lblBatchSize = new Label
            {
                Text = "Rozmiar paczki:",
                Location = new Point(20, 60),
                Size = new Size(180, 20),
                Font = new Font("Segoe UI", 9F)
            };
            numBatchSize = new NumericUpDown
            {
                Location = new Point(210, 57),
                Size = new Size(100, 25),
                Minimum = 100,
                Maximum = 10000,
                Value = 1000,
                Increment = 100
            };

            grpPerformance.Controls.AddRange(new Control[] {
                lblParallelTasks, numMaxParallelTasks,
                lblBatchSize, numBatchSize
            });
            panelExport.Controls.Add(grpPerformance);
            y += 120;

            // === RESILIENCE SECTION ===
            var grpResilience = new GroupBox
            {
                Text = "Odporność na błędy (Polly)",
                Location = new Point(20, y),
                Size = new Size(750, 150),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };

            var lblRetryCount = new Label
            {
                Text = "Liczba ponowień:",
                Location = new Point(20, 30),
                Size = new Size(180, 20),
                Font = new Font("Segoe UI", 9F)
            };
            numRetryCount = new NumericUpDown
            {
                Location = new Point(210, 27),
                Size = new Size(100, 25),
                Minimum = 0,
                Maximum = 10,
                Value = 3
            };

            var lblRetryDelay = new Label
            {
                Text = "Opóźnienie ponowienia (s):",
                Location = new Point(20, 60),
                Size = new Size(180, 20),
                Font = new Font("Segoe UI", 9F)
            };
            numRetryDelaySeconds = new NumericUpDown
            {
                Location = new Point(210, 57),
                Size = new Size(100, 25),
                Minimum = 1,
                Maximum = 60,
                Value = 5
            };

            var lblCircuitBreakerFailures = new Label
            {
                Text = "Próg błędów Circuit Breaker:",
                Location = new Point(20, 90),
                Size = new Size(180, 20),
                Font = new Font("Segoe UI", 9F)
            };
            numCircuitBreakerFailures = new NumericUpDown
            {
                Location = new Point(210, 87),
                Size = new Size(100, 25),
                Minimum = 1,
                Maximum = 20,
                Value = 5
            };

            var lblCircuitBreakerDuration = new Label
            {
                Text = "Czas otwarcia CB (s):",
                Location = new Point(20, 120),
                Size = new Size(180, 20),
                Font = new Font("Segoe UI", 9F)
            };
            numCircuitBreakerDuration = new NumericUpDown
            {
                Location = new Point(210, 117),
                Size = new Size(100, 25),
                Minimum = 10,
                Maximum = 300,
                Value = 60
            };

            grpResilience.Controls.AddRange(new Control[] {
                lblRetryCount, numRetryCount,
                lblRetryDelay, numRetryDelaySeconds,
                lblCircuitBreakerFailures, numCircuitBreakerFailures,
                lblCircuitBreakerDuration, numCircuitBreakerDuration
            });
            panelExport.Controls.Add(grpResilience);
            y += 170;

            // === MONITORING SECTION ===
            var grpMonitoring = new GroupBox
            {
                Text = "Monitorowanie",
                Location = new Point(20, y),
                Size = new Size(750, 140),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };

            chkEnableDetailedLogging = new CheckBox
            {
                Text = "Szczegółowe logowanie",
                Location = new Point(20, 30),
                Size = new Size(200, 25),
                Checked = true,
                Font = new Font("Segoe UI", 9F)
            };

            chkEnableMetrics = new CheckBox
            {
                Text = "Metryki wydajności",
                Location = new Point(20, 60),
                Size = new Size(200, 25),
                Checked = true,
                Font = new Font("Segoe UI", 9F)
            };

            chkEnableEmailNotifications = new CheckBox
            {
                Text = "Powiadomienia email",
                Location = new Point(20, 90),
                Size = new Size(200, 25),
                Checked = false,
                Font = new Font("Segoe UI", 9F)
            };

            var lblNotificationEmail = new Label
            {
                Text = "Email:",
                Location = new Point(230, 93),
                Size = new Size(50, 20),
                Font = new Font("Segoe UI", 9F)
            };
            txtNotificationEmail = new TextBox
            {
                Location = new Point(290, 90),
                Size = new Size(300, 25),
                Enabled = false,
                Font = new Font("Segoe UI", 9F)
            };

            chkEnableEmailNotifications.CheckedChanged += (s, e) =>
            {
                txtNotificationEmail.Enabled = chkEnableEmailNotifications.Checked;
            };

            grpMonitoring.Controls.AddRange(new Control[] {
                chkEnableDetailedLogging, chkEnableMetrics,
                chkEnableEmailNotifications, lblNotificationEmail, txtNotificationEmail
            });
            panelExport.Controls.Add(grpMonitoring);
            y += 160;

            // === PERIODIC MONITORING & TRIGGERS ===
            y += 10; // Odstęp od poprzedniej sekcji

            var grpPeriodicMonitoring = new GroupBox
            {
                Text = "Periodic Monitoring & Triggery",
                Location = new Point(20, y),
                Size = new Size(750, 200),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };

            chkEnablePeriodicMonitoring = new CheckBox
            {
                Text = "Włącz periodic monitoring (sprawdzanie co X minut)",
                Location = new Point(20, 30),
                Size = new Size(400, 25),
                Checked = false,
                Font = new Font("Segoe UI", 9F)
            };
            grpPeriodicMonitoring.Controls.Add(chkEnablePeriodicMonitoring);

            var lblMonitoringInterval = new Label
            {
                Text = "Interwał monitorowania (min):",
                Location = new Point(20, 65),
                Size = new Size(200, 20),
                Font = new Font("Segoe UI", 9F)
            };
            grpPeriodicMonitoring.Controls.Add(lblMonitoringInterval);

            numMonitoringIntervalMinutes = new NumericUpDown
            {
                Location = new Point(230, 62),
                Size = new Size(100, 25),
                Minimum = 1,
                Maximum = 1440,
                Value = 15,
                Enabled = false
            };
            grpPeriodicMonitoring.Controls.Add(numMonitoringIntervalMinutes);

            var lblMonitoringDaysBack = new Label
            {
                Text = "Sprawdzaj ostatnie N dni:",
                Location = new Point(20, 95),
                Size = new Size(200, 20),
                Font = new Font("Segoe UI", 9F)
            };
            grpPeriodicMonitoring.Controls.Add(lblMonitoringDaysBack);

            numMonitoringDaysBack = new NumericUpDown
            {
                Location = new Point(230, 92),
                Size = new Size(100, 25),
                Minimum = 1,
                Maximum = 30,
                Value = 7,
                Enabled = false
            };
            grpPeriodicMonitoring.Controls.Add(numMonitoringDaysBack);

            var lblTriggerFolder = new Label
            {
                Text = "Folder triggerów JSON:",
                Location = new Point(20, 125),
                Size = new Size(200, 20),
                Font = new Font("Segoe UI", 9F)
            };
            grpPeriodicMonitoring.Controls.Add(lblTriggerFolder);

            txtTriggerFolderPath = new TextBox
            {
                Location = new Point(230, 122),
                Size = new Size(350, 25),
                Font = new Font("Segoe UI", 9F),
                Text = @"C:\Services\DB2Export\Triggers"
            };
            grpPeriodicMonitoring.Controls.Add(txtTriggerFolderPath);

            btnBrowseTriggerFolder = new Button
            {
                Text = "...",
                Location = new Point(590, 122),
                Size = new Size(40, 25)
            };
            btnBrowseTriggerFolder.Click += (s, e) =>
            {
                using var dialog = new FolderBrowserDialog();
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    txtTriggerFolderPath.Text = dialog.SelectedPath;
                }
            };
            grpPeriodicMonitoring.Controls.Add(btnBrowseTriggerFolder);

            // Enable/Disable controls
            chkEnablePeriodicMonitoring.CheckedChanged += (s, e) =>
            {
                numMonitoringIntervalMinutes.Enabled = chkEnablePeriodicMonitoring.Checked;
                numMonitoringDaysBack.Enabled = chkEnablePeriodicMonitoring.Checked;
            };

            panelExport.Controls.Add(grpPeriodicMonitoring);
            y += 210;

            // === ENABLED EXPORT TYPES ===
            var grpEnabledExportTypes = new GroupBox
            {
                Text = "Włączone typy eksportu",
                Location = new Point(20, y),
                Size = new Size(750, 100),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };

            chkExportTypeBramkiBasic = new CheckBox
            {
                Text = "BramkiBasic (WS/WYS)",
                Location = new Point(20, 30),
                Size = new Size(200, 25),
                Checked = true,
                Font = new Font("Segoe UI", 9F)
            };
            grpEnabledExportTypes.Controls.Add(chkExportTypeBramkiBasic);

            chkExportTypeBramkiDetail = new CheckBox
            {
                Text = "BramkiDetail (4 drzwi)",
                Location = new Point(20, 60),
                Size = new Size(200, 25),
                Checked = true,
                Font = new Font("Segoe UI", 9F)
            };
            grpEnabledExportTypes.Controls.Add(chkExportTypeBramkiDetail);

            chkExportTypePunktualnosc = new CheckBox
            {
                Text = "Punktualność (placeholder)",
                Location = new Point(240, 30),
                Size = new Size(250, 25),
                Checked = false,
                Font = new Font("Segoe UI", 9F)
            };
            grpEnabledExportTypes.Controls.Add(chkExportTypePunktualnosc);

            panelExport.Controls.Add(grpEnabledExportTypes);
            y += 110;

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

            // === OLD CONTROLS (Hidden for backward compatibility) ===
            // Mode
            cmbPojazdyMode = new ComboBox
            {
                Location = new Point(200, y),
                Size = new Size(200, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Visible = false  // Hidden
            };
            cmbPojazdyMode.Items.AddRange(new object[] { "lista", "zakres" });
            cmbPojazdyMode.SelectedIndex = 0;
            panelVehicles.Controls.Add(cmbPojazdyMode);

            // Range controls (hidden)
            numPojazdyStart = new NumericUpDown
            {
                Location = new Point(110, y),
                Size = new Size(120, 25),
                Minimum = 1,
                Maximum = 9999,
                Value = 2209,
                Visible = false
            };
            panelVehicles.Controls.Add(numPojazdyStart);

            numPojazdyEnd = new NumericUpDown
            {
                Location = new Point(370, y),
                Size = new Size(120, 25),
                Minimum = 1,
                Maximum = 9999,
                Value = 2238,
                Visible = false
            };
            panelVehicles.Controls.Add(numPojazdyEnd);

            // Old list TextBox (hidden, used for config storage)
            txtPojazdyLista = new TextBox
            {
                Location = new Point(15, y),
                Size = new Size(670, 70),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Visible = false  // Hidden, used only for config storage
            };
            panelVehicles.Controls.Add(txtPojazdyLista);

            // === NEW UNIFIED INTERFACE ===

            // Unified Input Group
            var grpUnifiedInput = new GroupBox
            {
                Text = "Wprowadź numery pojazdów (format: 100-120, 789, 900-905)",
                Location = new Point(20, y),
                Size = new Size(700, 120),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };

            txtVehicleInput = new TextBox
            {
                Location = new Point(15, 30),
                Size = new Size(550, 25),
                Font = new Font("Segoe UI", 10F)
            };
            grpUnifiedInput.Controls.Add(txtVehicleInput);

            btnParseAndFetch = new Button
            {
                Text = "Pobierz pojazdy",
                Location = new Point(575, 27),
                Size = new Size(110, 30),
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnParseAndFetch.FlatAppearance.BorderSize = 0;
            btnParseAndFetch.Click += BtnParseAndFetch_Click;
            grpUnifiedInput.Controls.Add(btnParseAndFetch);

            lblParseStatus = new Label
            {
                Location = new Point(15, 65),
                Size = new Size(670, 40),
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 9F)
            };
            grpUnifiedInput.Controls.Add(lblParseStatus);

            panelVehicles.Controls.Add(grpUnifiedInput);
            y += 130;

            // Vehicle Selection Grid
            var grpVehicleGrid = new GroupBox
            {
                Text = "Wybór pojazdów",
                Location = new Point(20, y),
                Size = new Size(700, 420),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };

            chkSelectAll = new CheckBox
            {
                Text = "Zaznacz wszystkie",
                Location = new Point(15, 28),
                Size = new Size(150, 20),
                Font = new Font("Segoe UI", 9F)
            };
            chkSelectAll.CheckedChanged += (s, e) => {
                if (dgvVehicles != null && dgvVehicles.Rows.Count > 0)
                {
                    foreach (DataGridViewRow row in dgvVehicles.Rows)
                        row.Cells["Selected"].Value = chkSelectAll.Checked;
                }
            };
            grpVehicleGrid.Controls.Add(chkSelectAll);

            dgvVehicles = new DataGridView
            {
                Location = new Point(15, 55),
                Size = new Size(670, 300),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ReadOnly = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.Fixed3D
            };

            // Define columns
            dgvVehicles.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name = "Selected",
                HeaderText = "✓",
                Width = 40,
                ReadOnly = false
            });
            dgvVehicles.Columns.Add(new DataGridViewTextBoxColumn { Name = "NB", HeaderText = "NB", ReadOnly = true, Width = 60 });
            dgvVehicles.Columns.Add(new DataGridViewTextBoxColumn { Name = "NR", HeaderText = "Nr rej.", ReadOnly = true });
            dgvVehicles.Columns.Add(new DataGridViewTextBoxColumn { Name = "TYP_POJ", HeaderText = "Typ", ReadOnly = true, Width = 60 });
            dgvVehicles.Columns.Add(new DataGridViewTextBoxColumn { Name = "ZAJEZDNIA", HeaderText = "Zajezdnia", ReadOnly = true, Width = 80 });
            dgvVehicles.Columns.Add(new DataGridViewTextBoxColumn { Name = "MA_BRAMKI", HeaderText = "Bramki", ReadOnly = true, Width = 70 });
            dgvVehicles.Columns.Add(new DataGridViewTextBoxColumn { Name = "WGOTOWOSCI", HeaderText = "Aktywny", ReadOnly = true, Width = 70 });

            grpVehicleGrid.Controls.Add(dgvVehicles);

            btnApplySelection = new Button
            {
                Text = "Zastosuj wybór",
                Location = new Point(560, 365),
                Size = new Size(125, 35),
                BackColor = Color.FromArgb(39, 174, 96),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnApplySelection.FlatAppearance.BorderSize = 0;
            btnApplySelection.Click += BtnApplySelection_Click;
            grpVehicleGrid.Controls.Add(btnApplySelection);

            panelVehicles.Controls.Add(grpVehicleGrid);
            y += 430;

            // === FETCH VEHICLES FROM DB2 ===
            var grpFetchVehicles = new GroupBox
            {
                Text = "Pobierz pojazdy z bazy DB2",
                Location = new Point(20, y),
                Size = new Size(700, 200),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };

            var lblFetchHint = new Label
            {
                Text = "Filtruj pojazdy z bazy danych i automatycznie wypełnij listę",
                Location = new Point(20, 25),
                Size = new Size(650, 20),
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 9F, FontStyle.Italic)
            };
            grpFetchVehicles.Controls.Add(lblFetchHint);

            var lblNbFrom = new Label
            {
                Text = "NB od:",
                Location = new Point(20, 55),
                Size = new Size(120, 20),
                Font = new Font("Segoe UI", 9F)
            };
            grpFetchVehicles.Controls.Add(lblNbFrom);

            numFetchNbFrom = new NumericUpDown
            {
                Location = new Point(150, 52),
                Size = new Size(120, 25),
                Minimum = 0,
                Maximum = 9999,
                Value = 0
            };
            grpFetchVehicles.Controls.Add(numFetchNbFrom);

            var lblNbTo = new Label
            {
                Text = "NB do:",
                Location = new Point(290, 55),
                Size = new Size(60, 20),
                Font = new Font("Segoe UI", 9F)
            };
            grpFetchVehicles.Controls.Add(lblNbTo);

            numFetchNbTo = new NumericUpDown
            {
                Location = new Point(360, 52),
                Size = new Size(120, 25),
                Minimum = 0,
                Maximum = 9999,
                Value = 0
            };
            grpFetchVehicles.Controls.Add(numFetchNbTo);

            chkFetchActiveOnly = new CheckBox
            {
                Text = "Tylko aktywne pojazdy",
                Location = new Point(20, 90),
                Size = new Size(250, 25),
                Checked = true,
                Font = new Font("Segoe UI", 9F)
            };
            grpFetchVehicles.Controls.Add(chkFetchActiveOnly);

            btnFetchVehicles = new Button
            {
                Text = "📥 Pobierz pojazdy z DB2",
                Location = new Point(20, 125),
                Size = new Size(220, 45),
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnFetchVehicles.FlatAppearance.BorderSize = 0;
            btnFetchVehicles.Click += BtnFetchVehicles_Click;
            grpFetchVehicles.Controls.Add(btnFetchVehicles);

            lblFetchStatus = new Label
            {
                Text = "Gotowy do pobrania",
                Location = new Point(250, 135),
                Size = new Size(430, 30),
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 9F)
            };
            grpFetchVehicles.Controls.Add(lblFetchStatus);

            panelVehicles.Controls.Add(grpFetchVehicles);
            y += 210;

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
