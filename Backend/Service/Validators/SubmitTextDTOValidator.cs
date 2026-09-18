using Domain.DTOs;
using Domain.Utils;
using FluentValidation;

namespace Service.Validators;

public class SubmitTextDTOValidator : AbstractValidator<SubmitTextDTO>
{
    public SubmitTextDTOValidator()
    {
        RuleFor(x => x.Text)
            .NotEmpty().WithMessage("Text cannot be empty.")
            .MinimumLength(Constants.MinTextTokens)
                .WithMessage($"Text must be at least {Constants.MinTextTokens} characters.")
            .MaximumLength(Constants.MaxTextLength)
                .WithMessage($"Text cannot exceed {Constants.MaxTextLength} characters.");
    }
}
