using System.ComponentModel.DataAnnotations;

namespace NexAccess.Application.DTOs.Common;

public class StatusRequest
{
    [Required]
    public bool? IsActive { get; set; }
}
