using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexAccess.Application.DTOs.Roles;
using NexAccess.Application.Services;
using NexAccess.Domain.Enums;

namespace NexAccess.API.Controllers;

[ApiController]
[Route("api/v1/roles")]
[Authorize(Roles = SystemRoles.All)]
public class RolesController(RoleService roles) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<RoleResponse>> List([FromQuery] bool? isActive, CancellationToken cancellationToken) =>
        roles.ListAsync(isActive, cancellationToken);

    [HttpGet("{id:long}")]
    public Task<RoleResponse> Get(long id, CancellationToken cancellationToken) =>
        roles.GetAsync(id, cancellationToken);

    [HttpPost]
    [Authorize(Roles = SystemRoles.AdminOrSeguridad)]
    public async Task<ActionResult<RoleResponse>> Create(RoleRequest request, CancellationToken cancellationToken)
    {
        var role = await roles.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = role.Id }, role);
    }

    [HttpPut("{id:long}")]
    [Authorize(Roles = SystemRoles.AdminOrSeguridad)]
    public Task<RoleResponse> Update(long id, RoleRequest request, CancellationToken cancellationToken) =>
        roles.UpdateAsync(id, request, cancellationToken);
}
