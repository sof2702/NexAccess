using Microsoft.EntityFrameworkCore;
using NexAccess.Application.Interfaces.Services;
using NexAccess.Domain.Entities;
using NexAccess.Infrastructure.Persistence;

namespace NexAccess.Infrastructure.Repositories;

public class EmployeeRepository
{
    private readonly NexAccessDbContext _db;
    public EmployeeRepository(NexAccessDbContext db) => _db = db;

    public async Task<Employee?> GetByIdAsync(int id) => await _db.Set<Employee>().FindAsync(id);
}
