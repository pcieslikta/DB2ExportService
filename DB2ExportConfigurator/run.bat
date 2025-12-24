@echo off
echo Uruchamianie DB2 Export Configurator...
echo.

cd /d "%~dp0"

REM Sprawdz czy jest build
if not exist "bin\Publish\DB2ExportConfigurator.exe" (
    echo Brak pliku exe. Najpierw uruchom build.bat
    pause
    exit /b 1
)

REM Uruchom jako Administrator
powershell -Command "Start-Process 'bin\Publish\DB2ExportConfigurator.exe' -Verb RunAs"
