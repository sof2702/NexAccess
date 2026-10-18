using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexAccess.Application.DTOs.Common;
using NexAccess.Application.DTOs.Credentials;
using NexAccess.Application.Services;
using NexAccess.Domain.Enums;

namespace NexAccess.API.Controllers;

[ApiController]
[Route("api/v1/credentials")]
[Authorize(Roles = SystemRoles.AdminOrSeguridad)]
public class CredentialsController(CredentialService credentials) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<CredentialResponse>> Search([FromQuery] CredentialQuery query, CancellationToken cancellationToken) =>
        credentials.SearchAsync(query, cancellationToken);

    [HttpGet("{id:long}")]
    public Task<CredentialResponse> Get(long id, CancellationToken cancellationToken) =>
        credentials.GetAsync(id, cancellationToken);

    [HttpPost]
    public async Task<ActionResult<CredentialResponse>> Register(CredentialRegisterRequest request, CancellationToken cancellationToken)
    {
        var credential = await credentials.RegisterAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = credential.Id }, credential);
    }

    [HttpPatch("{id:long}/status")]
    public Task<CredentialResponse> SetStatus(long id, StatusRequest request, CancellationToken cancellationToken) =>
        credentials.SetStatusAsync(id, request, cancellationToken);

    [HttpPost("{id:long}/revoke")]
    public Task<CredentialResponse> Revoke(long id, RevokeCredentialRequest request, CancellationToken cancellationToken) =>
        credentials.RevokeAsync(id, request, cancellationToken);
}
