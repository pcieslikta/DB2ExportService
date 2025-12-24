# Publish Guide - Deployment Package

## 📦 Tworzenie paczki deployment

### **Krok 1: Uruchom skrypt publish**

```bash
cd C:\EXPORT\CSv\DB2ExportService
publish.bat
```

### **Co robi skrypt:**

1. ✅ **Czyści poprzednie buildy**
2. ✅ **Buduje DB2ExportService** (Windows Service)
3. ✅ **Buduje DB2ExportConfigurator** (GUI)
4. ✅ **Kopiuje skrypty instalacyjne**
5. ✅ **Kopiuje dokumentację**
6. ✅ **Tworzy INSTALL.txt** (instrukcje instalacji)
7. ✅ **Tworzy CHANGELOG.txt** (lista zmian)
8. ✅ **Pakuje wszystko do ZIP**

---

## 📁 Struktura wygenerowanego pakietu

```
publish/
├── DB2ExportService-v1.0.0-20241223/
│   ├── Service/
│   │   ├── DB2ExportService.exe          ⭐ Serwis Windows
│   │   ├── appsettings.json              ⚙️ Konfiguracja
│   │   └── clidriver/                    📁 DB2 drivers
│   │
│   ├── Configurator/
│   │   └── DB2ExportConfigurator.exe     🖥️ GUI
│   │
│   ├── Scripts/
│   │   ├── quick-install.bat             ⚡ Szybka instalacja (REKOMENDOWANE)
│   │   ├── install.bat                   📥 Instalacja serwisu
│   │   ├── uninstall.bat                 🗑️ Deinstalacja
│   │   ├── start.bat                     ▶️ Start
│   │   ├── stop.bat                      ⏹️ Stop
│   │   └── setup-credentials.bat         🔑 Konfiguracja credentials
│   │
│   ├── Documentation/
│   │   ├── README.md                     📖 Główna dokumentacja
│   │   ├── QUICKSTART.md                 🚀 Szybki start
│   │   ├── MIGRATION_GUIDE.md            🔄 Migracja z Python
│   │   └── CONFIGURATOR.md               🖥️ Konfigurator GUI
│   │
│   ├── INSTALL.txt                        📋 Instrukcja instalacji
│   ├── CHANGELOG.txt                      📝 Lista zmian
│   └── VERSION.txt                        🏷️ Informacje o wersji
│
└── DB2ExportService-v1.0.0-20241223.zip   📦 Plik do przeniesienia
```

---

## 🚀 Deployment na nowy serwer

### **1. Przygotowanie pakietu**

Na maszynie deweloperskiej:
```bash
cd C:\EXPORT\CSv\DB2ExportService
publish.bat
```

Poczekaj aż skrypt zakończy się i utworzy plik ZIP.

### **2. Przeniesienie**

Skopiuj plik ZIP na docelowy serwer:
```
publish\DB2ExportService-v1.0.0-YYYYMMDD.zip
```

### **3. Instalacja na docelowym serwerze**

#### **Opcja A: Szybka instalacja (REKOMENDOWANE)**

1. Rozpakuj ZIP
2. Uruchom jako **Administrator**:
   ```bash
   Scripts\quick-install.bat
   ```
3. Postępuj zgodnie z instrukcjami na ekranie

#### **Opcja B: Instalacja ręczna**

1. Rozpakuj ZIP
2. Przeczytaj `INSTALL.txt`
3. Wykonaj kroki:
   ```bash
   # Krok 1: Credentials
   Scripts\setup-credentials.bat

   # Krok 2: Konfiguracja (opcjonalnie)
   Configurator\DB2ExportConfigurator.exe

   # Krok 3: Instalacja
   Scripts\install.bat

   # Krok 4: Uruchomienie
   Scripts\start.bat
   ```

---

## 🔍 Weryfikacja instalacji

### **Sprawdź status serwisu:**
```bash
sc query RGExportService
```

Oczekiwany wynik:
```
STATE              : 4  RUNNING
```

### **Sprawdź logi:**
```bash
type C:\EXPORT\LOG\export_service_*.log
```

### **Sprawdź harmonogram:**
Powinien być komunikat w logach:
```
Zaplanowano eksport codziennie o 13:15
```

### **Poczekaj na eksport:**
Po wykonaniu (o 13:15 lub ręcznie wymuszony):
```bash
dir C:\EXPORT\BRAMKI_*.csv
dir C:\EXPORT\BRAMKID_*.csv
```

---

## 📝 Zawartość INSTALL.txt

Plik `INSTALL.txt` w pakiecie zawiera:
- ✅ Wymagania systemowe
- ✅ Strukturę plików
- ✅ Krok po kroku instalację
- ✅ Weryfikację
- ✅ Troubleshooting
- ✅ Linki do dokumentacji

---

## 🔄 Aktualizacja istniejącej instalacji

### **1. Backup starej konfiguracji:**
```bash
copy C:\Services\DB2Export\appsettings.json C:\Backup\appsettings.json.bak
```

### **2. Zatrzymaj serwis:**
```bash
net stop RGExportService
```

### **3. Rozpakuj nową wersję**

### **4. Skopiuj starą konfigurację:**
```bash
copy C:\Backup\appsettings.json.bak Service\appsettings.json
```

### **5. Uruchom install.bat:**
```bash
Scripts\install.bat
```

### **6. Uruchom serwis:**
```bash
Scripts\start.bat
```

---

## 📦 Tworzenie wersji custom

Jeśli chcesz zmienić wersję lub dodać własne pliki:

1. Edytuj `publish.bat`:
   ```batch
   set VERSION=1.1.0
   ```

2. Dodaj własne pliki do pakietu:
   ```batch
   copy "MojPlik.txt" "%PUBLISH_DIR%\" >nul
   ```

3. Uruchom:
   ```bash
   publish.bat
   ```

---

## 🎯 Najlepsze praktyki

### **Development:**
- ✅ Testuj lokalnie przed publish
- ✅ Aktualizuj VERSION w publish.bat
- ✅ Aktualizuj CHANGELOG.txt

### **Deployment:**
- ✅ Zawsze twórz backup przed aktualizacją
- ✅ Testuj na środowisku testowym
- ✅ Sprawdź logi po instalacji
- ✅ Zweryfikuj harmonogram

### **Dokumentacja:**
- ✅ Aktualizuj README przy zmianach
- ✅ Dodawaj wpisy do CHANGELOG
- ✅ Dokumentuj zmiany w konfiguracji

---

## 🛠️ Troubleshooting

### **Publish.bat się nie wykonuje:**

1. Uruchom jako Administrator
2. Sprawdź czy masz .NET 8 SDK:
   ```bash
   dotnet --version
   ```

### **Błąd podczas budowania:**

1. Sprawdź logi build:
   ```bash
   dotnet build -c Release
   ```
2. Usuń katalogi bin/obj i spróbuj ponownie

### **ZIP nie został utworzony:**

1. Sprawdź czy masz PowerShell 5.0+:
   ```bash
   $PSVersionTable.PSVersion
   ```
2. Ręcznie spakuj katalog publish\DB2ExportService-*\

---

## ✅ Checklist przed deploymentem

- [ ] Zaktualizowana wersja w publish.bat
- [ ] Zaktualizowany CHANGELOG.txt
- [ ] Build przeszedł bez błędów
- [ ] Przetestowano lokalnie
- [ ] Sprawdzono zawartość ZIP
- [ ] Przeczytano INSTALL.txt
- [ ] Przygotowano backup na docelowym serwerze

---

© 2024 R&G - DB2 Export Service
