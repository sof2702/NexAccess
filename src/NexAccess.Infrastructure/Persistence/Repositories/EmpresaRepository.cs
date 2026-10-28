
using Microsoft.EntityFrameworkCore;
using NexAccess.Application.Interfaces.Repositories;
using NexAccess.Domain.Entities;

namespace NexAccess.Infrastructure.Persistence.Repositories;

public class EmpresaRepository : IEmpresaRepository
{
    private readonly NexAccessDbContext _context;

    public EmpresaRepository(NexAccessDbContext context)
    {
        _context = context;
    }

    public Task<Empresa?> GetAsync(
        CancellationToken cancellationToken = default)
    {
        return _context.Empresas
            .OrderBy(e => e.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public void Add(Empresa empresa)
    {
        _context.Empresas.Add(empresa);
    }
}

