using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexAccess.Application.Options;
using NexAccess.Application.Services;

namespace NexAccess.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AccessOptions>(configuration.GetSection(AccessOptions.SectionName));
        services.Configure<AttendanceOptions>(configuration.GetSection(AttendanceOptions.SectionName));
        services.Configure<EnrollmentOptions>(configuration.GetSection(EnrollmentOptions.SectionName));

        services.AddSingleton<LocalTime>();
        services.AddScoped<AuditService>();
        services.AddScoped<AuthService>();
        services.AddScoped<UserService>();
        services.AddScoped<EmployeeService>();
        services.AddScoped<AreaService>();
        services.AddScoped<RoleService>();
        services.AddScoped<AccessPermissionService>();
        services.AddScoped<CredentialService>();
        services.AddScoped<DeviceService>();
        services.AddScoped<DeviceCommandService>();
        services.AddScoped<EnrollmentService>();
        services.AddScoped<AttendanceService>();
        services.AddScoped<AccessEventService>();
        services.AddScoped<AccessService>();
        return services;
    }
}
