using Microsoft.EntityFrameworkCore;
using NexAccess.Application.DTOs.Employees;
using NexAccess.Application.Interfaces.Repositories;
using NexAccess.Domain.Entities;

namespace NexAccess.Infrastructure.Persistence.Repositories;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly NexAccessDbContext _context;

    public EmployeeRepository(NexAccessDbContext context)
    {
        _context = context;
    }

    public Task<Employee?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        _context.Employees
            .Include(e => e.Roles).ThenInclude(r => r.Role)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public Task<bool> DocumentExistsAsync(string documentNumber, long? excludeId, CancellationToken cancellationToken = default) =>
        _context.Employees.AnyAsync(e => e.DocumentNumber == documentNumber && (excludeId == null || e.Id != excludeId), cancellationToken);

    public Task<bool> EmailExistsAsync(string email, long? excludeId, CancellationToken cancellationToken = default) =>
        _context.Employees.AnyAsync(e => e.Email != null && e.Email.ToLower() == email && (excludeId == null || e.Id != excludeId), cancellationToken);

    public Task<(IReadOnlyList<Employee> Items, int Total)> SearchAsync(EmployeeQuery query, CancellationToken cancellationToken = default)
    {
        var employees = _context.Employees.AsNoTracking().Include(e => e.Roles).ThenInclude(r => r.Role).AsQueryable();
        if (query.IsActive.HasValue)
        {
            employees = employees.Where(e => e.IsActive == query.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = query.Search.ToLikePattern();
            employees = employees.Where(e =>
                EF.Functions.ILike(e.DocumentNumber, pattern) ||
                EF.Functions.ILike(e.FirstName, pattern) ||
                EF.Functions.ILike(e.LastName, pattern) ||
                (e.Email != null && EF.Functions.ILike(e.Email, pattern)));
        }

        return employees.OrderBy(e => e.LastName).ThenBy(e => e.FirstName).ThenBy(e => e.Id).ToPageAsync(query, cancellationToken);
    }

    public void Add(Employee employee) => _context.Employees.Add(employee);

    public void AddRole(EmployeeRole employeeRole) => _context.EmployeeRoles.Add(employeeRole);

    public void RemoveRole(EmployeeRole employeeRole) => _context.EmployeeRoles.Remove(employeeRole);
}
