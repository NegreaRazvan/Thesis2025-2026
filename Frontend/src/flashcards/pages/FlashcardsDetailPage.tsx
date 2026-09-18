import { useEffect, useRef, useState } from "react";
import { useParams, useSearchParams, useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { useConfirm } from "../../core/ConfirmModal";
import toast from "react-hot-toast";
import "../flashcards.css";
import useFlashcardSetApi from "../useFlashcardsApi";
import {FlashcardCard, FlashcardDetail} from "../props";
import StudyModal from "../components/StudyModal";

const FlashcardSetDetailPage = () => {
    const { id } = useParams<{ id: string }>();
    const [searchParams]       = useSearchParams();
    const navigate             = useNavigate();
    const confirm              = useConfirm();
    const { t }                = useTranslation();
    const { getById, addCard, updateCard, deleteCard, bulkAdd } = useFlashcardSetApi();

    const [set, setSet]             = useState<FlashcardDetail | null>(null);
    const [loading, setLoading]     = useState(true);
    const [editId, setEditId]       = useState<string | null>(null);
    const [front, setFront]         = useState("");
    const [back, setBack]           = useState("");
    const [saving, setSaving]       = useState(false);
    const [studyOpen, setStudyOpen] = useState(searchParams.get("study") === "1");
    const csvRef = useRef<HTMLInputElement>(null);

    useEffect(() => {
        if (!id) return;
        getById(id).then(setSet).finally(() => setLoading(false));
    }, [id]);

    const resetForm = () => { setFront(""); setBack(""); setEditId(null); };

    const handleSave = async () => {
        if (!front.trim() || !back.trim() || !id) return;
        setSaving(true);
        try {
            if (editId) {
                const updated = await updateCard(id, editId, front.trim(), back.trim());
                setSet(s => s ? { ...s, cards: s.cards.map(c => c.id === editId ? updated : c) } : s);
                toast.success(t("flashcards.cardUpdated"));
            } else {
                const card = await addCard(id, front.trim(), back.trim());
                setSet(s => s ? { ...s, cards: [...s.cards, card] } : s);
                toast.success(t("flashcards.cardAdded"));
            }
            resetForm();
        } catch { toast.error(t("flashcards.cardSaveError")); }
        finally { setSaving(false); }
    };

    const handleDeleteCard = async (cardId: string, cardFront: string) => {
        if (!id) return;
        const ok = await confirm({
            title: t("flashcards.deleteCardTitle"),
            message: t("flashcards.deleteCardMsg", { front: cardFront }),
            confirmLabel: t("common.delete"),
            danger: true,
        });
        if (!ok) return;
        try {
            await deleteCard(id, cardId);
            setSet(s => s ? { ...s, cards: s.cards.filter(c => c.id !== cardId) } : s);
            toast.success(t("flashcards.cardDeleted"));
        } catch { toast.error(t("flashcards.cardDeleteError")); }
    };

    const startEdit = (card: FlashcardCard) => {
        setEditId(card.id); setFront(card.front); setBack(card.back);
        document.getElementById("fc-card-form")?.scrollIntoView({ behavior: "smooth" });
    };

    const handleCsv = async (e: React.ChangeEvent<HTMLInputElement>) => {
        const file = e.target.files?.[0];
        if (!file || !id) return;
        const text  = await file.text();
        const lines = text.split(/\r?\n/).map(l => l.trim()).filter(Boolean);
        const cards: { front: string; back: string }[] = [];

        for (const line of lines) {
            const match =
                line.match(/^"([^"]*)"\s*,\s*"([^"]*)"$/) ??
                line.match(/^"([^"]*)"\s*,\s*(.+)$/)       ??
                line.match(/^([^,"]+)\s*,\s*"([^"]*)"$/)   ??
                line.match(/^([^,]+),(.+)$/);

            if (match) {
                const f = match[1].trim().replace(/^"|"$/g, "");
                const b = match[2].trim().replace(/^"|"$/g, "");
                if (f && b) cards.push({ front: f, back: b });
            }
        }

        if (!cards.length) { toast.error(t("flashcards.csvNoRows")); return; }

        try {
            const added = await bulkAdd(id, cards);
            setSet(s => s ? { ...s, cards: [...s.cards, ...added] } : s);
            toast.success(t("flashcards.imported", { n: added.length, s: added.length !== 1 ? "s" : "" }));
        } catch { toast.error(t("flashcards.importFailed")); }
        e.target.value = "";
    };

    if (loading) return <div className="page page-enter fc-detail-page"><div className="fc-empty"><div className="fc-empty-title">{t("common.loading")}</div></div></div>;
    if (!set)    return <div className="page page-enter fc-detail-page"><div className="fc-empty"><div className="fc-empty-title">{t("flashcards.setNotFound")}</div></div></div>;

    return (
        <div className="page page-enter fc-detail-page">
            {studyOpen && set.cards.length > 0 && (
                <StudyModal cards={set.cards} onClose={() => setStudyOpen(false)} />
            )}

            <div className="fc-detail-header">
                <div>
                    <button className="fc-back" onClick={() => navigate("/flashcards")}>{t("flashcards.detailBack")}</button>
                    <h2 className="fc-detail-title">{set.name}</h2>
                    {set.description && <p className="fc-detail-desc">{set.description}</p>}
                    <p className="fc-detail-meta">
                        {set.cards.length} {t("flashcards.cards").toLowerCase()}
                    </p>
                </div>
                <div className="fc-detail-actions">
                    <button className="fc-btn-secondary" onClick={() => csvRef.current?.click()}>{t("flashcards.importCsv")}</button>
                    <input ref={csvRef} type="file" accept=".csv,.txt" style={{ display: "none" }} onChange={handleCsv} />
                    {set.cards.length > 0 && (
                        <button className="fc-btn-primary" onClick={() => setStudyOpen(true)}>{t("flashcards.studyBtn")}</button>
                    )}
                </div>
            </div>

            <div className="fc-csv-hint">
                <strong>CSV:</strong> {t("flashcards.csvHint")}
            </div>

            <div className="fc-create-form" id="fc-card-form">
                <p className="fc-form-label">
                    {editId ? t("flashcards.editCard") : t("flashcards.addCard")}
                </p>
                <div className="fc-form-inputs">
                    <input className="fc-input" placeholder={t("flashcards.frontPlaceholder")} value={front} onChange={e => setFront(e.target.value)} />
                    <input className="fc-input" placeholder={t("flashcards.backPlaceholder")} value={back} onChange={e => setBack(e.target.value)}
                           onKeyDown={e => e.key === "Enter" && front.trim() && back.trim() && handleSave()} />
                </div>
                <div className="fc-form-row">
                    <button className="fc-btn-primary" onClick={handleSave} disabled={saving || !front.trim() || !back.trim()}>
                        {saving ? t("flashcards.saving") : editId ? t("flashcards.updateBtn") : t("flashcards.addCardBtn")}
                    </button>
                    {editId && <button className="fc-btn-secondary" onClick={resetForm}>{t("common.cancel")}</button>}
                </div>
            </div>

            {set.cards.length === 0 ? (
                <div className="fc-empty">
                    <p className="fc-empty-icon">🃏</p>
                    <p>{t("flashcards.noCardsYet")}</p>
                </div>
            ) : (
                <div className="fc-card-list">
                    {set.cards.map((card) => (
                        <div key={card.id} className="fc-card-row">
                            <div className="fc-card-row-content">
                                <span className="fc-card-front">{card.front}</span>
                                <span className="fc-card-arrow">→</span>
                                <span className="fc-card-back">{card.back}</span>
                            </div>
                            <div className="fc-card-actions">
                                <button className="fc-card-action" onClick={() => startEdit(card)} title={t("common.edit")}>✏️</button>
                                <button className="fc-card-action" onClick={() => handleDeleteCard(card.id, card.front)} title={t("common.delete")}>🗑️</button>
                            </div>
                        </div>
                    ))}
                </div>
            )}
        </div>
    );
};

export default FlashcardSetDetailPage;
