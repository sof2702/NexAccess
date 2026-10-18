using System.Text.Json.Serialization;

namespace NexAccess.Application.DTOs.Devices;

public sealed record DeviceCommandResponse(
    string Command,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] long? EnrollmentId = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] DateTime? ExpiresAt = null)
{
    public const string None = "NONE";

    public static DeviceCommandResponse Empty { get; } = new(None);
}
