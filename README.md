# Face Recognition System

Aplikacja do rozpoznawania twarzy zbudowana z trzech warstw:

| Warstwa | Technologia | Folder |
|---------|-------------|--------|
| Backend + Web UI | ASP.NET Core 8 (Razor Pages + REST API) | `backend/` |
| Desktop (Windows) | .NET MAUI 8 | `desktop/` |
| Mobilna (Android / iOS) | .NET MAUI 8 | `desktop/` (ten sam projekt) |

---

## Wymagania wstępne

1. **[.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)** – wymagany do backendu i MAUI
2. **MAUI workload** – wymagany tylko do aplikacji desktop/mobile:

```bash
dotnet workload install maui
```

---

## Krok 1 – Uruchom backend i stronę webową

```bash
cd backend/FaceRecognitionApi
dotnet run
```

Po uruchomieniu dostępne są trzy adresy:

| Adres | Co otwiera |
|-------|-----------|
| `http://localhost:5233/` | Strona webowa – wgraj zdjęcie i rozpoznaj twarz |
| `http://localhost:5233/Persons` | Baza osób (przeglądarka + wyszukiwarka) |
| `http://localhost:5233/swagger` | Dokumentacja REST API (Swagger UI) |

> Baza SQLite (`face_recognition.db`) tworzy się **automatycznie** przy pierwszym starcie – nie musisz nic konfigurować.

---

## Krok 2 – Załaduj dane z CSV do bazy

Po uruchomieniu backendu wgraj dane z pliku CSV (przygotowany przykład w `backend/data/sample_faces.csv`):

**Przez Swagger UI** – otwórz `http://localhost:5233/swagger`, znajdź `POST /api/persons/seed`, kliknij "Try it out" i wklej:

```json
{
  "csvFilePath": "C:\\ścieżka\\do\\pliku\\faces.csv"
}
```

**Lub przez curl** (Linux/macOS/PowerShell):

```bash
curl -X POST http://localhost:5233/api/persons/seed \
     -H "Content-Type: application/json" \
     -d "{\"csvFilePath\": \"/bezwzgledna/sciezka/do/faces.csv\"}"
```

Format CSV (kolumny `id` i `label`):

```
id,label
1,Robert Downey Jr_87.jpg
2,Scarlett Johansson_12.jpg
```

---

## Krok 3 – Uruchom aplikację desktop (Windows)

```bash
cd desktop/FaceRecognitionApp
dotnet run -f net8.0-windows10.0.19041.0
```

Po uruchomieniu:
1. Otwórz zakładkę **Settings** i upewnij się, że URL to `http://localhost:5233`
2. Przejdź na zakładkę **Recognize**
3. Kliknij **📁 Select File**, wybierz zdjęcie twarzy
4. Kliknij **Recognize Face** – wynik pojawi się pod zdjęciem

---

## Krok 4 – Uruchom aplikację mobilną (Android)

### Na emulatorze Android

```bash
cd desktop/FaceRecognitionApp
dotnet run -f net8.0-android
```

Backend jest dostępny z emulatora pod adresem `http://10.0.2.2:5233` (domyślny URL w aplikacji).

### Na prawdziwym urządzeniu

1. Podłącz telefon przez USB (włącz debugowanie USB)
2. Sprawdź swoje IP w sieci lokalnej (np. `192.168.1.100`)
3. Uruchom aplikację:

```bash
dotnet run -f net8.0-android
```

4. W aplikacji otwórz **Settings** i zmień URL na `http://192.168.1.100:5233`

### iOS / macOS

Wymagane: macOS + Xcode

```bash
dotnet run -f net8.0-ios          # iPhone/iPad
dotnet run -f net8.0-maccatalyst  # macOS
```

---

## Konfiguracja modelu ML (opcjonalne)

Domyślnie rozpoznawanie twarzy zwraca odpowiedź "ML service is not configured". Żeby w pełni działało rozpoznawanie, należy podpiąć serwis Python:

1. Uruchom serwis Python ML na porcie `5001` – musi przyjmować `POST multipart/form-data` z polem `image` i zwracać:

```json
{ "label": "Robert Downey Jr_87.jpg", "confidence": 0.92 }
```

2. W `backend/FaceRecognitionApi/appsettings.Development.json` ustaw (domyślnie już ustawione):

```json
{
  "MlService": {
    "Url": "http://localhost:5001/recognize"
  }
}
```

---

## Testy

```bash
cd backend
dotnet test FaceRecognitionSystem.slnx
```

---

## Architektura

```
Strona webowa  ──┐
                 │  POST /api/faces/recognize
Desktop MAUI   ──┼──────────────────────────▶  ASP.NET Backend  ──▶  Python ML service
                 │                              (SQLite DB)            (port 5001)
Mobilna MAUI   ──┘

Strona webowa + REST API + baza SQLite żyją w jednym procesie (port 5233)
```
