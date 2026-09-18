using Domain.DTOs;

namespace Controller.Interfaces;

public interface ICoachService
{
    Task<CoachChatResponseDTO> ChatAsync(CoachChatRequestDTO request);
    Task<CoachSessionSummaryDTO> SaveSessionAsync(string userId, SaveCoachSessionDTO dto);
    Task<List<CoachSessionSummaryDTO>> GetSessionsAsync(string userId);
    Task<CoachSessionDetailDTO?> GetSessionDetailAsync(string userId, Guid id);
}
