using Review.Model;

namespace Review.Contract;

public interface IReviewRepository
{
    Task<Model.Review?> GetByIdAsync(long id);
    
    Task<IEnumerable<Model.Review>> GetReviewsForProductAsync(string productId);
    
    Task<long> CreateReviewAsync(Model.Review review);
    
    Task DeleteReviewAsync(long id);
    
    Task UpdateReviewAsync(Model.Review review);
    
    Task<bool> HasCustomerPurchasedProduct(string customerId, string productId);
    
    Task SaveCustomerPurchasesAsync(List<CustomerPurchase> customerPurchases);
    
    Task<bool> IsProductListedAsync(string productId);
    
    Task UpdateProductListingAsync(string productId, bool isListed);
    
    Task<List<CustomerPurchase>> GetCustomerPurchasesAsync(string customerId, List<string> productIds);
    
    Task UpdateCustomerPurchasesAsync(List<CustomerPurchase> purchasesToUpdate);
}