
namespace NexAccess.Domain.Entities;

public class Empresa
{
    public long Id { get; set; }

    public string RazonSocial { get; set; } = string.Empty;
    public string? NombreFantasia { get; set; }
    public string? Ruc { get; set; }

    public string? Direccion { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }

    public string? LogoPath { get; set; }

    public bool Activo { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

