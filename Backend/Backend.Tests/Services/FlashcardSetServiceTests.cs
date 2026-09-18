using Domain.DTOs;
using Domain.Exceptions.Custom;
using FluentAssertions;
using Moq;
using Service.Interfaces;
using Service.Services;

namespace Backend.Tests.Services;

public class FlashcardSetServiceTests
{
    private readonly Mock<IFlashcardSetRepository> _repo = new();
    private readonly FlashcardSetService _sut;

    private const string UserId = "user-123";

    public FlashcardSetServiceTests()
    {
        _sut = new FlashcardSetService(_repo.Object);
    }


    [Fact]
    public async Task GetAllAsync_ReturnsSets()
    {
        var sets = new List<FlashcardSetSummaryDTO>
        {
            new() { Id = Guid.NewGuid(), Name = "Animals", CardCount = 5 }
        };
        _repo.Setup(r => r.GetAllAsync(UserId)).ReturnsAsync(sets);

        var result = await _sut.GetAllAsync(UserId);

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Animals");
    }


    [Fact]
    public async Task GetByIdAsync_Found_ReturnsDetail()
    {
        var setId = Guid.NewGuid();
        var detail = new FlashcardSetDetailDTO
        {
            Id = setId,
            Name = "Animals",
            Cards = [new() { Id = Guid.NewGuid(), Front = "Hund", Back = "Dog" }]
        };
        _repo.Setup(r => r.GetByIdAsync(UserId, setId)).ReturnsAsync(detail);

        var result = await _sut.GetByIdAsync(UserId, setId);

        result.Name.Should().Be("Animals");
        result.Cards.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ThrowsNotFoundException()
    {
        var setId = Guid.NewGuid();
        _repo.Setup(r => r.GetByIdAsync(UserId, setId)).ReturnsAsync((FlashcardSetDetailDTO?)null);

        var act = () => _sut.GetByIdAsync(UserId, setId);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"Flashcard set {setId} not found.");
    }


    [Fact]
    public async Task CreateAsync_ReturnsCreatedSet()
    {
        var dto = new CreateFlashcardSetDTO("Colors", "German color words");
        var expected = new FlashcardSetSummaryDTO { Id = Guid.NewGuid(), Name = "Colors", Description = "German color words" };
        _repo.Setup(r => r.CreateAsync(UserId, dto)).ReturnsAsync(expected);

        var result = await _sut.CreateAsync(UserId, dto);

        result.Name.Should().Be("Colors");
    }


    [Fact]
    public async Task DeleteAsync_DelegatesToRepo()
    {
        var setId = Guid.NewGuid();

        await _sut.DeleteAsync(UserId, setId);

        _repo.Verify(r => r.DeleteAsync(UserId, setId), Times.Once);
    }


    [Fact]
    public async Task AddCardAsync_ReturnsNewCard()
    {
        var setId = Guid.NewGuid();
        var dto = new UpsertFlashcardCardDTO(null, "Rot", "Red");
        var expected = new FlashcardCardDTO { Id = Guid.NewGuid(), Front = "Rot", Back = "Red" };
        _repo.Setup(r => r.AddCardAsync(UserId, setId, dto)).ReturnsAsync(expected);

        var result = await _sut.AddCardAsync(UserId, setId, dto);

        result.Front.Should().Be("Rot");
        result.Back.Should().Be("Red");
    }


    [Fact]
    public async Task UpdateCardAsync_ReturnsUpdatedCard()
    {
        var setId = Guid.NewGuid();
        var cardId = Guid.NewGuid();
        var dto = new UpsertFlashcardCardDTO(cardId, "Blau", "Blue");
        var expected = new FlashcardCardDTO { Id = cardId, Front = "Blau", Back = "Blue" };
        _repo.Setup(r => r.UpdateCardAsync(UserId, setId, dto)).ReturnsAsync(expected);

        var result = await _sut.UpdateCardAsync(UserId, setId, dto);

        result.Front.Should().Be("Blau");
    }


    [Fact]
    public async Task DeleteCardAsync_DelegatesToRepo()
    {
        var setId = Guid.NewGuid();
        var cardId = Guid.NewGuid();

        await _sut.DeleteCardAsync(UserId, setId, cardId);

        _repo.Verify(r => r.DeleteCardAsync(UserId, setId, cardId), Times.Once);
    }


    [Fact]
    public async Task BulkAddCardsAsync_ReturnsAllCreatedCards()
    {
        var setId = Guid.NewGuid();
        var cards = new List<UpsertFlashcardCardDTO>
        {
            new(null, "Eins", "One"),
            new(null, "Zwei", "Two"),
            new(null, "Drei", "Three")
        };
        var dto = new BulkAddFlashcardCardsDTO(cards);
        var expected = cards.Select(c => new FlashcardCardDTO { Id = Guid.NewGuid(), Front = c.Front, Back = c.Back }).ToList();
        _repo.Setup(r => r.BulkAddCardsAsync(UserId, setId, cards)).ReturnsAsync(expected);

        var result = await _sut.BulkAddCardsAsync(UserId, setId, dto);

        result.Should().HaveCount(3);
    }
}
