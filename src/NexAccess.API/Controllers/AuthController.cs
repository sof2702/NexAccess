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
public class AuthController(
    AuthService auth,
    MobileAuthService mobileAuth) : ControllerBase
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


[HttpPost("mobile/login")]
[AllowAnonymous]
[EnableRateLimiting(ApiServiceExtensions.LoginRateLimitPolicy)]
public Task<MobileLoginResponse> MobileLogin(
    MobileLoginRequest request,
    CancellationToken cancellationToken)
{
    Response.Headers.CacheControl = "no-store";
    Response.Headers.Pragma = "no-cache";

    return mobileAuth.LoginAsync(
        request,
        cancellationToken);
}



[HttpPost("mobile/refresh")]
[AllowAnonymous]
[EnableRateLimiting(ApiServiceExtensions.LoginRateLimitPolicy)]
public Task<MobileLoginResponse> MobileRefresh(
    MobileRefreshRequest request,
    CancellationToken cancellationToken)
{
    Response.Headers.CacheControl = "no-store";
    Response.Headers.Pragma = "no-cache";

    return mobileAuth.RefreshAsync(
        request,
        cancellationToken);
}



[HttpPost("mobile/logout")]
[AllowAnonymous]
[EnableRateLimiting(ApiServiceExtensions.LoginRateLimitPolicy)]
public async Task<IActionResult> MobileLogout(
    MobileLogoutRequest request,
    CancellationToken cancellationToken)
{
    Response.Headers.CacheControl = "no-store";
    Response.Headers.Pragma = "no-cache";

    await mobileAuth.LogoutAsync(
        request,
        cancellationToken);

    return NoContent();
}


}
