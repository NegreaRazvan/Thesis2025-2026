namespace Repository.EFEntities;

public class FlashcardSet
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string UserId { get; set; }
    public AppUser User { get; set; } = null!;
    public required string Name { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<FlashcardCard> Cards { get; set; } = [];
}
