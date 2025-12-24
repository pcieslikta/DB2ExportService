@echo off
echo ========================================
echo Konfiguracja Credentials dla DB2 Export
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

echo Ten skrypt pomoze skonfigurowac credentials w Windows Credential Manager.
echo.

REM Konfiguracja dla PROD
echo === Baza PROD ===
set /p DB_USER_PROD="Podaj uzytkownika DB2 dla PROD [dbtaran1]: "
if "%DB_USER_PROD%"=="" set DB_USER_PROD=dbtaran1

set /p DB_PASS_PROD="Podaj haslo DB2 dla PROD: "
if "%DB_PASS_PROD%"=="" (
    echo BLAD: Haslo nie moze byc puste!
    pause
    exit /b 1
)

echo.
echo Zapisywanie credentials dla PROD...
cmdkey /add:DB2Export_PROD /user:%DB_USER_PROD% /pass:%DB_PASS_PROD%
if errorlevel 1 (
    echo BLAD: Nie udalo sie zapisac credentials!
    pause
    exit /b 1
)

echo.
echo ========================================
echo Credentials skonfigurowane pomyslnie!
echo.
echo Zapisano:
echo - DB2Export_PROD (uzytkownik: %DB_USER_PROD%)
echo.
echo Mozesz teraz uruchomic serwis.
echo ========================================
echo.
pause
