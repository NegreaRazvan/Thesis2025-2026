namespace Domain.DTOs;

public record GamificationDTO(
    int CurrentStreak,
    int LongestStreak,
    int TotalSubmissions,
    List<BadgeDTO> Badges);
