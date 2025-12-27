using DB2ExportService.Models;
using Microsoft.Extensions.Logging;

namespace DB2ExportService.Services.Exporters;

/// <summary>
/// Placeholder dla eksportu danych punktualności
/// </summary>
public class PunktualnosćExporter : IExporter
{
    private readonly ILogger<PunktualnosćExporter> _logger;

    public PunktualnosćExporter(ILogger<PunktualnosćExporter> logger)
    {
        _logger = logger;
    }

    public Task ExportAsync(DateTime targetDate, VehicleConfig vehicleConfig, bool skipChangeDetection = false)
    {
        _logger.LogWarning("Eksport punktualności nie jest jeszcze zaimplementowany");
        throw new NotImplementedException("Eksport punktualności zostanie zaimplementowany w przyszłości");
    }
}
