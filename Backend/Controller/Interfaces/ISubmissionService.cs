using Domain.DTOs;

namespace Controller.Interfaces;

public interface ISubmissionService
{
    Task<SubmissionResponseDTO> SubmitAsync(string userId, SubmitTextDTO dto);
    Task<List<SubmissionSummaryDTO>> GetHistoryAsync(string userId);
    Task<SubmissionResponseDTO> GetByIdAsync(string userId, Guid submissionId);
    Task DeleteAsync(string userId, Guid submissionId);
}