using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexAccess.Application.Interfaces;
using NexAccess.Application.Interfaces.Repositories;
using NexAccess.Infrastructure.Persistence;
using NexAccess.Infrastructure.Persistence.Repositories;
using NexAccess.Infrastructure.Security;
using NexAccess.Infrastructure.Services;
using NexAccess.Infrastructure.Reports;

namespace NexAccess.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "NexAccess";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Falta la cadena de conexión 'ConnectionStrings:{ConnectionStringName}'. Configúrela con User Secrets o la variable de entorno ConnectionStrings__{ConnectionStringName}.");
        }

        var postgresVersion = Version.Parse(configuration["Database:PostgresVersion"] ?? "9.3");
        services.AddDbContext<NexAccessDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql => npgsql.SetPostgresVersion(postgresVersion)));

        services
            .AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(options => Encoding.UTF8.GetByteCount(options.Key ?? string.Empty) >= JwtOptions.MinimumKeyBytes,
                $"Jwt:Key debe tener al menos {JwtOptions.MinimumKeyBytes} bytes. Configúrela con User Secrets o la variable de entorno Jwt__Key.")
            .Validate(options => options.ExpirationMinutes is > 0 and <= 1440, "Jwt:ExpirationMinutes debe estar entre 1 y 1440.")
            .ValidateOnStart();

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IAreaRepository, AreaRepository>();
        services.AddScoped<IEmpresaRepository, EmpresaRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IAccessPermissionRepository, AccessPermissionRepository>();
        services.AddScoped<ICredentialRepository, CredentialRepository>();
        services.AddScoped<IDeviceRepository, DeviceRepository>();
        services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
        services.AddScoped<IDeviceCommandRepository, DeviceCommandRepository>();
        services.AddScoped<IAccessEventRepository, AccessEventRepository>();
        services.AddScoped<IAttendanceRepository, AttendanceRepository>();
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAuditRepository, AuditRepository>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<IDeviceKeyService, DeviceKeyService>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddScoped<IPdfReportGenerator, QuestPdfReportGenerator>();
        services.AddScoped<IEmpresaLogoStorage, EmpresaLogoStorage>();
        return services;
    }
}
