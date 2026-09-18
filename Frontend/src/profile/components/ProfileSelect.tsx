import { useEffect, useRef, useState } from "react";

interface SelectOption {
    value: string;
    label: string;
}

interface ProfileSelectProps {
    value:       string;
    onChange:    (value: string) => void;
    options:     SelectOption[];
    placeholder?: string;
}

const ProfileSelect = ({ value, onChange, options, placeholder = "— select —" }: ProfileSelectProps) => {
    const [open, setOpen] = useState(false);
    const ref = useRef<HTMLDivElement>(null);

    useEffect(() => {
        const handler = (e: MouseEvent) => {
            if (ref.current && !ref.current.contains(e.target as Node))
                setOpen(false);
        };
        if (open) document.addEventListener("mousedown", handler);
        return () => document.removeEventListener("mousedown", handler);
    }, [open]);

    const selected = options.find(o => o.value === value);

    return (
        <div
            ref={ref}
            className={`pf-select ${open ? "pf-select--open" : ""}`}
        >
            <button
                type="button"
                className="pf-select-trigger"
                onClick={() => setOpen(o => !o)}
            >
                <span className={selected ? "" : "pf-select-trigger-placeholder"}>
                    {selected ? selected.label : placeholder}
                </span>
                <span className="pf-select-chevron">▾</span>
            </button>

            {open && (
                <div className="pf-select-dropdown">
                    <button
                        type="button"
                        className={`pf-select-option ${!value ? "pf-select-option--selected" : ""}`}
                        onClick={() => { onChange(""); setOpen(false); }}
                    >
                        {placeholder}
                    </button>
                    {options.map(opt => (
                        <button
                            key={opt.value}
                            type="button"
                            className={`pf-select-option ${value === opt.value ? "pf-select-option--selected" : ""}`}
                            onClick={() => { onChange(opt.value); setOpen(false); }}
                        >
                            {opt.label}
                        </button>
                    ))}
                </div>
            )}
        </div>
    );
};

export default ProfileSelect;
