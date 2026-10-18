using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexAccess.Application.DTOs.AccessEvents;
using NexAccess.Application.DTOs.Common;
using NexAccess.Application.Services;
using NexAccess.Domain.Enums;

namespace NexAccess.API.Controllers;

[ApiController]
[Route("api/v1/access-events")]
[Authorize(Roles = SystemRoles.AdminOrSeguridad)]
public class AccessEventsController(AccessEventService events) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<AccessEventResponse>> Search([FromQuery] AccessEventQuery query, CancellationToken cancellationToken) =>
        events.SearchAsync(query, cancellationToken);

    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] AccessEventQuery query, CancellationToken cancellationToken)
    {
        var csv = await events.ExportCsvAsync(query, cancellationToken);
        return File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray(), "text/csv; charset=utf-8", $"eventos_acceso_{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
    }
}
