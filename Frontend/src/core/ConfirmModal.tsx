import { createContext, useCallback, useContext, useRef, useState } from "react";
import { useTranslation } from "react-i18next";
import "./confirm-modal.css";

interface ConfirmOptions {
    title?:       string;
    message:      string;
    confirmLabel?: string;
    cancelLabel?:  string;
    danger?:       boolean;
}

type ConfirmFn = (options: ConfirmOptions) => Promise<boolean>;

const ConfirmContext = createContext<ConfirmFn>(() => Promise.resolve(false));

export const useConfirm = () => useContext(ConfirmContext);

export const ConfirmProvider = ({ children }: { children: React.ReactNode }) => {
    const [opts, setOpts]   = useState<ConfirmOptions | null>(null);
    const resolveRef        = useRef<((v: boolean) => void) | null>(null);
    const { t } = useTranslation();

    const confirm: ConfirmFn = useCallback((options) => {
        return new Promise<boolean>(resolve => {
            setOpts(options);
            resolveRef.current = resolve;
        });
    }, []);

    const respond = (value: boolean) => {
        setOpts(null);
        resolveRef.current?.(value);
    };

    return (
        <ConfirmContext.Provider value={confirm}>
            {children}
            {opts && (
                <div className="confirm-overlay" onClick={() => respond(false)}>
                    <div className="confirm-modal" onClick={e => e.stopPropagation()}>
                        {opts.title && <h3 className="confirm-title">{opts.title}</h3>}
                        <p className="confirm-message">{opts.message}</p>
                        <div className="confirm-actions">
                            <button className="confirm-btn-cancel" onClick={() => respond(false)}>
                                {opts.cancelLabel ?? t("confirm.cancel")}
                            </button>
                            <button
                                className={`confirm-btn-ok ${opts.danger ? "danger" : ""}`}
                                onClick={() => respond(true)}
                            >
                                {opts.confirmLabel ?? t("confirm.confirm")}
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </ConfirmContext.Provider>
    );
};