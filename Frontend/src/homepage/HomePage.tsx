import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { useAuthContext } from "../auth/context/AuthContext";
import useProgressApi from "../progress/useProgressApi";
import type { GamificationProps, ProgressPointProps } from "../progress/props";
import DashboardView from "./components/DashboardView";
import toast from "react-hot-toast";
import "./home.css";

const HomePage = () => {
    const { user }                             = useAuthContext();
    const { getGamification, getTimeline }     = useProgressApi();
    const { t } = useTranslation();

    const [gamification, setGamification] = useState<GamificationProps | null>(null);
    const [timeline, setTimeline]         = useState<ProgressPointProps[]>([]);
    const [lastLevel, setLastLevel]       = useState<string | null>(null);
    const [lastDate, setLastDate]         = useState<string | null>(null);

    useEffect(() => {
        if (!user) return;
        getGamification().then(setGamification).catch(() => toast.error(t("errors.loadStats")));
        getTimeline().then(tl => {
            setTimeline(tl);
            if (tl.length) {
                const last = tl[tl.length - 1];
                setLastLevel(last.predictedCefr);
                setLastDate(new Date(last.date).toLocaleDateString("en-GB", {
                    day: "numeric", month: "short",
                }));
            }
        }).catch(() => toast.error(t("errors.loadTimeline")));
    }, []);

    return (
        <DashboardView
            username={user?.username ?? ""}
            gamification={gamification}
            lastLevel={lastLevel}
            lastDate={lastDate}
            timeline={timeline}
        />
    );
};

export default HomePage;
