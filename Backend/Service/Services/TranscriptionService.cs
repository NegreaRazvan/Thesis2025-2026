using Controller.Interfaces;
using Domain.DTOs;
using log4net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace Service.Services;

public class TranscriptionService(IHttpClientFactory httpClientFactory, IConfiguration configuration) : ITranscriptionService
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly IConfiguration _configuration = configuration;
    private readonly ILog _logger = LogManager.GetLogger(typeof(TranscriptionService));

    private static readonly JsonSerializerOptions _snakeOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    public async Task<TranscriptionResultDTO> TranscribeAsync(IFormFile audio)
    {
        var pythonBaseUrl = _configuration["PythonApi:BaseUrl"]
            ?? throw new InvalidOperationException("PythonApi:BaseUrl not configured.");

        var client = _httpClientFactory.CreateClient("PythonProxy");

        using var formData = new MultipartFormDataContent();
        await using var stream = audio.OpenReadStream();
        using var streamContent = new StreamContent(stream);

        var baseContentType = (audio.ContentType ?? "audio/webm").Split(';')[0].Trim();
        streamContent.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue(baseContentType);

        var ext = baseContentType switch
        {
            "audio/mp4" => ".mp4",
            "audio/ogg" => ".ogg",
            "audio/wav" => ".wav",
            _ => ".webm",
        };
        formData.Add(streamContent, "file", $"recording{ext}");

        try
        {
            var response = await client.PostAsync($"{pythonBaseUrl}/transcribe", formData);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            _logger.DebugFormat("Python transcription: {0} bytes returned", json.Length);

            var result = JsonSerializer.Deserialize<PythonTranscriptionResponse>(json, _snakeOptions)
                         ?? throw new Domain.Exceptions.Custom.ServiceUnavailableException("Speech-to-text service returned an empty response.");

            return new TranscriptionResultDTO(
                result.Text ?? "",
                result.Language ?? "de",
                result.DurationSeconds);
        }
        catch (HttpRequestException ex)
        {
            _logger.Error("Python transcription service unavailable.", ex);
            throw new Domain.Exceptions.Custom.ServiceUnavailableException("Speech-to-text service is currently unavailable.");
        }
        catch (Exception ex) when (ex is not Domain.Exceptions.Custom.ServiceUnavailableException)
        {
            _logger.Error("Transcription failed.", ex);
            throw new Domain.Exceptions.Custom.ServiceUnavailableException("Speech-to-text processing failed. Please try again.");
        }
    }

    private sealed class PythonTranscriptionResponse
    {
        public string? Text { get; set; }
        public string? Language { get; set; }
        public float DurationSeconds { get; set; }
    }
}
