using NexAccess.Domain.Entities;

namespace NexAccess.Application.Interfaces.Repositories;

public interface IRoleRepository
{
    Task<Role?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<bool> CodeExistsAsync(string code, long? excludeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Role>> ListAsync(bool? isActive, CancellationToken cancellationToken = default);
    void Add(Role role);
}
