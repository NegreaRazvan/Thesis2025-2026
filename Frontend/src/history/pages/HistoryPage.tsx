import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import useWriteApi from "../../write/useWriteApi";
import useVocabGameApi from "../../vocabgame/useVocabGameApi";
import useCoachApi from "../../coach/useCoachApi";
import type { SubmissionSummaryProps } from "../../write/props";
import type { VocabGameSession } from "../../vocabgame/props";
import type { CoachSessionSummary } from "../../coach/props";
import { Masthead } from "../../core/SharedComponents";
import WritingHistoryList from "../components/WritingHistoryList";
import GameHistoryList from "../components/GameHistoryList";
import CoachHistoryList from "../components/CoachHistoryList";
import "../history.css";

type Tab = "writing" | "coach" | "game";

const HistoryPage = () => {
    const { getHistory }                     = useWriteApi();
    const { getHistory: getGameHistory }     = useVocabGameApi();
    const { getSessions: getCoachSessions }  = useCoachApi();
    const { t } = useTranslation();

    const [tab,           setTab]           = useState<Tab>("writing");
    const [history,       setHistory]       = useState<SubmissionSummaryProps[]>([]);
    const [gameHistory,   setGameHistory]   = useState<VocabGameSession[]>([]);
    const [coachSessions, setCoachSessions] = useState<CoachSessionSummary[]>([]);
    const [loading,       setLoading]       = useState(true);
    const [gameLoading,   setGameLoading]   = useState(true);
    const [coachLoading,  setCoachLoading]  = useState(true);

    useEffect(() => {
        getHistory().then(setHistory).finally(() => setLoading(false));
        getGameHistory().then(setGameHistory).finally(() => setGameLoading(false));
        getCoachSessions().then(setCoachSessions).finally(() => setCoachLoading(false));
    }, []);

    const handleDeleteSubmission = (id: string) =>
        setHistory(h => h.filter(s => s.id !== id));

    const first = history.length > 0
        ? new Date(history[history.length - 1].submittedAt).toLocaleDateString("en-GB", { day: "2-digit", month: "short", year: "2-digit" }).toUpperCase()
        : "—";
    const last = history.length > 0
        ? new Date(history[0].submittedAt).toLocaleDateString("en-GB", { day: "2-digit", month: "short", year: "2-digit" }).toUpperCase()
        : "—";
    const cefrSet   = [...new Set(history.map(h => h.predictedCefr))];
    const cefrRange = cefrSet.length > 1 ? `${cefrSet[cefrSet.length - 1]} - ${cefrSet[0]}` : (cefrSet[0] ?? "—");

    const TABS: { id: Tab; label: string; count: number; loading: boolean }[] = [
        { id: "writing", label: t("history.writingSessions"), count: history.length,       loading },
        { id: "coach",   label: t("history.coachChats"),      count: coachSessions.length, loading: coachLoading },
        { id: "game",    label: t("history.vocabRounds"),      count: gameHistory.length,   loading: gameLoading },
    ];

    return (
        <div className="page page-enter">
            <Masthead
                num="07"
                title={t("history.title")}
                sub={t("history.sessionArc", { n: history.length, s: history.length !== 1 ? "s" : "", range: cefrRange })}
                meta={[
                    { label: "FIRST", value: loading ? "…" : first },
                    { label: "LAST",  value: loading ? "…" : last  },
                    { label: "RANGE", value: loading ? "…" : cefrRange },
                ]}
            />

            <div className="history-tabs">
                {TABS.map(t => (
                    <button
                        key={t.id}
                        className={`history-tab${tab === t.id ? " history-tab--active" : ""}`}
                        onClick={() => setTab(t.id)}
                    >
                        {t.label}{" "}
                        <span className="history-tab-count">
                            {t.loading ? "" : t.count}
                        </span>
                    </button>
                ))}
            </div>

            {tab === "writing" && (
                <WritingHistoryList
                    history={history}
                    loading={loading}
                    onDelete={handleDeleteSubmission}
                />
            )}
            {tab === "coach" && (
                <CoachHistoryList sessions={coachSessions} loading={coachLoading} />
            )}
            {tab === "game" && (
                <GameHistoryList sessions={gameHistory} loading={gameLoading} />
            )}
        </div>
    );
};

export default HistoryPage;
