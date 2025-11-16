using Ecommerce.Domain.Cart.Contract;

namespace Ecommerce.Domain.Cart;

public static class CartServiceCollectionExtensions
{

    public static IServiceCollection AddCartService(this IServiceCollection services)
    {
        services.AddScoped<ICartService, CartService>();
        services.AddSingleton<ICartMapper, CartMapper>();
        services.AddSingleton<ICartStore, InMemoryCartStore>();
        return services;
    }
    
}