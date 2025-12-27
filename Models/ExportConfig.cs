namespace DB2ExportService.Models;

/// <summary>
/// Typy dostępnych eksportów
/// </summary>
public enum ExportType
{
    BramkiBasic,        // Podstawowe bramki (WS/WYS)
    BramkiDetail,       // Szczegółowe 4 drzwi
    Punktualnosc        // Placeholder na przyszłość
}

public class ExportConfig
{
    public string KodExportu { get; set; } = string.Empty;
    public string ExportPath { get; set; } = "C:\\EXPORT\\";
    public string LogPath { get; set; } = "C:\\EXPORT\\LOG\\";
    public string ScheduleTime { get; set; } = "13:15";
    public int DaysBack { get; set; } = -2;

    // Export Types Configuration
    /// <summary>
    /// Lista typów eksportu do wykonania
    /// </summary>
    public List<ExportType> EnabledExportTypes { get; set; } = new()
    {
        ExportType.BramkiBasic,
        ExportType.BramkiDetail
    };

    // Periodic Monitoring
    /// <summary>
    /// Włącz periodic monitoring (sprawdzanie co X minut)
    /// </summary>
    public bool EnablePeriodicMonitoring { get; set; } = false;

    /// <summary>
    /// Interwal sprawdzania w minutach (default: 15 minut)
    /// </summary>
    public int MonitoringIntervalMinutes { get; set; } = 15;

    /// <summary>
    /// Ile dni wstecz sprawdzać podczas periodic monitoring
    /// </summary>
    public int MonitoringDaysBack { get; set; } = 7;

    /// <summary>
    /// Ścieżka do folderu z triggerami JSON dla ręcznego eksportu
    /// </summary>
    public string TriggerFolderPath { get; set; } = "C:\\Services\\DB2Export\\Triggers";

    // File management
    public bool EnableZipCompression { get; set; } = true;
    public int FileRetentionDays { get; set; } = 90; // Kasuj pliki starsze niż 90 dni
    public bool EnableAutoArchiving { get; set; } = true;
    public string? ArchivePath { get; set; } // Jeśli null, użyj ExportPath/archive

    // Performance
    public int MaxParallelTasks { get; set; } = 3;
    public int BatchSize { get; set; } = 1000;

    // Resilience (Polly)
    public int RetryCount { get; set; } = 3;
    public int RetryDelaySeconds { get; set; } = 5;
    public int CircuitBreakerFailureThreshold { get; set; } = 5;
    public int CircuitBreakerDurationSeconds { get; set; } = 60;

    // Monitoring
    public bool EnableDetailedLogging { get; set; } = true;
    public bool EnableMetrics { get; set; } = true;

    // Notifications
    public bool EnableEmailNotifications { get; set; } = false;
    public string? NotificationEmail { get; set; }
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

public class VehicleConfig
{
    public string KodExportu { get; set; } = string.Empty;

    [Obsolete("Używaj PojazdyLista. Pole zachowane dla kompatybilności wstecznej.")]
    public string PojazdyMode { get; set; } = "lista"; // Always "lista" with unified interface

    [Obsolete("Używaj PojazdyLista. Pole zachowane dla kompatybilności wstecznej.")]
    public int? PojazdyStart { get; set; }

    [Obsolete("Używaj PojazdyLista. Pole zachowane dla kompatybilności wstecznej.")]
    public int? PojazdyEnd { get; set; }

    public List<int> PojazdyLista { get; set; } = new();
}

public class BramkiData
{
    public DateTime Data { get; set; }
    public int NrPojazdu { get; set; }
    public int NrKursu { get; set; }
    public string NrKursowk { get; set; } = string.Empty;
    public int LpPrzystanku { get; set; }
    public string Przystanek { get; set; } = string.Empty;
    public int SlupekStanowisko { get; set; }
    public int KodPrzystanku { get; set; }
    public string CzasNaPrzystankuRozkladowy { get; set; } = string.Empty;
    public int WS { get; set; }
    public int WYS { get; set; }
    public int Napeln { get; set; }
    public string NrLinii { get; set; } = string.Empty;
    public int IdKursu { get; set; }
    public string NrWozu { get; set; } = string.Empty;
}

public class BramkiDetailData
{
    public DateTime Data { get; set; }
    public int NrPojazdu { get; set; }
    public string NrLinii { get; set; } = string.Empty;
    public string NrLiniiPlanu { get; set; } = string.Empty;
    public int NrKursu { get; set; }
    public int LpPrzyst { get; set; }
    public string Przystanek { get; set; } = string.Empty;
    public string CzasNaPrzystankuRozklad { get; set; } = string.Empty;
    public int WejścieDrzwi1 { get; set; }
    public int WyjścieDrzwi1 { get; set; }
    public int WejścieDrzwi2 { get; set; }
    public int WyjścieDrzwi2 { get; set; }
    public int WejścieDrzwi3 { get; set; }
    public int WyjścieDrzwi3 { get; set; }
    public int WejścieDrzwi4 { get; set; }
    public int WyjścieDrzwi4 { get; set; }
    public int WS { get; set; }
    public int WYS { get; set; }
    public int Napełn { get; set; }
}
