using Controller.Interfaces;
using Controller.Security;
using Domain.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Controller.Controllers;

/// <summary>SRS flashcard reviews — get due cards, review with quality rating, create from errors.</summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
public class FlashcardController(IFlashcardService service) : ControllerBase
{

    /// <summary>Get flashcards that are due for review (SM-2 algorithm).</summary>
    [HttpGet("due")]
    [ProducesResponseType(typeof(List<FlashcardResponseDTO>), 200)]
    public async Task<ActionResult<List<FlashcardResponseDTO>>> GetDue()
    {
        var userId = ClaimsHelper.GetUserId(User);
        return Ok(await service.GetDueCardsAsync(userId));
    }

    /// <summary>Submit a review result for a flashcard (quality 0–5, SM-2 scoring).</summary>
    [HttpPost("review")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> Review([FromBody] ReviewFlashcardDTO dto)
    {
        var userId = ClaimsHelper.GetUserId(User);
        await service.ReviewCardAsync(userId, dto);
        return NoContent();
    }

    /// <summary>Auto-create a flashcard from a submission grammar error.</summary>
    [HttpPost("from-error/{errorId:guid}")]
    [ProducesResponseType(typeof(FlashcardResponseDTO), 201)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<FlashcardResponseDTO>> CreateFromError(Guid errorId)
    {
        var userId = ClaimsHelper.GetUserId(User);
        var card = await service.CreateFromErrorAsync(userId, errorId);
        return StatusCode(201, card);
    }
}
