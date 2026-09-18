using Domain.DTOs;

namespace Service.Interfaces;

public interface ISubmissionRepository
{
    Task<SubmissionResponseDTO> AddAsync(string userId, string text, PythonPredictResponseDTO mlResult, string aiFeedback);
    Task<List<SubmissionSummaryDTO>> GetByUserAsync(string userId);
    Task<SubmissionResponseDTO?> GetByIdAsync(string userId, Guid submissionId);
    Task UpsertErrorPatternAsync(string userId, string category, string ruleId);
    Task DeleteAsync(string userId, Guid submissionId);
}