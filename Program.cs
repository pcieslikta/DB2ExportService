using DB2ExportService;
using DB2ExportService.Configuration;
using DB2ExportService.Services;
using Quartz;
using Serilog;
using Serilog.Events;

// Konfiguracja Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Quartz", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: @"C:\EXPORT\LOG\export_service_.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
        encoding: System.Text.Encoding.UTF8)
    .CreateLogger();

try
{
    Log.Information("=== R&G DB2 Export Service - Uruchamianie ===");

    var builder = Host.CreateApplicationBuilder(args);

    // Konfiguracja
    builder.Configuration
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
        .AddEnvironmentVariables();

    // Serilog
    builder.Services.AddSerilog();

    // Rejestracja serwisów
    builder.Services.AddSingleton<ConfigurationHelper>();
    builder.Services.AddSingleton<ResiliencePolicyService>();
    builder.Services.AddSingleton<IDB2Service, DB2Service>();
    builder.Services.AddSingleton<ChangeDetectionService>();
    builder.Services.AddSingleton<ExportService>();

    // Quartz.NET dla schedulingu
    builder.Services.AddQuartz();

    builder.Services.AddQuartzHostedService(options =>
    {
        options.WaitForJobsToComplete = true;
    });

    // Worker Service
    builder.Services.AddHostedService<Worker>();

    // Windows Service support
    builder.Services.AddWindowsService(options =>
    {
        options.ServiceName = "RGExportService";
    });

    var host = builder.Build();

    Log.Information("Host skonfigurowany, uruchamianie serwisu...");
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Aplikacja zakończyła się niepowodzeniem podczas uruchamiania");
    return 1;
}
finally
{
    Log.Information("=== R&G DB2 Export Service - Zamykanie ===");
    await Log.CloseAndFlushAsync();
}

return 0;
