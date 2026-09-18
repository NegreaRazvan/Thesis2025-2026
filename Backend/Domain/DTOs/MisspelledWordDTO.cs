namespace Domain.DTOs;

public record MisspelledWordDTO
{
    public string Word { get; init; } = "";
    public List<string> Suggestions { get; init; } = [];
    public string Lemma { get; init; } = "";
    public string EnglishTranslation { get; init; } = "";
    public string Article { get; init; } = "";
    public string Plural { get; init; } = "";
}
