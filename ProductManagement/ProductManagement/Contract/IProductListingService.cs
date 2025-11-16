namespace ProductManagement.Contract;

public interface IProductListingService
{
    Task<IEnumerable<ProductListingResponse>?> GetProductListingHistoryAsync(string productId);
    
    Task ListProductAsync(ProductListingRequest productListingRequest);
    
    Task DelistProductAsync(string productId);
    
    Task<bool> IsProductListedAsync(string productId);
    
    Task ApplyDiscountAsync(string productId, DiscountRequest discountRequest);
    
    Task UpdateStockAvailabilityAsync(string productId, bool isInStock);
}