using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using DB2ExportService.Configuration;
using DB2ExportService.Models;
using Microsoft.Extensions.Logging;

namespace DB2ExportService.Services.Exporters;

public class BramkiBasicExporter : IExporter
{
    private readonly ILogger<BramkiBasicExporter> _logger;
    private readonly IDB2Service _db2Service;
    private readonly ChangeDetectionService _changeDetectionService;
    private readonly ConfigurationHelper _configHelper;
    private readonly string _exportPath;

    public BramkiBasicExporter(
        ILogger<BramkiBasicExporter> logger,
        IDB2Service db2Service,
        ChangeDetectionService changeDetectionService,
        ConfigurationHelper configHelper)
    {
        _logger = logger;
        _db2Service = db2Service;
        _changeDetectionService = changeDetectionService;
        _configHelper = configHelper;
        _exportPath = configHelper.GetExportConfig().ExportPath;
    }

    public async Task ExportAsync(DateTime targetDate, VehicleConfig vehicleConfig, bool skipChangeDetection = false)
    {
        try
        {
            _logger.LogInformation("Rozpoczęcie eksportu BRAMKI (basic) dla dnia {Date}", targetDate.ToString("yyyy-MM-dd"));

            // Sprawdź change detection
            if (!skipChangeDetection)
            {
                var recordCount = await _db2Service.GetRecordCountAsync(targetDate);
                if (!await _changeDetectionService.ShouldExportAsync(targetDate, recordCount, "r_count"))
                {
                    _logger.LogInformation("Eksport BRAMKI basic pominięty dla dnia {Date} - brak zmian", targetDate.ToString("yyyy-MM-dd"));
                    return;
                }
            }

            // Pobierz dane
            var data = await _db2Service.GetBramkiDataAsync(targetDate, vehicleConfig);
            if (!data.Any())
            {
                _logger.LogWarning("Brak danych do eksportu BRAMKI basic dla dnia {Date}", targetDate.ToString("yyyy-MM-dd"));
                return;
            }

            // Zapisz CSV
            var fileName = $"BRAMKI_{targetDate:yyyy-MM-dd}.csv";
            var filePath = Path.Combine(_exportPath, fileName);

            await WriteCsvAsync(filePath, data, new[]
            {
                "DATA", "NR_POJAZDU", "NR_KURSU", "NR_KURSOWK", "LP_PRZYSTANKU",
                "PRZYSTANEK", "SLUPEK_STANOWISKO", "KOD_PRZYSTANKU",
                "CZAS_NA_PRZYSTANKU_ROZKLADOWY", "WS", "WYS", "NAPELN",
                "NR_LINII", "ID_KURSU", "NR_WOZU"
            });

            _logger.LogInformation("Eksport BRAMKI basic zakończony: {FilePath} ({Count} rekordów)", filePath, data.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas eksportu BRAMKI basic dla dnia {Date}", targetDate.ToString("yyyy-MM-dd"));
            throw;
        }
    }

    private async Task WriteCsvAsync(string filePath, List<BramkiData> data, string[] headers)
    {
        // Użyj kodowania CP1250 dla polskich znaków
        var encoding = Encoding.GetEncoding(1250);

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ";",
            Encoding = encoding,
            HasHeaderRecord = true
        };

        await using var writer = new StreamWriter(filePath, false, encoding);
        await using var csv = new CsvWriter(writer, config);

        // Zapisz nagłówki
        foreach (var header in headers)
        {
            csv.WriteField(header);
        }
        await csv.NextRecordAsync();

        // Zapisz dane
        foreach (var record in data)
        {
            csv.WriteField(record.Data.ToString("yyyy-MM-dd"));
            csv.WriteField(record.NrPojazdu);
            csv.WriteField(record.NrKursu);
            csv.WriteField(record.NrKursowk);
            csv.WriteField(record.LpPrzystanku);
            csv.WriteField(record.Przystanek);
            csv.WriteField(record.SlupekStanowisko);
            csv.WriteField(record.KodPrzystanku);
            csv.WriteField(record.CzasNaPrzystankuRozkladowy);
            csv.WriteField(record.WS);
            csv.WriteField(record.WYS);
            csv.WriteField(record.Napeln);
            csv.WriteField(record.NrLinii);
            csv.WriteField(record.IdKursu);
            csv.WriteField(record.NrWozu);
            await csv.NextRecordAsync();
        }

        _logger.LogDebug("Zapisano {Count} rekordów do pliku {FilePath}", data.Count, filePath);
    }
}
