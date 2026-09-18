using Controller.Interfaces;
using Domain.DTOs;
using log4net;
using Service.Interfaces;

namespace Service.Services;

public class WritingPromptService(IWritingPromptRepository promptRepository) : IWritingPromptService
{
    private readonly IWritingPromptRepository _promptRepository = promptRepository;
    private readonly ILog _logger = LogManager.GetLogger(typeof(WritingPromptService));

    public async Task<List<WritingPromptDTO>> GetByLevelAsync(string level)
    {
        _logger.DebugFormat("Get writing prompts for level {0}", level);
        return await _promptRepository.GetByCefrLevelAsync(level);
    }

    public async Task<WritingPromptDTO?> GetRecommendedAsync(string userId)
    {
        _logger.DebugFormat("Get recommended prompt for user {0}", userId);
        return await _promptRepository.GetRecommendedAsync(userId);
    }
}