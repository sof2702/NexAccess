using NexAccess.Domain.Enums;

namespace NexAccess.Domain.Entities;

public class AccessEvent
{
    public long Id { get; set; }
    public long? EmployeeId { get; set; }
    public long? CredentialId { get; set; }
    public long DeviceId { get; set; }
    public long? AreaId { get; set; }
    public string? PresentedCode { get; set; }
    public DateTime OccurredAt { get; set; }
    public AccessResult Result { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; }

    public Employee? Employee { get; set; }
    public Device? Device { get; set; }
    public Area? Area { get; set; }
}
