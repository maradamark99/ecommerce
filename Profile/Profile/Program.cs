using EcommerceLib.Auth;
using EcommerceLib.Contract;
using EcommerceLib.Exception;
using EcommerceLib.Messaging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.EntityFrameworkCore;
using Profile.Contract;

namespace Profile;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Configuration.AddJsonFile("appsettings.Development.json");
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddExceptionHandler<ExceptionHandler>();
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddMemoryCache();
        builder.Services.Configure<ConsumerOptions>(builder.Configuration.GetSection(nameof(ConsumerOptions)));
        builder.Services.AddScoped<IProfileRepository, ProfileRepository>();
        builder.Services.AddScoped<IProfileService, ProfileService>();
        builder.Services.AddScoped<IUserContextService, UserContextService>();
        builder.Services.AddHostedService<AuthEventConsumerService>();
        builder.Services.AddApiVersioning(options =>
        {
            options.ReportApiVersions = true;
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
        });
        builder.Services.AddDbContext<AppDbContext>(optionsBuilder =>
        {
            optionsBuilder.UseNpgsql(
                builder.Configuration.GetConnectionString("Database")
            );
        });

        var app = builder.Build();

        app.MapControllers();
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        app.UseExceptionHandler(_ => { });
        
        await app.RunAsync();
    }
}