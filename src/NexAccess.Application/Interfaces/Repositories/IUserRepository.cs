using NexAccess.Domain.Entities;

namespace NexAccess.Application.Interfaces.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken = default);
    Task<bool> AnyAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<User>> ListAsync(CancellationToken cancellationToken = default);
    void Add(User user);
}
