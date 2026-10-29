using NexAccess.Application.Interfaces;

namespace NexAccess.API.Configuration;

public class HttpCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public HttpCurrentUser(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public long? UserId => _accessor.HttpContext?.User.FindUserId();

    public long? DeviceId => _accessor.HttpContext?.User.FindDeviceId();

    public string? IpAddress => _accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

public Guid? SessionId
{
    get
    {
        var sid = _accessor.HttpContext?.User.FindFirst("sid")?.Value;

        return Guid.TryParse(sid, out var sessionId)
            ? sessionId
            : null;
    }
}

}
