using NexAccess.Domain.Entities;

namespace NexAccess.Application.Interfaces.Repositories;

public interface IEnrollmentRepository
{
    Task<Enrollment?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    void Add(Enrollment enrollment);
}
