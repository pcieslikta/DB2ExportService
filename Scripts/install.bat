@echo off
echo ========================================
echo R^&G DB2 Export Service - Install Script
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

cd /d "%~dp0.."

REM Sprawdz czy plik exe istnieje (moze byc w katalogu glownym lub publish)
set EXE_PATH=
if exist "DB2ExportService.exe" (
    set EXE_PATH=.
    echo Znaleziono serwis w katalogu glownym
) else if exist "publish\DB2ExportService.exe" (
    set EXE_PATH=publish
    echo Znaleziono serwis w katalogu publish
) else (
    echo BLAD: Nie znaleziono pliku DB2ExportService.exe
    echo Najpierw uruchom publish.bat aby zbudowac projekt.
    pause
    exit /b 1
)

echo Zatrzymywanie serwisu (jesli dziala)...
sc query RGExportService >nul 2>&1
if %errorLevel% equ 0 (
    echo Serwis istnieje, zatrzymywanie...
    sc stop RGExportService
    timeout /t 5 /nobreak >nul

    echo Usuwanie starego serwisu...
    sc delete RGExportService
    timeout /t 3 /nobreak >nul
)

echo.
echo Tworzenie katalogow...
if not exist "C:\EXPORT\LOG\" mkdir "C:\EXPORT\LOG\"
if not exist "C:\Services\DB2Export\" mkdir "C:\Services\DB2Export\"

echo.
echo Kopiowanie plikow...
xcopy /Y /E /I "%EXE_PATH%\*" "C:\Services\DB2Export\"
if errorlevel 1 (
    echo BLAD: Kopiowanie plikow nie powiodlo sie!
    pause
    exit /b 1
)

REM Usun niepotrzebne podkatalogi jesli sie dostaly
if exist "C:\Services\DB2Export\DB2ExportConfigurator\" (
    echo Usuwanie niepotrzebnego katalogu DB2ExportConfigurator...
    rmdir /S /Q "C:\Services\DB2Export\DB2ExportConfigurator\"
)
if exist "C:\Services\DB2Export\obj\" rmdir /S /Q "C:\Services\DB2Export\obj\"
if exist "C:\Services\DB2Export\bin\" rmdir /S /Q "C:\Services\DB2Export\bin\"

echo.
echo Instalacja serwisu Windows...
sc create RGExportService binPath= "C:\Services\DB2Export\DB2ExportService.exe" start= auto DisplayName= "R&G Export Service"
if errorlevel 1 (
    echo BLAD: Instalacja serwisu nie powiodla sie!
    pause
    exit /b 1
)

echo.
echo Konfiguracja serwisu...
sc description RGExportService "Automatyczny eksport danych z bazy DB2 do plikow CSV - R&G"
sc failure RGExportService reset= 86400 actions= restart/60000/restart/60000/restart/60000

echo.
echo ========================================
echo Instalacja zakonczona pomyslnie!
echo.
echo WAZNE: Przed uruchomieniem serwisu:
echo 1. Skonfiguruj credentials w Windows Credential Manager
echo    - Uruchom: cmdkey /add:DB2Export_PROD /user:dbtaran1 /pass:haslo
echo 2. Sprawdz konfiguracje w: C:\Services\DB2Export\appsettings.json
echo 3. Uruchom serwis: sc start RGExportService
echo    lub: net start RGExportService
echo ========================================
echo.
pause
