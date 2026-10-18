using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexAccess.Application.DTOs.AccessEvents;
using NexAccess.Application.DTOs.Common;
using NexAccess.Application.DTOs.Devices;
using NexAccess.Application.Services;
using NexAccess.Domain.Enums;

namespace NexAccess.API.Controllers;

[ApiController]
[Route("api/v1/devices")]
[Authorize(Roles = SystemRoles.AdminOrSeguridad)]
public class DevicesController(DeviceService devices) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<DeviceResponse>> Search([FromQuery] DeviceQuery query, CancellationToken cancellationToken) =>
        devices.SearchAsync(query, cancellationToken);

    [HttpGet("{id:long}")]
    public Task<DeviceResponse> Get(long id, CancellationToken cancellationToken) =>
        devices.GetAsync(id, cancellationToken);

    [HttpPost]
    public async Task<ActionResult<DeviceKeyResponse>> Create(DeviceCreateRequest request, CancellationToken cancellationToken)
    {
        var created = await devices.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Device.Id }, created);
    }

    [HttpPut("{id:long}")]
    public Task<DeviceResponse> Update(long id, DeviceUpdateRequest request, CancellationToken cancellationToken) =>
        devices.UpdateAsync(id, request, cancellationToken);

    [HttpPatch("{id:long}/status")]
    public Task<DeviceResponse> SetStatus(long id, StatusRequest request, CancellationToken cancellationToken) =>
        devices.SetStatusAsync(id, request, cancellationToken);

    [HttpPost("{id:long}/rotate-key")]
    public Task<DeviceKeyResponse> RotateKey(long id, CancellationToken cancellationToken) =>
        devices.RotateKeyAsync(id, cancellationToken);

    [HttpGet("{id:long}/events")]
    public Task<PagedResult<AccessEventResponse>> Events(long id, [FromQuery] AccessEventQuery query, CancellationToken cancellationToken) =>
        devices.GetEventsAsync(id, query, cancellationToken);
}
