import type { ErrorItemProps } from "./props";

export const isSpellingError = (e: ErrorItemProps): boolean => {
    const rule = e.ruleId.toUpperCase();
    const cat  = e.category.toUpperCase();
    return (
        rule === "GERMAN_SPELLER_RULE" ||
        rule.startsWith("MORFOLOGIK")  ||
        rule.startsWith("DE_COMPOUND") ||
        cat  === "TYPOS"               ||
        e.message.includes("Tippfehler")
    );
};
