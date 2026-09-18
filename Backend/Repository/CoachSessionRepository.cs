using System.Text.Json;
using Domain.DTOs;
using log4net;
using Microsoft.EntityFrameworkCore;
using Repository.Context;
using Repository.EFEntities;
using Service.Interfaces;

namespace Repository;

public class CoachSessionRepository(GermanAIContext context) : ICoachSessionRepository
{
    private readonly ILog _logger = LogManager.GetLogger(typeof(CoachSessionRepository));
    private static readonly JsonSerializerOptions _jsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task<CoachSessionSummaryDTO> SaveAsync(string userId, SaveCoachSessionDTO dto)
    {
        var entity = new CoachSession
        {
            UserId       = userId,
            GermanText   = dto.GermanText,
            PredictedCefr = dto.PredictedCefr,
            TurnCount    = dto.Messages.Count(m => m.Role.Equals("user", StringComparison.OrdinalIgnoreCase)),
            MessagesJson = JsonSerializer.Serialize(dto.Messages, _jsonOpts),
        };

        context.CoachSessions.Add(entity);
        await context.SaveChangesAsync();
        _logger.InfoFormat("Saved coach session {0} for user {1}", entity.Id, userId);
        return ToDto(entity);
    }

    public async Task<List<CoachSessionSummaryDTO>> GetHistoryAsync(string userId) =>
        await context.CoachSessions
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.StartedAt)
            .Select(s => new CoachSessionSummaryDTO(s.Id, s.GermanText, s.PredictedCefr, s.TurnCount, s.StartedAt))
            .ToListAsync();

    public async Task<CoachSessionDetailDTO?> GetDetailAsync(string userId, Guid id)
    {
        var session = await context.CoachSessions
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);
        if (session is null) return null;

        var messages = JsonSerializer.Deserialize<List<CoachMessageDTO>>(session.MessagesJson, _jsonOpts) ?? [];
        return new CoachSessionDetailDTO(session.Id, session.GermanText, session.PredictedCefr, session.TurnCount, session.StartedAt, messages);
    }

    private static CoachSessionSummaryDTO ToDto(CoachSession s) =>
        new(s.Id, s.GermanText, s.PredictedCefr, s.TurnCount, s.StartedAt);
}
