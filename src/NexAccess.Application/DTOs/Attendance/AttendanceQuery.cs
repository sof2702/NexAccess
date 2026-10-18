using NexAccess.Application.DTOs.Common;
using NexAccess.Domain.Enums;

namespace NexAccess.Application.DTOs.Attendance;

public class AttendanceQuery : PageQuery
{
    public long? EmployeeId { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public AttendanceType? Type { get; set; }
    public AttendanceSource? Source { get; set; }
}
