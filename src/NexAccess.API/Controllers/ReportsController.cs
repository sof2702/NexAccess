using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexAccess.Application.DTOs.Reports;
using NexAccess.Application.Exceptions;
using NexAccess.Application.Interfaces;
using NexAccess.Application.Services.Reports;
using NexAccess.Domain.Enums;
using NexAccess.Application.Services;
using System.Globalization;

namespace NexAccess.API.Controllers;

[ApiController]
[Route("api/v1/reports")]
[Authorize(Roles = SystemRoles.All)]

public sealed class ReportsController(
    ReportService reports,
    IPdfReportGenerator pdfGenerator,
    AuditService audit,
    IUnitOfWork unitOfWork) : ControllerBase
{

[HttpGet("options")]
public IActionResult GetOptions()
{
    var isAdmin = User.IsInRole(SystemRoles.Admin);
    var isRrhh = User.IsInRole(SystemRoles.Rrhh);
    var isSeguridad = User.IsInRole(SystemRoles.Seguridad);

    var availableReports = new List<object>();

    if (isAdmin || isRrhh)
    {
        availableReports.Add(new
        {
            type = ReportType.Attendance.ToString(),
            name = "Reporte de asistencia",
            description = "Entradas, salidas y marcaciones de empleados.",
            formats = new[] { "Json", "Pdf" },
            filters = new[]
            {
                "from", "to", "employeeIds", "deviceIds",
                "attendanceTypes", "attendanceSources"
            },
            groupBy = new[]
            {
                "Employee", "Day", "Week", "Month", "Device"
            },
            columns = new[]
            {
                "occurredAt", "employeeId", "employeeName",
                "type", "source", "deviceCode", "note"
            }
        });
    }

    if (isAdmin || isSeguridad)
    {
        availableReports.Add(new
        {
            type = ReportType.AccessEvents.ToString(),
            name = "Reporte de accesos",
            description = "Accesos autorizados y rechazados por área.",
            formats = new[] { "Json", "Pdf" },
            filters = new[]
            {
                "from", "to", "employeeIds", "areaIds",
                "deviceIds", "accessResults"
            },
            groupBy = new[]
            {
                "Employee", "Day", "Week", "Month",
                "Area", "Device"
            },
            columns = new[]
            {
                "occurredAt", "employeeId", "employeeName",
                "result", "reason", "areaName", "deviceCode"
            }
        });
    }

    return Ok(new
    {
        reports = availableReports,
        maxReportDays = 366,
        defaultFormat = "Pdf",
        supportsSummary = true,
        supportsDetails = true
    });
}




    [HttpPost("generate")]
    public async Task<IActionResult> Generate(
        [FromBody] ReportRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new AppValidationException(
                "request",
                "La solicitud es obligatoria.");
        }

        // Verificar permisos antes de consultar PostgreSQL.
        var allowed = request.Type switch
        {
            ReportType.Attendance =>
                User.IsInRole(SystemRoles.Admin) ||
                User.IsInRole(SystemRoles.Rrhh),

            ReportType.AccessEvents =>
                User.IsInRole(SystemRoles.Admin) ||
                User.IsInRole(SystemRoles.Seguridad),

            _ => false
        };

        if (!allowed)
        {
            throw new ForbiddenException(
                "REPORT_ACCESS_DENIED",
                "No tiene permisos para generar este reporte.");
        }

        if (request.Format is not
            (ReportFormat.Json or ReportFormat.Pdf))
        {
            throw new AppValidationException(
                "format",
                "Por ahora se admiten JSON y PDF.");
        }

        // Validación y consulta mediante ReportService.
        var report = await reports.GenerateAsync(
            request, cancellationToken);

        if (request.Format == ReportFormat.Json)
            return Ok(report);

        var pdf = pdfGenerator.Generate(report, request);


	// Registrar únicamente una exportación PDF generada correctamente.
var detail = string.Join(";",
    $"tipo={request.Type}",
    $"formato={request.Format}",
    $"desde={request.From:O}",
    $"hasta={request.To:O}",
    $"filas={report.TotalRecords.ToString(CultureInfo.InvariantCulture)}",
    $"empleados={request.EmployeeIds.Count}",
    $"areas={request.AreaIds.Count}",
    $"dispositivos={request.DeviceIds.Count}",
    $"agrupaciones={string.Join(",", request.GroupBy)}"
);

	audit.Record(
		"REPORT_EXPORT",
		"reporte",
		null,
		detail);

	await unitOfWork.SaveChangesAsync(cancellationToken);

        var fileName =
            $"nexaccess_{request.Type.ToString().ToLowerInvariant()}_" +
            $"{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf";

        return File(
            pdf,
            "application/pdf",
            fileName);
    }
}

