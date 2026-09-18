using Domain.DTOs;
using Domain.Exceptions.Custom;
using log4net;
using Microsoft.EntityFrameworkCore;
using Repository.Context;
using Repository.EFEntities;
using Service.Interfaces;

namespace Repository;

public class FlashcardSetRepository(GermanAIContext context) : IFlashcardSetRepository
{
    private readonly GermanAIContext _context = context;
    private readonly ILog _logger = LogManager.GetLogger(typeof(FlashcardSetRepository));

    public async Task<List<FlashcardSetSummaryDTO>> GetAllAsync(string userId)
    {
        var sets = await _context.FlashcardSets
            .Where(s => s.UserId == userId)
            .Include(s => s.Cards)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

        var today = DateTime.UtcNow.Date;
        return sets.Select(s => new FlashcardSetSummaryDTO
        {
            Id = s.Id,
            Name = s.Name,
            Description = s.Description,
            CardCount = s.Cards.Count,
            NewCount = s.Cards.Count(c => c.ReviewedAt == null),
            DueCount = s.Cards.Count(c => c.ReviewedAt != null && c.ReviewedAt.Value.Date < today),
            CreatedAt = s.CreatedAt,
            LastReviewedAt = s.Cards.Any(c => c.ReviewedAt != null) ? s.Cards.Max(c => c.ReviewedAt) : null,
        }).ToList();
    }

    public async Task MarkReviewedAsync(string userId, Guid setId)
    {
        var set = await _context.FlashcardSets
            .Include(s => s.Cards)
            .FirstOrDefaultAsync(s => s.Id == setId && s.UserId == userId)
            ?? throw new NotFoundException($"Flashcard set {setId} not found.");

        var now = DateTime.UtcNow;
        foreach (var card in set.Cards)
            card.ReviewedAt = now;

        await _context.SaveChangesAsync();
    }

    public async Task<FlashcardSetDetailDTO?> GetByIdAsync(string userId, Guid setId)
    {
        var set = await _context.FlashcardSets
            .Include(s => s.Cards)
            .FirstOrDefaultAsync(s => s.Id == setId && s.UserId == userId);

        if (set is null) return null;

        return new FlashcardSetDetailDTO
        {
            Id = set.Id,
            Name = set.Name,
            Description = set.Description,
            CreatedAt = set.CreatedAt,
            Cards = set.Cards.Select(c => new FlashcardCardDTO { Id = c.Id, Front = c.Front, Back = c.Back }).ToList(),
        };
    }

    public async Task<FlashcardSetSummaryDTO> CreateAsync(string userId, CreateFlashcardSetDTO dto)
    {
        var set = new FlashcardSet { UserId = userId, Name = dto.Name, Description = dto.Description };
        _context.FlashcardSets.Add(set);
        await _context.SaveChangesAsync();

        return new FlashcardSetSummaryDTO
        {
            Id = set.Id,
            Name = set.Name,
            Description = set.Description,
            CardCount = 0,
            CreatedAt = set.CreatedAt,
        };
    }

    public async Task DeleteAsync(string userId, Guid setId)
    {
        var set = await _context.FlashcardSets
            .FirstOrDefaultAsync(s => s.Id == setId && s.UserId == userId)
            ?? throw new NotFoundException($"Flashcard set {setId} not found.");
        _context.FlashcardSets.Remove(set);
        await _context.SaveChangesAsync();
    }

    public async Task<FlashcardCardDTO> AddCardAsync(string userId, Guid setId, UpsertFlashcardCardDTO dto)
    {
        _ = await _context.FlashcardSets
            .FirstOrDefaultAsync(s => s.Id == setId && s.UserId == userId)
            ?? throw new NotFoundException($"Flashcard set {setId} not found.");

        var card = new FlashcardCard { FlashcardSetId = setId, Front = dto.Front, Back = dto.Back };
        _context.FlashcardCards.Add(card);
        await _context.SaveChangesAsync();
        return new FlashcardCardDTO { Id = card.Id, Front = card.Front, Back = card.Back };
    }

    public async Task<FlashcardCardDTO> UpdateCardAsync(string userId, Guid setId, UpsertFlashcardCardDTO dto)
    {
        if (dto.Id is null) throw new EntityValidationException("Card Id required for update.");

        var card = await _context.FlashcardCards
            .Include(c => c.FlashcardSet)
            .FirstOrDefaultAsync(c => c.Id == dto.Id && c.FlashcardSetId == setId && c.FlashcardSet.UserId == userId)
            ?? throw new NotFoundException($"Card {dto.Id} not found.");

        card.Front = dto.Front;
        card.Back = dto.Back;
        await _context.SaveChangesAsync();
        return new FlashcardCardDTO { Id = card.Id, Front = card.Front, Back = card.Back };
    }

    public async Task DeleteCardAsync(string userId, Guid setId, Guid cardId)
    {
        var card = await _context.FlashcardCards
            .Include(c => c.FlashcardSet)
            .FirstOrDefaultAsync(c => c.Id == cardId && c.FlashcardSetId == setId && c.FlashcardSet.UserId == userId)
            ?? throw new NotFoundException($"Card {cardId} not found.");
        _context.FlashcardCards.Remove(card);
        await _context.SaveChangesAsync();
    }

    public async Task<List<FlashcardCardDTO>> BulkAddCardsAsync(string userId, Guid setId, List<UpsertFlashcardCardDTO> cards)
    {
        _ = await _context.FlashcardSets
            .FirstOrDefaultAsync(s => s.Id == setId && s.UserId == userId)
            ?? throw new NotFoundException($"Flashcard set {setId} not found.");

        var entities = cards.Select(c => new FlashcardCard
        {
            FlashcardSetId = setId,
            Front = c.Front.Trim().Trim('"'),
            Back = c.Back.Trim().Trim('"'),
        }).ToList();

        _context.FlashcardCards.AddRange(entities);
        await _context.SaveChangesAsync();
        return entities.Select(c => new FlashcardCardDTO { Id = c.Id, Front = c.Front, Back = c.Back }).ToList();
    }
}