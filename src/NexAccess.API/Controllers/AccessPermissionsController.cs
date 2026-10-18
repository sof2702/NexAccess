using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexAccess.Application.DTOs.Common;
using NexAccess.Application.DTOs.Permissions;
using NexAccess.Application.Services;
using NexAccess.Domain.Enums;

namespace NexAccess.API.Controllers;

[ApiController]
[Route("api/v1/access-permissions")]
[Authorize(Roles = SystemRoles.AdminOrSeguridad)]
public class AccessPermissionsController(AccessPermissionService permissions) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<AccessPermissionResponse>> Search([FromQuery] AccessPermissionQuery query, CancellationToken cancellationToken) =>
        permissions.SearchAsync(query, cancellationToken);

    [HttpGet("{id:long}")]
    public Task<AccessPermissionResponse> Get(long id, CancellationToken cancellationToken) =>
        permissions.GetAsync(id, cancellationToken);

    [HttpPost]
    public async Task<ActionResult<AccessPermissionResponse>> Create(AccessPermissionRequest request, CancellationToken cancellationToken)
    {
        var permission = await permissions.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = permission.Id }, permission);
    }

    [HttpPut("{id:long}")]
    public Task<AccessPermissionResponse> Update(long id, AccessPermissionRequest request, CancellationToken cancellationToken) =>
        permissions.UpdateAsync(id, request, cancellationToken);

    [HttpPatch("{id:long}/status")]
    public Task<AccessPermissionResponse> SetStatus(long id, StatusRequest request, CancellationToken cancellationToken) =>
        permissions.SetStatusAsync(id, request, cancellationToken);
}
