using Controller.Interfaces;
using Domain.DTOs;
using log4net;
using Service.Interfaces;

namespace Service.Services;

public class ProgressService(IProgressRepository progressRepository) : IProgressService
{
    private readonly IProgressRepository _progressRepository = progressRepository;
    private readonly ILog _logger = LogManager.GetLogger(typeof(ProgressService));

    public async Task<List<ProgressPointDTO>> GetProgressAsync(string userId)
    {
        _logger.DebugFormat("Progress timeline for user {0}", userId);
        return await _progressRepository.GetProgressAsync(userId);
    }

    public async Task<List<ErrorPatternDTO>> GetErrorPatternsAsync(string userId)
    {
        _logger.DebugFormat("Error patterns for user {0}", userId);
        return await _progressRepository.GetErrorPatternsAsync(userId);
    }

    public async Task<GamificationDTO> GetGamificationAsync(string userId)
    {
        _logger.DebugFormat("Gamification data for user {0}", userId);
        return await _progressRepository.GetGamificationAsync(userId);
    }

    public async Task<byte[]> GenerateReportPdfAsync(string userId, string username)
    {
        _logger.InfoFormat("Generating PDF report for user {0}", userId);
        var timeline = await _progressRepository.GetProgressAsync(userId);
        var patterns = await _progressRepository.GetErrorPatternsAsync(userId);
        return ProgressReportPdfBuilder.Build(timeline, patterns, username);
    }
}
