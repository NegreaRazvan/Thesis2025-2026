using Domain.DTOs;

namespace Service.Interfaces;

public interface IProgressRepository
{
    Task<List<ProgressPointDTO>> GetProgressAsync(string userId);
    Task<List<ErrorPatternDTO>> GetErrorPatternsAsync(string userId);
    Task<GamificationDTO> GetGamificationAsync(string userId);
}
