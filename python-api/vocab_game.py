"""Timed vocabulary category game backed by fastText similarity scoring."""

import os
import random
import logging

import numpy as np
from fastapi import APIRouter, HTTPException
from pydantic import BaseModel

logger = logging.getLogger(__name__)

router = APIRouter(prefix="/vocab-game", tags=["vocab-game"])

CATEGORIES = {
    "kueche": {
        "display": "Küche",
        "seeds": ["Topf", "Pfanne", "Messer", "Herd", "Kühlschrank", "Teller"],
    },
    "tiere": {
        "display": "Tiere",
        "seeds": ["Hund", "Katze", "Vogel", "Pferd", "Fisch", "Maus"],
    },
    "kleidung": {
        "display": "Kleidung",
        "seeds": ["Hemd", "Hose", "Jacke", "Schuh", "Kleid", "Mantel"],
    },
    "sport": {
        "display": "Sport",
        "seeds": ["Fußball", "Tennis", "Schwimmen", "Laufen", "Basketball", "Turnen"],
    },
    "schule": {
        "display": "Schule",
        "seeds": ["Lehrer", "Tafel", "Buch", "Stift", "Hausaufgabe", "Klassenzimmer"],
    },
    "natur": {
        "display": "Natur",
        "seeds": ["Baum", "Blume", "Berg", "Fluss", "Wald", "Wiese"],
    },
    "transport": {
        "display": "Transport",
        "seeds": ["Auto", "Bus", "Zug", "Fahrrad", "Flugzeug", "Schiff"],
    },
    "koerper": {
        "display": "Körper",
        "seeds": ["Kopf", "Hand", "Auge", "Nase", "Mund", "Bein"],
    },
    "berufe": {
        "display": "Berufe",
        "seeds": ["Arzt", "Lehrer", "Polizist", "Ingenieur", "Koch", "Pilot"],
    },
    "wetter": {
        "display": "Wetter",
        "seeds": ["Regen", "Sonne", "Schnee", "Wind", "Wolke", "Sturm"],
    },
}

_ft_model = None
_anchors: dict[str, np.ndarray] = {}
_seed_lemmas: dict[str, set[str]] = {}

SIMILARITY_THRESHOLD = 0.45

GERMAN_ARTICLES = {
    "der", "die", "das", "ein", "eine", "einen", "einem",
    "eines", "einer", "dem", "den", "des",
}


def _strip_article(word: str) -> str:
    """Remove a leading German article so 'das Auto' → 'Auto'."""
    parts = word.split(None, 1)
    if len(parts) == 2 and parts[0].lower() in GERMAN_ARTICLES:
        return parts[1]
    return word


def _get_ft():
    global _ft_model
    if _ft_model is None:
        import fasttext

        model_path = os.environ.get(
            "FASTTEXT_MODEL_PATH", "/app/models/cc.de.300.bin"
        )
        if not os.path.exists(model_path):
            raise RuntimeError(
                f"fastText model not found at {model_path}. "
                "Download cc.de.300.bin and place it in python-api/models/"
            )
        logger.info("Loading fastText model from %s …", model_path)
        _ft_model = fasttext.load_model(model_path)
        logger.info("fastText model loaded")
        _precompute_anchors()
        _precompute_seed_lemmas()
    return _ft_model


def _precompute_anchors():
    global _anchors
    model = _ft_model
    for key, cat in CATEGORIES.items():
        vecs = [model.get_word_vector(w) for w in cat["seeds"]]
        anchor = np.mean(vecs, axis=0)
        norm = np.linalg.norm(anchor)
        if norm > 0:
            anchor = anchor / norm
        _anchors[key] = anchor


def _precompute_seed_lemmas():
    """Precompute lemmas of category display names only (not seed words) for blocking."""
    global _seed_lemmas
    try:
        from feature_extractor import _get_nlp
        nlp = _get_nlp()
        for key, cat in CATEGORIES.items():
            lemmas = set()
            # Only block the category display name itself
            display = cat["display"]
            lemmas.add(display.lower())
            doc = nlp(display)
            if doc:
                lemmas.add(doc[0].lemma_.lower())
            _seed_lemmas[key] = lemmas
        logger.info("Seed lemmas precomputed for %d categories", len(_seed_lemmas))
    except Exception as e:
        logger.warning("Could not precompute seed lemmas: %s", e)


def _cosine_similarity(vec: np.ndarray, anchor: np.ndarray) -> float:
    norm = np.linalg.norm(vec)
    if norm == 0:
        return 0.0
    return float(np.dot(vec / norm, anchor))


def _get_lemma(word: str) -> str:
    """Get spaCy lemma for a single word."""
    try:
        from feature_extractor import _get_nlp
        nlp = _get_nlp()
        doc = nlp(word)
        return doc[0].lemma_.lower() if doc else word.lower()
    except Exception:
        return word.lower()


def _spell_check(word: str) -> tuple[bool, list[str]]:
    try:
        from feature_extractor import _get_lt

        lt = _get_lt()
        matches = lt.check(word)
        for m in matches:
            rule = getattr(m, "ruleId", "") or ""
            cat = getattr(m, "category", "") or ""
            if any(
                k in (rule + cat).upper()
                for k in ["SPELLER", "MORFOLOGIK", "TYPO", "TIPPFEHLER"]
            ):
                reps = getattr(m, "replacements", [])
                suggestions = [
                    r.value if hasattr(r, "value") else str(r) for r in reps[:5]
                ]
                return True, suggestions
    except Exception as e:
        logger.warning("Spell check failed: %s", e)
    return False, []


class CategoryResponse(BaseModel):
    categoryKey: str
    displayName: str


class BatchValidateRequest(BaseModel):
    words: list[str]
    categoryKey: str


class WordResult(BaseModel):
    word: str
    valid: bool
    similarity: float
    reason: str  # "valid", "not_related", "duplicate", "seed_word"
    misspelled: bool
    suggestions: list[str]
    lemma: str = ""
    englishTranslation: str = ""
    article: str = ""
    plural: str = ""


class BatchValidateResponse(BaseModel):
    results: list[WordResult]
    score: int


@router.get("/category", response_model=CategoryResponse)
def get_category():
    key = random.choice(list(CATEGORIES.keys()))
    return CategoryResponse(
        categoryKey=key,
        displayName=CATEGORIES[key]["display"],
    )


@router.post("/validate-batch", response_model=BatchValidateResponse)
def validate_batch(req: BatchValidateRequest):
    key = req.categoryKey.lower()
    if key not in CATEGORIES:
        raise HTTPException(status_code=400, detail=f"Unknown category: {key}")

    if not req.words:
        return BatchValidateResponse(results=[], score=0)

    try:
        model = _get_ft()
    except RuntimeError as e:
        raise HTTPException(status_code=503, detail=str(e))

    anchor = _anchors[key]
    blocked_lemmas = _seed_lemmas.get(key, set())

    results: list[WordResult] = []
    seen_lemmas: set[str] = set()
    score = 0

    for word in req.words:
        word = word.strip()
        if not word:
            continue

        # Strip leading article so "das Auto" is treated the same as "Auto"
        core = _strip_article(word)

        lemma = _get_lemma(core)

        # Check if it's a seed word or category name
        if lemma in blocked_lemmas or core.lower() in blocked_lemmas:
            results.append(WordResult(
                word=word, valid=False, similarity=0.0,
                reason="seed_word", misspelled=False, suggestions=[],
            ))
            continue

        # Check for duplicate lemma (singular/plural of same word)
        if lemma in seen_lemmas:
            results.append(WordResult(
                word=word, valid=False, similarity=0.0,
                reason="duplicate", misspelled=False, suggestions=[],
            ))
            continue

        seen_lemmas.add(lemma)

        # Cosine similarity check against bare noun (no article)
        vec = model.get_word_vector(core)
        similarity = _cosine_similarity(vec, anchor)
        is_valid = similarity >= SIMILARITY_THRESHOLD

        # Spell check on the core word (article already stripped)
        misspelled, suggestions = _spell_check(core)

        # Enrichment for misspelled words
        enrichment = {"lemma": "", "englishTranslation": "", "article": "", "plural": ""}
        if misspelled and suggestions:
            try:
                from word_enricher import enrich_word
                enrichment = enrich_word(suggestions[0])
            except Exception as e:
                logger.warning("Word enrichment failed: %s", e)

        reason = "valid" if is_valid else "not_related"

        if is_valid:
            score += 1

        results.append(WordResult(
            word=word,
            valid=is_valid,
            similarity=round(similarity, 4),
            reason=reason,
            misspelled=misspelled,
            suggestions=suggestions,
            lemma=enrichment["lemma"],
            englishTranslation=enrichment["englishTranslation"],
            article=enrichment["article"],
            plural=enrichment["plural"],
        ))

    return BatchValidateResponse(results=results, score=score)
