using Microsoft.EntityFrameworkCore;
using NexAccess.Application.Interfaces.Repositories;
using NexAccess.Domain.Entities;

namespace NexAccess.Infrastructure.Persistence.Repositories;

public class RoleRepository : IRoleRepository
{
    private readonly NexAccessDbContext _context;

    public RoleRepository(NexAccessDbContext context)
    {
        _context = context;
    }

    public Task<Role?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        _context.Roles.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public Task<bool> CodeExistsAsync(string code, long? excludeId, CancellationToken cancellationToken = default) =>
        _context.Roles.AnyAsync(r => r.Code == code && (excludeId == null || r.Id != excludeId), cancellationToken);

    public async Task<IReadOnlyList<Role>> ListAsync(bool? isActive, CancellationToken cancellationToken = default) =>
        await _context.Roles.AsNoTracking()
            .Where(r => isActive == null || r.IsActive == isActive)
            .OrderBy(r => r.Code)
            .ToListAsync(cancellationToken);

    public void Add(Role role) => _context.Roles.Add(role);
}
