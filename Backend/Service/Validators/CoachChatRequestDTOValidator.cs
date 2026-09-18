using Domain.DTOs;
using FluentValidation;

namespace Service.Validators;

public class CoachChatRequestDTOValidator : AbstractValidator<CoachChatRequestDTO>
{
    public CoachChatRequestDTOValidator()
    {
        RuleFor(x => x.GermanText)
            .NotEmpty().WithMessage("German text is required.");

        RuleFor(x => x.UserMessage)
            .NotEmpty().WithMessage("Message is required.")
            .MaximumLength(2000).WithMessage("Message cannot exceed 2000 characters.");

        RuleFor(x => x.History)
            .Must(h => h.Count <= 50).WithMessage("Conversation history cannot exceed 50 messages.");

        RuleForEach(x => x.History)
            .ChildRules(msg =>
            {
                msg.RuleFor(m => m.Role)
                    .Must(r => r is "user" or "assistant")
                    .WithMessage("Role must be 'user' or 'assistant'.");

                msg.RuleFor(m => m.Content)
                    .NotEmpty().WithMessage("Message content cannot be empty.");
            });
    }
}
