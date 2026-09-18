import { useEffect, useRef, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import useCoachApi from "../useCoachApi";
import { useAuthContext } from "../../auth/context/AuthContext";
import toast from "react-hot-toast";
import "../coach.css";
import type { CoachMessage } from "../props";
import { CEFR_LEVELS } from "../../core/cefr";

const CoachPage = () => {
    const { chat, saveSession } = useCoachApi();
    const { user } = useAuthContext();
    const location = useLocation();
    const navigate = useNavigate();
    const { t } = useTranslation();

    const QUICK_PROMPTS = [
        t("coach.qp_explainErrors"),
        t("coach.qp_reviewErrors"),
        t("coach.qp_b1Prompt"),
        t("coach.qp_quizArticles"),
    ];

    const locState   = location.state as { text?: string; cefr?: string; num?: string } | null;
    const initText   = locState?.text  ?? "";
    const initCefr   = locState?.cefr  ?? "B1";
    const initNum    = locState?.num   ?? null;

    const [germanText,     setGermanText]     = useState(initText);
    const [cefr,           setCefr]           = useState(initCefr);
    const [history,        setHistory]        = useState<CoachMessage[]>([]);
    const [input,          setInput]          = useState("");
    const [loading,        setLoading]        = useState(false);
    const [sessionStarted, setSessionStarted] = useState(false);
    const [sessionTime,    setSessionTime]    = useState("");

    const bottomRef = useRef<HTMLDivElement>(null);
    const inputRef  = useRef<HTMLTextAreaElement>(null);

    const getNow = () => new Date().toLocaleTimeString("en-GB", { hour: "2-digit", minute: "2-digit" });
    const userInitial = user?.username?.[0]?.toUpperCase() ?? "U";

    useEffect(() => {
        bottomRef.current?.scrollIntoView({ behavior: "smooth" });
    }, [history, loading]);

    useEffect(() => {
        if (initText && !sessionStarted) startSession();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);

    const startSession = async () => {
        if (!germanText.trim()) return;
        setLoading(true);
        setHistory([]);
        setSessionStarted(true);
        setSessionTime(getNow());
        try {
            const res = await chat({
                germanText:    germanText.trim(),
                predictedCefr: cefr,
                history:       [],
                userMessage:   "",
            });
            setHistory([{ role: "assistant", content: res.reply, ts: getNow() }]);
        } catch (e) {
            toast.error(e instanceof Error ? e.message : t("coach.startError"));
        } finally {
            setLoading(false);
        }
    };

    const sendMessage = async (text?: string) => {
        const msg = (text ?? input).trim();
        if (!msg || loading) return;
        const userMsg: CoachMessage = { role: "user", content: msg, ts: getNow() };
        const newHistory = [...history, userMsg];
        setHistory(newHistory);
        setInput("");
        setLoading(true);
        try {
            const res = await chat({
                germanText:    germanText.trim(),
                predictedCefr: cefr,
                history:       newHistory,
                userMessage:   msg,
            });
            setHistory(h => [...h, { role: "assistant", content: res.reply, ts: getNow() }]);
        } catch (e) {
            toast.error(e instanceof Error ? e.message : t("coach.replyError"));
        } finally {
            setLoading(false);
            setTimeout(() => inputRef.current?.focus(), 50);
        }
    };

    const handleKey = (e: React.KeyboardEvent) => {
        if (e.key === "Enter" && !e.shiftKey) { e.preventDefault(); sendMessage(); }
    };

    const resetSession = async () => {
        if (sessionStarted && history.length > 0) {
            try {
                await saveSession({ germanText: germanText.trim(), predictedCefr: cefr, messages: history });
            } catch {}
        }
        setHistory([]);
        setSessionStarted(false);
        setInput("");
        setSessionTime("");
        navigate("/coach", { replace: true, state: null });
    };

    const userMessages = history.filter(m => m.role === "user").length;

    const firstAssistant = history.find(m => m.role === "assistant")?.content ?? "";
    const todaysFocus    = firstAssistant
        ? firstAssistant.split(/[.?!]/)[0]?.slice(0, 40) + "…"
        : germanText
            ? germanText.trim().split(" ").slice(0, 4).join(" ") + "…"
            : t("coach.generalPractice");

    return (
        <div className="coach-page">
            <div className="coach-masthead-bar">
                <div className="coach-masthead-num">03</div>
                <div>
                    <div className="coach-masthead-title">{t("coach.title")}</div>
                    <div className="coach-masthead-sub">{t("coach.sub")}</div>
                </div>
                <div className="coach-masthead-meta">
                    <div><span>MODEL</span> · {t("coach.modelLabel")}</div>
                    <div><span>CONTEXT</span> · {t("coach.contextLabel")}</div>
                    <div><span>LEVEL</span> · {cefr}</div>
                </div>
            </div>

            <div className="coach-body">

                <div className="coach-sidebar">
                    <div className="coach-sidebar-section">
                        <span className="coach-sidebar-label">{t("coach.sessionContext")}</span>

                        <div className="coach-ctx-row">
                            <span className="coach-ctx-key">{t("coach.coachLabel")}</span>
                            <span className="coach-ctx-val">Lena</span>
                        </div>
                        <div className="coach-ctx-row">
                            <span className="coach-ctx-key">{t("coach.learner")}</span>
                            <span className="coach-ctx-val">{user?.username ?? "—"} · {cefr}</span>
                        </div>
                        {sessionStarted && (
                            <>
                                <div className="coach-ctx-row">
                                    <span className="coach-ctx-key">{t("coach.todaysFocus")}</span>
                                    <span className="coach-ctx-val coach-ctx-val--focus">{todaysFocus}</span>
                                </div>
                                {initNum && (
                                    <div className="coach-ctx-row">
                                        <span className="coach-ctx-key">{t("coach.fromSession")}</span>
                                        <span className="coach-ctx-val">№ {initNum}</span>
                                    </div>
                                )}
                            </>
                        )}
                    </div>

                    {!sessionStarted && (
                        <div className="coach-sidebar-section">
                            <span className="coach-sidebar-label">{t("coach.cefrLevel")}</span>
                            <div className="coach-level-pills">
                                {CEFR_LEVELS.map(lvl => (
                                    <button
                                        key={lvl}
                                        className={`coach-level-pill${cefr === lvl ? " coach-level-pill--active" : ""}`}
                                        onClick={() => setCefr(lvl)}
                                    >
                                        {lvl}
                                    </button>
                                ))}
                            </div>
                        </div>
                    )}

                    {sessionStarted && (
                        <div className="coach-sidebar-section">
                            <span className="coach-sidebar-label">{t("coach.quickPrompts")}</span>
                            <div className="coach-quick-prompts">
                                {QUICK_PROMPTS.map(p => (
                                    <button
                                        key={p}
                                        className="coach-quick-prompt"
                                        onClick={() => sendMessage(p)}
                                        disabled={loading}
                                    >
                                        {p}
                                    </button>
                                ))}
                            </div>
                        </div>
                    )}

                    {sessionStarted && (
                        <div className="coach-sidebar-section coach-sidebar-section--stats">
                            <div className="coach-ctx-row">
                                <span className="coach-ctx-key">{t("coach.messages")}</span>
                                <span className="coach-ctx-val">{history.length}</span>
                            </div>
                            <div className="coach-ctx-row">
                                <span className="coach-ctx-key">{t("coach.yourReplies")}</span>
                                <span className="coach-ctx-val">{userMessages}</span>
                            </div>
                            <button className="coach-reset-btn" onClick={resetSession}>
                                {t("coach.saveAndEnd")}
                            </button>
                        </div>
                    )}
                </div>

                <div className="coach-main">

                    {!sessionStarted && (
                        <div className="coach-setup">
                            <div className="coach-setup-intro">
                                <div className="coach-setup-intro-title">{t("coach.setupTitle")}</div>
                                <div className="coach-setup-intro-sub">{t("coach.setupSub")}</div>
                            </div>

                            <div>
                                <span className="coach-setup-label">{t("coach.yourText")}</span>
                                <textarea
                                    className="coach-setup-textarea"
                                    placeholder={t("coach.textPlaceholder")}
                                    value={germanText}
                                    onChange={e => setGermanText(e.target.value)}
                                    rows={8}
                                />
                            </div>

                            <div className="coach-setup-controls">
                                <button
                                    className="btn btn--primary"
                                    onClick={startSession}
                                    disabled={!germanText.trim() || loading}
                                >
                                    {loading ? t("coach.starting") : t("coach.startSession")}
                                </button>
                            </div>
                        </div>
                    )}

                    {sessionStarted && (
                        <div className="coach-chat-container">
                            <div className="coach-chat-header">
                                <div className="coach-chat-hd-left">
                                    <div className="coach-avatar-sq">L</div>
                                    <div>
                                        <div className="coach-chat-hd-name">Lena</div>
                                        <div className="coach-chat-hd-sub">{t("coach.onlineSub")}</div>
                                    </div>
                                </div>
                                <div className="coach-chat-hd-session">
                                    {t("coach.session")} · {sessionTime}
                                </div>
                            </div>

                            <div className="coach-thread">
                                {history.map((msg, i) => (
                                    <div
                                        key={i}
                                        className={`coach-bubble coach-bubble--${msg.role}`}
                                    >
                                        {msg.role === "assistant" ? (
                                            <>
                                                <div className="coach-avatar-sq coach-avatar-sq--lena">L</div>
                                                <div>
                                                    <div className="coach-bubble-text">{msg.content}</div>
                                                    <div className="coach-bubble-meta">Lena · {msg.ts ?? ""}</div>
                                                </div>
                                            </>
                                        ) : (
                                            <>
                                                <div>
                                                    <div className="coach-bubble-text">{msg.content}</div>
                                                    <div className="coach-bubble-meta coach-bubble-meta--user">{t("coach.you")} · {msg.ts ?? ""}</div>
                                                </div>
                                                <div className="coach-avatar-sq coach-avatar-sq--user">{userInitial}</div>
                                            </>
                                        )}
                                    </div>
                                ))}

                                {loading && (
                                    <div className="coach-bubble coach-bubble--assistant">
                                        <div className="coach-avatar-sq coach-avatar-sq--lena">L</div>
                                        <div className="coach-bubble-typing">
                                            <span /><span /><span />
                                        </div>
                                    </div>
                                )}

                                <div ref={bottomRef} />
                            </div>

                            <div className="coach-dock">
                                <textarea
                                    ref={inputRef}
                                    className="coach-dock-input"
                                    placeholder={t("coach.inputPlaceholder")}
                                    value={input}
                                    onChange={e => setInput(e.target.value)}
                                    onKeyDown={handleKey}
                                    rows={2}
                                    disabled={loading}
                                />
                                <button
                                    className="coach-dock-send"
                                    onClick={() => sendMessage()}
                                    disabled={!input.trim() || loading}
                                >
                                    {t("coach.send")}
                                </button>
                            </div>
                        </div>
                    )}
                </div>
            </div>
        </div>
    );
};

export default CoachPage;
