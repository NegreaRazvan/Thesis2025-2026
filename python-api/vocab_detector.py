"""GermanBERT vocabulary candidate detection for German learner texts."""

import os
import logging
from dataclasses import dataclass

logger = logging.getLogger(__name__)

ENABLED           = os.getenv("VOCAB_DETECTION_ENABLED", "true").lower() == "true"
CONFIDENCE_THRESH = float(os.getenv("BERT_CONFIDENCE_THRESHOLD", "0.05"))
MAX_BERT_TOKENS   = int(os.getenv("BERT_MAX_TOKENS", "128"))
MODEL_NAME        = "dbmdz/bert-base-german-cased"   # well-maintained, correct config.json

CONTENT_POS  = {"NOUN", "VERB", "ADJ", "ADV"}
SKIP_POS     = {"PROPN", "PUNCT", "SYM", "NUM", "X", "SPACE"}
MIN_TOKEN_LEN = 3


@dataclass
class VocabCandidate:
    token:              str
    lemma:              str
    pos:                str
    offset_start:       int
    offset_end:         int
    actual_probability: float
    top_alternatives:   list
    zipf_score:         float
    reason:             str
    flashcard_front:    str
    flashcard_back:     str


class VocabDetector:
    def __init__(self):
        self._pipeline  = None
        self._wordfreq  = None

    def _load(self):
        if self._pipeline is not None or not ENABLED:
            return
        try:
            from transformers import pipeline as hf_pipeline
            self._pipeline = hf_pipeline(
                "fill-mask",
                model=MODEL_NAME,
                top_k=10,
                device=-1,
            )
            logger.info("GermanBERT loaded: %s", MODEL_NAME)
        except Exception as e:
            logger.warning("Could not load GermanBERT: %s. Vocab detection skipped.", e)
            self._pipeline = None

        try:
            from wordfreq import zipf_frequency
            self._wordfreq = zipf_frequency
        except ImportError:
            pass

    def detect(self, text: str, doc) -> list:
        self._load()
        if self._pipeline is None or not ENABLED:
            return []

        candidates = []
        seen: set = set()

        for token in doc:
            if token.pos_ in SKIP_POS or token.pos_ not in CONTENT_POS:
                continue
            if len(token.text) < MIN_TOKEN_LEN or token.is_stop:
                continue
            lower = token.text.lower()
            if lower in seen:
                continue
            seen.add(lower)

            prob, alternatives = self._bert_probability(text, token)
            if prob is None:
                continue

            zipf  = self._zipf(lower)
            flagged, reason = self._should_flag(prob, zipf, token)
            if not flagged:
                continue

            candidates.append(self._build_candidate(token, prob, alternatives, zipf, reason))

        candidates.sort(key=lambda c: c.actual_probability)
        return [self._to_dict(c) for c in candidates[:8]]

    def _bert_probability(self, text: str, token) -> tuple:
        try:
            masked   = text[:token.idx] + "[MASK]" + text[token.idx + len(token.text):]
            mask_pos = masked.index("[MASK]")
            half     = (MAX_BERT_TOKENS * 4) // 2
            window   = masked[max(0, mask_pos - half) : mask_pos + half]

            results      = self._pipeline(window)
            actual_lower = token.text.lower()
            actual_prob  = None

            for r in results:
                if r["token_str"].strip().lower() == actual_lower:
                    actual_prob = r["score"]
                    break

            if actual_prob is None:
                # Token not in BERT's top-k: use bottom score * 0.1 as a conservative
                # lower-bound so downstream flagging still works on a numeric value.
                actual_prob = results[-1]["score"] * 0.1

            alternatives = [
                r["token_str"].strip()
                for r in results
                if r["token_str"].strip().lower() != actual_lower
            ][:5]

            return actual_prob, alternatives
        except Exception as e:
            logger.debug("BERT inference failed for '%s': %s", token.text, e)
            return None, []

    def _zipf(self, word: str) -> float:
        if self._wordfreq is None:
            return 3.0
        return self._wordfreq(word, "de")

    def _should_flag(self, prob: float, zipf: float, token) -> tuple:
        if prob < CONFIDENCE_THRESH:
            if zipf < 2.5:
                return True, f"'{token.text}' is a rare word and fits poorly in context — you may have guessed at it."
            elif zipf > 5.0:
                return True, f"'{token.text}' is a common word but unexpected here — another word is likely needed."
            else:
                return True, f"'{token.text}' seems unexpected in context — the model predicts a different word here."
        if prob < CONFIDENCE_THRESH * 3 and zipf < 2.0:
            return True, f"'{token.text}' is a very rare word — add it to flashcards to make sure you know it well."
        return False, ""

    def _build_candidate(self, token, prob: float, alternatives: list, zipf: float, reason: str) -> VocabCandidate:
        pos_label = {"NOUN": "noun", "VERB": "verb", "ADJ": "adjective", "ADV": "adverb"}.get(token.pos_, token.pos_)
        alt_str   = ", ".join(alternatives[:3]) if alternatives else "—"
        snippet   = self._snippet(token)

        return VocabCandidate(
            token=token.text, lemma=token.lemma_, pos=token.pos_,
            offset_start=token.idx, offset_end=token.idx + len(token.text),
            actual_probability=round(prob, 4), top_alternatives=alternatives,
            zipf_score=round(zipf, 2), reason=reason,
            flashcard_front=f"What word fits here? «{snippet}»",
            flashcard_back=f"You wrote: {token.text}\nMore likely: {alt_str}\n\nLemma: {token.lemma_}  |  Type: {pos_label}\nWord frequency: {zipf:.1f}/7",
        )

    def _snippet(self, token, window: int = 4) -> str:
        tokens_in_sent = list(token.sent)
        idx = next((i for i, t in enumerate(tokens_in_sent) if t.i == token.i), None)
        if idx is None:
            return "… ___ …"
        start = max(0, idx - window)
        end   = min(len(tokens_in_sent), idx + window + 1)
        return " ".join("___" if (start + i) == idx else t.text for i, t in enumerate(tokens_in_sent[start:end]))

    def _to_dict(self, c: VocabCandidate) -> dict:
        return {
            "token": c.token, "lemma": c.lemma, "pos": c.pos,
            "offsetStart": c.offset_start, "offsetEnd": c.offset_end,
            "actualProbability": c.actual_probability, "topAlternatives": c.top_alternatives,
            "zipfScore": c.zipf_score, "reason": c.reason,
            "flashcardFront": c.flashcard_front, "flashcardBack": c.flashcard_back,
        }