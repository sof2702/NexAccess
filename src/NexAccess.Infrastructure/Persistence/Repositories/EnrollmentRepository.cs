using Microsoft.EntityFrameworkCore;
using NexAccess.Application.Interfaces.Repositories;
using NexAccess.Domain.Entities;

namespace NexAccess.Infrastructure.Persistence.Repositories;

public class EnrollmentRepository : IEnrollmentRepository
{
    private readonly NexAccessDbContext _context;

    public EnrollmentRepository(NexAccessDbContext context)
    {
        _context = context;
    }

    public Task<Enrollment?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        _context.Enrollments.Include(e => e.Employee).Include(e => e.Device).FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public void Add(Enrollment enrollment) => _context.Enrollments.Add(enrollment);
}
