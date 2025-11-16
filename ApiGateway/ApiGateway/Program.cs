using EcommerceLib.Auth;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

namespace ApiGateway;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddJwtAuthentication(builder.Configuration);
        builder.Configuration
            .SetBasePath(builder.Environment.ContentRootPath)
            .AddOcelot();
        builder.Services.AddOcelot(builder.Configuration)
            .AddDelegatingHandler<UserClaimsHandler>(true);
        
        var app = builder.Build();
        
        app.UseAuthentication();
        await app.UseOcelot();
        await app.RunAsync();
    }
}