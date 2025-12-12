using FastEndpoints;
using FastEndpoints.Swagger;

namespace KeepImproving.API.Extensions;

public static class FastEndpointExtension
{
    public static IServiceCollection AddFastEndpointWithSwagger(this IServiceCollection services)
    {
        services.AddFastEndpoints()
                .SwaggerDocument();

        return services;
    }
}
