using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexAccess.Application.DTOs.Common;
using NexAccess.Application.DTOs.Employees;
using NexAccess.Application.DTOs.Roles;
using NexAccess.Application.Services;
using NexAccess.Domain.Enums;

namespace NexAccess.API.Controllers;

[ApiController]
[Route("api/v1/employees")]
[Authorize(Roles = SystemRoles.All)]
public class EmployeesController(EmployeeService employees) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<EmployeeResponse>> Search([FromQuery] EmployeeQuery query, CancellationToken cancellationToken) =>
        employees.SearchAsync(query, cancellationToken);

    [HttpGet("{id:long}")]
    public Task<EmployeeResponse> Get(long id, CancellationToken cancellationToken) =>
        employees.GetAsync(id, cancellationToken);

    [HttpPost]
    [Authorize(Roles = SystemRoles.AdminOrRrhh)]
    public async Task<ActionResult<EmployeeResponse>> Create(EmployeeRequest request, CancellationToken cancellationToken)
    {
        var employee = await employees.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = employee.Id }, employee);
    }

    [HttpPut("{id:long}")]
    [Authorize(Roles = SystemRoles.AdminOrRrhh)]
    public Task<EmployeeResponse> Update(long id, EmployeeRequest request, CancellationToken cancellationToken) =>
        employees.UpdateAsync(id, request, cancellationToken);

    [HttpPatch("{id:long}/status")]
    [Authorize(Roles = SystemRoles.AdminOrRrhh)]
    public Task<EmployeeResponse> SetStatus(long id, StatusRequest request, CancellationToken cancellationToken) =>
        employees.SetStatusAsync(id, request, cancellationToken);

    [HttpGet("{id:long}/roles")]
    public Task<IReadOnlyList<RoleSummary>> GetRoles(long id, CancellationToken cancellationToken) =>
        employees.GetRolesAsync(id, cancellationToken);

    [HttpPost("{id:long}/roles/{roleId:long}")]
    [Authorize(Roles = SystemRoles.AdminOrSeguridad)]
    public Task<EmployeeResponse> AssignRole(long id, long roleId, CancellationToken cancellationToken) =>
        employees.AssignRoleAsync(id, roleId, cancellationToken);

    [HttpDelete("{id:long}/roles/{roleId:long}")]
    [Authorize(Roles = SystemRoles.AdminOrSeguridad)]
    public Task<EmployeeResponse> RemoveRole(long id, long roleId, CancellationToken cancellationToken) =>
        employees.RemoveRoleAsync(id, roleId, cancellationToken);
}
