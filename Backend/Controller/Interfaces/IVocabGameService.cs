using Domain.DTOs;

namespace Controller.Interfaces;

public interface IVocabGameService
{
    Task<VocabGameCategoryResponseDTO> GetCategoryAsync();
    Task<VocabGameBatchValidateResponseDTO> ValidateBatchAsync(VocabGameBatchValidateRequestDTO dto);
    Task<VocabGameSessionDTO> SaveSessionAsync(string userId, VocabGameSaveSessionDTO dto);
    Task<List<VocabGameSessionDTO>> GetHistoryAsync(string userId);
}
