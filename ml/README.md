# ML Service – rozpoznawanie twarzy (Python)

Serwis HTTP oparty na **DeepFace** + **Facenet512** przyjmuje zdjęcie twarzy,  
porównuje je z bazą embeddingów i zwraca imię osoby.

---

## Wymagania

- Python 3.10+
- pip

---

## Krok 1 – Zainstaluj zależności

```bash
cd ml
pip install -r requirements.txt
```

> Instalacja pobiera TensorFlow (~600 MB) i modele DeepFace (~250 MB) – tylko raz.

---

## Krok 2 – Przygotuj dataset

Utwórz folder `ml/dataset/` i umieść w nim **zdjęcia twarzy**.  
Nazwa pliku musi odpowiadać kolumnie `label` z CSV:

```
ml/
└── dataset/
    ├── Robert Downey Jr_87.jpg
    ├── Robert Downey Jr_23.jpg
    ├── Scarlett Johansson_12.jpg
    └── ...
```

Dataset opisany w zadaniu (CSV z ~2800 rekordów) ma format `Imię Nazwisko_N.jpg`.  
Możesz umieścić pliki płasko lub w podfolderach – skrypt znajdzie je rekurencyjnie.

---

## Krok 3 – Oblicz embeddingi (trenowanie)

```bash
cd ml
python train.py
```

Domyślnie szuka:
- CSV: `../backend/data/sample_faces.csv`
- Obrazów: `./dataset/`

Możesz podać własne ścieżki:

```bash
python train.py --dataset C:\moj_dataset --csv C:\faces.csv
```

Po zakończeniu powstaje plik `ml/embeddings.pkl` (~kilka MB).

### Opcje `train.py`

| Opcja | Domyślnie | Opis |
|-------|-----------|------|
| `--dataset` | `./dataset/` | Folder ze zdjęciami |
| `--csv` | `../backend/data/sample_faces.csv` | Plik CSV |
| `--output` | `./embeddings.pkl` | Plik wyjściowy |
| `--model` | `Facenet512` | Model: `Facenet512`, `ArcFace`, `VGG-Face` |
| `--detector` | `opencv` | Detektor: `opencv`, `retinaface`, `mtcnn` |

---

## Krok 4 – Uruchom serwis

```bash
cd ml
python service.py
```

Serwis startuje na **`http://localhost:5001`**.

### Opcje `service.py`

| Opcja | Domyślnie | Opis |
|-------|-----------|------|
| `--embeddings` | `./embeddings.pkl` | Plik z embeddingami |
| `--port` | `5001` | Port HTTP |
| `--threshold` | `0.40` | Próg odległości kosinusowej (niżej = bardziej restrykcyjny) |

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
{ "error": "Nie wykryto twarzy: ..." }
```
Status: `422`

### `GET /health`

```bash
curl http://localhost:5001/health
```

```json
{
  "status": "ok",
  "embeddings_loaded": true,
  "embeddings_count": 2800,
  "model": "Facenet512"
}
```

---

## Podłączenie do backendu ASP.NET

Po uruchomieniu serwisu ML, w `backend/FaceRecognitionApi/appsettings.Development.json` upewnij się, że:

```json
{
  "MlService": {
    "Url": "http://localhost:5001/recognize"
  }
}
```

(To jest już domyślnie ustawione.)

---

## Jak to działa

```
Zdjęcie (JPG/PNG)
      │
      ▼
  DeepFace.represent()  ← detekcja twarzy (OpenCV) + embedding (Facenet512, 512 wymiarów)
      │
      ▼
  Porównanie kosinusowe  ← z każdym embeddingiem w embeddings.pkl
      │
      ▼
  Najbliższy sąsiad  ← jeśli odległość < threshold (0.40) → zwróć label
```

---

## Wybór modelu

| Model | Dokładność | Szybkość | Rozmiar |
|-------|-----------|---------|---------|
| `Facenet512` | ⭐⭐⭐⭐ | ⭐⭐⭐ | ~90 MB |
| `ArcFace` | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ | ~130 MB |
| `VGG-Face` | ⭐⭐⭐ | ⭐⭐ | ~550 MB |

Domyślnie używamy `Facenet512` – dobry kompromis między dokładnością a rozmiarem.
