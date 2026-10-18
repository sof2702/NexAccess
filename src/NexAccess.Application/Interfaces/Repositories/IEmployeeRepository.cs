using NexAccess.Application.DTOs.Employees;
using NexAccess.Domain.Entities;

namespace NexAccess.Application.Interfaces.Repositories;

public interface IEmployeeRepository
{
    Task<Employee?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<bool> DocumentExistsAsync(string documentNumber, long? excludeId, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string email, long? excludeId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Employee> Items, int Total)> SearchAsync(EmployeeQuery query, CancellationToken cancellationToken = default);
    void Add(Employee employee);
    void AddRole(EmployeeRole employeeRole);
    void RemoveRole(EmployeeRole employeeRole);
}
