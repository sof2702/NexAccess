using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexAccess.Application.DTOs.Attendance;
using NexAccess.Application.DTOs.Common;
using NexAccess.Application.Services;
using NexAccess.Domain.Enums;

namespace NexAccess.API.Controllers;

[ApiController]
[Route("api/v1/attendance")]
[Authorize(Roles = SystemRoles.AdminOrRrhh)]
public class AttendanceController(AttendanceService attendance) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<AttendanceResponse>> Search([FromQuery] AttendanceQuery query, CancellationToken cancellationToken) =>
        attendance.SearchAsync(query, cancellationToken);

    [HttpPost]
    public async Task<ActionResult<AttendanceResponse>> RegisterManual(ManualAttendanceRequest request, CancellationToken cancellationToken)
    {
        var record = await attendance.RegisterManualAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, record);
    }

    [HttpGet("report")]
    public Task<IReadOnlyList<AttendanceDailySummary>> Report([FromQuery] AttendanceQuery query, CancellationToken cancellationToken) =>
        attendance.GetDailyReportAsync(query, cancellationToken);

    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] AttendanceQuery query, CancellationToken cancellationToken)
    {
        var csv = await attendance.ExportCsvAsync(query, cancellationToken);
        return File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray(), "text/csv; charset=utf-8", $"marcaciones_{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
    }
}
