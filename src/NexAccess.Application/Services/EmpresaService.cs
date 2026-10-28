using NexAccess.Application.DTOs.Empresa;
using NexAccess.Application.Interfaces;
using NexAccess.Application.Interfaces.Repositories;
using NexAccess.Application.Services;
using NexAccess.Domain.Entities;
using NexAccess.Application.Exceptions;

namespace NexAccess.Application.Services;

public class EmpresaService
{
    private readonly IEmpresaRepository _empresas;
    private readonly IUnitOfWork _unitOfWork;
    private readonly AuditService _audit;
    private readonly IClock _clock;
    private readonly IEmpresaLogoStorage _logoStorage;

    public EmpresaService(
        IEmpresaRepository empresas,
        IUnitOfWork unitOfWork,
        AuditService audit,
        IClock clock,
        IEmpresaLogoStorage logoStorage)
    {
        _empresas = empresas;
        _unitOfWork = unitOfWork;
        _audit = audit;
        _clock = clock;
        _logoStorage = logoStorage;
    }

    public async Task<EmpresaResponse?> GetAsync(
        CancellationToken cancellationToken = default)
    {
        var empresa = await _empresas.GetAsync(cancellationToken);

        return empresa is null
            ? null
            : EmpresaResponse.From(empresa);
    }

    public async Task<EmpresaResponse> SaveAsync(
        EmpresaRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.RazonSocial))
        {
            throw new ArgumentException(
                "La razón social es obligatoria.");
        }

        var empresa = await _empresas.GetAsync(cancellationToken);
        var isNew = empresa is null;
        var now = _clock.UtcNow;

        if (isNew)
        {
            empresa = new Empresa
            {
                CreatedAt = now
            };

            _empresas.Add(empresa);
        }

        if (empresa is null)
        {
            throw new InvalidOperationException(
            "No se pudo inicializar la empresa.");
        }

        empresa.RazonSocial = request.RazonSocial.Trim();
        empresa.NombreFantasia = request.NombreFantasia?.Trim();
        empresa.Ruc = request.Ruc?.Trim();
        empresa.Direccion = request.Direccion?.Trim();
        empresa.Telefono = request.Telefono?.Trim();
        empresa.Email = request.Email?.Trim();
        empresa.UpdatedAt = now;

        // Primero persistimos para obtener el ID si es nueva.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _audit.Record(
            isNew ? "CREATE" : "UPDATE",
            "empresa",
            empresa.Id);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return EmpresaResponse.From(empresa);
    }

public async Task<EmpresaResponse> SaveLogoAsync(
    Stream logo,
    CancellationToken cancellationToken = default)
{
    ArgumentNullException.ThrowIfNull(logo);

    var empresa = await _empresas.GetAsync(cancellationToken);

    if (empresa is null)
    {
        throw new AppValidationException(
            "empresa",
            "Primero debe configurar los datos de la empresa.");
    }

    var logoPath = await _logoStorage.SaveAsync(
        empresa.Id,
        logo,
        cancellationToken);

    empresa.LogoPath = logoPath;
    empresa.UpdatedAt = _clock.UtcNow;

    _audit.Record(
        "UPDATE_LOGO",
        "empresa",
        empresa.Id);

    await _unitOfWork.SaveChangesAsync(cancellationToken);

    return EmpresaResponse.From(empresa);
}

public async Task<(byte[] Content, string ContentType)> GetLogoAsync(
    CancellationToken cancellationToken = default)
{
    var empresa = await _empresas.GetAsync(cancellationToken);

    if (empresa is null || string.IsNullOrWhiteSpace(empresa.LogoPath))
    {
        throw new NotFoundException("Logo de empresa", 0);
    }

    return await _logoStorage.ReadAsync(
        empresa.LogoPath,
        cancellationToken);
}

}

