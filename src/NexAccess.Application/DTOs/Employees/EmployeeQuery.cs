using System.ComponentModel.DataAnnotations;
using NexAccess.Application.DTOs.Common;

namespace NexAccess.Application.DTOs.Employees;

public class EmployeeQuery : PageQuery
{
    [StringLength(100)]
    public string? Search { get; set; }

    public bool? IsActive { get; set; }
}
