using Domain.DTOs;

namespace Controller.Interfaces;

public interface IFlashcardSetService
{
    Task<List<FlashcardSetSummaryDTO>> GetAllAsync(string userId);
    Task<FlashcardSetDetailDTO> GetByIdAsync(string userId, Guid setId);
    Task<FlashcardSetSummaryDTO> CreateAsync(string userId, CreateFlashcardSetDTO dto);
    Task DeleteAsync(string userId, Guid setId);
    Task<FlashcardCardDTO> AddCardAsync(string userId, Guid setId, UpsertFlashcardCardDTO dto);
    Task<FlashcardCardDTO> UpdateCardAsync(string userId, Guid setId, UpsertFlashcardCardDTO dto);
    Task DeleteCardAsync(string userId, Guid setId, Guid cardId);
    Task<List<FlashcardCardDTO>> BulkAddCardsAsync(string userId, Guid setId, BulkAddFlashcardCardsDTO dto);
    Task MarkReviewedAsync(string userId, Guid setId);
}