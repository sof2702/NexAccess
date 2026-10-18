using NexAccess.Infrastructure.Security;

namespace NexAccess.UnitTests.Infrastructure;

public class SecurityTests
{
    private readonly Pbkdf2PasswordHasher _hasher = new(1000);

    [Fact]
    public void Password_hash_verifies_only_original_password()
    {
        var hash = _hasher.Hash("Clave-Segura-123");

        Assert.StartsWith("PBKDF2-SHA256$1000$", hash);
        Assert.DoesNotContain("Clave-Segura-123", hash);
        Assert.True(_hasher.Verify("Clave-Segura-123", hash));
        Assert.False(_hasher.Verify("clave-segura-123", hash));
    }

    [Fact]
    public void Same_password_produces_different_salted_hashes()
    {
        Assert.NotEqual(_hasher.Hash("Clave-Segura-123"), _hasher.Hash("Clave-Segura-123"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("texto-plano")]
    [InlineData("PBKDF2-SHA256$abc$xx$yy")]
    [InlineData("PBKDF2-SHA256$1000$no-base64$no-base64")]
    public void Malformed_hash_never_verifies(string hash)
    {
        Assert.False(_hasher.Verify("cualquiera", hash));
    }

    [Fact]
    public void Device_keys_are_random_and_hash_is_deterministic_sha256()
    {
        var keys = new DeviceKeyService();
        var first = keys.GenerateKey();
        var second = keys.GenerateKey();

        Assert.NotEqual(first, second);
        Assert.True(first.Length >= 40);
        Assert.Equal(64, keys.Hash(first).Length);
        Assert.Equal(keys.Hash(first), keys.Hash(first));
        Assert.NotEqual(keys.Hash(first), keys.Hash(second));
    }
}
