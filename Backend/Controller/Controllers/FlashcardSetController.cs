using Controller.Interfaces;
using Controller.Security;
using Domain.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Controller.Controllers;

/// <summary>Manage flashcard sets — CRUD for sets, cards, and CSV bulk import.</summary>
[ApiController]
[Route("api/FlashcardSet")]
[Authorize]
public class FlashcardSetController(IFlashcardSetService service) : ControllerBase
{
    private readonly IFlashcardSetService _service = service;

    /// <summary>List all flashcard sets for the authenticated user.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<FlashcardSetSummaryDTO>), 200)]
    public async Task<ActionResult<List<FlashcardSetSummaryDTO>>> GetAll() =>
        Ok(await _service.GetAllAsync(ClaimsHelper.GetUserId(User)));

    /// <summary>Get a flashcard set with all its cards.</summary>
    /// <param name="id">Set ID.</param>
    /// <response code="404">Set not found.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(FlashcardSetDetailDTO), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<FlashcardSetDetailDTO>> GetById(Guid id) =>
        Ok(await _service.GetByIdAsync(ClaimsHelper.GetUserId(User), id));

    /// <summary>Create a new empty flashcard set.</summary>
    /// <param name="dto">Set name and optional description.</param>
    /// <returns>The created set summary.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(FlashcardSetSummaryDTO), 201)]
    [ProducesResponseType(422)]
    public async Task<ActionResult<FlashcardSetSummaryDTO>> Create([FromBody] CreateFlashcardSetDTO dto)
    {
        var result = await _service.CreateAsync(ClaimsHelper.GetUserId(User), dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Delete a flashcard set and all its cards.</summary>
    /// <param name="id">Set ID.</param>
    /// <response code="204">Deleted.</response>
    /// <response code="404">Set not found.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.DeleteAsync(ClaimsHelper.GetUserId(User), id);
        return NoContent();
    }

    /// <summary>Add a single card to a flashcard set.</summary>
    /// <param name="id">Set ID.</param>
    /// <param name="dto">Front and back text for the card.</param>
    [HttpPost("{id:guid}/cards")]
    [ProducesResponseType(typeof(FlashcardCardDTO), 200)]
    [ProducesResponseType(422)]
    public async Task<ActionResult<FlashcardCardDTO>> AddCard(Guid id, [FromBody] UpsertFlashcardCardDTO dto) =>
        Ok(await _service.AddCardAsync(ClaimsHelper.GetUserId(User), id, dto));

    /// <summary>Update an existing card in a flashcard set.</summary>
    /// <param name="id">Set ID.</param>
    /// <param name="dto">Card ID, new front and back text.</param>
    [HttpPut("{id:guid}/cards")]
    [ProducesResponseType(typeof(FlashcardCardDTO), 200)]
    public async Task<ActionResult<FlashcardCardDTO>> UpdateCard(Guid id, [FromBody] UpsertFlashcardCardDTO dto) =>
        Ok(await _service.UpdateCardAsync(ClaimsHelper.GetUserId(User), id, dto));

    /// <summary>Delete a single card from a flashcard set.</summary>
    /// <param name="id">Set ID.</param>
    /// <param name="cardId">Card ID.</param>
    /// <response code="204">Card deleted.</response>
    [HttpDelete("{id:guid}/cards/{cardId:guid}")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> DeleteCard(Guid id, Guid cardId)
    {
        await _service.DeleteCardAsync(ClaimsHelper.GetUserId(User), id, cardId);
        return NoContent();
    }

    /// <summary>Bulk-add multiple cards to a set (e.g. CSV import).</summary>
    /// <param name="id">Set ID.</param>
    /// <param name="dto">List of cards to add.</param>
    [HttpPost("{id:guid}/cards/bulk")]
    [ProducesResponseType(typeof(List<FlashcardCardDTO>), 200)]
    [ProducesResponseType(422)]
    public async Task<ActionResult<List<FlashcardCardDTO>>> BulkAdd(Guid id, [FromBody] BulkAddFlashcardCardsDTO dto) =>
        Ok(await _service.BulkAddCardsAsync(ClaimsHelper.GetUserId(User), id, dto));

    /// <summary>Mark all cards in a set as reviewed (study session complete).</summary>
    /// <param name="id">Set ID.</param>
    /// <response code="204">Marked reviewed.</response>
    [HttpPost("{id:guid}/complete")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Complete(Guid id)
    {
        await _service.MarkReviewedAsync(ClaimsHelper.GetUserId(User), id);
        return NoContent();
    }
}
