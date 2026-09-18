using Domain.DTOs;

namespace Service.Interfaces;

public interface IAuthRepository
{
    Task<UserResponseDTO?> GetByEmailAsync(string email);
    Task<UserResponseDTO?> GetByIdAsync(string userId);
    Task<(string? Token, UserResponseDTO? User, string? Error)> RegisterAndLoginAsync(RegisterPostDTO dto);
    Task<(string? Token, UserResponseDTO? User, string? Error)> LoginAsync(LoginPostDTO dto);
}