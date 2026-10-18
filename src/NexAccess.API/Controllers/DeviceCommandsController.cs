using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexAccess.API.Configuration;
using NexAccess.Application.DTOs.Devices;
using NexAccess.Application.Services;

namespace NexAccess.API.Controllers;

[ApiController]
[Route("api/v1/devices/{codigo}/commands")]
[Authorize(AuthenticationSchemes = DeviceKeyAuthenticationHandler.SchemeName)]
public class DeviceCommandsController(DeviceCommandService commands) : ControllerBase
{
    [HttpGet]
    public Task<DeviceCommandResponse> Next(string codigo, CancellationToken cancellationToken) =>
        commands.GetNextAsync(User.GetDevice(), codigo, cancellationToken);
}
