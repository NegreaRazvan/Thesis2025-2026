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

public class FlashcardRepository(GermanAIContext context, IMapper mapper) : IFlashcardRepository
{
    private readonly GermanAIContext _context = context;
    private readonly IMapper         _mapper  = mapper;
    private readonly ILog _logger = LogManager.GetLogger(typeof(FlashcardRepository));

    public async Task<List<FlashcardResponseDTO>> GetDueAsync(string userId)
    {
        _logger.InfoFormat("Fetching due flashcards for user {0}", userId);

        var cards = await _context.Flashcards
            .Where(f => f.UserId == userId && f.NextReview <= DateTime.UtcNow)
            .OrderBy(f => f.NextReview)
            .Take(20)
            .ToListAsync();

        return _mapper.Map<List<FlashcardResponseDTO>>(cards);
    }

    public async Task ReviewAsync(string userId, ReviewFlashcardDTO dto)
    {
        _logger.InfoFormat("Reviewing flashcard {0} for user {1} with quality {2}",
            dto.FlashcardId, userId, dto.Quality);

        var card = await _context.Flashcards
            .FirstOrDefaultAsync(f => f.Id == dto.FlashcardId && f.UserId == userId)
            ?? throw new NotFoundException($"Flashcard {dto.FlashcardId} not found.");

        var q = Math.Clamp(dto.Quality, 0, 5);
        card.EaseFactor = Math.Max(1.3f,
            card.EaseFactor + (0.1f - (5 - q) * (0.08f + (5 - q) * 0.02f)));

        if (q < 3)
        {
            card.Repetitions  = 0;
            card.IntervalDays = 1;
        }
        else
        {
            card.Repetitions++;
            card.IntervalDays = card.Repetitions switch
            {
                1 => 1,
                2 => 6,
                _ => (int)(card.IntervalDays * card.EaseFactor),
            };
        }

        card.NextReview = DateTime.UtcNow.AddDays(card.IntervalDays);
        await _context.SaveChangesAsync();
    }

    public async Task<FlashcardResponseDTO> CreateFromErrorAsync(string userId, Guid errorId)
    {
        _logger.InfoFormat("Creating flashcard from error {0} for user {1}", errorId, userId);

        var error = await _context.SubmissionErrors
            .Include(e => e.Submission)
            .FirstOrDefaultAsync(e => e.Id == errorId && e.Submission.UserId == userId)
            ?? throw new NotFoundException($"Error {errorId} not found.");

        var suggestions = JsonSerializer.Deserialize<List<string>>(error.SuggestionsJson) ?? [];
        var suggestion  = suggestions.FirstOrDefault() ?? "(see correction)";

        var card = new Flashcard
        {
            UserId        = userId,
            Front         = $"Correct this: \"{error.BadText}\"",
            Back          = $"✓ {suggestion}\n\n{error.Message}",
            SourceErrorId = errorId,
        };

        _context.Flashcards.Add(card);
        await _context.SaveChangesAsync();

        return _mapper.Map<FlashcardResponseDTO>(card);
    }

    /// <summary>
    /// Auto-creates one flashcard per SubmissionError that has a non-empty BadText.
    /// Skips errors that already have a flashcard to avoid duplicates.
    /// Called in the background after every successful submission.
    /// </summary>
    public async Task CreateFromSubmissionAsync(string userId, Guid submissionId)
    {
        _logger.InfoFormat("Auto-creating flashcards for submission {0}", submissionId);

        var errors = await _context.SubmissionErrors
            .Where(e => e.SubmissionId == submissionId && e.BadText != null && e.BadText != "")
            .ToListAsync();

        foreach (var error in errors)
        {
            var exists = await _context.Flashcards
                .AnyAsync(f => f.SourceErrorId == error.Id && f.UserId == userId);
            if (exists) continue;

            var suggestions = JsonSerializer.Deserialize<List<string>>(error.SuggestionsJson) ?? [];
            var suggestion  = suggestions.FirstOrDefault() ?? "(no suggestion)";

            _context.Flashcards.Add(new Flashcard
            {
                UserId        = userId,
                Front         = $"Correct this: \"{error.BadText}\"",
                Back          = $"✓ {suggestion}\n\n{error.Message}",
                SourceErrorId = error.Id,
            });
        }

        if (errors.Any())
            await _context.SaveChangesAsync();
    }
}