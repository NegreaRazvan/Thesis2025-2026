using Domain.DTOs;

namespace Controller.Interfaces;

public interface IWritingPromptService
{
    Task<List<WritingPromptDTO>> GetByLevelAsync(string level);

    /// <summary>Returns one prompt tailored to the user's weak spots.</summary>
    Task<WritingPromptDTO?> GetRecommendedAsync(string userId);
}