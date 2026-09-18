namespace Domain.DTOs;

public record WordResultDTO
{
    public string Word { get; init; } = "";
    public bool Valid { get; init; }
    public float Similarity { get; init; }
    public string Reason { get; init; } = "";
    public bool Misspelled { get; init; }
    public List<string> Suggestions { get; init; } = [];
    public string Lemma { get; init; } = "";
    public string EnglishTranslation { get; init; } = "";
    public string Article { get; init; } = "";
    public string Plural { get; init; } = "";
}
