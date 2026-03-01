"""
prepare-dataset.py – kopiuje zdjęcia twarzy z podanego archiwum ZIP do folderu ml/dataset/.

Przygotuj swój dataset:
  1. Umieść zdjęcia twarzy w folderze (każde zdjęcie nazwij: ImięNazwisko_numer.jpg,
     np. "Jan Kowalski_1.jpg", "Anna Nowak_2.png").
  2. Spakuj ten folder do jednego pliku ZIP.
  3. Uruchom ten skrypt i podaj ścieżkę do ZIPa.

Wszystkie pliki graficzne z archiwum trafią płasko do folderu ml/dataset/.

Użycie:
  python prepare-dataset.py                          # pyta interaktywnie o ścieżkę
  python prepare-dataset.py --zip C:\\pobrane\\moje_twarze.zip
  python prepare-dataset.py --dir C:\\rozpakowany_folder
"""

import argparse
import shutil
import sys
import zipfile
from pathlib import Path

DATASET_DIR = Path(__file__).parent / "dataset"
IMAGE_EXTS = {".jpg", ".jpeg", ".png", ".bmp", ".webp"}


def count_images(folder: Path) -> int:
    return sum(1 for f in folder.rglob("*") if f.is_file() and f.suffix.lower() in IMAGE_EXTS)


def copy_to_dataset(source_dir: Path, dest_dir: Path) -> int:
    """Kopiuje wszystkie pliki graficzne z source_dir (rekurencyjnie) do dest_dir płasko."""
    dest_dir.mkdir(parents=True, exist_ok=True)
    copied = 0
    skipped = 0

    images = sorted(f for f in source_dir.rglob("*") if f.is_file() and f.suffix.lower() in IMAGE_EXTS)
    total = len(images)

    if total == 0:
        print("\nBrak plików graficznych w podanym folderze.")
        return 0

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
        description="Przygotuj dataset twarzy z archiwum ZIP lub folderu.",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog=__doc__,
    )
    parser.add_argument("--zip", metavar="SCIEZKA_DO_ZIP",
                        help="Ścieżka do pliku ZIP ze zdjęciami twarzy")
    parser.add_argument("--dir", metavar="SCIEZKA_DO_FOLDERU",
                        help="Ścieżka do folderu ze zdjęciami twarzy")
    parser.add_argument("--output", metavar="SCIEZKA_DATASET", default=str(DATASET_DIR),
                        help=f"Folder docelowy dataset (domyślnie: {DATASET_DIR})")
    args = parser.parse_args()

    print("=" * 60)
    print("  Przygotowanie datasetu twarzy dla serwisu ML")
    print("=" * 60)

    source_dir: Path | None = None

    if args.dir:
        source_dir = Path(args.dir)
        if not source_dir.exists():
            print(f"\nFolder nie istnieje: {source_dir}")
            sys.exit(1)

    elif args.zip:
        zip_path = Path(args.zip)
        if not zip_path.exists():
            print(f"\nPlik ZIP nie istnieje: {zip_path}")
            sys.exit(1)
        source_dir = zip_path.parent / zip_path.stem
        extract_zip(zip_path, source_dir)

    else:
        print("\nPodaj ścieżkę do pliku ZIP ze zdjęciami twarzy:")
        answer = input("Ścieżka: ").strip().strip('"').strip("'")
        if not answer:
            print("Nie podano ścieżki. Koniec.")
            sys.exit(1)
        p = Path(answer)
        if p.suffix.lower() == ".zip":
            source_dir = p.parent / p.stem
            extract_zip(p, source_dir)
        else:
            source_dir = p

        if not source_dir or not source_dir.exists():
            print(f"\nŚcieżka nie istnieje: {source_dir}")
            sys.exit(1)

    dest = Path(args.output)
    copied = copy_to_dataset(source_dir, dest)

    final_count = count_images(dest)
    print(f"\nGotowe!")
    print(f"  Folder dataset: {dest}")
    print(f"  Lacznie zdiec:  {final_count}")
    print()
    print("Nastepny krok - uruchom serwis ML:")
    print("  python service.py")


if __name__ == "__main__":
    main()
