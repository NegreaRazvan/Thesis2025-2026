import { useState } from "react";
import { useTranslation } from "react-i18next";
import type { VocabGameSession } from "../../vocabgame/props";

interface GameHistoryListProps {
    sessions: VocabGameSession[];
    loading: boolean;
}

const GameHistoryList = ({ sessions, loading }: GameHistoryListProps) => {
    const [expanded, setExpanded] = useState<string | null>(null);
    const { t } = useTranslation();

    if (loading) return (
        <div className="history-list">
            {[1,2,3,4,5].map(i => <div key={i} className="history-skeleton-row" />)}
        </div>
    );

    if (sessions.length === 0) return (
        <div className="history-empty">
            <div className="history-empty-num">{t("history.gameArchiveEmpty")}</div>
            <div className="history-empty-title">{t("history.noGames")}</div>
            <div className="history-empty-sub">{t("history.noGamesSub")}</div>
        </div>
    );

    return (
        <div className="history-list">
            {sessions.map((g, idx) => {
                const rowNum  = String(idx + 1).padStart(2, "0");
                const dateStr = new Date(g.playedAt).toLocaleDateString("en-GB", {
                    day: "numeric", month: "short", year: "numeric",
                    hour: "2-digit", minute: "2-digit",
                } as Intl.DateTimeFormatOptions);
                const isOpen = expanded === g.id;

                return (
                    <div key={g.id} className={`history-item${isOpen ? " history-item--active" : ""}`}>
                        <button
                            className="history-row"
                            style={{ gridTemplateColumns: "40px 64px 1fr auto 28px" }}
                            onClick={() => setExpanded(isOpen ? null : g.id)}
                        >
                            <span className="history-row-num">{rowNum}</span>
                            <div className="history-game-score">{g.score}</div>
                            <div className="history-row-meta">
                                <span className="history-row-date">{g.displayName}</span>
                                <span className="history-row-snippet">{dateStr}</span>
                            </div>
                            <span className="history-row-words">{g.validWords.length} {t("history.valid")}</span>
                            <span className="history-row-toggle">{isOpen ? "▲" : "▼"}</span>
                        </button>

                        {isOpen && (
                            <div className="history-detail">
                                {g.validWords.length > 0 && (
                                    <div>
                                        <div className="history-detail-section-hd">
                                            {t("history.validWords", { n: g.validWords.length })}
                                        </div>
                                        <div className="history-words-grid">
                                            {g.validWords.map(w => (
                                                <span key={w} className="history-word-valid">{w}</span>
                                            ))}
                                        </div>
                                    </div>
                                )}
                                {g.misspelledWords.length > 0 && (
                                    <div>
                                        <div className="history-detail-section-hd">
                                            {t("history.misspelledWords", { n: g.misspelledWords.length })}
                                        </div>
                                        <div className="history-errors-table">
                                            {g.misspelledWords.map(mw => (
                                                <div key={mw.word} className="history-error-row">
                                                    <span className="history-error-cat">{mw.word}</span>
                                                    <span className="history-error-msg" />
                                                    {mw.suggestions[0] && (
                                                        <span className="history-error-fix">→ {mw.suggestions[0]}</span>
                                                    )}
                                                </div>
                                            ))}
                                        </div>
                                    </div>
                                )}
                            </div>
                        )}
                    </div>
                );
            })}
        </div>
    );
};

export default GameHistoryList;
