namespace Domain.DTOs;

public record ErrorItemDTO
{
    public string Category { get; init; } = "";
    public string RuleId { get; init; } = "";
    public string Message { get; init; } = "";
    public int OffsetStart { get; init; }
    public int OffsetEnd { get; init; }
    public string BadText { get; init; } = "";
    public List<string> Suggestions { get; init; } = [];
    public string Lemma { get; init; } = "";
    public string EnglishTranslation { get; init; } = "";
    public string Article { get; init; } = "";
    public string Plural { get; init; } = "";
}
