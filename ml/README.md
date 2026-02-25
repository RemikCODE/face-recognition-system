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

---

### Krok 2 – Przygotuj folder z zdjęciami referencyjnymi

Utwórz folder `ml/dataset/` i wrzuć do niego **zdjęcia twarzy ze swojego datasetu**.  
Nazwy plików muszą odpowiadać kolumnie `label` z CSV:

```
ml/
└── dataset/
    ├── Robert Downey Jr_87.jpg
    ├── Robert Downey Jr_23.jpg
    ├── Scarlett Johansson_12.jpg
    └── ...   (wszystkie ~2800 zdjęć)
```

> Możesz umieścić pliki płasko lub w podfolderach – DeepFace znajdzie je rekurencyjnie.

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
