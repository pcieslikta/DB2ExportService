using System;
using System.IO;
using System.Windows.Forms;
using DB2ExportService.Configuration;
using DB2ExportService.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace DB2ExportConfigurator
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // Konfiguracja Serilog - logowanie do pliku
            var logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            Directory.CreateDirectory(logDirectory);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.File(
                    Path.Combine(logDirectory, "configurator_.log"),
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                    retainedFileCountLimit: 7)
                .WriteTo.Console()
                .CreateLogger();

            try
            {
                Log.Information("========================================");
                Log.Information("DB2 Export Configurator - Uruchamianie");
                Log.Information("========================================");
                Log.Information("Katalog aplikacji: {BaseDirectory}", AppDomain.CurrentDomain.BaseDirectory);
                Log.Information("Katalog logów: {LogDirectory}", logDirectory);

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.SetHighDpiMode(HighDpiMode.SystemAware);

                // Inicjalizacja Dependency Injection
                var services = new ServiceCollection();
                ConfigureServices(services);

                var serviceProvider = services.BuildServiceProvider();

                // Uruchom formularz z DI
                var mainForm = serviceProvider.GetRequiredService<MainForm>();
                Application.Run(mainForm);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Aplikacja zakończyła się z błędem krytycznym");
                MessageBox.Show($"Błąd krytyczny aplikacji:\n\n{ex.Message}\n\nSprawdź logi w katalogu: {logDirectory}",
                    "Błąd krytyczny", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Log.Information("DB2 Export Configurator - Zamykanie");
                Log.CloseAndFlush();
            }
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            // Konfiguracja appsettings.json
            var possiblePaths = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "DB2Export", "appsettings.json"),
                @"C:\Services\DB2Export\appsettings.json",
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json")
            };

            string? configPath = null;
            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    configPath = path;
                    break;
                }
            }

            if (configPath == null)
            {
                MessageBox.Show(
                    "Nie znaleziono pliku appsettings.json w żadnej z lokalizacji:\n" +
                    string.Join("\n", possiblePaths),
                    "Błąd konfiguracji",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                Environment.Exit(1);
            }

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Path.GetDirectoryName(configPath) ?? Directory.GetCurrentDirectory())
                .AddJsonFile(Path.GetFileName(configPath), optional: false, reloadOnChange: true)
                .Build();

            services.AddSingleton<IConfiguration>(configuration);

            // Logging - dodaj Serilog
            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddSerilog(dispose: true);
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            // Configuration Helper
            services.AddSingleton<ConfigurationHelper>();

            // Services
            services.AddSingleton<ResiliencePolicyService>();
            services.AddSingleton<IDB2Service, DB2Service>();
            services.AddSingleton<ChangeDetectionService>();

            // MainForm jako Transient (utworzone przez DI)
            services.AddTransient<MainForm>();
        }
    }
}
