"""
Train a CEFR classifier on MERLIN features and save it for inference.

Steps:
  1. Load extracted CAF features (outputs/merlin_features.csv)
  2. Train an MLP classifier with 5-fold nested cross-validation
  3. Report accuracy and Macro-F1
  4. Save the final model to outputs/cefr_model.pkl

Usage:
    python train_cefr_classifier.py
"""

import ast
import logging
import warnings
from pathlib import Path

import joblib
import numpy as np
import pandas as pd
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
import seaborn as sns

from sklearn.neural_network import MLPClassifier
from sklearn.preprocessing import StandardScaler, LabelEncoder
from sklearn.model_selection import StratifiedKFold, GridSearchCV
from sklearn.pipeline import Pipeline
from sklearn.metrics import (
    accuracy_score, f1_score,
    confusion_matrix, classification_report,
)

warnings.filterwarnings("ignore")

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s  %(levelname)s  %(message)s",
    datefmt="%H:%M:%S",
)
log = logging.getLogger(__name__)


FEATURES_CSV = Path("outputs/merlin_features.csv")
OUT_DIR      = Path("outputs")
MODEL_PATH   = OUT_DIR / "cefr_model.pkl"

CEFR_LEVELS = ["A1", "A2", "B1", "B2", "C1"]

CAF_FEATURES = [
    "n_tokens", "n_sentences", "avg_sent_len", "avg_word_len", "short_text_penalty",
    "mattr", "lex_density", "median_zipf", "rep_ratio",
    "avg_dep_depth", "sub_ratio", "sconj_ratio", "verb_ratio",
    "grammar_error_rate", "ortho_error_rate", "total_error_rate",
    "coherence_mean", "coherence_min", "coherence_var",
    # Interaction features -- fix vocab/syntax mismatch at higher CEFR levels
    "vocab_syntax_interaction", "lexical_sophistication", "content_depth_ratio",
]


def load_data() -> tuple[np.ndarray, np.ndarray, LabelEncoder, list]:
    if not FEATURES_CSV.exists():
        raise FileNotFoundError(
            f"Could not find {FEATURES_CSV}\n"
            "Run run_pipeline.py first to generate it."
        )

    df = pd.read_csv(FEATURES_CSV)
    log.info(f"Loaded {len(df)} rows from {FEATURES_CSV}")

    df = df[df["cefr_gold"].notna()].copy()
    df["cefr_gold"] = df["cefr_gold"].str.strip()

    df = df[df["cefr_gold"].isin(CEFR_LEVELS)].copy()
    log.info(f"Rows after filtering: {len(df)}")
    log.info(f"CEFR distribution:\n{df['cefr_gold'].value_counts().sort_index().to_string()}")

    available = [c for c in CAF_FEATURES if c in df.columns]
    missing   = set(CAF_FEATURES) - set(available)
    if missing:
        log.warning(f"Missing feature columns (will treat as 0): {missing}")

    X = df[available].fillna(0).values.astype(np.float32)

    le = LabelEncoder()
    le.fit(CEFR_LEVELS)
    y = le.transform(df["cefr_gold"])

    return X, y, le, available


PARAM_GRID = {
    "mlp__hidden_layer_sizes": [(64,), (128, 64), (128, 64, 32)],
    "mlp__alpha":              [1e-4, 1e-3, 1e-2],
    "mlp__learning_rate_init": [1e-3, 5e-4],
}


def build_pipeline() -> Pipeline:
    return Pipeline([
        ("scaler", StandardScaler()),
        ("mlp",    MLPClassifier(
            max_iter=500,
            early_stopping=True,
            validation_fraction=0.1,
            n_iter_no_change=20,
            random_state=42,
        )),
    ])


def cross_validate(X: np.ndarray, y: np.ndarray) -> list[dict]:
    outer_cv = StratifiedKFold(n_splits=5, shuffle=True, random_state=42)
    inner_cv = StratifiedKFold(n_splits=3, shuffle=True, random_state=42)

    fold_results = []
    log.info("\nRunning 5-fold cross-validation...")

    for fold, (train_idx, val_idx) in enumerate(outer_cv.split(X, y), 1):
        X_tr, X_val = X[train_idx], X[val_idx]
        y_tr, y_val = y[train_idx], y[val_idx]

        gs = GridSearchCV(
            build_pipeline(),
            PARAM_GRID,
            cv=inner_cv,
            scoring="f1_macro",
            n_jobs=-1,
            refit=True,
        )
        gs.fit(X_tr, y_tr)

        y_pred = gs.predict(X_val)
        acc    = accuracy_score(y_val, y_pred)
        f1     = f1_score(y_val, y_pred, average="macro")

        fold_results.append({
            "fold":        fold,
            "acc":         acc,
            "f1":          f1,
            "best_params": gs.best_params_,
        })
        log.info(
            f"  Fold {fold}: Acc={acc:.3f}  Macro-F1={f1:.3f}  "
            f"arch={gs.best_params_['mlp__hidden_layer_sizes']}"
        )

    mean_acc = np.mean([r["acc"] for r in fold_results])
    mean_f1  = np.mean([r["f1"]  for r in fold_results])
    std_acc  = np.std( [r["acc"] for r in fold_results])
    std_f1   = np.std( [r["f1"]  for r in fold_results])

    log.info(f"\n  CV Result: Acc = {mean_acc:.3f} +/- {std_acc:.3f}  |  Macro-F1 = {mean_f1:.3f} +/- {std_f1:.3f}")
    log.info(  f"  Chance baseline (5 classes) = 0.200")

    return fold_results


def fit_final_model(
    X: np.ndarray,
    y: np.ndarray,
    fold_results: list[dict],
) -> Pipeline:
    """
    Re-train on the full dataset using the most common best hyperparameters from CV folds.
    CV already gave an honest performance estimate -- using all data maximises real-world accuracy.
    """
    from collections import Counter

    hls_votes   = Counter(str(r["best_params"]["mlp__hidden_layer_sizes"]) for r in fold_results)
    alpha_votes = Counter(r["best_params"]["mlp__alpha"]              for r in fold_results)
    lr_votes    = Counter(r["best_params"]["mlp__learning_rate_init"] for r in fold_results)

    best_hls   = ast.literal_eval(hls_votes.most_common(1)[0][0])
    best_alpha = alpha_votes.most_common(1)[0][0]
    best_lr    = lr_votes.most_common(1)[0][0]

    log.info(f"\nFitting final model on full dataset...")
    log.info(f"  Architecture : {best_hls}")
    log.info(f"  L2 alpha     : {best_alpha}")
    log.info(f"  Learning rate: {best_lr}")

    pipe = Pipeline([
        ("scaler", StandardScaler()),
        ("mlp",    MLPClassifier(
            hidden_layer_sizes=best_hls,
            alpha=best_alpha,
            learning_rate_init=best_lr,
            max_iter=500,
            early_stopping=True,
            validation_fraction=0.1,
            n_iter_no_change=20,
            random_state=42,
        )),
    ])
    pipe.fit(X, y)
    return pipe


def save_model(
    pipeline:      Pipeline,
    le:            LabelEncoder,
    feature_names: list[str],
    fold_results:  list[dict],
) -> None:
    """
    Saves a single .pkl bundle containing:
      - pipeline      : scaler + trained MLP
      - label_encoder : int -> CEFR string
      - feature_names : ordered list of features the model expects
      - cv_acc / cv_f1: reported performance, stored for reference
    """
    bundle = {
        "pipeline":      pipeline,
        "label_encoder": le,
        "feature_names": feature_names,
        "cv_acc":        np.mean([r["acc"] for r in fold_results]),
        "cv_f1":         np.mean([r["f1"]  for r in fold_results]),
        "cefr_levels":   CEFR_LEVELS,
    }
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    joblib.dump(bundle, MODEL_PATH)
    log.info(f"\n  Model saved -> {MODEL_PATH.resolve()}")
    log.info(f"     (CV Acc={bundle['cv_acc']:.3f}, CV Macro-F1={bundle['cv_f1']:.3f})")


def plot_confusion_matrix(
    X: np.ndarray,
    y: np.ndarray,
    pipeline: Pipeline,
    le: LabelEncoder,
) -> None:
    y_pred  = pipeline.predict(X)
    cm      = confusion_matrix(y, y_pred, labels=le.transform(CEFR_LEVELS))
    cm_norm = cm.astype(float) / cm.sum(axis=1, keepdims=True)

    fig, ax = plt.subplots(figsize=(7, 5.5))
    sns.heatmap(
        cm_norm, annot=True, fmt=".2f", cmap="Blues",
        xticklabels=CEFR_LEVELS, yticklabels=CEFR_LEVELS,
        ax=ax, vmin=0, vmax=1,
    )
    ax.set_xlabel("Predicted CEFR level", fontsize=12)
    ax.set_ylabel("True CEFR level",      fontsize=12)
    ax.set_title("Confusion Matrix (final model, full training data)", fontsize=12, pad=10)
    plt.tight_layout()
    path = OUT_DIR / "confusion_matrix.png"
    fig.savefig(path, dpi=150)
    plt.close(fig)
    log.info(f"  Saved: {path}")


def print_classification_report(
    X: np.ndarray,
    y: np.ndarray,
    pipeline: Pipeline,
) -> None:
    y_pred = pipeline.predict(X)
    report = classification_report(y, y_pred, target_names=CEFR_LEVELS, digits=3)
    path   = OUT_DIR / "classification_report.txt"
    with open(path, "w") as f:
        f.write(report)
    log.info(f"  Saved: {path}")
    print("\nClassification Report (training data -- see CV scores for unbiased estimate):")
    print(report)


def main():
    log.info("CEFR Classifier -- Training")

    X, y, le, feature_names = load_data()

    fold_results = cross_validate(X, y)

    final_model = fit_final_model(X, y, fold_results)

    save_model(final_model, le, feature_names, fold_results)

    log.info("\nGenerating plots...")
    plot_confusion_matrix(X, y, final_model, le)
    print_classification_report(X, y, final_model)

    log.info("\nTraining complete.")
    log.info(f"To predict a new text, run:  python predict.py 'your text here'")


if __name__ == "__main__":
    main()
