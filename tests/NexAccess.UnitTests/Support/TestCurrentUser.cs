using NexAccess.Application.Interfaces;

namespace NexAccess.UnitTests.Support;

public class TestCurrentUser : ICurrentUser
{
    public long? UserId { get; set; } = 1;
    public long? DeviceId { get; set; }
    public string? IpAddress { get; set; } = "127.0.0.1";
    public Guid? SessionId { get; set; }
}
