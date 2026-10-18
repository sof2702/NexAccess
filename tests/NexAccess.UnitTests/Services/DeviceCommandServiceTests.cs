using NexAccess.Application.DTOs.Devices;
using NexAccess.Application.Exceptions;
using NexAccess.Domain.Entities;
using NexAccess.Domain.Enums;
using NexAccess.UnitTests.Support;
using NSubstitute;

namespace NexAccess.UnitTests.Services;

public class DeviceCommandServiceTests
{
    private readonly ServiceFactory _factory = new();
    private readonly AuthenticatedDevice _device = new(3, "PUERTA-RRHH-01");

    [Fact]
    public async Task Returns_none_when_no_pending_commands()
    {
        _factory.Commands.GetOpenForDeviceAsync(3, Arg.Any<CancellationToken>()).Returns(Array.Empty<DeviceCommand>());

        var response = await _factory.DeviceCommandService().GetNextAsync(_device, "PUERTA-RRHH-01");

        Assert.Equal("NONE", response.Command);
        Assert.Null(response.EnrollmentId);
    }

    [Fact]
    public async Task Returns_enroll_card_and_marks_command_delivered()
    {
        var command = new DeviceCommand { Id = 1, DeviceId = 3, EnrollmentId = 15, Type = DeviceCommandType.EnrollCard, Status = DeviceCommandStatus.Pendiente, ExpiresAt = TestData.MondayNoonUtc.AddMinutes(5) };
        _factory.Commands.GetOpenForDeviceAsync(3, Arg.Any<CancellationToken>()).Returns(new[] { command });

        var response = await _factory.DeviceCommandService().GetNextAsync(_device, "puerta-rrhh-01");

        Assert.Equal("ENROLL_CARD", response.Command);
        Assert.Equal(15, response.EnrollmentId);
        Assert.Equal(DeviceCommandStatus.Entregado, command.Status);
        Assert.Equal(TestData.MondayNoonUtc, command.DeliveredAt);
    }

    [Fact]
    public async Task Expired_command_is_closed_and_not_delivered()
    {
        var enrollment = new Enrollment { Id = 15, Status = EnrollmentStatus.Pendiente };
        var command = new DeviceCommand { Id = 1, DeviceId = 3, EnrollmentId = 15, Enrollment = enrollment, Status = DeviceCommandStatus.Pendiente, ExpiresAt = TestData.MondayNoonUtc.AddSeconds(-1) };
        _factory.Commands.GetOpenForDeviceAsync(3, Arg.Any<CancellationToken>()).Returns(new[] { command });

        var response = await _factory.DeviceCommandService().GetNextAsync(_device, "PUERTA-RRHH-01");

        Assert.Equal("NONE", response.Command);
        Assert.Equal(DeviceCommandStatus.Vencido, command.Status);
        Assert.Equal(EnrollmentStatus.Vencido, enrollment.Status);
        await _factory.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Route_code_of_another_device_is_forbidden()
    {
        await Assert.ThrowsAsync<ForbiddenException>(() => _factory.DeviceCommandService().GetNextAsync(_device, "OTRA-PUERTA"));
    }
}
