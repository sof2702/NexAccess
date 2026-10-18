using System.ComponentModel.DataAnnotations;

namespace NexAccess.Application.DTOs.Employees;

public class EmployeeRequest
{
    [Required, StringLength(20, MinimumLength = 3)]
    public string DocumentNumber { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [EmailAddress, StringLength(150)]
    public string? Email { get; set; }

    [StringLength(30)]
    public string? Phone { get; set; }
}
