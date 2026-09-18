using Controller.Interfaces;
using Controller.Security;
using Domain.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Controller.Controllers;

/// <summary>Vocabulary game — timed word challenges using fastText word embeddings.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VocabGameController(IVocabGameService service) : ControllerBase
{
    private readonly IVocabGameService _service = service;

    /// <summary>Get a random word category and seed words for a new game round.</summary>
    /// <returns>Category name and seed words.</returns>
    /// <response code="503">Vocab game service is unavailable.</response>
    [HttpGet("category")]
    [ProducesResponseType(typeof(VocabGameCategoryResponseDTO), 200)]
    [ProducesResponseType(503)]
    public async Task<ActionResult<VocabGameCategoryResponseDTO>> GetCategory() =>
        Ok(await _service.GetCategoryAsync());

    /// <summary>Validate a batch of words submitted during a game round.</summary>
    /// <param name="dto">Category, seed words, and the user's submitted words.</param>
    /// <returns>Validation results: valid words, scores, and misspelling info.</returns>
    [HttpPost("validate-batch")]
    [ProducesResponseType(typeof(VocabGameBatchValidateResponseDTO), 200)]
    [ProducesResponseType(503)]
    public async Task<ActionResult<VocabGameBatchValidateResponseDTO>> ValidateBatch([FromBody] VocabGameBatchValidateRequestDTO dto) =>
        Ok(await _service.ValidateBatchAsync(dto));

    /// <summary>Save a completed game session to the user's history.</summary>
    /// <param name="dto">Game results to persist.</param>
    /// <returns>The saved session.</returns>
    [HttpPost("session")]
    [ProducesResponseType(typeof(VocabGameSessionDTO), 200)]
    public async Task<ActionResult<VocabGameSessionDTO>> SaveSession([FromBody] VocabGameSaveSessionDTO dto) =>
        Ok(await _service.SaveSessionAsync(ClaimsHelper.GetUserId(User), dto));

    /// <summary>Get the user's past game sessions.</summary>
    /// <returns>List of game sessions with scores and word details.</returns>
    [HttpGet("history")]
    [ProducesResponseType(typeof(List<VocabGameSessionDTO>), 200)]
    public async Task<ActionResult<List<VocabGameSessionDTO>>> GetHistory() =>
        Ok(await _service.GetHistoryAsync(ClaimsHelper.GetUserId(User)));
}
