using System.ComponentModel.DataAnnotations;

namespace NexAccess.Application.DTOs.Auth;

public sealed class MobileLoginRequest
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    [Required]
    public Guid InstallationId { get; set; }
}
