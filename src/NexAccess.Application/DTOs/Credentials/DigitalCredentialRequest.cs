
using System.ComponentModel.DataAnnotations;

namespace NexAccess.Application.DTOs.Credentials;

public sealed class DigitalCredentialRequest
{
    [StringLength(100)]
    public string? Name { get; set; }

    public DateTime? ExpiresAt { get; set; }
}

