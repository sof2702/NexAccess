namespace NexAccess.Domain.Entities;

public class UsedMobileRefreshToken
{
    public long Id { get; set; }

    public Guid SessionId { get; set; }

    // Hash SHA-256, nunca el token original.
    public string TokenHash { get; set; } = string.Empty;

    public DateTime UsedAt { get; set; }

    public MobileSession? Session { get; set; }
}

