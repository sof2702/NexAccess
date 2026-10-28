using System.Security.Cryptography;
using NexAccess.Application.Interfaces;

namespace NexAccess.Infrastructure.Services;

public class EmpresaLogoStorage : IEmpresaLogoStorage
{
    private const long MaxFileSize = 2 * 1024 * 1024;
    private readonly string _storageRoot;

    public EmpresaLogoStorage()
    {
        _storageRoot = Path.Combine(
            Environment.GetEnvironmentVariable("NEXACCESS_STORAGE_PATH")
                ?? "/app/storage",
            "empresas");
    }

    public async Task<string> SaveAsync(
        long empresaId,
        Stream input,
        CancellationToken cancellationToken = default)
    {
        if (empresaId <= 0)
            throw new ArgumentException("ID de empresa inválido.");

        using var buffer = new MemoryStream();
        var chunk = new byte[81920];

        int read;
        while ((read = await input.ReadAsync(
            chunk.AsMemory(), cancellationToken)) > 0)
        {
            if (buffer.Length + read > MaxFileSize)
                throw new ArgumentException(
                    "El logotipo no puede superar los 2 MB.");

            buffer.Write(chunk, 0, read);
        }

        var data = buffer.ToArray();

        var isPng = data.Length >= 8 &&
            data.AsSpan(0, 8).SequenceEqual(
                new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });

        var isJpeg = data.Length >= 4 &&
            data[0] == 0xFF &&
            data[1] == 0xD8 &&
            data[^2] == 0xFF &&
            data[^1] == 0xD9;

        if (!isPng && !isJpeg)
            throw new ArgumentException(
                "Solo se permiten imágenes PNG o JPEG.");

        var extension = isPng ? ".png" : ".jpg";

        var directory = Path.Combine(
            _storageRoot, empresaId.ToString());

        Directory.CreateDirectory(directory);

        var fileName = Convert.ToHexString(
            RandomNumberGenerator.GetBytes(16)) + extension;

        var fullPath = Path.Combine(directory, fileName);

        await File.WriteAllBytesAsync(
            fullPath, data, cancellationToken);

        return $"empresas/{empresaId}/{fileName}";
    }

public async Task<(byte[] Content, string ContentType)> ReadAsync(
    string logoPath,
    CancellationToken cancellationToken = default)
{
    if (string.IsNullOrWhiteSpace(logoPath))
        throw new ArgumentException("Ruta de logotipo inválida.");

    var root = Path.GetFullPath(_storageRoot);
    var fullPath = Path.GetFullPath(
        Path.Combine(
            Directory.GetParent(root)!.FullName,
            logoPath));

    if (!fullPath.StartsWith(
        root + Path.DirectorySeparatorChar,
        StringComparison.Ordinal))
    {
        throw new UnauthorizedAccessException(
            "La ruta del logotipo no está permitida.");
    }

    var content = await File.ReadAllBytesAsync(
        fullPath, cancellationToken);

    var contentType = Path.GetExtension(fullPath)
        .ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" => "image/jpeg",
            _ => throw new InvalidOperationException(
                "Formato de imagen no permitido.")
        };

    return (content, contentType);

}



public Task DeleteAsync(
    string logoPath,
    CancellationToken cancellationToken = default)
{
    cancellationToken.ThrowIfCancellationRequested();

    if (string.IsNullOrWhiteSpace(logoPath))
        return Task.CompletedTask;

    var root = Path.GetFullPath(_storageRoot);

    var fullPath = Path.GetFullPath(
        Path.Combine(
            Directory.GetParent(root)!.FullName,
            logoPath));

    if (!fullPath.StartsWith(
        root + Path.DirectorySeparatorChar,
        StringComparison.Ordinal))
    {
        throw new UnauthorizedAccessException(
            "La ruta del logotipo no está permitida.");
    }

    if (File.Exists(fullPath))
        File.Delete(fullPath);

    return Task.CompletedTask;
}
}

