# ML Service – rozpoznawanie twarzy (Python)

Serwis HTTP oparty na **DeepFace** + gotowym, wytrenowanym modelu **Facenet512**.  
Przyjmuje zdjęcie twarzy i zwraca imię osoby.  
**Nie wymaga żadnego trenowania** – wystarczy podać folder ze zdjęciami referencyjnymi.

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

**Windows (problem z długimi ścieżkami):**

Jeśli pojawi się błąd `Could not install packages due to an OSError`, włącz obsługę długich ścieżek:

```cmd
reg add "HKLM\SYSTEM\CurrentControlSet\Control\FileSystem" /v LongPathsEnabled /t REG_DWORD /d 1 /f
```

Następnie uruchom ponownie komputer i ponów instalację.  
Alternatywnie kliknij dwa razy **`ml\setup-windows.bat`** – skrypt zainstaluje zależności w `C:\facerecog\venv`.

---

### Krok 2 – Przygotuj folder ze zdjęciami twarzy

1. Zgromadź zdjęcia twarzy (przycięte, jedna twarz na zdjęcie).
2. Nazwij je według schematu: `ImięNazwisko_numer.jpg`  
   np. `Jan Kowalski_1.jpg`, `Anna Nowak_2.png`
3. Spakuj wszystkie zdjęcia do jednego pliku ZIP.
4. Uruchom skrypt przygotowujący dataset:

```bash
cd ml
python prepare-dataset.py --zip C:\sciezka\do\moje_twarze.zip
```

Lub kliknij dwa razy **`ml\prepare-dataset.bat`** i podaj ścieżkę do ZIPa.

Po uruchomieniu folder `ml/dataset/` będzie wyglądał tak:

```
ml/
└── dataset/
    ├── Jan Kowalski_1.jpg
    ├── Jan Kowalski_2.jpg
    ├── Anna Nowak_1.png
    └── ...
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
{ "label": "Jan Kowalski_1.jpg", "confidence": 0.87 }
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
  "images_in_dataset": 150,
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
| `Facenet512` | wysoka | ~90 MB |
| `ArcFace` | bardzo wysoka | ~130 MB |
| `VGG-Face` | średnia | ~550 MB |

Domyślnie `Facenet512` – dobry kompromis.
