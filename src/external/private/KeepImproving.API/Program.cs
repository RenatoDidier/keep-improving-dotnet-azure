using KeepImproving.API.Extensions;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);


builder.Services.ConfigureDatabase(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.MapGet("/", () => "API Running");

app.Run();
