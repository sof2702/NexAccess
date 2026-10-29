
using NexAccess.Application.Validators;
using NexAccess.Domain.Entities;

namespace NexAccess.Application.DTOs.Credentials;

public sealed record DigitalCredentialResponse(
    long Id,
    long EmployeeId,
    string? Name,
    string Type,
    string MaskedCode,
    string Status,
    DateTime IssuedAt,
    DateTime? ExpiresAt
)
{
    public static DigitalCredentialResponse From(
        Credential credential,
        DateTime utcNow) => new(
            credential.Id,
            credential.EmployeeId,
            credential.Name,
            "DIGITAL",
            CredentialCode.Mask(credential.CredentialCode),
            CredentialResponse.StatusOf(credential, utcNow),
            credential.IssuedAt,
            credential.ExpiresAt
        );
}

