using Domain.DTOs;

namespace Controller.Interfaces;

public interface IProgressService
{
    Task<List<ProgressPointDTO>> GetProgressAsync(string userId);
    Task<List<ErrorPatternDTO>> GetErrorPatternsAsync(string userId);
    Task<byte[]> GenerateReportPdfAsync(string userId, string username);
    Task<GamificationDTO> GetGamificationAsync(string userId);
}
