using System.Text.Json.Serialization;
using EcommerceLib.Auth;
using EcommerceLib.Contract;
using EcommerceLib.Contract.Events;
using EcommerceLib.Exception;
using EcommerceLib.Messaging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Payment.Contract;
using Payment.Messaging;
using Payment.Model;

namespace Payment;

public class Program : ITestEnvironmentMarker
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddMemoryCache();
        builder.Configuration.AddJsonFile("appsettings.Development.json");
        builder.Services.AddControllers(options =>
        {
            options.SuppressAsyncSuffixInActionNames = false;
        }).AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });
        builder.Services.Configure<ConsumerOptions>(builder.Configuration.GetSection(nameof(ConsumerOptions)));
        builder.Services.Configure<ProducerOptions>(builder.Configuration.GetSection(nameof(ProducerOptions)));

        builder.Services.AddDbContext<AppDbContext>(optionsBuilder =>
        {
            optionsBuilder.UseNpgsql(
                builder.Configuration.GetConnectionString("Database")
            );
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

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddSwaggerGen();
        builder.Services.AddExceptionHandler<ExceptionHandler>();   
        builder.Services.AddKeyedScoped(
            typeof(IPaymentStrategy),
            PaymentMethod.CreditCard,
            typeof(AsyncPaymentStrategy));
        builder.Services.AddKeyedScoped(
            typeof(IPaymentStrategy),
            PaymentMethod.CashOnDelivery,
            typeof(PaymentOnDeliveryStrategy));
        builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
        builder.Services.AddScoped<IPaymentService, PaymentService>();
        builder.Services.AddScoped<IStripePaymentService, StripePaymentService>();
        builder.Services.AddHostedService<OrderEventConsumerService>();
        builder.Services.AddSingleton<IEventProducer<PaymentEventDto>, PaymentEventProducerService>();
        builder.Services.AddScoped<IUserContextService, UserContextService>();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();
        app.UseExceptionHandler(_ => { });
        
        app.Run();
    }
}