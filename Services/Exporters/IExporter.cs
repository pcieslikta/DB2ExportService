namespace DB2ExportService.Services.Exporters;

using DB2ExportService.Models;

/// <summary>
/// Interfejs dla wszystkich exporterów
/// </summary>
public interface IExporter
{
    /// <summary>
    /// Wykonuje eksport dla określonej daty
    /// </summary>
    /// <param name="targetDate">Data eksportu</param>
    /// <param name="vehicleConfig">Konfiguracja pojazdów</param>
    /// <param name="skipChangeDetection">Czy pominąć sprawdzanie zmian (dla ręcznych triggerów)</param>
    Task ExportAsync(DateTime targetDate, VehicleConfig vehicleConfig, bool skipChangeDetection = false);
}
