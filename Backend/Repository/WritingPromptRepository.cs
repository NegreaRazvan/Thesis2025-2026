using AutoMapper;
using Domain.DTOs;
using log4net;
using Microsoft.EntityFrameworkCore;
using Repository.Context;
using Service.Interfaces;

namespace Repository;

public class WritingPromptRepository(GermanAIContext context, IMapper mapper) : IWritingPromptRepository
{
    private readonly GermanAIContext _context = context;
    private readonly IMapper _mapper = mapper;
    private readonly ILog _logger = LogManager.GetLogger(typeof(WritingPromptRepository));

    public async Task<List<WritingPromptDTO>> GetByCefrLevelAsync(string level, int count = 3)
    {
        _logger.InfoFormat("Fetching {0} prompts for CEFR level {1}", count, level);

        var prompts = await _context.WritingPrompts
            .Where(p => p.CefrLevel == level.ToUpper())
            .OrderBy(_ => Guid.NewGuid())
            .Take(count)
            .ToListAsync();

        return _mapper.Map<List<WritingPromptDTO>>(prompts);
    }

    /// <summary>
    /// Adaptive recommendation algorithm:
    /// 1. Fetch the user's most recent CEFR level (from latest submission).
    /// 2. Fetch the user's top error category (from error_patterns table).
    /// 3. Try to find a prompt at that CEFR level whose topic_tag matches the
    ///    error category. Fall back to any prompt at that level if none match.
    /// 4. If the user has no submissions yet, return a random A2 prompt.
    /// </summary>
    public async Task<WritingPromptDTO?> GetRecommendedAsync(string userId)
    {
        _logger.InfoFormat("Generating recommended prompt for user {0}", userId);

        // 1. Latest CEFR level
        var latestCefr = await _context.Submissions
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.SubmittedAt)
            .Select(s => s.PredictedCefr)
            .FirstOrDefaultAsync() ?? "A2";

        // 2. Top error category
        var topCategory = await _context.ErrorPatterns
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.Count)
            .Select(p => p.ErrorCategory)
            .FirstOrDefaultAsync();

        // 3. Try topic-matched prompt; fall back to any prompt at level
        var query = _context.WritingPrompts
            .Where(p => p.CefrLevel == latestCefr);

        if (!string.IsNullOrEmpty(topCategory))
        {
            // Map error category → topic tag heuristic
            var topicHint = MapCategoryToTopic(topCategory);
            var matched = await query
                .Where(p => p.TopicTag != null && p.TopicTag.Contains(topicHint))
                .OrderBy(_ => Guid.NewGuid())
                .FirstOrDefaultAsync();

            if (matched is not null)
                return _mapper.Map<WritingPromptDTO>(matched);
        }

        // Fall back to any prompt at level
        var fallback = await query
            .OrderBy(_ => Guid.NewGuid())
            .FirstOrDefaultAsync();

        return fallback is null ? null : _mapper.Map<WritingPromptDTO>(fallback);
    }

    /// <summary>
    /// Heuristically maps LanguageTool error category names to prompt topic tags.
    /// Extend this map as you add more topic tags to your seeded prompts.
    /// </summary>
    private static string MapCategoryToTopic(string category) =>
        category.ToUpperInvariant() switch
        {
            var c when c.Contains("GRAMMAR")    => "grammar",
            var c when c.Contains("STYLE")      => "style",
            var c when c.Contains("SPELL")      => "vocabulary",
            var c when c.Contains("TYPO")       => "vocabulary",
            var c when c.Contains("PUNCT")      => "punctuation",
            var c when c.Contains("CASE")       => "grammar",
            var c when c.Contains("AGREEMENT")  => "grammar",
            var c when c.Contains("WORD_ORDER") => "syntax",
            _                                   => "general",
        };
}