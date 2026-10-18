using System.ComponentModel.DataAnnotations;
using NexAccess.Domain.Enums;

namespace NexAccess.Application.DTOs.Enrollments;

public class EnrollmentCreateRequest
{
    [Range(1, long.MaxValue)]
    public long EmployeeId { get; set; }

    [Range(1, long.MaxValue)]
    public long DeviceId { get; set; }

    public CredentialType CredentialType { get; set; } = CredentialType.Fisica;

    [StringLength(100)]
    public string? CredentialName { get; set; }

    [Range(1, 1440)]
    public int? ExpiresInMinutes { get; set; }
}
