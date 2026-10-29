using System.Security.Cryptography;
using System.Text;
using NexAccess.Application.DTOs.Auth;
using NexAccess.Application.DTOs.Users;
using NexAccess.Application.Exceptions;
using NexAccess.Application.Interfaces;
using NexAccess.Application.Interfaces.Repositories;
using NexAccess.Application.Validators;
using NexAccess.Domain.Entities;
using NexAccess.Domain.Enums;

namespace NexAccess.Application.Services;

public class MobileAuthService
{
    private readonly IUserRepository _users;
    private readonly IEmployeeRepository _employees;
    private readonly IMobileSessionRepository _sessions;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokens;
    private readonly IUnitOfWork _unitOfWork;
    private readonly AuditService _audit;
    private readonly IClock _clock;
    private readonly IUsedMobileRefreshTokenRepository _usedTokens;

    public MobileAuthService(
        IUserRepository users,
        IEmployeeRepository employees,
        IMobileSessionRepository sessions,
        IPasswordHasher hasher,
        ITokenService tokens,
        IUnitOfWork unitOfWork,
        AuditService audit,
        IClock clock,
        IUsedMobileRefreshTokenRepository usedTokens)
    {
        _users = users;
        _employees = employees;
        _sessions = sessions;
        _hasher = hasher;
        _tokens = tokens;
        _unitOfWork = unitOfWork;
        _audit = audit;
        _clock = clock;
        _usedTokens = usedTokens;
    }

    public async Task<MobileLoginResponse> LoginAsync(
        MobileLoginRequest request,
        CancellationToken cancellationToken = default)
    {
        ModelValidator.Validate(request);

        if (request.InstallationId == Guid.Empty)
        {
            throw new AppValidationException(
                "installationId",
                "El identificador de instalación es obligatorio.");
        }

        var username = request.Username.Trim();

        var user = await _users.GetByUsernameAsync(
            username,
            cancellationToken);

        // Verificar la contraseña sin revelar detalles
        // sobre el usuario consultado.
        var passwordHash = user?.PasswordHash
            ?? _hasher.Hash("NexAccess-Dummy-Password");

        var passwordValid = _hasher.Verify(
            request.Password,
            passwordHash);

        if (user is null ||
            !passwordValid ||
            !user.IsActive ||
            !user.MobileAccessEnabled ||
            !user.EmployeeId.HasValue)
        {
            throw new UnauthorizedException(
                "INVALID_CREDENTIALS",
                "Usuario o contraseña incorrectos.");
        }

        var employee = await _employees.GetByIdAsync(
            user.EmployeeId.Value,
            cancellationToken);

        if (employee is null || !employee.IsActive)
        {
            throw new UnauthorizedException(
                "INVALID_CREDENTIALS",
                "Usuario o contraseña incorrectos.");
        }

        var now = _clock.UtcNow;

        // Secreto criptográfico de 256 bits.
        var refreshToken = Convert.ToHexString(
            RandomNumberGenerator.GetBytes(32));

        // Guardar solamente su hash.
        var refreshHash = Convert.ToHexString(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(refreshToken)));

        var session = new MobileSession
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            InstallationId = request.InstallationId,
            RefreshTokenHash = refreshHash,
            IsActive = true,
            CreatedAt = now
        };

        _sessions.Add(session);

        user.LastLoginAt = now;

        _audit.Record(
            "MOBILE_LOGIN",
            "sesion_movil",
            null,
            $"usuario={user.Id};sesion={session.Id}",
            user.Id);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var jwt = _tokens.CreateMobile(
            user,
            session.Id);

        // En la sesión móvil, el rol efectivo es EMPLEADO,
        // independientemente del rol administrativo.
        var mobileUser = new UserResponse(
            user.Id,
            user.Username,
            SystemRole.Empleado,
            user.EmployeeId,
            user.IsActive,
            user.LastLoginAt,
            user.WebAccessEnabled,
            user.MobileAccessEnabled);

        return new MobileLoginResponse(
            jwt.Token,
            "Bearer",
            jwt.ExpiresAt,
            refreshToken,
            session.Id,
            mobileUser);
    }



public async Task<MobileLoginResponse> RefreshAsync(
    MobileRefreshRequest request,
    CancellationToken cancellationToken = default)
{
    ModelValidator.Validate(request);

    // El token original nunca se almacena en PostgreSQL.
    var currentTokenHash = Convert.ToHexString(
        SHA256.HashData(
            Encoding.UTF8.GetBytes(request.RefreshToken)));

    var session = await _sessions.GetByRefreshTokenHashAsync(
    currentTokenHash,
    cancellationToken);

if (session is null || session.IsRevoked)
{
    // Puede ser un token que ya fue consumido.
    await AuditUsedRefreshTokenAsync(
        currentTokenHash,
        cancellationToken);

    throw new UnauthorizedException(
        "INVALID_REFRESH_TOKEN",
        "El token de renovación no es válido.");
}

    var user = await _users.GetByIdAsync(
        session.UserId,
        cancellationToken);

    if (user is null ||
        !user.IsActive ||
        !user.MobileAccessEnabled ||
        !user.EmployeeId.HasValue)
    {
        throw new UnauthorizedException(
            "MOBILE_ACCESS_DISABLED",
            "La cuenta móvil no está habilitada.");
    }

    var employee = await _employees.GetByIdAsync(
        user.EmployeeId.Value,
        cancellationToken);

    if (employee is null || !employee.IsActive)
    {
        throw new UnauthorizedException(
            "EMPLOYEE_INACTIVE",
            "El empleado no está habilitado.");
    }

    var now = _clock.UtcNow;

    // Generar un nuevo secreto de 256 bits.
    var newRefreshToken = Convert.ToHexString(
        RandomNumberGenerator.GetBytes(32));

    var newRefreshHash = Convert.ToHexString(
        SHA256.HashData(
            Encoding.UTF8.GetBytes(newRefreshToken)));

    // Rotación atómica: únicamente se acepta
    // si el hash anterior todavía coincide.
    var rotated = await _sessions.RotateAndRecordTokenAsync(
    session.Id,
    currentTokenHash,
    newRefreshHash,
    now,
    cancellationToken);

    if (!rotated)
{
    // Otra solicitud pudo renovar el token
    // mientras procesábamos esta operación.
    await AuditUsedRefreshTokenAsync(
        currentTokenHash,
        cancellationToken);

    throw new UnauthorizedException(
        "INVALID_REFRESH_TOKEN",
        "El token de renovación no es válido.");
}

    var jwt = _tokens.CreateMobile(
        user,
        session.Id);

    // La respuesta móvil siempre presenta el rol
    // efectivo EMPLEADO.
    var mobileUser = new UserResponse(
        user.Id,
        user.Username,
        SystemRole.Empleado,
        user.EmployeeId,
        user.IsActive,
        user.LastLoginAt,
        user.WebAccessEnabled,
        user.MobileAccessEnabled);

    return new MobileLoginResponse(
        jwt.Token,
        "Bearer",
        jwt.ExpiresAt,
        newRefreshToken,
        session.Id,
        mobileUser);
}



public async Task LogoutAsync(
    MobileLogoutRequest request,
    CancellationToken cancellationToken = default)
{
    ModelValidator.Validate(request);

    var token = request.RefreshToken.Trim();

    if (string.IsNullOrWhiteSpace(token))
    {
        throw new AppValidationException(
            "refreshToken",
            "El token de renovación es obligatorio.");
    }

    var tokenHash = Convert.ToHexString(
        SHA256.HashData(
            Encoding.UTF8.GetBytes(token)));

    var session = await _sessions.GetByRefreshTokenHashAsync(
        tokenHash,
        cancellationToken);

    // Respuesta idempotente: una sesión inexistente
    // o previamente revocada no produce error.
    if (session is null || session.IsRevoked)
    {
        return;
    }

    var revoked = await _sessions.RevokeAsync(
        session.Id,
        _clock.UtcNow,
        cancellationToken);

    if (!revoked)
    {
        return;
    }

    _audit.Record(
        "MOBILE_LOGOUT",
        "sesion_movil",
        null,
        $"usuario={session.UserId};sesion={session.Id}",
        session.UserId);

    await _unitOfWork.SaveChangesAsync(cancellationToken);
}


private async Task AuditUsedRefreshTokenAsync(
    string tokenHash,
    CancellationToken cancellationToken)
{
    var usedToken = await _usedTokens.FindByHashAsync(
        tokenHash,
        cancellationToken);

    if (usedToken is null)
    {
        // Token desconocido. No hay sesión identificable.
        return;
    }

    // Identificamos la reutilización, pero no sabemos
    // todavía si fue maliciosa o accidental.
    _audit.Record(
        "MOBILE_REFRESH_REUSE",
        "sesion_movil",
        null,
        $"sesion={usedToken.SessionId};token_usado_id={usedToken.Id}");

    await _unitOfWork.SaveChangesAsync(
        cancellationToken);
}



}

