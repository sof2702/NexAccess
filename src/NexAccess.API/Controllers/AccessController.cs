using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexAccess.API.Configuration;
using NexAccess.Application.DTOs.Access;
using NexAccess.Application.Services;

namespace NexAccess.API.Controllers;

[ApiController]
[Route("api/v1/access")]
[Authorize(AuthenticationSchemes = DeviceKeyAuthenticationHandler.SchemeName)]
public class AccessController(AccessService access) : ControllerBase
{
    [HttpPost("verify")]
    public Task<AccessVerifyResponse> Verify(AccessVerifyRequest request, CancellationToken cancellationToken) =>
        access.VerifyAsync(User.GetDevice(), request, cancellationToken);
}
