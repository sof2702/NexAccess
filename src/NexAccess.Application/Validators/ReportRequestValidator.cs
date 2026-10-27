using NexAccess.Application.DTOs.Reports;
using NexAccess.Application.Exceptions;

namespace NexAccess.Application.Validators;

public static class ReportRequestValidator
{
    private const int MaxReportDays = 366;
    private const int MaxFilterIds = 500;
    private const int MaxColumns = 30;

    public static void Validate(ReportRequest request)
    {
        if (request is null)
            throw new AppValidationException(
                "request", "La solicitud es obligatoria.");

        if (!Enum.IsDefined(request.Type))
            throw new AppValidationException(
                "type", "Tipo de reporte inválido.");

        if (!Enum.IsDefined(request.Format))
            throw new AppValidationException(
                "format", "Formato de reporte inválido.");

        if (!request.From.HasValue || !request.To.HasValue)
            throw new AppValidationException(
                "from", "Debe indicar las fechas desde y hasta.");

        // Reutilizamos la validación de fechas del proyecto.
        var from = DateRange.ToUtc(request.From);
        var to = DateRange.ToUtc(request.To);

        DateRange.Ensure(from, to, MaxReportDays);

        // Normalizamos fechas a UTC.
        request.From = from;
        request.To = to;

        request.EmployeeIds ??= new();
        request.AreaIds ??= new();
        request.DeviceIds ??= new();
        request.AttendanceTypes ??= new();
        request.AttendanceSources ??= new();
        request.AccessResults ??= new();
        request.GroupBy ??= new();
        request.Columns ??= new();

        ValidateIds(request.EmployeeIds, "employeeIds");
        ValidateIds(request.AreaIds, "areaIds");
        ValidateIds(request.DeviceIds, "deviceIds");

        ValidateEnums(request.AttendanceTypes, "attendanceTypes");
        ValidateEnums(request.AttendanceSources, "attendanceSources");
        ValidateEnums(request.AccessResults, "accessResults");
        ValidateEnums(request.GroupBy, "groupBy");

        if (request.GroupBy.Count != request.GroupBy.Distinct().Count())
            throw new AppValidationException(
                "groupBy", "No se pueden repetir agrupaciones.");

        if (request.Columns.Count > MaxColumns)
            throw new AppValidationException(
                "columns", "Se superó el máximo de columnas.");

        if (request.Columns.Any(
            c => string.IsNullOrWhiteSpace(c) || c.Length > 64))
            throw new AppValidationException(
                "columns", "Hay nombres de columnas inválidos.");

        if (request.Columns.Count !=
            request.Columns.Distinct(StringComparer.OrdinalIgnoreCase).Count())
            throw new AppValidationException(
                "columns", "No se pueden repetir columnas.");

        if (!request.IncludeDetails && !request.IncludeSummary)
            throw new AppValidationException(
                "includeDetails",
                "Debe incluir al menos el detalle o el resumen.");

        // Combinaciones específicas por tipo.
        switch (request.Type)
        {
            case ReportType.Attendance:
                if (request.AreaIds.Count > 0 ||
                    request.AccessResults.Count > 0)
                    throw new AppValidationException(
                        "type",
                        "El reporte de asistencia no admite filtros de áreas ni resultados de acceso.");

                if (request.GroupBy.Contains(ReportGroupBy.Area))
                    throw new AppValidationException(
                        "groupBy",
                        "No se puede agrupar asistencia por área.");
                break;

            case ReportType.AccessEvents:
                if (request.AttendanceTypes.Count > 0 ||
                    request.AttendanceSources.Count > 0)
                    throw new AppValidationException(
                        "type",
                        "El reporte de accesos no admite filtros de marcación.");
                break;

            case ReportType.Incidents:
            case ReportType.Combined:
                // Se habilitarán cuando implementemos
                // las consultas específicas.
                throw new AppValidationException(
                    "type",
                    "Este tipo de reporte todavía no está implementado.");
        }
    }

    private static void ValidateIds(List<long> ids, string field)
    {
        if (ids.Count > MaxFilterIds)
            throw new AppValidationException(
                field, "Demasiados identificadores seleccionados.");

        if (ids.Any(id => id <= 0))
            throw new AppValidationException(
                field, "Todos los identificadores deben ser positivos.");
    }

    private static void ValidateEnums<TEnum>(
        List<TEnum> values,
        string field) where TEnum : struct, Enum
    {
        if (values.Any(value => !Enum.IsDefined(value)))
            throw new AppValidationException(
                field, "Se encontraron valores inválidos.");
    }
}

