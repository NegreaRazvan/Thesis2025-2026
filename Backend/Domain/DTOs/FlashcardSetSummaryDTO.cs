namespace Domain.DTOs;

public record FlashcardSetSummaryDTO
{
    public Guid Id { get; init; }
    public string Name { get; init; } = "";
    public string? Description { get; init; }
    public int CardCount { get; init; }
    public int DueCount { get; init; }
    public int NewCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? LastReviewedAt { get; init; }
}
