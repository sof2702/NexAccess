using NexAccess.Application.DTOs.Access;
using NexAccess.Application.DTOs.Devices;
using NexAccess.Application.Exceptions;
using NexAccess.Domain.Entities;
using NexAccess.Domain.Enums;
using NexAccess.UnitTests.Support;
using NSubstitute;

namespace NexAccess.UnitTests.Services;

public class AccessServiceTests
{
    private readonly ServiceFactory _factory = new();
    private readonly Employee _employee = TestData.Employee();
    private readonly Area _area = TestData.Area();
    private Device _device;

    public AccessServiceTests()
    {
        _device = TestData.Device(DeviceType.Acceso, _area);
        _factory.Devices.GetByIdAsync(_device.Id, Arg.Any<CancellationToken>()).Returns(_ => _device);
    }

    private Task<AccessVerifyResponse> VerifyAsync(string credential = "04A1B2C3D4", string? deviceCode = null) =>
        _factory.AccessService().VerifyAsync(
            new AuthenticatedDevice(_device.Id, _device.Code),
            new AccessVerifyRequest { DeviceCode = deviceCode ?? _device.Code, Credential = credential });

    private Credential GivenCredential(Action<Credential>? configure = null)
    {
        var credential = TestData.Credential(_employee);
        configure?.Invoke(credential);
        _factory.Credentials.GetByCodeAsync(credential.CredentialCode, Arg.Any<CancellationToken>()).Returns(credential);
        return credential;
    }

    private void GivenPermissions(params AccessPermission[] permissions) =>
        _factory.Permissions.GetActiveForEmployeeAndAreaAsync(_employee.Id, _area.Id, Arg.Any<CancellationToken>()).Returns(permissions);

    [Fact]
    public async Task Authorizes_and_opens_when_credential_employee_and_permission_are_valid()
    {
        GivenCredential();
        GivenPermissions(TestData.AllDayPermission());

        var response = await VerifyAsync();

        Assert.True(response.Authorized);
        Assert.Equal("OPEN", response.Action);
        Assert.Equal(3000, response.DurationMs);
        Assert.Equal(AccessReason.Authorized, response.Reason);
        _factory.Events.Received(1).Add(Arg.Is<AccessEvent>(e =>
            e.Result == AccessResult.Autorizado && e.EmployeeId == _employee.Id && e.DeviceId == _device.Id && e.AreaId == _area.Id));
        await _factory.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Normalizes_uid_separators_before_lookup()
    {
        GivenCredential();
        GivenPermissions(TestData.AllDayPermission());

        var response = await VerifyAsync("04:a1:b2:c3:d4");

        Assert.True(response.Authorized);
    }

    [Fact]
    public async Task Rejects_unknown_credential_and_records_event()
    {
        var response = await VerifyAsync("FFFFFFFF");

        Assert.False(response.Authorized);
        Assert.Equal("DENY", response.Action);
        Assert.Null(response.DurationMs);
        Assert.Equal(AccessReason.CredentialNotFound, response.Reason);
        _factory.Events.Received(1).Add(Arg.Is<AccessEvent>(e =>
            e.Result == AccessResult.Rechazado && e.Reason == AccessReason.CredentialNotFound && e.PresentedCode == "FFFFFFFF" && e.EmployeeId == null));
    }

    [Fact]
    public async Task Rejects_inactive_credential()
    {
        GivenCredential(c => c.IsActive = false);

        var response = await VerifyAsync();

        Assert.Equal(AccessReason.CredentialInactive, response.Reason);
        Assert.False(response.Authorized);
    }

    [Fact]
    public async Task Rejects_revoked_credential()
    {
        GivenCredential(c => c.Revoke("Extravío", TestData.MondayNoonUtc.AddDays(-1)));

        var response = await VerifyAsync();

        Assert.Equal(AccessReason.CredentialRevoked, response.Reason);
    }

    [Fact]
    public async Task Rejects_expired_credential()
    {
        GivenCredential(c => c.ExpiresAt = TestData.MondayNoonUtc.AddMinutes(-1));

        var response = await VerifyAsync();

        Assert.Equal(AccessReason.CredentialExpired, response.Reason);
    }

    [Fact]
    public async Task Rejects_inactive_employee()
    {
        _employee.IsActive = false;
        GivenCredential();

        var response = await VerifyAsync();

        Assert.Equal(AccessReason.EmployeeInactive, response.Reason);
        _factory.Events.Received(1).Add(Arg.Is<AccessEvent>(e => e.EmployeeId == _employee.Id && e.Result == AccessResult.Rechazado));
    }

    [Fact]
    public async Task Rejects_when_employee_roles_have_no_permission_for_area()
    {
        GivenCredential();
        GivenPermissions();

        var response = await VerifyAsync();

        Assert.Equal(AccessReason.NoPermission, response.Reason);
    }

    [Fact]
    public async Task Rejects_outside_permission_schedule()
    {
        GivenCredential();
        var permission = TestData.AllDayPermission();
        permission.StartTime = new TimeOnly(6, 0);
        permission.EndTime = new TimeOnly(8, 0);
        GivenPermissions(permission);

        var response = await VerifyAsync();

        Assert.Equal(AccessReason.OutsideSchedule, response.Reason);
    }

    [Fact]
    public async Task Rejects_on_day_not_enabled()
    {
        GivenCredential();
        var permission = TestData.AllDayPermission();
        permission.Monday = false;
        GivenPermissions(permission);

        var response = await VerifyAsync();

        Assert.Equal(AccessReason.OutsideSchedule, response.Reason);
    }

    [Fact]
    public async Task Allows_unrestricted_area_without_explicit_permission()
    {
        _area.IsRestricted = false;
        GivenCredential();

        var response = await VerifyAsync();

        Assert.True(response.Authorized);
        await _factory.Permissions.DidNotReceiveWithAnyArgs().GetActiveForEmployeeAndAreaAsync(default, default, default);
    }

    [Fact]
    public async Task Rejects_inactive_area()
    {
        _area.IsActive = false;
        GivenCredential();

        var response = await VerifyAsync();

        Assert.Equal(AccessReason.AreaInactive, response.Reason);
    }

    [Fact]
    public async Task Rejects_when_body_device_code_differs_from_authenticated_device()
    {
        GivenCredential();

        var response = await VerifyAsync(deviceCode: "OTRA-PUERTA");

        Assert.Equal(AccessReason.DeviceMismatch, response.Reason);
        await _factory.Credentials.DidNotReceiveWithAnyArgs().GetByCodeAsync(default!, default);
    }

    [Fact]
    public async Task Rejects_malformed_credential()
    {
        var response = await VerifyAsync("??");

        Assert.Equal(AccessReason.InvalidRequest, response.Reason);
    }

    [Fact]
    public async Task Attendance_device_marks_entry_without_opening_door()
    {
        _device = TestData.Device(DeviceType.Marcacion);
        GivenCredential();

        var response = await VerifyAsync();

        Assert.True(response.Authorized);
        Assert.Equal("MARK", response.Action);
        Assert.Null(response.DurationMs);
        Assert.Equal(AttendanceType.Entrada, response.Attendance!.Type);
        _factory.Attendance.Received(1).Add(Arg.Is<AttendanceRecord>(r => r.EmployeeId == _employee.Id && r.Type == AttendanceType.Entrada));
    }

    [Fact]
    public async Task Rejected_access_does_not_register_attendance()
    {
        _device = TestData.Device(DeviceType.Mixto, _area);
        GivenCredential(c => c.IsActive = false);

        await VerifyAsync();

        _factory.Attendance.DidNotReceiveWithAnyArgs().Add(default!);
    }

    [Fact]
    public async Task Unknown_or_inactive_authenticated_device_is_unauthorized()
    {
        _device.IsActive = false;

        await Assert.ThrowsAsync<UnauthorizedException>(() => VerifyAsync());
        _factory.Events.DidNotReceiveWithAnyArgs().Add(default!);
    }
}
