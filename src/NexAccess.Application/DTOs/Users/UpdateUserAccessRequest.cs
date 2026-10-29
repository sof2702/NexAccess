using System.ComponentModel.DataAnnotations;

namespace NexAccess.Application.DTOs.Users;

public sealed class UpdateUserAccessRequest
{
    [Required]
    public bool? WebAccessEnabled { get; set; }

    [Required]
    public bool? MobileAccessEnabled { get; set; }
}

