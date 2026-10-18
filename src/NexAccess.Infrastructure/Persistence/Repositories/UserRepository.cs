using Microsoft.EntityFrameworkCore;
using NexAccess.Application.Interfaces.Repositories;
using NexAccess.Domain.Entities;

namespace NexAccess.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly NexAccessDbContext _context;

    public UserRepository(NexAccessDbContext context)
    {
        _context = context;
    }

    public Task<User?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        _context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default) =>
        _context.Users.FirstOrDefaultAsync(u => u.Username == username, cancellationToken);

    public Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken = default) =>
        _context.Users.AnyAsync(u => u.Username.ToLower() == username.ToLower(), cancellationToken);

    public Task<bool> AnyAsync(CancellationToken cancellationToken = default) =>
        _context.Users.AnyAsync(cancellationToken);

    public async Task<IReadOnlyList<User>> ListAsync(CancellationToken cancellationToken = default) =>
        await _context.Users.AsNoTracking().OrderBy(u => u.Username).ToListAsync(cancellationToken);

    public void Add(User user) => _context.Users.Add(user);
}
