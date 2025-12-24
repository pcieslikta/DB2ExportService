# DB2 Export Configurator

Graficzny konfigurator dla DB2 Export Service - aplikacja Windows Forms do łatwego zarządzania konfiguracją i serwisem.

## 🎯 Funkcje

### 📋 **Zakładki konfiguracyjne:**

1. **🗄️ Baza DB2**
   - Konfiguracja połączenia (Database, Hostname, Port)
   - Credentials (User/Password lub Windows Credential Manager)
   - Automatyczna walidacja

2. **📊 Eksport**
   - Ścieżki eksportu i logów
   - Harmonogram (godzina eksportu)
   - Dni wstecz
   - Kod eksportu (SOSNO/STANDARD)

3. **🚌 Pojazdy**
   - Tryb wyboru: lista lub zakres
   - Lista pojazdów (oddzielonych przecinkami)
   - Zakres pojazdów (od-do)

4. **⚙️ Serwis**
   - Status serwisu (Uruchomiony/Zatrzymany)
   - Start/Stop/Restart serwisu
   - Otwieranie katalogu logów
   - Monitoring w czasie rzeczywistym

---

## 🚀 Uruchamianie

### **Opcja 1: Przez skrypt (REKOMENDOWANE)**

```bash
cd C:\EXPORT\CSv\DB2ExportService
Scripts\run-configurator.bat
```

### **Opcja 2: Bezpośrednio**

```bash
cd C:\EXPORT\CSv\DB2ExportService\DB2ExportConfigurator
run.bat
```

### **Opcja 3: Ręcznie**

```bash
cd C:\EXPORT\CSv\DB2ExportService\DB2ExportConfigurator\bin\Publish
DB2ExportConfigurator.exe
```

**UWAGA:** Konfigurator wymaga uprawnień administratora (do zarządzania serwisem).

---

## 📝 Konfiguracja

### **Lokalizacja pliku appsettings.json:**

1. **Preferowana:** `C:\Services\DB2Export\appsettings.json`
2. **Fallback:** `C:\ProgramData\DB2Export\appsettings.json`

### **Zapisywanie zmian:**

1. Edytuj ustawienia w odpowiednich zakładkach
2. Kliknij **💾 Zapisz**
3. Restart serwisu (jeśli działa) w zakładce **⚙️ Serwis**

---

## 🔧 Zarządzanie serwisem

### **Dostępne operacje:**

- **▶️ Uruchom serwis** - Startuje zatrzymany serwis
- **⏹️ Zatrzymaj serwis** - Zatrzymuje działający serwis
- **🔄 Restart serwisu** - Restartuje serwis (stop + start)
- **📄 Otwórz katalog logów** - Otwiera Explorer z logami

### **Status serwisu:**

- **Uruchomiony ✓** (zielony) - Serwis działa prawidłowo
- **Zatrzymany** (czerwony) - Serwis nie działa
- **Nie zainstalowany** (szary) - Serwis nie został zainstalowany

---

## 🛠️ Budowanie z kodu

```bash
cd C:\EXPORT\CSv\DB2ExportService\DB2ExportConfigurator
build.bat
```

Pliki zostaną utworzone w `bin\Publish\`

---

## 🎨 Interfejs

### **Główne okno:**
- Szerokość: 900px
- Wysokość: 700px
- 4 zakładki (DB2, Eksport, Pojazdy, Serwis)
- Przyciski: Zapisz, Anuluj

### **Walidacja:**
- Automatyczna walidacja pól
- Czerwone obramowanie przy błędach
- Tooltips z opisami

---

## 📦 Wymagania

- Windows 10/11 lub Windows Server 2016+
- .NET 8.0 Runtime
- Uprawnienia administratora (do zarządzania serwisem)

---

## 🐛 Rozwiązywanie problemów

### **Konfigurator nie uruchamia się:**

1. Sprawdź czy masz .NET 8.0 Runtime:
   ```bash
   dotnet --version
   ```

2. Uruchom jako Administrator:
   ```bash
   Prawy przycisk → Uruchom jako administrator
   ```

### **Nie można zapisać konfiguracji:**

1. Sprawdź uprawnienia do katalogu `C:\Services\DB2Export\`
2. Uruchom konfigurator jako Administrator

### **Serwis nie reaguje:**

1. Sprawdź czy serwis jest zainstalowany:
   ```bash
   sc query RGExportService
   ```

2. Sprawdź logi serwisu:
   ```bash
   type C:\EXPORT\LOG\export_service_*.log
   ```

---

## 📞 Wsparcie

W razie problemów:
1. Sprawdź logi w `C:\EXPORT\LOG\`
2. Sprawdź główny [README.md](../README.md)
3. Uruchom konfigurator z uprawnieniami administratora

---

© 2024 R&G - DB2 Export Service Configurator
