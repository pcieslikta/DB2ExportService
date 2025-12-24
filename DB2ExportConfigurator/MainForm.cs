using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text.Json;
using System.Windows.Forms;

namespace DB2ExportConfigurator
{
    public partial class MainForm : Form
    {
        private ExportSettings _settings = null!;
        private readonly string _configPath;
        private readonly string _servicePath;
        private bool _isDarkMode = false;

        // Service control
        private const string SERVICE_NAME = "RGExportService";

        public MainForm()
        {
            _configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "DB2Export", "appsettings.json");

            // Fallback to service directory
            if (!File.Exists(_configPath))
            {
                _configPath = @"C:\Services\DB2Export\appsettings.json";
            }

            _servicePath = @"C:\Services\DB2Export";

            InitializeComponent();
            InitializeSidebar();
            InitializeTheme();
            LoadSettings();
            CheckAdminRights();
            UpdateServiceStatus();
        }

        private void InitializeSidebar()
        {
            var items = new List<SidebarItem>
            {
                new SidebarItem("🗄️", "DB2", "db2"),
                new SidebarItem("📊", "Eksport", "export"),
                new SidebarItem("🚌", "Pojazdy", "vehicles"),
                new SidebarItem("⚙️", "Serwis", "service")
            };

            sidebar.SetItems(items);
        }

        private void InitializeTheme()
        {
            // Load saved theme preference
            ThemeManager.LoadThemePreference();

            // Apply initial theme
            var theme = ThemeManager.GetCurrentTheme();
            ThemeManager.ApplyTheme(this);
            sidebar.IsDarkMode = (ThemeManager.CurrentTheme == AppTheme.Dark);
            Windows11ThemeHelper.UseImmersiveDarkMode(this, ThemeManager.CurrentTheme == AppTheme.Dark);

            // Subscribe to theme changes
            ThemeManager.OnThemeChanged += (s, e) =>
            {
                var currentTheme = ThemeManager.GetCurrentTheme();
                ThemeManager.ApplyTheme(this);
                sidebar.IsDarkMode = (ThemeManager.CurrentTheme == AppTheme.Dark);
                Windows11ThemeHelper.UseImmersiveDarkMode(this, ThemeManager.CurrentTheme == AppTheme.Dark);
            };
        }

        private void CheckAdminRights()
        {
            using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            bool isAdmin = principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);

            if (!isAdmin)
            {
                MessageBox.Show(
                    "⚠️  OSTRZEŻENIE: BRAK UPRAWNIEŃ ADMINISTRATORA\n\n" +
                    "Niektóre funkcje mogą nie działać poprawnie:\n" +
                    "• Zarządzanie serwisem Windows\n" +
                    "• Zapisywanie konfiguracji\n\n" +
                    "Uruchom aplikację jako Administrator.",
                    "Uprawnienia",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
            }
        }

        private void LoadSettings()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    var json = File.ReadAllText(_configPath);
                    _settings = JsonSerializer.Deserialize<ExportSettings>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        ReadCommentHandling = JsonCommentHandling.Skip
                    }) ?? new ExportSettings();

                    PopulateForm();
                }
                else
                {
                    _settings = new ExportSettings();
                    MessageBox.Show(
                        $"Nie znaleziono pliku konfiguracji:\n{_configPath}\n\nZostanie utworzona nowa konfiguracja.",
                        "Konfiguracja",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd wczytywania konfiguracji:\n{ex.Message}", "Błąd",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                _settings = new ExportSettings();
            }
        }

        private void PopulateForm()
        {
            // DB2 Configuration
            txtDatabase.Text = _settings.DB2?.Database ?? "";
            txtHostname.Text = _settings.DB2?.Hostname ?? "";
            txtPort.Text = _settings.DB2?.Port.ToString() ?? "50000";
            txtUser.Text = _settings.DB2?.User ?? "";
            txtPassword.Text = _settings.DB2?.Password ?? "";
            chkUseCredentialManager.Checked = _settings.DB2?.UseCredentialManager ?? false;
            txtCredentialKey.Text = _settings.DB2?.CredentialKey ?? "DB2Export_PROD";

            // Export Configuration
            txtExportPath.Text = _settings.ExportConfig?.ExportPath ?? @"C:\EXPORT\";
            txtLogPath.Text = _settings.ExportConfig?.LogPath ?? @"C:\EXPORT\LOG\";
            txtScheduleTime.Text = _settings.ExportConfig?.ScheduleTime ?? "13:15";
            numDaysBack.Value = Math.Abs(_settings.ExportConfig?.DaysBack ?? -2);
            txtKodExportu.Text = _settings.ExportConfig?.KodExportu ?? "SOSNO";

            // Vehicle Configuration
            cmbPojazdyMode.SelectedItem = _settings.VehicleConfig?.PojazdyMode ?? "lista";
            numPojazdyStart.Value = _settings.VehicleConfig?.PojazdyStart ?? 2209;
            numPojazdyEnd.Value = _settings.VehicleConfig?.PojazdyEnd ?? 2238;

            if (_settings.VehicleConfig?.PojazdyLista != null && _settings.VehicleConfig.PojazdyLista.Any())
            {
                txtPojazdyLista.Text = string.Join(", ", _settings.VehicleConfig.PojazdyLista);
            }

            UpdateVehicleModeControls();
        }

        private void SaveSettings()
        {
            try
            {
                // Update settings from form
                _settings.DB2 = new DB2Config
                {
                    Database = txtDatabase.Text,
                    Hostname = txtHostname.Text,
                    Port = int.Parse(txtPort.Text),
                    Protocol = "TCPIP",
                    User = txtUser.Text,
                    Password = txtPassword.Text,
                    UseCredentialManager = chkUseCredentialManager.Checked,
                    CredentialKey = txtCredentialKey.Text,
                    CCSID = 1250
                };

                _settings.ExportConfig = new ExportConfig
                {
                    KodExportu = txtKodExportu.Text,
                    ExportPath = txtExportPath.Text,
                    LogPath = txtLogPath.Text,
                    ScheduleTime = txtScheduleTime.Text,
                    DaysBack = -(int)numDaysBack.Value
                };

                _settings.VehicleConfig = new VehicleConfig
                {
                    KodExportu = txtKodExportu.Text,
                    PojazdyMode = cmbPojazdyMode.SelectedItem?.ToString() ?? "lista",
                    PojazdyStart = (int)numPojazdyStart.Value,
                    PojazdyEnd = (int)numPojazdyEnd.Value,
                    PojazdyLista = ParseVehicleList(txtPojazdyLista.Text)
                };

                var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                // Ensure directory exists
                var dir = Path.GetDirectoryName(_configPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                File.WriteAllText(_configPath, json);

                MessageBox.Show("Konfiguracja została zapisana pomyślnie!", "Sukces",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd zapisywania konfiguracji:\n{ex.Message}", "Błąd",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private List<int> ParseVehicleList(string text)
        {
            var result = new List<int>();
            if (string.IsNullOrWhiteSpace(text)) return result;

            var parts = text.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                if (int.TryParse(part.Trim(), out int vehicleId))
                {
                    result.Add(vehicleId);
                }
            }
            return result;
        }

        private void UpdateVehicleModeControls()
        {
            var mode = cmbPojazdyMode.SelectedItem?.ToString() ?? "lista";

            if (mode == "lista")
            {
                txtPojazdyLista.Enabled = true;
                numPojazdyStart.Enabled = false;
                numPojazdyEnd.Enabled = false;
            }
            else
            {
                txtPojazdyLista.Enabled = false;
                numPojazdyStart.Enabled = true;
                numPojazdyEnd.Enabled = true;
            }
        }

        private void UpdateServiceStatus()
        {
            lblServiceStatus.Text = $"Status: {ServiceController.GetServiceStatusText()}";

            var status = ServiceController.GetServiceStatus();
            lblServiceStatus.ForeColor = status switch
            {
                ServiceController.ServiceStatus.Running => Color.Green,
                ServiceController.ServiceStatus.Stopped => Color.Red,
                ServiceController.ServiceStatus.NotInstalled => Color.Gray,
                _ => Color.Orange
            };

            btnStartService.Enabled = (status == ServiceController.ServiceStatus.Stopped);
            btnStopService.Enabled = (status == ServiceController.ServiceStatus.Running);
            btnRestartService.Enabled = (status == ServiceController.ServiceStatus.Running);
        }

        private async void ControlService(ServiceAction action)
        {
            try
            {
                bool success = false;

                switch (action)
                {
                    case ServiceAction.Start:
                        success = await ServiceController.StartServiceAsync();
                        break;

                    case ServiceAction.Stop:
                        success = await ServiceController.StopServiceAsync();
                        break;

                    case ServiceAction.Restart:
                        success = await ServiceController.RestartServiceAsync();
                        break;

                    case ServiceAction.Install:
                        success = ServiceController.InstallService();
                        if (success)
                        {
                            MessageBox.Show("Usługa została zainstalowana pomyślnie!\n\nMożesz teraz uruchomić serwis.",
                                "Instalacja zakończona", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        break;

                    case ServiceAction.Uninstall:
                        var result = MessageBox.Show(
                            "Czy na pewno chcesz odinstalować usługę?\n\nUsługa zostanie zatrzymana i usunięta z systemu.",
                            "Potwierdzenie",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question);

                        if (result == DialogResult.Yes)
                        {
                            success = ServiceController.UninstallService();
                            if (success)
                            {
                                MessageBox.Show("Usługa została odinstalowana pomyślnie!",
                                    "Dezinstalacja zakończona", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                        }
                        break;

                    case ServiceAction.RunConsole:
                        success = ServiceController.RunAsConsole();
                        if (success)
                        {
                            MessageBox.Show("Serwis został uruchomiony w trybie konsoli.\n\nSprawdź okno konsoli, aby zobaczyć logi.",
                                "Konsola uruchomiona", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        break;

                    case ServiceAction.Diagnostics:
                        ServiceController.ShowServiceDiagnostics();
                        success = true;
                        break;
                }

                UpdateServiceStatus();

                if (success && action == ServiceAction.Start)
                {
                    MessageBox.Show("Usługa została uruchomiona pomyślnie!", "Sukces",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else if (success && action == ServiceAction.Stop)
                {
                    MessageBox.Show("Usługa została zatrzymana pomyślnie!", "Sukces",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else if (success && action == ServiceAction.Restart)
                {
                    MessageBox.Show("Usługa została zrestartowana pomyślnie!", "Sukces",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Nieoczekiwany błąd:\n{ex.Message}", "Błąd",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateServiceStatus();
            }
        }

        private enum ServiceAction
        {
            Start,
            Stop,
            Restart,
            Install,
            Uninstall,
            RunConsole,
            Diagnostics
        }
    }

    // Configuration models
    public class ExportSettings
    {
        public ExportConfig? ExportConfig { get; set; }
        public VehicleConfig? VehicleConfig { get; set; }
        public DB2Config? DB2 { get; set; }
    }

    public class ExportConfig
    {
        public string KodExportu { get; set; } = string.Empty;
        public string ExportPath { get; set; } = @"C:\EXPORT\";
        public string LogPath { get; set; } = @"C:\EXPORT\LOG\";
        public string ScheduleTime { get; set; } = "13:15";
        public int DaysBack { get; set; } = -2;
    }

    public class VehicleConfig
    {
        public string KodExportu { get; set; } = string.Empty;
        public string PojazdyMode { get; set; } = "lista";
        public int PojazdyStart { get; set; } = 2209;
        public int PojazdyEnd { get; set; } = 2238;
        public List<int> PojazdyLista { get; set; } = new();
    }

    public class DB2Config
    {
        public string Database { get; set; } = string.Empty;
        public string Hostname { get; set; } = string.Empty;
        public int Port { get; set; } = 50000;
        public string Protocol { get; set; } = "TCPIP";
        public string User { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool UseCredentialManager { get; set; } = false;
        public string CredentialKey { get; set; } = string.Empty;
        public int CCSID { get; set; } = 1250;
    }
}
