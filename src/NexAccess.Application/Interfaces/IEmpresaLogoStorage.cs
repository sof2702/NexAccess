namespace NexAccess.Application.Interfaces;

public interface IEmpresaLogoStorage
{
    Task<string> SaveAsync(
        long empresaId,
        Stream input,
        CancellationToken cancellationToken = default);
}
