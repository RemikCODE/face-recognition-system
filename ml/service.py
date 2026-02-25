"""
service.py – serwis HTTP do rozpoznawania twarzy.

Nasłuchuje na porcie 5001.
Przyjmuje POST /recognize z polem 'image' (multipart/form-data).
Zwraca JSON: { "label": "Robert Downey Jr_87.jpg", "confidence": 0.92 }

Użycie:
    python service.py
    python service.py --embeddings embeddings.pkl --port 5001
"""

import argparse
import os
import pickle
import sys
import tempfile
from pathlib import Path

import numpy as np
from flask import Flask, jsonify, request
from flask_cors import CORS

# ── Konfiguracja ─────────────────────────────────────────────────────────────
DEFAULT_EMBEDDINGS = Path(__file__).parent / "embeddings.pkl"
DEFAULT_PORT = 5001

# Próg podobieństwa kosinusowego (0 = identyczne, 2 = zupełnie różne)
# Dla Facenet512 typowo: <0.40 = ta sama osoba
COSINE_THRESHOLD = 0.40

MODEL_NAME = "Facenet512"
DETECTOR = "opencv"

# ── Globalne dane (ładowane raz przy starcie) ─────────────────────────────────
_embeddings_data = None
_embeddings_matrix = None   # shape: (N, D)
_labels = []

app = Flask(__name__)
CORS(app)  # pozwala na zapytania z ASP.NET / przeglądarki


def load_embeddings(path):
    """Ładuje embeddingi z pliku pkl do pamięci."""
    global _embeddings_data, _embeddings_matrix, _labels, MODEL_NAME, DETECTOR

    if not Path(path).exists():
        print(f"❌ Brak pliku embeddingów: {path}", file=sys.stderr)
        return False

    with open(path, "rb") as f:
        _embeddings_data = pickle.load(f)

    MODEL_NAME = _embeddings_data.get("model", MODEL_NAME)
    DETECTOR = _embeddings_data.get("detector", DETECTOR)
    records = _embeddings_data["embeddings"]

    _labels = [r["label"] for r in records]
    _embeddings_matrix = np.stack([r["embedding"] for r in records])  # (N, D)

    print(f"✅ Załadowano {len(_labels)} embeddingów (model: {MODEL_NAME})")
    return True


def find_best_match(query_embedding):
    """Zwraca (label, confidence) dla najbardziej podobnej twarzy w bazie."""
    if _embeddings_matrix is None or len(_labels) == 0:
        return None, 0.0

    q = query_embedding / (np.linalg.norm(query_embedding) + 1e-10)
    db = _embeddings_matrix / (np.linalg.norm(_embeddings_matrix, axis=1, keepdims=True) + 1e-10)
    distances = 1.0 - db @ q  # cosine distance (N,)

    best_idx = int(np.argmin(distances))
    best_dist = float(distances[best_idx])

    if best_dist > COSINE_THRESHOLD:
        return None, float(max(0.0, 1.0 - best_dist / COSINE_THRESHOLD))

    confidence = float(max(0.0, 1.0 - best_dist / COSINE_THRESHOLD))
    return _labels[best_idx], confidence


@app.route("/health", methods=["GET"])
def health():
    """Prosty endpoint diagnostyczny."""
    loaded = _embeddings_matrix is not None
    return jsonify({
        "status": "ok",
        "embeddings_loaded": loaded,
        "embeddings_count": len(_labels) if loaded else 0,
        "model": MODEL_NAME,
    })


@app.route("/recognize", methods=["POST"])
def recognize():
    """
    Przyjmuje zdjęcie twarzy i zwraca nazwę osoby.

    Request:  POST multipart/form-data, pole 'image'
    Response: { "label": "Robert Downey Jr_87.jpg", "confidence": 0.92 }
              lub { "label": "", "confidence": 0.0 } gdy twarz nieznana
    """
    if _embeddings_matrix is None:
        return jsonify({"error": "Embeddingi nie są załadowane. Uruchom najpierw train.py."}), 503

    if "image" not in request.files:
        return jsonify({"error": "Brakuje pola 'image' w formularzu."}), 400

    file = request.files["image"]
    if file.filename == "":
        return jsonify({"error": "Przesłano pusty plik."}), 400

    suffix = Path(file.filename).suffix or ".jpg"
    tmp_path = None
    try:
        with tempfile.NamedTemporaryFile(suffix=suffix, delete=False) as tmp:
            file.save(tmp.name)
            tmp_path = tmp.name

        from deepface import DeepFace  # importujemy tu, żeby TF nie blokował startu serwisu
        result = DeepFace.represent(
            img_path=tmp_path,
            model_name=MODEL_NAME,
            detector_backend=DETECTOR,
            enforce_detection=True,
            align=True,
        )
        query_emb = np.array(result[0]["embedding"])

    except Exception as exc:
        return jsonify({"error": f"Nie wykryto twarzy: {exc}"}), 422
    finally:
        if tmp_path:
            try:
                os.unlink(tmp_path)
            except Exception:
                pass

    label, confidence = find_best_match(query_emb)

    if label is None:
        return jsonify({"label": "", "confidence": 0.0})

    return jsonify({"label": label, "confidence": round(confidence, 4)})


def main():
    global COSINE_THRESHOLD

    parser = argparse.ArgumentParser(description="Serwis rozpoznawania twarzy")
    parser.add_argument("--embeddings", default=str(DEFAULT_EMBEDDINGS),
                        help=f"Plik z embeddingami (domyslnie: {DEFAULT_EMBEDDINGS})")
    parser.add_argument("--port", type=int, default=DEFAULT_PORT,
                        help=f"Port HTTP (domyslnie: {DEFAULT_PORT})")
    parser.add_argument("--host", default="0.0.0.0",
                        help="Adres nasluchiwania (domyslnie: 0.0.0.0)")
    parser.add_argument("--threshold", type=float, default=COSINE_THRESHOLD,
                        help=f"Prog odleglosci kosinusowej (domyslnie: {COSINE_THRESHOLD})")
    args = parser.parse_args()

    COSINE_THRESHOLD = args.threshold

    if not load_embeddings(args.embeddings):
        print("⚠ Uruchomiono bez embeddingów. Uruchom najpierw: python train.py")
        print("  Serwis wystartuje, ale /recognize zwróci błąd 503.")

    print(f"\n🚀 Serwis startuje na http://{args.host}:{args.port}")
    print(f"   POST http://localhost:{args.port}/recognize  <- wyslij zdjecie twarzy")
    print(f"   GET  http://localhost:{args.port}/health     <- diagnostyka\n")

    app.run(host=args.host, port=args.port, debug=False)


if __name__ == "__main__":
    main()
