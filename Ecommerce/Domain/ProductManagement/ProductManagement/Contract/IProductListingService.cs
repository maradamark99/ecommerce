namespace Ecommerce.Domain.ProductManagement.ProductManagement.Contract;

public interface IProductListingService
{
    Task<IEnumerable<ProductListingResponse>?> GetProductListingHistoryAsync(string productId);
    
    Task ListProductAsync(ProductListingRequest productListingRequest);
    
    Task DelistProductAsync(string productId);
}