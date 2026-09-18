namespace Domain.DTOs;

public record BadgeDTO(
    string Key,
    string Title,
    string Description,
    string Emoji,
    DateTime? UnlockedAt);
