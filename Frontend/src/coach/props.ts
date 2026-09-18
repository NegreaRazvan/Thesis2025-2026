export interface CoachMessage {
    role: "user" | "assistant";
    content: string;
    ts?: string;
}

export interface CoachChatRequest {
    germanText: string;
    predictedCefr: string;
    history: CoachMessage[];
    userMessage: string;
}

export interface CoachChatResponse {
    reply: string;
}

export interface SaveCoachSession {
    germanText: string;
    predictedCefr: string;
    messages: CoachMessage[];
}

export interface CoachSessionSummary {
    id: string;
    germanText: string;
    predictedCefr: string;
    turnCount: number;
    startedAt: string;
}

export interface CoachSessionDetail extends CoachSessionSummary {
    messages: CoachMessage[];
}
