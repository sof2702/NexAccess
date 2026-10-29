using NexAccess.Application.DTOs.Auth;
using NexAccess.Application.Exceptions;
using NexAccess.Application.Interfaces;
using NexAccess.Domain.Entities;
using NexAccess.Domain.Enums;
using NexAccess.Infrastructure.Security;
using NexAccess.UnitTests.Support;
using NSubstitute;

namespace NexAccess.UnitTests.Services;

public class AuthServiceTests
{
    private readonly ServiceFactory _factory = new();
    private readonly Pbkdf2PasswordHasher _hasher = new(1000);
    private readonly ITokenService _tokens = Substitute.For<ITokenService>();
    private readonly User _user;

    public AuthServiceTests()
    {
        _user = new User
{
    Id = 7,
    Username = "rrhh",
    PasswordHash = _hasher.Hash("Clave-Segura-123"),
    SystemRole = SystemRole.Rrhh,
    IsActive = true,
    WebAccessEnabled = true,
    MobileAccessEnabled = false
};
        _factory.Users.GetByUsernameAsync("rrhh", Arg.Any<CancellationToken>()).Returns(_user);
        _tokens.Create(_user).Returns(new IssuedToken("token-de-prueba", TestData.MondayNoonUtc.AddHours(1)));
    }

    [Fact]
    public async Task Valid_credentials_return_token_and_update_last_login()
    {
        var response = await _factory.AuthService(_hasher, _tokens).LoginAsync(new LoginRequest { Username = " rrhh ", Password = "Clave-Segura-123" });

        Assert.Equal("token-de-prueba", response.AccessToken);
        Assert.Equal("Bearer", response.TokenType);
        Assert.Equal(SystemRole.Rrhh, response.User.Role);
        Assert.Equal(TestData.MondayNoonUtc, _user.LastLoginAt);
        _factory.AuditRepository.Received(1).Add(Arg.Is<AuditEntry>(a => a.Action == "LOGIN" && a.UserId == 7));
    }

    [Fact]
    public async Task Wrong_password_is_rejected_and_audited_without_password()
    {
        var error = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            _factory.AuthService(_hasher, _tokens).LoginAsync(new LoginRequest { Username = "rrhh", Password = "incorrecta" }));

        Assert.Equal("INVALID_CREDENTIALS", error.Code);
        _tokens.DidNotReceiveWithAnyArgs().Create(default!);
        _factory.AuditRepository.Received(1).Add(Arg.Is<AuditEntry>(a => a.Action == "LOGIN_FAILED" && !a.Detail!.Contains("incorrecta")));
    }

    [Fact]
    public async Task Unknown_user_gets_same_error()
    {
        var error = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            _factory.AuthService(_hasher, _tokens).LoginAsync(new LoginRequest { Username = "nadie", Password = "Clave-Segura-123" }));

        Assert.Equal("INVALID_CREDENTIALS", error.Code);
    }

    [Fact]
    public async Task Inactive_user_cannot_login()
    {
        _user.IsActive = false;

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            _factory.AuthService(_hasher, _tokens).LoginAsync(new LoginRequest { Username = "rrhh", Password = "Clave-Segura-123" }));
    }

    [Fact]
    public async Task Empty_login_fails_validation()
    {
        await Assert.ThrowsAsync<AppValidationException>(() =>
            _factory.AuthService(_hasher, _tokens).LoginAsync(new LoginRequest()));
    }
}
