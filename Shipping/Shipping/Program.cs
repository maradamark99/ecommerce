using System.Text.Json.Serialization;
using EcommerceLib.Contract.Events;
using EcommerceLib.Messaging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.OpenApi.Models;
using Shipping.Contract;
using Shipping.Messaging;

namespace Shipping;

public class Program : ITestEnvironmentMarker
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Configuration.AddJsonFile("appsettings.Development.json");

        builder.Services.AddControllers(options =>
        {
            options.SuppressAsyncSuffixInActionNames = false;
        }).AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        }); 
        builder.Services.AddMemoryCache();
        builder.Services.Configure<ConsumerOptions>(builder.Configuration.GetSection(nameof(ConsumerOptions)));
        builder.Services.Configure<ProducerOptions>(builder.Configuration.GetSection(nameof(ProducerOptions)));


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
        builder.Services.AddSingleton<IOrderEventToShipmentMapper, OrderEventToShipmentMapper>();
        builder.Services.AddSingleton<EventProducerBase<ShippingEventDto>, ShippingEventProducer>();
        builder.Services.AddSingleton<IShippingService, MockShippingService>();
        builder.Services.AddHostedService<OrderEventConsumerService>();
       
        var app = builder.Build();
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        } 
        app.UseExceptionHandler(_ => { });
        app.MapControllers();
        
        await app.RunAsync();
    }
}