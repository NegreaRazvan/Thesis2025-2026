using Domain.DTOs;
using FluentValidation;

namespace Service.Validators;

public class UpdateProfileDTOValidator : AbstractValidator<UpdateProfileDTO>
{
    private static readonly string[] ValidCefrLevels = ["A1", "A2", "B1", "B2", "C1", "C2"];

    public UpdateProfileDTOValidator()
    {
        RuleFor(x => x.Username)
            .MinimumLength(3).WithMessage("Username must be at least 3 characters.")
            .MaximumLength(50)
            .Matches(@"^[a-zA-Z0-9_\-]+$").WithMessage("Username can only contain letters, numbers, underscores and hyphens.")
            .When(x => x.Username is not null);

        RuleFor(x => x.Email)
            .MaximumLength(256)
            .EmailAddress().WithMessage("Invalid email format.")
            .When(x => x.Email is not null);

        RuleFor(x => x.DisplayName)
            .MaximumLength(80)
            .When(x => x.DisplayName is not null);

        RuleFor(x => x.Bio)
            .MaximumLength(500)
            .When(x => x.Bio is not null);

        RuleFor(x => x.TargetCefrLevel)
            .Must(l => ValidCefrLevels.Contains(l!.ToUpperInvariant()))
            .WithMessage("Target CEFR level must be one of: A1, A2, B1, B2, C1, C2.")
            .When(x => x.TargetCefrLevel is not null);

        RuleFor(x => x.NativeLanguage)
            .MaximumLength(50)
            .When(x => x.NativeLanguage is not null);
    }
}
