using NexAccess.Application.DTOs.Common;
using NexAccess.Application.DTOs.Permissions;
using NexAccess.Application.Exceptions;
using NexAccess.Application.Interfaces;
using NexAccess.Application.Interfaces.Repositories;
using NexAccess.Application.Validators;
using NexAccess.Domain.Entities;

namespace NexAccess.Application.Services;

public class AccessPermissionService
{
    private readonly IAccessPermissionRepository _permissions;
    private readonly IRoleRepository _roles;
    private readonly IAreaRepository _areas;
    private readonly IUnitOfWork _unitOfWork;
    private readonly AuditService _audit;
    private readonly IClock _clock;

    public AccessPermissionService(
        IAccessPermissionRepository permissions,
        IRoleRepository roles,
        IAreaRepository areas,
        IUnitOfWork unitOfWork,
        AuditService audit,
        IClock clock)
    {
        _permissions = permissions;
        _roles = roles;
        _areas = areas;
        _unitOfWork = unitOfWork;
        _audit = audit;
        _clock = clock;
    }

    public async Task<PagedResult<AccessPermissionResponse>> SearchAsync(AccessPermissionQuery query, CancellationToken cancellationToken = default)
    {
        ModelValidator.Validate(query);
        var (items, total) = await _permissions.SearchAsync(query, cancellationToken);
        return new PagedResult<AccessPermissionResponse>(items.Select(AccessPermissionResponse.From).ToList(), query.Page, query.PageSize, total);
    }

    public async Task<AccessPermissionResponse> GetAsync(long id, CancellationToken cancellationToken = default) =>
        AccessPermissionResponse.From(await FindAsync(id, cancellationToken));

    public async Task<AccessPermissionResponse> CreateAsync(AccessPermissionRequest request, CancellationToken cancellationToken = default)
    {
        ModelValidator.Validate(request);
        var permission = new AccessPermission { CreatedAt = _clock.UtcNow };
        await ApplyAsync(permission, request, null, cancellationToken);
        _permissions.Add(permission);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _audit.Record("CREATE", "permiso_acceso", permission.Id, $"rol={permission.RoleId};area={permission.AreaId}");
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return AccessPermissionResponse.From(permission);
    }

    public async Task<AccessPermissionResponse> UpdateAsync(long id, AccessPermissionRequest request, CancellationToken cancellationToken = default)
    {
        ModelValidator.Validate(request);
        var permission = await FindAsync(id, cancellationToken);
        await ApplyAsync(permission, request, id, cancellationToken);
        _audit.Record("UPDATE", "permiso_acceso", permission.Id);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return AccessPermissionResponse.From(permission);
    }

    public async Task<AccessPermissionResponse> SetStatusAsync(long id, StatusRequest request, CancellationToken cancellationToken = default)
    {
        ModelValidator.Validate(request);
        var permission = await FindAsync(id, cancellationToken);
        permission.IsActive = request.IsActive!.Value;
        _audit.Record(permission.IsActive ? "ACTIVATE" : "REVOKE", "permiso_acceso", permission.Id);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return AccessPermissionResponse.From(permission);
    }

    private async Task<AccessPermission> FindAsync(long id, CancellationToken cancellationToken) =>
        await _permissions.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Permiso de acceso", id);

    private async Task ApplyAsync(AccessPermission permission, AccessPermissionRequest request, long? excludeId, CancellationToken cancellationToken)
    {
        if (request.StartTime.HasValue && request.EndTime.HasValue && request.StartTime == request.EndTime)
        {
            throw new AppValidationException("endTime", "La hora final debe ser distinta de la hora inicial.");
        }

        if (!(request.Monday || request.Tuesday || request.Wednesday || request.Thursday || request.Friday || request.Saturday || request.Sunday))
        {
            throw new AppValidationException("days", "Debe habilitar al menos un día de la semana.");
        }

        var role = await _roles.GetByIdAsync(request.RoleId, cancellationToken) ?? throw new NotFoundException("Rol", request.RoleId);
        var area = await _areas.GetByIdAsync(request.AreaId, cancellationToken) ?? throw new NotFoundException("Área", request.AreaId);

        if (await _permissions.ExistsAsync(role.Id, area.Id, excludeId, cancellationToken))
        {
            throw new ConflictException("PERMISSION_DUPLICATE", "Ya existe un permiso para ese rol y área.");
        }

        permission.RoleId = role.Id;
        permission.Role = role;
        permission.AreaId = area.Id;
        permission.Area = area;
        permission.StartTime = request.StartTime;
        permission.EndTime = request.EndTime;
        permission.Monday = request.Monday;
        permission.Tuesday = request.Tuesday;
        permission.Wednesday = request.Wednesday;
        permission.Thursday = request.Thursday;
        permission.Friday = request.Friday;
        permission.Saturday = request.Saturday;
        permission.Sunday = request.Sunday;
        permission.IsActive = request.IsActive;
    }
}
