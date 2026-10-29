
using System.ComponentModel.DataAnnotations;

namespace NexAccess.Application.DTOs.Auth;

public sealed class MobileLogoutRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}

