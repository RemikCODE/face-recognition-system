"""
prepare-dataset.py – kopiuje zdjęcia twarzy z pobranego archiwum do folderu ml/dataset/.

Struktura archiwum (po rozpakowaniu):
  archive/
  ├── faces.csv                           ← plik CSV z kolumnami: id, label
  ├── Faces/
  │   └── Faces/                          ← ✅ UŻYWAMY TEN FOLDER
  │       ├── Robert Downey Jr_87.jpg     (przycięte twarze – idealne dla DeepFace)
  │       ├── Scarlett Johansson_12.jpg
  │       └── ...
  └── Original Images/
      └── Original Images/               ← ❌ NIE UŻYWAMY (zdjęcia całego ciała)
          ├── Robert Downey Jr/
          │   └── Robert Downey Jr_87.jpg
          └── ...

Użycie:
  python prepare-dataset.py                          # pyta interaktywnie o ścieżkę
  python prepare-dataset.py --zip C:\\pobrane\\archive.zip
  python prepare-dataset.py --dir C:\\rozpakowane\\archive
"""

import argparse
import shutil
import sys
import zipfile
from pathlib import Path

DATASET_DIR = Path(__file__).parent / "dataset"
IMAGE_EXTS = {".jpg", ".jpeg", ".png", ".bmp", ".webp"}

# Folder wewnątrz archiwum z przyciętymi twarzami
FACES_SUBDIR_CANDIDATES = [
    "Faces/Faces",
    "Faces\\Faces",
    "faces/faces",
    "Faces",
    "faces",
]


def find_faces_dir(root: Path) -> Path | None:
    """Szuka folderu z przyciętymi twarzami wewnątrz rozpakowanego archiwum."""
    for candidate in FACES_SUBDIR_CANDIDATES:
        p = root / candidate
        if p.exists() and p.is_dir():
            # Sprawdź czy zawiera pliki graficzne bezpośrednio (nie w podfolderach)
            imgs = [f for f in p.iterdir() if f.is_file() and f.suffix.lower() in IMAGE_EXTS]
            if imgs:
                return p
    # Fallback: szukaj rekurencyjnie folderu "Faces" z plikami bezpośrednio
    for d in root.rglob("*"):
        if d.is_dir() and d.name.lower() == "faces":
            imgs = [f for f in d.iterdir() if f.is_file() and f.suffix.lower() in IMAGE_EXTS]
            if imgs:
                return d
    return None


def count_images(folder: Path) -> int:
    return sum(1 for f in folder.iterdir() if f.is_file() and f.suffix.lower() in IMAGE_EXTS)


def copy_to_dataset(source_dir: Path, dest_dir: Path) -> int:
    """Kopiuje zdjęcia z source_dir do dest_dir. Zwraca liczbę skopiowanych plików."""
    dest_dir.mkdir(parents=True, exist_ok=True)
    copied = 0
    skipped = 0

    images = sorted(f for f in source_dir.iterdir() if f.is_file() and f.suffix.lower() in IMAGE_EXTS)
    total = len(images)

    print(f"\nKopiowanie {total} zdjęć do {dest_dir} ...")
    for i, img in enumerate(images, 1):
        dest = dest_dir / img.name
        if dest.exists():
            skipped += 1
        else:
            shutil.copy2(img, dest)
            copied += 1
        if i % 200 == 0 or i == total:
            print(f"  [{i}/{total}] skopiowano: {copied}, pominięto (już istnieje): {skipped}")

    return copied


def extract_zip(zip_path: Path, extract_to: Path) -> None:
    print(f"Rozpakowywanie {zip_path.name} ...")
    with zipfile.ZipFile(zip_path, "r") as zf:
        zf.extractall(extract_to)
    print(f"  Rozpakowano do: {extract_to}")


def main():
    parser = argparse.ArgumentParser(
        description="Przygotuj dataset twarzy z pobranego archiwum ZIP.",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog=__doc__,
    )
    parser.add_argument("--zip", metavar="SCIEZKA_DO_ZIP",
                        help="Ścieżka do pobranego pliku archive.zip")
    parser.add_argument("--dir", metavar="SCIEZKA_DO_FOLDERU",
                        help="Ścieżka do już rozpakowanego folderu archiwum")
    parser.add_argument("--output", metavar="SCIEZKA_DATASET", default=str(DATASET_DIR),
                        help=f"Folder docelowy dataset (domyślnie: {DATASET_DIR})")
    args = parser.parse_args()

    print("=" * 60)
    print("  Przygotowanie datasetu twarzy dla serwisu ML")
    print("=" * 60)

    # ── Ustal folder źródłowy ─────────────────────────────────────
    archive_root: Path | None = None

    if args.dir:
        archive_root = Path(args.dir)
        if not archive_root.exists():
            print(f"\n❌ Folder nie istnieje: {archive_root}")
            sys.exit(1)

    elif args.zip:
        zip_path = Path(args.zip)
        if not zip_path.exists():
            print(f"\n❌ Plik ZIP nie istnieje: {zip_path}")
            sys.exit(1)
        archive_root = zip_path.parent / zip_path.stem
        extract_zip(zip_path, archive_root)

    else:
        # Tryb interaktywny
        print("\nPodaj ścieżkę do pobranego pliku archive.zip")
        print("(lub wciśnij Enter jeśli masz już rozpakowany folder)")
        answer = input("Ścieżka: ").strip().strip('"').strip("'")
        if not answer:
            print("\nPodaj ścieżkę do już rozpakowanego folderu:")
            answer = input("Ścieżka: ").strip().strip('"').strip("'")
            archive_root = Path(answer)
        else:
            p = Path(answer)
            if p.suffix.lower() == ".zip":
                archive_root = p.parent / p.stem
                extract_zip(p, archive_root)
            else:
                archive_root = p

        if not archive_root or not archive_root.exists():
            print(f"\n❌ Ścieżka nie istnieje: {archive_root}")
            sys.exit(1)

    # ── Znajdź folder Faces/Faces ─────────────────────────────────
    print(f"\nSzukam folderu z twarzami w: {archive_root}")
    faces_dir = find_faces_dir(archive_root)

    if faces_dir is None:
        print("\n❌ Nie znaleziono folderu ze zdjęciami twarzy.")
        print("   Oczekiwana struktura archiwum:")
        print("     archive/Faces/Faces/*.jpg   ← szukam tutaj")
        print(f"\n   Zawartość folderu {archive_root}:")
        for p in sorted(archive_root.iterdir())[:20]:
            print(f"     {p.name}{'/' if p.is_dir() else ''}")
        sys.exit(1)

    img_count = count_images(faces_dir)
    print(f"✅ Znaleziono folder: {faces_dir}")
    print(f"   Zdjęć do skopiowania: {img_count}")

    if img_count == 0:
        print("\n❌ Folder istnieje ale jest pusty.")
        sys.exit(1)

    # ── Informacja o tym co NIE jest potrzebne ─────────────────────
    orig_dir = archive_root / "Original Images" / "Original Images"
    if orig_dir.exists():
        print(f"\nℹ  Folder 'Original Images' zostanie pominięty.")
        print("   Zawiera zdjęcia całego ciała – DeepFace potrzebuje tylko twarzy.")

    # ── Kopiowanie do dataset/ ─────────────────────────────────────
    dest = Path(args.output)
    copied = copy_to_dataset(faces_dir, dest)

    # ── Podsumowanie ───────────────────────────────────────────────
    final_count = count_images(dest)
    print(f"\n✅ Gotowe!")
    print(f"   Folder dataset: {dest}")
    print(f"   Łącznie zdjęć:  {final_count}")
    print()
    print("Następny krok – uruchom serwis ML:")
    print("  python service.py")


if __name__ == "__main__":
    main()
