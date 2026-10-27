namespace NexAccess.Application.DTOs.Reports;

public enum ReportType
{
    Attendance,
    AccessEvents,
    Incidents,
    Combined
}

public enum ReportGroupBy
{
    None,
    Employee,
    Day,
    Week,
    Month,
    Area,
    Device
}

public enum ReportFormat
{
    Json,
    Csv,
    Pdf
}
