using NexAccess.Application.DTOs.AccessEvents;
using NexAccess.Application.DTOs.Common;
using NexAccess.Application.DTOs.Devices;
using NexAccess.Application.Exceptions;
using NexAccess.Application.Interfaces;
using NexAccess.Application.Interfaces.Repositories;
using NexAccess.Application.Validators;
using NexAccess.Domain.Entities;
using NexAccess.Domain.Enums;

namespace NexAccess.Application.Services;

public class DeviceService
{
    private const int MaxKeyLength = 256;
    private static readonly TimeSpan ConnectionRefreshInterval = TimeSpan.FromSeconds(30);

    private readonly IDeviceRepository _devices;
    private readonly IAreaRepository _areas;
    private readonly IAccessEventRepository _events;
    private readonly IDeviceKeyService _keys;
    private readonly IUnitOfWork _unitOfWork;
    private readonly AuditService _audit;
    private readonly IClock _clock;

    public DeviceService(
        IDeviceRepository devices,
        IAreaRepository areas,
        IAccessEventRepository events,
        IDeviceKeyService keys,
        IUnitOfWork unitOfWork,
        AuditService audit,
        IClock clock)
    {
        _devices = devices;
        _areas = areas;
        _events = events;
        _keys = keys;
        _unitOfWork = unitOfWork;
        _audit = audit;
        _clock = clock;
    }

    public async Task<PagedResult<DeviceResponse>> SearchAsync(DeviceQuery query, CancellationToken cancellationToken = default)
    {
        ModelValidator.Validate(query);
        var (items, total) = await _devices.SearchAsync(query, cancellationToken);
        return new PagedResult<DeviceResponse>(items.Select(DeviceResponse.From).ToList(), query.Page, query.PageSize, total);
    }

    public async Task<DeviceResponse> GetAsync(long id, CancellationToken cancellationToken = default) =>
        DeviceResponse.From(await FindAsync(id, cancellationToken));

    public async Task<DeviceKeyResponse> CreateAsync(DeviceCreateRequest request, CancellationToken cancellationToken = default)
    {
        ModelValidator.Validate(request);
        var code = request.Code.Trim().ToUpperInvariant();
        if (await _devices.CodeExistsAsync(code, cancellationToken))
        {
            throw new ConflictException("DEVICE_CODE_DUPLICATE", "Ya existe un dispositivo con ese código.");
        }

        var now = _clock.UtcNow;
        var key = _keys.GenerateKey();
        var device = new Device { Code = code, IsActive = true, KeyHash = _keys.Hash(key), KeyRotatedAt = now, CreatedAt = now };
        await ApplyAsync(device, request, cancellationToken);
        _devices.Add(device);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _audit.Record("CREATE", "dispositivo", device.Id, $"codigo={device.Code}");
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new DeviceKeyResponse(DeviceResponse.From(device), key);
    }

    public async Task<DeviceResponse> UpdateAsync(long id, DeviceUpdateRequest request, CancellationToken cancellationToken = default)
    {
        ModelValidator.Validate(request);
        var device = await FindAsync(id, cancellationToken);
        await ApplyAsync(device, request, cancellationToken);
        _audit.Record("UPDATE", "dispositivo", device.Id);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return DeviceResponse.From(device);
    }

    public async Task<DeviceResponse> SetStatusAsync(long id, StatusRequest request, CancellationToken cancellationToken = default)
    {
        ModelValidator.Validate(request);
        var device = await FindAsync(id, cancellationToken);
        device.IsActive = request.IsActive!.Value;
        device.UpdatedAt = _clock.UtcNow;
        _audit.Record(device.IsActive ? "ACTIVATE" : "DEACTIVATE", "dispositivo", device.Id);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return DeviceResponse.From(device);
    }

    public async Task<DeviceKeyResponse> RotateKeyAsync(long id, CancellationToken cancellationToken = default)
    {
        var device = await FindAsync(id, cancellationToken);
        var key = _keys.GenerateKey();
        var now = _clock.UtcNow;
        device.KeyHash = _keys.Hash(key);
        device.KeyRotatedAt = now;
        device.UpdatedAt = now;
        _audit.Record("ROTATE_KEY", "dispositivo", device.Id);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new DeviceKeyResponse(DeviceResponse.From(device), key);
    }

    public async Task<PagedResult<AccessEventResponse>> GetEventsAsync(long id, AccessEventQuery query, CancellationToken cancellationToken = default)
    {
        await FindAsync(id, cancellationToken);
        ModelValidator.Validate(query);
        query.DeviceId = id;
        query.From = DateRange.ToUtc(query.From);
        query.To = DateRange.ToUtc(query.To);
        DateRange.Ensure(query.From, query.To);
        var (items, total) = await _events.SearchAsync(query, cancellationToken);
        return new PagedResult<AccessEventResponse>(items.Select(AccessEventResponse.From).ToList(), query.Page, query.PageSize, total);
    }

    public async Task<AuthenticatedDevice?> AuthenticateAsync(string? deviceKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deviceKey) || deviceKey.Length > MaxKeyLength)
        {
            return null;
        }

        var device = await _devices.GetByKeyHashAsync(_keys.Hash(deviceKey.Trim()), cancellationToken);
        if (device is null || !device.IsActive)
        {
            return null;
        }

        var now = _clock.UtcNow;
        if (device.LastConnectionAt is null || now - device.LastConnectionAt.Value >= ConnectionRefreshInterval)
        {
            device.LastConnectionAt = now;
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return new AuthenticatedDevice(device.Id, device.Code);
    }

    private async Task<Device> FindAsync(long id, CancellationToken cancellationToken) =>
        await _devices.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Dispositivo", id);

    private async Task ApplyAsync(Device device, DeviceUpdateRequest request, CancellationToken cancellationToken)
    {
        var type = request.Type!.Value;
        Area? area = null;
        if (request.AreaId.HasValue)
        {
            area = await _areas.GetByIdAsync(request.AreaId.Value, cancellationToken) ?? throw new NotFoundException("Área", request.AreaId.Value);
        }

        if (area is null && type is DeviceType.Acceso or DeviceType.Mixto)
        {
            throw new AppValidationException("areaId", "Los dispositivos de acceso o mixtos deben estar asociados a un área.");
        }

        device.Name = request.Name.Trim();
        device.Type = type;
        device.AreaId = area?.Id;
        device.Area = area;
        device.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        device.UpdatedAt = _clock.UtcNow;
    }
}
