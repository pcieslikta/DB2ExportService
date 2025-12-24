@echo off
echo Uruchamianie serwisu RGExportService...
net start RGExportService
if errorlevel 1 (
    echo BLAD: Nie udalo sie uruchomic serwisu!
    pause
    exit /b 1
)
echo Serwis uruchomiony pomyslnie.
pause
