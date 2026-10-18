namespace NexAccess.Domain.Entities;

public class EmployeeRole
{
    public long EmployeeId { get; set; }
    public long RoleId { get; set; }
    public DateTime CreatedAt { get; set; }

    public Employee? Employee { get; set; }
    public Role? Role { get; set; }
}
