using NexAccess.Domain.Enums;

namespace NexAccess.Domain.Entities;

public class Enrollment
{
    public long Id { get; set; }
    public long EmployeeId { get; set; }
    public long DeviceId { get; set; }
    public CredentialType CredentialType { get; set; } = CredentialType.Fisica;
    public string? CredentialName { get; set; }
    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Pendiente;
    public DateTime ExpiresAt { get; set; }
    public long? CredentialId { get; set; }
    public long? RequestedByUserId { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Employee? Employee { get; set; }
    public Device? Device { get; set; }
    public Credential? Credential { get; set; }

    public bool IsExpiredAt(DateTime utcNow) => ExpiresAt <= utcNow;
}
