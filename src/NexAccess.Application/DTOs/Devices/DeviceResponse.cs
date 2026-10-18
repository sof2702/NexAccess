using NexAccess.Domain.Entities;
using NexAccess.Domain.Enums;

namespace NexAccess.Application.DTOs.Devices;

public sealed record DeviceResponse(
    long Id,
    string Code,
    string Name,
    DeviceType Type,
    long? AreaId,
    string? AreaName,
    string? Description,
    bool IsActive,
    DateTime? LastConnectionAt,
    DateTime KeyRotatedAt,
    DateTime CreatedAt)
{
    public static DeviceResponse From(Device device) => new(
        device.Id,
        device.Code,
        device.Name,
        device.Type,
        device.AreaId,
        device.Area?.Name,
        device.Description,
        device.IsActive,
        device.LastConnectionAt,
        device.KeyRotatedAt,
        device.CreatedAt);
}

public sealed record DeviceKeyResponse(DeviceResponse Device, string DeviceKey);

public sealed record AuthenticatedDevice(long Id, string Code);
