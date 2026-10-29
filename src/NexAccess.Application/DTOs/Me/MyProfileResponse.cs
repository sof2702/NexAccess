
namespace NexAccess.Application.DTOs.Me;

public sealed record MyProfileResponse(
    long UserId,
    string Username,
    string Role,
    long EmployeeId,
    string FullName,
    bool IsActive
);

