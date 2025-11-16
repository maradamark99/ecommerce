using EcommerceLib.Auth;
using EcommerceLib.Contract;
using EcommerceLib.Exception;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Review.Contract;
using Review.Messaging;

namespace Review;

public class Program : ITestEnvironmentMarker
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Configuration.AddJsonFile("appsettings.Development.json");
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddExceptionHandler<ExceptionHandler>();

        builder.Services.AddEndpointsApiExplorer();
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
        builder.Services.AddControllers();
        builder.Services.AddMemoryCache();
        builder.Services.AddDbContext<AppDbContext>(optionsBuilder =>
        {
            optionsBuilder.UseNpgsql(
                builder.Configuration.GetConnectionString("Database")
            );
        });
        builder.Services.Configure<OrderEventConsumerConfig>(builder.Configuration.GetSection(nameof(OrderEventConsumerConfig)));
        builder.Services.Configure<ProductEventConsumerConfig>(builder.Configuration.GetSection(nameof(ProductEventConsumerConfig)));
        builder.Services.Configure<ProductManagementClientConfig>(builder.Configuration.GetSection(nameof(ProductManagementClientConfig)));
        builder.Services.AddHttpClient<IProductManagementClient, ProductManagementClient>();
        builder.Services.AddScoped<IUserContextService, UserContextService>();
        builder.Services.AddScoped<IReviewService, ReviewService>();
        builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
        builder.Services.AddHostedService<OrderEventConsumerService>(); 
        builder.Services.AddHostedService<ProductEventConsumerService>();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseExceptionHandler(_ => {});

        app.MapControllers();

        app.Run();
    }
}