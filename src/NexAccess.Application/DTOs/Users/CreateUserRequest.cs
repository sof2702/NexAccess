using System.ComponentModel.DataAnnotations;
using NexAccess.Domain.Enums;

namespace NexAccess.Application.DTOs.Users;

public class CreateUserRequest
{
    [Required]
    [RegularExpression(
        "^[A-Za-z0-9._-]{3,100}$",
        ErrorMessage = "El usuario debe tener entre 3 y 100 caracteres válidos.")]
    public string Username { get; set; } = string.Empty;

    [Required]
    [StringLength(200, MinimumLength = 10)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public SystemRole? Role { get; set; }

    [Range(1, long.MaxValue)]
    public long? EmployeeId { get; set; }

    // Nullable para mantener compatibilidad con clientes anteriores.
    public bool? WebAccessEnabled { get; set; }

    public bool? MobileAccessEnabled { get; set; }
}

