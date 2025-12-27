using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using DB2ExportService.Configuration;
using DB2ExportService.Models;
using Microsoft.Extensions.Logging;

namespace DB2ExportService.Services.Exporters;

public class BramkiDetailExporter : IExporter
{
    private readonly ILogger<BramkiDetailExporter> _logger;
    private readonly IDB2Service _db2Service;
    private readonly ChangeDetectionService _changeDetectionService;
    private readonly ConfigurationHelper _configHelper;
    private readonly string _exportPath;

    public BramkiDetailExporter(
        ILogger<BramkiDetailExporter> logger,
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
            _logger.LogInformation("Rozpoczęcie eksportu BRAMKID (detail) dla dnia {Date}", targetDate.ToString("yyyy-MM-dd"));

            // Sprawdź change detection
            if (!skipChangeDetection)
            {
                var recordCount = await _db2Service.GetRecordCountAsync(targetDate);
                if (!await _changeDetectionService.ShouldExportAsync(targetDate, recordCount, "r_countd"))
                {
                    _logger.LogInformation("Eksport BRAMKID detail pominięty dla dnia {Date} - brak zmian", targetDate.ToString("yyyy-MM-dd"));
                    return;
                }
            }

            // Pobierz dane
            var data = await _db2Service.GetBramkiDetailDataAsync(targetDate, vehicleConfig);
            if (!data.Any())
            {
                _logger.LogWarning("Brak danych do eksportu BRAMKID detail dla dnia {Date}", targetDate.ToString("yyyy-MM-dd"));
                return;
            }

            // Zapisz CSV
            var fileName = $"BRAMKID_{targetDate:yyyy-MM-dd}.csv";
            var filePath = Path.Combine(_exportPath, fileName);

            await WriteCsvAsync(filePath, data, new[]
            {
                "DATA", "NR_POJAZDU", "NR LINII", "NR LINII/PLANU", "NR KURSU",
                "LP PRZYST", "PRZYSTANEK", "CZAS NA PRZYSTANKU ROZKŁAD",
                "Wejście Drzwi 1", "Wyjście Drzwi 1", "Wejście Drzwi 2", "Wyjście Drzwi 2",
                "Wejście Drzwi 3", "Wyjście Drzwi 3", "Wejście Drzwi 4", "Wyjście Drzwi 4",
                "WS (Wejścia Suma)", "WYS (Wyjścia Suma)", "NAPEŁN"
            });

            _logger.LogInformation("Eksport BRAMKID detail zakończony: {FilePath} ({Count} rekordów)", filePath, data.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas eksportu BRAMKID detail dla dnia {Date}", targetDate.ToString("yyyy-MM-dd"));
            throw;
        }
    }

    private async Task WriteCsvAsync(string filePath, List<BramkiDetailData> data, string[] headers)
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
            csv.WriteField(record.NrLinii);
            csv.WriteField(record.NrLiniiPlanu);
            csv.WriteField(record.NrKursu);
            csv.WriteField(record.LpPrzyst);
            csv.WriteField(record.Przystanek);
            csv.WriteField(record.CzasNaPrzystankuRozklad);
            csv.WriteField(record.WejścieDrzwi1);
            csv.WriteField(record.WyjścieDrzwi1);
            csv.WriteField(record.WejścieDrzwi2);
            csv.WriteField(record.WyjścieDrzwi2);
            csv.WriteField(record.WejścieDrzwi3);
            csv.WriteField(record.WyjścieDrzwi3);
            csv.WriteField(record.WejścieDrzwi4);
            csv.WriteField(record.WyjścieDrzwi4);
            csv.WriteField(record.WS);
            csv.WriteField(record.WYS);
            csv.WriteField(record.Napełn);
            await csv.NextRecordAsync();
        }

        _logger.LogDebug("Zapisano {Count} rekordów do pliku {FilePath}", data.Count, filePath);
    }
}
