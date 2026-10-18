using Microsoft.EntityFrameworkCore;
using NexAccess.Application.DTOs.Audit;
using NexAccess.Application.Interfaces.Repositories;
using NexAccess.Domain.Entities;

namespace NexAccess.Infrastructure.Persistence.Repositories;

public class AuditRepository : IAuditRepository
{
    private readonly NexAccessDbContext _context;

    public AuditRepository(NexAccessDbContext context)
    {
        _context = context;
    }

    public Task<(IReadOnlyList<AuditEntry> Items, int Total)> SearchAsync(AuditQuery query, CancellationToken cancellationToken = default)
    {
        var entries = _context.AuditEntries.AsNoTracking();
        if (query.UserId.HasValue)
        {
            entries = entries.Where(a => a.UserId == query.UserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Entity))
        {
            entries = entries.Where(a => a.Entity == query.Entity);
        }

        if (!string.IsNullOrWhiteSpace(query.Action))
        {
            entries = entries.Where(a => a.Action == query.Action);
        }

        if (query.From.HasValue)
        {
            entries = entries.Where(a => a.OccurredAt >= query.From.Value);
        }

        if (query.To.HasValue)
        {
            entries = entries.Where(a => a.OccurredAt <= query.To.Value);
        }

        return entries.OrderByDescending(a => a.OccurredAt).ThenByDescending(a => a.Id).ToPageAsync(query, cancellationToken);
    }

    public void Add(AuditEntry entry) => _context.AuditEntries.Add(entry);
}
