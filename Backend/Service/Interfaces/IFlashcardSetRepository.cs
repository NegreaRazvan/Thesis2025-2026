using Domain.DTOs;

namespace Service.Interfaces;

public interface IFlashcardSetRepository
{
    Task<List<FlashcardSetSummaryDTO>> GetAllAsync(string userId);
    Task<FlashcardSetDetailDTO?> GetByIdAsync(string userId, Guid setId);
    Task<FlashcardSetSummaryDTO> CreateAsync(string userId, CreateFlashcardSetDTO dto);
    Task DeleteAsync(string userId, Guid setId);
    Task<FlashcardCardDTO> AddCardAsync(string userId, Guid setId, UpsertFlashcardCardDTO dto);
    Task<FlashcardCardDTO> UpdateCardAsync(string userId, Guid setId, UpsertFlashcardCardDTO dto);
    Task DeleteCardAsync(string userId, Guid setId, Guid cardId);
    Task<List<FlashcardCardDTO>> BulkAddCardsAsync(string userId, Guid setId, List<UpsertFlashcardCardDTO> cards);
    Task MarkReviewedAsync(string userId, Guid setId);
}