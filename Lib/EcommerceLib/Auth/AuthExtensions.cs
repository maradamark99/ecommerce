using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace EcommerceLib.Auth;

public static class AuthExtensions
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(x =>
            {
                var authConfig = configuration.GetSection(nameof(AuthConfig)).Get<AuthConfig>();
                var secret = Encoding.Default.GetBytes(authConfig?.JwtSecretKey ?? string.Empty);
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    IssuerSigningKey = new SymmetricSecurityKey(secret),
                    ValidIssuer = authConfig?.JwtIssuer ?? string.Empty,
                    ValidAudience = authConfig?.JwtAudience ?? string.Empty,
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                };
            });
        return services;
    }
}