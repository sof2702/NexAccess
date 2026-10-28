
using NexAccess.Domain.Entities;

namespace NexAccess.Application.Interfaces.Repositories;

public interface IEmpresaRepository
{
    Task<Empresa?> GetAsync(
        CancellationToken cancellationToken = default);

    void Add(Empresa empresa);
}

