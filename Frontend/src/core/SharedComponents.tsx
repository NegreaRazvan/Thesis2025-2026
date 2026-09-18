import { useId } from "react";
import "./shared.css";

interface MastheadMeta { label: string; value: string; }
interface SeriesPoint  { w: number; val: number; }

export const Masthead = ({
    num, title, sub, meta,
}: {
    num: string;
    title: React.ReactNode;
    sub?: string;
    meta?: MastheadMeta[];
}) => (
    <div className="masthead">
        <div className="mh-number">{num}</div>
        <div>
            <div className="mh-title">{title}</div>
            {sub && <div className="mh-sub">{sub}</div>}
        </div>
        {meta && meta.length > 0 && (
            <div className="mh-meta">
                {meta.map((m, i) => (
                    <div key={i}>{m.label}&nbsp;·&nbsp;<strong>{m.value}</strong></div>
                ))}
            </div>
        )}
    </div>
);

export const CEFRStamp = ({
    level = "B1",
    name = "Intermediate",
    date,
    size = "lg",
}: {
    level?: string;
    name?: string;
    date?: string;
    size?: "lg" | "sm";
}) => {
    const id = useId().replace(/:/g, "");
    const klass = size === "sm" ? "stamp stamp--sm" : "stamp";
    const dateStr = date ?? new Date().toLocaleDateString("de-DE", { day: "2-digit", month: "2-digit", year: "2-digit" });
    const ringChars = `· LINGUAFORGE ASSESSMENT · ${dateStr} · LEVEL · `;
    const r = size === "sm" ? 50 : 76;
    const cx = size === "sm" ? 60 : 90;

    return (
        <div className={klass} data-level={level}>
            <svg className="stamp-ring-text" viewBox={`0 0 ${cx * 2} ${cx * 2}`}>
                <defs>
                    <path
                        id={id}
                        d={`M ${cx},${cx} m -${r},0 a ${r},${r} 0 1,1 ${r * 2},0 a ${r},${r} 0 1,1 -${r * 2},0`}
                    />
                </defs>
                <text fill="var(--text-mute)">
                    <textPath href={`#${id}`} startOffset="0">
                        {(ringChars).repeat(4)}
                    </textPath>
                </text>
            </svg>
            <div className="stamp-lvl">{level}</div>
            <div className="stamp-lvl-name">{name}</div>
        </div>
    );
};

export const SectionHead = ({
    num, title, right,
}: {
    num: string;
    title: string;
    right?: React.ReactNode;
}) => (
    <div className="section-head">
        <span className="section-head-num">{num}</span>
        <div className="section-head-title-wrap">
            <span className="section-head-title">{title}</span>
            <div className="section-head-rule" />
        </div>
        <div>{right}</div>
    </div>
);

export const Glyph = ({
    kind = "square",
    color = "currentColor",
    size = 16,
}: {
    kind?: "square" | "circle" | "triangle" | "diamond";
    color?: string;
    size?: number;
}) => {
    const style: React.CSSProperties = { color, width: size, height: size, display: "inline-block", flexShrink: 0 };
    if (kind === "square")   return <span className="shape-square"   style={style} />;
    if (kind === "circle")   return <span className="shape-circle"   style={style} />;
    if (kind === "triangle") return <span className="shape-triangle" style={style} />;
    if (kind === "diamond")  return <span style={{ ...style, background: color, transform: "rotate(45deg)" }} />;
    return <span className="shape-square" style={style} />;
};

export const LineChart = ({
    series,
    max = 4,
    min = 0,
    color = "var(--red)",
    height = 220,
    yLabels,
    filled = true,
}: {
    series: SeriesPoint[];
    max?: number;
    min?: number;
    color?: string;
    height?: number;
    yLabels?: string[];
    filled?: boolean;
}) => {
    if (!series || series.length < 2) {
        return (
            <div style={{ height, display: "grid", placeItems: "center", color: "var(--text-mute)", fontFamily: "var(--font-mono)", fontSize: 11 }}>
                NOT ENOUGH DATA
            </div>
        );
    }

    const W = 600, H = height;
    const pad = { l: 36, r: 12, t: 12, b: 28 };
    const iw = W - pad.l - pad.r;
    const ih = H - pad.t - pad.b;

    const xs = series.map((_, i) => pad.l + (i / (series.length - 1)) * iw);
    const ys = series.map(d => pad.t + (1 - (d.val - min) / (max - min)) * ih);

    const linePath = series.map((_, i) => `${i === 0 ? "M" : "L"} ${xs[i]},${ys[i]}`).join(" ");
    const areaPath = `${linePath} L ${xs[xs.length - 1]},${pad.t + ih} L ${xs[0]},${pad.t + ih} Z`;

    const ticks = yLabels
        ? yLabels.map((lbl, i) => ({ lbl, y: pad.t + (1 - i / (yLabels.length - 1)) * ih }))
        : [min, (min + max) / 2, max].map((v, i, arr) => ({
              lbl: String(v),
              y: pad.t + (1 - (v - min) / (max - min)) * ih,
          }));

    return (
        <svg className="chart-svg" viewBox={`0 0 ${W} ${H}`} preserveAspectRatio="none">
            {ticks.map((t, i) => (
                <g key={i}>
                    <line className="chart-grid-line" x1={pad.l} x2={W - pad.r} y1={t.y} y2={t.y} />
                    <text className="chart-axis-text" x={pad.l - 6} y={t.y + 3} textAnchor="end">{t.lbl}</text>
                </g>
            ))}
            <line className="chart-axis-line" x1={pad.l} x2={W - pad.r} y1={pad.t + ih} y2={pad.t + ih} />
            {series.map((d, i) => (
                i % 2 === 0 ? (
                    <text key={i} className="chart-axis-text" x={xs[i]} y={H - 10} textAnchor="middle">
                        W{d.w}
                    </text>
                ) : null
            ))}
            {filled && <path className="chart-area" d={areaPath} fill={color} />}
            <path className="chart-line" d={linePath} stroke={color} />
            {series.map((_, i) => (
                <circle key={i} className="chart-dot" cx={xs[i]} cy={ys[i]} fill={color} />
            ))}
        </svg>
    );
};

export const BarChart = ({
    series,
    max = 10,
    color = "var(--gold)",
    height = 220,
}: {
    series: SeriesPoint[];
    max?: number;
    color?: string;
    height?: number;
}) => {
    if (!series || series.length === 0) {
        return (
            <div style={{ height, display: "grid", placeItems: "center", color: "var(--text-mute)", fontFamily: "var(--font-mono)", fontSize: 11 }}>
                NO DATA
            </div>
        );
    }

    const W = 600, H = height;
    const pad = { l: 36, r: 12, t: 12, b: 28 };
    const iw = W - pad.l - pad.r;
    const ih = H - pad.t - pad.b;
    const barW = (iw / series.length) * 0.72;
    const gap  = iw / series.length;

    return (
        <svg className="chart-svg" viewBox={`0 0 ${W} ${H}`} preserveAspectRatio="none">
            {[0, max / 2, max].map((v, i) => {
                const y = pad.t + (1 - v / max) * ih;
                return (
                    <g key={i}>
                        <line className="chart-grid-line" x1={pad.l} x2={W - pad.r} y1={y} y2={y} />
                        <text className="chart-axis-text" x={pad.l - 6} y={y + 3} textAnchor="end">{v}</text>
                    </g>
                );
            })}
            {series.map((d, i) => {
                const x    = pad.l + i * gap + (gap - barW) / 2;
                const barH = (d.val / max) * ih;
                const y    = pad.t + ih - barH;
                return (
                    <g key={i}>
                        <rect x={x} y={y} width={barW} height={barH} fill={color} />
                        <text className="chart-axis-text" x={x + barW / 2} y={H - 10} textAnchor="middle">
                            W{d.w}
                        </text>
                    </g>
                );
            })}
        </svg>
    );
};

const RUNGS = [
    { lvl: "A1", nm: "Breakthrough", key: "a1" },
    { lvl: "A2", nm: "Waystage",     key: "a2" },
    { lvl: "B1", nm: "Threshold",    key: "b1" },
    { lvl: "B2", nm: "Vantage",      key: "b2" },
    { lvl: "C1", nm: "Effective",    key: "c1" },
];

export const CEFRLadder = ({ current }: { current?: string | null }) => (
    <div className="cefr-ladder">
        {RUNGS.map(r => (
            <div key={r.lvl} className={`ladder-rung ladder-rung--${r.key} ${r.lvl === current ? "ladder-rung--current" : ""}`}>
                <div className="ladder-rung-lvl">{r.lvl}</div>
                <div className="ladder-rung-nm">{r.nm}</div>
            </div>
        ))}
    </div>
);
