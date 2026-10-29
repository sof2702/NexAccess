using NexAccess.Application.DTOs.Common;
using NexAccess.Application.DTOs.Credentials;
using NexAccess.Application.Exceptions;
using NexAccess.Application.Interfaces;
using NexAccess.Application.Interfaces.Repositories;
using NexAccess.Application.Validators;
using NexAccess.Domain.Entities;
using NexAccess.Domain.Enums;
using System.Security.Cryptography;

namespace NexAccess.Application.Services;

public class CredentialService
{
    private readonly ICredentialRepository _credentials;
    private readonly IEmployeeRepository _employees;
    private readonly IUnitOfWork _unitOfWork;
    private readonly AuditService _audit;
    private readonly IClock _clock;
    private readonly IMobileSessionRepository _mobileSessions;
    private readonly IUserRepository _users;


    public CredentialService(
        ICredentialRepository credentials,
        IEmployeeRepository employees,
        IUnitOfWork unitOfWork,
        AuditService audit,
        IClock clock,
        IMobileSessionRepository mobileSessions,
        IUserRepository users)
    {
        _credentials = credentials;
        _employees = employees;
        _unitOfWork = unitOfWork;
        _audit = audit;
        _clock = clock;
        _mobileSessions = mobileSessions;
        _users = users;
    }

    public async Task<PagedResult<CredentialResponse>> SearchAsync(CredentialQuery query, CancellationToken cancellationToken = default)
    {
        ModelValidator.Validate(query);
        var now = _clock.UtcNow;
        var (items, total) = await _credentials.SearchAsync(query, cancellationToken);
        return new PagedResult<CredentialResponse>(items.Select(item => CredentialResponse.From(item, now)).ToList(), query.Page, query.PageSize, total);
    }

    public async Task<CredentialResponse> GetAsync(long id, CancellationToken cancellationToken = default) =>
        CredentialResponse.From(await FindAsync(id, cancellationToken), _clock.UtcNow);

    public async Task<CredentialResponse> RegisterAsync(CredentialRegisterRequest request, CancellationToken cancellationToken = default)
    {
        ModelValidator.Validate(request);
        var employee = await _employees.GetByIdAsync(request.EmployeeId, cancellationToken)
            ?? throw new NotFoundException("Empleado", request.EmployeeId);

        var credential = await AddNewAsync(employee, request.CredentialCode, request.Type, request.Name, DateRange.ToUtc(request.ExpiresAt), cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _audit.Record("CREATE", "credencial", credential.Id, $"empleado={employee.Id};codigo={CredentialCode.Mask(credential.CredentialCode)}");
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return CredentialResponse.From(credential, _clock.UtcNow);
    }

    public async Task<Credential> AddNewAsync(
        Employee employee,
        string? rawCode,
        CredentialType type,
        string? name,
        DateTime? expiresAt,
        CancellationToken cancellationToken = default)
    {
        if (!CredentialCode.TryNormalize(rawCode, out var code))
        {
            throw new AppValidationException("codigo_credencial", $"El código de credencial debe tener entre {CredentialCode.MinLength} y {CredentialCode.MaxLength} caracteres alfanuméricos.");
        }

        if (!employee.IsActive)
        {
            throw new BusinessRuleException("EMPLOYEE_INACTIVE", "No se pueden registrar credenciales para un empleado inactivo.");
        }

        var now = _clock.UtcNow;
        if (expiresAt.HasValue && expiresAt.Value <= now)
        {
            throw new AppValidationException("expiresAt", "La fecha de expiración debe ser futura.");
        }

        if (await _credentials.CodeExistsAsync(code, cancellationToken))
        {
            throw new ConflictException("CREDENTIAL_ALREADY_REGISTERED", "La credencial ya está registrada en el sistema.");
        }

        var credential = new Credential
        {
            EmployeeId = employee.Id,
            Employee = employee,
            CredentialCode = code,
            Type = type,
            Name = string.IsNullOrWhiteSpace(name) ? null : name.Trim(),
            IsActive = true,
            IssuedAt = now,
            ExpiresAt = expiresAt,
            CreatedAt = now,
            UpdatedAt = now
        };
        _credentials.Add(credential);
        return credential;
    }

    public async Task<CredentialResponse> SetStatusAsync(long id, StatusRequest request, CancellationToken cancellationToken = default)
    {
        ModelValidator.Validate(request);
        var credential = await FindAsync(id, cancellationToken);
        if (credential.IsRevoked && request.IsActive == true)
        {
            throw new BusinessRuleException("CREDENTIAL_REVOKED", "Una credencial revocada no puede reactivarse.");
        }

        credential.IsActive = request.IsActive!.Value;
        credential.UpdatedAt = _clock.UtcNow;
        _audit.Record(credential.IsActive ? "ACTIVATE" : "DEACTIVATE", "credencial", credential.Id);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return CredentialResponse.From(credential, _clock.UtcNow);
    }

    public async Task<CredentialResponse> RevokeAsync(long id, RevokeCredentialRequest request, CancellationToken cancellationToken = default)
    {
        ModelValidator.Validate(request);
        var credential = await FindAsync(id, cancellationToken);
        if (credential.IsRevoked)
        {
            throw new ConflictException("CREDENTIAL_ALREADY_REVOKED", "La credencial ya fue revocada.");
        }

        credential.Revoke(request.Reason.Trim(), _clock.UtcNow);
        _audit.Record("REVOKE", "credencial", credential.Id, request.Reason.Trim());
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return CredentialResponse.From(credential, _clock.UtcNow);
    }

    private async Task<Credential> FindAsync(long id, CancellationToken cancellationToken) =>
        await _credentials.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Credencial", id);


public async Task<DigitalCredentialResponse> IssueDigitalAsync(
    long employeeId,
    DigitalCredentialRequest request,
    CancellationToken cancellationToken = default)
{
    if (employeeId <= 0)
    {
        throw new AppValidationException(
            "employeeId",
            "El identificador del empleado debe ser válido.");
    }

    ModelValidator.Validate(request);

    if (!request.InstallationId.HasValue ||
    request.InstallationId.Value == Guid.Empty)
{
    throw new AppValidationException(
        "installationId",
        "Debe indicar una instalación Android válida.");
}

var installationId = request.InstallationId.Value;

var sessions = await _mobileSessions.GetActiveByInstallationAsync(
    installationId,
    cancellationToken);

bool authorizedInstallation = false;

foreach (var session in sessions)
{
    var user = await _users.GetByIdAsync(
        session.UserId,
        cancellationToken);

    if (user is not null &&
        user.IsActive &&
        user.MobileAccessEnabled &&
        user.EmployeeId == employeeId)
    {
        authorizedInstallation = true;
        break;
    }
}

if (!authorizedInstallation)
{
    throw new BusinessRuleException(
        "INSTALLATION_NOT_AUTHORIZED",
        "La instalación Android no pertenece a una sesión móvil activa del empleado.");
}


    var employee = await _employees.GetByIdAsync(
        employeeId,
        cancellationToken)
        ?? throw new NotFoundException("Empleado", employeeId);

    // 256 bits generados por un generador criptográfico seguro.
    var code = Convert.ToHexString(
        RandomNumberGenerator.GetBytes(32));

    var credential = await AddNewAsync(
        employee,
        code,
        CredentialType.Digital,
        request.Name ?? "Credencial móvil",
        DateRange.ToUtc(request.ExpiresAt),
        cancellationToken);

    credential.InstallationId = installationId;

    await _unitOfWork.SaveChangesAsync(cancellationToken);

    _audit.Record(
        "ISSUE_DIGITAL",
        "credencial",
        credential.Id,
        $"empleado={employeeId};tipo=DIGITAL");

    await _unitOfWork.SaveChangesAsync(cancellationToken);

    return DigitalCredentialResponse.From(
        credential,
        _clock.UtcNow);
}


}
