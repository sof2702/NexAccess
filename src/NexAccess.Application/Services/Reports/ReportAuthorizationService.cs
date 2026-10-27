
using NexAccess.Application.DTOs.Reports;
using NexAccess.Application.Exceptions;
using NexAccess.Domain.Enums;

namespace NexAccess.Application.Services.Reports;

public static class ReportAuthorizationService
{
    public static void EnsureAllowed(
        ReportType reportType,
        string? role)
    {
        var allowed = reportType switch
        {
            ReportType.Attendance =>
                role is SystemRoles.Admin or SystemRoles.Rrhh,

            ReportType.AccessEvents =>
                role is SystemRoles.Admin or SystemRoles.Seguridad,

            _ => false
        };

        if (!allowed)
        {
            throw new ForbiddenException(
                "REPORT_ACCESS_DENIED",
                "No tiene permisos para generar este tipo de reporte.");
        }
    }
}

