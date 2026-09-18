using Domain.DTOs;
using FluentValidation;

namespace Service.Validators;

public class UpsertFlashcardCardDTOValidator : AbstractValidator<UpsertFlashcardCardDTO>
{
    public UpsertFlashcardCardDTOValidator()
    {
        RuleFor(x => x.Front)
            .NotEmpty().WithMessage("Front text is required.")
            .MaximumLength(500).WithMessage("Front text cannot exceed 500 characters.");

        RuleFor(x => x.Back)
            .NotEmpty().WithMessage("Back text is required.")
            .MaximumLength(500).WithMessage("Back text cannot exceed 500 characters.");
    }
}
