using Controller.Interfaces;
using Domain.DTOs;
using log4net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Controller.Controllers;

/// <summary>Authentication — register new accounts and log in to receive a JWT.</summary>
[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("auth")]
public class AuthController(IAuthService service) : ControllerBase
{
    private readonly IAuthService _service = service;
    private readonly ILog _logger = LogManager.GetLogger(typeof(AuthController));

    /// <summary>Register a new user account and receive a JWT token.</summary>
    /// <param name="dto">Email, username, and password.</param>
    /// <returns>JWT token and user info.</returns>
    /// <response code="201">Account created successfully.</response>
    /// <response code="409">Email or username already exists.</response>
    /// <response code="422">Validation failed (weak password, invalid email, etc.).</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponseDTO), 201)]
    [ProducesResponseType(409)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> Register([FromBody] RegisterPostDTO dto)
    {
        _logger.InfoFormat("Register request for {0}", dto.Email);
        var result = await _service.RegisterAsync(dto);
        return StatusCode(201, result);
    }

    /// <summary>Log in with email and password.</summary>
    /// <param name="dto">Email and password.</param>
    /// <returns>JWT token and user info.</returns>
    /// <response code="200">Login successful.</response>
    /// <response code="401">Invalid credentials.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponseDTO), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Login([FromBody] LoginPostDTO dto)
    {
        _logger.InfoFormat("Login request for {0}", dto.Email);
        var result = await _service.LoginAsync(dto);
        return Ok(result);
    }
}
