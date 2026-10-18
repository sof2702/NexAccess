using NexAccess.Application.Validators;
using NexAccess.Domain.Entities;
using NexAccess.Domain.Enums;

namespace NexAccess.Application.DTOs.AccessEvents;

public sealed record AccessEventResponse(
    long Id,
    DateTime OccurredAt,
    AccessResult Result,
    string? Reason,
    long? EmployeeId,
    string? EmployeeName,
    long? CredentialId,
    string? MaskedCredential,
    long DeviceId,
    string? DeviceCode,
    long? AreaId,
    string? AreaName)
{
    public static AccessEventResponse From(AccessEvent accessEvent) => new(
        accessEvent.Id,
        accessEvent.OccurredAt,
        accessEvent.Result,
        accessEvent.Reason,
        accessEvent.EmployeeId,
        accessEvent.Employee?.FullName,
        accessEvent.CredentialId,
        CredentialCode.Mask(accessEvent.PresentedCode),
        accessEvent.DeviceId,
        accessEvent.Device?.Code,
        accessEvent.AreaId,
        accessEvent.Area?.Name);
}
