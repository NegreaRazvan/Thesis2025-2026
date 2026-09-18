using Domain.DTOs;
using FluentAssertions;
using Moq;
using Service.Interfaces;
using Service.Services;

namespace Backend.Tests.Services;

public class WritingPromptServiceTests
{
    private readonly Mock<IWritingPromptRepository> _repo = new();
    private readonly WritingPromptService _sut;

    public WritingPromptServiceTests()
    {
        _sut = new WritingPromptService(_repo.Object);
    }

    [Theory]
    [InlineData("A1")]
    [InlineData("A2")]
    [InlineData("B1")]
    [InlineData("B2")]
    [InlineData("C1")]
    public async Task GetByLevelAsync_ReturnsPromptsForLevel(string level)
    {
        var prompts = new List<WritingPromptDTO>
        {
            new() { Id = Guid.NewGuid(), CefrLevel = level, PromptDe = "Schreibe...", PromptEn = "Write..." },
            new() { Id = Guid.NewGuid(), CefrLevel = level, PromptDe = "Beschreibe...", PromptEn = "Describe..." }
        };
        _repo.Setup(r => r.GetByCefrLevelAsync(level, 3)).ReturnsAsync(prompts);

        var result = await _sut.GetByLevelAsync(level);

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(p => p.CefrLevel.Should().Be(level));
    }

    [Fact]
    public async Task GetByLevelAsync_NoPrompts_ReturnsEmpty()
    {
        _repo.Setup(r => r.GetByCefrLevelAsync("C2", 3)).ReturnsAsync(new List<WritingPromptDTO>());

        var result = await _sut.GetByLevelAsync("C2");

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRecommendedAsync_HasRecommendation_ReturnsPrompt()
    {
        var prompt = new WritingPromptDTO
        {
            Id = Guid.NewGuid(),
            CefrLevel = "B1",
            PromptDe = "Schreibe über dein Hobby.",
            PromptEn = "Write about your hobby.",
            TopicTag = "hobbies"
        };
        _repo.Setup(r => r.GetRecommendedAsync("user-1")).ReturnsAsync(prompt);

        var result = await _sut.GetRecommendedAsync("user-1");

        result.Should().NotBeNull();
        result!.TopicTag.Should().Be("hobbies");
    }

    [Fact]
    public async Task GetRecommendedAsync_NoRecommendation_ReturnsNull()
    {
        _repo.Setup(r => r.GetRecommendedAsync("user-1")).ReturnsAsync((WritingPromptDTO?)null);

        var result = await _sut.GetRecommendedAsync("user-1");

        result.Should().BeNull();
    }
}
