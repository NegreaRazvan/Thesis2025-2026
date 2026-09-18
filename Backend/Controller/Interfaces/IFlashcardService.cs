using Domain.DTOs;

namespace Controller.Interfaces;

public interface IFlashcardService
{
    Task<List<FlashcardResponseDTO>> GetDueCardsAsync(string userId);
    Task ReviewCardAsync(string userId, ReviewFlashcardDTO dto);
    Task<FlashcardResponseDTO> CreateFromErrorAsync(string userId, Guid errorId);
}
