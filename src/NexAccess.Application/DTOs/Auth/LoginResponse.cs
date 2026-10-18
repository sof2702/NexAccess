using NexAccess.Application.DTOs.Users;

namespace NexAccess.Application.DTOs.Auth;

public sealed record LoginResponse(string AccessToken, string TokenType, DateTime ExpiresAt, UserResponse User);
