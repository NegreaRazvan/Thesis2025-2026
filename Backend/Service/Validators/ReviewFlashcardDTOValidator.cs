using Domain.DTOs;
using FluentValidation;

namespace Service.Validators;

public class ReviewFlashcardDTOValidator : AbstractValidator<ReviewFlashcardDTO>
{
    public ReviewFlashcardDTOValidator()
    {
        RuleFor(x => x.FlashcardId)
            .NotEmpty().WithMessage("FlashcardId is required.");

        RuleFor(x => x.Quality)
            .InclusiveBetween(0, 5).WithMessage("Quality must be between 0 and 5 (SM-2 scale).");
    }
}
