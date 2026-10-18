using Microsoft.EntityFrameworkCore;
using NexAccess.Application.DTOs.Permissions;
using NexAccess.Application.Interfaces.Repositories;
using NexAccess.Domain.Entities;

namespace NexAccess.Infrastructure.Persistence.Repositories;

public class AccessPermissionRepository : IAccessPermissionRepository
{
    private readonly NexAccessDbContext _context;

    public AccessPermissionRepository(NexAccessDbContext context)
    {
        _context = context;
    }

    public Task<AccessPermission?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        _context.AccessPermissions.Include(p => p.Role).Include(p => p.Area).FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(long roleId, long areaId, long? excludeId, CancellationToken cancellationToken = default) =>
        _context.AccessPermissions.AnyAsync(p => p.RoleId == roleId && p.AreaId == areaId && (excludeId == null || p.Id != excludeId), cancellationToken);

    public Task<(IReadOnlyList<AccessPermission> Items, int Total)> SearchAsync(AccessPermissionQuery query, CancellationToken cancellationToken = default)
    {
        var permissions = _context.AccessPermissions.AsNoTracking().Include(p => p.Role).Include(p => p.Area).AsQueryable();
        if (query.RoleId.HasValue)
        {
            permissions = permissions.Where(p => p.RoleId == query.RoleId.Value);
        }

        if (query.AreaId.HasValue)
        {
            permissions = permissions.Where(p => p.AreaId == query.AreaId.Value);
        }

        if (query.IsActive.HasValue)
        {
            permissions = permissions.Where(p => p.IsActive == query.IsActive.Value);
        }

        return permissions.OrderBy(p => p.AreaId).ThenBy(p => p.RoleId).ToPageAsync(query, cancellationToken);
    }

    public async Task<IReadOnlyList<AccessPermission>> GetActiveForEmployeeAndAreaAsync(long employeeId, long areaId, CancellationToken cancellationToken = default) =>
        await _context.AccessPermissions.AsNoTracking()
            .Where(p => p.AreaId == areaId && p.IsActive && p.Role!.IsActive)
            .Where(p => _context.EmployeeRoles.Any(link => link.EmployeeId == employeeId && link.RoleId == p.RoleId))
            .ToListAsync(cancellationToken);

    public void Add(AccessPermission permission) => _context.AccessPermissions.Add(permission);
}
