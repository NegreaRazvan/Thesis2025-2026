using Controller.Interfaces;
using Controller.Security;
using Domain.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Controller.Controllers;

/// <summary>Progress tracking — CEFR timeline, error patterns, gamification, and PDF reports.</summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ProgressController(IProgressService service) : ControllerBase
{

    /// <summary>Get the user's CEFR level timeline with feature snapshots per submission.</summary>
    /// <returns>Chronological list of progress data points.</returns>
    [HttpGet("timeline")]
    [ProducesResponseType(typeof(List<ProgressPointDTO>), 200)]
    public async Task<ActionResult<List<ProgressPointDTO>>> GetTimeline()
    {
        var userId = ClaimsHelper.GetUserId(User);
        return Ok(await service.GetProgressAsync(userId));
    }

    /// <summary>Get the user's most frequent grammar error patterns.</summary>
    /// <returns>Top 10 recurring errors by count.</returns>
    [HttpGet("error-patterns")]
    [ProducesResponseType(typeof(List<ErrorPatternDTO>), 200)]
    public async Task<ActionResult<List<ErrorPatternDTO>>> GetErrorPatterns()
    {
        var userId = ClaimsHelper.GetUserId(User);
        return Ok(await service.GetErrorPatternsAsync(userId));
    }

    /// <summary>Get gamification data: streaks, submission count, and achievement badges.</summary>
    /// <returns>Current/longest streak, total submissions, and badge unlock status.</returns>
    [HttpGet("gamification")]
    [ProducesResponseType(typeof(GamificationDTO), 200)]
    public async Task<ActionResult<GamificationDTO>> GetGamification()
    {
        var userId = ClaimsHelper.GetUserId(User);
        return Ok(await service.GetGamificationAsync(userId));
    }

    /// <summary>Download a PDF progress report summarising the user's learning journey.</summary>
    /// <returns>PDF file download.</returns>
    [HttpGet("report")]
    [ProducesResponseType(typeof(FileContentResult), 200)]
    public async Task<IActionResult> DownloadReport()
    {
        var userId = ClaimsHelper.GetUserId(User);
        var username = User.FindFirstValue(ClaimTypes.Name) ?? "User";

        var pdf = await service.GenerateReportPdfAsync(userId, username);
        return File(pdf, "application/pdf", $"LinguaForge_Report_{DateTime.UtcNow:yyyyMMdd}.pdf");
    }
}
