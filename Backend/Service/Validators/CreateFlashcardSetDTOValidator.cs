using Domain.DTOs;
using FluentValidation;

namespace Service.Validators;

public class CreateFlashcardSetDTOValidator : AbstractValidator<CreateFlashcardSetDTO>
{
    public CreateFlashcardSetDTOValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Set name is required.")
            .MaximumLength(100).WithMessage("Set name cannot exceed 100 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.")
            .When(x => x.Description is not null);
    }
}
