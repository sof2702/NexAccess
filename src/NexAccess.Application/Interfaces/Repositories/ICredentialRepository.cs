using NexAccess.Application.DTOs.Credentials;
using NexAccess.Domain.Entities;

namespace NexAccess.Application.Interfaces.Repositories;

public interface ICredentialRepository
{
    Task<Credential?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<Credential?> GetByCodeAsync(string credentialCode, CancellationToken cancellationToken = default);
    Task<bool> CodeExistsAsync(string credentialCode, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Credential> Items, int Total)> SearchAsync(CredentialQuery query, CancellationToken cancellationToken = default);
    void Add(Credential credential);
}
