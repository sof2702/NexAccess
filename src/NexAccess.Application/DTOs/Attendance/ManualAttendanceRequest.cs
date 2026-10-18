using System.ComponentModel.DataAnnotations;
using NexAccess.Domain.Enums;

namespace NexAccess.Application.DTOs.Attendance;

public class ManualAttendanceRequest
{
    [Range(1, long.MaxValue)]
    public long EmployeeId { get; set; }

    [Required]
    public AttendanceType? Type { get; set; }

    [Required]
    public DateTime? OccurredAt { get; set; }

    [Required, StringLength(255, MinimumLength = 3)]
    public string Note { get; set; } = string.Empty;
}
