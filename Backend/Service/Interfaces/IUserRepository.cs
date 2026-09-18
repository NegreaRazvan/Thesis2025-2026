using Domain.DTOs;
using Microsoft.AspNetCore.Http;

namespace Service.Interfaces;

public interface IUserRepository
{
    Task<UserProfileDTO?> GetByIdAsync(string userId);
    Task<bool> IsUsernameTakenAsync(string username, string excludeUserId);
    Task<bool> IsEmailTakenAsync(string email, string excludeUserId);
    Task<UserProfileDTO> UpdateProfileAsync(string userId, UpdateProfileDTO dto);
    Task ChangePasswordAsync(string userId, string currentPassword, string newPassword);
    Task UpdateAvatarAsync(string userId, byte[] bytes, string contentType);
    Task<(byte[] Bytes, string ContentType)?> GetAvatarAsync(string userId);
    Task DeleteAvatarAsync(string userId);
}
