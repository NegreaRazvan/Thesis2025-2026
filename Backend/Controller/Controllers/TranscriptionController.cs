using Controller.Interfaces;
using Domain.DTOs;
using log4net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Controller.Controllers;

/// <summary>Speech-to-text — transcribe German audio via OpenAI Whisper.</summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
public class TranscriptionController(ITranscriptionService service) : ControllerBase
{
    private readonly ILog _logger = LogManager.GetLogger(typeof(TranscriptionController));

    /// <summary>Transcribe an audio file to German text using Whisper.</summary>
    /// <param name="audio">Audio file (webm, mp4, ogg, or wav). Max 25 MB.</param>
    /// <returns>Transcribed text, detected language, and audio duration.</returns>
    /// <response code="200">Transcription successful.</response>
    /// <response code="400">No audio file provided.</response>
    /// <response code="503">Whisper service is unavailable.</response>
    [HttpPost]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("expensive")]
    [RequestSizeLimit(25 * 1024 * 1024)]
    [ProducesResponseType(typeof(TranscriptionResultDTO), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(503)]
    public async Task<ActionResult<TranscriptionResultDTO>> Transcribe([FromForm] IFormFile audio)
    {
        if (audio is null || audio.Length == 0)
            return BadRequest("No audio file provided.");

        _logger.InfoFormat("Transcription request: {0} bytes, type: {1}", audio.Length, audio.ContentType);

        return Ok(await service.TranscribeAsync(audio));
    }
}
