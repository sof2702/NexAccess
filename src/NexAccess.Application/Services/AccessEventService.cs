using Microsoft.Extensions.Options;
using NexAccess.Application.DTOs.AccessEvents;
using NexAccess.Application.DTOs.Common;
using NexAccess.Application.Exceptions;
using NexAccess.Application.Interfaces;
using NexAccess.Application.Interfaces.Repositories;
using NexAccess.Application.Options;
using NexAccess.Application.Services.Reports;
using NexAccess.Application.Validators;

namespace NexAccess.Application.Services;

public class AccessEventService
{
    private readonly IAccessEventRepository _events;
    private readonly IUnitOfWork _unitOfWork;
    private readonly AuditService _audit;
    private readonly LocalTime _localTime;
    private readonly AttendanceOptions _options;

    public AccessEventService(
        IAccessEventRepository events,
        IUnitOfWork unitOfWork,
        AuditService audit,
        LocalTime localTime,
        IOptions<AttendanceOptions> options)
    {
        _events = events;
        _unitOfWork = unitOfWork;
        _audit = audit;
        _localTime = localTime;
        _options = options.Value;
    }

    public async Task<PagedResult<AccessEventResponse>> SearchAsync(AccessEventQuery query, CancellationToken cancellationToken = default)
    {
        Prepare(query, false);
        var (items, total) = await _events.SearchAsync(query, cancellationToken);
        return new PagedResult<AccessEventResponse>(items.Select(AccessEventResponse.From).ToList(), query.Page, query.PageSize, total);
    }

    public async Task<string> ExportCsvAsync(AccessEventQuery query, CancellationToken cancellationToken = default)
    {
        Prepare(query, true);
        var items = await _events.ListAsync(query, AttendanceService.MaxExportRows, cancellationToken);
        _audit.Record("EXPORT", "evento_acceso", null, $"desde={query.From:O};hasta={query.To:O};filas={items.Count}");
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return CsvBuilder.Build(
            new[] { "id", "fecha_hora_local", "resultado", "motivo", "empleado_id", "empleado", "dispositivo", "area", "credencial" },
            items.Select(AccessEventResponse.From).Select(item => new object?[]
            {
                item.Id,
                _localTime.ToLocal(item.OccurredAt),
                item.Result.ToString().ToUpperInvariant(),
                item.Reason,
                item.EmployeeId,
                item.EmployeeName,
                item.DeviceCode,
                item.AreaName,
                item.MaskedCredential
            }));
    }

    private void Prepare(AccessEventQuery query, bool requireRange)
    {
        ModelValidator.Validate(query);
        if (requireRange && (!query.From.HasValue || !query.To.HasValue))
        {
            throw new AppValidationException("from", "Debe indicar el rango de fechas (from y to).");
        }

        query.From = DateRange.ToUtc(query.From);
        query.To = DateRange.ToUtc(query.To);
        DateRange.Ensure(query.From, query.To, requireRange ? _options.MaxReportDays : null);
    }
}
