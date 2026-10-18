using NexAccess.Application.DTOs.Attendance;
using NexAccess.Application.Exceptions;
using NexAccess.Domain.Entities;
using NexAccess.Domain.Enums;
using NexAccess.UnitTests.Support;
using NSubstitute;

namespace NexAccess.UnitTests.Services;

public class AttendanceServiceTests
{
    private readonly ServiceFactory _factory = new();
    private readonly DateTime _now = TestData.MondayNoonUtc;

    private void GivenLast(AttendanceType type, DateTime occurredAt) =>
        _factory.Attendance.GetLastForEmployeeAsync(10, Arg.Any<CancellationToken>())
            .Returns(new AttendanceRecord { EmployeeId = 10, Type = type, OccurredAt = occurredAt });

    [Fact]
    public async Task First_mark_is_entry()
    {
        var mark = await _factory.AttendanceService().RegisterFromDeviceAsync(10, 20, 3, _now);

        Assert.Equal(AttendanceType.Entrada, mark.Type);
        Assert.False(mark.Duplicate);
        _factory.Attendance.Received(1).Add(Arg.Is<AttendanceRecord>(r => r.Type == AttendanceType.Entrada && r.Source == AttendanceSource.Dispositivo && r.DeviceId == 3));
    }

    [Fact]
    public async Task Mark_after_entry_on_same_local_day_is_exit()
    {
        GivenLast(AttendanceType.Entrada, _now.AddHours(-4));

        var mark = await _factory.AttendanceService().RegisterFromDeviceAsync(10, 20, 3, _now);

        Assert.Equal(AttendanceType.Salida, mark.Type);
    }

    [Fact]
    public async Task Mark_after_exit_is_entry()
    {
        GivenLast(AttendanceType.Salida, _now.AddHours(-1));

        var mark = await _factory.AttendanceService().RegisterFromDeviceAsync(10, 20, 3, _now);

        Assert.Equal(AttendanceType.Entrada, mark.Type);
    }

    [Fact]
    public async Task Entry_from_previous_local_day_starts_new_entry()
    {
        GivenLast(AttendanceType.Entrada, _now.AddDays(-1));

        var mark = await _factory.AttendanceService().RegisterFromDeviceAsync(10, 20, 3, _now);

        Assert.Equal(AttendanceType.Entrada, mark.Type);
    }

    [Fact]
    public async Task Repeated_presentation_within_window_is_not_duplicated()
    {
        GivenLast(AttendanceType.Entrada, _now.AddSeconds(-20));

        var mark = await _factory.AttendanceService().RegisterFromDeviceAsync(10, 20, 3, _now);

        Assert.True(mark.Duplicate);
        Assert.Equal(AttendanceType.Entrada, mark.Type);
        _factory.Attendance.DidNotReceiveWithAnyArgs().Add(default!);
    }

    [Fact]
    public async Task Manual_mark_conflicts_with_existing_mark_in_window()
    {
        _factory.Employees.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(TestData.Employee());
        _factory.Attendance.ExistsNearAsync(10, Arg.Any<DateTime>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()).Returns(true);

        var request = new ManualAttendanceRequest { EmployeeId = 10, Type = AttendanceType.Entrada, OccurredAt = _now.AddHours(-2), Note = "Olvidó la tarjeta" };

        var error = await Assert.ThrowsAsync<ConflictException>(() => _factory.AttendanceService().RegisterManualAsync(request));
        Assert.Equal("ATTENDANCE_DUPLICATE", error.Code);
    }

    [Fact]
    public async Task Manual_mark_is_registered_with_author_and_audited()
    {
        _factory.Employees.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(TestData.Employee());
        var request = new ManualAttendanceRequest { EmployeeId = 10, Type = AttendanceType.Salida, OccurredAt = _now.AddHours(-1), Note = "Corrección RRHH" };

        var response = await _factory.AttendanceService().RegisterManualAsync(request);

        Assert.Equal(AttendanceSource.Manual, response.Source);
        _factory.Attendance.Received(1).Add(Arg.Is<AttendanceRecord>(r => r.RegisteredByUserId == 1 && r.Note == "Corrección RRHH"));
        _factory.AuditRepository.Received(1).Add(Arg.Is<AuditEntry>(a => a.Action == "MANUAL_MARK"));
    }

    [Fact]
    public async Task Manual_mark_in_future_is_rejected()
    {
        _factory.Employees.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(TestData.Employee());
        var request = new ManualAttendanceRequest { EmployeeId = 10, Type = AttendanceType.Entrada, OccurredAt = _now.AddHours(3), Note = "Futuro" };

        await Assert.ThrowsAsync<AppValidationException>(() => _factory.AttendanceService().RegisterManualAsync(request));
    }

    [Fact]
    public async Task Manual_mark_without_note_fails_validation()
    {
        var request = new ManualAttendanceRequest { EmployeeId = 10, Type = AttendanceType.Entrada, OccurredAt = _now };

        var error = await Assert.ThrowsAsync<AppValidationException>(() => _factory.AttendanceService().RegisterManualAsync(request));
        Assert.Contains(nameof(ManualAttendanceRequest.Note), error.Errors.Keys);
    }

    [Fact]
    public async Task Report_requires_date_range()
    {
        await Assert.ThrowsAsync<AppValidationException>(() => _factory.AttendanceService().GetDailyReportAsync(new AttendanceQuery()));
    }

    [Fact]
    public async Task Report_rejects_range_longer_than_configured()
    {
        var query = new AttendanceQuery { From = _now.AddDays(-40), To = _now };

        await Assert.ThrowsAsync<AppValidationException>(() => _factory.AttendanceService().GetDailyReportAsync(query));
    }

    [Fact]
    public async Task Daily_report_groups_marks_by_local_day_and_flags_incomplete()
    {
        var employee = TestData.Employee();
        _factory.Attendance.ListAsync(Arg.Any<AttendanceQuery>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(new List<AttendanceRecord>
        {
            new() { EmployeeId = 10, Employee = employee, Type = AttendanceType.Entrada, OccurredAt = new DateTime(2026, 10, 19, 11, 0, 0, DateTimeKind.Utc) },
            new() { EmployeeId = 10, Employee = employee, Type = AttendanceType.Salida, OccurredAt = new DateTime(2026, 10, 19, 20, 0, 0, DateTimeKind.Utc) },
            new() { EmployeeId = 10, Employee = employee, Type = AttendanceType.Entrada, OccurredAt = new DateTime(2026, 10, 20, 11, 0, 0, DateTimeKind.Utc) }
        });

        var report = await _factory.AttendanceService().GetDailyReportAsync(new AttendanceQuery { From = _now.AddDays(-1), To = _now.AddDays(2) });

        Assert.Equal(2, report.Count);
        Assert.False(report[0].Incomplete);
        Assert.Equal(new DateOnly(2026, 10, 19), report[0].Date);
        Assert.True(report[1].Incomplete);
        Assert.Null(report[1].LastExit);
    }
}
