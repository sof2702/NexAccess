namespace NexAccess.Application.Interfaces;

public interface IEmpresaLogoStorage
{
    Task<string> SaveAsync(
        long empresaId,
        Stream input,
        CancellationToken cancellationToken = default);

Task<(byte[] Content, string ContentType)> ReadAsync(
    string logoPath,
    CancellationToken cancellationToken = default);
}
