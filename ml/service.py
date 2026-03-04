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
import threading
import time
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

# ── Stan serwisu ──────────────────────────────────────────────────────────────
# Globalna ścieżka do datasetu (ustawiana przy starcie)
_dataset_path: str | None = None

# Moduł DeepFace – wczytywany raz w tle, trzymany w pamięci
_deepface = None

# Flaga gotowości: True gdy model + embeddingi są gotowe
_model_ready = False

# Ew. komunikat o błędzie podczas warmup
_warmup_error: str | None = None

# Czas startu warmup (sekund od epoch) – do informacyjnego /health
_warmup_start: float = 0.0

# Lock chroniący przed równoczesnym wywołaniem DeepFace.find()
# (zapobiega konfliktom przy przebudowie pliku .pkl)
_inference_lock = threading.Lock()

app = Flask(__name__)
CORS(app)  # pozwala na zapytania z ASP.NET / przeglądarki


# ── Warmup ────────────────────────────────────────────────────────────────────

def _warmup() -> None:
    """
    Uruchamiana w tle zaraz po starcie serwisu.

    1. Importuje DeepFace + TensorFlow (ciężki import, ~15-60 s przy pierwszym uruchomieniu).
    2. Wczytuje wagi modelu Facenet512 do pamięci.
    3. Buduje / odczytuje plik .pkl z embeddingami datasetu
       (wolne tylko raz – kolejne starty korzystają z cache).

    Po zakończeniu ustawia _model_ready = True – serwis jest gotowy do pracy.
    """
    global _deepface, _model_ready, _warmup_error

    try:
        # ── krok 1: import TF + model ───────────────────────────────────────
        print("⏳ [Warmup] Wczytywanie TensorFlow i modelu Facenet512…")
        print("   (pierwsze uruchomienie może potrwać kilka minut – pobieranie wag ~90 MB)")
        t0 = time.time()

        from deepface import DeepFace  # noqa: C0415  (intentional deferred import)

        # build_model ładuje wagi do pamięci i cachuje je w DeepFace internal dict
        DeepFace.build_model(MODEL_NAME)
        _deepface = DeepFace

        elapsed = time.time() - t0
        print(f"✅ [Warmup] Model wczytany ({elapsed:.1f} s)")

        # ── krok 2: zbuduj / wczytaj embeddingi datasetu ─────────────────────
        if _dataset_path and Path(_dataset_path).exists():
            imgs = sorted(
                f for f in Path(_dataset_path).rglob("*")
                if f.suffix.lower() in {".jpg", ".jpeg", ".png", ".bmp"}
            )
            if imgs:
                print(f"⏳ [Warmup] Budowanie bazy embeddingów ({len(imgs)} zdjęć)…")
                print("   (wolne tylko raz – wynik zapisywany w dataset/*.pkl)")
                t1 = time.time()
                try:
                    # Wywołanie z prawdziwym zdjęciem z datasetu jako query –
                    # to wymusza zbudowanie/odczytanie pliku .pkl dla WSZYSTKICH zdjęć.
                    # enforce_detection=False: jeśli wzorzec-zdjęcie nie ma wyraźnej
                    # twarzy, po prostu pomijamy błąd.
                    DeepFace.find(
                        img_path=str(imgs[0]),
                        db_path=str(_dataset_path),
                        model_name=MODEL_NAME,
                        distance_metric=DISTANCE_METRIC,
                        detector_backend=DETECTOR,
                        enforce_detection=False,
                        align=True,
                        silent=True,
                    )
                    elapsed2 = time.time() - t1
                    print(f"✅ [Warmup] Baza embeddingów gotowa ({elapsed2:.1f} s)")
                except Exception as emb_err:
                    # Nie fatal – .pkl zostanie zbudowany przy pierwszym prawdziwym zapytaniu.
                    print(f"⚠  [Warmup] Nie udało się wstępnie zbudować embeddingów: {emb_err}")
            else:
                print("ℹ  [Warmup] Dataset jest pusty – embeddingi zostaną zbudowane po dodaniu zdjęć.")
        else:
            print("ℹ  [Warmup] Brak datasetu – embeddingi zostaną zbudowane po uruchomieniu z --dataset.")

        _model_ready = True
        total = time.time() - _warmup_start
        print(f"\n🚀 [Warmup] Serwis gotowy do pracy! (łączny czas: {total:.1f} s)\n")

    except Exception as exc:
        _warmup_error = str(exc)
        print(f"\n❌ [Warmup] Błąd podczas wczytywania modelu: {exc}")
        print("   Serwis nie będzie mógł rozpoznawać twarzy. Sprawdź instalację zależności.\n")


# ── Endpointy ─────────────────────────────────────────────────────────────────

@app.route("/health", methods=["GET"])
def health():
    """Prosty endpoint diagnostyczny."""
    dataset_ok = _dataset_path is not None and Path(_dataset_path).exists()
    count = 0
    if dataset_ok:
        count = sum(1 for f in Path(_dataset_path).rglob("*")
                    if f.suffix.lower() in {".jpg", ".jpeg", ".png", ".bmp"})

    status = "ok" if _model_ready else ("error" if _warmup_error else "loading")
    elapsed = round(time.time() - _warmup_start, 1) if _warmup_start else 0

    return jsonify({
        "status": status,
        "model_ready": _model_ready,
        "warmup_error": _warmup_error,
        "warmup_elapsed_s": elapsed,
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
    # ── sprawdź gotowość modelu ──────────────────────────────────────────────
    if not _model_ready:
        if _warmup_error:
            return jsonify({
                "error": f"Model nie mógł zostać wczytany: {_warmup_error}. Sprawdź logi serwisu."
            }), 503
        elapsed = round(time.time() - _warmup_start, 1) if _warmup_start else 0
        return jsonify({
            "error": f"Serwis się jeszcze uruchamia (wczytywanie modelu, {elapsed} s)."
                     " Poczekaj chwilę i spróbuj ponownie."
        }), 503

    # ── walidacja datasetu ───────────────────────────────────────────────────
    if _dataset_path is None or not Path(_dataset_path).exists():
        return jsonify({
            "error": f"Folder z datasetem nie istnieje: {_dataset_path}. "
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

        # _inference_lock zapobiega równoczesnej przebudowie pliku .pkl
        # przez kilka równoległych żądań HTTP.
        with _inference_lock:
            results = _deepface.find(
                img_path=tmp_path,
                db_path=str(_dataset_path),
                model_name=MODEL_NAME,
                distance_metric=DISTANCE_METRIC,
                detector_backend=DETECTOR,
                enforce_detection=False,   # zamiast wyjątku – puste wyniki
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
        return jsonify({"error": f"Błąd rozpoznawania: {msg}"}), 500
    finally:
        if tmp_path:
            try:
                os.unlink(tmp_path)
            except Exception:
                pass


# ── Uruchomienie ──────────────────────────────────────────────────────────────

def _get_lan_ips() -> list[str]:
    """Return a list of non-loopback IPv4 addresses for the current machine."""
    ips: list[str] = []
    try:
        with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as s:
            s.connect(("8.8.8.8", 80))
            primary = s.getsockname()[0]
            if primary and not primary.startswith("127."):
                ips.append(primary)
    except Exception:
        pass
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
    global _dataset_path, _warmup_start

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
        print("  Utwórz folder i umieść w nim zdjęcia twarzy (np. Jan Kowalski_1.jpg)")
        print("  Serwis wystartuje, ale /recognize zwróci błąd 503 do czasu utworzenia folderu.")
    else:
        img_count = sum(1 for f in Path(_dataset_path).rglob("*")
                        if f.suffix.lower() in {".jpg", ".jpeg", ".png", ".bmp"})
        print(f"✅ Dataset: {_dataset_path} ({img_count} zdjęć)")

    lan_ips = _get_lan_ips()

    print(f"\n🚀 Serwis startuje na http://{args.host}:{args.port}")
    print(f"   POST http://localhost:{args.port}/recognize  <- wyslij zdjecie twarzy (backend)")
    print(f"   GET  http://localhost:{args.port}/health     <- diagnostyka / status warmup")

    if args.host in ("0.0.0.0", "::") and lan_ips:
        print(f"\n   Adresy sieciowe (dostępne z innych urządzeń w sieci):")
        for ip in lan_ips:
            print(f"      http://{ip}:{args.port}/recognize")

    print(f"\n💡 Aplikacja desktop/mobilna (MAUI) łączy się z backendem ASP.NET, nie z tym serwisem!")
    print(f"   URL backendu (zakodowany w aplikacji):")
    print(f"      Windows desktop:      http://localhost:{BACKEND_PORT}")
    print(f"      Emulator Android:     http://10.0.2.2:{BACKEND_PORT}")
    if lan_ips:
        for ip in lan_ips:
            print(f"      Fizyczne urządzenie:  http://{ip}:{BACKEND_PORT}")
    else:
        print(f"      Fizyczne urządzenie:  http://<IP-komputera>:{BACKEND_PORT}")

    print("\n⏳ Model wczytuje się w tle – serwis odpowiada na /health natychmiast,")
    print("   a na /recognize dopiero gdy model będzie gotowy (patrz logi).\n")

    # ── uruchom warmup w tle PRZED startem Flask ──────────────────────────────
    _warmup_start = time.time()
    warmup_thread = threading.Thread(target=_warmup, daemon=True, name="warmup")
    warmup_thread.start()

    app.run(host=args.host, port=args.port, debug=False, threaded=True)


if __name__ == "__main__":
    main()
