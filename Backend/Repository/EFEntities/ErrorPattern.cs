namespace Repository.EFEntities;

public class ErrorPattern
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string UserId { get; set; }
    public AppUser User { get; set; } = null!;

    public required string ErrorCategory { get; set; }
    public required string RuleId { get; set; }
    public int Count { get; set; } = 1;
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;
}
