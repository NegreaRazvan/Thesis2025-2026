namespace Repository.EFEntities;

public class CoachSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string UserId { get; set; }
    public AppUser User { get; set; } = null!;
    public required string GermanText { get; set; }
    public required string PredictedCefr { get; set; }
    public int TurnCount { get; set; }
    public string MessagesJson { get; set; } = "[]";
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
}
