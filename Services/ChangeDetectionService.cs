using DB2ExportService.Configuration;
using Microsoft.Extensions.Logging;

namespace DB2ExportService.Services;

public class ChangeDetectionService
{
    private readonly ILogger<ChangeDetectionService> _logger;
    private readonly ConfigurationHelper _configHelper;
    private readonly string _logPath;

    public ChangeDetectionService(ILogger<ChangeDetectionService> logger, ConfigurationHelper configHelper)
    {
        _logger = logger;
        _configHelper = configHelper;
        var config = _configHelper.GetExportConfig();
        _logPath = config.LogPath;

        // Upewnij się, że katalog LOG istnieje
        if (!Directory.Exists(_logPath))
        {
            Directory.CreateDirectory(_logPath);
            _logger.LogInformation("Utworzono katalog dla logów: {LogPath}", _logPath);
        }
    }

    public async Task<bool> ShouldExportAsync(DateTime targetDate, int? currentCount, string filePrefix = "r_count")
    {
        if (!currentCount.HasValue)
        {
            _logger.LogWarning("Brak rekordów dla dnia {Date}, pomijam eksport", targetDate.ToString("yyyy-MM-dd"));
            return false;
        }

        var filePath = GetCountFilePath(targetDate, filePrefix);
        var previousCount = await ReadPreviousCountAsync(filePath);

        if (previousCount == null || currentCount.Value != previousCount.Value)
        {
            _logger.LogInformation(
                "Liczba rekordów uległa zmianie (poprzednio: {Previous}, aktualnie: {Current}) lub plik nie istnieje. Eksport zostanie wykonany.",
                previousCount?.ToString() ?? "brak",
                currentCount.Value);

            await SaveCurrentCountAsync(filePath, currentCount.Value);
            return true;
        }

        _logger.LogInformation(
            "Liczba rekordów nie uległa zmianie ({Count}). Pomijam eksport.",
            currentCount.Value);
        return false;
    }

    private string GetCountFilePath(DateTime date, string prefix)
    {
        var dateStr = date.ToString("yyyy-MM-dd");
        return Path.Combine(_logPath, $"{prefix}_{dateStr}.txt");
    }

    private async Task<int?> ReadPreviousCountAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                _logger.LogDebug("Plik licznika nie istnieje: {FilePath}", filePath);
                return null;
            }

            var content = await File.ReadAllTextAsync(filePath);
            if (int.TryParse(content.Trim(), out int count))
            {
                _logger.LogDebug("Odczytano poprzednią liczbę rekordów z pliku: {Count}", count);
                return count;
            }

            _logger.LogWarning("Nie udało się sparsować zawartości pliku: {FilePath}", filePath);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas odczytu poprzedniej liczby rekordów z pliku: {FilePath}", filePath);
            return null;
        }
    }

    private async Task SaveCurrentCountAsync(string filePath, int count)
    {
        try
        {
            await File.WriteAllTextAsync(filePath, count.ToString());
            _logger.LogInformation("Zapisano aktualną liczbę rekordów ({Count}) do pliku: {FilePath}", count, filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas zapisu aktualnej liczby rekordów do pliku: {FilePath}", filePath);
            throw;
        }
    }
}
