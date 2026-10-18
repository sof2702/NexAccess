using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexAccess.Application.DTOs.Areas;
using NexAccess.Application.DTOs.Common;
using NexAccess.Application.Services;
using NexAccess.Domain.Enums;

namespace NexAccess.API.Controllers;

[ApiController]
[Route("api/v1/areas")]
[Authorize(Roles = SystemRoles.All)]
public class AreasController(AreaService areas) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<AreaResponse>> Search([FromQuery] AreaQuery query, CancellationToken cancellationToken) =>
        areas.SearchAsync(query, cancellationToken);

    [HttpGet("{id:long}")]
    public Task<AreaResponse> Get(long id, CancellationToken cancellationToken) =>
        areas.GetAsync(id, cancellationToken);

    [HttpPost]
    [Authorize(Roles = SystemRoles.AdminOrSeguridad)]
    public async Task<ActionResult<AreaResponse>> Create(AreaRequest request, CancellationToken cancellationToken)
    {
        var area = await areas.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = area.Id }, area);
    }

    [HttpPut("{id:long}")]
    [Authorize(Roles = SystemRoles.AdminOrSeguridad)]
    public Task<AreaResponse> Update(long id, AreaRequest request, CancellationToken cancellationToken) =>
        areas.UpdateAsync(id, request, cancellationToken);
}
