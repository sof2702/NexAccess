
using NexAccess.Application.DTOs.Reports;
using NexAccess.Domain.Entities;

namespace NexAccess.Application.Services.Reports;

public static class ReportMapper
{
    public static ReportRow FromAttendance(AttendanceRecord record)
    {
        return new ReportRow
        {
            Id = record.Id,
            EmployeeId = record.EmployeeId,
            EmployeeName = record.Employee?.FullName,
            OccurredAt = record.OccurredAt,
            Type = record.Type.ToString().ToUpperInvariant(),
            Source = record.Source.ToString().ToUpperInvariant(),
            DeviceCode = record.Device?.Code,
            Note = record.Note
        };
    }

    public static ReportRow FromAccessEvent(AccessEvent record)
    {
        return new ReportRow
        {
            Id = record.Id,
            EmployeeId = record.EmployeeId,
            EmployeeName = record.Employee?.FullName,
            OccurredAt = record.OccurredAt,
            Result = record.Result.ToString().ToUpperInvariant(),
            Reason = record.Reason,
            AreaName = record.Area?.Name,
            DeviceCode = record.Device?.Code
        };
    }
}

