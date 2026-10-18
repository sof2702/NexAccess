using System.ComponentModel.DataAnnotations;

namespace NexAccess.Application.DTOs.Roles;

public class RoleRequest
{
    [Required, RegularExpression("^[A-Z0-9_]{2,50}$", ErrorMessage = "El código debe tener entre 2 y 50 caracteres en mayúsculas, números o guion bajo.")]
    public string Code { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(255)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}
