using Domain.DTOs;
using FluentAssertions;
using Moq;
using Service.Interfaces;
using Service.Services;

namespace Backend.Tests.Services;

public class ProgressServiceTests
{
    private readonly Mock<IProgressRepository> _progressRepo = new();
    private readonly ProgressService _sut;

    private const string UserId = "user-123";

    public ProgressServiceTests()
    {
        _sut = new ProgressService(_progressRepo.Object);
    }

    [Fact]
    public async Task GetProgressAsync_ReturnsTimeline()
    {
        var timeline = new List<ProgressPointDTO>
        {
            new(DateTime.UtcNow.AddDays(-2), "A2", 0.65f, 3.2f, 2.1f, 0.08f, 0.72f),
            new(DateTime.UtcNow.AddDays(-1), "B1", 0.72f, 3.5f, 2.5f, 0.05f, 0.78f),
            new(DateTime.UtcNow, "B1", 0.74f, 3.6f, 2.6f, 0.04f, 0.80f)
        };
        _progressRepo.Setup(r => r.GetProgressAsync(UserId)).ReturnsAsync(timeline);

        var result = await _sut.GetProgressAsync(UserId);

        result.Should().HaveCount(3);
        result[2].PredictedCefr.Should().Be("B1");
    }

    [Fact]
    public async Task GetProgressAsync_NoSubmissions_ReturnsEmpty()
    {
        _progressRepo.Setup(r => r.GetProgressAsync(UserId)).ReturnsAsync(new List<ProgressPointDTO>());

        var result = await _sut.GetProgressAsync(UserId);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetErrorPatternsAsync_ReturnsPatterns()
    {
        var patterns = new List<ErrorPatternDTO>
        {
            new("grammar", "DE_CASE", 12, DateTime.UtcNow),
            new("spelling", "SPELLING_RULE", 5, DateTime.UtcNow)
        };
        _progressRepo.Setup(r => r.GetErrorPatternsAsync(UserId)).ReturnsAsync(patterns);

        var result = await _sut.GetErrorPatternsAsync(UserId);

        result.Should().HaveCount(2);
        result[0].Count.Should().Be(12);
    }

    [Fact]
    public async Task GetGamificationAsync_ReturnsGamificationData()
    {
        var badges = new List<BadgeDTO>
        {
            new("first_submission", "First Steps", "Submit your first text", "🎯", DateTime.UtcNow),
            new("streak_3", "3-Day Streak", "Practice 3 days in a row", "🔥", null)
        };
        var gamification = new GamificationDTO(2, 5, 15, badges);
        _progressRepo.Setup(r => r.GetGamificationAsync(UserId)).ReturnsAsync(gamification);

        var result = await _sut.GetGamificationAsync(UserId);

        result.CurrentStreak.Should().Be(2);
        result.LongestStreak.Should().Be(5);
        result.TotalSubmissions.Should().Be(15);
        result.Badges.Should().HaveCount(2);
        result.Badges[0].UnlockedAt.Should().NotBeNull();
        result.Badges[1].UnlockedAt.Should().BeNull();
    }

    [Fact]
    public async Task GenerateReportPdfAsync_ReturnsNonEmptyBytes()
    {
        var timeline = new List<ProgressPointDTO>
        {
            new(DateTime.UtcNow, "B1", 0.72f, 3.5f, 2.5f, 0.05f, 0.78f)
        };
        var patterns = new List<ErrorPatternDTO>
        {
            new("grammar", "DE_CASE", 3, DateTime.UtcNow)
        };
        _progressRepo.Setup(r => r.GetProgressAsync(UserId)).ReturnsAsync(timeline);
        _progressRepo.Setup(r => r.GetErrorPatternsAsync(UserId)).ReturnsAsync(patterns);

        var result = await _sut.GenerateReportPdfAsync(UserId, "TestUser");

        result.Should().NotBeEmpty();
        // PDF files start with %PDF
        System.Text.Encoding.ASCII.GetString(result[..4]).Should().Be("%PDF");
    }

    [Fact]
    public async Task GenerateReportPdfAsync_EmptyHistory_StillGeneratesPdf()
    {
        _progressRepo.Setup(r => r.GetProgressAsync(UserId)).ReturnsAsync(new List<ProgressPointDTO>());
        _progressRepo.Setup(r => r.GetErrorPatternsAsync(UserId)).ReturnsAsync(new List<ErrorPatternDTO>());

        var result = await _sut.GenerateReportPdfAsync(UserId, "TestUser");

        result.Should().NotBeEmpty();
    }
}
