using Ecommerce.Domain.Wishlist.Contract;

namespace Ecommerce.Domain.Wishlist;

public static class WishlistServiceCollectionExtensions
{
    
    public static IServiceCollection AddWishlistService(this IServiceCollection services)
    {
        services.AddScoped<IWishlistService, WishlistService>();
        services.AddScoped<IWishlistRepository, WishlistRepository>();
        return services;
    }
    
}