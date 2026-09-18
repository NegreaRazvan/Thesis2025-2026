using Domain.DTOs;
using Domain.Exceptions.Custom;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Moq;
using Service.Interfaces;
using Service.Services;

namespace Backend.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IAuthRepository> _authRepo = new();
    private readonly Mock<IAppValidatorFactory> _validatorFactory = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        SetupPassingValidator<RegisterPostDTO>();
        SetupPassingValidator<LoginPostDTO>();

        _sut = new AuthService(_authRepo.Object, _validatorFactory.Object);
    }

    private void SetupPassingValidator<T>()
    {
        var validator = new Mock<IValidator<T>>();
        validator.Setup(v => v.ValidateAsync(It.IsAny<T>(), default))
            .ReturnsAsync(new ValidationResult());
        _validatorFactory.Setup(f => f.Get<T>()).Returns(validator.Object);
    }

    private void SetupFailingValidator<T>(string errorMessage)
    {
        var validator = new Mock<IValidator<T>>();
        validator.Setup(v => v.ValidateAsync(It.IsAny<T>(), default))
            .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure("Field", errorMessage) }));
        _validatorFactory.Setup(f => f.Get<T>()).Returns(validator.Object);
    }


    [Fact]
    public async Task RegisterAsync_Success_ReturnsAuthResponse()
    {
        var dto = new RegisterPostDTO("test@mail.com", "TestUser", "Password123!");
        var user = new UserResponseDTO { Id = "u1", Email = "test@mail.com", Username = "TestUser" };

        _authRepo.Setup(r => r.RegisterAndLoginAsync(dto))
            .ReturnsAsync(("jwt-token", user, (string?)null));

        var result = await _sut.RegisterAsync(dto);

        result.Token.Should().Be("jwt-token");
        result.Username.Should().Be("TestUser");
        result.UserId.Should().Be("u1");
    }

    [Fact]
    public async Task RegisterAsync_RepoReturnsNull_ThrowsConflict()
    {
        var dto = new RegisterPostDTO("test@mail.com", "TestUser", "Password123!");

        _authRepo.Setup(r => r.RegisterAndLoginAsync(dto))
            .ReturnsAsync(((string?)null, (UserResponseDTO?)null, "Email already taken."));

        var act = () => _sut.RegisterAsync(dto);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("Email already taken.");
    }

    [Fact]
    public async Task RegisterAsync_RepoReturnsNullWithNoError_ThrowsGenericConflict()
    {
        var dto = new RegisterPostDTO("test@mail.com", "TestUser", "Password123!");

        _authRepo.Setup(r => r.RegisterAndLoginAsync(dto))
            .ReturnsAsync(((string?)null, (UserResponseDTO?)null, (string?)null));

        var act = () => _sut.RegisterAsync(dto);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("Registration failed.");
    }

    [Fact]
    public async Task RegisterAsync_ValidationFails_ThrowsEntityValidation()
    {
        SetupFailingValidator<RegisterPostDTO>("Email is required.");
        var sut = new AuthService(_authRepo.Object, _validatorFactory.Object);

        var act = () => sut.RegisterAsync(new RegisterPostDTO("", "User", "pass"));

        await act.Should().ThrowAsync<EntityValidationException>();
    }


    [Fact]
    public async Task LoginAsync_Success_ReturnsAuthResponse()
    {
        var dto = new LoginPostDTO("test@mail.com", "Password123!");
        var user = new UserResponseDTO { Id = "u1", Email = "test@mail.com", Username = "TestUser" };

        _authRepo.Setup(r => r.LoginAsync(dto))
            .ReturnsAsync(("jwt-token", user, (string?)null));

        var result = await _sut.LoginAsync(dto);

        result.Token.Should().Be("jwt-token");
        result.Username.Should().Be("TestUser");
    }

    [Fact]
    public async Task LoginAsync_InvalidCredentials_ThrowsUnauthorized()
    {
        var dto = new LoginPostDTO("test@mail.com", "wrong");

        _authRepo.Setup(r => r.LoginAsync(dto))
            .ReturnsAsync(((string?)null, (UserResponseDTO?)null, "Invalid credentials."));

        var act = () => _sut.LoginAsync(dto);

        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("Invalid credentials.");
    }

    [Fact]
    public async Task LoginAsync_NullErrorMessage_ThrowsGenericUnauthorized()
    {
        var dto = new LoginPostDTO("test@mail.com", "wrong");

        _authRepo.Setup(r => r.LoginAsync(dto))
            .ReturnsAsync(((string?)null, (UserResponseDTO?)null, (string?)null));

        var act = () => _sut.LoginAsync(dto);

        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("Invalid credentials.");
    }

    [Fact]
    public async Task LoginAsync_ValidationFails_ThrowsEntityValidation()
    {
        SetupFailingValidator<LoginPostDTO>("Email is required.");
        var sut = new AuthService(_authRepo.Object, _validatorFactory.Object);

        var act = () => sut.LoginAsync(new LoginPostDTO("", "pass"));

        await act.Should().ThrowAsync<EntityValidationException>();
    }
}
