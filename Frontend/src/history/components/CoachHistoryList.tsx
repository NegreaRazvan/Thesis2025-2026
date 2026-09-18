import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import type { CoachSessionSummary, CoachSessionDetail } from "../../coach/props";
import useCoachApi from "../../coach/useCoachApi";

interface CoachHistoryListProps {
    sessions: CoachSessionSummary[];
    loading: boolean;
}

const CEFR_TAG: Record<string, string> = {
    A1: "cefr-a1", A2: "cefr-a2",
    B1: "cefr-b1", B2: "cefr-b2", C1: "cefr-c1",
};

const CoachHistoryList = ({ sessions, loading }: CoachHistoryListProps) => {
    const navigate = useNavigate();
    const { getSessionDetail } = useCoachApi();
    const { t } = useTranslation();

    const [expanded, setExpanded] = useState<string | null>(null);
    const [detail,   setDetail]   = useState<CoachSessionDetail | null>(null);
    const [fetching, setFetching] = useState(false);

    const toggleExpand = async (id: string) => {
        if (expanded === id) { setExpanded(null); setDetail(null); return; }
        setExpanded(id);
        setDetail(null);
        setFetching(true);
        try {
            const d = await getSessionDetail(id);
            setDetail(d);
        } finally {
            setFetching(false);
        }
    };

    if (loading) return (
        <div className="history-list">
            {[1,2,3,4,5].map(i => <div key={i} className="history-skeleton-row" />)}
        </div>
    );

    if (sessions.length === 0) return (
        <div className="history-empty">
            <div className="history-empty-num">{t("history.coachArchiveEmpty")}</div>
            <div className="history-empty-title">{t("history.noChats")}</div>
            <div className="history-empty-sub">{t("history.noChatsSub")}</div>
        </div>
    );

    return (
        <div className="history-list">
            <div className="history-table-head">
                <span>{t("history.colNum")}</span>
                <span>{t("history.colLevel")}</span>
                <span>{t("history.colPreview")}</span>
                <span>{t("history.colDate")}</span>
                <span>{t("history.colTurns")}</span>
            </div>

            {sessions.map((s, idx) => {
                const rowNum  = String(idx + 1).padStart(3, "0");
                const dateStr = new Date(s.startedAt).toLocaleDateString("en-GB", {
                    day: "numeric", month: "short", year: "2-digit",
                });
                const snippet = s.germanText.slice(0, 56) + (s.germanText.length > 56 ? "…" : "");
                const isOpen  = expanded === s.id;

                return (
                    <div key={s.id} className={`history-item${isOpen ? " history-item--active" : ""}`}>
                        <button className="history-row" onClick={() => toggleExpand(s.id)}>
                            <span className="history-row-num"># {rowNum}</span>
                            <div className="history-level-cell">
                                <span className={`history-level-text history-level-text--${CEFR_TAG[s.predictedCefr] ?? ""}`}>
                                    {s.predictedCefr}
                                </span>
                            </div>
                            <div className="history-row-meta">
                                <span className="history-row-snippet">{snippet}</span>
                            </div>
                            <span className="history-row-words">{dateStr}</span>
                            <span className="history-row-words">{s.turnCount}t</span>
                            <span className="history-row-toggle">{isOpen ? "−" : "+"}</span>
                        </button>

                        {isOpen && (
                            <div className="history-detail">
                                {fetching && (
                                    <div className="history-detail-section-hd">{t("history.loadingConversation")}</div>
                                )}

                                {detail && (
                                    <>
                                        <div>
                                            <div className="history-detail-section-hd">
                                                {t("history.conversation", { n: detail.messages.length })}
                                            </div>
                                            <div className="coach-hist-thread">
                                                {detail.messages.map((msg, i) => (
                                                    <div
                                                        key={i}
                                                        className={`coach-hist-bubble coach-hist-bubble--${msg.role}`}
                                                    >
                                                        <div className="coach-hist-bubble-who">
                                                            {msg.role === "assistant" ? "Lena" : t("coach.you")}
                                                        </div>
                                                        <div className="coach-hist-bubble-text">{msg.content}</div>
                                                    </div>
                                                ))}
                                            </div>
                                        </div>

                                        <div className="history-detail-actions">
                                            <button
                                                className="history-discuss-btn"
                                                onClick={() => navigate("/coach", {
                                                    state: { text: s.germanText, cefr: s.predictedCefr, num: rowNum },
                                                })}
                                            >
                                                {t("history.continueCta")}
                                            </button>
                                        </div>
                                    </>
                                )}
                            </div>
                        )}
                    </div>
                );
            })}
        </div>
    );
};

export default CoachHistoryList;
