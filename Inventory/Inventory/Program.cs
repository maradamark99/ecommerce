using EcommerceLib;
using EcommerceLib.Auth;
using EcommerceLib.Contract;
using EcommerceLib.Contract.Events;
using EcommerceLib.Exception;
using EcommerceLib.Messaging;
using Inventory.Contract;
using Inventory.Messaging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Inventory;

public class Program : ITestEnvironmentMarker
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        builder.Configuration.AddJsonFile("appsettings.Development.json");
        builder.Services.AddAuthorization();
        builder.Services.AddMemoryCache();
        
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddControllers();
        builder.Services.AddApiVersioning(options =>
        {
            options.ReportApiVersions = true;
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
        });
        builder.Services.AddDbContext<AppDbContext>((sp, optionsBuilder) =>
        {
            optionsBuilder.UseNpgsql(
                builder.Configuration.GetConnectionString("Database")
            ).AddInterceptors(sp.GetRequiredService<ISaveChangesInterceptor>());
        });
        builder.Services.Configure<ProductEventConsumerConfig>(builder.Configuration.GetSection(nameof(ProductEventConsumerConfig)));
        builder.Services.Configure<OrderEventConsumerConfig>(builder.Configuration.GetSection(nameof(OrderEventConsumerConfig)));
        builder.Services.Configure<ProducerOptions>(builder.Configuration.GetSection(nameof(ProducerOptions)));

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<IUserContextService, UserContextService>();
        builder.Services.AddExceptionHandler<ExceptionHandler>();
        builder.Services.AddSingleton<ISaveChangesInterceptor, StockChangeInterceptor>();
        builder.Services.AddSingleton<EventProducerBase<InventoryEventDto>, InventoryEventProducerService>();
        builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();
        builder.Services.AddScoped<IInventoryService, InventoryService>();
        builder.Services.AddHostedService<ProductEventConsumerService>();
        builder.Services.AddHostedService<OrderEventConsumerService>();
        // builder.Services.AddHostedService<ExpiredReservationsCleanupService>();
        
        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.UseExceptionHandler(_ => {});
        app.MapControllers();

        app.Run();
    }
}