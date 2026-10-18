using System.ComponentModel.DataAnnotations;

namespace NexAccess.Application.DTOs.Credentials;

public class RevokeCredentialRequest
{
    [Required, StringLength(255, MinimumLength = 3)]
    public string Reason { get; set; } = string.Empty;
}
