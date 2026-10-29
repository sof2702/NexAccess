using NexAccess.Domain.Entities;

namespace NexAccess.Application.Interfaces.Repositories;

public interface IUsedMobileRefreshTokenRepository
{
    Task<UsedMobileRefreshToken?> FindByHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    void Add(UsedMobileRefreshToken token);
}

