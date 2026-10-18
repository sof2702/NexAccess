using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexAccess.Application.Interfaces;
using NexAccess.Domain.Entities;
using NexAccess.Domain.Enums;
using NexAccess.Infrastructure.Persistence;
using NexAccess.Infrastructure.Security;
using Npgsql;

namespace NexAccess.IntegrationTests.Support;

public class NexAccessApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string UserSecretsId = "nexaccess-integration-tests";
    public const string ConnectionStringKey = "ConnectionStrings:NexAccessTest";

    public NexAccessApiFactory()
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets(UserSecretsId)
            .AddEnvironmentVariables()
            .Build();

        ConnectionString = configuration[ConnectionStringKey]
            ?? throw new InvalidOperationException(
                $"Configure '{ConnectionStringKey}' con 'dotnet user-secrets set \"{ConnectionStringKey}\" \"...\" --id {UserSecretsId}' o la variable de entorno ConnectionStrings__NexAccessTest.");

        var database = new NpgsqlConnectionStringBuilder(ConnectionString).Database ?? string.Empty;
        if (!database.Contains("test", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Las pruebas de integración solo se ejecutan contra bases de prueba; '{database}' no contiene 'test' en el nombre.");
        }
    }

    public string ConnectionString { get; }
    public string AdminUsername { get; } = $"it_admin_{Guid.NewGuid():N}"[..24];
    public string AdminPassword { get; } = "It-" + Convert.ToHexString(RandomNumberGenerator.GetBytes(12));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:NexAccess", ConnectionString);
        builder.UseSetting("Jwt:Key", Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)));
        builder.UseSetting("RateLimiting:LoginPermitsPerMinute", "10000");
        builder.ConfigureTestServices(services => services.AddSingleton<IPasswordHasher>(new Pbkdf2PasswordHasher(1000)));
    }

    public async Task InitializeAsync()
    {
        await DatabaseScripts.EnsureDatabaseAsync(ConnectionString);
        await DatabaseScripts.ApplyAsync(ConnectionString);

        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<NexAccessDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var now = DateTime.UtcNow;
        context.Users.Add(new User
        {
            Username = AdminUsername,
            PasswordHash = hasher.Hash(AdminPassword),
            SystemRole = SystemRole.Admin,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        });
        await context.SaveChangesAsync();
    }

    Task IAsyncLifetime.DisposeAsync() => Task.CompletedTask;
}

[CollectionDefinition(Name)]
public class ApiCollection : ICollectionFixture<NexAccessApiFactory>
{
    public const string Name = "api";
}
