@echo off
echo ========================================
echo R^&G DB2 Export Service - Build Script
echo ========================================
echo.

cd /d "%~dp0.."

echo Czyszczenie poprzednich build'ow...
dotnet clean -c Release
if errorlevel 1 (
    echo BLAD: Czyszczenie nie powiodlo sie!
    pause
    exit /b 1
)

echo.
echo Budowanie projektu...
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish
if errorlevel 1 (
    echo BLAD: Budowanie nie powiodlo sie!
    pause
    exit /b 1
)

echo.
echo ========================================
echo Build zakonczony pomyslnie!
echo Pliki znajduja sie w katalogu: publish\
echo ========================================
echo.
pause
