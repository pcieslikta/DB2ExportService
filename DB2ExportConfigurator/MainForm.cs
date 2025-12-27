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
                var vehicleList = string.Join(", ", _settings.VehicleConfig.PojazdyLista);
                txtPojazdyLista.Text = vehicleList;  // Hidden field for backward compat
                txtVehicleInput.Text = vehicleList;   // New unified input (visible)
            }

            // Don't call UpdateVehicleModeControls() - old controls are hidden
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
                    PojazdyMode = "lista",  // Always use "lista" mode now (unified interface)
                    PojazdyStart = null,    // Deprecated - not used anymore
                    PojazdyEnd = null,      // Deprecated - not used anymore
                    PojazdyLista = ParseVehicleList(txtPojazdyLista.Text)  // Uses hidden field with selected vehicles
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

        /// <summary>
        /// Parsuje mieszane wejście: zakresy i pojedyncze numery
        /// Format: "100-120, 789, 900-905"
        /// Zwraca: (List<int> numbers, List<string> errors)
        /// </summary>
        private (List<int> numbers, List<string> errors) ParseVehicleInput(string text)
        {
            var result = new List<int>();
            var errors = new List<string>();
            var seen = new HashSet<int>();

            if (string.IsNullOrWhiteSpace(text))
                return (result, errors);

            var parts = text.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                var trimmed = part.Trim();

                // Obsługa zakresu (np. "100-120")
                if (trimmed.Contains("-"))
                {
                    var rangeParts = trimmed.Split('-');
                    if (rangeParts.Length != 2)
                    {
                        errors.Add($"Nieprawidłowy zakres: '{trimmed}'");
                        continue;
                    }

                    if (!int.TryParse(rangeParts[0].Trim(), out int start) ||
                        !int.TryParse(rangeParts[1].Trim(), out int end))
                    {
                        errors.Add($"Nieprawidłowe wartości w zakresie: '{trimmed}'");
                        continue;
                    }

                    if (start > end)
                    {
                        errors.Add($"Zakres odwrotny: '{trimmed}'");
                        continue;
                    }

                    if (end - start > 1000)
                    {
                        errors.Add($"Zakres zbyt duży: '{trimmed}' (max 1000)");
                        continue;
                    }

                    // Rozwiń zakres
                    for (int i = start; i <= end; i++)
                    {
                        if (seen.Add(i))
                            result.Add(i);
                    }
                }
                else
                {
                    // Pojedynczy numer
                    if (int.TryParse(trimmed, out int num))
                    {
                        if (seen.Add(num))
                            result.Add(num);
                    }
                    else
                    {
                        errors.Add($"Nieprawidłowy numer: '{trimmed}'");
                    }
                }
            }

            result.Sort();
            return (result, errors);
        }

        // Backward compatibility wrapper
        private List<int> ParseVehicleList(string text)
        {
            var (numbers, _) = ParseVehicleInput(text);
            return numbers;
        }

        // DEPRECATED: No longer needed with unified vehicle input interface
        // Old mode controls (cmbPojazdyMode, numPojazdyStart, numPojazdyEnd) are now hidden
        // Kept for backward compatibility only
        [Obsolete("This method is no longer used. Old mode controls are hidden.")]
        private void UpdateVehicleModeControls()
        {
            // Method body kept for backward compatibility but not called
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

        /// <summary>
        /// Handler dla przycisku "Pobierz pojazdy" - parsuje input i pobiera z bazy
        /// </summary>
        private async void BtnParseAndFetch_Click(object? sender, EventArgs e)
        {
            try
            {
                btnParseAndFetch.Enabled = false;
                lblParseStatus.ForeColor = Color.Blue;
                lblParseStatus.Text = "Parsowanie...";

                var (numbers, errors) = ParseVehicleInput(txtVehicleInput.Text);

                if (errors.Any())
                {
                    lblParseStatus.ForeColor = Color.Red;
                    lblParseStatus.Text = $"Błędy: {errors.Count}";
                    MessageBox.Show(
                        $"Błędy parsowania:\n\n{string.Join("\n", errors)}",
                        "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!numbers.Any())
                {
                    lblParseStatus.ForeColor = Color.Orange;
                    lblParseStatus.Text = "Brak numerów do pobrania";
                    return;
                }

                lblParseStatus.Text = $"Pobieranie {numbers.Count} pojazdów z bazy...";

                // Pobierz z DB2
                var db2Service = _serviceProvider.GetRequiredService<DB2ExportService.Services.IDB2Service>();
                int minNb = numbers.Min();
                int maxNb = numbers.Max();

                var allVehicles = await db2Service.GetVehiclesAsync(minNb, maxNb, null);
                var matchedVehicles = allVehicles.Where(v => numbers.Contains(v.NB)).ToList();

                if (!matchedVehicles.Any())
                {
                    lblParseStatus.ForeColor = Color.Orange;
                    lblParseStatus.Text = "Nie znaleziono pojazdów w bazie";
                    MessageBox.Show(
                        "Żaden z podanych numerów pojazdów nie został znaleziony w bazie danych.",
                        "Brak wyników",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                // Wypełnij DataGridView
                PopulateVehicleGrid(matchedVehicles);

                var notFound = numbers.Except(matchedVehicles.Select(v => v.NB)).ToList();
                lblParseStatus.ForeColor = Color.Green;
                lblParseStatus.Text = $"Znaleziono {matchedVehicles.Count}/{numbers.Count} pojazdów" +
                    (notFound.Any() ? $" (brak: {string.Join(", ", notFound.Take(5))}{(notFound.Count > 5 ? "..." : "")})" : "");
            }
            catch (Exception ex)
            {
                lblParseStatus.ForeColor = Color.Red;
                lblParseStatus.Text = "Błąd pobierania";
                _logger.LogError(ex, "Błąd podczas pobierania pojazdów");
                MessageBox.Show($"Błąd:\n{ex.Message}", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnParseAndFetch.Enabled = true;
            }
        }

        /// <summary>
        /// Wypełnia DataGridView pojazdami i auto-zaznacza te z MA_BRAMKI = 'Y' lub '1'
        /// </summary>
        private void PopulateVehicleGrid(List<VehicleInfo> vehicles)
        {
            dgvVehicles.Rows.Clear();

            foreach (var vehicle in vehicles.OrderBy(v => v.NB))
            {
                // Auto-zaznacz jeśli MA_BRAMKI = 'Y' lub '1'
                bool autoSelect = vehicle.MaBramki == "Y" || vehicle.MaBramki == "1";

                var rowIndex = dgvVehicles.Rows.Add(
                    autoSelect,              // Checkbox zaznaczony
                    vehicle.NB,
                    vehicle.NR,
                    vehicle.TypPoj,
                    vehicle.Zajezdnia,
                    vehicle.MaBramki,
                    vehicle.WGotowosci
                );

                // Podświetl auto-zaznaczone
                if (autoSelect)
                {
                    dgvVehicles.Rows[rowIndex].DefaultCellStyle.BackColor = Color.LightGreen;
                }
            }

            _logger.LogInformation(
                "Wypełniono grid: {Count} pojazdów, {AutoSelected} z bramkami",
                vehicles.Count,
                vehicles.Count(v => v.MaBramki == "Y" || v.MaBramki == "1"));
        }

        /// <summary>
        /// Handler dla przycisku "Zastosuj wybór" - zapisuje zaznaczone pojazdy
        /// </summary>
        private void BtnApplySelection_Click(object? sender, EventArgs e)
        {
            var selectedVehicles = new List<int>();

            foreach (DataGridViewRow row in dgvVehicles.Rows)
            {
                if (row.Cells["Selected"].Value is bool isSelected && isSelected)
                {
                    if (row.Cells["NB"].Value is int nb)
                        selectedVehicles.Add(nb);
                }
            }

            if (!selectedVehicles.Any())
            {
                MessageBox.Show("Nie wybrano żadnych pojazdów!",
                    "Uwaga", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Zapisz do ukrytego pola (backward compat)
            txtPojazdyLista.Text = string.Join(", ", selectedVehicles.OrderBy(v => v));

            MessageBox.Show(
                $"Wybrano {selectedVehicles.Count} pojazdów.\n\n" +
                $"Numery: {string.Join(", ", selectedVehicles.OrderBy(v => v).Take(10))}" +
                (selectedVehicles.Count > 10 ? "..." : "") + "\n\n" +
                "Pamiętaj o zapisaniu konfiguracji!",
                "Sukces", MessageBoxButtons.OK, MessageBoxIcon.Information);

            _logger.LogInformation("Zastosowano wybór pojazdów: {Vehicles}", string.Join(", ", selectedVehicles));
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

                // Wypełnij nowy unified input i ukryty txtPojazdyLista
                var vehicleList = string.Join(", ", vehicles.Select(v => v.NB));
                txtVehicleInput.Text = vehicleList;  // New unified input (visible)
                txtPojazdyLista.Text = vehicleList;  // Hidden field (backward compat)

                lblFetchStatus.Text = $"✓ Pobrano {vehicles.Count} pojazdów";
                lblFetchStatus.ForeColor = Color.Green;

                MessageBox.Show(
                    $"Pobrano {vehicles.Count} pojazdów z bazy danych.\n\n" +
                    $"Lista została automatycznie wypełniona.\n" +
                    $"Kliknij 'Pobierz pojazdy' aby załadować szczegóły do tabeli.\n\n" +
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
