using System.Globalization;
using System.Security.Cryptography;
using NexAccess.Application.Interfaces;

namespace NexAccess.Infrastructure.Security;

public class Pbkdf2PasswordHasher : IPasswordHasher
{
    public const int DefaultIterations = 600_000;
    private const string Prefix = "PBKDF2-SHA256";
    private const int SaltSize = 16;
    private const int HashSize = 32;

    private readonly int _iterations;

    public Pbkdf2PasswordHasher() : this(DefaultIterations)
    {
    }

    public Pbkdf2PasswordHasher(int iterations)
    {
        _iterations = iterations > 0 ? iterations : throw new ArgumentOutOfRangeException(nameof(iterations));
    }

    public string Hash(string password)
    {
        ArgumentNullException.ThrowIfNull(password);
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, _iterations, HashAlgorithmName.SHA256, HashSize);
        return string.Join('$', Prefix, _iterations.ToString(CultureInfo.InvariantCulture), Convert.ToBase64String(salt), Convert.ToBase64String(hash));
    }

    public bool Verify(string password, string hash)
    {
        if (password is null || string.IsNullOrEmpty(hash))
        {
            return false;
        }

        var parts = hash.Split('$');
        if (parts.Length != 4 || parts[0] != Prefix || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var iterations) || iterations <= 0)
        {
            return false;
        }

        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
