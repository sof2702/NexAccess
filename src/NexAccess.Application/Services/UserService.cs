using NexAccess.Application.DTOs.Common;
using NexAccess.Application.DTOs.Users;
using NexAccess.Application.Exceptions;
using NexAccess.Application.Interfaces;
using NexAccess.Application.Interfaces.Repositories;
using NexAccess.Application.Validators;
using NexAccess.Domain.Entities;
using NexAccess.Domain.Enums;

namespace NexAccess.Application.Services;

public class UserService
{
    public const int BootstrapMinPasswordLength = 12;

    private readonly IUserRepository _users;
    private readonly IEmployeeRepository _employees;
    private readonly IPasswordHasher _hasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly AuditService _audit;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public UserService(
        IUserRepository users,
        IEmployeeRepository employees,
        IPasswordHasher hasher,
        IUnitOfWork unitOfWork,
        AuditService audit,
        ICurrentUser currentUser,
        IClock clock)
    {
        _users = users;
        _employees = employees;
        _hasher = hasher;
        _unitOfWork = unitOfWork;
        _audit = audit;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<IReadOnlyList<UserResponse>> ListAsync(CancellationToken cancellationToken = default) =>
        (await _users.ListAsync(cancellationToken)).Select(UserResponse.From).ToList();

    public async Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        ModelValidator.Validate(request);
        var username = request.Username.Trim();
        if (await _users.UsernameExistsAsync(username, cancellationToken))
        {
            throw new ConflictException("USERNAME_DUPLICATE", "Ya existe un usuario con ese nombre.");
        }

        if (request.EmployeeId.HasValue && await _employees.GetByIdAsync(request.EmployeeId.Value, cancellationToken) is null)
        {
            throw new NotFoundException("Empleado", request.EmployeeId.Value);
        }

        var user = NewUser(username, request.Password, request.Role!.Value, request.EmployeeId);
        _users.Add(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _audit.Record("CREATE", "usuario", user.Id, $"rol={SystemRoles.From(user.SystemRole)}");
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return UserResponse.From(user);
    }

    public async Task<UserResponse> SetStatusAsync(long id, StatusRequest request, CancellationToken cancellationToken = default)
    {
        ModelValidator.Validate(request);
        var user = await _users.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Usuario", id);
        if (user.Id == _currentUser.UserId && request.IsActive == false)
        {
            throw new BusinessRuleException("CANNOT_DEACTIVATE_SELF", "No puede desactivar su propio usuario.");
        }

        user.IsActive = request.IsActive!.Value;
        user.UpdatedAt = _clock.UtcNow;
        _audit.Record(user.IsActive ? "ACTIVATE" : "DEACTIVATE", "usuario", user.Id);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return UserResponse.From(user);
    }

    public async Task ChangePasswordAsync(long id, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        ModelValidator.Validate(request);
        var user = await _users.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Usuario", id);
        user.PasswordHash = _hasher.Hash(request.NewPassword);
        user.UpdatedAt = _clock.UtcNow;
        _audit.Record("CHANGE_PASSWORD", "usuario", user.Id);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> EnsureBootstrapAdminAsync(string? username, string? password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
        {
            return false;
        }

        if (password.Length < BootstrapMinPasswordLength)
        {
            throw new InvalidOperationException($"Bootstrap:AdminPassword debe tener al menos {BootstrapMinPasswordLength} caracteres.");
        }

        if (await _users.AnyAsync(cancellationToken))
        {
            return false;
        }

        var user = NewUser(username.Trim(), password, SystemRole.Admin, null);
        _users.Add(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _audit.Record("BOOTSTRAP_ADMIN", "usuario", user.Id, null, user.Id);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private User NewUser(string username, string password, SystemRole role, long? employeeId)
    {
        var now = _clock.UtcNow;
        return new User
        {
            Username = username,
            PasswordHash = _hasher.Hash(password),
            SystemRole = role,
            EmployeeId = employeeId,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
