using System.Net;
using System.Net.Mail;
using System.Text.Json.Serialization;
using Auth.Contract;
using EcommerceLib.Auth;
using EcommerceLib.Contract;
using EcommerceLib.Contract.Events;
using EcommerceLib.Messaging;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace Auth;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        builder.Configuration.AddJsonFile("appsettings.json");
        builder.Services.AddControllers(options =>
        {
            options.SuppressAsyncSuffixInActionNames = false;
        }).AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });
            c.SwaggerDoc("v2", new OpenApiInfo { Title = "My API", Version = "v2" });
        });
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

        builder.Services.Configure<ProducerOptions>(builder.Configuration.GetSection(nameof(ProducerOptions)));
        builder.Services.Configure<AuthConfig>(builder.Configuration.GetSection(nameof(AuthConfig)));
        builder.Services.Configure<EmailConfig>(builder.Configuration.GetSection(nameof(EmailConfig)));
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddJwtAuthentication(builder.Configuration);
        builder.Services
            .AddIdentity<AppUser, AppRole>(options =>
            {
                options.SignIn.RequireConfirmedEmail = true;
                options.User.RequireUniqueEmail = true;
            })
            .AddDefaultTokenProviders()
            .AddEntityFrameworkStores<AppDbContext>();
        
        builder.Services.AddSingleton(sp =>
        {
            var config = sp.GetRequiredService<IOptions<EmailConfig>>().Value;
            var isCredentialsEmpty = config.Username.IsNullOrEmpty() || config.Password.IsNullOrEmpty();
            var smtpClient = new SmtpClient(config.Address, config.Port)
            {
                EnableSsl = config.UseSsl,
                UseDefaultCredentials = isCredentialsEmpty,
            };
            if (!config.Username.IsNullOrEmpty())
            {
                smtpClient.Credentials = new NetworkCredential(config.Username, config.Password);
            }
            return smtpClient;
        });
        
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddSingleton<ITokenProvider, TokenProvider>();
        builder.Services.AddSingleton<ITokenProvider, TokenProvider>();
        builder.Services.AddScoped<IUserContextService, UserContextService>();
        builder.Services.AddSingleton<IEmailService, EmailService>();
        builder.Services.AddSingleton<IEventProducer<AuthEventDto>, AuthEventProducer>();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
            using var serviceScope = app.Services.CreateScope();
            var services = serviceScope.ServiceProvider;
            await RoleSeeder.SeedRolesAsync(services);
        }
        
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        
        await app.RunAsync();
    }
}