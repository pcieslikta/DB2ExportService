using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text.Json;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DB2ExportService.Models;

namespace DB2ExportConfigurator
{
    public partial class MainForm : Form
    {
        private ExportSettings _settings = null!;
        private readonly string _configPath;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MainForm> _logger;

        // Service control
        private const string SERVICE_NAME = "RGExportService";

        public MainForm(IServiceProvider serviceProvider, ILogger<MainForm> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;

            _logger.LogInformation("Inicjalizacja MainForm");


            _configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "DB2Export", "appsettings.json");

            // Fallback to service directory
            if (!File.Exists(_configPath))
            {
                _configPath = @"C:\Services\DB2Export\appsettings.json";
            }

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

            // Export Configuration - Basic
            txtExportPath.Text = _settings.ExportConfig?.ExportPath ?? @"C:\EXPORT\";
            txtLogPath.Text = _settings.ExportConfig?.LogPath ?? @"C:\EXPORT\LOG\";
            txtScheduleTime.Text = _settings.ExportConfig?.ScheduleTime ?? "13:15";
            numDaysBack.Value = Math.Abs(_settings.ExportConfig?.DaysBack ?? -2);
            txtKodExportu.Text = _settings.ExportConfig?.KodExportu ?? "SOSNO";

            // Export Configuration - NEW PARAMETERS
            // File Management
            chkEnableZipCompression.Checked = _settings.ExportConfig?.EnableZipCompression ?? true;
            numFileRetentionDays.Value = _settings.ExportConfig?.FileRetentionDays ?? 90;
            chkEnableAutoArchiving.Checked = _settings.ExportConfig?.EnableAutoArchiving ?? true;
            txtArchivePath.Text = _settings.ExportConfig?.ArchivePath ?? "";

            // Performance
            numMaxParallelTasks.Value = _settings.ExportConfig?.MaxParallelTasks ?? 3;
            numBatchSize.Value = _settings.ExportConfig?.BatchSize ?? 1000;

            // Resilience
            numRetryCount.Value = _settings.ExportConfig?.RetryCount ?? 3;
            numRetryDelaySeconds.Value = _settings.ExportConfig?.RetryDelaySeconds ?? 5;
            numCircuitBreakerFailures.Value = _settings.ExportConfig?.CircuitBreakerFailureThreshold ?? 5;
            numCircuitBreakerDuration.Value = _settings.ExportConfig?.CircuitBreakerDurationSeconds ?? 60;

            // Monitoring
            chkEnableDetailedLogging.Checked = _settings.ExportConfig?.EnableDetailedLogging ?? true;
            chkEnableMetrics.Checked = _settings.ExportConfig?.EnableMetrics ?? true;
            chkEnableEmailNotifications.Checked = _settings.ExportConfig?.EnableEmailNotifications ?? false;
            txtNotificationEmail.Text = _settings.ExportConfig?.NotificationEmail ?? "";

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
                    DaysBack = -(int)numDaysBack.Value,

                    // File Management
                    EnableZipCompression = chkEnableZipCompression.Checked,
                    FileRetentionDays = (int)numFileRetentionDays.Value,
                    EnableAutoArchiving = chkEnableAutoArchiving.Checked,
                    ArchivePath = string.IsNullOrWhiteSpace(txtArchivePath.Text) ? null : txtArchivePath.Text,

                    // Performance
                    MaxParallelTasks = (int)numMaxParallelTasks.Value,
                    BatchSize = (int)numBatchSize.Value,

                    // Resilience
                    RetryCount = (int)numRetryCount.Value,
                    RetryDelaySeconds = (int)numRetryDelaySeconds.Value,
                    CircuitBreakerFailureThreshold = (int)numCircuitBreakerFailures.Value,
                    CircuitBreakerDurationSeconds = (int)numCircuitBreakerDuration.Value,

                    // Monitoring
                    EnableDetailedLogging = chkEnableDetailedLogging.Checked,
                    EnableMetrics = chkEnableMetrics.Checked,
                    EnableEmailNotifications = chkEnableEmailNotifications.Checked,
                    NotificationEmail = string.IsNullOrWhiteSpace(txtNotificationEmail.Text) ? null : txtNotificationEmail.Text
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

        private async void BtnTestConnection_Click(object? sender, EventArgs e)
        {
            _logger.LogInformation("========================================");
            _logger.LogInformation("TEST POŁĄCZENIA DB2 - START");
            _logger.LogInformation("========================================");

            try
            {
                btnTestConnection.Enabled = false;
                lblConnectionStatus.Text = "Testowanie połączenia...";
                lblConnectionStatus.ForeColor = Color.Blue;

                // Tymczasowo zapisz ustawienia DB2 do testowania
                var testConfig = new DB2Config
                {
                    Database = txtDatabase.Text,
                    Hostname = txtHostname.Text,
                    Port = int.TryParse(txtPort.Text, out int port) ? port : 50000,
                    Protocol = "TCPIP",
                    User = txtUser.Text,
                    Password = string.IsNullOrEmpty(txtPassword.Text) ? "***EMPTY***" : "***SET***",
                    UseCredentialManager = chkUseCredentialManager.Checked,
                    CredentialKey = txtCredentialKey.Text,
                    CCSID = 1250
                };

                _logger.LogInformation("Konfiguracja połączenia:");
                _logger.LogInformation("  Database: {Database}", testConfig.Database);
                _logger.LogInformation("  Hostname: {Hostname}", testConfig.Hostname);
                _logger.LogInformation("  Port: {Port}", testConfig.Port);
                _logger.LogInformation("  Protocol: {Protocol}", testConfig.Protocol);
                _logger.LogInformation("  User: {User}", string.IsNullOrEmpty(txtUser.Text) ? "***EMPTY***" : txtUser.Text);
                _logger.LogInformation("  Password: {Password}", testConfig.Password);
                _logger.LogInformation("  UseCredentialManager: {UseCredentialManager}", testConfig.UseCredentialManager);
                _logger.LogInformation("  CredentialKey: {CredentialKey}", testConfig.CredentialKey);
                _logger.LogInformation("  CCSID: {CCSID}", testConfig.CCSID);

                // Walidacja podstawowa
                if (string.IsNullOrWhiteSpace(testConfig.Database) ||
                    string.IsNullOrWhiteSpace(testConfig.Hostname))
                {
                    _logger.LogWarning("Walidacja nieudana - brakuje Database lub Hostname");
                    lblConnectionStatus.Text = "✗ Wypełnij wszystkie wymagane pola";
                    lblConnectionStatus.ForeColor = Color.Red;
                    MessageBox.Show("Wypełnij Database i Hostname przed testem połączenia.",
                        "Brakujące dane", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                _logger.LogInformation("Walidacja OK - pobieranie DB2Service z DI...");

                // Test połączenia przez próbę pobrania liczby rekordów
                var db2Service = _serviceProvider.GetRequiredService<DB2ExportService.Services.IDB2Service>();
                _logger.LogInformation("DB2Service otrzymany z DI: {ServiceType}", db2Service.GetType().Name);

                // Próba pobrania danych - to sprawdzi połączenie
                var testDate = DateTime.Now.AddDays(-1);
                _logger.LogInformation("Wykonywanie testowego zapytania GetRecordCountAsync dla daty: {TestDate}", testDate);

                var count = await db2Service.GetRecordCountAsync(testDate);

                _logger.LogInformation("Zapytanie wykonane pomyślnie! Liczba rekordów: {Count}", count);

                lblConnectionStatus.Text = $"✓ Połączenie udane! Testowe zapytanie zwróciło {count?.ToString() ?? "0"} rekordów";
                lblConnectionStatus.ForeColor = Color.Green;

                _logger.LogInformation("TEST POŁĄCZENIA DB2 - SUKCES");

                MessageBox.Show(
                    $"Połączenie z bazą DB2 zostało nawiązane pomyślnie!\n\n" +
                    $"Database: {txtDatabase.Text}\n" +
                    $"Hostname: {txtHostname.Text}:{testConfig.Port}\n" +
                    $"Testowe zapytanie zwróciło: {count?.ToString() ?? "0"} rekordów\n\n" +
                    $"Szczegóły w logach: {Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs")}",
                    "Test połączenia - Sukces",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TEST POŁĄCZENIA DB2 - BŁĄD");
                _logger.LogError("Typ błędu: {ExceptionType}", ex.GetType().Name);
                _logger.LogError("Komunikat: {Message}", ex.Message);
                if (ex.InnerException != null)
                {
                    _logger.LogError("Inner Exception: {InnerMessage}", ex.InnerException.Message);
                    _logger.LogError("Inner Exception Type: {InnerType}", ex.InnerException.GetType().Name);
                }
                _logger.LogError("Stack Trace: {StackTrace}", ex.StackTrace);

                lblConnectionStatus.Text = "✗ Błąd połączenia";
                lblConnectionStatus.ForeColor = Color.Red;

                string errorMsg = ex.Message;
                if (ex.InnerException != null)
                    errorMsg += $"\n\nSzczegóły: {ex.InnerException.Message}";

                var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                MessageBox.Show(
                    $"Nie udało się połączyć z bazą danych DB2:\n\n{errorMsg}\n\n" +
                    $"Sprawdź:\n" +
                    $"• Poprawność danych połączenia (Database, Hostname, Port)\n" +
                    $"• Czy baza danych jest dostępna\n" +
                    $"• Czy sterownik IBM DB2 Client jest zainstalowany\n" +
                    $"• Credentials (User/Password lub Credential Manager)\n\n" +
                    $"Pełne logi w: {logPath}",
                    "Test połączenia - Błąd",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                btnTestConnection.Enabled = true;
                _logger.LogInformation("========================================");
            }
        }

        private async void BtnFetchVehicles_Click(object? sender, EventArgs e)
        {
            try
            {
                btnFetchVehicles.Enabled = false;
                lblFetchStatus.Text = "Pobieranie pojazdów z bazy danych...";
                lblFetchStatus.ForeColor = Color.Blue;

                int? nbFrom = numFetchNbFrom.Value > 0 ? (int)numFetchNbFrom.Value : null;
                int? nbTo = numFetchNbTo.Value > 0 ? (int)numFetchNbTo.Value : null;
                bool? activeOnly = chkFetchActiveOnly.Checked ? true : null;

                var db2Service = _serviceProvider.GetRequiredService<DB2ExportService.Services.IDB2Service>();
                var vehicles = await db2Service.GetVehiclesAsync(nbFrom, nbTo, activeOnly);

                if (vehicles.Count == 0)
                {
                    lblFetchStatus.Text = "Nie znaleziono pojazdów spełniających kryteria";
                    lblFetchStatus.ForeColor = Color.Orange;
                    MessageBox.Show("Nie znaleziono pojazdów spełniających podane kryteria.",
                        "Brak wyników", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Wypełnij pole tekstowe listą pojazdów
                txtPojazdyLista.Text = string.Join(", ", vehicles.Select(v => v.NB));
                cmbPojazdyMode.SelectedItem = "lista";

                lblFetchStatus.Text = $"✓ Pobrano {vehicles.Count} pojazdów";
                lblFetchStatus.ForeColor = Color.Green;

                MessageBox.Show(
                    $"Pobrano {vehicles.Count} pojazdów z bazy danych.\n\n" +
                    $"Lista została automatycznie wypełniona w polu 'Lista pojazdów'.\n" +
                    $"Pamiętaj o zapisaniu konfiguracji!",
                    "Sukces",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                lblFetchStatus.Text = "✗ Błąd podczas pobierania";
                lblFetchStatus.ForeColor = Color.Red;

                MessageBox.Show(
                    $"Błąd podczas pobierania pojazdów z bazy danych:\n\n{ex.Message}\n\n" +
                    $"Sprawdź:\n" +
                    $"• Konfigurację połączenia DB2\n" +
                    $"• Dostępność bazy danych\n" +
                    $"• Uprawnienia użytkownika",
                    "Błąd",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                btnFetchVehicles.Enabled = true;
            }
        }
    }

    // Configuration models - używamy klas z głównego projektu DB2ExportService.Models
    public class ExportSettings
    {
        public ExportConfig? ExportConfig { get; set; }
        public VehicleConfig? VehicleConfig { get; set; }
        public DB2Config? DB2 { get; set; }
    }
}
