@echo off
echo ========================================
echo DB2 Export Service - QUICK INSTALL
echo ========================================
echo.

REM Sprawdz uprawnienia administratora
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo BLAD: Ten skrypt wymaga uprawnien administratora!
    echo Uruchom ponownie jako Administrator.
    pause
    exit /b 1
)

echo Ten skrypt przeprowadzi Cie przez caly proces instalacji.
echo.
pause

REM Krok 1: Setup credentials
echo.
echo ========================================
echo KROK 1/4: Konfiguracja Credentials
echo ========================================
echo.
call setup-credentials.bat
if errorlevel 1 (
    echo BLAD w kroku 1!
    pause
    exit /b 1
)

REM Krok 2: Edycja konfiguracji
echo.
echo ========================================
echo KROK 2/4: Konfiguracja
echo ========================================
echo.
echo Czy chcesz edytowac konfiguracje przed instalacja?
echo 1 - TAK (otworz konfigurator GUI)
echo 2 - NIE (uzyj domyslnej konfiguracji)
echo.
set /p CONFIG_CHOICE="Wybierz (1 lub 2): "

if "%CONFIG_CHOICE%"=="1" (
    echo Uruchamianie konfiguratora...
    cd /d "%~dp0.."
    if exist "DB2ExportConfigurator.exe" (
        start /wait DB2ExportConfigurator.exe
    ) else (
        echo Konfigurator nie znaleziony. Kontynuuje z domyslna konfiguracja...
    )
    cd /d "%~dp0"
)

REM Krok 3: Instalacja serwisu
echo.
echo ========================================
echo KROK 3/4: Instalacja Serwisu
echo ========================================
echo.
call install.bat
if errorlevel 1 (
    echo BLAD w kroku 3!
    pause
    exit /b 1
)

REM Krok 4: Start serwisu
echo.
echo ========================================
echo KROK 4/4: Uruchomienie Serwisu
echo ========================================
echo.
set /p START_CHOICE="Czy uruchomic serwis teraz? (T/N): "

if /i "%START_CHOICE%"=="T" (
    call start.bat
    if errorlevel 1 (
        echo BLAD podczas uruchamiania serwisu!
        pause
        exit /b 1
    )
)

REM Podsumowanie
echo.
echo ========================================
echo INSTALACJA ZAKONCZONA POMYSLNIE!
echo ========================================
echo.
echo Co dalej?
echo.
echo 1. Sprawdz status serwisu:
echo    sc query RGExportService
echo.
echo 2. Sprawdz logi:
echo    type C:\EXPORT\LOG\export_service_*.log
echo.
echo 3. Poczekaj na harmonogram (domyslnie: 13:15)
echo    lub edytuj harmonogram w konfiguratorze
echo.
echo 4. Sprawdz eksportowane pliki:
echo    dir C:\EXPORT\BRAMKI_*.csv
echo.
echo 5. Zarzadzaj serwisem przez konfigurator:
echo    %~dp0run-configurator.bat
echo.
pause
