import { createContext, useContext, useEffect, useState } from "react";
import { Route, Routes, useNavigate } from "react-router-dom";
import { Toaster } from "react-hot-toast";
import { useTranslation } from "react-i18next";
import i18n from "./core/i18n";
import Sidebar from "./sidebar/Sidebar";
import PrivateRouter from "./routing/PrivateRouter";
import AuthPage from "./auth/pages/AuthPages";
import WritePage from "./write/pages/WritePage";
import ProgressPage from "./progress/pages/ProgressPage";
import FlashcardsPage from "./flashcards/pages/FlashcardsPage";
import HistoryPage from "./history/pages/HistoryPage";
import CoachPage from "./coach/pages/CoachPage";
import VocabGamePage from "./vocabgame/pages/VocabGamePage";
import ProfilePage from "./profile/pages/ProfilePage";
import { useAuthContext } from "./auth/context/AuthContext";
import { ConfirmProvider } from "./core/ConfirmModal";
import ErrorBoundary from "./core/ErrorBoundary";
import NotFoundPage from "./core/NotFoundPage";
import "./App.css";
import FlashcardSetDetailPage from "./flashcards/pages/FlashcardsDetailPage.tsx";
import HomePage from "./homepage/HomePage.tsx";

type Theme = "light" | "dark";
const ThemeContext = createContext<{ theme: Theme; toggle: () => void; setTheme: (t: Theme) => void }>({
    theme: "dark", toggle: () => {}, setTheme: () => {},
});
export const useTheme = () => useContext(ThemeContext);

export const ThemeProvider = ({ children }: { children: React.ReactNode }) => {
    const [theme, setThemeState] = useState<Theme>(() =>
        (localStorage.getItem("theme") as Theme) ?? "dark"
    );

    useEffect(() => {
        document.documentElement.setAttribute("data-theme", theme);
        localStorage.setItem("theme", theme);
    }, [theme]);

    const toggle = () => setThemeState(t => (t === "light" ? "dark" : "light"));
    const setTheme = (t: Theme) => setThemeState(t);
    return (
        <ThemeContext.Provider value={{ theme, toggle, setTheme }}>
            {children}
        </ThemeContext.Provider>
    );
};

type Lang = "en" | "de";
const LanguageContext = createContext<{ lang: Lang; setLanguage: (l: Lang) => void }>({
    lang: "en", setLanguage: () => {},
});
export const useLanguage = () => useContext(LanguageContext);

export const LanguageProvider = ({ children }: { children: React.ReactNode }) => {
    const [lang, setLangState] = useState<Lang>(() =>
        (localStorage.getItem("lang") as Lang) ?? "en"
    );

    const setLanguage = (l: Lang) => {
        setLangState(l);
        localStorage.setItem("lang", l);
        i18n.changeLanguage(l);
    };

    return (
        <LanguageContext.Provider value={{ lang, setLanguage }}>
            {children}
        </LanguageContext.Provider>
    );
};

const SHORTCUTS: Record<string, string> = {
    h: "/", w: "/write", c: "/coach",
    f: "/flashcards", v: "/vocabgame", p: "/progress", y: "/history",
};

const PageShell = ({
    index, name, children,
}: {
    index: string;
    name: string;
    children: React.ReactNode;
}) => (
    <div className="page-shell">
        <div className="page-rail">
            <div className="page-rail-inner">
                <span className="rail-num">{index}</span>
                <span className="rail-name">{name}</span>
            </div>
            <div className="rail-hairline" />
        </div>
        <div className="page-content">
            {children}
        </div>
    </div>
);

const AppShell = () => {
    const navigate = useNavigate();
    const { t } = useTranslation();

    useEffect(() => {
        const handler = (e: KeyboardEvent) => {
            const tag = (e.target as HTMLElement).tagName.toLowerCase();
            if (tag === "input" || tag === "textarea" || (e.target as HTMLElement).isContentEditable) return;
            if (e.metaKey || e.ctrlKey || e.altKey) return;
            const dest = SHORTCUTS[e.key.toLowerCase()];
            if (dest) navigate(dest);
        };
        window.addEventListener("keydown", handler);
        return () => window.removeEventListener("keydown", handler);
    }, [navigate]);

    return (
        <div className="app-shell">
            <Sidebar />
            <div className="app-main">
                <Routes>
                    <Route Component={PrivateRouter}>
                        <Route index element={<PageShell index="01" name={t("nav.dashboard").toUpperCase()}><HomePage /></PageShell>} />
                        <Route path="/write"          element={<PageShell index="02" name={t("nav.write").toUpperCase()}><WritePage /></PageShell>} />
                        <Route path="/coach"          element={<PageShell index="03" name={t("nav.coach").toUpperCase()}><CoachPage /></PageShell>} />
                        <Route path="/flashcards"     element={<PageShell index="04" name={t("nav.flashcards").toUpperCase()}><FlashcardsPage /></PageShell>} />
                        <Route path="/flashcards/:id" element={<PageShell index="04" name={t("nav.flashcards").toUpperCase()}><FlashcardSetDetailPage /></PageShell>} />
                        <Route path="/vocabgame"      element={<PageShell index="05" name={t("nav.vocabgame").toUpperCase()}><VocabGamePage /></PageShell>} />
                        <Route path="/progress"       element={<PageShell index="06" name={t("nav.progress").toUpperCase()}><ProgressPage /></PageShell>} />
                        <Route path="/history"        element={<PageShell index="07" name={t("nav.history").toUpperCase()}><HistoryPage /></PageShell>} />
                        <Route path="/profile"        element={<PageShell index="08" name={t("nav.profile").toUpperCase()}><ProfilePage /></PageShell>} />
                        <Route path="*"               Component={NotFoundPage} />
                    </Route>
                </Routes>
            </div>
        </div>
    );
};

const App = () => {
    const { user, loading } = useAuthContext();
    if (loading) return <div className="app-loading">Loading…</div>;

    return (
        <ErrorBoundary>
            <LanguageProvider>
                <ThemeProvider>
                    <ConfirmProvider>
                        <Toaster
                            position="top-right"
                            toastOptions={{
                                style: {
                                    background: "var(--surface)",
                                    color:      "var(--text)",
                                    border:     "1px solid var(--rule-soft)",
                                    borderRadius: "0",
                                    fontFamily: "var(--font-ui)",
                                    fontSize:   "13px",
                                },
                            }}
                        />
                        <Routes>
                            <Route path="/login"    Component={AuthPage} />
                            <Route path="/register" Component={AuthPage} />
                            <Route path="/*"        Component={user ? AppShell : AuthPage} />
                        </Routes>
                    </ConfirmProvider>
                </ThemeProvider>
            </LanguageProvider>
        </ErrorBoundary>
    );
};

export default App;
