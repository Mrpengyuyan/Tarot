"""Repair corrupted or synthetic historical prediction questions."""

from __future__ import annotations

import argparse

from app.db.maintenance import repair_prediction_questions
from app.db.session import SessionLocal


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Repair corrupted or synthetic prediction question titles.",
    )
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Scan and report without committing any changes.",
    )
    parser.add_argument(
        "--skip-synthetic",
        action="store_true",
        help="Only repair obviously corrupted questions and keep synthetic titles untouched.",
    )
    return parser.parse_args()


def main() -> None:
    args = parse_args()
    db = SessionLocal()
    try:
        result = repair_prediction_questions(
            db,
            include_synthetic=not args.skip_synthetic,
            commit=not args.dry_run,
        )
        mode = "dry-run" if args.dry_run else "applied"
        print(
            f"[ok] prediction question maintenance {mode}: "
            f"scanned={result['scanned']} repaired={result['repaired']} "
            f"include_synthetic={result['include_synthetic']}"
        )
    finally:
        db.close()


if __name__ == "__main__":
    main()
