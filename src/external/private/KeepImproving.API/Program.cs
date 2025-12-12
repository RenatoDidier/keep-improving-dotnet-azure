using FastEndpoints;
using FastEndpoints.Swagger;
using KeepImproving.API.Extensions;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureDatabase(builder.Configuration)
    .AddAuthorization()
    .AddFastEndpointWithSwagger()
    .AddHealthChecks();

WebApplication app = builder.Build();

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