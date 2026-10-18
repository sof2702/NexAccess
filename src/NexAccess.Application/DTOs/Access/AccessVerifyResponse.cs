using System.Text.Json.Serialization;
using NexAccess.Domain.Enums;

namespace NexAccess.Application.DTOs.Access;

public sealed record AccessVerifyResponse(
    bool Authorized,
    string Action,
    string Reason,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? DurationMs = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] AttendanceMark? Attendance = null)
{
    public const string OpenAction = "OPEN";
    public const string MarkAction = "MARK";
    public const string DenyAction = "DENY";

    public static AccessVerifyResponse Deny(string reason) => new(false, DenyAction, reason);
}

public sealed record AttendanceMark(AttendanceType Type, bool Duplicate, DateTime OccurredAt);
