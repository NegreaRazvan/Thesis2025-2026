"""
Batch pipeline: MERLIN corpus -> feature extraction -> training-ready CSV.

Usage:
    python run_pipeline.py

Expected layout:
    data/merlin/merlin-metadata-v1.2/metadata_ratings_indicators.csv
    data/merlin/merlin-text-v1.2/plain/german/<author_id>.txt
    feature_extractor.py  (same directory or on PYTHONPATH)

Output:
    outputs/merlin_features.csv  -- one row per German text, labelled + feature vectors
"""

import sys
import csv
import logging
from pathlib import Path

import pandas as pd
from tqdm import tqdm

try:
    from feature_extractor import extract_features, FEATURE_NAMES
except ImportError:
    sys.exit(
        "Could not import feature_extractor.\n"
        "   Make sure feature_extractor.py is in the same directory as this script."
    )

MERLIN_META  = Path("data/merlin/merlin-metadata-v1.2/metadata_ratings_indicators.csv")
MERLIN_TEXTS = Path("data/merlin/merlin-text-v1.2/plain/german")
OUT_FILE     = Path("outputs/merlin_features.csv")

# MERLIN CSV column names (verified from metadata_ratings_indicators.csv v1.2)
COL_AUTHOR   = "_author"          # text file ID  (e.g. "1001")
COL_LANGUAGE = "_test_language"   # values: "German", "Czech", "Italian"
COL_CEFR     = "_test_level_cefr" # values: "A1", "A2", "B1", "B2", "C1"

OUT_FIELDS = ["text_id", "cefr_gold"] + FEATURE_NAMES

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s  %(levelname)s  %(message)s",
    datefmt="%H:%M:%S",
)
log = logging.getLogger(__name__)


def load_german_metadata(csv_path: Path) -> pd.DataFrame:
    """
    Load the MERLIN metadata CSV and return only German rows with valid CEFR.

    Key CSV facts (v1.2):
      - Separator: comma  (NOT tab -- a common gotcha)
      - Language column values: 'German', 'Czech', 'Italian'  (NOT 'de')
      - Text ID column: '_author'  (no '_filename' column exists)
    """
    log.info(f"Reading metadata: {csv_path}")

    df = pd.read_csv(
        csv_path,
        sep=",",               # comma-separated, confirmed from file header
        encoding="utf-8",
        low_memory=False,
    )

    log.info(f"Total rows in metadata: {len(df)}")
    log.info(f"Languages present: {df[COL_LANGUAGE].value_counts().to_dict()}")

    german = df[df[COL_LANGUAGE] == "German"].copy()
    german = german[german[COL_CEFR].notna()].copy()

    log.info(f"German texts with CEFR label: {len(german)}")
    log.info(f"CEFR distribution:\n{german[COL_CEFR].value_counts().sort_index().to_string()}")

    return german


def find_text_file(text_dir: Path, author_id: str) -> Path | None:
    """
    Locate the text file for a given author ID.
    MERLIN text files are named simply '<author_id>.txt' in the german/ folder.
    """
    candidates = [
        text_dir / f"{author_id}.txt",
        text_dir / f"{author_id}",
        text_dir / f"{author_id}.TXT",
    ]
    return next((p for p in candidates if p.exists()), None)


def run_batch(meta_df: pd.DataFrame, text_dir: Path, out_file: Path) -> None:
    out_file.parent.mkdir(parents=True, exist_ok=True)

    rows    = []
    missing = []
    errors  = []

    for _, row in tqdm(meta_df.iterrows(), total=len(meta_df), desc="Extracting features"):
        author_id = str(row[COL_AUTHOR])
        cefr_gold = str(row[COL_CEFR]).strip()

        text_path = find_text_file(text_dir, author_id)

        if text_path is None:
            missing.append(author_id)
            continue

        try:
            text = text_path.read_text(encoding="utf-8", errors="replace").strip()
            if not text:
                missing.append(author_id)
                continue

            feats = extract_features(text)

            record = {
                "text_id":   author_id,
                "cefr_gold": cefr_gold,
            }
            record.update(feats)
            rows.append(record)

        except Exception as exc:
            errors.append(f"{author_id}: {exc}")
            log.warning(f"Error on {author_id}: {exc}")

    with open(out_file, "w", newline="", encoding="utf-8") as f:
        writer = csv.DictWriter(f, fieldnames=OUT_FIELDS)
        writer.writeheader()
        writer.writerows(rows)

    log.info(f"Written {len(rows)} rows -> {out_file.resolve()}")

    if missing:
        log.warning(f"Missing / empty text files: {len(missing)}")
        for m in missing[:10]:
            log.warning(f"     {m}")
        if len(missing) > 10:
            log.warning(f"     ... and {len(missing) - 10} more")

    if errors:
        log.warning(f"Extraction errors: {len(errors)}")
        for e in errors[:5]:
            log.warning(f"     {e}")

    out_df = pd.read_csv(out_file)
    log.info(f"\nCEFR distribution in output:\n"
             f"{out_df['cefr_gold'].value_counts().sort_index().to_string()}")


def main():
    if not MERLIN_META.exists():
        sys.exit(f"Metadata file not found:\n   {MERLIN_META.resolve()}\n"
                 f"   Check the MERLIN_META path at the top of this script.")

    if not MERLIN_TEXTS.exists():
        sys.exit(f"Text directory not found:\n   {MERLIN_TEXTS.resolve()}\n"
                 f"   Check the MERLIN_TEXTS path at the top of this script.")

    meta_df = load_german_metadata(MERLIN_META)

    if meta_df.empty:
        sys.exit("No German rows found in the metadata CSV. Check COL_LANGUAGE filter.")

    run_batch(meta_df, MERLIN_TEXTS, OUT_FILE)


if __name__ == "__main__":
    main()
