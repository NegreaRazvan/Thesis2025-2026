using AutoMapper;
using Domain.DTOs;
using Domain.Exceptions.Custom;
using Microsoft.AspNetCore.Identity;
using Repository.EFEntities;
using Service.Interfaces;

namespace Repository;

public class UserRepository(UserManager<AppUser> userManager, IMapper mapper) : IUserRepository
{
    private readonly UserManager<AppUser> _userManager = userManager;
    private readonly IMapper _mapper = mapper;

    private static readonly HashSet<string> ValidCefrLevels = ["A1", "A2", "B1", "B2", "C1", "C2"];

    public async Task<UserProfileDTO?> GetByIdAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        return user is null ? null : _mapper.Map<UserProfileDTO>(user);
    }

    public async Task<bool> IsUsernameTakenAsync(string username, string excludeUserId)
    {
        var existing = await _userManager.FindByNameAsync(username);
        return existing is not null && existing.Id != excludeUserId;
    }

    public async Task<bool> IsEmailTakenAsync(string email, string excludeUserId)
    {
        var existing = await _userManager.FindByEmailAsync(email.ToLowerInvariant());
        return existing is not null && existing.Id != excludeUserId;
    }

    public async Task<UserProfileDTO> UpdateProfileAsync(string userId, UpdateProfileDTO dto)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new NotFoundException("User not found.");

        if (!string.IsNullOrWhiteSpace(dto.Username) && dto.Username != user.UserName)
        {
            var setNameResult = await _userManager.SetUserNameAsync(user, dto.Username);
            if (!setNameResult.Succeeded)
                throw new ConflictException(string.Join("; ", setNameResult.Errors.Select(e => e.Description)));
            user = await _userManager.FindByIdAsync(userId)!;
        }

        if (!string.IsNullOrWhiteSpace(dto.Email) && dto.Email.ToLowerInvariant() != user!.Email?.ToLowerInvariant())
        {
            var token = await _userManager.GenerateChangeEmailTokenAsync(user!, dto.Email);
            var setEmailResult = await _userManager.ChangeEmailAsync(user!, dto.Email, token);
            if (!setEmailResult.Succeeded)
                throw new ConflictException(string.Join("; ", setEmailResult.Errors.Select(e => e.Description)));
            user = await _userManager.FindByIdAsync(userId)!;
        }

        if (dto.DisplayName is not null) user!.DisplayName = dto.DisplayName.Trim();
        if (dto.Bio is not null) user!.Bio = dto.Bio.Trim();
        if (dto.TargetCefrLevel is not null) user!.TargetCefrLevel = dto.TargetCefrLevel.ToUpperInvariant();
        if (dto.NativeLanguage is not null) user!.NativeLanguage = dto.NativeLanguage.Trim();
        if (dto.LearningSince.HasValue) user!.LearningSince = dto.LearningSince;

        await _userManager.UpdateAsync(user!);
        return _mapper.Map<UserProfileDTO>(user);
    }

    public async Task ChangePasswordAsync(string userId, string currentPassword, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new NotFoundException("User not found.");

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (!result.Succeeded)
            throw new EntityValidationException(string.Join("; ", result.Errors.Select(e => e.Description)));
    }

    public async Task UpdateAvatarAsync(string userId, byte[] bytes, string contentType)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new NotFoundException("User not found.");
        user.AvatarBytes = bytes;
        user.AvatarContentType = contentType;
        await _userManager.UpdateAsync(user);
    }

    public async Task<(byte[] Bytes, string ContentType)?> GetAvatarAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user?.AvatarBytes is null || user.AvatarBytes.Length == 0) return null;
        return (user.AvatarBytes, user.AvatarContentType ?? "image/jpeg");
    }

    public async Task DeleteAvatarAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new NotFoundException("User not found.");
        user.AvatarBytes = null;
        user.AvatarContentType = null;
        await _userManager.UpdateAsync(user);
    }
}
