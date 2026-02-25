@echo off
REM ============================================================
REM  Face Recognition System – start.bat
REM  Uruchamia oba serwisy jednym podwójnym kliknięciem.
REM
REM  Wymagania przed pierwszym uruchomieniem:
REM    1. Python 3.10+  (https://www.python.org/downloads/)
REM    2. .NET 8 SDK    (https://dotnet.microsoft.com/download/dotnet/8)
REM    3. pip install -r ml\requirements.txt  (tylko raz)
REM    4. Folder ml\dataset\ z zdjęciami twarzy
REM ============================================================

SET SCRIPT_DIR=%~dp0

REM ---------- Okno 1: Python ML serwis (port 5001) ----------
start "ML Serwis – Python (port 5001)" cmd /k "cd /d "%SCRIPT_DIR%ml" && echo Uruchamianie serwisu ML... && python service.py"

REM Odczekaj chwilę, żeby Python zdążył wystartować
timeout /t 3 /nobreak > nul

REM ---------- Okno 2: ASP.NET Backend + Web UI (port 5233) ----------
start "Backend ASP.NET + Web UI (port 5233)" cmd /k "cd /d "%SCRIPT_DIR%backend\FaceRecognitionApi" && echo Uruchamianie backendu... && dotnet run"

REM ---------- Otwórz przeglądarkę po chwili ----------
timeout /t 8 /nobreak > nul
start "" "http://localhost:5233"

echo.
echo ============================================================
echo  Oba serwisy zostaly uruchomione w osobnych oknach.
echo.
echo  Strona webowa:  http://localhost:5233
echo  API (Swagger):  http://localhost:5233/swagger
echo  ML serwis:      http://localhost:5001/health
echo ============================================================
echo.
pause
