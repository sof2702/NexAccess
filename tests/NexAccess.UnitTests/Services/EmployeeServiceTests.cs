using NexAccess.Application.DTOs.Common;
using NexAccess.Application.DTOs.Employees;
using NexAccess.Application.Exceptions;
using NexAccess.Domain.Entities;
using NexAccess.UnitTests.Support;
using NSubstitute;

namespace NexAccess.UnitTests.Services;

public class EmployeeServiceTests
{
    private readonly ServiceFactory _factory = new();

    private static EmployeeRequest ValidRequest() => new()
    {
        DocumentNumber = " 1234567 ",
        FirstName = " Ana ",
        LastName = "Pérez",
        Email = " Ana@Example.COM "
    };

    [Fact]
    public async Task Create_normalizes_data_and_saves()
    {
        var response = await _factory.EmployeeService().CreateAsync(ValidRequest());

        Assert.Equal("1234567", response.DocumentNumber);
        Assert.Equal("Ana", response.FirstName);
        Assert.Equal("ana@example.com", response.Email);
        Assert.True(response.IsActive);
        _factory.Employees.Received(1).Add(Arg.Any<Employee>());
        await _factory.UnitOfWork.ReceivedWithAnyArgs(2).SaveChangesAsync(default);
    }

    [Fact]
    public async Task Create_rejects_duplicate_document()
    {
        _factory.Employees.DocumentExistsAsync("1234567", null, Arg.Any<CancellationToken>()).Returns(true);

        var error = await Assert.ThrowsAsync<ConflictException>(() => _factory.EmployeeService().CreateAsync(ValidRequest()));

        Assert.Equal("EMPLOYEE_DOCUMENT_DUPLICATE", error.Code);
        _factory.Employees.DidNotReceiveWithAnyArgs().Add(default!);
    }

    [Fact]
    public async Task Create_rejects_invalid_dto()
    {
        var error = await Assert.ThrowsAsync<AppValidationException>(() =>
            _factory.EmployeeService().CreateAsync(new EmployeeRequest { DocumentNumber = "1", Email = "no-es-correo" }));

        Assert.Contains(nameof(EmployeeRequest.FirstName), error.Errors.Keys);
        Assert.Contains(nameof(EmployeeRequest.Email), error.Errors.Keys);
        Assert.Contains(nameof(EmployeeRequest.DocumentNumber), error.Errors.Keys);
    }

    [Fact]
    public async Task Update_excludes_own_id_when_checking_duplicates()
    {
        var employee = TestData.Employee(10);
        _factory.Employees.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(employee);

        var response = await _factory.EmployeeService().UpdateAsync(10, ValidRequest());

        Assert.Equal("1234567", response.DocumentNumber);
        await _factory.Employees.Received(1).DocumentExistsAsync("1234567", 10, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_of_missing_employee_is_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _factory.EmployeeService().UpdateAsync(99, ValidRequest()));
    }

    [Fact]
    public async Task Deactivate_sets_status()
    {
        var employee = TestData.Employee(10);
        _factory.Employees.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(employee);

        var response = await _factory.EmployeeService().SetStatusAsync(10, new StatusRequest { IsActive = false });

        Assert.False(response.IsActive);
        Assert.False(employee.IsActive);
    }

    [Fact]
    public async Task Assigning_same_role_twice_is_conflict()
    {
        var employee = TestData.Employee(10);
        employee.Roles.Add(new EmployeeRole { EmployeeId = 10, RoleId = 1 });
        _factory.Employees.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(employee);
        _factory.Roles.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(new Role { Id = 1, Code = "RRHH", IsActive = true });

        var error = await Assert.ThrowsAsync<ConflictException>(() => _factory.EmployeeService().AssignRoleAsync(10, 1));

        Assert.Equal("ROLE_ALREADY_ASSIGNED", error.Code);
    }

    [Fact]
    public async Task Inactive_role_cannot_be_assigned()
    {
        _factory.Employees.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(TestData.Employee(10));
        _factory.Roles.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(new Role { Id = 1, Code = "RRHH", IsActive = false });

        await Assert.ThrowsAsync<BusinessRuleException>(() => _factory.EmployeeService().AssignRoleAsync(10, 1));
    }
}
