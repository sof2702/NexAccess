using NexAccess.Application.DTOs.Auth;
using NexAccess.Application.DTOs.Users;
using NexAccess.Application.Exceptions;
using NexAccess.Application.Interfaces;
using NexAccess.Application.Interfaces.Repositories;
using NexAccess.Application.Validators;
using NexAccess.Domain.Enums;

namespace NexAccess.Application.Services;

public class AuthService
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokens;
    private readonly IUnitOfWork _unitOfWork;
    private readonly AuditService _audit;
    private readonly IClock _clock;
    private readonly Lazy<string> _dummyHash;
    private readonly IEmployeeRepository _employees;

    public AuthService(
        IUserRepository users,
        IPasswordHasher hasher,
        ITokenService tokens,
        IUnitOfWork unitOfWork,
        AuditService audit,
        IClock clock,
	IEmployeeRepository employees)
    {
        _users = users;
        _hasher = hasher;
        _tokens = tokens;
        _unitOfWork = unitOfWork;
        _audit = audit;
        _clock = clock;
        _dummyHash = new Lazy<string>(() => hasher.Hash(Guid.NewGuid().ToString("N")));
        _employees = employees;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        ModelValidator.Validate(request);
        var username = request.Username.Trim();
        var user = await _users.GetByUsernameAsync(username, cancellationToken);
        var passwordMatches = _hasher.Verify(request.Password, user?.PasswordHash ?? _dummyHash.Value);

        if (user is null ||
    !passwordMatches ||
    !user.IsActive ||
    !user.WebAccessEnabled)
        {
            _audit.Record("LOGIN_FAILED", "usuario", user?.Id, $"username={username}", user?.Id);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedException("INVALID_CREDENTIALS", "Usuario o contraseña incorrectos.");
        }

// Las cuentas EMPLEADO requieren un empleado vinculado y activo.
if (user.SystemRole == SystemRole.Empleado)
{
    var employee = user.EmployeeId.HasValue
        ? await _employees.GetByIdAsync(
            user.EmployeeId.Value,
            cancellationToken)
        : null;

    if (employee is null || !employee.IsActive)
    {
        _audit.Record(
            "LOGIN_FAILED",
            "usuario",
            user.Id,
            "Empleado vinculado inexistente o inactivo",
            user.Id);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        throw new UnauthorizedException(
            "INVALID_CREDENTIALS",
            "Usuario o contraseña incorrectos.");
    }
}



        user.LastLoginAt = _clock.UtcNow;
        _audit.Record("LOGIN", "usuario", user.Id, null, user.Id);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var token = _tokens.Create(user);
        return new LoginResponse(token.Token, "Bearer", token.ExpiresAt, UserResponse.From(user));
    }

    public async Task<UserResponse> GetCurrentAsync(long userId, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            throw new UnauthorizedException("USER_INACTIVE", "El usuario no existe o está inactivo.");
        }

	if (user.SystemRole == SystemRole.Empleado)
{
    var employee = user.EmployeeId.HasValue
        ? await _employees.GetByIdAsync(
            user.EmployeeId.Value,
            cancellationToken)
        : null;

    if (employee is null || !employee.IsActive)
    {
        throw new UnauthorizedException(
            "USER_INACTIVE",
            "La cuenta no está habilitada.");
    }
}

        return UserResponse.From(user);
    }
}
