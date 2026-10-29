using NexAccess.Domain.Entities;

namespace NexAccess.Application.Interfaces.Repositories;

public interface IMobileSessionRepository
{
    Task<MobileSession?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<MobileSession?> GetByRefreshTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    void Add(MobileSession session);

    Task<bool> RotateTokenAsync(
        Guid sessionId,
        string currentTokenHash,
        string newTokenHash,
        DateTime utcNow,
        CancellationToken cancellationToken = default);

    Task<bool> RevokeAsync(
        Guid sessionId,
        DateTime utcNow,
        CancellationToken cancellationToken = default);

Task<int> RevokeAllForUserAsync(
    long userId,
    DateTime utcNow,
    CancellationToken cancellationToken = default);

Task<bool> RotateAndRecordTokenAsync(
    Guid sessionId,
    string currentTokenHash,
    string newTokenHash,
    DateTime utcNow,
    CancellationToken cancellationToken = default);


}

