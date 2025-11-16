using System.Text.Json.Serialization;
using EcommerceLib.Auth;
using EcommerceLib.Contract;
using EcommerceLib.Contract.Events;
using EcommerceLib.Exception;
using EcommerceLib.Filter;
using EcommerceLib.Messaging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Wishlist.Contract;
using Wishlist.Messaging;

namespace Wishlist;

public class Program : ITestEnvironmentMarker
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddMemoryCache();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddExceptionHandler<ExceptionHandler>();
        builder.Configuration.AddJsonFile("appsettings.Development.json");
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
        builder.Services.AddDbContext<AppDbContext>(optionsBuilder =>
        {
            optionsBuilder.UseNpgsql(
                builder.Configuration.GetConnectionString("Database")
            );
        });
        builder.Services.Configure<ProductManagementClientConfig>(builder.Configuration.GetSection(nameof(ProductManagementClientConfig)));
        builder.Services.Configure<ProductEventConsumerConfig>(builder.Configuration.GetSection(nameof(ProductEventConsumerConfig)));
        builder.Services.Configure<ProducerOptions>(builder.Configuration.GetSection(nameof(ProducerOptions)));
        builder.Services.AddScoped<IWishlistRepository, WishlistRepository>();
        builder.Services.AddScoped<IWishlistService, WishlistService>();
        builder.Services.AddHttpClient<IProductManagementClient, ProductManagementClient>();
        builder.Services.AddHostedService<ProductEventConsumerService>();
        builder.Services.AddSingleton<IEventProducer<WishlistEventDto>, WishlistEventProducerService>();
        builder.Services.AddScoped<IUserContextService, UserContextService>();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.MapControllers();
        app.UseHttpsRedirection();
        app.UseExceptionHandler(_ => { });
      
        app.Run();
    }
}