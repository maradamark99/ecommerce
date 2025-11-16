namespace Ecommerce.Domain.ProductManagement.ProductManagement.Contract;

public interface IProductListingRepository
{
    Task<List<ProductListing>?> GetProductListingHistoryAsync(string productId);
    
    Task ListProductAsync(string productId, ProductListing productListing);
    
    Task<bool> IsProductAlreadyListedAsync(string productId);
    
    Task DelistProductAsync(string productId);
    
}