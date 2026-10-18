using NexAccess.Domain.Entities;

namespace NexAccess.Application.Interfaces;

public interface ITokenService
{
    IssuedToken Create(User user);
}

public sealed record IssuedToken(string Token, DateTime ExpiresAt);
