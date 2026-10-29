using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexAccess.Application.DTOs.Me;
using NexAccess.Application.Services;
using NexAccess.Domain.Enums;
using NexAccess.Application.DTOs.Common;
using NexAccess.Application.DTOs.AccessEvents;


namespace NexAccess.API.Controllers;

[ApiController]
[Route("api/v1/me")]
[Authorize(Roles = SystemRoles.Empleado)]
[ResponseCache(
    NoStore = true,
    Location = ResponseCacheLocation.None)]
public class MeController(MyProfileService profiles) : ControllerBase
{
    [HttpGet]
    public Task<MyProfileResponse> Get(
        CancellationToken cancellationToken)
    {
        return profiles.GetAsync(cancellationToken);
    }


[HttpGet("credentials")]
public Task<PagedResult<MyCredentialResponse>> GetCredentials(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 50,
    CancellationToken cancellationToken = default)
{
    Response.Headers.CacheControl = "no-store";

    return profiles.GetCredentialsAsync(
        page,
        pageSize,
        cancellationToken);
}


[HttpGet("access-events")]
public Task<PagedResult<AccessEventResponse>> GetAccessEvents(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 50,
    [FromQuery] DateTime? from = null,
    [FromQuery] DateTime? to = null,
    [FromQuery] AccessResult? result = null,
    CancellationToken cancellationToken = default)
{
    Response.Headers.CacheControl = "no-store";

    return profiles.GetAccessEventsAsync(
        page,
        pageSize,
        from,
        to,
        result,
        cancellationToken);
}



[HttpGet("credentials/{id:long}/nfc")]
public Task<MyNfcCredentialResponse> GetNfcCredential(
    long id,
    CancellationToken cancellationToken)
{
    Response.Headers.CacheControl = "no-store";
    Response.Headers.Pragma = "no-cache";

    return profiles.GetNfcCredentialAsync(
        id,
        cancellationToken);
}


}

