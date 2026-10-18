using NexAccess.Domain.Entities;

namespace NexAccess.Application.DTOs.Permissions;

public sealed record AccessPermissionResponse(
    long Id,
    long RoleId,
    string? RoleCode,
    long AreaId,
    string? AreaCode,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    bool Monday,
    bool Tuesday,
    bool Wednesday,
    bool Thursday,
    bool Friday,
    bool Saturday,
    bool Sunday,
    bool IsActive,
    DateTime CreatedAt)
{
    public static AccessPermissionResponse From(AccessPermission permission) => new(
        permission.Id,
        permission.RoleId,
        permission.Role?.Code,
        permission.AreaId,
        permission.Area?.Code,
        permission.StartTime,
        permission.EndTime,
        permission.Monday,
        permission.Tuesday,
        permission.Wednesday,
        permission.Thursday,
        permission.Friday,
        permission.Saturday,
        permission.Sunday,
        permission.IsActive,
        permission.CreatedAt);
}
