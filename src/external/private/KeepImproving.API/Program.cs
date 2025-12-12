using KeepImproving.API.Configuration;
using KeepImproving.Infra.Contexts;
using KeepImproving.Infra.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

WebApplicationBuilder? builder = WebApplication.CreateBuilder(args);

ConfigureDatabase(builder);
ConfigureAuthentication(builder);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddAuthorization();



WebApplication? app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.MapGet("/", () => "API Running");

app.Run();



void ConfigureAuthentication(WebApplicationBuilder builder)
{
    builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    }
     )
       .AddJwtBearer(options =>
       {
           options.SaveToken = true;
           options.RequireHttpsMetadata = false;
           options.TokenValidationParameters = new TokenValidationParameters
           {
               ValidateIssuer = true,
               ValidateAudience = true,
               ValidAudience = AppSettings.JwtAudience,
               ValidIssuer = AppSettings.JwtIssuer,
               ClockSkew = TimeSpan.Zero,
               IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(AppSettings.JwtKey))
           };
       }
    );
}

void ConfigureDatabase(WebApplicationBuilder builder)
{
    builder.Services.AddDbContext<AppDbContext>(options =>
    {
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        //var connectionString = "Host=localhost;Port=5432;Database=appdb;Username=appuser;Password=SenhaForte123!;Pooling=true";
        options.UseNpgsql(connectionString);
    });
}