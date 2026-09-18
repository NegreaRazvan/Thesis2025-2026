import { useEffect, useState, useCallback } from "react";
import { useTranslation } from "react-i18next";
import useProgressApi from "../useProgressApi";
import type { ProgressPointProps, ErrorPatternProps, GamificationProps } from "../props";
import { Masthead, SectionHead, LineChart, BarChart, Glyph, CEFRLadder } from "../../core/SharedComponents";
import { CEFR_NAME, CEFR_CHART_IDX } from "../../core/cefr";
import toast from "react-hot-toast";
import "../progress.css";

type GlyphKind = "square" | "circle" | "triangle" | "diamond";

const BADGE_CONFIG: Record<string, { glyph: GlyphKind; color: string }> = {
    first_submission: { glyph: "square",   color: "var(--gold)"  },
    "10_submissions": { glyph: "circle",   color: "var(--blue)"  },
    "25_submissions": { glyph: "diamond",  color: "var(--gold)"  },
    streak_3:         { glyph: "triangle", color: "var(--red)"   },
    streak_7:         { glyph: "square",   color: "var(--red)"   },
    streak_30:        { glyph: "diamond",  color: "var(--blue)"  },
    reached_a2:       { glyph: "circle",   color: "var(--green)" },
    reached_b1:       { glyph: "square",   color: "var(--green)" },
    reached_b2:       { glyph: "triangle", color: "var(--blue)"  },
    reached_c1:       { glyph: "diamond",  color: "var(--red)"   },
};

function buildWeeklyBars(timeline: ProgressPointProps[]): { w: number; val: number }[] {
    if (!timeline.length) return [];
    const byWeek: Record<number, number> = {};
    const origin = new Date(timeline[0].date).getTime();
    for (const p of timeline) {
        const weekIdx = Math.floor((new Date(p.date).getTime() - origin) / (7 * 86400000));
        byWeek[weekIdx] = (byWeek[weekIdx] ?? 0) + 1;
    }
    const maxWeek = Math.max(...Object.keys(byWeek).map(Number));
    return Array.from({ length: maxWeek + 1 }, (_, i) => ({ w: i + 1, val: byWeek[i] ?? 0 }));
}

const ProgressPage = () => {
    const { getTimeline, getErrorPatterns, getGamification, downloadReport } = useProgressApi();
    const { t } = useTranslation();

    const [timeline,     setTimeline]     = useState<ProgressPointProps[]>([]);
    const [patterns,     setPatterns]     = useState<ErrorPatternProps[]>([]);
    const [gamification, setGamification] = useState<GamificationProps | null>(null);
    const [loading,      setLoading]      = useState(true);
    const [downloading,  setDownloading]  = useState(false);

    useEffect(() => {
        Promise.all([getTimeline(), getErrorPatterns(), getGamification()])
            .then(([tl, p, g]) => { setTimeline(tl); setPatterns(p); setGamification(g); })
            .catch(() => toast.error(t("progress.loadError")))
            .finally(() => setLoading(false));
    }, []);

    const handleDownload = useCallback(async () => {
        setDownloading(true);
        try { await downloadReport(); }
        catch { toast.error(t("progress.downloadError")); }
        finally { setDownloading(false); }
    }, [downloadReport]);

    const today = new Date().toLocaleDateString("en-GB", { day: "numeric", month: "short" });

    if (loading) return (
        <div className="page page-enter">
            <Masthead num="06" title={t("progress.title")} sub={t("common.loading")} />
        </div>
    );

    if (!timeline.length) return (
        <div className="page page-enter">
            <Masthead
                num="06"
                title={t("progress.title")}
                sub={t("progress.sub")}
                meta={[{ label: "DATE", value: today }]}
            />
            <div className="progress-empty">
                <div className="progress-empty-num">{t("progress.noDataNum")}</div>
                <div className="progress-empty-title">{t("progress.noDataTitle")}</div>
                <div className="progress-empty-sub">{t("progress.noDataSub")}</div>
            </div>
        </div>
    );

    const latest      = timeline[timeline.length - 1];
    const first       = timeline[0];
    const bestLevel   = timeline.reduce(
        (best, t) => (CEFR_CHART_IDX[t.predictedCefr] ?? 0) > (CEFR_CHART_IDX[best] ?? 0)
            ? t.predictedCefr : best,
        first.predictedCefr,
    );
    const avgMattr     = timeline.reduce((s, t) => s + t.mattr, 0) / timeline.length;
    const avgErrorRate = timeline.reduce((s, t) => s + t.grammarErrorRate, 0) / timeline.length;
    const errImproved  = timeline.length >= 2 && latest.grammarErrorRate < first.grammarErrorRate;
    const lastDate     = new Date(latest.date).toLocaleDateString("en-GB", { day: "numeric", month: "short" });

    const streak        = gamification?.currentStreak ?? 0;
    const longestStreak = gamification?.longestStreak ?? 0;
    const totalSessions = gamification?.totalSubmissions ?? timeline.length;
    const badges        = gamification?.badges ?? [];
    const badgesEarned  = badges.filter(b => b.unlockedAt).length;

    const cefrSeries   = timeline.map((p, i) => ({ w: i + 1, val: CEFR_CHART_IDX[p.predictedCefr] ?? 2 }));
    const mattrSeries  = timeline.map((p, i) => ({ w: i + 1, val: +(p.mattr * 100).toFixed(1) }));
    const errorSeries  = timeline.map((p, i) => ({ w: i + 1, val: +(p.grammarErrorRate * 100).toFixed(1) }));
    const weeklySeries = buildWeeklyBars(timeline);
    const maxError     = Math.max(...errorSeries.map(s => s.val), 5);
    const maxWeekly    = Math.max(...weeklySeries.map(s => s.val), 3);

    return (
        <div className="page page-enter">
            <Masthead
                num="06"
                title={t("progress.title")}
                sub={t("progress.sub")}
                meta={[
                    { label: "SESSIONS", value: String(totalSessions) },
                    { label: "LEVEL",    value: latest.predictedCefr },
                    { label: "DATE",     value: lastDate },
                ]}
            />

            <div className="progress-download-strip">
                <button className="btn btn--sm" onClick={handleDownload} disabled={downloading}>
                    {downloading ? t("progress.generating") : t("progress.downloadPdf")}
                </button>
            </div>

            <div className="stats-block">
                <div className="stat-cell">
                    <span className="stat-cell-num">01</span>
                    <div className="stat-cell-value stat-cell-value--accent">{latest.predictedCefr}</div>
                    <div className="stat-cell-label">{t("progress.currentLevel")}</div>
                    <div className="stat-cell-delta">{CEFR_NAME[latest.predictedCefr] ?? "—"}</div>
                </div>
                <div className="stat-cell">
                    <span className="stat-cell-num">02</span>
                    <div className="stat-cell-value">{totalSessions}</div>
                    <div className="stat-cell-label">{t("progress.writingSessions")}</div>
                    <div className="stat-cell-delta">{t("progress.best", { level: bestLevel, name: CEFR_NAME[bestLevel] ?? "" })}</div>
                </div>
                <div className="stat-cell">
                    <span className="stat-cell-num">03</span>
                    <div className="stat-cell-value">{(avgMattr * 100).toFixed(1)}%</div>
                    <div className="stat-cell-label">{t("progress.avgVocabulary")}</div>
                    <div className="stat-cell-delta">MATTR · {errImproved ? t("progress.errorRateDown") : t("progress.keepWriting")}</div>
                </div>
                <div className="stat-cell">
                    <span className="stat-cell-num">04</span>
                    <div className={`stat-cell-value${streak > 0 ? " stat-cell-value--gold" : ""}`}>
                        {streak > 0 ? `${streak}d` : "0d"}
                    </div>
                    <div className="stat-cell-label">{t("progress.currentStreak")}</div>
                    <div className="stat-cell-delta">
                        {streak > 0 ? t("progress.longestStreak", { n: longestStreak }) : t("progress.writeToday")}
                    </div>
                </div>
            </div>

            <SectionHead
                num="01"
                title={t("progress.cefrScale")}
                right={<span className="label">{lastDate}</span>}
            />
            <CEFRLadder current={latest.predictedCefr} />

            <SectionHead
                num="02"
                title={t("progress.trajectory")}
                right={<span className="label">{t("progress.sessionsAllTime", { n: timeline.length, s: timeline.length !== 1 ? "s" : "" })}</span>}
            />
            <div className="progress-chart-frame" style={{ marginTop: 8 }}>
                <div className="progress-chart-hd">
                    <span className="progress-chart-title">{t("progress.levelOverTime")}</span>
                    <span className="progress-chart-sub">{t("progress.a1c1")}</span>
                </div>
                <LineChart
                    series={cefrSeries}
                    max={4} min={0}
                    yLabels={["A1", "A2", "B1", "B2", "C1"]}
                    color="var(--red)"
                    height={240}
                />
            </div>

            <div className="progress-dual">
                <div>
                    <SectionHead num="03" title={t("progress.vocabRichness")} right={<span className="label">{t("progress.mattrPct")}</span>} />
                    <div className="progress-chart-frame" style={{ marginTop: 8 }}>
                        <div className="progress-chart-hd">
                            <span className="progress-chart-title">{t("progress.mattrOverSessions")}</span>
                            <span className="progress-chart-sub">0 → 100%</span>
                        </div>
                        <LineChart
                            series={mattrSeries}
                            max={100} min={0}
                            color="var(--green)"
                            height={200}
                        />
                    </div>
                </div>
                <div>
                    <SectionHead num="04" title={t("progress.grammarErrorRate")} right={<span className="label">{errImproved ? t("progress.improving") : t("progress.perSession")}</span>} />
                    <div className="progress-chart-frame" style={{ marginTop: 8 }}>
                        <div className="progress-chart-hd">
                            <span className="progress-chart-title">{t("progress.errorDensity")}</span>
                            <span className="progress-chart-sub">{t("progress.lowerBetter")}</span>
                        </div>
                        <BarChart
                            series={errorSeries}
                            max={maxError}
                            color={errImproved ? "var(--green)" : "var(--gold)"}
                            height={200}
                        />
                    </div>
                </div>
            </div>

            {weeklySeries.length > 1 && (
                <>
                    <SectionHead
                        num="05"
                        title={t("progress.sessionsPerWeek")}
                        right={<span className="label">{t("progress.weekLabel", { n: weeklySeries.length, s: weeklySeries.length !== 1 ? "s" : "" })}</span>}
                    />
                    <div className="progress-chart-frame" style={{ marginTop: 8 }}>
                        <div className="progress-chart-hd">
                            <span className="progress-chart-title">{t("progress.writingFrequency")}</span>
                            <span className="progress-chart-sub">{t("progress.sessionsPerWeekLabel")}</span>
                        </div>
                        <BarChart
                            series={weeklySeries}
                            max={maxWeekly}
                            color="var(--blue)"
                            height={180}
                        />
                    </div>
                </>
            )}

            {badges.length > 0 && (
                <>
                    <SectionHead
                        num="06"
                        title={t("progress.achievementBadges")}
                        right={<span className="label">{t("progress.ofUnlocked", { n: badgesEarned, total: badges.length })}</span>}
                    />
                    <div className="progress-badge-grid" style={{ marginTop: 8 }}>
                        {badges.map((badge, i) => {
                            const cfg     = BADGE_CONFIG[badge.key] ?? { glyph: "square" as GlyphKind, color: "var(--text-mute)" };
                            const unlocked = !!badge.unlockedAt;
                            const date     = badge.unlockedAt
                                ? new Date(badge.unlockedAt).toLocaleDateString("en-GB", { day: "numeric", month: "short", year: "numeric" })
                                : null;
                            return (
                                <div
                                    key={badge.key}
                                    className={`progress-badge-card${unlocked ? " progress-badge-card--unlocked" : " progress-badge-card--locked"}`}
                                    style={unlocked ? { borderTop: `3px solid ${cfg.color}` } : { borderTop: "3px solid var(--rule-soft)" }}
                                >
                                    <span className="progress-badge-num">{String(i + 1).padStart(2, "0")}</span>
                                    <div className="progress-badge-glyph" style={unlocked ? { borderColor: cfg.color, color: cfg.color } : {}}>
                                        <Glyph kind={cfg.glyph} color={unlocked ? cfg.color : "var(--text-mute)"} size={22} />
                                    </div>
                                    <div>
                                        <div className="progress-badge-title">{badge.title}</div>
                                        <div className="progress-badge-desc">{badge.description}</div>
                                        {unlocked && date
                                            ? <div className="progress-badge-date">{date}</div>
                                            : <span className="progress-badge-locked-lbl">{t("progress.locked")}</span>
                                        }
                                    </div>
                                </div>
                            );
                        })}
                    </div>
                </>
            )}

            {patterns.length > 0 && (
                <>
                    <SectionHead
                        num="07"
                        title={t("progress.recurringErrors")}
                        right={<span className="label">{t("progress.patternLabel", { n: patterns.length, s: patterns.length !== 1 ? "s" : "" })}</span>}
                    />
                    <div className="progress-pattern-list" style={{ marginTop: 8 }}>
                        {patterns.slice(0, 12).map(p => (
                            <div key={p.ruleId} className="progress-pattern-row">
                                <span className="progress-pattern-cat">{p.errorCategory}</span>
                                <span className="progress-pattern-rule">{p.ruleId}</span>
                                <span className="progress-pattern-count">{p.count}×</span>
                                <span className="progress-pattern-last">
                                    {new Date(p.lastSeen).toLocaleDateString("en-GB", { day: "numeric", month: "short" })}
                                </span>
                            </div>
                        ))}
                    </div>
                </>
            )}
        </div>
    );
};

export default ProgressPage;
