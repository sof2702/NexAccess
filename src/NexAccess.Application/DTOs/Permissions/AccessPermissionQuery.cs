using NexAccess.Application.DTOs.Common;

namespace NexAccess.Application.DTOs.Permissions;

public class AccessPermissionQuery : PageQuery
{
    public long? RoleId { get; set; }
    public long? AreaId { get; set; }
    public bool? IsActive { get; set; }
}
