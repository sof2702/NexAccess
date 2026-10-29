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
    private readonly IMobileSessionRepository _mobileSessions;

    public UserService(
        IUserRepository users,
        IEmployeeRepository employees,
        IPasswordHasher hasher,
        IUnitOfWork unitOfWork,
        AuditService audit,
        ICurrentUser currentUser,
        IClock clock,
        IMobileSessionRepository mobileSessions)
    {
        _users = users;
        _employees = employees;
        _hasher = hasher;
        _unitOfWork = unitOfWork;
        _audit = audit;
        _currentUser = currentUser;
        _clock = clock;
        _mobileSessions = mobileSessions;
    }

    public async Task<IReadOnlyList<UserResponse>> ListAsync(CancellationToken cancellationToken = default) =>
        (await _users.ListAsync(cancellationToken)).Select(UserResponse.From).ToList();


public async Task<UserResponse> CreateAsync(
    CreateUserRequest request,
    CancellationToken cancellationToken = default)
{
    ModelValidator.Validate(request);

    if (!request.Role.HasValue ||
        !Enum.IsDefined(request.Role.Value))
    {
        throw new AppValidationException(
            "role",
            "Debe seleccionar un rol válido.");
    }

    var role = request.Role.Value;

    // Compatibilidad con el formulario anterior:
    // los roles administrativos son web y EMPLEADO es móvil.
    bool webAccess = request.WebAccessEnabled
        ?? (role != SystemRole.Empleado);

    bool mobileAccess = request.MobileAccessEnabled
        ?? (role == SystemRole.Empleado);

    if (!webAccess && !mobileAccess)
    {
        throw new AppValidationException(
            "access",
            "Debe habilitar el acceso web, móvil o ambos.");
    }

    if (role == SystemRole.Empleado && webAccess)
    {
        throw new AppValidationException(
            "webAccessEnabled",
            "El rol EMPLEADO no puede acceder al panel web.");
    }

    if (role != SystemRole.Empleado && !webAccess)
    {
        throw new AppValidationException(
            "webAccessEnabled",
            "Los roles administrativos requieren acceso web.");
    }

    if (mobileAccess && !request.EmployeeId.HasValue)
    {
        throw new AppValidationException(
            "employeeId",
            "El acceso móvil requiere un empleado vinculado.");
    }

    var username = request.Username.Trim();

    if (await _users.UsernameExistsAsync(
        username,
        cancellationToken))
    {
        throw new ConflictException(
            "USERNAME_DUPLICATE",
            "Ya existe un usuario con ese nombre.");
    }

    if (request.EmployeeId.HasValue)
    {
        var employee = await _employees.GetByIdAsync(
            request.EmployeeId.Value,
            cancellationToken);

        if (employee is null)
        {
            throw new NotFoundException(
                "Empleado",
                request.EmployeeId.Value);
        }

        if (mobileAccess && !employee.IsActive)
        {
            throw new AppValidationException(
                "employeeId",
                "No se puede habilitar acceso móvil para un empleado inactivo.");
        }
    }

    var user = NewUser(
        username,
        request.Password,
        role,
        request.EmployeeId);

    user.WebAccessEnabled = webAccess;
    user.MobileAccessEnabled = mobileAccess;

    _users.Add(user);

    await _unitOfWork.SaveChangesAsync(cancellationToken);

    _audit.Record(
        "CREATE",
        "usuario",
        user.Id,
        $"rol={SystemRoles.From(role)};" +
        $"web={webAccess};movil={mobileAccess}");

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

    
private User NewUser(
    string username,
    string password,
    SystemRole role,
    long? employeeId)
{
    var now = _clock.UtcNow;

    return new User
    {
        Username = username,
        PasswordHash = _hasher.Hash(password),
        SystemRole = role,
        EmployeeId = employeeId,

        WebAccessEnabled = role != SystemRole.Empleado,
        MobileAccessEnabled = role == SystemRole.Empleado,

        IsActive = true,
        CreatedAt = now,
        UpdatedAt = now
    };
}


public async Task<UserResponse> UpdateAccessAsync(
    long id,
    UpdateUserAccessRequest request,
    CancellationToken cancellationToken = default)
{
    ModelValidator.Validate(request);

    var user = await _users.GetByIdAsync(
        id,
        cancellationToken)
        ?? throw new NotFoundException("Usuario", id);

    bool webAccess = request.WebAccessEnabled!.Value;
    bool mobileAccess = request.MobileAccessEnabled!.Value;

    if (!webAccess && !mobileAccess)
    {
        throw new AppValidationException(
            "access",
            "Debe habilitar al menos un tipo de acceso.");
    }

    // EMPLEADO no puede utilizar el panel administrativo.
    if (user.SystemRole == SystemRole.Empleado && webAccess)
    {
        throw new AppValidationException(
            "webAccessEnabled",
            "El rol EMPLEADO no puede acceder al panel web.");
    }

    // Los roles administrativos conservan su canal web.
    if (user.SystemRole != SystemRole.Empleado && !webAccess)
    {
        throw new AppValidationException(
            "webAccessEnabled",
            "Los roles administrativos requieren acceso web.");
    }

    if (mobileAccess)
    {
        if (!user.EmployeeId.HasValue)
        {
            throw new AppValidationException(
                "mobileAccessEnabled",
                "El acceso móvil requiere un empleado vinculado.");
        }

        var employee = await _employees.GetByIdAsync(
            user.EmployeeId.Value,
            cancellationToken);

        if (employee is null || !employee.IsActive)
        {
            throw new AppValidationException(
                "mobileAccessEnabled",
                "El empleado vinculado debe existir y estar activo.");
        }
    }

    // Evitar que el administrador se quite su propio
    // acceso web durante esta operación.
    if (_currentUser.UserId == user.Id && !webAccess)
    {
        throw new BusinessRuleException(
            "CANNOT_DISABLE_OWN_WEB_ACCESS",
            "No puede deshabilitar su propio acceso web.");
    }

    
bool disableMobile =
    user.MobileAccessEnabled && !mobileAccess;

user.WebAccessEnabled = webAccess;
user.MobileAccessEnabled = mobileAccess;
user.UpdatedAt = _clock.UtcNow;

_audit.Record(
    "UPDATE_ACCESS",
    "usuario",
    user.Id,
    $"web={webAccess};movil={mobileAccess}");

await _unitOfWork.SaveChangesAsync(cancellationToken);

// Cuando se deshabilita el acceso móvil,
// revocar todas las sesiones Android del usuario.
if (disableMobile)
{
    var revoked = await _mobileSessions.RevokeAllForUserAsync(
        user.Id,
        _clock.UtcNow,
        cancellationToken);

    _audit.Record(
        "REVOKE_MOBILE_SESSIONS",
        "usuario",
        user.Id,
        $"sesiones_revocadas={revoked}");

    await _unitOfWork.SaveChangesAsync(cancellationToken);
}

return UserResponse.From(user);

}




}
