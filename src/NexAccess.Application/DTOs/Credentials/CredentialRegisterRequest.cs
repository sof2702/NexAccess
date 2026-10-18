using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using NexAccess.Domain.Enums;

namespace NexAccess.Application.DTOs.Credentials;

public class CredentialRegisterRequest
{
    [Range(1, long.MaxValue)]
    public long EmployeeId { get; set; }

    [Required, StringLength(200)]
    [JsonPropertyName("codigo_credencial")]
    public string CredentialCode { get; set; } = string.Empty;

    public CredentialType Type { get; set; } = CredentialType.Fisica;

    [StringLength(100)]
    public string? Name { get; set; }

    public DateTime? ExpiresAt { get; set; }
}
