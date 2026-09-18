export interface ErrorItemProps {
    category:           string;
    ruleId:             string;
    message:            string;
    offsetStart:        number;
    offsetEnd:          number;
    badText:            string;
    suggestions:        string[];
    lemma:              string;
    englishTranslation: string;
    article:            string;
    plural:             string;
}

export interface FeatureSnapshotProps {
    nTokens:               number;
    mattr:                 number;
    medianZipf:            number;
    avgDepDepth:           number;
    grammarErrorRate:      number;
    coherenceMean:         number;
    vocabSyntaxInteraction:number;
    lexicalSophistication: number;
}

export interface VocabCandidateProps {
    token:              string;
    lemma:              string;
    pos:                string;
    offsetStart:        number;
    offsetEnd:          number;
    actualProbability:  number;
    topAlternatives:    string[];
    zipfScore:          number;
    reason:             string;
    flashcardFront:     string;
    flashcardBack:      string;
}

export interface SubmissionResponseProps {
    id:               string;
    textContent:      string;
    predictedCefr:    string;
    confidence:       Record<string, number>;
    aiFeedback:       string;
    features:         FeatureSnapshotProps;
    errors:           ErrorItemProps[];
    vocabCandidates:  VocabCandidateProps[];
    shortTextWarning: boolean;
    submittedAt:      string;
}

export interface SubmissionSummaryProps {
    id:            string;
    textContent:   string;
    predictedCefr: string;
    tokenCount:    number;
    submittedAt:   string;
}

