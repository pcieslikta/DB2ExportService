using System.Text.Json;
using DB2ExportService.Configuration;
using DB2ExportService.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DB2ExportService.Services;

/// <summary>
/// Serwis monitorujący folder triggerów JSON dla ręcznego eksportu
/// </summary>
public class TriggerFileWatcherService : BackgroundService
{
    private readonly ILogger<TriggerFileWatcherService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly ConfigurationHelper _configHelper;
    private FileSystemWatcher? _fileWatcher;

    public TriggerFileWatcherService(
        ILogger<TriggerFileWatcherService> logger,
        IServiceProvider serviceProvider,
        ConfigurationHelper configHelper)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _configHelper = configHelper;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var config = _configHelper.GetExportConfig();
            var triggerPath = config.TriggerFolderPath;

            // Utwórz folder jeśli nie istnieje
            if (!Directory.Exists(triggerPath))
            {
                Directory.CreateDirectory(triggerPath);
                _logger.LogInformation("Utworzono folder triggerów: {Path}", triggerPath);
            }

            // Utwórz folder processed
            var processedPath = Path.Combine(triggerPath, "processed");
            if (!Directory.Exists(processedPath))
            {
                Directory.CreateDirectory(processedPath);
            }

            // Setup FileSystemWatcher
            _fileWatcher = new FileSystemWatcher(triggerPath)
            {
                Filter = "*.json",
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime,
                IncludeSubdirectories = false
            };

            _fileWatcher.Created += async (s, e) => await OnTriggerFileCreatedAsync(e.FullPath, stoppingToken);
            _fileWatcher.EnableRaisingEvents = true;

            _logger.LogInformation("Trigger File Watcher uruchomiony: {Path}", triggerPath);
            _logger.LogInformation("Monitorowanie plików *.json - wrzuć plik JSON aby wywołać ręczny eksport");

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas uruchamiania Trigger File Watcher");
            throw;
        }
    }

    private async Task OnTriggerFileCreatedAsync(string filePath, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Wykryto plik triggera: {File}", Path.GetFileName(filePath));

            // Poczekaj chwilę, aby plik był całkowicie zapisany
            await Task.Delay(500, cancellationToken);

            // Odczytaj i parsuj JSON
            var jsonContent = await File.ReadAllTextAsync(filePath, cancellationToken);
            var request = JsonSerializer.Deserialize<ManualExportRequest>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (request == null || request.ExportTypes == null || !request.ExportTypes.Any())
            {
                _logger.LogWarning("Nieprawidłowy format pliku triggera lub brak typów eksportu: {File}", Path.GetFileName(filePath));
                MoveToProcessed(filePath, "invalid");
                return;
            }

            // Sprawdź scheduled time
            if (!string.IsNullOrEmpty(request.ScheduledTime))
            {
                _logger.LogInformation("Eksport zaplanowany na: {Time} - obecnie nie obsługiwane, wykonuję natychmiast", request.ScheduledTime);
                // TODO: Schedule delayed execution - do implementacji w przyszłości
            }

            // Wykonaj eksport natychmiast
            using var scope = _serviceProvider.CreateScope();
            var exportService = scope.ServiceProvider.GetRequiredService<ExportService>();

            _logger.LogInformation("Rozpoczęcie eksportu z triggera: {File}", Path.GetFileName(filePath));
            await exportService.RunManualExportAsync(request);

            // Przenieś plik do archiwum
            MoveToProcessed(filePath, "success");

            _logger.LogInformation("Plik triggera przetworzony pomyślnie: {File}", Path.GetFileName(filePath));
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(jsonEx, "Błąd parsowania JSON pliku triggera: {File}", Path.GetFileName(filePath));
            MoveToProcessed(filePath, "error");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd przetwarzania pliku triggera: {File}", Path.GetFileName(filePath));
            MoveToProcessed(filePath, "error");
        }
    }

    private void MoveToProcessed(string filePath, string status)
    {
        try
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var extension = Path.GetExtension(filePath);
            var directory = Path.GetDirectoryName(filePath)!;
            var processedPath = Path.Combine(directory, "processed");

            var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            var archiveFileName = $"{fileName}_{timestamp}_{status}{extension}";
            var archiveFile = Path.Combine(processedPath, archiveFileName);

            File.Move(filePath, archiveFile);
            _logger.LogDebug("Plik przeniesiony do: {Archive}", archiveFileName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Nie udało się przenieść pliku triggera do processed: {File}", Path.GetFileName(filePath));
        }
    }

    public override void Dispose()
    {
        if (_fileWatcher != null)
        {
            _fileWatcher.EnableRaisingEvents = false;
            _fileWatcher.Dispose();
        }

        base.Dispose();
    }
}
