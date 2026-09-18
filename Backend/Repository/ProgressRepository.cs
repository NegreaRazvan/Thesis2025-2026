using AutoMapper;
using Domain.DTOs;
using log4net;
using Microsoft.EntityFrameworkCore;
using Repository.Context;
using Repository.EFEntities;
using Service.Interfaces;

namespace Repository;

public class ProgressRepository(GermanAIContext context, IMapper mapper) : IProgressRepository
{
    private readonly GermanAIContext _context = context;
    private readonly IMapper _mapper = mapper;
    private readonly ILog _logger = LogManager.GetLogger(typeof(ProgressRepository));

    public async Task<List<ProgressPointDTO>> GetProgressAsync(string userId)
    {
        _logger.InfoFormat("Fetching progress timeline for user {0}", userId);

        return await _context.Submissions
            .Where(s => s.UserId == userId)
            .OrderBy(s => s.SubmittedAt)
            .Select(s => new ProgressPointDTO(
                s.SubmittedAt,
                s.PredictedCefr,
                s.Mattr,
                s.MedianZipf,
                s.AvgDepDepth,
                s.GrammarErrorRate,
                s.CoherenceMean))
            .ToListAsync();
    }

    public async Task<List<ErrorPatternDTO>> GetErrorPatternsAsync(string userId)
    {
        _logger.InfoFormat("Fetching error patterns for user {0}", userId);

        return await _context.ErrorPatterns
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.Count)
            .Take(10)
            .Select(p => new ErrorPatternDTO(p.ErrorCategory, p.RuleId, p.Count, p.LastSeen))
            .ToListAsync();
    }

    private static readonly List<(string Key, string Title, string Desc, string Emoji)> BadgeDefs =
    [
        ("first_submission", "First Steps",      "Submit your first text",                    "\U0001F423"),
        ("10_submissions",   "Getting Serious",   "Submit 10 texts",                          "\U0001F4DD"),
        ("25_submissions",   "Quarter Century",   "Submit 25 texts",                          "\U0001F3C5"),
        ("streak_3",         "On Fire",           "Maintain a 3-day writing streak",          "\U0001F525"),
        ("streak_7",         "Weekly Warrior",    "Maintain a 7-day writing streak",          "\u2694\uFE0F"),
        ("streak_30",        "Monthly Master",    "Maintain a 30-day writing streak",         "\U0001F451"),
        ("reached_a2",       "Level Up: A2",      "Reach CEFR level A2",                     "\U0001F7E2"),
        ("reached_b1",       "Level Up: B1",      "Reach CEFR level B1",                     "\U0001F535"),
        ("reached_b2",       "Level Up: B2",      "Reach CEFR level B2",                     "\U0001F7E0"),
        ("reached_c1",       "Level Up: C1",      "Reach CEFR level C1",                     "\U0001F7E3"),
    ];

    private static readonly Dictionary<string, int> CefrRank = new()
    {
        ["A1"] = 1, ["A2"] = 2, ["B1"] = 3, ["B2"] = 4, ["C1"] = 5
    };

    public async Task<GamificationDTO> GetGamificationAsync(string userId)
    {
        _logger.InfoFormat("Fetching gamification data for user {0}", userId);

        var submissions = await _context.Submissions
            .Where(s => s.UserId == userId)
            .OrderBy(s => s.SubmittedAt)
            .Select(s => new { s.SubmittedAt, s.PredictedCefr })
            .ToListAsync();

        var totalSubmissions = submissions.Count;

        var distinctDays = submissions
            .Select(s => DateOnly.FromDateTime(s.SubmittedAt))
            .Distinct()
            .OrderDescending()
            .ToList();

        var (currentStreak, longestStreak) = ComputeStreaks(distinctDays);

        var bestCefrRank = submissions.Count > 0
            ? submissions.Max(s => CefrRank.GetValueOrDefault(s.PredictedCefr, 0))
            : 0;

        var existing = await _context.UserAchievements
            .Where(a => a.UserId == userId)
            .ToDictionaryAsync(a => a.AchievementKey, a => a.UnlockedAt);

        var newAchievements = new List<UserAchievement>();
        var now = DateTime.UtcNow;

        bool ShouldUnlock(string key) => !existing.ContainsKey(key);

        if (totalSubmissions >= 1  && ShouldUnlock("first_submission"))
            newAchievements.Add(new UserAchievement { UserId = userId, AchievementKey = "first_submission", UnlockedAt = now });
        if (totalSubmissions >= 10 && ShouldUnlock("10_submissions"))
            newAchievements.Add(new UserAchievement { UserId = userId, AchievementKey = "10_submissions", UnlockedAt = now });
        if (totalSubmissions >= 25 && ShouldUnlock("25_submissions"))
            newAchievements.Add(new UserAchievement { UserId = userId, AchievementKey = "25_submissions", UnlockedAt = now });

        var peakStreak = Math.Max(currentStreak, longestStreak);
        if (peakStreak >= 3  && ShouldUnlock("streak_3"))
            newAchievements.Add(new UserAchievement { UserId = userId, AchievementKey = "streak_3", UnlockedAt = now });
        if (peakStreak >= 7  && ShouldUnlock("streak_7"))
            newAchievements.Add(new UserAchievement { UserId = userId, AchievementKey = "streak_7", UnlockedAt = now });
        if (peakStreak >= 30 && ShouldUnlock("streak_30"))
            newAchievements.Add(new UserAchievement { UserId = userId, AchievementKey = "streak_30", UnlockedAt = now });

        if (bestCefrRank >= 2 && ShouldUnlock("reached_a2"))
            newAchievements.Add(new UserAchievement { UserId = userId, AchievementKey = "reached_a2", UnlockedAt = now });
        if (bestCefrRank >= 3 && ShouldUnlock("reached_b1"))
            newAchievements.Add(new UserAchievement { UserId = userId, AchievementKey = "reached_b1", UnlockedAt = now });
        if (bestCefrRank >= 4 && ShouldUnlock("reached_b2"))
            newAchievements.Add(new UserAchievement { UserId = userId, AchievementKey = "reached_b2", UnlockedAt = now });
        if (bestCefrRank >= 5 && ShouldUnlock("reached_c1"))
            newAchievements.Add(new UserAchievement { UserId = userId, AchievementKey = "reached_c1", UnlockedAt = now });

        if (newAchievements.Count > 0)
        {
            _context.UserAchievements.AddRange(newAchievements);
            await _context.SaveChangesAsync();
            foreach (var a in newAchievements)
                existing[a.AchievementKey] = a.UnlockedAt;
        }

        var badges = BadgeDefs.Select(b => new BadgeDTO(
            b.Key,
            b.Title,
            b.Desc,
            b.Emoji,
            existing.TryGetValue(b.Key, out var dt) ? dt : null
        )).ToList();

        return new GamificationDTO(currentStreak, longestStreak, totalSubmissions, badges);
    }

    private static (int Current, int Longest) ComputeStreaks(List<DateOnly> sortedDaysDesc)
    {
        if (sortedDaysDesc.Count == 0) return (0, 0);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var current = 0;
        var longest = 0;
        var streak = 1;

        var diff = today.DayNumber - sortedDaysDesc[0].DayNumber;
        if (diff > 1)
        {
            current = 0;
            streak = 1;
            for (int i = 1; i < sortedDaysDesc.Count; i++)
            {
                if (sortedDaysDesc[i - 1].DayNumber - sortedDaysDesc[i].DayNumber == 1)
                    streak++;
                else
                    streak = 1;
                longest = Math.Max(longest, streak);
            }
            longest = Math.Max(longest, streak);
            return (current, longest);
        }

        current = 1;
        for (int i = 1; i < sortedDaysDesc.Count; i++)
        {
            if (sortedDaysDesc[i - 1].DayNumber - sortedDaysDesc[i].DayNumber == 1)
                current++;
            else
                break;
        }

        streak = 1;
        longest = 1;
        for (int i = 1; i < sortedDaysDesc.Count; i++)
        {
            if (sortedDaysDesc[i - 1].DayNumber - sortedDaysDesc[i].DayNumber == 1)
                streak++;
            else
                streak = 1;
            longest = Math.Max(longest, streak);
        }
        longest = Math.Max(longest, current);

        return (current, longest);
    }
}
