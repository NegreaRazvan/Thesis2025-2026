using Domain.DTOs;
using Domain.Exceptions.Custom;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Moq;
using Service.Interfaces;
using Service.Services;

namespace Backend.Tests.Services;

public class FlashcardServiceTests
{
    private readonly Mock<IFlashcardRepository> _flashcardRepo = new();
    private readonly Mock<IAppValidatorFactory> _validatorFactory = new();
    private readonly FlashcardService _sut;

    private const string UserId = "user-123";

    public FlashcardServiceTests()
    {
        var validator = new Mock<IValidator<ReviewFlashcardDTO>>();
        validator.Setup(v => v.ValidateAsync(It.IsAny<ReviewFlashcardDTO>(), default))
            .ReturnsAsync(new ValidationResult());
        _validatorFactory.Setup(f => f.Get<ReviewFlashcardDTO>()).Returns(validator.Object);

        _sut = new FlashcardService(_flashcardRepo.Object, _validatorFactory.Object);
    }

    [Fact]
    public async Task GetDueCardsAsync_ReturnsDueCards()
    {
        var cards = new List<FlashcardResponseDTO>
        {
            new() { Id = Guid.NewGuid(), Front = "Hund", Back = "Dog", NextReview = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Front = "Katze", Back = "Cat", NextReview = DateTime.UtcNow }
        };
        _flashcardRepo.Setup(r => r.GetDueAsync(UserId)).ReturnsAsync(cards);

        var result = await _sut.GetDueCardsAsync(UserId);

        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(cards);
    }

    [Fact]
    public async Task GetDueCardsAsync_NoDueCards_ReturnsEmpty()
    {
        _flashcardRepo.Setup(r => r.GetDueAsync(UserId)).ReturnsAsync(new List<FlashcardResponseDTO>());

        var result = await _sut.GetDueCardsAsync(UserId);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ReviewCardAsync_ValidInput_DelegatesToRepo()
    {
        var dto = new ReviewFlashcardDTO(Guid.NewGuid(), 4);

        await _sut.ReviewCardAsync(UserId, dto);

        _flashcardRepo.Verify(r => r.ReviewAsync(UserId, dto), Times.Once);
    }

    [Fact]
    public async Task ReviewCardAsync_ValidationFails_ThrowsEntityValidation()
    {
        var validator = new Mock<IValidator<ReviewFlashcardDTO>>();
        validator.Setup(v => v.ValidateAsync(It.IsAny<ReviewFlashcardDTO>(), default))
            .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure("Quality", "Must be 0-5") }));
        _validatorFactory.Setup(f => f.Get<ReviewFlashcardDTO>()).Returns(validator.Object);
        var sut = new FlashcardService(_flashcardRepo.Object, _validatorFactory.Object);

        var act = () => sut.ReviewCardAsync(UserId, new ReviewFlashcardDTO(Guid.NewGuid(), 10));

        await act.Should().ThrowAsync<EntityValidationException>();
    }

    [Fact]
    public async Task CreateFromErrorAsync_ReturnsCreatedFlashcard()
    {
        var errorId = Guid.NewGuid();
        var expected = new FlashcardResponseDTO
        {
            Id = Guid.NewGuid(),
            Front = "Fehler",
            Back = "Mistake",
            NextReview = DateTime.UtcNow,
            Repetitions = 0
        };
        _flashcardRepo.Setup(r => r.CreateFromErrorAsync(UserId, errorId)).ReturnsAsync(expected);

        var result = await _sut.CreateFromErrorAsync(UserId, errorId);

        result.Should().Be(expected);
    }
}
