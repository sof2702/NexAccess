using Microsoft.EntityFrameworkCore;
using NexAccess.Application.DTOs.Attendance;
using NexAccess.Application.Interfaces.Repositories;
using NexAccess.Domain.Entities;

namespace NexAccess.Infrastructure.Persistence.Repositories;

public class AttendanceRepository : IAttendanceRepository
{
    private readonly NexAccessDbContext _context;

    public AttendanceRepository(NexAccessDbContext context)
    {
        _context = context;
    }

    public Task<AttendanceRecord?> GetLastForEmployeeAsync(long employeeId, CancellationToken cancellationToken = default) =>
        _context.AttendanceRecords.AsNoTracking()
            .Where(m => m.EmployeeId == employeeId)
            .OrderByDescending(m => m.OccurredAt)
            .ThenByDescending(m => m.Id)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<bool> ExistsNearAsync(long employeeId, DateTime occurredAt, TimeSpan window, CancellationToken cancellationToken = default)
    {
        var from = occurredAt - window;
        var to = occurredAt + window;
        return _context.AttendanceRecords.AnyAsync(m => m.EmployeeId == employeeId && m.OccurredAt >= from && m.OccurredAt <= to, cancellationToken);
    }

    public Task<(IReadOnlyList<AttendanceRecord> Items, int Total)> SearchAsync(AttendanceQuery query, CancellationToken cancellationToken = default) =>
        Filter(query).OrderByDescending(m => m.OccurredAt).ThenByDescending(m => m.Id).ToPageAsync(query, cancellationToken);

    public async Task<IReadOnlyList<AttendanceRecord>> ListAsync(
    AttendanceQuery query,
    int limit,
    CancellationToken cancellationToken = default)
{
    if (limit <= 0 || limit == int.MaxValue)
        throw new ArgumentOutOfRangeException(nameof(limit));

    var records = await Filter(query)
        .OrderBy(m => m.EmployeeId)
        .ThenBy(m => m.OccurredAt)
        .ThenBy(m => m.Id)
        .Take(limit + 1)
        .ToListAsync(cancellationToken);

    if (records.Count > limit)
    {
        throw new InvalidOperationException(
            $"El reporte supera el máximo permitido de {limit:N0} registros. " +
            "Reducí el rango de fechas o aplicá filtros adicionales.");
    }

    return records;
}

    public void Add(AttendanceRecord record) => _context.AttendanceRecords.Add(record);

    private IQueryable<AttendanceRecord> Filter(AttendanceQuery query)
    {
        var records = _context.AttendanceRecords.AsNoTracking().Include(m => m.Employee).Include(m => m.Device).AsQueryable();
        if (query.EmployeeId.HasValue)
        {
            records = records.Where(m => m.EmployeeId == query.EmployeeId.Value);
        }

        if (query.Type.HasValue)
        {
            records = records.Where(m => m.Type == query.Type.Value);
        }

        if (query.Source.HasValue)
        {
            records = records.Where(m => m.Source == query.Source.Value);
        }

        if (query.From.HasValue)
        {
            records = records.Where(m => m.OccurredAt >= query.From.Value);
        }

        if (query.To.HasValue)
        {
            records = records.Where(m => m.OccurredAt <= query.To.Value);
        }

        return records;
    }
}
