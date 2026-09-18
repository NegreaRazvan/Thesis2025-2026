"""
Linguistic feature extraction for German learner texts (MERLIN corpus).
Produces a 22-dim feature vector grounded in the CAF framework (Complexity, Accuracy, Fluency).
"""

import logging
import statistics

import spacy
import language_tool_python
import numpy as np
from wordfreq import zipf_frequency
from sentence_transformers import SentenceTransformer, util

_logger = logging.getLogger(__name__)

_nlp: spacy.language.Language | None = None
_lt:  language_tool_python.LanguageTool | None = None
_st:  SentenceTransformer | None = None


def _get_nlp():
    global _nlp
    if _nlp is None:
        _nlp = spacy.load("de_core_news_lg")
    return _nlp


def _get_lt():
    global _lt
    if _lt is None:
        _lt = language_tool_python.LanguageTool("de-DE")
    return _lt


def _get_st():
    global _st
    if _st is None:
        _st = SentenceTransformer("paraphrase-multilingual-MiniLM-L12-v2")
    return _st


def _safe_div(a: float, b: float, fallback: float = 0.0) -> float:
    return a / b if b > 0 else fallback


# Fluency — surface / length features
# Ref: Hancke 2013 §3.1; Wolfe-Quintero et al. 1998

def _fluency_features(doc: spacy.tokens.Doc, text: str) -> dict:
    tokens = [t for t in doc if not t.is_space]
    sents  = list(doc.sents)

    n_tokens = len(tokens)
    n_sents  = len(sents)

    avg_sent_len = _safe_div(n_tokens, n_sents)
    avg_word_len = (
        statistics.mean([len(t.text) for t in tokens if t.is_alpha])
        if any(t.is_alpha for t in tokens) else 0.0
    )

    short_text_penalty = 1.0 if n_tokens < 40 else 0.0

    return {
        "n_tokens":           float(n_tokens),
        "n_sentences":        float(n_sents),
        "avg_sent_len":       avg_sent_len,
        "avg_word_len":       avg_word_len,
        "short_text_penalty": short_text_penalty,
    }


# Lexical complexity
# Ref: Hancke 2013; Vajjala & Meurers 2012; Lu 2011

def _lexical_features(doc: spacy.tokens.Doc) -> dict:
    """
    MATTR (Moving-Average TTR, window=50) is used instead of raw TTR
    because raw TTR is strongly length-biased (Malvern et al. 2004; Hancke 2013).
    """
    alpha_tokens = [t.lower_ for t in doc if t.is_alpha]
    lemmas       = [t.lemma_.lower() for t in doc if t.is_alpha]

    n_tokens = len(alpha_tokens)
    n_types  = len(set(alpha_tokens))

    window = 50
    if n_tokens >= window:
        ttr_windows = [
            len(set(alpha_tokens[i : i + window])) / window
            for i in range(n_tokens - window + 1)
        ]
        mattr = statistics.mean(ttr_windows)
    else:
        mattr = _safe_div(n_types, n_tokens)  # fallback for short texts

    content_pos = {"NOUN", "VERB", "ADJ", "ADV"}
    n_content   = sum(1 for t in doc if t.pos_ in content_pos and t.is_alpha)
    lex_density = _safe_div(n_content, n_tokens)

    zipf_scores = [
        max(0.5, zipf_frequency(t, "de"))
        for t in alpha_tokens
        if len(t) > 2
    ]
    median_zipf = statistics.median(zipf_scores) if zipf_scores else 3.0

    rep_ratio = 1.0 - _safe_div(n_types, n_tokens)

    return {
        "mattr":        mattr,
        "lex_density":  lex_density,
        "median_zipf":  median_zipf,    # lower -> more advanced
        "rep_ratio":    rep_ratio,       # higher -> more repetitive
    }


# Syntactic complexity
# Ref: Hancke 2013 §3.3; Lu 2010; Vajjala & Meurers 2012

def _syntactic_features(doc: spacy.tokens.Doc) -> dict:
    """
    Dependency tree depth, subordination, POS distribution.
    Syntactic features alone yield ~65% accuracy for German CEFR (Hancke 2013).
    """
    sents = list(doc.sents)

    def _tree_depth(token, depth=0):
        children = list(token.children)
        if not children:
            return depth
        return max(_tree_depth(c, depth + 1) for c in children)

    depths = []
    for sent in sents:
        root = [t for t in sent if t.dep_ == "ROOT"]
        if root:
            depths.append(_tree_depth(root[0]))
    avg_dep_depth = statistics.mean(depths) if depths else 0.0

    sub_labels    = {"sb", "oc", "da", "cm", "ng", "mo", "rc"}
    all_tokens    = [t for t in doc if not t.is_space]
    n_sub         = sum(1 for t in all_tokens if t.dep_ in sub_labels)
    sub_ratio     = _safe_div(n_sub, len(all_tokens))

    n_all   = max(len(all_tokens), 1)
    pos_counts = {
        "VERB": 0, "AUX": 0, "NOUN": 0, "ADJ": 0,
        "ADV": 0, "CONJ": 0, "SCONJ": 0, "PUNCT": 0
    }
    for t in all_tokens:
        if t.pos_ in pos_counts:
            pos_counts[t.pos_] += 1

    sconj_ratio = _safe_div(pos_counts["SCONJ"], n_all)
    verb_ratio  = _safe_div(pos_counts["VERB"] + pos_counts["AUX"], n_all)

    return {
        "avg_dep_depth": avg_dep_depth,
        "sub_ratio":     sub_ratio,
        "sconj_ratio":   sconj_ratio,
        "verb_ratio":    verb_ratio,
    }


# Grammatical accuracy
# Ref: Hancke 2013 §3.4; Gaillat et al. 2021; tool: LanguageTool

def _accuracy_features(text: str, doc: spacy.tokens.Doc) -> dict:
    """
    Error rate with Bayesian smoothing to prevent short texts from appearing
    perfect due to low token count (Hancke 2013, §3.4).
    """
    lt = _get_lt()

    try:
        matches = lt.check(text)
    except Exception:
        matches = []

    n_tokens = max(len([t for t in doc if t.is_alpha]), 1)

    grammar_errors = 0
    ortho_errors   = 0

    for m in matches:
        cat = getattr(m, "category", "") or ""
        rule = getattr(m, "ruleId", "") or ""

        if "STYLE" in cat.upper() or "REDUNDANCY" in rule.upper():
            continue

        if "TYPO" in cat.upper() or "SPELL" in cat.upper() or "ORTHO" in rule.upper():
            ortho_errors += 1
        else:
            grammar_errors += 1

    # Bayesian-smoothed error rate per 100 tokens (prior = 1 error per 50 words)
    prior_count  = 2.0
    prior_weight = 50.0
    grammar_rate = (grammar_errors + prior_count) / (n_tokens + prior_weight) * 100
    ortho_rate   = (ortho_errors   + prior_count) / (n_tokens + prior_weight) * 100
    total_rate   = grammar_rate + ortho_rate

    return {
        "grammar_error_rate": grammar_rate,
        "ortho_error_rate":   ortho_rate,
        "total_error_rate":   total_rate,
    }


# Discourse coherence
# Ref: Yannakoudakis & Briscoe 2012; Gaillat et al. 2021

def _coherence_features(doc: spacy.tokens.Doc) -> dict:
    """
    Adjacent cosine similarity captures local coherence (topic flow).
    Variance captures global coherence (topic collapse vs. drift).
    High similarity alone can mean repetition, so both are tracked.
    """
    sents = [s.text.strip() for s in doc.sents if len(s.text.strip()) > 5]

    if len(sents) < 2:
        return {
            "coherence_mean":  0.5,
            "coherence_min":   0.5,
            "coherence_var":   0.0,
        }

    st_model = _get_st()
    embeddings = st_model.encode(sents, convert_to_tensor=True)

    sims = []
    for i in range(len(embeddings) - 1):
        sim = util.cos_sim(embeddings[i], embeddings[i + 1]).item()
        sims.append(sim)

    return {
        "coherence_mean": float(np.mean(sims)),
        "coherence_min":  float(np.min(sims)),
        "coherence_var":  float(np.var(sims)),
    }


# Interaction features
# Rationale: genuine C1/B2 requires rare vocabulary AND complex syntax to co-occur.
# Without interaction terms the model cannot distinguish a text that uses subordinating
# conjunctions with simple everyday vocabulary from one that uses them with rare,
# abstract vocabulary. These three features make that distinction explicit.

def _interaction_features(features: dict) -> dict:
    """
    vocab_syntax_interaction = (1/median_zipf) * avg_dep_depth
        High only when BOTH vocabulary is rare AND syntax is deep.

    lexical_sophistication = (1/median_zipf) * mattr
        High when vocabulary is both rare AND varied.

    content_depth_ratio = lex_density * avg_dep_depth
        Whether syntactic depth is driven by content words rather than function words.
    """
    median_zipf   = features.get("median_zipf",   3.0)
    avg_dep_depth = features.get("avg_dep_depth",  0.0)
    mattr         = features.get("mattr",          0.0)
    lex_density   = features.get("lex_density",    0.0)

    inv_zipf = 1.0 / max(median_zipf, 0.5)

    return {
        "vocab_syntax_interaction": inv_zipf * avg_dep_depth,
        "lexical_sophistication":   inv_zipf * mattr,
        "content_depth_ratio":      lex_density * avg_dep_depth,
    }


FEATURE_NAMES = [
    "n_tokens",
    "n_sentences",
    "avg_sent_len",
    "avg_word_len",
    "short_text_penalty",
    "mattr",
    "lex_density",
    "median_zipf",
    "rep_ratio",
    "avg_dep_depth",
    "sub_ratio",
    "sconj_ratio",
    "verb_ratio",
    "grammar_error_rate",
    "ortho_error_rate",
    "total_error_rate",
    "coherence_mean",
    "coherence_min",
    "coherence_var",
    "vocab_syntax_interaction",
    "lexical_sophistication",
    "content_depth_ratio",
]


def extract_features(text: str) -> dict:
    """
    Run the full feature extraction pipeline on a German learner text.
    Returns a dict with keys matching FEATURE_NAMES; all values are floats.
    """
    if not text or not text.strip():
        return {name: 0.0 for name in FEATURE_NAMES}

    nlp = _get_nlp()
    doc = nlp(text)

    features = {}
    features.update(_fluency_features(doc, text))
    features.update(_lexical_features(doc))
    features.update(_syntactic_features(doc))
    features.update(_accuracy_features(text, doc))
    features.update(_coherence_features(doc))
    features.update(_interaction_features(features))  # must come last

    for name in FEATURE_NAMES:
        if name not in features:
            features[name] = 0.0

    return {name: float(features[name]) for name in FEATURE_NAMES}


def extract_features_vector(text: str) -> np.ndarray:
    """Same as extract_features but returns a numpy array (for direct ML use)."""
    feat_dict = extract_features(text)
    return np.array([feat_dict[name] for name in FEATURE_NAMES], dtype=np.float32)
