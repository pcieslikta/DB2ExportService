@echo off
setlocal enabledelayedexpansion

echo ========================================
echo DB2 Export Service - Complete Build
echo ========================================
echo.

cd /d "%~dp0"

REM Ustaw wersje (BEZ daty)
set VERSION=1.0.0
set RELEASE_NAME=DB2ExportService-v%VERSION%
set PUBLISH_DIR=publish\%RELEASE_NAME%

echo Wersja: %VERSION%
echo Katalog docelowy: %PUBLISH_DIR%
echo.

REM Czyszczenie poprzednich buildow
echo [1/7] Czyszczenie poprzednich buildow...
if exist "%PUBLISH_DIR%" rmdir /S /Q "%PUBLISH_DIR%"
if exist "publish\%RELEASE_NAME%.zip" del /Q "publish\%RELEASE_NAME%.zip"
if not exist "publish" mkdir "publish"

REM Utworz strukturę katalogow
echo [2/7] Tworzenie struktury katalogow...
mkdir "%PUBLISH_DIR%" 2>nul
mkdir "%PUBLISH_DIR%\Scripts" 2>nul
mkdir "%PUBLISH_DIR%\Documentation" 2>nul

REM Build serwisu (do glownego katalogu)
echo [3/7] Budowanie DB2ExportService...
dotnet clean -c Release >nul 2>&1
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o "%PUBLISH_DIR%"
if errorlevel 1 (
    echo BLAD: Build serwisu nie powiodl sie!
    pause
    exit /b 1
)

REM Build konfiguratora (self-contained, aby zawierał Windows Forms)
echo [4/7] Budowanie DB2ExportConfigurator...
cd DB2ExportConfigurator
dotnet clean -c Release >nul 2>&1
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o "bin\PublishTemp"
if errorlevel 1 (
    echo BLAD: Build konfiguratora nie powiodl sie!
    cd ..
    pause
    exit /b 1
)

REM Kopiuj wszystkie pliki DLL konfiguratora (unikaj duplikatow z serwisu)
echo Kopiowanie konfiguratora i wszystkich jego zaleznosci...
copy /Y "bin\PublishTemp\DB2ExportConfigurator.exe" "..\%PUBLISH_DIR%\" >nul
copy /Y "bin\PublishTemp\DB2ExportConfigurator.dll" "..\%PUBLISH_DIR%\" >nul
copy /Y "bin\PublishTemp\DB2ExportConfigurator.pdb" "..\%PUBLISH_DIR%\" >nul 2>nul

REM Kopiuj wszystkie DLL z konfiguratora (moze byc duplikacja, ale to jest OK)
for %%F in (bin\PublishTemp\*.dll) do (
    copy /Y "%%F" "..\%PUBLISH_DIR%\" >nul 2>nul
)

cd ..

REM Kopiowanie skryptow
echo [5/7] Kopiowanie skryptow instalacyjnych...
if exist "Scripts\*.bat" (
    copy /Y "Scripts\*.bat" "%PUBLISH_DIR%\Scripts\" >nul
) else (
    echo OSTRZEZENIE: Brak skryptow w katalogu Scripts\
)

REM Kopiowanie dokumentacji
echo [6/7] Kopiowanie dokumentacji...
if exist "README.md" copy /Y "README.md" "%PUBLISH_DIR%\Documentation\README.md" >nul
if exist "MIGRATION_GUIDE.md" copy /Y "MIGRATION_GUIDE.md" "%PUBLISH_DIR%\Documentation\MIGRATION_GUIDE.md" >nul
if exist "QUICKSTART.md" copy /Y "QUICKSTART.md" "%PUBLISH_DIR%\Documentation\QUICKSTART.md" >nul
if exist "PUBLISH.md" copy /Y "PUBLISH.md" "%PUBLISH_DIR%\Documentation\PUBLISH.md" >nul
if exist "DB2ExportConfigurator\README.md" copy /Y "DB2ExportConfigurator\README.md" "%PUBLISH_DIR%\Documentation\CONFIGURATOR.md" >nul
if exist "VERSION.txt" copy /Y "VERSION.txt" "%PUBLISH_DIR%\VERSION.txt" >nul

REM Utworz plik INSTALL.txt
(
echo ========================================
echo DB2 Export Service v%VERSION%
echo ========================================
echo.
echo INSTALACJA NA NOWYM SERWERZE:
echo.
echo 1. WYMAGANIA:
echo    - Windows Server 2016+ lub Windows 10/11
echo    - Uprawnienia administratora
echo    - Sterownik IBM DB2 Client zainstalowany
echo    - .NET Runtime zawarty w pakiecie ^(self-contained^)
echo.
echo 2. STRUKTURA PLIKOW:
echo    DB2ExportService.exe       - Serwis Windows
echo    DB2ExportConfigurator.exe  - Konfigurator GUI
echo    appsettings.json           - Konfiguracja
echo    Scripts\                   - Skrypty instalacyjne
echo    Documentation\             - Dokumentacja
echo    clidriver\                 - DB2 drivers
echo.
echo 3. INSTALACJA KROK PO KROKU:
echo.
echo    a^) Skopiuj caly katalog na docelowy serwer
echo.
echo    b^) Konfiguracja credentials:
echo       cd Scripts
echo       setup-credentials.bat
echo.
echo    c^) Edycja konfiguracji ^(opcjonalnie^):
echo       - Uruchom: DB2ExportConfigurator.exe
echo       - Lub recznie edytuj: appsettings.json
echo.
echo    d^) Instalacja serwisu:
echo       cd Scripts
echo       install.bat
echo.
echo    e^) Uruchomienie serwisu:
echo       cd Scripts
echo       start.bat
echo.
echo 4. SZYBKA INSTALACJA:
echo    cd Scripts
echo    quick-install.bat
echo.
echo 5. WERYFIKACJA:
echo    - Logi: C:\EXPORT\LOG\export_service_*.log
echo    - Status: sc query RGExportService
echo    - Pliki CSV: C:\EXPORT\BRAMKI_*.csv
echo.
echo 6. DEZINSTALACJA:
echo    cd Scripts
echo    uninstall.bat
echo.
echo ========================================
echo DOKUMENTACJA:
echo ========================================
echo.
echo Documentation\README.md           - Pelna dokumentacja
echo Documentation\QUICKSTART.md       - Szybki start
echo Documentation\MIGRATION_GUIDE.md  - Migracja z Python
echo Documentation\CONFIGURATOR.md     - Konfigurator GUI
echo Documentation\PUBLISH.md          - Deployment guide
echo.
) > "%PUBLISH_DIR%\INSTALL.txt"

REM Utworz changelog
(
echo ========================================
echo DB2 Export Service - CHANGELOG
echo ========================================
echo.
echo v%VERSION%
echo ========================================
echo.
echo [+] NOWE FUNKCJE:
echo     - Windows Service w .NET 8
echo     - Graficzny konfigurator GUI
echo     - Windows Credential Manager
echo     - Parametryzowane zapytania SQL
echo     - Auto-restart przy bledach
echo     - Structured logging z Serilog
echo     - Enterprise scheduling z Quartz.NET
echo.
echo [*] POPRAWKI BEZPIECZENSTWA:
echo     - Usunieto hardcoded credentials
echo     - Zabezpieczenie przed SQL injection
echo     - Bezpieczne przechowywanie hasel
echo.
echo [*] ULEPSZENIA:
echo     - Lepsza walidacja konfiguracji
echo     - Real-time monitoring serwisu
echo     - Automatyczne skrypty instalacyjne
echo     - Kompletna dokumentacja
echo.
) > "%PUBLISH_DIR%\CHANGELOG.txt"

REM Czyszczenie niepotrzebnych podkatalogów
echo Czyszczenie niepotrzebnych podkatalogów...
if exist "%PUBLISH_DIR%\DB2ExportConfigurator" (
    echo Usuwanie katalogu DB2ExportConfigurator...
    rmdir /S /Q "%PUBLISH_DIR%\DB2ExportConfigurator"
)
if exist "%PUBLISH_DIR%\obj" rmdir /S /Q "%PUBLISH_DIR%\obj"
if exist "%PUBLISH_DIR%\bin" rmdir /S /Q "%PUBLISH_DIR%\bin"
if exist "%PUBLISH_DIR%\publish" rmdir /S /Q "%PUBLISH_DIR%\publish"

REM Pakowanie do ZIP
echo [7/7] Pakowanie do ZIP...
powershell -Command "if (Test-Path '%PUBLISH_DIR%') { Compress-Archive -Path '%PUBLISH_DIR%\*' -DestinationPath 'publish\%RELEASE_NAME%.zip' -Force } else { Write-Error 'Katalog nie istnieje' }"
if errorlevel 1 (
    echo BLAD: Pakowanie do ZIP nie powiodlo sie!
    pause
    exit /b 1
)

REM Sprawdz zawartosc
echo.
echo ========================================
echo BUILD ZAKONCZONY POMYSLNIE!
echo ========================================
echo.
echo Wersja:        %VERSION%
echo Katalog:       %PUBLISH_DIR%
echo Plik ZIP:      publish\%RELEASE_NAME%.zip
echo.
echo === Glowne pliki ===
dir /B "%PUBLISH_DIR%" | findstr /i "\.exe$ \.json$"
echo.
echo === Skrypty ===
dir /B "%PUBLISH_DIR%\Scripts" 2>nul | findstr /i "\.bat$"
echo.
echo === Dokumentacja ===
dir /B "%PUBLISH_DIR%\Documentation" 2>nul
echo.

REM Rozmiar pliku ZIP
for %%A in ("publish\%RELEASE_NAME%.zip") do (
    set size=%%~zA
    set /a sizeMB=!size! / 1024 / 1024
    echo Rozmiar ZIP: !size! bytes ^(!sizeMB! MB^)
)

echo.
echo ========================================
echo GOTOWE DO PRZENIESIENIA NA SERWER!
echo ========================================
echo.
echo Plik: publish\%RELEASE_NAME%.zip
echo.
echo Instalacja na nowym serwerze:
echo 1. Rozpakuj %RELEASE_NAME%.zip
echo 2. Przeczytaj INSTALL.txt
echo 3. Uruchom: Scripts\quick-install.bat
echo.
pause
