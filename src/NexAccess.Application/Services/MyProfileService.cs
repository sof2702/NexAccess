using NexAccess.Application.DTOs.AccessEvents;
using NexAccess.Application.DTOs.Common;
using NexAccess.Application.DTOs.Credentials;
using NexAccess.Application.DTOs.Me;
using NexAccess.Application.Exceptions;
using NexAccess.Application.Interfaces;
using NexAccess.Application.Interfaces.Repositories;
using NexAccess.Application.Validators;
using NexAccess.Domain.Entities;
using NexAccess.Domain.Enums;

namespace NexAccess.Application.Services;

public class MyProfileService
{
    private readonly ICurrentUser _currentUser;
    private readonly IUserRepository _users;
    private readonly IEmployeeRepository _employees;
    private readonly ICredentialRepository _credentials;
    private readonly IClock _clock;
    private readonly AccessEventService _accessEvents;

    public MyProfileService(
        ICurrentUser currentUser,
        IUserRepository users,
        IEmployeeRepository employees,
        ICredentialRepository credentials,
        IClock clock,
        AccessEventService accessEvents)
    {
        _currentUser = currentUser;
        _users = users;
        _employees = employees;
        _credentials = credentials;
        _clock = clock;
        _accessEvents = accessEvents;
    }

    // ==========================================
    // PERFIL PERSONAL
    // ==========================================
    public async Task<MyProfileResponse> GetAsync(
        CancellationToken cancellationToken = default)
    {
        var (user, employee) = await GetCurrentEmployeeAsync(
            cancellationToken);

        return new MyProfileResponse(
            user.Id,
            user.Username,
            SystemRoles.Empleado,
            employee.Id,
            employee.FullName,
            employee.IsActive);
    }

    // ==========================================
    // CREDENCIALES DEL EMPLEADO
    // ==========================================
    public async Task<PagedResult<MyCredentialResponse>>
        GetCredentialsAsync(
            int page = 1,
            int pageSize = 50,
            CancellationToken cancellationToken = default)
    {
        var pagination = new PageQuery
        {
            Page = page,
            PageSize = pageSize
        };

        ModelValidator.Validate(pagination);

        var (_, employee) = await GetCurrentEmployeeAsync(
            cancellationToken);

        var query = new CredentialQuery
        {
            EmployeeId = employee.Id,
            Page = page,
            PageSize = pageSize
        };

        var (items, total) = await _credentials.SearchAsync(
            query,
            cancellationToken);

        var now = _clock.UtcNow;

        return new PagedResult<MyCredentialResponse>(
            items.Select(item =>
                MyCredentialResponse.From(item, now)).ToList(),
            page,
            pageSize,
            total);
    }

    // ==========================================
    // HISTORIAL PERSONAL DE ACCESOS
    // ==========================================
    public async Task<PagedResult<AccessEventResponse>>
        GetAccessEventsAsync(
            int page = 1,
            int pageSize = 50,
            DateTime? from = null,
            DateTime? to = null,
            AccessResult? result = null,
            CancellationToken cancellationToken = default)
    {
        var (_, employee) = await GetCurrentEmployeeAsync(
            cancellationToken);

        // El empleado se identifica desde la sesión.
        // Nunca recibimos EmployeeId desde Android.
        var query = new AccessEventQuery
        {
            EmployeeId = employee.Id,
            Page = page,
            PageSize = pageSize,
            From = from,
            To = to,
            Result = result
        };

        return await _accessEvents.SearchAsync(
            query,
            cancellationToken);
    }

    // ==========================================
    // ENTREGA DE CREDENCIAL DIGITAL NFC
    // ==========================================
    public async Task<MyNfcCredentialResponse>
        GetNfcCredentialAsync(
            long credentialId,
            CancellationToken cancellationToken = default)
    {
        var (_, employee) = await GetCurrentEmployeeAsync(
            cancellationToken);

        var credential = await _credentials.GetByIdAsync(
            credentialId,
            cancellationToken);

        // Mismo error para credenciales ajenas
        // y credenciales inexistentes.
        if (credential is null ||
            credential.EmployeeId != employee.Id)
        {
            throw new NotFoundException(
                "Credencial",
                credentialId);
        }

        if (credential.Type != CredentialType.Digital)
        {
            throw new BusinessRuleException(
                "CREDENTIAL_NOT_DIGITAL",
                "Esta credencial no es digital.");
        }

        if (credential.IsRevoked ||
            !credential.IsActive ||
            credential.IsExpiredAt(_clock.UtcNow))
        {
            throw new BusinessRuleException(
                "CREDENTIAL_NOT_AVAILABLE",
                "La credencial digital no está activa o vigente.");
        }

        return new MyNfcCredentialResponse(
            credential.Id,
            credential.CredentialCode,
            credential.ExpiresAt);
    }

    // ==========================================
    // VALIDACIÓN CENTRALIZADA DE IDENTIDAD
    // ==========================================
    private async Task<(User User, Employee Employee)>
        GetCurrentEmployeeAsync(
            CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue)
        {
            throw new UnauthorizedException(
                "UNAUTHORIZED",
                "Usuario no autenticado.");
        }

        var user = await _users.GetByIdAsync(
            _currentUser.UserId.Value,
            cancellationToken);

        if (user is null ||
            !user.IsActive ||
            !user.MobileAccessEnabled ||
            !user.EmployeeId.HasValue)
        {
            throw new UnauthorizedException(
                "UNAUTHORIZED",
                "Cuenta personal no habilitada.");
        }

        var employee = await _employees.GetByIdAsync(
            user.EmployeeId.Value,
            cancellationToken);

        if (employee is null || !employee.IsActive)
        {
            throw new UnauthorizedException(
                "UNAUTHORIZED",
                "Empleado no habilitado.");
        }

        return (user, employee);
    }
}

