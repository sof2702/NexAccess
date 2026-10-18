using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using NexAccess.Application.DTOs.Devices;
using NexAccess.Application.Exceptions;

namespace NexAccess.API.Configuration;

public static class ClaimsPrincipalExtensions
{
    public static long? FindUserId(this ClaimsPrincipal principal) =>
        principal.Identity?.AuthenticationType == DeviceKeyAuthenticationHandler.SchemeName
            ? null
            : ParseLong(principal.FindFirstValue(JwtRegisteredClaimNames.Sub));

    public static long? FindDeviceId(this ClaimsPrincipal principal) =>
        ParseLong(principal.FindFirstValue(DeviceKeyAuthenticationHandler.DeviceIdClaim));

    public static long GetUserId(this ClaimsPrincipal principal) =>
        principal.FindUserId() ?? throw new UnauthorizedException("UNAUTHORIZED", "Se requiere un usuario autenticado.");

    public static AuthenticatedDevice GetDevice(this ClaimsPrincipal principal)
    {
        var id = principal.FindDeviceId();
        var code = principal.FindFirstValue(DeviceKeyAuthenticationHandler.DeviceCodeClaim);
        return id.HasValue && code is not null
            ? new AuthenticatedDevice(id.Value, code)
            : throw new UnauthorizedException("DEVICE_UNAUTHORIZED", "Dispositivo no autorizado.");
    }

    private static long? ParseLong(string? value) => long.TryParse(value, out var result) ? result : null;
}
