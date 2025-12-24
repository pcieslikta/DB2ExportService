@echo off
echo Zatrzymywanie serwisu RGExportService...
net stop RGExportService
if errorlevel 1 (
    echo BLAD: Nie udalo sie zatrzymac serwisu!
    pause
    exit /b 1
)
echo Serwis zatrzymany pomyslnie.
pause
