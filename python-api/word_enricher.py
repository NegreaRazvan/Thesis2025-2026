"""German word enrichment: lemma, article, plural, English translation."""

import logging

from deep_translator import GoogleTranslator, MyMemoryTranslator
from feature_extractor import _get_nlp

logger = logging.getLogger(__name__)


def guess_plural(word: str, article: str) -> str:
    if not article:
        return ""
    if article == "die":
        if word.endswith("e"):
            return word + "n"
        if word.endswith(("ung", "heit", "keit", "schaft", "ion", "ik")):
            return word + "en"
        return word + "en"
    if article == "der":
        if word.endswith(("er", "el", "en")):
            return word
        if word.endswith("e"):
            return word + "n"
        return word + "e"
    if article == "das":
        if word.endswith(("chen", "lein")):
            return word
        if word.endswith(("er", "el", "en")):
            return word
        return word + "e"
    return ""


def enrich_word(corrected_word: str) -> dict:
    """Return lemma, English translation, article, and plural for a German word."""
    nlp = _get_nlp()
    lemma = corrected_word
    article = ""
    plural = ""

    try:
        doc = nlp(corrected_word)
        tok = doc[0] if doc else None
        if tok:
            lemma = tok.lemma_
            if tok.pos_ == "NOUN":
                gender = tok.morph.get("Gender")
                if gender:
                    article = {"Masc": "der", "Fem": "die", "Neut": "das"}.get(gender[0], "")
                plural = guess_plural(lemma, article)
    except Exception:
        pass

    translation = ""
    try:
        translation = MyMemoryTranslator(source="de-DE", target="en-GB").translate(lemma)
    except Exception:
        try:
            translation = GoogleTranslator(source="de", target="en").translate(lemma)
        except Exception:
            logger.warning("Translation failed for word: %s", lemma)

    if translation and " -- " in translation:
        translation = translation.split(" -- ")[0]

    return {
        "lemma": lemma,
        "englishTranslation": (translation or "").strip(),
        "article": article,
        "plural": plural,
    }
