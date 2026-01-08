using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text.Json;
using System.Text.Json.Serialization;
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

        // Vehicle auto-load and caching
        private bool _vehiclesPanelFirstLoad = true;
        private List<VehicleInfo> _cachedVehicles = new List<VehicleInfo>();
        private DateTime? _cacheTimestamp = null;
        private const int CACHE_EXPIRY_MINUTES = 5;

        // Virtual mode data store
        private List<VehicleInfo> _displayedVehicles = new List<VehicleInfo>();
        private HashSet<int> _selectedVehicleNBs = new HashSet<int>();
        private VehicleSelectionSynchronizer _selectionSynchronizer = new();

        public MainForm(IServiceProvider serviceProvider, ILogger<MainForm> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;

            _logger.LogInformation("Inicjalizacja MainForm");

            // Automatyczne wykrywanie ścieżki konfiguracji (jak w Program.cs serwisu)
            var configPaths = new[]
            {
                @"C:\config\appsettings.json",
                Path.Combine(AppContext.BaseDirectory, "appsettings.json"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "DB2Export", "appsettings.json"),
                @"C:\Services\DB2Export\appsettings.json"
            };

            _configPath = string.Empty;
            foreach (var path in configPaths)
            {
                if (File.Exists(path))
                {
                    _configPath = path;
                    _logger.LogInformation("Znaleziono konfigurację: {ConfigPath}", path);
                    break;
                }
            }

            // Jeśli nie znaleziono, użyj domyślnej lokalizacji (CommonApplicationData)
            if (string.IsNullOrEmpty(_configPath))
            {
                _configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    "DB2Export", "appsettings.json");
                _logger.LogWarning("Nie znaleziono konfiguracji. Użyję domyślnej: {ConfigPath}", _configPath);
            }

            InitializeComponent();
            InitializeSidebar();
            InitializeTheme();
            LoadSettings();
            CheckAdminRights();
            UpdateServiceStatus();

            // Auto-load vehicles from config when form is shown
            this.Shown += MainForm_Shown;
        }

        private async void MainForm_Shown(object? sender, EventArgs e)
        {
            _logger.LogInformation("===== MainForm_Shown START =====");
            _logger.LogInformation("txtVehicleInput is null: {IsNull}", txtVehicleInput == null);
            _logger.LogInformation("txtVehicleInput.Text: '{Text}'", txtVehicleInput?.Text ?? "(null)");
            _logger.LogInformation("VehicleConfig is null: {IsNull}", _settings.VehicleConfig == null);
            _logger.LogInformation("PojazdyLista count: {Count}", _settings.VehicleConfig?.PojazdyLista?.Count ?? 0);

            // Auto-fetch vehicles if config has vehicle numbers and field is populated
            if (!string.IsNullOrWhiteSpace(txtVehicleInput?.Text) &&
                _settings.VehicleConfig?.PojazdyLista != null &&
                _settings.VehicleConfig.PojazdyLista.Any())
            {
                _logger.LogInformation("✅ Auto-loading vehicles from config on startup...");
                // Wait a moment for UI to settle
                await Task.Delay(500);
                // Trigger the fetch
                BtnParseAndFetch_Click(null, EventArgs.Empty);
            }
            else
            {
                _logger.LogWarning("❌ Auto-load skipped. txtVehicleInput empty or no config vehicles.");
            }
            _logger.LogInformation("===== MainForm_Shown END =====");
        }

        private void ChkSelectAll_CheckedChanged(object? sender, EventArgs e)
        {
            if (dgvVehicles != null && dgvVehicles.Rows.Count > 0)
            {
                bool isChecked = chkSelectAll.Checked;

                // Update the hash set
                if (isChecked)
                {
                    foreach (var vehicle in _displayedVehicles)
                        _selectedVehicleNBs.Add(vehicle.NB);
                }
                else
                {
                    _selectedVehicleNBs.Clear();
                }

                // Actually check/uncheck all checkboxes in DataGridView
                foreach (DataGridViewRow row in dgvVehicles.Rows)
                {
                    row.Cells["Selected"].Value = isChecked;
                }

                // Refresh to update colors
                dgvVehicles.Refresh();

                _logger.LogInformation("Select All: {Status}, {Count} vehicles",
                    isChecked ? "Checked" : "Unchecked", _selectedVehicleNBs.Count);
            }
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

                // Refresh vehicle grid colors when theme changes
                if (dgvVehicles != null && _displayedVehicles.Count > 0)
                {
                    dgvVehicles.Refresh();
                    dgvVehicles.Invalidate();
                    _logger.LogInformation("Vehicle grid refreshed for {Theme} mode", ThemeManager.CurrentTheme);
                }
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
                _logger.LogInformation("===== LoadSettings START =====");
                _logger.LogInformation("Config path: {Path}", _configPath);
                _logger.LogInformation("File exists: {Exists}", File.Exists(_configPath));

                if (File.Exists(_configPath))
                {
                    var json = File.ReadAllText(_configPath);
                    _logger.LogInformation("JSON length: {Length} chars", json.Length);
                    _logger.LogInformation("JSON first 200 chars: {Json}", json.Substring(0, Math.Min(200, json.Length)));

                    _settings = JsonSerializer.Deserialize<ExportSettings>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        ReadCommentHandling = JsonCommentHandling.Skip,
                        Converters = { new JsonStringEnumConverter() }
                    }) ?? new ExportSettings();

                    _logger.LogInformation("Deserialization completed");
                    _logger.LogInformation("_settings is null: {IsNull}", _settings == null);
                    _logger.LogInformation("_settings.VehicleConfig is null: {IsNull}", _settings?.VehicleConfig == null);
                    _logger.LogInformation("_settings.ExportConfig is null: {IsNull}", _settings?.ExportConfig == null);
                    _logger.LogInformation("_settings.DB2 is null: {IsNull}", _settings?.DB2 == null);

                    PopulateForm();
                    _logger.LogInformation("PopulateForm completed");
                }
                else
                {
                    _logger.LogWarning("Config file not found: {Path}", _configPath);
                    _settings = new ExportSettings();
                    MessageBox.Show(
                        $"Nie znaleziono pliku konfiguracji:\n{_configPath}\n\nZostanie utworzona nowa konfiguracja.",
                        "Konfiguracja",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }
                _logger.LogInformation("===== LoadSettings END =====");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in LoadSettings");

                // IMPORTANT: Don't reset _settings if it was already loaded!
                // The exception might be from PopulateForm, not deserialization
                if (_settings == null || _settings.VehicleConfig == null)
                {
                    _logger.LogWarning("Settings failed to load, creating new empty settings");
                    _settings = new ExportSettings();
                    MessageBox.Show($"Błąd wczytywania konfiguracji:\n{ex.Message}", "Błąd",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    _logger.LogWarning("Exception during PopulateForm, but settings were loaded successfully. Continuing with loaded settings.");
                    MessageBox.Show($"Ostrzeżenie: Niektóre pola nie mogły być wypełnione:\n{ex.Message}\n\nKonfiguracja została wczytana, ale niektóre pola mogą być puste.",
                        "Ostrzeżenie",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
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

            // Periodic Monitoring & Triggers
            chkEnablePeriodicMonitoring.Checked = _settings.ExportConfig?.EnablePeriodicMonitoring ?? false;
            numMonitoringIntervalMinutes.Value = _settings.ExportConfig?.MonitoringIntervalMinutes ?? 15;
            numMonitoringDaysBack.Value = _settings.ExportConfig?.MonitoringDaysBack ?? 7;
            txtTriggerFolderPath.Text = _settings.ExportConfig?.TriggerFolderPath ?? @"C:\Services\DB2Export\Triggers";

            // Enabled Export Types
            var enabledTypes = _settings.ExportConfig?.EnabledExportTypes ?? new List<ExportType>();
            chkExportTypeBramkiBasic.Checked = enabledTypes.Contains(ExportType.BramkiBasic);
            chkExportTypeBramkiDetail.Checked = enabledTypes.Contains(ExportType.BramkiDetail);
            chkExportTypePunktualnosc.Checked = enabledTypes.Contains(ExportType.Punktualnosc);

#pragma warning disable CS0618 // Using obsolete members for backward compatibility
            // Vehicle Configuration
            cmbPojazdyMode.SelectedItem = _settings.VehicleConfig?.PojazdyMode ?? "lista";

            // Safely set numeric values - clamp to control's Min/Max range
            var pojazdyStart = _settings.VehicleConfig?.PojazdyStart;
            var pojazdyEnd = _settings.VehicleConfig?.PojazdyEnd;

            if (pojazdyStart.HasValue)
            {
                var clampedStart = Math.Max(numPojazdyStart.Minimum, Math.Min(numPojazdyStart.Maximum, pojazdyStart.Value));
                numPojazdyStart.Value = clampedStart;
                if (pojazdyStart.Value > numPojazdyStart.Maximum || pojazdyStart.Value < numPojazdyStart.Minimum)
                {
                    _logger.LogWarning("PojazdyStart value {Value} out of range [{Min}-{Max}], clamped to {Clamped}",
                        pojazdyStart.Value, numPojazdyStart.Minimum, numPojazdyStart.Maximum, clampedStart);
                }
            }

            if (pojazdyEnd.HasValue)
            {
                var clampedEnd = Math.Max(numPojazdyEnd.Minimum, Math.Min(numPojazdyEnd.Maximum, pojazdyEnd.Value));
                numPojazdyEnd.Value = clampedEnd;
                if (pojazdyEnd.Value > numPojazdyEnd.Maximum || pojazdyEnd.Value < numPojazdyEnd.Minimum)
                {
                    _logger.LogWarning("PojazdyEnd value {Value} out of range [{Min}-{Max}], clamped to {Clamped}",
                        pojazdyEnd.Value, numPojazdyEnd.Minimum, numPojazdyEnd.Maximum, clampedEnd);
                }
            }
#pragma warning restore CS0618

            _logger.LogInformation("===== VEHICLE CONFIG LOAD =====");
            _logger.LogInformation("VehicleConfig is null: {IsNull}", _settings.VehicleConfig == null);
            if (_settings.VehicleConfig != null)
            {
                _logger.LogInformation("PojazdyLista is null: {IsNull}", _settings.VehicleConfig.PojazdyLista == null);
                _logger.LogInformation("PojazdyLista count: {Count}", _settings.VehicleConfig.PojazdyLista?.Count ?? 0);
            }

            if (_settings.VehicleConfig?.PojazdyLista != null && _settings.VehicleConfig.PojazdyLista.Any())
            {
                var vehicleList = string.Join(", ", _settings.VehicleConfig.PojazdyLista);
                _logger.LogInformation("✅ Loading {Count} vehicles from config: {List}",
                    _settings.VehicleConfig.PojazdyLista.Count, vehicleList);

                if (txtPojazdyLista != null)
                {
                    txtPojazdyLista.Text = vehicleList;  // Hidden field for backward compat
                    _logger.LogInformation("✅ Set txtPojazdyLista.Text");
                }
                else
                {
                    _logger.LogWarning("❌ txtPojazdyLista is NULL!");
                }

                if (txtVehicleInput != null)
                {
                    txtVehicleInput.Text = vehicleList;   // New unified input (visible)
                    _logger.LogInformation("✅ Set txtVehicleInput.Text = '{Text}'", vehicleList);
                }
                else
                {
                    _logger.LogWarning("❌ txtVehicleInput is NULL! Controls not initialized yet.");
                }
            }
            else
            {
                _logger.LogWarning("❌ No vehicles found in VehicleConfig.PojazdyLista - txtVehicleInput will be empty");
            }
            _logger.LogInformation("===== VEHICLE CONFIG LOAD END =====");

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
                    NotificationEmail = string.IsNullOrWhiteSpace(txtNotificationEmail.Text) ? null : txtNotificationEmail.Text,

                    // Periodic Monitoring & Triggers
                    EnablePeriodicMonitoring = chkEnablePeriodicMonitoring.Checked,
                    MonitoringIntervalMinutes = (int)numMonitoringIntervalMinutes.Value,
                    MonitoringDaysBack = (int)numMonitoringDaysBack.Value,
                    TriggerFolderPath = string.IsNullOrWhiteSpace(txtTriggerFolderPath.Text)
                        ? @"C:\Services\DB2Export\Triggers"
                        : txtTriggerFolderPath.Text,

                    // Enabled Export Types
                    EnabledExportTypes = new List<ExportType>()
                };

                // Dodaj wybrane typy eksportu
                if (chkExportTypeBramkiBasic.Checked)
                    _settings.ExportConfig.EnabledExportTypes.Add(ExportType.BramkiBasic);
                if (chkExportTypeBramkiDetail.Checked)
                    _settings.ExportConfig.EnabledExportTypes.Add(ExportType.BramkiDetail);
                if (chkExportTypePunktualnosc.Checked)
                    _settings.ExportConfig.EnabledExportTypes.Add(ExportType.Punktualnosc);

                // Walidacja: ostrzeżenie jeśli brak typów eksportu
                if (_settings.ExportConfig.EnabledExportTypes.Count == 0)
                {
                    var result = MessageBox.Show(
                        "Nie wybrano żadnego typu eksportu!\n\n" +
                        "Serwis nie będzie wykonywał żadnych eksportów.\n" +
                        "Czy na pewno chcesz zapisać?",
                        "Ostrzeżenie",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (result == DialogResult.No)
                        return;
                }

#pragma warning disable CS0618 // Using obsolete members for backward compatibility
                _settings.VehicleConfig = new VehicleConfig
                {
                    KodExportu = txtKodExportu.Text,
                    PojazdyMode = "lista",  // Always use "lista" mode now (unified interface)
                    PojazdyStart = null,    // Deprecated - not used anymore
                    PojazdyEnd = null,      // Deprecated - not used anymore
                    PojazdyLista = ParseVehicleList(txtPojazdyLista.Text)  // Uses hidden field with selected vehicles
                };
#pragma warning restore CS0618

                var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    Converters = { new JsonStringEnumConverter() }
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

        /// <summary>
        /// Gets config numbers from txtVehicleInput with fallback to _settings
        /// </summary>
        private List<int> GetConfigNumbersWithFallback()
        {
            var inputText = txtVehicleInput?.Text ?? "";
            _logger.LogInformation("GetConfigNumbersWithFallback: txtVehicleInput.Text = '{Text}'", inputText);

            var configNumbers = ParseVehicleInput(inputText).numbers;
            _logger.LogInformation("GetConfigNumbersWithFallback: ParseVehicleInput returned {Count} numbers", configNumbers.Count);

            // Debug _settings.VehicleConfig
            _logger.LogInformation("GetConfigNumbersWithFallback: _settings.VehicleConfig is null? {IsNull}", _settings.VehicleConfig == null);
            if (_settings.VehicleConfig != null)
            {
                _logger.LogInformation("GetConfigNumbersWithFallback: _settings.VehicleConfig.PojazdyLista is null? {IsNull}", _settings.VehicleConfig.PojazdyLista == null);
                if (_settings.VehicleConfig.PojazdyLista != null)
                {
                    _logger.LogInformation("GetConfigNumbersWithFallback: _settings.VehicleConfig.PojazdyLista.Count = {Count}", _settings.VehicleConfig.PojazdyLista.Count);
                }
            }

            // Fallback: use config directly if txtVehicleInput is empty
            if (!configNumbers.Any() && _settings.VehicleConfig?.PojazdyLista != null && _settings.VehicleConfig.PojazdyLista.Any())
            {
                configNumbers = _settings.VehicleConfig.PojazdyLista;
                _logger.LogInformation("GetConfigNumbersWithFallback: ✅ Using fallback config from _settings: {Count} vehicles", configNumbers.Count);
            }
            else
            {
                _logger.LogInformation("GetConfigNumbersWithFallback: ❌ Fallback NOT used. Returning {Count} vehicles", configNumbers.Count);
            }

            return configNumbers;
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
                // Check if controls are initialized
                if (lblParseStatus == null || btnParseAndFetch == null || txtVehicleInput == null)
                {
                    _logger.LogWarning("Parse and fetch skipped: controls not initialized yet");
                    return;
                }

                InvalidateVehicleCache(); // Clear cache before manual fetch

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

                // Cache results (important for filters!)
                _cachedVehicles = matchedVehicles;
                _cacheTimestamp = DateTime.Now;

                // Wypełnij DataGridView with config merge
                var configNumbers = GetConfigNumbersWithFallback();
                PopulateVehicleGridWithConfigMerge(matchedVehicles, configNumbers);

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
        /// Wypełnia DataGridView pojazdami (Virtual Mode)
        /// Auto-zaznacza pojazdy z MA_BRAMKI = 'Y' lub '1'
        /// </summary>
        private void PopulateVehicleGrid(List<VehicleInfo> vehicles)
        {
            try
            {
                _logger.LogInformation("PopulateVehicleGrid START with {Count} vehicles", vehicles?.Count ?? 0);

                _displayedVehicles = vehicles ?? new List<VehicleInfo>();
                _logger.LogInformation("_displayedVehicles assigned");

                // Update vehicle count label
                if (lblVehicleCount != null)
                {
                    lblVehicleCount.Text = $"Pojazdy: {_displayedVehicles.Count}";
                    _logger.LogInformation("lblVehicleCount updated");
                }

                // Clear previous selections before auto-selecting
                _selectedVehicleNBs.Clear();
                _logger.LogInformation("_selectedVehicleNBs cleared");

                // Populate filter dropdowns if this is a fresh load
                if (_cachedVehicles.Count > 0)
                {
                    _logger.LogInformation("Populating filter dropdowns");
                    PopulateFilterDropdowns(_cachedVehicles);
                    _logger.LogInformation("Filter dropdowns populated");
                }

                // Auto-select vehicles with gates (MA_BRAMKI = 'Y' or '1')
                _logger.LogInformation("Starting auto-selection loop");
                foreach (var vehicle in _displayedVehicles)
                {
                    if (vehicle.MaBramki == "Y" || vehicle.MaBramki == "1")
                    {
                        _selectedVehicleNBs.Add(vehicle.NB);
                    }
                }
                _logger.LogInformation("Auto-selection completed: {Selected} vehicles", _selectedVehicleNBs.Count);

                // Set row count for virtual mode
                if (dgvVehicles != null)
                {
                    _logger.LogInformation("Setting dgvVehicles.GetItemCount() to {Count}", _displayedVehicles.Count);
                // lblVehicleCount.Text = $"Pojazdy: {dgvVehicles.GetItemCount()}"; // Commented - API issue
                    _logger.LogInformation("Calling dgvVehicles.Refresh()");
                    dgvVehicles.Refresh();
                    _logger.LogInformation("dgvVehicles.Refresh() completed");
                }

                _logger.LogInformation(
                    "PopulateVehicleGrid COMPLETED: {Count} vehicles displayed, {AutoSelected} with gates",
                    _displayedVehicles.Count,
                    _selectedVehicleNBs.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CRITICAL ERROR in PopulateVehicleGrid");
                throw; // Re-throw to be caught by calling method
            }
        }

        /// <summary>
        /// Gets config numbers with fallback to _settings if txtVehicleInput is empty
        /// </summary>
        private List<int> GetConfigNumbersWithFallback(string? inputText)
        {
            var configNumbers = ParseVehicleInput(inputText ?? "").numbers;

            // Fallback: use config directly if input is empty
            if (!configNumbers.Any() && _settings.VehicleConfig?.PojazdyLista != null && _settings.VehicleConfig.PojazdyLista.Any())
            {
                configNumbers = _settings.VehicleConfig.PojazdyLista;
                _logger.LogInformation("✅ Using fallback config from _settings: {Count} vehicles", configNumbers.Count);
            }

            return configNumbers;
        }

        /// <summary>
        /// Wypełnia DataGridView pojazdami z auto-zaznaczeniem z config (Virtual Mode)
        /// Auto-zaznacza pojazdy które są w configNumbers (z txtVehicleInput)
        /// </summary>
        private void PopulateVehicleGridWithConfigMerge(List<VehicleInfo> vehicles, List<int> configNumbers)
        {
            try
            {
                _logger.LogInformation("PopulateVehicleGridWithConfigMerge START with {Count} vehicles, {ConfigCount} config numbers",
                    vehicles?.Count ?? 0, configNumbers?.Count ?? 0);

                _displayedVehicles = vehicles ?? new List<VehicleInfo>();
                _logger.LogInformation("_displayedVehicles assigned");

                // Update vehicle count label
                if (lblVehicleCount != null)
                {
                    lblVehicleCount.Text = $"Pojazdy: {_displayedVehicles.Count}";
                    _logger.LogInformation("lblVehicleCount updated");
                }

                // Clear previous selections before auto-selecting
                _selectedVehicleNBs.Clear();
                _logger.LogInformation("_selectedVehicleNBs cleared");

                // Populate filter dropdowns if this is a fresh load
                if (_cachedVehicles.Count > 0)
                {
                    _logger.LogInformation("Populating filter dropdowns");
                    PopulateFilterDropdowns(_cachedVehicles);
                    _logger.LogInformation("Filter dropdowns populated");
                }

                // AUTO-SELECT vehicles from config (configNumbers)
                _logger.LogInformation("Starting auto-selection from config");
                if (configNumbers != null && configNumbers.Any())
                {
                    foreach (var vehicle in _displayedVehicles)
                    {
                        if (configNumbers.Contains(vehicle.NB))
                        {
                            _selectedVehicleNBs.Add(vehicle.NB);
                        }
                    }
                }
                _logger.LogInformation("Auto-selection from config completed: {Selected} vehicles", _selectedVehicleNBs.Count);

                // Update synchronizer
                _selectionSynchronizer.ConfigNumbers = configNumbers;
                _selectionSynchronizer.DatabaseVehicles = _displayedVehicles;
                _selectionSynchronizer.SelectedNBs = _selectedVehicleNBs;

                // Populate DataGridView
                if (dgvVehicles != null)
                {
                    _logger.LogInformation("Populating dgvVehicles with {Count} vehicles", _displayedVehicles.Count);

                    dgvVehicles.Rows.Clear();
                    foreach (var vehicle in _displayedVehicles)
                    {
                        int index = dgvVehicles.Rows.Add();
                        var row = dgvVehicles.Rows[index];

                        row.Cells["Selected"].Value = _selectedVehicleNBs.Contains(vehicle.NB);
                        row.Cells["NB"].Value = vehicle.NB;
                        row.Cells["NR"].Value = vehicle.NR;
                        row.Cells["TYP_POJ"].Value = vehicle.TypPoj;
                        row.Cells["ZAJEZDNIA"].Value = vehicle.Zajezdnia;
                        row.Cells["MA_BRAMKI"].Value = VehicleIconRenderer.GetGatesIcon(vehicle.MaBramki);
                        row.Cells["WGOTOWOSCI"].Value = VehicleIconRenderer.GetActiveIcon(vehicle.WGotowosci);
                    }

                    _logger.LogInformation("dgvVehicles populated and checkboxes set");
                }

                // Show warnings if config numbers not found in DB
                var notFound = _selectionSynchronizer.GetNotFoundInDatabase();
                if (notFound.Any() && lblParseStatus != null)
                {
                    lblParseStatus.Text = $"⚠️ {notFound.Count} numerów z config nie znaleziono w bazie: {string.Join(", ", notFound.Take(5))}{(notFound.Count > 5 ? "..." : "")}";
                    lblParseStatus.ForeColor = Color.Red;
                }

                _logger.LogInformation(
                    "PopulateVehicleGridWithConfigMerge COMPLETED: {Count} vehicles displayed, {AutoSelected} from config",
                    _displayedVehicles.Count,
                    _selectedVehicleNBs.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CRITICAL ERROR in PopulateVehicleGridWithConfigMerge");
                throw; // Re-throw to be caught by calling method
            }
        }

        /// <summary>
        /// Handler dla przycisku "Zastosuj wybór" - zapisuje zaznaczone pojazdy (Virtual Mode)
        /// Shows dialog with changes if selection differs from config
        /// </summary>
        private void BtnApplySelection_Click(object? sender, EventArgs e)
        {
            if (!_selectedVehicleNBs.Any())
            {
                MessageBox.Show("Nie wybrano żadnych pojazdów!",
                    "Uwaga", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var selectedList = _selectedVehicleNBs.OrderBy(nb => nb).ToList();

            // Get changes
            var (added, removed) = _selectionSynchronizer.GetChanges();

            // Show confirmation if selection differs from config
            if (added.Any() || removed.Any())
            {
                int configCount = _selectionSynchronizer.ConfigNumbers.Count;
                int selectedCount = selectedList.Count;

                string message = $"Wybór różni się od konfiguracji:\n\n" +
                                $"Config: {configCount} → Wybrano: {selectedCount}\n\n";

                if (added.Any())
                {
                    message += $"➕ Dodano ({added.Count}): {string.Join(", ", added.Take(10))}" +
                              (added.Count > 10 ? "..." : "") + "\n\n";
                }

                if (removed.Any())
                {
                    message += $"➖ Usunięto ({removed.Count}): {string.Join(", ", removed.Take(10))}" +
                              (removed.Count > 10 ? "..." : "") + "\n\n";
                }

                message += "Czy chcesz zastosować te zmiany?";

                var result = MessageBox.Show(message, "Potwierdzenie zmian",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.No)
                    return;
            }

            // Update hidden field (backward compat)
            txtPojazdyLista.Text = string.Join(", ", selectedList);

            // Update synchronizer
            _selectionSynchronizer.ConfigNumbers = selectedList;
            _selectionSynchronizer.SelectedNBs = _selectedVehicleNBs;

            MessageBox.Show(
                $"Wybrano {selectedList.Count} pojazdów.\n\n" +
                $"Numery: {string.Join(", ", selectedList.Take(10))}" +
                (selectedList.Count > 10 ? "..." : "") + "\n\n" +
                "Pamiętaj o zapisaniu konfiguracji!",
                "Sukces", MessageBoxButtons.OK, MessageBoxIcon.Information);

            _logger.LogInformation("Applied vehicle selection: {Vehicles}", string.Join(", ", selectedList));

            // Refresh grid to update colors
            if (dgvVehicles != null)
            {
                dgvVehicles.Refresh();
            }
        }

        /// <summary>
        /// Handler dla przycisku "Załaduj z configu" - wczytuje i zaznacza pojazdy z pliku konfiguracyjnego
        /// </summary>
        private void BtnLoadFromConfig_Click(object? sender, EventArgs e)
        {
            try
            {
                _logger.LogInformation("Loading vehicle selection from config...");

                // Try to get config numbers from synchronizer first, then from settings
                var configNumbers = _selectionSynchronizer.ConfigNumbers;

                // Fallback: Read directly from settings file
                if ((configNumbers == null || !configNumbers.Any()) &&
                    _settings.VehicleConfig?.PojazdyLista != null &&
                    _settings.VehicleConfig.PojazdyLista.Any())
                {
                    configNumbers = _settings.VehicleConfig.PojazdyLista;
                    _logger.LogInformation("Using vehicle numbers from _settings.VehicleConfig.PojazdyLista");
                }

                if (configNumbers == null || !configNumbers.Any())
                {
                    MessageBox.Show("Brak numerów pojazdów w pliku konfiguracyjnym!\n\n" +
                                  "Upewnij się, że pole 'VehicleConfig.PojazdyLista' w appsettings.json zawiera listę pojazdów.\n\n" +
                                  "Przykład w appsettings.json:\n" +
                                  "\"VehicleConfig\": {\n" +
                                  "  \"PojazdyLista\": [100, 101, 102, 500, 600]\n" +
                                  "}",
                        "Brak konfiguracji", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Clear current selections
                _selectedVehicleNBs.Clear();
                _logger.LogInformation("Cleared current selections");

                // Uncheck all items in grid first
                if (dgvVehicles != null)
                {
                    foreach (DataGridViewRow row in dgvVehicles.Rows)
                    {
                        row.Cells["Selected"].Value = false;
                    }
                }

                // Select vehicles from config
                int selectedCount = 0;
                int notFoundCount = 0;
                foreach (var nb in configNumbers)
                {
                    var vehicle = _displayedVehicles.FirstOrDefault(v => v.NB == nb);
                    if (vehicle != null)
                    {
                        _selectedVehicleNBs.Add(nb);
                        selectedCount++;
                    }
                    else
                    {
                        notFoundCount++;
                    }
                }

                // Update synchronizer
                _selectionSynchronizer.SelectedNBs = _selectedVehicleNBs;

                // Check items in grid
                if (dgvVehicles != null)
                {
                    for (int i = 0; i < dgvVehicles.Rows.Count && i < _displayedVehicles.Count; i++)
                    {
                        var vehicle = _displayedVehicles[i];
                        dgvVehicles.Rows[i].Cells["Selected"].Value = _selectedVehicleNBs.Contains(vehicle.NB);
                    }

                    // Refresh grid to update colors
                    dgvVehicles.Refresh();
                }

                // Show success message
                string message = $"✅ Załadowano wybór z konfiguracji!\n\n" +
                               $"Zaznaczono: {selectedCount} pojazdów\n" +
                               $"Z configu: {configNumbers.Count} numerów";

                if (notFoundCount > 0)
                {
                    message += $"\n\n⚠️ Nie znaleziono w bazie: {notFoundCount} numerów";
                }

                MessageBox.Show(message, "Sukces", MessageBoxButtons.OK, MessageBoxIcon.Information);

                _logger.LogInformation("Loaded {Selected} vehicles from config ({NotFound} not found in DB)",
                    selectedCount, notFoundCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading selection from config");
                MessageBox.Show($"Błąd podczas wczytywania z configu:\n{ex.Message}",
                    "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                InvalidateVehicleCache(); // Clear cache before manual fetch

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

                // Cache results (important for filters!)
                _cachedVehicles = vehicles;
                _cacheTimestamp = DateTime.Now;

                // NIE modyfikuj txtVehicleInput - ma zawsze pokazywać config
                // Bezpośrednio wypełnij grid z auto-zaznaczeniem z config
                var configNumbers = GetConfigNumbersWithFallback();
                PopulateVehicleGridWithConfigMerge(vehicles, configNumbers);

                lblFetchStatus.Text = $"✓ Pobrano {vehicles.Count} pojazdów (zaznaczono {_selectedVehicleNBs.Count} z config)";
                lblFetchStatus.ForeColor = Color.Green;

                _logger.LogInformation("Fetched {Total} vehicles, auto-selected {Selected} from config",
                    vehicles.Count, _selectedVehicleNBs.Count);
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

        /// <summary>
        /// Auto-loads vehicles when panel is first shown
        /// Uses cache to avoid repeated DB calls
        /// </summary>
        private async Task AutoLoadVehiclesAsync()
        {
            try
            {
                // Check if controls are initialized
                if (lblFetchStatus == null || btnFetchVehicles == null || dgvVehicles == null)
                {
                    _logger.LogWarning("Auto-load skipped: controls not initialized yet");
                    return;
                }

                // Check cache validity
                bool cacheValid = _cacheTimestamp.HasValue &&
                                 _cachedVehicles.Count > 0 &&
                                 (DateTime.Now - _cacheTimestamp.Value).TotalMinutes < CACHE_EXPIRY_MINUTES;

                if (cacheValid)
                {
                    _logger.LogInformation("Using cached vehicles: {Count} items", _cachedVehicles.Count);
                    PopulateVehicleGrid(_cachedVehicles);
                    lblFetchStatus.Text = $"✓ Wczytano {_cachedVehicles.Count} pojazdów z pamięci podręcznej";
                    lblFetchStatus.ForeColor = Color.Green;
                    return;
                }

                // Show loading indicator
                lblFetchStatus.Text = "Automatyczne ładowanie pojazdów z bazy...";
                lblFetchStatus.ForeColor = Color.Blue;
                btnFetchVehicles.Enabled = false;

                // Fetch from database
                var db2Service = _serviceProvider.GetRequiredService<DB2ExportService.Services.IDB2Service>();
                var vehicles = await db2Service.GetVehiclesAsync(null, null, true); // All active vehicles

                if (vehicles.Count == 0)
                {
                    lblFetchStatus.Text = "Brak aktywnych pojazdów w bazie";
                    lblFetchStatus.ForeColor = Color.Orange;
                    return;
                }

                // Cache results
                _cachedVehicles = vehicles;
                _cacheTimestamp = DateTime.Now;

                // Populate grid with config merge
                var configNumbers = GetConfigNumbersWithFallback();
                PopulateVehicleGridWithConfigMerge(vehicles, configNumbers);

                lblFetchStatus.Text = $"✓ Automatycznie wczytano {vehicles.Count} aktywnych pojazdów (zaznaczono {_selectedVehicleNBs.Count} z config)";
                lblFetchStatus.ForeColor = Color.Green;

                _logger.LogInformation("Auto-loaded {Count} vehicles", vehicles.Count);
            }
            catch (Exception ex)
            {
                lblFetchStatus.Text = "⚠️ Błąd automatycznego ładowania (kliknij 'Pobierz pojazdy' aby spróbować ponownie)";
                lblFetchStatus.ForeColor = Color.Orange;
                _logger.LogWarning(ex, "Auto-load vehicles failed (non-critical)");
            }
            finally
            {
                btnFetchVehicles.Enabled = true;
            }
        }

        /// <summary>
        /// Invalidates vehicle cache (call after manual fetch or filter change)
        /// </summary>
        private void InvalidateVehicleCache()
        {
            _cacheTimestamp = null;
            _cachedVehicles.Clear();
        }

        /// <summary>
        /// Applies all active filters to the vehicle list
        /// </summary>
        private List<VehicleInfo> ApplyFilters(List<VehicleInfo> vehicles)
        {
            var filtered = vehicles.AsEnumerable();

            // Filter: Zajezdnia
            if (cmbFilterZajezdnia?.SelectedItem != null &&
                cmbFilterZajezdnia.SelectedItem.ToString() != "(wszystkie)")
            {
                if (int.TryParse(cmbFilterZajezdnia.SelectedItem.ToString(), out int zajezdnia))
                {
                    filtered = filtered.Where(v => v.Zajezdnia == zajezdnia);
                }
            }

            // Filter: Typ Pojazdu
            if (cmbFilterTypPoj?.SelectedItem != null &&
                cmbFilterTypPoj.SelectedItem.ToString() != "(wszystkie)")
            {
                if (int.TryParse(cmbFilterTypPoj.SelectedItem.ToString(), out int typ))
                {
                    filtered = filtered.Where(v => v.TypPoj == typ);
                }
            }

            // Filter: Tylko z bramkami
            if (chkFilterBramki != null && chkFilterBramki.Checked)
            {
                filtered = filtered.Where(v => v.MaBramki == "Y" || v.MaBramki == "1");
            }

            // Filter: Text search (NB or NR)
            if (!string.IsNullOrWhiteSpace(txtFilterSearch?.Text))
            {
                var searchTerm = txtFilterSearch.Text.Trim().ToLower();
                filtered = filtered.Where(v =>
                    v.NB.ToString().Contains(searchTerm) ||
                    (v.NR != null && v.NR.ToLower().Contains(searchTerm)));
            }

            return filtered.ToList();
        }

        /// <summary>
        /// Populates filter dropdowns with distinct values from current dataset
        /// </summary>
        private void PopulateFilterDropdowns(List<VehicleInfo> vehicles)
        {
            if (vehicles == null || vehicles.Count == 0)
                return;

            // CRITICAL: Unhook events to prevent infinite loop!
            if (cmbFilterZajezdnia != null)
                cmbFilterZajezdnia.SelectedIndexChanged -= OnFilterChanged;
            if (cmbFilterTypPoj != null)
                cmbFilterTypPoj.SelectedIndexChanged -= OnFilterChanged;

            try
            {
                // Zajezdnia
                if (cmbFilterZajezdnia != null)
                {
                    var currentSelection = cmbFilterZajezdnia.SelectedItem;
                    cmbFilterZajezdnia.Items.Clear();
                    cmbFilterZajezdnia.Items.Add("(wszystkie)");

                var distinctZajezdnie = vehicles
                    .Select(v => v.Zajezdnia)
                    .Distinct()
                    .OrderBy(z => z);

                foreach (var z in distinctZajezdnie)
                {
                    cmbFilterZajezdnia.Items.Add(z.ToString());
                }

                cmbFilterZajezdnia.SelectedItem = currentSelection ?? "(wszystkie)";
                if (cmbFilterZajezdnia.SelectedIndex == -1)
                    cmbFilterZajezdnia.SelectedIndex = 0;
            }

            // Typ Pojazdu
            if (cmbFilterTypPoj != null)
            {
                var currentSelection = cmbFilterTypPoj.SelectedItem;
                cmbFilterTypPoj.Items.Clear();
                cmbFilterTypPoj.Items.Add("(wszystkie)");

                var distinctTypes = vehicles
                    .Select(v => v.TypPoj)
                    .Distinct()
                    .OrderBy(t => t);

                foreach (var t in distinctTypes)
                {
                    cmbFilterTypPoj.Items.Add(t.ToString());
                }

                    cmbFilterTypPoj.SelectedItem = currentSelection ?? "(wszystkie)";
                    if (cmbFilterTypPoj.SelectedIndex == -1)
                        cmbFilterTypPoj.SelectedIndex = 0;
                }
            }
            finally
            {
                // CRITICAL: Re-hook events after population
                if (cmbFilterZajezdnia != null)
                    cmbFilterZajezdnia.SelectedIndexChanged += OnFilterChanged;
                if (cmbFilterTypPoj != null)
                    cmbFilterTypPoj.SelectedIndexChanged += OnFilterChanged;
            }
        }

        /// <summary>
        /// Event handler for filter changes - applies filters and refreshes grid
        /// </summary>
        private void OnFilterChanged(object? sender, EventArgs e)
        {
            if (_cachedVehicles == null || _cachedVehicles.Count == 0)
                return;

            var filtered = ApplyFilters(_cachedVehicles);

            // Use config merge to preserve config-based selection
            var configNumbers = GetConfigNumbersWithFallback();
            PopulateVehicleGridWithConfigMerge(filtered, configNumbers);

            _logger.LogInformation("Filters applied: {Count} vehicles shown (from {Total})",
                filtered.Count, _cachedVehicles.Count);
        }

        // Debounced search
        private System.Threading.Timer? _searchDebounceTimer;
        private void TxtFilterSearch_TextChanged(object? sender, EventArgs e)
        {
            _searchDebounceTimer?.Dispose();
            _searchDebounceTimer = new System.Threading.Timer(_ =>
            {
                this.Invoke((Action)(() => OnFilterChanged(sender, e)));
            }, null, 300, Timeout.Infinite); // 300ms debounce
        }

        /// <summary>
        /// Virtual mode: Provides cell value on demand
        /// </summary>
        private void DgvVehicles_CellValueNeeded(object? sender, DataGridViewCellValueEventArgs e)
        {
            try
            {
                if (e.RowIndex < 0 || e.RowIndex >= _displayedVehicles.Count)
                    return;

                if (dgvVehicles == null || e.ColumnIndex < 0 || e.ColumnIndex >= dgvVehicles.Columns.Count)
                    return;

                var vehicle = _displayedVehicles[e.RowIndex];
                var columnName = dgvVehicles.Columns[e.ColumnIndex].Name;

                switch (columnName)
                {
                    case "Selected":
                        e.Value = _selectedVehicleNBs.Contains(vehicle.NB);
                        break;
                    case "NB":
                        e.Value = vehicle.NB;
                        break;
                    case "NR":
                        e.Value = vehicle.NR ?? "";
                        break;
                    case "TypPoj":
                        e.Value = vehicle.TypPoj;
                        break;
                    case "Zajezdnia":
                        e.Value = vehicle.Zajezdnia;
                        break;
                    case "MaBramki":
                        e.Value = vehicle.MaBramki ?? "";
                        break;
                    case "WGotowosci":
                        e.Value = vehicle.WGotowosci ?? "";
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DgvVehicles_CellValueNeeded for row {Row}, column {Col}", e.RowIndex, e.ColumnIndex);
            }
        }

        /// <summary>
        /// Virtual mode: Handles cell value changes (checkbox toggle)
        /// </summary>
        private void DgvVehicles_CellValuePushed(object? sender, DataGridViewCellValueEventArgs e)
        {
            try
            {
                if (e.RowIndex < 0 || e.RowIndex >= _displayedVehicles.Count)
                    return;

                if (dgvVehicles == null || e.ColumnIndex < 0 || e.ColumnIndex >= dgvVehicles.Columns.Count)
                    return;

                var vehicle = _displayedVehicles[e.RowIndex];
                var columnName = dgvVehicles.Columns[e.ColumnIndex].Name;

                if (columnName == "Selected" && e.Value is bool isSelected)
                {
                    if (isSelected)
                        _selectedVehicleNBs.Add(vehicle.NB);
                    else
                        _selectedVehicleNBs.Remove(vehicle.NB);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DgvVehicles_CellValuePushed for row {Row}, column {Col}", e.RowIndex, e.ColumnIndex);
            }
        }

        /// <summary>
        /// Virtual mode: Apply row styling based on vehicle properties
        /// Kolorowanie:
        /// - Config = niebieski
        /// - Gates = zielony
        /// - Config + Gates = ciemnozielony
        /// </summary>
        // DataGridView event handler for checkbox changes (immediate commit)
        private void DgvVehicles_CurrentCellDirtyStateChanged(object? sender, EventArgs e)
        {
            if (dgvVehicles.IsCurrentCellDirty && dgvVehicles.CurrentCell.ColumnIndex == 0)
            {
                dgvVehicles.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        // DataGridView event handler for cell value changes (checkbox clicks)
        private void DgvVehicles_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.RowIndex < 0 || e.ColumnIndex != 0) return; // Only handle checkbox column

                var row = dgvVehicles.Rows[e.RowIndex];
                var vehicle = _displayedVehicles[e.RowIndex];
                bool isChecked = row.Cells["Selected"].Value is true;

                if (isChecked)
                {
                    _selectedVehicleNBs.Add(vehicle.NB);
                    _logger.LogDebug("Vehicle {NB} checked (added to selection)", vehicle.NB);
                }
                else
                {
                    _selectedVehicleNBs.Remove(vehicle.NB);
                    _logger.LogDebug("Vehicle {NB} unchecked (removed from selection)", vehicle.NB);
                }

                // Refresh row formatting
                dgvVehicles.InvalidateRow(e.RowIndex);

                // Update synchronizer
                _selectionSynchronizer.SelectedNBs = _selectedVehicleNBs;

                _logger.LogInformation("Vehicle {NB} selection changed: {IsChecked}", vehicle.NB, isChecked);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DgvVehicles_CellValueChanged");
            }
        }

        // DataGridView event handler for cell formatting (colors, fonts, icons)
        private void DgvVehicles_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            try
            {
                if (e.RowIndex < 0 || e.RowIndex >= _displayedVehicles.Count) return;

                var vehicle = _displayedVehicles[e.RowIndex];
                var row = dgvVehicles.Rows[e.RowIndex];

                // Format icons for MA_BRAMKI and WGOTOWOSCI columns
                if (e.ColumnIndex == dgvVehicles.Columns["MA_BRAMKI"].Index)
                {
                    e.Value = VehicleIconRenderer.GetGatesIcon(vehicle.MaBramki);
                    bool isDarkMode = ThemeManager.CurrentTheme == AppTheme.Dark;
                    e.CellStyle.ForeColor = VehicleIconRenderer.GetGatesColor(vehicle.MaBramki, isDarkMode);
                }
                else if (e.ColumnIndex == dgvVehicles.Columns["WGOTOWOSCI"].Index)
                {
                    e.Value = VehicleIconRenderer.GetActiveIcon(vehicle.WGotowosci);
                    bool isDarkMode = ThemeManager.CurrentTheme == AppTheme.Dark;
                    e.CellStyle.ForeColor = VehicleIconRenderer.GetActiveColor(vehicle.WGotowosci, isDarkMode);
                }

                // Apply row background color based on selection state
                var state = new VehicleSelectionState
                {
                    Vehicle = vehicle,
                    IsInConfig = _selectionSynchronizer.ConfigNumbers.Contains(vehicle.NB),
                    IsInDatabase = true,
                    IsSelected = _selectedVehicleNBs.Contains(vehicle.NB),
                    HasGates = vehicle.MaBramki == "Y" || vehicle.MaBramki == "1"
                };

                e.CellStyle.BackColor = state.GetRowBackColor(ThemeManager.CurrentTheme == AppTheme.Dark);

                // Bold font for config matches
                if (state.Status == SelectionStatus.ConfigMatch)
                {
                    e.CellStyle.Font = new Font(e.CellStyle.Font ?? dgvVehicles.DefaultCellStyle.Font, FontStyle.Bold);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DgvVehicles_CellFormatting");
            }
        }

        /// <summary>
        /// Event handler: Eksport wybranych pojazdów do pliku CSV z możliwością wyboru kolumn
        /// </summary>
        private void BtnExportCSV_Click(object? sender, EventArgs e)
        {
            try
            {
                _logger.LogInformation("Rozpoczęcie eksportu CSV");

                if (_displayedVehicles == null || _displayedVehicles.Count == 0)
                {
                    MessageBox.Show("Brak pojazdów do eksportu!\n\nNajpierw pobierz pojazdy z bazy danych.",
                        "Informacja", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                using var columnChooser = new ColumnChooserDialog();
                if (columnChooser.ShowDialog() != DialogResult.OK)
                {
                    _logger.LogInformation("Eksport CSV anulowany przez użytkownika");
                    return;
                }

                var selectedColumns = columnChooser.SelectedColumns;
                if (selectedColumns.Count == 0)
                {
                    MessageBox.Show("Nie wybrano żadnych kolumn!", "Informacja",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                using var saveDialog = new SaveFileDialog
                {
                    Filter = "Pliki CSV (*.csv)|*.csv|Wszystkie pliki (*.*)|*.*",
                    FileName = $"pojazdy_export_{DateTime.Now:yyyy-MM-dd_HHmmss}.csv",
                    Title = "Eksportuj pojazdy do CSV",
                    DefaultExt = "csv"
                };

                if (saveDialog.ShowDialog() != DialogResult.OK) return;

                var csv = new System.Text.StringBuilder();
                var headers = new List<string>();
                foreach (var col in selectedColumns)
                {
                    if (ColumnChooserDialog.AvailableColumns.TryGetValue(col, out var displayName))
                        headers.Add(displayName);
                }
                csv.AppendLine(string.Join(";", headers));

                int exportedCount = 0;
                foreach (var vehicle in _displayedVehicles)
                {
                    var values = new List<string>();
                    foreach (var col in selectedColumns)
                    {
                        string value = col switch
                        {
                            "NB" => vehicle.NB.ToString(),
                            "NR" => vehicle.NR ?? "",
                            "TypPoj" => vehicle.TypPoj.ToString(),
                            "Zajezdnia" => vehicle.Zajezdnia.ToString(),
                            "MaBramki" => (vehicle.MaBramki == "Y" || vehicle.MaBramki == "1") ? "TAK" : "NIE",
                            "WGotowosci" => (vehicle.WGotowosci == "Y" || vehicle.WGotowosci == "1") ? "TAK" : "NIE",
                            _ => ""
                        };
                        value = value.Replace(";", ";;");
                        values.Add(value);
                    }
                    csv.AppendLine(string.Join(";", values));
                    exportedCount++;
                }

                File.WriteAllText(saveDialog.FileName, csv.ToString(), System.Text.Encoding.UTF8);
                _logger.LogInformation("Wyeksportowano {Count} pojazdów, {Columns} kolumn", exportedCount, selectedColumns.Count);

                var result = MessageBox.Show(
                    $"✅ Wyeksportowano {exportedCount} pojazdów ({selectedColumns.Count} kolumn) do:\n\n{saveDialog.FileName}\n\n" +
                    "Czy otworzyć wyeksportowany plik?",
                    "Eksport zakończony", MessageBoxButtons.YesNo, MessageBoxIcon.Information);

                if (result == DialogResult.Yes)
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = saveDialog.FileName,
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas eksportu CSV");
                MessageBox.Show($"Błąd podczas eksportu CSV:\n\n{ex.Message}", "Błąd",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
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
