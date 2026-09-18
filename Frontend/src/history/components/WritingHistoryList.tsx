import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import type { SubmissionSummaryProps, SubmissionResponseProps } from "../../write/props";
import { useConfirm } from "../../core/ConfirmModal";
import { CEFRStamp } from "../../core/SharedComponents";
import { CEFR_NAME } from "../../core/cefr";
import toast from "react-hot-toast";
import useWriteApi from "../../write/useWriteApi";

const PAGE_SIZE = 10;

const CEFR_TAG: Record<string, string> = {
    A1: "cefr-a1", A2: "cefr-a2",
    B1: "cefr-b1", B2: "cefr-b2", C1: "cefr-c1",
};

interface WritingHistoryListProps {
    history: SubmissionSummaryProps[];
    loading: boolean;
    onDelete: (id: string) => void;
}

const WritingHistoryList = ({ history, loading, onDelete }: WritingHistoryListProps) => {
    const { getById, deleteSubmission } = useWriteApi();
    const confirm  = useConfirm();
    const navigate = useNavigate();
    const { t } = useTranslation();

    const [selected, setSelected] = useState<string | null>(null);
    const [detail,   setDetail]   = useState<SubmissionResponseProps | null>(null);
    const [deleting, setDeleting] = useState<string | null>(null);
    const [search,   setSearch]   = useState("");
    const [page,     setPage]     = useState(0);

    const filtered = search.trim()
        ? history.filter(s => {
            const q = search.toLowerCase();
            const dateStr = new Date(s.submittedAt).toLocaleDateString("en-GB", {
                day: "numeric", month: "short", year: "numeric",
            }).toLowerCase();
            return (
                dateStr.includes(q) ||
                s.predictedCefr.toLowerCase().includes(q) ||
                (s.textContent && s.textContent.toLowerCase().includes(q))
            );
        })
        : history;

    const totalPages  = Math.max(1, Math.ceil(filtered.length / PAGE_SIZE));
    const currentPage = Math.min(page, totalPages - 1);
    const pageItems   = filtered.slice(currentPage * PAGE_SIZE, (currentPage + 1) * PAGE_SIZE);

    const toggleDetail = async (id: string) => {
        if (selected === id) { setSelected(null); setDetail(null); return; }
        setSelected(id);
        const data = await getById(id);
        setDetail(data);
    };

    const handleDelete = async (e: React.MouseEvent, id: string, date: string) => {
        e.stopPropagation();
        const ok = await confirm({
            title: t("history.deleteTitle"),
            message: t("history.deleteMsg", { date }),
            confirmLabel: t("common.delete"),
            danger: true,
        });
        if (!ok) return;
        setDeleting(id);
        try {
            await deleteSubmission(id);
            onDelete(id);
            if (selected === id) { setSelected(null); setDetail(null); }
            toast.success(t("history.deleted"));
        } catch {
            toast.error(t("history.deleteError"));
        } finally {
            setDeleting(null);
        }
    };

    if (loading) return (
        <div className="history-list">
            {[1,2,3,4,5].map(i => <div key={i} className="history-skeleton-row" />)}
        </div>
    );

    if (history.length === 0) return (
        <div className="history-empty">
            <div className="history-empty-num">{t("history.emptyArchive")}</div>
            <div className="history-empty-title">{t("history.noSubmissions")}</div>
            <div className="history-empty-sub">{t("history.noSubmissionsSub")}</div>
        </div>
    );

    return (
        <>
            <div className="history-search-wrap">
                <input
                    className="history-search"
                    type="text"
                    placeholder={t("history.searchPlaceholder")}
                    value={search}
                    onChange={e => { setSearch(e.target.value); setPage(0); }}
                />
                {search
                    ? <button className="history-search-clear" onClick={() => setSearch("")}>✕</button>
                    : <span className="history-search-filter">{t("history.filter")}</span>
                }
            </div>

            <div className="history-list">
                <div className="history-table-head">
                    <span>{t("history.colNum")}</span>
                    <span>{t("history.colLevel")}</span>
                    <span>{t("history.colPreview")}</span>
                    <span>{t("history.colDate")}</span>
                    <span>{t("history.colWords")}</span>
                </div>

                {pageItems.length === 0 ? (
                    <div className="history-empty" style={{ borderTop: "none" }}>
                        <div className="history-empty-title">{t("history.noResults")}</div>
                        <div className="history-empty-sub">{t("history.noResultsMsg", { search })}</div>
                    </div>
                ) : pageItems.map((s, idx) => {
                    const rowNum  = String(currentPage * PAGE_SIZE + idx + 1).padStart(3, "0");
                    const dateStr = new Date(s.submittedAt).toLocaleDateString("en-GB", {
                        day: "numeric", month: "short", year: "2-digit",
                    });
                    const snippet = s.textContent
                        ? s.textContent.slice(0, 56) + (s.textContent.length > 56 ? "…" : "")
                        : "—";
                    const isOpen = selected === s.id;

                    return (
                        <div key={s.id} className={`history-item${isOpen ? " history-item--active" : ""}`}>
                            <button className="history-row" onClick={() => toggleDetail(s.id)}>
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
                                <span className="history-row-words">{s.tokenCount}w</span>
                                <span className="history-row-toggle">{isOpen ? "−" : "+"}</span>
                            </button>

                            {isOpen && detail && (
                                <div className="history-detail">
                                    <div className="history-detail-grid">
                                        <div className="history-detail-left">
                                            <div className="history-detail-section-hd">
                                                {t("history.textSession", { num: rowNum })}
                                                <button
                                                    className="history-delete-btn"
                                                    onClick={e => handleDelete(e, s.id, dateStr)}
                                                    disabled={deleting === s.id}
                                                >
                                                    {deleting === s.id ? t("history.deleting") : t("history.deleteBtn")}
                                                </button>
                                            </div>
                                            <div className="history-text-content">
                                                {detail.textContent || t("history.textUnavailable")}
                                            </div>

                                            {detail.errors?.length > 0 && (
                                                <div style={{ marginTop: 16 }}>
                                                    <div className="history-detail-section-hd">
                                                        {t("history.grammarIssues", { n: detail.errors.length })}
                                                    </div>
                                                    <div className="history-errors-table">
                                                        {detail.errors.slice(0, 5).map((e, i) => (
                                                            <div key={i} className="history-error-row">
                                                                <span className="history-error-cat">{e.category}</span>
                                                                <span className="history-error-msg">{e.message}</span>
                                                                {e.suggestions?.[0] && (
                                                                    <span className="history-error-fix">→ {e.suggestions[0]}</span>
                                                                )}
                                                            </div>
                                                        ))}
                                                    </div>
                                                </div>
                                            )}
                                        </div>

                                        <div className="history-detail-right">
                                            <CEFRStamp
                                                level={s.predictedCefr}
                                                name={CEFR_NAME[s.predictedCefr] ?? s.predictedCefr}
                                                size="sm"
                                            />

                                            {detail.aiFeedback && (
                                                <div style={{ marginTop: 16 }}>
                                                    <div className="history-detail-section-hd">{t("history.coachFeedback")}</div>
                                                    <div className="history-feedback-block">{detail.aiFeedback}</div>
                                                </div>
                                            )}

                                            <div className="history-detail-actions">
                                                <button
                                                    className="history-discuss-btn"
                                                    onClick={() => navigate("/coach", {
                                                        state: {
                                                            text: detail.textContent,
                                                            cefr: s.predictedCefr,
                                                            num:  rowNum,
                                                        },
                                                    })}
                                                >
                                                    {t("history.discuss")}
                                                </button>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            )}
                        </div>
                    );
                })}
            </div>

            {totalPages > 1 && (
                <div className="history-pagination">
                    <button
                        className="history-page-btn"
                        disabled={currentPage === 0}
                        onClick={() => setPage(p => p - 1)}
                    >
                        {t("history.prev")}
                    </button>
                    <span className="history-page-info">{currentPage + 1} / {totalPages}</span>
                    <button
                        className="history-page-btn"
                        disabled={currentPage >= totalPages - 1}
                        onClick={() => setPage(p => p + 1)}
                    >
                        {t("history.next")}
                    </button>
                </div>
            )}
        </>
    );
};

export default WritingHistoryList;
