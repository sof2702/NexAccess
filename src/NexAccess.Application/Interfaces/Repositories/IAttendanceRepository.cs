using NexAccess.Application.DTOs.Attendance;
using NexAccess.Domain.Entities;

namespace NexAccess.Application.Interfaces.Repositories;

public interface IAttendanceRepository
{
    Task<AttendanceRecord?> GetLastForEmployeeAsync(long employeeId, CancellationToken cancellationToken = default);
    Task<bool> ExistsNearAsync(long employeeId, DateTime occurredAt, TimeSpan window, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<AttendanceRecord> Items, int Total)> SearchAsync(AttendanceQuery query, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AttendanceRecord>> ListAsync(AttendanceQuery query, int limit, CancellationToken cancellationToken = default);
    void Add(AttendanceRecord record);
}
