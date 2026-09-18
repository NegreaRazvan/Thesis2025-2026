using Microsoft.AspNetCore.Identity;

namespace Repository.EFEntities;

public class AppUser : IdentityUser
{
    // IdentityUser already provides: Id (string), Email, UserName, PasswordHash
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLogin { get; set; }

    public string? DisplayName { get; set; }
    public string? Bio { get; set; }
    public string? TargetCefrLevel { get; set; }
    public string? NativeLanguage { get; set; }
    public DateOnly? LearningSince { get; set; }
    public byte[]? AvatarBytes { get; set; }
    public string? AvatarContentType { get; set; }

    public List<Submission> Submissions { get; set; } = [];
    public List<Flashcard> Flashcards { get; set; } = [];
    public List<ErrorPattern> ErrorPatterns { get; set; } = [];
}
