using Microsoft.EntityFrameworkCore;
using NexAccess.Application.DTOs.Devices;
using NexAccess.Application.Interfaces.Repositories;
using NexAccess.Domain.Entities;

namespace NexAccess.Infrastructure.Persistence.Repositories;

public class DeviceRepository : IDeviceRepository
{
    private readonly NexAccessDbContext _context;

    public DeviceRepository(NexAccessDbContext context)
    {
        _context = context;
    }

    public Task<Device?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        _context.Devices.Include(d => d.Area).FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public Task<Device?> GetByKeyHashAsync(string keyHash, CancellationToken cancellationToken = default) =>
        _context.Devices.FirstOrDefaultAsync(d => d.KeyHash == keyHash, cancellationToken);

    public Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default) =>
        _context.Devices.AnyAsync(d => d.Code == code, cancellationToken);

    public Task<(IReadOnlyList<Device> Items, int Total)> SearchAsync(DeviceQuery query, CancellationToken cancellationToken = default)
    {
        var devices = _context.Devices.AsNoTracking().Include(d => d.Area).AsQueryable();
        if (query.IsActive.HasValue)
        {
            devices = devices.Where(d => d.IsActive == query.IsActive.Value);
        }

        if (query.Type.HasValue)
        {
            devices = devices.Where(d => d.Type == query.Type.Value);
        }

        if (query.AreaId.HasValue)
        {
            devices = devices.Where(d => d.AreaId == query.AreaId.Value);
        }

        return devices.OrderBy(d => d.Code).ToPageAsync(query, cancellationToken);
    }

    public void Add(Device device) => _context.Devices.Add(device);
}
