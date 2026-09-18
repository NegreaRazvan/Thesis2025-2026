using Controller.Interfaces;
using Domain.DTOs;
using Domain.Exceptions.Custom;
using log4net;
using Microsoft.AspNetCore.Http;
using Service.Interfaces;
using Service.Utils;

namespace Service.Services;

public class UserService(IUserRepository userRepository, IAppValidatorFactory validator) : IUserService
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IAppValidatorFactory _validator = validator;
    private readonly ILog _logger = LogManager.GetLogger(typeof(UserService));

    private static readonly HashSet<string> AllowedMimeTypes = ["image/jpeg", "image/png", "image/webp"];

    public async Task<UserProfileDTO> GetProfileAsync(string userId)
    {
        var profile = await _userRepository.GetByIdAsync(userId)
            ?? throw new NotFoundException("User not found.");
        return profile;
    }

    public async Task<UserProfileDTO> UpdateProfileAsync(string userId, UpdateProfileDTO dto)
    {
        await ValidationHelper.ValidateAndThrowAsync(_validator.Get<UpdateProfileDTO>(), dto);

        if (!string.IsNullOrWhiteSpace(dto.Username) && await _userRepository.IsUsernameTakenAsync(dto.Username, userId))
            throw new ConflictException("Username is already taken.");

        if (!string.IsNullOrWhiteSpace(dto.Email) && await _userRepository.IsEmailTakenAsync(dto.Email, userId))
            throw new ConflictException("Email is already in use.");

        var result = await _userRepository.UpdateProfileAsync(userId, dto);
        _logger.InfoFormat("Profile updated for user {0}", userId);
        return result;
    }

    public async Task ChangePasswordAsync(string userId, ChangePasswordDTO dto)
    {
        await ValidationHelper.ValidateAndThrowAsync(_validator.Get<ChangePasswordDTO>(), dto);
        await _userRepository.ChangePasswordAsync(userId, dto.CurrentPassword, dto.NewPassword);
        _logger.InfoFormat("Password changed for user {0}", userId);
    }

    public async Task UpdateAvatarAsync(string userId, IFormFile file)
    {
        if (file.Length > 2 * 1024 * 1024)
            throw new EntityValidationException("Avatar must be under 2 MB.");
        if (!AllowedMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
            throw new EntityValidationException("Only JPEG, PNG, and WebP images are allowed.");

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        await _userRepository.UpdateAvatarAsync(userId, ms.ToArray(), file.ContentType.ToLowerInvariant());
        _logger.InfoFormat("Avatar updated for user {0}", userId);
    }

    public async Task<(byte[] Bytes, string ContentType)> GetAvatarAsync(string userId)
    {
        var avatar = await _userRepository.GetAvatarAsync(userId);
        if (avatar is null) throw new NotFoundException("No avatar set.");
        return avatar.Value;
    }

    public async Task DeleteAvatarAsync(string userId)
    {
        await _userRepository.DeleteAvatarAsync(userId);
        _logger.InfoFormat("Avatar removed for user {0}", userId);
    }
}
