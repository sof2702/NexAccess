namespace NexAccess.Application.DTOs.Reports;

public sealed class ReportResponse
{
    public ReportType Type { get; set; }

    public DateTime GeneratedAt { get; set; }

    public DateTime From { get; set; }

    public DateTime To { get; set; }

    public int TotalRecords { get; set; }

    public List<ReportRow> Rows { get; set; } = new();

    public List<ReportGroupResult> Groups { get; set; } = new();
}

public sealed class ReportRow
{
    public long Id { get; set; }

    public long? EmployeeId { get; set; }

    public string? EmployeeName { get; set; }

    public DateTime OccurredAt { get; set; }

    public string? Type { get; set; }

    public string? Source { get; set; }

    public string? Result { get; set; }

    public string? Reason { get; set; }

    public string? AreaName { get; set; }

    public string? DeviceCode { get; set; }

    public string? Note { get; set; }
}

public sealed class ReportGroupResult
{
    public Dictionary<string, string> Keys { get; set; } = new();

    public int TotalRecords { get; set; }

    public int Entries { get; set; }

    public int Exits { get; set; }

    public int Authorized { get; set; }

    public int Rejected { get; set; }
}
