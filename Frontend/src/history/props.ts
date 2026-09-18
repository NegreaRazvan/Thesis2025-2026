export interface SubmissionSummary {
  id: string;
  predictedCefr: string;
  tokenCount: number;
  submittedAt: string;
}

export interface SubmissionDetail {
  id: string;
  predictedCefr: string;
  confidence: Record<string, number>;
  aiFeedback: string;
  errors: Array<{
    id: string;
    errorCategory: string;
    message: string;
    suggestions: string[];
  }>;
  submittedAt: string;
}

