using DB2ExportService.Configuration;
using DB2ExportService.Services;
using Microsoft.Extensions.Primitives;
using Quartz;

namespace DB2ExportService;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private IScheduler? _scheduler;
    private IDisposable? _configChangeToken;

    public Worker(
        ILogger<Worker> logger,
        ISchedulerFactory schedulerFactory,
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        _logger = logger;
        _schedulerFactory = schedulerFactory;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("R&G Export Service uruchamia się...");

        try
        {
            // Pobierz konfigurację
            using var scope = _serviceProvider.CreateScope();
            var configHelper = scope.ServiceProvider.GetRequiredService<ConfigurationHelper>();
            var exportConfig = configHelper.GetExportConfig();

            _logger.LogInformation("Konfiguracja załadowana:");
            _logger.LogInformation("  - Ścieżka eksportu: {ExportPath}", exportConfig.ExportPath);
            _logger.LogInformation("  - Ścieżka logów: {LogPath}", exportConfig.LogPath);
            _logger.LogInformation("  - Harmonogram: {ScheduleTime}", exportConfig.ScheduleTime);
            _logger.LogInformation("  - Kod eksportu: {KodExportu}", exportConfig.KodExportu);

            // Utwórz scheduler
            _scheduler = await _schedulerFactory.GetScheduler(stoppingToken);

            // Zdefiniuj Job
            var job = JobBuilder.Create<ExportJob>()
                .WithIdentity("ExportJob", "DB2Export")
                .Build();

            // Parsuj czas z konfiguracji (format: "HH:mm")
            var scheduleTimeParts = exportConfig.ScheduleTime.Split(':');
            if (scheduleTimeParts.Length != 2 ||
                !int.TryParse(scheduleTimeParts[0], out int hour) ||
                !int.TryParse(scheduleTimeParts[1], out int minute))
            {
                throw new InvalidOperationException($"Nieprawidłowy format czasu harmonogramu: {exportConfig.ScheduleTime}. Oczekiwany format: HH:mm");
            }

            // Zdefiniuj Trigger - codziennie o określonej godzinie
            var trigger = TriggerBuilder.Create()
                .WithIdentity("DailyTrigger", "DB2Export")
                .WithCronSchedule($"0 {minute} {hour} ? * *") // Cron: sekunda minuta godzina dzień miesiąc dzieńTygodnia
                .Build();

            // Zaplanuj Job
            await _scheduler.ScheduleJob(job, trigger, stoppingToken);

            _logger.LogInformation("Zaplanowano eksport codziennie o {Hour:D2}:{Minute:D2}", hour, minute);

            // Setup periodic monitoring (jeśli włączone)
            if (exportConfig.EnablePeriodicMonitoring)
            {
                var monitoringJob = JobBuilder.Create<MonitoringJob>()
                    .WithIdentity("MonitoringJob", "DB2Export")
                    .Build();

                var monitoringTrigger = TriggerBuilder.Create()
                    .WithIdentity("PeriodicMonitoringTrigger", "DB2Export")
                    .WithSimpleSchedule(x => x
                        .WithIntervalInMinutes(exportConfig.MonitoringIntervalMinutes)
                        .RepeatForever())
                    .Build();

                await _scheduler.ScheduleJob(monitoringJob, monitoringTrigger, stoppingToken);

                _logger.LogInformation("Zaplanowano periodic monitoring co {Minutes} minut (sprawdzanie ostatnich {Days} dni)",
                    exportConfig.MonitoringIntervalMinutes, exportConfig.MonitoringDaysBack);
            }
            else
            {
                _logger.LogInformation("Periodic monitoring wyłączony");
            }

            // Uruchom scheduler
            await _scheduler.Start(stoppingToken);

            _logger.LogInformation("R&G Export Service uruchomiony pomyślnie i oczekuje na harmonogram");

            // Monitorowanie zmian w konfiguracji
            _configChangeToken = ChangeToken.OnChange(
                () => _configuration.GetReloadToken(),
                async () => await OnConfigurationChanged());

            _logger.LogInformation("Monitorowanie zmian w pliku konfiguracyjnym włączone");

            // Opcjonalnie: Uruchom eksport natychmiast przy starcie (dla testów)
            // await _scheduler.TriggerJob(job.Key, stoppingToken);

            // Czekaj na zatrzymanie
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("R&G Export Service zatrzymuje się...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd krytyczny w R&G Export Service");
            throw;
        }
    }

    private async Task OnConfigurationChanged()
    {
        try
        {
            _logger.LogWarning("=== WYKRYTO ZMIANĘ W PLIKU KONFIGURACYJNYM ===");
            _logger.LogInformation("Przeładowywanie konfiguracji i przeplanowywanie harmonogramu...");

            using var scope = _serviceProvider.CreateScope();
            var configHelper = scope.ServiceProvider.GetRequiredService<ConfigurationHelper>();
            var exportConfig = configHelper.GetExportConfig();

            _logger.LogInformation("Nowa konfiguracja:");
            _logger.LogInformation("  - Harmonogram: {ScheduleTime}", exportConfig.ScheduleTime);
            _logger.LogInformation("  - DaysBack: {DaysBack}", exportConfig.DaysBack);
            _logger.LogInformation("  - EnabledExportTypes: {Types}", string.Join(", ", exportConfig.EnabledExportTypes));
            _logger.LogInformation("  - EnablePeriodicMonitoring: {Enabled}", exportConfig.EnablePeriodicMonitoring);
            _logger.LogInformation("  - MonitoringIntervalMinutes: {Interval}", exportConfig.MonitoringIntervalMinutes);

            if (_scheduler != null && !_scheduler.IsShutdown)
            {
                // Usuń stare joby
                await _scheduler.DeleteJob(new JobKey("ExportJob", "DB2Export"));
                await _scheduler.DeleteJob(new JobKey("MonitoringJob", "DB2Export"));
                _logger.LogInformation("Usunięto stare harmonogramy");

                // Dodaj nowe joby z nową konfiguracją
                var job = JobBuilder.Create<ExportJob>()
                    .WithIdentity("ExportJob", "DB2Export")
                    .Build();

                var scheduleTimeParts = exportConfig.ScheduleTime.Split(':');
                if (scheduleTimeParts.Length == 2 &&
                    int.TryParse(scheduleTimeParts[0], out int hour) &&
                    int.TryParse(scheduleTimeParts[1], out int minute))
                {
                    var trigger = TriggerBuilder.Create()
                        .WithIdentity("DailyTrigger", "DB2Export")
                        .WithCronSchedule($"0 {minute} {hour} ? * *")
                        .Build();

                    await _scheduler.ScheduleJob(job, trigger);
                    _logger.LogInformation("✓ Przeplanowano eksport dzienny na {Hour:D2}:{Minute:D2}", hour, minute);
                }

                // Periodic monitoring
                if (exportConfig.EnablePeriodicMonitoring)
                {
                    var monitoringJob = JobBuilder.Create<MonitoringJob>()
                        .WithIdentity("MonitoringJob", "DB2Export")
                        .Build();

                    var monitoringTrigger = TriggerBuilder.Create()
                        .WithIdentity("PeriodicMonitoringTrigger", "DB2Export")
                        .WithSimpleSchedule(x => x
                            .WithIntervalInMinutes(exportConfig.MonitoringIntervalMinutes)
                            .RepeatForever())
                        .Build();

                    await _scheduler.ScheduleJob(monitoringJob, monitoringTrigger);
                    _logger.LogInformation("✓ Przeplanowano periodic monitoring co {Minutes} minut", exportConfig.MonitoringIntervalMinutes);
                }
                else
                {
                    _logger.LogInformation("✓ Periodic monitoring wyłączony");
                }

                _logger.LogWarning("=== KONFIGURACJA PRZEŁADOWANA POMYŚLNIE ===");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas przeładowywania konfiguracji");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Zatrzymywanie R&G Export Service...");

        // Zatrzymaj monitorowanie konfiguracji
        _configChangeToken?.Dispose();

        if (_scheduler != null && !_scheduler.IsShutdown)
        {
            await _scheduler.Shutdown(cancellationToken);
        }

        await base.StopAsync(cancellationToken);
        _logger.LogInformation("R&G Export Service zatrzymany");
    }
}

// Job wykonywany przez Quartz
public class ExportJob : IJob
{
    private readonly ILogger<ExportJob> _logger;
    private readonly ExportService _exportService;

    public ExportJob(ILogger<ExportJob> logger, ExportService exportService)
    {
        _logger = logger;
        _exportService = exportService;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("Rozpoczęcie zaplanowanego eksportu o {Time}", DateTime.Now);

        try
        {
            await _exportService.RunScheduledExportAsync();
            _logger.LogInformation("Zaplanowany eksport zakończony pomyślnie");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas wykonywania zaplanowanego eksportu");
            // Nie rzucaj wyjątku - pozwól na kontynuację schedulingu
        }
    }
}

// Job dla periodic monitoring
public class MonitoringJob : IJob
{
    private readonly ILogger<MonitoringJob> _logger;
    private readonly ExportService _exportService;

    public MonitoringJob(ILogger<MonitoringJob> logger, ExportService exportService)
    {
        _logger = logger;
        _exportService = exportService;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("Rozpoczęcie periodic monitoring o {Time}", DateTime.Now);

        try
        {
            await _exportService.RunPeriodicMonitoringAsync();
            _logger.LogInformation("Periodic monitoring zakończony");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas periodic monitoring");
            // Nie rzucaj wyjątku - pozwól na kontynuację schedulingu
        }
    }
}
