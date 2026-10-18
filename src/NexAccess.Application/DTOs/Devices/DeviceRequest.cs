using System.ComponentModel.DataAnnotations;
using NexAccess.Domain.Enums;

namespace NexAccess.Application.DTOs.Devices;

public class DeviceCreateRequest : DeviceUpdateRequest
{
    [Required, RegularExpression("^[A-Z0-9_-]{2,50}$", ErrorMessage = "El código debe tener entre 2 y 50 caracteres en mayúsculas, números, guion o guion bajo.")]
    public string Code { get; set; } = string.Empty;
}

public class DeviceUpdateRequest
{
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public DeviceType? Type { get; set; }

    [Range(1, long.MaxValue)]
    public long? AreaId { get; set; }

    [StringLength(255)]
    public string? Description { get; set; }
}
