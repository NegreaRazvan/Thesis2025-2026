export const CEFR_LEVELS = ['A1', 'A2', 'B1', 'B2', 'C1'] as const;
export type CefrLevel = typeof CEFR_LEVELS[number];

export const CEFR_NAME: Record<string, string> = {
    A1: "Breakthrough", A2: "Waystage",
    B1: "Threshold",    B2: "Vantage",  C1: "Effective",
};

export const CEFR_CHART_IDX: Record<string, number> = { A1: 0, A2: 1, B1: 2, B2: 3, C1: 4 };

export const CEFR_NUM: Record<string, number> = { A1: 1, A2: 2, B1: 3, B2: 4, C1: 5 };
export const NUM_CEFR: Record<number, string> = { 1: 'A1', 2: 'A2', 3: 'B1', 4: 'B2', 5: 'C1' };

export const CEFR_POSITION: Record<CefrLevel, number> = { A1: 10, A2: 30, B1: 50, B2: 70, C1: 90 };

export const CEFR_DESC: Record<string, string> = {
    A1: 'Beginner — basic everyday expressions and simple interactions.',
    A2: 'Elementary — building a solid foundation across topics.',
    B1: 'Intermediate — handling most situations in German-speaking areas.',
    B2: 'Upper-intermediate — conversations flow naturally and fluently.',
    C1: 'Advanced — near-native fluency across complex topics.',
};

export const CEFR_BADGE_CLASS: Record<string, string> = {
    A1: 'badge-a1', A2: 'badge-a2', B1: 'badge-b1', B2: 'badge-b2', C1: 'badge-c1',
};

export const CEFR_COLORS = [
    { level: 'A1', label: 'Beginner',          color: '#E9C46A', text: '#1A1A1A' },
    { level: 'A2', label: 'Elementary',         color: '#E76F51', text: '#FFFFFF' },
    { level: 'B1', label: 'Intermediate',       color: '#2A9D8F', text: '#FFFFFF' },
    { level: 'B2', label: 'Upper-Intermediate', color: '#457B9D', text: '#FFFFFF' },
    { level: 'C1', label: 'Advanced',           color: '#1D3557', text: '#FFFFFF' },
] as const;
