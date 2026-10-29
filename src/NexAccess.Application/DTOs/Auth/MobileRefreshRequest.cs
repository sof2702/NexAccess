
using System.ComponentModel.DataAnnotations;

namespace NexAccess.Application.DTOs.Auth;

public sealed class MobileRefreshRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}

