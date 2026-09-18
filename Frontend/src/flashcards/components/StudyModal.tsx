import { useEffect, useRef, useState } from "react";
import { createPortal } from "react-dom";
import { useTranslation } from "react-i18next";
import "../flashcards.css";

type Mode = "choose" | "flip" | "write";
interface Card { front: string; back: string; }
interface Props { cards: Card[]; onClose: () => void; onComplete?: () => void; }

const StudyModal = ({ cards, onClose, onComplete }: Props) => {
    const { t } = useTranslation();
    const [mode, setMode]       = useState<Mode>("choose");
    const [index, setIndex]     = useState(0);
    const [revealed, setReveal] = useState(false);
    const [answer, setAnswer]   = useState("");
    const [checked, setChecked] = useState(false);
    const [correct, setCorrect] = useState<boolean | null>(null);
    const [done, setDone]       = useState(false);
    const [score, setScore]     = useState(0);

    const inputRef = useRef<HTMLInputElement>(null);
    const [shuffled] = useState(() => [...cards].sort(() => Math.random() - 0.5));
    const current    = shuffled[index];
    const total      = shuffled.length;
    const progress   = ((index + 1) / total) * 100;

    const advance = (wasCorrect?: boolean) => {
        if (wasCorrect !== undefined && wasCorrect) setScore(s => s + 1);
        setAnswer(""); setChecked(false); setCorrect(null); setReveal(false);
        if (index + 1 >= total) { setDone(true); return; }
        setIndex(i => i + 1);
    };

    const check = () => {
        const ok = answer.trim().toLowerCase() === current.back.trim().toLowerCase();
        setCorrect(ok); setChecked(true);
    };

    const restart = () => {
        setIndex(0); setDone(false); setScore(0);
        setAnswer(""); setChecked(false); setCorrect(null); setReveal(false); setMode("choose");
    };

    useEffect(() => {
        if (mode === "write" && !checked && !done) {
            setTimeout(() => inputRef.current?.focus(), 0);
        }
    }, [index, mode, checked, done]);

    useEffect(() => {
        const handler = (e: KeyboardEvent) => {
            if (e.key !== "Enter") return;
            if (mode === "flip") {
                if (!revealed) setReveal(true);
                else advance();
            }
            if (mode === "write") {
                if (!checked && answer.trim()) check();
                else if (checked) advance(correct ?? false);
            }
        };
        window.addEventListener("keydown", handler);
        return () => window.removeEventListener("keydown", handler);
    }, [mode, revealed, checked, answer, correct, index]);

    return createPortal(
        <div className="fc-study-overlay">
            <div className="fc-study-modal">

                <div className="fc-study-header">
                    {mode !== "choose" && !done ? (
                        <div className="fc-progress-bar" style={{ flex: 1 }}>
                            <span>{index + 1} / {total}</span>
                            <div className="fc-bar-track"><div className="fc-bar-fill" style={{ width: `${progress}%` }} /></div>
                        </div>
                    ) : <div style={{ flex: 1 }} />}
                    <button className="fc-study-close" onClick={onClose}>✕</button>
                </div>

                {mode === "choose" && (
                    <div className="fc-study-choose">
                        <h2>{t("flashcards.studySelectMode")}</h2>
                        <p style={{ color: "var(--text-muted)", fontSize: "0.88rem", marginBottom: "1.25rem" }}>
                            {t("flashcards.studyModeSub", { n: total, s: total !== 1 ? "s" : "" })}
                        </p>
                        <div className="fc-study-modes">
                            <button className="fc-study-mode-btn" onClick={() => setMode("flip")}>
                                <span className="mode-icon">🔄</span>
                                <strong>{t("flashcards.studyModeFlipTitle")}</strong>
                                <span>{t("flashcards.studyModeFlipSub")}</span>
                            </button>
                            <button className="fc-study-mode-btn" onClick={() => setMode("write")}>
                                <span className="mode-icon">✍️</span>
                                <strong>{t("flashcards.studyModeWriteTitle")}</strong>
                                <span>{t("flashcards.studyModeWriteSub")}</span>
                            </button>
                        </div>
                    </div>
                )}

                {done && mode !== "choose" && (
                    <div className="fc-done">
                        <p style={{ fontSize: "3rem" }}>🎉</p>
                        <h2>{t("flashcards.sessionComplete")}</h2>
                        {mode === "write" && <p className="fc-done-score">{t("flashcards.scoreLabel", { score, total })}</p>}
                        <div className="fc-done-btns">
                            <button className="fc-btn-primary" onClick={restart}>{t("flashcards.studyAgain")}</button>
                            <button className="fc-btn-secondary" onClick={() => { onComplete?.(); onClose(); }}>{t("flashcards.done")}</button>
                        </div>
                    </div>
                )}

                {mode === "flip" && !done && (
                    <>
                        <div className="fc-flip-card" onClick={() => setReveal(r => !r)}>
                            <div className="fc-flip-front">
                                <p className="fc-side-label">{t("flashcards.front")}</p>
                                <p className="fc-card-big-text">{current.front}</p>
                                <p className="fc-hint">{t("flashcards.tapReveal")}</p>
                            </div>
                            <div className={`fc-flip-answer ${revealed ? "visible" : ""}`}>
                                <p className="fc-side-label answer">{t("flashcards.answer")}</p>
                                <p className="fc-card-big-text">{current.back}</p>
                            </div>
                        </div>

                        <div className="fc-modal-actions">
                            <button
                                className="fc-btn-primary"
                                onClick={() => { if (!revealed) setReveal(true); else advance(); }}
                            >
                                {revealed ? t("flashcards.nextArrow") : t("flashcards.reveal")}
                            </button>
                        </div>
                    </>
                )}

                {mode === "write" && !done && (
                    <>
                        <div className="fc-write-card">
                            <p className="fc-side-label">{t("flashcards.translateThis")}</p>
                            <p className="fc-card-big-text">{current.front}</p>
                        </div>

                        {checked && (
                            <div className={`fc-feedback ${correct ? "correct" : "incorrect"}`}>
                                {correct
                                    ? t("flashcards.correct")
                                    : t("flashcards.incorrect", { answer: current.back })
                                }
                            </div>
                        )}

                        <div className="fc-write-row">
                            <input
                                ref={inputRef}
                                className="fc-input"
                                style={{ flex: 1 }}
                                placeholder={t("flashcards.yourAnswer")}
                                value={answer}
                                onChange={e => setAnswer(e.target.value)}
                                disabled={checked}
                                autoFocus
                            />
                        </div>

                        <div className="fc-modal-actions">
                            {!checked ? (
                                <button className="fc-btn-primary" onClick={check} disabled={!answer.trim()}>
                                    {t("flashcards.check")}
                                </button>
                            ) : (
                                <button className="fc-btn-primary" onClick={() => advance(correct ?? false)}>
                                    {t("flashcards.nextArrow")}
                                </button>
                            )}
                        </div>
                    </>
                )}
            </div>
        </div>,
        document.body
    );
};

export default StudyModal;
