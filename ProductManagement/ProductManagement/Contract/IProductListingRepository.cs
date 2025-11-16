using ProductManagement.Model;

namespace ProductManagement.Contract;

public interface IProductListingRepository
{
    Task<List<ProductListing>?> GetProductListingHistoryAsync(string productId);
    
    Task ListProductAsync(string productId, ProductListing productListing);
    
    Task<bool> IsProductAlreadyListedAsync(string productId);
    
    Task DelistProductAsync(string productId);
    
    Task<ProductListing?> GetActiveProductListingAsync(string productId);
    
    Task UpdateProductListingAsync(ProductListing listing);
}