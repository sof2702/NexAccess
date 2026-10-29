
using NexAccess.Application.DTOs.Credentials;
using NexAccess.Application.Validators;
using NexAccess.Domain.Entities;
using NexAccess.Domain.Enums;

namespace NexAccess.Application.DTOs.Me;

public sealed record MyCredentialResponse(
    long Id,
    string? Name,
    CredentialType Type,
    string MaskedCode,
    string Status,
    DateTime IssuedAt,
    DateTime? ExpiresAt
)
{
    public static MyCredentialResponse From(
        Credential credential,
        DateTime utcNow) => new(
            credential.Id,
            credential.Name,
            credential.Type,
            CredentialCode.Mask(credential.CredentialCode),
            CredentialResponse.StatusOf(credential, utcNow),
            credential.IssuedAt,
            credential.ExpiresAt
        );
}

