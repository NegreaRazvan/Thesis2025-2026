namespace Repository.EFEntities;

public class FlashcardCard
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid FlashcardSetId { get; set; }
    public FlashcardSet FlashcardSet { get; set; } = null!;
    public required string Front { get; set; }
    public required string Back { get; set; }
    public DateTime? ReviewedAt { get; set; }
}
