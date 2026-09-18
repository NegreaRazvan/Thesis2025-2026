namespace Domain.DTOs;

/// <summary>One message in a coaching conversation (role = "user" | "assistant").</summary>
public record CoachMessageDTO(string Role, string Content);

/// <summary>Request body: current text + full conversation history.</summary>
public record CoachChatRequestDTO
{
    public string GermanText { get; init; } = "";
    public string PredictedCefr { get; init; } = "";
    public List<CoachMessageDTO> History { get; init; } = [];
    public string UserMessage { get; init; } = "";
}

/// <summary>Response: the assistant's next message.</summary>
public record CoachChatResponseDTO(string Reply);

/// <summary>Request body to save a completed coaching session.</summary>
public record SaveCoachSessionDTO(
    string GermanText,
    string PredictedCefr,
    List<CoachMessageDTO> Messages
);

/// <summary>Summary of a saved coaching session for the history list.</summary>
public record CoachSessionSummaryDTO(
    Guid Id,
    string GermanText,
    string PredictedCefr,
    int TurnCount,
    DateTime StartedAt
);

/// <summary>Full coaching session with all messages.</summary>
public record CoachSessionDetailDTO(
    Guid Id,
    string GermanText,
    string PredictedCefr,
    int TurnCount,
    DateTime StartedAt,
    List<CoachMessageDTO> Messages
);
