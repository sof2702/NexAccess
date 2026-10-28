
using NexAccess.Domain.Entities;

namespace NexAccess.Application.DTOs.Empresa;

public sealed record EmpresaResponse(
    long Id,
    string RazonSocial,
    string? NombreFantasia,
    string? Ruc,
    string? Direccion,
    string? Telefono,
    string? Email,
    bool Activo,
    bool TieneLogo
)
{
    public static EmpresaResponse From(NexAccess.Domain.Entities.Empresa empresa) => new(
        empresa.Id,
        empresa.RazonSocial,
        empresa.NombreFantasia,
        empresa.Ruc,
        empresa.Direccion,
        empresa.Telefono,
        empresa.Email,
        empresa.Activo,
        !string.IsNullOrWhiteSpace(empresa.LogoPath)
    );
}

