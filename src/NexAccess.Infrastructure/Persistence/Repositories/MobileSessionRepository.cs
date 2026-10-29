using Microsoft.EntityFrameworkCore;
using NexAccess.Application.Interfaces.Repositories;
using NexAccess.Domain.Entities;
using NexAccess.Infrastructure.Persistence;

namespace NexAccess.Infrastructure.Persistence.Repositories;

public class MobileSessionRepository : IMobileSessionRepository
{
    private readonly NexAccessDbContext _context;

    public MobileSessionRepository(NexAccessDbContext context)
    {
        _context = context;
    }

    public Task<MobileSession?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _context.MobileSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.Id == id,
                cancellationToken);
    }

    public Task<MobileSession?> GetByRefreshTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        return _context.MobileSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.RefreshTokenHash == tokenHash,
                cancellationToken);
    }

    public void Add(MobileSession session)
    {
        _context.MobileSessions.Add(session);
    }

    public async Task<bool> RotateTokenAsync(
        Guid sessionId,
        string currentTokenHash,
        string newTokenHash,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        var updated = await _context.MobileSessions
            .Where(s =>
                s.Id == sessionId &&
                s.IsActive &&
                s.RevokedAt == null &&
                s.RefreshTokenHash == currentTokenHash)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        s => s.RefreshTokenHash,
                        newTokenHash)
                    .SetProperty(
                        s => s.TokenRotatedAt,
                        utcNow)
                    .SetProperty(
                        s => s.LastUsedAt,
                        utcNow),
                cancellationToken);

        return updated == 1;
    }

    public async Task<bool> RevokeAsync(
        Guid sessionId,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        var updated = await _context.MobileSessions
            .Where(s =>
                s.Id == sessionId &&
                s.IsActive)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(s => s.IsActive, false)
                    .SetProperty(s => s.RevokedAt, utcNow),
                cancellationToken);

        return updated == 1;
    }



public async Task<int> RevokeAllForUserAsync(
    long userId,
    DateTime utcNow,
    CancellationToken cancellationToken = default)
{
    return await _context.MobileSessions
        .Where(s =>
            s.UserId == userId &&
            s.IsActive)
        .ExecuteUpdateAsync(
            setters => setters
                .SetProperty(
                    s => s.IsActive,
                    false)
                .SetProperty(
                    s => s.RevokedAt,
                    utcNow),
            cancellationToken);
}


public async Task<bool> RotateAndRecordTokenAsync(
    Guid sessionId,
    string currentTokenHash,
    string newTokenHash,
    DateTime utcNow,
    CancellationToken cancellationToken = default)
{
    await using var transaction =
        await _context.Database.BeginTransactionAsync(
            cancellationToken);

    // 1. Reemplazar el hash únicamente si todavía
    // coincide con el token vigente.
    var updated = await _context.MobileSessions
        .Where(s =>
            s.Id == sessionId &&
            s.IsActive &&
            s.RevokedAt == null &&
            s.RefreshTokenHash == currentTokenHash)
        .ExecuteUpdateAsync(
            setters => setters
                .SetProperty(
                    s => s.RefreshTokenHash,
                    newTokenHash)
                .SetProperty(
                    s => s.TokenRotatedAt,
                    utcNow)
                .SetProperty(
                    s => s.LastUsedAt,
                    utcNow),
            cancellationToken);

    if (updated != 1)
    {
        // No se confirma ningún cambio.
        return false;
    }

    // 2. Registrar el hash anterior como utilizado.
    _context.UsedMobileRefreshTokens.Add(
        new UsedMobileRefreshToken
        {
            SessionId = sessionId,
            TokenHash = currentTokenHash,
            UsedAt = utcNow
        });

    // 3. Guardar el historial dentro de la transacción.
    await _context.SaveChangesAsync(cancellationToken);

    // 4. Confirmar ambas operaciones conjuntamente.
    await transaction.CommitAsync(cancellationToken);

    return true;
}



}




