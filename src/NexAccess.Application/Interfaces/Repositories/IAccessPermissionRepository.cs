using NexAccess.Application.DTOs.Permissions;
using NexAccess.Domain.Entities;

namespace NexAccess.Application.Interfaces.Repositories;

public interface IAccessPermissionRepository
{
    Task<AccessPermission?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(long roleId, long areaId, long? excludeId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<AccessPermission> Items, int Total)> SearchAsync(AccessPermissionQuery query, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AccessPermission>> GetActiveForEmployeeAndAreaAsync(long employeeId, long areaId, CancellationToken cancellationToken = default);
    void Add(AccessPermission permission);
}
