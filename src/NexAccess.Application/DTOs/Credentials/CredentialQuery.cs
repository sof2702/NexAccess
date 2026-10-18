using NexAccess.Application.DTOs.Common;

namespace NexAccess.Application.DTOs.Credentials;

public class CredentialQuery : PageQuery
{
    public long? EmployeeId { get; set; }
    public bool? IsActive { get; set; }
}
