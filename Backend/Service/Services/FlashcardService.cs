using Controller.Interfaces;
using Domain.DTOs;
using log4net;
using Service.Interfaces;
using Service.Utils;

namespace Service.Services;

public class FlashcardService(IFlashcardRepository flashcardRepository, IAppValidatorFactory validator) : IFlashcardService
{
    private readonly IFlashcardRepository _flashcardRepository = flashcardRepository;
    private readonly IAppValidatorFactory _validator = validator;
    private readonly ILog _logger = LogManager.GetLogger(typeof(FlashcardService));

    public async Task<List<FlashcardResponseDTO>> GetDueCardsAsync(string userId)
    {
        _logger.DebugFormat("Due flashcards for user {0}", userId);
        return await _flashcardRepository.GetDueAsync(userId);
    }

    public async Task ReviewCardAsync(string userId, ReviewFlashcardDTO dto)
    {
        _logger.InfoFormat("Review card {0} for user {1}", dto.FlashcardId, userId);
        await ValidationHelper.ValidateAndThrowAsync(_validator.Get<ReviewFlashcardDTO>(), dto);
        await _flashcardRepository.ReviewAsync(userId, dto);
    }

    public async Task<FlashcardResponseDTO> CreateFromErrorAsync(string userId, Guid errorId)
    {
        _logger.InfoFormat("Create flashcard from error {0} for user {1}", errorId, userId);
        return await _flashcardRepository.CreateFromErrorAsync(userId, errorId);
    }
}