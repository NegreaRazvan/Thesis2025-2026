using Controller.Interfaces;
using Controller.Security;
using Domain.DTOs;
using log4net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Controller.Controllers;

/// <summary>Submit German texts for CEFR analysis and manage submission history.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SubmissionController(ISubmissionService submissionService) : ControllerBase
{
    private readonly ISubmissionService _submissionService = submissionService;
    private readonly ILog _logger = LogManager.GetLogger(typeof(SubmissionController));

    /// <summary>Submit a German text for CEFR prediction, grammar analysis, and AI feedback.</summary>
    [HttpPost]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("expensive")]
    [ProducesResponseType(typeof(SubmissionResponseDTO), 201)]
    [ProducesResponseType(422)]
    [ProducesResponseType(503)]
    public async Task<ActionResult<SubmissionResponseDTO>> Submit([FromBody] SubmitTextDTO dto)
    {
        var userId = ClaimsHelper.GetUserId(User);
        _logger.InfoFormat("Submit from user {0}", userId);
        var result = await _submissionService.SubmitAsync(userId, dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Get the authenticated user's submission history.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<SubmissionSummaryDTO>), 200)]
    public async Task<ActionResult<List<SubmissionSummaryDTO>>> GetHistory()
    {
        var userId = ClaimsHelper.GetUserId(User);
        return Ok(await _submissionService.GetHistoryAsync(userId));
    }

    /// <summary>Get full details of a specific submission.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SubmissionResponseDTO), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<SubmissionResponseDTO>> GetById(Guid id)
    {
        var userId = ClaimsHelper.GetUserId(User);
        return Ok(await _submissionService.GetByIdAsync(userId, id));
    }

    /// <summary>Delete a submission permanently.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = ClaimsHelper.GetUserId(User);
        _logger.InfoFormat("Delete submission {0} from user {1}", id, userId);
        await _submissionService.DeleteAsync(userId, id);
        return NoContent();
    }
}
