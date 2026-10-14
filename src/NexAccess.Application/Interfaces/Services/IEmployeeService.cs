using NexAccess.Application.DTOs.Employees;

namespace NexAccess.Application.Interfaces.Services;

public interface IEmployeeService
{
    Task<EmployeeResponse> GetByIdAsync(int id);
}
