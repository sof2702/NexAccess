using NexAccess.Domain.Enums;

namespace NexAccess.Domain.Entities;

public class DeviceCommand
{
    public long Id { get; set; }
    public long DeviceId { get; set; }
    public DeviceCommandType Type { get; set; }
    public long? EnrollmentId { get; set; }
    public DeviceCommandStatus Status { get; set; } = DeviceCommandStatus.Pendiente;
    public DateTime ExpiresAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public Enrollment? Enrollment { get; set; }

    public bool IsOpen => Status is DeviceCommandStatus.Pendiente or DeviceCommandStatus.Entregado;
}
