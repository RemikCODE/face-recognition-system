# Face Recognition System

> **TL;DR** – sklonuj repozytorium, wrzuć zdjęcia do `ml/dataset/`, uruchom dwa terminale – gotowe.

---

## ⚡ SZYBKI START (Windows)

### Krok 1 – Sklonuj repozytorium (tylko raz)

```bash
git clone https://github.com/RemikCODE/face-recognition-system.git
cd face-recognition-system
```

Albo pobierz jako ZIP z GitHub → **Code → Download ZIP** → rozpakuj.

---

### Krok 2 – Zainstaluj wymagania (tylko raz)

**Python ML serwis:**
```bash
cd ml
pip install -r requirements.txt
```

> ⚠ **Windows – błąd "Long Path"?** Zamiast powyższego użyj `ml\setup-windows.bat` (kliknij dwa razy) lub włącz długie ścieżki komendą w CMD jako administrator:  
> `reg add "HKLM\SYSTEM\CurrentControlSet\Control\FileSystem" /v LongPathsEnabled /t REG_DWORD /d 1 /f`  
> Pełna instrukcja: [`ml/README.md`](ml/README.md)

> ⚠ **Python ze sklepu Microsoft Store?** Może powodować błąd "Long Path". Pobierz [Python 3.13 lub 3.12 z python.org](https://www.python.org/downloads/).

**.NET backend** – pobierz i zainstaluj [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8) (jeśli jeszcze nie masz).

---

### Krok 3 – Pobierz dataset i wrzuć zdjęcia do `ml/dataset/`

**Dataset:** Face Recognition Dataset by Vasuki Patel  
🔗 **https://www.kaggle.com/datasets/vasukipatel/face-recognition-dataset**  
→ Kliknij **Download** na Kaggle → pobierze się plik `archive.zip`

W środku są **dwa foldery** – użyj tylko właściwego:

| Folder w ZIP | Co zawiera | Czy używać? |
|---|---|---|
| `Faces/Faces/` | Przycięte twarze (płasko, ~2800 plików) | ✅ **TAK – ten folder** |
| `Original Images/Original Images/` | Zdjęcia całego ciała (podfoldery) | ❌ nie |

**Najszybciej – skrypt:** kliknij dwa razy `ml\prepare-dataset.bat` i podaj ścieżkę do ZIPa.  
Albo z terminala:

```cmd
cd ml
python prepare-dataset.py --zip C:\Users\Ty\Downloads\archive.zip
```

**Ręcznie:** rozpakuj ZIP → wejdź do `archive\Faces\Faces\` → zaznacz wszystko (`Ctrl+A`) → skopiuj do `ml\dataset\`.

---

### Krok 4 – Uruchom serwisy (dwa terminale)

**Terminal 1 – Python ML serwis:**
```cmd
cd ml
python service.py
```

**Terminal 2 – Backend + strona webowa:**
```cmd
cd backend\FaceRecognitionApi
dotnet run
```

Otwórz przeglądarkę: `http://localhost:5233`

---

### Krok 5 – Załaduj bazę danych

Otwórz `http://localhost:5233/Persons` → kliknij **Wybierz plik** → wybierz swój plik CSV z dysku → kliknij **Załaduj**.

> Nie masz CSV? Użyj opcji **Skanuj dataset** (podaj ścieżkę do `ml/dataset/`) albo po prostu rozpoznaj kilka zdjęć – baza wypełni się sama. Szczegóły: sekcja „Jak działa wyszukiwanie w bazie?" poniżej.

> Przykładowy plik CSV: `backend/data/sample_faces.csv`

---

### Krok 6 – Rozpoznaj twarz

Otwórz `http://localhost:5233` → wgraj zdjęcie → kliknij **Recognize**.

---

## Pytania i odpowiedzi

**Czy muszę to uruchamiać ręcznie za każdym razem?**  
Tak – dwa osobne procesy, dwa terminale. Możesz je zostawić otwarte przez cały czas pracy.

**Dlaczego dwa osobne serwisy?**  
Python i .NET to dwa różne środowiska uruchomieniowe. Python obsługuje modele AI (DeepFace), .NET obsługuje bazę danych i stronę webową.

**Jak otworzyć w Visual Studio?**  
Otwórz plik `backend/FaceRecognitionSystem.slnx` w Visual Studio 2022+ – i kliknij ▶ Run.  
Ale równie dobrze działa `dotnet run` z linii poleceń.

**Czy mogę uruchomić tylko w linii poleceń bez Visual Studio?**  
Tak – wystarczą dwa okna terminala z komendami powyżej. Visual Studio nie jest wymagane.

---

---

## 🖥️ Uruchamianie ręczne

Potrzebujesz **dwóch otwartych okien terminala** (cmd / PowerShell / Terminal).

### Okno 1 – Python ML serwis

```cmd
cd ml
python service.py
```

Zostaw to okno otwarte. Serwis działa na porcie `5001`.

### Okno 2 – Backend + strona webowa

```cmd
cd backend\FaceRecognitionApi
dotnet run
```

Zostaw to okno otwarte. Po uruchomieniu otwórz przeglądarkę:  
→ `http://localhost:5233`

### Załaduj bazę (raz, po pierwszym starcie)

1. Otwórz `http://localhost:5233/Persons` w przeglądarce
2. Kliknij **Wybierz plik** w sekcji "Załaduj bazę danych z pliku CSV" i wybierz swój plik CSV z dysku
3. Kliknij **Załaduj** – baza zostanie wypełniona automatycznie

Alternatywnie – jeśli masz zdjęcia w `ml/dataset/`, użyj `POST /api/persons/scan-dataset`  
(przez Swagger: `http://localhost:5233/swagger`) aby załadować bazę wprost z nazw plików.

> Przykładowy plik CSV z 8 rekordami: `backend/data/sample_faces.csv`

---

## Cały kod – mapa plików

### 📁 `backend/FaceRecognitionApi/` – serwer + strona webowa

```
backend/
├── FaceRecognitionSystem.slnx          ← plik solucji .NET (otwórz w Visual Studio)
├── data/
│   └── sample_faces.csv                ← przykładowy plik CSV do załadowania bazy
│
└── FaceRecognitionApi/                 ← projekt ASP.NET Core 8
    ├── Program.cs                      ← punkt wejścia; DI, SQLite, CORS, Swagger, Razor Pages
    ├── appsettings.json                ← konfiguracja: connection string, URL ML serwisu, CORS
    ├── appsettings.Development.json    ← konfiguracja deweloperska (ML URL: localhost:5001)
    │
    ├── Models/
    │   ├── Person.cs                   ← encja: Id, Name, ImageFileName
    │   └── RecognitionResult.cs        ← odpowiedź API: Found, Person, Confidence, Message
    │
    ├── Data/
    │   └── AppDbContext.cs             ← Entity Framework Core; tabela Persons w SQLite
    │
    ├── Services/
    │   ├── IFaceRecognitionService.cs  ← interfejs: RecognizeAsync(stream, fileName)
    │   ├── FaceRecognitionService.cs   ← implementacja: wysyła zdjęcie do Python ML serwisu
    │   └── CsvImportService.cs         ← importuje CSV (id,label) do bazy; parsuje nazwę z pliku
    │
    ├── Controllers/
    │   ├── PersonsController.cs        ← REST: GET/POST/DELETE /api/persons; seed (ścieżka), seed-upload (plik), scan-dataset (folder)
    │   └── FacesController.cs          ← REST: POST /api/faces/recognize (upload zdjęcia)
    │
    ├── Pages/                          ← strona webowa (Razor Pages)
    │   ├── _ViewImports.cshtml         ← globalny import taghelpers i namespace
    │   ├── _ViewStart.cshtml           ← ustawia _Layout jako domyślny layout
    │   ├── Shared/
    │   │   └── _Layout.cshtml          ← wspólny layout Bootstrap 5 (navbar, footer)
    │   ├── Index.cshtml                ← strona "/": upload zdjęcia + podgląd + wynik
    │   ├── Index.cshtml.cs             ← PageModel dla Index: OnGet / OnPostAsync
    │   ├── Persons/
    │   │   ├── Index.cshtml            ← strona "/Persons": tabela z paginacją i wyszukiwarką
    │   │   └── Index.cshtml.cs         ← PageModel dla Persons: OnGetAsync(page, search)
    │
    └── wwwroot/
        └── css/site.css                ← własne style CSS (minimalne, dopełnienie Bootstrap)
```

---

### 📁 `backend/FaceRecognitionApi.Tests/` – testy automatyczne

```
FaceRecognitionApi.Tests/
├── FaceRecognitionApi.Tests.csproj     ← projekt testów xUnit
├── CsvImportServiceTests.cs            ← testy: parsowanie nazw z pliku, import CSV, zastępowanie danych
├── FaceRecognitionServiceTests.cs      ← testy: odpowiedź gdy ML serwis nie jest skonfigurowany
└── PersonsApiTests.cs                  ← testy integracyjne: GET /api/persons, seed, recognize
```

---

### 📁 `desktop/FaceRecognitionApp/` – aplikacja desktop + mobilna (.NET MAUI)

```
desktop/
├── FaceRecognitionApp.slnx             ← plik solucji MAUI
│
└── FaceRecognitionApp/
    ├── FaceRecognitionApp.csproj       ← projekt MAUI; 4 platformy: Windows, Android, iOS, macOS
    ├── MauiProgram.cs                  ← punkt wejścia MAUI; DI: ApiService, strony
    ├── App.xaml / App.xaml.cs          ← inicjalizacja aplikacji, zasoby (kolory)
    ├── AppShell.xaml / AppShell.xaml.cs← Shell z zakładkami: "Recognize" i "Settings"
    │
    ├── MainPage.xaml                   ← UI: podgląd zdjęcia, przyciski, karta wyników
    ├── MainPage.xaml.cs                ← logika: FilePicker, MediaPicker, wywołanie ApiService
    ├── SettingsPage.xaml               ← UI: pole do wpisania URL backendu
    ├── SettingsPage.xaml.cs            ← logika: zapis URL do Preferences urządzenia
    │
    ├── Models/
    │   ├── Person.cs                   ← model danych osoby
    │   └── RecognitionResult.cs        ← model odpowiedzi z backendu
    │
    ├── Services/
    │   └── ApiService.cs               ← HTTP klient: POST /api/faces/recognize; URL z Preferences
    │
    └── Platforms/                      ← pliki specyficzne dla każdej platformy
        ├── Android/
        │   ├── AndroidManifest.xml     ← uprawnienia: INTERNET, CAMERA, READ_MEDIA_IMAGES
        │   ├── MainActivity.cs         ← punkt wejścia Android
        │   └── MainApplication.cs      ← inicjalizacja MAUI na Androidzie
        ├── iOS/
        │   ├── AppDelegate.cs          ← punkt wejścia iOS
        │   └── Info.plist              ← NSCameraUsageDescription, NSPhotoLibraryUsageDescription
        ├── MacCatalyst/
        │   ├── AppDelegate.cs
        │   └── Info.plist
        └── Windows/
            ├── App.xaml / App.xaml.cs  ← punkt wejścia Windows
            └── Package.appxmanifest    ← manifest aplikacji Windows (MSIX)
```

---

### 📁 `ml/` – serwis rozpoznawania twarzy (Python)

```
ml/
├── requirements.txt          ← zależności Python (pip install -r requirements.txt)
├── service.py                ← Flask HTTP serwis na porcie 5001; przyjmuje POST /recognize
├── requirements.txt          ← zależności Python
├── README.md                 ← pełna dokumentacja ML
├── .gitignore                ← wyklucza dataset/ z gita
│
└── dataset/                  ← folder ze zdjęciami twarzy (już istnieje, wrzuć tu swoje pliki)
    ├── Robert Downey Jr_87.jpg
    ├── Scarlett Johansson_12.jpg
    └── ...                   ← nazwy plików = kolumna 'label' z CSV
```

---

## Wymagania wstępne

1. **[.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)** – wymagany do backendu i MAUI
2. **MAUI workload** – wymagany tylko do aplikacji desktop/mobile:

```bash
dotnet workload install maui
```

3. **Python 3.10–3.13** – wymagany do serwisu ML  
   ⚠ **NIE Python ze sklepu Microsoft Store** – powoduje błąd "Long Path"  
   Pobierz Python 3.13 lub 3.12 z: https://www.python.org/downloads/

   **Windows – błąd "Long Path" podczas `pip install`?**  
   Uruchom CMD jako administrator i wykonaj:
   ```cmd
   reg add "HKLM\SYSTEM\CurrentControlSet\Control\FileSystem" /v LongPathsEnabled /t REG_DWORD /d 1 /f
   ```
   Uruchom ponownie komputer. Albo użyj gotowego skryptu `ml\setup-windows.bat`.

---

## Krok 0 – Uruchom serwis ML (Python)

```bash
# 1. Zainstaluj zależności (tylko raz)
cd ml
pip install -r requirements.txt

# 2. Skopiuj zdjęcia twarzy do folderu ml/dataset/
#    (pliki muszą mieć nazwy jak w CSV, np. "Robert Downey Jr_87.jpg")
#    Folder dataset/ już istnieje w repozytorium

# 3. Uruchom serwis – nasłuchuje na porcie 5001
#    Model Facenet512 jest gotowy (wytrenowany) – nie trzeba nic trenować.
#    Przy pierwszym zapytaniu DeepFace automatycznie pobierze wagi modelu (~90 MB).
python service.py
```

> Szczegółowa dokumentacja w [`ml/README.md`](ml/README.md)

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

## Krok 2 – Załaduj dane do bazy osób

> **TL;DR – najprostszy sposób:** otwórz `http://localhost:5233/Persons`, kliknij **Wybierz plik**, wybierz CSV → **Załaduj**.  
> Alternatywnie – jeśli masz zdjęcia w `ml/dataset/`, użyj **Skanuj dataset** (patrz niżej) – baza wypełni się automatycznie z plików w folderze.  
> Jeśli nie zrobisz nic, baza i tak wypełni się sama przy pierwszym rozpoznaniu (patrz „Jak działa wyszukiwanie?" niżej).

### Sposób A – Upload CSV przez stronę webową (najłatwiejszy)

1. Otwórz `http://localhost:5233/Persons`  
2. Kliknij **Wybierz plik** w sekcji „Załaduj bazę danych z pliku CSV"  
3. Wybierz plik CSV z dysku → kliknij **Załaduj**

> Przykładowy plik CSV (8 rekordów): `backend/data/sample_faces.csv`

Format CSV (kolumny `id` i `label`):
```
id,label
1,Robert Downey Jr_87.jpg
2,Scarlett Johansson_12.jpg
```

### Sposób B – Skanuj folder `ml/dataset/` (bez CSV)

Jeśli masz już zdjęcia w `ml/dataset/`, możesz wczytać bazę bezpośrednio z nazw plików – bez przygotowywania CSV.

**Przez Swagger UI** (`http://localhost:5233/swagger`) → `POST /api/persons/scan-dataset`:
```json
{ "datasetPath": "C:\\ścieżka\\do\\ml\\dataset" }
```

**Przez curl:**
```bash
curl -X POST http://localhost:5233/api/persons/scan-dataset \
     -H "Content-Type: application/json" \
     -d "{\"datasetPath\": \"/bezwzgledna/sciezka/do/ml/dataset\"}"
```

Backend odczyta nazwy plików (np. `Akshay Kumar_87.jpg`) i wpisze je do bazy jako osoby.

### Sposób C – Podaj ścieżkę do CSV na serwerze

**Przez Swagger UI** → `POST /api/persons/seed`:
```json
{ "csvFilePath": "C:\\ścieżka\\do\\faces.csv" }
```

**Przez curl:**
```bash
curl -X POST http://localhost:5233/api/persons/seed \
     -H "Content-Type: application/json" \
     -d "{\"csvFilePath\": \"/bezwzgledna/sciezka/do/faces.csv\"}"
```

---

## Jak działa wyszukiwanie w bazie?

### Pełny przepływ rozpoznawania twarzy

```
Twoje zdjęcie
     │
     ▼
POST /api/faces/recognize         ← backend ASP.NET
     │
     ├─► Python ML serwis         ← DeepFace porównuje twarz ze zdjęciami w dataset/
     │       │
     │       └─► zwraca: { "label": "Akshay Kumar_87.jpg", "confidence": 0.94 }
     │
     ├─► Wyciągnij nazwę z etykiety:
     │       "Akshay Kumar_87.jpg"  →  "Akshay Kumar"
     │
     ├─► Wyszukaj w bazie SQLite:
     │       WHERE ImageFileName = 'Akshay Kumar_87.jpg'
     │          OR Name          = 'Akshay Kumar'
     │
     ├─► [Znaleziono] → zwróć Found=true + dane osoby
     │
     └─► [Nie znaleziono] → auto-wstaw rekord do bazy → zwróć Found=true
                           (baza rośnie sama przy każdym nowym rozpoznaniu)
```

### Skąd backend wie, czyjej twarzy szuka?

Backend **nie wykonuje sam rozpoznawania** – deleguje to do serwisu Python (DeepFace).  
DeepFace porównuje przesłane zdjęcie ze wszystkimi plikami w folderze `ml/dataset/`  
i zwraca nazwę najbardziej podobnego pliku (np. `Akshay Kumar_87.jpg`).

Backend wyciąga wtedy imię i nazwisko z nazwy pliku (usuwa sufiks `_87` i rozszerzenie)  
i szuka tego rekordu w tabeli `Persons` w SQLite.

### Dlaczego baza czasem jest pusta?

Baza `Persons` w SQLite jest **oddzielna** od folderu `ml/dataset/`.  
Możesz mieć setki zdjęć w datasecie, ale pustą bazę w SQLite – i odwrotnie.

**Jak to naprawić:**
- **Automatycznie** – wystarczy rozpoznać kilka zdjęć; backend wstawia do bazy każdą rozpoznaną osobę, której tam jeszcze nie ma.  
- **Jednorazowo** – użyj `POST /api/persons/scan-dataset` (Sposób B powyżej) aby wczytać cały dataset naraz.

### Po co dwa nowe endpointy?

| Endpoint | Problem, który rozwiązuje |
|---|---|
| `POST /api/persons/seed-upload` | Wcześniej do załadowania CSV trzeba było podać **bezwzględną ścieżkę pliku na serwerze** (`/api/persons/seed`) – trudne jeśli serwer i klient są na innych maszynach. Teraz można **przesłać plik bezpośrednio** z przeglądarki lub curl. |
| `POST /api/persons/scan-dataset` | Eliminuje potrzebę posiadania pliku CSV w ogóle – jeśli masz zdjęcia w `ml/dataset/`, baza wczyta się **wprost z nazw plików**. Szczególnie przydatne po pobraniu datasetu z Kaggle. |

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
                 │  POST /api/faces/recognize      POST /recognize (multipart)
Desktop MAUI   ──┼──────────────────────────▶  ASP.NET Backend  ──────────────▶  Python ML serwis
                 │                              (SQLite DB)                       DeepFace Facenet512
Mobilna MAUI   ──┘                              port 5233                         port 5001
                                                     │                                │
                                                     ▼                                ▼
                                               Razor Pages UI                  cache .pkl (auto)
                                               REST API /swagger               (wektory twarzy)
```
