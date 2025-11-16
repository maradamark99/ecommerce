using System.Text.Json.Serialization;
using EcommerceLib;
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
using OrderManagement.Cart;
using OrderManagement.Common.Data;
using OrderManagement.Inventory;
using OrderManagement.Messaging;
using OrderManagement.Payment;
using OrderManagement.Shipping;

namespace OrderManagement;

public class Program : ITestEnvironmentMarker
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

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
        builder.Services.AddMemoryCache();
        builder.Services.Configure<ProducerOptions>(builder.Configuration.GetSection(nameof(ProducerOptions)));
        
        builder.Services.AddHttpContextAccessor();
        
        builder.Services.AddSingleton<IEventProducer<OrderEventDto>, OrderEventProducer>();
        builder.Services.AddScoped<IUserContextService, UserContextService>();
        builder.Services.AddExceptionHandler<ExceptionHandler>();

        builder.Services.AddInventoryClient(builder.Configuration);
        builder.Services.AddOrderServices();
        builder.Services.AddPaymentServices(builder.Configuration);
        builder.Services.AddShippingServices(builder.Configuration);
        builder.Services.AddCartServices(builder.Configuration);

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseExceptionHandler(_ => { });
        app.UseAuthorization();
        app.UseHttpsRedirection();
        app.MapControllers();
        
        await app.RunAsync();
    }
    
    
}