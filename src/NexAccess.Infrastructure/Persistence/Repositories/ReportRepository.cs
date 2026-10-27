
using Microsoft.EntityFrameworkCore;
using NexAccess.Application.DTOs.Reports;
using NexAccess.Application.Interfaces.Repositories;
using NexAccess.Domain.Entities;

namespace NexAccess.Infrastructure.Persistence.Repositories;

public sealed class ReportRepository : IReportRepository
{
    private readonly NexAccessDbContext _context;

    public ReportRepository(NexAccessDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<AttendanceRecord>> GetAttendanceAsync(
        ReportRequest request,
        int maxRows,
        CancellationToken cancellationToken = default)
    {
        if (maxRows <= 0 || maxRows == int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(maxRows));

        var query = _context.AttendanceRecords
            .AsNoTracking()
            .Include(x => x.Employee)
            .Include(x => x.Device)
            .AsQueryable();

        if (request.From.HasValue)
            query = query.Where(x => x.OccurredAt >= request.From.Value);

        if (request.To.HasValue)
            query = query.Where(x => x.OccurredAt <= request.To.Value);

        if (request.EmployeeIds.Count > 0)
            query = query.Where(x =>
                request.EmployeeIds.Contains(x.EmployeeId));

        if (request.DeviceIds.Count > 0)
            query = query.Where(x =>
                x.DeviceId.HasValue &&
                request.DeviceIds.Contains(x.DeviceId.Value));

        if (request.AttendanceTypes.Count > 0)
            query = query.Where(x =>
                request.AttendanceTypes.Contains(x.Type));

        if (request.AttendanceSources.Count > 0)
            query = query.Where(x =>
                request.AttendanceSources.Contains(x.Source));

        var records = await query
            .OrderBy(x => x.EmployeeId)
            .ThenBy(x => x.OccurredAt)
            .ThenBy(x => x.Id)
            .Take(maxRows + 1)
            .ToListAsync(cancellationToken);

        if (records.Count > maxRows)
            throw new InvalidOperationException(
                $"El reporte supera {maxRows:N0} registros. " +
                "Reducí el período o aplicá filtros adicionales.");

        return records;
    }

    public async Task<IReadOnlyList<AccessEvent>> GetAccessEventsAsync(
        ReportRequest request,
        int maxRows,
        CancellationToken cancellationToken = default)
    {
        if (maxRows <= 0 || maxRows == int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(maxRows));

        var query = _context.AccessEvents
            .AsNoTracking()
            .Include(x => x.Employee)
            .Include(x => x.Device)
            .Include(x => x.Area)
            .AsQueryable();

        if (request.From.HasValue)
            query = query.Where(x => x.OccurredAt >= request.From.Value);

        if (request.To.HasValue)
            query = query.Where(x => x.OccurredAt <= request.To.Value);

        if (request.EmployeeIds.Count > 0)
            query = query.Where(x =>
                x.EmployeeId.HasValue &&
                request.EmployeeIds.Contains(x.EmployeeId.Value));

        if (request.DeviceIds.Count > 0)
            query = query.Where(x =>
                request.DeviceIds.Contains(x.DeviceId));

        if (request.AreaIds.Count > 0)
            query = query.Where(x =>
                x.AreaId.HasValue &&
                request.AreaIds.Contains(x.AreaId.Value));

        if (request.AccessResults.Count > 0)
            query = query.Where(x =>
                request.AccessResults.Contains(x.Result));

        var events = await query
            .OrderBy(x => x.OccurredAt)
            .ThenBy(x => x.Id)
            .Take(maxRows + 1)
            .ToListAsync(cancellationToken);

        if (events.Count > maxRows)
            throw new InvalidOperationException(
                $"El reporte supera {maxRows:N0} registros. " +
                "Reducí el período o aplicá filtros adicionales.");

        return events;
    }
}

