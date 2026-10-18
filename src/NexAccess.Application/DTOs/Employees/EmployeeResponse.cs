using NexAccess.Application.DTOs.Roles;
using NexAccess.Domain.Entities;

namespace NexAccess.Application.DTOs.Employees;

public sealed record EmployeeResponse(
    long Id,
    string DocumentNumber,
    string FirstName,
    string LastName,
    string FullName,
    string? Email,
    string? Phone,
    bool IsActive,
    IReadOnlyList<RoleSummary> Roles,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public static EmployeeResponse From(Employee employee) => new(
        employee.Id,
        employee.DocumentNumber,
        employee.FirstName,
        employee.LastName,
        employee.FullName,
        employee.Email,
        employee.Phone,
        employee.IsActive,
        employee.Roles
            .Where(link => link.Role is not null)
            .Select(link => RoleSummary.From(link.Role!))
            .OrderBy(role => role.Code)
            .ToList(),
        employee.CreatedAt,
        employee.UpdatedAt);
}
