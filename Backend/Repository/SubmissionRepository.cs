using AutoMapper;
using Domain.DTOs;
using Domain.Exceptions.Custom;
using log4net;
using Microsoft.EntityFrameworkCore;
using Repository.Context;
using Repository.EFEntities;
using Service.Interfaces;
using System.Text.Json;

namespace Repository;

public class SubmissionRepository(GermanAIContext context, IMapper mapper) : ISubmissionRepository
{
    private readonly GermanAIContext _context = context;
    private readonly IMapper _mapper = mapper;
    private readonly ILog _logger = LogManager.GetLogger(typeof(SubmissionRepository));

    public async Task<SubmissionResponseDTO> AddAsync(
        string userId,
        string text,
        PythonPredictResponseDTO mlResult,
        string aiFeedback)
    {
        _logger.InfoFormat("Saving submission for user {0}, predicted {1}", userId, mlResult.PredictedLevel);

        var f = mlResult.Features;

        var submission = new Submission
        {
            UserId = userId,
            TextContent = text,
            PredictedCefr = mlResult.PredictedLevel,
            ConfidenceJson = JsonSerializer.Serialize(mlResult.Confidence),
            AiFeedback = aiFeedback,
            NTokens = f.GetValueOrDefault("n_tokens"),
            Mattr = f.GetValueOrDefault("mattr"),
            MedianZipf = f.GetValueOrDefault("median_zipf"),
            AvgDepDepth = f.GetValueOrDefault("avg_dep_depth"),
            GrammarErrorRate = f.GetValueOrDefault("grammar_error_rate"),
            CoherenceMean = f.GetValueOrDefault("coherence_mean"),
            VocabSyntaxInteraction = f.GetValueOrDefault("vocab_syntax_interaction"),
            LexicalSophistication = f.GetValueOrDefault("lexical_sophistication"),
            Errors = mlResult.Errors.Select(e => new SubmissionError
            {
                ErrorCategory = e.Category,
                RuleId = e.RuleId,
                Message = e.Message,
                OffsetStart = e.OffsetStart,
                OffsetEnd = e.OffsetEnd,
                BadText = e.BadText,
                SuggestionsJson = JsonSerializer.Serialize(e.Suggestions),
                Lemma = e.Lemma,
                EnglishTranslation = e.EnglishTranslation,
                Article = e.Article,
                Plural = e.Plural,
            }).ToList(),
        };

        _context.Submissions.Add(submission);
        await _context.SaveChangesAsync();

        // Map the stored entity (without vocab candidates — those are not persisted)
        var response = _mapper.Map<SubmissionResponseDTO>(submission);

        // Attach vocab candidates from the live ML result — not stored in DB,
        // returned only on the initial submission response.
        var vocabCandidates = mlResult.VocabCandidates
            .Select(v => new VocabCandidateDTO
            {
                Token = v.Token,
                Lemma = v.Lemma,
                Pos = v.Pos,
                OffsetStart = v.OffsetStart,
                OffsetEnd = v.OffsetEnd,
                ActualProbability = v.ActualProbability,
                TopAlternatives = v.TopAlternatives,
                ZipfScore = v.ZipfScore,
                Reason = v.Reason,
                FlashcardFront = v.FlashcardFront,
                FlashcardBack = v.FlashcardBack,
            })
            .ToList();

        return response with
        {
            ShortTextWarning = mlResult.ShortTextWarning,
            VocabCandidates = vocabCandidates,
        };
    }

    public async Task<List<SubmissionSummaryDTO>> GetByUserAsync(string userId)
    {
        var submissions = await _context.Submissions
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.SubmittedAt)
            .ToListAsync();

        return submissions.Select(s => new SubmissionSummaryDTO
        {
            Id = s.Id,
            TextContent = s.TextContent,
            PredictedCefr = s.PredictedCefr,
            TokenCount = (int)s.NTokens,
            SubmittedAt = s.SubmittedAt,
        }).ToList();
    }

    public async Task<SubmissionResponseDTO?> GetByIdAsync(string userId, Guid submissionId)
    {
        var submission = await _context.Submissions
            .Include(s => s.Errors)
            .FirstOrDefaultAsync(s => s.Id == submissionId && s.UserId == userId);

        return submission is null ? null : _mapper.Map<SubmissionResponseDTO>(submission);
    }

    public async Task DeleteAsync(string userId, Guid submissionId)
    {
        var submission = await _context.Submissions
            .FirstOrDefaultAsync(s => s.Id == submissionId && s.UserId == userId)
            ?? throw new NotFoundException($"Submission {submissionId} not found.");
        _context.Submissions.Remove(submission);
        await _context.SaveChangesAsync();
    }

    public async Task UpsertErrorPatternAsync(string userId, string category, string ruleId)
    {
        var pattern = await _context.ErrorPatterns
            .FirstOrDefaultAsync(p => p.UserId == userId && p.RuleId == ruleId);

        if (pattern is null)
            _context.ErrorPatterns.Add(new ErrorPattern { UserId = userId, ErrorCategory = category, RuleId = ruleId, Count = 1 });
        else
        {
            pattern.Count++;
            pattern.LastSeen = DateTime.UtcNow;
        }
        await _context.SaveChangesAsync();
    }
}