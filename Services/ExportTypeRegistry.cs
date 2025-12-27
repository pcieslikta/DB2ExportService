using DB2ExportService.Models;
using DB2ExportService.Services.Exporters;

namespace DB2ExportService.Services;

/// <summary>
/// Registry wzorca Strategy - mapuje typy eksportu na konkretne implementacje exporterów
/// </summary>
public class ExportTypeRegistry
{
    private readonly Dictionary<ExportType, IExporter> _exporters;

    public ExportTypeRegistry(
        BramkiBasicExporter bramkiBasic,
        BramkiDetailExporter bramkiDetail,
        PunktualnosćExporter punktualnosc)
    {
        _exporters = new Dictionary<ExportType, IExporter>
        {
            { ExportType.BramkiBasic, bramkiBasic },
            { ExportType.BramkiDetail, bramkiDetail },
            { ExportType.Punktualnosc, punktualnosc }
        };
    }

    /// <summary>
    /// Pobiera exporter dla danego typu eksportu
    /// </summary>
    /// <param name="type">Typ eksportu</param>
    /// <returns>Exporter lub null jeśli typ nie istnieje</returns>
    public IExporter? GetExporter(ExportType type)
    {
        return _exporters.GetValueOrDefault(type);
    }
}
