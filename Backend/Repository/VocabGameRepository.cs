using System.Text.Json;
using Domain.DTOs;
using log4net;
using Microsoft.EntityFrameworkCore;
using Repository.Context;
using Repository.EFEntities;
using Service.Interfaces;

namespace Repository;

public class VocabGameRepository(GermanAIContext context) : IVocabGameRepository
{
    private readonly GermanAIContext _context = context;
    private readonly ILog _logger = LogManager.GetLogger(typeof(VocabGameRepository));

    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public async Task<VocabGameSessionDTO> SaveSessionAsync(string userId, VocabGameSaveSessionDTO dto)
    {
        var entity = new VocabGameSession
        {
            UserId = userId,
            CategoryKey = dto.CategoryKey,
            DisplayName = dto.DisplayName,
            Score = dto.Score,
            ValidWordsJson = JsonSerializer.Serialize(dto.ValidWords, _jsonOpts),
            MisspelledWordsJson = JsonSerializer.Serialize(dto.MisspelledWords, _jsonOpts),
        };

        _context.VocabGameSessions.Add(entity);
        await _context.SaveChangesAsync();

        _logger.InfoFormat("Saved vocab game session {0} for user {1}", entity.Id, userId);

        return ToDto(entity);
    }

    public async Task<List<VocabGameSessionDTO>> GetHistoryAsync(string userId)
    {
        var sessions = await _context.VocabGameSessions
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.PlayedAt)
            .ToListAsync();

        return sessions.Select(ToDto).ToList();
    }

    private static VocabGameSessionDTO ToDto(VocabGameSession entity) => new()
    {
        Id = entity.Id,
        CategoryKey = entity.CategoryKey,
        DisplayName = entity.DisplayName,
        Score = entity.Score,
        ValidWords = JsonSerializer.Deserialize<List<string>>(entity.ValidWordsJson) ?? [],
        MisspelledWords = JsonSerializer.Deserialize<List<MisspelledWordDTO>>(entity.MisspelledWordsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [],
        PlayedAt = entity.PlayedAt,
    };
}
