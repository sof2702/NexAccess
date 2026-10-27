
using NexAccess.Application.DTOs.Reports;
using NexAccess.Application.Exceptions;
using NexAccess.Application.Validators;
using Xunit;

namespace NexAccess.UnitTests.Application;

public class ReportRequestValidatorTests
{
    private static ReportRequest ValidRequest() => new()
    {
        Type = ReportType.Attendance,
        Format = ReportFormat.Json,
        From = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
        To = new DateTime(2026, 10, 26, 23, 0, 0, DateTimeKind.Utc),
        IncludeDetails = true,
        IncludeSummary = true
    };

    [Fact]
    public void Accepts_valid_request()
    {
        var request = ValidRequest();

        ReportRequestValidator.Validate(request);

        Assert.Equal(DateTimeKind.Utc, request.From!.Value.Kind);
        Assert.Equal(DateTimeKind.Utc, request.To!.Value.Kind);
    }

    [Fact]
    public void Requires_date_range()
    {
        var request = ValidRequest();
        request.From = null;

        Assert.Throws<AppValidationException>(
            () => ReportRequestValidator.Validate(request));
    }

    [Fact]
    public void Rejects_inverted_date_range()
    {
        var request = ValidRequest();
        request.From = request.To!.Value.AddDays(1);

        Assert.Throws<AppValidationException>(
            () => ReportRequestValidator.Validate(request));
    }

    [Fact]
    public void Rejects_period_longer_than_366_days()
    {
        var request = ValidRequest();
        request.To = request.From!.Value.AddDays(367);

        Assert.Throws<AppValidationException>(
            () => ReportRequestValidator.Validate(request));
    }

    [Fact]
    public void Accepts_multiple_employee_filters()
    {
        var request = ValidRequest();
        request.EmployeeIds = new List<long> { 10, 15, 20 };

        ReportRequestValidator.Validate(request);

        Assert.Equal(3, request.EmployeeIds.Count);
    }

    [Fact]
    public void Rejects_invalid_employee_id()
    {
        var request = ValidRequest();
        request.EmployeeIds = new List<long> { 10, -1 };

        Assert.Throws<AppValidationException>(
            () => ReportRequestValidator.Validate(request));
    }

    [Fact]
    public void Rejects_duplicate_groupings()
    {
        var request = ValidRequest();
        request.GroupBy = new List<ReportGroupBy>
        {
            ReportGroupBy.Employee,
            ReportGroupBy.Employee
        };

        Assert.Throws<AppValidationException>(
            () => ReportRequestValidator.Validate(request));
    }

    [Fact]
    public void Rejects_area_filter_for_attendance()
    {
        var request = ValidRequest();
        request.AreaIds = new List<long> { 2 };

        Assert.Throws<AppValidationException>(
            () => ReportRequestValidator.Validate(request));
    }

    [Fact]
    public void Rejects_attendance_filters_for_access_events()
    {
        var request = ValidRequest();
        request.Type = ReportType.AccessEvents;
        request.AttendanceTypes.Add(
            NexAccess.Domain.Enums.AttendanceType.Entrada);

        Assert.Throws<AppValidationException>(
            () => ReportRequestValidator.Validate(request));
    }

    [Fact]
    public void Rejects_empty_report_content()
    {
        var request = ValidRequest();
        request.IncludeDetails = false;
        request.IncludeSummary = false;

        Assert.Throws<AppValidationException>(
            () => ReportRequestValidator.Validate(request));
    }

    [Fact]
    public void Rejects_unimplemented_report_types()
    {
        var request = ValidRequest();
        request.Type = ReportType.Combined;

        Assert.Throws<AppValidationException>(
            () => ReportRequestValidator.Validate(request));
    }
}

