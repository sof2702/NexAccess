
namespace NexAccess.Domain.Entities;

public class MobileSession
{
    // Identificador único de la sesión
    public Guid Id { get; set; }

    // Usuario propietario
    public long UserId { get; set; }

    // Instalación de la aplicación Android
    public Guid InstallationId { get; set; }

    // SHA-256 del refresh token.
    // Nunca almacenar el token original.
    public string RefreshTokenHash { get; set; } = string.Empty;

    // Estado de la sesión
    public bool IsActive { get; set; } = true;

    // Fechas en UTC
    public DateTime CreatedAt { get; set; }

    public DateTime? LastUsedAt { get; set; }

    public DateTime? TokenRotatedAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    // Navegación EF Core
    public User? User { get; set; }

    public bool IsRevoked => !IsActive || RevokedAt.HasValue;

    public void Revoke(DateTime utcNow)
    {
        IsActive = false;
        RevokedAt ??= utcNow;
    }

    public void RotateToken(
        string newTokenHash,
        DateTime utcNow)
    {
        if (IsRevoked)
        {
            throw new InvalidOperationException(
                "No se puede renovar una sesión revocada.");
        }

        if (string.IsNullOrWhiteSpace(newTokenHash))
        {
            throw new ArgumentException(
                "El hash del token es obligatorio.",
                nameof(newTokenHash));
        }

        RefreshTokenHash = newTokenHash;
        TokenRotatedAt = utcNow;
        LastUsedAt = utcNow;
    }
}

