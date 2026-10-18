using NexAccess.Domain.Entities;

namespace NexAccess.Application.Interfaces.Repositories;

public interface IDeviceCommandRepository
{
    Task<IReadOnlyList<DeviceCommand>> GetOpenForDeviceAsync(long deviceId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DeviceCommand>> GetOpenForEnrollmentAsync(long enrollmentId, CancellationToken cancellationToken = default);
    void Add(DeviceCommand command);
}
