using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace NexAccess.Application.DTOs.Enrollments;

public class EnrollmentCardRequest
{
    [Required, StringLength(200)]
    [JsonPropertyName("codigo_credencial")]
    public string CredentialCode { get; set; } = string.Empty;
}
