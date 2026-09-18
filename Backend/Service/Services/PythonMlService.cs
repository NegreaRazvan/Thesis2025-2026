using Controller.Interfaces;
using Domain.DTOs;
using Domain.Exceptions.Custom;
using log4net;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;

namespace Service.Services;

public class PythonMlService(HttpClient httpClient, IConfiguration config) : IPythonMlService
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly IConfiguration _config = config;
    private readonly ILog _logger = LogManager.GetLogger(typeof(PythonMlService));

    private static readonly JsonSerializerOptions _camelCase = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };
    private static readonly JsonSerializerOptions _caseInsensitive = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<PythonPredictResponseDTO> PredictAsync(string text)
    {
        _logger.InfoFormat("Calling Python ML API, text length {0}", text.Length);

        var payload = JsonSerializer.Serialize(new PythonPredictRequestDTO(text), _camelCase);
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync("/predict", content);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<PythonPredictResponseDTO>(json, _caseInsensitive)
                   ?? throw new ServiceUnavailableException("ML service returned an empty response.");
        }
        catch (Exception ex) when (ex is not ServiceUnavailableException)
        {
            _logger.Error("Python ML API call failed.", ex);
            throw new ServiceUnavailableException("ML service is unavailable. Please try again later.");
        }
    }
}
