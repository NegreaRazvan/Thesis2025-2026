namespace Domain.DTOs;

public record UpsertFlashcardCardDTO(Guid? Id, string Front, string Back);
public record BulkAddFlashcardCardsDTO(List<UpsertFlashcardCardDTO> Cards);
