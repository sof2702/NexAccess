
using System.ComponentModel.DataAnnotations;

namespace NexAccess.Application.DTOs.Empresa;

public class EmpresaRequest
{
    [Required]
    [StringLength(200)]
    public string RazonSocial { get; set; } = string.Empty;

    [StringLength(200)]
    public string? NombreFantasia { get; set; }

    [StringLength(20)]
    public string? Ruc { get; set; }

    [StringLength(300)]
    public string? Direccion { get; set; }

    [StringLength(50)]
    public string? Telefono { get; set; }

    [EmailAddress]
    [StringLength(150)]
    public string? Email { get; set; }
}

