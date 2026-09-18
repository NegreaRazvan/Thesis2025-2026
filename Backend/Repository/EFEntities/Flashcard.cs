namespace Repository.EFEntities;

public class Flashcard
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string UserId { get; set; }
    public AppUser User { get; set; } = null!;

    public required string Front { get; set; }   // wrong form / context sentence
    public required string Back { get; set; }    // correction + explanation
    public Guid? SourceErrorId { get; set; }

    // SM-2 spaced repetition fields
    public float EaseFactor { get; set; } = 2.5f;
    public int IntervalDays { get; set; } = 1;
    public int Repetitions { get; set; } = 0;
    public DateTime NextReview { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
