using NexAccess.Application.Interfaces;

namespace NexAccess.UnitTests.Support;

public class FakeClock : IClock
{
    public FakeClock(DateTime utcNow)
    {
        UtcNow = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
    }

    public DateTime UtcNow { get; set; }
}
