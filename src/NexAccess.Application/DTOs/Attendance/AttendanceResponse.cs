using NexAccess.Domain.Entities;
using NexAccess.Domain.Enums;

namespace NexAccess.Application.DTOs.Attendance;

public sealed record AttendanceResponse(
    long Id,
    long EmployeeId,
    string? EmployeeName,
    AttendanceType Type,
    DateTime OccurredAt,
    AttendanceSource Source,
    long? DeviceId,
    string? DeviceCode,
    string? Note)
{
    public static AttendanceResponse From(AttendanceRecord record) => new(
        record.Id,
        record.EmployeeId,
        record.Employee?.FullName,
        record.Type,
        record.OccurredAt,
        record.Source,
        record.DeviceId,
        record.Device?.Code,
        record.Note);
}

public sealed record AttendanceDailySummary(
    long EmployeeId,
    string? EmployeeName,
    DateOnly Date,
    DateTime? FirstEntry,
    DateTime? LastExit,
    int TotalMarks,
    bool Incomplete);
