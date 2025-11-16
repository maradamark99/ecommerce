using OrderManagement.Common;
using OrderManagement.Shipping.Contract;

namespace OrderManagement.Shipping;

public static class ShippingServiceExtensions
{
    public static IServiceCollection AddShippingServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ShippingClientConfig>(configuration.GetSection(nameof(ShippingClientConfig)));
        services.AddHttpClient<IShippingClient, ShippingClient>()
            .SetHandlerLifetime(TimeSpan.FromMinutes(3))
            .AddPolicyHandler(RetryPolicy.Create());
        services.Configure<ShippingEventConsumerConfig>(configuration.GetSection(nameof(ShippingEventConsumerConfig)));
        services.AddHostedService<ShippingEventConsumerService>();
        return services;
    }
}