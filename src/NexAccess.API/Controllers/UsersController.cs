using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexAccess.Application.DTOs.Common;
using NexAccess.Application.DTOs.Users;
using NexAccess.Application.Services;
using NexAccess.Domain.Enums;

namespace NexAccess.API.Controllers;

[ApiController]
[Route("api/v1/users")]
[Authorize(Roles = SystemRoles.Admin)]
public class UsersController(UserService users) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<UserResponse>> List(CancellationToken cancellationToken) =>
        users.ListAsync(cancellationToken);

    [HttpPost]
    public async Task<ActionResult<UserResponse>> Create(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var user = await users.CreateAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, user);
    }

    [HttpPatch("{id:long}/status")]
    public Task<UserResponse> SetStatus(long id, StatusRequest request, CancellationToken cancellationToken) =>
        users.SetStatusAsync(id, request, cancellationToken);

    [HttpPut("{id:long}/password")]
    public async Task<IActionResult> ChangePassword(long id, ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        await users.ChangePasswordAsync(id, request, cancellationToken);
        return NoContent();
    }
}
