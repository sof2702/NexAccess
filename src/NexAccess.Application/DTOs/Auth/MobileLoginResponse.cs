using NexAccess.Application.DTOs.Users;

namespace NexAccess.Application.DTOs.Auth;

public sealed record MobileLoginResponse(
    string AccessToken,
    string TokenType,
    DateTime ExpiresAt,
    string RefreshToken,
    Guid SessionId,
    UserResponse User
);

