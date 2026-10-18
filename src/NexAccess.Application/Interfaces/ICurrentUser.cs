namespace NexAccess.Application.Interfaces;

public interface ICurrentUser
{
    long? UserId { get; }
    long? DeviceId { get; }
    string? IpAddress { get; }
}
