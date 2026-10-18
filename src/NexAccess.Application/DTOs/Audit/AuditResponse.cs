using NexAccess.Domain.Entities;

namespace NexAccess.Application.DTOs.Audit;

public sealed record AuditResponse(
    long Id,
    DateTime OccurredAt,
    long? UserId,
    long? DeviceId,
    string Action,
    string Entity,
    long? EntityId,
    string? Detail,
    string? IpAddress)
{
    public static AuditResponse From(AuditEntry entry) => new(
        entry.Id, entry.OccurredAt, entry.UserId, entry.DeviceId, entry.Action, entry.Entity, entry.EntityId, entry.Detail, entry.IpAddress);
}
