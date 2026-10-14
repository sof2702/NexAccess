using Microsoft.Extensions.DependencyInjection;

namespace NexAccess.API.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddOpenApi(this IServiceCollection services)
    {
        // Método de extensión placeholder para mantener compatibilidad con el Program original
        services.AddEndpointsApiExplorer();
        return services;
    }
}
