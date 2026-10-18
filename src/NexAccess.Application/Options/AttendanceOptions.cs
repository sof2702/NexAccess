namespace NexAccess.Application.Options;

public class AttendanceOptions
{
    public const string SectionName = "Attendance";

    public int DuplicateWindowSeconds { get; set; } = 60;
    public int MaxReportDays { get; set; } = 366;
}
