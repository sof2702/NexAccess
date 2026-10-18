using System.ComponentModel.DataAnnotations;
using NexAccess.Application.DTOs.Common;

namespace NexAccess.Application.DTOs.Audit;

public class AuditQuery : PageQuery
{
    public long? UserId { get; set; }

    [StringLength(60)]
    public string? Entity { get; set; }

    [StringLength(60)]
    public string? Action { get; set; }

    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}
