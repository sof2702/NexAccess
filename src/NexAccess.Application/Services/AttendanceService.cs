using Microsoft.Extensions.Options;
using NexAccess.Application.DTOs.Access;
using NexAccess.Application.DTOs.Attendance;
using NexAccess.Application.DTOs.Common;
using NexAccess.Application.Exceptions;
using NexAccess.Application.Interfaces;
using NexAccess.Application.Interfaces.Repositories;
using NexAccess.Application.Options;
using NexAccess.Application.Services.Reports;
using NexAccess.Application.Validators;
using NexAccess.Domain.Entities;
using NexAccess.Domain.Enums;

namespace NexAccess.Application.Services;

public class AttendanceService
{
    public const int MaxExportRows = 50000;
    private static readonly TimeSpan ManualFutureTolerance = TimeSpan.FromMinutes(5);

    private readonly IAttendanceRepository _attendance;
    private readonly IEmployeeRepository _employees;
    private readonly IUnitOfWork _unitOfWork;
    private readonly AuditService _audit;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;
    private readonly LocalTime _localTime;
    private readonly AttendanceOptions _options;

    public AttendanceService(
        IAttendanceRepository attendance,
        IEmployeeRepository employees,
        IUnitOfWork unitOfWork,
        AuditService audit,
        ICurrentUser currentUser,
        IClock clock,
        LocalTime localTime,
        IOptions<AttendanceOptions> options)
    {
        _attendance = attendance;
        _employees = employees;
        _unitOfWork = unitOfWork;
        _audit = audit;
        _currentUser = currentUser;
        _clock = clock;
        _localTime = localTime;
        _options = options.Value;
    }

    private TimeSpan DuplicateWindow => TimeSpan.FromSeconds(Math.Max(0, _options.DuplicateWindowSeconds));

    public async Task<PagedResult<AttendanceResponse>> SearchAsync(AttendanceQuery query, CancellationToken cancellationToken = default)
    {
        Prepare(query, null);
        var (items, total) = await _attendance.SearchAsync(query, cancellationToken);
        return new PagedResult<AttendanceResponse>(items.Select(AttendanceResponse.From).ToList(), query.Page, query.PageSize, total);
    }

    public async Task<AttendanceMark> RegisterFromDeviceAsync(
        long employeeId,
        long? credentialId,
        long deviceId,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        var last = await _attendance.GetLastForEmployeeAsync(employeeId, cancellationToken);
        if (last is not null && last.OccurredAt <= utcNow && utcNow - last.OccurredAt < DuplicateWindow)
        {
            return new AttendanceMark(last.Type, true, last.OccurredAt);
        }

        var type = NextType(last, utcNow);
        _attendance.Add(new AttendanceRecord
        {
            EmployeeId = employeeId,
            CredentialId = credentialId,
            DeviceId = deviceId,
            Type = type,
            OccurredAt = utcNow,
            Source = AttendanceSource.Dispositivo,
            CreatedAt = utcNow
        });
        return new AttendanceMark(type, false, utcNow);
    }

    public AttendanceType NextType(AttendanceRecord? last, DateTime utcNow) =>
        last is not null && last.Type == AttendanceType.Entrada && _localTime.LocalDate(last.OccurredAt) == _localTime.LocalDate(utcNow)
            ? AttendanceType.Salida
            : AttendanceType.Entrada;

    public async Task<AttendanceResponse> RegisterManualAsync(ManualAttendanceRequest request, CancellationToken cancellationToken = default)
    {
        ModelValidator.Validate(request);
        var employee = await _employees.GetByIdAsync(request.EmployeeId, cancellationToken) ?? throw new NotFoundException("Empleado", request.EmployeeId);
        var now = _clock.UtcNow;
        var occurredAt = DateRange.ToUtc(request.OccurredAt!.Value);
        if (occurredAt > now + ManualFutureTolerance)
        {
            throw new AppValidationException("occurredAt", "No se pueden registrar marcaciones en el futuro.");
        }

        if (await _attendance.ExistsNearAsync(employee.Id, occurredAt, DuplicateWindow, cancellationToken))
        {
            throw new ConflictException("ATTENDANCE_DUPLICATE", "Ya existe una marcaciÃ³n del empleado en ese momento.");
        }

        var record = new AttendanceRecord
        {
            EmployeeId = employee.Id,
            Employee = employee,
            Type = request.Type!.Value,
            OccurredAt = occurredAt,
            Source = AttendanceSource.Manual,
            Note = request.Note.Trim(),
            RegisteredByUserId = _currentUser.UserId,
            CreatedAt = now
        };
        _attendance.Add(record);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _audit.Record("MANUAL_MARK", "marcacion", record.Id, $"empleado={employee.Id};tipo={record.Type};motivo={record.Note}");
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return AttendanceResponse.From(record);
    }

    public async Task<IReadOnlyList<AttendanceDailySummary>> GetDailyReportAsync(AttendanceQuery query, CancellationToken cancellationToken = default)
    {
        Prepare(query, _options.MaxReportDays, requireRange: true);
        var records = await _attendance.ListAsync(query, MaxExportRows, cancellationToken);
        return records
            .GroupBy(record => (record.EmployeeId, Date: _localTime.LocalDate(record.OccurredAt)))
            .Select(group =>
            {
                var ordered = group.OrderBy(record => record.OccurredAt).ToList();
                var entries = ordered.Count(record => record.Type == AttendanceType.Entrada);
                var exits = ordered.Count - entries;
                return new AttendanceDailySummary(
                    group.Key.EmployeeId,
                    ordered[0].Employee?.FullName,
                    group.Key.Date,
                    ordered.FirstOrDefault(record => record.Type == AttendanceType.Entrada)?.OccurredAt,
                    ordered.LastOrDefault(record => record.Type == AttendanceType.Salida)?.OccurredAt,
                    ordered.Count,
                    entries != exits);
            })
            .OrderBy(summary => summary.Date)
            .ThenBy(summary => summary.EmployeeName)
            .ToList();
    }

    public async Task<string> ExportCsvAsync(AttendanceQuery query, CancellationToken cancellationToken = default)
    {
        Prepare(query, _options.MaxReportDays, requireRange: true);
        var records = await _attendance.ListAsync(query, MaxExportRows, cancellationToken);
        _audit.Record("EXPORT", "marcacion", null, $"desde={query.From:O};hasta={query.To:O};filas={records.Count}");
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return CsvBuilder.Build(
            new[] { "id", "empleado_id", "empleado", "tipo", "fecha_hora_local", "origen", "dispositivo", "observacion" },
            records.Select(record => new object?[]
            {
                record.Id,
                record.EmployeeId,
                record.Employee?.FullName,
                record.Type.ToString().ToUpperInvariant(),
                _localTime.ToLocal(record.OccurredAt),
                record.Source.ToString().ToUpperInvariant(),
                record.Device?.Code,
                record.Note
            }));
    }

    private static void Prepare(AttendanceQuery query, int? maxDays, bool requireRange = false)
    {
        ModelValidator.Validate(query);
        if (requireRange && (!query.From.HasValue || !query.To.HasValue))
        {
            throw new AppValidationException("from", "Debe indicar el rango de fechas (from y to).");
        }

        query.From = DateRange.ToUtc(query.From);
        query.To = DateRange.ToUtc(query.To);
        DateRange.Ensure(query.From, query.To, maxDays);
    }
}
