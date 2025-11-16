namespace Wishlist.Contract;

public interface IWishlistRepository
{
    public Task<Wishlist?> GetWishlistForCustomerAsync(string customerId);
    
    public Task<List<string>> GetCustomersWhoHaveWishlistedTheProductAsync(string productId);
    
    public Task CreateWishlistAsync(string customerId, Product product);

    public Task AddToWishlistAsync(Wishlist wishlist, Product product);

    public Task RemoveFromWishlistAsync(Wishlist wishlist, string productId); 
    
    public Task UpdateProductAsync(Product product);
    
    public Task<Product?> GetProductByIdAsync(string productId);

    public Task UpdateProductListingAsync(string productId, bool isListed);
}