using EcommerceLib.Auth;
using EcommerceLib.Contract.Events;

namespace Wishlist.Contract;

public interface IWishlistService
{
    public Task<IEnumerable<WishlistItemDto>?> GetWishlistAsync(AppUser customer);

    public Task AddToWishlistAsync(AppUser customer, string productId);

    public Task RemoveFromWishlistAsync(AppUser customer, string productId);

    public Task UpdateProductAsync(ProductEventDto msg);
    
    public Task UpdateProductListingAsync(ProductEventDto msg, bool isListed);
}