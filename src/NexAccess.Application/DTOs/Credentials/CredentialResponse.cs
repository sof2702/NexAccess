using System.Text.Json.Serialization;
using NexAccess.Domain.Entities;
using NexAccess.Domain.Enums;

namespace NexAccess.Application.DTOs.Credentials;

public sealed record CredentialResponse(
    long Id,
    long EmployeeId,
    string? EmployeeName,
    [property: JsonPropertyName("codigo_credencial")] string CredentialCode,
    CredentialType Type,
    string? Name,
    bool IsActive,
    string Status,
    DateTime IssuedAt,
    DateTime? ExpiresAt,
    DateTime? RevokedAt,
    string? RevocationReason)
{
    public static CredentialResponse From(Credential credential, DateTime utcNow) => new(
        credential.Id,
        credential.EmployeeId,
        credential.Employee?.FullName,
        credential.CredentialCode,
        credential.Type,
        credential.Name,
        credential.IsActive,
        StatusOf(credential, utcNow),
        credential.IssuedAt,
        credential.ExpiresAt,
        credential.RevokedAt,
        credential.RevocationReason);

    public static string StatusOf(Credential credential, DateTime utcNow)
    {
        if (credential.IsRevoked)
        {
            return "REVOCADA";
        }

        if (credential.IsExpiredAt(utcNow))
        {
            return "VENCIDA";
        }

        return credential.IsActive ? "ACTIVA" : "INACTIVA";
    }
}
