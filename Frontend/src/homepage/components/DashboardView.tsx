import { useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import type { GamificationProps, ProgressPointProps } from "../../progress/props";
import { Masthead, CEFRStamp, CEFRLadder, SectionHead, LineChart, Glyph } from "../../core/SharedComponents";
import { CEFR_NAME, CEFR_CHART_IDX } from "../../core/cefr";

const getGreeting = () => {
    const h = new Date().getHours();
    if (h < 12) return "Guten Morgen";
    if (h < 18) return "Guten Tag";
    return "Guten Abend";
};

interface DashboardViewProps {
    username:     string;
    gamification: GamificationProps | null;
    lastLevel:    string | null;
    lastDate:     string | null;
    timeline:     ProgressPointProps[];
}

const DashboardView = ({ username, gamification, lastLevel, lastDate, timeline }: DashboardViewProps) => {
    const navigate = useNavigate();
    const { t } = useTranslation();

    const badgesEarned = gamification?.badges.filter(b => b.unlockedAt).length ?? 0;
    const streak = gamification?.currentStreak ?? 0;
    const sessions = gamification?.totalSubmissions ?? 0;

    const cefrSeries = timeline.map((p, i) => ({
        w: i + 1,
        val: CEFR_CHART_IDX[p.predictedCefr] ?? 2,
    }));

    const latestCefrVal = cefrSeries.length
        ? cefrSeries[cefrSeries.length - 1].val
        : null;
    const latestLevel = lastLevel ?? (latestCefrVal !== null ? Object.keys(CEFR_CHART_IDX).find(k => CEFR_CHART_IDX[k] === latestCefrVal) : null);

    const QUICK_ACTIONS = [
        { to: "/write",      num: "02", titleKey: "home.writeSessionTitle", hintKey: "home.writeSessionHint", glyph: "square"   as const, color: "var(--red)",   span: 6 },
        { to: "/coach",      num: "03", titleKey: "home.talkLena",          hintKey: "home.coachHint",        glyph: "circle"   as const, color: "var(--blue)",  span: 6 },
        { to: "/flashcards", num: "04", titleKey: "home.reviewFlashcards",  hintKey: "home.flashcardsHint",   glyph: "triangle" as const, color: "var(--gold)",  span: 4 },
        { to: "/vocabgame",  num: "05", titleKey: "home.vocabSprint",       hintKey: "home.vocabHint",        glyph: "diamond"  as const, color: "var(--green)", span: 4 },
        { to: "/progress",   num: "06", titleKey: "home.seeProgress",       hintKey: "home.progressHint",     glyph: "square"   as const, color: "var(--text)",  span: 4 },
    ];

    const stats = [
        { num: "01", val: latestLevel ?? "—",    lbl: t("home.currentLevel"),    delta: lastDate ? t("home.lastDate", { date: lastDate }) : t("home.noSubmissions"), accent: true },
        { num: "02", val: String(sessions),      lbl: t("home.writingSessions"), delta: sessions > 0 ? `+${Math.min(sessions, 4)} this week` : t("home.startWritingBang") },
        { num: "03", val: String(badgesEarned),  lbl: t("home.badgesEarned"),    delta: t("home.ofTotal", { total: gamification?.badges.length ?? 0 }) },
        { num: "04", val: streak > 0 ? `${streak}d` : "0d", lbl: t("home.currentStreak"), delta: streak > 0 ? t("home.keepGoing") : t("home.startToday"), gold: true },
    ];

    return (
        <div className="page page-enter">
            <Masthead
                num="01"
                title={<>{getGreeting()}, <em style={{ color: "var(--red)", fontStyle: "normal" }}>{username}</em>.</>}
                sub={t("home.sub")}
                meta={[
                    { label: "LEVEL",  value: latestLevel ?? "—" },
                    { label: "STREAK", value: streak > 0 ? `${streak} days` : "—" },
                    { label: "DATE",   value: lastDate ?? "—" },
                ]}
            />

            <div className="home-hero">
                <div>
                    <div className="home-greeting-display">
                        {t("home.heroLine1")}<br />
                        <em>{t("home.heroLine2")}</em>
                    </div>
                    <div className="home-greeting-sub">
                        {latestLevel
                            ? <>{t("home.heroSub_hasLevel", { level: latestLevel, name: CEFR_NAME[latestLevel] })}</>
                            : t("home.heroSub_noLevel")
                        }
                    </div>
                    <div className="home-hero-actions">
                        <button className="btn btn--primary" onClick={() => navigate("/write")}>
                            {t("home.startWriting")}
                            <span style={{ fontFamily: "var(--font-mono)", fontSize: 10, opacity: 0.7, marginLeft: 4 }}>W</span>
                        </button>
                        <button className="btn" onClick={() => navigate("/coach")}>{t("home.openCoach")}</button>
                        <button className="btn btn--ghost" onClick={() => navigate("/progress")}>{t("home.viewProgress")}</button>
                    </div>
                </div>

                <div className="home-stamp-card">
                    <div className="home-stamp-hd">
                        <span className="label">{t("home.lastAssessment")}</span>
                        <span className="label">{lastDate ?? "—"}</span>
                    </div>
                    {latestLevel ? (
                        <>
                            <div className="home-stamp-body">
                                <CEFRStamp
                                    level={latestLevel}
                                    name={CEFR_NAME[latestLevel]}
                                    date={lastDate ?? undefined}
                                    size="sm"
                                />
                                <div className="home-stamp-meta">
                                    <div className="home-stamp-conf-lbl">{t("home.cefrLevel")}</div>
                                    <div className="home-stamp-conf-val">{latestLevel}</div>
                                    <div className="home-stamp-conf-delta">{CEFR_NAME[latestLevel]}</div>
                                </div>
                            </div>
                            <div className="home-stamp-foot">
                                <span>{t("home.sessions")}: <strong>{sessions}</strong></span>
                                <span>{t("home.badges")}: <strong>{badgesEarned}</strong></span>
                            </div>
                        </>
                    ) : (
                        <div style={{ flex: 1, display: "grid", placeItems: "center", color: "var(--text-mute)", fontFamily: "var(--font-mono)", fontSize: 11, letterSpacing: "0.12em", textTransform: "uppercase" }}>
                            {t("home.noAssessment")}
                        </div>
                    )}
                </div>
            </div>

            <div className="stats-block">
                {stats.map(s => (
                    <div key={s.num} className="stat-cell">
                        <span className="stat-cell-num">{s.num}</span>
                        <div className={`stat-cell-value ${s.accent ? "stat-cell-value--accent" : ""} ${s.gold ? "stat-cell-value--gold" : ""}`}>
                            {s.val}
                        </div>
                        <div className="stat-cell-label">{s.lbl}</div>
                        <div className="stat-cell-delta">{s.delta}</div>
                    </div>
                ))}
            </div>

            <SectionHead num="02" title={t("home.continueOff")} right={<span className="label">{t("home.actionsLabel")}</span>} />
            <div className="quick-grid" style={{ marginTop: 8 }}>
                {QUICK_ACTIONS.map(q => (
                    <button
                        key={q.to}
                        className="qa-card"
                        style={{ gridColumn: `span ${q.span}` }}
                        onClick={() => navigate(q.to)}
                    >
                        <div className="qa-card-glyph"><Glyph kind={q.glyph} color={q.color} size={20} /></div>
                        <span className="qa-card-num">{q.num}</span>
                        <div className="qa-card-title">{t(q.titleKey)}</div>
                        <div className="qa-card-hint">{t(q.hintKey)}</div>
                    </button>
                ))}
            </div>

            <SectionHead num="03" title={t("home.ladder")} right={<span className="label">{lastDate ?? t("home.noAssessment")}</span>} />
            <CEFRLadder current={latestLevel} />

            <SectionHead num="04" title={t("home.trajectory")} right={
                <button className="btn btn--sm" onClick={() => navigate("/progress")}>{t("home.fullProgress")}</button>
            } />
            <div className="home-insight">
                <div className="insight-chart-frame">
                    <div className="insight-hd">
                        <span className="insight-title">{t("home.cefrHistory")}</span>
                        <span className="label mono">{t("home.a1c1")}</span>
                    </div>
                    <LineChart
                        series={cefrSeries.slice(-14)}
                        max={4} min={0}
                        yLabels={["A1", "A2", "B1", "B2", "C1"]}
                        color="var(--red)"
                    />
                </div>
                <div className="insight-legend">
                    <div className="legend-row">
                        <Glyph kind="square" color="var(--red)" size={14} />
                        <div>
                            <div className="legend-label">{t("home.cefrModel")}</div>
                            <div className="legend-sub">{t("home.fromMlPipeline")}</div>
                        </div>
                        <div className="legend-val">{latestLevel ?? "—"}</div>
                    </div>
                    <div className="legend-row">
                        <Glyph kind="circle" color="var(--green)" size={14} />
                        <div>
                            <div className="legend-label">{t("home.writingSessions")}</div>
                            <div className="legend-sub">{t("home.totalSubmitted")}</div>
                        </div>
                        <div className="legend-val">{sessions}</div>
                    </div>
                    <div className="legend-row">
                        <Glyph kind="triangle" color="var(--gold)" size={14} />
                        <div>
                            <div className="legend-label">{t("home.badgesEarned")}</div>
                            <div className="legend-sub">{t("home.unlockedMilestones")}</div>
                        </div>
                        <div className="legend-val">{badgesEarned}</div>
                    </div>
                </div>
            </div>

            <SectionHead num="05" title={t("home.recentSessions")} right={
                <button className="btn btn--sm" onClick={() => navigate("/history")}>{t("home.allHistory")}</button>
            } />
            {timeline.length === 0 ? (
                <div className="home-empty-state">
                    <div className="home-empty-num">{t("home.noSessions")}</div>
                    <div className="home-empty-text">{t("home.noSessionsText")}</div>
                    <button className="btn btn--primary" onClick={() => navigate("/write")}>{t("home.startWritingArrow")}</button>
                </div>
            ) : (
                <div className="feat-index" style={{ marginTop: 8 }}>
                    {timeline.slice(-5).reverse().map((p, i) => {
                        const lvl = p.predictedCefr;
                        const date = new Date(p.date).toLocaleDateString("en-GB", { day: "numeric", month: "short" });
                        return (
                            <div key={i} className="feat-row" onClick={() => navigate("/history")}>
                                <span className="feat-num">№ {String(timeline.length - i).padStart(3, "0")}</span>
                                <span className="feat-name">
                                    <span style={{ color: "var(--red)", marginRight: 12, fontSize: 14, fontFamily: "var(--font-mono)" }}>{lvl}</span>
                                    {CEFR_NAME[lvl]}
                                </span>
                                <span className="feat-desc">
                                    {t("home.grammarErrorRate", { rate: p.grammarErrorRate.toFixed(2), mattr: p.mattr.toFixed(2) })}
                                </span>
                                <span className="feat-arrow">{date} ↗</span>
                            </div>
                        );
                    })}
                </div>
            )}
        </div>
    );
};

export default DashboardView;
