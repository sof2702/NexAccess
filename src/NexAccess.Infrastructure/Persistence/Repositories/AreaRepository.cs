using Microsoft.EntityFrameworkCore;
using NexAccess.Application.DTOs.Areas;
using NexAccess.Application.Interfaces.Repositories;
using NexAccess.Domain.Entities;

namespace NexAccess.Infrastructure.Persistence.Repositories;

public class AreaRepository : IAreaRepository
{
    private readonly NexAccessDbContext _context;

    public AreaRepository(NexAccessDbContext context)
    {
        _context = context;
    }

    public Task<Area?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        _context.Areas.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public Task<bool> CodeExistsAsync(string code, long? excludeId, CancellationToken cancellationToken = default) =>
        _context.Areas.AnyAsync(a => a.Code == code && (excludeId == null || a.Id != excludeId), cancellationToken);

    public Task<(IReadOnlyList<Area> Items, int Total)> SearchAsync(AreaQuery query, CancellationToken cancellationToken = default)
    {
        var areas = _context.Areas.AsNoTracking();
        if (query.IsActive.HasValue)
        {
            areas = areas.Where(a => a.IsActive == query.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = query.Search.ToLikePattern();
            areas = areas.Where(a => EF.Functions.ILike(a.Code, pattern) || EF.Functions.ILike(a.Name, pattern));
        }

        return areas.OrderBy(a => a.Name).ThenBy(a => a.Id).ToPageAsync(query, cancellationToken);
    }

    public void Add(Area area) => _context.Areas.Add(area);
}
