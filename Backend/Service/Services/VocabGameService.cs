using Controller.Interfaces;
using Domain.DTOs;
using Domain.Exceptions.Custom;
using log4net;
using Microsoft.Extensions.Configuration;
using Service.Interfaces;
using System.Text;
using System.Text.Json;

namespace Service.Services;

public class VocabGameService(HttpClient httpClient, IConfiguration config, IVocabGameRepository repo) : IVocabGameService
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly IConfiguration _config = config;
    private readonly IVocabGameRepository _repo = repo;
    private readonly ILog _logger = LogManager.GetLogger(typeof(VocabGameService));

    private static readonly JsonSerializerOptions _camelCase = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };
    private static readonly JsonSerializerOptions _caseInsensitive = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<VocabGameCategoryResponseDTO> GetCategoryAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/vocab-game/category");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<VocabGameCategoryResponseDTO>(json, _caseInsensitive)
                   ?? throw new ServiceUnavailableException("Python API returned null for category.");
        }
        catch (ServiceUnavailableException) { throw; }
        catch (Exception ex)
        {
            _logger.Error("Failed to get vocab game category.", ex);
            throw new ServiceUnavailableException("Vocab game service is unavailable.");
        }
    }

    public async Task<VocabGameBatchValidateResponseDTO> ValidateBatchAsync(VocabGameBatchValidateRequestDTO dto)
    {
        try
        {
            var payload = JsonSerializer.Serialize(dto, _camelCase);
            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/vocab-game/validate-batch", content);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<VocabGameBatchValidateResponseDTO>(json, _caseInsensitive)
                   ?? throw new ServiceUnavailableException("Python API returned null for batch validation.");
        }
        catch (ServiceUnavailableException) { throw; }
        catch (Exception ex)
        {
            _logger.Error("Failed to batch validate vocab game words.", ex);
            throw new ServiceUnavailableException("Vocab game service is unavailable.");
        }
    }

    public async Task<VocabGameSessionDTO> SaveSessionAsync(string userId, VocabGameSaveSessionDTO dto)
    {
        _logger.InfoFormat("Save vocab game session for user {0}", userId);
        return await _repo.SaveSessionAsync(userId, dto);
    }

    public async Task<List<VocabGameSessionDTO>> GetHistoryAsync(string userId)
    {
        _logger.InfoFormat("Get vocab game history for user {0}", userId);
        return await _repo.GetHistoryAsync(userId);
    }
}
