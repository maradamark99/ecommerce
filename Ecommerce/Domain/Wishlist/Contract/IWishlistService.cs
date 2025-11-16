namespace Ecommerce.Domain.Wishlist.Contract;

public interface IWishlistService
{
    public Task<IEnumerable<WishlistItemDto>?> GetWishlistAsync(string userId);

    public Task AddToWishlistAsync(string customerId, string productId);

    public Task RemoveFromWishlistAsync(string customerId, string productId);

}