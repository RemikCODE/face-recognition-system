"""
service.py – serwis HTTP do rozpoznawania twarzy.

Używa gotowych, wytrenowanych modeli DeepFace (Facenet512).
Nie wymaga żadnego trenowania – wystarczy podać folder ze zdjęciami referencyjnymi.

Nasłuchuje na porcie 5001.
Przyjmuje POST /recognize z polem 'image' (multipart/form-data).
Zwraca JSON: { "label": "Robert Downey Jr_87.jpg", "confidence": 0.92 }

Użycie:
    python service.py
    python service.py --dataset ./dataset --port 5001
"""

import argparse
import os
import socket
import sys
import tempfile
from pathlib import Path

from flask import Flask, jsonify, request
from flask_cors import CORS

# ── Konfiguracja ─────────────────────────────────────────────────────────────
DEFAULT_DATASET = Path(__file__).parent / "dataset"
DEFAULT_PORT = 5001
BACKEND_PORT = 5233  # port backendu ASP.NET (FaceRecognitionApi)

# Model i metryka odległości
MODEL_NAME = "Facenet512"   # gotowy, wytrenowany model – pobierany automatycznie (~90 MB)
DETECTOR = "opencv"         # najszybszy detektor twarzy
DISTANCE_METRIC = "cosine"

# Globalna ścieżka do datasetu (ustawiana przy starcie)
_dataset_path = None

app = Flask(__name__)
CORS(app)  # pozwala na zapytania z ASP.NET / przeglądarki


@app.route("/health", methods=["GET"])
def health():
    """Prosty endpoint diagnostyczny."""
    dataset_ok = _dataset_path is not None and Path(_dataset_path).exists()
    count = 0
    if dataset_ok:
        count = sum(1 for f in Path(_dataset_path).rglob("*")
                    if f.suffix.lower() in {".jpg", ".jpeg", ".png", ".bmp"})
    return jsonify({
        "status": "ok",
        "dataset": str(_dataset_path),
        "dataset_exists": dataset_ok,
        "images_in_dataset": count,
        "model": MODEL_NAME,
    })


@app.route("/recognize", methods=["POST"])
def recognize():
    """
    Przyjmuje zdjęcie twarzy i zwraca nazwę osoby.

    Używa gotowego modelu Facenet512 (DeepFace) do porównania twarzy
    ze zdjęciami w folderze dataset/.

    Request:  POST multipart/form-data, pole 'image'
    Response: { "label": "Robert Downey Jr_87.jpg", "confidence": 0.92 }
              lub { "label": "", "confidence": 0.0 } gdy twarz nieznana
    """
    if _dataset_path is None or not Path(_dataset_path).exists():
        return jsonify({
            "error": f"Folder z datasystem nie istnieje: {_dataset_path}. "
                     "Utwórz folder dataset/ i umieść w nim zdjęcia twarzy."
        }), 503

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

        # Importujemy tu, żeby TensorFlow nie spowalniał startu serwisu
        from deepface import DeepFace

        # DeepFace.find() używa gotowego, wytrenowanego modelu.
        # Przy pierwszym wywołaniu automatycznie oblicza i cachuje reprezentacje
        # wszystkich zdjęć z datasetu w pliku .pkl (następne wywołania są szybkie).
        results = DeepFace.find(
            img_path=tmp_path,
            db_path=str(_dataset_path),
            model_name=MODEL_NAME,
            distance_metric=DISTANCE_METRIC,
            detector_backend=DETECTOR,
            enforce_detection=True,
            align=True,
            silent=True,
        )

        # results to lista DataFrames (po jednym na każdą wykrytą twarz).
        # Bierzemy pierwsze dopasowanie dla pierwszej wykrytej twarzy.
        if not results or results[0].empty:
            return jsonify({"label": "", "confidence": 0.0})

        df = results[0].sort_values("distance")
        best = df.iloc[0]

        # Wyciągnij samą nazwę pliku (bez pełnej ścieżki)
        label = Path(str(best["identity"])).name

        # DeepFace zwraca confidence w skali 0-100; normalizujemy do 0.0-1.0
        raw_confidence = float(best.get("confidence", 0))
        confidence = round(raw_confidence / 100.0, 4)

        return jsonify({"label": label, "confidence": confidence})

    except Exception as exc:
        msg = str(exc)
        # Zwykły błąd braku twarzy → 422; błędy serwisu → 500
        if "Face could not be detected" in msg or "No face" in msg.lower():
            return jsonify({"error": f"Nie wykryto twarzy na zdjęciu: {msg}"}), 422
        return jsonify({"error": f"Błąd rozpoznawania: {msg}"}), 500
    finally:
        if tmp_path:
            try:
                os.unlink(tmp_path)
            except Exception:
                pass


def _get_lan_ips() -> list[str]:
    """Return a list of non-loopback IPv4 addresses for the current machine."""
    ips: list[str] = []
    try:
        # Connect to an external address (no data is sent) to discover the outbound IP
        with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as s:
            s.connect(("8.8.8.8", 80))
            primary = s.getsockname()[0]
            if primary and not primary.startswith("127."):
                ips.append(primary)
    except Exception:
        pass
    # Fallback: enumerate all addresses via getaddrinfo
    try:
        hostname = socket.gethostname()
        for info in socket.getaddrinfo(hostname, None, socket.AF_INET):
            addr = info[4][0]
            if addr and not addr.startswith("127.") and addr not in ips:
                ips.append(addr)
    except Exception:
        pass
    return ips


def main():
    global _dataset_path

    parser = argparse.ArgumentParser(
        description="Serwis rozpoznawania twarzy (gotowy model Facenet512, bez trenowania)"
    )
    parser.add_argument("--dataset", default=str(DEFAULT_DATASET),
                        help=f"Folder z referencyjnymi zdjęciami twarzy (domyslnie: {DEFAULT_DATASET})")
    parser.add_argument("--port", type=int, default=DEFAULT_PORT,
                        help=f"Port HTTP (domyslnie: {DEFAULT_PORT})")
    parser.add_argument("--host", default="0.0.0.0",
                        help="Adres nasluchiwania (domyslnie: 0.0.0.0)")
    args = parser.parse_args()

    _dataset_path = args.dataset

    if not Path(_dataset_path).exists():
        print(f"⚠ Folder datasetu nie istnieje: {_dataset_path}")
        print("  Utwórz folder i umieść w nim zdjęcia twarzy (np. Robert Downey Jr_87.jpg)")
        print("  Serwis wystartuje, ale /recognize zwróci błąd 503 do czasu utworzenia folderu.")
    else:
        img_count = sum(1 for f in Path(_dataset_path).rglob("*")
                        if f.suffix.lower() in {".jpg", ".jpeg", ".png", ".bmp"})
        print(f"✅ Dataset: {_dataset_path} ({img_count} zdjęć)")
        print(f"   Model:   {MODEL_NAME} (gotowy, wytrenowany – pobierany automatycznie przy pierwszym użyciu)")

    lan_ips = _get_lan_ips()

    print(f"\n🚀 Serwis startuje na http://{args.host}:{args.port}")
    print(f"   POST http://localhost:{args.port}/recognize  <- wyslij zdjecie twarzy (backend)")
    print(f"   GET  http://localhost:{args.port}/health     <- diagnostyka")

    # Detect and display LAN IP addresses when listening on all interfaces
    if args.host in ("0.0.0.0", "::") and lan_ips:
        print(f"\n   Adresy sieciowe (dostępne z innych urządzeń w sieci):")
        for ip in lan_ips:
            print(f"      http://{ip}:{args.port}/recognize")

    print(f"\n💡 Aplikacja desktop/mobilna (MAUI) łączy się z backendem ASP.NET, nie z tym serwisem!")
    print(f"   W aplikacji (zakładka Settings) ustaw URL backendu:")
    print(f"      Windows desktop:      http://localhost:{BACKEND_PORT}")
    print(f"      Emulator Android:     http://10.0.2.2:{BACKEND_PORT}")
    if lan_ips:
        for ip in lan_ips:
            print(f"      Fizyczne urządzenie:  http://{ip}:{BACKEND_PORT}")
    else:
        print(f"      Fizyczne urządzenie:  http://<IP-komputera>:{BACKEND_PORT}")
    print()

    app.run(host=args.host, port=args.port, debug=False)


if __name__ == "__main__":
    main()
