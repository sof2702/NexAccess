using NexAccess.Domain.Entities;

namespace NexAccess.Application.DTOs.Areas;

public sealed record AreaResponse(
    long Id,
    string Code,
    string Name,
    string? Description,
    bool IsRestricted,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public static AreaResponse From(Area area) => new(
        area.Id, area.Code, area.Name, area.Description, area.IsRestricted, area.IsActive, area.CreatedAt, area.UpdatedAt);
}
