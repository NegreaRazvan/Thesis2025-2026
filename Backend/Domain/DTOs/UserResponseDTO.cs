namespace Domain.DTOs;

public record UserResponseDTO
{
    public string Id { get; init; } = "";
    public string Email { get; init; } = "";
    public string Username { get; init; } = "";
}
