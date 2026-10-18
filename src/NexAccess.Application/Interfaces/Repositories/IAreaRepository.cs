using NexAccess.Application.DTOs.Areas;
using NexAccess.Domain.Entities;

namespace NexAccess.Application.Interfaces.Repositories;

public interface IAreaRepository
{
    Task<Area?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<bool> CodeExistsAsync(string code, long? excludeId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Area> Items, int Total)> SearchAsync(AreaQuery query, CancellationToken cancellationToken = default);
    void Add(Area area);
}
