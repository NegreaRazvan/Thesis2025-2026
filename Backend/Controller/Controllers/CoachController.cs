using Controller.Interfaces;
using Controller.Security;
using Domain.DTOs;
using log4net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Controller.Controllers;

/// <summary>AI coaching — multi-turn Socratic tutoring with "Lena" (powered by Claude).</summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
public class CoachController(ICoachService coachService) : ControllerBase
{
    private readonly ICoachService _coachService = coachService;
    private readonly ILog _logger = LogManager.GetLogger(typeof(CoachController));

    /// <summary>Send a message in a coaching session.</summary>
    [HttpPost("chat")]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("expensive")]
    [ProducesResponseType(typeof(CoachChatResponseDTO), 200)]
    [ProducesResponseType(422)]
    public async Task<ActionResult<CoachChatResponseDTO>> Chat([FromBody] CoachChatRequestDTO request)
    {
        _logger.InfoFormat("Coach chat — history turns: {0}", request.History.Count);
        var reply = await _coachService.ChatAsync(request);
        return Ok(reply);
    }

    /// <summary>Save a completed coaching session to the user's history.</summary>
    [HttpPost("sessions")]
    [ProducesResponseType(typeof(CoachSessionSummaryDTO), 200)]
    public async Task<ActionResult<CoachSessionSummaryDTO>> SaveSession([FromBody] SaveCoachSessionDTO dto) =>
        Ok(await _coachService.SaveSessionAsync(ClaimsHelper.GetUserId(User), dto));

    /// <summary>Get the user's past coaching sessions.</summary>
    [HttpGet("sessions")]
    [ProducesResponseType(typeof(List<CoachSessionSummaryDTO>), 200)]
    public async Task<ActionResult<List<CoachSessionSummaryDTO>>> GetHistory() =>
        Ok(await _coachService.GetSessionsAsync(ClaimsHelper.GetUserId(User)));

    /// <summary>Get the full detail of a coaching session including all messages.</summary>
    [HttpGet("sessions/{id:guid}")]
    [ProducesResponseType(typeof(CoachSessionDetailDTO), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<CoachSessionDetailDTO>> GetSessionDetail(Guid id)
    {
        var detail = await _coachService.GetSessionDetailAsync(ClaimsHelper.GetUserId(User), id);
        return detail is null ? NotFound() : Ok(detail);
    }
}
