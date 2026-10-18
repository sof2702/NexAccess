using NexAccess.Domain.Enums;

namespace NexAccess.Domain.Entities;

public class Device
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DeviceType Type { get; set; } = DeviceType.Acceso;
    public long? AreaId { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public string KeyHash { get; set; } = string.Empty;
    public DateTime KeyRotatedAt { get; set; }
    public DateTime? LastConnectionAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Area? Area { get; set; }

    public bool ControlsAccess => Type is DeviceType.Acceso or DeviceType.Mixto;
    public bool RegistersAttendance => Type is DeviceType.Marcacion or DeviceType.Mixto;
}
