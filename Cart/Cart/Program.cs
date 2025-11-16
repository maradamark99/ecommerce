using System.Text.Json.Serialization;
using Cart.Contract;
using EcommerceLib.Auth;
using EcommerceLib.Contract;
using EcommerceLib.Contract.Events;
using EcommerceLib.Exception;
using EcommerceLib.Filter;
using EcommerceLib.Messaging;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.OpenApi.Models;

namespace Cart;

public class Program : ITestEnvironmentMarker
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddExceptionHandler<ExceptionHandler>();
        builder.Services.AddMemoryCache();
        builder.Services.AddEndpointsApiExplorer();
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
        builder.Services.AddScoped<IUserContextService, UserContextService>();
        builder.Services.AddScoped<IValidator<CheckoutRequestDto>, CheckoutRequestValidator>();
        builder.Services.Configure<ProducerOptions>(builder.Configuration.GetSection(nameof(ProducerOptions)));
        builder.Services.AddSingleton<IEventProducer<CartCheckedOutEventDto>, CartEventProducer>();
        builder.Services.AddSingleton<ICartMapper, CartMapper>();
        builder.Services.Configure<ProductManagementClientConfig>(builder.Configuration.GetSection(nameof(ProductManagementClientConfig)));
        builder.Services.AddHttpClient<IProductManagementClient, ProductManagementClient>();
        builder.Services.AddSingleton<ICartStore, InMemoryCartStore>();
        builder.Services.AddScoped<ICartService, CartService>();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.MapControllers();
        app.UseHttpsRedirection();
        app.UseExceptionHandler(_ => {});
        
        app.Run();
    }
}