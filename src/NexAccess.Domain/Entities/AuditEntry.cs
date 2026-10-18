namespace NexAccess.Domain.Entities;

public class AuditEntry
{
    public long Id { get; set; }
    public long? UserId { get; set; }
    public long? DeviceId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Entity { get; set; } = string.Empty;
    public long? EntityId { get; set; }
    public string? Detail { get; set; }
    public string? IpAddress { get; set; }
    public DateTime OccurredAt { get; set; }
}
