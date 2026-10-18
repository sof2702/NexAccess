using Microsoft.EntityFrameworkCore;
using NexAccess.Application.DTOs.Credentials;
using NexAccess.Application.Interfaces.Repositories;
using NexAccess.Domain.Entities;

namespace NexAccess.Infrastructure.Persistence.Repositories;

public class CredentialRepository : ICredentialRepository
{
    private readonly NexAccessDbContext _context;

    public CredentialRepository(NexAccessDbContext context)
    {
        _context = context;
    }

    public Task<Credential?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        _context.Credentials.Include(c => c.Employee).FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<Credential?> GetByCodeAsync(string credentialCode, CancellationToken cancellationToken = default) =>
        _context.Credentials.Include(c => c.Employee).FirstOrDefaultAsync(c => c.CredentialCode == credentialCode, cancellationToken);

    public Task<bool> CodeExistsAsync(string credentialCode, CancellationToken cancellationToken = default) =>
        _context.Credentials.AnyAsync(c => c.CredentialCode == credentialCode, cancellationToken);

    public Task<(IReadOnlyList<Credential> Items, int Total)> SearchAsync(CredentialQuery query, CancellationToken cancellationToken = default)
    {
        var credentials = _context.Credentials.AsNoTracking().Include(c => c.Employee).AsQueryable();
        if (query.EmployeeId.HasValue)
        {
            credentials = credentials.Where(c => c.EmployeeId == query.EmployeeId.Value);
        }

        if (query.IsActive.HasValue)
        {
            credentials = credentials.Where(c => c.IsActive == query.IsActive.Value);
        }

        return credentials.OrderByDescending(c => c.CreatedAt).ThenByDescending(c => c.Id).ToPageAsync(query, cancellationToken);
    }

    public void Add(Credential credential) => _context.Credentials.Add(credential);
}
