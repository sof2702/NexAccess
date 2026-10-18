using Microsoft.EntityFrameworkCore;
using NexAccess.Application.Interfaces.Repositories;
using NexAccess.Domain.Entities;
using NexAccess.Domain.Enums;

namespace NexAccess.Infrastructure.Persistence.Repositories;

public class DeviceCommandRepository : IDeviceCommandRepository
{
    private readonly NexAccessDbContext _context;

    public DeviceCommandRepository(NexAccessDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<DeviceCommand>> GetOpenForDeviceAsync(long deviceId, CancellationToken cancellationToken = default) =>
        await Open().Where(c => c.DeviceId == deviceId).Include(c => c.Enrollment).OrderBy(c => c.CreatedAt).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<DeviceCommand>> GetOpenForEnrollmentAsync(long enrollmentId, CancellationToken cancellationToken = default) =>
        await Open().Where(c => c.EnrollmentId == enrollmentId).ToListAsync(cancellationToken);

    public void Add(DeviceCommand command) => _context.DeviceCommands.Add(command);

    private IQueryable<DeviceCommand> Open() =>
        _context.DeviceCommands.Where(c => c.Status == DeviceCommandStatus.Pendiente || c.Status == DeviceCommandStatus.Entregado);
}
