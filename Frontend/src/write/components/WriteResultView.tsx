import { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import toast from "react-hot-toast";
import type { SubmissionResponseProps, ErrorItemProps } from "../props";
import { isSpellingError } from "../spellingUtils";
import { CEFRStamp, SectionHead } from "../../core/SharedComponents";
import { CEFR_NAME, CEFR_DESC, CEFR_LEVELS } from "../../core/cefr";
import useFlashcardSetApi from "../../flashcards/useFlashcardsApi";
import { buildGermanCard } from "../../core/flashcardUtils";
import type { FlashcardSummary } from "../../flashcards/props";

const POS_LABEL: Record<string, string> = { NOUN: "noun", VERB: "verb", ADJ: "adj.", ADV: "adv." };

const VOCAB_THRESHOLD = 0.05;

function grade(value: number, good: number, bad: number): "good" | "ok" | "poor" {
    const better = good > bad;
    if (better ? value >= good : value <= good) return "good";
    if (better ? value >= (good + bad) / 2 : value <= (good + bad) / 2) return "ok";
    return "poor";
}

interface Props {
    result:          SubmissionResponseProps;
    text:            string;
    onStartCoaching: () => void;
    onReset:         () => void;
}

const WriteResultView = ({ result, text, onStartCoaching, onReset }: Props) => {
    const navigate = useNavigate();
    const { t } = useTranslation();
    const { getAll, addCard } = useFlashcardSetApi();

    const [sets, setSets]           = useState<FlashcardSummary[]>([]);
    const [selectedSet, setSelectedSet] = useState("");
    const [addedCards, setAddedCards]   = useState<Set<string>>(new Set());
    const [addingCard, setAddingCard]   = useState<string | null>(null);
    const [vocabOpen, setVocabOpen]     = useState(false);

    useEffect(() => {
        const load = async () => {
            try {
                const s = await getAll();
                setSets(s);
                if (s.length > 0) setSelectedSet(s[0].id);
            } catch {
                toast.error(t("write.loadSetsError"));
            }
        };
        load();
    }, []);

    const level    = result.predictedCefr;
    const lvlName  = CEFR_NAME[level] ?? level;
    const lvlDesc  = CEFR_DESC[level] ?? "";
    const date     = new Date(result.submittedAt).toLocaleDateString("en-GB", { day: "numeric", month: "short" });

    const f            = result.features;
    const mattrPct     = +(f.mattr * 100).toFixed(1);
    const accuracyPct  = Math.max(0, 100 - f.grammarErrorRate * 100).toFixed(0);
    const vocabG       = grade(mattrPct, 70, 40);
    const grammarG     = grade(+accuracyPct, 90, 60);
    const syntaxG      = grade(f.avgDepDepth, 3.5, 1.5);
    const wordSophG    = grade(f.medianZipf, 5, 3.5);

    const gradeLabel = (g: string) =>
        g === "good" ? t("write.gradeGood") : g === "ok" ? t("write.gradeFair") : t("write.gradeNeedsWork");

    const interesting = (result.vocabCandidates ?? []).filter(c => c.actualProbability < VOCAB_THRESHOLD);
    const spellingErrors = result.errors.filter(isSpellingError);

    const handleAddSpelling = async (err: ErrorItemProps) => {
        if (!selectedSet) { toast.error(t("flashcards.createDeckFirst")); return; }
        const key   = `${err.offsetStart}-${err.badText}`;
        const lemma = err.lemma || err.suggestions[0] || err.badText;
        setAddingCard(key);
        try {
            const { front, back } = buildGermanCard({ lemma, englishTranslation: err.englishTranslation, article: err.article, plural: err.plural });
            await addCard(selectedSet, front, back);
            setAddedCards(prev => new Set(prev).add(key));
            toast.success(t("write.addCardSuccess", { lemma }));
        } catch {
            toast.error(t("write.addCardError"));
        } finally {
            setAddingCard(null);
        }
    };

    return (
        <div>
            <div className="write-result-hero">
                <div className="write-result-stamp-col">
                    <CEFRStamp level={level} name={lvlName} date={date} size="lg" />
                    <div className="write-result-level-name">{level} · {lvlName}</div>
                    <div className="write-result-level-desc">{lvlDesc}</div>
                </div>

                <div className="write-conf-col">
                    {CEFR_LEVELS.map(lvl => {
                        const pct = Math.round((result.confidence[lvl] ?? 0) * 100);
                        return (
                            <div key={lvl} className={`conf-row${lvl === level ? " conf-row--active" : ""}`}>
                                <span className="conf-row-lbl">{lvl}</span>
                                <div className="conf-bar-track">
                                    <div className="conf-bar-fill" style={{ width: `${pct}%` }} />
                                </div>
                                <span className="conf-row-pct">{pct}%</span>
                            </div>
                        );
                    })}
                </div>
            </div>

            <SectionHead num="01" title={t("write.yourText")} right={<span className="label">{text.trim().split(/\s+/).filter(Boolean).length} {t("write.words")}</span>} />
            <div className="write-original-text" style={{ marginTop: 8 }}>
                <div className="write-original-text-body">{text.trim()}</div>
            </div>

            {result.aiFeedback && (
                <>
                    <SectionHead num="02" title={t("write.tutorFeedback")} right={<span className="label">Lena · AI</span>} />
                    <div className="write-feedback" style={{ marginTop: 8 }}>
                        <div className="write-feedback-num">AI Tutor Feedback · {date}</div>
                        <div className="write-feedback-text">{result.aiFeedback}</div>
                    </div>
                </>
            )}

            {result.errors.length > 0 && (
                <>
                    <SectionHead num="03" title={t("write.grammarIssues")} right={<span className="label">{result.errors.length} {t("write.found")}</span>} />
                    <div className="write-error-list" style={{ marginTop: 8 }}>
                        {result.errors.map((err, i) => (
                            <div key={i} className="write-error-row">
                                <span className="write-error-cat">{err.category || t("common.error")}</span>
                                <div>
                                    <div className="write-error-msg">{err.message}</div>
                                    {err.badText && <span className="write-error-bad">{err.badText}</span>}
                                    {err.suggestions?.length > 0 && (
                                        <div className="write-error-suggestions">
                                            {err.suggestions.slice(0, 3).map((s, j) => (
                                                <span key={j} className="write-error-fix">{s}</span>
                                            ))}
                                        </div>
                                    )}
                                </div>
                            </div>
                        ))}
                    </div>
                </>
            )}

            <SectionHead num="04" title={t("write.features")} right={<span className="label">{t("write.fiveMetrics")}</span>} />
            <div className="write-feature-grid" style={{ marginTop: 8 }}>
                <div className="write-feature-tile">
                    <div className="write-feature-val">{mattrPct}%</div>
                    <div className="write-feature-lbl">{t("write.vocabVariety")}</div>
                    <div className="write-feature-desc">{t("write.uniqueWordRatio")}</div>
                    <span className={`write-feature-grade write-feature-grade--${vocabG}`}>{gradeLabel(vocabG)}</span>
                </div>
                <div className="write-feature-tile">
                    <div className="write-feature-val">{accuracyPct}%</div>
                    <div className="write-feature-lbl">{t("write.grammarAccuracy")}</div>
                    <div className="write-feature-desc">{t("write.errorFreeText")}</div>
                    <span className={`write-feature-grade write-feature-grade--${grammarG}`}>{gradeLabel(grammarG)}</span>
                </div>
                <div className="write-feature-tile">
                    <div className="write-feature-val">{f.avgDepDepth?.toFixed(1)}</div>
                    <div className="write-feature-lbl">{t("write.syntaxDepth")}</div>
                    <div className="write-feature-desc">{t("write.avgDepDepth")}</div>
                    <span className={`write-feature-grade write-feature-grade--${syntaxG}`}>{gradeLabel(syntaxG)}</span>
                </div>
                <div className="write-feature-tile">
                    <div className="write-feature-val">{f.medianZipf?.toFixed(1)}</div>
                    <div className="write-feature-lbl">{t("write.wordSophistication")}</div>
                    <div className="write-feature-desc">{t("write.medianZipf")}</div>
                    <span className={`write-feature-grade write-feature-grade--${wordSophG}`}>{gradeLabel(wordSophG)}</span>
                </div>
                <div className="write-feature-tile">
                    <div className="write-feature-val">{Math.round(f.nTokens)}</div>
                    <div className="write-feature-lbl">{t("write.tokensAnalysed")}</div>
                    <div className="write-feature-desc">{t("write.totalTokens")}</div>
                </div>
            </div>

            {interesting.length > 0 && (
                <>
                    <SectionHead
                        num="05"
                        title={t("write.contextSuggestions")}
                        right={<span className="label">GermanBERT · {interesting.length} item{interesting.length !== 1 ? "s" : ""}</span>}
                    />
                    <div className="write-vocab-panel" style={{ marginTop: 8 }}>
                        <button className="write-vocab-toggle" onClick={() => setVocabOpen(o => !o)}>
                            <div>
                                <div className="write-vocab-toggle-title">
                                    {t("write.flaggedByBERT", { n: interesting.length, s: interesting.length !== 1 ? "s" : "" })}
                                </div>
                                <div className="write-vocab-toggle-sub">
                                    {t("write.lowConfidenceSub")}
                                </div>
                            </div>
                            <span className="write-vocab-toggle-chevron">{vocabOpen ? "▲" : "▼"}</span>
                        </button>

                        {vocabOpen && (
                            <div className="write-vocab-body">
                                <div className="write-vocab-intro">
                                    {t("write.notNecessarilyError")}
                                </div>
                                <div className="write-vocab-cards">
                                    {interesting.map(c => {
                                        const confPct = Math.round(c.actualProbability * 100);
                                        return (
                                            <div key={c.token} className="write-vocab-card">
                                                <div className="write-vocab-card-top">
                                                    <span className="write-vocab-word">{c.token}</span>
                                                    <span className="write-vocab-pos">{POS_LABEL[c.pos] ?? c.pos.toLowerCase()}</span>
                                                </div>
                                                <div className="write-vocab-badges">
                                                    <span className="write-vocab-conf-badge">
                                                        {confPct < 1 ? "<1%" : `${confPct}%`} {t("write.confidence")}
                                                    </span>
                                                    <span className="write-vocab-zipf-badge">
                                                        freq {c.zipfScore.toFixed(1)}/7
                                                    </span>
                                                </div>
                                                {c.topAlternatives.length > 0 && (
                                                    <>
                                                        <span className="write-vocab-alts-label">{t("write.bertExpected")}</span>
                                                        <div className="write-vocab-alts">
                                                            {c.topAlternatives.slice(0, 4).map(a => (
                                                                <span key={a} className="write-vocab-alt">{a}</span>
                                                            ))}
                                                        </div>
                                                    </>
                                                )}
                                                <p className="write-vocab-reason">{c.reason}</p>
                                            </div>
                                        );
                                    })}
                                </div>
                            </div>
                        )}
                    </div>
                </>
            )}

            {spellingErrors.length > 0 && (
                <>
                    <SectionHead
                        num="06"
                        title={t("write.spelling")}
                        right={<span className="label">{spellingErrors.length} {t("write.found")}</span>}
                    />
                    <div className="write-spelling-panel" style={{ marginTop: 8 }}>
                        <div className="write-spelling-hd">
                            <div className="write-spelling-info">
                                <div className="write-spelling-title">{t("write.spellingMistakes")}</div>
                                <div className="write-spelling-sub">{t("write.spellingDesc")}</div>
                            </div>
                            {sets.length > 0 ? (
                                <div className="write-spelling-deck-picker">
                                    <span className="write-spelling-deck-label">{t("write.addTo")}</span>
                                    <select value={selectedSet} onChange={e => setSelectedSet(e.target.value)}>
                                        {sets.map(s => <option key={s.id} value={s.id}>{s.name}</option>)}
                                    </select>
                                </div>
                            ) : (
                                <div className="write-spelling-no-sets">
                                    <a href="/flashcards">{t("write.createSetLink")}</a>{t("write.createSetSuffix")}
                                </div>
                            )}
                        </div>
                        <div className="write-spelling-cards">
                            {spellingErrors.map(err => {
                                const key        = `${err.offsetStart}-${err.badText}`;
                                const correction = err.suggestions[0] ?? "—";
                                const lemma      = err.lemma || correction;
                                const isAdded    = addedCards.has(key);
                                const isAdding   = addingCard === key;
                                return (
                                    <div key={key} className={`write-spelling-card${isAdded ? " write-spelling-card--added" : ""}`}>
                                        <div>
                                            <div className="write-spelling-words">
                                                <span className="write-spelling-wrong">{err.badText}</span>
                                                <span className="write-spelling-arrow">→</span>
                                                <span className="write-spelling-correct">{lemma}</span>
                                            </div>
                                            {err.englishTranslation && (
                                                <span className="write-spelling-translation">"{err.englishTranslation}"</span>
                                            )}
                                        </div>
                                        <button
                                            className="btn btn--sm"
                                            onClick={() => handleAddSpelling(err)}
                                            disabled={isAdded || !!isAdding || !selectedSet}
                                            style={isAdded ? { color: "var(--green)", borderColor: "var(--green)" } : {}}
                                        >
                                            {isAdding ? t("write.adding") : isAdded ? t("write.added") : t("write.addBtn")}
                                        </button>
                                    </div>
                                );
                            })}
                        </div>
                    </div>
                </>
            )}

            {result.shortTextWarning && (
                <div className="write-short-warn">{t("write.shortTextWarn")}</div>
            )}

            <div className="write-coach-strip">
                <div>
                    <div className="write-coach-strip-title">{t("write.coachStripTitle")}</div>
                    <div className="write-coach-strip-sub">{t("write.coachStripSub")}</div>
                </div>
                <div style={{ display: "flex", gap: 10, flexShrink: 0 }}>
                    <button className="btn" onClick={onReset}>{t("write.newText")}</button>
                    <button className="btn btn--primary" onClick={onStartCoaching}>{t("write.openCoachBtn")}</button>
                </div>
            </div>
        </div>
    );
};

export default WriteResultView;
