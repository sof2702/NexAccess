using NexAccess.Application.DTOs.Reports;
using NexAccess.Application.Exceptions;

namespace NexAccess.Application.Validators;

public static class ReportColumnValidator
{
    private static readonly HashSet<string> AttendanceColumns =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "occurredAt",
            "employeeId",
            "employeeName",
            "type",
            "source",
            "deviceCode",
            "note"
        };

    private static readonly HashSet<string> AccessColumns =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "occurredAt",
            "employeeId",
            "employeeName",
            "result",
            "reason",
            "areaName",
            "deviceCode"
        };

    public static void Validate(ReportRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Columns is null || request.Columns.Count == 0)
            return;

        var allowed = request.Type switch
        {
            ReportType.Attendance => AttendanceColumns,
            ReportType.AccessEvents => AccessColumns,
            _ => throw new AppValidationException(
                "type",
                "Tipo de reporte no compatible con columnas.")
        };

        foreach (var column in request.Columns)
        {
            if (string.IsNullOrWhiteSpace(column) ||
                !allowed.Contains(column))
            {
                throw new AppValidationException(
                    "columns",
                    $"La columna '{column}' no está permitida para este reporte.");
            }
        }
    }
}

