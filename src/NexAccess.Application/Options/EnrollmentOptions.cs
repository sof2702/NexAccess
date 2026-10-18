namespace NexAccess.Application.Options;

public class EnrollmentOptions
{
    public const string SectionName = "Enrollment";

    public int DefaultExpirationMinutes { get; set; } = 5;
    public int MaxExpirationMinutes { get; set; } = 60;
}
