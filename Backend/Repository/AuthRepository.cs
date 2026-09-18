using AutoMapper;
using Domain.DTOs;
using log4net;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Repository.Context;
using Repository.EFEntities;
using Service.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Repository;

public class AuthRepository(
    GermanAIContext context,
    UserManager<AppUser> userManager,
    IMapper mapper,
    IConfiguration config) : IAuthRepository
{
    private readonly GermanAIContext _context = context;
    private readonly UserManager<AppUser> _userManager = userManager;
    private readonly IMapper _mapper = mapper;
    private readonly IConfiguration _config = config;
    private readonly ILog _logger = LogManager.GetLogger(typeof(AuthRepository));

    public async Task<UserResponseDTO?> GetByEmailAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        return user is null ? null : _mapper.Map<UserResponseDTO>(user);
    }

    public async Task<UserResponseDTO?> GetByIdAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        return user is null ? null : _mapper.Map<UserResponseDTO>(user);
    }

    public async Task<(string? Token, UserResponseDTO? User, string? Error)> RegisterAndLoginAsync(RegisterPostDTO dto)
    {
        _logger.InfoFormat("Register-and-login for: {0}", dto.Email);

        var user = new AppUser
        {
            Email = dto.Email.ToLowerInvariant().Trim(),
            UserName = dto.Username.Trim(),
            LastLogin = DateTime.UtcNow,
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            var error = string.Join("; ", result.Errors.Select(e => e.Description));
            _logger.WarnFormat("Registration failed for {0}: {1}", dto.Email, error);
            return (null, null, error);
        }

        var token = GenerateJwt(user);
        var userDto = _mapper.Map<UserResponseDTO>(user);

        _logger.InfoFormat("Register-and-login successful for user {0}", user.Id);
        return (token, userDto, null);
    }

    public async Task<(string? Token, UserResponseDTO? User, string? Error)> LoginAsync(LoginPostDTO dto)
    {
        _logger.InfoFormat("Login attempt for: {0}", dto.Email);

        var user = await _userManager.FindByEmailAsync(dto.Email.ToLowerInvariant());
        if (user is null) return (null, null, "Invalid credentials.");

        var passwordValid = await _userManager.CheckPasswordAsync(user, dto.Password);
        if (!passwordValid) return (null, null, "Invalid credentials.");

        user.LastLogin = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var token = GenerateJwt(user);
        var userDto = _mapper.Map<UserResponseDTO>(user);

        _logger.InfoFormat("Login successful for user {0}", user.Id);
        return (token, userDto, null);
    }

    private string GenerateJwt(AppUser user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Secret"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.UserName ?? ""),
            new Claim(ClaimTypes.Email, user.Email ?? ""),
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(double.Parse(_config["Jwt:ExpiryHours"] ?? "168")),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}