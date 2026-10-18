using NexAccess.Domain.Enums;

namespace NexAccess.Domain.Entities;

public class AttendanceRecord
{
    public long Id { get; set; }
    public long EmployeeId { get; set; }
    public long? CredentialId { get; set; }
    public long? DeviceId { get; set; }
    public AttendanceType Type { get; set; }
    public DateTime OccurredAt { get; set; }
    public AttendanceSource Source { get; set; } = AttendanceSource.Dispositivo;
    public string? Note { get; set; }
    public long? RegisteredByUserId { get; set; }
    public DateTime CreatedAt { get; set; }

    public Employee? Employee { get; set; }
    public Device? Device { get; set; }
}
