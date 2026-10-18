using NexAccess.Application.DTOs.Devices;
using NexAccess.Application.DTOs.Enrollments;
using NexAccess.Application.Exceptions;
using NexAccess.Domain.Entities;
using NexAccess.Domain.Enums;
using NexAccess.UnitTests.Support;
using NSubstitute;

namespace NexAccess.UnitTests.Services;

public class EnrollmentServiceTests
{
    private readonly ServiceFactory _factory = new();
    private readonly Employee _employee = TestData.Employee();
    private readonly Device _device = TestData.Device(DeviceType.Mixto, TestData.Area());
    private readonly Enrollment _enrollment;
    private readonly DeviceCommand _command;

    public EnrollmentServiceTests()
    {
        _enrollment = new Enrollment
        {
            Id = 15,
            EmployeeId = _employee.Id,
            Employee = _employee,
            DeviceId = _device.Id,
            Device = _device,
            Status = EnrollmentStatus.Pendiente,
            ExpiresAt = TestData.MondayNoonUtc.AddMinutes(5)
        };
        _command = new DeviceCommand { Id = 1, DeviceId = _device.Id, EnrollmentId = 15, Enrollment = _enrollment, Status = DeviceCommandStatus.Entregado, ExpiresAt = _enrollment.ExpiresAt };
        _factory.Enrollments.GetByIdAsync(15, Arg.Any<CancellationToken>()).Returns(_enrollment);
        _factory.Commands.GetOpenForEnrollmentAsync(15, Arg.Any<CancellationToken>()).Returns(new[] { _command });
    }

    private Task<EnrollmentCardResponse> SubmitAsync(string code = "04A1B2C3D4", long? deviceId = null) =>
        _factory.EnrollmentService().SubmitCardAsync(
            new AuthenticatedDevice(deviceId ?? _device.Id, _device.Code),
            15,
            new EnrollmentCardRequest { CredentialCode = code });

    [Fact]
    public async Task Valid_card_creates_credential_and_completes_enrollment_and_command()
    {
        var response = await SubmitAsync("04 a1 b2 c3 d4");

        Assert.Equal(EnrollmentStatus.Completado, response.Status);
        Assert.Equal(EnrollmentStatus.Completado, _enrollment.Status);
        Assert.Equal(DeviceCommandStatus.Completado, _command.Status);
        _factory.Credentials.Received(1).Add(Arg.Is<Credential>(c => c.CredentialCode == "04A1B2C3D4" && c.EmployeeId == _employee.Id && c.IsActive));
    }

    [Fact]
    public async Task Expired_enrollment_is_rejected_and_marked_expired()
    {
        _factory.Clock.UtcNow = _enrollment.ExpiresAt.AddSeconds(1);

        var error = await Assert.ThrowsAsync<ConflictException>(() => SubmitAsync());

        Assert.Equal("ENROLLMENT_EXPIRED", error.Code);
        Assert.Equal(EnrollmentStatus.Vencido, _enrollment.Status);
        Assert.Equal(DeviceCommandStatus.Vencido, _command.Status);
        _factory.Credentials.DidNotReceiveWithAnyArgs().Add(default!);
    }

    [Fact]
    public async Task Card_from_another_device_is_forbidden()
    {
        var error = await Assert.ThrowsAsync<ForbiddenException>(() => SubmitAsync(deviceId: 999));

        Assert.Equal("ENROLLMENT_DEVICE_MISMATCH", error.Code);
    }

    [Fact]
    public async Task Duplicate_card_is_rejected()
    {
        _factory.Credentials.CodeExistsAsync("04A1B2C3D4", Arg.Any<CancellationToken>()).Returns(true);

        var error = await Assert.ThrowsAsync<ConflictException>(() => SubmitAsync());

        Assert.Equal("CREDENTIAL_ALREADY_REGISTERED", error.Code);
        Assert.Equal(EnrollmentStatus.Pendiente, _enrollment.Status);
    }

    [Theory]
    [InlineData(EnrollmentStatus.Completado, "ENROLLMENT_ALREADY_COMPLETED")]
    [InlineData(EnrollmentStatus.Cancelado, "ENROLLMENT_CANCELLED")]
    [InlineData(EnrollmentStatus.Vencido, "ENROLLMENT_EXPIRED")]
    public async Task Closed_enrollment_cannot_receive_card(EnrollmentStatus status, string code)
    {
        _enrollment.Status = status;

        var error = await Assert.ThrowsAsync<ConflictException>(() => SubmitAsync());

        Assert.Equal(code, error.Code);
    }

    [Fact]
    public async Task Invalid_card_code_fails_validation()
    {
        await Assert.ThrowsAsync<AppValidationException>(() => SubmitAsync("$$"));
    }

    [Fact]
    public async Task Cannot_create_enrollment_for_inactive_employee()
    {
        _employee.IsActive = false;
        _factory.Employees.GetByIdAsync(_employee.Id, Arg.Any<CancellationToken>()).Returns(_employee);

        var error = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _factory.EnrollmentService().CreateAsync(new EnrollmentCreateRequest { EmployeeId = _employee.Id, DeviceId = _device.Id }));

        Assert.Equal("EMPLOYEE_INACTIVE", error.Code);
    }

    [Fact]
    public async Task Create_enrollment_queues_enroll_command_with_expiration()
    {
        _factory.Employees.GetByIdAsync(_employee.Id, Arg.Any<CancellationToken>()).Returns(_employee);
        _factory.Devices.GetByIdAsync(_device.Id, Arg.Any<CancellationToken>()).Returns(_device);

        var response = await _factory.EnrollmentService().CreateAsync(new EnrollmentCreateRequest { EmployeeId = _employee.Id, DeviceId = _device.Id, ExpiresInMinutes = 10 });

        Assert.Equal(EnrollmentStatus.Pendiente, response.Status);
        Assert.Equal(TestData.MondayNoonUtc.AddMinutes(10), response.ExpiresAt);
        _factory.Commands.Received(1).Add(Arg.Is<DeviceCommand>(c =>
            c.Type == DeviceCommandType.EnrollCard && c.DeviceId == _device.Id && c.Status == DeviceCommandStatus.Pendiente && c.ExpiresAt == response.ExpiresAt));
    }

    [Fact]
    public async Task Create_enrollment_rejects_expiration_above_maximum()
    {
        await Assert.ThrowsAsync<AppValidationException>(() =>
            _factory.EnrollmentService().CreateAsync(new EnrollmentCreateRequest { EmployeeId = 1, DeviceId = 1, ExpiresInMinutes = 120 }));
    }
}
