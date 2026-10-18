using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;

namespace NexAccess.API.Configuration;

public static class OpenApiConfiguration
{
    public const string BearerScheme = "Bearer";
    public const string DeviceKeyScheme = "DeviceKey";

    public static IServiceCollection AddNexAccessOpenApi(this IServiceCollection services)
    {
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info = new OpenApiInfo
                {
                    Title = "NexAccess API",
                    Version = "v1",
                    Description = "Control de acceso físico y asistencia con credenciales NFC. Usuarios administrativos con JWT Bearer; dispositivos ESP32 con el encabezado X-DEVICE-KEY."
                };
                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
                document.Components.SecuritySchemes[BearerScheme] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "Token obtenido en POST /api/v1/auth/login."
                };
                document.Components.SecuritySchemes[DeviceKeyScheme] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.ApiKey,
                    In = ParameterLocation.Header,
                    Name = DeviceKeyAuthenticationHandler.HeaderName,
                    Description = "Clave individual del dispositivo ESP32."
                };
                return Task.CompletedTask;
            });

            options.AddOperationTransformer((operation, context, _) =>
            {
                var metadata = context.Description.ActionDescriptor.EndpointMetadata;
                if (metadata.OfType<IAllowAnonymous>().Any())
                {
                    return Task.CompletedTask;
                }

                var authorize = metadata.OfType<IAuthorizeData>().ToList();
                if (authorize.Count == 0)
                {
                    return Task.CompletedTask;
                }

                var scheme = authorize.Any(item => item.AuthenticationSchemes == DeviceKeyAuthenticationHandler.SchemeName)
                    ? DeviceKeyScheme
                    : BearerScheme;
                operation.Security ??= new List<OpenApiSecurityRequirement>();
                operation.Security.Add(new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference(scheme, context.Document)] = new List<string>()
                });
                return Task.CompletedTask;
            });
        });

        return services;
    }
}
