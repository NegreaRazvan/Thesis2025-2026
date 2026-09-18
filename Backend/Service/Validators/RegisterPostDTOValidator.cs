using Domain.DTOs;
using Domain.Utils;
using FluentValidation;
using Service.Interfaces;

namespace Service.Validators;

public class RegisterPostDTOValidator : AbstractValidator<RegisterPostDTO>
{
    private readonly IAuthRepository _authRepository;

    public RegisterPostDTOValidator(IAuthRepository authRepository)
    {
        _authRepository = authRepository;

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .MaximumLength(Constants.DefaultStringMaxLength)
            .EmailAddress().WithMessage("Invalid email format.")
            .MustAsync(async (email, _) =>
            {
                var existing = await _authRepository.GetByEmailAsync(email);
                return existing is null;
            }).WithMessage("Email is already registered.");

        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required.")
            .MinimumLength(3).WithMessage("Username must be at least 3 characters.")
            .MaximumLength(Constants.DefaultStringMaxLength)
            .Matches(@"^[a-zA-Z0-9_\-]+$").WithMessage("Username can only contain letters, numbers, underscores and hyphens.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one number.");
    }
}
