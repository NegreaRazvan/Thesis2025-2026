using Controller.Interfaces;
using Domain.DTOs;
using log4net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Controller.Controllers;

/// <summary>Writing prompts — CEFR-levelled prompts to inspire German writing practice.</summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
public class WritingPromptController(IWritingPromptService service) : ControllerBase
{
    private readonly IWritingPromptService _service = service;
    private readonly ILog _logger = LogManager.GetLogger(typeof(WritingPromptController));

    /// <summary>Get writing prompts filtered by CEFR level.</summary>
    /// <param name="level">CEFR level (A1, A2, B1, B2, or C1).</param>
    /// <returns>List of prompts for the given level.</returns>
    [HttpGet("{level}")]
    [ProducesResponseType(typeof(List<WritingPromptDTO>), 200)]
    public async Task<ActionResult<List<WritingPromptDTO>>> GetByLevel(string level)
    {
        _logger.InfoFormat("Prompts for CEFR level {0}", level);
        return Ok(await _service.GetByLevelAsync(level));
    }
}
