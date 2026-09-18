using Controller.Interfaces;
using Controller.Security;
using Domain.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Controller.Controllers;

/// <summary>User profile — view and update account details, avatar, and password.</summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
public class UserController(IUserService service) : ControllerBase
{

    /// <summary>Get the current user's profile.</summary>
    /// <returns>Full profile including display name, bio, learning settings, and avatar flag.</returns>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserProfileDTO), 200)]
    public async Task<ActionResult<UserProfileDTO>> GetProfile()
    {
        var userId = ClaimsHelper.GetUserId(User);
        return Ok(await service.GetProfileAsync(userId));
    }

    /// <summary>Update profile fields (username, email, display name, bio, learning settings).</summary>
    /// <param name="dto">Fields to update — all are optional, only non-null values are applied.</param>
    [HttpPut("me")]
    [ProducesResponseType(typeof(UserProfileDTO), 200)]
    [ProducesResponseType(409)]
    [ProducesResponseType(422)]
    public async Task<ActionResult<UserProfileDTO>> UpdateProfile([FromBody] UpdateProfileDTO dto)
    {
        var userId = ClaimsHelper.GetUserId(User);
        var result = await service.UpdateProfileAsync(userId, dto);
        return Ok(result);
    }

    /// <summary>Change account password.</summary>
    /// <param name="dto">Current password and new password.</param>
    [HttpPut("me/password")]
    [ProducesResponseType(204)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDTO dto)
    {
        var userId = ClaimsHelper.GetUserId(User);
        await service.ChangePasswordAsync(userId, dto);
        return NoContent();
    }

    /// <summary>Upload or replace the current user's avatar (PNG/JPEG/WebP, max 2 MB).</summary>
    [HttpPost("me/avatar")]
    [ProducesResponseType(204)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> UploadAvatar([FromForm] IFormFile file)
    {
        var userId = ClaimsHelper.GetUserId(User);
        await service.UpdateAvatarAsync(userId, file);
        return NoContent();
    }

    /// <summary>Get the current user's avatar image.</summary>
    [HttpGet("me/avatar")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetAvatar()
    {
        var userId = ClaimsHelper.GetUserId(User);
        var (bytes, contentType) = await service.GetAvatarAsync(userId);
        return File(bytes, contentType);
    }

    /// <summary>Delete the current user's avatar.</summary>
    [HttpDelete("me/avatar")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> DeleteAvatar()
    {
        var userId = ClaimsHelper.GetUserId(User);
        await service.DeleteAvatarAsync(userId);
        return NoContent();
    }

    /// <summary>Get any user's avatar by ID — public, no auth required (safe: serves only image bytes).</summary>
    [HttpGet("{userId}/avatar")]
    [AllowAnonymous]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetAvatarById(string userId)
    {
        try
        {
            var (bytes, contentType) = await service.GetAvatarAsync(userId);
            return File(bytes, contentType);
        }
        catch
        {
            return NotFound();
        }
    }
}
