using NexAccess.Application.DTOs.Audit;
using NexAccess.Application.DTOs.Common;
using NexAccess.Application.Interfaces;
using NexAccess.Application.Interfaces.Repositories;
using NexAccess.Application.Validators;
using NexAccess.Domain.Entities;

namespace NexAccess.Application.Services;

public class AuditService
{
    private const int MaxDetailLength = 1000;

    private readonly IAuditRepository _audit;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public AuditService(IAuditRepository audit, ICurrentUser currentUser, IClock clock)
    {
        _audit = audit;
        _currentUser = currentUser;
        _clock = clock;
    }

    public void Record(string action, string entity, long? entityId, string? detail = null, long? userId = null)
    {
        _audit.Add(new AuditEntry
        {
            UserId = userId ?? _currentUser.UserId,
            DeviceId = _currentUser.DeviceId,
            Action = action,
            Entity = entity,
            EntityId = entityId,
            Detail = detail is { Length: > MaxDetailLength } ? detail[..MaxDetailLength] : detail,
            IpAddress = _currentUser.IpAddress,
            OccurredAt = _clock.UtcNow
        });
    }

    public async Task<PagedResult<AuditResponse>> SearchAsync(AuditQuery query, CancellationToken cancellationToken = default)
    {
        ModelValidator.Validate(query);
        query.From = DateRange.ToUtc(query.From);
        query.To = DateRange.ToUtc(query.To);
        DateRange.Ensure(query.From, query.To);
        var (items, total) = await _audit.SearchAsync(query, cancellationToken);
        return new PagedResult<AuditResponse>(items.Select(AuditResponse.From).ToList(), query.Page, query.PageSize, total);
    }
}
