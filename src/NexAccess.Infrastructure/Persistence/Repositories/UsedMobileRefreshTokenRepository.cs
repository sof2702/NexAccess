using Microsoft.EntityFrameworkCore;
using NexAccess.Application.Interfaces.Repositories;
using NexAccess.Domain.Entities;

namespace NexAccess.Infrastructure.Persistence.Repositories;

public class UsedMobileRefreshTokenRepository
    : IUsedMobileRefreshTokenRepository
{
    private readonly NexAccessDbContext _context;

    public UsedMobileRefreshTokenRepository(
        NexAccessDbContext context)
    {
        _context = context;
    }

    public Task<UsedMobileRefreshToken?> FindByHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        return _context.UsedMobileRefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(
                t => t.TokenHash == tokenHash,
                cancellationToken);
    }

    public void Add(UsedMobileRefreshToken token)
    {
        _context.UsedMobileRefreshTokens.Add(token);
    }
}

