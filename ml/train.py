"""
train.py – oblicza embeddingi twarzy z datasetu i zapisuje je do pliku embeddings.pkl.

Użycie:
    python train.py --dataset <folder_z_obrazami> --csv <plik.csv>

Wymagania dotyczące folderu z obrazami:
    Każdy plik powinien mieć nazwę dokładnie taką jak kolumna 'label' w CSV,
    np. "Robert Downey Jr_87.jpg".
    Można je umieścić płasko w jednym folderze LUB w podfolderach – skrypt
    rekurencyjnie szuka wszystkich plików .jpg/.jpeg/.png/.bmp.

Format CSV (z nagłówkiem):
    id,label
    1,Robert Downey Jr_87.jpg
    2,Scarlett Johansson_12.jpg
"""

import argparse
import csv
import os
import pickle
import sys
from pathlib import Path

import numpy as np
from deepface import DeepFace

# ── Domyślne ścieżki ─────────────────────────────────────────────────────────
DEFAULT_CSV = Path(__file__).parent.parent / "backend" / "data" / "sample_faces.csv"
DEFAULT_DATASET = Path(__file__).parent / "dataset"
DEFAULT_OUTPUT = Path(__file__).parent / "embeddings.pkl"

# Model i backend detekcji twarzy
MODEL_NAME = "Facenet512"       # dokładny, szybki model 512-wymiarowy
DETECTOR = "opencv"             # najszybszy detektor; można zmienić na "retinaface"
DISTANCE_METRIC = "cosine"


def find_image(dataset_dir: Path, filename: str) -> Path | None:
    """Rekurencyjnie szuka pliku 'filename' w drzewie dataset_dir."""
    for path in dataset_dir.rglob(filename):
        return path
    # Próba dopasowania bez rozróżniania wielkości liter
    lower = filename.lower()
    for path in dataset_dir.rglob("*"):
        if path.name.lower() == lower:
            return path
    return None


def compute_embedding(image_path: str) -> np.ndarray | None:
    """Zwraca embedding twarzy dla danego obrazu lub None jeśli brak twarzy."""
    try:
        result = DeepFace.represent(
            img_path=image_path,
            model_name=MODEL_NAME,
            detector_backend=DETECTOR,
            enforce_detection=True,
            align=True,
        )
        # DeepFace.represent zwraca listę wykrytych twarzy; bierzemy pierwszą
        return np.array(result[0]["embedding"])
    except Exception as exc:
        print(f"  ⚠ Pominięto {image_path}: {exc}", file=sys.stderr)
        return None


def main():
    parser = argparse.ArgumentParser(description="Trenuj (oblicz embeddingi) model rozpoznawania twarzy")
    parser.add_argument("--dataset", default=str(DEFAULT_DATASET),
                        help=f"Folder z obrazami twarzy (domyślnie: {DEFAULT_DATASET})")
    parser.add_argument("--csv", default=str(DEFAULT_CSV),
                        help=f"Plik CSV z kolumnami id,label (domyślnie: {DEFAULT_CSV})")
    parser.add_argument("--output", default=str(DEFAULT_OUTPUT),
                        help=f"Plik wyjściowy z embeddingami (domyślnie: {DEFAULT_OUTPUT})")
    parser.add_argument("--model", default=MODEL_NAME,
                        help="Model DeepFace: Facenet512, VGG-Face, ArcFace, ... (domyślnie: Facenet512)")
    parser.add_argument("--detector", default=DETECTOR,
                        help="Detektor twarzy: opencv, retinaface, mtcnn (domyślnie: opencv)")
    args = parser.parse_args()

    dataset_dir = Path(args.dataset)
    csv_path = Path(args.csv)
    output_path = Path(args.output)

    if not csv_path.exists():
        sys.exit(f"❌ Plik CSV nie istnieje: {csv_path}")
    if not dataset_dir.exists():
        sys.exit(f"❌ Folder z datasystem nie istnieje: {dataset_dir}\n"
                 f"   Stwórz folder i umieść w nim zdjęcia twarzy.")

    print(f"📂 Dataset:  {dataset_dir}")
    print(f"📄 CSV:      {csv_path}")
    print(f"🧠 Model:    {args.model}")
    print(f"🔍 Detektor: {args.detector}")

    # Wczytaj CSV
    records: list[dict] = []
    with open(csv_path, newline="", encoding="utf-8") as f:
        reader = csv.DictReader(f)
        for row in reader:
            records.append({"id": int(row["id"]), "label": row["label"].strip()})

    print(f"\n📋 Znaleziono {len(records)} rekordów w CSV. Obliczam embeddingi...\n")

    embeddings: list[dict] = []
    skipped = 0

    for i, rec in enumerate(records, 1):
        label = rec["label"]
        img_path = find_image(dataset_dir, label)

        if img_path is None:
            print(f"  [{i:4}/{len(records)}] ⚠ Nie znaleziono obrazu: {label}")
            skipped += 1
            continue

        print(f"  [{i:4}/{len(records)}] 🔄 {label}")
        emb = compute_embedding(str(img_path))
        if emb is not None:
            embeddings.append({
                "id": rec["id"],
                "label": label,
                "embedding": emb,
            })
        else:
            skipped += 1

    if not embeddings:
        sys.exit("❌ Brak embeddingów. Sprawdź czy folder dataset zawiera odpowiednie pliki.")

    # Zapisz embeddingi
    data = {
        "model": args.model,
        "detector": args.detector,
        "distance_metric": DISTANCE_METRIC,
        "embeddings": embeddings,
    }
    with open(output_path, "wb") as f:
        pickle.dump(data, f)

    print(f"\n✅ Zapisano {len(embeddings)} embeddingów → {output_path}")
    if skipped:
        print(f"   (pominięto {skipped} rekordów – brak pliku lub brak twarzy)")


if __name__ == "__main__":
    main()
