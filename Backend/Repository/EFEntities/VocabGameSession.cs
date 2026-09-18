namespace Repository.EFEntities;

public class VocabGameSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string UserId { get; set; }
    public AppUser User { get; set; } = null!;
    public required string CategoryKey { get; set; }
    public required string DisplayName { get; set; }
    public int Score { get; set; }
    public string ValidWordsJson { get; set; } = "[]";
    public string MisspelledWordsJson { get; set; } = "[]";
    public DateTime PlayedAt { get; set; } = DateTime.UtcNow;
}
