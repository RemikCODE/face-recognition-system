# ML Service – rozpoznawanie twarzy (Python)

Serwis HTTP oparty na **DeepFace** + gotowym, wytrenowanym modelu **Facenet512**.  
Przyjmuje zdjęcie twarzy i zwraca imię osoby.  
**Nie wymaga żadnego trenowania** – wystarczy podać folder ze zdjęciami referencyjnymi.

---

## ⚠ Przed instalacją na Windows – przeczytaj

### Problem: błąd "Long Path" podczas `pip install`

```
ERROR: Could not install packages due to an OSError:
HINT: This error might have occurred since this system does not have Windows Long Path support enabled.
```

TensorFlow ma bardzo zagnieżdżone ścieżki wewnętrzne, które przekraczają domyślny limit Windows (260 znaków).

**Masz dwie opcje – wybierz jedną:**

---

#### ✅ Opcja A – Włącz długie ścieżki (zalecane, wymaga admina, 1 komenda)

Otwórz **CMD lub PowerShell jako Administrator** i wklej:

```cmd
reg add "HKLM\SYSTEM\CurrentControlSet\Control\FileSystem" /v LongPathsEnabled /t REG_DWORD /d 1 /f
```

Następnie **uruchom ponownie komputer** i wróć do normalnej instalacji:

```cmd
cd ml
pip install -r requirements.txt
python service.py
```

---

#### ✅ Opcja B – Użyj krótkiej ścieżki venv (bez admina)

Kliknij dwa razy **`ml\setup-windows.bat`** – skrypt sam:
1. Tworzy środowisko wirtualne w `C:\facerecog\venv` (krótka ścieżka)
2. Instaluje wszystkie zależności
3. Generuje `ml\run-windows.bat` do uruchamiania serwisu

Potem zamiast `python service.py` używasz:
```cmd
run-windows.bat
```

---

### Problem: Python 3.13

TensorFlow **nie obsługuje Python 3.13**. Wymagany jest Python **3.10, 3.11 lub 3.12**.

Sprawdź wersję:
```cmd
python --version
```

Jeśli masz 3.13 (lub jeśli Python był zainstalowany ze **sklepu Microsoft Store**),  
pobierz Python 3.12 ze strony: **https://www.python.org/downloads/**  
*(Podczas instalacji zaznacz "Add Python to PATH")*

---

## Jak to działa

```
Zdjęcie (JPG/PNG)
      │
      ▼
  DeepFace.find()  ← gotowy model Facenet512 (pobierany automatycznie ~90 MB)
      │              porównuje twarz z każdym zdjęciem w dataset/
      ▼
  Najlepsze dopasowanie  → zwróć label + confidence
```

DeepFace przy pierwszym wywołaniu automatycznie:
1. Pobiera wagi modelu Facenet512 (~90 MB) z internetu
2. Oblicza reprezentacje wszystkich zdjęć z datasetu i zapisuje je w pliku `.pkl` (cache)
3. Kolejne zapytania są szybkie – cache jest odczytywany z dysku

---

## Uruchomienie (3 kroki)

### Krok 1 – Zainstaluj zależności

```bash
cd ml
pip install -r requirements.txt
```

> Pobiera TensorFlow + DeepFace – tylko raz, może chwilę potrwać (~600 MB).

---

## Uruchomienie (3 kroki)

### Krok 1 – Zainstaluj zależności

```bash
cd ml
pip install -r requirements.txt
```

> Pobiera TensorFlow + DeepFace – tylko raz, może chwilę potrwać (~600 MB).

---

### Krok 2 – Przygotuj folder z zdjęciami twarzy

#### Masz pobrany plik `archive.zip`? → użyj gotowego skryptu

Twoje archiwum ma taką strukturę:

```
archive.zip
├── faces.csv                            ← plik CSV (id, label)
├── Faces/
│   └── Faces/                           ← ✅ TO JEST TEN FOLDER – przycięte twarze
│       ├── Robert Downey Jr_87.jpg      (każda osoba ma kilkadziesiąt zdjęć)
│       ├── Robert Downey Jr_23.jpg
│       └── ...
└── Original Images/
    └── Original Images/                 ← ❌ NIE UŻYWAJ – zdjęcia całego ciała
        ├── Robert Downey Jr/
        │   └── ...
        └── ...
```

**DeepFace potrzebuje tylko przyciętych twarzy** – czyli folderu `Faces/Faces/`.  
Folder `Original Images/` możesz zignorować.

**Jak skopiować zdjęcia do `ml/dataset/`:**

**Opcja A – skrypt (zalecane):** Kliknij dwa razy `ml\prepare-dataset.bat` i podaj ścieżkę do ZIPa.  
Albo z terminala:

```cmd
cd ml
python prepare-dataset.py --zip C:\Users\Ty\Downloads\archive.zip
```

**Opcja B – ręcznie:**
1. Rozpakuj `archive.zip`
2. Wejdź do `archive\Faces\Faces\`
3. Zaznacz wszystkie pliki (`Ctrl+A`) i skopiuj je do folderu `ml\dataset\`

Po skopiowaniu `ml/dataset/` powinien wyglądać tak:

```
ml/
└── dataset/
    ├── Robert Downey Jr_87.jpg
    ├── Robert Downey Jr_23.jpg
    ├── Scarlett Johansson_12.jpg
    └── ...   (wszystkie ~2800 zdjęć płasko w jednym folderze)
```

---

### Krok 3 – Uruchom serwis

```bash
cd ml
python service.py
```

Serwis startuje na **`http://localhost:5001`**.

Przy pierwszym zapytaniu DeepFace automatycznie obliczy reprezentacje zdjęć  
(może potrwać chwilę przy dużym datasecie – tylko raz, później jest cache).

---

## Opcje `service.py`

| Opcja | Domyślnie | Opis |
|-------|-----------|------|
| `--dataset` | `./dataset/` | Folder z referencyjnymi zdjęciami twarzy |
| `--port` | `5001` | Port HTTP |
| `--host` | `0.0.0.0` | Adres nasłuchiwania |

```bash
python service.py --dataset C:\moj_dataset --port 5001
```

---

## Endpoint API

### `POST /recognize`

Wyślij zdjęcie twarzy jako `multipart/form-data`, pole `image`:

```bash
curl -X POST http://localhost:5001/recognize \
     -F "image=@moje_zdjecie.jpg"
```

**Odpowiedź (znana twarz):**
```json
{ "label": "Robert Downey Jr_87.jpg", "confidence": 0.87 }
```

**Odpowiedź (nieznana twarz):**
```json
{ "label": "", "confidence": 0.0 }
```

**Odpowiedź (brak twarzy na zdjęciu):**
```json
{ "error": "Nie wykryto twarzy na zdjęciu: ..." }
```
HTTP status: `422`

---

### `GET /health`

```bash
curl http://localhost:5001/health
```

```json
{
  "status": "ok",
  "dataset": "./dataset",
  "dataset_exists": true,
  "images_in_dataset": 2800,
  "model": "Facenet512"
}
```

---

## Podłączenie do backendu ASP.NET

W `backend/FaceRecognitionApi/appsettings.Development.json` masz już ustawione:

```json
{
  "MlService": {
    "Url": "http://localhost:5001/recognize"
  }
}
```

Uruchom najpierw serwis ML, potem backend – i gotowe.

---

## Wybór modelu (opcjonalne)

Możesz zmienić model w `service.py` (zmienna `MODEL_NAME`):

| Model | Dokładność | Rozmiar |
|-------|-----------|---------|
| `Facenet512` ✅ | ⭐⭐⭐⭐ | ~90 MB |
| `ArcFace` | ⭐⭐⭐⭐⭐ | ~130 MB |
| `VGG-Face` | ⭐⭐⭐ | ~550 MB |

Domyślnie `Facenet512` – dobry kompromis.
