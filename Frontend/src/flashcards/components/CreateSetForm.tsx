import { useTranslation } from "react-i18next";

interface CreateSetFormProps {
    name: string;
    desc: string;
    creating: boolean;
    onNameChange: (v: string) => void;
    onDescChange: (v: string) => void;
    onCreate: () => void;
    onCancel: () => void;
}

const CreateSetForm = ({
    name, desc, creating,
    onNameChange, onDescChange, onCreate, onCancel,
}: CreateSetFormProps) => {
    const { t } = useTranslation();

    return (
        <div className="fc-create-form">
            <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "0.75rem" }}>
                <input
                    className="fc-input"
                    placeholder={t("flashcards.setNamePlaceholder")}
                    value={name}
                    onChange={e => onNameChange(e.target.value)}
                    onKeyDown={e => e.key === "Enter" && onCreate()}
                    autoFocus
                />
                <input
                    className="fc-input"
                    placeholder={t("flashcards.setDescPlaceholder")}
                    value={desc}
                    onChange={e => onDescChange(e.target.value)}
                />
            </div>
            <div className="fc-form-row">
                <button className="fc-btn-primary" onClick={onCreate} disabled={creating || !name.trim()}>
                    {creating ? t("flashcards.creating") : t("flashcards.createSet")}
                </button>
                <button className="fc-btn-secondary" onClick={onCancel}>{t("common.cancel")}</button>
            </div>
        </div>
    );
};

export default CreateSetForm;
