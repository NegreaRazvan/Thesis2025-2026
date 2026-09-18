namespace Domain.DTOs;

public record UserProfileDTO
{
    public string Id { get; init; } = "";
    public string Username { get; init; } = "";
    public string Email { get; init; } = "";
    public string? DisplayName { get; init; }
    public string? Bio { get; init; }
    public string? TargetCefrLevel { get; init; }
    public string? NativeLanguage { get; init; }
    public DateOnly? LearningSince { get; init; }
    public bool HasAvatar { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record UpdateProfileDTO
{
    public string? Username { get; init; }
    public string? Email { get; init; }
    public string? DisplayName { get; init; }
    public string? Bio { get; init; }
    public string? TargetCefrLevel { get; init; }
    public string? NativeLanguage { get; init; }
    public DateOnly? LearningSince { get; init; }
}

public record ChangePasswordDTO(string CurrentPassword, string NewPassword);
