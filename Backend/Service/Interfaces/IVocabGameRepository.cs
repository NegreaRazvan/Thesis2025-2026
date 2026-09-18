using Domain.DTOs;

namespace Service.Interfaces;

public interface IVocabGameRepository
{
    Task<VocabGameSessionDTO> SaveSessionAsync(string userId, VocabGameSaveSessionDTO dto);
    Task<List<VocabGameSessionDTO>> GetHistoryAsync(string userId);
}
