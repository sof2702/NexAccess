using System.Security.Cryptography;
using System.Text;
using NexAccess.Application.Interfaces;

namespace NexAccess.Infrastructure.Security;

public class DeviceKeyService : IDeviceKeyService
{
    private const string Prefix = "nxd_";
    private const int KeySize = 32;

    public string GenerateKey() =>
        Prefix + Convert.ToBase64String(RandomNumberGenerator.GetBytes(KeySize)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public string Hash(string key) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key))).ToLowerInvariant();
}
