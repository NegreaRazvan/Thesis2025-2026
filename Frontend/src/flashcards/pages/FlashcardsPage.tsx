import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import toast from "react-hot-toast";
import useFlashcardSetApi from "../useFlashcardsApi";
import useWriteApi from "../../write/useWriteApi";
import { useConfirm } from "../../core/ConfirmModal";
import CreateSetForm from "../components/CreateSetForm";
import StudyModalLoader from "../components/StudyModalLoader";
import type { FlashcardSummary, ErrorSuggestion } from "../props";
import { Masthead } from "../../core/SharedComponents";
import "../flashcards.css";

const FlashcardsPage = () => {
    const { getAll, create, remove, addCard, markReviewed } = useFlashcardSetApi();
    const { getHistory, getById } = useWriteApi();
    const confirm  = useConfirm();
    const navigate = useNavigate();
    const { t }    = useTranslation();

    const [sets, setSets]         = useState<FlashcardSummary[]>([]);
    const [loading, setLoading]   = useState(true);
    const [search, setSearch]     = useState("");
    const [showForm, setShowForm] = useState(false);
    const [name, setName]         = useState("");
    const [desc, setDesc]         = useState("");
    const [creating, setCreating] = useState(false);

    const [suggestions, setSuggestions] = useState<ErrorSuggestion[]>([]);
    const [showSugg, setShowSugg]       = useState(true);
    const [studyId, setStudyId]         = useState<string | null>(null);

    const timeAgo = (iso?: string): string => {
        if (!iso) return t("flashcards.timeNever");
        const days = Math.floor((Date.now() - new Date(iso).getTime()) / 86_400_000);
        if (days === 0) return t("flashcards.timeToday");
        if (days === 1) return t("flashcards.timeYesterday");
        return t("flashcards.timeAgo", { n: days });
    };

    useEffect(() => {
        getAll().then(setSets).finally(() => setLoading(false));
        loadSuggestions();
    }, []);

    const loadSuggestions = async () => {
        try {
            const history = await getHistory();
            const seen    = new Set<string>();
            const suggs: ErrorSuggestion[] = [];
            for (const s of history.slice(0, 3)) {
                const detail = await getById(s.id);
                for (const err of detail.errors ?? []) {
                    if (!err.suggestions?.length || !err.badText) continue;
                    const key = err.badText.toLowerCase();
                    if (seen.has(key)) continue;
                    seen.add(key);
                    suggs.push({
                        badText: err.badText,
                        suggestion: err.suggestions[0],
                        submissionId: s.id,
                        category: err.category,
                    });
                    if (suggs.length >= 8) break;
                }
                if (suggs.length >= 8) break;
            }
            setSuggestions(suggs);
        } catch {
        }
    };

    const handleCreate = async () => {
        if (!name.trim()) return;
        setCreating(true);
        try {
            const pkg = await create(name.trim(), desc.trim() || undefined);
            setSets(p => [pkg, ...p]);
            setName(""); setDesc(""); setShowForm(false);
            toast.success(t("flashcards.deckCreated"));
        } catch {
            toast.error(t("flashcards.deckCreateError"));
        } finally {
            setCreating(false);
        }
    };

    const handleDelete = async (e: React.MouseEvent, id: string, deckName: string) => {
        e.stopPropagation();
        const ok = await confirm({
            title:        t("flashcards.deleteDeckTitle"),
            message:      t("flashcards.deleteDeckMsg", { name: deckName }),
            confirmLabel: t("common.delete"),
            danger:       true,
        });
        if (!ok) return;
        try {
            await remove(id);
            setSets(p => p.filter(x => x.id !== id));
            toast.success(t("flashcards.deckDeleted"));
        } catch {
            toast.error(t("flashcards.deckDeleteError"));
        }
    };

    const handleAddSuggestion = async (sugg: ErrorSuggestion) => {
        if (!sets.length) { toast(t("flashcards.createDeckFirst")); return; }
        const target = sets[0];
        try {
            await addCard(target.id, sugg.badText, sugg.suggestion);
            toast.success(t("flashcards.addedToSuccess", { name: target.name }));
            setSuggestions(s => s.filter(x => x.badText !== sugg.badText));
            setSets(prev => prev.map(s =>
                s.id === target.id ? { ...s, cardCount: s.cardCount + 1, newCount: s.newCount + 1 } : s
            ));
        } catch {
            toast.error(t("flashcards.addSuggestionError"));
        }
    };

    const handleAddAll = async () => {
        if (!sets.length) { toast(t("flashcards.createDeckFirst")); return; }
        if (!suggestions.length) return;
        const target = sets[0];
        let added = 0;
        for (const sugg of suggestions) {
            try {
                await addCard(target.id, sugg.badText, sugg.suggestion);
                added++;
            } catch {
            }
        }
        if (added > 0) {
            toast.success(t("flashcards.nAdded", { n: added, name: target.name }));
            setSuggestions([]);
            setSets(prev => prev.map(s =>
                s.id === target.id ? { ...s, cardCount: s.cardCount + added, newCount: s.newCount + added } : s
            ));
        }
    };

    const handleStudyComplete = async (id: string) => {
        try {
            await markReviewed(id);
            setSets(prev => prev.map(s =>
                s.id === id
                    ? { ...s, dueCount: 0, newCount: 0, lastReviewedAt: new Date().toISOString() }
                    : s
            ));
        } catch {
            toast.error(t("flashcards.saveReviewError"));
        }
    };

    const filtered = sets.filter(s =>
        !search || s.name.toLowerCase().includes(search.toLowerCase())
    );

    const totalDue    = sets.reduce((a, s) => a + s.dueCount + s.newCount, 0);
    const totalNew    = sets.reduce((a, s) => a + s.newCount, 0);
    const firstDueSet = sets.find(s => s.dueCount > 0 || s.newCount > 0);
    const dueDeckCount = sets.filter(s => s.dueCount + s.newCount > 0).length;

    if (loading) return (
        <div className="page page-enter fc-page">
            <Masthead num="04" title={t("flashcards.title")} sub={t("common.loading")} />
            <div className="fc-skeleton-grid">
                {[1,2,3,4,5,6].map(i => <div key={i} className="fc-skeleton-card" />)}
            </div>
        </div>
    );

    return (
        <div className="page page-enter fc-page">
            {studyId && (
                <StudyModalLoader
                    packageId={studyId}
                    onClose={() => setStudyId(null)}
                    onComplete={() => handleStudyComplete(studyId)}
                />
            )}

            <Masthead
                num="04"
                title={t("flashcards.title")}
                sub={`${sets.length} DECK${sets.length !== 1 ? "S" : ""} · ${totalDue} ${t("flashcards.dueToday")} · ${totalNew} ${t("flashcards.newLabel")}`}
                meta={[
                    { label: "ALGORITHM", value: t("flashcards.algorithm") },
                    { label: t("flashcards.dueToday"), value: String(totalDue) },
                    { label: t("flashcards.newLabel"), value: String(totalNew) },
                ]}
            />

            {showSugg && suggestions.length > 0 && (
                <div className="fc-assessment">
                    <div className="fc-assessment-top">
                        <div>
                            <span className="fc-assessment-badge">{t("flashcards.fromAssessment")}</span>
                            <span className="fc-assessment-msg">
                                {t("flashcards.addCards", { n: suggestions.length, s: suggestions.length !== 1 ? "s" : "" })}
                            </span>
                        </div>
                        <button className="fc-assessment-add-all" onClick={handleAddAll}>
                            {t("flashcards.addAll", { n: suggestions.length })}
                        </button>
                    </div>
                    <div className="fc-assessment-chips">
                        {suggestions.map(s => (
                            <div key={s.badText} className="fc-chip">
                                <span className="fc-chip-bad">{s.badText}</span>
                                <span className="fc-chip-arr">→</span>
                                <span className="fc-chip-fix">{s.suggestion}</span>
                                {s.category && <span className="fc-chip-cat">{s.category}</span>}
                                <button
                                    className="fc-chip-x"
                                    onClick={() => setSuggestions(p => p.filter(x => x.badText !== s.badText))}
                                >
                                    ✕
                                </button>
                            </div>
                        ))}
                    </div>
                </div>
            )}

            <div className="fc-search-wrap">
                <input
                    className="fc-search-input"
                    placeholder={t("flashcards.findDeck")}
                    value={search}
                    onChange={e => setSearch(e.target.value)}
                />
            </div>

            <div className="fc-section-hd">
                <span className="fc-section-num">01</span>
                <span className="fc-section-label">{t("flashcards.yourDecks")}</span>
                <div className="fc-section-rule" />
                <button className="fc-section-action" onClick={() => setShowForm(f => !f)}>
                    {showForm ? t("flashcards.cancelNewDeck") : t("flashcards.newDeck")}
                </button>
            </div>

            {showForm && (
                <CreateSetForm
                    name={name} desc={desc} creating={creating}
                    onNameChange={setName} onDescChange={setDesc}
                    onCreate={handleCreate} onCancel={() => setShowForm(false)}
                />
            )}

            {filtered.length === 0 && !showForm ? (
                <div className="fc-empty">
                    <div className="fc-empty-num">{t("flashcards.emptyTitle")}</div>
                    <div className="fc-empty-title">{search ? t("flashcards.noResults") : t("flashcards.noDecks")}</div>
                    <div className="fc-empty-sub">
                        {search
                            ? t("flashcards.noResultsHint", { search })
                            : t("flashcards.noDecksHint")
                        }
                    </div>
                </div>
            ) : (
                <div className="fc-grid">
                    {filtered.map((pkg, idx) => {
                        const due      = pkg.dueCount;
                        const newC     = pkg.newCount;
                        const total    = pkg.cardCount;
                        const pendPct  = total > 0 ? ((due + newC) / total) * 100 : 0;
                        const hasReview = due > 0 || newC > 0;

                        return (
                            <div
                                key={pkg.id}
                                className="fc-deck-card"
                                onClick={() => navigate(`/flashcards/${pkg.id}`)}
                            >
                                <span className="fc-deck-num">#{String(idx + 1).padStart(2, "0")}</span>
                                <button
                                    className="fc-deck-delete"
                                    style={{ top: 28 }}
                                    onClick={e => handleDelete(e, pkg.id, pkg.name)}
                                    title={t("common.delete")}
                                >
                                    ✕
                                </button>

                                <h3 className="fc-deck-name">{pkg.name}</h3>
                                {pkg.description && <p className="fc-deck-desc">{pkg.description}</p>}

                                <hr className="fc-deck-rule" />

                                <div className="fc-deck-cards-row">
                                    <span className="fc-deck-cards-lbl">{t("flashcards.cards")}</span>
                                    <span className="fc-deck-cards-num">{total}</span>
                                </div>

                                <div className="fc-deck-duenew">
                                    <span className="fc-deck-due">
                                        {t("flashcards.dueToday").split(" ")[0]} <span className="fc-deck-due-val">{due}</span>
                                    </span>
                                    <span className="fc-deck-new">
                                        {t("flashcards.newLabel")} <span className="fc-deck-new-val">{newC}</span>
                                    </span>
                                </div>

                                <div className="fc-deck-bar">
                                    <div className="fc-deck-bar-fill" style={{ width: `${pendPct}%` }} />
                                </div>

                                <div className="fc-deck-foot">
                                    <span className="fc-deck-last">{t("flashcards.last", { time: timeAgo(pkg.lastReviewedAt) })}</span>
                                    <button
                                        className={`fc-deck-study-btn${hasReview ? " fc-deck-study-btn--active" : ""}`}
                                        onClick={e => { e.stopPropagation(); setStudyId(pkg.id); }}
                                    >
                                        {hasReview ? t("flashcards.studyN", { n: due + newC }) : t("flashcards.study")}
                                    </button>
                                </div>
                            </div>
                        );
                    })}

                    <div className="fc-deck-new-cell" onClick={() => setShowForm(true)}>
                        <span>{t("flashcards.newDeckCell")}</span>
                    </div>
                </div>
            )}

            <div className="fc-section-hd" style={{ marginTop: 32 }}>
                <span className="fc-section-num">02</span>
                <span className="fc-section-label">{t("flashcards.todaysReview")}</span>
                <div className="fc-section-rule" />
                {totalDue > 0 && (
                    <span className="fc-section-count">{t("flashcards.cardsQueued", { n: totalDue })}</span>
                )}
            </div>

            <div className="fc-review-box">
                <div>
                    <div className="fc-review-heading">
                        {totalDue > 0
                            ? <>{t("flashcards.cardsReady", { n: totalDue })}</>
                            : <>{t("flashcards.allCaughtUp")}</>
                        }
                    </div>
                    <div className="fc-review-sub">
                        {totalDue > 0
                            ? t(dueDeckCount === 1 ? "flashcards.cardsReadySub_one" : "flashcards.cardsReadySub_other", { n: dueDeckCount })
                            : t("flashcards.caughtUpSub")
                        }
                    </div>
                </div>
                <button
                    className="fc-review-start-btn"
                    disabled={!firstDueSet}
                    onClick={() => firstDueSet && setStudyId(firstDueSet.id)}
                >
                    {t("flashcards.startReview")}
                </button>
            </div>
        </div>
    );
};

export default FlashcardsPage;
