namespace NexAccess.Application.DTOs.Devices;

public sealed record DeviceCommandResponse(string Command, long? EnrollmentId = null, DateTime? ExpiresAt = null)
{
    public const string None = "NONE";

    public static DeviceCommandResponse Empty { get; } = new(None);
}
