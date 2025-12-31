using System.Runtime.InteropServices;
using System.Text;
using DB2ExportService.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DB2ExportService.Configuration;

public class ConfigurationHelper
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ConfigurationHelper>? _logger;

    public ConfigurationHelper(IConfiguration configuration, ILogger<ConfigurationHelper>? logger = null)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public DB2Config GetDB2Config(string configKey = "DB2")
    {
        _logger?.LogDebug("Pobieranie konfiguracji DB2 z sekcji: {ConfigKey}", configKey);

        var config = _configuration.GetSection(configKey).Get<DB2Config>();
        if (config == null)
        {
            throw new InvalidOperationException($"Brak konfiguracji DB2 w sekcji '{configKey}'");
        }

        _logger?.LogDebug("Konfiguracja DB2 z appsettings.json:");
        _logger?.LogDebug("  Database: {Database}", config.Database);
        _logger?.LogDebug("  Hostname: {Hostname}", config.Hostname);
        _logger?.LogDebug("  Port: {Port}", config.Port);
        _logger?.LogDebug("  User z config: {User}", string.IsNullOrEmpty(config.User) ? "***EMPTY***" : config.User);
        _logger?.LogDebug("  Password z config: {Password}", string.IsNullOrEmpty(config.Password) ? "***EMPTY***" : "***SET***");
        _logger?.LogDebug("  UseCredentialManager: {UseCredentialManager}", config.UseCredentialManager);
        _logger?.LogDebug("  CredentialKey: {CredentialKey}", config.CredentialKey);

        // Jeśli UseCredentialManager = true, pobierz credentials z Windows Credential Manager
        if (config.UseCredentialManager && !string.IsNullOrEmpty(config.CredentialKey))
        {
            _logger?.LogInformation("Próba pobrania credentials z Windows Credential Manager: {CredentialKey}", config.CredentialKey);

            var credential = GetCredentialFromManager(config.CredentialKey);
            if (credential != null)
            {
                _logger?.LogInformation("Credentials pobrane pomyślnie z Credential Manager. Username: {Username}", credential.Username);
                config.User = credential.Username;
                config.Password = credential.Password;
            }
            else
            {
                _logger?.LogWarning("NIE ZNALEZIONO credentials w Credential Manager dla klucza: {CredentialKey}", config.CredentialKey);
                _logger?.LogWarning("User pozostaje: {User}", string.IsNullOrEmpty(config.User) ? "***EMPTY***" : config.User);
                _logger?.LogWarning("Password pozostaje: {Password}", string.IsNullOrEmpty(config.Password) ? "***EMPTY***" : "***SET***");
            }
        }

        _logger?.LogInformation("Finalna konfiguracja DB2:");
        _logger?.LogInformation("  User: {User}", string.IsNullOrEmpty(config.User) ? "***EMPTY***" : config.User);
        _logger?.LogInformation("  Password: {Password}", string.IsNullOrEmpty(config.Password) ? "***EMPTY***" : "***SET***");

        return config;
    }

    public ExportConfig GetExportConfig()
    {
        _logger?.LogDebug("Pobieranie konfiguracji ExportConfig z sekcji: ExportConfig");

        var section = _configuration.GetSection("ExportConfig");

        // Debug: zobacz co jest w sekcji
        var enabledTypesRaw = section.GetSection("EnabledExportTypes").Get<List<string>>();
        _logger?.LogInformation("EnabledExportTypes RAW z JSON: {Types}",
            enabledTypesRaw != null ? string.Join(", ", enabledTypesRaw) : "NULL");

        var config = section.Get<ExportConfig>();
        if (config == null)
        {
            throw new InvalidOperationException("Brak konfiguracji ExportConfig");
        }

        // POPRAWKA: Zawsze parsuj ręcznie EnabledExportTypes, ponieważ System.Text.Json
        // nie deserializuje automatycznie enumów ze stringów JSON bez JsonStringEnumConverter
        if (enabledTypesRaw != null && enabledTypesRaw.Count > 0)
        {
            _logger?.LogInformation("Ręczne parsowanie EnabledExportTypes z {Count} elementów", enabledTypesRaw.Count);
            config.EnabledExportTypes.Clear(); // Wyczyść wartości domyślne

            foreach (var typeStr in enabledTypesRaw)
            {
                if (Enum.TryParse<ExportType>(typeStr, ignoreCase: true, out var exportType))
                {
                    config.EnabledExportTypes.Add(exportType);
                    _logger?.LogInformation("  ✓ Sparsowano: {TypeString} → {ExportType}", typeStr, exportType);
                }
                else
                {
                    _logger?.LogError("  ✗ Nie można sparsować typu: '{Type}'. Dostępne wartości: {ValidValues}",
                        typeStr, string.Join(", ", Enum.GetNames<ExportType>()));
                }
            }
        }
        else
        {
            _logger?.LogWarning("EnabledExportTypes RAW jest NULL lub pusta! Sprawdź format JSON.");
        }

        _logger?.LogInformation("ExportConfig załadowany:");
        _logger?.LogInformation("  EnabledExportTypes ({Count}): {Types}",
            config.EnabledExportTypes.Count, string.Join(", ", config.EnabledExportTypes));
        _logger?.LogInformation("  DaysBack: {DaysBack}", config.DaysBack);
        _logger?.LogInformation("  ExportPath: {ExportPath}", config.ExportPath);
        _logger?.LogInformation("  EnablePeriodicMonitoring: {Enabled}", config.EnablePeriodicMonitoring);
        _logger?.LogInformation("  MonitoringIntervalMinutes: {Interval}", config.MonitoringIntervalMinutes);

        if (config.EnabledExportTypes.Count == 0)
        {
            _logger?.LogError("BŁĄD KRYTYCZNY: EnabledExportTypes jest pusta! Brak eksportów do wykonania. Sprawdź appsettings.json!");
        }

        return config;
    }

    public VehicleConfig GetVehicleConfig()
    {
        var config = _configuration.GetSection("VehicleConfig").Get<VehicleConfig>();
        if (config == null)
        {
            throw new InvalidOperationException("Brak konfiguracji VehicleConfig");
        }
        return config;
    }

    private CredentialInfo? GetCredentialFromManager(string targetName)
    {
        try
        {
            _logger?.LogDebug("Wywołanie CredRead dla klucza: {TargetName}", targetName);

            var credential = new CREDENTIAL();
            var credPointer = IntPtr.Zero;

            if (CredRead(targetName, CRED_TYPE.GENERIC, 0, out credPointer))
            {
                _logger?.LogDebug("CredRead zwrócił true - credentials znalezione");

                try
                {
                    credential = Marshal.PtrToStructure<CREDENTIAL>(credPointer);

                    var username = Marshal.PtrToStringUni(credential.UserName) ?? string.Empty;
                    var passwordBytes = new byte[credential.CredentialBlobSize];
                    Marshal.Copy(credential.CredentialBlob, passwordBytes, 0, credential.CredentialBlobSize);
                    var password = Encoding.Unicode.GetString(passwordBytes) ?? string.Empty;

                    _logger?.LogDebug("Credentials zdekodowane. Username: {Username}, Password length: {Length}",
                        username, password.Length);

                    return new CredentialInfo
                    {
                        Username = username,
                        Password = password
                    };
                }
                finally
                {
                    CredFree(credPointer);
                }
            }

            var lastError = Marshal.GetLastWin32Error();
            _logger?.LogWarning("CredRead zwrócił false. LastError: {LastError}", lastError);

            return null;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Wyjątek podczas odczytu credentials z Credential Manager");
            throw new InvalidOperationException($"Błąd podczas odczytu credentials z Windows Credential Manager (klucz: {targetName})", ex);
        }
    }

    #region Windows Credential Manager P/Invoke

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredRead(string target, CRED_TYPE type, int reservedFlag, out IntPtr credentialPtr);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool CredFree(IntPtr cred);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct CREDENTIAL
    {
        public int Flags;
        public int Type;
        public IntPtr TargetName;
        public IntPtr Comment;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
        public int CredentialBlobSize;
        public IntPtr CredentialBlob;
        public int Persist;
        public int AttributeCount;
        public IntPtr Attributes;
        public IntPtr TargetAlias;
        public IntPtr UserName;
    }

    private enum CRED_TYPE : int
    {
        GENERIC = 1,
        DOMAIN_PASSWORD = 2,
        DOMAIN_CERTIFICATE = 3,
        DOMAIN_VISIBLE_PASSWORD = 4
    }

    #endregion

    private class CredentialInfo
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
