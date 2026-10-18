using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using NexAccess.API.Middleware;
using NexAccess.Application.Services;

namespace NexAccess.API.Configuration;

public class DeviceKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "DeviceKey";
    public const string HeaderName = "X-DEVICE-KEY";
    public const string DeviceIdClaim = "device_id";
    public const string DeviceCodeClaim = "device_code";

    private readonly DeviceService _devices;

    public DeviceKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        DeviceService devices)
        : base(options, logger, encoder)
    {
        _devices = devices;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HeaderName, out var values) || string.IsNullOrWhiteSpace(values.ToString()))
        {
            return AuthenticateResult.NoResult();
        }

        var device = await _devices.AuthenticateAsync(values.ToString(), Context.RequestAborted);
        if (device is null)
        {
            Logger.LogWarning("Clave de dispositivo rechazada desde {RemoteIp}.", Context.Connection.RemoteIpAddress);
            return AuthenticateResult.Fail("Clave de dispositivo inválida o dispositivo inactivo.");
        }

        var identity = new ClaimsIdentity(
            new[]
            {
                new Claim(DeviceIdClaim, device.Id.ToString()),
                new Claim(DeviceCodeClaim, device.Code),
                new Claim(ClaimTypes.Name, device.Code)
            },
            SchemeName);
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties) =>
        ProblemResponses.WriteAsync(Context, StatusCodes.Status401Unauthorized, "DEVICE_UNAUTHORIZED", "Dispositivo no autorizado.");

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties) =>
        ProblemResponses.WriteAsync(Context, StatusCodes.Status403Forbidden, "FORBIDDEN", "El dispositivo no tiene permiso para esta operación.");
}
