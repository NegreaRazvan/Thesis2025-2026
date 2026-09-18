using Domain.DTOs;

namespace Service.Interfaces;

public interface IWritingPromptRepository
{
    Task<List<WritingPromptDTO>> GetByCefrLevelAsync(string level, int count = 3);

    /// <summary>
    /// Returns a single prompt targeted at the user's current weak spots.
    /// Uses the most frequent error category + latest CEFR level to pick
    /// the most pedagogically relevant prompt.
    /// </summary>
    Task<WritingPromptDTO?> GetRecommendedAsync(string userId);
}