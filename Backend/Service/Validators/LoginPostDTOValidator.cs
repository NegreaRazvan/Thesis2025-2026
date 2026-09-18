using Domain.DTOs;
using FluentValidation;

namespace Service.Validators;

public class LoginPostDTOValidator : AbstractValidator<LoginPostDTO>
{
    public LoginPostDTOValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.");
    }
}
