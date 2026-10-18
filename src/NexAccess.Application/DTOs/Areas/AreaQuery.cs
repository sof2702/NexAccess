using System.ComponentModel.DataAnnotations;
using NexAccess.Application.DTOs.Common;

namespace NexAccess.Application.DTOs.Areas;

public class AreaQuery : PageQuery
{
    [StringLength(100)]
    public string? Search { get; set; }

    public bool? IsActive { get; set; }
}
