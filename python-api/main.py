import logging
import os
import sys
from contextlib import asynccontextmanager
from pathlib import Path

import joblib
import numpy as np
import spacy
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel

logger = logging.getLogger(__name__)

MODEL_PATH = Path("models/cefr_model.pkl")

try:
    from feature_extractor import extract_features, _get_nlp, _get_lt, _get_st
except ImportError as e:
    logger.error("Could not import feature_extractor: %s", e)
    sys.exit(1)

try:
    from vocab_detector import VocabDetector
    _vocab_detector = VocabDetector()   # lazy — model loads on first request
except ImportError as e:
    logger.warning("vocab_detector not available: %s. Vocab detection will be skipped.", e)
    _vocab_detector = None

try:
    from transcription import router as transcription_router
    _transcription_available = True
except ImportError as e:
    logger.warning("transcription not available: %s. /transcribe endpoint will be absent.", e)
    _transcription_available = False
    transcription_router = None

try:
    from vocab_game import router as vocab_game_router
    _vocab_game_available = True
except ImportError as e:
    logger.warning("vocab_game not available: %s. /vocab-game endpoints will be absent.", e)
    _vocab_game_available = False
    vocab_game_router = None


def load_bundle():
    if not MODEL_PATH.exists():
        logger.error("Model not found at %s", MODEL_PATH)
        return None
    try:
        bundle = joblib.load(MODEL_PATH)
        logger.info("Model loaded from %s", MODEL_PATH)
        return bundle
    except Exception as exc:
        logger.error("Failed to load model: %s", exc)
        return None

_bundle = None

def get_bundle():
    global _bundle
    if _bundle is None:
        _bundle = load_bundle()
    if _bundle is None:
        raise HTTPException(status_code=503,
            detail="ML model is not loaded. Place cefr_model.pkl in models/ and restart.")
    return _bundle


@asynccontextmanager
async def lifespan(app: FastAPI):
    for name, loader in [("spaCy", _get_nlp), ("LanguageTool", _get_lt), ("SentenceTransformers", _get_st)]:
        try:
            loader()
            logger.info("%s loaded", name)
        except Exception as e:
            logger.warning("%s warm-up failed: %s", name, e)

    # GermanBERT loads lazily on first /predict call — it's only needed after a submission
    if _vocab_game_available:
        try:
            from vocab_game import _get_ft
            _get_ft()
            logger.info("fastText loaded")
        except Exception as e:
            logger.warning("fastText warm-up failed: %s", e)

    global _bundle
    _bundle = load_bundle()
    logger.info("LinguaForge ML API ready")
    yield
    logger.info("Shutting down")


app = FastAPI(
    title="LinguaForge ML API",
    description="CEFR prediction, grammar analysis, vocab detection and speech transcription",
    version="2.0.0",
    lifespan=lifespan,
)

if transcription_router is not None:
    app.include_router(transcription_router)
    logger.info("/transcribe endpoint registered")

if vocab_game_router is not None:
    app.include_router(vocab_game_router)
    logger.info("/vocab-game endpoints registered")


class PredictRequest(BaseModel):
    text: str

class ErrorItem(BaseModel):
    category: str
    ruleId: str
    message: str
    offsetStart: int
    offsetEnd: int
    badText: str
    suggestions: list[str]
    lemma: str = ""

    englishTranslation: str = ""

    article: str = ""

    plural: str = ""


class VocabCandidate(BaseModel):
    token: str
    lemma: str
    pos: str
    offsetStart: int
    offsetEnd: int
    actualProbability: float
    topAlternatives: list[str]
    zipfScore: float
    reason: str
    flashcardFront: str
    flashcardBack: str

class PredictResponse(BaseModel):
    predictedLevel: str
    confidence: dict[str, float]
    features: dict[str, float]
    errors: list[ErrorItem]
    vocabCandidates: list[VocabCandidate]
    shortTextWarning: bool


_SPELLING_RULES = {"GERMAN_SPELLER_RULE", "MORFOLOGIK_RULE_DE_DE"}

def _is_spelling_error(cat: str, rule: str) -> bool:
    cat_up  = cat.upper()
    rule_up = rule.upper()
    return (
        rule in _SPELLING_RULES
        or rule_up.startswith("MORFOLOGIK")
        or rule_up.startswith("DE_COMPOUND")
        or cat_up == "TYPOS"
        or "TIPPFEHLER" in cat_up
    )


from word_enricher import enrich_word as _enrich_word, guess_plural as _guess_plural


def _extract_lt_errors(text: str) -> list[ErrorItem]:
    try:
        lt = _get_lt()
        matches = lt.check(text)
    except Exception:
        return []

    result = []
    for m in matches:
        cat  = getattr(m, "category", "") or ""
        rule = getattr(m, "ruleId",   "") or ""

        if "STYLE" in cat.upper() or "REDUNDANCY" in rule.upper():
            continue

        offset_start = getattr(m, "offset",      0)
        length       = getattr(m, "errorLength", 0)
        offset_end   = offset_start + length
        bad_text     = text[offset_start:offset_end]

        reps        = getattr(m, "replacements", [])
        suggestions = [r.value if hasattr(r, "value") else str(r) for r in reps[:5]]

        enrichment = {"lemma": "", "englishTranslation": "", "article": "", "plural": ""}
        if _is_spelling_error(cat, rule) and suggestions:
            enrichment = _enrich_word(suggestions[0])

        result.append(ErrorItem(
            category           = cat,
            ruleId             = rule,
            message            = getattr(m, "message", ""),
            offsetStart        = offset_start,
            offsetEnd          = offset_end,
            badText            = bad_text,
            suggestions        = suggestions,
            lemma              = enrichment["lemma"],
            englishTranslation = enrichment["englishTranslation"],
            article            = enrichment["article"],
            plural             = enrichment["plural"],
        ))

    return result


def _extract_vocab_candidates(text: str, doc: spacy.tokens.Doc) -> list[VocabCandidate]:
    if _vocab_detector is None:
        return []
    try:
        raw = _vocab_detector.detect(text, doc)
        return [VocabCandidate(**item) for item in raw]
    except Exception as e:
        logger.warning("VocabDetector failed: %s", e)
        return []


SHORT_TEXT_TOKEN_THRESHOLD = 40
VOCAB_MIN_TOKENS = 30


@app.get("/health")
def health():
    return {
        "status": "ok",
        "model_loaded": _bundle is not None,
        "vocab_detection": _vocab_detector is not None,
        "transcription": _transcription_available,
        "vocab_game": _vocab_game_available,
    }

@app.post("/predict", response_model=PredictResponse)
def predict(request: PredictRequest):
    text = request.text.strip()
    if not text:
        raise HTTPException(status_code=422, detail="text must not be empty.")

    bundle        = get_bundle()
    pipeline      = bundle["pipeline"]
    le            = bundle["label_encoder"]
    feature_names = bundle["feature_names"]
    cefr_levels   = bundle["cefr_levels"]

    nlp = _get_nlp()
    doc = nlp(text)

    try:
        feats = extract_features(text)
    except Exception as exc:
        raise HTTPException(status_code=500, detail=f"Feature extraction failed: {exc}")

    X = np.array(
        [[feats.get(f, 0.0) for f in feature_names]],
        dtype=np.float32
    )

    predicted_idx   = pipeline.predict(X)[0]
    predicted_level = le.inverse_transform([predicted_idx])[0]
    probabilities   = pipeline.predict_proba(X)[0]

    confidence = {
        lvl: float(prob)
        for lvl, prob in zip(cefr_levels, probabilities)
    }

    errors          = _extract_lt_errors(text)
    n_tokens        = int(feats.get("n_tokens", 0))
    short_text_warn = n_tokens < SHORT_TEXT_TOKEN_THRESHOLD
    features_out    = {k: float(v) for k, v in feats.items() if isinstance(v, (int, float))}

    vocab_candidates = (
        _extract_vocab_candidates(text, doc)
        if n_tokens >= VOCAB_MIN_TOKENS
        else []
    )

    return PredictResponse(
        predictedLevel   = predicted_level,
        confidence       = confidence,
        features         = features_out,
        errors           = errors,
        vocabCandidates  = vocab_candidates,
        shortTextWarning = short_text_warn,
    )
