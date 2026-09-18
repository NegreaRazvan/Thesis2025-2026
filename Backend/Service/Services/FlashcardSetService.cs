using Controller.Interfaces;
using Domain.DTOs;
using Domain.Exceptions.Custom;
using log4net;
using Service.Interfaces;

namespace Service.Services;

public class FlashcardSetService(IFlashcardSetRepository repo) : IFlashcardSetService
{
    private readonly IFlashcardSetRepository _repo = repo;
    private readonly ILog _logger = LogManager.GetLogger(typeof(FlashcardSetService));

    public async Task<List<FlashcardSetSummaryDTO>> GetAllAsync(string userId)
    {
        _logger.DebugFormat("Get all flashcard sets for user {0}", userId);
        return await _repo.GetAllAsync(userId);
    }

    public async Task<FlashcardSetDetailDTO> GetByIdAsync(string userId, Guid setId)
    {
        _logger.DebugFormat("Get flashcard set {0} for user {1}", setId, userId);
        return await _repo.GetByIdAsync(userId, setId)
            ?? throw new NotFoundException($"Flashcard set {setId} not found.");
    }

    public async Task<FlashcardSetSummaryDTO> CreateAsync(string userId, CreateFlashcardSetDTO dto)
    {
        _logger.InfoFormat("Create flashcard set for user {0}", userId);
        return await _repo.CreateAsync(userId, dto);
    }

    public async Task DeleteAsync(string userId, Guid setId)
    {
        _logger.InfoFormat("Delete flashcard set {0} for user {1}", setId, userId);
        await _repo.DeleteAsync(userId, setId);
    }

    public async Task<FlashcardCardDTO> AddCardAsync(string userId, Guid setId, UpsertFlashcardCardDTO dto)
    {
        _logger.InfoFormat("Add card to set {0} for user {1}", setId, userId);
        return await _repo.AddCardAsync(userId, setId, dto);
    }

    public async Task<FlashcardCardDTO> UpdateCardAsync(string userId, Guid setId, UpsertFlashcardCardDTO dto)
    {
        _logger.InfoFormat("Update card in set {0} for user {1}", setId, userId);
        return await _repo.UpdateCardAsync(userId, setId, dto);
    }

    public async Task DeleteCardAsync(string userId, Guid setId, Guid cardId)
    {
        _logger.InfoFormat("Delete card {0} from set {1} for user {2}", cardId, setId, userId);
        await _repo.DeleteCardAsync(userId, setId, cardId);
    }

    public async Task<List<FlashcardCardDTO>> BulkAddCardsAsync(string userId, Guid setId, BulkAddFlashcardCardsDTO dto)
    {
        _logger.InfoFormat("Bulk add {0} cards to set {1} for user {2}", dto.Cards.Count, setId, userId);
        return await _repo.BulkAddCardsAsync(userId, setId, dto.Cards);
    }

    public async Task MarkReviewedAsync(string userId, Guid setId)
    {
        _logger.InfoFormat("Mark set {0} reviewed for user {1}", setId, userId);
        await _repo.MarkReviewedAsync(userId, setId);
    }
}