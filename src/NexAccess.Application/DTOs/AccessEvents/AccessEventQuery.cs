using NexAccess.Application.DTOs.Common;
using NexAccess.Domain.Enums;

namespace NexAccess.Application.DTOs.AccessEvents;

public class AccessEventQuery : PageQuery
{
    public long? EmployeeId { get; set; }
    public long? DeviceId { get; set; }
    public long? AreaId { get; set; }
    public AccessResult? Result { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}
