namespace Domain.DTOs;

/// <summary>
/// A word detected by GermanBERT as a likely vocabulary error or unknown word.
/// Sent to the frontend to power the "Words to Learn" panel.
/// </summary>
public record VocabCandidateDTO
{
    public string Token { get; init; } = "";
    public string Lemma { get; init; } = "";
    public string Pos { get; init; } = "";
    public int OffsetStart { get; init; }
    public int OffsetEnd { get; init; }
    public float ActualProbability { get; init; }
    public List<string> TopAlternatives { get; init; } = [];
    public float ZipfScore { get; init; }
    public string Reason { get; init; } = "";
    public string FlashcardFront { get; init; } = "";
    public string FlashcardBack { get; init; } = "";
}
