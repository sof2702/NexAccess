using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NexAccess.API.Configuration;
using NexAccess.Application.DTOs.Auth;
using NexAccess.Application.DTOs.Users;
using NexAccess.Application.Services;

namespace NexAccess.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(AuthService auth) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(ApiServiceExtensions.LoginRateLimitPolicy)]
    public Task<LoginResponse> Login(LoginRequest request, CancellationToken cancellationToken) =>
        auth.LoginAsync(request, cancellationToken);

    [HttpGet("me")]
    [Authorize]
    public Task<UserResponse> Me(CancellationToken cancellationToken) =>
        auth.GetCurrentAsync(User.GetUserId(), cancellationToken);
}
