namespace Domain.DTOs;

public record FlashcardSetDetailDTO
{
    public Guid Id { get; init; }
    public string Name { get; init; } = "";
    public string? Description { get; init; }
    public List<FlashcardCardDTO> Cards { get; init; } = [];
    public DateTime CreatedAt { get; init; }
}
