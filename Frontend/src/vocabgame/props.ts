export interface VocabGameCategoryResponse {
    categoryKey: string;
    displayName: string;
}

export interface WordResult {
    word: string;
    valid: boolean;
    similarity: number;
    reason: "valid" | "not_related" | "duplicate" | "seed_word";
    misspelled: boolean;
    suggestions: string[];
    lemma: string;
    englishTranslation: string;
    article: string;
    plural: string;
}

export interface BatchValidateResponse {
    results: WordResult[];
    score: number;
}

export interface MisspelledWord {
    word: string;
    suggestions: string[];
    lemma: string;
    englishTranslation: string;
    article: string;
    plural: string;
}

export interface VocabGameSaveSession {
    categoryKey: string;
    displayName: string;
    score: number;
    validWords: string[];
    misspelledWords: MisspelledWord[];
}

export interface VocabGameSession {
    id: string;
    categoryKey: string;
    displayName: string;
    score: number;
    validWords: string[];
    misspelledWords: MisspelledWord[];
    playedAt: string;
}

export interface GameState {
    phase: "idle" | "playing" | "finished";
    category: VocabGameCategoryResponse | null;
    timeLeft: number;
    submittedWords: string[];
    results: WordResult[] | null;
    score: number;
}
