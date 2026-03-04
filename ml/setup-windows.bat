@echo off
REM ============================================================
REM  setup-windows.bat
REM  Rozwiązuje błąd "Windows Long Path" podczas instalacji TensorFlow.
REM
REM  Co robi:
REM  1. Tworzy środowisko wirtualne (venv) w C:\facerecog\venv
REM     (krótka ścieżka = brak problemu z limitem 260 znaków)
REM  2. Instaluje wszystkie zależności z requirements.txt
REM  3. Wyświetla instrukcję jak aktywować venv przy kolejnych uruchomieniach
REM
REM  Uruchom ten plik ZAMIAST "pip install -r requirements.txt"
REM ============================================================

echo.
echo === Konfiguracja srodowiska Python dla ML serwisu ===
echo.

REM Sprawdź wersję Pythona
python --version 2>&1 | findstr /R "3\.1[012]" > nul
if errorlevel 1 (
    echo.
    echo UWAGA: Nie wykryto Pythona 3.10-3.12.
    echo TensorFlow NIE obsluguje Pythona 3.13.
    echo.
    echo Pobierz Python 3.12 z: https://www.python.org/downloads/
    echo (WAZNE: zaznacz "Add Python to PATH" podczas instalacji)
    echo.
    pause
    exit /b 1
)

REM Utwórz krótką ścieżkę dla venv
set VENV_DIR=C:\facerecog\venv
echo Tworzenie srodowiska wirtualnego w: %VENV_DIR%
mkdir C:\facerecog 2>nul
python -m venv "%VENV_DIR%"
if errorlevel 1 (
    echo BLAD: Nie udalo sie utworzyc srodowiska wirtualnego.
    pause
    exit /b 1
)

REM Aktywuj venv i zainstaluj zależności
echo.
echo Instalowanie zaleznosci (moze to chwile zajac)...
call "%VENV_DIR%\Scripts\activate.bat"
python -m pip install --upgrade pip
pip install -r "%~dp0requirements.txt"

if errorlevel 1 (
    echo.
    echo BLAD: Instalacja nie powiodla sie.
    echo Sprobuj wlaczyc Windows Long Path i ponow:
    echo   reg add "HKLM\SYSTEM\CurrentControlSet\Control\FileSystem" /v LongPathsEnabled /t REG_DWORD /d 1 /f
    pause
    exit /b 1
)

echo.
echo ============================================================
echo  Instalacja zakonczona sukcesem!
echo.
echo  Krok 2: Dodaj zdjecia twarzy do folderu dataset\
echo    Schemat nazewnictwa: ImieNazwisko_numer.jpg
echo    Przyklad: Jan Kowalski_1.jpg, Anna Nowak_2.png
echo.
echo  Krok 3: Uruchom serwis ML
echo    call C:\facerecog\venv\Scripts\activate.bat
echo    python service.py
echo.
echo  LUB uzyj gotowego skryptu: run-windows.bat
echo.
echo  UWAGA: przy pierwszym uruchomieniu serwis pobiera wagi modelu
echo  (~90 MB) i buduje baze embeddingów – moze to zajac kilka minut.
echo  Nastepne uruchomienia sa szybsze (cache na dysku).
echo ============================================================
echo.

REM Utwórz też skrypt run-windows.bat
echo @echo off > "%~dp0run-windows.bat"
echo call C:\facerecog\venv\Scripts\activate.bat >> "%~dp0run-windows.bat"
echo cd /d "%~dp0" >> "%~dp0run-windows.bat"
echo python service.py >> "%~dp0run-windows.bat"
echo Wygenerowano run-windows.bat – uzyj go do uruchamiania serwisu ML.

pause
