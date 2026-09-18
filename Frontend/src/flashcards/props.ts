export interface ErrorSuggestion {
    badText: string;
    suggestion: string;
    submissionId: string;
    category?: string;
}

export interface FlashcardSummary {
    id: string;
    name: string;
    description?: string;
    cardCount: number;
    dueCount: number;
    newCount: number;
    createdAt: string;
    lastReviewedAt?: string;
}

export interface FlashcardCard {
    id: string;
    front: string;
    back: string;
}

export interface FlashcardDetail {
    id: string;
    name: string;
    description?: string;
    cards: FlashcardCard[];
    createdAt: string;
}