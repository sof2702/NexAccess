using NexAccess.Application.DTOs.AccessEvents;
using NexAccess.Domain.Entities;

namespace NexAccess.Application.Interfaces.Repositories;

public interface IAccessEventRepository
{
    Task<(IReadOnlyList<AccessEvent> Items, int Total)> SearchAsync(AccessEventQuery query, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AccessEvent>> ListAsync(AccessEventQuery query, int limit, CancellationToken cancellationToken = default);
    void Add(AccessEvent accessEvent);
}
