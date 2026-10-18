using NexAccess.Domain.Enums;

namespace NexAccess.Domain.Entities;

public class Credential
{
    public long Id { get; set; }
    public long EmployeeId { get; set; }
    public string CredentialCode { get; set; } = string.Empty;
    public CredentialType Type { get; set; } = CredentialType.Fisica;
    public string? Name { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime IssuedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? RevocationReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Employee? Employee { get; set; }

    public bool IsRevoked => RevokedAt.HasValue;

    public bool IsExpiredAt(DateTime utcNow) => ExpiresAt.HasValue && ExpiresAt.Value <= utcNow;

    public void Revoke(string reason, DateTime utcNow)
    {
        IsActive = false;
        RevokedAt = utcNow;
        RevocationReason = reason;
        UpdatedAt = utcNow;
    }
}
