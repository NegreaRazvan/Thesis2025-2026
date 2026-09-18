import { useTranslation } from "react-i18next";
import { WRITING_STARTERS } from "../constants";
import { SectionHead } from "../../core/SharedComponents";

interface WriteEmptyStateProps {
    onSelectStarter: (text: string) => void;
}

const WriteEmptyState = ({ onSelectStarter }: WriteEmptyStateProps) => {
    const { t } = useTranslation();

    return (
        <div>
            <SectionHead num="01" title={t("write.promptsTitle")} right={<span className="label">{t("write.promptsLabel")}</span>} />
            <div className="write-starters-grid" style={{ marginTop: 8 }}>
                {WRITING_STARTERS.map(s => (
                    <button
                        key={s.num}
                        className="write-starter-chip"
                        onClick={() => onSelectStarter(`${s.topic}: `)}
                    >
                        <span className="write-starter-num"># {s.num}</span>
                        <span className="write-starter-topic">{s.topic}</span>
                        <span className="write-starter-hint">{s.hint}</span>
                    </button>
                ))}
            </div>
        </div>
    );
};

export default WriteEmptyState;
