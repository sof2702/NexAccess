using NexAccess.Domain.Entities;

namespace NexAccess.Application.DTOs.Roles;

public sealed record RoleResponse(long Id, string Code, string Name, string? Description, bool IsActive, DateTime CreatedAt)
{
    public static RoleResponse From(Role role) =>
        new(role.Id, role.Code, role.Name, role.Description, role.IsActive, role.CreatedAt);
}

public sealed record RoleSummary(long Id, string Code, string Name, bool IsActive)
{
    public static RoleSummary From(Role role) => new(role.Id, role.Code, role.Name, role.IsActive);
}
