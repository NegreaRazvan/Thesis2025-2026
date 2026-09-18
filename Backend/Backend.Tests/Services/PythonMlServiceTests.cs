using Domain.DTOs;
using Domain.Exceptions.Custom;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using Service.Services;
using System.Net;
using System.Text.Json;

namespace Backend.Tests.Services;

public class PythonMlServiceTests
{
    private readonly Mock<IConfiguration> _config = new();

    private PythonMlService CreateServiceWithHandler(HttpMessageHandler handler)
    {
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8001") };
        return new PythonMlService(client, _config.Object);
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
    public async Task PredictAsync_Success_ReturnsParsedResponse()
    {
        var mlResponse = new PythonPredictResponseDTO
        {
            PredictedLevel = "B1",
            Confidence = new() { ["A1"] = 0.05f, ["A2"] = 0.10f, ["B1"] = 0.70f, ["B2"] = 0.10f, ["C1"] = 0.05f },
            Features = new() { ["mattr"] = 0.72f, ["grammar_error_rate"] = 0.04f },
            Errors = [new() { Category = "grammar", RuleId = "DE_CASE", Message = "Wrong case", BadText = "den" }],
            VocabCandidates = []
        };
        var json = JsonSerializer.Serialize(mlResponse);
        var handler = CreateMockHandler(HttpStatusCode.OK, json);
        var sut = CreateServiceWithHandler(handler);

        var result = await sut.PredictAsync("Ich lerne Deutsch seit zwei Jahren.");

        result.PredictedLevel.Should().Be("B1");
        result.Confidence.Should().ContainKey("B1");
        result.Confidence["B1"].Should().BeApproximately(0.70f, 0.01f);
        result.Errors.Should().HaveCount(1);
        result.Errors[0].RuleId.Should().Be("DE_CASE");
    }

    [Fact]
    public async Task PredictAsync_ServerError_ThrowsServiceUnavailable()
    {
        var handler = CreateMockHandler(HttpStatusCode.InternalServerError, "Internal Server Error");
        var sut = CreateServiceWithHandler(handler);

        var act = () => sut.PredictAsync("some text");

        await act.Should().ThrowAsync<ServiceUnavailableException>()
            .WithMessage("ML service is unavailable*");
    }

    [Fact]
    public async Task PredictAsync_Timeout_ThrowsServiceUnavailable()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Request timed out"));
        var sut = CreateServiceWithHandler(handler.Object);

        var act = () => sut.PredictAsync("some text");

        await act.Should().ThrowAsync<ServiceUnavailableException>();
    }

    [Fact]
    public async Task PredictAsync_ConnectionRefused_ThrowsServiceUnavailable()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));
        var sut = CreateServiceWithHandler(handler.Object);

        var act = () => sut.PredictAsync("some text");

        await act.Should().ThrowAsync<ServiceUnavailableException>();
    }

    [Fact]
    public async Task PredictAsync_EmptyResponse_ThrowsServiceUnavailable()
    {
        var handler = CreateMockHandler(HttpStatusCode.OK, "null");
        var sut = CreateServiceWithHandler(handler);

        var act = () => sut.PredictAsync("some text");

        await act.Should().ThrowAsync<ServiceUnavailableException>();
    }
}
