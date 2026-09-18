using Domain.DTOs;

namespace Controller.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDTO> RegisterAsync(RegisterPostDTO dto);
    Task<AuthResponseDTO> LoginAsync(LoginPostDTO dto);
}
