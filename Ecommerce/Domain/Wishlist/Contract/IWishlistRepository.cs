using Ecommerce.Domain.Auth;
using Ecommerce.Domain.ProductManagement;

namespace Ecommerce.Domain.Wishlist.Contract;

public interface IWishlistRepository
{
    public Task<Wishlist?> GetWishlistAsync(string customerId);
    
    public Task CreateWishlistAsync(AppUser customer, string productId);

    public Task AddToWishlistAsync(Wishlist wishlist, string productId);

    public Task RemoveFromWishlistAsync(Wishlist wishlist, string productId); 

}