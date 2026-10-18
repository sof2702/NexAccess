using NexAccess.Application.DTOs.Devices;
using NexAccess.Domain.Entities;

namespace NexAccess.Application.Interfaces.Repositories;

public interface IDeviceRepository
{
    Task<Device?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<Device?> GetByKeyHashAsync(string keyHash, CancellationToken cancellationToken = default);
    Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Device> Items, int Total)> SearchAsync(DeviceQuery query, CancellationToken cancellationToken = default);
    void Add(Device device);
}
