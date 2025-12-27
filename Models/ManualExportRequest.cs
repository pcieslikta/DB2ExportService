namespace DB2ExportService.Models;

/// <summary>
/// Model żądania ręcznego eksportu z pliku JSON
/// </summary>
public class ManualExportRequest
{
    /// <summary>
    /// Godzina wykonania (opcjonalnie, null = natychmiast)
    /// Format: "HH:mm"
    /// </summary>
    public string? ScheduledTime { get; set; }

    /// <summary>
    /// Typy eksportu do wykonania
    /// </summary>
    public List<ExportType> ExportTypes { get; set; } = new();

    /// <summary>
    /// Zakres pojazdów (opcjonalnie, null = wszystkie z VehicleConfig)
    /// Format: "100-120, 789, 900-905"
    /// </summary>
    public string? VehicleRange { get; set; }

    /// <summary>
    /// Lista konkretnych pojazdów (alternatywa dla VehicleRange)
    /// </summary>
    public List<int>? VehicleList { get; set; }

    /// <summary>
    /// Ile dni wstecz eksportować (default: 1 - tylko dzisiaj)
    /// </summary>
    public int DaysCount { get; set; } = 1;

    /// <summary>
    /// Data początkowa eksportu (opcjonalnie, null = dzisiaj)
    /// Format: "yyyy-MM-dd"
    /// </summary>
    public DateTime? StartDate { get; set; }
}
