namespace Domain.DTOs;

// Input DTO — positional fine
public record ReviewFlashcardDTO(Guid FlashcardId, int Quality);

// Output DTO — init-property style for AutoMapper
public record FlashcardResponseDTO
{
    public Guid Id { get; init; }
    public string Front { get; init; } = "";
    public string Back { get; init; } = "";
    public DateTime NextReview { get; init; }
    public int Repetitions { get; init; }
}