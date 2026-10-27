using NexAccess.Application.DTOs.Reports;
using NexAccess.Domain.Entities;

namespace NexAccess.Application.Interfaces.Repositories;

public interface IReportRepository
{
    Task<IReadOnlyList<AttendanceRecord>> GetAttendanceAsync(
        ReportRequest request,
        int maxRows,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AccessEvent>> GetAccessEventsAsync(
        ReportRequest request,
        int maxRows,
        CancellationToken cancellationToken = default);
}

