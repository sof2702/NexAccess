using System.ComponentModel.DataAnnotations;

namespace NexAccess.Application.DTOs.Users;

public class ChangePasswordRequest
{
    [Required, StringLength(200, MinimumLength = 10, ErrorMessage = "La contraseña debe tener entre 10 y 200 caracteres.")]
    public string NewPassword { get; set; } = string.Empty;
}
