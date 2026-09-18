namespace Domain.DTOs;

public record VocabGameBatchValidateRequestDTO
{
    public List<string> Words { get; init; } = [];
    public string CategoryKey { get; init; } = "";
}

public record VocabGameBatchValidateResponseDTO
{
    public List<WordResultDTO> Results { get; init; } = [];
    public int Score { get; init; }
}
