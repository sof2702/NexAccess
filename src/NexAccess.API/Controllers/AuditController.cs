using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexAccess.Application.DTOs.Audit;
using NexAccess.Application.DTOs.Common;
using NexAccess.Application.Services;
using NexAccess.Domain.Enums;

namespace NexAccess.API.Controllers;

[ApiController]
[Route("api/v1/audit")]
[Authorize(Roles = SystemRoles.Admin)]
public class AuditController(AuditService audit) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<AuditResponse>> Search([FromQuery] AuditQuery query, CancellationToken cancellationToken) =>
        audit.SearchAsync(query, cancellationToken);
}
