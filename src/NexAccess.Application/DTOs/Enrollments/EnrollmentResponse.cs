using NexAccess.Domain.Entities;
using NexAccess.Domain.Enums;

namespace NexAccess.Application.DTOs.Enrollments;

public sealed record EnrollmentResponse(
    long Id,
    long EmployeeId,
    string? EmployeeName,
    long DeviceId,
    string? DeviceCode,
    CredentialType CredentialType,
    string? CredentialName,
    EnrollmentStatus Status,
    DateTime ExpiresAt,
    long? CredentialId,
    DateTime? CompletedAt,
    DateTime CreatedAt)
{
    public static EnrollmentResponse From(Enrollment enrollment) => new(
        enrollment.Id,
        enrollment.EmployeeId,
        enrollment.Employee?.FullName,
        enrollment.DeviceId,
        enrollment.Device?.Code,
        enrollment.CredentialType,
        enrollment.CredentialName,
        enrollment.Status,
        enrollment.ExpiresAt,
        enrollment.CredentialId,
        enrollment.CompletedAt,
        enrollment.CreatedAt);
}

public sealed record EnrollmentCardResponse(long EnrollmentId, EnrollmentStatus Status, long CredentialId);
