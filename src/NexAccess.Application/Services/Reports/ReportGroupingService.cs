
using System.Globalization;
using NexAccess.Application.DTOs.Reports;
using NexAccess.Application.Services;

namespace NexAccess.Application.Services.Reports;

public sealed class ReportGroupingService
{
    private readonly LocalTime _localTime;

    public ReportGroupingService(LocalTime localTime)
    {
        _localTime = localTime;
    }

    public List<ReportGroupResult> Group(
        IReadOnlyList<ReportRow> rows,
        IReadOnlyList<ReportGroupBy> groupBy)
    {
        if (groupBy.Count == 0 ||
            groupBy.Contains(ReportGroupBy.None))
        {
            return new List<ReportGroupResult>();
        }

        return rows
            .GroupBy(row => BuildKey(row, groupBy))
            .Select(group =>
            {
                var items = group.ToList();

                return new ReportGroupResult
                {
                    Keys = BuildKeys(items[0], groupBy),
                    TotalRecords = items.Count,

                    Entries = items.Count(x =>
                        x.Type == "ENTRADA"),

                    Exits = items.Count(x =>
                        x.Type == "SALIDA"),

                    Authorized = items.Count(x =>
                        x.Result == "AUTORIZADO"),

                    Rejected = items.Count(x =>
                        x.Result == "RECHAZADO")
                };
            })
            .OrderBy(x => string.Join("|",
                x.Keys.Select(k => k.Value)),
                StringComparer.Ordinal)
            .ToList();
    }

    private string BuildKey(
        ReportRow row,
        IReadOnlyList<ReportGroupBy> groupBy)
    {
        return string.Join("|",
            groupBy.Select(g => GetValue(row, g)));
    }

    private Dictionary<string, string> BuildKeys(
        ReportRow row,
        IReadOnlyList<ReportGroupBy> groupBy)
    {
        return groupBy.ToDictionary(
            g => g.ToString(),
            g => GetValue(row, g));
    }

    private string GetValue(ReportRow row, ReportGroupBy grouping)
    {
        var local = _localTime.ToLocal(row.OccurredAt);

        return grouping switch
        {
            ReportGroupBy.Employee =>
                row.EmployeeId?.ToString(
                    CultureInfo.InvariantCulture) ?? "SIN_EMPLEADO",

            ReportGroupBy.Day =>
                local.ToString("yyyy-MM-dd",
                    CultureInfo.InvariantCulture),

            ReportGroupBy.Week =>
                $"{ISOWeek.GetYear(local)}-W{ISOWeek.GetWeekOfYear(local):D2}",

            ReportGroupBy.Month =>
                local.ToString("yyyy-MM",
                    CultureInfo.InvariantCulture),

            ReportGroupBy.Area =>
                row.AreaName ?? "SIN_AREA",

            ReportGroupBy.Device =>
                row.DeviceCode ?? "SIN_DISPOSITIVO",

            _ => "TODOS"
        };
    }
}

