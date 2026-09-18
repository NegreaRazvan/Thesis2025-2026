namespace Domain.DTOs;

public record FlashcardCardDTO
{
    public Guid Id { get; init; }
    public string Front { get; init; } = "";
    public string Back { get; init; } = "";
}
