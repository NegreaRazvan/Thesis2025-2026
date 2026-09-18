namespace Domain.DTOs;

public record VocabGameCategoryResponseDTO
{
    public string CategoryKey { get; init; } = "";
    public string DisplayName { get; init; } = "";
}
