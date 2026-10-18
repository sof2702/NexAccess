namespace NexAccess.Domain.Entities;

public class Employee
{
    public long Id { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<EmployeeRole> Roles { get; set; } = new List<EmployeeRole>();
    public ICollection<Credential> Credentials { get; set; } = new List<Credential>();

    public string FullName => $"{FirstName} {LastName}".Trim();
}
