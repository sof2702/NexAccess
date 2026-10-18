using NexAccess.Application.DTOs.Common;
using NexAccess.Domain.Enums;

namespace NexAccess.Application.DTOs.Devices;

public class DeviceQuery : PageQuery
{
    public bool? IsActive { get; set; }
    public DeviceType? Type { get; set; }
    public long? AreaId { get; set; }
}
