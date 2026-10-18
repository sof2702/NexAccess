using NexAccess.Application.DTOs.Devices;
using NexAccess.Application.Exceptions;
using NexAccess.Application.Interfaces;
using NexAccess.Application.Interfaces.Repositories;
using NexAccess.Domain.Entities;
using NexAccess.Domain.Enums;

namespace NexAccess.Application.Services;

public class DeviceCommandService
{
    private readonly IDeviceCommandRepository _commands;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public DeviceCommandService(IDeviceCommandRepository commands, IUnitOfWork unitOfWork, IClock clock)
    {
        _commands = commands;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public static string NameOf(DeviceCommandType type) => type switch
    {
        DeviceCommandType.EnrollCard => "ENROLL_CARD",
        _ => DeviceCommandResponse.None
    };

    public async Task<DeviceCommandResponse> GetNextAsync(AuthenticatedDevice device, string deviceCode, CancellationToken cancellationToken = default)
    {
        if (!string.Equals(device.Code, deviceCode?.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new ForbiddenException("DEVICE_MISMATCH", "La clave no corresponde al dispositivo indicado.");
        }

        var now = _clock.UtcNow;
        var open = await _commands.GetOpenForDeviceAsync(device.Id, cancellationToken);
        var changed = ExpireOverdue(open, now);

        var next = open.Where(command => command.IsOpen).OrderBy(command => command.CreatedAt).ThenBy(command => command.Id).FirstOrDefault();
        if (next is null)
        {
            if (changed)
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return DeviceCommandResponse.Empty;
        }

        next.Status = DeviceCommandStatus.Entregado;
        next.DeliveredAt ??= now;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new DeviceCommandResponse(NameOf(next.Type), next.EnrollmentId, next.ExpiresAt);
    }

    public static bool ExpireOverdue(IEnumerable<DeviceCommand> commands, DateTime utcNow)
    {
        var changed = false;
        foreach (var command in commands.Where(item => item.IsOpen && item.ExpiresAt <= utcNow))
        {
            command.Status = DeviceCommandStatus.Vencido;
            if (command.Enrollment is { Status: EnrollmentStatus.Pendiente } enrollment)
            {
                enrollment.Status = EnrollmentStatus.Vencido;
                enrollment.UpdatedAt = utcNow;
            }

            changed = true;
        }

        return changed;
    }
}
