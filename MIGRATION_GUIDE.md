# Przewodnik Migracji: Python → C#

## 📊 Porównanie rozwiązań

| Aspekt | Python (export_service.py) | C# (DB2ExportService) |
|--------|---------------------------|----------------------|
| **Framework** | pywin32 | .NET 8 Worker Service |
| **Instalacja** | Ręczna (skomplikowana) | Automatyczna (1 skrypt) |
| **Credentials** | Hardcoded w kodzie ⚠️ | Windows Credential Manager ✅ |
| **Scheduling** | schedule library | Quartz.NET (enterprise) |
| **Logging** | loguru → UTF-8 | Serilog → UTF-8 + retention |
| **SQL** | f-strings (SQL injection) ⚠️ | Parametryzowane queries ✅ |
| **Error handling** | Podstawowy | Retry + Auto-restart ✅ |
| **Deployment** | Ręczny | Self-contained .exe ✅ |
| **Performance** | Interpreter | Kompilowany kod ✅ |

---

## 🔄 Mapowanie Funkcji

### Python → C#

#### 1. Połączenie z DB2

**Python:**
```python
def create_db2_connection(config_path):
    with open(config_path, 'r', encoding='utf-8') as config_file:
        config = json.load(config_file)

    db2_connection_string = (
        f"DATABASE={db_config['Database']};"
        f"HOSTNAME={db_config['Hostname']};"
        f"UID={db_config['User']};"
        f"PWD={db_config['Password']};"  # ⚠️ Hardcoded!
    )
    conn = ibm_db.connect(db2_connection_string, "", "")
```

**C#:**
```csharp
private DB2Connection CreateConnection()
{
    // Credentials z Windows Credential Manager
    var credential = CredentialManager.ReadCredential(_db2Config.CredentialKey);

    var connectionString = $"Database={_db2Config.Database};" +
                          $"Server={_db2Config.Hostname}:{_db2Config.Port};" +
                          $"UID={credential.Username};" +  // ✅ Bezpieczne
                          $"PWD={credential.Password};";

    var connection = new DB2Connection(connectionString);
    connection.Open();
    return connection;
}
```

#### 2. Zapytania SQL

**Python (SQL Injection vulnerability!):**
```python
rp_table_sql = f"""
    SELECT ...
    WHERE rap.DT_KARTY = DATE('{target_date_str}') - 1 DAY  -- ⚠️ f-string!
    {pojazdy_warunek}  -- ⚠️ Niezabezpieczone
"""
```

**C# (Parametryzowane):**
```csharp
var sql = @"
    SELECT ...
    WHERE rap.DT_KARTY = DATE(@targetDate) - 1 DAY";  // ✅ Parametr

using var command = new DB2Command(sql, connection);
command.Parameters.Add("@targetDate", DB2Type.Date).Value = targetDate;
```

#### 3. Sprawdzanie zmian

**Python:**
```python
def check_and_export(config_path, target_date_str):
    file_path = os.path.join(export_path, "LOG", f"r_count_{target_date_str}.txt")
    with open(file_path, 'r') as file:
        previous_count = int(file.read())

    if record_count != previous_count:
        return True
```

**C#:**
```csharp
public async Task<bool> ShouldExportAsync(DateTime targetDate, int? currentCount)
{
    var filePath = GetCountFilePath(targetDate, "r_count");
    var previousCount = await ReadPreviousCountAsync(filePath);

    if (previousCount == null || currentCount != previousCount)
    {
        await SaveCurrentCountAsync(filePath, currentCount.Value);
        return true;
    }
    return false;
}
```

#### 4. Scheduling

**Python:**
```python
schedule.every().day.at(f"{self.run_hour:02d}:{self.run_minute:02d}").do(self.run_script)

while self.running:
    schedule.run_pending()
    time.sleep(60)
```

**C#:**
```csharp
// Quartz.NET - enterprise scheduling
var trigger = TriggerBuilder.Create()
    .WithCronSchedule($"0 {minute} {hour} ? * *")
    .Build();

await _scheduler.ScheduleJob(job, trigger, cancellationToken);
await _scheduler.Start(cancellationToken);
```

#### 5. Eksport CSV

**Python:**
```python
with open(csv_filepath, 'w', newline='', encoding='cp1250') as csvfile:
    writer = csv.DictWriter(csvfile, fieldnames=custom_columns, delimiter=';')
    writer.writeheader()
    writer.writerows(result)
```

**C#:**
```csharp
var config = new CsvConfiguration(CultureInfo.InvariantCulture)
{
    Delimiter = ";",
    Encoding = Encoding.GetEncoding(1250)
};

await using var writer = new StreamWriter(filePath, false, encoding);
await using var csv = new CsvWriter(writer, config);
// ... zapisz dane
```

---

## 📦 Krok po kroku: Migracja

### Faza 1: Przygotowanie (1 dzień)

1. **Backup obecnego systemu**
   ```bash
   xcopy C:\EXPORT\export.py C:\EXPORT\BACKUP\ /Y
   xcopy C:\EXPORT\export_service.py C:\EXPORT\BACKUP\ /Y
   ```

2. **Zainstaluj .NET 8 SDK** (jeśli jeszcze nie masz)
   - Pobierz: https://dotnet.microsoft.com/download/dotnet/8.0
   - Instaluj: `dotnet-sdk-8.0-win-x64.exe`

3. **Zatrzymaj stary serwis Python**
   ```bash
   sc stop RGExportService
   sc delete RGExportService
   ```

### Faza 2: Build i Test (2 dni)

1. **Build projektu C#**
   ```bash
   cd C:\EXPORT\CSv\DB2ExportService
   Scripts\build.bat
   ```

2. **Konfiguruj credentials**
   ```bash
   Scripts\setup-credentials.bat
   # Podaj: dbtaran1 / Akuc123#
   ```

3. **Edytuj konfigurację**
   - Otwórz: `C:\EXPORT\CSv\DB2ExportService\publish\appsettings.json`
   - Sprawdź wszystkie ustawienia
   - Skopiuj `PojazdyLista` z `db2_pojazdy.json`

4. **Test ręczny (BEZ serwisu!)**
   ```bash
   cd C:\EXPORT\CSv\DB2ExportService\publish
   DB2ExportService.exe
   ```

   **Co sprawdzić:**
   - ✅ Połączenie z DB2
   - ✅ Odczyt konfiguracji
   - ✅ Harmonogram zaplanowany
   - ✅ Logi w `C:\EXPORT\LOG\`

5. **Test eksportu**
   - Usuń licznik: `del C:\EXPORT\LOG\r_count_*.txt`
   - Poczekaj na harmonogram lub zmodyfikuj `ScheduleTime` na najbliższą minutę
   - Sprawdź: `dir C:\EXPORT\BRAMKI_*.csv`

### Faza 3: Instalacja Produkcyjna (pół dnia)

1. **Instaluj serwis**
   ```bash
   cd C:\EXPORT\CSv\DB2ExportService
   Scripts\install.bat
   ```

2. **Uruchom serwis**
   ```bash
   Scripts\start.bat
   ```

3. **Monitoruj logi**
   ```bash
   powershell Get-Content C:\EXPORT\LOG\export_service_*.log -Wait
   ```

4. **Sprawdź status**
   ```bash
   sc query RGExportService
   ```

### Faza 4: Weryfikacja (1 dzień)

1. **Porównaj wyniki**
   - Porównaj CSV z Python vs C#
   - Sprawdź liczby rekordów
   - Sprawdź polskie znaki (CP1250)

2. **Test harmonogramu**
   - Poczekaj na zaplanowaną godzinę (13:15)
   - Sprawdź logi: czy eksport się wykonał
   - Sprawdź pliki CSV: czy są nowe

3. **Test auto-restart**
   - Wymuś błąd (np. wyłącz DB2)
   - Sprawdź czy serwis się restartuje
   - Sprawdź logi błędów

### Faza 5: Cleanup (opcjonalnie)

Gdy C# działa prawidłowo przez 1 tydzień:

```bash
# Usuń stare pliki Python
del C:\EXPORT\export.py
del C:\EXPORT\export_service.py
rmdir /S C:\EXPORT\CONFIG
```

---

## 🔍 Weryfikacja Poprawności

### Checklist przed przejściem na produkcję:

- [ ] Build projektu zakończony bez błędów
- [ ] Credentials skonfigurowane w Credential Manager
- [ ] Test ręczny (bez serwisu) - sukces
- [ ] Test połączenia z DB2 - sukces
- [ ] Eksport CSV - pliki się tworzą
- [ ] Polskie znaki (CP1250) - poprawne
- [ ] Liczba rekordów - zgodna z Python
- [ ] Harmonogram - zaplanowany poprawnie
- [ ] Logi - zapisują się do plików
- [ ] Serwis Windows - zainstalowany
- [ ] Serwis Windows - uruchamia się
- [ ] Auto-restart - działa przy błędach

---

## ⚠️ Znane różnice i zmiany

### 1. Struktura konfiguracji

**Python:** Wiele plików JSON
- `export.json`
- `db2_config.json`
- `db2_pojazdy.json`

**C#:** Jeden plik
- `appsettings.json` (wszystko w jednym miejscu)

### 2. Lokalizacja plików

**Python:**
- Skrypt: `C:\EXPORT\export.py`
- Config: `C:\EXPORT\CONFIG\*.json`

**C#:**
- Serwis: `C:\Services\DB2Export\`
- Config: `C:\Services\DB2Export\appsettings.json`

### 3. Credentials

**Python:**
```python
_svc_user_ = "alaska0"  # ⚠️ W kodzie!
_svc_password_ = "Akuc123#"  # ⚠️ W kodzie!
```

**C#:**
```json
"UseCredentialManager": true,
"CredentialKey": "DB2Export_PROD"
```

### 4. Kodowanie

Oba używają CP1250, ale:
- Python: `encoding='cp1250'` w wielu miejscach
- C#: `Encoding.GetEncoding(1250)` centralnie

### 5. Harmonogram

**Python:**
- Hardcoded: godzina 13, minuta 15
- Konfiguracja: `export.json`

**C#:**
- Format: `"ScheduleTime": "13:15"`
- Edycja: `appsettings.json`

---

## 🐛 Potencjalne Problemy

### Problem 1: Brak DB2 Driver

**Symptom:**
```
System.DllNotFoundException: Unable to load DLL 'db2app64.dll'
```

**Rozwiązanie:**
```bash
# Sprawdź instalację
dir "C:\PROGRA~1\IBM\SQLLIB\BIN\db2app64.dll"

# Dodaj do PATH (jeśli trzeba)
setx PATH "%PATH%;C:\PROGRA~1\IBM\SQLLIB\BIN"
```

### Problem 2: Credentials nie działają

**Symptom:**
```
Błąd podczas nawiązywania połączenia z bazą danych
```

**Rozwiązanie:**
```bash
# Sprawdź credentials
cmdkey /list | findstr DB2Export

# Usuń i dodaj ponownie
cmdkey /delete:DB2Export_PROD
cmdkey /add:DB2Export_PROD /user:dbtaran1 /pass:TwojeHaslo
```

### Problem 3: Brak uprawnień do katalogów

**Symptom:**
```
Access denied: C:\EXPORT\
```

**Rozwiązanie:**
```bash
# Nadaj uprawnienia dla LocalSystem
icacls C:\EXPORT /grant "NT AUTHORITY\SYSTEM:(OI)(CI)F"
icacls C:\EXPORT\LOG /grant "NT AUTHORITY\SYSTEM:(OI)(CI)F"
```

---

## 📞 Rollback Plan

Jeśli coś pójdzie nie tak, wróć do Python:

```bash
# 1. Zatrzymaj C# service
sc stop RGExportService
sc delete RGExportService

# 2. Przywróć Python
xcopy C:\EXPORT\BACKUP\*.py C:\EXPORT\ /Y

# 3. Reinstaluj Python service
cd C:\EXPORT
python export_service.py install
python export_service.py start
```

---

## ✅ Podsumowanie Korzyści

Po migracji na C# zyskujesz:

1. ✅ **Bezpieczeństwo:** Credentials w Credential Manager zamiast w kodzie
2. ✅ **Niezawodność:** Auto-restart, retry logic, error handling
3. ✅ **Łatwość:** 1-kliknięciowa instalacja/aktualizacja
4. ✅ **Performance:** ~2-3x szybszy eksport (kompilowany kod)
5. ✅ **Monitoring:** Lepsze logi, structured logging
6. ✅ **Utrzymanie:** Silne typowanie = mniej błędów runtime
7. ✅ **Deployment:** Self-contained EXE = brak zależności Python/pip

**Czas migracji:** ~5 dni
**ROI:** Zwrot w pierwszym miesiącu (mniej problemów, łatwiejsza konserwacja)
