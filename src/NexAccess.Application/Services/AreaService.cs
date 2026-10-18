using NexAccess.Application.DTOs.Areas;
using NexAccess.Application.DTOs.Common;
using NexAccess.Application.Exceptions;
using NexAccess.Application.Interfaces;
using NexAccess.Application.Interfaces.Repositories;
using NexAccess.Application.Validators;
using NexAccess.Domain.Entities;

namespace NexAccess.Application.Services;

public class AreaService
{
    private readonly IAreaRepository _areas;
    private readonly IUnitOfWork _unitOfWork;
    private readonly AuditService _audit;
    private readonly IClock _clock;

    public AreaService(IAreaRepository areas, IUnitOfWork unitOfWork, AuditService audit, IClock clock)
    {
        _areas = areas;
        _unitOfWork = unitOfWork;
        _audit = audit;
        _clock = clock;
    }

    public async Task<PagedResult<AreaResponse>> SearchAsync(AreaQuery query, CancellationToken cancellationToken = default)
    {
        ModelValidator.Validate(query);
        var (items, total) = await _areas.SearchAsync(query, cancellationToken);
        return new PagedResult<AreaResponse>(items.Select(AreaResponse.From).ToList(), query.Page, query.PageSize, total);
    }

    public async Task<AreaResponse> GetAsync(long id, CancellationToken cancellationToken = default) =>
        AreaResponse.From(await FindAsync(id, cancellationToken));

    public async Task<AreaResponse> CreateAsync(AreaRequest request, CancellationToken cancellationToken = default)
    {
        ModelValidator.Validate(request);
        var area = new Area { CreatedAt = _clock.UtcNow };
        await ApplyAsync(area, request, null, cancellationToken);
        _areas.Add(area);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _audit.Record("CREATE", "area", area.Id, $"codigo={area.Code}");
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return AreaResponse.From(area);
    }

    public async Task<AreaResponse> UpdateAsync(long id, AreaRequest request, CancellationToken cancellationToken = default)
    {
        ModelValidator.Validate(request);
        var area = await FindAsync(id, cancellationToken);
        await ApplyAsync(area, request, id, cancellationToken);
        _audit.Record("UPDATE", "area", area.Id);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return AreaResponse.From(area);
    }

    private async Task<Area> FindAsync(long id, CancellationToken cancellationToken) =>
        await _areas.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Área", id);

    private async Task ApplyAsync(Area area, AreaRequest request, long? excludeId, CancellationToken cancellationToken)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        if (await _areas.CodeExistsAsync(code, excludeId, cancellationToken))
        {
            throw new ConflictException("AREA_CODE_DUPLICATE", "Ya existe un área con ese código.");
        }

        area.Code = code;
        area.Name = request.Name.Trim();
        area.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        area.IsRestricted = request.IsRestricted;
        area.IsActive = request.IsActive;
        area.UpdatedAt = _clock.UtcNow;
    }
}
