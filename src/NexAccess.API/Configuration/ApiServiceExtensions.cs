using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NexAccess.API.Middleware;
using NexAccess.Application.Interfaces;
using NexAccess.Application.Interfaces.Repositories;
using NexAccess.Application.Services;
using NexAccess.Domain.Enums;
using NexAccess.Infrastructure.Security;

namespace NexAccess.API.Configuration;

public static class ApiServiceExtensions
{
    public const string LoginRateLimitPolicy = "login";

    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpCurrentUser>();

        services
            .AddControllers(options => options.ModelMetadataDetailsProviders.Add(new SpanishValidationMetadataProvider()))
            .AddJsonOptions(options => ConfigureJson(options.JsonSerializerOptions))
            .ConfigureApiBehaviorOptions(options =>
                options.InvalidModelStateResponseFactory = context => ProblemResponses.FromModelState(context.HttpContext, context.ModelState));

        services.AddNexAccessAuthentication();
        services.AddNexAccessOpenApi();
        services.AddRateLimiter(options =>
        {
            var permits = Math.Max(1, configuration.GetValue("RateLimiting:LoginPermitsPerMinute", 10));
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(LoginRateLimitPolicy, context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "desconocido",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = permits, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
        });

        return services;
    }

    public static void ConfigureJson(JsonSerializerOptions options)
    {
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper));
    }

    public static async Task EnsureBootstrapAdminAsync(this WebApplication app)
    {
        var username = app.Configuration["Bootstrap:AdminUsername"];
        var password = app.Configuration["Bootstrap:AdminPassword"];
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
        {
            return;
        }

        using var scope = app.Services.CreateScope();
        var created = await scope.ServiceProvider.GetRequiredService<UserService>().EnsureBootstrapAdminAsync(username, password);
        if (created)
        {
            app.Logger.LogWarning("Se creó el administrador inicial '{Username}'. Cambie la contraseña y elimine Bootstrap:AdminPassword de la configuración.", username);
        }
    }

    private static void AddNexAccessAuthentication(this IServiceCollection services)
    {
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer()
            .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, DeviceKeyAuthenticationHandler>(DeviceKeyAuthenticationHandler.SchemeName, null);

        services
            .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((options, jwt) =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Value.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Value.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = JwtTokenService.CreateSigningKey(jwt.Value.Key),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = JwtTokenService.NameClaim,
                    RoleClaimType = JwtTokenService.RoleClaim
                };
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = ValidateActiveUserAsync,
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        await ProblemResponses.WriteAsync(context.HttpContext, StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "Se requiere un token de acceso válido.");
                    },
                    OnForbidden = context =>
                        ProblemResponses.WriteAsync(context.HttpContext, StatusCodes.Status403Forbidden, "FORBIDDEN", "No tiene permisos para realizar esta operación.")
                };
            });

        services.AddAuthorization();
    }

    private static async Task ValidateActiveUserAsync(TokenValidatedContext context)
    {
        var userId = context.Principal?.FindUserId();
        var role = context.Principal?.FindFirst(JwtTokenService.RoleClaim)?.Value;
        var users = context.HttpContext.RequestServices.GetRequiredService<IUserRepository>();
        var user = userId.HasValue ? await users.GetByIdAsync(userId.Value, context.HttpContext.RequestAborted) : null;
        if (user is null || !user.IsActive || SystemRoles.From(user.SystemRole) != role)
        {
            context.Fail("El usuario no existe, está inactivo o cambió de rol.");
        }
    }
}
