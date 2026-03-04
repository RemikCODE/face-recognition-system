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

DeepFace przy **pierwszym** uruchomieniu automatycznie:
1. Pobiera wagi modelu Facenet512 (~90 MB) z internetu
2. Oblicza reprezentacje wszystkich zdjęć z datasetu i zapisuje je w pliku `.pkl` (cache)

**Od drugiego uruchomienia** serwis jest gotowy w kilkanaście sekund – model i embeddingi
ładują się z cache.

> **Warmup:** po uruchomieniu serwis od razu odpowiada na `/health`, ale `/recognize`
> zwróci `503` dopóki model nie zostanie załadowany do pamięci. Postęp widać w konsoli.

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
3. Skopiuj zdjęcia do folderu `ml/dataset/`:

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

```
⏳ [Warmup] Wczytywanie TensorFlow i modelu Facenet512…
   (pierwsze uruchomienie może potrwać kilka minut – pobieranie wag ~90 MB)
✅ [Warmup] Model wczytany (45.2 s)
⏳ [Warmup] Budowanie bazy embeddingów (12 zdjęć)…
✅ [Warmup] Baza embeddingów gotowa (8.3 s)
🚀 [Warmup] Serwis gotowy do pracy! (łączny czas: 53.5 s)
```

Od drugiego uruchomienia model i embeddingi ładują się z cache – typowo **10-20 s**.

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

**Odpowiedź (nieznana twarz / brak twarzy):**
```json
{ "label": "", "confidence": 0.0 }
```

**Odpowiedź (model jeszcze się wczytuje):**
```json
{ "error": "Serwis się jeszcze uruchamia (wczytywanie modelu, 12.3 s). Poczekaj chwilę i spróbuj ponownie." }
```
HTTP status: `503`

---

### `GET /health`

```bash
curl http://localhost:5001/health
```

```json
{
  "status": "ok",
  "model_ready": true,
  "warmup_error": null,
  "warmup_elapsed_s": 53.5,
  "dataset": "./dataset",
  "dataset_exists": true,
  "images_in_dataset": 12,
  "model": "Facenet512"
}
```

`status` przyjmuje wartości: `"loading"` | `"ok"` | `"error"`

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

Domyślnie `Facenet512` – dobry kompromis między dokładnością a szybkością.
