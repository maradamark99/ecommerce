using Ecommerce.Domain.Shipping.Contract;

namespace Ecommerce.Domain.Shipping;

public static class ShippingServiceExtensions
{
    public static IServiceCollection AddShippingServices(this IServiceCollection services)
    {
        services.AddScoped<IShippingService, MockShippingService>();
        return services;
    }
    
}