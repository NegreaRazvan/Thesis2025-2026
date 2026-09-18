import { useTranslation } from "react-i18next";
import { Masthead } from "../../core/SharedComponents";
import "../vocabgame.css";

interface VocabGameIdleProps {
    starting: boolean;
    onStart: () => void;
}

const VocabGameIdle = ({ starting, onStart }: VocabGameIdleProps) => {
    const { t } = useTranslation();
    return (
    <div className="page page-enter vg-page">
        <Masthead
            num="05"
            title={t("vocabgame.title")}
            sub={t("vocabgame.sub")}
            meta={[
                { label: "EMBEDDING", value: "cc.de.300" },
                { label: "ROUND",     value: "60s" },
            ]}
        />

        <div className="vg-idle-box">
            <div className="vg-idle-left">
                <div className="vg-idle-badge">{t("vocabgame.idle_badge")}</div>
                <h1 className="vg-idle-heading">
                    {t("vocabgame.idle_heading_1")}<br />
                    {t("vocabgame.idle_heading_2")}<br />
                    {t("vocabgame.idle_heading_3")} <span className="vg-idle-accent">{t("vocabgame.idle_heading_accent")}</span>
                </h1>
                <p className="vg-idle-desc">{t("vocabgame.idle_desc")}</p>
                <button
                    className="vg-start-btn"
                    onClick={onStart}
                    disabled={starting}
                >
                    {starting ? t("vocabgame.loading") : t("vocabgame.startSprint")}
                </button>
            </div>

            <div className="vg-idle-right">
                <div className="vg-rules">
                    <div className="vg-rule-row">
                        <span className="vg-rule-num">01</span>
                        <span className="vg-rule-text">{t("vocabgame.rule1")}</span>
                    </div>
                    <div className="vg-rule-row">
                        <span className="vg-rule-num">02</span>
                        <span className="vg-rule-text">{t("vocabgame.rule2")}</span>
                    </div>
                    <div className="vg-rule-row">
                        <span className="vg-rule-num">03</span>
                        <span className="vg-rule-text">{t("vocabgame.rule3")}</span>
                    </div>
                    <div className="vg-rule-row">
                        <span className="vg-rule-num">04</span>
                        <span className="vg-rule-text">
                            <span className="vg-green">{t("vocabgame.rule4_valid")}</span>
                            {t("vocabgame.rule4_mid")}
                            <span className="vg-red">{t("vocabgame.rule4_invalid")}</span>
                            {t("vocabgame.rule4_end")}
                        </span>
                    </div>
                    <div className="vg-rule-row">
                        <span className="vg-rule-num">05</span>
                        <span className="vg-rule-text">{t("vocabgame.rule5")}</span>
                    </div>
                </div>
            </div>
        </div>
    </div>
    );
};

export default VocabGameIdle;
