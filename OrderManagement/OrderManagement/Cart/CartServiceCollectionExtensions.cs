namespace OrderManagement.Cart;

public static class CartServiceCollectionExtensions
{

    public static IServiceCollection AddCartServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CartEventConsumerConfig>(configuration.GetSection(nameof(CartEventConsumerConfig)));
        services.AddHostedService<CartEventConsumerService>();
        return services;    
    }
    
}