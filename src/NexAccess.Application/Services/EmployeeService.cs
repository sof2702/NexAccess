using NexAccess.Application.DTOs.Common;
using NexAccess.Application.DTOs.Employees;
using NexAccess.Application.DTOs.Roles;
using NexAccess.Application.Exceptions;
using NexAccess.Application.Interfaces;
using NexAccess.Application.Interfaces.Repositories;
using NexAccess.Application.Validators;
using NexAccess.Domain.Entities;

namespace NexAccess.Application.Services;

public class EmployeeService
{
    private readonly IEmployeeRepository _employees;
    private readonly IRoleRepository _roles;
    private readonly IUnitOfWork _unitOfWork;
    private readonly AuditService _audit;
    private readonly IClock _clock;

    public EmployeeService(
        IEmployeeRepository employees,
        IRoleRepository roles,
        IUnitOfWork unitOfWork,
        AuditService audit,
        IClock clock)
    {
        _employees = employees;
        _roles = roles;
        _unitOfWork = unitOfWork;
        _audit = audit;
        _clock = clock;
    }

    public async Task<PagedResult<EmployeeResponse>> SearchAsync(EmployeeQuery query, CancellationToken cancellationToken = default)
    {
        ModelValidator.Validate(query);
        var (items, total) = await _employees.SearchAsync(query, cancellationToken);
        return new PagedResult<EmployeeResponse>(items.Select(EmployeeResponse.From).ToList(), query.Page, query.PageSize, total);
    }

    public async Task<EmployeeResponse> GetAsync(long id, CancellationToken cancellationToken = default) =>
        EmployeeResponse.From(await FindAsync(id, cancellationToken));

    public async Task<EmployeeResponse> CreateAsync(EmployeeRequest request, CancellationToken cancellationToken = default)
    {
        ModelValidator.Validate(request);
        var now = _clock.UtcNow;
        var employee = new Employee { IsActive = true, CreatedAt = now };
        await ApplyAsync(employee, request, null, cancellationToken);
        _employees.Add(employee);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _audit.Record("CREATE", "empleado", employee.Id, $"cedula={employee.DocumentNumber}");
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return EmployeeResponse.From(employee);
    }

    public async Task<EmployeeResponse> UpdateAsync(long id, EmployeeRequest request, CancellationToken cancellationToken = default)
    {
        ModelValidator.Validate(request);
        var employee = await FindAsync(id, cancellationToken);
        await ApplyAsync(employee, request, id, cancellationToken);
        _audit.Record("UPDATE", "empleado", employee.Id);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return EmployeeResponse.From(employee);
    }

    public async Task<EmployeeResponse> SetStatusAsync(long id, StatusRequest request, CancellationToken cancellationToken = default)
    {
        ModelValidator.Validate(request);
        var employee = await FindAsync(id, cancellationToken);
        employee.IsActive = request.IsActive!.Value;
        employee.UpdatedAt = _clock.UtcNow;
        _audit.Record(employee.IsActive ? "ACTIVATE" : "DEACTIVATE", "empleado", employee.Id);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return EmployeeResponse.From(employee);
    }

    public async Task<IReadOnlyList<RoleSummary>> GetRolesAsync(long id, CancellationToken cancellationToken = default) =>
        EmployeeResponse.From(await FindAsync(id, cancellationToken)).Roles;

    public async Task<EmployeeResponse> AssignRoleAsync(long employeeId, long roleId, CancellationToken cancellationToken = default)
    {
        var employee = await FindAsync(employeeId, cancellationToken);
        var role = await _roles.GetByIdAsync(roleId, cancellationToken) ?? throw new NotFoundException("Rol", roleId);
        if (!role.IsActive)
        {
            throw new BusinessRuleException("ROLE_INACTIVE", "No se puede asignar un rol inactivo.");
        }

        if (employee.Roles.Any(link => link.RoleId == roleId))
        {
            throw new ConflictException("ROLE_ALREADY_ASSIGNED", "El empleado ya tiene asignado ese rol.");
        }

        var link = new EmployeeRole { EmployeeId = employee.Id, RoleId = role.Id, Role = role, CreatedAt = _clock.UtcNow };
        _employees.AddRole(link);
        employee.Roles.Add(link);
        _audit.Record("ASSIGN_ROLE", "empleado", employee.Id, $"rol={role.Code}");
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return EmployeeResponse.From(employee);
    }

    public async Task<EmployeeResponse> RemoveRoleAsync(long employeeId, long roleId, CancellationToken cancellationToken = default)
    {
        var employee = await FindAsync(employeeId, cancellationToken);
        var link = employee.Roles.FirstOrDefault(item => item.RoleId == roleId)
            ?? throw new NotFoundException("Asignación de rol", $"{employeeId}/{roleId}");

        _employees.RemoveRole(link);
        employee.Roles.Remove(link);
        _audit.Record("REMOVE_ROLE", "empleado", employee.Id, $"rol={link.Role?.Code ?? roleId.ToString()}");
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return EmployeeResponse.From(employee);
    }

    private async Task<Employee> FindAsync(long id, CancellationToken cancellationToken) =>
        await _employees.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Empleado", id);

    private async Task ApplyAsync(Employee employee, EmployeeRequest request, long? excludeId, CancellationToken cancellationToken)
    {
        var documentNumber = request.DocumentNumber.Trim();
        var email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim().ToLowerInvariant();

        if (await _employees.DocumentExistsAsync(documentNumber, excludeId, cancellationToken))
        {
            throw new ConflictException("EMPLOYEE_DOCUMENT_DUPLICATE", "Ya existe un empleado con esa cédula.");
        }

        if (email is not null && await _employees.EmailExistsAsync(email, excludeId, cancellationToken))
        {
            throw new ConflictException("EMPLOYEE_EMAIL_DUPLICATE", "Ya existe un empleado con ese correo.");
        }

        employee.DocumentNumber = documentNumber;
        employee.FirstName = request.FirstName.Trim();
        employee.LastName = request.LastName.Trim();
        employee.Email = email;
        employee.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        employee.UpdatedAt = _clock.UtcNow;
    }
}
