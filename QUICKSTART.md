# Quick Start Guide - 5 minut

## 🚀 Szybki start (dla zaawansowanych)

### 1. Build (30 sekund)
```bash
cd C:\EXPORT\CSv\DB2ExportService
Scripts\build.bat
```

### 2. Credentials (1 minuta)
```bash
Scripts\setup-credentials.bat
# Podaj: dbtaran1 / Akuc123#
```

### 3. Konfiguracja (1 minuta)
Edytuj `publish\appsettings.json`:
- Sprawdź `"ScheduleTime": "13:15"`
- Sprawdź `"PojazdyLista": [...]`
- Sprawdź `"Database": "PROD"`

### 4. Test (1 minuta)
```bash
cd publish
DB2ExportService.exe
# Ctrl+C po weryfikacji logów
```

### 5. Instalacja (1 minuta)
```bash
cd ..
Scripts\install.bat
Scripts\start.bat
```

### 6. Weryfikacja (1 minuta)
```bash
# Sprawdź logi
type C:\EXPORT\LOG\export_service_*.log

# Sprawdź status
sc query RGExportService

# Czekaj na harmonogram (13:15) lub wymuś eksport:
del C:\EXPORT\LOG\r_count_*.txt
# i zmodyfikuj ScheduleTime na najbliższą minutę
```

---

## ✅ Gotowe!

Serwis działa w tle i automatycznie eksportuje dane codziennie o 13:15.

**Przydatne komendy:**
```bash
# Status
sc query RGExportService

# Start/Stop
net start RGExportService
net stop RGExportService

# Logi (ostatnie 50 linii)
powershell Get-Content C:\EXPORT\LOG\export_service_*.log -Tail 50

# Logi na żywo
powershell Get-Content C:\EXPORT\LOG\export_service_*.log -Wait
```

---

## 🔧 Zmiana harmonogramu

1. Edytuj: `C:\Services\DB2Export\appsettings.json`
2. Zmień: `"ScheduleTime": "14:30"` (przykład)
3. Restart: `net stop RGExportService && net start RGExportService`

---

## 📁 Gdzie szukać plików

- **Serwis:** `C:\Services\DB2Export\`
- **Logi:** `C:\EXPORT\LOG\export_service_*.log`
- **CSV:** `C:\EXPORT\BRAMKI_*.csv`, `C:\EXPORT\BRAMKID_*.csv`
- **Liczniki:** `C:\EXPORT\LOG\r_count_*.txt`

---

**Problemy?** Zobacz [README.md](README.md) → sekcja Troubleshooting
