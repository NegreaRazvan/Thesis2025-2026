namespace Domain.DTOs;

public record LoginPostDTO(string Email, string Password);
public record AuthResponseDTO(string Token, string Username, string UserId, string Email = "");
