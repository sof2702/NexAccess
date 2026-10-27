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

