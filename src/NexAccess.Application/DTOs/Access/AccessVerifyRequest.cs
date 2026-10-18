using System.ComponentModel.DataAnnotations;

namespace NexAccess.Application.DTOs.Access;

public class AccessVerifyRequest
{
    [Required, StringLength(50)]
    public string DeviceCode { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string Credential { get; set; } = string.Empty;
}
