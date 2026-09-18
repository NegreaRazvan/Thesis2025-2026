using Domain.DTOs;

namespace Service.Interfaces;

public interface ICoachSessionRepository
{
    Task<CoachSessionSummaryDTO> SaveAsync(string userId, SaveCoachSessionDTO dto);
    Task<List<CoachSessionSummaryDTO>> GetHistoryAsync(string userId);
    Task<CoachSessionDetailDTO?> GetDetailAsync(string userId, Guid id);
}
