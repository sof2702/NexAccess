using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NexAccess.Application.DTOs.Access;
using NexAccess.Application.DTOs.Devices;
using NexAccess.Application.Exceptions;
using NexAccess.Application.Interfaces;
using NexAccess.Application.Interfaces.Repositories;
using NexAccess.Application.Options;
using NexAccess.Application.Validators;
using NexAccess.Domain.Entities;
using NexAccess.Domain.Enums;

namespace NexAccess.Application.Services;

public class AccessService
{
    private readonly IDeviceRepository _devices;
    private readonly ICredentialRepository _credentials;
    private readonly IAccessPermissionRepository _permissions;
    private readonly IAccessEventRepository _events;
    private readonly AttendanceService _attendance;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly LocalTime _localTime;
    private readonly AccessOptions _options;
    private readonly ILogger<AccessService> _logger;
    private readonly IMobileSessionRepository _mobileSessions;
    private readonly IUserRepository _users;


    public AccessService(
        IDeviceRepository devices,
        ICredentialRepository credentials,
        IAccessPermissionRepository permissions,
        IAccessEventRepository events,
        AttendanceService attendance,
        IUnitOfWork unitOfWork,
        IClock clock,
        LocalTime localTime,
        IOptions<AccessOptions> options,
        ILogger<AccessService> logger,
        IMobileSessionRepository mobileSessions,
        IUserRepository users)
    {
        _devices = devices;
        _credentials = credentials;
        _permissions = permissions;
        _events = events;
        _attendance = attendance;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _localTime = localTime;
        _options = options.Value;
        _logger = logger;
        _mobileSessions = mobileSessions;
        _users = users;
    }

    public async Task<AccessVerifyResponse> VerifyAsync(
        AuthenticatedDevice authenticatedDevice,
        AccessVerifyRequest request,
        CancellationToken cancellationToken = default)
    {
        var device = await _devices.GetByIdAsync(authenticatedDevice.Id, cancellationToken);
        if (device is null || !device.IsActive)
        {
            throw new UnauthorizedException("DEVICE_UNAUTHORIZED", "Dispositivo no autorizado.");
        }

        var now = _clock.UtcNow;
        var accessEvent = new AccessEvent { DeviceId = device.Id, AreaId = device.AreaId, OccurredAt = now, CreatedAt = now };
        var reason = await DecideAsync(device, request, accessEvent, now, cancellationToken);
        var authorized = reason == AccessReason.Authorized;

        accessEvent.Result = authorized ? AccessResult.Autorizado : AccessResult.Rechazado;
        accessEvent.Reason = reason;
        _events.Add(accessEvent);

        AttendanceMark? mark = null;
        if (authorized && device.RegistersAttendance)
        {
            mark = await _attendance.RegisterFromDeviceAsync(accessEvent.EmployeeId!.Value, accessEvent.CredentialId, device.Id, now, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Verificación de acceso en {DeviceCode}: {Result} ({Reason}) credencial {Credential}",
            device.Code,
            accessEvent.Result,
            reason,
            CredentialCode.Mask(accessEvent.PresentedCode));

        if (!authorized)
        {
            return AccessVerifyResponse.Deny(reason);
        }

        var employeeName = accessEvent.Employee?.FullName;

        return device.ControlsAccess
            ? new AccessVerifyResponse(true, AccessVerifyResponse.OpenAction, reason, _options.OpenDurationMs, mark, employeeName)
            : new AccessVerifyResponse(true, AccessVerifyResponse.MarkAction, reason, null, mark, employeeName);
    }

    private async Task<string> DecideAsync(
        Device device,
        AccessVerifyRequest request,
        AccessEvent accessEvent,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(request.DeviceCode?.Trim(), device.Code, StringComparison.OrdinalIgnoreCase))
        {
            return AccessReason.DeviceMismatch;
        }

        if (!CredentialCode.TryNormalize(request.Credential, out var code))
        {
            return AccessReason.InvalidRequest;
        }

        accessEvent.PresentedCode = code;
        var credential = await _credentials.GetByCodeAsync(code, cancellationToken);
        if (credential is null)
        {
            return AccessReason.CredentialNotFound;
        }

        accessEvent.CredentialId = credential.Id;
        accessEvent.EmployeeId = credential.EmployeeId;

        if (credential.IsRevoked)
        {
            return AccessReason.CredentialRevoked;
        }

        if (!credential.IsActive)
        {
            return AccessReason.CredentialInactive;
        }

        if (credential.IsExpiredAt(now))
        {
            return AccessReason.CredentialExpired;
        }

        if (credential.Employee is not { IsActive: true })
        {
            return AccessReason.EmployeeInactive;
        }

        accessEvent.Employee = credential.Employee;

if (credential.Type == CredentialType.Digital)
{
    if (!credential.InstallationId.HasValue)
    {
        return AccessReason.CredentialInactive;
    }

    var sessions = await _mobileSessions.GetActiveByInstallationAsync(
        credential.InstallationId.Value,
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
            user.EmployeeId == credential.EmployeeId)
        {
            authorizedInstallation = true;
            break;
        }
    }

    if (!authorizedInstallation)
    {
        return AccessReason.CredentialInactive;
    }
}


        return device.ControlsAccess
            ? await DecideAreaAsync(device, credential.EmployeeId, now, cancellationToken)
            : AccessReason.Authorized;
    }

    private async Task<string> DecideAreaAsync(Device device, long employeeId, DateTime now, CancellationToken cancellationToken)
    {
        var area = device.Area;
        if (area is null)
        {
            return AccessReason.DeviceWithoutArea;
        }

        if (!area.IsActive)
        {
            return AccessReason.AreaInactive;
        }

        if (!area.IsRestricted)
        {
            return AccessReason.Authorized;
        }

        var permissions = await _permissions.GetActiveForEmployeeAndAreaAsync(employeeId, area.Id, cancellationToken);
        if (permissions.Count == 0)
        {
            return AccessReason.NoPermission;
        }

        var localNow = _localTime.ToLocal(now);
        return permissions.Any(permission => permission.AllowsAt(localNow))
            ? AccessReason.Authorized
            : AccessReason.OutsideSchedule;
    }
}
