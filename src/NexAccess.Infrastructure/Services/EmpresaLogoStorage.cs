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
}

