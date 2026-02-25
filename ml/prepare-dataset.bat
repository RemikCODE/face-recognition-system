@echo off
REM ============================================================
REM  prepare-dataset.bat
REM  Kopiuje zdjęcia twarzy z pobranego archiwum do ml/dataset/
REM
REM  Użycie: kliknij dwa razy i podaj ścieżkę do archive.zip
REM ============================================================

cd /d "%~dp0"

echo.
echo === Przygotowanie datasetu twarzy ===
echo.
echo Podaj pelna sciezke do pobranego pliku archive.zip:
echo (np. C:\Users\Ty\Downloads\archive.zip)
echo.
set /p ZIP_PATH="Sciezka do ZIP: "

if "%ZIP_PATH%"=="" (
    echo Nie podano sciezki. Koniec.
    pause
    exit /b 1
)

python prepare-dataset.py --zip "%ZIP_PATH%"

echo.
pause
