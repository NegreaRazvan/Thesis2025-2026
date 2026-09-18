using Domain.DTOs;
using Microsoft.AspNetCore.Http;

namespace Controller.Interfaces;

public interface IUserService
{
    Task<UserProfileDTO> GetProfileAsync(string userId);
    Task<UserProfileDTO> UpdateProfileAsync(string userId, UpdateProfileDTO dto);
    Task ChangePasswordAsync(string userId, ChangePasswordDTO dto);
    Task UpdateAvatarAsync(string userId, IFormFile file);
    Task<(byte[] Bytes, string ContentType)> GetAvatarAsync(string userId);
    Task DeleteAvatarAsync(string userId);
}
