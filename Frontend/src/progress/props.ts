export interface ProgressPointProps {
  date:             string;
  predictedCefr:    string;
  mattr:            number;
  medianZipf:       number;
  avgDepDepth:      number;
  grammarErrorRate: number;
  coherenceMean:    number;
}

export interface ErrorPatternProps {
  errorCategory: string;
  ruleId:        string;
  count:         number;
  lastSeen:      string;
}

export interface BadgeProps {
  key:         string;
  title:       string;
  description: string;
  emoji:       string;
  unlockedAt:  string | null;
}

export interface GamificationProps {
  currentStreak:    number;
  longestStreak:    number;
  totalSubmissions: number;
  badges:           BadgeProps[];
}
