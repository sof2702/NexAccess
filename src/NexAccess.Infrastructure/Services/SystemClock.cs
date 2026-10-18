using NexAccess.Application.Interfaces;

namespace NexAccess.Infrastructure.Services;

public class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
