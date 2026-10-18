using System.ComponentModel.DataAnnotations;

namespace NexAccess.Application.DTOs.Permissions;

public class AccessPermissionRequest
{
    [Range(1, long.MaxValue)]
    public long RoleId { get; set; }

    [Range(1, long.MaxValue)]
    public long AreaId { get; set; }

    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public bool Monday { get; set; } = true;
    public bool Tuesday { get; set; } = true;
    public bool Wednesday { get; set; } = true;
    public bool Thursday { get; set; } = true;
    public bool Friday { get; set; } = true;
    public bool Saturday { get; set; }
    public bool Sunday { get; set; }
    public bool IsActive { get; set; } = true;
}
