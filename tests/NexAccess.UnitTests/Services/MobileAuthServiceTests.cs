using System.Security.Cryptography;
using System.Text;
using NexAccess.Application.DTOs.Auth;
using NexAccess.Application.Interfaces;
using NexAccess.Domain.Entities;
using NexAccess.UnitTests.Support;
using NSubstitute;

namespace NexAccess.UnitTests.Services;

public class MobileAuthServiceTests
{
    private readonly ServiceFactory _factory = new();

    [Fact]
    public async Task Logout_with_previous_refresh_token_revokes_original_session()
    {
        const string oldRefreshToken = "refresh-token-anterior";

        var tokenHash = Convert.ToHexString(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(oldRefreshToken)));

        var sessionId = Guid.NewGuid();
        var otherSessionId = Guid.NewGuid();

        var session = new MobileSession
        {
            Id = sessionId,
            UserId = 1,
            InstallationId = Guid.NewGuid(),
            RefreshTokenHash = "HASH_TOKEN_ACTUAL",
            IsActive = true
        };

        _factory.MobileSessions
            .GetByRefreshTokenHashAsync(
                tokenHash,
                Arg.Any<CancellationToken>())
            .Returns((MobileSession?)null);

        _factory.UsedMobileTokens
            .FindByHashAsync(
                tokenHash,
                Arg.Any<CancellationToken>())
            .Returns(new UsedMobileRefreshToken
            {
                Id = 10,
                SessionId = sessionId,
                TokenHash = tokenHash
            });

        _factory.MobileSessions
            .GetByIdAsync(
                sessionId,
                Arg.Any<CancellationToken>())
            .Returns(session);

        _factory.MobileSessions
            .RevokeAsync(
                sessionId,
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(true);

        var service = _factory.MobileAuthService(
            Substitute.For<IPasswordHasher>(),
            Substitute.For<ITokenService>());

        await service.LogoutAsync(new MobileLogoutRequest
        {
            RefreshToken = oldRefreshToken
        });

        await _factory.MobileSessions.Received(1)
            .RevokeAsync(
                sessionId,
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>());

        await _factory.MobileSessions.DidNotReceive()
            .RevokeAsync(
                otherSessionId,
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>());
    }
}
