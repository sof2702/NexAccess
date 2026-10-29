using NexAccess.Domain.Entities;
using NexAccess.Domain.Enums;

namespace NexAccess.Application.DTOs.Users;

public sealed record UserResponse(
    long Id,
    string Username,
    SystemRole Role,
    long? EmployeeId,
    bool IsActive,
    DateTime? LastLoginAt,
    bool WebAccessEnabled,
    bool MobileAccessEnabled)
{
    public static UserResponse From(User user) => new(
        user.Id,
        user.Username,
        user.SystemRole,
        user.EmployeeId,
        user.IsActive,
        user.LastLoginAt,
        user.WebAccessEnabled,
        user.MobileAccessEnabled
    );
}

