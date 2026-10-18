using Microsoft.EntityFrameworkCore;
using NexAccess.Application.DTOs.AccessEvents;
using NexAccess.Application.Interfaces.Repositories;
using NexAccess.Domain.Entities;

namespace NexAccess.Infrastructure.Persistence.Repositories;

public class AccessEventRepository : IAccessEventRepository
{
    private readonly NexAccessDbContext _context;

    public AccessEventRepository(NexAccessDbContext context)
    {
        _context = context;
    }

    public Task<(IReadOnlyList<AccessEvent> Items, int Total)> SearchAsync(AccessEventQuery query, CancellationToken cancellationToken = default) =>
        Filter(query).OrderByDescending(e => e.OccurredAt).ThenByDescending(e => e.Id).ToPageAsync(query, cancellationToken);

    public async Task<IReadOnlyList<AccessEvent>> ListAsync(AccessEventQuery query, int limit, CancellationToken cancellationToken = default) =>
        await Filter(query).OrderBy(e => e.OccurredAt).ThenBy(e => e.Id).Take(limit).ToListAsync(cancellationToken);

    public void Add(AccessEvent accessEvent) => _context.AccessEvents.Add(accessEvent);

    private IQueryable<AccessEvent> Filter(AccessEventQuery query)
    {
        var events = _context.AccessEvents.AsNoTracking().Include(e => e.Employee).Include(e => e.Device).Include(e => e.Area).AsQueryable();
        if (query.EmployeeId.HasValue)
        {
            events = events.Where(e => e.EmployeeId == query.EmployeeId.Value);
        }

        if (query.DeviceId.HasValue)
        {
            events = events.Where(e => e.DeviceId == query.DeviceId.Value);
        }

        if (query.AreaId.HasValue)
        {
            events = events.Where(e => e.AreaId == query.AreaId.Value);
        }

        if (query.Result.HasValue)
        {
            events = events.Where(e => e.Result == query.Result.Value);
        }

        if (query.From.HasValue)
        {
            events = events.Where(e => e.OccurredAt >= query.From.Value);
        }

        if (query.To.HasValue)
        {
            events = events.Where(e => e.OccurredAt <= query.To.Value);
        }

        return events;
    }
}
