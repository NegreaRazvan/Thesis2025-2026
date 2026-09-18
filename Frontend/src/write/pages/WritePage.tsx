import { useState, useCallback } from "react";
import { useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import useWriteApi from "../useWriteApi";
import useTranscriptionApi from "../useTranscriptionApi";
import useRecorder from "../hooks/useRecorder";
import WriteEmptyState from "../components/WriteEmptyState";
import WriteResultView from "../components/WriteResultView";
import type { SubmissionResponseProps } from "../props";
import { Masthead, SectionHead } from "../../core/SharedComponents";
import { SAMPLE_TEXT } from "../constants";
import toast from "react-hot-toast";
import "../write.css";

const countWords = (t: string) =>
    t.trim().split(/\s+/).filter(w => /\p{L}/u.test(w)).length;

const countSentences = (t: string) =>
    t.trim() ? t.trim().split(/[.!?]+/).filter(s => s.trim().length > 0).length : 0;

const MIN_WORDS = 50;

const WritePage = () => {
    const { submitText }  = useWriteApi();
    const { transcribe }  = useTranscriptionApi();
    const navigate        = useNavigate();
    const { t }           = useTranslation();

    type Phase = "compose" | "analyzing" | "done";
    const [phase, setPhase]               = useState<Phase>("compose");
    const [text, setText]                 = useState("");
    const [result, setResult]             = useState<SubmissionResponseProps | null>(null);
    const [transcribing, setTranscribing] = useState(false);

    const handleBlob = useCallback(async (blob: Blob, mime: string) => {
        setTranscribing(true);
        try {
            const res = await transcribe(blob, mime);
            setText(prev => prev ? prev + " " + res.text : res.text);
            toast.success(t("write.transcribedAudio", { duration: res.durationSeconds.toFixed(1) }));
        } catch (e) {
            toast.error(e instanceof Error ? e.message : t("write.transcriptionFailed"));
        } finally {
            setTranscribing(false);
        }
    }, [transcribe, t]);

    const { recording, start: startRec, stop: stopRec } = useRecorder(handleBlob);

    const handleSubmit = useCallback(async () => {
        if (!text.trim()) return;
        setPhase("analyzing");
        setResult(null);
        try {
            const data = await submitText(text);
            setResult(data);
            setPhase("done");
            toast.success(t("write.levelDetected", { level: data.predictedCefr }));
        } catch (e) {
            setPhase("compose");
            toast.error(e instanceof Error ? e.message : t("write.analysisFailed"));
        }
    }, [text, submitText, t]);

    const handleReset = useCallback(() => {
        setResult(null);
        setPhase("compose");
    }, []);

    const wordCount  = countWords(text);
    const sentCount  = countSentences(text);
    const wordsLeft  = Math.max(0, MIN_WORDS - wordCount);
    const today      = new Date().toLocaleDateString("en-GB", { day: "numeric", month: "short" });

    const PIPELINE_STEPS = [
        { label: t("write.step_tokenize"),   sub: t("write.step_tokenize_sub") },
        { label: t("write.step_grammar"),    sub: t("write.step_grammar_sub") },
        { label: t("write.step_complexity"), sub: t("write.step_complexity_sub") },
        { label: t("write.step_predict"),    sub: t("write.step_predict_sub") },
    ];

    if (phase === "done" && result) {
        return (
            <div className="page page-enter">
                <Masthead
                    num="02"
                    title={<>{result.predictedCefr} · <em style={{ fontStyle: "normal", color: "var(--red)" }}>{t("nav.write")}</em></>}
                    sub={t("write.resultSub")}
                    meta={[
                        { label: "LEVEL",  value: result.predictedCefr },
                        { label: "ERRORS", value: String(result.errors.length) },
                        { label: "DATE",   value: today },
                    ]}
                />
                <WriteResultView
                    result={result}
                    text={text}
                    onStartCoaching={() => navigate("/coach", { state: { text, cefr: result?.predictedCefr ?? "B1" } })}
                    onReset={handleReset}
                />
            </div>
        );
    }

    if (phase === "analyzing") {
        return (
            <div className="page page-enter">
                <Masthead
                    num="—"
                    title={t("write.analysing")}
                    sub={t("write.pipelineSub")}
                    meta={[
                        { label: "WORDS",    value: String(wordCount) },
                        { label: "PIPELINE", value: "4 steps" },
                    ]}
                />
                <SectionHead num="01" title={t("write.pipelineLabel")} right={<span className="label">{t("write.pipelineMeta")}</span>} />
                <div className="analyze-pipeline" style={{ marginTop: 8 }}>
                    {PIPELINE_STEPS.map((step, i) => (
                        <div key={i} className="analyze-step">
                            <span className="analyze-step-num">0{i + 1}</span>
                            <div>
                                <div className="analyze-step-label">{step.label}</div>
                                <div className="analyze-step-sub">{step.sub}</div>
                            </div>
                            <div className="analyze-step-bar-track">
                                <div className="analyze-step-bar-fill" />
                            </div>
                        </div>
                    ))}
                </div>
                <div className="analyze-wait">{t("write.awaitingResult")}</div>
            </div>
        );
    }

    return (
        <div className="page page-enter">
            <Masthead
                num="02"
                title={<>{t("write.title")}</>}
                sub={t("write.sub")}
                meta={[
                    { label: "MODEL",    value: "LinguaForge v2.1" },
                    { label: "PIPELINE", value: "spaCy + LanguageTool" },
                    { label: "TARGET",   value: "B1 → B2" },
                ]}
            />

            <div className="write-input-block" style={{ marginBottom: 40 }}>
                <div className="write-editor-topbar">
                    <span className="write-editor-label">{t("write.editorLabel")}</span>
                    <div className="write-editor-topbar-actions">
                        <button className="btn btn--sm" onClick={() => setText(SAMPLE_TEXT)}>
                            {t("write.useSample")}
                        </button>
                        <button
                            className={`write-mic-btn${recording ? " write-mic-btn--recording" : ""}`}
                            onClick={recording ? stopRec : startRec}
                            disabled={transcribing}
                            title={recording ? t("write.stopRecording") : t("write.dictateTitle")}
                        >
                            {transcribing ? t("write.wait") : recording ? t("write.stopRecord") : t("write.dictate")}
                        </button>
                    </div>
                </div>

                <textarea
                    className="write-textarea"
                    value={text}
                    onChange={e => setText(e.target.value)}
                    placeholder={t("write.placeholder")}
                    rows={9}
                    disabled={transcribing}
                />

                <div className="write-toolbar">
                    <span className="write-char-count">
                        {wordCount} {t("write.words")} · {text.length} {t("write.chars")} · {sentCount} {t("write.sent")}
                        {wordsLeft > 0 && (
                            <> · <span style={{ color: "var(--text-mute)" }}>{t("write.moreWords", { n: wordsLeft })}</span></>
                        )}
                    </span>
                    <button
                        className="btn btn--primary"
                        onClick={handleSubmit}
                        disabled={wordCount < 5}
                    >
                        {t("write.analyze")}
                    </button>
                </div>
            </div>

            <WriteEmptyState onSelectStarter={setText} />
        </div>
    );
};

export default WritePage;
