using NexAccess.Application.DTOs.Devices;
using NexAccess.Application.Exceptions;
using NexAccess.Domain.Entities;
using NexAccess.Domain.Enums;
using NexAccess.Infrastructure.Security;
using NexAccess.UnitTests.Support;
using NSubstitute;

namespace NexAccess.UnitTests.Services;

public class DeviceServiceTests
{
    private readonly ServiceFactory _factory = new();
    private readonly DeviceKeyService _keys = new();

    [Fact]
    public async Task Create_returns_plain_key_once_and_stores_only_hash()
    {
        var area = TestData.Area();
        _factory.Areas.GetByIdAsync(area.Id, Arg.Any<CancellationToken>()).Returns(area);
        Device? stored = null;
        _factory.Devices.When(d => d.Add(Arg.Any<Device>())).Do(call => stored = call.Arg<Device>());

        var response = await _factory.DeviceService(_keys).CreateAsync(new DeviceCreateRequest { Code = "PUERTA-RRHH-01", Name = "Puerta", Type = DeviceType.Acceso, AreaId = area.Id });

        Assert.StartsWith("nxd_", response.DeviceKey);
        Assert.NotNull(stored);
        Assert.NotEqual(response.DeviceKey, stored!.KeyHash);
        Assert.Equal(_keys.Hash(response.DeviceKey), stored.KeyHash);
    }

    [Fact]
    public async Task Access_device_requires_area()
    {
        var error = await Assert.ThrowsAsync<AppValidationException>(() =>
            _factory.DeviceService(_keys).CreateAsync(new DeviceCreateRequest { Code = "PUERTA-01", Name = "Puerta", Type = DeviceType.Acceso }));

        Assert.Contains("areaId", error.Errors.Keys);
    }

    [Fact]
    public async Task Duplicate_device_code_is_conflict()
    {
        _factory.Devices.CodeExistsAsync("MARCADOR-01", Arg.Any<CancellationToken>()).Returns(true);

        await Assert.ThrowsAsync<ConflictException>(() =>
            _factory.DeviceService(_keys).CreateAsync(new DeviceCreateRequest { Code = "marcador-01", Name = "Marcador", Type = DeviceType.Marcacion }));
    }

    [Fact]
    public async Task Authenticate_accepts_valid_key_and_updates_last_connection()
    {
        var key = _keys.GenerateKey();
        var device = TestData.Device();
        device.KeyHash = _keys.Hash(key);
        _factory.Devices.GetByKeyHashAsync(device.KeyHash, Arg.Any<CancellationToken>()).Returns(device);

        var authenticated = await _factory.DeviceService(_keys).AuthenticateAsync(key);

        Assert.Equal(new AuthenticatedDevice(device.Id, device.Code), authenticated);
        Assert.Equal(TestData.MondayNoonUtc, device.LastConnectionAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("nxd_desconocida")]
    public async Task Authenticate_rejects_missing_or_unknown_key(string? key)
    {
        Assert.Null(await _factory.DeviceService(_keys).AuthenticateAsync(key));
    }

    [Fact]
    public async Task Authenticate_rejects_inactive_device()
    {
        var key = _keys.GenerateKey();
        var device = TestData.Device();
        device.IsActive = false;
        device.KeyHash = _keys.Hash(key);
        _factory.Devices.GetByKeyHashAsync(device.KeyHash, Arg.Any<CancellationToken>()).Returns(device);

        Assert.Null(await _factory.DeviceService(_keys).AuthenticateAsync(key));
    }

    [Fact]
    public async Task Rotate_key_replaces_hash()
    {
        var device = TestData.Device();
        var oldHash = device.KeyHash;
        _factory.Devices.GetByIdAsync(device.Id, Arg.Any<CancellationToken>()).Returns(device);

        var response = await _factory.DeviceService(_keys).RotateKeyAsync(device.Id);

        Assert.NotEqual(oldHash, device.KeyHash);
        Assert.Equal(_keys.Hash(response.DeviceKey), device.KeyHash);
        Assert.Equal(TestData.MondayNoonUtc, device.KeyRotatedAt);
    }
}
