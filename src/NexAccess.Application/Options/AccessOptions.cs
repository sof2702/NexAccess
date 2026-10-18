namespace NexAccess.Application.Options;

public class AccessOptions
{
    public const string SectionName = "Access";

    public int OpenDurationMs { get; set; } = 3000;
    public string TimeZoneId { get; set; } = "America/Asuncion";
}
