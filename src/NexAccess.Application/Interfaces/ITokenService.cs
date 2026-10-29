using NexAccess.Domain.Entities;

namespace NexAccess.Application.Interfaces;

public interface ITokenService
{
    // JWT tradicional del panel web.
    IssuedToken Create(User user);

    // JWT exclusivo de una sesión móvil.
    IssuedToken CreateMobile(User user, Guid sessionId);
}

public sealed record IssuedToken(
    string Token,
    DateTime ExpiresAt);

