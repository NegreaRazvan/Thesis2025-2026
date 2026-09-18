namespace Repository.EFEntities;

public class UserAchievement
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string UserId { get; set; }
    public AppUser User { get; set; } = null!;

    public required string AchievementKey { get; set; }
    public DateTime UnlockedAt { get; set; } = DateTime.UtcNow;
}
