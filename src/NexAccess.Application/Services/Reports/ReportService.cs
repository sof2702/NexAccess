using NexAccess.Application.DTOs.Reports;
using NexAccess.Application.Exceptions;
using NexAccess.Application.Interfaces.Repositories;
using NexAccess.Application.Validators;

namespace NexAccess.Application.Services.Reports;

public sealed class ReportService
{
    private const int MaxReportRows = 50000;

    private readonly IReportRepository _repository;
    private readonly ReportGroupingService _grouping;

    public ReportService(
        IReportRepository repository,
        ReportGroupingService grouping)
    {
        _repository = repository;
        _grouping = grouping;
    }

    public async Task<ReportResponse> GenerateAsync(
        ReportRequest request,
        CancellationToken cancellationToken = default)
    {
        ReportRequestValidator.Validate(request);
	ReportColumnValidator.Validate(request);
        ValidateGroupings(request);
		


        List<ReportRow> rows;

        switch (request.Type)
        {
            case ReportType.Attendance:
            {
                var records = await _repository.GetAttendanceAsync(
                    request,
                    MaxReportRows,
                    cancellationToken);

                rows = records
                    .Select(ReportMapper.FromAttendance)
                    .ToList();

                break;
            }

            case ReportType.AccessEvents:
            {
                var events = await _repository.GetAccessEventsAsync(
                    request,
                    MaxReportRows,
                    cancellationToken);

                rows = events
                    .Select(ReportMapper.FromAccessEvent)
                    .ToList();

                break;
            }

            default:
                throw new AppValidationException(
                    "type",
                    "El tipo de reporte todavía no está disponible.");
        }

        var groups = request.IncludeSummary
            ? _grouping.Group(rows, request.GroupBy)
            : new List<ReportGroupResult>();

        return new ReportResponse
        {
            Type = request.Type,
            GeneratedAt = DateTime.UtcNow,
            From = request.From!.Value,
            To = request.To!.Value,
            TotalRecords = rows.Count,
            Rows = request.IncludeDetails
                ? rows
                : new List<ReportRow>(),
            Groups = groups
        };
    }

    private static void ValidateGroupings(ReportRequest request)
    {
        if (request.GroupBy.Contains(ReportGroupBy.None)
            && request.GroupBy.Count > 1)
        {
            throw new AppValidationException(
                "groupBy",
                "None no puede combinarse con otras agrupaciones.");
        }

        if (request.Type == ReportType.Attendance &&
            request.GroupBy.Contains(ReportGroupBy.Area))
        {
            throw new AppValidationException(
                "groupBy",
                "Los reportes de asistencia no admiten agrupación por área.");
        }
    }
}

