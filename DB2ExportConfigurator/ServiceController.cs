using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DB2ExportConfigurator
{
    public class ServiceController
    {
        private const string SERVICE_NAME = "RGExportService";
        private static readonly string EXECUTABLE_PATH = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DB2ExportService.exe");

        public enum ServiceStatus
        {
            Running,
            Stopped,
            Starting,
            Stopping,
            NotInstalled,
            Unknown
        }

        public static ServiceStatus GetServiceStatus()
        {
            try
            {
                // Najpierw sprawdź czy usługa w ogóle istnieje w systemie
                var services = System.ServiceProcess.ServiceController.GetServices();
                var serviceExists = services.Any(s => s.ServiceName.Equals(SERVICE_NAME, StringComparison.OrdinalIgnoreCase));


                if (!serviceExists)
                {
                    return ServiceStatus.NotInstalled;
                }

                using var service = new System.ServiceProcess.ServiceController(SERVICE_NAME);
                var status = service.Status;


                return status switch
                {
                    System.ServiceProcess.ServiceControllerStatus.Running => ServiceStatus.Running,
                    System.ServiceProcess.ServiceControllerStatus.Stopped => ServiceStatus.Stopped,
                    System.ServiceProcess.ServiceControllerStatus.StartPending => ServiceStatus.Starting,
                    System.ServiceProcess.ServiceControllerStatus.StopPending => ServiceStatus.Stopping,
                    _ => ServiceStatus.Unknown
                };
            }
            catch (InvalidOperationException)
            {
                return ServiceStatus.NotInstalled;
            }
            catch (System.ComponentModel.Win32Exception)
            {
                // Prawdopodobnie brak uprawnień
                return ServiceStatus.Unknown;
            }
            catch
            {
                return ServiceStatus.Unknown;
            }
        }

        public static bool IsServiceInstalled()
        {
            try
            {
                var services = System.ServiceProcess.ServiceController.GetServices();
                return services.Any(s => s.ServiceName.Equals(SERVICE_NAME, StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                return false;
            }
        }

        public static void ShowServiceDiagnostics()
        {
            try
            {
                var services = System.ServiceProcess.ServiceController.GetServices();

                // Szukaj dokładnej nazwy usługi
                var exactMatch = services.FirstOrDefault(s => s.ServiceName.Equals(SERVICE_NAME, StringComparison.OrdinalIgnoreCase));

                var db2Services = services.Where(s => s.ServiceName.ToLower().Contains("db2") ||
                                                     s.DisplayName.ToLower().Contains("db2") ||
                                                     s.ServiceName.ToLower().Contains("export") ||
                                                     s.DisplayName.ToLower().Contains("export"))
                                        .Select(s => $"📋 {s.ServiceName} ({s.DisplayName}) - Status: {s.Status}")
                                        .ToArray();

                var message = $"🔍 DIAGNOSTYKA USŁUG\n";
                message += $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n\n";
                message += $"🎯 Poszukiwana usługa: '{SERVICE_NAME}'\n\n";

                if (exactMatch != null)
                {
                    message += $"✅ ZNALEZIONO DOKŁADNE DOPASOWANIE!\n";
                    message += $"📋 Nazwa: {exactMatch.ServiceName}\n";
                    message += $"📄 Opis: {exactMatch.DisplayName}\n";
                    message += $"🔧 Status: {exactMatch.Status}\n";
                    message += $"🏠 Typ: {exactMatch.ServiceType}\n\n";

                    try
                    {
                        message += $"🚀 Czy można uruchomić: {(exactMatch.Status == System.ServiceProcess.ServiceControllerStatus.Stopped ? "TAK" : "NIE (już działa)")}\n";
                        message += $"⏹️ Czy można zatrzymać: {(exactMatch.Status == System.ServiceProcess.ServiceControllerStatus.Running ? "TAK" : "NIE")}\n\n";
                    }
                    catch (Exception ex)
                    {
                        message += $"⚠️ Błąd sprawdzania statusu: {ex.Message}\n\n";
                    }
                }
                else
                {
                    message += $"❌ NIE ZNALEZIONO USŁUGI '{SERVICE_NAME}'!\n\n";
                }

                if (db2Services.Any())
                {
                    message += $"🔎 Podobne usługi (zawierające 'DB2' lub 'Export'):\n";
                    message += string.Join("\n", db2Services) + "\n\n";
                }
                else
                {
                    message += "❌ Brak usług zawierających 'DB2' lub 'Export'\n\n";
                }

                message += $"📊 Wszystkich usług w systemie: {services.Length}\n";
                message += $"🔧 Ścieżka do exe: {EXECUTABLE_PATH}\n";
                message += $"📁 Plik istnieje: {(File.Exists(EXECUTABLE_PATH) ? "✅ TAK" : "❌ NIE")}\n\n";

                // Pokazuj pierwsze kilka usług do weryfikacji
                var someServices = services.Take(5).Select(s => s.ServiceName).ToArray();
                message += $"🔍 Przykłady usług w systemie:\n{string.Join(", ", someServices)}...\n";

                MessageBox.Show(message, "🔍 Diagnostyka Usług", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Błąd diagnostyki: {ex.GetType().Name}\n{ex.Message}\n\nStack trace:\n{ex.StackTrace}",
                    "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static async Task<bool> StartServiceAsync()
        {
            try
            {
                // Sprawdź czy wymagane pliki istnieją
                if (!File.Exists(EXECUTABLE_PATH))
                {
                    MessageBox.Show($"Nie można znaleźć pliku wykonywalnego usługi:\n{EXECUTABLE_PATH}\n\nUpewnij się, że usługa została prawidłowo zainstalowana.",
                        "Brak pliku usługi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                var configPath = Path.Combine(Path.GetDirectoryName(EXECUTABLE_PATH) ?? "", "appsettings.json");
                if (!File.Exists(configPath))
                {
                    MessageBox.Show($"Nie można znaleźć pliku konfiguracyjnego:\n{configPath}\n\nUpewnij się, że plik konfiguracyjny istnieje.",
                        "Brak konfiguracji", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                using var service = new System.ServiceProcess.ServiceController(SERVICE_NAME);

                // Sprawdź aktualny status
                var currentStatus = service.Status;

                if (currentStatus == System.ServiceProcess.ServiceControllerStatus.Stopped)
                {

                    service.Start();
                    await WaitForStatusAsync(service, System.ServiceProcess.ServiceControllerStatus.Running, TimeSpan.FromSeconds(60));

                    // Sprawdź czy udało się uruchomić
                    service.Refresh();
                    var newStatus = service.Status;

                    return newStatus == System.ServiceProcess.ServiceControllerStatus.Running;
                }
                else if (currentStatus == System.ServiceProcess.ServiceControllerStatus.Running)
                {
                    MessageBox.Show("Usługa jest już uruchomiona!", "Informacja",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return true;
                }
                else
                {
                    MessageBox.Show($"Usługa ma nieoczekiwany status: {currentStatus}", "Ostrzeżenie",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show($"Usługa '{SERVICE_NAME}' nie została znaleziona w systemie!\n\nSzczegóły błędu: {ex.Message}\n\nMożliwe przyczyny:\n- Usługa nie jest zainstalowana\n- Nieprawidłowa nazwa usługi\n- Brak uprawnień administratora",
                    "Usługa nie znaleziona", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                string errorMessage = ex.NativeErrorCode switch
                {
                    1053 => "Usługa nie odpowiada na sygnał uruchomienia w oczekiwanym czasie.\n\nMożliwe przyczyny:\n- Usługa ma błąd w kodzie i nie może się uruchomić\n- Brak pliku konfiguracyjnego appsettings.json\n- Nieprawidłowe uprawnienia do plików\n- Błąd zależności (brakujące DLL)",
                    5 => "Odmowa dostępu. Uruchom aplikację jako administrator.",
                    2 => "Nie można znaleźć pliku wykonywalnego usługi.",
                    _ => $"Błąd systemu Windows podczas uruchamiania usługi:\n{ex.Message}\n\nKod błędu: {ex.NativeErrorCode}"
                };

                MessageBox.Show($"{errorMessage}\n\nSprawdź czy:\n- Masz uprawnienia administratora\n- Plik wykonywalny usługi istnieje: {EXECUTABLE_PATH}\n- Plik konfiguracyjny istnieje\n- Wszystkie zależności są dostępne",
                    "Błąd uruchamiania usługi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Nieoczekiwany błąd uruchamiania usługi:\n{ex.GetType().Name}: {ex.Message}\n\nStack trace:\n{ex.StackTrace}",
                    "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        public static async Task<bool> StopServiceAsync()
        {
            try
            {
                using var service = new System.ServiceProcess.ServiceController(SERVICE_NAME);

                var currentStatus = service.Status;

                if (currentStatus == System.ServiceProcess.ServiceControllerStatus.Running)
                {

                    service.Stop();
                    await WaitForStatusAsync(service, System.ServiceProcess.ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(60));

                    service.Refresh();
                    var newStatus = service.Status;

                    return newStatus == System.ServiceProcess.ServiceControllerStatus.Stopped;
                }
                else if (currentStatus == System.ServiceProcess.ServiceControllerStatus.Stopped)
                {
                    MessageBox.Show("Usługa jest już zatrzymana!", "Informacja",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return true;
                }
                else
                {
                    MessageBox.Show($"Nie można zatrzymać usługi - aktualny status: {currentStatus}", "Ostrzeżenie",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show($"Usługa '{SERVICE_NAME}' nie została znaleziona w systemie!\n\nSzczegóły błędu: {ex.Message}",
                    "Usługa nie znaleziona", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                MessageBox.Show($"Błąd systemu Windows podczas zatrzymywania usługi:\n{ex.Message}\n\nKod błędu: {ex.NativeErrorCode}",
                    "Błąd systemu", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Nieoczekiwany błąd zatrzymywania usługi:\n{ex.GetType().Name}: {ex.Message}",
                    "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        public static async Task<bool> RestartServiceAsync()
        {
            return await StopServiceAsync() && await StartServiceAsync();
        }

        public static bool InstallService()
        {
            try
            {
                // Sprawdź czy plik wykonywalny istnieje
                var fullPath = Path.GetFullPath(EXECUTABLE_PATH);
                if (!File.Exists(fullPath))
                {
                    MessageBox.Show($"Nie można znaleźć pliku wykonywalnego usługi:\n{fullPath}\n\nUpewnij się, że aplikacja znajduje się w odpowiednim katalogu.",
                        "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = "sc.exe",
                    Arguments = $"create {SERVICE_NAME} binpath= \"{fullPath}\" start= auto",
                    UseShellExecute = true,
                    Verb = "runas", // Uruchom jako administrator
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                using var process = Process.Start(startInfo);
                process?.WaitForExit();

                if (process?.ExitCode != 0)
                {
                    MessageBox.Show($"Instalacja usługi nie powiodła się.\nKod błędu: {process?.ExitCode}\n\nSprawdź czy:\n- Uruchamiasz jako administrator\n- Usługa nie jest już zainstalowana",
                        "Błąd instalacji", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd instalacji serwisu: {ex.Message}", "Błąd",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        public static bool UninstallService()
        {
            try
            {
                // Najpierw zatrzymaj serwis
                StopServiceAsync().Wait();

                var startInfo = new ProcessStartInfo
                {
                    FileName = "sc.exe",
                    Arguments = $"delete {SERVICE_NAME}",
                    UseShellExecute = true,
                    Verb = "runas", // Uruchom jako administrator
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                using var process = Process.Start(startInfo);
                process?.WaitForExit();
                return process?.ExitCode == 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd odinstalowania serwisu: {ex.Message}", "Błąd",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        public static bool RunAsConsole()
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = EXECUTABLE_PATH,
                    Arguments = "--console",
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Normal
                };

                Process.Start(startInfo);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd uruchamiania w trybie konsoli: {ex.Message}", "Błąd",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private static async Task WaitForStatusAsync(System.ServiceProcess.ServiceController service, System.ServiceProcess.ServiceControllerStatus expectedStatus, TimeSpan timeout)
        {
            var stopwatch = Stopwatch.StartNew();
            while (service.Status != expectedStatus && stopwatch.Elapsed < timeout)
            {
                await Task.Delay(500);
                service.Refresh();
            }
        }

        public static string GetServiceStatusText()
        {
            return GetServiceStatus() switch
            {
                ServiceStatus.Running => "🟢 Uruchomiony",
                ServiceStatus.Stopped => "🔴 Zatrzymany",
                ServiceStatus.Starting => "🟡 Uruchamianie...",
                ServiceStatus.Stopping => "🟡 Zatrzymywanie...",
                ServiceStatus.NotInstalled => "❌ Nie zainstalowany",
                _ => "❓ Nieznany"
            };
        }
    }
}
