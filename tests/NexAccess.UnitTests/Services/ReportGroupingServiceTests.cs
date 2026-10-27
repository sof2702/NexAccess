
using Microsoft.Extensions.Options;
using NexAccess.Application.DTOs.Reports;
using NexAccess.Application.Options;
using NexAccess.Application.Services;
using NexAccess.Application.Services.Reports;
using Xunit;

namespace NexAccess.UnitTests.Services;

public class ReportGroupingServiceTests
{
    private readonly ReportGroupingService _service;

    public ReportGroupingServiceTests()
    {
        var options = Options.Create(new AccessOptions
        {
            TimeZoneId = "America/Asuncion"
        });

        var localTime = new LocalTime(options);

        _service = new ReportGroupingService(localTime);
    }

    [Fact]
    public void Groups_attendance_by_employee()
    {
        var rows = new List<ReportRow>
        {
            Mark(1, 10, "ENTRADA", 8),
            Mark(2, 10, "SALIDA", 17),
            Mark(3, 20, "ENTRADA", 9)
        };

        var result = _service.Group(
            rows,
            new[] { ReportGroupBy.Employee });

        Assert.Equal(2, result.Count);

        var employee10 = Assert.Single(
                         result, x => x.Keys["Employee"] == "10");

        Assert.Equal(2, employee10.TotalRecords);
        Assert.Equal(1, employee10.Entries);
        Assert.Equal(1, employee10.Exits);

        var employee20 = Assert.Single(
    result, x => x.Keys["Employee"] == "20");

        Assert.Equal(1, employee20.TotalRecords);
        Assert.Equal(1, employee20.Entries);
        Assert.Equal(0, employee20.Exits);
    }

    [Fact]
    public void Groups_by_paraguay_local_date()
    {
        var rows = new List<ReportRow>
        {
            new()
            {
                Id = 1,
                EmployeeId = 10,
                Type = "ENTRADA",
                OccurredAt = new DateTime(
                    2026, 10, 20, 1, 30, 0,
                    DateTimeKind.Utc)
            }
        };

        var result = _service.Group(
            rows,
            new[] { ReportGroupBy.Day });

        var group = Assert.Single(result);

        // 01:30 UTC corresponde al día anterior
        // en America/Asuncion.
        Assert.Equal("2026-10-19", group.Keys["Day"]);
    }

    [Fact]
    public void Groups_by_employee_and_month()
    {
        var rows = new List<ReportRow>
        {
            Mark(1, 10, "ENTRADA", 8),
            Mark(2, 10, "SALIDA", 17),
            Mark(3, 20, "ENTRADA", 9)
        };

        var result = _service.Group(
            rows,
            new[]
            {
                ReportGroupBy.Employee,
                ReportGroupBy.Month
            });

        Assert.Equal(2, result.Count);

        Assert.Contains(result, x =>
            x.Keys["Employee"] == "10" &&
            x.Keys["Month"] == "2026-10" &&
            x.TotalRecords == 2);
    }

    [Fact]
    public void Counts_authorized_and_rejected_accesses()
    {
        var rows = new List<ReportRow>
        {
            new()
            {
                Id = 1,
                EmployeeId = 10,
                Result = "AUTORIZADO",
                OccurredAt = Utc(8)
            },
            new()
            {
                Id = 2,
                EmployeeId = 10,
                Result = "RECHAZADO",
                OccurredAt = Utc(9)
            },
            new()
            {
                Id = 3,
                EmployeeId = 10,
                Result = "AUTORIZADO",
                OccurredAt = Utc(10)
            }
        };

        var result = _service.Group(
            rows,
            new[] { ReportGroupBy.Employee });

        var group = Assert.Single(result);

        Assert.Equal(3, group.TotalRecords);
        Assert.Equal(2, group.Authorized);
        Assert.Equal(1, group.Rejected);
    }

    private static ReportRow Mark(
        long id,
        long employeeId,
        string type,
        int hour)
    {
        return new ReportRow
        {
            Id = id,
            EmployeeId = employeeId,
            Type = type,
            OccurredAt = Utc(hour)
        };
    }

    private static DateTime Utc(int hour) =>
        new(2026, 10, 20, hour, 0, 0, DateTimeKind.Utc);
}

