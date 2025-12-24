@echo off
echo ========================================
echo DB2 Export Service - Konfigurator
echo ========================================
echo.

cd /d "%~dp0.."

REM Sprawdz czy konfigurator istnieje
if exist "DB2ExportConfigurator.exe" (
    echo Uruchamianie konfiguratora jako Administrator...
    powershell -Command "Start-Process 'DB2ExportConfigurator.exe' -Verb RunAs"
) else if exist "DB2ExportConfigurator\bin\Publish\DB2ExportConfigurator.exe" (
    echo Uruchamianie konfiguratora z katalogu projektu...
    powershell -Command "Start-Process 'DB2ExportConfigurator\bin\Publish\DB2ExportConfigurator.exe' -Verb RunAs"
) else (
    echo BLAD: Nie znaleziono konfiguratora!
    echo Uruchom najpierw: publish.bat
    pause
    exit /b 1
)
