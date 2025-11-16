using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using Ecommerce.Common.Data;
using Ecommerce.Common.Exception;
using Ecommerce.Common.Filter;
using Ecommerce.Common.File;
using Ecommerce.Common.Storage;
using Ecommerce.Domain.Auth;
using Ecommerce.Domain.Auth.Contract;
using Ecommerce.Domain.Cart;
using Ecommerce.Domain.Inventory;
using Ecommerce.Domain.Notification;
using Ecommerce.Domain.OrderManagement;
using Ecommerce.Domain.Payment;
using Ecommerce.Domain.ProductManagement;
using Ecommerce.Domain.Profile;
using Ecommerce.Domain.Review;
using Ecommerce.Domain.Shipping;
using Ecommerce.Domain.Wishlist;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Minio;

namespace Ecommerce;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Configuration.AddJsonFile("appsettings.json");
        builder.Services.AddControllers(options =>
        {
            options.SuppressAsyncSuffixInActionNames = false;
            options.Filters.Add<ModelValidationFilter>();
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
        builder.Services.AddVersionedApiExplorer(setup =>
        {
            setup.GroupNameFormat = "'v'VVV";
            setup.SubstituteApiVersionInUrl = true;
        });
        builder.Services.AddDbContext<AppDbContext>(optionsBuilder =>
        {
            optionsBuilder.UseNpgsql(
                builder.Configuration.GetConnectionString("Database")
            );
        });
        builder.Services.AddExceptionHandler<ExceptionHandler>();
        builder.Services.Configure<FileConfig>(builder.Configuration.GetSection(nameof(FileConfig)));
        builder.Services.Configure<StorageConfig>(builder.Configuration.GetSection(nameof(StorageConfig)));
        builder.Services.Configure<PaymentConfig>(builder.Configuration.GetSection(nameof(PaymentConfig)));
        builder.Services.Configure<ProductConfig>(builder.Configuration.GetSection(nameof(ProductConfig)));
        builder.Services.Configure<AuthConfig>(builder.Configuration.GetSection(nameof(AuthConfig)));
        builder.Services.Configure<EmailConfig>(builder.Configuration.GetSection(nameof(EmailConfig)));
        
        builder.Services.AddHttpContextAccessor();
        if (!builder.Environment.IsDevelopment())
        {
            
        }
        builder.Services.AddAuthorization();
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(x =>
            {
                var authConfig = builder.Configuration.GetSection("AuthConfig").Get<AuthConfig>();
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
        builder.Services
            .AddIdentity<AppUser, AppRole>(options =>
            {
                options.SignIn.RequireConfirmedEmail = true;
                options.User.RequireUniqueEmail = true;
            })
            .AddDefaultTokenProviders()
            .AddEntityFrameworkStores<AppDbContext>();

        builder.Services.AddSingleton<IMinioClient>(sp =>
        {
            var config = sp.GetRequiredService<IOptions<StorageConfig>>().Value;
            var minioClient = new MinioClient()
                .WithEndpoint(config.Endpoint)
                .WithCredentials(config.AccessKey, config.SecretKey)
                .Build();
            return minioClient;
        });
        
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
        
        builder.Services.AddSingleton(sp => new TokenProvider(sp.GetRequiredService<IOptions<AuthConfig>>()));
        builder.Services.AddSingleton<IFileInspector, NaiveFileInspector>();
        builder.Services.AddSingleton<IFileValidator, FileValidator>();
        builder.Services.AddSingleton<ITokenProvider, TokenProvider>();
        builder.Services.AddSingleton<IStorageClient, MinioStorageClient>();
        builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
        builder.Services.AddScoped<IUserContextService, UserContextService>();
        builder.Services.AddExceptionHandler<ExceptionHandler>();

        builder.Services.AddCartService();
        builder.Services.AddInventoryServices();
        builder.Services.AddNotificationServices();
        builder.Services.AddOrderServices();
        builder.Services.AddPaymentServices();
        builder.Services.AddProductServices();
        builder.Services.AddProfileServices();
        builder.Services.AddReviewServices();
        builder.Services.AddShippingServices(); 
        builder.Services.AddWishlistService();

        builder.Services.AddControllers();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            var devUserConfig = app.Configuration.GetSection(nameof(DevUserConfig)).Get<DevUserConfig>() ?? 
                                throw new InvalidOperationException("DevUserConfig is missing!");
            app.UseSwagger();
            app.UseSwaggerUI();
            app.Use(async (context, next) =>
            {
                var claims = new[]
                {
                    new Claim(ClaimTypes.Email, devUserConfig.Email),
                    new Claim(ClaimTypes.NameIdentifier, devUserConfig.Id),
                    new Claim(ClaimTypes.Role, nameof(Roles.Admin)),
                    new Claim(ClaimTypes.Role, nameof(Roles.Customer)),
                };
                var identity = new ClaimsIdentity(claims, "DevAuth");
                context.User = new ClaimsPrincipal(identity);
                await next();
            });
            using var serviceScope = app.Services.CreateScope();
            var services = serviceScope.ServiceProvider;
            await RoleSeeder.SeedRolesAsync(services);
            
            await EnsureDevUserExists(
                services.GetRequiredService<UserManager<AppUser>>(),
                devUserConfig
            );
        }

        app.UseExceptionHandler(_ => { });
        app.UseAuthorization();
        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.MapControllers();
        
        await app.RunAsync();
    }
    
    private static async Task EnsureDevUserExists(UserManager<AppUser> userManager, DevUserConfig devUserConfig)
    {
        var devUser = await userManager.FindByIdAsync(devUserConfig.Id);
        if (devUser == null)
        {
            devUser = new AppUser
            {
                Id = devUserConfig.Id,
                UserName = devUserConfig.Name,
                Email = devUserConfig.Email,
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(devUser, devUserConfig.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new Exception($"Failed to create dev user: {errors}");
            }
            await userManager.AddToRoleAsync(devUser, nameof(Roles.Customer));
            await userManager.AddToRoleAsync(devUser, nameof(Roles.Admin));
        }
    }
}