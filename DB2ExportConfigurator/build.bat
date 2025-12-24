@echo off
echo ========================================
echo DB2 Export Configurator - Build
echo ========================================
echo.

cd /d "%~dp0"

echo Czyszczenie poprzednich build'ow...
dotnet clean -c Release
if errorlevel 1 (
    echo BLAD: Czyszczenie nie powiodlo sie!
    pause
    exit /b 1
)

echo.
echo Budowanie projektu...
dotnet publish -c Release -r win-x64 --self-contained false -o bin\Publish
if errorlevel 1 (
    echo BLAD: Budowanie nie powiodlo sie!
    pause
    exit /b 1
)

echo.
echo ========================================
echo Build zakonczony pomyslnie!
echo Pliki znajduja sie w: bin\Publish\
echo ========================================
echo.
pause
