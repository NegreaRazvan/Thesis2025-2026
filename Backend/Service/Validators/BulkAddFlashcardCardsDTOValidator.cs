using Domain.DTOs;
using FluentValidation;

namespace Service.Validators;

public class BulkAddFlashcardCardsDTOValidator : AbstractValidator<BulkAddFlashcardCardsDTO>
{
    public BulkAddFlashcardCardsDTOValidator()
    {
        RuleFor(x => x.Cards)
            .NotEmpty().WithMessage("At least one card is required.");

        RuleForEach(x => x.Cards)
            .SetValidator(new UpsertFlashcardCardDTOValidator());
    }
}
