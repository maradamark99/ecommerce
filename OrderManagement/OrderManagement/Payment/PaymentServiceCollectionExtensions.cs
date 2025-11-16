using OrderManagement.Common;

namespace OrderManagement.Payment;

public static class PaymentServiceCollectionExtensions
{
    public static IServiceCollection AddPaymentServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<PaymentClientConfig>(configuration.GetSection(nameof(PaymentClientConfig))); 
        services.AddHttpClient<IPaymentClient, PaymentClient>() 
            .SetHandlerLifetime(TimeSpan.FromMinutes(3))
            .AddPolicyHandler(RetryPolicy.Create());
        services.Configure<PaymentEventConsumerConfig>(configuration.GetSection(nameof(PaymentEventConsumerConfig)));
        services.AddHostedService<PaymentEventConsumerService>();
        return services;
    }
    
}