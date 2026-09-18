using Domain.DTOs;
using Domain.Exceptions.Custom;
using FluentAssertions;
using Moq;
using Moq.Protected;
using Service.Interfaces;
using Service.Services;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Backend.Tests.Services;

public class VocabGameServiceTests
{
    private readonly Mock<IVocabGameRepository> _repo = new();
    private readonly Mock<IConfiguration> _config = new();
    private const string UserId = "user-123";

    private VocabGameService CreateServiceWithHandler(HttpMessageHandler handler)
    {
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8001") };
        return new VocabGameService(client, _config.Object, _repo.Object);
    }

    private static HttpMessageHandler CreateMockHandler(HttpStatusCode status, string content)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(status)
            {
                Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json")
            });
        return handler.Object;
    }


    [Fact]
    public async Task GetCategoryAsync_Success_ReturnsCategory()
    {
        var category = new VocabGameCategoryResponseDTO { CategoryKey = "animals", DisplayName = "Tiere" };
        var json = JsonSerializer.Serialize(category);
        var handler = CreateMockHandler(HttpStatusCode.OK, json);
        var sut = CreateServiceWithHandler(handler);

        var result = await sut.GetCategoryAsync();

        result.CategoryKey.Should().Be("animals");
        result.DisplayName.Should().Be("Tiere");
    }

    [Fact]
    public async Task GetCategoryAsync_ApiDown_ThrowsServiceUnavailable()
    {
        var handler = CreateMockHandler(HttpStatusCode.InternalServerError, "error");
        var sut = CreateServiceWithHandler(handler);

        var act = () => sut.GetCategoryAsync();

        await act.Should().ThrowAsync<ServiceUnavailableException>();
    }


    [Fact]
    public async Task ValidateBatchAsync_Success_ReturnsResults()
    {
        var response = new VocabGameBatchValidateResponseDTO
        {
            Results = [new() { Word = "Hund", Valid = true, Similarity = 1.0f }],
            Score = 10
        };
        var json = JsonSerializer.Serialize(response);
        var handler = CreateMockHandler(HttpStatusCode.OK, json);
        var sut = CreateServiceWithHandler(handler);

        var dto = new VocabGameBatchValidateRequestDTO { Words = ["Hund"], CategoryKey = "animals" };
        var result = await sut.ValidateBatchAsync(dto);

        result.Score.Should().Be(10);
        result.Results.Should().HaveCount(1);
        result.Results[0].Word.Should().Be("Hund");
    }

    [Fact]
    public async Task ValidateBatchAsync_ApiDown_ThrowsServiceUnavailable()
    {
        var handler = CreateMockHandler(HttpStatusCode.InternalServerError, "error");
        var sut = CreateServiceWithHandler(handler);

        var dto = new VocabGameBatchValidateRequestDTO { Words = ["test"], CategoryKey = "animals" };
        var act = () => sut.ValidateBatchAsync(dto);

        await act.Should().ThrowAsync<ServiceUnavailableException>();
    }


    [Fact]
    public async Task SaveSessionAsync_DelegatesToRepo()
    {
        var dto = new VocabGameSaveSessionDTO
        {
            CategoryKey = "animals",
            DisplayName = "Tiere",
            Score = 25,
            ValidWords = ["Hund", "Katze"]
        };
        var expected = new VocabGameSessionDTO
        {
            Id = Guid.NewGuid(),
            CategoryKey = "animals",
            DisplayName = "Tiere",
            Score = 25,
            ValidWords = ["Hund", "Katze"],
            PlayedAt = DateTime.UtcNow
        };
        _repo.Setup(r => r.SaveSessionAsync(UserId, dto)).ReturnsAsync(expected);

        var client = new HttpClient { BaseAddress = new Uri("http://localhost:8001") };
        var sut = new VocabGameService(client, _config.Object, _repo.Object);
        var result = await sut.SaveSessionAsync(UserId, dto);

        result.Score.Should().Be(25);
        result.ValidWords.Should().Contain("Hund");
    }


    [Fact]
    public async Task GetHistoryAsync_ReturnsSessionHistory()
    {
        var sessions = new List<VocabGameSessionDTO>
        {
            new() { Id = Guid.NewGuid(), CategoryKey = "food", Score = 15, PlayedAt = DateTime.UtcNow }
        };
        _repo.Setup(r => r.GetHistoryAsync(UserId)).ReturnsAsync(sessions);

        var client = new HttpClient { BaseAddress = new Uri("http://localhost:8001") };
        var sut = new VocabGameService(client, _config.Object, _repo.Object);
        var result = await sut.GetHistoryAsync(UserId);

        result.Should().HaveCount(1);
        result[0].Score.Should().Be(15);
    }
}
