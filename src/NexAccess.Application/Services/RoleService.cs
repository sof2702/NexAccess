using NexAccess.Application.DTOs.Roles;
using NexAccess.Application.Exceptions;
using NexAccess.Application.Interfaces;
using NexAccess.Application.Interfaces.Repositories;
using NexAccess.Application.Validators;
using NexAccess.Domain.Entities;

namespace NexAccess.Application.Services;

public class RoleService
{
    private readonly IRoleRepository _roles;
    private readonly IUnitOfWork _unitOfWork;
    private readonly AuditService _audit;
    private readonly IClock _clock;

    public RoleService(IRoleRepository roles, IUnitOfWork unitOfWork, AuditService audit, IClock clock)
    {
        _roles = roles;
        _unitOfWork = unitOfWork;
        _audit = audit;
        _clock = clock;
    }

    public async Task<IReadOnlyList<RoleResponse>> ListAsync(bool? isActive, CancellationToken cancellationToken = default) =>
        (await _roles.ListAsync(isActive, cancellationToken)).Select(RoleResponse.From).ToList();

    public async Task<RoleResponse> GetAsync(long id, CancellationToken cancellationToken = default) =>
        RoleResponse.From(await FindAsync(id, cancellationToken));

    public async Task<RoleResponse> CreateAsync(RoleRequest request, CancellationToken cancellationToken = default)
    {
        ModelValidator.Validate(request);
        var role = new Role { CreatedAt = _clock.UtcNow };
        await ApplyAsync(role, request, null, cancellationToken);
        _roles.Add(role);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _audit.Record("CREATE", "rol", role.Id, $"codigo={role.Code}");
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return RoleResponse.From(role);
    }

    public async Task<RoleResponse> UpdateAsync(long id, RoleRequest request, CancellationToken cancellationToken = default)
    {
        ModelValidator.Validate(request);
        var role = await FindAsync(id, cancellationToken);
        await ApplyAsync(role, request, id, cancellationToken);
        _audit.Record("UPDATE", "rol", role.Id);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return RoleResponse.From(role);
    }

    private async Task<Role> FindAsync(long id, CancellationToken cancellationToken) =>
        await _roles.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Rol", id);

    private async Task ApplyAsync(Role role, RoleRequest request, long? excludeId, CancellationToken cancellationToken)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        if (await _roles.CodeExistsAsync(code, excludeId, cancellationToken))
        {
            throw new ConflictException("ROLE_CODE_DUPLICATE", "Ya existe un rol con ese código.");
        }

        role.Code = code;
        role.Name = request.Name.Trim();
        role.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        role.IsActive = request.IsActive;
    }
}
