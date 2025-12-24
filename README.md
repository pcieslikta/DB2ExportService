# R&G DB2 Export Service

Automatyczny eksport danych z bazy DB2 (RAPJAZDY) do plików CSV - implementacja w .NET 8 jako Windows Service.

## 📋 Spis treści

- [Funkcjonalność](#funkcjonalność)
- [Konfigurator GUI](#konfigurator-gui)
- [Wymagania](#wymagania)
- [Instalacja](#instalacja)
- [Konfiguracja](#konfiguracja)
- [Uruchamianie](#uruchamianie)
- [Architektura](#architektura)
- [Troubleshooting](#troubleshooting)

---

## 🎯 Funkcjonalność

### Eksport danych:
- **BRAMKI_*.csv** - podstawowe dane o przejazdach (przystawki, bramki, pasażerowie)
- **BRAMKID_*.csv** - szczegółowe dane z podziałem na 4 drzwi (tylko dla kod_exportu = "SOSNO")

### Cechy:
- ✅ Automatyczne uruchamianie według harmonogramu (domyślnie: 13:15)
- ✅ Sprawdzanie zmian liczby rekordów przed eksportem (optymalizacja)
- ✅ Bezpieczne przechowywanie credentials (Windows Credential Manager)
- ✅ Pełne logowanie do plików (30 dni retencji)
- ✅ Auto-restart przy błędach (Windows Service Recovery)
- ✅ Kodowanie CP1250 dla polskich znaków
- ✅ Parametryzowane zapytania SQL (bez SQL injection)
- ✅ **Graficzny konfigurator** - łatwa konfiguracja przez GUI

---

## 🖥️ Konfigurator GUI

### **DB2 Export Configurator** - graficzna aplikacja do zarządzania konfiguracją

![Konfigurator](https://img.shields.io/badge/GUI-Windows_Forms-blue)

**Uruchamianie:**
```bash
Scripts\run-configurator.bat
```

**Funkcje:**
- 🗄️ **Konfiguracja DB2** - połączenie, credentials, Credential Manager
- 📊 **Ustawienia eksportu** - ścieżki, harmonogram, dni wstecz
- 🚌 **Zarządzanie pojazdami** - tryb lista/zakres, edycja list
- ⚙️ **Sterowanie serwisem** - start/stop/restart, podgląd statusu
- 📄 **Dostęp do logów** - bezpośrednie otwarcie katalogu logów

**Wymagania:**
- Uprawnienia administratora (do zarządzania serwisem)
- .NET 8.0 Runtime

Więcej informacji: [DB2ExportConfigurator/README.md](DB2ExportConfigurator/README.md)

---

## 📦 Wymagania

### System:
- Windows 10/11 lub Windows Server 2016+
- .NET 8.0 Runtime (zawarte w publish - self-contained)
- Uprawnienia administratora (do instalacji serwisu)

### Baza danych:
- IBM DB2 (PROD lub TRPK)
- Sterownik IBM DB2 Client zainstalowany (`C:\PROGRA~1\IBM\SQLLIB\BIN`)

### Wymagane katalogi:
- `C:\EXPORT\` - katalog eksportu CSV
- `C:\EXPORT\LOG\` - katalog logów
- `C:\Services\DB2Export\` - katalog serwisu (tworzony automatycznie)

---

## 🚀 Instalacja

### Krok 1: Build projektu

```bash
cd C:\EXPORT\CSv\DB2ExportService
Scripts\build.bat
```

To utworzy katalog `publish\` z wszystkimi plikami.

### Krok 2: Konfiguracja credentials

**WAŻNE:** Przed instalacją skonfiguruj hasła w Windows Credential Manager:

```bash
Scripts\setup-credentials.bat
```

Lub ręcznie:
```bash
cmdkey /add:DB2Export_PROD /user:dbtaran1 /pass:TwojeHaslo
```

### Krok 3: Instalacja serwisu

```bash
Scripts\install.bat
```

Skrypt:
- Zatrzyma i usunie stary serwis (jeśli istnieje)
- Skopiuje pliki do `C:\Services\DB2Export\`
- Zainstaluje nowy serwis Windows
- Skonfiguruje auto-restart przy błędach

---

## ⚙️ Konfiguracja

### Plik: `C:\Services\DB2Export\appsettings.json`

```json
{
  "ExportConfig": {
    "KodExportu": "SOSNO",          // Kod eksportu (SOSNO = oba raporty)
    "ExportPath": "C:\\EXPORT\\",   // Ścieżka eksportu CSV
    "LogPath": "C:\\EXPORT\\LOG\\", // Ścieżka logów
    "ScheduleTime": "13:15",        // Godzina uruchamiania (HH:mm)
    "DaysBack": -2                   // Zakres dni wstecz
  },
  "VehicleConfig": {
    "KodExportu": "SOSNO",
    "PojazdyMode": "lista",         // "lista" lub "zakres"
    "PojazdyStart": 2209,           // (dla trybu "zakres")
    "PojazdyEnd": 2238,             // (dla trybu "zakres")
    "PojazdyLista": [598, 599, ...] // (dla trybu "lista")
  },
  "DB2": {
    "Database": "PROD",
    "Hostname": "192.168.10.136",
    "Port": 50000,
    "Protocol": "TCPIP",
    "User": "",                      // Puste - użyj Credential Manager
    "Password": "",                  // Puste - użyj Credential Manager
    "UseCredentialManager": true,
    "CredentialKey": "DB2Export_PROD",
    "CCSID": 1250                    // Kodowanie polskich znaków
  }
}
```

**Zmiana harmonogramu:**
Edytuj `"ScheduleTime": "13:15"` i zrestartuj serwis.

**Zmiana zakresu pojazdów:**
- **Tryb lista:** Edytuj `"PojazdyLista"` i ustaw `"PojazdyMode": "lista"`
- **Tryb zakres:** Ustaw `"PojazdyStart"` i `"PojazdyEnd"`, oraz `"PojazdyMode": "zakres"`

---

## 🎮 Uruchamianie

### Start serwisu:
```bash
Scripts\start.bat
# lub
net start RGExportService
```

### Stop serwisu:
```bash
Scripts\stop.bat
# lub
net stop RGExportService
```

### Status serwisu:
```bash
sc query RGExportService
```

### Logi:
```bash
# Logi serwisu
type C:\EXPORT\LOG\export_service_*.log

# Ostatnie 50 linii
powershell Get-Content C:\EXPORT\LOG\export_service_*.log -Tail 50
```

### Ręczne uruchomienie (bez serwisu):
```bash
cd C:\Services\DB2Export
DB2ExportService.exe
```

---

## 🏗️ Architektura

### Struktura projektu:

```
DB2ExportService/
├── Program.cs                      # Entry point + DI setup
├── Worker.cs                       # Background service + Quartz scheduling
├── Models/
│   └── ExportConfig.cs            # Modele danych i konfiguracji
├── Services/
│   ├── IDB2Service.cs             # Interface DB2
│   ├── DB2Service.cs              # Połączenie DB2 + zapytania
│   ├── ExportService.cs           # Generowanie CSV
│   └── ChangeDetectionService.cs  # Sprawdzanie zmian
├── Configuration/
│   └── ConfigurationHelper.cs     # Helper dla konfiguracji
├── Scripts/
│   ├── build.bat                  # Build projektu
│   ├── install.bat                # Instalacja serwisu
│   ├── uninstall.bat              # Deinstalacja
│   ├── start.bat                  # Uruchomienie
│   ├── stop.bat                   # Zatrzymanie
│   └── setup-credentials.bat      # Konfiguracja credentials
└── appsettings.json               # Konfiguracja
```

### Technologie:
- **.NET 8.0 Worker Service** - framework dla Windows Services
- **IBM.Data.DB2.Core** - sterownik DB2
- **Quartz.NET** - scheduling (cron jobs)
- **Serilog** - structured logging
- **CsvHelper** - generowanie CSV
- **CredentialManagement** - Windows Credential Manager

### Przepływ danych:

```
Worker (Scheduler)
    ↓
ExportService.RunExportAsync()
    ↓
ChangeDetectionService.ShouldExportAsync() → sprawdza zmiany
    ↓ (jeśli zmiany wykryte)
DB2Service.GetBramkiDataAsync() → pobiera dane z DB2
    ↓
ExportService.WriteCsvAsync() → zapisuje CSV (CP1250)
    ↓
Logi → C:\EXPORT\LOG\
```

---

## 🔧 Troubleshooting

### Serwis się nie uruchamia:

1. **Sprawdź logi:**
   ```bash
   type C:\EXPORT\LOG\export_service_*.log
   ```

2. **Sprawdź credentials:**
   ```bash
   cmdkey /list | findstr DB2Export
   ```

3. **Sprawdź uprawnienia:**
   - Serwis działa jako `Local System` (domyślnie)
   - Upewnij się, że ma dostęp do `C:\EXPORT\` i `C:\EXPORT\LOG\`

4. **Testuj ręcznie:**
   ```bash
   cd C:\Services\DB2Export
   DB2ExportService.exe
   ```

### Błędy połączenia z DB2:

1. **Sprawdź sterownik DB2:**
   ```bash
   dir "C:\PROGRA~1\IBM\SQLLIB\BIN\db2app64.dll"
   ```

2. **Test credentials:**
   - Uruchom `Scripts\setup-credentials.bat` ponownie
   - Sprawdź `appsettings.json`: `"UseCredentialManager": true`

3. **Sprawdź dostęp sieciowy:**
   ```bash
   ping 192.168.10.136
   telnet 192.168.10.136 50000
   ```

### Eksport się nie wykonuje:

1. **Sprawdź harmonogram w logach:**
   ```
   "Zaplanowano eksport codziennie o 13:15"
   ```

2. **Sprawdź liczniki rekordów:**
   ```bash
   dir C:\EXPORT\LOG\r_count_*.txt
   type C:\EXPORT\LOG\r_count_2023-12-23.txt
   ```

3. **Wymuś eksport (usuń liczniki):**
   ```bash
   del C:\EXPORT\LOG\r_count_*.txt
   ```

### Brak plików CSV:

1. **Sprawdź uprawnienia do zapisu:**
   ```bash
   icacls C:\EXPORT\
   ```

2. **Sprawdź logi błędów:**
   ```bash
   findstr /i "error" C:\EXPORT\LOG\export_service_*.log
   ```

---

## 📝 Deinstalacja

```bash
Scripts\uninstall.bat
```

To zatrzyma i usunie serwis. Opcjonalnie możesz usunąć pliki z `C:\Services\DB2Export\`.

---

## 🔄 Migracja z Python

### Główne zmiany:

| Python | C# |
|--------|-----|
| `export_service.py` (pywin32) | `Worker.cs` (.NET Worker Service) |
| `schedule` | Quartz.NET |
| `ibm_db` | IBM.Data.DB2.Core |
| Hardcoded credentials | Windows Credential Manager |
| `loguru` | Serilog |

### Konfiguracja:
- Python używał `export.json` + `db2_*.json`
- C# używa `appsettings.json` (wszystko w jednym miejscu)

### Instalacja:
- Python: ręczna instalacja przez `python export_service.py install`
- C#: automatyczna instalacja przez `Scripts\install.bat`

---

## 📞 Wsparcie

W razie problemów:
1. Sprawdź logi w `C:\EXPORT\LOG\`
2. Zobacz sekcję [Troubleshooting](#troubleshooting)
3. Testuj ręcznie bez serwisu

---

## 📄 Licencja

© 2024 R&G - Wewnętrzne użycie firmowe
