using FastEndpoints;
using FastEndpoints.Swagger;
using KeepImproving.API.Extensions;
using KeepImproving.Infra.Contexts;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureDatabase(builder.Configuration)
    .AddAuthorization()
    .AddFastEndpointWithSwagger()
    .AddHealthChecks();

WebApplication app = builder.Build();


using (IServiceScope scope = app.Services.CreateScope())
{
    ILogger<Program> logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    AppDbContext? db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var stopWatch = Stopwatch.StartNew();

    try
    {
        logger.LogInformation("Starting EF Core migrations...");
        db.Database.Migrate();
        stopWatch.Stop();

        logger.LogInformation(
            "EF Core migrations completed successfully in {ElapsedMs} ms",
            stopWatch.ElapsedMilliseconds
        );

    } 
    catch (Exception ex)
    {
        logger.LogCritical(
            ex,
            "EF Core migrations failed after {ElapsedMs} ms",
            stopWatch.ElapsedMilliseconds
        );
        throw;
    }
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.UseFastEndpoints(options =>
{
    options.Endpoints.RoutePrefix = "api";
});
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerGen();
}

app.MapHealthChecks("/health");


app.Run();