@echo off
echo ========================================
echo R^&G DB2 Export Service - Uninstall Script
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

echo Zatrzymywanie serwisu...
sc stop RGExportService
timeout /t 5 /nobreak >nul

echo.
echo Usuwanie serwisu...
sc delete RGExportService
if errorlevel 1 (
    echo OSTRZEZENIE: Nie mozna usunac serwisu (byc moze nie istnieje)
)

echo.
set /p DELETE_FILES="Czy usunac pliki z C:\Services\DB2Export? (T/N): "
if /i "%DELETE_FILES%"=="T" (
    echo Usuwanie plikow...
    rmdir /S /Q "C:\Services\DB2Export\"
    echo Pliki usuniete.
) else (
    echo Pliki pozostawione.
)

echo.
echo ========================================
echo Deinstalacja zakonczona!
echo ========================================
echo.
pause
