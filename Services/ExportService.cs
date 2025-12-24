using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using DB2ExportService.Configuration;
using DB2ExportService.Models;
using Microsoft.Extensions.Logging;

namespace DB2ExportService.Services;

public class ExportService
{
    private readonly ILogger<ExportService> _logger;
    private readonly IDB2Service _db2Service;
    private readonly ChangeDetectionService _changeDetectionService;
    private readonly ConfigurationHelper _configHelper;
    private readonly string _exportPath;

    public ExportService(
        ILogger<ExportService> logger,
        IDB2Service db2Service,
        ChangeDetectionService changeDetectionService,
        ConfigurationHelper configHelper)
    {
        _logger = logger;
        _db2Service = db2Service;
        _changeDetectionService = changeDetectionService;
        _configHelper = configHelper;

        var config = _configHelper.GetExportConfig();
        _exportPath = config.ExportPath;

        // Upewnij się, że katalog eksportu istnieje
        if (!Directory.Exists(_exportPath))
        {
            Directory.CreateDirectory(_exportPath);
            _logger.LogInformation("Utworzono katalog dla eksportów: {ExportPath}", _exportPath);
        }
    }

    public async Task RunExportAsync()
    {
        try
        {
            _logger.LogInformation("=== Rozpoczęcie eksportu danych ===");

            var exportConfig = _configHelper.GetExportConfig();
            var vehicleConfig = _configHelper.GetVehicleConfig();

            var kodExportu = vehicleConfig.KodExportu.ToUpper();
            _logger.LogInformation("Kod eksportu: {KodExportu}", kodExportu);

            // Eksport dla zakresu dni wstecz
            for (int daysBack = exportConfig.DaysBack; daysBack < -1; daysBack++)
            {
                var targetDate = DateTime.Today.AddDays(daysBack);
                _logger.LogInformation("--- Eksport dla dnia: {Date} (dni wstecz: {DaysBack}) ---",
                    targetDate.ToString("yyyy-MM-dd"), daysBack);

                // Eksport BRAMKI (podstawowy)
                await ExportBramkiAsync(targetDate, vehicleConfig);

                // Eksport BRAMKID (szczegółowy) - tylko dla SOSNO
                if (kodExportu == "SOSNO")
                {
                    await ExportBramkiDetailAsync(targetDate, vehicleConfig);
                }
            }

            _logger.LogInformation("=== Eksport danych zakończony pomyślnie ===");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas wykonywania eksportu");
            throw;
        }
    }

    private async Task ExportBramkiAsync(DateTime targetDate, VehicleConfig vehicleConfig)
    {
        try
        {
            _logger.LogInformation("Rozpoczęcie eksportu BRAMKI dla dnia {Date}", targetDate.ToString("yyyy-MM-dd"));

            // Sprawdź liczbę rekordów
            var recordCount = await _db2Service.GetRecordCountAsync(targetDate);
            if (!await _changeDetectionService.ShouldExportAsync(targetDate, recordCount, "r_count"))
            {
                _logger.LogInformation("Eksport BRAMKI pominięty dla dnia {Date}", targetDate.ToString("yyyy-MM-dd"));
                return;
            }

            // Pobierz dane
            var data = await _db2Service.GetBramkiDataAsync(targetDate, vehicleConfig);
            if (!data.Any())
            {
                _logger.LogWarning("Brak danych do eksportu BRAMKI dla dnia {Date}", targetDate.ToString("yyyy-MM-dd"));
                return;
            }

            // Zapisz do CSV
            var fileName = $"BRAMKI_{targetDate:yyyy-MM-dd}.csv";
            var filePath = Path.Combine(_exportPath, fileName);

            await WriteCsvAsync(filePath, data, new[]
            {
                "DATA", "NR_POJAZDU", "NR_KURSU", "NR_KURSOWK", "LP_PRZYSTANKU",
                "PRZYSTANEK", "SLUPEK_STANOWISKO", "KOD_PRZYSTANKU",
                "CZAS_NA_PRZYSTANKU_ROZKLADOWY", "WS", "WYS", "NAPELN",
                "NR_LINII", "ID_KURSU", "NR_WOZU"
            });

            _logger.LogInformation("Eksport BRAMKI zakończony: {FilePath} ({Count} rekordów)", filePath, data.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas eksportu BRAMKI dla dnia {Date}", targetDate.ToString("yyyy-MM-dd"));
            throw;
        }
    }

    private async Task ExportBramkiDetailAsync(DateTime targetDate, VehicleConfig vehicleConfig)
    {
        try
        {
            _logger.LogInformation("Rozpoczęcie eksportu BRAMKID dla dnia {Date}", targetDate.ToString("yyyy-MM-dd"));

            // Sprawdź liczbę rekordów
            var recordCount = await _db2Service.GetRecordCountAsync(targetDate);
            if (!await _changeDetectionService.ShouldExportAsync(targetDate, recordCount, "r_countd"))
            {
                _logger.LogInformation("Eksport BRAMKID pominięty dla dnia {Date}", targetDate.ToString("yyyy-MM-dd"));
                return;
            }

            // Pobierz dane
            var data = await _db2Service.GetBramkiDetailDataAsync(targetDate, vehicleConfig);
            if (!data.Any())
            {
                _logger.LogWarning("Brak danych do eksportu BRAMKID dla dnia {Date}", targetDate.ToString("yyyy-MM-dd"));
                return;
            }

            // Zapisz do CSV
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

            _logger.LogInformation("Eksport BRAMKID zakończony: {FilePath} ({Count} rekordów)", filePath, data.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas eksportu BRAMKID dla dnia {Date}", targetDate.ToString("yyyy-MM-dd"));
            throw;
        }
    }

    private async Task WriteCsvAsync<T>(string filePath, List<T> data, string[] headers)
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
            if (record != null)
            {
                WriteRecord(csv, record);
                await csv.NextRecordAsync();
            }
        }

        _logger.LogDebug("Zapisano {Count} rekordów do pliku {FilePath}", data.Count, filePath);
    }

    private void WriteRecord(CsvWriter csv, object record)
    {
        if (record is BramkiData bramkiData)
        {
            csv.WriteField(bramkiData.Data.ToString("yyyy-MM-dd"));
            csv.WriteField(bramkiData.NrPojazdu);
            csv.WriteField(bramkiData.NrKursu);
            csv.WriteField(bramkiData.NrKursowk);
            csv.WriteField(bramkiData.LpPrzystanku);
            csv.WriteField(bramkiData.Przystanek);
            csv.WriteField(bramkiData.SlupekStanowisko);
            csv.WriteField(bramkiData.KodPrzystanku);
            csv.WriteField(bramkiData.CzasNaPrzystankuRozkladowy);
            csv.WriteField(bramkiData.WS);
            csv.WriteField(bramkiData.WYS);
            csv.WriteField(bramkiData.Napeln);
            csv.WriteField(bramkiData.NrLinii);
            csv.WriteField(bramkiData.IdKursu);
            csv.WriteField(bramkiData.NrWozu);
        }
        else if (record is BramkiDetailData detailData)
        {
            csv.WriteField(detailData.Data.ToString("yyyy-MM-dd"));
            csv.WriteField(detailData.NrPojazdu);
            csv.WriteField(detailData.NrLinii);
            csv.WriteField(detailData.NrLiniiPlanu);
            csv.WriteField(detailData.NrKursu);
            csv.WriteField(detailData.LpPrzyst);
            csv.WriteField(detailData.Przystanek);
            csv.WriteField(detailData.CzasNaPrzystankuRozklad);
            csv.WriteField(detailData.WejścieDrzwi1);
            csv.WriteField(detailData.WyjścieDrzwi1);
            csv.WriteField(detailData.WejścieDrzwi2);
            csv.WriteField(detailData.WyjścieDrzwi2);
            csv.WriteField(detailData.WejścieDrzwi3);
            csv.WriteField(detailData.WyjścieDrzwi3);
            csv.WriteField(detailData.WejścieDrzwi4);
            csv.WriteField(detailData.WyjścieDrzwi4);
            csv.WriteField(detailData.WS);
            csv.WriteField(detailData.WYS);
            csv.WriteField(detailData.Napełn);
        }
    }
}
