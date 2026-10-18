using NexAccess.Application.DTOs.Audit;
using NexAccess.Domain.Entities;

namespace NexAccess.Application.Interfaces.Repositories;

public interface IAuditRepository
{
    Task<(IReadOnlyList<AuditEntry> Items, int Total)> SearchAsync(AuditQuery query, CancellationToken cancellationToken = default);
    void Add(AuditEntry entry);
}
