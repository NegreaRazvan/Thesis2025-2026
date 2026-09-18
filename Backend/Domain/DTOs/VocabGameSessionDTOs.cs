namespace Domain.DTOs;

public record VocabGameSaveSessionDTO
{
    public string CategoryKey { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public int Score { get; init; }
    public List<string> ValidWords { get; init; } = [];
    public List<MisspelledWordDTO> MisspelledWords { get; init; } = [];
}

public record VocabGameSessionDTO
{
    public Guid Id { get; init; }
    public string CategoryKey { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public int Score { get; init; }
    public List<string> ValidWords { get; init; } = [];
    public List<MisspelledWordDTO> MisspelledWords { get; init; } = [];
    public DateTime PlayedAt { get; init; }
}
