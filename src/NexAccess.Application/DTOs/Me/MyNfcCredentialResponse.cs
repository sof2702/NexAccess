
namespace NexAccess.Application.DTOs.Me;

public sealed record MyNfcCredentialResponse(
    long CredentialId,
    string Credential,
    DateTime? ExpiresAt
);

