using Controller.Interfaces;
using Domain.DTOs;
using Domain.Exceptions.Custom;
using log4net;
using Service.Interfaces;
using Service.Utils;

namespace Service.Services;

public class AuthService(IAuthRepository authRepository, IAppValidatorFactory validator) : IAuthService
{
    private readonly IAuthRepository _authRepository = authRepository;
    private readonly IAppValidatorFactory _validator = validator;
    private readonly ILog _logger = LogManager.GetLogger(typeof(AuthService));

    public async Task<AuthResponseDTO> RegisterAsync(RegisterPostDTO dto)
    {
        await ValidationHelper.ValidateAndThrowAsync(_validator.Get<RegisterPostDTO>(), dto);

        var (token, user, error) = await _authRepository.RegisterAndLoginAsync(dto);

        if (token is null || user is null)
            throw new ConflictException(error ?? "Registration failed.");

        _logger.InfoFormat("User registered, id {0}", user.Id);
        return new AuthResponseDTO(token, user.Username, user.Id, user.Email);
    }

    public async Task<AuthResponseDTO> LoginAsync(LoginPostDTO dto)
    {
        await ValidationHelper.ValidateAndThrowAsync(_validator.Get<LoginPostDTO>(), dto);

        // Single DB round-trip: FindByEmail + CheckPassword + map user, all in one call
        var (token, user, error) = await _authRepository.LoginAsync(dto);

        if (token is null || user is null)
            throw new UnauthorizedException(error ?? "Invalid credentials.");

        _logger.InfoFormat("User logged in, id {0}", user.Id);
        return new AuthResponseDTO(token, user.Username, user.Id, user.Email);
    }
}