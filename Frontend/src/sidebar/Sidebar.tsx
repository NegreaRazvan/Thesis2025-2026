import { useEffect, useState } from "react";
import { NavLink, useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { useAuthContext } from "../auth/context/AuthContext";
import { useTheme } from "../App";
import useProgressApi from "../progress/useProgressApi";
import useAvatarBlobUrl from "../profile/useAvatarBlobUrl";
import "./sidebar.css";

const Sidebar = () => {
    const { user, logout } = useAuthContext();
    const { theme, setTheme } = useTheme();
    const navigate = useNavigate();
    const { getGamification } = useProgressApi();
    const { t } = useTranslation();
    const [streak, setStreak] = useState(0);
    const avatarBlobUrl = useAvatarBlobUrl(!!user?.avatarVersion, user?.avatarVersion);

    useEffect(() => {
        getGamification()
            .then(g => setStreak(g.currentStreak))
            .catch(() => {});
    // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);

    const PRACTICE = [
        { to: "/",           num: "01", labelKey: "nav.dashboard",  kbd: "H", end: true },
        { to: "/write",      num: "02", labelKey: "nav.write",      kbd: "W" },
        { to: "/coach",      num: "03", labelKey: "nav.coach",      kbd: "C" },
    ];
    const VOCAB = [
        { to: "/flashcards", num: "04", labelKey: "nav.flashcards", kbd: "F" },
        { to: "/vocabgame",  num: "05", labelKey: "nav.vocabgame",  kbd: "V" },
    ];
    const REVIEW = [
        { to: "/progress",   num: "06", labelKey: "nav.progress",   kbd: "P" },
        { to: "/history",    num: "07", labelKey: "nav.history",     kbd: "Y" },
    ];

    const streakDots = Array.from({ length: 7 }).map((_, i) => (
        <span key={i} className={`sb-streak-dot ${i < Math.min(streak, 7) ? "sb-streak-dot--on" : ""}`} />
    ));

    const displayName = user?.displayName || user?.username;
    const initials = displayName?.slice(0, 2).toUpperCase() ?? "??";

    return (
        <aside className="sidebar">
            <button className="sb-brand" onClick={() => navigate("/")}>
                <div className="sb-mark">L</div>
                <div>
                    <div className="sb-wordmark">LinguaForge</div>
                    <div className="sb-tag">{t("sidebar.tagline")}</div>
                </div>
            </button>

            <div className="sb-section">{t("nav.practice")}</div>
            <nav className="sb-nav">
                {PRACTICE.map(item => (
                    <NavLink
                        key={item.to}
                        to={item.to}
                        end={item.end}
                        className={({ isActive }) => `sb-link ${isActive ? "sb-link--active" : ""}`}
                    >
                        <span className="sb-nindex">{item.num}</span>
                        <span>{t(item.labelKey)}</span>
                        <span className="sb-kbd">{item.kbd}</span>
                    </NavLink>
                ))}
            </nav>

            <div className="sb-section">{t("nav.vocabulary")}</div>
            <nav className="sb-nav">
                {VOCAB.map(item => (
                    <NavLink
                        key={item.to}
                        to={item.to}
                        className={({ isActive }) => `sb-link ${isActive ? "sb-link--active" : ""}`}
                    >
                        <span className="sb-nindex">{item.num}</span>
                        <span>{t(item.labelKey)}</span>
                        <span className="sb-kbd">{item.kbd}</span>
                    </NavLink>
                ))}
            </nav>

            <div className="sb-section">{t("nav.review")}</div>
            <nav className="sb-nav">
                {REVIEW.map(item => (
                    <NavLink
                        key={item.to}
                        to={item.to}
                        className={({ isActive }) => `sb-link ${isActive ? "sb-link--active" : ""}`}
                    >
                        <span className="sb-nindex">{item.num}</span>
                        <span>{t(item.labelKey)}</span>
                        <span className="sb-kbd">{item.kbd}</span>
                    </NavLink>
                ))}
            </nav>

            <div className="sb-foot">
                <div className="sb-theme-toggle">
                    <button
                        className={`sb-theme-btn ${theme === "light" ? "sb-theme-btn--active" : ""}`}
                        onClick={() => setTheme("light")}
                    >
                        {t("sidebar.light")}
                    </button>
                    <button
                        className={`sb-theme-btn ${theme === "dark" ? "sb-theme-btn--active" : ""}`}
                        onClick={() => setTheme("dark")}
                    >
                        {t("sidebar.dark")}
                    </button>
                </div>

                {streak > 0 && (
                    <div className="sb-streak">
                        <div>
                            <div className="sb-streak-num">{streak}</div>
                            <div className="sb-streak-lbl">{t("sidebar.dayStreak")}</div>
                            <div className="sb-streak-dots">{streakDots}</div>
                        </div>
                    </div>
                )}

                <div className="sb-user" onClick={() => navigate("/profile")} role="button" tabIndex={0} title={t("profile.viewProfile")}>
                    <div className="sb-avatar">
                        {avatarBlobUrl
                            ? <img src={avatarBlobUrl} alt="" style={{ width: "100%", height: "100%", objectFit: "cover" }} />
                            : initials
                        }
                    </div>
                    <div className="sb-user-info">
                        <div className="sb-user-name">{displayName}</div>
                        <div className="sb-user-sub">{t("sidebar.viewProfile")}</div>
                    </div>
                    <button
                        className="sb-logout"
                        onClick={e => { e.stopPropagation(); logout(); navigate("/login"); }}
                        title={t("profile.signOut")}
                    >
                        ↗
                    </button>
                </div>
            </div>
        </aside>
    );
};

export default Sidebar;
