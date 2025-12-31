using DB2ExportService.Configuration;
using DB2ExportService.Models;
using DB2ExportService.Services.Exporters;
using Microsoft.Extensions.Logging;

namespace DB2ExportService.Services;

public class ExportService
{
    private readonly ILogger<ExportService> _logger;
    private readonly IDB2Service _db2Service;
    private readonly ChangeDetectionService _changeDetectionService;
    private readonly ConfigurationHelper _configHelper;
    private readonly ExportTypeRegistry _exportRegistry;
    private readonly string _exportPath;

    public ExportService(
        ILogger<ExportService> logger,
        IDB2Service db2Service,
        ChangeDetectionService changeDetectionService,
        ConfigurationHelper configHelper,
        ExportTypeRegistry exportRegistry)
    {
        _logger = logger;
        _db2Service = db2Service;
        _changeDetectionService = changeDetectionService;
        _configHelper = configHelper;
        _exportRegistry = exportRegistry;

        var config = _configHelper.GetExportConfig();
        _exportPath = config.ExportPath;

        // Upewnij się, że katalog eksportu istnieje
        if (!Directory.Exists(_exportPath))
        {
            Directory.CreateDirectory(_exportPath);
            _logger.LogInformation("Utworzono katalog dla eksportów: {ExportPath}", _exportPath);
        }
    }

    /// <summary>
    /// Główna metoda eksportu - scheduled daily export
    /// </summary>
    public async Task RunScheduledExportAsync()
    {
        try
        {
            _logger.LogInformation("=== Rozpoczęcie zaplanowanego eksportu ===");

            var exportConfig = _configHelper.GetExportConfig();
            var vehicleConfig = _configHelper.GetVehicleConfig();

            var enabledTypes = exportConfig.EnabledExportTypes;
            _logger.LogInformation("Konfiguracja DaysBack: {DaysBack}", exportConfig.DaysBack);
            _logger.LogInformation("Włączone typy eksportu ({Count}): {Types}",
                enabledTypes.Count, string.Join(", ", enabledTypes));

            if (enabledTypes.Count == 0)
            {
                _logger.LogWarning("UWAGA: Lista EnabledExportTypes jest pusta! Brak eksportów do wykonania.");
                return;
            }

            // Eksport dla zakresu dni wstecz
            // DaysBack = -2 oznacza "eksportuj ostatnie 2 dni" (dzisiaj i wczoraj)
            // DaysBack = -7 oznacza "eksportuj ostatnie 7 dni" (dzisiaj i 6 dni wstecz)
            var daysToExport = Math.Abs(exportConfig.DaysBack);
            _logger.LogInformation("Eksport będzie wykonany dla ostatnich {Days} dni (od dzisiaj wstecz)", daysToExport);

            for (int i = 0; i < daysToExport; i++)
            {
                var targetDate = DateTime.Today.AddDays(-i);
                _logger.LogInformation("--- Eksport dla dnia: {Date} (dzień {Index}/{Total}) ---",
                    targetDate.ToString("yyyy-MM-dd"), i + 1, daysToExport);

                await RunExportsForDateAsync(targetDate, enabledTypes, vehicleConfig);
            }

            _logger.LogInformation("=== Zaplanowany eksport zakończony ===");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas zaplanowanego eksportu");
            throw;
        }
    }

    /// <summary>
    /// Periodic monitoring - sprawdza ostatnie N dni
    /// </summary>
    public async Task RunPeriodicMonitoringAsync()
    {
        try
        {
            _logger.LogInformation("=== Rozpoczęcie periodic monitoring ===");

            var exportConfig = _configHelper.GetExportConfig();
            var vehicleConfig = _configHelper.GetVehicleConfig();

            _logger.LogInformation("Sprawdzanie ostatnich {Days} dni (od dzisiaj wstecz)", exportConfig.MonitoringDaysBack);

            var enabledTypes = exportConfig.EnabledExportTypes;

            // Sprawdź ostatnie N dni (od dzisiaj wstecz)
            for (int i = 0; i < exportConfig.MonitoringDaysBack; i++)
            {
                var targetDate = DateTime.Today.AddDays(-i);
                _logger.LogInformation("--- Monitoring dla dnia: {Date} (dzień {Index}/{Total}) ---",
                    targetDate.ToString("yyyy-MM-dd"), i + 1, exportConfig.MonitoringDaysBack);

                await RunExportsForDateAsync(targetDate, enabledTypes, vehicleConfig);
            }

            _logger.LogInformation("=== Periodic monitoring zakończony ===");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas periodic monitoring");
            throw;
        }
    }

    /// <summary>
    /// Ręczny eksport z parametrami z JSON
    /// </summary>
    public async Task RunManualExportAsync(ManualExportRequest request)
    {
        try
        {
            _logger.LogInformation("=== Rozpoczęcie ręcznego eksportu ===");
            _logger.LogInformation("Typy: {Types}, Dni: {Days}",
                string.Join(", ", request.ExportTypes), request.DaysCount);

            // Określ pojazdy
            VehicleConfig vehicleConfig;
            if (request.VehicleList != null && request.VehicleList.Any())
            {
                vehicleConfig = new VehicleConfig
                {
                    KodExportu = _configHelper.GetVehicleConfig().KodExportu,
                    PojazdyLista = request.VehicleList
                };
                _logger.LogInformation("Użyto listy pojazdów z triggera: {Count} pojazdów", request.VehicleList.Count);
            }
            else if (!string.IsNullOrEmpty(request.VehicleRange))
            {
                vehicleConfig = new VehicleConfig
                {
                    KodExportu = _configHelper.GetVehicleConfig().KodExportu,
                    PojazdyLista = ParseVehicleRange(request.VehicleRange)
                };
                _logger.LogInformation("Sparsowano zakres pojazdów: {Range} → {Count} pojazdów",
                    request.VehicleRange, vehicleConfig.PojazdyLista.Count);
            }
            else
            {
                vehicleConfig = _configHelper.GetVehicleConfig();
                _logger.LogInformation("Użyto konfiguracji domyślnej pojazdów");
            }

            // Określ start date
            var startDate = request.StartDate ?? DateTime.Today;

            // Eksportuj dla każdego dnia
            for (int day = 0; day < request.DaysCount; day++)
            {
                var targetDate = startDate.AddDays(-day);

                await RunExportsForDateAsync(targetDate, request.ExportTypes, vehicleConfig,
                    skipChangeDetection: true); // Ręczny eksport ignoruje change detection
            }

            _logger.LogInformation("=== Ręczny eksport zakończony ===");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas ręcznego eksportu");
            throw;
        }
    }

    /// <summary>
    /// Wykonuje wszystkie typy eksportów dla danej daty
    /// </summary>
    private async Task RunExportsForDateAsync(
        DateTime targetDate,
        List<ExportType> exportTypes,
        VehicleConfig vehicleConfig,
        bool skipChangeDetection = false)
    {
        _logger.LogInformation("RunExportsForDateAsync - typy do eksportu ({Count}): {Types}",
            exportTypes.Count, string.Join(", ", exportTypes));

        foreach (var exportType in exportTypes)
        {
            try
            {
                _logger.LogInformation(">>> Przetwarzanie typu eksportu: {ExportType}", exportType);
                var exporter = _exportRegistry.GetExporter(exportType);

                if (exporter != null)
                {
                    _logger.LogInformation("Znaleziono exporter dla typu: {ExportType}, rozpoczynam eksport...", exportType);
                    await exporter.ExportAsync(targetDate, vehicleConfig, skipChangeDetection);
                }
                else
                {
                    _logger.LogWarning("Brak exportera dla typu: {ExportType}", exportType);
                }
            }
            catch (NotImplementedException)
            {
                _logger.LogWarning("Eksport typu {ExportType} nie jest jeszcze zaimplementowany", exportType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas eksportu typu {ExportType} dla dnia {Date}",
                    exportType, targetDate.ToString("yyyy-MM-dd"));
                // Nie przerywaj innych eksportów
            }
        }
    }

    /// <summary>
    /// Parsuje zakres pojazdów w formacie "100-120, 789, 900-905"
    /// Obsługuje:
    /// - Automatyczną zamianę zakresów wstecznych (200-100 → 100-200)
    /// - Maksymalny limit 10000 elementów (zabezpieczenie DoS)
    /// - Szczegółowe logowanie błędów
    /// </summary>
    private List<int> ParseVehicleRange(string range)
    {
        var result = new List<int>();
        var seen = new HashSet<int>();

        if (string.IsNullOrWhiteSpace(range))
        {
            _logger.LogWarning("ParseVehicleRange: Pusty zakres pojazdów");
            return result;
        }

        var parts = range.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
        _logger.LogDebug("ParseVehicleRange: Parsowanie {Count} części", parts.Length);

        foreach (var part in parts)
        {
            var trimmed = part.Trim();

            if (trimmed.Contains("-"))
            {
                var rangeParts = trimmed.Split('-', StringSplitOptions.RemoveEmptyEntries);

                if (rangeParts.Length != 2)
                {
                    _logger.LogWarning("Nieprawidłowy zakres (zbyt wiele '-'): {Range}", trimmed);
                    continue;
                }

                if (!int.TryParse(rangeParts[0].Trim(), out int start) ||
                    !int.TryParse(rangeParts[1].Trim(), out int end))
                {
                    _logger.LogWarning("Nieprawidłowe wartości w zakresie: {Range}", trimmed);
                    continue;
                }

                // AUTO-SWAP: zakresy wsteczne
                if (start > end)
                {
                    _logger.LogInformation("Zakres wsteczny: {Original} → zamiana na {Start}-{End}",
                        trimmed, end, start);
                    (start, end) = (end, start);
                }

                // LIMIT DoS: max 10000 elementów w zakresie
                int rangeSize = end - start + 1;
                if (rangeSize > 10000)
                {
                    _logger.LogError("Zakres zbyt duży: {Range} ({Size} elementów). Max: 10000. Pominięto.",
                        trimmed, rangeSize);
                    continue;
                }

                // LIMIT globalny: sprawdź przed dodaniem
                if (result.Count + rangeSize > 10000)
                {
                    _logger.LogError("Przekroczenie limitu 10000 pojazdów. Obecna liczba: {Current}, próba dodania: {Additional}. Pominięto: {Range}",
                        result.Count, rangeSize, trimmed);
                    continue;
                }

                // Rozwiń zakres
                int addedCount = 0;
                for (int i = start; i <= end; i++)
                {
                    if (seen.Add(i))
                    {
                        result.Add(i);
                        addedCount++;
                    }
                }

                _logger.LogDebug("Zakres {Range} → dodano {Added} pojazdów", trimmed, addedCount);
            }
            else
            {
                // Pojedynczy numer
                if (int.TryParse(trimmed, out int num))
                {
                    if (result.Count >= 10000)
                    {
                        _logger.LogError("Przekroczenie limitu 10000 pojazdów. Pominięto: {Number}", num);
                        continue;
                    }

                    if (seen.Add(num))
                    {
                        result.Add(num);
                    }
                }
                else
                {
                    _logger.LogWarning("Nieprawidłowy numer pojazdu: {Number}", trimmed);
                }
            }
        }

        result.Sort();
        _logger.LogInformation("ParseVehicleRange: Sparsowano {Total} unikalnych pojazdów", result.Count);

        return result;
    }
}
