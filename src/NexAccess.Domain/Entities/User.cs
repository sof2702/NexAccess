using NexAccess.Domain.Enums;

namespace NexAccess.Domain.Entities;

public class User
{
    public long Id { get; set; }
    public long? EmployeeId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public SystemRole SystemRole { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
