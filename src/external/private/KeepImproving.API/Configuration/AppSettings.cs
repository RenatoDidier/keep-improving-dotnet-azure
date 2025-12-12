using Microsoft.Extensions.Configuration;

namespace KeepImproving.API.Configuration;

public static class AppSettings
{
    private static IConfiguration? _config;
    public static void Initialize(IConfiguration configuration)
    {
        _config = configuration;
    }

    public static string JwtKey =>
        _config!["Jwt:secret"] ??
        throw new InvalidOperationException("Jwt:Key not configured.");

    public static string JwtIssuer =>
        _config!["Jwt:Issuer"] ??
        throw new InvalidOperationException("Jwt:Issuer not configured.");

    public static string JwtAudience =>
        _config!["Jwt:Audience"] ??
        throw new InvalidOperationException("Jwt:Audience not configured.");

}
