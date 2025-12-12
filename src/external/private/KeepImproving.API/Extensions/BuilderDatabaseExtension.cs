using KeepImproving.Infra.Contexts;
using Microsoft.EntityFrameworkCore;

namespace KeepImproving.API.Extensions;

public static class BuilderDatabaseExtension
{
    public static IServiceCollection ConfigureDatabase(this IServiceCollection service, IConfiguration configuration)
    {
        service.AddDbContext<AppDbContext>(options =>
        {
            string connection = configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string not found.");

            options.UseNpgsql(connection);
        });

        return service;
    }
}
