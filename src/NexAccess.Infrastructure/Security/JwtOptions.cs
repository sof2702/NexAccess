namespace NexAccess.Infrastructure.Security;

public class JwtOptions
{
    public const string SectionName = "Jwt";
    public const int MinimumKeyBytes = 32;

    public string Issuer { get; set; } = "NexAccess";
    public string Audience { get; set; } = "NexAccess.Admin";
    public string Key { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; } = 60;
}
