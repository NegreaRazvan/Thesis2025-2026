using Domain.DTOs;

namespace Service.Interfaces;

public interface IFlashcardRepository
{
    Task<List<FlashcardResponseDTO>> GetDueAsync(string userId);
    Task ReviewAsync(string userId, ReviewFlashcardDTO dto);
    Task<FlashcardResponseDTO> CreateFromErrorAsync(string userId, Guid errorId);
    Task CreateFromSubmissionAsync(string userId, Guid submissionId);
}